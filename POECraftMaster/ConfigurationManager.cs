using System.Text.Json;
using POECraftMaster.Crafting;
using POECraftMaster.Singleton;

namespace POECraftMaster;

internal sealed class ConfigurationManager : Singleton<ConfigurationManager>
{
    public static string DirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "POECraftMaster");

    public static string CoordinatesPath { get; } = Path.Combine(DirectoryPath, "coordinates.json");
    public static string ProfilesPath { get; } = Path.Combine(DirectoryPath, "craft-profiles.json");

    public string AccountPath { get; } = Path.Combine(DirectoryPath, "account.json");
    private string HotkeyPath { get; } = Path.Combine(DirectoryPath, "hotkey.json");
    public CraftProfileStore Profiles { get; }
    public CoordinateConfig? Coordinates { get; }
    public string? CoordinatesError { get; }
    public AccountConfig? DefaultAccount { get; }
    public string? AccountError { get; }
    public HotkeySettings Hotkey { get; private set; } = new();
    public string HotkeyError { get; }

    public ConfigurationManager()
    {
        Profiles = new CraftProfileStore(ProfilesPath);
        Coordinates = Load(() => CoordinateConfigFile.Load(CoordinatesPath), out var coordinatesError);
        CoordinatesError = coordinatesError;
        DefaultAccount = Load(() =>
        {
            if (!File.Exists(AccountPath))
                new AccountConfig().Save(AccountPath, overwrite: false);
            return ReadAccount(AccountPath);
        }, out var accountError);
        AccountError = accountError;
        Hotkey = Load(() => File.Exists(HotkeyPath)
            ? JsonSerializer.Deserialize<HotkeySettings>(File.ReadAllText(HotkeyPath))
            : new HotkeySettings(), out var hotkeyError) ?? new HotkeySettings();
        HotkeyError = hotkeyError == null ? "" : "无法读取快捷键配置，已使用默认值。";
    }

    public AccountConfig? ReadAccount(string path) => AccountConfig.Load(path);

    public void SaveAccount(string path, AccountConfig account) => account.Save(path);

    public void SaveHotkey(HotkeySettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        string temporary = Path.Combine(DirectoryPath, Path.GetRandomFileName());
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings));
            File.Move(temporary, HotkeyPath, overwrite: true);
            Hotkey = settings;
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    private static T? Load<T>(Func<T?> read, out string? error) where T : class
    {
        try
        {
            error = null;
            return read();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException
            or InvalidOperationException or JsonException or NotSupportedException or InvalidDataException)
        {
            error = e.Message;
            return null;
        }
    }
}
