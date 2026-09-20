using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Pure typed projection of retained Result2 disposition, joint loss and retreat receipts.</summary>
internal static class CampaignCombatLossRetreat
{
    internal static CampaignCombatRetreatDisposition Disposition(CombatResolutionContext context,
        CampaignCombatSettlementState settlement, string kind)
    {
        var required = settlement.Result.RequiredRetreat;
        Require(required == 0 ? kind == "not-required" : kind is "retreat" or "refuse-retreat", "Disposition contradicts resolved retreat.");
        var defender = settlement.PreLossElements.Single(e => e.ElementId == settlement.Defender.ElementId);
        var planned = kind == "retreat" ? 1 : 0;
        var route = planned == 0 ? [defender.CurrentLocationId] : RetreatRoute(context, settlement);
        return new(settlement.SettlementId + ".disposition", settlement.ResultId, kind, required, planned, required - planned, route);
    }
    internal static CampaignCombatLossReceipt Losses(CampaignCombatSettlementState settlement)
    {
        var disposition = settlement.Disposition ?? throw new JsonException("Losses require disposition.");
        var roles = new List<CampaignCombatRoleLoss>();
        foreach (var role in new[] { "attacker", "defender" })
        {
            var unit = role == "attacker" ? settlement.Attacker : settlement.Defender;
            var element = settlement.PreLossElements.Single(e => e.ElementId == unit.ElementId);
            var component = element.Components.Single();
            var percent = role == "attacker" ? settlement.Result.AttackerPercent : settlement.Result.DefenderPercent;
            var refusal = role == "defender" ? 10 * disposition.UnfulfilledDistance : 0;
            var numerator = checked(component.CurrentToe * (percent + refusal));
            var loss = role == "attacker" ? (numerator + 99) / 100 : numerator / 100;
            var captured = role == settlement.Result.CapturedRole ? (loss * settlement.Result.CaptureShare + 99) / 100 : 0;
            roles.Add(new(role, new(unit, component.ComponentId), component.CurrentToe, percent, refusal,
                loss, captured, loss - captured, component.CurrentToe - loss, loss >= 3 ? 3 : 0));
        }
        return new(settlement.SettlementId + ".losses", disposition.ReceiptId, roles);
    }
    internal static CampaignCombatRetreatReceipt Retreat(CampaignCombatSettlementState settlement)
    {
        var disposition = settlement.Disposition ?? throw new JsonException("Retreat requires disposition.");
        var before = settlement.PreLossElements.Single(e => e.ElementId == settlement.Defender.ElementId).OperationalState.CapabilityPointsExpended;
        var after = new CapabilityPointAmount(checked(before.Numerator + disposition.PlannedDistance), 1);
        return new(settlement.SettlementId + ".retreat", settlement.Losses?.ReceiptId ?? throw new JsonException("Retreat requires joint losses."),
            disposition.Kind, disposition.Route, disposition.PlannedDistance, before, after,
            checked((int)(Math.Max(0, after.Numerator - 10) - Math.Max(0, before.Numerator - 10))), disposition.PlannedDistance == 1 ? 3 : 0);
    }

