using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using POECraftMaster.Singleton;

namespace POECraftMaster;

internal sealed class PoeApiClient : Singleton<PoeApiClient>
{
    private readonly HttpClient _http = new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        AutomaticDecompression = DecompressionMethods.All
    })
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public override void Dispose() => _http.Dispose();

    public async Task<IReadOnlyList<PoeCharacter>> GetCharactersAsync(
        AccountConfig account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (string.IsNullOrWhiteSpace(account.AccountName) || string.IsNullOrWhiteSpace(account.Cookie))
            throw new ArgumentException("请先在设置中填写 AccountName 和 Cookie。", nameof(account));

        string cookie = account.Cookie.Trim();
        if (cookie.StartsWith("Cookie:", StringComparison.OrdinalIgnoreCase))
            cookie = cookie["Cookie:".Length..].TrimStart();
        if (cookie.Length == 0 || cookie.Contains('\r') || cookie.Contains('\n'))
            throw new ArgumentException("请填写单行 Cookie 请求头值。", nameof(account));

        using var request = new HttpRequestMessage(HttpMethod.Post,
            "https://poe.game.qq.com/character-window/get-characters");
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Add("Cookie", cookie);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["accountName"] = account.AccountName.Trim(),
            ["realm"] = "pc"
        });

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new HttpRequestException("国服登录会话无效或已过期，请更新 Cookie。", null, response.StatusCode);
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new HttpRequestException("国服拒绝查询该账号，请检查账号名及角色隐私设置。", null, response.StatusCode);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            string retryAfter = response.Headers.RetryAfter?.ToString() ?? "未提供";
            throw new HttpRequestException($"查询过于频繁，请稍后再试（Retry-After：{retryAfter}）。",
                null, response.StatusCode);
        }
        response.EnsureSuccessStatusCode();

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await JsonSerializer.DeserializeAsync<PoeCharacter[]>(body, cancellationToken: cancellationToken)
                .ConfigureAwait(false) ?? throw new InvalidDataException("国服接口未返回角色列表。");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("国服接口返回的内容不是有效的角色列表。", exception);
        }
    }
}

internal sealed record PoeCharacter(
    [property: JsonPropertyName("name"), JsonRequired] string Name,
    [property: JsonPropertyName("league")] string? League,
    [property: JsonPropertyName("class")] string? Class,
    [property: JsonPropertyName("level")] int Level);
