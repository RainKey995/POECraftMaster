using System.Text.Json;
using System.Net.Http;
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
    private Task<IReadOnlyList<PoeCharacter>>? _characterRequest;
    private IReadOnlyList<PoeCharacter> _characters = [];
    private string? _selectedCharacter;

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
            RenderCharacters();
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
        _selectedCharacter = null;
        RefreshCharacters(config);
    }

    private void Save(string path)
    {
        var account = new AccountConfig(_accountName, _cookie);
        _config.SaveAccount(path, account);
        _path = path;
        _hasConfig = true;
        _status = "账号配置已保存。";
        RefreshCharacters(account);
    }

    private void RefreshCharacters(AccountConfig? account)
    {
        _characters = [];
        _characterRequest = account != null && !string.IsNullOrWhiteSpace(account.AccountName)
            && !string.IsNullOrWhiteSpace(account.Cookie)
            ? FetchCharactersAsync(account) : null;
    }

    private static async Task<IReadOnlyList<PoeCharacter>> FetchCharactersAsync(AccountConfig account)
    {
        try
        {
            var characters = await PoeApiClient.Instance.GetCharactersAsync(account).ConfigureAwait(false);
            return characters.Where(character => character != null && !string.IsNullOrWhiteSpace(character.Name)).ToArray();
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException
            or InvalidDataException or OperationCanceledException or ArgumentException)
        {
            return [];
        }
    }

    private void RenderCharacters()
    {
        if (_characterRequest is { IsCompleted: true })
        {
            _characters = _characterRequest.GetAwaiter().GetResult();
            _characterRequest = null;
            if (!_characters.Any(character => character.Name == _selectedCharacter))
                _selectedCharacter = null;
        }

        if (_characters.Count == 0 || !ImGui.BeginCombo("角色", _selectedCharacter ?? "请选择角色"))
            return;

        foreach (var character in _characters)
        {
            bool selected = character.Name == _selectedCharacter;
            if (ImGui.Selectable(character.Name, selected))
                _selectedCharacter = character.Name;
            if (selected)
                ImGui.SetItemDefaultFocus();
        }
        ImGui.EndCombo();
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
