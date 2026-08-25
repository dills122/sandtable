using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal abstract record CampaignCommand(int ContractVersion, long ExpectedStateVersion);

internal sealed record CreateCampaign(
    string CampaignId,
    string RulesetHash,
    ulong Seed,
    string SetupId,
    string SetupHash,
    string ContentPackId,
    string ContentHash,
    string ScenarioId) : CampaignCommand(5, 0);

internal sealed record CompleteCurrentSequenceStep(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(2, ExpectedStateVersion);

internal sealed record ResolveInitiative(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(2, ExpectedStateVersion);

internal sealed record ResolveNoObligationNavalConvoySchedule(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(1, ExpectedStateVersion);

internal sealed record ResolveNoObligationTacticalShipping(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(1, ExpectedStateVersion);

internal sealed record ResolveWeather(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(1, ExpectedStateVersion);

internal enum InitiativeOrderChoice
{
    ActFirst = 1,
    ActLast = 2,
}

internal sealed record DeclareInitiativeOrder(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    int OperationStage,
    LandSide DeclaringSide,
    InitiativeOrderChoice Choice) : CampaignCommand(1, ExpectedStateVersion);
