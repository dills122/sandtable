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
    // Opt-in: SANDTABLE_TEST_POSTGRES_URL lets several concurrent worker processes (each in its
    // own git worktree) share ONE long-lived, externally-started Postgres instance instead of each
    // starting its own Testcontainers container -- see scripts/shared-test-postgres.sh and
    // docs/runbooks/local-testing.md. Unset (the default -- CI and any single local run): behavior
    // is completely unchanged from before this mode existed.
    private readonly string? externalConnectionString =
        Environment.GetEnvironmentVariable("SANDTABLE_TEST_POSTGRES_URL");

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private string BaseConnectionString => externalConnectionString ?? container.GetConnectionString();

    public ValueTask InitializeAsync() =>
        externalConnectionString is not null
            ? ValueTask.CompletedTask
            : new(container.StartAsync(TestContext.Current.CancellationToken));

    public ValueTask DisposeAsync() =>
        // External mode: we don't own that instance -- other concurrent workers may still be
        // using it, so never stop/dispose it here. Default mode: unchanged, dispose our own
        // ephemeral container as before.
        externalConnectionString is not null ? ValueTask.CompletedTask : container.DisposeAsync();

    /// <summary>
    /// Creates a fresh, empty database on the shared container and returns a connection string
    /// pointing at it. Used to give each test (or each tier within a test) its own database
    /// without starting a second container -- this is the local/test stand-in for design doc
    /// §2.1's "two ordinary separate stores, one per tier," each its own database.
    ///
    /// Default mode: the requested <paramref name="databaseName"/> is created verbatim, exactly as
    /// before this fixture gained an external mode -- there is no collision risk, since only one
    /// process ever touches that fresh, ephemeral container.
    ///
    /// External mode only: almost every call site passes a fixed literal name (e.g.
    /// "sandtable_c2_inline_fold"), which would collide if two worker processes pointed at the same
    /// shared instance created it at once, so the actual database created is uniquified (see
    /// <see cref="UniquifyDatabaseName"/>). Callers never see or assert on the literal database
    /// name, only the connection string returned here, so this is transparent to every caller.
    /// </summary>
    public async Task<string> CreateDatabaseAsync(string databaseName)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var actualDatabaseName = externalConnectionString is not null
            ? UniquifyDatabaseName(databaseName)
            : databaseName;

        await using var connection = new NpgsqlConnection(BaseConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{actualDatabaseName}\"";
        await command.ExecuteNonQueryAsync(cancellationToken);

        var builder = new NpgsqlConnectionStringBuilder(BaseConnectionString)
        {
            Database = actualDatabaseName,
        };
        return builder.ConnectionString;
    }

    // Postgres truncates identifiers longer than NAMEDATALEN - 1 = 63 bytes rather than erroring,
    // so two long uniquified names could silently collide on their shared truncated prefix if we
    // didn't account for the limit. Truncate the base name (never the suffix, which is what
    // guarantees uniqueness) so the combined name always fits.
    private const int PostgresMaxIdentifierBytes = 63;

    private static string UniquifyDatabaseName(string baseName)
    {
        var suffix = $"_p{Environment.ProcessId}_{Guid.NewGuid().ToString("N")[..8]}";
        var maxBaseNameLength = Math.Max(0, PostgresMaxIdentifierBytes - suffix.Length);
        var truncatedBaseName = baseName.Length > maxBaseNameLength ? baseName[..maxBaseNameLength] : baseName;
        return truncatedBaseName + suffix;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollectionDefinition : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "Postgres container";
}
