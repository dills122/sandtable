using System.Security.Cryptography;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Artifacts;

internal static class ExerciseBundleSemanticValidator
{
    internal static void Validate(
        ArtifactBundleProfile profile,
        byte[]? normalizedManifestBytes,
        ExerciseManifest? exerciseManifest,
        BuildIdentity? buildIdentity,
        ExerciseSeedLedger? seedLedger,
        ExerciseRunResult runResult,
        ExerciseCheckResults checkResults,
        IReadOnlyList<ExerciseAcceptedActionRecord> acceptedActions,
        IReadOnlyList<ExerciseCanonicalEventRecord> canonicalEvents,
        IReadOnlyList<ExerciseStepEvidenceRecord> stepEvidence,
        byte[]? initialSnapshot,
        byte[]? finalSnapshot,
        ReconstructionProof? reconstructionProof,
        ReadjudicationProof? readjudicationProof)
    {
        if (profile == ArtifactBundleProfile.FailedPreAdmission)
        {
            ValidateEarlyFailureEvidence(
                runResult,
                checkResults,
                seedLedger,
                acceptedActions,
                canonicalEvents,
                stepEvidence,
                initialSnapshot,
                finalSnapshot,
                reconstructionProof,
                readjudicationProof);
            return;
        }
        if (normalizedManifestBytes is null || exerciseManifest is null)
            throw new InvalidDataException("An admitted bundle is missing its manifest evidence.");

        ValidateAdmittedRunContract(exerciseManifest, runResult, acceptedActions.Count);
        if (profile == ArtifactBundleProfile.FailedAdmitted)
        {
            ValidateEarlyFailureEvidence(
                runResult,
                checkResults,
                seedLedger,
                acceptedActions,
                canonicalEvents,
                stepEvidence,
                initialSnapshot,
                finalSnapshot,
                reconstructionProof,
                readjudicationProof);
            return;
        }
        if (buildIdentity is null)
            throw new InvalidDataException("An identified bundle is missing build evidence.");

        ValidateBuildIdentity(normalizedManifestBytes, exerciseManifest, buildIdentity);
        if (profile == ArtifactBundleProfile.FailedIdentified)
        {
            ValidateEarlyFailureEvidence(
                runResult,
                checkResults,
                seedLedger,
                acceptedActions,
                canonicalEvents,
                stepEvidence,
                initialSnapshot,
                finalSnapshot,
                reconstructionProof,
                readjudicationProof);
            return;
        }
        if (seedLedger is null
            || initialSnapshot is null
            || finalSnapshot is null)
            throw new InvalidDataException("An executed bundle is missing semantic evidence.");

        ValidateSeedIdentity(exerciseManifest, seedLedger);
        var initial = ExerciseEvidenceCodec.DeserializeSnapshot(initialSnapshot);
        var final = ExerciseEvidenceCodec.DeserializeSnapshot(finalSnapshot);
        ValidateEvidence(
            exerciseManifest,
            acceptedActions,
            canonicalEvents,
            stepEvidence,
            initial,
            final,
            finalSnapshot);
        var authoritative = ObserveAuthoritativeReplay(
            exerciseManifest,
            seedLedger.Identity,
            acceptedActions,
            canonicalEvents,
            initialSnapshot,
            finalSnapshot,
            profile);
        ValidateProofObservations(
            profile,
            reconstructionProof,
            readjudicationProof,
            authoritative);
        ValidateProfileMatrix(
            profile,
            exerciseManifest,
            runResult,
            checkResults,
            acceptedActions,
            initial,
            final,
            initialSnapshot,
            finalSnapshot,
            reconstructionProof,
            readjudicationProof);
    }

