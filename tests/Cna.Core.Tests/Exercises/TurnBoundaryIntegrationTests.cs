using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Exercises.Sequencing;

namespace Cna.Core.Tests.Exercises;

public sealed class TurnBoundaryIntegrationTests
{
    [Theory]
    [InlineData(1, 2UL)]
    [InlineData(1, 3UL)]
    [InlineData(12, 12345UL)]
    public void SyntheticTurnBoundaryReplaysEachEventAndUsesSuccessorTurnAndCursor(int initialTurn, ulong seed)
    {
        var samples = SequencingScenarios.DriveTurnBoundary(initialTurn, seed).Samples;
        var boundary = samples.Where(sample => sample.Label.StartsWith("turn.", StringComparison.Ordinal)).ToArray();
        Assert.Equal(10, boundary.Length);
        var lastInitial = samples.Single(sample => sample.Label == "op3.second.post-combat-pass-3").Session;
        var terminalPass = Assert.IsType<CampaignNativePostCombatPassV42>(lastInitial.CurrentHistory[^1]);
        var end = Assert.IsType<CampaignNativeEndOfTurnV55>(boundary[0].Session.CurrentHistory[^1]);
        Assert.Equal(initialTurn, end.GameTurn);
        Assert.Equal(initialTurn + 1, end.NextGameTurn);
        Assert.Equal(terminalPass.WorldHash, end.WorldHash);

        foreach (var sample in boundary)
        {
            var session = sample.Session;
            Assert.IsType<TurnBoundaryPending>(session.Pending);
            Assert.Equal(initialTurn + 1, CampaignGameTurn.CurrentGameTurn(session));
            var info = CampaignGameTurn.BoundaryInfo(session.CurrentHistory[^1]);
            Assert.Equal(sample.Label == "turn.end" ? initialTurn : initialTurn + 1, info.GameTurn);
            Assert.Equal(terminalPass.WorldHash, info.WorldHash);
            Assert.Equal(info.PriorStateVersion + 1, CampaignExercises.QueryCheckpoint(session).StateVersion);
            Assert.True(CampaignExercises.Reconstruct(session).IsVerified, sample.Label);
            var (tampered, _) = SessionTamper.TamperOwnRecord(session);
            Assert.NotNull(tampered);
            Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, CampaignExercises.Reconstruct(tampered).FailureReason);
        }

