using Cna.Core.Actions;
using Cna.Core.Hosting;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournalStore
{
    internal async Task<(CampaignActionAudience Audience, CampaignHostObservation View, long? Deadline)>
        QueryObservationAsync(CampaignSeatCredential credential, CancellationToken cancellationToken)
    {
        var id = RequireSafeCampaignId(credential.CampaignId);
        if (fixedJournal is not null) return fixedJournal.QueryObservation(credential);
        if (marten is not null)
        {
            // Read one repeatable snapshot of the stream and owner metadata.
            // Dispose without flushing or transferring timer actions.
            using var workingSet = await marten.OpenAsync([id], () => marten.RandomBytes(32),
                cancellationToken, repeatableRead: true).ConfigureAwait(false);
            if (!workingSet.CampaignExists(id)) throw new UnauthorizedAccessException();
            var journal = CampaignDurableAuthorityJournal.ForWorkingSet(workingSet.Connect, marten.AuthenticateOperator,
                marten.Clock, marten.ContentResolver, marten.RandomBytes);
            return journal.QueryObservation(credential);
        }
        if (journals.TryGetValue(id, out var cached) && cached.IsValueCreated)
            return cached.Value.QueryObservation(credential);
        if (directory is null) throw new InvalidOperationException("Store has no backing directory.");
        var path = JournalPath(directory, id);
        if (!File.Exists(path)) throw new UnauthorizedAccessException();
        // Do not insert into the journal cache just because a seat polled.
        return CampaignDurableAuthorityJournal.OpenExisting(path, clock, contentResolver)
            .QueryObservation(credential);
    }
}
