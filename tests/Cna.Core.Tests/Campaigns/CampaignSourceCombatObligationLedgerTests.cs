using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignSourceCombatObligationLedgerTests
{
    [Fact]
    public void CreateOpensOneObligationPerDistinctLocationAndEachClearingPathClosesItsOwn()
    {
        var ledger = CampaignSourceCombatObligationLedger.Create(LandSide.Axis, 1, 1, ["d3516", "d3516", "e1829", "d3615"]);
        // Duplicates collapse; three distinct obligations remain, all open.
        Assert.Equal(3, ledger.Obligations.Count);
        Assert.True(ledger.HasOpenObligations);
        Assert.All(ledger.Obligations, value => Assert.Equal(CampaignSourceCombatObligationStatus.Open, value.Status));

        var attacked = ledger.ResolveAttack("d3516");
        Assert.True(attacked.HasOpenObligations);
        Assert.Equal(CampaignSourceCombatObligationStatus.Attacked,
            attacked.Obligations.Single(value => value.LocationId == "d3516").Status);

        var exempted = attacked.ApplyExemption("e1829", CampaignSourceCombatObligationExemptionReason.Pinned);
        Assert.True(exempted.HasOpenObligations);
        var exemptedObligation = exempted.Obligations.Single(value => value.LocationId == "e1829");
        Assert.Equal(CampaignSourceCombatObligationStatus.Exempted, exemptedObligation.Status);
        Assert.Equal(CampaignSourceCombatObligationExemptionReason.Pinned, exemptedObligation.ExemptionReason);

        var retreated = exempted.ResolveRetreat("d3615");
        Assert.False(retreated.HasOpenObligations);
        Assert.Equal(CampaignSourceCombatObligationStatus.Retreated,
            retreated.Obligations.Single(value => value.LocationId == "d3615").Status);

        // Clearing is immutable: the original ledger instances are untouched.
        Assert.True(ledger.HasOpenObligations);
        Assert.True(attacked.HasOpenObligations);
    }

    [Fact]
    public void CreateRejectsBlankLocationIds()
    {
        Assert.Throws<ArgumentException>(() => CampaignSourceCombatObligationLedger.Create(LandSide.Axis, 1, 1, ["d3516", " "]));
    }

    [Fact]
    public void ClearingAnUnknownOrAlreadyClearedObligationThrows()
    {
        var ledger = CampaignSourceCombatObligationLedger.Create(LandSide.Commonwealth, 1, 1, ["d3516"]);
        Assert.Throws<InvalidOperationException>(() => ledger.ResolveAttack("d3615"));
        var attacked = ledger.ResolveAttack("d3516");
        Assert.Throws<InvalidOperationException>(() => attacked.ResolveAttack("d3516"));
        Assert.Throws<InvalidOperationException>(() => attacked.ApplyExemption("d3516", CampaignSourceCombatObligationExemptionReason.Pinned));
        Assert.Throws<InvalidOperationException>(() => attacked.ResolveRetreat("d3516"));
    }

    [Fact]
    public void HashChangesWithStatusAndIsStableForEquivalentLedgers()
    {
        var ledger = CampaignSourceCombatObligationLedger.Create(LandSide.Axis, 1, 1, ["d3516"]);
        var sameAgain = CampaignSourceCombatObligationLedger.Create(LandSide.Axis, 1, 1, ["d3516"]);
        Assert.Equal(ledger.Hash(), sameAgain.Hash());
        Assert.NotEqual(ledger.Hash(), ledger.ResolveAttack("d3516").Hash());
    }

    // Reuses the exact real-map scenario CampaignSourceGroupedZocTests already
    // proves (Fourth Indian at d3615 projects ZOC onto its real neighbor
    // d3516), adding a small Axis formation sitting in that ZOC'd neighbor
    // and a second, untouched Axis formation elsewhere. This is the
    // "which of my hexes are enemy-ZOC'd and therefore must be attacked"
    // derivation the design sketch calls for (readiness-and-combat-plan.md
    // section 2): only the occupied, enemy-controlled hex is obligated.
    [Fact]
    public void DeriveObligatedLocationIdsIntersectsEnemyZocWithThePhasingSidesOwnOccupiedHexes()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var prior = CampaignWorldV8Factory.CreateInitial(artifact, Cna1979GrazianiCairoSourceExcerpt.ScenarioId,
            new CampaignWorldV8Initialization(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
        var commonwealthMembers = CampaignSourceFourthIndianDefinition.Members.Select(source => new CampaignGroupedMember(
            source.Id, source.ParentId, source.Kind is "infantry" or "motor-infantry" ? source.Toe : null, 0,
            source.Kind == "motor-infantry", source.Toe is null ? null : source.Kind, source.Toe,
            source.CapabilityPoints, CapabilityPointAmount.Zero, 0)).ToArray();
        var commonwealthGroup = new CampaignGroupedFormation(CampaignSourceGroupedZoc.FourthIndianGroupId, "commonwealth",
            "4-indian-division-hq", "d3615", 5, 22, commonwealthMembers, dispositionConfirmed: true);
        CampaignGroupedMember[] axisObligatedMembers =
            [new("axis-outpost-battalion", null, 6, 0, false, "infantry", 6, 10, CapabilityPointAmount.Zero, 0)];
        var axisObligatedGroup = new CampaignGroupedFormation("axis-outpost-group", "axis",
            "axis-outpost-battalion", "d3516", 1, 10, axisObligatedMembers, dispositionConfirmed: true);
        CampaignGroupedMember[] axisClearMembers =
            [new("axis-reserve-battalion", null, 6, 0, false, "infantry", 6, 10, CapabilityPointAmount.Zero, 0)];
        var axisClearGroup = new CampaignGroupedFormation("axis-reserve-group", "axis",
            "axis-reserve-battalion", "e1829", 1, 10, axisClearMembers, dispositionConfirmed: true);
        var grouped = CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(),
            [commonwealthGroup, axisObligatedGroup, axisClearGroup]);

        var controlled = CampaignSourceGroupedZoc.TryDeriveControlledLocationIds(grouped, cdSeamBound: false, "commonwealth");
        Assert.Contains("d3516", controlled);
        Assert.DoesNotContain("e1829", controlled);

        var obligated = CampaignSourceCombatObligations.DeriveObligatedLocationIds(controlled, grouped, "axis");

        Assert.Equal(["d3516"], obligated);
    }

    [Fact]
    public void SideIdMapsTheTwoLandSidesToTheirLowercaseStringIdentifiers()
    {
        Assert.Equal("axis", CampaignSourceCombatObligations.SideId(LandSide.Axis));
        Assert.Equal("commonwealth", CampaignSourceCombatObligations.SideId(LandSide.Commonwealth));
    }

    // CampaignSourceTurnAuthority.EnterCombatSegment/ResolveCombatObligationAttack/
    // ExemptCombatObligation/RetreatCombatObligation/CompleteCombatSegment are not
    // separately unit-tested here: constructing a valid, "gameplay-ready"
    // CampaignSourcePlayableWorld (the World the authority carries) requires
    // the full historical Graziani pregame-to-source-authority setup flow
    // used by GrazianiSourceAuthorityTests (hundreds of lines - Axis/
    // Commonwealth roster, disposition, and finalization actions), because
    // CampaignSourcePlayableWorld.Create cross-validates the grouped world
    // against a matching battlefield index. That cost is disproportionate to
    // this ledger addition. The wiring itself is a thin, mechanical
    // delegation to the ledger/derivation logic already covered above,
    // following this class's own existing precondition-guard style
    // throughout (see the obligation-ledger work report for file:line
    // pointers and this flagged gap).
}
