using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

internal sealed record CampaignObservationV6DisclosureCapability(
    string AuthorityId,
    IReadOnlyList<ObservedReactionMoveOption> MoveOptions,
    bool IsActive);

internal sealed record CampaignObservationV6DisclosureAlias(
    string AuthorityId,
    IReadOnlyList<ObservedReactionMoveOption> MoveOptions,
    bool IsActive,
    string PublicId);

internal static class CampaignObservationV6DisclosureIdentity
{
    public static string CreateWindow(
        string campaignId,
        string rulesetHash,
        long committedStateVersion,
        LandSide reactingSide)
    {
        campaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        if (!Cna1979Ruleset.IsCanonicalHash(rulesetHash))
        {
            throw new ArgumentException(
                "A ruleset hash must be canonical.",
                nameof(rulesetHash));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(committedStateVersion, 1);
        if (!Enum.IsDefined(reactingSide))
        {
            throw new ArgumentOutOfRangeException(nameof(reactingSide));
        }

        return Hash(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("domain", "sandtable.observation.reaction-window.v1");
            writer.WriteString("campaignId", campaignId);
            writer.WriteString("rulesetHash", rulesetHash);
            writer.WriteNumber("committedStateVersion", committedStateVersion);
            writer.WriteString("reactingSide", FormatSide(reactingSide));
            writer.WriteEndObject();
        });
    }

    public static string CreateCapabilityKey(
        IEnumerable<ObservedReactionMoveOption> moveOptions)
    {
        var options = ContentContractGuards.CopyValues(moveOptions, nameof(moveOptions));
        return Hash(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("domain", "sandtable.observation.reaction-capability.v1");
            writer.WriteStartArray("moveOptions");
            foreach (var option in options
                .OrderBy(value => value.OriginLocationId, StringComparer.Ordinal)
                .ThenBy(value => value.DestinationLocationId, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("originLocationId", option.OriginLocationId);
                writer.WriteString("destinationLocationId", option.DestinationLocationId);
                MovementActionJson.WriteCostBreakdown(writer, option.CostBreakdown);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        });
    }

    public static string CreateOpportunity(
        string publicWindowId,
        long stateVersion,
        string capabilityKey)
    {
        publicWindowId = ContentContractGuards.RequireSha256(
            publicWindowId,
            nameof(publicWindowId));
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        capabilityKey = ContentContractGuards.RequireSha256(
            capabilityKey,
            nameof(capabilityKey));
        return Hash(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("domain", "sandtable.observation.reaction-opportunity.v2");
            writer.WriteString("windowId", publicWindowId);
            writer.WriteNumber("stateVersion", stateVersion);
            writer.WriteString("capabilityKey", capabilityKey);
            writer.WriteEndObject();
        });
    }

    public static IReadOnlyList<CampaignObservationV6DisclosureAlias> CreateAliases(
        string publicWindowId,
        long stateVersion,
        IEnumerable<CampaignObservationV6DisclosureCapability> capabilities)
    {
        var values = ContentContractGuards.CopyValues(capabilities, nameof(capabilities));
        if (values.Select(value => value.AuthorityId)
                .Distinct(StringComparer.Ordinal).Count() != values.Length
            || values.Count(value => value.IsActive) > 1)
        {
            throw new ArgumentException(
                "Disclosure capabilities require unique authority IDs and at most one active participant.",
                nameof(capabilities));
        }

        var keyed = values.Select(value => new
        {
            Value = value,
            CapabilityKey = CreateCapabilityKey(value.MoveOptions),
        });
        var groups = keyed.GroupBy(value => value.CapabilityKey, StringComparer.Ordinal).ToArray();
        if (groups.Any(group => group.Skip(1).Any()))
        {
            throw new InvalidOperationException(
                "Indistinguishable Reaction capabilities cannot cross the user-space boundary without an explicit group-selection policy.");
        }

        return Array.AsReadOnly(groups
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.Single())
            .Select(value => new CampaignObservationV6DisclosureAlias(
                value.Value.AuthorityId,
                value.Value.MoveOptions,
                value.Value.IsActive,
                CreateOpportunity(
                    publicWindowId,
                    stateVersion,
                    value.CapabilityKey)))
            .ToArray());
    }

    public static void EnsureOpportunityIdentities(
        long stateVersion,
        CampaignObservationDecisionState decisionState)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        ArgumentNullException.ThrowIfNull(decisionState);
        if (decisionState is not CampaignObservationReactingDecisionState reacting)
        {
            return;
        }

        foreach (var opportunity in reacting.OwnOpportunities)
        {
            var expected = CreateOpportunity(
                reacting.WindowId,
                stateVersion,
                CreateCapabilityKey(opportunity.MoveOptions));
            if (!string.Equals(
                    expected,
                    opportunity.OpportunityId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A disclosed Reaction opportunity ID must bind its public window, state version, and exact published capability.",
                    nameof(decisionState));
            }
        }
    }

    private static string Hash(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            write(writer);
        }

        return $"sha256:{Convert.ToHexString(SHA256.HashData(buffer.WrittenSpan)).ToLowerInvariant()}";
    }

    private static string FormatSide(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };
}
