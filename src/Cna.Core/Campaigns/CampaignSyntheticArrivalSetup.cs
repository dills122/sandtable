using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Trusted only for the isolated synthetic phase; not admitted by campaign creation.
internal sealed class CampaignSyntheticArrivalSetup
{
    public const string PinnedHash = "sha256:0db4b957cad11cfe2d7189bbe01eab58de105c741332cfb402e7b3ba86a0b847";
    private readonly byte[] canonicalBytes;

    private CampaignSyntheticArrivalSetup()
    {
        var start = CampaignSyntheticArrivalPhase.ArrivalPosition();
        var successor = Cna1979LandSequence.GetNext(start);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("format", "sandtable.synthetic-arrival-setup.v1");
            writer.WriteString("setupId", "rules-lab.synthetic-arrival.v1");
            writer.WriteNumber("schemaVersion", 1);
            writer.WriteString("profileHash", CampaignArrivalSyntheticProfile.ProfileHash);
            writer.WriteString("startPositionId", start.PositionId);
            writer.WriteString("successorPositionId", successor.PositionId);
            writer.WriteString("otherArrivalObligations", "synthetic-none");
            writer.WriteEndObject();
        }
        canonicalBytes = stream.ToArray();
        Hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(canonicalBytes));
        if (Hash != PinnedHash)
            throw new InvalidOperationException("Synthetic arrival setup no longer matches its pinned version.");
    }

    public static CampaignSyntheticArrivalSetup Instance { get; } = new();
    public string Hash { get; }
    public byte[] CopyCanonicalBytes() => canonicalBytes.ToArray();

    public static CampaignSyntheticArrivalSetup Resolve(ReadOnlySpan<byte> bytes, string expectedHash)
    {
        if (expectedHash != Instance.Hash || !bytes.SequenceEqual(Instance.canonicalBytes))
            throw new ArgumentException("Unsupported synthetic arrival setup.", nameof(bytes));
        return Instance;
    }
}
