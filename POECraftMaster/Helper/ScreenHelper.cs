using System.Runtime.InteropServices;

namespace POECraftMaster.Helper;

/// <summary>
/// 窗口类,与窗口相关的所有方法
/// </summary>
public static class ScreenHelper
{
    private const int GwlExStyle = -20;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExAppWindow = 0x00040000;

    // Call before the overlay is first shown so Explorer never adds a taskbar button.
    public static void HideFromTaskbar(IntPtr windowHandle)
    {
        Marshal.SetLastPInvokeError(0);
        int style = GetWindowLong(windowHandle, GwlExStyle);
        int error = Marshal.GetLastPInvokeError();
        if (style == 0 && error != 0)
            throw new System.ComponentModel.Win32Exception(error);

        style = (style | WsExToolWindow) & ~WsExAppWindow;
        Marshal.SetLastPInvokeError(0);
        int previousStyle = SetWindowLong(windowHandle, GwlExStyle, style);
        error = Marshal.GetLastPInvokeError();
        if (previousStyle == 0 && error != 0)
            throw new System.ComponentModel.Win32Exception(error);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int index, int value);

    [StructLayout(LayoutKind.Sequential)]
    public struct U32Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
        public int Width => right - left;
        public int Height => bottom - top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct U32Point
    {
        public int x;
        public int y;
    }

    [DllImport("user32")]
    public static extern IntPtr WindowFromPoint(U32Point point);

    [DllImport("user32")]
    public static extern IntPtr GetWindowRect(IntPtr hWnd, ref U32Rect rect);

    [DllImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, ref U32Rect rect);

    [DllImport("user32")]
    public static extern IntPtr ScreenToClient(IntPtr hWnd, ref U32Point point);

    [DllImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClientToScreen(IntPtr hWnd, ref U32Point point);

    [DllImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
