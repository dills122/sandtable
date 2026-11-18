# Design proposal: second-side Combat continuation (#394)

**Status:** proposal, not a decision. Lays out options with tradeoffs; does not
pick one. Re-verified against current HEAD (`bb2c0f57`) on 2026-11-18 — all
citations below point at real, current code, not a stale snapshot.

## Background

Issue #394 ("Connect second-side Breakdown to actual Combat selection") is a
confirmed RC authority-continuation gap. PR #400 (merged) reproduced the
actual failure directly with a throwaway test and found it is not one bug but
four independent gaps in the same area, and explicitly declined to fix any of
them: "this touches central, correctness-critical Combat state-machine
plumbing in at least 4 independent places... needs an explicit design
decision before any of it is safe to implement." This document is that
design decision's input — the four gaps, concrete options for each, and an
honest risk assessment, so the actual choice can be made deliberately rather
than by whichever option happens to get coded first.

Nothing here changes code, contracts, or readiness status.

## Gap 1: `CurrentCombatDecision` never clears across the second-side boundary

`ExerciseSession.CurrentCombatDecision` (`src/Cna.Core/Exercises/ExerciseSession.cs:862`)
is a get-only field, set exactly once in the constructor
(`:446`, `CurrentCombatDecision = combatSelection;`). Every `With*` method in
the native second-side V41–V46 family —
`WithSecondBreakdown` (`:760-774`), `WithSecondMovementCompletion` (`:777-790`),
`WithSecondMove` (`:793-809`), `WithSecondReserve` (`:812-828`), and their
siblings `WithPostCombatPass`/`WithTruckConvoyPass` — builds its successor via
`(ExerciseSession)MemberwiseClone()` and never touches this field. So once any
first-side Combat decision has ever been made in a session's history,
`CurrentCombatDecision` stays permanently non-null for the rest of that
session's life, including through the entire V41–V46 second-side chain, even
though no second-side Combat decision has actually been made yet.

This is not an isolated oversight — it is how every field in this class
works. A grep of `ExerciseSession.cs` finds 56 `{ get; }` / `{ get; private
set; }` fields; none of them is ever explicitly reset to null once set. The
class's entire design is monotonic accumulation of history, not a state
machine with resettable phases. `CurrentCombatSuccessor` (`:884`), which might
look like a "combat is now concluded" signal, follows the exact same
forward-only pattern (confirmed: `:467,495,523,553,...` all just forward
`CurrentCombatSuccessor` unchanged) — so there is no existing field anywhere
in the class that already means "first-side Combat fully resolved, safe to
start fresh." PR #400's "no existing precedent" finding (for gap 3) applies
here too.

### Option A: explicit reset at a transition point

Add a real setter and clear `CurrentCombatDecision` (and whichever of its
~15 sibling Combat fields — `CurrentCombatBarrage`, `CurrentCombatSettlement`,
`CurrentCombatCustodyState`, etc. — also need clearing) at the specific point
where first-side Combat fully resolves and the second-side native chain
begins.

**Risk:** a grep of `CurrentCombatDecision` outside `ExerciseSession.cs` finds
over 70 usages across `CampaignExercises.cs` and `CampaignHostAdapter.cs`,
nearly all of them gating the *entire* first-side resolution chain —
selection, barrage, retreat, custody, settlement, successor — on this field
staying non-null throughout. This option requires identifying the exact
correct moment to clear it: too early and some first-side query/gate that
currently assumes it's still set will break; too late and the bug persists.
There is no existing field to key that moment off (per above), so the clear
point itself would need to be newly defined and justified — it does not fall
out of an existing signal. This would also be the first field in this class
ever given real reset semantics, which is a real precedent change to the
class's design, not just a bug fix.

### Option B: split into side-scoped fields

Add a second field (e.g. `CurrentSecondCombatDecision`) instead of reusing
`CurrentCombatDecision` for both sides. The existing field and its ~70 call
sites keep their exact current behavior, untouched. New second-side logic
(gaps 2–4) reads/writes the new field instead.

**Risk:** touches far less of the existing, heavily-depended-on first-side
logic (zero behavior change there), but every one of the ~70 call sites that
currently branches on `CurrentCombatDecision` needs to be checked for whether
it should also consider the new second-side field once second-side Combat is
reachable — not changed, but *audited*. The actual code size of this audit is
smaller than it sounds: `QueryContactOwnerMovementCut`/`QueryContactOwnerCombatCut`
(gap 2) already take a `side`/`actingElementId` parameter, so the gates
themselves can select the correct field without new plumbing; the deeper risk
is the ~15 downstream Combat-resolution fields (barrage/retreat/custody/
settlement) that assume a single combat-in-progress shape — those would need
their own second-side equivalents too if second-side Combat can reach the
same depth as first-side Combat currently does (e.g. does the V41–V46 chain
need its own barrage/custody/settlement path, or does it reuse the existing
one once CurrentSnapshot is bridged? Gap 3 and this question are coupled).

