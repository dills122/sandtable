using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Visibility;

namespace Cna.Core.Exercises;

public static partial class CampaignExercises
{
    internal static ExerciseStepResult CompleteEndOfTurn(ExerciseSession session) =>
        CompleteTurnBoundary(session, "land.position.end-of-turn");

    internal static ExerciseStepResult CompleteTurnInitiative(ExerciseSession session) =>
        CompleteTurnBoundary(session, "land.position.initiative-determination");

    internal static ExerciseStepResult CompleteTurnConvoySchedule(ExerciseSession session) =>
        CompleteTurnBoundary(session, "land.position.naval-convoy.schedule");

    internal static ExerciseStepResult CompleteTurnTacticalShipping(ExerciseSession session) =>
        CompleteTurnBoundary(session, "land.position.naval-convoy.tactical-shipping");

    internal static ExerciseStepResult SubmitStageOneDeclaration(ExerciseSession session,
        CampaignActionAudience audience, string actionId, bool actFirst) =>
        CompleteTurnBoundary(session, "land.position.operation-1.initiative-declaration", audience, actionId, actFirst);

    internal static ExerciseStepResult CompleteTurnStartWeather(ExerciseSession session) =>
        CompleteTurnBoundary(session, "land.position.operation-1.weather-determination");

    internal static ExerciseStepResult CompleteTurnStartOrganization(ExerciseSession session) =>
        CompleteTurnBoundary(session, "land.position.operation-1.organization");

    internal static ExerciseStepResult CompleteTurnStartConvoyArrival(ExerciseSession session) =>
        CompleteTurnBoundary(session, "land.position.operation-1.naval-convoy-arrival");

    internal static ExerciseStepResult CompleteTurnStartFleetAssignment(ExerciseSession session) =>
        CompleteTurnBoundary(session, "land.position.operation-1.commonwealth-fleet.assignment");

    internal static ExerciseStepResult CompleteTurnStartFleetRepair(ExerciseSession session) =>
        CompleteTurnBoundary(session, "land.position.operation-1.commonwealth-fleet.repair");

