using System.Text.Json;
using ImGuiNET;

namespace POECraftMaster;

internal sealed class AccountSettingsPanel
{
    private readonly Func<bool, string, Task<string?>> _chooseFile;
    private readonly ConfigurationManager _config = ConfigurationManager.Instance;
    private string _path = ConfigurationManager.Instance.AccountPath;
    private string _accountName = "";
    private string _cookie = "";
    private string _status = "";
    private bool _hasConfig;
    private bool _saveAs;
    private Task<string?>? _fileDialog;

    public AccountSettingsPanel(Func<bool, string, Task<string?>> chooseFile)
    {
        _chooseFile = chooseFile;
        SetAccount(_path, _config.DefaultAccount);
        if (_config.AccountError != null)
            _status = "无法读取或创建默认账号配置，请检查文件格式和权限。";
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

        if (!ImGui.CollapsingHeader("账号配置"))
            return;
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

    private void Load(string path) => SetAccount(path, _config.ReadAccount(path));

    private void SetAccount(string path, AccountConfig? config)
    {
        _path = path;
        _hasConfig = config != null;
        _accountName = config?.AccountName ?? "";
        _cookie = config?.Cookie ?? "";
        _status = _hasConfig ? "" : "配置文件为空，可通过「另存为」创建账号配置。";
    }

    private void Save(string path)
    {
        _config.SaveAccount(path, new AccountConfig(_accountName, _cookie));
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
