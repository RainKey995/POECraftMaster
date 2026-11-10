using System.Numerics;
using ImGuiNET;
using POECraftMaster.Tools;

namespace POECraftMaster.Crafting;

internal sealed class CraftPanel
{
    private readonly CraftProfileStore _store = new();
    private readonly CalculatorPanel _calculator = new();
    private readonly bool[] _slots = new bool[60];
    private string _affixes = "";
    private string _name = "";
    private string _filter = "";
    private string _selectedProfile = "";
    private string _message = "";
    private string _profilePath = CraftProfileStore.DefaultPath;
    private CraftMode _mode;
    private int _matches = 1, _interval = 100, _attempts = 1000;
    private bool _augmentation;
    private CraftOptions? _coordinates;
    private IntPtr _grid, _selectedGrid, _alterationIcon, _augmentationIcon;

    public void Initialize(Func<string, IntPtr> loadImage)
    {
        _grid = loadImage("Sprites/Grid.png");
        _selectedGrid = loadImage("Sprites/GridSelected.png");
        _alterationIcon = loadImage("Currency/currencyrerollmagic.png");
        _augmentationIcon = loadImage("Currency/currencyaddmodtomagic.png");
        _message = _store.Status;
        RunUiAction(() =>
        {
            _coordinates = CoordinateConfigFile.Load(CoordinateConfigFile.DefaultPath).ApplyTo(new CraftOptions());
        });
    }

