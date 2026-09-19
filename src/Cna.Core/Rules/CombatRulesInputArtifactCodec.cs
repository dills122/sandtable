using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Rules;

/// <summary>Strict dormant RulesInput1 codec; never registers a runtime artifact.</summary>
internal static class CombatRulesInputArtifactCodec
{
    private const int MaximumBytes = 1_048_576;
    private const int MaximumArrayItems = 512;
    private static readonly SearchValues<char> LowerHexDigits = SearchValues.Create("0123456789abcdef");
    private static readonly byte[] Authority = CreateAuthority();

    public static byte[] SerializeCanonical(CombatRulesInputDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var bytes = WriteDefinition(definition);
        RequireAuthority(bytes);
        return bytes;
    }

    public static CombatRulesInputDefinition Deserialize(ReadOnlySpan<byte> utf8Json, string expectedContentHash)
    {
        if (!string.Equals(expectedContentHash, Cna1979CombatAdjudication.ContentHash, StringComparison.Ordinal))
        {
            throw new JsonException("The expected Combat RulesInput content hash does not match authority.");
        }
        // This closed artifact admits exactly one canonical authority. Comparing before parsing
        // also rejects duplicate/unknown fields, alternate numbers/escapes and excessive depth
        // or arrays without allocating a caller-controlled JSON tree. No repairs or self-hashes.
        RequireAuthority(utf8Json);
        return Cna1979CombatAdjudication.Definition;
    }

    private static byte[] CreateAuthority()
    {
        var bytes = WriteDefinition(Cna1979CombatAdjudication.Definition);
        var hash = $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}";
        if (!string.Equals(hash, Cna1979CombatAdjudication.ContentHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The selected Combat definition differs from its frozen source hash.");
        }
        return bytes;
    }

