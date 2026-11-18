using System.Text.Json;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Microsoft.Extensions.Logging.Abstractions;

if (args is ["--source-authority-read"])
{
    var sourceAuthorityInput = JsonSerializer.Deserialize<SourceCampaignInput>(Console.ReadLine() ?? "")
        ?? throw new InvalidOperationException("Missing source campaign input.");
    var journal = CampaignDurableAuthorityJournal.OpenExisting(sourceAuthorityInput.Path);
    var session = journal.ReadVerified(sourceAuthorityInput.CampaignId);
    var checkpoint = CampaignExercises.QueryCheckpoint(session);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        ProcessId = Environment.ProcessId,
        checkpoint.CampaignId,
        checkpoint.StateVersion,
        checkpoint.PositionId,
        VisibleBase64 = ""
    }));
    return;
}

if (args is ["--pregame-axis-read"])
{
    var pregameInput = JsonSerializer.Deserialize<SourceCampaignInput>(Console.ReadLine() ?? "")
        ?? throw new InvalidOperationException("Missing pregame campaign input.");
    var journal = CampaignDurableAuthorityJournal.OpenExisting(pregameInput.Path);
    var session = journal.ReadVerified(pregameInput.CampaignId);
    var checkpoint = CampaignExercises.QueryCheckpoint(session);
    var draft = CampaignExercises.QueryPregameAxisDraft(session, Cna.Core.Actions.CampaignActionAudience.Axis);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        ProcessId = Environment.ProcessId,
        checkpoint.CampaignId,
        checkpoint.StateVersion,
        checkpoint.PositionId,
        VisibleBase64 = Convert.ToBase64String(draft.VisibleBytes)
    }));
    return;
}

if (args is ["--source-campaign-read"])
{
    var sourceInput = JsonSerializer.Deserialize<SourceCampaignInput>(Console.ReadLine() ?? "")
        ?? throw new InvalidOperationException("Missing source campaign input.");
    var journal = CampaignDurableAuthorityJournal.OpenExisting(sourceInput.Path);
    var source = journal.ReadSourceVerified(sourceInput.CampaignId);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        ProcessId = Environment.ProcessId,
        source.Request.CampaignId,
        source.Request.ContentHash,
        PendingGroupIds = source.PendingArrivalGroupIds
    }));
    return;
}

if (args is ["--scheduled-arrival-retry"])
{
    var scheduledInput = JsonSerializer.Deserialize<ScheduledArrivalInput>(Console.ReadLine() ?? "")
        ?? throw new InvalidOperationException("Missing scheduled arrival input.");
    var journal = CampaignDurableAuthorityJournal.OpenExisting(scheduledInput.Path);
    var offers = journal.QueryScheduledWorld6(scheduledInput.Credential);
    var receipt = journal.SubmitScheduledWorld6(scheduledInput.Credential, scheduledInput.Reference);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        ProcessId = Environment.ProcessId,
        OfferCount = offers.Count,
        Receipt = receipt is null ? null : Convert.ToBase64String(receipt)
    }));
    return;
}

if (args is ["--source-owner-crash-probe"])
{
    var sourceCrashInput = JsonSerializer.Deserialize<SourceOwnerCrashInput>(Console.ReadLine() ?? "")
        ?? throw new InvalidOperationException("Missing source-owner crash input.");
    var clock = new NativeCrashClock(sourceCrashInput.NowUnixMilliseconds, sourceCrashInput.AfterCommit ? 0 : 1);
    var journal = CampaignDurableAuthorityJournal.OpenExisting(sourceCrashInput.Path, clock);
    _ = journal.ReadVerified(sourceCrashInput.Credential.CampaignId);
    clock.Arm();
    var sourceCrashResult = journal.SubmitSourceActions(sourceCrashInput.Credential, sourceCrashInput.Revision, sourceCrashInput.Reference);
    if (!sourceCrashResult.IsAccepted) throw new InvalidOperationException("Source owner action was rejected.");
    if (sourceCrashInput.AfterCommit) NativeCrashClock.Pause("source-after-commit", clock.ReadIndex, clock.LastObserved);
    throw new InvalidOperationException("Source crash probe was not terminated by parent.");
}

