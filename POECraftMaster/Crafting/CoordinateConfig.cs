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
    public required CalibrationPoint InventoryOrigin { get; init; }
    public required CalibrationPoint InventoryStep { get; init; }
    public required CalibrationPoint ScrollFragment { get; init; }
    public required CalibrationPoint ScrollofWisdom { get; init; }
    public required CalibrationPoint PortalScroll { get; init; }
    public required CalibrationPoint OrbofTransmutation { get; init; }
    public required CalibrationPoint TransmutationShard { get; init; }
    public required CalibrationPoint OrbofAlteration { get; init; }
    public required CalibrationPoint AlterationShard { get; init; }
    public required CalibrationPoint OrbofAnnulment { get; init; }
    public required CalibrationPoint AnnulmentShard { get; init; }
    public required CalibrationPoint OrbofChance { get; init; }
    public required CalibrationPoint OrbofAugmentation { get; init; }
    public required CalibrationPoint FracturingOrb { get; init; }
    public required CalibrationPoint FracturingShard { get; init; }
    public required CalibrationPoint JewellersOrb { get; init; }
    public required CalibrationPoint AncientOrb { get; init; }
    public required CalibrationPoint OrbofFusing { get; init; }
    public required CalibrationPoint OrbofBinding { get; init; }
    public required CalibrationPoint ChromaticOrb { get; init; }
    public required CalibrationPoint HinekorasLock { get; init; }
    public required CalibrationPoint EnkindlingOrb { get; init; }
    public required CalibrationPoint InstillingOrb { get; init; }
    public required CalibrationPoint ExaltedOrb { get; init; }
    public required CalibrationPoint ExaltedShard { get; init; }
    public required CalibrationPoint MirrorofKalandra { get; init; }
    public required CalibrationPoint MirrorShard { get; init; }
    public required CalibrationPoint BlacksmithsWhetstone { get; init; }
    public required CalibrationPoint ArmourersScrap { get; init; }
    public required CalibrationPoint GlassblowersBauble { get; init; }
    public required CalibrationPoint GemcuttersPrism { get; init; }
    public required CalibrationPoint RegalOrb { get; init; }
    public required CalibrationPoint RegalShard { get; init; }
    public required CalibrationPoint OrbofAlchemy { get; init; }
    public required CalibrationPoint AlchemyShard { get; init; }
    public required CalibrationPoint ChaosOrb { get; init; }
    public required CalibrationPoint ChaosShard { get; init; }
    public required CalibrationPoint BlessedOrb { get; init; }
    public required CalibrationPoint DivineOrb { get; init; }
    public required CalibrationPoint OrbofScouring { get; init; }
    public required CalibrationPoint SacredOrb { get; init; }
    public required CalibrationPoint DivinationCards { get; init; }
    public required CalibrationPoint VaalOrb { get; init; }
    public required CalibrationPoint OrbofRegret { get; init; }
    public required CalibrationPoint OrbofUnmaking { get; init; }
    public required CalibrationPoint VeiledExaltedOrb { get; init; }
    public required CalibrationPoint VeiledChaosOrb { get; init; }
    public required CalibrationPoint EquipSlot { get; init; }
    public required CalibrationPoint DefaultUIButton { get; init; }
    public required CalibrationPoint OtherWorldUIButton { get; init; }
    public required CalibrationPoint SeasonUIButton { get; init; }

    public static CoordinateConfig FromOptions(CraftOptions options) => new()
    {
        SchemaVersion = 1,
        Units = "percent",
        InventoryOrigin = CalibrationPoint.From(options.InventoryOrigin),
        InventoryStep = CalibrationPoint.From(options.InventoryStep),
        ScrollFragment = CalibrationPoint.From(options.ScrollFragment),
        ScrollofWisdom = CalibrationPoint.From(options.ScrollofWisdom),
        PortalScroll = CalibrationPoint.From(options.PortalScroll),
        OrbofTransmutation = CalibrationPoint.From(options.OrbofTransmutation),
        TransmutationShard = CalibrationPoint.From(options.TransmutationShard),
        OrbofAlteration = CalibrationPoint.From(options.OrbofAlteration),
        AlterationShard = CalibrationPoint.From(options.AlterationShard),
        OrbofAnnulment = CalibrationPoint.From(options.OrbofAnnulment),
        AnnulmentShard = CalibrationPoint.From(options.AnnulmentShard),
        OrbofChance = CalibrationPoint.From(options.OrbofChance),
        OrbofAugmentation = CalibrationPoint.From(options.OrbofAugmentation),
        FracturingOrb = CalibrationPoint.From(options.FracturingOrb),
        FracturingShard = CalibrationPoint.From(options.FracturingShard),
        JewellersOrb = CalibrationPoint.From(options.JewellersOrb),
        AncientOrb = CalibrationPoint.From(options.AncientOrb),
        OrbofFusing = CalibrationPoint.From(options.OrbofFusing),
        OrbofBinding = CalibrationPoint.From(options.OrbofBinding),
        ChromaticOrb = CalibrationPoint.From(options.ChromaticOrb),
        HinekorasLock = CalibrationPoint.From(options.HinekorasLock),
        EnkindlingOrb = CalibrationPoint.From(options.EnkindlingOrb),
        InstillingOrb = CalibrationPoint.From(options.InstillingOrb),
        ExaltedOrb = CalibrationPoint.From(options.ExaltedOrb),
        ExaltedShard = CalibrationPoint.From(options.ExaltedShard),
        MirrorofKalandra = CalibrationPoint.From(options.MirrorofKalandra),
        MirrorShard = CalibrationPoint.From(options.MirrorShard),
        BlacksmithsWhetstone = CalibrationPoint.From(options.BlacksmithsWhetstone),
        ArmourersScrap = CalibrationPoint.From(options.ArmourersScrap),
        GlassblowersBauble = CalibrationPoint.From(options.GlassblowersBauble),
        GemcuttersPrism = CalibrationPoint.From(options.GemcuttersPrism),
        RegalOrb = CalibrationPoint.From(options.RegalOrb),
        RegalShard = CalibrationPoint.From(options.RegalShard),
        OrbofAlchemy = CalibrationPoint.From(options.OrbofAlchemy),
        AlchemyShard = CalibrationPoint.From(options.AlchemyShard),
        ChaosOrb = CalibrationPoint.From(options.ChaosOrb),
        ChaosShard = CalibrationPoint.From(options.ChaosShard),
        BlessedOrb = CalibrationPoint.From(options.BlessedOrb),
        DivineOrb = CalibrationPoint.From(options.DivineOrb),
        OrbofScouring = CalibrationPoint.From(options.OrbofScouring),
        SacredOrb = CalibrationPoint.From(options.SacredOrb),
        DivinationCards = CalibrationPoint.From(options.DivinationCards),
        VaalOrb = CalibrationPoint.From(options.VaalOrb),
        OrbofRegret = CalibrationPoint.From(options.OrbofRegret),
        OrbofUnmaking = CalibrationPoint.From(options.OrbofUnmaking),
        VeiledExaltedOrb = CalibrationPoint.From(options.VeiledExaltedOrb),
        VeiledChaosOrb = CalibrationPoint.From(options.VeiledChaosOrb),
        EquipSlot = CalibrationPoint.From(options.EquipSlot),
        DefaultUIButton = CalibrationPoint.From(options.DefaultUIButton),
        OtherWorldUIButton = CalibrationPoint.From(options.OtherWorldUIButton),
        SeasonUIButton = CalibrationPoint.From(options.SeasonUIButton)
    };

    public CraftOptions ApplyTo(CraftOptions options)
    {
        Validate();
        return options with
        {
            InventoryOrigin = InventoryOrigin.ToRelative(),
            InventoryStep = InventoryStep.ToRelative(),
            ScrollFragment = ScrollFragment.ToRelative(),
            ScrollofWisdom = ScrollofWisdom.ToRelative(),
            PortalScroll = PortalScroll.ToRelative(),
            OrbofTransmutation = OrbofTransmutation.ToRelative(),
            TransmutationShard = TransmutationShard.ToRelative(),
            OrbofAlteration = OrbofAlteration.ToRelative(),
            AlterationShard = AlterationShard.ToRelative(),
            OrbofAnnulment = OrbofAnnulment.ToRelative(),
            AnnulmentShard = AnnulmentShard.ToRelative(),
            OrbofChance = OrbofChance.ToRelative(),
            OrbofAugmentation = OrbofAugmentation.ToRelative(),
            FracturingOrb = FracturingOrb.ToRelative(),
            FracturingShard = FracturingShard.ToRelative(),
            JewellersOrb = JewellersOrb.ToRelative(),
            AncientOrb = AncientOrb.ToRelative(),
            OrbofFusing = OrbofFusing.ToRelative(),
            OrbofBinding = OrbofBinding.ToRelative(),
            ChromaticOrb = ChromaticOrb.ToRelative(),
            HinekorasLock = HinekorasLock.ToRelative(),
            EnkindlingOrb = EnkindlingOrb.ToRelative(),
            InstillingOrb = InstillingOrb.ToRelative(),
            ExaltedOrb = ExaltedOrb.ToRelative(),
            ExaltedShard = ExaltedShard.ToRelative(),
            MirrorofKalandra = MirrorofKalandra.ToRelative(),
            MirrorShard = MirrorShard.ToRelative(),
            BlacksmithsWhetstone = BlacksmithsWhetstone.ToRelative(),
            ArmourersScrap = ArmourersScrap.ToRelative(),
            GlassblowersBauble = GlassblowersBauble.ToRelative(),
            GemcuttersPrism = GemcuttersPrism.ToRelative(),
            RegalOrb = RegalOrb.ToRelative(),
            RegalShard = RegalShard.ToRelative(),
            OrbofAlchemy = OrbofAlchemy.ToRelative(),
            AlchemyShard = AlchemyShard.ToRelative(),
            ChaosOrb = ChaosOrb.ToRelative(),
            ChaosShard = ChaosShard.ToRelative(),
            BlessedOrb = BlessedOrb.ToRelative(),
            DivineOrb = DivineOrb.ToRelative(),
            OrbofScouring = OrbofScouring.ToRelative(),
            SacredOrb = SacredOrb.ToRelative(),
            DivinationCards = DivinationCards.ToRelative(),
            VaalOrb = VaalOrb.ToRelative(),
            OrbofRegret = OrbofRegret.ToRelative(),
            OrbofUnmaking = OrbofUnmaking.ToRelative(),
            VeiledExaltedOrb = VeiledExaltedOrb.ToRelative(),
            VeiledChaosOrb = VeiledChaosOrb.ToRelative(),
            EquipSlot = EquipSlot.ToRelative(),
            DefaultUIButton = DefaultUIButton.ToRelative(),
            OtherWorldUIButton = OtherWorldUIButton.ToRelative(),
            SeasonUIButton = SeasonUIButton.ToRelative()
        };
    }

    public void Validate()
    {
        if (SchemaVersion != 1) throw new ArgumentException($"不支持的坐标配置版本：{SchemaVersion}，当前支持 1。");
        if (Units != "percent") throw new ArgumentException("坐标配置 units 必须为 percent（百分比）。");
        CalibrationPoint?[] points = [ScrollFragment, ScrollofWisdom, PortalScroll, OrbofTransmutation, TransmutationShard, OrbofAlteration, AlterationShard, OrbofAnnulment, AnnulmentShard, OrbofChance, OrbofAugmentation, FracturingOrb, FracturingShard, JewellersOrb, AncientOrb, OrbofFusing, OrbofBinding, ChromaticOrb, HinekorasLock, EnkindlingOrb, InstillingOrb, ExaltedOrb, ExaltedShard, MirrorofKalandra, MirrorShard, BlacksmithsWhetstone, ArmourersScrap, GlassblowersBauble, GemcuttersPrism, RegalOrb, RegalShard, OrbofAlchemy, AlchemyShard, ChaosOrb, ChaosShard, BlessedOrb, DivineOrb, OrbofScouring, SacredOrb, DivinationCards, VaalOrb, OrbofRegret, OrbofUnmaking, VeiledExaltedOrb, VeiledChaosOrb, EquipSlot, DefaultUIButton, OtherWorldUIButton, SeasonUIButton, InventoryOrigin, InventoryStep];
        if (points.Any(point => point == null || !point.ToRelative().IsValid))
            throw new ArgumentException("所有坐标必须完整，x、y 必须是大于 0 且小于 100 的有限数字。");
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
        AllowTrailingCommas = true,
        Converters = { new TwoDecimalFloatConverter() }
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

// Keep percentage coordinates numeric while writing exactly two decimal places.
internal sealed class TwoDecimalFloatConverter : System.Text.Json.Serialization.JsonConverter<float>
{
    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetSingle();

    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        => writer.WriteRawValue(value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
}
