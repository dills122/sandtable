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
    internal CampaignNativeCombatRetreatDeclineV15? Decline { get; private set; }
    private readonly List<CampaignNativeCombatForceSealV17> seals = [];
    internal IReadOnlyList<CampaignNativeCombatForceSealV17> Seals => seals;
    internal CampaignNativeCombatForceCompletionV18? ForceCompletion { get; private set; }
    internal CampaignNativeCombatAntiArmorV20? AntiArmor { get; private set; }
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
        if (Decline is null)
        {
            if (item is not CampaignNativeCombatRetreatDeclineV15 decline)
                throw new InvalidCampaignHistoryException(
                    "Repeated assault needs an accepted defender RBA decision.");
            Decline = CampaignNativeCombatRetreatDeclineV15.Replay(Selection,
                Barrage, Context, Contacts, decline.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCombatRetreatDecline(Decline);
            return;
        }
        if (seals.Count < 2)
        {
            if (item is not CampaignNativeCombatForceSealV17 seal)
                throw new InvalidCampaignHistoryException(
                    "Repeated assault needs both accepted Force seals.");
            var verified = CampaignNativeCombatForceSealV17.Replay(Selection,
                Decline, Selection.World, Context, Contacts,
                seal.SerializeCanonical(), seals.Count == 0 ? null : seals[0]);
            seals.Add(verified);
            Contacts = Contacts.AdvanceNativeCombatForceSeal(verified);
            return;
        }
        if (ForceCompletion is null)
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
                ForceCompletion, null, Context, antiArmor.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCombatAntiArmor(AntiArmor);
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
                ForceCompletion, AntiArmor, seals, Selection.World,
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
