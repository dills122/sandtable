using System.Text;
using System.Text.Json;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Artifacts;

public sealed class ExerciseSeedLedgerEntry
{
    private readonly byte[] canonicalMaterial;

    internal ExerciseSeedLedgerEntry(
        ExerciseSeedDomain domain,
        ExerciseSeedRole? role,
        ExerciseSeedDerivation derivation)
    {
        if (!Enum.IsDefined(domain)) throw new ArgumentOutOfRangeException(nameof(domain));
        if (role.HasValue && !Enum.IsDefined(role.Value))
            throw new ArgumentOutOfRangeException(nameof(role));
        ArgumentNullException.ThrowIfNull(derivation);
        Domain = domain;
        Role = role;
        canonicalMaterial = derivation.CanonicalMaterial;
        Digest = derivation.Digest;
        DerivedSeed = derivation.DerivedSeed;
    }

    public ExerciseSeedDomain Domain { get; }
    public ExerciseSeedRole? Role { get; }
    public byte[] CanonicalMaterial => canonicalMaterial.ToArray();
    public string Digest { get; }
    public ulong DerivedSeed { get; }
}

public sealed class ExerciseSeedLedger
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = ExerciseSeedDeriver.SchemeId;

    private ExerciseSeedLedger(
        ExerciseRunIdentity identity,
        IEnumerable<ExerciseSeedLedgerEntry> entries)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        ContractVersion = CurrentContractVersion;
        ContractSchemeId = SchemeId;
        Entries = Array.AsReadOnly(entries.ToArray());
    }

    public int ContractVersion { get; }
    public string ContractSchemeId { get; }
    public ExerciseRunIdentity Identity { get; }
    public IReadOnlyList<ExerciseSeedLedgerEntry> Entries { get; }

    public static ExerciseSeedLedger Create(ExerciseRunIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        (ExerciseSeedDomain Domain, ExerciseSeedRole? Role)[] order =
        [
            (ExerciseSeedDomain.Umpire, null),
            (ExerciseSeedDomain.Controller, ExerciseSeedRole.System),
            (ExerciseSeedDomain.Controller, ExerciseSeedRole.Axis),
            (ExerciseSeedDomain.Controller, ExerciseSeedRole.Commonwealth),
            (ExerciseSeedDomain.ArtifactSampling, null),
            (ExerciseSeedDomain.DiagnosticSampling, null),
        ];
        return new ExerciseSeedLedger(
            identity,
            order.Select(value => new ExerciseSeedLedgerEntry(
                value.Domain,
                value.Role,
                ExerciseSeedDeriver.Derive(identity, value.Domain, value.Role))));
    }
}

public static class SeedLedgerCodec
{
    private static readonly string[] EntryProperties =
    [
        "domain", "role", "canonicalMaterial", "digest", "derivedSeed",
    ];

    private static readonly string[] MaterialProperties =
    [
        "contractVersion", "schemeId", "rootSeed", "maneuverId", "exerciseOrdinal",
        "pairKey", "domain", "role",
    ];

