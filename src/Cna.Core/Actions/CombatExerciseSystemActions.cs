namespace Cna.Core.Actions;

// Tasks 022A/022B: System-audience Combat steps offered to the trusted in-process Exercise harness
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

    internal CombatExerciseSystemAction(string kind, byte[] eventBytes) : base(kind, eventBytes)
    {
    }
}
