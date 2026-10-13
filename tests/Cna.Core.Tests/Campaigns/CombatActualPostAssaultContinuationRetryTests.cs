using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;
using Original = Cna.Core.Tests.Campaigns.CombatActualPostAssaultContinuationTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualPostAssaultContinuationRetryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void EveryOlderCommandRetriesAfterLaterAndTerminalCutsWithValidClockChanges(int index)
    {
        var trace = Original.Trace(index); var args = Original.Arguments(trace); var full = JsonNode.Parse(Original.Bytes(trace.GetProperty("sourceCanonicalUtf8")))!.AsObject(); var all = Ledger(trace, "continuationLedger");
        for (var cut = 1; cut <= 4; cut++)
        {
            var p = full.DeepClone().AsObject(); var events = p["continuationEventCanonicalUtf8"]!.AsArray(); while (events.Count > cut) events.RemoveAt(events.Count - 1); var source = CampaignCombatActualPostAssaultContinuationCodec.Encode(p); var ledger = all.Take(cut).ToArray(); var before = Original.Replay(source, args, ledger);
            for (var prior = 0; prior < cut; prior++)
            {
                using var accepted = JsonDocument.Parse(events[prior]!.GetValue<string>()); var receipt = accepted.RootElement.GetProperty("receiptId").GetString();
                foreach (var available in new[] { true, false })
                {
                    var retry = JsonNode.Parse(all[prior])!.AsObject(); if (prior >= 2) { retry["admittedAt"] = available ? 5000 : null; retry["clockAvailable"] = available; }
                    var result = CampaignCombatActualPostAssaultContinuation.ApplyTrustedSource(source, Context(), args.Selection, args.Rounds, args.Paid, ledger, CampaignCombatActualPostAssaultContinuationCodec.Encode(retry), Dependency);
                    Assert.True(result.Duplicate); Assert.Null(result.EventBytes); Assert.Equal(receipt, result.ReceiptId); Assert.Equal(source, result.SourceBytes); Assert.Equal(before.ControlBytes, result.ControlBytes); Assert.Equal(before.ProofBytes, result.ProofBytes); Assert.Equal(ledger.Length, result.ContinuationInputs.Count); for (var j = 0; j < ledger.Length; j++) Assert.Equal(ledger[j], result.ContinuationInputs[j]);
                }
            }
        }
    }
}
