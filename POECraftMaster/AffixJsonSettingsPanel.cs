using ImGuiNET;
using POECraftMaster.Crafting;

namespace POECraftMaster;

internal sealed class AffixJsonSettingsPanel(CraftProfileStore store, Func<bool, string, Task<string?>> chooseFile)
{
    private readonly ConfigFileButtons _files = new(chooseFile);

    public bool FileDialogOpen => _files.IsOpen;

    public void Render()
    {
        _files.Complete(store.SelectFile, store.SaveAs);
        if (!ImGui.CollapsingHeader("词缀 JSON 配置（改造 + 增幅）"))
            return;

        ImGui.PushID(nameof(AffixJsonSettingsPanel));
        _files.Render(store.FilePath);
        ImGui.PopID();
        string status = _files.Status.Length > 0 ? _files.Status : store.Status;
        if (status.Length > 0)
            ImGui.TextWrapped(status);
    }
}
