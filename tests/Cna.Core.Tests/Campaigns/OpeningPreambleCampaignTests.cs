using System.Security.Cryptography;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class OpeningPreambleCampaignTests
{
    [Theory]
    [InlineData((int)InitiativeOrderChoice.ActFirst, LandSide.Axis, LandSide.Commonwealth)]
    [InlineData((int)InitiativeOrderChoice.ActLast, LandSide.Commonwealth, LandSide.Axis)]
    public void MechanicsAdvanceInSourceOrderAndReplayCanonically(
        int choiceValue,
        LandSide expectedFirst,
        LandSide expectedSecond)
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        CampaignCommand[] commands =
        [
            CampaignTestHarness.Create("campaign-preamble", Cna1979Ruleset.Manifest.Hash, 12345,
                setup.SetupId, setup.Hash),
            new ResolveInitiative(1, "land.position.initiative-determination"),
            new ResolveNoObligationNavalConvoySchedule(2, "land.position.naval-convoy.schedule"),
            new ResolveNoObligationTacticalShipping(3, "land.position.naval-convoy.tactical-shipping"),
            new DeclareInitiativeOrder(4, "land.position.operation-1.initiative-declaration", 1,
                LandSide.Axis, (InitiativeOrderChoice)choiceValue),
        ];

        var execution = CampaignTestHarness.Execute(commands);

        Assert.True(execution.IsAccepted);
        Assert.Collection(execution.Events,
            value => Assert.IsType<CampaignCreated>(value),
            value => Assert.IsType<InitiativeDetermined>(value),
            value => Assert.IsType<NoObligationNavalConvoyScheduleResolved>(value),
            value => Assert.IsType<NoObligationTacticalShippingResolved>(value),
            value => Assert.IsType<InitiativeOrderDeclared>(value));
        var snapshot = execution.Snapshot!;
        Assert.Equal(5, snapshot.StateVersion);
        Assert.Equal(LandPhaseIds.WeatherDetermination, snapshot.PhaseId);
        Assert.Equal(0UL, snapshot.RandomState.NextByteCursor);
        var order = Assert.Single(snapshot.OperationStageOrders);
        Assert.Equal(1, order.GameTurn);
        Assert.Equal(1, order.OperationStage);
        Assert.Equal(expectedFirst, order.FirstSide);
        Assert.Equal(expectedSecond, order.SecondSide);
        Assert.Equal(LandSide.Axis, snapshot.InitiativeHolder);

        var canonicalEvents = execution.Events.Select(CampaignEventSerializer.Serialize).ToArray();
        var canonicalSnapshot = System.Text.Encoding.UTF8.GetString(
            CampaignSnapshotSerializer.Serialize(snapshot));
        Assert.Contains(
            "\"operationStageOrders\":[{\"contractVersion\":2,\"gameTurn\":1," +
            "\"operationStage\":1",
            canonicalSnapshot,
            StringComparison.Ordinal);
        var roundTripped = canonicalEvents
            .Select(bytes => CampaignEventSerializer.Deserialize(bytes))
            .ToArray();
        var replayed = CampaignTestHarness.Replay(roundTripped);
        Assert.Equal(CampaignSnapshotSerializer.Serialize(snapshot),
            CampaignSnapshotSerializer.Serialize(replayed));
    }

    [Fact]
    public void ConvoyProjectionRejectsAlteredPolicyProvenance()
    {
        var initial = CreateInitial();
        var initiative = CampaignTestHarness.Decide(initial,
            new ResolveInitiative(1, initial.SequencePosition.PositionId));
        var schedule = CampaignTestHarness.Apply(initial, Assert.Single(initiative.Events));
        var decision = CampaignTestHarness.Decide(schedule,
            new ResolveNoObligationNavalConvoySchedule(2, schedule.SequencePosition.PositionId));
        var valid = Assert.IsType<NoObligationNavalConvoyScheduleResolved>(Assert.Single(decision.Events));
        var forged = new NoObligationNavalConvoyScheduleResolved(valid.CampaignId,
            valid.StateVersion, valid.FromPositionId, valid.SequencePosition,
            [new RuleReference("forged", "source")]);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(schedule, forged));
    }

    [Fact]
    public void MissingOrResourcedOpeningPolicyIsInvalidAuthority()
    {
        var snapshot = CreateInitial();
        var policy = new CampaignOpeningPreamblePolicy(1,
            CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations,
            [new RuleReference("sandtable-rules-lab", "wrong.v1")]);
        var definition = Cna1979SetupCatalog.Definitions[0];
        var changed = new CampaignSetupDefinition(definition.SchemaVersion, definition.SetupId,
            definition.DisplayName, definition.IsSynthetic, definition.InitialGameTurn,
            definition.InitialInitiative, policy, definition.Weather, definition.StageEntry,
            definition.Content, definition.Sources);
        var invalid = snapshot with { Setup = CampaignSetupSnapshot.FromDefinition(changed) };

        var result = CampaignEngine.Decide(invalid,
            new ResolveInitiative(1, invalid.SequencePosition.PositionId),
            CampaignTestHarness.ContextFor(invalid));
        Assert.Equal(CampaignCommandRejectionReason.InvalidState, result.RejectionReason);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void NewPreambleEventsHaveStableCanonicalHashes()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        CampaignCommand[] commands =
        [
            CampaignTestHarness.Create("campaign-preamble", Cna1979Ruleset.Manifest.Hash, 12345,
                setup.SetupId, setup.Hash),
            new ResolveInitiative(1, "land.position.initiative-determination"),
            new ResolveNoObligationNavalConvoySchedule(2, "land.position.naval-convoy.schedule"),
            new ResolveNoObligationTacticalShipping(3, "land.position.naval-convoy.tactical-shipping"),
            new DeclareInitiativeOrder(4, "land.position.operation-1.initiative-declaration", 1,
                LandSide.Axis, InitiativeOrderChoice.ActFirst),
        ];
        var events = CampaignTestHarness.Execute(commands).Events.Skip(2);
        var hashes = events.Select(value => Convert.ToHexStringLower(
            SHA256.HashData(CampaignEventSerializer.Serialize(value)))).ToArray();

        Assert.Equal(
            [
                "1e52dc704e61df2ee70f1aa59703ca934b50fb78a9c706d041734878aaea4d01",
                "716ba5b8cd23029c2abda264e457a0f060f1abb23b33a54b5346b422060d57cd",
                "6e8cb94a31e429ed205feac46acffd77a9776e0c13000b2c6ccf674be52b380c",
            ],
            hashes);
    }

    private static CampaignSnapshot CreateInitial()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var creation = CampaignTestHarness.Decide(null,
            CampaignTestHarness.Create("campaign-preamble", Cna1979Ruleset.Manifest.Hash, 12345,
                setup.SetupId, setup.Hash));
        return CampaignTestHarness.Replay(creation.Events);
    }
}
