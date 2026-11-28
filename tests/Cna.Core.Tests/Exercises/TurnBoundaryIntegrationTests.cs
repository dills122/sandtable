using System.Security.Cryptography;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
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
        Assert.True(CampaignGameTurn.IsInitialTurn(lastInitial));
        Assert.Equal(initialTurn, end.GameTurn);
        Assert.Equal(initialTurn + 1, end.NextGameTurn);
        Assert.Equal(terminalPass.WorldHash, end.WorldHash);

        foreach (var sample in boundary)
        {
            var session = sample.Session;
            Assert.IsType<TurnBoundaryPending>(session.Pending);
            Assert.False(CampaignGameTurn.IsInitialTurn(session));
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
        var (reserve, rolled) = CampaignNativeSecondReserveV43.Create(repair, world, boundary[^1].Session.Context,
            orders, repair.FirstSide, null, initialTurn + 1, repair.StateVersion);
        Assert.Equal(CampaignNativeSecondReserveV43.LedgerRolloverContractVersion, reserve.ContractVersion);
        Assert.All(rolled.Elements, element =>
        {
            Assert.Equal(initialTurn + 1, element.OperationalState.LedgerGameTurn);
            Assert.Equal(1, element.OperationalState.LedgerOperationStage);
            Assert.Equal(CapabilityPointAmount.Zero, element.OperationalState.CapabilityPointsExpended);
        });
        Assert.Equal(reserve, CampaignNativeSecondReserveV43.Deserialize(reserve.SerializeCanonical()));
        Assert.Equal(CampaignNativeCombatPaidWorldV22.Hash(world), reserve.PriorWorldHash);
        Assert.Equal(CampaignNativeCombatPaidWorldV22.Hash(rolled), reserve.ResultWorldHash);
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

    // Canonical prefix digests independently captured from d82af0f5 (before D-B).
    // Stop just before the first changed Reserve event; scenarios that never enter
    // another stage retain their complete historical stream.
    [Theory]
    [InlineData("pregame-no-attack", 41, "8235b67ed6bb67603305810f17fa8282ec9a3d29fc6dff40b0e47b61d45ee980")]
    [InlineData("real-attack", 41, "90ec1f054c17b9e81171f7cbb633333ae0db0736dc753a85107a7a4816a8d93c")]
    [InlineData("real-attack-repeat", 29, "03a89bb7beaf832bfc868b95159fdb673b4e5c40ecbc0683c5cc004ae28786b4")]
    [InlineData("turn-boundary-12345-rules-lab.breakdown.turn-boundary.gt1.v1", 41, "0d3a3481b392030681e851a839d4c432e11e23dff92c48b9caca3c50690e08bc")]
    [InlineData("turn-boundary-2-rules-lab.breakdown.turn-boundary.gt1.v1", 41, "c201e4dc33f4ff036c877a94808cfb7416c8bd22efe6738d13e95fe3cabfa1fa")]
    [InlineData("turn-boundary-3-rules-lab.breakdown.turn-boundary.gt1.v1", 41, "21fd43b896274336facf00f53c8a813f9ae3d472b92ae7734eab9647e264c97b")]
    [InlineData("turn-boundary-12345-rules-lab.breakdown.turn-boundary.gt12.v1", 41, "de324f072dfd7e3deaa635eca533e5e6681122f89697aefd12622b7a3890dfaa")]
    public void PreRolloverCanonicalBytesMatchThePreviousRevision(string name, int count, string digest)
    {
        var fixture = name switch
        {
            "turn-boundary-2-rules-lab.breakdown.turn-boundary.gt1.v1" => SequencingScenarios.DriveTurnBoundary(1, 2),
            "turn-boundary-3-rules-lab.breakdown.turn-boundary.gt1.v1" => SequencingScenarios.DriveTurnBoundary(1, 3),
            "turn-boundary-12345-rules-lab.breakdown.turn-boundary.gt12.v1" => SequencingScenarios.DriveTurnBoundary(12, 12345),
            _ => SequencingScenarios.All.Single(scenario => scenario.Samples[0].Scenario == name),
        };
        var samples = fixture.Samples;
        var session = (samples.FirstOrDefault(sample => sample.Label == "op2.fleet-repair") ?? samples[^1]).Session;
        var records = session.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, session.Context)).ToArray();
        Assert.Equal(count, records.Length);
        Assert.Equal(digest, Convert.ToHexStringLower(SHA256.HashData(records.SelectMany(value => value).ToArray())));
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public void LaterStageReactionMoveOpensWindowAndAdvancesAuthority(int stage, bool second)
    {
        var session = SequencingScenarios.DriveReactionFixture().Samples
            .Single(sample => sample.Label == $"op{stage}.{(second ? "second" : "first")}.reserve").Session;
        var reserve = Assert.IsType<CampaignNativeSecondReserveV43>(session.CurrentHistory[^1]);
        var audience = reserve.Side == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var element = reserve.Side == LandSide.Axis ? "axis-infantry" : "commonwealth-infantry";
        var cut = (stage, second) switch
        {
            (2, false) => CampaignExercises.QueryStageEntryMovementCut(session, reserve.Side, element),
            (2, true) => CampaignExercises.QueryStageEntrySecondMovementCut(session, reserve.Side, element),
            (3, false) => CampaignExercises.QueryThirdStageMovementCut(session, reserve.Side, element),
            _ => CampaignExercises.QueryThirdStageSecondMovementCut(session, reserve.Side, element),
        };
        var offer = Assert.Single(cut.MovementOffers, value => value.DestinationLocationId == "north");
        var action = CampaignNativeSecondMoveV44.ActionIdFor(offer);
        var (source, world) = (stage, second) switch
        {
            (2, false) => CampaignExercises.QueryStageEntryMovementSource(session),
            (2, true) => CampaignExercises.QueryStageEntrySecondMovementSource(session),
            (3, false) => CampaignExercises.QueryThirdStageMovementSource(session),
            _ => CampaignExercises.QueryThirdStageSecondMovementSource(session),
        };
        var gameTurn = CampaignGameTurn.CurrentGameTurn(session);
        var orders = stage == 2 ? CampaignExercises.StageEntryOrders(session) : CampaignExercises.ThirdStageOrders(session);
        var (candidate, projected) = CampaignNativeSecondMoveV44.Create(source, world, session.Context, orders,
            session.CurrentContacts, reserve.Side, gameTurn, element, action, session.CurrentSnapshot.RulesetHash);
        Assert.NotNull(candidate.ReactionWindow);
        var position = candidate.ReactionWindow.ReactingPosition.SuspendedMovementPosition;
        Assert.Equal(gameTurn, position.GameTurn);
        Assert.Equal(stage, position.OperationStage);
        Assert.Equal(second ? LandActorRole.SecondActingSide : LandActorRole.FirstActingSide, position.ActorRole);
        Assert.All(world.Elements, value =>
        {
            Assert.Equal(gameTurn, value.OperationalState.LedgerGameTurn);
            Assert.Equal(stage, value.OperationalState.LedgerOperationStage);
        });
        var contacts = session.CurrentContacts.AppendNativeSecondMove(world, projected, candidate, session.Context);
        var detached = (stage, second) switch
        {
            (2, false) => session.WithStageEntryMove(candidate, projected, contacts),
            (2, true) => session.WithStageEntrySecondMove(candidate, projected, contacts),
            (3, false) => session.WithThirdStageMove(candidate, projected, contacts),
            _ => session.WithThirdStageSecondMove(candidate, projected, contacts),
        };
        Assert.True(CampaignExercises.Reconstruct(detached).IsVerified);
        var accepted = (stage, second) switch
        {
            (2, false) => CampaignExercises.SubmitStageEntryMovement(session, audience, action, element),
            (2, true) => CampaignExercises.SubmitStageEntrySecondMovement(session, audience, action, element),
            (3, false) => CampaignExercises.SubmitThirdStageMovement(session, audience, action, element),
            _ => CampaignExercises.SubmitThirdStageSecondMovement(session, audience, action, element),
        };
        Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
        var opened = accepted.SuccessorSession!;
        Assert.IsType<NativeReactionPending>(opened.Pending);
        Assert.Equal(candidate.SerializeCanonical(), Assert.IsType<CampaignNativeSecondMoveV44>(opened.CurrentHistory[^1]).SerializeCanonical());
        Assert.Equal(CampaignExercises.QueryCheckpoint(session).StateVersion + 1,
            CampaignExercises.QueryCheckpoint(opened).StateVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignHostAdapter.QueryReactionOwnerDraft(opened, audience));
        var reactor = audience == CampaignActionAudience.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        Assert.NotEmpty(CampaignHostAdapter.QueryReactionOwnerDraft(opened, reactor).PrivateActionIds);
        var replay = CampaignExercises.Reconstruct(opened);
        Assert.True(replay.IsVerified);
        Assert.Equal(CampaignExercises.Reconstruct(detached).EventStreamHash, replay.EventStreamHash);
    }

    [Fact]
    public void EachLaterStageStartsFreshAndSpendsOnlyFromItsOwnLedger()
    {
        var samples = SequencingScenarios.All.Single(s => s.Samples[0].Scenario == "pregame-no-attack").Samples;
        var stageTwoEntry = samples.Single(s => s.Label == "op2.fleet-repair").Session;
        var (_, priorWorld) = CampaignExercises.QueryStageEntryReserveSource(stageTwoEntry);
        Assert.Contains(priorWorld.Elements, element => element.OperationalState.CapabilityPointsExpended > CapabilityPointAmount.Zero);

        foreach (var stage in new[] { 2, 3 })
        {
            var fresh = samples.Single(s => s.Label == $"op{stage}.first.reserve").Session;
            var world = stage == 2 ? fresh.CurrentStageEntryReserveWorld! : fresh.CurrentThirdStageReserveWorld!;
            Assert.All(world.Elements, element =>
            {
                Assert.Equal(1, element.OperationalState.LedgerGameTurn);
                Assert.Equal(stage, element.OperationalState.LedgerOperationStage);
                Assert.Equal(CapabilityPointAmount.Zero, element.OperationalState.CapabilityPointsExpended);
            });
            var reserve = Assert.IsType<CampaignNativeSecondReserveV43>(fresh.CurrentHistory[^1]);
            Assert.Equal(CampaignNativeSecondReserveV43.LedgerRolloverContractVersion, reserve.ContractVersion);
            var move = samples.Single(s => s.Label == $"op{stage}.first.move").Session;
            var movement = Assert.IsType<CampaignNativeSecondMoveV44>(move.CurrentHistory[^1]);
            var cut = stage == 2 ? CampaignExercises.QueryStageEntryMovementCut(fresh, reserve.Side, movement.ElementId)
                : CampaignExercises.QueryThirdStageMovementCut(fresh, reserve.Side, movement.ElementId);
            Assert.NotEmpty(cut.MovementOffers);
            var spent = stage == 2 ? move.CurrentStageEntryMoveWorld! : move.CurrentThirdStageMoveWorld!;
            var state = spent.Elements.Single(element => element.ElementId == movement.ElementId).OperationalState;
            Assert.Equal(stage, state.LedgerOperationStage);
            Assert.True(state.CapabilityPointsExpended > CapabilityPointAmount.Zero);
            var second = samples.Single(s => s.Label == $"op{stage}.second.reserve").Session;
            var secondWorld = stage == 2 ? second.CurrentStageEntrySecondReserveWorld! : second.CurrentThirdStageSecondReserveWorld!;
            Assert.Equal(CampaignNativeSecondReserveV43.CurrentContractVersion,
                Assert.IsType<CampaignNativeSecondReserveV43>(second.CurrentHistory[^1]).ContractVersion);
            Assert.Equal(state.CapabilityPointsExpended,
                secondWorld.Elements.Single(element => element.ElementId == movement.ElementId).OperationalState.CapabilityPointsExpended);
            Assert.True(CampaignExercises.Reconstruct(fresh).IsVerified);
            Assert.True(CampaignExercises.Reconstruct(move).IsVerified);
        }
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
