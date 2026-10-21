using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

internal sealed record CampaignContactObservationV1(
    int ContractVersion,
    string CampaignId,
    LandSide Side,
    string ActingFormationId,
    long AudienceRevision,
    IReadOnlyList<CampaignContactView> Contacts,
    IReadOnlyList<CampaignContactTargetCandidate> CurrentTargets)
{
    public const int CurrentContractVersion = 1;

    public static CampaignContactObservationV1 Project(
        CampaignContactKnowledge knowledge, string actingFormationId)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        return new CampaignContactObservationV1(CurrentContractVersion, knowledge.CampaignId,
            knowledge.Side, actingFormationId, knowledge.AudienceRevisionFor(actingFormationId),
            knowledge.ObserveFor(actingFormationId), knowledge.CurrentTargetsFor(actingFormationId));
    }

    // The outward contract has no authority version, source event, or private target binding.
    public byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || !Enum.IsDefined(Side)
            || string.IsNullOrWhiteSpace(CampaignId)
            || string.IsNullOrWhiteSpace(ActingFormationId)
            || AudienceRevision < 0)
            throw new InvalidOperationException("Invalid contact observation contract.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteString("side", Side == LandSide.Axis ? "axis" : "commonwealth");
            writer.WriteString("actingFormationId", ActingFormationId);
            writer.WriteNumber("audienceRevision", AudienceRevision);
            writer.WriteStartArray("contacts");
            foreach (var contact in Contacts)
            {
                writer.WriteStartObject();
                writer.WriteString("handle", contact.Handle);
                writer.WriteString("observerFormationId", contact.ObserverFormationId);
                writer.WriteString("apparentLocationId", contact.ApparentLocationId);
                writer.WriteString("identification", contact.Identification switch
                {
                    CampaignContactIdentification.Presence => "presence",
                    CampaignContactIdentification.FormationType => "formation-type",
                    CampaignContactIdentification.Identified => "identified",
                    _ => throw new InvalidOperationException("Unknown contact identification."),
                });
                writer.WriteString("access", contact.Access == CampaignContactAccess.Side ? "side" : "observer");
                writer.WriteBoolean("isCurrent", contact.IsCurrent);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteStartArray("currentTargets");
            foreach (var target in CurrentTargets)
            {
                writer.WriteStartObject();
                writer.WriteString("actingFormationId", target.ActingFormationId);
                writer.WriteString("contactHandle", target.ContactHandle);
                writer.WriteString("apparentLocationId", target.ApparentLocationId);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }
}
