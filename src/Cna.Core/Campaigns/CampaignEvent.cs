using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

public abstract record CampaignEvent(
    int ContractVersion,
    string CampaignId,
    long StateVersion);

public sealed record CampaignCreated(
    string CampaignId,
    long StateVersion,
    string RulesetHash,
    CampaignSetupSnapshot Setup,
    RandomStreamState RandomState,
    LandSequencePosition SequencePosition) : CampaignEvent(2, CampaignId, StateVersion);

public sealed record CampaignSequenceAdvanced(
    string CampaignId,
    long StateVersion,
    string FromPositionId,
    LandSequencePosition SequencePosition) : CampaignEvent(1, CampaignId, StateVersion);
