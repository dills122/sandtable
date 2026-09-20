using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatUnitKey
{
    public CampaignCombatUnitKey(string creationBinding, string originalSide, string elementId)
    {
        CreationBinding = ContentContractGuards.RequireStableId(creationBinding, nameof(creationBinding));
        OriginalSide = originalSide is "axis" or "commonwealth" ? originalSide
            : throw new ArgumentException("Unsupported original side.", nameof(originalSide));
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
    }
    public string CreationBinding { get; }
    public string OriginalSide { get; }
    public string ElementId { get; }
}

internal sealed record CampaignCombatComponentKey
{
    public CampaignCombatComponentKey(CampaignCombatUnitKey unit, string componentId)
    {
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
        ComponentId = ContentContractGuards.RequireStableId(componentId, nameof(componentId));
    }
    public CampaignCombatUnitKey Unit { get; }
    public string ComponentId { get; }
}

internal sealed record CampaignCombatScope
{
    public CampaignCombatScope(int gameTurn, int operationStage, bool future = false)
    {
        if (gameTurn < 1 || gameTurn > (future ? 115 : 111))
            throw new ArgumentOutOfRangeException(nameof(gameTurn));
        if (operationStage is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(operationStage));
        GameTurn = gameTurn;
        OperationStage = operationStage;
    }
    public int GameTurn { get; }
    public int OperationStage { get; }
}

internal sealed record CampaignCombatRelationship
{
    public CampaignCombatRelationship(string relationId, string creationReceiptId, string kind,
        CampaignCombatUnitKey attacker, CampaignCombatUnitKey defender, int gameTurn, int operationStage,
        bool active, string? endedByReceiptId, string? endingCause)
    {
        RelationId = ContentContractGuards.RequireStableId(relationId, nameof(relationId));
        CreationReceiptId = ContentContractGuards.RequireStableId(creationReceiptId, nameof(creationReceiptId));
        if (kind is not ("contact" or "engaged")) throw new ArgumentException("Unknown relation kind.", nameof(kind));
        Kind = kind;
        Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
        Defender = defender ?? throw new ArgumentNullException(nameof(defender));
        _ = new CampaignCombatScope(gameTurn, operationStage);
        GameTurn = gameTurn;
        OperationStage = operationStage;
        if (attacker == defender || attacker.CreationBinding != defender.CreationBinding ||
            attacker.OriginalSide == defender.OriginalSide)
            throw new ArgumentException("Relation requires distinct units from one creation.", nameof(defender));
        if ((endedByReceiptId is null) != (endingCause is null) || active != (endedByReceiptId is null))
            throw new ArgumentException("Relation ending must be complete and agree with active state.", nameof(endedByReceiptId));
        Active = active;
        EndedByReceiptId = endedByReceiptId is null ? null : ContentContractGuards.RequireStableId(endedByReceiptId, nameof(endedByReceiptId));
        EndingCause = endingCause is null ? null : ContentContractGuards.RequireStableId(endingCause, nameof(endingCause));
    }
    public string RelationId { get; }
    public string CreationReceiptId { get; }
    public string Kind { get; }
    public CampaignCombatUnitKey Attacker { get; }
    public CampaignCombatUnitKey Defender { get; }
    public int GameTurn { get; }
    public int OperationStage { get; }
    public bool Active { get; }
    public string? EndedByReceiptId { get; }
    public string? EndingCause { get; }
}