if (args is ["--native-reserve-crash-probe"] or ["--native-custody-crash-probe"] or ["--native-retreat-crash-probe"])
{
    var crashInput = JsonSerializer.Deserialize<NativeCrashInput>(Console.ReadLine() ?? "")
        ?? throw new InvalidOperationException("Missing native crash input.");
    var clock = new NativeCrashClock(crashInput.NowUnixMilliseconds, crashInput.StopAtRead);
    var journal = CampaignDurableAuthorityJournal.OpenExisting(crashInput.Path, clock);
    var before = journal.ReadVerified(crashInput.CampaignId);
    if (!CampaignExercises.Reconstruct(before).IsVerified)
        throw new InvalidOperationException("Initial crash-probe replay failed.");
    clock.Arm();
    if (args[0] == "--native-custody-crash-probe")
        _ = journal.TickCustodyRound(crashInput.CampaignId);
    else if (args[0] == "--native-retreat-crash-probe")
        _ = journal.TickRetreatRound(crashInput.CampaignId);
    else
        _ = journal.TickReserveRound(crashInput.CampaignId);
    if (crashInput.StopAfterCommit) NativeCrashClock.Pause("after-commit", clock.ReadIndex, clock.LastObserved);
    var after = journal.ReadVerified(crashInput.CampaignId);
    var replay = CampaignExercises.Reconstruct(after);
    if (!replay.IsVerified) throw new InvalidOperationException("Final crash-probe replay failed.");
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        ProcessId = Environment.ProcessId,
        BeforeStateVersion = CampaignExercises.QueryCheckpoint(before).StateVersion,
        AfterStateVersion = CampaignExercises.QueryCheckpoint(after).StateVersion,
        ContractVersion = CampaignExercises.QueryCheckpoint(after).ContractVersion,
        replay.IsVerified,
        replay.EventStreamHash,
        replay.ReconstructedSnapshotHash
    }));
    return;
}

if (args is ["--native-journal-create-crash-probe"])
{
    var createCrashInput = JsonSerializer.Deserialize<JournalCreateCrashInput>(Console.ReadLine() ?? "")
        ?? throw new InvalidOperationException("Missing journal create crash input.");
    CampaignDurableAuthorityJournal.CreateNewCrashHook = phase =>
    {
        if (phase != createCrashInput.StopAtCheckpoint) return;
        Console.WriteLine(JsonSerializer.Serialize(new { Phase = phase, ProcessId = Environment.ProcessId }));
        Console.Out.Flush();
        // Parent kills this process while stdin remains open. Never release the half-finished
        // temp file via managed exception/disposal as a substitute for a real crash.
        _ = Console.ReadLine();
        throw new InvalidOperationException("Journal create crash probe was not terminated by parent.");
    };
    CampaignDurableAuthorityJournal.CreateNew(createCrashInput.Path, createCrashInput.OperatorSecret);
    Console.WriteLine(JsonSerializer.Serialize(new { ProcessId = Environment.ProcessId, Completed = true }));
    return;
}

