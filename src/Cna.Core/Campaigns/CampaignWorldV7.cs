using System.Collections.ObjectModel;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignWorldSnapshotV7
{
    public const int CurrentContractVersion = 7;

    public CampaignWorldSnapshotV7(int contractVersion, string creationBinding,
        IEnumerable<CampaignElementStateV6> elements,
        IEnumerable<CampaignMapRepresentationState> representations,
        IEnumerable<CampaignBrokenVehicleLot> brokenVehicleLots,
        IEnumerable<CampaignCombatCohesionCause> cohesionCauses,
        IEnumerable<CampaignCombatRelationship> relationships,
        IEnumerable<CampaignCombatCustodyLot> custodyLots,
        IEnumerable<CampaignCombatGuardAsset> guards,
        IEnumerable<CampaignCombatReplacementEntitlement> replacementEntitlements,
        IEnumerable<CampaignCombatFutureObligation> futureObligations,
        IEnumerable<CampaignCombatSettlementState> settlements)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(contractVersion, CurrentContractVersion);
        ContractVersion = contractVersion;
        CreationBinding = ContentContractGuards.RequireStableId(creationBinding, nameof(creationBinding));
        Elements = CopyUnique(elements, value => value.ElementId, nameof(elements));
        Representations = CopyUnique(representations, value => value.RepresentationId, nameof(representations));
        BrokenVehicleLots = CopyUnique(brokenVehicleLots, value => value.LotId, nameof(brokenVehicleLots));
        var causes = ContentContractGuards.CopyValues(cohesionCauses, nameof(cohesionCauses));
        if (causes.Select(value => value.CauseId).Distinct(StringComparer.Ordinal).Count() != causes.Length)
            throw new ArgumentException("Cause IDs must be unique.", nameof(cohesionCauses));
        CohesionCauses = Array.AsReadOnly(causes.OrderBy(value => value.Ordinal).ToArray());
        Relationships = CopyUnique(relationships, value => value.RelationId, nameof(relationships));
        CustodyLots = CopyUnique(custodyLots, value => value.LotId, nameof(custodyLots));
        Guards = CopyUnique(guards, value => value.GuardId, nameof(guards));
        ReplacementEntitlements = CopyUnique(replacementEntitlements, value => value.EntitlementId, nameof(replacementEntitlements));
        FutureObligations = CopyUnique(futureObligations, value => value.ObligationId, nameof(futureObligations));
        Settlements = CopyUnique(settlements, value => value.SettlementId, nameof(settlements));
        if (Representations.Count != Elements.Count ||
            Representations.SelectMany(value => value.BoundElementIds).Order(StringComparer.Ordinal)
                .SequenceEqual(Elements.Select(value => value.ElementId).Order(StringComparer.Ordinal)) == false)
            throw new ArgumentException("Representations must cover each element once.", nameof(representations));
        var elementById = Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var locations = Elements.ToDictionary(value => value.ElementId, value => value.CurrentLocationId, StringComparer.Ordinal);
        if (Representations.Any(value => value.CurrentLocationId != locations[value.BoundElementIds[0]]))
            throw new ArgumentException("Representation and element locations differ.", nameof(representations));
        if (BrokenVehicleLots.Count != 0)
            throw new ArgumentException("Selected nonmotorized Combat profile forbids broken vehicle lots.", nameof(brokenVehicleLots));
        if (Elements.Count != 2 || Representations.Count != 2 || Relationships.Count > 1 ||
            CustodyLots.Count > 1 || Guards.Count > 1 || ReplacementEntitlements.Count > 1 ||
            FutureObligations.Count > 1 || Settlements.Count > 1 || CohesionCauses.Count > 4096)
            throw new ArgumentException("Selected Combat world inventory bounds exceeded.", nameof(elements));
        if (CohesionCauses.Select(value => value.Ordinal).Order()
            .SequenceEqual(Enumerable.Range(1, CohesionCauses.Count)) == false)
            throw new ArgumentException("Cohesion cause ordinals must be contiguous.", nameof(cohesionCauses));
        if (CohesionCauses.Any(value => !locations.ContainsKey(value.ElementId)))
            throw new ArgumentException("Cohesion cause refers to an unknown element.", nameof(cohesionCauses));
        if (Relationships.Any(value => value.Attacker.CreationBinding != CreationBinding ||
                value.Defender.CreationBinding != CreationBinding ||
                !locations.ContainsKey(value.Attacker.ElementId) ||
                !locations.ContainsKey(value.Defender.ElementId)))
            throw new ArgumentException("Relationship refers outside this world.", nameof(relationships));
        var lotById = CustodyLots.ToDictionary(value => value.LotId, StringComparer.Ordinal);
        var guardById = Guards.ToDictionary(value => value.GuardId, StringComparer.Ordinal);
        if (CustodyLots.Any(value => value.OriginalComponent.Unit.CreationBinding != CreationBinding ||
                value.Captor.CreationBinding != CreationBinding ||
                !locations.ContainsKey(value.OriginalComponent.Unit.ElementId) ||
                !locations.ContainsKey(value.Captor.ElementId) ||
                !elementById[value.OriginalComponent.Unit.ElementId].Components.Any(component =>
                    component.ComponentId == value.OriginalComponent.ComponentId) ||
                (value.GuardId is not null && (!guardById.TryGetValue(value.GuardId, out var guard) ||
                    guard.LotId != value.LotId || guard.CurrentLocationId != value.CurrentLocationId))))
            throw new ArgumentException("Custody lot links are invalid.", nameof(custodyLots));
        if (Guards.Any(value => !lotById.TryGetValue(value.LotId, out var lot) ||
                lot.GuardId != value.GuardId || value.OriginComponent.Unit.CreationBinding != CreationBinding ||
                value.OriginComponent.Unit != lot.Captor ||
                !elementById[lot.Captor.ElementId].Components.Any(component =>
                    component.ComponentId == value.OriginComponent.ComponentId)))
            throw new ArgumentException("Guard links are invalid.", nameof(guards));
        if (ReplacementEntitlements.Any(value => !lotById.TryGetValue(value.LotId, out var lot) ||
                lot.Status != "escaped" || value.OriginalComponent != lot.OriginalComponent ||
                value.Quantity != lot.Quantity || value.EscapeReceiptId != lot.EscapeReceiptId))
            throw new ArgumentException("Replacement entitlement links are invalid.", nameof(replacementEntitlements));
        if (FutureObligations.Any(value => value.Kind == "guard-priority-upkeep"
                ? !guardById.ContainsKey(value.SubjectId)
                : !ReplacementEntitlements.Any(entitlement => entitlement.EntitlementId == value.SubjectId)))
            throw new ArgumentException("Future obligation refers to a missing subject.", nameof(futureObligations));
        foreach (var obligation in FutureObligations)
        {
            var settlement = Settlements.SingleOrDefault(value => value.Custody?.ReceiptId == obligation.ReceiptId);
            if (settlement is null || obligation.EarnedScope.GameTurn != settlement.GameTurn ||
                obligation.EarnedScope.OperationStage != settlement.OperationStage)
                throw new ArgumentException("Future obligation must match its custody settlement scope.", nameof(futureObligations));
            if (obligation.Kind == "guard-priority-upkeep")
            {
                var guard = guardById[obligation.SubjectId];
                if (obligation.ObligationId != $"{settlement.SettlementId}.upkeep" ||
                    settlement.Custody!.Kind != "relocate-and-guard" ||
                    settlement.Custody.GuardId != guard.GuardId ||
                    settlement.Custody.LotId != guard.LotId ||
                    guard.FormationReceiptId != obligation.ReceiptId)
                    throw new ArgumentException("Guard obligation does not match custody formation.", nameof(futureObligations));
            }
            else
            {
                var entitlement = ReplacementEntitlements.Single(value => value.EntitlementId == obligation.SubjectId);
                if (obligation.ObligationId != $"{settlement.SettlementId}.training" ||
                    settlement.Custody!.Kind != "leave-unguarded" ||
                    settlement.Custody.EntitlementId != entitlement.EntitlementId ||
                    settlement.Custody.LotId != entitlement.LotId ||
                    entitlement.EscapeReceiptId != obligation.ReceiptId ||
                    obligation.EarnedScope != entitlement.EarnedScope ||
                    obligation.EligibleScope != entitlement.EligibleScope)
                    throw new ArgumentException("Replacement obligation does not match custody entitlement.", nameof(futureObligations));
            }
        }
        if (Guards.Any(value => !FutureObligations.Any(obligation =>
                obligation.Kind == "guard-priority-upkeep" && obligation.SubjectId == value.GuardId)) ||
            ReplacementEntitlements.Any(value => !FutureObligations.Any(obligation =>
                obligation.Kind == "replacement-training-gate" && obligation.SubjectId == value.EntitlementId)))
            throw new ArgumentException("Created assets require their retained future work.", nameof(futureObligations));
        if (Settlements.Any(value => value.Attacker.CreationBinding != CreationBinding ||
                value.Defender.CreationBinding != CreationBinding ||
                !locations.ContainsKey(value.Attacker.ElementId) ||
                !locations.ContainsKey(value.Defender.ElementId)))
            throw new ArgumentException("Settlement refers outside this world.", nameof(settlements));
    }

    public int ContractVersion { get; }
    public string CreationBinding { get; }
    public IReadOnlyList<CampaignElementStateV6> Elements { get; }
    public IReadOnlyList<CampaignMapRepresentationState> Representations { get; }
    public IReadOnlyList<CampaignBrokenVehicleLot> BrokenVehicleLots { get; }
    public IReadOnlyList<CampaignCombatCohesionCause> CohesionCauses { get; }
    public IReadOnlyList<CampaignCombatRelationship> Relationships { get; }
    public IReadOnlyList<CampaignCombatCustodyLot> CustodyLots { get; }
    public IReadOnlyList<CampaignCombatGuardAsset> Guards { get; }
    public IReadOnlyList<CampaignCombatReplacementEntitlement> ReplacementEntitlements { get; }
    public IReadOnlyList<CampaignCombatFutureObligation> FutureObligations { get; }
    public IReadOnlyList<CampaignCombatSettlementState> Settlements { get; }

    public bool Equals(CampaignWorldSnapshotV7? other) => ReferenceEquals(this, other) ||
        (other is not null && ContractVersion == other.ContractVersion && CreationBinding == other.CreationBinding &&
         Elements.SequenceEqual(other.Elements) && Representations.SequenceEqual(other.Representations) &&
         BrokenVehicleLots.SequenceEqual(other.BrokenVehicleLots) && CohesionCauses.SequenceEqual(other.CohesionCauses) &&
         Relationships.SequenceEqual(other.Relationships) && CustodyLots.SequenceEqual(other.CustodyLots) &&
         Guards.SequenceEqual(other.Guards) && ReplacementEntitlements.SequenceEqual(other.ReplacementEntitlements) &&
         FutureObligations.SequenceEqual(other.FutureObligations) && Settlements.SequenceEqual(other.Settlements));

    public override int GetHashCode() => HashCode.Combine(ContractVersion, CreationBinding,
        Elements.Count, Representations.Count, CustodyLots.Count, Settlements.Count);

    private static ReadOnlyCollection<T> CopyUnique<T>(IEnumerable<T> source, Func<T, string> key, string parameter)
        where T : class
    {
        var copy = ContentContractGuards.CopyValues(source, parameter);
        if (copy.Select(key).Distinct(StringComparer.Ordinal).Count() != copy.Length)
            throw new ArgumentException("IDs must be unique.", parameter);
        return Array.AsReadOnly(copy.OrderBy(key, StringComparer.Ordinal).ToArray());
    }
}

