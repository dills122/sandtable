using System.Security.Cryptography;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Hosting;

public sealed class CampaignHostAdapterTests
{
    [Fact]
    public void TwelveStepHostTraceMatchesDirectCoreAtEveryCut()
    {
        var request = Request();
        var start = CampaignHostAdapter.Create(request);
        var hosted = start.Session!;
        var direct = CampaignExercises.Begin(request).Session!;
        var authority = CampaignAuthority.Create(request).Handle!;
        var recorded = new List<CampaignHostAcceptedStep>();
        var expectedKinds = new[]
        {
            "resolve-initiative", "resolve-no-obligation-naval-convoy-schedule",
            "resolve-no-obligation-tactical-shipping", "act-first", "resolve-weather",
            "resolve-no-obligation-organization", "resolve-no-obligation-naval-convoy-arrival",
            "resolve-no-obligation-fleet-assignment", "resolve-no-obligation-fleet-repair",
            "complete-reserve-designation", "complete-movement-segment",
            "complete-breakdown-segment",
        };

        for (var index = 0; index <= expectedKinds.Length; index++)
        {
            foreach (var audience in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
            {
                var cut = CampaignHostAdapter.QueryAudience(hosted, audience);
                var directCut = CampaignHostAdapter.QueryAudience(direct, audience);
                Assert.Equal(CampaignObservationV7Serializer.SerializeCanonical(directCut.Observation!),
                    CampaignObservationV7Serializer.SerializeCanonical(cut.Observation!));
                Assert.Equal(CampaignLegalActionSerializer.Serialize(directCut.LegalActions!),
                    CampaignLegalActionSerializer.Serialize(cut.LegalActions!));
                var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
                Assert.Equal(CampaignObservationV7Serializer.SerializeCanonical(
                        CampaignObservations.Query(authority, side).Observation!),
                    CampaignObservationV7Serializer.SerializeCanonical(cut.Observation!));
                Assert.Equal(CampaignLegalActionSerializer.Serialize(
                        CampaignLegalActions.Query(authority, audience).ActionSet!),
                    CampaignLegalActionSerializer.Serialize(cut.LegalActions!));
            }
            Assert.Equal(CampaignLegalActionSerializer.Serialize(
                    CampaignExercises.Query(direct, CampaignActionAudience.System).ActionSet!),
                CampaignLegalActionSerializer.Serialize(
                    CampaignHostAdapter.QuerySystem(hosted).ActionSet!));
            Assert.Equal(CampaignLegalActionSerializer.Serialize(
                    CampaignLegalActions.Query(authority, CampaignActionAudience.System).ActionSet!),
                CampaignLegalActionSerializer.Serialize(
                    CampaignHostAdapter.QuerySystem(hosted).ActionSet!));
            if (index == expectedKinds.Length) break;

            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(hosted, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            var candidate = set.Candidates.FirstOrDefault(value => value.Kind == "act-first")
                ?? set.Candidates.FirstOrDefault(value => value.Kind == "complete-reserve-designation")
                ?? set.Candidates.FirstOrDefault(value => value.Kind == "complete-movement-segment")
                ?? set.Candidates[0];
            Assert.Equal(expectedKinds[index], candidate.Kind);
            var command = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
                set.PositionId, set.Audience, candidate.ActionId);
            var hostStep = CampaignHostAdapter.Submit(hosted, command);
            var directStep = CampaignExercises.Submit(direct, command);
            var authorityStep = CampaignLegalActions.Submit(authority, command);
            Assert.True(hostStep.IsAccepted);
            Assert.True(directStep.IsAccepted);
            Assert.True(authorityStep.IsAccepted);
            Assert.Equal(directStep.Evidence!.EventRecords.Single(),
                hostStep.Evidence!.EventRecords.Single());
            Assert.Equal(CampaignActionAcceptanceReceiptSerializer.Serialize(directStep.Evidence.Receipt),
                CampaignActionAcceptanceReceiptSerializer.Serialize(hostStep.Evidence.Receipt));
            Assert.Equal(directStep.Evidence.SnapshotCheckpoint,
                hostStep.Evidence.SnapshotCheckpoint);
            Assert.Equal(CampaignCurrentSnapshotSerializer.Serialize(
                    authorityStep.SuccessorHandle!.CurrentSnapshot!),
                hostStep.Evidence.SnapshotCheckpoint);
            Assert.Equal(CampaignActionAcceptanceReceiptSerializer.Serialize(
                    authorityStep.Receipt!),
                CampaignActionAcceptanceReceiptSerializer.Serialize(hostStep.Evidence.Receipt));
            recorded.Add(Step(command, hostStep.Evidence));
            hosted = hostStep.SuccessorSession!;
            direct = directStep.SuccessorSession!;
            authority = authorityStep.SuccessorHandle;
        }

        Assert.True(CampaignHostAdapter.Import(request, start.CreationEventBytes!,
            start.InitialSnapshotBytes!, recorded).IsVerified);
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.combat.position-determination",
            CampaignExercises.QueryCheckpoint(hosted).PositionId);
        Assert.Equal(13, CampaignExercises.QueryCheckpoint(hosted).StateVersion);
        Assert.Equal("222b36760b250ab6ea8eb14a7913ffb56bab1b1786cb7a9d9b1f720360ca3c84",
            Convert.ToHexStringLower(SHA256.HashData(
                CampaignCurrentSnapshotSerializer.Serialize(hosted.CurrentSnapshot))));
    }

