using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSelectionTimeoutEvidenceV38(
    string CampaignId, long PriorStateVersion, int CycleOrdinal, string PositionId,
    string NextPositionId, LandSide Side, string EntryHash, string WorldHash,
    string ConfigurationHash, long OpenedAtUnixMilliseconds, long DeadlineUnixMilliseconds,
    long HighWaterUnixMilliseconds, long? CancelledAtUnixMilliseconds, string Reason)
{
    internal const string NoTimingConfigurationHash = "sha256:" +
        "0000000000000000000000000000000000000000000000000000000000000000";
    internal string Hash => Digest(SerializeCanonical());
    internal static string Digest(ReadOnlySpan<byte> bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    internal byte[] SerializeCanonical()
    {
        if (PriorStateVersion < 1 || CycleOrdinal < 1 || !Enum.IsDefined(Side)
            || !IsHash(EntryHash) || !IsHash(WorldHash) || !IsHash(ConfigurationHash)
            || Reason is not ("deadline" or "clock-unavailable" or "clock-regression"
                or "controller-unavailable" or "selection-unavailable")
            || (Reason == "selection-unavailable"
                ? OpenedAtUnixMilliseconds != -1 || DeadlineUnixMilliseconds != -1 || HighWaterUnixMilliseconds != -1
                : OpenedAtUnixMilliseconds < 0 || DeadlineUnixMilliseconds <= OpenedAtUnixMilliseconds
                    || HighWaterUnixMilliseconds < OpenedAtUnixMilliseconds || HighWaterUnixMilliseconds >= DeadlineUnixMilliseconds)
            || (Reason == "deadline" && (CancelledAtUnixMilliseconds is null || CancelledAtUnixMilliseconds < DeadlineUnixMilliseconds))
            || (Reason == "clock-regression" && (CancelledAtUnixMilliseconds is null
                || CancelledAtUnixMilliseconds < 0 || CancelledAtUnixMilliseconds >= HighWaterUnixMilliseconds))
            || ((Reason is "clock-unavailable" or "controller-unavailable" or "selection-unavailable") && CancelledAtUnixMilliseconds is not null))
            throw new JsonException("Invalid native selection timeout.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 38);
            writer.WriteString("eventType", "combat-selection-timeout");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteNumber("cycleOrdinal", CycleOrdinal);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("side", CampaignSnapshotSerializer.FormatSide(Side));
            writer.WriteString("entryHash", EntryHash);
            writer.WriteString("worldHash", WorldHash);
            writer.WriteString("fallback", "no-selection");
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

    internal static CampaignSelectionTimeoutEvidenceV38 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Selection timeout exceeds byte bound.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            var hasTime = root.TryGetProperty("cancelledAtUnixMilliseconds", out var time);
            var fields = new List<string> { "contractVersion", "eventType", "campaignId", "priorStateVersion",
                "cycleOrdinal", "positionId", "nextPositionId", "side", "entryHash", "worldHash", "fallback",
                "configurationHash", "openedAtUnixMilliseconds", "deadlineUnixMilliseconds", "highWaterUnixMilliseconds" };
            if (hasTime) fields.Add("cancelledAtUnixMilliseconds");
            fields.Add("reason");
            CampaignSnapshotSerializer.RequireProperties(root, fields.ToArray());
            if (root.GetProperty("contractVersion").GetInt32() != 38
                || root.GetProperty("eventType").GetString() != "combat-selection-timeout"
                || root.GetProperty("fallback").GetString() != "no-selection") throw new JsonException("Unknown selection timeout.");
            var result = new CampaignSelectionTimeoutEvidenceV38(root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(), root.GetProperty("cycleOrdinal").GetInt32(),
                root.GetProperty("positionId").GetString()!, root.GetProperty("nextPositionId").GetString()!,
                CampaignSnapshotSerializer.ParseSide(root.GetProperty("side").GetString()),
                root.GetProperty("entryHash").GetString()!, root.GetProperty("worldHash").GetString()!,
                root.GetProperty("configurationHash").GetString()!, root.GetProperty("openedAtUnixMilliseconds").GetInt64(),
                root.GetProperty("deadlineUnixMilliseconds").GetInt64(), root.GetProperty("highWaterUnixMilliseconds").GetInt64(),
                hasTime ? time.GetInt64() : null, root.GetProperty("reason").GetString()!);
            if (!bytes.Span.SequenceEqual(result.SerializeCanonical())) throw new JsonException("Noncanonical selection timeout.");
            return result;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        { throw new JsonException("Invalid selection timeout.", exception); }
    }

    internal static IEnumerable<string> ActingElements(CampaignContentContext context, LandSide side) =>
        context.ArtifactV6!.Definition.LegacyDefinition.Elements
            .Where(value => value.SideId == CampaignSnapshotSerializer.FormatSide(side))
            .Select(value => value.ElementId).OrderBy(value => value, StringComparer.Ordinal);

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}

internal sealed record CampaignNativeCombatSelectionTimeoutV38(CampaignSelectionTimeoutEvidenceV38 Evidence)
    : CampaignNativeCombatDecisionV12(38, Evidence.CampaignId, Evidence.PriorStateVersion, Evidence.PositionId,
        Evidence.NextPositionId, "finish-combat-without-attack", Evidence.Side, "system", null, null, null, Evidence.Hash)
{
    public override byte[] SerializeCanonical() => Evidence.CycleOrdinal == 1
        ? Evidence.SerializeCanonical() : throw new JsonException("Initial selection timeout requires first cycle.");

    internal static CampaignNativeCombatSelectionTimeoutV38 Create(CampaignSnapshotV11 prior,
        CampaignContentContext context, CampaignContactJournal contacts, string configurationHash,
        long opened, long deadline, long highWater, long? observed, string reason)
    {
        var side = ValidateInitialScope(prior, context);
        if (contacts.CampaignId != prior.CampaignId || contacts.AuthorityVersion != prior.StateVersion)
            throw new InvalidOperationException("Selection requires same-cut contacts.");
        var position = prior.CurrentPosition.SequencePosition!;
        var next = Cna1979LandSequenceV4.GetNext(position);
        var timeout = new CampaignNativeCombatSelectionTimeoutV38(new CampaignSelectionTimeoutEvidenceV38(
            prior.CampaignId, prior.StateVersion, 1, position.PositionId, next.PositionId, side,
            CampaignSelectionTimeoutEvidenceV38.Digest(CampaignSnapshotV11Serializer.Serialize(prior)),
            CampaignNativeCombatPaidWorldV22.Hash(prior.World), configurationHash, opened, deadline, highWater, observed, reason));
        // Verify no-work content scope before accepting the System closure.
        _ = CampaignNativeCombatNoAttackCompletionV13.Create(new CampaignNativeCombatDecisionStateV12(prior, timeout), context);
        return timeout;
    }
    internal static LandSide ValidateInitialScope(CampaignSnapshotV11 prior, CampaignContentContext context)
    {
        var position = prior.CurrentPosition.SequencePosition;
        if (context.ArtifactV6 is not { } artifact || position is null
            || prior.CurrentPosition.Kind != CampaignPositionV11Kind.Sequence
            || position.PhaseId != LandPhaseIds.MovementAndCombat || position.SegmentId != LandSegmentIds.Combat
            || position.StepId != LandStepIds.PositionDetermination || position.ActorRole != LandActorRole.FirstActingSide
            || prior.ReactionWindow is not null || prior.BreakdownFlow is not CampaignBreakdownFlow.Idle
            || prior.InitiativeHolder is not { } side
            || artifact.Definition.ElementCombatFacts.SelectMany(value => value.Components)
                .Any(value => value.ComponentClassId != Cna1979Combat.InfantryComponentClassId))
            throw new InvalidOperationException("No pending supported Combat selection.");
        if (CampaignElementMovedV3Factory.DeriveControlledLocationIds(prior.World, artifact, context.Scenario,
                side == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis).Count != 0)
            throw new InvalidOperationException("Selection timeout requires a source-legal no-attack fallback.");
        return side;
    }

}

internal sealed record CampaignNativeRepeatCombatSelectionTimeoutV38(CampaignSelectionTimeoutEvidenceV38 Evidence)
    : CampaignNativeRepeatCombatDecisionV31(38, Evidence.CampaignId, Evidence.PriorStateVersion, Evidence.EntryHash,
        Evidence.CycleOrdinal, Evidence.PositionId, Evidence.NextPositionId, "finish-combat-without-attack",
        Evidence.Side, "system", null, null, null, Evidence.Hash)
{
    internal override byte[] SerializeCanonical() => Evidence.CycleOrdinal >= 2
        ? Evidence.SerializeCanonical() : throw new JsonException("Repeated selection timeout requires later cycle.");

    internal static CampaignNativeRepeatCombatSelectionTimeoutV38 Create(
        CampaignNativeReserveReleaseStateV27 release, CampaignNativeCycleControlV28 control,
        CampaignNativeRepeatMovementV29 movement, CampaignNativeRepeatBreakdownV30 breakdown,
        CampaignWorldSnapshotV6 world, CampaignContentContext context, CampaignContactJournal contacts,
        string configurationHash, long opened, long deadline, long highWater, long? observed, string reason)
    {
        CampaignNativeRepeatCombatDecisionV31? legalFinish = null;
        foreach (var element in CampaignSelectionTimeoutEvidenceV38.ActingElements(context, release.ActingSide))
        {
            try
            {
                var cut = CampaignRepeatCombatOwnerCutV31.Project(release, control, movement, breakdown, world, context, contacts, element);
                legalFinish = Select(release, control, movement, breakdown, world, context, contacts, element, cut.FinishOffer.ActionId);
                break;
            }
            catch (InvalidOperationException) { }
        }
        if (legalFinish is null) throw new InvalidOperationException("No source-legal repeated selection fallback.");
        return new(new CampaignSelectionTimeoutEvidenceV38(release.CampaignId, breakdown.StateVersion, breakdown.CycleOrdinal,
            legalFinish.PositionId, legalFinish.NextPositionId, release.ActingSide,
            CampaignSelectionTimeoutEvidenceV38.Digest(breakdown.SerializeCanonical()), CampaignNativeCombatPaidWorldV22.Hash(world),
            configurationHash, opened, deadline, highWater, observed, reason));
    }
}
