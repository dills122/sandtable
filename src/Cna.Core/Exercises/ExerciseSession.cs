using System.Collections.Immutable;
using Cna.Core.Campaigns;
using Cna.Core.Visibility;

namespace Cna.Core.Exercises;

public sealed class ExerciseSession
{
    internal ExerciseSession(CampaignSnapshotV11 snapshot, CampaignContentContext context,
        IEnumerable<object> history, CampaignContactJournal? contacts = null,
        CampaignNativeCombatDecisionStateV12? combatSelection = null,
        CampaignNativeCombatNoAttackCompletionV13? noAttackCompletion = null,
        CampaignNativeCombatBarrageV14? combatBarrage = null,
        CampaignNativeCombatRetreatDeclineV15? combatRetreatDecline = null,
        CampaignNativeCombatRetreatMoveV16? combatRetreatMove = null,
        CampaignWorldSnapshotV6? combatRetreatWorld = null,
        CampaignContactJournal? combatRetreatPriorContacts = null,
        IEnumerable<CampaignNativeCombatRetreatMoveV16>? combatRetreatMoves = null,
        IEnumerable<CampaignNativeCombatForceSealV17>? combatForceSeals = null,
        CampaignNativeCombatForceCompletionV18? combatForceCompletion = null,
        CampaignNativeCombatForceCancellationV19? combatForceCancellation = null,
        CampaignNativeCombatAntiArmorV20? combatAntiArmor = null,
        CampaignNativeCombatCancelledAssaultV21? combatCancelledAssault = null,
        CampaignNativeCombatAssaultResultV22? combatAssaultResult = null,
        CampaignWorldSnapshotV6? combatPaidWorld = null,
        CampaignNativeCombatRetreatAfterAssaultV23? combatPostResultRetreat = null,
        CampaignNativeCombatSettlementV24? combatSettlement = null,
        CampaignWorldSnapshotV6? combatSettledWorld = null,
        CampaignNativeCombatCustodyDispositionV25? combatCustodyDisposition = null,
        CampaignWorldSnapshotV6? combatCustodyWorld = null,
        CampaignNativeCombatCustodyStateV25? combatCustodyState = null,
        CampaignNativeCombatSuccessorV26? combatSuccessor = null,
        IEnumerable<CampaignNativeReserveReleaseEventV27>? reserveReleaseEvents = null,
        CampaignNativeCycleControlV28? cycleControl = null,
        CampaignNativeRepeatMovementV29? repeatMove = null,
        CampaignWorldSnapshotV6? repeatMoveWorld = null,
        CampaignNativeRepeatBreakdownV30? repeatBreakdown = null,
        CampaignNativeRepeatCombatDecisionV31? repeatCombatSelection = null,
        CampaignNativeRepeatCombatNoAttackV32? repeatNoAttack = null,
        CampaignContactJournal? repeatAssaultBaseContacts = null,
        IEnumerable<object>? repeatAssaultEvents = null,
        CampaignContactJournal? postRepeatBaseContacts = null,
        IEnumerable<object>? postRepeatEvents = null)
    {
        CurrentSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        ArgumentNullException.ThrowIfNull(history);
        CurrentHistory = history as ImmutableList<object> ?? ImmutableList.CreateRange(history);
        CurrentContacts = contacts ?? CampaignContactJournal.Empty(snapshot.CampaignId);
        var laterEvents = postRepeatEvents?.ToArray() ?? [];
        var assaultEvents = repeatAssaultEvents?.ToArray() ?? [];
        if (laterEvents.Length + assaultEvents.Length > CurrentHistory.Count
            || laterEvents.Length != 0 && assaultEvents.Length != 0
            || !CurrentHistory.TakeLast(laterEvents.Length).SequenceEqual(laterEvents))
            throw new ArgumentException(
                "Later-cycle events must be retained at history tail.",
                nameof(postRepeatEvents));
        var prePostHistory = CurrentHistory.Take(
            CurrentHistory.Count - laterEvents.Length - assaultEvents.Length).ToArray();
        if (!CurrentHistory.Skip(prePostHistory.Length).Take(assaultEvents.Length)
                .SequenceEqual(assaultEvents))
            throw new ArgumentException(
                "Repeated assault events must be retained after selection.",
                nameof(repeatAssaultEvents));
        var prePostContacts = postRepeatBaseContacts
            ?? repeatAssaultBaseContacts ?? CurrentContacts;
        if (combatSelection is not null && combatSelection.PriorSnapshot != snapshot)
            throw new ArgumentException("Combat selection must bind the session's prior snapshot.", nameof(combatSelection));
        if (noAttackCompletion is not null && (combatSelection is null
            || noAttackCompletion.PriorStateVersion != combatSelection.StateVersion
            || noAttackCompletion.DecisionActionId != combatSelection.Decision.ActionId))
            throw new ArgumentException("No-attack completion must bind the owner decision.", nameof(noAttackCompletion));
        if (combatBarrage is not null && (combatSelection is null
            || noAttackCompletion is not null
            || combatBarrage.PriorStateVersion != combatSelection.StateVersion
            || combatBarrage.DecisionActionId != combatSelection.Decision.ActionId))
            throw new ArgumentException("Barrage must bind a selected assault.", nameof(combatBarrage));
        if (combatRetreatDecline is not null && (combatBarrage is null
            || combatSelection is null
            || combatRetreatDecline.PriorStateVersion
                != (combatRetreatMove?.StateVersion ?? combatBarrage.StateVersion)
            || combatRetreatDecline.DecisionActionId != combatSelection.Decision.ActionId))
            throw new ArgumentException("RBA decline must bind the selected Barrage.",
                nameof(combatRetreatDecline));
        var moves = combatRetreatMoves?.ToArray()
            ?? (combatRetreatMove is null ? [] : [combatRetreatMove]);
        if (moves.Length > 0 && (combatRetreatMove != moves[^1]
            || combatBarrage is null
            || combatSelection is null
            || moves[0].PriorStateVersion != combatBarrage.StateVersion
            || moves.Skip(1).Where((move, index) =>
                move.PriorStateVersion != moves[index].StateVersion
                || move.DefenderElementId != moves[0].DefenderElementId
                || move.InitiallyAdjacentToKnownEnemy != moves[0].InitiallyAdjacentToKnownEnemy).Any()
            || combatRetreatMove.DecisionActionId != combatSelection.Decision.ActionId
            || combatRetreatWorld is null
            || combatRetreatPriorContacts?.AuthorityVersion != combatBarrage.StateVersion
            || combatRetreatWorld.Representations.All(value =>
                !value.BoundElementIds.Contains(combatRetreatMove.DefenderElementId)
                || value.CurrentLocationId != combatRetreatMove.DestinationLocationId)))
            throw new ArgumentException("RBA move must bind the selected Barrage and moved world.",
                nameof(combatRetreatMove));
        if (combatRetreatMove is null
            && (combatRetreatWorld is not null || combatRetreatPriorContacts is not null))
            throw new ArgumentException("RBA world and prior contact cut require a retained move.",
                nameof(combatRetreatWorld));
        var forceSeals = combatForceSeals?.ToArray() ?? [];
        if (forceSeals.Length > 2 || (forceSeals.Length > 0 && (
            combatRetreatDecline is null || combatSelection is null
            || forceSeals[0].PriorStateVersion != combatRetreatDecline.StateVersion
            || forceSeals[0].DecisionActionId != combatSelection.Decision.ActionId))
            || (forceSeals.Length == 2 && (
                forceSeals[1].PriorStateVersion != forceSeals[0].StateVersion
                || forceSeals[1].OwnerSide == forceSeals[0].OwnerSide
                || forceSeals[1].PriorSealHash is null)))
            throw new ArgumentException("Force seals must bind distinct owners and retained Combat authority.",
                nameof(combatForceSeals));
        if (combatForceCompletion is not null && (forceSeals.Length != 2
            || combatForceCompletion.PriorStateVersion != forceSeals[1].StateVersion
            || combatForceCompletion.CampaignId != snapshot.CampaignId))
            throw new ArgumentException("Force completion requires both retained owner seals.",
                nameof(combatForceCompletion));
        if (combatForceCancellation is not null && (combatForceCompletion is not null
            || forceSeals.Length > 1 || combatRetreatDecline is null
            || combatForceCancellation.PriorStateVersion
                != (forceSeals.Length == 0 ? combatRetreatDecline.StateVersion
                    : forceSeals[0].StateVersion)
            || combatForceCancellation.CampaignId != snapshot.CampaignId))
            throw new ArgumentException("Force cancellation requires an incomplete retained round.",
                nameof(combatForceCancellation));
        if (combatAntiArmor is not null && ((combatForceCompletion is null) == (combatForceCancellation is null)
            || combatAntiArmor.PriorStateVersion
                != (combatForceCompletion?.StateVersion ?? combatForceCancellation!.StateVersion)
            || combatAntiArmor.CampaignId != snapshot.CampaignId))
            throw new ArgumentException("Anti Armor must bind one retained Force outcome.",
                nameof(combatAntiArmor));
        if (combatCancelledAssault is not null && (combatAntiArmor is null
            || combatForceCancellation is null
            || combatCancelledAssault.PriorStateVersion != combatAntiArmor.StateVersion
            || combatCancelledAssault.CampaignId != snapshot.CampaignId))
            throw new ArgumentException("Cancelled Close Assault must bind retained Anti Armor.",
                nameof(combatCancelledAssault));
        if (combatAssaultResult is not null && (combatAntiArmor is null
            || combatForceCompletion is null || combatForceCancellation is not null
            || combatCancelledAssault is not null
            || combatAssaultResult.PriorStateVersion != combatAntiArmor.StateVersion
            || combatAssaultResult.CampaignId != snapshot.CampaignId
            || combatPaidWorld is null
            || combatAssaultResult.PaidWorldHash
                != CampaignNativeCombatPaidWorldV22.Hash(combatPaidWorld)))
            throw new ArgumentException("Assault result must bind completed Force and Anti Armor.",
                nameof(combatAssaultResult));
        if (combatAssaultResult is null && combatPaidWorld is not null)
            throw new ArgumentException("Paid World requires a retained assault result.",
                nameof(combatPaidWorld));
        if (combatPostResultRetreat is not null && (combatAssaultResult is null
            || combatPostResultRetreat.CampaignId != snapshot.CampaignId
            || combatPostResultRetreat.PriorStateVersion != combatAssaultResult.StateVersion
            || combatPostResultRetreat.ResultHash != "sha256:"
                + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
                    combatAssaultResult.SerializeCanonical()))
            || combatPostResultRetreat.PaidWorldHash != combatAssaultResult.PaidWorldHash))
            throw new ArgumentException("Post-result retreat must bind the retained assault.",
                nameof(combatPostResultRetreat));
        if (combatSettlement is not null && (combatPostResultRetreat is null
            || combatSettledWorld is null
            || combatSettlement.CampaignId != snapshot.CampaignId
            || combatSettlement.PriorStateVersion != combatPostResultRetreat.StateVersion
            || combatSettlement.RetreatHash != "sha256:"
                + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
                    combatPostResultRetreat.SerializeCanonical()))
            || combatSettlement.SettledWorldHash
                != CampaignNativeCombatPaidWorldV22.Hash(combatSettledWorld)))
            throw new ArgumentException("Settlement must bind the retained retreat and World.",
                nameof(combatSettlement));
        if (combatSettlement is null && combatSettledWorld is not null)
            throw new ArgumentException("Settled World requires an accepted settlement.",
                nameof(combatSettledWorld));
        if (combatCustodyDisposition is not null && (combatSettlement is null
            || combatCustodyWorld is null
            || combatCustodyDisposition.CampaignId != snapshot.CampaignId
            || combatCustodyDisposition.PriorStateVersion != combatSettlement.StateVersion
            || combatCustodyDisposition.SettlementHash != "sha256:"
                + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
                    combatSettlement.SerializeCanonical()))
            || combatCustodyDisposition.WorldHash
                != CampaignNativeCombatPaidWorldV22.Hash(combatCustodyWorld)
            || combatCustodyDisposition.CustodyStateHash != "sha256:"
                + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
                    CampaignNativeCombatCustodyProjectionV25.SerializeStateCanonical(
                        combatCustodyState)))
            || (combatCustodyDisposition.ChoiceKind == "not-required")
                != (combatCustodyState is null)))
            throw new ArgumentException("Custody must bind retained settlement and assets.",
                nameof(combatCustodyDisposition));
        if (combatCustodyDisposition is null
            && (combatCustodyWorld is not null || combatCustodyState is not null))
            throw new ArgumentException("Custody assets require an accepted disposition.",
                nameof(combatCustodyWorld));
        if (combatSuccessor is not null && (combatSelection is null
            || combatAssaultResult is null || combatSettlement is null
            || combatCustodyDisposition is null || combatCustodyWorld is null
            || !combatSuccessor.SerializeCanonical().AsSpan().SequenceEqual(
                CampaignNativeCombatSuccessorV26.Create(combatSelection,
                    combatAssaultResult, combatSettlement, combatCustodyDisposition,
                    combatCustodyWorld, combatCustodyState, context).SerializeCanonical())))
            throw new ArgumentException("Combat successor must bind accepted custody and World.",
                nameof(combatSuccessor));
        if (repeatNoAttack is not null && repeatCombatSelection is null)
            throw new ArgumentException(
                "Repeated finish needs an accepted Combat selection.",
                nameof(repeatNoAttack));
        var cycleTail = new object?[]
        {
            cycleControl, repeatMove, repeatBreakdown,
            repeatCombatSelection, repeatNoAttack,
        }.Where(value => value is not null).Cast<object>().ToArray();
        var reserveEvents = reserveReleaseEvents?.ToArray() ?? [];
        if (reserveEvents.Length > 0)
        {
            if (combatSuccessor is null || combatSelection is null
                || combatAssaultResult is null || combatSettlement is null
                || combatCustodyDisposition is null || combatCustodyWorld is null
                || prePostHistory.Length < reserveEvents.Length + cycleTail.Length
                || !prePostHistory.TakeLast(reserveEvents.Length
                        + cycleTail.Length).Take(reserveEvents.Length)
                    .SequenceEqual(reserveEvents.Cast<object>()))
                throw new ArgumentException("Reserve Release requires the retained Combat successor and events.",
                    nameof(reserveReleaseEvents));
            var projection = CampaignNativeCombatSuccessorV26.Project(combatSelection,
                combatAssaultResult, combatSettlement, combatCustodyDisposition,
                combatCustodyWorld, combatCustodyState, context);
            var initial = CampaignNativeReserveReleaseStateV27.Initialize(
                combatSuccessor, projection, context);
            CurrentReserveRelease = CampaignNativeReserveReleaseStateV27.Replay(
                initial, context, reserveEvents.Select(value =>
                    (ReadOnlyMemory<byte>)value.SerializeCanonical()));
            if (CurrentReserveRelease.StateVersion
                != (cycleControl?.PriorStateVersion ?? CurrentContacts.AuthorityVersion))
                throw new ArgumentException("Reserve Release contact authority is stale.",
                    nameof(contacts));
        }
        CurrentReserveReleaseEvents = Array.AsReadOnly(reserveEvents);
        if (cycleControl is not null && (CurrentReserveRelease is not { Completed: true } release
            || cycleControl.CampaignId != snapshot.CampaignId
            || cycleControl.PriorStateVersion != release.StateVersion
            || cycleControl.PriorEventHash != release.HeadEventHash
            || cycleControl.PriorCycleOrdinal != release.CycleOrdinal
            || cycleControl.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(release.World)
            || prePostContacts.AuthorityVersion
                != (repeatNoAttack?.StateVersion
                    ?? repeatCombatSelection?.StateVersion
                    ?? repeatBreakdown?.StateVersion
                    ?? repeatMove?.StateVersion ?? cycleControl.StateVersion)
            || !Equals(prePostHistory[^cycleTail.Length], cycleControl)))
            throw new ArgumentException("Cycle control must bind completed Reserve Release.",
                nameof(cycleControl));
        CurrentCycleControl = cycleControl;
        if (repeatMove is not null && (cycleControl?.Choice
                != CampaignNativeCycleControlChoiceV28.Repeat
            || CurrentReserveRelease is not { } repeatedRelease
            || repeatMoveWorld is null
            || repeatMove.CampaignId != snapshot.CampaignId
            || repeatMove.PriorStateVersion != cycleControl.StateVersion
            || repeatMove.PriorWorldHash
                != CampaignNativeCombatPaidWorldV22.Hash(repeatedRelease.World)
            || repeatMove.ResultWorldHash
                != CampaignNativeCombatPaidWorldV22.Hash(repeatMoveWorld)
            || !Equals(prePostHistory[^(cycleTail.Length - 1)], repeatMove)))
            throw new ArgumentException("Repeated Movement must bind accepted repeat and World.",
                nameof(repeatMove));
        if (repeatMove is null && repeatMoveWorld is not null)
            throw new ArgumentException("Repeated World needs its accepted move.",
                nameof(repeatMoveWorld));
        CurrentRepeatMove = repeatMove;
        CurrentRepeatMoveWorld = repeatMoveWorld;
        if (repeatBreakdown is not null && (repeatMove is null
            || repeatBreakdown.CampaignId != snapshot.CampaignId
            || repeatBreakdown.PriorStateVersion != repeatMove.StateVersion
            || repeatBreakdown.WorldHash != repeatMove.ResultWorldHash
            || !Equals(prePostHistory[^(cycleTail.Length - 2)],
                repeatBreakdown)))
            throw new ArgumentException("Repeated Breakdown must bind accepted Movement.",
                nameof(repeatBreakdown));
        CurrentRepeatBreakdown = repeatBreakdown;
        if (repeatCombatSelection is not null && (repeatBreakdown is null
            || repeatCombatSelection.CampaignId != snapshot.CampaignId
            || repeatCombatSelection.PriorStateVersion != repeatBreakdown.StateVersion
            || repeatCombatSelection.PriorEventHash != "sha256:"
                + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
                    repeatBreakdown.SerializeCanonical()))
            || !Equals(prePostHistory[^(cycleTail.Length - 3)],
                repeatCombatSelection)))
            throw new ArgumentException(
                "Repeated Combat selection must bind accepted Breakdown.",
                nameof(repeatCombatSelection));
        CurrentRepeatCombatSelection = repeatCombatSelection;
        if (repeatNoAttack is not null)
        {
            if (repeatMoveWorld is null || combatAssaultResult is null
                || repeatNoAttack.PriorStateVersion != repeatCombatSelection!.StateVersion
                || !repeatNoAttack.SerializeCanonical().AsSpan().SequenceEqual(
                    CampaignNativeRepeatCombatNoAttackV32.Create(
                        repeatCombatSelection,
                        CampaignCombatSelectionFrame.FromRepeat(snapshot,
                            repeatCombatSelection, repeatMoveWorld,
                            combatAssaultResult.AfterRandomState),
                        context).SerializeCanonical())
                || !Equals(prePostHistory[^1], repeatNoAttack))
                throw new ArgumentException(
                    "Repeated finish must bind retained World and selection.",
                    nameof(repeatNoAttack));
        }
        CurrentRepeatNoAttack = repeatNoAttack;
        if (repeatAssaultBaseContacts is not null || assaultEvents.Length != 0)
        {
            if (repeatCombatSelection is not { Choice: "select-contact-assault" }
                || repeatNoAttack is not null || repeatMoveWorld is null
                || combatAssaultResult is null
                || repeatAssaultBaseContacts is null)
                throw new ArgumentException(
                    "Repeated assault needs retained selected Combat authority.",
                    nameof(repeatAssaultEvents));
            var runtime = CampaignNativeRepeatAssaultRuntime.Replay(snapshot,
                repeatCombatSelection, repeatMoveWorld,
                combatAssaultResult.AfterRandomState, context,
                repeatAssaultBaseContacts, prePostHistory, assaultEvents);
            if (CurrentContacts.AuthorityVersion
                    != runtime.Contacts.AuthorityVersion
                || CurrentContacts.Records.Count != runtime.Contacts.Records.Count
                || CurrentContacts.Records.Zip(runtime.Contacts.Records)
                    .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                throw new ArgumentException(
                    "Repeated assault contacts differ from retained replay.",
                    nameof(contacts));
            CurrentRepeatAssault = runtime;
        }
        CurrentRepeatAssaultBaseContacts = repeatAssaultBaseContacts;
        CurrentRepeatAssaultEvents = Array.AsReadOnly(assaultEvents);
        if (repeatNoAttack is null && (postRepeatBaseContacts is not null
            || laterEvents.Length != 0))
            throw new ArgumentException(
                "Later-cycle runtime needs a completed repeated Combat cut.",
                nameof(postRepeatEvents));
        if (repeatNoAttack is not null)
        {
            if (repeatMoveWorld is null || combatAssaultResult is null
                || prePostContacts.AuthorityVersion != repeatNoAttack.StateVersion)
                throw new ArgumentException(
                    "Later-cycle source contact cut is stale.",
                    nameof(postRepeatBaseContacts));
            var runtime = CampaignNativePostRepeatRuntime.Replay(snapshot,
                context, combatAssaultResult.AfterRandomState,
                repeatCombatSelection!, repeatNoAttack, repeatMoveWorld,
                prePostContacts, laterEvents);
            if (CurrentContacts.AuthorityVersion
                    != runtime.Contacts.AuthorityVersion
                || CurrentContacts.Records.Count != runtime.Contacts.Records.Count
                || CurrentContacts.Records.Zip(runtime.Contacts.Records)
                    .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                throw new ArgumentException(
                    "Later-cycle contacts differ from retained replay.",
                    nameof(contacts));
            CurrentPostRepeat = runtime;
        }
        CurrentPostRepeatBaseContacts = postRepeatBaseContacts;
        CurrentPostRepeatEvents = Array.AsReadOnly(laterEvents);
        CurrentCombatDecision = combatSelection;
        CurrentNoAttackCompletion = noAttackCompletion;
        CurrentCombatBarrage = combatBarrage;
        CurrentCombatRetreatDecline = combatRetreatDecline;
        CurrentCombatRetreatMove = combatRetreatMove;
        CurrentCombatRetreatWorld = combatRetreatWorld;
        CurrentCombatRetreatPriorContacts = combatRetreatPriorContacts;
        CurrentCombatRetreatMoves = Array.AsReadOnly(moves);
        CurrentCombatForceSeals = Array.AsReadOnly(forceSeals);
        CurrentCombatForceCompletion = combatForceCompletion;
        CurrentCombatForceCancellation = combatForceCancellation;
        CurrentCombatAntiArmor = combatAntiArmor;
        CurrentCombatCancelledAssault = combatCancelledAssault;
        CurrentCombatAssaultResult = combatAssaultResult;
        CurrentCombatPaidWorld = combatPaidWorld;
        CurrentCombatPostResultRetreat = combatPostResultRetreat;
        CurrentCombatSettlement = combatSettlement;
        CurrentCombatSettledWorld = combatSettledWorld;
        CurrentCombatCustodyDisposition = combatCustodyDisposition;
        CurrentCombatCustodyWorld = combatCustodyWorld;
        CurrentCombatCustodyState = combatCustodyState;
        CurrentCombatSuccessor = combatSuccessor;
    }

    internal ExerciseSession WithReserveReleaseEvent(
        CampaignNativeReserveReleaseEventV27 reserveEvent,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(reserveEvent), contacts, CurrentCombatDecision,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatAssaultResult: CurrentCombatAssaultResult,
            combatPaidWorld: CurrentCombatPaidWorld,
            combatPostResultRetreat: CurrentCombatPostResultRetreat,
            combatSettlement: CurrentCombatSettlement,
            combatSettledWorld: CurrentCombatSettledWorld,
            combatCustodyDisposition: CurrentCombatCustodyDisposition,
            combatCustodyWorld: CurrentCombatCustodyWorld,
            combatCustodyState: CurrentCombatCustodyState,
            combatSuccessor: CurrentCombatSuccessor,
            reserveReleaseEvents: [.. CurrentReserveReleaseEvents, reserveEvent]);

    internal ExerciseSession WithCycleControlEvent(
        CampaignNativeCycleControlV28 control,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(control), contacts, CurrentCombatDecision,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatAssaultResult: CurrentCombatAssaultResult,
            combatPaidWorld: CurrentCombatPaidWorld,
            combatPostResultRetreat: CurrentCombatPostResultRetreat,
            combatSettlement: CurrentCombatSettlement,
            combatSettledWorld: CurrentCombatSettledWorld,
            combatCustodyDisposition: CurrentCombatCustodyDisposition,
            combatCustodyWorld: CurrentCombatCustodyWorld,
            combatCustodyState: CurrentCombatCustodyState,
            combatSuccessor: CurrentCombatSuccessor,
            reserveReleaseEvents: CurrentReserveReleaseEvents,
            cycleControl: control);

    internal ExerciseSession WithRepeatMovementEvent(
        CampaignNativeRepeatMovementV29 movement,
        CampaignWorldSnapshotV6 world,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(movement), contacts, CurrentCombatDecision,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatAssaultResult: CurrentCombatAssaultResult,
            combatPaidWorld: CurrentCombatPaidWorld,
            combatPostResultRetreat: CurrentCombatPostResultRetreat,
            combatSettlement: CurrentCombatSettlement,
            combatSettledWorld: CurrentCombatSettledWorld,
            combatCustodyDisposition: CurrentCombatCustodyDisposition,
            combatCustodyWorld: CurrentCombatCustodyWorld,
            combatCustodyState: CurrentCombatCustodyState,
            combatSuccessor: CurrentCombatSuccessor,
            reserveReleaseEvents: CurrentReserveReleaseEvents,
            cycleControl: CurrentCycleControl,
            repeatMove: movement,
            repeatMoveWorld: world);

    internal ExerciseSession WithRepeatBreakdownEvent(
        CampaignNativeRepeatBreakdownV30 breakdown,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(breakdown), contacts, CurrentCombatDecision,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatAssaultResult: CurrentCombatAssaultResult,
            combatPaidWorld: CurrentCombatPaidWorld,
            combatPostResultRetreat: CurrentCombatPostResultRetreat,
            combatSettlement: CurrentCombatSettlement,
            combatSettledWorld: CurrentCombatSettledWorld,
            combatCustodyDisposition: CurrentCombatCustodyDisposition,
            combatCustodyWorld: CurrentCombatCustodyWorld,
            combatCustodyState: CurrentCombatCustodyState,
            combatSuccessor: CurrentCombatSuccessor,
            reserveReleaseEvents: CurrentReserveReleaseEvents,
            cycleControl: CurrentCycleControl,
            repeatMove: CurrentRepeatMove,
            repeatMoveWorld: CurrentRepeatMoveWorld,
            repeatBreakdown: breakdown);

    internal ExerciseSession WithRepeatCombatSelectionEvent(
        CampaignNativeRepeatCombatDecisionV31 selection,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(selection), contacts, CurrentCombatDecision,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatAssaultResult: CurrentCombatAssaultResult,
            combatPaidWorld: CurrentCombatPaidWorld,
            combatPostResultRetreat: CurrentCombatPostResultRetreat,
            combatSettlement: CurrentCombatSettlement,
            combatSettledWorld: CurrentCombatSettledWorld,
            combatCustodyDisposition: CurrentCombatCustodyDisposition,
            combatCustodyWorld: CurrentCombatCustodyWorld,
            combatCustodyState: CurrentCombatCustodyState,
            combatSuccessor: CurrentCombatSuccessor,
            reserveReleaseEvents: CurrentReserveReleaseEvents,
            cycleControl: CurrentCycleControl,
            repeatMove: CurrentRepeatMove,
            repeatMoveWorld: CurrentRepeatMoveWorld,
            repeatBreakdown: CurrentRepeatBreakdown,
            repeatCombatSelection: selection,
            repeatAssaultBaseContacts: selection.Choice == "select-contact-assault"
                ? contacts : null);

    internal ExerciseSession WithRepeatNoAttackEvent(
        CampaignNativeRepeatCombatNoAttackV32 completion,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(completion), contacts, CurrentCombatDecision,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatAssaultResult: CurrentCombatAssaultResult,
            combatPaidWorld: CurrentCombatPaidWorld,
            combatPostResultRetreat: CurrentCombatPostResultRetreat,
            combatSettlement: CurrentCombatSettlement,
            combatSettledWorld: CurrentCombatSettledWorld,
            combatCustodyDisposition: CurrentCombatCustodyDisposition,
            combatCustodyWorld: CurrentCombatCustodyWorld,
            combatCustodyState: CurrentCombatCustodyState,
            combatSuccessor: CurrentCombatSuccessor,
            reserveReleaseEvents: CurrentReserveReleaseEvents,
            cycleControl: CurrentCycleControl,
            repeatMove: CurrentRepeatMove,
            repeatMoveWorld: CurrentRepeatMoveWorld,
            repeatBreakdown: CurrentRepeatBreakdown,
            repeatCombatSelection: CurrentRepeatCombatSelection,
            repeatNoAttack: completion,
            postRepeatBaseContacts: contacts);

    internal ExerciseSession WithPostRepeatEvent(object campaignEvent,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(campaignEvent), contacts, CurrentCombatDecision,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatAssaultResult: CurrentCombatAssaultResult,
            combatPaidWorld: CurrentCombatPaidWorld,
            combatPostResultRetreat: CurrentCombatPostResultRetreat,
            combatSettlement: CurrentCombatSettlement,
            combatSettledWorld: CurrentCombatSettledWorld,
            combatCustodyDisposition: CurrentCombatCustodyDisposition,
            combatCustodyWorld: CurrentCombatCustodyWorld,
            combatCustodyState: CurrentCombatCustodyState,
            combatSuccessor: CurrentCombatSuccessor,
            reserveReleaseEvents: CurrentReserveReleaseEvents,
            cycleControl: CurrentCycleControl,
            repeatMove: CurrentRepeatMove,
            repeatMoveWorld: CurrentRepeatMoveWorld,
            repeatBreakdown: CurrentRepeatBreakdown,
            repeatCombatSelection: CurrentRepeatCombatSelection,
            repeatNoAttack: CurrentRepeatNoAttack,
            postRepeatBaseContacts: CurrentPostRepeatBaseContacts,
            postRepeatEvents: CurrentPostRepeatEvents.Append(campaignEvent));

    internal ExerciseSession WithRepeatAssaultEvent(object campaignEvent,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(campaignEvent), contacts, CurrentCombatDecision,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatAssaultResult: CurrentCombatAssaultResult,
            combatPaidWorld: CurrentCombatPaidWorld,
            combatPostResultRetreat: CurrentCombatPostResultRetreat,
            combatSettlement: CurrentCombatSettlement,
            combatSettledWorld: CurrentCombatSettledWorld,
            combatCustodyDisposition: CurrentCombatCustodyDisposition,
            combatCustodyWorld: CurrentCombatCustodyWorld,
            combatCustodyState: CurrentCombatCustodyState,
            combatSuccessor: CurrentCombatSuccessor,
            reserveReleaseEvents: CurrentReserveReleaseEvents,
            cycleControl: CurrentCycleControl,
            repeatMove: CurrentRepeatMove,
            repeatMoveWorld: CurrentRepeatMoveWorld,
            repeatBreakdown: CurrentRepeatBreakdown,
            repeatCombatSelection: CurrentRepeatCombatSelection,
            repeatAssaultBaseContacts: CurrentRepeatAssaultBaseContacts,
            repeatAssaultEvents: CurrentRepeatAssaultEvents.Append(campaignEvent));
    internal CampaignSnapshotV11 CurrentSnapshot { get; }
    internal CampaignContentContext Context { get; }
    internal ImmutableList<object> CurrentHistory { get; }
    internal CampaignContactJournal CurrentContacts { get; }
    internal CampaignNativeCombatDecisionStateV12? CurrentCombatDecision { get; }
    internal CampaignNativeCombatNoAttackCompletionV13? CurrentNoAttackCompletion { get; }
    internal CampaignNativeCombatBarrageV14? CurrentCombatBarrage { get; }
    internal CampaignNativeCombatRetreatDeclineV15? CurrentCombatRetreatDecline { get; }
    internal CampaignNativeCombatRetreatMoveV16? CurrentCombatRetreatMove { get; }
    internal CampaignWorldSnapshotV6? CurrentCombatRetreatWorld { get; }
    internal CampaignContactJournal? CurrentCombatRetreatPriorContacts { get; }
    internal IReadOnlyList<CampaignNativeCombatRetreatMoveV16> CurrentCombatRetreatMoves { get; }
    internal IReadOnlyList<CampaignNativeCombatForceSealV17> CurrentCombatForceSeals { get; }
    internal CampaignNativeCombatForceCompletionV18? CurrentCombatForceCompletion { get; }
    internal CampaignNativeCombatForceCancellationV19? CurrentCombatForceCancellation { get; }
    internal CampaignNativeCombatAntiArmorV20? CurrentCombatAntiArmor { get; }
    internal CampaignNativeCombatCancelledAssaultV21? CurrentCombatCancelledAssault { get; }
    internal CampaignNativeCombatAssaultResultV22? CurrentCombatAssaultResult { get; }
    internal CampaignWorldSnapshotV6? CurrentCombatPaidWorld { get; }
    internal CampaignNativeCombatRetreatAfterAssaultV23? CurrentCombatPostResultRetreat { get; }
    internal CampaignNativeCombatSettlementV24? CurrentCombatSettlement { get; }
    internal CampaignWorldSnapshotV6? CurrentCombatSettledWorld { get; }
    internal CampaignNativeCombatCustodyDispositionV25? CurrentCombatCustodyDisposition { get; }
    internal CampaignWorldSnapshotV6? CurrentCombatCustodyWorld { get; }
    internal CampaignNativeCombatCustodyStateV25? CurrentCombatCustodyState { get; }
    internal CampaignNativeCombatSuccessorV26? CurrentCombatSuccessor { get; }
    internal IReadOnlyList<CampaignNativeReserveReleaseEventV27> CurrentReserveReleaseEvents { get; }
    internal CampaignNativeReserveReleaseStateV27? CurrentReserveRelease { get; }
    internal CampaignNativeCycleControlV28? CurrentCycleControl { get; }
    internal CampaignNativeRepeatMovementV29? CurrentRepeatMove { get; }
    internal CampaignWorldSnapshotV6? CurrentRepeatMoveWorld { get; }
    internal CampaignNativeRepeatBreakdownV30? CurrentRepeatBreakdown { get; }
    internal CampaignNativeRepeatCombatDecisionV31? CurrentRepeatCombatSelection { get; }
    internal CampaignNativeRepeatCombatNoAttackV32? CurrentRepeatNoAttack { get; }
    internal CampaignContactJournal? CurrentRepeatAssaultBaseContacts { get; }
    internal IReadOnlyList<object> CurrentRepeatAssaultEvents { get; }
    internal CampaignNativeRepeatAssaultRuntime? CurrentRepeatAssault { get; }
    internal CampaignContactJournal? CurrentPostRepeatBaseContacts { get; }
    internal IReadOnlyList<object> CurrentPostRepeatEvents { get; }
    internal CampaignNativePostRepeatRuntime? CurrentPostRepeat { get; }
    public override string ToString() => nameof(ExerciseSession);
}
