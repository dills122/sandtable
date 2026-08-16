namespace Cna.Core.Campaigns;

public abstract record CampaignCommand(int ContractVersion, long ExpectedStateVersion);

public sealed record CreateCampaign(
    string CampaignId,
    string RulesetHash,
    ulong Seed,
    string SetupId,
    string SetupHash,
    string ContentPackId,
    string ContentHash,
    string ScenarioId) : CampaignCommand(3, 0);

public sealed record CompleteCurrentSequenceStep(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(2, ExpectedStateVersion);

public sealed record ResolveInitiative(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(2, ExpectedStateVersion);
