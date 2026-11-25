using System.Reflection;
using System.Runtime.CompilerServices;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Hosting;

// Code-audit finding (2026-11-23, medium confidence):
// CampaignDurableAuthorityJournal.SelectionRounds.cs's AppendSelectionNoAttack
// decided whether a Selection-round no-attack timeout belonged to the repeat
// or the non-repeat completion path by calling the bare
// CampaignHostAdapter.IsNativeRepeatCombatVoluntaryFinish(session), which
// reads session.CurrentPostRepeat?.Selection ?? session.CurrentRepeatCombatSelection.
// CurrentRepeatCombatSelection (ExerciseSession.cs) is never reset for the
// rest of the session once set, so once ANY earlier Combat finishes a repeat
// cycle voluntarily, every LATER, unrelated Combat's initial Selection-round
// timeout also reads that stale decision and gets misrouted to
// CompleteNativeRepeatCombatNoAttack instead of CompleteNativeCombatNoAttack.
// Today this fails safe -- CompleteNativeRepeatCombatNoAttack independently
// guards on session.CurrentContacts.AuthorityVersion == selected.StateVersion,
// so the stale version almost certainly won't match and the call is
// rejected -- but that still surfaces as a spurious
// InvalidOperationException("Selection timeout cannot complete no-attack
// Combat.") rejecting a LEGITIMATE timeout on an unrelated later Combat.
// Same "stale never-reset field, no scoping guard" shape as the
// CurrentSecondAssault bug PR #551 fixed in CampaignHostAdapter.cs/
// CampaignExercises.cs (guarding reads with a condition that proves the
// stale field is still the one bound to the CURRENT round).
//
// The fix: CampaignHostAdapter.IsNativeRepeatCombatNoAttackPending scopes the
// same read by requiring the stale decision's own StateVersion to match the
// live session.CurrentContacts.AuthorityVersion -- i.e. the same freshness
// guard CompleteNativeRepeatCombatNoAttack already enforces before
// accepting, moved earlier into the routing decision itself so a later,
// unrelated timeout is never misrouted (and never throws) in the first
// place. AppendSelectionNoAttack now calls this instead of the bare
// IsNativeRepeatCombatVoluntaryFinish.
//
// Building a full integration session that drives two separate Combats
// through repeat cycles end-to-end is the documented "honest gap" in this
// codebase (no integration test drives ExerciseSession/CampaignExercises
// mechanics directly yet). As in ExerciseSessionPendingTests.cs and (the
// in-flight, not-yet-merged) StaleSecondAssaultGuardTests.cs, this test
// instead places a bare session (via reflection) into exactly the shape the
// bug describes -- a stale, already-finished repeat selection plus a
// contact-authority version that has moved on past it -- and proves the
// scoped predicate, unlike the bare one, correctly reports "not pending".
public sealed class StaleRepeatCombatSelectionGuardTests
{
    private static ExerciseSession NewBareSession() =>
        (ExerciseSession)RuntimeHelpers.GetUninitializedObject(typeof(ExerciseSession));

    // Some ExerciseSession Current* properties are get-only auto-properties
    // (no setter at all, only ever assigned from the constructor), so this
    // sets the compiler-generated backing field directly rather than going
    // through the property, which works for both getter-only and
    // getter/private-setter properties alike.
    private static void SetCurrent(ExerciseSession session, string propertyName, object? value)
    {
        var field = typeof(ExerciseSession).GetField($"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(nameof(ExerciseSession), propertyName);
        field.SetValue(session, value);
    }

    private static CampaignNativeRepeatCombatDecisionV31 FinishedRepeatSelection(long priorStateVersion)
    {
        var constructor = typeof(CampaignNativeRepeatCombatDecisionV31).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            [
                typeof(int), typeof(string), typeof(long), typeof(string), typeof(int),
                typeof(string), typeof(string), typeof(string), typeof(LandSide),
                typeof(string), typeof(string), typeof(string), typeof(string), typeof(string),
            ])
            ?? throw new MissingMemberException(nameof(CampaignNativeRepeatCombatDecisionV31), "ctor");
        return (CampaignNativeRepeatCombatDecisionV31)constructor.Invoke(
        [
            CampaignNativeRepeatCombatDecisionV31.CurrentContractVersion,
            "campaign-1",
            priorStateVersion,
            "sha256:" + new string('0', 64),
            2,
            "position-a",
            "position-b",
            "finish-combat-without-attack",
            LandSide.Axis,
            "element-1",
            null,
            null,
            null,
            "action-1",
        ]);
    }

    // Advances empty contact authority independently of the repeat selection's StateVersion.
    private static CampaignContactJournal ContactsAtVersion(long authorityVersion)
    {
        var journal = CampaignContactJournal.InitializePregameSource("campaign-1");
        while (journal.AuthorityVersion < authorityVersion)
            journal = journal.AdvancePregameSource(journal.AuthorityVersion);
        Assert.Equal(authorityVersion, journal.AuthorityVersion);
        return journal;
    }

    [Fact]
    public void BareVoluntaryFinishReadIsStaleAcrossAnUnrelatedLaterCombat()
    {
        // The repeat selection finished at StateVersion 6 (PriorStateVersion
        // 5); the session has since moved on -- a later, unrelated Combat's
        // own Selection round has advanced contact authority to version 40 --
        // but CurrentRepeatCombatSelection was never reset.
        var session = NewBareSession();
        SetCurrent(session, "CurrentRepeatCombatSelection", FinishedRepeatSelection(priorStateVersion: 5));
        SetCurrent(session, "CurrentContacts", ContactsAtVersion(40));

        // This is exactly the stale read AppendSelectionNoAttack used to
        // route on: it has no idea the decision is from an earlier Combat.
        Assert.True(CampaignHostAdapter.IsNativeRepeatCombatVoluntaryFinish(session));
    }

    [Fact]
    public void ScopedPendingCheckIgnoresTheStaleRepeatSelectionOnceContactAuthorityHasMovedOn()
    {
        var session = NewBareSession();
        SetCurrent(session, "CurrentRepeatCombatSelection", FinishedRepeatSelection(priorStateVersion: 5));
        SetCurrent(session, "CurrentContacts", ContactsAtVersion(40));

        // Fixed: the scoped check requires the stale decision's StateVersion
        // (6) to match the live authority version (40). It does not, so this
        // correctly reports "no repeat no-attack is pending" and
        // AppendSelectionNoAttack now falls through to
        // CompleteNativeCombatNoAttack for the unrelated Combat instead of
        // misrouting to CompleteNativeRepeatCombatNoAttack.
        Assert.False(CampaignHostAdapter.IsNativeRepeatCombatNoAttackPending(session));
    }

    [Fact]
    public void ScopedPendingCheckStillRecognizesAGenuinelyCurrentRepeatSelection()
    {
        // Same shape, but the contact authority has NOT moved on: this is
        // the genuine case the repeat path exists for, and must still work.
        var session = NewBareSession();
        SetCurrent(session, "CurrentRepeatCombatSelection", FinishedRepeatSelection(priorStateVersion: 5));
        SetCurrent(session, "CurrentContacts", ContactsAtVersion(6));

        Assert.True(CampaignHostAdapter.IsNativeRepeatCombatNoAttackPending(session));
    }
}
