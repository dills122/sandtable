using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

// A counter is its own observer identity; it is not an invented formation.
// Projection requires the actual active source representation and owner side.
internal sealed record CampaignIndependentContactObservationV2(string CampaignId, LandSide Side,
    string ObserverCounterId, long AudienceRevision, IReadOnlyList<CampaignIndependentContactView> Contacts)
{
    internal static CampaignIndependentContactObservationV2 Project(CampaignSourcePlayableWorld world,
        CampaignContactKnowledge knowledge, string counterId)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentException.ThrowIfNullOrWhiteSpace(counterId);
        if (knowledge.CampaignId != world.Source.Request.CampaignId)
            throw new InvalidOperationException("Counter observation differs from the source campaign.");
        var side = knowledge.Side == LandSide.Axis ? "axis" : "commonwealth";
        var observer = CampaignSourcePolishArrivalContacts.EffectiveCounters(world).SingleOrDefault(value =>
            value.RepresentationId == counterId && value.SideId == side
            && value.ObserverKind == CampaignContactObserverKind.IndependentCounter
            && value.ObserverFormationId == counterId);
        if (observer is null) throw new InvalidOperationException("Counter observer is unavailable to this side.");
        return new(knowledge.CampaignId, knowledge.Side, counterId,
            knowledge.AudienceRevisionForCounter(counterId), knowledge.ObserveForCounter(counterId));
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 2);
            writer.WriteString("campaignId", CampaignId); writer.WriteString("side", Side == LandSide.Axis ? "axis" : "commonwealth");
            writer.WriteString("observerKind", "independent-counter"); writer.WriteString("observerId", ObserverCounterId);
            writer.WriteNumber("audienceRevision", AudienceRevision); writer.WriteStartArray("contacts");
            foreach (var contact in Contacts)
            {
                writer.WriteStartObject(); writer.WriteString("handle", contact.Handle);
                writer.WriteString("apparentLocationId", contact.ApparentLocationId);
                writer.WriteString("identification", CampaignContactTransitionCodec.Identification(contact.Identification));
                writer.WriteBoolean("isCurrent", contact.IsCurrent); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
