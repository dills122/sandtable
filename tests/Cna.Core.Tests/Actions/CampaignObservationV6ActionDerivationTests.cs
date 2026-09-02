using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Actions;

[Trait("Boundary", "UserSpace")]
public sealed class CampaignObservationV6ActionDerivationTests
{
    [Fact]
    public void NormalMembershipIsTopologyLocalAndMovementEndedAware()
    {
        var baseline = ProjectNormal([]);
        var target = Assert.Single(
            DerivePlayer(ProjectNormal(["east"])).Candidates.OfType<MoveElementAction>(),
            move => move.OriginLocationId == "west" && move.DestinationLocationId == "east");
        Assert.NotNull(target);

        Assert.Contains(
            DerivePlayer(ProjectNormal(["west"])).Candidates.OfType<MoveElementAction>(),
            move => move.OriginLocationId == "west" && move.DestinationLocationId == "east");
        Assert.DoesNotContain(
            DerivePlayer(ProjectNormal(["west", "east"])).Candidates.OfType<MoveElementAction>(),
            move => move.OriginLocationId == "west" && move.DestinationLocationId == "east");

        var remote = WithRemoteControl(ProjectNormal([]));
        Assert.Equal(
            SerializeCandidateVector(DerivePlayer(baseline)),
            SerializeCandidateVector(DerivePlayer(remote)));

        var ended = Copy(
            baseline,
            movementEndedElementIds: [baseline.OwnElements.Single().ElementId]);
        Assert.Empty(DerivePlayer(ended).Candidates.OfType<MoveElementAction>());
        Assert.Single(DerivePlayer(ended).Candidates.OfType<CompleteMovementSegmentAction>());
    }

    [Fact]
    public void ReactingMembershipSeparatesFirstLaterCompletionAndClosedCloseKinds()
    {
        var idle = ProjectReacting([]);
        var idleSet = DerivePlayer(idle);
        var firstStep = Assert.Single(idleSet.Candidates.OfType<MoveReactingElementAction>());
        Assert.Single(idleSet.Candidates.OfType<DeclineReactionWindowAction>());
        Assert.Empty(idleSet.Candidates.OfType<CompleteReactionParticipantAction>());
        Assert.Equal(
            Assert.IsType<CampaignObservationReactingDecisionState>(idle.DecisionState).WindowId,
            firstStep.WindowId);

        var idleState = Assert.IsType<CampaignObservationReactingDecisionState>(idle.DecisionState);
        var active = Copy(
            idle,
            decisionState: new CampaignObservationReactingDecisionState(
                idleState.WindowId,
                idleState.ApparentTrigger,
                idleState.OwnOpportunities,
                new ObservedReactionParticipant(
                    firstStep.OpportunityId)));
        var activeSet = DerivePlayer(active);
        Assert.NotEmpty(activeSet.Candidates.OfType<MoveReactingElementAction>());
        Assert.All(activeSet.Candidates.OfType<MoveReactingElementAction>(), move =>
            Assert.Equal(firstStep.OpportunityId, move.OpportunityId));
        Assert.Single(activeSet.Candidates.OfType<CompleteReactionParticipantAction>());
        Assert.Empty(activeSet.Candidates.OfType<DeclineReactionWindowAction>());

        var system = CampaignObservationV6ActionDerivation.DeriveSystem(active);
        Assert.Single(system.Candidates.OfType<CloseReactionWindowUnavailableAction>());
        Assert.Single(system.Candidates.OfType<CloseReactionWindowTimeoutAction>());
        Assert.Empty(system.Candidates.OfType<CloseReactionWindowNoEligibleAction>());

        var empty = Copy(
            idle,
            decisionState: new CampaignObservationReactingDecisionState(
                idleState.WindowId,
                idleState.ApparentTrigger,
                [],
                null));
        Assert.Empty(DerivePlayer(empty).Candidates);
        var emptySystem = CampaignObservationV6ActionDerivation.DeriveSystem(empty);
        Assert.Single(emptySystem.Candidates.OfType<CloseReactionWindowNoEligibleAction>());
        Assert.Empty(emptySystem.Candidates.OfType<CloseReactionWindowUnavailableAction>());
        Assert.Empty(emptySystem.Candidates.OfType<CloseReactionWindowTimeoutAction>());
    }

    [Fact]
    public void ReactionCannotEnterControlledDestinationAndRemoteControlDoesNotInterfere()
    {
        var baseline = DerivePlayer(ProjectReacting([]));
        var remote = DerivePlayer(WithRemoteControl(ProjectReacting([])));
        var controlled = DerivePlayer(ProjectReacting(["center"]));

        Assert.Equal(SerializeCandidateVector(baseline), SerializeCandidateVector(remote));
        Assert.DoesNotContain(
            controlled.Candidates.OfType<MoveReactingElementAction>(),
            move => move.DestinationLocationId == "center");
    }

