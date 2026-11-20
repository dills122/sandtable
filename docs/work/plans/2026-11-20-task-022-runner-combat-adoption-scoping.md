# Tasks 022–024 — Runner Combat adoption: scoping and frontier evidence (2026-11-20)

**Status:** author scoping plus one test-only evidence slice (022A0). No independent review has run.

**This is test-harness work only.** It is not Combat activation, certification or a product feature,
and it does not depend on Task 021. Task 021's capability manifest protects ordinary production
Core authority. `Cna.ExerciseRunner` is a separate CLI-only, deterministic, rules-lab lane
([exercise harness](../../specs/exercise-harness-v1.md)). Nothing here claims Combat is "Ready",
"certified" or "activated". Tracks [#448](https://github.com/dills122/sandtable/issues/448).

**Outcome of this slice:** the Runner still stops at Combat entry. Driving it through Combat with
only the public Combat audience surface is blocked by Core API gaps (§4). Closing them is Core work
that another agent owns, so this slice reports them and does not patch Core. Owner decision needed
(§5).

## 1. What the plan actually assigns to 022–024

Source: [combined plan](../../design/combat-cycle-implementation-plan.md), task table.

| Task | Plan output (abridged) | Paths |
| --- | --- | --- |
| 022 | Extend **opaque Core Exercise authority** with the same public actions and an occurrence-aware terminal; exactly one semantic event per accepted step; reconstruct and re-adjudicate through Core, no private test hook in production. | `src/Cna.Core/Exercises/`, Core Exercise tests |
| 023 | **Runner** adopts the decisions and terminal; strict child bundle readback; step-limit/cancellation/invariant failure stays failure. | `src/Cna.ExerciseRunner/`, Runner tests |
| 024 | Maneuver/paired parent evidence, first divergence, two clean runs. | Runner execution, Runner tests |

Correction to the dispatch premise: **022 is a Core change** (`CampaignExercises`), not Runner
wiring. 023 is the Runner adoption and depends on 022. `CMB-CON-006` also requires a versioned
occurrence/ordinal/position/obligation terminal that rejects unsupported terminals before running.

## 2. How the Runner works today

- `ExerciseExecutor.Execute` loop: `QueryCheckpoint` → success only when `PositionId` equals the
  manifest's `terminalBoundary` (then reconstruction) → `CampaignExercises.Query` for System, Axis,
  Commonwealth in fixed order → `ExerciseController.Select` (exactly one active audience; the policy
  picks a candidate by kind and action ID) → `CampaignActionSubmission` by action ID →
  `CampaignExercises.Submit` → checks: exactly one event, checkpoint continuity, receipt bound to the
  submitted audience and action ID → append step, continue with the successor session.
- Controller input is `ExerciseControllerCandidate` (contract v3): action ID, kind, and semantic data
  only for `move-element` / `designate-reserve`. Policies are kind-based (`first-by-action-id`,
  matrix, reaction).
- `ReadjudicationVerifier` replays the transcript's action IDs through `CampaignExercises`.
- The Runner reaches Core only through public API (`Cna.Core.Tests` is the only friend assembly;
  `AuthorityBoundaryTests.OnlyTestsReceiveCoreFriendAccess`).

**Why it stops.** At `combat.position-determination`, `CampaignExercises.Query` returns an empty set
for every audience, so the controller reports `no-active-audience` and the run fails
`no-unique-legal-action`. After any Combat decision is accepted, `CampaignExercises.Query` returns
`Rejected(InvalidState)` (`session.CurrentCombatDecision is not null`), which the Runner would treat
as an authority-query invariant failure.

## 3. Evidence (022A0, `CombatFrontierEvidenceTests`)

Runs from `Cna.ExerciseRunner.Tests`, which has no Core friend access, so it sees exactly what a
Runner sees. Manifests are built in the test (no checked scenario added): `reaction-all-by-action-id`
for all audiences, `rootSeed` 6, unreachable terminal, 200-step cap.

| Setup | Runner result | Frontier | Legal actions | Public audience decisions found |
| --- | --- | --- | --- | --- |
| `combat-assault` | `no-unique-legal-action` after 18 steps | `combat.position-determination` | 0 / 0 / 0 | `combat-selection` (Axis) for `axis-infantry`, `axis-truck` |
| `combat-double-fresh` | `no-unique-legal-action` after 19 steps | `combat.position-determination` | 0 / 0 / 0 | `combat-selection` (Axis) for `axis-infantry`, `axis-infantry-second`, `axis-truck` |

Decisions were found by probing every stage × audience × World element (plus the two fixed
subjects). No other stage opens at this cut.

`PublicAudienceSubmissionIsAcceptedButGivesTheRunnerNoSuccessorOrEvidence`: a public Selection
submission is accepted and returns `{"contractVersion":1,"outcome":"accepted","offerRef":…}`. The
result's only public properties are `IsAccepted` and `AcceptedRef`. The Runner's session is unchanged
(same state version, same position, byte-equal decision on re-query).

**Milestone status: not reached.** No Runner run progresses past `combat.position-determination`.

## 4. Findings (stop-and-report)

| ID | Gap | Why the Runner cannot work around it without a design call |
| --- | --- | --- |
| T022-Q1 | **No successor from a public submission.** `CampaignCombatAudienceSubmissionResult.TrustedStep` is internal, and 020D pins that (`AuthorityBoundaryTests`: no `TrustedStep` property, no public constructor). `ExerciseSession` is immutable. | Without the successor session, receipt, event bytes and snapshot, the Runner cannot advance, record one-event-per-step evidence, check continuity, reconstruct or re-adjudicate. This is the hard blocker. |
| T022-Q2 | **No pending-decision discovery.** The surface requires the caller to name `(stage, actingElementId)`. At first-side entry there is one Selection decision per acting-side element at once. | The Runner could probe stage × element from its trusted snapshot (as the test does), but which element acts first, and whether one element's Selection closes the others, is a Core sequencing rule the Runner should not guess. |
| T022-Q3 | **Combat System steps are not exposed as actions.** Barrage, assault resolution, Force completion and V19 `force-unavailable` cancellation, Anti Armor, World settlement, successor, no-attack, no-retreat/no-custody, Reserve Release completion, repeat Breakdown, and the timeouts exist only as `CampaignHostAdapter` methods. The Host decides when to append them (scoping 020 §7e: "an in-process driver must do the same"). The audience surface covers owner decisions only. | A Runner driver would have to copy the Host's orchestration, which is the Host/Core duplication T020-Q6 deferred. Post-Combat owner steps (Truck Convoy pass, post-Combat pass, second Reserve/Movement) are also HostAdapter-only. |
| T022-Q4 | **Terminal shape.** Manifests terminate on a position ID only. Repeat cycles revisit Combat positions. | `CMB-CON-006` requires an occurrence-aware terminal: an Exercise manifest contract change. |
| T022-Q5 | **Transcript and controller identity.** The transcript, receipt check and readjudication are keyed by Core action ID. Combat offers are visible bytes plus `cmbref.v1` refs; controller candidates (v3) have no Combat kinds. | Which identity the trusted bundle records (Core receipt action ID, audience ref, or both), and how a policy tells assault from finish (parse visible bytes, or a typed offer kind from Core), are contract decisions. |
| T022-Q6 | **v2 stages are not frozen.** `post-assault-retreat`, `repeat-combat-selection`, `repeat-movement` emit `contractVersion:2`; the v2 artifacts (020E4) are not written yet. Only v1 files exist in `docs/specs/`. | A Runner slice that stays on v1 stages (Selection → RBA decline → Force → Custody → Reserve Release → Cycle Control) is possible once Q1–Q3 are settled; anything past a retreat or a Repeat would consume unfrozen bytes. |

Not a finding: T020-Q8 (non-combat contact) applies. In `combat-assault` the `axis-truck` Selection
decision exists, and its assault would be cancelled downstream by the V19 System step, which is again
Q3.

## 5. Options for the owner

| Option | What it means | Cost / risk |
| --- | --- | --- |
| **(A) Plan as written (recommended).** Core Task 022 extends `CampaignExercises` with a trusted Exercise-side Combat query and submit: pending Combat decisions (stage, element, offers) and the Host-orchestrated System steps as ordinary System-audience actions, each submit returning `ExerciseStepResult` with exactly one event. Runner 023 then extends `ExerciseControllerCandidate` and policies, the same way Movement/Reaction were added. | Needs the Core owner (020 agent) and an owner call on how it relates to the pinned 020D surface (for example: the Exercise path wraps the audience surface internally). Keeps the Runner a thin consumer. |
| (B) Runner calls `CampaignHostAdapter` directly. | Private-ID drafts plus System steps, copying Host orchestration into the Runner. | Fastest route to a sweep that could hit #446, but it bypasses the audience surface, duplicates Host rules (Q6), and couples the Runner to Host-adapter internals. Acceptable only as an explicit throwaway spike. |
| (C) Expose the trusted step on the public result. | Reverse the 020D pin. | Fixes Q1 only; Q2/Q3 remain. Leaks the successor to every public caller, not just trusted in-process ones. |

## 6. Proposed breakdown once unblocked (assuming A)

| Child | Output | Depends on |
| --- | --- | --- |
| 022A0 (this slice) | Scoping + Runner-side frontier evidence. Test-only. | — |
| 022A | Core: trusted Exercise Combat query/submit for the first-cycle v1 path, System steps as actions, one event per step, reconstruction. | Owner ruling on §5, Core owner |
| 023A | Runner: controller candidate v4 with Combat kinds; `first-by-action-id` and a "finish-first" policy; drive `combat-assault` past Selection to Force or Reserve Release; readjudication through the same path. | 022A |
| 023B | Occurrence-aware terminal (manifest contract bump) and unsupported-terminal admission. | 022A, Q4 ruling |
| 023C | v2 stages (retreat, Repeat). | 020E4 freeze |
| 024 | Maneuver/paired evidence; a repeated-assault maneuver for #446-class coverage. | 023 |

## 7. What 022A0 changed

- Added `tests/Cna.ExerciseRunner.Tests/Execution/CombatFrontierEvidenceTests.cs` (3 test cases).
- Not touched: Core, Runner source, manifests, scenarios, frozen contracts, Host.
