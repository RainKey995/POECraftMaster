using System.Drawing;
using System.Windows.Forms;

namespace POECraftMaster;

internal sealed class SettingsForm : Form
{
    private readonly OverlayHotkey _hotkey;
    private readonly TextBox _shortcut = new() { ReadOnly = true, Width = 240 };
    private readonly Button _record = new() { Text = "录入快捷键", AutoSize = true };
    private readonly Label _status = new();
    private HotkeySettings _pending;
    private bool _recording;

    public SettingsForm(OverlayHotkey hotkey)
    {
        _hotkey = hotkey;
        _pending = hotkey.Settings;
        Text = "POECraftMaster 设置";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(480, 300);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        ShowInTaskbar = false;

        var content = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        content.Controls.Add(new Label
        {
            Text = "显示 / 隐藏覆盖层（全局快捷键）",
            AutoSize = true,
            Margin = new Padding(3, 3, 3, 12)
        });
        var combination = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        ShowShortcut();
        _record.Click += (_, _) =>
        {
            if (_recording)
            {
                StopRecording();
                return;
            }
            hotkey.SetCaptureMode(true);
            _recording = true;
            _record.Text = "取消录入";
            _shortcut.Text = "请按下快捷键…";
            _status.Text = "按下组合键进行录入，Esc 取消。";
        };
        combination.Controls.AddRange([_shortcut, _record]);
        content.Controls.Add(combination);
        content.Controls.Add(new Label
        {
            Text = "点击“录入快捷键”后按下组合键，再点击“保存”。",
            AutoSize = true,
            Margin = new Padding(3, 12, 3, 6)
        });
        var status = _status;
        status.Text = hotkey.Status;
        status.AutoSize = true;
        status.MaximumSize = new Size(420, 0);
        content.Controls.Add(status);
        var saveButton = new Button { Text = "保存", AutoSize = true };
        saveButton.Click += (_, _) =>
        {
            if (_recording)
            {
                status.Text = "请先按下快捷键，或取消录入。";
                return;
            }
            status.Text = hotkey.TryApply(_pending, out var error) ? "快捷键已保存并生效。" : error;
        };
        var closeButton = new Button
        {
            Text = "关闭",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };
        closeButton.Click += (_, _) => Close();
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12)
        };
        footer.Controls.Add(closeButton);
        footer.Controls.Add(saveButton);
        Controls.Add(content);
        Controls.Add(footer);
        CancelButton = closeButton;
        Deactivate += (_, _) => StopRecording();
        FormClosed += (_, _) => StopRecording();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_recording)
            return base.ProcessCmdKey(ref msg, keyData);

        var key = keyData & Keys.KeyCode;
        if (key == Keys.Escape)
        {
            StopRecording();
            return true;
        }
        if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu)
            return true;
        if (!OverlayHotkey.AvailableKeys.Contains(key))
        {
            _status.Text = "请使用 Ctrl、Alt、Shift 搭配字母、数字、F1–F11、空格或导航键。";
            return true;
        }
        _pending = new HotkeySettings(key, keyData.HasFlag(Keys.Control),
            keyData.HasFlag(Keys.Alt), keyData.HasFlag(Keys.Shift));
        StopRecording();
        _status.Text = "快捷键已录入，点击“保存”后生效。";
        return true;
    }

    private void StopRecording()
    {
        if (!_recording)
            return;
        _recording = false;
        _record.Text = "录入快捷键";
        ShowShortcut();
        _hotkey.SetCaptureMode(false);
        _status.Text = string.IsNullOrEmpty(_hotkey.Status) ? "录入已结束，尚未保存。" : _hotkey.Status;
    }

    private void ShowShortcut()
    {
        _shortcut.Text = (_pending.Control ? "Ctrl + " : "") +
            (_pending.Alt ? "Alt + " : "") + (_pending.Shift ? "Shift + " : "") +
            (_pending.Key is >= Keys.D0 and <= Keys.D9
                ? ((int)_pending.Key - (int)Keys.D0).ToString() : _pending.Key.ToString());
    }
}
