# RC overnight plan — October 23, 2026

## Baseline and intended result

Planning baseline: `16d76448a008ea4f16299402b8ef44e8c96a4843`, equal to fetched
`origin/main`. Working tree was clean. Planning branch:
`codex/rc-overnight-plan-20261023`. User authorized execution after the planning
session, with green Core tests as the entry gate, independent review, and a morning
handoff. Thread heartbeat `sandtable-overnight-rc-execution` is active every ten
minutes to continue this work. Target handoff: October 24 at 08:00 America/Toronto,
with final 60–90 minutes reserved for review/validation. No dependent gate starts
before baseline Core succeeds.

Ruflo swarm: `swarm-1792808186330-z3idiq`; coordinator record:
`sandtable-rc-coordinator-20261023`. Use task/memory/swarm APIs with native Codex
workers. Ruflo team API rejected creation because its configured directory points
at old `bf0b` checkout; no unrelated checkout configuration was changed. Registration
is not worker execution. Core baseline completed in exec session `89908` with
exit 0: 4,195 full-suite tests passed, zero failed/skipped, plus 81 boundary tests.
Core duration was 1h 01m 23s. Gate 1 began October 23 at 23:03 America/Toronto
with separate Core, Host and documentation workers.

Recent merged work supplies permanent contact fog, native contact-based Combat,
durable owner submissions/receipts, an initial assault plus two repeated assaults,
and ZOC authority/cache fixes. Current implementation plan still leaves original
P01 at 44 provisional positive / 256 absent, zero admitted/public. Development
loopback HTTP is not public Combat certification. Third-cycle Finish-only evidence
reflects an empty legal witness set, not a hard-coded three-cycle limit.

Tonight's target: reduce named Combat certification gaps and leave a reviewed,
reproducible engine checkpoint. A playable RC still requires six-turn Graziani
content/rules/victory, complete local lifecycle, Maproom hot-seat privacy, two
deterministic playthroughs, and distribution gates. No overnight completion date
for that whole product is implied.

## Ruflo queue and execution order

Use namespace `sandtable-rc-20261023` for memory and tag
`overnight-2026-10-23` for tasks. Existing Ruflo store contains unrelated projects;
never treat global task status as Sandtable status. Dependencies below are manual
coordinator gates, not an automatically executing workflow.

| Order / suggested budget | Ruflo task | Owner and completion gate |
| --- | --- | --- |
| 1 / 45–60 min | `task-1792806896374-jnc4ez` | Coordinator: reconcile current contact implementation with original 300 roots, parent017–019, HOST-PUB-001 and020–025. Produce current evidence matrix and smallest missing behavior packet. Sync stale roadmap/README/design statements. Check whether PR306/271/200/191 are superseded; no automatic merge. |
| 2 / 2–3 h | `task-1792806897517-vwvndp` | Core implementer: address one evidenced continuation gap. Freeze contracts first; retain deterministic fallback, replay, settlement/conservation and equal-visible hidden-fork tests. Exit: bounded patch mapped to named acceptance criteria. |
| 3 / 1–2 h | `task-1792806898923-7avwxt` | Host implementer: prove same patch through cold restart, duplicate/lost response, stale credentials/actions and atomic publication. Keep Development/loopback boundary. Exit: exact-profile durable/outward evidence or explicit blocker. |
| 4 / 1–2 h | `task-1792806900515-nrcdrx` | Evidence owner: reconcile certificate roots and applicable Python oracles; demonstrate empty/assault/repeat/finish via certified authority and strict reconstruction. Preserve true failures and unsupported rows. Public activation requires all applicable evidence and review. |
| 5 / final 60–90 min reserved | `task-1792806901829-d4a7az` | Coordinator and fresh reviewer: integrate, run full gate, independent code/plan review, fix material findings, obtain exact-head CI before any merge. Save morning handoff even if earlier gate remains open. |

Budgets are planning bounds, not measured estimates. Stop starting new features
when review/handoff reserve begins. If a source or contract question blocks an
implementation gate, retain evidence and advance only independent documentation
or six-turn inventory work; do not manufacture a compatibility waiver.

Separate dependency-triage task: `task-1792807038337-bb2s0g`. May run alongside
evidence reconciliation with separate Maproom ownership; must close before
distribution or leave an explicitly reviewed release blocker.

Use at most three implementers plus coordinator during execution. Assign distinct
Core, Host and evidence/docs ownership; serialize shared contract and integration
edits. All workers must preserve others' edits. Ruflo records task progress and
handoffs; worker registration alone never counts as execution evidence.

## Validation and handoff requirements

### Selected execution packets after gate 1

Gate 1 completed with the [baseline evidence matrix](../reviews/2026-10-24-rc-baseline-evidence.md).
No new Core runtime feature is justified by the audit. Gate 2 instead closes a
specific proof gap: zero-/one-seal third-cycle timeout through the generic
post-repeat runtime, no result/World/ammo mutation, retained cursor presence,
stale-seal rejection and cold
reconstruction through Finish. The same Host test owner adds fault injection at
the automatic Barrage insert to verify owner-selection/barrage transaction rollback.
Direct RNG cursor-byte equality remains unproved by this Host-only test surface;
do not infer it from cursor presence or successful replay alone.

Gate 3 addresses a separate concrete publication gap: exact campaign creation
retries currently repeat fresh admission and unconditional INSERT. Update the
journal contract first, authenticate and verify retained identity/timing, recover
the retained head without writes, reject conflicting retries, and serialize
independent creators. This Host packet can proceed alongside gate 2 because no
Core behavior change is planned and file ownership is separate.

