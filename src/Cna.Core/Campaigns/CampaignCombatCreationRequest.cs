using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Independently retained inputs; never resolved from an imported creation envelope.</summary>
internal sealed class CampaignCombatCreationContext
{
    public CampaignCombatCreationContext(RulesetManifest ruleset, CampaignSetupSnapshotV7 setup,
        ContentPackV7Artifact artifact, ContentCombatScenario scenario,
        CombatDecisionConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(ruleset);
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(configuration);
        // Predecessor stable-ID guards enforce syntax but not the envelope's length bound.
        if (setup.SetupId.Length > 128 || configuration.ConfigId.Length > 128)
            throw new JsonException("Combat creation Setup and configuration IDs exceed envelope bounds.");
        if (!Cna1979CombatRuleset.IsCanonicalHash(ruleset.Hash) ||
            !ContentPackV7Validator.Validate(artifact.Definition).IsValid)
            throw new JsonException("Combat creation requires supported trusted Rules10 and Content7.");
        Setup = CampaignSetupV7Codec.Deserialize(CampaignSetupV7Codec.Serialize(setup), artifact, scenario);
        Configuration = CombatDecisionConfigurationCodec.Deserialize(
            CombatDecisionConfigurationCodec.Serialize(configuration), ruleset);
        RulesetHash = ruleset.Hash;
    }

    public string RulesetHash { get; }
    public CampaignSetupSnapshotV7 Setup { get; }
    public CombatDecisionConfiguration Configuration { get; }
}

internal sealed class CampaignCombatCreationRequest
{
    private CampaignCombatCreationRequest(string campaignId, ulong seed, CampaignCombatCreationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        CampaignId = ContentContractGuards.RequireSourceAtom(campaignId, nameof(campaignId));
        Context = context;
        RandomState = new RandomStreamState(1, SandtableRandom.AlgorithmId, seed, 0);
        var preimage = Encoding.ASCII.GetBytes("sandtable.combat.creation-request.v1\0")
            .Concat(CampaignCombatCreationRequestCodec.Serialize(this)).ToArray();
        CreationBinding = "creation." + Convert.ToHexString(SHA256.HashData(preimage)).ToLowerInvariant();
    }

    public string CampaignId { get; }
    public CampaignCombatCreationContext Context { get; }
    public RandomStreamState RandomState { get; }
    public string CreationBinding { get; }

    public static CampaignCombatCreationRequest Create(string campaignId, ulong seed,
        CampaignCombatCreationContext context) => new(campaignId, seed, context);
}

internal static class CampaignCombatCreationRequestCodec
{
    public static byte[] Serialize(CampaignCombatCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) Write(writer, request);
        return stream.ToArray();
    }

    internal static void Write(Utf8JsonWriter writer, CampaignCombatCreationRequest request)
    {
        var setup = request.Context.Setup;
        var identity = setup.Artifact.Identity;
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 1);
        writer.WriteString("campaignId", request.CampaignId);
        writer.WriteString("rulesetHash", request.Context.RulesetHash);
        writer.WriteString("setupId", setup.SetupId);
        writer.WriteString("setupHash", setup.SetupHash);
        writer.WriteStartObject("content");
        writer.WriteNumber("schemaVersion", identity.SchemaVersion);
        writer.WriteString("formatId", identity.FormatId);
        writer.WriteString("packId", identity.PackId);
        writer.WriteString("rulesetId", identity.RulesetId);
        writer.WriteString("hash", identity.Hash);
        writer.WriteString("scenarioId", setup.Scenario.ScenarioId);
        writer.WriteEndObject();
        writer.WriteString("configurationHash", request.Context.Configuration.Hash);
        CampaignSnapshotSerializer.WriteRandomState(writer, request.RandomState);
        writer.WriteEndObject();
    }

    public static CampaignCombatCreationRequest Deserialize(ReadOnlySpan<byte> bytes,
        CampaignCombatCreationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (bytes.Length > 1_048_576) throw new JsonException("Combat creation request exceeds byte limit.");
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            // Only caller-selected identity and seed are read. All other fields are reconstructed
            // from independently trusted context, then compared including exact JSON spelling.
            var request = CampaignCombatCreationRequest.Create(root.GetProperty("campaignId").GetString()!,
                root.GetProperty("randomState").GetProperty("seed").GetUInt64(), context);
            if (!bytes.SequenceEqual(Serialize(request)))
                throw new JsonException("Combat creation request differs from canonical trusted inputs.");
            return request;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or OverflowException or FormatException)
        {
            throw new JsonException("Invalid Combat creation request.", exception);
        }
    }
}
