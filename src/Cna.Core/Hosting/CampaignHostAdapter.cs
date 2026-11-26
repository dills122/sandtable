using System.Text.Json;
using System.Text.Json.Serialization;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Exercises.Sequencing;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Hosting;

/// <summary>Trusted, in-process boundary. None of these private records are browser DTOs.</summary>
public static class CampaignHostAdapter
{
    public static ExerciseStartResult Create(CampaignCreationRequest request) =>
        CampaignExercises.Begin(request);

    public static ExerciseStartResult Create(CampaignCreationRequest request, IContentPackV6Resolver resolver) =>
        CampaignExercises.Begin(request, resolver);

    public static CampaignSourceCreationResult Create(CampaignCreationRequest request,
        ContentSourceScheduledCombatPackArtifact content, CampaignSourceInitialization initialization) =>
        CampaignCreationExecution.Execute(request, content, initialization);

    public static ExerciseStartResult CreatePregameSource(CampaignCreationRequest request,
        ContentSourceScheduledCombatPackArtifact content, CampaignSourceInitialization initialization) =>
        CampaignExercises.Begin(request, content, initialization);

    public static CampaignPregameAxisDraft QueryPregameAxisSupplyOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience)
    {
        if (!CampaignExercises.IsPregameAxisSetup(session, audience))
            throw new InvalidOperationException("This seat does not own the pregame Axis setup.");
        return CampaignExercises.QueryPregameAxisDraft(session, audience);
    }

    public static CampaignPregameAxisDraft QueryPregameAxisSupplyFinishDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, audience);

    public static ExerciseStepResult FinishPregameAxisSupply(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.FinishPregameAxisSupply(session, submission);

    public static CampaignPregameAxisDraft QuerySourceAxisDumpDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceAxisDumpDraft(session, audience);

    public static ExerciseStepResult PlaceSourceAxisDump(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.PlaceSourceAxisDump(session, submission);

    public static CampaignPregameAxisDraft QuerySourceCommonwealthSupplyDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceCommonwealthSupplyDraft(session, audience);

    public static ExerciseStepResult PlaceSourceCommonwealthSupply(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.PlaceSourceCommonwealthSupply(session, submission);

    public static CampaignPregameAxisDraft QuerySourceDummyDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceDummyDraft(session, audience);

    public static ExerciseStepResult DecideSourceDummy(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.DecideSourceDummy(session, submission);

    public static CampaignPregameAxisDraft QuerySourceMotorizationDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceMotorizationDraft(session, audience);

    public static ExerciseStepResult AdjustSourceMotorization(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.AdjustSourceMotorization(session, submission);

    public static CampaignPregameAxisDraft QuerySourceUnattachedCargoDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceUnattachedCargoDraft(session, audience);

    public static ExerciseStepResult SubmitSourceUnattachedCargo(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.SubmitSourceUnattachedCargo(session, submission);

    public static CampaignPregameAxisDraft QuerySourceActionsDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceActionsDraft(session, audience);

    /// <summary>Checks a retained source-owner action against its issued visible choice and revision.</summary>
    public static bool MatchesIssuedSourceAction(long authorityVersion, byte[] visibleBytes, int ordinal,
        string actionId)
    {
        try
        {
            using var document = JsonDocument.Parse(visibleBytes);
            var choices = document.RootElement.GetProperty("choices");
            if (choices.ValueKind != JsonValueKind.Array || ordinal < 0 || ordinal >= choices.GetArrayLength())
                return false;
            var choice = choices[ordinal];
            if (choice.GetProperty("family").GetString() == "sixth-australian-baseline")
            {
                var turnChoice = choice.GetProperty("choice");
                if (turnChoice.GetProperty("rulingId").GetString() != "graziani.6-australian.initial-untrained-zero.v1")
                    return false;
                var vote = turnChoice.GetProperty("kind").GetString() switch
                {
                    "approve" => CampaignSourceTurnAction.ApproveSixthAustralianBaseline(authorityVersion),
                    "withdraw" => CampaignSourceTurnAction.WithdrawSixthAustralianBaseline(authorityVersion),
                    _ => null,
                };
                return vote?.ActionId == actionId;
            }
            if (choice.GetProperty("family").GetString() == "fourth-indian-opening-move")
            {
                var turnChoice = choice.GetProperty("choice");
                if (turnChoice.GetProperty("kind").GetString() != "move"
                    || turnChoice.GetProperty("groupId").GetString() != "4-indian-division"
                    || turnChoice.GetProperty("destination").GetString() != "d3715")
                    return false;
                return CampaignSourceTurnAction.MoveFourthIndianOpening(authorityVersion,
                    turnChoice.GetProperty("supplyId").GetString()!).ActionId == actionId;
            }
            if (choice.GetProperty("family").GetString() == "sixth-australian-training")
            {
                if (choice.GetProperty("choice").GetProperty("kind").GetString() != "interrupt")
                    return false;
                return CampaignSourceTurnAction.InterruptSixthAustralianTraining(authorityVersion).ActionId == actionId;
            }
            if (choice.GetProperty("family").GetString() == "axis-gt1-convoy-attack")
            {
                var turnChoice = choice.GetProperty("choice");
                if (turnChoice.GetProperty("kind").GetString() != "decline"
                    || turnChoice.GetProperty("operationStage").GetInt32() != 1)
                    return false;
                var laneAction = turnChoice.GetProperty("lane").GetInt32() switch
                {
                    2 => CampaignSourceTurnAction.DeclineGt1Lane2Attack(authorityVersion),
                    3 => CampaignSourceTurnAction.DeclineGt1Lane3Attack(authorityVersion),
                    6 => CampaignSourceTurnAction.DeclineGt1Lane6Attack(authorityVersion),
                    _ => null,
                };
                return laneAction?.ActionId == actionId;
            }
            if (choice.GetProperty("family").GetString() == "first-movement")
            {
                if (choice.GetProperty("choice").GetProperty("kind").GetString() != "finish")
                    return false;
                return CampaignSourceTurnAction.FinishFirstMovement(authorityVersion).ActionId == actionId;
            }
            if (choice.GetProperty("family").GetString() == "first-reserve-designation")
            {
                if (choice.GetProperty("choice").GetProperty("kind").GetString() != "decline")
                    return false;
                return CampaignSourceTurnAction.DeclineFirstReserves(authorityVersion).ActionId == actionId;
            }
            if (choice.GetProperty("family").GetString() == "voluntary-organization")
            {
                if (choice.GetProperty("choice").GetProperty("kind").GetString() != "decline")
                    return false;
                return CampaignSourceTurnAction.DeclineVoluntaryOrganization(authorityVersion).ActionId == actionId;
            }
            if (choice.GetProperty("family").GetString() == "initiative-declaration")
            {
                var turnChoice = choice.GetProperty("choice");
                if (turnChoice.GetProperty("operationStage").GetInt32() != 1)
                    return false;
                var declaredAction = turnChoice.GetProperty("kind").GetString() switch
                {
                    "act-first" => CampaignSourceTurnAction.ActFirst(authorityVersion),
                    "act-last" => CampaignSourceTurnAction.ActLast(authorityVersion),
                    _ => null,
                };
                return declaredAction?.ActionId == actionId;
            }
            if (choice.GetProperty("family").GetString() == "tactical-shipping")
            {
                if (choice.GetProperty("choice").GetProperty("kind").GetString() != "decline")
                    return false;
                return CampaignSourceTurnAction.DeclineTacticalShipping(authorityVersion).ActionId == actionId;
            }
            if (choice.GetProperty("family").GetString() == "cw-gt5-supply")
            {
                var turnChoice = choice.GetProperty("choice");
                if (turnChoice.GetProperty("kind").GetString() != "roll"
                    || turnChoice.GetProperty("targetGameTurn").GetInt32() != 5)
                    return false;
                return CampaignSourceTurnAction.RollGt5Supply(authorityVersion).ActionId == actionId;
            }
            if (choice.GetProperty("family").GetString() == "axis-gt3-supply")
            {
                var turnChoice = choice.GetProperty("choice");
                if (turnChoice.GetProperty("targetGameTurn").GetInt32() != 3)
                    return false;
                var kind = turnChoice.GetProperty("kind").GetString();
                var turnAction = kind switch
                {
                    "roll" => CampaignSourceTurnAction.RollGt3Supply(authorityVersion),
                    "finish" => CampaignSourceTurnAction.FinishGt3Supply(authorityVersion),
                    "add" or "remove" when turnChoice.GetProperty("delta").GetInt32() == (kind == "add" ? 1 : -1)
                        => CampaignSourceTurnAction.AdjustGt3Supply(authorityVersion,
                            turnChoice.GetProperty("lane").GetInt32(), turnChoice.GetProperty("operationStage").GetInt32(),
                            turnChoice.GetProperty("delta").GetInt32()),
                    _ => null,
                };
                return turnAction?.ActionId == actionId;
            }
            if (choice.GetProperty("family").GetString() == "polish-arrival")
            {
                var turnAction = new CampaignSourceTurnAction(authorityVersion,
                    choice.GetProperty("choice").GetProperty("destination").GetString()!);
                return turnAction.ActionId == actionId;
            }
            var action = new CampaignSourceSetupAction(authorityVersion,
                choice.GetProperty("family").GetString()!, "", choice.GetProperty("choice"));
            return action.ActionId == actionId;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or JsonException or KeyNotFoundException or FormatException)
        {
            return false;
        }
    }

    public static CampaignPregameAxisDraft QuerySourceAxisGt1SupplyDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceAxisGt1SupplyDraft(session, audience);

    public static ExerciseStepResult SubmitSourceAxisGt1Supply(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.SubmitSourceAxisGt1Supply(session, submission);

    public static CampaignPregameAxisDraft QuerySourceCommonwealthSupplyPlanDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(session, audience);

    public static ExerciseStepResult SubmitSourceCommonwealthSupplyPlan(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.SubmitSourceCommonwealthSupplyPlan(session, submission);

    public static ExerciseStepResult StartSourceTurn(ExerciseSession session) =>
        CampaignExercises.StartSourceTurn(session);

    internal static bool IsLegacySourceTurnStart(CampaignHostAcceptedStep step)
        => SourceTurnStartVersion(step) == CampaignSourceTurnAuthority.ContractVersion;

    internal static int SourceTurnStartVersion(CampaignHostAcceptedStep step)
    {
        try
        {
            var records = step.EventRecords;
            if (records.Count != 1) return 0;
            using var document = System.Text.Json.JsonDocument.Parse(records[0]);
            return document.RootElement.GetProperty("contractVersion").GetInt32();
        }
        catch (Exception error) when (error is System.Text.Json.JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            return 0;
        }
    }

    public static ExerciseStepResult ResolveSourceInitiative(ExerciseSession session) =>
        CampaignExercises.ResolveSourceInitiative(session);

    public static ExerciseStepResult CompleteSourceGt1Scheduling(ExerciseSession session) =>
        CampaignExercises.CompleteSourceGt1Scheduling(session);

    public static ExerciseStepResult CompleteSourceGt1Organization(ExerciseSession session) =>
        CampaignExercises.CompleteSourceGt1Organization(session);

    public static ExerciseStepResult CompleteSourceGt1StageOneArrivals(ExerciseSession session) =>
        CampaignExercises.CompleteSourceGt1StageOneArrivals(session);

    public static ExerciseStepResult CompleteSourceGt1StageOneFleet(ExerciseSession session) =>
        CampaignExercises.CompleteSourceGt1StageOneFleet(session);

    public static ExerciseStepResult CompleteSourceGt1FirstBreakdown(ExerciseSession session) =>
        CampaignExercises.CompleteSourceGt1FirstBreakdown(session);

    public static ExerciseStepResult ResolveSourceGt1Weather(ExerciseSession session) =>
        CampaignExercises.ResolveSourceGt1Weather(session);

    public static ExerciseStepResult AnnounceSourceGt1Lane2(ExerciseSession session) =>
        CampaignExercises.AnnounceSourceGt1Lane2(session);

    public static ExerciseStepResult AnnounceSourceGt1Lane3(ExerciseSession session) =>
        CampaignExercises.AnnounceSourceGt1Lane3(session);

    public static ExerciseStepResult CorrectSourceSeventhShell(ExerciseSession session) =>
        CampaignExercises.CorrectSourceSeventhShell(session);

    public static ExerciseStepResult ActivateSourceAxisMinorGarrisons(ExerciseSession session) =>
        CampaignExercises.ActivateSourceAxisMinorGarrisons(session);

    public static ExerciseStepResult ActivateSourceAxisGarrisonRemainder(ExerciseSession session) =>
        CampaignExercises.ActivateSourceAxisGarrisonRemainder(session);

    public static ExerciseStepResult ActivateSourceGiarabubMotorMachineGun(ExerciseSession session) =>
        CampaignExercises.ActivateSourceGiarabubMotorMachineGun(session);

    public static ExerciseStepResult ActivateSourceCwFrontline(ExerciseSession session) =>
        CampaignExercises.ActivateSourceCwFrontline(session);

    public static ExerciseStepResult AnnounceSourceGt1MultiLanes(ExerciseSession session) =>
        CampaignExercises.AnnounceSourceGt1MultiLanes(session);

    public static CampaignPregameAxisDraft QuerySourceCairoSetupDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceCairoSetupDraft(session, audience);

    public static ExerciseStepResult PlaceSourceCairoUnit(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.PlaceSourceCairoUnit(session, submission);

    public static CampaignPregameAxisDraft QuerySourceAxisPriorityDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceAxisPriorityDraft(session, audience);

    public static ExerciseStepResult SubmitSourceAxisPriority(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.SubmitSourceAxisPriority(session, submission);

    public static CampaignPregameAxisDraft QuerySourcePriorityMotorizationDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourcePriorityMotorizationDraft(session, audience);

    public static ExerciseStepResult AdjustSourcePriorityMotorization(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.AdjustSourcePriorityMotorization(session, submission);

    public static CampaignPregameAxisDraft QuerySourceAttachedInitialLoadsDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceAttachedInitialLoadsDraft(session, audience);

    public static ExerciseStepResult SubmitSourceAttachedInitialLoads(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.SubmitSourceAttachedInitialLoads(session, submission);

    public static CampaignPregameAxisDraft QuerySourceInitialWrecksDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceInitialWrecksDraft(session, audience);

    public static ExerciseStepResult PlaceSourceInitialWreck(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.PlaceSourceInitialWreck(session, submission);

    public static CampaignPregameAxisDraft QuerySourceAxisFreeSetupDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceAxisFreeSetupDraft(session, audience);

    public static ExerciseStepResult PlaceSourceAxisFreeUnit(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.PlaceSourceAxisFreeUnit(session, submission);

    public static CampaignPregameAxisDraft QuerySourceAxisCorpsSetupDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceAxisCorpsSetupDraft(session, audience);

    public static ExerciseStepResult PlaceSourceAxisCorpsUnit(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.PlaceSourceAxisCorpsUnit(session, submission);

    public static CampaignPregameAxisDraft QuerySourceMalettiHierarchyDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceMalettiHierarchyDraft(session, audience);

    public static ExerciseStepResult SubmitSourceMalettiHierarchy(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.SubmitSourceMalettiHierarchy(session, submission);

    public static CampaignPregameAxisDraft QuerySourceGt2CorrectionDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceGt2CorrectionDraft(session, audience);

    public static ExerciseStepResult SubmitSourceGt2Correction(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.SubmitSourceGt2Correction(session, submission);

    public static CampaignPregameAxisDraft QuerySourceCwPriorityCatalogDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceCwPriorityCatalogDraft(session, audience);

    public static ExerciseStepResult SubmitSourceCwPriorityCatalog(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.SubmitSourceCwPriorityCatalog(session, submission);

    public static CampaignPregameAxisDraft QuerySourceMatruhFormationDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceMatruhFormationDraft(session, audience);

    public static ExerciseStepResult SubmitSourceMatruhFormation(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.SubmitSourceMatruhFormation(session, submission);

    public static CampaignPregameAxisDraft QuerySourceCwFreeGunsDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceCwFreeGunsDraft(session, audience);

    public static ExerciseStepResult PlaceSourceCwFreeGun(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.PlaceSourceCwFreeGun(session, submission);

    public static CampaignPregameAxisDraft QuerySourceAutoblindaDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CampaignExercises.QuerySourceAutoblindaDraft(session, audience);

    public static ExerciseStepResult SubmitSourceAutoblinda(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.SubmitSourceAutoblinda(session, submission);

    public static CampaignActionSubmission SourceAxisSupplementCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source supplement is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceCommonwealthSupplementCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source supplement is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourcePriorityMotorizationCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceAttachedInitialLoadsCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceInitialWrecksCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceAxisFreeSetupCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceAxisCorpsSetupCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceMalettiHierarchyCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceGt2CorrectionCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceCwPriorityCatalogCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceCompactCheckpointCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceFourthIndianWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceMatruhGroupedWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceMalettiGroupedWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceLibyanGroupedWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceItalianDivisionsGroupedWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceSeventhArmoredGroupedWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceSixteenthBrigadeGroupedWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceCatanzaroGroupedWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceTurnSystemCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source turn event is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceMatruhFormationCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceCwFreeGunsCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceAxisPriorityMotorizationCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source policy migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceAxisMinorGarrisonsCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceBattlefieldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source battlefield migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceCairoSetupCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted Cairo setup migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceCairoWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted Cairo World migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceMalettiCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted Maletti World promotion is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceIndependentCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted independent force promotion is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceExpandedIndependentCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted expanded independent force promotion is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceAxisFreeWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted Axis free-world promotion is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceAxisCorpsWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted Axis corps-world promotion is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceCertifiedMalettiWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted certified Maletti world promotion is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceMatruhDetachedWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted Matruh detached world promotion is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceCwPriorityWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted Commonwealth priority world promotion is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceCwFreeGunsWorldCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted Commonwealth priority world promotion is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission SourceLocalMapCommand(ExerciseSession predecessor,
        ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null)
            throw new ArgumentException("An accepted source map migration is required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence.Receipt.ActionId);
    }

    /// <summary>
    /// Trusted successor input for the normal campaign host. The visible bytes contain no
    /// private authority version or Core action ID; Host must durably bind an authenticated
    /// owner opening and its private action map before any client can receive them.
    /// </summary>
    public static CampaignHostContactOwnerDraft QueryContactOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            throw new ArgumentOutOfRangeException(nameof(audience));
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var cut = CampaignExercises.QueryContactOwnerMovementCut(session, side, actingElementId);
        return new CampaignHostContactOwnerDraft(cut.AuthorityVersion,
            CampaignContactOwnerVisibleCutV1.SerializeCanonical(cut), cut.MovementOffers);
    }

    /// <summary>Trusted Combat owner draft; only visible contact values enter outward bytes.</summary>
    public static CampaignHostContactCombatOwnerDraft QueryContactCombatOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            throw new ArgumentOutOfRangeException(nameof(audience));
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var cut = CampaignExercises.QueryContactOwnerCombatCut(session, side, actingElementId);
        return new CampaignHostContactCombatOwnerDraft(cut.AuthorityVersion,
            CampaignContactCombatOwnerVisibleCutV2.SerializeCanonical(cut),
            cut.AssaultOffers.OrderBy(value => value.ContactHandle, StringComparer.Ordinal)
                .ThenBy(value => value.ApparentLocationId, StringComparer.Ordinal)
                .Select(value => value.ActionId)
                .Concat(cut.FinishOffer is { } finish ? [finish.ActionId] : [])
                .ToArray());
    }

    /// <summary>Trusted defender draft after accepted Barrage; action IDs stay private.</summary>
    public static CampaignHostContactRbaOwnerDraft QueryContactRbaOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string defenderElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cut = CampaignExercises.QueryContactRbaCut(session);
        var defenderAudience = cut.DefenderSide == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        if (audience != defenderAudience || defenderElementId != cut.DefenderElementId)
            throw new InvalidOperationException("Only the bound defender may receive this retreat cut.");
        return new CampaignHostContactRbaOwnerDraft(cut.AuthorityVersion,
            CampaignContactRetreatBeforeAssaultOwnerVisibleCutV1.SerializeCanonical(cut),
            cut.MoveOffers.OrderBy(value => value.ElementId, StringComparer.Ordinal)
                .ThenBy(value => value.DestinationLocationId, StringComparer.Ordinal)
                .Select(value => value.ActionId).Append(cut.DeclineOffer.ActionId).ToArray());
    }

    /// <summary>
    /// Trusted decline-only defender draft (Task 020D; T020-Q7 option b, owner ruling 2026-11-20):
    /// the bounded <c>CMB-POL-001</c> profile adopts decline-only RBA, so the Host's RBA opening
    /// publishes <see cref="CampaignContactRbaDeclineOwnerVisibleCutV1"/> bytes and maps exactly one
    /// action, the decline. Core still derives move offers; they are never published or mapped. The
    /// bytes equal the Core audience surface's <c>rba-decline</c> decision visible bytes.
    /// </summary>
    public static CampaignHostContactRbaOwnerDraft QueryContactRbaDeclineOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string defenderElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cut = CampaignExercises.QueryContactRbaCut(session);
        var defenderAudience = cut.DefenderSide == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        if (audience != defenderAudience || defenderElementId != cut.DefenderElementId)
            throw new InvalidOperationException("Only the bound defender may receive this retreat cut.");
        return new CampaignHostContactRbaOwnerDraft(cut.AuthorityVersion,
            CampaignContactRbaDeclineOwnerVisibleCutV1.SerializeCanonical(cut),
            [cut.DeclineOffer.ActionId]);
    }

    /// <summary>Trusted post-result defender draft; Core action IDs stay in Host custody.</summary>
    public static CampaignHostContactPostAssaultRetreatOwnerDraft
        QueryContactPostAssaultRetreatOwnerDraft(ExerciseSession session,
            CampaignActionAudience audience, string defenderElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cut = CampaignExercises.QueryContactPostResultRetreatCut(session);
        var defenderAudience = cut.DefenderSide == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        if (audience != defenderAudience || defenderElementId != cut.DefenderElementId)
            throw new InvalidOperationException("Only the bound defender may receive this retreat cut.");
        return new CampaignHostContactPostAssaultRetreatOwnerDraft(cut.AuthorityVersion,
            CampaignContactRetreatAfterAssaultOwnerVisibleCutV1.SerializeCanonical(cut),
            cut.MoveOffers.OrderBy(value => value.DestinationLocationId,
                StringComparer.Ordinal).Select(value => value.ActionId)
                .Append(cut.RefuseOffer.ActionId).ToArray());
    }

    /// <summary>Trusted captor draft; private authority and Core action IDs stay in Host custody.</summary>
    public static CampaignHostContactCustodyOwnerDraft QueryContactCustodyOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience,
        string captorElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cut = CampaignExercises.QueryNativeCombatCustodyCut(session)
            ?? throw new InvalidOperationException("No captive lot awaits an owner.");
        var sideId = session.Context.ArtifactV6!.Definition.LegacyDefinition.Elements
            .Single(value => value.ElementId == cut.CaptorElementId).SideId;
        var captorAudience = sideId == "axis" ? CampaignActionAudience.Axis
            : sideId == "commonwealth" ? CampaignActionAudience.Commonwealth
            : throw new InvalidOperationException("Unknown captor side.");
        if (audience != captorAudience || captorElementId != cut.CaptorElementId)
            throw new InvalidOperationException("Only the bound captor may receive this cut.");
        return new CampaignHostContactCustodyOwnerDraft(cut.AuthorityVersion,
            CampaignContactCustodyOwnerVisibleCutV1.SerializeCanonical(cut),
            cut.Choices.Select(value => value.ActionId).ToArray());
    }

    /// <summary>Trusted Force Assignment draft for one authenticated owner.</summary>
    public static CampaignHostContactForceOwnerDraft QueryContactForceOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            throw new ArgumentOutOfRangeException(nameof(audience));
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var cut = CampaignExercises.QueryContactForceAssignmentCut(session, side);
        if (cut.SealOffer.ElementId != elementId)
            throw new InvalidOperationException("Only the bound owner may receive this seal cut.");
        return new CampaignHostContactForceOwnerDraft(
            session.CurrentContacts.AuthorityVersion,
            CampaignContactForceAssignmentOwnerVisibleCutV1.SerializeCanonical(cut),
            [cut.SealOffer.ActionId]);
    }

    /// <summary>Trusted seat routing for the pending defender decision.</summary>
    public static string QueryContactRbaDefenderId(ExerciseSession session,
        CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cut = CampaignExercises.QueryContactRbaCut(session);
        var defenderAudience = cut.DefenderSide == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        if (audience != defenderAudience)
            throw new InvalidOperationException("This seat does not own the pending defender decision.");
        return cut.DefenderElementId;
    }

    /// <summary>Trusted canonical contact evidence for durable campaign custody.</summary>
    public static IReadOnlyList<byte[]> QueryContactRecords(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.CurrentContacts.Records;
    }

    /// <summary>Side-authorized source contacts for an active independent counter.</summary>
    public static byte[] QuerySourceCounterContacts(ExerciseSession session,
        CampaignActionAudience audience, string counterId) =>
        CampaignExercises.QuerySourceCounterContacts(session, audience, counterId);

    public static CampaignHostAudienceCut QueryAudience(
        ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not null)
            return CampaignHostAudienceCut.Rejected(CampaignHostQueryRejectionReason.InvalidState);
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return CampaignHostAudienceCut.Rejected(CampaignHostQueryRejectionReason.InvalidAudience);

        var handle = new CampaignAuthorityHandle(session.CurrentSnapshot, session.Context);
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var observation = CampaignObservations.Query(handle, side);
        var legal = CampaignLegalActions.Query(handle, audience);
        if (!observation.IsProjected || !legal.IsSuccessful)
            return CampaignHostAudienceCut.Rejected(CampaignHostQueryRejectionReason.InvalidState);

        var view = observation.Observation!;
        var actions = legal.ActionSet!;
        if (view.CampaignId != actions.CampaignId
            || view.StateVersion != actions.StateVersion
            || view.RulesetHash != actions.RulesetHash
            || view.Position.PositionId != actions.PositionId)
            return CampaignHostAudienceCut.Rejected(CampaignHostQueryRejectionReason.InvalidState);

        return CampaignHostAudienceCut.Projected(view, actions);
    }

    /// <summary>One typed, trusted side cut. The representation keys and Core action IDs
    /// remain private inputs to the host's continuity and action trackers.</summary>
    public static CampaignHostPrivateSideCut? QueryPrivateSideCut(
        ExerciseSession session, CampaignActionAudience audience)
    {
        var cut = QueryAudience(session, audience);
        if (!cut.IsProjected) return null;
        var view = cut.Observation!;
        var actions = cut.LegalActions!;
        var decisionKind = view.DecisionState switch
        {
            CampaignObservationV7NormalDecisionState => "normal",
            CampaignObservationV7BreakdownWaitingDecisionState => "breakdown-waiting",
            CampaignObservationV7PhasingWaitingDecisionState => "phasing-waiting",
            CampaignObservationV7ReactingDecisionState => "reacting",
            _ => null,
        };
        if (decisionKind is null) return null;
        return new CampaignHostPrivateSideCut(
            view.CampaignId, view.StateVersion, view.RulesetHash, view.ScenarioId,
            view.Position.PositionId, view.Position.GameTurn, view.Position.PhaseId,
            view.Position.StageId, audience,
            view.Locations.Select(location => new CampaignHostPrivateLocation(
                location.LocationId, location.TerrainId)).ToArray(),
            view.OwnElements.Select(element => new CampaignHostPrivateOwnElement(
                element.ElementId, element.ParentFormationId, element.CurrentLocationId,
                element.ReserveStatus.ToString())).ToArray(),
            view.ApparentOpposingPresences.Select(presence => new CampaignHostPrivateContact(
                presence.RepresentationId, presence.CurrentLocationId, presence.ExertsZoc)).ToArray(),
            actions.Candidates.Select(candidate => ProjectPrivateAction(view, candidate)).ToArray(),
            decisionKind);
    }

    private static CampaignHostPrivateAction ProjectPrivateAction(
        CampaignObservationV7 view, CampaignActionCandidate candidate)
    {
        var elementId = candidate switch
        {
            DesignateReserveAction reserve => reserve.ElementId,
            MoveElementAction move => move.ElementId,
            StopElementMovementAction stop
                when view.DecisionState is CampaignObservationV7NormalDecisionState
                { ActiveMovement: { } active }
                    && active.RouteId == stop.RouteId => active.ElementId,
            _ => null,
        };
        var targetLocationId = candidate switch
        {
            MoveElementAction move => move.DestinationLocationId,
            MoveReactingElementAction move => move.DestinationLocationId,
            _ => null,
        };
        var opportunityId = candidate switch
        {
            MoveReactingElementAction move => move.OpportunityId,
            CompleteReactionParticipantAction complete => complete.OpportunityId,
            _ => null,
        };
        return new CampaignHostPrivateAction(candidate.ActionId, candidate.Kind,
            elementId, targetLocationId, opportunityId);
    }

    public static CampaignLegalActionQueryResult QuerySystem(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return CampaignExercises.Query(session, CampaignActionAudience.System);
    }

    public static ExerciseStepResult Submit(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.Submit(session, submission);

    /// <summary>Trusted Core submission against the contact-derived owner cut, never V7 actions.</summary>
    public static ExerciseStepResult SubmitContactOwnerMovement(
        ExerciseSession session, CampaignActionSubmission submission, string actingElementId) =>
        CampaignExercises.SubmitContactOwnerMovement(session, submission, actingElementId);

    /// <summary>Trusted native Combat selection against the same contact authority cut.</summary>
    public static ExerciseStepResult SubmitContactOwnerCombatSelection(
        ExerciseSession session, CampaignActionSubmission submission, string actingElementId) =>
        CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, actingElementId);

    /// <summary>Trusted explicit defender decline at the retained RBA contact cut.</summary>
    public static ExerciseStepResult SubmitContactDefenderRbaDecline(
        ExerciseSession session, CampaignActionSubmission submission, string defenderElementId) =>
        CampaignExercises.SubmitContactDefenderRbaDecline(session, submission, defenderElementId);

    public static ExerciseStepResult SubmitContactDefenderRbaMove(
        ExerciseSession session, CampaignActionSubmission submission, string defenderElementId) =>
        CampaignExercises.SubmitContactDefenderRbaMove(session, submission, defenderElementId);

    public static ExerciseStepResult SubmitContactOwnerForceSeal(
        ExerciseSession session, CampaignActionSubmission submission, string elementId) =>
        CampaignExercises.SubmitContactOwnerForceSeal(session, submission, elementId);

    /// <summary>System-only structural continuation of an accepted voluntary finish.</summary>
    public static ExerciseStepResult CompleteNativeCombatNoAttack(ExerciseSession session) =>
        session.Pending switch
        {
            StageEntryCombatSelectionPending => CampaignExercises.CompleteStageEntryCombatNoAttack(session),
            ThirdStageCombatSelectionPending => CampaignExercises.CompleteThirdStageCombatNoAttack(session),
            _ => CampaignExercises.CompleteNativeCombatNoAttack(session),
        };

    /// <summary>
    /// #394 extra-2/3: second-side analog of <see cref="CompleteNativeCombatNoAttack"/>.
    /// Deliberately does not auto-advance into Reserve Release -- see
    /// <see cref="CampaignExercises.CompleteSecondCombatNoAttack"/>'s remarks.
    /// </summary>
    public static ExerciseStepResult CompleteSecondCombatNoAttack(ExerciseSession session) =>
        session.Pending switch
        {
            StageEntrySecondCombatSelectionPending => CampaignExercises.CompleteStageEntrySecondCombatNoAttack(session),
            ThirdStageSecondCombatSelectionPending => CampaignExercises.CompleteThirdStageSecondCombatNoAttack(session),
            _ => CampaignExercises.CompleteSecondCombatNoAttack(session),
        };

    public static ExerciseStepResult CompleteNativeRepeatCombatNoAttack(
        ExerciseSession session) =>
        CampaignExercises.CompleteNativeRepeatCombatNoAttack(session);

    /// <summary>System-only no-effect Barrage for a selected infantry assault.</summary>
    public static ExerciseStepResult CompleteNativeCombatBarrage(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatBarrage(session);

    public static ExerciseStepResult CompleteNativeCombatForceAssignment(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatForceAssignment(session);

    public static ExerciseStepResult CancelNativeCombatForceAssignment(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.CancelNativeCombatForceAssignment(session, configuration,
            openedAtUnixMilliseconds, deadlineUnixMilliseconds,
            highWaterUnixMilliseconds, cancelledAtUnixMilliseconds, reason);

    public static string NoTimingForceCancellationHash =>
        CampaignNativeCombatForceCancellationV19.NoTimingConfigurationHash;

    public static ExerciseStepResult CancelNativeCombatRba(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.CancelNativeCombatRba(session, configuration,
            openedAtUnixMilliseconds, deadlineUnixMilliseconds,
            highWaterUnixMilliseconds, cancelledAtUnixMilliseconds, reason);

    public static string NoTimingRbaCancellationHash =>
        CampaignNativeCombatRbaCancellationV33.NoTimingConfigurationHash;

    private static CampaignNativeRepeatAssaultRuntime? CurrentOccurrenceAssault(ExerciseSession session) =>
        session.Pending switch
        {
            SecondAssaultPending => session.CurrentSecondAssault,
            StageEntryAssaultPending => session.CurrentStageEntryAssault,
            StageEntrySecondAssaultPending => session.CurrentStageEntrySecondAssault,
            ThirdStageAssaultPending => session.CurrentThirdStageAssault,
            ThirdStageSecondAssaultPending => session.CurrentThirdStageSecondAssault,
            null => session.CurrentRepeatAssault,
            _ => null,
        };

    public static (string DecisionActionId, string BarrageEventHash)
        QueryNativeCombatRbaRoundBinding(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (CurrentOccurrenceAssault(session) is
            { Barrage: { } repeatedBarrage, Decline: null, RbaCancellation: null } repeated
            && checkpoint.StateVersion == ((repeated.RetreatMoves.Count > 0 ? repeated.RetreatMoves[^1].StateVersion : (long?)null) ?? repeatedBarrage.StateVersion))
            return (repeated.Selection.Decision.ActionId,
                "sha256:" + Convert.ToHexStringLower(
                    System.Security.Cryptography.SHA256.HashData(repeatedBarrage.SerializeCanonical())));
        if (session.Pending is not null || CurrentOccurrenceAssault(session) is not null
            || session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatBarrage is not { } barrage
            || session.CurrentCombatRetreatDecline is not null
            || session.CurrentCombatRbaCancellation is not null
            || checkpoint.StateVersion != (session.CurrentCombatRetreatMove?.StateVersion ?? barrage.StateVersion))
            throw new InvalidOperationException("No collecting RBA round is available.");
        return (selected.Decision.ActionId,
            "sha256:" + Convert.ToHexStringLower(
                System.Security.Cryptography.SHA256.HashData(barrage.SerializeCanonical())));
    }

    public static bool CanCompleteNativeCombatForceAssignment(ExerciseSession session) =>
        CampaignExercises.CanCompleteNativeCombatForceAssignment(session);

    public static bool IsInitialNativeCombatForceRound(ExerciseSession session) =>
        session.Pending is null && CurrentOccurrenceAssault(session) is null && session.CurrentPostRepeat is null
        && session.CurrentCombatRetreatDecline is not null
        && session.CurrentCombatForceCompletion is null
        && session.CurrentCombatForceCancellation is null;

    /// <summary>
    /// #476: a repeated-cycle Force round about to open (decline accepted, no seal, not yet completed
    /// or cancelled). The Host applies the same pre-opening force-unavailable check as for
    /// <see cref="IsInitialNativeCombatForceRound"/>.
    /// </summary>
    public static bool IsRepeatedNativeCombatForceRound(ExerciseSession session) =>
        CurrentOccurrenceAssault(session) is
        {
            Decline: not null,
            ForceCompletion: null,
            ForceCancellation: null,
            Seals.Count: 0,
        };

    public static ExerciseStepResult CompleteNativeCombatAntiArmor(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatAntiArmor(session);

    public static ExerciseStepResult CompleteNativeCombatCancelledAssault(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatCancelledAssault(session);

    public static ExerciseStepResult ResolveNativeCombatAssault(ExerciseSession session) =>
        CampaignExercises.ResolveNativeCombatAssault(session);

    public static ExerciseStepResult SubmitContactPostAssaultRetreat(
        ExerciseSession session, CampaignActionSubmission submission,
        string defenderElementId) => CampaignExercises.SubmitContactPostResultRetreat(
            session, submission, defenderElementId);

    public static ExerciseStepResult CompleteNoPostAssaultRetreat(ExerciseSession session) =>
        CampaignExercises.CompleteNoPostResultRetreat(session);

    public static bool IsNativeCombatNoPostResultRetreat(ExerciseSession session) =>
        (session.Pending is null && session.CurrentCombatAssaultResult is { Facts.RequiredRetreat: 0 }
            && session.CurrentCombatPostResultRetreat is null)
        || CurrentOccurrenceAssault(session) is
        {
            Result.Facts.RequiredRetreat: 0, PostResultRetreat: null
        };

    public static bool IsNativeCombatForcePrepared(ExerciseSession session) =>
        (session.Pending is null && session.CurrentCombatForceSeals.Count == 2
            && session.CurrentCombatForceCompletion is null)
        || CurrentOccurrenceAssault(session) is
        {
            Seals.Count: 2,
            ForceCompletion: null
        };

    // Pending names identify the current occurrence, not simply a retained field.
    // The pre-#394 first-side path has no Pending arm; its decision must still
    // match live contact authority. In all other occurrences, only the
    // selection arm owns an accepted selection awaiting continuation.
    private static string? CurrentFirstSideCombatChoice(ExerciseSession session) =>
        session.Pending switch
        {
            StageEntryCombatSelectionPending => session.CurrentStageEntryCombatDecision!.Decision.Choice,
            ThirdStageCombatSelectionPending => session.CurrentThirdStageCombatDecision!.Decision.Choice,
            null when session.CurrentCombatDecision is { } selection
                && selection.StateVersion == session.CurrentContacts.AuthorityVersion => selection.Decision.Choice,
            _ => null,
        };

    private static string? CurrentSecondSideCombatChoice(ExerciseSession session) =>
        session.Pending switch
        {
            SecondCombatSelectionPending => session.CurrentSecondCombatDecision!.Decision.Choice,
            StageEntrySecondCombatSelectionPending => session.CurrentStageEntrySecondCombatDecision!.Decision.Choice,
            ThirdStageSecondCombatSelectionPending => session.CurrentThirdStageSecondCombatDecision!.Decision.Choice,
            _ => null,
        };

    public static bool IsNativeCombatVoluntaryFinish(ExerciseSession session) =>
        CurrentFirstSideCombatChoice(session) == "finish-combat-without-attack";

    public static bool IsNativeSecondCombatVoluntaryFinish(ExerciseSession session) =>
        CurrentSecondSideCombatChoice(session) == "finish-combat-without-attack";

    public static bool IsNativeSecondCombatSelectedAssault(ExerciseSession session) =>
        CurrentSecondSideCombatChoice(session) == "select-contact-assault";

    /// <summary>
    /// W4: the occurrence slot a Combat Selection (or its V48 timeout) currently belongs to,
    /// derived only from the live <see cref="ExerciseSession.Pending"/> subtype and the
    /// checkpoint position, never from retained Stage 1 fields. Null when no native
    /// first/second-side selection occurrence is current (for example a repeat cycle).
    /// </summary>
    public static CampaignCombatSelectionSlot? QueryCombatSelectionSlot(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.Pending switch
        {
            SecondBreakdownPending or SecondCombatSelectionPending => CampaignCombatSelectionSlot.StageOneSecond,
            StageEntryBreakdownPending or StageEntryCombatSelectionPending => CampaignCombatSelectionSlot.StageTwoFirst,
            StageEntrySecondBreakdownPending or StageEntrySecondCombatSelectionPending => CampaignCombatSelectionSlot.StageTwoSecond,
            ThirdStageBreakdownPending or ThirdStageCombatSelectionPending => CampaignCombatSelectionSlot.StageThreeFirst,
            ThirdStageSecondBreakdownPending or ThirdStageSecondCombatSelectionPending => CampaignCombatSelectionSlot.StageThreeSecond,
            null when CampaignExercises.QueryCheckpoint(session).PositionId.StartsWith(
                "land.position.operation-1.", StringComparison.Ordinal) => CampaignCombatSelectionSlot.StageOneFirst,
            _ => null,
        };
    }

    public static bool IsSecondSideCombatPending(ExerciseSession session) =>
        session.Pending is SecondBreakdownPending or SecondCombatSelectionPending
            or StageEntrySecondBreakdownPending or StageEntrySecondCombatSelectionPending
            or ThirdStageSecondBreakdownPending or ThirdStageSecondCombatSelectionPending;

    public static bool IsNativeCombatSelectedAssault(ExerciseSession session) =>
        CurrentFirstSideCombatChoice(session) == "select-contact-assault";

    // Historical selection accessor retained for compatibility. Current-round routing
    // must use IsNativeRepeatCombatNoAttackPending instead.
    public static bool IsNativeRepeatCombatVoluntaryFinish(ExerciseSession session) =>
        (session.CurrentPostRepeat?.Selection
            ?? session.CurrentRepeatCombatSelection)?.Choice
            == "finish-combat-without-attack";

    // #645: CurrentRepeatCombatSelection (and CurrentPostRepeat.Selection) is
    // never reset once a repeat cycle finishes voluntarily, so a bare
    // IsNativeRepeatCombatVoluntaryFinish read can alias onto a stale
    // decision left over from an earlier, already-completed repeat combat --
    // same "stale never-reset field, no scoping guard" shape PR #551 fixed
    // for CurrentSecondAssault. Scope the check to the round actually bound
    // to the CURRENT Selection timeout by requiring the stale decision's own
    // StateVersion to match the live contact-authority version, the same
    // freshness guard CompleteNativeRepeatCombatNoAttack already enforces
    // before accepting it. A caller that needs to route a no-attack timeout
    // between the repeat and non-repeat completion paths should use this,
    // not the bare IsNativeRepeatCombatVoluntaryFinish, so a brand-new,
    // unrelated Combat's initial Selection timeout is never misrouted to
    // CompleteNativeRepeatCombatNoAttack.
    public static bool IsNativeRepeatCombatNoAttackPending(ExerciseSession session) =>
        session.Pending is null
        && (session.CurrentPostRepeat?.Selection
            ?? session.CurrentRepeatCombatSelection) is { } selection
        && selection.Choice == "finish-combat-without-attack"
        && selection.StateVersion == session.CurrentContacts.AuthorityVersion;

    public static bool IsNativeRepeatCombatSelectedAssault(ExerciseSession session) =>
        session.Pending is null
        && (session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection) is { } selection
        && selection.Choice == "select-contact-assault"
        && selection.StateVersion == session.CurrentContacts.AuthorityVersion;

    public static (string DecisionActionId, string DeclineEventHash)
        QueryNativeCombatForceRoundBinding(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (CurrentOccurrenceAssault(session) is
            {
                Decline: { } repeatedDecline,
                ForceCompletion: null,
                ForceCancellation: null
            } repeated)
            return (repeated.Selection.Decision.ActionId,
                "sha256:" + Convert.ToHexStringLower(
                    System.Security.Cryptography.SHA256.HashData(
                        repeatedDecline.SerializeCanonical())));
        if (session.Pending is not null || CurrentOccurrenceAssault(session) is not null
            || session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatRetreatDecline is not { } decline
            || session.CurrentCombatForceCompletion is not null
            || session.CurrentCombatForceCancellation is not null)
            throw new InvalidOperationException("No collecting Force round is available.");
        return (selected.Decision.ActionId,
            "sha256:" + Convert.ToHexStringLower(
                System.Security.Cryptography.SHA256.HashData(decline.SerializeCanonical())));
    }

    // Slot-scoped: the predecessor's own occurrence decides which retained fields prove the
    // completion, so a stale Stage 1 field can neither satisfy nor block a later-stage check.
    private static bool CurrentFirstSideNoAttackCompleted(ExerciseSession predecessor, ExerciseSession? successor) =>
        successor is not null && predecessor.Pending switch
        {
            StageEntryCombatSelectionPending => predecessor.CurrentStageEntryCombatDecision is not null
                && successor.CurrentStageEntryNoAttackCompletion is not null,
            ThirdStageCombatSelectionPending => predecessor.CurrentThirdStageCombatDecision is not null
                && successor.CurrentThirdStageNoAttackCompletion is not null,
            _ => predecessor.CurrentCombatDecision is not null && successor.CurrentNoAttackCompletion is not null,
        };

    private static bool CurrentSecondSideNoAttackCompleted(ExerciseSession predecessor, ExerciseSession? successor) =>
        successor is not null && predecessor.Pending switch
        {
            StageEntrySecondCombatSelectionPending => predecessor.CurrentStageEntrySecondCombatDecision is not null
                && successor.CurrentStageEntrySecondNoAttackCompletion is not null,
            ThirdStageSecondCombatSelectionPending => predecessor.CurrentThirdStageSecondCombatDecision is not null
                && successor.CurrentThirdStageSecondNoAttackCompletion is not null,
            _ => predecessor.CurrentSecondCombatDecision is not null
                && successor.CurrentSecondNoAttackCompletion is not null,
        };

    /// <summary>Deterministic System command retained alongside the no-attack event.</summary>
    public static CampaignActionSubmission NativeCombatNoAttackCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null
            || !CurrentFirstSideNoAttackCompleted(predecessor, accepted.SuccessorSession))
            throw new ArgumentException("Accepted no-attack completion required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    /// <summary>
    /// #394 extra-2/3: second-side analog of <see cref="NativeCombatNoAttackCommand"/>,
    /// used only for the <c>NativeSecondCombatNoAttackKind</c> accepted step.
    /// </summary>
    public static CampaignActionSubmission SecondCombatNoAttackCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is null
            || !CurrentSecondSideNoAttackCompleted(predecessor, accepted.SuccessorSession))
            throw new ArgumentException("Accepted second-side no-attack completion required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeRepeatCombatNoAttackCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentPostRepeat?.Selection is null
                ? predecessor.CurrentRepeatCombatSelection is null
                    || accepted.SuccessorSession?.CurrentRepeatNoAttack is null
                : accepted.SuccessorSession?.CurrentPostRepeatEvents
                    is not [.., CampaignNativeRepeatCombatNoAttackV32]))
            throw new ArgumentException(
                "Accepted repeated no-attack completion required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion,
            checkpoint.PositionId, CampaignActionAudience.System,
            accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatBarrageCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentRepeatAssault is { Barrage: null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.Barrage is null
                : predecessor.CurrentCombatDecision is null
                    || accepted.SuccessorSession?.CurrentCombatBarrage is null))
            throw new ArgumentException("Accepted Barrage completion required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatForceCompletionCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted ||
            (predecessor.CurrentRepeatAssault is { Seals.Count: 2 }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.ForceCompletion is null
                : predecessor.CurrentCombatForceSeals.Count != 2
                    || accepted.SuccessorSession?.CurrentCombatForceCompletion is null))
            throw new ArgumentException("Accepted Force completion required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatForceCancellationCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentRepeatAssault is { } repeated
                ? repeated.Seals.Count > 1
                    || accepted.SuccessorSession?.CurrentRepeatAssault?.ForceCancellation is null
                : predecessor.CurrentCombatForceSeals.Count > 1
                    || accepted.SuccessorSession?.CurrentCombatForceCancellation is null))
            throw new ArgumentException("Accepted Force cancellation required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatRbaCancellationCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is not { EventRecords.Count: 1 } evidence
            || (predecessor.CurrentRepeatAssault is not null
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.RbaCancellation is null
                : accepted.SuccessorSession?.CurrentCombatRbaCancellation is null))
            throw new ArgumentException("Accepted RBA cancellation required.", nameof(accepted));
        var cancellation = CampaignNativeCombatRbaCancellationV33.Deserialize(evidence.EventRecords[0]);
        var binding = QueryNativeCombatRbaRoundBinding(predecessor);
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        if (cancellation.CampaignId != checkpoint.CampaignId
            || cancellation.PriorStateVersion != checkpoint.StateVersion
            || cancellation.PositionId != checkpoint.PositionId
            || cancellation.DecisionActionId != binding.DecisionActionId
            || cancellation.BarrageEventHash != binding.BarrageEventHash)
            throw new ArgumentException("RBA cancellation differs from predecessor.", nameof(accepted));
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatAntiArmorCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted ||
            (predecessor.CurrentRepeatAssault is
        { ForceCompletion: not null } or { ForceCancellation: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.AntiArmor is null
                : accepted.SuccessorSession?.CurrentCombatAntiArmor is null))
            throw new ArgumentException("Accepted Anti Armor completion required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatCancelledAssaultCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentRepeatAssault is { ForceCancellation: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.CancelledAssault is null
                : accepted.SuccessorSession?.CurrentCombatCancelledAssault is null))
            throw new ArgumentException("Accepted cancelled assault completion required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatAssaultResultCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted ||
            (predecessor.CurrentRepeatAssault is { AntiArmor: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.Result is null
                : accepted.SuccessorSession?.CurrentCombatAssaultResult is null))
            throw new ArgumentException("Accepted native assault result required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatNoPostAssaultRetreatCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentRepeatAssault is { Result: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.PostResultRetreat
                    is not { Disposition: "not-required" }
                : accepted.SuccessorSession?.CurrentCombatPostResultRetreat
                    is not { Disposition: "not-required" }))
            throw new ArgumentException("Accepted no-retreat completion required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static ExerciseStepResult SettleNativeCombatWorld(ExerciseSession session) =>
        CampaignExercises.SettleNativeCombatWorld(session);

    public static ExerciseStepResult CompleteNoNativeCombatCustody(ExerciseSession session) =>
        CampaignExercises.CompleteNoNativeCombatCustody(session);

    public static ExerciseStepResult CompleteNativeCombatSuccessor(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatSuccessor(session);

    public static bool IsNativeReserveReleaseEmpty(ExerciseSession session)
    {
        try
        {
            var state = CampaignExercises.QueryNativeReserveReleaseState(session);
            return state.StateVersion == session.CurrentContacts.AuthorityVersion
                && !state.Completed && state.Pending(session.Context).Count == 0;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static CampaignHostReserveReleaseOwnerDraft QueryNativeReserveReleaseOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string elementId)
    {
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Reserve Release needs an owner side."),
        };
        var state = CampaignExercises.QueryNativeReserveReleaseState(session);
        var cut = CampaignReserveReleaseOwnerCutV27.Project(state, session.Context,
            side, elementId);
        return new CampaignHostReserveReleaseOwnerDraft(state.StateVersion,
            cut.VisibleBytes, cut.ActionIds);
    }

    public static ExerciseStepResult SubmitNativeReserveReleaseChoice(
        ExerciseSession session, CampaignActionSubmission submission,
        string elementId)
    {
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidSubmission);
        CampaignNativeReserveReleaseStateV27 state;
        CampaignReserveReleaseOwnerCutV27 cut;
        try
        {
            state = CampaignExercises.QueryNativeReserveReleaseState(session);
            var side = submission.Audience == CampaignActionAudience.Axis
                ? LandSide.Axis : submission.Audience == CampaignActionAudience.Commonwealth
                    ? LandSide.Commonwealth : (LandSide?)null;
            if (side is null)
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.ActionNotLegal);
            cut = CampaignReserveReleaseOwnerCutV27.Project(state, session.Context,
                side.Value, elementId);
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
        if (submission.CampaignId != state.CampaignId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != state.StateVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != state.Position.PositionId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var ordinal = Array.FindIndex(cut.ActionIds.ToArray(), value =>
            value == submission.ActionId);
        if (ordinal < 0)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitNativeReserveReleaseChoice(session,
            submission.Audience, elementId, cut.Member.Choices[ordinal],
            submission.ActionId);
    }

    public static (string EntryHash, long StateVersion) QueryNativeCombatSelectionRoundBinding(ExerciseSession session)
    {
        return CampaignExercises.QueryNativeCombatSelectionRoundBinding(session);
    }

    public static (string EntryHash, long StateVersion)? QueryNativeCombatSelectionEntryBinding(CampaignHostAcceptedStep step) =>
        QueryNativeCombatSelectionEntryBinding(step, legacyInitiativeOwner: false);

    /// <summary>Resolve a retained entry under its recorded pre-correction owner policy.</summary>
    public static (string EntryHash, long StateVersion)? QueryNativeCombatSelectionEntryBinding(CampaignHostAcceptedStep step,
        bool legacyInitiativeOwner) =>
        QueryNativeCombatSelectionEntryBinding(step, legacyInitiativeOwner, Cna1979SyntheticContentResolver.Instance);

    public static (string EntryHash, long StateVersion)? QueryNativeCombatSelectionEntryBinding(CampaignHostAcceptedStep step,
        bool legacyInitiativeOwner, IContentPackV6Resolver resolver)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (step.Kind == CampaignHostAcceptedStep.NativeRepeatBreakdownKind && step.EventRecords.Count == 1)
        {
            var breakdown = CampaignNativeRepeatBreakdownV30.Deserialize(step.EventRecords[0]);
            return (CampaignSelectionTimeoutEvidenceV38.Digest(breakdown.SerializeCanonical()), breakdown.StateVersion);
        }
        // #394 gap 4: second-side analog of the NativeRepeatBreakdownKind
        // case immediately above -- VerifySelectionRounds replays every
        // accepted step's binding from history to cross-check the
        // selection_rounds table, so this must derive the exact same
        // EntryHash QuerySecondCombatSelectionRoundBinding computed when the
        // round was first initialized (CampaignExercises.cs), or a
        // genuinely correct second-side round looks tampered-with on replay.
        // Stage 2 Breakdown for either role opens its own V46-bound Selection round exactly
        // like Stage 1 second side. Live arming and cold replay use the same entry digest.
        if (step.Kind is CampaignHostAcceptedStep.NativeSecondBreakdownKind
                or CampaignHostAcceptedStep.NativeStageEntryBreakdownKind
                or CampaignHostAcceptedStep.NativeStageEntrySecondBreakdownKind && step.EventRecords.Count == 1)
        {
            var secondBreakdown = CampaignNativeSecondBreakdownV46.Deserialize(step.EventRecords[0]);
            return (CampaignSelectionTimeoutEvidenceV38.Digest(secondBreakdown.SerializeCanonical()), secondBreakdown.StateVersion);
        }
        if (step.Kind is not (CampaignHostAcceptedStep.LegacyKind or CampaignHostAcceptedStep.ContactMovementKind)) return null;
        using var document = System.Text.Json.JsonDocument.Parse(step.SnapshotCheckpoint);
        if (document.RootElement.GetProperty("contractVersion").GetInt32() != 11) return null;
        var snapshot = CampaignSnapshotV11Serializer.Deserialize(step.SnapshotCheckpoint);
        try
        {
            CampaignNativeCombatSelectionTimeoutV38.ValidateInitialScope(snapshot,
                CampaignCurrentSnapshotSerializer.ResolveContext(snapshot.Setup,
                    resolver), legacyInitiativeOwner);
        }
        catch (InvalidOperationException) { return null; }
        return (CampaignSelectionTimeoutEvidenceV38.Digest(step.SnapshotCheckpoint), snapshot.StateVersion);
    }

    public static string NoTimingSelectionTimeoutHash => CampaignSelectionTimeoutEvidenceV38.NoTimingConfigurationHash;

    public static ExerciseStepResult TimeoutNativeCombatSelection(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.TimeoutNativeCombatSelection(session, configuration, openedAtUnixMilliseconds,
            deadlineUnixMilliseconds, highWaterUnixMilliseconds, cancelledAtUnixMilliseconds, reason);

    public static CampaignActionSubmission NativeCombatSelectionTimeoutCommand(ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(accepted);
        if (!accepted.IsAccepted || accepted.Evidence is not { EventRecords.Count: 1 } evidence)
            throw new ArgumentException("Accepted selection timeout is required.", nameof(accepted));
        var timeout = CampaignSelectionTimeoutEvidenceV38.Deserialize(evidence.EventRecords[0]);
        var binding = CampaignExercises.QueryNativeCombatSelectionRoundBinding(predecessor,
            legacyInitiativeOwner: timeout.ContractVersion == 38);
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        if (timeout.CampaignId != checkpoint.CampaignId || timeout.PriorStateVersion != binding.StateVersion
            || timeout.EntryHash != binding.EntryHash || timeout.PositionId != checkpoint.PositionId
            || evidence.Receipt.Audience != CampaignActionAudience.System)
            throw new ArgumentException("Selection timeout differs from predecessor.", nameof(accepted));
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, evidence.Receipt.ActionId);
    }

    public static (string ResultHash, long StateVersion) QueryNativeCombatRetreatRoundBinding(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cut = CampaignExercises.QueryContactPostResultRetreatCut(session);
        var result = session.CurrentRepeatAssault is { Result: not null, PostResultRetreat: null } repeat
            ? repeat.Result : session.CurrentCombatAssaultResult!;
        return ($"sha256:{Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(result.SerializeCanonical()))}", result.StateVersion);
    }

    public static string NoTimingRetreatTimeoutHash => CampaignNativeCombatRetreatTimeoutV37.NoTimingConfigurationHash;

    public static ExerciseStepResult TimeoutNativeCombatRetreat(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.TimeoutNativeCombatRetreat(session, configuration, openedAtUnixMilliseconds,
            deadlineUnixMilliseconds, highWaterUnixMilliseconds, cancelledAtUnixMilliseconds, reason);

    public static CampaignActionSubmission NativeCombatRetreatTimeoutCommand(ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(accepted);
        if (!accepted.IsAccepted || accepted.Evidence is not { EventRecords.Count: 1 } evidence)
            throw new ArgumentException("Accepted retreat timeout is required.", nameof(accepted));
        var timeout = CampaignNativeCombatRetreatTimeoutV37.Deserialize(evidence.EventRecords[0]);
        var binding = QueryNativeCombatRetreatRoundBinding(predecessor);
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        if (timeout.CampaignId != checkpoint.CampaignId || timeout.PriorStateVersion != binding.StateVersion
            || timeout.ResultHash != binding.ResultHash || timeout.PositionId != checkpoint.PositionId
            || evidence.Receipt.Audience != CampaignActionAudience.System)
            throw new ArgumentException("Retreat timeout differs from predecessor.", nameof(accepted));
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, evidence.Receipt.ActionId);
    }

    public static (string SettlementHash, long StateVersion) QueryNativeCombatCustodyRoundBinding(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cut = CampaignExercises.QueryNativeCombatCustodyCut(session)
            ?? throw new InvalidOperationException("No pending custody decision.");
        if (!cut.Choices.Any(value => value.Kind == "leave-unguarded"))
            throw new InvalidOperationException("Custody fallback is outside supported source scope.");
        return (cut.SettlementHash, cut.AuthorityVersion);
    }

    public static string NoTimingCustodyTimeoutHash => CampaignNativeCombatCustodyTimeoutV36.NoTimingConfigurationHash;

    public static ExerciseStepResult TimeoutNativeCombatCustody(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.TimeoutNativeCombatCustody(session, configuration, openedAtUnixMilliseconds,
            deadlineUnixMilliseconds, highWaterUnixMilliseconds, cancelledAtUnixMilliseconds, reason);

    public static CampaignActionSubmission NativeCombatCustodyTimeoutCommand(ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(accepted);
        if (!accepted.IsAccepted || accepted.Evidence is not { EventRecords.Count: 1 } evidence)
            throw new ArgumentException("Accepted custody timeout is required.", nameof(accepted));
        var timeout = CampaignNativeCombatCustodyTimeoutV36.Deserialize(evidence.EventRecords[0]);
        var binding = QueryNativeCombatCustodyRoundBinding(predecessor);
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        if (timeout.CampaignId != checkpoint.CampaignId || timeout.PriorStateVersion != binding.StateVersion
            || timeout.SettlementHash != binding.SettlementHash || timeout.PositionId != checkpoint.PositionId
            || evidence.Receipt.Audience != CampaignActionAudience.System)
            throw new ArgumentException("Custody timeout differs from predecessor.", nameof(accepted));
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, evidence.Receipt.ActionId);
    }

    public static ExerciseStepResult CompleteNativeReserveRelease(ExerciseSession session) =>
        CampaignExercises.CompleteNativeReserveRelease(session);

    public static (string ReleaseEventHash, long StateVersion, int CycleOrdinal)
        QueryNativeReserveReleaseRoundBinding(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var release = CampaignExercises.QueryPendingNativeReserveRelease(session);
        return (release.ReleaseEventHash, release.StateVersion, release.CycleOrdinal);
    }

    public static string NoTimingReserveReleaseTimeoutHash =>
        CampaignNativeReserveReleaseTimeoutV35.NoTimingConfigurationHash;

    public static ExerciseStepResult TimeoutNativeReserveRelease(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.TimeoutNativeReserveRelease(session, configuration,
            openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds,
            cancelledAtUnixMilliseconds, reason);

    public static CampaignActionSubmission NativeReserveReleaseTimeoutCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(accepted);
        if (!accepted.IsAccepted || accepted.Evidence is not { EventRecords.Count: 1 } evidence)
            throw new ArgumentException("Accepted Reserve Release timeout is required.", nameof(accepted));
        var timeout = CampaignNativeReserveReleaseTimeoutV35.Deserialize(evidence.EventRecords[0]);
        var binding = QueryNativeReserveReleaseRoundBinding(predecessor);
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        if (timeout.CampaignId != checkpoint.CampaignId
            || timeout.PriorStateVersion != binding.StateVersion
            || timeout.ReleaseEventHash != binding.ReleaseEventHash
            || timeout.PriorEventHash != CampaignExercises.QueryPendingNativeReserveRelease(predecessor).HeadEventHash
            || timeout.CycleOrdinal != binding.CycleOrdinal
            || evidence.Receipt.Audience != CampaignActionAudience.System)
            throw new ArgumentException("Reserve Release timeout differs from predecessor.", nameof(accepted));
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, evidence.Receipt.ActionId);
    }

    public static (string ReleaseEventHash, long StateVersion, int CycleOrdinal)
        QueryNativeCycleControlRoundBinding(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var release = CampaignExercises.QueryPendingNativeCycleControl(session);
        return (release.HeadEventHash, release.StateVersion, release.CycleOrdinal);
    }

    public static string NoTimingCycleControlTimeoutHash =>
        CampaignNativeCycleControlTimeoutV34.NoTimingConfigurationHash;

    public static ExerciseStepResult TimeoutNativeCycleControl(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.TimeoutNativeCycleControl(session, configuration,
            openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds,
            cancelledAtUnixMilliseconds, reason);

    public static CampaignActionSubmission NativeCycleControlTimeoutCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(accepted);
        if (!accepted.IsAccepted || accepted.Evidence is not { EventRecords.Count: 1 } evidence)
            throw new ArgumentException("Accepted cycle timeout is required.", nameof(accepted));
        var timeout = CampaignNativeCycleControlTimeoutV34.Deserialize(evidence.EventRecords[0]);
        var binding = QueryNativeCycleControlRoundBinding(predecessor);
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        if (timeout.CampaignId != checkpoint.CampaignId
            || timeout.PriorStateVersion != binding.StateVersion
            || timeout.PriorEventHash != binding.ReleaseEventHash
            || timeout.PriorCycleOrdinal != binding.CycleOrdinal
            || evidence.Receipt.Audience != CampaignActionAudience.System)
            throw new ArgumentException("Cycle timeout differs from predecessor.", nameof(accepted));
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, evidence.Receipt.ActionId);
    }

    private static void RequireStageEntryPosition(ExerciseSession session, string position)
    {
        if (CampaignExercises.QueryCheckpoint(session).PositionId != position)
            throw new InvalidOperationException("Stage Entry opening is not available at this position.");
    }

    // The same Stage Entry wire kinds serve both acting roles. Retained Pending fields can
    // belong to the previous role at a boundary, so resolve the occurrence from the cursor.
    private static bool IsStageEntrySecondRole(ExerciseSession session, string phase, string? segment = null)
    {
        var turn = CampaignGameTurn.CurrentGameTurn(session);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var position = Cna1979LandSequence.CreateTurn(turn).SingleOrDefault(p => p.PositionId == checkpoint.PositionId);
        if (position is not { OperationStage: 2 } || position.PhaseId != phase || position.SegmentId != segment
            || PendingOccurrenceTable.Find(2, position.ActorRole, turn == session.CurrentSnapshot.Setup.InitialGameTurn) is null)
            throw new InvalidOperationException("Stage Entry opening is not available at this position.");
        return position.ActorRole == LandActorRole.SecondActingSide;
    }

    /// <summary>Deterministic Stage 2 System transitions, selected from the current native cut.</summary>
    public static (string Kind, ExerciseStepResult Result)? TryCompleteStageEntrySystemStep(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.Pending switch
        {
            InitiativeDeclarationPending => (CampaignHostAcceptedStep.NativeStageEntryWeatherKind, CampaignExercises.CompleteStageEntryWeather(session)),
            StageEntryWeatherPending => (CampaignHostAcceptedStep.NativeStageEntryOrganizationKind, CampaignStageEntryPreambleAuthority.CompleteStageEntryOrganization(session)),
            NoObligationOrganizationPending => (CampaignHostAcceptedStep.NativeStageEntryNavalConvoyArrivalKind, CampaignStageEntryPreambleAuthority.CompleteStageEntryNavalConvoyArrival(session)),
            NoObligationNavalConvoyArrivalPending => (CampaignHostAcceptedStep.NativeStageEntryFleetAssignmentKind, CampaignStageEntryPreambleAuthority.CompleteStageEntryFleetAssignment(session)),
            NoObligationFleetAssignmentPending => (CampaignHostAcceptedStep.NativeStageEntryFleetRepairKind, CampaignStageEntryPreambleAuthority.CompleteStageEntryFleetRepair(session)),
            StageEntryMovementCompletionPending => (CampaignHostAcceptedStep.NativeStageEntryBreakdownKind, CampaignExercises.CompleteStageEntryBreakdown(session)),
            StageEntrySecondMovementCompletionPending => (CampaignHostAcceptedStep.NativeStageEntrySecondBreakdownKind, CampaignExercises.CompleteStageEntrySecondBreakdown(session)),
            _ => null,
        };
    }

    private static ExerciseStepResult CompleteStageEntrySystemStep(ExerciseSession session, string kind) =>
        TryCompleteStageEntrySystemStep(session) is { } step && step.Kind == kind
            ? step.Result : ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);

    public static CampaignActionSubmission StageEntrySystemCommand(ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(accepted);
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is not { Receipt: { Audience: CampaignActionAudience.System } receipt }
            || receipt.CampaignId != checkpoint.CampaignId || receipt.PriorStateVersion != checkpoint.StateVersion)
            throw new ArgumentException("Accepted Stage Entry System step required.", nameof(accepted));
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, CampaignActionAudience.System, receipt.ActionId);
    }

    public static CampaignHostCycleControlOwnerDraft QueryStageEntryMovementOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var second = IsStageEntrySecondRole(session, LandPhaseIds.MovementAndCombat, LandSegmentIds.Movement);
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Stage Entry Movement needs an owner side."),
        };
        var cut = second ? CampaignExercises.QueryStageEntrySecondMovementCut(session, side, elementId)
            : CampaignExercises.QueryStageEntryMovementCut(session, side, elementId);
        var actions = cut.MovementOffers.OrderBy(value => value.OriginLocationId, StringComparer.Ordinal)
            .ThenBy(value => value.DestinationLocationId, StringComparer.Ordinal)
            .Select(CampaignNativeSecondMoveV44.ActionIdFor).ToArray();
        return new CampaignHostCycleControlOwnerDraft(cut.AuthorityVersion,
            CampaignContactOwnerVisibleCutV1.SerializeStageEntryMovement(cut,
                CampaignExercises.QueryCheckpoint(session).PositionId), actions);
    }

    public static ExerciseStepResult SubmitStageEntryMovement(ExerciseSession session,
        CampaignActionSubmission submission, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        CampaignHostCycleControlOwnerDraft draft;
        try { draft = QueryStageEntryMovementOwnerDraft(session, submission.Audience, elementId); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (!draft.PrivateActionIds.Contains(submission.ActionId, StringComparer.Ordinal))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return IsStageEntrySecondRole(session, LandPhaseIds.MovementAndCombat, LandSegmentIds.Movement)
            ? CampaignExercises.SubmitStageEntrySecondMovement(session, submission.Audience, submission.ActionId, elementId)
            : CampaignExercises.SubmitStageEntryMovement(session, submission.Audience, submission.ActionId, elementId);
    }

    public static CampaignHostCycleControlOwnerDraft QueryStageEntryMovementCompletionOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var second = IsStageEntrySecondRole(session, LandPhaseIds.MovementAndCombat, LandSegmentIds.Movement);
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Stage Entry Movement completion needs an owner."),
        };
        var (predecessor, world) = second ? CampaignExercises.QueryStageEntrySecondMovementSource(session)
            : CampaignExercises.QueryStageEntryMovementSource(session);
        var proposed = CampaignNativeSecondMovementCompletedV45.Create(predecessor, world, session.Context,
            CampaignExercises.StageEntryOrders(session), side, session.CurrentSnapshot.Setup.InitialGameTurn,
            session.CurrentContacts.AuthorityVersion);
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignNativeSecondMovementCompletedV45.CurrentContractVersion);
            writer.WriteString("campaignId", proposed.CampaignId);
            writer.WriteString("side", side.ToString());
            writer.WriteString("positionId", proposed.PositionId);
            writer.WriteString("choice", "complete-movement");
            writer.WriteEndObject();
        }
        var visible = stream.ToArray();
        var action = "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            (byte[])[.. visible, 0, .. System.Text.Encoding.UTF8.GetBytes("complete-movement")]));
        return new CampaignHostCycleControlOwnerDraft(proposed.PriorStateVersion, visible, [action]);
    }

    public static ExerciseStepResult SubmitStageEntryMovementCompletion(ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        CampaignHostCycleControlOwnerDraft draft;
        try { draft = QueryStageEntryMovementCompletionOwnerDraft(session, submission.Audience); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (!draft.PrivateActionIds.Contains(submission.ActionId, StringComparer.Ordinal))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return IsStageEntrySecondRole(session, LandPhaseIds.MovementAndCombat, LandSegmentIds.Movement)
            ? CampaignExercises.CompleteStageEntrySecondMovement(session, submission.Audience, submission.ActionId)
            : CampaignExercises.CompleteStageEntryMovement(session, submission.Audience, submission.ActionId);
    }

    private static (CampaignHostCycleControlOwnerDraft Draft, CampaignActionCandidate[] Actions)
        CreateStageEntryReserveOwnerDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var second = IsStageEntrySecondRole(session, LandPhaseIds.ReserveDesignation);
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Stage Entry Reserve needs an owner side."),
        };
        var (predecessor, world) = second ? CampaignExercises.QueryStageEntrySecondReserveSource(session)
            : CampaignExercises.QueryStageEntryReserveSource(session);
        var (check, _) = CampaignNativeSecondReserveV43.Create(predecessor, world, session.Context,
            CampaignExercises.StageEntryOrders(session), side, null, session.CurrentSnapshot.Setup.InitialGameTurn,
            session.CurrentContacts.AuthorityVersion);
        var definition = session.Context.ArtifactV6!.Definition.LegacyDefinition;
        var actions = CampaignOwnElementProjection.ProjectPlaced(definition, world, side)
            .Where(element => element.ReserveStatus == CampaignObservationReserveStatus.None
                && element.VehicleBreakdownRisk is null)
            .Select(element => (CampaignActionCandidate)new DesignateReserveAction(element.ElementId))
            .Append(new CompleteReserveDesignationAction()).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignNativeSecondReserveV43.CurrentContractVersion);
            writer.WriteString("campaignId", check.CampaignId);
            writer.WriteString("side", side.ToString());
            writer.WriteString("positionId", check.PositionId);
            writer.WriteStartArray("choices");
            foreach (var action in actions)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", action is DesignateReserveAction ? "designate" : "complete");
                if (action is DesignateReserveAction designated) writer.WriteString("elementId", designated.ElementId);
                else writer.WriteNull("elementId");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return (new CampaignHostCycleControlOwnerDraft(check.PriorStateVersion, stream.ToArray(),
            actions.Select(action => action.ActionId).ToArray()), actions);
    }

    public static CampaignHostCycleControlOwnerDraft QueryStageEntryReserveOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience) => CreateStageEntryReserveOwnerDraft(session, audience).Draft;

    public static ExerciseStepResult SubmitStageEntryReserve(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        (CampaignHostCycleControlOwnerDraft Draft, CampaignActionCandidate[] Actions) cut;
        try { cut = CreateStageEntryReserveOwnerDraft(session, submission.Audience); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var action = cut.Actions.SingleOrDefault(value => value.ActionId == submission.ActionId);
        if (action is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var elementId = action is DesignateReserveAction designated ? designated.ElementId : null;
        return IsStageEntrySecondRole(session, LandPhaseIds.ReserveDesignation)
            ? CampaignExercises.SubmitStageEntrySecondReserve(session, submission.Audience, submission.ActionId, elementId)
            : CampaignExercises.SubmitStageEntryReserve(session, submission.Audience, submission.ActionId, elementId);
    }

    public static CampaignHostCycleControlOwnerDraft QuerySecondMovementOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Second Movement needs an owner side."),
        };
        var cut = CampaignExercises.QuerySecondMovementCut(session, side, elementId);
        var actions = cut.MovementOffers.OrderBy(value => value.OriginLocationId, StringComparer.Ordinal)
            .ThenBy(value => value.DestinationLocationId, StringComparer.Ordinal)
            .Select(CampaignNativeSecondMoveV44.ActionIdFor).ToArray();
        return new CampaignHostCycleControlOwnerDraft(cut.AuthorityVersion,
            CampaignContactOwnerVisibleCutV1.SerializeSecondMovement(cut), actions);
    }

    public static ExerciseStepResult SubmitSecondMovement(ExerciseSession session,
        CampaignActionSubmission submission, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        CampaignHostCycleControlOwnerDraft draft;
        try { draft = QuerySecondMovementOwnerDraft(session, submission.Audience, elementId); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (!draft.PrivateActionIds.Contains(submission.ActionId, StringComparer.Ordinal))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitSecondMovement(session, submission.Audience, submission.ActionId, elementId);
    }

    public static CampaignHostCycleControlOwnerDraft QuerySecondMovementCompletionOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Second Movement completion needs an owner."),
        };
        var (predecessor, world) = CampaignExercises.QuerySecondMovementSource(session);
        var proposed = CampaignNativeSecondMovementCompletedV45.Create(predecessor, world, session.Context,
            session.CurrentSnapshot.OperationStageOrders, side, session.CurrentSnapshot.Setup.InitialGameTurn,
            session.CurrentContacts.AuthorityVersion);
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignNativeSecondMovementCompletedV45.CurrentContractVersion);
            writer.WriteString("campaignId", proposed.CampaignId);
            writer.WriteString("side", side.ToString());
            writer.WriteString("positionId", proposed.PositionId);
            writer.WriteString("choice", "complete-movement");
            writer.WriteEndObject();
        }
        var visible = stream.ToArray();
        var action = "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            (byte[])[.. visible, 0, .. System.Text.Encoding.UTF8.GetBytes("complete-movement")]));
        return new CampaignHostCycleControlOwnerDraft(proposed.PriorStateVersion, visible, [action]);
    }

    public static ExerciseStepResult SubmitSecondMovementCompletion(ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        CampaignHostCycleControlOwnerDraft draft;
        try { draft = QuerySecondMovementCompletionOwnerDraft(session, submission.Audience); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (!draft.PrivateActionIds.Contains(submission.ActionId, StringComparer.Ordinal))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.CompleteSecondMovement(session, submission.Audience, submission.ActionId);
    }

    private static (CampaignHostCycleControlOwnerDraft Draft, CampaignActionCandidate[] Actions)
        CreateStageEntryInitiativeDeclarationOwnerDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Initiative Declaration needs an owner side."),
        };
        var holder = session.CurrentSnapshot.InitiativeHolder
            ?? throw new InvalidOperationException("Initiative Declaration requires a resolved holder.");
        if (side != holder)
            throw new InvalidOperationException("Only the initiative holder may declare.");
        RequireStageEntryPosition(session, "land.position.operation-2.initiative-declaration");
        var predecessor = CampaignExercises.QueryStageEntryInitiativeDeclarationSource(session);
        // Same catalog lookup CampaignNativeInitiativeDeclarationV49.Create performs internally --
        // duplicated here (read-only, no event produced) because Create itself requires actFirst,
        // the very choice this draft is offering, so it cannot be used to probe for OperationStage.
        var position = Cna1979LandSequenceV4.CreateTurn(session.CurrentSnapshot.Setup.InitialGameTurn)
            .Single(value => value.PositionId == predecessor.NextPositionId);
        var actions = new CampaignActionCandidate[] { new ActFirstAction(position.OperationStage),
            new ActLastAction(position.OperationStage) };
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignNativeInitiativeDeclarationV49.CurrentContractVersion);
            writer.WriteString("campaignId", predecessor.CampaignId);
            writer.WriteString("holder", holder.ToString());
            writer.WriteString("positionId", position.PositionId);
            writer.WriteStartArray("choices");
            foreach (var action in actions)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", action is ActFirstAction ? "act-first" : "act-last");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return (new CampaignHostCycleControlOwnerDraft(predecessor.StateVersion, stream.ToArray(),
            actions.Select(action => action.ActionId).ToArray()), actions);
    }

    public static CampaignHostCycleControlOwnerDraft QueryStageEntryInitiativeDeclarationOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CreateStageEntryInitiativeDeclarationOwnerDraft(session, audience).Draft;

    public static ExerciseStepResult SubmitStageEntryInitiativeDeclaration(
        ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        (CampaignHostCycleControlOwnerDraft Draft, CampaignActionCandidate[] Actions) cut;
        try { cut = CreateStageEntryInitiativeDeclarationOwnerDraft(session, submission.Audience); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var action = cut.Actions.SingleOrDefault(value => value.ActionId == submission.ActionId);
        if (action is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitStageEntryInitiativeDeclaration(session, submission.Audience,
            submission.ActionId, action is ActFirstAction);
    }

    private static (CampaignHostCycleControlOwnerDraft Draft, CampaignActionCandidate[] Actions)
        CreateSecondReserveOwnerDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Second Reserve needs an owner side."),
        };
        var (predecessor, world) = CampaignExercises.QuerySecondReserveSource(session);
        var (check, _) = CampaignNativeSecondReserveV43.Create(predecessor, world, session.Context,
            session.CurrentSnapshot.OperationStageOrders, side, null, session.CurrentSnapshot.Setup.InitialGameTurn,
            session.CurrentContacts.AuthorityVersion);
        var definition = session.Context.ArtifactV6!.Definition.LegacyDefinition;
        var actions = CampaignOwnElementProjection.ProjectPlaced(definition, world, side)
            .Where(element => element.ReserveStatus == CampaignObservationReserveStatus.None
                && element.VehicleBreakdownRisk is null)
            .Select(element => (CampaignActionCandidate)new DesignateReserveAction(element.ElementId))
            .Append(new CompleteReserveDesignationAction()).ToArray();
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignNativeSecondReserveV43.CurrentContractVersion);
            writer.WriteString("campaignId", check.CampaignId);
            writer.WriteString("side", side.ToString());
            writer.WriteString("positionId", check.PositionId);
            writer.WriteStartArray("choices");
            foreach (var action in actions)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", action is DesignateReserveAction ? "designate" : "complete");
                if (action is DesignateReserveAction designated) writer.WriteString("elementId", designated.ElementId);
                else writer.WriteNull("elementId");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return (new CampaignHostCycleControlOwnerDraft(check.PriorStateVersion, stream.ToArray(),
            actions.Select(action => action.ActionId).ToArray()), actions);
    }

    public static CampaignHostCycleControlOwnerDraft QuerySecondReserveOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience) => CreateSecondReserveOwnerDraft(session, audience).Draft;

    public static ExerciseStepResult SubmitSecondReserve(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        (CampaignHostCycleControlOwnerDraft Draft, CampaignActionCandidate[] Actions) cut;
        try { cut = CreateSecondReserveOwnerDraft(session, submission.Audience); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var action = cut.Actions.SingleOrDefault(value => value.ActionId == submission.ActionId);
        if (action is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitSecondReserve(session, submission.Audience, submission.ActionId,
            action is DesignateReserveAction designated ? designated.ElementId : null);
    }

    public static CampaignHostCycleControlOwnerDraft QueryPostCombatPassOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Post-combat pass needs an owner side."),
        };
        var proposed = CampaignNativePostCombatPassV42.Create(CampaignExercises.QueryPostCombatPassSource(session),
            side, session.CurrentSnapshot.Setup.InitialGameTurn, session.CurrentContacts.AuthorityVersion);
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignNativePostCombatPassV42.CurrentContractVersion);
            writer.WriteString("campaignId", proposed.CampaignId);
            writer.WriteString("side", side.ToString());
            writer.WriteString("positionId", proposed.PositionId);
            writer.WriteString("choice", "finish-without-activity");
            writer.WriteEndObject();
        }
        var visible = stream.ToArray();
        var action = "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            (byte[])[.. visible, 0, .. System.Text.Encoding.UTF8.GetBytes("finish-without-activity")]));
        return new CampaignHostCycleControlOwnerDraft(proposed.PriorStateVersion, visible, [action]);
    }

    public static ExerciseStepResult SubmitPostCombatPass(ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        CampaignHostCycleControlOwnerDraft draft;
        try { draft = QueryPostCombatPassOwnerDraft(session, submission.Audience); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (!draft.PrivateActionIds.Contains(submission.ActionId, StringComparer.Ordinal))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitPostCombatPass(session, submission.Audience, submission.ActionId);
    }

    public static CampaignHostCycleControlOwnerDraft QueryTruckConvoyPassOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Truck Convoy pass needs an owner side."),
        };
        var (release, control) = CampaignExercises.QueryTruckConvoyPassSource(session);
        var proposed = CampaignNativeTruckConvoyPassV41.Create(release, control, side,
            session.CurrentContacts.AuthorityVersion);
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignNativeTruckConvoyPassV41.CurrentContractVersion);
            writer.WriteString("campaignId", proposed.CampaignId);
            writer.WriteString("side", side.ToString());
            writer.WriteString("positionId", proposed.PositionId);
            writer.WriteString("choice", "pass");
            writer.WriteEndObject();
        }
        var visible = stream.ToArray();
        var action = "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            (byte[])[.. visible, 0, .. System.Text.Encoding.UTF8.GetBytes("pass")]));
        return new CampaignHostCycleControlOwnerDraft(control.StateVersion, visible, [action]);
    }

    public static ExerciseStepResult SubmitTruckConvoyPass(ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        CampaignHostCycleControlOwnerDraft draft;
        try { draft = QueryTruckConvoyPassOwnerDraft(session, submission.Audience); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (!draft.PrivateActionIds.Contains(submission.ActionId, StringComparer.Ordinal))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitTruckConvoyPass(session, submission.Audience, submission.ActionId);
    }

    // ActFirst/ActLast (land.phase.initiative-declaration): the side that won initiative
    // (CampaignSnapshotV11.InitiativeHolder) chooses which side acts first this operation
    // stage. Core already computes legality/replay for this (CampaignV11Preamble,
    // CampaignLegalActions.GenerateForSide/Query) -- this is a redacted Host wrapper over
    // the same generic CampaignExercises.Query/Submit path every other preamble resolve
    // step already uses, not new Core logic. No round/deadline tracking, matching
    // TruckConvoyPass/PostCombatPass/SecondReserve's own simpler (non-CampaignRoundKind) shape.
    public static CampaignHostCycleControlOwnerDraft QueryInitiativeOrderOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var result = CampaignExercises.Query(session, audience);
        if (!result.IsSuccessful || result.ActionSet is not { } set)
            throw new InvalidOperationException("Initiative order is unavailable.");
        var first = set.Candidates.OfType<ActFirstAction>().SingleOrDefault();
        var last = set.Candidates.OfType<ActLastAction>().SingleOrDefault();
        if (first is null || last is null || set.Candidates.Count != 2)
            throw new InvalidOperationException("Initiative order is unavailable.");
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("campaignId", set.CampaignId);
            writer.WriteString("positionId", set.PositionId);
            writer.WriteStartArray("choices");
            writer.WriteStringValue("act-first");
            writer.WriteStringValue("act-last");
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignHostCycleControlOwnerDraft(set.StateVersion, stream.ToArray(),
            [first.ActionId, last.ActionId]);
    }

    public static ExerciseStepResult SubmitInitiativeOrder(ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        CampaignHostCycleControlOwnerDraft draft;
        try { draft = QueryInitiativeOrderOwnerDraft(session, submission.Audience); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (!draft.PrivateActionIds.Contains(submission.ActionId, StringComparer.Ordinal))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.Submit(session, submission);
    }

    // Movement Segment Completion (land.position...movement-and-combat.movement, Operation
    // Stage 1): the fourth gap of this exact shape found auditing the real-HTTP-player chain
    // end to end (scope doc, PR #507) -- CompleteMovementSegmentAction is a real, side-level
    // (not per-element) legal candidate CampaignMovementActionDerivation.cs already produces
    // generically alongside each element's own MoveElementAction options, but the dedicated
    // "movement" HTTP stage (CampaignContactOwnerMovementCut's own MovementOffers field) is
    // typed IReadOnlyList<MoveElementAction> -- a hard constraint that can never carry it, no
    // matter how the generic set is built. This is a redacted Host wrapper over the same
    // generic CampaignExercises.Query/Submit path InitiativeOrder/ReserveDesignation already
    // use, not new Core logic, and -- same as those two -- fires before any Combat decision
    // has ever been made in the session, so it needs no #394 gap 1 sidestep.
    //
    // StopElementMovementAction (the fifth gap of this exact shape, found building the E2E
    // test itself, scope doc PR #509) shares this one opening rather than getting its own:
    // CampaignObservationV7ActionDerivation.DeriveOrdinaryMovement's own dispatch
    // (`route is null ? [Complete] : [Stop(route.RouteId)]`) makes the two strictly mutually
    // exclusive -- Complete is offered only once nobody has an in-progress route, Stop only
    // once somebody does -- so a real HTTP player querying this one stage always gets
    // whichever of the two is actually the live choice, rather than needing to guess which of
    // two separate stages to ask. (StopElementMovementAction's constructor living in
    // BreakdownActionCandidates.cs is misleading: it is not vehicle-breakdown-only -- it is
    // this same ordinary ActiveMovement/route mechanism's own "stop here instead of continuing
    // or completing the whole segment" choice, reachable the moment a real player moves one
    // step and wants to stop without first exhausting that element's entire movement budget.)
    // The query deliberately ignores every MoveElementAction present in the same legal set
    // (that side of the decision is already served by the "movement" stage); it only ever
    // exposes the single Complete or Stop candidate, whichever exists. No round/deadline
    // tracking, same reasoning as InitiativeOrder/ReserveDesignation.
    public static CampaignHostCycleControlOwnerDraft QueryMovementSegmentCompletionOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var result = CampaignExercises.Query(session, audience);
        if (!result.IsSuccessful || result.ActionSet is not { } set)
            throw new InvalidOperationException("Movement segment completion is unavailable.");
        CampaignActionCandidate? resolution = set.Candidates.OfType<CompleteMovementSegmentAction>().SingleOrDefault();
        resolution ??= set.Candidates.OfType<StopElementMovementAction>().SingleOrDefault();
        if (resolution is null)
            throw new InvalidOperationException("Movement segment completion is unavailable.");
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("campaignId", set.CampaignId);
            writer.WriteString("positionId", set.PositionId);
            writer.WriteStartArray("choices");
            writer.WriteStartObject();
            switch (resolution)
            {
                case CompleteMovementSegmentAction:
                    writer.WriteString("kind", "complete-movement-segment");
                    break;
                case StopElementMovementAction stop:
                    writer.WriteString("kind", "stop-element-movement");
                    writer.WriteString("routeId", stop.RouteId);
                    break;
            }
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return new CampaignHostCycleControlOwnerDraft(set.StateVersion, stream.ToArray(),
            [resolution.ActionId]);
    }

    public static ExerciseStepResult SubmitMovementSegmentCompletion(ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        CampaignHostCycleControlOwnerDraft draft;
        try { draft = QueryMovementSegmentCompletionOwnerDraft(session, submission.Audience); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (!draft.PrivateActionIds.Contains(submission.ActionId, StringComparer.Ordinal))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.Submit(session, submission);
    }

    // First acting side's own Reserve Designation (land.phase.reserve-designation, Operation
    // Stage 1): the same gap class as ActFirst/ActLast -- Core already computes and
    // replay-verifies DesignateReserveAction/CompleteReserveDesignationAction
    // (CampaignV11Preamble, CampaignLegalActions.GenerateForSide) via the generic
    // CampaignExercises.Query/Submit path; this is a redacted Host wrapper over it, matching
    // QuerySecondReserveOwnerDraft/SubmitSecondReserve's own shape for the SECOND side's
    // already-reachable equivalent (one DesignateReserveAction per eligible element plus one
    // CompleteReserveDesignationAction, all in a single owner opening). No round/deadline
    // tracking, same reasoning as InitiativeOrder.
    private static (CampaignHostCycleControlOwnerDraft Draft, CampaignActionCandidate[] Actions)
        CreateReserveDesignationOwnerDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var result = CampaignExercises.Query(session, audience);
        if (!result.IsSuccessful || result.ActionSet is not { } set
            || set.Candidates.Count == 0
            || set.Candidates.Any(value => value is not (DesignateReserveAction or CompleteReserveDesignationAction))
            || set.Candidates.Count(value => value is CompleteReserveDesignationAction) != 1)
            throw new InvalidOperationException("Reserve Designation is unavailable.");
        var actions = set.Candidates.ToArray();
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("campaignId", set.CampaignId);
            writer.WriteString("positionId", set.PositionId);
            writer.WriteStartArray("choices");
            foreach (var action in actions)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", action is DesignateReserveAction ? "designate" : "complete");
                if (action is DesignateReserveAction designated) writer.WriteString("elementId", designated.ElementId);
                else writer.WriteNull("elementId");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return (new CampaignHostCycleControlOwnerDraft(set.StateVersion, stream.ToArray(),
            actions.Select(action => action.ActionId).ToArray()), actions);
    }

    public static CampaignHostCycleControlOwnerDraft QueryReserveDesignationOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CreateReserveDesignationOwnerDraft(session, audience).Draft;

    public static ExerciseStepResult SubmitReserveDesignation(ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        (CampaignHostCycleControlOwnerDraft Draft, CampaignActionCandidate[] Actions) cut;
        try { cut = CreateReserveDesignationOwnerDraft(session, submission.Audience); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var action = cut.Actions.SingleOrDefault(value => value.ActionId == submission.ActionId);
        if (action is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.Submit(session, submission);
    }

    // FID-002: a source-legal Reaction window the Host cannot yet resolve into an owner
    // opening (#394 gap 3 -- see CreateReactionOwnerDraft's remarks), surfaced distinguishably
    // from "no such opening" rather than silently treated as a no-op. Public (unlike Core's own
    // internal CampaignRuleNotImplementedException) so both the Journal query path
    // (CampaignDurableAuthorityJournal.Openings.cs) and the outward HTTP layer
    // (CampaignDurableOutwardApi.cs) can catch it by name across the Cna.OrleansHost boundary.
    public sealed class CampaignReactionWindowRuleNotImplementedException : InvalidOperationException
    {
        public CampaignReactionWindowRuleNotImplementedException(string message) : base(message) { }
    }

    // Reaction window resolution (land.position...reaction, opened whenever a Movement or
    // second-side Movement crosses an eligible opponent's reaction opportunity): the same gap
    // class as InitiativeOrder/ReserveDesignation. Core computes and replay-verifies this via
    // CampaignExercises.QueryReactionWindow/SubmitReactionWindow (CampaignLegalActions.Query
    // routes to CampaignObservationV7ActionDerivation.DerivePlayer whenever
    // snapshot.ReactionWindow is not null) -- a dedicated non-generic entry point, not the
    // shared CampaignExercises.Query/Submit (see that method's own remarks: #394 gap 1's stale
    // CurrentCombatDecision gate would otherwise reject every second-side-triggered window).
    // Two-phase, like SecondMovement/SecondMovementCompletion: with no active participant yet,
    // the legal set is one MoveReactingElementAction per (opportunity, move option) plus one
    // DeclineReactionWindowAction; once a MoveReactingElementAction commits to an opportunity,
    // the legal set narrows to that opportunity's own remaining move options plus one
    // CompleteReactionParticipantAction. Re-querying this same opening after each accepted move
    // reflects whichever phase the window is currently in. No round/deadline tracking, same
    // reasoning as InitiativeOrder/ReserveDesignation.
    //
    // Ruled out of scope for this release (PR #501, tracked future design decision): #394 gap
    // 3 means ExerciseSession.CurrentSnapshot is never bridged to the second-side native
    // chain's own world/position, so a window opened by a second-side Movement (WithSecondMove)
    // is invisible to CampaignLegalActions.Query -- it still reads session.CurrentSnapshot,
    // which stays frozen at the pre-second-side state, and finds no reaction candidates there
    // even with gap 1's gate removed. CurrentSecondMove?.ReactionWindow is not null is the one
    // reliable signal this layer has that a real window exists despite that -- so rather than
    // let it surface only as an opaque "Reaction is unavailable" (indistinguishable from
    // "nothing to see here", which would violate FID-002 by silently approximating a real,
    // open decision as a no-op), that specific case throws the distinguishable
    // CampaignReactionWindowRuleNotImplementedException instead. A first-side-opened window
    // (CurrentSnapshot kept current throughout ordinary first-side Movement) is unaffected and
    // still resolves normally through this same method.
    private static (CampaignHostCycleControlOwnerDraft Draft, CampaignActionCandidate[] Actions)
        CreateReactionOwnerDraft(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var result = CampaignExercises.QueryReactionWindow(session, audience);
        if (!result.IsSuccessful || result.ActionSet is not { } set
            || set.Candidates.Count == 0
            || set.Candidates.Any(value => value is not (MoveReactingElementAction
                or DeclineReactionWindowAction or CompleteReactionParticipantAction)))
        {
            if (session.Pending is SecondMovePending && session.CurrentSecondMove?.ReactionWindow is not null)
                throw new CampaignReactionWindowRuleNotImplementedException(
                    "A reaction window opened by a second-side Movement cannot yet be " +
                    "resolved through an owner opening (#394 gap 3).");
            throw new InvalidOperationException("Reaction is unavailable.");
        }
        var actions = set.Candidates.ToArray();
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("campaignId", set.CampaignId);
            writer.WriteString("positionId", set.PositionId);
            writer.WriteStartArray("choices");
            foreach (var action in actions)
            {
                writer.WriteStartObject();
                switch (action)
                {
                    case MoveReactingElementAction move:
                        writer.WriteString("kind", "move");
                        writer.WriteString("windowId", move.WindowId);
                        writer.WriteString("opportunityId", move.OpportunityId);
                        writer.WriteString("originLocationId", move.OriginLocationId);
                        writer.WriteString("destinationLocationId", move.DestinationLocationId);
                        MovementActionJson.WriteCostBreakdown(writer, move.CostBreakdown);
                        break;
                    case DeclineReactionWindowAction decline:
                        writer.WriteString("kind", "decline");
                        writer.WriteString("windowId", decline.WindowId);
                        break;
                    case CompleteReactionParticipantAction complete:
                        writer.WriteString("kind", "complete");
                        writer.WriteString("windowId", complete.WindowId);
                        writer.WriteString("opportunityId", complete.OpportunityId);
                        break;
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return (new CampaignHostCycleControlOwnerDraft(set.StateVersion, stream.ToArray(),
            actions.Select(action => action.ActionId).ToArray()), actions);
    }

    public static CampaignHostCycleControlOwnerDraft QueryReactionOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience) =>
        CreateReactionOwnerDraft(session, audience).Draft;

    public static ExerciseStepResult SubmitReaction(ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        (CampaignHostCycleControlOwnerDraft Draft, CampaignActionCandidate[] Actions) cut;
        try { cut = CreateReactionOwnerDraft(session, submission.Audience); }
        catch (CampaignReactionWindowRuleNotImplementedException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.RuleNotImplemented); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var action = cut.Actions.SingleOrDefault(value => value.ActionId == submission.ActionId);
        if (action is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitReactionWindow(session, submission);
    }

    public static CampaignHostCycleControlOwnerDraft QueryNativeCycleControlOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience)
    {
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Cycle control needs an owner side."),
        };
        var release = CampaignExercises.QueryPendingNativeCycleControl(session);
        var cut = CampaignCycleControlOwnerCutV28.Project(release,
            session.Context, session.CurrentContacts, side);
        return new CampaignHostCycleControlOwnerDraft(release.StateVersion,
            cut.VisibleBytes, cut.ActionIds);
    }

    public static ExerciseStepResult SubmitNativeCycleControl(
        ExerciseSession session, CampaignActionSubmission submission)
    {
        CampaignNativeReserveReleaseStateV27 release;
        CampaignCycleControlOwnerCutV28 cut;
        try
        {
            release = CampaignExercises.QueryPendingNativeCycleControl(session);
            var side = submission.Audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Cycle control needs an owner side."),
            };
            cut = CampaignCycleControlOwnerCutV28.Project(release,
                session.Context, session.CurrentContacts, side);
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
        if (submission.CampaignId != release.CampaignId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != release.StateVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != release.Position.PositionId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var ordinal = Array.FindIndex(cut.ActionIds.ToArray(), value =>
            value == submission.ActionId);
        if (ordinal < 0)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitNativeCycleControl(session,
            submission.Audience, cut.Choices[ordinal], submission.ActionId);
    }

    public static CampaignHostRepeatMovementOwnerDraft QueryNativeRepeatMovementOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience)
    {
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Repeated Movement needs an owner side."),
        };
        var release = CampaignExercises.QueryNativeReserveReleaseState(session);
        // Repeat movement is only wired for the legacy first-side occurrence.
        if (session.Pending is not null)
            throw new InvalidOperationException("Repeated Movement is unavailable.");
        if ((session.CurrentPostRepeat?.Movement is not null
                || session.CurrentPostRepeat is null
                    && session.CurrentRepeatAssault?.Control is null
                    && session.CurrentRepeatMove is not null)
            || (session.CurrentPostRepeat is not null
                ? session.CurrentPostRepeat.Control
                : session.CurrentRepeatAssault?.Control ?? session.CurrentCycleControl) is not { } control)
            throw new InvalidOperationException("Repeated Movement is unavailable.");
        var cut = CampaignRepeatMovementOwnerCutV29.Project(release, control,
            session.Context, session.CurrentContacts, side);
        return new CampaignHostRepeatMovementOwnerDraft(control.StateVersion,
            cut.VisibleBytes, cut.ActionIds);
    }

    public static ExerciseStepResult SubmitNativeRepeatMovement(
        ExerciseSession session, CampaignActionSubmission submission)
    {
        CampaignHostRepeatMovementOwnerDraft draft;
        try
        {
            draft = QueryNativeRepeatMovementOwnerDraft(session,
                submission.Audience);
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != draft.AuthorityVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (!draft.PrivateActionIds.Contains(submission.ActionId, StringComparer.Ordinal))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitNativeRepeatMovement(session,
            submission.Audience, submission.ActionId);
    }

    public static CampaignHostContactCombatOwnerDraft QueryNativeRepeatCombatOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience,
        string actingElementId)
    {
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException(
                "Repeated Combat needs an owner side."),
        };
        var cut = CampaignExercises.QueryNativeRepeatCombatCut(session,
            side, actingElementId);
        return new CampaignHostContactCombatOwnerDraft(cut.AuthorityVersion,
            cut.VisibleBytes,
            cut.AssaultOffers.OrderBy(value => value.ContactHandle,
                    StringComparer.Ordinal)
                .ThenBy(value => value.ApparentLocationId,
                    StringComparer.Ordinal)
                .Select(value => value.ActionId)
                .Append(cut.FinishOffer.ActionId).ToArray());
    }

    public static ExerciseStepResult SubmitNativeRepeatCombat(
        ExerciseSession session, CampaignActionSubmission submission,
        string actingElementId)
    {
        CampaignHostContactCombatOwnerDraft draft;
        try
        {
            draft = QueryNativeRepeatCombatOwnerDraft(session,
                submission.Audience, actingElementId);
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or InvalidCampaignHistoryException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != draft.AuthorityVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (!draft.PrivateCombatActionIds.Contains(submission.ActionId,
                StringComparer.Ordinal))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitNativeRepeatCombat(session,
            submission.Audience, actingElementId, submission.ActionId);
    }

    public static CampaignActionSubmission NativeReserveReleaseCompletionCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || accepted.SuccessorSession?.CurrentReserveRelease is not { Completed: true })
            throw new ArgumentException("Accepted Reserve Release completion required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static ExerciseStepResult CompleteNativeRepeatBreakdown(
        ExerciseSession session) =>
        CampaignExercises.CompleteNativeRepeatBreakdown(session);

    public static ExerciseStepResult CompleteSecondBreakdown(ExerciseSession session) =>
        CampaignExercises.CompleteSecondBreakdown(session);

    public static CampaignActionSubmission SecondBreakdownCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.SuccessorSession?.CurrentSecondBreakdown is null)
            throw new ArgumentException("Accepted second Breakdown completion required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeRepeatBreakdownCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || accepted.SuccessorSession?.CurrentRepeatBreakdown is null)
            throw new ArgumentException("Accepted repeated Breakdown required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatSuccessorCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentRepeatAssault is { Custody: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.Successor is null
                : accepted.SuccessorSession?.CurrentCombatSuccessor is null))
            throw new ArgumentException("Accepted Combat successor required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static bool IsNativeCombatNoCustody(ExerciseSession session) =>
        (session.Pending is null && session.CurrentCombatSettlement is { PendingCaptives.Count: 0 }
            && session.CurrentCombatCustodyDisposition is null)
        || CurrentOccurrenceAssault(session) is
        {
            Settlement.PendingCaptives.Count: 0,
            Custody: null
        };

    public static bool IsNativeCombatCustodyPending(ExerciseSession session) =>
        (session.Pending is null && session.CurrentCombatSettlement is { PendingCaptives.Count: > 0 }
            && session.CurrentCombatCustodyDisposition is null)
        || CurrentOccurrenceAssault(session) is
        {
            Settlement.PendingCaptives.Count: > 0,
            Custody: null
        };

    public static ExerciseStepResult SubmitNativeCombatCustody(
        ExerciseSession session, CampaignActionSubmission submission,
        string captorElementId)
    {
        if (string.IsNullOrWhiteSpace(captorElementId))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignNativeCombatCustodyCutV25? cut;
        try { cut = CampaignExercises.QueryNativeCombatCustodyCut(session); }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
        if (cut is null || cut.CaptorElementId != captorElementId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        return CampaignExercises.SubmitNativeCombatCustody(session, submission);
    }

    public static CampaignActionSubmission NativeCombatSettlementCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted ||
            (predecessor.CurrentRepeatAssault is { PostResultRetreat: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.Settlement is null
                : accepted.SuccessorSession?.CurrentCombatSettlement is null))
            throw new ArgumentException("Accepted native settlement required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatNoCustodyCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentRepeatAssault is { Settlement: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.Custody
                    is not { ChoiceKind: "not-required" }
                : accepted.SuccessorSession?.CurrentCombatCustodyDisposition
                    is not { ChoiceKind: "not-required" }))
            throw new ArgumentException("Accepted no-custody completion required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    /// <summary>Private typed decision proof for one accepted step. It neither publishes a
    /// second transition nor supplies browser diary content.</summary>
    public static CampaignAudienceOutcomePair DecideAcceptedOutcomes(
        ExerciseSession predecessor, CampaignHostAcceptedStep acceptedStep) =>
        CampaignAudienceOutcomeSource.Decide(predecessor, acceptedStep);

    /// <summary>
    /// Re-adjudicates a trusted private transcript from its pinned creation request. The caller
    /// must verify journal order, head/fence, request indexes and checkpoint binding separately.
    /// </summary>
    public static CampaignHostImportResult Import(
        CampaignCreationRequest request,
        ReadOnlyMemory<byte> creationEvent,
        ReadOnlyMemory<byte> initialSnapshot,
        IReadOnlyList<CampaignHostAcceptedStep> steps,
        IReadOnlyList<byte[]>? initialContactRecords = null,
        IReadOnlyList<IReadOnlyList<byte[]>>? stepContactRecords = null,
        CampaignDecisionTimingConfigurationV1? timing = null) =>
        ImportCore(request, creationEvent, initialSnapshot, steps, initialContactRecords, stepContactRecords,
            timing, Cna1979SyntheticContentResolver.Instance);

    public static CampaignHostImportResult ImportWithContentResolver(
        CampaignCreationRequest request,
        ReadOnlyMemory<byte> creationEvent,
        ReadOnlyMemory<byte> initialSnapshot,
        IReadOnlyList<CampaignHostAcceptedStep> steps,
        IContentPackV6Resolver resolver,
        IReadOnlyList<byte[]>? initialContactRecords = null,
        IReadOnlyList<IReadOnlyList<byte[]>>? stepContactRecords = null,
        CampaignDecisionTimingConfigurationV1? timing = null) =>
        ImportCore(request, creationEvent, initialSnapshot, steps, initialContactRecords, stepContactRecords,
            timing, resolver ?? throw new ArgumentNullException(nameof(resolver)));

    private static CampaignHostImportResult ImportCore(
        CampaignCreationRequest request,
        ReadOnlyMemory<byte> creationEvent,
        ReadOnlyMemory<byte> initialSnapshot,
        IReadOnlyList<CampaignHostAcceptedStep> steps,
        IReadOnlyList<byte[]>? initialContactRecords,
        IReadOnlyList<IReadOnlyList<byte[]>>? stepContactRecords,
        CampaignDecisionTimingConfigurationV1? timing,
        IContentPackV6Resolver resolver)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(steps);
        if (steps.Any(step => step?.Kind is CampaignHostAcceptedStep.PregameAxisSupplyKind
            or CampaignHostAcceptedStep.PregameAxisSupplyFinishKind or CampaignHostAcceptedStep.SourceAxisDumpKind
            or CampaignHostAcceptedStep.SourceCommonwealthSupplyKind or CampaignHostAcceptedStep.SourceDummyKind
            or CampaignHostAcceptedStep.SourceMotorizationKind or CampaignHostAcceptedStep.SourceAxisGt1SupplyKind
            or CampaignHostAcceptedStep.SourceUnattachedCargoKind
            or CampaignHostAcceptedStep.SourceActionsKind
            or CampaignHostAcceptedStep.SourceCommonwealthSupplyPlanKind or CampaignHostAcceptedStep.SourceAxisSupplementKind
            or CampaignHostAcceptedStep.SourceAutoblindaKind or CampaignHostAcceptedStep.SourceCommonwealthSupplementKind
            or CampaignHostAcceptedStep.SourcePriorityMotorizationKind or CampaignHostAcceptedStep.SourcePriorityMotorizationChoiceKind
            or CampaignHostAcceptedStep.SourceAttachedInitialLoadsKind or CampaignHostAcceptedStep.SourceAttachedInitialLoadsChoiceKind
            or CampaignHostAcceptedStep.SourceInitialWrecksKind or CampaignHostAcceptedStep.SourceInitialWrecksChoiceKind
            or CampaignHostAcceptedStep.SourceAxisFreeSetupKind or CampaignHostAcceptedStep.SourceAxisFreeSetupChoiceKind
            or CampaignHostAcceptedStep.SourceAxisCorpsSetupKind or CampaignHostAcceptedStep.SourceAxisCorpsSetupChoiceKind
            or CampaignHostAcceptedStep.SourceMalettiHierarchyKind or CampaignHostAcceptedStep.SourceMalettiHierarchyChoiceKind
            or CampaignHostAcceptedStep.SourceGt2CorrectionKind or CampaignHostAcceptedStep.SourceGt2CorrectionChoiceKind
            or CampaignHostAcceptedStep.SourceCwPriorityCatalogKind or CampaignHostAcceptedStep.SourceCwPriorityCatalogChoiceKind
            or CampaignHostAcceptedStep.SourceCompactCheckpointKind
            or CampaignHostAcceptedStep.SourceFourthIndianWorldKind
            or CampaignHostAcceptedStep.SourceMatruhGroupedWorldKind
            or CampaignHostAcceptedStep.SourceMalettiGroupedWorldKind
            or CampaignHostAcceptedStep.SourceLibyanGroupedWorldKind
            or CampaignHostAcceptedStep.SourceItalianDivisionsGroupedWorldKind
            or CampaignHostAcceptedStep.SourceSeventhArmoredGroupedWorldKind
            or CampaignHostAcceptedStep.SourceSixteenthBrigadeGroupedWorldKind
            or CampaignHostAcceptedStep.SourceCatanzaroGroupedWorldKind
            or CampaignHostAcceptedStep.SourceTurnStartKind
            or CampaignHostAcceptedStep.SourceTurnInitiativeKind
            or CampaignHostAcceptedStep.SourceGt1BreakdownCompleteKind or CampaignHostAcceptedStep.SourceGt1FleetCompleteKind or CampaignHostAcceptedStep.SourceGt1ArrivalsCompleteKind or CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind or CampaignHostAcceptedStep.SourceGt1SchedulingCompleteKind
            or CampaignHostAcceptedStep.SourceGt1WeatherKind
            or CampaignHostAcceptedStep.SourceGt1Lane2AnnounceKind
            or CampaignHostAcceptedStep.SourceGt1Lane3AnnounceKind
            or CampaignHostAcceptedStep.SourceSeventhShellCorrectionKind
            or CampaignHostAcceptedStep.SourceAxisMinorGarrisonsActivationKind
            or CampaignHostAcceptedStep.SourceAxisGarrisonRemainderKind
            or CampaignHostAcceptedStep.SourceGiarabubMotorMachineGunKind
            or CampaignHostAcceptedStep.SourceCwFrontlineKind
            or CampaignHostAcceptedStep.SourceGiarabubMotorPlacementPolicyKind
            or CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonAdmissionKind
            or CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonActivationKind
            or CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderAdmissionKind
            or CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderActivationKind
            or CampaignHostAcceptedStep.SourceTripoliTankRegimentAdmissionKind
            or CampaignHostAcceptedStep.SourceTripoliTankRegimentActivationKind
            or CampaignHostAcceptedStep.SourceCwAttachedArtilleryAdmissionKind
            or CampaignHostAcceptedStep.SourceCwAttachedArtilleryActivationKind
            or CampaignHostAcceptedStep.SourceGt1MultiLanesKind
            or CampaignHostAcceptedStep.SourceMatruhFormationKind or CampaignHostAcceptedStep.SourceMatruhFormationChoiceKind
            or CampaignHostAcceptedStep.SourceCwFreeGunsKind or CampaignHostAcceptedStep.SourceCwFreeGunsChoiceKind
            or CampaignHostAcceptedStep.SourceAxisPriorityMotorizationKind or CampaignHostAcceptedStep.SourceAxisPriorityChoiceKind
            or CampaignHostAcceptedStep.SourceAxisMinorGarrisonsKind or CampaignHostAcceptedStep.SourceBattlefieldKind
            or CampaignHostAcceptedStep.SourceCairoSetupKind or CampaignHostAcceptedStep.SourceCairoUnitKind
            or CampaignHostAcceptedStep.SourceCairoWorldKind or CampaignHostAcceptedStep.SourceMalettiKind
            or CampaignHostAcceptedStep.SourceIndependentKind or CampaignHostAcceptedStep.SourceLocalMapKind
            or CampaignHostAcceptedStep.SourceExpandedIndependentKind or CampaignHostAcceptedStep.SourceAxisFreeWorldKind or CampaignHostAcceptedStep.SourceAxisCorpsWorldKind or CampaignHostAcceptedStep.SourceCertifiedMalettiWorldKind or CampaignHostAcceptedStep.SourceMatruhDetachedWorldKind or CampaignHostAcceptedStep.SourceCwPriorityWorldKind or CampaignHostAcceptedStep.SourceCwFreeGunsWorldKind))
            return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
        return ImportStarted(Create(request, resolver), creationEvent, initialSnapshot, steps,
            initialContactRecords, stepContactRecords, timing);
    }

    public static CampaignHostImportResult ImportPregameSource(
        CampaignCreationRequest request, ReadOnlyMemory<byte> creationEvent,
        ReadOnlyMemory<byte> initialSnapshot, IReadOnlyList<CampaignHostAcceptedStep> steps,
        IReadOnlyList<byte[]>? initialContactRecords = null,
        IReadOnlyList<IReadOnlyList<byte[]>>? stepContactRecords = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(steps);
        if (steps.Any(step => !IsSourceStep(step)))
            return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
        ExerciseStartResult started;
        try { started = CampaignExercises.ReplayPregameGenesis(request, creationEvent, initialSnapshot); }
        catch (Exception exception) when (exception is System.Text.Json.JsonException
            or ArgumentException or InvalidOperationException or InvalidCampaignHistoryException
            or KeyNotFoundException or FormatException or OverflowException)
        { return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidCreation); }
        return ImportStarted(started, creationEvent, initialSnapshot, steps,
            initialContactRecords, stepContactRecords, null);
    }

    private static bool IsSourceStep(CampaignHostAcceptedStep? step) => step is not null && (step.Kind, step.ActingElementId) is
            ((CampaignHostAcceptedStep.PregameAxisSupplyKind, "land.pregame-axis-supply") or
             (CampaignHostAcceptedStep.PregameAxisSupplyFinishKind, "land.pregame-axis-supply-finish") or
             (CampaignHostAcceptedStep.SourceAxisDumpKind, "land.graziani-axis-dumps") or
             (CampaignHostAcceptedStep.SourceCommonwealthSupplyKind, "land.graziani-commonwealth-supply") or
             (CampaignHostAcceptedStep.SourceDummyKind, "land.graziani-dummy-groups") or
             (CampaignHostAcceptedStep.SourceMotorizationKind, "land.graziani-motorization") or
             (CampaignHostAcceptedStep.SourceUnattachedCargoKind, "land.graziani-unattached-cargo") or
             (CampaignHostAcceptedStep.SourceActionsKind, "land.graziani-source-actions") or
             (CampaignHostAcceptedStep.SourceAxisGt1SupplyKind, "land.graziani-axis-gt1-supply") or
             (CampaignHostAcceptedStep.SourceCommonwealthSupplyPlanKind, "land.graziani-commonwealth-supply-plan") or
             (CampaignHostAcceptedStep.SourceAxisSupplementKind, null) or
             (CampaignHostAcceptedStep.SourceAutoblindaKind, "land.graziani-axis-autoblinda") or
             (CampaignHostAcceptedStep.SourceCommonwealthSupplementKind, null) or
             (CampaignHostAcceptedStep.SourcePriorityMotorizationKind, null) or
             (CampaignHostAcceptedStep.SourcePriorityMotorizationChoiceKind, "land.graziani-commonwealth-priority-motorization") or
             (CampaignHostAcceptedStep.SourceAttachedInitialLoadsKind, null) or
             (CampaignHostAcceptedStep.SourceAttachedInitialLoadsChoiceKind, "land.graziani-commonwealth-attached-initial-loads") or
             (CampaignHostAcceptedStep.SourceInitialWrecksKind, null) or
             (CampaignHostAcceptedStep.SourceInitialWrecksChoiceKind, "land.graziani-commonwealth-initial-wrecks") or
             (CampaignHostAcceptedStep.SourceAxisFreeSetupKind, null) or
             (CampaignHostAcceptedStep.SourceAxisFreeSetupChoiceKind, "land.graziani-axis-free-setup") or
             (CampaignHostAcceptedStep.SourceAxisCorpsSetupKind, null) or
             (CampaignHostAcceptedStep.SourceAxisCorpsSetupChoiceKind, "land.graziani-axis-corps-setup") or
             (CampaignHostAcceptedStep.SourceMalettiHierarchyKind, null) or
             (CampaignHostAcceptedStep.SourceMalettiHierarchyChoiceKind, "land.graziani-axis-maletti-hierarchy") or
             (CampaignHostAcceptedStep.SourceGt2CorrectionKind, null) or
             (CampaignHostAcceptedStep.SourceGt2CorrectionChoiceKind, "land.graziani-axis-gt2-correction") or
             (CampaignHostAcceptedStep.SourceCwPriorityCatalogKind, null) or
             (CampaignHostAcceptedStep.SourceCompactCheckpointKind, null) or
             (CampaignHostAcceptedStep.SourceFourthIndianWorldKind, null) or
             (CampaignHostAcceptedStep.SourceMatruhGroupedWorldKind, null) or
             (CampaignHostAcceptedStep.SourceMalettiGroupedWorldKind, null) or
             (CampaignHostAcceptedStep.SourceLibyanGroupedWorldKind, null) or
             (CampaignHostAcceptedStep.SourceItalianDivisionsGroupedWorldKind, null) or
             (CampaignHostAcceptedStep.SourceSeventhArmoredGroupedWorldKind, null) or
             (CampaignHostAcceptedStep.SourceSixteenthBrigadeGroupedWorldKind, null) or
             (CampaignHostAcceptedStep.SourceCatanzaroGroupedWorldKind, null) or
             (CampaignHostAcceptedStep.SourceTurnStartKind, null) or
             (CampaignHostAcceptedStep.SourceTurnInitiativeKind, null) or
             (CampaignHostAcceptedStep.SourceGt1BreakdownCompleteKind, null) or
             (CampaignHostAcceptedStep.SourceGt1FleetCompleteKind, null) or
             (CampaignHostAcceptedStep.SourceGt1ArrivalsCompleteKind, null) or
             (CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind, null) or
             (CampaignHostAcceptedStep.SourceGt1SchedulingCompleteKind, null) or
             (CampaignHostAcceptedStep.SourceGt1WeatherKind, null) or
             (CampaignHostAcceptedStep.SourceGt1Lane2AnnounceKind, null) or
             (CampaignHostAcceptedStep.SourceGt1Lane3AnnounceKind, null) or
             (CampaignHostAcceptedStep.SourceSeventhShellCorrectionKind, null) or
             (CampaignHostAcceptedStep.SourceAxisMinorGarrisonsActivationKind, null) or
             (CampaignHostAcceptedStep.SourceAxisGarrisonRemainderKind, null) or
             (CampaignHostAcceptedStep.SourceGiarabubMotorMachineGunKind, null) or
             (CampaignHostAcceptedStep.SourceCwFrontlineKind, null) or
             (CampaignHostAcceptedStep.SourceGiarabubMotorPlacementPolicyKind, null) or
             (CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonAdmissionKind, null) or
             (CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonActivationKind, null) or
             (CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderAdmissionKind, null) or
             (CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderActivationKind, null) or
             (CampaignHostAcceptedStep.SourceTripoliTankRegimentAdmissionKind, null) or
             (CampaignHostAcceptedStep.SourceTripoliTankRegimentActivationKind, null) or
             (CampaignHostAcceptedStep.SourceCwAttachedArtilleryAdmissionKind, null) or
             (CampaignHostAcceptedStep.SourceCwAttachedArtilleryActivationKind, null) or
             (CampaignHostAcceptedStep.SourceGt1MultiLanesKind, null) or
             (CampaignHostAcceptedStep.SourceCwPriorityCatalogChoiceKind, "land.graziani-commonwealth-priority-catalog") or
             (CampaignHostAcceptedStep.SourceMatruhFormationKind, null) or
             (CampaignHostAcceptedStep.SourceMatruhFormationChoiceKind, "land.graziani-commonwealth-matruh-formation") or
             (CampaignHostAcceptedStep.SourceCwFreeGunsKind, null) or
             (CampaignHostAcceptedStep.SourceCwFreeGunsChoiceKind, "land.graziani-commonwealth-free-guns") or
             (CampaignHostAcceptedStep.SourceAxisPriorityMotorizationKind, null) or
             (CampaignHostAcceptedStep.SourceAxisPriorityChoiceKind, "land.graziani-axis-priority-motorization") or
             (CampaignHostAcceptedStep.SourceAxisMinorGarrisonsKind, null) or
             (CampaignHostAcceptedStep.SourceBattlefieldKind, null) or
             (CampaignHostAcceptedStep.SourceCairoSetupKind, null) or
             (CampaignHostAcceptedStep.SourceCairoUnitKind, "land.graziani-commonwealth-cairo-setup") or
             (CampaignHostAcceptedStep.SourceCairoWorldKind, null) or
             (CampaignHostAcceptedStep.SourceMalettiKind, null) or
             (CampaignHostAcceptedStep.SourceIndependentKind, null) or
             (CampaignHostAcceptedStep.SourceLocalMapKind, null) or
             (CampaignHostAcceptedStep.SourceExpandedIndependentKind, null) or
             (CampaignHostAcceptedStep.SourceAxisFreeWorldKind, null) or
             (CampaignHostAcceptedStep.SourceAxisCorpsWorldKind, null) or
             (CampaignHostAcceptedStep.SourceCertifiedMalettiWorldKind, null) or
             (CampaignHostAcceptedStep.SourceMatruhDetachedWorldKind, null) or
             (CampaignHostAcceptedStep.SourceCwPriorityWorldKind, null) or
             (CampaignHostAcceptedStep.SourceCwFreeGunsWorldKind, null));

    /// <summary>Re-adjudicates source journal rows lazily; caller verifies row chain and head.</summary>
    public static CampaignHostImportResult ImportPregameSourceStream(
        CampaignCreationRequest request, ReadOnlyMemory<byte> creationEvent, ReadOnlyMemory<byte> initialSnapshot,
        IEnumerable<(CampaignHostAcceptedStep Step, IReadOnlyList<byte[]> Contacts)> steps,
        IReadOnlyList<byte[]> initialContactRecords)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(initialContactRecords);
        ExerciseStartResult started;
        try { started = CampaignExercises.ReplayPregameGenesis(request, creationEvent, initialSnapshot); }
        catch (Exception exception) when (exception is System.Text.Json.JsonException
            or ArgumentException or InvalidOperationException or InvalidCampaignHistoryException
            or KeyNotFoundException or FormatException or OverflowException)
        { return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidCreation); }
        return ImportStartedStream(started, creationEvent, initialSnapshot,
            steps.Select(value => (value.Step, (IReadOnlyList<byte[]>?)value.Contacts)), initialContactRecords, null, true);
    }

    private static CampaignHostImportResult ImportStarted(
        ExerciseStartResult started, ReadOnlyMemory<byte> creationEvent,
        ReadOnlyMemory<byte> initialSnapshot, IReadOnlyList<CampaignHostAcceptedStep> steps,
        IReadOnlyList<byte[]>? initialContactRecords,
        IReadOnlyList<IReadOnlyList<byte[]>>? stepContactRecords,
        CampaignDecisionTimingConfigurationV1? timing)
    {
        ArgumentNullException.ThrowIfNull(steps);
        if ((initialContactRecords is null) != (stepContactRecords is null)
            || (stepContactRecords is not null && stepContactRecords.Count != steps.Count))
            return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
        return ImportStartedStream(started, creationEvent, initialSnapshot,
            steps.Select((step, index) => (step, stepContactRecords?[index])), initialContactRecords, timing, false);
    }

    /// <summary>
    /// Re-adjudicates only the tail of a trusted private transcript, starting from an
    /// already-constructed clean-boundary <see cref="ExerciseSession"/> seed (e.g. a verified
    /// snapshot resumed by a caller elsewhere) instead of a fresh <see cref="Create"/> call.
    /// The caller is responsible for having independently verified <paramref name="seedSession"/>
    /// itself and for passing only the steps after the seed's position — this method performs no
    /// deserialization and does not re-derive the seed from anything. <paramref name="steps"/>
    /// still goes through the exact same admission switch as <see cref="ImportStartedStream"/>'s
    /// from-genesis replay (via the shared <see cref="AdmitAcceptedStepsStream"/>), so resuming
    /// changes only where the fold starts, not how each subsequent step is admitted.
    /// </summary>
    public static CampaignHostImportResult ImportResumed(
        ExerciseSession seedSession,
        IReadOnlyList<CampaignHostAcceptedStep> steps,
        IReadOnlyList<byte[]>? initialContactRecords = null,
        IReadOnlyList<IReadOnlyList<byte[]>>? stepContactRecords = null,
        CampaignDecisionTimingConfigurationV1? timing = null)
    {
        ArgumentNullException.ThrowIfNull(seedSession);
        ArgumentNullException.ThrowIfNull(steps);
        if ((initialContactRecords is null) != (stepContactRecords is null)
            || (stepContactRecords is not null && stepContactRecords.Count != steps.Count))
            return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
        return AdmitAcceptedStepsStream(seedSession,
            steps.Select((step, index) => (step, stepContactRecords?[index])), initialContactRecords, timing, false);
    }

    private static CampaignHostImportResult ImportStartedStream(
        ExerciseStartResult started, ReadOnlyMemory<byte> creationEvent, ReadOnlyMemory<byte> initialSnapshot,
        IEnumerable<(CampaignHostAcceptedStep Step, IReadOnlyList<byte[]>? Contacts)> steps,
        IReadOnlyList<byte[]>? initialContactRecords, CampaignDecisionTimingConfigurationV1? timing, bool sourceOnly)
    {
        if (!started.IsStarted
            || !creationEvent.Span.SequenceEqual(started.CreationEventBytes)
            || !initialSnapshot.Span.SequenceEqual(started.InitialSnapshotBytes))
            return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidCreation);

        return AdmitAcceptedStepsStream(started.Session!, steps, initialContactRecords, timing, sourceOnly);
    }

    /// <summary>
    /// Folds <paramref name="steps"/> forward from <paramref name="session"/> through the shared
    /// step-admission switch, used identically by a from-genesis replay
    /// (<see cref="ImportStartedStream"/>, seeded by a fresh <see cref="Create"/> call) and by a
    /// resumed replay (<see cref="ImportResumed"/>, seeded by an already-constructed session).
    /// </summary>
    private static CampaignHostImportResult AdmitAcceptedStepsStream(
        ExerciseSession session,
        IEnumerable<(CampaignHostAcceptedStep Step, IReadOnlyList<byte[]>? Contacts)> steps,
        IReadOnlyList<byte[]>? initialContactRecords, CampaignDecisionTimingConfigurationV1? timing, bool sourceOnly)
    {
        if (initialContactRecords is not null
            && !ContactRecordsMatch(session.CurrentContacts.Records, initialContactRecords))
            return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidCreation);
        foreach (var item in steps)
        {
            var step = item.Step;
            if (step is null || (sourceOnly && (!IsSourceStep(step) || item.Contacts is null)))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            CampaignActionSubmission submission;
            try
            {
                submission = CampaignActionSubmissionSerializer.DeserializeCanonical(
                    step.CanonicalCommand);
            }
            catch (System.Text.Json.JsonException)
            {
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            }

            if (step.Kind is not (CampaignHostAcceptedStep.LegacyKind
                or CampaignHostAcceptedStep.PregameAxisSupplyKind
                or CampaignHostAcceptedStep.PregameAxisSupplyFinishKind
                or CampaignHostAcceptedStep.SourceAxisDumpKind
                or CampaignHostAcceptedStep.SourceCommonwealthSupplyKind
                or CampaignHostAcceptedStep.SourceDummyKind
                or CampaignHostAcceptedStep.SourceMotorizationKind
                or CampaignHostAcceptedStep.SourceUnattachedCargoKind
                or CampaignHostAcceptedStep.SourceActionsKind
                or CampaignHostAcceptedStep.SourceAxisGt1SupplyKind
                or CampaignHostAcceptedStep.SourceCommonwealthSupplyPlanKind
                or CampaignHostAcceptedStep.SourceAxisSupplementKind
                or CampaignHostAcceptedStep.SourceAutoblindaKind
                or CampaignHostAcceptedStep.SourceCommonwealthSupplementKind
                or CampaignHostAcceptedStep.SourcePriorityMotorizationKind
                or CampaignHostAcceptedStep.SourcePriorityMotorizationChoiceKind
                or CampaignHostAcceptedStep.SourceAttachedInitialLoadsKind
                or CampaignHostAcceptedStep.SourceAttachedInitialLoadsChoiceKind
                or CampaignHostAcceptedStep.SourceInitialWrecksKind
                or CampaignHostAcceptedStep.SourceInitialWrecksChoiceKind
                or CampaignHostAcceptedStep.SourceAxisFreeSetupKind
                or CampaignHostAcceptedStep.SourceAxisFreeSetupChoiceKind
                or CampaignHostAcceptedStep.SourceAxisCorpsSetupKind
                or CampaignHostAcceptedStep.SourceAxisCorpsSetupChoiceKind
                or CampaignHostAcceptedStep.SourceMalettiHierarchyKind
                or CampaignHostAcceptedStep.SourceMalettiHierarchyChoiceKind
                or CampaignHostAcceptedStep.SourceGt2CorrectionKind
                or CampaignHostAcceptedStep.SourceGt2CorrectionChoiceKind
                or CampaignHostAcceptedStep.SourceCwPriorityCatalogKind
                or CampaignHostAcceptedStep.SourceCompactCheckpointKind
                or CampaignHostAcceptedStep.SourceFourthIndianWorldKind
            or CampaignHostAcceptedStep.SourceMatruhGroupedWorldKind
            or CampaignHostAcceptedStep.SourceMalettiGroupedWorldKind
            or CampaignHostAcceptedStep.SourceLibyanGroupedWorldKind
            or CampaignHostAcceptedStep.SourceItalianDivisionsGroupedWorldKind
            or CampaignHostAcceptedStep.SourceSeventhArmoredGroupedWorldKind
            or CampaignHostAcceptedStep.SourceSixteenthBrigadeGroupedWorldKind
            or CampaignHostAcceptedStep.SourceCatanzaroGroupedWorldKind
            or CampaignHostAcceptedStep.SourceTurnStartKind
            or CampaignHostAcceptedStep.SourceTurnInitiativeKind
            or CampaignHostAcceptedStep.SourceGt1BreakdownCompleteKind or CampaignHostAcceptedStep.SourceGt1FleetCompleteKind or CampaignHostAcceptedStep.SourceGt1ArrivalsCompleteKind or CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind or CampaignHostAcceptedStep.SourceGt1SchedulingCompleteKind
            or CampaignHostAcceptedStep.SourceGt1WeatherKind
            or CampaignHostAcceptedStep.SourceGt1Lane2AnnounceKind
            or CampaignHostAcceptedStep.SourceGt1Lane3AnnounceKind
            or CampaignHostAcceptedStep.SourceSeventhShellCorrectionKind
            or CampaignHostAcceptedStep.SourceAxisMinorGarrisonsActivationKind
            or CampaignHostAcceptedStep.SourceAxisGarrisonRemainderKind
            or CampaignHostAcceptedStep.SourceGiarabubMotorMachineGunKind
            or CampaignHostAcceptedStep.SourceCwFrontlineKind
            or CampaignHostAcceptedStep.SourceGiarabubMotorPlacementPolicyKind
            or CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonAdmissionKind
            or CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonActivationKind
            or CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderAdmissionKind
            or CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderActivationKind
            or CampaignHostAcceptedStep.SourceTripoliTankRegimentAdmissionKind
            or CampaignHostAcceptedStep.SourceTripoliTankRegimentActivationKind
            or CampaignHostAcceptedStep.SourceCwAttachedArtilleryAdmissionKind
            or CampaignHostAcceptedStep.SourceCwAttachedArtilleryActivationKind
            or CampaignHostAcceptedStep.SourceGt1MultiLanesKind
                or CampaignHostAcceptedStep.SourceCwPriorityCatalogChoiceKind
                or CampaignHostAcceptedStep.SourceMatruhFormationKind
                or CampaignHostAcceptedStep.SourceMatruhFormationChoiceKind
                or CampaignHostAcceptedStep.SourceCwFreeGunsKind
                or CampaignHostAcceptedStep.SourceCwFreeGunsChoiceKind
                or CampaignHostAcceptedStep.SourceAxisPriorityMotorizationKind
                or CampaignHostAcceptedStep.SourceAxisPriorityChoiceKind
                or CampaignHostAcceptedStep.SourceAxisMinorGarrisonsKind
                or CampaignHostAcceptedStep.SourceBattlefieldKind
                or CampaignHostAcceptedStep.SourceCairoSetupKind
                or CampaignHostAcceptedStep.SourceCairoUnitKind
                or CampaignHostAcceptedStep.SourceCairoWorldKind
                or CampaignHostAcceptedStep.SourceMalettiKind
                or CampaignHostAcceptedStep.SourceIndependentKind
                or CampaignHostAcceptedStep.SourceLocalMapKind
                or CampaignHostAcceptedStep.SourceExpandedIndependentKind
                or CampaignHostAcceptedStep.SourceAxisFreeWorldKind
                or CampaignHostAcceptedStep.SourceAxisCorpsWorldKind
                or CampaignHostAcceptedStep.SourceCertifiedMalettiWorldKind
                or CampaignHostAcceptedStep.SourceMatruhDetachedWorldKind
                or CampaignHostAcceptedStep.SourceCwPriorityWorldKind
                or CampaignHostAcceptedStep.SourceCwFreeGunsWorldKind
                or CampaignHostAcceptedStep.ContactMovementKind
                or CampaignHostAcceptedStep.ContactInitiativeOrderKind
                or CampaignHostAcceptedStep.ContactReserveDesignationKind
                or CampaignHostAcceptedStep.ContactReactionKind
                or CampaignHostAcceptedStep.ContactMovementSegmentCompletionKind
                or CampaignHostAcceptedStep.ContactCombatSelectionKind
                or CampaignHostAcceptedStep.NativeCombatNoAttackKind
                or CampaignHostAcceptedStep.NativeSecondCombatNoAttackKind
                or CampaignHostAcceptedStep.NativeCombatBarrageKind
                or CampaignHostAcceptedStep.ContactRbaDeclineKind
                or CampaignHostAcceptedStep.ContactRbaMoveKind
                or CampaignHostAcceptedStep.ContactForceSealKind
                or CampaignHostAcceptedStep.NativeCombatForceCompletionKind
                or CampaignHostAcceptedStep.NativeCombatForceCancellationKind
                or CampaignHostAcceptedStep.NativeCombatRbaCancellationKind
                or CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind
                or CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind
                or CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind
                or CampaignHostAcceptedStep.NativeCycleControlTimeoutKind
                or CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind
                or CampaignHostAcceptedStep.NativeCombatAntiArmorKind
                or CampaignHostAcceptedStep.NativeCombatCancelledAssaultKind
                or CampaignHostAcceptedStep.NativeCombatAssaultResultKind
                or CampaignHostAcceptedStep.ContactPostAssaultRetreatKind
                or CampaignHostAcceptedStep.NativeCombatNoPostAssaultRetreatKind
                or CampaignHostAcceptedStep.NativeCombatSettlementKind
                or CampaignHostAcceptedStep.NativeCombatNoCustodyKind
                or CampaignHostAcceptedStep.NativeCombatSuccessorKind
                or CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind
                or CampaignHostAcceptedStep.ContactReserveReleaseChoiceKind
                or CampaignHostAcceptedStep.ContactCycleControlKind
                or CampaignHostAcceptedStep.ContactTruckConvoyPassKind
                or CampaignHostAcceptedStep.ContactPostCombatPassKind
                or CampaignHostAcceptedStep.ContactSecondReserveKind or CampaignHostAcceptedStep.ContactStageEntryInitiativeDeclarationKind
                or CampaignHostAcceptedStep.ContactSecondMovementKind
                or CampaignHostAcceptedStep.ContactSecondMovementCompletionKind
                or CampaignHostAcceptedStep.NativeSecondBreakdownKind
                or CampaignHostAcceptedStep.NativeStageEntryWeatherKind
                or CampaignHostAcceptedStep.NativeStageEntryOrganizationKind
                or CampaignHostAcceptedStep.NativeStageEntryNavalConvoyArrivalKind
                or CampaignHostAcceptedStep.NativeStageEntryFleetAssignmentKind
                or CampaignHostAcceptedStep.NativeStageEntryFleetRepairKind
                or CampaignHostAcceptedStep.NativeStageEntryBreakdownKind
                or CampaignHostAcceptedStep.NativeStageEntrySecondBreakdownKind
                or CampaignHostAcceptedStep.ContactStageEntryReserveKind
                or CampaignHostAcceptedStep.ContactStageEntryMovementKind
                or CampaignHostAcceptedStep.ContactStageEntryMovementCompletionKind
                or CampaignHostAcceptedStep.ContactRepeatMovementKind
                or CampaignHostAcceptedStep.NativeRepeatBreakdownKind
                or CampaignHostAcceptedStep.ContactRepeatCombatSelectionKind
                or CampaignHostAcceptedStep.NativeRepeatCombatNoAttackKind
                or CampaignHostAcceptedStep.ContactCombatCustodyKind))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            CampaignNativeCombatForceCancellationV19? forceCancellation = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatForceCancellationKind)
            {
                if (step.EventRecords.Count != 1)
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try
                {
                    forceCancellation = CampaignNativeCombatForceCancellationV19.Deserialize(
                        step.EventRecords[0]);
                    if (timing is null && (forceCancellation.Reason != "force-unavailable"
                        || forceCancellation.ConfigurationHash
                            != NoTimingForceCancellationHash))
                        return CampaignHostImportResult.Rejected(
                            CampaignHostImportRejectionReason.InvalidStep);
                }
                catch (System.Text.Json.JsonException)
                {
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                }
            }
            CampaignNativeCombatRbaCancellationV33? rbaCancellation = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatRbaCancellationKind)
            {
                if (step.EventRecords.Count != 1)
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try
                {
                    rbaCancellation = CampaignNativeCombatRbaCancellationV33.Deserialize(step.EventRecords[0]);
                    if (timing is null && (rbaCancellation.Reason != "rba-unavailable"
                        || rbaCancellation.ConfigurationHash != NoTimingRbaCancellationHash))
                        return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                }
                catch (System.Text.Json.JsonException)
                {
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                }
            }
            ReadOnlyMemory<byte>? retainedLegacySelection = null;
            if (step.Kind == CampaignHostAcceptedStep.ContactCombatSelectionKind)
            {
                if (step.EventRecords.Count != 1)
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                // #394: a second-side decision serializes as
                // CampaignNativeSecondCombatDecisionV47 -- a different,
                // deliberately non-legacy shape (see that type's remarks).
                // The legacy-contract-version check below only applies to
                // genuine first-side events; without this, a second-side
                // event fails CampaignNativeCombatDecisionV12.Deserialize
                // (wrong eventType) and the existing catch below rejects the
                // whole step as InvalidStep instead of just leaving
                // retainedLegacySelection null (there is no such thing as a
                // "legacy" second-side event -- the mechanism is net new).
                var isSecondSideDecision = false;
                try
                {
                    _ = CampaignNativeSecondCombatDecisionV47.Deserialize(step.EventRecords[0]);
                    isSecondSideDecision = true;
                }
                catch (System.Text.Json.JsonException) { }
                if (!isSecondSideDecision)
                {
                    try
                    {
                        var recorded = CampaignNativeCombatDecisionV12.Deserialize(step.EventRecords[0]);
                        if (recorded.ContractVersion == CampaignNativeCombatDecisionV12.CurrentContractVersion)
                            retainedLegacySelection = step.EventRecords[0];
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                    }
                }
            }
            CampaignSelectionTimeoutEvidenceV38? selectionTimeout = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind)
            {
                if (step.EventRecords.Count != 1) return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try { selectionTimeout = CampaignSelectionTimeoutEvidenceV38.Deserialize(step.EventRecords[0]); }
                catch (System.Text.Json.JsonException) { return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep); }
                if (selectionTimeout.ConfigurationHash != (timing?.Hash ?? NoTimingSelectionTimeoutHash))
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            }
            CampaignNativeCombatRetreatTimeoutV37? retreatTimeout = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind)
            {
                if (step.EventRecords.Count != 1) return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try { retreatTimeout = CampaignNativeCombatRetreatTimeoutV37.Deserialize(step.EventRecords[0]); }
                catch (System.Text.Json.JsonException) { return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep); }
                if (retreatTimeout.ConfigurationHash != (timing?.Hash ?? NoTimingRetreatTimeoutHash))
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            }
            CampaignNativeCombatCustodyTimeoutV36? custodyTimeout = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind)
            {
                if (step.EventRecords.Count != 1) return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try { custodyTimeout = CampaignNativeCombatCustodyTimeoutV36.Deserialize(step.EventRecords[0]); }
                catch (System.Text.Json.JsonException) { return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep); }
                if (custodyTimeout.ConfigurationHash != (timing?.Hash ?? NoTimingCustodyTimeoutHash))
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            }
            CampaignNativeReserveReleaseTimeoutV35? reserveTimeout = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind)
            {
                if (step.EventRecords.Count != 1)
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try { reserveTimeout = CampaignNativeReserveReleaseTimeoutV35.Deserialize(step.EventRecords[0]); }
                catch (System.Text.Json.JsonException)
                { return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep); }
                if (reserveTimeout.ConfigurationHash != (timing?.Hash ?? NoTimingReserveReleaseTimeoutHash))
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            }
            CampaignNativeCycleControlTimeoutV34? cycleTimeout = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeCycleControlTimeoutKind)
            {
                if (step.EventRecords.Count != 1)
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try { cycleTimeout = CampaignNativeCycleControlTimeoutV34.Deserialize(step.EventRecords[0]); }
                catch (System.Text.Json.JsonException)
                { return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep); }
                if (cycleTimeout.ConfigurationHash != (timing?.Hash ?? NoTimingCycleControlTimeoutHash))
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            }
            var accepted = step.Kind switch
            {
                CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind => CampaignExercises.TimeoutNativeCombatSelection(session,
                    timing, selectionTimeout!.OpenedAtUnixMilliseconds, selectionTimeout.DeadlineUnixMilliseconds,
                    selectionTimeout.HighWaterUnixMilliseconds, selectionTimeout.CancelledAtUnixMilliseconds, selectionTimeout.Reason,
                    legacyInitiativeOwner: selectionTimeout.ContractVersion == 38),
                CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind => TimeoutNativeCombatRetreat(session,
                    timing, retreatTimeout!.OpenedAtUnixMilliseconds, retreatTimeout.DeadlineUnixMilliseconds,
                    retreatTimeout.HighWaterUnixMilliseconds, retreatTimeout.CancelledAtUnixMilliseconds, retreatTimeout.Reason),
                CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind => TimeoutNativeCombatCustody(session,
                    timing, custodyTimeout!.OpenedAtUnixMilliseconds, custodyTimeout.DeadlineUnixMilliseconds,
                    custodyTimeout.HighWaterUnixMilliseconds, custodyTimeout.CancelledAtUnixMilliseconds, custodyTimeout.Reason),
                CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind => TimeoutNativeReserveRelease(session,
                    timing, reserveTimeout!.OpenedAtUnixMilliseconds, reserveTimeout.DeadlineUnixMilliseconds,
                    reserveTimeout.HighWaterUnixMilliseconds, reserveTimeout.CancelledAtUnixMilliseconds, reserveTimeout.Reason),
                CampaignHostAcceptedStep.NativeCycleControlTimeoutKind => TimeoutNativeCycleControl(session,
                    timing, cycleTimeout!.OpenedAtUnixMilliseconds, cycleTimeout.DeadlineUnixMilliseconds,
                    cycleTimeout.HighWaterUnixMilliseconds, cycleTimeout.CancelledAtUnixMilliseconds, cycleTimeout.Reason),
                CampaignHostAcceptedStep.LegacyKind => Submit(session, submission),
                CampaignHostAcceptedStep.PregameAxisSupplyKind => Submit(session, submission),
                CampaignHostAcceptedStep.PregameAxisSupplyFinishKind => FinishPregameAxisSupply(session, submission),
                CampaignHostAcceptedStep.SourceAxisDumpKind => PlaceSourceAxisDump(session, submission),
                CampaignHostAcceptedStep.SourceCommonwealthSupplyKind => PlaceSourceCommonwealthSupply(session, submission),
                CampaignHostAcceptedStep.SourceDummyKind => DecideSourceDummy(session, submission),
                CampaignHostAcceptedStep.SourceMotorizationKind => AdjustSourceMotorization(session, submission),
                CampaignHostAcceptedStep.SourceUnattachedCargoKind => SubmitSourceUnattachedCargo(session, submission),
                CampaignHostAcceptedStep.SourceActionsKind => Submit(session, submission),
                CampaignHostAcceptedStep.SourceAxisGt1SupplyKind => SubmitSourceAxisGt1Supply(session, submission),
                CampaignHostAcceptedStep.SourceCommonwealthSupplyPlanKind => SubmitSourceCommonwealthSupplyPlan(session, submission),
                CampaignHostAcceptedStep.SourceAxisSupplementKind => CampaignExercises.AdmitSourceAxisSupplement(session),
                CampaignHostAcceptedStep.SourceAutoblindaKind => SubmitSourceAutoblinda(session, submission),
                CampaignHostAcceptedStep.SourceCommonwealthSupplementKind => CampaignExercises.AdmitSourceCommonwealthSupplement(session),
                CampaignHostAcceptedStep.SourcePriorityMotorizationKind => CampaignExercises.AdmitSourcePriorityMotorization(session),
                CampaignHostAcceptedStep.SourcePriorityMotorizationChoiceKind => AdjustSourcePriorityMotorization(session, submission),
                CampaignHostAcceptedStep.SourceAttachedInitialLoadsKind => CampaignExercises.AdmitSourceAttachedInitialLoads(session),
                CampaignHostAcceptedStep.SourceAttachedInitialLoadsChoiceKind => SubmitSourceAttachedInitialLoads(session, submission),
                CampaignHostAcceptedStep.SourceInitialWrecksKind => CampaignExercises.AdmitSourceInitialWrecks(session),
                CampaignHostAcceptedStep.SourceInitialWrecksChoiceKind => PlaceSourceInitialWreck(session, submission),
                CampaignHostAcceptedStep.SourceAxisFreeSetupKind => CampaignExercises.AdmitSourceAxisFreeSetup(session),
                CampaignHostAcceptedStep.SourceAxisFreeSetupChoiceKind => PlaceSourceAxisFreeUnit(session, submission),
                CampaignHostAcceptedStep.SourceAxisCorpsSetupKind => CampaignExercises.AdmitSourceAxisCorpsSetup(session),
                CampaignHostAcceptedStep.SourceAxisCorpsSetupChoiceKind => PlaceSourceAxisCorpsUnit(session, submission),
                CampaignHostAcceptedStep.SourceMalettiHierarchyKind => CampaignExercises.AdmitSourceMalettiHierarchy(session),
                CampaignHostAcceptedStep.SourceMalettiHierarchyChoiceKind => SubmitSourceMalettiHierarchy(session, submission),
                CampaignHostAcceptedStep.SourceGt2CorrectionKind => CampaignExercises.AdmitSourceGt2Correction(session),
                CampaignHostAcceptedStep.SourceGt2CorrectionChoiceKind => SubmitSourceGt2Correction(session, submission),
                CampaignHostAcceptedStep.SourceCwPriorityCatalogKind => CampaignExercises.AdmitSourceCwPriorityCatalog(session),
                CampaignHostAcceptedStep.SourceCompactCheckpointKind => CampaignExercises.AdmitSourceCompactCheckpoint(session),
                CampaignHostAcceptedStep.SourceFourthIndianWorldKind => CampaignExercises.AdmitSourceFourthIndianWorld(session),
                CampaignHostAcceptedStep.SourceMatruhGroupedWorldKind => CampaignExercises.AdmitSourceMatruhGroupedWorld(session),
                CampaignHostAcceptedStep.SourceMalettiGroupedWorldKind => CampaignExercises.AdmitSourceMalettiGroupedWorld(session),
                CampaignHostAcceptedStep.SourceLibyanGroupedWorldKind => CampaignExercises.AdmitSourceLibyanGroupedWorld(session),
                CampaignHostAcceptedStep.SourceItalianDivisionsGroupedWorldKind => CampaignExercises.AdmitSourceItalianDivisionsGroupedWorld(session),
                CampaignHostAcceptedStep.SourceSeventhArmoredGroupedWorldKind => CampaignExercises.AdmitSourceSeventhArmoredGroupedWorld(session),
                CampaignHostAcceptedStep.SourceSixteenthBrigadeGroupedWorldKind => CampaignExercises.AdmitSourceSixteenthBrigadeGroupedWorld(session),
                CampaignHostAcceptedStep.SourceCatanzaroGroupedWorldKind => CampaignExercises.AdmitSourceCatanzaroGroupedWorld(session),
                CampaignHostAcceptedStep.SourceTurnStartKind => CampaignExercises.StartSourceTurn(session, SourceTurnStartVersion(step)),
                CampaignHostAcceptedStep.SourceTurnInitiativeKind => ResolveSourceInitiative(session),
                CampaignHostAcceptedStep.SourceGt1BreakdownCompleteKind => CompleteSourceGt1FirstBreakdown(session),
                CampaignHostAcceptedStep.SourceGt1FleetCompleteKind => CompleteSourceGt1StageOneFleet(session),
                CampaignHostAcceptedStep.SourceGt1ArrivalsCompleteKind => CompleteSourceGt1StageOneArrivals(session),
                CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind => CompleteSourceGt1Organization(session),
                CampaignHostAcceptedStep.SourceGt1SchedulingCompleteKind => CompleteSourceGt1Scheduling(session),
                CampaignHostAcceptedStep.SourceGt1WeatherKind => ResolveSourceGt1Weather(session),
                CampaignHostAcceptedStep.SourceGt1Lane2AnnounceKind => AnnounceSourceGt1Lane2(session),
                CampaignHostAcceptedStep.SourceGt1Lane3AnnounceKind => AnnounceSourceGt1Lane3(session),
                CampaignHostAcceptedStep.SourceSeventhShellCorrectionKind => CorrectSourceSeventhShell(session),
                CampaignHostAcceptedStep.SourceAxisMinorGarrisonsActivationKind => ActivateSourceAxisMinorGarrisons(session),
                CampaignHostAcceptedStep.SourceAxisGarrisonRemainderKind => ActivateSourceAxisGarrisonRemainder(session),
                CampaignHostAcceptedStep.SourceGiarabubMotorMachineGunKind => ActivateSourceGiarabubMotorMachineGun(session),
                CampaignHostAcceptedStep.SourceCwFrontlineKind => ActivateSourceCwFrontline(session),
                CampaignHostAcceptedStep.SourceGiarabubMotorPlacementPolicyKind => CampaignExercises.AdmitSourceGiarabubMotorPlacementPolicy(session),
                CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonAdmissionKind => CampaignExercises.AdmitSourceTobrukBardiaGarrison(session),
                CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonActivationKind => CampaignExercises.ActivateSourceTobrukBardiaGarrison(session),
                CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderAdmissionKind => CampaignExercises.AdmitSourceBardiaGarrisonRemainder(session),
                CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderActivationKind => CampaignExercises.ActivateSourceBardiaGarrisonRemainder(session),
                CampaignHostAcceptedStep.SourceTripoliTankRegimentAdmissionKind => CampaignExercises.AdmitSourceTripoliTankRegiment(session),
                CampaignHostAcceptedStep.SourceTripoliTankRegimentActivationKind => CampaignExercises.ActivateSourceTripoliTankRegiment(session),
                CampaignHostAcceptedStep.SourceCwAttachedArtilleryAdmissionKind => CampaignExercises.AdmitSourceCwAttachedArtillery(session),
                CampaignHostAcceptedStep.SourceCwAttachedArtilleryActivationKind => CampaignExercises.ActivateSourceCwAttachedArtillery(session),
                CampaignHostAcceptedStep.SourceGt1MultiLanesKind => AnnounceSourceGt1MultiLanes(session),
                CampaignHostAcceptedStep.SourceCwPriorityCatalogChoiceKind => SubmitSourceCwPriorityCatalog(session, submission),
                CampaignHostAcceptedStep.SourceMatruhFormationKind => CampaignExercises.AdmitSourceMatruhFormation(session),
                CampaignHostAcceptedStep.SourceMatruhFormationChoiceKind => SubmitSourceMatruhFormation(session, submission),
                CampaignHostAcceptedStep.SourceCwFreeGunsKind => CampaignExercises.AdmitSourceCwFreeGuns(session),
                CampaignHostAcceptedStep.SourceCwFreeGunsChoiceKind => PlaceSourceCwFreeGun(session, submission),
                CampaignHostAcceptedStep.SourceAxisPriorityMotorizationKind => CampaignExercises.AdmitSourceAxisPriorityMotorization(session),
                CampaignHostAcceptedStep.SourceAxisPriorityChoiceKind => CampaignExercises.ReplaySourceAxisPriority(session, submission),
                CampaignHostAcceptedStep.SourceAxisMinorGarrisonsKind => CampaignExercises.AdmitSourceAxisMinorGarrisons(session),
                CampaignHostAcceptedStep.SourceBattlefieldKind => CampaignExercises.AdmitSourceBattlefield(session),
                CampaignHostAcceptedStep.SourceCairoSetupKind => CampaignExercises.AdmitSourceCairoSetup(session),
                CampaignHostAcceptedStep.SourceCairoUnitKind => PlaceSourceCairoUnit(session, submission),
                CampaignHostAcceptedStep.SourceCairoWorldKind => CampaignExercises.AdmitSourceCairoWorld(session),
                CampaignHostAcceptedStep.SourceMalettiKind => CampaignExercises.ReplaySourceMalettiPromotion(session),
                CampaignHostAcceptedStep.SourceIndependentKind => CampaignExercises.ReplaySourceIndependentPromotion(session),
                CampaignHostAcceptedStep.SourceLocalMapKind => CampaignExercises.AdmitSourceLocalMap(session),
                CampaignHostAcceptedStep.SourceExpandedIndependentKind => CampaignExercises.ReplaySourceExpandedIndependentPromotion(session),
                CampaignHostAcceptedStep.SourceAxisFreeWorldKind => CampaignExercises.PromoteSourceAxisFreeWorld(session),
                CampaignHostAcceptedStep.SourceAxisCorpsWorldKind => CampaignExercises.PromoteSourceAxisCorpsWorld(session),
                CampaignHostAcceptedStep.SourceCertifiedMalettiWorldKind => CampaignExercises.PromoteSourceCertifiedMalettiWorld(session),
                CampaignHostAcceptedStep.SourceMatruhDetachedWorldKind => CampaignExercises.PromoteSourceMatruhDetachedWorld(session),
                CampaignHostAcceptedStep.SourceCwPriorityWorldKind => CampaignExercises.PromoteSourceCwPriorityWorld(session),
                CampaignHostAcceptedStep.SourceCwFreeGunsWorldKind => CampaignExercises.PromoteSourceCwFreeGunsWorld(session),
                CampaignHostAcceptedStep.ContactMovementKind => SubmitContactOwnerMovement(
                    session, submission, step.ActingElementId!),
                CampaignHostAcceptedStep.ContactInitiativeOrderKind => SubmitInitiativeOrder(
                    session, submission),
                CampaignHostAcceptedStep.ContactReserveDesignationKind => SubmitReserveDesignation(
                    session, submission),
                CampaignHostAcceptedStep.ContactReactionKind => SubmitReaction(
                    session, submission),
                CampaignHostAcceptedStep.ContactMovementSegmentCompletionKind => SubmitMovementSegmentCompletion(
                    session, submission),
                CampaignHostAcceptedStep.NativeCombatNoAttackKind => CompleteNativeCombatNoAttack(session),
                CampaignHostAcceptedStep.NativeSecondCombatNoAttackKind => CompleteSecondCombatNoAttack(session),
                CampaignHostAcceptedStep.NativeCombatBarrageKind => CompleteNativeCombatBarrage(session),
                CampaignHostAcceptedStep.NativeCombatForceCompletionKind =>
                    CompleteNativeCombatForceAssignment(session),
                CampaignHostAcceptedStep.NativeCombatForceCancellationKind =>
                    CancelNativeCombatForceAssignment(session, timing,
                        forceCancellation!.OpenedAtUnixMilliseconds,
                        forceCancellation.DeadlineUnixMilliseconds,
                        forceCancellation.HighWaterUnixMilliseconds,
                        forceCancellation.CancelledAtUnixMilliseconds,
                        forceCancellation.Reason),
                CampaignHostAcceptedStep.NativeCombatRbaCancellationKind =>
                    CancelNativeCombatRba(session, timing,
                        rbaCancellation!.OpenedAtUnixMilliseconds,
                        rbaCancellation.DeadlineUnixMilliseconds,
                        rbaCancellation.HighWaterUnixMilliseconds,
                        rbaCancellation.CancelledAtUnixMilliseconds,
                        rbaCancellation.Reason),
                CampaignHostAcceptedStep.NativeCombatAntiArmorKind =>
                    CompleteNativeCombatAntiArmor(session),
                CampaignHostAcceptedStep.NativeCombatCancelledAssaultKind =>
                    CompleteNativeCombatCancelledAssault(session),
                CampaignHostAcceptedStep.NativeCombatAssaultResultKind =>
                    ResolveNativeCombatAssault(session),
                CampaignHostAcceptedStep.ContactPostAssaultRetreatKind =>
                    SubmitContactPostAssaultRetreat(session, submission,
                        step.ActingElementId!),
                CampaignHostAcceptedStep.NativeCombatNoPostAssaultRetreatKind =>
                    CompleteNoPostAssaultRetreat(session),
                CampaignHostAcceptedStep.NativeCombatSettlementKind =>
                    SettleNativeCombatWorld(session),
                CampaignHostAcceptedStep.NativeCombatNoCustodyKind =>
                    CompleteNoNativeCombatCustody(session),
                CampaignHostAcceptedStep.NativeCombatSuccessorKind =>
                    CompleteNativeCombatSuccessor(session),
                CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind =>
                    CompleteNativeReserveRelease(session),
                CampaignHostAcceptedStep.ContactReserveReleaseChoiceKind =>
                    SubmitNativeReserveReleaseChoice(session, submission,
                        step.ActingElementId!),
                CampaignHostAcceptedStep.ContactCycleControlKind =>
                    SubmitNativeCycleControl(session, submission),
                CampaignHostAcceptedStep.ContactTruckConvoyPassKind =>
                    SubmitTruckConvoyPass(session, submission),
                CampaignHostAcceptedStep.ContactPostCombatPassKind =>
                    SubmitPostCombatPass(session, submission),
                CampaignHostAcceptedStep.ContactSecondReserveKind =>
                    SubmitSecondReserve(session, submission),
                CampaignHostAcceptedStep.ContactStageEntryInitiativeDeclarationKind =>
                    SubmitStageEntryInitiativeDeclaration(session, submission),
                CampaignHostAcceptedStep.ContactSecondMovementKind =>
                    SubmitSecondMovement(session, submission, step.ActingElementId!),
                CampaignHostAcceptedStep.ContactSecondMovementCompletionKind =>
                    SubmitSecondMovementCompletion(session, submission),
                CampaignHostAcceptedStep.NativeSecondBreakdownKind =>
                    CompleteSecondBreakdown(session),
                CampaignHostAcceptedStep.NativeStageEntryWeatherKind => CompleteStageEntrySystemStep(session, step.Kind),
                CampaignHostAcceptedStep.NativeStageEntryOrganizationKind => CompleteStageEntrySystemStep(session, step.Kind),
                CampaignHostAcceptedStep.NativeStageEntryNavalConvoyArrivalKind => CompleteStageEntrySystemStep(session, step.Kind),
                CampaignHostAcceptedStep.NativeStageEntryFleetAssignmentKind => CompleteStageEntrySystemStep(session, step.Kind),
                CampaignHostAcceptedStep.NativeStageEntryFleetRepairKind => CompleteStageEntrySystemStep(session, step.Kind),
                CampaignHostAcceptedStep.NativeStageEntryBreakdownKind => CompleteStageEntrySystemStep(session, step.Kind),
                CampaignHostAcceptedStep.NativeStageEntrySecondBreakdownKind => CompleteStageEntrySystemStep(session, step.Kind),
                CampaignHostAcceptedStep.ContactStageEntryReserveKind => SubmitStageEntryReserve(session, submission),
                CampaignHostAcceptedStep.ContactStageEntryMovementKind => SubmitStageEntryMovement(session, submission, step.ActingElementId!),
                CampaignHostAcceptedStep.ContactStageEntryMovementCompletionKind => SubmitStageEntryMovementCompletion(session, submission),
                CampaignHostAcceptedStep.ContactRepeatMovementKind =>
                    SubmitNativeRepeatMovement(session, submission),
                CampaignHostAcceptedStep.NativeRepeatBreakdownKind =>
                    CompleteNativeRepeatBreakdown(session),
                CampaignHostAcceptedStep.ContactRepeatCombatSelectionKind =>
                    SubmitNativeRepeatCombat(session, submission,
                        step.ActingElementId!),
                CampaignHostAcceptedStep.NativeRepeatCombatNoAttackKind =>
                    CompleteNativeRepeatCombatNoAttack(session),
                CampaignHostAcceptedStep.ContactCombatCustodyKind =>
                    SubmitNativeCombatCustody(session, submission,
                        step.ActingElementId!),
                CampaignHostAcceptedStep.ContactRbaDeclineKind => SubmitContactDefenderRbaDecline(
                    session, submission, step.ActingElementId!),
                CampaignHostAcceptedStep.ContactRbaMoveKind => SubmitContactDefenderRbaMove(
                    session, submission, step.ActingElementId!),
                CampaignHostAcceptedStep.ContactForceSealKind => SubmitContactOwnerForceSeal(
                    session, submission, step.ActingElementId!),
                _ => CampaignExercises.SubmitContactOwnerCombatSelection(session, submission,
                    step.ActingElementId!, retainedLegacySelection),
            };
            if (step.Kind == CampaignHostAcceptedStep.SourceAxisSupplementKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceAxisSupplementCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceCommonwealthSupplementKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceCommonwealthSupplementCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourcePriorityMotorizationKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourcePriorityMotorizationCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceAttachedInitialLoadsKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceAttachedInitialLoadsCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceInitialWrecksKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceInitialWrecksCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceAxisFreeSetupKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceAxisFreeSetupCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceAxisCorpsSetupKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceAxisCorpsSetupCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceMalettiHierarchyKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceMalettiHierarchyCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceGt2CorrectionKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceGt2CorrectionCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceCwPriorityCatalogKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceCwPriorityCatalogCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceCompactCheckpointKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceCompactCheckpointCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceFourthIndianWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceFourthIndianWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceMatruhGroupedWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceMatruhGroupedWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceMalettiGroupedWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceMalettiGroupedWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceLibyanGroupedWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceLibyanGroupedWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceItalianDivisionsGroupedWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceItalianDivisionsGroupedWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceSeventhArmoredGroupedWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceSeventhArmoredGroupedWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceSixteenthBrigadeGroupedWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceSixteenthBrigadeGroupedWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceCatanzaroGroupedWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceCatanzaroGroupedWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind is CampaignHostAcceptedStep.SourceTurnStartKind or CampaignHostAcceptedStep.SourceTurnInitiativeKind
                or CampaignHostAcceptedStep.SourceGt1BreakdownCompleteKind or CampaignHostAcceptedStep.SourceGt1FleetCompleteKind or CampaignHostAcceptedStep.SourceGt1ArrivalsCompleteKind or CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind or CampaignHostAcceptedStep.SourceGt1SchedulingCompleteKind or CampaignHostAcceptedStep.SourceGt1WeatherKind
                or CampaignHostAcceptedStep.SourceGt1Lane2AnnounceKind or CampaignHostAcceptedStep.SourceGt1Lane3AnnounceKind
                or CampaignHostAcceptedStep.SourceSeventhShellCorrectionKind or CampaignHostAcceptedStep.SourceGt1MultiLanesKind
                or CampaignHostAcceptedStep.SourceAxisMinorGarrisonsActivationKind
                or CampaignHostAcceptedStep.SourceAxisGarrisonRemainderKind
                or CampaignHostAcceptedStep.SourceGiarabubMotorMachineGunKind
                or CampaignHostAcceptedStep.SourceCwFrontlineKind
                or CampaignHostAcceptedStep.SourceGiarabubMotorPlacementPolicyKind
                or CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonAdmissionKind
                or CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonActivationKind
                or CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderAdmissionKind
                or CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderActivationKind
                or CampaignHostAcceptedStep.SourceTripoliTankRegimentAdmissionKind
                or CampaignHostAcceptedStep.SourceTripoliTankRegimentActivationKind
                or CampaignHostAcceptedStep.SourceCwAttachedArtilleryAdmissionKind
                or CampaignHostAcceptedStep.SourceCwAttachedArtilleryActivationKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceTurnSystemCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceMatruhFormationKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceMatruhFormationCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceCwFreeGunsKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceCwFreeGunsCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceAxisPriorityMotorizationKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceAxisPriorityMotorizationCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceAxisMinorGarrisonsKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceAxisMinorGarrisonsCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceBattlefieldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceBattlefieldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceCairoSetupKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceCairoSetupCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceCairoWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceCairoWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceMalettiKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceMalettiCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceIndependentKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceIndependentCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceExpandedIndependentKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceExpandedIndependentCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceAxisFreeWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceAxisFreeWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceAxisCorpsWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceAxisCorpsWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceCertifiedMalettiWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceCertifiedMalettiWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceMatruhDetachedWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceMatruhDetachedWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceCwPriorityWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceCwPriorityWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceCwFreeGunsWorldKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceCwFreeGunsWorldCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.SourceLocalMapKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SourceLocalMapCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind is CampaignHostAcceptedStep.NativeStageEntryWeatherKind or CampaignHostAcceptedStep.NativeStageEntryOrganizationKind or CampaignHostAcceptedStep.NativeStageEntryNavalConvoyArrivalKind or CampaignHostAcceptedStep.NativeStageEntryFleetAssignmentKind or CampaignHostAcceptedStep.NativeStageEntryFleetRepairKind or CampaignHostAcceptedStep.NativeStageEntryBreakdownKind or CampaignHostAcceptedStep.NativeStageEntrySecondBreakdownKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(StageEntrySystemCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeSecondBreakdownKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SecondBreakdownCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatNoAttackKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatNoAttackCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeSecondCombatNoAttackKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        SecondCombatNoAttackCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeRepeatCombatNoAttackKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeRepeatCombatNoAttackCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(
                    CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatBarrageKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatBarrageCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatForceCompletionKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatForceCompletionCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatForceCancellationKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatForceCancellationCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatRbaCancellationKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatRbaCancellationCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(NativeCombatSelectionTimeoutCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(NativeCombatRetreatTimeoutCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(NativeCombatCustodyTimeoutCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeReserveReleaseTimeoutCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCycleControlTimeoutKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCycleControlTimeoutCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatAntiArmorKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatAntiArmorCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatCancelledAssaultKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatCancelledAssaultCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatAssaultResultKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatAssaultResultCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatNoPostAssaultRetreatKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatNoPostAssaultRetreatCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatSettlementKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatSettlementCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatNoCustodyKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatNoCustodyCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatSuccessorKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatSuccessorCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeReserveReleaseCompletionCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (!accepted.IsAccepted
                || !step.Matches(accepted.Evidence!))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (item.Contacts is not null)
            {
                var before = session.CurrentContacts.Records;
                var after = accepted.SuccessorSession!.CurrentContacts.Records;
                if (after.Count < before.Count
                    || !ContactRecordsMatch(after.Take(before.Count).ToArray(), before)
                    || !ContactRecordsMatch(after.Skip(before.Count).ToArray(), item.Contacts))
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            }
            session = accepted.SuccessorSession!;
        }

        return CampaignHostImportResult.Verified(session);
    }

    private static bool ContactRecordsMatch(IReadOnlyList<byte[]> actual,
        IReadOnlyList<byte[]> retained) =>
        actual.Count == retained.Count
        && actual.Zip(retained).All(pair => pair.First.AsSpan().SequenceEqual(pair.Second));
}

/// <summary>Host-private authority fence and action map paired with one side-safe Core cut.</summary>
public sealed class CampaignHostContactOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly MoveElementAction[] privateMovementActions;

    internal CampaignHostContactOwnerDraft(long authorityVersion, byte[] visibleBytes,
        IReadOnlyList<MoveElementAction> privateMovementActions)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateMovementActions = privateMovementActions.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<MoveElementAction> PrivateMovementActions =>
        Array.AsReadOnly(privateMovementActions.ToArray());
}

/// <summary>Host-private Combat action map paired with canonical side-safe owner bytes.</summary>
public sealed class CampaignHostContactCombatOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateCombatActionIds;

    internal CampaignHostContactCombatOwnerDraft(long authorityVersion, byte[] visibleBytes,
        IReadOnlyList<string> privateCombatActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateCombatActionIds = privateCombatActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateCombatActionIds =>
        Array.AsReadOnly(privateCombatActionIds.ToArray());
}

/// <summary>Host-private defender action map paired with canonical contact-only bytes.</summary>
public sealed class CampaignHostContactRbaOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostContactRbaOwnerDraft(long authorityVersion, byte[] visibleBytes,
        IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds => Array.AsReadOnly(privateActionIds.ToArray());
}

/// <summary>Host-private post-result defender action map and side-safe bytes.</summary>
public sealed class CampaignHostContactPostAssaultRetreatOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostContactPostAssaultRetreatOwnerDraft(long authorityVersion,
        byte[] visibleBytes, IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds => Array.AsReadOnly(privateActionIds.ToArray());
}

/// <summary>Host-private captor action map and side-safe visible bytes.</summary>
public sealed class CampaignHostContactCustodyOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostContactCustodyOwnerDraft(long authorityVersion,
        byte[] visibleBytes, IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds =>
        Array.AsReadOnly(privateActionIds.ToArray());
}

/// <summary>Host-private Reserve Release action map paired with own-member bytes.</summary>
public sealed class CampaignHostReserveReleaseOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostReserveReleaseOwnerDraft(long authorityVersion,
        byte[] visibleBytes, IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds =>
        Array.AsReadOnly(privateActionIds.ToArray());
}

/// <summary>Host-private same-slot cycle choice map paired with side-safe bytes.</summary>
public sealed class CampaignHostCycleControlOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostCycleControlOwnerDraft(long authorityVersion,
        byte[] visibleBytes, IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds =>
        Array.AsReadOnly(privateActionIds.ToArray());
}

/// <summary>Host-private repeated Movement action map and side-safe bytes.</summary>
public sealed class CampaignHostRepeatMovementOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostRepeatMovementOwnerDraft(long authorityVersion,
        byte[] visibleBytes, IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds =>
        Array.AsReadOnly(privateActionIds.ToArray());
}

/// <summary>Host-private Force Assignment action map and side-safe bytes.</summary>
public sealed class CampaignHostContactForceOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostContactForceOwnerDraft(long authorityVersion, byte[] visibleBytes,
        IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds => Array.AsReadOnly(privateActionIds.ToArray());
}

public enum CampaignHostQueryRejectionReason { None, InvalidAudience, InvalidState }

/// <summary>The (stage, side) occurrence a native Combat Selection belongs to.</summary>
public enum CampaignCombatSelectionSlot
{
    StageOneFirst, StageOneSecond, StageTwoFirst, StageTwoSecond, StageThreeFirst, StageThreeSecond,
}

// Host-private source data. These records must never be returned from an HTTP endpoint.
public sealed record CampaignHostPrivateSideCut(
    string CampaignId, long StateVersion, string RulesetHash, string ScenarioId,
    string PositionId, int GameTurn, string PhaseId, string StageId,
    CampaignActionAudience Audience,
    IReadOnlyList<CampaignHostPrivateLocation> Locations,
    IReadOnlyList<CampaignHostPrivateOwnElement> OwnElements,
    IReadOnlyList<CampaignHostPrivateContact> Contacts,
    IReadOnlyList<CampaignHostPrivateAction> Actions,
    string DecisionStateKind);
public sealed record CampaignHostPrivateLocation(string LocationId, string TerrainId);
public sealed record CampaignHostPrivateOwnElement(string ElementId, string FormationId,
    string LocationId, string ReserveStatus);
public sealed record CampaignHostPrivateContact(string RepresentationId, string LocationId,
    bool ExertsZoc);
public sealed record CampaignHostPrivateAction(string CoreActionId, string Kind,
    string? ElementId = null, string? TargetLocationId = null,
    string? OpportunityId = null);

public sealed class CampaignHostAudienceCut
{
    private CampaignHostAudienceCut(CampaignObservationV7? observation,
        CampaignLegalActionSet? legalActions, CampaignHostQueryRejectionReason rejectionReason)
    {
        Observation = observation;
        LegalActions = legalActions;
        RejectionReason = rejectionReason;
    }

    public bool IsProjected => Observation is not null;
    public CampaignObservationV7? Observation { get; }
    public CampaignLegalActionSet? LegalActions { get; }
    public CampaignHostQueryRejectionReason RejectionReason { get; }

    internal static CampaignHostAudienceCut Projected(CampaignObservationV7 observation,
        CampaignLegalActionSet legalActions) => new(observation, legalActions,
        CampaignHostQueryRejectionReason.None);
    internal static CampaignHostAudienceCut Rejected(CampaignHostQueryRejectionReason reason) =>
        new(null, null, reason);
}

/// <summary>Private replay evidence; bytes are copied on both ingress and egress.</summary>
public sealed class CampaignHostAcceptedStep
{
    public const int CurrentContractVersion = 1;
    public const int ContactMovementContractVersion = 2;
    // Informational step-kind metadata only (never persisted or replay-checked -- see
    // CampaignHostAcceptedStep's own ContractVersion property); picked past every
    // existing Contact*/Native* number so it can never collide with one of them.
    public const int ContactInitiativeOrderContractVersion = 48;
    public const int ContactReserveDesignationContractVersion = 49;
    public const int ContactReactionContractVersion = 50;
    public const int ContactMovementSegmentCompletionContractVersion = 51;
    public const int ContactCombatSelectionContractVersion = 3;
    public const int NativeCombatNoAttackContractVersion = 4;
    public const int NativeCombatBarrageContractVersion = 5;
    public const int ContactRbaDeclineContractVersion = 6;
    public const int ContactRbaMoveContractVersion = 7;
    public const int ContactForceSealContractVersion = 8;
    public const int NativeCombatForceCompletionContractVersion = 9;
    public const int NativeCombatForceCancellationContractVersion = 10;
    public const int NativeCombatAntiArmorContractVersion = 11;
    public const int NativeCombatCancelledAssaultContractVersion = 12;
    public const int NativeCombatAssaultResultContractVersion = 13;
    public const int ContactPostAssaultRetreatContractVersion = 14;
    public const int NativeCombatNoPostAssaultRetreatContractVersion = 15;
    public const int NativeCombatSettlementContractVersion = 16;
    public const int NativeCombatNoCustodyContractVersion = 17;
    public const int ContactCombatCustodyContractVersion = 18;
    public const int NativeCombatSuccessorContractVersion = 19;
    public const int NativeReserveReleaseCompletionContractVersion = 20;
    public const int ContactReserveReleaseChoiceContractVersion = 21;
    public const int ContactCycleControlContractVersion = 22;
    public const int ContactRepeatMovementContractVersion = 23;
    public const int NativeRepeatBreakdownContractVersion = 24;
    public const int ContactRepeatCombatSelectionContractVersion = 25;
    public const int NativeRepeatCombatNoAttackContractVersion = 26;
    public const int NativeCombatRbaCancellationContractVersion = 27;
    public const int NativeCycleControlTimeoutContractVersion = 28;
    public const int NativeReserveReleaseTimeoutContractVersion = 29;
    public const int NativeCombatCustodyTimeoutContractVersion = 30;
    public const int NativeCombatRetreatTimeoutContractVersion = 31;
    public const int NativeCombatSelectionTimeoutContractVersion = 32;
    public const int ContactTruckConvoyPassContractVersion = 33;
    public const int ContactPostCombatPassContractVersion = 34;
    public const int ContactSecondReserveContractVersion = 35;
    public const int ContactStageEntryInitiativeDeclarationContractVersion = 116;
    public const int ContactSecondMovementContractVersion = 36;
    public const int ContactSecondMovementCompletionContractVersion = 37;
    public const int PregameAxisSupplyContractVersion = 38;
    public const int NativeSecondBreakdownContractVersion = 39;
    public const int PregameAxisSupplyFinishContractVersion = 40;
    public const int SourceAxisDumpContractVersion = 41;
    public const int SourceCommonwealthSupplyContractVersion = 42;
    public const int SourceDummyContractVersion = 43;
    public const int SourceMotorizationContractVersion = 44;
    public const int SourceAxisGt1SupplyContractVersion = 45;
    public const int SourceCommonwealthSupplyPlanContractVersion = 46;
    public const int SourceAxisSupplementContractVersion = 47;
    public const int SourceAutoblindaContractVersion = 48;
    public const int SourceCommonwealthSupplementContractVersion = 49;
    public const int SourcePriorityMotorizationContractVersion = 50;
    public const int SourcePriorityMotorizationChoiceContractVersion = 51;
    public const int SourceAxisPriorityMotorizationContractVersion = 52;
    public const int SourceAxisPriorityChoiceContractVersion = 53;
    public const int SourceAxisMinorGarrisonsContractVersion = 54;
    public const int SourceBattlefieldContractVersion = 55;
    public const int SourceCairoSetupContractVersion = 56;
    public const int SourceCairoUnitContractVersion = 57;
    public const int SourceCairoWorldContractVersion = 58;
    public const int SourceMalettiContractVersion = 59;
    public const int SourceIndependentContractVersion = 60;
    public const int SourceLocalMapContractVersion = 61;
    public const int SourceExpandedIndependentContractVersion = 62;
    public const int SourceAttachedInitialLoadsContractVersion = 63;
    public const int SourceAttachedInitialLoadsChoiceContractVersion = 64;
    public const int SourceInitialWrecksContractVersion = 65;
    public const int SourceInitialWrecksChoiceContractVersion = 66;
    public const int SourceAxisFreeSetupContractVersion = 67;
    public const int SourceAxisFreeSetupChoiceContractVersion = 68;
    public const int SourceAxisCorpsSetupContractVersion = 70;
    public const int SourceAxisCorpsSetupChoiceContractVersion = 71;
    public const int SourceMalettiHierarchyContractVersion = 73;
    public const int SourceMalettiHierarchyChoiceContractVersion = 74;
    public const int SourceMatruhFormationContractVersion = 76;
    public const int SourceMatruhFormationChoiceContractVersion = 77;
    public const int SourceMatruhDetachedWorldContractVersion = 78;
    public const int SourceGt2CorrectionContractVersion = 79;
    public const int SourceGt2CorrectionChoiceContractVersion = 80;
    public const int SourceCwPriorityCatalogContractVersion = 81;
    public const int SourceCwPriorityCatalogChoiceContractVersion = 82;
    public const int SourceCwPriorityWorldContractVersion = 83;
    public const int SourceCompactCheckpointContractVersion = 84;
    public const int SourceCwFreeGunsContractVersion = 85;
    public const int SourceCwFreeGunsChoiceContractVersion = 86;
    public const int SourceCwFreeGunsWorldContractVersion = 87;
    public const int SourceUnattachedCargoContractVersion = 88;
    public const int SourceActionsContractVersion = 89;
    public const int SourceFourthIndianWorldContractVersion = 90;
    public const int SourceMatruhGroupedWorldContractVersion = 91;
    public const int SourceMalettiGroupedWorldContractVersion = 92;
    public const int SourceLibyanGroupedWorldContractVersion = 93;
    public const int SourceItalianDivisionsGroupedWorldContractVersion = 94;
    public const int SourceSeventhArmoredGroupedWorldContractVersion = 95;
    public const int SourceSixteenthBrigadeGroupedWorldContractVersion = 96;
    public const int SourceCatanzaroGroupedWorldContractVersion = 97;
    public const int SourceTurnStartContractVersion = 98;
    public const int SourceTurnInitiativeContractVersion = 99;
    public const int SourceGt1BreakdownCompleteContractVersion = 114;
    public const int SourceGt1FleetCompleteContractVersion = 113;
    public const int SourceGt1ArrivalsCompleteContractVersion = 112;
    public const int SourceGt1OrganizationCompleteContractVersion = 111;
    public const int SourceGt1SchedulingCompleteContractVersion = 100;
    public const int SourceGt1WeatherContractVersion = 101;
    public const int SourceGt1Lane2AnnounceContractVersion = 102;
    public const int SourceGt1Lane3AnnounceContractVersion = 103;
    public const int SourceSeventhShellCorrectionContractVersion = 104;
    public const int SourceGt1MultiLanesContractVersion = 105;
    public const int SourceAxisMinorGarrisonsActivationContractVersion = 106;
    public const int SourceAxisGarrisonRemainderContractVersion = 107;
    public const int SourceGiarabubMotorMachineGunContractVersion = 108;
    public const int SourceCwFrontlineContractVersion = 109;
    public const int SourceGiarabubMotorPlacementPolicyContractVersion = 110;
    public const int SourceTobrukBardiaGarrisonAdmissionContractVersion = 117;
    public const int SourceTobrukBardiaGarrisonActivationContractVersion = 118;
    public const int SourceBardiaGarrisonRemainderAdmissionContractVersion = 119;
    public const int SourceBardiaGarrisonRemainderActivationContractVersion = 120;
    public const int SourceTripoliTankRegimentAdmissionContractVersion = 121;
    public const int SourceTripoliTankRegimentActivationContractVersion = 122;
    public const int SourceCwAttachedArtilleryAdmissionContractVersion = 123;
    public const int SourceCwAttachedArtilleryActivationContractVersion = 124;
    public const int NativeStageEntryWeatherContractVersion = 125;
    public const int NativeStageEntryOrganizationContractVersion = 126;
    public const int NativeStageEntryNavalConvoyArrivalContractVersion = 127;
    public const int NativeStageEntryFleetAssignmentContractVersion = 128;
    public const int NativeStageEntryFleetRepairContractVersion = 129;
    public const int NativeStageEntryBreakdownContractVersion = 130;
    public const int ContactStageEntryReserveContractVersion = 131;
    public const int ContactStageEntryMovementContractVersion = 132;
    public const int ContactStageEntryMovementCompletionContractVersion = 133;
    public const int NativeStageEntrySecondBreakdownContractVersion = 134;
    public const int SourceCertifiedMalettiWorldContractVersion = 75;
    public const int SourceAxisCorpsWorldContractVersion = 72;
    public const int SourceAxisFreeWorldContractVersion = 69;
    // #394 extra-3: Host-recorded system step for the second side's
    // voluntary-finish auto-continuation. Distinct from NativeCombatNoAttackKind
    // so cold replay dispatches to CompleteSecondCombatNoAttack instead of the
    // first-side method.
    public const int NativeSecondCombatNoAttackContractVersion = 115;
    public const string LegacyKind = "legacy-action";
    public const string ContactMovementKind = "contact-movement";
    public const string ContactInitiativeOrderKind = "contact-initiative-order";
    public const string ContactReserveDesignationKind = "contact-reserve-designation";
    public const string ContactReactionKind = "contact-reaction";
    public const string ContactMovementSegmentCompletionKind = "contact-movement-segment-completion";
    public const string ContactCombatSelectionKind = "contact-combat-selection";
    public const string NativeCombatNoAttackKind = "native-combat-no-attack";
    public const string NativeSecondCombatNoAttackKind = "native-second-combat-no-attack";
    public const string NativeCombatBarrageKind = "native-combat-barrage";
    public const string ContactRbaDeclineKind = "contact-rba-decline";
    public const string ContactRbaMoveKind = "contact-rba-move";
    public const string ContactForceSealKind = "contact-force-seal";
    public const string NativeCombatForceCompletionKind = "native-combat-force-completion";
    public const string NativeCombatForceCancellationKind = "native-combat-force-cancellation";
    public const string NativeCombatAntiArmorKind = "native-combat-anti-armor";
    public const string NativeCombatCancelledAssaultKind = "native-combat-cancelled-assault";
    public const string NativeCombatAssaultResultKind = "native-combat-assault-result";
    public const string ContactPostAssaultRetreatKind = "contact-post-assault-retreat";
    public const string NativeCombatNoPostAssaultRetreatKind = "native-combat-no-post-assault-retreat";
    public const string NativeCombatSettlementKind = "native-combat-world-settlement";
    public const string NativeCombatNoCustodyKind = "native-combat-no-custody";
    public const string ContactCombatCustodyKind = "contact-combat-custody";
    public const string NativeCombatSuccessorKind = "native-combat-successor";
    public const string NativeReserveReleaseCompletionKind = "native-reserve-release-completion";
    public const string ContactReserveReleaseChoiceKind = "contact-reserve-release-choice";
    public const string ContactCycleControlKind = "contact-cycle-control";
    public const string ContactTruckConvoyPassKind = "contact-truck-convoy-pass";
    public const string ContactPostCombatPassKind = "contact-post-combat-pass";
    public const string ContactSecondReserveKind = "contact-second-reserve";
    public const string ContactStageEntryInitiativeDeclarationKind = "contact-stage-entry-initiative-declaration";
    public const string ContactSecondMovementKind = "contact-second-movement";
    public const string ContactSecondMovementCompletionKind = "contact-second-movement-completion";
    public const string PregameAxisSupplyKind = "pregame-axis-supply";
    public const string PregameAxisSupplyFinishKind = "pregame-axis-supply-finish";
    public const string SourceAxisDumpKind = "source-axis-dump";
    public const string SourceCommonwealthSupplyKind = "source-commonwealth-supply";
    public const string SourceDummyKind = "source-dummy";
    public const string SourceMotorizationKind = "source-motorization";
    public const string SourceUnattachedCargoKind = "source-unattached-cargo";
    public const string SourceActionsKind = "source-actions";
    public const string SourceAxisGt1SupplyKind = "source-axis-gt1-supply";
    public const string SourceCommonwealthSupplyPlanKind = "source-commonwealth-supply-plan";
    public const string SourceAxisSupplementKind = "source-axis-supplement";
    public const string SourceAutoblindaKind = "source-autoblinda";
    public const string SourceCommonwealthSupplementKind = "source-commonwealth-supplement";
    public const string SourcePriorityMotorizationKind = "source-priority-motorization";
    public const string SourcePriorityMotorizationChoiceKind = "source-priority-motorization-choice";
    public const string SourceAttachedInitialLoadsKind = "source-attached-initial-loads";
    public const string SourceAttachedInitialLoadsChoiceKind = "source-attached-initial-loads-choice";
    public const string SourceInitialWrecksKind = "source-initial-wrecks";
    public const string SourceInitialWrecksChoiceKind = "source-initial-wrecks-choice";
    public const string SourceAxisFreeSetupKind = "source-axis-free-setup";
    public const string SourceAxisFreeSetupChoiceKind = "source-axis-free-setup-choice";
    public const string SourceAxisCorpsSetupKind = "source-axis-corps-setup";
    public const string SourceAxisCorpsSetupChoiceKind = "source-axis-corps-setup-choice";
    public const string SourceMalettiHierarchyKind = "source-maletti-hierarchy";
    public const string SourceMalettiHierarchyChoiceKind = "source-maletti-hierarchy-choice";
    public const string SourceGt2CorrectionKind = "source-gt2-correction";
    public const string SourceGt2CorrectionChoiceKind = "source-gt2-correction-choice";
    public const string SourceCwPriorityCatalogKind = "source-cw-priority-catalog";
    public const string SourceCompactCheckpointKind = "source-compact-checkpoint";
    public const string SourceFourthIndianWorldKind = "source-fourth-indian-world";
    public const string SourceMatruhGroupedWorldKind = "source-matruh-grouped-world";
    public const string SourceMalettiGroupedWorldKind = "source-maletti-grouped-world";
    public const string SourceLibyanGroupedWorldKind = "source-libyan-grouped-world";
    public const string SourceItalianDivisionsGroupedWorldKind = "source-italian-divisions-grouped-world";
    public const string SourceSeventhArmoredGroupedWorldKind = "source-seventh-armored-grouped-world";
    public const string SourceSixteenthBrigadeGroupedWorldKind = "source-sixteenth-brigade-grouped-world";
    public const string SourceCatanzaroGroupedWorldKind = "source-catanzaro-grouped-world";
    public const string SourceTurnStartKind = "source-turn-start";
    public const string SourceTurnInitiativeKind = "source-turn-initiative";
    public const string SourceGt1BreakdownCompleteKind = "source-gt1-first-breakdown-complete";
    public const string SourceGt1FleetCompleteKind = "source-gt1-stage1-fleet-complete";
    public const string SourceGt1ArrivalsCompleteKind = "source-gt1-stage1-arrivals-complete";
    public const string SourceGt1OrganizationCompleteKind = "source-gt1-organization-complete";
    public const string SourceGt1SchedulingCompleteKind = "source-gt1-scheduling-complete";
    public const string SourceGt1WeatherKind = "source-gt1-weather";
    public const string SourceGt1Lane2AnnounceKind = "source-gt1-lane2-announce";
    public const string SourceGt1Lane3AnnounceKind = "source-gt1-lane3-announce";
    public const string SourceSeventhShellCorrectionKind = "source-seventh-shell-correction";
    public const string SourceGt1MultiLanesKind = "source-gt1-multi-lanes";
    public const string SourceAxisMinorGarrisonsActivationKind = "source-axis-minor-garrisons-activation";
    public const string SourceAxisGarrisonRemainderKind = "source-axis-garrison-remainder-activation";
    public const string SourceGiarabubMotorMachineGunKind = "source-giarabub-motor-machine-gun-activation";
    public const string SourceCwFrontlineKind = "source-cw-frontline-activation";
    public const string SourceGiarabubMotorPlacementPolicyKind = "source-giarabub-motor-placement-policy";
    public const string SourceTobrukBardiaGarrisonAdmissionKind = "source-tobruk-bardia-garrison-admission";
    public const string SourceTobrukBardiaGarrisonActivationKind = "source-tobruk-bardia-garrison-activation";
    public const string SourceBardiaGarrisonRemainderAdmissionKind = "source-bardia-garrison-remainder-admission";
    public const string SourceBardiaGarrisonRemainderActivationKind = "source-bardia-garrison-remainder-activation";
    public const string SourceTripoliTankRegimentAdmissionKind = "source-tripoli-tank-regiment-admission";
    public const string SourceTripoliTankRegimentActivationKind = "source-tripoli-tank-regiment-activation";
    public const string SourceCwAttachedArtilleryAdmissionKind = "source-cw-attached-artillery-admission";
    public const string SourceCwAttachedArtilleryActivationKind = "source-cw-attached-artillery-activation";
    public const string SourceCwPriorityCatalogChoiceKind = "source-cw-priority-catalog-choice";
    public const string SourceMatruhFormationKind = "source-matruh-formation";
    public const string SourceMatruhFormationChoiceKind = "source-matruh-formation-choice";
    public const string SourceCwFreeGunsKind = "source-cw-free-guns";
    public const string SourceCwFreeGunsChoiceKind = "source-cw-free-guns-choice";
    public const string SourceAxisPriorityMotorizationKind = "source-axis-priority-motorization";
    public const string SourceAxisPriorityChoiceKind = "source-axis-priority-choice";
    public const string SourceAxisMinorGarrisonsKind = "source-axis-minor-garrisons";
    public const string SourceBattlefieldKind = "source-battlefield";
    public const string SourceCairoSetupKind = "source-cairo-setup";
    public const string SourceCairoUnitKind = "source-cairo-unit";
    public const string SourceCairoWorldKind = "source-cairo-world";
    public const string SourceMalettiKind = "source-maletti";
    public const string SourceIndependentKind = "source-independent";
    public const string SourceLocalMapKind = "source-local-map";
    public const string SourceExpandedIndependentKind = "source-expanded-independent";
    public const string SourceAxisFreeWorldKind = "source-axis-free-world";
    public const string SourceAxisCorpsWorldKind = "source-axis-corps-world";
    public const string SourceCertifiedMalettiWorldKind = "source-certified-maletti-world";
    public const string SourceMatruhDetachedWorldKind = "source-matruh-detached-world";
    public const string SourceCwPriorityWorldKind = "source-cw-priority-world";
    public const string SourceCwFreeGunsWorldKind = "source-cw-free-guns-world";
    public const string NativeSecondBreakdownKind = "native-second-breakdown";
    public const string NativeStageEntryWeatherKind = "native-stage-entry-weather";
    public const string NativeStageEntryOrganizationKind = "native-stage-entry-organization";
    public const string NativeStageEntryNavalConvoyArrivalKind = "native-stage-entry-naval-convoy-arrival";
    public const string NativeStageEntryFleetAssignmentKind = "native-stage-entry-fleet-assignment";
    public const string NativeStageEntryFleetRepairKind = "native-stage-entry-fleet-repair";
    public const string NativeStageEntryBreakdownKind = "native-stage-entry-breakdown";
    public const string NativeStageEntrySecondBreakdownKind = "native-stage-entry-second-breakdown";
    public const string ContactStageEntryReserveKind = "contact-stage-entry-reserve";
    public const string ContactStageEntryMovementKind = "contact-stage-entry-movement";
    public const string ContactStageEntryMovementCompletionKind = "contact-stage-entry-movement-completion";
    public const string ContactRepeatMovementKind = "contact-repeat-movement";
    public const string NativeRepeatBreakdownKind = "native-repeat-breakdown";
    public const string ContactRepeatCombatSelectionKind = "contact-repeat-combat-selection";
    public const string NativeRepeatCombatNoAttackKind = "native-repeat-combat-no-attack";
    public const string NativeCombatRbaCancellationKind = "native-combat-rba-cancellation";
    public const string NativeCycleControlTimeoutKind = "native-cycle-control-timeout";
    public const string NativeReserveReleaseTimeoutKind = "native-reserve-release-timeout";
    public const string NativeCombatCustodyTimeoutKind = "native-combat-custody-timeout";
    public const string NativeCombatRetreatTimeoutKind = "native-combat-retreat-timeout";
    public const string NativeCombatSelectionTimeoutKind = "native-combat-selection-timeout";

    private readonly byte[] canonicalCommand;
    private readonly byte[][] eventRecords;
    private readonly byte[] canonicalReceipt;
    private readonly byte[] snapshotCheckpoint;

    public CampaignHostAcceptedStep(byte[] canonicalCommand, IReadOnlyList<byte[]> eventRecords,
        byte[] canonicalReceipt, byte[] snapshotCheckpoint, string kind = LegacyKind,
        string? actingElementId = null)
    {
        if (kind is not (LegacyKind or PregameAxisSupplyKind or PregameAxisSupplyFinishKind or SourceAxisDumpKind or SourceCommonwealthSupplyKind or SourceDummyKind or SourceMotorizationKind or SourceUnattachedCargoKind or SourceActionsKind or SourceAxisGt1SupplyKind or SourceCommonwealthSupplyPlanKind or SourceAxisSupplementKind or SourceAutoblindaKind or SourceCommonwealthSupplementKind or SourcePriorityMotorizationKind or SourcePriorityMotorizationChoiceKind or SourceAttachedInitialLoadsKind or SourceAttachedInitialLoadsChoiceKind or SourceInitialWrecksKind or SourceInitialWrecksChoiceKind or SourceAxisFreeSetupKind or SourceAxisFreeSetupChoiceKind or SourceAxisCorpsSetupKind or SourceAxisCorpsSetupChoiceKind or SourceMalettiHierarchyKind or SourceMalettiHierarchyChoiceKind or SourceGt2CorrectionKind or SourceGt2CorrectionChoiceKind or SourceCwPriorityCatalogKind or SourceCwPriorityCatalogChoiceKind or SourceCompactCheckpointKind or SourceFourthIndianWorldKind or SourceMatruhGroupedWorldKind or SourceMalettiGroupedWorldKind or SourceLibyanGroupedWorldKind or SourceItalianDivisionsGroupedWorldKind or SourceSeventhArmoredGroupedWorldKind or SourceSixteenthBrigadeGroupedWorldKind or SourceCatanzaroGroupedWorldKind or SourceTurnStartKind or SourceTurnInitiativeKind or SourceGt1BreakdownCompleteKind or SourceGt1FleetCompleteKind or SourceGt1ArrivalsCompleteKind or SourceGt1OrganizationCompleteKind or SourceGt1SchedulingCompleteKind or SourceGt1WeatherKind or SourceGt1Lane2AnnounceKind or SourceGt1Lane3AnnounceKind or SourceSeventhShellCorrectionKind or SourceGt1MultiLanesKind or SourceAxisMinorGarrisonsActivationKind or SourceAxisGarrisonRemainderKind or SourceGiarabubMotorMachineGunKind or SourceCwFrontlineKind or SourceGiarabubMotorPlacementPolicyKind or SourceTobrukBardiaGarrisonAdmissionKind or SourceTobrukBardiaGarrisonActivationKind or SourceBardiaGarrisonRemainderAdmissionKind or SourceBardiaGarrisonRemainderActivationKind or SourceTripoliTankRegimentAdmissionKind or SourceTripoliTankRegimentActivationKind or SourceCwAttachedArtilleryAdmissionKind or SourceCwAttachedArtilleryActivationKind or SourceMatruhFormationKind or SourceMatruhFormationChoiceKind or SourceCwFreeGunsKind or SourceCwFreeGunsChoiceKind or SourceAxisPriorityMotorizationKind or SourceAxisPriorityChoiceKind or SourceAxisMinorGarrisonsKind or SourceBattlefieldKind or SourceCairoSetupKind or SourceCairoUnitKind or SourceCairoWorldKind or SourceMalettiKind or SourceIndependentKind or SourceLocalMapKind or SourceExpandedIndependentKind or SourceAxisFreeWorldKind or SourceAxisCorpsWorldKind or SourceCertifiedMalettiWorldKind or SourceMatruhDetachedWorldKind or SourceCwPriorityWorldKind or SourceCwFreeGunsWorldKind or ContactMovementKind or ContactInitiativeOrderKind or ContactReserveDesignationKind or ContactReactionKind or ContactMovementSegmentCompletionKind or ContactCombatSelectionKind or ContactStageEntryInitiativeDeclarationKind
                or NativeCombatNoAttackKind or NativeSecondCombatNoAttackKind or NativeCombatBarrageKind or ContactRbaDeclineKind
                or ContactRbaMoveKind or ContactForceSealKind or NativeCombatForceCompletionKind
                or NativeCombatForceCancellationKind or NativeCombatAntiArmorKind
                or NativeCombatCancelledAssaultKind or NativeCombatAssaultResultKind
                or ContactPostAssaultRetreatKind or NativeCombatNoPostAssaultRetreatKind
                or NativeCombatSettlementKind or NativeCombatNoCustodyKind
                or ContactCombatCustodyKind or NativeCombatSuccessorKind
                or NativeReserveReleaseCompletionKind
                or ContactReserveReleaseChoiceKind or ContactCycleControlKind or ContactTruckConvoyPassKind or ContactPostCombatPassKind or ContactSecondReserveKind or ContactStageEntryInitiativeDeclarationKind or ContactSecondMovementKind or ContactSecondMovementCompletionKind
                or ContactRepeatMovementKind or NativeRepeatBreakdownKind or NativeSecondBreakdownKind
                or NativeStageEntryWeatherKind
                or NativeStageEntryOrganizationKind
                or NativeStageEntryNavalConvoyArrivalKind
                or NativeStageEntryFleetAssignmentKind
                or NativeStageEntryFleetRepairKind
                or NativeStageEntryBreakdownKind
                or NativeStageEntrySecondBreakdownKind
                or ContactStageEntryReserveKind
                or ContactStageEntryMovementKind
                or ContactStageEntryMovementCompletionKind
                or ContactRepeatCombatSelectionKind or NativeRepeatCombatNoAttackKind
                or NativeCombatRbaCancellationKind or NativeCycleControlTimeoutKind or NativeReserveReleaseTimeoutKind or NativeCombatCustodyTimeoutKind or NativeCombatRetreatTimeoutKind or NativeCombatSelectionTimeoutKind)
            || (kind is PregameAxisSupplyKind or PregameAxisSupplyFinishKind or SourceAxisDumpKind or SourceCommonwealthSupplyKind or SourceDummyKind or SourceMotorizationKind or SourceUnattachedCargoKind or SourceActionsKind or SourceAxisGt1SupplyKind or SourceCommonwealthSupplyPlanKind or SourceAutoblindaKind or SourcePriorityMotorizationChoiceKind or SourceAttachedInitialLoadsChoiceKind or SourceInitialWrecksChoiceKind or SourceAxisFreeSetupChoiceKind or SourceAxisCorpsSetupChoiceKind or SourceMalettiHierarchyChoiceKind or SourceGt2CorrectionChoiceKind or SourceCwPriorityCatalogChoiceKind or SourceMatruhFormationChoiceKind or SourceCwFreeGunsChoiceKind or SourceAxisPriorityChoiceKind or SourceCairoUnitKind or ContactMovementKind or ContactCombatSelectionKind or ContactRbaDeclineKind
                or ContactRbaMoveKind or ContactForceSealKind
                or ContactPostAssaultRetreatKind or ContactCombatCustodyKind
                or ContactReserveReleaseChoiceKind or ContactCycleControlKind or ContactTruckConvoyPassKind or ContactPostCombatPassKind or ContactSecondReserveKind or ContactStageEntryInitiativeDeclarationKind or ContactSecondMovementKind or ContactSecondMovementCompletionKind
                or ContactRepeatMovementKind or ContactRepeatCombatSelectionKind
                or ContactStageEntryReserveKind
                or ContactStageEntryMovementKind
                or ContactStageEntryMovementCompletionKind
                && string.IsNullOrWhiteSpace(actingElementId))
            || (kind is LegacyKind or SourceAxisSupplementKind or SourceCommonwealthSupplementKind or SourcePriorityMotorizationKind or SourceAttachedInitialLoadsKind or SourceInitialWrecksKind or SourceAxisFreeSetupKind or SourceAxisCorpsSetupKind or SourceMalettiHierarchyKind or SourceGt2CorrectionKind or SourceCwPriorityCatalogKind or SourceCompactCheckpointKind or SourceFourthIndianWorldKind or SourceMatruhGroupedWorldKind or SourceMalettiGroupedWorldKind or SourceLibyanGroupedWorldKind or SourceItalianDivisionsGroupedWorldKind or SourceSeventhArmoredGroupedWorldKind or SourceSixteenthBrigadeGroupedWorldKind or SourceCatanzaroGroupedWorldKind or SourceTurnStartKind or SourceTurnInitiativeKind or SourceGt1BreakdownCompleteKind or SourceGt1FleetCompleteKind or SourceGt1ArrivalsCompleteKind or SourceGt1OrganizationCompleteKind or SourceGt1SchedulingCompleteKind or SourceGt1WeatherKind or SourceGt1Lane2AnnounceKind or SourceGt1Lane3AnnounceKind or SourceSeventhShellCorrectionKind or SourceGt1MultiLanesKind or SourceAxisMinorGarrisonsActivationKind or SourceAxisGarrisonRemainderKind or SourceGiarabubMotorMachineGunKind or SourceCwFrontlineKind or SourceGiarabubMotorPlacementPolicyKind or SourceTobrukBardiaGarrisonAdmissionKind or SourceTobrukBardiaGarrisonActivationKind or SourceBardiaGarrisonRemainderAdmissionKind or SourceBardiaGarrisonRemainderActivationKind or SourceTripoliTankRegimentAdmissionKind or SourceTripoliTankRegimentActivationKind or SourceCwAttachedArtilleryAdmissionKind or SourceCwAttachedArtilleryActivationKind or SourceMatruhFormationKind or SourceCwFreeGunsKind or SourceAxisPriorityMotorizationKind or SourceAxisMinorGarrisonsKind or SourceBattlefieldKind or SourceCairoSetupKind or SourceCairoWorldKind or SourceMalettiKind or SourceIndependentKind or SourceLocalMapKind or SourceExpandedIndependentKind or SourceAxisFreeWorldKind or SourceAxisCorpsWorldKind or SourceCertifiedMalettiWorldKind or SourceMatruhDetachedWorldKind or SourceCwPriorityWorldKind or SourceCwFreeGunsWorldKind or NativeCombatNoAttackKind or NativeSecondCombatNoAttackKind or NativeCombatBarrageKind
                or NativeCombatForceCompletionKind or NativeCombatForceCancellationKind
                or NativeCombatAntiArmorKind or NativeCombatCancelledAssaultKind
                or NativeCombatAssaultResultKind or NativeCombatNoPostAssaultRetreatKind
                or NativeCombatSettlementKind or NativeCombatNoCustodyKind
                or NativeCombatSuccessorKind or NativeReserveReleaseCompletionKind
                or NativeRepeatBreakdownKind or NativeSecondBreakdownKind or NativeRepeatCombatNoAttackKind or NativeCombatRbaCancellationKind
                or NativeStageEntryWeatherKind
                or NativeStageEntryOrganizationKind
                or NativeStageEntryNavalConvoyArrivalKind
                or NativeStageEntryFleetAssignmentKind
                or NativeStageEntryFleetRepairKind
                or NativeStageEntryBreakdownKind
                or NativeStageEntrySecondBreakdownKind
                or NativeCycleControlTimeoutKind or NativeReserveReleaseTimeoutKind or NativeCombatCustodyTimeoutKind or NativeCombatRetreatTimeoutKind or NativeCombatSelectionTimeoutKind
                && actingElementId is not null))
            throw new ArgumentException("Accepted step kind and acting element differ.", nameof(kind));
        ContractVersion = kind switch
        {
            LegacyKind => CurrentContractVersion,
            PregameAxisSupplyKind => PregameAxisSupplyContractVersion,
            PregameAxisSupplyFinishKind => PregameAxisSupplyFinishContractVersion,
            SourceAxisDumpKind => SourceAxisDumpContractVersion,
            SourceCommonwealthSupplyKind => SourceCommonwealthSupplyContractVersion,
            SourceDummyKind => SourceDummyContractVersion,
            SourceMotorizationKind => SourceMotorizationContractVersion,
            SourceUnattachedCargoKind => SourceUnattachedCargoContractVersion,
            SourceActionsKind => SourceActionsContractVersion,
            SourceAxisGt1SupplyKind => SourceAxisGt1SupplyContractVersion,
            SourceCommonwealthSupplyPlanKind => SourceCommonwealthSupplyPlanContractVersion,
            SourceAxisSupplementKind => SourceAxisSupplementContractVersion,
            SourceAutoblindaKind => SourceAutoblindaContractVersion,
            SourceCommonwealthSupplementKind => SourceCommonwealthSupplementContractVersion,
            SourcePriorityMotorizationKind => SourcePriorityMotorizationContractVersion,
            SourcePriorityMotorizationChoiceKind => SourcePriorityMotorizationChoiceContractVersion,
            SourceAttachedInitialLoadsKind => SourceAttachedInitialLoadsContractVersion,
            SourceAttachedInitialLoadsChoiceKind => SourceAttachedInitialLoadsChoiceContractVersion,
            SourceInitialWrecksKind => SourceInitialWrecksContractVersion,
            SourceInitialWrecksChoiceKind => SourceInitialWrecksChoiceContractVersion,
            SourceAxisFreeSetupKind => SourceAxisFreeSetupContractVersion,
            SourceAxisFreeSetupChoiceKind => SourceAxisFreeSetupChoiceContractVersion,
            SourceAxisCorpsSetupKind => SourceAxisCorpsSetupContractVersion,
            SourceAxisCorpsSetupChoiceKind => SourceAxisCorpsSetupChoiceContractVersion,
            SourceMalettiHierarchyKind => SourceMalettiHierarchyContractVersion,
            SourceMalettiHierarchyChoiceKind => SourceMalettiHierarchyChoiceContractVersion,
            SourceGt2CorrectionKind => SourceGt2CorrectionContractVersion,
            SourceGt2CorrectionChoiceKind => SourceGt2CorrectionChoiceContractVersion,
            SourceCwPriorityCatalogKind => SourceCwPriorityCatalogContractVersion,
            SourceCompactCheckpointKind => SourceCompactCheckpointContractVersion,
            SourceFourthIndianWorldKind => SourceFourthIndianWorldContractVersion,
            SourceMatruhGroupedWorldKind => SourceMatruhGroupedWorldContractVersion,
            SourceMalettiGroupedWorldKind => SourceMalettiGroupedWorldContractVersion,
            SourceLibyanGroupedWorldKind => SourceLibyanGroupedWorldContractVersion,
            SourceItalianDivisionsGroupedWorldKind => SourceItalianDivisionsGroupedWorldContractVersion,
            SourceSeventhArmoredGroupedWorldKind => SourceSeventhArmoredGroupedWorldContractVersion,
            SourceSixteenthBrigadeGroupedWorldKind => SourceSixteenthBrigadeGroupedWorldContractVersion,
            SourceCatanzaroGroupedWorldKind => SourceCatanzaroGroupedWorldContractVersion,
            SourceTurnStartKind => SourceTurnStartContractVersion,
            SourceTurnInitiativeKind => SourceTurnInitiativeContractVersion,
            SourceGt1BreakdownCompleteKind => SourceGt1BreakdownCompleteContractVersion,
            SourceGt1FleetCompleteKind => SourceGt1FleetCompleteContractVersion,
            SourceGt1ArrivalsCompleteKind => SourceGt1ArrivalsCompleteContractVersion,
            SourceGt1OrganizationCompleteKind => SourceGt1OrganizationCompleteContractVersion,
            SourceGt1SchedulingCompleteKind => SourceGt1SchedulingCompleteContractVersion,
            SourceGt1WeatherKind => SourceGt1WeatherContractVersion,
            SourceGt1Lane2AnnounceKind => SourceGt1Lane2AnnounceContractVersion,
            SourceGt1Lane3AnnounceKind => SourceGt1Lane3AnnounceContractVersion,
            SourceSeventhShellCorrectionKind => SourceSeventhShellCorrectionContractVersion,
            SourceGt1MultiLanesKind => SourceGt1MultiLanesContractVersion,
            SourceAxisMinorGarrisonsActivationKind => SourceAxisMinorGarrisonsActivationContractVersion,
            SourceAxisGarrisonRemainderKind => SourceAxisGarrisonRemainderContractVersion,
            SourceGiarabubMotorMachineGunKind => SourceGiarabubMotorMachineGunContractVersion,
            SourceCwFrontlineKind => SourceCwFrontlineContractVersion,
            SourceGiarabubMotorPlacementPolicyKind => SourceGiarabubMotorPlacementPolicyContractVersion,
            SourceTobrukBardiaGarrisonAdmissionKind => SourceTobrukBardiaGarrisonAdmissionContractVersion,
            SourceTobrukBardiaGarrisonActivationKind => SourceTobrukBardiaGarrisonActivationContractVersion,
            SourceBardiaGarrisonRemainderAdmissionKind => SourceBardiaGarrisonRemainderAdmissionContractVersion,
            SourceBardiaGarrisonRemainderActivationKind => SourceBardiaGarrisonRemainderActivationContractVersion,
            SourceTripoliTankRegimentAdmissionKind => SourceTripoliTankRegimentAdmissionContractVersion,
            SourceTripoliTankRegimentActivationKind => SourceTripoliTankRegimentActivationContractVersion,
            SourceCwAttachedArtilleryAdmissionKind => SourceCwAttachedArtilleryAdmissionContractVersion,
            SourceCwAttachedArtilleryActivationKind => SourceCwAttachedArtilleryActivationContractVersion,
            SourceCwPriorityCatalogChoiceKind => SourceCwPriorityCatalogChoiceContractVersion,
            SourceMatruhFormationKind => SourceMatruhFormationContractVersion,
            SourceMatruhFormationChoiceKind => SourceMatruhFormationChoiceContractVersion,
            SourceCwFreeGunsKind => SourceCwFreeGunsContractVersion,
            SourceCwFreeGunsChoiceKind => SourceCwFreeGunsChoiceContractVersion,
            SourceAxisPriorityMotorizationKind => SourceAxisPriorityMotorizationContractVersion,
            SourceAxisPriorityChoiceKind => SourceAxisPriorityChoiceContractVersion,
            SourceAxisMinorGarrisonsKind => SourceAxisMinorGarrisonsContractVersion,
            SourceBattlefieldKind => SourceBattlefieldContractVersion,
            SourceCairoSetupKind => SourceCairoSetupContractVersion,
            SourceCairoUnitKind => SourceCairoUnitContractVersion,
            SourceCairoWorldKind => SourceCairoWorldContractVersion,
            SourceMalettiKind => SourceMalettiContractVersion,
            SourceIndependentKind => SourceIndependentContractVersion,
            SourceLocalMapKind => SourceLocalMapContractVersion,
            SourceExpandedIndependentKind => SourceExpandedIndependentContractVersion,
            SourceAxisFreeWorldKind => SourceAxisFreeWorldContractVersion,
            SourceAxisCorpsWorldKind => SourceAxisCorpsWorldContractVersion,
            SourceCertifiedMalettiWorldKind => SourceCertifiedMalettiWorldContractVersion,
            SourceMatruhDetachedWorldKind => SourceMatruhDetachedWorldContractVersion,
            SourceCwPriorityWorldKind => SourceCwPriorityWorldContractVersion,
            SourceCwFreeGunsWorldKind => SourceCwFreeGunsWorldContractVersion,
            NativeSecondBreakdownKind => NativeSecondBreakdownContractVersion,
            NativeStageEntryWeatherKind => NativeStageEntryWeatherContractVersion,
            NativeStageEntryOrganizationKind => NativeStageEntryOrganizationContractVersion,
            NativeStageEntryNavalConvoyArrivalKind => NativeStageEntryNavalConvoyArrivalContractVersion,
            NativeStageEntryFleetAssignmentKind => NativeStageEntryFleetAssignmentContractVersion,
            NativeStageEntryFleetRepairKind => NativeStageEntryFleetRepairContractVersion,
            NativeStageEntryBreakdownKind => NativeStageEntryBreakdownContractVersion,
            NativeStageEntrySecondBreakdownKind => NativeStageEntrySecondBreakdownContractVersion,
            ContactStageEntryReserveKind => ContactStageEntryReserveContractVersion,
            ContactStageEntryMovementKind => ContactStageEntryMovementContractVersion,
            ContactStageEntryMovementCompletionKind => ContactStageEntryMovementCompletionContractVersion,
            ContactMovementKind => ContactMovementContractVersion,
            ContactInitiativeOrderKind => ContactInitiativeOrderContractVersion,
            ContactReserveDesignationKind => ContactReserveDesignationContractVersion,
            ContactReactionKind => ContactReactionContractVersion,
            ContactMovementSegmentCompletionKind => ContactMovementSegmentCompletionContractVersion,
            ContactCombatSelectionKind => ContactCombatSelectionContractVersion,
            ContactRbaDeclineKind => ContactRbaDeclineContractVersion,
            ContactRbaMoveKind => ContactRbaMoveContractVersion,
            ContactForceSealKind => ContactForceSealContractVersion,
            NativeCombatForceCompletionKind => NativeCombatForceCompletionContractVersion,
            NativeCombatForceCancellationKind => NativeCombatForceCancellationContractVersion,
            NativeCombatAntiArmorKind => NativeCombatAntiArmorContractVersion,
            NativeCombatCancelledAssaultKind => NativeCombatCancelledAssaultContractVersion,
            NativeCombatAssaultResultKind => NativeCombatAssaultResultContractVersion,
            ContactPostAssaultRetreatKind => ContactPostAssaultRetreatContractVersion,
            NativeCombatNoPostAssaultRetreatKind => NativeCombatNoPostAssaultRetreatContractVersion,
            NativeCombatSettlementKind => NativeCombatSettlementContractVersion,
            NativeCombatNoCustodyKind => NativeCombatNoCustodyContractVersion,
            ContactCombatCustodyKind => ContactCombatCustodyContractVersion,
            NativeCombatSuccessorKind => NativeCombatSuccessorContractVersion,
            NativeReserveReleaseCompletionKind => NativeReserveReleaseCompletionContractVersion,
            ContactReserveReleaseChoiceKind => ContactReserveReleaseChoiceContractVersion,
            ContactCycleControlKind => ContactCycleControlContractVersion,
            ContactTruckConvoyPassKind => ContactTruckConvoyPassContractVersion,
            ContactPostCombatPassKind => ContactPostCombatPassContractVersion,
            ContactSecondReserveKind => ContactSecondReserveContractVersion,
            ContactStageEntryInitiativeDeclarationKind => ContactStageEntryInitiativeDeclarationContractVersion,
            ContactSecondMovementKind => ContactSecondMovementContractVersion,
            ContactSecondMovementCompletionKind => ContactSecondMovementCompletionContractVersion,
            ContactRepeatMovementKind => ContactRepeatMovementContractVersion,
            NativeRepeatBreakdownKind => NativeRepeatBreakdownContractVersion,
            ContactRepeatCombatSelectionKind => ContactRepeatCombatSelectionContractVersion,
            NativeRepeatCombatNoAttackKind => NativeRepeatCombatNoAttackContractVersion,
            NativeCombatRbaCancellationKind => NativeCombatRbaCancellationContractVersion,
            NativeCycleControlTimeoutKind => NativeCycleControlTimeoutContractVersion,
            NativeReserveReleaseTimeoutKind => NativeReserveReleaseTimeoutContractVersion,
            NativeCombatCustodyTimeoutKind => NativeCombatCustodyTimeoutContractVersion,
            NativeCombatRetreatTimeoutKind => NativeCombatRetreatTimeoutContractVersion,
            NativeCombatSelectionTimeoutKind => NativeCombatSelectionTimeoutContractVersion,
            NativeCombatNoAttackKind => NativeCombatNoAttackContractVersion,
            NativeSecondCombatNoAttackKind => NativeSecondCombatNoAttackContractVersion,
            _ => NativeCombatBarrageContractVersion,
        };
        Kind = kind;
        ActingElementId = actingElementId;
        this.canonicalCommand = canonicalCommand?.ToArray()
            ?? throw new ArgumentNullException(nameof(canonicalCommand));
        ArgumentNullException.ThrowIfNull(eventRecords);
        if (eventRecords.Count == 0 || eventRecords.Any(value => value is null))
            throw new ArgumentException("Accepted steps require event records.", nameof(eventRecords));
        this.eventRecords = eventRecords.Select(value => value.ToArray()).ToArray();
        this.canonicalReceipt = canonicalReceipt?.ToArray()
            ?? throw new ArgumentNullException(nameof(canonicalReceipt));
        this.snapshotCheckpoint = snapshotCheckpoint?.ToArray()
            ?? throw new ArgumentNullException(nameof(snapshotCheckpoint));
    }

    public byte[] CanonicalCommand => canonicalCommand.ToArray();
    public int ContractVersion { get; }
    public string Kind { get; }
    public string? ActingElementId { get; }
    public IReadOnlyList<byte[]> EventRecords =>
        Array.AsReadOnly(eventRecords.Select(value => value.ToArray()).ToArray());
    public byte[] CanonicalReceipt => canonicalReceipt.ToArray();
    public byte[] SnapshotCheckpoint => snapshotCheckpoint.ToArray();

    internal bool Matches(ExerciseStepEvidence evidence) =>
        canonicalReceipt.AsSpan().SequenceEqual(
            CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt))
        && snapshotCheckpoint.AsSpan().SequenceEqual(evidence.SnapshotCheckpoint)
        && eventRecords.Length == evidence.EventRecords.Count
        && eventRecords.Zip(evidence.EventRecords).All(pair =>
            pair.First.AsSpan().SequenceEqual(pair.Second));
}

public enum CampaignHostImportRejectionReason { None, InvalidCreation, InvalidStep }

public sealed class CampaignHostImportResult
{
    private CampaignHostImportResult(ExerciseSession? session,
        CampaignHostImportRejectionReason rejectionReason)
    {
        Session = session;
        RejectionReason = rejectionReason;
    }

    public bool IsVerified => Session is not null;
    public ExerciseSession? Session { get; }
    public CampaignHostImportRejectionReason RejectionReason { get; }

    internal static CampaignHostImportResult Verified(ExerciseSession session) =>
        new(session, CampaignHostImportRejectionReason.None);
    internal static CampaignHostImportResult Rejected(CampaignHostImportRejectionReason reason) =>
        new(null, reason);
}
