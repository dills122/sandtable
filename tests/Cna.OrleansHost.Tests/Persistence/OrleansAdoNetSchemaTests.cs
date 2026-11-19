using Cna.OrleansHost.Persistence;
using Cna.OrleansHost.Timeouts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Cna.OrleansHost.Tests.Persistence;

// Phase C4 of issue #439 (design doc §4.1/§4.2): proves OrleansAdoNetSchema's embedded scripts
// are correct and that Program.cs's UseAdoNetClustering/UseAdoNetReminderService wiring actually
// works end to end -- against real PostgreSQL via Testcontainers (design doc §8: "test against a
// real PostgreSQL instance ... not an in-memory or SQLite stand-in"), the same fixture Phase
// A/C1/C2 already established (PostgresContainerFixture).
[Collection(PostgresCollectionDefinition.Name)]
public sealed class OrleansAdoNetSchemaTests(PostgresContainerFixture postgres)
{
    [Fact]
    public async Task EnsureSchemaAsyncCreatesTheMembershipAndReminderTables()
    {
        var connectionString = await postgres.CreateDatabaseAsync($"orleans_schema_{Guid.NewGuid():N}");

        await OrleansAdoNetSchema.EnsureSchemaAsync(connectionString, TestContext.Current.CancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        Assert.True(await TableExistsAsync(connection, "orleansquery"));
        Assert.True(await TableExistsAsync(connection, "orleansmembershiptable"));
        Assert.True(await TableExistsAsync(connection, "orleansmembershipversiontable"));
        Assert.True(await TableExistsAsync(connection, "orleansreminderstable"));
    }

    [Fact]
    public async Task EnsureSchemaAsyncIsANoOpOnARepeatSiloStartup()
    {
        var connectionString = await postgres.CreateDatabaseAsync($"orleans_schema_repeat_{Guid.NewGuid():N}");

        await OrleansAdoNetSchema.EnsureSchemaAsync(connectionString, TestContext.Current.CancellationToken);
        // The second call is the point of the test: the raw scripts are not idempotent on their
        // own (plain CREATE TABLE/CREATE FUNCTION, no IF NOT EXISTS guards), so this must not
        // throw -- exactly the "every silo startup calls this" scenario Program.cs relies on.
        await OrleansAdoNetSchema.EnsureSchemaAsync(connectionString, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AdoNetClusteringAndRemindersActuallyWorkAgainstRealPostgres()
    {
        var connectionString = await postgres.CreateDatabaseAsync($"orleans_adonet_e2e_{Guid.NewGuid():N}");
        await OrleansAdoNetSchema.EnsureSchemaAsync(connectionString, TestContext.Current.CancellationToken);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<ICampaignRoundDeadlineSink>(new NeverDueSink());
        builder.Services.AddSingleton(TimeProvider.System);
        // Mirrors Program.cs's non-Development branch exactly (same Invariant/options shape),
        // minus the schema bootstrap call (already done above) and the silo port/gateway
        // defaults, which matter only for real multi-silo clustering, not this single-silo test.
        builder.UseOrleans(silo =>
        {
            silo.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "test-cluster";
                options.ServiceId = "test-service";
            });
            silo.UseAdoNetClustering(options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
            });
            silo.AddReminders();
            silo.UseAdoNetReminderService(options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
            });
        });
        var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            // Clustering actually worked: this silo wrote its own membership row via
            // Clustering.AdoNet, not just that it started without throwing.
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1L, await ScalarCountAsync(connection, "OrleansMembershipTable"));
            Assert.Equal(1L, await ScalarCountAsync(connection, "OrleansMembershipVersionTable"));

            // Reminders.AdoNet actually worked: arming a CampaignRoundTimeoutGrain registers a
            // real durable reminder row, not just an in-memory one.
            var grain = host.Services.GetRequiredService<IGrainFactory>()
                .GetGrain<ICampaignRoundTimeoutGrain>(
                    CampaignRoundTimeoutGrainKey.For("adonet-e2e-campaign", CampaignRoundKind.Reserve));
            await grain.ArmAsync(DateTimeOffset.UtcNow.AddMinutes(30));
            Assert.Equal(1L, await ScalarCountAsync(connection, "OrleansRemindersTable"));
        }
        finally
        {
            await host.StopAsync(TestContext.Current.CancellationToken);
            host.Dispose();
        }
    }

    private static async Task<bool> TableExistsAsync(NpgsqlConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('public.' || @tableName) IS NOT NULL;";
        command.Parameters.AddWithValue("tableName", tableName);
        return (bool)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private static async Task<long> ScalarCountAsync(NpgsqlConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        // Unquoted: the schema scripts' unquoted CREATE TABLE names (e.g. OrleansMembershipTable)
        // are folded to lowercase by Postgres, same as this.
        command.CommandText = $"SELECT COUNT(*) FROM {tableName};";
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    // A deadline sink that is only ever asked "is anything due" by this test's own ArmAsync
    // call (which arms directly, bypassing GetDeadlineAsync) -- never consulted for real in
    // this end-to-end test, so it can safely report nothing is due.
    private sealed class NeverDueSink : ICampaignRoundDeadlineSink
    {
        public Task<DateTimeOffset?> GetDeadlineAsync(string campaignId, CampaignRoundKind roundKind,
            CancellationToken cancellationToken) => Task.FromResult((DateTimeOffset?)null);

        public Task<bool> TickAsync(string campaignId, CampaignRoundKind roundKind,
            CancellationToken cancellationToken) => Task.FromResult(false);
    }
}
