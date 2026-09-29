using System.Net.Http.Json;
using System.Text.Json;
using PaymentRagAssistant.Application;

namespace PaymentRagAssistant.Infrastructure.Embeddings;

public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "nomic-embed-text";
}

/// <summary>
/// Calls a locally-running Ollama instance for embeddings - free, no API key, no internet
/// required once the model is pulled. Install: https://ollama.com, then `ollama pull nomic-embed-text`.
/// Same IEmbeddingService contract as the Azure version, so swapping back later is a one-line change.
/// </summary>
public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    public OllamaEmbeddingService(HttpClient http, OllamaOptions options)
    {
        _http = http;
        _options = options;
        _http.BaseAddress = new Uri(_options.BaseUrl);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/embeddings", new
        {
            model = _options.Model,
            prompt = text
        }, ct);

        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return doc.GetProperty("embedding").EnumerateArray().Select(v => v.GetSingle()).ToArray();
    }
}
