using System.Reflection;
using System.Runtime.CompilerServices;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.Core.Tests.Hosting;

// fix/stale-secondassault-guard: ExerciseSession.Pending's SecondAssaultPending
// arm (ExerciseSession.cs) carries a deliberate guard --
// `CurrentInitiativeDeclaration is null && CurrentSecondAssault is { Events.Count: > 0 }`
// -- that stops the stale, never-reset CurrentSecondAssault field from being
// misread as "still active" once the session has moved on to the Stage
// 1->2 preamble chain (which requires CurrentSecondAssault.PostCombatPasses
// already committed, see WithInitiativeDeclaration's own guard comment).
//
// A review of PR #548 found 6 other sites across CampaignExercises.cs and
// CampaignHostAdapter.cs that read session.CurrentSecondAssault directly
// without this guard -- a pre-existing, separate bug from #548. This file
// adds the same CurrentInitiativeDeclaration-is-null guard at each of those
// sites and proves it here for the two CampaignHostAdapter.cs query sites,
// which run their CurrentSecondAssault check before anything else touches
// Reserve Release state, so a bare, mostly-uninitialized session (built via
// reflection, same technique as ExerciseSessionPendingTests -- no
// integration test drives ExerciseSession/CampaignExercises mechanics
// end-to-end into Stage 2 yet) is enough to distinguish the guarded
// behavior from the unguarded one by exception message alone.
//
// The five CampaignExercises.cs sites (SubmitNativeReserveReleaseChoice,
// CompleteNativeReserveRelease, QueryPendingNativeCycleControl,
// TimeoutNativeReserveRelease, TimeoutNativeCycleControl, SubmitTruckConvoyPass,
// SubmitNativeCycleControl) all call QueryNativeReserveReleaseState (or an
// equivalent Release-dependent lookup) before reaching their own guarded
// CurrentSecondAssault check, so a bare session throws there first and
// never distinguishes guarded from unguarded behavior this way; those sites
// are fixed by the same mechanical, already-proven pattern but not
// independently regression-tested here.
public sealed class StaleSecondAssaultGuardTests
{
    private static ExerciseSession NewBareSession() =>
        (ExerciseSession)RuntimeHelpers.GetUninitializedObject(typeof(ExerciseSession));

    private static T NewBareInstance<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T))!;

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(target.GetType().Name, propertyName);
        property.SetValue(target, value);
    }

    // Builds a session shaped exactly like the bug: CurrentSecondAssault is
    // set (with a stale, already-committed Control, standing in for a
    // never-reset field from earlier in the session) AND
    // CurrentInitiativeDeclaration is also set, i.e. the session has
    // chronologically moved on past the second assault into the Stage 1->2
    // preamble chain. Correct behavior must not treat the stale
    // CurrentSecondAssault.Control as "the" active cycle-control decision.
    private static ExerciseSession SessionPastSecondAssaultIntoInitiativeDeclaration()
    {
        var runtime = NewBareInstance<CampaignNativeRepeatAssaultRuntime>()!;
        SetProperty(runtime!, "Control", NewBareInstance<CampaignNativeCycleControlV28>());

        var session = NewBareSession();
        SetProperty(session, "CurrentSecondAssault", runtime);
        SetProperty(session, "CurrentInitiativeDeclaration",
            NewBareInstance<CampaignNativeInitiativeDeclarationV49>());
        return session;
    }

    [Fact]
    public void QueryNativeCycleControlOwnerDraftIgnoresStaleSecondAssaultControlOnceInitiativeDeclarationIsSet()
    {
        var session = SessionPastSecondAssaultIntoInitiativeDeclaration();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(session, CampaignActionAudience.Axis));

        // Pre-fix, the unguarded cascade read the stale CurrentSecondAssault.Control
        // and short-circuited with "Cycle control is already committed." --
        // wrong, since the session has actually moved on. Guarded, the stale
        // field is skipped and the call falls through to the real (empty)
        // Reserve Release state instead.
        Assert.Equal("Reserve Release requires accepted Combat successor.", exception.Message);
    }

    [Fact]
    public void QueryNativeRepeatMovementOwnerDraftIgnoresStaleSecondAssaultOnceInitiativeDeclarationIsSet()
    {
        var session = SessionPastSecondAssaultIntoInitiativeDeclaration();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CampaignHostAdapter.QueryNativeRepeatMovementOwnerDraft(session, CampaignActionAudience.Axis));

        // Pre-fix, this threw "Repeated Movement is unavailable." purely
        // because the stale CurrentSecondAssault was non-null -- true
        // regardless of whether that is the session's real, current state.
        // Guarded, it instead falls through to the real Reserve Release
        // lookup, same as the cycle-control draft above.
        Assert.Equal("Reserve Release requires accepted Combat successor.", exception.Message);
    }
}
