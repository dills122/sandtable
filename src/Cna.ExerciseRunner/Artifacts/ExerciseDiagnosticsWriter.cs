using System.Text.Json;
using Cna.Core.Actions;
using Cna.ExerciseRunner.Controllers;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Artifacts;

public static class ExerciseDiagnosticsWriter
{
    public static byte[] Write(
        ExerciseManifest manifest,
        ExerciseExecutionResult execution,
        ExerciseRunResult runResult) =>
        Write(manifest, execution, runResult, execution.CheckResults, null);

    public static byte[] Write(
        ExerciseManifest manifest,
        ExerciseExecutionResult execution,
        ExerciseRunResult runResult,
        ExerciseCheckResults checks,
        ReadjudicationProof? readjudication) =>
        Write(manifest, execution, runResult, checks, readjudication, null);

    internal static byte[] Write(
        ExerciseManifest manifest,
        ExerciseExecutionResult execution,
        ExerciseRunResult runResult,
        ExerciseCheckResults checks,
        ReadjudicationProof? readjudication,
        ExerciseDiagnosticTelemetry? telemetry)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(runResult);
        ArgumentNullException.ThrowIfNull(checks);
        using var stream = new MemoryStream();

        if (manifest.Detail == ExerciseDetail.Compact)
        {
            foreach (var step in execution.Steps) WriteCompactStep(stream, manifest, step);
            WriteCompletion(stream, manifest, execution, runResult, includeCorrelation: false);
            return stream.ToArray();
        }

