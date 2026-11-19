using System.Text;
using Cna.OrleansHost.Persistence;
using JasperFx;
using JasperFx.Events;
using Marten;

namespace Cna.OrleansHost.Tests.Persistence;

// Phase C1 of issue #439 (docs/work/plans/2026-11-18-postgres-migration-design.md): proves
// CampaignEventStreamSchema's envelope + header design round-trips a representative breadth of
// the real campaign event "shapes" found in CampaignCurrentEventSerializer.Serialize/Deserialize --
// a Movement event (element-moved, ElementMovedV3's real eventType/contractVersion), a
// Combat-result event (combat-assault-resolved, CampaignNativeCombatAssaultResultV22's), a
// #394-era second-side event (second-combat-selection-decided,
// CampaignNativeSecondCombatDecisionV47's), and a structurally unusual operator-only admission
// step (null ActingElementId/OwnerPublicRef/SeatEpoch/OwnerRevision and larger payloads -- the
// shape CampaignDurableAuthorityJournal.cs's migration PersistAccepted calls use, e.g.
// AdmitSourceAxisSupplement). CampaignAcceptedStepEvent deliberately treats
// CanonicalEvent/CanonicalCommand/etc. as opaque byte[] (see its own remarks on why: those real
// event CLR types are internal to Cna.Core with no InternalsVisibleTo grant to Cna.OrleansHost,
// and are hand-serialized in a shape default System.Text.Json reflection would not reproduce), so
// these tests use representative byte payloads tagged with the real eventType/contractVersion
// strings rather than constructing Cna.Core's actual (inaccessible) event types.
[Collection(PostgresCollectionDefinition.Name)]
public sealed class CampaignEventStreamSchemaTests
{
    private const string GenesisHash = "sha256:0000000000000000000000000000000000000000000000000000000000000000";

    private readonly PostgresContainerFixture fixture;

    public CampaignEventStreamSchemaTests(PostgresContainerFixture fixture) => this.fixture = fixture;

    private static DocumentStore BuildStore(string connectionString) =>
        MartenStoreFactory.CreateStore(connectionString, options =>
        {
            options.AutoCreateSchemaObjects = AutoCreate.All;
            CampaignEventStreamSchema.Configure(options);
        });

    private static byte[] Bytes(string content) => Encoding.UTF8.GetBytes(content);

    private static string RecordHashFor(long sequence) =>
        $"sha256:{sequence:D2}11111111111111111111111111111111111111111111111111111111111111";

    [Fact]
    public async Task EachRepresentativeEventShapeRoundTripsWithHeadersAndSequenceIntact()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await fixture.CreateDatabaseAsync("sandtable_c1_shapes");
        using var store = BuildStore(connectionString);
        const string campaignId = "campaign-c1-shapes-001";

        CampaignAcceptedStepEvent[] steps =
        [
            new CampaignAcceptedStepEvent(
                ActionKind: "element-moved",
                ActingElementId: "elem-1",
                OwnerPublicRef: "ref-1",
                SeatEpoch: 1,
                OwnerRevision: 1,
                CanonicalCommand: Bytes("""{"eventType":"element-moved","contractVersion":3,"command":"move"}"""),
                CanonicalEvent: Bytes("""{"eventType":"element-moved","contractVersion":3,"elementId":"elem-1"}"""),
                CanonicalReceipt: Bytes("""{"receipt":"element-moved"}"""),
                SnapshotCheckpoint: Bytes("""{"checkpoint":"after-element-moved"}"""),
                CanonicalContacts: Bytes("[]")),
            new CampaignAcceptedStepEvent(
                ActionKind: "combat-assault-resolved",
                ActingElementId: "elem-2",
                OwnerPublicRef: "ref-2",
                SeatEpoch: 2,
                OwnerRevision: 1,
                CanonicalCommand: Bytes("""{"eventType":"combat-assault-resolved","contractVersion":22,"command":"assault"}"""),
                CanonicalEvent: Bytes("""{"eventType":"combat-assault-resolved","contractVersion":22,"result":"attacker-eliminated"}"""),
                CanonicalReceipt: Bytes("""{"receipt":"combat-assault-resolved"}"""),
                SnapshotCheckpoint: Bytes("""{"checkpoint":"after-assault"}"""),
                CanonicalContacts: Bytes("""[{"contactId":"c1"}]""")),
            new CampaignAcceptedStepEvent(
                ActionKind: "second-combat-selection-decided",
                ActingElementId: "elem-3",
                OwnerPublicRef: null,
                SeatEpoch: 2,
                OwnerRevision: 2,
                CanonicalCommand: Bytes("""{"eventType":"second-combat-selection-decided","contractVersion":47,"command":"select"}"""),
                CanonicalEvent: Bytes("""{"eventType":"second-combat-selection-decided","contractVersion":47,"choice":"attack"}"""),
                CanonicalReceipt: Bytes("""{"receipt":"second-combat-selection-decided"}"""),
                SnapshotCheckpoint: Bytes("""{"checkpoint":"after-second-combat-selection"}"""),
                CanonicalContacts: Bytes("[]")),
            new CampaignAcceptedStepEvent(
                ActionKind: "source-axis-supplement-admitted",
                ActingElementId: null,
                OwnerPublicRef: null,
                SeatEpoch: null,
                OwnerRevision: null,
                CanonicalCommand: Bytes("""{"eventType":"source-axis-supplement-admitted","operator":true}"""),
                CanonicalEvent: Bytes(new string('x', 4096)),
                CanonicalReceipt: Bytes("""{"receipt":"source-axis-supplement-admitted"}"""),
                SnapshotCheckpoint: Bytes(new string('y', 8192)),
                CanonicalContacts: Bytes("[]")),
        ];

