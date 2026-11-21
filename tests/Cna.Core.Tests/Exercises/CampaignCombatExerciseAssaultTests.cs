using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Actions;

namespace Cna.Core.Tests.Exercises;

// Tasks 022B/022C (#448): the first-cycle Combat loop through the trusted Exercise path: decline-only
// RBA, Force Assignment, Anti Armor, the result or cancelled assault, settlement, Custody, Reserve
// Release and Cycle Control. Every accepted step must equal the trusted Core step the Host appends at
// the same cut. Test-harness capability only; not Combat activation or certification.
public sealed class CampaignCombatExerciseAssaultTests
{
    private const string Prefix = "land.position.operation-1.first-player.";
    private const string SecondPrefix = "land.position.operation-1.second-player.";

    private static readonly string[] Payable =
    [
        "select-contact-assault", "complete-combat-barrage", "decline-retreat-before-assault",
        "seal-combat-force-assignment", "seal-combat-force-assignment", "complete-combat-force-assignment",
        "complete-combat-anti-armor", "resolve-combat-assault",
    ];

    private static readonly string[] Unpayable =
    [
        "select-contact-assault", "complete-combat-barrage", "decline-retreat-before-assault",
        "cancel-combat-force-unavailable", "complete-combat-anti-armor", "complete-combat-cancelled-assault",
    ];

    private static readonly string[] Settled = ["complete-combat-no-retreat", "settle-combat-world"];

    private static readonly string[] SecondPlayerPrefix =
        ["combat-truck-convoy-pass-pass", "combat-post-combat-pass-finish-without-activity"];

    private static readonly string[] ReissuedReceiptKinds =
    [
        "select-contact-assault", "finish-combat-without-attack", "decline-retreat-before-assault",
        "seal-combat-force-assignment", "complete-combat-successor",
        "combat-second-movement-completion-complete-movement",
    ];

    [Theory]
    [InlineData(6)]
    [InlineData(12345)]
    public void EveryEntryAssaultFollowsTheHostOrchestrationToItsFrontier(ulong seed)
    {
        var entry = CombatCycleDisclosureForceTests.AdvanceToCombat($"campaign-022b-entry-{seed}", seed);
        var assaults = CampaignExercises.QueryWithCombat(entry, CampaignActionAudience.Axis).ActionSet!
            .Candidates.OfType<SelectContactAssaultAction>().ToArray();
        Assert.NotEmpty(assaults);
        var results = 0;
        foreach (var assault in assaults)
        {
            var (kinds, frontier) = Drive(entry, assault.ActionId);
            var payable = kinds.Take(Payable.Length).SequenceEqual(Payable);
            Assert.True(payable || kinds.Take(Unpayable.Length).SequenceEqual(Unpayable), string.Join(" > ", kinds));
            results += payable ? 1 : 0;
            AssertFrontier(frontier, kinds);
        }
        // The fixture's infantry pair is payable (020A1's Force fixture).
        Assert.True(results > 0);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(20)]
    public void FullFirstCycleLoopMatchesTheHostOrchestration(ulong seed)
    {
        // combat-double-fresh with a designated second infantry: a fresh, payable pair, no required
        // retreat, a capture with the captor's owner Custody choice (seeds 5 and 20, as in 020A3), and
        // Reserve Release member choices.
        var entry = AtCombatPosition($"campaign-022c-loop-{seed}", seed);
        var assault = CampaignExercises.QueryWithCombat(entry, CampaignActionAudience.Axis).ActionSet!
            .Candidates.OfType<SelectContactAssaultAction>().Single(value => value.ActingElementId == "axis-infantry");
        var (kinds, frontier) = Drive(entry, assault.ActionId);
        Assert.Equal(Payable, kinds.Take(Payable.Length));
        AssertFrontier(frontier, kinds);
        Assert.Equal(Settled, kinds.Skip(Payable.Length).Take(Settled.Length));
        Assert.Contains(kinds, kind => kind.StartsWith("combat-custody-", StringComparison.Ordinal));
        Assert.Contains("complete-combat-successor", kinds);
        Assert.Contains("complete-combat-reserve-release", kinds);
        Assert.Contains(kinds, kind => kind.StartsWith("combat-reserve-release-", StringComparison.Ordinal));
        // 022D: after Cycle Control Finish the second player's phase follows.
        var finish = kinds.IndexOf("combat-cycle-control-finish");
        Assert.True(finish > 0);
        Assert.Equal(SecondPlayerPrefix, kinds.Skip(finish + 1).Take(SecondPlayerPrefix.Length));
        Assert.Contains("complete-second-side-breakdown", kinds);
    }

