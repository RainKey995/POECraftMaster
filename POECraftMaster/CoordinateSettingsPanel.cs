using ImGuiNET;

namespace POECraftMaster;

internal sealed class CoordinateSettingsPanel(Func<bool, string, Task<string?>> chooseFile)
{
    private readonly ConfigurationManager _config = ConfigurationManager.Instance;
    private readonly ConfigFileButtons _files = new(chooseFile);

    public bool FileDialogOpen => _files.IsOpen;

    public void Render()
    {
        _files.Complete(_config.SelectCoordinates, _config.SaveCoordinatesAs);
        if (!ImGui.CollapsingHeader("坐标配置（百分比）"))
            return;

        ImGui.PushID(nameof(CoordinateSettingsPanel));
        _files.Render(_config.ActiveCoordinatesPath, _config.Coordinates != null);
        ImGui.PopID();
        ImGui.TextWrapped("通货坐标相对于本次拖动框选的仓库区域；背包起点和步长相对于整个游戏客户区。");
        if (_config.CoordinatesError is { } error)
            ImGui.TextWrapped($"坐标配置加载失败：{error}");
        if (_files.Status.Length > 0)
            ImGui.TextWrapped(_files.Status);
    }
}
