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

internal sealed record CompleteMovementSegment(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide ActingSide) : CampaignCommand(1, ExpectedStateVersion);