    [Fact]
    public void CycleControlOffersRepeatAndFinishAndRepeatStopsAtTheRepeatedMovementFrontier()
    {
        var entry = AtCombatPosition("campaign-022c-repeat", 5);
        var assault = CampaignExercises.QueryWithCombat(entry, CampaignActionAudience.Axis).ActionSet!
            .Candidates.OfType<SelectContactAssaultAction>().Single(value => value.ActingElementId == "axis-infantry");
        // Releasing the reserve gives Cycle Control a Repeat choice (the 020A2/020E path).
        var (kinds, session) = Drive(entry, assault.ActionId, stopAtCycleControl: true, releaseReserve: true);
        Assert.Contains("combat-reserve-release-release-i", kinds);
        var control = CampaignExercises.QueryWithCombat(session, CampaignActionAudience.Axis).ActionSet!;
        Assert.Equal(["combat-cycle-control-finish", "combat-cycle-control-repeat"],
            control.Candidates.Select(value => value.Kind).Order(StringComparer.Ordinal));
        var draft = CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(session, CampaignActionAudience.Axis);
        Assert.Equal(draft.PrivateActionIds.Order(StringComparer.Ordinal),
            control.Candidates.Select(value => value.ActionId).Order(StringComparer.Ordinal));
        var repeat = control.Candidates.Single(value => value.Kind == "combat-cycle-control-repeat");
        var repeated = CampaignExercises.SubmitWithCombat(session, Command(session, CampaignActionAudience.Axis, repeat.ActionId));
        Assert.True(repeated.IsAccepted, repeated.RejectionReason.ToString());
        // Repeated Movement is a contract-v2 stage: not covered, so the harness offers nothing.
        Assert.All(Enum.GetValues<CampaignActionAudience>(), audience =>
            Assert.Empty(CampaignExercises.QueryWithCombat(repeated.SuccessorSession!, audience).ActionSet!.Candidates));
        Assert.True(CampaignExercises.Reconstruct(repeated.SuccessorSession!).IsVerified);
    }

    [Fact]
    public void BothOwnersSealTogetherAndEitherOrderSettlesTheSameResult()
    {
        var entry = CombatCycleDisclosureForceTests.AdvanceToCombat("campaign-022b-seal-order", 6);
        var session = entry;
        var attack = CampaignExercises.QueryWithCombat(session, CampaignActionAudience.Axis).ActionSet!
            .Candidates.OfType<SelectContactAssaultAction>().Single(value => value.ActingElementId == "axis-infantry");
        session = Step(session, CampaignActionAudience.Axis, attack.ActionId);
        session = Step(session, CampaignActionAudience.System, Single(session, CampaignActionAudience.System));
        session = Step(session, CampaignActionAudience.Commonwealth, Single(session, CampaignActionAudience.Commonwealth));
        Assert.Empty(CampaignExercises.QueryWithCombat(session, CampaignActionAudience.System).ActionSet!.Candidates);
        var axisSeal = Single(session, CampaignActionAudience.Axis);
        var commonwealthSeal = Single(session, CampaignActionAudience.Commonwealth);

        byte[] Settle(CampaignActionAudience first, string firstSeal, CampaignActionAudience second, string secondSeal)
        {
            var sealedOnce = Step(session, first, firstSeal);
            // The first seal leaves the other owner's offer unchanged and removes the sealer's own.
            Assert.Empty(CampaignExercises.QueryWithCombat(sealedOnce, first).ActionSet!.Candidates);
            Assert.Equal(secondSeal, Single(sealedOnce, second));
            var next = Step(sealedOnce, second, secondSeal);
            next = Step(next, CampaignActionAudience.System, Single(next, CampaignActionAudience.System));
            next = Step(next, CampaignActionAudience.System, Single(next, CampaignActionAudience.System));
            var result = CampaignExercises.SubmitWithCombat(next, Command(next, CampaignActionAudience.System,
                Single(next, CampaignActionAudience.System)));
            Assert.True(result.IsAccepted);
            Assert.True(CampaignExercises.Reconstruct(result.SuccessorSession!).IsVerified);
            return result.Evidence!.EventRecords[0];
        }

        var axisFirst = Settle(CampaignActionAudience.Axis, axisSeal, CampaignActionAudience.Commonwealth, commonwealthSeal);
        var commonwealthFirst = Settle(CampaignActionAudience.Commonwealth, commonwealthSeal, CampaignActionAudience.Axis, axisSeal);
        // Seal order changes only the event-chain link (priorAntiArmorHash); the adjudicated result is
        // byte-identical otherwise.
        Assert.NotEqual(axisFirst, commonwealthFirst);
        Assert.Equal(WithoutChainLink(axisFirst), WithoutChainLink(commonwealthFirst));
    }

