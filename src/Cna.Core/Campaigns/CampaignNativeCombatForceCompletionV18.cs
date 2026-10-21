using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Clock-free System continuation after two accepted owner seals. Timing and
// cancellation belong to the durable Host decision round, not this rule event.
internal sealed record CampaignNativeCombatForceCompletionV18(
    int ContractVersion,
    string CampaignId,
    long PriorStateVersion,
    string DecisionActionId,
    string PositionId,
    string NextPositionId,
    string AttackerSealHash,
    string DefenderSealHash)
{
    internal const int CurrentContractVersion = 18;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeCombatForceCompletionV18 Create(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatRetreatDeclineV15 decline,
        IReadOnlyList<CampaignNativeCombatForceSealV17> seals)
    {
        if (seals.Count != 2 || seals[0].OwnerSide == seals[1].OwnerSide
            || seals[0].PriorStateVersion != decline.StateVersion
            || seals[1].PriorStateVersion != seals[0].StateVersion
            || seals.Any(value => value.CampaignId != selected.CampaignId
                || value.DecisionActionId != selected.Decision.ActionId
                || value.PositionId != decline.NextPositionId
                || value.NextPositionId != decline.NextPositionId))
            throw new InvalidOperationException("Force completion requires both retained owner seals.");
        var position = Cna1979LandSequenceV4.CreateTurn(
                selected.PriorSnapshot.CurrentPosition.SequencePosition!.GameTurn)
            .Single(value => value.PositionId == decline.NextPositionId);
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (position.StepId != LandStepIds.ForceAssignment
            || next.StepId != LandStepIds.AntiArmor
            || next.GameTurn != position.GameTurn
            || next.OperationStage != position.OperationStage)
            throw new InvalidOperationException("Force completion must enter same-slot Anti Armor.");
        var attacker = seals.Single(value => value.Role == "attacker");
        var defender = seals.Single(value => value.Role == "defender");
        return new(CurrentContractVersion, selected.CampaignId,
            seals[1].StateVersion, selected.Decision.ActionId,
            position.PositionId, next.PositionId,
            Hash(attacker.SerializeCanonical()), Hash(defender.SerializeCanonical()));
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !IsHash(AttackerSealHash) || !IsHash(DefenderSealHash))
            throw new JsonException("Invalid Force Assignment completion.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-force-completed");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("attackerSealHash", AttackerSealHash);
            writer.WriteString("defenderSealHash", DefenderSealHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeCombatForceCompletionV18 Replay(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatRetreatDeclineV15 decline,
        IReadOnlyList<CampaignNativeCombatForceSealV17> seals,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var expected = Create(selected, decline, seals);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException("Force completion differs from retained seals.");
        return expected;
    }

    internal static CampaignNativeCombatForceCompletionV18 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "decisionActionId", "positionId",
                "nextPositionId", "attackerSealHash", "defenderSealHash");
            if (root.GetProperty("eventType").GetString() != "combat-force-completed")
                throw new JsonException("Unknown Force Assignment completion.");
            var parsed = new CampaignNativeCombatForceCompletionV18(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("decisionActionId").GetString()!,
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("attackerSealHash").GetString()!,
                root.GetProperty("defenderSealHash").GetString()!);
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical Force Assignment completion.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        {
            throw new JsonException("Invalid Force Assignment completion.", exception);
        }
    }

    internal byte[] SerializeStateCanonical(ReadOnlySpan<byte> priorState)
    {
        if (priorState.IsEmpty) throw new ArgumentException("Force completion needs retained seals.");
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