if (args is ["--native-campaign-deadline"])
{
    var native = JsonSerializer.Deserialize<NativeDeadlineInput>(Console.ReadLine() ?? "")
        ?? throw new InvalidOperationException("Missing native deadline input.");
    var clock = new NativeFixedClock(native.NowUnixMilliseconds);
    var journal = CampaignDurableAuthorityJournal.OpenExisting(native.Path, clock);
    var before = journal.ReadVerified(native.CampaignId);
    var beforeReplay = CampaignExercises.Reconstruct(before);
    if (!beforeReplay.IsVerified) throw new InvalidOperationException("Child initial replay failed.");
    new CampaignCombatTimeoutService(journal, clock,
        NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None);
    var after = journal.ReadVerified(native.CampaignId);
    var replay = CampaignExercises.Reconstruct(after);
    if (!replay.IsVerified) throw new InvalidOperationException("Child final replay failed.");
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        ProcessId = Environment.ProcessId,
        BeforeStateVersion = CampaignExercises.QueryCheckpoint(before).StateVersion,
        AfterStateVersion = CampaignExercises.QueryCheckpoint(after).StateVersion,
        ContractVersion = CampaignExercises.QueryCheckpoint(after).ContractVersion,
        replay.IsVerified,
        replay.EventStreamHash,
        replay.ReconstructedSnapshotHash
    }));
    return;
}
if (args.Length != 0) throw new ArgumentException("Unknown child mode.");

var input = JsonSerializer.Deserialize<ProbeReadbackInput>(Console.ReadLine() ?? "")
    ?? throw new InvalidOperationException("Missing child readback input.");
var store = P01PrivateHostFoundation.OpenExisting(input.Path);
var credential = new P01PrivateCredential(input.CampaignId, input.Side,
    input.AuthorityId, input.CredentialSecret, input.SeatEpoch, input.CredentialExpiryMs);
var lease = new P01ActivationLease(input.CampaignId, input.Fence,
    input.LeaseSecret, input.OperatorSecret);
var result = store.FindProbe(credential, input.RequestId,
    Convert.FromHexString(input.ExactBytesHex), lease);
Console.WriteLine(result is null ? "missing" : $"{result.Sequence}|{result.RecordHash}");

internal sealed record ProbeReadbackInput(string Path, string CampaignId, string Side,
    string AuthorityId, string CredentialSecret, long SeatEpoch, long CredentialExpiryMs,
    long Fence, string LeaseSecret, string OperatorSecret, string RequestId,
    string ExactBytesHex);

internal sealed record NativeDeadlineInput(string Path, string CampaignId, long NowUnixMilliseconds);

internal sealed record JournalCreateCrashInput(string Path, string OperatorSecret, string StopAtCheckpoint);

internal sealed record ScheduledArrivalInput(string Path, CampaignSeatCredential Credential, string Reference);

internal sealed record SourceCampaignInput(string Path, string CampaignId);

internal sealed record SourceOwnerCrashInput(string Path, CampaignSeatCredential Credential, long Revision,
    string Reference, long NowUnixMilliseconds, bool AfterCommit);

internal sealed record NativeCrashInput(string Path, string CampaignId, long NowUnixMilliseconds,
    int StopAtRead = 0, bool StopAfterCommit = false);

internal sealed class NativeCrashClock(long now, int stopAtRead) : TimeProvider
{
    private bool armed;
    public int ReadIndex { get; private set; }
    public long LastObserved { get; private set; }
    public void Arm() { ReadIndex = 0; armed = true; }
    public override DateTimeOffset GetUtcNow()
    {
        if (!armed) return DateTimeOffset.FromUnixTimeMilliseconds(now);
        if (++ReadIndex > 32) throw new InvalidOperationException("native crash clock exceeded bound.");
        LastObserved = checked(now + ReadIndex - 1);
        if (ReadIndex == stopAtRead) Pause("clock-read", ReadIndex, LastObserved);
        return DateTimeOffset.FromUnixTimeMilliseconds(LastObserved);
    }
    public static void Pause(string phase, int readIndex, long observed)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { Phase = phase, ReadIndex = readIndex, Observed = observed }));
        Console.Out.Flush();
        // Parent kills this process while stdin remains open. Never release the
        // transaction via managed exception/disposal as a substitute for a crash.
        _ = Console.ReadLine();
        throw new InvalidOperationException("Crash probe must be terminated by parent.");
    }
}

internal sealed class NativeFixedClock(long nowUnixMilliseconds) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeMilliseconds(nowUnixMilliseconds);
}
