# `Reconstruct`/`ReconstructOne` cascade ordering audit (2026-11-23)

**Scope of this audit:** `CampaignExercises.ReconstructOne`
(`src/Cna.Core/Exercises/CampaignExercises.cs:9134-10840`, the real body behind
the public `Reconstruct` entry point), systematically checked against
`QueryCheckpoint` (`:4098-4324`) as ground truth for the correct
most-recent-first ordering. Read-only; no source changes made as part of this
audit.

**Method.** `QueryCheckpoint` was already fully audited and fixed earlier this
session, so its branch order is the trusted reference for "which
`ExerciseSession` field is chronologically later than which." Every
early-return `if (completedSession.CurrentX is {...})` branch in
`ReconstructOne`'s first ~1,000 lines (9134-10130) was checked against that
reference. The function's remaining ~700 lines (10131-10840) are a
structurally different, single linear accumulation (the original pre-`#394`
Combat/Reserve-Release chain, `CurrentCombatDecision` through
`CurrentPostRepeatEvents`) — not a "first-match-wins" dispatch over parallel
never-reset fields, so it is not vulnerable to this bug shape by construction
and was confirmed, not assumed, to be unchanged from `QueryCheckpoint`'s own
(already-correct) tail.

## Finding 1 (already fixed, cited for completeness)

