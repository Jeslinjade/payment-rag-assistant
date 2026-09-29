using PaymentRagAssistant.Application;
using PaymentRagAssistant.Application.Pipeline;
using PaymentRagAssistant.Infrastructure.Analytics;
using PaymentRagAssistant.Infrastructure.Embeddings;
using PaymentRagAssistant.Infrastructure.Llm;
using PaymentRagAssistant.Infrastructure.VectorStore;
var builder = WebApplication.CreateBuilder(args);

// ---- Options ----
var groqOptions = builder.Configuration.GetSection("Groq").Get<GroqOptions>() ?? new();
var embeddingOptions = builder.Configuration.GetSection("Ollama").Get<OllamaOptions>() ?? new();
var qdrantOptions = builder.Configuration.GetSection("Qdrant").Get<QdrantOptions>() ?? new();
var analyticsConnectionString = builder.Configuration.GetConnectionString("Analytics")!;

// ---- Pipeline stages (Application layer) ----
builder.Services.AddSingleton<IQueryRewriter, QueryRewriter>();
builder.Services.AddSingleton<IRetrievalGrader, RetrievalGrader>();
builder.Services.AddSingleton<IReranker, Reranker>();
builder.Services.AddSingleton<IKnowledgeGapDetector, KnowledgeGapDetector>();
builder.Services.AddSingleton<IConfidenceScorer, ConfidenceScorer>();
builder.Services.AddSingleton<RagOrchestrator>();

// ---- Infrastructure (swap implementations here without touching Application/) ----
builder.Services.AddHttpClient<ILlmClient, GroqLlmClient>()
    .Services.AddSingleton(groqOptions);

builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>()
    .Services.AddSingleton(embeddingOptions);

builder.Services.AddSingleton(qdrantOptions);
builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();

builder.Services.AddSingleton<IAnalyticsRepository>(new SqlAnalyticsRepository(analyticsConnectionString));

var app = builder.Build();

// Make sure the analytics table exists on startup (dev convenience only).
using (var scope = app.Services.CreateScope())
{
    var analytics = (SqlAnalyticsRepository)scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>();
    await analytics.EnsureTableAsync();
}

app.MapPost("/api/query", async (QueryRequest request, RagOrchestrator orchestrator, ILlmClient llm, HttpContext http, CancellationToken ct) =>
{
    var (context, trace, sources) = await orchestrator.PrepareAsync(request.Question, ct);

    http.Response.Headers.Append("Content-Type", "text/event-stream");
    http.Response.Headers.Append("Cache-Control", "no-cache");

    var userPrompt = $"Context:\n{context}\n\nQuestion: {request.Question}";

    await foreach (var token in llm.StreamCompleteAsync(RagOrchestrator.AnswerSystemPrompt, userPrompt, ct))
    {
        await http.Response.WriteAsync($"data: {token}\n\n", ct);
        await http.Response.Body.FlushAsync(ct);
    }

    // Final event carries metadata the frontend uses to render sources + confidence.
    var metadata = new
    {
        confidence = trace.ConfidenceScore,
        knowledgeGap = trace.KnowledgeGapDetected,
        retries = trace.RetryCount,
        sources = sources.Select(s => new { s.SourceDocument, s.PageNumber })
    };
    await http.Response.WriteAsync($"event: metadata\ndata: {System.Text.Json.JsonSerializer.Serialize(metadata)}\n\n", ct);
});

app.MapGet("/api/analytics", async (IAnalyticsRepository analytics, CancellationToken ct) =>
{
    var recent = await analytics.GetRecentAsync(50, ct);
    return Results.Ok(recent);
});

app.Run();

public record QueryRequest(string Question);
