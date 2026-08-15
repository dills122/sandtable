using Cna.Intelligence.Gateway.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// https://learn.microsoft.com/en-us/aspnet/core/grpc/aspnetcore?view=aspnetcore-10.0
builder.Services.AddGrpc();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGrpcService<IntelligenceGrpcService>();
app.MapGet("/", () => Results.Ok(new
{
    service = "Cna.Intelligence.Gateway",
    authoritative = false,
    providerConfigured = false,
}));

app.Run();

public partial class Program;
