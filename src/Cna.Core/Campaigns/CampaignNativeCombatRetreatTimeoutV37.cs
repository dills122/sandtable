using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

internal sealed record CampaignNativeCombatRetreatTimeoutV37(
    string CampaignId, long PriorStateVersion, string DecisionActionId, string PositionId,
    string ResultHash, string PaidWorldHash, string RetreatSourceHash, LandSide DefenderSide,
    string DefenderElementId, string ConfigurationHash, long OpenedAtUnixMilliseconds,
    long DeadlineUnixMilliseconds, long HighWaterUnixMilliseconds, long? CancelledAtUnixMilliseconds,
    string Reason)
    : CampaignNativeCombatRetreatAfterAssaultV23(37, CampaignId, PriorStateVersion, DecisionActionId,
        PositionId, ResultHash, PaidWorldHash, RetreatSourceHash, DefenderSide, DefenderElementId,
        "refused", null, null)
{
    internal new const int CurrentContractVersion = 37;
    internal const string NoTimingConfigurationHash = "sha256:" +
        "0000000000000000000000000000000000000000000000000000000000000000";

    internal static CampaignNativeCombatRetreatTimeoutV37 CreateTimeout(
        CampaignCombatSelectionFrame selected, CampaignNativeCombatAssaultResultV22 result,
        CampaignWorldSnapshotV6 paidWorld, CampaignContentContext context, CampaignContactJournal contacts,
        string configurationHash, long opened, long deadline, long highWater, long? cancelledAt, string reason)
    {
        if (result.Facts.RequiredRetreat != 1)
            throw new InvalidOperationException("Retreat timeout requires a pending required retreat.");
        var cut = CampaignContactRetreatAfterAssaultCut.Project(selected, result, paidWorld, context, contacts);
        var refusal = Create(selected, result, paidWorld, context, contacts, cut.RefuseOffer.ActionId);
        var timeout = new CampaignNativeCombatRetreatTimeoutV37(refusal.CampaignId, refusal.PriorStateVersion,
            refusal.DecisionActionId, refusal.PositionId, refusal.ResultHash, refusal.PaidWorldHash,
            refusal.RetreatSourceHash, refusal.DefenderSide, refusal.DefenderElementId,
            configurationHash, opened, deadline, highWater, cancelledAt, reason);
        _ = timeout.SerializeCanonical();
        return timeout;
    }

    internal override byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || Disposition != "refused" || ActionId is not null || DestinationLocationId is not null
            || PriorStateVersion < 1 || !Enum.IsDefined(DefenderSide)
            || !IsHash(ResultHash) || !IsHash(PaidWorldHash) || !IsHash(RetreatSourceHash)
            || string.IsNullOrWhiteSpace(DecisionActionId) || DecisionActionId.Length > 256
            || !IsHash(ConfigurationHash)
            || Reason is not ("deadline" or "clock-unavailable" or "clock-regression"
                or "controller-unavailable" or "retreat-unavailable")
            || (Reason == "retreat-unavailable"
                ? OpenedAtUnixMilliseconds != -1 || DeadlineUnixMilliseconds != -1 || HighWaterUnixMilliseconds != -1
                : OpenedAtUnixMilliseconds < 0 || DeadlineUnixMilliseconds <= OpenedAtUnixMilliseconds
                    || HighWaterUnixMilliseconds < OpenedAtUnixMilliseconds || HighWaterUnixMilliseconds >= DeadlineUnixMilliseconds)
            || (Reason == "deadline" && (CancelledAtUnixMilliseconds is null || CancelledAtUnixMilliseconds < DeadlineUnixMilliseconds))
            || (Reason == "clock-regression" && (CancelledAtUnixMilliseconds is null
                || CancelledAtUnixMilliseconds < 0 || CancelledAtUnixMilliseconds >= HighWaterUnixMilliseconds))
            || ((Reason is "clock-unavailable" or "controller-unavailable" or "retreat-unavailable") && CancelledAtUnixMilliseconds is not null))
            throw new JsonException("Invalid native retreat timeout.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-retreat-timeout");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("resultHash", ResultHash);
            writer.WriteString("paidWorldHash", PaidWorldHash);
            writer.WriteString("retreatSourceHash", RetreatSourceHash);
            writer.WriteString("defenderSide", DefenderSide.ToString());
            writer.WriteString("defenderElementId", ContentContractGuards.RequireStableId(DefenderElementId, nameof(DefenderElementId)));
            writer.WriteString("fallback", "refuse-retreat");
            writer.WriteString("configurationHash", ConfigurationHash);
            writer.WriteNumber("openedAtUnixMilliseconds", OpenedAtUnixMilliseconds);
            writer.WriteNumber("deadlineUnixMilliseconds", DeadlineUnixMilliseconds);
            writer.WriteNumber("highWaterUnixMilliseconds", HighWaterUnixMilliseconds);
            if (CancelledAtUnixMilliseconds is { } observed) writer.WriteNumber("cancelledAtUnixMilliseconds", observed);
            writer.WriteString("reason", Reason);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal new static CampaignNativeCombatRetreatTimeoutV37 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Retreat timeout exceeds byte bound.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            var hasTime = root.TryGetProperty("cancelledAtUnixMilliseconds", out var time);
            var fields = new List<string> { "contractVersion", "eventType", "campaignId", "priorStateVersion", "decisionActionId", "positionId", "resultHash", "paidWorldHash", "retreatSourceHash", "defenderSide", "defenderElementId", "fallback", "configurationHash", "openedAtUnixMilliseconds", "deadlineUnixMilliseconds", "highWaterUnixMilliseconds" };
            if (hasTime) fields.Add("cancelledAtUnixMilliseconds");
            fields.Add("reason");
            CampaignSnapshotSerializer.RequireProperties(root, fields.ToArray());
            if (root.GetProperty("contractVersion").GetInt32() != CurrentContractVersion
                || root.GetProperty("eventType").GetString() != "combat-retreat-timeout"
                || root.GetProperty("fallback").GetString() != "refuse-retreat")
                throw new JsonException("Unknown retreat timeout.");
            var result = new CampaignNativeCombatRetreatTimeoutV37(
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("decisionActionId").GetString()!,
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("resultHash").GetString()!,
                root.GetProperty("paidWorldHash").GetString()!,
                root.GetProperty("retreatSourceHash").GetString()!,
                Enum.Parse<LandSide>(root.GetProperty("defenderSide").GetString()!, false),
                root.GetProperty("defenderElementId").GetString()!,
                root.GetProperty("configurationHash").GetString()!,
                root.GetProperty("openedAtUnixMilliseconds").GetInt64(),
                root.GetProperty("deadlineUnixMilliseconds").GetInt64(),
                root.GetProperty("highWaterUnixMilliseconds").GetInt64(),
                hasTime ? time.GetInt64() : null,
                root.GetProperty("reason").GetString()!);
            if (!bytes.Span.SequenceEqual(result.SerializeCanonical())) throw new JsonException("Noncanonical retreat timeout.");
            return result;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        { throw new JsonException("Invalid retreat timeout.", exception); }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
