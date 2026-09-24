using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatArmedContinuationCodec
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    // Frozen3j contract-support identities, not claims of current full Snapshot/campaign admission.
    private static readonly (string Contract, string Hash, int Count)[] Support =
    [
        ("combat-selection-steps-v1", "sha256:151c8da5037dfd5fa921abf1f675ef1a10211f8b84a4e1fb385d6beb62b1d0de", 5),
        ("combat-sealed-round-v1", "sha256:8832568d97d3bd7cfe0d8f5dcd875bed58ea401250c38a178afeb7b1662cef6e", 4),
        ("combat-result-settlement-v1", "sha256:cc25c358c997e54dd61fe1d843db5e7385c4a85706c462b87badacb939ec71c2", 8),
        ("combat-snapshot-composition-v1", "sha256:392e805f3ee244d9dc988a64b85090d9f992335721fbbc91e8b5b6e2260f834a", 17)
    ];
    public static byte[] Serialize(CampaignCombatArmedContinuation.Proof proof)
    {
        ArgumentNullException.ThrowIfNull(proof);
        var p = proof.Source; var a = proof.Candidate.Attacker; var d = proof.Candidate.Defender;
        var acting = p.World.Elements.Single(e => e.ElementId == a.Unit.ElementId);
        var defending = p.World.Elements.Single(e => e.ElementId == d.Unit.ElementId);
        var basis = JsonNode.Parse(CampaignCombatReserveReleaseCodec.SerializeBase(p.Base.ReleaseBase, proof.Request))!;
        var state = JsonNode.Parse(CampaignCombatReserveReleaseCodec.SerializeState(p.Release))!;
        var support = JsonSerializer.SerializeToNode(Support.Select(s => new { contractId = s.Contract, fixtureHash = s.Hash, evidenceCount = s.Count }), Options)!;
        var candidateId = "cand." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.inherited-selection-candidate.v1",
            JsonSerializer.SerializeToUtf8Bytes(new[] { a.Unit.ElementId, d.Unit.ElementId, d.LocationId }))[7..];
        var assessment = JsonSerializer.SerializeToNode(new
        {
            actingSide = a.Unit.OriginalSide,
            actingElementId = a.Unit.ElementId,
            defendingSide = d.Unit.OriginalSide,
            defendingElementId = d.Unit.ElementId,
            actingLocationId = a.LocationId,
            defendingLocationId = d.LocationId,
            normalWeather = true,
            adjacent = true,
            actingCapabilityPointsExpended = acting.OperationalState.CapabilityPointsExpended,
            defendingCapabilityPointsExpended = defending.OperationalState.CapabilityPointsExpended,
            actingWithinVoluntaryCeiling = true,
            defendingWithinVoluntaryCeiling = true,
            candidateIds = new[] { candidateId }
        }, Options);
        var node = new JsonObject
        {
            ["contractVersion"] = 1,
            ["predecessorHash"] = proof.PredecessorHash,
            ["currentCycle"] = basis["cycle"]!.DeepClone(),
            ["nextOrdinal"] = 2,
            ["releaseCompletionReceiptId"] = p.Release.CompletionReceiptId,
            ["movementCompletionReceiptId"] = p.Base.Predecessor.MovementEnd!.CompletionReceiptId,
            ["progress"] = JsonSerializer.SerializeToNode(p.Progress.Single(), Options),
            ["member"] = state["members"]![0]!.DeepClone(),
            ["retainedWorldHash"] = p.Release.RetainedWorldHash,
            ["randomStateHash"] = Hash(Encode(basis["randomState"]!)),
            ["actingAmmunition"] = acting.Ammunition.Points,
            ["actingToe"] = acting.Components.Sum(c => c.CurrentToe),
            ["defendingToe"] = defending.Components.Sum(c => c.CurrentToe),
            ["assessment"] = assessment,
            ["emptyMovementSupported"] = true,
            ["emptyBreakdownSupported"] = true,
            ["emptyPrestepsSupported"] = true,
            ["targetUseAvailable"] = true,
            ["offensiveUseAvailable"] = true,
            ["immediateObligationsClear"] = true,
            ["support"] = support,
            ["supportDigest"] = Hash(Encode(support)),
            ["supported"] = true
        };
        return Encode(node);
    }
    public static CampaignCombatArmedContinuation.Proof Read(ReadOnlySpan<byte> bytes, CombatInheritedReserveReleaseSource source,
        IReadOnlyList<CombatReleaseInput> inputs, IReadOnlyList<byte[]> events)
    {
        Check(bytes);
        var proof = CampaignCombatArmedContinuation.Derive(source, inputs, events);
        if (!bytes.SequenceEqual(Serialize(proof))) throw new JsonException("Armed proof differs from authenticated Release history.");
        return proof;
    }
    private static string Hash(byte[] bytes) => CampaignOpeningPreambleCodec.Hash(bytes);
    private static byte[] Encode(JsonNode node)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(node, Options); Check(bytes); return bytes;
    }
    private static void Check(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Armed proof size exceeds bounds.");
        using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
    }
}
