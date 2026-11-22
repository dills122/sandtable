using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.OrleansHost;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;
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

// Same genuine-crash mechanism as --source-owner-crash-probe (NativeCrashClock pauses and the
// parent kills this process at that exact point -- never a managed exception/disposal standing
// in for a real crash), generalized to the two newest owner openings this release added
// (CompleteMovementSegmentAction/StopElementMovementAction, which share one submit method --
// Core's own dispatch, not the caller, decides which of the two a given accepted publicActionRef
// resolves to). AfterCommit=true pauses right after the submit call returns (durably committed,
// before any response reaches a real caller) -- this is the meaningful case for a one-shot
// decision with no deadline/timeout round of its own (so, unlike Source/RBA/Force/Retreat,
// there is no mid-transaction clock read to pause on with AfterCommit=false).
if (args is ["--movement-owner-crash-probe"])
{
    var movementCrashInput = JsonSerializer.Deserialize<SourceOwnerCrashInput>(Console.ReadLine() ?? "")
        ?? throw new InvalidOperationException("Missing movement-owner crash input.");
    var clock = new NativeCrashClock(movementCrashInput.NowUnixMilliseconds, movementCrashInput.AfterCommit ? 0 : 1);
    var journal = CampaignDurableAuthorityJournal.OpenExisting(movementCrashInput.Path, clock);
    _ = journal.ReadVerified(movementCrashInput.Credential.CampaignId);
    clock.Arm();
    var movementCrashResult = journal.SubmitOwnerMovementSegmentCompletion(
        movementCrashInput.Credential, movementCrashInput.Revision, movementCrashInput.Reference);
    if (!movementCrashResult.IsAccepted) throw new InvalidOperationException("Movement owner action was rejected.");
    if (movementCrashInput.AfterCommit) NativeCrashClock.Pause("movement-after-commit", clock.ReadIndex, clock.LastObserved);
    throw new InvalidOperationException("Movement crash probe was not terminated by parent.");
}

// Companion read-side probe for the other half of the same ask: a cold restart right after the
// System-audience Breakdown auto-drain fires (triggered, by design, as a side effect of the next
// real owner-opening query -- see CampaignDurableAuthorityJournal.Openings.cs's own C3b remarks),
// but before any real player request follows it. Queries the Combat Selection opening (the query
// that performs the drain) and pauses/crashes right after that query call returns, durably
// committed, before any response reaches a real caller.
if (args is ["--post-drain-owner-crash-probe"])
{
    var drainCrashInput = JsonSerializer.Deserialize<PostDrainCrashInput>(Console.ReadLine() ?? "")
        ?? throw new InvalidOperationException("Missing post-drain crash input.");
    var clock = new NativeCrashClock(drainCrashInput.NowUnixMilliseconds, 0);
    var journal = CampaignDurableAuthorityJournal.OpenExisting(drainCrashInput.Path, clock);
    _ = journal.ReadVerified(drainCrashInput.Credential.CampaignId);
    clock.Arm();
    _ = journal.QueryCombatOwnerOpening(drainCrashInput.Credential, drainCrashInput.ActingElementId);
    NativeCrashClock.Pause("post-drain-after-query", clock.ReadIndex, clock.LastObserved);
    throw new InvalidOperationException("Post-drain crash probe was not terminated by parent.");
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
    new CampaignCombatTimeoutService(CampaignDurableAuthorityJournalStore.ForSingleCampaign(journal), clock,
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

internal sealed record ScheduledArrivalInput(string Path, CampaignSeatCredential Credential, string Reference);

internal sealed record SourceCampaignInput(string Path, string CampaignId);

internal sealed record SourceOwnerCrashInput(string Path, CampaignSeatCredential Credential, long Revision,
    string Reference, long NowUnixMilliseconds, bool AfterCommit);

internal sealed record PostDrainCrashInput(string Path, CampaignSeatCredential Credential,
    string ActingElementId, long NowUnixMilliseconds);

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
