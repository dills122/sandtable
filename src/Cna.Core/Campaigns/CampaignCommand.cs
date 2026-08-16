using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

public abstract record CampaignCommand(int ContractVersion, long ExpectedStateVersion);

public sealed record CreateCampaign(
    string CampaignId,
    string RulesetHash,
    ulong Seed,
    LandSide FirstPlayer) : CampaignCommand(1, 0);

public sealed record CompleteCurrentSequenceStep(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(1, ExpectedStateVersion);
