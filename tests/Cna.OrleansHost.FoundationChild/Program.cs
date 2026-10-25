using System.Text.Json;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Microsoft.Extensions.Logging.Abstractions;

if (args is ["--native-reserve-crash-probe"])
{
    var crashInput = JsonSerializer.Deserialize<ReserveCrashInput>(Console.ReadLine() ?? "")
        ?? throw new InvalidOperationException("Missing Reserve crash input.");
    var clock = new ReserveCrashClock(crashInput.NowUnixMilliseconds, crashInput.StopAtRead);
    var journal = CampaignDurableAuthorityJournal.OpenExisting(crashInput.Path, clock);
    var before = journal.ReadVerified(crashInput.CampaignId);
    if (!CampaignExercises.Reconstruct(before).IsVerified)
        throw new InvalidOperationException("Initial Reserve replay failed.");
    clock.Arm();
    _ = journal.TickReserveRound(crashInput.CampaignId);
    if (crashInput.StopAfterCommit) ReserveCrashClock.Pause("after-commit", clock.ReadIndex, clock.LastObserved);
    var after = journal.ReadVerified(crashInput.CampaignId);
    var replay = CampaignExercises.Reconstruct(after);
    if (!replay.IsVerified) throw new InvalidOperationException("Final Reserve replay failed.");
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

internal sealed record ReserveCrashInput(string Path, string CampaignId, long NowUnixMilliseconds,
    int StopAtRead = 0, bool StopAfterCommit = false);

internal sealed class ReserveCrashClock(long now, int stopAtRead) : TimeProvider
{
    private bool armed;
    public int ReadIndex { get; private set; }
    public long LastObserved { get; private set; }
    public void Arm() { ReadIndex = 0; armed = true; }
    public override DateTimeOffset GetUtcNow()
    {
        if (!armed) return DateTimeOffset.FromUnixTimeMilliseconds(now);
        if (++ReadIndex > 32) throw new InvalidOperationException("Reserve crash clock exceeded bound.");
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
