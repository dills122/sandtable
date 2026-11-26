# Maintainability refactor: completion summary (2026-11-24)

> **2026-11-26 follow-up:** This is a dated baseline, not the current implementation ledger.
> #640, #645, #648–#650 and #658 cover bounded synthetic Stage 2/3 HTTP progression and real
> Combat rounds; #646, #651 and #652 prove Turn 1 through the next turn’s open Reserve.
> Turn 2 acceptance and later turns, native Reaction (#655) and executor migration remain open.
> See the [current roadmap](../../roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates).

**Status:** every slice in `2026-11-24-maintainability-refactor-direction.md` is shipped
and merged. This doc records what actually landed, against that plan's Section 5
sequencing table, so the direction doc doesn't need to be re-read to answer "did this
finish."

## Gate

| PR | Title |
|---|---|
| #551 | fix(core): guard pre-existing unguarded CurrentSecondAssault reads against staleness |
| #552 | fix(core): guard all six unguarded ComputePending() second-side arms [CRITICAL] |

Both merged before any slice touched `ExerciseSession.cs`/`CampaignExercises.cs`/
`CampaignHostAdapter.cs`, per the gate.

## Slices

| Slice | Scope | PR | Notes |
|---|---|---|---|
| 1 | `CampaignNativeSecondBreakdownV46` `Slot` extraction | #560 | New regression test added first (`Create` had zero prior coverage) |
| 2 | `CampaignExercises` Movement/Reserve `Slot` extraction | #562 | `QuerySecondReserveSource` deliberately left hand-written (out of scope) |
| 2b | Test-fixture + catch-block dedup | #564 | 63 catch-filter sites collapsed (not 15/49 as earlier estimated — file had grown); 9 test files share new `ExerciseSessionTestHarness` |
| 3a | `CampaignSourceAuthority` ordered-chain registry | #557 | `AdmissionSteps.cs`, additive only |
| 3b | Delete old `CampaignSourceAuthority` triads onto 3a | #563 | 27 methods collapsed (not ~16-20 estimated); `StepByKind` + `XxxCore()` split added to avoid the registry's Accept delegate recursing into the public entry point |
| 4a | `CampaignSourceBattlefield` generic quadruple | #556 | 4 of the 5 originally-estimated groups fit cleanly |
| 4b | Delete old battlefield-group methods onto 4a | #561 | FourthIndian/Catanzaro/SeventhArmored/SixteenthBrigade; Libyan/Matruh/ItalianDivisions correctly left hand-written as structural outliers |
| 5 | `CampaignDurableAuthorityJournalStore` split + registry | #559 | Split into `.cs` (1100 lines) + `.Openings.cs` (526 lines) |
| 6 | `CampaignHostAdapter` forwarder deletion | #566 | 37 forwarders deleted (not ~30 estimated); real caller set differed from the doc's guess (doc's "7 `*Rounds.cs` files" don't call these methods — actual callers were `CampaignHostAdapter`'s own replay switch + `CampaignDurableAuthorityJournal.SourceAdmission.cs` + a migration tuple array) |

Every slice's estimate (method counts, caller sets) drifted somewhat from the direction
doc's numbers once independently re-verified by the agent doing the work — expected, since
the doc itself was written same-day as a point-in-time audit, not as a literal spec.
Nothing here required a correction, PR #565 included (see below).

## One slice candidate the direction doc raised but correctly never scheduled

**switch-exhaustiveness hardening** (not in the original 9-slice table — proposed later,
after the 4 "morning swarm" slices landed) turned out to be a dead end: both switches on
`PendingDecision` in `CampaignExercises.cs` are switch *statements*, already fully
exhaustive over all 29 sealed-record subtypes. Converting to switch *expressions* (the
intended hardening) hits `CS8509` unconditionally — a confirmed Roslyn limitation where
switch-expression completeness analysis doesn't recognize sealed record hierarchies as
closed, regardless of case coverage. PR #565 shipped a comment-only explanation next to
each switch instead of a functional change, so this isn't re-attempted. The silent-
fallthrough gap this was meant to close (a missing case in a switch *statement* compiles
clean today) is still open; a reflection-based test asserting subtype-count == case-count
is flagged there as a safe follow-up, not scheduled.

## What's not part of this plan (unchanged, confirmed still true)

- The pre-#394 Combat/Reserve-Release/Cycle-Control ternary cascade (9 call sites) —
  explicitly out of scope per the overhaul doc's own Section 0.6 step 5.
- `CampaignHostAdapter.cs`'s 20 `NativeXxxCommand` nullable-branching methods — tracked as
  future `PendingDecision` consumers, not this plan's concern.

## Verification

Every slice ran the full `Cna.Core.Tests` suite (4163-4166/4166 passed depending on which
slice's test additions were present at the time) in addition to targeted tests, per the
direction doc's own Section 6 callout that a static byte-identical check is not proof.
`Cna.OrleansHost.Tests` failures seen during agent verification (Docker-unavailable in the
sandbox worktrees) are a sandbox-environment gap, not a regression — confirmed against
CI's own `orleans` job, which has Docker and passed clean on every merged PR.

## Still open (unrelated to this plan, carried from release-readiness tracking)

- Stage 3 and first rollover were open at this plan's close; now bounded synthetic HTTP
  paths reach open Turn 2 Reserve (#652, #658). Acceptance and further looping remain open.

Neither was in scope for this round; noted here only so this summary doubles as a current
pointer, not because this plan touches them.
