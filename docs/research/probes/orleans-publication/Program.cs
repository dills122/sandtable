using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Storage;

namespace HostProbe;

public static class Program
{
    public static async Task Main()
    {
        var store = new ProbeStorage();
        using var host = new HostBuilder().ConfigureLogging(log => log.SetMinimumLevel(LogLevel.Error))
            .UseOrleans(silo => silo.UseLocalhostClustering(siloPort: 19111, gatewayPort: 0,
                serviceId: "host-rsh-001", clusterId: "host-rsh-001")
                .ConfigureServices(services => services.AddKeyedSingleton<IGrainStorage>("probe", store)))
            .Build();
        await host.StartAsync();
        try
        {
            await Run(host.Services.GetRequiredService<IGrainFactory>(), store);
        }
        finally { await host.StopAsync(); }
    }

    private static async Task Run(IGrainFactory factory, ProbeStorage store)
    {
        // Pinned public creation input from BreakdownEvidenceAdmissionTests; no internal catalog access.
        var request = new CampaignCreationRequest(1, "host-rsh-001", Cna1979Ruleset.Manifest.Hash,
            12345, "rules-lab.breakdown.truck.v1",
            "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3",
            "rules-lab.content.breakdown-truck.v1",
            "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee",
            "breakdown-truck-lab");
        Check(request.RulesetHash == "17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a", "pinned Rules9 source");
        var start = CampaignExercises.Begin(request);
        Check(start.IsStarted, "direct creation");
        var direct = start.Session!;
        var grain = factory.GetGrain<IProbeCampaign>(request.CampaignId);
        var creation = JsonSerializer.Serialize(request);
        var initial = await grain.Create(creation);
        Check(initial == await grain.Create(creation), "creation retry");
        Check(Envelope.Read(initial).CreationEvent == Hex(start.CreationEventBytes!), "creation event parity");
        Check(Envelope.Read(initial).InitialSnapshot == Hex(start.InitialSnapshotBytes!), "creation snapshot parity");
        await Reject<InvalidOperationException>(() => grain.Create(creation + " "), "changed creation bytes");
        var expected = new List<Entry>();
        var trace = new List<string>();
        for (var i = 0; i < 32; i++)
        {
            var sets = Enum.GetValues<CampaignActionAudience>()
                .Select(a => CampaignExercises.Query(direct, a).ActionSet!).ToArray();
            foreach (var set in sets)
                Check(Hex(CampaignLegalActionSerializer.Serialize(set)) == await grain.Query((int)set.Audience), "query parity");
            if (sets.All(s => s.Candidates.Count == 0)) break;
            var chosen = sets.First(s => s.Candidates.Count != 0);
            var candidate = chosen.Candidates.FirstOrDefault(c => c is ActFirstAction)
                ?? chosen.Candidates.FirstOrDefault(c => c is CompleteReserveDesignationAction)
                ?? chosen.Candidates.FirstOrDefault(c => c.Kind == "complete-movement-segment")
                ?? chosen.Candidates[0];
            var command = new CampaignActionSubmission(1, chosen.CampaignId, chosen.StateVersion,
                chosen.PositionId, chosen.Audience, candidate.ActionId);
            var bytes = Hex(CampaignActionSubmissionSerializer.SerializeCanonical(command));
            var id = "request-" + i.ToString(CultureInfo.InvariantCulture);
            var step = CampaignExercises.Submit(direct, command);
            Check(step.IsAccepted, "direct submit");
            var entry = Entry.From(id, (int)chosen.Audience, bytes, step.Evidence!);
            var before = await grain.Inspect();
            await Reject<InvalidOperationException>(() => grain.Submit(id, ((int)chosen.Audience + 1) % 3, bytes, false), "actor mismatch");
            if (i == 0)
            {
                store.NextFailure = WriteFailure.Before;
                await Reject<OrleansException>(() => grain.Submit(id, (int)chosen.Audience, bytes, false), "before-write failure");
                Check(before == await grain.Inspect(), "failed write publishes nothing");
            }
            if (i == 1)
            {
                store.NextFailure = WriteFailure.After;
                await Reject<OrleansException>(() => grain.Submit(id, (int)chosen.Audience, bytes, false), "ambiguous storage acknowledgment");
            }
            if (i == 2)
                await Reject<TimeoutException>(() => grain.Submit(id, (int)chosen.Audience, bytes, true), "lost application reply");
            var replies = await Task.WhenAll(grain.Submit(id, (int)chosen.Audience, bytes, false),
                grain.Submit(id, (int)chosen.Audience, bytes, false));
            Check(replies.All(reply => reply == entry.Receipt), "concurrent exact receipt retry");
            expected.Add(entry);
            trace.Add(candidate.Kind);
            direct = step.SuccessorSession!;
            var stored = Envelope.Read(await grain.Inspect());
            Check(stored.Entries.SequenceEqual(expected), "complete canonical trace parity");
            var stable = await grain.Inspect();
            await Reject<InvalidOperationException>(() => grain.Submit(id, (int)chosen.Audience,
                Hex(CampaignActionSubmissionSerializer.SerializeCanonical(command with { ExpectedStateVersion = command.ExpectedStateVersion + 1 })), false), "changed ID reuse");
            await Reject<InvalidOperationException>(() => grain.Submit(id + "-stale", (int)chosen.Audience, bytes, false), "stale fresh ID");
            Check(stable == await grain.Inspect(), "rejections publish nothing");
            var activation = await grain.Activation();
            await grain.Deactivate();
            Check(activation != await grain.Activation(), "fresh activation identity");
            Check(stable == await grain.Inspect(), "reactivation complete evidence parity");
            Check(entry.Receipt == await grain.Submit(id, (int)chosen.Audience, bytes, false), "retry after reactivation");
        }
        var final = CampaignExercises.QueryCheckpoint(direct);
        Check(final.PositionId.Contains("combat", StringComparison.Ordinal), "first-side Combat terminal");
        Check(CampaignExercises.Reconstruct(direct).IsVerified, "Core event replay");
        Check(store.Writes == expected.Count + 1, "one write per creation/accepted command");
        var record = await grain.Inspect();
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            status = "PASS",
            commands = expected.Count,
            writes = store.Writes,
            terminal = final.PositionId,
            final.StateVersion,
            rulesetHash = request.RulesetHash,
            request.SetupId,
            request.SetupHash,
            request.ContentPackId,
            request.ContentHash,
            request.ScenarioId,
            finalSnapshotHash = Hash(Convert.FromHexString(expected[^1].Snapshot)),
            recordHash = Hash(Encoding.UTF8.GetBytes(record)),
            recordBytes = Encoding.UTF8.GetByteCount(record),
            trace,
            limits = "one silo; injected memory CAS; no durable process restart"
        }));
    }

    public static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("CHECK FAILED: " + label);
    }
    private static async Task Reject<T>(Func<Task<string>> operation, string label) where T : Exception
    {
        try { await operation(); }
        catch (T error)
        {
            Check(error is not OrleansException || error.InnerException is IOException, "expected injected storage cause");
            return;
        }
        throw new InvalidOperationException("EXPECTED REJECTION: " + label);
    }
    public static string Hex(byte[] bytes) => Convert.ToHexStringLower(bytes);
    public static string Hash(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
}

