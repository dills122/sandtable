using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// Subsequent-cycle selection is a new authority event. It does not reinterpret
// the accepted later-cycle World as an initial V11 snapshot.
internal sealed record CampaignNativeRepeatCombatDecisionV31(
    int ContractVersion, string CampaignId, long PriorStateVersion,
    string PriorEventHash, int CycleOrdinal, string PositionId,
    string NextPositionId, string Choice, LandSide Side,
    string ActingElementId, string? ContactHandle,
    string? ApparentLocationId, string? BoundRepresentationId,
    string ActionId)
{
    internal const int CurrentContractVersion = 31;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeRepeatCombatDecisionV31 Select(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignNativeCycleControlV28 control,
        CampaignNativeRepeatMovementV29 movement,
        CampaignNativeRepeatBreakdownV30 breakdown,
        CampaignWorldSnapshotV6 movedWorld,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        string actingElementId,
        string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        var cut = CampaignRepeatCombatOwnerCutV31.Project(release, control,
            movement, breakdown, movedWorld, context, contacts,
            actingElementId);
        var assault = cut.AssaultOffers.SingleOrDefault(value =>
            value.ActionId == actionId);
        var finish = cut.FinishOffer.ActionId == actionId;
        if (assault is null && !finish)
            throw new InvalidOperationException(
                "Repeated Combat selection is not offered at this cut.");
        var bound = assault is null ? null : contacts.ResolvePrivateTarget(
            release.ActingSide, cut.Observation.ActingFormationId,
            assault.ContactHandle);
        var combatPosition = Cna1979LandSequenceV4.CreateTurn(
            release.Position.GameTurn).Single(value =>
                value.PositionId == breakdown.NextPositionId);
        var barrage = Cna1979LandSequenceV4.GetNext(combatPosition);
        if (barrage.SegmentId != LandSegmentIds.Combat
            || barrage.StepId != LandStepIds.Barrage)
            throw new InvalidOperationException(
                "Repeated Combat selection must advance to Barrage.");
        var selected = new CampaignNativeRepeatCombatDecisionV31(
            CurrentContractVersion, release.CampaignId,
            breakdown.StateVersion, Hash(breakdown.SerializeCanonical()),
            breakdown.CycleOrdinal, breakdown.NextPositionId,
            barrage.PositionId,
            finish ? "finish-combat-without-attack"
                : "select-contact-assault",
            release.ActingSide, actingElementId, assault?.ContactHandle,
            assault?.ApparentLocationId, bound, actionId);
        _ = selected.SerializeCanonical();
        return selected;
    }

    internal static CampaignNativeRepeatCombatDecisionV31 Replay(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignNativeCycleControlV28 control,
        CampaignNativeRepeatMovementV29 movement,
        CampaignNativeRepeatBreakdownV30 breakdown,
        CampaignWorldSnapshotV6 movedWorld,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Select(release, control, movement, breakdown,
            movedWorld, context, contacts, parsed.ActingElementId,
            parsed.ActionId);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException(
                "Repeated Combat selection differs from retained authority.");
        return expected;
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion
            || PriorStateVersion < 1 || CycleOrdinal < 2
            || !Enum.IsDefined(Side) || !IsHash(PriorEventHash)
            || !IsHash(ActionId)
            || Choice is not ("select-contact-assault"
                or "finish-combat-without-attack")
            || Choice == "select-contact-assault"
                && (ContactHandle is null || ApparentLocationId is null
                    || BoundRepresentationId is null)
            || Choice == "finish-combat-without-attack"
                && (ContactHandle is not null || ApparentLocationId is not null
                    || BoundRepresentationId is not null))
            throw new JsonException("Invalid repeated Combat selection event.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "cycle-repeat-combat-selection-decided");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteNumber("cycleOrdinal", CycleOrdinal);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(
                PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(
                NextPositionId, nameof(NextPositionId)));
            writer.WriteString("choice", Choice);
            writer.WriteString("side", CampaignSnapshotSerializer.FormatSide(Side));
            writer.WriteString("actingElementId", ContentContractGuards.RequireStableId(
                ActingElementId, nameof(ActingElementId)));
            writer.WriteString("contactHandle", ContactHandle);
            writer.WriteString("apparentLocationId", ApparentLocationId);
            writer.WriteString("boundRepresentationId", BoundRepresentationId);
            writer.WriteString("actionId", ActionId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeRepeatCombatDecisionV31 Deserialize(
        ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            if (canonicalEvent.Length is < 1 or > 8192)
                throw new JsonException("Repeated Combat selection exceeds byte bound.");
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root,
                "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorEventHash", "cycleOrdinal",
                "positionId", "nextPositionId", "choice", "side",
                "actingElementId", "contactHandle", "apparentLocationId",
                "boundRepresentationId", "actionId");
            if (root.GetProperty("eventType").GetString()
                != "cycle-repeat-combat-selection-decided")
                throw new JsonException("Unknown repeated Combat event.");
            var parsed = new CampaignNativeRepeatCombatDecisionV31(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!,
                root.GetProperty("cycleOrdinal").GetInt32(),
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("choice").GetString()!,
                CampaignSnapshotSerializer.ParseSide(root.GetProperty("side").GetString()),
                root.GetProperty("actingElementId").GetString()!,
                root.GetProperty("contactHandle").GetString(),
                root.GetProperty("apparentLocationId").GetString(),
                root.GetProperty("boundRepresentationId").GetString(),
                root.GetProperty("actionId").GetString()!);
            if (!canonicalEvent.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical repeated Combat event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or KeyNotFoundException
            or FormatException)
        {
            throw new JsonException("Invalid repeated Combat selection.",
                exception);
        }
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9')
            or (>= 'a' and <= 'f'));
}
