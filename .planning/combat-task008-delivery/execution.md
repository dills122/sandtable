# Combat delivery execution

Initial base: `e64bed9` (merged planning PR #123). A1c checkpoint: `a8eed35`,
`codex/combat-task008-creation-binding`, [PR124](https://github.com/dills122/sandtable/pull/124).
A2 checkpoint: `b67b4f9`, `codex/combat-task008-creation-snapshot`,
[PR125](https://github.com/dills122/sandtable/pull/125), stacked on PR124.
B1 checkpoint: `22f8da6`, `codex/combat-task008-opening-preamble`,
[PR126](https://github.com/dills122/sandtable/pull/126), stacked on PR125.
C checkpoint: `a0babdb`, `codex/combat-task008-weather`,
[PR127](https://github.com/dills122/sandtable/pull/127), stacked on PR126.
B2 checkpoint: `3ded1eb`, `codex/combat-task008-stage-entry`,
[PR128](https://github.com/dills122/sandtable/pull/128), stacked on PR127.
D1 checkpoint: `073423f`, `codex/combat-task008-reserve-designation`,
[PR129](https://github.com/dills122/sandtable/pull/129), stacked on PR128.
D2 checkpoint: `fa5e723`, `codex/combat-task008-reserve-completion`,
[PR130](https://github.com/dills122/sandtable/pull/130), stacked on PR129.
019A checkpoint: `ba58c43`, `codex/combat-task019a-first-opening`,
[PR131](https://github.com/dills122/sandtable/pull/131), stacked on PR130.
E1 checkpoint: `e641bd3`, `codex/combat-task008-inherited-movement`,
[PR132](https://github.com/dills122/sandtable/pull/132), stacked on PR131.
E2/G1 checkpoint: `e177a4b`, `codex/combat-task008-movement-lifecycle`,
[PR133](https://github.com/dills122/sandtable/pull/133), stacked on PR132.
ActiveG2 branch: `codex/combat-task008-breakdown-completion`, based on E2/G1 checkpoint.
Integration destination: reviewed feature branch; lead owns retained commits and PR.

## Objective and boundaries

Advance Sprint 5 capability gates in canonical dependency order from
[Task008 A1c](../../docs/design/combat-cycle-implementation-plan.md#task008-execution-index).
Canonical plan and specs own requirements; this file tracks execution only.
No public activation or parent completion inferred from a codec slice.

## Execution queue

| Item | Dependencies | Owner / unit | Evidence | Status |
| --- | --- | --- | --- | --- |
| 008A1c | merged A1b | implementation internal subagent; lead integrates | frozen request/Created11 bytes, trusted inputs, retry/conflict/raw-byte negatives; focused52, boundary81, final full1840 pass | Accepted; feature-branch checkpoint |
| A2 publication boundary | existing contract and HOST-RSH-001 | read-only research subagent; lead decides | explicit seam and evidence limits, no provider selection | Complete; HOST-PUB-001 retains actual-persistence obligation |
| 008A1c dev review | implementation | lead | code-review-and-quality axes, spec/test reconciliation | Complete; no remaining findings |
| 008A1c independent rounds 1–3 | dev review, fixes serially | fresh-context reviewers | separate bootstrap/author/report per round | Round1 Ready; round2 P2 accepted/fixed with RED/GREEN; round3 Ready; accepted |
| 008A2 | A1c accepted | implementation internal subagent; lead integrates | focused34/full1874 pass; dev review plus three Ready independent rounds; publication obligation explicitly open | Accepted; feature-branch checkpoint |
| 008B1 | A2 | implementation internal subagent; lead integrates | six frozen traces/30cuts, focused12/full1886 pass; dev+three Ready independent rounds | Accepted; feature-branch checkpoint |
| 008C Weather | B1 | implementation internal subagent; lead integrates | 34chains/68cuts, focused42/full1928 pass; dev+three Ready review rounds | Accepted; feature-branch checkpoint |
| 008B2 stage-entry | C | implementation internal subagent; lead integrates | 12chains/60cuts, focused17/full1945 pass; dev+three Ready rounds | Accepted; B parent adapters complete |
| 008D1 Reserve designation | B2 | implementation internal subagent; lead integrates | 16rows/24cuts/88fingerprints;focused23/full1968;dev+threeReadyrounds | Accepted; feature-branchcheckpoint |
| 008D2 completion codec | D1 | implementation internal subagent;lead integrates | 16chains48fingerprints;focused44/full1989;dev+threeReadyrounds | Accepted; feature-branchcheckpoint |
| 019A first opening | D2 | implementation subagent; lead integrates | 16traces40cuts152artifacts;focused67/full2012;dev+threeReadyrounds | Accepted; feature-branchcheckpoint |
| 008E1 ordinary Move4 |019A| implementation subagent;lead integrates | twoowners14moves16cuts48artifacts;focused31/full2020;dev+threeReadyreviews | Accepted; feature-branchcheckpoint |
| 008E2/G1 route lifecycle |E1| implementation subagent;lead integrates | 8traces24events32cuts88artifacts;focused20/full2032;dev+threeReadyreviews | Accepted; feature-branchcheckpoint |
| 008G2 Breakdown completion |E2/G1| implementation subagent;lead integrates | 8traces8events16cuts40artifacts;focused24/full2044;dev+threeReadyreviews | Accepted; feature-branch checkpoint |
| 008B–H, 019A | canonical dependency graph | unassigned | cumulative Core replay, full retained-history restore | Pending |
| 009–025 | canonical gates | unassigned | cumulative gameplay/public/Runner/72-AC evidence | Pending |

## Review and stop policy

User requests three sequential independent review rounds after dev review before next task.
Each reviewer starts without implementation conversation, inspects neutral bootstrap before author
rationale, and returns evidence-backed findings. Lead addresses findings between rounds.
If blockers remain after round 3, run one bounded deep research/experiment against blockers,
then one final independent review (round 4). Proceed only if blockers cleared; otherwise stop
for user intervention. Do not restart review counts or silently expand scope.

## Verification

Use repository .NET 10 native MTP commands with `--project` / `--solution`, focused xUnit
filters, unique binlogs, frozen Python oracle and full repository format/build/boundary/tests gate.
Record exact commands, failures and totals in per-slice evidence; never count unrun checks.

## Decisions

- Existing frozen C2 contract supplies A1c bytes. No new storage-provider or six-turn inventory
  spike before A1c; publication evidence boundary reviewed separately for A2.
- Initial checkout clean, already includes merged #123; feature branch created before edits.
- CCE current-task search returned unrelated/stale matches; canonical local files used as fallback.
- A1c PR124 remote checks verified during A2: verify, CodeQL (all language jobs), dependency-review
  and observational offline links all passed. No merge performed; A2 remains stacked on A1c.
- B parent split into opening B1 and stage-entry B2; Weather C must run between them. Canonical
  opening terminal is state5; stage-entry requires actual C replay tostate6. This is a compatible
  execution refinement, not a wire-contract change or synthetic predecessor admission.

- B1 PR126 remote verify, dependency-review and observational offline links passed during C work.

- D split D1 designation/D2 completioncodec then019A applies sameevent; full terminalReserve replay
  and Movement handoff require019A. Preserve atomiccompletion wire contract; no syntheticstate.
- Weather PR127 remote verify/dependency-review/offline links passed during B2 work.

- B2 PR128 remote verify, dependency-review and observational offline links passed during D1 work.

- D1 PR129 remote verify, dependency-review and observational offline links passed during D2 work.

- D2 PR130 remote verify, dependency-review and observational offline links passed during019A work.

-019A PR131 remote verify, dependency-review and observational offline links passed duringE1 work.

- E1 PR132 remote verify, dependency-review and observational offline links passed during E2/G1 work.

- E2/G1 PR133 remote verify, dependency-review and offline links passed during G2 work.
