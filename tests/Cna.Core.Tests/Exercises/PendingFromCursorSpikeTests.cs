using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Xunit;

namespace Cna.Core.Tests.Exercises;

// Feasibility spike S1 (docs/work/plans/2026-11-24-pending-from-cursor-spike.md): can
// ExerciseSession.Pending be derived from (checkpoint position id, last event type,
// side role) over Cna1979LandSequenceV4, with only an explicit exception list?
// Read-only on every production type: samples the REAL Core dispatch and the bare
// reflection fixtures, and compares ExerciseSession.Pending to PendingFromCursor.Derive.
public sealed class PendingFromCursorSpikeTests
{
    // ---- the pure function under test ---------------------------------------------

    internal enum LastEvent
    {
        // The last accepted event completed the catalog position immediately before the
        // checkpoint's position (the checkpoint carries NextPositionId).
        CompletedPosition,
        // A Move: the only event in the Movement segment that does NOT advance the
        // position (checkpoint = CurrentPositionId(snapshot), the Movement position).
        MoveWithinPosition,
    }

    internal enum TailOwner { Standalone, InsideAssault }

    // DATA, not branches. One row per (operation stage, role-class) occurrence.
    // Prefix == null means "pre-#394 legacy chain, no PendingDecision for its
    // Reserve/Move/Breakdown/Combat positions".
    internal sealed record Occurrence(string? Prefix, TailOwner Tail, bool HasPreamble);

    internal static class PendingFromCursor
    {
        // Role-class is read off the catalog position (LandActorRole), so the "side
        // role" input of the proposal is already carried by positionId.
        private static readonly Dictionary<(bool IsFirstTurn, int Stage, LandActorRole Role), Occurrence> Occurrences = new()
        {
            [(true, 1, LandActorRole.FirstActingSide)] = new(null, TailOwner.Standalone, false),
            [(true, 1, LandActorRole.SecondActingSide)] = new("Second", TailOwner.InsideAssault, false),
            [(true, 2, LandActorRole.FirstActingSide)] = new("StageEntry", TailOwner.InsideAssault, true),
            [(true, 2, LandActorRole.SecondActingSide)] = new("StageEntrySecond", TailOwner.InsideAssault, true),
            // S3-1..S3-5: Reserve/Move/MovementCompletion/Breakdown/CombatSelection/Assault (+ unobservable NoAttack).
            [(true, 3, LandActorRole.FirstActingSide)] = new("ThirdStage", TailOwner.InsideAssault, true),
            // S3-6..S3-9: second-side Reserve through Combat and its tail.
            [(true, 3, LandActorRole.SecondActingSide)] = new("ThirdStageSecond", TailOwner.InsideAssault, true),
        };

