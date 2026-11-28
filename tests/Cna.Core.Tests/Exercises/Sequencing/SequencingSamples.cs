using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Visibility;
using Xunit;

namespace Cna.Core.Tests.Exercises.Sequencing;

// S0 of the data-driven sequence executor (docs/work/plans/2026-11-24-pending-from-cursor-spike.md
// section 5.1): real-session sample drivers. Test-only; no production type is touched. Each
// scenario drives the REAL Core dispatch and records, after every accepted step, what the old
// resolver (ExerciseSession.Pending) says and where the checkpoint cursor is. The S2 oracle and
// the S1 shadow derivation consume these samples; nothing here derives anything.

// How the last accepted step relates to the cursor. A Move is the only history event that leaves
// the checkpoint at the same catalog position (see PendingFromCursorSpikeTests).
internal enum SampleLastEvent { CompletedPosition, MoveWithinPosition }

internal sealed record SequencingSample(
    string Scenario,
    string Label,
    ExerciseSession Session,
    string? PendingName,
    string PositionId,
    SampleLastEvent LastEvent,
    string LastHistoryType,
    int GameTurn,
    IReadOnlyList<string> NonNullCurrentFields);

internal sealed class SequencingRecorder(string scenario)
{
    private readonly List<SequencingSample> samples = [];
    internal List<string> Notes { get; } = [];
    internal IReadOnlyList<SequencingSample> Samples => samples;

    internal ExerciseSession Rec(string label, ExerciseSession session)
    {
        string position;
        try { position = CampaignExercises.QueryCheckpoint(session).PositionId; }
        catch (Exception error) { position = "<throws " + error.GetType().Name + ">"; }
        var last = session.CurrentHistory.Count == 0 ? null : session.CurrentHistory[^1];
        var nonNull = typeof(ExerciseSession)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => p.Name.StartsWith("Current", StringComparison.Ordinal)
                && p.PropertyType.Name.StartsWith("CampaignNative", StringComparison.Ordinal)
                && p.GetValue(session) is not null)
            .Select(p => p.Name).Order(StringComparer.Ordinal).ToList();
        samples.Add(new SequencingSample(scenario, label, session, session.Pending?.GetType().Name, position,
            last is CampaignNativeSecondMoveV44 ? SampleLastEvent.MoveWithinPosition : SampleLastEvent.CompletedPosition,
            last?.GetType().Name ?? "<none>", CampaignGameTurn.CurrentGameTurn(session), nonNull));
        return session;
    }

    internal ExerciseSession Ok(string label, ExerciseStepResult result)
    {
        Assert.True(result.IsAccepted, $"{scenario}/{label}: {result.RejectionReason}");
        return Rec(label, result.SuccessorSession!);
    }
}

