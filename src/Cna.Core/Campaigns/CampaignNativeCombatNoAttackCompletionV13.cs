using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// One System transition records every remaining structural position. It performs no
// attack, draw, expenditure, release, or world mutation. The preceding owner choice
// and the admitted infantry-only content are the no-work authority.
internal sealed record CampaignNativeCombatNoAttackCompletionV13(
    int ContractVersion,
    string CampaignId,
    long PriorStateVersion,
    string DecisionActionId,
    IReadOnlyList<CampaignNativeCombatNoAttackStepV13> Steps)
{
    public const int CurrentContractVersion = 13;
    public long StateVersion => checked(PriorStateVersion + 1);
    public string NextPositionId => Steps[^1].NextPositionId;

    public static CampaignNativeCombatNoAttackCompletionV13 Create(
        CampaignCombatSelectionFrame selected, CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(context);
        if (selected.Decision.Choice != "finish-combat-without-attack"
            || selected.Decision.ContactHandle is not null
            || selected.Decision.BoundRepresentationId is not null
            || context.ArtifactV6 is not { } artifact
            || artifact.Definition.ElementCombatFacts.SelectMany(facts => facts.Components)
                .Any(component => component.ComponentClassId != Cna1979Combat.InfantryComponentClassId))
            throw new InvalidOperationException("No-attack completion lacks admitted authority or has unsupported work.");

        var positions = Cna1979LandSequenceV4.CreateTurn(
            selected.PriorSnapshot.CurrentPosition.SequencePosition!.GameTurn);
        var position = positions.Single(candidate => candidate.PositionId == selected.Decision.NextPositionId);
        var steps = new List<CampaignNativeCombatNoAttackStepV13>(5);
        foreach (var currentStep in new[]
                 {
                     LandStepIds.Barrage, LandStepIds.RetreatBeforeAssault,
                     LandStepIds.ForceAssignment, LandStepIds.AntiArmor,
                     LandStepIds.CloseAssault,
                 })
        {
            if (position.SegmentId != LandSegmentIds.Combat || position.StepId != currentStep)
                throw new InvalidOperationException("No-attack completion is outside the exact Combat sequence.");
            var next = Cna1979LandSequenceV4.GetNext(position);
            steps.Add(new CampaignNativeCombatNoAttackStepV13(position.PositionId, next.PositionId));
            position = next;
        }
        if (position.SegmentId != LandSegmentIds.ReserveRelease
            || position.GameTurn != selected.PriorSnapshot.CurrentPosition.SequencePosition.GameTurn
            || position.OperationStage != selected.PriorSnapshot.CurrentPosition.SequencePosition.OperationStage)
            throw new InvalidOperationException("No-attack completion must reach same-slot Reserve Release.");
        return new(CurrentContractVersion, selected.CampaignId, selected.StateVersion,
            selected.Decision.ActionId, steps);
    }

    public byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1 || Steps.Count != 5)
            throw new JsonException("Invalid native no-attack completion.");
        _ = ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId));
        if (string.IsNullOrWhiteSpace(DecisionActionId) || DecisionActionId.Length > 256)
            throw new JsonException("Invalid native Combat decision binding.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-no-attack-completed");
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteStartArray("steps");
            foreach (var step in Steps)
            {
                writer.WriteStartObject();
                writer.WriteString("positionId", ContentContractGuards.RequireStableId(
                    step.PositionId, nameof(step.PositionId)));
                writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(
                    step.NextPositionId, nameof(step.NextPositionId)));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignNativeCombatNoAttackCompletionV13 Replay(
        CampaignCombatSelectionFrame selected, CampaignContentContext context,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "decisionActionId", "steps");
            if (root.GetProperty("eventType").GetString() != "combat-no-attack-completed")
                throw new JsonException("Unknown native Combat completion event.");
            var expected = Create(selected, context);
            if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
                throw new InvalidCampaignHistoryException("No-attack completion differs from source authority.");
            return expected;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException)
        {
            throw new JsonException("Invalid native no-attack completion event.", exception);
        }
    }

    public static CampaignNativeCombatNoAttackCompletionV13 Deserialize(
        ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "decisionActionId", "steps");
            if (root.GetProperty("eventType").GetString() != "combat-no-attack-completed")
                throw new JsonException("Unknown native Combat completion event.");
            var steps = root.GetProperty("steps").EnumerateArray().Select(value =>
            {
                CampaignSnapshotSerializer.RequireProperties(value, "positionId", "nextPositionId");
                return new CampaignNativeCombatNoAttackStepV13(
                    value.GetProperty("positionId").GetString()!,
                    value.GetProperty("nextPositionId").GetString()!);
            }).ToArray();
            var parsed = new CampaignNativeCombatNoAttackCompletionV13(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("decisionActionId").GetString()!, steps);
            if (!canonicalEvent.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical native Combat completion event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException)
        {
            throw new JsonException("Invalid native no-attack completion event.", exception);
        }
    }

    public byte[] SerializeStateCanonical(CampaignCombatSelectionFrame selected)
    {
        ArgumentNullException.ThrowIfNull(selected);
        if (selected.CampaignId != CampaignId || selected.StateVersion != PriorStateVersion
            || selected.Decision.ActionId != DecisionActionId)
            throw new InvalidOperationException("No-attack state does not bind its owner decision.");
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
            writer.WritePropertyName("completion");
            writer.WriteRawValue(SerializeCanonical());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}

internal sealed record CampaignNativeCombatNoAttackStepV13(string PositionId, string NextPositionId);
