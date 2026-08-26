using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignReplayPreparationTests
{
    [Theory]
    [InlineData(0, 12345)]
    [InlineData(1, 7)]
    public void CanonicalCompleteHistoryFreshReplaysWithIdenticalActions(
        int setupIndex,
        int seed)
    {
        var evidence = StageEntryCampaignTestData.Execute(
            Cna1979SetupCatalog.Definitions[setupIndex],
            (ulong)seed,
            InitiativeOrderChoice.ActLast);
        var eventBytes = evidence.Events
            .Select(CampaignEventSerializer.Serialize)
            .ToArray();
        var preparation = CampaignReplayPreparation.Prepare(
            eventBytes[0],
            Cna1979SyntheticContentResolver.Instance);
        Assert.True(preparation.IsPrepared);
        var freshEvents = eventBytes
            .Select(bytes => CampaignEventSerializer.Deserialize(bytes))
            .ToArray();

        Assert.Equal(eventBytes, freshEvents.Select(CampaignEventSerializer.Serialize));

        for (var count = 1; count <= evidence.Events.Count; count++)
        {
            var original = CampaignProjector.Replay(
                evidence.Events.Take(count),
                evidence.Context);
            var fresh = CampaignProjector.Replay(
                freshEvents.Take(count),
                preparation.Context!.Content);

            Assert.Equal(
                CampaignSnapshotSerializer.Serialize(original),
                CampaignSnapshotSerializer.Serialize(fresh));

            foreach (var audience in Enum.GetValues<CampaignActionAudience>())
            {
                var originalActions = CampaignLegalActions.Query(
                    new CampaignAuthorityHandle(original, evidence.Context),
                    audience);
                var freshActions = CampaignLegalActions.Query(
                    new CampaignAuthorityHandle(fresh, preparation.Context.Content),
                    audience);
                Assert.True(originalActions.IsSuccessful);
                Assert.True(freshActions.IsSuccessful);
                Assert.Equal(
                    CampaignLegalActionSerializer.Serialize(originalActions.ActionSet!),
                    CampaignLegalActionSerializer.Serialize(freshActions.ActionSet!));
            }
        }
    }

    [Fact]
    public void ExactCreationBytesPrepareReplayContextAndProjectCanonically()
    {
        var created = CreateEvent();
        var bytes = CampaignEventSerializer.Serialize(created);

        var result = CampaignReplayPreparation.Prepare(
            bytes,
            Cna1979SyntheticContentResolver.Instance);

        Assert.True(result.IsPrepared);
        Assert.Equal(CampaignReplayPreparationRejectionReason.None, result.RejectionReason);
        Assert.NotNull(result.Context);
        Assert.Equal(created.RulesetHash, result.Context.RulesetHash);
        Assert.Equal(created.Setup.Content, result.Context.Content.Selection);
        var expected = CampaignTestHarness.Replay([created]);
        var projected = CampaignProjector.Replay([created], result.Context.Content);
        Assert.Equal(created.Setup.StageEntry, projected.Setup.StageEntry);
        Assert.Equal(expected, projected);
        Assert.Equal(8, projected.ContractVersion);
    }

    [Fact]
    public void CreationAndSnapshotBytesRequireExactEmbeddedStageEntryPolicy()
    {
        var created = CreateEvent();
        var projected = CampaignTestHarness.Replay([created]);
        var creationJson = Encoding.UTF8.GetString(
            CampaignEventSerializer.Serialize(created));
        var snapshotJson = Encoding.UTF8.GetString(
            CampaignSnapshotSerializer.Serialize(projected));
        var policyJson = Encoding.UTF8.GetString(
            CampaignStageEntryPolicyCodec.SerializeCanonical(created.Setup.StageEntry));
        var embeddedPolicy = $"\"stageEntry\":{policyJson},";

        Assert.Contains(
            $"\"weather\":{{{GetWeatherBody(created)}}},{embeddedPolicy}\"content\":",
            creationJson,
            StringComparison.Ordinal);
        Assert.Equal(
            created.Setup.StageEntry,
            CampaignSnapshotSerializer.Deserialize(
                Encoding.UTF8.GetBytes(snapshotJson)).Setup.StageEntry);

        string[] invalidCreation =
        [
            creationJson.Replace(embeddedPolicy, string.Empty, StringComparison.Ordinal),
            creationJson.Replace(
                "\"organization\":\"explicit-none\"",
                "\"organization\":\"has-obligations\"",
                StringComparison.Ordinal),
            creationJson.Replace(
                $"\"gameTurn\":{created.Setup.StageEntry.GameTurn},\"operationStage\":1",
                $"\"gameTurn\":{created.Setup.StageEntry.GameTurn + 1},\"operationStage\":1",
                StringComparison.Ordinal),
            creationJson.Replace(
                $"\"gameTurn\":{created.Setup.StageEntry.GameTurn},\"operationStage\":1",
                $"\"operationStage\":1,\"gameTurn\":{created.Setup.StageEntry.GameTurn}",
                StringComparison.Ordinal),
            creationJson.Replace(
                "stage-entry.no-obligations.v1",
                "stage-entry.wrong.v1",
                StringComparison.Ordinal),
        ];

        Assert.All(invalidCreation, json =>
        {
            var resolver = new CountingResolver();
            var result = CampaignReplayPreparation.Prepare(
                Encoding.UTF8.GetBytes(json),
                resolver);
            Assert.Equal(
                CampaignReplayPreparationRejectionReason.InvalidHistory,
                result.RejectionReason);
            Assert.Equal(0, resolver.CallCount);
        });

        string[] invalidSnapshots =
        [
            snapshotJson.Replace(embeddedPolicy, string.Empty, StringComparison.Ordinal),
            snapshotJson.Replace(
                "\"organization\":\"explicit-none\"",
                "\"organization\":\"has-obligations\"",
                StringComparison.Ordinal),
            snapshotJson.Replace(
                "{\"contractVersion\":8,",
                "{\"contractVersion\":7,",
                StringComparison.Ordinal),
        ];

        Assert.All(invalidSnapshots, json => Assert.Throws<JsonException>(() =>
            CampaignSnapshotSerializer.Deserialize(Encoding.UTF8.GetBytes(json))));
    }

    [Fact]
    public void ExactCatalogCheckpointStatesOneThroughTenAreLocallyValid()
    {
        var snapshots = ReachOrganizationSnapshots();
        var organization = snapshots[^1];
        var positions = Cna1979LandSequence.CreateTurn(organization.GameTurn);

        snapshots.AddRange(Enumerable.Range(7, 4).Select(stateVersion =>
            organization with
            {
                StateVersion = stateVersion,
                SequencePosition = positions[stateVersion - 1],
            }));

        Assert.Equal(Enumerable.Range(1, 10).Select(value => (long)value),
            snapshots.Select(snapshot => snapshot.StateVersion));
        Assert.All(snapshots, snapshot => Assert.True(
            CampaignSnapshotValidator.IsValid(
                snapshot,
                CampaignTestHarness.ContextFor(snapshot))));

        var forged = snapshots[^1] with { SequencePosition = positions[8] };
        Assert.False(CampaignSnapshotValidator.IsLocallyValid(forged));
    }

    [Fact]
    public void SelfConsistentRehashedHybridSetupRejectsBeforeContentResolution()
    {
        var created = CreateEvent();
        var setup = created.Setup;
        var hybridHash = CampaignSetupHash.Calculate(
            setup.SchemaVersion,
            setup.SetupId,
            false,
            setup.InitialGameTurn,
            setup.InitialInitiative,
            setup.OpeningPreamble,
            setup.Weather,
            setup.StageEntry,
            setup.Content,
            setup.Sources);
        var hybrid = new CampaignSetupSnapshot(
            setup.SchemaVersion,
            setup.SetupId,
            hybridHash,
            false,
            setup.InitialGameTurn,
            setup.InitialInitiative,
            setup.OpeningPreamble,
            setup.Weather,
            setup.StageEntry,
            setup.Content,
            setup.Sources);
        var canonicalCreation = Encoding.UTF8.GetString(
            CampaignEventSerializer.Serialize(created));
        var forgedCreation = canonicalCreation
            .Replace(setup.SetupHash, hybridHash, StringComparison.Ordinal)
            .Replace("\"isSynthetic\":true", "\"isSynthetic\":false",
                StringComparison.Ordinal);
        var resolver = new CountingResolver();

        var preparation = CampaignReplayPreparation.Prepare(
            Encoding.UTF8.GetBytes(forgedCreation),
            resolver);
        var forgedSnapshot = CampaignTestHarness.Replay([created]) with { Setup = hybrid };

        Assert.Equal(
            CampaignReplayPreparationRejectionReason.InvalidHistory,
            preparation.RejectionReason);
        Assert.Equal(0, resolver.CallCount);
        Assert.False(CampaignSnapshotValidator.IsLocallyValid(forgedSnapshot));
    }

    [Fact]
    public void MalformedCreationBytesMapToInvalidHistoryWhileDirectReadThrows()
    {
        var malformed = Encoding.UTF8.GetBytes("{not-json");

        Assert.ThrowsAny<System.Text.Json.JsonException>(
            () => CampaignEventSerializer.Deserialize(malformed));

        var result = CampaignReplayPreparation.Prepare(
            malformed,
            Cna1979SyntheticContentResolver.Instance);

        Assert.False(result.IsPrepared);
        Assert.Null(result.Context);
        Assert.Equal(
            CampaignReplayPreparationRejectionReason.InvalidHistory,
            result.RejectionReason);
    }

    [Fact]
    public void NonIntegerCreationMetadataMapsToInvalidHistory()
    {
        var canonical = Encoding.UTF8.GetString(
            CampaignEventSerializer.Serialize(CreateEvent()));
        var malformed = Encoding.UTF8.GetBytes(canonical.Replace(
            "\"stateVersion\":1,",
            "\"stateVersion\":1.5,",
            StringComparison.Ordinal));

        var result = CampaignReplayPreparation.Prepare(
            malformed,
            Cna1979SyntheticContentResolver.Instance);

        Assert.False(result.IsPrepared);
        Assert.Null(result.Context);
        Assert.Equal(
            CampaignReplayPreparationRejectionReason.InvalidHistory,
            result.RejectionReason);
    }

    [Theory]
    [InlineData(ContentCatalogRejectionReason.UnknownPackId, (int)CampaignReplayPreparationRejectionReason.MissingContent)]
    [InlineData(ContentCatalogRejectionReason.HashMismatch, (int)CampaignReplayPreparationRejectionReason.ContentHashMismatch)]
    public void ExactContentResolutionFailuresAreTyped(
        ContentCatalogRejectionReason catalogReason,
        int expected)
    {
        var bytes = CampaignEventSerializer.Serialize(CreateEvent());

        var result = CampaignReplayPreparation.Prepare(
            bytes,
            new RejectingResolver(catalogReason));

        Assert.False(result.IsPrepared);
        Assert.Null(result.Context);
        Assert.Equal((CampaignReplayPreparationRejectionReason)expected, result.RejectionReason);
    }

    [Fact]
    public void WellFormedButUnsupportedRulesetHashIsTypedBeforeContentResolution()
    {
        var created = CreateEvent();
        var canonical = Encoding.UTF8.GetString(CampaignEventSerializer.Serialize(created));
        var unsupported = canonical.Replace(
            created.RulesetHash,
            new string('0', 64),
            StringComparison.Ordinal);
        var resolver = new CountingResolver();

        var result = CampaignReplayPreparation.Prepare(
            Encoding.UTF8.GetBytes(unsupported),
            resolver);

        Assert.Equal(
            CampaignReplayPreparationRejectionReason.UnsupportedRuleset,
            result.RejectionReason);
        Assert.Equal(0, resolver.CallCount);
    }

    [Fact]
    public void AlteredWeatherPolicyMapsToInvalidHistoryBeforeContentResolution()
    {
        var canonical = Encoding.UTF8.GetString(
            CampaignEventSerializer.Serialize(CreateEvent()));
        var altered = canonical.Replace(
            "weather.no-immediate-effect-subjects.v1",
            "weather.wrong.v1",
            StringComparison.Ordinal);
        var resolver = new CountingResolver();

        var result = CampaignReplayPreparation.Prepare(
            Encoding.UTF8.GetBytes(altered),
            resolver);

        Assert.Equal(
            CampaignReplayPreparationRejectionReason.InvalidHistory,
            result.RejectionReason);
        Assert.Equal(0, resolver.CallCount);
    }

    private static CampaignCreated CreateEvent()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var result = CampaignEngine.DecideCreation(
            null,
            new CreateCampaign(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash,
                setup.Content.Pack.PackId,
                setup.Content.Pack.Hash,
                setup.Content.ScenarioId),
            Cna1979SyntheticContentResolver.Instance);
        return Assert.IsType<CampaignCreated>(Assert.Single(result.Events));
    }

    private static List<CampaignSnapshot> ReachOrganizationSnapshots()
    {
        var created = CreateEvent();
        var snapshots = new List<CampaignSnapshot>();
        var history = new List<CampaignEvent> { created };
        var snapshot = CampaignTestHarness.Replay(history);
        snapshots.Add(snapshot);

        CampaignCommand[] openingCommands =
        [
            new ResolveInitiative(1, snapshot.SequencePosition.PositionId),
            new ResolveNoObligationNavalConvoySchedule(
                2,
                "land.position.naval-convoy.schedule"),
            new ResolveNoObligationTacticalShipping(
                3,
                "land.position.naval-convoy.tactical-shipping"),
        ];

        foreach (var command in openingCommands)
        {
            var campaignEvent = Assert.Single(
                CampaignTestHarness.Decide(snapshot, command).Events);
            history.Add(campaignEvent);
            snapshot = CampaignTestHarness.Replay(history);
            snapshots.Add(snapshot);
        }

        var declaration = Assert.Single(CampaignTestHarness.Decide(
            snapshot,
            new DeclareInitiativeOrder(
                4,
                snapshot.SequencePosition.PositionId,
                1,
                snapshot.InitiativeHolder!.Value,
                InitiativeOrderChoice.ActFirst)).Events);
        history.Add(declaration);
        snapshot = CampaignTestHarness.Replay(history);
        snapshots.Add(snapshot);

        var weather = Assert.Single(CampaignTestHarness.Decide(
            snapshot,
            new ResolveWeather(5, snapshot.SequencePosition.PositionId)).Events);
        history.Add(weather);
        snapshots.Add(CampaignTestHarness.Replay(history));
        return snapshots;
    }

    private static string GetWeatherBody(CampaignCreated created)
    {
        var creationJson = Encoding.UTF8.GetString(
            CampaignEventSerializer.Serialize(created));
        var start = creationJson.IndexOf("\"weather\":{", StringComparison.Ordinal)
            + "\"weather\":{".Length;
        var end = creationJson.IndexOf("},\"stageEntry\":", start, StringComparison.Ordinal);
        return creationJson[start..end];
    }

    private sealed class RejectingResolver(ContentCatalogRejectionReason reason)
        : IContentPackResolver
    {
        public ContentCatalogResolution Resolve(string packId, string expectedHash) =>
            reason == ContentCatalogRejectionReason.UnknownPackId
                ? Cna1979SyntheticContentCatalog.Resolve("unknown-pack", expectedHash)
                : Cna1979SyntheticContentCatalog.Resolve(
                    packId,
                    $"sha256:{new string('0', 64)}");
    }

    private sealed class CountingResolver : IContentPackResolver
    {
        public int CallCount { get; private set; }

        public ContentCatalogResolution Resolve(string packId, string expectedHash)
        {
            CallCount++;
            return Cna1979SyntheticContentCatalog.Resolve(packId, expectedHash);
        }
    }
}