        // Stage-2 preamble: catalog phase/segment -> subtype name. The names are
        // irregular ("StageEntryWeather" vs "NoObligation*"), so they are table data.
        private static readonly Dictionary<(bool IsFirstTurn, int Stage, string Phase, string? Segment), string> PreambleNames = new()
        {
            [(false, 0, LandPhaseIds.EndOfTurn, null)] = "TurnBoundaryPending",
            [(false, 0, LandPhaseIds.InitiativeDetermination, null)] = "TurnBoundaryPending",
            [(false, 0, LandPhaseIds.NavalConvoySchedule, null)] = "TurnBoundaryPending",
            [(false, 0, LandPhaseIds.TacticalShipping, null)] = "TurnBoundaryPending",
            [(false, 1, LandPhaseIds.InitiativeDeclaration, null)] = "TurnBoundaryPending",
            [(false, 1, LandPhaseIds.WeatherDetermination, null)] = "TurnBoundaryPending",
            [(false, 1, LandPhaseIds.Organization, null)] = "TurnBoundaryPending",
            [(false, 1, LandPhaseIds.NavalConvoyArrival, null)] = "TurnBoundaryPending",
            [(false, 1, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetAssignment)] = "TurnBoundaryPending",
            [(false, 1, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetRepair)] = "TurnBoundaryPending",
            [(true, 3, LandPhaseIds.InitiativeDeclaration, null)] = "ThirdStageInitiativeDeclarationPending",
            [(true, 3, LandPhaseIds.WeatherDetermination, null)] = "ThirdStageWeatherPending",
            [(true, 3, LandPhaseIds.Organization, null)] = "ThirdStageNoObligationOrganizationPending",
            [(true, 3, LandPhaseIds.NavalConvoyArrival, null)] = "ThirdStageNoObligationNavalConvoyArrivalPending",
            [(true, 3, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetAssignment)] = "ThirdStageNoObligationFleetAssignmentPending",
            [(true, 3, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetRepair)] = "ThirdStageNoObligationFleetRepairPending",
            [(true, 2, LandPhaseIds.InitiativeDeclaration, null)] = "InitiativeDeclarationPending",
            [(true, 2, LandPhaseIds.WeatherDetermination, null)] = "StageEntryWeatherPending",
            [(true, 2, LandPhaseIds.Organization, null)] = "NoObligationOrganizationPending",
            [(true, 2, LandPhaseIds.NavalConvoyArrival, null)] = "NoObligationNavalConvoyArrivalPending",
            [(true, 2, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetAssignment)] = "NoObligationFleetAssignmentPending",
            [(true, 2, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetRepair)] = "NoObligationFleetRepairPending",
        };

        private enum Mech { Reserve, MovementCompletion, Breakdown, CombatSelection, Combat, Truck, PostCombat }

        // Completed catalog position -> mechanic.
        private static Mech? MechanicOf(LandSequencePosition p) => p.PhaseId switch
        {
            var x when x == LandPhaseIds.ReserveDesignation => Mech.Reserve,
            var x when x == LandPhaseIds.MovementAndCombat => p.SegmentId switch
            {
                var s when s == LandSegmentIds.Movement => Mech.MovementCompletion,
                var s when s == LandSegmentIds.BreakdownDetermination => Mech.Breakdown,
                var s when s == LandSegmentIds.Combat && p.StepId == LandStepIds.PositionDetermination => Mech.CombatSelection,
                _ => Mech.Combat, // later combat steps and reserve-release
            },
            var x when x == LandPhaseIds.TruckConvoyMovement => Mech.Truck,
            var x when x == LandPhaseIds.CommonwealthRailMovement
                || x == LandPhaseIds.Repair || x == LandPhaseIds.Patrol => Mech.PostCombat,
            _ => null,
        };

        public static string? Derive(IReadOnlyList<LandSequencePosition> turn, string checkpointPositionId, LastEvent last, bool isFirstTurn = true)
        {
            var index = turn.ToList().FindIndex(p => p.PositionId == checkpointPositionId);
            if (index < 0) throw new ArgumentException("Position is not in this turn.", nameof(checkpointPositionId));
            var at = turn[index];

            if (last == LastEvent.MoveWithinPosition)
                return at.PhaseId == LandPhaseIds.MovementAndCombat && at.SegmentId == LandSegmentIds.Movement
                    && Occurrences.TryGetValue((isFirstTurn, at.OperationStage, at.ActorRole), out var mo) && mo.Prefix is { } mp
                    ? mp + "MovePending" : null;

            if (index == 0 && isFirstTurn) return null;
            var done = index == 0 ? Cna1979LandSequenceV4.CreateTurn(at.GameTurn - 1)[^1] : turn[index - 1];

            if (done.ActorRole is LandActorRole.FirstActingSide or LandActorRole.SecondActingSide)
            {
                if (!Occurrences.TryGetValue((isFirstTurn, done.OperationStage, done.ActorRole), out var occ)) return null;
                if (MechanicOf(done) is not { } mech) return null;
                if (occ.Prefix is null) // legacy: only the #394-era tail arms exist
                    return mech switch { Mech.Truck => "TruckConvoyPassPending", Mech.PostCombat => "PostCombatPassPending", _ => null };
                return mech switch
                {
                    Mech.Reserve => occ.Prefix + "ReservePending",
                    Mech.MovementCompletion => occ.Prefix + "MovementCompletionPending",
                    Mech.Breakdown => occ.Prefix + "BreakdownPending",
                    Mech.CombatSelection => occ.Prefix + "CombatSelectionPending",
                    // Combat / Truck / PostCombat all accrete inside the Assault chain here.
                    _ => occ.Prefix + "AssaultPending",
                };
            }

            // Stage 3 preamble positions have no side occurrence; the preamble name table applies.
            return PreambleNames.TryGetValue((isFirstTurn, done.OperationStage, done.PhaseId, done.SegmentId), out var name) ? name : null;
        }
    }

