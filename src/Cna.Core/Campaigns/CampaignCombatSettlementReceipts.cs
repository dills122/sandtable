using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatResultFacts
{
    public CampaignCombatResultFacts(int differential, int attackerCoordinate, int defenderCoordinate,
        int? captureDie, int attackerPercent, int defenderPercent, bool rawEngaged,
        int requiredRetreat, string? capturedRole, int captureShare)
    {
        if (differential is < -2 or > 2) throw new ArgumentOutOfRangeException(nameof(differential));
        if (!IsDiceCoordinate(attackerCoordinate) || !IsDiceCoordinate(defenderCoordinate))
            throw new ArgumentOutOfRangeException(nameof(attackerCoordinate));
        if (captureDie is not null and (< 1 or > 6)) throw new ArgumentOutOfRangeException(nameof(captureDie));
        if (attackerPercent is < 0 or > 100 || defenderPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(attackerPercent));
        if (requiredRetreat is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(requiredRetreat));
        if (capturedRole is not null and not ("attacker" or "defender"))
            throw new ArgumentException("Unknown captured role.", nameof(capturedRole));
        if (captureShare is < 0 or > 100 || (capturedRole is null) != (captureDie is null) ||
            (capturedRole is null && captureShare != 0))
            throw new ArgumentException("Capture result fields disagree.", nameof(captureShare));
        var rules = Cna1979CombatAdjudication.Definition;
        var effects = rules.Effects[differential + 2];
        var attackerSum = attackerCoordinate / 10 + attackerCoordinate % 10;
        var defenderSum = defenderCoordinate / 10 + defenderCoordinate % 10;
        var expectedCapturedRole = effects.AttackerCaptureSums.Contains(attackerSum) ? "attacker"
            : effects.DefenderCaptureSums.Contains(defenderSum) ? "defender" : null;
        if (attackerPercent != Cna1979CombatAdjudication.LookupLossPercent(
                CombatRole.Attacker, differential, attackerCoordinate) ||
            defenderPercent != Cna1979CombatAdjudication.LookupLossPercent(
                CombatRole.Defender, differential, defenderCoordinate) ||
            rawEngaged != effects.AttackerEngagedSums.Contains(attackerSum) ||
            requiredRetreat != (effects.DefenderRetreatOneHexSums.Contains(defenderSum) ? 1 : 0) ||
            capturedRole != expectedCapturedRole ||
            captureShare != (captureDie is int die ? rules.CaptureShares[die - 1].Percent : 0))
            throw new ArgumentException("Result facts differ from selected Rules tables or effects.", nameof(differential));
        Differential = differential;
        AttackerCoordinate = attackerCoordinate;
        DefenderCoordinate = defenderCoordinate;
        CaptureDie = captureDie;
        AttackerPercent = attackerPercent;
        DefenderPercent = defenderPercent;
        RawEngaged = rawEngaged;
        RequiredRetreat = requiredRetreat;
        CapturedRole = capturedRole;
        CaptureShare = captureShare;
    }
    public int Differential { get; }
    public int AttackerCoordinate { get; }
    public int DefenderCoordinate { get; }
    public int? CaptureDie { get; }
    public int AttackerPercent { get; }
    public int DefenderPercent { get; }
    public bool RawEngaged { get; }
    public int RequiredRetreat { get; }
    public string? CapturedRole { get; }
    public int CaptureShare { get; }

    private static bool IsDiceCoordinate(int value) => value is >= 11 and <= 66 &&
        value / 10 is >= 1 and <= 6 && value % 10 is >= 1 and <= 6;
}

