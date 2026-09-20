# G2 independent review

Review instance: 3 of 3

## Preliminary ledger — before author packet

Scope verified: branch `codex/combat-task008-breakdown-completion`, HEAD/base `e177a4be23ea9205e02fca1c123a556ad07d31e2`; five frozen primary-file SHA256 checks pass. Four new C# files are untracked, project fixture link and six navigation/plan files modified. No prior review reports read. CCE recall/search automatically exposed short current author/evidence summaries before first pass; full packets remained unread. Independence is therefore not perfectly blind, although substantive inspection precedes author rationale.

Inspected canonical spec, ordered schema, oracle, tests, all new implementation, preceding Movement lifecycle, and complete tracked diff.

- Full-history admission: Replay reconstructs accepted lifecycle and insists on exactly three events, actual proof, idle flow, null interrupt, and correct Breakdown boundary. No supplied projection admits state.
- Actor/retry: SerializeInput and Authorize precede retry; exact accepted input alone returns original event. Actor, action, creation and cycle cannot be changed on retry.
- Preservation: new state retains actual Lifecycle object; event derives predecessor sources and Movement receipt. State codec writes all inherited fields and appends distinct Breakdown receipt.
- Canonicality: exact re-emission protects whole event and cache; malformed/unknown/reordered/duplicate bytes cannot become authoritative by recomputing receipt/prefix.
- Coverage: eight fixture traces, both owners and 1/5/6/7 moves; canonical hashes cover five artifacts each. Tests exercise actual predecessor construction, state cuts, retries, malformed records, source/receipt forgery and caller-buffer mutation.
- Plan: G2 is bounded internal Combat-entry child, dependent on E2/G1; Reaction, positive Breakdown, full restore/public activation remain open. Navigation still says in progress pending completion of review; final closeout should update status.
- Preliminary actionable findings: none. Remaining checks: author-claim reconciliation, existing full-suite output and independent focused no-build test.

## Findings

No actionable findings. Full-history canonical reconstruction, authority checks, state preservation and exact retries match frozen G2 contract.

## Plan Review

Task008 execution index and E/G refinement correctly order `019A → E1 → E2/G1 → G2`. Implementation stays within five primary files, consumes actual completed lifecycle and reaches only first Combat Position Determination. Canonical spec/schema/fixture/oracle remain unchanged. G2 satisfies its bounded acceptance criteria; parent G/H, Reaction, positive vehicle/Reserve families, later ordinals, general Snapshot12, public registration and HOST-PUB-001 publication evidence remain separate. No rollout or migration is needed for this dormant internal child. Existing full-history replay and immutable predecessor reuse are proportionate to bounded profile; repeated codec layout is an acknowledged maintenance cost, not a current defect. Lead should update in-progress navigation/status to reviewed checkpoint during normal closeout.

## Author-Claim Reconciliation

| Claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Complete creation-rooted predecessor required | `CampaignCombatBreakdownCompletion.Replay`; `CampaignCombatMovementLifecycle.Replay`; actual test `Moved`/`Lifecycle` helpers | Confirmed | Missing, reordered and foreign lifecycle cannot admit completion. |
| System identity checked before exact retry | `Apply`, `Authorize`, `SerializeInput`; `ActorActionIdentityAndRetryConflictsRejectBeforeAdmission` | Confirmed | No owner impersonation or changed identity through retry. |
| Sources/receipts preserve correct provenance | `SerializeEvent`; frozen event hashes; independent legacy action/source assertions; receipt/prefix recomputation | Confirmed | Predecessor Breakdown sources and actual Movement proof survive; Reserve/Movement/Breakdown IDs stay distinct. |
| Full retained state preserved | state constructor retains lifecycle; `SerializeState`; `ImmutableFields`; CP/cohesion/exclusion assertions | Confirmed | Position/head/prefix/ledger/new receipt alone change; no material-progress insertion. |
| Eight traces, sixteen cuts, forty artifacts | fixture case matrix; five `CheckGolden` calls per case; focused run | Confirmed | Both owners and 1/5/6/7 movement costs covered. |
| Forgery, canonical bytes and history swaps reject | mutation test, coherent source/receipt/cache tampering, same-owner count6 and opposite-owner histories, actual replay byte equality | Confirmed | C# tests use two coherent re-signed G2 cases; canonical oracle supplies broader re-signed event-leaf matrix. Neither is mistaken for public runtime evidence. |
| Capacity/byte bounds enforced | `Emit` version/receipt guards; `Parse` depth32/1MiB; serializer cap | Confirmed statically | Huge version/receipt counts not reachable through bounded accepted C# histories; guard exhaustion not directly exercised through public G2 test path. |
| Full solution 2,044 pass; build/format pass | `/tmp/g2-suite.log`, `/tmp/g2-build.log`, empty `/tmp/g2-format.log`, root-reported exit statuses | Confirmed as retained lead evidence | Independent reviewer reran focused subset only. |
| TDD RED preceded implementation | Author packet references `/tmp/g2-red.log` | Unverified | Not needed for readiness; no independent claim of reproducing RED. |

## Verification Performed

- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, tracked diff and source reads: expected branch/base and bounded working-tree target.
- `shasum -a 256 -c .planning/combat-task008-delivery/g2-source.sha256`: all five hashes pass, checked before and after inspection.
- `git diff --check`: exit 0.
- `dotnet --version`: `10.0.400`; global.json selects native MTP, SDK-style net10.0 test project enables xUnit MTP.
- Independent command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatBreakdownCompletionTests' --filter-class '*CombatMovementLifecycleTests' '-bl:/tmp/g2-review3-{}.binlog'`: exit 0; 24 passed, zero failed/skipped; 18s 115ms. Approved escalation used for known MTP IPC restriction. Binlog exists at `/tmp/g2-review3-20260920-003053--72215--bh6UDu-dotnet-test.binlog`.
- Canonical oracle source and retained `/tmp/g2-breakdown-baseline.log` inspected: 8 traces/events, 16 cuts, 8 retries, 426 mutations, 92 raw, 298 boundaries, 10 pins. Oracle not redundantly rerun.
- Retained lead full suite: 2,044 passed, zero failed/skipped; build log zero warnings/errors. Build and format not rerun by reviewer, per assignment.

## Open Questions And Residual Risks

No blocking open question. Review covers dormant bounded profile and frozen working tree, not live Combat, durable persistence, arbitrary history/performance or public actor authentication. System wrapper authentication remains adapter responsibility. All predecessor compatibility conclusions rely on unchanged prior implementations plus focused lifecycle and lead full-suite evidence. CCE retrieved brief author/evidence summaries before ledger; full author rationale read only afterward, and no prior review reports read.

## Verdict

**Ready** for bounded G2 checkpoint. Implementation and plan align; no heavy pivot or corrective code change warranted.

## Recommended Next Actions

Lead reconcile this report, update completion/navigation evidence, and retain branch/PR checkpoint with exact test results. Review instance 3 of 3 is complete; no further independent instance initiated.
