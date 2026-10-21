using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// A retained defender choice is required when the sealed result calls for one
// hex of retreat. Silence cannot become refusal or a chosen route.
internal sealed record CampaignNativeCombatRetreatAfterAssaultV23(
    int ContractVersion, string CampaignId, long PriorStateVersion,
    string DecisionActionId, string PositionId, string ResultHash,
    string PaidWorldHash, string RetreatSourceHash, LandSide DefenderSide,
    string DefenderElementId, string Disposition, string? ActionId,
    string? DestinationLocationId)
{
    internal const int CurrentContractVersion = 23;
    internal long StateVersion => checked(PriorStateVersion + 1);
    internal string NextPositionId => PositionId;

    internal static CampaignNativeCombatRetreatAfterAssaultV23 Create(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatAssaultResultV22 result,
        CampaignWorldSnapshotV6 paidWorld, CampaignContentContext context,
        CampaignContactJournal contacts, string? actionId)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(paidWorld);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        if (result.CampaignId != selected.CampaignId
            || result.DecisionActionId != selected.Decision.ActionId
            || result.PaidWorldHash != CampaignNativeCombatPaidWorldV22.Hash(paidWorld)
            || contacts.CampaignId != result.CampaignId
            || contacts.AuthorityVersion != result.StateVersion)
            throw new InvalidOperationException("Post-result choice lacks retained assault authority.");
        var source = context.CombatRetreatSource
            ?? throw new InvalidOperationException("Post-result choice needs exact-pack retreat rules.");
        var defender = context.ArtifactV6!.Definition.LegacyDefinition.Elements
            .Single(value => value.ElementId == result.DefenderElementId);
        var side = defender.SideId switch
        {
            "axis" => LandSide.Axis,
            "commonwealth" => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Unknown defender side.")
        };
        var disposition = "not-required";
        string? destination = null;
        if (result.Facts.RequiredRetreat == 1)
        {
            var cut = CampaignContactRetreatAfterAssaultCut.Project(selected,
                result, paidWorld, context, contacts);
            if (actionId == cut.RefuseOffer.ActionId)
                disposition = "refused";
            else
            {
                var offered = cut.MoveOffers.SingleOrDefault(value =>
                    value.ActionId == actionId)
                    ?? throw new InvalidOperationException("Defender route was not offered at this cut.");
                disposition = "moved";
                destination = offered.DestinationLocationId;
            }
        }
        else if (actionId is not null)
            throw new InvalidOperationException("Retreat is not required by the sealed result.");
        return new(CurrentContractVersion, result.CampaignId, result.StateVersion,
            result.DecisionActionId, result.PositionId,
            Hash(result.SerializeCanonical()), result.PaidWorldHash, source.Hash,
            side, result.DefenderElementId, disposition, actionId, destination);
    }

    internal static CampaignNativeCombatRetreatAfterAssaultV23 Replay(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatAssaultResultV22 result,
        CampaignWorldSnapshotV6 paidWorld, CampaignContentContext context,
        CampaignContactJournal contacts, ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Create(selected, result, paidWorld, context,
            contacts, parsed.ActionId);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException(
                "Post-result retreat differs from retained defender authority.");
        return expected;
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !Enum.IsDefined(DefenderSide)
            || !IsHash(ResultHash) || !IsHash(PaidWorldHash)
            || !IsHash(RetreatSourceHash)
            || string.IsNullOrWhiteSpace(DecisionActionId)
            || DecisionActionId.Length > 256
            || Disposition is not ("not-required" or "refused" or "moved")
            || (Disposition == "not-required" && (ActionId is not null
                || DestinationLocationId is not null))
            || (Disposition == "refused" && (ActionId is null
                || DestinationLocationId is not null))
            || (Disposition == "moved" && (ActionId is null
                || DestinationLocationId is null))
            || ActionId is { Length: > 256 })
            throw new JsonException("Invalid post-result retreat event.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-post-result-retreat-decided");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(
                PositionId, nameof(PositionId)));
            writer.WriteString("resultHash", ResultHash);
            writer.WriteString("paidWorldHash", PaidWorldHash);
            writer.WriteString("retreatSourceHash", RetreatSourceHash);
            writer.WriteString("defenderSide", CampaignSnapshotSerializer.FormatSide(DefenderSide));
            writer.WriteString("defenderElementId", ContentContractGuards.RequireStableId(
                DefenderElementId, nameof(DefenderElementId)));
            writer.WriteString("disposition", Disposition);
            if (ActionId is null) writer.WriteNull("actionId");
            else writer.WriteString("actionId", ActionId);
            if (DestinationLocationId is null) writer.WriteNull("destinationLocationId");
            else writer.WriteString("destinationLocationId", ContentContractGuards.RequireStableId(
                DestinationLocationId, nameof(DestinationLocationId)));
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal byte[] SerializeStateCanonical(ReadOnlySpan<byte> priorState)
    {
        if (priorState.IsEmpty)
            throw new ArgumentException("Retreat choice requires retained result authority.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("positionId", NextPositionId);
            writer.WriteString("priorStateHash", Hash(priorState));
            writer.WritePropertyName("retreatDecision");
            writer.WriteRawValue(SerializeCanonical());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeCombatRetreatAfterAssaultV23 Deserialize(
        ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion",
                "eventType", "campaignId", "priorStateVersion", "decisionActionId",
                "positionId", "resultHash", "paidWorldHash", "retreatSourceHash",
                "defenderSide", "defenderElementId", "disposition", "actionId",
                "destinationLocationId");
            if (root.GetProperty("eventType").GetString()
                != "combat-post-result-retreat-decided")
                throw new JsonException("Unknown post-result retreat event.");
            var parsed = new CampaignNativeCombatRetreatAfterAssaultV23(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("decisionActionId").GetString()!,
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("resultHash").GetString()!,
                root.GetProperty("paidWorldHash").GetString()!,
                root.GetProperty("retreatSourceHash").GetString()!,
                CampaignSnapshotSerializer.ParseSide(root.GetProperty("defenderSide").GetString()),
                root.GetProperty("defenderElementId").GetString()!,
                root.GetProperty("disposition").GetString()!,
                root.GetProperty("actionId").GetString(),
                root.GetProperty("destinationLocationId").GetString());
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical post-result retreat event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or KeyNotFoundException or FormatException
            or NullReferenceException)
        {
            throw new JsonException("Invalid post-result retreat event.", exception);
        }
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
