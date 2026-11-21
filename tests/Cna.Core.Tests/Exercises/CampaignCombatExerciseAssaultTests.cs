using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Tests.Actions;

namespace Cna.Core.Tests.Exercises;

// Task 022B (#448): decline-only RBA, Force Assignment, Anti Armor and the result or cancelled
// assault through the trusted Exercise path. Every accepted step must equal the trusted Core step the
// Host appends at the same cut. Test-harness capability only; not Combat activation or certification.
public sealed class CampaignCombatExerciseAssaultTests
{
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

    [Theory]
    [InlineData(6)]
    [InlineData(12345)]
    public void EveryEntryAssaultFollowsTheHostOrchestrationToItsFrontier(ulong seed)
    {
        var entry = CombatCycleDisclosureForceTests.AdvanceToCombat($"campaign-022b-entry-{seed}", seed);
        var axis = CampaignExercises.QueryWithCombat(entry, CampaignActionAudience.Axis).ActionSet!;
        var assaults = axis.Candidates.OfType<SelectContactAssaultAction>().ToArray();
        Assert.NotEmpty(assaults);
        var paths = new List<string>();
        foreach (var assault in assaults)
        {
            var (kinds, frontier) = Drive(entry, assault.ActionId);
            Assert.True(kinds.SequenceEqual(Payable) || kinds.SequenceEqual(Unpayable), string.Join(" > ", kinds));
            paths.Add(kinds[^1]);
            Assert.True(CampaignExercises.Reconstruct(frontier).IsVerified);
            var position = CampaignExercises.QueryCheckpoint(frontier).PositionId;
            Assert.EndsWith(kinds[^1] == "resolve-combat-assault" ? ".combat.close-assault" : ".reserve-release",
                position, StringComparison.Ordinal);
            Assert.All(Enum.GetValues<CampaignActionAudience>(), audience =>
                Assert.Empty(CampaignExercises.QueryWithCombat(frontier, audience).ActionSet!.Candidates));
        }
        // The fixture's infantry pair is payable (020A1's Force fixture).
        Assert.Contains("resolve-combat-assault", paths);
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

    private static string WithoutChainLink(byte[] resultEvent)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(resultEvent)!.AsObject();
        Assert.True(node.Remove("priorAntiArmorHash"));
        return node.ToJsonString();
    }

    // Drives one entry assault with the Host's order: a single offered audience each step, except the
    // two simultaneous seals (Axis first). Each accepted step is compared with the direct trusted step.
    private static (List<string> Kinds, ExerciseSession Frontier) Drive(ExerciseSession session, string assaultId)
    {
        var kinds = new List<string> { "select-contact-assault" };
        session = Step(session, CampaignActionAudience.Axis, assaultId);
        for (var index = 0; index < 12; index++)
        {
            var sets = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.QueryWithCombat(session, audience).ActionSet!)
                .Where(set => set.Candidates.Count > 0)
                .ToArray();
            if (sets.Length == 0) return (kinds, session);
            Assert.True(sets.Length == 1 || sets.All(set => set.Candidates.Single().Kind == "seal-combat-force-assignment"));
            var set = sets[0];
            var candidate = Assert.Single(set.Candidates);
            var direct = Direct(session, set, candidate);
            var submitted = CampaignExercises.SubmitWithCombat(session, Command(session, set.Audience, candidate.ActionId));
            Assert.True(submitted.IsAccepted, $"{candidate.Kind}: {submitted.RejectionReason}");
            Assert.True(direct.IsAccepted, candidate.Kind);
            Assert.Equal(direct.Evidence!.EventRecords, submitted.Evidence!.EventRecords);
            Assert.Equal(direct.Evidence.SnapshotCheckpoint, submitted.Evidence.SnapshotCheckpoint);
            var directReceipt = direct.Evidence.Receipt;
            if (set.Audience == CampaignActionAudience.System) Assert.Equal(directReceipt, submitted.Evidence.Receipt);
            else
            {
                // T022-Q7: owner-decision receipts name the cut the decision was taken at; only the
                // harness re-issues the resulting position as the successor checkpoint.
                Assert.Equal(set.PositionId, directReceipt.ResultingPositionId);
                var receipt = submitted.Evidence.Receipt;
                Assert.Equal((directReceipt.CampaignId, directReceipt.PriorStateVersion, directReceipt.CommittedStateVersion,
                        directReceipt.Audience, directReceipt.ActionId),
                    (receipt.CampaignId, receipt.PriorStateVersion, receipt.CommittedStateVersion, receipt.Audience,
                        receipt.ActionId));
            }
            Assert.Equal(CampaignExercises.QueryCheckpoint(submitted.SuccessorSession!).PositionId,
                submitted.Evidence.Receipt.ResultingPositionId);
            kinds.Add(candidate.Kind);
            session = submitted.SuccessorSession!;
        }
        throw new InvalidOperationException("No frontier within the covered steps.");
    }

    private static ExerciseStepResult Direct(ExerciseSession session, CampaignLegalActionSet set, CampaignActionCandidate candidate) =>
        candidate switch
        {
            DeclineRetreatBeforeAssaultAction decline => CampaignHostAdapter.SubmitContactDefenderRbaDecline(session,
                Command(session, set.Audience, decline.ActionId), decline.ElementId),
            SealCombatForceAssignmentAction seal => CampaignHostAdapter.SubmitContactOwnerForceSeal(session,
                Command(session, set.Audience, seal.ActionId), seal.ElementId),
            _ => candidate.Kind switch
            {
                "complete-combat-barrage" => CampaignHostAdapter.CompleteNativeCombatBarrage(session),
                "cancel-combat-force-unavailable" => CampaignHostAdapter.CancelNativeCombatForceAssignment(session,
                    null, -1, -1, -1, null, "force-unavailable"),
                "complete-combat-force-assignment" => CampaignHostAdapter.CompleteNativeCombatForceAssignment(session),
                "complete-combat-anti-armor" => CampaignHostAdapter.CompleteNativeCombatAntiArmor(session),
                "complete-combat-cancelled-assault" => CampaignHostAdapter.CompleteNativeCombatCancelledAssault(session),
                "resolve-combat-assault" => CampaignHostAdapter.ResolveNativeCombatAssault(session),
                _ => throw new InvalidOperationException(candidate.Kind),
            },
        };

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
