using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Actual empty G2 admission and separate dormant certification of independently trusted positive facts.</summary>
internal static class CampaignCombatCertification
{
    public static CampaignCombatAdmissionBoundary Admit(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> retainedCreated, IReadOnlyList<byte[]> events)
    {
        var result = CampaignCombatHistoryReplay.Replay(request, retainedCreated, events);
        if (result.Projection is not CampaignCombatHistoryProjection.BreakdownCompletion { State: var entry } ||
            entry.Completion is null || entry.BreakdownCompletionReceiptId is null ||
            entry.SequencePosition.PositionId != "land.position.operation-1.first-player.movement-and-combat.combat.position-determination" ||
            entry.SequencePosition.ActiveSide is not null || entry.Lifecycle.BreakdownFlow is not CampaignBreakdownFlow.Idle ||
            entry.Lifecycle.InterruptContext is not null || entry.Lifecycle.MovementEnd is null)
            throw new JsonException("Inherited Combat assessment requires actual completed G2 authority.");
        var movement = entry.Lifecycle.Movement;
        var cycle = movement.Opening.Cycle!;
        if (cycle.GameTurn != 1 || cycle.OperationStage != 1 || cycle.PlayerPhaseSlot != "first-acting-side" || cycle.Ordinal != 1 ||
            movement.Events.Count is not (6 or 7))
            throw new JsonException("Unsupported inherited Combat assessment scope.");
        var actingSide = CampaignSnapshotSerializer.FormatSide(cycle.ActingSide);
        var defendingSide = actingSide == "axis" ? "commonwealth" : "axis";
        var content = request.Context.Setup.Artifact.Definition;
        var acting = BindParticipant(request, movement.World, new CampaignCombatUnitKey(request.CreationBinding,
            actingSide, content.Elements.Single(element => element.SideId == actingSide).ElementId));
        var defending = BindParticipant(request, movement.World, new CampaignCombatUnitKey(request.CreationBinding,
            defendingSide, content.Elements.Single(element => element.SideId == defendingSide).ElementId));
        var actingCp = movement.World.Elements.Single(element => element.ElementId == acting.Unit.ElementId).OperationalState.CapabilityPointsExpended;
        var defendingCp = movement.World.Elements.Single(element => element.ElementId == defending.Unit.ElementId).OperationalState.CapabilityPointsExpended;
        var weather = movement.Opening.Predecessor.Stage.Weather.Weather;
        var normal = weather.Count > 0 && weather.All(value => value.Kind == WeatherKind.Normal);
        var adjacent = content.Edges.Any(edge => edge.FirstLocationId == acting.LocationId && edge.SecondLocationId == defending.LocationId ||
            edge.FirstLocationId == defending.LocationId && edge.SecondLocationId == acting.LocationId);
        var actingWithin = actingCp.Denominator == 1 && actingCp.Numerator <= 5;
        var defendingWithin = defendingCp.Denominator == 1 && defendingCp.Numerator <= 7;
        // Supported absence is proven by replay and all frozen predicates, never inferred from an
        // unsupported candidate implementation or a caller-supplied empty collection.
        if (actingCp.Denominator != 1 || actingCp.Numerator is not (12 or 14) ||
            !normal || adjacent || actingWithin || !defendingWithin)
            throw new JsonException("History is outside inherited-selection-v1 empty admission.");
        return new(result.History, entry, new(acting, defending, normal, adjacent, actingCp, defendingCp, actingWithin, defendingWithin));
    }

    private sealed record SupportCatalogue(CombatSelectedResult[] Results, int MoralePairs, int JointCoordinates, int ResolvedCases);
    private static readonly Lazy<SupportCatalogue> Support = new(CreateSupportCatalogue);
    internal static int DistinctSupportResultCount => Support.Value.Results.Length;
    internal static (int MoralePairs, int JointCoordinates, int ResolvedCases) SupportCoverage =>
        (Support.Value.MoralePairs, Support.Value.JointCoordinates, Support.Value.ResolvedCases);

