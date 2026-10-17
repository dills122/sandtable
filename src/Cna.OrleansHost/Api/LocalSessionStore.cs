namespace Cna.OrleansHost.Api;

// Process-local identity scaffold. Campaign creation will issue grants only after a committed
// MAP-08B creation record exists; this store never infers a seat from browser input.
internal sealed class LocalSessionStore(TimeProvider clock)
{
    internal const int MaxSessions = 256;
    private static readonly TimeSpan OperatorLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SeatLifetime = TimeSpan.FromHours(2);
    private readonly object gate = new();
    private readonly Dictionary<string, SessionEntry> sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalGrant> grants = new(StringComparer.Ordinal);

    internal LocalSession? Bootstrap(string? cookie)
    {
        lock (gate)
        {
            SweepExpired();
            if (cookie is not null && sessions.TryGetValue(cookie, out var existing))
                return existing.Session;
            if (sessions.Count >= MaxSessions) return null;
            var session = new LocalSession(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(), null, null, null);
            sessions.Add(session.Token, new SessionEntry(session,
                clock.GetUtcNow().Add(OperatorLifetime)));
            return session;
        }
    }

    internal LocalSession? Find(string? cookie)
    {
        lock (gate)
        {
            SweepExpired();
            return cookie is not null && sessions.TryGetValue(cookie, out var found)
                ? found.Session : null;
        }
    }

    // Invoked only by a trusted creation coordinator after its campaign record commits.
    internal (string Axis, string Commonwealth) IssueGrants(string operatorId, string campaignId)
    {
        lock (gate)
        {
            var axis = Guid.NewGuid().ToString();
            var commonwealth = Guid.NewGuid().ToString();
            grants.Add(axis, new LocalGrant(operatorId, campaignId, "axis",
                clock.GetUtcNow().AddMinutes(10)));
            grants.Add(commonwealth, new LocalGrant(operatorId, campaignId, "commonwealth",
                clock.GetUtcNow().AddMinutes(10)));
            return (axis, commonwealth);
        }
    }

    internal LocalSession? Redeem(LocalSession current, string campaignId, string grantId)
    {
        lock (gate)
        {
            SweepExpired();
            if (!sessions.TryGetValue(current.Token, out var active) || active.Session != current
                || !grants.TryGetValue(grantId, out var grant)
                || grant.OperatorId != current.OperatorId || grant.CampaignId != campaignId
                || grant.ExpiresAt <= clock.GetUtcNow()) return null;
            grants.Remove(grantId);
            sessions.Remove(current.Token);
            var next = current with
            {
                Token = Guid.NewGuid().ToString(),
                CsrfToken = Guid.NewGuid().ToString(),
                Generation = Guid.NewGuid().ToString(),
                CampaignId = campaignId,
                Seat = grant.Seat,
            };
            sessions.Add(next.Token, new SessionEntry(next, clock.GetUtcNow().Add(SeatLifetime)));
            return next;
        }
    }

    internal bool Relinquish(LocalSession current, string campaignId)
    {
        lock (gate)
        {
            SweepExpired();
            if (!sessions.TryGetValue(current.Token, out var active) || active.Session != current
                || current.CampaignId != campaignId || current.Seat is null) return false;
            sessions.Remove(current.Token);
            return true;
        }
    }

    private void SweepExpired()
    {
        var now = clock.GetUtcNow();
        foreach (var token in sessions.Where(pair => pair.Value.ExpiresAt <= now)
                     .Select(pair => pair.Key).ToArray())
            sessions.Remove(token);
    }
}

internal sealed record LocalSession(string Token, string CsrfToken, string OperatorId,
    string? Generation, string? CampaignId, string? Seat);
internal sealed record LocalGrant(string OperatorId, string CampaignId, string Seat,
    DateTimeOffset ExpiresAt);
internal sealed record SessionEntry(LocalSession Session, DateTimeOffset ExpiresAt);
