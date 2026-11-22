using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignNativeSecondReserveV43(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    LandSide Side, string PositionId, string NextPositionId, string? ElementId,
    string PriorWorldHash, string ResultWorldHash)
{
    internal const int CurrentContractVersion = 43;
    internal long StateVersion => checked(PriorStateVersion + 1);
    internal bool Completed => ElementId is null;

    internal static (CampaignNativeSecondReserveV43 Event, CampaignWorldSnapshotV6 World) Create(
        object predecessor, CampaignWorldSnapshotV6 world, CampaignContentContext context,
        IReadOnlyList<CampaignOperationStageOrder> orders, LandSide side, string? elementId,
        int gameTurn, long contactVersion)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(orders);
        var (campaignId, version, positionId, expectedWorldHash, priorBytes) = predecessor switch
        {
            CampaignNativePostCombatPassV42 phase => (phase.CampaignId, phase.StateVersion,
                phase.NextPositionId, phase.WorldHash, phase.SerializeCanonical()),
            CampaignNativeSecondReserveV43 reserve when !reserve.Completed => (reserve.CampaignId, reserve.StateVersion,
                reserve.NextPositionId, reserve.ResultWorldHash, reserve.SerializeCanonical()),
            // MTS-TASK-002 (2026-11-22, docs/work/plans/2026-11-22-stage-entry-bridge-design-options.md):
            // the terminal event of the new stage's six-event native preamble
            // chain -- additive, does not change the two arms above. The
            // caller is responsible for including this predecessor's own
            // CampaignNativeNoObligationFleetRepairV54.ToOrder() in `orders`
            // before calling Create, per the owner's ruling that the resolved
            // order is self-contained on this terminal event rather than a
            // dedicated ExerciseSession field.
            CampaignNativeNoObligationFleetRepairV54 fleetRepair => (fleetRepair.CampaignId, fleetRepair.StateVersion,
                fleetRepair.NextPositionId, fleetRepair.WorldHash, fleetRepair.SerializeCanonical()),
            _ => throw new InvalidOperationException("Second Reserve requires an accepted predecessor."),
        };
        var position = Cna1979LandSequenceV4.CreateTurn(gameTurn).Single(value => value.PositionId == positionId);
        var order = orders.Single(value => value.GameTurn == position.GameTurn && value.OperationStage == position.OperationStage);
        var priorHash = CampaignNativeCombatPaidWorldV22.Hash(world);
        // MTS-A (2026-11-22): generalized from a hardcoded SecondActingSide/
        // order.SecondSide pairing to CampaignOperationStageOrder.ActingRole --
        // Stage 2+ needs this native path for whichever side acts first there
        // too, since CampaignSnapshotV11 stays Stage 1/Turn 1-only (owner's
        // Option-2 ruling). actingRole is null, and this rejects, whenever side
        // is not one of this stage's two acting sides at all.
        var actingRole = order.ActingRole(side);
        if (position.PhaseId != LandPhaseIds.ReserveDesignation || actingRole is null || position.ActorRole != actingRole
            || contactVersion != version || priorHash != expectedWorldHash
            || context.ArtifactV6 is not { } artifact || !CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario))
            throw new InvalidOperationException("Second Reserve requires its owner and settled World.");
        var next = position;
        var resultWorld = world;
        if (elementId is null)
        {
            next = Cna1979LandSequenceV4.GetNext(position);
            if (next.SegmentId != LandSegmentIds.Movement || next.ActorRole != actingRole)
                throw new InvalidOperationException("Second Reserve completion needs its Movement successor.");
        }
        else
        {
            var content = artifact.Definition.LegacyDefinition.Elements.SingleOrDefault(value => value.ElementId == elementId);
            if (content is null || content.SideId != CampaignSnapshotSerializer.FormatSide(side)
                || content.PlacementMode != ContentPlacementMode.Independent || content.BreakdownVehicleCohort is not null
                || !world.Representations.Any(value => value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
                    && value.BoundElementIds.Count == 1 && value.BoundElementIds[0] == elementId))
                throw new InvalidOperationException("Reserve selection is not an eligible own element.");
            resultWorld = CampaignWorldV6Factory.DesignateReserve(world, elementId);
            if (!CampaignWorldV6Validator.IsValid(resultWorld, artifact, context.Scenario))
                throw new InvalidOperationException("Reserve selection leaves invalid World authority.");
        }
        return (new(CurrentContractVersion, campaignId, version,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(priorBytes)), side,
            position.PositionId, next.PositionId, elementId, priorHash,
            CampaignNativeCombatPaidWorldV22.Hash(resultWorld)), resultWorld);
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1 || !Enum.IsDefined(Side)
            || !IsHash(PriorEventHash) || !IsHash(PriorWorldHash) || !IsHash(ResultWorldHash))
            throw new JsonException("Invalid second Reserve event.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "second-reserve-decided");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteString("side", CampaignSnapshotSerializer.FormatSide(Side));
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            if (ElementId is null) writer.WriteNull("elementId");
            else writer.WriteString("elementId", ContentContractGuards.RequireStableId(ElementId, nameof(ElementId)));
            writer.WriteString("priorWorldHash", PriorWorldHash);
            writer.WriteString("resultWorldHash", ResultWorldHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeSecondReserveV43 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Invalid event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorEventHash", "side", "positionId", "nextPositionId", "elementId",
                "priorWorldHash", "resultWorldHash");
            if (root.GetProperty("eventType").GetString() != "second-reserve-decided") throw new JsonException("Invalid event type.");
            var side = root.GetProperty("side").GetString() switch
            {
                "axis" => LandSide.Axis,
                "commonwealth" => LandSide.Commonwealth,
                _ => throw new JsonException("Invalid side."),
            };
            var element = root.GetProperty("elementId");
            var parsed = new CampaignNativeSecondReserveV43(root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!, root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!, side, root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!, element.ValueKind == JsonValueKind.Null ? null : element.GetString(),
                root.GetProperty("priorWorldHash").GetString()!, root.GetProperty("resultWorldHash").GetString()!);
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical())) throw new JsonException("Noncanonical event.");
            return parsed;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        { throw new JsonException("Invalid second Reserve event.", error); }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
