using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// Replays the Combat stages following a selected later-cycle assault from the
// accepted V31 cut. It uses the retained World and random cursor, not a
// reconstructed first-cycle snapshot.
internal sealed class CampaignNativeRepeatAssaultRuntime
{
    private CampaignNativeRepeatAssaultRuntime(
        CampaignCombatSelectionFrame selection,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        IReadOnlyList<object> priorHistory)
    {
        Selection = selection;
        Context = context;
        Contacts = contacts;
        PriorHistory = priorHistory;
    }

    internal CampaignCombatSelectionFrame Selection { get; }
    internal CampaignContentContext Context { get; }
    internal CampaignContactJournal Contacts { get; private set; }
    private IReadOnlyList<object> PriorHistory { get; }
    internal CampaignNativeCombatBarrageV14? Barrage { get; private set; }
    private readonly List<CampaignNativeCombatRetreatMoveV16> retreatMoves = [];
    private readonly List<CampaignWorldSnapshotV6> retreatWorlds = [];
    internal IReadOnlyList<CampaignNativeCombatRetreatMoveV16> RetreatMoves => retreatMoves;
    internal CampaignWorldSnapshotV6? RetreatWorld { get; private set; }
    internal CampaignContactJournal? RetreatPriorContacts { get; private set; }
    internal byte[] SerializeRetreatMoveState()
    {
        if (retreatMoves.Count == 0 || RetreatPriorContacts is null)
            throw new InvalidOperationException("Repeated RBA move state is absent.");
        var state = retreatMoves[0].SerializeStateCanonical(Selection, Barrage!,
            Context, RetreatPriorContacts);
        for (var index = 1; index < retreatMoves.Count; index++)
            state = retreatMoves[index].SerializeNextStateCanonical(state,
                retreatWorlds[index]);
        return state;
    }
    internal byte[] SerializeDeclineState() => retreatMoves.Count > 0
        ? Decline!.SerializeAfterMoveStateCanonical(SerializeRetreatMoveState())
        : Decline!.SerializeStateCanonical(Selection, Barrage!);
    internal CampaignNativeCombatRbaCancellationV33? RbaCancellation { get; private set; }
    internal CampaignNativeCombatRetreatDeclineV15? Decline { get; private set; }
    private readonly List<CampaignNativeCombatForceSealV17> seals = [];
    internal IReadOnlyList<CampaignNativeCombatForceSealV17> Seals => seals;
    internal CampaignNativeCombatForceCompletionV18? ForceCompletion { get; private set; }
    internal CampaignNativeCombatForceCancellationV19? ForceCancellation { get; private set; }
    internal CampaignNativeCombatAntiArmorV20? AntiArmor { get; private set; }
    internal CampaignNativeCombatCancelledAssaultV21? CancelledAssault { get; private set; }
    internal CampaignNativeCombatAssaultResultV22? Result { get; private set; }
    internal CampaignWorldSnapshotV6? PaidWorld { get; private set; }
    internal CampaignNativeCombatAssaultInputs? Inputs { get; private set; }
    internal CampaignNativeCombatRetreatAfterAssaultV23? PostResultRetreat { get; private set; }
    internal CampaignNativeCombatSettlementV24? Settlement { get; private set; }
    internal CampaignWorldSnapshotV6? SettledWorld { get; private set; }
    internal CampaignNativeCombatCustodyDispositionV25? Custody { get; private set; }
    internal CampaignWorldSnapshotV6? CustodyWorld { get; private set; }
    internal CampaignNativeCombatCustodyStateV25? CustodyState { get; private set; }
    internal CampaignNativeCombatSuccessorV26? Successor { get; private set; }
    internal CampaignNativeReserveReleaseStateV27? Release { get; private set; }
    internal CampaignNativeCycleControlV28? Control { get; private set; }

    internal static CampaignNativeRepeatAssaultRuntime Replay(
        CampaignSnapshotV11 initial,
        CampaignNativeRepeatCombatDecisionV31 selection,
        CampaignWorldSnapshotV6 world,
        RandomStreamState randomState,
        CampaignContentContext context,
        CampaignContactJournal contactsAfterSelection,
        IReadOnlyList<object> priorHistory,
        IEnumerable<object> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var retained = events.ToArray();
        if (retained.Length > 1024)
            throw new InvalidCampaignHistoryException(
                "Repeated assault history exceeds its retained event bound.");
        var frame = CampaignCombatSelectionFrame.FromRepeat(initial,
            selection, world, randomState);
        if (selection.Choice != "select-contact-assault"
            || contactsAfterSelection.CampaignId != selection.CampaignId
            || contactsAfterSelection.AuthorityVersion != selection.StateVersion)
            throw new InvalidCampaignHistoryException(
                "Repeated assault lacks accepted same-cut contact authority.");
        var runtime = new CampaignNativeRepeatAssaultRuntime(frame, context,
            contactsAfterSelection, priorHistory);
        foreach (var item in retained)
            runtime.Apply(item);
        return runtime;
    }

