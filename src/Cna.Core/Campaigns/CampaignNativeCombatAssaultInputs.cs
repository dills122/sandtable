using System.Security.Cryptography;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Private inputs to the normal Close Assault result. The public owner cuts never
// include these facts. Every value is re-derived from sealed campaign authority.
internal sealed record CampaignNativeCombatAssaultRoleInputs(
    string ElementId, string ComponentId, int CommittedToe,
    int BasicMorale, int OffensiveRating, int DefensiveRating,
    CapabilityPointAmount CapabilityPointsBefore,
    CapabilityPointAmount CapabilityPointsAfter,
    int AmmunitionBefore, int AmmunitionAfter);

internal sealed record CampaignNativeCombatAssaultInputs(
    string ContentHash, string SourceHash, string RulesInputHash,
    string CompletionHash, string AntiArmorHash, string PriorAmmunitionHash,
    string PriorWorldHash, string PaidWorldHash,
    CampaignNativeCombatAssaultRoleInputs Attacker,
    CampaignNativeCombatAssaultRoleInputs Defender,
    CampaignCombatAmmunitionLedgerV1 AfterAmmunition,
    CampaignWorldSnapshotV6 PaidWorld)
{
    internal static CampaignNativeCombatAssaultInputs Create(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatForceCompletionV18 completion,
        CampaignNativeCombatAntiArmorV20 antiArmor,
        IReadOnlyList<CampaignNativeCombatForceSealV17> seals,
        CampaignWorldSnapshotV6 world,
        CampaignContentContext context,
        CampaignCombatAmmunitionLedgerV1 ammunition)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentNullException.ThrowIfNull(antiArmor);
        ArgumentNullException.ThrowIfNull(seals);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(ammunition);
        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException("Normal assault requires Content 6.");
        var source = context.CombatSource
            ?? throw new InvalidOperationException("Normal assault requires pinned Combat facts.");
        if (!CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario)
            || source.ContentHash != artifact.Identity.Hash
            || ammunition.ContentHash != artifact.Identity.Hash
            || ammunition.SourceHash != source.Hash
            || seals.Count != 2
            || seals[0].OwnerSide == seals[1].OwnerSide
            || completion.CampaignId != selected.CampaignId
            || completion.DecisionActionId != selected.Decision.ActionId
            || completion.PriorStateVersion != seals[1].StateVersion
            || antiArmor.CampaignId != selected.CampaignId
            || antiArmor.DecisionActionId != selected.Decision.ActionId
            || antiArmor.PriorStateVersion != completion.StateVersion
            || antiArmor.Disposition != "no-effect"
            || antiArmor.PriorForceEventHash != Hash(completion.SerializeCanonical()))
            throw new InvalidOperationException("Assault lacks retained normal campaign authority.");
        var attackerSeal = seals.Single(value => value.Role == "attacker");
        var defenderSeal = seals.Single(value => value.Role == "defender");
        if (attackerSeal.OwnerSide != selected.Decision.Side
            || defenderSeal.OwnerSide == selected.Decision.Side
            || completion.AttackerSealHash != Hash(attackerSeal.SerializeCanonical())
            || completion.DefenderSealHash != Hash(defenderSeal.SerializeCanonical()))
            throw new InvalidOperationException("Assault seals differ from Force completion.");

        var costs = Cna1979CombatAdjudication.Definition.Costs;
        var attacker = Role(attackerSeal, costs.AttackerCapabilityPoints);
        var defender = Role(defenderSeal, costs.DefenderCapabilityPoints);
        var afterAmmunition = ammunition.Spend(attacker.ElementId,
            checked(attacker.CommittedToe * costs.AmmunitionPointsPerToe));
        afterAmmunition = afterAmmunition.Spend(defender.ElementId,
            checked(defender.CommittedToe * costs.AmmunitionPointsPerToe));
        var paidWorld = CampaignNativeCombatPaidWorldV22.Project(world, attacker, defender);
        if (!CampaignWorldV6Validator.IsValid(paidWorld, artifact, context.Scenario))
            throw new InvalidOperationException("Assault costs produced an invalid normal World.");
        return new(artifact.Identity.Hash, source.Hash,
            Cna1979CombatAdjudication.CreateArtifact().ContentHash,
            Hash(completion.SerializeCanonical()), Hash(antiArmor.SerializeCanonical()),
            ammunition.Hash, CampaignNativeCombatPaidWorldV22.Hash(world),
            CampaignNativeCombatPaidWorldV22.Hash(paidWorld),
            attacker, defender, afterAmmunition, paidWorld);

        CampaignNativeCombatAssaultRoleInputs Role(CampaignNativeCombatForceSealV17 seal,
            int capabilityCost)
        {
            var element = artifact.Definition.LegacyDefinition.Elements.Single(value =>
                value.ElementId == seal.ElementId);
            var state = world.Elements.Single(value => value.ElementId == seal.ElementId);
            var component = artifact.Definition.ElementCombatFacts.Single(value =>
                value.ElementId == seal.ElementId).Components.Single(value =>
                value.ComponentId == seal.ComponentId);
            var currentToe = state.Components.Single(value =>
                value.ComponentId == seal.ComponentId).CurrentToe;
            var before = state.OperationalState.CapabilityPointsExpended;
            var after = before + new CapabilityPointAmount(capabilityCost, 1);
            var ammoCost = checked(seal.CommittedToe * costs.AmmunitionPointsPerToe);
            var ammoBefore = ammunition.Remaining(seal.ElementId);
            if (element.SideId != CampaignSnapshotSerializer.FormatSide(seal.OwnerSide)
                || seal.CommittedToe != costs.CommittedToePerRole
                || currentToe != seal.CommittedToe
                || source.BasicMorale(element.ParentFormationId) != costs.BasicMoralePerRole
                || source.OffensiveRating(seal.ComponentId) != costs.RatingPerRole
                || component.DefensiveCloseAssaultRating != costs.RatingPerRole
                || state.OperationalState.CohesionLevel != costs.RequiredCohesionPerRole
                || element.BaseCapabilityPointAllowance < costs.BaseCapabilityPointAllowance
                || after > new CapabilityPointAmount(element.BaseCapabilityPointAllowance, 1)
                || ammoBefore < ammoCost)
                throw new InvalidOperationException("Assault role lacks source-backed resources.");
            return new(seal.ElementId, seal.ComponentId, seal.CommittedToe,
                source.BasicMorale(element.ParentFormationId),
                source.OffensiveRating(seal.ComponentId), component.DefensiveCloseAssaultRating,
                before, after, ammoBefore, ammoBefore - ammoCost);
        }
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
}
