using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// An incomplete Force round cannot produce an assault result or random draw.
internal sealed record CampaignNativeCombatCancelledAssaultV21(
    int ContractVersion, string CampaignId, long PriorStateVersion,
    string DecisionActionId, string PositionId, string NextPositionId,
    string AntiArmorEventHash)
{
    internal const int CurrentContractVersion = 21;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeCombatCancelledAssaultV21 Create(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatAntiArmorV20 antiArmor)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(antiArmor);
        if (antiArmor.Disposition != "cancelled-no-attack"
            || antiArmor.CampaignId != selected.CampaignId
            || antiArmor.DecisionActionId != selected.Decision.ActionId)
            throw new InvalidOperationException("Only a cancelled Force round may finish without assault.");
        var position = Cna1979LandSequenceV4.CreateTurn(
                selected.PriorSnapshot.CurrentPosition.SequencePosition!.GameTurn)
            .Single(value => value.PositionId == antiArmor.NextPositionId);
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (position.SegmentId != LandSegmentIds.Combat
            || position.StepId != LandStepIds.CloseAssault
            || next.SegmentId != LandSegmentIds.ReserveRelease
            || next.GameTurn != position.GameTurn
            || next.OperationStage != position.OperationStage)
            throw new InvalidOperationException("Cancelled assault must reach same-slot Reserve Release.");
        return new(CurrentContractVersion, selected.CampaignId, antiArmor.StateVersion,
            selected.Decision.ActionId, position.PositionId, next.PositionId,
            Hash(antiArmor.SerializeCanonical()));
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !IsHash(AntiArmorEventHash))
            throw new JsonException("Invalid cancelled Close Assault.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-cancelled-assault-completed");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("antiArmorEventHash", AntiArmorEventHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeCombatCancelledAssaultV21 Replay(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatAntiArmorV20 antiArmor,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var expected = Create(selected, antiArmor);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException("Cancelled Close Assault differs from retained authority.");
        return expected;
    }

    internal static CampaignNativeCombatCancelledAssaultV21 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "decisionActionId", "positionId",
                "nextPositionId", "antiArmorEventHash");
            if (root.GetProperty("eventType").GetString() != "combat-cancelled-assault-completed")
                throw new JsonException("Unknown cancelled Close Assault event.");
            var parsed = new CampaignNativeCombatCancelledAssaultV21(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("decisionActionId").GetString()!,
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("antiArmorEventHash").GetString()!);
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical cancelled Close Assault event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        {
            throw new JsonException("Invalid cancelled Close Assault event.", exception);
        }
    }

    internal byte[] SerializeStateCanonical(ReadOnlySpan<byte> priorState)
    {
        if (priorState.IsEmpty) throw new ArgumentException("Cancelled assault needs retained Anti Armor authority.");
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
