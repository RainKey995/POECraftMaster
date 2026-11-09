using System.Runtime.InteropServices;

namespace POECraftMaster.Helper;

internal static class BackdropHelper
{
    // Accent blur is used because DwmEnableBlurBehindWindow no longer blurs on Windows 8+.
    // This optional Windows composition API may be unavailable; keep a tinted fallback.
    public static bool SetBlur(IntPtr windowHandle, bool enabled)
    {
        var policy = new AccentPolicy { State = enabled ? 3 : 0 }; // BLURBEHIND / DISABLED
        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<AccentPolicy>());
        try
        {
            Marshal.StructureToPtr(policy, pointer, false);
            var data = new CompositionAttributeData
            {
                Attribute = 19, // WCA_ACCENT_POLICY
                Data = pointer,
                Size = (nuint)Marshal.SizeOf<AccentPolicy>()
            };
            return SetWindowCompositionAttribute(windowHandle, ref data);
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int State;
        public int Flags;
        public uint GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public nuint Size;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowCompositionAttribute(IntPtr windowHandle,
        ref CompositionAttributeData data);
}
