using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignNativeReactionEventTests
{
    [Fact]
    public void NativeEventsRoundTripAndBindTheirPredecessors()
    {
        var (trigger, world, context, rulesetHash, weather) = Fixture();
        var window = trigger.ReactionWindow!;
        var opportunity = Assert.Single(window.FrozenOpportunities);
        var (move, result) = CampaignNativeReactionMoveV59.Create(trigger, world, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, opportunity.OpportunityId, "south");
        var bytes = CampaignCurrentEventSerializer.Serialize(move);
        Assert.Equal(move, CampaignCurrentEventSerializer.Deserialize(bytes));
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(bytes));
        Assert.Equal(59, move.ContractVersion);
        Assert.Equal(TurnBoundaryCanonical.Hash(trigger.SerializeCanonical()), move.PriorEventHash);
        Assert.Equal(opportunity.OpportunityId, move.WindowAfter.ActiveOpportunityId);
        Assert.Equal(result, move.Replay(trigger, world, context, weather.GameTurn, rulesetHash, weather));
        var before = world.Elements.Single(x => x.ElementId == "commonwealth-infantry");
        var after = result.Elements.Single(x => x.ElementId == before.ElementId);
        Assert.Equal(before.OperationalState.LedgerGameTurn, after.OperationalState.LedgerGameTurn);
        Assert.Equal(before.OperationalState.LedgerOperationStage, after.OperationalState.LedgerOperationStage);
        Assert.Equal(before.OperationalState.CapabilityPointsExpended + move.Cost.TotalCost,
            after.OperationalState.CapabilityPointsExpended);
        var close = CampaignNativeReactionClosedV60.Create(move, result, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId,
            CampaignNativeReactionCloseKind.ParticipantCompleted, opportunity.OpportunityId);
        Assert.Equal(60, close.ContractVersion);
        Assert.Equal("sha256:c4d3909a1c616b87479002343c3d14b49678c816fd7d0f3d60baad48079eff2c",
            TurnBoundaryCanonical.Hash(trigger.SerializeCanonical()));
        Assert.Equal("sha256:f268ab7bf936a7197b846cd80bd9aa98be3202f9582bf10c27a46779be692a9c",
            TurnBoundaryCanonical.Hash(bytes));
        Assert.Equal("sha256:6be4b05dfc466b9cae86d89508517d29e3f359d05a9ba4176d581ec4c28b0ccc",
            TurnBoundaryCanonical.Hash(close.SerializeCanonical()));
        Assert.Equal(window.ReactingPosition.SuspendedMovementPosition.PositionId, close.ResumePositionId);
        Assert.Null(close.WindowAfter);
        Assert.Equal(close.PriorWorldHash, close.ResultWorldHash);
        Assert.Equal(close, CampaignCurrentEventSerializer.Deserialize(CampaignCurrentEventSerializer.Serialize(close)));
        Assert.Equal(result, close.Replay(move, result, context, weather.GameTurn, rulesetHash, weather));
        Assert.Throws<InvalidOperationException>(() => close.Replay(trigger, world, context, weather.GameTurn, rulesetHash, weather));
        Assert.Throws<InvalidOperationException>(() => (move with { PriorEventHash = CampaignBreakdownCodec.EmptyHash })
            .Replay(trigger, world, context, weather.GameTurn, rulesetHash, weather));
        Assert.Throws<InvalidOperationException>(() => move.Replay(trigger, world, context, weather.GameTurn + 1, rulesetHash, weather));
        Assert.Throws<JsonException>(() => CampaignCurrentEventSerializer.Deserialize(bytes.Concat(new byte[] { 32 }).ToArray()));
        var reordered = JsonNode.Parse(bytes)!.AsObject();
        var version = reordered["contractVersion"]!.DeepClone();
        reordered.Remove("contractVersion");
        reordered.Add("contractVersion", version);
        Assert.Throws<JsonException>(() => CampaignCurrentEventSerializer.Deserialize(Encoding.UTF8.GetBytes(reordered.ToJsonString())));
        Assert.Throws<InvalidOperationException>(() => (move with { WindowId = new(CampaignBreakdownCodec.EmptyHash) })
            .Replay(trigger, world, context, weather.GameTurn, rulesetHash, weather));
        Assert.Throws<InvalidOperationException>(() => (close with { ResumePositionId = "wrong-position" })
            .Replay(move, result, context, weather.GameTurn, rulesetHash, weather));
        Assert.Throws<InvalidOperationException>(() => CampaignNativeReactionMoveV59.Create(close, result, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, opportunity.OpportunityId, "south-west"));
    }

    [Fact]
    public void DeclineLeavesWorldUnchangedAndClosesWindow()
    {
        var (trigger, world, context, rulesetHash, weather) = Fixture();
        var window = trigger.ReactionWindow!;
        var close = CampaignNativeReactionClosedV60.Create(trigger, world, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, CampaignNativeReactionCloseKind.PlayerDecline);
        Assert.Null(close.WindowAfter);
        Assert.Equal(close.PriorWorldHash, close.ResultWorldHash);
        Assert.Same(world, close.Replay(trigger, world, context, weather.GameTurn, rulesetHash, weather));
        Assert.Throws<InvalidOperationException>(() => CampaignNativeReactionClosedV60.Create(trigger, world, context,
            weather.GameTurn, rulesetHash, weather, new(CampaignBreakdownCodec.EmptyHash), CampaignNativeReactionCloseKind.PlayerDecline));
    }

    [Theory]
    [InlineData(1, 1, LandActorRole.SecondActingSide)]
    [InlineData(1, 2, LandActorRole.FirstActingSide)]
    [InlineData(1, 2, LandActorRole.SecondActingSide)]
    [InlineData(1, 3, LandActorRole.FirstActingSide)]
    [InlineData(1, 3, LandActorRole.SecondActingSide)]
    [InlineData(2, 1, LandActorRole.FirstActingSide)]
    [InlineData(2, 2, LandActorRole.SecondActingSide)]
    [InlineData(2, 3, LandActorRole.FirstActingSide)]
    public void ChargePreservesReactorScopeAndOtherLedger(int turn, int stage, LandActorRole role)
    {
        var (trigger, world, context, rulesetHash, weather) = Fixture(turn, stage, role);
        var window = trigger.ReactionWindow!;
        var opportunity = Assert.Single(window.FrozenOpportunities);
        var (move, result) = CampaignNativeReactionMoveV59.Create(trigger, world, context,
            turn, rulesetHash, weather, window.WindowId, opportunity.OpportunityId, "south");
        var reactor = result.Elements.Single(x => x.ElementId == move.ElementId).OperationalState;
        Assert.Equal(turn, reactor.LedgerGameTurn);
        Assert.Equal(stage, reactor.LedgerOperationStage);
        Assert.Equal(move.Cost.TotalCost, reactor.CapabilityPointsExpended);
        Assert.Equal(world.Elements.Where(x => x.ElementId != move.ElementId),
            result.Elements.Where(x => x.ElementId != move.ElementId));
        Assert.Throws<InvalidOperationException>(() => CampaignNativeReactionMoveV59.Create(trigger, world, context,
            turn, rulesetHash, weather, window.WindowId, opportunity.OpportunityId, "west"));
        Assert.Throws<InvalidOperationException>(() => (move with { ResultWorldHash = CampaignBreakdownCodec.EmptyHash })
            .Replay(trigger, world, context, turn, rulesetHash, weather));
    }

    [Fact]
    public void ExhaustedReactorLedgerOffersNoMoveAndRequiresNoEligibleClosure()
    {
        var (trigger, world, context, rulesetHash, weather) = Fixture(1, 3, LandActorRole.SecondActingSide);
        var window = trigger.ReactionWindow!;
        var opportunity = Assert.Single(window.FrozenOpportunities);
        var elementId = opportunity.ReactingRepresentation.BoundElementIds.Single();
        var allowance = context.ArtifactV6!.Definition.LegacyDefinition.Elements.Single(x => x.ElementId == elementId).BaseCapabilityPointAllowance;
        var exhausted = new CampaignWorldSnapshotV6(world.ContractVersion, world.Elements.Select(x => x.ElementId != elementId ? x :
            new CampaignElementStateV5(x.ElementId, x.CurrentLocationId, x.ReserveStatus,
                new CampaignElementOperationalStateV5(x.OperationalState.LedgerGameTurn, x.OperationalState.LedgerOperationStage,
                    new CapabilityPointAmount(allowance, 1), x.OperationalState.CohesionLevel,
                    x.OperationalState.VehicleBreakdownState, x.OperationalState.MovementEnded), x.Components)),
            world.Representations, world.BrokenVehicleLots);
        trigger = trigger with { ResultWorldHash = CampaignNativeCombatPaidWorldV22.Hash(exhausted) };
        Assert.Throws<InvalidOperationException>(() => CampaignNativeReactionMoveV59.Create(trigger, exhausted, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, opportunity.OpportunityId, "south"));
        Assert.Throws<InvalidOperationException>(() => CampaignNativeReactionClosedV60.Create(trigger, exhausted, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, CampaignNativeReactionCloseKind.PlayerDecline));
        var close = CampaignNativeReactionClosedV60.Create(trigger, exhausted, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, CampaignNativeReactionCloseKind.NoEligibleReactor);
        Assert.Null(close.WindowAfter);
        Assert.Equal(close.PriorWorldHash, close.ResultWorldHash);
    }

    [Fact]
    public void RehashedWorldFromDifferentLedgerScopeIsRejected()
    {
        var (trigger, _, context, rulesetHash, weather) = Fixture(1, 3);
        var (_, wrongScope, _, _, _) = Fixture(1, 2);
        trigger = trigger with { ResultWorldHash = CampaignNativeCombatPaidWorldV22.Hash(wrongScope) };
        Assert.Throws<InvalidOperationException>(() => CampaignNativeReactionClosedV60.Create(trigger, wrongScope,
            context, weather.GameTurn, rulesetHash, weather, trigger.ReactionWindow!.WindowId,
            CampaignNativeReactionCloseKind.PlayerDecline));
    }

    [Fact]
    public void NoWindowCannotCreateAReactionEvent()
    {
        var (trigger, world, context, rulesetHash, weather) = Fixture();
        Assert.Throws<InvalidOperationException>(() => CampaignNativeReactionClosedV60.Create(
            trigger with { ReactionWindow = null }, world, context, weather.GameTurn, rulesetHash, weather,
            trigger.ReactionWindow!.WindowId, CampaignNativeReactionCloseKind.PlayerDecline));
    }

    [Fact]
    public void NativeCohortAccountingUsesSharedProjection()
    {
        // V6 admits cohorts only on certified truck counters, not eligible combat reactors.
        // Exercise the same native accounting delegate and projection on that admitted cohort.
        var (trigger, world, context, rulesetHash, weather) = Fixture();
        var cut = CampaignNativeReactionCut.Bind(trigger, world, context, weather.GameTurn, rulesetHash, weather,
            trigger.ReactionWindow!.WindowId);
        var definition = cut.Artifact.Definition.LegacyDefinition;
        var element = world.Elements.Single(x => x.ElementId == "axis-truck");
        var content = definition.Elements.Single(x => x.ElementId == element.ElementId);
        var edge = CampaignElementMovedV3Factory.FindEdge(definition, "west", "center")!;
        var destination = definition.Locations.Single(x => x.LocationId == "center");
        var accounting = cut.Accounting(content, element, edge, destination);
        Assert.Equal(new BreakdownPointAmount(26, 1), Assert.Single(accounting).Delta);
        var representation = world.Representations.Single(x => x.BoundElementIds.Contains(element.ElementId));
        var relocated = new CampaignMapRepresentationState(representation.RepresentationId, "center",
            representation.BindingKind, representation.BoundElementIds);
        var projected = CampaignElementMovedV3Factory.ProjectMoveForAuthority(world, element, relocated,
            element.OperationalState.CapabilityPointsExpended, accounting, element.OperationalState.MovementEnded);
        Assert.Equal(accounting.Single().After, projected.Elements.Single(x => x.ElementId == element.ElementId)
            .OperationalState.VehicleBreakdownState!.CumulativeBreakdownPoints);
    }

    [Fact]
    public void CompletingParticipantKeepsOtherOpportunityAndRejectsReopening()
    {
        var (trigger, world, context, rulesetHash, weather) = MultiParticipantFixture();
        var window = trigger.ReactionWindow!;
        Assert.Equal(2, window.FrozenOpportunities.Count);
        var b = window.FrozenOpportunities.Single(x => x.ReactingRepresentation.BoundElementIds.Single() == "commonwealth-reactor-b");
        var a = window.FrozenOpportunities.Single(x => x != b);
        var (move, result) = CampaignNativeReactionMoveV59.Create(trigger, world, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, b.OpportunityId, "reactor-b-end");
        Assert.Throws<CampaignRuleNotImplementedException>(() => CampaignNativeReactionMoveV59.Create(move, result, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, b.OpportunityId, "reactor-b-start"));
        Assert.Throws<InvalidOperationException>(() => CampaignNativeReactionMoveV59.Create(move, result, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, a.OpportunityId, "reactor-a-end"));
        var close = CampaignNativeReactionClosedV60.Create(move, result, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, CampaignNativeReactionCloseKind.ParticipantCompleted, b.OpportunityId);
        Assert.NotNull(close.WindowAfter);
        Assert.Null(close.WindowAfter.ActiveOpportunityId);
        Assert.Equal(b.OpportunityId, Assert.Single(close.WindowAfter.ResolvedOpportunityIds));
        Assert.Equal(close, CampaignCurrentEventSerializer.Deserialize(close.SerializeCanonical()));
        Assert.Throws<InvalidOperationException>(() => CampaignNativeReactionMoveV59.Create(close, result, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, b.OpportunityId, "reactor-b-start"));
        var decline = CampaignNativeReactionClosedV60.Create(close, result, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, CampaignNativeReactionCloseKind.PlayerDecline);
        Assert.Null(decline.WindowAfter);
        Assert.Same(result, decline.Replay(close, result, context, weather.GameTurn, rulesetHash, weather));
        var (next, nextWorld) = CampaignNativeReactionMoveV59.Create(close, result, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, a.OpportunityId, "reactor-a-end");
        var last = CampaignNativeReactionClosedV60.Create(next, nextWorld, context,
            weather.GameTurn, rulesetHash, weather, window.WindowId, CampaignNativeReactionCloseKind.ParticipantCompleted, a.OpportunityId);
        Assert.Null(last.WindowAfter);
    }

    [Fact]
    public void NoAdjacentRepresentationProducesNoWindow()
    {
        var (trigger, world, context, rulesetHash, _) = MultiParticipantFixture();
        var opened = trigger.ReactionWindow!;
        var position = opened.ReactingPosition.SuspendedMovementPosition;
        var result = BreakdownMoveReplayTests.Relocate(world, trigger.ElementId, "axis-origin-a");
        var representation = result.Representations.Single(x => x.RepresentationId == trigger.RepresentationId);
        var input = new ElementMovedV3ReplayInput(trigger.CampaignId, trigger.StateVersion, position.PositionId,
            trigger.Side, trigger.ElementId, "trigger-a", "axis-origin-a");
        Assert.Null(CampaignElementMovedV3Factory.CreateWindowForMovement(trigger.CampaignId, rulesetHash,
            trigger.StateVersion, position, context.ArtifactV6!, context.Scenario, input, representation, result,
            opened.ReactingSide, CampaignNativeSecondMoveV44.CurrentContractVersion));
    }

    private static (CampaignNativeSecondMoveV44 Trigger, CampaignWorldSnapshotV6 World,
        CampaignContentContext Context, string RulesetHash, CampaignOperationStageWeather Weather) MultiParticipantFixture()
    {
        var artifact = Cna1979BreakdownContentCatalog.RunnerArtifacts.Single(x =>
            x.Identity.PackId == "rules-lab.content.breakdown-reaction.adjacent.v1");
        var scenario = Assert.Single(artifact.Definition.LegacyDefinition.Scenarios);
        var world = CampaignWorldV6Factory.CreateInitial(artifact, scenario);
        var result = BreakdownMoveReplayTests.Relocate(world, "axis-phasing-a", "trigger-a");
        var position = BreakdownSnapshotTests.Create(true).CurrentPosition.SequenceContext;
        var rulesetHash = Cna1979BreakdownRuleset.Manifest.Hash;
        var input = new ElementMovedV3ReplayInput("native-reaction-multi", 11, position.PositionId,
            LandSide.Axis, "axis-phasing-a", "axis-origin-a", "trigger-a");
        var representation = result.Representations.Single(x => x.BoundElementIds.Contains(input.ElementId));
        var window = CampaignElementMovedV3Factory.CreateWindowForMovement(input.CampaignId, rulesetHash,
            input.PriorStateVersion, position, artifact, scenario, input, representation, result,
            LandSide.Commonwealth, CampaignNativeSecondMoveV44.CurrentContractVersion);
        Assert.NotNull(window);
        var trigger = new CampaignNativeSecondMoveV44(44, input.CampaignId, input.PriorStateVersion,
            CampaignBreakdownCodec.EmptyHash, input.ActingSide, position.PositionId, CampaignBreakdownCodec.EmptyHash,
            input.ElementId, representation.RepresentationId, input.OriginLocationId, input.DestinationLocationId,
            CampaignNativeCombatPaidWorldV22.Hash(world), CampaignNativeCombatPaidWorldV22.Hash(result), window);
        return (trigger, result, CampaignContentContext.Create(artifact, scenario.ScenarioId), rulesetHash,
            BreakdownSnapshotTests.NormalWeather());
    }

    private static (CampaignNativeSecondMoveV44 Trigger, CampaignWorldSnapshotV6 World,
        CampaignContentContext Context, string RulesetHash, CampaignOperationStageWeather Weather) Fixture(
        int turn = 1, int stage = 1, LandActorRole role = LandActorRole.FirstActingSide)
    {
        var (prior, artifact, scenario) = BreakdownMoveReplayTests.Reaction();
        if (turn > 1)
        {
            artifact = Cna1979BreakdownContentCatalog.TurnBoundaryArtifacts.Single(x =>
                x.Identity.PackId == "rules-lab.content.breakdown-turn-boundary.gt1.v1");
            scenario = Assert.Single(artifact.Definition.LegacyDefinition.Scenarios);
        }
        var legacy = prior.ReactionWindow!;
        var catalog = Cna1979LandSequenceV4.CreateTurn(turn).Single(x => x.OperationStage == stage
            && x.ActorRole == role && x.SegmentId == LandSegmentIds.Movement);
        var position = new LandSequencePosition(catalog.ContractVersion, catalog.PositionId, catalog.GameTurn,
            catalog.OperationStage, catalog.StageId, catalog.PhaseId, catalog.SegmentId, catalog.StepId,
            catalog.ActorRole, legacy.PhasingSide, catalog.Sources);
        var world = turn == 1 && stage == 1 ? prior.World : new CampaignWorldSnapshotV6(prior.World.ContractVersion,
            prior.World.Elements.Select(x => new CampaignElementStateV5(x.ElementId, x.CurrentLocationId, x.ReserveStatus,
                new CampaignElementOperationalStateV5(turn, stage, x.OperationalState.CapabilityPointsExpended,
                    x.OperationalState.CohesionLevel, x.OperationalState.VehicleBreakdownState, null), x.Components)),
            prior.World.Representations, prior.World.BrokenVehicleLots);
        var authority = legacy.TriggerAuthority;
        var input = new ElementMovedV3ReplayInput(prior.CampaignId, prior.StateVersion - 1, position.PositionId,
            legacy.PhasingSide, authority.ElementId, authority.OriginLocationId, authority.DestinationLocationId);
        var window = CampaignElementMovedV3Factory.CreateWindowForMovement(prior.CampaignId, prior.RulesetHash,
            prior.StateVersion - 1, position, artifact, scenario, input, authority.TriggeringRepresentation,
            world, legacy.ReactingSide, CampaignNativeSecondMoveV44.CurrentContractVersion)!;
        var worldHash = CampaignNativeCombatPaidWorldV22.Hash(world);
        var trigger = new CampaignNativeSecondMoveV44(44, prior.CampaignId, prior.StateVersion - 1,
            CampaignBreakdownCodec.EmptyHash, legacy.PhasingSide, position.PositionId, CampaignBreakdownCodec.EmptyHash,
            authority.ElementId, authority.TriggeringRepresentation.RepresentationId, authority.OriginLocationId,
            authority.DestinationLocationId, CampaignBreakdownCodec.EmptyHash, worldHash, window);
        var season = Cna1979Weather.GetSeason(turn);
        var dice = (from first in Enumerable.Range(1, 6)
                    from second in Enumerable.Range(1, 6)
                    where Cna1979Weather.GetKind(season, first * 10 + second) == WeatherKind.Normal
                    select (First: first, Second: second)).First();
        var weather = new CampaignOperationStageWeather(CampaignOperationStageWeather.CurrentContractVersion,
            turn, stage, LandSide.Axis, season, dice.First, dice.Second, WeatherKind.Normal,
            WeatherScope.None, null, [], 0, 0, 0);
        return (trigger, world, CampaignContentContext.Create(artifact, scenario.ScenarioId), prior.RulesetHash, weather);
    }
}
