using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

// Independent (ungrouped) Commonwealth artillery/antitank counters from
// Cna1979GrazianiSourceFacts.CommonwealthInitialPlacementCandidates that
// ContentSourceCwFrontlineArtifact (1-rtr, 11-hussars only) never picked up.
// The six infantry battalions sharing those same source placements (2-scots-
// guards, 1-french-motor-marines, 1-royal-northumberland-fusiliers,
// 2-rifle-brigade, 3-coldstream-guards, 1-kings-royal-rifle-corps) are
// already admitted through ContentSourceIndependentPackArtifact/
// ContentSourceExpandedIndependentPackArtifact; these 4 attached support
// elements are not admitted anywhere. Every element and TOE here already
// carries a citation in Cna1979GrazianiSourceFacts.cs
// (CommonwealthInitialCombatFacts / CreateCommonwealthInitialToeSeeds,
// filtered to this pack's 4 elements, reused verbatim); none of it is newly
// asserted here.
internal sealed class ContentSourceCwAttachedArtilleryPackArtifact
{
    internal const string FormatId = "sandtable.source-cw-attached-artillery.v1";
    internal const string ContentHash = "sha256:01fbe0c7648a72ed7aa33a55cedb7f5dbef9f301595335ef3efafd4cc4f868ff";
    private readonly byte[] bytes;

    private ContentSourceCwAttachedArtilleryPackArtifact(ContentSourceCwFrontlineArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    internal ContentSourceCwFrontlineArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();

    internal static IReadOnlyList<(string Id, string Location)> Units { get; } = Array.AsReadOnly(new[]
    {
        ("3-royal-horse-artillery-antitank", "c3721"),
        ("4-royal-horse-artillery", "c3922"),
        ("7-medium-artillery", "c3922"),
        ("31-field-artillery", "c4131"),
    });

    internal static IReadOnlyList<string> AllIds { get; } =
        Array.AsReadOnly(Units.Select(value => value.Id).ToArray());

    internal static string LocationOf(string elementId) =>
        Units.Single(value => value.Id == elementId).Location;

    internal static ContentElementCombatFacts Facts(string id) =>
        Cna1979GrazianiSourceFacts.CommonwealthInitialCombatFacts.Single(value => value.ElementId == id);

    // Filters the already-published Commonwealth initial TOE candidates down
    // to this pack's 4 elements. No new TOE value is introduced anywhere here.
    internal static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateToeSeeds(string scenarioId)
    {
        var seeds = Cna1979GrazianiSourceFacts.CreateCommonwealthInitialToeSeeds(scenarioId)
            .Where(value => AllIds.Contains(value.ElementId, StringComparer.Ordinal))
            .ToArray();
        if (seeds.Length != AllIds.Count
            || seeds.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count() != AllIds.Count
            || seeds.Any(value => !AllIds.Contains(value.ElementId, StringComparer.Ordinal)))
            throw new InvalidOperationException("CW attached artillery TOE seeds differ from its retained element list.");
        return Array.AsReadOnly(seeds);
    }

    internal static ContentSourceCwAttachedArtilleryPackArtifact Create(ContentSourceCwFrontlineArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Identity.Hash != ContentSourceCwFrontlineArtifact.ContentHash)
            throw new InvalidOperationException("CW attached artillery requires the admitted CW frontline pack.");
        var seeds = CreateToeSeeds(Cna1979GrazianiCairoSourceExcerpt.ScenarioId)
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Identity.Hash);
            writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("sources", "scenario:60.41;cw-charts:oa-7-armored-p6,oa-indian-p15,oa-british-artillery-p12;land:4.45");
            writer.WriteStartArray("units");
            foreach (var unit in Units.OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                var toe = seeds[unit.Id];
                writer.WriteStartObject();
                writer.WriteString("elementId", unit.Id);
                writer.WriteString("sideId", "commonwealth");
                writer.WriteString("locationId", unit.Location);
                writer.WriteStartArray("components");
                foreach (var component in toe.InitialComponentToes)
                {
                    writer.WriteStartObject();
                    writer.WriteString("componentId", component.ComponentId);
                    writer.WriteNumber("toe", component.CurrentToe);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        var result = new ContentSourceCwAttachedArtilleryPackArtifact(basis, stream.ToArray());
        if (result.Identity.Hash != ContentHash)
            throw new InvalidOperationException("CW attached artillery content differs from its pinned identity.");
        return result;
    }

    internal static ContentSourceCwAttachedArtilleryPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceCwFrontlineArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("CW attached artillery content is not canonical.");
        return expected;
    }
}
