using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Exercises;

public static class CampaignExercises
{
    public static ExerciseStepResult SubmitSourceOwnerFinalization(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority || session.SourceTurn is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion
            || submission.Audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var finish = !authority.IsOwnerFinalized(submission.Audience);
        if (submission.ActionId != CampaignSourceAuthorityState.SetupFinalizationActionId(submission.Audience, finish))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.SetOwnerFinalization(submission.Audience, finish);
        var bytes = authority.SerializeSetupFinalizationEvent(next, submission.Audience, finish);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame,
            session.CurrentContacts.AdvancePregameSource(authority.StateVersion), [.. session.PregameHistory, bytes], next),
            new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static CampaignPregameAxisDraft QuerySourceHorseSetupDraft(ExerciseSession session, CampaignActionAudience audience, bool stageOne)
    {
        ArgumentNullException.ThrowIfNull(session);
        var authority = session.SourceAuthority ?? throw new InvalidOperationException("Source setup authority is unavailable.");
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth)
            || stageOne && audience != CampaignActionAudience.Commonwealth)
            throw new InvalidOperationException("Horse setup owner is unavailable.");
        var kind = stageOne ? authority.HorseStageOneNoTraining ? "reopen-stage1" : "decline-stage1"
            : authority.HasHorseRulingVote(audience) ? "withdraw" : "approve";
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("rulingId", CampaignSourceAuthorityState.HorseRulingId);
            writer.WriteString("classification", "provisional-oa-note-c-untrained-motorized-infantry");
            writer.WriteString("classificationStatus", authority.HorseRulingVotes == 3 ? "agreed-provisional" : "awaiting-independent-approvals");
            writer.WriteBoolean("ownApproval", authority.HasHorseRulingVote(audience));
            writer.WriteNull("priorTrainingProgress"); writer.WriteNull("priorTrainingActive");
            if (stageOne) { writer.WriteBoolean("noTrainingInGt1Stage1", authority.HorseStageOneNoTraining); writer.WriteString("priorProgressEffect", "unknown-not-reset-by-this-choice"); }
            writer.WriteStartArray("choices"); writer.WriteStartObject(); writer.WriteString("kind", kind);
            writer.WriteString("rulingId", CampaignSourceAuthorityState.HorseRulingId); writer.WriteEndObject(); writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(),
            [CampaignSourceAuthorityState.HorseSetupActionId(audience, kind)]);
    }

    public static ExerciseStepResult SubmitSourceHorseSetup(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority || session.SourceTurn is not null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion
            || submission.Audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var kinds = submission.Audience == CampaignActionAudience.Commonwealth
            ? new[] { "approve", "withdraw", "decline-stage1", "reopen-stage1" } : new[] { "approve", "withdraw" };
        var kind = kinds.SingleOrDefault(value => CampaignSourceAuthorityState.HorseSetupActionId(submission.Audience, value) == submission.ActionId);
        if (kind is null || authority.IsOwnerFinalized(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        CampaignSourceAuthorityState next;
        try { next = authority.SetHorseSetupDecision(submission.Audience, kind); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal); }
        var bytes = authority.SerializeHorseSetupEvent(next, submission.Audience, kind);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame,
            session.CurrentContacts.AdvancePregameSource(authority.StateVersion), [.. session.PregameHistory, bytes], next),
            new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static CampaignPregameAxisDraft QuerySourceGiarabubMotorMgRulingDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var authority = session.SourceAuthority ?? throw new InvalidOperationException("Source setup authority is unavailable.");
        if (audience != CampaignActionAudience.Axis || !authority.CanAcceptGiarabubMotorMgRuling)
            throw new InvalidOperationException("Motor MG ruling owner is unavailable.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("rulingId", CampaignSourceCwFreeGunsWorld.MotorMachineGunRulingId);
            writer.WriteString("finding", "no-scenario-motor-pool-or-effective-allowance");
            writer.WriteString("printedCapabilityPoints", "8+"); writer.WriteNull("effectiveCapabilityPoints");
            writer.WriteNull("mobilityId"); writer.WriteNull("motorPoolId"); writer.WriteNull("assignedMotorPoints");
            writer.WriteBoolean("accepted", authority.CwFreeGunsWorld!.MotorMachineGunRulingAccepted);
            writer.WriteStartArray("choices"); writer.WriteStartObject(); writer.WriteString("kind", "approve");
            writer.WriteString("rulingId", CampaignSourceCwFreeGunsWorld.MotorMachineGunRulingId); writer.WriteEndObject();
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(),
            [CampaignSourceAuthorityState.GiarabubMotorMgRulingActionId]);
    }

    public static ExerciseStepResult SubmitSourceGiarabubMotorMgRuling(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority || session.SourceTurn is not null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || submission.Audience != CampaignActionAudience.Axis)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (submission.ActionId != CampaignSourceAuthorityState.GiarabubMotorMgRulingActionId || !authority.CanAcceptGiarabubMotorMgRuling)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        CampaignSourceAuthorityState next;
        try { next = authority.AcceptGiarabubMotorMgRuling(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal); }
        var bytes = authority.SerializeGiarabubMotorMgRulingEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame,
            session.CurrentContacts.AdvancePregameSource(authority.StateVersion), [.. session.PregameHistory, bytes], next),
            new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static bool IsSourceTurnStarted(ExerciseSession session) => session?.SourceTurn is not null;

    public static byte[] QuerySourceCounterContacts(ExerciseSession session, CampaignActionAudience audience, string counterId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var turn = session.SourceTurn ?? throw new InvalidOperationException("Source turn authority is unavailable.");
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Source counter observer seat is unavailable."),
        };
        return session.CurrentContacts.ObserveSourceCounter(turn, side, counterId);
    }

    public static bool IsSourceInitiativeResolved(ExerciseSession session) => session?.SourceTurn?.InitiativeHolder is not null;

    public static bool IsSourceGt1SchedulingCompleted(ExerciseSession session) =>
        session?.SourceTurn is { Gt3Supply.Finished: true, Gt5Supply.Die: not null } turn
        && turn.Position.PhaseId != LandPhaseIds.NavalConvoySchedule;

    public static bool IsSourceGt1WeatherResolved(ExerciseSession session) => session?.SourceTurn?.StageOneWeather is not null;

    public static bool IsSourceGt1Lane2Announced(ExerciseSession session) => session?.SourceTurn?.AxisArrival.Announced == true;

    public static bool IsSourceGt1Lane3Announced(ExerciseSession session) => session?.SourceTurn?.BenghaziArrival.Announced == true;

    public static bool IsSourceGt1MultiLanesAnnounced(ExerciseSession session) => session?.SourceTurn?.MultiArrival is not null;

    public static bool IsSourceTurnSeat(ExerciseSession session, CampaignActionAudience audience) =>
        session is not null && session.SourceTurn is not null
        && audience is CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth;

    public static ExerciseStepResult SubmitSourceGt3SupplyRoll(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || submission.Audience != CampaignActionAudience.Axis)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != prior.Source.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != prior.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != prior.Position.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var query = CampaignSourceSetupBridge.QueryTurn(session, submission.Audience);
        if (!query.IsSuccessful || query.ActionSet?.Candidates.SingleOrDefault(value => value.ActionId == submission.ActionId)
                is not CampaignSourceTurnAction action)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = action.Kind switch
        {
            "source-axis-gt3-supply-roll" => prior.RollGt3Supply(),
            "source-axis-gt3-supply-adjust" => prior.AdjustGt3Supply(action.Lane!.Value, action.Stage!.Value, action.Delta!.Value),
            "source-axis-gt3-supply-finish" => prior.FinishGt3Supply(),
            _ => throw new InvalidOperationException("Unsupported GT3 supply action."),
        };
        var eventType = action.Kind switch
        {
            "source-axis-gt3-supply-roll" => "source-axis-gt3-supply-rolled",
            "source-axis-gt3-supply-adjust" => "source-axis-gt3-supply-adjusted",
            _ => "source-axis-gt3-supply-finished",
        };
        var bytes = next.SerializeEvent(eventType, submission.ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), actor: "axis", lane: action.Lane, stage: action.Stage, delta: action.Delta);
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.Axis, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion), [.. session.PregameHistory, bytes], prior.Source, next),
            new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult SubmitSourceGt5SupplyRoll(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || submission.Audience != CampaignActionAudience.Commonwealth)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != prior.Source.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != prior.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != prior.Position.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var query = CampaignSourceSetupBridge.QueryTurn(session, submission.Audience);
        if (!query.IsSuccessful || query.ActionSet?.Candidates.SingleOrDefault(value => value.ActionId == submission.ActionId)
                is not CampaignSourceTurnAction { Kind: "source-commonwealth-gt5-supply-roll" })
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = prior.RollGt5Supply();
        var bytes = next.SerializeEvent("source-commonwealth-gt5-supply-rolled", submission.ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), actor: "commonwealth");
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.Commonwealth, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion), [.. session.PregameHistory, bytes], prior.Source, next),
            new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult SubmitSourceTacticalShippingDecline(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion
            || submission.Audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != prior.Source.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != prior.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != prior.Position.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var query = CampaignSourceSetupBridge.QueryTurn(session, submission.Audience);
        if (!query.IsSuccessful || query.ActionSet?.Candidates.SingleOrDefault(value => value.ActionId == submission.ActionId)
                is not CampaignSourceTurnAction { Kind: "source-tactical-shipping-decline" })
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var side = submission.Audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var next = prior.DeclineTacticalShipping(side);
        var bytes = next.SerializeEvent("source-tactical-shipping-declined", submission.ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), actor: side == LandSide.Axis ? "axis" : "commonwealth");
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult SubmitSourceStageOneOrder(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || submission.Audience != CampaignActionAudience.Axis)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != prior.Source.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != prior.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != prior.Position.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var query = CampaignSourceSetupBridge.QueryTurn(session, submission.Audience);
        if (!query.IsSuccessful || query.ActionSet?.Candidates.SingleOrDefault(value => value.ActionId == submission.ActionId)
                is not CampaignSourceTurnAction action || action.Kind is not ("source-stage1-act-first" or "source-stage1-act-last"))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = prior.DeclareStageOneOrder(action.Kind == "source-stage1-act-first");
        var bytes = next.SerializeEvent("source-stage1-order-declared", submission.ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), actor: "axis", axisFirst: action.Kind == "source-stage1-act-first");
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.Axis, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult SubmitSourceVoluntaryOrganizationDecline(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion
            || submission.Audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != prior.Source.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != prior.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != prior.Position.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var query = CampaignSourceSetupBridge.QueryTurn(session, submission.Audience);
        if (!query.IsSuccessful || query.ActionSet?.Candidates.SingleOrDefault(value => value.ActionId == submission.ActionId)
                is not CampaignSourceTurnAction { Kind: "source-voluntary-organization-decline" })
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var side = submission.Audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var next = prior.DeclineVoluntaryOrganization(side);
        var bytes = next.SerializeEvent("source-voluntary-organization-declined", submission.ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), actor: side == LandSide.Axis ? "axis" : "commonwealth");
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult SubmitSourceGt1FirstReservesDecline(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion
            || submission.Audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != prior.Source.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != prior.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != prior.Position.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var query = CampaignSourceSetupBridge.QueryTurn(session, submission.Audience);
        if (!query.IsSuccessful || query.ActionSet?.Candidates.SingleOrDefault(value => value.ActionId == submission.ActionId)
                is not CampaignSourceTurnAction { Kind: "source-stage1-first-reserves-decline" })
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var side = submission.Audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var next = prior.DeclineGt1FirstReserves(side);
        var actor = side == LandSide.Axis ? "axis" : "commonwealth";
        var bytes = next.SerializeEvent("source-gt1-first-reserves-declined", submission.ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), actor: actor);
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult SubmitSourceGt1FirstMovementFinish(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion
            || submission.Audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != prior.Source.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != prior.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != prior.Position.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var query = CampaignSourceSetupBridge.QueryTurn(session, submission.Audience);
        if (!query.IsSuccessful || query.ActionSet?.Candidates.SingleOrDefault(value => value.ActionId == submission.ActionId)
                is not CampaignSourceTurnAction { Kind: "source-stage1-first-movement-finish" })
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var side = submission.Audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var next = prior.FinishGt1FirstMovement(side);
        var actor = side == LandSide.Axis ? "axis" : "commonwealth";
        var bytes = next.SerializeEvent("source-gt1-first-movement-finished", submission.ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), actor: actor);
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult SubmitSourceFourthIndianOpeningMove(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is not { } prior) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || submission.Audience != CampaignActionAudience.Commonwealth)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != prior.Source.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != prior.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != prior.Position.PositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var query = CampaignSourceSetupBridge.QueryTurn(session, submission.Audience);
        if (!query.IsSuccessful || query.ActionSet?.Candidates.SingleOrDefault(value => value.ActionId == submission.ActionId)
                is not CampaignSourceTurnAction { Kind: "source-fourth-indian-opening-move", Destination: { } supplyId })
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        CampaignSourceTurnAuthority next; CampaignContactJournal contacts;
        try
        {
            next = prior.MoveFourthIndianOpening(supplyId);
            contacts = session.CurrentContacts.AppendSourceMovement(prior, next, CampaignSourceFourthIndianMovement.GroupId);
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException or JsonException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = next.SerializeEvent("source-fourth-indian-opening-moved", submission.ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), actor: "commonwealth", supplyId: supplyId);
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame, contacts,
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult SubmitSourceAustralianBaselineVote(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is not { } prior) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion
            || submission.Audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != prior.Source.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != prior.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != prior.Position.PositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var query = CampaignSourceSetupBridge.QueryTurn(session, submission.Audience);
        if (!query.IsSuccessful || query.ActionSet?.Candidates.SingleOrDefault(value => value.ActionId == submission.ActionId)
                is not CampaignSourceTurnAction { Kind: "source-australian-baseline-approve" or "source-australian-baseline-withdraw" } action)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var side = submission.Audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var approve = action.Kind == "source-australian-baseline-approve";
        var next = prior.VoteSixthAustralianBaseline(side, approve);
        var bytes = next.SerializeEvent("source-australian-baseline-voted", submission.ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), actor: side == LandSide.Axis ? "axis" : "commonwealth", baselineApproved: approve);
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult SubmitSourceSixthAustralianTrainingInterrupt(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is not { } prior) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || submission.Audience != CampaignActionAudience.Commonwealth)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != prior.Source.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != prior.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != prior.Position.PositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var query = CampaignSourceSetupBridge.QueryTurn(session, submission.Audience);
        if (!query.IsSuccessful || query.ActionSet?.Candidates.SingleOrDefault(value => value.ActionId == submission.ActionId)
                is not CampaignSourceTurnAction { Kind: "source-sixth-australian-training-interrupt" })
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = prior.InterruptSixthAustralianTraining();
        var bytes = next.SerializeEvent("source-sixth-australian-training-interrupted", submission.ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), actor: "commonwealth");
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult SubmitSourceGt1Lane2AttackDecline(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is not { } prior) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || submission.Audience != CampaignActionAudience.Commonwealth)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != prior.Source.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != prior.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != prior.Position.PositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var query = CampaignSourceSetupBridge.QueryTurn(session, submission.Audience);
        if (!query.IsSuccessful || query.ActionSet?.Candidates.SingleOrDefault(value => value.ActionId == submission.ActionId)
                is not CampaignSourceTurnAction { Kind: "source-gt1-lane2-attack-decline" or "source-gt1-lane3-attack-decline" or "source-gt1-lane6-attack-decline" } action)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var lane = action.Kind switch { "source-gt1-lane2-attack-decline" => 2, "source-gt1-lane3-attack-decline" => 3, _ => 6 };
        var next = prior.MultiArrival is not null ? prior.DeclineGt1MultiLaneAttack(lane)
            : lane == 2 ? prior.DeclineGt1Lane2Attack() : prior.DeclineGt1Lane3Attack();
        var eventType = prior.MultiArrival is not null ? "source-gt1-op1-multi-lane-declined"
            : lane == 2 ? "source-gt1-op1-lane2-attack-declined" : "source-gt1-op1-lane3-attack-declined";
        var bytes = next.SerializeEvent(eventType, submission.ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), actor: "commonwealth", lane: lane, stage: 1);
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult AnnounceSourceGt1Lane2(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceTurn is not { } prior) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceTurnAuthority next;
        try { next = prior.AnnounceGt1Lane2(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = next.SerializeEvent("source-gt1-op1-lane2-announced", CampaignSourceTurnAuthority.AnnounceGt1Lane2ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), lane: 2, stage: 1);
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.System, CampaignSourceTurnAuthority.AnnounceGt1Lane2ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult AnnounceSourceGt1Lane3(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceTurn is not { } prior) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceTurnAuthority next;
        try { next = prior.AnnounceGt1Lane3(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = next.SerializeEvent("source-gt1-op1-lane3-announced", CampaignSourceTurnAuthority.AnnounceGt1Lane3ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), lane: 3, stage: 1);
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.System, CampaignSourceTurnAuthority.AnnounceGt1Lane3ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult AnnounceSourceGt1MultiLanes(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceTurn is not { } prior) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceTurnAuthority next;
        try { next = prior.AnnounceGt1MultiLanes(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = next.SerializeEvent("source-gt1-op1-multi-lanes-announced", CampaignSourceTurnAuthority.AnnounceGt1MultiLanesActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), announcedLanes: next.MultiArrival!.Lanes);
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.System, CampaignSourceTurnAuthority.AnnounceGt1MultiLanesActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    /// <summary>Return the Commonwealth's source-backed Polish arrival options at their actual turn position.</summary>
    public static CampaignPregameAxisDraft QuerySourcePolishArrivalDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceTurn is not { } turn)
            throw new InvalidOperationException("Polish arrival authority is unavailable.");
        var choices = CampaignSourcePolishArrival.ValidDestinations(turn.World, turn.Position);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", turn.Position.PositionId);
            writer.WriteString("groupId", CampaignSourcePolishArrival.GroupId);
            writer.WriteStartArray("choices");
            foreach (var location in choices) { writer.WriteStartObject(); writer.WriteString("destination", location); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(turn.StateVersion, stream.ToArray(), choices.Select(turn.PolishActionId));
    }

    public static ExerciseStepResult SubmitSourcePolishArrival(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || submission.Audience != CampaignActionAudience.Commonwealth)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != prior.Source.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != prior.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != prior.Position.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var destination = CampaignSourcePolishArrival.ValidDestinations(prior.World, prior.Position)
            .SingleOrDefault(value => prior.PolishActionId(value) == submission.ActionId);
        if (destination is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = prior.ArrivePolish(destination);
        var bytes = next.SerializeEvent("source-polish-brigade-arrived", submission.ActionId,
            prior.StateVersion, prior.SerializeCheckpoint(), destination);
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.Commonwealth, submission.ActionId);
        CampaignContactJournal contacts;
        try { contacts = session.CurrentContacts.AppendSourcePolishArrival(prior, next); }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException or JsonException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            contacts, [.. session.PregameHistory, bytes], prior.Source, next),
            new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    /// <summary>Start the source-backed turn only after its historical setup is ready.</summary>
    public static ExerciseStepResult StartSourceTurn(ExerciseSession session) => StartSourceTurn(session, CampaignSourceTurnAuthority.CdSeamContractVersion);

    internal static ExerciseStepResult StartSourceTurn(ExerciseSession session, bool legacy) => StartSourceTurn(session,
        legacy ? CampaignSourceTurnAuthority.ContractVersion : CampaignSourceTurnAuthority.MapBoundContractVersion);

    internal static ExerciseStepResult StartSourceTurn(ExerciseSession session, int contractVersion)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (contractVersion is not (CampaignSourceTurnAuthority.ContractVersion or CampaignSourceTurnAuthority.MapBoundContractVersion
                or CampaignSourceTurnAuthority.CdSeamContractVersion)
            || session.SourceTurn is not null || session.SourceAuthority is not { } source)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceTurnAuthority turn;
        try
        {
            turn = CampaignSourceTurnAuthority.Start(source, legacy: contractVersion == CampaignSourceTurnAuthority.ContractVersion,
            cdSeam: contractVersion == CampaignSourceTurnAuthority.CdSeamContractVersion);
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = turn.SerializeEvent("source-turn-started", CampaignSourceTurnAuthority.StartActionId,
            source.StateVersion, source.SerializeCheckpoint());
        var receipt = new CampaignActionAcceptanceReceipt(source.Request.CampaignId, source.StateVersion, turn.StateVersion,
            turn.Position.PositionId, CampaignActionAudience.System, CampaignSourceTurnAuthority.StartActionId);
        CampaignContactJournal contacts;
        try { contacts = session.CurrentContacts.AppendSourceTurnStart(turn); }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException or JsonException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        return ExerciseStepResult.Accepted(new ExerciseSession(source.Pregame,
            contacts, [.. session.PregameHistory, bytes], source, turn),
            new ExerciseStepEvidence(receipt, bytes, turn.SerializeCheckpoint()));
    }

    /// <summary>Resolve the first turn's predetermined initiative without resetting the retained RNG.</summary>
    public static ExerciseStepResult ResolveSourceInitiative(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceTurnAuthority next;
        try { next = prior.ResolveInitiative(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = next.SerializeEvent("source-initiative-determined", CampaignSourceTurnAuthority.InitiativeActionId,
            prior.StateVersion, prior.SerializeCheckpoint());
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.System, CampaignSourceTurnAuthority.InitiativeActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion), [.. session.PregameHistory, bytes], prior.Source, next),
            new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult CompleteSourceGt1Scheduling(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceTurnAuthority next;
        try { next = prior.CompleteGt1Scheduling(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = next.SerializeEvent("source-gt1-convoy-schedule-completed", CampaignSourceTurnAuthority.CompleteGt1ScheduleActionId,
            prior.StateVersion, prior.SerializeCheckpoint());
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.System, CampaignSourceTurnAuthority.CompleteGt1ScheduleActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion), [.. session.PregameHistory, bytes], prior.Source, next),
            new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static bool IsSourceGt1OrganizationCompleted(ExerciseSession session) =>
        session?.SourceTurn is { } turn && (turn.Position.GameTurn > 1 || turn.Position.OperationStage > 1
            || (turn.VoluntaryOrganizationDeclinedMask == 3 && turn.Position.PhaseId != LandPhaseIds.Organization));

    public static bool IsSourceGt1StageOneArrivalsCompleted(ExerciseSession session) =>
        session?.SourceTurn is { } turn && (turn.Position.GameTurn > 1 || turn.Position.OperationStage > 1
            || (turn.Position.GameTurn == 1 && turn.Position.OperationStage == 1
                && turn.Position.PositionId == Cna1979LandSequenceV4.BreakdownStopPositionId)
            || (turn.Position.GameTurn == 1 && turn.Position.OperationStage == 1
                && turn.VoluntaryOrganizationDeclinedMask == 3
                && Cna1979LandSequenceV4.CreateTurn(1).ToList().FindIndex(value => value.PositionId == turn.Position.PositionId)
                    > Cna1979LandSequenceV4.CreateTurn(1).ToList().FindIndex(value => value.OperationStage == 1
                        && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null)));

    public static bool IsSourceGt1StageOneFleetCompleted(ExerciseSession session) =>
        session?.SourceTurn is { } turn && (turn.Position.GameTurn > 1 || turn.Position.OperationStage > 1
            || (turn.Position.GameTurn == 1 && turn.Position.OperationStage == 1
                && turn.Position.PositionId == Cna1979LandSequenceV4.BreakdownStopPositionId)
            || (turn.Position.GameTurn == 1 && turn.Position.OperationStage == 1
                && Cna1979LandSequenceV4.CreateTurn(1).ToList().FindIndex(value => value.PositionId == turn.Position.PositionId)
                    > Cna1979LandSequenceV4.CreateTurn(1).ToList().FindIndex(value => value.OperationStage == 1
                        && value.PhaseId == LandPhaseIds.CommonwealthFleet && value.SegmentId == LandSegmentIds.FleetRepair)));

    public static bool IsSourceGt1FirstBreakdownCompleted(ExerciseSession session) =>
        session?.SourceTurn is { } turn && (turn.Position.GameTurn > 1 || turn.Position.OperationStage > 1
            || (turn.Position.GameTurn == 1 && turn.Position.OperationStage == 1
                && Cna1979LandSequenceV4.CreateTurn(1).ToList().FindIndex(value => value.PositionId == turn.Position.PositionId)
                    > Cna1979LandSequenceV4.CreateTurn(1).ToList().FindIndex(value => value.OperationStage == 1
                        && value.ActorRole == LandActorRole.FirstActingSide && value.SegmentId == LandSegmentIds.BreakdownDetermination)));

    public static ExerciseStepResult CompleteSourceGt1FirstBreakdown(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceTurnAuthority next;
        try { next = prior.CompleteGt1FirstBreakdown(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = next.SerializeEvent("source-gt1-first-breakdown-completed", CampaignSourceTurnAuthority.CompleteGt1FirstBreakdownActionId,
            prior.StateVersion, prior.SerializeCheckpoint());
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.System, CampaignSourceTurnAuthority.CompleteGt1FirstBreakdownActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult CompleteSourceGt1StageOneFleet(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceTurnAuthority next;
        try { next = prior.CompleteGt1StageOneFleet(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = next.SerializeEvent("source-gt1-stage1-fleet-completed", CampaignSourceTurnAuthority.CompleteGt1FleetActionId,
            prior.StateVersion, prior.SerializeCheckpoint());
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.System, CampaignSourceTurnAuthority.CompleteGt1FleetActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult CompleteSourceGt1StageOneArrivals(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceTurnAuthority next;
        try { next = prior.CompleteGt1StageOneArrivals(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = next.SerializeEvent("source-gt1-stage1-arrivals-completed", CampaignSourceTurnAuthority.CompleteGt1ArrivalsActionId,
            prior.StateVersion, prior.SerializeCheckpoint());
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.System, CampaignSourceTurnAuthority.CompleteGt1ArrivalsActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult CompleteSourceGt1Organization(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceTurnAuthority next;
        try { next = prior.CompleteGt1Organization(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = next.SerializeEvent("source-gt1-organization-completed", CampaignSourceTurnAuthority.CompleteGt1OrganizationActionId,
            prior.StateVersion, prior.SerializeCheckpoint());
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.System, CampaignSourceTurnAuthority.CompleteGt1OrganizationActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult ResolveSourceGt1Weather(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceTurn is not { } prior)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceTurnAuthority next;
        try { next = prior.ResolveStageOneWeather(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = next.SerializeEvent("source-gt1-stage1-weather-determined", CampaignSourceTurnAuthority.ResolveStageOneWeatherActionId,
            prior.StateVersion, prior.SerializeCheckpoint());
        var receipt = new CampaignActionAcceptanceReceipt(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion,
            next.Position.PositionId, CampaignActionAudience.System, CampaignSourceTurnAuthority.ResolveStageOneWeatherActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(prior.Source.Pregame,
            session.CurrentContacts.AdvanceSourceTurnNonMovement(prior.Source.Request.CampaignId, prior.StateVersion, next.StateVersion),
            [.. session.PregameHistory, bytes], prior.Source, next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    /// <summary>Begin the bounded source-backed pre-game setup, not a complete campaign.</summary>
    public static ExerciseStartResult Begin(CampaignCreationRequest request,
        ContentSourceScheduledCombatPackArtifact artifact, CampaignSourceInitialization initialization)
    {
        var pregame = CampaignPregameSourceState.Create(request, artifact, initialization);
        return ExerciseStartResult.Started(new ExerciseSession(pregame),
            pregame.SerializeGenesis(), pregame.SerializeCheckpoint());
    }

    public static ExerciseStartResult ReplayPregameGenesis(CampaignCreationRequest request,
        ReadOnlyMemory<byte> creationEvent, ReadOnlyMemory<byte> initialSnapshot)
    {
        var pregame = CampaignPregameSourceState.ReplayGenesis(request, creationEvent, initialSnapshot);
        return ExerciseStartResult.Started(new ExerciseSession(pregame),
            pregame.SerializeGenesis(), pregame.SerializeCheckpoint());
    }

    public static bool IsPregameAxisSetup(ExerciseSession session, CampaignActionAudience audience) =>
        session is not null && session.Pregame is not null && session.SourceAuthority is null && session.SourceTurn is null
        && audience == CampaignActionAudience.Axis;

    /// <summary>Setup seats are available only after the source-authority transition.</summary>
    public static bool IsSourceSetupSeat(ExerciseSession session, CampaignActionAudience audience) =>
        session is not null && session.SourceAuthority is not null
        && audience is CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth;

    public static bool IsSourceAxisSupplementAdmitted(ExerciseSession session) =>
        session is not null && session.SourceAuthority?.AxisSupplement is not null;

    public static bool IsSourceCommonwealthSupplementAdmitted(ExerciseSession session) =>
        session is not null && session.SourceAuthority?.CommonwealthSupplement is not null;

    public static bool IsSourcePriorityMotorizationAdmitted(ExerciseSession session) =>
        session is not null && session.SourceAuthority?.PriorityMotorization is not null;

    public static bool IsSourceAxisPriorityMotorizationAdmitted(ExerciseSession session) =>
        session is not null && session.SourceAuthority?.AxisPriorityMotorization is not null;

    public static bool IsSourceAxisMinorGarrisonsAdmitted(ExerciseSession session) =>
        session is not null && session.SourceAuthority?.AxisGarrisons is not null;

    public static bool IsSourceBattlefieldAdmitted(ExerciseSession session) =>
        session is not null && session.SourceAuthority?.Battlefield is not null;

    public static bool IsSourceCairoSetupAdmitted(ExerciseSession session) =>
        session is not null && session.SourceAuthority?.CairoSetup is not null;

    public static bool IsSourceCairoWorldAdmitted(ExerciseSession session) =>
        session is not null && session.SourceAuthority?.CairoWorld is not null;

    public static bool CanPromoteSourceMaletti(ExerciseSession session) =>
        false;

    private static bool HasUncertifiedPendingMaletti(CampaignSourceAuthorityState authority) =>
        authority.AxisPriorityMotorization?.DetachedTargets.Any(id =>
            authority.World.World.Elements.All(element => element.ElementId != id)) == true;

    public static bool CanPromoteSourceIndependent(ExerciseSession session) =>
        session is not null && session.SourceAuthority is { CanPromoteIndependent: true } authority
            && authority.AxisFreeArtilleryWorld is null && !HasUncertifiedPendingMaletti(authority);

    public static bool IsSourceLocalMapAdmitted(ExerciseSession session) =>
        session is not null && session.SourceAuthority?.LocalMapWorld is not null;

    public static CampaignPregameAxisDraft QuerySourceActionsDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        var query = Query(session, audience);
        if (query.IsSuccessful && query.ActionSet?.PolicyId == CampaignLegalActionSet.SourceTurnPolicyId)
        {
            using var sourceStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(sourceStream))
            {
                writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
                writer.WriteString("positionId", query.ActionSet.PositionId);
                writer.WriteString("status", "turn-in-progress"); writer.WriteStartArray("contexts");
                if (session.SourceTurn?.StageOneFirstSide is { } firstSide)
                {
                    writer.WriteStartObject(); writer.WriteString("family", "stage1-order"); writer.WriteStartObject("state");
                    writer.WriteString("firstSide", firstSide == LandSide.Axis ? "axis" : "commonwealth");
                    writer.WriteString("secondSide", firstSide == LandSide.Axis ? "commonwealth" : "axis");
                    writer.WriteEndObject(); writer.WriteEndObject();
                }
                if (session.SourceTurn?.StageOneWeather is { } weather)
                {
                    writer.WriteStartObject(); writer.WriteString("family", "stage1-weather"); writer.WriteStartObject("state");
                    writer.WriteString("season", weather.Season.ToString()); writer.WriteNumber("firstDie", weather.FirstDie);
                    writer.WriteNumber("secondDie", weather.SecondDie); writer.WriteString("kind", weather.Kind.ToString());
                    writer.WriteString("scope", weather.Scope.ToString());
                    if (weather.LocationDie is int locationDie) writer.WriteNumber("locationDie", locationDie); else writer.WriteNull("locationDie");
                    writer.WriteStartArray("affectedAreas"); foreach (var area in weather.AffectedAreas) writer.WriteStringValue(area.ToString()); writer.WriteEndArray();
                    writer.WriteEndObject(); writer.WriteEndObject();
                }
                if (audience == CampaignActionAudience.Axis && session.SourceTurn is { Position.PhaseId: LandPhaseIds.NavalConvoySchedule, Gt3Supply: { } supply })
                {
                    writer.WriteStartObject(); writer.WriteString("family", "axis-gt3-supply"); writer.WriteStartObject("state");
                    writer.WriteNumber("targetGameTurn", 3);
                    if (supply.Die is int die) writer.WriteNumber("die", die); else writer.WriteNull("die");
                    if (supply.Maximum is int maximum) writer.WriteNumber("maximumSupply", maximum); else writer.WriteNull("maximumSupply");
                    writer.WriteNumber("allocated", supply.Allocated); writer.WriteBoolean("finished", supply.Finished);
                    writer.WriteString("scope", "gt3-supply-planning-only-not-delivery");
                    writer.WriteString("unusedSupplyPolicy", "provisional-owner-choice");
                    writer.WriteStartArray("allocations");
                    foreach (var lane in new[] { 2, 3, 6 }) for (var stage = 1; stage <= 3; stage++)
                    {
                        var quantity = supply.QuantityAt(lane, stage); if (quantity == 0) continue;
                        writer.WriteStartObject(); writer.WriteNumber("lane", lane); writer.WriteNumber("operationStage", stage);
                        writer.WriteNumber("supplyUnits", quantity); writer.WriteEndObject();
                    }
                    writer.WriteEndArray(); writer.WriteEndObject(); writer.WriteEndObject();
                }
                if (audience == CampaignActionAudience.Commonwealth && session.SourceTurn is { Position.PhaseId: LandPhaseIds.NavalConvoySchedule, Gt5Supply: { } cwSupply })
                {
                    writer.WriteStartObject(); writer.WriteString("family", "cw-gt5-supply"); writer.WriteStartObject("state");
                    writer.WriteNumber("targetGameTurn", 5);
                    if (cwSupply.Die is int die) writer.WriteNumber("die", die); else writer.WriteNull("die");
                    if (cwSupply.Units is int units) writer.WriteNumber("supplyUnits", units); else writer.WriteNull("supplyUnits");
                    writer.WriteString("scope", "availability-only-not-delivered"); writer.WriteEndObject(); writer.WriteEndObject();
                }
                if (session.SourceTurn is { Position.PhaseId: LandPhaseIds.NavalConvoyArrival, Position.OperationStage: 1, AxisArrival.Announced: true } arrival)
                {
                    writer.WriteStartObject(); writer.WriteString("family", "axis-gt1-convoy-attack"); writer.WriteStartObject("state");
                    writer.WriteNumber("lane", 2); writer.WriteNumber("operationStage", 1);
                    writer.WriteBoolean("delivered", arrival.AxisArrival.Delivered);
                    writer.WriteString("supportedChoices", "decline-only-attacks-not-yet-supported");
                    if (audience == CampaignActionAudience.Axis) writer.WriteNumber("supplyUnits", arrival.AxisArrival.Lane2Units);
                    writer.WriteEndObject(); writer.WriteEndObject();
                }
                if (session.SourceTurn is { Position.PhaseId: LandPhaseIds.NavalConvoyArrival, Position.OperationStage: 1, BenghaziArrival.Announced: true } benghazi)
                {
                    writer.WriteStartObject(); writer.WriteString("family", "axis-gt1-convoy-attack"); writer.WriteStartObject("state");
                    writer.WriteNumber("lane", 3); writer.WriteNumber("operationStage", 1);
                    writer.WriteBoolean("delivered", benghazi.BenghaziArrival.Delivered);
                    writer.WriteString("supportedChoices", "decline-only-attacks-not-yet-supported");
                    writer.WriteString("coordinateRuling", "rul-gra-05-a4827-provisional-scenario-b4827");
                    if (audience == CampaignActionAudience.Axis) writer.WriteNumber("supplyUnits", benghazi.BenghaziArrival.Units);
                    writer.WriteEndObject(); writer.WriteEndObject();
                }
                if (session.SourceTurn is { Position.PhaseId: LandPhaseIds.NavalConvoyArrival, Position.OperationStage: 1, MultiArrival: { } multi })
                {
                    writer.WriteStartObject(); writer.WriteString("family", "axis-gt1-convoy-arrivals"); writer.WriteStartObject("state");
                    writer.WriteString("supportedChoices", "decline-only-attacks-not-yet-supported");
                    writer.WriteStartArray("lanes");
                    foreach (var announcedLane in multi.Lanes)
                    {
                        writer.WriteStartObject(); writer.WriteNumber("lane", announcedLane); writer.WriteNumber("operationStage", 1);
                        if (announcedLane != 6 || audience == CampaignActionAudience.Axis)
                            writer.WriteBoolean("delivered", multi.IsDelivered(announcedLane));
                        if (announcedLane == 6)
                        {
                            writer.WriteBoolean("attackDeclined", multi.Lane6CommonwealthCut().AttackDeclined);
                            writer.WriteString("portEfficiency", "scenario-7-versus-chart-5-obstruction-conflict-unresolved");
                            if (audience == CampaignActionAudience.Axis && multi.Lane6Declined && !multi.IsDelivered(6))
                                writer.WriteString("status", "delivery-blocked-overflow-ruling-unresolved");
                        }
                        if (announcedLane == 3) writer.WriteString("coordinateRuling", "rul-gra-05-a4827-provisional-scenario-b4827");
                        if (audience == CampaignActionAudience.Axis) writer.WriteNumber("supplyUnits", multi.Quantity(announcedLane));
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray(); writer.WriteEndObject(); writer.WriteEndObject();
                }
                if (session.SourceTurn is { Position.PhaseId: LandPhaseIds.TacticalShipping } shipping)
                {
                    var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
                    writer.WriteStartObject(); writer.WriteString("family", "tactical-shipping"); writer.WriteStartObject("state");
                    writer.WriteBoolean("ownSideDeclined", shipping.HasDeclinedTacticalShipping(side));
                    writer.WriteString("supportedChoices", "decline-only-shipping-plans-not-yet-supported");
                    writer.WriteEndObject(); writer.WriteEndObject();
                }
                if (session.SourceTurn is { Position.PhaseId: LandPhaseIds.Organization } organization)
                {
                    var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
                    writer.WriteStartObject(); writer.WriteString("family", "voluntary-organization"); writer.WriteStartObject("state");
                    writer.WriteBoolean("ownSideDeclined", organization.HasDeclinedVoluntaryOrganization(side));
                    writer.WriteString("scope", "no-voluntary-changes-only");
                    writer.WriteString("mandatoryWork", "construction-and-training-completion-unresolved");
                    writer.WriteStartObject("provisionalTrainingBaseline");
                    writer.WriteString("rulingId", CampaignSourceTrainingLedger.ProvisionalBaselineId);
                    writer.WriteString("interpretation", "uniform-initial-morale-minus-three-zero-prior-qualifying-stages");
                    writer.WriteString("status", organization.Training.BaselineVotes == 3 ? "agreed-provisional" : "awaiting-independent-approvals");
                    writer.WriteBoolean("ownApproval", organization.Training.HasBaselineVote(audience == CampaignActionAudience.Axis));
                    writer.WriteBoolean("historicalFact", false);
                    writer.WriteEndObject();

                    if (audience == CampaignActionAudience.Commonwealth)
                    {
                        writer.WriteStartObject("sixthAustralianTraining");
                        if (organization.Training.CurrentMorale is int morale) writer.WriteNumber("currentMorale", morale); else writer.WriteNull("currentMorale");
                        if (organization.Training.QualifyingStages is int stages) writer.WriteNumber("priorQualifyingStages", stages); else writer.WriteNull("priorQualifyingStages");
                        writer.WriteString("status", "starting-state-ruling-and-instructor-evidence-required");
                        writer.WriteBoolean("interrupted", organization.Training.Interrupted);
                        writer.WriteString("supportedTrainingChoices", "interrupt-only-resumption-and-progress-not-yet-supported");
                        writer.WriteBoolean("rulingInputSupported", false);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject(); writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteStartArray("choices");
                foreach (var action in query.ActionSet.Candidates.Cast<CampaignSourceTurnAction>())
                {
                    writer.WriteStartObject();
                    var isSupply = action.Kind.StartsWith("source-axis-gt3-supply-", StringComparison.Ordinal);
                    var isCwSupply = action.Kind == "source-commonwealth-gt5-supply-roll";
                    var isShipping = action.Kind == "source-tactical-shipping-decline";
                    var isOrder = action.Kind is "source-stage1-act-first" or "source-stage1-act-last";
                    var isOrganization = action.Kind == "source-voluntary-organization-decline";
                    var isReserves = action.Kind == "source-stage1-first-reserves-decline";
                    var isMovementFinish = action.Kind == "source-stage1-first-movement-finish";
                    var isConvoyAttack = action.Kind is "source-gt1-lane2-attack-decline" or "source-gt1-lane3-attack-decline" or "source-gt1-lane6-attack-decline";
                    var isTrainingInterrupt = action.Kind == "source-sixth-australian-training-interrupt";
                    var isBaselineVote = action.Kind is "source-australian-baseline-approve" or "source-australian-baseline-withdraw";
                    var isOpeningMove = action.Kind == "source-fourth-indian-opening-move";
                    writer.WriteString("family", isSupply ? "axis-gt3-supply" : isCwSupply ? "cw-gt5-supply" : isShipping ? "tactical-shipping" : isOrder ? "initiative-declaration" : isOrganization ? "voluntary-organization" : isReserves ? "first-reserve-designation" : isMovementFinish ? "first-movement" : isConvoyAttack ? "axis-gt1-convoy-attack" : isTrainingInterrupt ? "sixth-australian-training" : isBaselineVote ? "sixth-australian-baseline" : isOpeningMove ? "fourth-indian-opening-move" : "polish-arrival");
                    writer.WriteStartObject("choice");
                    if (isSupply)
                    {
                        writer.WriteString("kind", action.Kind switch
                        {
                            "source-axis-gt3-supply-roll" => "roll",
                            "source-axis-gt3-supply-finish" => "finish",
                            "source-axis-gt3-supply-adjust" when action.Delta == 1 => "add",
                            "source-axis-gt3-supply-adjust" => "remove",
                            _ => throw new InvalidOperationException("Unsupported GT3 supply action."),
                        });
                        writer.WriteNumber("targetGameTurn", 3);
                        if (action.Lane is int lane) { writer.WriteNumber("lane", lane); writer.WriteNumber("operationStage", action.Stage!.Value); writer.WriteNumber("delta", action.Delta!.Value); }
                    }
                    else if (isCwSupply) { writer.WriteString("kind", "roll"); writer.WriteNumber("targetGameTurn", 5); }
                    else if (isShipping) writer.WriteString("kind", "decline");
                    else if (isOrder) { writer.WriteString("kind", action.Kind == "source-stage1-act-first" ? "act-first" : "act-last"); writer.WriteNumber("operationStage", 1); }
                    else if (isOrganization) writer.WriteString("kind", "decline");
                    else if (isReserves) writer.WriteString("kind", "decline");
                    else if (isMovementFinish) writer.WriteString("kind", "finish");
                    else if (isTrainingInterrupt) writer.WriteString("kind", "interrupt");
                    else if (isBaselineVote) { writer.WriteString("kind", action.Kind == "source-australian-baseline-approve" ? "approve" : "withdraw"); writer.WriteString("rulingId", CampaignSourceTrainingLedger.ProvisionalBaselineId); }
                    else if (isOpeningMove) { writer.WriteString("kind", "move"); writer.WriteString("groupId", CampaignSourceFourthIndianMovement.GroupId); writer.WriteString("destination", "d3715"); writer.WriteString("supplyId", action.Destination); }
                    else if (isConvoyAttack) { writer.WriteString("kind", "decline"); writer.WriteNumber("lane", action.Kind switch { "source-gt1-lane2-attack-decline" => 2, "source-gt1-lane3-attack-decline" => 3, _ => 6 }); writer.WriteNumber("operationStage", 1); }
                    else writer.WriteString("destination", action.Destination);
                    writer.WriteEndObject(); writer.WriteEndObject();
                }
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            return new CampaignPregameAxisDraft(query.ActionSet.StateVersion, sourceStream.ToArray(),
                query.ActionSet.Candidates.Select(value => value.ActionId));
        }
        if (!query.IsSuccessful || query.ActionSet?.PolicyId != CampaignLegalActionSet.SourceSetupPolicyId)
            throw new InvalidOperationException("Source setup action cut unavailable.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteNull("gameClock"); writer.WriteStartArray("contexts");
            foreach (var (family, draft) in CampaignSourceSetupBridge.Drafts(session, audience))
            {
                using var document = JsonDocument.Parse(draft.VisibleBytes);
                writer.WriteStartObject(); writer.WriteString("family", family); writer.WriteStartObject("state");
                foreach (var property in document.RootElement.EnumerateObject())
                    if (property.Name != "choices") property.WriteTo(writer);
                writer.WriteEndObject(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            foreach (var action in query.ActionSet.Candidates.Cast<CampaignSourceSetupAction>())
            { writer.WriteStartObject(); writer.WriteString("family", action.Family); writer.WritePropertyName("choice"); writer.WriteRawValue(action.ChoiceJson); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(query.ActionSet.StateVersion, stream.ToArray(), query.ActionSet.Candidates.Select(value => value.ActionId));
    }
    public static bool IsSourceCatanzaroGroupedAdmitted(ExerciseSession session) => session.SourceAuthority?.IsCatanzaroGrouped == true;
    public static ExerciseStepResult AdmitSourceCatanzaroGroupedWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { } authority || !authority.CanAdmitCatanzaro)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitCatanzaroGroupedWorld(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeCatanzaroGroupedEvent(next, false);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitCatanzaroActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static CampaignPregameAxisDraft QuerySourceCatanzaroDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.SourceAuthority?.IsCatanzaroGrouped != true)
            throw new InvalidOperationException("Catanzaro source world unavailable.");
        var authority = session.SourceAuthority;
        var group = authority.GroupedWorld!.Groups.Single(value => value.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("dispositionPolicy", "provisional-source-default-alternatives-unavailable");
            writer.WriteString("groupId", group.GroupId); writer.WriteString("locationId", group.LocationId);
            writer.WriteBoolean("dispositionConfirmed", group.DispositionConfirmed);
            writer.WriteNumber("remainingMotorPoints", authority.GroupedWorld.RemainingMotorPoints(group.GroupId));
            writer.WriteStartArray("members"); foreach (var member in group.Members)
            { writer.WriteStartObject(); writer.WriteString("elementId", member.ElementId); if (member.ParentId is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", member.ParentId); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            if (!group.DispositionConfirmed) { writer.WriteStartObject(); writer.WriteString("kind", "confirm"); writer.WriteString("groupId", group.GroupId); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), group.DispositionConfirmed ? [] : [CampaignSourceAuthorityState.ConfirmCatanzaroActionId]);
    }
    public static ExerciseStepResult SubmitSourceCatanzaro(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.IsCatanzaroGrouped != true) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var group = authority.GroupedWorld!.Groups.Single(value => value.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId);
        if (submission.Audience != CampaignActionAudience.Axis || group.DispositionConfirmed || submission.ActionId != CampaignSourceAuthorityState.ConfirmCatanzaroActionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ConfirmCatanzaro(); var bytes = authority.SerializeCatanzaroGroupedEvent(next, true);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceSeventhArmoredGroupedAdmitted(ExerciseSession session) => session.SourceAuthority?.IsSeventhArmoredGrouped == true;
    public static bool IsSourceSeventhShellCorrected(ExerciseSession session) => session.SourceAuthority?.IsSeventhShellCorrected == true;
    public static ExerciseStepResult CorrectSourceSeventhShell(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanCorrectSeventhShell: true } authority || session.SourceTurn is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.CorrectSeventhShell(); var bytes = authority.SerializeSeventhShellCorrection(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.CorrectSeventhShellActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static ExerciseStepResult AdmitSourceSeventhArmoredGroupedWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { } authority || !authority.CanAdmitSeventhArmored)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitSeventhArmoredGroupedWorld(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeSeventhArmoredGroupedEvent(next, false);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitSeventhArmoredActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static CampaignPregameAxisDraft QuerySourceSeventhArmoredDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceAuthority?.IsSeventhArmoredGrouped != true)
            throw new InvalidOperationException("Seventh Armored source world unavailable.");
        var authority = session.SourceAuthority;
        var group = authority.GroupedWorld!.Groups.Single(value => value.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("dispositionPolicy", "provisional-source-default-alternatives-unavailable");
            writer.WriteString("shellStatus", "division-shell-one-nonshell-brigade-of-three");
            writer.WriteString("stackRuling", authority.IsSeventhShellCorrected
                ? "common-charts-9.4-division-shell-stacks-three"
                : "provisional-shell-stacks-printed-five-when-confirmed");
            writer.WriteString("groupId", group.GroupId); writer.WriteString("locationId", group.LocationId);
            writer.WriteBoolean("dispositionConfirmed", group.DispositionConfirmed);
            writer.WriteNumber("remainingMotorPoints", authority.GroupedWorld.RemainingMotorPoints(group.GroupId));
            writer.WriteStartArray("members"); foreach (var member in group.Members)
            { writer.WriteStartObject(); writer.WriteString("elementId", member.ElementId); if (member.ParentId is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", member.ParentId); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            if (!group.DispositionConfirmed) { writer.WriteStartObject(); writer.WriteString("kind", "confirm"); writer.WriteString("groupId", group.GroupId); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), group.DispositionConfirmed ? [] : [CampaignSourceAuthorityState.ConfirmSeventhArmoredActionId]);
    }
    public static ExerciseStepResult SubmitSourceSeventhArmored(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.IsSeventhArmoredGrouped != true) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var group = authority.GroupedWorld!.Groups.Single(value => value.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId);
        if (submission.Audience != CampaignActionAudience.Commonwealth || group.DispositionConfirmed || submission.ActionId != CampaignSourceAuthorityState.ConfirmSeventhArmoredActionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ConfirmSeventhArmored(); var bytes = authority.SerializeSeventhArmoredGroupedEvent(next, true);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static CampaignPregameAxisDraft QuerySourceArescaDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var authority = session.SourceAuthority ?? throw new InvalidOperationException("Source authority missing.");
        if (audience != CampaignActionAudience.Axis || !authority.CanAdmitAresca) throw new InvalidOperationException("Aresca setup unavailable.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("status", "setup-incomplete");
            writer.WriteString("policy", "source-default-colocated-hierarchy-other-dispositions-unsupported");
            writer.WriteString("shellStack", "one-point-common-charts-9.4");
            writer.WriteStartArray("choices"); writer.WriteStartObject(); writer.WriteString("kind", "accept");
            writer.WriteString("groupId", CampaignSourceArescaGroupedDefinition.GroupId); writer.WriteEndObject(); writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), [CampaignSourceAuthorityState.ArescaActionId]);
    }
    public static ExerciseStepResult SubmitSourceAresca(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority || submission.Audience != CampaignActionAudience.Axis
            || !authority.CanAdmitAresca) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (submission.ActionId != CampaignSourceAuthorityState.ArescaActionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.AdmitAresca(); var bytes = authority.SerializeAresca(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static CampaignPregameAxisDraft QuerySourceCcnnBatchDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var authority = session.SourceAuthority ?? throw new InvalidOperationException("Source authority missing.");
        if (audience != CampaignActionAudience.Axis || !authority.CanAdmitCcnnBatch) throw new InvalidOperationException("CCNN setup unavailable.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("status", "setup-incomplete");
            writer.WriteString("policy", "source-default-colocated-hierarchies-other-dispositions-unsupported");
            writer.WriteString("limitations", "division-hq-and-engineer-component-details-incomplete");
            writer.WriteStartArray("choices"); writer.WriteStartObject(); writer.WriteString("kind", "accept");
            writer.WriteString("groupId", "1-and-4-ccnn"); writer.WriteEndObject(); writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), [CampaignSourceAuthorityState.CcnnBatchActionId]);
    }
    public static ExerciseStepResult SubmitSourceCcnnBatch(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority || submission.Audience != CampaignActionAudience.Axis
            || !authority.CanAdmitCcnnBatch) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (submission.ActionId != CampaignSourceAuthorityState.CcnnBatchActionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.AdmitCcnnBatch(); var bytes = authority.SerializeCcnnBatch(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static CampaignPregameAxisDraft QuerySourceSixthAustralianDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var authority = session.SourceAuthority ?? throw new InvalidOperationException("Source authority missing.");
        if (audience != CampaignActionAudience.Commonwealth || !authority.CanPlaceSixthAustralian)
            throw new InvalidOperationException("Australian placement is unavailable.");
        var locations = CampaignSourceSixthAustralianGroupedDefinition.Locations.Where(authority.CanPlaceSixthAustralianAt).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("status", "setup-incomplete");
            writer.WriteString("policy", "all-in-one-source-hex-split-placement-unsupported");
            writer.WriteString("training", "current-morale-and-prior-stages-unknown");
            writer.WriteStartArray("choices");
            foreach (var location in locations)
            { writer.WriteStartObject(); writer.WriteString("kind", "place"); writer.WriteString("locationId", location); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), locations.Select(CampaignSourceAuthorityState.SixthAustralianActionId).ToArray());
    }
    public static ExerciseStepResult SubmitSourceSixthAustralian(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority || submission.Audience != CampaignActionAudience.Commonwealth
            || authority.IsOwnerFinalized(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var location = CampaignSourceSixthAustralianGroupedDefinition.Locations.SingleOrDefault(value => CampaignSourceAuthorityState.SixthAustralianActionId(value) == submission.ActionId);
        if (location is null || !authority.CanPlaceSixthAustralianAt(location)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.PlaceSixthAustralian(location); var bytes = authority.SerializeSixthAustralianPlacement(next, location);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static bool IsSourceSixteenthBrigadeGroupedAdmitted(ExerciseSession session) => session.SourceAuthority?.IsSixteenthBrigadeGrouped == true;
    public static ExerciseStepResult AdmitSourceSixteenthBrigadeGroupedWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { } authority || !authority.CanAdmitSixteenthBrigade)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitSixteenthBrigadeGroupedWorld(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeSixteenthBrigadeGroupedEvent(next, false);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitSixteenthBrigadeActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static CampaignPregameAxisDraft QuerySourceSixteenthBrigadeDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceAuthority?.IsSixteenthBrigadeGrouped != true)
            throw new InvalidOperationException("Sixteenth Brigade source world unavailable.");
        var authority = session.SourceAuthority;
        var group = authority.GroupedWorld!.Groups.Single(value => value.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("dispositionPolicy", "provisional-source-default-alternatives-unavailable");
            writer.WriteString("shellStatus", "brigade-non-shell-three-battalions");
            writer.WriteString("stackRuling", "non-shell-stacks-printed-two-when-confirmed");
            writer.WriteString("groupId", group.GroupId); writer.WriteString("locationId", group.LocationId);
            writer.WriteBoolean("dispositionConfirmed", group.DispositionConfirmed);
            writer.WriteNumber("remainingMotorPoints", authority.GroupedWorld.RemainingMotorPoints(group.GroupId));
            writer.WriteStartArray("members"); foreach (var member in group.Members)
            { writer.WriteStartObject(); writer.WriteString("elementId", member.ElementId); if (member.ParentId is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", member.ParentId); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            if (!group.DispositionConfirmed) { writer.WriteStartObject(); writer.WriteString("kind", "confirm"); writer.WriteString("groupId", group.GroupId); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), group.DispositionConfirmed ? [] : [CampaignSourceAuthorityState.ConfirmSixteenthBrigadeActionId]);
    }
    public static ExerciseStepResult SubmitSourceSixteenthBrigade(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.IsSixteenthBrigadeGrouped != true) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var group = authority.GroupedWorld!.Groups.Single(value => value.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId);
        if (submission.Audience != CampaignActionAudience.Commonwealth || group.DispositionConfirmed || submission.ActionId != CampaignSourceAuthorityState.ConfirmSixteenthBrigadeActionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ConfirmSixteenthBrigade(); var bytes = authority.SerializeSixteenthBrigadeGroupedEvent(next, true);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceItalianDivisionsGroupedAdmitted(ExerciseSession session) => session.SourceAuthority?.IsItalianDivisionsGrouped == true;
    public static ExerciseStepResult AdmitSourceItalianDivisionsGroupedWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { } authority || !authority.CanAdmitItalianDivisionsGrouped)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitItalianDivisionsGroupedWorld(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeItalianDivisionsGroupedEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitItalianDivisionsActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    private static IEnumerable<string> SourceItalianDivisionChoices(CampaignSourceAuthorityState authority) => authority.GroupedWorld is null ? []
        : authority.GroupedWorld.Groups.Where(value => value.GroupId is CampaignSourceCireneMarmaricaGroupedDefinition.CireneId or CampaignSourceCireneMarmaricaGroupedDefinition.MarmaricaId)
            .Where(value => !value.DispositionConfirmed).Select(value => value.GroupId);
    public static CampaignPregameAxisDraft QuerySourceItalianDivisionsDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.SourceAuthority?.IsItalianDivisionsGrouped != true)
            throw new InvalidOperationException("Italian source world unavailable.");
        var authority = session.SourceAuthority;
        var choices = SourceItalianDivisionChoices(authority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("dispositionPolicy", "provisional-source-default-alternatives-unavailable");
            writer.WriteString("priorityStatus", "global-priority-incomplete"); writer.WriteStartArray("groups");
            foreach (var group in authority.GroupedWorld!.Groups.Where(value => value.GroupId is CampaignSourceCireneMarmaricaGroupedDefinition.CireneId or CampaignSourceCireneMarmaricaGroupedDefinition.MarmaricaId))
            {
                writer.WriteStartObject(); writer.WriteString("groupId", group.GroupId); writer.WriteString("locationId", group.LocationId);
                writer.WriteBoolean("dispositionConfirmed", group.DispositionConfirmed);
                writer.WriteNumber("remainingMotorPoints", authority.GroupedWorld.RemainingMotorPoints(group.GroupId));
                writer.WriteStartArray("members"); foreach (var member in group.Members)
                { writer.WriteStartObject(); writer.WriteString("elementId", member.ElementId); if (member.ParentId is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", member.ParentId); writer.WriteEndObject(); }
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            foreach (var id in choices) { writer.WriteStartObject(); writer.WriteString("kind", "confirm"); writer.WriteString("groupId", id); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), choices.Select(CampaignSourceAuthorityState.ConfirmItalianDivisionActionId));
    }
    public static ExerciseStepResult SubmitSourceItalianDivision(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.IsItalianDivisionsGrouped != true) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var selected = submission.Audience == CampaignActionAudience.Axis ? SourceItalianDivisionChoices(authority).SingleOrDefault(value => CampaignSourceAuthorityState.ConfirmItalianDivisionActionId(value) == submission.ActionId) : null;
        if (selected is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ConfirmItalianDivision(selected); var bytes = authority.SerializeItalianDivisionsGroupedEvent(next, selected);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceLibyanGroupedWorldAdmitted(ExerciseSession session) => session.SourceAuthority?.IsLibyanGrouped == true;
    public static ExerciseStepResult AdmitSourceLibyanGroupedWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { } authority || !authority.CanAdmitLibyanGrouped)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitLibyanGroupedWorld(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeLibyanGroupedEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitLibyanGroupedActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    private static IEnumerable<string> SourceLibyanChoices(CampaignSourceAuthorityState authority) => authority.GroupedWorld is null ? []
        : authority.GroupedWorld.Groups.Where(value => value.GroupId is "1-libyan-division" or "2-libyan-division")
            .Where(value => !value.DispositionConfirmed).Select(value => value.GroupId);
    public static CampaignPregameAxisDraft QuerySourceLibyanDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.SourceAuthority?.IsLibyanGrouped != true)
            throw new InvalidOperationException("Libyan source world unavailable.");
        var authority = session.SourceAuthority;
        var choices = SourceLibyanChoices(authority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("dispositionPolicy", "provisional-source-default-alternatives-unavailable");
            writer.WriteString("priorityStatus", "global-priority-incomplete"); writer.WriteStartArray("groups");
            foreach (var group in authority.GroupedWorld!.Groups.Where(value => value.GroupId is "1-libyan-division" or "2-libyan-division"))
            {
                writer.WriteStartObject(); writer.WriteString("groupId", group.GroupId); writer.WriteString("locationId", group.LocationId);
                writer.WriteBoolean("dispositionConfirmed", group.DispositionConfirmed);
                writer.WriteNumber("remainingMotorPoints", authority.GroupedWorld.RemainingMotorPoints(group.GroupId));
                writer.WriteStartArray("members"); foreach (var member in group.Members)
                { writer.WriteStartObject(); writer.WriteString("elementId", member.ElementId); if (member.ParentId is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", member.ParentId); writer.WriteEndObject(); }
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            foreach (var id in choices) { writer.WriteStartObject(); writer.WriteString("kind", "confirm"); writer.WriteString("groupId", id); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), choices.Select(CampaignSourceAuthorityState.ConfirmLibyanGroupActionId));
    }
    public static ExerciseStepResult SubmitSourceLibyan(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.IsLibyanGrouped != true) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var selected = submission.Audience == CampaignActionAudience.Axis ? SourceLibyanChoices(authority).SingleOrDefault(value => CampaignSourceAuthorityState.ConfirmLibyanGroupActionId(value) == submission.ActionId) : null;
        if (selected is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ConfirmLibyanGroup(selected); var bytes = authority.SerializeLibyanGroupedEvent(next, selected);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceMalettiGroupedWorldAdmitted(ExerciseSession session) => session.SourceAuthority?.IsMalettiGrouped == true;
    public static ExerciseStepResult AdmitSourceMalettiGroupedWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { } authority || !authority.CanAdmitMalettiGrouped)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitMalettiGroupedWorld(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeMalettiGroupedEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitMalettiGroupedActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceMatruhGroupedWorldAdmitted(ExerciseSession session) => session.SourceAuthority?.IsMatruhGrouped == true;
    public static ExerciseStepResult AdmitSourceMatruhGroupedWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { } authority || !authority.CanAdmitMatruhGrouped)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitMatruhGroupedWorld(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeMatruhGroupedEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitMatruhGroupedActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceFourthIndianWorldAdmitted(ExerciseSession session) => session.SourceAuthority?.GroupedWorld is not null;
    public static ExerciseStepResult AdmitSourceFourthIndianWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { } authority || !authority.CanAdmitFourthIndian)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitFourthIndianWorld(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeFourthIndianEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitFourthIndianActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    private static IEnumerable<(string Kind, string? Id, int Delta)> SourceFourthIndianChoices(CampaignSourceAuthorityState authority)
    {
        var group = authority.GroupedWorld?.Groups.SingleOrDefault(value => value.GroupId == "4-indian-division");
        if (group is null) yield break;
        if (!group.DispositionConfirmed) { yield return ("confirm", null, 0); yield break; }
        foreach (var member in group.Members.Where(value => value.HistoricalMotorPriority && value.InfantryToe is not null))
        {
            if (member.AssignedMotorPoints > 0) yield return ("adjust", member.ElementId, -1);
            if (member.AssignedMotorPoints < member.InfantryToe && authority.GroupedWorld!.RemainingMotorPoints(group.GroupId) > 0) yield return ("adjust", member.ElementId, 1);
        }
    }
    private static string FourthIndianChoiceId((string Kind, string? Id, int Delta) choice) => choice.Kind == "confirm"
        ? CampaignSourceAuthorityState.ConfirmFourthIndianActionId
        : CampaignSourceAuthorityState.FourthIndianMotorActionId(choice.Id!, choice.Delta);
    public static CampaignPregameAxisDraft QuerySourceFourthIndianDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceAuthority?.GroupedWorld is null)
            throw new InvalidOperationException("Fourth Indian source world is unavailable.");
        var authority = session.SourceAuthority;
        var group = authority.GroupedWorld.Groups.Single(value => value.GroupId == "4-indian-division");
        var choices = SourceFourthIndianChoices(authority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("horseRuling", CampaignSourceFourthIndianDefinition.HorseRuling);
            writer.WriteString("dispositionPolicy", "provisional-source-default-alternatives-unavailable");
            writer.WriteBoolean("dispositionConfirmed", group.DispositionConfirmed);
            writer.WriteString("priorityStatus", "global-priority-incomplete"); writer.WriteString("allocationMeaning", "parent-controlled-transport-duty-reservation"); writer.WriteNumber("remainingMotorPoints", authority.GroupedWorld.RemainingMotorPoints(group.GroupId));
            writer.WriteStartArray("members");
            foreach (var member in group.Members)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", member.ElementId);
                if (member.ParentId is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", member.ParentId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteStartArray("assignments");
            foreach (var member in group.Members.Where(value => value.AssignedMotorPoints > 0))
            { writer.WriteStartObject(); writer.WriteString("elementId", member.ElementId); writer.WriteNumber("points", member.AssignedMotorPoints); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject(); writer.WriteString("kind", choice.Kind);
                if (choice.Id is not null) { writer.WriteString("elementId", choice.Id); writer.WriteNumber("delta", choice.Delta); }
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), choices.Select(FourthIndianChoiceId));
    }
    public static ExerciseStepResult SubmitSourceFourthIndian(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.GroupedWorld is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var selected = submission.Audience == CampaignActionAudience.Commonwealth ? SourceFourthIndianChoices(authority).SingleOrDefault(choice => FourthIndianChoiceId(choice) == submission.ActionId) : default;
        if (selected.Kind is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var confirm = selected.Kind == "confirm";
        var next = confirm ? authority.ConfirmFourthIndianDisposition() : authority.AdjustFourthIndianMotor(selected.Id!, selected.Delta);
        var bytes = authority.SerializeFourthIndianEvent(next, selected.Id, selected.Delta, confirm);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceCwFreeGunsAdmitted(ExerciseSession session) => session.SourceAuthority?.CwFreeGuns is not null;
    public static bool IsSourceAxisMinorGarrisonsActivated(ExerciseSession session) =>
        (session.SourceAuthority ?? session.SourceTurn?.Source)?.CwFreeGunsWorld?.MinorGarrisonContent is not null;
    public static bool IsSourceAxisGarrisonRemainderActivated(ExerciseSession session) =>
        (session.SourceAuthority ?? session.SourceTurn?.Source)?.CwFreeGunsWorld?.GarrisonRemainderContent is not null;
    public static bool IsSourceCwFrontlineActivated(ExerciseSession session) =>
        (session.SourceAuthority ?? session.SourceTurn?.Source)?.CwFreeGunsWorld?.CwFrontlineContent is not null;
    public static ExerciseStepResult ActivateSourceCwFrontline(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanActivateCwFrontline: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.ActivateCwFrontline(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeCwFrontlineEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.ActivateCwFrontlineActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceGiarabubMotorPlacementPolicyAdmitted(ExerciseSession session) =>
        (session.SourceAuthority ?? session.SourceTurn?.Source)?.AxisMotorization.GiarabubPlacementAdmitted == true;

    public static ExerciseStepResult AdmitSourceGiarabubMotorPlacementPolicy(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanAdmitGiarabubMotorPlacementPolicy: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitGiarabubMotorPlacementPolicy();
        var bytes = authority.SerializeGiarabubMotorPlacementPolicyEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitGiarabubMotorPlacementPolicyActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static bool IsSourceGiarabubMotorMachineGunActivated(ExerciseSession session) =>
        (session.SourceAuthority ?? session.SourceTurn?.Source)?.CwFreeGunsWorld?.MotorMachineGunContent is not null;
    public static ExerciseStepResult ActivateSourceGiarabubMotorMachineGun(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanActivateGiarabubMotorMachineGun: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.ActivateGiarabubMotorMachineGun(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeGiarabubMotorMachineGunEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.ActivateGiarabubMotorMachineGunActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static ExerciseStepResult ActivateSourceAxisGarrisonRemainder(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanActivateAxisGarrisonRemainder: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.ActivateAxisGarrisonRemainder(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeAxisGarrisonRemainderEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.ActivateAxisGarrisonRemainderActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static ExerciseStepResult ActivateSourceAxisMinorGarrisons(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanActivateAxisMinorGarrisons: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.ActivateAxisMinorGarrisons(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeAxisMinorGarrisonsEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.ActivateAxisMinorGarrisonsActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static ExerciseStepResult AdmitSourceTobrukBardiaGarrison(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanAdmitSourceTobrukBardiaGarrison: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitSourceTobrukBardiaGarrison(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeSourceTobrukBardiaGarrisonAdmission(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitSourceTobrukBardiaGarrisonActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static ExerciseStepResult ActivateSourceTobrukBardiaGarrison(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanActivateSourceTobrukBardiaGarrison: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.ActivateSourceTobrukBardiaGarrison(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeSourceTobrukBardiaGarrisonEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.ActivateSourceTobrukBardiaGarrisonActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceBardiaGarrisonRemainderActivated(ExerciseSession session) =>
        (session.SourceAuthority ?? session.SourceTurn?.Source)?.CwFreeGunsWorld?.BardiaGarrisonRemainderContent is not null;
    public static ExerciseStepResult AdmitSourceBardiaGarrisonRemainder(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanAdmitSourceBardiaGarrisonRemainder: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitSourceBardiaGarrisonRemainder(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeSourceBardiaGarrisonRemainderAdmission(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitSourceBardiaGarrisonRemainderActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static ExerciseStepResult ActivateSourceBardiaGarrisonRemainder(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanActivateSourceBardiaGarrisonRemainder: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.ActivateSourceBardiaGarrisonRemainder(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeSourceBardiaGarrisonRemainderEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.ActivateSourceBardiaGarrisonRemainderActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceTripoliTankRegimentActivated(ExerciseSession session) =>
        (session.SourceAuthority ?? session.SourceTurn?.Source)?.CwFreeGunsWorld?.TripoliTankRegimentContent is not null;
    public static ExerciseStepResult AdmitSourceTripoliTankRegiment(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanAdmitSourceTripoliTankRegiment: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitSourceTripoliTankRegiment(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeSourceTripoliTankRegimentAdmission(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitSourceTripoliTankRegimentActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static ExerciseStepResult ActivateSourceTripoliTankRegiment(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanActivateSourceTripoliTankRegiment: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.ActivateSourceTripoliTankRegiment(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeSourceTripoliTankRegimentEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.ActivateSourceTripoliTankRegimentActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceCwAttachedArtilleryActivated(ExerciseSession session) =>
        (session.SourceAuthority ?? session.SourceTurn?.Source)?.CwFreeGunsWorld?.CwAttachedArtilleryContent is not null;
    public static ExerciseStepResult AdmitSourceCwAttachedArtillery(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanAdmitSourceCwAttachedArtillery: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitSourceCwAttachedArtillery(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeSourceCwAttachedArtilleryAdmission(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitSourceCwAttachedArtilleryActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static ExerciseStepResult ActivateSourceCwAttachedArtillery(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanActivateSourceCwAttachedArtillery: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.ActivateSourceCwAttachedArtillery(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeSourceCwAttachedArtilleryEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.ActivateSourceCwAttachedArtilleryActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool CanPromoteSourceCwFreeGunsWorld(ExerciseSession session) => session.SourceAuthority?.CanPromoteCwFreeGunsWorld == true;
    public static ExerciseStepResult AdmitSourceCwFreeGuns(ExerciseSession session) => ApplySourceCwFreeGunsSystem(session, false);
    public static ExerciseStepResult PromoteSourceCwFreeGunsWorld(ExerciseSession session) => ApplySourceCwFreeGunsSystem(session, true);
    private static ExerciseStepResult ApplySourceCwFreeGunsSystem(ExerciseSession session, bool promote)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority?.FinalizedOwners != 0 && session.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (session.SourceAuthority is not { } authority || (promote ? !authority.CanPromoteCwFreeGunsWorld : authority.CwPriorityWorld is null || authority.CwFreeGuns is not null))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = promote ? authority.PromoteCwFreeGunsWorld() : authority.AdmitCwFreeGuns(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeCwFreeGunsEvent(next, promote ? "promote" : null);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, promote ? CampaignSourceAuthorityState.PromoteCwFreeGunsWorldActionId : CampaignSourceAuthorityState.AdmitCwFreeGunsActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    private static IEnumerable<(string ElementId, string LocationId)> SourceCwFreeGunChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.CwFreeGuns is not { } setup || authority.Battlefield is not { } battlefield) yield break;
        foreach (var id in setup.PendingIds) foreach (var location in setup.ValidDestinations(id, battlefield)) yield return (id, location);
    }
    public static CampaignPregameAxisDraft QuerySourceCwFreeGunsDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceAuthority?.CwFreeGuns is not { } setup)
            throw new InvalidOperationException("Commonwealth free guns unavailable.");
        var choices = SourceCwFreeGunChoices(session.SourceAuthority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("siteCoverage", "restricted-three-source-cairo-hexes-not-all-map-d-e");
            writer.WriteStartArray("units");
            foreach (var unit in CampaignSourceCwFreeGuns.Units)
            { writer.WriteStartObject(); writer.WriteString("elementId", unit.ElementId); var placement = setup.Placements.SingleOrDefault(value => value.ElementId == unit.ElementId); if (placement is null) writer.WriteNull("locationId"); else writer.WriteString("locationId", placement.LocationId); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            foreach (var choice in choices) { writer.WriteStartObject(); writer.WriteString("elementId", choice.ElementId); writer.WriteString("locationId", choice.LocationId); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(session.SourceAuthority.StateVersion, stream.ToArray(), choices.Select(choice => CampaignSourceAuthorityState.CwFreeGunPlacementActionId(choice.ElementId, choice.LocationId)));
    }
    public static ExerciseStepResult PlaceSourceCwFreeGun(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.CwFreeGuns is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var choice = submission.Audience == CampaignActionAudience.Commonwealth ? SourceCwFreeGunChoices(authority).SingleOrDefault(value => CampaignSourceAuthorityState.CwFreeGunPlacementActionId(value.ElementId, value.LocationId) == submission.ActionId) : default;
        if (choice.ElementId is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.PlaceCwFreeGun(choice.ElementId, choice.LocationId); var bytes = authority.SerializeCwFreeGunsEvent(next, "place", choice.ElementId, choice.LocationId);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion), [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    private static IEnumerable<(string Kind, string? Location, int Delta)> SourceUnattachedCargoChoices(CampaignSourceAuthorityState authority, bool axis)
    {
        var cargo = axis ? authority.AxisUnattachedCargo : authority.CommonwealthUnattachedCargo;
        if (cargo is null)
        {
            try { _ = CampaignSourceUnattachedCargo.Create(authority, axis ? LandSide.Axis : LandSide.Commonwealth); }
            catch (InvalidOperationException) { yield break; }
            yield return ("begin", null, 0); yield break;
        }
        foreach (var location in cargo.Locations(authority))
            foreach (var delta in cargo.ValidDeltas(authority, location)) yield return ("adjust", location, delta);
        if (!cargo.IsFinished) yield return ("finish", null, 0);
    }
    public static CampaignPregameAxisDraft QuerySourceUnattachedCargoDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { } authority || audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            throw new InvalidOperationException("Source initial cargo unavailable.");
        var axis = audience == CampaignActionAudience.Axis;
        var cargo = axis ? authority.AxisUnattachedCargo : authority.CommonwealthUnattachedCargo;
        var choices = SourceUnattachedCargoChoices(authority, axis).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("stackPolicy", "provisional-loaded-units-count-including-other-map-hex-cap3");
            writer.WriteBoolean("siteCoverageComplete", false); writer.WriteBoolean("begun", cargo is not null); writer.WriteBoolean("finished", cargo?.IsFinished == true);
            writer.WriteStartArray("loads");
            if (cargo is not null) foreach (var item in cargo.Loads) { writer.WriteStartObject(); writer.WriteString("locationId", item.Key); writer.WriteNumber("regularSupplyUnits", item.Value); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            foreach (var choice in choices) { writer.WriteStartObject(); writer.WriteString("kind", choice.Kind); if (choice.Location is null) writer.WriteNull("locationId"); else writer.WriteString("locationId", choice.Location); writer.WriteNumber("delta", choice.Delta); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(),
            choices.Select(choice => CampaignSourceAuthorityState.UnattachedCargoActionId(axis, choice.Kind, choice.Location, choice.Delta)));
    }
    public static ExerciseStepResult SubmitSourceUnattachedCargo(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (submission.Audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var axis = submission.Audience == CampaignActionAudience.Axis;
        var choice = SourceUnattachedCargoChoices(authority, axis).SingleOrDefault(value =>
            CampaignSourceAuthorityState.UnattachedCargoActionId(axis, value.Kind, value.Location, value.Delta) == submission.ActionId);
        if (choice.Kind is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ApplyUnattachedCargo(axis, choice.Kind, choice.Location, choice.Delta);
        var bytes = authority.SerializeUnattachedCargoEvent(next, axis, choice.Kind, choice.Location, choice.Delta);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceCompactCheckpointAdmitted(ExerciseSession session) => session.SourceAuthority?.CompactCheckpoint == true;
    public static ExerciseStepResult AdmitSourceCompactCheckpoint(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CompactCheckpoint: false } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitCompactCheckpoint(); var bytes = authority.SerializeCompactCheckpointEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitCompactCheckpointActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool CanPromoteSourceCwPriorityWorld(ExerciseSession session) => session.SourceAuthority?.CanPromoteCwPriorityWorld == true;
    public static ExerciseStepResult PromoteSourceCwPriorityWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanPromoteCwPriorityWorld: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.PromoteCwPriorityWorld(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeCwPriorityWorldEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.PromoteCwPriorityWorldActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceCwPriorityCatalogAdmitted(ExerciseSession session) => session.SourceAuthority?.CwPriorityCatalog is not null;
    public static ExerciseStepResult AdmitSourceCwPriorityCatalog(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { AttachedInitialLoads: not null, CwPriorityCatalog: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitCwPriorityCatalog(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeCwPriorityCatalogEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitCwPriorityCatalogActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    private static IEnumerable<(string ElementId, int Delta)> SourceCwPriorityCatalogChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.CwPriorityCatalog is not { } catalog) yield break;
        foreach (var target in CampaignSourceCwPriorityCatalog.Targets)
            foreach (var delta in catalog.ValidDeltas(authority, target.ElementId)) yield return (target.ElementId, delta);
    }
    public static CampaignPregameAxisDraft QuerySourceCwPriorityCatalogDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceAuthority?.CwPriorityCatalog is not { } catalog)
            throw new InvalidOperationException("Commonwealth source priority catalog unavailable.");
        var choices = SourceCwPriorityCatalogChoices(session.SourceAuthority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteBoolean("globalPriorityComplete", false); writer.WriteStartArray("targets");
            foreach (var target in CampaignSourceCwPriorityCatalog.Targets)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", target.ElementId); writer.WriteString("poolId", target.PoolId);
                writer.WriteNumber("infantryToe", target.InfantryToe); writer.WriteNumber("assignedPoints", catalog.AssignedPoints(session.SourceAuthority, target.ElementId));
                writer.WriteNumber("remainingPoolPoints", catalog.Remaining(session.SourceAuthority, target.PoolId)); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            foreach (var choice in choices) { writer.WriteStartObject(); writer.WriteString("elementId", choice.ElementId); writer.WriteNumber("delta", choice.Delta); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(session.SourceAuthority.StateVersion, stream.ToArray(),
            choices.Select(choice => CampaignSourceAuthorityState.CwPriorityCatalogActionId(choice.ElementId, choice.Delta)));
    }
    public static ExerciseStepResult SubmitSourceCwPriorityCatalog(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.CwPriorityCatalog is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var choice = submission.Audience == CampaignActionAudience.Commonwealth ? SourceCwPriorityCatalogChoices(authority).SingleOrDefault(value =>
            CampaignSourceAuthorityState.CwPriorityCatalogActionId(value.ElementId, value.Delta) == submission.ActionId) : default;
        if (choice.ElementId is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.AdjustCwPriorityCatalog(choice.ElementId, choice.Delta); var bytes = authority.SerializeCwPriorityCatalogEvent(next, choice.ElementId, choice.Delta);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceGt2CorrectionAdmitted(ExerciseSession session) => session.SourceAuthority?.Gt2Correction is not null;
    public static ExerciseStepResult AdmitSourceGt2Correction(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { Gt2Correction: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitGt2Correction(); var bytes = authority.SerializeGt2CorrectionEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitGt2CorrectionActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    private static IEnumerable<(string Kind, int Lane, int Stage, int Delta)> SourceGt2CorrectionChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.Gt2Correction is not { } correction) yield break;
        foreach (var lane in new[] { 2, 3, 6 })
            for (var stage = 1; stage <= 3; stage++)
                foreach (var delta in correction.ValidDeltas(lane, stage)) yield return ("adjust", lane, stage, delta);
        if (correction.CanFinish) yield return ("finish", 0, 0, 0);
    }
    public static CampaignPregameAxisDraft QuerySourceGt2CorrectionDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.SourceAuthority?.Gt2Correction is not { } correction)
            throw new InvalidOperationException("Axis GT2 correction unavailable.");
        var choices = SourceGt2CorrectionChoices(session.SourceAuthority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("ruling", "provisional-positive-gt2-roll-requires-one-planned-supply");
            writer.WriteString("cargoCompleteness", "supply-only-other-cargo-unresolved"); writer.WriteNumber("maximumSupply", correction.MaximumSupply);
            writer.WriteNumber("allocatedSupply", correction.AllocatedSupply); writer.WriteBoolean("finished", correction.IsFinished);
            writer.WriteStartArray("allocations");
            foreach (var lane in new[] { 2, 3, 6 }) for (var stage = 1; stage <= 3; stage++)
            { var count = correction.Quantity(lane, stage); if (count == 0) continue; writer.WriteStartObject(); writer.WriteNumber("lane", lane); writer.WriteNumber("stage", stage); writer.WriteNumber("quantity", count); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            foreach (var choice in choices) { writer.WriteStartObject(); writer.WriteString("kind", choice.Kind); writer.WriteNumber("lane", choice.Lane); writer.WriteNumber("stage", choice.Stage); writer.WriteNumber("delta", choice.Delta); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(session.SourceAuthority.StateVersion, stream.ToArray(),
            choices.Select(choice => CampaignSourceAuthorityState.Gt2CorrectionActionId(choice.Kind, choice.Lane, choice.Stage, choice.Delta)));
    }
    public static ExerciseStepResult SubmitSourceGt2Correction(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.Gt2Correction is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var choice = submission.Audience == CampaignActionAudience.Axis ? SourceGt2CorrectionChoices(authority).SingleOrDefault(value =>
            CampaignSourceAuthorityState.Gt2CorrectionActionId(value.Kind, value.Lane, value.Stage, value.Delta) == submission.ActionId) : default;
        if (choice.Kind is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ApplyGt2Correction(choice.Kind, choice.Lane, choice.Stage, choice.Delta);
        var bytes = authority.SerializeGt2CorrectionEvent(next, choice.Kind, choice.Lane, choice.Stage, choice.Delta);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool CanPromoteSourceMatruhDetachedWorld(ExerciseSession session) => session.SourceAuthority?.CanPromoteMatruhDetachedWorld == true;
    public static ExerciseStepResult PromoteSourceMatruhDetachedWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanPromoteMatruhDetachedWorld: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.PromoteMatruhDetachedWorld(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeMatruhDetachedWorldEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.PromoteMatruhDetachedWorldActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceMatruhFormationAdmitted(ExerciseSession session) => session.SourceAuthority?.MatruhFormation is not null;
    public static ExerciseStepResult AdmitSourceMatruhFormation(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { MalettiHierarchy.Confirmed: true, Battlefield: not null, MatruhFormation: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitMatruhFormation(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeMatruhFormationEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitMatruhFormationActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    private static IEnumerable<(string Kind, string? Id)> SourceMatruhFormationChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.MatruhFormation is not { } group) yield break;
        group.Validate(authority);
        if (!group.Confirmed) yield return ("confirm", null);
        else foreach (var member in CampaignSourceMatruhFormation.Members)
            if (group.CanDetach(authority, member.ElementId)) yield return ("detach", member.ElementId);
    }
    public static CampaignPregameAxisDraft QuerySourceMatruhFormationDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceAuthority?.MatruhFormation is not { } group)
            throw new InvalidOperationException("Commonwealth Matruh formation unavailable.");
        var choices = SourceMatruhFormationChoices(session.SourceAuthority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("assignmentRuling", "provisional-errata-buffs-hampshire-assigned");
            writer.WriteString("essexDurhamAssignment", "scenario-vs-oa-unresolved"); writer.WriteBoolean("confirmed", group.Confirmed);
            if (group.DetachedId is null) writer.WriteNull("detachedId"); else writer.WriteString("detachedId", group.DetachedId);
            writer.WriteStartArray("attachedIds"); foreach (var id in group.AttachedIds) writer.WriteStringValue(id); writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var choice in choices) { writer.WriteStartObject(); writer.WriteString("kind", choice.Kind); if (choice.Id is null) writer.WriteNull("elementId"); else writer.WriteString("elementId", choice.Id); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(session.SourceAuthority.StateVersion, stream.ToArray(),
            choices.Select(choice => CampaignSourceAuthorityState.MatruhFormationActionId(choice.Kind, choice.Id)));
    }
    public static ExerciseStepResult SubmitSourceMatruhFormation(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.MatruhFormation is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var choice = submission.Audience == CampaignActionAudience.Commonwealth ? SourceMatruhFormationChoices(authority).SingleOrDefault(value =>
            CampaignSourceAuthorityState.MatruhFormationActionId(value.Kind, value.Id) == submission.ActionId) : default;
        if (choice.Kind is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ApplyMatruhFormation(choice.Kind, choice.Id); var bytes = authority.SerializeMatruhFormationEvent(next, choice.Kind, choice.Id);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool CanPromoteSourceCertifiedMalettiWorld(ExerciseSession session) => session.SourceAuthority?.CanPromoteCertifiedMalettiWorld == true;
    public static ExerciseStepResult PromoteSourceCertifiedMalettiWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanPromoteCertifiedMalettiWorld: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.PromoteCertifiedMalettiWorld(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeCertifiedMalettiWorldEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.PromoteCertifiedMalettiWorldActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceMalettiHierarchyAdmitted(ExerciseSession session) => session.SourceAuthority?.MalettiHierarchy is not null;
    public static ExerciseStepResult AdmitSourceMalettiHierarchy(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { AxisCorpsArtilleryWorld: not null, MalettiHierarchy: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignSourceAuthorityState next;
        try { next = authority.AdmitMalettiHierarchy(); }
        catch (InvalidOperationException) { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var bytes = authority.SerializeMalettiHierarchyEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitMalettiHierarchyActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    private static IEnumerable<(string Kind, string? Id)> SourceMalettiHierarchyChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.MalettiHierarchy is not { } hierarchy) yield break;
        hierarchy.Validate(authority);
        if (!hierarchy.Confirmed) yield return ("confirm", null);
        else foreach (var target in CampaignSourceAxisPriorityMotorization.Targets)
            if (hierarchy.CanDetach(authority, target)) yield return ("detach", target);
    }
    public static CampaignPregameAxisDraft QuerySourceMalettiHierarchyDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.SourceAuthority?.MalettiHierarchy is not { } hierarchy)
            throw new InvalidOperationException("Axis Maletti hierarchy unavailable.");
        var choices = SourceMalettiHierarchyChoices(session.SourceAuthority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("ruling", "provisional-scenario-over-oa-lx-whole-bardia");
            writer.WriteString("stackPolicy", "c3617-limit6-division5-plus-at-most-one-battalion1");
            writer.WriteBoolean("confirmed", hierarchy.Confirmed);
            if (hierarchy.DetachedId is null) writer.WriteNull("detachedId"); else writer.WriteString("detachedId", hierarchy.DetachedId);
            writer.WriteStartArray("choices");
            foreach (var choice in choices) { writer.WriteStartObject(); writer.WriteString("kind", choice.Kind); if (choice.Id is null) writer.WriteNull("elementId"); else writer.WriteString("elementId", choice.Id); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(session.SourceAuthority.StateVersion, stream.ToArray(),
            choices.Select(choice => CampaignSourceAuthorityState.MalettiHierarchyActionId(choice.Kind, choice.Id)));
    }
    public static ExerciseStepResult SubmitSourceMalettiHierarchy(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.MalettiHierarchy is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var choice = submission.Audience == CampaignActionAudience.Axis ? SourceMalettiHierarchyChoices(authority).SingleOrDefault(value =>
            CampaignSourceAuthorityState.MalettiHierarchyActionId(value.Kind, value.Id) == submission.ActionId) : default;
        if (choice.Kind is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ApplyMalettiHierarchy(choice.Kind, choice.Id); var bytes = authority.SerializeMalettiHierarchyEvent(next, choice.Kind, choice.Id);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool CanPromoteSourceAxisCorpsWorld(ExerciseSession session) => session.SourceAuthority?.CanPromoteAxisCorpsWorld == true;
    public static ExerciseStepResult PromoteSourceAxisCorpsWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanPromoteAxisCorpsWorld: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.PromoteAxisCorpsWorld(); var bytes = authority.SerializeAxisCorpsWorldEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.PromoteAxisCorpsWorldActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    public static bool IsSourceAxisCorpsSetupAdmitted(ExerciseSession session) => session.SourceAuthority?.AxisCorpsArtillery is not null;
    public static ExerciseStepResult AdmitSourceAxisCorpsSetup(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not
            {
                AxisFreeArtilleryWorld: not null, Battlefield: not null, AxisCorpsArtillery: null,
                AxisFreeArtillery.PendingIds.Count: 0, CanPromoteAxisFreeWorld: false
            } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitAxisCorpsSetup(); var bytes = authority.SerializeAxisCorpsSetupEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitAxisCorpsSetupActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }
    private static IEnumerable<(string ElementId, string LocationId)> SourceAxisCorpsChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.AxisCorpsArtillery is not { } setup || authority.Battlefield is not { } battlefield) yield break;
        foreach (var id in setup.PendingIds)
            foreach (var location in setup.ValidDestinations(id, battlefield)) yield return (id, location);
    }
    public static CampaignPregameAxisDraft QuerySourceAxisCorpsSetupDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.SourceAuthority?.AxisCorpsArtillery is not { } setup)
            throw new InvalidOperationException("Axis corps setup unavailable.");
        var choices = SourceAxisCorpsChoices(session.SourceAuthority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("siteCoverage", "restricted-not-all-libya");
            writer.WriteString("xxviIdentity", "provisional-unique-xxvi-corps-artillery-counter"); writer.WriteStartArray("units");
            foreach (var unit in CampaignSourceAxisCorpsArtillery.Units)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", unit.ElementId); writer.WriteString("sourceLabel", unit.SourceLabel);
                var placement = setup.Placements.SingleOrDefault(value => value.ElementId == unit.ElementId);
                if (placement is null) writer.WriteNull("locationId"); else writer.WriteString("locationId", placement.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            foreach (var choice in choices) { writer.WriteStartObject(); writer.WriteString("elementId", choice.ElementId); writer.WriteString("locationId", choice.LocationId); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(session.SourceAuthority.StateVersion, stream.ToArray(),
            choices.Select(choice => CampaignSourceAuthorityState.AxisCorpsPlacementActionId(choice.ElementId, choice.LocationId)));
    }
    public static ExerciseStepResult PlaceSourceAxisCorpsUnit(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.AxisCorpsArtillery is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var choice = submission.Audience == CampaignActionAudience.Axis ? SourceAxisCorpsChoices(authority).SingleOrDefault(value =>
            CampaignSourceAuthorityState.AxisCorpsPlacementActionId(value.ElementId, value.LocationId) == submission.ActionId) : default;
        if (choice.ElementId is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.PlaceAxisCorpsUnit(choice.ElementId, choice.LocationId); var bytes = authority.SerializeAxisCorpsSetupEvent(next, choice.ElementId, choice.LocationId);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static bool CanPromoteSourceAxisFreeWorld(ExerciseSession session) => session.SourceAuthority?.CanPromoteAxisFreeWorld == true;
    public static ExerciseStepResult PromoteSourceAxisFreeWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CanPromoteAxisFreeWorld: true } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.PromoteAxisFreeWorld(); var bytes = authority.SerializeAxisFreeWorldEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.PromoteAxisFreeWorldActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static bool IsSourceAxisFreeSetupAdmitted(ExerciseSession session) => session.SourceAuthority?.AxisFreeArtillery is not null;

    public static ExerciseStepResult AdmitSourceAxisFreeSetup(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { InitialWrecks: not null, Battlefield: not null, AxisFreeArtillery: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitAxisFreeSetup(); var bytes = authority.SerializeAxisFreeSetupEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitAxisFreeSetupActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    private static IEnumerable<(string ElementId, string LocationId)> SourceAxisFreeChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.AxisFreeArtillery is not { } setup || authority.Battlefield is not { } battlefield) yield break;
        foreach (var id in setup.PendingIds)
            foreach (var location in setup.ValidDestinations(id, battlefield)) yield return (id, location);
    }

    public static CampaignPregameAxisDraft QuerySourceAxisFreeSetupDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.SourceAuthority?.AxisFreeArtillery is not { } setup)
            throw new InvalidOperationException("Axis free setup unavailable.");
        var choices = SourceAxisFreeChoices(session.SourceAuthority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("siteCoverage", "restricted-not-all-libya");
            writer.WriteStartArray("units");
            foreach (var unit in CampaignSourceAxisFreeArtillery.Units)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", unit.ElementId); writer.WriteString("sourceLabel", unit.SourceLabel);
                var placement = setup.Placements.SingleOrDefault(value => value.ElementId == unit.ElementId);
                if (placement is null) writer.WriteNull("locationId"); else writer.WriteString("locationId", placement.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            foreach (var choice in choices) { writer.WriteStartObject(); writer.WriteString("elementId", choice.ElementId); writer.WriteString("locationId", choice.LocationId); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(session.SourceAuthority.StateVersion, stream.ToArray(),
            choices.Select(choice => CampaignSourceAuthorityState.AxisFreePlacementActionId(choice.ElementId, choice.LocationId)));
    }

    public static ExerciseStepResult PlaceSourceAxisFreeUnit(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.AxisFreeArtillery is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var choice = submission.Audience == CampaignActionAudience.Axis ? SourceAxisFreeChoices(authority).SingleOrDefault(value =>
            CampaignSourceAuthorityState.AxisFreePlacementActionId(value.ElementId, value.LocationId) == submission.ActionId) : default;
        if (choice.ElementId is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.PlaceAxisFreeUnit(choice.ElementId, choice.LocationId);
        var bytes = authority.SerializeAxisFreeSetupEvent(next, choice.ElementId, choice.LocationId);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static bool IsSourceInitialWrecksAdmitted(ExerciseSession session) => session.SourceAuthority?.InitialWrecks is not null;

    public static ExerciseStepResult AdmitSourceInitialWrecks(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { AttachedInitialLoads: not null, InitialWrecks: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitInitialWrecks();
        var bytes = authority.SerializeInitialWreckEvent(next);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System, CampaignSourceAuthorityState.AdmitInitialWrecksActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    private static IEnumerable<(string Id, string Location)> InitialWreckChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.InitialWrecks is not { } wrecks) yield break;
        foreach (var id in wrecks.PendingIds)
            foreach (var location in wrecks.ValidDestinations(id)) yield return (id, location);
    }

    public static CampaignPregameAxisDraft QuerySourceInitialWrecksDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceAuthority?.InitialWrecks is not { } wrecks)
            throw new InvalidOperationException("Commonwealth initial wreck setup unavailable.");
        var choices = InitialWreckChoices(session.SourceAuthority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete"); writer.WriteString("repairStatus", "not-adjudicated");
            writer.WriteStartArray("stock");
            foreach (var unit in CampaignSourceInitialWrecks.Stocks)
            {
                writer.WriteStartObject(); writer.WriteString("id", unit.Id); writer.WriteString("vehicleTypeId", unit.VehicleTypeId);
                var placement = wrecks.Placements.SingleOrDefault(value => value.Id == unit.Id);
                if (placement is null) writer.WriteNull("locationId"); else writer.WriteString("locationId", placement.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteStartArray("choices");
            foreach (var choice in choices) { writer.WriteStartObject(); writer.WriteString("id", choice.Id); writer.WriteString("locationId", choice.Location); writer.WriteEndObject(); }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(session.SourceAuthority.StateVersion, stream.ToArray(),
            choices.Select(choice => CampaignSourceAuthorityState.InitialWreckPlacementActionId(choice.Id, choice.Location)));
    }

    public static ExerciseStepResult PlaceSourceInitialWreck(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.InitialWrecks is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var choice = submission.Audience == CampaignActionAudience.Commonwealth ? InitialWreckChoices(authority).SingleOrDefault(value =>
            CampaignSourceAuthorityState.InitialWreckPlacementActionId(value.Id, value.Location) == submission.ActionId) : default;
        if (choice.Id is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.PlaceInitialWreck(choice.Id, choice.Location);
        var bytes = authority.SerializeInitialWreckEvent(next, choice.Id, choice.Location);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, session.CurrentContacts.AdvancePregameSource(authority.StateVersion),
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static bool IsSourceAttachedInitialLoadsAdmitted(ExerciseSession session) => session.SourceAuthority?.AttachedInitialLoads is not null;

    public static ExerciseStepResult AdmitSourceAttachedInitialLoads(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { ExpandedIndependentWorld: not null, AttachedInitialLoads: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitAttachedInitialLoads();
        var bytes = authority.SerializeAttachedInitialLoadsEvent(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.AdmitAttachedInitialLoadsActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    private static IEnumerable<(string Kind, string ElementId)> AttachedInitialLoadChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.AttachedInitialLoads is not { } loads || authority.PriorityMotorization is not { } priority) yield break;
        foreach (var target in CampaignSourcePriorityMotorization.Targets)
        {
            var id = target.ElementId;
            if (loads.CanAttach(id, priority, authority.CommonwealthMotorization)) yield return ("attach", id);
            if (loads.CanDetach(id)) yield return ("detach", id);
            if (loads.CanLoad(id)) { yield return ("ammo", id); yield return ("fuel", id); }
            if (loads.CanClearLoad(id, "ammo")) yield return ("unload-ammo", id);
            if (loads.CanClearLoad(id, "fuel")) yield return ("unload-fuel", id);
        }
        if (authority.CwFreeGunsWorld?.CwFrontlineContent is null) yield break;
        if (loads.FrontlineFinished) { yield return ("frontline-reopen", "frontline"); yield break; }
        foreach (var target in CampaignSourceAttachedInitialLoads.FrontlineTargets)
        {
            var id = target.ElementId;
            if (loads.CanAttachFrontline(id, authority.CommonwealthMotorization)) yield return ("frontline-attach", id);
            if (loads.CanDetach(id)) yield return ("frontline-detach", id);
            if (loads.CanLoad(id)) { yield return ("frontline-ammo", id); yield return ("frontline-fuel", id); }
            if (loads.CanClearLoad(id, "ammo")) yield return ("frontline-unload-ammo", id);
            if (loads.CanClearLoad(id, "fuel")) yield return ("frontline-unload-fuel", id);
        }
        if (loads.CanFinishFrontline(authority.CommonwealthMotorization)) yield return ("frontline-finish", "frontline");
    }

    public static CampaignPregameAxisDraft QuerySourceAttachedInitialLoadsDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceAuthority?.AttachedInitialLoads is not { } loads)
            throw new InvalidOperationException("Commonwealth initial loads are unavailable.");
        var choices = AttachedInitialLoadChoices(session.SourceAuthority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", session.SourceAuthority.CwFreeGunsWorld?.CwFrontlineContent is null ? 1 : 2);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteString("globalPriorityStatus", "incomplete");
            if (session.SourceAuthority.CwFreeGunsWorld?.CwFrontlineContent is not null)
            {
                writer.WriteBoolean("frontlineFinished", loads.FrontlineFinished);
                writer.WriteStartArray("frontlinePools");
                foreach (var target in CampaignSourceAttachedInitialLoads.FrontlineTargets)
                {
                    writer.WriteStartObject(); writer.WriteString("elementId", target.ElementId);
                    writer.WriteString("poolId", target.PoolId);
                    writer.WriteNumber("totalPoints", session.SourceAuthority.CommonwealthMotorization.Pools.Single(value => value.Id == target.PoolId).Points);
                    writer.WriteNumber("assignedPoints", loads.Attached(target.ElementId)); writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            writer.WriteStartArray("targets");
            foreach (var target in loads.Targets)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", target.ElementId);
                writer.WriteString("poolId", target.PoolId);
                writer.WriteNumber("attachedPoints", target.AttachedPoints);
                writer.WriteNumber("ammoPoints", target.AmmoPoints);
                writer.WriteNumber("fuelPoints", target.FuelPoints * 3);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject(); writer.WriteString("kind", choice.Kind); writer.WriteString("elementId", choice.ElementId); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(session.SourceAuthority.StateVersion, stream.ToArray(),
            choices.Select(value => CampaignSourceAuthorityState.AttachedInitialLoadsActionId(value.Kind, value.ElementId)));
    }

    public static ExerciseStepResult SubmitSourceAttachedInitialLoads(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.AttachedInitialLoads is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var choice = submission.Audience == CampaignActionAudience.Commonwealth ? AttachedInitialLoadChoices(authority).SingleOrDefault(value =>
            CampaignSourceAuthorityState.AttachedInitialLoadsActionId(value.Kind, value.ElementId) == submission.ActionId) : default;
        if (choice.ElementId is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ApplyAttachedInitialLoads(choice.Kind, choice.ElementId);
        var bytes = authority.SerializeAttachedInitialLoadsEvent(next, choice.Kind, choice.ElementId);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static bool CanPromoteSourceExpandedIndependent(ExerciseSession session) =>
        session.SourceAuthority is { CanPromoteExpandedIndependent: true } authority && authority.AxisFreeArtilleryWorld is null
            && !HasUncertifiedPendingMaletti(authority);

    public static ExerciseStepResult PromoteSourceExpandedIndependent(ExerciseSession session)
        => PromoteSourceExpandedIndependentCore(session, false);
    internal static ExerciseStepResult ReplaySourceExpandedIndependentPromotion(ExerciseSession session)
        => session.SourceAuthority?.AxisFreeArtillery is not null
            ? ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority)
            : PromoteSourceExpandedIndependentCore(session, true);
    private static ExerciseStepResult PromoteSourceExpandedIndependentCore(ExerciseSession session, bool replay)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority?.FinalizedOwners != 0 && session.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (session.SourceAuthority is not { CanPromoteExpandedIndependent: true } authority
            || (!replay && (authority.AxisFreeArtilleryWorld is not null || HasUncertifiedPendingMaletti(authority))))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.PromoteExpandedIndependent();
        var bytes = authority.SerializeExpandedIndependentPromotion(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.PromoteExpandedIndependentActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult AdmitSourceLocalMap(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { IndependentWorld: not null, LocalMapWorld: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitLocalMap();
        var bytes = authority.SerializeLocalMapAdmission(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.AdmitLocalMapActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult PromoteSourceIndependent(ExerciseSession session)
        => PromoteSourceIndependentCore(session, false);
    internal static ExerciseStepResult ReplaySourceIndependentPromotion(ExerciseSession session)
        => session.SourceAuthority?.AxisFreeArtillery is not null
            ? ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority)
            : PromoteSourceIndependentCore(session, true);
    private static ExerciseStepResult PromoteSourceIndependentCore(ExerciseSession session, bool replay)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority?.FinalizedOwners != 0 && session.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (session.SourceAuthority is not { CanPromoteIndependent: true } authority
            || (!replay && (authority.AxisFreeArtilleryWorld is not null || HasUncertifiedPendingMaletti(authority))))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.PromoteIndependent();
        var bytes = authority.SerializeIndependentPromotion(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.PromoteIndependentActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult PromoteSourceMaletti(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
    }

    internal static ExerciseStepResult ReplaySourceMalettiPromotion(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority?.FinalizedOwners != 0 && session.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (session.SourceAuthority is not { CanPromoteMaletti: true, AxisFreeArtillery: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.PromoteMaletti();
        var bytes = authority.SerializeMalettiPromotion(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.PromoteMalettiActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult AdmitSourceCairoWorld(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CairoSetup: { PendingIds.Count: 0 }, CairoWorld: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitCairoWorld();
        var bytes = authority.SerializeCairoWorldAdmission(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.AdmitCairoWorldActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult AdmitSourceCairoSetup(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { Battlefield: not null, CairoSetup: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitCairoSetup();
        var bytes = authority.SerializeCairoSetupAdmission(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.AdmitCairoSetupActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult AdmitSourceBattlefield(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { AxisGarrisons: not null, Battlefield: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitBattlefield();
        var bytes = authority.SerializeBattlefieldAdmission(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.AdmitBattlefieldActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult AdmitSourceAxisMinorGarrisons(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { AxisPriorityMotorization: not null, AxisGarrisons: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitAxisGarrisons();
        var bytes = authority.SerializeAxisGarrisonsAdmission(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.AdmitAxisGarrisonsActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult AdmitSourceAxisPriorityMotorization(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { PriorityMotorization: not null, AxisPriorityMotorization: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitAxisPriorityMotorization();
        var bytes = authority.SerializeAxisPriorityAdmission(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.AdmitAxisPriorityActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult AdmitSourcePriorityMotorization(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { CommonwealthSupplement: not null, PriorityMotorization: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitPriorityMotorization();
        var bytes = authority.SerializePriorityMotorizationAdmission(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.AdmitPriorityMotorizationActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStepResult AdmitSourceCommonwealthSupplement(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { AxisSupplement: not null, CommonwealthSupplement: null } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitCommonwealthSupplement();
        var bytes = authority.SerializeCommonwealthSupplementEvent(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.AdmitCommonwealthSupplementActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    /// <summary>System migration to a source-bound expanded Axis roster; no player choice or game clock advance.</summary>
    public static ExerciseStepResult AdmitSourceAxisSupplement(ExerciseSession session)
    {
        if (session?.SourceAuthority?.FinalizedOwners != 0 && session?.SourceAuthority is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { } authority || authority.AxisSupplement is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var next = authority.AdmitAxisSupplement();
        var bytes = authority.SerializeAxisSupplementEvent(next);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, CampaignActionAudience.System,
            CampaignSourceAuthorityState.AdmitSupplementActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    /// <summary>Trusted Host draft: private action IDs must never be sent directly to a player.</summary>
    public static CampaignPregameAxisDraft QueryPregameAxisDraft(ExerciseSession session,
        CampaignActionAudience audience)
    {
        if (!IsPregameAxisSetup(session, audience))
            throw new InvalidOperationException("Axis pre-game setup is unavailable.");
        var pregame = session.Pregame!;
        var actions = Query(session, audience).ActionSet!.Candidates;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignPregameSourceCreation.PositionId);
            writer.WriteString("planningDate", "1940-september-ii");
            writer.WriteNumber("targetGameTurn", 2);
            writer.WriteString("status", "incomplete");
            writer.WriteString("policyStatus", "provisional");
            writer.WriteString("policyExplanation", "Provisional September II planning for Game Turn 2: the first recorded pre-game draw sets a maximum. The standard convoy letter is used. Lane allocations are partial; route and port eligibility remain unresolved.");
            if (pregame.Die is { } die) writer.WriteNumber("die", die);
            else writer.WriteNull("die");
            if (pregame.MaximumSupply is { } max) writer.WriteNumber("maximumSupply", max);
            else writer.WriteNull("maximumSupply");
            writer.WriteStartArray("allocations");
            foreach (var allocation in pregame.Allocations())
            {
                var route = CampaignPregameSourceCreation.LaneRoute(allocation.Lane);
                writer.WriteStartObject();
                writer.WriteNumber("lane", allocation.Lane);
                writer.WriteString("origin", route.Origin);
                writer.WriteString("destination", route.Destination);
                writer.WriteNumber("operationStage", allocation.Stage);
                writer.WriteNumber("units", allocation.Units);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var action in actions)
            {
                writer.WriteStartObject();
                if (action is PregameAxisSupplyRollAction) writer.WriteString("kind", "roll");
                else if (action is PregameAxisSupplyAllocationAction allocation)
                {
                    var route = CampaignPregameSourceCreation.LaneRoute(allocation.Lane);
                    writer.WriteString("kind", allocation.Delta == 1 ? "add" : "remove");
                    writer.WriteNumber("lane", allocation.Lane);
                    writer.WriteString("origin", route.Origin);
                    writer.WriteString("destination", route.Destination);
                    writer.WriteNumber("operationStage", allocation.Stage);
                }
                else throw new InvalidOperationException("Unknown pre-game choice.");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(pregame.StateVersion, stream.ToArray(), actions.Select(value => value.ActionId));
    }

    /// <summary>Separate opt-in source setup transition; legacy pregame offers are unchanged.</summary>
    public static CampaignPregameAxisDraft QueryPregameAxisSupplyFinishDraft(ExerciseSession session,
        CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.Pregame is not { } pregame
            || session.SourceAuthority is not null || session.SourceTurn is not null || pregame.Die is null)
            throw new InvalidOperationException("The Axis supply plan cannot be finalized at this position.");
        var plan = CampaignSourceConvoyPlan.Create(pregame);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignPregameSourceCreation.PositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteString("policyStatus", "provisional");
            writer.WriteNumber("targetGameTurn", CampaignSourceConvoyPlan.TargetGameTurn);
            writer.WriteNumber("maximumSupply", plan.MaximumSupply);
            writer.WriteNumber("allocatedSupply", plan.AllocatedSupply);
            writer.WriteNumber("unusedAvailability", plan.MaximumSupply - plan.AllocatedSupply);
            writer.WriteString("choice", "finalize-gt2-supply-plan");
            writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(pregame.StateVersion, stream.ToArray(),
            [CampaignSourceAuthorityState.FinishActionId]);
    }

    /// <summary>Finalize the Axis GT2 draft into source authority; this does not complete setup.</summary>
    public static ExerciseStepResult FinishPregameAxisSupply(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.Pregame is not { } pregame || session.SourceAuthority is not null || session.SourceTurn is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != pregame.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != pregame.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignPregameSourceCreation.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (submission.Audience != CampaignActionAudience.Axis || submission.ActionId != CampaignSourceAuthorityState.FinishActionId
            || pregame.Die is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = CampaignSourceAuthorityState.Finish(pregame);
        var bytes = next.SerializeEvent();
        var contacts = session.CurrentContacts.AdvancePregameSource(pregame.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(pregame.Request.CampaignId, pregame.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(pregame, contacts, [.. session.PregameHistory, bytes], next),
            new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    /// <summary>Axis-only setup choices; available sites are a verified subset of the real map.</summary>
    public static CampaignPregameAxisDraft QuerySourceAxisDumpDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.SourceAuthority is not { } authority)
            throw new InvalidOperationException("Axis source setup is unavailable.");
        var choices = authority.AxisSupplyWorld.PendingDumpIds.SelectMany(dump =>
            authority.AxisSupplyWorld.ValidDestinations(dump).Select(location => (Dump: dump, Location: location))).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteBoolean("siteCoverageComplete", false);
            writer.WriteString("otherMapHexCapacityPolicy", "provisional-three-land-units");
            writer.WriteStartArray("placed");
            foreach (var dump in authority.AxisSupplyWorld.Dumps)
            {
                writer.WriteStartObject();
                writer.WriteString("dumpId", dump.DumpId);
                writer.WriteString("locationId", dump.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject();
                writer.WriteString("dumpId", choice.Dump);
                writer.WriteString("locationId", choice.Location);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(),
            choices.Select(choice => CampaignSourceAuthorityState.DumpActionId(choice.Dump, choice.Location)));
    }

    public static ExerciseStepResult PlaceSourceAxisDump(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var choices = authority.AxisSupplyWorld.PendingDumpIds.SelectMany(dump =>
            authority.AxisSupplyWorld.ValidDestinations(dump).Select(location => (Dump: dump, Location: location)));
        var selected = submission.Audience == CampaignActionAudience.Axis
            ? choices.SingleOrDefault(choice => CampaignSourceAuthorityState.DumpActionId(choice.Dump, choice.Location) == submission.ActionId)
            : default;
        if (selected.Dump is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.PlaceDump(selected.Dump, selected.Location);
        var bytes = authority.SerializeDumpEvent(next, selected.Dump, selected.Location);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    /// <summary>Commonwealth-only initial supply choices, over the verified site subset.</summary>
    public static CampaignPregameAxisDraft QuerySourceCommonwealthSupplyDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceAuthority is not { } authority)
            throw new InvalidOperationException("Commonwealth source setup is unavailable.");
        var choices = authority.CommonwealthSupplyWorld.PendingPlacementIds.SelectMany(id =>
            authority.CommonwealthSupplyWorld.ValidDestinations(id).Select(location => (Id: id, Location: location))).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteBoolean("siteCoverageComplete", false);
            writer.WriteString("otherMapHexCapacityPolicy", "provisional-three-land-units");
            writer.WriteStartArray("placed");
            foreach (var placement in authority.CommonwealthSupplyWorld.Placements)
            {
                writer.WriteStartObject();
                writer.WriteString("placementId", placement.DumpId);
                writer.WriteString("locationId", placement.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject();
                writer.WriteString("placementId", choice.Id);
                writer.WriteString("locationId", choice.Location);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(),
            choices.Select(choice => CampaignSourceAuthorityState.CommonwealthSupplyActionId(choice.Id, choice.Location)));
    }

    public static ExerciseStepResult PlaceSourceCommonwealthSupply(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var choices = authority.CommonwealthSupplyWorld.PendingPlacementIds.SelectMany(id =>
            authority.CommonwealthSupplyWorld.ValidDestinations(id).Select(location => (Id: id, Location: location)));
        var selected = submission.Audience == CampaignActionAudience.Commonwealth
            ? choices.SingleOrDefault(choice => CampaignSourceAuthorityState.CommonwealthSupplyActionId(choice.Id, choice.Location) == submission.ActionId)
            : default;
        if (selected.Id is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.PlaceCommonwealthSupply(selected.Id, selected.Location);
        var bytes = authority.SerializeCommonwealthSupplyEvent(next, selected.Id, selected.Location);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    private static IEnumerable<(string Id, string? Location)> SourceDummyChoices(CampaignSourceAuthorityState authority, bool axis)
    {
        if (axis)
        {
            foreach (var id in authority.AxisSupplyWorld.PendingDummyIds)
            {
                yield return (id, null);
                foreach (var location in authority.AxisSupplyWorld.ValidDummyDestinations(id)) yield return (id, location);
            }
        }
        else if (authority.CommonwealthSupplyWorld.DummyDecision is null)
        {
            yield return ("cw-dummy-1", null);
            foreach (var location in authority.CommonwealthSupplyWorld.ValidDummyDestinations()) yield return ("cw-dummy-1", location);
        }
    }

    /// <summary>Private owner choices for the scenario's optional dummy supply groups.</summary>
    public static CampaignPregameAxisDraft QuerySourceDummyDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { } authority || audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            throw new InvalidOperationException("Source dummy setup is unavailable.");
        var axis = audience == CampaignActionAudience.Axis;
        var choices = SourceDummyChoices(authority, axis).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteString("omissionPolicy", "provisional-whole-group-optional");
            writer.WriteBoolean("siteCoverageComplete", false);
            writer.WriteString("otherMapHexCapacityPolicy", "provisional-three-land-units");
            writer.WriteStartArray("decisions");
            var decisions = axis ? authority.AxisSupplyWorld.DummyDecisions
                : authority.CommonwealthSupplyWorld.DummyDecision is { } own ? [own] : Array.Empty<CampaignSourceDummyDecision>();
            foreach (var decision in decisions)
            {
                writer.WriteStartObject();
                writer.WriteString("dummyId", decision.Id);
                if (decision.LocationId is null) writer.WriteNull("locationId");
                else writer.WriteString("locationId", decision.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject();
                writer.WriteString("dummyId", choice.Id);
                if (choice.Location is null) writer.WriteNull("locationId");
                else writer.WriteString("locationId", choice.Location);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(),
            choices.Select(choice => CampaignSourceAuthorityState.DummyActionId(axis, choice.Id, choice.Location)));
    }

    public static ExerciseStepResult DecideSourceDummy(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (submission.Audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var axis = submission.Audience == CampaignActionAudience.Axis;
        var selected = SourceDummyChoices(authority, axis).SingleOrDefault(choice =>
            CampaignSourceAuthorityState.DummyActionId(axis, choice.Id, choice.Location) == submission.ActionId);
        if (selected.Id is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.DecideDummy(axis, selected.Id, selected.Location);
        var bytes = authority.SerializeDummyEvent(next, axis, selected.Id, selected.Location);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    private static IEnumerable<(string Pool, string Location, int Delta)> SourceMotorizationChoices(
        CampaignSourceAuthorityState authority, bool axis)
    {
        // Cargo choices bind the final physical distribution of unattached points.
        if ((axis ? authority.AxisUnattachedCargo : authority.CommonwealthUnattachedCargo) is not null) yield break;
        var world = axis ? authority.AxisMotorization : authority.CommonwealthMotorization;
        foreach (var pool in world.Pools)
            foreach (var location in world.ValidDestinations(pool.Id))
                foreach (var delta in world.ValidDeltas(pool.Id, location))
                    yield return (pool.Id, location, delta);
    }

    public static CampaignPregameAxisDraft QuerySourceMotorizationDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceAuthority is not { } authority || audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            throw new InvalidOperationException("Source motorization setup is unavailable.");
        var axis = audience == CampaignActionAudience.Axis;
        var choices = SourceMotorizationChoices(authority, axis).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteBoolean("targetCoverageComplete", false);
            writer.WriteString("attachedEligibilityStatus", "unresolved-land-32.53-32.54");
            writer.WriteStartArray("allocations");
            var motorization = axis ? authority.AxisMotorization : authority.CommonwealthMotorization;
            foreach (var allocation in motorization.Allocations)
            {
                writer.WriteStartObject();
                writer.WriteString("poolId", allocation.PoolId);
                writer.WriteString("locationId", allocation.LocationId);
                writer.WriteNumber("points", allocation.Points);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject();
                writer.WriteString("poolId", choice.Pool);
                writer.WriteString("locationId", choice.Location);
                writer.WriteNumber("delta", choice.Delta);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), choices.Select(choice =>
            CampaignSourceAuthorityState.MotorizationActionId(axis, choice.Pool, choice.Location, choice.Delta)));
    }

    public static ExerciseStepResult AdjustSourceMotorization(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (submission.Audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var axis = submission.Audience == CampaignActionAudience.Axis;
        var selected = SourceMotorizationChoices(authority, axis).SingleOrDefault(choice =>
            CampaignSourceAuthorityState.MotorizationActionId(axis, choice.Pool, choice.Location, choice.Delta) == submission.ActionId);
        if (selected.Pool is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.AdjustMotorization(axis, selected.Pool, selected.Location, selected.Delta);
        var bytes = authority.SerializeMotorizationEvent(next, axis, selected.Pool, selected.Location, selected.Delta);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    private static IEnumerable<(string Kind, int Lane, int Stage, int Delta)> SourceGt1SupplyChoices(CampaignSourceAuthorityState authority)
    {
        var plan = authority.OpeningConvoyPlan;
        if (plan.IsFinished) yield break;
        if (plan.Die is null) { yield return ("roll", 0, 0, 0); yield break; }
        yield return ("finish", 0, 0, 0);
        foreach (var lane in new[] { 2, 3, 6 })
            for (var stage = 1; stage <= 3; stage++)
                foreach (var delta in plan.ValidDeltas(lane, stage))
                    yield return ("adjust", lane, stage, delta);
    }

    public static CampaignPregameAxisDraft QuerySourceAxisGt1SupplyDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.SourceAuthority is not { } authority)
            throw new InvalidOperationException("Axis GT1 supply planning is unavailable.");
        var plan = authority.OpeningConvoyPlan;
        var choices = SourceGt1SupplyChoices(authority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteString("policyStatus", "provisional");
            writer.WriteString("planningPeriod", "1940-september-i");
            writer.WriteString("rngOrder", "gt2-then-gt1");
            writer.WriteBoolean("finished", plan.IsFinished);
            if (plan.Die is { } die) writer.WriteNumber("die", die); else writer.WriteNull("die");
            if (plan.MaximumSupply is { } max) writer.WriteNumber("maximumSupply", max); else writer.WriteNull("maximumSupply");
            writer.WriteStartArray("allocations");
            foreach (var allocation in plan.Allocations())
            {
                writer.WriteStartObject();
                writer.WriteNumber("lane", allocation.Lane);
                writer.WriteNumber("operationStage", allocation.OperationStage);
                writer.WriteNumber("supplyUnits", allocation.SupplyUnits);
                writer.WriteString("origin", allocation.Origin);
                writer.WriteString("destination", allocation.Destination);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", choice.Kind);
                if (choice.Kind == "adjust")
                {
                    writer.WriteNumber("lane", choice.Lane);
                    writer.WriteNumber("operationStage", choice.Stage);
                    writer.WriteNumber("delta", choice.Delta);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), choices.Select(choice =>
            CampaignSourceAuthorityState.OpeningConvoyActionId(choice.Kind, choice.Lane, choice.Stage, choice.Delta)));
    }

    public static ExerciseStepResult SubmitSourceAxisGt1Supply(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var selected = submission.Audience == CampaignActionAudience.Axis
            ? SourceGt1SupplyChoices(authority).SingleOrDefault(choice => CampaignSourceAuthorityState.OpeningConvoyActionId(
                choice.Kind, choice.Lane, choice.Stage, choice.Delta) == submission.ActionId) : default;
        if (selected.Kind is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ApplyOpeningConvoyChoice(selected.Kind, selected.Lane, selected.Stage, selected.Delta);
        var bytes = authority.SerializeOpeningConvoyEvent(next, selected.Kind, selected.Lane, selected.Stage, selected.Delta);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    private static IEnumerable<(string Kind, int Target)> SourceCommonwealthSupplyPlanChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.CommonwealthSupplyPlans?.NextTargetGameTurn is not { } target) yield break;
        if (target == 1) yield return ("skip", target);
        yield return ("roll", target);
    }

    public static CampaignPregameAxisDraft QuerySourceCommonwealthSupplyPlanDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceAuthority is not { } authority
            || authority.CommonwealthSupplyPlans is not { } plans)
            throw new InvalidOperationException("Commonwealth prestart supply planning is unavailable.");
        var choices = SourceCommonwealthSupplyPlanChoices(authority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteString("policyStatus", "provisional");
            writer.WriteString("rngOrder", "axis-gt2-axis-gt1-cw-gt1-to-gt4");
            writer.WriteStartArray("plans");
            foreach (var entry in plans.Entries)
            {
                writer.WriteStartObject();
                writer.WriteNumber("targetGameTurn", entry.TargetGameTurn);
                writer.WriteString("status", entry.Status);
                if (entry.Die is { } die) writer.WriteNumber("die", die); else writer.WriteNull("die");
                if (entry.SupplyUnits is { } units) writer.WriteNumber("supplyUnits", units); else writer.WriteNull("supplyUnits");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", choice.Kind);
                writer.WriteNumber("targetGameTurn", choice.Target);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), choices.Select(choice =>
            CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId(choice.Kind, choice.Target)));
    }

    public static ExerciseStepResult SubmitSourceCommonwealthSupplyPlan(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var selected = submission.Audience == CampaignActionAudience.Commonwealth
            ? SourceCommonwealthSupplyPlanChoices(authority).SingleOrDefault(choice =>
                CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId(choice.Kind, choice.Target) == submission.ActionId)
            : default;
        if (selected.Kind is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ApplyCommonwealthSupplyChoice(selected.Kind, selected.Target);
        var bytes = authority.SerializeCommonwealthSupplyPlanEvent(next, selected.Kind, selected.Target);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    private static IEnumerable<(string Kind, int Target, int Lane, int Stage, int Delta)> SourceSeptemberSupplyChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.SeptemberPlans is not { } plans)
        {
            if (authority.CommonwealthSupplyPlans is { NextTargetGameTurn: null }) yield return ("begin", 0, 0, 0, 0);
            yield break;
        }
        if (plans.NextTargetGameTurn is not { } target) yield break;
        var entry = plans.Entries.Single(value => value.TargetGameTurn == target);
        if (entry.Die is null) { yield return ("roll", target, 0, 0, 0); yield break; }
        foreach (var lane in new[] { 2, 3, 6 })
            for (var stage = 1; stage <= 3; stage++)
                foreach (var delta in plans.ValidDeltas(target, lane, stage)) yield return ("adjust", target, lane, stage, delta);
        if (plans.CanFinish(target)) yield return ("finish", target, 0, 0, 0);
    }

    public static CampaignPregameAxisDraft QuerySourceSeptemberSupplyDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.SourceAuthority is not { } authority)
            throw new InvalidOperationException("Axis September supply planning is unavailable.");
        var choices = SourceSeptemberSupplyChoices(authority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteString("policyStatus", "provisional");
            writer.WriteString("otherCargoStatus", "unresolved-including-gt4-light-aa-replacement-eligibility");
            if (authority.SeptemberPlans is { } plans) { writer.WritePropertyName("plans"); writer.WriteRawValue(plans.SerializeCanonical()); }
            else writer.WriteNull("plans");
            writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject(); writer.WriteString("kind", choice.Kind); writer.WriteNumber("targetGameTurn", choice.Target);
                writer.WriteNumber("lane", choice.Lane); writer.WriteNumber("operationStage", choice.Stage); writer.WriteNumber("delta", choice.Delta); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), choices.Select(choice =>
            CampaignSourceAuthorityState.SeptemberSupplyActionId(choice.Kind, choice.Target, choice.Lane, choice.Stage, choice.Delta)));
    }

    public static ExerciseStepResult SubmitSourceSeptemberSupply(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session); ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority is not { } authority) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var selected = submission.Audience == CampaignActionAudience.Axis ? SourceSeptemberSupplyChoices(authority).SingleOrDefault(choice =>
            CampaignSourceAuthorityState.SeptemberSupplyActionId(choice.Kind, choice.Target, choice.Lane, choice.Stage, choice.Delta) == submission.ActionId) : default;
        if (selected.Kind is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ApplySeptemberSupplyChoice(selected.Kind, selected.Target, selected.Lane, selected.Stage, selected.Delta);
        var bytes = authority.SerializeSeptemberSupplyEvent(next, selected.Kind, selected.Target, selected.Lane, selected.Stage, selected.Delta);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, next.StateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts, [.. session.PregameHistory, bytes], next),
            new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    private static IEnumerable<(string Kind, string Target)> SourceAutoblindaChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.Autoblinda is not { } setup) yield break;
        if (setup.CanChooseReplacements) yield return ("replacements", "");
        else yield return ("reset", "");
        foreach (var target in setup.ValidAttachmentTargets()) yield return ("attach", target);
    }

    public static CampaignPregameAxisDraft QuerySourceAutoblindaDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.SourceAuthority?.Autoblinda is not { } setup)
            throw new InvalidOperationException("Axis Autoblinda setup is unavailable.");
        var authority = session.SourceAuthority;
        var choices = SourceAutoblindaChoices(authority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteString("policyStatus", "provisional-either-or-no-mixing");
            writer.WriteBoolean("targetCoverageComplete", false);
            writer.WriteNumber("replacementPoints", setup.ReplacementPoints);
            writer.WriteStartArray("attachments");
            foreach (var target in setup.Targets) writer.WriteStringValue(target);
            writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", choice.Kind);
                if (choice.Target.Length > 0) writer.WriteString("target", choice.Target);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(),
            choices.Select(choice => CampaignSourceAuthorityState.AutoblindaActionId(choice.Kind, choice.Target)));
    }

    public static ExerciseStepResult SubmitSourceAutoblinda(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.Autoblinda is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var selected = submission.Audience == CampaignActionAudience.Axis
            ? SourceAutoblindaChoices(authority).SingleOrDefault(choice =>
                CampaignSourceAuthorityState.AutoblindaActionId(choice.Kind, choice.Target) == submission.ActionId) : default;
        if (selected.Kind is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ApplyAutoblindaChoice(selected.Kind, selected.Target);
        var bytes = authority.SerializeAutoblindaEvent(next, selected.Kind, selected.Target);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    private static IEnumerable<(string ElementId, int Delta)> SourcePriorityMotorizationChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.PriorityMotorization is not { } priority) yield break;
        foreach (var target in CampaignSourcePriorityMotorization.Targets)
            foreach (var delta in priority.ValidDeltas(target.ElementId, authority.CommonwealthMotorization))
                if (authority.AttachedInitialLoads is null || authority.AttachedInitialLoads.CanApplyPriorityDelta(target.ElementId, delta, priority, authority.CommonwealthMotorization))
                    yield return (target.ElementId, delta);
    }

    public static CampaignPregameAxisDraft QuerySourcePriorityMotorizationDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceAuthority?.PriorityMotorization is not { } priority)
            throw new InvalidOperationException("Commonwealth priority motorization setup is unavailable.");
        var authority = session.SourceAuthority;
        var choices = SourcePriorityMotorizationChoices(authority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteString("globalPriorityStatus", "incomplete");
            writer.WriteString("fullMotorizationStatus", "unproven");
            writer.WriteBoolean("targetCoverageComplete", false);
            writer.WriteStartArray("targets");
            foreach (var target in CampaignSourcePriorityMotorization.Targets)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", target.ElementId);
                writer.WriteString("poolId", target.PoolId);
                writer.WriteNumber("remainingPoolPoints", priority.Remaining(target.PoolId, authority.CommonwealthMotorization)
                    - (authority.AttachedInitialLoads?.Reserved(target.PoolId) ?? 0));
                var assignedPoints = priority.AssignedPoints(target.ElementId);
                writer.WriteNumber("assignedPoints", assignedPoints);
                writer.WriteNumber("infantryToe", 6);
                writer.WriteBoolean("infantryComponentLiftSatisfied", assignedPoints == 6);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", choice.ElementId);
                writer.WriteNumber("delta", choice.Delta);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), choices.Select(choice =>
            CampaignSourceAuthorityState.PriorityMotorizationActionId(choice.ElementId, choice.Delta)));
    }

    public static ExerciseStepResult AdjustSourcePriorityMotorization(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.PriorityMotorization is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var selected = submission.Audience == CampaignActionAudience.Commonwealth
            ? SourcePriorityMotorizationChoices(authority).SingleOrDefault(choice =>
                CampaignSourceAuthorityState.PriorityMotorizationActionId(choice.ElementId, choice.Delta) == submission.ActionId) : default;
        if (selected.ElementId is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.AdjustPriorityMotorization(selected.ElementId, selected.Delta);
        var bytes = authority.SerializePriorityMotorizationEvent(next, selected.ElementId, selected.Delta);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    private static IEnumerable<(string Kind, string ElementId, int Delta)> SourceAxisPriorityChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.MalettiHierarchy is not { DetachedId: not null } hierarchy
            || authority.AxisPriorityMotorization is not { } priority) yield break;
        hierarchy.Validate(authority);
        foreach (var delta in priority.ValidDeltas(hierarchy.DetachedId, authority.AxisMotorization))
            yield return ("adjust", hierarchy.DetachedId, delta);
    }

    private static IEnumerable<(string Kind, string ElementId, int Delta)> LegacySourceAxisPriorityChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.AxisPriorityMotorization is not { } priority) yield break;
        foreach (var target in CampaignSourceAxisPriorityMotorization.Targets)
        {
            if (!priority.DetachedTargets.Contains(target, StringComparer.Ordinal)) yield return ("detach", target, 0);
            else
            {
                if (priority.AssignedPoints(target) == 0 && !authority.World.World.Elements.Any(value => value.ElementId == target))
                    yield return ("undo-detachment", target, 0);
                foreach (var delta in priority.ValidDeltas(target, authority.AxisMotorization)) yield return ("adjust", target, delta);
            }
        }
    }

    public static CampaignPregameAxisDraft QuerySourceAxisPriorityDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Axis || session.SourceAuthority?.AxisPriorityMotorization is not { } priority)
            throw new InvalidOperationException("Axis priority motorization setup is unavailable.");
        var authority = session.SourceAuthority;
        var choices = SourceAxisPriorityChoices(authority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteString("globalPriorityStatus", "incomplete");
            writer.WriteString("fullUnitMobilityStatus", "unproven");
            writer.WriteString("detachmentStackStatus", "unverified-c3617-hierarchy-and-stacking");
            writer.WriteBoolean("targetCoverageComplete", false);
            writer.WriteNumber("remainingSharedPoolPoints", priority.Remaining(authority.AxisMotorization));
            writer.WriteStartArray("targets");
            foreach (var target in CampaignSourceAxisPriorityMotorization.Targets)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", target);
                writer.WriteBoolean("detached", priority.DetachedTargets.Contains(target, StringComparer.Ordinal));
                var assigned = priority.AssignedPoints(target);
                writer.WriteNumber("assignedPoints", assigned);
                writer.WriteNumber("infantryToe", 5);
                writer.WriteBoolean("infantryComponentLiftSatisfied", assigned == 5);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", choice.Kind);
                writer.WriteString("elementId", choice.ElementId);
                if (choice.Kind == "adjust") writer.WriteNumber("delta", choice.Delta);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), choices.Select(choice =>
            CampaignSourceAuthorityState.AxisPriorityActionId(choice.Kind, choice.ElementId, choice.Delta)));
    }

    public static ExerciseStepResult SubmitSourceAxisPriority(ExerciseSession session, CampaignActionSubmission submission)
        => SubmitSourceAxisPriorityCore(session, submission, false);

    internal static ExerciseStepResult ReplaySourceAxisPriority(ExerciseSession session, CampaignActionSubmission submission)
        => session.SourceAuthority?.AxisFreeArtillery is not null
            ? SubmitSourceAxisPriorityCore(session, submission, false)
            : SubmitSourceAxisPriorityCore(session, submission, true);

    private static ExerciseStepResult SubmitSourceAxisPriorityCore(ExerciseSession session, CampaignActionSubmission submission, bool replay)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        if (session.SourceAuthority?.AxisPriorityMotorization is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var selected = submission.Audience == CampaignActionAudience.Axis
            ? (replay ? LegacySourceAxisPriorityChoices(authority) : SourceAxisPriorityChoices(authority)).SingleOrDefault(choice =>
                CampaignSourceAuthorityState.AxisPriorityActionId(choice.Kind, choice.ElementId, choice.Delta) == submission.ActionId) : default;
        if (selected.Kind is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.ApplyAxisPriorityChoice(selected.Kind, selected.ElementId, selected.Delta);
        var bytes = authority.SerializeAxisPriorityEvent(next, selected.Kind, selected.ElementId, selected.Delta);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    private static IEnumerable<(string ElementId, string LocationId)> SourceCairoChoices(CampaignSourceAuthorityState authority)
    {
        if (authority.CairoSetup is not { } cairo) yield break;
        foreach (var element in cairo.PendingIds)
            foreach (var location in cairo.ValidDestinations(element))
                yield return (element, location);
    }

    public static CampaignPregameAxisDraft QuerySourceCairoSetupDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience != CampaignActionAudience.Commonwealth || session.SourceAuthority?.CairoSetup is not { } cairo)
            throw new InvalidOperationException("Commonwealth Cairo setup is unavailable.");
        var authority = session.SourceAuthority;
        var choices = SourceCairoChoices(authority).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteString("citySelectionPolicy", "provisional-curated-city-hexes");
            writer.WriteBoolean("siteCoverageComplete", false);
            writer.WriteString("otherCairoForcesStatus", "incomplete");
            writer.WriteStartArray("placed");
            foreach (var placement in cairo.Placements)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", placement.ElementId);
                writer.WriteString("locationId", placement.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("choices");
            foreach (var choice in choices)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", choice.ElementId);
                writer.WriteString("locationId", choice.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(), choices.Select(choice =>
            CampaignSourceAuthorityState.CairoPlacementActionId(choice.ElementId, choice.LocationId)));
    }

    public static ExerciseStepResult PlaceSourceCairoUnit(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (submission is not null && session?.SourceAuthority?.IsOwnerFinalized(submission.Audience) == true)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceAuthority?.CairoSetup is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var authority = session.SourceAuthority;
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var selected = submission.Audience == CampaignActionAudience.Commonwealth
            ? SourceCairoChoices(authority).SingleOrDefault(choice =>
                CampaignSourceAuthorityState.CairoPlacementActionId(choice.ElementId, choice.LocationId) == submission.ActionId) : default;
        if (selected.ElementId is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var next = authority.PlaceCairoUnit(selected.ElementId, selected.LocationId);
        var bytes = authority.SerializeCairoPlacementEvent(next, selected.ElementId, selected.LocationId);
        var contacts = session.CurrentContacts.AdvancePregameSource(authority.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion,
            next.StateVersion, CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(authority.Pregame, contacts,
            [.. session.PregameHistory, bytes], next), new ExerciseStepEvidence(receipt, bytes, next.SerializeCheckpoint()));
    }

    public static ExerciseStartResult Begin(CampaignCreationRequest request) =>
        Begin(request, Cna1979SyntheticContentResolver.Instance);

    public static ExerciseStartResult Begin(CampaignCreationRequest request, IContentPackV6Resolver resolver)
    {
        var execution = CampaignCreationExecution.Execute(request, resolver);
        if (!execution.IsCreated)
            return ExerciseStartResult.Rejected(execution.RejectionReason);

        var session = new ExerciseSession(
            execution.CurrentSnapshot!,
            execution.Context!,
            [execution.CurrentCreatedEvent!],
            CampaignContactJournal.Initialize(execution.CurrentSnapshot!, execution.Context!));
        return ExerciseStartResult.Started(
            session,
            CampaignCurrentEventSerializer.Serialize(execution.CurrentCreatedEvent!, execution.Context!),
            CampaignCurrentSnapshotSerializer.Serialize(execution.CurrentSnapshot!, execution.Context!));
    }

    public static CampaignLegalActionQueryResult Query(
        ExerciseSession session,
        CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceTurn is not null)
            return CampaignSourceSetupBridge.QueryTurn(session, audience);
        if (session.SourceAuthority is not null)
            return CampaignSourceSetupBridge.Query(session, audience);
        if (session.Pregame is { } pregame)
        {
            if (audience != CampaignActionAudience.Axis)
                return CampaignLegalActionQueryResult.Rejected(CampaignLegalActionQueryRejectionReason.InvalidAudience);
            return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(pregame.Request.CampaignId,
                pregame.StateVersion, pregame.Request.RulesetHash, CampaignPregameSourceCreation.PositionId,
                audience, pregame.Candidates(), CampaignLegalActionSet.PregamePolicyId));
        }
        if (session.CurrentCombatDecision is not null)
            return CampaignLegalActionQueryResult.Rejected(CampaignLegalActionQueryRejectionReason.InvalidState);
        return CampaignLegalActions.Query(
            new CampaignAuthorityHandle(session.CurrentSnapshot, session.Context),
            audience);
    }

    /// <summary>
    /// Task 022A (#448): <see cref="Query"/> plus the first-cycle Combat steps the trusted in-process
    /// Exercise harness can drive (first-side Selection and the System step that follows it).
    /// Outside that range it returns exactly what <see cref="Query"/> returns. Test-harness
    /// capability only: the Host does not call it, and it is not Combat activation or certification.
    /// </summary>
    public static CampaignLegalActionQueryResult QueryWithCombat(
        ExerciseSession session,
        CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        return CampaignCombatExerciseActions.Query(session, audience) ?? Query(session, audience);
    }

    /// <summary>
    /// Task 022A (#448): <see cref="Submit"/> for actions offered by <see cref="QueryWithCombat"/>.
    /// Each accepted Combat step is the unchanged trusted Core step with exactly one event.
    /// </summary>
    public static ExerciseStepResult SubmitWithCombat(
        ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        return CampaignCombatExerciseActions.Submit(session, submission) ?? Submit(session, submission);
    }

    // #394 gap 2: CurrentCombatDecision never clears across the second-side
    // boundary (by design -- see ExerciseSession's remarks), so once any
    // first-side Combat decision has ever been made this must check the
    // side-appropriate field instead of the single first-side field, or a
    // second-side query is unconditionally rejected as "already advanced"
    // even though no second-side decision exists yet.
    private static bool HasAdvancedCombatSelection(ExerciseSession session) =>
        session.CurrentStageEntrySecondBreakdown is not null
            ? session.CurrentStageEntrySecondCombatDecision is not null
            : session.CurrentStageEntryBreakdown is not null
            ? session.CurrentStageEntryCombatDecision is not null
            : session.CurrentSecondBreakdown is not null
                ? session.CurrentSecondCombatDecision is not null
                : session.CurrentCombatDecision is not null;

    // #394 gap 3: the second side's own (World, Position) pair, following
    // CampaignNativeSecondMoveV44.Source's own precedent for binding a
    // catalog slot's ActiveSide -- the catalog's own entries carry no side
    // (see LandSequencePosition.ActiveSide), so it is rebound here to the
    // side the second Breakdown actually recorded.
    private static (CampaignWorldSnapshotV6 World, LandSequencePosition Position)
        QuerySecondCombatWorldAndPosition(ExerciseSession session)
    {
        var breakdown = session.CurrentSecondBreakdown
            ?? throw new InvalidOperationException("Second Combat selection requires a completed second Breakdown.");
        var world = session.CurrentSecondMoveWorld ?? session.CurrentSecondReserveWorld
            ?? throw new InvalidOperationException("Second Combat selection requires a retained World.");
        var catalog = Cna1979LandSequenceV4.CreateTurn(session.CurrentSnapshot.Setup.InitialGameTurn)
            .Single(value => value.PositionId == breakdown.NextPositionId);
        var position = new LandSequencePosition(catalog.ContractVersion, catalog.PositionId, catalog.GameTurn,
            catalog.OperationStage, catalog.StageId, catalog.PhaseId, catalog.SegmentId, catalog.StepId,
            catalog.ActorRole, breakdown.Side, catalog.Sources);
        return (world, position);
    }

    // MTS-TASK-005/006 (2026-11-23): this stage's own (World, Position) pair
    // for Combat selection, same shape as QuerySecondCombatWorldAndPosition
    // above, substituting this stage's own Breakdown/Move/Reserve slots for
    // the #394 second side's.
    private static (CampaignWorldSnapshotV6 World, LandSequencePosition Position)
        QueryStageEntryCombatWorldAndPosition(ExerciseSession session)
    {
        var breakdown = session.CurrentStageEntryBreakdown
            ?? throw new InvalidOperationException("Stage Entry Combat selection requires a completed Stage Entry Breakdown.");
        var world = session.CurrentStageEntryMoveWorld ?? session.CurrentStageEntryReserveWorld
            ?? throw new InvalidOperationException("Stage Entry Combat selection requires a retained World.");
        var catalog = Cna1979LandSequenceV4.CreateTurn(session.CurrentSnapshot.Setup.InitialGameTurn)
            .Single(value => value.PositionId == breakdown.NextPositionId);
        var position = new LandSequencePosition(catalog.ContractVersion, catalog.PositionId, catalog.GameTurn,
            catalog.OperationStage, catalog.StageId, catalog.PhaseId, catalog.SegmentId, catalog.StepId,
            catalog.ActorRole, breakdown.Side, catalog.Sources);
        return (world, position);
    }

    // MTS-TASK-006 (2026-11-23): this stage's own SECOND acting side's
    // (World, Position) pair for Combat selection, same shape as
    // QueryStageEntryCombatWorldAndPosition above, substituting this
    // stage's own second-side Breakdown/Move/Reserve slots.
    private static (CampaignWorldSnapshotV6 World, LandSequencePosition Position)
        QueryStageEntrySecondCombatWorldAndPosition(ExerciseSession session)
    {
        var breakdown = session.CurrentStageEntrySecondBreakdown
            ?? throw new InvalidOperationException("Stage Entry second-side Combat selection requires a completed second-side Breakdown.");
        var world = session.CurrentStageEntrySecondMoveWorld ?? session.CurrentStageEntrySecondReserveWorld
            ?? throw new InvalidOperationException("Stage Entry second-side Combat selection requires a retained World.");
        var catalog = Cna1979LandSequenceV4.CreateTurn(session.CurrentSnapshot.Setup.InitialGameTurn)
            .Single(value => value.PositionId == breakdown.NextPositionId);
        var position = new LandSequencePosition(catalog.ContractVersion, catalog.PositionId, catalog.GameTurn,
            catalog.OperationStage, catalog.StageId, catalog.PhaseId, catalog.SegmentId, catalog.StepId,
            catalog.ActorRole, breakdown.Side, catalog.Sources);
        return (world, position);
    }

    internal static CampaignContactOwnerMovementCut QueryContactOwnerMovementCut(
        ExerciseSession session, Cna.Core.Rules.LandSide side, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (HasAdvancedCombatSelection(session))
            throw new InvalidOperationException("Combat selection has already advanced this authority cut.");
        return CampaignContactOwnerMovementCut.Project(session.CurrentSnapshot, session.Context,
            session.CurrentContacts, side, actingElementId);
    }

    internal static CampaignContactCombatSelectionCut QueryContactOwnerCombatCut(
        ExerciseSession session, LandSide side, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (HasAdvancedCombatSelection(session))
            throw new InvalidOperationException("Combat selection has already advanced this authority cut.");
        if (session.CurrentStageEntrySecondBreakdown is not null)
        {
            var (stageEntrySecondWorld, stageEntrySecondPosition) = QueryStageEntrySecondCombatWorldAndPosition(session);
            return CampaignContactCombatSelectionCut.ProjectSecondCombat(stageEntrySecondWorld, stageEntrySecondPosition,
                session.Context, session.CurrentContacts, side, actingElementId);
        }
        if (session.CurrentStageEntryBreakdown is not null)
        {
            var (stageEntryWorld, stageEntryPosition) = QueryStageEntryCombatWorldAndPosition(session);
            return CampaignContactCombatSelectionCut.ProjectSecondCombat(stageEntryWorld, stageEntryPosition,
                session.Context, session.CurrentContacts, side, actingElementId);
        }
        if (session.CurrentSecondBreakdown is not null)
        {
            var (world, position) = QuerySecondCombatWorldAndPosition(session);
            return CampaignContactCombatSelectionCut.ProjectSecondCombat(world, position, session.Context,
                session.CurrentContacts, side, actingElementId);
        }
        return CampaignContactCombatSelectionCut.Project(session.CurrentSnapshot, session.Context,
            session.CurrentContacts, side, actingElementId);
    }

    internal static CampaignContactRetreatBeforeAssaultCut QueryContactRbaCut(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                Barrage: { } repeatedBarrage,
                Decline: null,
                RbaCancellation: null
            } repeated)
            return repeated.RetreatMoves.Count > 0
                ? CampaignContactRetreatBeforeAssaultCut.ProjectAfterMove(
                    repeated.Selection, repeatedBarrage, repeated.RetreatMoves[^1],
                    repeated.RetreatWorld!, repeated.RetreatMoves[0].OriginLocationId,
                    session.Context, session.CurrentContacts)
                : CampaignContactRetreatBeforeAssaultCut.Project(
                    repeated.Selection, repeatedBarrage, session.Context,
                    session.CurrentContacts);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatBarrage is not { } barrage
            || session.CurrentCombatRetreatDecline is not null
            || session.CurrentCombatRbaCancellation is not null)
            throw new InvalidOperationException("Defender retreat requires completed Barrage authority.");
        if (session.CurrentCombatRetreatMove is { } move)
            return CampaignContactRetreatBeforeAssaultCut.ProjectAfterMove(
                selected, barrage, move, session.CurrentCombatRetreatWorld!,
                session.CurrentCombatRetreatMoves[0].OriginLocationId,
                session.Context, session.CurrentContacts);
        return CampaignContactRetreatBeforeAssaultCut.Project(selected, barrage,
            session.Context, session.CurrentContacts);
    }

    internal static CampaignContactForceAssignmentCut QueryContactForceAssignmentCut(
        ExerciseSession session, LandSide ownerSide)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                Decline: { } repeatedDecline,
                ForceCompletion: null
            } repeated
            && repeated.Seals.Count < 2
            && repeated.Seals.All(value => value.OwnerSide != ownerSide))
            return CampaignContactForceAssignmentCut.Project(repeated.Selection,
                repeatedDecline, repeated.RetreatWorld ?? repeated.Selection.World,
                session.Context,
                session.CurrentContacts, ownerSide);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatRetreatDecline is not { } decline
            || session.CurrentCombatForceCompletion is not null
            || session.CurrentCombatForceSeals.Count == 2
            || session.CurrentCombatForceSeals.Any(value => value.OwnerSide == ownerSide))
            throw new InvalidOperationException("Owner Force Assignment seal is unavailable.");
        return CampaignContactForceAssignmentCut.Project(selected, decline,
            session.CurrentCombatRetreatWorld ?? selected.PriorSnapshot.World,
            session.Context, session.CurrentContacts, ownerSide);
    }

    public static ExerciseCheckpoint QueryCheckpoint(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.SourceTurn is { } sourceTurn) return new ExerciseCheckpoint(sourceTurn);
        if (session.SourceAuthority is { } sourceAuthority) return new ExerciseCheckpoint(sourceAuthority);
        if (session.Pregame is { } pregame) return new ExerciseCheckpoint(pregame);
        // Overhaul doc (docs/work/plans/2026-11-23-codebase-overhaul-direction.md),
        // Section 0.6 step 2: dispatch off session.Pending's runtime type
        // instead of a 29-branch ordered null-check cascade. session.Pending
        // is computed once, in the exact order this cascade used to check
        // directly (see ExerciseSession.Pending's remarks) -- this switch is
        // a mechanical transcription, not a reordering: "stop reading the
        // fields directly, read the derived value."
        switch (session.Pending)
        {
            case StageEntrySecondAssaultPending:
                return ExerciseCheckpoint.ForSecondAssault(session.CurrentStageEntrySecondAssault!,
                    session.CurrentStageEntrySecondCombatDecision!);
            case StageEntrySecondNoAttackPending:
                return new ExerciseCheckpoint(session.CurrentStageEntrySecondNoAttackCompletion!,
                    session.CurrentStageEntrySecondCombatDecision!);
            case StageEntrySecondCombatSelectionPending:
                return new ExerciseCheckpoint(session.CurrentStageEntrySecondCombatDecision!);
            case StageEntrySecondBreakdownPending:
                return new ExerciseCheckpoint(session.CurrentStageEntrySecondBreakdown!, session.CurrentSnapshot);
            case StageEntrySecondMovementCompletionPending:
                return new ExerciseCheckpoint(session.CurrentStageEntrySecondMovementCompletion!, session.CurrentSnapshot);
            case StageEntrySecondMovePending:
                return new ExerciseCheckpoint(session.CurrentStageEntrySecondMove!, session.CurrentSnapshot);
            case StageEntrySecondReservePending:
                return new ExerciseCheckpoint(session.CurrentStageEntrySecondReserve!, session.CurrentSnapshot);
            case StageEntryAssaultPending:
                return ExerciseCheckpoint.ForSecondAssault(session.CurrentStageEntryAssault!,
                    session.CurrentStageEntryCombatDecision!);
            case StageEntryNoAttackPending:
                return new ExerciseCheckpoint(session.CurrentStageEntryNoAttackCompletion!,
                    session.CurrentStageEntryCombatDecision!);
            case SecondAssaultPending:
                return ExerciseCheckpoint.ForSecondAssault(session.CurrentSecondAssault!, session.CurrentSecondCombatDecision!);
            case SecondNoAttackPending:
                return new ExerciseCheckpoint(session.CurrentSecondNoAttackCompletion!, session.CurrentSecondCombatDecision!);
            case SecondCombatSelectionPending:
                return new ExerciseCheckpoint(session.CurrentSecondCombatDecision!);
            case SecondBreakdownPending:
                return new ExerciseCheckpoint(session.CurrentSecondBreakdown!, session.CurrentSnapshot);
            case SecondMovementCompletionPending:
                return new ExerciseCheckpoint(session.CurrentSecondMovementCompletion!, session.CurrentSnapshot);
            case SecondMovePending:
                return new ExerciseCheckpoint(session.CurrentSecondMove!, session.CurrentSnapshot);
            case SecondReservePending:
                return new ExerciseCheckpoint(session.CurrentSecondReserve!, session.CurrentSnapshot);
            case NoObligationFleetRepairPending:
                return new ExerciseCheckpoint(session.CurrentNoObligationFleetRepair!, session.CurrentSnapshot);
            case NoObligationFleetAssignmentPending:
                return new ExerciseCheckpoint(session.CurrentNoObligationFleetAssignment!, session.CurrentSnapshot);
            case NoObligationNavalConvoyArrivalPending:
                return new ExerciseCheckpoint(session.CurrentNoObligationNavalConvoyArrival!, session.CurrentSnapshot);
            case NoObligationOrganizationPending:
                return new ExerciseCheckpoint(session.CurrentNoObligationOrganization!, session.CurrentSnapshot);
            case StageEntryWeatherPending:
                return new ExerciseCheckpoint(session.CurrentStageEntryWeather!, session.CurrentSnapshot);
            case InitiativeDeclarationPending:
                return new ExerciseCheckpoint(session.CurrentInitiativeDeclaration!, session.CurrentSnapshot);
            case StageEntryCombatSelectionPending:
                return new ExerciseCheckpoint(session.CurrentStageEntryCombatDecision!);
            case StageEntryBreakdownPending:
                return new ExerciseCheckpoint(session.CurrentStageEntryBreakdown!, session.CurrentSnapshot);
            case StageEntryMovementCompletionPending:
                return new ExerciseCheckpoint(session.CurrentStageEntryMovementCompletion!, session.CurrentSnapshot);
            case StageEntryMovePending:
                return new ExerciseCheckpoint(session.CurrentStageEntryMove!, session.CurrentSnapshot);
            case StageEntryReservePending:
                return new ExerciseCheckpoint(session.CurrentStageEntryReserve!, session.CurrentSnapshot);
            case PostCombatPassPending:
                return new ExerciseCheckpoint(session.CurrentPostCombatPass!, session.CurrentSnapshot);
            case TruckConvoyPassPending:
                return new ExerciseCheckpoint(session.CurrentTruckConvoyPass!, session.CurrentSnapshot);
        }
        if (session.CurrentPostRepeatEvents.Count > 0
            && session.CurrentPostRepeat?.ActiveAssault is null)
        {
            var decision = session.CurrentCombatDecision!;
            var later = session.CurrentPostRepeat!;
            return session.CurrentPostRepeatEvents[^1] switch
            {
                CampaignNativeReserveReleaseEventV27 =>
                    new ExerciseCheckpoint(later.Release, decision),
                CampaignNativeCycleControlV28 control =>
                    new ExerciseCheckpoint(control, decision),
                CampaignNativeRepeatMovementV29 movement =>
                    new ExerciseCheckpoint(movement, decision),
                CampaignNativeRepeatBreakdownV30 breakdown =>
                    new ExerciseCheckpoint(breakdown, decision),
                CampaignNativeRepeatCombatDecisionV31 selection =>
                    new ExerciseCheckpoint(selection, decision),
                CampaignNativeRepeatCombatNoAttackV32 laterCompletion =>
                    new ExerciseCheckpoint(laterCompletion, decision),
                _ => throw new InvalidCampaignHistoryException(
                    "Later cycle checkpoint has an unsupported event."),
            };
        }
        if (session.CurrentRepeatAssault is { Release: { } repeatedRelease }
            && (session.CurrentRepeatAssaultEvents is [.., CampaignNativeReserveReleaseEventV27]
                || session.CurrentPostRepeatEvents is [.., CampaignNativeReserveReleaseEventV27]))
            return new ExerciseCheckpoint(
                session.CurrentPostRepeat?.Release ?? repeatedRelease,
                session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.Control is { } repeatedControl)
            return new ExerciseCheckpoint(repeatedControl, session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.RbaCancellation is { } repeatedRbaCancellation)
            return new ExerciseCheckpoint(repeatedRbaCancellation,
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.CancelledAssault is { } repeatedCancelled)
            return new ExerciseCheckpoint(repeatedCancelled,
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.Successor is { } repeatedSuccessor)
            return new ExerciseCheckpoint(repeatedSuccessor,
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.Custody is { } repeatedCustody)
            return new ExerciseCheckpoint(repeatedCustody,
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.Settlement is { } repeatedSettlement)
            return new ExerciseCheckpoint(repeatedSettlement,
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.PostResultRetreat is { } repeatedPostResult)
            return new ExerciseCheckpoint(repeatedPostResult,
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.Result is { } repeatedResult)
            return new ExerciseCheckpoint(repeatedResult,
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.AntiArmor is { } repeatedAntiArmor)
            return new ExerciseCheckpoint(repeatedAntiArmor,
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.ForceCancellation is { } repeatedCancellation)
            return new ExerciseCheckpoint(repeatedCancellation,
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.ForceCompletion is { } repeatedForceCompletion)
            return new ExerciseCheckpoint(repeatedForceCompletion,
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.Seals is { Count: > 0 } repeatedSeals)
            return new ExerciseCheckpoint(repeatedSeals[^1],
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.Decline is { } repeatedDecline)
            return new ExerciseCheckpoint(repeatedDecline,
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.RetreatMoves is { Count: > 0 } repeatedMoves)
            return new ExerciseCheckpoint(repeatedMoves[^1],
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatAssault?.Barrage is { } repeatedBarrage)
            return new ExerciseCheckpoint(repeatedBarrage,
                (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection!), session.CurrentSnapshot);
        if (session.CurrentRepeatNoAttack is { } repeatNoAttack)
            return new ExerciseCheckpoint(repeatNoAttack,
                session.CurrentCombatDecision!);
        if (session.CurrentRepeatCombatSelection is { } repeatSelection)
            return new ExerciseCheckpoint(repeatSelection,
                session.CurrentCombatDecision!);
        if (session.CurrentRepeatBreakdown is { } repeatBreakdown)
            return new ExerciseCheckpoint(repeatBreakdown, session.CurrentCombatDecision!);
        if (session.CurrentRepeatMove is { } repeatedMove)
            return new ExerciseCheckpoint(repeatedMove, session.CurrentCombatDecision!);
        if (session.CurrentCycleControl is { } cycleControl)
            return new ExerciseCheckpoint(cycleControl, session.CurrentCombatDecision!);
        if (session.CurrentReserveRelease is { } reserveRelease)
            return new ExerciseCheckpoint(reserveRelease, session.CurrentCombatDecision!);
        if (session.CurrentCombatSuccessor is { } successor)
            return new ExerciseCheckpoint(successor, session.CurrentCombatDecision!);
        if (session.CurrentCombatCustodyDisposition is { } custody)
            return new ExerciseCheckpoint(custody, session.CurrentCombatDecision!);
        if (session.CurrentCombatSettlement is { } settlement)
            return new ExerciseCheckpoint(settlement, session.CurrentCombatDecision!);
        if (session.CurrentCombatPostResultRetreat is { } postResultRetreat)
            return new ExerciseCheckpoint(postResultRetreat, session.CurrentCombatDecision!);
        if (session.CurrentCombatAssaultResult is { } result)
            return new ExerciseCheckpoint(result, session.CurrentCombatDecision!);
        if (session.CurrentCombatCancelledAssault is { } cancelledAssault)
            return new ExerciseCheckpoint(cancelledAssault, session.CurrentCombatDecision!);
        if (session.CurrentCombatAntiArmor is { } antiArmor)
            return new ExerciseCheckpoint(antiArmor, session.CurrentCombatDecision!);
        if (session.CurrentCombatForceCancellation is { } forceCancellation)
            return new ExerciseCheckpoint(forceCancellation, session.CurrentCombatDecision!);
        if (session.CurrentCombatForceCompletion is { } forceCompletion)
            return new ExerciseCheckpoint(forceCompletion, session.CurrentCombatDecision!);
        if (session.CurrentCombatForceSeals.Count > 0)
            return new ExerciseCheckpoint(session.CurrentCombatForceSeals[^1],
                session.CurrentCombatDecision!);
        if (session.CurrentCombatRbaCancellation is { } rbaCancellation)
            return new ExerciseCheckpoint(rbaCancellation, session.CurrentCombatDecision!);
        if (session.CurrentCombatRetreatDecline is { } decline)
            return new ExerciseCheckpoint(decline, session.CurrentCombatDecision!);
        if (session.CurrentCombatRetreatMove is { } retreatMove)
            return new ExerciseCheckpoint(retreatMove, session.CurrentCombatDecision!);
        if (session.CurrentNoAttackCompletion is { } completion)
            return new ExerciseCheckpoint(completion, session.CurrentCombatDecision!);
        if (session.CurrentCombatBarrage is { } barrage)
            return new ExerciseCheckpoint(barrage, session.CurrentCombatDecision!);
        return session.CurrentCombatDecision is { } selected
            ? new ExerciseCheckpoint(selected)
            : new ExerciseCheckpoint(session.CurrentSnapshot);
    }

    public static ExerciseCheckpoint ReadCheckpoint(ReadOnlyMemory<byte> canonicalSnapshot)
    {
        RequireLegacyCheckpoint(canonicalSnapshot);
        return new(CampaignCurrentSnapshotSerializer.Deserialize(canonicalSnapshot));
    }

    public static ExerciseCheckpoint ReadCheckpoint(ReadOnlyMemory<byte> canonicalSnapshot,
        IContentPackV6Resolver resolver)
    {
        RequireLegacyCheckpoint(canonicalSnapshot);
        return new(CampaignCurrentSnapshotSerializer.Deserialize(canonicalSnapshot, resolver));
    }

    public static void ValidateCanonicalEvent(ReadOnlyMemory<byte> canonicalEvent)
    {
        RequireLegacyEvent(canonicalEvent);
        _ = CampaignCurrentEventSerializer.Deserialize(canonicalEvent);
    }

    public static void ValidateCanonicalEvent(ReadOnlyMemory<byte> canonicalEvent, IContentPackV6Resolver resolver)
    {
        RequireLegacyEvent(canonicalEvent);
        _ = CampaignCurrentEventSerializer.DeserializeWithResolver(canonicalEvent, resolver);
    }

    private static void RequireLegacyCheckpoint(ReadOnlyMemory<byte> bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        if (document.RootElement.TryGetProperty("kind", out var kind)
            && kind.ValueKind == JsonValueKind.String && kind.GetString() == "source-turn")
            throw new JsonException("Source turn checkpoints require retained genesis and replay.");
        if (document.RootElement.TryGetProperty("contractVersion", out var version)
            && version.ValueKind == JsonValueKind.Number && version.TryGetInt32(out var value)
            && (value == CampaignPregameSourceCreation.RequestVersion
                || (value is CampaignSourceAuthorityState.ContractVersion or CampaignSourceAuthorityState.SupplementalContractVersion
                    or CampaignSourceAuthorityState.CommonwealthSupplementalContractVersion
                    or CampaignSourceAuthorityState.PriorityMotorizationContractVersion
                    or CampaignSourceAuthorityState.AxisPriorityMotorizationContractVersion
                    or CampaignSourceAuthorityState.AxisGarrisonsContractVersion
                    or CampaignSourceAuthorityState.BattlefieldContractVersion
                    or CampaignSourceAuthorityState.CairoSetupContractVersion
                    or CampaignSourceAuthorityState.CairoWorldContractVersion
                    or CampaignSourceAuthorityState.MalettiWorldContractVersion
                    or CampaignSourceAuthorityState.IndependentWorldContractVersion
                    or CampaignSourceAuthorityState.LocalMapWorldContractVersion
                    or CampaignSourceAuthorityState.ExpandedIndependentWorldContractVersion
                    or CampaignSourceAuthorityState.AttachedInitialLoadsContractVersion
                    or CampaignSourceAuthorityState.InitialWrecksContractVersion
                    or CampaignSourceAuthorityState.AxisFreeSetupContractVersion
                    or CampaignSourceAuthorityState.AxisFreeWorldContractVersion
                    or CampaignSourceAuthorityState.AxisCorpsSetupContractVersion
                    or CampaignSourceAuthorityState.AxisCorpsWorldContractVersion
                    or CampaignSourceAuthorityState.MalettiHierarchyContractVersion
                    or CampaignSourceAuthorityState.CertifiedMalettiWorldContractVersion
                    or CampaignSourceAuthorityState.MatruhFormationContractVersion
                    or CampaignSourceAuthorityState.MatruhDetachedWorldContractVersion
                    or CampaignSourceAuthorityState.Gt2CorrectionContractVersion
                    or CampaignSourceAuthorityState.CwPriorityCatalogContractVersion
                    or CampaignSourceAuthorityState.CwPriorityWorldContractVersion
                    or CampaignSourceAuthorityState.CompactCheckpointContractVersion
                    or CampaignSourceAuthorityState.UnattachedCargoContractVersion
                    or CampaignSourceAuthorityState.CwFreeGunsContractVersion
                    or CampaignSourceAuthorityState.CwFreeGunsWorldContractVersion or 34 or 35 or 36 or 37 or 38 or 39 or 40 or 41 or 42 or 45 or 47 or 49 or 55 or 57
                    or CampaignSourceAuthorityState.AustralianCompactCheckpointContractVersion
                    or CampaignSourceAuthorityState.CcnnCompactCheckpointContractVersion or CampaignSourceAuthorityState.CombinedCompactCheckpointContractVersion
                    or CampaignSourceAuthorityState.ShellCorrectedLegacyCheckpointContractVersion or CampaignSourceAuthorityState.ShellCorrectedCompactCheckpointContractVersion
                    or CampaignSourceAuthorityState.ShellCorrectedFinalizedLegacyCheckpointContractVersion or CampaignSourceAuthorityState.ShellCorrectedFinalizedCompactCheckpointContractVersion
                    or CampaignSourceAuthorityState.ArescaCompactCheckpointContractVersion or CampaignSourceAuthorityState.CombinedArescaCompactCheckpointContractVersion
                    or CampaignSourceAuthorityState.HorseRulingCheckpointContractVersion
                    or CampaignSourceAuthorityState.AxisMinorGarrisonsCheckpointContractVersion
                    or CampaignSourceAuthorityState.AxisGarrisonRemainderCheckpointContractVersion
                    or CampaignSourceAuthorityState.CwFrontlineCheckpointContractVersion
                    or CampaignSourceAuthorityState.GiarabubMotorMachineGunCheckpointContractVersion
                    or CampaignSourceAuthorityState.FinalizedLegacyCheckpointContractVersion
                    or CampaignSourceAuthorityState.FinalizedCompactCheckpointContractVersion)
                    && document.RootElement.TryGetProperty("status", out var status)
                    && status.GetString() == "setup-in-progress"))
            throw new JsonException("Pre-game checkpoints require retained genesis and replay.");
    }

    private static void RequireLegacyEvent(ReadOnlyMemory<byte> bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        if (document.RootElement.TryGetProperty("eventType", out var kind)
            && kind.ValueKind == JsonValueKind.String
            && kind.GetString() is "pregame-source-created" or "pregame-axis-gt2-supply-rolled"
                or "pregame-axis-gt2-supply-adjusted" or "source-authority-started" or "source-axis-supply-dump-placed"
                or "source-commonwealth-supply-placed" or "source-dummy-group-decided" or "source-motorization-adjusted"
                or "source-giarabub-motor-placement-policy-admitted"
                or "source-gt1-supply-planned" or "source-commonwealth-supply-planned" or "source-axis-september-supply-changed"
                or "source-horse-setup-decision" or "source-owner-setup-finalization-changed"
                or "source-axis-force-supplement-admitted" or "source-autoblinda-choice"
                or "source-commonwealth-force-supplement-admitted" or "source-priority-motorization-admitted"
                or "source-priority-motorization-adjusted" or "source-axis-priority-motorization-admitted"
                or "source-axis-priority-motorization-changed" or "source-axis-minor-garrisons-admitted"
                or "source-battlefield-admitted" or "source-cairo-setup-admitted" or "source-cairo-unit-placed"
                or "source-cairo-world-admitted" or "source-maletti-world-promoted"
                or "source-independent-world-promoted" or "source-local-map-admitted" or "source-expanded-independent-world-promoted"
                or "source-attached-initial-loads-admitted" or "source-attached-initial-loads-changed" or "source-frontline-initial-loads-changed"
                or "source-initial-wrecks-admitted" or "source-initial-wreck-placed"
                or "source-axis-free-setup-admitted" or "source-axis-free-unit-placed" or "source-axis-free-world-promoted"
                or "source-axis-corps-setup-admitted" or "source-axis-corps-unit-placed" or "source-axis-corps-world-promoted"
                or "source-maletti-hierarchy-admitted" or "source-maletti-hierarchy-changed" or "source-certified-maletti-world-promoted"
                or "source-matruh-formation-admitted" or "source-matruh-formation-changed" or "source-matruh-detached-world-promoted"
                or "source-gt2-correction-admitted" or "source-gt2-correction-changed"
                or "source-cw-priority-catalog-admitted" or "source-cw-priority-catalog-adjusted" or "source-cw-priority-world-promoted"
                or "source-compact-checkpoint-admitted" or "source-unattached-cargo-changed"
                or "source-libyan-grouped-world-admitted" or "source-libyan-disposition-confirmed"
                or "source-italian-divisions-grouped-world-admitted" or "source-italian-division-disposition-confirmed"
                or "source-seventh-armored-grouped-world-admitted" or "source-seventh-armored-disposition-confirmed"
                or "source-catanzaro-grouped-world-admitted" or "source-catanzaro-disposition-confirmed" or "source-sixth-australian-placed" or "source-ccnn-batch-admitted" or "source-seventh-shell-stack-corrected" or "source-aresca-admitted"
                or "source-maletti-grouped-world-admitted" or "source-matruh-grouped-world-admitted"
                or "source-fourth-indian-world-admitted" or "source-fourth-indian-disposition-confirmed" or "source-fourth-indian-motor-adjusted"
                or "source-cw-free-guns-admitted" or "source-cw-free-gun-placed" or "source-cw-free-guns-world-promoted")
            throw new JsonException("Pre-game events require retained genesis and replay.");
    }

    public static ExerciseStepResult Submit(
        ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.SourceTurn is { } sourceTurn)
            return sourceTurn.Position.PhaseId switch
            {
                LandPhaseIds.NavalConvoySchedule => submission.Audience == CampaignActionAudience.Axis
                    ? SubmitSourceGt3SupplyRoll(session, submission) : SubmitSourceGt5SupplyRoll(session, submission),
                LandPhaseIds.TacticalShipping => SubmitSourceTacticalShippingDecline(session, submission),
                LandPhaseIds.InitiativeDeclaration => SubmitSourceStageOneOrder(session, submission),
                LandPhaseIds.Organization => submission.ActionId == CampaignSourceTurnAction.InterruptSixthAustralianTraining(sourceTurn.StateVersion).ActionId
                    ? SubmitSourceSixthAustralianTrainingInterrupt(session, submission)
                    : submission.ActionId == CampaignSourceTurnAction.ApproveSixthAustralianBaseline(sourceTurn.StateVersion).ActionId
                        || submission.ActionId == CampaignSourceTurnAction.WithdrawSixthAustralianBaseline(sourceTurn.StateVersion).ActionId
                        ? SubmitSourceAustralianBaselineVote(session, submission) : SubmitSourceVoluntaryOrganizationDecline(session, submission),
                LandPhaseIds.ReserveDesignation when sourceTurn.Position.OperationStage == 1 => SubmitSourceGt1FirstReservesDecline(session, submission),
                LandPhaseIds.MovementAndCombat when sourceTurn.Position.SegmentId == LandSegmentIds.Movement =>
                    submission.ActionId == CampaignSourceTurnAction.FinishFirstMovement(sourceTurn.StateVersion).ActionId
                        ? SubmitSourceGt1FirstMovementFinish(session, submission) : SubmitSourceFourthIndianOpeningMove(session, submission),
                LandPhaseIds.NavalConvoyArrival when sourceTurn.Position.OperationStage == 1 => SubmitSourceGt1Lane2AttackDecline(session, submission),
                _ => SubmitSourcePolishArrival(session, submission),
            };
        if (session.SourceAuthority is not null)
            return CampaignSourceSetupBridge.Submit(session, submission);
        if (session.Pregame is { } pregame)
            return SubmitPregame(session, pregame, submission);
        if (session.CurrentCombatDecision is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var execution = CampaignCurrentActionExecution.Execute(
            session.CurrentSnapshot,
            session.Context,
            submission);
        return Complete(session, execution);
    }

    // Dedicated non-generic entry point for Reaction -- ruled (scope doc,
    // PR #500): sidestep #394 gap 1 here rather than remediate the shared
    // gate, which stays its own tracked future design decision (see
    // docs/work/plans/2026-11-18-combat-continuation-second-side-design-proposal.md).
    // A reaction window opened by a second-side Movement is reached only
    // after the first-side's own Combat decision has already been made,
    // which trips the generic Query/Submit's stale CurrentCombatDecision
    // gate above even though no second-side Combat decision exists yet --
    // the exact gap 1 symptom, just hit from a third call site the scope
    // doc's two gates (QueryContactOwnerMovementCut/QueryContactOwnerCombatCut)
    // never covered. This mirrors Query/Submit's own fallthrough exactly
    // (the same CampaignLegalActions.Query / CampaignCurrentActionExecution
    // .Execute + Complete dispatch, on the same session.CurrentSnapshot)
    // with only that one gate removed -- no other Core logic changes.
    // CampaignHostAdapter's own candidate-shape validation
    // (CreateReactionOwnerDraft/SubmitReaction) is what keeps this entry
    // point from ever returning or accepting anything but Reaction's own
    // three candidate types.
    internal static CampaignLegalActionQueryResult QueryReactionWindow(
        ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        return CampaignLegalActions.Query(
            new CampaignAuthorityHandle(session.CurrentSnapshot, session.Context), audience);
    }

    internal static ExerciseStepResult SubmitReactionWindow(
        ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        var execution = CampaignCurrentActionExecution.Execute(
            session.CurrentSnapshot, session.Context, submission);
        return Complete(session, execution);
    }

    private static ExerciseStepResult SubmitPregame(ExerciseSession session, CampaignPregameSourceState pregame,
        CampaignActionSubmission submission)
    {
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion
            || !Enum.IsDefined(submission.Audience))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != pregame.Request.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != pregame.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignPregameSourceCreation.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var action = submission.Audience == CampaignActionAudience.Axis
            ? pregame.Candidates().SingleOrDefault(value => value.ActionId == submission.ActionId) : null;
        if (action is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var (next, eventBytes) = pregame.Apply(action);
        var contacts = session.CurrentContacts.AdvancePregameSource(pregame.StateVersion);
        var receipt = new CampaignActionAcceptanceReceipt(pregame.Request.CampaignId, pregame.StateVersion,
            next.StateVersion, CampaignPregameSourceCreation.PositionId, submission.Audience, action.ActionId);
        return ExerciseStepResult.Accepted(new ExerciseSession(next, contacts, [.. session.PregameHistory, eventBytes]),
            new ExerciseStepEvidence(receipt, eventBytes, next.SerializeCheckpoint()));
    }

    internal static ExerciseStepResult SubmitContactOwnerMovement(
        ExerciseSession session, CampaignActionSubmission submission, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.CurrentCombatDecision is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var execution = CampaignCurrentActionExecution.ExecuteContactMovement(
            session.CurrentSnapshot, session.Context, session.CurrentContacts,
            submission, actingElementId);
        return Complete(session, execution);
    }

    internal static ExerciseStepResult SubmitContactOwnerCombatSelection(
        ExerciseSession session, CampaignActionSubmission submission, string actingElementId,
        ReadOnlyMemory<byte>? retainedLegacyEvent = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (string.IsNullOrWhiteSpace(actingElementId)
            || !CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        // #394 extra-2: this method now also serves the second side's first
        // Combat decision after their own Breakdown (V46) completes -- gated
        // the same way as the two Query cuts above, by the side-appropriate
        // field rather than the always-stale first-side one. The expected
        // state version/position for a second-side submission come from the
        // second Breakdown's own fields, not from the (stale)
        // session.CurrentSnapshot -- QueryCheckpoint already resolves these
        // correctly for the Host's submission-building, so this must match.
        // MTS-TASK-005/006 (2026-11-23): extended a third time for this
        // stage's own first-acting-side Combat Decision, gated by
        // CurrentStageEntryBreakdown the same way, checked first since it is
        // chronologically later than the #394 second side's own Breakdown.
        // MTS-TASK-006 (2026-11-23): extended a fourth time for this
        // stage's own SECOND acting side's Combat Decision, gated by
        // CurrentStageEntrySecondBreakdown, checked first since it is
        // chronologically latest.
        var isStageEntrySecondSide = session.CurrentStageEntrySecondBreakdown is not null;
        var isStageEntry = !isStageEntrySecondSide && session.CurrentStageEntryBreakdown is not null;
        var isSecondSide = !isStageEntrySecondSide && !isStageEntry && session.CurrentSecondBreakdown is not null;
        var expectedStateVersion = isStageEntrySecondSide
            ? session.CurrentStageEntrySecondBreakdown!.StateVersion
            : isStageEntry
            ? session.CurrentStageEntryBreakdown!.StateVersion
            : isSecondSide
                ? session.CurrentSecondBreakdown!.StateVersion : session.CurrentSnapshot.StateVersion;
        if (HasAdvancedCombatSelection(session)
            || session.Context.ArtifactV6 is not { } artifact
            || !CampaignSnapshotV11Admission.IsValid(session.CurrentSnapshot, artifact,
                session.Context.Scenario)
            || session.CurrentContacts.AuthorityVersion != expectedStateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.CampaignId != session.CurrentSnapshot.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != expectedStateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        var expectedPositionId = isStageEntrySecondSide
            ? session.CurrentStageEntrySecondBreakdown!.NextPositionId
            : isStageEntry
            ? session.CurrentStageEntryBreakdown!.NextPositionId
            : isSecondSide
                ? session.CurrentSecondBreakdown!.NextPositionId
                : CampaignCurrentActionExecution.CurrentPositionId(session.CurrentSnapshot);
        if (submission.ExpectedPositionId != expectedPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var side = submission.Audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => (LandSide?)null,
        };
        if (side is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        try
        {
            if (isStageEntrySecondSide)
            {
                // Net-new path: no legacy decision event can exist for this
                // stage's second side either.
                var (world, position) = QueryStageEntrySecondCombatWorldAndPosition(session);
                var stageEntrySecondCut = CampaignContactCombatSelectionCut.ProjectSecondCombat(world, position,
                    session.Context, session.CurrentContacts, side.Value, actingElementId);
                if (!stageEntrySecondCut.AssaultOffers.Any(offer => offer.ActionId == submission.ActionId)
                    && stageEntrySecondCut.FinishOffer?.ActionId != submission.ActionId)
                    return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
                var stageEntrySecondSelected = CampaignNativeSecondCombatDecisionStateV47.Select(world, position,
                    session.CurrentSnapshot, session.CurrentSnapshot.RandomState, session.Context,
                    session.CurrentContacts, side.Value, actingElementId, submission.ActionId);
                if (stageEntrySecondSelected.Decision.Side != side || stageEntrySecondSelected.Decision.ActingElementId != actingElementId
                    || stageEntrySecondSelected.Decision.ActionId != submission.ActionId)
                    return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
                var stageEntrySecondContacts = session.CurrentContacts.AdvanceSecondCombatSelection(stageEntrySecondSelected.Decision);
                var stageEntrySecondSuccessor = session.WithStageEntrySecondCombatSelection(stageEntrySecondSelected, stageEntrySecondContacts);
                var stageEntrySecondBytes = stageEntrySecondSelected.Decision.SerializeCanonical();
                var stageEntrySecondReceipt = new CampaignActionAcceptanceReceipt(stageEntrySecondSelected.CampaignId,
                    stageEntrySecondSelected.Decision.PriorStateVersion, stageEntrySecondSelected.StateVersion,
                    stageEntrySecondSelected.Decision.PositionId, submission.Audience, submission.ActionId);
                return ExerciseStepResult.Accepted(stageEntrySecondSuccessor,
                    new ExerciseStepEvidence(stageEntrySecondReceipt, stageEntrySecondBytes, stageEntrySecondBytes));
            }
            if (isStageEntry)
            {
                // Net-new path: no legacy decision event can exist for this
                // stage either, so retainedLegacyEvent never applies here.
                var (world, position) = QueryStageEntryCombatWorldAndPosition(session);
                var stageEntryCut = CampaignContactCombatSelectionCut.ProjectSecondCombat(world, position,
                    session.Context, session.CurrentContacts, side.Value, actingElementId);
                if (!stageEntryCut.AssaultOffers.Any(offer => offer.ActionId == submission.ActionId)
                    && stageEntryCut.FinishOffer?.ActionId != submission.ActionId)
                    return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
                var stageEntrySelected = CampaignNativeSecondCombatDecisionStateV47.Select(world, position,
                    session.CurrentSnapshot, session.CurrentSnapshot.RandomState, session.Context,
                    session.CurrentContacts, side.Value, actingElementId, submission.ActionId);
                if (stageEntrySelected.Decision.Side != side || stageEntrySelected.Decision.ActingElementId != actingElementId
                    || stageEntrySelected.Decision.ActionId != submission.ActionId)
                    return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
                var stageEntryContacts = session.CurrentContacts.AdvanceSecondCombatSelection(stageEntrySelected.Decision);
                var stageEntrySuccessor = session.WithStageEntryCombatSelection(stageEntrySelected, stageEntryContacts);
                var stageEntryBytes = stageEntrySelected.Decision.SerializeCanonical();
                var stageEntryReceipt = new CampaignActionAcceptanceReceipt(stageEntrySelected.CampaignId,
                    stageEntrySelected.Decision.PriorStateVersion, stageEntrySelected.StateVersion,
                    stageEntrySelected.Decision.PositionId, submission.Audience, submission.ActionId);
                return ExerciseStepResult.Accepted(stageEntrySuccessor,
                    new ExerciseStepEvidence(stageEntryReceipt, stageEntryBytes, stageEntryBytes));
            }
            if (isSecondSide)
            {
                // Net-new path: no legacy second-side Combat decision event
                // can exist, so retainedLegacyEvent never applies here.
                var (world, position) = QuerySecondCombatWorldAndPosition(session);
                var secondCut = CampaignContactCombatSelectionCut.ProjectSecondCombat(world, position,
                    session.Context, session.CurrentContacts, side.Value, actingElementId);
                if (!secondCut.AssaultOffers.Any(offer => offer.ActionId == submission.ActionId)
                    && secondCut.FinishOffer?.ActionId != submission.ActionId)
                    return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
                var secondSelected = CampaignNativeSecondCombatDecisionStateV47.Select(world, position,
                    session.CurrentSnapshot, session.CurrentSnapshot.RandomState, session.Context,
                    session.CurrentContacts, side.Value, actingElementId, submission.ActionId);
                if (secondSelected.Decision.Side != side || secondSelected.Decision.ActingElementId != actingElementId
                    || secondSelected.Decision.ActionId != submission.ActionId)
                    return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
                var secondContacts = session.CurrentContacts.AdvanceSecondCombatSelection(secondSelected.Decision);
                var secondSuccessor = session.WithSecondCombatSelection(secondSelected, secondContacts);
                var secondBytes = secondSelected.Decision.SerializeCanonical();
                var secondReceipt = new CampaignActionAcceptanceReceipt(secondSelected.CampaignId,
                    secondSelected.Decision.PriorStateVersion, secondSelected.StateVersion,
                    secondSelected.Decision.PositionId, submission.Audience, submission.ActionId);
                return ExerciseStepResult.Accepted(secondSuccessor,
                    new ExerciseStepEvidence(secondReceipt, secondBytes, secondBytes));
            }
            var cut = retainedLegacyEvent is null
                ? QueryContactOwnerCombatCut(session, side.Value, actingElementId)
                : CampaignContactCombatSelectionCut.Project(session.CurrentSnapshot, session.Context,
                    session.CurrentContacts, side.Value, actingElementId, legacyInitiativeOwner: true);
            if (!cut.AssaultOffers.Any(offer => offer.ActionId == submission.ActionId)
                && cut.FinishOffer?.ActionId != submission.ActionId)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var selected = retainedLegacyEvent is null
                ? CampaignNativeCombatDecisionStateV12.Select(session.CurrentSnapshot,
                    session.Context, session.CurrentContacts, side.Value, actingElementId, submission.ActionId)
                : CampaignNativeCombatDecisionStateV12.Replay(session.CurrentSnapshot,
                    session.Context, session.CurrentContacts, retainedLegacyEvent.Value);
            if (selected.Decision.Side != side || selected.Decision.ActingElementId != actingElementId
                || selected.Decision.ActionId != submission.ActionId
                || (retainedLegacyEvent is not null && selected.Decision.ContractVersion != CampaignNativeCombatDecisionV12.CurrentContractVersion))
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var contacts = session.CurrentContacts.AdvanceNativeCombatSelection(selected);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(selected.Decision), contacts, selected,
                ammunitionLedger: session.CurrentAmmunitionLedger);
            var receipt = new CampaignActionAcceptanceReceipt(session.CurrentSnapshot.CampaignId,
                session.CurrentSnapshot.StateVersion, selected.StateVersion,
                selected.Decision.PositionId, submission.Audience, submission.ActionId);
            var evidence = new ExerciseStepEvidence(receipt,
                selected.Decision.SerializeCanonical(), selected.SerializeCanonical());
            return ExerciseStepResult.Accepted(successor, evidence);
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static (string EntryHash, long StateVersion) QueryNativeCombatSelectionRoundBinding(ExerciseSession session,
        bool legacyInitiativeOwner = false)
    {
        ArgumentNullException.ThrowIfNull(session);
        // #394 gap 4: second side has no CampaignSnapshotV11-backed position
        // to feed CampaignNativeCombatSelectionTimeoutV38.Create (see
        // CampaignNativeSecondCombatDecisionV47's remarks), and no
        // repeat/cycle chain of its own either, so it cannot share
        // CreateSelectionTimeout's first-side-only logic below at all.
        if (session.CurrentSecondBreakdown is not null)
            return QuerySecondCombatSelectionRoundBinding(session);
        var timeout = CreateSelectionTimeout(session, CampaignSelectionTimeoutEvidenceV38.NoTimingConfigurationHash,
            -1, -1, -1, null, "selection-unavailable", legacyInitiativeOwner);
        var evidence = timeout is CampaignNativeCombatSelectionTimeoutV38 initial
            ? initial.Evidence : ((CampaignNativeRepeatCombatSelectionTimeoutV38)timeout).Evidence;
        return (evidence.EntryHash, evidence.PriorStateVersion);
    }

    // #394 gap 4: binds the Host's selection-round timing row to the second
    // side's own pending authority cut. Mirrors
    // CampaignNativeRepeatCombatSelectionTimeoutV38.Create's own precedent
    // for a cut with no CampaignSnapshotV11 of its own: EntryHash is the
    // hash of the preceding bound event (there, the repeated Breakdown;
    // here, the second Breakdown that opened this round), not a hash of a
    // snapshot. Only binding is supported here -- see
    // TimeoutNativeCombatSelection's second-side branch for why actually
    // *applying* a second-side selection timeout is out of this PR's scope.
    private static (string EntryHash, long StateVersion) QuerySecondCombatSelectionRoundBinding(ExerciseSession session)
    {
        if (session.CurrentSecondCombatDecision is not null)
            throw new InvalidOperationException("No pending second-side Combat selection.");
        var breakdown = session.CurrentSecondBreakdown!;
        return (CampaignSelectionTimeoutEvidenceV38.Digest(breakdown.SerializeCanonical()), breakdown.StateVersion);
    }

    internal static CampaignNativeSecondCombatSelectionTimeoutV48 CreateSecondSelectionTimeout(
        ExerciseSession session, string configurationHash, long opened, long deadline, long highWater,
        long? observed, string reason)
    {
        var (world, position) = QuerySecondCombatWorldAndPosition(session);
        return CampaignNativeSecondCombatSelectionTimeoutV48.Create(session.CurrentSecondBreakdown!, world,
            position, session.Context, session.CurrentContacts, configurationHash, opened, deadline,
            highWater, observed, reason);
    }

    private static object CreateSelectionTimeout(ExerciseSession session, string configurationHash,
        long opened, long deadline, long highWater, long? observed, string reason,
        bool legacyInitiativeOwner = false)
    {
        if (session.CurrentCombatDecision is null)
            return CampaignNativeCombatSelectionTimeoutV38.Create(session.CurrentSnapshot, session.Context,
                session.CurrentContacts, configurationHash, opened, deadline, highWater, observed, reason,
                legacyInitiativeOwner);
        var release = QueryNativeReserveReleaseState(session);
        var later = session.CurrentPostRepeat;
        if ((later is null ? session.CurrentRepeatCombatSelection : later.Selection) is not null
            || (later is null ? session.CurrentCycleControl : later.Control) is not { } control
            || (later is null ? session.CurrentRepeatMove : later.Movement) is not { } movement
            || (later is null ? session.CurrentRepeatBreakdown : later.Breakdown) is not { } breakdown
            || (later is null ? session.CurrentRepeatMoveWorld : later.MovedWorld) is not { } world)
            throw new InvalidOperationException("No pending repeated Combat selection.");
        return CampaignNativeRepeatCombatSelectionTimeoutV38.Create(release, control, movement, breakdown,
            world, session.Context, session.CurrentContacts, configurationHash, opened, deadline, highWater, observed, reason);
    }

    internal static ExerciseStepResult TimeoutNativeCombatSelection(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds, long? cancelledAtUnixMilliseconds, string reason,
        bool legacyInitiativeOwner = false)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            if ((configuration is not null && (configuration.RulesetHash != session.CurrentSnapshot.RulesetHash
                    || configuration.ContentHash != session.Context.ArtifactV6!.Identity.Hash))
                || (configuration is null && reason != "selection-unavailable")
                || (reason != "selection-unavailable" && (configuration is null
                    || checked(deadlineUnixMilliseconds - openedAtUnixMilliseconds) != configuration.Budget("selection"))))
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
            // The second acting side's round binds its V46 Breakdown (V48),
            // never the first side's stale snapshot or later-cycle chain.
            if (session.CurrentSecondBreakdown is { } secondBreakdown)
            {
                if (session.CurrentSecondCombatDecision is not null)
                    return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
                var secondTimeout = CreateSecondSelectionTimeout(session,
                    configuration?.Hash ?? CampaignSelectionTimeoutEvidenceV38.NoTimingConfigurationHash,
                    openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds,
                    cancelledAtUnixMilliseconds, reason);
                var (secondWorld, _) = QuerySecondCombatWorldAndPosition(session);
                var secondSelected = new CampaignNativeSecondCombatDecisionStateV47(session.CurrentSnapshot,
                    secondWorld, session.CurrentSnapshot.RandomState, secondTimeout);
                var secondContacts = session.CurrentContacts.AdvanceSecondCombatSelection(secondTimeout);
                var secondNext = session.WithSecondCombatSelection(secondSelected, secondContacts);
                var secondBytes = secondTimeout.SerializeCanonical();
                var secondReceipt = new CampaignActionAcceptanceReceipt(secondTimeout.CampaignId,
                    secondBreakdown.StateVersion, secondTimeout.StateVersion, secondTimeout.NextPositionId,
                    CampaignActionAudience.System, secondTimeout.Evidence.Hash);
                return ExerciseStepResult.Accepted(secondNext,
                    new ExerciseStepEvidence(secondReceipt, secondBytes, secondBytes));
            }
            var timeout = CreateSelectionTimeout(session, configuration?.Hash ?? CampaignSelectionTimeoutEvidenceV38.NoTimingConfigurationHash,
                openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds, cancelledAtUnixMilliseconds, reason,
                legacyInitiativeOwner);
            ExerciseSession next;
            byte[] bytes;
            byte[] state;
            CampaignSelectionTimeoutEvidenceV38 evidence;
            if (timeout is CampaignNativeCombatSelectionTimeoutV38 initial)
            {
                var selected = new CampaignNativeCombatDecisionStateV12(session.CurrentSnapshot, initial);
                var contacts = session.CurrentContacts.AdvanceNativeCombatSelection(selected);
                next = new ExerciseSession(session.CurrentSnapshot, session.Context, session.CurrentHistory.Add(initial), contacts, selected,
                    ammunitionLedger: session.CurrentAmmunitionLedger);
                bytes = initial.SerializeCanonical();
                state = selected.SerializeCanonical();
                evidence = initial.Evidence;
            }
            else
            {
                var repeated = (CampaignNativeRepeatCombatSelectionTimeoutV38)timeout;
                var contacts = session.CurrentContacts.AdvanceNativeRepeatCombatSelection(repeated);
                next = session.CurrentPostRepeat is null ? session.WithRepeatCombatSelectionEvent(repeated, contacts)
                    : session.WithPostRepeatEvent(repeated, contacts);
                bytes = state = repeated.SerializeCanonical();
                evidence = repeated.Evidence;
            }
            var receipt = new CampaignActionAcceptanceReceipt(evidence.CampaignId, evidence.PriorStateVersion,
                checked(evidence.PriorStateVersion + 1), evidence.NextPositionId, CampaignActionAudience.System, evidence.Hash);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, state));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    internal static ExerciseStepResult CompleteNativeCombatNoAttack(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentNoAttackCompletion is not null
            || session.CurrentContacts.AuthorityVersion != selected.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var completion = CampaignNativeCombatNoAttackCompletionV13.Create(selected, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatNoAttackCompletion(completion);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(completion), contacts, selected, completion,
                ammunitionLedger: session.CurrentAmmunitionLedger);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                selected.StateVersion, completion.StateVersion,
                completion.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(
                    completion.SerializeCanonical()))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                completion.SerializeCanonical(), completion.SerializeStateCanonical(selected)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    // #394 extra-2: second-side analog of CompleteNativeCombatNoAttack. A
    // distinct method rather than a branch inside the first-side one, to
    // match this file's own convention of a separate Complete* method per
    // "track" (c.f. CompleteNativeRepeatCombatNoAttack for the repeat track)
    // and to keep CompleteNativeCombatNoAttack's own ~70-call-site behavior
    // completely untouched. CampaignNativeCombatNoAttackCompletionV13.Create
    // itself needs no change: it takes a CampaignCombatSelectionFrame, and
    // that type's implicit operator from CampaignNativeCombatDecisionStateV12
    // (CampaignCombatSelectionFrame.cs) derives World/RandomState from
    // `selected.PriorSnapshot`, which gap 3 already bridges correctly for
    // the second side.
    //
    // The completion also advances the second side's retained stage chain
    // (ExerciseSession.WithSecondNoAttack), whose Reserve Release initializes
    // from this V13 (CampaignNativeReserveReleaseStateV27.InitializeAfterSecondNoAttack).
    internal static ExerciseStepResult CompleteSecondCombatNoAttack(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentSecondCombatDecision is not { } selected
            || session.CurrentSecondNoAttackCompletion is not null
            || session.CurrentContacts.AuthorityVersion != selected.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var completion = CampaignNativeCombatNoAttackCompletionV13.Create(selected, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatNoAttackCompletion(completion);
            var successor = session.WithSecondNoAttack(completion, contacts);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                selected.StateVersion, completion.StateVersion,
                completion.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(
                    completion.SerializeCanonical()))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                completion.SerializeCanonical(), completion.SerializeStateCanonical(selected)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    // MTS-TASK-005/006 follow-up: Stage Entry analog of CompleteSecondCombatNoAttack,
    // same shape and same reasoning, writing CurrentStageEntryCombatDecision/
    // CurrentStageEntryNoAttackCompletion instead of CurrentSecondCombatDecision/
    // CurrentSecondNoAttackCompletion.
    internal static ExerciseStepResult CompleteStageEntryCombatNoAttack(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentStageEntryCombatDecision is not { } selected
            || session.CurrentStageEntryNoAttackCompletion is not null
            || session.CurrentContacts.AuthorityVersion != selected.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var completion = CampaignNativeCombatNoAttackCompletionV13.Create(selected, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatNoAttackCompletion(completion);
            var successor = session.WithStageEntryNoAttack(completion, contacts);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                selected.StateVersion, completion.StateVersion,
                completion.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(
                    completion.SerializeCanonical()))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                completion.SerializeCanonical(), completion.SerializeStateCanonical(selected)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    // MTS-TASK-006 (2026-11-23): Stage Entry second-side analog of
    // CompleteStageEntryCombatNoAttack, same shape and same reasoning,
    // writing CurrentStageEntrySecondCombatDecision/CurrentStageEntrySecondNoAttackCompletion
    // instead of the first-acting-side equivalents.
    internal static ExerciseStepResult CompleteStageEntrySecondCombatNoAttack(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentStageEntrySecondCombatDecision is not { } selected
            || session.CurrentStageEntrySecondNoAttackCompletion is not null
            || session.CurrentContacts.AuthorityVersion != selected.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var completion = CampaignNativeCombatNoAttackCompletionV13.Create(selected, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatNoAttackCompletion(completion);
            var successor = session.WithStageEntrySecondNoAttack(completion, contacts);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                selected.StateVersion, completion.StateVersion,
                completion.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(
                    completion.SerializeCanonical()))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                completion.SerializeCanonical(), completion.SerializeStateCanonical(selected)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeCombatBarrage(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is { Barrage: null })
            return CompleteNativeRepeatCombatBarrage(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentNoAttackCompletion is not null
            || session.CurrentCombatBarrage is not null
            || session.CurrentContacts.AuthorityVersion != selected.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var barrage = CampaignNativeCombatBarrageV14.Create(selected, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatBarrage(barrage);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(barrage), contacts, selected, combatBarrage: barrage,
                ammunitionLedger: session.CurrentAmmunitionLedger);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                selected.StateVersion, barrage.StateVersion, barrage.NextPositionId,
                CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(barrage.SerializeCanonical()))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                barrage.SerializeCanonical(), barrage.SerializeStateCanonical(selected)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult SubmitContactDefenderRbaDecline(
        ExerciseSession session, CampaignActionSubmission submission, string defenderElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (string.IsNullOrWhiteSpace(defenderElementId)
            || !CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (session.CurrentRepeatAssault is
            {
                Barrage: not null,
                Decline: null,
                RbaCancellation: null
            })
            return SubmitRepeatedDefenderRbaDecline(session, submission,
                defenderElementId);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatBarrage is not { } barrage
            || session.CurrentCombatRetreatDecline is not null
            || session.CurrentContacts.AuthorityVersion
                != (session.CurrentCombatRetreatMove?.StateVersion ?? barrage.StateVersion))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.CampaignId != selected.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion
            != (session.CurrentCombatRetreatMove?.StateVersion ?? barrage.StateVersion))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != barrage.NextPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        try
        {
            var cut = QueryContactRbaCut(session);
            var audience = cut.DefenderSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            if (submission.Audience != audience || defenderElementId != cut.DefenderElementId
                || submission.ActionId != cut.DeclineOffer.ActionId)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var decline = session.CurrentCombatRetreatMove is { } previousMove
                ? CampaignNativeCombatRetreatDeclineV15.CreateAfterMove(
                    selected, barrage, previousMove, session.CurrentCombatRetreatWorld!,
                    session.CurrentCombatRetreatMoves[0].OriginLocationId,
                    session.Context, session.CurrentContacts, submission.ActionId)
                : CampaignNativeCombatRetreatDeclineV15.Create(selected, barrage,
                    session.Context, session.CurrentContacts, submission.ActionId);
            var contacts = session.CurrentContacts.AdvanceNativeCombatRetreatDecline(decline);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(decline), contacts, selected,
                combatBarrage: barrage, combatRetreatDecline: decline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                ammunitionLedger: session.CurrentAmmunitionLedger);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                decline.PriorStateVersion, decline.StateVersion, decline.PositionId,
                submission.Audience, submission.ActionId);
            var checkpoint = session.CurrentCombatRetreatMove is { } move
                ? decline.SerializeAfterMoveStateCanonical(
                    SerializeRetreatMoveState(session))
                : decline.SerializeStateCanonical(selected, barrage);
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                decline.SerializeCanonical(), checkpoint));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult SubmitRepeatedDefenderRbaDecline(
        ExerciseSession session, CampaignActionSubmission submission,
        string defenderElementId)
    {
        var runtime = session.CurrentRepeatAssault!;
        var selected = runtime.Selection;
        var barrage = runtime.Barrage!;
        if (submission.CampaignId != selected.CampaignId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion
            != (runtime.RetreatMoves.Count > 0
                ? runtime.RetreatMoves[^1].StateVersion : barrage.StateVersion))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != barrage.NextPositionId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        try
        {
            var cut = QueryContactRbaCut(session);
            var audience = cut.DefenderSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            if (submission.Audience != audience
                || defenderElementId != cut.DefenderElementId
                || submission.ActionId != cut.DeclineOffer.ActionId)
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var decline = runtime.RetreatMoves.Count > 0
                ? CampaignNativeCombatRetreatDeclineV15.CreateAfterMove(
                    selected, barrage, runtime.RetreatMoves[^1],
                    runtime.RetreatWorld!, runtime.RetreatMoves[0].OriginLocationId,
                    session.Context, session.CurrentContacts, submission.ActionId)
                : CampaignNativeCombatRetreatDeclineV15.Create(
                    selected, barrage, session.Context,
                    session.CurrentContacts, submission.ActionId);
            var contacts = session.CurrentContacts
                .AdvanceNativeCombatRetreatDecline(decline);
            var next = session.WithRepeatAssaultEvent(decline, contacts);
            var receipt = new CampaignActionAcceptanceReceipt(
                selected.CampaignId, decline.PriorStateVersion,
                decline.StateVersion, decline.NextPositionId,
                submission.Audience, submission.ActionId);
            var checkpoint = runtime.RetreatMoves.Count > 0
                ? decline.SerializeAfterMoveStateCanonical(
                    runtime.SerializeRetreatMoveState())
                : decline.SerializeStateCanonical(selected, barrage);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, decline.SerializeCanonical(), checkpoint));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult SubmitContactOwnerForceSeal(
        ExerciseSession session, CampaignActionSubmission submission, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (string.IsNullOrWhiteSpace(elementId)
            || !CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (session.CurrentRepeatAssault is
            {
                Decline: not null,
                ForceCompletion: null, Seals.Count: < 2
            })
            return SubmitRepeatedForceSeal(session, submission, elementId);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatRetreatDecline is not { } decline
            || session.CurrentCombatForceCancellation is not null
            || session.CurrentCombatForceCompletion is not null
            || session.CurrentCombatForceSeals.Count == 2)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.CampaignId != selected.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != session.CurrentContacts.AuthorityVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != decline.NextPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var side = submission.Audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => (LandSide?)null,
        };
        if (side is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        try
        {
            var cut = QueryContactForceAssignmentCut(session, side.Value);
            if (cut.SealOffer.ElementId != elementId
                || cut.SealOffer.ActionId != submission.ActionId)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var first = session.CurrentCombatForceSeals.Count == 0
                ? null : session.CurrentCombatForceSeals[0];
            var seal = CampaignNativeCombatForceSealV17.Create(selected, decline,
                session.CurrentCombatRetreatWorld ?? selected.PriorSnapshot.World,
                session.Context, session.CurrentContacts, side.Value,
                submission.ActionId, first);
            var contacts = session.CurrentContacts.AdvanceNativeCombatForceSeal(seal);
            var seals = session.CurrentCombatForceSeals.Append(seal).ToArray();
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(seal), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: decline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: seals,
                ammunitionLedger: session.CurrentAmmunitionLedger);
            var priorState = SerializeRetreatDeclineState(session);
            foreach (var previous in session.CurrentCombatForceSeals)
                priorState = previous.SerializeStateCanonical(priorState);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                seal.PriorStateVersion, seal.StateVersion, seal.PositionId,
                submission.Audience, submission.ActionId);
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                seal.SerializeCanonical(), seal.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeCombatForceAssignment(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                ForceCompletion: null,
                Seals.Count: 2
            })
            return CompleteRepeatedForceAssignment(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatRetreatDecline is not { } decline
            || session.CurrentCombatForceCancellation is not null
            || session.CurrentCombatForceSeals.Count != 2
            || session.CurrentCombatForceCompletion is not null
            || session.CurrentContacts.AuthorityVersion
                != session.CurrentCombatForceSeals[^1].StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var completion = CampaignNativeCombatForceCompletionV18.Create(selected,
                decline, session.CurrentCombatForceSeals);
            var contacts = session.CurrentContacts.AdvanceNativeCombatForceCompletion(completion);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(completion), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: decline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: session.CurrentCombatForceSeals,
                combatForceCompletion: completion,
                ammunitionLedger: session.CurrentAmmunitionLedger);
            var priorState = SerializeRetreatDeclineState(session);
            foreach (var seal in session.CurrentCombatForceSeals)
                priorState = seal.SerializeStateCanonical(priorState);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                completion.PriorStateVersion, completion.StateVersion,
                completion.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(completion.SerializeCanonical()))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                completion.SerializeCanonical(), completion.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult SubmitRepeatedForceSeal(ExerciseSession session,
        CampaignActionSubmission submission, string elementId)
    {
        var runtime = session.CurrentRepeatAssault!;
        var selected = runtime.Selection;
        var decline = runtime.Decline!;
        if (submission.CampaignId != selected.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != session.CurrentContacts.AuthorityVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != decline.NextPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var side = submission.Audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => (LandSide?)null,
        };
        if (side is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        try
        {
            var cut = QueryContactForceAssignmentCut(session, side.Value);
            if (cut.SealOffer.ElementId != elementId
                || cut.SealOffer.ActionId != submission.ActionId)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var first = runtime.Seals.Count == 0 ? null : runtime.Seals[0];
            var seal = CampaignNativeCombatForceSealV17.Create(selected, decline,
                runtime.RetreatWorld ?? selected.World,
                session.Context, session.CurrentContacts,
                side.Value, submission.ActionId, first);
            var contacts = session.CurrentContacts.AdvanceNativeCombatForceSeal(seal);
            var next = session.WithRepeatAssaultEvent(seal, contacts);
            var priorState = runtime.SerializeDeclineState();
            foreach (var previous in runtime.Seals)
                priorState = previous.SerializeStateCanonical(priorState);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                seal.PriorStateVersion, seal.StateVersion, seal.PositionId,
                submission.Audience, submission.ActionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt,
                seal.SerializeCanonical(), seal.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult CompleteRepeatedForceAssignment(ExerciseSession session)
    {
        var runtime = session.CurrentRepeatAssault!;
        var selected = runtime.Selection;
        if (session.CurrentContacts.AuthorityVersion != runtime.Seals[^1].StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var completion = CampaignNativeCombatForceCompletionV18.Create(selected,
                runtime.Decline!, runtime.Seals);
            var contacts = session.CurrentContacts.AdvanceNativeCombatForceCompletion(completion);
            var next = session.WithRepeatAssaultEvent(completion, contacts);
            var priorState = runtime.SerializeDeclineState();
            foreach (var seal in runtime.Seals)
                priorState = seal.SerializeStateCanonical(priorState);
            var bytes = completion.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                completion.PriorStateVersion, completion.StateVersion,
                completion.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}");
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt,
                bytes, completion.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CancelNativeCombatRba(
        ExerciseSession session, CampaignDecisionTimingConfigurationV1? configuration,
        long openedAtUnixMilliseconds, long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason)
    {
        ArgumentNullException.ThrowIfNull(session);
        var repeated = session.CurrentRepeatAssault;
        CampaignCombatSelectionFrame? selected = repeated?.Selection
            ?? (session.CurrentCombatDecision is { } initial ? (CampaignCombatSelectionFrame)initial : null);
        var barrage = repeated?.Barrage ?? session.CurrentCombatBarrage;
        var move = repeated is not null ? (repeated.RetreatMoves.Count > 0 ? repeated.RetreatMoves[^1] : null) : session.CurrentCombatRetreatMove;
        if (selected is null || barrage is null
            || (repeated is not null ? repeated.Decline is not null || repeated.RbaCancellation is not null
                : session.CurrentCombatRetreatDecline is not null || session.CurrentCombatRbaCancellation is not null)
            || QueryCheckpoint(session).StateVersion != (move?.StateVersion ?? barrage.StateVersion)
            || (configuration is not null && (configuration.RulesetHash != selected.PriorSnapshot.RulesetHash
                || configuration.ContentHash != session.Context.ArtifactV6!.Identity.Hash))
            || (configuration is null && reason != "rba-unavailable"))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            if (reason != "rba-unavailable" && (configuration is null
                || checked(deadlineUnixMilliseconds - openedAtUnixMilliseconds) != configuration.Budget("rba")))
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var cancellation = CampaignNativeCombatRbaCancellationV33.Create(selected, barrage, move,
                configuration?.Hash ?? CampaignNativeCombatRbaCancellationV33.NoTimingConfigurationHash,
                openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds,
                cancelledAtUnixMilliseconds, reason);
            var contacts = session.CurrentContacts.AdvanceNativeCombatRbaCancellation(cancellation);
            var successor = repeated is not null
                ? session.WithRepeatAssaultEvent(cancellation, contacts)
                : new ExerciseSession(session.CurrentSnapshot, session.Context,
                    session.CurrentHistory.Add(cancellation), contacts, session.CurrentCombatDecision,
                    combatBarrage: barrage, combatRetreatMove: move,
                    combatRetreatWorld: session.CurrentCombatRetreatWorld,
                    combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                    combatRetreatMoves: session.CurrentCombatRetreatMoves,
                    combatRbaCancellation: cancellation,
                    ammunitionLedger: session.CurrentAmmunitionLedger);
            var priorState = repeated is not null
                ? move is null ? barrage.SerializeStateCanonical(selected) : repeated.SerializeRetreatMoveState()
                : move is null ? barrage.SerializeStateCanonical(selected) : SerializeRetreatMoveState(session);
            var bytes = cancellation.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                cancellation.PriorStateVersion, cancellation.StateVersion, cancellation.NextPositionId,
                CampaignActionAudience.System, Hash(bytes));
            return ExerciseStepResult.Accepted(successor,
                new ExerciseStepEvidence(receipt, bytes, cancellation.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CancelNativeCombatForceAssignment(
        ExerciseSession session, CampaignDecisionTimingConfigurationV1? configuration,
        long openedAtUnixMilliseconds, long deadlineUnixMilliseconds,
        long highWaterUnixMilliseconds, long? cancelledAtUnixMilliseconds,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                Decline: not null,
                ForceCompletion: null,
                ForceCancellation: null,
                Seals.Count: < 2
            })
            return CancelRepeatedForceAssignment(session, configuration,
                openedAtUnixMilliseconds, deadlineUnixMilliseconds,
                highWaterUnixMilliseconds, cancelledAtUnixMilliseconds, reason);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatRetreatDecline is not { } decline
            || session.CurrentCombatForceSeals.Count > 1
            || session.CurrentCombatForceCompletion is not null
            || session.CurrentCombatForceCancellation is not null
            || (configuration is not null
                && configuration.RulesetHash != selected.PriorSnapshot.RulesetHash)
            || (configuration is null && reason != "force-unavailable")
            || (reason == "force-unavailable"
                ? openedAtUnixMilliseconds != -1
                    || deadlineUnixMilliseconds != -1
                    || highWaterUnixMilliseconds != -1
                : configuration is null || openedAtUnixMilliseconds < 0
                    || deadlineUnixMilliseconds - openedAtUnixMilliseconds
                        != configuration.Budget("force-assignment")))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            if (reason == "force-unavailable"
                && configuration is not null
                && CanCompleteNativeCombatForceAssignment(session))
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var cancellation = CampaignNativeCombatForceCancellationV19.Create(
                selected, decline, session.CurrentCombatForceSeals,
                configuration?.Hash
                    ?? CampaignNativeCombatForceCancellationV19.NoTimingConfigurationHash,
                openedAtUnixMilliseconds,
                deadlineUnixMilliseconds, highWaterUnixMilliseconds,
                cancelledAtUnixMilliseconds, reason);
            var contacts = session.CurrentContacts.AdvanceNativeCombatForceCancellation(cancellation);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(cancellation), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: decline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: session.CurrentCombatForceSeals,
                combatForceCancellation: cancellation,
                ammunitionLedger: session.CurrentAmmunitionLedger);
            var priorState = SerializeRetreatDeclineState(session);
            foreach (var seal in session.CurrentCombatForceSeals)
                priorState = seal.SerializeStateCanonical(priorState);
            var bytes = cancellation.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                cancellation.PriorStateVersion, cancellation.StateVersion,
                cancellation.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                bytes, cancellation.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static bool CanCompleteNativeCombatForceAssignment(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        // #476: a repeated round is checked against its own selection, decline and World, with the
        // same eligibility rule the repeated result (CampaignNativeCombatAssaultInputs) applies.
        if (session.CurrentRepeatAssault is { } repeat)
        {
            if (repeat.Decline is not { } repeatDecline
                || repeat.ForceCompletion is not null
                || repeat.ForceCancellation is not null)
                return false;
            try
            {
                return CampaignNativeCombatForceEligibility.CanCompleteAssault(repeat.Selection,
                    repeatDecline, repeat.RetreatWorld ?? repeat.Selection.World, session.Context,
                    CurrentAmmunition(session, session.CurrentHistory));
            }
            catch (Exception exception) when (exception is ArgumentException
                or InvalidOperationException or InvalidCampaignHistoryException)
            {
                return false;
            }
        }
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatRetreatDecline is not { } decline
            || session.CurrentCombatForceCompletion is not null
            || session.CurrentCombatForceCancellation is not null)
            return false;
        try
        {
            return CampaignNativeCombatForceEligibility.CanCompleteAssault(
                selected, decline,
                session.CurrentCombatRetreatWorld ?? selected.PriorSnapshot.World,
                session.Context, CurrentAmmunition(session, session.CurrentHistory));
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or InvalidCampaignHistoryException)
        {
            return false;
        }
    }

    private static ExerciseStepResult CancelRepeatedForceAssignment(
        ExerciseSession session, CampaignDecisionTimingConfigurationV1? configuration,
        long openedAtUnixMilliseconds, long deadlineUnixMilliseconds,
        long highWaterUnixMilliseconds, long? cancelledAtUnixMilliseconds,
        string reason)
    {
        var runtime = session.CurrentRepeatAssault!;
        var selected = runtime.Selection;
        // #476 (owner ruling 2026-11-20): a repeated round accepts the same V19 force-unavailable
        // cancellation as the initial round: no clock evidence, and with timing authority only when
        // the round cannot be paid. It also requires that no seal exists yet, the invariant the Host
        // verifies for force-unavailable (no retained seal). Timed cancellations are unchanged.
        var unavailable = reason == "force-unavailable";
        if ((configuration is not null
                && configuration.RulesetHash != selected.PriorSnapshot.RulesetHash)
            || (unavailable
                ? openedAtUnixMilliseconds != -1 || deadlineUnixMilliseconds != -1
                    || highWaterUnixMilliseconds != -1 || runtime.Seals.Count != 0
                : configuration is null || openedAtUnixMilliseconds < 0
                    || deadlineUnixMilliseconds - openedAtUnixMilliseconds
                        != configuration.Budget("force-assignment"))
            || session.CurrentContacts.AuthorityVersion
                != (runtime.Seals.Count == 0
                    ? runtime.Decline!.StateVersion : runtime.Seals[^1].StateVersion))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            if (unavailable && configuration is not null
                && CanCompleteNativeCombatForceAssignment(session))
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var cancellation = CampaignNativeCombatForceCancellationV19.Create(
                selected, runtime.Decline!, runtime.Seals,
                configuration?.Hash
                    ?? CampaignNativeCombatForceCancellationV19.NoTimingConfigurationHash,
                openedAtUnixMilliseconds,
                deadlineUnixMilliseconds, highWaterUnixMilliseconds,
                cancelledAtUnixMilliseconds, reason);
            var contacts = session.CurrentContacts
                .AdvanceNativeCombatForceCancellation(cancellation);
            var next = session.WithRepeatAssaultEvent(cancellation, contacts);
            var priorState = runtime.SerializeDeclineState();
            foreach (var seal in runtime.Seals)
                priorState = seal.SerializeStateCanonical(priorState);
            var bytes = cancellation.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                cancellation.PriorStateVersion, cancellation.StateVersion,
                cancellation.NextPositionId, CampaignActionAudience.System,
                Hash(bytes));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, bytes, cancellation.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeCombatAntiArmor(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is { AntiArmor: null } repeated
            && (repeated.ForceCompletion is not null
                || repeated.ForceCancellation is not null))
            return CompleteRepeatedAntiArmor(session);
        if (session.CurrentCombatDecision is not { } selected
            || (session.CurrentCombatForceCompletion is null)
                == (session.CurrentCombatForceCancellation is null)
            || session.CurrentCombatAntiArmor is not null
            || session.CurrentContacts.AuthorityVersion
                != (session.CurrentCombatForceCompletion?.StateVersion
                    ?? session.CurrentCombatForceCancellation!.StateVersion))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var antiArmor = CampaignNativeCombatAntiArmorV20.Create(selected,
                session.CurrentCombatForceCompletion,
                session.CurrentCombatForceCancellation, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatAntiArmor(antiArmor);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(antiArmor), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: session.CurrentCombatRetreatDecline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: session.CurrentCombatForceSeals,
                combatForceCompletion: session.CurrentCombatForceCompletion,
                combatForceCancellation: session.CurrentCombatForceCancellation,
                combatAntiArmor: antiArmor,
                ammunitionLedger: session.CurrentAmmunitionLedger);
            var priorState = SerializeRetreatDeclineState(session);
            foreach (var seal in session.CurrentCombatForceSeals)
                priorState = seal.SerializeStateCanonical(priorState);
            if (session.CurrentCombatForceCompletion is { } completion)
                priorState = completion.SerializeStateCanonical(priorState);
            if (session.CurrentCombatForceCancellation is { } cancellation)
                priorState = cancellation.SerializeStateCanonical(priorState);
            var bytes = antiArmor.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                antiArmor.PriorStateVersion, antiArmor.StateVersion,
                antiArmor.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                bytes, antiArmor.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult CompleteRepeatedAntiArmor(ExerciseSession session)
    {
        var runtime = session.CurrentRepeatAssault!;
        var selected = runtime.Selection;
        if (session.CurrentContacts.AuthorityVersion
            != (runtime.ForceCompletion?.StateVersion
                ?? runtime.ForceCancellation!.StateVersion))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var antiArmor = CampaignNativeCombatAntiArmorV20.Create(selected,
                runtime.ForceCompletion, runtime.ForceCancellation, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatAntiArmor(antiArmor);
            var next = session.WithRepeatAssaultEvent(antiArmor, contacts);
            var priorState = runtime.SerializeDeclineState();
            foreach (var seal in runtime.Seals)
                priorState = seal.SerializeStateCanonical(priorState);
            priorState = runtime.ForceCompletion is { } completion
                ? completion.SerializeStateCanonical(priorState)
                : runtime.ForceCancellation!.SerializeStateCanonical(priorState);
            var bytes = antiArmor.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                antiArmor.PriorStateVersion, antiArmor.StateVersion,
                antiArmor.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}");
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt,
                bytes, antiArmor.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeCombatCancelledAssault(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                ForceCancellation: not null,
                AntiArmor: not null,
                CancelledAssault: null
            })
            return CompleteRepeatedCancelledAssault(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatAntiArmor is not { } antiArmor
            || session.CurrentCombatForceCancellation is null
            || session.CurrentCombatCancelledAssault is not null
            || session.CurrentContacts.AuthorityVersion != antiArmor.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var cancelledAssault = CampaignNativeCombatCancelledAssaultV21.Create(
                selected, antiArmor);
            var contacts = session.CurrentContacts.AdvanceNativeCombatCancelledAssault(
                cancelledAssault);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(cancelledAssault), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: session.CurrentCombatRetreatDecline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: session.CurrentCombatForceSeals,
                combatForceCancellation: session.CurrentCombatForceCancellation,
                combatAntiArmor: antiArmor,
                combatCancelledAssault: cancelledAssault,
                ammunitionLedger: session.CurrentAmmunitionLedger);
            var priorState = SerializeRetreatDeclineState(session);
            foreach (var seal in session.CurrentCombatForceSeals)
                priorState = seal.SerializeStateCanonical(priorState);
            priorState = session.CurrentCombatForceCancellation.SerializeStateCanonical(priorState);
            priorState = antiArmor.SerializeStateCanonical(priorState);
            var bytes = cancelledAssault.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                cancelledAssault.PriorStateVersion, cancelledAssault.StateVersion,
                cancelledAssault.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                bytes, cancelledAssault.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult CompleteRepeatedCancelledAssault(
        ExerciseSession session)
    {
        var runtime = session.CurrentRepeatAssault!;
        var selected = runtime.Selection;
        if (session.CurrentContacts.AuthorityVersion
            != runtime.AntiArmor!.StateVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var cancelled = CampaignNativeCombatCancelledAssaultV21.Create(
                selected, runtime.AntiArmor);
            var contacts = session.CurrentContacts
                .AdvanceNativeCombatCancelledAssault(cancelled);
            var next = session.WithRepeatAssaultEvent(cancelled, contacts);
            var priorState = runtime.SerializeDeclineState();
            foreach (var seal in runtime.Seals)
                priorState = seal.SerializeStateCanonical(priorState);
            priorState = runtime.ForceCancellation!
                .SerializeStateCanonical(priorState);
            priorState = runtime.AntiArmor.SerializeStateCanonical(priorState);
            var bytes = cancelled.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                cancelled.PriorStateVersion, cancelled.StateVersion,
                cancelled.NextPositionId, CampaignActionAudience.System,
                Hash(bytes));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, bytes, cancelled.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    // #446: the #437 seed is deliberately absent after a later repeat cycle
    // (ExerciseSession.WithPostRepeatEvent). Fall back to the pre-#437
    // from-genesis fold over the same history, never to the unspent initial
    // ledger, which would let a later-cycle result spend ammunition twice.
    private static CampaignCombatAmmunitionLedgerV1 CurrentAmmunition(ExerciseSession session,
        IEnumerable<object> preSpendHistory) =>
        session.CurrentAmmunitionLedger
            ?? CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(session.Context, preSpendHistory);

    internal static ExerciseStepResult ResolveNativeCombatAssault(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                AntiArmor: not null,
                Result: null
            })
            return ResolveRepeatedCombatAssault(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatForceCompletion is not { } completion
            || session.CurrentCombatAntiArmor is not { } antiArmor
            || session.CurrentCombatAssaultResult is not null
            || session.CurrentCombatCancelledAssault is not null
            || session.CurrentCombatForceCancellation is not null
            || session.CurrentContacts.AuthorityVersion != antiArmor.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            // Perf seeding path (Phase 1 of #436): seed from the session's
            // own carried ammunition ledger (fresh as of this round's last
            // clean boundary or last repeat settlement -- see
            // ExerciseSession.CurrentAmmunitionLedger) instead of rescanning
            // the full accepted-event history on every Combat result. No
            // Combat result can have been accepted since that ledger was
            // captured without this exact session reflecting it (nothing
            // else advances CurrentCombatAssaultResult), so there is no
            // tail to fold here -- CreateInitial only applies to the very
            // first Combat result in a campaign, or after a later repeat
            // cycle that intentionally left the ledger null (see
            // ExerciseSession.WithPostRepeatEvent).
            var ammunition = CurrentAmmunition(session, session.CurrentHistory);
            var inputs = CampaignNativeCombatAssaultInputs.Create(selected, completion,
                antiArmor, session.CurrentCombatForceSeals,
                session.CurrentCombatRetreatWorld ?? selected.PriorSnapshot.World,
                session.Context, ammunition);
            var result = CampaignNativeCombatAssaultResultV22.Create(selected,
                antiArmor, inputs, selected.PriorSnapshot.RandomState);
            var contacts = session.CurrentContacts.AdvanceNativeCombatAssaultResult(result);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(result), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: session.CurrentCombatRetreatDecline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: session.CurrentCombatForceSeals,
                combatForceCompletion: completion,
                combatAntiArmor: antiArmor,
                combatAssaultResult: result,
                combatPaidWorld: inputs.PaidWorld,
                // ammunition is the pre-spend ledger used to build `inputs`
                // above; it stays the session's CurrentAmmunitionLedger
                // unchanged until SettleNativeCombatWorld folds the spend
                // once settlement is accepted, since that function needs
                // to re-derive the same pre-spend `inputs` to verify the
                // already-accepted result.
                ammunitionLedger: ammunition);
            var priorState = SerializeRetreatDeclineState(session);
            foreach (var seal in session.CurrentCombatForceSeals)
                priorState = seal.SerializeStateCanonical(priorState);
            priorState = completion.SerializeStateCanonical(priorState);
            priorState = antiArmor.SerializeStateCanonical(priorState);
            var bytes = result.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                result.PriorStateVersion, result.StateVersion, result.NextPositionId,
                CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(
                receipt, bytes, result.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult ResolveRepeatedCombatAssault(ExerciseSession session)
    {
        var runtime = session.CurrentRepeatAssault!;
        var selected = runtime.Selection;
        if (session.CurrentContacts.AuthorityVersion
            != runtime.AntiArmor!.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            // Perf seeding path (Phase 1 of #436): see the matching
            // comment in ResolveNativeCombatAssault -- same reasoning
            // applies to the repeated-assault path.
            var ammunition = CurrentAmmunition(session, session.CurrentHistory);
            var inputs = CampaignNativeCombatAssaultInputs.Create(selected,
                runtime.ForceCompletion!, runtime.AntiArmor,
                runtime.Seals, runtime.RetreatWorld ?? selected.World,
                session.Context, ammunition);
            var result = CampaignNativeCombatAssaultResultV22.Create(selected,
                runtime.AntiArmor, inputs, selected.RandomState);
            var contacts = session.CurrentContacts.AdvanceNativeCombatAssaultResult(result);
            var next = session.WithRepeatAssaultEvent(result, contacts);
            var priorState = runtime.SerializeDeclineState();
            foreach (var seal in runtime.Seals)
                priorState = seal.SerializeStateCanonical(priorState);
            priorState = runtime.ForceCompletion!.SerializeStateCanonical(priorState);
            priorState = runtime.AntiArmor.SerializeStateCanonical(priorState);
            var bytes = result.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                result.PriorStateVersion, result.StateVersion,
                result.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}");
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, bytes, result.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static CampaignContactRetreatAfterAssaultCut QueryContactPostResultRetreatCut(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                Result: { } repeatedResult,
                PaidWorld: { } repeatedPaidWorld,
                PostResultRetreat: null
            } repeated)
            return CampaignContactRetreatAfterAssaultCut.Project(
                repeated.Selection, repeatedResult, repeatedPaidWorld,
                session.Context, session.CurrentContacts);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatAssaultResult is not { } result
            || session.CurrentCombatPaidWorld is not { } paidWorld
            || session.CurrentCombatPostResultRetreat is not null)
            throw new InvalidOperationException("Defender retreat has no unresolved assault result.");
        return CampaignContactRetreatAfterAssaultCut.Project(selected, result,
            paidWorld, session.Context, session.CurrentContacts);
    }

    internal static ExerciseStepResult SubmitContactPostResultRetreat(
        ExerciseSession session, CampaignActionSubmission submission,
        string defenderElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission)
            || string.IsNullOrWhiteSpace(defenderElementId))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (session.CurrentRepeatAssault is
            {
                Result.Facts.RequiredRetreat: 1,
                PostResultRetreat: null
            })
            return SubmitRepeatedPostResultRetreat(session, submission,
                defenderElementId);
        if (session.CurrentCombatAssaultResult is not { } result
            || session.CurrentCombatPostResultRetreat is not null
            || result.Facts.RequiredRetreat != 1)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.CampaignId != result.CampaignId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != result.StateVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != result.PositionId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        try
        {
            var cut = QueryContactPostResultRetreatCut(session);
            var audience = cut.DefenderSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            if (submission.Audience != audience
                || defenderElementId != cut.DefenderElementId
                || (submission.ActionId != cut.RefuseOffer.ActionId
                    && !cut.MoveOffers.Any(value => value.ActionId == submission.ActionId)))
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.ActionNotLegal);
            return AcceptPostResultRetreat(session, submission.ActionId, audience);
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult TimeoutNativeCombatRetreat(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            _ = QueryContactPostResultRetreatCut(session);
            if ((configuration is not null
                    && (configuration.RulesetHash != session.CurrentSnapshot.RulesetHash
                        || configuration.ContentHash != session.Context.ArtifactV6!.Identity.Hash))
                || (configuration is null && reason != "retreat-unavailable")
                || (reason != "retreat-unavailable" && (configuration is null
                    || checked(deadlineUnixMilliseconds - openedAtUnixMilliseconds) != configuration.Budget("retreat"))))
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var runtime = session.CurrentRepeatAssault;
            var repeated = runtime is { Result: not null, PostResultRetreat: null };
            var timeout = CampaignNativeCombatRetreatTimeoutV37.CreateTimeout(
                repeated ? runtime!.Selection : session.CurrentCombatDecision!,
                repeated ? runtime!.Result! : session.CurrentCombatAssaultResult!,
                repeated ? runtime!.PaidWorld! : session.CurrentCombatPaidWorld!, session.Context,
                session.CurrentContacts, configuration?.Hash ?? CampaignNativeCombatRetreatTimeoutV37.NoTimingConfigurationHash,
                openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds,
                cancelledAtUnixMilliseconds, reason);
            return repeated
                ? AcceptRepeatedPostResultRetreat(session, null, CampaignActionAudience.System, timeout)
                : AcceptPostResultRetreat(session, null, CampaignActionAudience.System, timeout);
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    internal static ExerciseStepResult CompleteNoPostResultRetreat(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                Result.Facts.RequiredRetreat: 0,
                PostResultRetreat: null
            })
            return AcceptRepeatedPostResultRetreat(session, null,
                CampaignActionAudience.System);
        if (session.CurrentCombatAssaultResult is not { Facts.RequiredRetreat: 0 }
            || session.CurrentCombatPostResultRetreat is not null)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            return AcceptPostResultRetreat(session, null, CampaignActionAudience.System);
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult SubmitRepeatedPostResultRetreat(
        ExerciseSession session, CampaignActionSubmission submission,
        string defenderElementId)
    {
        var result = session.CurrentRepeatAssault!.Result!;
        if (submission.CampaignId != result.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != result.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != result.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        try
        {
            var cut = QueryContactPostResultRetreatCut(session);
            var audience = cut.DefenderSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            if (submission.Audience != audience
                || defenderElementId != cut.DefenderElementId
                || (submission.ActionId != cut.RefuseOffer.ActionId
                    && !cut.MoveOffers.Any(value => value.ActionId == submission.ActionId)))
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            return AcceptRepeatedPostResultRetreat(session, submission.ActionId, audience);
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult AcceptRepeatedPostResultRetreat(
        ExerciseSession session, string? actionId, CampaignActionAudience audience,
        CampaignNativeCombatRetreatTimeoutV37? timeout = null)
    {
        try
        {
            var runtime = session.CurrentRepeatAssault!;
            var result = runtime.Result!;
            var retreat = timeout ?? CampaignNativeCombatRetreatAfterAssaultV23.Create(
                runtime.Selection, result, runtime.PaidWorld!, session.Context,
                session.CurrentContacts, actionId);
            var contacts = session.CurrentContacts.AdvanceNativeCombatRetreatAfterAssault(retreat);
            var next = session.WithRepeatAssaultEvent(retreat, contacts);
            var priorState = runtime.SerializeDeclineState();
            foreach (var seal in runtime.Seals)
                priorState = seal.SerializeStateCanonical(priorState);
            priorState = runtime.ForceCompletion!.SerializeStateCanonical(priorState);
            priorState = runtime.AntiArmor!.SerializeStateCanonical(priorState);
            priorState = result.SerializeStateCanonical(priorState);
            var bytes = retreat.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(result.CampaignId,
                retreat.PriorStateVersion, retreat.StateVersion,
                retreat.NextPositionId, audience, actionId ?? Hash(bytes));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt,
                bytes, retreat.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult AcceptPostResultRetreat(
        ExerciseSession session, string? actionId, CampaignActionAudience audience,
        CampaignNativeCombatRetreatTimeoutV37? timeout = null)
    {
        var selected = session.CurrentCombatDecision!;
        var result = session.CurrentCombatAssaultResult!;
        var retreat = timeout ?? CampaignNativeCombatRetreatAfterAssaultV23.Create(selected,
            result, session.CurrentCombatPaidWorld!, session.Context,
            session.CurrentContacts, actionId);
        var contacts = session.CurrentContacts
            .AdvanceNativeCombatRetreatAfterAssault(retreat);
        var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
            session.CurrentHistory.Add(retreat), contacts, selected,
            combatBarrage: session.CurrentCombatBarrage,
            combatRetreatDecline: session.CurrentCombatRetreatDecline,
            combatRetreatMove: session.CurrentCombatRetreatMove,
            combatRetreatWorld: session.CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: session.CurrentCombatRetreatMoves,
            combatForceSeals: session.CurrentCombatForceSeals,
            combatForceCompletion: session.CurrentCombatForceCompletion,
            combatAntiArmor: session.CurrentCombatAntiArmor,
            combatAssaultResult: result,
            combatPaidWorld: session.CurrentCombatPaidWorld,
            combatPostResultRetreat: retreat,
            ammunitionLedger: session.CurrentAmmunitionLedger);
        var bytes = retreat.SerializeCanonical();
        var receipt = new CampaignActionAcceptanceReceipt(result.CampaignId,
            retreat.PriorStateVersion, retreat.StateVersion,
            retreat.NextPositionId, audience, actionId ?? Hash(bytes));
        return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(
            receipt, bytes, retreat.SerializeStateCanonical(
                SerializeNativeCombatResultState(session))));
    }

    internal static ExerciseStepResult SettleNativeCombatWorld(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                PostResultRetreat: not null,
                Settlement: null
            })
            return SettleRepeatedCombatWorld(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatAssaultResult is not { } result
            || session.CurrentCombatPostResultRetreat is not { } retreat
            || session.CurrentCombatSettlement is not null
            || session.CurrentContacts.AuthorityVersion != retreat.StateVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            // Perf seeding path (Phase 1 of #436): this needs the exact
            // same pre-spend ledger ResolveNativeCombatAssault used to
            // build `inputs` for `result` (so the re-derived `inputs` here
            // match, letting settlement/World projection reproduce
            // deterministically) -- that is exactly
            // session.CurrentAmmunitionLedger, which ResolveNativeCombatAssault
            // deliberately left unchanged (pre-spend) on this session
            // rather than folding the result's spend in immediately. The
            // old `priorHistory = CurrentHistory.Take(Count - 2)` rescan
            // existed only to exclude `result` itself from the fold for
            // the same reason; seeding removes the need for it entirely.
            // History minus the result and its retreat choice is the pre-spend cut.
            var ammunition = CurrentAmmunition(session,
                session.CurrentHistory.Take(session.CurrentHistory.Count - 2));
            var inputs = CampaignNativeCombatAssaultInputs.Create(selected,
                session.CurrentCombatForceCompletion!, session.CurrentCombatAntiArmor!,
                session.CurrentCombatForceSeals,
                session.CurrentCombatRetreatWorld ?? selected.PriorSnapshot.World,
                session.Context, ammunition);
            var settlement = CampaignNativeCombatSettlementV24.Create(result,
                retreat, inputs, session.Context);
            var projected = CampaignNativeCombatSettledWorldV24.Project(result,
                retreat, inputs, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatSettlement(
                inputs.PaidWorld, projected.World, retreat, settlement,
                session.Context);
            // This settlement is the point this engagement's ammunition
            // spend becomes final, so fold exactly the one accepted
            // `result` event forward now -- an O(1) fold re-validated
            // against the hash chain (see
            // CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents), not a
            // blindly-trusted value -- so the next engagement in this
            // round (a repeat) can keep seeding from
            // CurrentAmmunitionLedger instead of rescanning.
            var settledAmmunition = CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(
                ammunition, new object[] { result });
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(settlement), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: session.CurrentCombatRetreatDecline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: session.CurrentCombatForceSeals,
                combatForceCompletion: session.CurrentCombatForceCompletion,
                combatAntiArmor: session.CurrentCombatAntiArmor,
                combatAssaultResult: result,
                combatPaidWorld: inputs.PaidWorld,
                combatPostResultRetreat: retreat,
                combatSettlement: settlement,
                combatSettledWorld: projected.World,
                ammunitionLedger: settledAmmunition);
            var bytes = settlement.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(result.CampaignId,
                settlement.PriorStateVersion, settlement.StateVersion,
                settlement.NextPositionId, CampaignActionAudience.System,
                Hash(bytes));
            var priorState = retreat.SerializeStateCanonical(
                SerializeNativeCombatResultState(session));
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(
                receipt, bytes, settlement.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult SettleRepeatedCombatWorld(ExerciseSession session)
    {
        var runtime = session.CurrentRepeatAssault!;
        var result = runtime.Result!;
        var retreat = runtime.PostResultRetreat!;
        if (session.CurrentContacts.AuthorityVersion != retreat.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var settlement = CampaignNativeCombatSettlementV24.Create(result,
                retreat, runtime.Inputs!, session.Context);
            var projected = CampaignNativeCombatSettledWorldV24.Project(result,
                retreat, runtime.Inputs!, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatSettlement(
                runtime.PaidWorld!, projected.World, retreat, settlement,
                session.Context);
            var next = session.WithRepeatAssaultEvent(settlement, contacts);
            var priorState = runtime.SerializeDeclineState();
            foreach (var seal in runtime.Seals)
                priorState = seal.SerializeStateCanonical(priorState);
            priorState = runtime.ForceCompletion!.SerializeStateCanonical(priorState);
            priorState = runtime.AntiArmor!.SerializeStateCanonical(priorState);
            priorState = result.SerializeStateCanonical(priorState);
            priorState = retreat.SerializeStateCanonical(priorState);
            var bytes = settlement.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(result.CampaignId,
                settlement.PriorStateVersion, settlement.StateVersion,
                settlement.NextPositionId, CampaignActionAudience.System,
                Hash(bytes));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt,
                bytes, settlement.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static CampaignNativeCombatCustodyCutV25? QueryNativeCombatCustodyCut(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                Settlement: { } repeatedSettlement,
                SettledWorld: { } repeatedWorld, Custody: null
            })
            return CampaignNativeCombatCustodyCutV25.Project(repeatedSettlement,
                repeatedWorld, session.Context, session.CurrentContacts);
        if (session.CurrentCombatSettlement is not { } settlement
            || session.CurrentCombatSettledWorld is not { } world
            || session.CurrentContacts.AuthorityVersion != settlement.StateVersion)
            throw new InvalidOperationException("Custody requires the accepted normal World settlement.");
        return CampaignNativeCombatCustodyCutV25.Project(settlement,
            world, session.Context, session.CurrentContacts);
    }

    internal static ExerciseStepResult SubmitNativeCombatCustody(
        ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (session.CurrentRepeatAssault is
            {
                Settlement.PendingCaptives.Count: > 0,
                Custody: null
            })
            return SubmitRepeatedCombatCustody(session, submission);
        if (session.CurrentCombatSettlement is not { } settlement
            || settlement.PendingCaptives.Count == 0
            || session.CurrentCombatCustodyDisposition is not null)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.CampaignId != settlement.CampaignId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != settlement.StateVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != settlement.PositionId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        try
        {
            var cut = QueryNativeCombatCustodyCut(session)
                ?? throw new InvalidOperationException("Custody requires a captive lot.");
            var content = session.Context.ArtifactV6!.Definition.LegacyDefinition;
            var side = content.Elements.Single(value =>
                value.ElementId == cut.CaptorElementId).SideId;
            var audience = side switch
            {
                "axis" => CampaignActionAudience.Axis,
                "commonwealth" => CampaignActionAudience.Commonwealth,
                _ => throw new InvalidOperationException("Unknown captor side."),
            };
            if (submission.Audience != audience)
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var choice = cut.Choices.SingleOrDefault(value =>
                value.ActionId == submission.ActionId);
            if (choice is null)
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.ActionNotLegal);
            return AcceptNativeCombatCustody(session, cut, choice.Kind,
                audience, submission.ActionId);
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult TimeoutNativeCombatCustody(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var cut = QueryNativeCombatCustodyCut(session)
                ?? throw new InvalidOperationException("No pending captive lot.");
            if ((configuration is not null
                    && (configuration.RulesetHash != session.CurrentSnapshot.RulesetHash
                        || configuration.ContentHash != session.Context.ArtifactV6!.Identity.Hash))
                || (configuration is null && reason != "custody-unavailable")
                || (reason != "custody-unavailable" && (configuration is null
                    || checked(deadlineUnixMilliseconds - openedAtUnixMilliseconds) != configuration.Budget("custody"))))
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var repeated = session.CurrentRepeatAssault is { Settlement: not null, Custody: null };
            var settlement = repeated ? session.CurrentRepeatAssault!.Settlement! : session.CurrentCombatSettlement!;
            var world = repeated ? session.CurrentRepeatAssault!.SettledWorld! : session.CurrentCombatSettledWorld!;
            var timeout = CampaignNativeCombatCustodyTimeoutV36.CreateTimeout(session.CurrentSnapshot,
                settlement, world, cut, session.Context,
                configuration?.Hash ?? CampaignNativeCombatCustodyTimeoutV36.NoTimingConfigurationHash,
                openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds,
                cancelledAtUnixMilliseconds, reason);
            return repeated
                ? AcceptRepeatedCombatCustody(session, cut, "leave-unguarded", CampaignActionAudience.System, null, timeout)
                : AcceptNativeCombatCustody(session, cut, "leave-unguarded", CampaignActionAudience.System, null, timeout);
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    internal static ExerciseStepResult CompleteNoNativeCombatCustody(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                Settlement.PendingCaptives.Count: 0,
                Custody: null
            })
            return AcceptRepeatedCombatCustody(session, null,
                "not-required", CampaignActionAudience.System, null);
        if (session.CurrentCombatSettlement is not { } settlement
            || settlement.PendingCaptives.Count != 0
            || session.CurrentCombatCustodyDisposition is not null)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            return AcceptNativeCombatCustody(session, null, "not-required",
                CampaignActionAudience.System, null);
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult SubmitRepeatedCombatCustody(
        ExerciseSession session, CampaignActionSubmission submission)
    {
        var settlement = session.CurrentRepeatAssault!.Settlement!;
        if (submission.CampaignId != settlement.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != settlement.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != settlement.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        try
        {
            var cut = QueryNativeCombatCustodyCut(session)
                ?? throw new InvalidOperationException("Custody requires a captive lot.");
            var content = session.Context.ArtifactV6!.Definition.LegacyDefinition;
            var side = content.Elements.Single(value =>
                value.ElementId == cut.CaptorElementId).SideId;
            var audience = side switch
            {
                "axis" => CampaignActionAudience.Axis,
                "commonwealth" => CampaignActionAudience.Commonwealth,
                _ => throw new InvalidOperationException("Unknown captor side."),
            };
            if (submission.Audience != audience)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var choice = cut.Choices.SingleOrDefault(value =>
                value.ActionId == submission.ActionId);
            if (choice is null)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            return AcceptRepeatedCombatCustody(session, cut, choice.Kind,
                audience, submission.ActionId);
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult AcceptRepeatedCombatCustody(
        ExerciseSession session, CampaignNativeCombatCustodyCutV25? cut,
        string choiceKind, CampaignActionAudience audience, string? actionId,
        CampaignNativeCombatCustodyDispositionV25? systemDisposition = null)
    {
        try
        {
            var runtime = session.CurrentRepeatAssault!;
            var settlement = runtime.Settlement!;
            var world = runtime.SettledWorld!;
            if (session.CurrentContacts.AuthorityVersion != settlement.StateVersion)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var disposition = systemDisposition ?? CampaignNativeCombatCustodyDispositionV25.Create(
                session.CurrentSnapshot, settlement, world, cut, choiceKind,
                session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatCustodyDisposition(
                disposition);
            var next = session.WithRepeatAssaultEvent(disposition, contacts);
            var priorState = runtime.SerializeDeclineState();
            foreach (var seal in runtime.Seals)
                priorState = seal.SerializeStateCanonical(priorState);
            priorState = runtime.ForceCompletion!.SerializeStateCanonical(priorState);
            priorState = runtime.AntiArmor!.SerializeStateCanonical(priorState);
            priorState = runtime.Result!.SerializeStateCanonical(priorState);
            priorState = runtime.PostResultRetreat!.SerializeStateCanonical(priorState);
            priorState = settlement.SerializeStateCanonical(priorState);
            var bytes = disposition.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(settlement.CampaignId,
                disposition.PriorStateVersion, disposition.StateVersion,
                disposition.PositionId, audience, actionId ?? Hash(bytes));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt,
                bytes, disposition.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult AcceptNativeCombatCustody(
        ExerciseSession session, CampaignNativeCombatCustodyCutV25? cut,
        string choiceKind, CampaignActionAudience audience, string? actionId,
        CampaignNativeCombatCustodyDispositionV25? systemDisposition = null)
    {
        var settlement = session.CurrentCombatSettlement!;
        var settledWorld = session.CurrentCombatSettledWorld!;
        if (session.CurrentContacts.AuthorityVersion != settlement.StateVersion)
            throw new InvalidOperationException("Custody requires the accepted contact cut.");
        var disposition = systemDisposition ?? CampaignNativeCombatCustodyDispositionV25.Create(
            session.CurrentSnapshot, settlement, settledWorld, cut,
            choiceKind, session.Context);
        var projection = CampaignNativeCombatCustodyProjectionV25.Project(
            session.CurrentSnapshot, settlement, settledWorld, cut,
            choiceKind, session.Context);
        var contacts = session.CurrentContacts.AdvanceNativeCombatCustodyDisposition(
            disposition);
        var successor = new ExerciseSession(session.CurrentSnapshot,
            session.Context, session.CurrentHistory.Add(disposition), contacts,
            session.CurrentCombatDecision, combatBarrage: session.CurrentCombatBarrage,
            combatRetreatDecline: session.CurrentCombatRetreatDecline,
            combatRetreatMove: session.CurrentCombatRetreatMove,
            combatRetreatWorld: session.CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: session.CurrentCombatRetreatMoves,
            combatForceSeals: session.CurrentCombatForceSeals,
            combatForceCompletion: session.CurrentCombatForceCompletion,
            combatAntiArmor: session.CurrentCombatAntiArmor,
            combatAssaultResult: session.CurrentCombatAssaultResult,
            combatPaidWorld: session.CurrentCombatPaidWorld,
            combatPostResultRetreat: session.CurrentCombatPostResultRetreat,
            combatSettlement: settlement, combatSettledWorld: settledWorld,
            combatCustodyDisposition: disposition,
            combatCustodyWorld: projection.World,
            combatCustodyState: projection.Custody,
            ammunitionLedger: session.CurrentAmmunitionLedger);
        var bytes = disposition.SerializeCanonical();
        var receipt = new CampaignActionAcceptanceReceipt(settlement.CampaignId,
            disposition.PriorStateVersion, disposition.StateVersion,
            disposition.PositionId, audience, actionId ?? Hash(bytes));
        var priorState = settlement.SerializeStateCanonical(
            session.CurrentCombatPostResultRetreat!.SerializeStateCanonical(
                SerializeNativeCombatResultState(session)));
        return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(
            receipt, bytes, disposition.SerializeStateCanonical(priorState)));
    }

    internal static ExerciseStepResult CompleteNativeCombatSuccessor(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                Custody: not null,
                Successor: null
            })
            return CompleteRepeatedCombatSuccessor(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatAssaultResult is not { } result
            || session.CurrentCombatSettlement is not { } settlement
            || session.CurrentCombatCustodyDisposition is not { } custody
            || session.CurrentCombatCustodyWorld is not { } world
            || session.CurrentCombatSuccessor is not null
            || session.CurrentContacts.AuthorityVersion != custody.StateVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var successorEvent = CampaignNativeCombatSuccessorV26.Create(
                selected, result, settlement, custody, world,
                session.CurrentCombatCustodyState, session.Context);
            var projected = CampaignNativeCombatSuccessorV26.Project(
                selected, result, settlement, custody, world,
                session.CurrentCombatCustodyState, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatSuccessor(
                successorEvent);
            var next = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(successorEvent), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: session.CurrentCombatRetreatDecline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: session.CurrentCombatForceSeals,
                combatForceCompletion: session.CurrentCombatForceCompletion,
                combatAntiArmor: session.CurrentCombatAntiArmor,
                combatAssaultResult: result,
                combatPaidWorld: session.CurrentCombatPaidWorld,
                combatPostResultRetreat: session.CurrentCombatPostResultRetreat,
                combatSettlement: settlement,
                combatSettledWorld: session.CurrentCombatSettledWorld,
                combatCustodyDisposition: custody,
                combatCustodyWorld: world,
                combatCustodyState: session.CurrentCombatCustodyState,
                combatSuccessor: successorEvent,
                ammunitionLedger: session.CurrentAmmunitionLedger);
            var bytes = successorEvent.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(
                successorEvent.CampaignId, successorEvent.PriorStateVersion,
                successorEvent.StateVersion, custody.PositionId,
                CampaignActionAudience.System, Hash(bytes));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, bytes, projected.SerializeCanonical(session.Context)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult CompleteRepeatedCombatSuccessor(
        ExerciseSession session)
    {
        var runtime = session.CurrentRepeatAssault!;
        var custody = runtime.Custody!;
        if (session.CurrentContacts.AuthorityVersion != custody.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var successor = CampaignNativeCombatSuccessorV26.Create(
                runtime.Selection, runtime.Result!, runtime.Settlement!,
                custody, runtime.CustodyWorld!, runtime.CustodyState,
                session.Context);
            var projected = CampaignNativeCombatSuccessorV26.Project(
                runtime.Selection, runtime.Result!, runtime.Settlement!,
                custody, runtime.CustodyWorld!, runtime.CustodyState,
                session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatSuccessor(successor);
            var next = session.WithRepeatAssaultEvent(successor, contacts);
            var bytes = successor.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(successor.CampaignId,
                successor.PriorStateVersion, successor.StateVersion,
                successor.NextPositionId, CampaignActionAudience.System,
                Hash(bytes));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt,
                bytes, projected.SerializeCanonical(session.Context)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static CampaignNativeReserveReleaseStateV27 QueryNativeReserveReleaseState(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentStageEntrySecondAssault?.Release is { } stageEntrySecondSide)
            return stageEntrySecondSide;
        if (session.CurrentStageEntryAssault?.Release is { } stageEntrySide)
            return stageEntrySide;
        if (session.CurrentSecondAssault?.Release is { } secondSide)
            return secondSide;
        if (session.CurrentPostRepeat is { } later)
            return later.Release;
        if (session.CurrentRepeatAssault?.Release is { } repeated)
            return repeated;
        if (session.CurrentCombatDecision is { } initial)
        {
            if (session.CurrentNoAttackCompletion is { } noAttack)
                return session.CurrentReserveRelease
                    ?? CampaignNativeReserveReleaseStateV27
                        .InitializeAfterInitialNoAttack(initial, noAttack,
                            session.Context);
            if (session.CurrentCombatRbaCancellation is { } cancelledRba)
                return session.CurrentReserveRelease
                    ?? CampaignNativeReserveReleaseStateV27.InitializeAfterInitialRbaCancellation(
                        initial, cancelledRba, session.CurrentCombatRetreatWorld ?? initial.PriorSnapshot.World,
                        session.Context);
            if (session.CurrentCombatCancelledAssault is { } cancelled)
                return session.CurrentReserveRelease
                    ?? CampaignNativeReserveReleaseStateV27
                        .InitializeAfterInitialCancellation(initial, cancelled,
                            session.CurrentCombatRetreatWorld
                                ?? initial.PriorSnapshot.World, session.Context);
        }
        if (session.CurrentCombatSuccessor is not { } successor
            || session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatAssaultResult is not { } result
            || session.CurrentCombatSettlement is not { } settlement
            || session.CurrentCombatCustodyDisposition is not { } custody
            || session.CurrentCombatCustodyWorld is not { } world)
            throw new InvalidOperationException("Reserve Release requires accepted Combat successor.");
        var projected = CampaignNativeCombatSuccessorV26.Project(selected, result,
            settlement, custody, world, session.CurrentCombatCustodyState,
            session.Context);
        return session.CurrentReserveRelease
            ?? CampaignNativeReserveReleaseStateV27.Initialize(successor, projected,
                session.Context);
    }

    internal static ExerciseStepResult SubmitNativeReserveReleaseChoice(
        ExerciseSession session, CampaignActionAudience audience,
        string elementId, CampaignReserveReleaseChoice choice, string actionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var state = QueryNativeReserveReleaseState(session);
            var expectedAudience = state.ActingSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            if (audience != expectedAudience || state.Completed
                || state.StateVersion != session.CurrentContacts.AuthorityVersion)
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var (nextState, emitted) = state.Choose(session.Context, elementId, choice);
            var contacts = session.CurrentContacts.AdvanceNativeReserveRelease(emitted);
            var next = session.CurrentStageEntrySecondAssault is not null
                ? session.WithStageEntrySecondAssaultEvent(emitted, contacts)
                : session.CurrentStageEntryAssault is not null
                ? session.WithStageEntryAssaultEvent(emitted, contacts)
                : session.CurrentSecondAssault is not null
                ? session.WithSecondAssaultEvent(emitted, contacts)
                : session.CurrentPostRepeat is not null
                ? session.WithPostRepeatEvent(emitted, contacts)
                : session.CurrentRepeatAssault?.Release is not null
                    ? session.WithRepeatAssaultEvent(emitted, contacts)
                    : session.WithReserveReleaseEvent(emitted, contacts);
            var bytes = emitted.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(state.CampaignId,
                state.StateVersion, nextState.StateVersion,
                nextState.Position.PositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, bytes, nextState.SerializeCanonical()));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeReserveRelease(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var state = QueryNativeReserveReleaseState(session);
            if (state.StateVersion != session.CurrentContacts.AuthorityVersion)
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var (nextState, emitted) = state.Complete(session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeReserveRelease(emitted);
            var next = session.CurrentStageEntrySecondAssault is not null
                ? session.WithStageEntrySecondAssaultEvent(emitted, contacts)
                : session.CurrentStageEntryAssault is not null
                ? session.WithStageEntryAssaultEvent(emitted, contacts)
                : session.CurrentSecondAssault is not null
                ? session.WithSecondAssaultEvent(emitted, contacts)
                : session.CurrentPostRepeat is not null
                ? session.WithPostRepeatEvent(emitted, contacts)
                : session.CurrentRepeatAssault?.Release is not null
                    ? session.WithRepeatAssaultEvent(emitted, contacts)
                    : session.WithReserveReleaseEvent(emitted, contacts);
            var bytes = emitted.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(state.CampaignId,
                state.StateVersion, nextState.StateVersion,
                nextState.Position.PositionId, CampaignActionAudience.System,
                Hash(bytes));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, bytes, nextState.SerializeCanonical()));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static CampaignNativeReserveReleaseStateV27 QueryPendingNativeCycleControl(ExerciseSession session)
    {
        var release = QueryNativeReserveReleaseState(session);
        if ((session.CurrentStageEntrySecondAssault is { } stageEntrySecondControl
                ? stageEntrySecondControl.Control
                : session.CurrentStageEntryAssault is { } stageEntryControl
                ? stageEntryControl.Control
                : session.CurrentSecondAssault is { } secondSideControl
                ? secondSideControl.Control
                : session.CurrentRepeatAssault?.Release is not null
                ? session.CurrentRepeatAssault.Control
                : session.CurrentPostRepeat is not null
                    ? session.CurrentPostRepeat.Control
                    : session.CurrentCycleControl) is not null
            || !release.Completed || release.StateVersion != session.CurrentContacts.AuthorityVersion)
            throw new InvalidOperationException("No pending cycle-control decision is available.");
        return release;
    }

    internal static CampaignNativeReserveReleaseStateV27 QueryPendingNativeReserveRelease(ExerciseSession session)
    {
        var release = QueryNativeReserveReleaseState(session);
        if (release.Completed || release.Pending(session.Context).Count == 0
            || release.StateVersion != session.CurrentContacts.AuthorityVersion)
            throw new InvalidOperationException("No pending Reserve Release decision is available.");
        return release;
    }

    internal static ExerciseStepResult TimeoutNativeReserveRelease(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var release = QueryPendingNativeReserveRelease(session);
            if ((configuration is not null
                    && (configuration.RulesetHash != session.CurrentSnapshot.RulesetHash
                        || configuration.ContentHash != session.Context.ArtifactV6!.Identity.Hash))
                || (configuration is null && reason != "reserve-release-unavailable")
                || (reason != "reserve-release-unavailable" && (configuration is null
                    || checked(deadlineUnixMilliseconds - openedAtUnixMilliseconds)
                        != configuration.Budget("reserve-release"))))
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var (nextState, control) = release.Timeout(session.Context,
                configuration?.Hash ?? CampaignNativeReserveReleaseTimeoutV35.NoTimingConfigurationHash,
                openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds,
                cancelledAtUnixMilliseconds, reason);
            var contacts = session.CurrentContacts.AdvanceNativeReserveRelease(control);
            var next = session.CurrentStageEntrySecondAssault is not null
                ? session.WithStageEntrySecondAssaultEvent(control, contacts)
                : session.CurrentStageEntryAssault is not null
                ? session.WithStageEntryAssaultEvent(control, contacts)
                : session.CurrentSecondAssault is not null
                ? session.WithSecondAssaultEvent(control, contacts)
                : session.CurrentPostRepeat is not null
                ? session.WithPostRepeatEvent(control, contacts)
                : session.CurrentRepeatAssault?.Release is not null
                    ? session.WithRepeatAssaultEvent(control, contacts)
                    : session.WithReserveReleaseEvent(control, contacts);
            var bytes = control.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(release.CampaignId,
                release.StateVersion, control.StateVersion, control.NextPositionId,
                CampaignActionAudience.System, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, nextState.SerializeCanonical()));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult TimeoutNativeCycleControl(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var release = QueryPendingNativeCycleControl(session);
            if ((configuration is not null
                    && (configuration.RulesetHash != session.CurrentSnapshot.RulesetHash
                        || configuration.ContentHash != session.Context.ArtifactV6!.Identity.Hash))
                || (configuration is null && reason != "cycle-control-unavailable")
                || (reason != "cycle-control-unavailable" && (configuration is null
                    || checked(deadlineUnixMilliseconds - openedAtUnixMilliseconds)
                        != configuration.Budget("cycle-control"))))
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var control = CampaignNativeCycleControlTimeoutV34.Create(release,
                session.Context, session.CurrentContacts,
                configuration?.Hash ?? CampaignNativeCycleControlTimeoutV34.NoTimingConfigurationHash,
                openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds,
                cancelledAtUnixMilliseconds, reason);
            var contacts = session.CurrentContacts.AdvanceNativeCycleControl(control);
            var next = session.CurrentStageEntrySecondAssault is not null
                ? session.WithStageEntrySecondAssaultEvent(control, contacts)
                : session.CurrentStageEntryAssault is not null
                ? session.WithStageEntryAssaultEvent(control, contacts)
                : session.CurrentSecondAssault is not null
                ? session.WithSecondAssaultEvent(control, contacts)
                : session.CurrentPostRepeat is not null
                ? session.WithPostRepeatEvent(control, contacts)
                : session.CurrentRepeatAssault?.Release is not null
                    ? session.WithRepeatAssaultEvent(control, contacts)
                    : session.WithCycleControlEvent(control, contacts);
            var bytes = control.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(release.CampaignId,
                release.StateVersion, control.StateVersion, control.NextPositionId,
                CampaignActionAudience.System, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static (object Predecessor, CampaignWorldSnapshotV6 World) QuerySecondMovementSource(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentSecondMovementCompletion is not null)
            throw new InvalidOperationException("Second Movement has already completed.");
        if (session.CurrentSecondMove is { } movement)
            return (movement, session.CurrentSecondMoveWorld
                ?? throw new InvalidOperationException("Second Movement World is missing."));
        if (session.CurrentSecondReserve is { Completed: true } reserve)
            return (reserve, session.CurrentSecondReserveWorld
                ?? throw new InvalidOperationException("Second Reserve World is missing."));
        throw new InvalidOperationException("Second Movement requires completed Reserve Designation.");
    }

    internal static ExerciseStepResult CompleteSecondMovement(ExerciseSession session,
        CampaignActionAudience audience, string actionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Second Movement requires an owner."),
            };
            var (source, world) = QuerySecondMovementSource(session);
            var completed = CampaignNativeSecondMovementCompletedV45.Create(source, world, session.Context,
                session.CurrentSnapshot.OperationStageOrders, side, session.CurrentSnapshot.Setup.InitialGameTurn,
                session.CurrentContacts.AuthorityVersion);
            var contacts = session.CurrentContacts.AdvanceSecondMovementCompletion(completed);
            var next = session.WithSecondMovementCompletion(completed, contacts);
            var bytes = completed.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(completed.CampaignId,
                completed.PriorStateVersion, completed.StateVersion, completed.PositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    // A second-movement route currently admits only non-vehicle moves and
    // cannot proceed past an open reaction. Verify retained move history rather
    // than infer an empty breakdown obligation from a missing ledger entry.
    internal static ExerciseStepResult CompleteSecondBreakdown(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var transition = CampaignNativeSecondBreakdownV46.Create(session);
            var contacts = session.CurrentContacts.AdvanceSecondBreakdown(transition);
            var next = session.WithSecondBreakdown(transition, contacts);
            var bytes = transition.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(transition.CampaignId, transition.PriorStateVersion,
                transition.StateVersion, transition.NextPositionId, CampaignActionAudience.System,
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    internal static CampaignContactOwnerMovementCut QuerySecondMovementCut(ExerciseSession session,
        LandSide side, string elementId)
    {
        var (source, world) = QuerySecondMovementSource(session);
        var (_, _, position) = CampaignNativeSecondMoveV44.Source(source, world, session.Context,
            session.CurrentSnapshot.OperationStageOrders, side, session.CurrentSnapshot.Setup.InitialGameTurn,
            session.CurrentContacts.AuthorityVersion);
        return CampaignContactOwnerMovementCut.ProjectSecondMovement(world, position, session.Context,
            session.CurrentContacts, side, elementId);
    }

    internal static ExerciseStepResult SubmitSecondMovement(ExerciseSession session,
        CampaignActionAudience audience, string actionId, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Second Movement requires an owner."),
            };
            var (source, world) = QuerySecondMovementSource(session);
            var (movement, result) = CampaignNativeSecondMoveV44.Create(source, world, session.Context,
                session.CurrentSnapshot.OperationStageOrders, session.CurrentContacts, side,
                session.CurrentSnapshot.Setup.InitialGameTurn, elementId, actionId, session.CurrentSnapshot.RulesetHash);
            var contacts = session.CurrentContacts.AppendNativeSecondMove(world, result, movement, session.Context);
            var next = session.WithSecondMove(movement, result, contacts);
            var bytes = movement.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(movement.CampaignId,
                movement.PriorStateVersion, movement.StateVersion, movement.PositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static (object Predecessor, CampaignWorldSnapshotV6 World) QuerySecondReserveSource(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentSecondReserve is { } reserve)
            return (reserve, session.CurrentSecondReserveWorld
                ?? throw new InvalidOperationException("Second Reserve World is missing."));
        if (session.CurrentPostCombatPass is { } phase)
            return (phase, QueryNativeReserveReleaseState(session).World);
        throw new InvalidOperationException("Second Reserve requires completed first-side phases.");
    }

    internal static ExerciseStepResult SubmitSecondReserve(ExerciseSession session,
        CampaignActionAudience audience, string actionId, string? elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Second Reserve requires an owner."),
            };
            var (predecessor, world) = QuerySecondReserveSource(session);
            var (reserve, result) = CampaignNativeSecondReserveV43.Create(predecessor, world,
                session.Context, session.CurrentSnapshot.OperationStageOrders, side, elementId,
                session.CurrentSnapshot.Setup.InitialGameTurn, session.CurrentContacts.AuthorityVersion);
            var contacts = session.CurrentContacts.AdvanceSecondReserve(reserve);
            var next = session.WithSecondReserve(reserve, result, contacts);
            var bytes = reserve.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(reserve.CampaignId,
                reserve.PriorStateVersion, reserve.StateVersion, reserve.NextPositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    // MTS-TASK-003 (2026-11-23): the six-event Stage-entry preamble chain's
    // own opening decision -- the only one of the six that is a genuine
    // player choice, not an automatic/no-decision step. Confirmed against
    // this project's own existing classification of the Stage 1 original
    // (CampaignAudienceOutcomeSource.cs: InitiativeOrderDeclared's
    // "act-first"/"act-last" require Human(actor, e.DeclaringHolder),
    // while WeatherDetermined/NoObligationOrganizationResolved/
    // NoObligationNavalConvoyScheduleResolved all require System(actor)) --
    // the five mechanics after this one are automatic and do not get their
    // own owner-opening; wiring them is this task's own follow-up, tracked
    // separately, not guessed at here.
    internal static CampaignNativePostCombatPassV42 QueryStageEntryInitiativeDeclarationSource(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentInitiativeDeclaration is not null)
            throw new InvalidOperationException("Initiative Declaration has already advanced its retained cut.");
        if (session.CurrentSecondAssault is not { PostCombatPasses.Count: > 0 } repeatAssault)
            throw new InvalidOperationException("Initiative Declaration requires the prior stage's completed preamble.");
        return repeatAssault.PostCombatPasses[^1];
    }

    // `actionId` here follows every other Submit method's own convention: the real
    // sha256-hash core action id (used only for the acceptance receipt's audit trail),
    // never a semantic label -- confirmed against SubmitStageEntryReserve's identical
    // shape, whose own `elementId` parameter carries the semantic choice separately.
    // The actual decision (act-first/act-last) is `actFirst`, its own explicit parameter.
    internal static ExerciseStepResult SubmitStageEntryInitiativeDeclaration(ExerciseSession session,
        CampaignActionAudience audience, string actionId, bool actFirst)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Initiative Declaration requires an owner."),
            };
            var holder = session.CurrentSnapshot.InitiativeHolder
                ?? throw new InvalidOperationException("Initiative Declaration requires a resolved holder.");
            if (side != holder)
                throw new InvalidOperationException("Only the initiative holder may declare.");
            var predecessor = QueryStageEntryInitiativeDeclarationSource(session);
            var declared = CampaignNativeInitiativeDeclarationV49.Create(predecessor, holder, actFirst,
                session.CurrentSnapshot.Setup.InitialGameTurn, session.CurrentContacts.AuthorityVersion);
            var contacts = session.CurrentContacts.AdvanceInitiativeDeclaration(declared);
            var next = session.WithInitiativeDeclaration(declared, contacts);
            var bytes = declared.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(declared.CampaignId,
                declared.PriorStateVersion, declared.StateVersion, declared.NextPositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    // MTS-TASK-005 (2026-11-22): the new stage's own first-acting-side Reserve
    // Designation -- same shape as QuerySecondReserveSource/SubmitSecondReserve
    // above, reusing CampaignNativeSecondReserveV43 unchanged (role-as-data
    // generic as of #518), but entered from the six-event preamble chain's
    // terminal event instead of the first side's Post Combat Pass. The
    // `orders` list must additionally carry this stage's own resolved order
    // (CampaignNativeNoObligationFleetRepairV54.ToOrder()), per that event's
    // own remarks -- it is not yet a member of CurrentSnapshot.OperationStageOrders,
    // which only ever holds the orders CampaignSnapshotV11 itself was built
    // with (Stage 1's).
    internal static (object Predecessor, CampaignWorldSnapshotV6 World) QueryStageEntryReserveSource(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentStageEntryReserve is { } reserve)
            return (reserve, session.CurrentStageEntryReserveWorld
                ?? throw new InvalidOperationException("Stage Entry Reserve World is missing."));
        if (session.CurrentNoObligationFleetRepair is { } fleetRepair)
            return (fleetRepair, session.CurrentSecondAssault?.Release?.World
                ?? throw new InvalidOperationException("Stage Entry Reserve requires the prior stage's settled World."));
        throw new InvalidOperationException("Stage Entry Reserve requires a completed prior-stage preamble.");
    }

    internal static IReadOnlyList<CampaignOperationStageOrder> StageEntryOrders(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.CurrentNoObligationFleetRepair is { } fleetRepair
            ? [.. session.CurrentSnapshot.OperationStageOrders, fleetRepair.ToOrder()]
            : session.CurrentSnapshot.OperationStageOrders;
    }

    internal static ExerciseStepResult SubmitStageEntryReserve(ExerciseSession session,
        CampaignActionAudience audience, string actionId, string? elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Stage Entry Reserve requires an owner."),
            };
            var (predecessor, world) = QueryStageEntryReserveSource(session);
            var (reserve, result) = CampaignNativeSecondReserveV43.Create(predecessor, world,
                session.Context, StageEntryOrders(session), side, elementId,
                session.CurrentSnapshot.Setup.InitialGameTurn, session.CurrentContacts.AuthorityVersion);
            var contacts = session.CurrentContacts.AdvanceSecondReserve(reserve);
            var next = session.WithStageEntryReserve(reserve, result, contacts);
            var bytes = reserve.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(reserve.CampaignId,
                reserve.PriorStateVersion, reserve.StateVersion, reserve.NextPositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    // MTS-TASK-006 (2026-11-23): the new stage's own SECOND acting side's
    // Reserve Designation -- same shape as QueryStageEntryReserveSource/
    // SubmitStageEntryReserve above, reusing CampaignNativeSecondReserveV43
    // unchanged, but entered from this stage's own first acting side's
    // completed chain (CurrentStageEntryAssault's terminal PostCombatPasses
    // entry) instead of the preamble's terminal event. The carried-forward
    // World is this stage's own first acting side's settled Release world
    // (CurrentStageEntryAssault.Release.World), the direct analogue of how
    // QueryStageEntryReserveSource itself carries forward
    // CurrentSecondAssault.Release.World (Stage 1's second side's settled
    // world) when entering Stage 2 -- same "carry forward whichever chain
    // just finished" pattern, one stage-internal side-transition later.
    // StageEntryOrders(session) is reused unchanged: the stage's own
    // CampaignOperationStageOrder (FirstSide/SecondSide pairing) does not
    // change between this stage's two acting sides, only which side is
    // currently acting -- resolved inside the native class via #518's
    // ActingRole, not by varying the orders list itself.
    internal static (object Predecessor, CampaignWorldSnapshotV6 World) QueryStageEntrySecondReserveSource(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentStageEntrySecondReserve is { } reserve)
            return (reserve, session.CurrentStageEntrySecondReserveWorld
                ?? throw new InvalidOperationException("Stage Entry second-side Reserve World is missing."));
        if (session.CurrentStageEntryAssault is { PostCombatPasses: [.., var lastPass] })
            return (lastPass, session.CurrentStageEntryAssault.Release?.World
                ?? throw new InvalidOperationException("Stage Entry second-side Reserve requires this stage's settled World."));
        throw new InvalidOperationException("Stage Entry second-side Reserve requires this stage's completed first-acting-side chain.");
    }

    internal static ExerciseStepResult SubmitStageEntrySecondReserve(ExerciseSession session,
        CampaignActionAudience audience, string actionId, string? elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Stage Entry second-side Reserve requires an owner."),
            };
            var (predecessor, world) = QueryStageEntrySecondReserveSource(session);
            var (reserve, result) = CampaignNativeSecondReserveV43.Create(predecessor, world,
                session.Context, StageEntryOrders(session), side, elementId,
                session.CurrentSnapshot.Setup.InitialGameTurn, session.CurrentContacts.AuthorityVersion);
            var contacts = session.CurrentContacts.AdvanceSecondReserve(reserve);
            var next = session.WithStageEntrySecondReserve(reserve, result, contacts);
            var bytes = reserve.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(reserve.CampaignId,
                reserve.PriorStateVersion, reserve.StateVersion, reserve.NextPositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    // MTS-TASK-006 (2026-11-23): this stage's own SECOND acting side's
    // Movement -- same shape as QueryStageEntryMovementSource/CompleteStageEntryMovement/
    // QueryStageEntryMovementCut/SubmitStageEntryMovement above, reusing
    // CampaignNativeSecondMoveV44/CampaignNativeSecondMovementCompletedV45
    // unchanged (confirmed by reading CampaignNativeSecondMoveV44.Source
    // directly: its predecessor switch matches by type alone), entered from
    // this stage's own completed second-side Reserve Designation.
    internal static (object Predecessor, CampaignWorldSnapshotV6 World) QueryStageEntrySecondMovementSource(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentStageEntrySecondMovementCompletion is not null)
            throw new InvalidOperationException("Stage Entry second-side Movement has already completed.");
        if (session.CurrentStageEntrySecondMove is { } movement)
            return (movement, session.CurrentStageEntrySecondMoveWorld
                ?? throw new InvalidOperationException("Stage Entry second-side Movement World is missing."));
        if (session.CurrentStageEntrySecondReserve is { Completed: true } reserve)
            return (reserve, session.CurrentStageEntrySecondReserveWorld
                ?? throw new InvalidOperationException("Stage Entry second-side Reserve World is missing."));
        throw new InvalidOperationException("Stage Entry second-side Movement requires completed second-side Reserve Designation.");
    }

    internal static ExerciseStepResult CompleteStageEntrySecondMovement(ExerciseSession session,
        CampaignActionAudience audience, string actionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Stage Entry second-side Movement requires an owner."),
            };
            var (source, world) = QueryStageEntrySecondMovementSource(session);
            var completed = CampaignNativeSecondMovementCompletedV45.Create(source, world, session.Context,
                StageEntryOrders(session), side, session.CurrentSnapshot.Setup.InitialGameTurn,
                session.CurrentContacts.AuthorityVersion);
            var contacts = session.CurrentContacts.AdvanceSecondMovementCompletion(completed);
            var next = session.WithStageEntrySecondMovementCompletion(completed, contacts);
            var bytes = completed.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(completed.CampaignId,
                completed.PriorStateVersion, completed.StateVersion, completed.PositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    internal static CampaignContactOwnerMovementCut QueryStageEntrySecondMovementCut(ExerciseSession session,
        LandSide side, string elementId)
    {
        var (source, world) = QueryStageEntrySecondMovementSource(session);
        var (_, _, position) = CampaignNativeSecondMoveV44.Source(source, world, session.Context,
            StageEntryOrders(session), side, session.CurrentSnapshot.Setup.InitialGameTurn,
            session.CurrentContacts.AuthorityVersion);
        return CampaignContactOwnerMovementCut.ProjectSecondMovement(world, position, session.Context,
            session.CurrentContacts, side, elementId);
    }

    internal static ExerciseStepResult SubmitStageEntrySecondMovement(ExerciseSession session,
        CampaignActionAudience audience, string actionId, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Stage Entry second-side Movement requires an owner."),
            };
            var (source, world) = QueryStageEntrySecondMovementSource(session);
            var (movement, result) = CampaignNativeSecondMoveV44.Create(source, world, session.Context,
                StageEntryOrders(session), session.CurrentContacts, side,
                session.CurrentSnapshot.Setup.InitialGameTurn, elementId, actionId, session.CurrentSnapshot.RulesetHash);
            var contacts = session.CurrentContacts.AppendNativeSecondMove(world, result, movement, session.Context);
            var next = session.WithStageEntrySecondMove(movement, result, contacts);
            var bytes = movement.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(movement.CampaignId,
                movement.PriorStateVersion, movement.StateVersion, movement.PositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    // MTS-TASK-006 (2026-11-23): this stage's own SECOND acting side's
    // Breakdown, entered from this stage's own completed second-side
    // Movement Completion. Same System-audience, no-owner-decision shape as
    // CompleteStageEntryBreakdown above (Breakdown is a verify-and-replay
    // transition, not a submitted player action), via
    // CampaignNativeSecondBreakdownV46.CreateForStageEntrySecondSide -- see
    // that method's remarks for why a third entry point was needed instead
    // of reusing Create/CreateForStageEntry directly.
    internal static ExerciseStepResult CompleteStageEntrySecondBreakdown(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var transition = CampaignNativeSecondBreakdownV46.CreateForStageEntrySecondSide(session);
            var contacts = session.CurrentContacts.AdvanceSecondBreakdown(transition);
            var next = session.WithStageEntrySecondBreakdown(transition, contacts);
            var bytes = transition.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(transition.CampaignId, transition.PriorStateVersion,
                transition.StateVersion, transition.NextPositionId, CampaignActionAudience.System,
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    // MTS-TASK-005/006 (2026-11-22): the new stage's own first-acting-side
    // Movement -- same shape as QuerySecondMovementSource/CompleteSecondMovement/
    // QuerySecondMovementCut/SubmitSecondMovement above, reusing
    // CampaignNativeSecondMoveV44/CampaignNativeSecondMovementCompletedV45
    // unchanged (role-as-data generic as of #518; their predecessor switches
    // match by type, not by occurrence), entered from this stage's own
    // completed Reserve Designation, and carrying StageEntryOrders(session)
    // instead of CurrentSnapshot.OperationStageOrders directly, for the same
    // reason SubmitStageEntryReserve does.
    internal static (object Predecessor, CampaignWorldSnapshotV6 World) QueryStageEntryMovementSource(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentStageEntryMovementCompletion is not null)
            throw new InvalidOperationException("Stage Entry Movement has already completed.");
        if (session.CurrentStageEntryMove is { } movement)
            return (movement, session.CurrentStageEntryMoveWorld
                ?? throw new InvalidOperationException("Stage Entry Movement World is missing."));
        if (session.CurrentStageEntryReserve is { Completed: true } reserve)
            return (reserve, session.CurrentStageEntryReserveWorld
                ?? throw new InvalidOperationException("Stage Entry Reserve World is missing."));
        throw new InvalidOperationException("Stage Entry Movement requires completed Stage Entry Reserve Designation.");
    }

    internal static ExerciseStepResult CompleteStageEntryMovement(ExerciseSession session,
        CampaignActionAudience audience, string actionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Stage Entry Movement requires an owner."),
            };
            var (source, world) = QueryStageEntryMovementSource(session);
            var completed = CampaignNativeSecondMovementCompletedV45.Create(source, world, session.Context,
                StageEntryOrders(session), side, session.CurrentSnapshot.Setup.InitialGameTurn,
                session.CurrentContacts.AuthorityVersion);
            var contacts = session.CurrentContacts.AdvanceSecondMovementCompletion(completed);
            var next = session.WithStageEntryMovementCompletion(completed, contacts);
            var bytes = completed.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(completed.CampaignId,
                completed.PriorStateVersion, completed.StateVersion, completed.PositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    internal static CampaignContactOwnerMovementCut QueryStageEntryMovementCut(ExerciseSession session,
        LandSide side, string elementId)
    {
        var (source, world) = QueryStageEntryMovementSource(session);
        var (_, _, position) = CampaignNativeSecondMoveV44.Source(source, world, session.Context,
            StageEntryOrders(session), side, session.CurrentSnapshot.Setup.InitialGameTurn,
            session.CurrentContacts.AuthorityVersion);
        return CampaignContactOwnerMovementCut.ProjectSecondMovement(world, position, session.Context,
            session.CurrentContacts, side, elementId);
    }

    internal static ExerciseStepResult SubmitStageEntryMovement(ExerciseSession session,
        CampaignActionAudience audience, string actionId, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Stage Entry Movement requires an owner."),
            };
            var (source, world) = QueryStageEntryMovementSource(session);
            var (movement, result) = CampaignNativeSecondMoveV44.Create(source, world, session.Context,
                StageEntryOrders(session), session.CurrentContacts, side,
                session.CurrentSnapshot.Setup.InitialGameTurn, elementId, actionId, session.CurrentSnapshot.RulesetHash);
            var contacts = session.CurrentContacts.AppendNativeSecondMove(world, result, movement, session.Context);
            var next = session.WithStageEntryMove(movement, result, contacts);
            var bytes = movement.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(movement.CampaignId,
                movement.PriorStateVersion, movement.StateVersion, movement.PositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    // MTS-TASK-005/006 (2026-11-23): this stage's own Breakdown, entered
    // from this stage's own completed Movement Completion. Same
    // System-audience, no-owner-decision shape as CompleteSecondBreakdown
    // above (Breakdown is a verify-and-replay transition, not a submitted
    // player action), via CampaignNativeSecondBreakdownV46.CreateForStageEntry
    // -- see that method's remarks for why a new entry point was needed
    // instead of reusing Create directly.
    internal static ExerciseStepResult CompleteStageEntryBreakdown(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var transition = CampaignNativeSecondBreakdownV46.CreateForStageEntry(session);
            var contacts = session.CurrentContacts.AdvanceSecondBreakdown(transition);
            var next = session.WithStageEntryBreakdown(transition, contacts);
            var bytes = transition.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(transition.CampaignId, transition.PriorStateVersion,
                transition.StateVersion, transition.NextPositionId, CampaignActionAudience.System,
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    internal static object QueryPostCombatPassSource(ExerciseSession session) =>
        session.CurrentStageEntrySecondAssault is { TruckConvoyPass: { } stageEntrySecondConvoy } stageEntrySecondSide
            ? stageEntrySecondSide.PostCombatPasses.Count > 0 ? stageEntrySecondSide.PostCombatPasses[^1] : stageEntrySecondConvoy
            : session.CurrentStageEntryAssault is { TruckConvoyPass: { } stageEntryConvoy } stageEntrySide
            ? stageEntrySide.PostCombatPasses.Count > 0 ? stageEntrySide.PostCombatPasses[^1] : stageEntryConvoy
            : session.CurrentSecondAssault is { TruckConvoyPass: { } secondConvoy } secondSide
            ? secondSide.PostCombatPasses.Count > 0 ? secondSide.PostCombatPasses[^1] : secondConvoy
            : (object?)session.CurrentPostCombatPass ?? session.CurrentTruckConvoyPass
        ?? throw new InvalidOperationException("Post-combat pass requires the completed convoy phase.");

    internal static ExerciseStepResult SubmitPostCombatPass(ExerciseSession session,
        CampaignActionAudience audience, string actionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Post-combat pass requires an owner."),
            };
            var passed = CampaignNativePostCombatPassV42.Create(QueryPostCombatPassSource(session), side,
                session.CurrentSnapshot.Setup.InitialGameTurn, session.CurrentContacts.AuthorityVersion);
            var contacts = session.CurrentContacts.AdvancePostCombatPass(passed);
            var next = session.CurrentStageEntrySecondAssault is not null
                ? session.WithStageEntrySecondAssaultEvent(passed, contacts)
                : session.CurrentStageEntryAssault is not null
                ? session.WithStageEntryAssaultEvent(passed, contacts)
                : session.CurrentSecondAssault is not null
                ? session.WithSecondAssaultEvent(passed, contacts)
                : session.WithPostCombatPass(passed, contacts);
            var bytes = passed.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(passed.CampaignId,
                passed.PriorStateVersion, passed.StateVersion, passed.NextPositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static (CampaignNativeReserveReleaseStateV27 Release, CampaignNativeCycleControlV28 Control)
        QueryTruckConvoyPassSource(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentStageEntrySecondAssault is { } stageEntrySecondSide)
        {
            if (stageEntrySecondSide.TruckConvoyPass is not null)
                throw new InvalidOperationException("Truck Convoy has already been passed.");
            if (stageEntrySecondSide is not
                {
                    Release: { } stageEntrySecondRelease,
                    Control.Choice: CampaignNativeCycleControlChoiceV28.Finish
                })
                throw new InvalidOperationException("Truck Convoy requires a finished cycle.");
            return (stageEntrySecondRelease, stageEntrySecondSide.Control);
        }
        if (session.CurrentStageEntryAssault is { } stageEntrySide)
        {
            if (stageEntrySide.TruckConvoyPass is not null)
                throw new InvalidOperationException("Truck Convoy has already been passed.");
            if (stageEntrySide is not
                {
                    Release: { } stageEntryRelease,
                    Control.Choice: CampaignNativeCycleControlChoiceV28.Finish
                })
                throw new InvalidOperationException("Truck Convoy requires a finished cycle.");
            return (stageEntryRelease, stageEntrySide.Control);
        }
        if (session.CurrentSecondAssault is { } secondSide)
        {
            if (secondSide.TruckConvoyPass is not null)
                throw new InvalidOperationException("Truck Convoy has already been passed.");
            if (secondSide is not
                {
                    Release: { } secondRelease,
                    Control.Choice: CampaignNativeCycleControlChoiceV28.Finish
                })
                throw new InvalidOperationException("Truck Convoy requires a finished cycle.");
            return (secondRelease, secondSide.Control);
        }
        if (session.CurrentTruckConvoyPass is not null)
            throw new InvalidOperationException("Truck Convoy has already been passed.");
        var release = QueryNativeReserveReleaseState(session);
        var control = session.CurrentStageEntryAssault is { } stageEntrySideControl
            ? stageEntrySideControl.Control
            : session.CurrentSecondAssault is { } secondSideControl
            ? secondSideControl.Control
            : session.CurrentRepeatAssault?.Release is not null
            ? session.CurrentRepeatAssault.Control
            : session.CurrentPostRepeat is not null ? session.CurrentPostRepeat.Control : session.CurrentCycleControl;
        if (control is null || control.Choice != CampaignNativeCycleControlChoiceV28.Finish)
            throw new InvalidOperationException("Truck Convoy requires a finished cycle.");
        return (release, control);
    }

    internal static ExerciseStepResult SubmitTruckConvoyPass(ExerciseSession session,
        CampaignActionAudience audience, string actionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var (release, control) = QueryTruckConvoyPassSource(session);
            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Truck Convoy pass requires an owner."),
            };
            var passed = CampaignNativeTruckConvoyPassV41.Create(release, control, side,
                session.CurrentContacts.AuthorityVersion);
            var contacts = session.CurrentContacts.AdvanceTruckConvoyPass(passed);
            var next = session.CurrentStageEntrySecondAssault is not null
                ? session.WithStageEntrySecondAssaultEvent(passed, contacts)
                : session.CurrentStageEntryAssault is not null
                ? session.WithStageEntryAssaultEvent(passed, contacts)
                : session.CurrentSecondAssault is not null
                ? session.WithSecondAssaultEvent(passed, contacts)
                : session.WithTruckConvoyPass(passed, contacts);
            var bytes = passed.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(passed.CampaignId,
                passed.PriorStateVersion, passed.StateVersion, passed.NextPositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult SubmitNativeCycleControl(
        ExerciseSession session, CampaignActionAudience audience,
        CampaignNativeCycleControlChoiceV28 choice, string actionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var release = QueryNativeReserveReleaseState(session);
            var expectedAudience = release.ActingSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            if ((session.CurrentStageEntrySecondAssault is { } stageEntrySecondSideControl
                    ? stageEntrySecondSideControl.Control
                    : session.CurrentStageEntryAssault is { } stageEntrySideControl
                    ? stageEntrySideControl.Control
                    : session.CurrentSecondAssault is { } secondSideControl
                    ? secondSideControl.Control
                    : session.CurrentRepeatAssault?.Release is not null
                    ? session.CurrentRepeatAssault.Control
                    : session.CurrentPostRepeat is not null
                        ? session.CurrentPostRepeat.Control
                        : session.CurrentCycleControl) is not null
                || !release.Completed
                || audience != expectedAudience
                || release.StateVersion != session.CurrentContacts.AuthorityVersion)
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.InvalidAuthority);
            CampaignNativeCycleControlV28 control;
            try
            {
                control = CampaignNativeCycleControlV28.Create(release,
                    session.Context, session.CurrentContacts, choice);
            }
            catch (CampaignRuleNotImplementedException)
            {
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.RuleNotImplemented);
            }
            var contacts = session.CurrentContacts.AdvanceNativeCycleControl(control);
            var next = session.CurrentStageEntrySecondAssault is not null
                ? session.WithStageEntrySecondAssaultEvent(control, contacts)
                : session.CurrentStageEntryAssault is not null
                ? session.WithStageEntryAssaultEvent(control, contacts)
                : session.CurrentSecondAssault is not null
                ? session.WithSecondAssaultEvent(control, contacts)
                : session.CurrentPostRepeat is not null
                ? session.WithPostRepeatEvent(control, contacts)
                : session.CurrentRepeatAssault?.Release is not null
                    ? session.WithRepeatAssaultEvent(control, contacts)
                    : session.WithCycleControlEvent(control, contacts);
            var bytes = control.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(release.CampaignId,
                release.StateVersion, control.StateVersion,
                control.NextPositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, bytes, bytes));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult SubmitNativeRepeatMovement(
        ExerciseSession session, CampaignActionAudience audience,
        string actionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var release = QueryNativeReserveReleaseState(session);
            var control = session.CurrentPostRepeat is not null
                ? session.CurrentPostRepeat.Control
                : session.CurrentRepeatAssault?.Control ?? session.CurrentCycleControl;
            var expectedAudience = release.ActingSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            if (control is null || control.Choice
                    != CampaignNativeCycleControlChoiceV28.Repeat
                || (session.CurrentPostRepeat?.Movement is not null
                    || session.CurrentPostRepeat is null
                        && session.CurrentRepeatAssault?.Control is null
                        && session.CurrentRepeatMove is not null)
                || audience != expectedAudience
                || control.StateVersion != session.CurrentContacts.AuthorityVersion)
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var movement = CampaignNativeRepeatMovementV29.Create(release,
                control, session.Context, session.CurrentContacts, actionId);
            var world = movement.ProjectWorld(release, control,
                session.Context, session.CurrentContacts);
            var contacts = session.CurrentContacts.AdvanceNativeRepeatMove(
                release.World, world, movement, session.Context);
            var next = session.CurrentPostRepeat is not null
                || session.CurrentRepeatAssault?.Control is not null
                ? session.WithPostRepeatEvent(movement, contacts)
                : session.WithRepeatMovementEvent(movement, world, contacts);
            var bytes = movement.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(release.CampaignId,
                control.StateVersion, movement.StateVersion,
                movement.PositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, bytes, bytes));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeRepeatBreakdown(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var release = QueryNativeReserveReleaseState(session);
            var control = session.CurrentPostRepeat is null ? session.CurrentCycleControl : session.CurrentPostRepeat.Control;
            var movement = session.CurrentPostRepeat is null ? session.CurrentRepeatMove : session.CurrentPostRepeat.Movement;
            var world = session.CurrentPostRepeat is null ? session.CurrentRepeatMoveWorld : session.CurrentPostRepeat.MovedWorld;
            if (control is null || movement is null || world is null
                || (session.CurrentPostRepeat is null ? session.CurrentRepeatBreakdown : session.CurrentPostRepeat.Breakdown) is not null
                || session.CurrentContacts.AuthorityVersion != movement.StateVersion)
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var breakdown = CampaignNativeRepeatBreakdownV30.Create(
                release, control, movement, world, session.Context,
                session.CurrentContacts);
            var contacts = session.CurrentContacts
                .AdvanceNativeRepeatBreakdown(breakdown);
            var next = session.CurrentPostRepeat is null
                ? session.WithRepeatBreakdownEvent(breakdown, contacts)
                : session.WithPostRepeatEvent(breakdown, contacts);
            var bytes = breakdown.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(release.CampaignId,
                movement.StateVersion, breakdown.StateVersion,
                breakdown.NextPositionId, CampaignActionAudience.System, Hash(bytes));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, bytes, bytes));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static CampaignRepeatCombatOwnerCutV31 QueryNativeRepeatCombatCut(
        ExerciseSession session, LandSide side, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var release = QueryNativeReserveReleaseState(session);
        if ((session.CurrentPostRepeat is null ? session.CurrentRepeatCombatSelection : session.CurrentPostRepeat.Selection) is not null
            || side != release.ActingSide
            || (session.CurrentPostRepeat is null ? session.CurrentCycleControl : session.CurrentPostRepeat.Control) is not { } control
            || (session.CurrentPostRepeat is null ? session.CurrentRepeatMove : session.CurrentPostRepeat.Movement) is not { } movement
            || (session.CurrentPostRepeat is null ? session.CurrentRepeatBreakdown : session.CurrentPostRepeat.Breakdown) is not { } breakdown
            || (session.CurrentPostRepeat is null ? session.CurrentRepeatMoveWorld : session.CurrentPostRepeat.MovedWorld) is not { } world)
            throw new InvalidOperationException(
                "Repeated Combat needs an accepted acting-side Breakdown cut.");
        return CampaignRepeatCombatOwnerCutV31.Project(release, control,
            movement, breakdown, world, session.Context,
            session.CurrentContacts, actingElementId);
    }

    internal static ExerciseStepResult SubmitNativeRepeatCombat(
        ExerciseSession session, CampaignActionAudience audience,
        string actingElementId, string actionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            var release = QueryNativeReserveReleaseState(session);
            var expectedAudience = release.ActingSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            if (audience != expectedAudience
                || (session.CurrentPostRepeat is null ? session.CurrentRepeatCombatSelection : session.CurrentPostRepeat.Selection) is not null
                || (session.CurrentPostRepeat is null ? session.CurrentCycleControl : session.CurrentPostRepeat.Control) is not { } control
                || (session.CurrentPostRepeat is null ? session.CurrentRepeatMove : session.CurrentPostRepeat.Movement) is not { } movement
                || (session.CurrentPostRepeat is null ? session.CurrentRepeatBreakdown : session.CurrentPostRepeat.Breakdown) is not { } breakdown
                || (session.CurrentPostRepeat is null ? session.CurrentRepeatMoveWorld : session.CurrentPostRepeat.MovedWorld) is not { } world
                || session.CurrentContacts.AuthorityVersion != breakdown.StateVersion)
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var selection = CampaignNativeRepeatCombatDecisionV31.Select(
                release, control, movement, breakdown, world,
                session.Context, session.CurrentContacts,
                actingElementId, actionId);
            var contacts = session.CurrentContacts
                .AdvanceNativeRepeatCombatSelection(selection);
            var next = session.CurrentPostRepeat is null
                ? session.WithRepeatCombatSelectionEvent(selection, contacts)
                : session.WithPostRepeatEvent(selection, contacts);
            var bytes = selection.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(release.CampaignId,
                breakdown.StateVersion, selection.StateVersion,
                selection.NextPositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, bytes, bytes));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeRepeatCombatNoAttack(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if ((session.CurrentPostRepeat is null ? session.CurrentRepeatCombatSelection : session.CurrentPostRepeat.Selection) is not { } selected
            || selected.Choice != "finish-combat-without-attack"
            || session.CurrentPostRepeat is null
                && session.CurrentRepeatNoAttack is not null
            || (session.CurrentPostRepeat is null ? session.CurrentRepeatMoveWorld : session.CurrentPostRepeat.MovedWorld) is not { } world
            || session.CurrentCombatAssaultResult is not { } priorResult
            || session.CurrentContacts.AuthorityVersion != selected.StateVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var frame = CampaignCombatSelectionFrame.FromRepeat(
                session.CurrentSnapshot, selected, world,
                priorResult.AfterRandomState);
            var completion = CampaignNativeRepeatCombatNoAttackV32.Create(
                selected, frame, session.Context);
            var contacts = session.CurrentContacts
                .AdvanceNativeRepeatCombatNoAttack(completion);
            var next = session.CurrentPostRepeat is null
                ? session.WithRepeatNoAttackEvent(completion, contacts)
                : session.WithPostRepeatEvent(completion, contacts);
            var bytes = completion.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(
                completion.CampaignId, completion.PriorStateVersion,
                completion.StateVersion, completion.NextPositionId,
                CampaignActionAudience.System, Hash(bytes));
            return ExerciseStepResult.Accepted(next,
                new ExerciseStepEvidence(receipt, bytes,
                    completion.SerializeStateCanonical(selected.SerializeCanonical())));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeRepeatCombatBarrage(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        // A second-side runtime is bound to its own V47 selection, not V31.
        if (session.CurrentRepeatAssault is not { Barrage: null } runtime
            || (!runtime.IsSecondSide && (session.CurrentPostRepeat?.Selection
                ?? session.CurrentRepeatCombatSelection) is not
                { Choice: "select-contact-assault" })
            || session.CurrentContacts.AuthorityVersion != runtime.Selection.StateVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var barrage = CampaignNativeCombatBarrageV14.Create(
                runtime.Selection, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatBarrage(barrage);
            var next = session.WithRepeatAssaultEvent(barrage, contacts);
            var bytes = barrage.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(
                barrage.CampaignId, barrage.PriorStateVersion,
                barrage.StateVersion, barrage.NextPositionId,
                CampaignActionAudience.System, Hash(bytes));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, bytes, barrage.SerializeStateCanonical(runtime.Selection)));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static byte[] SerializeNativeCombatResultState(ExerciseSession session)
    {
        var state = SerializeRetreatDeclineState(session);
        foreach (var seal in session.CurrentCombatForceSeals)
            state = seal.SerializeStateCanonical(state);
        state = session.CurrentCombatForceCompletion!.SerializeStateCanonical(state);
        state = session.CurrentCombatAntiArmor!.SerializeStateCanonical(state);
        return session.CurrentCombatAssaultResult!.SerializeStateCanonical(state);
    }

    internal static ExerciseStepResult SubmitContactDefenderRbaMove(
        ExerciseSession session, CampaignActionSubmission submission, string defenderElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (string.IsNullOrWhiteSpace(defenderElementId)
            || !CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (session.CurrentRepeatAssault is
            {
                Barrage: not null,
                Decline: null,
                RbaCancellation: null
            })
            return SubmitRepeatedDefenderRbaMove(session, submission,
                defenderElementId);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatBarrage is not { } barrage
            || session.CurrentCombatRetreatDecline is not null
            || session.CurrentContacts.AuthorityVersion
                != (session.CurrentCombatRetreatMove?.StateVersion ?? barrage.StateVersion))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.CampaignId != selected.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion
            != (session.CurrentCombatRetreatMove?.StateVersion ?? barrage.StateVersion))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != barrage.NextPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        try
        {
            var cut = QueryContactRbaCut(session);
            var audience = cut.DefenderSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            if (submission.Audience != audience || defenderElementId != cut.DefenderElementId
                || !cut.MoveOffers.Any(value => value.ActionId == submission.ActionId))
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var previous = session.CurrentCombatRetreatMove;
            var initialLocationId = session.CurrentCombatRetreatMoves.Count == 0
                ? cut.InitialLocationId : session.CurrentCombatRetreatMoves[0].OriginLocationId;
            var movement = previous is null
                ? CampaignNativeCombatRetreatMoveV16.CreateFirst(selected, barrage,
                    session.Context, session.CurrentContacts, submission.ActionId)
                : CampaignNativeCombatRetreatMoveV16.CreateNext(selected, barrage,
                    previous, session.CurrentCombatRetreatWorld!, initialLocationId,
                    session.Context, session.CurrentContacts, submission.ActionId);
            var world = previous is null
                ? movement.ProjectWorld(selected, barrage, session.Context,
                    session.CurrentContacts)
                : movement.ProjectWorldAfter(selected, barrage, previous,
                    session.CurrentCombatRetreatWorld!, initialLocationId,
                    session.Context, session.CurrentContacts);
            var beforeWorld = session.CurrentCombatRetreatWorld ?? session.CurrentSnapshot.World;
            var contacts = session.CurrentContacts.AdvanceNativeCombatRetreatMove(
                beforeWorld, world, movement,
                selected.Decision.BoundRepresentationId!, session.Context);
            var checkpoint = previous is null
                ? movement.SerializeStateCanonical(selected, barrage,
                    session.Context, session.CurrentContacts)
                : movement.SerializeNextStateCanonical(
                    SerializeRetreatMoveState(session), world);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(movement), contacts, selected,
                combatBarrage: barrage, combatRetreatMove: movement, combatRetreatWorld: world,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts
                    ?? session.CurrentContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves.Append(movement),
                ammunitionLedger: session.CurrentAmmunitionLedger);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                movement.PriorStateVersion, movement.StateVersion, movement.PositionId,
                submission.Audience, submission.ActionId);
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                movement.SerializeCanonical(), checkpoint));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult SubmitRepeatedDefenderRbaMove(
        ExerciseSession session, CampaignActionSubmission submission,
        string defenderElementId)
    {
        var runtime = session.CurrentRepeatAssault!;
        var selected = runtime.Selection;
        var barrage = runtime.Barrage!;
        var previous = runtime.RetreatMoves.Count == 0
            ? null : runtime.RetreatMoves[^1];
        if (submission.CampaignId != selected.CampaignId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion
            != (previous?.StateVersion ?? barrage.StateVersion))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != barrage.NextPositionId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        try
        {
            var cut = QueryContactRbaCut(session);
            var audience = cut.DefenderSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            if (submission.Audience != audience
                || defenderElementId != cut.DefenderElementId
                || !cut.MoveOffers.Any(value => value.ActionId == submission.ActionId))
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var movement = previous is null
                ? CampaignNativeCombatRetreatMoveV16.CreateFirst(selected,
                    barrage, session.Context, session.CurrentContacts,
                    submission.ActionId)
                : CampaignNativeCombatRetreatMoveV16.CreateNext(selected,
                    barrage, previous, runtime.RetreatWorld!,
                    runtime.RetreatMoves[0].OriginLocationId, session.Context,
                    session.CurrentContacts, submission.ActionId);
            var world = previous is null
                ? movement.ProjectWorld(selected, barrage, session.Context,
                    session.CurrentContacts)
                : movement.ProjectWorldAfter(selected, barrage, previous,
                    runtime.RetreatWorld!, runtime.RetreatMoves[0].OriginLocationId,
                    session.Context, session.CurrentContacts);
            var beforeWorld = runtime.RetreatWorld ?? selected.World;
            var contacts = session.CurrentContacts.AdvanceNativeCombatRetreatMove(
                beforeWorld, world, movement,
                selected.Decision.BoundRepresentationId!, session.Context);
            var priorState = previous is null ? null
                : runtime.SerializeRetreatMoveState();
            var checkpoint = previous is null
                ? movement.SerializeStateCanonical(selected, barrage,
                    session.Context, session.CurrentContacts)
                : movement.SerializeNextStateCanonical(priorState!, world);
            var next = session.WithRepeatAssaultEvent(movement, contacts);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                movement.PriorStateVersion, movement.StateVersion,
                movement.PositionId, submission.Audience, submission.ActionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(
                receipt, movement.SerializeCanonical(), checkpoint));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException or InvalidCampaignHistoryException
            or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult Complete(ExerciseSession session,
        CampaignCurrentActionExecutionResult execution)
    {
        if (!execution.IsAccepted)
            return ExerciseStepResult.Rejected(execution.RejectionReason);

        var history = session.CurrentHistory.Add(execution.AcceptedEvent!);
        var contacts = execution.AcceptedEvent switch
        {
            ElementMovedV3 movement => session.CurrentContacts.AppendMovement(
                session.CurrentSnapshot, execution.SuccessorSnapshot!, movement, session.Context),
            ReactingElementMovedV2 movement => session.CurrentContacts.AppendMovement(
                session.CurrentSnapshot, execution.SuccessorSnapshot!, movement, session.Context),
            _ => session.CurrentContacts.AdvanceNonMovement(
                session.CurrentSnapshot, execution.SuccessorSnapshot!),
        };
        // Perf seeding path (Phase 1 of #436): every ordinary step is a
        // clean boundary, so where Combat ammunition is even meaningful
        // for this content (not every scenario pins a Combat source) this
        // hands the next session a correct, up-to-date ledger -- the hot
        // Combat call sites below trust it instead of rescanning the full
        // accepted-event history themselves. The fallback deliberately
        // re-derives from genesis (not CreateInitial) rather than assuming
        // a null ledger means "no Combat yet": a second or later repeat
        // cycle within a round (see ExerciseSession.WithPostRepeatEvent)
        // leaves CurrentAmmunitionLedger null on purpose once it can no
        // longer prove the carried value is current, and this is where
        // that self-heals -- at the cost of one full rescan, not a wrong
        // ammunition count carried forward indefinitely. Content with no
        // pinned Combat source (CreateInitial's own precondition) never
        // reaches the hot Combat call sites either, so staying null there
        // is correct, not just a missed optimization.
        var ammunitionLedger = session.CurrentAmmunitionLedger
            ?? (session.Context.ArtifactV6 is not null && session.Context.CombatSource is not null
                ? CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(session.Context, session.CurrentHistory)
                : null);
        var successor = new ExerciseSession(execution.SuccessorSnapshot!, session.Context, history, contacts,
            ammunitionLedger: ammunitionLedger);
        var evidence = new ExerciseStepEvidence(
            execution.Receipt!,
            CampaignCurrentEventSerializer.Serialize(execution.AcceptedEvent!),
            CampaignCurrentSnapshotSerializer.Serialize(execution.SuccessorSnapshot!, session.Context));
        return ExerciseStepResult.Accepted(successor, evidence);
    }

    private static byte[] SerializeRetreatDeclineState(ExerciseSession session)
    {
        var decline = session.CurrentCombatRetreatDecline
            ?? throw new InvalidOperationException("Force Assignment lacks RBA completion.");
        return session.CurrentCombatRetreatMove is not null
            ? decline.SerializeAfterMoveStateCanonical(SerializeRetreatMoveState(session))
            : decline.SerializeStateCanonical(session.CurrentCombatDecision!,
                session.CurrentCombatBarrage!);
    }

    private static byte[] SerializeRetreatMoveState(ExerciseSession session)
    {
        var selected = session.CurrentCombatDecision
            ?? throw new InvalidOperationException("Retreat move lacks selected assault.");
        var barrage = session.CurrentCombatBarrage
            ?? throw new InvalidOperationException("Retreat move lacks Barrage.");
        var contacts = session.CurrentCombatRetreatPriorContacts
            ?? throw new InvalidOperationException("Retreat move lacks its first contact cut.");
        var world = selected.PriorSnapshot.World;
        CampaignNativeCombatRetreatMoveV16? previous = null;
        byte[] state = [];
        foreach (var move in session.CurrentCombatRetreatMoves)
        {
            var nextWorld = previous is null
                ? move.ProjectWorld(selected, barrage, session.Context, contacts)
                : move.ProjectWorldAfter(selected, barrage, previous, world,
                    session.CurrentCombatRetreatMoves[0].OriginLocationId,
                    session.Context, contacts);
            state = previous is null
                ? move.SerializeStateCanonical(selected, barrage, session.Context, contacts)
                : move.SerializeNextStateCanonical(state, nextWorld);
            contacts = contacts.AdvanceNativeCombatRetreatMove(world, nextWorld, move,
                selected.Decision.BoundRepresentationId!, session.Context);
            world = nextWorld;
            previous = move;
        }
        if (previous is null)
            throw new InvalidOperationException("Retreat movement state is empty.");
        return state;
    }

    public static ExerciseReconstructionResult Reconstruct(ExerciseSession completedSession) =>
        Reconstruct(completedSession, includeStreamHash: true);

    private static ExerciseReconstructionResult Reconstruct(ExerciseSession completedSession, bool includeStreamHash)
    {
        ArgumentNullException.ThrowIfNull(completedSession);
        if (completedSession.Pregame is not null)
            return ReconstructPregame(completedSession, includeStreamHash);
        var chain = new List<ExerciseSession>();
        var seen = new HashSet<ExerciseSession>(ReferenceEqualityComparer.Instance);
        var cursor = completedSession;
        while (true)
        {
            if (!seen.Add(cursor))
                return InvalidNativeHistory(completedSession);
            // Overhaul doc (docs/work/plans/2026-11-23-codebase-overhaul-direction.md),
            // Section 0.2/0.6 step 1: this was an 18-arm (now 20, after
            // MTS-TASK-005/006) hand-maintained switch pattern-matching the
            // same Current*/*Predecessor pairs QueryCheckpoint/ReconstructOne
            // dispatch off, in the same hand-maintained order. Collapses to
            // a single property read: cursor.Pending is already computed in
            // the correct order and already carries Predecessor, so there is
            // no separate cascade left to keep in sync here.
            var pending = cursor.Pending;
            if (pending is null) break;
            if (pending.Predecessor is null) return InvalidNativeHistory(completedSession);
            chain.Add(cursor);
            cursor = pending.Predecessor;
        }
        if (chain.Count == 0) return ReconstructOne(cursor, includeStreamHash, predecessorVerified: false);
        if (!ReconstructOne(cursor, includeStreamHash: false, predecessorVerified: false).IsVerified)
            return InvalidNativeHistory(completedSession);
        for (var index = chain.Count - 1; index >= 0; index--)
        {
            var result = ReconstructOne(chain[index], includeStreamHash && index == 0, predecessorVerified: true);
            if (!result.IsVerified) return InvalidNativeHistory(completedSession);
            if (index == 0) return result;
        }
        throw new InvalidOperationException("A retained native history must have a head.");
    }

    private static ExerciseReconstructionResult ReconstructPregame(ExerciseSession session, bool includeStreamHash)
    {
        var state = session.Pregame!;
        var expected = session.SourceTurn?.SerializeCheckpoint() ?? session.SourceAuthority?.SerializeCheckpoint() ?? state.SerializeCheckpoint();
        var expectedHash = Hash(expected);
        try
        {
            var records = session.PregameHistory;
            if (session.CurrentContacts.CampaignId != state.Request.CampaignId
                || session.CurrentContacts.AuthorityVersion != (session.SourceTurn?.StateVersion ?? session.SourceAuthority?.StateVersion ?? state.StateVersion)
                || (session.SourceTurn is null && session.CurrentContacts.Records.Count != 0)
                || records.Count != (session.SourceTurn?.StateVersion ?? session.SourceAuthority?.StateVersion ?? state.StateVersion))
                return new(ExerciseReconstructionFailureReason.InvalidHistory, null, null, expectedHash, null);
            var initial = state.Source.Session.InitialWorld.Initialization;
            var genesis = CampaignPregameSourceState.Create(state.Request, state.Source.Session.Content,
                new(initial.CapabilityPointsExpended, initial.CohesionLevel));
            var replayed = CampaignPregameSourceState.ReplayGenesis(state.Request, records[0], genesis.SerializeCheckpoint());
            CampaignSourceAuthorityState? authority = null;
            CampaignSourceTurnAuthority? sourceTurn = null;
            var replayedContacts = CampaignContactJournal.InitializePregameSource(state.Request.CampaignId);
            foreach (var record in records.Skip(1))
            {
                using var document = JsonDocument.Parse(record);
                var eventType = document.RootElement.GetProperty("eventType").GetString();
                if (sourceTurn is null && eventType != "source-turn-started")
                    replayedContacts = replayedContacts.AdvancePregameSource(replayedContacts.AuthorityVersion);
                else if (sourceTurn is not null && eventType is not ("source-polish-brigade-arrived" or "source-fourth-indian-opening-moved"))
                    replayedContacts = replayedContacts.AdvanceSourceTurnNonMovement(state.Request.CampaignId,
                        sourceTurn.StateVersion, checked(sourceTurn.StateVersion + 1));
                if (authority is not null && sourceTurn is null && authority.FinalizedOwners != 0
                    && eventType is not ("source-owner-setup-finalization-changed" or "source-turn-started" or "source-seventh-shell-stack-corrected"))
                {
                    var actor = document.RootElement.GetProperty("actor").GetString();
                    if (actor == "system" || actor == "axis" && authority.IsOwnerFinalized(CampaignActionAudience.Axis)
                        || actor == "commonwealth" && authority.IsOwnerFinalized(CampaignActionAudience.Commonwealth))
                        throw new InvalidOperationException("Finalized setup cannot accept this source event.");
                }
                if (eventType == "source-horse-setup-decision")
                {
                    if (authority is null || sourceTurn is not null) throw new InvalidOperationException("Invalid Horse setup decision.");
                    var owner = document.RootElement.GetProperty("actor").GetString() switch
                    {
                        "axis" => CampaignActionAudience.Axis,
                        "commonwealth" => CampaignActionAudience.Commonwealth,
                        _ => throw new InvalidOperationException("Invalid Horse setup owner."),
                    };
                    var kind = document.RootElement.GetProperty("kind").GetString() ?? throw new InvalidOperationException("Horse decision missing.");
                    var decided = authority.SetHorseSetupDecision(owner, kind);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeHorseSetupEvent(decided, owner, kind)))
                        throw new InvalidOperationException("Horse setup decision differs from re-adjudication.");
                    authority = decided;
                    continue;
                }
                if (eventType == "source-owner-setup-finalization-changed")
                {
                    if (authority is null || sourceTurn is not null) throw new InvalidOperationException("Invalid owner finalization.");
                    var audience = document.RootElement.GetProperty("actor").GetString() switch
                    {
                        "axis" => CampaignActionAudience.Axis,
                        "commonwealth" => CampaignActionAudience.Commonwealth,
                        _ => throw new InvalidOperationException("Invalid setup owner."),
                    };
                    var finish = document.RootElement.GetProperty("finished").GetBoolean();
                    var finalized = authority.SetOwnerFinalization(audience, finish);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeSetupFinalizationEvent(finalized, audience, finish)))
                        throw new InvalidOperationException("Owner finalization differs from re-adjudication.");
                    authority = finalized;
                    continue;
                }
                if (eventType == "source-turn-started")
                {
                    if (authority is null || sourceTurn is not null) throw new InvalidOperationException("Invalid source turn start.");
                    var version = document.RootElement.GetProperty("contractVersion").GetInt32();
                    if (version is not (CampaignSourceTurnAuthority.ContractVersion or CampaignSourceTurnAuthority.MapBoundContractVersion or CampaignSourceTurnAuthority.CdSeamContractVersion))
                        throw new InvalidOperationException("Unsupported source turn contract.");
                    sourceTurn = CampaignSourceTurnAuthority.Start(authority, legacy: version == CampaignSourceTurnAuthority.ContractVersion,
                        cdSeam: version == CampaignSourceTurnAuthority.CdSeamContractVersion);
                    if (!record.AsSpan().SequenceEqual(sourceTurn.SerializeEvent(eventType, CampaignSourceTurnAuthority.StartActionId,
                            authority.StateVersion, authority.SerializeCheckpoint())))
                        throw new InvalidOperationException("Source turn start differs from re-adjudication.");
                    replayedContacts = replayedContacts.AppendSourceTurnStart(sourceTurn);
                    continue;
                }
                if (sourceTurn is not null)
                {
                    if (eventType == "source-gt1-op1-multi-lanes-announced")
                    {
                        var announced = sourceTurn.AnnounceGt1MultiLanes();
                        if (!record.AsSpan().SequenceEqual(announced.SerializeEvent(eventType, CampaignSourceTurnAuthority.AnnounceGt1MultiLanesActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), announcedLanes: announced.MultiArrival!.Lanes)))
                            throw new InvalidOperationException("GT1 multi-lane announcement differs from re-adjudication.");
                        sourceTurn = announced;
                        continue;
                    }
                    if (eventType == "source-gt1-op1-multi-lane-declined")
                    {
                        var lane = document.RootElement.GetProperty("lane").GetInt32();
                        var move = lane switch
                        {
                            2 => CampaignSourceTurnAction.DeclineGt1Lane2Attack(sourceTurn.StateVersion),
                            3 => CampaignSourceTurnAction.DeclineGt1Lane3Attack(sourceTurn.StateVersion),
                            6 => CampaignSourceTurnAction.DeclineGt1Lane6Attack(sourceTurn.StateVersion),
                            _ => throw new InvalidOperationException("Unsupported multi-lane choice.")
                        };
                        var delivered = sourceTurn.DeclineGt1MultiLaneAttack(lane);
                        if (!record.AsSpan().SequenceEqual(delivered.SerializeEvent(eventType, move.ActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: "commonwealth", lane: lane, stage: 1)))
                            throw new InvalidOperationException("GT1 multi-lane delivery differs from re-adjudication.");
                        sourceTurn = delivered;
                        continue;
                    }
                    if (eventType == "source-gt1-op1-lane3-announced")
                    {
                        var announced = sourceTurn.AnnounceGt1Lane3();
                        if (!record.AsSpan().SequenceEqual(announced.SerializeEvent(eventType, CampaignSourceTurnAuthority.AnnounceGt1Lane3ActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), lane: 3, stage: 1)))
                            throw new InvalidOperationException("GT1 lane 3 announcement differs from re-adjudication.");
                        sourceTurn = announced;
                        continue;
                    }
                    if (eventType == "source-gt1-op1-lane3-attack-declined")
                    {
                        var lane3Decline = CampaignSourceTurnAction.DeclineGt1Lane3Attack(sourceTurn.StateVersion);
                        var delivered = sourceTurn.DeclineGt1Lane3Attack();
                        if (!record.AsSpan().SequenceEqual(delivered.SerializeEvent(eventType, lane3Decline.ActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: "commonwealth", lane: 3, stage: 1)))
                            throw new InvalidOperationException("GT1 lane 3 delivery differs from re-adjudication.");
                        sourceTurn = delivered;
                        continue;
                    }
                    if (eventType == "source-gt1-op1-lane2-announced")
                    {
                        var announced = sourceTurn.AnnounceGt1Lane2();
                        if (!record.AsSpan().SequenceEqual(announced.SerializeEvent(eventType, CampaignSourceTurnAuthority.AnnounceGt1Lane2ActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), lane: 2, stage: 1)))
                            throw new InvalidOperationException("GT1 lane 2 announcement differs from re-adjudication.");
                        sourceTurn = announced;
                        continue;
                    }
                    if (eventType == "source-gt1-op1-lane2-attack-declined")
                    {
                        var convoyDeclineAction = CampaignSourceTurnAction.DeclineGt1Lane2Attack(sourceTurn.StateVersion);
                        var delivered = sourceTurn.DeclineGt1Lane2Attack();
                        if (!record.AsSpan().SequenceEqual(delivered.SerializeEvent(eventType, convoyDeclineAction.ActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: "commonwealth", lane: 2, stage: 1)))
                            throw new InvalidOperationException("GT1 lane 2 delivery differs from re-adjudication.");
                        sourceTurn = delivered;
                        continue;
                    }
                    if (eventType == "source-fourth-indian-opening-moved")
                    {
                        var supplyId = document.RootElement.GetProperty("supplyId").GetString()
                            ?? throw new InvalidOperationException("Movement supply selection missing.");
                        var moveAction = CampaignSourceTurnAction.MoveFourthIndianOpening(sourceTurn.StateVersion, supplyId);
                        var moved = sourceTurn.MoveFourthIndianOpening(supplyId);
                        if (!record.AsSpan().SequenceEqual(moved.SerializeEvent(eventType, moveAction.ActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: "commonwealth", supplyId: supplyId)))
                            throw new InvalidOperationException("Fourth Indian opening move differs from re-adjudication.");
                        replayedContacts = replayedContacts.AppendSourceMovement(sourceTurn, moved, CampaignSourceFourthIndianMovement.GroupId);
                        sourceTurn = moved;
                        continue;
                    }
                    if (eventType == "source-australian-baseline-voted")
                    {
                        var actor = document.RootElement.GetProperty("actor").GetString();
                        var side = actor switch { "axis" => LandSide.Axis, "commonwealth" => LandSide.Commonwealth, _ => throw new InvalidOperationException("Invalid baseline voter.") };
                        var approve = document.RootElement.GetProperty("baselineApproved").GetBoolean();
                        var voteAction = approve ? CampaignSourceTurnAction.ApproveSixthAustralianBaseline(sourceTurn.StateVersion)
                            : CampaignSourceTurnAction.WithdrawSixthAustralianBaseline(sourceTurn.StateVersion);
                        var voted = sourceTurn.VoteSixthAustralianBaseline(side, approve);
                        if (!record.AsSpan().SequenceEqual(voted.SerializeEvent(eventType, voteAction.ActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: actor, baselineApproved: approve)))
                            throw new InvalidOperationException("Australian baseline vote differs from re-adjudication.");
                        sourceTurn = voted;
                        continue;
                    }
                    if (eventType == "source-sixth-australian-training-interrupted")
                    {
                        var trainingAction = CampaignSourceTurnAction.InterruptSixthAustralianTraining(sourceTurn.StateVersion);
                        var interrupted = sourceTurn.InterruptSixthAustralianTraining();
                        if (!record.AsSpan().SequenceEqual(interrupted.SerializeEvent(eventType, trainingAction.ActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: "commonwealth")))
                            throw new InvalidOperationException("Sixth Australian training choice differs from re-adjudication.");
                        sourceTurn = interrupted;
                        continue;
                    }
                    if (eventType == "source-gt1-first-breakdown-completed")
                    {
                        var completed = sourceTurn.CompleteGt1FirstBreakdown();
                        if (!record.AsSpan().SequenceEqual(completed.SerializeEvent(eventType,
                                CampaignSourceTurnAuthority.CompleteGt1FirstBreakdownActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint())))
                            throw new InvalidOperationException("First-player breakdown differs from re-adjudication.");
                        sourceTurn = completed;
                        continue;
                    }
                    if (eventType == "source-gt1-first-movement-finished")
                    {
                        var actor = document.RootElement.GetProperty("actor").GetString();
                        var side = actor switch
                        {
                            "axis" => LandSide.Axis,
                            "commonwealth" => LandSide.Commonwealth,
                            _ => throw new InvalidOperationException("Invalid movement owner.")
                        };
                        var finishActionId = CampaignSourceTurnAction.FinishFirstMovement(sourceTurn.StateVersion).ActionId;
                        var finished = sourceTurn.FinishGt1FirstMovement(side);
                        if (!record.AsSpan().SequenceEqual(finished.SerializeEvent(eventType, finishActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: actor)))
                            throw new InvalidOperationException("First-player movement completion differs from re-adjudication.");
                        sourceTurn = finished;
                        continue;
                    }
                    if (eventType == "source-gt1-first-reserves-declined")
                    {
                        var actor = document.RootElement.GetProperty("actor").GetString();
                        var side = actor switch
                        {
                            "axis" => LandSide.Axis,
                            "commonwealth" => LandSide.Commonwealth,
                            _ => throw new InvalidOperationException("Invalid reserve owner.")
                        };
                        var reserveActionId = CampaignSourceTurnAction.DeclineFirstReserves(sourceTurn.StateVersion).ActionId;
                        var declined = sourceTurn.DeclineGt1FirstReserves(side);
                        if (!record.AsSpan().SequenceEqual(declined.SerializeEvent(eventType, reserveActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: actor)))
                            throw new InvalidOperationException("First-player reserve choice differs from re-adjudication.");
                        sourceTurn = declined;
                        continue;
                    }
                    if (eventType == "source-gt1-stage1-fleet-completed")
                    {
                        var completed = sourceTurn.CompleteGt1StageOneFleet();
                        if (!record.AsSpan().SequenceEqual(completed.SerializeEvent(eventType,
                                CampaignSourceTurnAuthority.CompleteGt1FleetActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint())))
                            throw new InvalidOperationException("Stage-one fleet completion differs from re-adjudication.");
                        sourceTurn = completed;
                        continue;
                    }
                    if (eventType == "source-gt1-stage1-arrivals-completed")
                    {
                        var completed = sourceTurn.CompleteGt1StageOneArrivals();
                        if (!record.AsSpan().SequenceEqual(completed.SerializeEvent(eventType,
                                CampaignSourceTurnAuthority.CompleteGt1ArrivalsActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint())))
                            throw new InvalidOperationException("Stage-one arrivals completion differs from re-adjudication.");
                        sourceTurn = completed;
                        continue;
                    }
                    if (eventType == "source-gt1-organization-completed")
                    {
                        var completed = sourceTurn.CompleteGt1Organization();
                        if (!record.AsSpan().SequenceEqual(completed.SerializeEvent(eventType,
                                CampaignSourceTurnAuthority.CompleteGt1OrganizationActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint())))
                            throw new InvalidOperationException("Opening organization completion differs from re-adjudication.");
                        sourceTurn = completed;
                        continue;
                    }
                    if (eventType == "source-voluntary-organization-declined")
                    {
                        var actor = document.RootElement.GetProperty("actor").GetString();
                        var side = actor switch { "axis" => LandSide.Axis, "commonwealth" => LandSide.Commonwealth, _ => throw new InvalidOperationException("Invalid organization owner.") };
                        var organizationActionId = CampaignSourceTurnAction.DeclineVoluntaryOrganization(sourceTurn.StateVersion).ActionId;
                        var declined = sourceTurn.DeclineVoluntaryOrganization(side);
                        if (!record.AsSpan().SequenceEqual(declined.SerializeEvent(eventType, organizationActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: actor)))
                            throw new InvalidOperationException("Voluntary organization choice differs from re-adjudication.");
                        sourceTurn = declined;
                        continue;
                    }
                    if (eventType == "source-gt1-stage1-weather-determined")
                    {
                        var weather = sourceTurn.ResolveStageOneWeather();
                        if (!record.AsSpan().SequenceEqual(weather.SerializeEvent(eventType, CampaignSourceTurnAuthority.ResolveStageOneWeatherActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint())))
                            throw new InvalidOperationException("Stage-one weather differs from re-adjudication.");
                        sourceTurn = weather;
                        continue;
                    }
                    if (eventType == "source-stage1-order-declared")
                    {
                        var axisFirst = document.RootElement.GetProperty("axisFirst").GetBoolean();
                        var orderAction = axisFirst ? CampaignSourceTurnAction.ActFirst(sourceTurn.StateVersion) : CampaignSourceTurnAction.ActLast(sourceTurn.StateVersion);
                        var declared = sourceTurn.DeclareStageOneOrder(axisFirst);
                        if (!record.AsSpan().SequenceEqual(declared.SerializeEvent(eventType, orderAction.ActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: "axis", axisFirst: axisFirst)))
                            throw new InvalidOperationException("Stage-one initiative declaration differs from re-adjudication.");
                        sourceTurn = declared;
                        continue;
                    }
                    if (eventType == "source-tactical-shipping-declined")
                    {
                        var actor = document.RootElement.GetProperty("actor").GetString();
                        var side = actor switch { "axis" => LandSide.Axis, "commonwealth" => LandSide.Commonwealth, _ => throw new InvalidOperationException("Invalid tactical shipping owner.") };
                        var shippingActionId = CampaignSourceTurnAction.DeclineTacticalShipping(sourceTurn.StateVersion).ActionId;
                        var declined = sourceTurn.DeclineTacticalShipping(side);
                        if (!record.AsSpan().SequenceEqual(declined.SerializeEvent(eventType, shippingActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: actor)))
                            throw new InvalidOperationException("Tactical shipping choice differs from re-adjudication.");
                        sourceTurn = declined;
                        continue;
                    }
                    if (eventType == "source-gt1-convoy-schedule-completed")
                    {
                        var completed = sourceTurn.CompleteGt1Scheduling();
                        if (!record.AsSpan().SequenceEqual(completed.SerializeEvent(eventType, CampaignSourceTurnAuthority.CompleteGt1ScheduleActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint())))
                            throw new InvalidOperationException("GT1 convoy scheduling differs from re-adjudication.");
                        sourceTurn = completed;
                        continue;
                    }
                    if (eventType == "source-commonwealth-gt5-supply-rolled")
                    {
                        var cwRollActionId = CampaignSourceTurnAction.RollGt5Supply(sourceTurn.StateVersion).ActionId;
                        var cwRolled = sourceTurn.RollGt5Supply();
                        if (!record.AsSpan().SequenceEqual(cwRolled.SerializeEvent(eventType, cwRollActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: "commonwealth")))
                            throw new InvalidOperationException("GT5 supply roll differs from re-adjudication.");
                        sourceTurn = cwRolled;
                        continue;
                    }
                    if (eventType is "source-axis-gt3-supply-rolled" or "source-axis-gt3-supply-adjusted" or "source-axis-gt3-supply-finished")
                    {
                        var isAdjustment = eventType == "source-axis-gt3-supply-adjusted";
                        int? lane = isAdjustment ? document.RootElement.GetProperty("lane").GetInt32() : null;
                        int? stage = isAdjustment ? document.RootElement.GetProperty("operationStage").GetInt32() : null;
                        int? delta = isAdjustment ? document.RootElement.GetProperty("delta").GetInt32() : null;
                        var supplyAction = eventType switch
                        {
                            "source-axis-gt3-supply-rolled" => CampaignSourceTurnAction.RollGt3Supply(sourceTurn.StateVersion),
                            "source-axis-gt3-supply-finished" => CampaignSourceTurnAction.FinishGt3Supply(sourceTurn.StateVersion),
                            _ => CampaignSourceTurnAction.AdjustGt3Supply(sourceTurn.StateVersion, lane!.Value, stage!.Value, delta!.Value),
                        };
                        var supplyNext = eventType switch
                        {
                            "source-axis-gt3-supply-rolled" => sourceTurn.RollGt3Supply(),
                            "source-axis-gt3-supply-finished" => sourceTurn.FinishGt3Supply(),
                            _ => sourceTurn.AdjustGt3Supply(lane!.Value, stage!.Value, delta!.Value),
                        };
                        if (!record.AsSpan().SequenceEqual(supplyNext.SerializeEvent(eventType, supplyAction.ActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), actor: "axis", lane: lane, stage: stage, delta: delta)))
                            throw new InvalidOperationException("GT3 supply plan differs from re-adjudication.");
                        sourceTurn = supplyNext;
                        continue;
                    }
                    if (eventType == "source-polish-brigade-arrived")
                    {
                        var destination = document.RootElement.GetProperty("destination").GetString()
                            ?? throw new InvalidOperationException("Polish arrival destination missing.");
                        var arrivalActionId = sourceTurn.PolishActionId(destination);
                        var arrived = sourceTurn.ArrivePolish(destination);
                        if (!record.AsSpan().SequenceEqual(arrived.SerializeEvent(eventType, arrivalActionId,
                                sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint(), destination)))
                            throw new InvalidOperationException("Polish arrival differs from re-adjudication.");
                        replayedContacts = replayedContacts.AppendSourcePolishArrival(sourceTurn, arrived);
                        sourceTurn = arrived;
                        continue;
                    }
                    if (eventType != "source-initiative-determined") throw new InvalidOperationException("Unsupported source turn event.");
                    var successor = sourceTurn.ResolveInitiative();
                    if (!record.AsSpan().SequenceEqual(successor.SerializeEvent(eventType, CampaignSourceTurnAuthority.InitiativeActionId,
                            sourceTurn.StateVersion, sourceTurn.SerializeCheckpoint())))
                        throw new InvalidOperationException("Source initiative differs from re-adjudication.");
                    sourceTurn = successor;
                    continue;
                }
                if (eventType == "source-authority-started")
                {
                    if (authority is not null) throw new InvalidOperationException("Duplicate source authority transition.");
                    authority = CampaignSourceAuthorityState.Finish(replayed);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeEvent()))
                        throw new InvalidOperationException("Source authority event differs from re-adjudication.");
                    continue;
                }
                if (eventType == "source-aresca-admitted")
                {
                    if (authority is null || authority.IsOwnerFinalized(CampaignActionAudience.Axis)) throw new InvalidOperationException("Aresca setup owner is unavailable.");
                    var successor = authority.AdmitAresca();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAresca(successor))) throw new InvalidOperationException("Aresca setup differs from re-adjudication.");
                    authority = successor; continue;
                }
                if (eventType == "source-seventh-shell-stack-corrected")
                {
                    if (authority is null || sourceTurn is not null) throw new InvalidOperationException("Seventh Armored setup correction unavailable.");
                    var successor = authority.CorrectSeventhShell();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeSeventhShellCorrection(successor)))
                        throw new InvalidOperationException("Seventh Armored shell correction differs from re-adjudication.");
                    authority = successor; continue;
                }
                if (eventType == "source-ccnn-batch-admitted")
                {
                    if (authority is null || authority.IsOwnerFinalized(CampaignActionAudience.Axis))
                        throw new InvalidOperationException("CCNN setup owner is unavailable.");
                    var successor = authority.AdmitCcnnBatch();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCcnnBatch(successor)))
                        throw new InvalidOperationException("CCNN batch differs from re-adjudication.");
                    authority = successor; continue;
                }
                if (eventType == "source-sixth-australian-placed")
                {
                    if (authority is null || authority.IsOwnerFinalized(CampaignActionAudience.Commonwealth))
                        throw new InvalidOperationException("Australian setup owner is unavailable.");
                    var location = document.RootElement.GetProperty("locationId").GetString()!;
                    var successor = authority.PlaceSixthAustralian(location);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeSixthAustralianPlacement(successor, location)))
                        throw new InvalidOperationException("Australian placement differs from re-adjudication.");
                    authority = successor; continue;
                }
                if (eventType is "source-catanzaro-grouped-world-admitted" or "source-catanzaro-disposition-confirmed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var confirm = eventType == "source-catanzaro-disposition-confirmed";
                    var successor = confirm ? authority.ConfirmCatanzaro() : authority.AdmitCatanzaroGroupedWorld();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCatanzaroGroupedEvent(successor, confirm)))
                        throw new InvalidOperationException("Catanzaro grouped world differs from re-adjudication.");
                    authority = successor; continue;
                }
                if (eventType is "source-seventh-armored-grouped-world-admitted" or "source-seventh-armored-disposition-confirmed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var confirm = eventType == "source-seventh-armored-disposition-confirmed";
                    var successor = confirm ? authority.ConfirmSeventhArmored() : authority.AdmitSeventhArmoredGroupedWorld();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeSeventhArmoredGroupedEvent(successor, confirm)))
                        throw new InvalidOperationException("Seventh Armored grouped world differs from re-adjudication.");
                    authority = successor; continue;
                }
                if (eventType is "source-sixteenth-brigade-grouped-world-admitted" or "source-sixteenth-brigade-disposition-confirmed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var confirm = eventType == "source-sixteenth-brigade-disposition-confirmed";
                    var successor = confirm ? authority.ConfirmSixteenthBrigade() : authority.AdmitSixteenthBrigadeGroupedWorld();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeSixteenthBrigadeGroupedEvent(successor, confirm)))
                        throw new InvalidOperationException("Sixteenth Brigade grouped world differs from re-adjudication.");
                    authority = successor; continue;
                }
                if (eventType is "source-italian-divisions-grouped-world-admitted" or "source-italian-division-disposition-confirmed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var id = eventType == "source-italian-division-disposition-confirmed" ? document.RootElement.GetProperty("groupId").GetString()! : null;
                    var successor = id is null ? authority.AdmitItalianDivisionsGroupedWorld() : authority.ConfirmItalianDivision(id);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeItalianDivisionsGroupedEvent(successor, id)))
                        throw new InvalidOperationException("Italian grouped world differs from re-adjudication.");
                    authority = successor; continue;
                }
                if (eventType is "source-libyan-grouped-world-admitted" or "source-libyan-disposition-confirmed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var id = eventType == "source-libyan-disposition-confirmed" ? document.RootElement.GetProperty("groupId").GetString()! : null;
                    var successor = id is null ? authority.AdmitLibyanGroupedWorld() : authority.ConfirmLibyanGroup(id);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeLibyanGroupedEvent(successor, id)))
                        throw new InvalidOperationException("Libyan grouped world differs from re-adjudication.");
                    authority = successor; continue;
                }
                if (eventType == "source-maletti-grouped-world-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitMalettiGroupedWorld();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeMalettiGroupedEvent(successor)))
                        throw new InvalidOperationException("Maletti grouped world differs from re-adjudication.");
                    authority = successor; continue;
                }
                if (eventType == "source-matruh-grouped-world-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitMatruhGroupedWorld();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeMatruhGroupedEvent(successor)))
                        throw new InvalidOperationException("Matruh grouped world differs from re-adjudication.");
                    authority = successor; continue;
                }
                if (eventType is "source-fourth-indian-world-admitted" or "source-fourth-indian-disposition-confirmed" or "source-fourth-indian-motor-adjusted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var id = eventType == "source-fourth-indian-motor-adjusted" ? document.RootElement.GetProperty("elementId").GetString()! : null;
                    var delta = id is null ? 0 : document.RootElement.GetProperty("delta").GetInt32();
                    var confirm = eventType == "source-fourth-indian-disposition-confirmed";
                    var successor = confirm ? authority.ConfirmFourthIndianDisposition() : id is null ? authority.AdmitFourthIndianWorld() : authority.AdjustFourthIndianMotor(id, delta);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeFourthIndianEvent(successor, id, delta, confirm)))
                        throw new InvalidOperationException("Fourth Indian source event differs from re-adjudication.");
                    authority = successor; continue;
                }
                if (eventType == "source-axis-supply-dump-placed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var dump = document.RootElement.GetProperty("dumpId").GetString()!;
                    var location = document.RootElement.GetProperty("locationId").GetString()!;
                    var successor = authority.PlaceDump(dump, location);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeDumpEvent(successor, dump, location)))
                        throw new InvalidOperationException("Source setup event differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-commonwealth-supply-placed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var placement = document.RootElement.GetProperty("placementId").GetString()!;
                    var location = document.RootElement.GetProperty("locationId").GetString()!;
                    var successor = authority.PlaceCommonwealthSupply(placement, location);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCommonwealthSupplyEvent(successor, placement, location)))
                        throw new InvalidOperationException("Source setup event differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-dummy-group-decided")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var actor = document.RootElement.GetProperty("actor").GetString();
                    if (actor is not ("axis" or "commonwealth")) throw new InvalidOperationException("Invalid dummy owner.");
                    var axis = actor == "axis";
                    var dummy = document.RootElement.GetProperty("dummyId").GetString()!;
                    var location = document.RootElement.GetProperty("locationId").GetString();
                    var successor = authority.DecideDummy(axis, dummy, location);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeDummyEvent(successor, axis, dummy, location)))
                        throw new InvalidOperationException("Dummy setup event differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-motorization-adjusted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var actor = document.RootElement.GetProperty("actor").GetString();
                    if (actor is not ("axis" or "commonwealth")) throw new InvalidOperationException("Invalid motorization owner.");
                    var axis = actor == "axis";
                    var pool = document.RootElement.GetProperty("poolId").GetString()!;
                    var location = document.RootElement.GetProperty("locationId").GetString()!;
                    var delta = document.RootElement.GetProperty("delta").GetInt32();
                    var successor = authority.AdjustMotorization(axis, pool, location, delta);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeMotorizationEvent(successor, axis, pool, location, delta)))
                        throw new InvalidOperationException("Motorization event differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-gt1-supply-planned")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var choice = document.RootElement.GetProperty("choice").GetString()!;
                    var lane = document.RootElement.GetProperty("lane").GetInt32();
                    var stage = document.RootElement.GetProperty("operationStage").GetInt32();
                    var delta = document.RootElement.GetProperty("delta").GetInt32();
                    var successor = authority.ApplyOpeningConvoyChoice(choice, lane, stage, delta);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeOpeningConvoyEvent(successor, choice, lane, stage, delta)))
                        throw new InvalidOperationException("GT1 convoy event differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-commonwealth-supply-planned")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var choice = document.RootElement.GetProperty("choice").GetString()!;
                    var target = document.RootElement.GetProperty("targetGameTurn").GetInt32();
                    var successor = authority.ApplyCommonwealthSupplyChoice(choice, target);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCommonwealthSupplyPlanEvent(successor, choice, target)))
                        throw new InvalidOperationException("Commonwealth supply plan event differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-axis-september-supply-changed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var choice = document.RootElement.GetProperty("choice").GetString()!;
                    var target = document.RootElement.GetProperty("targetGameTurn").GetInt32();
                    var lane = document.RootElement.GetProperty("lane").GetInt32();
                    var stage = document.RootElement.GetProperty("operationStage").GetInt32();
                    var delta = document.RootElement.GetProperty("delta").GetInt32();
                    var successor = authority.ApplySeptemberSupplyChoice(choice, target, lane, stage, delta);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeSeptemberSupplyEvent(successor, choice, target, lane, stage, delta)))
                        throw new InvalidOperationException("September supply event differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-axis-force-supplement-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitAxisSupplement();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAxisSupplementEvent(successor)))
                        throw new InvalidOperationException("Source supplement event differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-autoblinda-choice")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var choice = document.RootElement.GetProperty("choice").GetString()!;
                    var target = document.RootElement.GetProperty("target").GetString()!;
                    var successor = authority.ApplyAutoblindaChoice(choice, target);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAutoblindaEvent(successor, choice, target)))
                        throw new InvalidOperationException("Autoblinda event differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-commonwealth-force-supplement-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitCommonwealthSupplement();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCommonwealthSupplementEvent(successor)))
                        throw new InvalidOperationException("Commonwealth source supplement differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-priority-motorization-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitPriorityMotorization();
                    if (!record.AsSpan().SequenceEqual(authority.SerializePriorityMotorizationAdmission(successor)))
                        throw new InvalidOperationException("Priority motorization admission differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-priority-motorization-adjusted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var elementId = document.RootElement.GetProperty("elementId").GetString()!;
                    var delta = document.RootElement.GetProperty("delta").GetInt32();
                    var successor = authority.AdjustPriorityMotorization(elementId, delta);
                    if (!record.AsSpan().SequenceEqual(authority.SerializePriorityMotorizationEvent(successor, elementId, delta)))
                        throw new InvalidOperationException("Priority motorization event differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-axis-priority-motorization-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitAxisPriorityMotorization();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAxisPriorityAdmission(successor)))
                        throw new InvalidOperationException("Axis priority admission differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-axis-priority-motorization-changed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var choice = document.RootElement.GetProperty("choice").GetString()!;
                    var elementId = document.RootElement.GetProperty("elementId").GetString()!;
                    var delta = document.RootElement.GetProperty("delta").GetInt32();
                    if (authority.AxisFreeArtillery is not null && !SourceAxisPriorityChoices(authority)
                        .Any(candidate => candidate.Kind == choice && candidate.ElementId == elementId && candidate.Delta == delta))
                        throw new InvalidOperationException("Legacy Axis priority choice after audit cutoff.");
                    var successor = authority.ApplyAxisPriorityChoice(choice, elementId, delta);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAxisPriorityEvent(successor, choice, elementId, delta)))
                        throw new InvalidOperationException("Axis priority event differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-axis-minor-garrisons-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitAxisGarrisons();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAxisGarrisonsAdmission(successor)))
                        throw new InvalidOperationException("Axis garrisons differ from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-battlefield-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitBattlefield();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeBattlefieldAdmission(successor)))
                        throw new InvalidOperationException("Source battlefield differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-cairo-setup-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitCairoSetup();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCairoSetupAdmission(successor)))
                        throw new InvalidOperationException("Cairo setup admission differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-cairo-unit-placed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var elementId = document.RootElement.GetProperty("elementId").GetString()!;
                    var location = document.RootElement.GetProperty("locationId").GetString()!;
                    var successor = authority.PlaceCairoUnit(elementId, location);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCairoPlacementEvent(successor, elementId, location)))
                        throw new InvalidOperationException("Cairo placement differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-cairo-world-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitCairoWorld();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCairoWorldAdmission(successor)))
                        throw new InvalidOperationException("Cairo active World differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-maletti-world-promoted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    if (authority.AxisFreeArtillery is not null) throw new InvalidOperationException("Legacy Maletti promotion after audit cutoff.");
                    var successor = authority.PromoteMaletti();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeMalettiPromotion(successor)))
                        throw new InvalidOperationException("Maletti active World differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-independent-world-promoted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    if (authority.AxisFreeArtillery is not null) throw new InvalidOperationException("Legacy independent promotion after audit cutoff.");
                    var successor = authority.PromoteIndependent();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeIndependentPromotion(successor)))
                        throw new InvalidOperationException("Independent source World differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-cw-frontline-activated")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.ActivateCwFrontline();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCwFrontlineEvent(successor)))
                        throw new InvalidOperationException("CW frontline differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-giarabub-motor-placement-policy-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitGiarabubMotorPlacementPolicy();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeGiarabubMotorPlacementPolicyEvent(successor)))
                        throw new InvalidOperationException("Giarabub motor placement policy differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-giarabub-motor-machine-gun-activated")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.ActivateGiarabubMotorMachineGun();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeGiarabubMotorMachineGunEvent(successor)))
                        throw new InvalidOperationException("Giarabub motor MG differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-giarabub-motor-mg-ruling-accepted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AcceptGiarabubMotorMgRuling();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeGiarabubMotorMgRulingEvent(successor)))
                        throw new InvalidOperationException("Giarabub motor MG ruling differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-axis-garrison-remainder-activated")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.ActivateAxisGarrisonRemainder();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAxisGarrisonRemainderEvent(successor)))
                        throw new InvalidOperationException("Axis garrison remainder differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-axis-minor-garrisons-activated")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.ActivateAxisMinorGarrisons();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAxisMinorGarrisonsEvent(successor)))
                        throw new InvalidOperationException("Axis minor garrisons differ from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType is "source-cw-free-guns-admitted" or "source-cw-free-gun-placed" or "source-cw-free-guns-world-promoted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var kind = eventType == "source-cw-free-gun-placed" ? "place" : eventType == "source-cw-free-guns-world-promoted" ? "promote" : null;
                    var id = kind == "place" ? document.RootElement.GetProperty("elementId").GetString()! : null;
                    var location = id is null ? null : document.RootElement.GetProperty("locationId").GetString()!;
                    var successor = kind switch { "place" => authority.PlaceCwFreeGun(id!, location!), "promote" => authority.PromoteCwFreeGunsWorld(), _ => authority.AdmitCwFreeGuns() };
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCwFreeGunsEvent(successor, kind, id, location))) throw new InvalidOperationException("CW free guns differ from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-unattached-cargo-changed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var actor = document.RootElement.GetProperty("actor").GetString();
                    if (actor is not ("axis" or "commonwealth")) throw new InvalidOperationException("Invalid cargo actor.");
                    var kind = document.RootElement.GetProperty("kind").GetString()!;
                    var location = document.RootElement.GetProperty("locationId").GetString();
                    var delta = document.RootElement.GetProperty("delta").GetInt32();
                    var successor = authority.ApplyUnattachedCargo(actor == "axis", kind, location, delta);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeUnattachedCargoEvent(successor, actor == "axis", kind, location, delta)))
                        throw new InvalidOperationException("Initial cargo differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-compact-checkpoint-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitCompactCheckpoint();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCompactCheckpointEvent(successor)))
                        throw new InvalidOperationException("Compact checkpoint differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-cw-priority-world-promoted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.PromoteCwPriorityWorld();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCwPriorityWorldEvent(successor)))
                        throw new InvalidOperationException("Commonwealth priority world differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType is "source-cw-priority-catalog-admitted" or "source-cw-priority-catalog-adjusted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var id = eventType == "source-cw-priority-catalog-adjusted" ? document.RootElement.GetProperty("elementId").GetString()! : null;
                    var delta = id is null ? 0 : document.RootElement.GetProperty("delta").GetInt32();
                    var successor = id is null ? authority.AdmitCwPriorityCatalog() : authority.AdjustCwPriorityCatalog(id, delta);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCwPriorityCatalogEvent(successor, id, delta)))
                        throw new InvalidOperationException("Commonwealth priority catalog differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType is "source-gt2-correction-admitted" or "source-gt2-correction-changed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var kind = eventType == "source-gt2-correction-changed" ? document.RootElement.GetProperty("kind").GetString()! : null;
                    var lane = kind is null ? 0 : document.RootElement.GetProperty("lane").GetInt32();
                    var stage = kind is null ? 0 : document.RootElement.GetProperty("stage").GetInt32();
                    var delta = kind is null ? 0 : document.RootElement.GetProperty("delta").GetInt32();
                    var successor = kind is null ? authority.AdmitGt2Correction() : authority.ApplyGt2Correction(kind, lane, stage, delta);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeGt2CorrectionEvent(successor, kind, lane, stage, delta)))
                        throw new InvalidOperationException("GT2 correction differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-matruh-detached-world-promoted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.PromoteMatruhDetachedWorld();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeMatruhDetachedWorldEvent(successor)))
                        throw new InvalidOperationException("Matruh detached world differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType is "source-matruh-formation-admitted" or "source-matruh-formation-changed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var kind = eventType == "source-matruh-formation-changed" ? document.RootElement.GetProperty("kind").GetString()! : null;
                    var id = kind is null ? null : document.RootElement.GetProperty("elementId").GetString();
                    var successor = kind is null ? authority.AdmitMatruhFormation() : authority.ApplyMatruhFormation(kind, id);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeMatruhFormationEvent(successor, kind, id)))
                        throw new InvalidOperationException("Matruh formation differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-certified-maletti-world-promoted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.PromoteCertifiedMalettiWorld();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeCertifiedMalettiWorldEvent(successor)))
                        throw new InvalidOperationException("Certified Maletti world differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType is "source-maletti-hierarchy-admitted" or "source-maletti-hierarchy-changed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var kind = eventType == "source-maletti-hierarchy-changed" ? document.RootElement.GetProperty("kind").GetString()! : null;
                    var id = kind is null ? null : document.RootElement.GetProperty("elementId").GetString();
                    var successor = kind is null ? authority.AdmitMalettiHierarchy() : authority.ApplyMalettiHierarchy(kind, id);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeMalettiHierarchyEvent(successor, kind, id)))
                        throw new InvalidOperationException("Maletti hierarchy differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-axis-corps-world-promoted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.PromoteAxisCorpsWorld();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAxisCorpsWorldEvent(successor)))
                        throw new InvalidOperationException("Axis corps world differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType is "source-axis-corps-setup-admitted" or "source-axis-corps-unit-placed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var id = eventType == "source-axis-corps-unit-placed" ? document.RootElement.GetProperty("elementId").GetString()! : null;
                    var location = id is null ? null : document.RootElement.GetProperty("locationId").GetString()!;
                    var successor = id is null ? authority.AdmitAxisCorpsSetup() : authority.PlaceAxisCorpsUnit(id, location!);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAxisCorpsSetupEvent(successor, id, location)))
                        throw new InvalidOperationException("Axis corps setup differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-axis-free-world-promoted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.PromoteAxisFreeWorld();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAxisFreeWorldEvent(successor)))
                        throw new InvalidOperationException("Axis source world differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType is "source-axis-free-setup-admitted" or "source-axis-free-unit-placed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var id = eventType == "source-axis-free-unit-placed" ? document.RootElement.GetProperty("elementId").GetString()! : null;
                    var location = id is null ? null : document.RootElement.GetProperty("locationId").GetString()!;
                    var successor = id is null ? authority.AdmitAxisFreeSetup() : authority.PlaceAxisFreeUnit(id, location!);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAxisFreeSetupEvent(successor, id, location)))
                        throw new InvalidOperationException("Axis free setup differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType is "source-initial-wrecks-admitted" or "source-initial-wreck-placed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var id = eventType == "source-initial-wreck-placed" ? document.RootElement.GetProperty("id").GetString()! : null;
                    var location = id is null ? null : document.RootElement.GetProperty("locationId").GetString()!;
                    var successor = id is null ? authority.AdmitInitialWrecks() : authority.PlaceInitialWreck(id, location!);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeInitialWreckEvent(successor, id, location)))
                        throw new InvalidOperationException("Initial wrecks differ from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType is "source-attached-initial-loads-admitted" or "source-attached-initial-loads-changed" or "source-frontline-initial-loads-changed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var kind = eventType != "source-attached-initial-loads-admitted" ? document.RootElement.GetProperty("kind").GetString()! : null;
                    var element = kind is null ? null : document.RootElement.GetProperty("elementId").GetString()!;
                    var successor = kind is null ? authority.AdmitAttachedInitialLoads() : authority.ApplyAttachedInitialLoads(kind, element!);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeAttachedInitialLoadsEvent(successor, kind, element)))
                        throw new InvalidOperationException("Initial loads differ from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-expanded-independent-world-promoted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    if (authority.AxisFreeArtillery is not null) throw new InvalidOperationException("Legacy expanded promotion after audit cutoff.");
                    var successor = authority.PromoteExpandedIndependent();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeExpandedIndependentPromotion(successor)))
                        throw new InvalidOperationException("Expanded source world differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (eventType == "source-local-map-admitted")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var successor = authority.AdmitLocalMap();
                    if (!record.AsSpan().SequenceEqual(authority.SerializeLocalMapAdmission(successor)))
                        throw new InvalidOperationException("Local source map differs from re-adjudication.");
                    authority = successor;
                    continue;
                }
                if (authority is not null) throw new InvalidOperationException("Pre-game choice after finalization.");
                var actionId = document.RootElement.GetProperty("actionId").GetString();
                var action = replayed.Candidates().SingleOrDefault(value => value.ActionId == actionId)
                    ?? throw new InvalidOperationException("Pre-game history contains an unavailable choice.");
                var (next, actualEvent) = replayed.Apply(action);
                if (!record.AsSpan().SequenceEqual(actualEvent))
                    throw new InvalidOperationException("Pre-game event differs from re-adjudication.");
                replayed = next;
            }
            var actual = sourceTurn?.SerializeCheckpoint() ?? authority?.SerializeCheckpoint() ?? replayed.SerializeCheckpoint();
            var actualContacts = session.CurrentContacts.Records;
            var reconstructedContacts = replayedContacts.Records;
            if (replayedContacts.AuthorityVersion != session.CurrentContacts.AuthorityVersion
                || actualContacts.Count != reconstructedContacts.Count
                || actualContacts.Where((value, index) => !value.AsSpan().SequenceEqual(reconstructedContacts[index])).Any())
                return new(ExerciseReconstructionFailureReason.InvalidHistory, null, null, expectedHash, null);
            if (!expected.AsSpan().SequenceEqual(actual))
                return new(ExerciseReconstructionFailureReason.SnapshotMismatch, null, null, expectedHash, Hash(actual));
            return new(ExerciseReconstructionFailureReason.None, actual,
                includeStreamHash ? HashFramed(records) : null, expectedHash, Hash(actual));
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or JsonException or KeyNotFoundException or OverflowException)
        { return new(ExerciseReconstructionFailureReason.InvalidHistory, null, null, expectedHash, null); }
    }

    private static ExerciseReconstructionResult InvalidNativeHistory(ExerciseSession session)
    {
        object head = (object?)session.CurrentSecondBreakdown ?? session.CurrentSecondMovementCompletion ?? (object?)session.CurrentSecondMove
            ?? (object?)session.CurrentSecondReserve ?? session.CurrentPostCombatPass ?? (object?)session.CurrentTruckConvoyPass
            ?? throw new InvalidOperationException("Invalid native history requires a native head.");
        return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
            null, null, Hash(CampaignCurrentEventSerializer.Serialize(head)), null);
    }

    private static ExerciseReconstructionResult ReconstructOne(ExerciseSession completedSession, bool includeStreamHash,
        bool predecessorVerified)
    {
        ArgumentNullException.ThrowIfNull(completedSession);
        // Overhaul doc (docs/work/plans/2026-11-23-codebase-overhaul-direction.md),
        // Section 0.6 step 2: dispatch off completedSession.Pending's runtime type
        // instead of a 29-branch ordered null-check cascade. completedSession.Pending
        // is the SAME derived value QueryCheckpoint dispatches off (ExerciseSession.Pending,
        // computed once, in QueryCheckpoint's own already-correct most-recent-first order)
        // -- so this cascade can no longer drift out of sync with QueryCheckpoint's ordering
        // the way it had (2026-11-23 Reconstruct cascade ordering audit, Findings 2/3: the
        // StageEntrySecond group's internal order, and the six-mechanic preamble chain's
        // earliest-first order). There is no manual order left to get wrong: dispatch is by
        // the matched decision's runtime type, not by null-check sequence.
        switch (completedSession.Pending)
        {
            case StageEntrySecondBreakdownPending:
            {
                var stageEntrySecondBreakdown = completedSession.CurrentStageEntrySecondBreakdown!;
                var expected = stageEntrySecondBreakdown.SerializeCanonical();
                var stageEntrySecondBreakdownHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntrySecondBreakdownPredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry second-side Breakdown predecessor is missing.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry second-side Breakdown predecessor did not replay.");
                    var result = CompleteStageEntrySecondBreakdown(predecessor);
                    if (!result.IsAccepted || result.Evidence is null || result.SuccessorSession is null
                        || !expected.AsSpan().SequenceEqual(result.Evidence.EventRecords.Single())
                        || completedSession.CurrentContacts.AuthorityVersion != result.SuccessorSession.CurrentContacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != result.SuccessorSession.CurrentContacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(result.SuccessorSession.CurrentContacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry second-side Breakdown differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        expected, streamHash, stageEntrySecondBreakdownHash, Hash(result.Evidence.EventRecords.Single()));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, stageEntrySecondBreakdownHash, null); }
            }
            case StageEntrySecondMovementCompletionPending:
            {
                var stageEntrySecondMovementCompletion = completedSession.CurrentStageEntrySecondMovementCompletion!;
                var expected = stageEntrySecondMovementCompletion.SerializeCanonical();
                var secondCompletionExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntrySecondMovementCompletionPredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry second-side Movement completion has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry second-side Movement predecessor did not replay.");
                    var (source, world) = QueryStageEntrySecondMovementSource(predecessor);
                    var replayed = CampaignNativeSecondMovementCompletedV45.Create(source, world, predecessor.Context,
                        StageEntryOrders(predecessor), stageEntrySecondMovementCompletion.Side,
                        predecessor.CurrentSnapshot.Setup.InitialGameTurn, predecessor.CurrentContacts.AuthorityVersion);
                    var bytes = replayed.SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceSecondMovementCompletion(replayed);
                    if (!expected.AsSpan().SequenceEqual(bytes)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry second-side Movement completion differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        bytes, streamHash, secondCompletionExpectedHash, Hash(bytes));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, secondCompletionExpectedHash, null);
                }
            }
            case StageEntrySecondMovePending:
            {
                var stageEntrySecondMove = completedSession.CurrentStageEntrySecondMove!;
                var expected = stageEntrySecondMove.SerializeCanonical();
                var secondMoveExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntrySecondMovePredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry second-side Movement has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry second-side Movement predecessor did not replay.");
                    var (source, world) = QueryStageEntrySecondMovementSource(predecessor);
                    var (replayed, resultWorld) = CampaignNativeSecondMoveV44.Create(source, world,
                        predecessor.Context, StageEntryOrders(predecessor), predecessor.CurrentContacts,
                        stageEntrySecondMove.Side, predecessor.CurrentSnapshot.Setup.InitialGameTurn, stageEntrySecondMove.ElementId,
                        stageEntrySecondMove.ActionId, predecessor.CurrentSnapshot.RulesetHash);
                    var bytes = replayed.SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AppendNativeSecondMove(world, resultWorld, replayed, predecessor.Context);
                    if (!expected.AsSpan().SequenceEqual(bytes) || completedSession.CurrentStageEntrySecondMoveWorld != resultWorld
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry second-side Movement differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        bytes, streamHash, secondMoveExpectedHash, Hash(bytes));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, secondMoveExpectedHash, null);
                }
            }
            case StageEntrySecondReservePending:
            {
                var stageEntrySecondReserve = completedSession.CurrentStageEntrySecondReserve!;
                var expected = stageEntrySecondReserve.SerializeCanonical();
                var secondReserveExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntrySecondReservePredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry second-side Reserve has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry second-side Reserve predecessor did not replay.");
                    var (source, world) = QueryStageEntrySecondReserveSource(predecessor);
                    var (replayed, resultWorld) = CampaignNativeSecondReserveV43.Create(source, world,
                        predecessor.Context, StageEntryOrders(predecessor), stageEntrySecondReserve.Side,
                        stageEntrySecondReserve.ElementId, predecessor.CurrentSnapshot.Setup.InitialGameTurn,
                        predecessor.CurrentContacts.AuthorityVersion);
                    var bytes = replayed.SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceSecondReserve(stageEntrySecondReserve);
                    if (!expected.AsSpan().SequenceEqual(bytes) || completedSession.CurrentStageEntrySecondReserveWorld != resultWorld
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry second-side Reserve differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        bytes, streamHash, secondReserveExpectedHash, Hash(bytes));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, secondReserveExpectedHash, null);
                }
            }
            case StageEntrySecondAssaultPending:
            {
                var stageEntrySecondAssault = completedSession.CurrentStageEntrySecondAssault!;
                var expected = CampaignCurrentEventSerializer.Serialize(stageEntrySecondAssault.Events[^1],
                    completedSession.Context);
                var stageEntrySecondAssaultHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntrySecondAssaultPredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry second-side assault step has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry second-side assault predecessor did not replay.");
                    var replayed = (predecessor.CurrentStageEntrySecondAssault
                        ?? throw new InvalidCampaignHistoryException("Stage Entry second-side assault predecessor has no stage chain."))
                        .Advance(stageEntrySecondAssault.Events[^1]);
                    var actual = CampaignCurrentEventSerializer.Serialize(replayed.Events[^1], completedSession.Context);
                    if (!expected.AsSpan().SequenceEqual(actual)
                        || completedSession.CurrentContacts.AuthorityVersion != replayed.Contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != replayed.Contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(replayed.Contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry second-side assault step differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        expected, streamHash, stageEntrySecondAssaultHash, Hash(actual));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, stageEntrySecondAssaultHash, null); }
            }
            case StageEntrySecondNoAttackPending:
            {
                var stageEntrySecondNoAttack = completedSession.CurrentStageEntrySecondNoAttackCompletion!;
                var expected = stageEntrySecondNoAttack.SerializeCanonical();
                var stageEntrySecondNoAttackHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntrySecondNoAttackPredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry second-side no-attack completion has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry second-side no-attack predecessor did not replay.");
                    var result = CompleteStageEntrySecondCombatNoAttack(predecessor);
                    if (!result.IsAccepted || result.Evidence is null || result.SuccessorSession is null
                        || !expected.AsSpan().SequenceEqual(result.Evidence.EventRecords.Single())
                        || completedSession.CurrentContacts.AuthorityVersion != result.SuccessorSession.CurrentContacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != result.SuccessorSession.CurrentContacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(result.SuccessorSession.CurrentContacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry second-side no-attack completion differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        expected, streamHash, stageEntrySecondNoAttackHash, Hash(result.Evidence.EventRecords.Single()));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, stageEntrySecondNoAttackHash, null); }
            }
            case StageEntrySecondCombatSelectionPending:
            {
                var stageEntrySecondDecision = completedSession.CurrentStageEntrySecondCombatDecision!;
                var expected = stageEntrySecondDecision.Decision.SerializeCanonical();
                var stageEntrySecondDecisionHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntrySecondCombatSelectionPredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry second-side Combat selection has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry second-side Combat selection predecessor did not replay.");
                    var (world, position) = QueryStageEntrySecondCombatWorldAndPosition(predecessor);
                    var replayedDecision = CampaignNativeSecondCombatDecisionStateV47.Select(world, position,
                        predecessor.CurrentSnapshot, predecessor.CurrentSnapshot.RandomState, predecessor.Context,
                        predecessor.CurrentContacts, stageEntrySecondDecision.Decision.Side, stageEntrySecondDecision.Decision.ActingElementId,
                        stageEntrySecondDecision.Decision.ActionId).Decision;
                    var bytes = replayedDecision.SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceSecondCombatSelection(replayedDecision);
                    if (!expected.AsSpan().SequenceEqual(bytes)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry second-side Combat selection differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        bytes, streamHash, stageEntrySecondDecisionHash, Hash(bytes));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, stageEntrySecondDecisionHash, null); }
            }
            case StageEntryAssaultPending:
            {
                var stageEntryAssault = completedSession.CurrentStageEntryAssault!;
                var expected = CampaignCurrentEventSerializer.Serialize(stageEntryAssault.Events[^1],
                    completedSession.Context);
                var stageEntryAssaultHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntryAssaultPredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry assault step has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry assault predecessor did not replay.");
                    var replayed = (predecessor.CurrentStageEntryAssault
                        ?? throw new InvalidCampaignHistoryException("Stage Entry assault predecessor has no stage chain."))
                        .Advance(stageEntryAssault.Events[^1]);
                    var actual = CampaignCurrentEventSerializer.Serialize(replayed.Events[^1], completedSession.Context);
                    if (!expected.AsSpan().SequenceEqual(actual)
                        || completedSession.CurrentContacts.AuthorityVersion != replayed.Contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != replayed.Contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(replayed.Contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry assault step differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        expected, streamHash, stageEntryAssaultHash, Hash(actual));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, stageEntryAssaultHash, null); }
            }
            case StageEntryNoAttackPending:
            {
                var stageEntryNoAttack = completedSession.CurrentStageEntryNoAttackCompletion!;
                var expected = stageEntryNoAttack.SerializeCanonical();
                var stageEntryNoAttackHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntryNoAttackPredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry no-attack completion has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry no-attack predecessor did not replay.");
                    var result = CompleteStageEntryCombatNoAttack(predecessor);
                    if (!result.IsAccepted || result.Evidence is null || result.SuccessorSession is null
                        || !expected.AsSpan().SequenceEqual(result.Evidence.EventRecords.Single())
                        || completedSession.CurrentContacts.AuthorityVersion != result.SuccessorSession.CurrentContacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != result.SuccessorSession.CurrentContacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(result.SuccessorSession.CurrentContacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry no-attack completion differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        expected, streamHash, stageEntryNoAttackHash, Hash(result.Evidence.EventRecords.Single()));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, stageEntryNoAttackHash, null); }
            }
            case SecondAssaultPending:
            {
                var secondAssault = completedSession.CurrentSecondAssault!;
                var expected = CampaignCurrentEventSerializer.Serialize(secondAssault.Events[^1],
                    completedSession.Context);
                var secondAssaultHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.SecondAssaultPredecessor
                        ?? throw new InvalidCampaignHistoryException("Second-side assault step has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Second-side assault predecessor did not replay.");
                    var replayed = (predecessor.CurrentSecondAssault
                        ?? throw new InvalidCampaignHistoryException("Second-side assault predecessor has no stage chain."))
                        .Advance(secondAssault.Events[^1]);
                    var actual = CampaignCurrentEventSerializer.Serialize(replayed.Events[^1], completedSession.Context);
                    if (!expected.AsSpan().SequenceEqual(actual)
                        || completedSession.CurrentContacts.AuthorityVersion != replayed.Contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != replayed.Contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(replayed.Contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Second-side assault step differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        expected, streamHash, secondAssaultHash, Hash(actual));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, secondAssaultHash, null); }
            }
            case SecondNoAttackPending:
            {
                var secondNoAttack = completedSession.CurrentSecondNoAttackCompletion!;
                var expected = secondNoAttack.SerializeCanonical();
                var secondNoAttackHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.SecondNoAttackPredecessor
                        ?? throw new InvalidCampaignHistoryException("Second no-attack completion has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Second no-attack predecessor did not replay.");
                    var result = CompleteSecondCombatNoAttack(predecessor);
                    if (!result.IsAccepted || result.Evidence is null || result.SuccessorSession is null
                        || !expected.AsSpan().SequenceEqual(result.Evidence.EventRecords.Single())
                        || completedSession.CurrentContacts.AuthorityVersion != result.SuccessorSession.CurrentContacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != result.SuccessorSession.CurrentContacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(result.SuccessorSession.CurrentContacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Second no-attack completion differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        expected, streamHash, secondNoAttackHash, Hash(result.Evidence.EventRecords.Single()));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, secondNoAttackHash, null); }
            }
            case SecondCombatSelectionPending:
            {
                var secondDecision = completedSession.CurrentSecondCombatDecision!;
                var expected = secondDecision.Decision.SerializeCanonical();
                var secondDecisionHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.SecondCombatSelectionPredecessor
                        ?? throw new InvalidCampaignHistoryException("Second Combat selection has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Second Combat selection predecessor did not replay.");
                    var (world, position) = QuerySecondCombatWorldAndPosition(predecessor);
                    CampaignNativeSecondCombatDecisionV47 replayedDecision = secondDecision.Decision
                        is CampaignNativeSecondCombatSelectionTimeoutV48 { Evidence: var timeout }
                        ? CreateSecondSelectionTimeout(predecessor, timeout.ConfigurationHash,
                            timeout.OpenedAtUnixMilliseconds, timeout.DeadlineUnixMilliseconds,
                            timeout.HighWaterUnixMilliseconds, timeout.CancelledAtUnixMilliseconds, timeout.Reason)
                        : CampaignNativeSecondCombatDecisionStateV47.Select(world, position,
                            predecessor.CurrentSnapshot, predecessor.CurrentSnapshot.RandomState, predecessor.Context,
                            predecessor.CurrentContacts, secondDecision.Decision.Side, secondDecision.Decision.ActingElementId,
                            secondDecision.Decision.ActionId).Decision;
                    var bytes = replayedDecision.SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceSecondCombatSelection(replayedDecision);
                    if (!expected.AsSpan().SequenceEqual(bytes)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Second Combat selection differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        bytes, streamHash, secondDecisionHash, Hash(bytes));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, secondDecisionHash, null); }
            }
            case SecondBreakdownPending:
            {
                var secondBreakdown = completedSession.CurrentSecondBreakdown!;
                var expected = secondBreakdown.SerializeCanonical();
                var secondBreakdownHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.SecondBreakdownPredecessor
                        ?? throw new InvalidCampaignHistoryException("Second Breakdown predecessor is missing.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Second Breakdown predecessor did not replay.");
                    var result = CompleteSecondBreakdown(predecessor);
                    if (!result.IsAccepted || result.Evidence is null || result.SuccessorSession is null
                        || !expected.AsSpan().SequenceEqual(result.Evidence.EventRecords.Single())
                        || completedSession.CurrentContacts.AuthorityVersion != result.SuccessorSession.CurrentContacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != result.SuccessorSession.CurrentContacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(result.SuccessorSession.CurrentContacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Second Breakdown differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        expected, streamHash, secondBreakdownHash, Hash(result.Evidence.EventRecords.Single()));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, secondBreakdownHash, null); }
            }
            case SecondMovementCompletionPending:
            {
                var movementCompletion = completedSession.CurrentSecondMovementCompletion!;
                var expected = movementCompletion.SerializeCanonical();
                var completionExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.SecondMovementCompletionPredecessor
                        ?? throw new InvalidCampaignHistoryException("Second Movement completion has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Second Movement predecessor did not replay.");
                    var (source, world) = QuerySecondMovementSource(predecessor);
                    var replayed = CampaignNativeSecondMovementCompletedV45.Create(source, world, predecessor.Context,
                        predecessor.CurrentSnapshot.OperationStageOrders, movementCompletion.Side,
                        predecessor.CurrentSnapshot.Setup.InitialGameTurn, predecessor.CurrentContacts.AuthorityVersion);
                    var bytes = replayed.SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceSecondMovementCompletion(replayed);
                    if (!expected.AsSpan().SequenceEqual(bytes)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Second Movement completion differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        bytes, streamHash, completionExpectedHash, Hash(bytes));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, completionExpectedHash, null);
                }
            }
            case SecondMovePending:
            {
                var secondMove = completedSession.CurrentSecondMove!;
                var expected = secondMove.SerializeCanonical();
                var moveExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.SecondMovePredecessor
                        ?? throw new InvalidCampaignHistoryException("Second Movement has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Second Movement predecessor did not replay.");
                    var (source, world) = QuerySecondMovementSource(predecessor);
                    var (replayed, resultWorld) = CampaignNativeSecondMoveV44.Create(source, world,
                        predecessor.Context, predecessor.CurrentSnapshot.OperationStageOrders, predecessor.CurrentContacts,
                        secondMove.Side, predecessor.CurrentSnapshot.Setup.InitialGameTurn, secondMove.ElementId,
                        secondMove.ActionId, predecessor.CurrentSnapshot.RulesetHash);
                    var bytes = replayed.SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AppendNativeSecondMove(world, resultWorld, replayed, predecessor.Context);
                    if (!expected.AsSpan().SequenceEqual(bytes) || completedSession.CurrentSecondMoveWorld != resultWorld
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Second Movement differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        bytes, streamHash, moveExpectedHash, Hash(bytes));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, moveExpectedHash, null);
                }
            }
            case SecondReservePending:
            {
                var secondReserve = completedSession.CurrentSecondReserve!;
                var expected = secondReserve.SerializeCanonical();
                var reserveExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.SecondReservePredecessor
                        ?? throw new InvalidCampaignHistoryException("Second Reserve has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Second Reserve predecessor did not replay.");
                    var (source, world) = QuerySecondReserveSource(predecessor);
                    var (replayed, resultWorld) = CampaignNativeSecondReserveV43.Create(source, world,
                        predecessor.Context, predecessor.CurrentSnapshot.OperationStageOrders, secondReserve.Side,
                        secondReserve.ElementId, predecessor.CurrentSnapshot.Setup.InitialGameTurn,
                        predecessor.CurrentContacts.AuthorityVersion);
                    var bytes = replayed.SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceSecondReserve(secondReserve);
                    if (!expected.AsSpan().SequenceEqual(bytes) || completedSession.CurrentSecondReserveWorld != resultWorld
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Second Reserve differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        bytes, streamHash, reserveExpectedHash, Hash(bytes));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, reserveExpectedHash, null);
                }
            }
            case StageEntryCombatSelectionPending:
            {
                var stageEntryDecision = completedSession.CurrentStageEntryCombatDecision!;
                var expected = stageEntryDecision.Decision.SerializeCanonical();
                var stageEntryDecisionHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntryCombatSelectionPredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry Combat selection has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry Combat selection predecessor did not replay.");
                    var (world, position) = QueryStageEntryCombatWorldAndPosition(predecessor);
                    var replayedDecision = CampaignNativeSecondCombatDecisionStateV47.Select(world, position,
                        predecessor.CurrentSnapshot, predecessor.CurrentSnapshot.RandomState, predecessor.Context,
                        predecessor.CurrentContacts, stageEntryDecision.Decision.Side, stageEntryDecision.Decision.ActingElementId,
                        stageEntryDecision.Decision.ActionId).Decision;
                    var bytes = replayedDecision.SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceSecondCombatSelection(replayedDecision);
                    if (!expected.AsSpan().SequenceEqual(bytes)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry Combat selection differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        bytes, streamHash, stageEntryDecisionHash, Hash(bytes));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, stageEntryDecisionHash, null); }
            }
            case StageEntryBreakdownPending:
            {
                var stageEntryBreakdown = completedSession.CurrentStageEntryBreakdown!;
                var expected = stageEntryBreakdown.SerializeCanonical();
                var stageEntryBreakdownHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntryBreakdownPredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry Breakdown predecessor is missing.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry Breakdown predecessor did not replay.");
                    var result = CompleteStageEntryBreakdown(predecessor);
                    if (!result.IsAccepted || result.Evidence is null || result.SuccessorSession is null
                        || !expected.AsSpan().SequenceEqual(result.Evidence.EventRecords.Single())
                        || completedSession.CurrentContacts.AuthorityVersion != result.SuccessorSession.CurrentContacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != result.SuccessorSession.CurrentContacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(result.SuccessorSession.CurrentContacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry Breakdown differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        expected, streamHash, stageEntryBreakdownHash, Hash(result.Evidence.EventRecords.Single()));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, stageEntryBreakdownHash, null); }
            }
            case StageEntryMovementCompletionPending:
            {
                var stageEntryMovementCompletion = completedSession.CurrentStageEntryMovementCompletion!;
                var expected = stageEntryMovementCompletion.SerializeCanonical();
                var completionExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntryMovementCompletionPredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry Movement completion has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry Movement predecessor did not replay.");
                    var (source, world) = QueryStageEntryMovementSource(predecessor);
                    var replayed = CampaignNativeSecondMovementCompletedV45.Create(source, world, predecessor.Context,
                        StageEntryOrders(predecessor), stageEntryMovementCompletion.Side,
                        predecessor.CurrentSnapshot.Setup.InitialGameTurn, predecessor.CurrentContacts.AuthorityVersion);
                    var bytes = replayed.SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceSecondMovementCompletion(replayed);
                    if (!expected.AsSpan().SequenceEqual(bytes)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry Movement completion differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        bytes, streamHash, completionExpectedHash, Hash(bytes));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, completionExpectedHash, null);
                }
            }
            case StageEntryMovePending:
            {
                var stageEntryMove = completedSession.CurrentStageEntryMove!;
                var expected = stageEntryMove.SerializeCanonical();
                var moveExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntryMovePredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry Movement has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry Movement predecessor did not replay.");
                    var (source, world) = QueryStageEntryMovementSource(predecessor);
                    var (replayed, resultWorld) = CampaignNativeSecondMoveV44.Create(source, world,
                        predecessor.Context, StageEntryOrders(predecessor), predecessor.CurrentContacts,
                        stageEntryMove.Side, predecessor.CurrentSnapshot.Setup.InitialGameTurn, stageEntryMove.ElementId,
                        stageEntryMove.ActionId, predecessor.CurrentSnapshot.RulesetHash);
                    var bytes = replayed.SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AppendNativeSecondMove(world, resultWorld, replayed, predecessor.Context);
                    if (!expected.AsSpan().SequenceEqual(bytes) || completedSession.CurrentStageEntryMoveWorld != resultWorld
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry Movement differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        bytes, streamHash, moveExpectedHash, Hash(bytes));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, moveExpectedHash, null);
                }
            }
            case StageEntryReservePending:
            {
                var stageEntryReserve = completedSession.CurrentStageEntryReserve!;
                var expected = stageEntryReserve.SerializeCanonical();
                var reserveExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntryReservePredecessor
                        ?? throw new InvalidCampaignHistoryException("Stage Entry Reserve has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Stage Entry Reserve predecessor did not replay.");
                    var (source, world) = QueryStageEntryReserveSource(predecessor);
                    var (replayed, resultWorld) = CampaignNativeSecondReserveV43.Create(source, world,
                        predecessor.Context, StageEntryOrders(predecessor), stageEntryReserve.Side,
                        stageEntryReserve.ElementId, predecessor.CurrentSnapshot.Setup.InitialGameTurn,
                        predecessor.CurrentContacts.AuthorityVersion);
                    var bytes = replayed.SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceSecondReserve(stageEntryReserve);
                    if (!expected.AsSpan().SequenceEqual(bytes) || completedSession.CurrentStageEntryReserveWorld != resultWorld
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Stage Entry Reserve differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        bytes, streamHash, reserveExpectedHash, Hash(bytes));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, reserveExpectedHash, null);
                }
            }
            case PostCombatPassPending:
            {
                var phasePassed = completedSession.CurrentPostCombatPass!;
                var expected = phasePassed.SerializeCanonical();
                var passExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.PostCombatPassPredecessor
                        ?? throw new InvalidCampaignHistoryException("Post-combat pass has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Post-combat predecessor did not replay.");
                    var replayed = CampaignNativePostCombatPassV42.Create(QueryPostCombatPassSource(predecessor),
                        phasePassed.Side, predecessor.CurrentSnapshot.Setup.InitialGameTurn,
                        predecessor.CurrentContacts.AuthorityVersion).SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvancePostCombatPass(phasePassed);
                    if (!expected.AsSpan().SequenceEqual(replayed)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Post-combat pass differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        replayed, streamHash, passExpectedHash, Hash(replayed));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, passExpectedHash, null);
                }
            }
            case TruckConvoyPassPending:
            {
                var passed = completedSession.CurrentTruckConvoyPass!;
                var expected = passed.SerializeCanonical();
                var passExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.TruckConvoyPassPredecessor
                        ?? throw new InvalidCampaignHistoryException("Truck Convoy pass has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Truck Convoy predecessor did not replay.");
                    var (release, control) = QueryTruckConvoyPassSource(predecessor);
                    var replayed = CampaignNativeTruckConvoyPassV41.Create(release, control, passed.Side,
                        predecessor.CurrentContacts.AuthorityVersion).SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceTruckConvoyPass(passed);
                    if (!expected.AsSpan().SequenceEqual(replayed)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Truck Convoy pass differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        replayed, streamHash, passExpectedHash, Hash(replayed));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, passExpectedHash, null);
                }
            }
            case InitiativeDeclarationPending:
            {
                var declared = completedSession.CurrentInitiativeDeclaration!;
                var expected = declared.SerializeCanonical();
                var declaredExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.InitiativeDeclarationPredecessor
                        ?? throw new InvalidCampaignHistoryException("Initiative Declaration has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Initiative Declaration predecessor did not replay.");
                    // MTS-TASK-003 (2026-11-23) correction: this previously read
                    // predecessor.CurrentPostCombatPass -- the FIRST side's own
                    // Patrol pass, fixed forever once set and long stale by this
                    // point -- instead of the SECOND side's own terminal pass
                    // (CampaignNativeRepeatAssaultRuntime.PostCombatPasses' last
                    // entry, confirmed as the real predecessor shape by
                    // CampaignNativeStageEntryPreambleTests' own
                    // SecondSidePatrolComplete helper). Never triggered until
                    // now: no caller of WithInitiativeDeclaration existed before
                    // this change, so this mismatch was latent, not observed.
                    var replayed = CampaignNativeInitiativeDeclarationV49.Create(
                        predecessor.CurrentSecondAssault!.PostCombatPasses[^1],
                        declared.DeclaringHolder, declared.FirstSide == declared.DeclaringHolder,
                        predecessor.CurrentSnapshot.Setup.InitialGameTurn, predecessor.CurrentContacts.AuthorityVersion)
                        .SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceInitiativeDeclaration(declared);
                    if (!expected.AsSpan().SequenceEqual(replayed)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Initiative Declaration differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        replayed, streamHash, declaredExpectedHash, Hash(replayed));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, declaredExpectedHash, null);
                }
            }
            case StageEntryWeatherPending:
            {
                var determined = completedSession.CurrentStageEntryWeather!;
                var expected = determined.SerializeCanonical();
                var determinedExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.StageEntryWeatherPredecessor
                        ?? throw new InvalidCampaignHistoryException("Weather Determination has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Weather Determination predecessor did not replay.");
                    var genesisRandom = predecessor.CurrentSnapshot.RandomState;
                    var priorRandomState = new RandomStreamState(genesisRandom.ContractVersion, genesisRandom.AlgorithmId,
                        genesisRandom.Seed, determined.PriorRandomCursor);
                    var replayed = CampaignNativeWeatherDeterminationV50.Create(predecessor.CurrentInitiativeDeclaration!,
                        priorRandomState, predecessor.CurrentSnapshot.Setup.InitialGameTurn,
                        predecessor.CurrentContacts.AuthorityVersion).SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceWeatherDetermination(determined);
                    if (!expected.AsSpan().SequenceEqual(replayed)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Weather Determination differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        replayed, streamHash, determinedExpectedHash, Hash(replayed));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, determinedExpectedHash, null);
                }
            }
            case NoObligationOrganizationPending:
            {
                var organizationResolved = completedSession.CurrentNoObligationOrganization!;
                var expected = organizationResolved.SerializeCanonical();
                var organizationExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.NoObligationOrganizationPredecessor
                        ?? throw new InvalidCampaignHistoryException("Organization resolution has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Organization predecessor did not replay.");
                    var replayed = CampaignNativeNoObligationOrganizationV51.Create(predecessor.CurrentStageEntryWeather!,
                        predecessor.CurrentContacts.AuthorityVersion).SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceNoObligationOrganization(organizationResolved);
                    if (!expected.AsSpan().SequenceEqual(replayed)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Organization resolution differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        replayed, streamHash, organizationExpectedHash, Hash(replayed));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, organizationExpectedHash, null);
                }
            }
            case NoObligationNavalConvoyArrivalPending:
            {
                var convoyArrivalResolved = completedSession.CurrentNoObligationNavalConvoyArrival!;
                var expected = convoyArrivalResolved.SerializeCanonical();
                var convoyArrivalExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.NoObligationNavalConvoyArrivalPredecessor
                        ?? throw new InvalidCampaignHistoryException("Naval Convoy Arrival resolution has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Naval Convoy Arrival predecessor did not replay.");
                    var replayed = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(
                        predecessor.CurrentNoObligationOrganization!, predecessor.Context,
                        predecessor.CurrentContacts.AuthorityVersion).SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceNoObligationNavalConvoyArrival(convoyArrivalResolved);
                    if (!expected.AsSpan().SequenceEqual(replayed)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Naval Convoy Arrival resolution differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        replayed, streamHash, convoyArrivalExpectedHash, Hash(replayed));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, convoyArrivalExpectedHash, null);
                }
            }
            case NoObligationFleetAssignmentPending:
            {
                var fleetAssignmentResolved = completedSession.CurrentNoObligationFleetAssignment!;
                var expected = fleetAssignmentResolved.SerializeCanonical();
                var fleetAssignmentExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.NoObligationFleetAssignmentPredecessor
                        ?? throw new InvalidCampaignHistoryException("Fleet Assignment resolution has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Fleet Assignment predecessor did not replay.");
                    var replayed = CampaignNativeNoObligationFleetAssignmentV53.Create(
                        predecessor.CurrentNoObligationNavalConvoyArrival!,
                        predecessor.CurrentContacts.AuthorityVersion).SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceNoObligationFleetAssignment(fleetAssignmentResolved);
                    if (!expected.AsSpan().SequenceEqual(replayed)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Fleet Assignment resolution differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        replayed, streamHash, fleetAssignmentExpectedHash, Hash(replayed));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, fleetAssignmentExpectedHash, null);
                }
            }
            case NoObligationFleetRepairPending:
            {
                var fleetRepairResolved = completedSession.CurrentNoObligationFleetRepair!;
                var expected = fleetRepairResolved.SerializeCanonical();
                var fleetRepairExpectedHash = Hash(expected);
                try
                {
                    var predecessor = completedSession.NoObligationFleetRepairPredecessor
                        ?? throw new InvalidCampaignHistoryException("Fleet Repair resolution has no predecessor.");
                    if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                        throw new InvalidCampaignHistoryException("Fleet Repair predecessor did not replay.");
                    var replayed = CampaignNativeNoObligationFleetRepairV54.Create(
                        predecessor.CurrentNoObligationFleetAssignment!,
                        predecessor.CurrentContacts.AuthorityVersion).SerializeCanonical();
                    var contacts = predecessor.CurrentContacts.AdvanceNoObligationFleetRepair(fleetRepairResolved);
                    if (!expected.AsSpan().SequenceEqual(replayed)
                        || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                        || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                        || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                            .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                        throw new InvalidCampaignHistoryException("Fleet Repair resolution differs from replay.");
                    var streamHash = includeStreamHash
                        ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                        replayed, streamHash, fleetRepairExpectedHash, Hash(replayed));
                }
                catch (Exception error) when (error is JsonException or InvalidCampaignHistoryException
                    or ArgumentException or InvalidOperationException)
                {
                    return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                        null, null, fleetRepairExpectedHash, null);
                }
            }
        }
        var expectedBytes = completedSession.CurrentNoAttackCompletion is { } completion
            ? completion.SerializeStateCanonical(completedSession.CurrentCombatDecision!)
            : completedSession.CurrentCombatRetreatDecline is { } decline
                ? completedSession.CurrentCombatRetreatMove is not null
                    ? decline.SerializeAfterMoveStateCanonical(
                        SerializeRetreatMoveState(completedSession))
                    : decline.SerializeStateCanonical(completedSession.CurrentCombatDecision!,
                        completedSession.CurrentCombatBarrage!)
            : completedSession.CurrentCombatRetreatMove is not null
                ? SerializeRetreatMoveState(completedSession)
            : completedSession.CurrentCombatBarrage is { } barrage
                ? barrage.SerializeStateCanonical(completedSession.CurrentCombatDecision!)
            : completedSession.CurrentCombatDecision is { } combat
                ? combat.SerializeCanonical()
                : CampaignCurrentSnapshotSerializer.Serialize(completedSession.CurrentSnapshot, completedSession.Context);
        if (completedSession.CurrentCombatRbaCancellation is { } rbaCancellation)
            expectedBytes = rbaCancellation.SerializeStateCanonical(expectedBytes);
        if (completedSession.CurrentCombatForceSeals.Count > 0
            || completedSession.CurrentCombatForceCancellation is not null)
        {
            expectedBytes = SerializeRetreatDeclineState(completedSession);
            foreach (var seal in completedSession.CurrentCombatForceSeals)
                expectedBytes = seal.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatForceCompletion is { } forceCompletion)
                expectedBytes = forceCompletion.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatForceCancellation is { } forceCancellation)
                expectedBytes = forceCancellation.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatAntiArmor is { } antiArmor)
                expectedBytes = antiArmor.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatCancelledAssault is { } cancelledAssault)
                expectedBytes = cancelledAssault.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatAssaultResult is { } assaultResult)
                expectedBytes = assaultResult.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatPostResultRetreat is { } postResultRetreat)
                expectedBytes = postResultRetreat.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatSettlement is { } settlement)
                expectedBytes = settlement.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatCustodyDisposition is { } custody)
                expectedBytes = custody.SerializeStateCanonical(expectedBytes);
        }
        if (completedSession.CurrentCombatSuccessor is not null)
            expectedBytes = CampaignNativeCombatSuccessorV26.Project(
                completedSession.CurrentCombatDecision!,
                completedSession.CurrentCombatAssaultResult!,
                completedSession.CurrentCombatSettlement!,
                completedSession.CurrentCombatCustodyDisposition!,
                completedSession.CurrentCombatCustodyWorld!,
                completedSession.CurrentCombatCustodyState,
                completedSession.Context).SerializeCanonical(completedSession.Context);
        if (completedSession.CurrentReserveRelease is { } reserveRelease)
            expectedBytes = reserveRelease.SerializeCanonical();
        if (completedSession.CurrentCycleControl is { } cycleControl)
            expectedBytes = cycleControl.SerializeCanonical();
        if (completedSession.CurrentRepeatMove is { } repeatMove)
            expectedBytes = repeatMove.SerializeCanonical();
        if (completedSession.CurrentRepeatBreakdown is { } repeatBreakdown)
            expectedBytes = repeatBreakdown.SerializeCanonical();
        if (completedSession.CurrentRepeatCombatSelection is { } repeatSelection)
            expectedBytes = repeatSelection.SerializeCanonical();
        if (completedSession.CurrentRepeatNoAttack is { } repeatNoAttack)
            expectedBytes = repeatNoAttack.SerializeCanonical();
        if (completedSession.CurrentRepeatAssaultEvents.Count > 0)
            expectedBytes = CampaignCurrentEventSerializer.Serialize(
                completedSession.CurrentRepeatAssaultEvents[^1]);
        if (completedSession.CurrentPostRepeatEvents.Count > 0)
            expectedBytes = CampaignCurrentEventSerializer.Serialize(
                completedSession.CurrentPostRepeatEvents[^1]);
        var expectedHash = Hash(expectedBytes);
        string? eventStreamHash = null;

        try
        {
            var canonicalEvents = completedSession.CurrentHistory
                .Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))
                .ToArray();
            eventStreamHash = HashFramed(canonicalEvents);
            var replayEvents = canonicalEvents
                .Select(value => CampaignCurrentEventSerializer.Deserialize(value, completedSession.Context))
                .ToArray();
            var combatTailCount = completedSession.CurrentCombatDecision is null ? 0
                : completedSession.CurrentCombatBarrage is not null
                    ? 2 + completedSession.CurrentCombatRetreatMoves.Count
                        + (completedSession.CurrentCombatRetreatDecline is null ? 0 : 1)
                        + completedSession.CurrentCombatForceSeals.Count
                        + (completedSession.CurrentCombatForceCompletion is null ? 0 : 1)
                        + (completedSession.CurrentCombatForceCancellation is null ? 0 : 1)
                        + (completedSession.CurrentCombatRbaCancellation is null ? 0 : 1)
                        + (completedSession.CurrentCombatAntiArmor is null ? 0 : 1)
                        + (completedSession.CurrentCombatCancelledAssault is null ? 0 : 1)
                        + (completedSession.CurrentCombatAssaultResult is null ? 0 : 1)
                        + (completedSession.CurrentCombatPostResultRetreat is null ? 0 : 1)
                        + (completedSession.CurrentCombatSettlement is null ? 0 : 1)
                        + (completedSession.CurrentCombatCustodyDisposition is null ? 0 : 1)
                        + (completedSession.CurrentCombatSuccessor is null ? 0 : 1)
                        + completedSession.CurrentReserveReleaseEvents.Count
                        + (completedSession.CurrentCycleControl is null ? 0 : 1)
                        + (completedSession.CurrentRepeatMove is null ? 0 : 1)
                        + (completedSession.CurrentRepeatBreakdown is null ? 0 : 1)
                        + (completedSession.CurrentRepeatCombatSelection is null ? 0 : 1)
                        + (completedSession.CurrentRepeatNoAttack is null ? 0 : 1)
                        + completedSession.CurrentRepeatAssaultEvents.Count
                        + completedSession.CurrentPostRepeatEvents.Count
                : completedSession.CurrentNoAttackCompletion is not null
                    ? 2 + completedSession.CurrentReserveReleaseEvents.Count
                        + (completedSession.CurrentCycleControl is null ? 0 : 1)
                    : 1;
            if (combatTailCount > 0 && replayEvents.Length < combatTailCount + 1)
                throw new InvalidCampaignHistoryException("Native Combat selection requires prior authority history.");
            var priorEvents = combatTailCount == 0 ? replayEvents
                : replayEvents[..^combatTailCount];
            var replayed = CampaignCurrentProjector.Replay(priorEvents, completedSession.Context);
            var replayedContacts = CampaignContactJournal.FromEvents(
                completedSession.Context, priorEvents);
            byte[] replayedBytes;
            var worldMatch = completedSession.CurrentCombatRetreatWorld is null;
            var paidWorldMatch = completedSession.CurrentCombatPaidWorld is null;
            var settledWorldMatch = completedSession.CurrentCombatSettledWorld is null;
            var custodyMatch = completedSession.CurrentCombatCustodyDisposition is null;
            var repeatWorldMatch = completedSession.CurrentRepeatMoveWorld is null;
            var postWorldMatch = completedSession.CurrentPostRepeatEvents.Count == 0;
            if (completedSession.CurrentCombatDecision is not null)
            {
                var decisionIndex = replayEvents.Length - combatTailCount;
                if (replayEvents[decisionIndex] is not CampaignNativeCombatDecisionV12 selected)
                    throw new InvalidCampaignHistoryException("Native Combat selection is missing.");
                var verified = CampaignNativeCombatDecisionStateV12.Replay(replayed,
                    completedSession.Context, replayedContacts, selected.SerializeCanonical());
                replayedContacts = replayedContacts.AdvanceNativeCombatSelection(verified);
                if (completedSession.CurrentNoAttackCompletion is not null)
                {
                    if (replayEvents[decisionIndex + 1]
                        is not CampaignNativeCombatNoAttackCompletionV13 noAttack)
                        throw new InvalidCampaignHistoryException("Native no-attack completion is missing.");
                    var verifiedCompletion = CampaignNativeCombatNoAttackCompletionV13.Replay(
                        verified, completedSession.Context, noAttack.SerializeCanonical());
                    replayedBytes = verifiedCompletion.SerializeStateCanonical(verified);
                    replayedContacts = replayedContacts.AdvanceNativeCombatNoAttackCompletion(
                        verifiedCompletion);
                }
                else if (completedSession.CurrentCombatBarrage is not null)
                {
                    if (replayEvents[decisionIndex + 1] is not CampaignNativeCombatBarrageV14 barrageEvent)
                        throw new InvalidCampaignHistoryException("Native Barrage completion is missing.");
                    var verifiedBarrage = CampaignNativeCombatBarrageV14.Replay(
                        verified, completedSession.Context, barrageEvent.SerializeCanonical());
                    replayedContacts = replayedContacts.AdvanceNativeCombatBarrage(verifiedBarrage);
                    var world = verified.PriorSnapshot.World;
                    CampaignNativeCombatRetreatMoveV16? previousMove = null;
                    string? initialLocationId = null;
                    byte[] moveState = [];
                    for (var moveIndex = 0;
                         moveIndex < completedSession.CurrentCombatRetreatMoves.Count;
                         moveIndex++)
                    {
                        if (replayEvents[decisionIndex + 2 + moveIndex]
                            is not CampaignNativeCombatRetreatMoveV16 moveEvent)
                            throw new InvalidCampaignHistoryException("Native defender move is missing.");
                        var beforeContacts = replayedContacts;
                        var verifiedMove = previousMove is null
                            ? CampaignNativeCombatRetreatMoveV16.ReplayFirst(
                                verified, verifiedBarrage, completedSession.Context,
                                beforeContacts, moveEvent.SerializeCanonical())
                            : CampaignNativeCombatRetreatMoveV16.ReplayNext(
                                verified, verifiedBarrage, previousMove, world,
                                initialLocationId!, completedSession.Context,
                                beforeContacts, moveEvent.SerializeCanonical());
                        var nextWorld = previousMove is null
                            ? verifiedMove.ProjectWorld(verified, verifiedBarrage,
                                completedSession.Context, beforeContacts)
                            : verifiedMove.ProjectWorldAfter(verified, verifiedBarrage,
                                previousMove, world, initialLocationId!,
                                completedSession.Context, beforeContacts);
                        moveState = previousMove is null
                            ? verifiedMove.SerializeStateCanonical(verified, verifiedBarrage,
                                completedSession.Context, beforeContacts)
                            : verifiedMove.SerializeNextStateCanonical(moveState, nextWorld);
                        replayedContacts = replayedContacts.AdvanceNativeCombatRetreatMove(
                            world, nextWorld, verifiedMove,
                            verified.Decision.BoundRepresentationId!, completedSession.Context);
                        initialLocationId ??= verifiedMove.OriginLocationId;
                        world = nextWorld;
                        previousMove = verifiedMove;
                    }
                    if (previousMove is not null)
                        worldMatch = world.Equals(completedSession.CurrentCombatRetreatWorld);
                    if (completedSession.CurrentCombatRbaCancellation is not null)
                    {
                        if (replayEvents[decisionIndex + 2 + completedSession.CurrentCombatRetreatMoves.Count]
                            is not CampaignNativeCombatRbaCancellationV33 rbaEvent)
                            throw new InvalidCampaignHistoryException("Native RBA cancellation is missing.");
                        var verifiedRba = CampaignNativeCombatRbaCancellationV33.Replay(
                            verified, verifiedBarrage, previousMove, rbaEvent.SerializeCanonical());
                        replayedBytes = verifiedRba.SerializeStateCanonical(previousMove is null
                            ? verifiedBarrage.SerializeStateCanonical(verified) : moveState);
                        replayedContacts = replayedContacts.AdvanceNativeCombatRbaCancellation(verifiedRba);
                    }
                    else if (completedSession.CurrentCombatRetreatDecline is not null)
                    {
                        if (replayEvents[decisionIndex + 2 + completedSession.CurrentCombatRetreatMoves.Count]
                            is not CampaignNativeCombatRetreatDeclineV15 declineEvent)
                            throw new InvalidCampaignHistoryException("Native defender decline is missing.");
                        var verifiedDecline = previousMove is null
                            ? CampaignNativeCombatRetreatDeclineV15.Replay(
                                verified, verifiedBarrage, completedSession.Context,
                                replayedContacts, declineEvent.SerializeCanonical())
                            : CampaignNativeCombatRetreatDeclineV15.ReplayAfterMove(
                                verified, verifiedBarrage, previousMove, world,
                                initialLocationId!, completedSession.Context,
                                replayedContacts, declineEvent.SerializeCanonical());
                        replayedBytes = previousMove is null
                            ? verifiedDecline.SerializeStateCanonical(verified, verifiedBarrage)
                            : verifiedDecline.SerializeAfterMoveStateCanonical(moveState);
                        replayedContacts = replayedContacts.AdvanceNativeCombatRetreatDecline(
                            verifiedDecline);
                        CampaignNativeCombatForceSealV17? firstSeal = null;
                        for (var sealIndex = 0;
                             sealIndex < completedSession.CurrentCombatForceSeals.Count;
                             sealIndex++)
                        {
                            var eventIndex = decisionIndex + 3
                                + completedSession.CurrentCombatRetreatMoves.Count + sealIndex;
                            if (replayEvents[eventIndex] is not CampaignNativeCombatForceSealV17 sealEvent)
                                throw new InvalidCampaignHistoryException("Native Force Assignment seal is missing.");
                            var verifiedSeal = CampaignNativeCombatForceSealV17.Replay(
                                verified, verifiedDecline, world, completedSession.Context,
                                replayedContacts, sealEvent.SerializeCanonical(), firstSeal);
                            replayedBytes = verifiedSeal.SerializeStateCanonical(replayedBytes);
                            replayedContacts = replayedContacts.AdvanceNativeCombatForceSeal(verifiedSeal);
                            firstSeal ??= verifiedSeal;
                        }
                        CampaignNativeCombatForceCompletionV18? verifiedForceCompletion = null;
                        if (completedSession.CurrentCombatForceCompletion is not null)
                        {
                            var completionIndex = decisionIndex + 3
                                + completedSession.CurrentCombatRetreatMoves.Count
                                + completedSession.CurrentCombatForceSeals.Count;
                            if (replayEvents[completionIndex]
                                is not CampaignNativeCombatForceCompletionV18 completionEvent)
                                throw new InvalidCampaignHistoryException("Native Force completion is missing.");
                            var verifiedCompletion = CampaignNativeCombatForceCompletionV18.Replay(
                                verified, verifiedDecline, completedSession.CurrentCombatForceSeals,
                                completionEvent.SerializeCanonical());
                            verifiedForceCompletion = verifiedCompletion;
                            replayedBytes = verifiedCompletion.SerializeStateCanonical(replayedBytes);
                            replayedContacts = replayedContacts.AdvanceNativeCombatForceCompletion(
                                verifiedCompletion);
                        }
                        if (completedSession.CurrentCombatForceCancellation is not null)
                        {
                            var cancellationIndex = decisionIndex + 3
                                + completedSession.CurrentCombatRetreatMoves.Count
                                + completedSession.CurrentCombatForceSeals.Count;
                            if (replayEvents[cancellationIndex]
                                is not CampaignNativeCombatForceCancellationV19 cancellationEvent)
                                throw new InvalidCampaignHistoryException("Native Force cancellation is missing.");
                            var verifiedCancellation = CampaignNativeCombatForceCancellationV19.Replay(
                                verified, verifiedDecline, completedSession.CurrentCombatForceSeals,
                                cancellationEvent.SerializeCanonical());
                            if (verifiedCancellation.Reason == "force-unavailable"
                                && verifiedCancellation.ConfigurationHash
                                    != CampaignNativeCombatForceCancellationV19
                                        .NoTimingConfigurationHash
                                && CampaignNativeCombatForceEligibility.CanCompleteAssault(
                                    verified, verifiedDecline, world,
                                    completedSession.Context, priorEvents))
                                throw new InvalidCampaignHistoryException(
                                    "Force-unavailable cancellation has a legal assault role pair.");
                            replayedBytes = verifiedCancellation.SerializeStateCanonical(replayedBytes);
                            replayedContacts = replayedContacts.AdvanceNativeCombatForceCancellation(
                                verifiedCancellation);
                        }
                        CampaignNativeCombatAntiArmorV20? verifiedAntiArmor = null;
                        if (completedSession.CurrentCombatAntiArmor is not null)
                        {
                            var antiArmorIndex = decisionIndex + 3
                                + completedSession.CurrentCombatRetreatMoves.Count
                                + completedSession.CurrentCombatForceSeals.Count + 1;
                            if (replayEvents[antiArmorIndex]
                                is not CampaignNativeCombatAntiArmorV20 antiArmorEvent)
                                throw new InvalidCampaignHistoryException("Native Anti Armor completion is missing.");
                            var replayedAntiArmor = CampaignNativeCombatAntiArmorV20.Replay(
                                verified, completedSession.CurrentCombatForceCompletion,
                                completedSession.CurrentCombatForceCancellation,
                                completedSession.Context, antiArmorEvent.SerializeCanonical());
                            verifiedAntiArmor = replayedAntiArmor;
                            replayedBytes = replayedAntiArmor.SerializeStateCanonical(replayedBytes);
                            replayedContacts = replayedContacts.AdvanceNativeCombatAntiArmor(
                                replayedAntiArmor);
                        }
                        if (completedSession.CurrentCombatCancelledAssault is not null)
                        {
                            var cancelledIndex = decisionIndex + 3
                                + completedSession.CurrentCombatRetreatMoves.Count
                                + completedSession.CurrentCombatForceSeals.Count + 2;
                            if (replayEvents[cancelledIndex]
                                is not CampaignNativeCombatCancelledAssaultV21 cancelledEvent)
                                throw new InvalidCampaignHistoryException("Cancelled Close Assault completion is missing.");
                            var verifiedCancelled = CampaignNativeCombatCancelledAssaultV21.Replay(
                                verified, completedSession.CurrentCombatAntiArmor!,
                                cancelledEvent.SerializeCanonical());
                            replayedBytes = verifiedCancelled.SerializeStateCanonical(replayedBytes);
                            replayedContacts = replayedContacts.AdvanceNativeCombatCancelledAssault(
                                verifiedCancelled);
                        }
                        if (completedSession.CurrentCombatAssaultResult is not null)
                        {
                            var resultIndex = decisionIndex + 3
                                + completedSession.CurrentCombatRetreatMoves.Count
                                + completedSession.CurrentCombatForceSeals.Count + 2;
                            if (replayEvents[resultIndex]
                                is not CampaignNativeCombatAssaultResultV22 resultEvent
                                || verifiedForceCompletion is null
                                || verifiedAntiArmor is null)
                                throw new InvalidCampaignHistoryException(
                                    "Native Combat result or its prior authority is missing.");
                            var ammunition = CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(
                                completedSession.Context, priorEvents);
                            var inputs = CampaignNativeCombatAssaultInputs.Create(verified,
                                verifiedForceCompletion, verifiedAntiArmor,
                                completedSession.CurrentCombatForceSeals, world,
                                completedSession.Context, ammunition);
                            var verifiedResult = CampaignNativeCombatAssaultResultV22.Replay(
                                verified, verifiedAntiArmor, inputs,
                                verified.PriorSnapshot.RandomState,
                                resultEvent.SerializeCanonical());
                            paidWorldMatch = inputs.PaidWorld.Equals(
                                completedSession.CurrentCombatPaidWorld);
                            replayedBytes = verifiedResult.SerializeStateCanonical(replayedBytes);
                            replayedContacts = replayedContacts.AdvanceNativeCombatAssaultResult(
                                verifiedResult);
                            if (completedSession.CurrentCombatPostResultRetreat is not null)
                            {
                                if (replayEvents.ElementAtOrDefault(resultIndex + 1)
                                    is not CampaignNativeCombatRetreatAfterAssaultV23 retreatEvent)
                                    throw new InvalidCampaignHistoryException(
                                        "Post-result defender retreat is missing.");
                                var verifiedRetreat = CampaignNativeCombatRetreatAfterAssaultV23.Replay(
                                    verified, verifiedResult, inputs.PaidWorld,
                                    completedSession.Context, replayedContacts,
                                    retreatEvent.SerializeCanonical());
                                replayedBytes = verifiedRetreat.SerializeStateCanonical(replayedBytes);
                                replayedContacts = replayedContacts
                                    .AdvanceNativeCombatRetreatAfterAssault(verifiedRetreat);
                                if (completedSession.CurrentCombatSettlement is not null)
                                {
                                    if (replayEvents.ElementAtOrDefault(resultIndex + 2)
                                        is not CampaignNativeCombatSettlementV24 settlementEvent)
                                        throw new InvalidCampaignHistoryException(
                                            "Normal Combat World settlement is missing.");
                                    var verifiedSettlement = CampaignNativeCombatSettlementV24.Replay(
                                        verifiedResult, verifiedRetreat, inputs,
                                        completedSession.Context,
                                        settlementEvent.SerializeCanonical());
                                    var projected = CampaignNativeCombatSettledWorldV24.Project(
                                        verifiedResult, verifiedRetreat, inputs,
                                        completedSession.Context);
                                    settledWorldMatch = projected.World.Equals(
                                        completedSession.CurrentCombatSettledWorld);
                                    replayedBytes = verifiedSettlement.SerializeStateCanonical(
                                        replayedBytes);
                                    replayedContacts = replayedContacts.AdvanceNativeCombatSettlement(
                                        inputs.PaidWorld, projected.World, verifiedRetreat,
                                        verifiedSettlement, completedSession.Context);
                                    if (completedSession.CurrentCombatCustodyDisposition is not null)
                                    {
                                        if (replayEvents.ElementAtOrDefault(resultIndex + 3)
                                            is not CampaignNativeCombatCustodyDispositionV25 custodyEvent)
                                            throw new InvalidCampaignHistoryException(
                                                "Native Combat custody disposition is missing.");
                                        var custodyCut = CampaignNativeCombatCustodyCutV25.Project(
                                            verifiedSettlement, projected.World,
                                            completedSession.Context, replayedContacts);
                                        var verifiedCustody = CampaignNativeCombatCustodyDispositionV25.Replay(
                                            replayed, verifiedSettlement, projected.World,
                                            custodyCut, completedSession.Context,
                                            custodyEvent.SerializeCanonical());
                                        var custodyProjection = CampaignNativeCombatCustodyProjectionV25.Project(
                                            replayed, verifiedSettlement, projected.World,
                                            custodyCut, verifiedCustody.ChoiceKind,
                                            completedSession.Context);
                                        custodyMatch = custodyProjection.World.Equals(
                                                completedSession.CurrentCombatCustodyWorld)
                                            && CampaignNativeCombatCustodyProjectionV25.SerializeStateCanonical(
                                                custodyProjection.Custody).AsSpan().SequenceEqual(
                                                CampaignNativeCombatCustodyProjectionV25.SerializeStateCanonical(
                                                    completedSession.CurrentCombatCustodyState));
                                        replayedBytes = verifiedCustody.SerializeStateCanonical(
                                            replayedBytes);
                                        replayedContacts = replayedContacts.AdvanceNativeCombatCustodyDisposition(
                                            verifiedCustody);
                                        if (completedSession.CurrentCombatSuccessor is not null)
                                        {
                                            if (replayEvents.ElementAtOrDefault(resultIndex + 4)
                                                is not CampaignNativeCombatSuccessorV26 successorEvent)
                                                throw new InvalidCampaignHistoryException(
                                                    "Native Combat successor is missing.");
                                            var verifiedSuccessor = CampaignNativeCombatSuccessorV26.Replay(
                                                verified, verifiedResult,
                                                verifiedSettlement, verifiedCustody,
                                                custodyProjection.World,
                                                custodyProjection.Custody,
                                                completedSession.Context,
                                                successorEvent.SerializeCanonical());
                                            replayedBytes = CampaignNativeCombatSuccessorV26.Project(
                                                verified, verifiedResult,
                                                verifiedSettlement, verifiedCustody,
                                                custodyProjection.World,
                                                custodyProjection.Custody,
                                                completedSession.Context).SerializeCanonical(completedSession.Context);
                                            replayedContacts = replayedContacts.AdvanceNativeCombatSuccessor(
                                                verifiedSuccessor);
                                            if (completedSession.CurrentReserveReleaseEvents.Count > 0)
                                            {
                                                var projectedSuccessor = CampaignNativeCombatSuccessorV26.Project(
                                                    verified, verifiedResult, verifiedSettlement,
                                                    verifiedCustody, custodyProjection.World,
                                                    custodyProjection.Custody, completedSession.Context);
                                                var reserveState = CampaignNativeReserveReleaseStateV27.Initialize(
                                                    verifiedSuccessor, projectedSuccessor,
                                                    completedSession.Context);
                                                for (var index = 0;
                                                    index < completedSession.CurrentReserveReleaseEvents.Count;
                                                    index++)
                                                {
                                                    if (replayEvents.ElementAtOrDefault(resultIndex + 5 + index)
                                                        is not CampaignNativeReserveReleaseEventV27 reserveEvent)
                                                        throw new InvalidCampaignHistoryException(
                                                            "Native Reserve Release event is missing.");
                                                    reserveState = CampaignNativeReserveReleaseStateV27.Replay(
                                                        reserveState, completedSession.Context,
                                                        [(ReadOnlyMemory<byte>)reserveEvent.SerializeCanonical()]);
                                                    replayedContacts = replayedContacts.AdvanceNativeReserveRelease(
                                                        reserveEvent);
                                                }
                                                replayedBytes = reserveState.SerializeCanonical();
                                                if (completedSession.CurrentCycleControl is not null)
                                                {
                                                    if (replayEvents.ElementAtOrDefault(resultIndex + 5
                                                        + completedSession.CurrentReserveReleaseEvents.Count)
                                                        is not CampaignNativeCycleControlV28 controlEvent)
                                                        throw new InvalidCampaignHistoryException(
                                                            "Movement/Combat cycle control event is missing.");
                                                    var verifiedControl = CampaignNativeCycleControlV28.Replay(
                                                        reserveState, completedSession.Context,
                                                        replayedContacts, controlEvent.SerializeCanonical());
                                                    replayedContacts = replayedContacts.AdvanceNativeCycleControl(
                                                        verifiedControl);
                                                    replayedBytes = verifiedControl.SerializeCanonical();
                                                    if (completedSession.CurrentRepeatMove is not null)
                                                    {
                                                        if (replayEvents.ElementAtOrDefault(resultIndex + 6
                                                            + completedSession.CurrentReserveReleaseEvents.Count)
                                                            is not CampaignNativeRepeatMovementV29 repeatEvent)
                                                            throw new InvalidCampaignHistoryException(
                                                                "Repeated Movement event is missing.");
                                                        var verifiedMove = CampaignNativeRepeatMovementV29.Replay(
                                                            reserveState, verifiedControl,
                                                            completedSession.Context, replayedContacts,
                                                            repeatEvent.SerializeCanonical());
                                                        var repeatedWorld = verifiedMove.ProjectWorld(
                                                            reserveState, verifiedControl,
                                                            completedSession.Context, replayedContacts);
                                                        repeatWorldMatch = repeatedWorld.Equals(
                                                            completedSession.CurrentRepeatMoveWorld);
                                                        replayedContacts = replayedContacts.AdvanceNativeRepeatMove(
                                                            reserveState.World, repeatedWorld, verifiedMove,
                                                            completedSession.Context);
                                                        replayedBytes = verifiedMove.SerializeCanonical();
                                                        if (completedSession.CurrentRepeatBreakdown is not null)
                                                        {
                                                            if (replayEvents.ElementAtOrDefault(resultIndex + 7
                                                                + completedSession.CurrentReserveReleaseEvents.Count)
                                                                is not CampaignNativeRepeatBreakdownV30 breakdownEvent)
                                                                throw new InvalidCampaignHistoryException(
                                                                    "Repeated Breakdown event is missing.");
                                                            var verifiedBreakdown = CampaignNativeRepeatBreakdownV30.Replay(
                                                                reserveState, verifiedControl,
                                                                verifiedMove, repeatedWorld,
                                                                completedSession.Context, replayedContacts,
                                                                breakdownEvent.SerializeCanonical());
                                                            replayedContacts = replayedContacts
                                                                .AdvanceNativeRepeatBreakdown(verifiedBreakdown);
                                                            replayedBytes = verifiedBreakdown.SerializeCanonical();
                                                            if (completedSession.CurrentRepeatCombatSelection is not null)
                                                            {
                                                                if (replayEvents.ElementAtOrDefault(resultIndex + 8
                                                                    + completedSession.CurrentReserveReleaseEvents.Count)
                                                                    is not CampaignNativeRepeatCombatDecisionV31 selectionEvent)
                                                                    throw new InvalidCampaignHistoryException(
                                                                        "Repeated Combat selection is missing.");
                                                                var verifiedSelection = CampaignNativeRepeatCombatDecisionV31.Replay(
                                                                    reserveState, verifiedControl,
                                                                    verifiedMove, verifiedBreakdown,
                                                                    repeatedWorld, completedSession.Context,
                                                                    replayedContacts,
                                                                    selectionEvent.SerializeCanonical());
                                                                replayedContacts = replayedContacts
                                                                    .AdvanceNativeRepeatCombatSelection(verifiedSelection);
                                                                replayedBytes = verifiedSelection.SerializeCanonical();
                                                                if (completedSession.CurrentRepeatNoAttack is not null)
                                                                {
                                                                    if (replayEvents.ElementAtOrDefault(resultIndex + 9
                                                                        + completedSession.CurrentReserveReleaseEvents.Count)
                                                                        is not CampaignNativeRepeatCombatNoAttackV32 noAttackEvent)
                                                                        throw new InvalidCampaignHistoryException(
                                                                            "Repeated no-attack completion is missing.");
                                                                    var frame = CampaignCombatSelectionFrame.FromRepeat(
                                                                        replayed, verifiedSelection,
                                                                        repeatedWorld,
                                                                        verifiedResult.AfterRandomState);
                                                                    var verifiedNoAttack = CampaignNativeRepeatCombatNoAttackV32.Replay(
                                                                        verifiedSelection, frame,
                                                                        completedSession.Context,
                                                                        noAttackEvent.SerializeCanonical());
                                                                    replayedContacts = replayedContacts
                                                                        .AdvanceNativeRepeatCombatNoAttack(verifiedNoAttack);
                                                                    replayedBytes = verifiedNoAttack.SerializeCanonical();
                                                                    if (completedSession.CurrentPostRepeatEvents.Count > 0)
                                                                    {
                                                                        var laterEvents = replayEvents.Skip(resultIndex + 10
                                                                            + completedSession.CurrentReserveReleaseEvents.Count).ToArray();
                                                                        var later = CampaignNativePostRepeatRuntime.Replay(
                                                                            replayed, completedSession.Context,
                                                                            verifiedResult.AfterRandomState,
                                                                            verifiedSelection, verifiedNoAttack,
                                                                            repeatedWorld, replayedContacts,
                                                                            replayEvents.Take(resultIndex + 10
                                                                                + completedSession.CurrentReserveReleaseEvents.Count)
                                                                                .ToArray(),
                                                                            laterEvents);
                                                                        postWorldMatch = later.World.Equals(
                                                                            completedSession.CurrentPostRepeat!.World);
                                                                        replayedContacts = later.Contacts;
                                                                        replayedBytes = CampaignCurrentEventSerializer.Serialize(
                                                                            laterEvents[^1]);
                                                                    }
                                                                }
                                                                else if (completedSession.CurrentRepeatAssaultEvents.Count > 0)
                                                                {
                                                                    var assaultStart = resultIndex + 9
                                                                        + completedSession.CurrentReserveReleaseEvents.Count;
                                                                    var assaultEvents = replayEvents.Skip(assaultStart)
                                                                        .Take(completedSession.CurrentRepeatAssaultEvents.Count)
                                                                        .ToArray();
                                                                    var assault = CampaignNativeRepeatAssaultRuntime.Replay(
                                                                        replayed, verifiedSelection, repeatedWorld,
                                                                        verifiedResult.AfterRandomState,
                                                                        completedSession.Context, replayedContacts,
                                                                        replayEvents.Take(resultIndex + 9
                                                                            + completedSession.CurrentReserveReleaseEvents.Count)
                                                                            .ToArray(),
                                                                        assaultEvents);
                                                                    replayedContacts = assault.Contacts;
                                                                    replayedBytes = CampaignCurrentEventSerializer.Serialize(
                                                                        assaultEvents[^1]);
                                                                    if (completedSession.CurrentPostRepeatEvents.Count > 0)
                                                                    {
                                                                        var laterEvents = replayEvents.Skip(assaultStart
                                                                            + assaultEvents.Length).ToArray();
                                                                        if (assault.Release is null || assault.Control is null)
                                                                            throw new InvalidCampaignHistoryException(
                                                                                "Later cycle lacks repeated-assault control.");
                                                                        var later = CampaignNativePostRepeatRuntime
                                                                            .ReplayAfterSecondAssault(replayed,
                                                                                completedSession.Context,
                                                                                assault.Result?.AfterRandomState
                                                                                    ?? verifiedResult.AfterRandomState,
                                                                                assault.Release, assault.Control,
                                                                                replayedContacts,
                                                                                replayEvents.Take(assaultStart
                                                                                    + assaultEvents.Length).ToArray(),
                                                                                laterEvents);
                                                                        postWorldMatch = later.World.Equals(
                                                                            completedSession.CurrentPostRepeat!.World);
                                                                        replayedContacts = later.Contacts;
                                                                        replayedBytes = CampaignCurrentEventSerializer.Serialize(
                                                                            laterEvents[^1]);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (previousMove is not null) replayedBytes = moveState;
                    else replayedBytes = verifiedBarrage.SerializeStateCanonical(verified);
                }
                else replayedBytes = verified.SerializeCanonical();
                if (completedSession.CurrentReserveReleaseEvents.Count > 0
                    && (completedSession.CurrentNoAttackCompletion is not null
                        || completedSession.CurrentCombatCancelledAssault is not null
                        || completedSession.CurrentCombatRbaCancellation is not null))
                {
                    var initialRelease = completedSession.CurrentNoAttackCompletion
                        is { } noAttack
                        ? CampaignNativeReserveReleaseStateV27
                            .InitializeAfterInitialNoAttack(verified, noAttack,
                                completedSession.Context)
                        : completedSession.CurrentCombatRbaCancellation is { } initialRba
                            ? CampaignNativeReserveReleaseStateV27.InitializeAfterInitialRbaCancellation(
                                verified, initialRba, completedSession.CurrentCombatRetreatWorld ?? verified.PriorSnapshot.World,
                                completedSession.Context)
                        : CampaignNativeReserveReleaseStateV27
                            .InitializeAfterInitialCancellation(verified,
                                completedSession.CurrentCombatCancelledAssault!,
                                completedSession.CurrentCombatRetreatWorld
                                    ?? verified.PriorSnapshot.World,
                                completedSession.Context);
                    var reserveStart = decisionIndex + 1 + checked((int)(
                        initialRelease.StateVersion - verified.StateVersion));
                    for (var index = 0;
                         index < completedSession.CurrentReserveReleaseEvents.Count;
                         index++)
                    {
                        if (replayEvents.ElementAtOrDefault(reserveStart + index)
                            is not CampaignNativeReserveReleaseEventV27 reserveEvent)
                            throw new InvalidCampaignHistoryException(
                                "Initial Reserve Release event is missing.");
                        initialRelease = CampaignNativeReserveReleaseStateV27.Replay(
                            initialRelease, completedSession.Context,
                            [(ReadOnlyMemory<byte>)reserveEvent.SerializeCanonical()]);
                        replayedContacts = replayedContacts.AdvanceNativeReserveRelease(
                            reserveEvent);
                    }
                    replayedBytes = initialRelease.SerializeCanonical();
                    if (completedSession.CurrentCycleControl is not null)
                    {
                        if (replayEvents.ElementAtOrDefault(reserveStart
                                + completedSession.CurrentReserveReleaseEvents.Count)
                            is not CampaignNativeCycleControlV28 controlEvent)
                            throw new InvalidCampaignHistoryException(
                                "Initial cycle control event is missing.");
                        var control = CampaignNativeCycleControlV28.Replay(
                            initialRelease, completedSession.Context,
                            replayedContacts, controlEvent.SerializeCanonical());
                        replayedContacts = replayedContacts.AdvanceNativeCycleControl(control);
                        replayedBytes = control.SerializeCanonical();
                    }
                }
            }
            else
            {
                replayedBytes = CampaignCurrentSnapshotSerializer.Serialize(replayed, completedSession.Context);
            }
            var replayedHash = Hash(replayedBytes);
            var contactRecords = completedSession.CurrentContacts.Records;
            var contactsMatch = completedSession.CurrentContacts.AuthorityVersion
                == (completedSession.CurrentPostRepeatEvents.Count > 0
                    ? completedSession.CurrentPostRepeat!.Contacts.AuthorityVersion
                    : completedSession.CurrentRepeatAssaultEvents.Count > 0
                    ? completedSession.CurrentRepeatAssault!.Contacts.AuthorityVersion
                    : completedSession.CurrentRepeatNoAttack?.StateVersion
                    ?? completedSession.CurrentRepeatCombatSelection?.StateVersion
                    ?? completedSession.CurrentRepeatBreakdown?.StateVersion
                    ?? completedSession.CurrentRepeatMove?.StateVersion
                    ?? completedSession.CurrentCycleControl?.StateVersion
                    ?? completedSession.CurrentReserveRelease?.StateVersion
                    ?? completedSession.CurrentCombatSuccessor?.StateVersion
                    ?? completedSession.CurrentCombatCustodyDisposition?.StateVersion
                    ?? completedSession.CurrentCombatSettlement?.StateVersion
                    ?? completedSession.CurrentCombatPostResultRetreat?.StateVersion
                    ?? completedSession.CurrentCombatAssaultResult?.StateVersion
                    ?? completedSession.CurrentCombatCancelledAssault?.StateVersion
                    ?? completedSession.CurrentCombatAntiArmor?.StateVersion
                    ?? completedSession.CurrentCombatForceCancellation?.StateVersion
                    ?? completedSession.CurrentCombatForceCompletion?.StateVersion
                    ?? (completedSession.CurrentCombatForceSeals.Count > 0
                    ? completedSession.CurrentCombatForceSeals[^1].StateVersion
                    : completedSession.CurrentNoAttackCompletion?.StateVersion
                    ?? completedSession.CurrentCombatRbaCancellation?.StateVersion
                    ?? completedSession.CurrentCombatRetreatDecline?.StateVersion
                    ?? completedSession.CurrentCombatRetreatMove?.StateVersion
                    ?? completedSession.CurrentCombatBarrage?.StateVersion
                    ?? completedSession.CurrentCombatDecision?.StateVersion
                    ?? completedSession.CurrentSnapshot.StateVersion))
                && completedSession.CurrentContacts.AuthorityVersion == replayedContacts.AuthorityVersion
                && contactRecords.Count == replayedContacts.Records.Count
                && contactRecords.Zip(replayedContacts.Records).All(pair =>
                    pair.First.AsSpan().SequenceEqual(pair.Second));
            var failureReason = expectedBytes.AsSpan().SequenceEqual(replayedBytes)
                && contactsMatch && worldMatch && paidWorldMatch && settledWorldMatch
                && custodyMatch && repeatWorldMatch && postWorldMatch
                ? ExerciseReconstructionFailureReason.None
                : ExerciseReconstructionFailureReason.SnapshotMismatch;
            return new ExerciseReconstructionResult(
                failureReason,
                replayedBytes,
                eventStreamHash,
                expectedHash,
                replayedHash);
        }
        catch (Exception exception) when (exception is JsonException
            or InvalidCampaignHistoryException
            or ArgumentException
            or InvalidOperationException)
        {
            return new ExerciseReconstructionResult(
                ExerciseReconstructionFailureReason.InvalidHistory,
                null,
                eventStreamHash,
                expectedHash,
                null);
        }
    }

    private static string Hash(ReadOnlySpan<byte> value) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(value))}";

    private static string HashFramed(IEnumerable<byte[]> records)
    {
        using var stream = new MemoryStream();
        foreach (var record in records)
        {
            stream.Write(record);
            stream.WriteByte((byte)'\n');
        }
        return Hash(stream.ToArray());
    }
}
