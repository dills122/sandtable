using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class BreakdownSequenceTests
{
    [Fact]
    public void DormantCatalogPreservesLinearOrderAndSeparatesSystemInterrupt()
    {
        var prior = Cna1979LandSequence.CreateTurn(2);
        var next = Cna1979LandSequenceV4.CreateTurn(2);
        Assert.Equal(prior.Select(position => position.PositionId), next.Select(position => position.PositionId));
        Assert.All(next, position => Assert.Equal(4, position.ContractVersion));
        Assert.DoesNotContain(next, position => position.PositionId == Cna1979LandSequenceV4.BreakdownStopPositionId);
        for (var index = 0; index < prior.Count; index++)
            Assert.Equal(prior[index], Copy(next[index], 3, next[index].ActiveSide));

        var stop = Cna1979LandSequenceV4.CreateBreakdownStopPosition(Movement());
        Assert.Equal("land.position.breakdown-stop", stop.PositionId);
        Assert.Equal(LandActorRole.None, stop.ActorRole);
        Assert.Null(stop.ActiveSide);
        Assert.Equal((1, 1), (stop.GameTurn, stop.OperationStage));
        Assert.Throws<ArgumentException>(() => Cna1979LandSequenceV4.GetNext(stop));
    }

    [Fact]
    public void MaterializedMovementRejectsPredecessorAndForgedSources()
    {
        var movement = Movement();
        Cna1979LandSequenceV4.RequireMaterializedMovement(movement);
        Assert.Throws<ArgumentException>(() => Cna1979LandSequenceV4.RequireMaterializedMovement(Copy(movement, 3, LandSide.Axis)));
        Assert.Throws<ArgumentException>(() => Cna1979LandSequenceV4.RequireMaterializedMovement(Copy(movement, 4, null)));
        Assert.Throws<ArgumentException>(() => Cna1979LandSequenceV4.RequireMaterializedMovement(Copy(movement, 4, LandSide.Axis, [new("spi-1979-land-rules", "21.99")])));
        Assert.Throws<ArgumentException>(() => Cna1979LandSequenceV4.GetNext(Copy(movement, 3, null)));
        Assert.Throws<ArgumentException>(() => Cna1979LandSequenceV4.GetNext(Copy(movement, 4, null, [new("spi-1979-land-rules", "21.99")])));
    }

    [Fact]
    public void CheckpointsStopAtFirstSideCombatAndMaterializedSideRequiresOrder()
    {
        var positions = Cna1979LandSequenceV4.CreateTurn(1);
        var combat = positions.Single(position => position.OperationStage == 1
            && position.ActorRole == LandActorRole.FirstActingSide
            && position.StepId == LandStepIds.PositionDetermination);
        Assert.True(Cna1979LandSequenceV4.IsSupportedCheckpoint(combat));
        Assert.False(Cna1979LandSequenceV4.IsSupportedCheckpoint(Cna1979LandSequenceV4.GetNext(combat)));
        Assert.False(Cna1979LandSequenceV4.IsSupportedCheckpoint(positions.First(position => position.OperationStage == 2)));
        Assert.Throws<ArgumentException>(() => CampaignSequenceV6Guards.RequireCurrentPosition(Movement(), []));
        CampaignSequenceV6Guards.RequireCurrentPosition(Movement(), [new(2, 1, 1, LandSide.Axis, LandSide.Commonwealth)]);
        Assert.Throws<ArgumentException>(() => CampaignSequenceV6Guards.RequireCurrentPosition(Movement(), [new(2, 1, 1, LandSide.Commonwealth, LandSide.Axis)]));
        Assert.Throws<ArgumentException>(() => CampaignSequenceV6Guards.RequireCurrentPosition(Movement(), [new(2, 1, 2, LandSide.Axis, LandSide.Commonwealth)]));
        Assert.Throws<ArgumentException>(() => CampaignSequenceV6Guards.RequireCurrentPosition(Copy(Movement(), 3, LandSide.Axis), [new(2, 1, 1, LandSide.Axis, LandSide.Commonwealth)]));
        Assert.Throws<ArgumentException>(() => CampaignSequenceV6Guards.RequireCurrentPosition(Cna1979LandSequenceV4.GetNext(combat), []));
    }

    [Fact]
    public void CanonicalCatalogAndManifestPreserveExplicitHistoricalIdentityDifference()
    {
        var bytes = Cna1979LandSequenceV4.SerializeCanonicalCatalog();
        using var document = JsonDocument.Parse(bytes);
        Assert.Equal(4, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(Cna1979LandSequenceV4.CreateTurn(1).Count, document.RootElement.GetProperty("positions").GetArrayLength());
        Assert.Equal("land.position.breakdown-stop", document.RootElement.GetProperty("interruptPositions")[0].GetProperty("positionId").GetString());
        Assert.Equal($"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}", Cna1979LandSequenceV4.CreateArtifact().ContentHash);
        Assert.Equal(9, Cna1979BreakdownRuleset.Manifest.ContractVersion);
        Assert.Equal(8, Cna1979Ruleset.HistoricalManifestV8.ContractVersion);
        Assert.Equal("0e80a8ba917113b401ea709f9f2a6cd7fb7cfec03b8adbdae978f1b219e141e0", Cna1979Ruleset.HistoricalManifestV8.Hash);
        Assert.False(Cna1979BreakdownRuleset.IsCanonicalHash(Cna1979Ruleset.HistoricalManifestV8.Hash));
        Assert.False(Cna1979Ruleset.IsHistoricalHashV8(Cna1979BreakdownRuleset.Manifest.Hash));
        Assert.Equal(Cna1979Ruleset.HistoricalManifestV8.Rulings.Count + 4, Cna1979BreakdownRuleset.Manifest.Rulings.Count);
        foreach (var artifactId in new[] { Cna1979LandSequenceV4.ArtifactId, Cna1979Breakdown.ArtifactId })
        {
            var mixed = new RulesetManifest(Cna1979Ruleset.RulesetId, 9,
                Cna1979BreakdownRuleset.Manifest.Artifacts.Select(artifact => artifact.ArtifactId == artifactId
                    ? Cna1979Ruleset.HistoricalManifestV8.Artifacts.Single(value => value.ArtifactId == artifactId) : artifact),
                Cna1979BreakdownRuleset.Manifest.Rulings);
            Assert.False(Cna1979BreakdownRuleset.IsCanonicalHash(mixed.Hash));
        }
        foreach (var artifact in Cna1979Ruleset.HistoricalManifestV8.Artifacts.Where(artifact => artifact.ArtifactId != Cna1979LandSequenceV4.ArtifactId && artifact.ArtifactId != Cna1979Breakdown.ArtifactId))
            Assert.Equal(artifact, Cna1979BreakdownRuleset.Manifest.Artifacts.Single(value => value.ArtifactId == artifact.ArtifactId));
    }

    [Fact]
    public void ExplicitSuccessorFactoriesKeepLegacyConstructorsStrict()
    {
        var movement = Movement();
        Assert.Equal(4, CampaignMovementEndedState.CreateForBreakdown(movement).SequenceContractVersion);
        Assert.Equal(movement, CampaignReactingPosition.CreateForBreakdown(movement).SuspendedMovementPosition);
        Assert.Throws<ArgumentException>(() => new CampaignMovementEndedState(movement));
        Assert.Throws<ArgumentException>(() => new CampaignReactingPosition(movement));
        Assert.Throws<ArgumentException>(() => CampaignReactingPosition.CreateForBreakdown(Copy(movement, 3, LandSide.Axis)));
        var representation = new CampaignMapRepresentationState("map-representation.0001", "east", CampaignMapRepresentationBindingKind.IndependentElement, ["axis-battalion"]);
        var trigger = CampaignReactionTriggerAuthority.CreateForBreakdown("axis-battalion", representation, "west", "east");
        Assert.Equal(3, trigger.MoveContractVersion);
        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignReactionTriggerAuthority(3, "axis-battalion", representation, "west", "east"));
        var oldTrigger = new CampaignReactionTriggerAuthority(2, "axis-battalion", representation, "west", "east");
        var reacting = CampaignReactingPosition.CreateForBreakdown(movement);
        var id = CampaignReactionIdentity.CreateWindow("campaign-1", Cna1979BreakdownRuleset.Manifest.Hash, 3, 2, representation, "west", "east", LandSide.Commonwealth);
        var apparent = new CampaignApparentReactionTrigger("apparent.0001", "west", "east");
        var window = new CampaignReactionWindow(id, 2, LandSide.Axis, LandSide.Commonwealth, reacting, trigger, apparent, [], [], null);
        window.ValidateIdentities("campaign-1", Cna1979BreakdownRuleset.Manifest.Hash);
        Assert.Throws<ArgumentException>(() => new CampaignReactionWindow(id, 2, LandSide.Axis, LandSide.Commonwealth, reacting, oldTrigger, apparent, [], [], null));
        Assert.Throws<ArgumentException>(() => new CampaignReactionWindow(id, 2, LandSide.Axis, LandSide.Commonwealth, new CampaignReactingPosition(Copy(movement, 3, LandSide.Axis)), trigger, apparent, [], [], null));
    }

    [Fact]
    public void PredecessorWorldRejectsSuccessorMovementEndedMarker()
    {
        var movement = Movement();
        var successor = Element(CampaignMovementEndedState.CreateForBreakdown(movement));
        var legacy = Element(new CampaignMovementEndedState(Copy(movement, 3, LandSide.Axis)));

        Assert.Throws<ArgumentException>(() => new CampaignWorldSnapshotV5(5, [successor], []));
        Assert.Equal(legacy, Assert.Single(new CampaignWorldSnapshotV5(5, [legacy], []).Elements));

        static CampaignElementStateV5 Element(CampaignMovementEndedState ended) => new(
            "axis-battalion", "east", CampaignElementReserveStatus.None,
            new CampaignElementOperationalStateV5(1, 1, CapabilityPointAmount.Zero, 0, null, ended), []);
    }

    private static LandSequencePosition Movement() => Copy(Cna1979LandSequenceV4.CreateTurn(1).Single(position => position.OperationStage == 1 && position.ActorRole == LandActorRole.FirstActingSide && position.SegmentId == LandSegmentIds.Movement), 4, LandSide.Axis);

    private static LandSequencePosition Copy(LandSequencePosition position, int version, LandSide? side, IReadOnlyList<RuleReference>? sources = null) => new(version, position.PositionId, position.GameTurn, position.OperationStage, position.StageId, position.PhaseId, position.SegmentId, position.StepId, position.ActorRole, side, sources ?? position.Sources);
}