internal sealed record CampaignCombatCustodyLot
{
    public CampaignCombatCustodyLot(string lotId, string lossReceiptId,
        CampaignCombatComponentKey originalComponent, CampaignCombatUnitKey captor, int quantity,
        string originLocationId, string? currentLocationId, string status,
        string? guardId, string? escapeReceiptId)
    {
        LotId = ContentContractGuards.RequireStableId(lotId, nameof(lotId));
        LossReceiptId = ContentContractGuards.RequireStableId(lossReceiptId, nameof(lossReceiptId));
        OriginalComponent = originalComponent ?? throw new ArgumentNullException(nameof(originalComponent));
        Captor = captor ?? throw new ArgumentNullException(nameof(captor));
        if (originalComponent.Unit.CreationBinding != captor.CreationBinding ||
            originalComponent.Unit.OriginalSide == captor.OriginalSide)
            throw new ArgumentException("Captor must belong to opposing original side in same creation.", nameof(captor));
        if (quantity is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(quantity));
        OriginLocationId = ContentContractGuards.RequireStableId(originLocationId, nameof(originLocationId));
        Status = status is "pending" or "guarded" or "escaped" ? status
            : throw new ArgumentException("Unsupported custody status.", nameof(status));
        CurrentLocationId = currentLocationId is null ? null : ContentContractGuards.RequireStableId(currentLocationId, nameof(currentLocationId));
        GuardId = guardId is null ? null : ContentContractGuards.RequireStableId(guardId, nameof(guardId));
        EscapeReceiptId = escapeReceiptId is null ? null : ContentContractGuards.RequireStableId(escapeReceiptId, nameof(escapeReceiptId));
        if ((status == "pending" && (CurrentLocationId != OriginLocationId || GuardId is not null || EscapeReceiptId is not null)) ||
            (status == "guarded" && (CurrentLocationId is null || GuardId is null || EscapeReceiptId is not null)) ||
            (status == "escaped" && (CurrentLocationId is not null || GuardId is not null || EscapeReceiptId is null)))
            throw new ArgumentException("Custody status and links do not agree.", nameof(status));
        Quantity = quantity;
    }
    public string LotId { get; }
    public string LossReceiptId { get; }
    public CampaignCombatComponentKey OriginalComponent { get; }
    public CampaignCombatUnitKey Captor { get; }
    public int Quantity { get; }
    public string OriginLocationId { get; }
    public string? CurrentLocationId { get; }
    public string Status { get; }
    public string? GuardId { get; }
    public string? EscapeReceiptId { get; }
}

internal sealed record CampaignCombatGuardAsset
{
    public CampaignCombatGuardAsset(string guardId, string formationReceiptId, string lotId,
        CampaignCombatComponentKey originComponent, string currentLocationId, int toe,
        int baseCapabilityPointAllowance, int offensiveCloseAssaultRating,
        int defensiveCloseAssaultRating, CampaignElementOperationalStateV6 operationalState,
        CampaignElementAmmunitionState ammunition, CampaignElementCombatReadinessState readiness)
    {
        GuardId = ContentContractGuards.RequireStableId(guardId, nameof(guardId));
        FormationReceiptId = ContentContractGuards.RequireStableId(formationReceiptId, nameof(formationReceiptId));
        LotId = ContentContractGuards.RequireStableId(lotId, nameof(lotId));
        OriginComponent = originComponent ?? throw new ArgumentNullException(nameof(originComponent));
        CurrentLocationId = ContentContractGuards.RequireStableId(currentLocationId, nameof(currentLocationId));
        if (toe != 1 || baseCapabilityPointAllowance != 10 || offensiveCloseAssaultRating != 0 || defensiveCloseAssaultRating != 1)
            throw new ArgumentException("Unsupported selected guard profile.", nameof(toe));
        OperationalState = operationalState ?? throw new ArgumentNullException(nameof(operationalState));
        Ammunition = ammunition ?? throw new ArgumentNullException(nameof(ammunition));
        Readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
        if (ammunition.Points != 0) throw new ArgumentException("Guard starts with no ammunition.", nameof(ammunition));
        Toe = toe;
        BaseCapabilityPointAllowance = baseCapabilityPointAllowance;
        OffensiveCloseAssaultRating = offensiveCloseAssaultRating;
        DefensiveCloseAssaultRating = defensiveCloseAssaultRating;
    }
    public string GuardId { get; }
    public string FormationReceiptId { get; }
    public string LotId { get; }
    public CampaignCombatComponentKey OriginComponent { get; }
    public string CurrentLocationId { get; }
    public int Toe { get; }
    public int BaseCapabilityPointAllowance { get; }
    public int OffensiveCloseAssaultRating { get; }
    public int DefensiveCloseAssaultRating { get; }
    public CampaignElementOperationalStateV6 OperationalState { get; }
    public CampaignElementAmmunitionState Ammunition { get; }
    public CampaignElementCombatReadinessState Readiness { get; }
}

