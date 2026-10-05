**Verdict: Not ready for unconditional acceptance.** No actionable P0–P3 defect found in N2’s implementation. The confirmed baseline Breakdown source-pin failure still needs explicit coordinator disposition before acceptance or dependent native work.

Review instance: **N2 set1/pass1, total1 of9; maximum3 per set.**

### Findings

No implementation findings. One unresolved acceptance gate:

The unchanged Breakdown oracle fails at [check_fixture](../../specs/verify-combat-inherited-breakdown-completion-v1.py), before running semantic checks. I independently reproduced the identical failure at reviewed HEAD and in a freshly extracted exact baseline archive.

Evidence supports stale verification metadata:

- Expected sequence-source hash: `c5426245…`, matching `2e17f60^`.
- Baseline and reviewed HEAD hash: `d019a3bc…`.
- PR146 changes catalog caching; position-construction code is unchanged.
- Separately executed all eight retained Breakdown traces, golden comparisons and semantic checks successfully.

Those supplementary checks establish useful behavioral evidence. They **do not turn the original oracle into a pass**. No pins were changed or checks patched.

Smallest next action: coordinator explicitly records disposition of this baseline failure, as required by the [plan](../../design/combat-cycle-implementation-plan.md). Evidence supports accepting it as a pre-existing limitation for this bounded contract change.

### Plan Review

Implementation matches accepted entry-only R1 scope:

- Both actual seed1, Normal, ordinary NONE owner openings.
- Owner idle Movement completion `11→12`; System empty Breakdown completion `12→13`.
- Real original-unit Movement-end proof and distinct completion receipts.
- Preserved World, resources, RNG, Weather, order and cycle.
- Candidate absent at versions11/12 and supported at13, before selection.
- Existing route admission and C3a boundaries remain closed.

Exactly five primary and three administrative paths changed. All204 predecessor spec files and protected native/test/project-map paths are byte-identical to baseline.

The640-line oracle combines admission, composition and adversarial verification. Reusing unchanged private transition kernels is reasonable here; source pins expose coupling. No architectural pivot or scope expansion is warranted.

### Claim Ledger

| Author claim | Assessment |
|---|---|
| Full original source authenticates admission; fixture is output only | Confirmed by replay and opening-pin predicates |
| Events retain predecessor canonical grammar and actual receipt/proof lineage | Confirmed by kernel inspection and literal oracle |
| Cuts, exact retries and forged history/proof rejection hold | Confirmed by full matrix execution |
| World/resources/RNG remain unchanged; candidate appears only at13 | Confirmed by semantic assertions and reproduced traces |
| Existing readers remain unchanged | Confirmed against exact baseline |
| Breakdown failure predates N2 and comes from PR146 | Confirmed independently |
| Seven predecessor oracles, build, Boundary81 and full2512 passed | Retained logs inspected and hashes matched; not independently rerun |
| Semantic RED demonstrated missing entry | Retained script/log verified; log stops at first owner. Additional independent probes confirmed both-owner rejection |

Preliminary findings ledger was recorded before reading the separate author explanation. Dispatch/bootstrap had already disclosed the baseline-failure claim, so that aspect was not blind.

### Exact Verification

Reviewed:

- Worktree: `/Users/dsteele/.codex/worktrees/66db/sandtable`
- Branch: `codex/combat-positive-entry-contract`
- Base: `18e8f99f81aed8bb76afb97c86117584b788fa62`
- HEAD: `5ba58040bc225a96b9cd30e537002f86c9c941e9`

Executed:

- `python3 -B docs/specs/verify-combat-positive-entry-v1.py` — **passed**: two owners, four events, six literal proofs, six cuts, six retries and3966 rejection probes.
- Same command with `--semantic` — **passed**.
- Original Breakdown oracle at HEAD and fresh exact baseline — **both failed identically** on sequence-source pin.
- Supplementary Breakdown verification — **passed**: eight traces/goldens,16cuts,8retries,426mutations,92raw probes and298boundary probes.
- Additional both-owner predecessor admission and truncated-opening rejection checks — **passed**.
- Protected-path comparisons,204 old-spec byte comparisons and `git diff --check` — **passed**.
- Four executable artifact hashes and plan hash — matched bootstrap.
- All20 retained gate log/binlog hashes — matched handoff.

Final HEAD and scope remained unchanged. Only excluded generated directories were untracked. No reviewed files were edited.

Exact-worktree codebase-memory index/coverage checks and focused source reads supplied evidence. Serena and CCE tools were unavailable.

### Residual Risks

This proves a private executable contract, not native positive-entry execution. Native source ownership, codec parity and facts certification remain separately gated. Selection, C3a/Result2 consumption, later cycles, repeats and public activation remain outside scope.

### Next Actions

Coordinator should record its baseline-gate disposition and reconcile this report. If it accepts the documented limitation, no N2 implementation correction or additional review instance is justified by these findings. Publication still requires exact-head CI; dependent native work waits for accepted/merged N2.

## Same-instance administrative reconciliation

Reviewer returned Ready with non-blocking follow-up for unchanged5ba58040bc225a96b9cd30e537002f86c9c941e9 after coordinator accepted baseline limitation. Sole administrative blocker resolved, no actionable implementation findings. Original Breakdown oracle remains FAILED; separate reviewed pin maintenance coordinator-owned. No checks rerun/files edited; same N2set1/pass1,total1of9. Publication exact-head CI and accepted/merged N2 required before native work.
