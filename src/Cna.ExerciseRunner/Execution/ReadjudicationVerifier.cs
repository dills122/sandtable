using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Execution;

public static class ReadjudicationVerifier
{
    public static ReadjudicationProof Verify(
        ExerciseManifest manifest,
        ExerciseExecutionResult expected)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(expected);
        if (!expected.IsSucceeded
            || !string.Equals(
                expected.BoundaryPositionId,
                manifest.TerminalBoundary,
                StringComparison.Ordinal))
            return FailedWithoutReplay(expected);

        var start = CampaignExercises.Begin(ExerciseExecutor.CreateRequest(
            manifest,
            expected.SeedLedger.Identity));
        if (!start.IsStarted) return FailedWithoutReplay(expected);
        var session = start.Session!;
        var readjudicatedTranscript = new List<byte[]>();
        var readjudicatedEvents = new List<byte[]>();
        var final = start.InitialSnapshotBytes!;

        for (var index = 0; index < expected.Steps.Count; index++)
        {
            var recorded = expected.Steps[index];
            var query = CampaignExercises.Query(session, recorded.Audience);
            var candidate = query.ActionSet?.Candidates.SingleOrDefault(value => string.Equals(
                value.ActionId,
                recorded.ActionId,
                StringComparison.Ordinal));
            if (candidate is null)
            {
                break;
            }
            var set = query.ActionSet!;
            var submitted = CampaignExercises.Submit(session, new CampaignActionSubmission(
                CampaignActionSubmission.CurrentContractVersion,
                set.CampaignId,
                set.StateVersion,
                set.PositionId,
                set.Audience,
                candidate.ActionId));
            if (!submitted.IsAccepted)
            {
                break;
            }
            var evidence = submitted.Evidence!;
            if (recorded.Ordinal != index || !ReceiptsEqual(evidence.Receipt, recorded.Receipt))
                break;
            readjudicatedTranscript.Add(
                CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt));
            readjudicatedEvents.AddRange(evidence.EventRecords.Select(value => value.ToArray()));
            final = evidence.SnapshotCheckpoint;
            session = submitted.SuccessorSession!;
        }

        var terminalQuery = CampaignExercises.Query(session, CampaignActionAudience.System);
        if (!terminalQuery.IsSuccessful
            || !string.Equals(
                terminalQuery.ActionSet!.PositionId,
                manifest.TerminalBoundary,
                StringComparison.Ordinal))
            final = [];
        return CreateProof(expected, readjudicatedTranscript, readjudicatedEvents, final);
    }

    private static ReadjudicationProof FailedWithoutReplay(ExerciseExecutionResult expected) =>
        CreateProof(expected, [], [], []);

    private static ReadjudicationProof CreateProof(
        ExerciseExecutionResult expected,
        IEnumerable<byte[]> readjudicatedTranscript,
        IEnumerable<byte[]> readjudicatedEvents,
        byte[] readjudicatedFinalSnapshot)
    {
        var expectedTranscript = expected.Steps.Select(step =>
            CampaignActionAcceptanceReceiptSerializer.Serialize(step.Receipt));
        var expectedEvents = expected.Steps.SelectMany(step => step.EventRecords);
        return new ReadjudicationProof(
            ReplayEvidenceHasher.HashRecords(expectedTranscript),
            ReplayEvidenceHasher.HashRecords(readjudicatedTranscript),
            ReplayEvidenceHasher.HashRecords(expectedEvents),
            ReplayEvidenceHasher.HashRecords(readjudicatedEvents),
            ReplayEvidenceHasher.HashBytes(expected.FinalSnapshot),
            ReplayEvidenceHasher.HashBytes(readjudicatedFinalSnapshot));
    }

    private static bool ReceiptsEqual(
        CampaignActionAcceptanceReceipt first,
        CampaignActionAcceptanceReceipt second) =>
        first.ContractVersion == second.ContractVersion
        && string.Equals(first.CampaignId, second.CampaignId, StringComparison.Ordinal)
        && first.PriorStateVersion == second.PriorStateVersion
        && first.CommittedStateVersion == second.CommittedStateVersion
        && string.Equals(
            first.ResultingPositionId,
            second.ResultingPositionId,
            StringComparison.Ordinal)
        && first.Audience == second.Audience
        && string.Equals(first.ActionId, second.ActionId, StringComparison.Ordinal);
}
