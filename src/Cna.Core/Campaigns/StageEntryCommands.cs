namespace Cna.Core.Campaigns;

internal sealed record ResolveNoObligationOrganization(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(1, ExpectedStateVersion);

internal sealed record ResolveNoObligationNavalConvoyArrival(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(1, ExpectedStateVersion);

internal sealed record ResolveNoObligationFleetAssignment(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(1, ExpectedStateVersion);

internal sealed record ResolveNoObligationFleetRepair(
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand(1, ExpectedStateVersion);
