namespace Cna.Core.Actions;

// Tasks 022A-022C: System-audience Combat steps offered to the trusted in-process Exercise harness
// (CampaignExercises.QueryWithCombat). The action ID is SHA-256 over the step's own canonical event
// bytes, which is exactly the receipt action ID the existing Core step already reports, so the
// harness's receipt/action binding holds without a new identity scheme. Not served by the Host and
// not Combat activation.
internal sealed record CombatExerciseSystemAction : CampaignActionCandidate
{
    internal const string NoAttackKind = "complete-combat-no-attack";
    internal const string BarrageKind = "complete-combat-barrage";
    internal const string ForceUnavailableKind = "cancel-combat-force-unavailable";
    internal const string ForceCompletionKind = "complete-combat-force-assignment";
    internal const string AntiArmorKind = "complete-combat-anti-armor";
    internal const string CancelledAssaultKind = "complete-combat-cancelled-assault";
    internal const string AssaultResultKind = "resolve-combat-assault";
    internal const string NoRetreatKind = "complete-combat-no-retreat";
    internal const string SettlementKind = "settle-combat-world";
    internal const string NoCustodyKind = "complete-combat-no-custody";
    internal const string SuccessorKind = "complete-combat-successor";
    internal const string ReserveReleaseKind = "complete-combat-reserve-release";
    internal const string SecondBreakdownKind = "complete-second-side-breakdown";
    internal const string SecondNoAttackKind = "complete-second-side-combat-no-attack";

    internal CombatExerciseSystemAction(string kind, byte[] eventBytes) : base(kind, eventBytes)
    {
    }
}

// Task 022C: an owner choice from a cut whose Core action IDs are precomputed hashes (Custody,
// Reserve Release, Cycle Control). The kind names the choice; the action ID is the cut's own Core
// action ID, so submission uses the unchanged trusted step. Not served by the Host.
internal sealed record CombatExerciseOwnerChoiceAction : CampaignActionCandidate
{
    internal const string CustodyStage = "custody";
    internal const string ReserveReleaseStage = "reserve-release";
    internal const string CycleControlStage = "cycle-control";
    internal const string TruckConvoyPassStage = "truck-convoy-pass";
    internal const string PostCombatPassStage = "post-combat-pass";
    internal const string SecondMovementStage = "second-movement";
    internal const string SecondMovementCompletionStage = "second-movement-completion";

    internal CombatExerciseOwnerChoiceAction(string stage, string choice, string subjectId, string actionId)
        : base($"combat-{stage}-{choice}", actionId, precomputedActionId: true)
    {
        Stage = stage;
        SubjectId = subjectId;
    }

    internal string Stage { get; }
    internal string SubjectId { get; }
}