        var initiative = Assert.IsType<CampaignNativeTurnInitiativeV56>(boundary[1].Session.CurrentHistory[^1]);
        var declaration = Assert.IsType<CampaignNativeInitiativeDeclarationV49>(boundary[4].Session.CurrentHistory[^1]);
        var weather = Assert.IsType<CampaignNativeWeatherDeterminationV50>(boundary[5].Session.CurrentHistory[^1]);
        var priorWeather = lastInitial.CurrentHistory.OfType<CampaignNativeWeatherDeterminationV50>().ToArray();
        Assert.Equal(2, priorWeather.Length);
        Assert.Equal(lastInitial.CurrentSnapshot.RandomState.NextByteCursor, priorWeather[0].PriorRandomCursor);
        Assert.Equal(priorWeather[0].ResultRandomCursor, priorWeather[1].PriorRandomCursor);
        Assert.Equal(priorWeather[1].ResultRandomCursor, initiative.PriorRandomCursor);
        Assert.Equal(ExerciseSession.LatestCursor(lastInitial.CurrentSnapshot.RandomState, lastInitial.CurrentHistory).NextByteCursor,
            initiative.PriorRandomCursor);
        Assert.True(initiative.ResultRandomCursor > initiative.PriorRandomCursor);
        Assert.Equal(initiative.ResultRandomCursor, weather.PriorRandomCursor);
        Assert.True(weather.ResultRandomCursor > weather.PriorRandomCursor);
        Assert.Equal(initiative.Holder, declaration.DeclaringHolder);
        Assert.Equal(initiative.Holder, declaration.SecondSide); // actLast from the driver
        Assert.Equal(initiative.Holder, weather.DeterminingSide);
        Assert.Equal(Cna1979Weather.GetSeason(initialTurn + 1), weather.Season);
        if (initialTurn == 12)
        {
            Assert.Equal(WeatherSeason.Fall, lastInitial.CurrentSnapshot.OperationStageWeather.Single().Season);
            Assert.Equal(WeatherSeason.Winter, weather.Season);
        }
        var wrongAudience = initiative.Holder == LandSide.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        Assert.False(CampaignExercises.SubmitStageOneDeclaration(boundary[3].Session, wrongAudience,
            SequencingScenarios.Hash("wrong-holder"), true).IsAccepted);
        var (repair, world) = CampaignExercises.QueryTurnStartReserve(boundary[^1].Session);
        Assert.Equal(declaration.FirstSide, repair.FirstSide);
        var orders = CampaignExercises.TurnStartOrders(boundary[^1].Session);
        Assert.Equal(4, orders.Count);
        Assert.Equal(repair.ToOrder(), orders.Single(order => order.GameTurn == initialTurn + 1 && order.OperationStage == 1));
        Assert.Equal(terminalPass.WorldHash, CampaignNativeCombatPaidWorldV22.Hash(world));
        Assert.Same(lastInitial.CurrentThirdStageSecondAssault!.Release!.World, world);
        Assert.Equal("land.position.operation-1.first-player.reserve-designation", CampaignExercises.QueryCheckpoint(boundary[^1].Session).PositionId);
        Assert.False(CampaignExercises.CompleteTurnStartFleetRepair(boundary[^1].Session).IsAccepted);
        var frontier = CampaignExercises.QueryCheckpoint(boundary[^1].Session);
        var firstAudience = repair.FirstSide == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var reserveSubmission = new CampaignActionSubmission(1, frontier.CampaignId, frontier.StateVersion,
            frontier.PositionId, firstAudience, new CompleteReserveDesignationAction().ActionId);
        Assert.False(CampaignExercises.Submit(boundary[^1].Session, reserveSubmission).IsAccepted);
        Assert.Equal(CampaignActionSubmissionRejectionReason.RuleNotImplemented,
            CampaignExercises.SubmitWithCombat(boundary[^1].Session, reserveSubmission).RejectionReason);
        Assert.Null(CampaignExercises.QueryWithCombat(boundary[^1].Session, firstAudience).ActionSet);
    }

    [Fact]
    public void IndependentSameSeedDrivesHaveIdenticalAcceptedHistory()
    {
        var first = SequencingScenarios.DriveTurnBoundary(1, 2).Samples[^1].Session;
        var second = SequencingScenarios.DriveTurnBoundary(1, 2).Samples[^1].Session;
        Assert.Equal(first.CurrentHistory.Count, second.CurrentHistory.Count);
        Assert.All(first.CurrentHistory.Zip(second.CurrentHistory), pair => Assert.Equal(
            CampaignCurrentEventSerializer.Serialize(pair.First, first.Context),
            CampaignCurrentEventSerializer.Serialize(pair.Second, second.Context)));
    }

    [Fact]
    public void WrongTurnWeatherStampCannotReplayEvenIfTheSerializedShapeIsValid()
    {
        var session = SequencingScenarios.DriveTurnBoundary(12, 12345).Samples
            .Single(sample => sample.Label == "turn.weather").Session;
        var weather = (CampaignNativeWeatherDeterminationV50)session.CurrentHistory[^1];
        var wrong = weather with { GameTurn = 12 };
        Assert.NotEqual(weather.SerializeCanonical(), wrong.SerializeCanonical());
        var clone = SessionTamper.Clone(session);
        SessionTamper.Set(clone, nameof(ExerciseSession.CurrentHistory),
            session.CurrentHistory.SetItem(session.CurrentHistory.Count - 1, wrong));
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, CampaignExercises.Reconstruct(clone).FailureReason);
    }

    [Fact]
    public void TwoSeedsDriveBothDiceDeterminedHolders()
    {
        var holders = new ulong[] { 2, 3 }.Select(seed => SequencingScenarios.DriveTurnBoundary(1, seed).Samples
            .Single(sample => sample.Label == "turn.initiative").Session.CurrentHistory[^1])
            .Cast<CampaignNativeTurnInitiativeV56>().Select(initiative => initiative.Holder).Distinct().ToArray();
        Assert.Equal(2, holders.Length);
    }

    private static readonly string[] SupportedTurnLevelPositions =
    ["land.position.initiative-determination", "land.position.naval-convoy.schedule",
        "land.position.naval-convoy.tactical-shipping", "land.position.end-of-turn"];

    [Fact]
    public void CatalogListsOnlyTheSupportedTurnLevelPositions()
    {
        Assert.Equal(SupportedTurnLevelPositions,
            Cna1979LandSequenceV4.CreateTurn(1).Where(position => position.OperationStage == 0).Select(position => position.PositionId));
    }

    [Fact]
    public void SourceContentCannotEnterSyntheticRollover()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "turn-boundary-source-refusal", Cna1979Ruleset.Manifest.Hash, 0UL,
            CampaignPregameSourceCreation.SetupId,
            CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, initialization),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var session = CampaignExercises.Begin(request, artifact, initialization).Session!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.RuleNotImplemented, CampaignExercises.CompleteEndOfTurn(session).RejectionReason);
    }

    [Fact]
    public void LegacySyntheticContentCannotRollOverAndHasNoLaterTurnPolicy()
    {
        var old = Cna1979BreakdownSetupCatalog.Definitions.Where(setup => setup.OpeningPreamble.ContractVersion == 1).ToArray();
        Assert.Equal(18, old.Length);
        Assert.All(old, setup =>
        {
            Assert.False(Cna1979BreakdownSetupCatalog.AdmitsLaterTurnPasses(setup.Snapshot));
            var created = CampaignExercises.Begin(new CampaignCreationRequest(1, "legacy-boundary-refusal",
                Cna1979Ruleset.Manifest.Hash, 0, setup.SetupId, setup.Hash, setup.Content.Pack.PackId,
                setup.Content.Pack.Hash, setup.Content.ScenarioId));
            Assert.NotNull(created.Session);
            Assert.Equal(CampaignActionSubmissionRejectionReason.RuleNotImplemented,
                CampaignExercises.CompleteEndOfTurn(created.Session).RejectionReason);
        });
        var terminal = SequencingScenarios.All.Single(s => s.Samples[0].Scenario == "pregame-no-attack").Samples[^1].Session;
        var result = CampaignExercises.CompleteEndOfTurn(terminal);
        Assert.Equal(CampaignActionSubmissionRejectionReason.RuleNotImplemented, result.RejectionReason);
    }
}
