using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Content;

/// <summary>
/// Versioned source content that adds unassigned, setup-pending counters to a
/// retained scheduled source pack. It does not assert movement or attachment.
/// </summary>
internal sealed class ContentSourceSetupPackArtifact
{
    internal const string FormatId = "sandtable.source-setup-combat.v1";
    private readonly byte[] bytes;
    private readonly ReadOnlyCollection<ContentSourceSetupUnit> units;

    private ContentSourceSetupPackArtifact(ContentSourceScheduledCombatPackArtifact basis,
        IEnumerable<ContentSourceSetupUnit> units, byte[] bytes)
    {
        Basis = basis;
        this.units = Array.AsReadOnly(units.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());
        this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    internal ContentSourceScheduledCombatPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal IReadOnlyList<ContentSourceSetupUnit> Units => units;
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;

    internal static ContentSourceSetupPackArtifact Create(ContentSourceScheduledCombatPackArtifact basis,
        IEnumerable<ContentSourceSetupUnit> sourceUnits)
    {
        ArgumentNullException.ThrowIfNull(basis);
        ArgumentNullException.ThrowIfNull(sourceUnits);
        var input = sourceUnits.ToArray();
        if (input.Length == 0 || input.Any(value => value is null))
            throw new ArgumentException("Source setup elements are required.", nameof(sourceUnits));
        var units = input.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        if (units.Length == 0
            || units.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count() != units.Length
            || units.Select(value => value.ComponentId).Distinct(StringComparer.Ordinal).Count() != units.Length
            || units.Select(value => value.ElementId).Intersect(
                basis.Definition.LegacyDefinition.Elements.Select(value => value.ElementId), StringComparer.Ordinal).Any()
            || units.Select(value => value.ComponentId).Intersect(
                basis.Definition.ElementCombatFacts.SelectMany(value => value.Components).Select(value => value.ComponentId),
                StringComparer.Ordinal).Any())
            throw new ArgumentException("Source setup elements must be distinct and absent from retained content.", nameof(sourceUnits));
        var sourceIds = basis.Definition.LegacyDefinition.SourceIndex.Select(value => value.SourceId).ToHashSet(StringComparer.Ordinal);
        foreach (var unit in units)
        {
            ContentContractGuards.RequireStableId(unit.ElementId, nameof(sourceUnits));
            ContentContractGuards.RequireStableId(unit.SideId, nameof(sourceUnits));
            ContentContractGuards.RequireStableId(unit.ComponentId, nameof(sourceUnits));
            ContentContractGuards.RequireStableId(unit.ComponentClassId, nameof(sourceUnits));
            if (unit.SideId is not ("axis" or "commonwealth") || unit.CapabilityPoints <= 0 || unit.Toe <= 0 || unit.Stacking < 0
                || unit.Origin is null || unit.Origin.Kind != ContentOriginKind.SourceDerived
                || unit.Origin.References.Any(reference => !sourceIds.Contains(reference.SourceId)))
                throw new ArgumentException("Invalid setup source facts or source references.", nameof(sourceUnits));
        }
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("formatId", FormatId);
            writer.WriteString("baseFormatId", basis.Identity.FormatId);
            writer.WriteString("basePackId", basis.Identity.PackId);
            writer.WriteString("rulesetId", basis.Identity.RulesetId);
            writer.WriteString("baseContentHash", basis.Identity.Hash);
            writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteStartArray("setupPendingUnits");
            foreach (var unit in units)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", unit.ElementId);
                writer.WriteString("sideId", unit.SideId);
                writer.WriteNull("parentFormationId");
                writer.WriteNull("mobilityId");
                writer.WriteNumber("capabilityPoints", unit.CapabilityPoints);
                writer.WriteString("componentId", unit.ComponentId);
                writer.WriteString("componentClassId", unit.ComponentClassId);
                writer.WriteNumber("initialToe", unit.Toe);
                writer.WriteNumber("stacking", unit.Stacking);
                writer.WriteStartArray("sources");
                foreach (var reference in unit.Origin.References)
                {
                    writer.WriteStartObject();
                    writer.WriteString("sourceId", reference.SourceId);
                    writer.WriteString("locator", reference.Locator);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new(basis, units, stream.ToArray());
    }

    internal static ContentSourceSetupPackArtifact Read(byte[] canonicalBytes)
    {
        ArgumentNullException.ThrowIfNull(canonicalBytes);
        if (canonicalBytes.Length is < 1 or > 4_194_304) throw new JsonException("Invalid source setup content size.");
        using var document = JsonDocument.Parse(canonicalBytes);
        var root = document.RootElement;
        Require(root, "contractVersion", "formatId", "baseFormatId", "basePackId", "rulesetId", "baseContentHash", "baseContent", "setupPendingUnits");
        if (root.GetProperty("contractVersion").GetInt32() != 1 || root.GetProperty("formatId").GetString() != FormatId)
            throw new JsonException("Unsupported setup source format.");
        var identity = new ContentScheduledPackIdentity(root.GetProperty("baseFormatId").GetString()!,
            root.GetProperty("basePackId").GetString()!, root.GetProperty("rulesetId").GetString()!,
            root.GetProperty("baseContentHash").GetString()!);
        var basis = ContentSourceScheduledCombatPackArtifact.Read(root.GetProperty("baseContent").GetBytesFromBase64(), identity);
        var units = root.GetProperty("setupPendingUnits").EnumerateArray().Select(value =>
        {
            Require(value, "elementId", "sideId", "parentFormationId", "mobilityId", "capabilityPoints", "componentId", "componentClassId", "initialToe", "stacking", "sources");
            if (value.GetProperty("parentFormationId").ValueKind != JsonValueKind.Null
                || value.GetProperty("mobilityId").ValueKind != JsonValueKind.Null)
                throw new JsonException("Unassigned source units cannot fabricate parent or mobility.");
            var refs = value.GetProperty("sources").EnumerateArray().Select(reference =>
            {
                Require(reference, "sourceId", "locator");
                return new RuleReference(reference.GetProperty("sourceId").GetString()!, reference.GetProperty("locator").GetString()!);
            });
            return new ContentSourceSetupUnit(value.GetProperty("elementId").GetString()!, value.GetProperty("sideId").GetString()!,
                value.GetProperty("capabilityPoints").GetInt32(), value.GetProperty("componentId").GetString()!,
                value.GetProperty("componentClassId").GetString()!, value.GetProperty("initialToe").GetInt32(),
                value.GetProperty("stacking").GetInt32(), new ContentOrigin(ContentOriginKind.SourceDerived, refs));
        }).ToArray();
        var result = Create(basis, units);
        if (!canonicalBytes.AsSpan().SequenceEqual(result.CanonicalBytes))
            throw new JsonException("Source setup content is not canonical.");
        return result;
    }

    private static void Require(JsonElement value, params string[] names)
    {
        if (value.ValueKind != JsonValueKind.Object || !value.EnumerateObject().Select(property => property.Name).SequenceEqual(names))
            throw new JsonException("Unexpected source setup content properties.");
    }
}

internal sealed record ContentSourceSetupUnit(string ElementId, string SideId, int CapabilityPoints,
    string ComponentId, string ComponentClassId, int Toe, int Stacking, ContentOrigin Origin);