        foreach (var step in execution.Steps)
        {
            foreach (var query in step.QueryDiagnostics)
                WriteQuery(
                    stream,
                    manifest,
                    query,
                    writer => WriteStepCorrelation(writer, step, resulting: false));

            WriteRecord(stream, writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("event", "exercise.controller-selected");
                WriteRunCorrelation(writer, manifest);
                WriteStepCorrelation(writer, step, resulting: false);
                writer.WriteNumber("activeAudienceCount", step.ActiveAudienceCount);
                writer.WriteString("audience", FormatAudience(step.Audience));
                writer.WriteString("actionId", step.ActionId);
                writer.WriteEndObject();
            });
            WriteForensicStep(stream, manifest, step);
        }
        foreach (var decision in execution.FailedDecisions)
            WriteFailedDecision(stream, manifest, decision);

        foreach (var check in checks.Results) WriteCheck(stream, manifest, execution, check);
        if (execution.Reconstruction is not null)
            WriteReconstruction(stream, manifest, execution.Reconstruction);
        if (readjudication is not null)
            WriteReadjudication(stream, manifest, readjudication);
        if (telemetry?.PayloadCountBeforeDiagnostics is not null)
            WriteArtifactPrepared(stream, manifest, telemetry);
        if (manifest.Detail == ExerciseDetail.Debug)
            WriteTimings(stream, manifest, execution, telemetry);
        WriteCompletion(stream, manifest, execution, runResult, includeCorrelation: true);
        return stream.ToArray();
    }

    private static void WriteCompactStep(
        MemoryStream stream,
        ExerciseManifest manifest,
        ExerciseAcceptedStep step) =>
        WriteRecord(stream, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("event", "exercise.step-accepted");
            writer.WriteString("exerciseId", manifest.ExerciseId);
            writer.WriteNumber("stepOrdinal", step.Ordinal);
            writer.WriteString("campaignId", step.Receipt.CampaignId);
            writer.WriteNumber("stateVersion", step.Receipt.CommittedStateVersion);
            writer.WriteString("positionId", step.Receipt.ResultingPositionId);
            writer.WriteString("audience", FormatAudience(step.Audience));
            writer.WriteString("actionId", step.ActionId);
            writer.WriteEndObject();
        });

    private static void WriteForensicStep(
        MemoryStream stream,
        ExerciseManifest manifest,
        ExerciseAcceptedStep step)
    {
        var eventStreamHash = ReplayEvidenceHasher.HashRecords(step.EventRecords);
        var snapshot = step.SnapshotCheckpoint;
        var snapshotHash = ReplayEvidenceHasher.HashBytes(snapshot);
        WriteRecord(stream, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("event", "exercise.step-accepted");
            WriteRunCorrelation(writer, manifest);
            WriteStepCorrelation(writer, step, resulting: true);
            writer.WriteString("audience", FormatAudience(step.Audience));
            writer.WriteString("actionId", step.ActionId);
            writer.WriteNumber("eventCount", step.EventRecords.Count);
            writer.WriteString("eventStreamHash", eventStreamHash);
            writer.WriteNumber("snapshotBytes", snapshot.LongLength);
            writer.WriteString("snapshotHash", snapshotHash);
            writer.WriteEndObject();
        });
    }

    private static void WriteCheck(
        MemoryStream stream,
        ExerciseManifest manifest,
        ExerciseExecutionResult execution,
        ExerciseCheckResult check) =>
        WriteRecord(stream, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("event", "exercise.check-evaluated");
            WriteRunCorrelation(writer, manifest);
            if (check.StepOrdinal is { } ordinal && ordinal < execution.Steps.Count)
                WriteStepCorrelation(writer, execution.Steps[ordinal], resulting: false);
            else if (check.StepOrdinal is { } failedOrdinal
                && execution.FailedDecisions.SingleOrDefault(
                    decision => decision.Ordinal == failedOrdinal) is { } decision)
                WriteDecisionCorrelation(writer, decision);
            else
            {
                writer.WriteNull("stepOrdinal");
                writer.WriteNull("campaignId");
                writer.WriteNull("stateVersion");
                writer.WriteNull("positionId");
            }
            if (check.Audience.HasValue)
                writer.WriteString("audience", FormatAudience(check.Audience.Value));
            else writer.WriteNull("audience");
            writer.WriteString("check", FormatCheckId(check.CheckId));
            writer.WriteString("status", check.IsPassed ? "passed" : "failed");
            if (check.IsPassed) writer.WriteNull("failureCode");
            else writer.WriteString("failureCode", FormatFailureCode(check.FailureCode));
            writer.WriteEndObject();
        });

    private static void WriteFailedDecision(
        MemoryStream stream,
        ExerciseManifest manifest,
        ExerciseDecisionDiagnostic decision)
    {
        foreach (var query in decision.Queries)
            WriteQuery(
                stream,
                manifest,
                query,
                writer => WriteDecisionCorrelation(writer, decision));
        if (decision.SelectionFailure.HasValue)
        {
            WriteRecord(stream, writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("event", "exercise.controller-selection-failed");
                WriteRunCorrelation(writer, manifest);
                WriteDecisionCorrelation(writer, decision);
                writer.WriteNumber("activeAudienceCount", decision.ActiveAudienceCount);
                writer.WriteString(
                    "failureReason",
                    FormatSelectionFailure(decision.SelectionFailure.Value));
                writer.WriteEndObject();
            });
        }
        else if (decision.SelectedAudience.HasValue && decision.SelectedActionId is not null)
        {
            WriteRecord(stream, writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("event", "exercise.controller-selected");
                WriteRunCorrelation(writer, manifest);
                WriteDecisionCorrelation(writer, decision);
                writer.WriteNumber("activeAudienceCount", decision.ActiveAudienceCount);
                writer.WriteString("audience", FormatAudience(decision.SelectedAudience.Value));
                writer.WriteString("actionId", decision.SelectedActionId);
                writer.WriteEndObject();
            });
        }
        if (decision.SubmissionAttempted)
        {
            WriteRecord(stream, writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("event", "exercise.action-submission-evaluated");
                WriteRunCorrelation(writer, manifest);
                WriteDecisionCorrelation(writer, decision);
                if (decision.SelectedAudience.HasValue)
                    writer.WriteString("audience", FormatAudience(decision.SelectedAudience.Value));
                else writer.WriteNull("audience");
                if (decision.SelectedActionId is null) writer.WriteNull("actionId");
                else writer.WriteString("actionId", decision.SelectedActionId);
                writer.WriteString(
                    "status",
                    decision.SubmissionAccepted == true ? "accepted" : "rejected");
                if (decision.SubmissionRejectionReason.HasValue)
                    writer.WriteString(
                        "rejectionReason",
                        FormatSubmissionRejection(decision.SubmissionRejectionReason.Value));
                else writer.WriteNull("rejectionReason");
                if (decision.SubmittedEventCount.HasValue)
                    writer.WriteNumber("eventCount", decision.SubmittedEventCount.Value);
                else writer.WriteNull("eventCount");
                writer.WriteEndObject();
            });
        }
        WriteRecord(stream, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("event", "exercise.decision-failed");
            WriteRunCorrelation(writer, manifest);
            WriteDecisionCorrelation(writer, decision);
            writer.WriteString("failureStage", FormatFailureStage(decision.FailureStage));
            writer.WriteString("failureCode", FormatFailureCode(decision.FailureCode));
            if (decision.SelectedAudience.HasValue)
                writer.WriteString("audience", FormatAudience(decision.SelectedAudience.Value));
            else writer.WriteNull("audience");
            if (decision.SelectedActionId is null) writer.WriteNull("actionId");
            else writer.WriteString("actionId", decision.SelectedActionId);
            writer.WriteEndObject();
        });
    }

    private static void WriteQuery(
        MemoryStream stream,
        ExerciseManifest manifest,
        ExerciseQueryDiagnostic query,
        Action<Utf8JsonWriter> writeCorrelation) =>
        WriteRecord(stream, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("event", "exercise.query-evaluated");
            WriteRunCorrelation(writer, manifest);
            writeCorrelation(writer);
            writer.WriteString("audience", FormatAudience(query.Audience));
            writer.WriteNumber("candidateCount", query.CandidateCount);
            writer.WriteBoolean("active", query.CandidateCount > 0);
            writer.WriteString(
                "status",
                query.FailureCode == ExerciseCheckFailureCode.None ? "passed" : "failed");
            if (query.FailureCode == ExerciseCheckFailureCode.None)
                writer.WriteNull("failureCode");
            else writer.WriteString("failureCode", FormatFailureCode(query.FailureCode));
            writer.WriteEndObject();
        });

    private static void WriteReconstruction(
        MemoryStream stream,
        ExerciseManifest manifest,
        ReconstructionProof proof) =>
        WriteRecord(stream, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("event", "exercise.reconstruction-verified");
            WriteRunCorrelation(writer, manifest);
            writer.WriteBoolean("verified", proof.IsVerified);
            writer.WriteBoolean("historyAccepted", proof.HistoryAccepted);
            writer.WriteBoolean("finalSnapshotMatches", proof.FinalSnapshotMatches);
            writer.WriteString("eventStreamHash", proof.EventStreamHash);
            writer.WriteString("expectedSnapshotHash", proof.ExpectedSnapshotHash);
            if (proof.ReconstructedSnapshotHash is null)
                writer.WriteNull("reconstructedSnapshotHash");
            else writer.WriteString(
                "reconstructedSnapshotHash",
                proof.ReconstructedSnapshotHash);
            writer.WriteEndObject();
        });

    private static void WriteReadjudication(
        MemoryStream stream,
        ExerciseManifest manifest,
        ReadjudicationProof proof) =>
        WriteRecord(stream, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("event", "exercise.readjudication-verified");
            WriteRunCorrelation(writer, manifest);
            writer.WriteBoolean("verified", proof.IsVerified);
            writer.WriteBoolean("transcriptMatches", proof.TranscriptMatches);
            writer.WriteBoolean("eventsMatch", proof.EventsMatch);
            writer.WriteBoolean("finalSnapshotMatches", proof.FinalSnapshotMatches);
            writer.WriteString("expectedTranscriptHash", proof.ExpectedTranscriptHash);
            writer.WriteString("readjudicatedTranscriptHash", proof.ReadjudicatedTranscriptHash);
            writer.WriteString("expectedEventsHash", proof.ExpectedEventsHash);
            writer.WriteString("readjudicatedEventsHash", proof.ReadjudicatedEventsHash);
            writer.WriteString("expectedFinalSnapshotHash", proof.ExpectedFinalSnapshotHash);
            writer.WriteString(
                "readjudicatedFinalSnapshotHash",
                proof.ReadjudicatedFinalSnapshotHash);
            writer.WriteEndObject();
        });

    private static void WriteTimings(
        MemoryStream stream,
        ExerciseManifest manifest,
        ExerciseExecutionResult execution,
        ExerciseDiagnosticTelemetry? telemetry)
    {
        if (execution.BeginElapsedMicroseconds.HasValue)
            WriteTiming(
                stream,
                manifest,
                "core-begin",
                null,
                null,
                execution.BeginElapsedMicroseconds.Value);
        foreach (var step in execution.Steps)
        {
            foreach (var query in step.QueryDiagnostics)
                WriteTiming(
                    stream,
                    manifest,
                    "authority-query",
                    step.Ordinal,
                    query.Audience,
                    query.ElapsedMicroseconds);
            WriteTiming(
                stream,
                manifest,
                "controller-selection",
                step.Ordinal,
                step.Audience,
                step.ControllerElapsedMicroseconds);
            WriteTiming(
                stream,
                manifest,
                "action-submission",
                step.Ordinal,
                step.Audience,
                step.SubmissionElapsedMicroseconds);
        }
        foreach (var decision in execution.FailedDecisions)
        {
            foreach (var query in decision.Queries)
                WriteTiming(
                    stream,
                    manifest,
                    "authority-query",
                    decision.Ordinal,
                    query.Audience,
                    query.ElapsedMicroseconds);
            if (decision.ControllerElapsedMicroseconds.HasValue)
                WriteTiming(
                    stream,
                    manifest,
                    "controller-selection",
                    decision.Ordinal,
                    decision.SelectedAudience,
                    decision.ControllerElapsedMicroseconds.Value);
            if (decision.SubmissionElapsedMicroseconds.HasValue)
                WriteTiming(
                    stream,
                    manifest,
                    "action-submission",
                    decision.Ordinal,
                    decision.SelectedAudience,
                    decision.SubmissionElapsedMicroseconds.Value);
        }
        if (execution.ReconstructionElapsedMicroseconds.HasValue)
            WriteTiming(
                stream,
                manifest,
                "history-reconstruction",
                null,
                null,
                execution.ReconstructionElapsedMicroseconds.Value);
        if (telemetry is not null)
        {
            foreach (var phase in telemetry.Phases)
                WriteTiming(
                    stream,
                    manifest,
                    phase.Operation,
                    null,
                    null,
                    phase.ElapsedMicroseconds);
        }
    }

    private static void WriteArtifactPrepared(
        MemoryStream stream,
        ExerciseManifest manifest,
        ExerciseDiagnosticTelemetry telemetry) =>
        WriteRecord(stream, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("event", "exercise.artifact-prepared");
            WriteRunCorrelation(writer, manifest);
            writer.WriteNumber(
                "payloadCountBeforeDiagnostics",
                telemetry.PayloadCountBeforeDiagnostics!.Value);
            writer.WriteNumber(
                "logicalBytesBeforeDiagnostics",
                telemetry.LogicalBytesBeforeDiagnostics!.Value);
            writer.WriteEndObject();
        });

    private static void WriteTiming(
        MemoryStream stream,
        ExerciseManifest manifest,
        string operation,
        int? stepOrdinal,
        CampaignActionAudience? audience,
        long elapsedMicroseconds) =>
        WriteRecord(stream, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("event", "exercise.operation-timing");
            WriteRunCorrelation(writer, manifest);
            writer.WriteString("operation", operation);
            if (stepOrdinal.HasValue) writer.WriteNumber("stepOrdinal", stepOrdinal.Value);
            else writer.WriteNull("stepOrdinal");
            if (audience.HasValue)
                writer.WriteString("audience", FormatAudience(audience.Value));
            else writer.WriteNull("audience");
            writer.WriteNumber("elapsedMicroseconds", elapsedMicroseconds);
            writer.WriteEndObject();
        });

    private static void WriteCompletion(
        MemoryStream stream,
        ExerciseManifest manifest,
        ExerciseExecutionResult execution,
        ExerciseRunResult runResult,
        bool includeCorrelation) =>
        WriteRecord(stream, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("event", "exercise.completed");
            if (includeCorrelation) WriteRunCorrelation(writer, manifest);
            else writer.WriteString("exerciseId", manifest.ExerciseId);
            writer.WriteString(
                "status",
                runResult.Completion is ExerciseSucceeded ? "succeeded" : "failed");
            writer.WriteNumber("stepsAccepted", execution.Steps.Count);
            if (includeCorrelation)
            {
                if ((runResult.Completion as ExerciseSucceeded)?.Outcome is BoundaryReached boundary)
                    writer.WriteString("boundaryPositionId", boundary.PositionId);
                else writer.WriteNull("boundaryPositionId");
                if ((runResult.Completion as ExerciseFailed)?.Failure is { } failure)
                    writer.WriteString(
                        "failureCategory",
                        ExerciseContractText.FormatFailure(failure.Category));
                else writer.WriteNull("failureCategory");
            }
            writer.WriteEndObject();
        });

    private static void WriteRunCorrelation(Utf8JsonWriter writer, ExerciseManifest manifest)
    {
        writer.WriteNull("maneuverId");
        writer.WriteString("exerciseId", manifest.ExerciseId);
        writer.WriteString("variant", "standalone");
    }

    private static void WriteStepCorrelation(
        Utf8JsonWriter writer,
        ExerciseAcceptedStep step,
        bool resulting)
    {
        writer.WriteNumber("stepOrdinal", step.Ordinal);
        writer.WriteString("campaignId", step.Receipt.CampaignId);
        writer.WriteNumber(
            "stateVersion",
            resulting
                ? step.Receipt.CommittedStateVersion
                : step.Receipt.PriorStateVersion);
        writer.WriteString(
            "positionId",
            resulting ? step.Receipt.ResultingPositionId : step.PriorPositionId);
    }

    private static void WriteDecisionCorrelation(
        Utf8JsonWriter writer,
        ExerciseDecisionDiagnostic decision)
    {
        writer.WriteNumber("stepOrdinal", decision.Ordinal);
        writer.WriteString("campaignId", decision.CampaignId);
        writer.WriteNumber("stateVersion", decision.StateVersion);
        writer.WriteString("positionId", decision.PositionId);
    }

    private static string FormatAudience(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.System => "system",
        CampaignActionAudience.Axis => "axis",
        CampaignActionAudience.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(audience)),
    };

    private static string FormatCheckId(ExerciseCheckId check) => check switch
    {
        ExerciseCheckId.AuthorityQueryValid => "authority-query-valid",
        ExerciseCheckId.ActiveAudienceCardinality => "active-audience-cardinality",
        ExerciseCheckId.SelectedActionMembership => "selected-action-membership",
        ExerciseCheckId.AcceptedEventCardinality => "accepted-event-cardinality",
        ExerciseCheckId.CheckpointContinuity => "checkpoint-continuity",
        ExerciseCheckId.TerminalBoundary => "terminal-boundary",
        ExerciseCheckId.HistoryReconstruction => "history-reconstruction",
        ExerciseCheckId.Readjudication => "readjudication",
        _ => throw new ArgumentOutOfRangeException(nameof(check)),
    };

    private static string FormatFailureCode(ExerciseCheckFailureCode failure) => failure switch
    {
        ExerciseCheckFailureCode.AuthorityQueryRejected => "authority-query-rejected",
        ExerciseCheckFailureCode.AuthorityQueryCoordinateMismatch =>
            "authority-query-coordinate-mismatch",
        ExerciseCheckFailureCode.NoActiveAudience => "no-active-audience",
        ExerciseCheckFailureCode.MultipleActiveAudiences => "multiple-active-audiences",
        ExerciseCheckFailureCode.SelectedActionNotCurrent => "selected-action-not-current",
        ExerciseCheckFailureCode.ActionRejected => "action-rejected",
        ExerciseCheckFailureCode.EventCardinalityMismatch => "event-cardinality-mismatch",
        ExerciseCheckFailureCode.CampaignMismatch => "campaign-mismatch",
        ExerciseCheckFailureCode.RulesetMismatch => "ruleset-mismatch",
        ExerciseCheckFailureCode.StateVersionDiscontinuity => "state-version-discontinuity",
        ExerciseCheckFailureCode.PositionMismatch => "position-mismatch",
        ExerciseCheckFailureCode.TerminalBoundaryNotReached => "terminal-boundary-not-reached",
        ExerciseCheckFailureCode.ReconstructionMismatch => "reconstruction-mismatch",
        ExerciseCheckFailureCode.ReadjudicationMismatch => "readjudication-mismatch",
        _ => throw new ArgumentOutOfRangeException(nameof(failure)),
    };

    private static string FormatSelectionFailure(ExerciseControllerSelectionFailure failure) =>
        failure switch
        {
            ExerciseControllerSelectionFailure.NoActiveAudience => "no-active-audience",
            ExerciseControllerSelectionFailure.MultipleActiveAudiences =>
                "multiple-active-audiences",
            ExerciseControllerSelectionFailure.PolicyFailed => "policy-failed",
            _ => throw new ArgumentOutOfRangeException(nameof(failure)),
        };

    private static string FormatFailureStage(ExerciseDecisionFailureStage stage) => stage switch
    {
        ExerciseDecisionFailureStage.AuthorityQuery => "authority-query",
        ExerciseDecisionFailureStage.ControllerSelection => "controller-selection",
        ExerciseDecisionFailureStage.SelectedActionMembership => "selected-action-membership",
        ExerciseDecisionFailureStage.ActionSubmission => "action-submission",
        ExerciseDecisionFailureStage.EventCardinality => "event-cardinality",
        ExerciseDecisionFailureStage.CheckpointContinuity => "checkpoint-continuity",
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };

    private static string FormatSubmissionRejection(
        CampaignActionSubmissionRejectionReason rejection) => rejection switch
        {
            CampaignActionSubmissionRejectionReason.InvalidSubmission => "invalid-submission",
            CampaignActionSubmissionRejectionReason.InvalidAuthority => "invalid-authority",
            CampaignActionSubmissionRejectionReason.CampaignMismatch => "campaign-mismatch",
            CampaignActionSubmissionRejectionReason.StaleState => "stale-state",
            CampaignActionSubmissionRejectionReason.UnexpectedPosition => "unexpected-position",
            CampaignActionSubmissionRejectionReason.ActionNotLegal => "action-not-legal",
            _ => throw new ArgumentOutOfRangeException(nameof(rejection)),
        };

    private static void WriteRecord(MemoryStream stream, Action<Utf8JsonWriter> write)
    {
        using var record = new MemoryStream();
        using (var writer = new Utf8JsonWriter(record)) write(writer);
        stream.Write(record.ToArray());
        stream.WriteByte((byte)'\n');
    }
}
