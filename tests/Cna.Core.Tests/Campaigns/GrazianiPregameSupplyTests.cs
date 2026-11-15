using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiPregameSupplyTests
{
    [Fact]
    public void SourcePregameCannotSkipDirectlyToSecondBreakdown()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "no-pregame-clock-skip", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var session = CampaignExercises.Begin(request, artifact, init).Session!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.CompleteSecondBreakdown(session).RejectionReason);
        Assert.Equal(CampaignPregameSourceCreation.PositionId, CampaignExercises.QueryCheckpoint(session).PositionId);
    }

    [Theory]
    [InlineData(14, 1, 0)]
    [InlineData(1, 2, 1)]
    [InlineData(5, 3, 1)]
    [InlineData(8, 4, 2)]
    [InlineData(2, 5, 2)]
    [InlineData(0, 6, 3)]
    [InlineData(94, 2, 1)]
    public void SeededNormalActionsCoverEverySourceBColumnOutcome(int seed, int die, int available)
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(new CapabilityPointAmount(0, 1), 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "graziani-b-column", Cna1979Ruleset.Manifest.Hash, (ulong)seed,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var started = CampaignExercises.Begin(request, artifact, init);
        var action = Assert.Single(CampaignExercises.Query(started.Session!, CampaignActionAudience.Axis).ActionSet!.Candidates);
        var result = Accept(started.Session!, action);
        using var checkpoint = JsonDocument.Parse(result.Evidence!.SnapshotCheckpoint);
        Assert.Equal(die, checkpoint.RootElement.GetProperty("die").GetInt32());
        Assert.Equal(available, checkpoint.RootElement.GetProperty("maximumSupply").GetInt32());
        Assert.Equal(seed == 94 ? 2UL : 1UL, checkpoint.RootElement.GetProperty("randomCursor").GetUInt64());
        Assert.True(CampaignExercises.Reconstruct(result.SuccessorSession!).IsVerified);
    }

    [Fact]
    public void PregameActionPolicyIsVersionedAndCannotBeMixedWithLegacyActions()
    {
        var hash = Cna1979Ruleset.Manifest.Hash;
        var sourceAction = new PregameAxisSupplyRollAction();
        var legacyAction = new ResolveInitiativeAction();
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet("policy-test", 1, hash,
            CampaignPregameSourceCreation.PositionId, CampaignActionAudience.Axis, [sourceAction]));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet("policy-test", 1,
            Cna1979Ruleset.HistoricalManifestV8.Hash, CampaignPregameSourceCreation.PositionId,
            CampaignActionAudience.Axis, [sourceAction], CampaignLegalActionSet.HistoricalPolicyIdV2));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet("policy-test", 1, hash,
            CampaignPregameSourceCreation.PositionId, CampaignActionAudience.Axis, [legacyAction],
            CampaignLegalActionSet.PregamePolicyId));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet("policy-test", 1, hash,
            CampaignPregameSourceCreation.PositionId, CampaignActionAudience.Commonwealth, [sourceAction],
            CampaignLegalActionSet.PregamePolicyId));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet("policy-test", 1, hash,
            CampaignPregameSourceCreation.PositionId, CampaignActionAudience.Axis, [],
            CampaignLegalActionSet.PregamePolicyId));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet("policy-test", 2, hash,
            CampaignPregameSourceCreation.PositionId, CampaignActionAudience.Axis, [sourceAction],
            CampaignLegalActionSet.PregamePolicyId));
        _ = new CampaignLegalActionSet("policy-test", 2, hash, CampaignPregameSourceCreation.PositionId,
            CampaignActionAudience.Axis, [], CampaignLegalActionSet.PregamePolicyId);
        var legacy = new CampaignLegalActionSet("policy-test", 1, hash, "legacy-position",
            CampaignActionAudience.System, [legacyAction]);
        Assert.Equal(CampaignObservationV7LegalActionSerializer.Serialize(legacy), CampaignLegalActionSerializer.Serialize(legacy));
        Assert.Equal(legacy, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(legacy)));
    }

    [Fact]
    public void ZeroAvailabilityDoesNotCompleteOrAdvanceAndRejectsUnknownAllocations()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(new CapabilityPointAmount(0, 1), 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "graziani-zero", Cna1979Ruleset.Manifest.Hash, 14,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var start = CampaignExercises.Begin(request, artifact, init);
        var rolled = Accept(start.Session!, Assert.Single(CampaignExercises.Query(start.Session!, CampaignActionAudience.Axis).ActionSet!.Candidates));
        using var checkpoint = JsonDocument.Parse(rolled.Evidence!.SnapshotCheckpoint);
        Assert.Equal(1, checkpoint.RootElement.GetProperty("die").GetInt32());
        Assert.Equal(0, checkpoint.RootElement.GetProperty("maximumSupply").GetInt32());
        Assert.Empty(CampaignExercises.Query(rolled.SuccessorSession!, CampaignActionAudience.Axis).ActionSet!.Candidates);
        Assert.Equal(CampaignPregameSourceCreation.PositionId, CampaignExercises.QueryCheckpoint(rolled.SuccessorSession!).PositionId);
        Assert.Throws<ArgumentException>(() => new PregameAxisSupplyAllocationAction(1, 1, 1));
        Assert.Throws<ArgumentException>(() => new PregameAxisSupplyAllocationAction(2, 4, 1));
        var cut = CampaignExercises.QueryCheckpoint(rolled.SuccessorSession!);
        var forged = new CampaignActionSubmission(1, cut.CampaignId, cut.StateVersion, cut.PositionId,
            CampaignActionAudience.Axis, "sha256:" + new string('0', 64));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.Submit(rolled.SuccessorSession!, forged).RejectionReason);
        using var draft = JsonDocument.Parse(CampaignExercises.QueryPregameAxisDraft(
            rolled.SuccessorSession!, CampaignActionAudience.Axis).VisibleBytes);
        Assert.Equal("incomplete", draft.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void RealPregameSupplyRollAndPrivateAllocationsReplayWithoutAdvancingGame()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        Assert.Equal(CampaignPregameSourceCreation.SupportedContentHash, artifact.Identity.Hash);
        var init = new CampaignSourceInitialization(new CapabilityPointAmount(0, 1), 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "graziani-pregame-supply", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var start = CampaignExercises.Begin(request, artifact, init);
        Assert.True(start.IsStarted);
        using var genesis = JsonDocument.Parse(start.CreationEventBytes!);
        Assert.Equal(CampaignPregameSourceCreation.PolicyId, genesis.RootElement.GetProperty("policyId").GetString());
        var sources = genesis.RootElement.GetProperty("sources").EnumerateArray()
            .Select(value => value.GetString()).ToArray();
        Assert.Contains(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.43", sources);
        Assert.Contains(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.44", sources);
        Assert.Contains(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.61", sources);
        Assert.Contains(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.62", sources);
        Assert.Contains(Cna1979GrazianiSourceFacts.AxisChartsSourceId + ":32.46", sources);
        Assert.Contains(Cna1979GrazianiSourceFacts.AxisChartsSourceId + ":56.4", sources);
        Assert.Contains(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.37", sources);
        Assert.Contains(Cna1979GrazianiSourceFacts.AirLogisticsSourceId + ":56.18", sources);
        Assert.Contains(Cna1979GrazianiSourceFacts.ErrataSourceId + ":56.25", sources);
        Assert.Contains(Cna1979GrazianiSourceFacts.ErrataSourceId + ":56.29", sources);
        var initial = start.Session!;
        Assert.True(CampaignExercises.IsPregameAxisSetup(initial, CampaignActionAudience.Axis));
        Assert.False(CampaignExercises.IsPregameAxisSetup(initial, CampaignActionAudience.Commonwealth));
        Assert.False(CampaignExercises.Query(initial, CampaignActionAudience.Commonwealth).IsSuccessful);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisDraft(initial, CampaignActionAudience.Commonwealth));
        var initialSet = CampaignExercises.Query(initial, CampaignActionAudience.Axis).ActionSet!;
        Assert.Equal(CampaignLegalActionSet.PregamePolicyId, initialSet.PolicyId);
        Assert.IsType<PregameAxisSupplyRollAction>(Assert.Single(initialSet.Candidates));
        Assert.Equal(initialSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(initialSet)));
        Assert.Throws<JsonException>(() => CampaignObservationV7LegalActionSerializer.DeserializeCanonical(
            CampaignLegalActionSerializer.Serialize(initialSet)));
        var draft = CampaignExercises.QueryPregameAxisDraft(initial, CampaignActionAudience.Axis);
        Assert.Equal("{}", JsonSerializer.Serialize(draft));
        var visible = Encoding.UTF8.GetString(draft.VisibleBytes);
        Assert.DoesNotContain("randomSeed", visible);
        Assert.DoesNotContain("randomCursor", visible);
        Assert.DoesNotContain("sourceSnapshot", visible);
        Assert.DoesNotContain("actionId", visible);
        Assert.DoesNotContain(initialSet.Candidates[0].ActionId, visible);
        Assert.Contains("\"status\":\"incomplete\"", visible);
        Assert.Contains("\"policyStatus\":\"provisional\"", visible);
        var roll = Accept(initial, initialSet.Candidates[0]);
        using var rolled = JsonDocument.Parse(roll.Evidence!.SnapshotCheckpoint);
        var expectedRandom = SandtableRandom.RollD6(SandtableRandom.Create(0));
        Assert.Equal(6, expectedRandom.Value);
        Assert.Equal(6, rolled.RootElement.GetProperty("die").GetInt32());
        Assert.Equal(3, rolled.RootElement.GetProperty("maximumSupply").GetInt32());
        Assert.Equal(expectedRandom.State.NextByteCursor, rolled.RootElement.GetProperty("randomCursor").GetUInt64());
        Assert.Equal(CampaignPregameSourceCreation.PositionId, CampaignExercises.QueryCheckpoint(roll.SuccessorSession!).PositionId);
        using var retained = JsonDocument.Parse(rolled.RootElement.GetProperty("sourceSnapshotBase64").GetBytesFromBase64());
        var pending = retained.RootElement.GetProperty("pendingArrivalGroupIds").EnumerateArray().Select(value => value.GetString()).ToArray();
        Assert.Contains(Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId, pending);
        Assert.Contains(Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId, pending);

        var available = CampaignExercises.Query(roll.SuccessorSession!, CampaignActionAudience.Axis).ActionSet!;
        using var visibleAfterRoll = JsonDocument.Parse(CampaignExercises.QueryPregameAxisDraft(
            roll.SuccessorSession!, CampaignActionAudience.Axis).VisibleBytes);
        foreach (var choice in visibleAfterRoll.RootElement.GetProperty("choices").EnumerateArray())
        {
            var lane = choice.GetProperty("lane").GetInt32();
            var expectedRoute = lane switch
            {
                2 => (Origin: "West Sicily", Destination: "Tripoli"),
                3 => (Origin: "Italy", Destination: "Benghazi"),
                6 => (Origin: "Italy", Destination: "Tobruk"),
                _ => throw new InvalidOperationException("Unexpected lane."),
            };
            Assert.Equal(expectedRoute.Origin, choice.GetProperty("origin").GetString());
            Assert.Equal(expectedRoute.Destination, choice.GetProperty("destination").GetString());
        }
        Assert.Equal(9, available.Candidates.OfType<PregameAxisSupplyAllocationAction>().Count());
        Assert.Equal(available, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(available)));
        foreach (var lane in new[] { 2, 3, 6 }) foreach (var stage in new[] { 1, 2, 3 })
            Assert.Contains(available.Candidates.OfType<PregameAxisSupplyAllocationAction>(),
                action => action.Lane == lane && action.Stage == stage && action.Delta == 1);

        var current = roll.SuccessorSession!;
        for (var i = 0; i < 3; i++)
        {
            var add = CampaignExercises.Query(current, CampaignActionAudience.Axis).ActionSet!.Candidates
                .OfType<PregameAxisSupplyAllocationAction>().Single(value => value.Lane == 2 && value.Stage == 1 && value.Delta == 1);
            current = Accept(current, add).SuccessorSession!;
        }
        var capped = CampaignExercises.Query(current, CampaignActionAudience.Axis).ActionSet!;
        using var visibleAllocation = JsonDocument.Parse(CampaignExercises.QueryPregameAxisDraft(
            current, CampaignActionAudience.Axis).VisibleBytes);
        var allocationRow = Assert.Single(visibleAllocation.RootElement.GetProperty("allocations").EnumerateArray());
        Assert.Equal(2, allocationRow.GetProperty("lane").GetInt32());
        Assert.Equal("West Sicily", allocationRow.GetProperty("origin").GetString());
        Assert.Equal("Tripoli", allocationRow.GetProperty("destination").GetString());
        Assert.Equal(3, allocationRow.GetProperty("units").GetInt32());
        Assert.DoesNotContain(capped.Candidates, value => value is PregameAxisSupplyAllocationAction { Delta: 1 });
        var remove = Assert.Single(capped.Candidates.OfType<PregameAxisSupplyAllocationAction>());
        Assert.Equal(-1, remove.Delta);
        var removed = Accept(current, remove);
        using var afterRemove = JsonDocument.Parse(removed.Evidence!.SnapshotCheckpoint);
        Assert.Equal(expectedRandom.State.NextByteCursor, afterRemove.RootElement.GetProperty("randomCursor").GetUInt64());
        var other = CampaignExercises.Query(removed.SuccessorSession!, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<PregameAxisSupplyAllocationAction>().Single(value => value.Lane == 6 && value.Stage == 3 && value.Delta == 1);
        var final = Accept(removed.SuccessorSession!, other);
        Assert.Equal(CampaignPregameSourceCreation.PositionId, CampaignExercises.QueryCheckpoint(final.SuccessorSession!).PositionId);
        Assert.DoesNotContain(CampaignExercises.Query(final.SuccessorSession!, CampaignActionAudience.Axis).ActionSet!.Candidates,
            value => value.Kind.Contains("finish", StringComparison.Ordinal));
        var reconstructed = CampaignExercises.Reconstruct(final.SuccessorSession!);
        Assert.True(reconstructed.IsVerified);
        Assert.Equal(final.Evidence!.SnapshotCheckpoint, reconstructed.ReconstructedSnapshotBytes);
        var history = final.SuccessorSession!.PregameHistory;
        var altered = history.Select(value => value.ToArray()).ToArray();
        altered[^1][^2] ^= 1;
        var corrupt = new ExerciseSession(final.SuccessorSession.Pregame!, final.SuccessorSession.CurrentContacts, altered);
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, CampaignExercises.Reconstruct(corrupt).FailureReason);
        var missing = new ExerciseSession(final.SuccessorSession.Pregame!, final.SuccessorSession.CurrentContacts, history.SkipLast(1));
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, CampaignExercises.Reconstruct(missing).FailureReason);
        var badGenesis = history.Select(value => value.ToArray()).ToArray();
        badGenesis[0][^2] ^= 1;
        var forgedGenesis = new ExerciseSession(final.SuccessorSession.Pregame!, final.SuccessorSession.CurrentContacts, badGenesis);
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, CampaignExercises.Reconstruct(forgedGenesis).FailureReason);

        var replay = CampaignExercises.ReplayPregameGenesis(request, start.CreationEventBytes!, start.InitialSnapshotBytes!);
        Assert.Throws<ArgumentNullException>(() => CampaignExercises.ReplayPregameGenesis(
            null!, start.CreationEventBytes!, start.InitialSnapshotBytes!));
        var rerolled = Accept(replay.Session!, Assert.Single(CampaignExercises.Query(replay.Session!, CampaignActionAudience.Axis).ActionSet!.Candidates));
        Assert.Equal(roll.Evidence.EventRecords[0], rerolled.Evidence!.EventRecords[0]);
        Assert.Equal(roll.Evidence.SnapshotCheckpoint, rerolled.Evidence.SnapshotCheckpoint);
        var corruptedSnapshot = start.InitialSnapshotBytes!;
        corruptedSnapshot[^2] ^= 1;
        Assert.ThrowsAny<Exception>(() => CampaignExercises.ReplayPregameGenesis(request,
            start.CreationEventBytes!, corruptedSnapshot));
        var badRequest = new CampaignCreationRequest(request.ContractVersion, request.CampaignId, request.RulesetHash,
            request.Seed, request.SetupId, "sha256:" + new string('0', 64), request.ContentPackId, request.ContentHash, request.ScenarioId);
        Assert.Throws<ArgumentException>(() => CampaignExercises.Begin(badRequest, artifact, init));
        Assert.Throws<JsonException>(() => CampaignExercises.ReadCheckpoint(final.Evidence.SnapshotCheckpoint));
        Assert.Throws<JsonException>(() => CampaignExercises.ValidateCanonicalEvent(roll.Evidence.EventRecords[0]));
    }

    private static ExerciseStepResult Accept(ExerciseSession session, CampaignActionCandidate action)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var command = new CampaignActionSubmission(1, checkpoint.CampaignId, checkpoint.StateVersion,
            checkpoint.PositionId, CampaignActionAudience.Axis, action.ActionId);
        var result = CampaignExercises.Submit(session, command);
        Assert.True(result.IsAccepted);
        return result;
    }
}
