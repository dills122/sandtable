# Independent Engineering Review Report

Review instance: 9 of 9

## Exact Scope And Independence

This review covered the frozen branch `codex/combat-sealed-round-contracts` in `/private/tmp/sandtable-combat-creation-snapshot`, from base `c126f16358eb1cf74275893d4361d09eca936166` through head `a96d2a1474536cb6cc7ec9e261ebf45fedcacc47` (13 commits; 32 changed files; `+7221/-31`). The target was clean at entry. All 32 file hashes matched `/private/tmp/sandtable-review-9/manifest.json`, and the diff contains no `src/`, `tests/`, or `scenarios/` changes.

I read repository requirements, canonical designs, the plan, the complete diff, all new specs/schemas/fixtures/oracles, and relevant unchanged runtime context before opening the author explanation. The blind preliminary ledger was saved to `/private/tmp/sandtable-review-9/preliminary.md` first. The author explanation was then treated as testimony and reconciled against the already-recorded assessment.

## Findings

### P3 — Synchronize stale checkpoint language in the cycle and hosting documents

Evidence:

- The newly added `docs/research/orleans-publication-feasibility.md:38-41` says to continue with `003C3c →003D2 →004`, and line 232 says “Next scheduled work is003C3c.” At the reviewed head, C3c, D2a, and D2b.1 are already complete; the current next slice is D2b.2, as correctly recorded in `docs/design/combat-cycle-implementation-plan.md:20-25` and `docs/README.md:101-105`.
- `docs/design/continual-cycle-reserve-composition-v1.md:294-301`, especially line 298, still states that the budget is exhausted at `4of4` and that “policy/contract approval remains pending.” The current policy register records POL-001–008 and the corrected plan as owner-accepted (`docs/design/combat-cycle-policy-reconciliation.md:3-16`), and the implementation plan records later review checkpoints and current contract progress (`docs/design/combat-cycle-implementation-plan.md:3-26`). This stale paragraph predates the branch base, but the reviewed branch touches the same canonical design for the Clear2 correction and the author expressly claims status synchronization.

Failing scenario and impact: a maintainer following either the standalone hosting report or the canonical cycle design can select an already-completed next task or misread an accepted policy decision as still pending. The correct runtime/checkpoint-B gates remain clear elsewhere, so this is bounded documentation drift rather than a contract or delivery blocker.

Smallest credible correction: update the Orleans report's forward pointer to D2b.2/D2c/004/checkpoint B. In the cycle design, label the 4-of-4/pending-policy sentence explicitly as historical Review4 state and point to the current policy register/combined plan, or update the current-status clause without rewriting the historical Review4 verdict.

No P0, P1, or P2 findings remain.

## Implementation Assessment

The reviewed artifacts are sound within their declared contract-only boundaries:

- C3b correctly separates the two private slots, uses one pinned deadline, prevents a private authority-version token from staling the other slot, derives Prepared only after the second seal, and atomically commits CP/ammunition/history/target use before RNG.
- C3c.1 preserves role-ordered RNG purposes/cursors, immutable pre-loss evidence, mandatory settlement ordering, deterministic fallback, closure proofs, and future obligations. Exact retries are resolved before stale-version checks but after authority/actor binding.
- C3c.2 composes the current World/RNG/position/prefix and command ledger into prospective Snapshot12 arms without claiming that its fabricated pre-Combat header is authentic history. The full pre-round snapshot hash includes that inherited metadata.
- D2a computes terrain plus the maximum applicable Contact/Engaged break-off cost, applies only incremental excess-CPA DP, moves the unit and representation together, ends only the moving unit's active pair memberships, and retains unrelated state. The fixed Map A source and current C# table both support Clear cost 2.
- D2b.1 correctly models canonical Reserve membership order, first/later occurrence rules, one deadline, retained conversion/release/offensive-use/exception history, deterministic locked fallback, and the last-choice-before-completion timer cut.
- The Orleans probe exercises a real local silo and a deliberately bounded injected CAS provider through existing public Exercise authority APIs. Its conclusions stay proportionate: it proves same-process single-silo behavior, not durable provider semantics, authentication, multi-silo fencing, process restart, Combat runtime, or production publication.

The Python oracles share implementation lineage and therefore do not independently prove source fidelity or future C# serializer parity. The documents state that limitation clearly, and the frozen vectors still provide useful exact compatibility targets plus strong negative/replay coverage.

## Plan Review

The plan is appropriately conservative and dependency-aware.

