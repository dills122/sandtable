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

/// <summary>
/// One row per (operation stage, acting role) occurrence. <paramref name="Prefix"/> is the
/// PendingDecision type-name prefix ("Second", "StageEntry", ...); null means the pre-#394 legacy
/// chain, which has no PendingDecision for Reserve/Move/Breakdown/Combat.
/// <paramref name="CombatArms"/> says whether the occurrence's own
/// CombatSelection/Assault/Truck/Post-Combat subtypes exist yet (Stage 3's first side flipped it with S3-5;
/// the Stage 3 second side stays false until S3-9). While false, positions after Breakdown derive to null.
/// </summary>
internal sealed record PendingOccurrence(string? Prefix, PendingTailOwner Tail, bool CombatArms);

/// <summary>
/// DATA, not branches. To cover a new occurrence (Stage 3 second side, Turns 2-6), add one row.
/// </summary>
internal static class PendingOccurrenceTable
{
    private static readonly Dictionary<(int Stage, LandActorRole Role), PendingOccurrence> Rows = new()
    {
        [(1, LandActorRole.FirstActingSide)] = new(null, PendingTailOwner.Standalone, true),
        [(1, LandActorRole.SecondActingSide)] = new("Second", PendingTailOwner.InsideAssault, true),
        [(2, LandActorRole.FirstActingSide)] = new("StageEntry", PendingTailOwner.InsideAssault, true),
        [(2, LandActorRole.SecondActingSide)] = new("StageEntrySecond", PendingTailOwner.InsideAssault, true),
        // S3-1..S3-5: Reserve/Move/MovementCompletion/Breakdown/CombatSelection/Assault/NoAttack (CombatArms flipped by S3-5).
        [(3, LandActorRole.FirstActingSide)] = new("ThirdStage", PendingTailOwner.InsideAssault, true),
        // S3-6..S3-9: second-side Reserve through Combat and its tail.
        [(3, LandActorRole.SecondActingSide)] = new("ThirdStageSecond", PendingTailOwner.InsideAssault, true),
    };

    internal static PendingOccurrence? Find(int operationStage, LandActorRole role) =>
        Rows.GetValueOrDefault((operationStage, role));
}
