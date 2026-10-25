using System.Text.Json;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Microsoft.Extensions.Logging.Abstractions;

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

internal sealed class NativeFixedClock(long nowUnixMilliseconds) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeMilliseconds(nowUnixMilliseconds);
}