internal sealed record CampaignCombatRetreatDisposition
{
    public CampaignCombatRetreatDisposition(string receiptId, string predecessorReceiptId, string kind,
        int requiredDistance, int plannedDistance, int unfulfilledDistance, IEnumerable<string> route)
    {
        ReceiptId = ContentContractGuards.RequireStableId(receiptId, nameof(receiptId));
        PredecessorReceiptId = ContentContractGuards.RequireStableId(predecessorReceiptId, nameof(predecessorReceiptId));
        Kind = kind is "retreat" or "refuse-retreat" or "not-required" ? kind
            : throw new ArgumentException("Unknown retreat disposition.", nameof(kind));
        if (requiredDistance is < 0 or > 1 || plannedDistance < 0 || unfulfilledDistance < 0 ||
            (long)plannedDistance + unfulfilledDistance != requiredDistance)
            throw new ArgumentException("Retreat distance does not conserve.", nameof(plannedDistance));
        if ((Kind == "retreat" && (requiredDistance != 1 || plannedDistance != 1 || unfulfilledDistance != 0)) ||
            (Kind == "refuse-retreat" && (requiredDistance != 1 || plannedDistance != 0 || unfulfilledDistance != 1)) ||
            (Kind == "not-required" && (requiredDistance != 0 || plannedDistance != 0 || unfulfilledDistance != 0)))
            throw new ArgumentException("Retreat disposition kind contradicts its distance.", nameof(kind));
        RequiredDistance = requiredDistance;
        PlannedDistance = plannedDistance;
        UnfulfilledDistance = unfulfilledDistance;
        Route = CopyRoute(route);
        if (Route.Count - 1 != plannedDistance)
            throw new ArgumentException("Disposition route does not match planned distance.", nameof(route));
    }
    public string ReceiptId { get; }
    public string PredecessorReceiptId { get; }
    public string Kind { get; }
    public int RequiredDistance { get; }
    public int PlannedDistance { get; }
    public int UnfulfilledDistance { get; }
    public IReadOnlyList<string> Route { get; }
    public bool Equals(CampaignCombatRetreatDisposition? other) => ReferenceEquals(this, other) ||
        (other is not null && ReceiptId == other.ReceiptId && PredecessorReceiptId == other.PredecessorReceiptId &&
         Kind == other.Kind && RequiredDistance == other.RequiredDistance && PlannedDistance == other.PlannedDistance &&
         UnfulfilledDistance == other.UnfulfilledDistance && Route.SequenceEqual(other.Route));
    public override int GetHashCode() => HashCode.Combine(ReceiptId, PredecessorReceiptId, Kind);

    internal static IReadOnlyList<string> CopyRoute(IEnumerable<string> route)
    {
        ArgumentNullException.ThrowIfNull(route);
        var copy = route.Select(value => ContentContractGuards.RequireStableId(value, nameof(route))).ToArray();
        if (copy.Length == 0)
            throw new ArgumentException("A route must retain its origin and ordered path.", nameof(route));
        return Array.AsReadOnly(copy);
    }
}

internal sealed record CampaignCombatRoleLoss
{
    public CampaignCombatRoleLoss(string role, CampaignCombatComponentKey component, int committedToe,
        int tablePercent, int refusalPercent, int lossToe, int capturedToe, int otherLossToe,
        int remainingToe, int lossDp)
    {
        if (role is not ("attacker" or "defender")) throw new ArgumentException("Unknown role.", nameof(role));
        Component = component ?? throw new ArgumentNullException(nameof(component));
        if (committedToe != 10 || tablePercent is < 0 or > 100 || refusalPercent is < 0 or > 100 ||
            lossToe < 0 || capturedToe < 0 || otherLossToe < 0 || remainingToe < 0 || lossDp < 0 ||
            committedToe != checked(lossToe + remainingToe) || lossToe != checked(capturedToe + otherLossToe))
            throw new ArgumentException("Role loss does not conserve TOE.", nameof(lossToe));
        if (lossDp != (lossToe >= 3 ? 3 : 0))
            throw new ArgumentException("Loss DP must match the selected 30%-of-10 threshold.", nameof(lossDp));
        if ((role == "attacker" && refusalPercent != 0) ||
            (role == "defender" && refusalPercent is not (0 or 10)))
            throw new ArgumentException("Refusal percent does not match the selected role bound.", nameof(refusalPercent));
        var effectivePercent = tablePercent + refusalPercent;
        var expectedLoss = role == "attacker"
            ? (10 * effectivePercent + 99) / 100
            : 10 * effectivePercent / 100;
        if (lossToe != expectedLoss)
            throw new ArgumentException("Role loss does not match selected rounding.", nameof(lossToe));
        Role = role;
        CommittedToe = committedToe;
        TablePercent = tablePercent;
        RefusalPercent = refusalPercent;
        LossToe = lossToe;
        CapturedToe = capturedToe;
        OtherLossToe = otherLossToe;
        RemainingToe = remainingToe;
        LossDp = lossDp;
    }
    public string Role { get; }
    public CampaignCombatComponentKey Component { get; }
    public int CommittedToe { get; }
    public int TablePercent { get; }
    public int RefusalPercent { get; }
    public int LossToe { get; }
    public int CapturedToe { get; }
    public int OtherLossToe { get; }
    public int RemainingToe { get; }
    public int LossDp { get; }
}

