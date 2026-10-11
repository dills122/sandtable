using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultCodec;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultSupport;

namespace Cna.Core.Campaigns;

/// <summary>Private native admission-only prerequisite. Capability flags remain false.</summary>
internal static class CampaignCombatActualAssaultAdmission
{
    internal static PaidAssaultFrame Admit(byte[] data, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds, Func<string, byte[]> dependencies)
    {
        (selection, rounds) = OwnLedgers(selection, rounds);
        var source = Object(data, "AdmissionSource");
        var original = Encoding.ASCII.GetBytes(T(source, "actualRoundSourceCanonicalUtf8")); CombatActualRoundEntryResult result;
        try { result = CampaignCombatActualRoundEntry.ReplayTrustedSource(original, context, selection, rounds, dependencies); }
        catch (JsonException e) { throw new JsonException(e.Message.StartsWith("CMB-ARE-009", StringComparison.Ordinal) ? "CMB-APA-009" : "CMB-APA-004", e); }
        var prior = JsonNode.Parse(result.ProofBytes)!.AsObject(); var control = prior["control"]!.AsObject();
        var originalSource = CampaignCombatActualRoundEntryCodec.Object(original, "ActualRoundSource");
        Require(N(control, "stateVersion") == 25 && N(control, "stepIndex") == 5 && T(control, "status") == "prepared" && !B(control, "closed") &&
            control["slots"]!.AsArray().Count == 2 && control["slots"]!.AsArray().All(x => x!["sealedReceiptId"] is not null) && control["stepReceipts"]!.AsArray().Count == 5 &&
            originalSource["roundEventCanonicalUtf8"]!.AsArray().Count == 5 && control["cancellationReceiptId"] is null && control["commitmentId"] is null &&
            control["attackHistory"]!.AsArray().Count == 0 && control["targetUses"]!.AsArray().Count == 0, 4);
        var nativeRules = CombatRulesInputArtifactCodec.SerializeCanonical(Cna1979CombatAdjudication.Definition);
        _ = CombatRulesInputArtifactCodec.Deserialize(nativeRules, Cna1979CombatAdjudication.ContentHash); Require(Hash(nativeRules) == context.Configuration.RulesInputHash, 6);
        var assessment = O(("contractVersion", 1), ("scopeKind", "pure-selected-arithmetic-v1"), ("actualRoundSourceHash", prior["sourceHash"]),
            ("dependencyManifestHash", Manifest(true)), ("rulesInputHash", Hash(nativeRules)), ("moralePairs", 1296), ("assaultPairs", 6480),
            ("captureRefusalVariants", 8840), ("arithmeticCatalogueHash", CatalogueHash()), ("paidAdmissionEnabled", false), ("nativeSettlementCertified", false), ("authenticSettlementCertified", false));
        var digest = Domain("sandtable.combat.actual-assault.admission-source.v1", Canonical(source, "AdmissionSource"));
        var proof = O(("contractVersion", 1), ("sourceId", "aaadm." + digest), ("sourceHash", "sha256:" + digest), ("actualRoundProof", prior), ("arithmeticAssessment", assessment));
        _ = Canonical(proof, "AdmissionProof"); var basis = O(("contractVersion", 1), ("dependencyManifestHash", Manifest(false)), ("admissionProof", proof), ("resultClockPolicyId", ClockPolicy));
        var actualSelection = prior["base"]!["actualSelectionProof"]!;
        return new(basis, control, actualSelection["control"]!["selection"]!.AsObject(), actualSelection["boundary"]!.AsObject(), actualSelection["boundary"]!["cycle"]!.AsObject(),
            context.Configuration, JsonNode.Parse(Consume("docs/specs/fixtures/combat-content-v7.canonical.json", dependencies))!.AsObject());
    }
    // Authenticate syntax independently so replay mismatches retain their distinct APA004 meaning.
    internal static (byte[][] Selection, byte[][] Rounds) OwnLedgers(IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds)
    {
        byte[][] Own(IReadOnlyList<byte[]> ledger, string kind)
        {
            Require(ledger is not null && ledger.Count <= 16, 1);
            var owned = ledger.Select(input =>
            {
                Require(input is not null, 1);
                var copy = input.ToArray(); _ = Parse(copy, kind); return copy;
            }).ToArray();
            Require(Encode(A(owned.Select(input => JsonNode.Parse(input)))).Length <= 1_048_576, 1);
            return owned;
        }
        return (Own(selection, "Input"), Own(rounds, "RoundInput"));
    }
    internal static string CatalogueHash()
    {
        var header = O(("kind", "pure-selected-arithmetic-catalogue-v1"), ("catalogueVersion", 1), ("rulesInputHash", Cna1979CombatAdjudication.ContentHash),
            ("dependencyManifestHash", Manifest(true)), ("moralePairs", 1296), ("assaultPairs", 6480), ("captureRefusalVariants", 8840), ("totalRecords", 16616));
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); hash.AppendData(Encoding.ASCII.GetBytes("sandtable.combat.actual-assault.catalogue.v1\0"));
        hash.AppendData(Canonical(header, "header")); hash.AppendData("\n"u8);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal) { ["morale"] = 0, ["assault-cell"] = 0, ["capture-refusal"] = 0 }; var ordinal = 0;
        foreach (var row in Catalogue())
        {
            Require(N(row, "ordinal") == ordinal++, 7); var kind = T(row, "kind"); counts[kind]++;
            var bytes = Canonical(row, kind == "morale" ? "MoraleRow" : kind == "assault-cell" ? "AssaultCell" : "VariantRow");
            Require(bytes.Length <= 4096, 7); hash.AppendData(bytes); hash.AppendData("\n"u8);
        }
        Require(ordinal == 16616 && counts["morale"] == 1296 && counts["assault-cell"] == 6480 && counts["capture-refusal"] == 8840, 7);
        return "sha256:" + Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
