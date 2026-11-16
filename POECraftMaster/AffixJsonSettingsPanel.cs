using ImGuiNET;
using POECraftMaster.Crafting;

namespace POECraftMaster;

internal sealed class AffixJsonSettingsPanel(CraftProfileStore store)
{
    private string _profilePath = store.FilePath;
    private string _profileJsonStatus = "";

    public void Render()
    {
        if (ImGui.CollapsingHeader("词缀 JSON 配置（改造 + 增幅）"))
        {
            ImGui.TextWrapped($"本项目配置：{store.FilePath}");
            ImGui.InputText("JSON 路径", ref _profilePath, 2048);
            if (ImGui.Button("导入词缀 JSON")) RunUiAction(() =>
            {
                store.ImportJson(_profilePath);
                _profileJsonStatus = store.Status;
            });
            ImGui.SameLine();
            if (ImGui.Button("导出词缀 JSON")) RunUiAction(() =>
            {
                store.ExportJson(_profilePath);
                _profileJsonStatus = store.Status;
            });
            ImGui.TextWrapped("同名导入保留当前方案；保存当前方案可更新同名方案。坐标由独立配置管理。");
            if (_profileJsonStatus.Length > 0)
                ImGui.TextWrapped(_profileJsonStatus);
        }
    }

    private void RunUiAction(Action action)
    {
        try { action(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException
            or InvalidOperationException or System.Text.Json.JsonException or NotSupportedException)
        {
            _profileJsonStatus = e.Message;
        }
    }
}
