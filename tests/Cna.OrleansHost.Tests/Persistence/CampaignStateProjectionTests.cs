using System.Text;
using Cna.OrleansHost.Persistence;
using JasperFx;
using Marten;

namespace Cna.OrleansHost.Tests.Persistence;

// Phase C2 of issue #439 (docs/work/plans/2026-11-18-postgres-migration-design.md §3.2): proves
// CampaignStateProjection -- the Marten SingleStreamProjection<CampaignStateDocument, string>
// registered inline in CampaignEventStreamSchema.Configure -- actually folds the real
// CampaignAcceptedStepEvent envelope correctly against a real Postgres instance (not mocks, per
// the design doc's §8), and that it produces an identical result whether the stream was built in
// one continuous append (a from-genesis replay) or in two separate append batches with a gap
// between them (the shape a resumed import -- Phase B's CampaignHostAdapter.ImportResumed, PR
// #441 -- produces: a seed prefix committed first, the tail committed later). This is the
// "genesis vs. resume" equivalence this phase's task asks for, expressed at the Marten projection
// layer: the projection does not know or care whether a gap in wall-clock commit time separated
// two parts of the same logical stream, because SnapshotLifecycle.Inline folds strictly off the
// stream's own event order, not off when SaveChangesAsync happened to be called.
[Collection(PostgresCollectionDefinition.Name)]
public sealed class CampaignStateProjectionTests
{
    private const string GenesisHash = "sha256:0000000000000000000000000000000000000000000000000000000000000000";

    private readonly PostgresContainerFixture fixture;

    public CampaignStateProjectionTests(PostgresContainerFixture fixture) => this.fixture = fixture;

    private static DocumentStore BuildStore(string connectionString) =>
        MartenStoreFactory.CreateStore(connectionString, options =>
        {
            options.AutoCreateSchemaObjects = AutoCreate.All;
            CampaignEventStreamSchema.Configure(options);
        });

    private static byte[] Bytes(string content) => Encoding.UTF8.GetBytes(content);

    private static string RecordHashFor(long sequence) =>
        $"sha256:{sequence:D2}11111111111111111111111111111111111111111111111111111111111111";

    private static CampaignAcceptedStepEvent Step(long sequence) => new(
        ActionKind: sequence % 2 == 0 ? "combat-assault-resolved" : "element-moved",
        ActingElementId: $"elem-{sequence}",
        OwnerPublicRef: $"ref-{sequence}",
        SeatEpoch: 1,
        OwnerRevision: sequence,
        CanonicalCommand: Bytes($$"""{"eventType":"step","sequence":{{sequence}}}"""),
        CanonicalEvent: Bytes($$"""{"eventType":"step-event","sequence":{{sequence}}}"""),
        CanonicalReceipt: Bytes($$"""{"receipt":"step","sequence":{{sequence}}}"""),
        SnapshotCheckpoint: Bytes($$"""{"checkpoint":"after-step-{{sequence}}"}"""),
        CanonicalContacts: Bytes($$"""[{"contactId":"c{{sequence}}"}]"""));

    private static async Task AppendAsync(DocumentStore store, string campaignId,
        CampaignAcceptedStepEvent[] steps, long firstSequence, string priorHashBeforeFirst,
        CancellationToken cancellationToken)
    {
        var priorHash = priorHashBeforeFirst;
        for (var index = 0; index < steps.Length; index++)
        {
            var sequence = firstSequence + index;
            var recordHash = RecordHashFor(sequence);
            await using var session = store.LightweightSession();
            var built = CampaignEventStreamSchema.BuildStepEvent(store, steps[index], sequence, priorHash, recordHash);
            if (sequence == 1)
                session.Events.StartStream(campaignId, built);
            else
                session.Events.Append(campaignId, sequence, built);
            await session.SaveChangesAsync(cancellationToken);
            priorHash = recordHash;
        }
    }

    [Fact]
    public async Task ProjectionFoldsEveryAppendedStepInlineAndMatchesTheLatestOne()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await fixture.CreateDatabaseAsync("sandtable_c2_inline_fold");
        using var store = BuildStore(connectionString);
        const string campaignId = "campaign-c2-inline-001";

        var steps = Enumerable.Range(1, 5).Select(sequence => Step(sequence)).ToArray();
        await AppendAsync(store, campaignId, steps, 1, GenesisHash, cancellationToken);

        await using var query = store.QuerySession();
        var document = await query.LoadAsync<CampaignStateDocument>(campaignId, cancellationToken);

