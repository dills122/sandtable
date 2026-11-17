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
| `MVP-GATE-03` six-turn authority | Every reachable mechanic/victory rule executes deterministically, zero silent approximation | **Partial** | This is the engine's `ReadinessBlockers()` gate. [PR #361](https://github.com/dills122/sandtable/pull/361) (in flight, CI pending on its latest push) wired every admitted cluster into one continuous setup path. Empirically-observed remaining count is now **7** (not 8, and not the earlier "4-of-8"/"different 8" framing — see §7 below for the final resolution of that churn). |
| `MVP-GATE-04` durable local lifecycle | Create/checkpoint/stop/resume/recover, no model/I/O in authoritative turns | **Partial** | Bounded journal implemented (per checklist). This session confirmed (via dedicated investigation) the Orleans journal's O(n²) replay-cost scaling issue is **not safely fixable** without real architectural investment — a known risk for long campaigns, not a blocker for RC itself, but worth tracking. |
| `MVP-GATE-05` Maproom/hot-seat | Two seats inspect/choose/confirm/submit/hand off privately, no model | **Open** | Foundation/read preview only; no work this session. The dependency findings above (26 dev-toolchain vulns) are relevant here before this becomes player-facing, though they don't affect the shipped app itself. **See section 10 — the deep audit found this status description itself is understated.** |
| `MVP-GATE-06` full playthrough | Two independent complete six-turn games, save/resume, correct victory, seed reproduction | **Open** | Not demonstrated; no work this session. |
| `MVP-GATE-07` delivery | Approved distribution/rights, CI-built package, fresh-machine smoke | **Open** | Not started; no work this session. |

## 4. Native second-side movement chain (feeds `MVP-GATE-03`)

**Mostly Done — much further along than previously tracked.** A background
investigation this session confirmed V43 (Reserve) through V46 (Breakdown)
are fully implemented, tested end-to-end, and wired into the main
`CampaignHostAdapter` path; the chain's eventual Combat step is a confirmed
hand-off to the existing Combat engine, not a separate mechanism to build.
Two stale docs (`docs/specs/native-second-movement-v44.md`,
`2026-10-24-rc-checklist.md`) said otherwise and have been corrected this
session. The one real remaining gap: no event yet resolves/waives a Reaction
window opened by a second-side move (proposed "V47"), plus separately,
vehicle-cohort breakdown accounting. Both are small, scoped, not urgent.

## 5. Completed this session (merged to `main`)

- [#352](https://github.com/dills122/sandtable/pull/352) — Giarabub cluster admission, 13 elements; CI memory-ceiling fix (host runtime reserve 4GiB→3GiB).
- [#355](https://github.com/dills122/sandtable/pull/355) — Tobruk/Bardia garrison admission, 28 elements.
- [#359](https://github.com/dills122/sandtable/pull/359) — Category B admission (12 elements) + Aresca sequencing proof (2 elements).
- [#360](https://github.com/dills122/sandtable/pull/360) — Angular/Vite build-tooling bump (unrelated Maproom maintenance, already landed).
- [#349](https://github.com/dills122/sandtable/pull/349)/[#350](https://github.com/dills122/sandtable/pull/350)/[#351](https://github.com/dills122/sandtable/pull/351) — pre-existing cleanup PRs (duplicate-hand-copy retirement, combat-obligation ledger, `ProfileVersion` refactor), confirmed merged; their local worktrees are stale leftovers, safe to remove.

## 6. In flight (not yet merged)

- **[PR #361](https://github.com/dills122/sandtable/pull/361)** — integration fixture chaining every admitted cluster before owner finalization. The CI RSS-ceiling failure is fixed (`GrazianiSourceAuthorityTests` split `parts: 2`→`3`, same precedent as commit 05f370bb); CI is re-running on the latest push. Final empirically-observed remaining-blocker count: **7** — `battlefield-incomplete`, `control-incomplete`, `ccnn-characteristics-incomplete`, `source-grouped-movement-domain-incomplete`, both `giarabub-motor-mg-*` blockers, and `source-initial-contacts-unavailable`. `cw-frontline-initial-loads-unresolved` is resolved (pure wiring, as expected).
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
