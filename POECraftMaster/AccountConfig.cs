using System.Text.Json;

namespace POECraftMaster;

internal sealed record AccountConfig(string AccountName = "", string Cookie = "")
{
    public static AccountConfig? Load(string path)
    {
        string contents = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(contents))
            return null;

        var config = JsonSerializer.Deserialize<AccountConfig>(contents)
            ?? throw new InvalidDataException("账号配置必须是 JSON 对象。");
        return new AccountConfig(config.AccountName ?? "", config.Cookie ?? "");
    }

    public void Save(string path, bool overwrite = true)
    {
        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, Path.GetRandomFileName());
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, overwrite);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
