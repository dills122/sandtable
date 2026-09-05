using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignReactionWindowClosedReplayTests
{
    [Fact]
    public void PlayerDeclineClosesOnlyRemainingOpportunitiesAndResumesExactMovement()
    {
        var fixture = CreateOpenedWindow(
            ["commonwealth-reactor-alpha", "commonwealth-reactor-bravo"]);
        var window = fixture.Snapshot.ReactionWindow!;
        var previouslyResolved = window.FrozenOpportunities[0].OpportunityId;
        var remaining = window.FrozenOpportunities[1].OpportunityId;
        var prior = WithWindowState(
            fixture.Snapshot,
            [previouslyResolved],
            activeOpportunityId: null,
            stateVersion: 13);
        var intent = CreateIntent(prior, CampaignReactionCloseIntentKind.PlayerDecline);

        var closed = CampaignReactionWindowClosedFactory.Create(
            prior,
            fixture.Artifact,
            fixture.Scenario,
            intent);
        var projected = CampaignV10Projector.ApplyReactionClose(
            prior,
            closed,
            fixture.Artifact,
            fixture.Scenario);

        Assert.Equal(14, closed.StateVersion);
        Assert.Equal(CampaignReactionWindowCloseReason.PlayerDecline, closed.Reason);
        Assert.Equal([remaining], closed.ClosedOpportunityIds);
        Assert.Equal(window.ReactingPosition.SuspendedMovementPosition,
            closed.ResumedSequencePosition);
        Assert.Equal(CampaignPositionV10Kind.Sequence, projected.CurrentPosition.Kind);
        Assert.Equal(closed.ResumedSequencePosition, projected.CurrentPosition.SequencePosition);
        Assert.Null(projected.ReactionWindow);
        Assert.Equal(prior.World, projected.World);
        Assert.Equal(prior.RandomState, projected.RandomState);
    }

    [Theory]
    [InlineData((int)CampaignReactionCloseIntentKind.ScriptedUnavailable)]
    [InlineData((int)CampaignReactionCloseIntentKind.Timeout)]
    public void SystemFallbackClosesActiveEpisodeWithoutUndoingCommittedState(
        int closeKindValue)
    {
        var closeKind = (CampaignReactionCloseIntentKind)closeKindValue;
        var fixture = CreateOpenedWindow(
            ["commonwealth-reactor-alpha"],
            includeReactionExit: true);
        var opportunity = Assert.Single(
            fixture.Snapshot.ReactionWindow!.FrozenOpportunities).OpportunityId;
        var prior = WithActiveParticipantAtCenter(fixture.Snapshot, opportunity, 13);
        var intent = CreateIntent(prior, closeKind);

        var closed = CampaignReactionWindowClosedFactory.Create(
            prior,
            fixture.Artifact,
            fixture.Scenario,
            intent);
        var projected = CampaignV10Projector.ApplyReactionClose(
            prior,
            closed,
            fixture.Artifact,
            fixture.Scenario);

        Assert.Null(closed.ActingSide);
        Assert.Equal([opportunity], closed.ClosedOpportunityIds);
        Assert.Equal(prior.World, projected.World);
        Assert.Equal(prior.RandomState, projected.RandomState);
        Assert.Null(projected.ReactionWindow);
        Assert.Equal(
            prior.ReactionWindow!.ReactingPosition.SuspendedMovementPosition,
            projected.CurrentPosition.SequencePosition);
    }

    [Fact]
    public void EmptyWindowClosesDeterministicallyAtSecondCommittedVersion()
    {
        var fixture = CreateOpenedWindow(
            ["commonwealth-headquarters"],
            reactorClassificationId: Cna1979Combat.HeadquartersClassificationId);
        Assert.Empty(fixture.Snapshot.ReactionWindow!.FrozenOpportunities);
        var intent = CreateIntent(
            fixture.Snapshot,
            CampaignReactionCloseIntentKind.NoEligibleReactor);

        var first = CampaignReactionWindowClosedFactory.Create(
            fixture.Snapshot,
            fixture.Artifact,
            fixture.Scenario,
            intent);
        var second = CampaignReactionWindowClosedFactory.Create(
            fixture.Snapshot,
            fixture.Artifact,
            fixture.Scenario,
            intent);
        var projected = CampaignV10Projector.ApplyReactionClose(
            fixture.Snapshot,
            first,
            fixture.Artifact,
            fixture.Scenario);

        Assert.Equal(13, first.StateVersion);
        Assert.Equal(CampaignReactionWindowCloseReason.NoEligibleReactor, first.Reason);
        Assert.Empty(first.ClosedOpportunityIds);
        Assert.Equal(
            CampaignSuccessorEventSerializer.Serialize(first),
            CampaignSuccessorEventSerializer.Serialize(second));
        Assert.Equal(fixture.Snapshot.World, projected.World);
        Assert.Equal(fixture.Snapshot.RandomState, projected.RandomState);
    }

    [Fact]
    public void PlayerDeclineRejectsWhileParticipantIsActive()
    {
        var fixture = CreateOpenedWindow(["commonwealth-reactor-alpha"]);
        var opportunity = Assert.Single(
            fixture.Snapshot.ReactionWindow!.FrozenOpportunities).OpportunityId;
        var prior = WithWindowState(
            fixture.Snapshot,
            [],
            opportunity,
            stateVersion: 13);
        var publicWindowId = PublicWindowId(prior);
        var action = new DeclineReactionWindowAction(publicWindowId);
        var intent = new CloseReactionWindowIntent(
            prior.StateVersion,
            prior.ReactionWindow!.ReactingPosition.SuspendedMovementPosition.PositionId,
            prior.ReactionWindow.ReactingSide,
            action.ActionId,
            publicWindowId,
            CampaignReactionCloseIntentKind.PlayerDecline);

        Assert.Throws<InvalidOperationException>(() =>
            CampaignReactionWindowClosedFactory.Create(
                prior,
                fixture.Artifact,
                fixture.Scenario,
                intent));
    }

    [Fact]
    public void CloseAuthorityRejectsStaleWrongWindowWrongAudienceAndReasonMismatch()
    {
        var fixture = CreateOpenedWindow(["commonwealth-reactor-alpha"]);
        var valid = CreateIntent(
            fixture.Snapshot,
            CampaignReactionCloseIntentKind.Timeout);
        var invalid = new CloseReactionWindowIntent[]
        {
            valid with { ExpectedStateVersion = valid.ExpectedStateVersion + 1 },
            valid with
            {
                WindowId = "sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
            },
            valid with { ActingSide = fixture.Snapshot.ReactionWindow!.ReactingSide },
            valid with { CloseKind = CampaignReactionCloseIntentKind.ScriptedUnavailable },
            valid with { CloseKind = (CampaignReactionCloseIntentKind)999 },
            valid with { ActionId = "sha256:eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee" },
        };

        Assert.All(invalid, intent => Assert.Throws<InvalidOperationException>(() =>
            CampaignReactionWindowClosedFactory.Create(
                fixture.Snapshot,
                fixture.Artifact,
                fixture.Scenario,
                intent)));
    }

    [Fact]
    public void CloseEventRoundTripsReconstructsAndRejectsDuplicateApplication()
    {
        var fixture = CreateOpenedWindow(["commonwealth-reactor-alpha"]);
        var intent = CreateIntent(
            fixture.Snapshot,
            CampaignReactionCloseIntentKind.ScriptedUnavailable);
        var closed = CampaignReactionWindowClosedFactory.Create(
            fixture.Snapshot,
            fixture.Artifact,
            fixture.Scenario,
            intent);
        var bytes = CampaignSuccessorEventSerializer.Serialize(closed);
        var roundTripped = Assert.IsType<ReactionWindowClosed>(
            CampaignSuccessorEventSerializer.Deserialize(bytes));

        var projected = CampaignV10Projector.ApplyReactionClose(
            fixture.Snapshot,
            roundTripped,
            fixture.Artifact,
            fixture.Scenario);

        Assert.Equal(bytes, CampaignSuccessorEventSerializer.Serialize(roundTripped));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyReactionClose(
                projected,
                roundTripped,
                fixture.Artifact,
                fixture.Scenario));
        Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(bytes));
    }

    [Fact]
    public void ReplayRejectsReasonActionSemanticTampering()
    {
        var fixture = CreateOpenedWindow(["commonwealth-reactor-alpha"]);
        var intent = CreateIntent(
            fixture.Snapshot,
            CampaignReactionCloseIntentKind.Timeout);
        var canonical = CampaignReactionWindowClosedFactory.Create(
            fixture.Snapshot,
            fixture.Artifact,
            fixture.Scenario,
            intent);
        var forged = new ReactionWindowClosed(
            canonical.CampaignId,
            canonical.StateVersion,
            canonical.PriorStateVersion,
            canonical.FromPositionId,
            canonical.ActingSide,
            canonical.ActionId,
            canonical.SubmittedWindowId,
            canonical.WindowId,
            CampaignReactionWindowCloseReason.ScriptedUnavailable,
            canonical.ClosedOpportunityIds,
            canonical.ResumedSequencePosition);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyReactionClose(
                fixture.Snapshot,
                forged,
                fixture.Artifact,
                fixture.Scenario,
                (_, _, _, input) => CampaignReactionWindowClosedFactory.Create(
                    fixture.Snapshot,
                    fixture.Artifact,
                    fixture.Scenario,
                    input)));
    }

    [Fact]
    public void EventReaderRejectsNonCanonicalCloseShapes()
    {
        var fixture = CreateOpenedWindow(["commonwealth-reactor-alpha"]);
        var closed = CampaignReactionWindowClosedFactory.Create(
            fixture.Snapshot,
            fixture.Artifact,
            fixture.Scenario,
            CreateIntent(fixture.Snapshot, CampaignReactionCloseIntentKind.Timeout));
        var canonical = Encoding.UTF8.GetString(
            CampaignSuccessorEventSerializer.Serialize(closed));

        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(canonical.Replace(
                "{\"contractVersion\":1,",
                "{\"contractVersion\":1,\"unexpected\":true,",
                StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(canonical.Replace(
                "\"eventType\":\"reaction-window-closed\",",
                string.Empty,
                StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(canonical.Replace(
                "{\"contractVersion\":1,",
                "{\"contractVersion\":1,\"contractVersion\":1,",
                StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(canonical.Replace(
                "{\"contractVersion\":1,\"eventType\":\"reaction-window-closed\",",
                "{\"eventType\":\"reaction-window-closed\",\"contractVersion\":1,",
                StringComparison.Ordinal))));
    }

    private static CloseFixture CreateOpenedWindow(
        IReadOnlyList<string> reactorIds,
        bool includeReactionExit = true,
        string? reactorClassificationId = null)
    {
        var source = CampaignV10TestData.CreateWithReactors(
            reactorIds,
            includeReactionExit,
            reactorClassificationId,
            reactorLocationId: "north");
        var moved = CampaignElementMovedV2Factory.Create(
            source.MovementSnapshot,
            source.Artifact,
            source.Scenario,
            source.TriggeringMove.ToReplayInput());
        var snapshot = CampaignV10Projector.ApplyMovement(
            source.MovementSnapshot,
            moved,
            source.Artifact,
            source.Scenario);
        return new CloseFixture(source.Artifact, source.Scenario, snapshot);
    }

    private static CloseReactionWindowIntent CreateIntent(
        CampaignSnapshotV10 snapshot,
        CampaignReactionCloseIntentKind closeKind)
    {
        var window = snapshot.ReactionWindow!;
        var publicWindowId = PublicWindowId(snapshot);
        ReactionWindowAction action = closeKind switch
        {
            CampaignReactionCloseIntentKind.PlayerDecline =>
                new DeclineReactionWindowAction(publicWindowId),
            CampaignReactionCloseIntentKind.ScriptedUnavailable =>
                new CloseReactionWindowUnavailableAction(publicWindowId),
            CampaignReactionCloseIntentKind.Timeout =>
                new CloseReactionWindowTimeoutAction(publicWindowId),
            CampaignReactionCloseIntentKind.NoEligibleReactor =>
                new CloseReactionWindowNoEligibleAction(publicWindowId),
            _ => throw new ArgumentOutOfRangeException(nameof(closeKind)),
        };
        return new CloseReactionWindowIntent(
            snapshot.StateVersion,
            window.ReactingPosition.SuspendedMovementPosition.PositionId,
            closeKind == CampaignReactionCloseIntentKind.PlayerDecline
                ? window.ReactingSide
                : null,
            action.ActionId,
            publicWindowId,
            closeKind);
    }

    private static string PublicWindowId(CampaignSnapshotV10 snapshot)
    {
        var window = snapshot.ReactionWindow!;
        return CampaignObservationV6DisclosureIdentity.CreateWindow(
            snapshot.CampaignId,
            snapshot.RulesetHash,
            window.TriggerCommittedStateVersion,
            window.ReactingSide);
    }

    private static CampaignSnapshotV10 WithWindowState(
        CampaignSnapshotV10 snapshot,
        IReadOnlyList<CampaignReactionOpportunityId> resolvedOpportunityIds,
        CampaignReactionOpportunityId? activeOpportunityId,
        long stateVersion,
        CampaignWorldSnapshotV5? world = null)
    {
        var current = snapshot.ReactionWindow!;
        var window = new CampaignReactionWindow(
            current.WindowId,
            current.TriggerCommittedStateVersion,
            current.PhasingSide,
            current.ReactingSide,
            current.ReactingPosition,
            current.TriggerAuthority,
            current.ApparentTrigger,
            current.FrozenOpportunities,
            resolvedOpportunityIds,
            activeOpportunityId);
        return new CampaignSnapshotV10(
            snapshot.ContractVersion,
            snapshot.CampaignId,
            stateVersion,
            snapshot.RulesetHash,
            snapshot.Setup,
            world ?? snapshot.World,
            snapshot.InitiativeHolder,
            snapshot.OperationStageOrders,
            snapshot.OperationStageWeather,
            snapshot.RandomState,
            CampaignPositionV10.FromReaction(window.ReactingPosition),
            window);
    }

    private static CampaignSnapshotV10 WithActiveParticipantAtCenter(
        CampaignSnapshotV10 snapshot,
        CampaignReactionOpportunityId opportunityId,
        long stateVersion)
    {
        var opportunity = snapshot.ReactionWindow!.FrozenOpportunities.Single(value =>
            value.OpportunityId == opportunityId);
        var elementId = Assert.Single(opportunity.ReactingRepresentation.BoundElementIds);
        var representationId = opportunity.ReactingRepresentation.RepresentationId;
        var world = new CampaignWorldSnapshotV5(
            CampaignWorldSnapshotV5.CurrentContractVersion,
            snapshot.World.Elements.Select(element => string.Equals(
                    element.ElementId,
                    elementId,
                    StringComparison.Ordinal)
                ? new CampaignElementStateV5(
                    element.ElementId,
                    "center",
                    element.ReserveStatus,
                    new CampaignElementOperationalStateV5(
                        element.OperationalState.LedgerGameTurn,
                        element.OperationalState.LedgerOperationStage,
                        element.OperationalState.CapabilityPointsExpended
                            + new CapabilityPointAmount(1, 1),
                        element.OperationalState.CohesionLevel,
                        element.OperationalState.VehicleBreakdownState,
                        element.OperationalState.MovementEnded),
                    element.Components)
                : element),
            snapshot.World.Representations.Select(representation => string.Equals(
                    representation.RepresentationId,
                    representationId,
                    StringComparison.Ordinal)
                ? new CampaignMapRepresentationState(
                    representation.RepresentationId,
                    "center",
                    representation.BindingKind,
                    representation.BoundElementIds)
                : representation));
        return WithWindowState(snapshot, [], opportunityId, stateVersion, world);
    }

    private sealed record CloseFixture(
        Cna.Core.Content.ContentPackV5Artifact Artifact,
        Cna.Core.Content.ContentScenario Scenario,
        CampaignSnapshotV10 Snapshot);
}