        Assert.NotNull(document);
        Assert.Equal(campaignId, document!.Id);
        Assert.Equal(5, document.Sequence);
        Assert.Equal(RecordHashFor(4), document.PriorHash);
        Assert.Equal(RecordHashFor(5), document.RecordHash);
        Assert.Equal(steps[4].ActionKind, document.ActionKind);
        Assert.Equal(steps[4].ActingElementId, document.ActingElementId);
        Assert.Equal(steps[4].SnapshotCheckpoint, document.SnapshotCheckpoint);
        Assert.Equal(steps[4].CanonicalContacts, document.CanonicalContacts);
    }

    [Fact]
    public async Task ProjectionBuiltFromGenesisInOneBatchMatchesOneBuiltAcrossASeedAndATailBatch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var steps = Enumerable.Range(1, 6).Select(sequence => Step(sequence)).ToArray();

        // "From genesis": every step appended in one continuous sequence of commits, the shape
        // CampaignHostAdapter.ImportStartedStream/Load() produces today for a fresh replay.
        var genesisConnectionString = await fixture.CreateDatabaseAsync("sandtable_c2_genesis");
        using var genesisStore = BuildStore(genesisConnectionString);
        const string genesisCampaignId = "campaign-c2-genesis-001";
        await AppendAsync(genesisStore, genesisCampaignId, steps, 1, GenesisHash, cancellationToken);

        // "Resumed": the first 2 steps committed as a seed batch (standing in for whatever earlier
        // session produced the seed CampaignStateDocument), then the remaining 4 committed later
        // as a separate batch -- the shape Phase B's CampaignHostAdapter.ImportResumed(seedSession,
        // tailSteps) produces when only the tail is replayed against an already-advanced stream.
        var resumedConnectionString = await fixture.CreateDatabaseAsync("sandtable_c2_resumed");
        using var resumedStore = BuildStore(resumedConnectionString);
        const string resumedCampaignId = "campaign-c2-resumed-001";
        const int seedCount = 2;
        await AppendAsync(resumedStore, resumedCampaignId, steps.Take(seedCount).ToArray(), 1, GenesisHash,
            cancellationToken);
        await AppendAsync(resumedStore, resumedCampaignId, steps.Skip(seedCount).ToArray(), seedCount + 1,
            RecordHashFor(seedCount), cancellationToken);

        await using var genesisQuery = genesisStore.QuerySession();
        await using var resumedQuery = resumedStore.QuerySession();
        var genesisDocument = await genesisQuery.LoadAsync<CampaignStateDocument>(genesisCampaignId, cancellationToken);
        var resumedDocument = await resumedQuery.LoadAsync<CampaignStateDocument>(resumedCampaignId, cancellationToken);

        Assert.NotNull(genesisDocument);
        Assert.NotNull(resumedDocument);
        Assert.Equal(genesisDocument!.Sequence, resumedDocument!.Sequence);
        Assert.Equal(genesisDocument.PriorHash, resumedDocument.PriorHash);
        Assert.Equal(genesisDocument.RecordHash, resumedDocument.RecordHash);
        Assert.Equal(genesisDocument.ActionKind, resumedDocument.ActionKind);
        Assert.Equal(genesisDocument.ActingElementId, resumedDocument.ActingElementId);
        Assert.Equal(genesisDocument.SnapshotCheckpoint, resumedDocument.SnapshotCheckpoint);
        Assert.Equal(genesisDocument.CanonicalContacts, resumedDocument.CanonicalContacts);
    }

    [Fact]
    public async Task DistinctCampaignsGetIndependentProjectionDocuments()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await fixture.CreateDatabaseAsync("sandtable_c2_isolation");
        using var store = BuildStore(connectionString);

        await AppendAsync(store, "campaign-c2-alpha", [Step(1)], 1, GenesisHash, cancellationToken);
        await AppendAsync(store, "campaign-c2-beta", [Step(1), Step(2)], 1, GenesisHash, cancellationToken);

        await using var query = store.QuerySession();
        var alpha = await query.LoadAsync<CampaignStateDocument>("campaign-c2-alpha", cancellationToken);
        var beta = await query.LoadAsync<CampaignStateDocument>("campaign-c2-beta", cancellationToken);

        Assert.NotNull(alpha);
        Assert.NotNull(beta);
        Assert.Equal(1, alpha!.Sequence);
        Assert.Equal(2, beta!.Sequence);
    }
}
