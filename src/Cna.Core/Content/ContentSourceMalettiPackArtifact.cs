using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Content;

/// <summary>Source successor retaining the Cairo artifact and the limited C3617 setup facts.
/// C3617 has no certified terrain or topology and is not a playable map.</summary>
internal sealed class ContentSourceMalettiPackArtifact
{
    internal const string FormatId = "sandtable.source-maletti-setup.v1";
    private static readonly string[] PinnedHashes = ["",
        "sha256:9d29241a4cdd378aac176d4a3257936e7503ead7b320292d9220e6d9821b0ece",
        "sha256:66fe0b1e3aa7ce55f7b381f5bb93c19e8a91070d3bd3664a38bb174b587d38c2",
        "sha256:56613b16926495ae6317764b4b7e5500bf2a4616701a3de81049f5525ad0f1cb",
        "sha256:1577244ea41215eeac51ec089bb4b9605c7b80ae894a967dee9b2db625f769a7",
        "sha256:636027a49fed0be24db46343415022fb261ded5a91348c9166be5f846371e678",
        "sha256:fe67491957ab3a3e51fb84962c263dca61e1b34f6b7c8ddf9254c73fed5c8645",
        "sha256:cc7a776d4b81a05e431adbf24535b8eb3b54c6d9cfa92e682d86f7a41151464e"];
    private readonly byte[] bytes;
    private ContentSourceMalettiPackArtifact(ContentSourceSetupPackArtifact basis, string[] units, byte[] bytes)
    {
        Basis = basis;
        Units = Array.AsReadOnly(units);
        this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    internal ContentSourceSetupPackArtifact Basis { get; }
    internal IReadOnlyList<string> Units { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;

    internal static ContentOrigin Origin => new(ContentOriginKind.SourceDerived,
        [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.31"),
         new RuleReference(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "oa-maletti-p7"),
         new RuleReference(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "4.46b")]);

    internal static ContentSourceMalettiPackArtifact Create(ContentSourceSetupPackArtifact basis, IEnumerable<string> activatedIds)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != CampaignSourceCairoActiveWorld.SourceContentHash)
            throw new InvalidOperationException("Maletti successor requires pinned Cairo source content.");
        ArgumentNullException.ThrowIfNull(activatedIds);
        var units = activatedIds.Order(StringComparer.Ordinal).ToArray();
        if (units.Length == 0 || units.Distinct(StringComparer.Ordinal).Count() != units.Length
            || units.Any(id => !CampaignSourceAxisPriorityMotorization.Targets.Contains(id, StringComparer.Ordinal)))
            throw new InvalidOperationException("Invalid Maletti source unit selection.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash());
            writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("sourceSupplementHash", CampaignSourceAxisSupplementalForces.ContentHash);
            writer.WriteString("locationId", "c3617");
            writer.WriteNull("terrainId");
            writer.WriteNull("topology");
            writer.WriteString("historicalParentId", CampaignSourceAxisPriorityMotorization.HistoricalParentId);
            writer.WriteString("unitActivation", "only-after-explicit-detachment");
            writer.WriteStartArray("units");
            foreach (var id in units)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", id);
                writer.WriteString("sideId", "axis");
                writer.WriteNumber("capabilityPoints", 10);
                writer.WriteString("printedCapabilityPoints", "10+");
                writer.WriteString("componentId", id + ".infantry");
                writer.WriteString("componentClassId", Cna1979Combat.InfantryComponentClassId);
                writer.WriteNumber("toe", 5);
                writer.WriteNumber("stacking", 1);
                writer.WriteString("stackingSource", "spi-1979-air-logistics:counter-section-2-p40");
                writer.WriteNull("currentParentId");
                writer.WriteNull("mobilityId");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        var result = new ContentSourceMalettiPackArtifact(basis, units, stream.ToArray());
        var mask = CampaignSourceAxisPriorityMotorization.Targets.Select((id, index) =>
            units.Contains(id, StringComparer.Ordinal) ? 1 << index : 0).Sum();
        if (result.Hash() != PinnedHashes[mask])
            throw new InvalidOperationException("Maletti source content differs from the pinned source profile.");
        return result;
    }

    internal static ContentSourceMalettiPackArtifact Read(byte[] canonicalBytes)
    {
        ArgumentNullException.ThrowIfNull(canonicalBytes);
        using var document = JsonDocument.Parse(canonicalBytes);
        var root = document.RootElement;
        var basis = ContentSourceSetupPackArtifact.Read(root.GetProperty("baseContent").GetBytesFromBase64());
        var units = root.GetProperty("units").EnumerateArray().Select(unit => unit.GetProperty("elementId").GetString()
            ?? throw new JsonException("Missing source unit ID."));
        var expected = Create(basis, units);
        if (!canonicalBytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("Maletti content is not the canonical source profile.");
        return expected;
    }
}