    private static void RequireAuthority(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > MaximumBytes || !bytes.SequenceEqual(Authority))
        {
            throw new JsonException("The Combat RulesInput artifact is not canonical authority.");
        }
    }

    private static byte[] WriteDefinition(CombatRulesInputDefinition definition)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { MaxDepth = 32 }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", definition.SchemaVersion);
            WriteId(writer, "artifactId", definition.ArtifactId);
            WriteId(writer, "profileId", definition.ProfileId);
            writer.WriteStartArray("sources");
            foreach (var source in Sorted(definition.Sources, static (a, b) =>
                Compare(a.SourceId, b.SourceId, a.Locator, b.Locator)))
            {
                writer.WriteStartObject();
                WriteId(writer, "sourceId", source.SourceId);
                WriteText(writer, "locator", source.Locator);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("sourceEvidence");
            foreach (var evidence in Sorted(definition.SourceEvidence, static (a, b) =>
                string.Compare(a.SourceId, b.SourceId, StringComparison.Ordinal)))
            {
                writer.WriteStartObject();
                WriteId(writer, "sourceId", evidence.SourceId);
                WriteHash(writer, "sha256", evidence.Sha256);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartObject("amendment");
            WriteId(writer, "rulingId", definition.Amendment.RulingId);
            WriteId(writer, "role", RoleName(definition.Amendment.Role));
            writer.WriteNumber("differential", definition.Amendment.Differential);
            WriteSet(writer, "coordinates", definition.Amendment.Coordinates);
            writer.WriteNumber("lossPercent", definition.Amendment.LossPercent);
            writer.WriteEndObject();
            writer.WriteStartArray("morale");
            foreach (var cell in Sorted(definition.Morale, static (a, b) => a.Coordinate.CompareTo(b.Coordinate)))
            {
                writer.WriteStartObject();
                writer.WriteNumber("coordinate", cell.Coordinate);
                writer.WriteNumber("adjustment", cell.Adjustment);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("losses");
            foreach (var cell in Sorted(definition.Losses, CompareLosses))
            {
                writer.WriteStartObject();
                WriteId(writer, "role", RoleName(cell.Role));
                writer.WriteNumber("differential", cell.Differential);
                writer.WriteNumber("coordinate", cell.Coordinate);
                writer.WriteNumber("lossPercent", cell.LossPercent);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("effects");
            foreach (var row in Sorted(definition.Effects, static (a, b) => a.Differential.CompareTo(b.Differential)))
            {
                writer.WriteStartObject();
                writer.WriteNumber("differential", row.Differential);
                WriteSet(writer, "attackerEngagedSums", row.AttackerEngagedSums);
                WriteSet(writer, "defenderRetreatOneHexSums", row.DefenderRetreatOneHexSums);
                WriteSet(writer, "attackerCaptureSums", row.AttackerCaptureSums);
                WriteSet(writer, "defenderCaptureSums", row.DefenderCaptureSums);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("captureShares");
            foreach (var share in Sorted(definition.CaptureShares, static (a, b) => a.Die.CompareTo(b.Die)))
            {
                writer.WriteStartObject();
                writer.WriteNumber("die", share.Die);
                writer.WriteNumber("percent", share.Percent);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            WriteProcedure(writer, definition.Procedure);
            WriteCosts(writer, definition.Costs);
            WriteSettlement(writer, definition.Settlement);
            writer.WriteStartArray("policies");
            foreach (var policy in Sorted(definition.Policies, static (a, b) =>
                string.Compare(a.PolicyId, b.PolicyId, StringComparison.Ordinal)))
            {
                writer.WriteStartObject();
                WriteId(writer, "policyId", policy.PolicyId);
                writer.WriteNumber("contractVersion", policy.ContractVersion);
                WriteId(writer, "selectedBehaviorId", policy.SelectedBehaviorId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        if (stream.Length > MaximumBytes)
        {
            throw new JsonException("The Combat RulesInput artifact exceeds the byte limit.");
        }
        return stream.ToArray();
    }

    private static void WriteProcedure(Utf8JsonWriter writer, CombatProcedure value)
    {
        writer.WriteStartObject("procedure");
        WriteId(writer, "procedureId", value.ProcedureId);
        WriteId(writer, "streamAlgorithm", value.StreamAlgorithm);
        writer.WriteNumber("acceptedByteUpperExclusive", value.AcceptedByteUpperExclusive);
        writer.WriteNumber("faces", value.Faces);
        WritePurposes(writer, "orderedPurposes", value.OrderedPurposes);
        WritePurposes(writer, "conditionalCapturePurposes", value.ConditionalCapturePurposes);
        writer.WriteEndObject();
    }

    private static void WriteCosts(Utf8JsonWriter writer, CombatCosts value)
    {
        writer.WriteStartObject("costs");
        writer.WriteNumber("committedToePerRole", value.CommittedToePerRole);
        writer.WriteNumber("ratingPerRole", value.RatingPerRole);
        writer.WriteNumber("basicMoralePerRole", value.BasicMoralePerRole);
        writer.WriteNumber("requiredCohesionPerRole", value.RequiredCohesionPerRole);
        writer.WriteNumber("baseCapabilityPointAllowance", value.BaseCapabilityPointAllowance);
        writer.WriteNumber("attackerCapabilityPoints", value.AttackerCapabilityPoints);
        writer.WriteNumber("defenderCapabilityPoints", value.DefenderCapabilityPoints);
        writer.WriteNumber("ammunitionPointsPerToe", value.AmmunitionPointsPerToe);
        writer.WriteEndObject();
    }

    private static void WriteSettlement(Utf8JsonWriter writer, CombatSettlementRules value)
    {
        writer.WriteStartObject("settlement");
        WriteId(writer, "attackerLossRounding", value.AttackerLossRounding);
        WriteId(writer, "defenderLossRounding", value.DefenderLossRounding);
        WriteId(writer, "capturedLossRounding", value.CapturedLossRounding);
        writer.WriteNumber("refusalPercentPerHex", value.RefusalPercentPerHex);
        writer.WriteNumber("lossDpThresholdToe", value.LossDpThresholdToe);
        writer.WriteNumber("lossDpPoints", value.LossDpPoints);
        writer.WriteNumber("victoryRpPoints", value.VictoryRpPoints);
        writer.WriteNumber("cohesionUpperCap", value.CohesionUpperCap);
        writer.WriteNumber("clearRetreatCapabilityPointsPerHex", value.ClearRetreatCapabilityPointsPerHex);
        writer.WriteNumber("guardToe", value.GuardToe);
        writer.WriteNumber("guardCapabilityPointAllowance", value.GuardCapabilityPointAllowance);
        writer.WriteNumber("guardAttackRating", value.GuardAttackRating);
        writer.WriteNumber("guardDefenseRating", value.GuardDefenseRating);
        writer.WriteNumber("guardInitialAmmunition", value.GuardInitialAmmunition);
        writer.WriteNumber("guardedPathMaximumHexes", value.GuardedPathMaximumHexes);
        writer.WriteNumber("escapePathMaximumCapabilityPoints", value.EscapePathMaximumCapabilityPoints);
        writer.WriteNumber("replacementDelayOperationStages", value.ReplacementDelayOperationStages);
        writer.WriteNumber("operationStagesPerGameTurn", value.OperationStagesPerGameTurn);
        writer.WriteEndObject();
    }

    private static T[] Sorted<T>(IReadOnlyList<T> values, Comparison<T> comparison)
    {
        CheckArray(values);
        var sorted = values.ToArray();
        Array.Sort(sorted, comparison);
        for (var i = 1; i < sorted.Length; i++)
        {
            if (comparison(sorted[i - 1], sorted[i]) == 0)
            {
                throw new JsonException("Duplicate Combat RulesInput array key.");
            }
        }
        return sorted;
    }

    private static void CheckArray<T>(IReadOnlyList<T> values)
    {
        if (values.Count > MaximumArrayItems)
        {
            throw new JsonException("The Combat RulesInput array exceeds the item limit.");
        }
    }

    private static void WriteSet(Utf8JsonWriter writer, string name, IReadOnlyList<int> values)
    {
        writer.WriteStartArray(name);
        foreach (var value in Sorted(values, static (a, b) => a.CompareTo(b)))
        {
            writer.WriteNumberValue(value);
        }
        writer.WriteEndArray();
    }

    private static void WritePurposes(Utf8JsonWriter writer, string name, IReadOnlyList<string> values)
    {
        CheckArray(values);
        writer.WriteStartArray(name);
        foreach (var value in values)
        {
            CheckId(value);
            writer.WriteStringValue(value);
        }
        writer.WriteEndArray();
    }

    private static string RoleName(CombatRole role) => role switch
    {
        CombatRole.Attacker => "attacker",
        CombatRole.Defender => "defender",
        _ => throw new JsonException("Unsupported Combat RulesInput role."),
    };

    private static int Compare(string firstA, string firstB, string secondA, string secondB)
    {
        var first = string.Compare(firstA, firstB, StringComparison.Ordinal);
        return first != 0 ? first : string.Compare(secondA, secondB, StringComparison.Ordinal);
    }

    private static int CompareLosses(CombatLossCell a, CombatLossCell b)
    {
        var role = string.Compare(RoleName(a.Role), RoleName(b.Role), StringComparison.Ordinal);
        var differential = a.Differential.CompareTo(b.Differential);
        return role != 0 ? role : differential != 0 ? differential : a.Coordinate.CompareTo(b.Coordinate);
    }

    private static void WriteId(Utf8JsonWriter writer, string name, string value)
    {
        CheckId(value);
        writer.WriteString(name, value);
    }

    private static void CheckId(string value)
    {
        if (value is null || value.Length is < 1 or > 128 || !char.IsAsciiLetterOrDigit(value[0])
            || value.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '_' or ':' or '-')))
        {
            throw new JsonException("Invalid Combat RulesInput identifier.");
        }
    }

    private static void WriteText(Utf8JsonWriter writer, string name, string value)
    {
        if (value is null || value.Length is < 1 or > 512 || value.Any(c => c is < ' ' or > '~'))
        {
            throw new JsonException("Invalid Combat RulesInput source locator.");
        }
        writer.WriteString(name, value);
    }

    private static void WriteHash(Utf8JsonWriter writer, string name, string value)
    {
        if (value is null || value.Length != 71 || !value.StartsWith("sha256:", StringComparison.Ordinal)
            || value.AsSpan(7).ContainsAnyExcept(LowerHexDigits))
        {
            throw new JsonException("Invalid Combat RulesInput source hash.");
        }
        writer.WriteString(name, value);
    }
}
