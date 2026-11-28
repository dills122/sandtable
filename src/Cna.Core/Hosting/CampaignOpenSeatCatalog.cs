using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Exercises.Sequencing;
using Cna.Core.Rules;

namespace Cna.Core.Hosting;

/// <summary>
/// The single registry that says which Host owner-opening stages can be open at a given
/// <see cref="PendingDecision"/> position. It backs the read-only "what is open for my seat"
/// derivation (issue #621): the Host narrows its in-memory opening probes to the candidate
/// stages named here, so a client never has to guess the stage ladder.
///
/// Every concrete <see cref="PendingDecision"/> subtype MUST have exactly one row in
/// <see cref="Rows"/>: either its candidate Host stages, or an explicit exception reason
/// (<see cref="CampaignOpenSeatRow.Unwired"/> when no Host opening kind exists yet,
/// <see cref="CampaignOpenSeatRow.SystemOnly"/> when Core completes it without a human).
/// A reflection test in Cna.Core.Tests fails when a subtype has no row, so a new Pending
/// subtype cannot land silently.
/// </summary>
public static class CampaignOpenSeatCatalog
{
    private static readonly string[] CombatChain =
    [
        "combat-selection", "retreat-before-assault", "force-assignment", "post-assault-retreat",
        "custody", "reserve-release", "cycle-control", "repeat-movement", "repeat-combat-selection",
    ];

    private static readonly string[] StageEntryReserve = ["stage-entry-reserve", "stage-entry-movement", "stage-entry-movement-completion"];
    private static readonly string[] StageEntryMove = ["stage-entry-movement", "stage-entry-movement-completion", "reaction"];
    private static readonly string[] StageEntryTail = ["reserve-release", "cycle-control", "truck-convoy-pass", "post-combat-pass"];

