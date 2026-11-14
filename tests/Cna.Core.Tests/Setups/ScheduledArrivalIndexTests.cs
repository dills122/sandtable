using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Setups;

public sealed class ScheduledArrivalIndexTests
{
    private static readonly RuleReference Source = new("synthetic-arrivals", "example");

    [Fact]
    public void LookupUsesExactSlotAndSideWithStableOrderingAndCopies()
    {
        var units = new List<string> { "unit-b", "unit-a" };
        var sources = new List<RuleReference> { Source };
        var first = Group("group-b", LandSide.Commonwealth, 2, 3, units, sources);
        var groups = new List<ScheduledArrivalGroup>
        {
            first,
            Group("group-a", LandSide.Commonwealth, 2, 3, ["unit-c"]),
            Group("opponent", LandSide.Axis, 2, 3, ["unit-d"]),
            Group("later", LandSide.Commonwealth, 3, 3, ["unit-e"]),
        };
        var index = new ScheduledArrivalIndex(groups);
        var reversedIndex = new ScheduledArrivalIndex(groups.AsEnumerable().Reverse());
        units.Clear();
        sources.Clear();
        groups.Clear();

        var result = index.Find(2, 3, LandSide.Commonwealth);
        Assert.Equal(["group-a", "group-b"], result.Select(group => group.GroupId));
        Assert.Equal(result.Select(group => group.GroupId),
            reversedIndex.Find(2, 3, LandSide.Commonwealth).Select(group => group.GroupId));
        Assert.Equal(["unit-a", "unit-b"], first.UnitIds);
        Assert.Equal([Source], first.Sources);
        Assert.Single(index.Find(2, 3, LandSide.Axis));
        Assert.Empty(index.Find(2, 2, LandSide.Commonwealth));
        Assert.Throws<NotSupportedException>(() => ((IList<ScheduledArrivalGroup>)result).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)first.UnitIds).Clear());
    }

    [Fact]
    public void LaterOccurrenceOfAUnitIsNotConfusedWithDuplicateInSameSlot()
    {
        var index = new ScheduledArrivalIndex(
        [
            Group("first", LandSide.Axis, 1, 1, ["unit-a"]),
            Group("later", LandSide.Axis, 2, 1, ["unit-a"]),
        ]);

        Assert.Equal("first", Assert.Single(index.Find(1, 1, LandSide.Axis)).GroupId);
        Assert.Equal("later", Assert.Single(index.Find(2, 1, LandSide.Axis)).GroupId);
        Assert.Throws<ArgumentException>(() => new ScheduledArrivalIndex(
        [
            Group("first", LandSide.Axis, 1, 1, ["unit-a"]),
            Group("duplicate", LandSide.Commonwealth, 1, 1, ["unit-a"]),
        ]));
    }

    [Fact]
    public void DuplicateGroupAndUnitIdsAndMissingSourcesReject()
    {
        Assert.Throws<ArgumentException>(() => new ScheduledArrivalIndex(
        [
            Group("same", LandSide.Axis, 1, 1, ["unit-a"]),
            Group("same", LandSide.Axis, 2, 1, ["unit-b"]),
        ]));
        Assert.Throws<ArgumentException>(() => Group("group", LandSide.Axis, 1, 1, ["unit-a", "unit-a"]));
        Assert.Throws<ArgumentException>(() => Group("group", LandSide.Axis, 1, 1, []));
        Assert.Throws<ArgumentException>(() => Group("group", LandSide.Axis, 1, 1, ["unit-a"], []));
        Assert.Throws<ArgumentException>(() => Group("Bad ID", LandSide.Axis, 1, 1, ["unit-a"]));
        Assert.Throws<ArgumentException>(() => Group("group", LandSide.Axis, 1, 1, ["Bad ID"]));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(112, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 4)]
    public void InvalidSlotsRejectInBothConstructionAndQuery(int turn, int stage)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Group("group", LandSide.Axis, turn, stage, ["unit-a"]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScheduledArrivalIndex([]).Find(turn, stage, LandSide.Axis));
    }

    [Fact]
    public void UnknownSideAndNullGroupReject()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Group("group", (LandSide)42, 1, 1, ["unit-a"]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScheduledArrivalIndex([]).Find(1, 1, (LandSide)42));
        Assert.Throws<ArgumentException>(() => new ScheduledArrivalIndex([null!]));
    }

    private static ScheduledArrivalGroup Group(
        string id,
        LandSide owner,
        int turn,
        int stage,
        IEnumerable<string> units,
        IEnumerable<RuleReference>? sources = null) =>
        new(id, owner, turn, stage, units, sources ?? [Source]);
}
