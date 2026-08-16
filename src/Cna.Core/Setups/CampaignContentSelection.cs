using Cna.Core.Content;

namespace Cna.Core.Setups;

public sealed record CampaignContentSelection
{
    public CampaignContentSelection(ContentPackIdentity pack, string scenarioId)
    {
        ArgumentNullException.ThrowIfNull(pack);
        Pack = pack;
        ScenarioId = ContentContractGuards.RequireStableId(scenarioId, nameof(scenarioId));
    }

    public ContentPackIdentity Pack { get; }

    public string ScenarioId { get; }
}
