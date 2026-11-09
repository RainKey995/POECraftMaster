using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace POECraftMaster.Crafting;

public sealed class CraftProfileStore
{
    public string FilePath { get; }
    public Dictionary<string, CraftOptions> Profiles { get; private set; } = new(StringComparer.Ordinal);
    public string Status { get; private set; } = "";

    public CraftProfileStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "POECraftMaster", "craft-profiles.json");
        try
        {
            if (File.Exists(FilePath))
                Profiles = JsonSerializer.Deserialize<Dictionary<string, CraftOptions>>(File.ReadAllText(FilePath))
                    ?? new(StringComparer.Ordinal);
            else
            {
                string legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AppData", "LocalLow", "PoeHelper", "affixConfig.xml");
                if (filePath == null && File.Exists(legacy)) ImportLegacy(legacy);
                Profiles.TryAdd("紫晶戒指", new CraftOptions { Affixes = "\"风之\"\n\"天才之\"\n\"彗星之\"\n\"挖掘之\"\n\"提耶须之\"\n\"哈斯特之\"\n\"艾菲吉之\"\n\"巴曼斯之\"\n\"阳刚的\"\n\"普兰德斯的\"" });
                Profiles.TryAdd("护身符词缀", new CraftOptions { Affixes = "\"运动员的\"\n\"普兰德斯的\"\n\"风之\"\n\"多维之\"\n\"挖掘之\"\n\"手术之\"\n\"毁灭之\"\n\"巴曼斯之\"" });
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or XmlException)
        {
            Status = $"读取方案失败：{e.Message}（原文件未修改）";
        }
    }

    public void Save(string name, CraftOptions options)
    {
        if (string.IsNullOrWhiteSpace(name)
            || (options.Mode == CraftMode.Alteration && string.IsNullOrWhiteSpace(options.Affixes)))
            throw new ArgumentException("请输入方案名称和词缀。");
        var updated = new Dictionary<string, CraftOptions>(Profiles) { [name.Trim()] = options };
        Persist(updated);
    }

    public void Delete(string name)
    {
        var updated = new Dictionary<string, CraftOptions>(Profiles);
        updated.Remove(name);
        Persist(updated);
    }

    public void ImportLegacy(string path)
    {
        using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
        var document = XDocument.Load(reader);
        var group = document.Root?.Element("AlterationAndAugmentation");
        if (group == null) throw new XmlException("没有找到旧版词缀配置组。");
        var updated = new Dictionary<string, CraftOptions>(Profiles);
        foreach (var element in group.Elements())
            updated.TryAdd(element.Name.LocalName, new CraftOptions { Affixes = element.Value.Replace("\r", "") });
        // Existing names win; importing never overwrites a user-edited profile.
        Persist(updated);
        Status = "已导入旧版词缀方案（同名方案保留当前版本）。";
    }

    private void Persist(Dictionary<string, CraftOptions> profiles)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!);
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, FilePath, true);
        Profiles = profiles;
        Status = "方案已保存。";
    }
}
