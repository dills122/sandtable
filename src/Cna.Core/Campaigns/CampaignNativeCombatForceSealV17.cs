using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// Each owner seals only its own allocation. The second seal advances the same
// Combat slot; the first cannot disclose the other owner's allocation.
internal sealed record CampaignNativeCombatForceSealV17(
    int ContractVersion,
    string CampaignId,
    long PriorStateVersion,
    string DecisionActionId,
    string PositionId,
    string NextPositionId,
    string Role,
    LandSide OwnerSide,
    string ElementId,
    string ComponentId,
    int CommittedToe,
    string ActionId,
    string? PriorSealHash)
{
    internal const int CurrentContractVersion = 17;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeCombatForceSealV17 Create(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatRetreatDeclineV15 decline,
        CampaignWorldSnapshotV6 world,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        LandSide ownerSide,
        string actionId,
        CampaignNativeCombatForceSealV17? firstSeal = null)
    {
        if (firstSeal is null && contacts.AuthorityVersion != decline.StateVersion)
            throw new InvalidOperationException("First seal requires the retained Force Assignment cut.");
        if (firstSeal is not null && (firstSeal.CampaignId != selected.CampaignId
            || firstSeal.PriorStateVersion != decline.StateVersion
            || firstSeal.DecisionActionId != selected.Decision.ActionId
            || firstSeal.OwnerSide == ownerSide
            || contacts.AuthorityVersion != firstSeal.StateVersion))
            throw new InvalidOperationException("Second seal requires the opposing retained first seal.");
        var cut = CampaignContactForceAssignmentCut.Project(selected, decline,
            world, context, contacts, ownerSide);
        if (cut.SealOffer.ActionId != actionId)
            throw new InvalidOperationException("Force Assignment seal is not offered to this owner.");
        var position = Cna1979LandSequenceV4.CreateTurn(
                selected.PriorSnapshot.CurrentPosition.SequencePosition!.GameTurn)
            .Single(value => value.PositionId == decline.NextPositionId);
        var nextPositionId = position.PositionId;
        return new(CurrentContractVersion, selected.CampaignId,
            firstSeal?.StateVersion ?? decline.StateVersion,
            selected.Decision.ActionId, position.PositionId, nextPositionId,
            cut.Role, ownerSide, cut.SealOffer.ElementId,
            cut.SealOffer.ComponentId, cut.SealOffer.CommittedToe,
            actionId, firstSeal is null ? null : Hash(firstSeal.SerializeCanonical()));
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || Role is not ("attacker" or "defender") || !Enum.IsDefined(OwnerSide)
            || CommittedToe <= 0 || ActionId.Length != 71
            || (PriorSealHash is not null && !PriorSealHash.StartsWith("sha256:", StringComparison.Ordinal)))
            throw new JsonException("Invalid Force Assignment seal.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-force-sealed");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("role", Role);
            writer.WriteString("ownerSide", CampaignSnapshotSerializer.FormatSide(OwnerSide));
            writer.WriteString("elementId", ContentContractGuards.RequireStableId(ElementId, nameof(ElementId)));
            writer.WriteString("componentId", ContentContractGuards.RequireStableId(ComponentId, nameof(ComponentId)));
            writer.WriteNumber("committedToe", CommittedToe);
            writer.WriteString("actionId", ActionId);
            if (PriorSealHash is not null) writer.WriteString("priorSealHash", PriorSealHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeCombatForceSealV17 Replay(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatRetreatDeclineV15 decline,
        CampaignWorldSnapshotV6 world,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        ReadOnlyMemory<byte> canonicalEvent,
        CampaignNativeCombatForceSealV17? firstSeal = null)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Create(selected, decline, world, context, contacts,
            parsed.OwnerSide, parsed.ActionId, firstSeal);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException("Force Assignment seal differs from owner authority.");
        return expected;
    }

    internal static CampaignNativeCombatForceSealV17 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            var second = root.TryGetProperty("priorSealHash", out var priorHash);
            CampaignSnapshotSerializer.RequireProperties(root, second
                ? ["contractVersion", "eventType", "campaignId", "priorStateVersion",
                    "decisionActionId", "positionId", "nextPositionId", "role",
                    "ownerSide", "elementId", "componentId", "committedToe", "actionId", "priorSealHash"]
                : ["contractVersion", "eventType", "campaignId", "priorStateVersion",
                    "decisionActionId", "positionId", "nextPositionId", "role",
                    "ownerSide", "elementId", "componentId", "committedToe", "actionId"]);
            if (root.GetProperty("eventType").GetString() != "combat-force-sealed")
                throw new JsonException("Unknown Force Assignment event.");
            var parsed = new CampaignNativeCombatForceSealV17(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("decisionActionId").GetString()!,
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("role").GetString()!,
                CampaignSnapshotSerializer.ParseSide(root.GetProperty("ownerSide").GetString()),
                root.GetProperty("elementId").GetString()!,
                root.GetProperty("componentId").GetString()!,
                root.GetProperty("committedToe").GetInt32(),
                root.GetProperty("actionId").GetString()!,
                second ? priorHash.GetString() : null);
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical Force Assignment event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        {
            throw new JsonException("Invalid Force Assignment event.", exception);
        }
    }

    internal byte[] SerializeStateCanonical(ReadOnlySpan<byte> priorState)
    {
        if (priorState.IsEmpty) throw new ArgumentException("Force Assignment needs retained authority.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("positionId", NextPositionId);
            writer.WriteString("priorStateHash", Hash(priorState));
            writer.WritePropertyName("seal");
            writer.WriteRawValue(SerializeCanonical());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
}
