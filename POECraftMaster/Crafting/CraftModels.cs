using System.Drawing;

namespace POECraftMaster.Crafting;

public enum CraftMode
{
    Alteration, // 改造 + 增幅
    ScouringAndChance // 重铸 + 机会石
}

public readonly record struct RelativePoint(float X, float Y)
{
    public bool IsValid => float.IsFinite(X) && float.IsFinite(Y) && X > 0 && X < 1 && Y > 0 && Y < 1;
    public Point ToScreen(Rectangle bounds) => new(bounds.Left + (int)(bounds.Width * X),
        bounds.Top + (int)(bounds.Height * Y));
}

public sealed record CraftOptions
{
    public CraftMode Mode { get; init; }
    public string Affixes { get; init; } = "";
    public int RequiredMatches { get; init; } = 1;
    public int IntervalMs { get; init; } = 100;
    public bool UseAugmentation { get; init; }
    public int MaxAttempts { get; init; } = 1000;
    public int[] SelectedSlots { get; init; } = [];
    // Inventory positions use the game client; currency positions use the user-selected stash region.
    public RelativePoint InventoryOrigin { get; init; }
    public RelativePoint InventoryStep { get; init; }
    public RelativePoint ScrollFragment { get; init; }
    public RelativePoint ScrollofWisdom { get; init; }
    public RelativePoint PortalScroll { get; init; }
    public RelativePoint OrbofTransmutation { get; init; }
    public RelativePoint TransmutationShard { get; init; }
    public RelativePoint OrbofAlteration { get; init; }
    public RelativePoint AlterationShard { get; init; }
    public RelativePoint OrbofAnnulment { get; init; }
    public RelativePoint AnnulmentShard { get; init; }
    public RelativePoint OrbofChance { get; init; }
    public RelativePoint OrbofAugmentation { get; init; }
    public RelativePoint FracturingOrb { get; init; }
    public RelativePoint FracturingShard { get; init; }
    public RelativePoint JewellersOrb { get; init; }
    public RelativePoint AncientOrb { get; init; }
    public RelativePoint OrbofFusing { get; init; }
    public RelativePoint OrbofBinding { get; init; }
    public RelativePoint ChromaticOrb { get; init; }
    public RelativePoint HinekorasLock { get; init; }
    public RelativePoint EnkindlingOrb { get; init; }
    public RelativePoint InstillingOrb { get; init; }
    public RelativePoint ExaltedOrb { get; init; }
    public RelativePoint ExaltedShard { get; init; }
    public RelativePoint MirrorofKalandra { get; init; }
    public RelativePoint MirrorShard { get; init; }
    public RelativePoint BlacksmithsWhetstone { get; init; }
    public RelativePoint ArmourersScrap { get; init; }
    public RelativePoint GlassblowersBauble { get; init; }
    public RelativePoint GemcuttersPrism { get; init; }
    public RelativePoint RegalOrb { get; init; }
    public RelativePoint RegalShard { get; init; }
    public RelativePoint OrbofAlchemy { get; init; }
    public RelativePoint AlchemyShard { get; init; }
    public RelativePoint ChaosOrb { get; init; }
    public RelativePoint ChaosShard { get; init; }
    public RelativePoint BlessedOrb { get; init; }
    public RelativePoint DivineOrb { get; init; }
    public RelativePoint OrbofScouring { get; init; }
    public RelativePoint SacredOrb { get; init; }
    public RelativePoint DivinationCards { get; init; }
    public RelativePoint VaalOrb { get; init; }
    public RelativePoint OrbofRegret { get; init; }
    public RelativePoint OrbofUnmaking { get; init; }
    public RelativePoint VeiledExaltedOrb { get; init; }
    public RelativePoint VeiledChaosOrb { get; init; }
    public RelativePoint EquipSlot { get; init; }
    public RelativePoint DefaultUIButton { get; init; }
    public RelativePoint OtherWorldUIButton { get; init; }
    public RelativePoint SeasonUIButton { get; init; }

    // Legacy inventory indices are column-major: column * 5 + row.
    public RelativePoint SlotPoint(int index) => new(InventoryOrigin.X + index / 5 * InventoryStep.X,
        InventoryOrigin.Y + index % 5 * InventoryStep.Y);

