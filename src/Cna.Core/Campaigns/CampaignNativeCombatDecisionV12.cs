using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// This is the first native Combat authority transition. Selected assault binds a
// defender privately; voluntary finish carries no target. Public action identity
// contains only owner-visible data.
internal record CampaignNativeCombatDecisionV12(
    int ContractVersion,
    string CampaignId,
    long PriorStateVersion,
    string PositionId,
    string NextPositionId,
    string Choice,
    LandSide Side,
    string ActingElementId,
    string? ContactHandle,
    string? ApparentLocationId,
    string? BoundRepresentationId,
    string ActionId)
{
    public const int CurrentContractVersion = 12;

    public virtual byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !Enum.IsDefined(Side))
            throw new JsonException("Invalid native Combat selection event.");
        _ = ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId));
        _ = ContentContractGuards.RequireStableId(PositionId, nameof(PositionId));
        _ = ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId));
        _ = ContentContractGuards.RequireStableId(ActingElementId, nameof(ActingElementId));
        if (Choice is not ("select-contact-assault" or "finish-combat-without-attack"))
            throw new JsonException("Invalid native Combat choice.");
        if (Choice == "select-contact-assault")
        {
            _ = ContentContractGuards.RequireStableId(ContactHandle!, nameof(ContactHandle));
            _ = ContentContractGuards.RequireStableId(ApparentLocationId!, nameof(ApparentLocationId));
            _ = ContentContractGuards.RequireStableId(BoundRepresentationId!, nameof(BoundRepresentationId));
        }
        else if (ContactHandle is not null || ApparentLocationId is not null
            || BoundRepresentationId is not null)
            throw new JsonException("A voluntary finish has no target binding.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-selection-decided");
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("positionId", PositionId);
            writer.WriteString("nextPositionId", NextPositionId);
            writer.WriteString("choice", Choice);
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

    public static CampaignNativeCombatDecisionV12 Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            if (root.GetProperty("eventType").GetString() == "combat-selection-timeout")
                return new CampaignNativeCombatSelectionTimeoutV38(CampaignSelectionTimeoutEvidenceV38.Deserialize(canonicalJson));
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "positionId", "nextPositionId", "choice", "side", "actingElementId",
                "contactHandle", "apparentLocationId", "boundRepresentationId", "actionId");
            if (root.GetProperty("eventType").GetString() != "combat-selection-decided")
                throw new JsonException("Unknown native Combat event.");
            var selected = new CampaignNativeCombatDecisionV12(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("choice").GetString()!,
                CampaignSnapshotSerializer.ParseSide(root.GetProperty("side").GetString()),
                root.GetProperty("actingElementId").GetString()!,
                root.GetProperty("contactHandle").GetString(),
                root.GetProperty("apparentLocationId").GetString(),
                root.GetProperty("boundRepresentationId").GetString(),
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

internal sealed record CampaignNativeCombatDecisionStateV12(
    CampaignSnapshotV11 PriorSnapshot,
    CampaignNativeCombatDecisionV12 Decision)
{
    public const int CurrentContractVersion = 12;
    public long StateVersion => checked(PriorSnapshot.StateVersion + 1);
    public string CampaignId => PriorSnapshot.CampaignId;

    public static CampaignNativeCombatDecisionStateV12 Select(CampaignSnapshotV11 prior,
        CampaignContentContext context, CampaignContactJournal contacts, LandSide side,
        string actingElementId, string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        var cut = CampaignContactCombatSelectionCut.Project(prior, context, contacts,
            side, actingElementId);
        var offer = cut.AssaultOffers.SingleOrDefault(candidate => candidate.ActionId == actionId);
        var finish = cut.FinishOffer?.ActionId == actionId;
        if (offer is null && !finish)
            throw new InvalidOperationException("Combat selection is not offered at this owner cut.");
        var binding = offer is null ? null : contacts.ResolvePrivateTarget(side,
            cut.OwnerCut.ContactObservation.ActingFormationId, offer.ContactHandle);
        var nextPosition = Cna1979LandSequenceV4.GetNext(
            prior.CurrentPosition.SequencePosition!);
        if (nextPosition.StepId != LandStepIds.Barrage
            || nextPosition.SegmentId != LandSegmentIds.Combat)
            throw new InvalidOperationException("Combat selection must advance to Barrage.");
        var selected = new CampaignNativeCombatDecisionV12(
            CampaignNativeCombatDecisionV12.CurrentContractVersion, prior.CampaignId,
            prior.StateVersion, prior.CurrentPosition.SequenceContext.PositionId,
            nextPosition.PositionId,
            finish ? "finish-combat-without-attack" : "select-contact-assault",
            side, actingElementId, offer?.ContactHandle, offer?.ApparentLocationId,
            binding, actionId);
        _ = selected.SerializeCanonical();
        return new(prior, selected);
    }

    public static CampaignNativeCombatDecisionStateV12 Replay(CampaignSnapshotV11 prior,
        CampaignContentContext context, CampaignContactJournal contacts,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var selected = CampaignNativeCombatDecisionV12.Deserialize(canonicalEvent);
        var expected = selected is CampaignNativeCombatSelectionTimeoutV38 timeout
            ? new CampaignNativeCombatDecisionStateV12(prior, CampaignNativeCombatSelectionTimeoutV38.Create(prior, context, contacts,
                timeout.Evidence.ConfigurationHash, timeout.Evidence.OpenedAtUnixMilliseconds, timeout.Evidence.DeadlineUnixMilliseconds,
                timeout.Evidence.HighWaterUnixMilliseconds, timeout.Evidence.CancelledAtUnixMilliseconds, timeout.Evidence.Reason))
            : Select(prior, context, contacts, selected.Side, selected.ActingElementId, selected.ActionId);
        if (!canonicalEvent.Span.SequenceEqual(expected.Decision.SerializeCanonical()))
            throw new InvalidCampaignHistoryException("Native Combat selection differs from the authority cut.");
        return expected;
    }

    public byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", Decision.ContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WritePropertyName("priorSnapshot");
            writer.WriteRawValue(CampaignSnapshotV11Serializer.Serialize(PriorSnapshot));
            writer.WritePropertyName("decision");
            writer.WriteRawValue(Decision.SerializeCanonical());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignNativeCombatDecisionStateV12 DeserializeCanonical(
        ReadOnlyMemory<byte> canonicalJson, CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "campaignId",
                "stateVersion", "priorSnapshot", "decision");
            if (root.GetProperty("contractVersion").GetInt32() is not (CurrentContractVersion or 38))
                throw new JsonException("Unsupported native Combat snapshot contract.");
            var priorBytes = JsonSerializer.SerializeToUtf8Bytes(root.GetProperty("priorSnapshot"));
            var prior = CampaignSnapshotV11Serializer.Deserialize(priorBytes);
            var eventBytes = JsonSerializer.SerializeToUtf8Bytes(root.GetProperty("decision"));
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
