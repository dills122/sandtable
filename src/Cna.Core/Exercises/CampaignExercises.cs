using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Exercises;

public static class CampaignExercises
{
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
        session is not null && session.Pregame is not null && session.SourceAuthority is null
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
    public static bool IsSourceItalianDivisionsGroupedAdmitted(ExerciseSession session) => session.SourceAuthority?.IsItalianDivisionsGrouped == true;
    public static ExerciseStepResult AdmitSourceItalianDivisionsGroupedWorld(ExerciseSession session)
    {
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
    public static bool CanPromoteSourceCwFreeGunsWorld(ExerciseSession session) => session.SourceAuthority?.CanPromoteCwFreeGunsWorld == true;
    public static ExerciseStepResult AdmitSourceCwFreeGuns(ExerciseSession session) => ApplySourceCwFreeGunsSystem(session, false);
    public static ExerciseStepResult PromoteSourceCwFreeGunsWorld(ExerciseSession session) => ApplySourceCwFreeGunsSystem(session, true);
    private static ExerciseStepResult ApplySourceCwFreeGunsSystem(ExerciseSession session, bool promote)
    {
        ArgumentNullException.ThrowIfNull(session);
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
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("positionId", CampaignSourceAuthorityState.SetupPositionId);
            writer.WriteString("status", "setup-incomplete");
            writer.WriteString("globalPriorityStatus", "incomplete");
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
        ArgumentNullException.ThrowIfNull(session);
        return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
    }

    internal static ExerciseStepResult ReplaySourceMalettiPromotion(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
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
            || session.SourceAuthority is not null || pregame.Die is null)
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
        if (session.Pregame is not { } pregame || session.SourceAuthority is not null)
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

    internal static CampaignContactOwnerMovementCut QueryContactOwnerMovementCut(
        ExerciseSession session, Cna.Core.Rules.LandSide side, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not null)
            throw new InvalidOperationException("Combat selection has already advanced this authority cut.");
        return CampaignContactOwnerMovementCut.Project(session.CurrentSnapshot, session.Context,
            session.CurrentContacts, side, actingElementId);
    }

