namespace Cna.OrleansHost.Api;

internal static partial class CampaignDurableOutwardApi
{
    /// <summary>
    /// <c>POST /{campaignId}/openings/open</c> (issue #621): the read-only list of owner openings
    /// currently open for the credential's seat. POST only because the seat secret travels in the
    /// body like every other v2 call; it is side-effect free (no opening upsert, no durable write).
    /// Registered on the same Development + loopback + same-origin group as every other v2 route.
    /// </summary>
    private static void MapOpenForSeat(RouteGroupBuilder api) =>
        api.MapPost("/{campaignId}/openings/open", async (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) =>
            await Execute(context, async () =>
            {
                using var document = await ReadBody(context);
                var root = document.RootElement;
                RequireShape(root, "contractVersion", "credential");
                RequireVersion(root);
                var credential = Credential(root.GetProperty("credential"), campaignId);
                var result = await journal.QueryOpenForSeatAsync(credential);
                return Results.Json(new
                {
                    contractVersion = 2,
                    result.CampaignId,
                    audience = Side(result.Audience),
                    open = result.Open.Select(entry => new
                    {
                        entry.OpeningStage,
                        entry.ActionStage,
                        entry.ActingElementIds,
                        entry.RuleNotImplemented,
                    }),
                });
            }));
}