    [Fact]
    public void CreationAndEachAudienceCutMatchDirectCore()
    {
        var request = Request();
        var hosted = CampaignHostAdapter.Create(request);
        var direct = CampaignExercises.Begin(request);
        Assert.True(hosted.IsStarted);
        Assert.Equal(direct.CreationEventBytes, hosted.CreationEventBytes);
        Assert.Equal(direct.InitialSnapshotBytes, hosted.InitialSnapshotBytes);
        var directHandle = CampaignAuthority.Create(request).Handle!;

        foreach (var audience in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
        {
            var cut = CampaignHostAdapter.QueryAudience(hosted.Session!, audience);
            var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
            Assert.True(cut.IsProjected);
            Assert.Equal(CampaignObservationV7Serializer.SerializeCanonical(
                    CampaignObservations.Query(directHandle, side).Observation!),
                CampaignObservationV7Serializer.SerializeCanonical(cut.Observation!));
            Assert.Equal(CampaignLegalActionSerializer.Serialize(
                    CampaignLegalActions.Query(directHandle, audience).ActionSet!),
                CampaignLegalActionSerializer.Serialize(cut.LegalActions!));
        }

        Assert.Equal(CampaignHostQueryRejectionReason.InvalidAudience,
            CampaignHostAdapter.QueryAudience(hosted.Session!, CampaignActionAudience.System)
                .RejectionReason);
        Assert.Equal(CampaignLegalActionSerializer.Serialize(
                CampaignExercises.Query(direct.Session!, CampaignActionAudience.System).ActionSet!),
            CampaignLegalActionSerializer.Serialize(
                CampaignHostAdapter.QuerySystem(hosted.Session!).ActionSet!));
    }

    [Fact]
    public void AcceptedStepAndImportMatchDirectExerciseAndRejectTampering()
    {
        var request = Request();
        var started = CampaignHostAdapter.Create(request);
        var set = CampaignHostAdapter.QuerySystem(started.Session!).ActionSet!;
        var command = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
            set.PositionId, set.Audience, Assert.Single(set.Candidates).ActionId);
        var hosted = CampaignHostAdapter.Submit(started.Session!, command);
        var direct = CampaignExercises.Submit(CampaignExercises.Begin(request).Session!, command);
        Assert.True(hosted.IsAccepted);
        Assert.Equal(direct.Evidence!.EventRecords.Single(), hosted.Evidence!.EventRecords.Single());
        Assert.Equal(direct.Evidence.SnapshotCheckpoint, hosted.Evidence.SnapshotCheckpoint);
        Assert.Equal(direct.Evidence.Receipt, hosted.Evidence.Receipt);

        var step = Step(command, hosted.Evidence);
        Assert.Equal(CampaignHostAcceptedStep.CurrentContractVersion, step.ContractVersion);
        var restored = CampaignHostAdapter.Import(request, started.CreationEventBytes!,
            started.InitialSnapshotBytes!, [step]);
        Assert.True(restored.IsVerified);
        Assert.Equal(hosted.Evidence.SnapshotCheckpoint,
            CampaignCurrentSnapshotSerializer.Serialize(restored.Session!.CurrentSnapshot));
        var initialContacts = CampaignHostAdapter.QueryContactRecords(started.Session!);
        var stepContacts = CampaignHostAdapter.QueryContactRecords(hosted.SuccessorSession!)
            .Skip(initialContacts.Count).ToArray();
        Assert.True(CampaignHostAdapter.Import(request, started.CreationEventBytes!,
            started.InitialSnapshotBytes!, [step], initialContacts, [stepContacts]).IsVerified);
        Assert.Equal(CampaignHostImportRejectionReason.InvalidCreation,
            CampaignHostAdapter.Import(request, started.CreationEventBytes!,
                started.InitialSnapshotBytes!, [step], [.. initialContacts, new byte[] { 1 }],
                [stepContacts]).RejectionReason);
        Assert.Equal(CampaignHostImportRejectionReason.InvalidStep,
            CampaignHostAdapter.Import(request, started.CreationEventBytes!,
                started.InitialSnapshotBytes!, [step], initialContacts,
                [[.. stepContacts, new byte[] { 1 }]]).RejectionReason);

        Assert.Equal(CampaignHostImportRejectionReason.InvalidCreation,
            CampaignHostAdapter.Import(request, new byte[] { 0 }, started.InitialSnapshotBytes!, [step])
                .RejectionReason);
        Assert.Equal(CampaignHostImportRejectionReason.InvalidStep,
            CampaignHostAdapter.Import(request, started.CreationEventBytes!,
                started.InitialSnapshotBytes!, [new CampaignHostAcceptedStep(
                    step.CanonicalCommand, [[0]], step.CanonicalReceipt,
                    step.SnapshotCheckpoint)]).RejectionReason);
        Assert.Equal(CampaignHostImportRejectionReason.InvalidStep,
            CampaignHostAdapter.Import(request, started.CreationEventBytes!,
                started.InitialSnapshotBytes!, [new CampaignHostAcceptedStep(
                    step.CanonicalCommand, step.EventRecords, [0],
                    step.SnapshotCheckpoint)]).RejectionReason);
        Assert.Equal(CampaignHostImportRejectionReason.InvalidStep,
            CampaignHostAdapter.Import(request, started.CreationEventBytes!,
                started.InitialSnapshotBytes!, [new CampaignHostAcceptedStep(
                    step.CanonicalCommand, step.EventRecords, step.CanonicalReceipt,
                    [0])]).RejectionReason);
        Assert.Equal(CampaignHostImportRejectionReason.InvalidStep,
            CampaignHostAdapter.Import(request, started.CreationEventBytes!,
                started.InitialSnapshotBytes!, [new CampaignHostAcceptedStep(
                    CampaignActionSubmissionSerializer.SerializeCanonical(command with
                    {
                        Audience = CampaignActionAudience.Axis,
                    }), step.EventRecords, step.CanonicalReceipt,
                    step.SnapshotCheckpoint)]).RejectionReason);
    }