    /// <summary>
    /// Certify caller-trusted C3a facts only, not a positive history or an admitted opportunity.
    /// Caller authenticates complete history, Breakdown/position/order and applicable Weather receipt.
    /// Synthetic probes must retain that provenance. No current G2 history supplies positive facts.
    /// </summary>
    public static CampaignCombatCandidate? CertifyInitialProfileFacts(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> retainedCreated, CampaignWorldSnapshotV7 currentWorld, CampaignCombatCycleAuthority cycle,
        LandSide firstActingSide, WeatherKind attackerApplicableWeather, WeatherKind defenderApplicableWeather)
    {
        ArgumentNullException.ThrowIfNull(request); ArgumentNullException.ThrowIfNull(currentWorld); ArgumentNullException.ThrowIfNull(cycle);
        var initial = CampaignCreatedV11Serializer.Deserialize(retainedCreated, request).InitialWorld;
        var setup = request.Context.Setup;
        if (!Enum.IsDefined(firstActingSide) || !Enum.IsDefined(attackerApplicableWeather) || !Enum.IsDefined(defenderApplicableWeather) ||
            cycle.ContractVersion != 1 || cycle.CampaignId != request.CampaignId || cycle.RulesetHash != request.Context.RulesetHash ||
            cycle.SetupId != setup.SetupId || cycle.SetupHash != setup.SetupHash || cycle.ContentPackId != setup.Artifact.Identity.PackId ||
            cycle.ContentHash != setup.Artifact.Identity.Hash || cycle.ScenarioId != setup.Scenario.ScenarioId ||
            cycle.AdmittedPolicyBundleDigest != request.Context.Configuration.Hash || cycle.GameTurn != 1 || cycle.OperationStage != 1 ||
            cycle.PlayerPhaseSlot != "first-acting-side" || cycle.Ordinal != 1 || cycle.ActingSide != firstActingSide || cycle.OpenedAuthorityVersion < 1)
            throw new JsonException("Unsupported or foreign positive profile scope.");
        try { _ = ContentContractGuards.RequireSha256(cycle.OpeningPrefix, nameof(cycle)); }
        catch (ArgumentException error) { throw new JsonException("Invalid cycle prefix.", error); }
        foreach (var element in currentWorld.Elements)
            if (element.OperationalState.CapabilityPointsExpended is not { Denominator: 1, Numerator: >= 0 and <= 10 })
                throw new JsonException("Initial profile requires integral current CP0..10.");
        // Compare all typed state/provenance except current CP. Comparison-only element copies
        // grant no authority; original current CP is used below for every cost support check.
        if (currentWorld.ContractVersion != initial.ContractVersion || currentWorld.CreationBinding != initial.CreationBinding ||
            !currentWorld.Elements.Select(element => WithOperational(element, Operational(element.OperationalState, new(0, 1), element.OperationalState.CohesionLevel))).SequenceEqual(initial.Elements) ||
            !currentWorld.Representations.SequenceEqual(initial.Representations) || !currentWorld.BrokenVehicleLots.SequenceEqual(initial.BrokenVehicleLots) ||
            !currentWorld.CohesionCauses.SequenceEqual(initial.CohesionCauses) || !currentWorld.Relationships.SequenceEqual(initial.Relationships) ||
            !currentWorld.CustodyLots.SequenceEqual(initial.CustodyLots) || !currentWorld.Guards.SequenceEqual(initial.Guards) ||
            !currentWorld.ReplacementEntitlements.SequenceEqual(initial.ReplacementEntitlements) || !currentWorld.FutureObligations.SequenceEqual(initial.FutureObligations) ||
            !currentWorld.Settlements.SequenceEqual(initial.Settlements)) throw new JsonException("Current facts differ from certified initial infantry profile.");
        var side = CampaignSnapshotSerializer.FormatSide(firstActingSide);
        var other = side == "axis" ? "commonwealth" : "axis";
        var attacker = BindParticipant(request, currentWorld, new(request.CreationBinding, side, setup.Artifact.Definition.Elements.Single(e => e.SideId == side).ElementId));
        var defender = BindParticipant(request, currentWorld, new(request.CreationBinding, other, setup.Artifact.Definition.Elements.Single(e => e.SideId == other).ElementId));
        var a = currentWorld.Elements.Single(e => e.ElementId == attacker.Unit.ElementId);
        var d = currentWorld.Elements.Single(e => e.ElementId == defender.Unit.ElementId);
        ProveSupport(request, a, d, other);
        if (attackerApplicableWeather != WeatherKind.Normal || defenderApplicableWeather != WeatherKind.Normal ||
            a.OperationalState.CapabilityPointsExpended.Numerator > 5 || d.OperationalState.CapabilityPointsExpended.Numerator > 7) return null;
        return new(attacker, defender, defender.LocationId, "voluntary-adjacent");
    }

