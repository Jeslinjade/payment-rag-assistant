using PaymentRagAssistant.Application;
using PaymentRagAssistant.Application.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace PaymentRagAssistant.Infrastructure.VectorStore;

public class QdrantOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6334;
    public string CollectionName { get; set; } = "payment_docs";
}

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;

    public QdrantVectorStore(QdrantOptions options)
    {
        _options = options;
        _client = new QdrantClient(options.Host, options.Port);
    }

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken ct = default)
    {
        var results = await _client.SearchAsync(
            _options.CollectionName,
            queryEmbedding,
            limit: (ulong)topK);

        return results.Select(hit => new RetrievedChunk(
            Id: hit.Id.Uuid,
            Text: hit.Payload["text"].StringValue,
            SourceDocument: hit.Payload["source"].StringValue,
            PageNumber: hit.Payload.TryGetValue("page", out var page) ? (int)page.IntegerValue : null,
            SimilarityScore: hit.Score
        )).ToList();
    }

    // Called once by the ingestion tool (or on first run) to make sure the collection exists.
    public async Task EnsureCollectionAsync(int vectorSize, CancellationToken ct = default)
    {
        var collections = await _client.ListCollectionsAsync();
        if (!collections.Contains(_options.CollectionName))
        {
            await _client.CreateCollectionAsync(
                _options.CollectionName,
                new VectorParams { Size = (ulong)vectorSize, Distance = Distance.Cosine });
        }
    }

    // Used by the ingestion console app to write chunked + embedded documents.
    public async Task UpsertAsync(string id, float[] embedding, string text, string sourceDocument, int? page, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, Value>
        {
            ["text"] = text,
            ["source"] = sourceDocument
        };
        if (page is not null) payload["page"] = page.Value;

        var point = new PointStruct
        {
            Id = new PointId { Uuid = id },
            Vectors = embedding
        };
        foreach (var kvp in payload) point.Payload.Add(kvp.Key, kvp.Value);

        await _client.UpsertAsync(_options.CollectionName, new[] { point });
    }
}
