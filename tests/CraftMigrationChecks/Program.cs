using System.Drawing;
using POECraftMaster.Crafting;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    checks++;
    Console.WriteLine("PASS: " + name);
}

Check(AffixMatcher.Count("ababc", ["abc", "bc"]) == 2, "overlapping substring matching without overrun");
Check(AffixMatcher.Count("短", ["更长的词缀"]) == 0, "longer rule does not overrun input");
Check(AffixMatcher.Rules("词缀\r\n\n词缀\n 第二条 ").SequenceEqual(["词缀", "第二条"]), "blank and duplicate rules ignored");
Check(AffixMatcher.Sparse("护身符词缀", "护词") && !AffixMatcher.Sparse("护身符", "词护"), "legacy sparse profile search");
var options = new CraftOptions { Affixes = "目标", SelectedSlots = [5, 0, 5], MaxAttempts = 2 };
Check(options.SlotPoint(5).Y == options.SlotPoint(0).Y
    && options.SlotPoint(5).X > options.SlotPoint(0).X, "legacy column-major inventory indexing");
var screen = new RelativePoint(.5f, .5f).ToScreen(new Rectangle(-1000, 100, 800, 600));
Check(screen == new Point(-600, 400), "client-relative coordinates on secondary monitor");
Check((options with { SelectedSlots = [60] }).Validate() != null, "invalid inventory index rejected");
Check((options with { RequiredMatches = 2 }).Validate() != null, "insufficient distinct affixes rejected");
Check((options with { Alteration = new(float.NaN, .5f) }).Validate() != null, "non-finite coordinates rejected");

var reports = new List<CraftProgress>();
var game = new FakeGame(["旧词缀", "目标", "目标"]);
CraftRunner.Execute(options with { UseAugmentation = true }, game, CancellationToken.None, reports.Add);
Check(game.CurrencyUses == 2 && game.Reads == 3 && reports[^1].Completed == 2,
    "sequential crafting, augmentation, duplicate slot elimination and pre-matched item skip");
Check(game.UsedItems[0].X == options.SlotPoint(0).X, "selected slots processed in legacy sorted order");
Check(!reports[^1].Running && reports[^1].Attempts == 1, "completion progress and attempt count");

game = new FakeGame(["旧", "旧", "旧"]);
bool limitStopped = false;
try { CraftRunner.Execute(options with { SelectedSlots = [0] }, game, CancellationToken.None, _ => { }); }
catch (InvalidOperationException) { limitStopped = true; }
Check(limitStopped && game.CurrencyUses == 2, "attempt cap prevents endless currency use");

using var cancellation = new CancellationTokenSource();
cancellation.Cancel();
game = new FakeGame(["旧"]);
bool cancelled = false;
try { CraftRunner.Execute(options, game, cancellation.Token, _ => { }); }
catch (OperationCanceledException) { cancelled = true; }
Check(cancelled && game.CurrencyUses == 0, "cancelled jobs send no game input");

using var midCancellation = new CancellationTokenSource();
game = new FakeGame(["旧", "旧"]);
cancelled = false;
try
{
    CraftRunner.Execute(options with { SelectedSlots = [0] }, game, midCancellation.Token,
        progress => { if (progress.Attempts == 1) midCancellation.Cancel(); });
}
catch (OperationCanceledException) { cancelled = true; }
Check(cancelled && game.CurrencyUses == 1, "cancellation between attempts prevents further spending");

game = new FakeGame([]);
bool copyFailed = false;
try { CraftRunner.Execute(options, game, CancellationToken.None, _ => { }); }
catch (InvalidOperationException) { copyFailed = true; }
Check(copyFailed && game.CurrencyUses == 0, "initial copy failure spends no currency");

game = new FakeGame(["Rarity: Unique\n--------\n传奇装备"]);
CraftRunner.Execute(options with { Mode = CraftMode.ScouringAndChance, SelectedSlots = [0] }, game,
    CancellationToken.None, _ => { });
Check(game.CurrencyUses == 0, "chance mode skips existing unique items");
game = new FakeGame(["旧", "旧", "旧"]);
CraftRunner.Execute(options with { Mode = CraftMode.ScouringAndChance, SelectedSlots = [0] }, game,
    CancellationToken.None, _ => { });
Check(game.CurrencyUses == 4, "bounded scouring and chance cycles");

string directory = Path.Combine(Path.GetTempPath(), "POECraftMaster-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
string path = Path.Combine(directory, "profiles.json");
var store = new CraftProfileStore(path);
store.Save("我的方案", options);
store = new CraftProfileStore(path);
Check(store.Profiles["我的方案"].SelectedSlots.SequenceEqual(options.SelectedSlots)
    && store.Profiles["我的方案"].Affixes == "目标", "profile options survive restart");
string legacy = Path.Combine(directory, "legacy.xml");
File.WriteAllText(legacy, "<Configuration><AlterationAndAugmentation><我的方案>不覆盖</我的方案><旧版方案>甲&#10;乙</旧版方案></AlterationAndAugmentation></Configuration>");
store.ImportLegacy(legacy);
Check(store.Profiles["我的方案"].Affixes == "目标" && store.Profiles["旧版方案"].Affixes == "甲\n乙",
    "legacy XML import preserves multiline rules and current profiles");
store.Delete("旧版方案");
Check(!new CraftProfileStore(path).Profiles.ContainsKey("旧版方案"), "profile deletion persists");
Console.WriteLine($"All {checks} checks passed. No native input was sent. Fixtures: {directory}");

sealed class FakeGame(IEnumerable<string> items) : IGameAutomation
{
    private readonly Queue<string> _items = new(items);
    public int CurrencyUses, Reads;
    public List<RelativePoint> UsedItems { get; } = [];
    public void Wait(int milliseconds, CancellationToken token) => token.ThrowIfCancellationRequested();
    public void UseCurrency(RelativePoint currency, RelativePoint item, int interval, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        CurrencyUses++;
        UsedItems.Add(item);
    }
    public string ReadItem(RelativePoint item, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Reads++;
        return _items.Dequeue();
    }
}
