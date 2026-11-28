using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Hosting;

namespace Cna.OrleansHost.Api;

internal static partial class CampaignDurableOutwardApi
{
    private static void MapObservation(RouteGroupBuilder api) =>
        api.MapPost("/{campaignId}/observation", async (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) =>
            await Execute(context, async () =>
            {
                using var document = await ReadBody(context);
                var root = document.RootElement;
                RequireShape(root, "contractVersion", "credential");
                RequireVersion(root);
                var credential = Credential(root.GetProperty("credential"), campaignId);
                var (audience, view, deadline) = await journal.QueryObservationAsync(credential,
                    context.RequestAborted);
                // Hash the entire published cut (including audience and deadline), not
                // the private authority version, which changes for hidden-only events.
                var payload = new CampaignObservationResponse(2, campaignId, Side(audience), view.Position,
                    deadline, view.WorldAvailable, view.VisibleLocations, view.VisibleEdges,
                    view.OwnElements, view.ApparentContacts, null);
                var revision = Convert.ToHexStringLower(SHA256.HashData(
                    JsonSerializer.SerializeToUtf8Bytes(payload)));
                return Results.Json(payload with { ObservationRevision = revision });
            }));

    private sealed record CampaignObservationResponse(int ContractVersion, string CampaignId, string Audience,
        CampaignHostObservationPosition? Position, long? Deadline, bool WorldAvailable,
        IReadOnlyList<CampaignHostObservationLocation> VisibleLocations,
        IReadOnlyList<CampaignHostObservationEdge> VisibleEdges,
        IReadOnlyList<CampaignHostObservationElement> OwnElements,
        IReadOnlyList<CampaignHostObservationContact> ApparentContacts, string? ObservationRevision);
}
