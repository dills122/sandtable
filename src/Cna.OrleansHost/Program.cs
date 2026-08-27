var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/server-configuration
builder.Host.UseOrleans(siloBuilder =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Single-machine, dev-only clustering provider. Fine for local development,
        // but silently using it in any other environment would produce a silo that
        // can never form a multi-instance cluster.
        siloBuilder.UseLocalhostClustering();
    }
    else
    {
        // TODO: wire up a persistent clustering provider (e.g. Azure Table Storage,
        // ADO.NET, or Kubernetes) before deploying outside local development. There is
        // no production clustering provider configured yet, so fail loudly here instead
        // of silently falling back to localhost clustering, which would misbehave (or
        // simply not cluster) outside a single dev machine.
        throw new InvalidOperationException(
            "No production Orleans clustering provider is configured. " +
            "UseLocalhostClustering() is dev-only and cannot be used outside Development. " +
            "Configure a persistent clustering provider before deploying this host.");
    }
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", () => Results.Ok(new
{
    service = "Cna.OrleansHost",
    authoritative = true,
}));

app.Run();

public partial class Program;
