using System;
using System.Windows.Forms;

namespace POECraftMaster;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var overlay = new PoeOverlay();
        using var tray = new TrayApplicationContext(overlay);
        Application.Run(tray);
    }
}
