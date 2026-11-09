using System.Drawing;
using System.Windows.Forms;

namespace POECraftMaster;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly PoeOverlay _overlay;
    private readonly Icon _icon;
    private readonly ContextMenuStrip _menu;
    private readonly NotifyIcon _trayIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Task _overlayTask;
    private SettingsForm? _settingsForm;
    private readonly OverlayHotkey _hotkey;
    private readonly ToolStripMenuItem _stopCrafting;

    public TrayApplicationContext(PoeOverlay overlay)
    {
        _overlay = overlay;
        _hotkey = new OverlayHotkey();
        _hotkey.Pressed += () =>
        {
            if (_settingsForm == null || !_settingsForm.ContainsFocus)
                _overlay.ToggleVisibility();
        };
        _icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "poe.ico"));
        _menu = new ContextMenuStrip();
        _menu.Items.Add("设置", null, (_, _) => ShowSettings());
        _stopCrafting = new ToolStripMenuItem("停止制作", null, (_, _) => _overlay.StopCrafting());
        _stopCrafting.Enabled = false;
        _menu.Items.Add(_stopCrafting);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("退出", null, (_, _) => ExitThread());
        _trayIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "POECraftMaster",
            ContextMenuStrip = _menu,
            Visible = true
        };
        _overlayTask = overlay.Run();
        _timer = new System.Windows.Forms.Timer { Interval = 250 };
        _timer.Tick += CheckOverlay;
        _timer.Start();
    }

    private void ShowSettings()
    {
        _overlay.StopCrafting();
        _overlay.SetSettingsOpen(true);
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(_hotkey) { Icon = _icon };
            _settingsForm.FormClosed += (_, _) => _overlay.SetSettingsOpen(false);
            _settingsForm.Show();
        }

        if (_settingsForm.WindowState == FormWindowState.Minimized)
            _settingsForm.WindowState = FormWindowState.Normal;
        _settingsForm.Activate();
    }

    private void CheckOverlay(object? sender, EventArgs e)
    {
        var progress = _overlay.CraftProgress;
        _stopCrafting.Enabled = progress.Running;
        _trayIcon.Text = progress.Running
            ? $"POECraftMaster 制作中 {progress.Completed}/{progress.Total}，{progress.Attempts} 轮"
            : "POECraftMaster";
        if (!_overlayTask.IsCompleted)
            return;
        try
        {
            _overlayTask.GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "POECraftMaster", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _trayIcon.Visible = false;
        _settingsForm?.Close();
        _overlay.Close();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _settingsForm?.Dispose();
            _hotkey.Dispose();
            _timer.Dispose();
            _trayIcon.Dispose();
            _menu.Dispose();
            _icon.Dispose();
        }
        base.Dispose(disposing);
    }
}
