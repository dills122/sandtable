namespace Cna.Core.Campaigns;

public static class CampaignReplayHarness
{
    public static CampaignReplayResult Execute(IEnumerable<CampaignCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        var events = new List<CampaignEvent>();
        CampaignSnapshot? snapshot = null;
        var commandIndex = 0;

        foreach (var command in commands)
        {
            var result = CampaignEngine.Decide(snapshot, command);

            if (!result.IsAccepted)
            {
                return new CampaignReplayResult(
                    events,
                    snapshot,
                    result.RejectionReason,
                    commandIndex);
            }

            foreach (var campaignEvent in result.Events)
            {
                snapshot = CampaignProjector.Apply(snapshot, campaignEvent);
                events.Add(campaignEvent);
            }

            commandIndex++;
        }

        return snapshot is null
            ? new CampaignReplayResult(
                events,
                null,
                CampaignCommandRejectionReason.CampaignNotCreated,
                null)
            : new CampaignReplayResult(
                events,
                snapshot,
                CampaignCommandRejectionReason.None,
                null);
    }
}
