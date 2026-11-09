using System.Drawing;
using System.Windows.Forms;

namespace POECraftMaster;

internal sealed class SettingsForm : Form
{
    public SettingsForm(OverlayHotkey hotkey)
    {
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
        var control = new CheckBox { Text = "Ctrl", AutoSize = true, Checked = hotkey.Settings.Control };
        var alt = new CheckBox { Text = "Alt", AutoSize = true, Checked = hotkey.Settings.Alt };
        var shift = new CheckBox { Text = "Shift", AutoSize = true, Checked = hotkey.Settings.Shift };
        var key = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        foreach (var availableKey in OverlayHotkey.AvailableKeys)
            key.Items.Add(availableKey);
        key.SelectedItem = hotkey.Settings.Key;
        combination.Controls.AddRange([control, alt, shift, key]);
        content.Controls.Add(combination);
        content.Controls.Add(new Label
        {
            Text = "选择组合键后点击“保存”，重启应用后仍然有效。",
            AutoSize = true,
            Margin = new Padding(3, 12, 3, 6)
        });
        var status = new Label
        {
            Text = hotkey.Status,
            AutoSize = true,
            MaximumSize = new Size(420, 0)
        };
        content.Controls.Add(status);
        var saveButton = new Button { Text = "保存", AutoSize = true };
        saveButton.Click += (_, _) =>
        {
            if (key.SelectedItem is not Keys selectedKey)
            {
                status.Text = "请选择快捷键。";
                return;
            }
            var settings = new HotkeySettings(selectedKey, control.Checked, alt.Checked, shift.Checked);
            status.Text = hotkey.TryApply(settings, out var error) ? "快捷键已保存并生效。" : error;
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
    }
}
