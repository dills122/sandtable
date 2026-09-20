# E1 independent review

Review instance: 1 of 3

## Preliminary ledger (before author explanation)

Frozen five-file SHA256 manifest matches; HEAD ba58c430efe692d9acacaee5eef9c9b6d827aad4, branch codex/combat-task008-inherited-movement. Explicit untracked implementation and test files included. Canonical movement specification, schema inventory, Python kernel, successor inventory, C# implementation/tests, predecessor replay and pure spending kernel inspected before author explanation.

Independence disclosure: CCE search accidentally exposed first paragraph of e1-evidence.md and review2/review3 bootstrap headers; no prior review report read. Required session recall also returned summaries of historical contract/review decisions. Those summaries did not supply this implementation's verdict; direct source inspection established conclusions below.

- No demonstrated core behavior defect found in initial pass. Replays reconstruct Created11 through Reserve completion before each movement; exact derived bytes reject altered effects and cache claims.
- Seven Clear2 moves reach CP14/Cohesion-4; eighth fails ordinary spending ceiling15. DP uses existing spending kernel with actual movement receipt.
- Retry authorization precedes consumed-occurrence matching, returning old event and current state without reapplying effects.
- World serialization reconstructs expected World from opening and all movement inputs, preventing supported writer from silently dropping forged resources. Need confirm structural equality and predecessor's fixed-profile constraints before final verdict.
- E/G refinement makes stop/resolution shared ownership explicit; avoids circular E-complete-before-G dependency while preserving parent responsibilities and remaining gates.
- Focused fixture tests cover 48 hash/length artifacts, two owners, 16 cuts and retries at each later cut. Full integration gate owned by initiating task; no reviewer build planned.

## Findings

No actionable findings. Preliminary structural-equality concern resolved: `CampaignWorldSnapshotV7.Equals` compares every World collection and creation identity; `WriteWorld` compares against `ExpectedWorld` reconstructed from full typed movement history before emitting bounded absent fields. Trusted creation context validates supported Content7; Setup7 constrains initial CP/Cohesion/Reserve and stage obligations. UnitKey constructor validates original side, while Move4 input writer separately enforces bounded source atoms.

## Plan Review

E1 implements ordinary Move4 only, consistent with canonical inherited-movement scope. Original E movement responsibility remains parent E; inventory-assigned stop/resolution stay visibly shared with G through E2/G1. Required order `019A → E1 → E2/G1 → G2` preserves actual stop2, empty-cohort resolution2, completion3 and subsequent Breakdown completion evidence. Moving flow never becomes synthetic idle or completion. Five primary files remain bounded; README, naming, design and roadmap identify remaining lifecycle, Reaction, full restore and publication work. No parent Task008, public-admission, vehicle, later-cycle or HOST-PUB-001 closure claimed. Refinement is dependency clarification, not architecture pivot.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Full creation-to-opening authority; cache cannot substitute | `Replay`, `ReadState`, `CampaignCombatReserveOpening.Replay`, creation context validation | Confirmed | Every accepted move derives from complete retained predecessor records. |
| Clear2, CPA10/ceiling15, immediate cumulative DP | `Emit`, `CampaignCombatSpending.ChargeOrdinary`, both-owner frozen tests | Confirmed | CP14/Cohesion-4 and two receipt-bound causes; eighth rejects. |
| Atomic World/member/route/progress update | `Emit`, typed models, `WriteWorld`, World structural equality, golden states | Confirmed | Own locations/CP/Cohesion and causal metadata advance together; unrelated resources preserved. |
| Exact retry after later events | Authorization and occurrence matching in `Apply`; retries at each fixture cut | Confirmed | Original event/current replayed state; no extra effect or prefix. |
| Strict canonical input/events/cache and bounded buffers | Parse/serialize comparison, 1MiB/depth32 checks, record count guard, mutation/buffer tests | Confirmed | Malformed/cached/re-signed effects lack authority. |
| Both owners, 14 moves, 16 cuts, 48 artifacts | Two frozen test rows: request + predecessor records + eight states + seven inputs + seven events each | Confirmed | Complete fixture parity, not sampled positive path. |
| Focused runtime success | Reviewer no-build execution, 31/31 tests | Confirmed | 8 E1 cases and 23 predecessor opening cases pass. |
| Full suite/format underway | Author evidence packet only at review time | Unverified by reviewer | Initiating task owns integration evidence before delivery. |

## Verification Performed

- `shasum -a 256 -c .planning/combat-task008-delivery/e1-source.sha256`: five files OK before review and after tests.
- `git diff --check`: passed.
- Reviewed unchanged canonical Python oracle source and `/tmp/e1-movement-baseline.log`: recorded PASS, two traces, 14 moves, 16 cuts, 384 mutations, 66 raw rejections, 14 retries, 81 boundaries, 18 source pins. Did not rerun oracle or regenerate fixtures.
- Runtime detection: SDK-style net10.0 project, xUnit v3 MTP package, native MTP selected in global.json, installed SDK10.0.400. Applied run-tests, platform-detection and binlog-generation guidance.
- Exact reviewer command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatInheritedMovementTests' --filter-class '*CombatReserveOpeningTests' '-bl:/tmp/e1-review1-{}.binlog'`.
- Initial sandbox invocation terminated with local named-pipe permission denial before tests. Same command under approved escalation passed: total31, succeeded31, failed0, skipped0, 6.704s. Successful binlog `/tmp/e1-review1-20260919-234253--66253--2fRXo0-dotnet-test.binlog`; initial blocked invocation `/tmp/e1-review1-20260919-234244--66227--HpnnC4-dotnet-test.binlog`.
- No builds, source edits, commits, delegation or additional experiments performed. Only this report written.

## Open Questions And Residual Risks

No blocking open question. Full solution test/format results remain initiating-task responsibility. Repeated history reconstruction and duplicated bounded World serialization are deliberate finite-profile costs, acceptable at seven legal moves; broader profile admission requires separate validation. Internal state constructors are not authenticated restore entry points; supplied canonical state is checked by `ReadState` against replay. Contract-specific Python error-code taxonomy is not treated as public C# API since adapter remains internal and follows existing JsonException predecessor conventions.

## Verdict

Ready for bounded E1 scope. No claim of E/G parent completion, full Snapshot12 recovery, runtime activation or durable publication.

## Recommended Next Actions

Initiating task should retain integration results, complete remaining authorized review rounds, then continue actual route lifecycle through E2/G1 under current parent gates. No remediation or heavy-pivot gate required by this review.
