var builder = DistributedApplication.CreateBuilder(args);

// https://learn.microsoft.com/en-us/dotnet/aspire/get-started/add-aspire-existing-app
var intelligenceGateway = builder.AddProject<Projects.Cna_Intelligence_Gateway>(
    "intelligence-gateway");

builder.AddProject<Projects.Cna_OrleansHost>("orleans-host");

builder.AddProject<Projects.Cna_DecisionWorker>("decision-worker")
    .WithReference(intelligenceGateway)
    .WaitFor(intelligenceGateway);

builder.Build().Run();
