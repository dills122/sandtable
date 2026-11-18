# Graziani RC — exhaustive status, November 17, 2026

**Purpose:** one place that lists everything required for the RC release,
what's actually done, what's planned/scoped but not done, and what this
session's work surfaced as genuinely new/unplanned. Built from
[the RC checklist](2026-10-24-rc-checklist.md),
[the roadmap's MVP gates](../../roadmap/pre-alpha-roadmap.md#post-skeleton-milestones-toward-the-first-playable-mvp),
[the six-turn surface inventory](../../research/2026-10-18-graziani-six-turn-surface-inventory.md),
and everything traced/fixed/discovered in this session. Status labels:
**Done** (merged, evidenced), **Partial** (real progress, real gap remains),
**Open** (not meaningfully started), **Blocked** (needs a human decision or
source material, not more engineering time).

**Note (2026-11-17, later in the same session):** this snapshot predates
[the engine capability deep audit](2026-11-17-engine-capability-deep-audit.md),
which went further than this doc's synthesis into the code itself. See
section 10 below for what it changed or added.

This doc is a snapshot. The underlying checklist/roadmap docs remain
authoritative for gate definitions; this one is for seeing the whole board
at once.

## 1. Native Combat reliability (bounded engine checkpoints)

Per the [RC checklist](2026-10-24-rc-checklist.md#native-combat-reliability) —
**largely Done**, evidenced with commits. RBA/Force/Cycle-control/Reserve/
Custody/Retreat/Selection deadlines are each implemented with fresh-process
crash recovery proved for Reserve/Custody/Retreat. Two explicit residual gaps
on this otherwise-done track: **arbitrary crash cuts / real power-loss**
remain unproved (only process-termination kill-points are proved), and the
"Integrated durable Combat flow" row is explicitly only partially closed —
one retained genuine history is exercised, broader gate still open.

## 2. Release evidence and publication

| Gate | Status | Note |
|---|---|---|
| Source-oracle reconciliation | Partial | Cycle/Breakdown roots pass; full release-head owning-oracle coverage still pending. |
| Original P01 certificate | Open | 44/300 ancestry rows provisional-positive historically; terminal ancestry alone can't close it. |
| Public Combat (020–021, HOST-PUB-001) | Open | Not started this session. |
| Runner/studies (022–025) | Open | Not started this session. |
| Dependencies / Maproom | Partial | **Corrected this session** (was mis-recorded as basically resolved) — production deps audit clean (0 vulns), but 26 high-severity dev-toolchain vulns (nested vite via `@angular/build`/Storybook/vitest) are still open, blocked on npm peer resolution. See [rc-checklist.md](2026-10-24-rc-checklist.md). |
| Release validation / integration | Partial | Focused packets green locally; exact-head CI still outstanding. |

## 3. Playable RC gates (`MVP-GATE-01` through `07`)

| Gate | Roadmap definition | Status | What's actually true today |
|---|---|---|---|
| `MVP-GATE-01` scenario freeze | Exact six-turn/18-stage rules/content/source matrix, explicit unsupported-item count, accepted task graph | **Open** | Oct 18 inventory still stands as "initial inventory, reconciliation open." This session found real progress hiding in it: Cirene, Marmarica, and Catanzaro OA facts are already transcribed in `Cna1979GrazianiSourceFacts.cs`; Sirte is confirmed **out of scope** (arrives turn 10, beyond the 6-turn scenario) — not a gap. 7 double-entry review packets (`docs/work/plans/graziani-*-review/`) still need their next/first-blind reading; handed to codex this session ([handoff doc](2026-11-17-graziani-mvp-gate-01-research-handoff.md)). The 18-stage reachability pass itself hasn't started. |
| `MVP-GATE-02` content admission | Versioned, stable-ID content with no dangling references, provenance/rights accepted | **Partial** | Large jump this session: **57 elements admitted** across PRs [#352](https://github.com/dills122/sandtable/pull/352) (Giarabub, 13), [#355](https://github.com/dills122/sandtable/pull/355) (Tobruk/Bardia, 28), [#359](https://github.com/dills122/sandtable/pull/359) (Category B + Aresca, 14), each as standalone proven fixtures. 3 genuine fact gaps remain, handed to codex: CCNN 1st-HQ weapon ID, Matruh's 5 units' real combat ratings, Essex/Durham assignment-chronology ruling ([handoff doc](2026-11-17-graziani-source-material-research-tasks.md)). Provenance/rights posture for the source PDFs still undecided. |
| `MVP-GATE-03` six-turn authority | Every reachable mechanic/victory rule executes deterministically, zero silent approximation | **Partial** | This is the engine's `ReadinessBlockers()` gate. [PR #361](https://github.com/dills122/sandtable/pull/361) **merged** — every admitted cluster is now wired into one continuous setup path on `main`. Remaining count is **7**, all root-caused (see §7): one missing mechanism (motor-MG), two missing rule gaps (initial-contacts), and three missing chart citations (grouped-ZOC), two already with codex. |
| `MVP-GATE-04` durable local lifecycle | Create/checkpoint/stop/resume/recover, no model/I/O in authoritative turns | **Partial, materially closer** | Bounded journal implemented. 2026-11-18 session closed both of this gate's named sub-requirements: operational diagnostics/recovery procedure is now written ([runbook](../../runbooks/durable-journal-recovery.md), PR #428), and "no model/I/O in authoritative turns" is verified true (zero project references from `Cna.Core`/`Cna.OrleansHost` to `Cna.Intelligence.Gateway`). Per-campaign file topology landed (PR #427) and was stress-tested at N=10-1000, which also found and fixed a real TOCTOU directory-bootstrap race (PR #429). The O(n²) replay-cost scaling issue noted previously remains **not safely fixable** without real architectural investment — unchanged, still a long-campaign risk not an RC blocker. |
| `MVP-GATE-05` Maproom/hot-seat | Two seats inspect/choose/confirm/submit/hand off privately, no model | **Open** | Foundation/read preview only; no work this session. The dependency findings above (26 dev-toolchain vulns) are relevant here before this becomes player-facing, though they don't affect the shipped app itself. **See section 10 — the deep audit found this status description itself is understated.** |
| `MVP-GATE-06` full playthrough | Two independent complete six-turn games, save/resume, correct victory, seed reproduction | **Open** | Not demonstrated; no work this session. |
| `MVP-GATE-07` delivery | Approved distribution/rights, CI-built package, fresh-machine smoke | **Open** | Not started; no work this session. |

## 4. Native second-side movement chain (feeds `MVP-GATE-03`)

**Resolved 2026-11-18 — [PR #430](https://github.com/dills122/sandtable/pull/430), closes #394.**
The gap described below is fixed. Summary of the fix, since the detailed
gap analysis right after this note is now historical: a new
`CampaignNativeSecondCombatDecisionV47` type + `ProjectSecondCombat`/
`ProjectSecondCombatOwner` query, keyed to `(CampaignWorldSnapshotV6,
LandSequencePosition)` directly (mirroring the existing `CampaignNativeSecondMoveV44`/
`ProjectSecondMovement` precedent), gives the second acting side a real
Combat-selection path without ever touching `CampaignSnapshotV11`/
`IsSupportedCheckpoint`'s validator boundary — the first fix attempt tried
routing through that boundary and threw, confirming it genuinely couldn't be
widened safely. `Cna.Core.Tests` 3978/3978, `Cna.OrleansHost.Tests` 311/317
(6 pre-existing unrelated failures, same count PR #427 already carried).
One explicitly deferred gap: second-side Reserve-Release initialization is
still unimplemented for the no-attack-completion path (documented at the
call sites; tracked as a follow-up issue, not silently dropped). Second-side
assault-selection also has no automated test with a real adjacent enemy
contact yet (noted in the PR, not blocking).

**Historical gap analysis (below), now closed by the above — V43 through V46
were real, but the "Combat is a confirmed hand-off" claim was wrong and was
retracted (GitHub issue #394):**
V43 (Reserve) through V46 (Breakdown) are implemented, tested, and wired
into the main `CampaignHostAdapter` path, and V46's checkpoint correctly
*reports* a successor position at the Combat engine's
`PositionDetermination` step for the second acting side. A direct
reproduction this session (build a real V43→V46 history and attempt the
normal second-side Combat opening through it) showed that checkpoint is not
actually reachable through any existing path:

- `ExerciseSession.WithSecondBreakdown` (and the whole V41–V46 `With*`
  family) never advances `CurrentSnapshot` — it stays frozen at the
  *first*-acting-side's old Combat position from earlier the same turn —
  and never clears `CurrentCombatDecision`, so a V46-checkpoint session
  still carries the stale decision object from that unrelated first-side
  combat. The normal owner-cut path rejects immediately with "Combat
  selection has already advanced this authority cut." (from the stale
  decision, not any real second-side progress), and
  `QueryNativeCombatSelectionRoundBinding` rejects with "No pending
  repeated Combat selection."
- Independently, `CampaignContactCombatSelectionCut.Project` and
  `CampaignNativeCombatSelectionTimeoutV38.ValidateInitialScope` hard-require
  `LandActorRole.FirstActingSide` / `order.FirstSide`, so they would reject a
  genuine second-acting-side Combat entry even with a correct snapshot.
- There is no `CampaignOwnerOpeningKind` for a second-side Combat opening on
  the Host, and no call analogous to `TryInitializeSelectionRound` is wired
  after `SecondMovementCompletion`/`SecondBreakdown` persist
  (`CampaignDurableAuthorityJournal.Submissions.cs`).

This is a real RC-authority-continuation gap, not a one-call composition
fix: it needs clearing/rescoping combat-cycle session state across the
native second-side `With*` methods, a real `CurrentSnapshot` projection for
the native track's result, generalizing two independent central
Combat-authority gates beyond `FirstActingSide`, and new Host wiring —
central, correctness-sensitive Combat code that needs a scoped human design
decision, not a speculative fix. `docs/specs/native-second-movement-v44.md`
has been corrected to match. The Reaction-window-resolution gap (proposed
"V47") and vehicle-cohort breakdown accounting remain additional, separate,
smaller unimplemented cuts on top of this.

## 5. Completed this session (merged to `main`)

- [#352](https://github.com/dills122/sandtable/pull/352) — Giarabub cluster admission, 13 elements; CI memory-ceiling fix (host runtime reserve 4GiB→3GiB).
- [#355](https://github.com/dills122/sandtable/pull/355) — Tobruk/Bardia garrison admission, 28 elements.
- [#359](https://github.com/dills122/sandtable/pull/359) — Category B admission (12 elements) + Aresca sequencing proof (2 elements).
- [#360](https://github.com/dills122/sandtable/pull/360) — Angular/Vite build-tooling bump (unrelated Maproom maintenance, already landed).
- [#349](https://github.com/dills122/sandtable/pull/349)/[#350](https://github.com/dills122/sandtable/pull/350)/[#351](https://github.com/dills122/sandtable/pull/351) — pre-existing cleanup PRs (duplicate-hand-copy retirement, combat-obligation ledger, `ProfileVersion` refactor), confirmed merged; their local worktrees are stale leftovers, safe to remove.

## 6. In flight (not yet merged)

- **[PR #361](https://github.com/dills122/sandtable/pull/361) — merged** (`64eccfe4`). Every admitted cluster is now wired into one continuous setup path on `main`. `main`'s empirically-observed readiness-blocker count is now **7**: `battlefield-incomplete`, `control-incomplete`, `ccnn-characteristics-incomplete`, `source-grouped-movement-domain-incomplete`, both `giarabub-motor-mg-*` blockers, and `source-initial-contacts-unavailable`. Took 3 rounds of CI memory-ceiling tuning to land (per-group, then aggregate) — real measured growth from the admitted content, not flaky CI.
- **[PR #362](https://github.com/dills122/sandtable/pull/362)** — docs-only: RC checklist dependency-status correction, research-handoff docs, native-movement stale-doc fixes. No CI risk expected (docs-only).

## 7. New/unplanned work this session surfaced (not on any prior plan)

- **Giarabub motor-MG's 2 readiness blockers have zero clearing mechanism anywhere in the code — confirmed genuine, not optional.** PR #361's follow-up work confirmed the motor-MG roster element only ever gets a `LocationId` via `ActivateGiarabubMotorMachineGun` — skipping that activation would leave `forces-incomplete` open instead, so this isn't avoidable. Needs its own scoped task to design and build the missing resolution mechanism(s); not attempted.
- **`source-initial-contacts-unavailable`'s root cause is now known — two independent, unrelated gaps**, neither a missing action call: (1) `CampaignSourcePolishArrivalContacts.EffectiveCounters` has no "independent" classification case for several commonwealth counters this chain exercises (`cw-attached-artillery.*`, `independent-setup.*`, `cw-priority.*`); (2) `CampaignSourceInitialContacts.TryAdjacent` has no map-adjacency rule connecting rear-area axis locations (Soluch `a4130`, Mechili `b4921`) to the forward commonwealth positions this chain admits. Both need new classification/geometry rules, not wiring.
- **`source-grouped-movement-domain-incomplete`'s root cause is now known.** `CanDeriveForSide` fails for both sides for the same reason: `TryDefensiveCloseAssaultRating`'s hand-maintained table has zero entry for three formations — the Aresca Tank Regiment's CV33, the 6th Australian Division, and the Matruh Garrison. Two of three (Australian, Matruh) are already queued with codex (research-tasks.md Tasks A/B); the third (Aresca's CV33) is a brand-new Task D added to that same handoff. This blocker should close once all three land — not before.
- Another round of the CI per-group-RSS whack-a-mole (same mechanism tuned twice earlier this session) — the new integration test's memory footprint pushed a shared process group over the 6GiB ceiling.
- The dependency/Maproom gate's recorded status was **wrong** — corrected from "basically resolved" to the real split (prod clean / 26 dev-toolchain vulns open).
- The native-movement chain's tracked status was **wrong** — corrected from "Reaction, Breakdown and Combat unimplemented" to "mostly done, one narrow gap."
- 4 stale, unrelated draft PRs (`#306`, `#271`, `#200`, `#191`) closed as dead weight.
- 3 worktrees confirmed to be stale leftovers of already-merged PRs, safe to remove.

## 8. Research handed to codex (planned, external, not engineering time)

- [`2026-11-17-graziani-source-material-research-tasks.md`](2026-11-17-graziani-source-material-research-tasks.md) (v2) — CCNN 1st-HQ weapon ID, Matruh's 5 units' real combat ratings, Essex/Durham assignment-chronology ruling, and (added after PR #361's root-cause trace) the Aresca Tank Regiment's CV33 close-assault rating — the last 3 of these also directly gate `source-grouped-movement-domain-incomplete`'s closure.
- [`2026-11-17-graziani-mvp-gate-01-research-handoff.md`](2026-11-17-graziani-mvp-gate-01-research-handoff.md) — 7 double-entry review packets (arrival/convoy/rail/victory/transport/supply/truck) still needing an independent reading.

## 9. Accepted risks / non-blockers (not on the critical path, don't re-litigate)

- Orleans journal O(n²) replay cost — confirmed not safely fixable without real investment; a long-campaign scaling risk, not an RC blocker.
- Native second-side movement's Reaction-waiver gap — small, scoped, low urgency.
- `battlefieldComplete`/`controlCertified` — ruling already made: stay `false` until the 5-step source-control-map work is complete. Not an open question anymore.
- `ccnn-characteristics-incomplete` — down to one specific fact (the weapon ID above), already in codex's queue.

## 10. What the deep audit added (see the full doc for detail/citations)

A follow-up pass went into the code itself rather than re-synthesizing this
doc's own sources, and found:

- **Maproom's `MVP-GATE-05` status above is understated.** Real workspace,
  action-flow/draft, order-workflow, and map-camera code exists, plus 6
  Playwright e2e specs with accessibility checks — this is well past
  "foundation/read preview."
- **A specific, real `MVP-GATE-05` gap, not previously named anywhere:** the
  roadmap's Sprint 8 seat-handoff privacy requirement (invalidate drafts,
  DOM, storage, worker state, late responses on every local seat handoff)
  has exactly one partial implementation found (`clearAudience()`, two call
  sites) and zero dedicated test coverage.
- **Missing from tracking, likely non-blocking:** the Orleans Host has no
  production clustering provider configured and fails loudly (by design)
  outside a single dev machine — real, but probably fine given the roadmap's
  explicitly local-only RC scope. Worth one line wherever `MVP-GATE-07`'s
  delivery scope is tracked.
- Confirmed the two duplication findings from the earlier PR #347 review are
  fully closed (merged via #349) — not re-flagging them.
- Confirmed zero skipped tests anywhere in the suite.
- A few already-tracked provisional-policy markers in the supply/convoy
  layer were traced to their exact serialized form in code, including one
  well-handled example (the Benghazi `a4827`/`b4827` location-identity
  conflict) worth knowing about by name rather than rediscovering later.

See [the full audit](2026-11-17-engine-capability-deep-audit.md) for
per-subsystem detail, citations, and three concrete bounded next-step
candidates.

## 11. 2026-11-18 session update

Separate session, data-layer/concurrency focus plus closing out #394. Merged
to `main`: #420 (data-layer design doc §3.2 recovery), #421/#423 (manifest
gate + test cleanup), #424 (`AdvanceSystemAsync` fix, filed/scoped/closed as
#422), #425 (accepted #394 design proposal), #426 (RUL-GRA-29 ruling,
terrain priority on compulsory-combat/retreat overlap), #427 (per-campaign
journal file topology), #428 (durable-journal recovery runbook), #429
(TOCTOU directory-bootstrap race fix, found via stress-testing #427), #430
(closes #394 — see §4 above). Net effect on the gates above: `MVP-GATE-04`
moved from "bounded journal, no diagnostics doc" to "bounded journal,
documented recovery, stress-tested topology, one real concurrency bug
found and fixed." `MVP-GATE-03`'s native second-side movement/combat chain
(§4) is now fully resolved except the explicitly-deferred Reserve-Release
follow-up. The 7 `MVP-GATE-03` readiness-blockers from §3/§6/§7 above
(motor-MG, initial-contacts, grouped-movement-domain) were **not** touched
this session — still open, still queued with codex. `MVP-GATE-01`/`02`/`05`/
`06`/`07` saw no engineering time this session.

Still open from this session's own work, not yet actioned: file the
Reserve-Release follow-up issue (#394's explicitly-deferred scope).

## Honest bottom line

`MVP-GATE-03` (six-turn authority) is the gate getting the most direct
engineering attention right now and is genuinely closer than a week ago.
"4 of 8 blockers cleared" became "4 of a *different* 8," which then
resolved to a real, fully-traced **7** once PR #361's follow-up work ran
every remaining blocker to a concrete root cause — no more mystery items,
just real remaining work: one genuine missing mechanism (motor-MG), two
genuine missing rule gaps (initial-contacts' classification/geometry), and
three missing chart citations (now all three queued, two with codex, one
newly added). `MVP-GATE-01` and `02` have a lot of real research
completed (most of it just never reached code), now queued with codex.
`MVP-GATE-04` through `07` have seen no engineering time this session and
remain exactly where the Oct 24 checklist left them, except for the two
corrections in section 10 above. No fabricated calendar estimate follows
from this — per this project's own stated position, an invented date is
worse than an honest "not yet measured."