internal static class CampaignWorldV7Factory
{
    private static readonly ContentOrigin ExpectedInitialLedgerOrigin = new(ContentOriginKind.Synthetic,
        [new RuleReference("sandtable-rules-lab", "combat.close-assault-positive.v1:initial-ledger")]);

    public static CampaignWorldSnapshotV7 CreateInitial(ContentPackV7Artifact artifact,
        ContentCombatScenario scenario, CampaignCombatInitializationPolicy initialization,
        string creationBinding)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(initialization);
        if (!ContentPackV7Validator.Validate(artifact.Definition).IsValid)
            throw new ArgumentException("Content7 is not certified for this Combat profile.", nameof(artifact));
        if (!artifact.Definition.Scenarios.Any(value => value == scenario))
            throw new ArgumentException("Scenario is not in the supplied Content7 artifact.", nameof(scenario));
        if (initialization.GameTurn != scenario.Start.GameTurn || initialization.OperationStage != 1 ||
            scenario.Start.OperationStage != 1 || scenario.End != scenario.Start ||
            initialization.CapabilityPointsExpended != CapabilityPointAmount.Zero ||
            initialization.CohesionLevel != 0 || initialization.ReserveStatus != CampaignElementReserveStatus.None ||
            initialization.Origin != ExpectedInitialLedgerOrigin)
            throw new ArgumentException("Combat initialization does not match the selected stage-1 profile.", nameof(initialization));