    /// <summary>
    /// ONE-LINE PROCEDURE when adding a <see cref="PendingDecision"/> subtype: add
    /// <c>[typeof(NewPending)] = CampaignOpenSeatRow.Unwired("reason")</c> (or
    /// <c>.Stages(...)</c> / <c>.SystemOnly("reason")</c>) to this dictionary.
    /// </summary>
    internal static readonly IReadOnlyDictionary<Type, CampaignOpenSeatRow> Rows =
        new Dictionary<Type, CampaignOpenSeatRow>
        {
            // E4b (#655): an open native Reaction window belongs to the reacting seat, whatever the stage.
            [typeof(NativeReactionPending)] = CampaignOpenSeatRow.Stages("reaction"),
            // Stage 3 second side.
            [typeof(ThirdStageSecondCombatSelectionPending)] = CampaignOpenSeatRow.Stages("combat-selection"),
            [typeof(TurnSuccessorPending)] = CampaignOpenSeatRow.Unwired("Successor-turn play is Core-only until Host exposure lands."),
            [typeof(TurnBoundaryPending)] = CampaignOpenSeatRow.Stages("stage-entry-initiative-declaration", "stage-entry-reserve"),
            [typeof(ThirdStageSecondAssaultPending)] = CampaignOpenSeatRow.Stages(StageEntryTail),
            [typeof(ThirdStageSecondNoAttackPending)] = CampaignOpenSeatRow.SystemOnly("No-attack auto-completes."),
            [typeof(ThirdStageSecondBreakdownPending)] = CampaignOpenSeatRow.Stages("combat-selection"),
            [typeof(ThirdStageSecondMovementCompletionPending)] = CampaignOpenSeatRow.SystemOnly("Stage 3 Breakdown auto-drains."),
            [typeof(ThirdStageSecondMovePending)] = CampaignOpenSeatRow.Stages(StageEntryMove),
            [typeof(ThirdStageSecondReservePending)] = CampaignOpenSeatRow.Stages(StageEntryReserve),
            // Stage 3 first side.
            [typeof(ThirdStageAssaultPending)] = CampaignOpenSeatRow.Stages(StageEntryTail),
            [typeof(ThirdStageNoAttackPending)] = CampaignOpenSeatRow.SystemOnly("No-attack auto-completes."),
            [typeof(ThirdStageCombatSelectionPending)] = CampaignOpenSeatRow.Stages("combat-selection"),
            [typeof(ThirdStageBreakdownPending)] = CampaignOpenSeatRow.Stages("combat-selection"),
            [typeof(ThirdStageMovementCompletionPending)] = CampaignOpenSeatRow.SystemOnly("Stage 3 Breakdown auto-drains."),
            [typeof(ThirdStageMovePending)] = CampaignOpenSeatRow.Stages(StageEntryMove),
            [typeof(ThirdStageReservePending)] = CampaignOpenSeatRow.Stages(StageEntryReserve),
            // Stage 2 -> 3 entry-bridge preamble.
            [typeof(ThirdStageNoObligationFleetRepairPending)] = CampaignOpenSeatRow.Stages("stage-entry-reserve"),
            [typeof(ThirdStageNoObligationFleetAssignmentPending)] = CampaignOpenSeatRow.SystemOnly("Stage 3 preamble auto-drains."),
            [typeof(ThirdStageNoObligationNavalConvoyArrivalPending)] = CampaignOpenSeatRow.SystemOnly("Stage 3 preamble auto-drains."),
            [typeof(ThirdStageNoObligationOrganizationPending)] = CampaignOpenSeatRow.SystemOnly("Stage 3 preamble auto-drains."),
            [typeof(ThirdStageWeatherPending)] = CampaignOpenSeatRow.SystemOnly("Stage 3 preamble auto-drains."),
            [typeof(ThirdStageInitiativeDeclarationPending)] = CampaignOpenSeatRow.SystemOnly("Stage 3 preamble auto-drains."),
            // Stage 2 second side.
            [typeof(StageEntrySecondAssaultPending)] = CampaignOpenSeatRow.Stages(StageEntryTail),
            [typeof(StageEntrySecondNoAttackPending)] = CampaignOpenSeatRow.SystemOnly("No-attack auto-completes."),
            [typeof(StageEntrySecondCombatSelectionPending)] = CampaignOpenSeatRow.Stages("combat-selection"),
            [typeof(StageEntrySecondBreakdownPending)] = CampaignOpenSeatRow.Stages("combat-selection"),
            [typeof(StageEntrySecondMovementCompletionPending)] = CampaignOpenSeatRow.SystemOnly("Stage 2 Breakdown auto-drains."),
            [typeof(StageEntrySecondMovePending)] = CampaignOpenSeatRow.Stages(StageEntryMove),
            [typeof(StageEntrySecondReservePending)] = CampaignOpenSeatRow.Stages(StageEntryReserve),
            // Stage 2 first side.
            [typeof(StageEntryAssaultPending)] = CampaignOpenSeatRow.Stages(StageEntryTail),
            [typeof(StageEntryNoAttackPending)] = CampaignOpenSeatRow.SystemOnly("No-attack auto-completes."),
            [typeof(StageEntryCombatSelectionPending)] = CampaignOpenSeatRow.Stages("combat-selection"),
            [typeof(StageEntryBreakdownPending)] = CampaignOpenSeatRow.Stages("combat-selection"),
            [typeof(StageEntryMovementCompletionPending)] = CampaignOpenSeatRow.SystemOnly("Stage 2 Breakdown auto-drains."),
            [typeof(StageEntryMovePending)] = CampaignOpenSeatRow.Stages(StageEntryMove),
            [typeof(StageEntryReservePending)] = CampaignOpenSeatRow.Stages(StageEntryReserve),
            // Prior cycle's second-acting side tail (Stage 1).
            [typeof(SecondAssaultPending)] = CampaignOpenSeatRow.Stages([.. CombatChain, "truck-convoy-pass", "post-combat-pass", "stage-entry-initiative-declaration"]),
            [typeof(SecondNoAttackPending)] = CampaignOpenSeatRow.Stages(CombatChain),
            [typeof(SecondCombatSelectionPending)] = CampaignOpenSeatRow.Stages(CombatChain),
            [typeof(SecondBreakdownPending)] = CampaignOpenSeatRow.Stages("combat-selection"),
            [typeof(SecondMovementCompletionPending)] = CampaignOpenSeatRow.SystemOnly("Breakdown is a System step the Host auto-drains."),
            [typeof(SecondMovePending)] = CampaignOpenSeatRow.Stages("second-movement", "second-movement-completion", "reaction"),
            [typeof(SecondReservePending)] = CampaignOpenSeatRow.Stages("second-reserve", "second-movement", "second-movement-completion"),
            // Stage 1 -> 2 entry-bridge preamble.
            [typeof(NoObligationFleetRepairPending)] = CampaignOpenSeatRow.Stages("stage-entry-reserve"),
            [typeof(NoObligationFleetAssignmentPending)] = CampaignOpenSeatRow.SystemOnly("Stage 2 preamble auto-drains."),
            [typeof(NoObligationNavalConvoyArrivalPending)] = CampaignOpenSeatRow.SystemOnly("Stage 2 preamble auto-drains."),
            [typeof(NoObligationOrganizationPending)] = CampaignOpenSeatRow.SystemOnly("Stage 2 preamble auto-drains."),
            [typeof(StageEntryWeatherPending)] = CampaignOpenSeatRow.SystemOnly("Stage 2 preamble auto-drains."),
            [typeof(InitiativeDeclarationPending)] = CampaignOpenSeatRow.SystemOnly("Stage 2 preamble auto-drains."),
            // Prior stage's close-out.
            [typeof(PostCombatPassPending)] = CampaignOpenSeatRow.Stages("post-combat-pass", "second-reserve"),
            [typeof(TruckConvoyPassPending)] = CampaignOpenSeatRow.Stages("post-combat-pass"),
        };