    public string? Validate()
    {
        if (!Enum.IsDefined(Mode)) return "无效的制作模式。";
        if (SelectedSlots is null || SelectedSlots.Length == 0
            || SelectedSlots.Any(slot => slot is < 0 or >= 60)) return "请先选择有效的背包格子。";
        if (IntervalMs is < 50 or > 2000) return "操作间隔应为 50～2000 毫秒。";
        if (MaxAttempts is < 1 or > 100000) return "次数上限应为 1～100000。";
        if (!InventoryOrigin.IsValid || !InventoryStep.IsValid || !ScrollFragment.IsValid
            || !ScrollofWisdom.IsValid || !PortalScroll.IsValid || !OrbofTransmutation.IsValid
            || !TransmutationShard.IsValid || !OrbofAlteration.IsValid || !AlterationShard.IsValid
            || !OrbofAnnulment.IsValid || !AnnulmentShard.IsValid || !OrbofChance.IsValid
            || !OrbofAugmentation.IsValid || !FracturingOrb.IsValid || !FracturingShard.IsValid
            || !JewellersOrb.IsValid || !AncientOrb.IsValid || !OrbofFusing.IsValid
            || !OrbofBinding.IsValid || !ChromaticOrb.IsValid || !HinekorasLock.IsValid
            || !EnkindlingOrb.IsValid || !InstillingOrb.IsValid || !ExaltedOrb.IsValid
            || !ExaltedShard.IsValid || !MirrorofKalandra.IsValid || !MirrorShard.IsValid
            || !BlacksmithsWhetstone.IsValid || !ArmourersScrap.IsValid || !GlassblowersBauble.IsValid
            || !GemcuttersPrism.IsValid || !RegalOrb.IsValid || !RegalShard.IsValid
            || !OrbofAlchemy.IsValid || !AlchemyShard.IsValid || !ChaosOrb.IsValid
            || !ChaosShard.IsValid || !BlessedOrb.IsValid || !DivineOrb.IsValid
            || !OrbofScouring.IsValid || !SacredOrb.IsValid || !DivinationCards.IsValid
            || !VaalOrb.IsValid || !OrbofRegret.IsValid || !OrbofUnmaking.IsValid
            || !VeiledExaltedOrb.IsValid || !VeiledChaosOrb.IsValid || !EquipSlot.IsValid
            || !DefaultUIButton.IsValid || !OtherWorldUIButton.IsValid || !SeasonUIButton.IsValid
            || SelectedSlots.Any(slot => !SlotPoint(slot).IsValid)) return "坐标必须在游戏客户区内部。";
        if (Mode == CraftMode.Alteration && (RequiredMatches is < 1 or > 2
            || AffixMatcher.Rules(Affixes).Length < RequiredMatches)) return "有效词缀条数不能少于匹配数量（1～2）。";
        return null;
    }
}

public static class AffixMatcher
{
    public static string[] Rules(string? text) => (text ?? "").Split(['\r', '\n'],
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.Ordinal).ToArray();

    // Retain the old full-word behavior as an ordinal substring match, fixing its overrun bug.
    public static int Count(string text, string[] rules) => rules.Count(rule =>
        text.Contains(rule, StringComparison.Ordinal));

    public static bool Sparse(string text, string search)
    {
        int index = 0;
        foreach (char character in text)
            if (index < search.Length && character == search[index]) index++;
        return index == search.Length;
    }
}

public static class ItemRarity
{
    public static bool IsMagic(string text)
    {
        foreach (string line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string header = string.Concat(line.Where(character => !char.IsWhiteSpace(character)))
                .Replace('：', ':');
            int separator = header.IndexOf(':');
            if (separator < 0) continue;
            string name = header[..separator];
            if (name != "稀有度" && !name.Equals("Rarity", StringComparison.OrdinalIgnoreCase)) continue;
            string value = header[(separator + 1)..];
            return value == "魔法" || value.Equals("Magic", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}

public sealed record CraftProgress(bool Running = false, string Message = "就绪", int Completed = 0,
    int Total = 0, int Attempts = 0, int Slot = -1, string ItemText = "")
{
    public int[] RejectedSlots { get; init; } = [];
}
