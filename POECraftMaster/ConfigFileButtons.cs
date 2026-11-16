using System.Text.Json;
using ImGuiNET;

namespace POECraftMaster;

internal sealed class ConfigFileButtons(Func<bool, string, Task<string?>> chooseFile)
{
    private Task<string?>? _dialog;
    private bool _saveAs;
    public bool IsOpen => _dialog is { IsCompleted: false };
    public string Status { get; private set; } = "";

    public void Complete(Action<string> load, Action<string> save)
    {
        if (_dialog is not { IsCompleted: true } dialog)
            return;
        _dialog = null;
        try
        {
            if (dialog.GetAwaiter().GetResult() is not { } path)
                return;
            if (_saveAs)
                save(path);
            else
                load(path);
            Status = _saveAs ? "配置已另存并切换。" : "配置已加载。";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException
            or InvalidOperationException or JsonException or NotSupportedException or InvalidDataException)
        {
            Status = $"配置操作失败：{e.Message}";
        }
    }

    public void Render(string path, bool canSave = true)
    {
        ImGui.TextWrapped(path);
        if (ImGui.Button("选择配置文件"))
        {
            _saveAs = false;
            _dialog = chooseFile(false, path);
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(!canSave);
        if (ImGui.Button("另存为"))
        {
            _saveAs = true;
            _dialog = chooseFile(true, path);
        }
        ImGui.EndDisabled();
    }
}
