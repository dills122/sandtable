namespace Cna.Core.Campaigns;

internal static class CampaignReplayHarness
{
    public static CampaignReplayResult Execute(
        IEnumerable<CampaignCommand> commands,
        IContentPackResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(resolver);

        var events = new List<CampaignEvent>();
        CampaignSnapshot? snapshot = null;
        CampaignContentContext? context = null;
        var commandIndex = 0;

        foreach (var command in commands)
        {
            var result = command is CreateCampaign create
                ? CampaignEngine.DecideCreation(snapshot, create, resolver)
                : context is null
                    ? CampaignCommandResult.Reject(
                        CampaignCommandRejectionReason.CampaignNotCreated)
                    : CampaignEngine.Decide(snapshot, command, context);

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
                if (campaignEvent is CampaignCreated created)
                {
                    var resolution = resolver.Resolve(
                        created.Setup.Content.Pack.PackId,
                        created.Setup.Content.Pack.Hash);
                    context = resolution.IsResolved
                        ? CampaignContentContext.Create(
                            resolution.Artifact!,
                            created.Setup.Content.ScenarioId)
                        : null;
                }

                if (context is null)
                {
                    return new CampaignReplayResult(
                        events,
                        snapshot,
                        CampaignCommandRejectionReason.InvalidState,
                        commandIndex);
                }

                snapshot = CampaignProjector.Apply(snapshot, campaignEvent, context);
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