    private static void ValidateEarlyFailureEvidence(
        ExerciseRunResult runResult,
        ExerciseCheckResults checkResults,
        ExerciseSeedLedger? seedLedger,
        IReadOnlyList<ExerciseAcceptedActionRecord> acceptedActions,
        IReadOnlyList<ExerciseCanonicalEventRecord> canonicalEvents,
        IReadOnlyList<ExerciseStepEvidenceRecord> stepEvidence,
        byte[]? initialSnapshot,
        byte[]? finalSnapshot,
        ReconstructionProof? reconstructionProof,
        ReadjudicationProof? readjudicationProof)
    {
        if (runResult.Completion is not ExerciseFailed
            || checkResults.Results.Count != 0
            || acceptedActions.Count != 0
            || canonicalEvents.Count != 0
            || stepEvidence.Count != 0
            || seedLedger is not null
            || initialSnapshot is not null
            || finalSnapshot is not null
            || reconstructionProof is not null
            || readjudicationProof is not null)
            throw new InvalidDataException(
                "An early failure bundle cannot claim an execution footprint.");
    }

    private static void ValidateAdmittedRunContract(
        ExerciseManifest manifest,
        ExerciseRunResult result,
        int acceptedCount)
    {
        if (acceptedCount > manifest.MaximumSteps)
            throw new InvalidDataException(
                "Accepted Exercise evidence exceeds the admitted maximum step count.");

        var expectedFailure = manifest.AssertFailureCategory;
        var assertion = result.FailureAssertion;
        if (expectedFailure is null)
        {
            if (assertion is not null)
                throw new InvalidDataException(
                    "The run result has a failure assertion not admitted by the manifest.");
            return;
        }
        if (result.Completion is not ExerciseFailed
            || assertion is null
            || assertion.ExpectedCategory != expectedFailure.Value)
            throw new InvalidDataException(
                "The run result does not carry the failure assertion admitted by the manifest.");
    }

    private static AuthoritativeReplayObservations ObserveAuthoritativeReplay(
        ExerciseManifest manifest,
        ExerciseRunIdentity identity,
        IReadOnlyList<ExerciseAcceptedActionRecord> actions,
        IReadOnlyList<ExerciseCanonicalEventRecord> events,
        byte[] initialSnapshot,
        byte[] finalSnapshot,
        ArtifactBundleProfile profile)
    {
        var expectedCampaignId = ExerciseCampaignId.Derive(identity);
        var start = CampaignExercises.Begin(ExerciseExecutor.CreateRequest(manifest, identity));
        if (!start.IsStarted
            || start.InitialSnapshotBytes is null
            || !start.InitialSnapshotBytes.AsSpan().SequenceEqual(initialSnapshot))
            throw new InvalidDataException(
                "The initial snapshot is not canonical evidence for the admitted Exercise identity.");
        if (actions.Any(value => !string.Equals(
                value.CampaignId,
                expectedCampaignId,
                StringComparison.Ordinal))
            || events.Any(value => !string.Equals(
                value.CampaignId,
                expectedCampaignId,
                StringComparison.Ordinal)))
            throw new InvalidDataException(
                "Exercise evidence does not belong to the campaign derived from its seed ledger.");

        var reconstruction = ObserveReconstruction(
            start,
            events,
            finalSnapshot);
        var readjudication = ObserveReadjudication(
            manifest,
            identity,
            actions,
            events,
            finalSnapshot);
        if (profile == ArtifactBundleProfile.FailedExecuted && !reconstruction.IsVerified)
            throw new InvalidDataException(
                "Failed execution evidence does not reconstruct its accepted prefix.");
        if (profile is ArtifactBundleProfile.FailedExecuted
                or ArtifactBundleProfile.FailedReconstructed
            && (!readjudication.TranscriptMatches || !readjudication.EventsMatch))
            throw new InvalidDataException(
                "Pre-replay-failure evidence does not authenticate its accepted prefix.");
        return new AuthoritativeReplayObservations(reconstruction, readjudication);
    }

