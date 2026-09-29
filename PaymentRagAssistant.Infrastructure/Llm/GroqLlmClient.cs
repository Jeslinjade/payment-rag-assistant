using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using PaymentRagAssistant.Application;

namespace PaymentRagAssistant.Infrastructure.Llm;

public class GroqOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "openai/gpt-oss-20b";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}

/// <summary>
/// Talks to Groq's OpenAI-compatible chat completions endpoint. Swap this for an
/// Azure OpenAI client later without touching anything in Application/ - it only
/// depends on ILlmClient.
/// </summary>
public class GroqLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly GroqOptions _options;

    public GroqLlmClient(HttpClient http, GroqOptions options)
    {
        _http = http;
        _options = options;
         _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("chat/completions", new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            stream = false
        }, ct);

        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return doc.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamCompleteAsync(
        string systemPrompt, string userPrompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(new
            {
                model = _options.Model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                stream = true
            })
        };

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

            var payload = line["data: ".Length..];
            if (payload == "[DONE]") yield break;

            using var doc = JsonDocument.Parse(payload);
            var delta = doc.RootElement.GetProperty("choices")[0].GetProperty("delta");
            if (delta.TryGetProperty("content", out var contentProp))
            {
                var token = contentProp.GetString();
                if (!string.IsNullOrEmpty(token)) yield return token;
            }
        }
    }
}
