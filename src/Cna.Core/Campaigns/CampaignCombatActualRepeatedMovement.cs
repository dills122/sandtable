using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultSupport;
using static Cna.Core.Campaigns.CampaignCombatActualRepeatedMovementCodec;

namespace Cna.Core.Campaigns;

internal sealed class CombatActualRepeatedMovementResult
{
    private readonly byte[] source;
    private readonly byte[] control;
    private readonly byte[] proof;
    private readonly byte[][] inputs;
    private readonly byte[]? acceptedEvent;

    internal CombatActualRepeatedMovementResult(byte[] sourceBytes, byte[] controlBytes, byte[] proofBytes,
        IReadOnlyList<byte[]> movementInputs, byte[]? eventBytes = null, string? receiptId = null, bool duplicate = false)
    {
        source = sourceBytes.ToArray();
        control = controlBytes.ToArray();
        proof = proofBytes.ToArray();
        inputs = movementInputs.Select(x => x.ToArray()).ToArray();
        acceptedEvent = eventBytes?.ToArray();
        ReceiptId = receiptId;
        Duplicate = duplicate;
    }

    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public IReadOnlyList<byte[]> MovementInputs => inputs.Select(x => x.ToArray()).ToArray();
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

/// <summary>Private native consumer of authentic repeated Movement; semantic RED boundary.</summary>
internal static class CampaignCombatActualRepeatedMovement
{
    public static CombatActualRepeatedMovementResult ApplyTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement,
        ReadOnlySpan<byte> newInput, Func<string, byte[]> dependencies)
    {
        CampaignCombatActualRepeatedMovementCodec.VerifyDependencies(dependencies); var packet = Object(source, "Source");
        try { _ = CampaignCombatActualPostAssaultContinuation.ReplayTrustedSource(Encoding.ASCII.GetBytes(T(packet, "continuationSourceCanonicalUtf8")), context, selection, rounds, paid, continuation, dependencies); }
        catch (JsonException e) { throw new JsonException(string.Concat("CMB-RMV-", e.Message.AsSpan(8, 3)), e); }
        Require(movement is not null && movement.Count <= 4 && movement.All(x => x is not null), 1);
        foreach (var input in movement) _ = Parse(input, "Input");
        Require(Encode(A(movement.Select(x => JsonNode.Parse(x)))).Length <= 8192, 1);
        Require(packet["movementEventCanonicalUtf8"]!.AsArray().Count == movement.Count, 4);
        Require(movement.Count == 0, 7); _ = Parse(newInput, "Input");
        throw new JsonException("CMB-RMV-007");
    }
}
