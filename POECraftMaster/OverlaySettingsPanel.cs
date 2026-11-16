using ImGuiNET;
using POECraftMaster.Crafting;

namespace POECraftMaster;

internal sealed class OverlaySettingsPanel(
    HotkeySettings initial, string initialStatus, Func<HotkeySettings, Task<string>> save,
    Func<bool, string, Task<string?>> chooseFile, CraftProfileStore store)
{
    private readonly HotkeySettingsPanel _hotkey = new(initial, initialStatus, save);
    private readonly AccountSettingsPanel _account = new(chooseFile);
    private readonly CoordinateSettingsPanel _coordinates = new(chooseFile);
    private readonly AffixJsonSettingsPanel _affixes = new(store, chooseFile);

    public bool FileDialogOpen => _account.FileDialogOpen || _coordinates.FileDialogOpen || _affixes.FileDialogOpen;
    public bool IsRecording => _hotkey.IsRecording;

    public void CancelRecording() => _hotkey.CancelRecording();

    public void Render()
    {
        _hotkey.Render();
        ImGui.Separator();
        _account.Render();
        ImGui.Separator();
        _coordinates.Render();
        ImGui.Separator();
        _affixes.Render();
    }
}
