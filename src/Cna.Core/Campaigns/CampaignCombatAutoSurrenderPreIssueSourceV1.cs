using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>
/// Exact private Content8 source bytes retained for a future creation contract.
/// No campaign identity, authority, Created event or ledger can be issued from this type.
/// </summary>
internal sealed class CampaignCombatAutoSurrenderPreIssueSourceV1
{
    private const string Kind = "private-combat-auto-surrender-source-preissue";
    private const int MaximumEnvelopeBytes = 65_536;
    private readonly byte[] contentBytes;
    private readonly byte[] rulesBytes;
    private readonly byte[] setupBytes;
    private readonly CampaignCombatAutoSurrenderSourceV1 source;

    private CampaignCombatAutoSurrenderPreIssueSourceV1(
        byte[] contentBytes, byte[] rulesBytes, byte[] setupBytes)
    {
        source = CampaignCombatAutoSurrenderSourceV1.Read(contentBytes, rulesBytes, setupBytes);
        this.contentBytes = (byte[])contentBytes.Clone();
        this.rulesBytes = (byte[])rulesBytes.Clone();
        this.setupBytes = (byte[])setupBytes.Clone();
    }

    public string SourceSealId
    {
        get
        {
            var prefix = "sandtable.combat.auto-surrender.preissue-source.v1\0"u8;
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            hash.AppendData(prefix);
            hash.AppendData(Serialize());
            return "source." + Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        }
    }

    public static CampaignCombatAutoSurrenderPreIssueSourceV1 Read(
        ReadOnlySpan<byte> contentBytes, ReadOnlySpan<byte> rulesBytes, ReadOnlySpan<byte> setupBytes) =>
        new(contentBytes.ToArray(), rulesBytes.ToArray(), setupBytes.ToArray());

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("kind", Kind);
            WriteArtifact(writer, "content", contentBytes);
            WriteArtifact(writer, "rulesSource", rulesBytes);
            WriteArtifact(writer, "setup", setupBytes);
            writer.WriteStartObject("closedInitialInventory");
            writer.WriteString("attackerElementId", source.AttackerElementId);
            writer.WriteNumber("attackerAmmunition", source.AttackerInitialAmmunition);
            writer.WriteString("defenderElementId", source.DefenderElementId);
            writer.WriteNumber("defenderAmmunition", source.DefenderInitialAmmunition);
            writer.WriteNumber("cohesionLevel", 0);
            writer.WriteStartArray("externalAmmunitionSources");
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignCombatAutoSurrenderPreIssueSourceV1 Deserialize(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > MaximumEnvelopeBytes)
            throw new JsonException("Private pre-issue source seal exceeds byte limit.");
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(),
                new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (root.GetProperty("contractVersion").GetInt32() != 1 ||
                root.GetProperty("kind").GetString() != Kind)
                throw new JsonException("Unsupported private pre-issue source seal.");
            var seal = new CampaignCombatAutoSurrenderPreIssueSourceV1(
                ReadArtifact(root, "content"), ReadArtifact(root, "rulesSource"),
                ReadArtifact(root, "setup"));
            if (!bytes.SequenceEqual(seal.Serialize()))
                throw new JsonException("Private pre-issue source seal is not canonical.");
            return seal;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or OverflowException)
        {
            throw new JsonException("Invalid private pre-issue source seal.", exception);
        }
    }

    private static void WriteArtifact(Utf8JsonWriter writer, string name, byte[] bytes)
    {
        writer.WriteStartObject(name);
        writer.WriteString("hash", "sha256:" +
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        writer.WriteString("bytesBase64", Convert.ToBase64String(bytes));
        writer.WriteEndObject();
    }

    private static byte[] ReadArtifact(JsonElement root, string name) =>
        Convert.FromBase64String(root.GetProperty(name).GetProperty("bytesBase64").GetString()!);
}
