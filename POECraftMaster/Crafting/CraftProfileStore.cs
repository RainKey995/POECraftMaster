using System.Text.Json;
using System.Text.Json.Serialization;

namespace POECraftMaster.Crafting;

public sealed class CraftProfileStore
{
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "POECraftMaster", "craft-profiles.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string FilePath { get; }
    public Dictionary<string, CraftOptions> Profiles { get; private set; } = new(StringComparer.Ordinal);
    public string Status { get; private set; } = "";

    public CraftProfileStore(string? filePath = null)
    {
        FilePath = filePath ?? DefaultPath;
        try
        {
            if (File.Exists(FilePath)) Profiles = ReadProfiles(FilePath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            Status = $"读取方案失败：{e.Message}（原文件未修改）";
        }
    }

    public void Save(string name, CraftOptions options)
    {
        var profile = ProfileSettings.FromOptions(options);
        Validate(name, profile);
        var updated = new Dictionary<string, CraftOptions>(Profiles, StringComparer.Ordinal)
        {
            [name.Trim()] = profile.ToOptions()
        };
        Persist(updated);
    }

    public void Delete(string name)
    {
        var updated = new Dictionary<string, CraftOptions>(Profiles, StringComparer.Ordinal);
        updated.Remove(name);
        Persist(updated);
    }

    public void ImportJson(string path)
    {
        var imported = ReadProfiles(path);
        var updated = new Dictionary<string, CraftOptions>(Profiles, StringComparer.Ordinal);
        foreach (var (name, profile) in imported) updated.TryAdd(name, profile);
        Persist(updated);
        Status = "已导入 JSON 词缀方案（同名方案保留当前版本）。";
    }

    public void ExportJson(string path)
    {
        WriteProfiles(path, Profiles);
        Status = $"词缀方案已导出：{Path.GetFullPath(path)}";
    }

    private static Dictionary<string, CraftOptions> ReadProfiles(string path)
    {
        var source = JsonSerializer.Deserialize<Dictionary<string, ProfileSettings?>>(File.ReadAllText(path), JsonOptions)
            ?? throw new JsonException("词缀配置不能为空，请使用以方案名称为键的 JSON 对象。");
        var profiles = new Dictionary<string, CraftOptions>(StringComparer.Ordinal);
        foreach (var (name, profile) in source)
        {
            if (profile == null) throw new JsonException($"词缀方案“{name}”不能为空。");
            Validate(name, profile);
            if (!profiles.TryAdd(name.Trim(), profile.ToOptions()))
                throw new JsonException($"词缀方案名称重复：{name.Trim()}。");
        }
        return profiles;
    }

    private static void Validate(string name, ProfileSettings profile)
    {
        if (string.IsNullOrWhiteSpace(name) || !Enum.IsDefined(profile.Mode)
            || (profile.Mode == CraftMode.Alteration && string.IsNullOrWhiteSpace(profile.Affixes)))
            throw new ArgumentException("请输入有效的方案名称、制作模式和词缀。");
        if (profile.RequiredMatches is < 1 or > 2 || profile.IntervalMs is < 50 or > 2000
            || profile.MaxAttempts is < 1 or > 100000 || profile.SelectedSlots == null
            || profile.SelectedSlots.Any(slot => slot is < 0 or >= 60))
            throw new ArgumentException($"词缀方案“{name}”的制作参数或背包格子无效。");
    }

    private void Persist(Dictionary<string, CraftOptions> profiles)
    {
        WriteProfiles(FilePath, profiles);
        Profiles = profiles;
        Status = "方案已保存。";
    }

    private static void WriteProfiles(string path, Dictionary<string, CraftOptions> profiles)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var document = profiles.ToDictionary(p => p.Key, p => ProfileSettings.FromOptions(p.Value), StringComparer.Ordinal);
        string temporary = fullPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(document, JsonOptions));
        File.Move(temporary, fullPath, true);
    }

    // Coordinates belong to coordinates.json, separate from affix recipes.
    private sealed record ProfileSettings
    {
        public CraftMode Mode { get; init; }
        public string Affixes { get; init; } = "";
        public int RequiredMatches { get; init; } = 1;
        public int IntervalMs { get; init; } = 100;
        public bool UseAugmentation { get; init; }
        public int MaxAttempts { get; init; } = 1000;
        public int[] SelectedSlots { get; init; } = [];

        public static ProfileSettings FromOptions(CraftOptions options) => new()
        {
            Mode = options.Mode, Affixes = options.Affixes, RequiredMatches = options.RequiredMatches,
            IntervalMs = options.IntervalMs, UseAugmentation = options.UseAugmentation,
            MaxAttempts = options.MaxAttempts, SelectedSlots = options.SelectedSlots?.ToArray()!
        };

        public CraftOptions ToOptions() => new()
        {
            Mode = Mode, Affixes = Affixes, RequiredMatches = RequiredMatches, IntervalMs = IntervalMs,
            UseAugmentation = UseAugmentation, MaxAttempts = MaxAttempts, SelectedSlots = SelectedSlots.ToArray()
        };
    }
}