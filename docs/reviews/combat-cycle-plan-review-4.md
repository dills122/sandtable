# Combat Cycle and Combined Plan Independent Review 4

Review instance: **4 of 4**. Date: 2026-09-06. User explicitly approved one additional pass;
initiating task raised the previous maximum from3to4 without resetting the count.

- Base: `2e7fbe0fa980085bc1f2ead6eb79f7f66e11090b`.
- Reviewed head: `9f683d1913614797c8badef9640ee965afaa2eb0`.
- Branch: `codex/cmb-des-004-resolution-order`.
- Scope: two commits, seven documentation files, +602/-34; clean review worktree.
- Reviewer: fresh-context `combat_cycle_plan_review_4`, read-only, no inherited author history.
- Blind preliminary completed before the separately delivered author explanation.

## Findings

**P2 — Assign ordinary Contact/Engaged break-off work before enabling continuation.**

At the frozen head, [TASK-018/019](../design/combat-cycle-implementation-plan.md) lines147–148 assign
proximity, Reserve exceptions, retained state and continuation witnesses but omit ordinary movement's
relation-dependent costs and atomic membership changes. [DES-001](../design/combat-opportunity-identity-v1.md)
lines109–114 explicitly leaves that handoff to downstream design.

Failing case: an admitted assault produces zero-loss Engaged; attacker has spent5 against CPA10 and
has an otherwise legal empty Clear neighbor. Land8.15/8.24 permits the move with4 break-off plus1
terrain CP even without ZOC. Existing [Movement calculation](../../src/Cna.Core/Campaigns/CampaignElementMovedV3Factory.cs)
lines413–423 charges terrain/hexside only and lacks relation handling. Reusing it undercharges the
move and leaves stale relations. Rejecting an unsupported move cannot establish no legal continuation
under the [cycle assessment](../design/continual-cycle-reserve-composition-v1.md).

Smallest correction: assign bounded ordinary break-off semantics, atomic CP/membership effects,
continuation assessment and replay tests to contract freeze and TASK-018/019. Alternatively, prove
all such continuations unreachable under an explicit all-result admission certificate. Current
restrictions supply no such proof. Actual RBA and broader ZOC work need not enter this correction.

## Plan Review

Source/calendar → exact contracts → dormant authority → public projection/admission → Exercise/Runner
ordering is otherwise sound. Compatibility, recovery with admission disabled, mandatory settlement,
full selected-table coverage and future obligations have explicit owners. All25 dependencies resolve
backward or to G0; six contract families, eight policies and all72 design ACs are indexed. TASK-004
appropriately owns per-test expansion. One completeness gap remains in the reviewed head. No heavy
architectural pivot was established; a bounded contract/plan correction is credible.

## Author-Claim Reconciliation

| Author claim | Evidence | Status / consequence |
| --- | --- | --- |
| Two commits, seven docs, +602/-34; clean frozen head | Git scope/status | Confirmed. |
| Eight composition decisions,12 new ACs,25 tasks,six contract families | Document/structural inspection | Confirmed. |
| Opening identity excludes opening event; public bindings omit private provenance | Composition lines48–96 | Confirmed; coherent nonrecursive proposal. |
| Policies remain pending; runtime/exact schemas unimplemented | Policy/plan gates | Confirmed; no production-readiness claim. |
| Required supported-continuation dependencies are assigned | TASK-018/019, DES-001 and current Movement | Contradicted in part; ordinary relation-dependent movement omitted. |
| Three research probes pass and prove bounded research facts only | Independent execution | Confirmed; no runtime/replay/privacy proof inferred. |
| Seven temporary tuple probes and prior exact link count | Author testimony; temporary probes unavailable | Unverified; not used for readiness. |
| Current World6 rejects expenditure above CPA | CampaignWorldV6.cs lines227/243 | Confirmed; successor-state work required. |

## Verification Performed

- Git status/head/log/stat and `git diff 2e7fbe0..9f683d1 --check`: scope matches, clean, whitespace passes.
- `python3 docs/research/verify-reserve-release.py`:7 release,8 CP,7 scope,4 offensive-use cases pass.
- `python3 docs/research/verify-combat-mutable-state.py`:8 boundaries,108 combinations,3 guard probes pass.
- `python3 docs/research/verify-combat-rng.py`:12 seeded vectors pass.
- Read-only structural checks:263 local links across seven changed docs resolve; ordered IDs,
  six12-AC tables and acyclic task graph pass.
- Visually inspected Land pages12/14/28 and errata8.23; inspected sequence, Movement, world validation,
  opaque Exercise and protobuf boundaries. No .NET build/tests for documentation-only scope.

Reviewer changed no files. Historical author probes are not promoted to independently reproduced evidence.

## Open Questions And Residual Risks

Full table transcription, calendar mapping, exact codecs and runtime privacy/recovery remain future
gates. Initial fixture certification must cover every reachable continuation/terminal branch.
Research vectors cannot establish that. Frozen3/3 prose predates user authorization for this pass;
current closeout records4/4 while preserving prior review scopes.

## Verdict

**Not ready** for combined-plan acceptance at9f683d1 until ordinary relation-dependent continuation
receives an explicit disposition. This verdict is preserved; no independent reassessment of later
corrections is claimed.

## Author Disposition and Bounded Correction

**Accept.** Author added ordinary Contact/Engaged break-off to cycle composition and expanded existing
AC-007 with the no-ZOC Engaged counterexample, insufficient CP and atomic restart requirements.
CON-004 and TASK-001/003/018/019 now assign source precedence, exact receipt contracts, ordinary
movement implementation and use of the same rule in continuation assessment. The plan explicitly
requires affected-membership closure, unrelated-membership preservation and relevant boundary tests.

This assigns the existing DES-001 obligation within its existing continuation checkpoint; it does
not introduce actual RBA, broader ZOC categories, new workstreams or runtime behavior. Task IDs and
72 canonical ACs remain stable. Author checks passed266 local path links across seven closeout docs,25 ordered task/dependency
rows,72 stable AC IDs and explicit TASK-001/003/018/019 handoff coverage; those checks are not a new independent Ready verdict or implemented movement evidence.

## Recommended Next Actions

Consider the author correction during owner policy/plan acceptance, then execute source and contract
freeze. Keep the eight policy choices pending until explicitly decided. The approved maximum is now
**4 of 4 used**; no fifth instance has been started or authorized.
