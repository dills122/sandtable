using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

// Independent (ungrouped) Bardia static-defense counters left out of
// ContentSourceTobrukBardiaGarrisonPackArtifact's 28-element "part 1" pack by
// design (see that file's own comment and CampaignSourceBattlefield.
// AdmitSourceTobrukBardiaGarrison's comment: "the other 5 ... stay out of
// this pack's scope"). Every element and TOE here already carries a citation
// in Cna1979GrazianiSourceFacts.cs (BardiaInfantryCombatFacts/
// BardiaSupportCombatFacts and their TOE seed functions, reused verbatim,
// filtered to this pack's 5 elements) or in the retained
// BardiaGarrisonChartCandidate group-level citation; none of it is newly
// asserted here.
internal sealed class ContentSourceBardiaGarrisonRemainderPackArtifact
{
    internal const string FormatId = "sandtable.source-bardia-garrison-remainder.v1";
    internal const string ContentHash = "sha256:eb9eac054deb78ec2da5e0b81bf964a2d21acb844665a379d8a3bc7e4c9ffd57"; // RUL-GRA-26: derived from CampaignSourceAxisSupplementalForces.ContentHash (via ContentSourceTobrukBardiaGarrisonPackArtifact).
    private readonly byte[] bytes;

    private ContentSourceBardiaGarrisonRemainderPackArtifact(ContentSourceTobrukBardiaGarrisonPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    internal ContentSourceTobrukBardiaGarrisonPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();

    internal const string LocationId = ContentSourceTobrukBardiaGarrisonPackArtifact.BardiaLocationId;

    // The machine-gun battalion attached to Bardia's infantry subset, and the
    // antitank/artillery support left out of the "part 1" pack. Citation:
    // BardiaGarrisonChartCandidate (scenario 60.31; oa-bardia-p13).
    internal static IReadOnlyList<string> AllIds { get; } = Array.AsReadOnly(new[]
    {
        "vec-machine-gun-battalion", "30-gaf-antitank-battalion",
        "8-gaf-artillery", "9-gaf-artillery", "10-gaf-artillery",
    });

    // Unions the already-published Bardia TOE seed functions, filtered to
    // this pack's 5 elements. No new TOE value is introduced anywhere here.
    internal static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateToeSeeds(string scenarioId)
    {
        var seeds = Cna1979GrazianiSourceFacts.CreateBardiaInfantryToeSeeds(scenarioId)
            .Concat(Cna1979GrazianiSourceFacts.CreateBardiaSupportToeSeeds(scenarioId))
            .Where(value => AllIds.Contains(value.ElementId, StringComparer.Ordinal))
            .ToArray();
        if (seeds.Length != AllIds.Count
            || seeds.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count() != AllIds.Count
            || seeds.Any(value => !AllIds.Contains(value.ElementId, StringComparer.Ordinal)))
            throw new InvalidOperationException("Bardia garrison remainder TOE seeds differ from its retained element list.");
        return Array.AsReadOnly(seeds);
    }

    internal static ContentSourceBardiaGarrisonRemainderPackArtifact Create(ContentSourceTobrukBardiaGarrisonPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Identity.Hash != ContentSourceTobrukBardiaGarrisonPackArtifact.ContentHash)
            throw new InvalidOperationException("Bardia garrison remainder requires the admitted Tobruk/Bardia pack.");
        var seeds = CreateToeSeeds(Cna1979GrazianiCairoSourceExcerpt.ScenarioId)
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Identity.Hash);
            writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("sources", "scenario:60.31;axis-charts:oa-bardia-p13;land:4.45");
            writer.WriteStartArray("units");
            foreach (var id in AllIds.Order(StringComparer.Ordinal))
            {
                var toe = seeds[id];
                writer.WriteStartObject();
                writer.WriteString("elementId", id);
                writer.WriteString("sideId", "axis");
                writer.WriteString("locationId", LocationId);
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
        var result = new ContentSourceBardiaGarrisonRemainderPackArtifact(basis, stream.ToArray());
        if (result.Identity.Hash != ContentHash)
            throw new InvalidOperationException("Bardia garrison remainder content differs from its pinned identity.");
        return result;
    }

    internal static ContentSourceBardiaGarrisonRemainderPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceTobrukBardiaGarrisonPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("Bardia garrison remainder content is not canonical.");
        return expected;
    }
}
