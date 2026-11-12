
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace GameCursorTranslator;

internal sealed class TranslationService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(6) };
    private readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<string> TranslateAsync(string word, CancellationToken token)
    {
        if (_cache.TryGetValue(word, out var cached)) return cached;
        if (!word.Any(char.IsLetter)) return word;

        var url = "https://api.mymemory.translated.net/get?q=" +
                  Uri.EscapeDataString(word) + "&langpair=en|zh-CN";
        using var response = await Http.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<MyMemoryResponse>(cancellationToken: token);
        var translated = WebUtility.HtmlDecode(payload?.ResponseData?.TranslatedText ?? "").Trim();
        if (string.IsNullOrWhiteSpace(translated)) translated = word;
        _cache[word] = translated;
        return translated;
    }

    private sealed class MyMemoryResponse
    {
        [JsonPropertyName("responseData")] public ResponseData? ResponseData { get; set; }
    }
    private sealed class ResponseData
    {
        [JsonPropertyName("translatedText")] public string? TranslatedText { get; set; }
    }
}
