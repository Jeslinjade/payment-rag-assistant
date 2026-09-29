using PaymentRagAssistant.Application;

namespace PaymentRagAssistant.Application.Pipeline;

public interface IQueryRewriter
{
    Task<string> RewriteAsync(string originalQuery, string? failureReason, CancellationToken ct = default);
}

/// <summary>
/// Uses the LLM to turn a vague/ambiguous payment support question into a sharper
/// search query (expands abbreviations like "txn", adds likely failure-code terms, etc).
/// </summary>
public class QueryRewriter : IQueryRewriter
{
    private readonly ILlmClient _llm;

    public QueryRewriter(ILlmClient llm) => _llm = llm;

    private const string SystemPrompt = """
        You rewrite user questions about payment transactions, disputes, chargebacks and
        reconciliation into a single, precise search query for a document retrieval system.
        Expand abbreviations (txn -> transaction, chgbk -> chargeback), add likely error/status
        codes if implied, and keep it to one line. Return ONLY the rewritten query, nothing else.
        """;

    public async Task<string> RewriteAsync(string originalQuery, string? failureReason, CancellationToken ct = default)
    {
        var userPrompt = failureReason is null
            ? $"Original question: {originalQuery}"
            : $"Original question: {originalQuery}\nPrevious search failed because: {failureReason}\nRewrite it to fix that.";

        var rewritten = await _llm.CompleteAsync(SystemPrompt, userPrompt, ct);
        return rewritten.Trim().Trim('"');
    }
}