    private static void AssertFrontier(ExerciseSession frontier, IReadOnlyList<string> kinds)
    {
        Assert.True(CampaignExercises.Reconstruct(frontier).IsVerified);
        Assert.All(Enum.GetValues<CampaignActionAudience>(), audience =>
            Assert.Empty(CampaignExercises.QueryWithCombat(frontier, audience).ActionSet!.Candidates));
        var position = CampaignExercises.QueryCheckpoint(frontier).PositionId;
        if (kinds[^1] == "resolve-combat-assault")
            // A required post-assault retreat (contract-v2 stage) stops the loop at the result.
            Assert.Equal(Prefix + "movement-and-combat.combat.close-assault", position);
        else if (kinds[^1] == "complete-second-side-combat-no-attack")
            // The second side's no-attack completion opens no Reserve Release in Core (022D frontier).
            Assert.Equal(SecondPrefix + "movement-and-combat.reserve-release", position);
        else
        {
            // A second-side assault has no Core continuation past its Selection (022D frontier).
            Assert.Equal("select-contact-assault", kinds[^1]);
            Assert.Equal(SecondPrefix + "movement-and-combat.combat.barrage", position);
        }
    }

    private static string WithoutChainLink(byte[] resultEvent)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(resultEvent)!.AsObject();
        Assert.True(node.Remove("priorAntiArmorHash"));
        return node.ToJsonString();
    }

    // Drives one entry assault in the Host's order: a single offered audience each step, except the
    // two simultaneous seals (Axis first); an owner choice takes the lowest action ID, except Cycle
    // Control, which takes Finish. Each accepted step is compared with the direct trusted step.
    private static (List<string> Kinds, ExerciseSession Frontier) Drive(ExerciseSession session, string assaultId,
        bool stopAtCycleControl = false, bool releaseReserve = false)
    {
        var kinds = new List<string> { "select-contact-assault" };
        session = Step(session, CampaignActionAudience.Axis, assaultId);
        for (var index = 0; index < 80; index++)
        {
            var sets = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.QueryWithCombat(session, audience).ActionSet!)
                .Where(set => set.Candidates.Count > 0)
                .ToArray();
            if (sets.Length == 0) return (kinds, session);
            Assert.True(sets.Length == 1 || sets.All(set => set.Candidates.Single().Kind == "seal-combat-force-assignment"),
                string.Join(" | ", sets.Select(set => set.Audience + ":" + string.Join(",", set.Candidates.Select(value => value.Kind)))));
            var set = sets[0];
            if (stopAtCycleControl && set.Candidates.Any(value => value.Kind.StartsWith("combat-cycle-control-",
                    StringComparison.Ordinal)))
                return (kinds, session);
            var candidate = set.Candidates.FirstOrDefault(value => value.Kind == "combat-cycle-control-finish")
                ?? (releaseReserve
                    ? set.Candidates.FirstOrDefault(value => value.Kind == "combat-reserve-release-release-i")
                    : null)
                ?? set.Candidates.OrderBy(value => value.ActionId, StringComparer.Ordinal).First();
            Assert.True(set.Candidates.Count == 1 || candidate is CombatExerciseOwnerChoiceAction
                or DesignateReserveAction or CompleteReserveDesignationAction or SelectContactAssaultAction
                or FinishCombatWithoutAttackAction, candidate.Kind);
            var direct = Direct(session, set, candidate);
            var submitted = CampaignExercises.SubmitWithCombat(session, Command(session, set.Audience, candidate.ActionId));
            Assert.True(submitted.IsAccepted, $"{candidate.Kind}: {submitted.RejectionReason}");
            Assert.True(direct.IsAccepted, candidate.Kind);
            Assert.Equal(direct.Evidence!.EventRecords, submitted.Evidence!.EventRecords);
            Assert.Equal(direct.Evidence.SnapshotCheckpoint, submitted.Evidence.SnapshotCheckpoint);
            var directReceipt = direct.Evidence.Receipt;
            var receipt = submitted.Evidence.Receipt;
            Assert.Equal((directReceipt.CampaignId, directReceipt.PriorStateVersion, directReceipt.CommittedStateVersion,
                    directReceipt.Audience, directReceipt.ActionId),
                (receipt.CampaignId, receipt.PriorStateVersion, receipt.CommittedStateVersion, receipt.Audience,
                    receipt.ActionId));
            // T022-Q7: the harness re-issues only receipts that name the step's own cut.
            var successor = CampaignExercises.QueryCheckpoint(submitted.SuccessorSession!).PositionId;
            Assert.Equal(successor, receipt.ResultingPositionId);
            if (directReceipt.ResultingPositionId != successor)
            {
                Assert.Equal(set.PositionId, directReceipt.ResultingPositionId);
                Assert.Contains(candidate.Kind, ReissuedReceiptKinds);
            }
            kinds.Add(candidate.Kind);
            session = submitted.SuccessorSession!;
        }
        throw new InvalidOperationException("No frontier within the covered steps.");
    }

    private static ExerciseStepResult Direct(ExerciseSession session, CampaignLegalActionSet set, CampaignActionCandidate candidate)
    {
        var command = Command(session, set.Audience, candidate.ActionId);
        return candidate switch
        {
            DeclineRetreatBeforeAssaultAction decline =>
                CampaignHostAdapter.SubmitContactDefenderRbaDecline(session, command, decline.ElementId),
            SealCombatForceAssignmentAction seal =>
                CampaignHostAdapter.SubmitContactOwnerForceSeal(session, command, seal.ElementId),
            CombatExerciseOwnerChoiceAction { Stage: CombatExerciseOwnerChoiceAction.CustodyStage } custody =>
                CampaignHostAdapter.SubmitNativeCombatCustody(session, command, custody.SubjectId),
            CombatExerciseOwnerChoiceAction { Stage: CombatExerciseOwnerChoiceAction.ReserveReleaseStage } release =>
                CampaignHostAdapter.SubmitNativeReserveReleaseChoice(session, command, release.SubjectId),
            CombatExerciseOwnerChoiceAction { Stage: CombatExerciseOwnerChoiceAction.CycleControlStage } =>
                CampaignHostAdapter.SubmitNativeCycleControl(session, command),
            CombatExerciseOwnerChoiceAction { Stage: CombatExerciseOwnerChoiceAction.TruckConvoyPassStage } =>
                CampaignHostAdapter.SubmitTruckConvoyPass(session, command),
            CombatExerciseOwnerChoiceAction { Stage: CombatExerciseOwnerChoiceAction.PostCombatPassStage } =>
                CampaignHostAdapter.SubmitPostCombatPass(session, command),
            CombatExerciseOwnerChoiceAction { Stage: CombatExerciseOwnerChoiceAction.SecondMovementStage } move =>
                CampaignHostAdapter.SubmitSecondMovement(session, command, move.SubjectId),
            CombatExerciseOwnerChoiceAction { Stage: CombatExerciseOwnerChoiceAction.SecondMovementCompletionStage } =>
                CampaignHostAdapter.SubmitSecondMovementCompletion(session, command),
            DesignateReserveAction or CompleteReserveDesignationAction =>
                CampaignHostAdapter.SubmitSecondReserve(session, command),
            SelectContactAssaultAction assault =>
                CampaignHostAdapter.SubmitContactOwnerCombatSelection(session, command, assault.ActingElementId),
            FinishCombatWithoutAttackAction finish =>
                CampaignHostAdapter.SubmitContactOwnerCombatSelection(session, command, finish.ActingElementId),
            _ => candidate.Kind switch
            {
                "complete-combat-barrage" => CampaignHostAdapter.CompleteNativeCombatBarrage(session),
                "cancel-combat-force-unavailable" => CampaignHostAdapter.CancelNativeCombatForceAssignment(session,
                    null, -1, -1, -1, null, "force-unavailable"),
                "complete-combat-force-assignment" => CampaignHostAdapter.CompleteNativeCombatForceAssignment(session),
                "complete-combat-anti-armor" => CampaignHostAdapter.CompleteNativeCombatAntiArmor(session),
                "complete-combat-cancelled-assault" => CampaignHostAdapter.CompleteNativeCombatCancelledAssault(session),
                "resolve-combat-assault" => CampaignHostAdapter.ResolveNativeCombatAssault(session),
                "complete-combat-no-retreat" => CampaignHostAdapter.CompleteNoPostAssaultRetreat(session),
                "settle-combat-world" => CampaignHostAdapter.SettleNativeCombatWorld(session),
                "complete-combat-no-custody" => CampaignHostAdapter.CompleteNoNativeCombatCustody(session),
                "complete-combat-successor" => CampaignHostAdapter.CompleteNativeCombatSuccessor(session),
                "complete-combat-reserve-release" => CampaignHostAdapter.CompleteNativeReserveRelease(session),
                "complete-second-side-breakdown" => CampaignHostAdapter.CompleteSecondBreakdown(session),
                "complete-second-side-combat-no-attack" => CampaignHostAdapter.CompleteSecondCombatNoAttack(session),
                _ => throw new InvalidOperationException(candidate.Kind),
            },
        };
    }

    private static ExerciseSession AtCombatPosition(string campaignId, ulong seed)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
            value.SetupId == "rules-lab.breakdown.reaction.combat-double-fresh.v1"));
        var session = CampaignExercises.Begin(new CampaignCreationRequest(1, campaignId, Cna1979Ruleset.Manifest.Hash,
            seed, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash,
            setup.Content.ScenarioId)).Session!;
        var reserveDesignated = false;
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination)
                return session;
            var set = Enum.GetValues<CampaignActionAudience>().Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var reserve = !reserveDesignated ? set.Candidates.OfType<DesignateReserveAction>()
                .FirstOrDefault(value => value.ElementId == "axis-infantry-second") : null;
            var action = reserve ?? set.Candidates.FirstOrDefault(value => value is not MoveElementAction) ?? set.Candidates[0];
            if (reserve is not null) reserveDesignated = true;
            session = CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
                set.StateVersion, set.PositionId, set.Audience, action.ActionId)).SuccessorSession!;
        }
        throw new InvalidOperationException("Combat position was not reached.");
    }

    private static string Single(ExerciseSession session, CampaignActionAudience audience) =>
        Assert.Single(CampaignExercises.QueryWithCombat(session, audience).ActionSet!.Candidates).ActionId;

    private static ExerciseSession Step(ExerciseSession session, CampaignActionAudience audience, string actionId)
    {
        var result = CampaignExercises.SubmitWithCombat(session, Command(session, audience, actionId));
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        return result.SuccessorSession!;
    }

    private static CampaignActionSubmission Command(ExerciseSession session, CampaignActionAudience audience, string actionId)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(CampaignActionSubmission.CurrentContractVersion, checkpoint.CampaignId, checkpoint.StateVersion,
            checkpoint.PositionId, audience, actionId);
    }
}
