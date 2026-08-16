using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

internal static class CampaignTestHarness
{
    public static CreateCampaign Create(
        string campaignId,
        string rulesetHash,
        ulong seed,
        string setupId,
        string setupHash)
    {
        var setup = Cna1979SetupCatalog.TryGet(setupId, out var known)
            ? known
            : Cna1979SetupCatalog.Definitions[0];
        return new CreateCampaign(
            campaignId,
            rulesetHash,
            seed,
            setupId,
            setupHash,
            setup.Content.Pack.PackId,
            setup.Content.Pack.Hash,
            setup.Content.ScenarioId);
    }

    public static CampaignCommandResult Decide(
        CampaignSnapshot? snapshot,
        CampaignCommand command) => command is CreateCampaign create
            ? CampaignEngine.DecideCreation(
                snapshot,
                create,
                Cna1979SyntheticContentResolver.Instance)
            : CampaignEngine.Decide(snapshot, command, ContextFor(snapshot));

    public static CampaignSnapshot Apply(
        CampaignSnapshot? snapshot,
        CampaignEvent campaignEvent) => CampaignProjector.Apply(
            snapshot,
            campaignEvent,
            snapshot is null && campaignEvent is CampaignCreated created
                ? ContextFor(created.Setup)
                : ContextFor(snapshot));

    public static CampaignSnapshot Replay(IEnumerable<CampaignEvent> events)
    {
        var copy = events.ToArray();
        var created = copy.OfType<CampaignCreated>().FirstOrDefault()
            ?? throw new InvalidCampaignHistoryException(
                "Campaign history must contain a creation event.");
        return CampaignProjector.Replay(copy, ContextFor(created.Setup));
    }

    public static CampaignReplayResult Execute(IEnumerable<CampaignCommand> commands) =>
        CampaignReplayHarness.Execute(
            commands,
            Cna1979SyntheticContentResolver.Instance);

    public static CampaignContentContext ContextFor(CampaignSnapshot? snapshot) =>
        ContextFor(snapshot?.Setup ?? CampaignSetupSnapshot.FromDefinition(
            Cna1979SetupCatalog.Definitions[0]));

    public static CampaignContentContext ContextFor(CampaignSetupSnapshot setup) =>
        CampaignContentContext.Create(
            Cna1979SyntheticContentCatalog.Artifact,
            setup.Content.ScenarioId);
}
