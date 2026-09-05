using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

internal sealed record CampaignProjectedDecisionHistoryV2Entry
{
    public const int CurrentContractVersion = 2;

    public CampaignProjectedDecisionHistoryV2Entry(
        int contractVersion,
        string campaignId,
        long stateVersion,
        LandSide observer,
        CampaignObservationV7DecisionState decisionState)
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
        CampaignObservationV7DisclosureIdentity.EnsureOpportunityIdentities(
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

    public CampaignObservationV7DecisionState DecisionState { get; }
}

internal static class CampaignProjectedDecisionHistoryV2
{
    public static CampaignProjectedDecisionHistoryV2Entry Project(
        CampaignObservationV7 observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        return new CampaignProjectedDecisionHistoryV2Entry(
            CampaignProjectedDecisionHistoryV2Entry.CurrentContractVersion,
            observation.CampaignId,
            observation.StateVersion,
            observation.Observer,
            observation.DecisionState);
    }
}