    private static ReconstructionProof ObserveReconstruction(
        ExerciseStartResult start,
        IReadOnlyList<ExerciseCanonicalEventRecord> events,
        byte[] finalSnapshot)
    {
        var session = start.Session!;
        foreach (var campaignEvent in events)
        {
            var successors = new List<ExerciseSession>();
            foreach (var audience in Enum.GetValues<CampaignActionAudience>())
            {
                var query = CampaignExercises.Query(session, audience);
                if (!query.IsSuccessful) continue;
                var set = query.ActionSet!;
                foreach (var candidate in set.Candidates)
                {
                    var submitted = CampaignExercises.Submit(
                        session,
                        new CampaignActionSubmission(
                            CampaignActionSubmission.CurrentContractVersion,
                            set.CampaignId,
                            set.StateVersion,
                            set.PositionId,
                            set.Audience,
                            candidate.ActionId));
                    if (submitted.IsAccepted
                        && submitted.Evidence!.EventRecords.Count == 1
                        && submitted.Evidence.EventRecords[0].AsSpan().SequenceEqual(
                            campaignEvent.CanonicalBytes))
                        successors.Add(submitted.SuccessorSession!);
                }
            }
            if (successors.Count != 1)
            {
                return new ReconstructionProof(
                    ExerciseReconstructionFailureReason.InvalidHistory,
                    HashEventStream(start.CreationEventBytes!, events),
                    ReplayEvidenceHasher.HashBytes(finalSnapshot),
                    null);
            }
            session = successors[0];
        }

        var observed = CampaignExercises.Reconstruct(session);
        var expectedHash = ReplayEvidenceHasher.HashBytes(finalSnapshot);
        if (observed.FailureReason == ExerciseReconstructionFailureReason.InvalidHistory)
            return new ReconstructionProof(
                observed.FailureReason,
                observed.EventStreamHash,
                expectedHash,
                null);
        var reconstructedHash = observed.ReconstructedSnapshotHash!;
        var failureReason = string.Equals(
            expectedHash,
            reconstructedHash,
            StringComparison.Ordinal)
            ? ExerciseReconstructionFailureReason.None
            : ExerciseReconstructionFailureReason.SnapshotMismatch;
        return new ReconstructionProof(
            failureReason,
            observed.EventStreamHash,
            expectedHash,
            reconstructedHash);
    }

    private static ReadjudicationProof ObserveReadjudication(
        ExerciseManifest manifest,
        ExerciseRunIdentity identity,
        IReadOnlyList<ExerciseAcceptedActionRecord> actions,
        IReadOnlyList<ExerciseCanonicalEventRecord> events,
        byte[] finalSnapshot)
    {
        var start = CampaignExercises.Begin(ExerciseExecutor.CreateRequest(manifest, identity));
        var session = start.Session!;
        var readjudicatedTranscript = new List<byte[]>();
        var readjudicatedEvents = new List<byte[]>();
        var readjudicatedFinal = start.InitialSnapshotBytes!;
        for (var index = 0; index < actions.Count; index++)
        {
            var recorded = actions[index];
            var query = CampaignExercises.Query(session, recorded.Audience);
            var candidate = query.ActionSet?.Candidates.SingleOrDefault(value => string.Equals(
                value.ActionId,
                recorded.ActionId,
                StringComparison.Ordinal));
            if (candidate is null) break;
            var set = query.ActionSet!;
            var submitted = CampaignExercises.Submit(session, new CampaignActionSubmission(
                CampaignActionSubmission.CurrentContractVersion,
                set.CampaignId,
                set.StateVersion,
                set.PositionId,
                set.Audience,
                candidate.ActionId));
            if (!submitted.IsAccepted || !ReceiptMatches(submitted.Evidence!.Receipt, recorded))
                break;
            var evidence = submitted.Evidence;
            readjudicatedTranscript.Add(
                CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt));
            readjudicatedEvents.AddRange(evidence.EventRecords.Select(value => value.ToArray()));
            readjudicatedFinal = evidence.SnapshotCheckpoint;
            session = submitted.SuccessorSession!;
        }
        var terminal = CampaignExercises.Query(session, CampaignActionAudience.System);
        if (!terminal.IsSuccessful
            || !string.Equals(
                terminal.ActionSet!.PositionId,
                manifest.TerminalBoundary,
                StringComparison.Ordinal))
            readjudicatedFinal = [];

