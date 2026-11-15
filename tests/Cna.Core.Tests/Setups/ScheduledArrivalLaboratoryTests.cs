using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Setups;

public sealed class ScheduledArrivalLaboratoryTests
{
    [Fact]
    public void GroupsAndOtherObligationsMustAllCompleteBeforePhaseCanComplete()
    {
        var state = ArrivalLabState.Create(Fixture());
        var first = state.Decide(ArrivalLabActor.Axis, Land("a", 0, "group-a", "port-a"));
        Assert.NotNull(first.Event);
        Assert.Equal(1, first.State.Version);
        Assert.Equal("port-a", first.State.Units["unit-a"]);
        Assert.Equal("port-a", first.State.Units["unit-b"]);
        Assert.Same(first.State, first.State.Decide(ArrivalLabActor.System, Complete("early", 1)).State);
        var second = first.State.Decide(ArrivalLabActor.Commonwealth, Land("b", 1, "group-b", "port-b"));
        Assert.NotNull(second.Event);
        Assert.Same(second.State, second.State.Decide(ArrivalLabActor.System, Complete("early", 2)).State);
        var other = second.State.Decide(ArrivalLabActor.System, Other("c", 2, "convoy"));
        var done = other.State.Decide(ArrivalLabActor.System, Complete("d", 3));
        Assert.NotNull(done.Event);
        Assert.True(done.State.Complete);
        Assert.Equal(4, done.State.Version);
        Assert.Null(done.State.Decide(ArrivalLabActor.System, Complete("again", 4)).Event);
        Assert.True(done.State.Decide(ArrivalLabActor.System, Complete("d", 3)).IsRetry);
    }

    [Fact]
    public void WrongActorSlotDestinationAndStaleVersionsRejectWithoutMutation()
    {
        var state = ArrivalLabState.Create(Fixture());
        var invalid = new (ArrivalLabActor Actor, ArrivalLabCommand Command)[]
        {
            (ArrivalLabActor.Commonwealth, Land("a", 0, "group-a", "port-a")),
            (ArrivalLabActor.System, Land("a", 0, "group-a", "port-a")),
            (ArrivalLabActor.Axis, Land("a", 1, "group-a", "port-a")),
            (ArrivalLabActor.Axis, Land("a", 0, "group-a", "wrong-port")),
            (ArrivalLabActor.Axis, Land("a", 0, "unknown", "port-a")),
            (ArrivalLabActor.Axis, Land("a", 0, "group-a", "port-a") with { GameTurn = 3 }),
            (ArrivalLabActor.Axis, Land("a", 0, "group-a", "port-a") with { OperationStage = 2 }),
            (ArrivalLabActor.Axis, Land("a", 0, "group-a", "port-a") with { ContractVersion = 2 }),
            (ArrivalLabActor.Axis, Land("Bad ID", 0, "group-a", "port-a")),
            (ArrivalLabActor.Axis, Other("a", 0, "convoy")),
            (ArrivalLabActor.System, Other("a", 0, "unknown")),
            (ArrivalLabActor.Axis, Complete("a", 0)),
        };
        foreach (var (actor, command) in invalid)
        {
            var result = state.Decide(actor, command);
            Assert.Same(state, result.State);
            Assert.Null(result.Event);
            Assert.False(result.IsRetry);
        }
        Assert.Equal(0, state.Version);
        Assert.Empty(state.Units);
    }

    [Fact]
    public void RetryReturnsOriginalEventButConflictingReuseAndRepeatedGroupReject()
    {
        var state = ArrivalLabState.Create(Fixture());
        var command = Land("arrival-1", 0, "group-a", "port-a");
        var first = state.Decide(ArrivalLabActor.Axis, command);
        var retry = first.State.Decide(ArrivalLabActor.Axis, command);
        Assert.True(retry.IsRetry);
        Assert.Same(first.State, retry.State);
        Assert.Same(first.Event, retry.Event);
        Assert.Null(first.State.Decide(ArrivalLabActor.Commonwealth, command).Event);
        Assert.Null(first.State.Decide(ArrivalLabActor.Axis, command with { DestinationId = "port-b" }).Event);
        Assert.Null(first.State.Decide(ArrivalLabActor.Axis, command with { ExpectedVersion = 1 }).Event);
        Assert.Null(first.State.Decide(ArrivalLabActor.Axis, Land("new-id", 1, "group-a", "port-a")).Event);
    }