- Exactly 25 top-level `CMB-TASK` IDs, 72 design acceptance IDs, and 8 policy IDs are retained.
- C3c, D2a, and D2b.1 are closed only as bounded contract checkpoints. Parent D2b/D2/003, Task004, checkpoint B, and Tasks005–025 remain open.
- D2b.2 still owns continuation witnesses, semantic progress, guarded repeat/finish, and next-Movement exception consumption/expiry. D2c still owns real inherited sequence5 successors, first-cycle opening, positive World/Snapshot composition, and combined CON-002–004 reconciliation.
- Task004 still owns outward/public/Exercise contracts and the complete AC evidence map. Production registration, privacy equivalence, capacity proof, runtime replay, and simulator evidence are not inferred from private fragments.
- The plan explicitly schedules the Task008 inherited-family children and first-opening work before end-to-end predecessor-to-Combat evidence; exact child IDs remain a checkpoint-B output. This is an open planning detail, not an architecture defect at the current checkpoint.
- HOST-RSH-001 remains independent of checkpoint B and does not authorize a provider or production host. The proposed atomic batch/head/receipt direction is reasonable, while provider choice remains evidence-gated.

No heavy pivot is required. There is no human decision gate beyond the already-recorded future owner acceptance for a production host/storage contract.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| The work freezes bounded contracts, not a running Combat campaign or production persistence. | All five contract specs, plan status, unchanged runtime diff, Orleans limits. | Confirmed | The verdict is scoped to contract/research readiness only. |
| C3b, C3c.1/.2, D2a, D2b.1, and HOST-RSH-001 are complete within stated bounds; parent D2b/D2/003 remains open. | Plan lines 3–26 and 154–218; artifact presence and passing oracles. | Confirmed | Completion claims do not prematurely pass checkpoint B. |
| README/docs/roadmap/tech-design/naming synchronize current status. | Navigation documents, cycle design line 298, Orleans report lines 38–39 and 232. | Contradicted in part | Produces the sole P3 finding; core gate status remains correct in the plan/index. |
| Golden hashes are regression vectors and not independent serializer equivalence. | Specs and verifier construction. | Confirmed | Residual production-parity risk is honestly retained. |
| Clear terrain is 2 CP and the corrected `5+4+2=11/DP1` and ceiling examples are source-grounded. | Local PDF SHA-256, rendered Map A chart8.37, `Cna1979Movement.cs`, D2a oracle. | Confirmed | No policy or Rules artifact pivot is needed. |
| All 15 normal Python oracles pass without regeneration. | Independently executed 12 spec oracles and 3 research oracles. | Confirmed | Contract/research evidence is reproducible at the frozen head. |
| Capture-trigger coordinates in the selected profile always produce a positive rounded capture lot. | Independent exhaustive probe across the selected source surface: 144 trigger coordinates, 0 zero-rounded capture cases. | Confirmed | The current eight result cases need no zero-lot capture event branch; the general design rule remains valid. |
| The Reserve last-choice/expiry cut is fixed and preserves prior disposition/history. | `timing_checks` in the Release oracle and its passing 20 timing checks. | Confirmed for current behavior; historical red run not independently reproduced | No current defect found. |
| Orleans produced 12 commands, 13 writes, parity/retry/failure/reactivation evidence and the pinned hashes/record size. | Rebuilt source, rerun executable, program flow, retained logs. | Confirmed | The bounded recommendation is supported; durable/provider claims remain deferred. |
| Planning checker reported 503 links/16 anchors. | No retained checker or output matching that exact claim was found; independent broader audit found 598 local links and 0 missing targets. | Unverified exact count; no adverse consequence | Link integrity is independently supported, but the author's exact checker count is not relied upon. |
| No full .NET suite or new simulator study is claimed. | Author packet, research report, changed paths. | Confirmed | Acceptable for documentation/contract-only scope; runtime gates remain open. |

## Verification Performed

All commands ran from `/private/tmp/sandtable-combat-creation-snapshot` with `login:false` unless noted.

Scope and integrity:

- `git rev-parse --show-toplevel`, `git branch --show-current`, `git rev-parse HEAD`, `git status --short --branch` — exact target/branch/head; clean at entry and clean at final readback.
- `git diff --name-status BASE HEAD`, `git diff --stat BASE HEAD` — 32 files, `+7221/-31`; no runtime/test/scenario path.
- `git diff --check BASE HEAD` — passed before and after review.
- SHA-256 comparison of every manifest entry — all 32 matched before and after review.
- Read-only local Markdown-link audit across changed Markdown — 598 local links, 0 missing files.
- ID counts — 25 task IDs, 72 acceptance IDs, 8 policy IDs.

Contract and research oracles, all passed:

