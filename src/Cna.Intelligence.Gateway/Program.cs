using Cna.Intelligence.Gateway.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// https://learn.microsoft.com/en-us/aspnet/core/grpc/aspnetcore?view=aspnetcore-10.0
builder.Services.AddGrpc();
builder.Services.AddSingleton<IIntelligenceProviderStatus, NoIntelligenceProviderStatus>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGrpcService<IntelligenceGrpcService>();
app.MapGet("/", (IIntelligenceProviderStatus providerStatus) => Results.Ok(new
{
    service = "Cna.Intelligence.Gateway",
    authoritative = false,
    providerConfigured = providerStatus.AnyProviderAvailable,
}));

app.Run();

public partial class Program;
