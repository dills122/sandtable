using System.Text;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

public sealed class P01PrivateHostFoundationTests
{
    private const string Campaign = "rules-lab.combat-creation.1";
    private const string Checkpoint = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Operator = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void GrantIsSingleUseAndReplacementCredentialSurvivesRestart()
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        store.SeedUnissuableCampaign(Campaign, Checkpoint, Operator);
        var fence = store.AcquireActivationFence(Campaign, Operator);
        var grant = store.IssueGrant(Campaign, "axis", fence, Operator);
        var credential = store.RedeemGrant(grant, fence)!;
        Assert.Null(store.RedeemGrant(grant, fence));
        Assert.NotEqual(grant.Secret, credential.Secret);
        Assert.NotNull(store.Authenticate(credential));

        var reopened = fixture.Open();
        var first = reopened.Authenticate(credential)!;
        var second = reopened.Authenticate(credential)!;
        Assert.Equal(credential.AuthorityId, first.AuthorityId);
        Assert.NotEqual(first.SessionGeneration, second.SessionGeneration);
        Assert.Null(reopened.Authenticate(credential with { Secret = Operator }));
        Assert.Throws<UnauthorizedAccessException>(() => reopened.IssueGrant(
            Campaign, "commonwealth", fence, credential.Secret));
    }

    [Fact]
    public void ProbeCasExactRetryAndColdReadbackPreserveOneRecord()
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        store.SeedUnissuableCampaign(Campaign, Checkpoint, Operator);
        var fence = store.AcquireActivationFence(Campaign, Operator);
        var credential = store.RedeemGrant(store.IssueGrant(Campaign, "axis", fence, Operator), fence)!;
        var head = store.ReadHeadForTest(Campaign)!;
        var requestId = Guid.NewGuid().ToString("D");
        var bytes = Encoding.ASCII.GetBytes("storage-probe-only");
        var first = store.AppendProbe(credential, requestId, bytes, head.Sequence,
            head.Hash, fence);
        Assert.Equal(P01ProbeStatus.Committed, first.Status);
        Assert.Equal(1, first.Sequence);

        var reopened = fixture.Open();
        Assert.Equal(P01ProbeStatus.Duplicate, reopened.AppendProbe(credential,
            requestId, bytes, head.Sequence, head.Hash, fence).Status);
        Assert.Equal(P01ProbeStatus.Conflict, reopened.AppendProbe(credential,
            requestId, Encoding.ASCII.GetBytes("changed"), head.Sequence,
            head.Hash, fence).Status);
        Assert.Equal(P01ProbeStatus.Stale, reopened.AppendProbe(credential,
            Guid.NewGuid().ToString("D"), bytes, head.Sequence, head.Hash, fence).Status);
        Assert.Equal(1, reopened.ReadHeadForTest(Campaign)!.Sequence);
        Assert.Equal(first.Sequence, reopened.FindProbe(credential, requestId, bytes, fence)!.Sequence);
    }

    [Fact]
    public void RevocationAndActivationFenceDenyOldCredentialsAndWriters()
    {
        using var fixture = new StoreFixture();
        var old = fixture.Create();
        old.SeedUnissuableCampaign(Campaign, Checkpoint, Operator);
        var firstFence = old.AcquireActivationFence(Campaign, Operator);
        var credential = old.RedeemGrant(old.IssueGrant(Campaign, "axis", firstFence, Operator), firstFence)!;
        var head = old.ReadHeadForTest(Campaign)!;
        var newHost = fixture.Open();
        var secondFence = newHost.AcquireActivationFence(Campaign, Operator);
        Assert.True(secondFence.Fence > firstFence.Fence);
        Assert.Equal(P01ProbeStatus.Stale, old.AppendProbe(credential,
            Guid.NewGuid().ToString("D"), [1], head.Sequence, head.Hash,
            secondFence with { Secret = firstFence.Secret }).Status);
        Assert.Equal(P01ProbeStatus.Stale, old.AppendProbe(credential,
            Guid.NewGuid().ToString("D"), [1], head.Sequence, head.Hash,
            firstFence).Status);
        Assert.Throws<InvalidOperationException>(() => old.Revoke(
            Campaign, "axis", firstFence, Operator));
        Assert.True(newHost.Revoke(Campaign, "axis", secondFence, Operator));
        Assert.Null(fixture.Open().Authenticate(credential));
        Assert.Equal(P01ProbeStatus.Unauthorized, newHost.AppendProbe(credential,
            Guid.NewGuid().ToString("D"), [1], head.Sequence, head.Hash,
            secondFence).Status);
    }

    [Fact]
    public async Task CompetingAppendAndRevocationHaveOneDurableOrder()
    {
        using var fixture = new StoreFixture();
        var first = fixture.Create();
        first.SeedUnissuableCampaign(Campaign, Checkpoint, Operator);
        var fence = first.AcquireActivationFence(Campaign, Operator);
        var credential = first.RedeemGrant(first.IssueGrant(Campaign, "axis", fence, Operator), fence)!;
        var head = first.ReadHeadForTest(Campaign)!;
        var second = fixture.Open();
        var requestId = Guid.NewGuid().ToString("D");
        var append = Task.Run(() => first.AppendProbe(credential, requestId, [7],
            head.Sequence, head.Hash, fence));
        var revoke = Task.Run(() => second.Revoke(Campaign, "axis", fence, Operator));
        var appended = await append;
        Assert.True(await revoke);
        Assert.True(appended.Status is P01ProbeStatus.Committed
            or P01ProbeStatus.Unauthorized);
        Assert.Equal(appended.Status == P01ProbeStatus.Committed ? 1 : 0,
            fixture.Open().ReadHeadForTest(Campaign)!.Sequence);
    }

    [Fact]
    public void TamperedRecordAndOverCapInputFailClosed()
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        store.SeedUnissuableCampaign(Campaign, Checkpoint, Operator);
        var fence = store.AcquireActivationFence(Campaign, Operator);
        var credential = store.RedeemGrant(store.IssueGrant(Campaign, "axis", fence, Operator), fence)!;
        var head = store.ReadHeadForTest(Campaign)!;
        var requestId = Guid.NewGuid().ToString("D");
        Assert.Throws<ArgumentOutOfRangeException>(() => store.AppendProbe(credential,
            requestId, new byte[65_537], head.Sequence, head.Hash, fence));
        Assert.Equal(P01ProbeStatus.Committed, store.AppendProbe(credential,
            requestId, [1, 2], head.Sequence, head.Hash, fence).Status);
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE probe_records SET record_bytes=x'0304' WHERE campaign_id=$campaign";
            command.Parameters.AddWithValue("$campaign", Campaign);
            Assert.Equal(1, command.ExecuteNonQuery());
        }
        Assert.Null(fixture.Open().FindProbe(credential, requestId, [1, 2], fence));
    }

    [Fact]
    public void UnknownStoreVersionFailsClosed()
    {
        using var fixture = new StoreFixture();
        fixture.Create();
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version=2";
            command.ExecuteNonQuery();
        }
        Assert.Throws<InvalidOperationException>(() => fixture.Open());
    }

    [Fact]
    public void ForeignCampaignAndSeatCannotUseRetainedCredential()
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        store.SeedUnissuableCampaign(Campaign, Checkpoint, Operator);
        var lease = store.AcquireActivationFence(Campaign, Operator);
        var credential = store.RedeemGrant(store.IssueGrant(Campaign, "axis", lease,
            Operator), lease)!;
        var head = store.ReadHeadForTest(Campaign)!;
        var request = Guid.NewGuid().ToString("D");
        Assert.Equal(P01ProbeStatus.Stale, store.AppendProbe(credential with
        {
            CampaignId = "rules-lab.other",
        }, request, [1], head.Sequence, head.Hash, lease).Status);
        Assert.Equal(P01ProbeStatus.Unauthorized, store.AppendProbe(credential with
        {
            Side = "commonwealth",
        }, request, [1], head.Sequence, head.Hash, lease).Status);
        Assert.Equal(0, store.ReadHeadForTest(Campaign)!.Sequence);
    }

    [Fact]
    public void ExpiredGrantAndReplacementCredentialAreRejected()
    {
        var clock = new ManualClock();
        using var fixture = new StoreFixture();
        var store = fixture.Create(clock);
        store.SeedUnissuableCampaign(Campaign, Checkpoint, Operator);
        var lease = store.AcquireActivationFence(Campaign, Operator);
        var grant = store.IssueGrant(Campaign, "axis", lease, Operator);
        clock.Advance(TimeSpan.FromMinutes(11));
        Assert.Null(store.RedeemGrant(grant, lease));

        using var secondFixture = new StoreFixture();
        var second = secondFixture.Create(clock);
        second.SeedUnissuableCampaign(Campaign, Checkpoint, Operator);
        var secondLease = second.AcquireActivationFence(Campaign, Operator);
        var credential = second.RedeemGrant(second.IssueGrant(Campaign, "axis",
            secondLease, Operator), secondLease)!;
        clock.Advance(TimeSpan.FromHours(2).Add(TimeSpan.FromMilliseconds(1)));
        Assert.Null(secondFixture.Open(clock).Authenticate(credential));
    }

    private sealed class StoreFixture : IDisposable
    {
        private readonly string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "sandtable-p01-host-" + Guid.NewGuid().ToString("N"));
        internal string Path => System.IO.Path.Combine(directory, "authority.sqlite");

        internal P01PrivateHostFoundation Create(TimeProvider? clock = null) =>
            P01PrivateHostFoundation.CreateNew(Path, Operator, clock);

        internal P01PrivateHostFoundation Open(TimeProvider? clock = null) =>
            P01PrivateHostFoundation.OpenExisting(Path, clock);

        public void Dispose()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        internal void Advance(TimeSpan duration) => now = now.Add(duration);
    }
}
