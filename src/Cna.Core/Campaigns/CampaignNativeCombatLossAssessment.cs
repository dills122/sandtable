using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Shared source arithmetic for the normal post-result owner retreat and World
// settlement steps. The choice changes only the defender's refusal loss.
internal sealed record CampaignNativeCombatLossAssessment(
    CombatRoleLoss Attacker, CombatRoleLoss Defender,
    bool RetreatRequired, bool RefusedRetreat)
{
    internal static CampaignNativeCombatLossAssessment Project(
        CampaignNativeCombatAssaultResultV22 result,
        CampaignNativeCombatAssaultInputs inputs,
        bool refuseRetreat)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(inputs);
        if (result.PaidWorldHash != inputs.PaidWorldHash
            || result.AttackerElementId != inputs.Attacker.ElementId
            || result.DefenderElementId != inputs.Defender.ElementId
            || inputs.Attacker.CommittedToe != Cna1979CombatAdjudication.Definition.Costs.CommittedToePerRole
            || inputs.Defender.CommittedToe != Cna1979CombatAdjudication.Definition.Costs.CommittedToePerRole)
            throw new InvalidOperationException("Loss assessment requires the paid normal assault cut.");
        var selected = Cna1979CombatAdjudication.Resolve(
            result.AttackerMoraleCoordinate, result.DefenderMoraleCoordinate,
            result.Facts.AttackerCoordinate, result.Facts.DefenderCoordinate,
            refuseRetreat, result.Facts.CaptureDie);
        if (selected.Differential != result.Facts.Differential
            || selected.AttackerLossPercent != result.Facts.AttackerPercent
            || selected.DefenderBaseLossPercent != result.Facts.DefenderPercent
            || selected.DefenderRetreatHexes != result.Facts.RequiredRetreat
            || selected.RawEngaged != result.Facts.RawEngaged
            || selected.CaptureSharePercent.GetValueOrDefault() != result.Facts.CaptureShare)
            throw new InvalidOperationException("Loss assessment differs from sealed chart facts.");
        return new(selected.Attacker, selected.Defender,
            selected.DefenderRetreatHexes == 1, selected.RefusedRetreat);
    }
}
