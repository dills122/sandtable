using Cna.OrleansHost.Api;
using Cna.OrleansHost.Persistence;
using JasperFx;
using Marten;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Cna.OrleansHost.Tests.Persistence;

/// <summary>
/// Phase C3b of issue #439: one real Postgres container per test process (Testcontainers, never a
/// mock -- design doc &#167;8), lazily started on first use, from which every Marten-backed journal
/// test gets its own fresh database. Separate from <see cref="PostgresContainerFixture"/> (an xUnit
/// collection fixture) because the journal fixtures that need it are plain helper objects used
/// across many existing test classes, not collection members.
/// </summary>
internal static class SharedPostgres
{
    // Same opt-in external-Postgres mode as PostgresContainerFixture (see
    // docs/runbooks/local-testing.md) -- when set, reuse that shared instance instead of starting
    // a second, independent container here. The database name already includes a fresh GUID below,
    // so it is already collision-safe against other concurrent workers without further change.
    private static readonly string? ExternalConnectionString =
        Environment.GetEnvironmentVariable("SANDTABLE_TEST_POSTGRES_URL");

    private static readonly Lazy<Task<PostgreSqlContainer>> Container = new(async () =>
    {
        var container = new PostgreSqlBuilder("postgres:16-alpine").Build();
        await container.StartAsync();
        return container;
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    private static async Task<string> BaseConnectionStringAsync() =>
        ExternalConnectionString ?? (await Container.Value.ConfigureAwait(false)).GetConnectionString();

    internal static async Task<string> CreateDatabaseAsync()
    {
        var baseConnectionString = await BaseConnectionStringAsync().ConfigureAwait(false);
        var name = "journal_" + Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(baseConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{name}\"";
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        return new NpgsqlConnectionStringBuilder(baseConnectionString) { Database = name }.ConnectionString;
    }

    internal static DocumentStore CreateCampaignStore(string connectionString) =>
        MartenStoreFactory.CreateStore(connectionString, options =>
        {
            options.AutoCreateSchemaObjects = AutoCreate.All;
            CampaignEventStreamSchema.Configure(options);
        });
}

/// <summary>
/// A fresh Postgres database + Marten campaign store + <see cref="CampaignJournalMartenBackend"/>.
/// </summary>
internal sealed class MartenJournalDatabase : IDisposable
{
    private MartenJournalDatabase(DocumentStore store, CampaignJournalMartenBackend backend)
    {
        Store = store;
        Backend = backend;
    }

    internal DocumentStore Store { get; }
    internal CampaignJournalMartenBackend Backend { get; }

    internal static MartenJournalDatabase Create(string operatorSecret, TimeProvider? clock = null,
        Func<int, byte[]>? randomBytes = null) => Task.Run(async () =>
    {
        var store = SharedPostgres.CreateCampaignStore(await SharedPostgres.CreateDatabaseAsync());
        var backend = await CampaignJournalMartenBackend.CreateAsync(store, operatorSecret, clock,
            randomBytes: randomBytes);
        return new MartenJournalDatabase(store, backend);
    }).GetAwaiter().GetResult();

    /// <summary>A journal whose every call round-trips through Postgres (see
    /// <see cref="CampaignDurableAuthorityJournal.ForMartenSynchronousHarness"/>).</summary>
    internal CampaignDurableAuthorityJournal Journal() =>
        CampaignDurableAuthorityJournal.ForMartenSynchronousHarness(Backend);

    /// <summary>A fresh-process-equivalent journal over the same database, with its own clock.</summary>
    internal CampaignDurableAuthorityJournal Journal(string operatorSecret, TimeProvider? clock) =>
        CampaignDurableAuthorityJournal.ForMartenSynchronousHarness(Task.Run(() =>
            CampaignJournalMartenBackend.CreateAsync(Store, operatorSecret, clock)).GetAwaiter().GetResult());

    public void Dispose() => Store.Dispose();
}

/// <summary>A deterministic random source (DET-002) shared by both sides of an equivalence run.</summary>
internal sealed class DeterministicRandom(int seed)
{
    private readonly Random random = new(seed);

    internal byte[] Next(int count)
    {
        var bytes = new byte[count];
        random.NextBytes(bytes);
        return bytes;
    }
}