    /// <summary>Every Host stage name any row references (for the Host's own table-coverage test).</summary>
    public static IReadOnlyList<string> ReferencedStageNames { get; } =
        Rows.Values.SelectMany(row => row.StageNames).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

    /// <summary>
    /// Candidate stages for the session's current position. <see cref="CampaignOpenSeatCandidates.IsLegacyChain"/>
    /// is true when no Pending subtype applies (pregame/source setup and the pre-#394 Stage 1
    /// first-side chain): the caller then probes every registered stage.
    /// </summary>
    public static CampaignOpenSeatCandidates CandidatesFor(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var pending = session.Pending;
        if (pending is null) return new CampaignOpenSeatCandidates(true, [], null);
        if (!Rows.TryGetValue(pending.GetType(), out var row))
            throw new InvalidOperationException(
                $"Pending subtype {pending.GetType().Name} has no CampaignOpenSeatCatalog row.");
        // The retained subtype describes the last accepted event, not necessarily the new
        // decision at the cursor. In particular the first role's final pass leaves an Assault
        // subtype at the second role's Reserve Designation. Use the exact catalog position
        // for Stages 2 and 3, including the inter-stage declaration boundary.
        // Reaction overlays the suspended catalog position rather than advancing its cursor.
        // Probing that position would offer the phasing route instead of the reacting seat.
        if (pending is NativeReactionPending)
            return new CampaignOpenSeatCandidates(false, row.StageNames, null);
        var gameTurn = CampaignGameTurn.CurrentGameTurn(session);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var position = Cna1979LandSequence.CreateTurn(gameTurn).SingleOrDefault(p => p.PositionId == checkpoint.PositionId);
        var cursorStages = position is null ? null : StagesAt(position, gameTurn == session.CurrentSnapshot.Setup.InitialGameTurn);
        if (cursorStages is null) return new CampaignOpenSeatCandidates(false, row.StageNames, row.ExceptionReason);
        var reason = cursorStages.Count > 0 ? null
            : row.ExceptionReason ?? "No wired owner opening at this position.";
        return new CampaignOpenSeatCandidates(false, cursorStages, reason);
    }

