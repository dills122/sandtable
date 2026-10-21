using System.Text.Json;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

// Defender-facing bytes. No Core state version, attacker binding, action ID or
// opposing World position enters this projection.
internal static class CampaignContactRetreatBeforeAssaultOwnerVisibleCutV1
{
    public const int ContractVersion = 1;

    public static byte[] SerializeCanonical(CampaignContactRetreatBeforeAssaultCut cut)
    {
        ArgumentNullException.ThrowIfNull(cut);
        if (cut.AuthorityVersion < 1
            || cut.ContactObservation.ContractVersion != CampaignContactObservationV1.CurrentContractVersion
            || cut.ContactObservation.Side != cut.DefenderSide
            || cut.ContactObservation.ActingFormationId != cut.DefenderFormationId)
            throw new ArgumentException("RBA cut is not a defender contact projection.", nameof(cut));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("campaignId", cut.ContactObservation.CampaignId);
            writer.WriteString("side", cut.DefenderSide == LandSide.Axis ? "axis" : "commonwealth");
            writer.WriteString("positionId", cut.PositionId);
            writer.WriteString("defenderFormationId", cut.DefenderFormationId);
            writer.WriteNumber("contactAudienceRevision", cut.ContactObservation.AudienceRevision);
            writer.WritePropertyName("contactObservation");
            writer.WriteRawValue(cut.ContactObservation.SerializeCanonical());
            CampaignObservationSerializer.WriteLocations(writer, cut.Locations);
            CampaignObservationSerializer.WriteEdges(writer, cut.Edges);
            CampaignObservationSerializer.WriteOwnElements(writer, [cut.Defender]);
            writer.WriteBoolean("initiallyAdjacentToKnownEnemy", cut.InitiallyAdjacentToKnownEnemy);
            writer.WriteStartArray("moveOffers");
            foreach (var offer in cut.MoveOffers.OrderBy(value => value.ElementId, StringComparer.Ordinal)
                         .ThenBy(value => value.DestinationLocationId, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("kind", offer.Kind);
                writer.WriteString("elementId", offer.ElementId);
                writer.WriteString("originLocationId", offer.OriginLocationId);
                writer.WriteString("destinationLocationId", offer.DestinationLocationId);
                writer.WriteNumber("totalCostNumerator", offer.CostBreakdown.TotalCost.Numerator);
                writer.WriteNumber("totalCostDenominator", offer.CostBreakdown.TotalCost.Denominator);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartObject("declineOffer");
            writer.WriteString("kind", cut.DeclineOffer.Kind);
            writer.WriteString("elementId", cut.DeclineOffer.ElementId);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
