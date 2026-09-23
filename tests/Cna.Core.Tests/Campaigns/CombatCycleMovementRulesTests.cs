using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatCycleMovementRulesTests
{
    [Fact]
    public void OverlappingCounterpartsChargeMaximumOnceAndRetainOriginalMembership()
    {
        var own = new CampaignCombatUnitKey("fixture.creation", "axis", "own");
        var other = new CampaignCombatUnitKey("fixture.creation", "commonwealth", "other");
        var second = new CampaignCombatUnitKey("fixture.creation", "commonwealth", "second");
        var contact = new CampaignCombatRelationship("pair.z", "receipt.z", "contact", own, other, 1, 1, true, null, null);
        var engaged = new CampaignCombatRelationship("pair.a", "receipt.a", "engaged", second, own, 1, 1, true, null, null);
        var relations = new[] { contact, engaged };
        var result = CampaignCombatCycleMovementRules.Assess(own, 1, 1, 2, relations);
        Assert.Equal(4, result.BreakOffCost);
        Assert.Equal(new CapabilityPointAmount(6, 1), result.TotalCost);
        Assert.Equal(new[] { engaged, contact }, result.Affected);
        Assert.All(relations, r => Assert.True(r.Active));
        relations[0] = engaged;
        Assert.Equal(new[] { engaged, contact }, result.Affected);
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void InactiveUnrelatedAndUnboundArrivalsDoNotAddCost(string side)
    {
        var own = Unit(side, "own"); var enemy = Unit(side == "axis" ? "commonwealth" : "axis", "enemy");
        var ended = Relation("ended", "engaged", own, enemy, false);
        var unrelated = Relation("unrelated", "engaged", Unit(side, "neighbor"), enemy);
        var contact = Relation("contact", "contact", enemy, own);
        var records = new[] { ended, unrelated, contact };
        var assessed = CampaignCombatCycleMovementRules.Assess(own, 1, 1, 2, records);
        Assert.Equal(2, assessed.BreakOffCost); Assert.Equal(new CapabilityPointAmount(4, 1), assessed.TotalCost);
        Assert.Same(contact, Assert.Single(assessed.Affected));
        var arrival = CampaignCombatCycleMovementRules.Assess(Unit(side, "arrival"), 1, 1, 2, records);
        Assert.Empty(arrival.Affected); Assert.Equal(0, arrival.BreakOffCost);
        var lastDeparted = CampaignCombatCycleMovementRules.Assess(enemy, 1, 1, 2, [ended]);
        Assert.Empty(lastDeparted.Affected); Assert.False(ended.Active); Assert.True(unrelated.Active);
        Assert.Throws<NotSupportedException>(() => ((IList<CampaignCombatRelationship>)assessed.Affected)[0] = ended);
    }

    [Theory]
    [InlineData(5, 10, (int)CampaignCombatSpendCeiling.Ordinary, 4, 2, 11, 1)]
    [InlineData(6, 10, (int)CampaignCombatSpendCeiling.Ordinary, 4, 2, 12, 2)]
    [InlineData(9, 10, (int)CampaignCombatSpendCeiling.Ordinary, 4, 2, 15, 5)]
    [InlineData(8, 10, (int)CampaignCombatSpendCeiling.ReleasedReserveI, 0, 2, 10, 0)]
    [InlineData(3, 10, (int)CampaignCombatSpendCeiling.ReleasedReserveII, 0, 2, 5, 0)]
    [InlineData(3, 9, (int)CampaignCombatSpendCeiling.ReleasedReserveII, 0, 1, 4, 0)]
    [InlineData(7, 9, (int)CampaignCombatSpendCeiling.ReleasedReserveI, 0, 2, 9, 0)]
    [InlineData(9, 9, (int)CampaignCombatSpendCeiling.Ordinary, 2, 2, 13, 4)]
    [InlineData(11, 10, (int)CampaignCombatSpendCeiling.Ordinary, 0, 2, 13, 2)]
    public void ChargeUsesCumulativeCeilingsAndOnlyNewExcessDp(int spent, int cpa,
        int ceiling, int breakOff, int terrain, int after, int dp)
    {
        var own = Unit("axis", "own"); var state = State(spent);
        CampaignCombatRelationship[] relations = breakOff == 0 ? [] : [Relation("pair", breakOff == 2 ? "contact" : "engaged", own, Unit("commonwealth", "enemy"))];
        var result = CampaignCombatCycleMovementRules.AssessAndCharge(own, state, cpa, (CampaignCombatSpendCeiling)ceiling, terrain, relations, "move.test", []);
        Assert.Equal(after, result.Spending.State.CapabilityPointsExpended.Numerator);
        Assert.Equal(3 - dp, result.Spending.State.CohesionLevel);
        Assert.Equal(dp, result.Spending.Cause?.Points ?? 0);
        Assert.Equal(spent, state.CapabilityPointsExpended.Numerator); Assert.Equal(3, state.CohesionLevel);
        Assert.Equal(state.InitialLedgerOrigin, result.Spending.State.InitialLedgerOrigin);
        Assert.Equal(state.VehicleBreakdownState, result.Spending.State.VehicleBreakdownState);
        Assert.Equal(state.MovementEnded, result.Spending.State.MovementEnded);
        Assert.All(relations, r => Assert.True(r.Active));
    }

    [Theory]
    [InlineData(10, 10, (int)CampaignCombatSpendCeiling.Ordinary, 4)]
    [InlineData(9, 10, (int)CampaignCombatSpendCeiling.ReleasedReserveI, 0)]
    [InlineData(4, 10, (int)CampaignCombatSpendCeiling.ReleasedReserveII, 0)]
    [InlineData(3, 9, (int)CampaignCombatSpendCeiling.ReleasedReserveII, 0)]
    [InlineData(16, 10, (int)CampaignCombatSpendCeiling.Ordinary, 0)]
    public void RejectedSpendingDoesNotEndRelationshipsOrChangePriorState(int spent, int cpa,
        int ceiling, int breakOff)
    {
        var own = Unit("axis", "own"); var state = State(spent);
        var relation = Relation("pair", "engaged", own, Unit("commonwealth", "enemy"));
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignCombatCycleMovementRules.AssessAndCharge(
            own, state, cpa, (CampaignCombatSpendCeiling)ceiling, 2, breakOff == 0 ? [] : [relation], "move.rejected", []));
        Assert.True(relation.Active); Assert.Null(relation.EndedByReceiptId);
        Assert.Equal(spent, state.CapabilityPointsExpended.Numerator); Assert.Equal(3, state.CohesionLevel);
    }

    [Fact]
    public void PriorMandatoryExcessIsNotChargedTwiceAndCauseHistoryIsOwned()
    {
        var own = Unit("axis", "own"); var initial = State(10);
        var mandatory = CampaignCombatSpending.ChargeMandatoryRetreat(initial, 10, "own", "retreat.one", "retreat.one.dp", []);
        var causes = mandatory.Causes.ToArray();
        var result = CampaignCombatCycleMovementRules.AssessAndCharge(own, mandatory.State, 10,
            CampaignCombatSpendCeiling.Ordinary, 2, [], "move.next", causes);
        Assert.Equal(13, result.Spending.State.CapabilityPointsExpended.Numerator);
        Assert.Equal(0, result.Spending.State.CohesionLevel);
        Assert.Equal(2, result.Spending.Cause!.Points); Assert.Equal(2, result.Spending.Causes.Count);
        Assert.Equal(2, result.Spending.Cause.Ordinal); Assert.Same(causes[0], result.Spending.Causes[0]);
        causes[0] = result.Spending.Cause;
        Assert.Equal("retreat.one.dp", result.Spending.Causes[0].CauseId);
        Assert.Throws<OverflowException>(() => CampaignCombatCycleMovementRules.AssessAndCharge(
            own, State(long.MaxValue), 10, CampaignCombatSpendCeiling.Ordinary, 2, [], "move.overflow", []));
    }

    [Fact]
    public void AssessmentRejectsInvalidScopeCreationDuplicatesAndCapacity()
    {
        var own = Unit("axis", "own"); var enemy = Unit("commonwealth", "enemy");
        var valid = Relation("pair", "contact", own, enemy);
        var foreign = new CampaignCombatRelationship("foreign", "receipt.foreign", "contact",
            new("other.creation", "axis", "own"), new("other.creation", "commonwealth", "enemy"), 1, 1, true, null, null);
        var wrongScope = new CampaignCombatRelationship("wrong.scope", "receipt.scope", "contact", own, enemy, 1, 2, true, null, null);
        foreach (var records in new CampaignCombatRelationship[][] { [valid, valid], [foreign], [wrongScope], [null!], Enumerable.Repeat(valid, 513).ToArray() })
            Assert.ThrowsAny<ArgumentException>(() => CampaignCombatCycleMovementRules.Assess(own, 1, 1, 2, records));
        foreach (var terrain in new[] { 0, -1, 3, int.MaxValue })
            Assert.Throws<ArgumentOutOfRangeException>(() => CampaignCombatCycleMovementRules.Assess(own, 1, 1, terrain, []));
        Assert.Throws<ArgumentNullException>(() => CampaignCombatCycleMovementRules.Assess(null!, 1, 1, 2, []));
        Assert.Throws<ArgumentNullException>(() => CampaignCombatCycleMovementRules.Assess(own, 1, 1, 2, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignCombatCycleMovementRules.Assess(own, 0, 1, 2, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignCombatCycleMovementRules.Assess(own, 1, 4, 2, []));
        var max = Enumerable.Range(0, 512).Select(i => Relation($"pair.{i:D3}", "contact", own, enemy)).Reverse().ToArray();
        var assessed = CampaignCombatCycleMovementRules.Assess(own, 1, 1, 2, max);
        Assert.Equal(512, assessed.Affected.Count); Assert.Equal(2, assessed.BreakOffCost);
        Assert.Equal("pair.000", assessed.Affected[0].RelationId);
    }

    [Fact]
    public void SameElementIdOnOpposingSideDoesNotInheritMembership()
    {
        var own = Unit("axis", "shared-id");
        var relation = Relation("pair", "engaged", own, Unit("commonwealth", "enemy"));
        var result = CampaignCombatCycleMovementRules.Assess(Unit("commonwealth", "shared-id"), 1, 1, 2, [relation]);
        Assert.Empty(result.Affected);
        Assert.Equal(new CapabilityPointAmount(2, 1), result.TotalCost);
    }

    private static CampaignCombatUnitKey Unit(string side, string id) => new("fixture.creation", side, id);
    private static CampaignCombatRelationship Relation(string id, string kind, CampaignCombatUnitKey first,
        CampaignCombatUnitKey second, bool active = true) => new(id, $"receipt.{id}", kind, first, second, 1, 1,
            active, active ? null : "move.old", active ? null : "ordinary-break-off");
    private static CampaignElementOperationalStateV6 State(long spent) => new(1, 1, new(spent, 1), 3, null, null,
        new ContentOrigin(ContentOriginKind.Synthetic, [new RuleReference("sandtable-rules-lab", "combat.close-assault-positive.v1:initial-ledger")]));
}
