using System.Text.Json;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

// The Force Assignment opening contains only this owner's allocation and
// contact knowledge. An opposing seal cannot change these bytes.
internal static class CampaignContactForceAssignmentOwnerVisibleCutV1
{
    internal const int ContractVersion = 1;

    internal static byte[] SerializeCanonical(CampaignContactForceAssignmentCut cut)
    {
        ArgumentNullException.ThrowIfNull(cut);
        if (cut.AuthorityVersion < 1
            || cut.ContactObservation.ContractVersion != CampaignContactObservationV1.CurrentContractVersion
            || cut.ContactObservation.Side != cut.OwnerSide)
            throw new ArgumentException("Force Assignment cut is not owner scoped.", nameof(cut));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("campaignId", cut.ContactObservation.CampaignId);
            writer.WriteString("side", cut.OwnerSide == LandSide.Axis ? "axis" : "commonwealth");
            writer.WriteString("positionId", cut.PositionId);
            writer.WriteString("role", cut.Role);
            writer.WritePropertyName("contactObservation");
            writer.WriteRawValue(cut.ContactObservation.SerializeCanonical());
            CampaignObservationSerializer.WriteOwnElements(writer, [cut.OwnElement]);
            writer.WriteStartObject("sealOffer");
            writer.WriteString("kind", cut.SealOffer.Kind);
            writer.WriteString("role", cut.SealOffer.Role);
            writer.WriteString("elementId", cut.SealOffer.ElementId);
            writer.WriteString("componentId", cut.SealOffer.ComponentId);
            writer.WriteNumber("committedToe", cut.SealOffer.CommittedToe);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
