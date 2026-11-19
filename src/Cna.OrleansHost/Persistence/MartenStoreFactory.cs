using Marten;

namespace Cna.OrleansHost.Persistence;

// Phase A infrastructure scaffolding only (docs/work/plans/2026-11-18-postgres-migration-design.md,
// implementing issue #439's Phase A). This does not define the real campaign event schema or touch
// CampaignDurableAuthorityJournal -- that is Phase C. Two responsibilities land here:
//
// 1. One Marten DocumentStore per PostgreSQL database, never one shared schema with a
//    tier-discriminator column (design doc §2/§2.1: "database-per-tenant as a first-class,
//    built-in pattern ... we would just configure two ordinary separate stores, one per tier").
//    Callers build the hot store and the cold store with two separate calls, each its own
//    connection string pointing at its own database.
// 2. Resolves the §3.3 open item -- the hash-chain's prior_hash/record_hash pair attaches via
//    Marten's own opt-in event headers (StoreOptions.Events.MetadataConfig.HeadersEnabled),
//    verified against Marten 9.62.0's current API (martendb.io/events/metadata), not a companion
//    table keyed by stream id + version. Headers are off by default ("lean" mode omits the extra
//    metadata columns), so every store built here turns them on.
public static class MartenStoreFactory
{
    /// <summary>
    /// Builds a Marten <see cref="DocumentStore"/> against a single PostgreSQL database, with
    /// event headers enabled so the hash-chain pair can ride alongside each event (design doc
    /// §3.3). <paramref name="configure"/> lets a caller layer on tier-specific schema
    /// configuration without duplicating the header/connection setup every tier needs.
    /// </summary>
    public static DocumentStore CreateStore(string connectionString, Action<StoreOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return DocumentStore.For(options =>
        {
            options.Connection(connectionString);
            options.Events.MetadataConfig.HeadersEnabled = true;
            configure?.Invoke(options);
        });
    }
}
