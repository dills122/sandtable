using System.Diagnostics;
using System.Globalization;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Persistence;
using Cna.OrleansHost.Tests.Persistence;
using JasperFx.Events;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    /// <summary>
    /// The Marten-specific per-call cost C3b introduced (fetch the whole stream, copy it into the
    /// in-memory working set, flush one appended step), isolated from journal/Core replay, as a
    /// function of stream length. Streams are built from real accepted-step payloads (a combat
    /// lab step and a Graziani pregame-source step) repeated N times with consistent sequence
    /// headers; the working-set loader asserts only sequence/version shape, so this measures storage
    /// alone -- the replay half of each call is measured separately on real histories.
    /// </summary>
    [Fact]
    public async Task CapacityBenchmarkStreamLengthStorageCost()
    {
        if (!CapacityBenchmarkEnabled) return;
        using var database = MartenJournalDatabase.Create(OperatorSecret);
        var store = new CampaignDurableAuthorityJournalStore(database.Backend);

        // Real payload 1: the last accepted step of a combat lab campaign at its Selection cut.
        await SetupSelectionCampaignAsync(store, "payload-combat", withTiming: true);
        // Real payload 2: a Graziani pregame-source step (the campaign family with the large
        // per-step checkpoints the orleans-slow soak exists for).
        var content = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion, "payload-graziani",
            Cna1979Ruleset.Manifest.Hash, 0, CampaignPregameSourceCreation.SetupId,
            CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, init),
            content.Identity.PackId, content.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        await store.CreatePregameSourceCampaignAsync(request, content, init);
        var axis = (await store.RedeemSeatGrantAsync(
            await store.IssueSeatGrantAsync(request.CampaignId, CampaignActionAudience.Axis, OperatorSecret)))!;
        for (var step = 0; step < 2; step++)
        {
            var (roll, rollTiming) = await TimedUnitOfWork(database.Backend, request.CampaignId,
                journal => journal.QueryPregameAxisSupplyOpening(axis));
            var (accepted, submitTiming) = await TimedUnitOfWork(database.Backend, request.CampaignId,
                journal => journal.SubmitPregameAxisSupply(axis, roll.Revision, roll.PublicActionRefs[0]));
            Assert.True(accepted.IsAccepted);
            BenchmarkRecord(string.Create(CultureInfo.InvariantCulture,
                $"{{\"kind\":\"graziani-real\",\"steps\":{step + 1},\"queryOpen\":{rollTiming.OpenMs:F1},\"queryBody\":{rollTiming.BodyMs:F1},\"queryFlush\":{rollTiming.FlushMs:F1},\"submitOpen\":{submitTiming.OpenMs:F1},\"submitBody\":{submitTiming.BodyMs:F1},\"submitFlush\":{submitTiming.FlushMs:F1},\"bytes\":{StreamBytes(database.Backend, request.CampaignId)}}}"));
        }

        foreach (var (label, sourceId, lengths) in new[]
        {
            ("combat", "payload-combat", new[] { 10, 50, 100, 250, 500, 1000 }),
            // The combat step with its checkpoint replaced by ~961KB of incompressible bytes: the
            // per-step average the orleans-slow soak's comment measured for late Graziani pregame
            // history (early pregame steps, recorded above, are only ~6KB).
            ("checkpoint-961kb", "payload-combat", new[] { 10, 50, 100, 150 }),
        })
        {
            IReadOnlyList<IEvent> source;
            await using (var query = database.Store.QuerySession())
                source = await query.Events.FetchStreamAsync(sourceId, token: TestContext.Current.CancellationToken);
            var genesis = source[0].Data;
            var step = (CampaignAcceptedStepEvent)source[^1].Data;
            if (label == "checkpoint-961kb")
                // Padded on the contacts blob, which no inline projection parses (the checkpoint
                // itself is contract-validated by the C2 state projection); storage cost depends
                // on bytes per event, not on which column carries them.
                step = step with { CanonicalContacts = new DeterministicRandom(961).Next(961 * 1024) };
            var stepBytes = step.CanonicalCommand.Length + step.CanonicalEvent.Length + step.CanonicalReceipt.Length
                + step.SnapshotCheckpoint.Length + step.CanonicalContacts.Length;
            foreach (var length in lengths)
            {
                var streamId = $"synthetic-{label}-{length}";
                await using (var session = database.Store.LightweightSession())
                {
                    session.Events.StartStream(streamId, CampaignEventStreamSchema.BuildStepEvent(database.Store, genesis,
                        0, CampaignGenesisEvent.PriorHashSentinel, "h0"));
                    await session.SaveChangesAsync(TestContext.Current.CancellationToken);
                }
                for (var start = 1; start <= length; start += 25)
                {
                    await using var session = database.Store.LightweightSession();
                    var batch = Enumerable.Range(start, Math.Min(25, length - start + 1))
                        .Select(sequence => CampaignEventStreamSchema.BuildStepEvent(database.Store, step, sequence,
                            "h" + (sequence - 1).ToString(CultureInfo.InvariantCulture),
                            "h" + sequence.ToString(CultureInfo.InvariantCulture)))
                        .ToArray();
                    session.Events.Append(streamId, start + batch.Length, batch);
                    await session.SaveChangesAsync(TestContext.Current.CancellationToken);
                }
                for (var round = 0; round < 3; round++)
                {
                    var watch = Stopwatch.StartNew();
                    using var workingSet = await database.Backend.OpenAsync([streamId],
                        () => database.Backend.RandomBytes(32), CancellationToken.None);
                    var openMs = watch.Elapsed.TotalMilliseconds;
                    var head = length + round;
                    using (var connection = workingSet.Connect())
                    {
                        using var copy = connection.CreateCommand();
                        copy.CommandText = """
                            INSERT INTO accepted_steps SELECT campaign_id,$next,action_kind,acting_element_id,
                              owner_public_ref,seat_epoch,owner_revision,owner_side,canonical_command,canonical_event,
                              canonical_receipt,snapshot_checkpoint,canonical_contacts,$prior,$hash
                            FROM accepted_steps WHERE campaign_id=$id AND sequence=$head;
                            UPDATE campaigns SET head_sequence=$next, head_hash=$hash WHERE campaign_id=$id;
                            """;
                        copy.Parameters.AddWithValue("$id", streamId);
                        copy.Parameters.AddWithValue("$head", head);
                        copy.Parameters.AddWithValue("$next", head + 1);
                        copy.Parameters.AddWithValue("$prior", "h" + head.ToString(CultureInfo.InvariantCulture));
                        copy.Parameters.AddWithValue("$hash", "h" + (head + 1).ToString(CultureInfo.InvariantCulture));
                        copy.ExecuteNonQuery();
                    }
                    var flushStart = watch.Elapsed.TotalMilliseconds;
                    await workingSet.FlushAsync(CancellationToken.None);
                    var flushMs = watch.Elapsed.TotalMilliseconds - flushStart;
                    BenchmarkRecord(string.Create(CultureInfo.InvariantCulture,
                        $"{{\"kind\":\"stream-length\",\"label\":\"{label}\",\"events\":{length},\"round\":{round},\"stepBytes\":{stepBytes},\"streamMB\":{(double)stepBytes * length / 1_048_576:F1},\"openMs\":{openMs:F1},\"flushMs\":{flushMs:F1}}}"));
                }
            }
        }
    }
}
