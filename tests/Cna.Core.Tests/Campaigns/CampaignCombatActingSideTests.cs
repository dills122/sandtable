using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignCombatActingSideTests
{
    private static (CampaignSnapshotV11 Snapshot, CampaignContentContext Context, CampaignContactJournal Contacts) ReachCombat()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var current = CampaignCreationV10Factory.CreateSnapshot(BreakdownPreambleReplayTests.Create(), artifact, scenario);
        foreach (var candidate in BreakdownPreambleReplayTests.Actions())
        {
            var action = candidate is ActFirstAction ? new ActLastAction(1) : candidate;
            var recorded = CampaignV11Preamble.Create(current, artifact, scenario, action);
            current = CampaignV11Preamble.Apply(current, recorded, artifact, scenario);
        }
        var movement = CampaignBreakdownLifecycleFactory.CreateMovementCompletion(current, artifact, scenario);
        current = CampaignV11BreakdownProjector.Apply(current, movement, artifact, scenario);
        var breakdown = CampaignBreakdownLifecycleFactory.CreateBreakdownCompletion(current, artifact, scenario,
            CampaignBreakdownLifecycleFactory.CreateBreakdownCompletionActionId());
        current = CampaignV11BreakdownProjector.Apply(current, breakdown, artifact, scenario);
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        return (current, context, CampaignContactJournal.Initialize(current, context));
    }

    [Fact]
    public void ActLastUsesFirstActingSideForCombatAndReplaysVersionedDecision()
    {
        var (snapshot, context, contacts) = ReachCombat();
        Assert.Equal(LandSide.Axis, snapshot.InitiativeHolder);
        Assert.Equal(LandSide.Commonwealth, Assert.Single(snapshot.OperationStageOrders).FirstSide);
        var cut = CampaignContactCombatSelectionCut.Project(snapshot, context, contacts,
            LandSide.Commonwealth, "commonwealth-infantry");
        Assert.Throws<InvalidOperationException>(() => CampaignContactCombatSelectionCut.Project(snapshot,
            context, contacts, LandSide.Axis, "axis-infantry"));
        var action = cut.AssaultOffers.Select(value => value.ActionId).FirstOrDefault()
            ?? cut.FinishOffer?.ActionId;
        Assert.NotNull(action);
        var selected = CampaignNativeCombatDecisionStateV12.Select(snapshot, context, contacts,
            LandSide.Commonwealth, "commonwealth-infantry", action);
        Assert.Equal(39, selected.Decision.ContractVersion);
        Assert.Equal(LandSide.Commonwealth, selected.Decision.Side);
        var bytes = selected.Decision.SerializeCanonical();
        Assert.Equal(bytes, CampaignNativeCombatDecisionStateV12.Replay(snapshot, context, contacts, bytes)
            .Decision.SerializeCanonical());
        Assert.Equal(bytes, CampaignCurrentEventSerializer.Serialize(CampaignCurrentEventSerializer.Deserialize(bytes)));
        var forged = selected.Decision with { Side = LandSide.Axis, ActingElementId = "axis-infantry" };
        Assert.Throws<InvalidOperationException>(() => CampaignNativeCombatDecisionStateV12.Replay(snapshot,
            context, contacts, forged.SerializeCanonical()));
    }

    [Fact]
    public void RetainedLegacyDecisionReplaysWithItsOriginalOwnerPolicy()
    {
        var (snapshot, context, contacts) = ReachCombat();
        var legacyCut = CampaignContactCombatSelectionCut.Project(snapshot, context, contacts,
            LandSide.Axis, "axis-infantry", legacyInitiativeOwner: true);
        Assert.NotNull(legacyCut.FinishOffer);
        var position = snapshot.CurrentPosition.SequencePosition!;
        var legacy = new CampaignNativeCombatDecisionV12(12, snapshot.CampaignId, snapshot.StateVersion,
            position.PositionId, Cna1979LandSequenceV4.GetNext(position).PositionId,
            "finish-combat-without-attack", LandSide.Axis, "axis-infantry", null, null, null,
            legacyCut.FinishOffer.ActionId);
        var bytes = legacy.SerializeCanonical();
        Assert.Equal(bytes, CampaignNativeCombatDecisionStateV12.Replay(snapshot, context, contacts, bytes)
            .Decision.SerializeCanonical());
    }

    [Fact]
    public void HostImportReplaysRetainedLegacyDecisionButLiveSubmissionRejectsIt()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(
            value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        var request = new CampaignCreationRequest(1, "campaign-legacy-act-last", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash,
            setup.Content.ScenarioId);
        var started = CampaignHostAdapter.Create(request);
        Assert.True(started.IsStarted);
        var session = started.Session!;
        var steps = new List<CampaignHostAcceptedStep>();
        for (var index = 0; index < 30; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination)
                break;
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var action = set.Candidates.FirstOrDefault(value => value is ActLastAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteMovementSegmentAction)
                ?? set.Candidates[0];
            var command = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
                set.PositionId, set.Audience, action.ActionId);
            var accepted = CampaignHostAdapter.Submit(session, command);
            Assert.True(accepted.IsAccepted);
            steps.Add(ToStep(command, accepted));
            session = accepted.SuccessorSession!;
        }
        Assert.Equal(LandStepIds.PositionDetermination, session.CurrentSnapshot.CurrentPosition.SequenceContext.StepId);
        var oldTimeout = CampaignExercises.TimeoutNativeCombatSelection(session, null,
            -1, -1, -1, null, "selection-unavailable", legacyInitiativeOwner: true);
        Assert.True(oldTimeout.IsAccepted);
        var timeoutCommand = CampaignHostAdapter.NativeCombatSelectionTimeoutCommand(session, oldTimeout);
        var timeoutSteps = steps.Append(ToStep(timeoutCommand, oldTimeout,
            CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind)).ToArray();
        Assert.True(CampaignHostAdapter.Import(request, started.CreationEventBytes!,
            started.InitialSnapshotBytes!, timeoutSteps).IsVerified);
        var cut = CampaignContactCombatSelectionCut.Project(session.CurrentSnapshot, session.Context,
            session.CurrentContacts, LandSide.Axis, "axis-infantry", legacyInitiativeOwner: true);
        Assert.NotNull(cut.FinishOffer);
        var positionAtCombat = session.CurrentSnapshot.CurrentPosition.SequencePosition!;
        var oldEvent = new CampaignNativeCombatDecisionV12(12, request.CampaignId,
            session.CurrentSnapshot.StateVersion, positionAtCombat.PositionId,
            Cna1979LandSequenceV4.GetNext(positionAtCombat).PositionId,
            "finish-combat-without-attack", LandSide.Axis, "axis-infantry", null, null, null,
            cut.FinishOffer.ActionId);
        var submission = new CampaignActionSubmission(1, request.CampaignId, session.CurrentSnapshot.StateVersion,
            positionAtCombat.PositionId, CampaignActionAudience.Axis, cut.FinishOffer.ActionId);
        Assert.False(CampaignHostAdapter.SubmitContactOwnerCombatSelection(session, submission, "axis-infantry").IsAccepted);
        var legacy = CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, "axis-infantry",
            oldEvent.SerializeCanonical());
        Assert.True(legacy.IsAccepted);
        steps.Add(ToStep(submission, legacy, CampaignHostAcceptedStep.ContactCombatSelectionKind, "axis-infantry"));
        var imported = CampaignHostAdapter.Import(request, started.CreationEventBytes!, started.InitialSnapshotBytes!, steps);
        Assert.True(imported.IsVerified, imported.RejectionReason.ToString());
    }

    private static CampaignHostAcceptedStep ToStep(CampaignActionSubmission command, ExerciseStepResult result,
        string kind = CampaignHostAcceptedStep.LegacyKind, string? elementId = null)
    {
        var evidence = result.Evidence!;
        return new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(command),
            evidence.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt),
            evidence.SnapshotCheckpoint, kind, elementId);
    }

    [Fact]
    public void ActLastTimeoutUsesActingSideAndLegacyTimeoutStillReplays()
    {
        var (snapshot, context, contacts) = ReachCombat();
        var hash = CampaignSelectionTimeoutEvidenceV38.NoTimingConfigurationHash;
        var timeout = CampaignNativeCombatSelectionTimeoutV38.Create(snapshot, context, contacts,
            hash, -1, -1, -1, null, "selection-unavailable");
        Assert.Equal(40, timeout.ContractVersion);
        Assert.Equal(LandSide.Commonwealth, timeout.Evidence.Side);
        Assert.Equal(timeout.SerializeCanonical(), CampaignNativeCombatDecisionStateV12.Replay(snapshot,
            context, contacts, timeout.SerializeCanonical()).Decision.SerializeCanonical());
        var legacy = CampaignNativeCombatSelectionTimeoutV38.Create(snapshot, context, contacts,
            hash, -1, -1, -1, null, "selection-unavailable", legacyInitiativeOwner: true);
        Assert.Equal(38, legacy.ContractVersion);
        Assert.Equal(LandSide.Axis, legacy.Evidence.Side);
        Assert.Equal(legacy.SerializeCanonical(), CampaignNativeCombatDecisionStateV12.Replay(snapshot,
            context, contacts, legacy.SerializeCanonical()).Decision.SerializeCanonical());
    }

    [Fact]
    public void CommonwealthHolderCanChooseAxisFirstForCombat()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(
            value => value.SetupId == "rules-lab.breakdown.battalion-contested.v1"));
        var seed = Enumerable.Range(1, 256).Select(value => (ulong)value).First(value =>
            InitiativeResolver.Resolve(setup.InitialGameTurn, setup.InitialInitiative, SandtableRandom.Create(value),
                setup.Sources).Outcome.Holder == LandSide.Commonwealth);
        var request = new CampaignCreationRequest(1, "campaign-commonwealth-holder", Cna1979Ruleset.Manifest.Hash,
            seed, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash,
            setup.Content.ScenarioId);
        var started = CampaignExercises.Begin(request);
        Assert.True(started.IsStarted);
        var session = started.Session!;
        for (var index = 0; index < 30; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination)
                break;
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var action = set.Candidates.FirstOrDefault(value => value is ActLastAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteMovementSegmentAction)
                ?? set.Candidates[0];
            var result = CampaignExercises.Submit(session, new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, action.ActionId));
            Assert.True(result.IsAccepted);
            session = result.SuccessorSession!;
        }
        Assert.Equal(LandSide.Commonwealth, session.CurrentSnapshot.InitiativeHolder);
        Assert.Equal(LandSide.Axis, Assert.Single(session.CurrentSnapshot.OperationStageOrders).FirstSide);
        var cut = CampaignContactCombatSelectionCut.Project(session.CurrentSnapshot, session.Context,
            session.CurrentContacts, LandSide.Axis, "axis-battalion-a");
        Assert.NotNull(cut.FinishOffer);
        Assert.Throws<InvalidOperationException>(() => CampaignContactCombatSelectionCut.Project(session.CurrentSnapshot,
            session.Context, session.CurrentContacts, LandSide.Commonwealth, "commonwealth-battalion-a"));
        var selected = CampaignNativeCombatDecisionStateV12.Select(session.CurrentSnapshot, session.Context,
            session.CurrentContacts, LandSide.Axis, "axis-battalion-a", cut.FinishOffer.ActionId);
        Assert.Equal(39, selected.Decision.ContractVersion);
    }
}
