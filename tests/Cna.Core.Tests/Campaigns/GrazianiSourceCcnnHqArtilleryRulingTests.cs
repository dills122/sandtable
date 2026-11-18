using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

// RUL-GRA-28 (docs/work/plans/2026-11-12-graziani-ruling-decisions.md): the
// 1st/4th CCNN division HQs (1-ccnn-hq, 4-ccnn-hq) carry one artillery TOE
// point from deployment, with its weapon identity permanently unidentified.
//
// This uses the minimum real, directly-callable Admit chain (mirrors
// GrazianiSourceTobrukBardiaGarrisonTests.CreateThroughAxisGarrisons) rather
// than replaying the giant GrazianiSourceAuthorityTests fixture: that fixture
// can never show "ccnn-characteristics-incomplete" clearing, because its real
// CCNN groups also include 201-ccnn-engineer/204-ccnn-engineer, which remain
// genuinely unresolved and keep tripping the same combined blocker regardless
// of this ruling (see IntegratingEveryAdmittedClusterBeforeOwnerFinalizationRevealsRealRemainingBlockers's
// own "ccnn-characteristics-incomplete remains, as expected" assertion). To
// isolate the ruling's actual effect on just the two HQs, these tests filter
// the real production CCNN groups down to each HQ alone (a valid single-member,
// self-rooted formation) before evaluating the blocker predicate.
public sealed class GrazianiSourceCcnnHqArtilleryRulingTests
{
    [Fact]
    public void BothDivisionHqsCarryOneKnownPointWithAnExplicitUnknownWeapon()
    {
        var groups = CampaignSourceCcnnBatch.Create(CreateThroughAxisSupplement());
        var firstHq = groups.Single(value => value.GroupId == CampaignSourceCcnnBatch.FirstId)
            .Members.Single(value => value.ElementId == "1-ccnn-hq");
        var fourthHq = groups.Single(value => value.GroupId == CampaignSourceCcnnBatch.FourthId)
            .Members.Single(value => value.ElementId == "4-ccnn-hq");
        foreach (var hq in new[] { firstHq, fourthHq })
        {
            Assert.NotNull(hq.CapabilityPointAllowance);
            Assert.NotNull(hq.ComponentType);
            Assert.Equal(Cna1979GrazianiSourceFacts.CcnnHqUnidentifiedArtilleryComponentType, hq.ComponentType);
            Assert.Equal(1, hq.ComponentToe);
            // Never silently borrow the Legion HQs' own, separately cited weapon.
            Assert.NotEqual("65-17-gun", hq.ComponentType);
            Assert.Null(hq.AdditionalComponents);
        }
    }

    [Fact]
    public void RulingClearsTheCharacteristicsBlockerForBothHqsAloneButRevertingRestoresIt()
    {
        var groups = CampaignSourceCcnnBatch.Create(CreateThroughAxisSupplement());
        // Isolate: a synthetic formation containing only the HQ (its own
        // root, no other members) proves the ruling's effect on these two
        // elements specifically, since the real groups' still-unresolved
        // engineers would otherwise mask it under the same combined OR check.
        var hqOnly = groups.Select(group => new CampaignGroupedFormation(group.GroupId, group.SideId, group.RootId,
            group.LocationId, group.StackValue, group.MotorPool,
            group.Members.Where(member => member.ElementId == group.RootId), true)).ToArray();
        Assert.Equal(2, hqOnly.Length);
        Assert.False(CampaignSourceCcnnBatch.HasIncompleteCharacteristics(hqOnly));
        Assert.False(CampaignSourceCcnnBatch.HasIncompleteCharacteristics(
            [hqOnly.Single(value => value.GroupId == CampaignSourceCcnnBatch.FirstId)]));
        Assert.False(CampaignSourceCcnnBatch.HasIncompleteCharacteristics(
            [hqOnly.Single(value => value.GroupId == CampaignSourceCcnnBatch.FourthId)]));

        // Reverted: forcing the resolved HQ member's weapon identity back to
        // null (the pre-ruling state) must trip the blocker again, for each
        // HQ independently (neither hides behind the other).
        var reverted = hqOnly.Select(group => new CampaignGroupedFormation(group.GroupId, group.SideId, group.RootId,
            group.LocationId, group.StackValue, group.MotorPool,
            group.Members.Select(member => member with { ComponentType = null, ComponentToe = null }), true)).ToArray();
        Assert.True(CampaignSourceCcnnBatch.HasIncompleteCharacteristics(reverted));
        Assert.True(CampaignSourceCcnnBatch.HasIncompleteCharacteristics(
            [reverted.Single(value => value.GroupId == CampaignSourceCcnnBatch.FirstId)]));
        Assert.True(CampaignSourceCcnnBatch.HasIncompleteCharacteristics(
            [reverted.Single(value => value.GroupId == CampaignSourceCcnnBatch.FourthId)]));

        // The real production groups (including the two still-unresolved
        // engineers) still trip the blocker - that is genuinely out of this
        // ruling's scope, per RUL-GRA-28's own text, not a regression here.
        Assert.True(CampaignSourceCcnnBatch.HasIncompleteCharacteristics(groups));
    }

    [Fact]
    public void UnknownWeaponNeverContributesAGuessedDefenseRating()
    {
        var groups = CampaignSourceCcnnBatch.Create(CreateThroughAxisSupplement());
        var firstHq = groups.Single(value => value.GroupId == CampaignSourceCcnnBatch.FirstId)
            .Members.Single(value => value.ElementId == "1-ccnn-hq");
        var firstEstimate = CampaignSourceGroupedZoc.EstimateRawDefenseLowerBound(CampaignSourceCcnnBatch.FirstId, [firstHq]);
        Assert.False(firstEstimate.AllKnown);
        Assert.Equal(0, firstEstimate.LowerBound);

        var fourthHq = groups.Single(value => value.GroupId == CampaignSourceCcnnBatch.FourthId)
            .Members.Single(value => value.ElementId == "4-ccnn-hq");
        var fourthEstimate = CampaignSourceGroupedZoc.EstimateRawDefenseLowerBound(CampaignSourceCcnnBatch.FourthId, [fourthHq]);
        Assert.False(fourthEstimate.AllKnown);
        Assert.Equal(0, fourthEstimate.LowerBound);
    }

    // The minimum real chain needed to reach a genuine Axis source supplement
    // (mirrors GrazianiSourceTobrukBardiaGarrisonTests.CreateThroughAxisGarrisons,
    // stopping one step earlier): every step is a plain, directly-callable,
    // choice-free Admit method on CampaignSourceAuthorityState (no dice, no
    // session/action replay).
    private static CampaignSourceAuthorityState CreateThroughAxisSupplement()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-ccnn-hq-artillery-ruling", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var (rolled, _) = CampaignPregameSourceState.Create(request, artifact, init).Apply(new PregameAxisSupplyRollAction());
        return CampaignSourceAuthorityState.Finish(rolled).AdmitAxisSupplement();
    }
}
