using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

internal sealed class CombatActualPostAssaultContinuationResult
{
    private readonly byte[] source;
    private readonly byte[] control;
    private readonly byte[] proof;
    private readonly byte[][] inputs;
    private readonly byte[]? acceptedEvent;

    internal CombatActualPostAssaultContinuationResult(byte[] sourceBytes, byte[] controlBytes, byte[] proofBytes,
        IReadOnlyList<byte[]> continuationInputs, byte[]? eventBytes = null, string? receiptId = null, bool duplicate = false)
    {
        source = sourceBytes.ToArray();
        control = controlBytes.ToArray();
        proof = proofBytes.ToArray();
        inputs = continuationInputs.Select(x => x.ToArray()).ToArray();
        acceptedEvent = eventBytes?.ToArray();
        ReceiptId = receiptId;
        Duplicate = duplicate;
    }

    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public IReadOnlyList<byte[]> ContinuationInputs => inputs.Select(x => x.ToArray()).ToArray();
    public byte[]? EventBytes => acceptedEvent?.ToArray();
    public JsonElement Control => Read(control);
    public JsonElement Proof => Read(proof);
    public string? ReceiptId { get; }
    public bool Duplicate { get; }

    private static JsonElement Read(byte[] data)
    {
        using var document = JsonDocument.Parse(data);
        return document.RootElement.Clone();
    }
}

/// <summary>Compile-valid missing-consumer boundary for semantic RED only.</summary>
internal static class CampaignCombatActualPostAssaultContinuation
{
    public static CombatActualPostAssaultContinuationResult ApplyTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, ReadOnlySpan<byte> newInput, Func<string, byte[]> dependencies)
    {
        using var document = JsonDocument.Parse(source.ToArray());
        var predecessor = Encoding.ASCII.GetBytes(document.RootElement.GetProperty("paidSourceCanonicalUtf8").GetString()!);
        _ = CampaignCombatActualPaidAssault.ReplayTrustedSource(predecessor, context, selection, rounds, paid, dependencies);
        throw new JsonException("CMB-PAC-007");
    }
}