internal sealed record CampaignCombatReplacementEntitlement
{
    public CampaignCombatReplacementEntitlement(string entitlementId, string escapeReceiptId,
        string lotId, CampaignCombatComponentKey originalComponent, int quantity,
        string reunionLocationId, CampaignCombatScope earnedScope, int delayOperationStages,
        CampaignCombatScope eligibleScope, string status)
    {
        EntitlementId = ContentContractGuards.RequireStableId(entitlementId, nameof(entitlementId));
        EscapeReceiptId = ContentContractGuards.RequireStableId(escapeReceiptId, nameof(escapeReceiptId));
        LotId = ContentContractGuards.RequireStableId(lotId, nameof(lotId));
        OriginalComponent = originalComponent ?? throw new ArgumentNullException(nameof(originalComponent));
        if (quantity is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(quantity));
        Quantity = quantity;
        ReunionLocationId = ContentContractGuards.RequireStableId(reunionLocationId, nameof(reunionLocationId));
        EarnedScope = earnedScope ?? throw new ArgumentNullException(nameof(earnedScope));
        EligibleScope = eligibleScope ?? throw new ArgumentNullException(nameof(eligibleScope));
        if (delayOperationStages != 12 ||
            (eligibleScope.GameTurn - 1) * 3 + eligibleScope.OperationStage -
            ((earnedScope.GameTurn - 1) * 3 + earnedScope.OperationStage) != 12)
            throw new ArgumentException("Replacement eligibility must be twelve Operation Stages later.", nameof(eligibleScope));
        if (status != "awaiting-eligibility-and-training")
            throw new ArgumentException("Unsupported replacement status.", nameof(status));
        DelayOperationStages = delayOperationStages;
        Status = status;
    }
    public string EntitlementId { get; }
    public string EscapeReceiptId { get; }
    public string LotId { get; }
    public CampaignCombatComponentKey OriginalComponent { get; }
    public int Quantity { get; }
    public string ReunionLocationId { get; }
    public CampaignCombatScope EarnedScope { get; }
    public int DelayOperationStages { get; }
    public CampaignCombatScope EligibleScope { get; }
    public string Status { get; }
}

internal sealed record CampaignCombatFutureObligation
{
    public CampaignCombatFutureObligation(string obligationId, string receiptId, string kind,
        string subjectId, CampaignCombatScope earnedScope, CampaignCombatScope? eligibleScope,
        string activationGate, string status)
    {
        ObligationId = ContentContractGuards.RequireStableId(obligationId, nameof(obligationId));
        ReceiptId = ContentContractGuards.RequireStableId(receiptId, nameof(receiptId));
        SubjectId = ContentContractGuards.RequireStableId(subjectId, nameof(subjectId));
        EarnedScope = earnedScope ?? throw new ArgumentNullException(nameof(earnedScope));
        if (kind == "guard-priority-upkeep" && eligibleScope is null &&
            activationGate == "before-prisoner-upkeep-or-guard-action")
            Kind = kind;
        else if (kind == "replacement-training-gate" && eligibleScope is not null &&
            activationGate == "before-replacement-eligibility-training")
            Kind = kind;
        else
            throw new ArgumentException("Unsupported obligation gate or scope.", nameof(kind));
        if (status != "retained-unimplemented")
            throw new ArgumentException("Unsupported obligation status.", nameof(status));
        EligibleScope = eligibleScope;
        ActivationGate = activationGate;
        Status = status;
    }
    public string ObligationId { get; }
    public string ReceiptId { get; }
    public string Kind { get; }
    public string SubjectId { get; }
    public CampaignCombatScope EarnedScope { get; }
    public CampaignCombatScope? EligibleScope { get; }
    public string ActivationGate { get; }
    public string Status { get; }
}

