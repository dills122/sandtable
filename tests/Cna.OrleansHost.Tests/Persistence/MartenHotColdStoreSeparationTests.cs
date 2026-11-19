using Cna.OrleansHost.Persistence;
using JasperFx;
using Marten;

namespace Cna.OrleansHost.Tests.Persistence;

// Proves the shape design doc §2/§2.1 asks for: two independent Marten DocumentStores, each its
// own PostgreSQL database, not one shared schema with a tier-discriminator column. The same
// document id is written with different payloads into each store; if the stores shared a schema
// or database, the second write would overwrite the first and both reads would see the same
// (wrong) value. This uses a trivial shared document type -- not the real campaign event schema,
// which is Phase C's job.
[Collection(PostgresCollectionDefinition.Name)]
public sealed class MartenHotColdStoreSeparationTests
{
    private readonly PostgresContainerFixture fixture;

    public MartenHotColdStoreSeparationTests(PostgresContainerFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task HotAndColdStoresDoNotShareData()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var hotConnectionString = await fixture.CreateDatabaseAsync("sandtable_hot_poc");
        var coldConnectionString = await fixture.CreateDatabaseAsync("sandtable_cold_poc");

        using var hotStore = MartenStoreFactory.CreateStore(hotConnectionString,
            options => options.AutoCreateSchemaObjects = AutoCreate.All);
        using var coldStore = MartenStoreFactory.CreateStore(coldConnectionString,
            options => options.AutoCreateSchemaObjects = AutoCreate.All);

        var sharedId = Guid.NewGuid();

        await using (var hotSession = hotStore.LightweightSession())
        {
            hotSession.Store(new TierProbeDocument(sharedId, "hot"));
            await hotSession.SaveChangesAsync(cancellationToken);
        }

        await using (var coldSession = coldStore.LightweightSession())
        {
            coldSession.Store(new TierProbeDocument(sharedId, "cold"));
            await coldSession.SaveChangesAsync(cancellationToken);
        }

        await using var hotQuery = hotStore.QuerySession();
        await using var coldQuery = coldStore.QuerySession();

        var hotDocument = await hotQuery.LoadAsync<TierProbeDocument>(sharedId, cancellationToken);
        var coldDocument = await coldQuery.LoadAsync<TierProbeDocument>(sharedId, cancellationToken);

        Assert.Equal("hot", hotDocument?.Tier);
        Assert.Equal("cold", coldDocument?.Tier);
    }

    private sealed record TierProbeDocument(Guid Id, string Tier);
}