    internal static readonly IReadOnlyList<string> ExplicitExceptions =
    [
        // NoAttack: no-attack completion also installs the Assault runtime, whose Events
        // are non-empty, and the Assault arm is checked first -> never observable.
        "SecondNoAttackPending", "StageEntryNoAttackPending", "StageEntrySecondNoAttackPending", "ThirdStageNoAttackPending", "ThirdStageSecondNoAttackPending",
    ];

    // ---- sampling -----------------------------------------------------------------

    private sealed record Sample(string Label, string? Pending, string PositionId, int NonNullCurrentFields, int GameTurn);

    private readonly List<Sample> samples = [];

    private void Rec(string label, ExerciseSession session)
    {
        var pending = session.Pending?.GetType().Name;
        string position;
        try { position = CampaignExercises.QueryCheckpoint(session).PositionId; }
        catch (Exception ex) { position = "<throws " + ex.GetType().Name + ">"; }
        var nonNull = typeof(ExerciseSession).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Count(p => p.Name.StartsWith("Current", StringComparison.Ordinal)
                && p.PropertyType.Name.StartsWith("CampaignNative", StringComparison.Ordinal)
                && p.GetValue(session) is not null);
        samples.Add(new Sample(label, pending, position, nonNull, CampaignGameTurn.CurrentGameTurn(session)));
    }

    [Fact]
    public void CursorDerivationReproducesPendingOverEveryDrivenRealSessionState()
    {
        RunRealSession();
        var turn = Cna1979LandSequenceV4.CreateTurn(samples[0].GameTurn);

        var rows = samples.Select(sample => (sample,
            derived: PendingFromCursor.Derive(turn, sample.PositionId, LastEvent.CompletedPosition))).ToList();
        var mismatches = rows.Where(r => r.derived != r.sample.Pending).ToList();

        if (Environment.GetEnvironmentVariable("PENDING_SPIKE_DUMP") is { Length: > 0 } dump)
            File.WriteAllLines(dump, rows.Select(r =>
                $"{(r.derived == r.sample.Pending ? "OK  " : "DIFF")} {r.sample.Label} | actual={r.sample.Pending ?? "<null>"} | derived={r.derived ?? "<null>"} | nonNullFields={r.sample.NonNullCurrentFields} | {r.sample.PositionId}")
                .Append($"samples={rows.Count} mismatches={mismatches.Count} distinctPending={rows.Select(r => r.sample.Pending).Where(x => x is not null).Distinct().Count()}"));

        Assert.Empty(mismatches.Select(m => $"{m.sample.Label}: actual={m.sample.Pending} derived={m.derived}"));
        Assert.True(rows.Count > 50);
        // Staleness really was exercised: many states carry >1 never-reset Current* fields.
        Assert.Contains(rows, r => r.sample.NonNullCurrentFields >= 5 && r.derived == r.sample.Pending);
    }

    [Fact]
    public void LaterTurnReferenceDerivesTheBoundaryPendingFromRepeatedPositionIds()
    {
        foreach (var sample in Sequencing.SequencingScenarios.All.SelectMany(s => s.Samples)
            .Where(sample => sample.Label.StartsWith("turn.", StringComparison.Ordinal)))
        {
            var turn = Cna1979LandSequenceV4.CreateTurn(sample.GameTurn);
            Assert.Equal(sample.PendingName, PendingFromCursor.Derive(turn, sample.PositionId, LastEvent.CompletedPosition, false));
        }
    }