internal sealed record CampaignCombatSettlementState
{
    public CampaignCombatSettlementState(string settlementId, string commitmentId, string resultId,
        int gameTurn, int operationStage, CampaignCombatUnitKey attacker, CampaignCombatUnitKey defender,
        IEnumerable<CampaignElementStateV6> preLossElements, CampaignCombatResultFacts result,
        CampaignCombatRetreatDisposition? disposition, CampaignCombatLossReceipt? losses,
        CampaignCombatRetreatReceipt? retreat, CampaignCombatCustodyReceipt? custody,
        CampaignCombatRelationshipsReceipt? relationships)
        : this(settlementId, commitmentId, resultId, gameTurn, operationStage, attacker, defender,
            preLossElements, result, disposition, losses, retreat, custody, relationships, false)
    { }

    internal static CampaignCombatSettlementState CreateResolvedResultV2(string commitmentId, string resultId,
        int gameTurn, int operationStage, CampaignCombatUnitKey attacker, CampaignCombatUnitKey defender,
        IEnumerable<CampaignElementStateV6> preLossElements, CampaignCombatResultFacts result)
    {
        var settlementId = ResultV2SettlementId(commitmentId, resultId);
        return new(settlementId, commitmentId, resultId, gameTurn, operationStage, attacker, defender,
            preLossElements, result, null, null, null, null, null, true);
    }

