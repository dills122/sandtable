using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// The selected contact is an offer, not a promise that hidden opposing resources
// can pay the pinned close-assault chart. Test both roles only after selection.
internal static class CampaignNativeCombatForceEligibility
{
    internal static bool CanCompleteAssault(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatRetreatDeclineV15 decline,
        CampaignWorldSnapshotV6 world,
        CampaignContentContext context,
        IEnumerable<object> acceptedEvents)
    {
        try
        {
            var artifact = context.ArtifactV6;
            var source = context.CombatSource;
            if (artifact is null || source is null
                || source.ContentHash != artifact.Identity.Hash
                || selected.Decision.Choice != "select-contact-assault"
                || decline.CampaignId != selected.CampaignId
                || decline.DecisionActionId != selected.Decision.ActionId
                || !CampaignWorldV6Validator.IsValid(world, artifact,
                    context.Scenario))
                return false;
            var ammunition = CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(
                context, acceptedEvents);
            if (ammunition.ContentHash != artifact.Identity.Hash
                || ammunition.SourceHash != source.Hash)
                return false;

            var defender = world.Representations.SingleOrDefault(value =>
                value.RepresentationId == selected.Decision.BoundRepresentationId);
            if (defender is null || defender.BoundElementIds.Count != 1
                || defender.BoundElementIds[0] != decline.DefenderElementId)
                return false;

            var costs = Cna1979CombatAdjudication.Definition.Costs;
            return Role(selected.Decision.ActingElementId,
                       selected.Decision.Side, costs.AttackerCapabilityPoints)
                && Role(decline.DefenderElementId,
                    decline.DefenderSide, costs.DefenderCapabilityPoints);

            bool Role(string elementId, LandSide side, int capabilityCost)
            {
                var element = artifact.Definition.LegacyDefinition.Elements.Single(
                    value => value.ElementId == elementId);
                var state = world.Elements.Single(value => value.ElementId == elementId);
                var facts = artifact.Definition.ElementCombatFacts.Single(
                    value => value.ElementId == elementId);
                if (facts.Components.Count != 1 || state.Components.Count != 1)
                    return false;
                var component = facts.Components[0];
                var toe = state.Components[0];
                var after = state.OperationalState.CapabilityPointsExpended
                    + new CapabilityPointAmount(capabilityCost, 1);
                return element.SideId == CampaignSnapshotSerializer.FormatSide(side)
                    && component.ComponentClassId
                        == Cna1979Combat.InfantryComponentClassId
                    && toe.ComponentId == component.ComponentId
                    && toe.CurrentToe == costs.CommittedToePerRole
                    && source.BasicMorale(element.ParentFormationId)
                        == costs.BasicMoralePerRole
                    && source.OffensiveRating(component.ComponentId)
                        == costs.RatingPerRole
                    && component.DefensiveCloseAssaultRating
                        == costs.RatingPerRole
                    && state.OperationalState.CohesionLevel
                        == costs.RequiredCohesionPerRole
                    && element.BaseCapabilityPointAllowance
                        >= costs.BaseCapabilityPointAllowance
                    && after <= new CapabilityPointAmount(
                        element.BaseCapabilityPointAllowance, 1)
                    && ammunition.Remaining(elementId)
                        >= costs.CommittedToePerRole
                            * costs.AmmunitionPointsPerToe;
            }
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or KeyNotFoundException
            or ArithmeticException)
        {
            return false;
        }
    }
}
