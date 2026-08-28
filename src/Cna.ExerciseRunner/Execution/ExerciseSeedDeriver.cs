using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Execution;

public enum ExerciseSeedDomain
{
    Umpire,
    Controller,
    ArtifactSampling,
    DiagnosticSampling,
}

public enum ExerciseSeedRole
{
    System,
    Axis,
    Commonwealth,
}

public sealed record ExerciseRunIdentity
{
    public ExerciseRunIdentity(
        ulong rootSeed,
        string maneuverId,
        int exerciseOrdinal,
        string? pairKey)
    {
        StableIdValidation.Require(maneuverId, nameof(maneuverId));
        ArgumentOutOfRangeException.ThrowIfNegative(exerciseOrdinal);
        if (pairKey is not null)
        {
            StableIdValidation.Require(pairKey, nameof(pairKey));
        }

        RootSeed = rootSeed;
        ManeuverId = maneuverId;
        ExerciseOrdinal = exerciseOrdinal;
        PairKey = pairKey;
    }

    public ulong RootSeed { get; }
    public string ManeuverId { get; }
    public int ExerciseOrdinal { get; }
    public string? PairKey { get; }

    public static ExerciseRunIdentity Standalone(string exerciseId, ulong rootSeed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exerciseId);
        return new ExerciseRunIdentity(rootSeed, $"standalone.{exerciseId}", 0, null);
    }
}

public sealed class ExerciseSeedDerivation
{
    private readonly byte[] canonicalMaterial;

    internal ExerciseSeedDerivation(byte[] canonicalMaterial, string digest, ulong derivedSeed)
    {
        this.canonicalMaterial = canonicalMaterial.ToArray();
        Digest = digest;
        DerivedSeed = derivedSeed;
    }

    public byte[] CanonicalMaterial => canonicalMaterial.ToArray();
    public string Digest { get; }
    public ulong DerivedSeed { get; }
}

public static class ExerciseSeedDeriver
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = "sandtable.exercise-seeds.v1";

    public static ExerciseSeedDerivation Derive(
        ExerciseRunIdentity identity,
        ExerciseSeedDomain domain,
        ExerciseSeedRole? role)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (!Enum.IsDefined(domain)) throw new ArgumentOutOfRangeException(nameof(domain));
        if (role.HasValue && !Enum.IsDefined(role.Value))
            throw new ArgumentOutOfRangeException(nameof(role));
        if ((domain == ExerciseSeedDomain.Controller) != role.HasValue)
            throw new ArgumentException("Only controller seeds require a role.", nameof(role));

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("schemeId", SchemeId);
            writer.WriteNumber("rootSeed", identity.RootSeed);
            writer.WriteString("maneuverId", identity.ManeuverId);
            writer.WriteNumber("exerciseOrdinal", identity.ExerciseOrdinal);
            if (identity.PairKey is null) writer.WriteNull("pairKey");
            else writer.WriteString("pairKey", identity.PairKey);
            writer.WriteString("domain", FormatDomain(domain));
            if (role is null) writer.WriteNull("role");
            else writer.WriteString("role", FormatRole(role.Value));
            writer.WriteEndObject();
        }

        var material = stream.ToArray();
        var digest = SHA256.HashData(material);
        return new ExerciseSeedDerivation(
            material,
            $"sha256:{Convert.ToHexStringLower(digest)}",
            BinaryPrimitives.ReadUInt64BigEndian(digest));
    }

    internal static string FormatDomain(ExerciseSeedDomain domain) => domain switch
    {
        ExerciseSeedDomain.Umpire => "umpire",
        ExerciseSeedDomain.Controller => "controller",
        ExerciseSeedDomain.ArtifactSampling => "artifact-sampling",
        ExerciseSeedDomain.DiagnosticSampling => "diagnostic-sampling",
        _ => throw new ArgumentOutOfRangeException(nameof(domain)),
    };

    internal static string FormatRole(ExerciseSeedRole role) => role switch
    {
        ExerciseSeedRole.System => "system",
        ExerciseSeedRole.Axis => "axis",
        ExerciseSeedRole.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}