public interface IProbeCampaign : IGrainWithStringKey
{
    Task<string> Create(string creation);
    Task<string> Query(int actor);
    Task<string> Submit(string requestId, int actor, string command, bool dropReply);
    Task<string> Inspect();
    Task<string> Activation();
    Task Deactivate();
}

public sealed record Entry(string RequestId, int Actor, string Command, string Event, string Snapshot, string Receipt)
{
    public static Entry From(string id, int actor, string command, ExerciseStepEvidence evidence) =>
        new(id, actor, command, Program.Hex(evidence.EventRecords.Single()),
            Program.Hex(evidence.SnapshotCheckpoint), Program.Hex(CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt)));
}

public sealed record Envelope(int Schema, string Creation, string CreationEvent, string InitialSnapshot, Entry[] Entries)
{
    public static Envelope Read(string text) => JsonSerializer.Deserialize<Envelope>(text)
        ?? throw new InvalidOperationException("Missing record");
    public string Encode() => JsonSerializer.Serialize(this);
}

[GenerateSerializer]
public sealed class StoredRecord
{
    [Id(0)] public string Bytes { get; set; } = "";
}

public sealed class ProbeCampaign([PersistentState("campaign", "probe")] IPersistentState<StoredRecord> state)
    : Grain, IProbeCampaign
{
    private readonly string activation = Guid.NewGuid().ToString("N");
    private ExerciseSession? session;
    private Envelope? envelope;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Restore();
        return Task.CompletedTask;
    }
    public async Task<string> Create(string creation)
    {
        if (envelope is not null)
        {
            Program.Check(envelope.Creation == creation, "creation identity conflict");
            return envelope.Encode();
        }
        var request = JsonSerializer.Deserialize<CampaignCreationRequest>(creation)!;
        Program.Check(request.CampaignId == this.GetPrimaryKeyString(), "campaign key binding");
        var start = CampaignExercises.Begin(request);
        Program.Check(start.IsStarted, "host creation");
        var next = new Envelope(1, creation, Program.Hex(start.CreationEventBytes!), Program.Hex(start.InitialSnapshotBytes!), []);
        await Publish(next, start.Session!);
        return next.Encode();
    }
    public async Task<string> Submit(string requestId, int actor, string command, bool dropReply)
    {
        Program.Check(envelope is not null && session is not null, "created authority required");
        Program.Check(requestId.Length is > 0 and <= 80, "bounded request identity");
        var submission = CampaignActionSubmissionSerializer.DeserializeCanonical(Convert.FromHexString(command));
        Program.Check(Enum.IsDefined((CampaignActionAudience)actor) && (int)submission.Audience == actor,
            "authenticated actor must match submission");
        var prior = envelope!.Entries.SingleOrDefault(entry => entry.RequestId == requestId);
        if (prior is not null)
        {
            Program.Check(prior.Actor == actor && prior.Command == command, "request identity conflict");
            return prior.Receipt;
        }
        Program.Check(envelope.Entries.Length < 32, "bounded experiment history");
        var result = CampaignExercises.Submit(session!, submission);
        Program.Check(result.IsAccepted, "Core rejection: " + result.RejectionReason);
        var accepted = Entry.From(requestId, actor, command, result.Evidence!);
        await Publish(envelope with { Entries = [.. envelope.Entries, accepted] }, result.SuccessorSession!);
        if (dropReply) throw new TimeoutException("Injected lost application reply after publication");
        return accepted.Receipt;
    }
    public Task<string> Query(int actor) => Task.FromResult(Program.Hex(CampaignLegalActionSerializer.Serialize(
        CampaignExercises.Query(session ?? throw new InvalidOperationException("No session"), (CampaignActionAudience)actor).ActionSet!)));
    public Task<string> Inspect() => Task.FromResult(envelope?.Encode() ?? "");
    public Task<string> Activation() => Task.FromResult(activation);
    public Task Deactivate() { DeactivateOnIdle(); return Task.CompletedTask; }

    private async Task Publish(Envelope next, ExerciseSession successor)
    {
        state.State = new StoredRecord { Bytes = next.Encode() };
        try { await state.WriteStateAsync(); }
        catch
        {
            envelope = null;
            session = null;
            try { await state.ReadStateAsync(); Restore(); }
            catch { DeactivateOnIdle(); throw; }
            throw;
        }
        envelope = next;
        session = successor;
    }
    private void Restore()
    {
        if (state.State.Bytes.Length == 0) return;
        var record = Envelope.Read(state.State.Bytes);
        Program.Check(record.Schema == 1 && record.Entries.Length <= 32
            && record.Entries.Select(entry => entry.RequestId).Distinct(StringComparer.Ordinal).Count() == record.Entries.Length,
            "record shape and unique command identities");
        var request = JsonSerializer.Deserialize<CampaignCreationRequest>(record.Creation)!;
        Program.Check(request.CampaignId == this.GetPrimaryKeyString(), "restore campaign key");
        var start = CampaignExercises.Begin(request);
        Program.Check(start.IsStarted && Program.Hex(start.CreationEventBytes!) == record.CreationEvent
            && Program.Hex(start.InitialSnapshotBytes!) == record.InitialSnapshot, "restore creation evidence");
        var restored = start.Session!;
        foreach (var entry in record.Entries)
        {
            var command = CampaignActionSubmissionSerializer.DeserializeCanonical(Convert.FromHexString(entry.Command));
            Program.Check((int)command.Audience == entry.Actor, "restore actor binding");
            var result = CampaignExercises.Submit(restored, command);
            Program.Check(result.IsAccepted && Entry.From(entry.RequestId, entry.Actor, entry.Command, result.Evidence!) == entry, "restore entry evidence");
            restored = result.SuccessorSession!;
        }
        envelope = record;
        session = restored;
    }
}