    private static string ResultV2SettlementId(string commitmentId, string resultId)
    {
        static bool Valid(string value, string prefix) => value is not null && value.StartsWith(prefix, StringComparison.Ordinal) &&
            value.Length == prefix.Length + 64 && value[prefix.Length..].All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
        if (!Valid(commitmentId, "cmt.") || !Valid(resultId, "res."))
            throw new ArgumentException("Result2 occurrence identities require exact domain hashes.");
        return "set." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.settlement.v2",
            JsonSerializer.SerializeToUtf8Bytes(new { commitmentId, resultId }))[7..];
    }

    internal CampaignCombatSettlementState WithResultV2Disposition(CampaignCombatRetreatDisposition disposition)
    {
        ArgumentNullException.ThrowIfNull(disposition);
        if (Disposition is not null || Losses is not null || Retreat is not null || Custody is not null || Relationships is not null)
            throw new ArgumentException("Disposition requires resolved Result2 settlement.");
        return ResultV2Next(disposition, null, null);
    }
    internal CampaignCombatSettlementState WithResultV2Losses(CampaignCombatLossReceipt losses)
    {
        ArgumentNullException.ThrowIfNull(losses);
        if (Disposition is null || Losses is not null || Retreat is not null || Custody is not null || Relationships is not null)
            throw new ArgumentException("Losses require retained Result2 disposition.");
        return ResultV2Next(Disposition, losses, null);
    }
    internal CampaignCombatSettlementState WithResultV2Retreat(CampaignCombatRetreatReceipt retreat)
    {
        ArgumentNullException.ThrowIfNull(retreat);
        if (Disposition is null || Losses is null || Retreat is not null || Custody is not null || Relationships is not null)
            throw new ArgumentException("Retreat requires retained joint Result2 losses.");
        return ResultV2Next(Disposition, Losses, retreat);
    }
    internal CampaignCombatSettlementState WithResultV2Custody(CampaignCombatCustodyReceipt custody)
    {
        ArgumentNullException.ThrowIfNull(custody);
        if (Disposition is null || Losses is null || Retreat is null || Custody is not null || Relationships is not null ||
            !Losses.Roles.Any(role => role.CapturedToe > 0))
            throw new ArgumentException("Custody requires retained retreat and positive captured allocation.");
        return ResultV2Next(Disposition, Losses, Retreat, custody);
    }
    internal CampaignCombatSettlementState WithResultV2Relationships(CampaignCombatRelationshipsReceipt relationships)
    {
        ArgumentNullException.ThrowIfNull(relationships);
        if (Disposition is null || Losses is null || Retreat is null || Relationships is not null ||
            (Custody is not null) != Losses.Roles.Any(role => role.CapturedToe > 0))
            throw new ArgumentException("Relationships require complete immediate Result2 settlement.");
        return ResultV2Next(Disposition, Losses, Retreat, Custody, relationships);
    }
    private CampaignCombatSettlementState ResultV2Next(CampaignCombatRetreatDisposition disposition,
        CampaignCombatLossReceipt? losses, CampaignCombatRetreatReceipt? retreat, CampaignCombatCustodyReceipt? custody = null,
        CampaignCombatRelationshipsReceipt? relationships = null) =>
        new(SettlementId, CommitmentId, ResultId, GameTurn, OperationStage, Attacker, Defender, PreLossElements,
            Result, disposition, losses, retreat, custody, relationships, true);

    private CampaignCombatSettlementState(string settlementId, string commitmentId, string resultId,
        int gameTurn, int operationStage, CampaignCombatUnitKey attacker, CampaignCombatUnitKey defender,
        IEnumerable<CampaignElementStateV6> preLossElements, CampaignCombatResultFacts result,
        CampaignCombatRetreatDisposition? disposition, CampaignCombatLossReceipt? losses,
        CampaignCombatRetreatReceipt? retreat, CampaignCombatCustodyReceipt? custody,
        CampaignCombatRelationshipsReceipt? relationships, bool resolvedV2)
    {
        SettlementId = ContentContractGuards.RequireStableId(settlementId, nameof(settlementId));
        if (SettlementId.Length > 80) throw new ArgumentOutOfRangeException(nameof(settlementId));
        CommitmentId = ContentContractGuards.RequireStableId(commitmentId, nameof(commitmentId));
        ResultId = ContentContractGuards.RequireStableId(resultId, nameof(resultId));
        if (resolvedV2 && SettlementId != ResultV2SettlementId(CommitmentId, ResultId))
            throw new ArgumentException("Result2 settlement identity differs from its occurrence.", nameof(settlementId));
        if (!resolvedV2 && (CommitmentId != $"{SettlementId}.commit" || ResultId != $"{SettlementId}.result"))
            throw new ArgumentException("Settlement IDs must bind the same occurrence.", nameof(resultId));
        _ = new CampaignCombatScope(gameTurn, operationStage);
        GameTurn = gameTurn;
        OperationStage = operationStage;
        Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
        Defender = defender ?? throw new ArgumentNullException(nameof(defender));
        if (attacker == defender || attacker.CreationBinding != defender.CreationBinding ||
            attacker.OriginalSide == defender.OriginalSide)
            throw new ArgumentException("Settlement requires original opposing units.", nameof(defender));
        var copy = ContentContractGuards.CopyValues(preLossElements, nameof(preLossElements));
        if (copy.Length != 2 || copy.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count() != 2 ||
            !copy.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal)
                .SetEquals([attacker.ElementId, defender.ElementId]))
            throw new ArgumentException("Settlement must retain both original pre-loss elements.", nameof(preLossElements));
        if (copy.Any(value =>
        {
            var ledger = value.OperationalState;
            var cp = ledger.CapabilityPointsExpended;
            var minimumCp = value.ElementId == attacker.ElementId ? 5 : 3;
            return value.Ammunition.Points != 0 || ledger.CohesionLevel != 0 ||
                ledger.LedgerGameTurn != gameTurn || ledger.LedgerOperationStage != operationStage ||
                cp.Denominator != 1 || cp.Numerator < minimumCp || cp.Numerator > 10 ||
                value.Components.Count != 1 || value.Components[0].CurrentToe != 10;
        }))
            throw new ArgumentException("Pre-loss elements must retain selected paid post-commit state.", nameof(preLossElements));
        PreLossElements = Array.AsReadOnly(copy.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());
        Result = result ?? throw new ArgumentNullException(nameof(result));
        if ((losses is not null && disposition is null) ||
            (retreat is not null && losses is null) ||
            (custody is not null && retreat is null) ||
            (relationships is not null && retreat is null))
            throw new ArgumentException("Settlement receipts are out of order.", nameof(losses));
        if (disposition is not null && disposition.RequiredDistance != result.RequiredRetreat)
            throw new ArgumentException("Disposition must match the resolved retreat requirement.", nameof(disposition));
        if (retreat is not null && disposition is not null &&
            (retreat.Kind != disposition.Kind || retreat.CompletedDistance != disposition.PlannedDistance ||
                !retreat.Route.SequenceEqual(disposition.Route)))
            throw new ArgumentException("Retreat receipt must match its disposition.", nameof(retreat));
        if (retreat is not null && retreat.BeforeCp !=
            PreLossElements.Single(value => value.ElementId == Defender.ElementId)
                .OperationalState.CapabilityPointsExpended)
            throw new ArgumentException("Retreat CP must start from the frozen paid defender ledger.", nameof(retreat));
        var hasCapturedToe = losses?.Roles.Any(value => value.CapturedToe > 0) ?? false;
        if ((custody is not null && !hasCapturedToe) ||
            (relationships is not null && hasCapturedToe != (custody is not null)))
            throw new ArgumentException("Custody receipt presence must match captured TOE.", nameof(custody));
        string previous = ResultId;
        if (disposition is not null)
        {
            RequireReceipt(disposition.ReceiptId, disposition.PredecessorReceiptId, "disposition", previous);
            previous = disposition.ReceiptId;
        }
        if (losses is not null)
        {
            RequireReceipt(losses.ReceiptId, losses.PredecessorReceiptId, "losses", previous);
            if (losses.Roles.Any(value => value.Component.Unit !=
                    (value.Role == "attacker" ? Attacker : Defender)))
                throw new ArgumentException("Loss role must bind the original participant.", nameof(losses));
            foreach (var loss in losses.Roles)
            {
                var expectedElement = PreLossElements.Single(value => value.ElementId == loss.Component.Unit.ElementId);
                var expectedPercent = loss.Role == "attacker" ? Result.AttackerPercent : Result.DefenderPercent;
                var expectedRefusal = loss.Role == "defender" ? 10 * disposition!.UnfulfilledDistance : 0;
                var expectedCaptured = loss.Role == Result.CapturedRole
                    ? (loss.LossToe * Result.CaptureShare + 99) / 100 : 0;
                if (loss.TablePercent != expectedPercent || loss.RefusalPercent != expectedRefusal ||
                    loss.CapturedToe != expectedCaptured ||
                    !expectedElement.Components.Any(value => value.ComponentId == loss.Component.ComponentId &&
                        value.CurrentToe == loss.CommittedToe))
                    throw new ArgumentException("Loss receipt differs from result, disposition, or pre-loss component.", nameof(losses));
            }
            previous = losses.ReceiptId;
        }
        if (retreat is not null)
        {
            RequireReceipt(retreat.ReceiptId, retreat.PredecessorReceiptId, "retreat", previous);
            previous = retreat.ReceiptId;
        }
        if (custody is not null)
        {
            RequireReceipt(custody.ReceiptId, custody.PredecessorReceiptId, "custody", previous);
            previous = custody.ReceiptId;
        }
        if (relationships is not null)
            RequireReceipt(relationships.ReceiptId, relationships.PredecessorReceiptId, "relationships", previous);
        Disposition = disposition;
        Losses = losses;
        Retreat = retreat;
        Custody = custody;
        Relationships = relationships;
    }
    public string SettlementId { get; }
    public string CommitmentId { get; }
    public string ResultId { get; }
    public int GameTurn { get; }
    public int OperationStage { get; }
    public CampaignCombatUnitKey Attacker { get; }
    public CampaignCombatUnitKey Defender { get; }
    public IReadOnlyList<CampaignElementStateV6> PreLossElements { get; }
    public CampaignCombatResultFacts Result { get; }
    public CampaignCombatRetreatDisposition? Disposition { get; }
    public CampaignCombatLossReceipt? Losses { get; }
    public CampaignCombatRetreatReceipt? Retreat { get; }
    public CampaignCombatCustodyReceipt? Custody { get; }
    public CampaignCombatRelationshipsReceipt? Relationships { get; }
    public bool Equals(CampaignCombatSettlementState? other) => ReferenceEquals(this, other) ||
        (other is not null && SettlementId == other.SettlementId && CommitmentId == other.CommitmentId &&
         ResultId == other.ResultId && GameTurn == other.GameTurn && OperationStage == other.OperationStage &&
         Attacker == other.Attacker &&
         Defender == other.Defender && PreLossElements.SequenceEqual(other.PreLossElements) &&
         Result == other.Result && Disposition == other.Disposition && Losses == other.Losses &&
         Retreat == other.Retreat && Custody == other.Custody && Relationships == other.Relationships);
    public override int GetHashCode() => HashCode.Combine(SettlementId, CommitmentId, ResultId);

    private void RequireReceipt(string receiptId, string predecessor, string suffix, string expectedPredecessor)
    {
        if (receiptId != $"{SettlementId}.{suffix}" || predecessor != expectedPredecessor)
            throw new ArgumentException("Settlement receipt identity or predecessor differs.", nameof(receiptId));
    }
}