- `python3 docs/specs/verify-combat-content-v7.py`
- `python3 docs/specs/verify-combat-creation-ledger-v1.py`
- `python3 docs/specs/verify-combat-world-settlement-v1.py`
- `python3 docs/specs/verify-combat-rules-inputs-v1.py`
- `python3 docs/specs/verify-combat-cycle-sequence-v1.py`
- `python3 docs/specs/verify-combat-authority-envelope-v1.py`
- `python3 docs/specs/verify-combat-selection-steps-v1.py`
- `python3 docs/specs/verify-combat-sealed-round-v1.py`
- `python3 docs/specs/verify-combat-result-settlement-v1.py`
- `python3 docs/specs/verify-combat-snapshot-composition-v1.py`
- `python3 docs/specs/verify-combat-ordinary-movement-v1.py`
- `python3 docs/specs/verify-combat-reserve-release-v1.py`
- `python3 docs/research/verify-combat-source-freeze.py`
- `python3 docs/research/verify-combat-rng.py`
- `python3 docs/research/verify-reserve-release.py`

Focused .NET checks:

- `dotnet build docs/research/probes/orleans-publication/HostProbe.csproj --no-restore` — passed; 0 warnings, 0 errors.
- `dotnet run --project docs/research/probes/orleans-publication/HostProbe.csproj --no-build` after the rebuild — passed with 12 commands, 13 writes, state version 13, final snapshot `sha256:222b36760b250ab6ea8eb14a7913ffb56bab1b1786cb7a9d9b1f720360ca3c84`, record `sha256:12eb59912e12a86891e2c3a9ce4b9bc7b881f6f1f3f5858e3a2bee410a390a83`, and 179260 bytes. Local socket access was required; the initial sandbox-only bind failed with `SocketException (13)`.
- `dotnet format docs/research/probes/orleans-publication/HostProbe.csproj --verify-no-changes --no-restore --include docs/research/probes/orleans-publication/Program.cs` — passed with approved local IPC access. The first sandbox-only attempt failed because Roslyn's build-host pipe could not bind; this was environmental.

Source checks:

- `/private/tmp/cna-maps-d2-source.pdf` SHA-256 matched the documented `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
- `/private/tmp/cna-map-a-chart-d2.png` was visually inspected; chart8.37 shows Clear cost 2 for non-motorized and motorized movement and Road cost 1 for non-motorized movement.
- Independent selected-surface capture probe: 144 capture-trigger coordinates, 0 with zero rounded loss for the captured role.

One reviewer-generated `__pycache__` was moved out of the worktree to `/private/tmp/sandtable-review-9/reviewer-generated-research-pycache`; no tracked file was changed. Final Git state is clean and all manifest hashes still match.

## Open Questions And Residual Risks

- The inherited pre-Combat header/prefix/receipt chain is still synthetic. D2c must derive and authenticate it from accepted sequence5 predecessors and the actual first-cycle opening.
- Positive Reserve history has only isolated ledger probes; full positive World/Snapshot12 composition and next-Movement exception use/expiry remain open.
- Snapshot/receipt capacity limits have not been proven for a full bounded campaign history. D2c/009 must resolve envelope sizing and retention rather than truncate evidence.
- Public side equivalence, action/reference stability, hidden-information rejection behavior, and safe outward error mapping remain Task004/020 obligations.
- Production C# serializers/readers/projectors, actual restart/replay, and runtime admission remain unimplemented. Passing Python self-consistency is not production parity.
- The Orleans experiment does not cover an actual durable provider, process kill/restart, storage read failure, corrupt records, multi-silo fencing, authenticated principals, outbound delivery recovery, latency, or scaling.
- No full solution build/test or simulator study was run because no production runtime files changed. Those checks remain required at the plan's runtime/public checkpoints.

## Verdict

**Ready with non-blocking follow-ups.**

The frozen branch is ready for the next planned contract checkpoint within its stated non-production scope. The implementation artifacts and plan are coherent, all proportionate checks passed, and no high- or medium-severity defect or heavy pivot was found. The P3 checkpoint-language drift should be corrected during the next navigation/status update without reopening contract bytes or historical review verdicts.

## Recommended Next Actions

1. Correct the two stale forward/status references identified in the P3 finding.
2. Continue only with the already-planned D2b.2 slice, then D2c, Task004, and checkpoint B; do not begin Tasks005–025 early.
3. At D2c/checkpoint B, require the actual inherited sequence5/first-opening trace, positive Reserve World/Snapshot composition, capacity proof, and named Task008/019 child ownership before declaring parent003 complete.
4. Keep the production hosting proposal at its explicit human decision gate and validate a selected durable provider against process-restart/fencing/read-failure/corruption scenarios before implementation claims.
5. Do not start another independent review instance automatically. This is the user-authorized final instance, 9 of 9.

## Initiating Task Disposition

**Accept — P3 stale checkpoint language.** Current cycle design now labels Review4 state as
historical and points to accepted policy/current planning. Orleans forward pointers now distinguish
experiment-time next work from the live plan. Historical verdicts and contract bytes are unchanged.
No P0–P2 findings or heavy pivot. Review9of9 is exhausted; no further independent pass is authorized.