        var contentElements = artifact.Definition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var elements = scenario.InitialPlacements.Select(placement =>
        {
            var content = contentElements[placement.ElementId];
            return new CampaignElementStateV6(content.ElementId, placement.LocationId,
                initialization.ReserveStatus,
                new CampaignElementOperationalStateV6(initialization.GameTurn, initialization.OperationStage,
                    initialization.CapabilityPointsExpended, initialization.CohesionLevel, null, null, initialization.Origin),
                placement.InitialComponentToes.Select(value => new CampaignComponentToeState(
                    value.ComponentId, value.CurrentToe, value.Origin)),
                content.ParentFormationId, content.ParentFormationId,
                new CampaignElementAmmunitionState(placement.InitialAmmunition.Points,
                    placement.InitialAmmunition.Origin),
                new CampaignElementCombatReadinessState(placement.InitialReadiness.GameTurn,
                    placement.InitialReadiness.OperationStage, placement.InitialReadiness.WaterStatus,
                    placement.InitialReadiness.StoresStatus, placement.InitialReadiness.Pinned,
                    placement.InitialReadiness.Origin));
        }).ToArray();
        var representations = elements.OrderBy(value => value.ElementId, StringComparer.Ordinal)
            .Select((element, index) => new CampaignMapRepresentationState(
                $"map-representation.{index + 1:0000}", element.CurrentLocationId,
                CampaignMapRepresentationBindingKind.IndependentElement, [element.ElementId]));
        return new CampaignWorldSnapshotV7(CampaignWorldSnapshotV7.CurrentContractVersion,
            creationBinding, elements, representations, [], [], [], [], [], [], [], []);
    }
}
