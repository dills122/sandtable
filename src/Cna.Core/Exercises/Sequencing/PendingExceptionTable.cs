namespace Cna.Core.Exercises.Sequencing;

/// <summary>
/// PendingDecision subtypes the cursor derivation deliberately never produces. Each entry needs a
/// reason; the S2 oracle fails on any subtype that is neither derivable nor listed here.
/// </summary>
internal static class PendingExceptionTable
{
    /// <summary>
    /// NoAttack arms: the no-attack completion also installs the Assault runtime with non-empty
    /// Events, and the Assault arm is checked first, so these are never observable in a driven session.
    /// </summary>
    internal static readonly IReadOnlyList<string> Unobservable =
    [
        // E4b (#655): an open Reaction window is an overlay on the stage cut, derived from the last
        // accepted V44/V59/V60 and never from the catalog cursor position.
        "NativeReactionPending",
        // Successor-turn play is derived from the latest accepted event after a rollover, not the catalog cursor.
        "TurnSuccessorPending",
        "SecondNoAttackPending",
        "StageEntryNoAttackPending",
        "StageEntrySecondNoAttackPending",
        "ThirdStageNoAttackPending",
        "ThirdStageSecondNoAttackPending",
    ];
}