`CurrentSecondAssault` (`:9446`) intercepting `CurrentInitiativeDeclaration`
(`:9936`) — this is the instance the Stage 2 HTTP wiring work (PR #543)
already found and fixed. Confirmed present on current `main` (PR #543 not yet
merged), consistent with its own report.

## Finding 2 — confirmed, NOT yet fixed: `StageEntrySecond` group internally misordered

**Location:** `CampaignExercises.cs:9145-9381`.

**The bug.** `QueryCheckpoint`'s correct order for this stage's second acting
side (`:4109-4135`) is: `CurrentStageEntrySecondAssault` →
`CurrentStageEntrySecondNoAttackCompletion` →
`CurrentStageEntrySecondCombatDecision` → `CurrentStageEntrySecondBreakdown` →
`CurrentStageEntrySecondMovementCompletion` → `CurrentStageEntrySecondMove` →
`CurrentStageEntrySecondReserve` (most-recent-first).

`ReconstructOne`'s actual order is: `CurrentStageEntrySecondBreakdown`
(`:9145`) → `CurrentStageEntrySecondMovementCompletion` (`:9180`) →
`CurrentStageEntrySecondMove` (`:9214`) → `CurrentStageEntrySecondReserve`
(`:9254`) → `CurrentStageEntrySecondAssault` (`:9293`) →
`CurrentStageEntrySecondNoAttackCompletion` (`:9323`) →
`CurrentStageEntrySecondCombatDecision` (`:9350`).

`CurrentStageEntrySecondBreakdown` is checked *before* the three fields that
are chronologically later than it (`Assault`/`NoAttackCompletion`/
`CombatDecision`), not after, as the comment at `:9138-9144` itself claims
("Chronologically latest in this function... mirroring QueryCheckpoint's own
ordering") — that claim is simply wrong; it only compared itself against
`MovementCompletion` below it, not against the `Assault`/`NoAttackCompletion`/
`CombatDecision` block three groups later.

**Concrete failure scenario.** A session that has progressed past this
stage's second-side Breakdown all the way to Combat Decision or Assault still
has `CurrentStageEntrySecondBreakdown` non-null (fields never reset). Calling
`Reconstruct` on that session hits the `CurrentStageEntrySecondBreakdown`
branch first (`:9145`), replays and verifies *only* the Breakdown step
(which still matches, since Breakdown genuinely happened), and returns a
`None`-failure, fully-verified result — without ever checking that Combat
Decision/Assault/No-Attack also replay correctly. This is a silent
*under*-verification, not a crash or a wrong-data corruption: a cold
restart/replay of any such campaign would falsely report itself fully
verified while the real chronological tail of the session (Combat Decision
onward) was never re-checked.

**Likelihood: high, not theoretical.** This is the ordinary path for this
stage's second acting side once MTS-TASK-006 ever gets Host/HTTP wiring
(not yet attempted) — any real campaign that completes Breakdown and moves on
to Combat will trip this on its very first cold restart.

## Finding 3 — confirmed, NOT yet fixed, highest-severity: six-mechanic preamble chain checked in forward (not reverse) chronological order

**Location:** `CampaignExercises.cs:9936-10130`.

**The bug.** Every other group in this cascade is ordered most-recent-first.
This one group — the six Stage 1→2 entry-bridge preamble mechanics — is
ordered **earliest-first**: `CurrentInitiativeDeclaration` (`:9936`) →
`CurrentStageEntryWeather` (`:9969`) → `CurrentNoObligationOrganization`
(`:10004`) → `CurrentNoObligationNavalConvoyArrival` (`:10035`) →
`CurrentNoObligationFleetAssignment` (`:10067`) →
`CurrentNoObligationFleetRepair` (`:10099`).

The strict chronological succession is confirmed directly from each event's
own `Create()` call, each of which takes the *previous* mechanic's field as
its required input: `WeatherDetermination.Create` takes
`predecessor.CurrentInitiativeDeclaration!` (`:9982`);
`NoObligationOrganization.Create` takes `predecessor.CurrentStageEntryWeather!`
(`:10014`); `NoObligationNavalConvoyArrival.Create` takes
`predecessor.CurrentNoObligationOrganization!` (`:10046`);
`NoObligationFleetAssignment.Create` takes
`predecessor.CurrentNoObligationNavalConvoyArrival!` (`:10078`);
`NoObligationFleetRepair.Create` takes
`predecessor.CurrentNoObligationFleetAssignment!` (`:10110`). So the real
temporal order is Initiative → Weather → Organization → Convoy Arrival →
Fleet Assignment → Fleet Repair, and the cascade checks them in exactly that
order — the *opposite* of what the most-recent-first convention requires.

**Concrete failure scenario.** Since none of these fields ever reset, a
session that has progressed all the way to `CurrentNoObligationFleetRepair`
(the last of the six) still has `CurrentInitiativeDeclaration` non-null too
(the first of the six, set long before). `ReconstructOne` checks
`CurrentInitiativeDeclaration` first and always matches it, so **no campaign
can ever have its replay verified past the very first of these six
mechanics** — Weather, Organization, Naval Convoy Arrival, Fleet Assignment,
and Fleet Repair's own replay correctness can never be reached by this
function, for any session, under any circumstance. This is the most severe
finding in this audit: it is not conditional on a particular play sequence,
it is unconditionally broken for every session that reaches past Initiative
Declaration.

**Likelihood: certain, and imminent.** PR #543 (Stage 2 Initiative Declaration
HTTP wiring) explicitly deferred "the five automatic preamble mechanics' own
auto-drain wiring" as its very next follow-up slice. The moment that slice
lands and any real campaign auto-drains through Weather/Organization/Convoy
Arrival/Fleet Assignment/Fleet Repair, this bug fires on the first cold
restart of that campaign. This should be fixed **before**, not alongside or
after, that next slice.

## Summary

| # | Fields involved | Status | Severity |
|---|---|---|---|
| 1 | `CurrentSecondAssault` vs `CurrentInitiativeDeclaration` | Fixed on PR #543 (unmerged) | High (already remediated) |
| 2 | `StageEntrySecond` group internal order | **Not fixed** | High — silent under-verification once Stage 2's second-side wiring lands |
| 3 | Six-mechanic preamble chain order | **Not fixed** | **Critical** — unconditionally blocks all replay verification past Initiative Declaration, for every session, the moment the next planned slice (auto-drain wiring) lands |

**Recommendation:** fix Findings 2 and 3 before the preamble auto-drain
wiring slice, not after. Both are mechanical reordering fixes (move the
correct branches earlier in the cascade, same pattern as `QueryCheckpoint`'s
own fix) — no new design question, no `PendingDecision` refactor required to
unblock them specifically, though the `PendingDecision` refactor (overhaul
doc, Section 0) remains the structural fix that makes this entire bug class
impossible going forward rather than requiring a fourth, fifth, and sixth
manual audit as more mechanics are added.
