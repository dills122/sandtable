using System.Security.Cryptography;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Pure normal-campaign settlement. The V24 event retains these derived facts;
// replay must derive them again from V22, the accepted V23 choice and paid World.
internal static class CampaignNativeCombatSettledWorldV24
{
    internal sealed record PendingCaptiveLot(
        string VictimElementId, string VictimComponentId,
        string CaptorElementId, int Quantity, string OriginLocationId);

    internal sealed record Projection(
        CampaignWorldSnapshotV6 World,
        CampaignNativeCombatLossAssessment Losses,
        IReadOnlyList<PendingCaptiveLot> PendingCaptives);

    internal static Projection Project(
        CampaignNativeCombatAssaultResultV22 result,
        CampaignNativeCombatRetreatAfterAssaultV23 retreat,
        CampaignNativeCombatAssaultInputs inputs,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(retreat);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(context);
        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException("Normal settlement requires Content 6.");
        var paid = inputs.PaidWorld;
        if (retreat.CampaignId != result.CampaignId
            || retreat.PriorStateVersion != result.StateVersion
            || retreat.DecisionActionId != result.DecisionActionId
            || retreat.ResultHash != "sha256:" + Convert.ToHexStringLower(
                SHA256.HashData(result.SerializeCanonical()))
            || retreat.PaidWorldHash != result.PaidWorldHash
            || result.PaidWorldHash != inputs.PaidWorldHash
            || result.PaidWorldHash != CampaignNativeCombatPaidWorldV22.Hash(paid)
            || retreat.DefenderElementId != result.DefenderElementId
            || retreat.RetreatSourceHash != context.CombatRetreatSource?.Hash
            || !CampaignWorldV6Validator.IsValid(paid, artifact, context.Scenario))
            throw new InvalidOperationException("Settlement differs from sealed normal campaign authority.");

        var required = result.Facts.RequiredRetreat == 1;
        if (required != (retreat.Disposition is "moved" or "refused")
            || (retreat.Disposition == "moved") !=
                (retreat.DestinationLocationId is not null))
            throw new InvalidOperationException("Settlement lacks the required defender choice.");
        var losses = CampaignNativeCombatLossAssessment.Project(result, inputs,
            retreat.Disposition == "refused");
        var rules = Cna1979CombatAdjudication.Definition.Settlement;
        var attacker = paid.Elements.Single(value => value.ElementId == inputs.Attacker.ElementId);
        var defender = paid.Elements.Single(value => value.ElementId == inputs.Defender.ElementId);
        var settled = new Dictionary<string, CampaignElementStateV5>(StringComparer.Ordinal)
        {
            [attacker.ElementId] = ApplyLoss(attacker, inputs.Attacker.ComponentId,
                losses.Attacker, retreat.Disposition == "moved"
                    ? rules.VictoryRpPoints : 0, rules.CohesionUpperCap),
            [defender.ElementId] = ApplyLoss(defender, inputs.Defender.ComponentId,
                losses.Defender, 0, rules.CohesionUpperCap)
        };

        if (retreat.Disposition == "moved")
        {
            var current = settled[defender.ElementId];
            var ledger = current.OperationalState;
            var after = ledger.CapabilityPointsExpended
                + new CapabilityPointAmount(rules.ClearRetreatCapabilityPointsPerHex, 1);
            var allowance = artifact.Definition.LegacyDefinition.Elements.Single(value =>
                value.ElementId == defender.ElementId).BaseCapabilityPointAllowance;
            // World v6 cannot retain CP above its content ceiling. Until its
            // successor carries excess-CP causes, refuse an unrepresentable cut.
            if (after > new CapabilityPointAmount(allowance, 1))
                throw new InvalidOperationException("Retreat exceeds normal World CP capacity.");
            settled[defender.ElementId] = new CampaignElementStateV5(
                current.ElementId, retreat.DestinationLocationId!, current.ReserveStatus,
                new CampaignElementOperationalStateV5(ledger.LedgerGameTurn,
                    ledger.LedgerOperationStage, after, ledger.CohesionLevel,
                    ledger.VehicleBreakdownState, ledger.MovementEnded),
                current.Components);
        }

        var representations = paid.Representations.Select(value =>
            value.BoundElementIds[0] == defender.ElementId
                ? new CampaignMapRepresentationState(value.RepresentationId,
                    settled[defender.ElementId].CurrentLocationId,
                    value.BindingKind, value.BoundElementIds)
                : value);
        var world = new CampaignWorldSnapshotV6(
            CampaignWorldSnapshotV6.CurrentContractVersion,
            paid.Elements.Select(value => settled.GetValueOrDefault(value.ElementId) ?? value),
            representations, paid.BrokenVehicleLots);
        if (!CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario))
            throw new InvalidOperationException("Settlement produced an invalid normal World.");

        var captives = new List<PendingCaptiveLot>();
        AddCaptives(attacker, inputs.Attacker.ComponentId, defender.ElementId,
            losses.Attacker);
        AddCaptives(defender, inputs.Defender.ComponentId, attacker.ElementId,
            losses.Defender);
        return new(world, losses, Array.AsReadOnly(captives.ToArray()));

        void AddCaptives(CampaignElementStateV5 victim, string componentId,
            string captorId, CombatRoleLoss loss)
        {
            if (loss.CapturedLoss > 0)
                captives.Add(new PendingCaptiveLot(victim.ElementId, componentId,
                    captorId, loss.CapturedLoss, victim.CurrentLocationId));
        }
    }

    private static CampaignElementStateV5 ApplyLoss(
        CampaignElementStateV5 element, string componentId,
        CombatRoleLoss loss, int victoryRp, int cohesionCap)
    {
        var component = element.Components.Single(value => value.ComponentId == componentId);
        if (component.CurrentToe != loss.TotalLoss + loss.RemainingToe)
            throw new InvalidOperationException("Combat loss differs from paid World TOE.");
        var ledger = element.OperationalState;
        var cohesion = Math.Min(cohesionCap,
            checked(ledger.CohesionLevel - loss.LossDp + victoryRp));
        return new CampaignElementStateV5(element.ElementId,
            element.CurrentLocationId, element.ReserveStatus,
            new CampaignElementOperationalStateV5(ledger.LedgerGameTurn,
                ledger.LedgerOperationStage, ledger.CapabilityPointsExpended,
                cohesion, ledger.VehicleBreakdownState, ledger.MovementEnded),
            element.Components.Select(value => value.ComponentId == componentId
                ? new CampaignComponentToeState(value.ComponentId,
                    loss.RemainingToe, value.InitialToeOrigin) : value));
    }
}
