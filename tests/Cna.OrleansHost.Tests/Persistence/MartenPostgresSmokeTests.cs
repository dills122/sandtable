using Cna.OrleansHost.Persistence;
using JasperFx;
using Marten;

namespace Cna.OrleansHost.Tests.Persistence;

// Phase A smoke test (issue #439): proves a Marten DocumentStore can connect to a real
// Testcontainers-provided PostgreSQL instance, that Marten can create/track its own schema there,
// and that a trivial document round-trips. Deliberately not a real campaign-domain test -- that
// schema is Phase C's job.
[Collection(PostgresCollectionDefinition.Name)]
public sealed class MartenPostgresSmokeTests
{
    private readonly PostgresContainerFixture fixture;

    public MartenPostgresSmokeTests(PostgresContainerFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task DocumentStoreConnectsCreatesSchemaAndRoundTripsADocument()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await fixture.CreateDatabaseAsync("sandtable_smoke");
        using var store = MartenStoreFactory.CreateStore(connectionString,
            options => options.AutoCreateSchemaObjects = AutoCreate.All);

        var document = new SmokeTestDocument(Guid.NewGuid(), "phase-a-smoke-test");
        await using (var session = store.LightweightSession())
        {
            session.Store(document);
            await session.SaveChangesAsync(cancellationToken);
        }

        await using var query = store.QuerySession();
        var loaded = await query.LoadAsync<SmokeTestDocument>(document.Id, cancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(document.Payload, loaded.Payload);
    }

    private sealed record SmokeTestDocument(Guid Id, string Payload);
}
