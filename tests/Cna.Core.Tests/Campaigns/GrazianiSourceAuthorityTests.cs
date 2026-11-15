using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceAuthorityTests
{
    [Theory]
    [InlineData(0UL, 3, 1)]
    [InlineData(14UL, 0, 0)]
    public void AxisFinalizesRealSupplyPlanAndReplaysWithoutStartingGame(ulong seed, int maximum, int allocated)
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "source-authority-transition", Cna1979Ruleset.Manifest.Hash, seed,
            CampaignPregameSourceCreation.SetupId,
            CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, initialization),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var session = CampaignExercises.Begin(request, artifact, initialization).Session!;
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis));
        var finishBeforeRoll = Submission(session, CampaignSourceAuthorityState.FinishActionId, CampaignActionAudience.Axis);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.FinishPregameAxisSupply(session, finishBeforeRoll).RejectionReason);
        var roll = Assert.Single(CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates);
        session = CampaignExercises.Submit(session, Submission(session, roll.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        if (allocated > 0)
        {
            var add = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates
                .OfType<PregameAxisSupplyAllocationAction>()
                .Single(value => value.Lane == 2 && value.Stage == 1 && value.Delta == 1);
            session = CampaignExercises.Submit(session, Submission(session, add.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        var legacyOffers = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates.Select(x => x.ActionId).ToArray();
        var draft = CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis);
        Assert.Single(draft.PrivateActionIds);
        Assert.DoesNotContain(draft.PrivateActionIds[0], legacyOffers);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.FinishPregameAxisSupply(session, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Commonwealth)).RejectionReason);
        var accepted = CampaignExercises.FinishPregameAxisSupply(session, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Axis));
        Assert.True(accepted.IsAccepted);
        var next = accepted.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(next).IsVerified);
        var earlyCompact = CampaignExercises.AdmitSourceCompactCheckpoint(next).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(earlyCompact).ContractVersion);
        var earlyDump = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        earlyCompact = CampaignExercises.PlaceSourceAxisDump(earlyCompact,
            Submission(earlyCompact, earlyDump, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(earlyCompact).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(earlyCompact).IsVerified);
        var normalAxis = CampaignExercises.Query(next, CampaignActionAudience.Axis);
        Assert.True(normalAxis.IsSuccessful);
        Assert.Equal(CampaignLegalActionSet.SourceSetupPolicyId, normalAxis.ActionSet!.PolicyId);
        var encodedActions = CampaignLegalActionSerializer.Serialize(normalAxis.ActionSet);
        Assert.Equal(encodedActions, CampaignLegalActionSerializer.Serialize(CampaignLegalActionSerializer.DeserializeCanonical(encodedActions)));
        var unifiedDump = normalAxis.ActionSet.Candidates.OfType<CampaignSourceSetupAction>()
            .Single(value => value.Family == "axis-dump" && value.ChoiceJson.Contains("c4507", StringComparison.Ordinal)
                && value.ChoiceJson.Contains("axis-dump-1", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(normalAxis.ActionSet.CampaignId,
            normalAxis.ActionSet.StateVersion, normalAxis.ActionSet.RulesetHash, normalAxis.ActionSet.PositionId,
            CampaignActionAudience.Axis, [unifiedDump], CampaignLegalActionSet.CurrentPolicyId));
        var ownerCut = CampaignExercises.QuerySourceActionsDraft(next, CampaignActionAudience.Axis);
        Assert.DoesNotContain(unifiedDump.ActionId, System.Text.Encoding.UTF8.GetString(ownerCut.VisibleBytes));
        var ownContext = System.Text.Encoding.UTF8.GetString(ownerCut.VisibleBytes);
        Assert.Contains("axis-gt1-supply", ownContext);
        Assert.Contains("maximumSupply", ownContext);
        var hiddenCwAction = CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-dump-1", "d3612");
        var hiddenAdvance = CampaignExercises.PlaceSourceCommonwealthSupply(next,
            Submission(next, hiddenCwAction, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var afterHidden = CampaignExercises.QuerySourceActionsDraft(hiddenAdvance, CampaignActionAudience.Axis);
        Assert.Equal(ownerCut.VisibleBytes, afterHidden.VisibleBytes);
        Assert.NotEqual(ownerCut.PrivateActionIds[0], afterHidden.PrivateActionIds[0]);
        var wrongSide = Submission(next, unifiedDump.ActionId, CampaignActionAudience.Commonwealth);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(next, wrongSide).RejectionReason);
        var unifiedResult = CampaignExercises.Submit(next, Submission(next, unifiedDump.ActionId, CampaignActionAudience.Axis));
        Assert.True(unifiedResult.IsAccepted);
        Assert.Equal(unifiedDump.ActionId, unifiedResult.Evidence!.Receipt.ActionId);
        Assert.True(CampaignExercises.Reconstruct(unifiedResult.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(unifiedResult.SuccessorSession!,
            Submission(next, unifiedDump.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(unifiedResult.SuccessorSession!,
            Submission(unifiedResult.SuccessorSession!, unifiedDump.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.FinishPregameAxisSupply(next, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Axis)).RejectionReason);
        using var snapshot = JsonDocument.Parse(accepted.Evidence!.SnapshotCheckpoint);
        Assert.Equal("setup-in-progress", snapshot.RootElement.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, snapshot.RootElement.GetProperty("gameClock").ValueKind);
        using var plan = JsonDocument.Parse(snapshot.RootElement.GetProperty("convoyPlanBase64").GetBytesFromBase64());
        Assert.Equal(maximum, plan.RootElement.GetProperty("maximumSupply").GetInt32());
        Assert.Equal(maximum - allocated, plan.RootElement.GetProperty("unusedAvailability").GetInt32());
        using var source = JsonDocument.Parse(snapshot.RootElement.GetProperty("sourceSnapshotBase64").GetBytesFromBase64());
        Assert.Contains(source.RootElement.GetProperty("pendingArrivalGroupIds").EnumerateArray().Select(x => x.GetString()),
            id => id == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId);
        var dumpDraft = CampaignExercises.QuerySourceAxisDumpDraft(next, CampaignActionAudience.Axis);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisDumpDraft(next, CampaignActionAudience.Commonwealth));
        Assert.Equal(8, dumpDraft.PrivateActionIds.Count);
        var chosenId = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        Assert.Contains(chosenId, dumpDraft.PrivateActionIds);
        var dumpCommand = Submission(next, chosenId, CampaignActionAudience.Axis);
        var placed = CampaignExercises.PlaceSourceAxisDump(next, dumpCommand);
        Assert.True(placed.IsAccepted);
        var after = placed.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            CampaignExercises.PlaceSourceAxisDump(after, dumpCommand).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceAxisDump(after, Submission(after,
                CampaignSourceAuthorityState.DumpActionId("axis-dump-2", "c4507"), CampaignActionAudience.Axis)).RejectionReason);
        var second = CampaignExercises.PlaceSourceAxisDump(after, Submission(after,
            CampaignSourceAuthorityState.DumpActionId("axis-dump-2", "c4707"), CampaignActionAudience.Axis));
        Assert.True(second.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(second.SuccessorSession!).IsVerified);
        var corruptHistory = second.SuccessorSession!.PregameHistory.Select(value => value.ToArray()).ToArray();
        var lastEvent = System.Text.Encoding.UTF8.GetString(corruptHistory[^1]).Replace("c4707", "c4507", StringComparison.Ordinal);
        corruptHistory[^1] = System.Text.Encoding.UTF8.GetBytes(lastEvent);
        var corrupt = new ExerciseSession(second.SuccessorSession.Pregame!, second.SuccessorSession.CurrentContacts,
            corruptHistory, second.SuccessorSession.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(corrupt).IsVerified);
        using var finalSnapshot = JsonDocument.Parse(second.Evidence!.SnapshotCheckpoint);
        using var world = JsonDocument.Parse(finalSnapshot.RootElement.GetProperty("axisSupplyWorldBase64").GetBytesFromBase64());
        Assert.Equal(22, world.RootElement.GetProperty("units").GetArrayLength());
        Assert.False(world.RootElement.GetProperty("siteCoverageComplete").GetBoolean());
        Assert.Equal(JsonValueKind.Null, world.RootElement.GetProperty("units").EnumerateArray()
            .Single(x => x.GetProperty("id").GetString() == "axis-derna-1").GetProperty("ammo").ValueKind);
        var cwSession = second.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceSetupSeat(cwSession, CampaignActionAudience.Axis));
        Assert.True(CampaignExercises.IsSourceSetupSeat(cwSession, CampaignActionAudience.Commonwealth));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Axis));
        var placements = new[]
        {
            ("cw-dump-1", "d3612"),
            ("cw-alexandria-1", "e3613"), ("cw-alexandria-2", "e3613"), ("cw-alexandria-3", "e3714"),
            ("cw-cairo-1", "e1829"), ("cw-cairo-2", "e1830"), ("cw-cairo-3", "e1930"),
        };
        foreach (var (id, location) in placements)
        {
            var idHash = CampaignSourceAuthorityState.CommonwealthSupplyActionId(id, location);
            var cwDraft = CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Commonwealth);
            Assert.Contains(idHash, cwDraft.PrivateActionIds);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCommonwealthSupply(cwSession, Submission(cwSession, idHash, CampaignActionAudience.Axis)).RejectionReason);
            var step = CampaignExercises.PlaceSourceCommonwealthSupply(cwSession, Submission(cwSession, idHash, CampaignActionAudience.Commonwealth));
            Assert.True(step.IsAccepted);
            cwSession = step.SuccessorSession!;
            Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        }
        Assert.Empty(CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.Equal(15, cwSession.SourceAuthority!.CommonwealthSupplyWorld.Units.Count);
        Assert.Equal(18, cwSession.SourceAuthority.CommonwealthInitialForces.Forces.Count);
        Assert.Null(cwSession.SourceAuthority.CommonwealthInitialForces.Forces
            .Single(value => value.ElementId == "central-india-horse").InitialToes);
        Assert.Null(cwSession.SourceAuthority.Position);
        var axisDummy = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-1", "c4407");
        Assert.Contains(axisDummy, CampaignExercises.QuerySourceDummyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, axisDummy, CampaignActionAudience.Commonwealth)).RejectionReason);
        var dummyStep = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, axisDummy, CampaignActionAudience.Axis));
        Assert.True(dummyStep.IsAccepted);
        cwSession = dummyStep.SuccessorSession!;
        var omit = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-2", null);
        cwSession = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, omit, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwDummy = CampaignSourceAuthorityState.DummyActionId(false, "cw-dummy-1", "d3611");
        Assert.Contains(cwDummy, CampaignExercises.QuerySourceDummyDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        cwSession = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, cwDummy, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var priorRandom = cwSession.SourceAuthority!.RandomState;
        var expectedGt1 = SandtableRandom.RollD6(priorRandom);
        var rollGt1 = CampaignSourceAuthorityState.OpeningConvoyActionId("roll", 0, 0, 0);
        Assert.Single(CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, rollGt1, CampaignActionAudience.Commonwealth)).RejectionReason);
        var gt1Step = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, rollGt1, CampaignActionAudience.Axis));
        Assert.True(gt1Step.IsAccepted);
        cwSession = gt1Step.SuccessorSession!;
        Assert.Equal(expectedGt1.Value, cwSession.SourceAuthority!.OpeningConvoyPlan.Die);
        Assert.Equal(expectedGt1.State.NextByteCursor, cwSession.SourceAuthority.RandomState.NextByteCursor);
        if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply > 0)
        {
            var gt1Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 2, 1, 1);
            cwSession = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, gt1Add, CampaignActionAudience.Axis)).SuccessorSession!;
            Assert.Single(cwSession.SourceAuthority!.OpeningConvoyPlan.Allocations());
        }
        var gt1Finish = CampaignSourceAuthorityState.OpeningConvoyActionId("finish", 0, 0, 0);
        cwSession = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, gt1Finish, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(cwSession.SourceAuthority!.OpeningConvoyPlan.IsFinished);
        Assert.Empty(CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var skipCwGt1 = CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId("skip", 1);
        var cwPlanning = CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Commonwealth);
        Assert.Contains(skipCwGt1, cwPlanning.PrivateActionIds);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Axis));
        var beforeSkip = cwSession.SourceAuthority.RandomState.NextByteCursor;
        cwSession = CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession,
            Submission(cwSession, skipCwGt1, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(beforeSkip, cwSession.SourceAuthority!.RandomState.NextByteCursor);
        Assert.Equal("skipped-opening", cwSession.SourceAuthority.CommonwealthSupplyPlans!.Entries[0].Status);
        foreach (var turn in new[] { 2, 3, 4 })
        {
            var prior = cwSession.SourceAuthority.RandomState;
            var expected = SandtableRandom.RollD6(prior);
            var action = CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId("roll", turn);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession, Submission(cwSession, action, CampaignActionAudience.Axis)).RejectionReason);
            cwSession = CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession,
                Submission(cwSession, action, CampaignActionAudience.Commonwealth)).SuccessorSession!;
            var entry = cwSession.SourceAuthority!.CommonwealthSupplyPlans!.Entries.Single(value => value.TargetGameTurn == turn);
            Assert.Equal(expected.Value, entry.Die);
            Assert.Equal((expected.Value + 1) / 2, entry.SupplyUnits);
            Assert.Equal(expected.State.NextByteCursor, cwSession.SourceAuthority.RandomState.NextByteCursor);
        }
        Assert.Empty(CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        // Exercise the real prestart September plans on an independent branch of the retained source session.
        var september = cwSession;
        var beginSeptember = CampaignSourceAuthorityState.SeptemberSupplyActionId("begin", 0, 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceSeptemberSupplyDraft(september, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, beginSeptember, CampaignActionAudience.Commonwealth)).RejectionReason);
        var normalSeptember = CampaignExercises.Query(september, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "axis-september-supply");
        Assert.DoesNotContain(CampaignExercises.Query(september, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "axis-september-supply");
        var beginCommand = Submission(september, normalSeptember.ActionId, CampaignActionAudience.Axis);
        september = CampaignExercises.Submit(september, beginCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(september, beginCommand).RejectionReason);
        foreach (var turn in new[] { 3, 4 })
        {
            var expected = SandtableRandom.RollD6(september.SourceAuthority!.RandomState);
            var septemberRoll = CampaignSourceAuthorityState.SeptemberSupplyActionId("roll", turn, 0, 0, 0);
            september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, septemberRoll, CampaignActionAudience.Axis)).SuccessorSession!;
            var entry = september.SourceAuthority!.SeptemberPlans!.Entries.Single(value => value.TargetGameTurn == turn);
            Assert.Equal(expected.Value, entry.Die);
            Assert.Equal(expected.Value switch { 1 => 0, <= 3 => 1, <= 5 => 2, _ => 3 }, entry.Maximum);
            Assert.Equal(expected.State.NextByteCursor, september.SourceAuthority.RandomState.NextByteCursor);
            var finish = CampaignSourceAuthorityState.SeptemberSupplyActionId("finish", turn, 0, 0, 0);
            if (entry.Maximum > 0)
            {
                var zeroPlan = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, finish, CampaignActionAudience.Axis)).SuccessorSession!;
                Assert.True(zeroPlan.SourceAuthority!.SeptemberPlans!.Entries.Single(value => value.TargetGameTurn == turn).Finished);
                Assert.Equal(0, zeroPlan.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == turn).Allocations.Sum());
                var add = CampaignSourceAuthorityState.SeptemberSupplyActionId("adjust", turn, 2, 1, 1);
                september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, add, CampaignActionAudience.Axis)).SuccessorSession!;
            }
            september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, finish, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        Assert.Null(september.SourceAuthority!.SeptemberPlans!.NextTargetGameTurn);
        var septemberHash = september.SourceAuthority.SeptemberPlans.Hash();
        september = CampaignExercises.AdmitSourceAxisSupplement(september).SuccessorSession!;
        Assert.Equal(septemberHash, september.SourceAuthority!.SeptemberPlans!.Hash());
        Assert.True(CampaignExercises.Reconstruct(september).IsVerified);
        Assert.False(CampaignExercises.IsSourceAxisSupplementAdmitted(cwSession));
        var migrated = CampaignExercises.AdmitSourceAxisSupplement(cwSession);
        Assert.True(migrated.IsAccepted);
        cwSession = migrated.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceAxisSupplementAdmitted(cwSession));
        Assert.Equal(5, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceAxisSupplement(cwSession).RejectionReason);
        var replacements = CampaignSourceAuthorityState.AutoblindaActionId("replacements", "");
        Assert.Contains(replacements, CampaignExercises.QuerySourceAutoblindaDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, replacements, CampaignActionAudience.Commonwealth)).RejectionReason);
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, replacements, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, cwSession.SourceAuthority!.Autoblinda!.ReplacementPoints);
        var reset = CampaignSourceAuthorityState.AutoblindaActionId("reset", "");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, reset, CampaignActionAudience.Axis)).SuccessorSession!;
        var attach1 = CampaignSourceAuthorityState.AutoblindaActionId("attach", "1-141-infantry");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach1, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach1, CampaignActionAudience.Axis)).RejectionReason);
        var attach2 = CampaignSourceAuthorityState.AutoblindaActionId("attach", "2-141-infantry");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach2, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(cwSession.SourceAuthority!.Autoblinda!.IsComplete);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.False(CampaignExercises.IsSourceCommonwealthSupplementAdmitted(cwSession));
        var cwMigrated = CampaignExercises.AdmitSourceCommonwealthSupplement(cwSession);
        Assert.True(cwMigrated.IsAccepted);
        cwSession = cwMigrated.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCommonwealthSupplementAdmitted(cwSession));
        Assert.Equal(6, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(12, cwSession.SourceAuthority!.CommonwealthSupplement!.Forces.Count);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceCommonwealthSupplement(cwSession).RejectionReason);
        Assert.False(CampaignExercises.IsSourcePriorityMotorizationAdmitted(cwSession));
        var priorityMigration = CampaignExercises.AdmitSourcePriorityMotorization(cwSession);
        Assert.True(priorityMigration.IsAccepted);
        cwSession = priorityMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourcePriorityMotorizationAdmitted(cwSession));
        Assert.Equal(7, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourcePriorityMotorizationDraft(cwSession, CampaignActionAudience.Axis));
        var priorityId = CampaignSourceAuthorityState.PriorityMotorizationActionId("1-kings-royal-rifle-corps", 6);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourcePriorityMotorization(cwSession, Submission(cwSession, priorityId, CampaignActionAudience.Axis)).RejectionReason);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, priorityId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(14, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3922-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        var reversePriority = CampaignSourceAuthorityState.PriorityMotorizationActionId("1-kings-royal-rifle-corps", -6);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, reversePriority, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(20, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3922-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        var secondPriority = CampaignSourceAuthorityState.PriorityMotorizationActionId("2-rifle-brigade", 6);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, secondPriority, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(4, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3320-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var axisMigration = CampaignExercises.AdmitSourceAxisPriorityMotorization(cwSession);
        Assert.True(axisMigration.IsAccepted);
        cwSession = axisMigration.SuccessorSession!;
        Assert.Equal(8, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var targetAxis = "1-1-libyan-motor-infantry";
        var oldDetach = CampaignSourceAuthorityState.AxisPriorityActionId("detach", targetAxis, 0);
        var historical = CampaignExercises.ReplaySourceAxisPriority(cwSession,
            Submission(cwSession, oldDetach, CampaignActionAudience.Axis));
        Assert.True(historical.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(historical.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, oldDetach, CampaignActionAudience.Axis)).RejectionReason);
        var prematureLift = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, 5);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, prematureLift, CampaignActionAudience.Axis)).RejectionReason);
        foreach (var axisTarget in CampaignSourceAxisPriorityMotorization.Targets)
        {
            var detach = CampaignSourceAuthorityState.AxisPriorityActionId("detach", axisTarget, 0);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, detach, CampaignActionAudience.Axis)).RejectionReason);
        }
        Assert.Equal(35, cwSession.SourceAuthority!.AxisPriorityMotorization!.Remaining(cwSession.SourceAuthority.AxisMotorization));
        var undoDetachment = CampaignSourceAuthorityState.AxisPriorityActionId("undo-detachment", targetAxis, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, undoDetachment, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var garrisonMigration = CampaignExercises.AdmitSourceAxisMinorGarrisons(cwSession);
        Assert.True(garrisonMigration.IsAccepted);
        cwSession = garrisonMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceAxisMinorGarrisonsAdmitted(cwSession));
        Assert.Equal(9, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(13, cwSession.SourceAuthority!.AxisGarrisons!.Forces.Count);
        Assert.Contains(cwSession.SourceAuthority.AxisGarrisons.Forces,
            value => value.ElementId == "giarabub-motor-machine-gun");
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceAxisMinorGarrisons(cwSession).RejectionReason);
        var battlefieldMigration = CampaignExercises.AdmitSourceBattlefield(cwSession);
        Assert.True(battlefieldMigration.IsAccepted);
        cwSession = battlefieldMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceBattlefieldAdmitted(cwSession));
        Assert.Equal(10, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var battlefield = cwSession.SourceAuthority!.Battlefield!;
        Assert.Equal(battlefield.Entries.Count, battlefield.Entries.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, battlefield.Pending.Count);
        Assert.Equal(8, battlefield.Pending.Single(value => value.GroupId == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId).ElementIds.Count);
        Assert.Equal(4, battlefield.Pending.Single(value => value.GroupId == Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId).ElementIds.Count);
        using var battlefieldBytes = JsonDocument.Parse(battlefield.SerializeCanonical());
        Assert.False(battlefieldBytes.RootElement.GetProperty("battlefieldComplete").GetBoolean());
        Assert.False(battlefieldBytes.RootElement.GetProperty("controlCertified").GetBoolean());
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceBattlefield(cwSession).RejectionReason);
        var cairoAdmission = CampaignExercises.AdmitSourceCairoSetup(cwSession);
        Assert.True(cairoAdmission.IsAccepted);
        cwSession = cairoAdmission.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCairoSetupAdmitted(cwSession));
        Assert.Equal(11, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(4, cwSession.SourceAuthority!.CairoSetup!.PendingIds.Count);
        Assert.Equal(4, cwSession.SourceAuthority.Battlefield!.Entries.Count(x => x.AuthorityStatus == "source-setup-unplaced"));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCairoSetupDraft(cwSession, CampaignActionAudience.Axis));
        var invalidCity = CampaignSourceAuthorityState.CairoPlacementActionId("1-welch", "e1829");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, invalidCity, CampaignActionAudience.Commonwealth)).RejectionReason);
        var inferredCity = CampaignSourceAuthorityState.CairoPlacementActionId("1-welch", "e1931");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, inferredCity, CampaignActionAudience.Commonwealth)).RejectionReason);
        var chosenCairoHex = seed == 14 ? "e1830" : "e1730";
        foreach (var element in CampaignSourceCairoInitialSetup.Units.Select(value => value.ElementId))
        {
            var action = CampaignSourceAuthorityState.CairoPlacementActionId(element, chosenCairoHex);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, action, CampaignActionAudience.Axis)).RejectionReason);
            var command = Submission(cwSession, action, CampaignActionAudience.Commonwealth);
            var placedCairo = CampaignExercises.PlaceSourceCairoUnit(cwSession, command);
            Assert.True(placedCairo.IsAccepted);
            cwSession = placedCairo.SuccessorSession!;
            Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
                CampaignExercises.PlaceSourceCairoUnit(cwSession, command).RejectionReason);
        }
        Assert.Empty(cwSession.SourceAuthority!.CairoSetup!.PendingIds);
        Assert.Equal(4, cwSession.SourceAuthority.Battlefield!.Entries.Count(x => x.LocationId == chosenCairoHex && x.AuthorityStatus == "source-setup-placed"));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var activeMigration = CampaignExercises.AdmitSourceCairoWorld(cwSession);
        Assert.True(activeMigration.IsAccepted);
        cwSession = activeMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCairoWorldAdmitted(cwSession));
        Assert.Equal(12, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var active = cwSession.SourceAuthority!.CairoWorld!;
        Assert.Equal("sha256:3a7fddd510854c81b408e07742dd837f7be416206117ca58ea79357357a11ddd", active.Content.Hash());
        Assert.NotEqual(cwSession.SourceAuthority.Content.Identity.Hash, active.Content.Identity.Hash);
        Assert.Equal(active.Content.Identity, active.World.ContentIdentity);
        Assert.Equal(8, active.World.World.Elements.Count);
        Assert.Equal(8, active.World.World.Representations.Count);
        Assert.Equal(2, active.World.PendingArrivals.Count);
        foreach (var unit in CampaignSourceCairoInitialSetup.Units)
        {
            var element = Assert.Single(active.World.World.Elements, value => value.ElementId == unit.ElementId);
            Assert.Equal(chosenCairoHex, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
        }
        var alteredContentText = System.Text.Encoding.UTF8.GetString(active.Content.CanonicalBytes)
            .Replace("\"initialToe\":6", "\"initialToe\":7", StringComparison.Ordinal);
        var alteredContent = ContentSourceSetupPackArtifact.Read(System.Text.Encoding.UTF8.GetBytes(alteredContentText));
        Assert.NotEqual(active.Content.Hash(), alteredContent.Hash());
        var forgedHistory = cwSession.PregameHistory.Select(value => value.ToArray()).ToArray();
        var forgedEvent = System.Text.Encoding.UTF8.GetString(forgedHistory[^1])
            .Replace(active.Content.Hash(), alteredContent.Hash(), StringComparison.Ordinal);
        forgedHistory[^1] = System.Text.Encoding.UTF8.GetBytes(forgedEvent);
        var forgedSession = new ExerciseSession(cwSession.Pregame!, cwSession.CurrentContacts, forgedHistory, cwSession.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(forgedSession).IsVerified);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceCairoWorld(cwSession).RejectionReason);
        var directV12 = cwSession;
        Assert.True(CampaignExercises.CanPromoteSourceIndependent(directV12));
        directV12 = CampaignExercises.PromoteSourceIndependent(directV12).SuccessorSession!;
        Assert.Equal(14, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(10, directV12.SourceAuthority!.World.World.Elements.Count);
        var genericIdentity = directV12.SourceAuthority.World.ContentIdentity;
        Assert.False(CampaignExercises.CanPromoteSourceIndependent(directV12));
        directV12 = CampaignExercises.AdmitSourceLocalMap(directV12).SuccessorSession!;
        Assert.Equal(15, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var mapIdentity = directV12.SourceAuthority!.World.ContentIdentity;
        Assert.NotEqual(genericIdentity, mapIdentity);
        Assert.Equal(10, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.PromoteSourceExpandedIndependent(directV12).SuccessorSession!;
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(13, directV12.SourceAuthority!.World.World.Elements.Count);
        var expandedIdentity = directV12.SourceAuthority.World.ContentIdentity;
        Assert.False(CampaignExercises.CanPromoteSourceExpandedIndependent(directV12));
        Assert.False(CampaignExercises.CanPromoteSourceMaletti(directV12));
        Assert.Equal(expandedIdentity, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(mapIdentity, directV12.SourceAuthority.LocalMapWorld!.Content.Identity);
        Assert.Equal(genericIdentity, directV12.SourceAuthority.IndependentWorld!.Content.Identity);
        Assert.Equal(13, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.AdmitSourceAttachedInitialLoads(directV12).SuccessorSession!;
        Assert.Equal(17, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        const string loadedTarget = "2-rifle-brigade";
        var attach = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("attach", loadedTarget);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, attach, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, attach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, directV12.SourceAuthority!.AttachedInitialLoads!.Reserved("cw-c3320-attached"));
        var fuel = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("fuel", loadedTarget);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, fuel, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, Assert.Single(directV12.SourceAuthority!.AttachedInitialLoads!.Targets).FuelPoints);
        var detachLoad = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("detach", loadedTarget);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, detachLoad, CampaignActionAudience.Commonwealth)).RejectionReason);
        var removeLift = CampaignSourceAuthorityState.PriorityMotorizationActionId(loadedTarget, -6);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourcePriorityMotorization(directV12, Submission(directV12, removeLift, CampaignActionAudience.Commonwealth)).RejectionReason);
        var clear = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("unload-fuel", loadedTarget);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, clear, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, detachLoad, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Empty(directV12.SourceAuthority!.AttachedInitialLoads!.Targets);
        directV12 = CampaignExercises.AdmitSourceInitialWrecks(directV12).SuccessorSession!;
        Assert.Equal(18, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(3, directV12.SourceAuthority!.InitialWrecks!.PendingIds.Count);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceInitialWrecksDraft(directV12, CampaignActionAudience.Axis));
        foreach (var id in directV12.SourceAuthority.InitialWrecks.PendingIds.ToArray())
        {
            var place = CampaignSourceAuthorityState.InitialWreckPlacementActionId(id, "e3613");
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceInitialWreck(directV12, Submission(directV12, place, CampaignActionAudience.Axis)).RejectionReason);
            directV12 = CampaignExercises.PlaceSourceInitialWreck(directV12, Submission(directV12, place, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        }
        Assert.Empty(directV12.SourceAuthority!.InitialWrecks!.PendingIds);
        Assert.Equal(3, directV12.SourceAuthority.InitialWrecks.Placements.Count);
        directV12 = CampaignExercises.AdmitSourceAxisFreeSetup(directV12).SuccessorSession!;
        Assert.Equal(19, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var forgedLegacyState = directV12.SourceAuthority!.ApplyAxisPriorityChoice("detach", targetAxis, 0);
        var forgedLegacyEvent = directV12.SourceAuthority.SerializeAxisPriorityEvent(forgedLegacyState, "detach", targetAxis, 0);
        var forgedLegacySession = new ExerciseSession(directV12.Pregame!, directV12.CurrentContacts.AdvancePregameSource(directV12.SourceAuthority.StateVersion),
            [.. directV12.PregameHistory, forgedLegacyEvent], forgedLegacyState);
        Assert.False(CampaignExercises.Reconstruct(forgedLegacySession).IsVerified);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisFreeSetupDraft(directV12, CampaignActionAudience.Commonwealth));
        var freeIds = directV12.SourceAuthority!.AxisFreeArtillery!.PendingIds.ToArray();
        for (var i = 0; i < freeIds.Length; i++)
        {
            var location = i < 2 ? "c4407" : "c4607";
            var action = CampaignSourceAuthorityState.AxisFreePlacementActionId(freeIds[i], location);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, action, CampaignActionAudience.Commonwealth)).RejectionReason);
            if (i == 2)
            {
                var overstack = CampaignSourceAuthorityState.AxisFreePlacementActionId(freeIds[i], "c4407");
                Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                    CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, overstack, CampaignActionAudience.Axis)).RejectionReason);
            }
            directV12 = CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, action, CampaignActionAudience.Axis)).SuccessorSession!;
            if (i == 0)
            {
                directV12 = CampaignExercises.PromoteSourceAxisFreeWorld(directV12).SuccessorSession!;
                Assert.Equal(20, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
                Assert.Equal(14, directV12.SourceAuthority!.World.World.Elements.Count);
                var identityAfterPartial = directV12.SourceAuthority.World.ContentIdentity;
                var adjustAxis = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", 1);
                directV12 = CampaignExercises.AdjustSourceMotorization(directV12,
                    Submission(directV12, adjustAxis, CampaignActionAudience.Axis)).SuccessorSession!;
                directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12,
                    Submission(directV12, attach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
                Assert.Equal(identityAfterPartial, directV12.SourceAuthority!.World.ContentIdentity);
                Assert.Equal(14, directV12.SourceAuthority.World.World.Elements.Count);
                Assert.False(CampaignExercises.CanPromoteSourceIndependent(directV12));
                Assert.False(CampaignExercises.CanPromoteSourceExpandedIndependent(directV12));
            }
        }
        Assert.Empty(directV12.SourceAuthority!.AxisFreeArtillery!.PendingIds);
        Assert.Equal(3, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-1ar-placed"));
        var axisContent = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.PromoteSourceAxisFreeWorld(directV12).SuccessorSession!;
        Assert.Equal(axisContent, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(17, directV12.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(4, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-1ar-active"));
        Assert.False(CampaignExercises.CanPromoteSourceAxisFreeWorld(directV12));
        directV12 = CampaignExercises.AdmitSourceAxisCorpsSetup(directV12).SuccessorSession!;
        Assert.Equal(21, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisCorpsSetupDraft(directV12, CampaignActionAudience.Commonwealth));
        var corpsIds = directV12.SourceAuthority!.AxisCorpsArtillery!.PendingIds.ToArray();
        for (var i = 0; i < corpsIds.Length; i++)
        {
            var location = i == 0 ? "c4307" : "c4308";
            var action = CampaignSourceAuthorityState.AxisCorpsPlacementActionId(corpsIds[i], location);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, action, CampaignActionAudience.Commonwealth)).RejectionReason);
            if (i == 1)
            {
                var occupied = CampaignSourceAuthorityState.AxisCorpsPlacementActionId(corpsIds[i], "c4307");
                Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                    CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, occupied, CampaignActionAudience.Axis)).RejectionReason);
            }
            directV12 = CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, action, CampaignActionAudience.Axis)).SuccessorSession!;
            directV12 = CampaignExercises.PromoteSourceAxisCorpsWorld(directV12).SuccessorSession!;
            Assert.Equal(22, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        }
        Assert.Equal(19, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(2, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-corps-artillery-active"));
        foreach (var id in corpsIds)
            Assert.Equal(3, directV12.SourceAuthority.World.World.Elements.Single(value => value.ElementId == id).Components.Single(value => value.ComponentId.EndsWith(".light-aa", StringComparison.Ordinal)).CurrentToe);
        Assert.False(CampaignExercises.CanPromoteSourceAxisCorpsWorld(directV12));
        var corpsContent = directV12.SourceAuthority.World.ContentIdentity;
        var removeAxisPoint = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", -1);
        directV12 = CampaignExercises.AdjustSourceMotorization(directV12,
            Submission(directV12, removeAxisPoint, CampaignActionAudience.Axis)).SuccessorSession!;
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12,
            Submission(directV12, fuel, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(corpsContent, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(19, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.AdmitSourceMalettiHierarchy(directV12).SuccessorSession!;
        Assert.Equal(23, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var confirmHierarchy = CampaignSourceAuthorityState.MalettiHierarchyActionId("confirm", null);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMalettiHierarchy(directV12, Submission(directV12, confirmHierarchy, CampaignActionAudience.Commonwealth)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceMalettiHierarchy(directV12,
            Submission(directV12, confirmHierarchy, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwFirst = directV12;
        var frenchFirst = directV12;
        var certifiedDetach = CampaignSourceAuthorityState.MalettiHierarchyActionId("detach", targetAxis);
        directV12 = CampaignExercises.SubmitSourceMalettiHierarchy(directV12,
            Submission(directV12, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(targetAxis, directV12.SourceAuthority!.MalettiHierarchy!.DetachedId);
        var otherDetach = CampaignSourceAuthorityState.MalettiHierarchyActionId("detach", "3-1-libyan-motor-infantry");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMalettiHierarchy(directV12, Submission(directV12, otherDetach, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(directV12));
        directV12 = CampaignExercises.PromoteSourceCertifiedMalettiWorld(directV12).SuccessorSession!;
        Assert.Equal(24, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(20, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Contains(directV12.SourceAuthority.Battlefield!.Entries,
            value => value.ElementId == targetAxis && value.AuthorityStatus == "source-certified-maletti-active");
        Assert.False(CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(directV12));
        var axisLift = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, 5);
        directV12 = CampaignExercises.SubmitSourceAxisPriority(directV12,
            Submission(directV12, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(5, directV12.SourceAuthority!.AxisPriorityMotorization!.AssignedPoints(targetAxis));
        directV12 = CampaignExercises.AdmitSourceMatruhFormation(directV12).SuccessorSession!;
        Assert.Equal(25, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceMatruhFormationDraft(directV12, CampaignActionAudience.Axis));
        var matruhConfirm = CampaignSourceAuthorityState.MatruhFormationActionId("confirm", null);
        directV12 = CampaignExercises.SubmitSourceMatruhFormation(directV12,
            Submission(directV12, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(5, directV12.SourceAuthority!.MatruhFormation!.AttachedIds.Count);
        var matruhDetach = CampaignSourceAuthorityState.MatruhFormationActionId("detach", "1-essex");
        directV12 = CampaignExercises.SubmitSourceMatruhFormation(directV12,
            Submission(directV12, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(4, directV12.SourceAuthority!.MatruhFormation!.AttachedIds.Count);
        Assert.Contains(directV12.SourceAuthority.Battlefield!.Entries,
            entry => entry.ElementId == "1-essex" && entry.AuthorityStatus == "source-matruh-detached");
        var secondMatruhDetach = CampaignSourceAuthorityState.MatruhFormationActionId("detach", "1-durham-light-infantry");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMatruhFormation(directV12, Submission(directV12, secondMatruhDetach, CampaignActionAudience.Commonwealth)).RejectionReason);
        directV12 = CampaignExercises.PromoteSourceMatruhDetachedWorld(directV12).SuccessorSession!;
        Assert.Equal(26, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(21, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(4, directV12.SourceAuthority.MatruhFormation!.AttachedIds.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        cwFirst = CampaignExercises.AdmitSourceMatruhFormation(cwFirst).SuccessorSession!;
        cwFirst = CampaignExercises.SubmitSourceMatruhFormation(cwFirst,
            Submission(cwFirst, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        cwFirst = CampaignExercises.SubmitSourceMatruhFormation(cwFirst,
            Submission(cwFirst, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        cwFirst = CampaignExercises.PromoteSourceMatruhDetachedWorld(cwFirst).SuccessorSession!;
        var cwFirstIdentity = cwFirst.SourceAuthority!.World.ContentIdentity;
        cwFirst = CampaignExercises.SubmitSourceMalettiHierarchy(cwFirst,
            Submission(cwFirst, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        cwFirst = CampaignExercises.PromoteSourceCertifiedMalettiWorld(cwFirst).SuccessorSession!;
        Assert.NotEqual(cwFirstIdentity, cwFirst.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(21, cwFirst.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, cwFirst.SourceAuthority.World.PendingArrivals.Count);
        Assert.True(CampaignExercises.Reconstruct(cwFirst).IsVerified);
        var rngBeforeGt2 = directV12.SourceAuthority!.RandomState;
        var worldBeforeGt2 = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.AdmitSourceGt2Correction(directV12).SuccessorSession!;
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var finishGt2 = CampaignSourceAuthorityState.Gt2CorrectionActionId("finish", 0, 0, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Commonwealth)).RejectionReason);
        if (maximum > 0)
        {
            foreach (var lane in new[] { 2, 3, 6 })
                for (var stage = 1; stage <= 3; stage++)
                    while (directV12.SourceAuthority!.Gt2Correction!.Quantity(lane, stage) > 0)
                    {
                        var removeUnit = CampaignSourceAuthorityState.Gt2CorrectionActionId("adjust", lane, stage, -1);
                        directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12,
                            Submission(directV12, removeUnit, CampaignActionAudience.Axis)).SuccessorSession!;
                    }
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Axis)).RejectionReason);
            var addGt2 = CampaignSourceAuthorityState.Gt2CorrectionActionId("adjust", 2, 1, 1);
            directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, addGt2, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(directV12.SourceAuthority!.Gt2Correction!.IsFinished);
        Assert.Equal(rngBeforeGt2, directV12.SourceAuthority.RandomState);
        Assert.Equal(worldBeforeGt2, directV12.SourceAuthority.World.ContentIdentity);
        directV12 = CampaignExercises.AdmitSourceCwPriorityCatalog(directV12).SuccessorSession!;
        directV12 = CampaignExercises.PromoteSourceCwPriorityWorld(directV12).SuccessorSession!;
        Assert.Equal(29, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(22, directV12.SourceAuthority!.World.World.Elements.Count);
        var frenchAdd = CampaignSourceAuthorityState.CwPriorityCatalogActionId("1-french-motor-marines", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceCwPriorityCatalog(directV12, Submission(directV12, frenchAdd, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchAdd, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, directV12.SourceAuthority!.CwPriorityCatalog!.FrenchPoints);
        Assert.Equal(1, directV12.SourceAuthority.CwPriorityCatalog.Remaining(directV12.SourceAuthority, "cw-c3926-attached"));
        var frenchRemove = CampaignSourceAuthorityState.CwPriorityCatalogActionId("1-french-motor-marines", -1);
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchRemove, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(2, directV12.SourceAuthority!.CwPriorityCatalog!.Remaining(directV12.SourceAuthority, "cw-c3926-attached"));
        frenchFirst = CampaignExercises.AdmitSourceCwPriorityCatalog(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.PromoteSourceCwPriorityWorld(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.AdmitSourceCwFreeGuns(frenchFirst).SuccessorSession!;
        var freeGunIds = frenchFirst.SourceAuthority!.CwFreeGuns!.PendingIds.ToArray();
        foreach (var id in freeGunIds)
        {
            var placeGun = CampaignSourceAuthorityState.CwFreeGunPlacementActionId(id, chosenCairoHex);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCwFreeGun(frenchFirst, Submission(frenchFirst, placeGun, CampaignActionAudience.Axis)).RejectionReason);
            frenchFirst = CampaignExercises.PlaceSourceCwFreeGun(frenchFirst,
                Submission(frenchFirst, placeGun, CampaignActionAudience.Commonwealth)).SuccessorSession!;
            frenchFirst = CampaignExercises.PromoteSourceCwFreeGunsWorld(frenchFirst).SuccessorSession!;
        }
        _ = ContentSourceFourthIndianPackArtifact.Create(frenchFirst.SourceAuthority!.CwFreeGunsWorld!.Content);
        var indianResult = CampaignExercises.AdmitSourceFourthIndianWorld(frenchFirst);
        Assert.True(indianResult.IsAccepted);
        var indian = indianResult.SuccessorSession!;
        var grouped = indian.SourceAuthority!.GroupedWorld!;
        Assert.Equal(ContentSourceFourthIndianPackArtifact.ContentHash, grouped.ContentHash);
        Assert.Equal(11, grouped.Groups.Single().Members.Count);
        var originalGroup = grouped.Groups.Single();
        var forgedMembers = originalGroup.Members.Select(value => value.ElementId == "25-field-artillery"
            ? value with { AdditionalComponents = Array.AsReadOnly(new[] { new CampaignGroupedComponent("extra-gun", 1) }) } : value);
        var forgedGroup = new CampaignGroupedFormation(originalGroup.GroupId, originalGroup.SideId, originalGroup.RootId,
            originalGroup.LocationId, originalGroup.StackValue, originalGroup.MotorPool, forgedMembers, originalGroup.DispositionConfirmed);
        var forgedWorld = CampaignGroupedWorld.Create(grouped.Prior, grouped.ContentHash, [forgedGroup]);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceFourthIndianDefinition.Validate(forgedWorld));
        var fieldArtillery = grouped.Groups.Single().Members.Single(value => value.ElementId == "25-field-artillery");
        Assert.Equal("3-7-inch-howitzer", fieldArtillery.ComponentType);
        Assert.Equal(6, fieldArtillery.ComponentToe);
        Assert.Equal(grouped.Prior.Initialization.CapabilityPointsExpended, fieldArtillery.CapabilityPointsExpended);
        Assert.Equal(11, indian.SourceAuthority.Battlefield!.Entries.Count(value => value.LocationId == "d3615" && value.AuthorityStatus == "source-fourth-indian-proposed"));
        Assert.Null(grouped.StackAt("commonwealth", "d3615", _ => null));
        Assert.Contains(grouped.Prior.PendingArrivals, value => value.GroupId == "graziani-7-indian-brigade");
        var priorityAction = CampaignSourceAuthorityState.FourthIndianMotorActionId("central-india-horse", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Axis)).RejectionReason);
        Assert.False(grouped.Groups.Single().DispositionConfirmed);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        var confirm = CampaignExercises.Query(indian, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "fourth-indian-motorization");
        Assert.Contains("confirm", confirm.ChoiceJson, StringComparison.Ordinal);
        var ownerDraft = System.Text.Encoding.UTF8.GetString(CampaignExercises.QuerySourceFourthIndianDraft(indian, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.Contains("provisional-source-default-alternatives-unavailable", ownerDraft, StringComparison.Ordinal);
        Assert.Contains("5-indian-brigade-hq", ownerDraft, StringComparison.Ordinal);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(indian,
            Submission(indian, confirm.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var confirmCommand = Submission(indian, confirm.ActionId, CampaignActionAudience.Commonwealth);
        indian = CampaignExercises.Submit(indian, confirmCommand).SuccessorSession!;
        Assert.True(indian.SourceAuthority!.GroupedWorld!.Groups.Single().DispositionConfirmed);
        Assert.Equal(5, indian.SourceAuthority.GroupedWorld.StackAt("commonwealth", "d3615", _ => null));
        Assert.Equal(11, indian.SourceAuthority.Battlefield!.Entries.Count(value => value.LocationId == "d3615" && value.AuthorityStatus == "source-fourth-indian-grouped"));
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(indian, confirmCommand).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        indian = CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(21, indian.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("4-indian-division"));
        var footAction = CampaignSourceAuthorityState.FourthIndianMotorActionId("1-royal-fusiliers", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, footAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        var indianWithoutMalettiDetachment = indian;
        indian = CampaignExercises.SubmitSourceMalettiHierarchy(indian,
            Submission(indian, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        indian = CampaignExercises.PromoteSourceCertifiedMalettiWorld(indian).SuccessorSession!;
        Assert.Equal(21, indian.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("4-indian-division"));
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        var matruhBeforeDetach = CampaignExercises.AdmitSourceMatruhFormation(indian).SuccessorSession!;
        matruhBeforeDetach = CampaignExercises.SubmitSourceMatruhFormation(matruhBeforeDetach,
            Submission(matruhBeforeDetach, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var matruhAfterDetach = matruhBeforeDetach;
        var groupedResult = CampaignExercises.AdmitSourceMatruhGroupedWorld(matruhBeforeDetach);
        Assert.True(groupedResult.IsAccepted);
        matruhBeforeDetach = groupedResult.SuccessorSession!;
        Assert.Equal(2, matruhBeforeDetach.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(0, matruhBeforeDetach.SourceAuthority.GroupedWorld.StackAt("commonwealth", "d3714", _ => null));
        Assert.Equal(6, matruhBeforeDetach.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "matruh-garrison").Members.Count);
        var activeMaletti = CampaignExercises.SubmitSourceAxisPriority(matruhBeforeDetach,
            Submission(matruhBeforeDetach, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        activeMaletti = CampaignExercises.AdmitSourceMalettiGroupedWorld(activeMaletti).SuccessorSession!;
        Assert.Equal(3, activeMaletti.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(30, activeMaletti.SourceAuthority.GroupedWorld.RemainingMotorPoints("maletti-division"));
        Assert.Equal(5, activeMaletti.SourceAuthority.GroupedWorld.ExternalReservations.Single(value => value.GroupId == "maletti-division").AssignedMotorPoints);
        Assert.True(CampaignExercises.Reconstruct(activeMaletti).IsVerified);
        var libyanAdmission = CampaignExercises.AdmitSourceLibyanGroupedWorld(activeMaletti);
        Assert.True(libyanAdmission.IsAccepted);
        var libyanProposed = libyanAdmission.SuccessorSession!;
        Assert.Equal(5, libyanProposed.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(22, libyanProposed.SourceAuthority.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-libyan-proposed"));
        var secondArtillery = libyanProposed.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "2-libyan-division")
            .Members.Single(value => value.ElementId == "2-libyan-artillery");
        Assert.Equal("75-27-gun", secondArtillery.ComponentType);
        Assert.Equal(9, secondArtillery.ComponentToe);
        Assert.Contains(secondArtillery.AdditionalComponents!, value => value.ComponentType == "100-17-howitzer" && value.Toe == 3);
        var axisChoices = CampaignExercises.Query(libyanProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "libyan-disposition").ToArray();
        Assert.Equal(2, axisChoices.Length);
        Assert.DoesNotContain(CampaignExercises.Query(libyanProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "libyan-disposition");
        var firstChoice = axisChoices.Single(value => value.ChoiceJson.Contains("1-libyan-division", StringComparison.Ordinal));
        var secondChoice = axisChoices.Single(value => value.ChoiceJson.Contains("2-libyan-division", StringComparison.Ordinal));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(libyanProposed,
            Submission(libyanProposed, firstChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var firstCommand = Submission(libyanProposed, firstChoice.ActionId, CampaignActionAudience.Axis);
        var firstConfirmed = CampaignExercises.Submit(libyanProposed, firstCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(firstConfirmed, firstCommand).RejectionReason);
        var secondAfter = CampaignExercises.Query(firstConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "libyan-disposition");
        firstConfirmed = CampaignExercises.Submit(firstConfirmed, Submission(firstConfirmed, secondAfter.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(22, firstConfirmed.SourceAuthority!.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-libyan-grouped"));
        Assert.Equal(5, firstConfirmed.SourceAuthority.GroupedWorld!.StackAt("axis", "c4020", _ => null));
        Assert.Equal(5, firstConfirmed.SourceAuthority.GroupedWorld.StackAt("axis", "c3920", _ => null));
        Assert.True(CampaignExercises.Reconstruct(firstConfirmed).IsVerified);
        var italianBasisFourth = ContentSourceFourthIndianPackArtifact.Create(firstConfirmed.SourceAuthority.CwFreeGunsWorld!.Content);
        var italianBasisMatruh = ContentSourceMatruhGroupedPackArtifact.Create(italianBasisFourth);
        var italianBasisMaletti = ContentSourceMalettiGroupedPackArtifact.Create(italianBasisMatruh);
        var italianBasisLibyan = ContentSourceLibyanGroupedPackArtifact.Create(italianBasisMaletti);
        var italianContent = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(italianBasisLibyan);
        Assert.Equal(ContentSourceCireneMarmaricaGroupedPackArtifact.ContentHash, italianContent.Hash());
        var italianAdmission = CampaignExercises.AdmitSourceItalianDivisionsGroupedWorld(firstConfirmed);
        Assert.True(italianAdmission.IsAccepted);
        var italianProposed = italianAdmission.SuccessorSession!;
        Assert.Equal(7, italianProposed.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(29, italianProposed.SourceAuthority.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-italian-division-proposed"));
        var italianChoices = CampaignExercises.Query(italianProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "italian-division-disposition").ToArray();
        Assert.Equal(2, italianChoices.Length);
        Assert.DoesNotContain(CampaignExercises.Query(italianProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "italian-division-disposition");
        var cireneChoice = italianChoices.Single(value => value.ChoiceJson.Contains("cirene-division", StringComparison.Ordinal));
        var italianCommand = Submission(italianProposed, cireneChoice.ActionId, CampaignActionAudience.Axis);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(italianProposed,
            Submission(italianProposed, cireneChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var italianConfirmed = CampaignExercises.Submit(italianProposed, italianCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(italianConfirmed, italianCommand).RejectionReason);
        var marmaricaChoice = CampaignExercises.Query(italianConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "italian-division-disposition");
        italianConfirmed = CampaignExercises.Submit(italianConfirmed, Submission(italianConfirmed, marmaricaChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(29, italianConfirmed.SourceAuthority!.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-italian-division-grouped"));
        Assert.Equal(5, italianConfirmed.SourceAuthority.GroupedWorld!.StackAt("axis", "c4120", _ => null));
        Assert.Equal(5, italianConfirmed.SourceAuthority.GroupedWorld.StackAt("axis", "c3918", _ => null));
        Assert.True(CampaignExercises.Reconstruct(italianConfirmed).IsVerified);
        if (seed == 0)
        {
            // Stress the unified legal-action contract after real grouped admissions.
            // Each advertised family is submitted against the same retained state,
            // independently of the other choices, then checked for replay and loss.
            foreach (var audience in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
            {
                var actions = CampaignExercises.Query(italianConfirmed, audience).ActionSet!.Candidates
                    .OfType<CampaignSourceSetupAction>().GroupBy(value => value.Family, StringComparer.Ordinal)
                    .SelectMany(group => group.Take(1).Concat(group.TakeLast(1)).DistinctBy(value => value.ActionId));
                foreach (var action in actions)
                {
                    var result = CampaignExercises.Submit(italianConfirmed, Submission(italianConfirmed, action.ActionId, audience));
                    Assert.True(result.IsAccepted, $"Advertised {audience} action {action.Family} was rejected: {result.RejectionReason}");
                    var successor = result.SuccessorSession!;
                    Assert.Equal(italianConfirmed.SourceAuthority.World.PendingArrivals, successor.SourceAuthority!.World.PendingArrivals);
                    Assert.Equal(italianConfirmed.SourceAuthority.GroupedWorld.Groups.Count, successor.SourceAuthority.GroupedWorld!.Groups.Count);
                    Assert.True(CampaignExercises.Reconstruct(successor).IsVerified, $"Could not replay {audience} action {action.Family}");
                }
            }
            // Exercise a deterministic interleaving from the same historical state.
            // The second branch executes the same user choices independently.
            var random = new Random(3451979);
            var left = italianConfirmed;
            var right = italianConfirmed;
            for (var i = 0; i < 16; i++)
            {
                var audience = i % 2 == 0 ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
                var choices = CampaignExercises.Query(left, audience).ActionSet!.Candidates
                    .OfType<CampaignSourceSetupAction>().ToArray();
                Assert.NotEmpty(choices);
                var action = choices[random.Next(choices.Length)];
                var leftStep = CampaignExercises.Submit(left, Submission(left, action.ActionId, audience));
                var rightStep = CampaignExercises.Submit(right, Submission(right, action.ActionId, audience));
                Assert.True(leftStep.IsAccepted, $"Interleaved {audience} action {action.Family} failed at step {i}");
                Assert.True(rightStep.IsAccepted, $"Repeated {audience} action {action.Family} failed at step {i}");
                Assert.Equal(leftStep.Evidence!.SnapshotCheckpoint, rightStep.Evidence!.SnapshotCheckpoint);
                left = leftStep.SuccessorSession!;
                right = rightStep.SuccessorSession!;
                Assert.Equal(left.SourceAuthority!.RandomState, right.SourceAuthority!.RandomState);
                Assert.Equal(italianConfirmed.SourceAuthority.World.PendingArrivals, left.SourceAuthority.World.PendingArrivals);
            }
            Assert.True(CampaignExercises.Reconstruct(left).IsVerified);
            Assert.True(CampaignExercises.Reconstruct(right).IsVerified);
            if (Environment.GetEnvironmentVariable("SANDTABLE_GRAZIANI_SOAK") == "1")
                RunSourceSoak(italianConfirmed);
        }
        var secondConfirmed = CampaignExercises.Submit(libyanProposed, Submission(libyanProposed, secondChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var firstAfter = CampaignExercises.Query(secondConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "libyan-disposition");
        secondConfirmed = CampaignExercises.Submit(secondConfirmed, Submission(secondConfirmed, firstAfter.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(secondConfirmed).IsVerified);

        var noMaletti = CampaignExercises.AdmitSourceMatruhFormation(indianWithoutMalettiDetachment).SuccessorSession!;
        noMaletti = CampaignExercises.SubmitSourceMatruhFormation(noMaletti,
            Submission(noMaletti, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        noMaletti = CampaignExercises.AdmitSourceMatruhGroupedWorld(noMaletti).SuccessorSession!;
        var afterDetachBeforeGroup = CampaignExercises.SubmitSourceMalettiHierarchy(noMaletti,
            Submission(noMaletti, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        afterDetachBeforeGroup = CampaignExercises.SubmitSourceAxisPriority(afterDetachBeforeGroup,
            Submission(afterDetachBeforeGroup, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        afterDetachBeforeGroup = CampaignExercises.AdmitSourceMalettiGroupedWorld(afterDetachBeforeGroup).SuccessorSession!;
        Assert.Equal(5, afterDetachBeforeGroup.SourceAuthority!.GroupedWorld!.Detached.Single(value => value.GroupId == "maletti-division").Member.AssignedMotorPoints);
        afterDetachBeforeGroup = CampaignExercises.PromoteSourceCertifiedMalettiWorld(afterDetachBeforeGroup).SuccessorSession!;
        Assert.Equal(5, afterDetachBeforeGroup.SourceAuthority!.GroupedWorld!.ExternalReservations.Single(value => value.GroupId == "maletti-division").AssignedMotorPoints);
        Assert.True(CampaignExercises.Reconstruct(afterDetachBeforeGroup).IsVerified);

        var beforeDetach = CampaignExercises.AdmitSourceMalettiGroupedWorld(noMaletti).SuccessorSession!;
        beforeDetach = CampaignExercises.SubmitSourceMalettiHierarchy(beforeDetach,
            Submission(beforeDetach, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        beforeDetach = CampaignExercises.SubmitSourceAxisPriority(beforeDetach,
            Submission(beforeDetach, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(5, beforeDetach.SourceAuthority!.GroupedWorld!.Detached.Single(value => value.GroupId == "maletti-division").Member.AssignedMotorPoints);
        beforeDetach = CampaignExercises.PromoteSourceCertifiedMalettiWorld(beforeDetach).SuccessorSession!;
        Assert.Equal(5, beforeDetach.SourceAuthority!.GroupedWorld!.ExternalReservations.Single(value => value.GroupId == "maletti-division").AssignedMotorPoints);
        var axisRemove = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, -5);
        beforeDetach = CampaignExercises.SubmitSourceAxisPriority(beforeDetach,
            Submission(beforeDetach, axisRemove, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(35, beforeDetach.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("maletti-division"));
        Assert.True(CampaignExercises.Reconstruct(beforeDetach).IsVerified);
        matruhBeforeDetach = CampaignExercises.SubmitSourceMatruhFormation(matruhBeforeDetach,
            Submission(matruhBeforeDetach, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Single(matruhBeforeDetach.SourceAuthority!.GroupedWorld!.Detached);
        Assert.Equal(2, matruhBeforeDetach.SourceAuthority.GroupedWorld.OccupantsAt("d3714").Count);
        Assert.True(CampaignExercises.Reconstruct(matruhBeforeDetach).IsVerified);
        matruhBeforeDetach = CampaignExercises.PromoteSourceMatruhDetachedWorld(matruhBeforeDetach).SuccessorSession!;
        Assert.Empty(matruhBeforeDetach.SourceAuthority!.GroupedWorld!.Detached);
        Assert.Equal(2, matruhBeforeDetach.SourceAuthority.GroupedWorld.OccupantsAt("d3714").Count);
        Assert.True(CampaignExercises.Reconstruct(matruhBeforeDetach).IsVerified);
        matruhAfterDetach = CampaignExercises.SubmitSourceMatruhFormation(matruhAfterDetach,
            Submission(matruhAfterDetach, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var matruhBetween = CampaignExercises.AdmitSourceMatruhGroupedWorld(matruhAfterDetach).SuccessorSession!;
        Assert.Single(matruhBetween.SourceAuthority!.GroupedWorld!.Detached);
        matruhBetween = CampaignExercises.PromoteSourceMatruhDetachedWorld(matruhBetween).SuccessorSession!;
        Assert.Empty(matruhBetween.SourceAuthority!.GroupedWorld!.Detached);
        Assert.Equal(2, matruhBetween.SourceAuthority.GroupedWorld.OccupantsAt("d3714").Count);
        Assert.True(CampaignExercises.Reconstruct(matruhBetween).IsVerified);
        matruhAfterDetach = CampaignExercises.PromoteSourceMatruhDetachedWorld(matruhAfterDetach).SuccessorSession!;
        matruhAfterDetach = CampaignExercises.AdmitSourceMatruhGroupedWorld(matruhAfterDetach).SuccessorSession!;
        Assert.Empty(matruhAfterDetach.SourceAuthority!.GroupedWorld!.Detached);
        Assert.Equal(2, matruhAfterDetach.SourceAuthority.GroupedWorld.OccupantsAt("d3714").Count);
        Assert.Equal(5, matruhAfterDetach.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "matruh-garrison").Members.Count);
        Assert.True(CampaignExercises.Reconstruct(matruhAfterDetach).IsVerified);
        var normalizedIdentity = frenchFirst.SourceAuthority!.World.ContentIdentity;
        frenchFirst = CampaignExercises.SubmitSourceMalettiHierarchy(frenchFirst,
            Submission(frenchFirst, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        frenchFirst = CampaignExercises.PromoteSourceCertifiedMalettiWorld(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.AdmitSourceMatruhFormation(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.SubmitSourceMatruhFormation(frenchFirst,
            Submission(frenchFirst, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        frenchFirst = CampaignExercises.SubmitSourceMatruhFormation(frenchFirst,
            Submission(frenchFirst, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        frenchFirst = CampaignExercises.PromoteSourceMatruhDetachedWorld(frenchFirst).SuccessorSession!;
        Assert.Equal(normalizedIdentity, frenchFirst.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(25, frenchFirst.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, frenchFirst.SourceAuthority.World.PendingArrivals.Count);
        Assert.True(CampaignExercises.Reconstruct(frenchFirst).IsVerified);
        var oldCheckpointLength = directV12.SourceAuthority!.SerializeCheckpoint().Length;
        directV12 = CampaignExercises.AdmitSourceCompactCheckpoint(directV12).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.True(directV12.SourceAuthority!.SerializeCheckpoint().Length < oldCheckpointLength / 100);
        var contentBeforeCompactAction = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchAdd, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(contentBeforeCompactAction, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.True(CampaignExercises.Reconstruct(directV12).IsVerified);
        var beginCargo = CampaignSourceAuthorityState.UnattachedCargoActionId(true, "begin", null, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceUnattachedCargo(directV12, Submission(directV12, beginCargo, CampaignActionAudience.Axis)).RejectionReason);
        foreach (var amount in new[] { 50, 50, 25 })
        {
            var allocate = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", amount);
            directV12 = CampaignExercises.AdjustSourceMotorization(directV12, Submission(directV12, allocate, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        directV12 = CampaignExercises.SubmitSourceUnattachedCargo(directV12, Submission(directV12, beginCargo, CampaignActionAudience.Axis)).SuccessorSession!;
        var addCargo = CampaignSourceAuthorityState.UnattachedCargoActionId(true, "adjust", "box.tripoli", 1);
        for (var i = 0; i < 6; i++)
            directV12 = CampaignExercises.SubmitSourceUnattachedCargo(directV12, Submission(directV12, addCargo, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(6, directV12.SourceAuthority!.AxisUnattachedCargo!.Quantity("box.tripoli"));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceUnattachedCargo(directV12, Submission(directV12, addCargo, CampaignActionAudience.Axis)).RejectionReason);
        var forbiddenMove = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", -1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourceMotorization(directV12, Submission(directV12, forbiddenMove, CampaignActionAudience.Axis)).RejectionReason);
        var finishCargo = CampaignSourceAuthorityState.UnattachedCargoActionId(true, "finish", null, 0);
        directV12 = CampaignExercises.SubmitSourceUnattachedCargo(directV12, Submission(directV12, finishCargo, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(directV12.SourceAuthority!.AxisUnattachedCargo!.IsFinished);
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(directV12).IsVerified);
        var lateAxis = CampaignExercises.QuerySourceActionsDraft(directV12, CampaignActionAudience.Axis);
        var lateCw = CampaignExercises.QuerySourceActionsDraft(directV12, CampaignActionAudience.Commonwealth);
        var lateAxisText = System.Text.Encoding.UTF8.GetString(lateAxis.VisibleBytes);
        var lateCwText = System.Text.Encoding.UTF8.GetString(lateCw.VisibleBytes);
        Assert.Contains("axis-gt2-correction", lateAxisText);
        Assert.DoesNotContain("cw-priority-catalog", lateAxisText);
        Assert.Contains("cw-priority-catalog", lateCwText);
        Assert.DoesNotContain("axis-gt2-correction", lateCwText);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(directV12, Submission(directV12, undoDetachment, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(directV12).IsVerified);
        Assert.False(CampaignExercises.CanPromoteSourceMaletti(cwSession));
        cwSession = CampaignExercises.PromoteSourceIndependent(cwSession).SuccessorSession!;
        Assert.Equal(10, cwSession.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, cwSession.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(genericIdentity, cwSession.SourceAuthority.World.ContentIdentity);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(3, cwSession.SourceAuthority!.AxisSupplyWorld.Units.Count(x => x.IsDummy));
        Assert.Equal(3, cwSession.SourceAuthority.CommonwealthSupplyWorld.Units.Count(x => x.IsDummy));
        Assert.Null(cwSession.SourceAuthority.Position);
        var motor = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", 50);
        Assert.Contains(motor, CampaignExercises.QuerySourceMotorizationDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourceMotorization(cwSession, Submission(cwSession, motor, CampaignActionAudience.Commonwealth)).RejectionReason);
        cwSession = CampaignExercises.AdjustSourceMotorization(cwSession, Submission(cwSession, motor, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(75, cwSession.SourceAuthority!.AxisMotorization.Remaining("axis-libya-unattached"));
        var remove = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", -10);
        cwSession = CampaignExercises.AdjustSourceMotorization(cwSession, Submission(cwSession, remove, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(85, cwSession.SourceAuthority!.AxisMotorization.Remaining("axis-libya-unattached"));
        var cwMotor = CampaignSourceAuthorityState.MotorizationActionId(false, "cw-cairo-unattached", "e1829", 25);
        cwSession = CampaignExercises.AdjustSourceMotorization(cwSession, Submission(cwSession, cwMotor, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(25, cwSession.SourceAuthority!.CommonwealthMotorization.Remaining("cw-cairo-unattached"));
        var tooMuch = CampaignSourceAuthorityState.MotorizationActionId(false, "cw-cairo-unattached", "e1829", 50);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourceMotorization(cwSession, Submission(cwSession, tooMuch, CampaignActionAudience.Commonwealth)).RejectionReason);
        Assert.Equal(cwSession.SourceAuthority!.IndependentWorld!.Content.Identity, cwSession.SourceAuthority.World.ContentIdentity);
        Assert.Equal(10, cwSession.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, cwSession.SourceAuthority.World.PendingArrivals.Count);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
    }

    private static void RunSourceSoak(ExerciseSession initial)
    {
        foreach (var seed in new[] { 104, 345, 1979 })
        {
            var random = new Random(seed);
            var left = initial;
            var right = initial;
            var step = 0;
            try
            {
                for (; step < 32; step++)
                {
                    var audience = step % 2 == 0 ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
                    var options = CampaignExercises.Query(left, audience).ActionSet!.Candidates.OfType<CampaignSourceSetupAction>().ToArray();
                    Assert.True(options.Length > 0, $"No action at seed {seed}, step {step}");
                    var choice = options[random.Next(options.Length)];
                    var before = left.SourceAuthority!.SerializeCheckpoint();
                    var forged = Submission(left, "sha256:" + new string('0', 64), audience);
                    Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(left, forged).RejectionReason);
                    Assert.Equal(before, left.SourceAuthority.SerializeCheckpoint());
                    var command = Submission(left, choice.ActionId, audience);
                    var stale = command with { ExpectedStateVersion = command.ExpectedStateVersion - 1 };
                    Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(left, stale).RejectionReason);
                    Assert.Equal(before, left.SourceAuthority.SerializeCheckpoint());
                    var one = CampaignExercises.Submit(left, command);
                    var two = CampaignExercises.Submit(right, Submission(right, choice.ActionId, audience));
                    Assert.True(one.IsAccepted && two.IsAccepted, $"Rejected {choice.Family} at seed {seed}, step {step}");
                    Assert.Equal(one.Evidence!.SnapshotCheckpoint, two.Evidence!.SnapshotCheckpoint);
                    left = one.SuccessorSession!;
                    right = two.SuccessorSession!;
                    Assert.Equal(left.SourceAuthority!.RandomState, right.SourceAuthority!.RandomState);
                    Assert.Equal(initial.SourceAuthority!.World.PendingArrivals, left.SourceAuthority.World.PendingArrivals);
                    Assert.Equal(initial.SourceAuthority.GroupedWorld!.Groups.Count, left.SourceAuthority.GroupedWorld!.Groups.Count);
                }
                Assert.True(CampaignExercises.Reconstruct(left).IsVerified, $"Replay failed at seed {seed}");
                Assert.True(CampaignExercises.Reconstruct(right).IsVerified, $"Repeated replay failed at seed {seed}");
            }
            catch (Exception error)
            {
                throw new InvalidOperationException($"Graziani soak failed at seed {seed}, step {step}.", error);
            }
        }
    }

    private static CampaignActionSubmission Submission(ExerciseSession session, string actionId, CampaignActionAudience audience)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(CampaignActionSubmission.CurrentContractVersion, checkpoint.CampaignId,
            checkpoint.StateVersion, checkpoint.PositionId, audience, actionId);
    }
}
