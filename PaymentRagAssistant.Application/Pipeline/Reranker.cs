using PaymentRagAssistant.Application.Models;

namespace PaymentRagAssistant.Application.Pipeline;

public interface IReranker
{
    IReadOnlyList<RetrievedChunk> Rerank(IReadOnlyList<RetrievedChunk> gradedChunks, int keep);
}

/// <summary>
/// Orders graded chunks best-first and trims to the top N that get sent to the LLM as context.
/// Sorting by grade first, similarity as a tiebreaker.
/// </summary>
public class Reranker : IReranker
{
    public IReadOnlyList<RetrievedChunk> Rerank(IReadOnlyList<RetrievedChunk> gradedChunks, int keep)
    {
        return gradedChunks
            .OrderByDescending(c => c.RelevanceGrade ?? 0f)
            .ThenByDescending(c => c.SimilarityScore)
            .Take(keep)
            .ToList();
    }
}
