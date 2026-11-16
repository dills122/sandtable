using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiHorseSetupTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RealSetupOwnerChoicesArePrivateReversibleAndReplayable(bool axisFirst)
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "horse-source-setup", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId,
            CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, initialization),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var session = CampaignExercises.Begin(request, artifact, initialization).Session!;
        var roll = Assert.Single(CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates);
        session = CampaignExercises.Submit(session, Submission(session, roll.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var finish = CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis).PrivateActionIds.Single();
        session = CampaignExercises.FinishPregameAxisSupply(session, Submission(session, finish, CampaignActionAudience.Axis)).SuccessorSession!;
        var original = session.SourceAuthority!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(original.SerializeLegacyCheckpoint()), original.DigestLegacyCheckpoint());
        Assert.False(original.HorseStageOneResolved);
        var initialSet = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!;
        var oldChoice = initialSet.Candidates.OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization").ChoiceJson;
        Assert.Equal("{\"kind\":\"finish\"}", oldChoice);
        Assert.Equal(initialSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(initialSet)));
        Assert.Equal("awaiting-independent-approvals", ClassificationStatus(session, CampaignActionAudience.Axis));
        var axisBefore = CampaignExercises.QuerySourceActionsDraft(session, CampaignActionAudience.Axis).VisibleBytes;
        var cwBefore = CampaignExercises.QuerySourceActionsDraft(session, CampaignActionAudience.Commonwealth).VisibleBytes;
        var first = axisFirst ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var second = axisFirst ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        session = Act(session, first, "central-india-horse-ruling", "approve");
        Assert.False(session.SourceAuthority!.HorseStageOneResolved);
        Assert.Equal("awaiting-independent-approvals", ClassificationStatus(session, second));
        Assert.Equal(axisFirst ? cwBefore : axisBefore, CampaignExercises.QuerySourceActionsDraft(session, second).VisibleBytes);
        Assert.Equal(CampaignSourceAuthorityState.HorseRulingCheckpointContractVersion, CampaignExercises.QueryCheckpoint(session).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        session = Act(session, second, "central-india-horse-ruling", "approve");
        Assert.Equal("agreed-provisional", ClassificationStatus(session, CampaignActionAudience.Axis));
        Assert.Equal("agreed-provisional", ClassificationStatus(session, CampaignActionAudience.Commonwealth));
        Assert.False(session.SourceAuthority!.HorseStageOneResolved);
        var axisDump = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        var unrelated = CampaignExercises.PlaceSourceAxisDump(session, Submission(session, axisDump, CampaignActionAudience.Axis));
        Assert.True(unrelated.IsAccepted);
        session = unrelated.SuccessorSession!;
        Assert.Equal(3, session.SourceAuthority!.HorseRulingVotes);
        session = Act(session, CampaignActionAudience.Commonwealth, "central-india-horse-stage-one", "decline-stage1");
        Assert.True(session.SourceAuthority!.HorseStageOneResolved);
        Assert.Equal(3, session.SourceAuthority.HorseRulingVotes);
        Assert.Equal(CampaignPregameSourceCreation.Digest(session.SourceAuthority.SerializeLegacyCheckpoint()),
            session.SourceAuthority.DigestLegacyCheckpoint());
        var axisDummy = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-1", "c4407");
        var axisDummyResult = CampaignExercises.DecideSourceDummy(session, Submission(session, axisDummy, CampaignActionAudience.Axis));
        Assert.True(axisDummyResult.IsAccepted);
        session = axisDummyResult.SuccessorSession!;
        Assert.True(session.SourceAuthority!.HorseStageOneResolved);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        var cwDummy = CampaignSourceAuthorityState.DummyActionId(false, "cw-dummy-1", "d3611");
        var cwDummyResult = CampaignExercises.DecideSourceDummy(session, Submission(session, cwDummy, CampaignActionAudience.Commonwealth));
        Assert.True(cwDummyResult.IsAccepted);
        session = cwDummyResult.SuccessorSession!;
        Assert.True(session.SourceAuthority!.HorseStageOneResolved);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        var axisMotor = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", 1);
        var motorResult = CampaignExercises.AdjustSourceMotorization(session, Submission(session, axisMotor, CampaignActionAudience.Axis));
        Assert.True(motorResult.IsAccepted);
        session = motorResult.SuccessorSession!;
        Assert.True(session.SourceAuthority!.HorseStageOneResolved);
        Assert.Equal(3, session.SourceAuthority.HorseRulingVotes);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        using var checkpoint = JsonDocument.Parse(session.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(JsonValueKind.Null, checkpoint.RootElement.GetProperty("priorTrainingProgress").ValueKind);
        Assert.Equal(JsonValueKind.Null, checkpoint.RootElement.GetProperty("priorTrainingActive").ValueKind);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        session = Act(session, CampaignActionAudience.Axis, "owner-finalization", "finish");
        Assert.True(session.SourceAuthority!.HorseStageOneResolved);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        session = Act(session, CampaignActionAudience.Axis, "owner-finalization", "reopen");
        Assert.True(session.SourceAuthority!.HorseStageOneResolved);
        var axisAfterAgreement = CampaignExercises.QuerySourceActionsDraft(session, CampaignActionAudience.Axis).VisibleBytes;
        session = Act(session, CampaignActionAudience.Commonwealth, "central-india-horse-stage-one", "reopen-stage1");
        Assert.False(session.SourceAuthority!.HorseStageOneResolved);
        Assert.Equal(axisAfterAgreement, CampaignExercises.QuerySourceActionsDraft(session, CampaignActionAudience.Axis).VisibleBytes);
        session = Act(session, first, "central-india-horse-ruling", "withdraw");
        Assert.False(session.SourceAuthority!.HorseStageOneResolved);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        Assert.Equal(0, original.HorseRulingVotes);
        Assert.False(original.HorseRulingVersion);
    }

    private static string? ClassificationStatus(ExerciseSession session, CampaignActionAudience audience)
    {
        using var document = JsonDocument.Parse(CampaignExercises.QuerySourceHorseSetupDraft(session, audience, false).VisibleBytes);
        return document.RootElement.GetProperty("classificationStatus").GetString();
    }

    private static ExerciseSession Act(ExerciseSession session, CampaignActionAudience audience, string family, string kind)
    {
        var candidate = CampaignExercises.Query(session, audience).ActionSet!.Candidates.OfType<CampaignSourceSetupAction>()
            .Single(value => value.Family == family && JsonDocument.Parse(value.ChoiceJson).RootElement.GetProperty("kind").GetString() == kind);
        var result = CampaignExercises.Submit(session, Submission(session, candidate.ActionId, audience));
        Assert.True(result.IsAccepted);
        return result.SuccessorSession!;
    }

    private static CampaignActionSubmission Submission(ExerciseSession session, string actionId, CampaignActionAudience audience)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(CampaignActionSubmission.CurrentContractVersion, checkpoint.CampaignId,
            checkpoint.StateVersion, checkpoint.PositionId, audience, actionId);
    }
}