        return new ReadjudicationProof(
            ReplayEvidenceHasher.HashRecords(
                actions.Select(ExerciseEvidenceCodec.SerializeReceipt)),
            ReplayEvidenceHasher.HashRecords(readjudicatedTranscript),
            ReplayEvidenceHasher.HashRecords(
                events.Select(value => value.CanonicalBytes)),
            ReplayEvidenceHasher.HashRecords(readjudicatedEvents),
            ReplayEvidenceHasher.HashBytes(finalSnapshot),
            ReplayEvidenceHasher.HashBytes(readjudicatedFinal));
    }

    private static bool ReceiptMatches(
        CampaignActionAcceptanceReceipt receipt,
        ExerciseAcceptedActionRecord action) =>
        receipt.ContractVersion == CampaignActionAcceptanceReceipt.CurrentContractVersion
        && string.Equals(receipt.CampaignId, action.CampaignId, StringComparison.Ordinal)
        && receipt.PriorStateVersion == action.PriorStateVersion
        && receipt.CommittedStateVersion == action.CommittedStateVersion
        && string.Equals(
            receipt.ResultingPositionId,
            action.ResultingPositionId,
            StringComparison.Ordinal)
        && receipt.Audience == action.Audience
        && string.Equals(receipt.ActionId, action.ActionId, StringComparison.Ordinal);

    private static string HashEventStream(
        byte[] creationEvent,
        IReadOnlyList<ExerciseCanonicalEventRecord> events)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        ReadOnlySpan<byte> lineFeed = [(byte)'\n'];
        hash.AppendData(creationEvent);
        hash.AppendData(lineFeed);
        foreach (var campaignEvent in events)
        {
            hash.AppendData(campaignEvent.CanonicalBytes);
            hash.AppendData(lineFeed);
        }
        return $"sha256:{Convert.ToHexStringLower(hash.GetHashAndReset())}";
    }

    private static void ValidateBuildIdentity(
        byte[] normalizedManifestBytes,
        ExerciseManifest manifest,
        BuildIdentity build)
    {
        if (manifest.BuildMode != build.BuildMode
            || !string.Equals(manifest.RulesetHash, build.RulesetHash, StringComparison.Ordinal)
            || !string.Equals(
                build.ManifestHash,
                ReplayEvidenceHasher.HashBytes(normalizedManifestBytes),
                StringComparison.Ordinal)
            || !string.Equals(
                build.ConfigurationHash,
                ExerciseConfigurationIdentity.ComputeHash(manifest),
                StringComparison.Ordinal)
            || !string.Equals(
                build.SeedSchemeId,
                ExerciseSeedLedger.SchemeId,
                StringComparison.Ordinal))
            throw new InvalidDataException(
                "The manifest and build identity are semantically inconsistent.");
    }

    private static void ValidateSeedIdentity(
        ExerciseManifest manifest,
        ExerciseSeedLedger ledger)
    {
        if (manifest.RootSeed != ledger.Identity.RootSeed)
            throw new InvalidDataException(
                "The manifest and seed ledger are semantically inconsistent.");
    }

    private static void ValidateEvidence(
        ExerciseManifest manifest,
        IReadOnlyList<ExerciseAcceptedActionRecord> actions,
        IReadOnlyList<ExerciseCanonicalEventRecord> events,
        IReadOnlyList<ExerciseStepEvidenceRecord> steps,
        ExerciseSnapshotFacts initial,
        ExerciseSnapshotFacts final,
        byte[] finalSnapshot)
    {
        if (actions.Count != events.Count || actions.Count != steps.Count)
            throw new InvalidDataException("Action, event, and step evidence counts disagree.");
        if (!string.Equals(initial.CampaignId, final.CampaignId, StringComparison.Ordinal)
            || !string.Equals(initial.RulesetHash, manifest.RulesetHash, StringComparison.Ordinal)
            || !string.Equals(final.RulesetHash, manifest.RulesetHash, StringComparison.Ordinal))
            throw new InvalidDataException("Snapshot campaign or ruleset evidence disagrees.");

        var expectedPriorVersion = initial.StateVersion;
        for (var index = 0; index < actions.Count; index++)
        {
            var action = actions[index];
            var step = steps[index];
            var campaignEvent = events[index];
            if (action.StepOrdinal != index
                || step.StepOrdinal != index
                || action.PriorStateVersion != expectedPriorVersion
                || action.CommittedStateVersion != checked(expectedPriorVersion + 1)
                || !string.Equals(action.CampaignId, initial.CampaignId, StringComparison.Ordinal)
                || !string.Equals(step.CampaignId, action.CampaignId, StringComparison.Ordinal)
                || !string.Equals(campaignEvent.CampaignId, action.CampaignId, StringComparison.Ordinal)
                || step.StateVersion != action.CommittedStateVersion
                || campaignEvent.StateVersion != action.CommittedStateVersion
                || !string.Equals(step.PositionId, action.ResultingPositionId, StringComparison.Ordinal)
                || !string.Equals(campaignEvent.PositionId, action.ResultingPositionId, StringComparison.Ordinal)
                || step.Audience != action.Audience
                || !string.Equals(step.ActionId, action.ActionId, StringComparison.Ordinal)
                || !string.Equals(
                    step.EventsHash,
                    ReplayEvidenceHasher.HashRecords([campaignEvent.CanonicalBytes]),
                    StringComparison.Ordinal))
                throw new InvalidDataException("Accepted action, event, and step coordinates disagree.");
            expectedPriorVersion = action.CommittedStateVersion;
        }

        if (final.StateVersion != expectedPriorVersion)
            throw new InvalidDataException("The final snapshot state version is not continuous.");
        if (actions.Count > 0)
        {
            var action = actions[^1];
            var finalStep = steps[^1];
            if (!string.Equals(final.CampaignId, action.CampaignId, StringComparison.Ordinal)
                || !string.Equals(final.PositionId, action.ResultingPositionId, StringComparison.Ordinal)
                || !string.Equals(
                    finalStep.SnapshotHash,
                    ReplayEvidenceHasher.HashBytes(finalSnapshot),
                    StringComparison.Ordinal))
                throw new InvalidDataException("The final step does not anchor the final snapshot.");
        }
    }

    private static void ValidateProofObservations(
        ArtifactBundleProfile profile,
        ReconstructionProof? reconstruction,
        ReadjudicationProof? readjudication,
        AuthoritativeReplayObservations authoritative)
    {
        if (reconstruction is not null
            && !ReconstructionMatches(reconstruction, authoritative.Reconstruction))
            throw new InvalidDataException(
                "The reconstruction proof does not match authoritative replay observations.");
        if (readjudication is not null
            && !ReadjudicationMatches(readjudication, authoritative.Readjudication))
            throw new InvalidDataException(
                "The re-adjudication proof does not match authoritative replay observations.");
        if (profile is ArtifactBundleProfile.Succeeded
                or ArtifactBundleProfile.FailedReconstructed
                or ArtifactBundleProfile.FailedReadjudicated
            && reconstruction is null)
            throw new InvalidDataException(
                "The replay profile is missing its authoritative reconstruction observation.");
        if (profile is ArtifactBundleProfile.Succeeded
                or ArtifactBundleProfile.FailedReadjudicated
            && readjudication is null)
            throw new InvalidDataException(
                "The replay profile is missing its authoritative re-adjudication observation.");
    }

    private static bool ReconstructionMatches(
        ReconstructionProof stored,
        ReconstructionProof authoritative) =>
        stored.FailureReason == authoritative.FailureReason
        && string.Equals(
            stored.EventStreamHash,
            authoritative.EventStreamHash,
            StringComparison.Ordinal)
        && string.Equals(
            stored.ExpectedSnapshotHash,
            authoritative.ExpectedSnapshotHash,
            StringComparison.Ordinal)
        && string.Equals(
            stored.ReconstructedSnapshotHash,
            authoritative.ReconstructedSnapshotHash,
            StringComparison.Ordinal);

    private static bool ReadjudicationMatches(
        ReadjudicationProof stored,
        ReadjudicationProof authoritative) =>
        string.Equals(
            stored.ExpectedTranscriptHash,
            authoritative.ExpectedTranscriptHash,
            StringComparison.Ordinal)
        && string.Equals(
            stored.ReadjudicatedTranscriptHash,
            authoritative.ReadjudicatedTranscriptHash,
            StringComparison.Ordinal)
        && string.Equals(
            stored.ExpectedEventsHash,
            authoritative.ExpectedEventsHash,
            StringComparison.Ordinal)
        && string.Equals(
            stored.ReadjudicatedEventsHash,
            authoritative.ReadjudicatedEventsHash,
            StringComparison.Ordinal)
        && string.Equals(
            stored.ExpectedFinalSnapshotHash,
            authoritative.ExpectedFinalSnapshotHash,
            StringComparison.Ordinal)
        && string.Equals(
            stored.ReadjudicatedFinalSnapshotHash,
            authoritative.ReadjudicatedFinalSnapshotHash,
            StringComparison.Ordinal);

    private static void ValidateProfileMatrix(
        ArtifactBundleProfile profile,
        ExerciseManifest manifest,
        ExerciseRunResult result,
        ExerciseCheckResults checks,
        IReadOnlyList<ExerciseAcceptedActionRecord> actions,
        ExerciseSnapshotFacts initial,
        ExerciseSnapshotFacts final,
        byte[] initialSnapshot,
        byte[] finalSnapshot,
        ReconstructionProof? reconstruction,
        ReadjudicationProof? readjudication)
    {
        var nextCheck = ValidateCompletedStepChecks(checks.Results, actions);
        switch (profile)
        {
            case ArtifactBundleProfile.Succeeded:
                ValidateSucceeded(
                    manifest,
                    result,
                    checks.Results,
                    nextCheck,
                    actions.Count,
                    initial,
                    final,
                    initialSnapshot,
                    finalSnapshot,
                    reconstruction,
                    readjudication);
                break;
            case ArtifactBundleProfile.FailedExecuted:
                ValidateFailedExecuted(
                    result,
                    checks.Results,
                    nextCheck,
                    actions.Count,
                    manifest.MaximumSteps);
                RequireProofs(reconstruction, readjudication, false, false);
                break;
            case ArtifactBundleProfile.FailedReconstructed:
                RequireFailure(result, ExerciseFailureCategory.ReconstructionMismatch);
                RequireRunChecks(
                    checks.Results,
                    nextCheck,
                    (ExerciseCheckId.TerminalBoundary, true, ExerciseCheckFailureCode.None),
                    (ExerciseCheckId.HistoryReconstruction, false,
                        ExerciseCheckFailureCode.ReconstructionMismatch));
                RequireProofs(reconstruction, readjudication, true, false);
                if (reconstruction!.IsVerified)
                    throw new InvalidDataException("A reconstruction-failure proof cannot be verified.");
                break;
            case ArtifactBundleProfile.FailedReadjudicated:
                RequireFailure(result, ExerciseFailureCategory.ReadjudicationMismatch);
                RequireRunChecks(
                    checks.Results,
                    nextCheck,
                    (ExerciseCheckId.TerminalBoundary, true, ExerciseCheckFailureCode.None),
                    (ExerciseCheckId.HistoryReconstruction, true, ExerciseCheckFailureCode.None),
                    (ExerciseCheckId.Readjudication, false,
                        ExerciseCheckFailureCode.ReadjudicationMismatch));
                RequireProofs(reconstruction, readjudication, true, true);
                if (!reconstruction!.IsVerified || readjudication!.IsVerified)
                    throw new InvalidDataException("The replay-failure proofs contradict the profile.");
                break;
            case ArtifactBundleProfile.FailedSummarized:
                if (result.Completion is not ExerciseFailed)
                    throw new InvalidDataException("A failed summary profile requires failure.");
                break;
            default:
                throw new InvalidDataException("The executed bundle profile is unsupported.");
        }
    }

    private static void ValidateSucceeded(
        ExerciseManifest manifest,
        ExerciseRunResult result,
        IReadOnlyList<ExerciseCheckResult> checks,
        int nextCheck,
        int acceptedCount,
        ExerciseSnapshotFacts initial,
        ExerciseSnapshotFacts final,
        byte[] initialSnapshot,
        byte[] finalSnapshot,
        ReconstructionProof? reconstruction,
        ReadjudicationProof? readjudication)
    {
        if (result.Completion is not ExerciseSucceeded { Outcome: BoundaryReached boundary }
            || !string.Equals(boundary.PositionId, manifest.TerminalBoundary, StringComparison.Ordinal)
            || !string.Equals(final.PositionId, manifest.TerminalBoundary, StringComparison.Ordinal))
            throw new InvalidDataException(
                "Success must reach the admitted terminal boundary in final evidence.");
        if (acceptedCount == 0
            && (!initialSnapshot.AsSpan().SequenceEqual(finalSnapshot)
                || !string.Equals(
                    initial.PositionId,
                    manifest.TerminalBoundary,
                    StringComparison.Ordinal)))
            throw new InvalidDataException(
                "Zero-step success must begin and end at the admitted boundary.");
        RequireRunChecks(
            checks,
            nextCheck,
            (ExerciseCheckId.TerminalBoundary, true, ExerciseCheckFailureCode.None),
            (ExerciseCheckId.HistoryReconstruction, true, ExerciseCheckFailureCode.None),
            (ExerciseCheckId.Readjudication, true, ExerciseCheckFailureCode.None));
        RequireProofs(reconstruction, readjudication, true, true);
        if (!reconstruction!.IsVerified || !readjudication!.IsVerified)
            throw new InvalidDataException("A successful bundle requires two verified proofs.");
    }

    private static int ValidateCompletedStepChecks(
        IReadOnlyList<ExerciseCheckResult> checks,
        IReadOnlyList<ExerciseAcceptedActionRecord> actions)
    {
        var index = 0;
        foreach (var action in actions)
        {
            RequireStepCheck(checks, ref index, ExerciseCheckId.AuthorityQueryValid,
                CampaignActionAudience.System, action.StepOrdinal);
            RequireStepCheck(checks, ref index, ExerciseCheckId.AuthorityQueryValid,
                CampaignActionAudience.Axis, action.StepOrdinal);
            RequireStepCheck(checks, ref index, ExerciseCheckId.AuthorityQueryValid,
                CampaignActionAudience.Commonwealth, action.StepOrdinal);
            RequireStepCheck(checks, ref index, ExerciseCheckId.ActiveAudienceCardinality,
                null, action.StepOrdinal);
            RequireStepCheck(checks, ref index, ExerciseCheckId.SelectedActionMembership,
                action.Audience, action.StepOrdinal);
            RequireStepCheck(checks, ref index, ExerciseCheckId.AcceptedEventCardinality,
                action.Audience, action.StepOrdinal);
            RequireStepCheck(checks, ref index, ExerciseCheckId.CheckpointContinuity,
                action.Audience, action.StepOrdinal);
        }
        return index;
    }

    private static void ValidateFailedExecuted(
        ExerciseRunResult result,
        IReadOnlyList<ExerciseCheckResult> checks,
        int nextCheck,
        int acceptedCount,
        int maximumSteps)
    {
        if (result.Completion is not ExerciseFailed failed)
            throw new InvalidDataException("A failed-executed profile requires failure.");
        if (checks.Count == nextCheck
            || !IsCheck(
                checks[^1],
                ExerciseCheckId.TerminalBoundary,
                false,
                ExerciseCheckFailureCode.TerminalBoundaryNotReached,
                null,
                null))
            throw new InvalidDataException(
                "A failed-executed profile must end with terminal-boundary failure.");
        var decisionChecks = checks.Skip(nextCheck).SkipLast(1).ToArray();
        if (failed.Failure.Category == ExerciseFailureCategory.StepLimitExceeded)
        {
            if (decisionChecks.Length != 0 || acceptedCount != maximumSteps)
                throw new InvalidDataException(
                    "Step-limit failure requires the admitted maximum accepted-step count.");
            return;
        }
        if (failed.Failure.Category == ExerciseFailureCategory.Cancelled)
        {
            if (decisionChecks.Length != 0)
                throw new InvalidDataException("Cancellation has no failed step check.");
            return;
        }
        if (decisionChecks.Length == 0
            || decisionChecks.Any(value => value.StepOrdinal != acceptedCount)
            || decisionChecks.Take(decisionChecks.Length - 1).Any(value => !value.IsPassed))
            throw new InvalidDataException("The failed decision check sequence is contradictory.");
        var terminalDecision = decisionChecks[^1];
        var valid = failed.Failure.Category switch
        {
            ExerciseFailureCategory.ControllerFailed => IsCheck(
                terminalDecision,
                ExerciseCheckId.SelectedActionMembership,
                false,
                ExerciseCheckFailureCode.SelectedActionNotCurrent),
            ExerciseFailureCategory.NoUniqueLegalAction => IsCheck(
                terminalDecision,
                ExerciseCheckId.ActiveAudienceCardinality,
                false,
                ExerciseCheckFailureCode.NoActiveAudience),
            ExerciseFailureCategory.IllegalAction => IsCheck(
                terminalDecision,
                ExerciseCheckId.AcceptedEventCardinality,
                false,
                ExerciseCheckFailureCode.ActionRejected),
            ExerciseFailureCategory.InvariantFailed => IsAllowedInvariantFailure(terminalDecision),
            _ => false,
        };
        if (!valid)
            throw new InvalidDataException("The Exercise failure category and failed check disagree.");
    }

    private static bool IsAllowedInvariantFailure(ExerciseCheckResult check) =>
        (check.CheckId, check.FailureCode) switch
        {
            (ExerciseCheckId.AuthorityQueryValid,
                ExerciseCheckFailureCode.AuthorityQueryRejected
                    or ExerciseCheckFailureCode.AuthorityQueryCoordinateMismatch) => true,
            (ExerciseCheckId.ActiveAudienceCardinality,
                ExerciseCheckFailureCode.MultipleActiveAudiences) => true,
            (ExerciseCheckId.AcceptedEventCardinality,
                ExerciseCheckFailureCode.EventCardinalityMismatch) => true,
            (ExerciseCheckId.CheckpointContinuity,
                ExerciseCheckFailureCode.CampaignMismatch
                    or ExerciseCheckFailureCode.RulesetMismatch
                    or ExerciseCheckFailureCode.StateVersionDiscontinuity
                    or ExerciseCheckFailureCode.PositionMismatch) => true,
            _ => false,
        };

    private static void RequireStepCheck(
        IReadOnlyList<ExerciseCheckResult> checks,
        ref int index,
        ExerciseCheckId id,
        CampaignActionAudience? audience,
        int stepOrdinal)
    {
        if (index >= checks.Count
            || !IsCheck(
                checks[index],
                id,
                true,
                ExerciseCheckFailureCode.None,
                stepOrdinal,
                audience))
            throw new InvalidDataException("A completed step lacks its full passed check catalog.");
        index++;
    }

    private static void RequireRunChecks(
        IReadOnlyList<ExerciseCheckResult> checks,
        int start,
        params (ExerciseCheckId Id, bool Passed, ExerciseCheckFailureCode Failure)[] expected)
    {
        if (checks.Count - start != expected.Length)
            throw new InvalidDataException("The run-level check matrix is incomplete.");
        for (var index = 0; index < expected.Length; index++)
        {
            var item = expected[index];
            if (!IsCheck(checks[start + index], item.Id, item.Passed, item.Failure, null, null))
                throw new InvalidDataException("The run-level check matrix contradicts the profile.");
        }
    }

    private static bool IsCheck(
        ExerciseCheckResult check,
        ExerciseCheckId id,
        bool passed,
        ExerciseCheckFailureCode failure,
        int? stepOrdinal = null,
        CampaignActionAudience? audience = null) =>
        check.CheckId == id
        && check.IsPassed == passed
        && check.FailureCode == failure
        && check.StepOrdinal == stepOrdinal
        && check.Audience == audience;

    private static void RequireFailure(
        ExerciseRunResult result,
        ExerciseFailureCategory category)
    {
        if (result.Completion is not ExerciseFailed { Failure.Category: var actual }
            || actual != category)
            throw new InvalidDataException("The run result failure contradicts its profile.");
    }

    private static void RequireProofs(
        ReconstructionProof? reconstruction,
        ReadjudicationProof? readjudication,
        bool hasReconstruction,
        bool hasReadjudication)
    {
        if ((reconstruction is not null) != hasReconstruction
            || (readjudication is not null) != hasReadjudication)
            throw new InvalidDataException("The bundle profile and replay proofs disagree.");
    }

    private sealed record AuthoritativeReplayObservations(
        ReconstructionProof Reconstruction,
        ReadjudicationProof Readjudication);
}
