namespace Cna.OrleansHost.Api;

// One admitted, process-local fixture. The gate binds creation, grant minting and the
// retained outward response so an identical lost-reply retry never mints new grants.
internal sealed class LocalCampaignCreationStore : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly TimeSpan deadline;
    private Creation? created;
    private PendingCreation? pending;

    public LocalCampaignCreationStore() : this(TimeSpan.FromSeconds(10)) { }

    internal LocalCampaignCreationStore(TimeSpan deadline)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(deadline, TimeSpan.Zero);
        this.deadline = deadline;
    }

    public void Dispose() => gate.Dispose();

    internal async Task<LocalCreationResult> CreateAsync(LocalSession session,
        string fixtureId, string requestId, LocalSessionStore sessions,
        ITemporaryCampaignCreator creator, CancellationToken cancellation)
    {
        await gate.WaitAsync(cancellation);
        try
        {
            if (sessions.Find(session.Token) != session || session.Seat is not null)
                return new LocalCreationResult(LocalCreationStatus.Unauthorized);
            if (created is not null)
                return created.OperatorId == session.OperatorId
                    && created.RequestId == requestId && created.FixtureId == fixtureId
                    ? new LocalCreationResult(LocalCreationStatus.Created, created.Response)
                    : new LocalCreationResult(LocalCreationStatus.Conflict);
            if (fixtureId != TemporaryCampaignGrain.FixtureCampaignId)
                return new LocalCreationResult(LocalCreationStatus.Invalid);
            if (pending is not null && (pending.OperatorId != session.OperatorId
                || pending.RequestId != requestId || pending.FixtureId != fixtureId))
                return new LocalCreationResult(LocalCreationStatus.Conflict);
            if (pending is null)
            {
                try
                {
                    var pair = sessions.ReserveCreationGrants(session, fixtureId);
                    if (pair is null)
                        return new LocalCreationResult(LocalCreationStatus.Unauthorized);
                    pending = new PendingCreation(session.OperatorId, requestId, fixtureId, pair.Value);
                }
                catch (InvalidOperationException)
                {
                    return new LocalCreationResult(LocalCreationStatus.Unavailable);
                }
            }

            TemporaryCampaignReply reply;
            try
            {
                reply = await creator.CreateAsync(fixtureId, session.OperatorId, requestId)
                    .WaitAsync(deadline, cancellation);
            }
            catch (TimeoutException)
            {
                // Outcome is unknown. The same operator/request retries against the grain;
                // its idempotent creation record decides whether it committed.
                return new LocalCreationResult(LocalCreationStatus.Unavailable);
            }
            if (reply.Status == TemporaryCampaignStatus.Conflict)
            {
                sessions.RevokeCreationGrants(pending.Grants);
                pending = null;
                return new LocalCreationResult(LocalCreationStatus.Conflict);
            }
            if (reply.Status != TemporaryCampaignStatus.Created)
            {
                sessions.RevokeCreationGrants(pending.Grants);
                pending = null;
                return new LocalCreationResult(LocalCreationStatus.Unavailable);
            }

            var grants = pending.Grants;
            // Expired grants stay expired. An exact retry retains their original IDs;
            // renewal needs a separate, authenticated contract.
            _ = sessions.ActivateCreationGrants(session.OperatorId, fixtureId, grants);
            var response = new
            {
                contractVersion = 1,
                campaign = new
                {
                    campaignId = fixtureId,
                    scenarioId = TemporaryCampaignGrain.ScenarioId,
                    displayName = "Breakdown truck lab",
                    lifetime = "temporary-memory",
                    status = "unsupported-phase",
                },
                rulesetHash = TemporaryCampaignGrain.RulesetHash,
                setupHash = TemporaryCampaignGrain.SetupHash[7..],
                contentHash = TemporaryCampaignGrain.ContentHash[7..],
                seatGrants = new { axis = grants.Axis, commonwealth = grants.Commonwealth },
            };
            created = new Creation(session.OperatorId, requestId, fixtureId, response);
            pending = null;
            return new LocalCreationResult(LocalCreationStatus.Created, response);
        }
        finally { gate.Release(); }
    }

    private sealed record Creation(string OperatorId, string RequestId, string FixtureId,
        object Response);
    private sealed record PendingCreation(string OperatorId, string RequestId,
        string FixtureId, (string Axis, string Commonwealth) Grants);
}

internal enum LocalCreationStatus { Created, Invalid, Unauthorized, Conflict, Unavailable }
internal sealed record LocalCreationResult(LocalCreationStatus Status, object? Response = null);
