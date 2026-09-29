using Microsoft.Extensions.Configuration;
using PaymentRagAssistant.Infrastructure.Embeddings;
using PaymentRagAssistant.Infrastructure.VectorStore;

// Run with: dotnet run --project PaymentRagAssistant.Ingestion -- ./docs
// Point it at a folder of .txt/.md files: transaction failure-code lists, dispute
// runbooks, reconciliation SOPs, exported Swagger descriptions, etc.

var docsFolder = args.Length > 0 ? args[0] : "./docs";

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var embeddingOptions = config.GetSection("Ollama").Get<OllamaOptions>() ?? new();
var qdrantOptions = config.GetSection("Qdrant").Get<QdrantOptions>() ?? new();

using var http = new HttpClient();
var embeddingService = new OllamaEmbeddingService(http, embeddingOptions);
var vectorStore = new QdrantVectorStore(qdrantOptions);

const int chunkSizeChars = 1200;
const int chunkOverlapChars = 200;

Console.WriteLine($"Ingesting documents from {docsFolder}...");

var files = Directory.EnumerateFiles(docsFolder, "*.*", SearchOption.AllDirectories)
    .Where(f => f.EndsWith(".txt") || f.EndsWith(".md"));

var firstEmbedding = await embeddingService.EmbedAsync("dimension probe");
await vectorStore.EnsureCollectionAsync(firstEmbedding.Length);

foreach (var file in files)
{
    var text = await File.ReadAllTextAsync(file);
    var chunks = ChunkText(text, chunkSizeChars, chunkOverlapChars);
    var sourceName = Path.GetFileName(file);

    Console.WriteLine($"  {sourceName}: {chunks.Count} chunks");

    foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
    {
        var embedding = await embeddingService.EmbedAsync(chunk);
        var id = Guid.NewGuid().ToString();
        await vectorStore.UpsertAsync(id, embedding, chunk, sourceName, page: index + 1);
    }
}

Console.WriteLine("Done.");

static List<string> ChunkText(string text, int chunkSize, int overlap)
{
    var chunks = new List<string>();
    var start = 0;
    while (start < text.Length)
    {
        var length = Math.Min(chunkSize, text.Length - start);
        chunks.Add(text.Substring(start, length));
        if (start + length >= text.Length) break;
        start += chunkSize - overlap;
    }
    return chunks;
}
