using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// #394 gap 1/extra-2 (Option A for gap 3): the second-acting side's own first
// Combat decision after their own Breakdown (V46) completes. Mirrors
// CampaignNativeCombatDecisionV12/CampaignNativeCombatDecisionStateV12's
// shape, but -- following CampaignNativeSecondMoveV44's existing precedent
// for the same architectural problem -- is keyed to a (CampaignWorldSnapshotV6,
// LandSequencePosition) pair instead of a CampaignSnapshotV11. A direct
// CampaignSnapshotV11 pointing at the second side's post-breakdown
// Combat/PositionDetermination position is rejected by
// Cna1979LandSequenceV4.IsSupportedCheckpoint (confirmed by testing): that
// catalog walk stops considering anything a "supported checkpoint" once it
// reaches the *first*-acting-side's own PositionDetermination, so no
// CampaignSnapshotV11 can ever legally represent the second side's Combat
// position. This type exists so second-side Combat selection never needs to
// construct one.
internal record CampaignNativeSecondCombatDecisionV47(
    int ContractVersion,
    string CampaignId,
    long PriorStateVersion,
    string PositionId,
    string NextPositionId,
    string Choice,
    LandSide Side,
    string ActingElementId,
    string? ContactHandle,
    string? ApparentLocationId,
    string? BoundRepresentationId,
    string ActionId)
{
    internal const int CurrentContractVersion = 47;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal virtual byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !Enum.IsDefined(Side))
            throw new JsonException("Invalid native second Combat selection event.");
        _ = ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId));
        _ = ContentContractGuards.RequireStableId(PositionId, nameof(PositionId));
        _ = ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId));
        _ = ContentContractGuards.RequireStableId(ActingElementId, nameof(ActingElementId));
        if (Choice is not ("select-contact-assault" or "finish-combat-without-attack"))
            throw new JsonException("Invalid native second Combat choice.");
        if (Choice == "select-contact-assault")
        {
            _ = ContentContractGuards.RequireStableId(ContactHandle!, nameof(ContactHandle));
            _ = ContentContractGuards.RequireStableId(ApparentLocationId!, nameof(ApparentLocationId));
            _ = ContentContractGuards.RequireStableId(BoundRepresentationId!, nameof(BoundRepresentationId));
        }
        else if (ContactHandle is not null || ApparentLocationId is not null
            || BoundRepresentationId is not null)
            throw new JsonException("A voluntary finish has no target binding.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "second-combat-selection-decided");
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("positionId", PositionId);
            writer.WriteString("nextPositionId", NextPositionId);
            writer.WriteString("choice", Choice);
            writer.WriteString("side", CampaignSnapshotSerializer.FormatSide(Side));
            writer.WriteString("actingElementId", ActingElementId);
            writer.WriteString("contactHandle", ContactHandle);
            writer.WriteString("apparentLocationId", ApparentLocationId);
            writer.WriteString("boundRepresentationId", BoundRepresentationId);
            writer.WriteString("actionId", ActionId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeSecondCombatDecisionV47 Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "positionId", "nextPositionId", "choice", "side",
                "actingElementId", "contactHandle", "apparentLocationId", "boundRepresentationId", "actionId");
            if (root.GetProperty("eventType").GetString() != "second-combat-selection-decided")
                throw new JsonException("Unknown native second Combat event.");
            var parsed = new CampaignNativeSecondCombatDecisionV47(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("choice").GetString()!,
                CampaignSnapshotSerializer.ParseSide(root.GetProperty("side").GetString()),
                root.GetProperty("actingElementId").GetString()!,
                root.GetProperty("contactHandle").GetString(),
                root.GetProperty("apparentLocationId").GetString(),
                root.GetProperty("boundRepresentationId").GetString(),
                root.GetProperty("actionId").GetString()!);
            if (!canonicalJson.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Native second Combat selection event is not canonical.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException)
        {
            throw new JsonException("Invalid native second Combat selection event.", exception);
        }
    }
}

