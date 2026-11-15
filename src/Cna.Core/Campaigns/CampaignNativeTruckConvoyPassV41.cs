using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// The owner may decline convoy movement; this does not certify that no trucks exist.
internal sealed record CampaignNativeTruckConvoyPassV41(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    LandSide Side, string PositionId, string NextPositionId, string WorldHash)
{
    internal const int CurrentContractVersion = 41;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeTruckConvoyPassV41 Create(
        CampaignNativeReserveReleaseStateV27 release, CampaignNativeCycleControlV28 control,
        LandSide side, long contactVersion)
    {
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(control);
        if (!release.Completed || side != release.ActingSide
            || control.Choice != CampaignNativeCycleControlChoiceV28.Finish
            || control.CampaignId != release.CampaignId || control.PriorStateVersion != release.StateVersion
            || control.PriorEventHash != release.HeadEventHash
            || control.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(release.World)
            || contactVersion != control.StateVersion)
            throw new InvalidOperationException("Truck Convoy pass requires the finished owner cycle.");
        var position = Cna1979LandSequenceV4.CreateTurn(release.Position.GameTurn)
            .Single(value => value.PositionId == control.NextPositionId);
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (position.PhaseId != LandPhaseIds.TruckConvoyMovement
            || position.ActorRole != release.Position.ActorRole
            || position.GameTurn != release.Position.GameTurn
            || position.OperationStage != release.Position.OperationStage
            || next.PhaseId != LandPhaseIds.CommonwealthRailMovement)
            throw new InvalidOperationException("Truck Convoy pass requires its exact catalog successor.");
        return new(CurrentContractVersion, release.CampaignId, control.StateVersion,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(control.SerializeCanonical())),
            side, position.PositionId, next.PositionId, control.ResultWorldHash);
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1 || !Enum.IsDefined(Side)
            || !IsHash(PriorEventHash) || !IsHash(WorldHash))
            throw new JsonException("Invalid Truck Convoy pass.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "truck-convoy-movement-passed");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteString("side", CampaignSnapshotSerializer.FormatSide(Side));
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("worldHash", WorldHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeTruckConvoyPassV41 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Invalid event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorEventHash", "side", "positionId", "nextPositionId", "worldHash");
            if (root.GetProperty("eventType").GetString() != "truck-convoy-movement-passed")
                throw new JsonException("Invalid event type.");
            var side = root.GetProperty("side").GetString() switch
            {
                "axis" => LandSide.Axis,
                "commonwealth" => LandSide.Commonwealth,
                _ => throw new JsonException("Invalid side."),
            };
            var parsed = new CampaignNativeTruckConvoyPassV41(root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!, root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!, side, root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!, root.GetProperty("worldHash").GetString()!);
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical())) throw new JsonException("Noncanonical event.");
            return parsed;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        {
            throw new JsonException("Invalid Truck Convoy pass.", error);
        }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