    private static ExerciseStepResult CompleteTurnBoundary(ExerciseSession session, string position,
        CampaignActionAudience audience = CampaignActionAudience.System, string? actionId = null, bool actFirst = true)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            if (session.Pregame is not null || !Cna1979BreakdownSetupCatalog.AdmitsLaterTurnPasses(session.CurrentSnapshot.Setup))
                throw new CampaignRuleNotImplementedException("Content has no admitted later-turn policy.");
            if (QueryCheckpoint(session).PositionId != position)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
            var campaignEvent = CreateTurnBoundaryEvent(session, actFirst, audience);
            var info = CampaignGameTurn.BoundaryInfo(campaignEvent);
            var bytes = CampaignCurrentEventSerializer.Serialize(campaignEvent, session.Context);
            var contacts = AdvanceTurnBoundaryContacts(session.CurrentContacts, campaignEvent);
            var next = session.WithTurnBoundaryEvent(campaignEvent, contacts);
            var receipt = new CampaignActionAcceptanceReceipt(info.CampaignId, info.PriorStateVersion,
                checked(info.PriorStateVersion + 1), info.NextPositionId, audience, actionId ?? Hash(bytes));
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (CampaignRuleNotImplementedException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.RuleNotImplemented);
        }
        catch (Exception error) when (IsExpectedCampaignException(error))
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static object CreateTurnBoundaryEvent(ExerciseSession session, bool actFirst, CampaignActionAudience audience)
    {
        var turn = CampaignGameTurn.CurrentGameTurn(session);
        var version = session.CurrentContacts.AuthorityVersion;
        var admitted = Cna1979BreakdownSetupCatalog.AdmitsLaterTurnPasses(session.CurrentSnapshot.Setup);
        var position = QueryCheckpoint(session).PositionId;
        if (position == "land.position.end-of-turn")
        {
            if (!admitted)
                throw new CampaignRuleNotImplementedException("Rollover requires explicit later-turn policy.");
            if (audience != CampaignActionAudience.System || session.Pending is not ThirdStageSecondAssaultPending
                || session.CurrentHistory[^1] is not CampaignNativePostCombatPassV42 terminal)
                throw new InvalidOperationException("End of Turn requires the terminal pass.");
            return CampaignNativeEndOfTurnV55.Create(terminal, turn, version, session.Context.Scenario.End.GameTurn);
        }
        if (session.Pending is not TurnBoundaryPending || !admitted)
            throw new CampaignRuleNotImplementedException("Later-turn prelude requires explicit later-turn policy.");
        var predecessor = session.CurrentHistory[^1];
        var holder = CampaignGameTurn.LatestInitiativeHolder(session.CurrentHistory,
            session.CurrentSnapshot.InitiativeHolder ?? throw new InvalidOperationException("Missing initial holder."));
        var random = ExerciseSession.LatestCursor(session.CurrentSnapshot.RandomState, session.CurrentHistory);
        if (position == "land.position.operation-1.initiative-declaration")
        {
            if (audience != (holder == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth))
                throw new InvalidOperationException("Only the current initiative holder may declare.");
            return CampaignNativeInitiativeDeclarationV49.Create(predecessor, holder, actFirst, turn, version);
        }
        if (audience != CampaignActionAudience.System)
            throw new InvalidOperationException("This turn position requires System authority.");
        return predecessor switch
        {
            CampaignNativeEndOfTurnV55 end when position == "land.position.initiative-determination" =>
                CampaignNativeTurnInitiativeV56.Create(end, session.CurrentSnapshot.Setup.InitialInitiative, random, version),
            CampaignNativeTurnInitiativeV56 or CampaignNativeNoObligationTurnPhaseV57
                when position is "land.position.naval-convoy.schedule" or "land.position.naval-convoy.tactical-shipping" =>
                CampaignNativeNoObligationTurnPhaseV57.Create(predecessor, admitted, version),
            CampaignNativeInitiativeDeclarationV49 declaration when position == "land.position.operation-1.weather-determination" =>
                CampaignNativeWeatherDeterminationV50.Create(declaration, random, turn, version),
            CampaignNativeWeatherDeterminationV50 weather when position == "land.position.operation-1.organization" =>
                CampaignNativeNoObligationOrganizationV51.Create(weather, version),
            CampaignNativeNoObligationOrganizationV51 organization when position == "land.position.operation-1.naval-convoy-arrival" =>
                CampaignNativeNoObligationNavalConvoyArrivalV52.Create(organization, session.Context, version),
            CampaignNativeNoObligationNavalConvoyArrivalV52 arrival when position == "land.position.operation-1.commonwealth-fleet.assignment" =>
                CampaignNativeNoObligationFleetAssignmentV53.Create(arrival, version),
            CampaignNativeNoObligationFleetAssignmentV53 assignment when position == "land.position.operation-1.commonwealth-fleet.repair" =>
                CampaignNativeNoObligationFleetRepairV54.Create(assignment, version),
            _ => throw new InvalidOperationException("Turn prelude has no supported successor."),
        };
    }

    private static CampaignContactJournal AdvanceTurnBoundaryContacts(CampaignContactJournal contacts, object campaignEvent) =>
        campaignEvent switch
        {
            CampaignNativeEndOfTurnV55 end => contacts.AdvanceEndOfTurn(end),
            CampaignNativeTurnInitiativeV56 initiative => contacts.AdvanceTurnInitiative(initiative),
            CampaignNativeNoObligationTurnPhaseV57 phase => contacts.AdvanceNoObligationTurnPhase(phase),
            CampaignNativeInitiativeDeclarationV49 declaration => contacts.AdvanceInitiativeDeclaration(declaration),
            CampaignNativeWeatherDeterminationV50 weather => contacts.AdvanceWeatherDetermination(weather),
            CampaignNativeNoObligationOrganizationV51 organization => contacts.AdvanceNoObligationOrganization(organization),
            CampaignNativeNoObligationNavalConvoyArrivalV52 arrival => contacts.AdvanceNoObligationNavalConvoyArrival(arrival),
            CampaignNativeNoObligationFleetAssignmentV53 assignment => contacts.AdvanceNoObligationFleetAssignment(assignment),
            CampaignNativeNoObligationFleetRepairV54 repair => contacts.AdvanceNoObligationFleetRepair(repair),
            _ => throw new InvalidOperationException("Invalid turn boundary event."),
        };

    private static ExerciseReconstructionResult ReconstructTurnBoundary(ExerciseSession session,
        bool includeStreamHash, bool predecessorVerified)
    {
        var expectedHash = Hash([]);
        try
        {
            var predecessor = session.TurnBoundaryPredecessor
                ?? throw new InvalidCampaignHistoryException("Missing turn boundary predecessor.");
            if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                throw new InvalidCampaignHistoryException("Turn boundary predecessor did not replay.");
            if (session.CurrentHistory.Count != predecessor.CurrentHistory.Count + 1
                || !session.CurrentHistory.Take(predecessor.CurrentHistory.Count).SequenceEqual(predecessor.CurrentHistory))
                throw new InvalidCampaignHistoryException("Turn boundary history does not extend its predecessor.");
            var candidate = session.CurrentHistory[^1];
            var expected = CampaignCurrentEventSerializer.Serialize(candidate, session.Context);
            expectedHash = Hash(expected);
            CampaignGameTurn.RequireCurrentGameTurn(predecessor, CampaignGameTurn.BoundaryInfo(candidate).GameTurn);
            var (audience, actFirst) = candidate is CampaignNativeInitiativeDeclarationV49 declaration
                ? (declaration.DeclaringHolder == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth,
                    declaration.FirstSide == declaration.DeclaringHolder)
                : (CampaignActionAudience.System, true);
            var regenerated = CreateTurnBoundaryEvent(predecessor, actFirst, audience);
            var replayed = CampaignCurrentEventSerializer.Serialize(regenerated, session.Context);
            var contacts = AdvanceTurnBoundaryContacts(predecessor.CurrentContacts, regenerated);
            if (!expected.AsSpan().SequenceEqual(replayed) || session.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                || session.CurrentContacts.Records.Count != contacts.Records.Count
                || session.CurrentContacts.Records.Zip(contacts.Records).Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                throw new InvalidCampaignHistoryException("Turn boundary differs from replay.");
            var streamHash = includeStreamHash
                ? HashFramed(session.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, session.Context))) : null;
            return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                replayed, streamHash, expectedHash, Hash(replayed));
        }
        catch (Exception error) when (IsExpectedCampaignException(error) || error is KeyNotFoundException)
        {
            return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                null, null, expectedHash, null);
        }
    }

    internal static IReadOnlyList<CampaignOperationStageOrder> TurnStartOrders(ExerciseSession session) =>
        [.. session.CurrentSnapshot.OperationStageOrders, .. session.CurrentHistory.OfType<CampaignNativeInitiativeDeclarationV49>().Select(declaration => declaration.ToOrder())];

    private static readonly ReserveSourceSlot TurnStartReserveSourceSlot = new(
        "Turn start ",
        _ => null,
        _ => null,
        session => session.Pending is TurnBoundaryPending && session.CurrentHistory[^1] is CampaignNativeNoObligationFleetRepairV54 repair ? repair : null,
        session => session.CurrentThirdStageSecondAssault?.Release?.World,
        "Turn-start Reserve requires the previous turn's settled World.",
        "Turn-start Reserve requires a completed later-turn prelude.");

    // Read-only opening: the previous turn's settled world is carried unchanged by the prelude.
    internal static (CampaignNativeNoObligationFleetRepairV54 Predecessor, CampaignWorldSnapshotV6 World) QueryTurnStartReserve(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var (predecessor, world) = QueryReserveSourceCore(session, TurnStartReserveSourceSlot);
        if (predecessor is not CampaignNativeNoObligationFleetRepairV54 repair
            || repair.NextPositionId != "land.position.operation-1.first-player.reserve-designation"
            || repair.GameTurn != CampaignGameTurn.CurrentGameTurn(session)
            || CampaignNativeCombatPaidWorldV22.Hash(world) != repair.WorldHash)
            throw new InvalidOperationException("Turn-start Reserve requires the completed prelude and settled prior-turn world.");
        return (repair, world);
    }
}
