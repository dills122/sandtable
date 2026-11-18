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
    // All seven non-empty entries derived from (and so changed when)
    // CampaignSourceAxisSupplementalForces.ContentHash changed - see
    // RUL-GRA-26's note on that constant for the most recent reason.
    private static readonly string[] PinnedHashes = ["",
        "sha256:b5db187fc519f2e40b3030d45bdda788ea0ede1c48f0b7907572ca1e6a880e3c",
        "sha256:1d1a76b61c9c7d0615a5a610b30d423ed3ee4841e512dd1746b0b1d1d9a6e066",
        "sha256:d89e7d3f1f003c13149fe6cbe6b08202a881a4b5a4f44531ace8309b64330831",
        "sha256:54ff3e00df5323dee80d1ae6645c1bf56f67aa70bc58a6bfdb341742b4ae70bf",
        "sha256:376f129b54e27dcb7d9095f0b1cb48a613e9f12e6fa06dee164edd446e6703db",
        "sha256:ae263cc210aa20a13b85c177131ea6691a533ad2c57a39cb089a0ec951b5008b",
        "sha256:9045a938cc0b5d46d767bce25869b907fcae22444cf134b787505534c8d8129e"];
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