internal sealed record CampaignCombatGuardTransfer(
    CampaignElementStateV6 Donor,
    CampaignCombatCustodyLot Lot,
    CampaignCombatGuardAsset Guard);

internal static class CampaignCombatGuardFormation
{
    public static CampaignCombatGuardTransfer Transfer(CampaignElementStateV6 donor,
        CampaignCombatCustodyLot pendingLot, string donorComponentId,
        string formationReceiptId, string guardId)
    {
        ArgumentNullException.ThrowIfNull(donor);
        ArgumentNullException.ThrowIfNull(pendingLot);
        if (pendingLot.Status != "pending" || pendingLot.Captor.ElementId != donor.ElementId)
            throw new ArgumentException("Guard transfer requires a pending lot held by the donor.", nameof(pendingLot));
        var component = donor.Components.SingleOrDefault(value => value.ComponentId == donorComponentId);
        if (component is null || component.CurrentToe < 1)
            throw new ArgumentException("Guard donor component must retain at least one TOE.", nameof(donorComponentId));
        var donorAfter = new CampaignElementStateV6(donor.ElementId, donor.CurrentLocationId,
            donor.ReserveStatus, donor.OperationalState,
            donor.Components.Select(value => value.ComponentId == donorComponentId
                ? new CampaignComponentToeState(value.ComponentId, checked(value.CurrentToe - 1), value.InitialToeOrigin)
                : value),
            donor.SourceParentFormationId, donor.CurrentParentFormationId,
            donor.Ammunition, donor.Readiness);
        var guard = new CampaignCombatGuardAsset(guardId, formationReceiptId, pendingLot.LotId,
            new CampaignCombatComponentKey(pendingLot.Captor, donorComponentId), donor.CurrentLocationId,
            1, 10, 0, 1, donor.OperationalState,
            new CampaignElementAmmunitionState(0, donor.Ammunition.InitialAmmunitionOrigin), donor.Readiness);
        var lotAfter = new CampaignCombatCustodyLot(pendingLot.LotId, pendingLot.LossReceiptId,
            pendingLot.OriginalComponent, pendingLot.Captor, pendingLot.Quantity,
            pendingLot.OriginLocationId, donor.CurrentLocationId, "guarded", guard.GuardId, null);
        return new CampaignCombatGuardTransfer(donorAfter, lotAfter, guard);
    }
}
