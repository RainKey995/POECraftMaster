using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace POECraftMaster;

internal sealed record HotkeySettings(Keys Key = Keys.F10, bool Control = true,
    bool Alt = true, bool Shift = false);

/// <summary>Owns a message-only window and its global shortcut on the tray UI thread.</summary>
internal sealed class OverlayHotkey : NativeWindow, IDisposable
{
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "POECraftMaster", "hotkey.json");
    private int _registeredId;
    public HotkeySettings Settings { get; private set; } = new();
    public string Status { get; private set; } = "";
    public event Action? Pressed;

    public OverlayHotkey()
    {
        CreateHandle(new CreateParams { Parent = new IntPtr(-3) }); // HWND_MESSAGE
        try
        {
            if (File.Exists(_settingsPath))
                Settings = JsonSerializer.Deserialize<HotkeySettings>(File.ReadAllText(_settingsPath))
                    ?? new HotkeySettings();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            Status = "无法读取快捷键配置，已使用默认值。";
        }

        if (TryRegister(Settings, 1, out var error))
            _registeredId = 1;
        else
            Status = error;
    }

    public bool TryApply(HotkeySettings settings, out string error)
    {
        if (settings == Settings && _registeredId != 0)
        {
            error = "";
            return true;
        }

        // Keep the existing registration until both the new registration and save succeed.
        int nextId = _registeredId == 1 ? 2 : 1;
        if (!TryRegister(settings, nextId, out error))
            return false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            var temporaryPath = _settingsPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings));
            File.Move(temporaryPath, _settingsPath, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            UnregisterHotKey(Handle, nextId);
            error = $"无法保存快捷键：{e.Message}";
            return false;
        }

        if (_registeredId != 0)
            UnregisterHotKey(Handle, _registeredId);
        _registeredId = nextId;
        Settings = settings;
        Status = "";
        error = "";
        return true;
    }

    public void SetCaptureMode(bool capturing)
    {
        if (capturing)
        {
            if (_registeredId != 0)
                UnregisterHotKey(Handle, _registeredId);
            _registeredId = 0;
        }
        else if (_registeredId == 0)
        {
            if (TryRegister(Settings, 1, out var error))
            {
                _registeredId = 1;
                Status = "";
            }
            else
                Status = error;
        }
    }

    private bool TryRegister(HotkeySettings settings, int id, out string error)
    {
        if (!AvailableKeys.Contains(settings.Key))
        {
            error = "请选择有效的快捷键。";
            return false;
        }
        uint modifiers = 0x4000; // MOD_NOREPEAT
        if (settings.Control) modifiers |= 0x0002;
        if (settings.Alt) modifiers |= 0x0001;
        if (settings.Shift) modifiers |= 0x0004;
        if (!RegisterHotKey(Handle, id, modifiers, (uint)settings.Key))
        {
            int code = Marshal.GetLastWin32Error();
            error = code == 1409 ? "快捷键已被占用，请选择其他组合。"
                : $"无法注册快捷键：{new Win32Exception(code).Message}";
            return false;
        }
        error = "";
        return true;
    }

    public static Keys[] AvailableKeys { get; } = Enumerable.Range((int)Keys.F1, 11)
        .Concat(Enumerable.Range((int)Keys.A, 26))
        .Concat(Enumerable.Range((int)Keys.D0, 10))
        .Select(key => (Keys)key)
        .Concat([Keys.Insert, Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown, Keys.Space])
        .ToArray();

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x0312 && message.WParam.ToInt32() == _registeredId)
            Pressed?.Invoke();
        base.WndProc(ref message);
    }

    public void Dispose()
    {
        if (_registeredId != 0)
            UnregisterHotKey(Handle, _registeredId);
        _registeredId = 0;
        DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