    public static byte[] Serialize(ExerciseSeedLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ledger.ContractVersion);
            writer.WriteString("schemeId", ledger.ContractSchemeId);
            writer.WriteStartArray("entries");
            foreach (var entry in ledger.Entries)
            {
                writer.WriteStartObject();
                writer.WriteString("domain", ExerciseSeedDeriver.FormatDomain(entry.Domain));
                if (entry.Role is null) writer.WriteNull("role");
                else writer.WriteString("role", ExerciseSeedDeriver.FormatRole(entry.Role.Value));
                writer.WritePropertyName("canonicalMaterial");
                writer.WriteRawValue(entry.CanonicalMaterial, skipInputValidation: false);
                writer.WriteString("digest", entry.Digest);
                writer.WriteNumber("derivedSeed", entry.DerivedSeed);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static ExerciseSeedLedger Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            StrictJson.RequireExactProperties(root, ["contractVersion", "schemeId", "entries"]);
            if (root.GetProperty("contractVersion").GetInt32()
                != ExerciseSeedLedger.CurrentContractVersion)
                throw new JsonException("Unknown seed-ledger contract version.");
            if (!string.Equals(
                    root.GetProperty("schemeId").GetString(),
                    ExerciseSeedLedger.SchemeId,
                    StringComparison.Ordinal))
                throw new JsonException("Unknown seed-ledger scheme.");

            var entriesElement = root.GetProperty("entries");
            if (entriesElement.ValueKind != JsonValueKind.Array)
                throw new JsonException("Seed-ledger entries must be an array.");
            var parsed = entriesElement.EnumerateArray().Select(ParseEntry).ToArray();
            if (parsed.Length != 6)
                throw new JsonException("A version-1 seed ledger requires exactly six entries.");
            var identity = parsed[0].Identity;
            if (parsed.Any(value => value.Identity != identity))
                throw new JsonException("Every seed-ledger entry must use the same run identity.");

            var expected = ExerciseSeedLedger.Create(identity);
            for (var index = 0; index < parsed.Length; index++)
            {
                var actual = parsed[index];
                var expectedEntry = expected.Entries[index];
                if (actual.Domain != expectedEntry.Domain
                    || actual.Role != expectedEntry.Role
                    || !actual.CanonicalMaterial.AsSpan()
                        .SequenceEqual(expectedEntry.CanonicalMaterial)
                    || !string.Equals(actual.Digest, expectedEntry.Digest, StringComparison.Ordinal)
                    || actual.DerivedSeed != expectedEntry.DerivedSeed)
                    throw new JsonException("A seed-ledger entry is noncanonical or corrupt.");
            }
            if (!Serialize(expected).AsSpan().SequenceEqual(canonicalJson.Span))
                throw new JsonException("The seed ledger is not canonically encoded.");
            return expected;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or OverflowException
            or FormatException)
        {
            throw new JsonException("The seed ledger is invalid.", exception);
        }
    }

    private static ParsedEntry ParseEntry(JsonElement entry)
    {
        StrictJson.RequireExactProperties(entry, EntryProperties);
        var domain = ParseDomain(entry.GetProperty("domain").GetString());
        var roleElement = entry.GetProperty("role");
        var role = roleElement.ValueKind == JsonValueKind.Null
            ? (ExerciseSeedRole?)null
            : ParseRole(roleElement.GetString());
        var material = entry.GetProperty("canonicalMaterial");
        StrictJson.RequireExactProperties(material, MaterialProperties);
        if (material.GetProperty("contractVersion").GetInt32()
            != ExerciseSeedDeriver.CurrentContractVersion)
            throw new JsonException("Unknown seed material contract version.");
        if (!string.Equals(
                material.GetProperty("schemeId").GetString(),
                ExerciseSeedDeriver.SchemeId,
                StringComparison.Ordinal))
            throw new JsonException("Unknown seed material scheme.");
        var materialDomain = ParseDomain(material.GetProperty("domain").GetString());
        var materialRoleElement = material.GetProperty("role");
        var materialRole = materialRoleElement.ValueKind == JsonValueKind.Null
            ? (ExerciseSeedRole?)null
            : ParseRole(materialRoleElement.GetString());
        if (domain != materialDomain || role != materialRole)
            throw new JsonException("Ledger labels do not match canonical seed material.");

        var pairKeyElement = material.GetProperty("pairKey");
        var pairKey = pairKeyElement.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => pairKeyElement.GetString(),
            _ => throw new JsonException("pairKey must be a string or null."),
        };
        var identity = new ExerciseRunIdentity(
            material.GetProperty("rootSeed").GetUInt64(),
            material.GetProperty("maneuverId").GetString()
                ?? throw new JsonException("maneuverId must be a string."),
            material.GetProperty("exerciseOrdinal").GetInt32(),
            pairKey);
        return new ParsedEntry(
            identity,
            domain,
            role,
            Encoding.UTF8.GetBytes(material.GetRawText()),
            entry.GetProperty("digest").GetString()
                ?? throw new JsonException("digest must be a string."),
            entry.GetProperty("derivedSeed").GetUInt64());
    }

    private static ExerciseSeedDomain ParseDomain(string? value) => value switch
    {
        "umpire" => ExerciseSeedDomain.Umpire,
        "controller" => ExerciseSeedDomain.Controller,
        "artifact-sampling" => ExerciseSeedDomain.ArtifactSampling,
        "diagnostic-sampling" => ExerciseSeedDomain.DiagnosticSampling,
        _ => throw new JsonException("Unknown seed domain."),
    };

    private static ExerciseSeedRole ParseRole(string? value) => value switch
    {
        "system" => ExerciseSeedRole.System,
        "axis" => ExerciseSeedRole.Axis,
        "commonwealth" => ExerciseSeedRole.Commonwealth,
        _ => throw new JsonException("Unknown seed role."),
    };

    private sealed record ParsedEntry(
        ExerciseRunIdentity Identity,
        ExerciseSeedDomain Domain,
        ExerciseSeedRole? Role,
        byte[] CanonicalMaterial,
        string Digest,
        ulong DerivedSeed);
}
