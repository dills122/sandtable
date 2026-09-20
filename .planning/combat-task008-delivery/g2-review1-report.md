# Independent review

Review instance: 1 of 3

## Preliminary ledger — recorded before author/evidence packet

Scope verified: branch codex/combat-task008-breakdown-completion, HEAD/base e177a4be23ea9205e02fca1c123a556ad07d31e2; four untracked implementation/test files plus tracked fixture link and documentation. All five frozen primary SHA256 entries match.

Inspected canonical spec/schema, all three new implementation files, tests, predecessor lifecycle replay, plan/documentation diff and existing oracle baseline. No concrete correctness defect found. Full three-record lifecycle replay prevents supplied proof/cache admission. Authorization precedes duplicate return; record equality enforces exact retry. Canonical regeneration rejects altered event fields. Projection preserves inherited authority and adds only required transition/receipt fields. Golden tests cover both owners at 1/5/6/7 moves, two cuts, and legacy reader rejection.

Open checks: run focused executable tests; reconcile author claims and full-suite evidence. Capacity guards are inspected, not proven through reachable long.MaxValue/512-receipt runtime histories (closed profile has short bounded histories). Serializer duplicates predecessor projection structure, but golden parity and immutable-field checks bound drift; no blocker identified.

Independence note: CCE search automatically returned a short g2-evidence summary before ledger; author rationale and full evidence packet remain unread. Recall returned historical decisions, no prior review reports.

## Findings

No actionable findings. Frozen implementation meets bounded G2 contract. `Replay` rebuilds complete predecessor history; `Emit` derives successor and sources from trusted authority; `Apply` validates actor/identity before exact retry; `ReadState` compares full canonical replay. Caller-buffer independence and historical reader rejection are covered. No migration, transport, external service, or public admission changed.

## Plan Review

Task008 execution index and E/G refinement correctly order 019A → E1 → E2/G1 → G2. Five primary files match scope. G2 acceptance criteria map to actual System completion, exact first Combat position, preserved inherited World/RNG/proof/progress and distinct completion receipts. Eight frozen traces and both replay cuts exercise actual creation-rooted chain. F Reaction, parent G positive families, H full restore, live Combat, and durable publication remain explicitly open. Documentation still labels review-time G2 work in progress; lead should update evidence/status during closeout, without promoting parent completion. No heavy pivot or missing dependency found.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Full actual lifecycle required; no supplied proof/cache | Replay delegates to predecessor reader and requires exactly three records, idle flow, null interrupt and actual proof; malformed/transplanted lifecycle test | Confirmed |
| System/action/creation/cycle validation precedes retry | Authorize then accepted.Input equality in Apply; actor/action/retry tests | Confirmed |
| Canonical legacy action, predecessor sources, separate receipts | Codec ordered event fields and domain hash; independent action/receipt/prefix assertions; 40 golden artifacts | Confirmed |
| All inherited authority preserved; no material progress | Wrapper retains lifecycle; immutable-field comparison excludes exactly five changed fields; explicit CP/cohesion/exclusion assertions | Confirmed |
| Eight traces, sixteen cuts, 24 focused tests | Cases both owners × 1/5/6/7; two readback cuts; independent focused execution 24/24 | Confirmed |
| TDD red then green | /tmp/g2-red.log contains missing-type compilation failures; /tmp/g2-final.log green24 | Confirmed as compilation RED; independent semantic transition assertions and oracle semantic RED provide stronger no-op sensitivity |
| Bounds/forgeries/caller buffers and compatibility | BoundsAndCallerBuffersCannotChangeTerminalState; canonical mutation and coherent re-signing cases; legacy serializer/Snapshot12 rejection | Confirmed within bounded supported profile |
| No general Snapshot12/public/durable capability | Only internal new types; no activation path edits; parent plan gates retained | Confirmed |

## Verification Performed

- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`: expected branch/base and explicit untracked primary files.
- `shasum -a 256 -c .planning/combat-task008-delivery/g2-source.sha256`: all five OK, before and after review.
- `git diff --check`: exit 0.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatBreakdownCompletionTests' --filter-class '*CombatMovementLifecycleTests' '-bl:/tmp/g2-review1-{}.binlog'`: exit 0, 24 passed, 0 failed, 0 skipped, 18.027s. Approved escalation used for known native MTP IPC requirement. Binlog `/tmp/g2-review1-20260920-002505--71281--xF1zpb-dotnet-test.binlog` exists.
- Inspected canonical oracle transition/authorization/receipt logic and `/tmp/g2-breakdown-baseline.log`: recorded PASS, eight traces/events, sixteen cuts, eight retries, 426 mutations, 92 raw, 298 boundaries, ten source pins. Baseline not rerun by reviewer.
- Inspected `/tmp/g2-build.log`: build succeeded, zero warnings/errors. Root reports format exit 0; `/tmp/g2-format.log` empty. Full suite remains root-owned and pending at report completion.

## Open Questions And Residual Risks

No blocking question. Full suite completion must be recorded by lead before integration. Projection codec duplicates inherited layout; frozen whole-state goldens guard current parity, but future predecessor changes must update/review both layouts. Long-version and 512-receipt checks are inspected rather than reachable through this short closed profile. Trusted actor wrapper authentication remains future adapter responsibility; this dormant internal projector correctly enforces supplied System identity.

## Verdict

Ready for bounded G2 implementation scope. No claim of live Combat, general restore, parent Task008 completion or durable publication. Integration remains subject to lead-owned full gate.

## Recommended Next Actions

Record full-suite outcome and closeout status; retain existing parent exclusions. No code fix or further review required by this instance's findings.