    [Fact]
    public void ReplayRecomputesEffectsAndRejectsInconsistentOrReorderedEvents()
    {
        var fixture = Fixture();
        var initial = ArrivalLabState.Create(fixture);
        var first = initial.Decide(ArrivalLabActor.Axis, Land("a", 0, "group-a", "port-a"));
        var second = first.State.Decide(ArrivalLabActor.System, Other("b", 1, "convoy"));
        var third = second.State.Decide(ArrivalLabActor.Commonwealth, Land("c", 2, "group-b", "port-b"));
        var fourth = third.State.Decide(ArrivalLabActor.System, Complete("d", 3));
        var events = new[] { first.Event!, second.Event!, third.Event!, fourth.Event! };
        var replayed = ArrivalLabState.Replay(fixture, events);
        Assert.True(replayed.Complete);
        Assert.Equal(fourth.State.Version, replayed.Version);
        Assert.Equal(fourth.State.Units.OrderBy(pair => pair.Key), replayed.Units.OrderBy(pair => pair.Key));
        Assert.True(replayed.Decide(ArrivalLabActor.Axis, events[0].Command).IsRetry);
        Assert.Throws<InvalidOperationException>(() => ArrivalLabState.Replay(fixture, [events[1], events[0]]));
        Assert.Throws<InvalidOperationException>(() => ArrivalLabState.Replay(fixture, [events[0], events[0]]));
        Assert.Throws<InvalidOperationException>(() => ArrivalLabState.Replay(fixture, [events[0] with { ContractVersion = 2 }]));
        Assert.Throws<InvalidOperationException>(() => ArrivalLabState.Replay(fixture, [events[0] with { ResultVersion = 8 }]));
        Assert.Throws<InvalidOperationException>(() => ArrivalLabState.Replay(fixture, [events[0] with { Actor = ArrivalLabActor.Commonwealth }]));
        Assert.Throws<InvalidOperationException>(() => ArrivalLabState.Replay(fixture, [events[0] with { UnitIds = ["unit-a"] }]));
        Assert.Throws<InvalidOperationException>(() => ArrivalLabState.Replay(Fixture("another-fixture"), [events[0]]));
    }

