using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

public sealed class CampaignDurableAuthorityJournalTests
{
    private const string OperatorSecret = "bb2ba979667134402906109cd92a6cf68427d84b58c30db5bcd0fae8bef692b1";

    [Fact]
    public void SeatCredentialSurvivesRestartAndRotationRevokesIt()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        journal.CreateCampaign(Request());
        Assert.Throws<UnauthorizedAccessException>(() => journal.IssueSeatGrant(
            "campaign-durable-journal", CampaignActionAudience.Axis,
            new string('0', 64)));
        var grant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        Assert.DoesNotContain(grant.Secret, grant.ToString());
        var credential = fixture.Open().RedeemSeatGrant(grant)!;
        Assert.NotNull(credential);
        Assert.DoesNotContain(credential.Secret, credential.ToString());
        Assert.Null(fixture.Open().RedeemSeatGrant(grant));
        Assert.Equal(grant.Epoch, fixture.Open().AuthenticateSeat(credential)!.Epoch);
        Assert.Null(fixture.Open().AuthenticateSeat(credential with { Secret = new string('0', 64) }));

        var replacement = fixture.Open().IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        Assert.Equal(grant.Epoch + 1, replacement.Epoch);
        Assert.Null(fixture.Open().AuthenticateSeat(credential));
        Assert.NotNull(fixture.Open().AuthenticateSeat(fixture.Open().RedeemSeatGrant(replacement)!));
    }

    [Fact]
    public void GrantAndCredentialExpireAtTheirStoredDeadlines()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        journal.CreateCampaign(Request());
        var expiredGrant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        clock.Advance(TimeSpan.FromMinutes(10));
        Assert.Null(fixture.Open(clock).RedeemSeatGrant(expiredGrant));

        var grant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        var credential = fixture.Open(clock).RedeemSeatGrant(grant)!;
        Assert.NotNull(fixture.Open(clock).AuthenticateSeat(credential));
        clock.Advance(TimeSpan.FromHours(2));
        Assert.Null(fixture.Open(clock).AuthenticateSeat(credential));
    }

    [Fact]
    public void OwnerOpeningRemainsByteIdenticalAcrossUnseenAuthorityAdvanceAndRestart()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var session = journal.CreateCampaign(Request());
        var grant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var elementId = CampaignHostAdapter.QueryPrivateSideCut(session,
            CampaignActionAudience.Axis)!.OwnElements[0].ElementId;
        var first = journal.QueryOwnerOpening(credential, elementId);
        var reopened = fixture.Open().QueryOwnerOpening(credential, elementId);
        Assert.Equal(first.VisibleBytes, reopened.VisibleBytes);
        Assert.Equal(first.PublicActionRefs, reopened.PublicActionRefs);
        Assert.Equal(first.Revision, reopened.Revision);

        var system = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        var advance = new CampaignActionSubmission(1, system.CampaignId,
            system.StateVersion, system.PositionId, system.Audience,
            system.Candidates.Single().ActionId);
        Assert.True(journal.Append(advance.CampaignId, advance).IsAccepted);
        var after = fixture.Open().QueryOwnerOpening(credential, elementId);
        Assert.Equal(first.VisibleBytes, after.VisibleBytes);
        Assert.Equal(first.PublicActionRefs, after.PublicActionRefs);
        Assert.Equal(first.Revision, after.Revision);

        var replacementGrant = journal.IssueSeatGrant("campaign-durable-journal",
            CampaignActionAudience.Axis, OperatorSecret);
        var replacement = journal.RedeemSeatGrant(replacementGrant)!;
        Assert.Throws<UnauthorizedAccessException>(() => journal.QueryOwnerOpening(
            credential, elementId));
        Assert.Equal(first.Revision + 1,
            journal.QueryOwnerOpening(replacement, elementId).Revision);
    }

    [Fact]
    public void ExistingSharedDirectoryIsRejectedWithoutChangingItsPermissions()
    {
        if (OperatingSystem.IsWindows()) return;
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "sandtable-authority-shared-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var sharedMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead | UnixFileMode.GroupExecute;
        File.SetUnixFileMode(directory, sharedMode);
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                CampaignDurableAuthorityJournal.CreateNew(System.IO.Path.Combine(directory,
                    "campaign.sqlite"), OperatorSecret));
            Assert.Equal(sharedMode, File.GetUnixFileMode(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AcceptedCommandSurvivesColdReadAndStaleRetryDoesNotAdvanceHead()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var session = journal.CreateCampaign(Request());
        var actionSet = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        var command = new CampaignActionSubmission(1, CampaignExercises.QueryCheckpoint(session).CampaignId,
            actionSet.StateVersion, actionSet.PositionId, CampaignActionAudience.System,
            actionSet.Candidates.Single().ActionId);

        var accepted = journal.Append(command.CampaignId, command);
        Assert.True(accepted.IsAccepted);
        var reopened = fixture.Open();
        Assert.Equal(CampaignExercises.QueryCheckpoint(accepted.SuccessorSession!).StateVersion,
            CampaignExercises.QueryCheckpoint(reopened.ReadVerified(command.CampaignId)).StateVersion);

        var stale = reopened.Append(command.CampaignId, command);
        Assert.False(stale.IsAccepted);
        Assert.Equal(CampaignExercises.QueryCheckpoint(accepted.SuccessorSession!).StateVersion,
            CampaignExercises.QueryCheckpoint(fixture.Open().ReadVerified(command.CampaignId)).StateVersion);
    }

    [Fact]
    public void AlteredAcceptedEventFailsColdRead()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var session = journal.CreateCampaign(Request());
        var actionSet = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        var command = new CampaignActionSubmission(1, CampaignExercises.QueryCheckpoint(session).CampaignId,
            actionSet.StateVersion, actionSet.PositionId, CampaignActionAudience.System,
            actionSet.Candidates.Single().ActionId);
        Assert.True(journal.Append(command.CampaignId, command).IsAccepted);
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var update = connection.CreateCommand();
            update.CommandText = "UPDATE accepted_steps SET canonical_event=x'00'";
            Assert.Equal(1, update.ExecuteNonQuery());
        }
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadVerified(command.CampaignId));
    }

    private static CampaignCreationRequest Request()
    {
        return new CampaignCreationRequest(1, "campaign-durable-journal",
            Cna1979Ruleset.Manifest.Hash, 12345,
            "rules-lab.breakdown.truck.v1", TemporaryCampaignGrain.SetupHash,
            "rules-lab.content.breakdown-truck.v1", TemporaryCampaignGrain.ContentHash,
            TemporaryCampaignGrain.ScenarioId);
    }

    private sealed class JournalFixture : IDisposable
    {
        private readonly string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "sandtable-authority-journal-" + Guid.NewGuid().ToString("N"));
        internal string Path => System.IO.Path.Combine(directory, "campaign.sqlite");
        internal CampaignDurableAuthorityJournal Create(TimeProvider? clock = null) =>
            CampaignDurableAuthorityJournal.CreateNew(Path, OperatorSecret, clock);
        internal CampaignDurableAuthorityJournal Open(TimeProvider? clock = null) =>
            CampaignDurableAuthorityJournal.OpenExisting(Path, clock);
        public void Dispose() => Directory.Delete(directory, recursive: true);
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.Parse("2026-01-01T00:00:00Z",
            global::System.Globalization.CultureInfo.InvariantCulture);
        public override DateTimeOffset GetUtcNow() => now;
        internal void Advance(TimeSpan duration) => now = now.Add(duration);
    }
}
