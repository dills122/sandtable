using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Content;

// Internal identity for a non-admitted candidate, never an authority or journal head.
internal sealed class CampaignArrivalDraftIdentity
{
    private readonly byte[] canonicalBytes;
    private readonly ReadOnlyDictionary<string, IReadOnlyList<string>> destinations;

    private CampaignArrivalDraftIdentity(ContentPackDefinition pack, string scenarioId,
        ScheduledArrivalGroup[] groups, Dictionary<string, IReadOnlyList<string>> destinations,
        string rulesetHash, string rulingHash)
    {
        Pack = pack;
        ScenarioId = scenarioId;
        Groups = Array.AsReadOnly(groups);
        this.destinations = new ReadOnlyDictionary<string, IReadOnlyList<string>>(destinations);
        RulesetHash = rulesetHash;
        RulingHash = rulingHash;
        canonicalBytes = Encode();
        Hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(canonicalBytes));
    }

    public ContentPackDefinition Pack { get; }
    public string ScenarioId { get; }
    public IReadOnlyList<ScheduledArrivalGroup> Groups { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Destinations => destinations;
    public string RulesetHash { get; }
    public string RulingHash { get; }
    public string Hash { get; }
    public byte[] CopyCanonicalBytes() => canonicalBytes.ToArray();

    public static CampaignArrivalDraftIdentity Create(ContentPackDefinition pack, string scenarioId,
        IEnumerable<ScheduledArrivalGroup> groups,
        IReadOnlyDictionary<string, IReadOnlyList<string>> allowedDestinations,
        string rulesetHash, string rulingHash)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(allowedDestinations);
        ContentContractGuards.RequireSha256(rulesetHash, nameof(rulesetHash));
        ContentContractGuards.RequireSha256(rulingHash, nameof(rulingHash));
        var groupCopy = groups.ToArray();
        var report = CampaignScheduledArrivalContentDraft.Analyze(pack, scenarioId, groupCopy);
        if (!report.DraftValidation.IsValid || !report.UnexplainedLegacyValidation.IsValid
            || !Cna1979ContentCompatibilityValidator.Validate(pack).IsValid)
            throw new ArgumentException("The arrival content candidate is invalid.", nameof(pack));

        var ids = groupCopy.Select(group => group.GroupId).ToHashSet(StringComparer.Ordinal);
        if (!ids.SetEquals(allowedDestinations.Keys))
            throw new ArgumentException("Each group needs exactly one destination list.", nameof(allowedDestinations));
        var knownLocations = pack.Locations.Select(location => location.LocationId).ToHashSet(StringComparer.Ordinal);
        var destinations = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var (groupId, values) in allowedDestinations)
        {
            ArgumentNullException.ThrowIfNull(values);
            var copy = values.Select(value => ContentContractGuards.RequireStableId(value, nameof(allowedDestinations)))
                .Order(StringComparer.Ordinal).ToArray();
            if (copy.Length == 0 || copy.Distinct(StringComparer.Ordinal).Count() != copy.Length
                || copy.Any(value => !knownLocations.Contains(value)))
                throw new ArgumentException("Destination lists must contain distinct known locations.", nameof(allowedDestinations));
            destinations.Add(groupId, Array.AsReadOnly(copy));
        }

        foreach (var source in groupCopy.SelectMany(group => group.Sources))
        {
            ContentContractGuards.RequireStableId(source.SourceId, nameof(groups));
            ContentContractGuards.RequireSourceAtom(source.Locator, nameof(groups));
        }
        Array.Sort(groupCopy, (left, right) =>
        {
            var order = left.GameTurn.CompareTo(right.GameTurn);
            if (order == 0) order = left.OperationStage.CompareTo(right.OperationStage);
            if (order == 0) order = left.Owner.CompareTo(right.Owner);
            return order == 0 ? StringComparer.Ordinal.Compare(left.GroupId, right.GroupId) : order;
        });
        return new CampaignArrivalDraftIdentity(pack, scenarioId, groupCopy, destinations, rulesetHash, rulingHash);
    }

    private byte[] Encode()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("format", "sandtable.campaign-arrival-draft.v1");
            writer.WritePropertyName("candidateContent");
            writer.WriteRawValue(ContentPackSerializer.SerializeDraftDefinition(Pack));
            writer.WriteString("scenarioId", ScenarioId);
            writer.WriteString("rulesetHash", RulesetHash);
            writer.WriteString("rulingHash", RulingHash);
            writer.WriteStartArray("groups");
            foreach (var group in Groups)
            {
                writer.WriteStartObject();
                writer.WriteString("id", group.GroupId);
                writer.WriteString("owner", group.Owner == LandSide.Axis ? "axis" : "commonwealth");
                writer.WriteNumber("turn", group.GameTurn);
                writer.WriteNumber("stage", group.OperationStage);
                writer.WriteStartArray("elements");
                foreach (var id in group.UnitIds) writer.WriteStringValue(id);
                writer.WriteEndArray();
                writer.WriteStartArray("sources");
                foreach (var source in group.Sources)
                {
                    writer.WriteStartObject();
                    writer.WriteString("sourceId", source.SourceId);
                    writer.WriteString("locator", source.Locator);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteStartArray("destinations");
                foreach (var id in destinations[group.GroupId]) writer.WriteStringValue(id);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