internal sealed record CampaignCombatLossReceipt
{
    public CampaignCombatLossReceipt(string receiptId, string predecessorReceiptId,
        IEnumerable<CampaignCombatRoleLoss> roles)
    {
        ReceiptId = ContentContractGuards.RequireStableId(receiptId, nameof(receiptId));
        PredecessorReceiptId = ContentContractGuards.RequireStableId(predecessorReceiptId, nameof(predecessorReceiptId));
        var copy = ContentContractGuards.CopyValues(roles, nameof(roles));
        if (copy.Length != 2 || copy.Select(value => value.Role).Distinct(StringComparer.Ordinal).Count() != 2)
            throw new ArgumentException("Loss receipt requires one role loss per side.", nameof(roles));
        Roles = Array.AsReadOnly(copy.OrderBy(value => value.Role, StringComparer.Ordinal).ToArray());
    }
    public string ReceiptId { get; }
    public string PredecessorReceiptId { get; }
    public IReadOnlyList<CampaignCombatRoleLoss> Roles { get; }
    public bool Equals(CampaignCombatLossReceipt? other) => ReferenceEquals(this, other) ||
        (other is not null && ReceiptId == other.ReceiptId && PredecessorReceiptId == other.PredecessorReceiptId &&
         Roles.SequenceEqual(other.Roles));
    public override int GetHashCode() => HashCode.Combine(ReceiptId, PredecessorReceiptId);
}

internal sealed record CampaignCombatRetreatReceipt
{
    public CampaignCombatRetreatReceipt(string receiptId, string predecessorReceiptId, string kind,
        IEnumerable<string> route, int completedDistance, CapabilityPointAmount beforeCp,
        CapabilityPointAmount afterCp, int excessCpDp, int attackerVictoryRp)
    {
        ReceiptId = ContentContractGuards.RequireStableId(receiptId, nameof(receiptId));
        PredecessorReceiptId = ContentContractGuards.RequireStableId(predecessorReceiptId, nameof(predecessorReceiptId));
        Kind = kind is "retreat" or "refuse-retreat" or "not-required" ? kind
            : throw new ArgumentException("Unknown retreat outcome.", nameof(kind));
        Route = CampaignCombatRetreatDisposition.CopyRoute(route);
        if (completedDistance != Route.Count - 1) throw new ArgumentException("Retreat route distance differs.", nameof(completedDistance));
        BeforeCp = beforeCp ?? throw new ArgumentNullException(nameof(beforeCp));
        AfterCp = afterCp ?? throw new ArgumentNullException(nameof(afterCp));
        if (afterCp < beforeCp || excessCpDp < 0 || attackerVictoryRp < 0)
            throw new ArgumentException("Retreat cannot refund CP or cause balances.", nameof(afterCp));
        if (beforeCp.Denominator != 1 || afterCp.Denominator != 1)
            throw new ArgumentException("Selected retreat requires integer CP.", nameof(beforeCp));
        var actualRetreat = Kind == "retreat";
        if (completedDistance != (actualRetreat ? 1 : 0) ||
            afterCp.Numerator - beforeCp.Numerator != (actualRetreat ? 1 : 0) ||
            excessCpDp != Math.Max(0L, afterCp.Numerator - 10) - Math.Max(0L, beforeCp.Numerator - 10) ||
            attackerVictoryRp != (actualRetreat ? 3 : 0))
            throw new ArgumentException("Retreat CP and effects do not match selected outcome.", nameof(afterCp));
        CompletedDistance = completedDistance;
        ExcessCpDp = excessCpDp;
        AttackerVictoryRp = attackerVictoryRp;
    }
    public string ReceiptId { get; }
    public string PredecessorReceiptId { get; }
    public string Kind { get; }
    public IReadOnlyList<string> Route { get; }
    public int CompletedDistance { get; }
    public CapabilityPointAmount BeforeCp { get; }
    public CapabilityPointAmount AfterCp { get; }
    public int ExcessCpDp { get; }
    public int AttackerVictoryRp { get; }
    public bool Equals(CampaignCombatRetreatReceipt? other) => ReferenceEquals(this, other) ||
        (other is not null && ReceiptId == other.ReceiptId && PredecessorReceiptId == other.PredecessorReceiptId &&
         Kind == other.Kind && Route.SequenceEqual(other.Route) && CompletedDistance == other.CompletedDistance &&
         BeforeCp == other.BeforeCp && AfterCp == other.AfterCp && ExcessCpDp == other.ExcessCpDp &&
         AttackerVictoryRp == other.AttackerVictoryRp);
    public override int GetHashCode() => HashCode.Combine(ReceiptId, PredecessorReceiptId, Kind);
}

