using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

// The second-cycle Combat cut reads the accepted World and contact journal.
// It never constructs a first-cycle V11 snapshot from later World authority.
internal sealed class CampaignRepeatCombatOwnerCutV31
{
    private CampaignRepeatCombatOwnerCutV31(long authorityVersion,
        CampaignContactObservationV1 observation,
        ObservedOwnElement actingElement,
        IReadOnlyList<SelectContactAssaultAction> assaultOffers,
        FinishCombatWithoutAttackAction finishOffer,
        IReadOnlyList<CampaignObservationLocation> locations,
        IReadOnlyList<CampaignObservationEdge> edges)
    {
        AuthorityVersion = authorityVersion;
        Observation = observation;
        ActingElement = actingElement;
        AssaultOffers = assaultOffers;
        FinishOffer = finishOffer;
        VisibleBytes = SerializeVisible(observation, actingElement, assaultOffers,
            finishOffer, locations, edges);
    }

    internal long AuthorityVersion { get; }
    internal CampaignContactObservationV1 Observation { get; }
    internal ObservedOwnElement ActingElement { get; }
    internal IReadOnlyList<SelectContactAssaultAction> AssaultOffers { get; }
    internal FinishCombatWithoutAttackAction FinishOffer { get; }
    internal byte[] VisibleBytes { get; }

    internal static CampaignRepeatCombatOwnerCutV31 Project(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignNativeCycleControlV28 control,
        CampaignNativeRepeatMovementV29 movement,
        CampaignNativeRepeatBreakdownV30 breakdown,
        CampaignWorldSnapshotV6 movedWorld,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        string actingElementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actingElementId);
        if (context.ArtifactV6 is not { } artifact
            || contacts.CampaignId != release.CampaignId
            || contacts.AuthorityVersion != breakdown.StateVersion)
            throw new InvalidOperationException("Repeated Combat needs accepted contact authority.");
        _ = control.SerializeCanonical();
        _ = movement.SerializeCanonical();
        _ = breakdown.SerializeCanonical();
        var movementPosition = Cna1979LandSequenceV4.CreateTurn(
            release.Position.GameTurn).Single(value =>
                value.PositionId == movement.PositionId);
        var breakdownPosition = Cna1979LandSequenceV4.GetNext(movementPosition);
        var combatPosition = Cna1979LandSequenceV4.GetNext(breakdownPosition);
        if (control.Choice != CampaignNativeCycleControlChoiceV28.Repeat
            || control.CampaignId != release.CampaignId
            || control.PriorStateVersion != release.StateVersion
            || control.PriorEventHash != release.HeadEventHash
            || movement.PriorStateVersion != control.StateVersion
            || movement.PriorEventHash != Hash(control.SerializeCanonical())
            || movement.PriorWorldHash
                != CampaignNativeCombatPaidWorldV22.Hash(release.World)
            || movement.ResultWorldHash
                != CampaignNativeCombatPaidWorldV22.Hash(movedWorld)
            || movement.CycleOrdinal != control.NextCycleOrdinal
            || movement.CampaignId != release.CampaignId
            || breakdown.CampaignId != release.CampaignId
            || breakdown.PriorStateVersion != movement.StateVersion
            || breakdown.CycleOrdinal != movement.CycleOrdinal
            || breakdown.PriorEventHash != Hash(movement.SerializeCanonical())
            || breakdown.WorldHash != CampaignNativeCombatPaidWorldV22.Hash(movedWorld)
            || breakdown.PositionId != breakdownPosition.PositionId
            || breakdown.NextPositionId != combatPosition.PositionId
            || combatPosition.SegmentId != LandSegmentIds.Combat
            || combatPosition.StepId != LandStepIds.PositionDetermination)
            throw new InvalidCampaignHistoryException(
                "Repeated Combat does not follow accepted Breakdown.");

        var side = release.ActingSide;
        var definition = artifact.Definition.LegacyDefinition;
        var content = definition.Elements.SingleOrDefault(element =>
            element.ElementId == actingElementId
            && element.SideId == CampaignSnapshotSerializer.FormatSide(side)
            && element.PlacementMode == ContentPlacementMode.Independent)
            ?? throw new InvalidOperationException("Combat actor must belong to the acting side.");
        var state = movedWorld.Elements.Single(element =>
            element.ElementId == actingElementId);
        var own = CampaignOwnElementProjection.Project(content, state);
        var observation = contacts.ObserveFor(side, content.ParentFormationId);
        var locations = definition.Locations.Select(location =>
            new CampaignObservationLocation(location.LocationId,
                location.TerrainId)).ToArray();
        var edges = definition.Edges.Select(edge => new CampaignObservationEdge(
            edge.FirstLocationId, edge.SecondLocationId,
            edge.Features.Select(feature => new CampaignObservationEdgeFeature(
                feature.FeatureId, feature.DirectionFromLocationId)).ToArray())).ToArray();
        var offers = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            actingElementId, content.ParentFormationId, own.CurrentLocationId,
            edges, observation.CurrentTargets);
        // Certified V6 World has no positive ZOC source (Land 10.11). The
        // same voluntary-finish rule applies at every cycle of this profile.
        if (CampaignElementMovedV3Factory.DeriveControlledLocationIds(
                movedWorld, artifact, context.Scenario,
                side == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis).Count != 0)
            throw new InvalidOperationException("Repeated Combat needs a certified forced-assault rule.");
        return new(breakdown.StateVersion, observation, own,
            Array.AsReadOnly(offers),
            new FinishCombatWithoutAttackAction(actingElementId),
            Array.AsReadOnly(locations), Array.AsReadOnly(edges));
    }

    private static byte[] SerializeVisible(
        CampaignContactObservationV1 observation,
        ObservedOwnElement actingElement,
        IReadOnlyList<SelectContactAssaultAction> assaultOffers,
        FinishCombatWithoutAttackAction finishOffer,
        IReadOnlyList<CampaignObservationLocation> locations,
        IReadOnlyList<CampaignObservationEdge> edges)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 31);
            writer.WriteString("campaignId", observation.CampaignId);
            writer.WriteString("side", observation.Side == LandSide.Axis
                ? "axis" : "commonwealth");
            writer.WriteString("actingFormationId", observation.ActingFormationId);
            writer.WriteNumber("contactAudienceRevision",
                observation.AudienceRevision);
            writer.WritePropertyName("contactObservation");
            writer.WriteRawValue(observation.SerializeCanonical());
            CampaignObservationSerializer.WriteLocations(writer, locations);
            CampaignObservationSerializer.WriteEdges(writer, edges);
            CampaignObservationSerializer.WriteOwnElements(writer, [actingElement]);
            writer.WriteStartArray("assaultOffers");
            foreach (var offer in assaultOffers.OrderBy(value =>
                         value.ContactHandle, StringComparer.Ordinal)
                         .ThenBy(value => value.ApparentLocationId,
                             StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("kind", "select-contact-assault");
                writer.WriteString("actingElementId", offer.ActingElementId);
                writer.WriteString("contactHandle", offer.ContactHandle);
                writer.WriteString("apparentLocationId", offer.ApparentLocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartObject("finishOffer");
            writer.WriteString("kind", "finish-combat-without-attack");
            writer.WriteString("actingElementId", finishOffer.ActingElementId);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
}
