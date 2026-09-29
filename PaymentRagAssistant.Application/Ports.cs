using PaymentRagAssistant.Application.Models;

namespace PaymentRagAssistant.Application;

/// <summary>Talks to the vector database (Qdrant / pgvector).</summary>
public interface IVectorStore
{
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken ct = default);
}

/// <summary>Turns text into embeddings.</summary>
public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}

/// <summary>Talks to the LLM (Groq / Azure OpenAI / OpenAI).</summary>
public interface ILlmClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);

    IAsyncEnumerable<string> StreamCompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}

/// <summary>Persists per-request analytics for the dashboard.</summary>
public interface IAnalyticsRepository
{
    Task RecordAsync(PipelineTrace trace, CancellationToken ct = default);

    Task<IReadOnlyList<PipelineTrace>> GetRecentAsync(int count, CancellationToken ct = default);
}