internal static class SequencingScenarios
{
    internal static string Hash(string label) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(label)));

    private static readonly Lazy<SequencingRecorder> PregameLazy = new(() => DrivePregameThroughStageThreeFirstSide());
    private static readonly Lazy<SequencingRecorder> BoundaryLazy = new(() => DriveTurnBoundary(1, 12345));
    private static readonly Lazy<SequencingRecorder> AttackLazy = new(() => DriveRealAttack("real-attack", repeat: false));
    private static readonly Lazy<SequencingRecorder> RepeatLazy = new(() => DriveRealAttack("real-attack-repeat", repeat: true));

    // name -> recorder. Add a scenario by adding one line here; the S2 oracle iterates this list.
    internal static IReadOnlyList<SequencingRecorder> All { get; } =
        [PregameLazy.Value, AttackLazy.Value, RepeatLazy.Value, BoundaryLazy.Value];

    // ---- scenario 1: no-attack chain, Stage 1 -> Stage 3 first side, real Moves where offered ----

    internal static SequencingRecorder DriveTurnBoundary(int initialGameTurn, ulong seed) =>
        DrivePregameThroughStageThreeFirstSide($"rules-lab.breakdown.turn-boundary.gt{initialGameTurn}.v1", seed, true);

    internal static SequencingRecorder DriveReactionFixture() =>
        DrivePregameThroughStageThreeFirstSide(Cna1979BreakdownSetupCatalog.ReactionSetupId, completionOnly: true);

    private static SequencingRecorder DrivePregameThroughStageThreeFirstSide(
        string setupId = Cna1979BreakdownSetupCatalog.TruckSetupId, ulong seed = 12345, bool rollover = false, bool completionOnly = false)
    {
        var r = new SequencingRecorder(rollover ? $"turn-boundary-{seed}-{setupId}" : "pregame-no-attack");
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions
            .Single(value => value.SetupId == setupId));
        var session = r.Rec("begin", CampaignExercises.Begin(new CampaignCreationRequest(1,
            "campaign-sequencing-samples", Cna1979Ruleset.Manifest.Hash, seed, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId)).Session!);
        session = AdvanceToCombatPositionDetermination(r, session, "op1.first.legacy");
        var order1 = session.CurrentSnapshot.OperationStageOrders.Single(v => v.OperationStage == 1);
        var firstAud = Audience(order1.FirstSide);
        var secondAud = Other(firstAud);

        session = FinishWithoutAttack(r, session, firstAud, "op1.first", CampaignExercises.CompleteNativeCombatNoAttack);
        session = CycleThroughPasses(r, session, firstAud, "op1.first");
        // Stage 1 second side
        session = r.Ok("op1.second.reserve", CampaignExercises.SubmitSecondReserve(session, secondAud, Hash("second-reserve"), elementId: null));
        if (!completionOnly) session = TryMove(r, session, secondAud, "op1.second", CampaignExercises.QuerySecondMovementCut, CampaignExercises.SubmitSecondMovement);
        session = r.Ok("op1.second.movement-completion", CampaignExercises.CompleteSecondMovement(session, secondAud, Hash("second-movement-completion")));
        session = r.Ok("op1.second.breakdown", CampaignExercises.CompleteSecondBreakdown(session));
        session = FinishWithoutAttack(r, session, secondAud, "op1.second", CampaignExercises.CompleteSecondCombatNoAttack);
        session = CycleThroughPasses(r, session, secondAud, "op1.second");

        // Stage 2
        var gameTurn = session.CurrentSnapshot.Setup.InitialGameTurn;
        var holder = session.CurrentSnapshot.InitiativeHolder!.Value;
        var holderAud = Audience(holder);
        var otherAud = Other(holderAud);
        session = r.Ok("op2.initiative-declaration", CampaignExercises.SubmitStageEntryInitiativeDeclaration(
            session, holderAud, Hash("decl"), actFirst: true));
        var weather = CampaignNativeWeatherDeterminationV50.Create(session.CurrentInitiativeDeclaration!,
            session.CurrentSnapshot.RandomState, gameTurn, session.CurrentContacts.AuthorityVersion);
        session = r.Rec("op2.weather", session.WithStageEntryWeather(weather, session.CurrentContacts.AdvanceWeatherDetermination(weather)));
        var org = CampaignNativeNoObligationOrganizationV51.Create(weather, weather.StateVersion);
        session = r.Rec("op2.organization", session.WithNoObligationOrganization(org, session.CurrentContacts.AdvanceNoObligationOrganization(org)));
        var convoy = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(org, session.Context, org.StateVersion);
        session = r.Rec("op2.naval-convoy-arrival", session.WithNoObligationNavalConvoyArrival(convoy, session.CurrentContacts.AdvanceNoObligationNavalConvoyArrival(convoy)));
        var assign = CampaignNativeNoObligationFleetAssignmentV53.Create(convoy, convoy.StateVersion);
        session = r.Rec("op2.fleet-assignment", session.WithNoObligationFleetAssignment(assign, session.CurrentContacts.AdvanceNoObligationFleetAssignment(assign)));
        var repair = CampaignNativeNoObligationFleetRepairV54.Create(assign, assign.StateVersion);
        session = r.Rec("op2.fleet-repair", session.WithNoObligationFleetRepair(repair, session.CurrentContacts.AdvanceNoObligationFleetRepair(repair)));

        session = StageEntrySide(r, session, holderAud, second: false, completionOnly);
        session = StageEntrySide(r, session, otherAud, second: true, completionOnly);

        // Stage 3 preamble + first side (S3-1..S3-3)
        var terminal = session.CurrentStageEntrySecondAssault!.PostCombatPasses[^1];
        var declared3 = CampaignNativeInitiativeDeclarationV49.Create(terminal, holder, actFirst: true, gameTurn, terminal.StateVersion);
        session = r.Rec("op3.initiative-declaration", session.WithThirdStageInitiativeDeclaration(declared3, session.CurrentContacts.AdvanceInitiativeDeclaration(declared3)));
        var initial = session.CurrentSnapshot.RandomState;
        var random = new RandomStreamState(initial.ContractVersion, initial.AlgorithmId, initial.Seed, weather.ResultRandomCursor);
        var weather3 = CampaignNativeWeatherDeterminationV50.Create(declared3, random, gameTurn, session.CurrentContacts.AuthorityVersion);
        session = r.Rec("op3.weather", session.WithThirdStageWeather(weather3, session.CurrentContacts.AdvanceWeatherDetermination(weather3)));
        var org3 = CampaignNativeNoObligationOrganizationV51.Create(weather3, weather3.StateVersion);
        session = r.Rec("op3.organization", session.WithThirdStageNoObligationOrganization(org3, session.CurrentContacts.AdvanceNoObligationOrganization(org3)));
        var arrival3 = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(org3, session.Context, org3.StateVersion);
        session = r.Rec("op3.naval-convoy-arrival", session.WithThirdStageNoObligationNavalConvoyArrival(arrival3, session.CurrentContacts.AdvanceNoObligationNavalConvoyArrival(arrival3)));
        var assign3 = CampaignNativeNoObligationFleetAssignmentV53.Create(arrival3, arrival3.StateVersion);
        session = r.Rec("op3.fleet-assignment", session.WithThirdStageNoObligationFleetAssignment(assign3, session.CurrentContacts.AdvanceNoObligationFleetAssignment(assign3)));
        var repair3 = CampaignNativeNoObligationFleetRepairV54.Create(assign3, assign3.StateVersion);
        session = r.Rec("op3.fleet-repair", session.WithThirdStageNoObligationFleetRepair(repair3, session.CurrentContacts.AdvanceNoObligationFleetRepair(repair3)));

        var firstSide3 = repair3.ToOrder().FirstSide;
        var aud3 = Audience(firstSide3);
        session = r.Ok("op3.first.reserve", CampaignExercises.SubmitThirdStageReserve(session, aud3, Hash("stage3-reserve"), elementId: null));
        if (!completionOnly) session = TryMove(r, session, aud3, "op3.first", CampaignExercises.QueryThirdStageMovementCut, CampaignExercises.SubmitThirdStageMovement);
        session = r.Ok("op3.first.movement-completion", CampaignExercises.CompleteThirdStageMovement(session, aud3, Hash("stage3-movement-completion")));
        session = r.Ok("op3.first.breakdown", CampaignExercises.CompleteThirdStageBreakdown(session));
        // S3-4/S3-5: first-side Combat Decision (finish without attack), No-Attack completion, Reserve Release,
        // Cycle Control Finish, Truck Convoy and the four Post Combat Passes.
        var element3 = aud3 == CampaignActionAudience.Axis ? "axis-infantry" : "commonwealth-infantry";
        _ = element3;
        session = FinishWithoutAttack(r, session, aud3, "op3.first", CampaignExercises.CompleteThirdStageCombatNoAttack);
        session = CycleThroughPasses(r, session, aud3, "op3.first");
        // S3-6..S3-8: second side Reserve, Movement + Completion, Breakdown (Combat is S3-9).
        var aud3Second = Other(aud3);
        session = r.Ok("op3.second.reserve", CampaignExercises.SubmitThirdStageSecondReserve(session, aud3Second, Hash("stage3-second-reserve"), elementId: null));
        if (!completionOnly) session = TryMove(r, session, aud3Second, "op3.second", CampaignExercises.QueryThirdStageSecondMovementCut, CampaignExercises.SubmitThirdStageSecondMovement);
        session = r.Ok("op3.second.movement-completion", CampaignExercises.CompleteThirdStageSecondMovement(session, aud3Second, Hash("stage3-second-movement-completion")));
        session = r.Ok("op3.second.breakdown", CampaignExercises.CompleteThirdStageSecondBreakdown(session));
        session = FinishWithoutAttack(r, session, aud3Second, "op3.second", CampaignExercises.CompleteThirdStageSecondCombatNoAttack);
        session = CycleThroughPasses(r, session, aud3Second, "op3.second");
        if (rollover)
        {
            session = r.Ok("turn.end", CampaignExercises.CompleteEndOfTurn(session));
            session = r.Ok("turn.initiative", CampaignExercises.CompleteTurnInitiative(session));
            var initiative = (CampaignNativeTurnInitiativeV56)session.CurrentHistory[^1];
            session = r.Ok("turn.schedule", CampaignExercises.CompleteTurnConvoySchedule(session));
            session = r.Ok("turn.shipping", CampaignExercises.CompleteTurnTacticalShipping(session));
            session = r.Ok("turn.declaration", CampaignExercises.SubmitStageOneDeclaration(session,
                Audience(initiative.Holder), Hash("turn-declaration"), actFirst: false));
            session = r.Ok("turn.weather", CampaignExercises.CompleteTurnStartWeather(session));
            session = r.Ok("turn.organization", CampaignExercises.CompleteTurnStartOrganization(session));
            session = r.Ok("turn.arrival", CampaignExercises.CompleteTurnStartConvoyArrival(session));
            session = r.Ok("turn.assignment", CampaignExercises.CompleteTurnStartFleetAssignment(session));
            r.Ok("turn.repair", CampaignExercises.CompleteTurnStartFleetRepair(session));
        }
        return r;
    }

    private static ExerciseSession StageEntrySide(SequencingRecorder r, ExerciseSession session,
        CampaignActionAudience audience, bool second, bool completionOnly)
    {
        var tag = second ? "op2.second" : "op2.first";
        session = r.Ok($"{tag}.reserve", second
            ? CampaignExercises.SubmitStageEntrySecondReserve(session, audience, Hash("stage2-reserve"), elementId: null)
            : CampaignExercises.SubmitStageEntryReserve(session, audience, Hash("stage2-reserve"), elementId: null));
        if (!completionOnly) session = second
            ? TryMove(r, session, audience, tag, CampaignExercises.QueryStageEntrySecondMovementCut, CampaignExercises.SubmitStageEntrySecondMovement)
            : TryMove(r, session, audience, tag, CampaignExercises.QueryStageEntryMovementCut, CampaignExercises.SubmitStageEntryMovement);
        session = r.Ok($"{tag}.movement-completion", second
            ? CampaignExercises.CompleteStageEntrySecondMovement(session, audience, Hash("stage2-movement-completion"))
            : CampaignExercises.CompleteStageEntryMovement(session, audience, Hash("stage2-movement-completion")));
        session = r.Ok($"{tag}.breakdown", second
            ? CampaignExercises.CompleteStageEntrySecondBreakdown(session)
            : CampaignExercises.CompleteStageEntryBreakdown(session));
        session = FinishWithoutAttack(r, session, audience, tag, second
            ? CampaignExercises.CompleteStageEntrySecondCombatNoAttack : CampaignExercises.CompleteStageEntryCombatNoAttack);
        return CycleThroughPasses(r, session, audience, tag);
    }

    // One real Move when any own element has a legal offer; otherwise a note (reported by S0 tests).
    private static ExerciseSession TryMove(SequencingRecorder r, ExerciseSession session, CampaignActionAudience audience,
        string tag,
        Func<ExerciseSession, LandSide, string, CampaignContactOwnerMovementCut> cut,
        Func<ExerciseSession, CampaignActionAudience, string, string, ExerciseStepResult> submit)
    {
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var offered = 0;
        foreach (var element in session.CurrentSnapshot.World.Elements.Select(v => v.ElementId).Order(StringComparer.Ordinal))
        {
            CampaignContactOwnerMovementCut projected;
            try { projected = cut(session, side, element); }
            catch (Exception error) when (error is InvalidOperationException or ArgumentException) { continue; }
            foreach (var offer in projected.MovementOffers
                .OrderBy(v => v.OriginLocationId, StringComparer.Ordinal)
                .ThenBy(v => v.DestinationLocationId, StringComparer.Ordinal))
            {
                offered++;
                var moved = submit(session, audience, CampaignNativeSecondMoveV44.ActionIdFor(offer), element);
                if (!moved.IsAccepted) continue;
                // This driver is the frozen no-Reaction history oracle. Select the same
                // non-triggering offer as before E4b, without adopting the immutable
                // triggering successor. NativeReactionPendingTests drive those offers.
                if (moved.SuccessorSession!.Pending is NativeReactionPending)
                {
                    r.Notes.Add($"{tag}: optional Reaction trigger skipped for frozen history");
                    continue;
                }
                r.Notes.Add($"{tag}: real move accepted ({element})");
                return r.Rec($"{tag}.move", moved.SuccessorSession!);
            }
        }
        r.Notes.Add($"{tag}: no accepted move ({offered} offers)");
        return session;
    }

    private static ExerciseSession FinishWithoutAttack(SequencingRecorder r, ExerciseSession session,
        CampaignActionAudience audience, string tag, Func<ExerciseSession, ExerciseStepResult> completeNoAttack)
    {
        var element = audience == CampaignActionAudience.Axis ? "axis-infantry" : "commonwealth-infantry";
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var submission = new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience,
            new FinishCombatWithoutAttackAction(element).ActionId);
        session = r.Ok($"{tag}.combat-selection", CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, element));
        var noAttack = completeNoAttack(session);
        if (tag.StartsWith("op3.", StringComparison.Ordinal))
            Assert.Equal(noAttack.Evidence!.EventRecords.Single(), noAttack.Evidence.SnapshotCheckpoint);
        session = r.Ok($"{tag}.combat-no-attack", noAttack);
        return r.Ok($"{tag}.reserve-release", CampaignExercises.CompleteNativeReserveRelease(session));
    }

    private static ExerciseSession CycleThroughPasses(SequencingRecorder r, ExerciseSession session,
        CampaignActionAudience audience, string tag)
    {
        session = r.Ok($"{tag}.cycle-control-finish", CampaignExercises.SubmitNativeCycleControl(session, audience,
            CampaignNativeCycleControlChoiceV28.Finish, Hash("cycle-control-finish")));
        session = r.Ok($"{tag}.truck-convoy-pass", CampaignExercises.SubmitTruckConvoyPass(session, audience, Hash("truck-convoy-pass")));
        for (var index = 0; index < 4; index++)
            session = r.Ok($"{tag}.post-combat-pass-{index}", CampaignExercises.SubmitPostCombatPass(session, audience, Hash($"post-combat-pass-{index}")));
        return session;
    }

    private static ExerciseSession AdvanceToCombatPositionDetermination(SequencingRecorder r, ExerciseSession session, string tag)
    {
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination)
                return session;
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(a => CampaignExercises.Query(session, a).ActionSet).First(v => v?.Candidates.Count > 0)!;
            var action = set.Candidates.FirstOrDefault(v => v is not MoveElementAction) ?? set.Candidates[0];
            session = r.Rec($"{tag}.step-{index}", CampaignExercises.Submit(session, new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, action.ActionId)).SuccessorSession!);
        }
        throw new InvalidOperationException("Combat position was not reached.");
    }

    // ---- scenarios 2/3: a real attack (Stage 1 first side, the only side the Combat harness drives) ----

    // Mirrors CampaignCombatExerciseAssaultTests.Drive: Select Contact Assault, Barrage, RBA decline,
    // Force seals, Anti Armor, result, retreat/settlement, Custody, Reserve Release, Cycle Control.
    // repeat=true takes Cycle Control's Repeat (a real repeat cycle) after releasing the reserve.
    private static SequencingRecorder DriveRealAttack(string scenario, bool repeat)
    {
        var r = new SequencingRecorder(scenario);
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(v =>
            v.SetupId == "rules-lab.breakdown.reaction.combat-double-fresh.v1"));
        var session = CampaignExercises.Begin(new CampaignCreationRequest(1, $"campaign-sequencing-{scenario}",
            Cna1979Ruleset.Manifest.Hash, 5, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId)).Session!;
        r.Rec("begin", session);
        var reserveDesignated = false;
        for (var index = 0; ; index++)
        {
            Assert.True(index < 80, "Combat position was not reached.");
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination) break;
            var set = Enum.GetValues<CampaignActionAudience>().Select(a => CampaignExercises.Query(session, a).ActionSet)
                .First(v => v?.Candidates.Count > 0)!;
            var reserve = !reserveDesignated ? set.Candidates.OfType<DesignateReserveAction>()
                .FirstOrDefault(v => v.ElementId == "axis-infantry-second") : null;
            var action = reserve ?? set.Candidates.FirstOrDefault(v => v is not MoveElementAction) ?? set.Candidates[0];
            if (reserve is not null) reserveDesignated = true;
            session = r.Rec($"pre-combat-{index}", CampaignExercises.Submit(session, new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, action.ActionId)).SuccessorSession!);
        }

        var assault = CampaignExercises.QueryWithCombat(session, CampaignActionAudience.Axis).ActionSet!
            .Candidates.OfType<SelectContactAssaultAction>().Single(v => v.ActingElementId == "axis-infantry");
        session = Step(r, session, CampaignActionAudience.Axis, assault.ActionId, assault.Kind);
        var repeated = false;
        for (var index = 0; index < 80; index++)
        {
            var sets = Enum.GetValues<CampaignActionAudience>()
                .Select(a => CampaignExercises.QueryWithCombat(session, a).ActionSet!)
                .Where(s => s.Candidates.Count > 0).ToArray();
            if (sets.Length == 0) break;
            var set = sets[0];
            var control = set.Candidates.Any(v => v.Kind.StartsWith("combat-cycle-control-", StringComparison.Ordinal));
            var candidate = control
                ? set.Candidates.First(v => v.Kind == (repeat && !repeated ? "combat-cycle-control-repeat" : "combat-cycle-control-finish"))
                : (repeat && !repeated ? set.Candidates.FirstOrDefault(v => v.Kind == "combat-reserve-release-release-i") : null)
                    ?? set.Candidates.OrderBy(v => v.ActionId, StringComparer.Ordinal).First();
            if (candidate.Kind == "combat-cycle-control-repeat") repeated = true;
            session = Step(r, session, set.Audience, candidate.ActionId, candidate.Kind);
        }
        r.Notes.Add($"{scenario}: frontier at {CampaignExercises.QueryCheckpoint(session).PositionId}, repeated={repeated}");
        return r;
    }

    private static ExerciseSession Step(SequencingRecorder r, ExerciseSession session, CampaignActionAudience audience,
        string actionId, string kind)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return r.Ok(kind, CampaignExercises.SubmitWithCombat(session, new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion, checkpoint.CampaignId, checkpoint.StateVersion,
            checkpoint.PositionId, audience, actionId)));
    }

    private static CampaignActionAudience Audience(LandSide side) =>
        side == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;

    private static CampaignActionAudience Other(CampaignActionAudience audience) =>
        audience == CampaignActionAudience.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
}
