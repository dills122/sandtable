using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Actions;

namespace Cna.Core.Tests.Exercises;

// Task 022A (#448): trusted in-process Exercise path through first-cycle Combat Selection and the
// System step after it. Test-harness capability only; not Combat activation or certification.
public sealed class CampaignCombatExerciseActionsTests
{
    private const string Attacker = "axis-infantry";
    private const string Prefix = "land.position.operation-1.first-player.movement-and-combat.";

    [Fact]
    public void OutsideCombatQueryWithCombatEqualsTheOrdinaryQuery()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions
            .Single(value => value.SetupId == "rules-lab.breakdown.reaction.combat-assault.v1"));
        var started = CampaignExercises.Begin(new CampaignCreationRequest(1, "campaign-022a-unchanged",
            Cna1979Ruleset.Manifest.Hash, 6, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId));
        var session = started.Session!;
        var compared = 0;
        while (CampaignExercises.QueryCheckpoint(session).PositionId != Prefix + "combat.position-determination")
        {
            foreach (var audience in Enum.GetValues<CampaignActionAudience>())
            {
                var ordinary = CampaignExercises.Query(session, audience);
                var combined = CampaignExercises.QueryWithCombat(session, audience);
                Assert.Equal(ordinary.IsSuccessful, combined.IsSuccessful);
                Assert.Equal(ordinary.RejectionReason, combined.RejectionReason);
                Assert.Equal(ordinary.ActionSet, combined.ActionSet);
                compared++;
            }
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var action = set.Candidates.OfType<CompleteMovementSegmentAction>().FirstOrDefault() ?? set.Candidates[0];
            var submission = Command(set, action.ActionId);
            var ordinaryStep = CampaignExercises.Submit(session, submission);
            var combinedStep = CampaignExercises.SubmitWithCombat(session, submission);
            Assert.True(combinedStep.IsAccepted);
            Assert.Equal(ordinaryStep.Evidence!.EventRecords, combinedStep.Evidence!.EventRecords);
            Assert.Equal(ordinaryStep.Evidence.SnapshotCheckpoint, combinedStep.Evidence.SnapshotCheckpoint);
            Assert.Equal(ordinaryStep.Evidence.Receipt, combinedStep.Evidence.Receipt);
            session = combinedStep.SuccessorSession!;
        }
        Assert.True(compared > 30);
    }

    [Fact]
    public void CombatEntryOffersTheUnionOfEveryActingElementsSelection()
    {
        var session = CombatCycleDisclosureForceTests.AdvanceToCombat("campaign-022a-entry", 6);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        Assert.Equal(Prefix + "combat.position-determination", checkpoint.PositionId);

        var axis = CampaignExercises.QueryWithCombat(session, CampaignActionAudience.Axis).ActionSet!;
        Assert.Equal(CampaignLegalActionSet.CombatExercisePolicyId, axis.PolicyId);
        Assert.Equal(checkpoint.StateVersion, axis.StateVersion);
        Assert.Equal(checkpoint.PositionId, axis.PositionId);
        var expected = session.CurrentSnapshot.World.Elements.Select(value => value.ElementId)
            .Select(element => CampaignCombatAudienceDecisions.QuerySelection(session, CampaignActionAudience.Axis, element))
            .OfType<CampaignCombatAudienceDecision>()
            .ToArray();
        Assert.True(expected.Length > 1);
        Assert.Equal(expected.Sum(value => value.OfferRefs.Count), axis.Candidates.Count);
        Assert.All(axis.Candidates, candidate => Assert.True(candidate.Kind
            is "select-contact-assault" or "finish-combat-without-attack", candidate.Kind));
        Assert.Equal(expected.Select(value => value.ActingElementId).Order(StringComparer.Ordinal),
            axis.Candidates.Select(ActingElement).Distinct().Order(StringComparer.Ordinal));

        foreach (var audience in new[] { CampaignActionAudience.System, CampaignActionAudience.Commonwealth })
        {
            var other = CampaignExercises.QueryWithCombat(session, audience);
            Assert.True(other.IsSuccessful);
            Assert.Empty(other.ActionSet!.Candidates);
            Assert.Equal(CampaignLegalActionSet.CombatExercisePolicyId, other.ActionSet.PolicyId);
        }
        // The ordinary query and the frozen audience surface are unchanged.
        Assert.All(Enum.GetValues<CampaignActionAudience>(), audience =>
            Assert.Empty(CampaignExercises.Query(session, audience).ActionSet!.Candidates));
    }

    [Theory]
    [InlineData(0, "complete-combat-barrage", "combat.retreat-before-assault")]
    [InlineData(1, "complete-combat-no-attack", "reserve-release")]
    public void AcceptedSelectionAndSystemStepAreTheUnchangedTrustedSteps(
        int fromEnd, string systemKind, string frontier)
    {
        var session = CombatCycleDisclosureForceTests.AdvanceToCombat("campaign-022a-step", 6);
        var decision = CampaignCombatAudienceDecisions.QuerySelection(session, CampaignActionAudience.Axis, Attacker)!;
        var ordinal = fromEnd == 0 ? 0 : decision.OfferRefs.Count - 1;
        var trusted = CampaignCombatAudienceDecisions.Submit(session, decision.Choose(ordinal)).TrustedStep!;
        var axis = CampaignExercises.QueryWithCombat(session, CampaignActionAudience.Axis).ActionSet!;
        var candidate = axis.Candidates.Single(value => value.ActionId == trusted.Evidence!.Receipt.ActionId);

        var selected = CampaignExercises.SubmitWithCombat(session, Command(axis, candidate.ActionId));
        Assert.True(selected.IsAccepted, selected.RejectionReason.ToString());
        Assert.Equal(trusted.Evidence!.EventRecords, selected.Evidence!.EventRecords);
        Assert.Equal(trusted.Evidence.SnapshotCheckpoint, selected.Evidence.SnapshotCheckpoint);
        var receipt = selected.Evidence.Receipt;
        var original = trusted.Evidence.Receipt;
        Assert.Equal((original.CampaignId, original.PriorStateVersion, original.CommittedStateVersion, original.Audience, original.ActionId),
            (receipt.CampaignId, receipt.PriorStateVersion, receipt.CommittedStateVersion, receipt.Audience, receipt.ActionId));
        // T022-Q7: only the resulting position differs; it names the successor checkpoint.
        Assert.Equal(Prefix + "combat.position-determination", original.ResultingPositionId);
        Assert.Equal(CampaignExercises.QueryCheckpoint(selected.SuccessorSession!).PositionId, receipt.ResultingPositionId);
        Assert.True(CampaignExercises.Reconstruct(selected.SuccessorSession!).IsVerified);

        var afterSelection = selected.SuccessorSession!;
        Assert.Empty(CampaignExercises.QueryWithCombat(afterSelection, CampaignActionAudience.Axis).ActionSet!.Candidates);
        Assert.Empty(CampaignExercises.QueryWithCombat(afterSelection, CampaignActionAudience.Commonwealth).ActionSet!.Candidates);
        var system = CampaignExercises.QueryWithCombat(afterSelection, CampaignActionAudience.System).ActionSet!;
        var step = Assert.Single(system.Candidates);
        Assert.Equal(systemKind, step.Kind);
        var direct = fromEnd == 0
            ? CampaignHostAdapter.CompleteNativeCombatBarrage(afterSelection)
            : CampaignHostAdapter.CompleteNativeCombatNoAttack(afterSelection);
        Assert.Equal(direct.Evidence!.Receipt.ActionId, step.ActionId);

        var completed = CampaignExercises.SubmitWithCombat(afterSelection, Command(system, step.ActionId));
        Assert.True(completed.IsAccepted, completed.RejectionReason.ToString());
        Assert.Equal(direct.Evidence.EventRecords, completed.Evidence!.EventRecords);
        Assert.Equal(direct.Evidence.SnapshotCheckpoint, completed.Evidence.SnapshotCheckpoint);
        Assert.Equal(direct.Evidence.Receipt, completed.Evidence.Receipt);
        var atFrontier = completed.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(atFrontier).IsVerified);
        Assert.Equal(Prefix + frontier, CampaignExercises.QueryCheckpoint(atFrontier).PositionId);
        Assert.All(Enum.GetValues<CampaignActionAudience>(), audience =>
        {
            var set = CampaignExercises.QueryWithCombat(atFrontier, audience).ActionSet!;
            Assert.Empty(set.Candidates);
            Assert.Equal(Prefix + frontier, set.PositionId);
            Assert.Equal(CampaignLegalActionQueryRejectionReason.InvalidState,
                CampaignExercises.Query(atFrontier, audience).RejectionReason);
        });
    }

    [Fact]
    public void InvalidCombatSubmissionsAreRejectedWithoutAStep()
    {
        var session = CombatCycleDisclosureForceTests.AdvanceToCombat("campaign-022a-reject", 6);
        var axis = CampaignExercises.QueryWithCombat(session, CampaignActionAudience.Axis).ActionSet!;
        var valid = Command(axis, axis.Candidates[0].ActionId);
        var forged = "sha256:" + new string('0', 64);
        (CampaignActionSubmission Submission, CampaignActionSubmissionRejectionReason Reason)[] invalid =
        [
            (valid with { ExpectedStateVersion = valid.ExpectedStateVersion - 1 }, CampaignActionSubmissionRejectionReason.StaleState),
            (valid with { ExpectedPositionId = Prefix + "combat.barrage" }, CampaignActionSubmissionRejectionReason.UnexpectedPosition),
            (valid with { CampaignId = "campaign-other" }, CampaignActionSubmissionRejectionReason.CampaignMismatch),
            (valid with { Audience = CampaignActionAudience.Commonwealth }, CampaignActionSubmissionRejectionReason.ActionNotLegal),
            (valid with { Audience = CampaignActionAudience.System }, CampaignActionSubmissionRejectionReason.ActionNotLegal),
            (valid with { ActionId = forged }, CampaignActionSubmissionRejectionReason.ActionNotLegal),
        ];
        foreach (var (submission, reason) in invalid)
        {
            var rejected = CampaignExercises.SubmitWithCombat(session, submission);
            Assert.False(rejected.IsAccepted);
            Assert.Equal(reason, rejected.RejectionReason);
            Assert.Null(rejected.Evidence);
        }

        var accepted = CampaignExercises.SubmitWithCombat(session, valid);
        Assert.True(accepted.IsAccepted);
        var next = accepted.SuccessorSession!;
        var nextCheckpoint = CampaignExercises.QueryCheckpoint(next);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitWithCombat(next,
            valid with { ExpectedStateVersion = nextCheckpoint.StateVersion, ExpectedPositionId = nextCheckpoint.PositionId })
            .RejectionReason);
        var system = CampaignExercises.QueryWithCombat(next, CampaignActionAudience.System).ActionSet!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitWithCombat(next,
            Command(system, system.Candidates[0].ActionId) with { Audience = CampaignActionAudience.Axis }).RejectionReason);
    }

    [Fact]
    public void CombatExerciseSystemStepsAreConfinedToTheirPolicy()
    {
        var session = CombatCycleDisclosureForceTests.AdvanceToCombat("campaign-022a-policy", 6);
        var axis = CampaignExercises.QueryWithCombat(session, CampaignActionAudience.Axis).ActionSet!;
        var after = CampaignExercises.SubmitWithCombat(session, Command(axis, axis.Candidates[0].ActionId)).SuccessorSession!;
        var step = CampaignExercises.QueryWithCombat(after, CampaignActionAudience.System).ActionSet!.Candidates.Single();
        var checkpoint = CampaignExercises.QueryCheckpoint(after);
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(checkpoint.CampaignId, checkpoint.StateVersion,
            checkpoint.RulesetHash, checkpoint.PositionId, CampaignActionAudience.System, [step]));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(checkpoint.CampaignId, checkpoint.StateVersion,
            checkpoint.RulesetHash, checkpoint.PositionId, CampaignActionAudience.Axis, [step],
            CampaignLegalActionSet.CombatExercisePolicyId));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(checkpoint.CampaignId, checkpoint.StateVersion,
            checkpoint.RulesetHash, checkpoint.PositionId, CampaignActionAudience.System, [axis.Candidates[0]],
            CampaignLegalActionSet.CombatExercisePolicyId));
    }

    private static string ActingElement(CampaignActionCandidate candidate) => candidate switch
    {
        SelectContactAssaultAction assault => assault.ActingElementId,
        FinishCombatWithoutAttackAction finish => finish.ActingElementId,
        _ => throw new InvalidOperationException(candidate.Kind),
    };

    private static CampaignActionSubmission Command(CampaignLegalActionSet set, string actionId) => new(
        CampaignActionSubmission.CurrentContractVersion, set.CampaignId, set.StateVersion,
        set.PositionId, set.Audience, actionId);
}
