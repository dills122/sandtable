using System.Text.Json;

namespace Cna.Core.Campaigns;

internal sealed class CombatActualPaidAssaultResult
{
    private readonly byte[] source;
    private readonly byte[] control;
    private readonly byte[] proof;
    private readonly byte[][] inputs;
    private readonly byte[]? acceptedEvent;

    internal CombatActualPaidAssaultResult(byte[] sourceBytes, byte[] controlBytes, byte[] proofBytes,
        IReadOnlyList<byte[]> paidInputs, byte[]? eventBytes = null, string? receiptId = null, bool duplicate = false)
    {
        source = sourceBytes.ToArray();
        control = controlBytes.ToArray();
        proof = proofBytes.ToArray();
        inputs = paidInputs.Select(x => x.ToArray()).ToArray();
        acceptedEvent = eventBytes?.ToArray();
        ReceiptId = receiptId;
        Duplicate = duplicate;
    }

    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public IReadOnlyList<byte[]> PaidInputs => inputs.Select(x => x.ToArray()).ToArray();
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

/// <summary>
/// Private native consumer of full actual paid-assault history. Independently retained
/// selection, round and paid inputs remain caller-authenticated authority. Dependency
/// acquisition belongs outside authoritative turns. Semantic unsupported TDD checkpoint.
/// </summary>
internal static class CampaignCombatActualPaidAssault
{
    public static CombatActualPaidAssaultResult ReplayTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> trustedSelectionInputs,
        IReadOnlyList<byte[]> trustedRoundInputs, IReadOnlyList<byte[]> trustedPaidInputs,
        Func<string, byte[]> dependencyBytes) => throw new JsonException("CMB-APA-007");

    public static CombatActualPaidAssaultResult ApplyTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> trustedSelectionInputs,
        IReadOnlyList<byte[]> trustedRoundInputs, IReadOnlyList<byte[]> trustedPaidInputs,
        ReadOnlySpan<byte> newInput, Func<string, byte[]> dependencyBytes, bool admissionEnabled = true)
        => throw new JsonException("CMB-APA-007");
}
