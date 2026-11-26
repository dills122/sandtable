using Cna.Core.Campaigns;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Persistence;
using Marten;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Cna.LoadHarness;

// Each run gets an isolated database, even on a shared external Postgres server.
internal sealed class PostgresBacking(int hotPool) : IAsyncDisposable
{
    private readonly string database = "load_" + Guid.NewGuid().ToString("N");
    private PostgreSqlContainer? container;
    private string? adminConnectionString;
    private string? hotConnectionString;
    private DocumentStore? documentStore;
    private bool created;

    internal async Task StartAsync(CancellationToken cancellationToken)
    {
        var external = Environment.GetEnvironmentVariable("SANDTABLE_POSTGRES_HOT_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(external))
        {
            var builder = new PostgreSqlBuilder("postgres:16-alpine");
            if (Environment.GetEnvironmentVariable("AO_SESSION_ID") is { Length: > 0 } session)
                builder = builder.WithLabel("ao.session", session);
            container = builder.Build();
            await container.StartAsync(cancellationToken).ConfigureAwait(false);
            external = container.GetConnectionString();
        }

        var admin = new NpgsqlConnectionStringBuilder(external) { Pooling = false };
        adminConnectionString = admin.ConnectionString;
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{database}\"";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        created = true;
        hotConnectionString = new NpgsqlConnectionStringBuilder(external)
        {
            Database = database,
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = hotPool,
        }.ConnectionString;
    }

    internal async Task<CampaignDurableAuthorityJournalStore> CreateStoreAsync(string operatorSecret,
        CancellationToken cancellationToken)
    {
        // Identical factory, schema and backend to the production Host.
        documentStore = MartenStoreFactory.CreateStore(hotConnectionString!, CampaignEventStreamSchema.Configure);
        var backend = await CampaignJournalMartenBackend.CreateAsync(documentStore, operatorSecret,
            TimeProvider.System, Cna1979SyntheticContentResolver.Instance, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new CampaignDurableAuthorityJournalStore(backend);
    }

    internal async Task<int> CountConnectionsAsync(CancellationToken cancellationToken)
    {
        // Monitor from the administrative database without adding a connection to the hot pool.
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*)::int FROM pg_stat_activity WHERE datname = @database AND backend_type = 'client backend'";
        command.Parameters.AddWithValue("database", database);
        return (int)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            documentStore?.Dispose();
            if (created && container is null)
            {
                await using var connection = new NpgsqlConnection(adminConnectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                await using var command = connection.CreateCommand();
                // Only the unique database created successfully by this instance is removed.
                command.CommandText = $"DROP DATABASE \"{database}\" WITH (FORCE)";
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            if (container is not null) await container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
