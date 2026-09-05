using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

internal static class CampaignObservationV7DisclosureIdentity
{
    public static void EnsureOpportunityIdentities(long stateVersion, CampaignObservationV7DecisionState state)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        ArgumentNullException.ThrowIfNull(state);
        if (state is CampaignObservationV7ReactingDecisionState reacting)
            CampaignObservationV6DisclosureIdentity.EnsureOpportunityIdentities(stateVersion,
                new CampaignObservationReactingDecisionState(reacting.WindowId, reacting.ApparentTrigger,
                    reacting.OwnOpportunities, reacting.ActiveParticipant));
    }

    public static string CreateWindow(string campaignId, string rulesetHash, long committedStateVersion, LandSide reactingSide)
    {
        ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        if (!Cna1979BreakdownRuleset.IsCanonicalHash(rulesetHash)) throw new ArgumentException("Invalid ruleset hash.");
        ArgumentOutOfRangeException.ThrowIfLessThan(committedStateVersion, 1);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("domain", "sandtable.observation.reaction-window.v1");
            writer.WriteString("campaignId", campaignId);
            writer.WriteString("rulesetHash", rulesetHash);
            writer.WriteNumber("committedStateVersion", committedStateVersion);
            writer.WriteString("reactingSide", CampaignObservationSerializer.FormatSide(reactingSide));
            writer.WriteEndObject();
        }
        return $"sha256:{Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()))}";
    }

    public static string CreateRoute(string campaignId, string rulesetHash, long stateVersion, LandSide audience,
        string elementId, string originLocationId, string currentLocationId)
    {
        ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        if (!Cna1979BreakdownRuleset.IsCanonicalHash(rulesetHash)) throw new ArgumentException("Invalid ruleset hash.");
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        ContentContractGuards.RequireStableId(originLocationId, nameof(originLocationId));
        ContentContractGuards.RequireStableId(currentLocationId, nameof(currentLocationId));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("domain", "sandtable.observation.movement-route.v1");
            writer.WriteString("campaignId", campaignId);
            writer.WriteString("rulesetHash", rulesetHash);
            writer.WriteNumber("stateVersion", stateVersion);
            writer.WriteString("audience", CampaignObservationSerializer.FormatSide(audience));
            writer.WriteString("elementId", elementId);
            writer.WriteString("originLocationId", originLocationId);
            writer.WriteString("currentLocationId", currentLocationId);
            writer.WriteEndObject();
        }
        return $"sha256:{Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()))}";
    }
}
