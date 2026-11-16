using System.Text.Json;
using ImGuiNET;

namespace POECraftMaster;

internal sealed class AccountSettingsPanel
{
    private readonly Func<bool, string, Task<string?>> _chooseFile;
    private string _path = AccountConfig.DefaultPath;
    private string _accountName = "";
    private string _cookie = "";
    private string _status = "";
    private bool _hasConfig;
    private bool _saveAs;
    private Task<string?>? _fileDialog;

    public AccountSettingsPanel(Func<bool, string, Task<string?>> chooseFile)
    {
        _chooseFile = chooseFile;
        RunFileAction(() =>
        {
            if (!File.Exists(_path))
                new AccountConfig().Save(_path, overwrite: false);
            Load(_path);
        });
    }

    public bool FileDialogOpen => _fileDialog is { IsCompleted: false };

    public void Render()
    {
        if (_fileDialog?.IsCompleted == true)
        {
            var dialog = _fileDialog;
            _fileDialog = null;
            RunFileAction(() =>
            {
                string? path = dialog.GetAwaiter().GetResult();
                if (path == null)
                    return;
                if (_saveAs)
                    Save(path);
                else
                    Load(path);
            });
        }

        ImGui.TextUnformatted("账号配置");
        ImGui.TextWrapped(_path);
        if (ImGui.Button("选择配置文件"))
            OpenDialog(false);
        ImGui.SameLine();
        if (ImGui.Button("另存为"))
            OpenDialog(true);

        if (_hasConfig)
        {
            ImGui.InputText("AccountName", ref _accountName, 1024);
            ImGui.InputText("Cookie", ref _cookie, 65536, ImGuiInputTextFlags.Password);
            if (ImGui.Button("保存账号配置"))
                RunFileAction(() => Save(_path));
        }
        if (_status.Length > 0)
            ImGui.TextWrapped(_status);
    }

    private void OpenDialog(bool saveAs)
    {
        _saveAs = saveAs;
        _fileDialog = _chooseFile(saveAs, _path);
    }

    private void Load(string path)
    {
        var config = AccountConfig.Load(path);
        _path = path;
        _hasConfig = config != null;
        _accountName = config?.AccountName ?? "";
        _cookie = config?.Cookie ?? "";
        _status = _hasConfig ? "" : "配置文件为空，可通过「另存为」创建账号配置。";
    }

    private void Save(string path)
    {
        new AccountConfig(_accountName, _cookie).Save(path);
        _path = path;
        _hasConfig = true;
        _status = "账号配置已保存。";
    }

    private void RunFileAction(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            _status = "无法读取账号配置，文件必须是有效的 JSON 对象。";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _status = "无法读取或保存账号配置，请检查文件路径和权限。";
        }
    }
}
