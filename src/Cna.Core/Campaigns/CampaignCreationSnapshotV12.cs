using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCreationReceiptV1(
    CampaignCombatCreationRequest Request,
    string CreationBinding,
    string CreationEventHash);

/// <summary>
/// Snapshot12's creation cut only. Construction requires separately trusted request and exact
/// validated Created11 evidence; later state must use its completed causal replay reader.
/// </summary>
internal sealed class CampaignCreationSnapshotV12
{
    private readonly CampaignCreatedV11 created;

    private CampaignCreationSnapshotV12(CampaignCreatedV11 created, ReadOnlySpan<byte> createdBytes)
    {
        this.created = created;
        CreationReceipt = new CampaignCreationReceiptV1(created.Request, created.Request.CreationBinding,
            FormatHash(SHA256.HashData(createdBytes)));
        Span<byte> length = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(length, checked((ulong)createdBytes.Length));
        using var prefix = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        prefix.AppendData("sandtable.cycle.prefix.creation.v1\0"u8);
        prefix.AppendData(length);
        prefix.AppendData(createdBytes);
        ChroniclePrefix = FormatHash(prefix.GetHashAndReset());
    }

    public int ContractVersion { get; } = 12;
    public string CampaignId => created.Request.CampaignId;
    public long StateVersion { get; } = 1;
    public string RulesetHash => created.Request.Context.RulesetHash;
    public CampaignSetupSnapshotV7 Setup => created.Request.Context.Setup;
    public CampaignWorldSnapshotV7 World => created.InitialWorld;
    public RandomStreamState RandomState => created.Request.RandomState;
    public LandSequencePosition SequencePosition => created.SequencePosition;
    public CombatDecisionConfiguration Configuration => created.Request.Context.Configuration;
    public CampaignCreationReceiptV1 CreationReceipt { get; }
    public string ChroniclePrefix { get; }

    public static CampaignCreationSnapshotV12 Create(ReadOnlySpan<byte> createdBytes,
        CampaignCombatCreationRequest trustedRequest)
    {
        // Validation precedes both digests. A self-consistent hash alone is not creation evidence.
        if (createdBytes.Length > 1_048_576) throw new JsonException("Created11 exceeds byte limit.");
        var evidence = createdBytes.ToArray();
        var created = CampaignCreatedV11Serializer.Deserialize(evidence, trustedRequest);
        return new CampaignCreationSnapshotV12(created, evidence);
    }

    private static string FormatHash(ReadOnlySpan<byte> digest) =>
        "sha256:" + Convert.ToHexString(digest).ToLowerInvariant();
}