    internal static CampaignWorldSnapshotV7 Project(CombatResolutionContext context, CombatAssaultResult result,
        CampaignCombatSettlementState retained)
    {
        var paid = context.Committed.World; var selection = context.Committed.Base.Steps.Selection!;
        var expected = CampaignCombatSettlementState.CreateResolvedResultV2(context.Committed.CommitmentId!, result.ResultId,
            context.Committed.Base.Boundary.Cycle.GameTurn, context.Committed.Base.Boundary.Cycle.OperationStage,
            selection.Attacker.Unit, selection.Defender.Unit, paid.Elements, result.Facts);
        if (retained.Disposition is { } disposition) expected = expected.WithResultV2Disposition(Disposition(context, expected, disposition.Kind));
        if (retained.Losses is not null) expected = expected.WithResultV2Losses(Losses(expected));
        if (retained.Retreat is not null) expected = expected.WithResultV2Retreat(Retreat(expected));
        Require(expected == retained, "Settlement differs from original result and causal typed receipts.");
        var elements = paid.Elements.ToDictionary(e => e.ElementId, StringComparer.Ordinal);
        var causes = paid.CohesionCauses.ToList(); var lots = paid.CustodyLots.ToList();
        void Cause(string elementId, string receipt, string kind, int points)
        {
            if (points == 0) return;
            var element = elements[elementId]; var prior = element.OperationalState.CohesionLevel;
            var after = kind == "assault-victory-rp" ? Math.Min(10, checked(prior + points)) : checked(prior - points);
            var side = elementId == expected.Attacker.ElementId ? expected.Attacker.OriginalSide : expected.Defender.OriginalSide;
            causes.Add(new(expected.SettlementId + "." + kind + "." + side, causes.Count + 1, receipt, elementId,
                expected.GameTurn, expected.OperationStage, kind, points, prior, after));
            elements[elementId] = WithEffects(element, cohesion: after);
        }
        if (expected.Losses is { } losses)
        {
            foreach (var role in losses.Roles)
            {
                var id = role.Component.Unit.ElementId;
                elements[id] = WithEffects(elements[id], toe: role.RemainingToe);
                Cause(id, losses.ReceiptId, "loss-dp", role.LossDp);
                if (role.CapturedToe > 0)
                {
                    var original = expected.PreLossElements.Single(e => e.ElementId == id);
                    lots.Add(new(expected.SettlementId + ".captives", losses.ReceiptId, role.Component,
                        role.Role == "attacker" ? expected.Defender : expected.Attacker, role.CapturedToe,
                        original.CurrentLocationId, original.CurrentLocationId, "pending", null, null));
                }
            }
        }
        if (expected.Retreat is { } retreat)
        {
            var defender = elements[expected.Defender.ElementId];
            if (retreat.CompletedDistance == 1)
            {
                var charge = CampaignCombatSpending.ChargeMandatoryRetreat(defender.OperationalState, 10, defender.ElementId,
                    retreat.ReceiptId, expected.SettlementId + ".retreat-excess-dp." + expected.Defender.OriginalSide, causes);
                Require(charge.State.CapabilityPointsExpended == retreat.AfterCp && (charge.Cause?.Points ?? 0) == retreat.ExcessCpDp,
                    "Mandatory retreat ledger differs from receipt.");
                elements[defender.ElementId] = WithEffects(defender, retreat.Route[^1], charge.State.CapabilityPointsExpended, charge.State.CohesionLevel);
                causes = charge.Causes.ToList();
            }
            Cause(expected.Attacker.ElementId, retreat.ReceiptId, "assault-victory-rp", retreat.AttackerVictoryRp);
        }
        var representations = paid.Representations.Select(r => new CampaignMapRepresentationState(r.RepresentationId,
            elements[r.BoundElementIds.Single()].CurrentLocationId, r.BindingKind, r.BoundElementIds));
        return new(7, paid.CreationBinding, elements.Values, representations, paid.BrokenVehicleLots, causes,
            paid.Relationships, lots, paid.Guards, paid.ReplacementEntitlements, paid.FutureObligations, [expected]);
    }
    private static CampaignElementStateV6 WithEffects(CampaignElementStateV6 element, string? location = null,
        CapabilityPointAmount? cp = null, int? cohesion = null, int? toe = null)
    {
        var ledger = element.OperationalState; var component = element.Components.Single();
        return new(element.ElementId, location ?? element.CurrentLocationId, element.ReserveStatus,
            new(ledger.LedgerGameTurn, ledger.LedgerOperationStage, cp ?? ledger.CapabilityPointsExpended, cohesion ?? ledger.CohesionLevel,
                ledger.VehicleBreakdownState, ledger.MovementEnded, ledger.InitialLedgerOrigin),
            [new CampaignComponentToeState(component.ComponentId, toe ?? component.CurrentToe, component.InitialToeOrigin)],
            element.SourceParentFormationId, element.CurrentParentFormationId, element.Ammunition, element.Readiness);
    }
    private static string[] RetreatRoute(CombatResolutionContext context, CampaignCombatSettlementState settlement)
    {
        var content = context.Creation.Setup.Artifact.Definition;
        var graph = content.Locations.ToDictionary(l => l.LocationId, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var edge in content.Edges) { graph[edge.FirstLocationId].Add(edge.SecondLocationId); graph[edge.SecondLocationId].Add(edge.FirstLocationId); }
        var defender = settlement.PreLossElements.Single(e => e.ElementId == settlement.Defender.ElementId).CurrentLocationId;
        var attacker = settlement.PreLossElements.Single(e => e.ElementId == settlement.Attacker.ElementId).CurrentLocationId;
        var anchor = context.Creation.Setup.Scenario.RetreatSupplyAnchors.Single(a => a.SideId == settlement.Defender.OriginalSide).LocationId;
        var route = Path(defender, anchor);
        Require(route.Length >= 2 && route[1] != attacker && Path(attacker, route[1]).Length == 3, "Unsupported actual retreat geometry.");
        return [defender, route[1]];
        string[] Path(string start, string end)
        {
            var queue = new Queue<string[]>(); queue.Enqueue([start]); var seen = new HashSet<string>(StringComparer.Ordinal) { start };
            while (queue.TryDequeue(out var path))
            {
                if (path[^1] == end) return path;
                foreach (var next in graph[path[^1]]) if (seen.Add(next)) queue.Enqueue([.. path, next]);
            }
            throw new JsonException("No certified retreat route.");
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new JsonException(message); }
}
