using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

internal sealed record CampaignProjectedDecisionHistoryEntry
{
    public const int CurrentContractVersion = 1;

    public CampaignProjectedDecisionHistoryEntry(
        int contractVersion,
        string campaignId,
        long stateVersion,
        LandSide observer,
        CampaignObservationDecisionState decisionState)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractVersion,
            CurrentContractVersion);
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        if (!Enum.IsDefined(observer))
        {
            throw new ArgumentOutOfRangeException(nameof(observer));
        }

        ArgumentNullException.ThrowIfNull(decisionState);
        CampaignObservationV6DisclosureIdentity.EnsureOpportunityIdentities(
            stateVersion,
            decisionState);
        CampaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        ContractVersion = contractVersion;
        StateVersion = stateVersion;
        Observer = observer;
        DecisionState = decisionState;
    }

    public int ContractVersion { get; }

    public string CampaignId { get; }

    public long StateVersion { get; }

    public LandSide Observer { get; }

    public CampaignObservationDecisionState DecisionState { get; }
}

internal static class CampaignProjectedDecisionHistory
{
    public static CampaignProjectedDecisionHistoryEntry Project(
        CampaignObservationV6 observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        return new CampaignProjectedDecisionHistoryEntry(
            CampaignProjectedDecisionHistoryEntry.CurrentContractVersion,
            observation.CampaignId,
            observation.StateVersion,
            observation.Observer,
            observation.DecisionState);
    }
}
