using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// System cancels unanswered RBA without inventing a defender decline.
// Host supplies and durably verifies trusted clock evidence.
internal sealed record CampaignNativeCombatRbaCancellationV33(
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
    string BarrageEventHash,
    string? RetainedRetreatHash)
{
    internal const int CurrentContractVersion = 33;
    // An absent timing envelope has no budget, deadline or trusted clock cut.
    internal const string NoTimingConfigurationHash = "sha256:" +
        "0000000000000000000000000000000000000000000000000000000000000000";
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeCombatRbaCancellationV33 Create(
        CampaignCombatSelectionFrame selected, CampaignNativeCombatBarrageV14 barrage,
        CampaignNativeCombatRetreatMoveV16? retreat,
        string configurationHash, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason)
    {
        if (selected.Decision.Choice != "select-contact-assault"
            || barrage.CampaignId != selected.CampaignId
            || barrage.DecisionActionId != selected.Decision.ActionId
            || (retreat is not null && (retreat.CampaignId != selected.CampaignId
                || retreat.DecisionActionId != selected.Decision.ActionId
                || retreat.PositionId != barrage.NextPositionId
                || retreat.StateVersion <= barrage.StateVersion))
            || !IsHash(configurationHash)
            || (reason == "rba-unavailable"
                ? openedAtUnixMilliseconds != -1 || deadlineUnixMilliseconds != -1
                    || highWaterUnixMilliseconds != -1 || retreat is not null
                : openedAtUnixMilliseconds < 0 || deadlineUnixMilliseconds <= openedAtUnixMilliseconds
                    || highWaterUnixMilliseconds < openedAtUnixMilliseconds
                    || highWaterUnixMilliseconds >= deadlineUnixMilliseconds)
            || reason is not ("deadline" or "clock-unavailable" or "clock-regression"
                or "controller-unavailable" or "rba-unavailable"))
            throw new InvalidOperationException("Unanswered RBA cancellation has invalid authority.");
        if ((reason == "deadline" && (cancelledAtUnixMilliseconds is null
                    || cancelledAtUnixMilliseconds < deadlineUnixMilliseconds))
            || (reason == "clock-regression" && (cancelledAtUnixMilliseconds is null
                    || cancelledAtUnixMilliseconds < 0 || cancelledAtUnixMilliseconds >= highWaterUnixMilliseconds))
            || ((reason is "clock-unavailable" or "controller-unavailable" or "rba-unavailable")
                && cancelledAtUnixMilliseconds is not null))
            throw new InvalidOperationException("RBA cancellation clock and reason differ.");
        var position = Cna1979LandSequenceV4.CreateTurn(
            selected.PriorSnapshot.CurrentPosition.SequencePosition!.GameTurn)
            .Single(value => value.PositionId == barrage.NextPositionId);
        if (position.SegmentId != LandSegmentIds.Combat
            || position.StepId != LandStepIds.RetreatBeforeAssault)
            throw new InvalidOperationException("RBA cancellation left Retreat Before Assault.");
        var next = position;
        while (next.SegmentId == LandSegmentIds.Combat)
            next = Cna1979LandSequenceV4.GetNext(next);
        if (next.SegmentId != LandSegmentIds.ReserveRelease
            || next.GameTurn != position.GameTurn || next.OperationStage != position.OperationStage)
            throw new InvalidOperationException("RBA cancellation must reach same-slot Reserve Release.");
        return new(CurrentContractVersion, selected.CampaignId,
            retreat?.StateVersion ?? barrage.StateVersion, selected.Decision.ActionId,
            position.PositionId, next.PositionId, configurationHash, openedAtUnixMilliseconds,
            deadlineUnixMilliseconds, highWaterUnixMilliseconds, cancelledAtUnixMilliseconds,
            reason, Hash(barrage.SerializeCanonical()), retreat is null ? null : Hash(retreat.SerializeCanonical()));
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !IsHash(ConfigurationHash) || !IsHash(BarrageEventHash)
            || (RetainedRetreatHash is not null && !IsHash(RetainedRetreatHash)))
            throw new JsonException("Invalid RBA cancellation.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-rba-cancelled");
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
            writer.WriteString("barrageEventHash", BarrageEventHash);
            if (RetainedRetreatHash is not null)
                writer.WriteString("retainedRetreatHash", RetainedRetreatHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeCombatRbaCancellationV33 Replay(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignNativeCombatRetreatMoveV16? retreat,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Create(selected, barrage, retreat, parsed.ConfigurationHash,
            parsed.OpenedAtUnixMilliseconds, parsed.DeadlineUnixMilliseconds,
            parsed.HighWaterUnixMilliseconds, parsed.CancelledAtUnixMilliseconds,
            parsed.Reason);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException("RBA cancellation differs from retained authority.");
        return expected;
    }

    internal static CampaignNativeCombatRbaCancellationV33 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            var cancelled = root.TryGetProperty("cancelledAtUnixMilliseconds", out var cancelledAt);
            var hasRetreat = root.TryGetProperty("retainedRetreatHash", out var retreatHash);
            var properties = new List<string> { "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "decisionActionId", "positionId", "nextPositionId",
                "configurationHash", "openedAtUnixMilliseconds", "deadlineUnixMilliseconds",
                "highWaterUnixMilliseconds" };
            if (cancelled) properties.Add("cancelledAtUnixMilliseconds");
            properties.Add("reason");
            properties.Add("barrageEventHash");
            if (hasRetreat) properties.Add("retainedRetreatHash");
            CampaignSnapshotSerializer.RequireProperties(root, properties.ToArray());
            if (root.GetProperty("eventType").GetString() != "combat-rba-cancelled")
                throw new JsonException("Unknown RBA cancellation event.");
            var parsed = new CampaignNativeCombatRbaCancellationV33(
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
                root.GetProperty("barrageEventHash").GetString()!,
                hasRetreat ? retreatHash.GetString() : null);
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical RBA cancellation event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        {
            throw new JsonException("Invalid RBA cancellation event.", exception);
        }
    }

    internal byte[] SerializeStateCanonical(ReadOnlySpan<byte> priorState)
    {
        if (priorState.IsEmpty) throw new ArgumentException("RBA cancellation needs retained authority.");
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
