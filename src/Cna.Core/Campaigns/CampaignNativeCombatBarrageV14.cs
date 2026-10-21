using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// A selected assault uses the ordinary Combat sequence. Infantry-only Content V6 has
// no barrage participant, so this exact stage records the no-effect transition.
internal sealed record CampaignNativeCombatBarrageV14(
    int ContractVersion,
    string CampaignId,
    long PriorStateVersion,
    string DecisionActionId,
    string PositionId,
    string NextPositionId)
{
    public const int CurrentContractVersion = 14;
    public long StateVersion => checked(PriorStateVersion + 1);

    public static CampaignNativeCombatBarrageV14 Create(
        CampaignNativeCombatDecisionStateV12 selected, CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(context);
        if (selected.Decision.Choice != "select-contact-assault"
            || selected.Decision.BoundRepresentationId is null
            || context.ArtifactV6 is not { } artifact
            || artifact.Definition.ElementCombatFacts.SelectMany(facts => facts.Components)
                .Any(component => component.ComponentClassId != Cna1979Combat.InfantryComponentClassId))
            throw new InvalidOperationException("Selected barrage lacks supported infantry-only authority.");

        var position = Cna1979LandSequenceV4.CreateTurn(
            selected.PriorSnapshot.CurrentPosition.SequencePosition!.GameTurn)
            .Single(candidate => candidate.PositionId == selected.Decision.NextPositionId);
        if (position.SegmentId != LandSegmentIds.Combat || position.StepId != LandStepIds.Barrage)
            throw new InvalidOperationException("Selected assault must enter Barrage.");
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (next.SegmentId != LandSegmentIds.Combat
            || next.StepId != LandStepIds.RetreatBeforeAssault
            || next.GameTurn != position.GameTurn
            || next.OperationStage != position.OperationStage)
            throw new InvalidOperationException("Barrage must reach same-slot Retreat Before Assault.");
        return new(CurrentContractVersion, selected.CampaignId, selected.StateVersion,
            selected.Decision.ActionId, position.PositionId, next.PositionId);
    }

    public byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || string.IsNullOrWhiteSpace(DecisionActionId) || DecisionActionId.Length > 256)
            throw new JsonException("Invalid native Barrage completion.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-barrage-no-effect");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignNativeCombatBarrageV14 Replay(
        CampaignNativeCombatDecisionStateV12 selected, CampaignContentContext context,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Create(selected, context);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException("Barrage differs from selected authority.");
        return expected;
    }

    public static CampaignNativeCombatBarrageV14 Deserialize(ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "decisionActionId", "positionId", "nextPositionId");
            if (root.GetProperty("eventType").GetString() != "combat-barrage-no-effect")
                throw new JsonException("Unknown native Barrage event.");
            var parsed = new CampaignNativeCombatBarrageV14(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("decisionActionId").GetString()!,
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!);
            if (!canonicalEvent.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical native Barrage event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException)
        {
            throw new JsonException("Invalid native Barrage event.", exception);
        }
    }

    public byte[] SerializeStateCanonical(CampaignNativeCombatDecisionStateV12 selected)
    {
        if (selected.CampaignId != CampaignId || selected.StateVersion != PriorStateVersion
            || selected.Decision.ActionId != DecisionActionId)
            throw new InvalidOperationException("Barrage state does not bind its owner decision.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("positionId", NextPositionId);
            writer.WritePropertyName("priorDecisionState");
            writer.WriteRawValue(selected.SerializeCanonical());
            writer.WritePropertyName("barrage");
            writer.WriteRawValue(SerializeCanonical());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
