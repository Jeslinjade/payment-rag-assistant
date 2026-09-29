namespace PaymentRagAssistant.Application.Models;

public record RetrievedChunk(
    string Id,
    string Text,
    string SourceDocument,
    int? PageNumber,
    float SimilarityScore)
{
    // Set by the RetrievalGrader (0-1). Null until graded.
    public float? RelevanceGrade { get; set; }
}

public record PipelineTrace
{
    public string OriginalQuery { get; init; } = string.Empty;
    public List<string> RewrittenQueries { get; init; } = new();
    public int RetryCount { get; set; }
    public bool KnowledgeGapDetected { get; set; }
    public double ConfidenceScore { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public List<RetrievedChunk> ChunksUsed { get; init; } = new();
}

public record QueryResult(
    string Answer,
    double ConfidenceScore,
    bool KnowledgeGapDetected,
    IReadOnlyList<RetrievedChunk> Sources,
    PipelineTrace Trace);