    [Fact]
    public void ReactionMoveOptionsRejectEveryCurrentlyIneligibleElementState()
    {
        var observation = ProjectNormal([]);
        var element = Assert.Single(observation.OwnElements);
        var stacking = Cna1979Movement.LookupStackingValue(element.OrganizationId);
        Assert.True(stacking.IsSupported);
        var ownStacking = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [element.CurrentLocationId] = stacking.Value.StackingValue,
        };

        Assert.NotEmpty(Options(element));
        Assert.Empty(Options(CopyElement(
            element,
            reserveStatus: CampaignObservationReserveStatus.ReserveI)));
        Assert.Empty(Options(CopyElement(element, cohesionLevel: -26)));
        Assert.Empty(Options(CopyElement(
            element,
            ledgerGameTurn: checked(element.LedgerGameTurn + 1))));
        Assert.Empty(Options(CopyElement(
            element,
            ledgerOperationStage: checked(element.LedgerOperationStage + 1))));

        IReadOnlyList<ObservedReactionMoveOption> Options(ObservedOwnElement candidate) =>
            CampaignObservationV6ActionDerivation.DeriveReactionMoveOptions(
                observation.Position,
                observation.Locations,
                observation.Edges,
                [],
                [],
                candidate,
                ownStacking);
    }

    [Fact]
    public void ReactionActionsUseOnlyPublishedMoveOptions()
    {
        var baseline = ProjectReacting([]);
        var baselineState = Assert.IsType<CampaignObservationReactingDecisionState>(
            baseline.DecisionState);
        Assert.Contains(
            DerivePlayer(baseline).Candidates.OfType<MoveReactingElementAction>(),
            move => move.DestinationLocationId == "center");

        Assert.Single(baselineState.OwnOpportunities);
        var emptyOpportunity = new ObservedReactionOpportunity(
            CampaignObservationV6DisclosureIdentity.CreateOpportunity(
                baselineState.WindowId,
                baseline.StateVersion,
                CampaignObservationV6DisclosureIdentity.CreateCapabilityKey([])),
            []);
        var withoutOptions = Copy(
            baseline,
            decisionState: new CampaignObservationReactingDecisionState(
                baselineState.WindowId,
                baselineState.ApparentTrigger,
                [emptyOpportunity],
                new ObservedReactionParticipant(emptyOpportunity.OpportunityId)));

        Assert.Empty(withoutOptions.OwnElements);
        Assert.Empty(DerivePlayer(withoutOptions).Candidates.OfType<MoveReactingElementAction>());
        Assert.Single(
            DerivePlayer(withoutOptions).Candidates.OfType<CompleteReactionParticipantAction>());
    }

    [Fact]
    public void SuccessorReaderIsStrictCoherentAndLeavesActiveReaderClosed()
    {
        var set = DerivePlayer(ProjectReacting([]));
        var canonical = CampaignObservationV6LegalActionSerializer.Serialize(set);
        var roundTrip = CampaignObservationV6LegalActionSerializer.DeserializeCanonical(canonical);
        var json = Encoding.UTF8.GetString(canonical);

        Assert.Equal(set, roundTrip);
        Assert.Equal(
            set,
            CampaignObservationV6LegalActionSerializer.DeserializeCurrent(
                canonical,
                ProjectReacting([])));
        Assert.Throws<JsonException>(() => CampaignLegalActionSerializer.DeserializeCanonical(canonical));
        Assert.Throws<JsonException>(() => CampaignObservationV6LegalActionSerializer
            .DeserializeCanonical(Encoding.UTF8.GetBytes(json.Replace(
                "\"contractVersion\":2,",
                "\"contractVersion\":2,\"extra\":true,",
                StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => CampaignObservationV6LegalActionSerializer
            .DeserializeCanonical(Encoding.UTF8.GetBytes(json.Replace(
                "\"destinationLocationId\":\"center\"",
                "\"destinationLocationId\":\"east\"",
                StringComparison.Ordinal))));

        var move = Assert.Single(set.Candidates.OfType<MoveReactingElementAction>());
        var rehashed = new MoveReactingElementAction(
            move.WindowId,
            move.OpportunityId,
            move.DestinationLocationId,
            move.OriginLocationId,
            move.CostBreakdown);
        var forged = new CampaignLegalActionSet(
            set.CampaignId,
            set.StateVersion,
            set.RulesetHash,
            set.PositionId,
            set.Audience,
            set.Candidates.Select(candidate => candidate == move ? rehashed : candidate)
                .ToArray());
        var forgedBytes = CampaignObservationV6LegalActionSerializer.Serialize(forged);
        Assert.Equal(forged, CampaignObservationV6LegalActionSerializer
            .DeserializeCanonical(forgedBytes));
        Assert.Throws<JsonException>(() => CampaignObservationV6LegalActionSerializer
            .DeserializeCurrent(forgedBytes, ProjectReacting([])));
    }

    [Fact]
    public void SubmissionMappingRequiresExactCurrentMembershipAndProducesClosedIntents()
    {
        var observation = ProjectReacting([]);
        var set = DerivePlayer(observation);
        var move = Assert.Single(set.Candidates.OfType<MoveReactingElementAction>());
        var submission = Submission(set, move);

        var intent = Assert.IsType<MoveReactingElementIntent>(
            CampaignObservationV6ActionDerivation.MapSubmission(observation, submission));
        Assert.Equal(move.WindowId, intent.WindowId);
        Assert.Equal(move.OpportunityId, intent.OpportunityId);
        Assert.Equal(move.ActionId, intent.ActionId);

        Assert.Null(CampaignObservationV6ActionDerivation.MapSubmission(
            observation,
            submission with { ExpectedStateVersion = submission.ExpectedStateVersion + 1 }));
        Assert.Null(CampaignObservationV6ActionDerivation.MapSubmission(
            ProjectReacting([move.DestinationLocationId]),
            submission));

        var systemSet = CampaignObservationV6ActionDerivation.DeriveSystem(observation);
        var timeout = Assert.Single(systemSet.Candidates.OfType<CloseReactionWindowTimeoutAction>());
        var close = Assert.IsType<CloseReactionWindowIntent>(
            CampaignObservationV6ActionDerivation.MapSubmission(
                observation,
                Submission(systemSet, timeout)));
        Assert.Equal(CampaignReactionCloseIntentKind.Timeout, close.CloseKind);
    }

    [Fact]
    public void CandidateIdentityBindsEverySideSafeSemanticAndIgnoresHiddenSources()
    {
        var baseline = Assert.Single(
            DerivePlayer(ProjectReacting([])).Candidates.OfType<MoveReactingElementAction>());
        var changedWindow = new MoveReactingElementAction(
            "sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
            baseline.OpportunityId,
            baseline.OriginLocationId,
            baseline.DestinationLocationId,
            baseline.CostBreakdown);
        var changedOpportunity = new MoveReactingElementAction(
            baseline.WindowId,
            "sha256:eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee",
            baseline.OriginLocationId,
            baseline.DestinationLocationId,
            baseline.CostBreakdown);

        Assert.NotEqual(baseline.ActionId, changedWindow.ActionId);
        Assert.NotEqual(baseline.ActionId, changedOpportunity.ActionId);
        Assert.DoesNotContain(
            "source",
            Encoding.UTF8.GetString(CampaignObservationV6LegalActionSerializer.Serialize(
                DerivePlayer(ProjectReacting([])))),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ByteIdenticalPublicFactsProduceByteIdenticalActionBytes()
    {
        var first = WithRemoteControl(ProjectNormal([]));
        var second = Copy(
            first,
            locations: first.Locations.Reverse().ToArray(),
            controlledLocationIds: first.ApparentEnemyControlledLocationIds.Reverse().ToArray());

        Assert.Equal(
            CampaignObservationV6Serializer.SerializeCanonical(first),
            CampaignObservationV6Serializer.SerializeCanonical(second));
        Assert.Equal(
            CampaignObservationV6LegalActionSerializer.Serialize(DerivePlayer(first)),
            CampaignObservationV6LegalActionSerializer.Serialize(DerivePlayer(second)));
    }

    [Fact]
    public void ReactionActionBytesNeverPublishTheAuthoritativeElementBinding()
    {
        var observation = ProjectReacting([]);
        var bytes = CampaignObservationV6LegalActionSerializer.Serialize(DerivePlayer(observation));
        var observationJson = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(observation));

        Assert.DoesNotContain(
            "\"elementId\":",
            Encoding.UTF8.GetString(bytes),
            StringComparison.Ordinal);
        Assert.NotEmpty(DerivePlayer(observation).Candidates.OfType<MoveReactingElementAction>());
        Assert.Contains("\"moveOptions\":", observationJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"movement\":", observationJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"ownStacking\":", observationJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ReactionCapabilitySurfaceHasNoStableRepresentationJoinKey()
    {
        Type[] outwardCapabilityTypes =
        [
            typeof(ObservedReactionOpportunity),
            typeof(ObservedReactionParticipant),
            typeof(MoveReactingElementAction),
            typeof(CompleteReactionParticipantAction),
        ];
        var observation = ProjectReacting([]);
        var observationJson = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(observation));
        var actionJson = Encoding.UTF8.GetString(
            CampaignObservationV6LegalActionSerializer.Serialize(DerivePlayer(observation)));

        Assert.All(outwardCapabilityTypes, type => Assert.DoesNotContain(
            type.GetProperties(),
            property => property.Name == "RepresentationId"));
        Assert.DoesNotContain("\"representationId\":", actionJson, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(observationJson);
        var opportunities = document.RootElement.GetProperty("decisionState")
            .GetProperty("ownOpportunities");
        Assert.All(opportunities.EnumerateArray(), opportunity =>
            Assert.False(opportunity.TryGetProperty("representationId", out _)));
    }

    private static CampaignLegalActionSet DerivePlayer(CampaignObservationV6 observation) =>
        CampaignObservationV6ActionDerivation.DerivePlayer(observation);

    private static CampaignObservationV6 ProjectNormal(IReadOnlyList<string> controlled)
    {
        var fixture = CampaignV10TestData.Create();
        return Copy(CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            new CampaignObservationV6AuthorityFacts(controlled, [])),
            apparentOpposingPresences: []);
    }

    private static CampaignObservationV6 ProjectReacting(IReadOnlyList<string> controlled)
    {
        var fixture = CampaignV10TestData.Create(includeReactionExit: true);
        var snapshot = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            fixture.TriggeringMove,
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => fixture.TriggeringMove);
        var projected = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            new CampaignObservationV6AuthorityFacts(controlled, []));
        var state = Assert.IsType<CampaignObservationReactingDecisionState>(
            projected.DecisionState);
        if (controlled.Contains("center", StringComparer.Ordinal))
        {
            Assert.Empty(state.OwnOpportunities);
        }
        else
        {
            Assert.Single(state.OwnOpportunities);
        }

        return projected;
    }

    private static CampaignActionSubmission Submission(
        CampaignLegalActionSet set,
        CampaignActionCandidate candidate) => new(
            CampaignActionSubmission.CurrentContractVersion,
            set.CampaignId,
            set.StateVersion,
            set.PositionId,
            set.Audience,
            candidate.ActionId);

    private static byte[] SerializeCandidateVector(CampaignLegalActionSet set) =>
        CampaignObservationV6LegalActionSerializer.Serialize(set);

    private static CampaignObservationV6 Copy(
        CampaignObservationV6 source,
        IReadOnlyList<string>? movementEndedElementIds = null,
        CampaignObservationDecisionState? decisionState = null,
        IReadOnlyList<CampaignObservationLocation>? locations = null,
        IReadOnlyList<ObservedApparentPresence>? apparentOpposingPresences = null,
        IReadOnlyList<string>? controlledLocationIds = null) => new(
            source.ContractVersion,
            source.PolicyId,
            source.CampaignId,
            source.StateVersion,
            source.RulesetHash,
            source.ScenarioId,
            source.Observer,
            source.Position,
            source.Weather,
            locations ?? source.Locations,
            source.Edges,
            source.OwnElements,
            apparentOpposingPresences ?? source.ApparentOpposingPresences,
            controlledLocationIds ?? source.ApparentEnemyControlledLocationIds,
            movementEndedElementIds ?? source.MovementEndedElementIds,
            decisionState ?? source.DecisionState);

    private static CampaignObservationV6 WithRemoteControl(CampaignObservationV6 source) => Copy(
        source,
        locations: [.. source.Locations, new CampaignObservationLocation(
            "remote",
            "land.terrain.clear")],
        controlledLocationIds: ["remote"]);

    private static ObservedOwnElement CopyElement(
        ObservedOwnElement source,
        CampaignObservationReserveStatus? reserveStatus = null,
        int? cohesionLevel = null,
        int? ledgerGameTurn = null,
        int? ledgerOperationStage = null) => new(
            source.ElementId,
            source.ParentFormationId,
            source.OrganizationId,
            source.BaseCapabilityPointAllowance,
            source.CurrentLocationId,
            reserveStatus ?? source.ReserveStatus,
        source.MobilityId,
        ledgerGameTurn ?? source.LedgerGameTurn,
        ledgerOperationStage ?? source.LedgerOperationStage,
            source.CapabilityPointsExpended,
            cohesionLevel ?? source.CohesionLevel,
            source.VehicleBreakdownRisk);
}
