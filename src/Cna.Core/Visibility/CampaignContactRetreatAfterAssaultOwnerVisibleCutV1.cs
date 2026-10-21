using System.Text.Json;
using Cna.Core.Observations;

namespace Cna.Core.Visibility;

// Defender-facing post-result retreat bytes omit the private authority fence,
// Core action IDs and opposing World identity.
internal static class CampaignContactRetreatAfterAssaultOwnerVisibleCutV1
{
    internal const int ContractVersion = 1;

    internal static byte[] SerializeCanonical(CampaignContactRetreatAfterAssaultCut cut)
    {
        ArgumentNullException.ThrowIfNull(cut);
        if (cut.AuthorityVersion < 1
            || cut.ContactObservation.CampaignId.Length == 0
            || cut.ContactObservation.Side != cut.DefenderSide
            || cut.ContactObservation.ActingFormationId != cut.DefenderFormationId)
            throw new ArgumentException("Retreat cut is not a defender contact projection.",
                nameof(cut));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("campaignId", cut.ContactObservation.CampaignId);
            writer.WriteString("side", cut.DefenderSide == Cna.Core.Rules.LandSide.Axis
                ? "axis" : "commonwealth");
            writer.WriteString("positionId", cut.PositionId);
            writer.WriteString("defenderFormationId", cut.DefenderFormationId);
            writer.WriteString("retreatSourceHash", cut.RetreatSourceHash);
            writer.WriteNumber("contactAudienceRevision",
                cut.ContactObservation.AudienceRevision);
            writer.WritePropertyName("contactObservation");
            writer.WriteRawValue(cut.ContactObservation.SerializeCanonical());
            CampaignObservationSerializer.WriteOwnElements(writer, [cut.Defender]);
            writer.WriteStartArray("moveOffers");
            foreach (var offer in cut.MoveOffers.OrderBy(value =>
                         value.DestinationLocationId, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("kind", offer.Kind);
                writer.WriteString("elementId", offer.ElementId);
                writer.WriteString("originLocationId", offer.OriginLocationId);
                writer.WriteString("destinationLocationId", offer.DestinationLocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartObject("refuseOffer");
            writer.WriteString("kind", cut.RefuseOffer.Kind);
            writer.WriteString("elementId", cut.RefuseOffer.ElementId);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
