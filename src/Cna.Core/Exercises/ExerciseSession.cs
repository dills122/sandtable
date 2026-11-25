using System.Collections.Immutable;
using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Visibility;

namespace Cna.Core.Exercises;

public sealed class ExerciseSession
{
    private readonly CampaignSnapshotV11? currentSnapshot;
    private readonly CampaignContentContext? context;
    private readonly byte[][]? pregameHistory;
    private readonly CampaignSourceAuthorityState? sourceAuthorityState;

    internal ExerciseSession(CampaignPregameSourceState pregame, CampaignContactJournal? contacts = null,
        IEnumerable<byte[]>? history = null, CampaignSourceAuthorityState? sourceAuthority = null,
        CampaignSourceTurnAuthority? sourceTurn = null)
    {
        Pregame = pregame ?? throw new ArgumentNullException(nameof(pregame));
        if (sourceAuthority is not null && sourceAuthority.Pregame != pregame)
            throw new ArgumentException("Source authority must bind the retained pre-game state.", nameof(sourceAuthority));
        sourceAuthorityState = sourceAuthority;
        if (sourceTurn is not null && !ReferenceEquals(sourceTurn.Source, sourceAuthority))
            throw new ArgumentException("Source turn must retain its source setup authority.", nameof(sourceTurn));
        SourceTurn = sourceTurn;
        pregameHistory = (history ?? [pregame.SerializeGenesis()]).Select(value => value.ToArray()).ToArray();
        CurrentHistory = ImmutableList<object>.Empty;
        CurrentContacts = contacts ?? CampaignContactJournal.InitializePregameSource(pregame.Request.CampaignId);
        if (CurrentContacts.CampaignId != pregame.Request.CampaignId
            || CurrentContacts.AuthorityVersion != (sourceTurn?.StateVersion ?? sourceAuthority?.StateVersion ?? pregame.StateVersion)
            || (sourceTurn is null && CurrentContacts.Records.Count != 0))
            throw new ArgumentException("Pre-game contacts do not match authority.", nameof(contacts));
        CurrentCombatRetreatMoves = [];
        CurrentCombatForceSeals = [];
        CurrentReserveReleaseEvents = [];
        CurrentRepeatAssaultEvents = [];
        CurrentPostRepeatEvents = [];
    }

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
        IEnumerable<object>? postRepeatEvents = null,
        CampaignNativeCombatRbaCancellationV33? combatRbaCancellation = null,
        CampaignCombatAmmunitionLedgerV1? ammunitionLedger = null)
    {
        currentSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        ArgumentNullException.ThrowIfNull(history);
        CurrentHistory = history as ImmutableList<object> ?? ImmutableList.CreateRange(history);
        CurrentContacts = contacts ?? CampaignContactJournal.Empty(snapshot.CampaignId);
        var laterEvents = postRepeatEvents?.ToArray() ?? [];
        var assaultEvents = repeatAssaultEvents?.ToArray() ?? [];
        if (laterEvents.Length + assaultEvents.Length > CurrentHistory.Count
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
        var prePostContacts = repeatNoAttack is not null
            ? postRepeatBaseContacts ?? CurrentContacts
            : repeatAssaultBaseContacts ?? CurrentContacts;
        if (combatSelection is not null && combatSelection.PriorSnapshot != snapshot)
            throw new ArgumentException("Combat selection must bind the session's prior snapshot.", nameof(combatSelection));
        if (noAttackCompletion is not null && (combatSelection is null
            || noAttackCompletion.PriorStateVersion != combatSelection.StateVersion
            || noAttackCompletion.DecisionActionId != combatSelection.Decision.ActionId))
            throw new ArgumentException("No-attack completion must bind the owner decision.", nameof(noAttackCompletion));
        if (combatRbaCancellation is not null && (combatSelection is null || combatBarrage is null
            || combatRetreatDecline is not null || combatForceCancellation is not null
            || combatRbaCancellation.PriorStateVersion != (combatRetreatMove?.StateVersion ?? combatBarrage.StateVersion)
            || combatRbaCancellation.DecisionActionId != combatSelection.Decision.ActionId))
            throw new ArgumentException("RBA cancellation must bind unanswered RBA.", nameof(combatRbaCancellation));
        CurrentCombatRbaCancellation = combatRbaCancellation;
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
            if (combatSelection is null
                || (combatSuccessor is null && noAttackCompletion is null
                    && combatCancelledAssault is null && combatRbaCancellation is null)
                || prePostHistory.Length < reserveEvents.Length + cycleTail.Length
                || !prePostHistory.TakeLast(reserveEvents.Length
                        + cycleTail.Length).Take(reserveEvents.Length)
                    .SequenceEqual(reserveEvents.Cast<object>()))
                throw new ArgumentException("Reserve Release requires accepted Combat completion and events.",
                    nameof(reserveReleaseEvents));
            var initial = noAttackCompletion is not null
                ? CampaignNativeReserveReleaseStateV27.InitializeAfterInitialNoAttack(
                    combatSelection, noAttackCompletion, context)
                : combatRbaCancellation is not null
                    ? CampaignNativeReserveReleaseStateV27.InitializeAfterInitialRbaCancellation(
                        combatSelection, combatRbaCancellation, combatRetreatWorld ?? combatSelection.PriorSnapshot.World, context)
                : combatCancelledAssault is not null
                    ? CampaignNativeReserveReleaseStateV27.InitializeAfterInitialCancellation(
                        combatSelection, combatCancelledAssault,
                        combatRetreatWorld ?? combatSelection.PriorSnapshot.World,
                        context)
                    : CampaignNativeReserveReleaseStateV27.Initialize(
                        combatSuccessor!, CampaignNativeCombatSuccessorV26.Project(
                            combatSelection, combatAssaultResult!, combatSettlement!,
                            combatCustodyDisposition!, combatCustodyWorld!,
                            combatCustodyState, context), context);
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
                repeatAssaultBaseContacts, prePostHistory, assaultEvents,
                // #446: once settled, ammunitionLedger already holds this
                // engagement's spend; replaying its result from that ledger
                // would spend twice. Fold the pre-assault history instead.
                assaultEvents.Any(value => value is CampaignNativeCombatSettlementV24)
                    ? null : ammunitionLedger);
            var assaultTerminalContacts = postRepeatBaseContacts ?? CurrentContacts;
            if (assaultTerminalContacts.AuthorityVersion
                    != runtime.Contacts.AuthorityVersion
                || assaultTerminalContacts.Records.Count != runtime.Contacts.Records.Count
                || assaultTerminalContacts.Records.Zip(runtime.Contacts.Records)
                    .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                throw new ArgumentException(
                    "Repeated assault contacts differ from retained replay.",
                    nameof(contacts));
            firstSideRepeatAssault = runtime;
            CurrentFirstRepeatAssault = runtime;
        }
        CurrentRepeatAssaultBaseContacts = repeatAssaultBaseContacts;
        CurrentRepeatAssaultEvents = Array.AsReadOnly(assaultEvents);
        if (repeatNoAttack is null
            && CurrentRepeatAssault?.Control?.Choice
                != CampaignNativeCycleControlChoiceV28.Repeat
            && (postRepeatBaseContacts is not null || laterEvents.Length != 0))
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
                prePostContacts, CurrentHistory.Take(
                    CurrentHistory.Count - laterEvents.Length).ToArray(), laterEvents);
            if (CurrentContacts.AuthorityVersion
                    != runtime.Contacts.AuthorityVersion
                || CurrentContacts.Records.Count != runtime.Contacts.Records.Count
                || CurrentContacts.Records.Zip(runtime.Contacts.Records)
                    .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                throw new ArgumentException(
                    "Later-cycle contacts differ from retained replay.",
                    nameof(contacts));
            CurrentPostRepeat = runtime;
            firstSideRepeatAssault = runtime.ActiveAssault;
        }
        else if (CurrentRepeatAssault is
        {
            Release: { } laterRelease,
            Control: { Choice: CampaignNativeCycleControlChoiceV28.Repeat } repeatedControl
        }
            && postRepeatBaseContacts is not null)
        {
            var runtime = CampaignNativePostRepeatRuntime.ReplayAfterSecondAssault(
                snapshot, context,
                CurrentRepeatAssault.Result?.AfterRandomState
                    ?? combatAssaultResult!.AfterRandomState,
                laterRelease, repeatedControl, postRepeatBaseContacts,
                CurrentHistory.Take(CurrentHistory.Count - laterEvents.Length).ToArray(),
                laterEvents);
            if (CurrentContacts.AuthorityVersion != runtime.Contacts.AuthorityVersion
                || CurrentContacts.Records.Count != runtime.Contacts.Records.Count
                || CurrentContacts.Records.Zip(runtime.Contacts.Records)
                    .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                throw new ArgumentException(
                    "Later-cycle contacts differ from retained replay.",
                    nameof(contacts));
            CurrentPostRepeat = runtime;
            firstSideRepeatAssault = runtime.ActiveAssault;
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
        CurrentAmmunitionLedger = ammunitionLedger;
    }

    internal ExerciseSession WithReserveReleaseEvent(
        CampaignNativeReserveReleaseEventV27 reserveEvent,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(reserveEvent), contacts, CurrentCombatDecision,
            noAttackCompletion: CurrentNoAttackCompletion,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatForceCancellation: CurrentCombatForceCancellation,
            combatRbaCancellation: CurrentCombatRbaCancellation,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatCancelledAssault: CurrentCombatCancelledAssault,
            combatAssaultResult: CurrentCombatAssaultResult,
            combatPaidWorld: CurrentCombatPaidWorld,
            combatPostResultRetreat: CurrentCombatPostResultRetreat,
            combatSettlement: CurrentCombatSettlement,
            combatSettledWorld: CurrentCombatSettledWorld,
            combatCustodyDisposition: CurrentCombatCustodyDisposition,
            combatCustodyWorld: CurrentCombatCustodyWorld,
            combatCustodyState: CurrentCombatCustodyState,
            combatSuccessor: CurrentCombatSuccessor,
            reserveReleaseEvents: [.. CurrentReserveReleaseEvents, reserveEvent],
            ammunitionLedger: CurrentAmmunitionLedger);

    internal ExerciseSession WithCycleControlEvent(
        CampaignNativeCycleControlV28 control,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(control), contacts, CurrentCombatDecision,
            noAttackCompletion: CurrentNoAttackCompletion,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatForceCancellation: CurrentCombatForceCancellation,
            combatRbaCancellation: CurrentCombatRbaCancellation,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatCancelledAssault: CurrentCombatCancelledAssault,
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
            cycleControl: control,
            ammunitionLedger: CurrentAmmunitionLedger);

    internal ExerciseSession WithRepeatMovementEvent(
        CampaignNativeRepeatMovementV29 movement,
        CampaignWorldSnapshotV6 world,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(movement), contacts, CurrentCombatDecision,
            noAttackCompletion: CurrentNoAttackCompletion,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatForceCancellation: CurrentCombatForceCancellation,
            combatRbaCancellation: CurrentCombatRbaCancellation,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatCancelledAssault: CurrentCombatCancelledAssault,
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
            repeatMoveWorld: world,
            ammunitionLedger: CurrentAmmunitionLedger);

    internal ExerciseSession WithRepeatBreakdownEvent(
        CampaignNativeRepeatBreakdownV30 breakdown,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(breakdown), contacts, CurrentCombatDecision,
            noAttackCompletion: CurrentNoAttackCompletion,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatForceCancellation: CurrentCombatForceCancellation,
            combatRbaCancellation: CurrentCombatRbaCancellation,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatCancelledAssault: CurrentCombatCancelledAssault,
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
            repeatBreakdown: breakdown,
            ammunitionLedger: CurrentAmmunitionLedger);

    internal ExerciseSession WithRepeatCombatSelectionEvent(
        CampaignNativeRepeatCombatDecisionV31 selection,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(selection), contacts, CurrentCombatDecision,
            noAttackCompletion: CurrentNoAttackCompletion,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatForceCancellation: CurrentCombatForceCancellation,
            combatRbaCancellation: CurrentCombatRbaCancellation,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatCancelledAssault: CurrentCombatCancelledAssault,
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
                ? contacts : null,
            ammunitionLedger: CurrentAmmunitionLedger);

    internal ExerciseSession WithRepeatNoAttackEvent(
        CampaignNativeRepeatCombatNoAttackV32 completion,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(completion), contacts, CurrentCombatDecision,
            noAttackCompletion: CurrentNoAttackCompletion,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatForceCancellation: CurrentCombatForceCancellation,
            combatRbaCancellation: CurrentCombatRbaCancellation,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatCancelledAssault: CurrentCombatCancelledAssault,
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
            postRepeatBaseContacts: contacts,
            ammunitionLedger: CurrentAmmunitionLedger);

    // Perf seeding path (Phase 1 of #436): deliberately does NOT forward
    // ammunitionLedger here. A second or later repeat cycle's own
    // ammunition folding is done by CampaignNativePostRepeatRuntime's
    // CampaignNativeRepeatAssaultRuntime.Replay calls, which this method
    // does not participate in and which are not seeded by this change
    // (out of this PR's scope). Leaving CurrentAmmunitionLedger null from
    // this point on means any later reader (including the next ordinary
    // CampaignExercises.Complete call) safely falls back to the
    // from-genesis fold instead of carrying forward a value that may be
    // stale by however many later-cycle Combat results this round still
    // has ahead of it.
    internal ExerciseSession WithPostRepeatEvent(object campaignEvent,
        CampaignContactJournal contacts) => new(CurrentSnapshot, Context,
            CurrentHistory.Add(campaignEvent), contacts, CurrentCombatDecision,
            noAttackCompletion: CurrentNoAttackCompletion,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatForceCancellation: CurrentCombatForceCancellation,
            combatRbaCancellation: CurrentCombatRbaCancellation,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatCancelledAssault: CurrentCombatCancelledAssault,
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
            repeatAssaultBaseContacts: CurrentRepeatAssaultBaseContacts,
            repeatAssaultEvents: CurrentRepeatAssaultEvents,
            postRepeatBaseContacts: CurrentPostRepeatBaseContacts ?? CurrentContacts,
            postRepeatEvents: CurrentPostRepeatEvents.Append(campaignEvent));

    internal ExerciseSession WithRepeatAssaultEvent(object campaignEvent,
        CampaignContactJournal contacts)
    {
        // MTS-TASK-006 (2026-11-23): this stage's own SECOND acting side's
        // assault runtime is chronologically latest, so it is checked
        // first -- same ordering discipline as every branch in this
        // dispatch.
        if (CurrentThirdStageCombatDecision is not null)
            throw new InvalidOperationException("Stage 3 Assault events are not wired until S3-5.");
        if (CurrentStageEntrySecondAssault is not null)
            return WithStageEntrySecondAssaultEvent(campaignEvent, contacts);
        if (CurrentStageEntryAssault is not null)
            return WithStageEntryAssaultEvent(campaignEvent, contacts);
        if (CurrentSecondAssault is not null)
            return WithSecondAssaultEvent(campaignEvent, contacts);
        if (CurrentPostRepeat?.ActiveAssault is not null)
            return WithPostRepeatEvent(campaignEvent, contacts);
        // Perf seeding path (Phase 1 of #436): CurrentAmmunitionLedger is
        // carried forward unchanged (still the pre-spend value used to
        // build this engagement's CampaignNativeCombatAssaultResultV22)
        // through every repeat-assault step except the World settlement
        // that follows an accepted result -- the point this engagement's
        // ammunition spend becomes final. Folding exactly that one
        // CurrentRepeatAssault.Result event here, instead of only at the
        // next read, keeps the invariant that a non-null
        // CurrentAmmunitionLedger always reflects CurrentHistory as of
        // this session, so a second repeat-assault within the same Combat
        // round (ResolveRepeatedCombatAssault) can still seed from it
        // without rescanning the full history. The fold still goes
        // through CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents, so
        // the result's ContentHash/SourceHash/PriorAmmunitionHash/
        // AfterAmmunitionHash are re-validated, not trusted blindly.
        // #446: with no carried seed (after a later repeat cycle), fold from
        // genesis; CurrentHistory already holds this engagement's result.
        var ammunitionLedger = campaignEvent is CampaignNativeCombatSettlementV24
            ? CurrentAmmunitionLedger is { } seed
                ? CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(
                    seed, new object[] { CurrentRepeatAssault!.Result! })
                : CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(Context, CurrentHistory)
            : CurrentAmmunitionLedger;
        return new(CurrentSnapshot, Context,
            CurrentHistory.Add(campaignEvent), contacts, CurrentCombatDecision,
            noAttackCompletion: CurrentNoAttackCompletion,
            combatBarrage: CurrentCombatBarrage,
            combatRetreatDecline: CurrentCombatRetreatDecline,
            combatRetreatMove: CurrentCombatRetreatMove,
            combatRetreatWorld: CurrentCombatRetreatWorld,
            combatRetreatPriorContacts: CurrentCombatRetreatPriorContacts,
            combatRetreatMoves: CurrentCombatRetreatMoves,
            combatForceSeals: CurrentCombatForceSeals,
            combatForceCompletion: CurrentCombatForceCompletion,
            combatForceCancellation: CurrentCombatForceCancellation,
            combatRbaCancellation: CurrentCombatRbaCancellation,
            combatAntiArmor: CurrentCombatAntiArmor,
            combatCancelledAssault: CurrentCombatCancelledAssault,
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
            repeatAssaultEvents: CurrentRepeatAssaultEvents.Append(campaignEvent),
            ammunitionLedger: ammunitionLedger);
    }
    internal CampaignPregameSourceState? Pregame { get; }
    internal CampaignSourceAuthorityState? SourceAuthority => SourceTurn is null ? sourceAuthorityState : null;
    internal CampaignSourceTurnAuthority? SourceTurn { get; }
    internal IReadOnlyList<byte[]> PregameHistory => Array.AsReadOnly(
        (pregameHistory ?? throw new InvalidOperationException("Session has no pre-game history."))
            .Select(value => value.ToArray()).ToArray());
    internal CampaignSnapshotV11 CurrentSnapshot => currentSnapshot
        ?? throw new InvalidOperationException("Pre-game setup does not have Snapshot 11 authority.");
    internal CampaignContentContext Context => context
        ?? throw new InvalidOperationException("Pre-game setup does not have Content 6 authority.");
    internal ImmutableList<object> CurrentHistory { get; private set; }
    internal CampaignContactJournal CurrentContacts { get; private set; }
    internal CampaignNativeTruckConvoyPassV41? CurrentTruckConvoyPass { get; private set; }
    internal ExerciseSession? TruckConvoyPassPredecessor { get; private set; }
    internal CampaignNativePostCombatPassV42? CurrentPostCombatPass { get; private set; }
    internal ExerciseSession? PostCombatPassPredecessor { get; private set; }
    internal CampaignNativeSecondReserveV43? CurrentSecondReserve { get; private set; }
    internal CampaignWorldSnapshotV6? CurrentSecondReserveWorld { get; private set; }
    internal ExerciseSession? SecondReservePredecessor { get; private set; }
    internal CampaignNativeSecondMoveV44? CurrentSecondMove { get; private set; }
    internal CampaignWorldSnapshotV6? CurrentSecondMoveWorld { get; private set; }
    internal ExerciseSession? SecondMovePredecessor { get; private set; }
    internal CampaignNativeSecondMovementCompletedV45? CurrentSecondMovementCompletion { get; private set; }
    internal ExerciseSession? SecondMovementCompletionPredecessor { get; private set; }
    internal CampaignNativeSecondBreakdownV46? CurrentSecondBreakdown { get; private set; }
    internal ExerciseSession? SecondBreakdownPredecessor { get; private set; }
    // #394 gap 1 (Option A for gap 3): side-scoped siblings of
    // CurrentCombatDecision/CurrentNoAttackCompletion for the second-acting
    // side, so the ~70 existing first-side call sites and the big first-side
    // ExerciseSession constructor are untouched. CurrentSecondCombatDecision
    // uses a dedicated type (CampaignNativeSecondCombatDecisionStateV47, not
    // CampaignNativeCombatDecisionStateV12) because it is keyed to a
    // (World, Position) pair instead of a CampaignSnapshotV11 -- see that
    // type's remarks. CurrentSecondNoAttackCompletion stays the exact same
    // first-side type: CampaignNativeCombatNoAttackCompletionV13 and its
    // factory are already generic over "which decision" via
    // CampaignCombatSelectionFrame's implicit operator (which both decision
    // types now support), so no second type is needed there.
    internal CampaignNativeSecondCombatDecisionStateV47? CurrentSecondCombatDecision { get; private set; }
    internal CampaignNativeCombatNoAttackCompletionV13? CurrentSecondNoAttackCompletion { get; private set; }

    internal ExerciseSession WithSecondBreakdown(CampaignNativeSecondBreakdownV46 completed,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentSecondBreakdown is not null || CurrentSecondMovementCompletion is null
            || completed.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != completed.StateVersion)
            throw new InvalidOperationException("Second Breakdown must advance its retained cut once.");
        // #394 gap 3: CurrentSnapshot is intentionally left untouched here --
        // a direct CampaignSnapshotV11 pointing at the second side's
        // post-breakdown Combat/PositionDetermination position is rejected
        // by Cna1979LandSequenceV4.IsSupportedCheckpoint (confirmed by
        // testing), so no such snapshot is ever constructed. Gap 1/2/4 below
        // instead carry the second side's own (World, Position) pair
        // directly -- see CampaignNativeSecondCombatDecisionV47's remarks.
        var successor = (ExerciseSession)MemberwiseClone();
        successor.SecondBreakdownPredecessor = this;
        successor.CurrentSecondBreakdown = completed;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(completed);
        return successor;
    }

    // #394 gap 1 / extra-2: parallel to the first-side ExerciseSession
    // constructor's handling of a fresh CampaignNativeCombatDecisionStateV12,
    // but implemented as a clone (like every other V41-V46 With* method) so
    // the second side's accumulated history (CurrentSecondBreakdown and its
    // predecessors) survives instead of being dropped, and keyed to the new
    // CampaignNativeSecondCombatDecisionStateV47 type instead of a
    // CampaignSnapshotV11-bound one.
    internal ExerciseSession? SecondCombatSelectionPredecessor { get; private set; }

    internal ExerciseSession WithSecondCombatSelection(CampaignNativeSecondCombatDecisionStateV47 selected,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentSecondBreakdown is null || CurrentSecondCombatDecision is not null
            || selected.Decision.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != selected.StateVersion)
            throw new InvalidOperationException("Second Combat selection must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.SecondCombatSelectionPredecessor = this;
        successor.CurrentSecondCombatDecision = selected;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(selected.Decision);
        successor.CurrentSecondAssault = CampaignNativeRepeatAssaultRuntime.ReplaySecond(
            SecondAssaultFrame(selected, CurrentSnapshot, CurrentHistory), Context, contacts,
            successor.CurrentHistory, []);
        return successor;
    }

    // The second side's V14-V26 assault stages after its V47 selection,
    // re-verified from that cut on every accepted step.
    internal CampaignNativeRepeatAssaultRuntime? CurrentSecondAssault { get; private set; }
    internal ExerciseSession? SecondAssaultPredecessor { get; private set; }

    // The latest accepted draw in chronological history owns the next cursor.
    // The retained V11 state already includes the legacy opening preamble.
    internal static RandomStreamState LatestCursor(RandomStreamState initial, IEnumerable<object> history) =>
        history.LastOrDefault(value => value is CampaignNativeCombatAssaultResultV22 or CampaignNativeWeatherDeterminationV50) switch
        {
            CampaignNativeCombatAssaultResultV22 assault => assault.AfterRandomState,
            CampaignNativeWeatherDeterminationV50 weather => new RandomStreamState(initial.ContractVersion,
                initial.AlgorithmId, initial.Seed, weather.ResultRandomCursor),
            _ => initial,
        };

    internal static CampaignCombatSelectionFrame SecondAssaultFrame(
        CampaignNativeSecondCombatDecisionStateV47 selected, CampaignSnapshotV11 turnFacts,
        IEnumerable<object> history)
    {
        CampaignCombatSelectionFrame frame = selected;
        return frame with { RandomState = LatestCursor(turnFacts.RandomState, history) };
    }

    internal ExerciseSession WithSecondAssaultEvent(object campaignEvent,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(campaignEvent);
        ArgumentNullException.ThrowIfNull(contacts);
        var runtime = CurrentSecondAssault!.Advance(campaignEvent);
        if (runtime.Contacts.AuthorityVersion != contacts.AuthorityVersion
            || runtime.Contacts.Records.Count != contacts.Records.Count
            || runtime.Contacts.Records.Zip(contacts.Records)
                .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
            throw new InvalidOperationException(
                "Second-side assault step differs from its retained replay.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.SecondAssaultPredecessor = this;
        successor.CurrentSecondAssault = runtime;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(campaignEvent);
        // As for later cycles: the settlement makes this engagement's spend
        // final, so the carried ledger folds exactly its result.
        if (campaignEvent is CampaignNativeCombatSettlementV24)
            successor.CurrentAmmunitionLedger = CurrentAmmunitionLedger is { } seed
                ? CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(seed, new object[] { runtime.Result! })
                : CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(Context, CurrentHistory);
        return successor;
    }

    internal ExerciseSession? SecondNoAttackPredecessor { get; private set; }

    // #394 extra-2: second-side analog of the no-attack completion the full
    // constructor records for the first side. CampaignNativeCombatNoAttackCompletionV13
    // and its factory are already generic over "which decision" via
    // CampaignCombatSelectionFrame's implicit operator, so no new completion
    // type is needed here, only this clone to preserve second-side history.
    internal ExerciseSession WithSecondNoAttack(CampaignNativeCombatNoAttackCompletionV13 completion,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentSecondCombatDecision is not { } selected || CurrentSecondNoAttackCompletion is not null
            || completion.PriorStateVersion != selected.StateVersion
            || contacts.AuthorityVersion != completion.StateVersion)
            throw new InvalidOperationException("Second no-attack completion must advance its retained cut once.");
        var runtime = (CurrentSecondAssault
            ?? throw new InvalidOperationException("Second no-attack completion lacks its stage chain."))
            .Advance(completion);
        if (runtime.Contacts.AuthorityVersion != contacts.AuthorityVersion)
            throw new InvalidOperationException("Second no-attack completion differs from its retained replay.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.SecondNoAttackPredecessor = this;
        successor.SecondAssaultPredecessor = this;
        successor.CurrentSecondAssault = runtime;
        successor.CurrentSecondNoAttackCompletion = completion;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(completion);
        return successor;
    }

    internal ExerciseSession WithSecondMovementCompletion(CampaignNativeSecondMovementCompletedV45 completed,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentNullException.ThrowIfNull(contacts);
        if (completed.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != completed.StateVersion)
            throw new InvalidOperationException("Second Movement completion must advance its retained cut.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.SecondMovementCompletionPredecessor = this;
        successor.CurrentSecondMovementCompletion = completed;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(completed);
        return successor;
    }

    internal ExerciseSession WithSecondMove(CampaignNativeSecondMoveV44 movement,
        CampaignWorldSnapshotV6 world, CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(contacts);
        if (movement.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != movement.StateVersion
            || movement.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(world))
            throw new InvalidOperationException("Second Movement must advance its retained World and contact cut.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.SecondMovePredecessor = this;
        successor.CurrentSecondMove = movement;
        successor.CurrentSecondMoveWorld = world;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(movement);
        return successor;
    }

    internal ExerciseSession WithSecondReserve(CampaignNativeSecondReserveV43 reserve,
        CampaignWorldSnapshotV6 world, CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(reserve);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(contacts);
        if (reserve.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != reserve.StateVersion
            || reserve.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(world))
            throw new InvalidOperationException("Second Reserve must advance its retained World and contact cut.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.SecondReservePredecessor = this;
        successor.CurrentSecondReserve = reserve;
        successor.CurrentSecondReserveWorld = world;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(reserve);
        return successor;
    }

    internal ExerciseSession WithPostCombatPass(CampaignNativePostCombatPassV42 passed,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(passed);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentTruckConvoyPass is null || passed.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != passed.StateVersion)
            throw new InvalidOperationException("Post-combat pass must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.PostCombatPassPredecessor = this;
        successor.CurrentPostCombatPass = passed;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(passed);
        return successor;
    }

    internal ExerciseSession WithTruckConvoyPass(CampaignNativeTruckConvoyPassV41 passed,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(passed);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentTruckConvoyPass is not null || passed.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != passed.StateVersion)
            throw new InvalidOperationException("Truck Convoy pass must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.TruckConvoyPassPredecessor = this;
        successor.CurrentTruckConvoyPass = passed;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(passed);
        return successor;
    }
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
    internal CampaignNativeCombatRbaCancellationV33? CurrentCombatRbaCancellation { get; }
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
    // Perf seeding path (Phase 1 of #436): a cache of
    // CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(Context,
    // CurrentHistory) as of the last point it was known current -- either
    // this session's own clean ordinary-step boundary (populated by
    // CampaignExercises.Complete) or carried forward through a single
    // Combat round's steps (see the hot call sites in CampaignExercises.cs
    // and CampaignNativeCombatForceEligibility/CampaignNativeRepeatAssaultRuntime).
    // This is the one place in Cna.Core that deliberately duplicates a
    // small piece of derived state outside the event log -- a pattern this
    // type's own header comment says the engine avoids everywhere else.
    // It is safe specifically because it is never trusted blindly: every
    // fold through CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents
    // re-validates the tail events against the running
    // ContentHash/SourceHash/PriorAmmunitionHash/AfterAmmunitionHash hash
    // chain, so a stale-but-wrong seed cannot silently corrupt a result --
    // it would throw InvalidCampaignHistoryException. A path that cannot
    // prove the seed is still current for this exact session (e.g. once a
    // second or later repeat cycle is entered via CurrentPostRepeat, see
    // WithPostRepeatEvent) leaves this null rather than forward a stale
    // value, so a reader always either gets a current ledger or a null
    // that forces the safe from-genesis fallback -- never a wrong answer.
    internal CampaignCombatAmmunitionLedgerV1? CurrentAmmunitionLedger { get; private set; }
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
    private readonly CampaignNativeRepeatAssaultRuntime? firstSideRepeatAssault;
    // The active V14-V26 assault runtime: this stage's own second-side
    // combat decision if that's what's active (checked first, chronologically
    // latest, same ordering discipline as every other cascade in this file),
    // otherwise Stage 1's #394 second acting side's, otherwise the first
    // side's later-cycle one.
    internal CampaignNativeRepeatAssaultRuntime? CurrentRepeatAssault =>
        // Stage 3's own Assault runtime arrives with S3-5; once its Combat Decision exists the
        // never-reset Stage 2 runtimes below are stale, so none is active.
        CurrentThirdStageCombatDecision is not null ? null :
        (CurrentStageEntrySecondAssault is { IsSecondSideFinish: false } ? CurrentStageEntrySecondAssault : null)
            ?? (CurrentStageEntryAssault is { IsSecondSideFinish: false } ? CurrentStageEntryAssault : null)
            ?? (CurrentSecondAssault is { IsSecondSideFinish: false } ? CurrentSecondAssault : null)
            ?? firstSideRepeatAssault;
    internal CampaignNativeRepeatAssaultRuntime? CurrentFirstRepeatAssault { get; }
    internal CampaignContactJournal? CurrentPostRepeatBaseContacts { get; }
    internal IReadOnlyList<object> CurrentPostRepeatEvents { get; }
    internal CampaignNativePostRepeatRuntime? CurrentPostRepeat { get; }

    // MTS-TASK-002 (2026-11-22, docs/work/plans/2026-11-22-stage-entry-bridge-design-options.md):
    // the six native-chain preamble events, same CurrentXxx/XxxPredecessor/
    // WithXxx shape as every other native-chain step above (e.g.
    // WithTruckConvoyPass/WithPostCombatPass). Each guards both idempotency
    // (CurrentXxx must still be null) and chain order (the immediate
    // predecessor's own CurrentXxx must already be set) -- the native
    // event's own Create method is what actually validates the predecessor
    // object and catalog position; these guards are defense in depth, the
    // same depth WithPostCombatPass already applies.
    internal CampaignNativeInitiativeDeclarationV49? CurrentInitiativeDeclaration { get; private set; }
    internal ExerciseSession? InitiativeDeclarationPredecessor { get; private set; }

    internal ExerciseSession WithInitiativeDeclaration(CampaignNativeInitiativeDeclarationV49 declared,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(declared);
        ArgumentNullException.ThrowIfNull(contacts);
        // Correction (MTS-TASK-005, 2026-11-22): this guard originally read
        // `CurrentPostCombatPass is null`, i.e. the FIRST acting side's own
        // Patrol pass -- which is already non-null long before the SECOND
        // side's own cycle (Reserve through its own Truck Convoy/Rail/Repair/
        // Patrol, modeled entirely inside CurrentSecondAssault per
        // CampaignNativeRepeatAssaultRuntime.ApplySecondSidePhase) has even
        // started, let alone finished. That let Initiative Declaration become
        // constructible before both sides had actually completed the prior
        // stage. The real terminal fact is the second side's own last Post
        // Combat Pass, which CampaignNativeRepeatAssaultRuntime retains as
        // the last entry of its own PostCombatPasses list (Rail, Repair,
        // Patrol in order) once IsSecondSideFinish's Truck Convoy/Patrol
        // sequence has run -- there is no dedicated ExerciseSession field for
        // it because, unlike every earlier step, this one is produced inside
        // that runtime rather than via its own WithXxx method.
        if (CurrentSecondAssault is not { PostCombatPasses.Count: > 0 } || CurrentInitiativeDeclaration is not null
            || declared.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != declared.StateVersion)
            throw new InvalidOperationException("Initiative Declaration must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.InitiativeDeclarationPredecessor = this;
        successor.CurrentInitiativeDeclaration = declared;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(declared);
        return successor;
    }

    internal CampaignNativeWeatherDeterminationV50? CurrentStageEntryWeather { get; private set; }
    internal ExerciseSession? StageEntryWeatherPredecessor { get; private set; }

    internal ExerciseSession WithStageEntryWeather(CampaignNativeWeatherDeterminationV50 determined,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(determined);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentInitiativeDeclaration is null || CurrentStageEntryWeather is not null
            || determined.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != determined.StateVersion)
            throw new InvalidOperationException("Weather Determination must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntryWeatherPredecessor = this;
        successor.CurrentStageEntryWeather = determined;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(determined);
        return successor;
    }

    internal CampaignNativeNoObligationOrganizationV51? CurrentNoObligationOrganization { get; private set; }
    internal ExerciseSession? NoObligationOrganizationPredecessor { get; private set; }

    internal ExerciseSession WithNoObligationOrganization(CampaignNativeNoObligationOrganizationV51 resolved,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentStageEntryWeather is null || CurrentNoObligationOrganization is not null
            || resolved.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != resolved.StateVersion)
            throw new InvalidOperationException("Organization resolution must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.NoObligationOrganizationPredecessor = this;
        successor.CurrentNoObligationOrganization = resolved;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(resolved);
        return successor;
    }

    internal CampaignNativeNoObligationNavalConvoyArrivalV52? CurrentNoObligationNavalConvoyArrival { get; private set; }
    internal ExerciseSession? NoObligationNavalConvoyArrivalPredecessor { get; private set; }

    internal ExerciseSession WithNoObligationNavalConvoyArrival(
        CampaignNativeNoObligationNavalConvoyArrivalV52 resolved, CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentNoObligationOrganization is null || CurrentNoObligationNavalConvoyArrival is not null
            || resolved.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != resolved.StateVersion)
            throw new InvalidOperationException("Naval Convoy Arrival resolution must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.NoObligationNavalConvoyArrivalPredecessor = this;
        successor.CurrentNoObligationNavalConvoyArrival = resolved;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(resolved);
        return successor;
    }

    internal CampaignNativeNoObligationFleetAssignmentV53? CurrentNoObligationFleetAssignment { get; private set; }
    internal ExerciseSession? NoObligationFleetAssignmentPredecessor { get; private set; }

    internal ExerciseSession WithNoObligationFleetAssignment(CampaignNativeNoObligationFleetAssignmentV53 resolved,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentNoObligationNavalConvoyArrival is null || CurrentNoObligationFleetAssignment is not null
            || resolved.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != resolved.StateVersion)
            throw new InvalidOperationException("Fleet Assignment resolution must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.NoObligationFleetAssignmentPredecessor = this;
        successor.CurrentNoObligationFleetAssignment = resolved;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(resolved);
        return successor;
    }

    internal CampaignNativeNoObligationFleetRepairV54? CurrentNoObligationFleetRepair { get; private set; }
    internal ExerciseSession? NoObligationFleetRepairPredecessor { get; private set; }

    internal ExerciseSession WithNoObligationFleetRepair(CampaignNativeNoObligationFleetRepairV54 resolved,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentNoObligationFleetAssignment is null || CurrentNoObligationFleetRepair is not null
            || resolved.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != resolved.StateVersion)
            throw new InvalidOperationException("Fleet Repair resolution must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.NoObligationFleetRepairPredecessor = this;
        successor.CurrentNoObligationFleetRepair = resolved;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(resolved);
        return successor;
    }

    // Stage 2->3 bridge: retain a separate occurrence of the same native
    // preamble chain, starting from Stage 2's own second-side terminal pass.
    internal CampaignNativeInitiativeDeclarationV49? CurrentThirdStageInitiativeDeclaration { get; private set; }
    internal ExerciseSession? ThirdStageInitiativeDeclarationPredecessor { get; private set; }

    internal ExerciseSession WithThirdStageInitiativeDeclaration(CampaignNativeInitiativeDeclarationV49 declared,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(declared);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentStageEntrySecondAssault is not { PostCombatPasses.Count: > 0 } || CurrentThirdStageInitiativeDeclaration is not null
            || declared.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != declared.StateVersion)
            throw new InvalidOperationException("Initiative Declaration must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.ThirdStageInitiativeDeclarationPredecessor = this;
        successor.CurrentThirdStageInitiativeDeclaration = declared;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(declared);
        return successor;
    }

    internal CampaignNativeWeatherDeterminationV50? CurrentThirdStageWeather { get; private set; }
    internal ExerciseSession? ThirdStageWeatherPredecessor { get; private set; }

    internal ExerciseSession WithThirdStageWeather(CampaignNativeWeatherDeterminationV50 determined,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(determined);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentThirdStageInitiativeDeclaration is null || CurrentThirdStageWeather is not null
            || determined.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != determined.StateVersion)
            throw new InvalidOperationException("Weather Determination must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.ThirdStageWeatherPredecessor = this;
        successor.CurrentThirdStageWeather = determined;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(determined);
        return successor;
    }

    internal CampaignNativeNoObligationOrganizationV51? CurrentThirdStageNoObligationOrganization { get; private set; }
    internal ExerciseSession? ThirdStageNoObligationOrganizationPredecessor { get; private set; }

    internal ExerciseSession WithThirdStageNoObligationOrganization(CampaignNativeNoObligationOrganizationV51 resolved,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentThirdStageWeather is null || CurrentThirdStageNoObligationOrganization is not null
            || resolved.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != resolved.StateVersion)
            throw new InvalidOperationException("Organization resolution must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.ThirdStageNoObligationOrganizationPredecessor = this;
        successor.CurrentThirdStageNoObligationOrganization = resolved;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(resolved);
        return successor;
    }

    internal CampaignNativeNoObligationNavalConvoyArrivalV52? CurrentThirdStageNoObligationNavalConvoyArrival { get; private set; }
    internal ExerciseSession? ThirdStageNoObligationNavalConvoyArrivalPredecessor { get; private set; }

    internal ExerciseSession WithThirdStageNoObligationNavalConvoyArrival(
        CampaignNativeNoObligationNavalConvoyArrivalV52 resolved, CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentThirdStageNoObligationOrganization is null || CurrentThirdStageNoObligationNavalConvoyArrival is not null
            || resolved.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != resolved.StateVersion)
            throw new InvalidOperationException("Naval Convoy Arrival resolution must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.ThirdStageNoObligationNavalConvoyArrivalPredecessor = this;
        successor.CurrentThirdStageNoObligationNavalConvoyArrival = resolved;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(resolved);
        return successor;
    }

    internal CampaignNativeNoObligationFleetAssignmentV53? CurrentThirdStageNoObligationFleetAssignment { get; private set; }
    internal ExerciseSession? ThirdStageNoObligationFleetAssignmentPredecessor { get; private set; }

    internal ExerciseSession WithThirdStageNoObligationFleetAssignment(CampaignNativeNoObligationFleetAssignmentV53 resolved,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentThirdStageNoObligationNavalConvoyArrival is null || CurrentThirdStageNoObligationFleetAssignment is not null
            || resolved.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != resolved.StateVersion)
            throw new InvalidOperationException("Fleet Assignment resolution must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.ThirdStageNoObligationFleetAssignmentPredecessor = this;
        successor.CurrentThirdStageNoObligationFleetAssignment = resolved;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(resolved);
        return successor;
    }

    internal CampaignNativeNoObligationFleetRepairV54? CurrentThirdStageNoObligationFleetRepair { get; private set; }
    internal ExerciseSession? ThirdStageNoObligationFleetRepairPredecessor { get; private set; }

    internal ExerciseSession WithThirdStageNoObligationFleetRepair(CampaignNativeNoObligationFleetRepairV54 resolved,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentThirdStageNoObligationFleetAssignment is null || CurrentThirdStageNoObligationFleetRepair is not null
            || resolved.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != resolved.StateVersion)
            throw new InvalidOperationException("Fleet Repair resolution must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.ThirdStageNoObligationFleetRepairPredecessor = this;
        successor.CurrentThirdStageNoObligationFleetRepair = resolved;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(resolved);
        return successor;
    }

    // S3-1..S3-3 (2026-11-24): Operation Stage 3's own first-acting-side
    // Reserve Designation, Movement + Movement Completion and Breakdown,
    // entered from the Stage 2->3 preamble chain's terminal event
    // (CurrentThirdStageNoObligationFleetRepair). Same native event types as
    // Stage 2's first side (V43/V44/V45/V46, role-as-data generic); a distinct
    // slot set is required because every CurrentStageEntry* field is already
    // permanently bound to Stage 2's first acting side and no field on this
    // class is ever reset once set. Combat (S3-4+) is NOT wired: while
    // CurrentThirdStageBreakdown is the head, every Combat entry point must
    // fail closed (see CampaignExercises.IsStageThreeCombatUnwired).
    internal CampaignNativeSecondReserveV43? CurrentThirdStageReserve { get; private set; }
    internal CampaignWorldSnapshotV6? CurrentThirdStageReserveWorld { get; private set; }
    internal ExerciseSession? ThirdStageReservePredecessor { get; private set; }

    internal ExerciseSession WithThirdStageReserve(CampaignNativeSecondReserveV43 reserve,
        CampaignWorldSnapshotV6 world, CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(reserve);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentThirdStageNoObligationFleetRepair is null || CurrentThirdStageReserve is not null
            || reserve.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != reserve.StateVersion
            || reserve.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(world))
            throw new InvalidOperationException("Stage 3 Reserve must advance its retained World and contact cut.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.ThirdStageReservePredecessor = this;
        successor.CurrentThirdStageReserve = reserve;
        successor.CurrentThirdStageReserveWorld = world;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(reserve);
        return successor;
    }

    internal CampaignNativeSecondMoveV44? CurrentThirdStageMove { get; private set; }
    internal CampaignWorldSnapshotV6? CurrentThirdStageMoveWorld { get; private set; }
    internal ExerciseSession? ThirdStageMovePredecessor { get; private set; }
    internal CampaignNativeSecondMovementCompletedV45? CurrentThirdStageMovementCompletion { get; private set; }
    internal ExerciseSession? ThirdStageMovementCompletionPredecessor { get; private set; }

    internal ExerciseSession WithThirdStageMovementCompletion(CampaignNativeSecondMovementCompletedV45 completed,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentThirdStageReserve is not { Completed: true } || CurrentThirdStageMovementCompletion is not null
            || completed.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != completed.StateVersion)
            throw new InvalidOperationException("Stage 3 Movement completion must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.ThirdStageMovementCompletionPredecessor = this;
        successor.CurrentThirdStageMovementCompletion = completed;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(completed);
        return successor;
    }

    internal ExerciseSession WithThirdStageMove(CampaignNativeSecondMoveV44 movement,
        CampaignWorldSnapshotV6 world, CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentThirdStageReserve is not { Completed: true } || CurrentThirdStageMovementCompletion is not null
            || movement.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != movement.StateVersion
            || movement.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(world))
            throw new InvalidOperationException("Stage 3 Movement must advance its retained World and contact cut.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.ThirdStageMovePredecessor = this;
        successor.CurrentThirdStageMove = movement;
        successor.CurrentThirdStageMoveWorld = world;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(movement);
        return successor;
    }

    internal CampaignNativeSecondBreakdownV46? CurrentThirdStageBreakdown { get; private set; }
    internal ExerciseSession? ThirdStageBreakdownPredecessor { get; private set; }

    internal ExerciseSession WithThirdStageBreakdown(CampaignNativeSecondBreakdownV46 completed,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentThirdStageBreakdown is not null || CurrentThirdStageMovementCompletion is null
            || completed.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != completed.StateVersion)
            throw new InvalidOperationException("Stage 3 Breakdown must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.ThirdStageBreakdownPredecessor = this;
        successor.CurrentThirdStageBreakdown = completed;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(completed);
        return successor;
    }

    // S3-4 (2026-11-24): Operation Stage 3's own first-acting-side Combat
    // Decision (V47 Select, role-as-data generic), entered from this stage's
    // own completed Breakdown. Deliberately does NOT create an assault runtime:
    // the Stage 3 Assault/No-Attack chain is S3-5. Until then
    // CurrentRepeatAssault resolves to null and WithRepeatAssaultEvent rejects
    // once this decision exists, so the never-reset Stage 2 runtimes cannot
    // answer for Stage 3.
    internal CampaignNativeSecondCombatDecisionStateV47? CurrentThirdStageCombatDecision { get; private set; }
    internal ExerciseSession? ThirdStageCombatSelectionPredecessor { get; private set; }

    internal ExerciseSession WithThirdStageCombatSelection(CampaignNativeSecondCombatDecisionStateV47 selected,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentThirdStageBreakdown is null || CurrentThirdStageCombatDecision is not null
            || selected.Decision.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != selected.StateVersion)
            throw new InvalidOperationException("Stage 3 Combat selection must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.ThirdStageCombatSelectionPredecessor = this;
        successor.CurrentThirdStageCombatDecision = selected;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(selected.Decision);
        return successor;
    }

    // MTS-TASK-005 (2026-11-22): the new stage's own first-acting-side Reserve
    // Designation, entered from the six-event preamble chain's terminal
    // (CurrentNoObligationFleetRepair). Reuses CampaignNativeSecondReserveV43
    // unchanged -- the type is role-as-data generic as of #518's refactor, so
    // this is a new occurrence of the same event type, not a new one. A
    // distinct ExerciseSession slot is required because CurrentSecondReserve
    // is already permanently bound to Stage 1's second acting side by the
    // time this stage's preamble can even complete, and no field in this
    // class is ever reset once set.
    internal CampaignNativeSecondReserveV43? CurrentStageEntryReserve { get; private set; }
    internal CampaignWorldSnapshotV6? CurrentStageEntryReserveWorld { get; private set; }
    internal ExerciseSession? StageEntryReservePredecessor { get; private set; }

    internal ExerciseSession WithStageEntryReserve(CampaignNativeSecondReserveV43 reserve,
        CampaignWorldSnapshotV6 world, CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(reserve);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentNoObligationFleetRepair is null || CurrentStageEntryReserve is not null
            || reserve.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != reserve.StateVersion
            || reserve.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(world))
            throw new InvalidOperationException("Stage Entry Reserve must advance its retained World and contact cut.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntryReservePredecessor = this;
        successor.CurrentStageEntryReserve = reserve;
        successor.CurrentStageEntryReserveWorld = world;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(reserve);
        return successor;
    }

    // MTS-TASK-005/006 (2026-11-22): the new stage's own first-acting-side
    // Movement + Movement Completion, entered from this stage's own
    // completed Reserve Designation (CurrentStageEntryReserve) instead of
    // the first side's own Reserve/Post Combat Pass. Reuses
    // CampaignNativeSecondMoveV44/CampaignNativeSecondMovementCompletedV45
    // unchanged -- both are already role-as-data generic (#518) and their
    // predecessor switches already match on CampaignNativeSecondReserveV43/
    // CampaignNativeSecondMoveV44 by type alone, not by which ExerciseSession
    // slot holds them, so no change was needed there (confirmed by reading
    // CampaignNativeSecondMoveV44.Source/.Create and
    // CampaignNativeSecondMovementCompletedV45.Create directly). A distinct
    // slot is required for the same reason CurrentStageEntryReserve needed
    // one: CurrentSecondMove/CurrentSecondMovementCompletion are already
    // permanently bound to Stage 1's second acting side, and no field on
    // this class is ever reset once set.
    internal CampaignNativeSecondMoveV44? CurrentStageEntryMove { get; private set; }
    internal CampaignWorldSnapshotV6? CurrentStageEntryMoveWorld { get; private set; }
    internal ExerciseSession? StageEntryMovePredecessor { get; private set; }
    internal CampaignNativeSecondMovementCompletedV45? CurrentStageEntryMovementCompletion { get; private set; }
    internal ExerciseSession? StageEntryMovementCompletionPredecessor { get; private set; }

    internal ExerciseSession WithStageEntryMovementCompletion(CampaignNativeSecondMovementCompletedV45 completed,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentNullException.ThrowIfNull(contacts);
        if (completed.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != completed.StateVersion)
            throw new InvalidOperationException("Stage Entry Movement completion must advance its retained cut.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntryMovementCompletionPredecessor = this;
        successor.CurrentStageEntryMovementCompletion = completed;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(completed);
        return successor;
    }

    internal ExerciseSession WithStageEntryMove(CampaignNativeSecondMoveV44 movement,
        CampaignWorldSnapshotV6 world, CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(contacts);
        if (movement.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != movement.StateVersion
            || movement.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(world))
            throw new InvalidOperationException("Stage Entry Movement must advance its retained World and contact cut.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntryMovePredecessor = this;
        successor.CurrentStageEntryMove = movement;
        successor.CurrentStageEntryMoveWorld = world;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(movement);
        return successor;
    }

    // MTS-TASK-005/006 (2026-11-23): the new stage's own first-acting-side
    // Breakdown, entered from this stage's own completed Movement Completion
    // (CurrentStageEntryMovementCompletion) instead of the first side's.
    // Reuses CampaignNativeSecondBreakdownV46's event payload/contract
    // unchanged, but -- unlike V43/V44/V45 -- V46.Create(ExerciseSession)
    // reads named session fields directly (CurrentSecondMovementCompletion,
    // SecondMovementCompletionPredecessor, SecondMovePredecessor,
    // CurrentSecondMoveWorld) rather than taking its predecessor by type, so
    // it cannot be reused as-is for this stage's own slots (confirmed by
    // reading CampaignNativeSecondBreakdownV46.Create directly). Added
    // CampaignNativeSecondBreakdownV46.CreateForStageEntry, a duplicate of
    // Create with those four reads substituted for their Stage Entry
    // equivalents -- mirroring the per-generation duplication this codebase
    // already uses at the CampaignExercises layer (QuerySecondXxx/
    // QueryStageEntryXxx), rather than introducing new delegate-based
    // parameterization for a single call site.
    internal CampaignNativeSecondBreakdownV46? CurrentStageEntryBreakdown { get; private set; }
    internal ExerciseSession? StageEntryBreakdownPredecessor { get; private set; }

    internal ExerciseSession WithStageEntryBreakdown(CampaignNativeSecondBreakdownV46 completed,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentStageEntryBreakdown is not null || CurrentStageEntryMovementCompletion is null
            || completed.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != completed.StateVersion)
            throw new InvalidOperationException("Stage Entry Breakdown must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntryBreakdownPredecessor = this;
        successor.CurrentStageEntryBreakdown = completed;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(completed);
        return successor;
    }

    // MTS-TASK-005/006 (2026-11-23): the new stage's own first-acting-side
    // Combat Decision, entered from this stage's own completed Breakdown
    // (CurrentStageEntryBreakdown) instead of the #394 second side's.
    // Unlike V46 (Breakdown), CampaignNativeSecondCombatDecisionStateV47's
    // own Select factory already takes world/position/turnFacts/randomState
    // as explicit parameters rather than reading session fields directly
    // (confirmed by reading CampaignNativeSecondCombatDecisionV47.Select
    // directly), so it needed no new entry point on the native class
    // itself -- only this stage's own ExerciseSession slot, same reason
    // every prior Stage Entry slot needed one. SecondAssaultFrame (used
    // below, unchanged) is likewise already parameterized by the decision/
    // turnFacts/history passed in, not by session field name.
    //
    // Accepting the decision immediately spins up CurrentStageEntryAssault
    // (mirroring WithSecondCombatSelection exactly). The follow-up slice
    // (see WithStageEntryAssaultEvent below) found most of the ~15
    // pre-existing CampaignExercises call sites that ternary on
    // CurrentSecondAssault don't need per-site changes at all: the
    // Barrage-through-Custody methods dispatch generically through the
    // CurrentRepeatAssault computed property, which now resolves to
    // CurrentStageEntryAssault first. Only the later-tier methods that
    // bypass that property on purpose (Reserve Release onward) needed
    // explicit new branches.
    internal CampaignNativeSecondCombatDecisionStateV47? CurrentStageEntryCombatDecision { get; private set; }
    internal ExerciseSession? StageEntryCombatSelectionPredecessor { get; private set; }

    internal ExerciseSession WithStageEntryCombatSelection(CampaignNativeSecondCombatDecisionStateV47 selected,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentStageEntryBreakdown is null || CurrentStageEntryCombatDecision is not null
            || selected.Decision.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != selected.StateVersion)
            throw new InvalidOperationException("Stage Entry Combat selection must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntryCombatSelectionPredecessor = this;
        successor.CurrentStageEntryCombatDecision = selected;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(selected.Decision);
        successor.CurrentStageEntryAssault = CampaignNativeRepeatAssaultRuntime.ReplaySecond(
            SecondAssaultFrame(selected, CurrentSnapshot, CurrentHistory), Context, contacts,
            successor.CurrentHistory, []);
        return successor;
    }

    // The new stage's own V14-V26 assault stages after its V47 selection --
    // same shape as CurrentSecondAssault, a distinct slot for the same
    // already-established reason (CurrentSecondAssault is permanently bound
    // to Stage 1's second acting side). Created above as soon as the
    // decision is accepted (matching WithSecondCombatSelection's own
    // behavior), but not yet advanced past zero events by anything in this
    // slice -- see the remarks above WithStageEntryCombatSelection.
    internal CampaignNativeRepeatAssaultRuntime? CurrentStageEntryAssault { get; private set; }
    internal ExerciseSession? StageEntryAssaultPredecessor { get; private set; }

    // MTS-TASK-005/006 follow-up (2026-11-23, docs/work/plans/2026-11-23-mts-task-005-006-stage-entry-assault.md):
    // the Stage Entry counterpart to WithSecondAssaultEvent, same shape,
    // writing CurrentStageEntryAssault instead of CurrentSecondAssault. This
    // is the half of the fix that lets the ~10 Barrage-through-Custody
    // submit/complete methods (CampaignExercises.cs) pick up this stage's
    // own assault generically: they all read the CurrentRepeatAssault
    // computed property below (now extended to resolve to
    // CurrentStageEntryAssault first) rather than their own per-call-site
    // ternary, so they needed no changes themselves once that property and
    // WithRepeatAssaultEvent's own dispatch (below) both learned about this
    // slot. Any later-tier method that still reads CurrentSecondAssault
    // explicitly (not through CurrentRepeatAssault) -- e.g.
    // QueryNativeReserveReleaseState's own `?.Release` check, which
    // deliberately bypasses CurrentRepeatAssault's IsSecondSideFinish guard
    // -- still needs its own explicit CurrentStageEntryAssault branch; see
    // the doc for the full accounting of which methods are which.
    internal ExerciseSession WithStageEntryAssaultEvent(object campaignEvent,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(campaignEvent);
        ArgumentNullException.ThrowIfNull(contacts);
        var runtime = CurrentStageEntryAssault!.Advance(campaignEvent);
        if (runtime.Contacts.AuthorityVersion != contacts.AuthorityVersion
            || runtime.Contacts.Records.Count != contacts.Records.Count
            || runtime.Contacts.Records.Zip(contacts.Records)
                .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
            throw new InvalidOperationException(
                "Stage Entry assault step differs from its retained replay.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntryAssaultPredecessor = this;
        successor.CurrentStageEntryAssault = runtime;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(campaignEvent);
        if (campaignEvent is CampaignNativeCombatSettlementV24)
            successor.CurrentAmmunitionLedger = CurrentAmmunitionLedger is { } seed
                ? CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(seed, new object[] { runtime.Result! })
                : CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(Context, CurrentHistory);
        return successor;
    }

    internal CampaignNativeCombatNoAttackCompletionV13? CurrentStageEntryNoAttackCompletion { get; private set; }
    internal ExerciseSession? StageEntryNoAttackPredecessor { get; private set; }

    // Stage Entry counterpart to WithSecondNoAttack, same shape, writing
    // CurrentStageEntryAssault/CurrentStageEntryNoAttackCompletion instead of
    // CurrentSecondAssault/CurrentSecondNoAttackCompletion.
    // CampaignNativeCombatNoAttackCompletionV13 needs no change here either,
    // same reasoning as WithSecondNoAttack's own remarks.
    internal ExerciseSession WithStageEntryNoAttack(CampaignNativeCombatNoAttackCompletionV13 completion,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentStageEntryCombatDecision is not { } selected || CurrentStageEntryNoAttackCompletion is not null
            || completion.PriorStateVersion != selected.StateVersion
            || contacts.AuthorityVersion != completion.StateVersion)
            throw new InvalidOperationException("Stage Entry no-attack completion must advance its retained cut once.");
        var runtime = (CurrentStageEntryAssault
            ?? throw new InvalidOperationException("Stage Entry no-attack completion lacks its stage chain."))
            .Advance(completion);
        if (runtime.Contacts.AuthorityVersion != contacts.AuthorityVersion)
            throw new InvalidOperationException("Stage Entry no-attack completion differs from its retained replay.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntryNoAttackPredecessor = this;
        successor.StageEntryAssaultPredecessor = this;
        successor.CurrentStageEntryAssault = runtime;
        successor.CurrentStageEntryNoAttackCompletion = completion;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(completion);
        return successor;
    }

    // MTS-TASK-006 (2026-11-23): the new stage's own SECOND acting side's
    // Reserve Designation, entered from this stage's own first acting
    // side's completed chain (CurrentStageEntryAssault's terminal
    // PostCombatPasses entry) instead of the six-event preamble chain's
    // terminal event -- the direct, side-generic analogue of how #394's
    // second side was entered from the first side's own singular
    // CurrentPostCombatPass (see
    // docs/work/plans/2026-11-22-stage-entry-bridge-design-options.md's
    // "actually-relevant precedent" section: CampaignNativePostCombatPassV42's
    // own `next` position is already fully generic/catalog-driven via
    // Cna1979LandSequenceV4.GetNext, and CampaignNativeSecondReserveV43's
    // predecessor switch already accepts that type regardless of which
    // ExerciseSession slot it came from). Reuses CampaignNativeSecondReserveV43
    // unchanged -- same reasoning as CurrentStageEntryReserve above, this is a
    // fourth occurrence of the same event type. A distinct slot is required
    // for the same reason every prior Stage Entry slot needed one:
    // CurrentStageEntryReserve is already permanently bound to this stage's
    // first acting side, and no field on this class is ever reset once set.
    internal CampaignNativeSecondReserveV43? CurrentStageEntrySecondReserve { get; private set; }
    internal CampaignWorldSnapshotV6? CurrentStageEntrySecondReserveWorld { get; private set; }
    internal ExerciseSession? StageEntrySecondReservePredecessor { get; private set; }

    internal ExerciseSession WithStageEntrySecondReserve(CampaignNativeSecondReserveV43 reserve,
        CampaignWorldSnapshotV6 world, CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(reserve);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentStageEntryAssault is not { PostCombatPasses.Count: > 0 } || CurrentStageEntrySecondReserve is not null
            || reserve.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != reserve.StateVersion
            || reserve.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(world))
            throw new InvalidOperationException("Stage Entry second-side Reserve must advance its retained World and contact cut.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntrySecondReservePredecessor = this;
        successor.CurrentStageEntrySecondReserve = reserve;
        successor.CurrentStageEntrySecondReserveWorld = world;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(reserve);
        return successor;
    }

    // MTS-TASK-006 (2026-11-23): this stage's own SECOND acting side's
    // Movement + Movement Completion, entered from this stage's own
    // completed second-side Reserve Designation (CurrentStageEntrySecondReserve)
    // instead of the first acting side's completed chain. Reuses
    // CampaignNativeSecondMoveV44/CampaignNativeSecondMovementCompletedV45
    // unchanged -- confirmed by reading CampaignNativeSecondMoveV44.Source
    // directly: its predecessor switch matches on CampaignNativeSecondReserveV43/
    // CampaignNativeSecondMoveV44 by type alone, not by which ExerciseSession
    // slot holds them, so a fifth occurrence needs no change. A distinct
    // slot is required for the same reason every prior Stage Entry slot
    // needed one: CurrentStageEntryMove/CurrentStageEntryMovementCompletion
    // are already permanently bound to this stage's first acting side, and
    // no field on this class is ever reset once set.
    internal CampaignNativeSecondMoveV44? CurrentStageEntrySecondMove { get; private set; }
    internal CampaignWorldSnapshotV6? CurrentStageEntrySecondMoveWorld { get; private set; }
    internal ExerciseSession? StageEntrySecondMovePredecessor { get; private set; }
    internal CampaignNativeSecondMovementCompletedV45? CurrentStageEntrySecondMovementCompletion { get; private set; }
    internal ExerciseSession? StageEntrySecondMovementCompletionPredecessor { get; private set; }

    internal ExerciseSession WithStageEntrySecondMovementCompletion(CampaignNativeSecondMovementCompletedV45 completed,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentNullException.ThrowIfNull(contacts);
        if (completed.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != completed.StateVersion)
            throw new InvalidOperationException("Stage Entry second-side Movement completion must advance its retained cut.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntrySecondMovementCompletionPredecessor = this;
        successor.CurrentStageEntrySecondMovementCompletion = completed;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(completed);
        return successor;
    }

    internal ExerciseSession WithStageEntrySecondMove(CampaignNativeSecondMoveV44 movement,
        CampaignWorldSnapshotV6 world, CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(contacts);
        if (movement.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != movement.StateVersion
            || movement.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(world))
            throw new InvalidOperationException("Stage Entry second-side Movement must advance its retained World and contact cut.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntrySecondMovePredecessor = this;
        successor.CurrentStageEntrySecondMove = movement;
        successor.CurrentStageEntrySecondMoveWorld = world;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(movement);
        return successor;
    }

    // MTS-TASK-006 (2026-11-23): this stage's own SECOND acting side's
    // Breakdown, entered from this stage's own completed second-side
    // Movement Completion (CurrentStageEntrySecondMovementCompletion)
    // instead of the first acting side's. Via
    // CampaignNativeSecondBreakdownV46.CreateForStageEntrySecondSide -- see
    // that method's remarks for why a third entry point was needed. A
    // distinct slot is required for the same reason every prior Stage Entry
    // slot needed one: CurrentStageEntryBreakdown is already permanently
    // bound to this stage's first acting side, and no field on this class
    // is ever reset once set.
    internal CampaignNativeSecondBreakdownV46? CurrentStageEntrySecondBreakdown { get; private set; }
    internal ExerciseSession? StageEntrySecondBreakdownPredecessor { get; private set; }

    internal ExerciseSession WithStageEntrySecondBreakdown(CampaignNativeSecondBreakdownV46 completed,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentStageEntrySecondBreakdown is not null || CurrentStageEntrySecondMovementCompletion is null
            || completed.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != completed.StateVersion)
            throw new InvalidOperationException("Stage Entry second-side Breakdown must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntrySecondBreakdownPredecessor = this;
        successor.CurrentStageEntrySecondBreakdown = completed;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(completed);
        return successor;
    }

    // MTS-TASK-006 (2026-11-23): this stage's own SECOND acting side's
    // Combat Decision, entered from this stage's own completed second-side
    // Breakdown (CurrentStageEntrySecondBreakdown) instead of the
    // first-acting-side equivalent. CampaignNativeSecondCombatDecisionStateV47.Select
    // needs no change -- confirmed already fully parameterized when
    // MTS-TASK-005's own Combat Decision slice built it, same confirmation
    // applies to a fourth occurrence.
    internal CampaignNativeSecondCombatDecisionStateV47? CurrentStageEntrySecondCombatDecision { get; private set; }
    internal ExerciseSession? StageEntrySecondCombatSelectionPredecessor { get; private set; }

    internal ExerciseSession WithStageEntrySecondCombatSelection(CampaignNativeSecondCombatDecisionStateV47 selected,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentStageEntrySecondBreakdown is null || CurrentStageEntrySecondCombatDecision is not null
            || selected.Decision.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != selected.StateVersion)
            throw new InvalidOperationException("Stage Entry second-side Combat selection must advance its retained cut once.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntrySecondCombatSelectionPredecessor = this;
        successor.CurrentStageEntrySecondCombatDecision = selected;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(selected.Decision);
        successor.CurrentStageEntrySecondAssault = CampaignNativeRepeatAssaultRuntime.ReplaySecond(
            SecondAssaultFrame(selected, CurrentSnapshot, CurrentHistory), Context, contacts,
            successor.CurrentHistory, []);
        return successor;
    }

    // This stage's own SECOND acting side's V14-V26 assault stages after
    // its V47 selection -- same shape as CurrentStageEntryAssault, a
    // distinct slot for the same already-established reason
    // (CurrentStageEntryAssault is permanently bound to this stage's first
    // acting side). Created above as soon as the decision is accepted.
    internal CampaignNativeRepeatAssaultRuntime? CurrentStageEntrySecondAssault { get; private set; }
    internal ExerciseSession? StageEntrySecondAssaultPredecessor { get; private set; }

    // Stage Entry second-side counterpart to WithStageEntryAssaultEvent,
    // same shape, writing CurrentStageEntrySecondAssault instead of
    // CurrentStageEntryAssault. This is the half of the fix that lets the
    // Barrage-through-Custody submit/complete methods pick up this stage's
    // own second acting side's assault generically through the
    // CurrentRepeatAssault computed property (now extended to a four-way
    // resolution) rather than their own per-call-site branching.
    internal ExerciseSession WithStageEntrySecondAssaultEvent(object campaignEvent,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(campaignEvent);
        ArgumentNullException.ThrowIfNull(contacts);
        var runtime = CurrentStageEntrySecondAssault!.Advance(campaignEvent);
        if (runtime.Contacts.AuthorityVersion != contacts.AuthorityVersion
            || runtime.Contacts.Records.Count != contacts.Records.Count
            || runtime.Contacts.Records.Zip(contacts.Records)
                .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
            throw new InvalidOperationException(
                "Stage Entry second-side assault step differs from its retained replay.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntrySecondAssaultPredecessor = this;
        successor.CurrentStageEntrySecondAssault = runtime;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(campaignEvent);
        if (campaignEvent is CampaignNativeCombatSettlementV24)
            successor.CurrentAmmunitionLedger = CurrentAmmunitionLedger is { } seed
                ? CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(seed, new object[] { runtime.Result! })
                : CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(Context, CurrentHistory);
        return successor;
    }

    internal CampaignNativeCombatNoAttackCompletionV13? CurrentStageEntrySecondNoAttackCompletion { get; private set; }
    internal ExerciseSession? StageEntrySecondNoAttackPredecessor { get; private set; }

    // Stage Entry second-side counterpart to WithStageEntryNoAttack, same
    // shape, writing CurrentStageEntrySecondAssault/CurrentStageEntrySecondNoAttackCompletion
    // instead of CurrentStageEntryAssault/CurrentStageEntryNoAttackCompletion.
    internal ExerciseSession WithStageEntrySecondNoAttack(CampaignNativeCombatNoAttackCompletionV13 completion,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentNullException.ThrowIfNull(contacts);
        if (CurrentStageEntrySecondCombatDecision is not { } selected || CurrentStageEntrySecondNoAttackCompletion is not null
            || completion.PriorStateVersion != selected.StateVersion
            || contacts.AuthorityVersion != completion.StateVersion)
            throw new InvalidOperationException("Stage Entry second-side no-attack completion must advance its retained cut once.");
        var runtime = (CurrentStageEntrySecondAssault
            ?? throw new InvalidOperationException("Stage Entry second-side no-attack completion lacks its stage chain."))
            .Advance(completion);
        if (runtime.Contacts.AuthorityVersion != contacts.AuthorityVersion)
            throw new InvalidOperationException("Stage Entry second-side no-attack completion differs from its retained replay.");
        var successor = (ExerciseSession)MemberwiseClone();
        successor.StageEntrySecondNoAttackPredecessor = this;
        successor.StageEntrySecondAssaultPredecessor = this;
        successor.CurrentStageEntrySecondAssault = runtime;
        successor.CurrentStageEntrySecondNoAttackCompletion = completion;
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(completion);
        return successor;
    }

    // Overhaul doc (docs/work/plans/2026-11-23-codebase-overhaul-direction.md),
    // Section 0.6 step 1: PendingDecision as a computed/derived property,
    // literally the same cascade CampaignExercises.QueryCheckpoint already
    // uses (ground truth for ordering), just centralized here instead of
    // being re-derived independently at every call site. Scoped to the
    // #394-onward mechanics only; the original pre-#394 Combat/Reserve-
    // Release/Cycle-Control/Repeat chain (CurrentCombatDecision through
    // CurrentPostRepeatEvents) is untouched and has no PendingDecision
    // subtype.
    //
    // Correctness requirement, not a style choice: deliberately NOT cached
    // on a backing field. Every WithXxx successor is built via
    // MemberwiseClone(), which would copy a cached value straight from the
    // predecessor into the successor before the successor's own Current*
    // mutation runs -- silently pinning Pending to the predecessor's stale
    // answer forever (caught by CampaignCombatExerciseAssaultTests while
    // building this change). Recomputing on every access is the only safe
    // option under MemberwiseClone; the cascade itself is cheap.
    //
    // One deliberate ordering exception: the whole "Second*" (#394, Stage 1's
    // own second-acting-side) group below carries the
    // CurrentInitiativeDeclaration-is-null guard that ReconstructOne's own
    // cascade already added for CurrentSecondAssault alone (MTS-TASK-003) to
    // stop a stale, never-reset field from intercepting the chronologically
    // later preamble chain. Stage 3 scoping verification (2026-11-23) found
    // the same staleness on CurrentSecondAssault's six siblings
    // (CurrentSecondNoAttackCompletion/CurrentSecondCombatDecision/
    // CurrentSecondBreakdown/CurrentSecondMovementCompletion/CurrentSecondMove/
    // CurrentSecondReserve) -- unguarded, any one of them permanently
    // shadows Stage 2's own preamble/StageEntry chain in this cascade once a
    // real session has played through Operation Stage 1's second side, which
    // no test exercised until Stage 2's own close-out existed to drive to.
    // The guard is applied to the whole group now, not just the one arm.
    internal PendingDecision? Pending => ComputePending();

    private PendingDecision? ComputePending()
    {
        // Stage 3's own first-acting-side cuts (S3-1..S3-3) are chronologically
        // latest, so they come first, most-recent-first.
        if (CurrentThirdStageCombatDecision is not null)
            return new ThirdStageCombatSelectionPending(ThirdStageCombatSelectionPredecessor);
        if (CurrentThirdStageBreakdown is not null)
            return new ThirdStageBreakdownPending(ThirdStageBreakdownPredecessor);
        if (CurrentThirdStageMovementCompletion is not null)
            return new ThirdStageMovementCompletionPending(ThirdStageMovementCompletionPredecessor);
        if (CurrentThirdStageMove is not null)
            return new ThirdStageMovePending(ThirdStageMovePredecessor);
        if (CurrentThirdStageReserve is not null)
            return new ThirdStageReservePending(ThirdStageReservePredecessor);
        // Later-stage preamble cuts take precedence over every retained Stage 2 cut.
        // Same staleness shape as the Stage 2 preamble group further down:
        // these fields are never reset, so once Stage 3's own Reserve exists
        // (that group's first field) they must not shadow it.
        if (CurrentThirdStageReserve is null && CurrentThirdStageNoObligationFleetRepair is not null)
            return new ThirdStageNoObligationFleetRepairPending(ThirdStageNoObligationFleetRepairPredecessor);
        if (CurrentThirdStageReserve is null && CurrentThirdStageNoObligationFleetAssignment is not null)
            return new ThirdStageNoObligationFleetAssignmentPending(ThirdStageNoObligationFleetAssignmentPredecessor);
        if (CurrentThirdStageReserve is null && CurrentThirdStageNoObligationNavalConvoyArrival is not null)
            return new ThirdStageNoObligationNavalConvoyArrivalPending(ThirdStageNoObligationNavalConvoyArrivalPredecessor);
        if (CurrentThirdStageReserve is null && CurrentThirdStageNoObligationOrganization is not null)
            return new ThirdStageNoObligationOrganizationPending(ThirdStageNoObligationOrganizationPredecessor);
        if (CurrentThirdStageReserve is null && CurrentThirdStageWeather is not null)
            return new ThirdStageWeatherPending(ThirdStageWeatherPredecessor);
        if (CurrentThirdStageReserve is null && CurrentThirdStageInitiativeDeclaration is not null)
            return new ThirdStageInitiativeDeclarationPending(ThirdStageInitiativeDeclarationPredecessor);
        if (CurrentStageEntrySecondAssault is { Events.Count: > 0 })
            return new StageEntrySecondAssaultPending(StageEntrySecondAssaultPredecessor);
        if (CurrentStageEntrySecondNoAttackCompletion is not null)
            return new StageEntrySecondNoAttackPending(StageEntrySecondNoAttackPredecessor);
        if (CurrentStageEntrySecondCombatDecision is not null)
            return new StageEntrySecondCombatSelectionPending(StageEntrySecondCombatSelectionPredecessor);
        if (CurrentStageEntrySecondBreakdown is not null)
            return new StageEntrySecondBreakdownPending(StageEntrySecondBreakdownPredecessor);
        if (CurrentStageEntrySecondMovementCompletion is not null)
            return new StageEntrySecondMovementCompletionPending(StageEntrySecondMovementCompletionPredecessor);
        if (CurrentStageEntrySecondMove is not null)
            return new StageEntrySecondMovePending(StageEntrySecondMovePredecessor);
        if (CurrentStageEntrySecondReserve is not null)
            return new StageEntrySecondReservePending(StageEntrySecondReservePredecessor);
        if (CurrentStageEntryAssault is { Events.Count: > 0 })
            return new StageEntryAssaultPending(StageEntryAssaultPredecessor);
        if (CurrentStageEntryNoAttackCompletion is not null)
            return new StageEntryNoAttackPending(StageEntryNoAttackPredecessor);
        // See this property's remarks: guarded so a stale, never-reset
        // CurrentSecondAssault cannot intercept the chronologically later
        // preamble chain below (mirrors ReconstructOne's MTS-TASK-003 fix).
        // Stage 3 scoping verification (2026-11-23,
        // docs/work/plans/2026-11-23-stage-3-scoping-verification.md): the
        // same staleness applies to every one of this field's siblings below
        // (SecondNoAttack/SecondCombatSelection/SecondBreakdown/
        // SecondMovementCompletion/SecondMove/SecondReserve) -- the
        // 2026-11-23 Reconstruct cascade ordering audit assumed this whole
        // cascade was already fully correct (its own "Method" section took
        // QueryCheckpoint as ground truth without re-checking it) and so
        // never looked for this; it only ever surfaces once a real session
        // reaches Stage 2's own second-acting-side close-out, which no test
        // drove until this one.
        if (CurrentInitiativeDeclaration is null && CurrentSecondAssault is { Events.Count: > 0 })
            return new SecondAssaultPending(SecondAssaultPredecessor);
        if (CurrentInitiativeDeclaration is null && CurrentSecondNoAttackCompletion is not null)
            return new SecondNoAttackPending(SecondNoAttackPredecessor);
        if (CurrentInitiativeDeclaration is null && CurrentSecondCombatDecision is not null)
            return new SecondCombatSelectionPending(SecondCombatSelectionPredecessor);
        if (CurrentInitiativeDeclaration is null && CurrentSecondBreakdown is not null)
            return new SecondBreakdownPending(SecondBreakdownPredecessor);
        if (CurrentInitiativeDeclaration is null && CurrentSecondMovementCompletion is not null)
            return new SecondMovementCompletionPending(SecondMovementCompletionPredecessor);
        if (CurrentInitiativeDeclaration is null && CurrentSecondMove is not null)
            return new SecondMovePending(SecondMovePredecessor);
        if (CurrentInitiativeDeclaration is null && CurrentSecondReserve is not null)
            return new SecondReservePending(SecondReservePredecessor);
        // Same staleness shape one more time: the six-event preamble chain's
        // own fields (through CurrentInitiativeDeclaration) are never reset
        // either, so once Stage 2's own StageEntry chain (Reserve onward)
        // has actually started, these would otherwise permanently shadow it
        // the same way the "Second*" group above would shadow this preamble
        // chain. CurrentStageEntryReserve is this next group's own first
        // field, the same role CurrentInitiativeDeclaration itself plays as
        // guard for the group above.
        if (CurrentStageEntryReserve is null && CurrentNoObligationFleetRepair is not null)
            return new NoObligationFleetRepairPending(NoObligationFleetRepairPredecessor);
        if (CurrentStageEntryReserve is null && CurrentNoObligationFleetAssignment is not null)
            return new NoObligationFleetAssignmentPending(NoObligationFleetAssignmentPredecessor);
        if (CurrentStageEntryReserve is null && CurrentNoObligationNavalConvoyArrival is not null)
            return new NoObligationNavalConvoyArrivalPending(NoObligationNavalConvoyArrivalPredecessor);
        if (CurrentStageEntryReserve is null && CurrentNoObligationOrganization is not null)
            return new NoObligationOrganizationPending(NoObligationOrganizationPredecessor);
        if (CurrentStageEntryReserve is null && CurrentStageEntryWeather is not null)
            return new StageEntryWeatherPending(StageEntryWeatherPredecessor);
        if (CurrentStageEntryReserve is null && CurrentInitiativeDeclaration is not null)
            return new InitiativeDeclarationPending(InitiativeDeclarationPredecessor);
        if (CurrentStageEntryCombatDecision is not null)
            return new StageEntryCombatSelectionPending(StageEntryCombatSelectionPredecessor);
        if (CurrentStageEntryBreakdown is not null)
            return new StageEntryBreakdownPending(StageEntryBreakdownPredecessor);
        if (CurrentStageEntryMovementCompletion is not null)
            return new StageEntryMovementCompletionPending(StageEntryMovementCompletionPredecessor);
        if (CurrentStageEntryMove is not null)
            return new StageEntryMovePending(StageEntryMovePredecessor);
        if (CurrentStageEntryReserve is not null)
            return new StageEntryReservePending(StageEntryReservePredecessor);
        if (CurrentPostCombatPass is not null)
            return new PostCombatPassPending(PostCombatPassPredecessor);
        if (CurrentTruckConvoyPass is not null)
            return new TruckConvoyPassPending(TruckConvoyPassPredecessor);
        return null;
    }

    public override string ToString() => nameof(ExerciseSession);
}
