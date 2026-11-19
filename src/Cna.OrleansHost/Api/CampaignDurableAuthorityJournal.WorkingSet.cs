using Cna.Core.Campaigns;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

/// <summary>
/// Phase C3b of issue #439: the Marten-backed construction paths for this journal. See
/// <see cref="CampaignJournalWorkingSet"/> for the storage design.
/// </summary>
internal sealed partial class CampaignDurableAuthorityJournal
{
    private const string WorkingSetPath = ":marten-working-set:";

    /// <summary>
    /// A journal instance whose every <c>Connect</c> returns a connection to an already-loaded
    /// Marten working set (<paramref name="connect"/>). Used by
    /// <see cref="CampaignDurableAuthorityJournalStore"/>'s async Marten path, which loads the
    /// working set before calling into the (unchanged, synchronous) journal logic and flushes it
    /// back to Postgres afterwards in one <c>SaveChangesAsync</c>.
    /// </summary>
    internal static CampaignDurableAuthorityJournal ForWorkingSet(Func<SqliteConnection> connect,
        Func<string, bool> operatorAuthenticator, TimeProvider? clock = null,
        IContentPackV6Resolver? contentResolver = null, Func<int, byte[]>? randomBytes = null) =>
        new(WorkingSetPath, clock ?? TimeProvider.System, SchemaVersion, contentResolver, randomBytes,
            connect, operatorAuthenticator);

    /// <summary>
    /// Test-harness only (the SQLite-vs-Marten equivalence suite): a journal whose every
    /// <c>Connect</c> loads a fresh working set from Postgres (every campaign in the backend's
    /// database) and whose connection's disposal flushes it back -- i.e. every single journal call
    /// round-trips through Marten, exactly like the production async path, but behind this class's
    /// existing synchronous signatures so the existing journal fixtures run unchanged against
    /// Postgres. Bridges sync to async with <see cref="Task.Run(Func{Task})"/>; never used by the
    /// host, which only ever goes through the store's async path.
    /// </summary>
    internal static CampaignDurableAuthorityJournal ForMartenSynchronousHarness(CampaignJournalMartenBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);
        byte[]? pendingReferenceKey = null;
        byte[] NewReferenceKey() => pendingReferenceKey ??= backend.RandomBytes(32);
        // Each harness call holds the backend's harness gate from load to flush -- the moral
        // equivalent of the SQLite file's write lock (BEGIN IMMEDIATE + busy_timeout), so harness
        // instances sharing one database serialize exactly as two connections to one file did. The
        // production async path serializes differently (optimistic concurrency + retry in the store).
        return new(WorkingSetPath, backend.Clock, SchemaVersion, backend.ContentResolver, backend.RandomBytes,
            () =>
            {
                backend.HarnessGate.Wait();
                try
                {
                    var workingSet = Task.Run(() => backend.OpenAsync(null, NewReferenceKey, CancellationToken.None))
                        .GetAwaiter().GetResult();
                    return new FlushOnDisposeConnection(workingSet, backend.HarnessGate);
                }
                catch
                {
                    backend.HarnessGate.Release();
                    throw;
                }
            },
            backend.AuthenticateOperator);
    }

    /// <summary>Operator-capability digest, shared with the Marten backend's host document.</summary>
    internal static byte[] OperatorCapabilityDigest(string operatorSecret) =>
        CredentialDigest("operator", operatorSecret);

    /// <summary>Constant-time operator check against a stored capability digest.</summary>
    internal static bool OperatorCapabilityMatches(byte[] storedDigest, string secret) =>
        TryCredentialDigest("operator", secret) is { } digest && SameDigest(storedDigest, digest);

    private sealed class FlushOnDisposeConnection : SqliteConnection
    {
        private readonly CampaignJournalWorkingSet workingSet;
        private readonly SemaphoreSlim gate;
        private bool flushed;

        internal FlushOnDisposeConnection(CampaignJournalWorkingSet workingSet, SemaphoreSlim gate)
            : base(workingSet.ConnectionString)
        {
            this.workingSet = workingSet;
            this.gate = gate;
            Open();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing || flushed) return;
            flushed = true;
            try
            {
                Task.Run(() => workingSet.FlushAsync(CancellationToken.None)).GetAwaiter().GetResult();
            }
            finally
            {
                workingSet.Dispose();
                gate.Release();
            }
        }
    }
}
