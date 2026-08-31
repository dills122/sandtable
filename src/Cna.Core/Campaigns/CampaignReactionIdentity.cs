using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignMovementEndedState
{
    public CampaignMovementEndedState(LandSequencePosition movementPosition)
    {
        CampaignSequenceV5Guards.RequireMaterializedMovement(movementPosition);
        SequenceContractVersion = movementPosition.ContractVersion;
        PositionId = movementPosition.PositionId;
        GameTurn = movementPosition.GameTurn;
        OperationStage = movementPosition.OperationStage;
        StageId = movementPosition.StageId;
        PhaseId = movementPosition.PhaseId;
        SegmentId = movementPosition.SegmentId!;
        PhasingSide = movementPosition.ActiveSide!.Value;
    }

    public int SequenceContractVersion { get; }

    public string PositionId { get; }

    public int GameTurn { get; }

    public int OperationStage { get; }

    public string StageId { get; }

    public string PhaseId { get; }

    public string SegmentId { get; }

    public LandSide PhasingSide { get; }
}

internal sealed record CampaignReactingPosition
{
    public CampaignReactingPosition(LandSequencePosition suspendedMovementPosition)
    {
        CampaignSequenceV5Guards.RequireMaterializedMovement(suspendedMovementPosition);
        SuspendedMovementPosition = suspendedMovementPosition;
        PhasingSide = suspendedMovementPosition.ActiveSide!.Value;
        ReactingSide = PhasingSide == LandSide.Axis
            ? LandSide.Commonwealth
            : LandSide.Axis;
    }

    public LandSequencePosition SuspendedMovementPosition { get; }

    public LandSide PhasingSide { get; }

    public LandSide ReactingSide { get; }
}

internal sealed record CampaignReactionWindowId
{
    public CampaignReactionWindowId(string value)
    {
        Value = ContentContractGuards.RequireSha256(value, nameof(value));
    }

    public string Value { get; }
}

internal sealed record CampaignReactionOpportunityId
{
    public CampaignReactionOpportunityId(string value)
    {
        Value = ContentContractGuards.RequireSha256(value, nameof(value));
    }

    public string Value { get; }
}

internal static class CampaignReactionIdentity
{
    public static CampaignReactionWindowId CreateWindow(
        string campaignId,
        string rulesetHash,
        int moveContractVersion,
        long committedStateVersion,
        CampaignMapRepresentationState triggeringRepresentation,
        string originLocationId,
        string destinationLocationId,
        LandSide reactingSide)
    {
        campaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        if (!CampaignSnapshotValidator.IsRulesHash(rulesetHash))
        {
            throw new ArgumentException(
                "A ruleset hash must contain 64 lowercase hexadecimal digits.",
                nameof(rulesetHash));
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(moveContractVersion, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(committedStateVersion, 1);
        ArgumentNullException.ThrowIfNull(triggeringRepresentation);
        originLocationId = ContentContractGuards.RequireStableId(
            originLocationId,
            nameof(originLocationId));
        destinationLocationId = ContentContractGuards.RequireStableId(
            destinationLocationId,
            nameof(destinationLocationId));
        if (string.Equals(originLocationId, destinationLocationId, StringComparison.Ordinal))
        {
            throw new ArgumentException("A triggering move must change location.", nameof(destinationLocationId));
        }

        if (!string.Equals(
            triggeringRepresentation.CurrentLocationId,
            destinationLocationId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The authoritative triggering representation must occupy the committed destination.",
                nameof(triggeringRepresentation));
        }

        if (!Enum.IsDefined(reactingSide))
        {
            throw new ArgumentOutOfRangeException(nameof(reactingSide));
        }

        return new CampaignReactionWindowId(Hash(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("domain", "sandtable.campaign.reaction-window.v1");
            writer.WriteString("campaignId", campaignId);
            writer.WriteString("rulesetHash", rulesetHash);
            writer.WriteNumber("moveContractVersion", moveContractVersion);
            writer.WriteNumber("committedStateVersion", committedStateVersion);
            WriteRepresentation(writer, "triggeringRepresentation", triggeringRepresentation);
            writer.WriteString("originLocationId", originLocationId);
            writer.WriteString("destinationLocationId", destinationLocationId);
            writer.WriteString("reactingSide", FormatSide(reactingSide));
            writer.WriteEndObject();
        }));
    }

    public static CampaignReactionOpportunityId CreateOpportunity(
        CampaignReactionWindowId windowId,
        CampaignMapRepresentationState reactingRepresentation)
    {
        ArgumentNullException.ThrowIfNull(windowId);
        ArgumentNullException.ThrowIfNull(reactingRepresentation);
        return new CampaignReactionOpportunityId(Hash(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("domain", "sandtable.campaign.reaction-opportunity.v1");
            writer.WriteString("windowId", windowId.Value);
            WriteRepresentation(writer, "reactingRepresentation", reactingRepresentation);
            writer.WriteEndObject();
        }));
    }

    private static string Hash(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            write(writer);
        }

        return $"sha256:{Convert.ToHexString(SHA256.HashData(buffer.WrittenSpan)).ToLowerInvariant()}";
    }

    private static void WriteRepresentation(
        Utf8JsonWriter writer,
        string propertyName,
        CampaignMapRepresentationState representation)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteString("representationId", representation.RepresentationId);
        writer.WriteString("currentLocationId", representation.CurrentLocationId);
        writer.WriteString("bindingKind", representation.BindingKind switch
        {
            CampaignMapRepresentationBindingKind.IndependentElement => "independent-element",
            _ => throw new ArgumentOutOfRangeException(nameof(representation)),
        });
        writer.WriteStartArray("boundElementIds");
        foreach (var elementId in representation.BoundElementIds.Order(StringComparer.Ordinal))
        {
            writer.WriteStringValue(elementId);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static string FormatSide(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };
}

internal static class CampaignSequenceV5Guards
{
    public static void RequireMaterializedMovement(LandSequencePosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        var catalogPosition = Cna1979LandSequence.CreateTurn(position.GameTurn)
            .SingleOrDefault(candidate => string.Equals(
                candidate.PositionId,
                position.PositionId,
                StringComparison.Ordinal));
        if (catalogPosition is null
            || position.ContractVersion != Cna1979LandSequence.ContractVersion
            || position.StageId != LandStageIds.Operation
            || position.PhaseId != LandPhaseIds.MovementAndCombat
            || position.SegmentId != LandSegmentIds.Movement
            || position.StepId is not null
            || position.ActorRole is not (LandActorRole.FirstActingSide
                or LandActorRole.SecondActingSide)
            || position.ActiveSide is null
            || catalogPosition.ContractVersion != position.ContractVersion
            || catalogPosition.GameTurn != position.GameTurn
            || catalogPosition.OperationStage != position.OperationStage
            || catalogPosition.StageId != position.StageId
            || catalogPosition.PhaseId != position.PhaseId
            || catalogPosition.SegmentId != position.SegmentId
            || catalogPosition.StepId != position.StepId
            || catalogPosition.ActorRole != position.ActorRole
            || !catalogPosition.Sources.SequenceEqual(position.Sources))
        {
            throw new ArgumentException(
                "The position must be an exact materialized contract-2 Movement segment.",
                nameof(position));
        }
    }
}
