using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

public sealed record CampaignLegalActionSet
{
    public const int CurrentContractVersion = 2;
    public const string CurrentPolicyId = "sandtable.legal-actions.v3";
    public const string PregamePolicyId = "sandtable.legal-actions.graziani-pregame.v1";
    public const string SourceSetupPolicyId = "sandtable.legal-actions.graziani-source-setup.v1";
    public const string SourceTurnPolicyId = "sandtable.legal-actions.graziani-source-turn.v1";
    /// <summary>
    /// Task 022A: first-cycle Combat steps for the trusted in-process Exercise harness only
    /// (<c>CampaignExercises.QueryWithCombat</c>). Not served by the Host; not Combat activation.
    /// </summary>
    public const string CombatExercisePolicyId = "sandtable.legal-actions.combat-exercise.v1";
    internal const string HistoricalPolicyIdV2 = "sandtable.legal-actions.v2";

    internal CampaignLegalActionSet(string campaignId, long stateVersion, string rulesetHash,
        string positionId, CampaignActionAudience audience,
        IReadOnlyList<CampaignActionCandidate> candidates,
        string policyId = CurrentPolicyId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        if (policyId != CurrentPolicyId && policyId != HistoricalPolicyIdV2 && policyId != PregamePolicyId && policyId != SourceSetupPolicyId && policyId != SourceTurnPolicyId
            && policyId != CombatExercisePolicyId)
            throw new ArgumentException("Unsupported legal-action policy.", nameof(policyId));
        if (!(policyId == HistoricalPolicyIdV2
                ? Cna1979Ruleset.IsHistoricalHashV8(rulesetHash)
                : Cna1979Ruleset.IsCanonicalHash(rulesetHash)))
        {
            throw new ArgumentException("The action set must use the canonical ruleset hash.",
                nameof(rulesetHash));
        }
        if (!Enum.IsDefined(audience)) throw new ArgumentOutOfRangeException(nameof(audience));
        ArgumentNullException.ThrowIfNull(candidates);
        var copy = candidates.ToArray();
        if (copy.Any(candidate => candidate is null)
            || copy.Any(candidate => candidate.ContractVersion != CampaignActionCandidate.CurrentContractVersion)
            || copy.Select(candidate => candidate.ActionId).Distinct(StringComparer.Ordinal).Count()
                != copy.Length)
        {
            throw new ArgumentException("Candidates must be non-null, current, and unique.",
                nameof(candidates));
        }
        foreach (var candidate in copy)
        {
            _ = ContentContractGuards.RequireSha256(candidate.ActionId, nameof(candidates));
        }

        var hasPregameCandidate = copy.Any(candidate => candidate is PregameAxisSupplyRollAction or PregameAxisSupplyAllocationAction);
        if (policyId == PregamePolicyId)
        {
            if (audience != CampaignActionAudience.Axis || positionId != CampaignPregameSourceCreation.PositionId
                || copy.Any(candidate => candidate is not (PregameAxisSupplyRollAction or PregameAxisSupplyAllocationAction))
                || (stateVersion == 1 && (copy.Length != 1 || copy[0] is not PregameAxisSupplyRollAction))
                || (stateVersion > 1 && copy.Any(candidate => candidate is PregameAxisSupplyRollAction)))
                throw new ArgumentException("Pre-game policy requires the Axis setup cut and pre-game candidates.", nameof(candidates));
        }
        else if (policyId == SourceSetupPolicyId)
        {
            if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth)
                || positionId != CampaignSourceAuthorityState.SetupPositionId
                || copy.Any(candidate => candidate is not CampaignSourceSetupAction action || action.StateVersion != stateVersion))
                throw new ArgumentException("Source setup policy requires a side-private source setup cut.", nameof(candidates));
        }
        else if (policyId == SourceTurnPolicyId)
        {
            var arrival = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
                && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
            var firstArrival = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
                && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
            var firstMovement = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
                && value.ActorRole == LandActorRole.FirstActingSide && value.SegmentId == LandSegmentIds.Movement);
            var firstReserves = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
                && value.ActorRole == LandActorRole.FirstActingSide && value.PhaseId == LandPhaseIds.ReserveDesignation);
            var schedule = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PhaseId == LandPhaseIds.NavalConvoySchedule);
            var shipping = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PhaseId == LandPhaseIds.TacticalShipping);
            var declaration = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.InitiativeDeclaration);
            var organization = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.Organization);
            if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth)
                || (positionId != arrival.PositionId && positionId != firstArrival.PositionId && positionId != firstMovement.PositionId && positionId != firstReserves.PositionId && positionId != schedule.PositionId && positionId != shipping.PositionId && positionId != declaration.PositionId && positionId != organization.PositionId)
                || copy.Any(candidate => candidate is not CampaignSourceTurnAction action || action.StateVersion != stateVersion)
                || (positionId == arrival.PositionId && copy.Any(candidate => candidate.Kind != "source-polish-arrival"))
                || (positionId == firstMovement.PositionId && copy.Any(candidate => candidate.Kind is not ("source-fourth-indian-opening-move" or "source-stage1-first-movement-finish")))
                || (positionId == firstReserves.PositionId && copy.Any(candidate => candidate.Kind != "source-stage1-first-reserves-decline"))
                || (positionId == firstMovement.PositionId && audience != CampaignActionAudience.Commonwealth && copy.Any(candidate => candidate.Kind != "source-stage1-first-movement-finish"))
                || (positionId == firstArrival.PositionId && copy.Any(candidate => candidate.Kind is not ("source-gt1-lane2-attack-decline" or "source-gt1-lane3-attack-decline" or "source-gt1-lane6-attack-decline")))
                || (positionId == firstArrival.PositionId && audience != CampaignActionAudience.Commonwealth && copy.Length != 0)
                || (positionId == schedule.PositionId && copy.Any(candidate => audience == CampaignActionAudience.Axis
                    ? candidate.Kind is not ("source-axis-gt3-supply-roll" or "source-axis-gt3-supply-adjust" or "source-axis-gt3-supply-finish")
                    : candidate.Kind != "source-commonwealth-gt5-supply-roll"))
                || (positionId == shipping.PositionId && copy.Any(candidate => candidate.Kind != "source-tactical-shipping-decline"))
                || (positionId == declaration.PositionId && copy.Any(candidate => candidate.Kind is not ("source-stage1-act-first" or "source-stage1-act-last")))
                || (positionId == declaration.PositionId && audience != CampaignActionAudience.Axis && copy.Length != 0)
                || (positionId == organization.PositionId && copy.Any(candidate => candidate.Kind is not ("source-voluntary-organization-decline" or "source-sixth-australian-training-interrupt" or "source-australian-baseline-approve" or "source-australian-baseline-withdraw")))
                || (positionId == organization.PositionId && audience != CampaignActionAudience.Commonwealth && copy.Any(candidate => candidate.Kind == "source-sixth-australian-training-interrupt"))
                || (positionId == arrival.PositionId && audience != CampaignActionAudience.Commonwealth && copy.Length != 0)
                )
                throw new ArgumentException("Source turn policy requires an owner-specific source turn cut.", nameof(candidates));
        }
        else if (policyId == CombatExercisePolicyId)
        {
            if (audience == CampaignActionAudience.System
                    ? copy.Any(candidate => candidate is not CombatExerciseSystemAction)
                    : copy.Any(candidate => candidate is not (SelectContactAssaultAction or FinishCombatWithoutAttackAction
                        or DeclineRetreatBeforeAssaultAction or SealCombatForceAssignmentAction
                        or CombatExerciseOwnerChoiceAction or DesignateReserveAction
                        or CompleteReserveDesignationAction)))
                throw new ArgumentException("Combat Exercise policy requires first-cycle Combat candidates for the audience.", nameof(candidates));
        }
        else if (hasPregameCandidate || copy.Any(candidate => candidate is CampaignSourceSetupAction or CampaignSourceTurnAction))
            throw new ArgumentException("Pre-game candidates require the pre-game policy.", nameof(candidates));
        if (policyId != CombatExercisePolicyId
            && copy.Any(candidate => candidate is CombatExerciseSystemAction))
            throw new ArgumentException("Combat Exercise System steps require the Combat Exercise policy.", nameof(candidates));

        ContractVersion = CurrentContractVersion;
        PolicyId = policyId;
        CampaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        StateVersion = stateVersion;
        RulesetHash = rulesetHash;
        PositionId = ContentContractGuards.RequireStableId(positionId, nameof(positionId));
        Audience = audience;
        Candidates = Array.AsReadOnly(policyId == SourceSetupPolicyId
            ? copy.Cast<CampaignSourceSetupAction>().OrderBy(candidate => candidate.Family, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.ChoiceJson, StringComparer.Ordinal).Cast<CampaignActionCandidate>().ToArray()
            : copy.OrderBy(candidate => candidate.Kind, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.ActionId, StringComparer.Ordinal).ToArray());
    }

    public int ContractVersion { get; }
    public string PolicyId { get; }
    public string CampaignId { get; }
    /// <summary>
    /// Gets the revision visible to this audience. A non-empty current set remains bound to the
    /// exact authority revision used by submission.
    /// </summary>
    public long StateVersion { get; }
    public string RulesetHash { get; }
    public string PositionId { get; }
    public CampaignActionAudience Audience { get; }
    public IReadOnlyList<CampaignActionCandidate> Candidates { get; }

    public bool Equals(CampaignLegalActionSet? other) => ReferenceEquals(this, other)
        || (other is not null && ContractVersion == other.ContractVersion && PolicyId == other.PolicyId
            && CampaignId == other.CampaignId && StateVersion == other.StateVersion
            && RulesetHash == other.RulesetHash && PositionId == other.PositionId
            && Audience == other.Audience && Candidates.SequenceEqual(other.Candidates));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion); hash.Add(PolicyId, StringComparer.Ordinal);
        hash.Add(CampaignId, StringComparer.Ordinal); hash.Add(StateVersion);
        hash.Add(RulesetHash, StringComparer.Ordinal); hash.Add(PositionId, StringComparer.Ordinal);
        hash.Add(Audience); foreach (var candidate in Candidates) hash.Add(candidate);
        return hash.ToHashCode();
    }
}

public enum CampaignLegalActionQueryRejectionReason
{
    None,
    InvalidAudience,
    InvalidState,
}

public sealed record CampaignLegalActionQueryResult
{
    private CampaignLegalActionQueryResult(CampaignLegalActionSet? actionSet,
        CampaignLegalActionQueryRejectionReason rejectionReason)
    { ActionSet = actionSet; RejectionReason = rejectionReason; }

    public bool IsSuccessful => ActionSet is not null;
    public CampaignLegalActionSet? ActionSet { get; }
    public CampaignLegalActionQueryRejectionReason RejectionReason { get; }
    internal static CampaignLegalActionQueryResult Success(CampaignLegalActionSet set) => new(set,
        CampaignLegalActionQueryRejectionReason.None);
    internal static CampaignLegalActionQueryResult Rejected(CampaignLegalActionQueryRejectionReason reason) =>
        new(null, reason);
}
