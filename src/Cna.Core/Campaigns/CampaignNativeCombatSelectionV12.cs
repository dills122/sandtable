using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// This is the first native Combat authority transition. The defender binding stays
// in the private event; public action identity contains only contact-visible data.
internal sealed record CampaignNativeCombatSelectedV12(
    int ContractVersion,
    string CampaignId,
    long PriorStateVersion,
    string PositionId,
    LandSide Side,
    string ActingElementId,
    string ContactHandle,
    string ApparentLocationId,
    string BoundRepresentationId,
    string ActionId)
{
    public const int CurrentContractVersion = 12;

    public byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !Enum.IsDefined(Side))
            throw new JsonException("Invalid native Combat selection event.");
        _ = ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId));
        _ = ContentContractGuards.RequireStableId(PositionId, nameof(PositionId));
        _ = ContentContractGuards.RequireStableId(ActingElementId, nameof(ActingElementId));
        _ = ContentContractGuards.RequireStableId(ContactHandle, nameof(ContactHandle));
        _ = ContentContractGuards.RequireStableId(ApparentLocationId, nameof(ApparentLocationId));
        _ = ContentContractGuards.RequireStableId(BoundRepresentationId, nameof(BoundRepresentationId));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "contact-assault-selected");
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("positionId", PositionId);
            writer.WriteString("side", CampaignSnapshotSerializer.FormatSide(Side));
            writer.WriteString("actingElementId", ActingElementId);
            writer.WriteString("contactHandle", ContactHandle);
            writer.WriteString("apparentLocationId", ApparentLocationId);
            writer.WriteString("boundRepresentationId", BoundRepresentationId);
            writer.WriteString("actionId", ActionId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignNativeCombatSelectedV12 Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "positionId", "side", "actingElementId",
                "contactHandle", "apparentLocationId", "boundRepresentationId", "actionId");
            if (root.GetProperty("eventType").GetString() != "contact-assault-selected")
                throw new JsonException("Unknown native Combat event.");
            var selected = new CampaignNativeCombatSelectedV12(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("positionId").GetString()!,
                CampaignSnapshotSerializer.ParseSide(root.GetProperty("side").GetString()),
                root.GetProperty("actingElementId").GetString()!,
                root.GetProperty("contactHandle").GetString()!,
                root.GetProperty("apparentLocationId").GetString()!,
                root.GetProperty("boundRepresentationId").GetString()!,
                root.GetProperty("actionId").GetString()!);
            if (!canonicalJson.Span.SequenceEqual(selected.SerializeCanonical()))
                throw new JsonException("Native Combat selection event is not canonical.");
            return selected;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException)
        {
            throw new JsonException("Invalid native Combat selection event.", exception);
        }
    }
}

internal sealed record CampaignNativeCombatSelectionStateV12(
    CampaignSnapshotV11 PriorSnapshot,
    CampaignNativeCombatSelectedV12 Selection)
{
    public const int CurrentContractVersion = 12;
    public long StateVersion => checked(PriorSnapshot.StateVersion + 1);
    public string CampaignId => PriorSnapshot.CampaignId;

    public static CampaignNativeCombatSelectionStateV12 Select(CampaignSnapshotV11 prior,
        CampaignContentContext context, CampaignContactJournal contacts, LandSide side,
        string actingElementId, string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        var cut = CampaignContactCombatSelectionCut.Project(prior, context, contacts,
            side, actingElementId);
        var offer = cut.AssaultOffers.SingleOrDefault(candidate => candidate.ActionId == actionId)
            ?? throw new InvalidOperationException("Combat selection is not offered at this owner cut.");
        var binding = contacts.ResolvePrivateTarget(side,
            cut.OwnerCut.ContactObservation.ActingFormationId, offer.ContactHandle);
        var selected = new CampaignNativeCombatSelectedV12(
            CampaignNativeCombatSelectedV12.CurrentContractVersion, prior.CampaignId,
            prior.StateVersion, prior.CurrentPosition.SequenceContext.PositionId,
            side, actingElementId, offer.ContactHandle, offer.ApparentLocationId,
            binding, offer.ActionId);
        _ = selected.SerializeCanonical();
        return new(prior, selected);
    }

    public static CampaignNativeCombatSelectionStateV12 Replay(CampaignSnapshotV11 prior,
        CampaignContentContext context, CampaignContactJournal contacts,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var selected = CampaignNativeCombatSelectedV12.Deserialize(canonicalEvent);
        var expected = Select(prior, context, contacts, selected.Side,
            selected.ActingElementId, selected.ActionId);
        if (!canonicalEvent.Span.SequenceEqual(expected.Selection.SerializeCanonical()))
            throw new InvalidCampaignHistoryException("Native Combat selection differs from the authority cut.");
        return expected;
    }

    public byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WritePropertyName("priorSnapshot");
            writer.WriteRawValue(CampaignSnapshotV11Serializer.Serialize(PriorSnapshot));
            writer.WritePropertyName("selection");
            writer.WriteRawValue(Selection.SerializeCanonical());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignNativeCombatSelectionStateV12 DeserializeCanonical(
        ReadOnlyMemory<byte> canonicalJson, CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "campaignId",
                "stateVersion", "priorSnapshot", "selection");
            if (root.GetProperty("contractVersion").GetInt32() != CurrentContractVersion)
                throw new JsonException("Unsupported native Combat snapshot contract.");
            var priorBytes = JsonSerializer.SerializeToUtf8Bytes(root.GetProperty("priorSnapshot"));
            var prior = CampaignSnapshotV11Serializer.Deserialize(priorBytes);
            var eventBytes = JsonSerializer.SerializeToUtf8Bytes(root.GetProperty("selection"));
            var state = Replay(prior, context, contacts, eventBytes);
            if (root.GetProperty("campaignId").GetString() != state.CampaignId
                || root.GetProperty("stateVersion").GetInt64() != state.StateVersion
                || !canonicalJson.Span.SequenceEqual(state.SerializeCanonical()))
                throw new JsonException("Native Combat snapshot differs from replayed authority.");
            return state;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or InvalidCampaignHistoryException or KeyNotFoundException or FormatException)
        {
            throw new JsonException("Invalid native Combat snapshot.", exception);
        }
    }
}
