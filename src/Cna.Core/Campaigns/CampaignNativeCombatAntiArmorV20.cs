using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Source-legal empty Anti Armor position for an infantry-only campaign cut.
internal sealed record CampaignNativeCombatAntiArmorV20(
    int ContractVersion, string CampaignId, long PriorStateVersion,
    string DecisionActionId, string PositionId, string NextPositionId,
    string PriorForceEventHash, string Disposition)
{
    internal const int CurrentContractVersion = 20;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeCombatAntiArmorV20 Create(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatForceCompletionV18? completion,
        CampaignNativeCombatForceCancellationV19? cancellation,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(context);
        var components = context.ArtifactV6?.Definition.ElementCombatFacts
            .SelectMany(facts => facts.Components).ToArray();
        if ((completion is null) == (cancellation is null)
            || components is not { Length: > 0 }
            || components.Any(component => component.ComponentClassId
                    != Cna1979Combat.InfantryComponentClassId))
            throw new InvalidOperationException("Anti Armor requires one retained infantry-only Force outcome.");
        var priorVersion = completion?.StateVersion ?? cancellation!.StateVersion;
        var positionId = completion?.NextPositionId ?? cancellation!.NextPositionId;
        var priorEvent = completion?.SerializeCanonical() ?? cancellation!.SerializeCanonical();
        if ((completion?.CampaignId ?? cancellation!.CampaignId) != selected.CampaignId
            || (completion?.DecisionActionId ?? cancellation!.DecisionActionId)
                != selected.Decision.ActionId)
            throw new InvalidOperationException("Anti Armor Force outcome differs from selected assault.");
        var position = Cna1979LandSequenceV4.CreateTurn(
                selected.PriorSnapshot.CurrentPosition.SequencePosition!.GameTurn)
            .Single(value => value.PositionId == positionId);
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (position.SegmentId != LandSegmentIds.Combat
            || position.StepId != LandStepIds.AntiArmor
            || next.StepId != LandStepIds.CloseAssault
            || next.GameTurn != position.GameTurn
            || next.OperationStage != position.OperationStage)
            throw new InvalidOperationException("Anti Armor must enter same-slot Close Assault.");
        return new(CurrentContractVersion, selected.CampaignId, priorVersion,
            selected.Decision.ActionId, positionId, next.PositionId,
            Hash(priorEvent), completion is null ? "cancelled-no-attack" : "no-effect");
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !IsHash(PriorForceEventHash)
            || Disposition is not ("no-effect" or "cancelled-no-attack"))
            throw new JsonException("Invalid native Anti Armor completion.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-anti-armor-completed");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("priorForceEventHash", PriorForceEventHash);
            writer.WriteString("disposition", Disposition);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeCombatAntiArmorV20 Replay(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatForceCompletionV18? completion,
        CampaignNativeCombatForceCancellationV19? cancellation,
        CampaignContentContext context, ReadOnlyMemory<byte> canonicalEvent)
    {
        var expected = Create(selected, completion, cancellation, context);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException("Anti Armor completion differs from retained authority.");
        return expected;
    }

    internal static CampaignNativeCombatAntiArmorV20 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "decisionActionId", "positionId",
                "nextPositionId", "priorForceEventHash", "disposition");
            if (root.GetProperty("eventType").GetString() != "combat-anti-armor-completed")
                throw new JsonException("Unknown Anti Armor event.");
            var parsed = new CampaignNativeCombatAntiArmorV20(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("decisionActionId").GetString()!,
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("priorForceEventHash").GetString()!,
                root.GetProperty("disposition").GetString()!);
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical Anti Armor event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        {
            throw new JsonException("Invalid Anti Armor event.", exception);
        }
    }

    internal byte[] SerializeStateCanonical(ReadOnlySpan<byte> priorState)
    {
        if (priorState.IsEmpty) throw new ArgumentException("Anti Armor needs retained Force authority.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("positionId", NextPositionId);
            writer.WriteString("priorStateHash", Hash(priorState));
            writer.WritePropertyName("completion");
            writer.WriteRawValue(SerializeCanonical());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static bool IsHash(string? value)
    {
        if (value is not { Length: 71 }
            || !value.StartsWith("sha256:", StringComparison.Ordinal)) return false;
        foreach (var digit in value.AsSpan(7))
            if (digit is not (>= '0' and <= '9') and not (>= 'a' and <= 'f'))
                return false;
        return true;
    }
}