    private static SupportCatalogue CreateSupportCatalogue()
    {
        var coordinates = Enumerable.Range(1, 6).SelectMany(tens => Enumerable.Range(1, 6).Select(ones => tens * 10 + ones)).ToArray();
        var representatives = new Dictionary<int, (int Attacker, int Defender)>(); var moralePairs = 0;
        foreach (var attacker in coordinates) foreach (var defender in coordinates)
        {
            representatives.TryAdd(Cna1979CombatAdjudication.CalculateDifferential(attacker, defender), (attacker, defender)); moralePairs++;
        }
        if (!representatives.Keys.Order().SequenceEqual(Enumerable.Range(-2, 5))) throw new JsonException("Unsupported morale surface.");
        var results = new List<CombatSelectedResult>(); var joint = 0;
        foreach (var (differential, morale) in representatives.OrderBy(pair => pair.Key))
        {
            var effects = Cna1979CombatAdjudication.Definition.Effects.Single(value => value.Differential == differential);
            foreach (var ac in coordinates) foreach (var dc in coordinates)
            {
                joint++;
                // Flags select legal Resolve arguments only; Resolve owns all arithmetic.
                var attackerCapture = effects.AttackerCaptureSums.Contains(ac / 10 + ac % 10);
                var defenderCapture = effects.DefenderCaptureSums.Contains(dc / 10 + dc % 10);
                if (attackerCapture && defenderCapture) throw new JsonException("Simultaneous capture is unsupported.");
                var retreat = effects.DefenderRetreatOneHexSums.Contains(dc / 10 + dc % 10);
                var capture = attackerCapture || defenderCapture;
                for (var refusal = 0; refusal <= (retreat ? 1 : 0); refusal++)
                    for (var die = 1; die <= (capture ? 6 : 1); die++)
                        results.Add(Cna1979CombatAdjudication.Resolve(morale.Attacker, morale.Defender, ac, dc, refusal == 1, capture ? die : null));
            }
        }
        return new(results.Distinct().ToArray(), moralePairs, joint, results.Count);
    }

    private static void ProveSupport(CampaignCombatCreationRequest request, CampaignElementStateV6 attacker,
        CampaignElementStateV6 defender, string defenderSide)
    {
        var rules = Cna1979CombatAdjudication.Definition; var content = request.Context.Setup.Artifact.Definition;
        var graph = content.Locations.ToDictionary(location => location.LocationId, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var edge in content.Edges) { graph[edge.FirstLocationId].Add(edge.SecondLocationId); graph[edge.SecondLocationId].Add(edge.FirstLocationId); }
        if (!graph[attacker.CurrentLocationId].Contains(defender.CurrentLocationId) || content.Locations.Any(l => l.TerrainId != "land.terrain.clear"))
            throw new JsonException("Unsupported initial assault geometry.");
        var footCost = Cna1979Movement.LookupTerrain("land.terrain.clear", Cna1979Movement.NonMotorizedMobilityId);
        if (!footCost.IsSupported || footCost.Value.Cost.Denominator != 1) throw new JsonException("Unsupported Clear reunion cost.");
        var anchor = request.Context.Setup.Scenario.RetreatSupplyAnchors.Single(value => value.SideId == defenderSide).LocationId;
        var retreats = graph[defender.CurrentLocationId].Where(location => location != attacker.CurrentLocationId &&
            Path(attacker.CurrentLocationId, location).Length > Path(attacker.CurrentLocationId, defender.CurrentLocationId).Length &&
            Path(location, anchor).Length < Path(defender.CurrentLocationId, anchor).Length).ToArray();
        if (retreats.Length != 1) throw new JsonException("Retreat requires one approved Clear destination.");
        // Scratch arithmetic only: no cost receipt or changed world leaves certification.
        var paidA = CampaignCombatSpending.ChargeOrdinary(attacker.OperationalState, new(rules.Costs.AttackerCapabilityPoints, 1),
            rules.Costs.BaseCapabilityPointAllowance, CampaignCombatSpendCeiling.Ordinary, attacker.ElementId, "proof.attacker", []).State;
        var paidD = CampaignCombatSpending.ChargeOrdinary(defender.OperationalState, new(rules.Costs.DefenderCapabilityPoints, 1),
            rules.Costs.BaseCapabilityPointAllowance, CampaignCombatSpendCeiling.Ordinary, defender.ElementId, "proof.defender", []).State;
        foreach (var result in Support.Value.Results)
        {
            foreach (var loss in new[] { result.Attacker, result.Defender })
                if (loss.RemainingToe < 7 || loss.CapturedLoss is < 0 or > 3 || loss.DestroyedLoss < 0 ||
                    loss.CapturedLoss + loss.DestroyedLoss != loss.TotalLoss || loss.RemainingToe + loss.TotalLoss != rules.Costs.CommittedToePerRole)
                    throw new JsonException("Result exceeds bounded casualty/custody support.");
            if (result.DefenderRetreatHexes is < 0 or > 1) throw new JsonException("Unsupported retreat distance.");
            _ = Operational(paidA, paidA.CapabilityPointsExpended, checked(paidA.CohesionLevel - result.Attacker.LossDp));
            var afterLossD = Operational(paidD, paidD.CapabilityPointsExpended, checked(paidD.CohesionLevel - result.Defender.LossDp));
            var retreat = result.DefenderRetreatHexes == 1 && !result.RefusedRetreat;
            var defenderFinal = retreat ? retreats[0] : defender.CurrentLocationId;
            if (retreat) _ = CampaignCombatSpending.ChargeMandatoryRetreat(afterLossD, rules.Costs.BaseCapabilityPointAllowance,
                defender.ElementId, "proof.retreat", "proof.retreat.dp", []);
            foreach (var capturedRole in new[] { CombatRole.Attacker, CombatRole.Defender })
            {
                var captured = capturedRole == CombatRole.Attacker ? result.Attacker : result.Defender;
                if (captured.CapturedLoss == 0) continue;
                var captor = capturedRole == CombatRole.Attacker ? result.Defender : result.Attacker;
                var origin = capturedRole == CombatRole.Attacker ? attacker.CurrentLocationId : defender.CurrentLocationId;
                var captorFinal = capturedRole == CombatRole.Attacker ? defenderFinal : attacker.CurrentLocationId;
                var victimFinal = capturedRole == CombatRole.Attacker ? attacker.CurrentLocationId : defenderFinal;
                var guarded = Path(origin, captorFinal);
                if (guarded.Length - 1 > rules.Settlement.GuardedPathMaximumHexes || guarded.Skip(1).Contains(victimFinal) ||
                    captor.RemainingToe <= rules.Settlement.GuardToe || captured.CapturedLoss > 5 * rules.Settlement.GuardToe ||
                    checked((Path(origin, victimFinal).Length - 1) * footCost.Value.Cost.Numerator) > rules.Settlement.EscapePathMaximumCapabilityPoints)
                    throw new JsonException("Result lacks legal guarded relocation or escape reunion.");
            }
        }
        // Content7 proves the public two-unit Clear line and empty ZOC for every allowed world.
        // Its unique paths exclude hidden blockers; no visibility filter supplies that proof.
        string[] Path(string start, string end)
        {
            var queue = new Queue<string[]>(); queue.Enqueue([start]); var seen = new HashSet<string>(StringComparer.Ordinal) { start };
            while (queue.TryDequeue(out var path))
            {
                if (path[^1] == end) return path;
                foreach (var next in graph[path[^1]]) if (seen.Add(next)) queue.Enqueue([.. path, next]);
            }
            throw new JsonException("Missing bounded route.");
        }
    }

