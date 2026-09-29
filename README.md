# Payment Support Self-Healing RAG Assistant (.NET)

A self-healing Retrieval-Augmented Generation assistant for payment/transaction support
questions — query rewriting, retrieval grading, automatic retries, reranking, knowledge-gap
detection and confidence scoring, in ASP.NET Core.

## Project layout

```
PaymentRagAssistant.Application/    # pipeline stages + orchestrator, no external deps
PaymentRagAssistant.Infrastructure/ # Qdrant, Groq, Azure OpenAI embeddings, SQL analytics
PaymentRagAssistant.Api/            # ASP.NET Core Web API host (streaming /api/query)
PaymentRagAssistant.Ingestion/      # console app: chunk + embed your docs into Qdrant
docker-compose.yml                  # Qdrant + SQL Server + the API, for local dev
```

## 1. Create the solution (one-time)

The files above are ready to drop in — you just need a `.sln` tying them together:

```bash
mkdir PaymentRagAssistant && cd PaymentRagAssistant
dotnet new sln -n PaymentRagAssistant
# copy all four project folders + docker-compose.yml + this README in here, then:
dotnet sln add PaymentRagAssistant.Application/PaymentRagAssistant.Application.csproj
dotnet sln add PaymentRagAssistant.Infrastructure/PaymentRagAssistant.Infrastructure.csproj
dotnet sln add PaymentRagAssistant.Api/PaymentRagAssistant.Api.csproj
dotnet sln add PaymentRagAssistant.Ingestion/PaymentRagAssistant.Ingestion.csproj
dotnet restore
```

## 2. Configure secrets

Fill in `PaymentRagAssistant.Api/appsettings.json` (or use `dotnet user-secrets` /
environment variables so keys don't end up in git):

- `Groq:ApiKey` — free key from https://console.groq.com
- `AzureOpenAiEmbeddings:*` — or swap `AzureOpenAiEmbeddingService` for a local
  sentence-transformers ONNX model if you don't have Azure OpenAI access
- `ConnectionStrings:Analytics` — SQL Server connection string

## 3. Start the infra

```bash
docker compose up -d qdrant sqlserver
```

## 4. Ingest your payment docs

Drop `.txt`/`.md` files (transaction failure-code lists, dispute/chargeback runbooks,
reconciliation SOPs, exported API docs) into a `docs/` folder, then:

```bash
dotnet run --project PaymentRagAssistant.Ingestion -- ./docs
```

## 5. Run the API

```bash
dotnet run --project PaymentRagAssistant.Api
```

Test it:

```bash
curl -N -X POST http://localhost:5000/api/query \
  -H "Content-Type: application/json" \
  -d '{"question": "Why would a transaction fail with error code 51?"}'
```

You'll see the answer stream as SSE `data:` lines, followed by a final `event: metadata`
line with confidence score, retry count, and sources.

`GET /api/analytics` returns the last 50 query traces for a dashboard.

## What to build next

- A small React/Blazor page for the chat + analytics dashboard (mirrors the reference repo's UI)
- Swap the `RetrievalGrader`'s heuristic for an LLM-as-judge prompt
- Add auth (API key or Azure AD) before deploying anywhere shared
- GitHub Actions workflow: build → test → push image → deploy (same pattern as your repo-migration work)