    public void Render(CraftRunner runner, Action<CraftOptions> start)
    {
        ImGui.Text("POE Craft Master");
        ImGui.SameLine();
        ImGui.TextDisabled("功能栏");
        ImGui.Separator();
        if (ImGui.BeginTabBar("craft modes"))
        {
            if (ImGui.BeginTabItem("改造 + 增幅"))
            {
                _mode = CraftMode.Alteration;
                DrawEditor(runner, start);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("重铸 + 机会"))
            {
                _mode = CraftMode.ScouringAndChance;
                DrawEditor(runner, start);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("计算器"))
            {
                _calculator.Render();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawEditor(CraftRunner runner, Action<CraftOptions> start)
    {
        var progress = runner.Progress;
        var rejectedSlots = !progress.Running && _mode == CraftMode.Alteration
            ? progress.RejectedSlots : [];
        bool columns = ImGui.GetContentRegionAvail().X >= 900;
        bool table = ImGui.BeginTable("craft layout", columns ? 2 : 1, ImGuiTableFlags.Resizable);
        if (!table) return;
        ImGui.TableNextColumn();
        ImGui.BeginChild("craft inputs", new Vector2(0, columns ? 0 : 680));
        if (_mode == CraftMode.Alteration)
        {
            ImGui.Image(_alterationIcon, new Vector2(32), Vector2.Zero, Vector2.One,
                new Vector4(1, 1, 1, ImGui.GetStyle().Alpha), Vector4.Zero);
            ImGui.SameLine();
            ImGui.Image(_augmentationIcon, new Vector2(32), Vector2.Zero, Vector2.One,
                new Vector4(1, 1, 1, ImGui.GetStyle().Alpha), Vector4.Zero);
            ImGui.Text("目标词缀（每行一条，按包含文本匹配）");
            ImGui.InputTextMultiline("##affixes", ref _affixes, 16384,
                new Vector2(-1, 140));
            ImGui.SliderInt("匹配条数", ref _matches, 1, 2);
            ImGui.Checkbox("每次改造后使用增幅石", ref _augmentation);
        }
        else
        {
            ImGui.TextWrapped("按选中的背包格子依次重铸、机会。读取到传奇装备时跳过，达到每格次数后进入下一格。");
        }
        ImGui.SliderInt("操作间隔（毫秒）", ref _interval, 50, 2000);
        ImGui.InputInt("每格次数上限", ref _attempts);
        ImGui.Text("背包选择：12 列 × 5 行");
        float cell = Math.Clamp((ImGui.GetContentRegionAvail().X - 11 * 3) / 12, 18, 44);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(3));
        for (int row = 0; row < 5; row++)
        {
            for (int column = 0; column < 12; column++)
            {
                if (column != 0) ImGui.SameLine();
                int slot = column * 5 + row;
                var position = ImGui.GetCursorScreenPos();
                if (ImGui.InvisibleButton($"slot{slot}", new Vector2(cell))) _slots[slot] = !_slots[slot];
                ImGui.GetWindowDrawList().AddImage(_slots[slot] ? _selectedGrid : _grid,
                    position, position + new Vector2(cell), Vector2.Zero, Vector2.One,
                    ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));
                bool rejected = rejectedSlots.Contains(slot);
                if (rejected)
                {
                    ImGui.GetWindowDrawList().AddRectFilled(position, position + new Vector2(cell),
                        ImGui.GetColorU32(new Vector4(1f, 0.05f, 0.05f, 0.4f)));
                    ImGui.GetWindowDrawList().AddRect(position + Vector2.One,
                        position + new Vector2(cell - 1), ImGui.GetColorU32(new Vector4(1f, 0.15f, 0.15f, 1f)),
                        0f, ImDrawFlags.None, 2f);
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip($"第 {column + 1} 列，第 {row + 1} 行（编号 {slot + 1}）"
                    + (rejected ? "\n稀有度不是魔法，已跳过制作。" : ""));
            }
        }
        ImGui.PopStyleVar();
        if (ImGui.Button("全选")) Array.Fill(_slots, true);
        ImGui.SameLine();
        if (ImGui.Button("清空")) Array.Fill(_slots, false);
        ImGui.SameLine();
        ImGui.Text($"已选 {_slots.Count(selected => selected)} 格");
        if (rejectedSlots.Length > 0)
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f),
                $"红色格子：非魔法物品，已跳过 {rejectedSlots.Length} 格。重新开始任务后重新检测。");
        ImGui.Separator();
        ImGui.TextWrapped("开始前请打开游戏背包和通货仓库。每次开始后，按住鼠标左键拖动框选完整仓库区域，松开后制作；Esc / F12 / 右键取消选择。制作中请勿移动仓库。F12、托盘“停止制作”或显示快捷键可停止；切出游戏也会停止。");
        if (ImGui.Button("开始制作"))
        {
            RunUiAction(() =>
            {
                var options = Options();
                string? error = options.Validate();
                if (error != null) throw new ArgumentException(error);
                start(options);
            });
        }
        ImGui.EndChild();

        ImGui.TableNextColumn();
        // With a single-column table this starts the next row; keep it scrollable on small windows.
        ImGui.BeginChild("craft profiles", new Vector2(0, columns ? 0 : 500));
        ImGui.Text("词缀方案");
        ImGui.InputText("筛选", ref _filter, 128);
        if (ImGui.BeginCombo("方案", _selectedProfile))
        {
            foreach (string name in _store.Profiles.Keys.Order())
                if (AffixMatcher.Sparse(name, _filter) && ImGui.Selectable(name, name == _selectedProfile))
                    _selectedProfile = name;
            ImGui.EndCombo();
        }
        if (ImGui.Button("应用方案")) RunUiAction(() => ApplySelected());
        ImGui.SameLine();
        if (ImGui.Button("删除方案")) ImGui.OpenPopup("删除方案确认");
        bool confirmOpen = true;
        if (ImGui.BeginPopupModal("删除方案确认", ref confirmOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text($"删除方案：{_selectedProfile}？");
            if (ImGui.Button("确认删除"))
            {
                RunUiAction(() => { _store.Delete(_selectedProfile); _selectedProfile = ""; });
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("取消")) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
        ImGui.InputText("方案名称", ref _name, 256);
        if (ImGui.Button("保存当前方案")) RunUiAction(() =>
        {
            _store.Save(_name, WithEditor(new CraftOptions()));
            _selectedProfile = _name.Trim();
            _message = "方案已保存（同名方案更新）。";
        });
        if (ImGui.CollapsingHeader("词缀 JSON 配置"))
        {
            ImGui.TextWrapped($"本项目配置：{_store.FilePath}");
            ImGui.InputText("JSON 路径", ref _profilePath, 2048);
            if (ImGui.Button("导入词缀 JSON")) RunUiAction(() =>
            {
                _store.ImportJson(_profilePath);
                _message = _store.Status;
            });
            ImGui.SameLine();
            if (ImGui.Button("导出词缀 JSON")) RunUiAction(() =>
            {
                _store.ExportJson(_profilePath);
                _message = _store.Status;
            });
            ImGui.TextWrapped("同名导入保留当前方案；保存当前方案可更新同名方案。坐标由独立配置管理。");
        }
        if (ImGui.CollapsingHeader("坐标配置（百分比）"))
        {
            ImGui.TextWrapped($"配置文件：{CoordinateConfigFile.DefaultPath}");
            ImGui.TextWrapped("坐标仅从配置文件读取。修改 JSON 后请重启应用，坐标单位为 0～100 的百分比。");
            ImGui.TextWrapped("通货坐标相对于本次拖动框选的仓库区域；背包起点和步长相对于整个游戏客户区。");
            ImGui.TextWrapped(_coordinates == null ? "坐标配置加载失败，无法开始制作。请检查配置文件后重启应用。" : "坐标配置已加载。");
        }
        ImGui.Separator();
        ImGui.TextWrapped(progress.Message);
        ImGui.Text($"完成 {progress.Completed}/{progress.Total}，已执行 {progress.Attempts} 轮");
        ImGui.TextWrapped(_message);
        if (!string.IsNullOrEmpty(progress.ItemText) && ImGui.CollapsingHeader("最近读取的装备"))
            ImGui.TextWrapped(progress.ItemText);
        ImGui.EndChild();
        ImGui.EndTable();
    }

    private CraftOptions Options() => WithEditor(_coordinates
        ?? throw new InvalidOperationException("坐标配置未加载，请检查 coordinates.json 后重启应用。"));

    private CraftOptions WithEditor(CraftOptions options) => options with
    {
        Mode = _mode,
        Affixes = _affixes,
        RequiredMatches = _matches,
        IntervalMs = _interval,
        MaxAttempts = _attempts,
        UseAugmentation = _augmentation,
        SelectedSlots = Enumerable.Range(0, 60).Where(slot => _slots[slot]).ToArray()
    };

    private void ApplySelected()
    {
        if (!_store.Profiles.TryGetValue(_selectedProfile, out var options))
            throw new ArgumentException("请先选择方案。");
        _affixes = options.Affixes ?? "";
        _matches = options.RequiredMatches;
        _interval = options.IntervalMs;
        _attempts = options.MaxAttempts;
        _augmentation = options.UseAugmentation;
        _name = _selectedProfile;
        Array.Fill(_slots, false);
        foreach (int slot in options.SelectedSlots ?? []) if (slot is >= 0 and < 60) _slots[slot] = true;
        _message = "方案已应用。";
    }

    private void RunUiAction(Action action)
    {
        try { action(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException
            or InvalidOperationException or System.Text.Json.JsonException
            or NotSupportedException)
        { _message = e.Message; }
    }

}