This also matches an existing pattern in the class better than Option A does:
the V41–V46 family already keeps side-scoped parallel fields next to
first-side equivalents (e.g. `CurrentSecondReserve`/`CurrentSecondReserveWorld`
alongside `CurrentCombatDecision`'s broader first-side family) rather than
reusing and clearing a shared field.

## Gap 2: the two throwing gates

`QueryContactOwnerMovementCut` (`CampaignExercises.cs:3901-3909`) and
`QueryContactOwnerCombatCut` (`:3911-3919`) both throw
`"Combat selection has already advanced this authority cut."` whenever
`session.CurrentCombatDecision is not null` — which, per gap 1, is always true
once any first-side combat has ever occurred, regardless of whether a
second-side decision actually exists yet. This is the exact gate PR #400's
reproduction hit (`ATTEMPT1`/`ATTEMPT2` in its captured output).

- **Under Option A:** once gap 1's reset lands at the right point, these two
  gates need no change at all — the field is genuinely null again by the time
  a second-side query runs, and the existing check is correct as written.
- **Under Option B:** both gates need their condition rewritten to check the
  field appropriate to the side being queried (first-side field for a
  first-side query, second-side field for a second-side query) rather than
  the current single unconditional check. Since both methods already receive
  `side`, this is a localized, mechanical change to these two methods — the
  real risk under Option B is elsewhere (gap 1's downstream audit), not here.

## Gap 3: `CurrentSnapshot` is never bridged to V46's world/position

`CurrentSnapshot` (`ExerciseSession.cs:739-740`) is backed by a private field
set once at session construction and never reassigned by any V41–V46 method —
confirmed by reading all of `WithSecondBreakdown`/`WithSecondMovementCompletion`/
`WithSecondMove`/`WithSecondReserve` in full; none of them touch it. It stays
frozen at the first-acting-side's old Combat position forever, exactly as
PR #400's reproduction showed (`CurrentSnapshot.PositionId` pointing at
`first-player.../combat.position-determination` while the V46 checkpoint
itself correctly reports `second-player.../combat.position-determination`).

This is a real type gap, not just a missing assignment: `CurrentSnapshot` is
`CampaignSnapshotV11` (the full canonical snapshot type consumed throughout
the session), while the V41–V46 chain tracks a narrower
`CampaignWorldSnapshotV6` (`CurrentSecondMoveWorld`, `CurrentSecondReserveWorld`).
There is no existing method anywhere in this codebase that converts/projects
a `CampaignWorldSnapshotV6` plus a V46 position into a new
`CampaignSnapshotV11` — confirmed by PR #400's own "no existing precedent"
finding and not contradicted by anything found in this pass.

Concretely, closing this gap requires either:
- a new projection method (shape TBD — likely something like
  `CampaignSnapshotV11` construction from `(CampaignWorldSnapshotV6, V46
  position/side data, existing Context)`, mirroring however
  `CampaignSnapshotV11` is normally constructed elsewhere in the codebase —
  that normal construction path would need to be read closely by whoever
  picks this up, since it is not covered by this scoping pass), or
- changing `WithSecondBreakdown` (or an earlier V41–V46 step) to also compute
  and assign a new `CurrentSnapshot` value as part of its clone, once the
  rest of the required inputs are available.

This is the gap with the least existing precedent to build on and the
highest remaining uncertainty — whoever implements it should expect to need
one more close read of `CampaignSnapshotV11`'s normal construction
requirements before committing to a specific method shape. This document
does not attempt to resolve that; it is flagged as a known open question
rather than guessed at, per this project's "don't invent a value" discipline.

## Gap 4: Host-side missing successor-round initialization

`CampaignDurableAuthorityJournal.Submissions.cs`'s `SecondMovementCompletion`
branch (`:780-792`) calls `CampaignHostAdapter.CompleteSecondBreakdown` and,
if accepted, `PersistAccepted` — and stops. Every sibling branch in the same
`if` chain that completes a round initializes whatever comes next: `Movement`
calls `TryInitializeSelectionRound` (`:765-766`); `RetreatBeforeAssault`
calls `InitializeForceRound` (`:770-771`); `Custody` calls
`AppendNativeCombatSuccessor` (`:796`). `SecondMovementCompletion` has no
analogous call.

This gap looks mechanical — match whichever sibling pattern turns out to be
the right analog (`TryInitializeSelectionRound`-style vs.
`AppendNativeCombatSuccessor`-style; determining which is itself a small
follow-up read once gaps 1–3's shape is picked, since the right call depends
on what a "second-side Selection round" actually needs to look like).

**Is it independently fixable?** Mechanically, yes — it's a self-contained
addition to one `if` branch. But it is **not independently *testable* today**:
per gap 2, any owner action that would reach this code path is currently
rejected by the stale `CurrentCombatDecision` check before it ever gets here.
PR #400's reproduction confirms this ordering directly ("Two independent
`FirstActingSide`-only gates and a missing Host... wiring are real but never
even reached, since the stale-state check fires first"). So this gap can be
coded in isolation, but proving it correct — per issue #394's own acceptance
criteria ("exercise owner opening, legal finish/selection, atomic persistence,
cold replay and retry through the normal durable Host") — requires gaps 1–3
to land first, or at minimum a throwaway/targeted test that bypasses the
normal gates the way PR #400's reproduction did. Recommend bundling this with
1–3 into one implementation pass rather than shipping it alone untested.

## Recommendation

Not forcing a pick where the evidence is genuinely close, but there is one
real asymmetry worth naming: Option A would be the first field in this
56-field, append-only-history class ever given explicit reset/mutation
semantics — it breaks an invariant the entire class is currently built
around, and that invariant is almost certainly load-bearing for the
determinism/replay guarantees this project cares about elsewhere (hash-chain
verification, cold replay, etc.). Option B's cost is a wider *audit* surface
(check ~70 sites, change few), while Option A's cost is a narrower but deeper
*correctness* risk (one new reset point, but it has to be exactly right, and
nothing in the class currently helps prove that it is). On that basis, Option
B looks like the lower-risk default — but this is a judgment call about
risk *shape*, not a claim that Option A is unworkable, and the actual choice
should also weigh implementation effort, which this pass did not attempt to
estimate in engineer-hours for either option.

Whichever option is chosen, gap 3 (the snapshot bridge) needs its own close
read of `CampaignSnapshotV11` construction before a method shape can be
committed to, independent of the gap 1 choice. Gap 4 is mechanical and should
be bundled with whichever of 1–3 lands, not shipped alone.
