using PaymentRagAssistant.Application.Models;

namespace PaymentRagAssistant.Application.Pipeline;

public interface IRetrievalGrader
{
    Task<IReadOnlyList<RetrievedChunk>> GradeAsync(string query, IReadOnlyList<RetrievedChunk> chunks, CancellationToken ct = default);

    /// <summary>True if the graded chunks are good enough to answer from; false triggers a retry.</summary>
    bool IsSufficient(IReadOnlyList<RetrievedChunk> gradedChunks);
}

/// <summary>
/// Scores each retrieved chunk's relevance to the query. Starts with a cheap lexical/
/// vector-score heuristic; swap the TODO for an LLM-as-judge call if you want higher accuracy.
/// </summary>
public class RetrievalGrader : IRetrievalGrader
{
    private const float SufficiencyThreshold = 0.55f;

    public Task<IReadOnlyList<RetrievedChunk>> GradeAsync(string query, IReadOnlyList<RetrievedChunk> chunks, CancellationToken ct = default)
    {
        // Cheap baseline: reuse vector similarity as the grade.
        // TODO: replace with an LLM-as-judge prompt ("Is this chunk relevant to the query? 0-1")
        // once the vector-only version is working end-to-end.
        foreach (var chunk in chunks)
        {
            chunk.RelevanceGrade = chunk.SimilarityScore;
        }

        return Task.FromResult(chunks);
    }

    public bool IsSufficient(IReadOnlyList<RetrievedChunk> gradedChunks)
    {
        if (gradedChunks.Count == 0) return false;
        return gradedChunks.Any(c => (c.RelevanceGrade ?? 0f) >= SufficiencyThreshold);
    }
}
