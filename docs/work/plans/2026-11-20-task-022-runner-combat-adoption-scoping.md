# Tasks 022–024 — Runner Combat adoption: scoping and frontier evidence (2026-11-20)

**Status:** author scoping, a test-only evidence slice (022A0), and the first Core/Runner slice
(022A, §8, owner chose option A on 2026-11-20). No independent review has run.

**This is test-harness work only.** It is not Combat activation, certification or a product feature,
and it does not depend on Task 021. Task 021's capability manifest protects ordinary production
Core authority. `Cna.ExerciseRunner` is a separate CLI-only, deterministic, rules-lab lane
([exercise harness](../../specs/exercise-harness-v1.md)). Nothing here claims Combat is "Ready",
"certified" or "activated". Tracks [#448](https://github.com/dills122/sandtable/issues/448).

**022A0 outcome:** with only the public Combat audience surface, the Runner could not get past Combat
entry because of Core API gaps (§4). The owner chose option A (§5).

**022A outcome (§8):** Runner executions with the staged Combat runtime now get past
`combat.position-determination` for the first time. They drive first-side Selection and the System
step after it (Barrage or no-attack), with reconstruction and readjudication verified. Bundles and the
CLI stay on the old path until T022-Q8/Q9 are ruled.

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

## 3. Evidence (022A0; kept in `CombatSelectionRunnerTests`)

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

- Added Runner-side frontier evidence tests (now folded into `CombatSelectionRunnerTests`).
- Not touched: Core, Runner source, manifests, scenarios, frozen contracts, Host.

## 8. Slice 022A — trusted Exercise Combat path, first-cycle Selection

Owner ruling (2026-11-20, relayed): option A, built in Core now. Start with first-cycle v1 stages.
Do not change the frozen `combat-audience-decision-v1` contract or the 020D `AuthorityBoundaryTests`
pins. Neither was changed.

### Built

- **Core** `CampaignExercises.QueryWithCombat` / `SubmitWithCombat` (public). Outside the range
  below they return exactly what `Query` / `Submit` return. `Query` and `Submit` themselves are
  unchanged, so the Host's generic route, which calls `Submit`, cannot reach any Combat step through
  them.
  - **Selection.** At first-side Combat Position Determination, when the ordinary query offers
    nothing to anyone, the acting side gets the union of every own element's Selection offers
    (`select-contact-assault`, `finish-combat-without-attack`). These are the existing internal
    candidates from `QueryContactOwnerCombatCut`. Submission delegates to the unchanged
    `SubmitContactOwnerCombatSelection`. An owner may open Selection for any own element; the first
    accepted one closes the cut. This matches the Host, which opens per `(side, element)`.
  - **System step.** After a finish, System gets `complete-combat-no-attack`; after an assault,
    `complete-combat-barrage`. These are the same steps the Host appends
    (`CompleteNativeCombatNoAttack` / `CompleteNativeCombatBarrage`). Each candidate's action ID is
    SHA-256 over the step's event bytes, which is the receipt ID the existing step already reports.
  - **Frontier.** At the cut right after that System step, every audience gets an empty set, so the
    Runner reports `no-unique-legal-action` there honestly.
  - Candidates use a new policy, `sandtable.legal-actions.combat-exercise.v1`, and the two System
    kinds are rejected under any other policy.
  - Files: `src/Cna.Core/Exercises/CampaignCombatExerciseActions.cs` (new),
    `src/Cna.Core/Actions/CombatExerciseSystemActions.cs` (new), `CampaignExercises.cs`,
    `CampaignLegalActionSet.cs`.
  - The two new candidate types are registered in `user-space-disclosure-manifest.v1.json` and
    `.v2.json` with no declared members. Every candidate type reachable from a legal-action set is
    in those closed allowlists, as was done for the existing Combat candidates. They disclose no
    new field. This is still a disclosure-manifest edit for owner review.
- **Runner** `CoreExerciseExecutionRuntime.WithCombat` and
  `ReadjudicationVerifier.Verify(..., includeCombat: true)`, both internal. The controller policies
  are unchanged: existing policies pick the lowest action ID, so assault vs finish is
  deterministic but depends on the Exercise-derived campaign ID. The CLI and the public
  `ExerciseExecutor.Execute(manifest)` still use the ordinary runtime (see Q8/Q9).

### Evidence (milestone)

`CombatSelectionRunnerTests` (9 cases), seed 6:

| Setup / policy / Exercise ID | Steps after Combat entry | Terminal reached |
| --- | --- | --- |
| `combat-assault` / `reaction-all-by-action-id` / `combat-selection.c` | Axis `combat-selection-decided` (V12) → System `combat-barrage-no-effect` (V14) | `combat.retreat-before-assault` |
| `combat-assault` / `first-by-action-id` / `combat-selection.a` | Axis V12 finish → System `combat-no-attack-completed` (V13) | `reserve-release` |
| `combat-double-fresh` / `reaction-all-by-action-id` / `combat-selection.a` | V12 assault → V14 Barrage | `combat.retreat-before-assault` |
| `combat-double-fresh` / `first-by-action-id` / `combat-selection.a` | V12 finish → V13 no-attack | `reserve-release` |

Every run succeeds at its terminal, with one event per step, reconstruction verified and
Combat-path readjudication verified. The ordinary readjudication path cannot replay them. Repeated
runs are byte-identical. With an unreachable terminal, the run ends `no-unique-legal-action` at the
frontier, with every audience query valid and empty.

`CampaignCombatExerciseActionsTests` (Core, 6 cases):

- outside Combat, `QueryWithCombat`/`SubmitWithCombat` equal `Query`/`Submit` at every step to
  Combat entry;
- the entry offer count equals the sum of the frozen audience surface's per-element Selection offers;
- accepted Selection and System steps have the same event and snapshot bytes as the audience
  surface's trusted step and the Host adapter;
- the frontier offers nothing, and plain `Query` still rejects there;
- six rejection causes return no step;
- the System kinds are confined to their policy.

### New findings

| ID | Finding | Slice choice (owner review needed) |
| --- | --- | --- |
| T022-Q7 | The existing V12 Selection receipt names the decision's own position (`combat.position-determination`) as `ResultingPositionId`. The successor checkpoint is the decision's `NextPositionId` (`combat.barrage`). Every other Combat step's receipt names its next position. The Runner's checkpoint-continuity check fails on it. | The Host persists and returns that receipt, so Core's step is unchanged. Only `SubmitWithCombat` re-issues the receipt with the successor checkpoint position; event, snapshot, versions, audience and action ID are unchanged. Alternative: fix the V12 receipt in Core (changes Host-returned receipts). |
| T022-Q8 | A Combat step's snapshot checkpoint is a Combat state document, not a campaign snapshot V11. The bundle's `final-snapshot` reader (`ExerciseEvidenceCodec.DeserializeSnapshot` → `CampaignExercises.ReadCheckpoint`) admits only V11, and Core has no public reader for Combat state documents. A Combat-terminal or post-Combat-failure run therefore cannot publish a bundle: the writer fails closed with `artifact-failed`. Enabling Combat for the CLI broke `ANaturallyFailedNoUniqueLegalActionRunProducesAStrictlyReadableBundle` this way. | Combat is staged off for the CLI. Bundle readback of Combat states is plan Task 023 ("strict child bundle readback") and needs a schema decision. |
| T022-Q9 | How a manifest opts into Combat: a new controller policy token, a manifest field, or always-on once Q8 lands. | Runner-internal runtime switch for now; no manifest or contract change. |

### Not done in 022A

RBA decline, Force (both seals, plus the V19 `force-unavailable` System cancel), Anti Armor,
result/settlement, Custody, Reserve Release, Cycle Control, the timeouts, and every v2 stage.
Combat-aware controller policies, the occurrence-aware terminal (Q4), and the CLI/bundle path
(Q8/Q9) are also not done.

## 9. Slice 022B — RBA decline, Force, Anti Armor, result (first cycle)

Owner direction (2026-11-20): broaden coverage one round kind at a time, using v1 stages only.
Test-harness only, as before.

### Built

- **Core** (`CampaignCombatExerciseActions.cs`): the first-cycle assault path follows the Host's
  orchestration order (`CampaignDurableAuthorityJournal.Submissions`/`ForceRounds`). Each step is the
  unchanged trusted Core step:
  1. Barrage.
  2. Decline-only RBA for the bound defender. RBA moves are never offered (`CMB-POL-001`, T020-Q7).
  3. Force Assignment. If the round cannot be paid, System gets the V19 `force-unavailable`
     cancellation (the Host's pre-opening check) and no owner gets a seal. Otherwise both owners'
     seals are offered at once; the System Force completion follows the second seal.
  4. System Anti Armor.
  5. System V21 cancelled assault, or V22 assault result.
  6. The cut after V21 (Reserve Release) or V22 (post-result retreat) offers nothing; that is the
     frontier.
  - Repeat cycles, the second side, RBA/Force timeouts and RBA movement stay uncovered.
- The two 022A System candidate types are folded into one internal `CombatExerciseSystemAction`
  with a kind per step. Both disclosure manifests list it with no declared members.
- **Runner** (`ExerciseController`): when the only active offers are one Force seal per owner,
  every policy takes them in fixed audience order (Axis, then Commonwealth). Any other multi-audience
  cut still fails closed.

### Evidence

`CombatSelectionRunnerTests` (12 cases, staged Combat runtime):

| Setup / policy / Exercise ID / seed | Combat steps driven by the Runner | Ends at |
| --- | --- | --- |
| combat-double-fresh / reaction-all / `.c` / 5, 6 | V12 assault → Barrage → RBA decline → Axis seal → Commonwealth seal → V18 → V20 → **V22 result** | `combat.close-assault`, then `no-unique-legal-action` |
| combat-assault / reaction-all / `.c` / 6 | V12 assault (truck-bound) → Barrage → RBA decline → **V19 force-unavailable** → V20 → V21 cancelled assault | `reserve-release` |
| combat-assault and combat-double-fresh / first-by-action-id / `.a` | V12 finish → V13 no-attack | `reserve-release` |

All runs verify reconstruction and Combat-path readjudication.

`CampaignCombatExerciseAssaultTests` (Core, 3 cases):

- Every assault offered at entry on combat-assault (seeds 6 and 12345) follows either the payable
  or the unpayable sequence. Each step's event and snapshot equal the direct Host-adapter step on
  the same session.
- Both seal orders are accepted, and the first seal leaves the other owner's offer unchanged. The
  V22 results differ only in `priorAntiArmorHash`, the event-chain link.

### New findings

- **T022-Q7 is broader than Selection.** Every owner-decision receipt (V12 Selection, V15 RBA
  decline, V17 Force seal) names the cut the decision was taken at; every System receipt names the
  next position. It looks like a deliberate Core convention, not a Selection bug. The harness keeps
  re-issuing owner receipts with the successor checkpoint. The owner should confirm the convention,
  or rule that the Runner's continuity check accept it.
- **T022-Q4 evidence.** Anti Armor and the V22 result share `combat.close-assault`, so a
  position-only terminal stops before the result.
- **T022-Q10 (new, author convention).** The Runner takes simultaneous Force seals in fixed audience
  order. Seals are hidden and order-independent in outcome (020A1, and the test above), but the
  event chain records the order, so runs only ever exercise Axis-first. Alternatives: a policy
  token for seal order, or Core offering seals sequentially.

### Next (022C)

No-retreat System step (retreat is v2, excluded), settlement, Custody (v1 owner or System
no-custody), successor, Reserve Release (owner choices plus System completion) and Cycle Control
(Finish only; Repeat leads to v2 repeated Movement). That closes a full first-cycle loop.
