using System.Text.Json;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

// Task 020A4 (dormant): defender-facing bytes for the bounded decline-only Retreat Before Assault
// decision (CMB-POL-001 adopts decline-only RBA; T020-Q7 ruled 2026-11-20, option b). A separate
// codec from CampaignContactRetreatBeforeAssaultOwnerVisibleCutV1: it carries no move offers, no
// map topology and no move-only adjacency fact, so every listed offer is one the defender can take.
// No Core state version, attacker binding, action ID or opposing World position enters it.
internal static class CampaignContactRbaDeclineOwnerVisibleCutV1
{
    public const int ContractVersion = 1;

    public static byte[] SerializeCanonical(CampaignContactRetreatBeforeAssaultCut cut)
    {
        ArgumentNullException.ThrowIfNull(cut);
        if (cut.AuthorityVersion < 1
            || cut.ContactObservation.ContractVersion != CampaignContactObservationV1.CurrentContractVersion
            || cut.ContactObservation.Side != cut.DefenderSide
            || cut.ContactObservation.ActingFormationId != cut.DefenderFormationId
            || cut.DeclineOffer.ElementId != cut.DefenderElementId)
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
            CampaignObservationSerializer.WriteOwnElements(writer, [cut.Defender]);
            writer.WriteStartObject("declineOffer");
            writer.WriteString("kind", cut.DeclineOffer.Kind);
            writer.WriteString("elementId", cut.DeclineOffer.ElementId);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
