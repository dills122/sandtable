using Cna.OrleansHost.Persistence;
using JasperFx;
using Marten;

namespace Cna.OrleansHost.Tests.Persistence;

// Resolves design doc §3.3's open item: whether the hash-chain's prior_hash/record_hash pair
// attaches via Marten's own event metadata/headers, or needs a companion table keyed by stream id
// + version. Verified against Marten 9.62.0's current API (martendb.io/events/metadata): event
// headers are an opt-in feature (MartenStoreFactory.CreateStore turns them on) set per-session via
// IDocumentSession.SetHeader before SaveChangesAsync, and read back off IEvent.Headers. This proof
// demonstrates the round trip; it does not wire prior_hash/record_hash into the real campaign
// event schema -- that is Phase C's job.
[Collection(PostgresCollectionDefinition.Name)]
public sealed class MartenEventHeaderHashChainTests
{
    private const string PriorHashHeaderKey = "prior_hash";
    private const string RecordHashHeaderKey = "record_hash";

    private readonly PostgresContainerFixture fixture;

    public MartenEventHeaderHashChainTests(PostgresContainerFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task HashChainPairRoundTripsAsEventHeaders()
    {
        const string priorHash = "sha256:0000000000000000000000000000000000000000000000000000000000000000";
        const string recordHash = "sha256:1111111111111111111111111111111111111111111111111111111111111111";

        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await fixture.CreateDatabaseAsync("sandtable_hashchain_poc");
        using var store = MartenStoreFactory.CreateStore(connectionString,
            options => options.AutoCreateSchemaObjects = AutoCreate.All);

        var streamId = Guid.NewGuid();
        await using (var session = store.LightweightSession())
        {
            session.Events.StartStream(streamId, new HashChainPocEventAppended("payload-1"));
            session.SetHeader(PriorHashHeaderKey, priorHash);
            session.SetHeader(RecordHashHeaderKey, recordHash);
            await session.SaveChangesAsync(cancellationToken);
        }

        await using var query = store.QuerySession();
        var events = await query.Events.FetchStreamAsync(streamId, token: cancellationToken);
        var appended = Assert.Single(events);

        Assert.NotNull(appended.Headers);
        Assert.Equal(priorHash, (string?)appended.Headers![PriorHashHeaderKey]);
        Assert.Equal(recordHash, (string?)appended.Headers![RecordHashHeaderKey]);
    }

    private sealed record HashChainPocEventAppended(string Payload);
}