public enum WriteFailure { None, Before, After }

// This provider deliberately models atomic compare-and-swap, not a durable database.
public sealed class ProbeStorage : IGrainStorage
{
    private readonly Dictionary<(string, GrainId), (string Bytes, string Etag)> records = [];
    public WriteFailure NextFailure { get; set; }
    public int Writes { get; private set; }
    public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        lock (records)
        {
            if (records.TryGetValue((stateName, grainId), out var stored))
            {
                grainState.State = JsonSerializer.Deserialize<T>(stored.Bytes)!;
                grainState.ETag = stored.Etag;
                grainState.RecordExists = true;
            }
            else { grainState.State = Activator.CreateInstance<T>(); grainState.ETag = null; grainState.RecordExists = false; }
        }
        return Task.CompletedTask;
    }
    public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        lock (records)
        {
            var failure = NextFailure;
            NextFailure = WriteFailure.None;
            if (failure == WriteFailure.Before) throw new IOException("Injected before-write failure");
            records.TryGetValue((stateName, grainId), out var prior);
            if (prior.Etag != grainState.ETag) throw new InconsistentStateException("Injected store CAS mismatch", prior.Etag, grainState.ETag);
            var etag = (++Writes).ToString(CultureInfo.InvariantCulture);
            records[(stateName, grainId)] = (JsonSerializer.Serialize(grainState.State), etag);
            if (failure == WriteFailure.After) throw new IOException("Injected error after committed write");
            grainState.ETag = etag;
            grainState.RecordExists = true;
        }
        return Task.CompletedTask;
    }
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => throw new NotSupportedException();
}