    internal static CampaignContactCombatSelectionCut QueryContactOwnerCombatCut(
        ExerciseSession session, LandSide side, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not null)
            throw new InvalidOperationException("Combat selection has already advanced this authority cut.");
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
        if (session.SourceAuthority is { } sourceAuthority) return new ExerciseCheckpoint(sourceAuthority);
        if (session.Pregame is { } pregame) return new ExerciseCheckpoint(pregame);
        if (session.CurrentSecondBreakdown is { } secondBreakdown)
            return new ExerciseCheckpoint(secondBreakdown, session.CurrentSnapshot);
        if (session.CurrentSecondMovementCompletion is { } completed)
            return new ExerciseCheckpoint(completed, session.CurrentSnapshot);
        if (session.CurrentSecondMove is { } secondMove)
            return new ExerciseCheckpoint(secondMove, session.CurrentSnapshot);
        if (session.CurrentSecondReserve is { } secondReserve)
            return new ExerciseCheckpoint(secondReserve, session.CurrentSnapshot);
        if (session.CurrentPostCombatPass is { } phasePassed)
            return new ExerciseCheckpoint(phasePassed, session.CurrentSnapshot);
        if (session.CurrentTruckConvoyPass is { } passed)
            return new ExerciseCheckpoint(passed, session.CurrentSnapshot);
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
                    or CampaignSourceAuthorityState.CwFreeGunsWorldContractVersion or 34 or 35 or 36 or 37 or 38)
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
                or "source-gt1-supply-planned" or "source-commonwealth-supply-planned" or "source-axis-september-supply-changed"
                or "source-axis-force-supplement-admitted" or "source-autoblinda-choice"
                or "source-commonwealth-force-supplement-admitted" or "source-priority-motorization-admitted"
                or "source-priority-motorization-adjusted" or "source-axis-priority-motorization-admitted"
                or "source-axis-priority-motorization-changed" or "source-axis-minor-garrisons-admitted"
                or "source-battlefield-admitted" or "source-cairo-setup-admitted" or "source-cairo-unit-placed"
                or "source-cairo-world-admitted" or "source-maletti-world-promoted"
                or "source-independent-world-promoted" or "source-local-map-admitted" or "source-expanded-independent-world-promoted"
                or "source-attached-initial-loads-admitted" or "source-attached-initial-loads-changed"
                or "source-initial-wrecks-admitted" or "source-initial-wreck-placed"
                or "source-axis-free-setup-admitted" or "source-axis-free-unit-placed" or "source-axis-free-world-promoted"
                or "source-axis-corps-setup-admitted" or "source-axis-corps-unit-placed" or "source-axis-corps-world-promoted"
                or "source-maletti-hierarchy-admitted" or "source-maletti-hierarchy-changed" or "source-certified-maletti-world-promoted"
                or "source-matruh-formation-admitted" or "source-matruh-formation-changed" or "source-matruh-detached-world-promoted"
                or "source-gt2-correction-admitted" or "source-gt2-correction-changed"
                or "source-cw-priority-catalog-admitted" or "source-cw-priority-catalog-adjusted" or "source-cw-priority-world-promoted"
                or "source-compact-checkpoint-admitted" or "source-unattached-cargo-changed"
                or "source-libyan-grouped-world-admitted" or "source-libyan-disposition-confirmed"
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
        if (session.CurrentCombatDecision is not null
            || session.Context.ArtifactV6 is not { } artifact
            || !CampaignSnapshotV11Admission.IsValid(session.CurrentSnapshot, artifact,
                session.Context.Scenario)
            || session.CurrentContacts.AuthorityVersion != session.CurrentSnapshot.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.CampaignId != session.CurrentSnapshot.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != session.CurrentSnapshot.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignCurrentActionExecution.CurrentPositionId(session.CurrentSnapshot))
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
            var history = session.CurrentHistory.Add(selected.Decision);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                history, contacts, selected);
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
        var timeout = CreateSelectionTimeout(session, CampaignSelectionTimeoutEvidenceV38.NoTimingConfigurationHash,
            -1, -1, -1, null, "selection-unavailable", legacyInitiativeOwner);
        var evidence = timeout is CampaignNativeCombatSelectionTimeoutV38 initial
            ? initial.Evidence : ((CampaignNativeRepeatCombatSelectionTimeoutV38)timeout).Evidence;
        return (evidence.EntryHash, evidence.PriorStateVersion);
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
                next = new ExerciseSession(session.CurrentSnapshot, session.Context, session.CurrentHistory.Add(initial), contacts, selected);
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
                session.CurrentHistory.Add(completion), contacts, selected, completion);
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
                session.CurrentHistory.Add(barrage), contacts, selected, combatBarrage: barrage);
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
                combatRetreatMoves: session.CurrentCombatRetreatMoves);
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
                combatForceSeals: seals);
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
                combatForceCompletion: completion);
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
                    combatRbaCancellation: cancellation);
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
            return configuration is null
                ? ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.InvalidAuthority)
                : CancelRepeatedForceAssignment(session, configuration,
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
                combatForceCancellation: cancellation);
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
                session.Context, session.CurrentHistory);
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or InvalidCampaignHistoryException)
        {
            return false;
        }
    }

    private static ExerciseStepResult CancelRepeatedForceAssignment(
        ExerciseSession session, CampaignDecisionTimingConfigurationV1 configuration,
        long openedAtUnixMilliseconds, long deadlineUnixMilliseconds,
        long highWaterUnixMilliseconds, long? cancelledAtUnixMilliseconds,
        string reason)
    {
        var runtime = session.CurrentRepeatAssault!;
        var selected = runtime.Selection;
        if (configuration.RulesetHash != selected.PriorSnapshot.RulesetHash
            || openedAtUnixMilliseconds < 0
            || deadlineUnixMilliseconds - openedAtUnixMilliseconds
                != configuration.Budget("force-assignment")
            || session.CurrentContacts.AuthorityVersion
                != (runtime.Seals.Count == 0
                    ? runtime.Decline!.StateVersion : runtime.Seals[^1].StateVersion))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var cancellation = CampaignNativeCombatForceCancellationV19.Create(
                selected, runtime.Decline!, runtime.Seals,
                configuration.Hash, openedAtUnixMilliseconds,
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
                combatAntiArmor: antiArmor);
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
                combatCancelledAssault: cancelledAssault);
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
            var ammunition = CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(
                session.Context, session.CurrentHistory);
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
                combatPaidWorld: inputs.PaidWorld);
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
            var ammunition = CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(
                session.Context, session.CurrentHistory);
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
            combatPostResultRetreat: retreat);
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
            var priorHistory = session.CurrentHistory.Take(
                session.CurrentHistory.Count - 2);
            var ammunition = CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(
                session.Context, priorHistory);
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
                combatSettledWorld: projected.World);
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
            combatCustodyState: projection.Custody);
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
                combatSuccessor: successorEvent);
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
            var next = session.CurrentPostRepeat is not null
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
            var next = session.CurrentPostRepeat is not null
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
        if ((session.CurrentRepeatAssault?.Release is not null
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
            var next = session.CurrentPostRepeat is not null
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
            var next = session.CurrentPostRepeat is not null
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

    internal static object QueryPostCombatPassSource(ExerciseSession session) =>
        (object?)session.CurrentPostCombatPass ?? session.CurrentTruckConvoyPass
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
            var next = session.WithPostCombatPass(passed, contacts);
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
        if (session.CurrentTruckConvoyPass is not null)
            throw new InvalidOperationException("Truck Convoy has already been passed.");
        var release = QueryNativeReserveReleaseState(session);
        var control = session.CurrentRepeatAssault?.Release is not null
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
            var next = session.WithTruckConvoyPass(passed, contacts);
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
            if ((session.CurrentRepeatAssault?.Release is not null
                    ? session.CurrentRepeatAssault.Control
                    : session.CurrentPostRepeat is not null
                        ? session.CurrentPostRepeat.Control
                        : session.CurrentCycleControl) is not null
                || !release.Completed
                || audience != expectedAudience
                || release.StateVersion != session.CurrentContacts.AuthorityVersion)
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.InvalidAuthority);
            var control = CampaignNativeCycleControlV28.Create(release,
                session.Context, session.CurrentContacts, choice);
            var contacts = session.CurrentContacts.AdvanceNativeCycleControl(control);
            var next = session.CurrentPostRepeat is not null
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
        if ((session.CurrentPostRepeat?.Selection
                ?? session.CurrentRepeatCombatSelection) is not
                { Choice: "select-contact-assault" } selected
            || session.CurrentRepeatAssault is not { Barrage: null } runtime
            || session.CurrentContacts.AuthorityVersion != selected.StateVersion)
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
                combatRetreatMoves: session.CurrentCombatRetreatMoves.Append(movement));
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
        var successor = new ExerciseSession(execution.SuccessorSnapshot!, session.Context, history, contacts);
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
            var (isNativeSuccessor, predecessor) = cursor switch
            {
                { CurrentSecondBreakdown: not null } => (true, cursor.SecondBreakdownPredecessor),
                { CurrentSecondMovementCompletion: not null } => (true, cursor.SecondMovementCompletionPredecessor),
                { CurrentSecondMove: not null } => (true, cursor.SecondMovePredecessor),
                { CurrentSecondReserve: not null } => (true, cursor.SecondReservePredecessor),
                { CurrentPostCombatPass: not null } => (true, cursor.PostCombatPassPredecessor),
                { CurrentTruckConvoyPass: not null } => (true, cursor.TruckConvoyPassPredecessor),
                _ => (false, null),
            };
            if (!isNativeSuccessor) break;
            if (predecessor is null) return InvalidNativeHistory(completedSession);
            chain.Add(cursor);
            cursor = predecessor;
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
        var expected = session.SourceAuthority?.SerializeCheckpoint() ?? state.SerializeCheckpoint();
        var expectedHash = Hash(expected);
        try
        {
            var records = session.PregameHistory;
            if (session.CurrentContacts.CampaignId != state.Request.CampaignId
                || session.CurrentContacts.AuthorityVersion != (session.SourceAuthority?.StateVersion ?? state.StateVersion)
                || session.CurrentContacts.Records.Count != 0
                || records.Count != (session.SourceAuthority?.StateVersion ?? state.StateVersion))
                return new(ExerciseReconstructionFailureReason.InvalidHistory, null, null, expectedHash, null);
            var initial = state.Source.Session.InitialWorld.Initialization;
            var genesis = CampaignPregameSourceState.Create(state.Request, state.Source.Session.Content,
                new(initial.CapabilityPointsExpended, initial.CohesionLevel));
            var replayed = CampaignPregameSourceState.ReplayGenesis(state.Request, records[0], genesis.SerializeCheckpoint());
            CampaignSourceAuthorityState? authority = null;
            foreach (var record in records.Skip(1))
            {
                using var document = JsonDocument.Parse(record);
                var eventType = document.RootElement.GetProperty("eventType").GetString();
                if (eventType == "source-authority-started")
                {
                    if (authority is not null) throw new InvalidOperationException("Duplicate source authority transition.");
                    authority = CampaignSourceAuthorityState.Finish(replayed);
                    if (!record.AsSpan().SequenceEqual(authority.SerializeEvent()))
                        throw new InvalidOperationException("Source authority event differs from re-adjudication.");
                    continue;
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
                if (eventType is "source-attached-initial-loads-admitted" or "source-attached-initial-loads-changed")
                {
                    if (authority is null) throw new InvalidOperationException("Source setup has not started.");
                    var kind = eventType == "source-attached-initial-loads-changed" ? document.RootElement.GetProperty("kind").GetString()! : null;
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
            var actual = authority?.SerializeCheckpoint() ?? replayed.SerializeCheckpoint();
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
        if (completedSession.CurrentSecondBreakdown is { } secondBreakdown)
        {
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
        if (completedSession.CurrentSecondMovementCompletion is { } movementCompletion)
        {
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
        if (completedSession.CurrentSecondMove is { } secondMove)
        {
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
        if (completedSession.CurrentSecondReserve is { } secondReserve)
        {
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
        if (completedSession.CurrentPostCombatPass is { } phasePassed)
        {
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
        if (completedSession.CurrentTruckConvoyPass is { } passed)
        {
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