    [Fact]
    public void EverySubtypeIsDerivableFromTheCatalogExceptTheExplicitExceptionList()
    {
        var subtypes = typeof(PendingDecision).Assembly.GetTypes()
            .Where(t => t.BaseType == typeof(PendingDecision)).Select(t => t.Name).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(50, subtypes.Count); // 29 + 6 Stage 3 preamble + 7 per Stage 3 acting side.

        var turn = Cna1979LandSequenceV4.CreateTurn(1);
        var later = Cna1979LandSequenceV4.CreateTurn(2);
        var reachable = turn.SelectMany(p => new[] { LastEvent.CompletedPosition, LastEvent.MoveWithinPosition }
                .Select(e => PendingFromCursor.Derive(turn, p.PositionId, e)))
            .Concat(later.Select(p => PendingFromCursor.Derive(later, p.PositionId, LastEvent.CompletedPosition, false)))
            .Where(x => x is not null).Select(x => x!).ToHashSet(StringComparer.Ordinal);

        Assert.Empty(reachable.Except(subtypes)); // derivation never invents a type
        var underived = subtypes.Except(reachable).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(ExplicitExceptions.Order(StringComparer.Ordinal), underived);
    }

    [Fact]
    public void MoveIsTheOnlyStateThatNeedsTheLastEventTypeBeyondTheCursor()
    {
        var turn = Cna1979LandSequenceV4.CreateTurn(1);
        var ambiguous = turn.Where(p => Enum.GetValues<LastEvent>()
                .Select(e => PendingFromCursor.Derive(turn, p.PositionId, e)).Where(x => x is not null).Distinct().Count() > 1)
            .Select(p => p.PositionId).ToList();
        // Exactly the five Movement positions that can be reached by Reserve-completion
        // OR Move (stage 1 first side is legacy and has no Move arm).
        Assert.Equal(
        [
            "land.position.operation-1.second-player.movement-and-combat.movement",
            "land.position.operation-2.first-player.movement-and-combat.movement",
            "land.position.operation-2.second-player.movement-and-combat.movement",
            "land.position.operation-3.first-player.movement-and-combat.movement",
            "land.position.operation-3.second-player.movement-and-combat.movement",
        ], ambiguous);
    }

    // ---- real session driver (mirrors CampaignStageThreeEntryVerificationTests) --------

