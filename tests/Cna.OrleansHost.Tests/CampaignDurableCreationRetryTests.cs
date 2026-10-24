using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

public sealed class CampaignDurableCreationRetryTests
{
    private const string Secret = "bb2ba979667134402906109cd92a6cf68427d84b58c30db5bcd0fae8bef692b1";

    [Fact]
    public void ExactRetryColdLoadsAdvancedHeadWithoutChangingEvidence()
    {
        using var fixture = new Fixture();
        var journal = fixture.Create();
        var request = Request();
        var session = journal.CreateCampaign(request);
        var set = Enum.GetValues<CampaignActionAudience>()
            .Select(side => CampaignExercises.Query(session, side).ActionSet!)
            .First(value => value.Candidates.Count > 0);
        Assert.True(journal.Append(request.CampaignId, new CampaignActionSubmission(1,
            set.CampaignId, set.StateVersion, set.PositionId, set.Audience,
            set.Candidates[0].ActionId)).IsAccepted);
        var before = fixture.Evidence();
        var retry = fixture.Open().CreateCampaign(Request());
        var expected = fixture.Open().ReadVerified(request.CampaignId);
        foreach (var side in Enum.GetValues<CampaignActionAudience>())
            Assert.Equal(CampaignExercises.Query(expected, side).ActionSet!.StateVersion,
                CampaignExercises.Query(retry, side).ActionSet!.StateVersion);
        Assert.Equal(before, fixture.Evidence());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void EveryChangedIdentityFieldConflictsWithoutWrites(int field)
    {
        using var fixture = new Fixture();
        var journal = fixture.Create();
        var r = Request();
        journal.CreateCampaign(r);
        var before = fixture.Evidence();
        var changed = new CampaignCreationRequest(field == 0 ? 2 : r.ContractVersion,
            r.CampaignId, field == 1 ? "changed" : r.RulesetHash,
            field == 2 ? r.Seed + 1 : r.Seed, field == 3 ? "changed" : r.SetupId,
            field == 4 ? "changed" : r.SetupHash, field == 5 ? "changed" : r.ContentPackId,
            field == 6 ? "changed" : r.ContentHash, field == 7 ? "changed" : r.ScenarioId);
        Assert.Throws<InvalidOperationException>(() => journal.CreateCampaign(changed));
        Assert.Equal(before, fixture.Evidence());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TimingRetryRequiresExactCanonicalBytesIncludingAbsence(bool explicitTiming)
    {
        using var fixture = new Fixture();
        var journal = fixture.Create();
        var request = Request();
        var timing = Timing(request);
        journal.CreateCampaign(request, explicitTiming ? timing : null);
        var before = fixture.Evidence();
        Assert.NotNull(fixture.Open().CreateCampaign(request, explicitTiming ? Timing(request) : null));
        Assert.Equal(before, fixture.Evidence());
        Assert.Throws<InvalidOperationException>(() => fixture.Open().CreateCampaign(request,
            explicitTiming ? null : timing));
        Assert.Throws<InvalidOperationException>(() => fixture.Open().CreateCampaign(
            new CampaignCreationRequest(request.ContractVersion, request.CampaignId, "changed",
                request.Seed, request.SetupId, request.SetupHash, request.ContentPackId,
                request.ContentHash, request.ScenarioId), timing));
        if (explicitTiming)
            Assert.Throws<InvalidOperationException>(() => fixture.Open().CreateCampaign(request,
                new CampaignDecisionTimingConfigurationV1("changed", request.RulesetHash,
                    request.ContentHash, timing.Windows)));
        Assert.Equal(before, fixture.Evidence());
    }

    private static CampaignDecisionTimingConfigurationV1 Timing(CampaignCreationRequest request) =>
        new("rules-lab.campaign-timing.1", request.RulesetHash, request.ContentHash,
            [new("custody", 30_000, "leave-unguarded"),
                new("cycle-control", 30_000, "finish-phase"),
                new("force-assignment", 30_000, "cancel-incomplete-voluntary-round"),
                new("rba", 30_000, "cancel-without-decline"),
                new("reserve-release", 30_000, "convert-unresolved-i-retain-ii"),
                new("retreat", 30_000, "refuse-retreat"),
                new("selection", 30_000, "no-selection")]);

    [Fact]
    public void CorruptRetainedCreationCannotBeAcknowledged()
    {
        using var fixture = new Fixture();
        var journal = fixture.Create();
        journal.CreateCampaign(Request());
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE campaigns SET initial_snapshot=x'01'";
            command.ExecuteNonQuery();
        }
        var before = fixture.Evidence();
        Assert.Throws<InvalidOperationException>(() => fixture.Open().CreateCampaign(Request()));
        Assert.Equal(before, fixture.Evidence());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IndependentConcurrentCreatorsRetainOneAuthority(bool different)
    {
        using var fixture = new Fixture();
        fixture.Create();
        using var start = new Barrier(2);
        Task<Exception?> Create(ulong seed) => Task.Run(() =>
        {
            var journal = fixture.Open();
            start.SignalAndWait();
            return Record.Exception(() => journal.CreateCampaign(Request(seed)));
        });
        var outcomes = await Task.WhenAll(Create(12345), Create(different ? 12346UL : 12345UL));
        Assert.Equal(different ? 1 : 2, outcomes.Count(value => value is null));
        if (different) Assert.IsType<InvalidOperationException>(outcomes.Single(value => value is not null));
        Assert.NotNull(fixture.Open().ReadVerified(Request().CampaignId));
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM campaigns WHERE head_sequence=0";
        Assert.Equal(1L, query.ExecuteScalar());
    }

    private static CampaignCreationRequest Request(ulong seed = 12345) => new(1,
        "campaign-creation-retry", Cna1979Ruleset.Manifest.Hash, seed,
        "rules-lab.breakdown.truck.v1", TemporaryCampaignGrain.SetupHash,
        "rules-lab.content.breakdown-truck.v1", TemporaryCampaignGrain.ContentHash,
        TemporaryCampaignGrain.ScenarioId);

    private sealed class Fixture : IDisposable
    {
        private readonly string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "sandtable-creation-retry-" + Guid.NewGuid().ToString("N"));
        internal string Path => System.IO.Path.Combine(directory, "campaign.sqlite");
        internal CampaignDurableAuthorityJournal Create() =>
            CampaignDurableAuthorityJournal.CreateNew(Path, Secret);
        internal CampaignDurableAuthorityJournal Open() => CampaignDurableAuthorityJournal.OpenExisting(Path);
        internal string[] Evidence()
        {
            using var connection = new SqliteConnection($"Data Source={Path}");
            connection.Open();
            var rows = new List<string>();
            foreach (var table in new[] { "campaigns", "accepted_steps" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table} ORDER BY rowid";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    for (var i = 0; i < reader.FieldCount; i++)
                        rows.Add(reader.GetValue(i) is byte[] bytes ? Convert.ToHexString(bytes)
                            : Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture)!);
            }
            return rows.ToArray();
        }
        public void Dispose() => Directory.Delete(directory, recursive: true);
    }
}
