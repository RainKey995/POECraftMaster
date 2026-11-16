using ImGuiNET;
using POECraftMaster.Crafting;

namespace POECraftMaster;

internal sealed class CoordinateSettingsPanel
{
    private readonly ConfigurationManager _config = ConfigurationManager.Instance;

    public CraftOptions ApplyTo(CraftOptions options) => (_config.Coordinates
        ?? throw new InvalidOperationException("坐标配置未加载，请检查 coordinates.json 后重启应用。"))
        .ApplyTo(options);

    public void Render()
    {
        if (ImGui.CollapsingHeader("坐标配置（百分比）"))
        {
            ImGui.TextWrapped($"配置文件：{ConfigurationManager.CoordinatesPath}");
            ImGui.TextWrapped("坐标仅从配置文件读取。修改 JSON 后请重启应用，坐标单位为 0～100 的百分比。");
            ImGui.TextWrapped("通货坐标相对于本次拖动框选的仓库区域；背包起点和步长相对于整个游戏客户区。");
            ImGui.TextWrapped(_config.CoordinatesError == null ? "坐标配置已加载。" : $"坐标配置加载失败：{_config.CoordinatesError}");
        }
    }
}
