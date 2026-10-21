using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// A later-cycle voluntary finish advances the same five no-work Combat
// positions as the first cycle. It binds the accepted V31 choice and actual
// moved World; it never recreates an initial campaign snapshot.
internal sealed record CampaignNativeRepeatCombatNoAttackV32(
    int ContractVersion, string CampaignId, long PriorStateVersion,
    string PriorSelectionHash, int CycleOrdinal,
    string PositionId, string NextPositionId, string WorldHash,
    IReadOnlyList<CampaignNativeCombatNoAttackStepV13> Steps)
{
    internal const int CurrentContractVersion = 32;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeRepeatCombatNoAttackV32 Create(
        CampaignNativeRepeatCombatDecisionV31 selected,
        CampaignCombatSelectionFrame frame,
        CampaignContentContext context)
    {
        if (!frame.IsRepeat || frame.CampaignId != selected.CampaignId
            || frame.StateVersion != selected.StateVersion
            || frame.Decision.ActionId != selected.ActionId
            || frame.Decision.Choice != selected.Choice
            || !frame.CanonicalSelection.AsSpan().SequenceEqual(
                selected.SerializeCanonical()))
            throw new InvalidOperationException(
                "Later-cycle finish lacks accepted selection authority.");
        var completion = CampaignNativeCombatNoAttackCompletionV13.Create(
            frame, context);
        return new(CurrentContractVersion, selected.CampaignId,
            selected.StateVersion, Hash(selected.SerializeCanonical()),
            selected.CycleOrdinal, selected.NextPositionId,
            completion.NextPositionId,
            CampaignNativeCombatPaidWorldV22.Hash(frame.World),
            completion.Steps);
    }

    internal static CampaignNativeRepeatCombatNoAttackV32 Replay(
        CampaignNativeRepeatCombatDecisionV31 selected,
        CampaignCombatSelectionFrame frame,
        CampaignContentContext context,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Create(selected, frame, context);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException(
                "Later-cycle finish differs from accepted Combat selection.");
        return expected;
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion
            || PriorStateVersion < 1 || CycleOrdinal < 2
            || !IsHash(PriorSelectionHash) || !IsHash(WorldHash)
            || Steps.Count != 5 || Steps[0].PositionId != PositionId
            || Steps[^1].NextPositionId != NextPositionId)
            throw new JsonException("Invalid later-cycle no-attack event.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "cycle-repeat-combat-no-attack-completed");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorSelectionHash", PriorSelectionHash);
            writer.WriteNumber("cycleOrdinal", CycleOrdinal);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(
                PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(
                NextPositionId, nameof(NextPositionId)));
            writer.WriteString("worldHash", WorldHash);
            writer.WriteStartArray("steps");
            foreach (var step in Steps)
            {
                writer.WriteStartObject();
                writer.WriteString("positionId", ContentContractGuards.RequireStableId(
                    step.PositionId, nameof(Steps)));
                writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(
                    step.NextPositionId, nameof(Steps)));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeRepeatCombatNoAttackV32 Deserialize(
        ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            if (canonicalEvent.Length is < 1 or > 8192)
                throw new JsonException("Later-cycle no-attack event exceeds byte bound.");
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root,
                "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorSelectionHash", "cycleOrdinal",
                "positionId", "nextPositionId", "worldHash", "steps");
            if (root.GetProperty("eventType").GetString()
                != "cycle-repeat-combat-no-attack-completed")
                throw new JsonException("Unknown later-cycle Combat event.");
            var steps = root.GetProperty("steps").EnumerateArray().Select(step =>
            {
                CampaignSnapshotSerializer.RequireProperties(step,
                    "positionId", "nextPositionId");
                return new CampaignNativeCombatNoAttackStepV13(
                    step.GetProperty("positionId").GetString()!,
                    step.GetProperty("nextPositionId").GetString()!);
            }).ToArray();
            var parsed = new CampaignNativeRepeatCombatNoAttackV32(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorSelectionHash").GetString()!,
                root.GetProperty("cycleOrdinal").GetInt32(),
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("worldHash").GetString()!, steps);
            if (!canonicalEvent.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical later-cycle Combat event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or KeyNotFoundException
            or FormatException)
        {
            throw new JsonException("Invalid later-cycle Combat event.", exception);
        }
    }

    internal byte[] SerializeStateCanonical(ReadOnlySpan<byte> priorState)
    {
        if (priorState.IsEmpty)
            throw new ArgumentException("Later-cycle finish requires a retained selection.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("positionId", NextPositionId);
            writer.WriteString("worldHash", WorldHash);
            writer.WriteString("priorStateHash", Hash(priorState));
            writer.WritePropertyName("completion");
            writer.WriteRawValue(SerializeCanonical());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9')
            or (>= 'a' and <= 'f'));
}
