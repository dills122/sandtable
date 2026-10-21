using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// System closes an incomplete voluntary round without inventing the missing
// owner seal. The Host supplies and durably verifies trusted clock evidence.
internal sealed record CampaignNativeCombatForceCancellationV19(
    int ContractVersion,
    string CampaignId,
    long PriorStateVersion,
    string DecisionActionId,
    string PositionId,
    string NextPositionId,
    string ConfigurationHash,
    long OpenedAtUnixMilliseconds,
    long DeadlineUnixMilliseconds,
    long HighWaterUnixMilliseconds,
    long? CancelledAtUnixMilliseconds,
    string Reason,
    string? RetainedSealHash)
{
    internal const int CurrentContractVersion = 19;
    // An absent timing envelope has no budget, deadline or trusted clock cut.
    internal const string NoTimingConfigurationHash = "sha256:" +
        "0000000000000000000000000000000000000000000000000000000000000000";
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeCombatForceCancellationV19 Create(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatRetreatDeclineV15 decline,
        IReadOnlyList<CampaignNativeCombatForceSealV17> seals,
        string configurationHash, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds,
        string reason)
    {
        if (selected.Decision.Choice != "select-contact-assault"
            || seals.Count > 1
            || (seals.Count == 1 && (seals[0].PriorStateVersion != decline.StateVersion
                || seals[0].DecisionActionId != selected.Decision.ActionId
                || seals[0].PositionId != decline.NextPositionId))
            || !IsHash(configurationHash)
            || (reason == "force-unavailable"
                ? seals.Count != 0
                    || openedAtUnixMilliseconds != -1
                    || deadlineUnixMilliseconds != -1
                    || highWaterUnixMilliseconds != -1
                : openedAtUnixMilliseconds < 0
                    || deadlineUnixMilliseconds <= openedAtUnixMilliseconds
                    || highWaterUnixMilliseconds < openedAtUnixMilliseconds
                    || highWaterUnixMilliseconds >= deadlineUnixMilliseconds)
            || reason is not ("deadline" or "clock-unavailable" or "clock-regression"
                or "controller-unavailable" or "force-unavailable"))
            throw new InvalidOperationException("Incomplete Force round cancellation has invalid authority.");
        if ((reason == "deadline"
                && (cancelledAtUnixMilliseconds is null
                    || cancelledAtUnixMilliseconds < deadlineUnixMilliseconds))
            || (reason == "clock-regression"
                && (cancelledAtUnixMilliseconds is null
                    || cancelledAtUnixMilliseconds < 0
                    || cancelledAtUnixMilliseconds >= highWaterUnixMilliseconds))
            || ((reason is "clock-unavailable" or "controller-unavailable"
                or "force-unavailable")
                && cancelledAtUnixMilliseconds is not null))
            throw new InvalidOperationException("Force cancellation clock and reason differ.");
        var position = Cna1979LandSequenceV4.CreateTurn(
                selected.PriorSnapshot.CurrentPosition.SequencePosition!.GameTurn)
            .Single(value => value.PositionId == decline.NextPositionId);
        if (position.SegmentId != LandSegmentIds.Combat
            || position.StepId != LandStepIds.ForceAssignment)
            throw new InvalidOperationException("Force cancellation left Force Assignment.");
        position = Cna1979LandSequenceV4.GetNext(position);
        if (position.SegmentId != LandSegmentIds.Combat
            || position.StepId != LandStepIds.AntiArmor
            || position.GameTurn != selected.PriorSnapshot.CurrentPosition.SequencePosition.GameTurn
            || position.OperationStage != selected.PriorSnapshot.CurrentPosition.SequencePosition.OperationStage)
            throw new InvalidOperationException("Force cancellation must reach same-slot Anti Armor.");
        return new(CurrentContractVersion, selected.CampaignId,
            seals.Count == 0 ? decline.StateVersion : seals[0].StateVersion,
            selected.Decision.ActionId, decline.NextPositionId, position.PositionId,
            configurationHash, openedAtUnixMilliseconds, deadlineUnixMilliseconds,
            highWaterUnixMilliseconds, cancelledAtUnixMilliseconds, reason,
            seals.Count == 0 ? null : Hash(seals[0].SerializeCanonical()));
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !IsHash(ConfigurationHash)
            || (RetainedSealHash is not null && !IsHash(RetainedSealHash)))
            throw new JsonException("Invalid Force cancellation.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-force-cancelled");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("configurationHash", ConfigurationHash);
            writer.WriteNumber("openedAtUnixMilliseconds", OpenedAtUnixMilliseconds);
            writer.WriteNumber("deadlineUnixMilliseconds", DeadlineUnixMilliseconds);
            writer.WriteNumber("highWaterUnixMilliseconds", HighWaterUnixMilliseconds);
            if (CancelledAtUnixMilliseconds is { } cancelled)
                writer.WriteNumber("cancelledAtUnixMilliseconds", cancelled);
            writer.WriteString("reason", Reason);
            if (RetainedSealHash is not null)
                writer.WriteString("retainedSealHash", RetainedSealHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeCombatForceCancellationV19 Replay(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatRetreatDeclineV15 decline,
        IReadOnlyList<CampaignNativeCombatForceSealV17> seals,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Create(selected, decline, seals, parsed.ConfigurationHash,
            parsed.OpenedAtUnixMilliseconds, parsed.DeadlineUnixMilliseconds,
            parsed.HighWaterUnixMilliseconds, parsed.CancelledAtUnixMilliseconds,
            parsed.Reason);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException("Force cancellation differs from retained authority.");
        return expected;
    }

    internal static CampaignNativeCombatForceCancellationV19 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            var cancelled = root.TryGetProperty("cancelledAtUnixMilliseconds", out var cancelledAt);
            var sealedFirst = root.TryGetProperty("retainedSealHash", out var sealHash);
            var properties = new List<string> { "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "decisionActionId", "positionId", "nextPositionId",
                "configurationHash", "openedAtUnixMilliseconds", "deadlineUnixMilliseconds",
                "highWaterUnixMilliseconds" };
            if (cancelled) properties.Add("cancelledAtUnixMilliseconds");
            properties.Add("reason");
            if (sealedFirst) properties.Add("retainedSealHash");
            CampaignSnapshotSerializer.RequireProperties(root, properties.ToArray());
            if (root.GetProperty("eventType").GetString() != "combat-force-cancelled")
                throw new JsonException("Unknown Force cancellation event.");
            var parsed = new CampaignNativeCombatForceCancellationV19(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("decisionActionId").GetString()!,
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("configurationHash").GetString()!,
                root.GetProperty("openedAtUnixMilliseconds").GetInt64(),
                root.GetProperty("deadlineUnixMilliseconds").GetInt64(),
                root.GetProperty("highWaterUnixMilliseconds").GetInt64(),
                cancelled ? cancelledAt.GetInt64() : null,
                root.GetProperty("reason").GetString()!,
                sealedFirst ? sealHash.GetString() : null);
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical Force cancellation event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        {
            throw new JsonException("Invalid Force cancellation event.", exception);
        }
    }

    internal byte[] SerializeStateCanonical(ReadOnlySpan<byte> priorState)
    {
        if (priorState.IsEmpty) throw new ArgumentException("Force cancellation needs retained authority.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("positionId", NextPositionId);
            writer.WriteString("priorStateHash", Hash(priorState));
            writer.WritePropertyName("cancellation");
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
