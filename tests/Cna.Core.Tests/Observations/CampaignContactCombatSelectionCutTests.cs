using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Observations;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Tests.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignContactCombatSelectionCutTests
{
    [Fact]
    public void NormalTenToeAssaultBindsBothSealsAndDebitsPinnedAmmunition()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
                value.SetupId == "rules-lab.breakdown.reaction.combat-assault.v1"));
        var request = new CampaignCreationRequest(1, "campaign-native-positive-assault",
            Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
        var started = CampaignExercises.Begin(request);
        Assert.True(started.IsStarted);
        var session = started.Session!;
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.PhaseId == LandPhaseIds.MovementAndCombat
                && position.SegmentId == LandSegmentIds.Combat
                && position.StepId == LandStepIds.PositionDetermination)
                break;
            var actionSet = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .FirstOrDefault(value => value?.Candidates.Count > 0);
            Assert.NotNull(actionSet);
            var action = position.SegmentId == LandSegmentIds.Movement
                ? actionSet.Candidates.OfType<CompleteMovementSegmentAction>().FirstOrDefault()
                    ?? actionSet.Candidates[0]
                : actionSet.Candidates[0];
            var command = new CampaignActionSubmission(
                CampaignActionSubmission.CurrentContractVersion, actionSet.CampaignId,
                actionSet.StateVersion, actionSet.PositionId, actionSet.Audience,
                action.ActionId);
            var accepted = CampaignExercises.Submit(session, command);
            Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
            session = accepted.SuccessorSession!;
        }
        var atCombat = session.CurrentSnapshot;
        Assert.Equal(LandStepIds.PositionDetermination,
            atCombat.CurrentPosition.SequenceContext.StepId);
        var context = session.Context;
        Assert.Equal("sha256:96b408f10d836eaa6f5f0314857d23f5dfd87dd5e15cfbd83d17625a3237058c",
            context.CombatRetreatSource!.Hash);
        Assert.Equal("west", context.CombatRetreatSource.Anchor("axis"));
        Assert.Equal("east", context.CombatRetreatSource.Anchor("commonwealth"));
        var contacts = session.CurrentContacts;
        var offer = Assert.Single(CampaignContactCombatSelectionCut.Project(atCombat,
            context, contacts, LandSide.Axis, "axis-infantry").AssaultOffers);
        var selected = CampaignNativeCombatDecisionStateV12.Select(atCombat,
            context, contacts, LandSide.Axis, "axis-infantry", offer.ActionId);
        contacts = contacts.AdvanceNativeCombatSelection(selected);
        var barrage = CampaignNativeCombatBarrageV14.Create(selected, context);
        contacts = contacts.AdvanceNativeCombatBarrage(barrage);
        var declineOffer = CampaignContactRetreatBeforeAssaultCut.Project(selected,
            barrage, context, contacts).DeclineOffer;
        var decline = CampaignNativeCombatRetreatDeclineV15.Create(selected,
            barrage, context, contacts, declineOffer.ActionId);
        contacts = contacts.AdvanceNativeCombatRetreatDecline(decline);
        var attackerOffer = CampaignContactForceAssignmentCut.Project(selected,
            decline, atCombat.World, context, contacts, LandSide.Axis).SealOffer;
        var attackerSeal = CampaignNativeCombatForceSealV17.Create(selected,
            decline, atCombat.World, context, contacts, LandSide.Axis,
            attackerOffer.ActionId);
        contacts = contacts.AdvanceNativeCombatForceSeal(attackerSeal);
        var defenderOffer = CampaignContactForceAssignmentCut.Project(selected,
            decline, atCombat.World, context, contacts, LandSide.Commonwealth).SealOffer;
        var defenderSeal = CampaignNativeCombatForceSealV17.Create(selected,
            decline, atCombat.World, context, contacts, LandSide.Commonwealth,
            defenderOffer.ActionId, attackerSeal);
        contacts = contacts.AdvanceNativeCombatForceSeal(defenderSeal);
        var completion = CampaignNativeCombatForceCompletionV18.Create(selected,
            decline, [attackerSeal, defenderSeal]);
        contacts = contacts.AdvanceNativeCombatForceCompletion(completion);
        var antiArmor = CampaignNativeCombatAntiArmorV20.Create(selected,
            completion, null, context);
        contacts = contacts.AdvanceNativeCombatAntiArmor(antiArmor);
        var ammunition = CampaignCombatAmmunitionLedgerV1.CreateInitial(context);

        var inputs = CampaignNativeCombatAssaultInputs.Create(selected, completion,
            antiArmor, [attackerSeal, defenderSeal], atCombat.World, context, ammunition);

        Assert.Equal(10, inputs.Attacker.CommittedToe);
        Assert.Equal(10, inputs.Defender.CommittedToe);
        Assert.Equal(0, inputs.AfterAmmunition.Remaining("axis-infantry"));
        Assert.Equal(0, inputs.AfterAmmunition.Remaining("commonwealth-infantry"));
        Assert.Equal(context.CombatSource!.Hash, inputs.SourceHash);
        Assert.Equal(CampaignNativeCombatPaidWorldV22.Hash(atCombat.World),
            inputs.PriorWorldHash);
        Assert.Equal(CampaignNativeCombatPaidWorldV22.Hash(inputs.PaidWorld),
            inputs.PaidWorldHash);
        Assert.Equal(inputs.Attacker.CapabilityPointsAfter,
            inputs.PaidWorld.Elements.Single(value =>
                value.ElementId == inputs.Attacker.ElementId)
                .OperationalState.CapabilityPointsExpended);
        Assert.Equal(inputs.Defender.CapabilityPointsAfter,
            inputs.PaidWorld.Elements.Single(value =>
                value.ElementId == inputs.Defender.ElementId)
                .OperationalState.CapabilityPointsExpended);
        Assert.NotEqual(inputs.PriorWorldHash, inputs.PaidWorldHash);
        var result = CampaignNativeCombatAssaultResultV22.Create(selected,
            antiArmor, inputs, atCombat.RandomState);
        var canonical = result.SerializeCanonical();
        Assert.Equal(8 + (result.Facts.CapturedRole is null ? 0 : 1), result.Draws.Count);
        Assert.Equal(result.Draws[^1].AfterCursor,
            result.AfterRandomState.NextByteCursor);
        Assert.Equal(canonical, CampaignNativeCombatAssaultResultV22.Replay(
            selected, antiArmor, inputs, atCombat.RandomState, canonical)
            .SerializeCanonical());
        Assert.Equal(canonical, CampaignCurrentEventSerializer.Serialize(
            CampaignCurrentEventSerializer.Deserialize(canonical)));
        var losses = CampaignNativeCombatLossAssessment.Project(result, inputs,
            refuseRetreat: false);
        Assert.Equal(inputs.Attacker.CommittedToe,
            losses.Attacker.TotalLoss + losses.Attacker.RemainingToe);
        Assert.Equal(inputs.Defender.CommittedToe,
            losses.Defender.TotalLoss + losses.Defender.RemainingToe);
        if (result.Facts.RequiredRetreat == 1)
        {
            var retreatCut = CampaignContactRetreatAfterAssaultCut.Project(selected,
                result, inputs.PaidWorld, context,
                contacts.AdvanceNativeCombatAssaultResult(result));
            Assert.Equal(result.StateVersion, retreatCut.AuthorityVersion);
            Assert.Equal(context.CombatRetreatSource.Hash, retreatCut.RetreatSourceHash);
            Assert.NotEmpty(retreatCut.MoveOffers);
            Assert.All(retreatCut.MoveOffers, offer => Assert.NotEqual(
                retreatCut.Defender.CurrentLocationId, offer.DestinationLocationId));
            var refusal = CampaignNativeCombatLossAssessment.Project(result, inputs,
                refuseRetreat: true);
            Assert.Equal(losses.Attacker, refusal.Attacker);
            Assert.True(refusal.Defender.TotalLoss >= losses.Defender.TotalLoss);
            Assert.True(refusal.RefusedRetreat);
        }
        else
            Assert.Throws<ArgumentException>(() => CampaignNativeCombatLossAssessment.Project(
                result, inputs, refuseRetreat: true));
        // Exercise the source-directed route with a separately seeded Core result.
        // The retained campaign cursor is still verified by the replay assertion above.
        var routeResult = Enumerable.Range(1, 256).Select(seed =>
            CampaignNativeCombatAssaultResultV22.Create(selected, antiArmor, inputs,
                new RandomStreamState(atCombat.RandomState.ContractVersion,
                    atCombat.RandomState.AlgorithmId, (ulong)seed,
                    atCombat.RandomState.NextByteCursor)))
            .First(value => value.Facts.RequiredRetreat == 1);
        var routeCut = CampaignContactRetreatAfterAssaultCut.Project(selected,
            routeResult, inputs.PaidWorld, context,
            contacts.AdvanceNativeCombatAssaultResult(routeResult));
        Assert.Equal("east", Assert.Single(routeCut.MoveOffers).DestinationLocationId);
        Assert.Equal("commonwealth-infantry", routeCut.RefuseOffer.ElementId);
        var routeContacts = contacts.AdvanceNativeCombatAssaultResult(routeResult);
        var routeDecision = CampaignNativeCombatRetreatAfterAssaultV23.Create(
            selected, routeResult, inputs.PaidWorld, context, routeContacts,
            Assert.Single(routeCut.MoveOffers).ActionId);
        Assert.Equal("moved", routeDecision.Disposition);
        Assert.Equal("east", routeDecision.DestinationLocationId);
        Assert.Equal(routeDecision.SerializeCanonical(),
            CampaignNativeCombatRetreatAfterAssaultV23.Replay(selected, routeResult,
                inputs.PaidWorld, context, routeContacts,
                routeDecision.SerializeCanonical()).SerializeCanonical());
        Assert.Equal(routeDecision.SerializeCanonical(),
            CampaignCurrentEventSerializer.Serialize(CampaignCurrentEventSerializer.Deserialize(
                routeDecision.SerializeCanonical())));
        Assert.Equal(routeDecision.StateVersion, routeContacts
            .AdvanceNativeCombatRetreatAfterAssault(routeDecision).AuthorityVersion);
        var refusalDecision = CampaignNativeCombatRetreatAfterAssaultV23.Create(
            selected, routeResult, inputs.PaidWorld, context, routeContacts,
            routeCut.RefuseOffer.ActionId);
        Assert.Equal("refused", refusalDecision.Disposition);
        Assert.Null(refusalDecision.DestinationLocationId);
        Assert.Throws<InvalidOperationException>(() =>
            CampaignNativeCombatRetreatAfterAssaultV23.Create(selected,
                routeResult, inputs.PaidWorld, context, routeContacts,
                "sha256:" + new string('0', 64)));
        var forgedDecision = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(
            routeDecision.SerializeCanonical()).Replace(routeDecision.ResultHash,
                "sha256:" + new string('0', 64), StringComparison.Ordinal));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCombatRetreatAfterAssaultV23.Replay(selected,
                routeResult, inputs.PaidWorld, context, routeContacts,
                forgedDecision));
        using (var visible = JsonDocument.Parse(
                   CampaignContactRetreatAfterAssaultOwnerVisibleCutV1.SerializeCanonical(
                       routeCut)))
        {
            Assert.Equal(routeCut.RetreatSourceHash,
                visible.RootElement.GetProperty("retreatSourceHash").GetString());
            Assert.False(visible.RootElement.TryGetProperty("authorityVersion", out _));
            Assert.False(visible.RootElement.TryGetProperty("attackerElementId", out _));
            Assert.False(visible.RootElement.GetProperty("moveOffers")[0]
                .TryGetProperty("actionId", out _));
        }
        Assert.NotEmpty(result.SerializeStateCanonical(antiArmor.SerializeCanonical()));
        var replayedAmmunition = CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(
            context, [result]);
        Assert.Equal(inputs.AfterAmmunition.Hash, replayedAmmunition.Hash);
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(
                context, [result, result]));
        var positiveSession = new ExerciseSession(atCombat, context,
            session.CurrentHistory.AddRange(new object[] { selected.Decision,
                barrage, decline, attackerSeal, defenderSeal, completion, antiArmor }),
            contacts, selected, combatBarrage: barrage,
            combatRetreatDecline: decline,
            combatForceSeals: [attackerSeal, defenderSeal],
            combatForceCompletion: completion, combatAntiArmor: antiArmor);
        var acceptedResult = CampaignExercises.ResolveNativeCombatAssault(positiveSession);
        Assert.True(acceptedResult.IsAccepted, acceptedResult.RejectionReason.ToString());
        Assert.Equal(canonical, Assert.Single(acceptedResult.Evidence!.EventRecords));
        Assert.Equal(inputs.PaidWorld,
            acceptedResult.SuccessorSession!.CurrentCombatPaidWorld);
        Assert.True(CampaignExercises.Reconstruct(acceptedResult.SuccessorSession!).IsVerified);
        ExerciseStepResult postResult;
        if (result.Facts.RequiredRetreat == 1)
        {
            var cut = CampaignExercises.QueryContactPostResultRetreatCut(
                acceptedResult.SuccessorSession);
            var choice = Assert.Single(cut.MoveOffers);
            postResult = CampaignExercises.SubmitContactPostResultRetreat(
                acceptedResult.SuccessorSession,
                new CampaignActionSubmission(
                    CampaignActionSubmission.CurrentContractVersion,
                    result.CampaignId, result.StateVersion, result.PositionId,
                    CampaignActionAudience.Commonwealth, choice.ActionId),
                cut.DefenderElementId);
        }
        else
            postResult = CampaignExercises.CompleteNoPostResultRetreat(
                acceptedResult.SuccessorSession);
        Assert.True(postResult.IsAccepted, postResult.RejectionReason.ToString());
        Assert.Equal(23, CampaignExercises.QueryCheckpoint(
            postResult.SuccessorSession!).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(postResult.SuccessorSession!).IsVerified);
        Assert.Throws<ArgumentException>(() => new ExerciseSession(atCombat, context,
            acceptedResult.SuccessorSession.CurrentHistory,
            acceptedResult.SuccessorSession.CurrentContacts, selected,
            combatBarrage: barrage, combatRetreatDecline: decline,
            combatForceSeals: [attackerSeal, defenderSeal],
            combatForceCompletion: completion, combatAntiArmor: antiArmor,
            combatAssaultResult: result, combatPaidWorld: atCombat.World));
        var tampered = canonical.ToArray();
        tampered[^2] ^= 1;
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCombatAssaultResultV22.Replay(selected,
                antiArmor, inputs, atCombat.RandomState, tampered));
        Assert.Throws<InvalidOperationException>(() => CampaignNativeCombatAssaultInputs.Create(
            selected, completion, antiArmor, [attackerSeal, defenderSeal],
            atCombat.World, context, inputs.AfterAmmunition));
    }

    [Fact]
    public void AcceptedContactSelectionRetainsReceiptAndReplaysFromOrdinaryExercise()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
                value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        var request = new CampaignCreationRequest(1, "campaign-native-combat",
            Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
        var started = CampaignExercises.Begin(request);
        Assert.True(started.IsStarted);
        var session = started.Session!;
        var retainedSteps = new List<CampaignHostAcceptedStep>();
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.PhaseId == LandPhaseIds.MovementAndCombat
                && position.SegmentId == LandSegmentIds.Combat
                && position.StepId == LandStepIds.PositionDetermination)
                break;
            var actionSet = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .FirstOrDefault(value => value?.Candidates.Count > 0);
            Assert.NotNull(actionSet);
            var action = position.SegmentId == LandSegmentIds.Movement
                ? actionSet.Candidates.OfType<MoveElementAction>().FirstOrDefault(value =>
                    value.ElementId == "axis-infantry" && value.DestinationLocationId == "north")
                    ?? actionSet.Candidates[0]
                : actionSet.Candidates[0];
            var command = new CampaignActionSubmission(
                CampaignActionSubmission.CurrentContractVersion, actionSet.CampaignId,
                actionSet.StateVersion, actionSet.PositionId, actionSet.Audience,
                action.ActionId);
            var accepted = CampaignExercises.Submit(session, command);
            Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
            retainedSteps.Add(new CampaignHostAcceptedStep(
                CampaignActionSubmissionSerializer.SerializeCanonical(command),
                accepted.Evidence!.EventRecords,
                CampaignActionAcceptanceReceiptSerializer.Serialize(accepted.Evidence.Receipt),
                accepted.Evidence.SnapshotCheckpoint));
            session = accepted.SuccessorSession!;
        }
        Assert.Equal(LandStepIds.PositionDetermination,
            session.CurrentSnapshot.CurrentPosition.SequenceContext.StepId);
        var actor = session.CurrentSnapshot.World.Elements
            .First(value => value.ElementId == "axis-infantry");
        var cut = CampaignExercises.QueryContactOwnerCombatCut(session, LandSide.Axis,
            actor.ElementId);
        Assert.True(cut.MayFinishWithoutAttack);
        Assert.True(cut.AssaultOffers.Count > 0,
            string.Join(",", session.CurrentSnapshot.World.Elements.Select(value =>
                $"{value.ElementId}:{value.CurrentLocationId}"))
            + " targets=" + string.Join(",", cut.OwnerCut.ContactObservation.CurrentTargets.Select(value =>
                value.ApparentLocationId)));
        var offer = Assert.Single(cut.AssaultOffers);
        var submission = new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion, session.CurrentSnapshot.CampaignId,
            session.CurrentSnapshot.StateVersion,
            CampaignCurrentActionExecution.CurrentPositionId(session.CurrentSnapshot),
            CampaignActionAudience.Axis, offer.ActionId);
        var acceptedCombat = CampaignHostAdapter.SubmitContactOwnerCombatSelection(
            session, submission, actor.ElementId);
        Assert.True(acceptedCombat.IsAccepted, acceptedCombat.RejectionReason.ToString());
        var successor = acceptedCombat.SuccessorSession!;
        Assert.Equal(session.CurrentSnapshot.StateVersion + 1,
            CampaignExercises.QueryCheckpoint(successor).StateVersion);
        Assert.True(CampaignExercises.Reconstruct(successor).IsVerified);
        Assert.Equal(CampaignHostQueryRejectionReason.InvalidState,
            CampaignHostAdapter.QueryAudience(successor, CampaignActionAudience.Axis).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.Submit(successor, submission).RejectionReason);
        retainedSteps.Add(new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(submission),
            acceptedCombat.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(acceptedCombat.Evidence.Receipt),
            acceptedCombat.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactCombatSelectionKind, actor.ElementId));
        var imported = CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, retainedSteps);
        Assert.True(imported.IsVerified, imported.RejectionReason.ToString());
        Assert.Equal(acceptedCombat.Evidence.SnapshotCheckpoint,
            imported.Session!.CurrentCombatDecision!.SerializeCanonical());
        Assert.True(CampaignExercises.Reconstruct(imported.Session).IsVerified);
        var barrage = CampaignHostAdapter.CompleteNativeCombatBarrage(imported.Session);
        Assert.True(barrage.IsAccepted, barrage.RejectionReason.ToString());
        Assert.True(CampaignExercises.Reconstruct(barrage.SuccessorSession!).IsVerified);
        var rbaSession = barrage.SuccessorSession!;
        var rba = CampaignContactRetreatBeforeAssaultCut.Project(
            rbaSession.CurrentCombatDecision!, rbaSession.CurrentCombatBarrage!,
            rbaSession.Context, rbaSession.CurrentContacts);
        Assert.Equal(LandSide.Commonwealth, rba.DefenderSide);
        Assert.Equal(rbaSession.CurrentCombatBarrage!.StateVersion,
            rba.AuthorityVersion);
        Assert.True(rba.InitiallyAdjacentToKnownEnemy);
        Assert.Equal("decline-retreat-before-assault", rba.DeclineOffer.Kind);
        Assert.NotEmpty(rba.MoveOffers);
        Assert.All(rba.MoveOffers, move =>
            Assert.DoesNotContain(rba.ContactObservation.CurrentTargets,
                target => target.ApparentLocationId == move.DestinationLocationId));
        var firstMove = rba.MoveOffers[0];
        var projectedRetreat = CampaignNativeCombatRetreatMovement.ProjectFirst(
            rbaSession.CurrentCombatDecision!, rbaSession.CurrentCombatBarrage!,
            rbaSession.Context, rbaSession.CurrentContacts, firstMove.ActionId);
        Assert.Equal(firstMove.DestinationLocationId,
            projectedRetreat.World.Elements.Single(value =>
                value.ElementId == rba.DefenderElementId).CurrentLocationId);
        Assert.Equal(firstMove.DestinationLocationId,
            projectedRetreat.World.Representations.Single(value =>
                value.BoundElementIds.Contains(rba.DefenderElementId)).CurrentLocationId);
        Assert.Equal(firstMove.CostBreakdown, projectedRetreat.Movement.ActionCost);
        Assert.Equal(rba.InitialLocationId,
            rbaSession.CurrentSnapshot.World.Elements.Single(value =>
                value.ElementId == rba.DefenderElementId).CurrentLocationId);
        var moveEvent = CampaignNativeCombatRetreatMoveV16.CreateFirst(
            rbaSession.CurrentCombatDecision!, rbaSession.CurrentCombatBarrage!,
            rbaSession.Context, rbaSession.CurrentContacts, firstMove.ActionId);
        Assert.Equal(rba.PositionId, moveEvent.PositionId);
        Assert.Equal(projectedRetreat.World,
            moveEvent.ProjectWorld(rbaSession.CurrentCombatDecision!,
                rbaSession.CurrentCombatBarrage!, rbaSession.Context,
                rbaSession.CurrentContacts));
        Assert.Equal(moveEvent, CampaignNativeCombatRetreatMoveV16.ReplayFirst(
            rbaSession.CurrentCombatDecision!, rbaSession.CurrentCombatBarrage!,
            rbaSession.Context, rbaSession.CurrentContacts, moveEvent.SerializeCanonical()));
        Assert.Equal(moveEvent, CampaignCurrentEventSerializer.Deserialize(
            CampaignCurrentEventSerializer.Serialize(moveEvent)));
        var movedContacts = rbaSession.CurrentContacts.AdvanceNativeCombatRetreatMove(
            rbaSession.CurrentSnapshot.World, projectedRetreat.World, moveEvent,
            rbaSession.CurrentCombatDecision!.Decision.BoundRepresentationId!,
            rbaSession.Context);
        Assert.Equal(moveEvent.StateVersion, movedContacts.AuthorityVersion);
        var afterMove = CampaignContactRetreatBeforeAssaultCut.ProjectAfterFirstMove(
            rbaSession.CurrentCombatDecision!, rbaSession.CurrentCombatBarrage!,
            moveEvent, projectedRetreat.World, rbaSession.Context, movedContacts);
        Assert.Equal(moveEvent.StateVersion, afterMove.AuthorityVersion);
        Assert.Equal(firstMove.DestinationLocationId, afterMove.Defender.CurrentLocationId);
        Assert.Equal(rba.InitialLocationId, afterMove.InitialLocationId);
        Assert.Equal(rba.InitiallyAdjacentToKnownEnemy,
            afterMove.InitiallyAdjacentToKnownEnemy);
        Assert.NotEmpty(afterMove.MoveOffers);
        Assert.NotEmpty(moveEvent.SerializeStateCanonical(rbaSession.CurrentCombatDecision!,
            rbaSession.CurrentCombatBarrage!, rbaSession.Context, rbaSession.CurrentContacts));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCombatRetreatMoveV16.ReplayFirst(
                rbaSession.CurrentCombatDecision!, rbaSession.CurrentCombatBarrage!,
                rbaSession.Context, rbaSession.CurrentContacts,
                (moveEvent with { DestinationLocationId = rba.InitialLocationId }).SerializeCanonical()));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignNativeCombatRetreatMovement.ProjectFirst(
                rbaSession.CurrentCombatDecision!, rbaSession.CurrentCombatBarrage!,
                rbaSession.Context, rbaSession.CurrentContacts, "act.forged"));
        var moveSubmission = new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion, rbaSession.CurrentSnapshot.CampaignId,
            rba.AuthorityVersion, rba.PositionId, CampaignActionAudience.Commonwealth,
            firstMove.ActionId);
        var acceptedMove = CampaignExercises.SubmitContactDefenderRbaMove(
            rbaSession, moveSubmission, rba.DefenderElementId);
        Assert.True(acceptedMove.IsAccepted, acceptedMove.RejectionReason.ToString());
        Assert.True(CampaignExercises.Reconstruct(acceptedMove.SuccessorSession!).IsVerified);
        Assert.Equal(rba.PositionId,
            CampaignExercises.QueryCheckpoint(acceptedMove.SuccessorSession!).PositionId);
        Assert.Equal(afterMove.Defender.CurrentLocationId,
            CampaignExercises.QueryContactRbaCut(acceptedMove.SuccessorSession!)
                .Defender.CurrentLocationId);
        var repeatSubmission = new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion, rbaSession.CurrentSnapshot.CampaignId,
            afterMove.AuthorityVersion, afterMove.PositionId,
            CampaignActionAudience.Commonwealth, afterMove.MoveOffers[0].ActionId);
        var repeated = CampaignExercises.SubmitContactDefenderRbaMove(
            acceptedMove.SuccessorSession!, repeatSubmission, rba.DefenderElementId);
        Assert.True(repeated.IsAccepted, repeated.RejectionReason.ToString());
        Assert.True(CampaignExercises.Reconstruct(repeated.SuccessorSession!).IsVerified);
        Assert.Equal(afterMove.MoveOffers[0].DestinationLocationId,
            CampaignExercises.QueryContactRbaCut(repeated.SuccessorSession!)
                .Defender.CurrentLocationId);
        var afterMoveCut = CampaignExercises.QueryContactRbaCut(acceptedMove.SuccessorSession!);
        var stopAfterMoveSubmission = new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion,
            rbaSession.CurrentSnapshot.CampaignId, afterMoveCut.AuthorityVersion,
            afterMoveCut.PositionId, CampaignActionAudience.Commonwealth,
            afterMoveCut.DeclineOffer.ActionId);
        var stopAfterMove = CampaignExercises.SubmitContactDefenderRbaDecline(
            acceptedMove.SuccessorSession!, stopAfterMoveSubmission, rba.DefenderElementId);
        Assert.True(stopAfterMove.IsAccepted, stopAfterMove.RejectionReason.ToString());
        Assert.True(CampaignExercises.Reconstruct(stopAfterMove.SuccessorSession!).IsVerified);
        Assert.Equal(LandStepIds.ForceAssignment,
            Cna1979LandSequenceV4.CreateTurn(
                rbaSession.CurrentSnapshot.CurrentPosition.SequencePosition!.GameTurn)
                .Single(position => position.PositionId ==
                    CampaignExercises.QueryCheckpoint(stopAfterMove.SuccessorSession!).PositionId)
                .StepId);
        var defenderDraft = CampaignHostAdapter.QueryContactRbaOwnerDraft(rbaSession,
            CampaignActionAudience.Commonwealth, rba.DefenderElementId);
        Assert.Equal(rba.AuthorityVersion, defenderDraft.AuthorityVersion);
        Assert.Equal(rba.MoveOffers.Count + 1, defenderDraft.PrivateActionIds.Count);
        Assert.Equal("{}", JsonSerializer.Serialize(defenderDraft));
        var visibleRba = Encoding.UTF8.GetString(defenderDraft.VisibleBytes);
        Assert.DoesNotContain("authorityVersion", visibleRba, StringComparison.Ordinal);
        Assert.DoesNotContain("actionId", visibleRba, StringComparison.Ordinal);
        Assert.DoesNotContain("boundRepresentationId", visibleRba, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => CampaignHostAdapter.QueryContactRbaOwnerDraft(
            rbaSession, CampaignActionAudience.Axis, rba.DefenderElementId));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignContactRetreatBeforeAssaultCut.Project(
                rbaSession.CurrentCombatDecision!,
                rbaSession.CurrentCombatBarrage! with { NextPositionId = "forged-rba-position" },
                rbaSession.Context, rbaSession.CurrentContacts));
        var declineSubmission = new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion, rbaSession.CurrentSnapshot.CampaignId,
            rba.AuthorityVersion, rba.PositionId, CampaignActionAudience.Commonwealth,
            rba.DeclineOffer.ActionId);
        var acceptedDecline = CampaignHostAdapter.SubmitContactDefenderRbaDecline(
            rbaSession, declineSubmission, rba.DefenderElementId);
        Assert.True(acceptedDecline.IsAccepted, acceptedDecline.RejectionReason.ToString());
        Assert.True(CampaignExercises.Reconstruct(acceptedDecline.SuccessorSession!).IsVerified);
        Assert.Equal(LandStepIds.ForceAssignment,
            Cna1979LandSequenceV4.CreateTurn(
                rbaSession.CurrentSnapshot.CurrentPosition.SequencePosition!.GameTurn)
                .Single(position => position.PositionId ==
                    CampaignExercises.QueryCheckpoint(acceptedDecline.SuccessorSession!).PositionId)
                .StepId);
        var truckContact = acceptedDecline.SuccessorSession!;
        Assert.Throws<InvalidOperationException>(() =>
            CampaignContactForceAssignmentCut.Project(
                truckContact.CurrentCombatDecision!,
                truckContact.CurrentCombatRetreatDecline!,
                truckContact.CurrentSnapshot.World, truckContact.Context,
                truckContact.CurrentContacts, LandSide.Commonwealth));
        Assert.False(CampaignHostAdapter.SubmitContactDefenderRbaDecline(
            rbaSession, declineSubmission with { Audience = CampaignActionAudience.Axis },
            rba.DefenderElementId).IsAccepted);
        Assert.False(CampaignHostAdapter.SubmitContactDefenderRbaDecline(
            rbaSession, declineSubmission with { ActionId = rba.MoveOffers[0].ActionId },
            rba.DefenderElementId).IsAccepted);
        var barrageCommand = CampaignHostAdapter.NativeCombatBarrageCommand(imported.Session,
            barrage);
        var barrageSteps = retainedSteps.Append(new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(barrageCommand),
            barrage.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(barrage.Evidence.Receipt),
            barrage.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.NativeCombatBarrageKind)).ToArray();
        Assert.True(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, barrageSteps).IsVerified);
        var moveSteps = barrageSteps.Append(new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(moveSubmission),
            acceptedMove.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(acceptedMove.Evidence.Receipt),
            acceptedMove.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactRbaMoveKind, rba.DefenderElementId)).ToArray();
        Assert.True(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, moveSteps).IsVerified);
        var repeatedSteps = moveSteps.Append(new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(repeatSubmission),
            repeated.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(repeated.Evidence.Receipt),
            repeated.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactRbaMoveKind, rba.DefenderElementId)).ToArray();
        Assert.True(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, repeatedSteps).IsVerified);
        var forgedRepeat = repeated.SuccessorSession!.CurrentCombatRetreatMove! with
        {
            DestinationLocationId = repeated.SuccessorSession.CurrentCombatRetreatMove.OriginLocationId,
        };
        repeatedSteps[^1] = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(repeatSubmission),
            [forgedRepeat.SerializeCanonical()],
            CampaignActionAcceptanceReceiptSerializer.Serialize(repeated.Evidence.Receipt),
            repeated.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactRbaMoveKind, rba.DefenderElementId);
        Assert.False(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, repeatedSteps).IsVerified);
        var stoppedMoveSteps = moveSteps.Append(new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(stopAfterMoveSubmission),
            stopAfterMove.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(stopAfterMove.Evidence.Receipt),
            stopAfterMove.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactRbaDeclineKind, rba.DefenderElementId)).ToArray();
        Assert.True(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, stoppedMoveSteps).IsVerified);
        var declineSteps = barrageSteps.Append(new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(declineSubmission),
            acceptedDecline.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(acceptedDecline.Evidence.Receipt),
            acceptedDecline.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactRbaDeclineKind, rba.DefenderElementId)).ToArray();
        Assert.True(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, declineSteps).IsVerified);
        var forgedDecline = acceptedDecline.SuccessorSession!.CurrentCombatRetreatDecline! with
        {
            NextPositionId = "forged-force-assignment",
        };
        declineSteps[^1] = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(declineSubmission),
            [forgedDecline.SerializeCanonical()],
            CampaignActionAcceptanceReceiptSerializer.Serialize(acceptedDecline.Evidence.Receipt),
            acceptedDecline.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactRbaDeclineKind, rba.DefenderElementId);
        Assert.False(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, declineSteps).IsVerified);
        var forgedBarrage = barrage.SuccessorSession!.CurrentCombatBarrage! with
        {
            NextPositionId = "forged-barrage-route",
        };
        barrageSteps[^1] = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(barrageCommand),
            [forgedBarrage.SerializeCanonical()],
            CampaignActionAcceptanceReceiptSerializer.Serialize(barrage.Evidence.Receipt),
            barrage.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.NativeCombatBarrageKind);
        Assert.False(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, barrageSteps).IsVerified);
        var forgedSelection = imported.Session.CurrentCombatDecision.Decision with
        {
            BoundRepresentationId = "forged",
        };
        var forgedSteps = retainedSteps.ToArray();
        forgedSteps[^1] = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(submission),
            [forgedSelection.SerializeCanonical()],
            CampaignActionAcceptanceReceiptSerializer.Serialize(acceptedCombat.Evidence.Receipt),
            acceptedCombat.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactCombatSelectionKind, actor.ElementId);
        Assert.False(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, forgedSteps).IsVerified);
        Assert.False(CampaignHostAdapter.SubmitContactOwnerCombatSelection(
            successor, submission, actor.ElementId).IsAccepted);
        var finishSubmission = submission with { ActionId = cut.FinishOffer!.ActionId };
        var acceptedFinish = CampaignHostAdapter.SubmitContactOwnerCombatSelection(
            session, finishSubmission, actor.ElementId);
        Assert.True(acceptedFinish.IsAccepted);
        Assert.Equal("finish-combat-without-attack",
            acceptedFinish.SuccessorSession!.CurrentCombatDecision!.Decision.Choice);
        Assert.Equal(acceptedFinish.SuccessorSession.CurrentCombatDecision.Decision.NextPositionId,
            CampaignExercises.QueryCheckpoint(acceptedFinish.SuccessorSession).PositionId);
        Assert.True(CampaignExercises.Reconstruct(acceptedFinish.SuccessorSession).IsVerified);
        var directCompletion = CampaignNativeCombatNoAttackCompletionV13.Create(
            acceptedFinish.SuccessorSession.CurrentCombatDecision!,
            acceptedFinish.SuccessorSession.Context);
        Assert.Equal(acceptedFinish.SuccessorSession.CurrentCombatDecision!.StateVersion,
            acceptedFinish.SuccessorSession.CurrentContacts.AuthorityVersion);
        _ = acceptedFinish.SuccessorSession.CurrentContacts
            .AdvanceNativeCombatNoAttackCompletion(directCompletion);
        _ = directCompletion.SerializeStateCanonical(
            acceptedFinish.SuccessorSession.CurrentCombatDecision);
        var completed = CampaignHostAdapter.CompleteNativeCombatNoAttack(
            acceptedFinish.SuccessorSession);
        Assert.True(completed.IsAccepted, completed.RejectionReason.ToString());
        Assert.Equal(CampaignExercises.QueryCheckpoint(completed.SuccessorSession!).PositionId,
            completed.Evidence!.Receipt.ResultingPositionId);
        Assert.Equal(LandSegmentIds.ReserveRelease,
            Cna1979LandSequenceV4.CreateTurn(
                session.CurrentSnapshot.CurrentPosition.SequencePosition!.GameTurn)
                .Single(position => position.PositionId ==
                    CampaignExercises.QueryCheckpoint(completed.SuccessorSession!).PositionId)
                .SegmentId);
        Assert.True(CampaignExercises.Reconstruct(completed.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignHostAdapter.CompleteNativeCombatNoAttack(completed.SuccessorSession!).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignHostAdapter.CompleteNativeCombatNoAttack(acceptedCombat.SuccessorSession!).RejectionReason);
        var finishSteps = retainedSteps.Take(retainedSteps.Count - 1).Append(new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(finishSubmission),
            acceptedFinish.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(acceptedFinish.Evidence.Receipt),
            acceptedFinish.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactCombatSelectionKind, actor.ElementId)).ToArray();
        Assert.True(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, finishSteps).IsVerified);
        var completionCommand = CampaignHostAdapter.NativeCombatNoAttackCommand(
            acceptedFinish.SuccessorSession, completed);
        var completionStep = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(completionCommand),
            completed.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(completed.Evidence.Receipt),
            completed.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.NativeCombatNoAttackKind);
        var completedSteps = finishSteps.Append(completionStep).ToArray();
        var importedCompletion = CampaignHostAdapter.Import(request,
            started.CreationEventBytes, started.InitialSnapshotBytes, completedSteps);
        Assert.True(importedCompletion.IsVerified, importedCompletion.RejectionReason.ToString());
        Assert.True(CampaignExercises.Reconstruct(importedCompletion.Session!).IsVerified);
        var forgedRoute = completed.SuccessorSession!.CurrentNoAttackCompletion! with
        {
            Steps = completed.SuccessorSession.CurrentNoAttackCompletion!.Steps
                .Select((step, index) => index == 3
                    ? step with { NextPositionId = step.PositionId }
                    : step).ToArray(),
        };
        var forgedCompletionStep = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(completionCommand),
            [forgedRoute.SerializeCanonical()],
            CampaignActionAcceptanceReceiptSerializer.Serialize(completed.Evidence.Receipt),
            completed.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.NativeCombatNoAttackKind);
        Assert.False(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes,
            finishSteps.Append(forgedCompletionStep).ToArray()).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignHostAdapter.SubmitContactOwnerCombatSelection(session,
                submission with { ActionId = $"sha256:{new string('0', 64)}" },
                actor.ElementId).RejectionReason);
    }

    [Fact]
    public void AssaultOffersUseOnlyCurrentAdjacentContactHandlesAndApparentHexes()
    {
        CampaignObservationEdge[] edges =
        [
            new("hex-a", "hex-b", []),
            new("hex-b", "hex-c", []),
        ];
        CampaignContactTargetCandidate[] current =
        [
            new("axis-formation", "contact-adjacent", "hex-b"),
            new("axis-formation", "contact-remote", "hex-c"),
            new("other-formation", "contact-other", "hex-b"),
        ];

        var offers = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, current);

        var offer = Assert.Single(offers);
        Assert.Equal("axis-element", offer.ActingElementId);
        Assert.Equal("contact-adjacent", offer.ContactHandle);
        Assert.Equal("hex-b", offer.ApparentLocationId);
        Assert.Empty(CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, []));
    }

    [Fact]
    public void SameVisibleContactsProduceSameStableActionReferences()
    {
        CampaignObservationEdge[] edges = [new("hex-a", "hex-b", [])];
        CampaignContactTargetCandidate[] first =
        [
            new("axis-formation", "contact-two", "hex-b"),
            new("axis-formation", "contact-one", "hex-b"),
        ];
        CampaignContactTargetCandidate[] reversed = [first[1], first[0]];

        var left = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, first);
        var right = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, reversed);

        Assert.Equal(left.Select(value => value.ActionId), right.Select(value => value.ActionId));
        Assert.Equal(2, left.Select(value => value.ActionId).Distinct().Count());
    }

    [Fact]
    public void RealCombatCutSelectsCurrentContactAndRejectsForgedPrivateBinding()
    {
        var prior = BreakdownMovementTests.ChangeElement(
            BreakdownSnapshotTests.Create(true), "commonwealth-infantry", "north");
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var atCombat = CombatCutFrom(prior, artifact, scenario);
        Assert.Equal(LandStepIds.PositionDetermination,
            atCombat.CurrentPosition.SequenceContext.StepId);
        Assert.Equal(LandPhaseIds.MovementAndCombat,
            atCombat.CurrentPosition.SequenceContext.PhaseId);
        Assert.Equal(LandSegmentIds.Combat,
            atCombat.CurrentPosition.SequenceContext.SegmentId);
        Assert.Equal(LandActorRole.FirstActingSide,
            atCombat.CurrentPosition.SequenceContext.ActorRole);
        Assert.Equal(LandSide.Axis, atCombat.InitiativeHolder);
        var contacts = CampaignContactJournal.Initialize(atCombat, context);
        var cut = CampaignContactCombatSelectionCut.Project(atCombat, context,
            contacts, LandSide.Axis, "axis-infantry");
        Assert.True(cut.MayFinishWithoutAttack);
        var offer = Assert.Single(cut.AssaultOffers);
        var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(
            new ExerciseSession(atCombat, context, [], contacts),
            CampaignActionAudience.Axis, "axis-infantry");
        var visible = Encoding.UTF8.GetString(draft.VisibleBytes);
        Assert.Contains(offer.ContactHandle, visible, StringComparison.Ordinal);
        Assert.Contains("finish-combat-without-attack", visible, StringComparison.Ordinal);
        Assert.DoesNotContain(offer.ActionId, visible, StringComparison.Ordinal);
        Assert.DoesNotContain("boundRepresentationId", visible, StringComparison.Ordinal);
        Assert.Equal("{}", JsonSerializer.Serialize(draft));

        var selected = CampaignNativeCombatDecisionStateV12.Select(atCombat,
            context, contacts, LandSide.Axis, "axis-infantry", offer.ActionId);
        var canonicalEvent = selected.Decision.SerializeCanonical();
        var replayed = CampaignNativeCombatDecisionStateV12.Replay(atCombat,
            context, contacts, canonicalEvent);
        var readback = CampaignNativeCombatDecisionStateV12.DeserializeCanonical(
            selected.SerializeCanonical(), context, contacts);

        Assert.Equal(atCombat.StateVersion + 1, selected.StateVersion);
        Assert.Equal(LandStepIds.Barrage,
            Cna1979LandSequenceV4.GetNext(atCombat.CurrentPosition.SequencePosition!).StepId);
        Assert.Equal(Cna1979LandSequenceV4.GetNext(
            atCombat.CurrentPosition.SequencePosition!).PositionId,
            selected.Decision.NextPositionId);
        Assert.Equal(selected.SerializeCanonical(), replayed.SerializeCanonical());
        Assert.Equal(selected.SerializeCanonical(), readback.SerializeCanonical());
        Assert.Equal(offer.ContactHandle, selected.Decision.ContactHandle);
        Assert.NotEqual("forged", selected.Decision.BoundRepresentationId);
        var selectedContacts = contacts.AdvanceNativeCombatSelection(selected);
        var barrage = CampaignNativeCombatBarrageV14.Create(selected, context);
        var rbaContacts = selectedContacts.AdvanceNativeCombatBarrage(barrage);
        var rba = CampaignContactRetreatBeforeAssaultCut.Project(selected,
            barrage, context, rbaContacts);
        var decline = CampaignNativeCombatRetreatDeclineV15.Create(selected,
            barrage, context, rbaContacts, rba.DeclineOffer.ActionId);
        var forceContacts = rbaContacts.AdvanceNativeCombatRetreatDecline(decline);
        var attackerForce = CampaignContactForceAssignmentCut.Project(selected,
            decline, atCombat.World, context, forceContacts, LandSide.Axis);
        var defenderForce = CampaignContactForceAssignmentCut.Project(selected,
            decline, atCombat.World, context, forceContacts, LandSide.Commonwealth);
        Assert.Equal("attacker", attackerForce.Role);
        Assert.Equal("defender", defenderForce.Role);
        Assert.Equal("axis-infantry", attackerForce.SealOffer.ElementId);
        Assert.Equal("commonwealth-infantry", defenderForce.SealOffer.ElementId);
        Assert.Equal(5, attackerForce.SealOffer.CommittedToe);
        Assert.Equal(5, defenderForce.SealOffer.CommittedToe);
        Assert.NotEqual(attackerForce.SealOffer.ActionId, defenderForce.SealOffer.ActionId);
        var attackerSeal = CampaignNativeCombatForceSealV17.Create(selected,
            decline, atCombat.World, context, forceContacts, LandSide.Axis,
            attackerForce.SealOffer.ActionId);
        var afterAttackerSeal = forceContacts.AdvanceNativeCombatForceSeal(attackerSeal);
        var defenderAfterSeal = CampaignContactForceAssignmentCut.Project(selected,
            decline, atCombat.World, context, afterAttackerSeal,
            LandSide.Commonwealth);
        Assert.Equal(defenderForce.SealOffer.ActionId,
            defenderAfterSeal.SealOffer.ActionId);
        Assert.Equal(decline.NextPositionId, attackerSeal.NextPositionId);
        Assert.Equal(attackerSeal, CampaignNativeCombatForceSealV17.Replay(selected,
            decline, atCombat.World, context, forceContacts,
            attackerSeal.SerializeCanonical()));
        Assert.Equal(attackerSeal, CampaignCurrentEventSerializer.Deserialize(
            attackerSeal.SerializeCanonical()));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignNativeCombatForceSealV17.Create(selected, decline,
                atCombat.World, context, afterAttackerSeal, LandSide.Axis,
                attackerForce.SealOffer.ActionId, attackerSeal));
        var defenderSeal = CampaignNativeCombatForceSealV17.Create(selected,
            decline, atCombat.World, context, afterAttackerSeal,
            LandSide.Commonwealth, defenderForce.SealOffer.ActionId, attackerSeal);
        Assert.Equal(LandStepIds.ForceAssignment,
            Cna1979LandSequenceV4.CreateTurn(
                atCombat.CurrentPosition.SequencePosition!.GameTurn).Single(value =>
                value.PositionId == defenderSeal.NextPositionId).StepId);
        Assert.Equal(defenderSeal, CampaignNativeCombatForceSealV17.Replay(selected,
            decline, atCombat.World, context, afterAttackerSeal,
            defenderSeal.SerializeCanonical(), attackerSeal));
        var forceCompletion = CampaignNativeCombatForceCompletionV18.Create(selected,
            decline, [attackerSeal, defenderSeal]);
        Assert.Equal(LandStepIds.AntiArmor,
            Cna1979LandSequenceV4.CreateTurn(
                atCombat.CurrentPosition.SequencePosition!.GameTurn).Single(value =>
                value.PositionId == forceCompletion.NextPositionId).StepId);
        Assert.Equal(forceCompletion, CampaignNativeCombatForceCompletionV18.Replay(
            selected, decline, [attackerSeal, defenderSeal],
            forceCompletion.SerializeCanonical()));
        var forceSession = new ExerciseSession(atCombat, context,
            [selected.Decision, barrage, decline], forceContacts, selected,
            combatBarrage: barrage, combatRetreatDecline: decline);
        var defenderDraftBefore = CampaignHostAdapter.QueryContactForceOwnerDraft(
            forceSession, CampaignActionAudience.Commonwealth, "commonwealth-infantry");
        var attackerSubmission = new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion, selected.CampaignId,
            decline.StateVersion, decline.NextPositionId, CampaignActionAudience.Axis,
            attackerForce.SealOffer.ActionId);
        var acceptedAttackerSeal = CampaignHostAdapter.SubmitContactOwnerForceSeal(
            forceSession, attackerSubmission, "axis-infantry");
        Assert.True(acceptedAttackerSeal.IsAccepted,
            acceptedAttackerSeal.RejectionReason.ToString());
        var timing = new CampaignDecisionTimingConfigurationV1(
            "rules-lab.native-combat-timing.1", atCombat.RulesetHash,
            context.ArtifactV6!.Identity.Hash,
            [new("custody", 30_000, "leave-unguarded"),
                new("cycle-control", 30_000, "finish-phase"),
                new("force-assignment", 30_000, "cancel-incomplete-voluntary-round"),
                new("rba", 30_000, "cancel-without-decline"),
                new("reserve-release", 30_000, "convert-unresolved-i-retain-ii"),
                new("retreat", 30_000, "refuse-retreat"),
                new("selection", 30_000, "no-selection")]);
        var cancelled = CampaignExercises.CancelNativeCombatForceAssignment(
            acceptedAttackerSeal.SuccessorSession!, timing, 1_000, 31_000, 2_000, 31_000,
            "deadline");
        Assert.True(cancelled.IsAccepted, cancelled.RejectionReason.ToString());
        Assert.Single(cancelled.SuccessorSession!.CurrentCombatForceSeals);
        Assert.Equal(LandStepIds.AntiArmor,
            Cna1979LandSequenceV4.CreateTurn(
                atCombat.CurrentPosition.SequencePosition!.GameTurn).Single(value =>
                value.PositionId == CampaignExercises.QueryCheckpoint(cancelled.SuccessorSession).PositionId)
                .StepId);
        var cancelledEvent = cancelled.SuccessorSession.CurrentCombatForceCancellation!;
        Assert.Equal(cancelledEvent, CampaignNativeCombatForceCancellationV19.Replay(
            selected, decline, [attackerSeal], cancelledEvent.SerializeCanonical()));
        Assert.Equal(cancelledEvent, CampaignCurrentEventSerializer.Deserialize(
            cancelledEvent.SerializeCanonical()));
        Assert.False(CampaignExercises.CancelNativeCombatForceAssignment(
            acceptedAttackerSeal.SuccessorSession!, timing, 1_000, 31_000, 2_000, 30_999,
            "deadline").IsAccepted);
        Assert.False(CampaignExercises.CancelNativeCombatForceAssignment(
            acceptedAttackerSeal.SuccessorSession!, timing, 1_000, 31_000, 2_000, null,
            "deadline").IsAccepted);
        Assert.True(CampaignExercises.CancelNativeCombatForceAssignment(
            acceptedAttackerSeal.SuccessorSession!, timing, 1_000, 31_000, 2_000, 1_500,
            "clock-regression").IsAccepted);
        var noSealCancellation = CampaignExercises.CancelNativeCombatForceAssignment(
            forceSession, timing, 1_000, 31_000, 1_000, 31_000, "deadline");
        Assert.True(noSealCancellation.IsAccepted,
            noSealCancellation.RejectionReason.ToString());
        Assert.Empty(noSealCancellation.SuccessorSession!.CurrentCombatForceSeals);
        Assert.Null(noSealCancellation.SuccessorSession.CurrentCombatForceCancellation!
            .RetainedSealHash);
        Assert.False(CampaignHostAdapter.SubmitContactOwnerForceSeal(
            cancelled.SuccessorSession, attackerSubmission, "axis-infantry").IsAccepted);
        Assert.Throws<InvalidOperationException>(() =>
            CampaignNativeCombatForceCancellationV19.Create(selected, decline,
                [attackerSeal, defenderSeal], timing.Hash, 1_000, 31_000, 2_000,
                31_000,
                "deadline"));
        var defenderDraftAfter = CampaignHostAdapter.QueryContactForceOwnerDraft(
            acceptedAttackerSeal.SuccessorSession!, CampaignActionAudience.Commonwealth,
            "commonwealth-infantry");
        Assert.Equal(defenderDraftBefore.VisibleBytes, defenderDraftAfter.VisibleBytes);
        Assert.Equal(defenderDraftBefore.PrivateActionIds, defenderDraftAfter.PrivateActionIds);
        Assert.Equal("{}", JsonSerializer.Serialize(defenderDraftAfter));
        var defenderSubmission = attackerSubmission with
        {
            ExpectedStateVersion = attackerSeal.StateVersion,
            Audience = CampaignActionAudience.Commonwealth,
            ActionId = defenderForce.SealOffer.ActionId,
        };
        var acceptedDefenderSeal = CampaignHostAdapter.SubmitContactOwnerForceSeal(
            acceptedAttackerSeal.SuccessorSession!, defenderSubmission,
            "commonwealth-infantry");
        Assert.True(acceptedDefenderSeal.IsAccepted,
            acceptedDefenderSeal.RejectionReason.ToString());
        Assert.Equal(defenderSeal.StateVersion,
            CampaignExercises.QueryCheckpoint(acceptedDefenderSeal.SuccessorSession!).StateVersion);
        Assert.Equal(defenderSeal.NextPositionId,
            CampaignExercises.QueryCheckpoint(acceptedDefenderSeal.SuccessorSession!).PositionId);
        var acceptedForceCompletion = CampaignHostAdapter.CompleteNativeCombatForceAssignment(
            acceptedDefenderSeal.SuccessorSession!);
        Assert.True(acceptedForceCompletion.IsAccepted,
            acceptedForceCompletion.RejectionReason.ToString());
        Assert.Equal(forceCompletion.NextPositionId,
            CampaignExercises.QueryCheckpoint(acceptedForceCompletion.SuccessorSession!).PositionId);
        var forceCommand = CampaignHostAdapter.NativeCombatForceCompletionCommand(
            acceptedDefenderSeal.SuccessorSession!, acceptedForceCompletion);
        Assert.Equal(CampaignActionAudience.System, forceCommand.Audience);
        Assert.Equal(defenderSeal.StateVersion, forceCommand.ExpectedStateVersion);
        Assert.Equal(forceCompletion,
            CampaignCurrentEventSerializer.Deserialize(forceCompletion.SerializeCanonical()));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCombatForceCompletionV18.Replay(selected, decline,
                [attackerSeal, defenderSeal],
                (forceCompletion with { NextPositionId = decline.NextPositionId })
                .SerializeCanonical()));
        Assert.False(CampaignHostAdapter.SubmitContactOwnerForceSeal(
            acceptedAttackerSeal.SuccessorSession!, defenderSubmission with
            { ActionId = attackerForce.SealOffer.ActionId }, "commonwealth-infantry").IsAccepted);
        var forged = selected.Decision with { BoundRepresentationId = "forged" };
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCombatDecisionStateV12.Replay(atCombat, context,
                contacts, forged.SerializeCanonical()));
        var forgedRoute = selected.Decision with { NextPositionId = selected.Decision.PositionId };
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCombatDecisionStateV12.Replay(atCombat, context,
                contacts, forgedRoute.SerializeCanonical()));

        var finish = CampaignNativeCombatDecisionStateV12.Select(atCombat,
            context, contacts, LandSide.Axis, "axis-infantry", cut.FinishOffer!.ActionId);
        Assert.Equal("finish-combat-without-attack", finish.Decision.Choice);
        Assert.Null(finish.Decision.ContactHandle);
        Assert.Null(finish.Decision.BoundRepresentationId);
        Assert.Equal(finish.SerializeCanonical(),
            CampaignNativeCombatDecisionStateV12.Replay(atCombat, context,
                contacts, finish.Decision.SerializeCanonical()).SerializeCanonical());
        Assert.Throws<JsonException>(() => (finish.Decision with
        {
            BoundRepresentationId = selected.Decision.BoundRepresentationId,
        }).SerializeCanonical());

        var completion = CampaignNativeCombatNoAttackCompletionV13.Create(finish, context);
        var completionBytes = completion.SerializeCanonical();
        Assert.Equal(5, completion.Steps.Count);
        Assert.Equal(finish.Decision.NextPositionId, completion.Steps[0].PositionId);
        Assert.Equal(LandSegmentIds.ReserveRelease,
            Cna1979LandSequenceV4.CreateTurn(atCombat.CurrentPosition.SequencePosition!.GameTurn)
                .Single(position => position.PositionId == completion.Steps[^1].NextPositionId).SegmentId);
        Assert.Equal(completionBytes,
            CampaignNativeCombatNoAttackCompletionV13.Replay(finish, context,
                completionBytes).SerializeCanonical());
        Assert.Throws<InvalidOperationException>(() =>
            CampaignNativeCombatNoAttackCompletionV13.Create(selected, context));
        var forgedCompletion = completion with
        {
            Steps = completion.Steps.Select((step, index) => index == 2
                ? step with { NextPositionId = completion.Steps[0].NextPositionId }
                : step).ToArray(),
        };
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCombatNoAttackCompletionV13.Replay(finish, context,
                forgedCompletion.SerializeCanonical()));

    }

    [Fact]
    public void HiddenEnemyPositionsKeepFinishOfferAndVisibleBytesEqual()
    {
        var source = BreakdownSnapshotTests.Create(true);
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var left = CombatCutFrom(BreakdownMovementTests.ChangeElement(source,
            "commonwealth-infantry", "east"), artifact, scenario);
        var right = CombatCutFrom(BreakdownMovementTests.ChangeElement(source,
            "commonwealth-infantry", "south-east"), artifact, scenario);
        var first = CampaignContactCombatSelectionCut.Project(left, context,
            CampaignContactJournal.Initialize(left, context), LandSide.Axis, "axis-infantry");
        var second = CampaignContactCombatSelectionCut.Project(right, context,
            CampaignContactJournal.Initialize(right, context), LandSide.Axis, "axis-infantry");

        Assert.NotNull(first.FinishOffer);
        Assert.Equal(first.FinishOffer.ActionId, second.FinishOffer!.ActionId);
        Assert.Equal(CampaignContactCombatOwnerVisibleCutV2.SerializeCanonical(first),
            CampaignContactCombatOwnerVisibleCutV2.SerializeCanonical(second));
    }

    private static CampaignSnapshotV11 CombatCutFrom(CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        var movement = CampaignBreakdownLifecycleFactory.CreateMovementCompletion(
            prior, artifact, scenario);
        var atBreakdown = new CampaignSnapshotV11(11, prior.CampaignId,
            movement.StateVersion, prior.RulesetHash, prior.Setup, prior.World,
            prior.InitiativeHolder, prior.OperationStageOrders,
            prior.OperationStageWeather, prior.RandomState,
            CampaignPositionV11.FromSequence(movement.SequencePosition),
            null, new CampaignBreakdownFlow.Idle());
        var completion = CampaignBreakdownLifecycleFactory.CreateBreakdownCompletion(
            atBreakdown, artifact, scenario,
            CampaignBreakdownLifecycleFactory.CreateBreakdownCompletionActionId());
        return new CampaignSnapshotV11(11, prior.CampaignId,
            completion.StateVersion, prior.RulesetHash, prior.Setup, prior.World,
            prior.InitiativeHolder, prior.OperationStageOrders,
            prior.OperationStageWeather, prior.RandomState,
            CampaignPositionV11.FromSequence(completion.SequencePosition),
            null, new CampaignBreakdownFlow.Idle());
    }
}
