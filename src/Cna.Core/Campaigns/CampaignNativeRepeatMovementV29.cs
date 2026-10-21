using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// A normal subsequent-cycle Movement action. Its offer is projected from the
// owner's same-cut contacts; its World effect is independently recalculated
// from the admitted content and the retained Reserve Release allowance.
internal sealed record CampaignNativeRepeatMovementV29(
    int ContractVersion, string CampaignId, long PriorStateVersion,
    string PriorEventHash, int CycleOrdinal, string PositionId,
    string ActionId, string ElementId, string RepresentationId,
    string OriginLocationId, string DestinationLocationId,
    string PriorWorldHash, string ResultWorldHash)
{
    internal const int CurrentContractVersion = 29;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeRepeatMovementV29 Create(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignNativeCycleControlV28 control,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        string actionId)
    {
        var (offer, world, representationId) = Project(
            release, control, context, contacts, actionId);
        return new(CurrentContractVersion, control.CampaignId, control.StateVersion,
            Hash(control.SerializeCanonical()), control.NextCycleOrdinal,
            control.NextPositionId, offer.ActionId, offer.ElementId,
            representationId, offer.OriginLocationId,
            offer.DestinationLocationId,
            CampaignNativeCombatPaidWorldV22.Hash(release.World),
            CampaignNativeCombatPaidWorldV22.Hash(world));
    }

    internal CampaignWorldSnapshotV6 ProjectWorld(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignNativeCycleControlV28 control,
        CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        var expected = Create(release, control, context, contacts, ActionId);
        if (this != expected)
            throw new InvalidCampaignHistoryException(
                "Repeated Movement differs from retained owner authority.");
        return Project(release, control, context, contacts, ActionId).World;
    }

    internal static CampaignNativeRepeatMovementV29 Replay(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignNativeCycleControlV28 control,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Create(release, control, context, contacts, parsed.ActionId);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException(
                "Repeated Movement differs from retained owner authority.");
        return expected;
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || CycleOrdinal < 2 || !IsHash(PriorEventHash) || !IsHash(ActionId)
            || !IsHash(PriorWorldHash) || !IsHash(ResultWorldHash))
            throw new JsonException("Invalid repeated Movement event.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "cycle-repeat-element-moved");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteNumber("cycleOrdinal", CycleOrdinal);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(
                PositionId, nameof(PositionId)));
            writer.WriteString("actionId", ActionId);
            writer.WriteString("elementId", ContentContractGuards.RequireStableId(
                ElementId, nameof(ElementId)));
            writer.WriteString("representationId", ContentContractGuards.RequireStableId(
                RepresentationId, nameof(RepresentationId)));
            writer.WriteString("originLocationId", ContentContractGuards.RequireStableId(
                OriginLocationId, nameof(OriginLocationId)));
            writer.WriteString("destinationLocationId", ContentContractGuards.RequireStableId(
                DestinationLocationId, nameof(DestinationLocationId)));
            writer.WriteString("priorWorldHash", PriorWorldHash);
            writer.WriteString("resultWorldHash", ResultWorldHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeRepeatMovementV29 Deserialize(
        ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            if (canonicalEvent.Length is < 1 or > 8192)
                throw new JsonException("Repeated Movement event exceeds its byte bound.");
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion",
                "eventType", "campaignId", "priorStateVersion", "priorEventHash",
                "cycleOrdinal", "positionId", "actionId", "elementId",
                "representationId", "originLocationId", "destinationLocationId",
                "priorWorldHash", "resultWorldHash");
            if (root.GetProperty("eventType").GetString()
                != "cycle-repeat-element-moved")
                throw new JsonException("Unknown repeated Movement event.");
            var parsed = new CampaignNativeRepeatMovementV29(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!,
                root.GetProperty("cycleOrdinal").GetInt32(),
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("actionId").GetString()!,
                root.GetProperty("elementId").GetString()!,
                root.GetProperty("representationId").GetString()!,
                root.GetProperty("originLocationId").GetString()!,
                root.GetProperty("destinationLocationId").GetString()!,
                root.GetProperty("priorWorldHash").GetString()!,
                root.GetProperty("resultWorldHash").GetString()!);
            if (!canonicalEvent.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical repeated Movement event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            throw new JsonException("Invalid repeated Movement event.", exception);
        }
    }

    private static (MoveElementAction Offer, CampaignWorldSnapshotV6 World,
        string RepresentationId) Project(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignNativeCycleControlV28 control,
        CampaignContentContext context,
        CampaignContactJournal contacts, string actionId)
    {
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        if (control.Choice != CampaignNativeCycleControlChoiceV28.Repeat
            || control.PriorStateVersion != release.StateVersion
            || control.CampaignId != release.CampaignId
            || control.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(release.World)
            || contacts.AuthorityVersion != control.StateVersion)
            throw new InvalidOperationException(
                "Repeated Movement requires the accepted same-cut repeat edge.");
        var offer = CampaignCycleContinuationMovementWitnessV28.OffersAfterRepeat(
            release, control, context, contacts).SingleOrDefault(candidate =>
            candidate.ActionId == actionId)
            ?? throw new InvalidOperationException(
                "Repeated move is absent from the owner-visible contact offer.");
        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException("Repeated Movement needs normal Content 6.");
        var definition = artifact.Definition.LegacyDefinition;
        var content = definition.Elements.Single(value =>
            value.ElementId == offer.ElementId
            && value.SideId == CampaignSnapshotSerializer.FormatSide(release.ActingSide)
            && value.PlacementMode == ContentPlacementMode.Independent);
        if (content.BreakdownVehicleCohort is not null)
            throw new InvalidOperationException(
                "Vehicle breakdown movement needs its native accounting stage.");
        var allowance = release.NextMovement.Single(value =>
            value.ElementId == offer.ElementId
            && value.NextCycleOrdinal == control.NextCycleOrdinal);
        var element = release.World.Elements.Single(value =>
            value.ElementId == offer.ElementId
            && value.CurrentLocationId == offer.OriginLocationId);
        var representation = release.World.Representations.Single(value =>
            value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
            && value.BoundElementIds.Count == 1
            && value.BoundElementIds[0] == offer.ElementId
            && value.CurrentLocationId == offer.OriginLocationId);
        var edge = definition.Edges.Single(value =>
            value.FirstLocationId == offer.OriginLocationId
                && value.SecondLocationId == offer.DestinationLocationId
            || value.SecondLocationId == offer.OriginLocationId
                && value.FirstLocationId == offer.DestinationLocationId);
        var destination = definition.Locations.Single(value =>
            value.LocationId == offer.DestinationLocationId);
        var movement = CampaignElementMovedV3Factory.CalculateMovement(
            release.World, definition, content, element, edge, destination,
            offer.OriginLocationId, offer.DestinationLocationId);
        if (movement.ExpendedAfter > new CapabilityPointAmount(
                allowance.CapabilityPointCeiling, 1)
            || new MoveElementAction(offer.ElementId, offer.OriginLocationId,
                offer.DestinationLocationId, movement.ActionCost).ActionId != actionId)
            throw new InvalidOperationException(
                "Repeated Movement exceeds its release ceiling or differs from visible cost.");
        var movedRepresentation = new CampaignMapRepresentationState(
            representation.RepresentationId, offer.DestinationLocationId,
            representation.BindingKind, representation.BoundElementIds);
        var world = CampaignElementMovedV3Factory.ProjectMoveForAuthority(
            release.World, element, movedRepresentation,
            movement.ExpendedAfter, [], null);
        if (!CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario))
            throw new InvalidOperationException(
                "Repeated Movement would leave an invalid World.");
        return (offer, world, representation.RepresentationId);
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9')
            or (>= 'a' and <= 'f'));
}
