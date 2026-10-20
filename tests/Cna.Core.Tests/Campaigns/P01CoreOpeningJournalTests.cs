using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class P01CoreOpeningJournalTests
{
    private const string Fixture = "docs/specs/fixtures/p01-r03-a1-r012-c01-import-v1.json.gz";
    private const string FixtureHash = "5019978589938979d7010416f6e3424e07615532475f5633a60db66414db14aa";
    private const string ZeroRecordDigest = "fe42392dceb566faf641e7e5de0b32f482c3def94f840daceb719ee9f28a6f6c";
    private const string OneRecordDigest = "ce7795a0993e5bb0fb35793a375cecc5dd8e9d8cada97d62fd66237b825d763a";

    [Fact]
    public void PinnedCreationIssuesOneSystemInitiativeAndReplaysIdentically()
    {
        var import = AuthenticImport();
        var request = CampaignCombatCreationRequestCodec.Deserialize(import.CreationRequest, Context());
        var initial = P01CoreOpeningJournalV1.Create(request);
        Assert.Equal(ZeroRecordDigest, Digest(initial));
        var initialState = P01CoreOpeningJournalV1.Read(initial, request);
        Assert.Equal(1, initialState.StateVersion);
        Assert.Empty(initialState.Events);

        var advanced = P01CoreOpeningJournalV1.AppendSystemInitiative(initial, request);
        Assert.Equal(OneRecordDigest, Digest(advanced));
        Assert.Equal(advanced, P01CoreOpeningJournalV1.AppendSystemInitiative(advanced, request));
        var replayed = P01CoreOpeningJournalV1.Read(advanced, request);
        Assert.Equal(2, replayed.StateVersion);
        Assert.Single(replayed.Events);
        Assert.Single(replayed.Receipts);
        using var document = JsonDocument.Parse(advanced);
        var root = document.RootElement;
        Assert.False(root.GetProperty("admitted").GetBoolean());
        var entry = root.GetProperty("records")[0];
        Assert.Equal(import.Predecessors[0][0], Encoding.ASCII.GetBytes(entry.GetProperty("inputCanonicalUtf8").GetString()!));
        using var authenticSource = JsonDocument.Parse(AuthenticAncestorSource(import));
        Assert.Equal(authenticSource.RootElement.GetProperty("createdCanonicalUtf8").GetString(),
            root.GetProperty("createdEventCanonicalUtf8").GetString());
        Assert.Equal(authenticSource.RootElement.GetProperty("prefixEventCanonicalUtf8")[0].GetString(),
            entry.GetProperty("eventCanonicalUtf8").GetString());
        Assert.Equal(CampaignOpeningPreambleCodec.SerializeEvent(replayed.Events[0]),
            Encoding.ASCII.GetBytes(entry.GetProperty("eventCanonicalUtf8").GetString()!));
        Assert.Equal(replayed.Receipts[0].ReceiptId, entry.GetProperty("receiptId").GetString());
        Assert.Equal(initialState.Prefix, entry.GetProperty("priorPrefix").GetString());
        Assert.Equal(replayed.Prefix, entry.GetProperty("afterPrefix").GetString());
        using var eventDocument = JsonDocument.Parse(CampaignOpeningPreambleCodec.SerializeEvent(replayed.Events[0]));
        Assert.Equal(eventDocument.RootElement.GetProperty("sources").GetRawText(),
            entry.GetProperty("sourceReferencesCanonicalUtf8").GetString());
    }

    [Fact]
    public void RecoveredBytesRejectForeignRequestAndTamperedActorClockSourceOrReceipt()
    {
        var import = AuthenticImport();
        var request = CampaignCombatCreationRequestCodec.Deserialize(import.CreationRequest, Context());
        var initial = P01CoreOpeningJournalV1.Create(request);
        var advanced = P01CoreOpeningJournalV1.AppendSystemInitiative(initial, request);
        var changedInitial = initial.ToArray();
        changedInitial[^2] = (byte)'x';
        Assert.NotEqual(ZeroRecordDigest, Digest(changedInitial));
        Assert.ThrowsAny<JsonException>(() => P01CoreOpeningJournalV1.Read(changedInitial, request));
        var changedRequest = CampaignCombatCreationRequest.Create(request.CampaignId, 2, request.Context);
        Assert.Throws<JsonException>(() => P01CoreOpeningJournalV1.Read(initial, changedRequest));
        Assert.Throws<JsonException>(() => P01CoreOpeningJournalV1.AppendSystemInitiative(advanced, changedRequest));
        Assert.Throws<JsonException>(() => P01CoreOpeningJournalV1.Read([.. advanced, (byte)' '], request));
        Assert.Throws<JsonException>(() => P01CoreOpeningJournalV1.Read(new byte[1_100_001], request));

        var journal = Encoding.UTF8.GetString(advanced);
        foreach (var mutation in new[]
        {
            journal.Replace("system", "axis", StringComparison.Ordinal),
            journal.Replace("expectedPriorVersion\\\":1", "expectedPriorVersion\\\":2", StringComparison.Ordinal),
            journal.Replace("7.12", "7.13", StringComparison.Ordinal),
            journal.Replace("pre.", "bad.", StringComparison.Ordinal),
            journal.Replace("campaign-created", "campaign-changed", StringComparison.Ordinal),
            journal.Replace("\"sequence\":1", "\"sequence\":2", StringComparison.Ordinal),
            journal.Replace("\"admitted\":false", "\"admitted\":true", StringComparison.Ordinal),
        })
        {
            Assert.NotEqual(journal, mutation);
            var changed = Encoding.UTF8.GetBytes(mutation);
            Assert.NotEqual(OneRecordDigest, Digest(changed));
            Assert.Throws<JsonException>(() => P01CoreOpeningJournalV1.Read(changed, request));
        }
    }

    private static P01R03ImportV1 AuthenticImport()
    {
        var gzip = File.ReadAllBytes(Path.Combine(Repository(), Fixture));
        Assert.Equal(FixtureHash, Convert.ToHexStringLower(SHA256.HashData(gzip)));
        using var compressed = new MemoryStream(gzip);
        using var decompressor = new GZipStream(compressed, CompressionMode.Decompress);
        using var output = new MemoryStream();
        decompressor.CopyTo(output);
        return P01R03ImportV1.Deserialize(output.ToArray());
    }

    private static byte[] AuthenticAncestorSource(P01R03ImportV1 import)
    {
        var source = import.R03Source;
        foreach (var field in new[] { "movementCompletionSourceCanonicalUtf8", "movementSourceCanonicalUtf8",
            "controlSourceCanonicalUtf8", "continuationSourceCanonicalUtf8", "movedAssaultSourceCanonicalUtf8",
            "reactionEntrySourceCanonicalUtf8" })
        {
            using var document = JsonDocument.Parse(source);
            source = Encoding.ASCII.GetBytes(document.RootElement.GetProperty(field).GetString()!);
        }
        return source;
    }

    private static string Digest(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
