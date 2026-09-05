using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownPreambleReplayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreationPreambleAndReserveReplayToExactMovement(bool reserve)
    {
        var artifact = BreakdownContentFixture.Artifact(); var scenario = BreakdownSnapshotTests.Scenario();
        var created = Create();
        var initial = CampaignCreationV10Factory.CreateSnapshot(created, artifact, scenario);
        var live = initial; var history = new List<byte[]>();
        foreach (var action in Actions(reserve))
        {
            var value = CampaignV11Preamble.Create(live, artifact, scenario, action);
            var bytes = CampaignV11PreambleCodec.Serialize(value);
            history.Add(bytes);
            Assert.Contains("\"contractVersion\":4", Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
            live = CampaignV11Preamble.Apply(live, value, artifact, scenario);
            Assert.True(CampaignSnapshotV11Admission.IsValid(live, artifact, scenario));
        }
        Assert.Equal(LandSegmentIds.Movement, live.CurrentPosition.SequenceContext.SegmentId);
        Assert.NotNull(live.CurrentPosition.SequenceContext.ActiveSide);
        Assert.IsType<CampaignBreakdownFlow.Idle>(live.BreakdownFlow);
        Assert.Empty(live.World.BrokenVehicleLots);
        var replay = initial;
        foreach (var bytes in history)
        {
            var value = CampaignV11PreambleCodec.Deserialize(bytes);
            Assert.Equal(bytes, CampaignV11PreambleCodec.Serialize(value));
            replay = CampaignV11Preamble.Apply(replay, value, artifact, scenario);
        }
        Assert.Equal(CampaignSnapshotV11Serializer.Serialize(live), CampaignSnapshotV11Serializer.Serialize(replay));
    }

    [Fact]
    public void InitialCheckpointCannotSkipToWeather()
    {
        var artifact = BreakdownContentFixture.Artifact(); var scenario = BreakdownSnapshotTests.Scenario();
        var initial = CampaignCreationV10Factory.CreateSnapshot(Create(), artifact, scenario);
        Assert.Throws<InvalidOperationException>(() => CampaignV11Preamble.Create(initial, artifact, scenario, new ResolveWeatherAction()));
    }

    [Fact]
    public void InitialCheckpointIsAdmittedButFabricatedProgressIsNot()
    {
        var artifact = BreakdownContentFixture.Artifact(); var scenario = BreakdownSnapshotTests.Scenario();
        var initial = CampaignCreationV10Factory.CreateSnapshot(Create(), artifact, scenario);
        Assert.True(CampaignSnapshotV11Admission.IsValid(initial, artifact, scenario));
        var forged = new CampaignSnapshotV11(11, initial.CampaignId, 2, initial.RulesetHash, initial.Setup,
            initial.World, null, [], [], initial.RandomState, initial.CurrentPosition, null, initial.BreakdownFlow);
        Assert.False(CampaignSnapshotV11Admission.IsValid(forged, artifact, scenario));
    }

    [Theory]
    [InlineData("initiative")]
    [InlineData("weather")]
    [InlineData("cursor")]
    public void CurrentCheckpointRecomputesRetainedPreambleEvidence(string mutation)
    {
        var current = BreakdownPublicActivationTests.ReachMovement().CurrentSnapshot!;
        var holder = mutation == "initiative" ? LandSide.Commonwealth : current.InitiativeHolder;
        var retained = current.OperationStageWeather.Single();
        var weather = mutation == "weather" ? new[] { new CampaignOperationStageWeather(
            retained.ContractVersion, retained.GameTurn, retained.OperationStage, LandSide.Commonwealth,
            retained.Season, retained.FirstDie, retained.SecondDie, retained.Kind, retained.Scope,
            retained.LocationDie, retained.AffectedAreas, 0, 0, 0) } : current.OperationStageWeather;
        var rng = mutation == "cursor" ? new RandomStreamState(current.RandomState.ContractVersion,
            current.RandomState.AlgorithmId, current.RandomState.Seed, current.RandomState.NextByteCursor + 1)
            : current.RandomState;
        var forged = new CampaignSnapshotV11(11, current.CampaignId, current.StateVersion,
            current.RulesetHash, current.Setup, current.World, holder, current.OperationStageOrders,
            weather, rng, current.CurrentPosition, current.ReactionWindow, current.BreakdownFlow);
        var canonical = CampaignSnapshotV11Serializer.Serialize(forged);
        Assert.Throws<System.Text.Json.JsonException>(() =>
            Cna.Core.Exercises.CampaignExercises.ReadCheckpoint(canonical));
    }

    internal static CampaignCreatedV10 Create()
    {
        var artifact = Cna.Core.Content.Cna1979SyntheticContentCatalog.ArtifactV6;
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        return CampaignCreationV10Factory.Create("breakdown-campaign", Cna1979BreakdownRuleset.Manifest.Hash,
            setup, artifact, artifact.Definition.LegacyDefinition.Scenarios.Single(), SandtableRandom.Create(0),
            Cna1979LandSequenceV4.CreateTurn(setup.InitialGameTurn)[0]);
    }

    internal static CampaignActionCandidate[] Actions(bool reserve = false) =>
    [
        new ResolveInitiativeAction(), new ResolveNoObligationNavalConvoyScheduleAction(),
        new ResolveNoObligationTacticalShippingAction(), new ActFirstAction(1), new ResolveWeatherAction(),
        new ResolveNoObligationOrganizationAction(), new ResolveNoObligationNavalConvoyArrivalAction(),
        new ResolveNoObligationFleetAssignmentAction(), new ResolveNoObligationFleetRepairAction(),
        .. (reserve ? new CampaignActionCandidate[] { new DesignateReserveAction("axis-infantry") } : []),
        new CompleteReserveDesignationAction(),
    ];
}