    private void RunRealSession()
    {
        var session = CampaignExercises.Begin(CreateRequest()).Session!;
        Rec("begin", session);
        session = AdvanceFirstSideToCombatPositionDetermination(session);
        Rec("op1.first.legacy-at-combat-position", session);
        var stage1Order = session.CurrentSnapshot.OperationStageOrders.Single(value => value.OperationStage == 1);
        var firstAud = stage1Order.FirstSide == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var secondAud = firstAud == CampaignActionAudience.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var firstEl = stage1Order.FirstSide == LandSide.Axis ? "axis-infantry" : "commonwealth-infantry";
        var secondEl = stage1Order.FirstSide == LandSide.Axis ? "commonwealth-infantry" : "axis-infantry";

        session = SubmitFinishWithoutAttack(session, firstAud, firstEl, CampaignExercises.CompleteNativeCombatNoAttack, "op1.first");
        session = FinishCycleThroughPostCombatPasses(session, firstAud, "op1.first");
        session = CloseOutSecondSideChain(session, secondAud, secondEl);

        var gameTurn = session.CurrentSnapshot.Setup.InitialGameTurn;
        var holder = session.CurrentSnapshot.InitiativeHolder!.Value;
        var holderAud = holder == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var otherAud = holderAud == CampaignActionAudience.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var holderEl = holder == LandSide.Axis ? "axis-infantry" : "commonwealth-infantry";
        var otherEl = holder == LandSide.Axis ? "commonwealth-infantry" : "axis-infantry";

        var declared = CampaignExercises.SubmitStageEntryInitiativeDeclaration(session, holderAud, Hash("decl"), actFirst: true);
        Assert.True(declared.IsAccepted, declared.RejectionReason.ToString());
        session = declared.SuccessorSession!;
        Rec("op2.initiative-declaration", session);

        var weather = CampaignNativeWeatherDeterminationV50.Create(session.CurrentInitiativeDeclaration!,
            session.CurrentSnapshot.RandomState, gameTurn, session.CurrentContacts.AuthorityVersion);
        session = session.WithStageEntryWeather(weather, session.CurrentContacts.AdvanceWeatherDetermination(weather));
        Rec("op2.weather", session);
        var org = CampaignNativeNoObligationOrganizationV51.Create(weather, weather.StateVersion);
        session = session.WithNoObligationOrganization(org, session.CurrentContacts.AdvanceNoObligationOrganization(org));
        Rec("op2.organization", session);
        var convoy = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(org, session.Context, org.StateVersion);
        session = session.WithNoObligationNavalConvoyArrival(convoy, session.CurrentContacts.AdvanceNoObligationNavalConvoyArrival(convoy));
        Rec("op2.naval-convoy-arrival", session);
        var assign = CampaignNativeNoObligationFleetAssignmentV53.Create(convoy, convoy.StateVersion);
        session = session.WithNoObligationFleetAssignment(assign, session.CurrentContacts.AdvanceNoObligationFleetAssignment(assign));
        Rec("op2.fleet-assignment", session);
        var repair = CampaignNativeNoObligationFleetRepairV54.Create(assign, assign.StateVersion);
        session = session.WithNoObligationFleetRepair(repair, session.CurrentContacts.AdvanceNoObligationFleetRepair(repair));
        Rec("op2.fleet-repair", session);

        session = CloseOutStageEntrySide(session, holderAud, holderEl, isSecondSide: false);
        session = CloseOutStageEntrySide(session, otherAud, otherEl, isSecondSide: true);

        // Stage 3 preamble (retained by #570), same construction as
        // CampaignStageThreeEntryVerificationTests.
        var terminal = session.CurrentStageEntrySecondAssault!.PostCombatPasses[^1];
        var declared3 = CampaignNativeInitiativeDeclarationV49.Create(terminal, holder, actFirst: false, gameTurn, terminal.StateVersion);
        session = session.WithThirdStageInitiativeDeclaration(declared3, session.CurrentContacts.AdvanceInitiativeDeclaration(declared3));
        Rec("op3.initiative-declaration", session);
        var initialRandom = session.CurrentSnapshot.RandomState;
        var random = new RandomStreamState(initialRandom.ContractVersion, initialRandom.AlgorithmId,
            initialRandom.Seed, weather.ResultRandomCursor);
        var weather3 = CampaignNativeWeatherDeterminationV50.Create(declared3, random, gameTurn, session.CurrentContacts.AuthorityVersion);
        session = session.WithThirdStageWeather(weather3, session.CurrentContacts.AdvanceWeatherDetermination(weather3));
        Rec("op3.weather", session);
        var org3 = CampaignNativeNoObligationOrganizationV51.Create(weather3, weather3.StateVersion);
        session = session.WithThirdStageNoObligationOrganization(org3, session.CurrentContacts.AdvanceNoObligationOrganization(org3));
        Rec("op3.organization", session);
        var arrival3 = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(org3, session.Context, org3.StateVersion);
        session = session.WithThirdStageNoObligationNavalConvoyArrival(arrival3, session.CurrentContacts.AdvanceNoObligationNavalConvoyArrival(arrival3));
        Rec("op3.naval-convoy-arrival", session);
        var assign3 = CampaignNativeNoObligationFleetAssignmentV53.Create(arrival3, arrival3.StateVersion);
        session = session.WithThirdStageNoObligationFleetAssignment(assign3, session.CurrentContacts.AdvanceNoObligationFleetAssignment(assign3));
        Rec("op3.fleet-assignment", session);
        var repair3 = CampaignNativeNoObligationFleetRepairV54.Create(assign3, assign3.StateVersion);
        session = session.WithThirdStageNoObligationFleetRepair(repair3, session.CurrentContacts.AdvanceNoObligationFleetRepair(repair3));
        Rec("op3.fleet-repair", session);
    }