        var priorHash = GenesisHash;
        for (var index = 0; index < steps.Length; index++)
        {
            var sequence = index + 1L;
            var recordHash = RecordHashFor(sequence);
            await using var session = store.LightweightSession();
            var built = CampaignEventStreamSchema.BuildStepEvent(store, steps[index], sequence, priorHash, recordHash);
            if (sequence == 1)
                session.Events.StartStream(campaignId, built);
            else
                // Verified empirically (see CampaignEventStreamSchema's remarks): expectedVersion
                // here is the stream's version AFTER this append, i.e. exactly `sequence` -- not
                // `sequence - 1`, despite what Append's own XML doc comment implies.
                session.Events.Append(campaignId, sequence, built);
            await session.SaveChangesAsync(cancellationToken);
            priorHash = recordHash;
        }

        await using var query = store.QuerySession();
        var state = await query.Events.FetchStreamStateAsync(campaignId, cancellationToken);
        Assert.NotNull(state);
        // Design doc remark (CampaignEventStreamSchema): Marten's own per-stream Version, assigned
        // automatically, is expected to exactly match the explicit sequence header written
        // alongside it -- proven here, not assumed.
        Assert.Equal(steps.Length, state!.Version);

        var events = await query.Events.FetchStreamAsync(campaignId, token: cancellationToken);
        Assert.Equal(steps.Length, events.Count);

