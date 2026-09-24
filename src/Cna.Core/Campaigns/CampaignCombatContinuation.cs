using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CombatMovementEndLocation(CampaignCombatUnitKey Unit, string LocationId);
internal sealed class CombatContinuationMovementEnd(CombatReleaseScope scope, int ordinal, string completionReceiptId,
    IEnumerable<CombatMovementEndLocation> locations, IEnumerable<CampaignCombatUnitKey> excludedBefore)
{
    public CombatReleaseScope Scope { get; } = scope;
    public int Ordinal { get; } = ordinal;
    public string CompletionReceiptId { get; } = completionReceiptId;
    public IReadOnlyList<CombatMovementEndLocation> Locations { get; } = Array.AsReadOnly(locations.ToArray());
    public IReadOnlyList<CampaignCombatUnitKey> ExcludedBefore { get; } = Array.AsReadOnly(excludedBefore.ToArray());
}
internal sealed record CombatContinuationWitness(CampaignCombatUnitKey Unit, string DestinationLocationId,
    int TerrainCost, int BreakOffCost, long AfterCp, int ExcessCpDp, bool UsesReleaseException);
internal sealed class CombatContinuationAssessment(string releaseReceipt, string movementReceipt,
    IEnumerable<CombatContinuationWitness> witnesses)
{
    public string ReleaseCompletionReceiptId { get; } = releaseReceipt;
    public string MovementCompletionReceiptId { get; } = movementReceipt;
    public string CombatAssessment { get; } = "no-own-ammunition";
    public IReadOnlyList<CombatContinuationWitness> Witnesses { get; } = Array.AsReadOnly(witnesses.ToArray());
}

