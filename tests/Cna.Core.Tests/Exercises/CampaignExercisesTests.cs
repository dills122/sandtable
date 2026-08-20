using System.Reflection;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Exercises;

public sealed class CampaignExercisesTests
{
    [Fact]
    public void BeginMintsOnlyAnOpaqueFreshSessionWithDefensiveCanonicalEvidence()
    {
        var request = CreateRequest();

        var started = CampaignExercises.Begin(request);
        var ordinary = CampaignAuthority.Create(request);

        Assert.True(started.IsStarted);
        Assert.Equal(CampaignCreationRejectionReason.None, started.RejectionReason);
        Assert.NotNull(started.Session);
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(ordinary.Handle!.Snapshot),
            started.InitialSnapshotBytes);
        Assert.Equal(
            CampaignEventSerializer.Serialize(started.Session!.History[0]),
            started.CreationEventBytes);

        var snapshotCopy = started.InitialSnapshotBytes!;
        var eventCopy = started.CreationEventBytes!;
        snapshotCopy[0] ^= 0xff;
        eventCopy[0] ^= 0xff;
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(started.Session.Snapshot),
            started.InitialSnapshotBytes);
        Assert.Equal(
            CampaignEventSerializer.Serialize(started.Session.History[0]),
            started.CreationEventBytes);

        var sessionType = typeof(ExerciseSession);
        Assert.True(sessionType.IsSealed);
        Assert.False(IsRecord(sessionType));
        Assert.Empty(sessionType.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(sessionType.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(sessionType.GetFields(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(nameof(ExerciseSession), started.Session.ToString());
        Assert.Equal("{}", JsonSerializer.Serialize(started.Session));
        Assert.DoesNotContain(sessionType.GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name is "op_Implicit" or "op_Explicit");
        Assert.All(typeof(CampaignExercises).GetMethods(BindingFlags.Public | BindingFlags.Static),
            method =>
            {
                Assert.DoesNotContain(
                    method.GetParameters(),
                    parameter => ContainsType(parameter.ParameterType, typeof(CampaignAuthorityHandle)));
                Assert.False(ContainsType(method.ReturnType, typeof(CampaignAuthorityHandle)));
                Assert.DoesNotContain(
                    method.GetParameters(),
                    parameter => parameter.ParameterType == typeof(byte[]));
            });
    }

    [Fact]
    public void RejectedBeginReturnsNoSessionOrEvidence()
    {
        var valid = CreateRequest();
        var result = CampaignExercises.Begin(new CampaignCreationRequest(
            99,
            valid.CampaignId,
            valid.RulesetHash,
            valid.Seed,
            valid.SetupId,
            valid.SetupHash,
            valid.ContentPackId,
            valid.ContentHash,
            valid.ScenarioId));

        Assert.False(result.IsStarted);
        Assert.Equal(CampaignCreationRejectionReason.InvalidRequest, result.RejectionReason);
        Assert.Null(result.Session);
        Assert.Null(result.CreationEventBytes);
        Assert.Null(result.InitialSnapshotBytes);
    }

    [Fact]
    public void QueryUsesTheExistingLegalActionSemantics()
    {
        var request = CreateRequest();
        var session = Start(request);
        var authority = CampaignAuthority.Create(request).Handle!;

        foreach (var audience in Enum.GetValues<CampaignActionAudience>())
        {
            var exercise = CampaignExercises.Query(session, audience);
            var ordinary = CampaignLegalActions.Query(authority, audience);

            Assert.Equal(exercise.RejectionReason, ordinary.RejectionReason);
            Assert.Equal(
                CampaignLegalActionSerializer.Serialize(ordinary.ActionSet!),
                CampaignLegalActionSerializer.Serialize(exercise.ActionSet!));
        }
    }

    [Fact]
    public void CheckpointQueryExposesOnlyCurrentConcurrencyCoordinates()
    {
        var session = Start(CreateRequest());
        var actionSet = CampaignExercises.Query(
            session,
            CampaignActionAudience.System).ActionSet!;

        var checkpoint = CampaignExercises.QueryCheckpoint(session);

        Assert.Equal(ExerciseCheckpoint.CurrentContractVersion, checkpoint.ContractVersion);
        Assert.Equal(actionSet.CampaignId, checkpoint.CampaignId);
        Assert.Equal(actionSet.StateVersion, checkpoint.StateVersion);
        Assert.Equal(actionSet.RulesetHash, checkpoint.RulesetHash);
        Assert.Equal(actionSet.PositionId, checkpoint.PositionId);
        Assert.Equal(
            new[]
            {
                nameof(ExerciseCheckpoint.CampaignId),
                nameof(ExerciseCheckpoint.ContractVersion),
                nameof(ExerciseCheckpoint.PositionId),
                nameof(ExerciseCheckpoint.RulesetHash),
                nameof(ExerciseCheckpoint.StateVersion),
            },
            typeof(ExerciseCheckpoint).GetProperties()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
        Assert.Empty(typeof(ExerciseCheckpoint).GetConstructors(
            BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void ExerciseAndOrdinaryPathsMatchAtEverySupportedCheckpoint()
    {
        var request = CreateRequest();
        var session = Start(request);
        var authority = CampaignAuthority.Create(request).Handle!;

        while (session.Snapshot.SequencePosition.PositionId
            != "land.position.operation-1.organization")
        {
            var active = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience))
                .Single(result => result.ActionSet!.Candidates.Count > 0)
                .ActionSet!;
            var candidate = active.Candidates[0];
            var submission = new CampaignActionSubmission(
                CampaignActionSubmission.CurrentContractVersion,
                active.CampaignId,
                active.StateVersion,
                active.PositionId,
                active.Audience,
                candidate.ActionId);
            var ordinaryExecution = CampaignActionExecution.Execute(
                authority.Snapshot,
                authority.Context,
                submission);

            var exercise = CampaignExercises.Submit(session, submission);
            var ordinary = CampaignLegalActions.Submit(authority, submission);

            Assert.True(exercise.IsAccepted);
            Assert.True(ordinary.IsAccepted);
            Assert.Equal(ordinary.Receipt, exercise.Evidence!.Receipt);
            Assert.Equal(
                CampaignEventSerializer.Serialize(ordinaryExecution.AcceptedEvent!),
                Assert.Single(exercise.Evidence.EventRecords));
            Assert.Equal(
                CampaignSnapshotSerializer.Serialize(ordinary.SuccessorHandle!.Snapshot),
                exercise.Evidence.SnapshotCheckpoint);
            Assert.Equal(
                CampaignSnapshotSerializer.Serialize(ordinary.SuccessorHandle.Snapshot),
                CampaignSnapshotSerializer.Serialize(exercise.SuccessorSession!.Snapshot));

            var eventCopy = Assert.Single(exercise.Evidence.EventRecords);
            var checkpointCopy = exercise.Evidence.SnapshotCheckpoint;
            eventCopy[0] ^= 0xff;
            checkpointCopy[0] ^= 0xff;
            Assert.Equal(
                CampaignEventSerializer.Serialize(ordinaryExecution.AcceptedEvent!),
                Assert.Single(exercise.Evidence.EventRecords));
            Assert.Equal(
                CampaignSnapshotSerializer.Serialize(ordinary.SuccessorHandle.Snapshot),
                exercise.Evidence.SnapshotCheckpoint);

            session = exercise.SuccessorSession;
            authority = ordinary.SuccessorHandle;
        }
    }

    [Fact]
    public void RejectedSubmitDoesNotChangeTheInputSessionOrReturnEvidence()
    {
        var session = Start(CreateRequest());
        var beforeQuery = CampaignExercises.Query(session, CampaignActionAudience.System);
        var beforeBytes = CampaignLegalActionSerializer.Serialize(beforeQuery.ActionSet!);
        var candidate = Assert.Single(beforeQuery.ActionSet!.Candidates);
        var invalid = new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion,
            beforeQuery.ActionSet.CampaignId,
            beforeQuery.ActionSet.StateVersion,
            beforeQuery.ActionSet.PositionId,
            CampaignActionAudience.Axis,
            candidate.ActionId);

        var result = CampaignExercises.Submit(session, invalid);

        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, result.RejectionReason);
        Assert.Null(result.SuccessorSession);
        Assert.Null(result.Evidence);
        Assert.Equal(beforeBytes, CampaignLegalActionSerializer.Serialize(
            CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!));
        Assert.Single(session.History);
    }

    [Fact]
    public void ReconstructionUsesOnlyRetainedHistoryAndMatchesTheFinalSnapshotExactly()
    {
        var completed = CompleteExercise();

        var result = CampaignExercises.Reconstruct(completed);

        Assert.True(result.IsVerified);
        Assert.Equal(ExerciseReconstructionFailureReason.None, result.FailureReason);
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(completed.Snapshot),
            result.ReconstructedSnapshotBytes);
        Assert.Equal(result.ExpectedSnapshotHash, result.ReconstructedSnapshotHash);
        Assert.StartsWith("sha256:", result.EventStreamHash, StringComparison.Ordinal);
    }

    [Fact]
    public void ReconstructionFailsForRemovedReorderedOrChangedInternalHistory()
    {
        var completed = CompleteExercise();
        var history = completed.History.ToArray();
        var removed = new ExerciseSession(completed.Snapshot, completed.Context, history[1..]);
        var reordered = new ExerciseSession(
            completed.Snapshot,
            completed.Context,
            history.Reverse());
        var changedHistory = history.ToArray();
        var created = Assert.IsType<CampaignCreated>(changedHistory[0]);
        changedHistory[0] = created with
        {
            RandomState = new RandomStreamState(
                created.RandomState.ContractVersion,
                created.RandomState.AlgorithmId,
                created.RandomState.Seed + 1,
                created.RandomState.NextByteCursor),
        };
        var changed = new ExerciseSession(
            completed.Snapshot,
            completed.Context,
            changedHistory);

        Assert.False(CampaignExercises.Reconstruct(removed).IsVerified);
        Assert.False(CampaignExercises.Reconstruct(reordered).IsVerified);
        Assert.False(CampaignExercises.Reconstruct(changed).IsVerified);
    }

    private static ExerciseSession CompleteExercise()
    {
        var session = Start(CreateRequest());
        while (session.Snapshot.SequencePosition.PositionId
            != "land.position.operation-1.organization")
        {
            var active = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience))
                .Single(result => result.ActionSet!.Candidates.Count > 0)
                .ActionSet!;
            var candidate = active.Candidates[0];
            var result = CampaignExercises.Submit(session, new CampaignActionSubmission(
                CampaignActionSubmission.CurrentContractVersion,
                active.CampaignId,
                active.StateVersion,
                active.PositionId,
                active.Audience,
                candidate.ActionId));
            Assert.True(result.IsAccepted);
            session = result.SuccessorSession!;
        }
        return session;
    }

    private static ExerciseSession Start(CampaignCreationRequest request)
    {
        var result = CampaignExercises.Begin(request);
        Assert.True(result.IsStarted);
        return result.Session!;
    }

    private static CampaignCreationRequest CreateRequest()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        return new CampaignCreationRequest(
            CampaignCreationRequest.CurrentContractVersion,
            "campaign-exercise",
            Cna1979Ruleset.Manifest.Hash,
            12345,
            setup.SetupId,
            setup.Hash,
            setup.Content.Pack.PackId,
            setup.Content.Pack.Hash,
            setup.Content.ScenarioId);
    }

    private static bool ContainsType(Type candidate, Type forbidden)
    {
        if (candidate == forbidden) return true;
        if (candidate.IsArray) return ContainsType(candidate.GetElementType()!, forbidden);
        return candidate.IsGenericType
            && candidate.GetGenericArguments().Any(argument => ContainsType(argument, forbidden));
    }

    private static bool IsRecord(Type type) => type.GetMethod(
        "<Clone>$",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null;
}
