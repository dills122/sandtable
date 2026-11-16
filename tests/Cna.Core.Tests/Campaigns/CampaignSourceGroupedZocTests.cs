using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignSourceGroupedZocTests
{
    [Fact]
    public void SixteenthBrigadeThreeRiflBattalionsGiveASoundThirtySixPointLowerBound()
    {
        CampaignGroupedMember[] members =
        [
            new("16-infantry-brigade-hq", null, null, 0, false, "3-7-howitzer", 3, 30, CapabilityPointAmount.Zero, 0),
            new("2-queens-royal-regiment", "16-infantry-brigade-hq", 6, 0, false, "infantry", 6, 10, CapabilityPointAmount.Zero, 0),
            new("2-leicesters", "16-infantry-brigade-hq", 6, 0, false, "infantry", 6, 10, CapabilityPointAmount.Zero, 0),
            new("1-argyll-sutherland", "16-infantry-brigade-hq", 6, 0, false, "infantry", 6, 10, CapabilityPointAmount.Zero, 0),
        ];
        var group = new CampaignGroupedFormation(CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId, "commonwealth",
            "16-infantry-brigade-hq", "e1829", 2, 20, members, dispositionConfirmed: true, usesRetainedMembers: true);

        var estimate = CampaignSourceGroupedZoc.EstimateRawDefenseLowerBound(group.GroupId, group.Members);
        // The HQ's integral 3.7" howitzer company has no established Close
        // Assault rating here, so the formation total is not fully known -
        // but the three rifle battalions alone already prove the floor.
        Assert.Equal(36, estimate.LowerBound);
        Assert.False(estimate.AllKnown);

        var result = CampaignSourceGroupedZoc.EvaluateFormationSource(group);
        Assert.NotNull(result);
        Assert.True(result!.IsQualified);
    }

    [Fact]
    public void FourthIndianDivisionRosterIsFullyKnownAndQualifiesWithoutSubstitutingTheArtillery()
    {
        var members = CampaignSourceFourthIndianDefinition.Members.Select(source => new CampaignGroupedMember(
            source.Id, source.ParentId, source.Kind is "infantry" or "motor-infantry" ? source.Toe : null, 0,
            source.Kind == "motor-infantry", source.Toe is null ? null : source.Kind, source.Toe,
            source.CapabilityPoints, CapabilityPointAmount.Zero, 0)).ToArray();
        var group = new CampaignGroupedFormation(CampaignSourceGroupedZoc.FourthIndianGroupId, "commonwealth",
            "4-indian-division-hq", "d3615", 5, 22, members, dispositionConfirmed: true, usesRetainedMembers: true);

        var estimate = CampaignSourceGroupedZoc.EstimateRawDefenseLowerBound(group.GroupId, group.Members);
        // Six rifle battalions (72) + Central India Horse under infantry code
        // 'l' (12) + the 3.7" howitzer battery's known-zero rating (0): every
        // component is accounted for, not just the 72-point lower bound.
        Assert.True(estimate.AllKnown);
        Assert.Equal(84, estimate.LowerBound);

        var result = CampaignSourceGroupedZoc.EvaluateFormationSource(group);
        Assert.NotNull(result);
        Assert.True(result!.IsQualified);
    }

    [Fact]
    public void SeventhArmoredArmorUnitsUseTheirOwnWeaponsRatingNotABlanketArmorValue()
    {
        CampaignGroupedMember[] members =
        [
            new("7-armored-division-hq", null, null, 0, false, null, null, 30, CapabilityPointAmount.Zero, 0),
            new("4-armored-brigade-hq", "7-armored-division-hq", null, 0, false, null, null, 30, CapabilityPointAmount.Zero, 0),
            new("6-rtr", "4-armored-brigade-hq", null, 0, false, "mark-vi-light", 10, 25, CapabilityPointAmount.Zero, 0),
            new("7-queens-own-hussars", "4-armored-brigade-hq", null, 0, false, "mark-vi-light", 10, 25, CapabilityPointAmount.Zero, 0),
            new("7-armored-brigade-hq", "7-armored-division-hq", null, 0, false, null, null, 20, CapabilityPointAmount.Zero, 0),
            new("8-hussars", "7-armored-brigade-hq", null, 0, false, "a10-cruiser", 10, 25, CapabilityPointAmount.Zero, 0),
            new("7-support-group-hq", "7-armored-division-hq", null, 0, false, null, null, 30, CapabilityPointAmount.Zero, 0),
        ];
        var group = new CampaignGroupedFormation(CampaignSourceGroupedZoc.SeventhArmoredGroupId, "commonwealth",
            "7-armored-division-hq", "d3612", 5, 10, members, dispositionConfirmed: true);

        var estimate = CampaignSourceGroupedZoc.EstimateRawDefenseLowerBound(group.GroupId, group.Members);
        // Mark VI light (10 TOE x DR 2 x 2 units = 40) + A10 Cruiser (10 x 3 = 30).
        Assert.Equal(70, estimate.LowerBound);
        Assert.True(estimate.AllKnown);
        var result = CampaignSourceGroupedZoc.EvaluateFormationSource(group);
        Assert.NotNull(result);
        Assert.True(result!.IsQualified);
    }

    [Fact]
    public void CatanzaroInfantryAndMachineGunUseTheirCitedAxisRatingsNotTheCommonwealthDefault()
    {
        CampaignGroupedMember[] members =
        [
            new("64-catanzaro-hq", null, null, 0, false, null, null, 30, CapabilityPointAmount.Zero, 0),
            new("141-infantry-hq", "64-catanzaro-hq", null, 0, false, null, null, 20, CapabilityPointAmount.Zero, 0),
            new("1-141-infantry", "141-infantry-hq", null, 0, false, "infantry", 7, 10, CapabilityPointAmount.Zero, 0),
            new("2-141-infantry", "141-infantry-hq", null, 0, false, "infantry", 7, 10, CapabilityPointAmount.Zero, 0),
            new("3-141-infantry", "141-infantry-hq", null, 0, false, "infantry", 7, 10, CapabilityPointAmount.Zero, 0),
            new("64-machine-gun", "64-catanzaro-hq", null, 0, false, "infantry", 2, 8, CapabilityPointAmount.Zero, 0),
        ];
        var group = new CampaignGroupedFormation(CampaignSourceCatanzaroGroupedDefinition.GroupId, "axis",
            "64-catanzaro-hq", "c4707", 5, 0, members, dispositionConfirmed: true);

        var estimate = CampaignSourceGroupedZoc.EstimateRawDefenseLowerBound(group.GroupId, group.Members);
        // Three rifle companies (7 x 1 x 3 = 21) + the machine-gun company (2 x 7 = 14).
        // This bounded fixture omits 142-infantry-hq's regiment and the
        // antitank/artillery/engineer members entirely (rather than include
        // them as unknown), so every included component resolves.
        Assert.Equal(35, estimate.LowerBound);
        Assert.True(estimate.AllKnown);
        var result = CampaignSourceGroupedZoc.EvaluateFormationSource(group);
        Assert.NotNull(result);
        Assert.True(result!.IsQualified);
    }

    [Fact]
    public void UnrecognizedComponentsBelowTheFloorAreIndeterminateNotZero()
    {
        CampaignGroupedMember[] members =
        [
            new("unrecognized-hq", null, null, 0, false, "unrecognized-weapon", 3, 30, CapabilityPointAmount.Zero, 0),
            new("unrecognized-infantry", "unrecognized-hq", 6, 0, false, "infantry", 6, 10, CapabilityPointAmount.Zero, 0),
        ];
        var group = new CampaignGroupedFormation("unrecognized-formation", "commonwealth",
            "unrecognized-hq", "e1829", 2, 20, members, dispositionConfirmed: true, usesRetainedMembers: true);

        var estimate = CampaignSourceGroupedZoc.EstimateRawDefenseLowerBound(group.GroupId, group.Members);
        Assert.Equal(0, estimate.LowerBound);
        Assert.False(estimate.AllKnown);
        Assert.Null(CampaignSourceGroupedZoc.EvaluateFormationSource(group));
    }

    [Fact]
    public void UnconfirmedDispositionIsIndeterminate()
    {
        CampaignGroupedMember[] members =
        [
            new("16-infantry-brigade-hq", null, null, 0, false, "3-7-howitzer", 3, 30, CapabilityPointAmount.Zero, 0),
            new("2-queens-royal-regiment", "16-infantry-brigade-hq", 6, 0, false, "infantry", 6, 10, CapabilityPointAmount.Zero, 0),
        ];
        var group = new CampaignGroupedFormation(CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId, "commonwealth",
            "16-infantry-brigade-hq", "e1829", null, 20, members, dispositionConfirmed: false, usesRetainedMembers: true);
        Assert.Null(CampaignSourceGroupedZoc.EvaluateFormationSource(group));
    }

    [Fact]
    public void MismatchedMemberCohesionIsIndeterminate()
    {
        CampaignGroupedMember[] members =
        [
            new("16-infantry-brigade-hq", null, null, 0, false, "3-7-howitzer", 3, 30, CapabilityPointAmount.Zero, 1),
            new("2-queens-royal-regiment", "16-infantry-brigade-hq", 6, 0, false, "infantry", 6, 10, CapabilityPointAmount.Zero, 0),
            new("2-leicesters", "16-infantry-brigade-hq", 6, 0, false, "infantry", 6, 10, CapabilityPointAmount.Zero, 0),
            new("1-argyll-sutherland", "16-infantry-brigade-hq", 6, 0, false, "infantry", 6, 10, CapabilityPointAmount.Zero, 0),
        ];
        var group = new CampaignGroupedFormation(CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId, "commonwealth",
            "16-infantry-brigade-hq", "e1829", 2, 20, members, dispositionConfirmed: true, usesRetainedMembers: true);
        Assert.Null(CampaignSourceGroupedZoc.EvaluateFormationSource(group));
    }

    [Fact]
    public void TryDeriveControlledLocationIdsRejectsMismatchedCohesionRatherThanGuessing()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var prior = CampaignWorldV8Factory.CreateInitial(artifact, Cna1979GrazianiCairoSourceExcerpt.ScenarioId,
            new CampaignWorldV8Initialization(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
        CampaignGroupedMember[] members =
        [
            new("mismatched-group-hq", null, null, 0, false, "3-7-howitzer", 3, 30, CapabilityPointAmount.Zero, 1),
            new("mismatched-group-battalion", "mismatched-group-hq", 6, 0, false, "infantry", 6, 10, CapabilityPointAmount.Zero, 0),
        ];
        var group = new CampaignGroupedFormation("mismatched-group", "commonwealth",
            "mismatched-group-hq", "e1829", 2, 20, members, dispositionConfirmed: true);
        var grouped = CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(), [group]);
        // CanDeriveForSide/EvaluateFormationSource both treat mismatched member
        // cohesion as indeterminate; a caller reaching this lower-level overload
        // directly, bypassing that check, must get an explicit error instead of
        // a silent guess from one unrepresentative member's level.
        Assert.Throws<InvalidOperationException>(() =>
            CampaignSourceGroupedZoc.TryDeriveControlledLocationIds(grouped, cdSeamBound: false, "commonwealth"));
    }

    [Fact]
    public void FourthIndianAtD3615ProjectsZocOntoARealEnterablePassThroughNeighbor()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var prior = CampaignWorldV8Factory.CreateInitial(artifact, Cna1979GrazianiCairoSourceExcerpt.ScenarioId,
            new CampaignWorldV8Initialization(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
        var members = CampaignSourceFourthIndianDefinition.Members.Select(source => new CampaignGroupedMember(
            source.Id, source.ParentId, source.Kind is "infantry" or "motor-infantry" ? source.Toe : null, 0,
            source.Kind == "motor-infantry", source.Toe is null ? null : source.Kind, source.Toe,
            source.CapabilityPoints, CapabilityPointAmount.Zero, 0)).ToArray();
        var group = new CampaignGroupedFormation(CampaignSourceGroupedZoc.FourthIndianGroupId, "commonwealth",
            "4-indian-division-hq", "d3615", 5, 22, members, dispositionConfirmed: true);
        var grouped = CampaignGroupedWorld.Create(prior, CampaignSourceFourthIndianDefinition.Hash(), [group]);

        var controlled = CampaignSourceGroupedZoc.TryDeriveControlledLocationIds(grouped, cdSeamBound: false, "commonwealth");

        // d3516 is a real same-sheet neighbor of d3615 (rough terrain, a
        // plain track hexside - both enterable and ZOC pass-through). The
        // division's own hex is never self-controlled.
        Assert.Contains("d3516", controlled);
        Assert.DoesNotContain("d3615", controlled);
        // d3715 is also a real neighbor, but its only recorded hexside
        // carries an unmapped "railroad" feature, so it is honestly left out
        // rather than guessed open or closed.
        Assert.DoesNotContain("d3715", controlled);
    }

    [Fact]
    public void DetachedSingleBattalionBelowStackingThresholdIsKnownNotQualified()
    {
        var member = new CampaignGroupedMember("2-queens-royal-regiment", null, 6, 0, false, "infantry", 6, 10,
            CapabilityPointAmount.Zero, 0);
        var detached = new CampaignDetachedGroupedMember(CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId,
            "commonwealth", "e1829", 1, member);
        var result = CampaignSourceGroupedZoc.EvaluateDetachedSource(detached);
        Assert.NotNull(result);
        Assert.False(result!.IsQualified);
        Assert.Contains(ZocSourceFailureKind.InsufficientStackingPoints, result.Failures);
    }
}
