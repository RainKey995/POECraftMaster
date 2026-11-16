using System.Windows.Forms;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace POECraftMaster;

internal sealed class OverlaySettingsPanel(
    HotkeySettings initial, string initialStatus, Func<HotkeySettings, Task<string>> save,
    Func<bool, string, Task<string?>> chooseFile)
{
    private HotkeySettings _pending = initial;
    private string _status = initialStatus;
    private Task<string>? _saving;
    private readonly AccountSettingsPanel _account = new(chooseFile);
    private volatile bool _recording;
    private HashSet<Keys> _heldKeys = [];

    public bool FileDialogOpen => _account.FileDialogOpen;
    public bool IsRecording => _recording;

    public void CancelRecording() => _recording = false;

    public void Render()
    {
        if (_saving?.IsCompleted == true)
        {
            _status = _saving.GetAwaiter().GetResult();
            _saving = null;
        }

        ImGui.TextUnformatted("显示 / 隐藏覆盖层（全局快捷键）");
        if (_recording && ImGui.IsWindowFocused())
            RecordShortcut();
        else if (_recording)
            CancelRecording();

        ImGui.BeginDisabled(_saving != null);
        ImGui.TextUnformatted(_recording ? "请按下组合键，Esc 取消。" : ShortcutLabel(_pending));
        ImGui.SameLine();
        if (ImGui.Button(_recording ? "取消录入" : "录入快捷键"))
        {
            if (_recording)
                CancelRecording();
            else
            {
                _heldKeys = ReadHeldKeys();
                _recording = true;
            }
        }
        ImGui.BeginDisabled(_recording);
        if (ImGui.Button("保存快捷键"))
            _saving = save(_pending);
        ImGui.EndDisabled();
        ImGui.EndDisabled();
        ImGui.TextWrapped(_saving != null ? "正在保存…" : _status);
        ImGui.Spacing();
        ImGui.Separator();
        _account.Render();
    }

    private void RecordShortcut()
    {
        var heldKeys = ReadHeldKeys();
        if (heldKeys.Contains(Keys.Escape) && !_heldKeys.Contains(Keys.Escape))
        {
            CancelRecording();
            return;
        }

        Keys key = heldKeys.FirstOrDefault(key => !_heldKeys.Contains(key));
        _heldKeys = heldKeys;
        if (key == Keys.None)
            return;

        _pending = new HotkeySettings(key, IsDown(Keys.ControlKey), IsDown(Keys.Menu), IsDown(Keys.ShiftKey));
        CancelRecording();
        _status = "快捷键已录入，点击「保存快捷键」后生效。";
    }

    private static HashSet<Keys> ReadHeldKeys()
        => OverlayHotkey.AvailableKeys.Append(Keys.Escape).Where(IsDown).ToHashSet();

    private static bool IsDown(Keys key) => (GetAsyncKeyState((int)key) & 0x8000) != 0;

    private static string ShortcutLabel(HotkeySettings settings)
        => (settings.Control ? "Ctrl + " : "") + (settings.Alt ? "Alt + " : "")
            + (settings.Shift ? "Shift + " : "") + KeyLabel(settings.Key);

    private static string KeyLabel(Keys key) => key is >= Keys.D0 and <= Keys.D9
        ? ((int)key - (int)Keys.D0).ToString() : key.ToString();

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);
}
