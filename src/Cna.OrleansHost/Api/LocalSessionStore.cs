namespace Cna.OrleansHost.Api;

// Process-local identity scaffold. Campaign creation will issue grants only after a committed
// MAP-08B creation record exists; this store never infers a seat from browser input.
internal sealed class LocalSessionStore(TimeProvider clock)
{
    private readonly object gate = new();
    private readonly Dictionary<string, LocalSession> sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalGrant> grants = new(StringComparer.Ordinal);

    internal LocalSession Bootstrap(string? cookie)
    {
        lock (gate)
        {
            if (cookie is not null && sessions.TryGetValue(cookie, out var existing))
                return existing;
            var session = new LocalSession(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(), null, null, null);
            sessions.Add(session.Token, session);
            return session;
        }
    }

    internal LocalSession? Find(string? cookie)
    {
        lock (gate) return cookie is not null && sessions.TryGetValue(cookie, out var found)
            ? found : null;
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
            if (!sessions.TryGetValue(current.Token, out var active) || active != current
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
            sessions.Add(next.Token, next);
            return next;
        }
    }

    internal bool Relinquish(LocalSession current, string campaignId)
    {
        lock (gate)
        {
            if (!sessions.TryGetValue(current.Token, out var active) || active != current
                || current.CampaignId != campaignId || current.Seat is null) return false;
            sessions.Remove(current.Token);
            return true;
        }
    }
}

internal sealed record LocalSession(string Token, string CsrfToken, string OperatorId,
    string? Generation, string? CampaignId, string? Seat);
internal sealed record LocalGrant(string OperatorId, string CampaignId, string Seat,
    DateTimeOffset ExpiresAt);
