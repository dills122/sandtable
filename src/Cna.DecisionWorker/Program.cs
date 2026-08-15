using Cna.Intelligence.Contracts.V1;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Aspire service discovery resolves the named gateway in local and deployed environments.
// https://learn.microsoft.com/en-us/dotnet/aspire/service-discovery/overview
var endpoint = builder.Configuration["Intelligence:Endpoint"]
    ?? "https+http://intelligence-gateway";

builder.Services.AddGrpcClient<IntelligenceService.IntelligenceServiceClient>(options =>
{
    options.Address = new Uri(endpoint);
});

var host = builder.Build();
host.Run();