    [Fact]
    public void RejectedStepLeavesAllCutsAndCheckpointUnchanged()
    {
        var session = CampaignHostAdapter.Create(Request()).Session!;
        var before = CampaignExercises.QueryCheckpoint(session);
        var axis = CampaignHostAdapter.QueryAudience(session, CampaignActionAudience.Axis);
        var commonwealth = CampaignHostAdapter.QueryAudience(session, CampaignActionAudience.Commonwealth);
        var system = CampaignHostAdapter.QuerySystem(session).ActionSet!;
        var candidateId = Assert.Single(system.Candidates).ActionId;
        var wrongAudience = new CampaignActionSubmission(1, system.CampaignId,
            system.StateVersion, system.PositionId, CampaignActionAudience.Axis, candidateId);
        var attempts = new[]
        {
            wrongAudience,
            wrongAudience with { CampaignId = "another-campaign" },
            wrongAudience with { ExpectedStateVersion = system.StateVersion + 1 },
            wrongAudience with { ExpectedPositionId = "another-position" },
        };
        foreach (var attempt in attempts)
        {
            var rejected = CampaignHostAdapter.Submit(session, attempt);
            Assert.False(rejected.IsAccepted);
            Assert.Null(rejected.SuccessorSession);
            Assert.Null(rejected.Evidence);
        }
        Assert.Equal(before.StateVersion, CampaignExercises.QueryCheckpoint(session).StateVersion);
        Assert.Equal(CampaignObservationV7Serializer.SerializeCanonical(axis.Observation!),
            CampaignObservationV7Serializer.SerializeCanonical(
                CampaignHostAdapter.QueryAudience(session, CampaignActionAudience.Axis).Observation!));
        Assert.Equal(CampaignObservationV7Serializer.SerializeCanonical(commonwealth.Observation!),
            CampaignObservationV7Serializer.SerializeCanonical(
                CampaignHostAdapter.QueryAudience(session, CampaignActionAudience.Commonwealth).Observation!));
        Assert.Equal(CampaignLegalActionSerializer.Serialize(axis.LegalActions!),
            CampaignLegalActionSerializer.Serialize(
                CampaignHostAdapter.QueryAudience(session, CampaignActionAudience.Axis).LegalActions!));
        Assert.Equal(CampaignLegalActionSerializer.Serialize(commonwealth.LegalActions!),
            CampaignLegalActionSerializer.Serialize(
                CampaignHostAdapter.QueryAudience(session, CampaignActionAudience.Commonwealth).LegalActions!));
        Assert.Equal(CampaignLegalActionSerializer.Serialize(system),
            CampaignLegalActionSerializer.Serialize(CampaignHostAdapter.QuerySystem(session).ActionSet!));
    }

    private static CampaignHostAcceptedStep Step(CampaignActionSubmission command,
        ExerciseStepEvidence evidence) => new(
        CampaignActionSubmissionSerializer.SerializeCanonical(command), evidence.EventRecords,
        CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt),
        evidence.SnapshotCheckpoint);

    private static CampaignCreationRequest Request()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
                value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        return new CampaignCreationRequest(1, "host-rsh-001", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId);
    }
}