    private static CampaignElementOperationalStateV6 Operational(CampaignElementOperationalStateV6 source, CapabilityPointAmount cp, int cohesion) =>
        new(source.LedgerGameTurn, source.LedgerOperationStage, cp, cohesion, source.VehicleBreakdownState, source.MovementEnded, source.InitialLedgerOrigin);
    private static CampaignElementStateV6 WithOperational(CampaignElementStateV6 source, CampaignElementOperationalStateV6 operational) =>
        new(source.ElementId, source.CurrentLocationId, source.ReserveStatus, operational, source.Components, source.SourceParentFormationId,
            source.CurrentParentFormationId, source.Ammunition, source.Readiness);

    /// <summary>Bind current identity in caller-trusted World; this does not authenticate history or admit combat.</summary>
    public static CampaignCombatParticipant BindParticipant(CampaignCombatCreationRequest request,
        CampaignWorldSnapshotV7 world, CampaignCombatUnitKey unit)
    {
        ArgumentNullException.ThrowIfNull(request); ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(unit);
        var content = request.Context.Setup.Artifact.Definition;
        var source = content.Elements.SingleOrDefault(element => element.ElementId == unit.ElementId);
        var element = world.Elements.SingleOrDefault(value => value.ElementId == unit.ElementId);
        if (unit.CreationBinding != request.CreationBinding || world.CreationBinding != request.CreationBinding ||
            source is null || element is null || source.SideId != unit.OriginalSide ||
            source.ParentFormationId != element.SourceParentFormationId ||
            !element.Components.Select(component => component.ComponentId).SequenceEqual(source.Components.Select(component => component.ComponentId), StringComparer.Ordinal) ||
            !content.Locations.Any(location => location.LocationId == element.CurrentLocationId))
            throw new JsonException("Participant unit/component provenance differs from trusted current context.");
        var representations = world.Representations.Where(value => value.BoundElementIds.Contains(unit.ElementId, StringComparer.Ordinal)).ToArray();
        if (representations.Length != 1 || representations[0].BindingKind != CampaignMapRepresentationBindingKind.IndependentElement ||
            representations[0].BoundElementIds.Count != 1 || representations[0].CurrentLocationId != element.CurrentLocationId)
            throw new JsonException("Participant requires one exact current independent representation.");
        return new(unit, representations[0].RepresentationId, element.CurrentLocationId, element.Components.Select(component => component.ComponentId).ToArray());
    }
}
