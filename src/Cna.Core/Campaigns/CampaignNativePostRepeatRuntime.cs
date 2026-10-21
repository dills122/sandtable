using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// Replays every cycle after the first repeated Combat finish from retained
// authority. One reducer handles later cycle ordinals; no historical V11
// snapshot is reconstructed from the evolving World.
internal sealed class CampaignNativePostRepeatRuntime
{
    private CampaignNativePostRepeatRuntime(
        CampaignSnapshotV11 initial,
        CampaignContentContext context,
        RandomStreamState randomState,
        CampaignNativeReserveReleaseStateV27 release,
        CampaignNativeCycleControlV28? control,
        CampaignContactJournal contacts,
        IReadOnlyList<object> priorHistory,
        IReadOnlyList<object> events)
    {
        Initial = initial;
        Context = context;
        RandomState = randomState;
        assaultRandomState = randomState;
        World = release.World;
        Contacts = contacts;
        PriorHistory = priorHistory;
        Events = events;
        Release = release;
        Control = control;
    }

    internal CampaignSnapshotV11 Initial { get; }
    internal CampaignContentContext Context { get; }
    internal RandomStreamState RandomState { get; private set; }
    internal IReadOnlyList<object> PriorHistory { get; }
    internal IReadOnlyList<object> Events { get; }
    internal CampaignWorldSnapshotV6 World { get; private set; }
    internal CampaignContactJournal Contacts { get; private set; }
    internal CampaignNativeReserveReleaseStateV27 Release { get; private set; }
    internal CampaignNativeCycleControlV28? Control { get; private set; }
    internal CampaignNativeRepeatMovementV29? Movement { get; private set; }
    internal CampaignWorldSnapshotV6? MovedWorld { get; private set; }
    internal CampaignNativeRepeatBreakdownV30? Breakdown { get; private set; }
    internal CampaignNativeRepeatCombatDecisionV31? Selection { get; private set; }
    internal CampaignNativeRepeatAssaultRuntime? ActiveAssault { get; private set; }
    private CampaignContactJournal? assaultBaseContacts;
    private int assaultSelectionIndex = -1;
    private CampaignWorldSnapshotV6? assaultWorld;
    private RandomStreamState assaultRandomState;

    internal static CampaignNativePostRepeatRuntime Replay(
        CampaignSnapshotV11 initial,
        CampaignContentContext context,
        RandomStreamState randomState,
        CampaignNativeRepeatCombatDecisionV31 firstSelection,
        CampaignNativeRepeatCombatNoAttackV32 firstCompletion,
        CampaignWorldSnapshotV6 world,
        CampaignContactJournal contactsAfterCompletion,
        IReadOnlyList<object> priorHistory,
        IEnumerable<object> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var retained = events.ToArray();
        if (retained.Length > 1024)
            throw new InvalidCampaignHistoryException(
                "Later cycle history exceeds its retained event bound.");
        var release = CampaignNativeReserveReleaseStateV27
            .InitializeAfterRepeatNoAttack(initial, firstSelection,
                firstCompletion, world, context);
        var runtime = new CampaignNativePostRepeatRuntime(initial, context,
            randomState, release, null, contactsAfterCompletion,
            priorHistory, retained);
        for (var index = 0; index < retained.Length; index++)
            runtime.Apply(retained[index], index);
        return runtime;
    }

    internal static CampaignNativePostRepeatRuntime ReplayAfterSecondAssault(
        CampaignSnapshotV11 initial,
        CampaignContentContext context,
        RandomStreamState randomState,
        CampaignNativeReserveReleaseStateV27 release,
        CampaignNativeCycleControlV28 control,
        CampaignContactJournal contactsAfterControl,
        IReadOnlyList<object> priorHistory,
        IEnumerable<object> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var retained = events.ToArray();
        if (retained.Length > 1024 || !release.Completed
            || control.Choice != CampaignNativeCycleControlChoiceV28.Repeat
            || control.PriorStateVersion != release.StateVersion
            || contactsAfterControl.AuthorityVersion != control.StateVersion)
            throw new InvalidCampaignHistoryException(
                "Later cycle lacks a completed repeated-assault control cut.");
        var runtime = new CampaignNativePostRepeatRuntime(initial, context,
            randomState, release, control, contactsAfterControl,
            priorHistory, retained);
        for (var index = 0; index < retained.Length; index++)
            runtime.Apply(retained[index], index);
        return runtime;
    }

