namespace Cna.OrleansHost.Api;

// Process-local identity scaffold. Campaign creation will issue grants only after a committed
// MAP-08B creation record exists; this store never infers a seat from browser input.
internal sealed class LocalSessionStore(TimeProvider clock)
{
    internal const int MaxSessions = 256;
    internal const int MaxGrants = 512;
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
                && found.ExpiresAt > clock.GetUtcNow()
                ? found.Session : null;
        }
    }

    // Invoked only by a trusted creation coordinator after its campaign record commits.
    internal (string Axis, string Commonwealth) IssueGrants(string operatorId, string campaignId)
    {
        lock (gate)
        {
            SweepExpired();
            return IssueGrantsLocked(operatorId, campaignId, active: true);
        }
    }

    internal (string Axis, string Commonwealth)? ReserveCreationGrants(
        LocalSession session, string campaignId)
    {
        lock (gate)
        {
            SweepExpired();
            if (!sessions.TryGetValue(session.Token, out var entry)
                || entry.Session != session || session.Seat is not null)
                return null;
            var pair = IssueGrantsLocked(session.OperatorId, campaignId, active: false);
            entry.ExpiresAt = clock.GetUtcNow().Add(OperatorLifetime);
            return pair;
        }
    }

    internal bool ActivateCreationGrants(string operatorId, string campaignId,
        (string Axis, string Commonwealth) pair)
    {
        lock (gate)
        {
            SweepExpired();
            if (!grants.TryGetValue(pair.Axis, out var axis)
                || !grants.TryGetValue(pair.Commonwealth, out var commonwealth)
                || axis.OperatorId != operatorId || commonwealth.OperatorId != operatorId
                || axis.CampaignId != campaignId || commonwealth.CampaignId != campaignId
                || axis.Seat != "axis" || commonwealth.Seat != "commonwealth"
                || axis.Active || commonwealth.Active)
                return false;
            grants[pair.Axis] = axis with { Active = true };
            grants[pair.Commonwealth] = commonwealth with { Active = true };
            return true;
        }
    }

    internal void RevokeCreationGrants((string Axis, string Commonwealth) pair)
    {
        lock (gate)
        {
            grants.Remove(pair.Axis);
            grants.Remove(pair.Commonwealth);
        }
    }

    private (string Axis, string Commonwealth) IssueGrantsLocked(
        string operatorId, string campaignId, bool active)
    {
        if (grants.Count > MaxGrants - 2)
            throw new InvalidOperationException("Local grant capacity reached.");
        var axis = Guid.NewGuid().ToString();
        var commonwealth = Guid.NewGuid().ToString();
        grants.Add(axis, new LocalGrant(operatorId, campaignId, "axis",
            clock.GetUtcNow().AddMinutes(10), active));
        grants.Add(commonwealth, new LocalGrant(operatorId, campaignId, "commonwealth",
            clock.GetUtcNow().AddMinutes(10), active));
        return (axis, commonwealth);
    }

    internal async Task<LocalSession?> RedeemAsync(LocalSession current,
        string campaignId, string grantId)
    {
        SessionEntry? entry;
        lock (gate)
        {
            SweepExpired();
            sessions.TryGetValue(current.Token, out entry);
            if (entry?.Session != current || entry.ExpiresAt <= clock.GetUtcNow()
                || !grants.TryGetValue(grantId, out var grant)
                || !grant.Active || grant.OperatorId != current.OperatorId
                || grant.CampaignId != campaignId || grant.ExpiresAt <= clock.GetUtcNow())
                return null;
            entry.RevocationsRequested++;
        }
        await entry.AdmissionGate.WaitAsync();
        try
        {
            lock (gate)
            {
                SweepExpired();
                if (!sessions.TryGetValue(current.Token, out var active)
                    || active != entry || active.Session != current
                    || active.ExpiresAt <= clock.GetUtcNow()
                    || !grants.TryGetValue(grantId, out var grant)
                    || !grant.Active || grant.OperatorId != current.OperatorId
                    || grant.CampaignId != campaignId
                    || grant.ExpiresAt <= clock.GetUtcNow())
                {
                    entry.RevocationsRequested--;
                    return null;
                }
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
                sessions.Add(next.Token, new SessionEntry(next,
                    clock.GetUtcNow().Add(SeatLifetime)));
                return next;
            }
        }
        finally { entry.AdmissionGate.Release(); }
    }

    // Action admission and explicit seat revocation share this per-generation gate.
    // A dispatch admitted first finishes its serialized grain turn before relinquish
    // completes; relinquish admitted first makes the later dispatch unauthorized.
    internal async Task<T> WithAuthorizedSeatAsync<T>(string sessionToken,
        string campaignId, Func<LocalSession, Task<T>> dispatch, T unauthorized)
    {
        SessionEntry? entry;
        lock (gate)
        {
            SweepExpired();
            sessions.TryGetValue(sessionToken, out entry);
        }
        if (entry is null) return unauthorized;
        await entry.AdmissionGate.WaitAsync();
        try
        {
            lock (gate)
            {
                if (!sessions.TryGetValue(sessionToken, out var active)
                    || active != entry || entry.ExpiresAt <= clock.GetUtcNow()
                    || entry.RevocationsRequested > 0
                    || entry.Session.CampaignId != campaignId
                    || entry.Session.Seat is null)
                    return unauthorized;
            }
            return await dispatch(entry.Session);
        }
        finally { entry.AdmissionGate.Release(); }
    }

    internal async Task<bool> RelinquishAsync(LocalSession current, string campaignId)
    {
        SessionEntry? entry;
        lock (gate)
        {
            SweepExpired();
            sessions.TryGetValue(current.Token, out entry);
            if (entry?.Session != current || current.CampaignId != campaignId
                || current.Seat is null)
                return false;
            entry.RevocationsRequested++;
        }
        await entry.AdmissionGate.WaitAsync();
        try
        {
            lock (gate)
            {
                if (!sessions.TryGetValue(current.Token, out var active)
                    || active != entry || active.Session != current
                    || current.CampaignId != campaignId || current.Seat is null)
                {
                    entry.RevocationsRequested--;
                    return false;
                }
                sessions.Remove(current.Token);
                return true;
            }
        }
        finally { entry.AdmissionGate.Release(); }
    }

    private void SweepExpired()
    {
        var now = clock.GetUtcNow();
        foreach (var token in sessions.Where(pair => pair.Value.ExpiresAt <= now
                     && pair.Value.AdmissionGate.CurrentCount > 0
                     && pair.Value.RevocationsRequested == 0)
                     .Select(pair => pair.Key).ToArray())
            sessions.Remove(token);
        foreach (var id in grants.Where(pair => pair.Value.ExpiresAt <= now)
                     .Select(pair => pair.Key).ToArray())
            grants.Remove(id);
    }
}

internal sealed record LocalSession(string Token, string CsrfToken, string OperatorId,
    string? Generation, string? CampaignId, string? Seat);
internal sealed record LocalGrant(string OperatorId, string CampaignId, string Seat,
    DateTimeOffset ExpiresAt, bool Active);
internal sealed class SessionEntry(LocalSession session, DateTimeOffset expiresAt)
{
    internal LocalSession Session { get; } = session;
    internal DateTimeOffset ExpiresAt { get; set; } = expiresAt;
    internal SemaphoreSlim AdmissionGate { get; } = new(1, 1);
    internal int RevocationsRequested { get; set; }
}
