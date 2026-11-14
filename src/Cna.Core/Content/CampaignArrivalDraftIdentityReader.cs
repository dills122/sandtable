using System.Text;
using System.Text.Json;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Content;

// Candidate reconstruction only: these bytes are not trusted campaign admission evidence.
internal static class CampaignArrivalDraftIdentityReader
{
    public static CampaignArrivalDraftIdentity Read(ReadOnlySpan<byte> bytes, string expectedHash)
    {
        if (bytes.Length is 0 or > 4194304)
            throw new JsonException("Invalid arrival draft identity size.");
        try
        {
            ContentContractGuards.RequireSha256(expectedHash, nameof(expectedHash));
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 64 });
            var root = document.RootElement;
            if (root.GetProperty("format").GetString() != "sandtable.campaign-arrival-draft.v1")
                throw new JsonException("Unsupported arrival draft identity format.");
            var contentBytes = Encoding.UTF8.GetBytes(root.GetProperty("candidateContent").GetRawText());
            var pack = ContentPackSerializer.Deserialize(contentBytes).Definition
                ?? throw new JsonException("Invalid candidate content.");
            var groups = new List<ScheduledArrivalGroup>();
            var destinations = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (var value in root.GetProperty("groups").EnumerateArray())
            {
                var groupId = RequiredString(value, "id");
                var side = RequiredString(value, "owner") switch
                {
                    "axis" => LandSide.Axis,
                    "commonwealth" => LandSide.Commonwealth,
                    _ => throw new JsonException("Invalid arrival owner."),
                };
                var elements = value.GetProperty("elements").EnumerateArray()
                    .Select(item => item.GetString() ?? throw new JsonException("Invalid element ID.")).ToArray();
                var sources = value.GetProperty("sources").EnumerateArray()
                    .Select(item => new RuleReference(RequiredString(item, "sourceId"), RequiredString(item, "locator"))).ToArray();
                groups.Add(new ScheduledArrivalGroup(groupId, side, value.GetProperty("turn").GetInt32(),
                    value.GetProperty("stage").GetInt32(), elements, sources));
                destinations.Add(groupId, value.GetProperty("destinations").EnumerateArray()
                    .Select(item => item.GetString() ?? throw new JsonException("Invalid destination ID.")).ToArray());
            }
            var identity = CampaignArrivalDraftIdentity.Create(pack, RequiredString(root, "scenarioId"), groups,
                destinations, RequiredString(root, "rulesetHash"), RequiredString(root, "rulingHash"));
            if (!bytes.SequenceEqual(identity.CopyCanonicalBytes()) || identity.Hash != expectedHash)
                throw new JsonException("Arrival draft identity is not canonical.");
            return identity;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or OverflowException)
        {
            throw new JsonException("Invalid arrival draft identity.", exception);
        }
    }

    private static string RequiredString(JsonElement value, string property) =>
        value.GetProperty(property).GetString() ?? throw new JsonException("Missing string value.");
}