    // Real Move (one per chain, when the legal-action set offers one): the only
    // non-position-advancing event in the Movement segment, so it is where "last event
    // type" matters.
    private ExerciseSession TryMove(ExerciseSession session, CampaignActionAudience audience, string tag,
        Func<ExerciseSession, CampaignActionAudience, string, string, ExerciseStepResult> submit)
    {
        var set = CampaignExercises.Query(session, audience).ActionSet;
        var move = set?.Candidates.OfType<CombatExerciseOwnerChoiceAction>()
            .FirstOrDefault(c => c.Stage == CombatExerciseOwnerChoiceAction.SecondMovementStage);
        if (move is null)
        {
            moveNotes.Add($"{tag}: no move candidate offered ({set?.Candidates.Count ?? -1} candidates: "
                + string.Join(",", set?.Candidates.Select(c => c.GetType().Name).Distinct() ?? []) + ")");
            return session;
        }
        var result = submit(session, audience, move.ActionId, move.SubjectId);
        if (!result.IsAccepted)
        {
            moveNotes.Add($"{tag}: move rejected {result.RejectionReason}");
            return session;
        }
        session = result.SuccessorSession!;
        Rec($"{tag}.move", session);
        return session;
    }

    private readonly List<string> moveNotes = [];

    private static CampaignCreationRequest CreateRequest()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        return new CampaignCreationRequest(1, "campaign-pending-cursor-spike", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId);
    }

    private static string Hash(string label) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(label)));

    private ExerciseSession AdvanceFirstSideToCombatPositionDetermination(ExerciseSession session)
    {
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination)
                return session;
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var action = set.Candidates.FirstOrDefault(value => value is not MoveElementAction) ?? set.Candidates[0];
            session = CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
                set.StateVersion, set.PositionId, set.Audience, action.ActionId)).SuccessorSession!;
            Rec($"op1.first.legacy-step-{index}", session);
        }
        throw new InvalidOperationException("Combat position was not reached.");
    }

    private ExerciseSession FinishCycleThroughPostCombatPasses(ExerciseSession session,
        CampaignActionAudience audience, string tag)
    {
        var cc = CampaignExercises.SubmitNativeCycleControl(session, audience,
            CampaignNativeCycleControlChoiceV28.Finish, Hash("cycle-control-finish"));
        Assert.True(cc.IsAccepted, cc.RejectionReason.ToString());
        session = cc.SuccessorSession!;
        Rec($"{tag}.cycle-control-finish", session);

        var truck = CampaignExercises.SubmitTruckConvoyPass(session, audience, Hash("truck-convoy-pass"));
        Assert.True(truck.IsAccepted, truck.RejectionReason.ToString());
        session = truck.SuccessorSession!;
        Rec($"{tag}.truck-convoy-pass", session);

        for (var index = 0; index < 4; index++)
        {
            var pass = CampaignExercises.SubmitPostCombatPass(session, audience, Hash($"post-combat-pass-{index}"));
            Assert.True(pass.IsAccepted, pass.RejectionReason.ToString());
            session = pass.SuccessorSession!;
            Rec($"{tag}.post-combat-pass-{index}", session);
        }
        return session;
    }

    private ExerciseSession CloseOutSecondSideChain(ExerciseSession session,
        CampaignActionAudience audience, string actingElementId)
    {
        const string tag = "op1.second";
        var reserve = CampaignExercises.SubmitSecondReserve(session, audience, Hash("second-reserve"), elementId: null);
        Assert.True(reserve.IsAccepted, reserve.RejectionReason.ToString());
        session = reserve.SuccessorSession!;
        Rec($"{tag}.reserve", session);
        session = TryMove(session, audience, tag, (x, a, id, el) => CampaignExercises.SubmitSecondMovement(x, a, id, el));

        var move = CampaignExercises.CompleteSecondMovement(session, audience, Hash("second-movement-completion"));
        Assert.True(move.IsAccepted, move.RejectionReason.ToString());
        session = move.SuccessorSession!;
        Rec($"{tag}.movement-completion", session);

        var breakdown = CampaignExercises.CompleteSecondBreakdown(session);
        Assert.True(breakdown.IsAccepted, breakdown.RejectionReason.ToString());
        session = breakdown.SuccessorSession!;
        Rec($"{tag}.breakdown", session);

        session = SubmitFinishWithoutAttack(session, audience, actingElementId,
            CampaignExercises.CompleteSecondCombatNoAttack, tag);
        return FinishCycleThroughPostCombatPasses(session, audience, tag);
    }

    private ExerciseSession CloseOutStageEntrySide(ExerciseSession session,
        CampaignActionAudience audience, string actingElementId, bool isSecondSide)
    {
        var tag = isSecondSide ? "op2.second" : "op2.first";
        var reserve = isSecondSide
            ? CampaignExercises.SubmitStageEntrySecondReserve(session, audience, Hash("stage2-reserve"), elementId: null)
            : CampaignExercises.SubmitStageEntryReserve(session, audience, Hash("stage2-reserve"), elementId: null);
        Assert.True(reserve.IsAccepted, reserve.RejectionReason.ToString());
        session = reserve.SuccessorSession!;
        Rec($"{tag}.reserve", session);
        session = TryMove(session, audience, tag, (x, a, id, el) => isSecondSide
            ? CampaignExercises.SubmitStageEntrySecondMovement(x, a, id, el)
            : CampaignExercises.SubmitStageEntryMovement(x, a, id, el));

        var move = isSecondSide
            ? CampaignExercises.CompleteStageEntrySecondMovement(session, audience, Hash("stage2-movement-completion"))
            : CampaignExercises.CompleteStageEntryMovement(session, audience, Hash("stage2-movement-completion"));
        Assert.True(move.IsAccepted, move.RejectionReason.ToString());
        session = move.SuccessorSession!;
        Rec($"{tag}.movement-completion", session);

        var breakdown = isSecondSide
            ? CampaignExercises.CompleteStageEntrySecondBreakdown(session)
            : CampaignExercises.CompleteStageEntryBreakdown(session);
        Assert.True(breakdown.IsAccepted, breakdown.RejectionReason.ToString());
        session = breakdown.SuccessorSession!;
        Rec($"{tag}.breakdown", session);

        session = SubmitFinishWithoutAttack(session, audience, actingElementId,
            isSecondSide ? CampaignExercises.CompleteStageEntrySecondCombatNoAttack
                : CampaignExercises.CompleteStageEntryCombatNoAttack, tag);
        return FinishCycleThroughPostCombatPasses(session, audience, tag);
    }

    private ExerciseSession SubmitFinishWithoutAttack(ExerciseSession session,
        CampaignActionAudience audience, string actingElementId,
        Func<ExerciseSession, ExerciseStepResult> completeNoAttack, string tag)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var actionId = new FinishCombatWithoutAttackAction(actingElementId).ActionId;
        var submission = new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, actionId);
        var result = CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, actingElementId);
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        session = result.SuccessorSession!;
        Rec($"{tag}.combat-selection", session);

        var completion = completeNoAttack(session);
        Assert.True(completion.IsAccepted, completion.RejectionReason.ToString());
        session = completion.SuccessorSession!;
        Rec($"{tag}.combat-no-attack", session);

        var release = CampaignExercises.CompleteNativeReserveRelease(session);
        Assert.True(release.IsAccepted, release.RejectionReason.ToString());
        session = release.SuccessorSession!;
        Rec($"{tag}.reserve-release", session);
        return session;
    }
}
