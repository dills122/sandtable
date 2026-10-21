using System.Text.Json;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

/// <summary>Canonical owner bytes for a Combat decision; private defender bindings and action IDs stay in Core.</summary>
internal static class CampaignContactCombatOwnerVisibleCutV2
{
    public const int ContractVersion = 2;

    public static byte[] SerializeCanonical(CampaignContactCombatSelectionCut cut)
    {
        ArgumentNullException.ThrowIfNull(cut);
        var owner = cut.OwnerCut;
        var observation = owner.ContactObservation;
        if (cut.AuthorityVersion < 1
            || cut.AuthorityVersion != owner.AuthorityVersion
            || observation.ContractVersion != CampaignContactObservationV1.CurrentContractVersion)
            throw new ArgumentException("Combat owner cut is not an accepted contact projection.", nameof(cut));

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("campaignId", observation.CampaignId);
            writer.WriteString("side", observation.Side == LandSide.Axis ? "axis" : "commonwealth");
            writer.WriteString("actingFormationId", observation.ActingFormationId);
            writer.WriteNumber("contactAudienceRevision", observation.AudienceRevision);
            writer.WritePropertyName("contactObservation");
            writer.WriteRawValue(observation.SerializeCanonical());
            CampaignObservationSerializer.WriteLocations(writer, owner.Locations);
            CampaignObservationSerializer.WriteEdges(writer, owner.Edges);
            CampaignObservationSerializer.WriteOwnElements(writer, [owner.ActingElement]);
            writer.WriteStartArray("assaultOffers");
            foreach (var offer in cut.AssaultOffers.OrderBy(value => value.ContactHandle,
                         StringComparer.Ordinal).ThenBy(value => value.ApparentLocationId,
                         StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("kind", "select-contact-assault");
                writer.WriteString("actingElementId", offer.ActingElementId);
                writer.WriteString("contactHandle", offer.ContactHandle);
                writer.WriteString("apparentLocationId", offer.ApparentLocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
