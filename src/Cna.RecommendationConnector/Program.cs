using Cna.RecommendationConnector;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

var outwardApiBaseUrl = builder.Configuration["OutwardApi:BaseUrl"]
    ?? throw new InvalidOperationException("OutwardApi:BaseUrl must be configured.");
var ollamaBaseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
var ollamaModel = builder.Configuration["Ollama:Model"] ?? "qwen2.5:1.5b";

builder.Services.AddHttpClient("OutwardApi", client => client.BaseAddress = new Uri(outwardApiBaseUrl));
builder.Services.AddSingleton<IOllamaApiClient>(new OllamaApiClient(new Uri(ollamaBaseUrl), ollamaModel));
builder.Services.AddScoped(sp => new InitiativeOrderAdvisor(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("OutwardApi"),
    sp.GetRequiredService<IOllamaApiClient>()));

var app = builder.Build();

// Pull-only UX (decision 2): one explicit "ask for advice" endpoint, no proactive
// push. Advisory only -- this never calls /actions, only queries an opening.
app.MapPost("/advise/initiative-order", async (
    AdviceRequest request, InitiativeOrderAdvisor advisor, CancellationToken cancellationToken) =>
{
    var credential = new CampaignSeatCredential(
        request.CampaignId, request.Audience, request.Epoch, request.Secret, request.ExpiresUnixMs);
    var result = await advisor.Advise(credential, request.ActingElementId, request.Stage, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/", () => Results.Ok(new { service = "Cna.RecommendationConnector", authoritative = false }));

app.Run();

internal sealed record AdviceRequest(
    string CampaignId, string Audience, long Epoch, string Secret, long ExpiresUnixMs,
    string ActingElementId, string Stage);

public partial class Program;