/// <summary>Pure mechanism over independently admitted history projections. Does not authenticate history or authorize repeat.</summary>
internal static class CampaignCombatContinuation
{
    public static CombatContinuationAssessment AssessTrustedBoundary(ContentPackV7Definition pack,
        CampaignCombatCycleAuthority cycle, CampaignWorldSnapshotV7 world, CombatReleaseState release,
        CombatContinuationMovementEnd movementEnd, WeatherKind weather)
    {
        ArgumentNullException.ThrowIfNull(pack); ArgumentNullException.ThrowIfNull(cycle);
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(movementEnd);
        Require(weather == WeatherKind.Normal && cycle.ContractVersion == 1 && cycle.Ordinal >= 1 &&
            cycle.GameTurn is >= 1 and <= 111 && cycle.OperationStage is >= 1 and <= 3 &&
            Enum.IsDefined(cycle.ActingSide) && cycle.PlayerPhaseSlot is "first-acting-side" or "second-acting-side",
            "Unsupported continuation scope or Weather.");
        Require(release.Status == "completed" && release.Pending.Count == 0 && release.CompletionReceiptId is not null &&
            world.Settlements.All(s => s.Disposition is not null && s.Losses is not null && s.Retreat is not null && s.Relationships is not null),
            "Continuation requires completed Release and settlement.");
        ContentContractGuards.RequireStableId(release.CompletionReceiptId!, nameof(release));
        ContentContractGuards.RequireStableId(movementEnd.CompletionReceiptId, nameof(movementEnd));
        var scope = new CombatReleaseScope(cycle.GameTurn, cycle.OperationStage, cycle.PlayerPhaseSlot, cycle.ActingSide);
        var side = CampaignSnapshotSerializer.FormatSide(cycle.ActingSide);
        Require(pack.Elements.Count == 2 && world.Elements.Count == 2 && pack.Locations.Count is > 0 and <= 512 &&
            pack.Edges.Count <= 512 && world.CohesionCauses.Count <= 512 && world.BrokenVehicleLots.Count == 0,
            "Unsupported continuation inventory.");
        var graph = pack.Locations.ToDictionary(l => l.LocationId, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        Require(pack.Locations.All(l => l.TerrainId == "land.terrain.clear"), "Unsupported continuation terrain.");
        foreach (var edge in pack.Edges)
        {
            Require(edge.Features.Count == 0 && graph.ContainsKey(edge.FirstLocationId) && graph.ContainsKey(edge.SecondLocationId) &&
                edge.FirstLocationId != edge.SecondLocationId && graph[edge.FirstLocationId].Add(edge.SecondLocationId), "Unsupported continuation edge.");
            graph[edge.SecondLocationId].Add(edge.FirstLocationId);
        }
        var units = new List<CampaignCombatUnitKey>();
        foreach (var e in world.Elements)
        {
            var f = pack.Elements.SingleOrDefault(f => f.ElementId == e.ElementId);
            Require(f is not null && f.BaseCapabilityPointAllowance == 10 && f.MobilityId == Cna1979Movement.NonMotorizedMobilityId &&
                f.PlacementMode == ContentPlacementMode.Independent && f.CombatClassificationId == "land.combat-classification.combat-unit" &&
                f.Components.Count == 1 && f.Components[0].ComponentClassId == "land.combat-component.infantry" &&
                e.Components.Count == 1 && e.Components[0].ComponentId == f.Components[0].ComponentId && e.Components[0].CurrentToe <= 10 &&
                graph.ContainsKey(e.CurrentLocationId) && e.OperationalState.VehicleBreakdownState is null && e.OperationalState.MovementEnded is null,
                "Unsupported infantry or control profile.");
            var reps = world.Representations.Where(r => r.BoundElementIds.SequenceEqual([e.ElementId])).ToArray();
            Require(reps.Length == 1 && reps[0].BindingKind == CampaignMapRepresentationBindingKind.IndependentElement && reps[0].CurrentLocationId == e.CurrentLocationId,
                "Continuation representation differs from World.");
            units.Add(new(world.CreationBinding, f!.SideId, e.ElementId));
        }
        var ordered = units.OrderBy(Key, StringComparer.Ordinal).ToArray();
        Require(units.Count(u => u.OriginalSide == side) == 1 && units.Any(u => u.OriginalSide != side) &&
            world.Elements.Select(e => e.CurrentLocationId).Distinct(StringComparer.Ordinal).Count() == world.Elements.Count,
            "Unsupported side coverage or stacked World.");
        Require(movementEnd.Scope == scope && movementEnd.Ordinal == cycle.Ordinal &&
            movementEnd.Locations.Count == ordered.Length && movementEnd.Locations.All(l => l is not null && graph.ContainsKey(l.LocationId)) &&
            movementEnd.Locations.Select(l => l.Unit).SequenceEqual(ordered), "Movement-end proof differs from complete original-unit scope.");
        var excluded = movementEnd.ExcludedBefore;
        Require(excluded.Count <= ordered.Length && excluded.All(u => u is not null && units.Contains(u) && u.OriginalSide == side) &&
            excluded.SequenceEqual(excluded.Distinct().OrderBy(Key, StringComparer.Ordinal)), "Invalid prior exclusion history.");
        var own = ordered.Where(u => u.OriginalSide == side).ToArray();
        Require(release.Members.Count == own.Length && release.Members.All(m => m is not null) &&
            release.Members.Select(m => m.Unit).SequenceEqual(own), "Release must cover every original own unit exactly once.");
        var witnesses = new List<CombatContinuationWitness>();
        foreach (var m in release.Members)
        {
            var e = world.Elements.Single(e => e.ElementId == m.Unit.ElementId); var op = e.OperationalState;
            Require(m.BaseCpa == 10 && m.Status == e.ReserveStatus && m.Status is CampaignElementReserveStatus.None or CampaignElementReserveStatus.ReserveII &&
                m.SpentCp == op.CapabilityPointsExpended && m.SpentCp.Denominator == 1 &&
                op.LedgerGameTurn == cycle.GameTurn && op.LedgerOperationStage == cycle.OperationStage && e.Components[0].CurrentToe > 0,
                "Release member differs from supported World ledger.");
            // Check capability before eligibility/cost: absence of a Movement witness cannot hide armed Combat.
            Require(e.Ammunition.Points == 0, "Armed Combat continuation needs its explicit adapter.");
            var (ceiling, exceptional) = Reserve(m, release, cycle, scope);
            var assessment = CampaignCombatCycleMovementRules.Assess(m.Unit, cycle.GameTurn, cycle.OperationStage, 2, world.Relationships);
            var end = movementEnd.Locations.Single(l => l.Unit == m.Unit);
            var near = movementEnd.Locations.Any(l => l.Unit.OriginalSide != side && WithinTwo(graph, end.LocationId, l.LocationId));
            if (m.Status != CampaignElementReserveStatus.None || (!exceptional && (excluded.Contains(m.Unit) || !near))) continue;
            var limit = ceiling switch { CampaignCombatSpendCeiling.ReleasedReserveI => 10, CampaignCombatSpendCeiling.ReleasedReserveII => 5, _ => 15 };
            var after = checked(m.SpentCp.Numerator + assessment.TotalCost.Numerator);
            if (after > limit) continue; // Only a proved cumulative ceiling failure means no affordable move.
            var charge = CampaignCombatCycleMovementRules.AssessAndCharge(m.Unit, op, 10, ceiling, 2,
                world.Relationships, "continuation.probe", world.CohesionCauses);
            Require(charge.Spending.Causes.Count <= 512, "Continuation Cause capacity exhausted.");
            foreach (var dest in graph[e.CurrentLocationId].Order(StringComparer.Ordinal))
            {
                if (world.Elements.Any(x => x.CurrentLocationId == dest) || world.Guards.Any(g => g.CurrentLocationId == dest)) continue;
                var terrain = Cna1979Movement.LookupTerrain("land.terrain.clear", Cna1979Movement.NonMotorizedMobilityId);
                Require(terrain.IsSupported && terrain.Value.Cost == new CapabilityPointAmount(2, 1), "Unsupported Clear movement cost.");
                witnesses.Add(new(m.Unit, dest, 2, assessment.BreakOffCost, after, charge.Spending.Cause?.Points ?? 0, exceptional));
            }
        }
        return new(release.CompletionReceiptId!, movementEnd.CompletionReceiptId, witnesses);
    }
    private static (CampaignCombatSpendCeiling Ceiling, bool Exceptional) Reserve(CombatReleaseMember m,
        CombatReleaseState release, CampaignCombatCycleAuthority cycle, CombatReleaseScope scope)
    {
        var h = m.History;
        Require(h is not null && h.Scope == scope, "Foreign Reserve history scope.");
        if (h!.ReleasedType is null)
        {
            Require(h.ReleaseReceiptId is null && h.ReleaseCycle is null && h.CpaBasis is null && h.VoluntaryCeiling is null &&
                h.OffensiveCommitmentId is null && h.NextMovement is null &&
                (m.Status == CampaignElementReserveStatus.None ? h.DesignationReceiptId is null && h.ConversionReceiptId is null :
                    h.DesignationReceiptId is not null && h.ConversionReceiptId is not null), "Incomplete unreleased history.");
            return (CampaignCombatSpendCeiling.Ordinary, false);
        }
        Require(m.Status == CampaignElementReserveStatus.None && h.ReleasedType is "I" or "II" &&
            h.DesignationReceiptId is not null && h.ReleaseReceiptId is not null && h.ReleaseCycle >= 1 && h.ReleaseCycle <= cycle.Ordinal &&
            (h.ReleasedType == "I" ? h.ReleaseCycle == 1 && h.ConversionReceiptId is null : h.ReleaseCycle > 1 && h.ConversionReceiptId is not null) &&
            h.CpaBasis == 10 && h.VoluntaryCeiling == (h.ReleasedType == "I" ? 10 : 5), "Invalid released Reserve history.");
        var ex = h.NextMovement;
        Require(ex is not null && ex.Scope == scope && ex.Ordinal == checked(h.ReleaseCycle!.Value + 1), "Invalid next-Movement exception scope.");
        var exceptional = ex!.Status == "pending";
        Require(exceptional ? h.ReleaseCycle == cycle.Ordinal && ex.Ordinal == checked(cycle.Ordinal + 1) && ex.CompletionReceiptId is null :
            ex.Status == "expired" && ex.CompletionReceiptId is not null, "Invalid next-Movement exception lifecycle.");
        if (h.OffensiveCommitmentId is not null)
            Require(release.AttackHistory.Count(a => a.CommitmentId == h.OffensiveCommitmentId && a.Attacker == m.Unit &&
                a.GameTurn == cycle.GameTurn && a.OperationStage == cycle.OperationStage) == 1, "Unbound offensive-use history.");
        return (h.ReleasedType == "I" ? CampaignCombatSpendCeiling.ReleasedReserveI : CampaignCombatSpendCeiling.ReleasedReserveII, exceptional);
    }
    private static bool WithinTwo(Dictionary<string, HashSet<string>> graph, string start, string end) =>
        start == end || graph[start].Contains(end) || graph[start].Any(middle => graph[middle].Contains(end));
    private static string Key(CampaignCombatUnitKey unit) => $"{unit.CreationBinding}/{unit.OriginalSide}/{unit.ElementId}";
    private static void Require(bool condition, string message) { if (!condition) throw new JsonException(message); }
}
