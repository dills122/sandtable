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
        if (Settlements.Count == 0)
        {
            if (CustodyLots.Count != 0 || Relationships.Count != 0)
                throw new ArgumentException("Custody and relationships require a retained settlement.", nameof(settlements));
        }
        else
        {
            var settlement = Settlements[0];
            ValidateCustodyLot(settlement);
            ValidateCustodyAssets(settlement);
            ValidateRelationship(settlement);
            ValidateSettlementCauses(settlement);
            if (settlement.Relationships is null)
                ValidateOpenSettlementEffects(settlement);
        }
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

    private void ValidateCustodyLot(CampaignCombatSettlementState settlement)
    {
        var captured = settlement.Losses?.Roles.SingleOrDefault(value => value.CapturedToe > 0);
        if (captured is null)
        {
            if (CustodyLots.Count != 0)
                throw new ArgumentException("Custody lot has no captured loss receipt.");
            return;
        }
        if (CustodyLots.Count != 1)
            throw new ArgumentException("Positive captured loss requires one custody lot.");
        var lot = CustodyLots[0];
        var captor = captured.Role == "attacker" ? settlement.Defender : settlement.Attacker;
        var original = settlement.PreLossElements.Single(value => value.ElementId == captured.Component.Unit.ElementId);
        if (lot.LossReceiptId != settlement.Losses!.ReceiptId ||
            lot.OriginalComponent != captured.Component || lot.Captor != captor ||
            lot.Quantity != captured.CapturedToe || lot.OriginLocationId != original.CurrentLocationId)
            throw new ArgumentException("Custody lot differs from retained captured loss.");
        var custody = settlement.Custody;
        if (custody is null)
        {
            if (lot.Status != "pending")
                throw new ArgumentException("Lot must remain pending before custody disposition.");
        }
        else if (custody.LotId != lot.LotId ||
            (custody.Kind == "relocate-and-guard" &&
                (lot.Status != "guarded" || lot.GuardId != custody.GuardId ||
                    lot.CurrentLocationId != custody.Route[^1])) ||
            (custody.Kind == "leave-unguarded" &&
                (lot.Status != "escaped" || lot.EscapeReceiptId != custody.ReceiptId)))
            throw new ArgumentException("Custody lot differs from its disposition receipt.");
    }

    private void ValidateOpenSettlementEffects(CampaignCombatSettlementState settlement)
    {
        var expected = ExpectedPreCustodyElements(settlement);
        if (settlement.Custody is { Kind: "relocate-and-guard" })
        {
            var captorId = CustodyLots[0].Captor.ElementId;
            var donor = expected[captorId];
            expected[captorId] = WithEffects(donor, donor.CurrentLocationId,
                donor.OperationalState.CapabilityPointsExpended, donor.OperationalState.CohesionLevel,
                checked(donor.Components[0].CurrentToe - 1));
        }
        if (!Elements.SequenceEqual(expected.Values.OrderBy(value => value.ElementId, StringComparer.Ordinal)))
            throw new ArgumentException("Current elements differ from open settlement receipt effects.");
    }

    private static Dictionary<string, CampaignElementStateV6> ExpectedPreCustodyElements(
        CampaignCombatSettlementState settlement)
    {
        var expected = settlement.PreLossElements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (settlement.Losses is not null)
        {
            foreach (var loss in settlement.Losses.Roles)
            {
                var original = expected[loss.Component.Unit.ElementId];
                expected[original.ElementId] = WithEffects(original, original.CurrentLocationId,
                    original.OperationalState.CapabilityPointsExpended,
                    checked(original.OperationalState.CohesionLevel - loss.LossDp), loss.RemainingToe);
            }
        }
        if (settlement.Retreat is not null)
        {
            var retreat = settlement.Retreat;
            var attacker = expected[settlement.Attacker.ElementId];
            var defender = expected[settlement.Defender.ElementId];
            expected[attacker.ElementId] = WithEffects(attacker, attacker.CurrentLocationId,
                attacker.OperationalState.CapabilityPointsExpended,
                Math.Min(10, checked(attacker.OperationalState.CohesionLevel + retreat.AttackerVictoryRp)),
                attacker.Components[0].CurrentToe);
            expected[defender.ElementId] = WithEffects(defender,
                retreat.Kind == "retreat" ? retreat.Route[^1] : defender.CurrentLocationId,
                retreat.AfterCp, checked(defender.OperationalState.CohesionLevel - retreat.ExcessCpDp),
                defender.Components[0].CurrentToe);
        }
        return expected;
    }

    private void ValidateRelationship(CampaignCombatSettlementState settlement)
    {
        var receipt = settlement.Relationships;
        if (receipt?.RelationshipId is null)
        {
            if (Relationships.Count != 0)
                throw new ArgumentException("Relationship has no publication receipt.");
            return;
        }
        if (Relationships.Count != 1)
            throw new ArgumentException("Published relationship must be retained.");
        var relation = Relationships[0];
        if (relation.RelationId != receipt.RelationshipId || relation.CreationReceiptId != receipt.ReceiptId ||
            relation.Kind != receipt.Kind || relation.Attacker != settlement.Attacker ||
            relation.Defender != settlement.Defender || relation.GameTurn != settlement.GameTurn ||
            relation.OperationStage != settlement.OperationStage || !relation.Active)
            throw new ArgumentException("Relationship differs from its publication receipt.");
    }

    private void ValidateCustodyAssets(CampaignCombatSettlementState settlement)
    {
        var custody = settlement.Custody;
        if (custody is null)
        {
            if (Guards.Count != 0)
                throw new ArgumentException("Guard requires a custody receipt.");
            return;
        }
        var lot = CustodyLots[0];
        var donor = ExpectedPreCustodyElements(settlement)[lot.Captor.ElementId];
        var donorComponent = donor.Components[0];
        var transfer = custody.Kind == "relocate-and-guard" ? 1 : 0;
        if (custody.DonorToeBefore != donorComponent.CurrentToe ||
            custody.DonorToeAfter != donorComponent.CurrentToe - transfer)
            throw new ArgumentException("Custody donor TOE differs from retained settlement state.");
        if (transfer == 0)
        {
            if (Guards.Count != 0)
                throw new ArgumentException("Unguarded custody cannot retain a guard.");
            return;
        }
        if (Guards.Count != 1)
            throw new ArgumentException("Guarded custody requires one guard.");
        var guard = Guards[0];
        if (guard.GuardId != custody.GuardId || guard.FormationReceiptId != custody.ReceiptId ||
            guard.LotId != lot.LotId || guard.OriginComponent != new CampaignCombatComponentKey(lot.Captor,
                donorComponent.ComponentId) || guard.CurrentLocationId != donor.CurrentLocationId ||
            guard.OperationalState != donor.OperationalState || guard.Readiness != donor.Readiness ||
            guard.Ammunition.InitialAmmunitionOrigin != donor.Ammunition.InitialAmmunitionOrigin)
            throw new ArgumentException("Guard differs from retained donor provenance.");
    }

    private void ValidateSettlementCauses(CampaignCombatSettlementState settlement)
    {
        var expected = new List<(string Id, string Receipt, string Element, string Kind, int Points, int Before, int After)>();
        void Add(string receipt, CampaignCombatUnitKey unit, string kind, int points, int before, int after)
        {
            if (points > 0)
                expected.Add(($"{settlement.SettlementId}.{kind}.{unit.OriginalSide}",
                    receipt, unit.ElementId, kind, points, before, after));
        }
        var attackerBefore = settlement.PreLossElements.Single(value => value.ElementId == settlement.Attacker.ElementId)
            .OperationalState.CohesionLevel;
        var defenderBefore = settlement.PreLossElements.Single(value => value.ElementId == settlement.Defender.ElementId)
            .OperationalState.CohesionLevel;
        if (settlement.Losses is not null)
        {
            var attackerLoss = settlement.Losses.Roles.Single(value => value.Role == "attacker");
            var defenderLoss = settlement.Losses.Roles.Single(value => value.Role == "defender");
            Add(settlement.Losses.ReceiptId, settlement.Attacker, "loss-dp", attackerLoss.LossDp,
                attackerBefore, checked(attackerBefore - attackerLoss.LossDp));
            Add(settlement.Losses.ReceiptId, settlement.Defender, "loss-dp", defenderLoss.LossDp,
                defenderBefore, checked(defenderBefore - defenderLoss.LossDp));
            attackerBefore = checked(attackerBefore - attackerLoss.LossDp);
            defenderBefore = checked(defenderBefore - defenderLoss.LossDp);
        }
        if (settlement.Retreat is not null)
        {
            Add(settlement.Retreat.ReceiptId, settlement.Defender, "retreat-excess-dp",
                settlement.Retreat.ExcessCpDp, defenderBefore,
                checked(defenderBefore - settlement.Retreat.ExcessCpDp));
            Add(settlement.Retreat.ReceiptId, settlement.Attacker, "assault-victory-rp",
                settlement.Retreat.AttackerVictoryRp, attackerBefore,
                Math.Min(10, checked(attackerBefore + settlement.Retreat.AttackerVictoryRp)));
        }
        var actual = CohesionCauses.Where(value =>
                value.CauseId.StartsWith($"{settlement.SettlementId}.", StringComparison.Ordinal) ||
                value.ReceiptId == settlement.Losses?.ReceiptId ||
                value.ReceiptId == settlement.Retreat?.ReceiptId)
            .OrderBy(value => value.Ordinal).ToArray();
        if (actual.Length != expected.Count)
            throw new ArgumentException("Settlement Cohesion cause count differs from receipts.");
        for (var index = 0; index < actual.Length; index++)
        {
            var cause = actual[index];
            var requirement = expected[index];
            if (cause.CauseId != requirement.Id || cause.ReceiptId != requirement.Receipt ||
                cause.ElementId != requirement.Element || cause.Kind != requirement.Kind ||
                cause.Points != requirement.Points || cause.Before != requirement.Before ||
                cause.After != requirement.After || cause.GameTurn != settlement.GameTurn ||
                cause.OperationStage != settlement.OperationStage ||
                cause.Ordinal != actual[0].Ordinal + index)
                throw new ArgumentException("Settlement Cohesion cause differs from receipt effects.");
        }
    }

    private static CampaignElementStateV6 WithEffects(CampaignElementStateV6 element,
        string location, CapabilityPointAmount cp, int cohesion, int toe)
    {
        var source = element.Components[0];
        var ledger = element.OperationalState;
        return new CampaignElementStateV6(element.ElementId, location, element.ReserveStatus,
            new CampaignElementOperationalStateV6(ledger.LedgerGameTurn, ledger.LedgerOperationStage,
                cp, cohesion, ledger.VehicleBreakdownState, ledger.MovementEnded, ledger.InitialLedgerOrigin),
            [new CampaignComponentToeState(source.ComponentId, toe, source.InitialToeOrigin)],
            element.SourceParentFormationId, element.CurrentParentFormationId,
            element.Ammunition, element.Readiness);
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
