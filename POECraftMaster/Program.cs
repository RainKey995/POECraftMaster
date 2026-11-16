using System;
using System.Windows.Forms;

namespace POECraftMaster;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        _ = ConfigurationManager.Instance;
        using var overlay = new PoeOverlay();
        using var tray = new TrayApplicationContext(overlay);
        Application.Run(tray);
    }
}
