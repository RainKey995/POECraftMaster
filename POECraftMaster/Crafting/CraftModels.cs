using System.Drawing;

namespace POECraftMaster.Crafting;

public enum CraftMode { Alteration, ScouringAndChance }

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
    public RelativePoint Alteration { get; init; } = new(0.05859375f, 0.28842592f);
    public RelativePoint Augmentation { get; init; } = new(0.117187f, 0.3425925f);
    public RelativePoint Scouring { get; init; } = new(0.2270833f, 0.510648f);
    public RelativePoint Chance { get; init; } = new(0.116927f, 0.289351f);
    public RelativePoint InventoryOrigin { get; init; } = new(0.676041667f, 0.568518519f);
    public RelativePoint InventoryStep { get; init; } = new(0.02734375f, 0.048611111f);

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
        if (!Alteration.IsValid || !Augmentation.IsValid || !Scouring.IsValid || !Chance.IsValid
            || !InventoryOrigin.IsValid || !InventoryStep.IsValid
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
