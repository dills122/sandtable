using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignSuccessorActivationTests
{
    [Fact]
    public void CurrentIdentitySetAndPublicCreationAreSuccessorOnly()
    {
        Assert.Equal(8, Cna1979Ruleset.ContractVersion);
        Assert.Equal(3, Cna1979LandSequence.ContractVersion);
        Assert.Equal(3, Cna1979LandSequence.CatalogSchemaVersion);
        Assert.Contains(
            Cna1979Ruleset.Manifest.Artifacts,
            artifact => artifact.ArtifactId == Cna1979Zoc.AuthorityId);

        var artifact = Cna1979SyntheticContentCatalog.ArtifactV5;
        var definition = Cna1979SetupCatalog.Definitions[0];
        var setup = CampaignSetupSnapshotV5.FromPredecessor(
            CampaignSetupSnapshot.FromDefinition(definition),
            new CampaignContentV5Selection(
                artifact.Identity,
                definition.Content.ScenarioId));
        var request = new CampaignCreationRequest(
            CampaignCreationRequest.CurrentContractVersion,
            "campaign-successor-activation",
            Cna1979Ruleset.Manifest.Hash,
            12345,
            setup.SetupId,
            setup.SetupHash,
            artifact.Identity.PackId,
            artifact.Identity.Hash,
            setup.Content.ScenarioId);

        var start = CampaignExercises.Begin(request);

        Assert.True(start.IsStarted);
        var created = Assert.IsType<CampaignCreatedV9>(
            CampaignSuccessorEventSerializer.Deserialize(start.CreationEventBytes!));
        var snapshot = CampaignSnapshotV10Serializer.Deserialize(start.InitialSnapshotBytes!);
        Assert.Equal(9, created.ContractVersion);
        Assert.Equal(10, snapshot.ContractVersion);
        Assert.Equal(5, snapshot.Setup.Content.Pack.SchemaVersion);
        Assert.Equal(5, snapshot.World.ContractVersion);

        var authority = CampaignAuthority.Create(request);
        Assert.True(authority.IsCreated);
        var observation = CampaignObservations.Query(
            authority.Handle!,
            LandSide.Axis);
        Assert.True(observation.IsProjected);
        Assert.Equal(
            CampaignObservationV6.CurrentPolicyId,
            Assert.IsType<CampaignObservationV6>(observation.Observation).PolicyId);

        var legacyRequest = new CampaignCreationRequest(
            CampaignCreationRequest.CurrentContractVersion,
            request.CampaignId,
            request.RulesetHash,
            request.Seed,
            definition.SetupId,
            definition.Hash,
            Cna1979SyntheticContentCatalog.Artifact.Identity.PackId,
            Cna1979SyntheticContentCatalog.Artifact.Identity.Hash,
            definition.Content.ScenarioId);
        Assert.False(CampaignExercises.Begin(legacyRequest).IsStarted);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void PublicCreationRejectsEveryLegacyCurrentIdentityMixture(
        bool legacyRuleset,
        bool legacySetup,
        bool legacyContent)
    {
        const string legacyRulesetHash =
            "f135fb9582a9aabd8fdd628df3d9fef732cea2fc81967632f7ddb85c1650d387";
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV5;
        var legacyArtifact = Cna1979SyntheticContentCatalog.Artifact;
        var definition = Cna1979SetupCatalog.Definitions[0];
        var currentSetup = CampaignSetupSnapshotV5.FromPredecessor(
            CampaignSetupSnapshot.FromDefinition(definition),
            new CampaignContentV5Selection(
                artifact.Identity,
                definition.Content.ScenarioId));
        var request = new CampaignCreationRequest(
            CampaignCreationRequest.CurrentContractVersion,
            $"campaign-mixed-{legacyRuleset}-{legacySetup}-{legacyContent}",
            legacyRuleset ? legacyRulesetHash : Cna1979Ruleset.Manifest.Hash,
            12345,
            definition.SetupId,
            legacySetup ? definition.Hash : currentSetup.SetupHash,
            legacyContent ? legacyArtifact.Identity.PackId : artifact.Identity.PackId,
            legacyContent ? legacyArtifact.Identity.Hash : artifact.Identity.Hash,
            definition.Content.ScenarioId);

        Assert.False(CampaignExercises.Begin(request).IsStarted);
        Assert.False(CampaignAuthority.Create(request).IsCreated);
    }

    [Fact]
    public void PublicObservationPublishesTruthfulSourceFlagsWithoutControlMappings()
    {
        var positive = CampaignV10TestData.CreateWithReactors(
            ["commonwealth-reactor-alpha", "commonwealth-reactor-bravo"],
            reactorLocationId: "north");
        var positiveHandle = new CampaignAuthorityHandle(
            positive.MovementSnapshot,
            CampaignContentContext.Create(positive.Artifact, positive.Scenario.ScenarioId));
        var positiveResult = CampaignObservations.Query(positiveHandle, LandSide.Axis);
        var positiveObservation = Assert.IsType<CampaignObservationV6>(
            positiveResult.Observation);

        Assert.Equal(["east"], positiveObservation.ApparentEnemyControlledLocationIds);
        Assert.Equal(2, positiveObservation.ApparentOpposingPresences.Count);
        Assert.All(positiveObservation.ApparentOpposingPresences, presence =>
            Assert.True(presence.ExertsZoc));

        var negative = CampaignV10TestData.CreateWithReactors(
            ["commonwealth-lone-reactor"],
            reactorLocationId: "north");
        var negativeHandle = new CampaignAuthorityHandle(
            negative.MovementSnapshot,
            CampaignContentContext.Create(negative.Artifact, negative.Scenario.ScenarioId));
        var negativeResult = CampaignObservations.Query(negativeHandle, LandSide.Axis);
        var negativeObservation = Assert.IsType<CampaignObservationV6>(
            negativeResult.Observation);

        Assert.Empty(negativeObservation.ApparentEnemyControlledLocationIds);
        Assert.All(negativeObservation.ApparentOpposingPresences, presence =>
            Assert.False(presence.ExertsZoc));
        Assert.DoesNotContain(
            typeof(CampaignObservationV6).GetProperties(),
            property => property.Name.Contains("Mapping", StringComparison.Ordinal)
                || property.Name.Contains("Source", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false, CampaignActionAudience.Axis)]
    [InlineData(true, CampaignActionAudience.Commonwealth)]
    public void PublicNormalProgressionResolvesFirstActingSide(
        bool actLast,
        CampaignActionAudience expectedAudience)
    {
        var handle = CreatePublicHandle(
            actLast ? "campaign-current-reserve-last" : "campaign-current-reserve-first");
        while (handle.CurrentSnapshot!.CurrentPosition.SequencePosition!.PhaseId
            != LandPhaseIds.ReserveDesignation)
        {
            var sets = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => Query(handle, audience))
                .Where(set => set.Candidates.Count > 0)
                .ToArray();
            var set = Assert.Single(sets);
            var candidate = set.Candidates.FirstOrDefault(value => actLast
                ? value is ActLastAction
                : value is ActFirstAction) ?? Assert.Single(set.Candidates);
            handle = Submit(handle, set, candidate);
        }

        var expected = Query(handle, expectedAudience);
        var other = Query(
            handle,
            expectedAudience == CampaignActionAudience.Axis
                ? CampaignActionAudience.Commonwealth
                : CampaignActionAudience.Axis);

        Assert.Equal(3, expected.Candidates.Count);
        Assert.Empty(other.Candidates);

        var movement = Submit(
            handle,
            expected,
            Assert.Single(expected.Candidates.OfType<CompleteReserveDesignationAction>()));
        Assert.Equal(
            expectedAudience == CampaignActionAudience.Axis
                ? LandSide.Axis
                : LandSide.Commonwealth,
            movement.CurrentSnapshot!.CurrentPosition.SequencePosition!.ActiveSide);
        var movementActions = Query(movement, expectedAudience);
        var moved = Submit(
            movement,
            movementActions,
            movementActions.Candidates.OfType<MoveElementAction>().First());
        Assert.Equal(
            movement.CurrentSnapshot.StateVersion + 1,
            moved.CurrentSnapshot!.StateVersion);
    }

    [Fact]
    public void PublicSubmissionRunsReactionEpisodeAndResumesExactMovement()
    {
        const string firstReactor = "commonwealth-reactor-alpha";
        const string secondReactor = "commonwealth-reactor-bravo";
        var fixture = CampaignV10TestData.CreateWithReactors(
            [firstReactor, secondReactor],
            includeReactionExit: true,
            includeReactionContinuation: true,
            reactorLocationId: "north",
            reactorLocationIds: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [firstReactor] = "north",
                [secondReactor] = "north-two",
            });
        var context = CampaignContentContext.Create(
            fixture.Artifact,
            fixture.Scenario.ScenarioId);
        var handle = new CampaignAuthorityHandle(fixture.MovementSnapshot, context);
        var suspended = fixture.MovementSnapshot.CurrentPosition.SequencePosition!;
        var phasing = Query(handle, CampaignActionAudience.Axis);
        var trigger = Assert.Single(phasing.Candidates.OfType<MoveElementAction>(), candidate =>
            candidate.ElementId == fixture.TriggeringMove.ElementId
            && candidate.OriginLocationId == fixture.TriggeringMove.OriginLocationId
            && candidate.DestinationLocationId == fixture.TriggeringMove.DestinationLocationId);

        var opened = Submit(handle, phasing, trigger);

        Assert.Equal(CampaignPositionV10Kind.Reaction, opened.CurrentSnapshot!.CurrentPosition.Kind);
        Assert.Equal(2, opened.CurrentSnapshot.ReactionWindow!.FrozenOpportunities.Count);
        Assert.Empty(Query(opened, CampaignActionAudience.Axis).Candidates);
        var reacting = Query(opened, CampaignActionAudience.Commonwealth);
        Assert.Contains(reacting.Candidates, candidate => candidate is DeclineReactionWindowAction);
        var firstMove = reacting.Candidates.OfType<MoveReactingElementAction>().First();
        var active = Submit(opened, reacting, firstMove);
        var activeSet = Query(active, CampaignActionAudience.Commonwealth);
        Assert.DoesNotContain(activeSet.Candidates, candidate =>
            candidate is DeclineReactionWindowAction);
        var completion = Assert.Single(
            activeSet.Candidates.OfType<CompleteReactionParticipantAction>());
        var completed = Submit(active, activeSet, completion);
        var remaining = Query(completed, CampaignActionAudience.Commonwealth);
        Assert.Contains(remaining.Candidates, candidate =>
            candidate is MoveReactingElementAction);
        var decline = Assert.Single(remaining.Candidates.OfType<DeclineReactionWindowAction>());
        var closed = Submit(completed, remaining, decline);

        Assert.Null(closed.CurrentSnapshot!.ReactionWindow);
        Assert.Equal(
            suspended,
            closed.CurrentSnapshot.CurrentPosition.SequencePosition);
        Assert.Equal(
            completed.CurrentSnapshot!.StateVersion + 1,
            closed.CurrentSnapshot.StateVersion);

        var stale = new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion,
            remaining.CampaignId,
            remaining.StateVersion,
            remaining.PositionId,
            remaining.Audience,
            decline.ActionId);
        var rejected = CampaignLegalActions.Submit(closed, stale);
        Assert.False(rejected.IsAccepted);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, rejected.RejectionReason);
    }

    [Theory]
    [InlineData("north", "north-two")]
    [InlineData("north-two", "north")]
    public void PublicSubmissionPreservesEitherParticipantOrderAndReplaysExactly(
        string firstOrigin,
        string secondOrigin)
    {
        var fixture = CampaignV10TestData.CreateWithReactors(
            ["commonwealth-reactor-alpha", "commonwealth-reactor-bravo"],
            includeReactionExit: true,
            reactorLocationId: "north",
            reactorLocationIds: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["commonwealth-reactor-alpha"] = "north",
                ["commonwealth-reactor-bravo"] = "north-two",
            });
        var context = CampaignContentContext.Create(fixture.Artifact, fixture.Scenario.ScenarioId);
        var initial = fixture.MovementSnapshot;
        var handle = new CampaignAuthorityHandle(initial, context);
        var events = new List<object>();
        var phasing = Query(handle, CampaignActionAudience.Axis);
        var trigger = Assert.Single(phasing.Candidates.OfType<MoveElementAction>(), candidate =>
            candidate.ElementId == fixture.TriggeringMove.ElementId
            && candidate.DestinationLocationId == fixture.TriggeringMove.DestinationLocationId);
        handle = SubmitAndCapture(handle, phasing, trigger, events);

        foreach (var origin in new[] { firstOrigin, secondOrigin })
        {
            var reacting = Query(handle, CampaignActionAudience.Commonwealth);
            var move = Assert.Single(
                reacting.Candidates.OfType<MoveReactingElementAction>(),
                candidate => candidate.OriginLocationId == origin
                    && candidate.DestinationLocationId == "center");
            handle = SubmitAndCapture(handle, reacting, move, events);
            var active = Query(handle, CampaignActionAudience.Commonwealth);
            handle = SubmitAndCapture(
                handle,
                active,
                Assert.Single(active.Candidates.OfType<CompleteReactionParticipantAction>()),
                events);
        }

        var system = Query(handle, CampaignActionAudience.System);
        handle = SubmitAndCapture(handle, system, Assert.Single(system.Candidates), events);

        Assert.Equal(
            [firstOrigin, secondOrigin],
            events.OfType<ReactingElementMoved>().Select(value => value.OriginLocationId));
        var replayed = events.Aggregate(
            initial,
            (snapshot, campaignEvent) => CampaignCurrentProjector.Apply(
                snapshot,
                CampaignCurrentEventSerializer.Deserialize(
                    CampaignCurrentEventSerializer.Serialize(campaignEvent)),
                context));
        Assert.Equal(
            CampaignSnapshotV10Serializer.Serialize(handle.CurrentSnapshot!),
            CampaignSnapshotV10Serializer.Serialize(replayed));
    }

    [Fact]
    public void PublicSubmissionRecalculatesPriorParticipantForLaterTrigger()
    {
        const string reactor = "commonwealth-repeat-reactor";
        var fixture = CampaignV10TestData.CreateWithReactors(
            [reactor],
            includeReactionExit: true,
            includeReactionContinuation: true,
            reactorLocationId: "north");
        var context = CampaignContentContext.Create(fixture.Artifact, fixture.Scenario.ScenarioId);
        var handle = new CampaignAuthorityHandle(fixture.MovementSnapshot, context);
        var events = new List<object>();
        var phasing = Query(handle, CampaignActionAudience.Axis);
        var firstTrigger = Assert.Single(phasing.Candidates.OfType<MoveElementAction>(), candidate =>
            candidate.ElementId == fixture.TriggeringMove.ElementId
            && candidate.OriginLocationId == "west"
            && candidate.DestinationLocationId == "east");
        handle = SubmitAndCapture(handle, phasing, firstTrigger, events);
        var reacting = Query(handle, CampaignActionAudience.Commonwealth);
        var firstReaction = Assert.Single(
            reacting.Candidates.OfType<MoveReactingElementAction>(),
            candidate => candidate.OriginLocationId == "north"
                && candidate.DestinationLocationId == "center");
        handle = SubmitAndCapture(handle, reacting, firstReaction, events);
        var active = Query(handle, CampaignActionAudience.Commonwealth);
        handle = SubmitAndCapture(
            handle,
            active,
            Assert.Single(active.Candidates.OfType<CompleteReactionParticipantAction>()),
            events);
        var firstClose = Query(handle, CampaignActionAudience.System);
        handle = SubmitAndCapture(
            handle,
            firstClose,
            Assert.Single(firstClose.Candidates),
            events);

        var resumed = Query(handle, CampaignActionAudience.Axis);
        var laterTrigger = Assert.Single(resumed.Candidates.OfType<MoveElementAction>(), candidate =>
            candidate.ElementId == fixture.TriggeringMove.ElementId
            && candidate.OriginLocationId == "east"
            && candidate.DestinationLocationId == "west");
        handle = SubmitAndCapture(handle, resumed, laterTrigger, events);
        var repeated = Query(handle, CampaignActionAudience.Commonwealth);

        Assert.Contains(
            repeated.Candidates.OfType<MoveReactingElementAction>(),
            candidate => candidate.OriginLocationId == "center"
                && candidate.DestinationLocationId == "south");
        var replayed = events.Aggregate(
            fixture.MovementSnapshot,
            (snapshot, campaignEvent) => CampaignCurrentProjector.Apply(
                snapshot,
                CampaignCurrentEventSerializer.Deserialize(
                    CampaignCurrentEventSerializer.Serialize(campaignEvent)),
                context));
        Assert.Equal(
            CampaignSnapshotV10Serializer.Serialize(handle.CurrentSnapshot!),
            CampaignSnapshotV10Serializer.Serialize(replayed));
    }

    [Theory]
    [InlineData("capability")]
    [InlineData("cohesion")]
    [InlineData("reserve")]
    [InlineData("ledger-turn")]
    [InlineData("ledger-stage")]
    [InlineData("movement-ended")]
    [InlineData("position")]
    [InlineData("zoc")]
    public void PublicLaterTriggerExcludesPriorParticipantWhenCurrentRestrictionFails(
        string restriction)
    {
        const string reactor = "commonwealth-prior-reactor";
        const string anchor = "commonwealth-trigger-anchor";
        var fixture = CampaignV10TestData.CreateWithReactors(
            [reactor, anchor],
            includeReactionExit: true,
            includeReactionContinuation: true,
            reactorLocationId: "north",
            includeRemoteArea: true,
            reactorLocationIds: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [reactor] = "north",
                [anchor] = "north-two",
            },
            includePhasingZocSupport: restriction == "zoc");
        var context = CampaignContentContext.Create(fixture.Artifact, fixture.Scenario.ScenarioId);
        var handle = new CampaignAuthorityHandle(fixture.MovementSnapshot, context);
        var phasing = Query(handle, CampaignActionAudience.Axis);
        handle = Submit(
            handle,
            phasing,
            Assert.Single(phasing.Candidates.OfType<MoveElementAction>(), candidate =>
                candidate.ElementId == fixture.TriggeringMove.ElementId
                && candidate.OriginLocationId == "west"
                && candidate.DestinationLocationId == "east"));
        var reacting = Query(handle, CampaignActionAudience.Commonwealth);
        handle = Submit(
            handle,
            reacting,
            Assert.Single(reacting.Candidates.OfType<MoveReactingElementAction>(), candidate =>
                candidate.OriginLocationId == "north"
                && candidate.DestinationLocationId == "center"));
        var active = Query(handle, CampaignActionAudience.Commonwealth);
        handle = Submit(
            handle,
            active,
            Assert.Single(active.Candidates.OfType<CompleteReactionParticipantAction>()));
        var remaining = Query(handle, CampaignActionAudience.Commonwealth);
        handle = Submit(
            handle,
            remaining,
            Assert.Single(remaining.Candidates.OfType<DeclineReactionWindowAction>()));
        handle = ApplyLaterTriggerRestriction(handle, fixture, reactor, anchor, restriction);

        var replayPrior = handle.CurrentSnapshot!;
        var laterEvents = new List<object>();
        var resumed = Query(handle, CampaignActionAudience.Axis);
        handle = SubmitAndCapture(
            handle,
            resumed,
            Assert.Single(resumed.Candidates.OfType<MoveElementAction>(), candidate =>
                candidate.ElementId == fixture.TriggeringMove.ElementId
                && candidate.OriginLocationId == "east"
                && candidate.DestinationLocationId == "west"),
            laterEvents);
        var repeated = Query(handle, CampaignActionAudience.Commonwealth);
        var excludedOrigin = restriction == "position" ? "remote-source" : "center";

        Assert.NotNull(handle.CurrentSnapshot!.ReactionWindow);
        Assert.DoesNotContain(
            repeated.Candidates.OfType<MoveReactingElementAction>(),
            candidate => candidate.OriginLocationId == excludedOrigin);
        if (restriction == "position")
        {
            Assert.Contains(
                repeated.Candidates.OfType<MoveReactingElementAction>(),
                candidate => candidate.OriginLocationId == "center");
        }

        var replayed = laterEvents.Aggregate(
            replayPrior,
            (snapshot, campaignEvent) => CampaignCurrentProjector.Apply(
                snapshot,
                CampaignCurrentEventSerializer.Deserialize(
                    CampaignCurrentEventSerializer.Serialize(campaignEvent)),
                context));
        Assert.Equal(
            CampaignSnapshotV10Serializer.Serialize(handle.CurrentSnapshot),
            CampaignSnapshotV10Serializer.Serialize(replayed));
    }

    [Fact]
    public void PublicExerciseSessionKeepsReactionAuthorityCurrent()
    {
        var start = CampaignExercises.Begin(CreatePublicRequest("campaign-session-reaction"));
        Assert.True(start.IsStarted);
        var session = start.Session!;
        while (session.CurrentSnapshot.CurrentPosition.SequencePosition!.SegmentId
            != LandSegmentIds.Movement)
        {
            var sets = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .Where(set => set.Candidates.Count > 0)
                .ToArray();
            var set = Assert.Single(sets);
            var candidate = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? set.Candidates.FirstOrDefault(value =>
                    value is CompleteReserveDesignationAction)
                ?? Assert.Single(set.Candidates);
            var step = CampaignExercises.Submit(session, Submission(set, candidate));
            Assert.True(step.IsAccepted);
            session = step.SuccessorSession!;
        }

        var phasing = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!;
        var trigger = Assert.Single(phasing.Candidates.OfType<MoveElementAction>(), candidate =>
            candidate.DestinationLocationId == "center");

        var opened = CampaignExercises.Submit(session, Submission(phasing, trigger));

        Assert.True(opened.IsAccepted);
        var reactingSession = opened.SuccessorSession!;
        Assert.NotNull(reactingSession.CurrentSnapshot.ReactionWindow);
        Assert.Equal(
            session.CurrentSnapshot.StateVersion,
            reactingSession.Snapshot.StateVersion);
        var system = CampaignExercises.Query(
            reactingSession,
            CampaignActionAudience.System).ActionSet!;
        var close = Assert.Single(system.Candidates, candidate =>
            candidate is CloseReactionWindowUnavailableAction);

        var closed = CampaignExercises.Submit(
            reactingSession,
            Submission(system, close));

        Assert.True(closed.IsAccepted);
        Assert.Null(closed.SuccessorSession!.CurrentSnapshot.ReactionWindow);
        Assert.Equal(
            closed.SuccessorSession.CurrentSnapshot.StateVersion,
            closed.SuccessorSession.Snapshot.StateVersion);
    }

    [Theory]
    [InlineData("close-reaction-window-scripted-unavailable")]
    [InlineData("close-reaction-window-timeout")]
    public void PublicSystemFallbackClosesReactionAndResumesExactMovement(string kind)
    {
        var fixture = CampaignV10TestData.CreateWithReactors(
            ["commonwealth-reactor"],
            includeReactionExit: true,
            reactorLocationId: "north");
        var context = CampaignContentContext.Create(
            fixture.Artifact,
            fixture.Scenario.ScenarioId);
        var handle = new CampaignAuthorityHandle(fixture.MovementSnapshot, context);
        var suspended = fixture.MovementSnapshot.CurrentPosition.SequencePosition!;
        var phasing = Query(handle, CampaignActionAudience.Axis);
        var trigger = Assert.Single(phasing.Candidates.OfType<MoveElementAction>(), candidate =>
            candidate.ElementId == fixture.TriggeringMove.ElementId
            && candidate.OriginLocationId == fixture.TriggeringMove.OriginLocationId
            && candidate.DestinationLocationId == fixture.TriggeringMove.DestinationLocationId);
        var opened = Submit(handle, phasing, trigger);
        var system = Query(opened, CampaignActionAudience.System);
        var close = Assert.Single(system.Candidates, candidate => candidate.Kind == kind);

        var closed = Submit(opened, system, close);

        Assert.Null(closed.CurrentSnapshot!.ReactionWindow);
        Assert.Equal(suspended, closed.CurrentSnapshot.CurrentPosition.SequencePosition);
    }

    [Fact]
    public void PublicSystemClosesEmptyReactionWindowDeterministically()
    {
        const string reactor = "commonwealth-no-eligible";
        var fixture = CampaignV10TestData.CreateWithReactors(
            [reactor],
            includeReactionExit: true,
            reactorLocationId: "north");
        var snapshot = EndMovementFor(fixture.MovementSnapshot, reactor);
        var context = CampaignContentContext.Create(
            fixture.Artifact,
            fixture.Scenario.ScenarioId);
        var handle = new CampaignAuthorityHandle(snapshot, context);
        var phasing = Query(handle, CampaignActionAudience.Axis);
        var trigger = Assert.Single(phasing.Candidates.OfType<MoveElementAction>(), candidate =>
            candidate.ElementId == fixture.TriggeringMove.ElementId
            && candidate.OriginLocationId == fixture.TriggeringMove.OriginLocationId
            && candidate.DestinationLocationId == fixture.TriggeringMove.DestinationLocationId);
        var opened = Submit(handle, phasing, trigger);

        Assert.Empty(opened.CurrentSnapshot!.ReactionWindow!.FrozenOpportunities);
        var system = Query(opened, CampaignActionAudience.System);
        var close = Assert.Single(system.Candidates);
        Assert.Equal("close-reaction-window-no-eligible-reactor", close.Kind);
        var closed = Submit(opened, system, close);

        Assert.Null(closed.CurrentSnapshot!.ReactionWindow);
        Assert.Equal(
            snapshot.CurrentPosition.SequencePosition,
            closed.CurrentSnapshot.CurrentPosition.SequencePosition);
    }

    [Fact]
    public void CurrentEventRouterRejectsLegacyCreationAndMovementRoots()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var creation = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
                "campaign-legacy-router",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash));
        var created = Assert.IsType<CampaignCreated>(Assert.Single(creation.Events));
        var movement = CampaignMovementTestData.ReachMovement();
        var candidate = CampaignMovementTestData.FindMove(
            movement.Snapshot,
            movement.Context,
            movement.ActingSide,
            "commonwealth-element-a",
            "north-east");
        var execution = CampaignEngine.Decide(
            movement.Snapshot,
            CampaignMovementTestData.CommandFor(
                movement.Snapshot,
                movement.ActingSide,
                candidate),
            movement.Context);
        var moved = Assert.IsType<ElementMoved>(Assert.Single(execution.Events));

        Assert.Throws<System.Text.Json.JsonException>(() =>
            CampaignCurrentEventSerializer.Serialize(created));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            CampaignCurrentEventSerializer.Deserialize(
                CampaignEventSerializer.Serialize(created)));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            CampaignCurrentEventSerializer.Serialize(moved));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            CampaignCurrentEventSerializer.Deserialize(
                CampaignEventSerializer.Serialize(moved)));
    }

    private static CampaignSnapshotV10 EndMovementFor(
        CampaignSnapshotV10 snapshot,
        string elementId)
    {
        var movement = snapshot.CurrentPosition.SequencePosition!;
        var current = snapshot.World.Elements.Single(element =>
            element.ElementId == elementId);
        var operational = current.OperationalState;
        var replacement = new CampaignElementStateV5(
            current.ElementId,
            current.CurrentLocationId,
            current.ReserveStatus,
            new CampaignElementOperationalStateV5(
                operational.LedgerGameTurn,
                operational.LedgerOperationStage,
                operational.CapabilityPointsExpended,
                operational.CohesionLevel,
                operational.VehicleBreakdownState,
                new CampaignMovementEndedState(movement)),
            current.Components);
        var world = new CampaignWorldSnapshotV5(
            CampaignWorldSnapshotV5.CurrentContractVersion,
            snapshot.World.Elements.Select(element => element.ElementId == elementId
                ? replacement
                : element),
            snapshot.World.Representations);
        return new CampaignSnapshotV10(
            CampaignSnapshotV10.CurrentContractVersion,
            snapshot.CampaignId,
            snapshot.StateVersion,
            snapshot.RulesetHash,
            snapshot.Setup,
            world,
            snapshot.InitiativeHolder,
            snapshot.OperationStageOrders,
            snapshot.OperationStageWeather,
            snapshot.RandomState,
            snapshot.CurrentPosition,
            snapshot.ReactionWindow);
    }

    private static CampaignAuthorityHandle CreatePublicHandle(string campaignId)
    {
        var result = CampaignAuthority.Create(CreatePublicRequest(campaignId));
        Assert.True(result.IsCreated);
        return result.Handle!;
    }

    private static CampaignCreationRequest CreatePublicRequest(string campaignId)
    {
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV5;
        var definition = Cna1979SetupCatalog.Definitions[0];
        var setup = CampaignSetupSnapshotV5.FromPredecessor(
            CampaignSetupSnapshot.FromDefinition(definition),
            new CampaignContentV5Selection(
                artifact.Identity,
                definition.Content.ScenarioId));
        return new CampaignCreationRequest(
            CampaignCreationRequest.CurrentContractVersion,
            campaignId,
            Cna1979Ruleset.Manifest.Hash,
            12345,
            setup.SetupId,
            setup.SetupHash,
            artifact.Identity.PackId,
            artifact.Identity.Hash,
            setup.Content.ScenarioId);
    }

    private static CampaignLegalActionSet Query(
        CampaignAuthorityHandle handle,
        CampaignActionAudience audience)
    {
        var query = CampaignLegalActions.Query(handle, audience);
        Assert.True(query.IsSuccessful);
        return query.ActionSet!;
    }

    private static CampaignAuthorityHandle Submit(
        CampaignAuthorityHandle handle,
        CampaignLegalActionSet set,
        CampaignActionCandidate candidate)
    {
        var result = CampaignLegalActions.Submit(handle, new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion,
            set.CampaignId,
            set.StateVersion,
            set.PositionId,
            set.Audience,
            candidate.ActionId));
        Assert.True(result.IsAccepted);
        return result.SuccessorHandle!;
    }

    private static CampaignAuthorityHandle SubmitAndCapture(
        CampaignAuthorityHandle handle,
        CampaignLegalActionSet set,
        CampaignActionCandidate candidate,
        List<object> events)
    {
        var submission = Submission(set, candidate);
        var execution = CampaignCurrentActionExecution.Execute(
            handle.CurrentSnapshot!,
            handle.Context,
            submission);
        Assert.True(execution.IsAccepted);
        events.Add(execution.AcceptedEvent!);

        var result = CampaignLegalActions.Submit(handle, submission);
        Assert.True(result.IsAccepted);
        Assert.Equal(
            CampaignSnapshotV10Serializer.Serialize(execution.SuccessorSnapshot!),
            CampaignSnapshotV10Serializer.Serialize(result.SuccessorHandle!.CurrentSnapshot!));
        return result.SuccessorHandle;
    }

    private static CampaignAuthorityHandle ApplyLaterTriggerRestriction(
        CampaignAuthorityHandle handle,
        CampaignV10Fixture fixture,
        string reactor,
        string anchor,
        string restriction)
    {
        var snapshot = handle.CurrentSnapshot!;
        var movement = snapshot.CurrentPosition.SequencePosition!;
        var allowance = fixture.Artifact.Definition.LegacyDefinition.Elements
            .Single(value => value.ElementId == reactor)
            .BaseCapabilityPointAllowance;
        var changedLocations = new Dictionary<string, string>(StringComparer.Ordinal);
        if (restriction == "position")
        {
            changedLocations[reactor] = "remote-source";
            changedLocations[anchor] = "center";
        }

        var world = new CampaignWorldSnapshotV5(
            CampaignWorldSnapshotV5.CurrentContractVersion,
            snapshot.World.Elements.Select(element =>
            {
                var location = changedLocations.GetValueOrDefault(
                    element.ElementId,
                    element.CurrentLocationId);
                if (element.ElementId != reactor)
                {
                    return location == element.CurrentLocationId
                        ? element
                        : new CampaignElementStateV5(
                            element.ElementId,
                            location,
                            element.ReserveStatus,
                            element.OperationalState,
                            element.Components);
                }

                var operational = element.OperationalState;
                return new CampaignElementStateV5(
                    element.ElementId,
                    location,
                    restriction == "reserve"
                        ? CampaignElementReserveStatus.ReserveI
                        : element.ReserveStatus,
                    new CampaignElementOperationalStateV5(
                        restriction == "ledger-turn"
                            ? checked(operational.LedgerGameTurn + 1)
                            : operational.LedgerGameTurn,
                        restriction == "ledger-stage"
                            ? checked(operational.LedgerOperationStage + 1)
                            : operational.LedgerOperationStage,
                        restriction == "capability"
                            ? new CapabilityPointAmount(allowance, 1)
                            : operational.CapabilityPointsExpended,
                        restriction == "cohesion" ? -26 : operational.CohesionLevel,
                        operational.VehicleBreakdownState,
                        restriction == "movement-ended"
                            ? new CampaignMovementEndedState(movement)
                            : operational.MovementEnded),
                    element.Components);
            }),
            snapshot.World.Representations.Select(representation =>
            {
                var locations = representation.BoundElementIds
                    .Where(changedLocations.ContainsKey)
                    .Select(elementId => changedLocations[elementId])
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                return locations.Length == 0
                    ? representation
                    : new CampaignMapRepresentationState(
                        representation.RepresentationId,
                        Assert.Single(locations),
                        representation.BindingKind,
                        representation.BoundElementIds);
            }));
        var changed = new CampaignSnapshotV10(
            snapshot.ContractVersion,
            snapshot.CampaignId,
            snapshot.StateVersion,
            snapshot.RulesetHash,
            snapshot.Setup,
            world,
            snapshot.InitiativeHolder,
            snapshot.OperationStageOrders,
            snapshot.OperationStageWeather,
            snapshot.RandomState,
            snapshot.CurrentPosition,
            snapshot.ReactionWindow);
        return new CampaignAuthorityHandle(changed, handle.Context);
    }

    private static CampaignActionSubmission Submission(
        CampaignLegalActionSet set,
        CampaignActionCandidate candidate) => new(
        CampaignActionSubmission.CurrentContractVersion,
        set.CampaignId,
        set.StateVersion,
        set.PositionId,
        set.Audience,
        candidate.ActionId);
}
