using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record MoveElement(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide ActingSide,
    string CandidateId,
    string ElementId,
    string OriginLocationId,
    string DestinationLocationId) : CampaignCommand(1, ExpectedStateVersion);
