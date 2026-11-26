using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

internal sealed record CampaignNativeSecondMoveV44(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    LandSide Side, string PositionId, string ActionId, string ElementId, string RepresentationId,
    string OriginLocationId, string DestinationLocationId, string PriorWorldHash, string ResultWorldHash,
    CampaignReactionWindow? ReactionWindow)
{
    internal const int CurrentContractVersion = 44;
    internal long StateVersion => checked(PriorStateVersion + 1);

    // E4b (#655): keep the initial-turn Stage 1 second-side contract unchanged,
    // but refuse any other native move that would enter an unsupported Reaction.
    // The detached window already binds the actual turn/stage movement position.
    internal bool RequiresUnsupportedReaction(int initialGameTurn) =>
        ReactionWindow is { ReactingPosition.SuspendedMovementPosition: var position }
        && (position.GameTurn != initialGameTurn || position.OperationStage != 1
            || position.ActorRole != LandActorRole.SecondActingSide);

    internal static string ActionIdFor(MoveElementAction offer) => "sha256:" + Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes("sandtable.second-move-and-stop.v1\0" + offer.ActionId)));

    internal static (object Predecessor, CampaignWorldSnapshotV6 World, LandSequencePosition Position)
        Source(object predecessor, CampaignWorldSnapshotV6 world, CampaignContentContext context,
            IReadOnlyList<CampaignOperationStageOrder> orders, LandSide side, int gameTurn, long contactVersion)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);
        var (version, priorSide, positionId, worldHash) = predecessor switch
        {
            CampaignNativeSecondReserveV43 reserve when reserve.Completed =>
                (reserve.StateVersion, reserve.Side, reserve.NextPositionId, reserve.ResultWorldHash),
            CampaignNativeSecondMoveV44 move when move.ReactionWindow is null =>
                (move.StateVersion, move.Side, move.PositionId, move.ResultWorldHash),
            _ => throw new InvalidOperationException("Second Movement requires a completed Reserve or unblocked move."),
        };
        var catalog = Cna1979LandSequenceV4.CreateTurn(gameTurn).Single(value => value.PositionId == positionId);
        var order = orders.Single(value => value.GameTurn == gameTurn && value.OperationStage == catalog.OperationStage);
        // MTS-A (2026-11-22): generalized, same reasoning as
        // CampaignNativeSecondReserveV43.Create.
        var actingRole = order.ActingRole(side);
        if (priorSide != side || actingRole is null || contactVersion != version
            || catalog.ActorRole != actingRole || catalog.SegmentId != LandSegmentIds.Movement
            || worldHash != CampaignNativeCombatPaidWorldV22.Hash(world)
            || context.ArtifactV6 is not { } artifact || !CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario))
            throw new InvalidOperationException("Second Movement does not bind the owner and World.");
        var position = new LandSequencePosition(catalog.ContractVersion, catalog.PositionId, catalog.GameTurn,
            catalog.OperationStage, catalog.StageId, catalog.PhaseId, catalog.SegmentId, catalog.StepId,
            catalog.ActorRole, side, catalog.Sources);
        Cna1979LandSequenceV4.RequireMaterializedMovement(position);
        return (predecessor, world, position);
    }

    internal static (CampaignNativeSecondMoveV44 Event, CampaignWorldSnapshotV6 World) Create(
        object predecessor, CampaignWorldSnapshotV6 world, CampaignContentContext context,
        IReadOnlyList<CampaignOperationStageOrder> orders, CampaignContactJournal contacts,
        LandSide side, int gameTurn, string elementId, string actionId, string rulesetHash)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        var (_, _, position) = Source(predecessor, world, context, orders, side, gameTurn, contacts.AuthorityVersion);
        var previousCampaign = predecessor switch
        {
            CampaignNativeSecondReserveV43 reserve => reserve.CampaignId,
            CampaignNativeSecondMoveV44 move => move.CampaignId,
            _ => throw new InvalidOperationException("Second Movement predecessor is unsupported."),
        };
        if (contacts.CampaignId != previousCampaign)
            throw new InvalidOperationException("Second Movement contacts belong to another campaign.");
        var cut = CampaignContactOwnerMovementCut.ProjectSecondMovement(world, position, context, contacts, side, elementId);
        var offer = cut.MovementOffers.SingleOrDefault(value => ActionIdFor(value) == actionId)
            ?? throw new InvalidOperationException("Second Movement choice is absent from the owner cut.");
        var artifact = context.ArtifactV6!;
        var definition = artifact.Definition.LegacyDefinition;
        var content = definition.Elements.Single(value => value.ElementId == elementId);
        var state = world.Elements.Single(value => value.ElementId == elementId);
        var representation = world.Representations.Single(value => value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
            && value.BoundElementIds.Count == 1 && value.BoundElementIds[0] == elementId);
        var opponent = side == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
        var opponentId = CampaignSnapshotSerializer.FormatSide(opponent);
        var contents = definition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (world.Representations.Any(value => value.CurrentLocationId == offer.DestinationLocationId
            && value.BoundElementIds.Any(id => contents[id].SideId == opponentId)))
            throw new InvalidOperationException("Second Movement destination is occupied.");
        var edge = CampaignElementMovedV3Factory.FindEdge(definition, offer.OriginLocationId, offer.DestinationLocationId)
            ?? throw new InvalidOperationException("Second Movement destination is not adjacent.");
        var destination = definition.Locations.Single(value => value.LocationId == offer.DestinationLocationId);
        var movement = CampaignElementMovedV3Factory.CalculateMovement(world, definition, content, state, edge,
            destination, offer.OriginLocationId, offer.DestinationLocationId);
        if (new MoveElementAction(elementId, offer.OriginLocationId, offer.DestinationLocationId,
                movement.ActionCost).ActionId != offer.ActionId)
            throw new InvalidOperationException("Second Movement differs from the visible cost.");
        var moved = new CampaignMapRepresentationState(representation.RepresentationId, offer.DestinationLocationId,
            representation.BindingKind, representation.BoundElementIds);
        var result = CampaignElementMovedV3Factory.ProjectMoveForAuthority(world, state, moved, movement.ExpendedAfter,
            [], CampaignMovementEndedState.CreateForBreakdown(position));
        var enemyControl = CampaignElementMovedV3Factory.DeriveControlledLocationIds(result, artifact, context.Scenario, opponent);
        if (enemyControl.Contains(offer.OriginLocationId, StringComparer.Ordinal)
            && enemyControl.Contains(offer.DestinationLocationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Second Movement cannot cross between opposing ZOCs.");
        var version = contacts.AuthorityVersion;
        var input = new ElementMovedV3ReplayInput(contacts.CampaignId, version, position.PositionId, side,
            elementId, offer.OriginLocationId, offer.DestinationLocationId);
        var window = CampaignElementMovedV3Factory.CreateWindowForMovement(contacts.CampaignId, rulesetHash,
            version, position, artifact, context.Scenario, input, moved, result, opponent, CurrentContractVersion);
        window?.ValidateIdentities(contacts.CampaignId, rulesetHash);
        if (!CampaignWorldV6Validator.IsValid(result, artifact, context.Scenario))
            throw new InvalidOperationException("Second Movement would leave invalid World authority.");
        var priorBytes = CampaignCurrentEventSerializer.Serialize(predecessor);
        return (new(CurrentContractVersion, contacts.CampaignId, version,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(priorBytes)), side, position.PositionId,
            actionId, elementId, representation.RepresentationId, offer.OriginLocationId, offer.DestinationLocationId,
            CampaignNativeCombatPaidWorldV22.Hash(world), CampaignNativeCombatPaidWorldV22.Hash(result), window), result);
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion is < 1 or long.MaxValue || !Enum.IsDefined(Side)
            || !IsHash(PriorEventHash) || !IsHash(ActionId) || !IsHash(PriorWorldHash) || !IsHash(ResultWorldHash))
            throw new JsonException("Invalid second Movement event.");
        if (ReactionWindow is { } window
            && (window.TriggerAuthority.MoveContractVersion != CurrentContractVersion
                || window.TriggerCommittedStateVersion != StateVersion || window.PhasingSide != Side
                || window.ReactingPosition.SuspendedMovementPosition.PositionId != PositionId
                || window.TriggerAuthority.ElementId != ElementId
                || window.TriggerAuthority.TriggeringRepresentation.RepresentationId != RepresentationId
                || window.TriggerAuthority.OriginLocationId != OriginLocationId
                || window.TriggerAuthority.DestinationLocationId != DestinationLocationId))
            throw new JsonException("Reaction window does not bind its triggering second Movement.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "second-element-moved-and-stopped");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteString("side", CampaignSnapshotSerializer.FormatSide(Side));
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("actionId", ActionId);
            writer.WriteString("elementId", ContentContractGuards.RequireStableId(ElementId, nameof(ElementId)));
            writer.WriteString("representationId", ContentContractGuards.RequireStableId(RepresentationId, nameof(RepresentationId)));
            writer.WriteString("originLocationId", ContentContractGuards.RequireStableId(OriginLocationId, nameof(OriginLocationId)));
            writer.WriteString("destinationLocationId", ContentContractGuards.RequireStableId(DestinationLocationId, nameof(DestinationLocationId)));
            writer.WriteString("priorWorldHash", PriorWorldHash);
            writer.WriteString("resultWorldHash", ResultWorldHash);
            CampaignV11CanonicalCodec.WriteReactionWindow(writer, "reactionWindow", ReactionWindow);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeSecondMoveV44 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 65536) throw new JsonException("Invalid event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId", "priorStateVersion",
                "priorEventHash", "side", "positionId", "actionId", "elementId", "representationId", "originLocationId",
                "destinationLocationId", "priorWorldHash", "resultWorldHash", "reactionWindow");
            if (root.GetProperty("eventType").GetString() != "second-element-moved-and-stopped") throw new JsonException("Invalid event type.");
            var side = root.GetProperty("side").GetString() switch
            {
                "axis" => LandSide.Axis,
                "commonwealth" => LandSide.Commonwealth,
                _ => throw new JsonException("Invalid side."),
            };
            var parsed = new CampaignNativeSecondMoveV44(root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!, root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!, side, root.GetProperty("positionId").GetString()!,
                root.GetProperty("actionId").GetString()!, root.GetProperty("elementId").GetString()!,
                root.GetProperty("representationId").GetString()!, root.GetProperty("originLocationId").GetString()!,
                root.GetProperty("destinationLocationId").GetString()!, root.GetProperty("priorWorldHash").GetString()!,
                root.GetProperty("resultWorldHash").GetString()!, CampaignV11CanonicalCodec.ParseReactionWindow(
                    root.GetProperty("reactionWindow"), allowSecondMovement: true));
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical())) throw new JsonException("Noncanonical event.");
            return parsed;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        { throw new JsonException("Invalid second Movement event.", error); }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
