using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using POECraftMaster.Helper;

namespace POECraftMaster.Crafting;

public interface IGameAutomation
{
    void SelectCurrencyStash(CancellationToken token) { }
    void Wait(int milliseconds, CancellationToken token);
    void UseCurrency(RelativePoint currency, RelativePoint item, int interval, CancellationToken token);
    string ReadItem(RelativePoint item, CancellationToken token);
}

internal sealed class GameAutomation(IntPtr gameWindow, Rectangle bounds) : IGameAutomation
{
    private Rectangle _stashBounds;

    public void SelectCurrencyStash(CancellationToken token)
    {
        Check(token);
        using (var selection = new CurrencyStashSelectionForm(bounds, token,
            () => TryGetBounds(gameWindow, out var current) && current == bounds))
        {
            if (selection.ShowDialog() != DialogResult.OK)
                throw new OperationCanceledException("已取消仓库区域选择。", token);
            token.ThrowIfCancellationRequested();
            _stashBounds = selection.Selection;
        }
        CurrencyStashRegion.Validate(_stashBounds, bounds.Size);
        if (!ScreenHelper.SetForegroundWindow(gameWindow))
            throw new OperationCanceledException("无法激活游戏窗口，已停止。", token);
        Wait(300, token);
    }

    private void Check(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if ((GetAsyncKeyState((int)Keys.F12) & 0x8000) != 0)
            throw new OperationCanceledException("已按 F12 停止。", token);
        if (ScreenHelper.GetForegroundWindow() != gameWindow || ScreenHelper.IsIconic(gameWindow))
            throw new OperationCanceledException("游戏失去焦点，已停止。", token);
        if (!TryGetBounds(gameWindow, out var current) || current != bounds)
            throw new OperationCanceledException("游戏窗口位置或尺寸已改变，请重新开始。", token);
    }

    public static bool TryGetBounds(IntPtr handle, out Rectangle bounds)
    {
        var rect = new ScreenHelper.U32Rect();
        var origin = new ScreenHelper.U32Point();
        bounds = Rectangle.Empty;
        if (handle == IntPtr.Zero || !ScreenHelper.GetClientRect(handle, ref rect)
            || !ScreenHelper.ClientToScreen(handle, ref origin) || rect.Width <= 0 || rect.Height <= 0)
            return false;
        bounds = new Rectangle(origin.x, origin.y, rect.Width, rect.Height);
        return true;
    }

    public void Wait(int milliseconds, CancellationToken token)
    {
        var timer = Stopwatch.StartNew();
        do
        {
            Check(token);
            token.WaitHandle.WaitOne(Math.Min(20, Math.Max(1, milliseconds - (int)timer.ElapsedMilliseconds)));
        } while (timer.ElapsedMilliseconds < milliseconds);
        Check(token);
    }

    private void Move(RelativePoint position, CancellationToken token, Rectangle? coordinateBounds = null)
    {
        Check(token);
        Point point = position.ToScreen(coordinateBounds ?? bounds);
        if (!SetCursorPos(point.X, point.Y)) throw new Win32Exception(Marshal.GetLastWin32Error());
        Wait(80, token);
    }

    public void UseCurrency(RelativePoint currency, RelativePoint item, int interval, CancellationToken token)
    {
        Check(token);
        CurrencyStashRegion.Validate(_stashBounds, bounds.Size);
        var screenStash = _stashBounds;
        screenStash.Offset(bounds.Location);
        Move(currency, token, screenStash);
        Click(true, token);
        Wait(interval, token);
        Move(item, token);
        Click(false, token);
        Wait(interval, token);
    }

    private void Click(bool right, CancellationToken token)
    {
        Check(token);
        // User-held modifiers could turn a normal click into a different in-game action.
        EnsureModifiersReleased();
        try
        {
            Send([Mouse(right ? 0x0008u : 0x0002u), Mouse(right ? 0x0010u : 0x0004u)]);
        }
        catch
        {
            SendInput(1, [Mouse(right ? 0x0010u : 0x0004u)], Marshal.SizeOf<Input>());
            throw;
        }
    }

    public string ReadItem(RelativePoint item, CancellationToken token)
    {
        Move(item, token);
        Wait(300, token);
        EnsureModifiersReleased();
        uint sequence = GetClipboardSequenceNumber();
        Check(token);
        try
        {
            Send([Key(0x11), Key(0x12), Key(0x43), Key(0x43, true), Key(0x12, true), Key(0x11, true)]);
        }
        finally
        {
            // Release only the keys injected by this operation, including partial SendInput failure.
            SendInput(3, [Key(0x43, true), Key(0x12, true), Key(0x11, true)], Marshal.SizeOf<Input>());
        }
        var timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < 1500)
        {
            Wait(30, token);
            if (GetClipboardSequenceNumber() == sequence) continue;
            try
            {
                string text = Clipboard.GetText(TextDataFormat.UnicodeText);
                string compact = text.Replace(" ", "");
                if (text.Contains("--------", StringComparison.Ordinal)
                    && (compact.Contains("Rarity:", StringComparison.OrdinalIgnoreCase)
                        || compact.Contains("稀有度：", StringComparison.Ordinal)
                        || compact.Contains("稀有度:", StringComparison.Ordinal)))
                    return text;
            }
            catch (ExternalException) { /* Clipboard temporarily owned by the game; retry. */ }
        }
        throw new InvalidOperationException("未获取到新的装备信息，已停止。请检查背包、鼠标坐标及游戏复制功能。");
    }

    private static void EnsureModifiersReleased()
    {
        int[] keys = [0x10, 0x11, 0x12, 0x5B, 0x5C];
        if (keys.Any(key => (GetAsyncKeyState(key) & 0x8000) != 0))
            throw new InvalidOperationException("检测到按住的修饰键，已停止。请松开 Shift / Ctrl / Alt / Win 后重试。");
    }

    private static Input Mouse(uint flags) => new() { Type = 0, Data = new InputUnion { Mouse = new MouseInput { Flags = flags } } };
    private static Input Key(ushort key, bool up = false) => new() { Type = 1,
        Data = new InputUnion { Keyboard = new KeyboardInput { Key = key, Flags = up ? 2u : 0u } } };
    private static void Send(Input[] inputs)
    {
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
            throw new InvalidOperationException("发送游戏输入失败，请确认游戏与工具的运行权限一致。");
    }

    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion
    {
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public KeyboardInput Keyboard;
    }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput
    {
        public int X, Y;
        public uint MouseData, Flags, Time;
        public UIntPtr ExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput
    {
        public ushort Key, Scan;
        public uint Flags, Time;
        public UIntPtr ExtraInfo;
    }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern uint GetClipboardSequenceNumber();
}
