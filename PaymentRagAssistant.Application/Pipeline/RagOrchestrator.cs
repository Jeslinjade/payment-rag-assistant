using System.Diagnostics;
using PaymentRagAssistant.Application.Models;

namespace PaymentRagAssistant.Application.Pipeline;

public class RagOrchestrator
{
    private const int MaxRetries = 2;
    private const int TopKPerSearch = 8;
    private const int ChunksKeptForAnswer = 4;

    private readonly IQueryRewriter _rewriter;
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _vectorStore;
    private readonly IRetrievalGrader _grader;
    private readonly IReranker _reranker;
    private readonly IKnowledgeGapDetector _gapDetector;
    private readonly IConfidenceScorer _confidenceScorer;
    private readonly ILlmClient _llm;
    private readonly IAnalyticsRepository _analytics;

    public RagOrchestrator(
        IQueryRewriter rewriter,
        IEmbeddingService embeddings,
        IVectorStore vectorStore,
        IRetrievalGrader grader,
        IReranker reranker,
        IKnowledgeGapDetector gapDetector,
        IConfidenceScorer confidenceScorer,
        ILlmClient llm,
        IAnalyticsRepository analytics)
    {
        _rewriter = rewriter;
        _embeddings = embeddings;
        _vectorStore = vectorStore;
        _grader = grader;
        _reranker = reranker;
        _gapDetector = gapDetector;
        _confidenceScorer = confidenceScorer;
        _llm = llm;
        _analytics = analytics;
    }

    /// <summary>
    /// Runs rewrite -> retrieve -> grade -> retry-if-needed -> rerank -> gap-check,
    /// stopping right before generation. The API layer calls this once, then streams
    /// the LLM answer itself so tokens can go straight to the HTTP response.
    /// </summary>
    public async Task<(string PromptContext, PipelineTrace Trace, IReadOnlyList<RetrievedChunk> Sources)> PrepareAsync(
        string originalQuery, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var trace = new PipelineTrace { OriginalQuery = originalQuery };

        var currentQuery = originalQuery;
        IReadOnlyList<RetrievedChunk> graded = Array.Empty<RetrievedChunk>();
        string? lastFailureReason = null;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 0)
            {
                currentQuery = await _rewriter.RewriteAsync(originalQuery, lastFailureReason, ct);
                trace.RewrittenQueries.Add(currentQuery);
                trace.RetryCount = attempt;
            }

            var embedding = await _embeddings.EmbedAsync(currentQuery, ct);
            var retrieved = await _vectorStore.SearchAsync(embedding, TopKPerSearch, ct);
            graded = await _grader.GradeAsync(currentQuery, retrieved, ct);

            if (_grader.IsSufficient(graded))
                break;

            lastFailureReason = "retrieved documents were not relevant enough";
        }

        var reranked = _reranker.Rerank(graded, ChunksKeptForAnswer);
        var gap = _gapDetector.HasGap(reranked);
        var confidence = _confidenceScorer.Score(reranked, trace.RetryCount, gap);

        trace.KnowledgeGapDetected = gap;
        trace.ConfidenceScore = confidence;
        trace.ChunksUsed.AddRange(reranked);
        trace.ElapsedTime = stopwatch.Elapsed;

        await _analytics.RecordAsync(trace, ct);

        var context = string.Join(
            "\n\n---\n\n",
            reranked.Select(c => $"[Source: {c.SourceDocument}{(c.PageNumber is null ? "" : $", p.{c.PageNumber}")}]\n{c.Text}"));

        return (context, trace, reranked);
    }

    public const string AnswerSystemPrompt = """
        You are a payments support assistant. Answer the user's question about transactions,
        disputes, chargebacks or reconciliation using ONLY the provided context. If the context
        does not contain the answer, say you don't have enough information rather than guessing.
        Cite the source document name for any specific fact.
        """;
}
