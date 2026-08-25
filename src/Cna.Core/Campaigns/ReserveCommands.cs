using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record DesignateReserveElement(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide ActingSide,
    string ElementId) : CampaignCommand(1, ExpectedStateVersion);

internal sealed record CompleteReserveDesignation(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide ActingSide) : CampaignCommand(1, ExpectedStateVersion);
