namespace Cna.Core.Rules;

internal enum CombatRole
{
    Attacker,
    Defender,
}

internal sealed record CombatSourceEvidence(string SourceId, string Sha256);
internal sealed record CombatMoraleCell(int Coordinate, int Adjustment);
internal sealed record CombatLossCell(CombatRole Role, int Differential, int Coordinate, int LossPercent);
internal sealed record CombatCaptureShare(int Die, int Percent);
internal sealed record CombatPolicy(string PolicyId, int ContractVersion, string SelectedBehaviorId);

internal sealed record CombatSourceAmendment
{
    public CombatSourceAmendment(string rulingId, CombatRole role, int differential,
        IEnumerable<int> coordinates, int lossPercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulingId);
        RulingId = rulingId;
        Role = role;
        Differential = differential;
        Coordinates = CombatRuleCollections.Copy(coordinates);
        LossPercent = lossPercent;
    }

    public string RulingId { get; }
    public CombatRole Role { get; }
    public int Differential { get; }
    public IReadOnlyList<int> Coordinates { get; }
    public int LossPercent { get; }
}

internal sealed record CombatEffectRow
{
    public CombatEffectRow(int differential, IEnumerable<int> attackerEngagedSums,
        IEnumerable<int> defenderRetreatOneHexSums, IEnumerable<int> attackerCaptureSums,
        IEnumerable<int> defenderCaptureSums)
    {
        Differential = differential;
        AttackerEngagedSums = CombatRuleCollections.Copy(attackerEngagedSums);
        DefenderRetreatOneHexSums = CombatRuleCollections.Copy(defenderRetreatOneHexSums);
        AttackerCaptureSums = CombatRuleCollections.Copy(attackerCaptureSums);
        DefenderCaptureSums = CombatRuleCollections.Copy(defenderCaptureSums);
    }

    public int Differential { get; }
    public IReadOnlyList<int> AttackerEngagedSums { get; }
    public IReadOnlyList<int> DefenderRetreatOneHexSums { get; }
    public IReadOnlyList<int> AttackerCaptureSums { get; }
    public IReadOnlyList<int> DefenderCaptureSums { get; }
}

internal sealed record CombatProcedure
{
    public CombatProcedure(string procedureId, string streamAlgorithm, int acceptedByteUpperExclusive,
        int faces, IEnumerable<string> orderedPurposes, IEnumerable<string> conditionalCapturePurposes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureId);
        ArgumentException.ThrowIfNullOrWhiteSpace(streamAlgorithm);
        ProcedureId = procedureId;
        StreamAlgorithm = streamAlgorithm;
        AcceptedByteUpperExclusive = acceptedByteUpperExclusive;
        Faces = faces;
        OrderedPurposes = CombatRuleCollections.Copy(orderedPurposes);
        ConditionalCapturePurposes = CombatRuleCollections.Copy(conditionalCapturePurposes);
    }

    public string ProcedureId { get; }
    public string StreamAlgorithm { get; }
    public int AcceptedByteUpperExclusive { get; }
    public int Faces { get; }
    public IReadOnlyList<string> OrderedPurposes { get; }
    public IReadOnlyList<string> ConditionalCapturePurposes { get; }
}

internal sealed record CombatCosts(
    int CommittedToePerRole,
    int RatingPerRole,
    int BasicMoralePerRole,
    int RequiredCohesionPerRole,
    int BaseCapabilityPointAllowance,
    int AttackerCapabilityPoints,
    int DefenderCapabilityPoints,
    int AmmunitionPointsPerToe);

internal sealed record CombatSettlementRules(
    string AttackerLossRounding,
    string DefenderLossRounding,
    string CapturedLossRounding,
    int RefusalPercentPerHex,
    int LossDpThresholdToe,
    int LossDpPoints,
    int VictoryRpPoints,
    int CohesionUpperCap,
    int ClearRetreatCapabilityPointsPerHex,
    int GuardToe,
    int GuardCapabilityPointAllowance,
    int GuardAttackRating,
    int GuardDefenseRating,
    int GuardInitialAmmunition,
    int GuardedPathMaximumHexes,
    int EscapePathMaximumCapabilityPoints,
    int ReplacementDelayOperationStages,
    int OperationStagesPerGameTurn);

/// <summary>Complete selected RulesInput1 value; every collection owns a read-only copy.</summary>
internal sealed record CombatRulesInputDefinition
{
    public CombatRulesInputDefinition(int schemaVersion, string artifactId, string profileId,
        IEnumerable<RuleReference> sources, IEnumerable<CombatSourceEvidence> sourceEvidence,
        CombatSourceAmendment amendment, IEnumerable<CombatMoraleCell> morale,
        IEnumerable<CombatLossCell> losses, IEnumerable<CombatEffectRow> effects,
        IEnumerable<CombatCaptureShare> captureShares, CombatProcedure procedure,
        CombatCosts costs, CombatSettlementRules settlement, IEnumerable<CombatPolicy> policies)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentNullException.ThrowIfNull(amendment);
        ArgumentNullException.ThrowIfNull(procedure);
        ArgumentNullException.ThrowIfNull(costs);
        ArgumentNullException.ThrowIfNull(settlement);
        SchemaVersion = schemaVersion;
        ArtifactId = artifactId;
        ProfileId = profileId;
        Sources = CombatRuleCollections.Copy(sources);
        SourceEvidence = CombatRuleCollections.Copy(sourceEvidence);
        Amendment = amendment;
        Morale = CombatRuleCollections.Copy(morale);
        Losses = CombatRuleCollections.Copy(losses);
        Effects = CombatRuleCollections.Copy(effects);
        CaptureShares = CombatRuleCollections.Copy(captureShares);
        Procedure = procedure;
        Costs = costs;
        Settlement = settlement;
        Policies = CombatRuleCollections.Copy(policies);
    }

    public int SchemaVersion { get; }
    public string ArtifactId { get; }
    public string ProfileId { get; }
    public IReadOnlyList<RuleReference> Sources { get; }
    public IReadOnlyList<CombatSourceEvidence> SourceEvidence { get; }
    public CombatSourceAmendment Amendment { get; }
    public IReadOnlyList<CombatMoraleCell> Morale { get; }
    public IReadOnlyList<CombatLossCell> Losses { get; }
    public IReadOnlyList<CombatEffectRow> Effects { get; }
    public IReadOnlyList<CombatCaptureShare> CaptureShares { get; }
    public CombatProcedure Procedure { get; }
    public CombatCosts Costs { get; }
    public CombatSettlementRules Settlement { get; }
    public IReadOnlyList<CombatPolicy> Policies { get; }
}

internal sealed record CombatRoleLoss(int TotalLoss, int CapturedLoss, int DestroyedLoss,
    int RemainingToe, int LossDp);

/// <summary>Pure selected-profile arithmetic, before World settlement or victory proof.</summary>
internal sealed record CombatSelectedResult(int Differential, int AttackerLossPercent,
    int DefenderBaseLossPercent, int DefenderLossPercent, bool RawEngaged,
    int DefenderRetreatHexes, bool RefusedRetreat, CombatRole? CapturedRole,
    int? CaptureSharePercent, CombatRoleLoss Attacker, CombatRoleLoss Defender);

internal static class CombatRuleCollections
{
    public static IReadOnlyList<T> Copy<T>(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.ToArray();
        if (copy.Any(value => value is null))
        {
            throw new ArgumentException("Rule collections cannot contain null values.", nameof(values));
        }
        return Array.AsReadOnly(copy);
    }
}
