# Task010A independent review

Review instance: 2 of 3. Reviewer mode; fresh context.
Base: `6f52d81c5d0ac95a9f0b9799545f304444661dac`.
Candidate: `2eda2edca9f56d8b0d736cf7b8259598255c61ef`.

## Preliminary blind ledger

Persisted before opening `task010a-author.md` or `task010a-checks.md`.
No prior reports, aggregate evidence, execution history, or cross-session retrieval read.

- Scope verified: candidate is HEAD on `codex/combat-task008-reaction-lifecycle`; initial dirty state contains only checks metadata. All five primary SHA-256 pins match.
- Inspected seven new tests, both transition implementations, codec additions and existing bounds/canonicalization, admission and retained-history dependencies, fixture link, supporting documentation changes, inherited selection/no-attack contract prose, and canonical Task009/010 refinement.
- No actionable defect established in blind pass. Selection reconstructs actual G2; traversal reconstructs closed selection. Both regenerate every local event and compare exact bytes. Retries use accepted input hashes and retained events after identity/actor validation. Cache equality cannot substitute for replay.
- Six catalog successors preserve frozen nested selection; outer closure stops at Reserve Release. No World/RNG/resource mutation or release action found.
- Tests derive four real predecessor histories and compare 28 selection plus 60 traversal hash/length commitments, all cuts, earlier retries, representative rehashed corruption, bounds, ownership and invalid commands.
- Plan split is coherent: 010B timed synthetic mechanism and 010C cumulative routing remain separate; 011 Prepared and extended Snapshot12 are not delivered here.
- Pending checks: inspect normative schemas/oracle serialization against codec; reconcile author claims; execute focused native MTP tests. Runtime results and fresh build provenance not yet verified.
- Residual coverage question: negative tests are representative, not exhaustive leaf mutation of both event families. Exact-byte regeneration provides broader rejection than enumerated C# negative tests alone demonstrate.

## Findings

No actionable findings. No source or plan defect established within Task010A scope.

## Plan Review

Canonical `docs/design/combat-cycle-implementation-plan.md:875` explicitly bounds 010A to actual empty selection and six no-attack steps. Implementation meets that boundary through `CampaignCombatInheritedSelection.Replay/Apply/ReadControl` and `CampaignCombatInheritedNoAttack.Replay/Apply/ReadControl`. Admission delegates to actual creation-rooted G2 certification; cached state never establishes authority. Private transitions preserve deterministic version/prefix advancement, authenticated System input and exact retry semantics.

Compared local codec descriptors, ordered fields, hash domains, effects and receipt chaining against both inherited schemas and relevant oracle transition functions. Selection keeps stepIndex zero and segmentClosed false. Traversal freezes complete selection bytes and owns six-step closure. Catalog route and frozen commitments substantiate same-slot Reserve Release arrival, with no release or extra completion event.

Dependency order and exclusions remain sound. 010B timed C3a, 010C cumulative routing, 011 Prepared, extended Snapshot12 and HOST-PUB-001 publication are not completed by this change. Supporting README, design, naming and roadmap changes retain that distinction. No schema, oracle, fixture, hosting, provider, or public-action change is present in candidate diff. Delivery metadata outside designated bootstrap/author/checks/hash files was not read.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Every authority operation reconstructs complete predecessor and local history | Both Replay/Apply/ReadControl implementations; Certification.Admit; retained-history capture | Confirmed | No cache-only authority path found |
| Four actual histories, 28 selection and 60 traversal commitments | Test Histories/Build/CheckGoldens and successful focused run | Confirmed | Exact bytes checked through independent frozen hash/length commitments, not stored literal event JSON |
| All cuts and prior retries | Nested cut/retry loops: 12 selection cuts, 28 traversal cuts, 12 selection retries, 84 traversal retries | Confirmed | 40 cuts and 96 retries exercised |
| First step chains opening receipt; subsequent steps chain previous completion | SerializeTraversalEvent and normative oracle `_transition` | Confirmed | Matches contract despite disposition separately binding selection closure |
| Frozen selection and World/RNG preserved; no release | State construction, serializers, route and boundary equality assertions | Confirmed | Outer traversal alone owns closure |
| Strict canonical parsing, bounds and owned buffers | ValidateStepSyntax/WriteBoundarySyntax, CaptureLocal, defensive copies and hostile-input tests | Confirmed | Malformed shape rejected before trusted history; retained Created copied before local list access |
| Build/full suite/Boundary/format/CI passed | Supplied checks metadata only | Unverified independently | Not treated as reviewer-run evidence |
| Initial RED and ownership regression failed before fix | Author/checks testimony only | Unverified independently | Final ownership regression passes; historical failure not independently reproduced |

## Verification Performed

- `git rev-parse HEAD`, branch/status inspection: exact candidate and named feature branch verified. Initial dirty path was checks metadata only. Later status additionally showed `task010a-evidence.md` dirty through external activity; contents were not read. Candidate HEAD stayed fixed.
- `shasum -a 256 -c .planning/combat-task008-delivery/task010a-source.sha256`: all five paths OK before and after focused verification.
- `git diff --check 6f52d81c5d0ac95a9f0b9799545f304444661dac 2eda2edca9f56d8b0d736cf7b8259598255c61ef`: exit 0, no whitespace errors.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatInheritedStepsTests' '-bl:/tmp/task010a-review2-{}.binlog'`: login:false, approved local IPC; exit 0; 7 passed, 0 failed, 0 skipped; 35.924 seconds. Binlog exists: `/tmp/task010a-review2-20260920-084119--17569--H+BxRW-dotnet-test.binlog`.
- Focused runner completed. Approved process inspection found no remaining dotnet, Cna.Core.Tests or MSBuild processes. Initial sandbox process inspection was denied; approved read-only inspection succeeded.
- No fixes, commits, agents or extra review instances. Sole intentional repository write is this report.

## Open Questions And Residual Risks

Tests used existing build outputs under authorized `--no-build` command; this reviewer did not rebuild or independently establish binary provenance. Full suite, format and CI were not rerun. Python oracle transitions/schemas were inspected but oracle suites were not executed. Negative C# tests cover representative corruptions rather than every leaf; deterministic whole-event comparison and frozen commitments support broader rejection behavior. Repeated bounded predecessor replay has cost, but matches required authority model and introduces no unbounded history path.

## Verdict

**Ready** for bounded Task010A scope. This is review instance 2 of 3, not parent010 completion or root acceptance.

## Recommended Next Actions

Return report to root for acceptance and remaining review-budget decisions. Preserve 010B/010C/011 and publication obligations; no further review or workstream started here.
