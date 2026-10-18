using Cna.Core.Actions;

namespace Cna.OrleansHost.Api;

// Private development-host seam. No HTTP route calls this until MAP-08B's coherent
// post-step view and diary gate is satisfied. The public OpenAPI stays unchanged.
internal sealed class TemporaryOutwardActionIngress(
    LocalSessionStore sessions, IGrainFactory grains, TemporaryCampaignAccess access)
{
    internal async Task<TemporaryOutwardActionReply> SubmitAsync(string sessionToken,
        TemporaryOutwardActionRequest request)
    {
        var session = sessions.Find(sessionToken);
        var seat = Bind(session, request.CampaignId);
        if (seat is null)
            return new(TemporaryCampaignStatus.Rejected, "unauthorized");
        var reply = await grains.GetGrain<ITemporaryCampaignGrain>(request.CampaignId)
            .SubmitOutwardAsync(access.Capability, seat, request);
        return sessions.Find(sessionToken) == session
            ? reply : new(TemporaryCampaignStatus.Rejected, "unauthorized");
    }

    internal async Task<TemporaryOutwardActionReply> FindRequestAsync(string sessionToken,
        string campaignId, string requestId)
    {
        var session = sessions.Find(sessionToken);
        var seat = Bind(session, campaignId);
        if (seat is null)
            return new(TemporaryCampaignStatus.Rejected, "unauthorized");
        var reply = await grains.GetGrain<ITemporaryCampaignGrain>(campaignId)
            .FindOutwardRequestAsync(access.Capability, seat, requestId);
        return sessions.Find(sessionToken) == session
            ? reply : new(TemporaryCampaignStatus.Rejected, "unauthorized");
    }

    private static TemporaryAuthenticatedSeat? Bind(LocalSession? session, string campaignId)
    {
        if (session?.CampaignId != campaignId || session.Generation is null) return null;
        var audience = session.Seat switch
        {
            "axis" => CampaignActionAudience.Axis,
            "commonwealth" => CampaignActionAudience.Commonwealth,
            _ => (CampaignActionAudience?)null,
        };
        return audience is null ? null : new TemporaryAuthenticatedSeat(
            session.OperatorId, campaignId, audience.Value, session.Generation);
    }
}
