using Cna.Core.Rules;

namespace Cna.Core.Exercises.Sequencing;

/// <summary>Who owns the tail (Truck Convoy, Post-Combat passes) of a side occurrence.</summary>
internal enum PendingTailOwner
{
    /// <summary>The legacy first side: the passes are their own decisions.</summary>
    Standalone,

    /// <summary>Every other side: the passes accrete inside the Assault runtime, so the Assault arm shadows them.</summary>
    InsideAssault,
}

/// <summary>Which post-Breakdown PendingDecision arms an occurrence has so far (one flag per mechanic).</summary>
[Flags]
internal enum PendingArms
{
    None = 0,

    /// <summary>The Combat Selection arm (<c>*CombatSelectionPending</c>).</summary>
    CombatSelection = 1,

    /// <summary>The Assault arm, which also owns Combat, Truck Convoy and Post-Combat passes.</summary>
    Assault = 2,

    All = CombatSelection | Assault,
}

/// <summary>
/// One row per (operation stage, acting role) occurrence. <paramref name="Prefix"/> is the
/// PendingDecision type-name prefix ("Second", "StageEntry", ...); null means the pre-#394 legacy
/// chain, which has no PendingDecision for Reserve/Move/Breakdown/Combat.
/// <paramref name="Arms"/> says which of the occurrence's later arms exist yet; a Stage 3 slice adds one flag.
/// </summary>
internal sealed record PendingOccurrence(string? Prefix, PendingTailOwner Tail, PendingArms Arms);

/// <summary>
/// DATA, not branches. To cover a new occurrence (Stage 3 second side, Turns 2-6), add one row.
/// </summary>
internal static class PendingOccurrenceTable
{
    private static readonly Dictionary<(int Stage, LandActorRole Role), PendingOccurrence> Rows = new()
    {
        [(1, LandActorRole.FirstActingSide)] = new(null, PendingTailOwner.Standalone, PendingArms.All),
        [(1, LandActorRole.SecondActingSide)] = new("Second", PendingTailOwner.InsideAssault, PendingArms.All),
        [(2, LandActorRole.FirstActingSide)] = new("StageEntry", PendingTailOwner.InsideAssault, PendingArms.All),
        [(2, LandActorRole.SecondActingSide)] = new("StageEntrySecond", PendingTailOwner.InsideAssault, PendingArms.All),
        // S3-1..S3-4: Reserve/Move/MovementCompletion/Breakdown/CombatSelection; add PendingArms.Assault with S3-5+.
        [(3, LandActorRole.FirstActingSide)] = new("ThirdStage", PendingTailOwner.InsideAssault, PendingArms.CombatSelection),
    };

    internal static PendingOccurrence? Find(int operationStage, LandActorRole role) =>
        Rows.GetValueOrDefault((operationStage, role));
}
