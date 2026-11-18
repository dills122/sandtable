namespace Cna.OrleansHost.Api;

// Process-local identity scaffold. Campaign creation will issue grants only after a committed
// MAP-08B creation record exists; this store never infers a seat from browser input.
internal sealed class LocalSessionStore(TimeProvider clock)
{
    internal const int MaxSessions = 256;
    internal const int MaxGrants = 512;
    internal const int MaxRedemptions = 256;
    private static readonly TimeSpan OperatorLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SeatLifetime = TimeSpan.FromHours(2);
    private readonly object gate = new();
    private readonly Dictionary<string, SessionEntry> sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalGrant> grants = new(StringComparer.Ordinal);
    // Keyed on the OLD (consumed) session token - see RedeemAsync/RecoverRedemption.
    private readonly Dictionary<string, RedemptionRecord> redemptions = new(StringComparer.Ordinal);

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

    // Recoverability contract (issue #389): the grant and the old session are consumed
    // atomically with minting `next` below, inside the same lock. If the HTTP response
    // carrying `next.Token` never reaches the caller (dropped connection, lost reply),
    // the grant can't be redeemed again - it's gone - so without RecoverRedemption the
    // browser would be stuck holding a cookie that matches nothing. A retry that presents
    // the SAME old cookie, the SAME grantId, the SAME campaignId and the old session's CSRF
    // token recovers the SAME `next` via RecoverRedemption; it does not make the grant
    // reusable by a different caller (see that method's own comment). What is NOT
    // recoverable: once `next`'s own session entry is gone - independently relinquished,
    // superseded by a later seat switch, or past the redemption record's retention window
    // (bounded by the grant's own original expiry, below) - a retry can no longer recover
    // it and must re-authenticate from scratch (there is no grant left to redeem). That is
    // intentional: this cache is a short-lived reply cache for one lost response, not a
    // durable session-recovery mechanism.
    internal async Task<LocalSession?> RedeemAsync(LocalSession current,
        string campaignId, string grantId, CancellationToken cancellation = default)
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
        try
        {
            await entry.AdmissionGate.WaitAsync(cancellation);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up before being admitted (e.g. the HTTP request was
            // aborted/disconnected while waiting). Nothing was redeemed - undo the
            // pending-revocation marker so WithAuthorizedSeatAsync/RelinquishAsync don't
            // see a phantom revocation that would otherwise never resolve.
            lock (gate) { entry.RevocationsRequested--; }
            throw;
        }
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
                // Retain just enough to answer an exact retry of this same redemption if
                // its response never reached the caller. Retention is bounded by the
                // grant's own already-defined expiry (it supplants that grant, so it
                // inherits its lifetime rather than adding a new unbounded cache); dropping
                // the record under capacity pressure just means that one retry window is
                // unavailable, not a correctness issue.
                if (redemptions.Count < MaxRedemptions)
                    redemptions[current.Token] = new RedemptionRecord(campaignId, grantId,
                        current.CsrfToken, next.Token, grant.ExpiresAt);
                return next;
            }
        }
        finally { entry.AdmissionGate.Release(); }
    }

    // Recovers the outcome of an already-committed RedeemAsync whose response never
    // reached its caller. Requires all four of: the exact old session cookie (the
    // dictionary key), the same grantId, the same campaignId, and the old session's own
    // CSRF token. An unrelated caller never had that cookie or CSRF token, so this
    // recovers only the SAME caller's own already-redeemed outcome - it is "the same
    // caller retrying because it never got its own response," never "the grant becomes
    // reusable." See RedeemAsync's comment for what is and isn't recoverable.
    internal LocalSession? RecoverRedemption(string oldToken, string campaignId,
        string grantId, string? csrfToken)
    {
        lock (gate)
        {
            SweepExpired();
            if (!redemptions.TryGetValue(oldToken, out var record)
                || record.ExpiresAt <= clock.GetUtcNow()
                || record.CampaignId != campaignId || record.GrantId != grantId
                || record.CsrfToken != csrfToken)
                return null;
            // The retention window above only bounds how long we'll try; the recovered
            // session must still be live right now (not since relinquished, superseded by
            // a later seat switch, or swept for its own expiry) for recovery to succeed.
            return sessions.TryGetValue(record.NextToken, out var next) ? next.Session : null;
        }
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
        foreach (var token in redemptions.Where(pair => pair.Value.ExpiresAt <= now)
                     .Select(pair => pair.Key).ToArray())
            redemptions.Remove(token);
    }
}

internal sealed record LocalSession(string Token, string CsrfToken, string OperatorId,
    string? Generation, string? CampaignId, string? Seat);
internal sealed record LocalGrant(string OperatorId, string CampaignId, string Seat,
    DateTimeOffset ExpiresAt, bool Active);
internal sealed record RedemptionRecord(string CampaignId, string GrantId,
    string CsrfToken, string NextToken, DateTimeOffset ExpiresAt);
internal sealed class SessionEntry(LocalSession session, DateTimeOffset expiresAt)
{
    internal LocalSession Session { get; } = session;
    internal DateTimeOffset ExpiresAt { get; set; } = expiresAt;
    internal SemaphoreSlim AdmissionGate { get; } = new(1, 1);
    internal int RevocationsRequested { get; set; }
}
