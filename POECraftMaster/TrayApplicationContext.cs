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
    private readonly Control _dispatcher = new();
    private readonly OverlayHotkey _hotkey;
    private readonly ToolStripMenuItem _stopCrafting;

    public TrayApplicationContext(PoeOverlay overlay)
    {
        _overlay = overlay;
        _hotkey = new OverlayHotkey();
        _hotkey.Pressed += _overlay.ToggleVisibility;
        _ = _dispatcher.Handle;
        _overlay.ConfigureSettings(_hotkey.Settings, _hotkey.Status, SaveHotkey, ChooseAccountFile);
        _icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "poe.ico"));
        _menu = new ContextMenuStrip();
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

    private Task<string> SaveHotkey(HotkeySettings settings)
        => RunOnTrayThread(() => _hotkey.TryApply(settings, out var error) ? "快捷键已保存并生效。" : error);

    private Task<string?> ChooseAccountFile(bool save, string path)
        => RunOnTrayThread<string?>(() =>
        {
            using FileDialog dialog = save ? new SaveFileDialog() : new OpenFileDialog();
            dialog.Filter = "JSON 配置文件 (*.json)|*.json";
            dialog.DefaultExt = "json";
            dialog.InitialDirectory = Path.GetDirectoryName(path);
            dialog.FileName = Path.GetFileName(path);
            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
        });

    private Task<T> RunOnTrayThread<T>(Func<T> action)
    {
        var result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcher.BeginInvoke(() =>
        {
            try
            {
                result.SetResult(action());
            }
            catch (Exception exception)
            {
                result.SetException(exception);
            }
        });
        return result.Task;
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
        _overlay.Close();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dispatcher.Dispose();
            _hotkey.Dispose();
            _timer.Dispose();
            _trayIcon.Dispose();
            _menu.Dispose();
            _icon.Dispose();
        }
        base.Dispose(disposing);
    }
}
