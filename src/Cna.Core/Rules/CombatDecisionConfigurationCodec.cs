using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Rules;

internal sealed record CombatDecisionWindowBudget(string Kind, int DecisionBudgetMilliseconds,
    string Fallback);

internal sealed class CombatDecisionConfiguration
{
    public CombatDecisionConfiguration(string configId, string rulesInputHash,
        IEnumerable<CombatDecisionWindowBudget> windows)
    {
        ConfigId = ContentContractGuards.RequireStableId(configId, nameof(configId));
        RulesInputHash = ContentContractGuards.RequireSha256(rulesInputHash, nameof(rulesInputHash));
        ArgumentNullException.ThrowIfNull(windows);
        if (RulesInputHash != Cna1979CombatRuleset.Manifest.Artifacts.Single(value =>
                value.ArtifactId == "cna-1979.1.combat-selected-inputs.v1").ContentHash)
            throw new ArgumentException("Unsupported Combat selected-input artifact.", nameof(rulesInputHash));
        var windowCopy = windows.ToArray();
        if (windowCopy.Any(value => value is null))
            throw new ArgumentException("Null Combat window is unsupported.", nameof(windows));
        Windows = Array.AsReadOnly(windowCopy.OrderBy(value => value.Kind, StringComparer.Ordinal).ToArray());
        if (Windows.Count != CombatDecisionConfigurationCodec.RequiredFallbacks.Count ||
            Windows.Any(value => value is null ||
                !CombatDecisionConfigurationCodec.RequiredFallbacks.TryGetValue(value.Kind, out var fallback) ||
                value.Fallback != fallback || value.DecisionBudgetMilliseconds < 1) ||
            Windows.Select(value => value.Kind).Distinct(StringComparer.Ordinal).Count() != Windows.Count)
            throw new ArgumentException("All seven supported window kinds need positive budgets and exact fallbacks.",
                nameof(windows));
        Hash = $"sha256:{Convert.ToHexString(SHA256.HashData(
            CombatDecisionConfigurationCodec.Serialize(this))).ToLowerInvariant()}";
    }

    public string ConfigId { get; }
    public string RulesInputHash { get; }
    public IReadOnlyList<CombatDecisionWindowBudget> Windows { get; }
    public string Hash { get; }
}

internal static class CombatDecisionConfigurationCodec
{
    internal static readonly IReadOnlyDictionary<string, string> RequiredFallbacks =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["custody"] = "leave-unguarded",
            ["cycle-control"] = "finish-phase",
            ["force-assignment"] = "cancel-incomplete-voluntary-round",
            ["rba"] = "cancel-without-decline",
            ["reserve-release"] = "convert-unresolved-i-retain-ii",
            ["retreat"] = "refuse-retreat",
            ["selection"] = "no-selection",
        };

    public static byte[] Serialize(CombatDecisionConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 1);
            writer.WriteString("configId", config.ConfigId);
            writer.WriteString("profileId", ContentPackV7Definition.SupportedCapabilityProfileId);
            writer.WriteString("rulesInputHash", config.RulesInputHash);
            writer.WriteString("codecId", "sandtable.combat.inputs-json.v1");
            writer.WriteString("timingPolicyId", "sandtable.combat.fixed-deadline.v1");
            writer.WriteStartArray("windows");
            foreach (var window in config.Windows)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", window.Kind);
                writer.WriteNumber("decisionBudgetMilliseconds", window.DecisionBudgetMilliseconds);
                writer.WriteString("fallback", window.Fallback);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CombatDecisionConfiguration Deserialize(ReadOnlySpan<byte> bytes, RulesetManifest ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);
        if (ruleset.Hash != Cna1979CombatRuleset.Manifest.Hash || bytes.Length > 1_048_576)
            throw new JsonException("Unsupported Combat ruleset or configuration size.");
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            RequireProperties(root, "schemaVersion", "configId", "profileId", "rulesInputHash",
                "codecId", "timingPolicyId", "windows");
            if (root.GetProperty("schemaVersion").GetInt32() != 1 ||
                root.GetProperty("profileId").GetString() != ContentPackV7Definition.SupportedCapabilityProfileId ||
                root.GetProperty("codecId").GetString() != "sandtable.combat.inputs-json.v1" ||
                root.GetProperty("timingPolicyId").GetString() != "sandtable.combat.fixed-deadline.v1")
                throw new JsonException("Unsupported Combat configuration identity.");
            var selectedHash = ruleset.Artifacts.Single(value =>
                value.ArtifactId == "cna-1979.1.combat-selected-inputs.v1").ContentHash;
            if (root.GetProperty("rulesInputHash").GetString() != selectedHash)
                throw new JsonException("Combat configuration has a foreign selected-input artifact.");
            var windows = root.GetProperty("windows").EnumerateArray().Select(value =>
            {
                RequireProperties(value, "kind", "decisionBudgetMilliseconds", "fallback");
                return new CombatDecisionWindowBudget(value.GetProperty("kind").GetString()!,
                    value.GetProperty("decisionBudgetMilliseconds").GetInt32(),
                    value.GetProperty("fallback").GetString()!);
            });
            var config = new CombatDecisionConfiguration(root.GetProperty("configId").GetString()!,
                selectedHash, windows);
            if (!bytes.SequenceEqual(Serialize(config)))
                throw new JsonException("Combat configuration requires canonical bytes.");
            return config;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or OverflowException or FormatException)
        {
            throw new JsonException("Invalid Combat configuration.", exception);
        }
    }

    private static void RequireProperties(JsonElement value, params string[] expected)
    {
        if (value.ValueKind != JsonValueKind.Object ||
            !value.EnumerateObject().Select(property => property.Name)
                .SequenceEqual(expected, StringComparer.Ordinal))
            throw new JsonException("Combat configuration property contract is invalid.");
    }
}
