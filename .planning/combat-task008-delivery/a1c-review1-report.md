# A1c independent review

Review instance: 1 of 3.

## Preliminary ledger — before author explanation

Scope verified: HEAD/base `e64bed90d60375f281c65e20c982005a26b57e92`, branch `codex/combat-task008-creation-binding`, five modified documentation files and four untracked implementation/test files exactly match bootstrap.

Blind pass limitation: CCE search returned the opening intent paragraph of author packet and heading of evidence packet automatically. No rationale or claimed results read before this ledger. Implementation, tests, canonical envelope specification and Task008 plan inspected first.

- Exact golden comparisons, independently supplied context, request-domain hash and seed handling appear faithful to A1c.
- Investigate context immutability and predecessor-derived catalog5 first-position assumptions.
- Retry recreates an expected initial value to validate retained bytes; no authoritative publication occurs. Must distinguish pure validation from runtime reinitialization and avoid claiming persistence proof.
- Specification lists detailed private diagnostics whereas implementation uses JsonException; determine whether contract-only diagnostic prose implies a required typed runtime diagnostic surface for this bounded slice.
- No actionable defect established yet. Snapshot12/publication/causal restore explicitly remain outside A1c; plan preserves their gates.

## Findings

No actionable findings.

Preliminary concerns closed: Setup and configuration are reconstructed through strict predecessor codecs; inspected Content models use get-only properties and copied read-only collections, artifact bytes are copied, and Rules context retains only validated hash. Catalog5 preamble version wrapping matches complete retained Created11 literal and turn1/111 checks. Retry reconstruction is validation of a temporary value, with no mutable campaign state, publication or RNG draw. Internal JsonException diagnostics follow predecessor codec style; detailed private code/path projection is not an exposed A1c API or an A1c execution-index acceptance criterion, so no speculative blocker assigned.

## Plan Review

A1c covers request/Created11 bytes, independently trusted artifact binding, identity forks, canonical negatives and retry conflict selection. Four primary C# files stay within bounded delivery scope. Existing codecs, runtime registry and host are unchanged. Five documentation edits accurately distinguish feature-branch implementation from merge/acceptance. A2 and B–H remain pending; explicit gate audit retains actual publication, lost-response and complete disabled-admission restore obligations. Publication seam ownership must still be pinned before A2/parent acceptance, as already required by plan. This review does not close parent Task008.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Exact Request1/Created11 bytes and nonrecursive binding | Request codec, Created11 serializer, retained 741/7,520-byte literals, focused test | Confirmed | Frozen creation contract reproduced |
| Independent immutable context | Context constructor, Setup7/configuration codecs, ContentPackV7 models and copy semantics | Confirmed | Embedded rehashes cannot replace caller-supplied identity |
| Full unsigned seed, cursor0 and catalog5 preamble | RandomStreamState construction, seed theory, turn1/111 theory, complete literal equality | Confirmed | No signed narrowing or RNG draws |
| Retry before admission, conflicts preserve retained bytes | CampaignCombatCreationCut.Decide; retained/disabled and 12-fork tests | Confirmed | Pure decision boundary holds; persistence not proved |
| 48 focused tests pass | Independent no-build execution | Confirmed | Production codec paths exercised |
| No public/historical activation | Verified complete Git delta and internal type scope | Confirmed | Prior behavior remains isolated |
| Full solution initially pending | Completed `/tmp/a1c-suite.log` inspected after author packet | Updated | 1,836 succeeded, zero failed/skipped; lead-owned execution |
| Atomic publication/Snapshot12 not implemented | New source contains no I/O; canonical plan exclusions/gates | Confirmed | No overclaim of runtime replay or durability |

## Verification Performed

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCreationBindingTests' '-bl:/tmp/a1c-review1-{}.binlog'`: initial sandbox attempt failed before execution with named-pipe `SocketException (13): Permission denied`. Same command with approved sandbox escalation passed: 48 succeeded, zero failed/skipped. Successful binlog `/tmp/a1c-review1-20260919-205631--39423--DgrV8G-dotnet-test.binlog` exists.
- `python3 docs/specs/verify-combat-authority-envelope-v1.py`: exit0; 4 goldens, 67 mutations, 36 raw-byte negatives, 9 recovery boundary cases, 12 identity/context forks, 2 turn boundaries, 693 nested type negatives. Contract-only oracle; no runtime replay claim.
- `git diff --check`: exit0.
- Independently read completed lead `/tmp/a1c-suite.log`: full solution 1,836 succeeded, zero failed/skipped. Did not launch another whole-suite run/build.
- Git base/branch/dirty scope inspected, canonical plan/specification and all new production/test source inspected. CCE used first; failed expansion (`Chunk not found`) and partial retrieval required focused local source reads.

## Open Questions And Residual Risks

Trusted registry/archive must eventually pin exact artifact digests; internal context establishes validation, not provenance authentication. Atomic campaign-ID uniqueness, lost responses, Snapshot12 and noninitial replay remain unimplemented by design. No production caller exists in this slice. Frozen diagnostic code/path mapping may need reconciliation when an outward error adapter is introduced. No new acceptance gate imposed on A1c.

## Verdict

**Ready** for bounded A1c implementation scope. No actionable finding or heavy pivot. Parent Task008/runtime activation remain open.

## Recommended Next Actions

Return this report to lead for acceptance and required delivery checks. Preserve documented publication and recovery gates; reviewer does not select subsequent task or start another review.
