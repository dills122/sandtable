using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;

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

    // Stage 3 is the sequential chain owned by the executor/hot-file workers; its rows are
    // Unwired until its Host kinds exist. Replace the reason with the stage list when wired.
    private const string Stage3 = "Stage 3 Host opening kinds are not wired yet (Stage 3 Core chain).";
    private const string Stage2Auto = "Automatic Core completion; Host System-step auto-drain not wired yet (T3 Stage 2 wiring).";
    private const string Stage2Owner = "Stage 2 owner opening kind is not wired yet (T3 Stage 2 wiring).";

    /// <summary>
    /// ONE-LINE PROCEDURE when adding a <see cref="PendingDecision"/> subtype: add
    /// <c>[typeof(NewPending)] = CampaignOpenSeatRow.Unwired("reason")</c> (or
    /// <c>.Stages(...)</c> / <c>.SystemOnly("reason")</c>) to this dictionary.
    /// </summary>
    internal static readonly IReadOnlyDictionary<Type, CampaignOpenSeatRow> Rows =
        new Dictionary<Type, CampaignOpenSeatRow>
        {
            // Stage 3 second side (S3-6..S3-8).
            [typeof(ThirdStageSecondCombatSelectionPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(TurnBoundaryPending)] = CampaignOpenSeatRow.Unwired("Turn-start Host openings are deferred to B5."),
            [typeof(ThirdStageSecondAssaultPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageSecondNoAttackPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageSecondBreakdownPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageSecondMovementCompletionPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageSecondMovePending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageSecondReservePending)] = CampaignOpenSeatRow.Unwired(Stage3),
            // Stage 3 first side.
            [typeof(ThirdStageAssaultPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageNoAttackPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageCombatSelectionPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageBreakdownPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageMovementCompletionPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageMovePending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageReservePending)] = CampaignOpenSeatRow.Unwired(Stage3),
            // Stage 2 -> 3 entry-bridge preamble.
            [typeof(ThirdStageNoObligationFleetRepairPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageNoObligationFleetAssignmentPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageNoObligationNavalConvoyArrivalPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageNoObligationOrganizationPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageWeatherPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            [typeof(ThirdStageInitiativeDeclarationPending)] = CampaignOpenSeatRow.Unwired(Stage3),
            // Stage 2 second side.
            [typeof(StageEntrySecondAssaultPending)] = CampaignOpenSeatRow.Unwired(Stage2Owner),
            [typeof(StageEntrySecondNoAttackPending)] = CampaignOpenSeatRow.Unwired(Stage2Auto),
            [typeof(StageEntrySecondCombatSelectionPending)] = CampaignOpenSeatRow.Unwired(Stage2Owner),
            [typeof(StageEntrySecondBreakdownPending)] = CampaignOpenSeatRow.Unwired(Stage2Auto),
            [typeof(StageEntrySecondMovementCompletionPending)] = CampaignOpenSeatRow.Unwired(Stage2Owner),
            [typeof(StageEntrySecondMovePending)] = CampaignOpenSeatRow.Unwired(Stage2Owner),
            [typeof(StageEntrySecondReservePending)] = CampaignOpenSeatRow.Unwired(Stage2Owner),
            // Stage 2 first side.
            [typeof(StageEntryAssaultPending)] = CampaignOpenSeatRow.Unwired(Stage2Owner),
            [typeof(StageEntryNoAttackPending)] = CampaignOpenSeatRow.Unwired(Stage2Auto),
            [typeof(StageEntryCombatSelectionPending)] = CampaignOpenSeatRow.Unwired(Stage2Owner),
            [typeof(StageEntryBreakdownPending)] = CampaignOpenSeatRow.Unwired(Stage2Owner),
            [typeof(StageEntryMovementCompletionPending)] = CampaignOpenSeatRow.SystemOnly("Stage 2 first-side Breakdown auto-drains."),
            [typeof(StageEntryMovePending)] = CampaignOpenSeatRow.Stages("stage-entry-movement", "stage-entry-movement-completion"),
            [typeof(StageEntryReservePending)] = CampaignOpenSeatRow.Stages("stage-entry-reserve", "stage-entry-movement", "stage-entry-movement-completion"),
            // Prior cycle's second-acting side tail (Stage 1).
            [typeof(SecondAssaultPending)] = CampaignOpenSeatRow.Stages([.. CombatChain, "truck-convoy-pass", "post-combat-pass", "stage-entry-initiative-declaration"]),
            [typeof(SecondNoAttackPending)] = CampaignOpenSeatRow.Stages(CombatChain),
            [typeof(SecondCombatSelectionPending)] = CampaignOpenSeatRow.Stages(CombatChain),
            [typeof(SecondBreakdownPending)] = CampaignOpenSeatRow.Stages("combat-selection"),
            [typeof(SecondMovementCompletionPending)] = CampaignOpenSeatRow.SystemOnly("Breakdown is a System step the Host auto-drains."),
            [typeof(SecondMovePending)] = CampaignOpenSeatRow.Stages("second-movement", "second-movement-completion"),
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
        return new CampaignOpenSeatCandidates(false, row.StageNames, row.ExceptionReason);
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
