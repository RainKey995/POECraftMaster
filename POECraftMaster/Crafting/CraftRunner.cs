using System.Drawing;

namespace POECraftMaster.Crafting;

public sealed class CraftRunner : IDisposable
{
    private readonly object _gate = new();
    private CancellationTokenSource? _cancellation;
    private Thread? _thread;
    private CraftProgress _progress = new();
    public CraftProgress Progress => Volatile.Read(ref _progress);

    public void Start(CraftOptions options, IntPtr gameWindow, Rectangle bounds)
    {
        string? error = options.Validate();
        if (error != null) throw new ArgumentException(error);
        lock (_gate)
        {
            if (_thread?.IsAlive == true) throw new InvalidOperationException("制作任务正在运行。");
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            var token = _cancellation.Token;
            _progress = new CraftProgress(true, "1 秒后开始；F12 或托盘菜单停止", Total: options.SelectedSlots.Distinct().Count());
            _thread = new Thread(() =>
            {
                try
                {
                    Execute(options, new GameAutomation(gameWindow, bounds), token,
                        progress => Volatile.Write(ref _progress, progress));
                }
                catch (OperationCanceledException e)
                {
                    Volatile.Write(ref _progress, Progress with { Running = false,
                        Message = token.IsCancellationRequested ? "任务已停止" : e.Message });
                }
                catch (Exception e)
                {
                    Volatile.Write(ref _progress, Progress with { Running = false, Message = $"已停止：{e.Message}" });
                }
            }) { IsBackground = true, Name = "POE crafting" };
            // Clipboard access must run on an STA thread, independent of ImGui's render thread.
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }
    }

    public static void Execute(CraftOptions options, IGameAutomation game, CancellationToken token,
        Action<CraftProgress> report)
    {
        string? error = options.Validate();
        if (error != null) throw new ArgumentException(error);
        int[] slots = options.SelectedSlots.Distinct().Order().ToArray();
        string[] rules = AffixMatcher.Rules(options.Affixes);
        int completed = 0, attempts = 0;
        string lastText = "";
        game.Wait(1000, token);
        foreach (int slot in slots)
        {
            RelativePoint point = options.SlotPoint(slot);
            // Verify fresh item text before spending currency, and skip already matching items.
            string text = game.ReadItem(point, token);
            for (int attempt = 0; ; attempt++)
            {
                token.ThrowIfCancellationRequested();
                if (options.Mode == CraftMode.Alteration && AffixMatcher.Count(text, rules) >= options.RequiredMatches)
                    break;
                if (options.Mode == CraftMode.ScouringAndChance && attempt >= options.MaxAttempts) break;
                if (attempt >= options.MaxAttempts)
                    throw new InvalidOperationException($"格子 {slot + 1} 已达到次数上限，未匹配词缀。");
                if (options.Mode == CraftMode.ScouringAndChance)
                {
                    string rarity = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault(line => line.Replace(" ", "").StartsWith("Rarity:", StringComparison.OrdinalIgnoreCase)
                            || line.Replace(" ", "").StartsWith("稀有度", StringComparison.Ordinal)) ?? "";
                    if (rarity.Contains("Unique", StringComparison.OrdinalIgnoreCase)
                        || rarity.Contains("传奇", StringComparison.Ordinal) || rarity.Contains("傳奇", StringComparison.Ordinal)) break;
                }
                report(new(true, "制作中；F12 或托盘菜单停止", completed, slots.Length, attempts, slot, text));
                game.UseCurrency(options.Mode == CraftMode.Alteration ? options.Alteration : options.Scouring,
                    point, options.IntervalMs, token);
                if (options.Mode == CraftMode.ScouringAndChance || options.UseAugmentation)
                    game.UseCurrency(options.Mode == CraftMode.Alteration ? options.Augmentation : options.Chance,
                        point, options.IntervalMs, token);
                attempts++;
                text = game.ReadItem(point, token);
                report(new(true, "已读取装备信息", completed, slots.Length, attempts, slot, text));
            }
            completed++;
            lastText = text;
            report(new(true, "当前物品已完成", completed, slots.Length, attempts, slot, text));
        }
        report(new(false, "全部任务已完成", completed, slots.Length, attempts, ItemText: lastText));
    }

    public void Stop()
    {
        lock (_gate) _cancellation?.Cancel();
    }

    public void Dispose()
    {
        Stop();
        _thread?.Join();
        _cancellation?.Dispose();
    }
}
