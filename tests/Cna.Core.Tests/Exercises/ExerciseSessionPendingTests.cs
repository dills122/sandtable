using System.Reflection;
using System.Runtime.CompilerServices;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;

namespace Cna.Core.Tests.Exercises;

// Direct proof that the 2026-11-23 Reconstruct cascade ordering audit's
// Findings 2 and 3 (docs/work/plans/2026-11-23-reconstruct-cascade-ordering-audit.md)
// stay fixed now that QueryCheckpoint and ReconstructOne both dispatch off the
// same derived ExerciseSession.Pending value instead of two independently
// hand-ordered cascades, so they can no longer drift apart from each other.
// This does NOT make the ordering-discipline bug class structurally
// impossible outright -- ComputePending() itself (ExerciseSession.cs) is
// still a manually-ordered cascade; that is only eliminated by the overhaul
// doc's (docs/work/plans/2026-11-23-codebase-overhaul-direction.md, Section
// 0.3) step-3 type-keyed handler registry, not built here.
//
// Both findings are "two never-reset fields are simultaneously non-null, and
// the chronologically EARLIER one was (wrongly) checked first" bugs. Building
// a full real campaign session that reaches these states end-to-end is the
// documented "honest gap" this codebase already flags (no integration test
// drives ExerciseSession/CampaignExercises mechanics directly yet -- see
// docs/work/plans/2026-11-22-mts-task-002-slices-2-3-and-wiring.md). These
// tests instead exercise ExerciseSession.Pending directly: they place the
// session into exactly the "two fields set at once" shape each finding
// describes (via reflection, since only WithXxx can set these in production
// code, and only Pending's *ordering*, not the fields' domain content, is
// under test here) and assert the derived Pending resolves to the
// chronologically later mechanic.
public sealed class ExerciseSessionPendingTests
{
    private static ExerciseSession NewBareSession() =>
        (ExerciseSession)RuntimeHelpers.GetUninitializedObject(typeof(ExerciseSession));

    private static T NewBareRecord<T>() where T : class =>
        (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static void SetCurrent(ExerciseSession session, string propertyName, object? value)
    {
        var property = typeof(ExerciseSession).GetProperty(propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(nameof(ExerciseSession), propertyName);
        property.SetValue(session, value);
    }

    // Finding 2: "StageEntrySecond group internally misordered" --
    // CurrentStageEntrySecondBreakdown was checked before the chronologically
    // later CurrentStageEntrySecondCombatDecision (and Assault/NoAttack). A
    // session that has progressed past second-side Breakdown to Combat
    // Decision still has both fields non-null (fields never reset).
    [Fact]
    public void PendingResolvesToTheLaterCombatDecisionNotTheEarlierBreakdown()
    {
        var session = NewBareSession();
        SetCurrent(session, "CurrentStageEntrySecondBreakdown",
            NewBareRecord<CampaignNativeSecondBreakdownV46>());
        SetCurrent(session, "CurrentStageEntrySecondCombatDecision",
            NewBareRecord<CampaignNativeSecondCombatDecisionStateV47>());

        Assert.IsType<StageEntrySecondCombatSelectionPending>(session.Pending);
    }

    // Finding 3 (CRITICAL): the six-mechanic preamble chain was checked
    // earliest-first (InitiativeDeclaration first) instead of
    // most-recent-first. A session that has progressed all the way to
    // NoObligationFleetRepair (the last of the six) still has
    // CurrentInitiativeDeclaration (the first of the six) non-null too.
    [Fact]
    public void PendingResolvesToTheLatestPreambleMechanicNotTheEarliestInitiativeDeclaration()
    {
        var session = NewBareSession();
        SetCurrent(session, "CurrentInitiativeDeclaration",
            NewBareRecord<CampaignNativeInitiativeDeclarationV49>());
        SetCurrent(session, "CurrentNoObligationFleetRepair",
            NewBareRecord<CampaignNativeNoObligationFleetRepairV54>());

        Assert.IsType<NoObligationFleetRepairPending>(session.Pending);
    }

    // Same preamble chain, a step short of Finding 3's full span: confirms
    // the whole six-mechanic order is most-recent-first, not just the two
    // endpoints.
    [Fact]
    public void PendingResolvesToTheLatestPreambleMechanicAmongAnyPrefix()
    {
        var session = NewBareSession();
        SetCurrent(session, "CurrentInitiativeDeclaration",
            NewBareRecord<CampaignNativeInitiativeDeclarationV49>());
        SetCurrent(session, "CurrentStageEntryWeather",
            NewBareRecord<CampaignNativeWeatherDeterminationV50>());
        SetCurrent(session, "CurrentNoObligationOrganization",
            NewBareRecord<CampaignNativeNoObligationOrganizationV51>());

        Assert.IsType<NoObligationOrganizationPending>(session.Pending);
    }

    // Sanity check: with nothing #394-onward set, Pending is null and both
    // cascades fall through to the untouched pre-#394 chain.
    [Fact]
    public void PendingIsNullWhenNoPostHash394MechanicIsSet()
    {
        var session = NewBareSession();

        Assert.Null(session.Pending);
    }
}
