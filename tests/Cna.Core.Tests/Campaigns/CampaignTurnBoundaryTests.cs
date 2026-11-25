using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignTurnBoundaryTests
{
    private static readonly string Hash = "sha256:" + new string('a', 64);
    private static ContestedInitiative Policy => new(new AxisInitiativeSourceFacts(
        AxisInitiativeLocation.OffMapOrUnavailable, [AxisInitiativeLocation.QualifyingGameMap]));

    private static CampaignNativePostCombatPassV42 Terminal(int turn)
    {
        var positions = Cna1979LandSequenceV4.CreateTurn(turn);
        var convoy = positions.Last(position => position.PhaseId == LandPhaseIds.TruckConvoyMovement);
        var pass = new CampaignNativeTruckConvoyPassV41(41, "test.campaign", 100, Hash, LandSide.Axis,
            convoy.PositionId, Cna1979LandSequenceV4.GetNext(convoy).PositionId, Hash);
        var rail = CampaignNativePostCombatPassV42.Create(pass, LandSide.Axis, turn, pass.StateVersion);
        var towing = CampaignNativePostCombatPassV42.Create(rail, LandSide.Axis, turn, rail.StateVersion);
        var maintenance = CampaignNativePostCombatPassV42.Create(towing, LandSide.Axis, turn, towing.StateVersion);
        return CampaignNativePostCombatPassV42.Create(maintenance, LandSide.Axis, turn, maintenance.StateVersion);
    }

    private static CampaignNativeEndOfTurnV55 Rollover(int turn = 1)
    {
        var predecessor = Terminal(turn);
        return CampaignNativeEndOfTurnV55.Create(predecessor, turn, predecessor.StateVersion, Cna1979Weather.MaxGameTurn);
    }

    private static CampaignNativeTurnInitiativeV56 Initiative(ulong seed = 0)
    {
        var rollover = Rollover();
        return CampaignNativeTurnInitiativeV56.Create(rollover, Policy, SandtableRandom.Create(seed), rollover.StateVersion);
    }

    [Fact]
    public void RolloverCarriesWorldAndDerivesCurrentTurnFromHistory()
    {
        var rollover = Rollover();
        Assert.Equal(1, rollover.GameTurn);
        Assert.Equal(2, rollover.NextGameTurn);
        Assert.Equal(Hash, rollover.WorldHash);
        Assert.Equal(1, CampaignGameTurn.CurrentGameTurn(1, []));
        Assert.Equal(2, CampaignGameTurn.CurrentGameTurn(1, [rollover, Initiative()]));
        Assert.Equal(3, CampaignGameTurn.CurrentGameTurn(1, [rollover, Rollover(2)]));
        Assert.Equal(12, CampaignGameTurn.CurrentGameTurn(12, []));
        Assert.Equal(rollover.SerializeCanonical(), CampaignNativeEndOfTurnV55.Deserialize(rollover.SerializeCanonical()).SerializeCanonical());
        Assert.Throws<InvalidOperationException>(() => CampaignGameTurn.RequireCurrentGameTurn(1, [rollover], 1));
        CampaignGameTurn.RequireCurrentGameTurn(1, [rollover], 2);
    }

    [Theory]
    [InlineData(110)]
    public void UnsupportedEndOfTurnRefuses(int turn) =>
        Assert.Throws<CampaignRuleNotImplementedException>(() => Rollover(turn));

    [Theory]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(109)]
    public void RolloverWithinWeatherRangeIsSupported(int turn) =>
        Assert.Equal(turn + 1, Rollover(turn).NextGameTurn);

    [Fact]
    public void ScenarioEndIsAnInclusiveMaximumTurnNotARolloverPosition()
    {
        var terminal = Terminal(12);
        Assert.Equal(13, CampaignNativeEndOfTurnV55.Create(terminal, 12, terminal.StateVersion, 13).NextGameTurn);
        Assert.Throws<CampaignRuleNotImplementedException>(() =>
            CampaignNativeEndOfTurnV55.Create(terminal, 12, terminal.StateVersion, 12));
    }

    [Fact]
    public void WrongPositionAndContactVersionRefuse()
    {
        var terminal = Terminal(1);
        Assert.Throws<InvalidOperationException>(() => CampaignNativeEndOfTurnV55.Create(
            terminal with { NextPositionId = terminal.PositionId }, 1, terminal.StateVersion, Cna1979Weather.MaxGameTurn));
        Assert.Throws<InvalidOperationException>(() => CampaignNativeEndOfTurnV55.Create(terminal, 1, terminal.PriorStateVersion, Cna1979Weather.MaxGameTurn));
        var rollover = Rollover();
        Assert.Throws<InvalidOperationException>(() => CampaignNativeTurnInitiativeV56.Create(
            rollover with { NextPositionId = rollover.PositionId }, Policy, SandtableRandom.Create(0), rollover.StateVersion));
        var initiative = Initiative();
        Assert.Throws<InvalidOperationException>(() => CampaignNativeNoObligationTurnPhaseV57.Create(
            initiative with { NextPositionId = rollover.PositionId }, true, initiative.StateVersion));
    }

    [Fact]
    public void PredeterminedInitiativeRefusesInsteadOfReusingInitialHolder()
    {
        var rollover = Rollover();
        Assert.Throws<CampaignRuleNotImplementedException>(() => CampaignNativeTurnInitiativeV56.Create(
            rollover, new PredeterminedInitiative(LandSide.Axis), SandtableRandom.Create(0), rollover.StateVersion));
    }

    [Fact]
    public void TieRoundsConsumeAllDrawsAndWeatherStartsAtTheirSuccessor()
    {
        var initial = SandtableRandom.Create(0);
        var rollover = Rollover();
        var initiative = Initiative();
        Assert.True(initiative.Rounds.Count > 1);
        Assert.Equal(initiative.Rounds[0].AxisTotal, initiative.Rounds[0].CommonwealthTotal);
        var expected = InitiativeResolver.Resolve(2, Policy, initial, []);
        Assert.Equal(expected.RandomState.NextByteCursor, initiative.ResultRandomCursor);
        Assert.Equal(expected.Outcome.Holder, initiative.Holder);
        var schedule = CampaignNativeNoObligationTurnPhaseV57.Create(initiative, true, initiative.StateVersion);
        var shipping = CampaignNativeNoObligationTurnPhaseV57.Create(schedule, true, schedule.StateVersion);
        Assert.Equal(2, shipping.GameTurn);
        Assert.Equal(initiative.Holder, CampaignGameTurn.LatestInitiativeHolder([rollover, initiative, schedule, shipping], LandSide.Axis));
        var declaration = new CampaignNativeInitiativeDeclarationV49(49, shipping.CampaignId, shipping.StateVersion,
            TurnBoundaryCanonical.Hash(shipping.SerializeCanonical()), shipping.NextPositionId, "land.position.operation-1.weather-determination", shipping.WorldHash,
            2, 1, initiative.Holder, LandSide.Axis, LandSide.Commonwealth);
        var cursor = CampaignGameTurn.LatestCursor(initial, [rollover, initiative, schedule, shipping, declaration]);
        Assert.Equal(initiative.ResultRandomCursor, cursor.NextByteCursor);
        var weather = CampaignNativeWeatherDeterminationV50.Create(declaration, cursor, 2, declaration.StateVersion);
        Assert.Equal(initiative.ResultRandomCursor, weather.PriorRandomCursor);
        Assert.Throws<InvalidOperationException>(() => CampaignGameTurn.RequireCurrentGameTurn(
            1, [rollover, initiative, schedule, shipping, declaration], (weather with { GameTurn = 1 }).GameTurn));
        Assert.True(weather.ResultRandomCursor > initiative.ResultRandomCursor);
        Assert.Equal(weather.ResultRandomCursor, CampaignGameTurn.LatestCursor(initial, [initiative, schedule, shipping, declaration, weather]).NextByteCursor);
        Assert.Equal(initiative.SerializeCanonical(), CampaignNativeTurnInitiativeV56.Deserialize(initiative.SerializeCanonical()).SerializeCanonical());
        Assert.Equal(schedule.SerializeCanonical(), CampaignNativeNoObligationTurnPhaseV57.Deserialize(schedule.SerializeCanonical()).SerializeCanonical());
        Assert.Equal(shipping.SerializeCanonical(), CampaignNativeNoObligationTurnPhaseV57.Deserialize(shipping.SerializeCanonical()).SerializeCanonical());
    }

    [Fact]
    public void CanonicalTamperingThatPreservesShapeStillDiffersFromRegeneratedEvent()
    {
        var rollover = Rollover();
        var wrongRollover = rollover with { GameTurn = 2, NextGameTurn = 3 };
        var parsed = CampaignNativeEndOfTurnV55.Deserialize(wrongRollover.SerializeCanonical());
        Assert.Throws<InvalidOperationException>(() => CampaignGameTurn.RequireCurrentGameTurn(1, [], parsed.GameTurn));

        var initiative = Initiative();
        var wrongInitiative = initiative with
        {
            PriorRandomCursor = initiative.PriorRandomCursor + 10,
            ResultRandomCursor = initiative.ResultRandomCursor + 10,
        };
        var parsedInitiative = CampaignNativeTurnInitiativeV56.Deserialize(wrongInitiative.SerializeCanonical());
        Assert.NotEqual(initiative.SerializeCanonical(), parsedInitiative.SerializeCanonical());

        var schedule = CampaignNativeNoObligationTurnPhaseV57.Create(initiative, true, initiative.StateVersion);
        var wrongSchedule = schedule with { WorldHash = "sha256:" + new string('b', 64) };
        Assert.NotEqual(schedule.SerializeCanonical(),
            CampaignNativeNoObligationTurnPhaseV57.Deserialize(wrongSchedule.SerializeCanonical()).SerializeCanonical());
    }

    [Fact]
    public void LatestHolderComesFromAcceptedInitiativeNotGenesis()
    {
        var outcomes = Enumerable.Range(0, 32).Select(seed => Initiative((ulong)seed)).ToArray();
        foreach (var side in Enum.GetValues<LandSide>())
        {
            var outcome = outcomes.First(value => value.Holder == side);
            var other = side == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
            Assert.Equal(side, CampaignGameTurn.LatestInitiativeHolder([Rollover(), outcome], other));
        }
    }

    [Fact]
    public void ConvoyPassesRequireExplicitAuthorizationAndCannotAdvanceTwice()
    {
        var initiative = Initiative();
        Assert.Throws<CampaignRuleNotImplementedException>(() => CampaignNativeNoObligationTurnPhaseV57.Create(initiative, false, initiative.StateVersion));
        var schedule = CampaignNativeNoObligationTurnPhaseV57.Create(initiative, true, initiative.StateVersion);
        Assert.Throws<CampaignRuleNotImplementedException>(() => CampaignNativeNoObligationTurnPhaseV57.Create(schedule, false, schedule.StateVersion));
        var shipping = CampaignNativeNoObligationTurnPhaseV57.Create(schedule, true, schedule.StateVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignNativeNoObligationTurnPhaseV57.Create(shipping, true, shipping.StateVersion));
    }

    [Fact]
    public void CanonicalReadersRejectTamperedTurnPositionAndRound()
    {
        var rollover = Rollover();
        var initiative = Initiative();
        var schedule = CampaignNativeNoObligationTurnPhaseV57.Create(initiative, true, initiative.StateVersion);
        static byte[] Replace(byte[] bytes, string oldValue, string newValue) =>
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace(oldValue, newValue, StringComparison.Ordinal));
        Assert.Throws<JsonException>(() => CampaignNativeEndOfTurnV55.Deserialize(Replace(rollover.SerializeCanonical(), "\"nextGameTurn\":2", "\"nextGameTurn\":3")));
        Assert.Throws<JsonException>(() => CampaignNativeTurnInitiativeV56.Deserialize(Replace(initiative.SerializeCanonical(), "\"gameTurn\":2", "\"gameTurn\":1")));
        Assert.Throws<JsonException>(() => CampaignNativeTurnInitiativeV56.Deserialize(Replace(initiative.SerializeCanonical(), "\"axisRoll\":6", "\"axisRoll\":0")));
        Assert.Throws<JsonException>(() => CampaignNativeNoObligationTurnPhaseV57.Deserialize(Replace(schedule.SerializeCanonical(), "\"gameTurn\":2", "\"gameTurn\":1")));
        Assert.Throws<JsonException>(() => CampaignNativeNoObligationTurnPhaseV57.Deserialize(Replace(schedule.SerializeCanonical(), "naval-convoy.schedule", "end-of-turn")));
    }
}
