using Npgsql;
using Testcontainers.PostgreSql;

namespace Cna.OrleansHost.Tests.Persistence;

// Design doc §8 (docs/work/plans/2026-11-18-postgres-migration-design.md) is explicit: "test
// against a real PostgreSQL instance ... not an in-memory or SQLite stand-in." This fixture starts
// exactly one real Postgres container, shared across every test in PostgresCollection.Name so
// Phase A's handful of tests pay the container-startup cost once. Each test then creates its own
// database off the shared instance (CreateDatabaseAsync) so tests stay isolated from each other
// without needing a second container per test.
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public ValueTask InitializeAsync() =>
        new(container.StartAsync(TestContext.Current.CancellationToken));

    public ValueTask DisposeAsync() => container.DisposeAsync();

    /// <summary>
    /// Creates a fresh, empty database on the shared container and returns a connection string
    /// pointing at it. Used to give each test (or each tier within a test) its own database
    /// without starting a second container -- this is the local/test stand-in for design doc
    /// §2.1's "two ordinary separate stores, one per tier," each its own database.
    /// </summary>
    public async Task<string> CreateDatabaseAsync(string databaseName)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync(cancellationToken);

        var builder = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = databaseName,
        };
        return builder.ConnectionString;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollectionDefinition : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "Postgres container";
}
