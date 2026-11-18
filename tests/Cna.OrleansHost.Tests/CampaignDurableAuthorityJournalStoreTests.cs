using Cna.Core.Campaigns;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

/// <summary>
/// Covers <see cref="CampaignDurableAuthorityJournalStore"/>'s own responsibilities under the
/// design doc's &#167;3 file-per-campaign topology
/// (docs/work/plans/2026-11-18-data-layer-design-1000-campaign-ceiling.md): per-campaign file
/// routing/isolation, campaign-id path-injection rejection, host-identity sharing across campaign
/// files (including before any campaign exists), and the cross-file sweep-enumeration aggregation.
/// The existing <c>CampaignDurableAuthorityJournalTests*</c> suite already covers everything about
/// a single per-campaign file's own behavior (crash safety, migrations, hash-chain verification,
/// round mechanics) unmodified -- this file only covers what is new.
/// </summary>
public sealed class CampaignDurableAuthorityJournalStoreTests
{
    private const string OperatorSecret =
        "bb2ba979667134402906109cd92a6cf68427d84b58c30db5bcd0fae8bef692b1";

    private static CampaignCreationRequest Request(string campaignId) => new(1, campaignId,
        Cna1979Ruleset.Manifest.Hash, 12345,
        "rules-lab.breakdown.truck.v1", TemporaryCampaignGrain.SetupHash,
        "rules-lab.content.breakdown-truck.v1", TemporaryCampaignGrain.ContentHash,
        TemporaryCampaignGrain.ScenarioId);

    private sealed class StoreFixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(),
            "sandtable-authority-store-" + Guid.NewGuid().ToString("N"));
        internal CampaignDurableAuthorityJournalStore Create(TimeProvider? clock = null) =>
            new(directory, OperatorSecret, clock);
        internal string PathFor(string campaignId) =>
            Path.Combine(directory, campaignId + ".sqlite");
        internal string[] CampaignFiles() => Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*.sqlite") : [];
        public void Dispose()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void TwoCampaignsLandInTwoSeparateFilesAndStayIsolated()
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        var first = Request("campaign-store-isolation-one");
        var second = Request("campaign-store-isolation-two");
        store.CreateCampaign(first);
        store.CreateCampaign(second);
        Assert.True(File.Exists(fixture.PathFor(first.CampaignId)));
        Assert.True(File.Exists(fixture.PathFor(second.CampaignId)));
        Assert.NotEqual(fixture.PathFor(first.CampaignId), fixture.PathFor(second.CampaignId));
        // Each campaign's own file holds only its own row -- not the other campaign's.
        using var firstConnection = new SqliteConnection($"Data Source={fixture.PathFor(first.CampaignId)}");
        firstConnection.Open();
        using var firstCount = firstConnection.CreateCommand();
        firstCount.CommandText = "SELECT COUNT(*) FROM campaigns";
        Assert.Equal(1L, firstCount.ExecuteScalar());
        using var firstLookup = firstConnection.CreateCommand();
        firstLookup.CommandText = "SELECT campaign_id FROM campaigns";
        Assert.Equal(first.CampaignId, firstLookup.ExecuteScalar());
        Assert.NotNull(store.ReadVerified(first.CampaignId));
        Assert.NotNull(store.ReadVerified(second.CampaignId));
    }

    [Fact]
    public void UnknownCampaignThrowsAndNeverCreatesAFile()
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        Assert.Throws<InvalidOperationException>(() => store.ReadVerified("campaign-store-never-created"));
        Assert.False(File.Exists(fixture.PathFor("campaign-store-never-created")));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("a/b")]
    [InlineData("")]
    [InlineData("-leading-separator")]
    public void RejectsCampaignIdsUnsafeForAFilePath(string campaignId)
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        Assert.ThrowsAny<ArgumentException>(() => store.CreateCampaign(Request(campaignId)));
    }

    [Fact]
    public void HostIdentityIsSharedAndWorksBeforeAnyCampaignExists()
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        // No campaign created yet -- operator auth must still work, since §3's file-per-campaign
        // split moved operator_capability out of the (not-yet-existing) per-campaign file into a
        // small shared host-identity file created eagerly by the store's own constructor.
        Assert.True(store.AuthenticateOperator(OperatorSecret));
        Assert.False(store.AuthenticateOperator("0000000000000000000000000000000000000000000000000000000000dead"));
        var request = Request("campaign-store-host-identity");
        store.CreateCampaign(request);
        // Still works, and still shared, once a campaign exists.
        Assert.True(store.AuthenticateOperator(OperatorSecret));
        var grant = store.IssueSeatGrant(request.CampaignId,
            Cna.Core.Actions.CampaignActionAudience.Axis, OperatorSecret);
        Assert.NotNull(store.RedeemSeatGrant(grant));
    }

    [Fact]
    public void SweepEnumerationAggregatesAcrossSeparateCampaignFiles()
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        var first = Request("campaign-store-sweep-aggregate-one");
        var second = Request("campaign-store-sweep-aggregate-two");
        store.CreateCampaign(first);
        store.CreateCampaign(second);
        // Each campaign's collecting round lives only in its own file -- there is no single file a
        // pre-§3 CollectingSelectionCampaigns query could have scanned for both.
        foreach (var request in new[] { first, second })
        {
            using var connection = new SqliteConnection($"Data Source={fixture.PathFor(request.CampaignId)}");
            connection.Open();
            using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO selection_rounds VALUES " +
                "($id,'entry','0','config',0,1,0,'collecting','record')";
            insert.Parameters.AddWithValue("$id", request.CampaignId);
            insert.ExecuteNonQuery();
        }
        var collecting = store.CollectingSelectionCampaigns("", 100);
        Assert.Contains(first.CampaignId, collecting);
        Assert.Contains(second.CampaignId, collecting);
        Assert.Equal(2, fixture.CampaignFiles().Length);
    }

    [Fact]
    public void SweepEnumerationIsEmptyWhenNoCampaignExistsYet()
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        Assert.Empty(store.CollectingSelectionCampaigns("", 100));
        Assert.Empty(store.CollectingRbaCampaigns("", 100));
    }

    [Fact]
    public void ForSingleCampaignIgnoresTheRequestedIdAndAlwaysReturnsTheWrappedInstance()
    {
        var directory = Path.Combine(Path.GetTempPath(), "sandtable-authority-fixed-" + Guid.NewGuid().ToString("N"));
        try
        {
            var journal = CampaignDurableAuthorityJournal.CreateNew(
                Path.Combine(directory, "campaign.sqlite"), OperatorSecret);
            var request = Request("campaign-store-fixed-wrapper");
            journal.CreateCampaign(request);
            var store = CampaignDurableAuthorityJournalStore.ForSingleCampaign(journal);
            Assert.Same(journal, store.JournalFor("completely-unrelated-id", createIfMissing: false));
            Assert.NotNull(store.ReadVerified(request.CampaignId));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
