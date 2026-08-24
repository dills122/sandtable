using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class ExerciseBundleSemanticValidatorTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        $"sandtable-exercise-semantic-reader-{Guid.NewGuid():N}");

    [Fact]
    public void ReaderRejectsARehashedSuccessfulBoundaryThatContradictsTheAdmittedManifest()
    {
        var bundlePath = CreateSuccessfulBundle();
        var resultPath = Path.Combine(bundlePath, ArtifactSchema.RunResultPath);
        var result = Encoding.UTF8.GetString(File.ReadAllBytes(resultPath)).Replace(
            "land.position.operation-1.organization",
            "land.position.operation-1.movement",
            StringComparison.Ordinal);
        File.WriteAllText(resultPath, result, new UTF8Encoding(false));
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsARehashedNestedInitialSnapshotMutation()
    {
        var bundlePath = CreateSuccessfulBundle();
        RewritePayload(bundlePath, ArtifactSchema.InitialSnapshotPath, bytes =>
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace(
                "\"elementId\":\"axis-element-a\",\"currentLocationId\":\"west\"",
                "\"elementId\":\"axis-element-a\",\"currentLocationId\":\"east\"",
                StringComparison.Ordinal)));
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsARehashedEventWithAContradictoryFromPosition()
    {
        var bundlePath = CreateSuccessfulBundle();
        RewritePayload(bundlePath, ArtifactSchema.CanonicalEventsPath, bytes =>
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace(
                "\"fromPositionId\":\"land.position.initiative-determination\"",
                "\"fromPositionId\":\"land.position.naval-convoy.schedule\"",
                StringComparison.Ordinal)));
        RefreshDependentHashes(bundlePath);
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsARehashedCampaignIdNotDerivedFromTheSeedLedger()
    {
        var bundlePath = CreateSuccessfulBundle();
        var initial = File.ReadAllBytes(Path.Combine(
            bundlePath,
            ArtifactSchema.InitialSnapshotPath));
        var campaignId = ExerciseEvidenceCodec.DeserializeSnapshot(initial).CampaignId;
        var changedCampaignId = $"exercise-{new string('f', 64)}";
        foreach (var path in new[]
        {
            ArtifactSchema.AcceptedActionsPath,
            ArtifactSchema.CanonicalEventsPath,
            ArtifactSchema.StepEvidencePath,
            ArtifactSchema.InitialSnapshotPath,
            ArtifactSchema.FinalSnapshotPath,
        })
        {
            RewritePayload(bundlePath, path, bytes => Encoding.UTF8.GetBytes(
                Encoding.UTF8.GetString(bytes).Replace(
                    campaignId,
                    changedCampaignId,
                    StringComparison.Ordinal)));
        }
        RefreshDependentHashes(bundlePath);
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsChangedFinalStateForAZeroActionFailedExecution()
    {
        var bundlePath = CreateCancelledZeroStepBundle();
        RewritePayload(bundlePath, ArtifactSchema.FinalSnapshotPath, bytes =>
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace(
                "\"elementId\":\"axis-element-a\",\"currentLocationId\":\"west\"",
                "\"elementId\":\"axis-element-a\",\"currentLocationId\":\"east\"",
                StringComparison.Ordinal)));
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsARehashedReconstructionEventStreamHash()
    {
        var bundlePath = CreateSuccessfulBundle();
        RewritePayload(bundlePath, ArtifactSchema.ReconstructionProofPath, bytes =>
        {
            var proof = ReplayProofCodec.DeserializeReconstruction(bytes);
            return ReplayProofCodec.Serialize(new ReconstructionProof(
                proof.FailureReason,
                Sha('f'),
                proof.ExpectedSnapshotHash,
                proof.ReconstructedSnapshotHash));
        });
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsZeroStepCancellationRelabeledAsStepLimitFailure()
    {
        var bundlePath = CreateCancelledZeroStepBundle();
        RewritePayload(
            bundlePath,
            ArtifactSchema.RunResultPath,
            _ => ExerciseRunResultCodec.Serialize(ExerciseRunResult.Failed(
                ExerciseFailureCategory.StepLimitExceeded,
                null)));
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsRehashedFailedIdentifiedChecksThatClaimExecutionEvidence()
    {
        var bundlePath = CreateFailedIdentifiedBundle();
        RewritePayload(
            bundlePath,
            ArtifactSchema.CheckResultsPath,
            _ => ExerciseCheckResultsCodec.Serialize(new ExerciseCheckResults(
            [
                ExerciseCheckResult.Passed(
                    ExerciseCheckId.TerminalBoundary,
                    null,
                    null),
            ])));
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Theory]
    [InlineData(ArtifactBundleProfile.FailedAdmitted)]
    [InlineData(ArtifactBundleProfile.FailedIdentified)]
    public void ReaderRejectsAnEarlyFailureAssertionContradictingTheAdmittedManifest(
        ArtifactBundleProfile profile)
    {
        var bundlePath = profile == ArtifactBundleProfile.FailedAdmitted
            ? CreateFailedAdmittedBundle(ExerciseFailureCategory.Cancelled)
            : CreateFailedIdentifiedBundle(ExerciseFailureCategory.Cancelled);
        var actualCategory = profile == ArtifactBundleProfile.FailedAdmitted
            ? ExerciseFailureCategory.BuildIdentityUnavailable
            : ExerciseFailureCategory.UnexpectedFailure;
        RewritePayload(
            bundlePath,
            ArtifactSchema.RunResultPath,
            _ => ExerciseRunResultCodec.Serialize(ExerciseRunResult.Failed(
                actualCategory,
                null)));
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Theory]
    [InlineData("build-mode")]
    [InlineData("manifest-hash")]
    [InlineData("ruleset-hash")]
    [InlineData("configuration-hash")]
    [InlineData("seed-scheme")]
    public void ReaderRejectsARehashedFailedIdentifiedBuildContradiction(string mutation)
    {
        var bundlePath = CreateFailedIdentifiedBundle();
        RewritePayload(bundlePath, ArtifactSchema.BuildIdentityPath, bytes =>
        {
            if (mutation == "ruleset-hash")
                return Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace(
                    Cna1979Ruleset.Manifest.Hash,
                    new string('e', 64),
                    StringComparison.Ordinal));
            if (mutation == "seed-scheme")
                return Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace(
                    ExerciseSeedLedger.SchemeId,
                    "sandtable.exercise-seed-ledger.v2",
                    StringComparison.Ordinal));
            var identity = BuildIdentityCodec.Deserialize(bytes);
            return BuildIdentityCodec.Serialize(mutation switch
            {
                "build-mode" => CopyBuildIdentity(
                    identity,
                    ExerciseBuildMode.Baseline,
                    identity.ManifestHash,
                    identity.ConfigurationHash,
                    dirty: false,
                    baselineEligible: true,
                    reproducible: true),
                "manifest-hash" => CopyBuildIdentity(
                    identity,
                    identity.BuildMode,
                    Sha('e'),
                    identity.ConfigurationHash,
                    identity.Dirty,
                    identity.BaselineEligible,
                    identity.Reproducible),
                "configuration-hash" => CopyBuildIdentity(
                    identity,
                    identity.BuildMode,
                    identity.ManifestHash,
                    Sha('e'),
                    identity.Dirty,
                    identity.BaselineEligible,
                    identity.Reproducible),
                _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
            });
        });
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderAcceptsProfileSpecificEarlyFailureEvidenceWithoutASeedLedger()
    {
        var admitted = ExerciseBundleReader.Read(CreateFailedAdmittedBundle(
            ExerciseFailureCategory.Cancelled));
        var identified = ExerciseBundleReader.Read(CreateFailedIdentifiedBundle(
            ExerciseFailureCategory.Cancelled));

        Assert.Equal(
            ExerciseFailureCategory.Cancelled,
            admitted.RunResult.FailureAssertion!.ExpectedCategory);
        Assert.Null(admitted.BuildIdentity);
        Assert.Equal(
            ExerciseFailureCategory.Cancelled,
            identified.RunResult.FailureAssertion!.ExpectedCategory);
        Assert.NotNull(identified.BuildIdentity);
        Assert.Null(identified.SeedLedger);
    }

    [Theory]
    [InlineData(ArtifactBundleProfile.Succeeded)]
    [InlineData(ArtifactBundleProfile.FailedReconstructed)]
    [InlineData(ArtifactBundleProfile.FailedReadjudicated)]
    public void ReaderRejectsExecutedEvidenceExceedingTheAdmittedMaximumSteps(
        ArtifactBundleProfile profile)
    {
        var bundlePath = profile switch
        {
            ArtifactBundleProfile.Succeeded => CreateSuccessfulBundle(),
            ArtifactBundleProfile.FailedReconstructed =>
                CreateReconstructionFailureBundle(fabricated: false),
            ArtifactBundleProfile.FailedReadjudicated =>
                CreateReadjudicationFailureBundle(fabricated: false),
            _ => throw new ArgumentOutOfRangeException(nameof(profile)),
        };
        var actions = ExerciseEvidenceCodec.DeserializeAcceptedActions(File.ReadAllBytes(
            Path.Combine(bundlePath, ArtifactSchema.AcceptedActionsPath)));
        Assert.NotEmpty(actions);
        RewriteManifest(
            bundlePath,
            manifest => WithMaximumSteps(manifest, actions.Count - 1));

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsSuccessWhenTheManifestExpectedFailure()
    {
        var bundlePath = CreateSuccessfulBundle();
        RewriteManifest(
            bundlePath,
            manifest => WithFailureAssertion(
                manifest,
                ExerciseFailureCategory.StepLimitExceeded));

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReaderRejectsFailureAssertionPresenceThatContradictsTheManifest(
        bool assertionIsOnlyInManifest)
    {
        var bundlePath = CreateCancelledZeroStepBundle();
        if (assertionIsOnlyInManifest)
        {
            RewriteManifest(
                bundlePath,
                manifest => WithFailureAssertion(
                    manifest,
                    ExerciseFailureCategory.Cancelled));
        }
        else
        {
            RewritePayload(
                bundlePath,
                ArtifactSchema.RunResultPath,
                _ => ExerciseRunResultCodec.Serialize(ExerciseRunResult.Failed(
                    ExerciseFailureCategory.Cancelled,
                    ExerciseFailureCategory.Cancelled)));
            RehashManifest(bundlePath);
        }

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Theory]
    [InlineData(ExerciseFailureCategory.Cancelled)]
    [InlineData(ExerciseFailureCategory.StepLimitExceeded)]
    public void ReaderAcceptsFailureAssertionMatchingTheManifest(
        ExerciseFailureCategory expectedCategory)
    {
        var bundlePath = CreateCancelledZeroStepBundle();
        RewriteManifest(
            bundlePath,
            manifest => WithFailureAssertion(manifest, expectedCategory));
        RewritePayload(
            bundlePath,
            ArtifactSchema.RunResultPath,
            _ => ExerciseRunResultCodec.Serialize(ExerciseRunResult.Failed(
                ExerciseFailureCategory.Cancelled,
                expectedCategory)));
        RehashManifest(bundlePath);

        var bundle = ExerciseBundleReader.Read(bundlePath);

        Assert.Equal(expectedCategory, bundle.RunResult.FailureAssertion!.ExpectedCategory);
        Assert.Equal(
            expectedCategory == ExerciseFailureCategory.Cancelled,
            bundle.RunResult.FailureAssertion.Matches);
    }

    [Fact]
    public void ReaderAcceptsAGenuineFailedReconstructedBundle()
    {
        var bundle = ExerciseBundleReader.Read(CreateReconstructionFailureBundle(
            fabricated: false));

        Assert.Equal(ArtifactBundleProfile.FailedReconstructed, bundle.Manifest.Profile);
        Assert.Equal(
            ExerciseReconstructionFailureReason.SnapshotMismatch,
            bundle.ReconstructionProof!.FailureReason);
        Assert.False(bundle.ReconstructionProof.IsVerified);
        Assert.Null(bundle.ReadjudicationProof);
    }

    [Fact]
    public void ReaderAcceptsAGenuineFailedReadjudicatedBundle()
    {
        var bundle = ExerciseBundleReader.Read(CreateReadjudicationFailureBundle(
            fabricated: false));

        Assert.Equal(ArtifactBundleProfile.FailedReadjudicated, bundle.Manifest.Profile);
        Assert.True(bundle.ReconstructionProof!.IsVerified);
        Assert.False(bundle.ReadjudicationProof!.IsVerified);
        Assert.False(bundle.ReadjudicationProof.TranscriptMatches);
        Assert.False(bundle.ReadjudicationProof.EventsMatch);
        Assert.False(bundle.ReadjudicationProof.FinalSnapshotMatches);
    }

    [Fact]
    public void ReaderRejectsRehashedTamperingWithAReconstructionFailureObservation()
    {
        var bundlePath = CreateReconstructionFailureBundle(fabricated: false);
        RewritePayload(bundlePath, ArtifactSchema.ReconstructionProofPath, bytes =>
        {
            var proof = ReplayProofCodec.DeserializeReconstruction(bytes);
            return ReplayProofCodec.Serialize(new ReconstructionProof(
                proof.FailureReason,
                proof.EventStreamHash,
                proof.ExpectedSnapshotHash,
                Sha('e')));
        });
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsRehashedActionEvidenceInAReconstructionFailureBundle()
    {
        var bundlePath = CreateReconstructionFailureBundle(fabricated: false);
        var actions = ExerciseEvidenceCodec.DeserializeAcceptedActions(File.ReadAllBytes(
            Path.Combine(bundlePath, ArtifactSchema.AcceptedActionsPath)));
        var originalActionId = actions[^1].ActionId;
        RewritePayload(bundlePath, ArtifactSchema.AcceptedActionsPath, bytes =>
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace(
                originalActionId,
                Sha('e'),
                StringComparison.Ordinal)));
        RewritePayload(bundlePath, ArtifactSchema.StepEvidencePath, bytes =>
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace(
                originalActionId,
                Sha('e'),
                StringComparison.Ordinal)));
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsRehashedTamperingWithAReadjudicationFailureObservation()
    {
        var bundlePath = CreateReadjudicationFailureBundle(fabricated: false);
        RewritePayload(bundlePath, ArtifactSchema.ReadjudicationProofPath, bytes =>
        {
            var proof = ReplayProofCodec.DeserializeReadjudication(bytes);
            return ReplayProofCodec.Serialize(new ReadjudicationProof(
                proof.ExpectedTranscriptHash,
                Sha('e'),
                proof.ExpectedEventsHash,
                proof.ReadjudicatedEventsHash,
                proof.ExpectedFinalSnapshotHash,
                proof.ReadjudicatedFinalSnapshotHash));
        });
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Theory]
    [InlineData(ArtifactBundleProfile.FailedReconstructed)]
    [InlineData(ArtifactBundleProfile.FailedReadjudicated)]
    public void ReaderRejectsSuccessfulReplayRelabeledAsAReplayFailure(
        ArtifactBundleProfile profile)
    {
        var bundlePath = profile == ArtifactBundleProfile.FailedReconstructed
            ? CreateReconstructionFailureBundle(fabricated: true)
            : CreateReadjudicationFailureBundle(fabricated: true);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderReturnsDefensiveCopiesOfEveryRetainedByteSequence()
    {
        var bundle = ExerciseBundleReader.Read(CreateSuccessfulBundle());
        var artifactManifest = bundle.ArtifactManifestBytes;
        var normalizedManifest = bundle.NormalizedManifestBytes!;
        var initialSnapshot = bundle.InitialSnapshotBytes!;
        var finalSnapshot = bundle.FinalSnapshotBytes!;
        var canonicalEvent = bundle.CanonicalEvents[0];
        var expectedArtifactManifest = artifactManifest.ToArray();
        var expectedNormalizedManifest = normalizedManifest.ToArray();
        var expectedInitialSnapshot = initialSnapshot.ToArray();
        var expectedFinalSnapshot = finalSnapshot.ToArray();
        var expectedCanonicalEvent = canonicalEvent.ToArray();

        artifactManifest[0] ^= 0xff;
        normalizedManifest[0] ^= 0xff;
        initialSnapshot[0] ^= 0xff;
        finalSnapshot[0] ^= 0xff;
        canonicalEvent[0] ^= 0xff;

        Assert.Equal(expectedArtifactManifest, bundle.ArtifactManifestBytes);
        Assert.Equal(expectedNormalizedManifest, bundle.NormalizedManifestBytes);
        Assert.Equal(expectedInitialSnapshot, bundle.InitialSnapshotBytes);
        Assert.Equal(expectedFinalSnapshot, bundle.FinalSnapshotBytes);
        Assert.Equal(expectedCanonicalEvent, bundle.CanonicalEvents[0]);
    }

    [Theory]
    [InlineData("manifest")]
    [InlineData("build")]
    [InlineData("ledger")]
    public void ReaderRejectsRehashedManifestBuildAndLedgerIdentityContradictions(string mutation)
    {
        var bundlePath = CreateSuccessfulBundle();
        switch (mutation)
        {
            case "manifest":
                RewritePayload(
                    bundlePath,
                    ArtifactSchema.ExerciseManifestPath,
                    bytes => ExerciseManifestCodec.Serialize(WithRootSeed(
                        ExerciseManifestCodec.Deserialize(bytes),
                        1)));
                break;
            case "build":
                RewritePayload(bundlePath, ArtifactSchema.BuildIdentityPath, bytes =>
                {
                    var identity = BuildIdentityCodec.Deserialize(bytes);
                    return BuildIdentityCodec.Serialize(new BuildIdentity(
                        identity.BuildMode,
                        identity.HeadCommit,
                        identity.HeadTree,
                        identity.Dirty,
                        identity.PorcelainSha256,
                        identity.FrameworkDescription,
                        identity.OsArchitecture,
                        identity.ProcessArchitecture,
                        identity.RulesetHash,
                        identity.ConfigurationHash,
                        Sha('f'),
                        identity.SeedSchemeId,
                        identity.BaselineEligible,
                        identity.Reproducible,
                        identity.Artifacts));
                });
                break;
            case "ledger":
                RewritePayload(bundlePath, ArtifactSchema.SeedLedgerPath, bytes =>
                {
                    var ledger = SeedLedgerCodec.Deserialize(bytes);
                    return SeedLedgerCodec.Serialize(ExerciseSeedLedger.Create(
                        new ExerciseRunIdentity(
                            1,
                            ledger.Identity.ManeuverId,
                            ledger.Identity.ExerciseOrdinal,
                            ledger.Identity.PairKey)));
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation));
        }
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Theory]
    [InlineData("action")]
    [InlineData("proof")]
    public void ReaderRejectsRehashedEvidenceAndProofContradictions(string mutation)
    {
        var bundlePath = CreateSuccessfulBundle();
        if (mutation == "action")
        {
            RewritePayload(bundlePath, ArtifactSchema.AcceptedActionsPath, bytes =>
            {
                var actions = ExerciseEvidenceCodec.DeserializeAcceptedActions(bytes);
                return Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace(
                    actions[0].ActionId,
                    Sha('f'),
                    StringComparison.Ordinal));
            });
        }
        else
        {
            RewritePayload(bundlePath, ArtifactSchema.ReadjudicationProofPath, bytes =>
            {
                var proof = ReplayProofCodec.DeserializeReadjudication(bytes);
                return ReplayProofCodec.Serialize(new ReadjudicationProof(
                    Sha('f'),
                    Sha('f'),
                    proof.ExpectedEventsHash,
                    proof.ReadjudicatedEventsHash,
                    proof.ExpectedFinalSnapshotHash,
                    proof.ReadjudicatedFinalSnapshotHash));
            });
        }
        RehashManifest(bundlePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Theory]
    [InlineData("reordered")]
    [InlineData("extra")]
    [InlineData("unknown")]
    public void CanonicalEventReaderRejectsNonContractFieldsAndOrdering(string mutation)
    {
        var bundle = ExerciseBundleReader.Read(CreateSuccessfulBundle());
        var canonicalEvent = Encoding.UTF8.GetString(bundle.CanonicalEvents[0]);
        var changed = mutation switch
        {
            "reordered" => canonicalEvent.Replace(
                "{\"contractVersion\":2,\"eventType\":\"initiative-determined\"",
                "{\"eventType\":\"initiative-determined\",\"contractVersion\":2",
                StringComparison.Ordinal),
            "extra" => $"{{\"extra\":null,{canonicalEvent[1..]}",
            "unknown" => canonicalEvent.Replace(
                "\"eventType\":\"initiative-determined\"",
                "\"eventType\":\"unknown-event\"",
                StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        Assert.Throws<JsonException>(() => ExerciseEvidenceCodec.DeserializeCanonicalEvents(
            Encoding.UTF8.GetBytes($"{changed}\n")));
    }

    [Fact]
    public void CanonicalEventReaderRejectsTheCreationEventExcludedFromAcceptedStepEvidence()
    {
        var record = "{\"contractVersion\":4,\"eventType\":\"campaign-created\","
            + "\"campaignId\":\"campaign-1\",\"stateVersion\":1,"
            + "\"sequencePosition\":{\"positionId\":\"land.position.start\"}}\n";

        Assert.Throws<JsonException>(() => ExerciseEvidenceCodec.DeserializeCanonicalEvents(
            Encoding.UTF8.GetBytes(record)));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        GC.SuppressFinalize(this);
    }

    private string CreateSuccessfulBundle()
    {
        Directory.CreateDirectory(root);
        var manifest = ExerciseManifestCodecTests.Create(buildMode: ExerciseBuildMode.Exploratory);
        var normalizedManifest = ExerciseManifestCodec.Serialize(manifest);
        var execution = ExerciseExecutor.Execute(manifest, TestContext.Current.CancellationToken);
        var readjudication = ReadjudicationVerifier.Verify(manifest, execution);
        var checks = execution.CheckResults.WithReadjudication(readjudication);
        var payloads = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [ArtifactSchema.AcceptedActionsPath] =
                ExerciseEvidenceWriter.WriteAcceptedActions(execution),
            [ArtifactSchema.BuildIdentityPath] = BuildIdentityCodec.Serialize(new BuildIdentity(
                ExerciseBuildMode.Exploratory,
                new string('1', 40),
                new string('2', 40),
                true,
                Sha('3'),
                ".NET 10.0.11",
                "arm64",
                "arm64",
                Cna1979Ruleset.Manifest.Hash,
                ExerciseConfigurationIdentity.ComputeHash(manifest),
                Hash(normalizedManifest),
                ExerciseSeedLedger.SchemeId,
                false,
                false,
                [new BuildArtifactIdentity("runner.dll", 12, Sha('4'))])),
            [ArtifactSchema.CanonicalEventsPath] =
                ExerciseEvidenceWriter.WriteCanonicalEvents(execution),
            [ArtifactSchema.CheckResultsPath] = ExerciseCheckResultsCodec.Serialize(checks),
            [ArtifactSchema.DiagnosticsPath] = [],
            [ArtifactSchema.ExerciseManifestPath] = normalizedManifest,
            [ArtifactSchema.FinalSnapshotPath] = execution.FinalSnapshot,
            [ArtifactSchema.InitialSnapshotPath] = execution.InitialSnapshot,
            [ArtifactSchema.ReadjudicationProofPath] =
                ReplayProofCodec.Serialize(readjudication),
            [ArtifactSchema.ReconstructionProofPath] =
                ReplayProofCodec.Serialize(execution.Reconstruction!),
            [ArtifactSchema.RunResultPath] =
                ExerciseRunResultCodec.Serialize(execution.RunResult),
            [ArtifactSchema.SeedLedgerPath] = SeedLedgerCodec.Serialize(execution.SeedLedger),
            [ArtifactSchema.StepEvidencePath] = ExerciseEvidenceWriter.WriteStepEvidence(execution),
            [ArtifactSchema.SummaryJsonPath] = "{}"u8.ToArray(),
            [ArtifactSchema.SummaryMarkdownPath] = [],
        };
        return ExerciseBundleWriter.Write(
            root,
            new ExerciseBundleWriteRequest(ArtifactBundleProfile.Succeeded, payloads)).Path;
    }

    private string CreateReconstructionFailureBundle(bool fabricated)
    {
        if (fabricated)
        {
            var bundlePath = CreateReconstructionFailureBundle(fabricated: false);
            var successful = ReadPayloads(CreateSuccessfulBundle());
            RewritePayload(
                bundlePath,
                ArtifactSchema.FinalSnapshotPath,
                _ => successful[ArtifactSchema.FinalSnapshotPath]);
            RewritePayload(
                bundlePath,
                ArtifactSchema.StepEvidencePath,
                _ => successful[ArtifactSchema.StepEvidencePath]);
            var successfulProof = ReplayProofCodec.DeserializeReconstruction(
                successful[ArtifactSchema.ReconstructionProofPath]);
            RewritePayload(
                bundlePath,
                ArtifactSchema.ReconstructionProofPath,
                _ => ReplayProofCodec.Serialize(new ReconstructionProof(
                    ExerciseReconstructionFailureReason.SnapshotMismatch,
                    successfulProof.EventStreamHash,
                    successfulProof.ExpectedSnapshotHash,
                    Sha('f'))));
            RehashManifest(bundlePath);
            return bundlePath;
        }

        var payloads = ReadPayloads(CreateSuccessfulBundle());
        payloads.Remove(ArtifactSchema.SummaryJsonPath);
        payloads.Remove(ArtifactSchema.SummaryMarkdownPath);
        payloads.Remove(ArtifactSchema.ReadjudicationProofPath);
        payloads[ArtifactSchema.RunResultPath] = ExerciseRunResultCodec.Serialize(
            ExerciseRunResult.Failed(ExerciseFailureCategory.ReconstructionMismatch, null));
        payloads[ArtifactSchema.CheckResultsPath] = ExerciseCheckResultsCodec.Serialize(
            ReplayFailureChecks(payloads, ExerciseCheckId.HistoryReconstruction));

        var originalProof = ReplayProofCodec.DeserializeReconstruction(
            payloads[ArtifactSchema.ReconstructionProofPath]);
        var originalFinal = payloads[ArtifactSchema.FinalSnapshotPath];
        var changedFinal = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(originalFinal).Replace(
            "\"elementId\":\"axis-element-a\",\"currentLocationId\":\"west\"",
            "\"elementId\":\"axis-element-a\",\"currentLocationId\":\"east\"",
            StringComparison.Ordinal));
        Assert.NotEqual(originalFinal, changedFinal);
        payloads[ArtifactSchema.FinalSnapshotPath] = changedFinal;
        payloads[ArtifactSchema.StepEvidencePath] = Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(payloads[ArtifactSchema.StepEvidencePath]).Replace(
                ReplayEvidenceHasher.HashBytes(originalFinal),
                ReplayEvidenceHasher.HashBytes(changedFinal),
                StringComparison.Ordinal));
        payloads[ArtifactSchema.ReconstructionProofPath] = ReplayProofCodec.Serialize(
            new ReconstructionProof(
                ExerciseReconstructionFailureReason.SnapshotMismatch,
                originalProof.EventStreamHash,
                ReplayEvidenceHasher.HashBytes(changedFinal),
                originalProof.ReconstructedSnapshotHash));

        return ExerciseBundleWriter.Write(
            root,
            new ExerciseBundleWriteRequest(
                ArtifactBundleProfile.FailedReconstructed,
                payloads)).Path;
    }

    private string CreateReadjudicationFailureBundle(bool fabricated)
    {
        if (fabricated)
        {
            var bundlePath = CreateReadjudicationFailureBundle(fabricated: false);
            var successful = ReadPayloads(CreateSuccessfulBundle());
            RewritePayload(
                bundlePath,
                ArtifactSchema.AcceptedActionsPath,
                _ => successful[ArtifactSchema.AcceptedActionsPath]);
            RewritePayload(
                bundlePath,
                ArtifactSchema.StepEvidencePath,
                _ => successful[ArtifactSchema.StepEvidencePath]);
            var successfulProof = ReplayProofCodec.DeserializeReadjudication(
                successful[ArtifactSchema.ReadjudicationProofPath]);
            RewritePayload(
                bundlePath,
                ArtifactSchema.ReadjudicationProofPath,
                _ => ReplayProofCodec.Serialize(new ReadjudicationProof(
                    successfulProof.ExpectedTranscriptHash,
                    Sha('f'),
                    successfulProof.ExpectedEventsHash,
                    successfulProof.ReadjudicatedEventsHash,
                    successfulProof.ExpectedFinalSnapshotHash,
                    successfulProof.ReadjudicatedFinalSnapshotHash)));
            RehashManifest(bundlePath);
            return bundlePath;
        }

        var payloads = ReadPayloads(CreateSuccessfulBundle());
        payloads.Remove(ArtifactSchema.SummaryJsonPath);
        payloads.Remove(ArtifactSchema.SummaryMarkdownPath);
        payloads[ArtifactSchema.RunResultPath] = ExerciseRunResultCodec.Serialize(
            ExerciseRunResult.Failed(ExerciseFailureCategory.ReadjudicationMismatch, null));
        payloads[ArtifactSchema.CheckResultsPath] = ExerciseCheckResultsCodec.Serialize(
            ReplayFailureChecks(payloads, ExerciseCheckId.Readjudication));

        var actions = ExerciseEvidenceCodec.DeserializeAcceptedActions(
            payloads[ArtifactSchema.AcceptedActionsPath]);
        var events = ExerciseEvidenceCodec.DeserializeCanonicalEvents(
            payloads[ArtifactSchema.CanonicalEventsPath]);
        var finalSnapshot = payloads[ArtifactSchema.FinalSnapshotPath];
        var originalActionId = actions[^1].ActionId;
        payloads[ArtifactSchema.AcceptedActionsPath] = Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(payloads[ArtifactSchema.AcceptedActionsPath]).Replace(
                originalActionId,
                Sha('f'),
                StringComparison.Ordinal));
        payloads[ArtifactSchema.StepEvidencePath] = Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(payloads[ArtifactSchema.StepEvidencePath]).Replace(
                originalActionId,
                Sha('f'),
                StringComparison.Ordinal));
        var expectedActions = ExerciseEvidenceCodec.DeserializeAcceptedActions(
            payloads[ArtifactSchema.AcceptedActionsPath]);

        var expectedTranscriptHash = ReplayEvidenceHasher.HashRecords(
            expectedActions.Select(ExerciseEvidenceCodec.SerializeReceipt));
        var expectedEventsHash = ReplayEvidenceHasher.HashRecords(
            events.Select(value => value.CanonicalBytes));
        var expectedFinalHash = ReplayEvidenceHasher.HashBytes(finalSnapshot);
        var prefixCount = actions.Count - 1;
        payloads[ArtifactSchema.ReadjudicationProofPath] = ReplayProofCodec.Serialize(
            new ReadjudicationProof(
                expectedTranscriptHash,
                ReplayEvidenceHasher.HashRecords(actions
                    .Take(prefixCount)
                    .Select(ExerciseEvidenceCodec.SerializeReceipt)),
                expectedEventsHash,
                ReplayEvidenceHasher.HashRecords(events
                    .Take(prefixCount)
                    .Select(value => value.CanonicalBytes)),
                expectedFinalHash,
                ReplayEvidenceHasher.HashBytes([])));

        return ExerciseBundleWriter.Write(
            root,
            new ExerciseBundleWriteRequest(
                ArtifactBundleProfile.FailedReadjudicated,
                payloads)).Path;
    }

    private static ExerciseCheckResults ReplayFailureChecks(
        Dictionary<string, byte[]> payloads,
        ExerciseCheckId failedCheck)
    {
        var successful = ExerciseCheckResultsCodec.Deserialize(
            payloads[ArtifactSchema.CheckResultsPath]);
        var retainedCount = failedCheck == ExerciseCheckId.HistoryReconstruction
            ? successful.Results.Count - 2
            : successful.Results.Count - 1;
        var failureCode = failedCheck == ExerciseCheckId.HistoryReconstruction
            ? ExerciseCheckFailureCode.ReconstructionMismatch
            : ExerciseCheckFailureCode.ReadjudicationMismatch;
        return new ExerciseCheckResults(successful.Results
            .Take(retainedCount)
            .Append(ExerciseCheckResult.Failed(failedCheck, null, null, failureCode)));
    }

    private static Dictionary<string, byte[]> ReadPayloads(string bundlePath)
    {
        var manifest = ArtifactManifestCodec.Deserialize(File.ReadAllBytes(Path.Combine(
            bundlePath,
            ArtifactSchema.ArtifactManifestPath)));
        return manifest.Files.ToDictionary(
            entry => entry.Path,
            entry => File.ReadAllBytes(Path.Combine(bundlePath, entry.Path)),
            StringComparer.Ordinal);
    }

    private string CreateCancelledZeroStepBundle()
    {
        Directory.CreateDirectory(root);
        var manifest = ExerciseManifestCodecTests.Create(buildMode: ExerciseBuildMode.Exploratory);
        var normalizedManifest = ExerciseManifestCodec.Serialize(manifest);
        using var cancellation = new CancellationTokenSource();
        var execution = ExerciseExecutor.Execute(
            manifest,
            new CancelAfterBeginRuntime(cancellation),
            cancellation.Token);
        Assert.Empty(execution.Steps);
        Assert.Equal(ExerciseFailureCategory.Cancelled, execution.FailureCategory);
        var payloads = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [ArtifactSchema.AcceptedActionsPath] = [],
            [ArtifactSchema.BuildIdentityPath] = BuildIdentityCodec.Serialize(BuildIdentityFor(
                manifest,
                normalizedManifest)),
            [ArtifactSchema.CanonicalEventsPath] = [],
            [ArtifactSchema.CheckResultsPath] =
                ExerciseCheckResultsCodec.Serialize(execution.CheckResults),
            [ArtifactSchema.DiagnosticsPath] = [],
            [ArtifactSchema.ExerciseManifestPath] = normalizedManifest,
            [ArtifactSchema.FinalSnapshotPath] = execution.FinalSnapshot,
            [ArtifactSchema.InitialSnapshotPath] = execution.InitialSnapshot,
            [ArtifactSchema.RunResultPath] = ExerciseRunResultCodec.Serialize(execution.RunResult),
            [ArtifactSchema.SeedLedgerPath] = SeedLedgerCodec.Serialize(execution.SeedLedger),
            [ArtifactSchema.StepEvidencePath] = [],
        };
        return ExerciseBundleWriter.Write(
            root,
            new ExerciseBundleWriteRequest(ArtifactBundleProfile.FailedExecuted, payloads)).Path;
    }

    private string CreateFailedAdmittedBundle(
        ExerciseFailureCategory? expectedCategory = null)
    {
        Directory.CreateDirectory(root);
        var manifest = ExerciseManifestCodecTests.Create(
            buildMode: ExerciseBuildMode.Exploratory,
            assertFailureCategory: expectedCategory);
        var payloads = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [ArtifactSchema.CheckResultsPath] =
                ExerciseCheckResultsCodec.Serialize(new ExerciseCheckResults([])),
            [ArtifactSchema.ExerciseManifestPath] = ExerciseManifestCodec.Serialize(manifest),
            [ArtifactSchema.RunResultPath] = ExerciseRunResultCodec.Serialize(
                ExerciseRunResult.Failed(
                    ExerciseFailureCategory.BuildIdentityUnavailable,
                    expectedCategory)),
        };
        return ExerciseBundleWriter.Write(
            root,
            new ExerciseBundleWriteRequest(
                ArtifactBundleProfile.FailedAdmitted,
                payloads)).Path;
    }

    private string CreateFailedIdentifiedBundle(
        ExerciseFailureCategory? expectedCategory = null)
    {
        Directory.CreateDirectory(root);
        var manifest = ExerciseManifestCodecTests.Create(
            buildMode: ExerciseBuildMode.Exploratory,
            assertFailureCategory: expectedCategory);
        var normalizedManifest = ExerciseManifestCodec.Serialize(manifest);
        var payloads = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [ArtifactSchema.BuildIdentityPath] = BuildIdentityCodec.Serialize(BuildIdentityFor(
                manifest,
                normalizedManifest)),
            [ArtifactSchema.CheckResultsPath] =
                ExerciseCheckResultsCodec.Serialize(new ExerciseCheckResults([])),
            [ArtifactSchema.ExerciseManifestPath] = normalizedManifest,
            [ArtifactSchema.RunResultPath] = ExerciseRunResultCodec.Serialize(
                ExerciseRunResult.Failed(
                    ExerciseFailureCategory.UnexpectedFailure,
                    expectedCategory)),
        };
        return ExerciseBundleWriter.Write(
            root,
            new ExerciseBundleWriteRequest(
                ArtifactBundleProfile.FailedIdentified,
                payloads)).Path;
    }

    private static BuildIdentity BuildIdentityFor(
        ExerciseManifest manifest,
        byte[] normalizedManifest) => new(
        ExerciseBuildMode.Exploratory,
        new string('1', 40),
        new string('2', 40),
        true,
        Sha('3'),
        ".NET 10.0.11",
        "arm64",
        "arm64",
        Cna1979Ruleset.Manifest.Hash,
        ExerciseConfigurationIdentity.ComputeHash(manifest),
        Hash(normalizedManifest),
        ExerciseSeedLedger.SchemeId,
        false,
        false,
        [new BuildArtifactIdentity("runner.dll", 12, Sha('4'))]);

    private static BuildIdentity CopyBuildIdentity(
        BuildIdentity value,
        ExerciseBuildMode buildMode,
        string manifestHash,
        string configurationHash,
        bool dirty,
        bool baselineEligible,
        bool reproducible) => new(
        buildMode,
        value.HeadCommit,
        value.HeadTree,
        dirty,
        value.PorcelainSha256,
        value.FrameworkDescription,
        value.OsArchitecture,
        value.ProcessArchitecture,
        value.RulesetHash,
        configurationHash,
        manifestHash,
        value.SeedSchemeId,
        baselineEligible,
        reproducible,
        value.Artifacts);

    private static void RehashManifest(string bundlePath)
    {
        var manifestPath = Path.Combine(bundlePath, ArtifactSchema.ArtifactManifestPath);
        var manifest = ArtifactManifestCodec.Deserialize(File.ReadAllBytes(manifestPath));
        var entries = manifest.Files.Select(entry =>
        {
            var payload = File.ReadAllBytes(Path.Combine(bundlePath, entry.Path));
            return new ArtifactManifestEntry(
                entry.Path,
                entry.SchemaId,
                payload.LongLength,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(payload))}");
        });
        File.WriteAllBytes(
            manifestPath,
            ArtifactManifestCodec.Serialize(new ArtifactManifest(manifest.Profile, entries)));
    }

    private static void RewriteManifest(
        string bundlePath,
        Func<ExerciseManifest, ExerciseManifest> rewrite)
    {
        var manifestPath = Path.Combine(bundlePath, ArtifactSchema.ExerciseManifestPath);
        var manifest = rewrite(ExerciseManifestCodec.Deserialize(File.ReadAllBytes(manifestPath)));
        var normalizedManifest = ExerciseManifestCodec.Serialize(manifest);
        File.WriteAllBytes(manifestPath, normalizedManifest);
        var buildIdentityPath = Path.Combine(bundlePath, ArtifactSchema.BuildIdentityPath);
        if (File.Exists(buildIdentityPath))
            File.WriteAllBytes(
                buildIdentityPath,
                BuildIdentityCodec.Serialize(BuildIdentityFor(manifest, normalizedManifest)));
        RehashManifest(bundlePath);
    }

    private static void RewritePayload(
        string bundlePath,
        string path,
        Func<byte[], byte[]> rewrite)
    {
        var fullPath = Path.Combine(bundlePath, path);
        File.WriteAllBytes(fullPath, rewrite(File.ReadAllBytes(fullPath)));
    }

    private static void RefreshDependentHashes(string bundlePath)
    {
        var events = ExerciseEvidenceCodec.DeserializeCanonicalEvents(File.ReadAllBytes(
            Path.Combine(bundlePath, ArtifactSchema.CanonicalEventsPath)));
        var stepPath = Path.Combine(bundlePath, ArtifactSchema.StepEvidencePath);
        var originalSteps = ExerciseEvidenceCodec.DeserializeStepEvidence(
            File.ReadAllBytes(stepPath));
        var stepJson = Encoding.UTF8.GetString(File.ReadAllBytes(stepPath));
        for (var index = 0; index < originalSteps.Count; index++)
        {
            stepJson = stepJson.Replace(
                originalSteps[index].EventsHash,
                ReplayEvidenceHasher.HashRecords([events[index].CanonicalBytes]),
                StringComparison.Ordinal);
        }
        var finalSnapshot = File.ReadAllBytes(Path.Combine(
            bundlePath,
            ArtifactSchema.FinalSnapshotPath));
        if (originalSteps.Count > 0)
        {
            stepJson = stepJson.Replace(
                originalSteps[^1].SnapshotHash,
                ReplayEvidenceHasher.HashBytes(finalSnapshot),
                StringComparison.Ordinal);
        }
        File.WriteAllText(stepPath, stepJson, new UTF8Encoding(false));

        var actions = ExerciseEvidenceCodec.DeserializeAcceptedActions(File.ReadAllBytes(
            Path.Combine(bundlePath, ArtifactSchema.AcceptedActionsPath)));
        var transcriptHash = ReplayEvidenceHasher.HashRecords(
            actions.Select(ExerciseEvidenceCodec.SerializeReceipt));
        var eventsHash = ReplayEvidenceHasher.HashRecords(
            events.Select(value => value.CanonicalBytes));
        var finalHash = ReplayEvidenceHasher.HashBytes(finalSnapshot);
        RewritePayload(
            bundlePath,
            ArtifactSchema.ReadjudicationProofPath,
            _ => ReplayProofCodec.Serialize(new ReadjudicationProof(
                transcriptHash,
                transcriptHash,
                eventsHash,
                eventsHash,
                finalHash,
                finalHash)));
        RewritePayload(bundlePath, ArtifactSchema.ReconstructionProofPath, bytes =>
        {
            var proof = ReplayProofCodec.DeserializeReconstruction(bytes);
            return ReplayProofCodec.Serialize(new ReconstructionProof(
                proof.FailureReason,
                proof.EventStreamHash,
                finalHash,
                finalHash));
        });
    }

    private static string Hash(byte[] payload) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(payload))}";

    private static ExerciseManifest WithRootSeed(ExerciseManifest value, ulong rootSeed) => new(
        value.ContractVersion,
        value.ExerciseId,
        value.SetupId,
        value.SetupHash,
        value.ContentPackId,
        value.ContentHash,
        value.ScenarioId,
        value.RulesetHash,
        value.TerminalBoundary,
        value.MaximumSteps,
        rootSeed,
        value.BuildMode,
        value.Confidentiality,
        value.Detail,
        value.Controllers,
        value.AssertFailureCategory);

    private static ExerciseManifest WithMaximumSteps(
        ExerciseManifest value,
        int maximumSteps) => new(
        value.ContractVersion,
        value.ExerciseId,
        value.SetupId,
        value.SetupHash,
        value.ContentPackId,
        value.ContentHash,
        value.ScenarioId,
        value.RulesetHash,
        value.TerminalBoundary,
        maximumSteps,
        value.RootSeed,
        value.BuildMode,
        value.Confidentiality,
        value.Detail,
        value.Controllers,
        value.AssertFailureCategory);

    private static ExerciseManifest WithFailureAssertion(
        ExerciseManifest value,
        ExerciseFailureCategory? assertion) => new(
        value.ContractVersion,
        value.ExerciseId,
        value.SetupId,
        value.SetupHash,
        value.ContentPackId,
        value.ContentHash,
        value.ScenarioId,
        value.RulesetHash,
        value.TerminalBoundary,
        value.MaximumSteps,
        value.RootSeed,
        value.BuildMode,
        value.Confidentiality,
        value.Detail,
        value.Controllers,
        assertion);

    private static string Sha(char value) => $"sha256:{new string(value, 64)}";

    private sealed class CancelAfterBeginRuntime(CancellationTokenSource cancellation)
        : IExerciseExecutionRuntime
    {
        public ExerciseStartResult Begin(CampaignCreationRequest request)
        {
            var result = CoreExerciseExecutionRuntime.Instance.Begin(request);
            cancellation.Cancel();
            return result;
        }

        public ExerciseCheckpoint QueryCheckpoint(ExerciseSession session) =>
            CoreExerciseExecutionRuntime.Instance.QueryCheckpoint(session);

        public ExerciseRuntimeQueryResult Query(
            ExerciseSession session,
            CampaignActionAudience audience) =>
            CoreExerciseExecutionRuntime.Instance.Query(session, audience);

        public ExerciseControllerSelection Select(
            ExerciseControllerManifest policies,
            IReadOnlyList<ExerciseControllerActionSet> actionSets) =>
            CoreExerciseExecutionRuntime.Instance.Select(policies, actionSets);

        public ExerciseRuntimeStepResult Submit(
            ExerciseSession session,
            CampaignActionSubmission submission) =>
            CoreExerciseExecutionRuntime.Instance.Submit(session, submission);

        public ReconstructionProof Reconstruct(ExerciseSession session) =>
            CoreExerciseExecutionRuntime.Instance.Reconstruct(session);
    }
}
