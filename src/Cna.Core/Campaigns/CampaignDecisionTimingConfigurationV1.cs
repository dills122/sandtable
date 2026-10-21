using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>
/// Explicit timing policy selected at normal campaign creation. Every decision window has a
/// pinned budget; the policy is retained with the campaign rather than inferred by the host.
/// </summary>
public sealed class CampaignDecisionTimingConfigurationV1
{
    public const int CurrentContractVersion = 1;
    public const string PolicyId = "sandtable.campaign.fixed-deadline.v1";
    private readonly IReadOnlyList<CampaignDecisionWindowV1> windows;

    public CampaignDecisionTimingConfigurationV1(string configId, string rulesetHash,
        string contentHash, IEnumerable<CampaignDecisionWindowV1> windows)
    {
        ConfigId = ContentContractGuards.RequireStableId(configId, nameof(configId));
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesetHash);
        RulesetHash = rulesetHash;
        ContentHash = ContentContractGuards.RequireSha256(contentHash, nameof(contentHash));
        ArgumentNullException.ThrowIfNull(windows);
        var ordered = windows.OrderBy(value => value.Kind, StringComparer.Ordinal).ToArray();
        if (ordered.Length != CombatDecisionConfigurationCodec.RequiredFallbacks.Count
            || ordered.Any(value => value is null
                || !CombatDecisionConfigurationCodec.RequiredFallbacks.TryGetValue(
                    value.Kind, out var fallback)
                || value.Fallback != fallback || value.DecisionBudgetMilliseconds < 1)
            || ordered.Select(value => value.Kind).Distinct(StringComparer.Ordinal).Count() != ordered.Length)
            throw new ArgumentException("Every supported decision needs an explicit positive budget and fallback.",
                nameof(windows));
        this.windows = Array.AsReadOnly(ordered);
        Hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
    }

    public string ConfigId { get; }
    public string RulesetHash { get; }
    public string ContentHash { get; }
    public IReadOnlyList<CampaignDecisionWindowV1> Windows => windows;
    public string Hash { get; }

    public int Budget(string kind) => windows.Single(value => value.Kind == kind).DecisionBudgetMilliseconds;

    public byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("configId", ConfigId);
            writer.WriteString("rulesetHash", RulesetHash);
            writer.WriteString("contentHash", ContentHash);
            writer.WriteString("timingPolicyId", PolicyId);
            writer.WriteStartArray("windows");
            foreach (var window in windows)
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

    public static CampaignDecisionTimingConfigurationV1 DeserializeCanonical(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 4096) throw new JsonException("Invalid campaign timing size.");
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            RequireProperties(root, "contractVersion", "configId", "rulesetHash", "contentHash",
                "timingPolicyId", "windows");
            if (root.GetProperty("contractVersion").GetInt32() != CurrentContractVersion
                || root.GetProperty("timingPolicyId").GetString() != PolicyId)
                throw new JsonException("Unsupported campaign timing policy.");
            var values = root.GetProperty("windows").EnumerateArray().Select(value =>
            {
                RequireProperties(value, "kind", "decisionBudgetMilliseconds", "fallback");
                return new CampaignDecisionWindowV1(value.GetProperty("kind").GetString()!,
                    value.GetProperty("decisionBudgetMilliseconds").GetInt32(),
                    value.GetProperty("fallback").GetString()!);
            });
            var configuration = new CampaignDecisionTimingConfigurationV1(
                root.GetProperty("configId").GetString()!,
                root.GetProperty("rulesetHash").GetString()!,
                root.GetProperty("contentHash").GetString()!, values);
            if (!bytes.SequenceEqual(configuration.SerializeCanonical()))
                throw new JsonException("Noncanonical campaign timing configuration.");
            return configuration;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or OverflowException or FormatException)
        {
            throw new JsonException("Invalid campaign timing configuration.", exception);
        }
    }

    private static void RequireProperties(JsonElement value, params string[] properties)
    {
        if (value.ValueKind != JsonValueKind.Object
            || !value.EnumerateObject().Select(item => item.Name)
                .SequenceEqual(properties, StringComparer.Ordinal))
            throw new JsonException("Campaign timing property contract is invalid.");
    }
}

public sealed record CampaignDecisionWindowV1(string Kind, int DecisionBudgetMilliseconds,
    string Fallback);