    private void Apply(object item, int eventIndex)
    {
        if (ActiveAssault is
            {
                Release: { } completedRelease,
                Control: { Choice: CampaignNativeCycleControlChoiceV28.Repeat } completedControl
            }
            && item is CampaignNativeRepeatMovementV29)
        {
            Release = completedRelease;
            Control = completedControl;
            World = completedRelease.World;
            RandomState = ActiveAssault.Result?.AfterRandomState ?? RandomState;
            Contacts = ActiveAssault.Contacts;
            ActiveAssault = null;
            assaultBaseContacts = null;
            assaultSelectionIndex = -1;
            Movement = null;
            MovedWorld = null;
            Breakdown = null;
            Selection = null;
        }
        if (!Release.Completed)
        {
            if (item is not CampaignNativeReserveReleaseEventV27 reserveEvent)
                throw new InvalidCampaignHistoryException(
                    "Later cycle must finish Reserve Release before cycle control.");
            var expected = reserveEvent.Kind == "member-choice"
                ? Release.Choose(Context, reserveEvent.ElementId!,
                    reserveEvent.Choice!.Value)
                : Release.Complete(Context);
            if (!reserveEvent.SerializeCanonical().AsSpan().SequenceEqual(
                expected.Event.SerializeCanonical()))
                throw new InvalidCampaignHistoryException(
                    "Later Reserve Release differs from its World.");
            Release = expected.State;
            World = Release.World;
            Contacts = Contacts.AdvanceNativeReserveRelease(reserveEvent);
            return;
        }
        if (Control is null)
        {
            if (item is not CampaignNativeCycleControlV28 control)
                throw new InvalidCampaignHistoryException(
                    "Later cycle lacks its owner repeat-or-finish edge.");
            Control = CampaignNativeCycleControlV28.Replay(Release,
                Context, Contacts, control.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeCycleControl(Control);
            return;
        }
        if (Control.Choice == CampaignNativeCycleControlChoiceV28.Finish)
            throw new InvalidCampaignHistoryException(
                "Finished later cycle cannot accept another Combat event.");
        if (Movement is null)
        {
            if (item is not CampaignNativeRepeatMovementV29 movement)
                throw new InvalidCampaignHistoryException(
                    "Later cycle lacks accepted Movement.");
            Movement = CampaignNativeRepeatMovementV29.Replay(Release,
                Control, Context, Contacts, movement.SerializeCanonical());
            MovedWorld = Movement.ProjectWorld(Release, Control,
                Context, Contacts);
            Contacts = Contacts.AdvanceNativeRepeatMove(World,
                MovedWorld, Movement, Context);
            World = MovedWorld;
            return;
        }
        if (Breakdown is null)
        {
            if (item is not CampaignNativeRepeatBreakdownV30 breakdown)
                throw new InvalidCampaignHistoryException(
                    "Later cycle lacks accepted Breakdown.");
            Breakdown = CampaignNativeRepeatBreakdownV30.Replay(Release,
                Control, Movement, World, Context, Contacts,
                breakdown.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeRepeatBreakdown(Breakdown);
            return;
        }
        if (Selection is null)
        {
            if (item is not CampaignNativeRepeatCombatDecisionV31 selection)
                throw new InvalidCampaignHistoryException(
                    "Later cycle lacks accepted Combat selection.");
            Selection = CampaignNativeRepeatCombatDecisionV31.Replay(Release,
                Control, Movement, Breakdown, World, Context, Contacts,
                selection.SerializeCanonical());
            Contacts = Contacts.AdvanceNativeRepeatCombatSelection(Selection);
            if (Selection.Choice == "select-contact-assault")
            {
                assaultSelectionIndex = eventIndex;
                assaultBaseContacts = Contacts;
                assaultWorld = World;
                assaultRandomState = RandomState;
                ActiveAssault = CampaignNativeRepeatAssaultRuntime.Replay(Initial,
                    Selection, World, RandomState, Context, Contacts,
                    PriorHistory.Concat(Events.Take(eventIndex + 1)).ToArray(), []);
            }
            return;
        }
        if (ActiveAssault is not null)
        {
            var assaultEvents = Events.Skip(assaultSelectionIndex + 1)
                .Take(eventIndex - assaultSelectionIndex).ToArray();
            ActiveAssault = CampaignNativeRepeatAssaultRuntime.Replay(Initial,
                Selection, assaultWorld!, assaultRandomState, Context,
                assaultBaseContacts!, PriorHistory.Concat(
                    Events.Take(assaultSelectionIndex + 1)).ToArray(),
                assaultEvents);
            Contacts = ActiveAssault.Contacts;
            if (ActiveAssault.Release is { } released)
            {
                Release = released;
                World = released.World;
            }
            return;
        }
        if (item is not CampaignNativeRepeatCombatNoAttackV32 noAttack)
            throw new InvalidCampaignHistoryException(
                "Later cycle cannot skip its selected Combat work.");
        var frame = CampaignCombatSelectionFrame.FromRepeat(Initial,
            Selection, World, RandomState);
        _ = CampaignNativeRepeatCombatNoAttackV32.Replay(Selection, frame,
            Context, noAttack.SerializeCanonical());
        Contacts = Contacts.AdvanceNativeRepeatCombatNoAttack(noAttack);
        Release = CampaignNativeReserveReleaseStateV27
            .InitializeAfterRepeatNoAttack(Initial, Selection, noAttack,
                World, Context);
        Control = null;
        Movement = null;
        MovedWorld = null;
        Breakdown = null;
        Selection = null;
    }
}