Gate 4 retains the original300-root and source-pin obligations. Passing bounded
new tests cannot close that certificate. Current original Breakdown and sequence
oracles still fail LandSequence source hashes; no pin refresh or admission waiver
is part of these packets.

- Run focused deterministic tests for each changed behavior, then `just check`
  once on integrated head. Use native Microsoft.Testing.Platform arguments.
- Run relevant retained Python Combat oracles separately: `just check` omits them.
  Known pin drift remains a failure until properly reconciled.
- If Maproom changes, also run `just maproom-check` and relevant browser/privacy
  tests. Otherwise keep UI implementation out of this engine push.
- Preserve exact head, commands, exit codes, counts, test artifacts, remaining
  unsupported profiles and review findings. No historical passing run substitutes
  for current-head checks.
- Morning report: delivered behavior, RC gates closed/open, PR disposition,
  recovery limitations, next bounded task, and whether any workers remain active.

## Integrated validation checkpoint

October 24 resumed execution: original session `26801` is unavailable and its
`/tmp` log is absent. Process inspection found no running test suite. Completion
of that run is unknown, not green. Replacement `just check` runs in session
`32680`, retaining output at `artifacts/rc-overnight/integrated-check-recovered.log`.
User requested validation and commit, then the next concrete Combat runtime gap.
Core/Host scouts prepare that bounded scope while the existing packet validates;
no duplicate suite or concurrent source changes. Original morning deadline passed
without completed handoff; final acceptance remains due.

Next concrete blocker selected in Ruflo task `task-1792866458752-tef8mn`:
`SubmitOwnerActionCore` evaluates Force timeout before validating the current
owner offer. Four isolated regression cases reproduce rejected forged/stale/
foreign proposals advancing durable head 16 to 20. Prepared fix moves only clock
gating after opening/revision/action/HMAC validation; all four pass with fix.
Scratch validation and patch reside under `artifacts/rc-overnight/`; main source
remains unchanged until current packet validates and commits. Independent review
instance 2 covers this fix; existing Force compatibility checks remain pending.

Next-after candidate: persisted native RBA timeout with configured
`cancel-without-decline` fallback. Existing cancellation paths require an actual
decline or cancelled Anti Armor, so neither can substitute. This requires a new
versioned cancellation contract and no-attack completion, then durable immutable
deadline integration. Do not fabricate a decline, debit, roll, or P01 lineage.

Bounded creation retry, third-cycle timeout and Barrage rollback packets are
implemented. Focused checks passed: creation 14/14, outward API 1/1, third-cycle
4/4 and rollback 1/1. Independent review found no actionable source defects;
completed acceptance remains pending the integrated gate. See
[review 1](../reviews/2026-10-24-rc-independent-review-1.md).

First integrated attempt stopped on import ordering in
`CampaignDurableOutwardApiTests.cs`. Corrected ordering only; failure retained in
`/tmp/sandtable-rc-integrated-check-20261024.log`. Final run is active in exec
session `26801`, logging to
`/tmp/sandtable-rc-integrated-check-final-20261024.log`. Restore, format, build
(zero warnings/errors), 81 boundary tests and both contract assemblies passed;
full solution completion is pending. Do not duplicate this run.

Original-300 ancestry verifier passed all 300 rows, retaining 44 provisional
positive / 256 absent / zero admitted. Original Breakdown and cycle-sequence
oracles still fail source pins. See
[certificate status](../reviews/2026-10-24-rc-certificate-status.md). Neither
bounded tests nor ancestry verification close public Combat certification.

## Initial sync observations (historical)

- Git fetch succeeded; no incoming commits or local divergence.
- Three latest listed main CI runs succeeded at baseline; one run:
  <https://github.com/dills122/sandtable/actions/runs/39990704127>.
- SDK `10.0.400`, Just `1.54.0`, Node `22.23.3`, npm `10.9.9` available.
- `npm --prefix src/Cna.Maproom ci` passed (529 packages); Maproom typecheck
  passed. Audit reports three vulnerable packages: `@angular/build` low,
  `fast-uri` moderate, nested `vite` high. Separate release triage required;
  no forced dependency changes made. Full frontend/browser suite not run.
- Serena active for this worktree. Codebase-memory fast index completed; checked
  outward API/test paths have no recorded coverage issue (best-effort only).
- Ruflo MCP/config/memory health checks pass; prior swarm is terminated and
  workflow list empty. Agent execution and overnight scheduling are not tested.
- CCE `context_search` / `session_recall` tools unavailable; used current repository
  docs, graph/source checks and Ruflo memory. Retained Combat memory predates merged
  continuation work, so it is historical context rather than current status.
- Baseline local `just check` passed with exit 0: restore, format, build,
  81/81 boundary tests and 4,195/4,195 full-suite tests; zero failed/skipped.
  Log: `/tmp/sandtable-rc-check-20261023-unrestricted.log`.
  Frontend browser tests, Docker/Aspire launch and retained Python oracles remain
  unverified in this session. Plan relative links and `git diff --check` passed.

## References

- [Current Combat plan](../../design/combat-cycle-implementation-plan.md)
- [Roadmap and MVP gates](../../roadmap/pre-alpha-roadmap.md)
- [Oracle pin maintenance](../../research/combat-verification-pin-maintenance.md)
- [Six-turn inventory](../../research/2026-10-18-graziani-six-turn-surface-inventory.md)