internal sealed record CampaignCombatCustodyReceipt
{
    public CampaignCombatCustodyReceipt(string receiptId, string predecessorReceiptId, string lotId,
        string kind, IEnumerable<string> route, string? guardId, string? entitlementId,
        int donorToeBefore, int donorToeAfter)
    {
        ReceiptId = ContentContractGuards.RequireStableId(receiptId, nameof(receiptId));
        PredecessorReceiptId = ContentContractGuards.RequireStableId(predecessorReceiptId, nameof(predecessorReceiptId));
        LotId = ContentContractGuards.RequireStableId(lotId, nameof(lotId));
        Kind = kind is "relocate-and-guard" or "leave-unguarded" ? kind
            : throw new ArgumentException("Unknown custody disposition.", nameof(kind));
        Route = CampaignCombatRetreatDisposition.CopyRoute(route);
        if (Route.Count - 1 > (kind == "relocate-and-guard" ? 3 : 4))
            throw new ArgumentException("Custody route exceeds the selected source bound.", nameof(route));
        GuardId = guardId is null ? null : ContentContractGuards.RequireStableId(guardId, nameof(guardId));
        EntitlementId = entitlementId is null ? null : ContentContractGuards.RequireStableId(entitlementId, nameof(entitlementId));
        if (donorToeBefore < 0 || donorToeAfter < 0 ||
            (kind == "relocate-and-guard" && (donorToeBefore - donorToeAfter != 1 || GuardId is null || EntitlementId is not null)) ||
            (kind == "leave-unguarded" && (donorToeBefore != donorToeAfter || GuardId is not null || EntitlementId is null)))
            throw new ArgumentException("Custody receipt does not conserve donor TOE or asset links.", nameof(donorToeAfter));
        DonorToeBefore = donorToeBefore;
        DonorToeAfter = donorToeAfter;
    }
    public string ReceiptId { get; }
    public string PredecessorReceiptId { get; }
    public string LotId { get; }
    public string Kind { get; }
    public IReadOnlyList<string> Route { get; }
    public string? GuardId { get; }
    public string? EntitlementId { get; }
    public int DonorToeBefore { get; }
    public int DonorToeAfter { get; }
    public bool Equals(CampaignCombatCustodyReceipt? other) => ReferenceEquals(this, other) ||
        (other is not null && ReceiptId == other.ReceiptId && PredecessorReceiptId == other.PredecessorReceiptId &&
         LotId == other.LotId && Kind == other.Kind && Route.SequenceEqual(other.Route) &&
         GuardId == other.GuardId && EntitlementId == other.EntitlementId &&
         DonorToeBefore == other.DonorToeBefore && DonorToeAfter == other.DonorToeAfter);
    public override int GetHashCode() => HashCode.Combine(ReceiptId, PredecessorReceiptId, LotId);
}

internal sealed record CampaignCombatRelationshipsReceipt
{
    public CampaignCombatRelationshipsReceipt(string receiptId, string predecessorReceiptId,
        string? relationshipId, string? kind)
    {
        ReceiptId = ContentContractGuards.RequireStableId(receiptId, nameof(receiptId));
        PredecessorReceiptId = ContentContractGuards.RequireStableId(predecessorReceiptId, nameof(predecessorReceiptId));
        if ((relationshipId is null) != (kind is null) || kind is not (null or "contact" or "engaged"))
            throw new ArgumentException("Relationship receipt ID and kind must agree.", nameof(kind));
        RelationshipId = relationshipId is null ? null : ContentContractGuards.RequireStableId(relationshipId, nameof(relationshipId));
        Kind = kind;
    }
    public string ReceiptId { get; }
    public string PredecessorReceiptId { get; }
    public string? RelationshipId { get; }
    public string? Kind { get; }
}
