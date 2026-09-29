using PaymentRagAssistant.Application.Models;

namespace PaymentRagAssistant.Application.Pipeline;

public interface IKnowledgeGapDetector
{
    bool HasGap(IReadOnlyList<RetrievedChunk> rerankedChunks);
}

/// <summary>
/// Flags when no retrieved chunk clears a minimum relevance bar even after retries -
/// i.e. the knowledge base probably just doesn't cover this question.
/// </summary>
public class KnowledgeGapDetector : IKnowledgeGapDetector
{
    private const float MinAcceptableGrade = 0.35f;

    public bool HasGap(IReadOnlyList<RetrievedChunk> rerankedChunks)
    {
        if (rerankedChunks.Count == 0) return true;
        return rerankedChunks.All(c => (c.RelevanceGrade ?? 0f) < MinAcceptableGrade);
    }
}

public interface IConfidenceScorer
{
    double Score(IReadOnlyList<RetrievedChunk> finalChunks, int retryCount, bool knowledgeGap);
}

/// <summary>
/// Combines retrieval quality, how many retries it took, and whether a knowledge gap
/// was flagged into a single 0-1 confidence number shown to the user/analytics.
/// </summary>
public class ConfidenceScorer : IConfidenceScorer
{
    public double Score(IReadOnlyList<RetrievedChunk> finalChunks, int retryCount, bool knowledgeGap)
    {
        if (knowledgeGap) return 0.1;

        var avgGrade = finalChunks.Count == 0
            ? 0
            : finalChunks.Average(c => c.RelevanceGrade ?? 0f);

        var retryPenalty = Math.Min(retryCount * 0.1, 0.3);
        var score = Math.Clamp(avgGrade - retryPenalty, 0, 1);
        return Math.Round(score, 2);
    }
}
