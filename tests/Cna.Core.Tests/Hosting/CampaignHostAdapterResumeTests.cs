using System.Security.Cryptography;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Hosting;

/// <summary>
/// Proves <see cref="CampaignHostAdapter.ImportResumed"/> (Phase B of #439 — the
/// snapshotting design doc's §5.3 <c>CampaignHostAdapter</c> changes): resuming from an
/// already-constructed seed <see cref="ExerciseSession"/> plus only the tail steps must
/// reach exactly the same canonical state as a from-genesis replay of the complete step
/// list, and every tail step must still be admitted through the same switch a from-genesis
/// replay uses (so tampering is still caught identically).
/// </summary>
public sealed class CampaignHostAdapterResumeTests
{
    [Fact]
    public void ResumedTailReplayMatchesFullGenesisReplayAtCleanBoundary()
    {
        var request = Request();
        var start = CampaignHostAdapter.Create(request);
        var session = start.Session!;
        var recorded = new List<CampaignHostAcceptedStep>();

        // None of these steps opens a Combat round, so every intermediate session in this
        // walk is already a "clean boundary" per the snapshotting design doc's §2 (all 33
        // optional ExerciseSession fields and the 8 predecessor-chain fields are null) —
        // any prefix cut is therefore a valid seed boundary for this fixture.
        for (var index = 0; index < 5; index++)
        {
            var (command, accepted) = AdvanceOneStep(session);
            recorded.Add(Step(command, accepted.Evidence!));
            session = accepted.SuccessorSession!;
        }

        const int seedCount = 2;
        var seedSteps = recorded.Take(seedCount).ToArray();
        var tailSteps = recorded.Skip(seedCount).ToArray();

        var seedImport = CampaignHostAdapter.Import(request, start.CreationEventBytes!,
            start.InitialSnapshotBytes!, seedSteps);
        Assert.True(seedImport.IsVerified);

        var resumed = CampaignHostAdapter.ImportResumed(seedImport.Session!, tailSteps);
        Assert.True(resumed.IsVerified);

        var fullImport = CampaignHostAdapter.Import(request, start.CreationEventBytes!,
            start.InitialSnapshotBytes!, recorded);
        Assert.True(fullImport.IsVerified);

        var fullBytes = CampaignCurrentSnapshotSerializer.Serialize(fullImport.Session!.CurrentSnapshot);
        var resumedBytes = CampaignCurrentSnapshotSerializer.Serialize(resumed.Session!.CurrentSnapshot);
        Assert.Equal(fullBytes, resumedBytes);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(fullBytes)),
            Convert.ToHexStringLower(SHA256.HashData(resumedBytes)));
        Assert.Equal(
            CampaignHostAdapter.QueryContactRecords(fullImport.Session!).Select(Convert.ToHexString),
            CampaignHostAdapter.QueryContactRecords(resumed.Session!).Select(Convert.ToHexString));
        Assert.Equal(CampaignExercises.QueryCheckpoint(fullImport.Session!).StateVersion,
            CampaignExercises.QueryCheckpoint(resumed.Session!).StateVersion);
        Assert.Equal(CampaignExercises.QueryCheckpoint(fullImport.Session!).PositionId,
            CampaignExercises.QueryCheckpoint(resumed.Session!).PositionId);
    }

    [Fact]
    public void ResumedImportRejectsTamperedTailStepExactlyLikeFreshReplay()
    {
        var request = Request();
        var start = CampaignHostAdapter.Create(request);
        var session = start.Session!;
        var recorded = new List<CampaignHostAcceptedStep>();
        for (var index = 0; index < 3; index++)
        {
            var (command, accepted) = AdvanceOneStep(session);
            recorded.Add(Step(command, accepted.Evidence!));
            session = accepted.SuccessorSession!;
        }

        var seedImport = CampaignHostAdapter.Import(request, start.CreationEventBytes!,
            start.InitialSnapshotBytes!, recorded.Take(1).ToArray());
        Assert.True(seedImport.IsVerified);

        var goodTail = recorded.Skip(1).ToArray();
        Assert.True(CampaignHostAdapter.ImportResumed(seedImport.Session!, goodTail).IsVerified);

        var tamperedStep = goodTail[0];
        var tamperedCommand = CampaignActionSubmissionSerializer.DeserializeCanonical(
            tamperedStep.CanonicalCommand);
        var tamperedTail = new[]
        {
            new CampaignHostAcceptedStep(
                CampaignActionSubmissionSerializer.SerializeCanonical(
                    tamperedCommand with { ExpectedStateVersion = tamperedCommand.ExpectedStateVersion + 1 }),
                tamperedStep.EventRecords, tamperedStep.CanonicalReceipt, tamperedStep.SnapshotCheckpoint),
            goodTail[1],
        };

        Assert.Equal(CampaignHostImportRejectionReason.InvalidStep,
            CampaignHostAdapter.ImportResumed(seedImport.Session!, tamperedTail).RejectionReason);
    }

    [Fact]
    public void ResumedImportValidatesContactRecordsLikeFreshReplayAndRejectsTampering()
    {
        var request = Request();
        var start = CampaignHostAdapter.Create(request);
        var session = start.Session!;
        var recorded = new List<CampaignHostAcceptedStep>();
        var contactsAfterEachStep = new List<IReadOnlyList<byte[]>>
        {
            CampaignHostAdapter.QueryContactRecords(session),
        };
        for (var index = 0; index < 4; index++)
        {
            var (command, accepted) = AdvanceOneStep(session);
            recorded.Add(Step(command, accepted.Evidence!));
            session = accepted.SuccessorSession!;
            contactsAfterEachStep.Add(CampaignHostAdapter.QueryContactRecords(session));
        }

        const int seedCount = 2;
        var seedImport = CampaignHostAdapter.Import(request, start.CreationEventBytes!,
            start.InitialSnapshotBytes!, recorded.Take(seedCount).ToArray());
        Assert.True(seedImport.IsVerified);
        var seedContacts = contactsAfterEachStep[seedCount];
        Assert.Equal(seedContacts.Select(Convert.ToHexString),
            CampaignHostAdapter.QueryContactRecords(seedImport.Session!).Select(Convert.ToHexString));

        var tail = recorded.Skip(seedCount).ToArray();
        var stepContacts = Enumerable.Range(0, tail.Length)
            .Select(offset => (IReadOnlyList<byte[]>)contactsAfterEachStep[seedCount + offset + 1]
                .Skip(contactsAfterEachStep[seedCount + offset].Count).ToArray())
            .ToArray();

        Assert.True(CampaignHostAdapter.ImportResumed(seedImport.Session!, tail,
            seedContacts, stepContacts).IsVerified);
        Assert.Equal(CampaignHostImportRejectionReason.InvalidCreation,
            CampaignHostAdapter.ImportResumed(seedImport.Session!, tail,
                [.. seedContacts, new byte[] { 1 }], stepContacts).RejectionReason);
        Assert.Equal(CampaignHostImportRejectionReason.InvalidStep,
            CampaignHostAdapter.ImportResumed(seedImport.Session!, tail, seedContacts,
                [[.. stepContacts[0], new byte[] { 1 }], .. stepContacts.Skip(1)]).RejectionReason);
    }

    private static (CampaignActionSubmission Command, ExerciseStepResult Accepted) AdvanceOneStep(
        ExerciseSession session)
    {
        var set = Enum.GetValues<CampaignActionAudience>()
            .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
            .First(value => value.Candidates.Count > 0);
        var candidate = set.Candidates[0];
        var command = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
            set.PositionId, set.Audience, candidate.ActionId);
        var accepted = CampaignHostAdapter.Submit(session, command);
        Assert.True(accepted.IsAccepted);
        return (command, accepted);
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
        return new CampaignCreationRequest(1, "host-rsh-002", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId);
    }
}
