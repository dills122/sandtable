namespace Cna.Core.Actions;

// Task 022A: System-audience Combat steps offered to the trusted in-process Exercise harness
// (CampaignExercises.QueryWithCombat). The action ID is SHA-256 over the step's own canonical event
// bytes, which is exactly the receipt action ID the existing Core step already reports, so the
// harness's receipt/action binding holds without a new identity scheme. Not served by the Host and
// not Combat activation.
internal sealed record CompleteCombatNoAttackAction : CampaignActionCandidate
{
    internal const string ActionKind = "complete-combat-no-attack";

    internal CompleteCombatNoAttackAction(byte[] eventBytes) : base(ActionKind, eventBytes)
    {
    }
}

internal sealed record CompleteCombatBarrageAction : CampaignActionCandidate
{
    internal const string ActionKind = "complete-combat-barrage";

    internal CompleteCombatBarrageAction(byte[] eventBytes) : base(ActionKind, eventBytes)
    {
    }
}