    // null means use the subtype row; an empty list means no wired owner opening at this cursor.
    internal static IReadOnlyList<string>? StagesAt(LandSequencePosition position, bool isFirstTurn)
    {
        if (position.PhaseId == LandPhaseIds.EndOfTurn) return [];
        if (!isFirstTurn)
            return position.OperationStage == 1 ? position.PhaseId switch
            {
                LandPhaseIds.InitiativeDeclaration => ["stage-entry-initiative-declaration"],
                LandPhaseIds.ReserveDesignation when position.ActorRole == LandActorRole.FirstActingSide => ["stage-entry-reserve"],
                _ => [],
            } : [];
        if (position.OperationStage < 2) return null;
        if (position.PhaseId == LandPhaseIds.InitiativeDeclaration) return ["stage-entry-initiative-declaration"];
        if (PendingOccurrenceTable.Find(position.OperationStage, position.ActorRole, isFirstTurn) is null) return [];
        return position.PhaseId switch
        {
            LandPhaseIds.ReserveDesignation => ["stage-entry-reserve"],
            LandPhaseIds.MovementAndCombat => position.SegmentId switch
            {
                LandSegmentIds.Movement => StageEntryMove,
                LandSegmentIds.Combat when position.StepId == LandStepIds.PositionDetermination => ["combat-selection"],
                LandSegmentIds.Combat when position.StepId == LandStepIds.RetreatBeforeAssault => ["retreat-before-assault"],
                LandSegmentIds.Combat when position.StepId == LandStepIds.ForceAssignment => ["force-assignment"],
                LandSegmentIds.Combat when position.StepId == LandStepIds.CloseAssault => ["post-assault-retreat", "custody"],
                LandSegmentIds.ReserveRelease => ["reserve-release", "cycle-control"],
                _ => [],
            },
            LandPhaseIds.TruckConvoyMovement => ["truck-convoy-pass"],
            LandPhaseIds.CommonwealthRailMovement or LandPhaseIds.Repair or LandPhaseIds.Patrol => ["post-combat-pass"],
            _ => [],
        };
    }

    /// <summary>
    /// Independently owned element ids of the audience's side, ordinal-sorted, for per-element
    /// stages. Empty when the session carries no admitted content artifact (setup/source tracks).
    /// </summary>
    public static IReadOnlyList<string> OwnedElementIds(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth)) return [];
        if (session.Context.ArtifactV6 is not { } artifact) return [];
        var sideId = CampaignSnapshotSerializer.FormatSide(
            audience == CampaignActionAudience.Axis ? Rules.LandSide.Axis : Rules.LandSide.Commonwealth);
        return artifact.Definition.LegacyDefinition.Elements
            .Where(element => element.SideId == sideId && element.PlacementMode == ContentPlacementMode.Independent)
            .Select(element => element.ElementId)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}

/// <summary>Result of <see cref="CampaignOpenSeatCatalog.CandidatesFor"/>.</summary>
public sealed record CampaignOpenSeatCandidates(
    bool IsLegacyChain, IReadOnlyList<string> Stages, string? ExceptionReason);

/// <summary>One registry row: candidate Host stages, or an explicit exception reason.</summary>
internal sealed record CampaignOpenSeatRow(IReadOnlyList<string> StageNames, string? ExceptionReason)
{
    internal static CampaignOpenSeatRow Stages(params string[] stages) => new(stages, null);

    internal static CampaignOpenSeatRow Unwired(string reason) => new([], Reason(reason));

    internal static CampaignOpenSeatRow SystemOnly(string reason) => new([], Reason(reason));

    private static string Reason(string reason) =>
        string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException("An exception reason is required.") : reason;
}