// Carries just enough fresh, non-stale context (World/RandomState) alongside
// the decision to build a CampaignCombatSelectionFrame, without ever routing
// through CampaignSnapshotV11. `TurnFacts` supplies only the turn-wide,
// position-independent facts CampaignCombatSelectionFrame's own consumers
// actually read from PriorSnapshot for this scope (CampaignId/RulesetHash/
// GameTurn, via CampaignNativeCombatNoAttackCompletionV13.Create) -- it can
// safely be the session's existing (first-side-stale) CampaignSnapshotV11,
// since GameTurn/CampaignId/RulesetHash do not change between sides within
// one turn.
internal sealed record CampaignNativeSecondCombatDecisionStateV47(
    CampaignSnapshotV11 TurnFacts,
    CampaignWorldSnapshotV6 World,
    RandomStreamState RandomState,
    CampaignNativeSecondCombatDecisionV47 Decision)
{
    internal long StateVersion => checked(Decision.PriorStateVersion + 1);
    internal string CampaignId => Decision.CampaignId;

    public static implicit operator CampaignCombatSelectionFrame(
        CampaignNativeSecondCombatDecisionStateV47 selected) =>
        new(selected.TurnFacts, selected.World, selected.RandomState, selected.StateVersion,
            new(selected.Decision.Choice, selected.Decision.Side,
                selected.Decision.ActingElementId,
                selected.Decision.ContactHandle,
                selected.Decision.ApparentLocationId,
                selected.Decision.BoundRepresentationId,
                selected.Decision.ActionId,
                selected.Decision.PositionId,
                selected.Decision.NextPositionId),
            selected.Decision.SerializeCanonical(), false);

    internal static CampaignNativeSecondCombatDecisionStateV47 Select(
        CampaignWorldSnapshotV6 world, LandSequencePosition position, CampaignSnapshotV11 turnFacts,
        RandomStreamState randomState, CampaignContentContext context, CampaignContactJournal contacts,
        LandSide side, string actingElementId, string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        var cut = CampaignContactCombatSelectionCut.ProjectSecondCombat(world, position, context,
            contacts, side, actingElementId);
        var offer = cut.AssaultOffers.SingleOrDefault(candidate => candidate.ActionId == actionId);
        var finish = cut.FinishOffer?.ActionId == actionId;
        if (offer is null && !finish)
            throw new InvalidOperationException("Second Combat selection is not offered at this owner cut.");
        var binding = offer is null ? null : contacts.ResolvePrivateTarget(side,
            cut.OwnerCut.ContactObservation.ActingFormationId, offer.ContactHandle);
        // Cna1979LandSequenceV4.GetNext finds `current` by exact equality against
        // its own freshly-generated catalog, whose relative-role (First/Second
        // ActingSide) entries always carry ActiveSide=null -- it never rebinds
        // a side the way callers do for their own validation (see
        // QuerySecondCombatWorldAndPosition's remarks). Strip the bound side
        // back off before asking for the next position, or GetNext's linear
        // search never matches and throws.
        var catalogPosition = new LandSequencePosition(position.ContractVersion, position.PositionId,
            position.GameTurn, position.OperationStage, position.StageId, position.PhaseId,
            position.SegmentId, position.StepId, position.ActorRole, null, position.Sources);
        var nextPosition = Cna1979LandSequenceV4.GetNext(catalogPosition);
        if (nextPosition.StepId != LandStepIds.Barrage || nextPosition.SegmentId != LandSegmentIds.Combat)
            throw new InvalidOperationException("Second Combat selection must advance to Barrage.");
        var decision = new CampaignNativeSecondCombatDecisionV47(
            CampaignNativeSecondCombatDecisionV47.CurrentContractVersion, contacts.CampaignId,
            contacts.AuthorityVersion, position.PositionId, nextPosition.PositionId,
            finish ? "finish-combat-without-attack" : "select-contact-assault",
            side, actingElementId, offer?.ContactHandle, offer?.ApparentLocationId, binding, actionId);
        _ = decision.SerializeCanonical();
        return new(turnFacts, world, randomState, decision);
    }
}
