using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

// Private Core candidate for the normal two-owner Force Assignment. Only the
// requesting owner's own allocation and contact knowledge appear in this cut.
internal sealed record CampaignContactForceAssignmentCut(
    long AuthorityVersion,
    string PositionId,
    LandSide OwnerSide,
    string Role,
    ObservedOwnElement OwnElement,
    CampaignContactObservationV1 ContactObservation,
    SealCombatForceAssignmentAction SealOffer)
{
    public static CampaignContactForceAssignmentCut Project(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatRetreatDeclineV15 decline,
        CampaignWorldSnapshotV6 world,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        LandSide ownerSide)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(decline);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException("Force Assignment requires Content 6 authority.");
        if (selected.Decision.Choice != "select-contact-assault"
            || decline.CampaignId != selected.CampaignId
            || decline.DecisionActionId != selected.Decision.ActionId
            || decline.PriorStateVersion < selected.StateVersion
            || contacts.CampaignId != selected.CampaignId
            || contacts.AuthorityVersion < decline.StateVersion
            || contacts.AuthorityVersion > decline.StateVersion + 1
            || !CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario))
            throw new InvalidOperationException("Force Assignment lacks retained Combat authority.");
        var boundDefender = world.Representations.SingleOrDefault(value =>
            value.RepresentationId == selected.Decision.BoundRepresentationId);
        if (boundDefender is null || boundDefender.BoundElementIds.Count != 1
            || boundDefender.BoundElementIds[0] != decline.DefenderElementId)
            throw new InvalidOperationException("Force Assignment defender binding differs from selection.");
        var position = Cna1979LandSequenceV4.CreateTurn(
                selected.PriorSnapshot.CurrentPosition.SequencePosition!.GameTurn)
            .SingleOrDefault(candidate => candidate.PositionId == decline.NextPositionId);
        if (position is null || position.SegmentId != LandSegmentIds.Combat
            || position.StepId != LandStepIds.ForceAssignment)
            throw new InvalidOperationException("Force Assignment has the wrong sequence position.");

        var role = ownerSide == selected.Decision.Side ? "attacker"
            : ownerSide == decline.DefenderSide ? "defender"
            : throw new InvalidOperationException("Force Assignment has no slot for this owner.");
        if (decline.DefenderSide == selected.Decision.Side)
            throw new InvalidOperationException("Selected attacker and defender cannot share a side.");
        var elementId = role == "attacker"
            ? selected.Decision.ActingElementId : decline.DefenderElementId;
        var definition = artifact.Definition.LegacyDefinition.Elements
            .Single(value => value.ElementId == elementId);
        var expectedSide = ownerSide == LandSide.Axis ? "axis" : "commonwealth";
        if (definition.SideId != expectedSide)
            throw new InvalidOperationException("Force Assignment slot has the wrong owner.");
        var state = world.Elements.Single(value => value.ElementId == elementId);
        var combatFacts = artifact.Definition.ElementCombatFacts
            .Single(value => value.ElementId == elementId);
        if (combatFacts.Components.Count != 1)
            throw new InvalidOperationException("Force Assignment needs one supported combat component.");
        var component = combatFacts.Components[0];
        var toe = state.Components.Single(value => value.ComponentId == component.ComponentId);
        var committedToe = toe.CurrentToe;
        if (component.ComponentClassId != Cna1979Combat.InfantryComponentClassId
            || committedToe <= 0 || committedToe > component.MaximumToe)
            throw new InvalidOperationException("Force Assignment needs a supported full-TOE slot.");
        var own = CampaignOwnElementProjection.Project(definition, state);
        var knowledge = contacts.ObserveFor(ownerSide, definition.ParentFormationId);
        return new(decline.StateVersion, position.PositionId, ownerSide,
            role, own, knowledge,
            new SealCombatForceAssignmentAction(role, elementId,
                component.ComponentId, committedToe));
    }
}
