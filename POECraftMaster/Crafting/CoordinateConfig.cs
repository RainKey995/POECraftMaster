using System.Text.Json;

namespace POECraftMaster.Crafting;

// This DTO is deliberately separate from CraftOptions so the file format can evolve independently.
public sealed record CalibrationPoint
{
    public required float X { get; init; }
    public required float Y { get; init; }

    public static CalibrationPoint From(RelativePoint point) => new() { X = point.X * 100, Y = point.Y * 100 };
    public RelativePoint ToRelative() => new(X / 100, Y / 100);
}

public sealed record CoordinateConfig
{
    public required int SchemaVersion { get; init; }
    public required string Units { get; init; }
    public required CalibrationPoint Alteration { get; init; }
    public required CalibrationPoint Augmentation { get; init; }
    public required CalibrationPoint Scouring { get; init; }
    public required CalibrationPoint Chance { get; init; }
    public required CalibrationPoint InventoryOrigin { get; init; }
    public required CalibrationPoint InventoryStep { get; init; }

    public static CoordinateConfig FromOptions(CraftOptions options) => new()
    {
        SchemaVersion = 1, Units = "percent",
        Alteration = CalibrationPoint.From(options.Alteration),
        Augmentation = CalibrationPoint.From(options.Augmentation),
        Scouring = CalibrationPoint.From(options.Scouring),
        Chance = CalibrationPoint.From(options.Chance),
        InventoryOrigin = CalibrationPoint.From(options.InventoryOrigin),
        InventoryStep = CalibrationPoint.From(options.InventoryStep)
    };

    public CraftOptions ApplyTo(CraftOptions options)
    {
        Validate();
        return options with
        {
            Alteration = Alteration.ToRelative(), Augmentation = Augmentation.ToRelative(),
            Scouring = Scouring.ToRelative(), Chance = Chance.ToRelative(),
            InventoryOrigin = InventoryOrigin.ToRelative(), InventoryStep = InventoryStep.ToRelative()
        };
    }

    public void Validate()
    {
        if (SchemaVersion != 1) throw new ArgumentException($"不支持的坐标配置版本：{SchemaVersion}，当前支持 1。");
        if (Units != "percent") throw new ArgumentException("坐标配置 units 必须为 percent（百分比）。");
        CalibrationPoint?[] points = [Alteration, Augmentation, Scouring, Chance, InventoryOrigin, InventoryStep];
        if (points.Any(point => point == null || !point.ToRelative().IsValid))
            throw new ArgumentException("六组坐标必须完整，x、y 必须是大于 0 且小于 100 的有限数字。");
        var lastSlot = new RelativePoint(InventoryOrigin.X / 100 + 11 * InventoryStep.X / 100,
            InventoryOrigin.Y / 100 + 4 * InventoryStep.Y / 100);
        if (!lastSlot.IsValid) throw new ArgumentException("背包起点和步长使背包格子超出游戏客户区。");
    }
}

public static class CoordinateConfigFile
{
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "POECraftMaster", "coordinates.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static CoordinateConfig Load(string path)
    {
        var config = JsonSerializer.Deserialize<CoordinateConfig>(File.ReadAllText(path), JsonOptions)
            ?? throw new JsonException("坐标配置不能为空。");
        config.Validate();
        return config;
    }

    public static void Save(string path, CoordinateConfig config)
    {
        config.Validate();
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string temporary = fullPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(config, JsonOptions));
        File.Move(temporary, fullPath, true);
    }
}