    private void Apply(object item)
    {
        if (Barrage is null)
        {
            if (item is not CampaignNativeCombatBarrageV14 barrage)
                throw new InvalidCampaignHistoryException(
                    "Selected repeated assault must enter Barrage.");
            Barrage = CampaignNativeCombatBarrageV14.Replay(Selection,
                Context, barrage.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCombatBarrage(Barrage);
            return;
        }
        if (RbaCancellation is not null)
        {
            ApplyReleaseOrControl(item);
            return;
        }
        if (Decline is null && item is CampaignNativeCombatRbaCancellationV33 rbaCancellation)
        {
            RbaCancellation = CampaignNativeCombatRbaCancellationV33.Replay(
                Selection, Barrage, retreatMoves.LastOrDefault(), rbaCancellation.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCombatRbaCancellation(RbaCancellation);
            Release = CampaignNativeReserveReleaseStateV27.InitializeAfterRepeatRbaCancellation(
                Selection, RbaCancellation, RetreatWorld ?? Selection.World, Context);
            return;
        }
        if (Decline is null)
        {
            if (item is CampaignNativeCombatRetreatMoveV16 move)
            {
                var previous = retreatMoves.Count == 0 ? null : retreatMoves[^1];
                var verified = previous is null
                    ? CampaignNativeCombatRetreatMoveV16.ReplayFirst(Selection,
                        Barrage, Context, Contacts, move.SerializeCanonical())
                    : CampaignNativeCombatRetreatMoveV16.ReplayNext(Selection,
                        Barrage, previous, RetreatWorld!, retreatMoves[0].OriginLocationId,
                        Context, Contacts, move.SerializeCanonical());
                var world = previous is null
                    ? verified.ProjectWorld(Selection, Barrage, Context, Contacts)
                    : verified.ProjectWorldAfter(Selection, Barrage, previous,
                        RetreatWorld!, retreatMoves[0].OriginLocationId, Context, Contacts);
                var beforeWorld = RetreatWorld ?? Selection.World;
                RetreatPriorContacts ??= Contacts;
                Contacts = Contacts.AdvanceNativeCombatRetreatMove(beforeWorld,
                    world, verified, Selection.Decision.BoundRepresentationId!, Context);
                RetreatWorld = world;
                retreatMoves.Add(verified);
                retreatWorlds.Add(world);
                return;
            }
            if (item is not CampaignNativeCombatRetreatDeclineV15 decline)
                throw new InvalidCampaignHistoryException(
                    "Repeated assault needs an accepted defender RBA decision.");
            Decline = retreatMoves.Count == 0
                ? CampaignNativeCombatRetreatDeclineV15.Replay(Selection,
                    Barrage, Context, Contacts, decline.SerializeCanonical())
                : CampaignNativeCombatRetreatDeclineV15.ReplayAfterMove(
                    Selection, Barrage, retreatMoves[^1], RetreatWorld!,
                    retreatMoves[0].OriginLocationId, Context, Contacts,
                    decline.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCombatRetreatDecline(Decline);
            return;
        }
        if (ForceCompletion is null && ForceCancellation is null
            && item is CampaignNativeCombatForceCancellationV19 cancellation)
        {
            ForceCancellation = CampaignNativeCombatForceCancellationV19.Replay(
                Selection, Decline, seals, cancellation.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCombatForceCancellation(ForceCancellation);
            return;
        }
        if (seals.Count < 2 && ForceCancellation is null)
        {
            if (item is not CampaignNativeCombatForceSealV17 seal)
                throw new InvalidCampaignHistoryException(
                    "Repeated assault needs both accepted Force seals.");
            var verified = CampaignNativeCombatForceSealV17.Replay(Selection,
                Decline, RetreatWorld ?? Selection.World, Context, Contacts,
                seal.SerializeCanonical(), seals.Count == 0 ? null : seals[0]);
            seals.Add(verified);
            Contacts = Contacts.AdvanceNativeCombatForceSeal(verified);
            return;
        }
        if (ForceCompletion is null && ForceCancellation is null)
        {
            if (item is not CampaignNativeCombatForceCompletionV18 completion)
                throw new InvalidCampaignHistoryException(
                    "Repeated Force Assignment needs its accepted completion.");
            ForceCompletion = CampaignNativeCombatForceCompletionV18.Replay(
                Selection, Decline, seals, completion.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCombatForceCompletion(ForceCompletion);
            return;
        }
        if (AntiArmor is null)
        {
            if (item is not CampaignNativeCombatAntiArmorV20 antiArmor)
                throw new InvalidCampaignHistoryException(
                    "Repeated assault needs accepted Anti Armor completion.");
            AntiArmor = CampaignNativeCombatAntiArmorV20.Replay(Selection,
                ForceCompletion, ForceCancellation, Context,
                antiArmor.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCombatAntiArmor(AntiArmor);
            return;
        }
        if (ForceCancellation is not null && CancelledAssault is null)
        {
            if (item is not CampaignNativeCombatCancelledAssaultV21 cancelled)
                throw new InvalidCampaignHistoryException(
                    "Cancelled repeated assault needs accepted no-attack completion.");
            CancelledAssault = CampaignNativeCombatCancelledAssaultV21.Replay(
                Selection, AntiArmor, cancelled.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCombatCancelledAssault(CancelledAssault);
            Release = CampaignNativeReserveReleaseStateV27
                .InitializeAfterRepeatCancelledAssault(
                    Selection, CancelledAssault, RetreatWorld ?? Selection.World,
                    Context);
            return;
        }
        if (CancelledAssault is not null)
        {
            ApplyReleaseOrControl(item);
            return;
        }
        if (Result is null)
        {
            if (item is not CampaignNativeCombatAssaultResultV22 result)
                throw new InvalidCampaignHistoryException(
                    "Repeated assault needs an accepted Close Assault result.");
            var ammunition = CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(
                Context, PriorHistory);
            var inputs = CampaignNativeCombatAssaultInputs.Create(Selection,
                ForceCompletion!, AntiArmor, seals, RetreatWorld ?? Selection.World,
                Context, ammunition);
            Result = CampaignNativeCombatAssaultResultV22.Replay(Selection,
                AntiArmor, inputs, Selection.RandomState,
                result.SerializeCanonical());
            Inputs = inputs;
            PaidWorld = inputs.PaidWorld;
            Contacts = Contacts.AdvanceNativeCombatAssaultResult(Result);
            return;
        }
        if (PostResultRetreat is null)
        {
            if (item is not CampaignNativeCombatRetreatAfterAssaultV23 retreat)
                throw new InvalidCampaignHistoryException(
                    "Repeated result needs accepted defender retreat decision.");
            PostResultRetreat = CampaignNativeCombatRetreatAfterAssaultV23.Replay(
                Selection, Result, PaidWorld!, Context, Contacts,
                retreat.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCombatRetreatAfterAssault(PostResultRetreat);
            return;
        }
        if (Settlement is null)
        {
            if (item is not CampaignNativeCombatSettlementV24 settlement)
                throw new InvalidCampaignHistoryException(
                    "Repeated assault needs accepted World settlement.");
            Settlement = CampaignNativeCombatSettlementV24.Replay(Result,
                PostResultRetreat, Inputs!, Context,
                settlement.SerializeCanonical());
            SettledWorld = CampaignNativeCombatSettledWorldV24.Project(Result,
                PostResultRetreat, Inputs!, Context).World;
            Contacts = Contacts.AdvanceNativeCombatSettlement(PaidWorld!,
                SettledWorld, PostResultRetreat, Settlement, Context);
            return;
        }
        if (Custody is null)
        {
            if (item is not CampaignNativeCombatCustodyDispositionV25 custody)
                throw new InvalidCampaignHistoryException(
                    "Repeated assault needs accepted custody disposition.");
            var cut = CampaignNativeCombatCustodyCutV25.Project(Settlement,
                SettledWorld!, Context, Contacts);
            Custody = CampaignNativeCombatCustodyDispositionV25.Replay(
                Selection.PriorSnapshot, Settlement, SettledWorld!, cut,
                Context, custody.SerializeCanonical());
            var projection = CampaignNativeCombatCustodyProjectionV25.Project(
                Selection.PriorSnapshot, Settlement, SettledWorld!, cut,
                Custody.ChoiceKind, Context);
            CustodyWorld = projection.World;
            CustodyState = projection.Custody;
            Contacts = Contacts.AdvanceNativeCombatCustodyDisposition(Custody);
            return;
        }
        if (Successor is null)
        {
            if (item is not CampaignNativeCombatSuccessorV26 successor)
                throw new InvalidCampaignHistoryException(
                    "Repeated assault needs accepted same-slot successor.");
            Successor = CampaignNativeCombatSuccessorV26.Replay(Selection,
                Result!, Settlement!, Custody, CustodyWorld!, CustodyState,
                Context, successor.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCombatSuccessor(Successor);
            var projection = CampaignNativeCombatSuccessorV26.Project(Selection,
                Result!, Settlement!, Custody, CustodyWorld!, CustodyState, Context);
            Release = CampaignNativeReserveReleaseStateV27.InitializeAfterRepeatAssault(
                CampaignNativeRepeatCombatDecisionV31.Deserialize(
                    Selection.CanonicalSelection), Successor, projection, Context);
            return;
        }
        ApplyReleaseOrControl(item);
    }

    private void ApplyReleaseOrControl(object item)
    {
        if (item is CampaignNativeReserveReleaseEventV27 releaseEvent)
        {
            if (Control is not null)
                throw new InvalidCampaignHistoryException(
                    "Reserve Release cannot follow accepted cycle control.");
            Release = CampaignNativeReserveReleaseStateV27.Replay(Release!, Context,
                [releaseEvent.SerializeCanonical()]);
            Contacts = Contacts.AdvanceNativeReserveRelease(releaseEvent);
            return;
        }
        if (item is CampaignNativeCycleControlV28 control && Control is null)
        {
            Control = CampaignNativeCycleControlV28.Replay(Release!, Context,
                Contacts, control.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCycleControl(Control);
            return;
        }
        throw new InvalidCampaignHistoryException(
            "Repeated assault stage is not yet supported by the normal campaign.");
    }
}