    [Fact]
    public void FixtureCanonicalizationBindsInputsAndCopiesMutableCollections()
    {
        var destinations = new Dictionary<string, IReadOnlyList<string>>
        {
            ["group-a"] = new List<string> { "port-b", "port-a" },
        };
        var others = new List<string> { "convoy" };
        var initial = new Dictionary<string, string> { ["existing"] = "port-a" };
        var group = Group("group-a", LandSide.Axis, "unit-a");
        var fixture = new ArrivalLabFixture("fixture", 2, 3, [group], destinations, others, initial);
        var equalFixture = new ArrivalLabFixture("fixture", 2, 3, [group],
            new Dictionary<string, IReadOnlyList<string>> { ["group-a"] = ["port-a", "port-b"] },
            ["convoy"], new Dictionary<string, string> { ["existing"] = "port-a" });
        Assert.Equal(fixture.FixtureHash, equalFixture.FixtureHash);
        ((List<string>)destinations["group-a"]).Clear();
        destinations.Clear();
        others.Clear();
        initial.Clear();
        var state = ArrivalLabState.Create(fixture);
        Assert.NotNull(state.Decide(ArrivalLabActor.Axis, Land("a", 0, "group-a", "port-a")).Event);
        Assert.Equal("port-a", state.Units["existing"]);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)state.Units).Clear());
        Assert.NotEqual(fixture.FixtureHash, Fixture("different").FixtureHash);
    }

    [Fact]
    public void InvalidFixturesRejectBeforeAnInitialStateExists()
    {
        var group = Group("group-a", LandSide.Axis, "unit-a");
        Assert.Throws<ArgumentException>(() => new ArrivalLabFixture("fixture", 2, 3, [group],
            new Dictionary<string, IReadOnlyList<string>>(), [], new Dictionary<string, string>()));
        Assert.Throws<ArgumentException>(() => new ArrivalLabFixture("fixture", 2, 3, [group],
            new Dictionary<string, IReadOnlyList<string>> { ["group-a"] = [] }, [], new Dictionary<string, string>()));
        Assert.Throws<ArgumentException>(() => new ArrivalLabFixture("fixture", 2, 3, [group],
            new Dictionary<string, IReadOnlyList<string>> { ["group-a"] = ["port-a"] }, ["x", "x"], new Dictionary<string, string>()));
        Assert.Throws<ArgumentException>(() => new ArrivalLabFixture("fixture", 2, 3, [group],
            new Dictionary<string, IReadOnlyList<string>> { ["group-a"] = ["port-a"] }, [],
            new Dictionary<string, string> { ["unit-a"] = "port-a" }));
        Assert.Throws<ArgumentException>(() => new ArrivalLabFixture("fixture", 1, 3, [group],
            new Dictionary<string, IReadOnlyList<string>> { ["group-a"] = ["port-a"] }, [], new Dictionary<string, string>()));
    }

    [Fact]
    public void MalformedSourceUnicodeRejectsBeforeFixtureHashing()
    {
        foreach (var invalid in new[] { new string('\uD800', 1), new string('\uDC00', 1) })
        {
            foreach (var source in new[] { new RuleReference("synthetic", invalid), new RuleReference(invalid, "locator") })
            {
                var group = new ScheduledArrivalGroup("group-a", LandSide.Axis, 2, 3, ["unit-a"], [source]);
                Assert.Throws<ArgumentException>(() => new ArrivalLabFixture("fixture", 2, 3, [group],
                    new Dictionary<string, IReadOnlyList<string>> { ["group-a"] = ["port-a"] }, [],
                    new Dictionary<string, string>()));
            }
        }
    }

    [Fact]
    public void ValidUnicodeAndReorderedGroupsHaveStableFixtureHash()
    {
        var first = new ScheduledArrivalGroup("group-a", LandSide.Axis, 2, 3, ["unit-a"],
            [new RuleReference("synthetic", "valid-\U0001F30D")]);
        var second = Group("group-b", LandSide.Commonwealth, "unit-b");
        var destinations = new Dictionary<string, IReadOnlyList<string>>
        {
            ["group-a"] = ["port-a"],
            ["group-b"] = ["port-b"],
        };
        var a = new ArrivalLabFixture("fixture", 2, 3, [first, second], destinations, [], new Dictionary<string, string>());
        var b = new ArrivalLabFixture("fixture", 2, 3, [second, first], destinations, [], new Dictionary<string, string>());
        Assert.Equal(a.FixtureHash, b.FixtureHash);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("acb")]
    [InlineData("bac")]
    [InlineData("bca")]
    [InlineData("cab")]
    [InlineData("cba")]
    public void AllObligationOrdersReachTheSameWorldAndReplay(string order)
    {
        var fixture = Fixture();
        var state = ArrivalLabState.Create(fixture);
        var events = new List<ArrivalLabEvent>();
        foreach (var item in order)
        {
            var (actor, command) = item switch
            {
                'a' => (ArrivalLabActor.Axis, Land("a", state.Version, "group-a", "port-a")),
                'b' => (ArrivalLabActor.Commonwealth, Land("b", state.Version, "group-b", "port-b")),
                'c' => (ArrivalLabActor.System, Other("c", state.Version, "convoy")),
                _ => throw new InvalidOperationException(),
            };
            var result = state.Decide(actor, command);
            Assert.NotNull(result.Event);
            events.Add(result.Event);
            state = result.State;
        }
        var completion = state.Decide(ArrivalLabActor.System, Complete("done", state.Version));
        Assert.NotNull(completion.Event);
        events.Add(completion.Event);
        var replayed = ArrivalLabState.Replay(fixture, events);
        Assert.True(replayed.Complete);
        Assert.Equal("port-a", replayed.Units["unit-a"]);
        Assert.Equal("port-a", replayed.Units["unit-b"]);
        Assert.Equal("port-b", replayed.Units["unit-c"]);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)events.First(e => e.UnitIds.Count > 0).UnitIds).Clear());
        Assert.Throws<InvalidOperationException>(() => ArrivalLabState.Replay(fixture, [events[0], events[2]]));
    }

    [Fact]
    public void FixtureHashChangesWhenRulesInputsChangeWithoutChangingFixtureId()
    {
        var original = Fixture();
        var changedDestination = new ArrivalLabFixture("fixture", 2, 3,
            [Group("group-a", LandSide.Axis, "unit-a", "unit-b"), Group("group-b", LandSide.Commonwealth, "unit-c")],
            new Dictionary<string, IReadOnlyList<string>> { ["group-a"] = ["port-z"], ["group-b"] = ["port-b"] },
            ["convoy"], new Dictionary<string, string>());
        var changedObligation = new ArrivalLabFixture("fixture", 2, 3,
            [Group("group-a", LandSide.Axis, "unit-a", "unit-b"), Group("group-b", LandSide.Commonwealth, "unit-c")],
            new Dictionary<string, IReadOnlyList<string>> { ["group-a"] = ["port-a"], ["group-b"] = ["port-b"] },
            ["other-convoy"], new Dictionary<string, string>());
        Assert.NotEqual(original.FixtureHash, changedDestination.FixtureHash);
        Assert.NotEqual(original.FixtureHash, changedObligation.FixtureHash);
        var recorded = ArrivalLabState.Create(original).Decide(ArrivalLabActor.Axis,
            Land("a", 0, "group-a", "port-a")).Event!;
        Assert.Throws<InvalidOperationException>(() => ArrivalLabState.Replay(changedObligation, [recorded]));
    }

    private static ArrivalLabFixture Fixture(string id = "fixture") => new(id, 2, 3,
        [Group("group-a", LandSide.Axis, "unit-a", "unit-b"), Group("group-b", LandSide.Commonwealth, "unit-c")],
        new Dictionary<string, IReadOnlyList<string>> { ["group-a"] = ["port-a"], ["group-b"] = ["port-b"] },
        ["convoy"], new Dictionary<string, string>());

    private static ScheduledArrivalGroup Group(string id, LandSide side, params string[] units) =>
        new(id, side, 2, 3, units, [new RuleReference("synthetic", "lab")]);

    private static ArrivalLabCommand Land(string id, long version, string group, string destination) =>
        new(1, id, version, 2, 3, ArrivalLabCommandKind.ResolveLand, group, destination);
    private static ArrivalLabCommand Other(string id, long version, string obligation) =>
        new(1, id, version, 2, 3, ArrivalLabCommandKind.ResolveOther, obligation);
    private static ArrivalLabCommand Complete(string id, long version) =>
        new(1, id, version, 2, 3, ArrivalLabCommandKind.Complete);
}
