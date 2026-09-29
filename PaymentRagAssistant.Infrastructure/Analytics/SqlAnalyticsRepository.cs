using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using PaymentRagAssistant.Application;
using PaymentRagAssistant.Application.Models;

namespace PaymentRagAssistant.Infrastructure.Analytics;

/// <summary>
/// Logs one row per query: rewritten queries, retry count, confidence, knowledge-gap flag
/// and elapsed time - this is what the /api/analytics endpoint reads for the dashboard.
/// </summary>
public class SqlAnalyticsRepository : IAnalyticsRepository
{
    private readonly string _connectionString;

    public SqlAnalyticsRepository(string connectionString) => _connectionString = connectionString;

    public async Task EnsureTableAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync("""
            IF OBJECT_ID('dbo.QueryTraces', 'U') IS NULL
            CREATE TABLE dbo.QueryTraces (
                Id INT IDENTITY PRIMARY KEY,
                OriginalQuery NVARCHAR(1000) NOT NULL,
                RewrittenQueriesJson NVARCHAR(MAX) NULL,
                RetryCount INT NOT NULL,
                KnowledgeGapDetected BIT NOT NULL,
                ConfidenceScore FLOAT NOT NULL,
                ElapsedMs INT NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );
            """);
    }

    public async Task RecordAsync(PipelineTrace trace, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync("""
            INSERT INTO dbo.QueryTraces
                (OriginalQuery, RewrittenQueriesJson, RetryCount, KnowledgeGapDetected, ConfidenceScore, ElapsedMs)
            VALUES
                (@OriginalQuery, @RewrittenQueriesJson, @RetryCount, @KnowledgeGapDetected, @ConfidenceScore, @ElapsedMs)
            """, new
        {
            trace.OriginalQuery,
            RewrittenQueriesJson = JsonSerializer.Serialize(trace.RewrittenQueries),
            trace.RetryCount,
            trace.KnowledgeGapDetected,
            trace.ConfidenceScore,
            ElapsedMs = (int)trace.ElapsedTime.TotalMilliseconds
        });
    }

    public async Task<IReadOnlyList<PipelineTrace>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync("""
            SELECT TOP (@count) OriginalQuery, RewrittenQueriesJson, RetryCount,
                   KnowledgeGapDetected, ConfidenceScore, ElapsedMs
            FROM dbo.QueryTraces
            ORDER BY CreatedAtUtc DESC
            """, new { count });

        return rows.Select(r => new PipelineTrace
        {
            OriginalQuery = r.OriginalQuery,
            RewrittenQueries = JsonSerializer.Deserialize<List<string>>((string)(r.RewrittenQueriesJson ?? "[]")) ?? new List<string>(),
            RetryCount = r.RetryCount,
            KnowledgeGapDetected = r.KnowledgeGapDetected,
            ConfidenceScore = r.ConfidenceScore,
            ElapsedTime = TimeSpan.FromMilliseconds((int)r.ElapsedMs)
        }).ToList();
    }
}