        for (var index = 0; index < steps.Length; index++)
        {
            var loadedEvent = events[index];
            var sequence = index + 1L;
            Assert.Equal(sequence, loadedEvent.Version);

            var (headerSequence, headerPriorHash, headerRecordHash) =
                CampaignEventStreamSchema.ReadStepHeaders(loadedEvent);
            Assert.Equal(sequence, headerSequence);
            Assert.Equal(sequence == 1 ? GenesisHash : RecordHashFor(sequence - 1), headerPriorHash);
            Assert.Equal(RecordHashFor(sequence), headerRecordHash);

            var loaded = Assert.IsType<CampaignAcceptedStepEvent>(loadedEvent.Data);
            var original = steps[index];
            Assert.Equal(original.ActionKind, loaded.ActionKind);
            Assert.Equal(original.ActingElementId, loaded.ActingElementId);
            Assert.Equal(original.OwnerPublicRef, loaded.OwnerPublicRef);
            Assert.Equal(original.SeatEpoch, loaded.SeatEpoch);
            Assert.Equal(original.OwnerRevision, loaded.OwnerRevision);
            Assert.Equal(original.CanonicalCommand, loaded.CanonicalCommand);
            Assert.Equal(original.CanonicalEvent, loaded.CanonicalEvent);
            Assert.Equal(original.CanonicalReceipt, loaded.CanonicalReceipt);
            Assert.Equal(original.SnapshotCheckpoint, loaded.SnapshotCheckpoint);
            Assert.Equal(original.CanonicalContacts, loaded.CanonicalContacts);
        }
    }

    [Fact]
    public async Task DistinctCampaignIdsGetIndependentStringKeyedStreams()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await fixture.CreateDatabaseAsync("sandtable_c1_isolation");
        using var store = BuildStore(connectionString);

        var first = new CampaignAcceptedStepEvent("element-moved", "elem-1", "ref-1", 1, 1,
            Bytes("{}"), Bytes("""{"eventType":"element-moved"}"""), Bytes("{}"), Bytes("{}"), Bytes("[]"));
        var second = new CampaignAcceptedStepEvent("combat-assault-resolved", "elem-9", "ref-9", 1, 1,
            Bytes("{}"), Bytes("""{"eventType":"combat-assault-resolved"}"""), Bytes("{}"), Bytes("{}"), Bytes("[]"));

        await using (var session = store.LightweightSession())
        {
            session.Events.StartStream("campaign-c1-alpha",
                CampaignEventStreamSchema.BuildStepEvent(store, first, 1, GenesisHash, RecordHashFor(1)));
            await session.SaveChangesAsync(cancellationToken);
        }
        await using (var session = store.LightweightSession())
        {
            session.Events.StartStream("campaign-c1-beta",
                CampaignEventStreamSchema.BuildStepEvent(store, second, 1, GenesisHash, RecordHashFor(1)));
            await session.SaveChangesAsync(cancellationToken);
        }

        await using var query = store.QuerySession();
        var alphaEvents = await query.Events.FetchStreamAsync("campaign-c1-alpha", token: cancellationToken);
        var betaEvents = await query.Events.FetchStreamAsync("campaign-c1-beta", token: cancellationToken);

        Assert.Single(alphaEvents);
        Assert.Single(betaEvents);
        Assert.Equal("element-moved", Assert.IsType<CampaignAcceptedStepEvent>(alphaEvents[0].Data).ActionKind);
        Assert.Equal("combat-assault-resolved", Assert.IsType<CampaignAcceptedStepEvent>(betaEvents[0].Data).ActionKind);
    }

    /// <summary>
    /// Regression for the C1 header bug fixed in Phase C3b: C1's session-scoped
    /// <c>SetStepHeaders</c> made Marten copy one header set onto every event of a save, so a unit of
    /// work appending several steps (e.g. a timeout cascade) stored a duplicated <c>sequence</c> and a
    /// broken <c>prior_hash</c>/<c>record_hash</c> chain. Three steps appended in ONE save must each
    /// keep their own headers.
    /// </summary>
    [Fact]
    public async Task SeveralStepsAppendedInOneSaveEachKeepTheirOwnHeaders()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await fixture.CreateDatabaseAsync("sandtable_c3b_multi_step_save");
        using var store = BuildStore(connectionString);
        const string campaignId = "campaign-c3b-multi-001";
        var steps = Enumerable.Range(1, 3).Select(index => new CampaignAcceptedStepEvent($"step-{index}", null, null,
            null, null, Bytes("{}"), Bytes($$"""{"eventType":"step-{{index}}"}"""), Bytes("{}"), Bytes("{}"),
            Bytes("[]"))).ToArray();

        await using (var session = store.LightweightSession())
        {
            session.Events.StartStream(campaignId,
                CampaignEventStreamSchema.BuildStepEvent(store, steps[0], 1, GenesisHash, RecordHashFor(1)),
                CampaignEventStreamSchema.BuildStepEvent(store, steps[1], 2, RecordHashFor(1), RecordHashFor(2)),
                CampaignEventStreamSchema.BuildStepEvent(store, steps[2], 3, RecordHashFor(2), RecordHashFor(3)));
            await session.SaveChangesAsync(cancellationToken);
        }

        await using var query = store.QuerySession();
        var events = await query.Events.FetchStreamAsync(campaignId, token: cancellationToken);
        Assert.Equal(3, events.Count);
        var prior = GenesisHash;
        for (var index = 0; index < events.Count; index++)
        {
            var (sequence, priorHash, recordHash) = CampaignEventStreamSchema.ReadStepHeaders(events[index]);
            Assert.Equal(index + 1L, sequence);
            Assert.Equal(prior, priorHash);
            Assert.Equal(RecordHashFor(index + 1L), recordHash);
            prior = recordHash;
        }
        var state = await query.LoadAsync<CampaignStateDocument>(campaignId, cancellationToken);
        Assert.Equal(3, state!.Sequence);
        Assert.Equal(RecordHashFor(3), state.RecordHash);
    }
}
