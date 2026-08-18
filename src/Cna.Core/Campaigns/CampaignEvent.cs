using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal abstract record CampaignEvent(
    int ContractVersion,
    string CampaignId,
    long StateVersion);

internal sealed record CampaignCreated(
    string CampaignId,
    long StateVersion,
    string RulesetHash,
    CampaignSetupSnapshot Setup,
    CampaignWorldSnapshot InitialWorld,
    RandomStreamState RandomState,
    LandSequencePosition SequencePosition) : CampaignEvent(4, CampaignId, StateVersion);

internal sealed record CampaignSequenceAdvanced(
    string CampaignId,
    long StateVersion,
    string FromPositionId,
    LandSequencePosition SequencePosition) : CampaignEvent(1, CampaignId, StateVersion);
