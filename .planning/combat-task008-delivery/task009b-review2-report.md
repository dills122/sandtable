# Task009B independent review

Review instance: 2 of 3.
Candidate: `0f3ec7b3714686ec669624a9462d0618d38003d4`; base: `652c49a`.

## Findings

No actionable P0–P3 findings. Verdict: **Ready**, limited to dormant Task009B facts certification and identity mechanics.

Scope: five primary files named by bootstrap, plus README, technical design, naming overview and roadmap changes. Branch `codex/combat-task008-reaction-lifecycle`. HEAD and all five source-manifest hashes matched before and after verification. Existing dirty checks/evidence files were not source changes. Other diff entries are review/delivery metadata; excluded from implementation evidence. No generated client, public action, package dependency or contract change found.

Correctness evidence:

- `CampaignCombatCertification.cs:59`, `CertifyInitialProfileFacts`: Created11 establishes trusted initial baseline; cycle identity/config/scope and enum checks precede certification. All World fields and provenance equal initial inventory except integral current CP0..10. Comparison copies do not reset CP used by support probes. Unsupported facts throw; supported non-Normal Weather or CP above5/7 returns no Candidate.
- `CampaignCombatCertification.cs:101`, `CreateSupportCatalogue`: morale coordinates affect `Cna1979CombatAdjudication.Resolve` only through differential. All1,296 pairs establish all five differentials; joint assault coordinates, conditional refusal and all six capture dice cover complete reachable support before record-value deduplication. Independent source reconstruction confirmed6,480 coordinates,8,840 resolutions and637 distinct full results. No seeded result narrows certification.
- `CampaignCombatCertification.cs:127`, `ProveSupport`: Content7 independently guarantees complete two-unit infantry inventory and six-location Clear line; Created/current equality excludes hidden added blockers, depleted resources, Reserve, relationships and outstanding obligations. Both role orientations have one retreat toward own anchor and away from attacker. Every result checks survivors, loss conservation, guarded route to post-retreat captor, guard capacity and escape reunion with original survivor. Current CP feeds scratch ordinary costs and mandatory retreat; accepted CP5/7 reaches paidCP10 and mandatoryCP11. No scratch state escapes.
- `CampaignCombatIdentityModels.cs`, `CampaignCombatCandidate`: participants and component bindings retain immutable original unit/component identity separately from current representation/location. Existing identity tests cover relocation, renamed representation and new occupants without positive-admission claims.
- `CampaignCombatIdentityCodec.cs:83`, `ReadCandidate`: byte/depth/array bounds, closed nested shape, primitive grammar and exact canonical bytes precede trusted expected comparison. Null trusted sentries test that order. Duplicate, escaped, reordered, missing and extra structure cannot substitute for canonical values.
- `CampaignCombatIdentityCodec.cs:104`, `CalculateOpportunityId`: exact ordered `{baseHash,cycleId,positionId,candidate,declineReceiptId}` preimage, NUL domain separator and `sandtable.combat.opportunity.v2` match Round2 schema/oracle. Ten literal cases verify hashes; each preimage field has a mutation check. Hash construction grants no authority.

Architecture/readability/security/performance: bounded internal Core functions reuse Rules arithmetic, spending and existing identity guards. No remote I/O, RNG consumption, event publication or outward fog-of-war exposure. Lazy catalogue is private, immutable and bounded; graph search is over fixed six-location geometry. Repeated scratch allocations are bounded and introduce no demonstrated operational defect in this dormant slice. No unrelated source refactor found.

## Plan Review

Canonical Task009 refinement at `docs/design/combat-cycle-implementation-plan.md:862` authorizes this exact split: accepted009A supplies actual empty inherited admission;009B supplies exhaustive positive facts and provisional identity mechanics. Five primary paths stay within agreed scope. Supporting docs accurately retain in-progress status and name caller-trusted facts.

Selection-v1 spec/schema/oracle, current Round2 spec/schema/oracle and settlement design agree with implemented initial-profile subset. Existing Rules10/Content7/Created11 validation and World/spending contracts support assumptions used here. Positive tests explicitly retain synthetic provenance. No competing Content certificate introduced.

Deferrals are required integration gates, not concealed completion:010 authenticates selection and decline;011 authenticates Base2/Force Assignment and final opportunity; later composition must check current cursor/version, whole successor-root capacity and actual positive history. Parent009 remains subject to remaining review gates. HOST-PUB-001 and public activation remain outside this verdict. No migration, rollout or rollback step needed for these unregistered, non-publishing internal helpers.

## Author-Claim Reconciliation

Preliminary ledger below was written before reading author explanation or checks file and before executing tests/oracles. Author claims then checked against source and independent results.

| Author claim | Evidence inspected | Status / consequence |
| --- | --- | --- |
| Only independently trusted facts; actual G2 remains empty | `CertifyInitialProfileFacts` API/comments, unchanged `Admit`, four inherited history cases | Confirmed; no positive-history admission inferred. |
| All World values except integralCP retained | Full equality chain, typed value equality, unsupported-inventory tests, CP snapshot assertions | Confirmed for selected initial profile. |
|1,296/6,480/8,840/637 coverage,141 accepted retreats,141 refusals,45 attacker captures,125 defender captures | Rules `Resolve`, catalogue loop, focused tests and separate Python reconstruction from frozen source oracle | Confirmed; complete immutable result tuples retain distinctions before reduction. |
| Both role geometries, custody and escape supported | Content7 `ValidateGeometry`, `ProveSupport`, both-side CP5/7 tests and settlement design | Confirmed within fixed initial geometry; broader profiles intentionally reject. |
| CP10→11 mandatory retreat/DP1 | `CampaignCombatSpending.Charge`, existing `CombatWorldTests` assertions, new boundary certification cases | Source-confirmed; focused identity tests execute helper, separate World suite not independently rerun here. |
| Strict Candidate bytes and current v2 identity | Reader/serializer, ten retained Round2 goldens, parser-before-trust sentries, independent contract oracles | Confirmed. |
| No costs/RNG/events publish | Return values and complete changed source inspected | Confirmed; scratch arithmetic only. |
| Prior TDD, full suite, format, CI and earlier reviews passed | Author/checks testimony only | Not independently verified or used as passing evidence. No worker evidence, aggregate evidence, execution history or prior report opened. |

Independence caveat: permitted `task009b-checks.md` had been appended with earlier-review status. That unsolicited status became visible after preliminary ledger; excluded from findings and verdict. Initial broad canonical-plan read also exposed embedded historical summaries; only requirements and Task009 refinement used as authority. No cross-session retrieval or CCE calls made.

## Verification Performed

All commands used `login:false`. Focused .NET command used authorized local IPC outside sandbox; no concurrent .NET invocation started.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatIdentityTests' '-bl:/tmp/task009b-review2-{}.binlog'
```

Exit0;14 passed,0 failed,0 skipped;12.091s. Binlog exists: `/tmp/task009b-review2-20260920-075605--14334--01Wnd7-dotnet-test.binlog`. SDK/native-MTP settings and xUnit test project inspected. Uses existing compiled output as explicitly requested; no independent rebuild or full-suite claim.

```sh
python3 -B docs/specs/verify-combat-selection-steps-v1.py
python3 -B docs/specs/verify-combat-sealed-round-v2.py
```

Both exit0. Selection:5 literal traces,41 event/control cuts,246 mutations,164 raw rejects. Round2:12 semantic groups,10 retained traces,68 cuts,610 replay mutations,340 raw rejects,288 clock comparisons/retries,480 lifecycle retries,30 invalid proposals. These are contract checks, not actual campaign replay.

Independent `python3 -B -` probe imported only `verify-combat-rules-inputs-v1.py.expected_rules()`, expanded frozen source rules, independently calculated attacker ceiling/defender floor/capture ceiling and refusal, and deduplicated complete result tuples. Output: moralePairs1296, joint6480, calls8840, distinct637, acceptedRetreats141, refusals141, attackerCapture45, defenderCapture125. Asserted all results leave≥7 TOE, captured loss0..3, and captured+destroyed=total. Exit0. No source or fixture generated or rewritten.

```sh
shasum -a 256 -c .planning/combat-task008-delivery/task009b-source.sha256
git diff --check 652c49a HEAD -- src tests README.md tech-design.md naming-overview.md docs/roadmap/pre-alpha-roadmap.md
git rev-parse HEAD
git branch --show-current
git status --short
```

Manifest5/5 OK at start and finish; scoped diff check clean; candidate and branch unchanged. Only this report written by reviewer. Existing dirty `task009b-checks.md` and `task009b-evidence.md` untouched. Report resides under ignored planning path.

All three launched long-running sessions returned exit0 and closed. Final process inventory found no `dotnet test`, `Cna.Core.Tests`, selection-oracle or Round2-oracle process. No agents, fixes, commits or additional reviews created.

## Open Questions And Residual Risks

No unresolved blocker within frozen scope. Trusted facts remain caller responsibility; API does not authenticate Weather receipt, complete history, current phase position or positive opportunity. Negative current-world tests cover representative unsupported fields, not every combinatorial mutation. Result-count checks alone would not prove tuple correctness; this review additionally traced Rules arithmetic and independently reconstructed source support. No performance benchmark or fresh build performed.

## Verdict

**Ready** for dormant Task009B at exact candidate above. No heavy pivot required. This verdict does not close parent009, authorize live Combat or establish authentic positive history/publication.

## Recommended Next Actions

Return report to initiating task. Preserve010/011 and later composition gates and existing review-count limit. No reviewer-owned follow-up, source edit or further review started.

## Preliminary ledger — persisted before author explanation and execution checks

Blind source/tests/canonical-contract pass complete. No actionable defect established yet.

| Concern | Blind evidence / provisional disposition |
| --- | --- |
| Exhaustive result support | `CreateSupportCatalogue` covers all 1,296 morale pairs, reduces only by differential, enumerates both assault coordinates and conditional refusal/capture dice. `Resolve` depends on morale solely through differential. Verify counts independently. |
| Profile and hidden geometry | Creation context validates Content7; current World must equal Created11 initial World except integral CP0..10. Verify validator's fixed line, full TOE/ammo/readiness and empty-ZOC guarantees. |
| Both roles and cost boundaries | Tests cover Axis/Commonwealth, CP0/0,5/7,6/7,5/8,10/10, all non-Normal weather, malformed current facts. Support probes use scratch costs and mandatory retreat; no world/event/RNG mutation. |
| Candidate reader order | Closed shape, nested participant grammar, bounds and exact canonical spelling precede trusted expected comparison. Candidate identity remains provisional. |
| Opportunity identity | Ordered Base2/cycle/position/candidate/decline preimage and v2 domain match current schema; ten retained Round2 cases supply literal expected hashes. |
| Plan boundary | Task009 refinement explicitly defers authenticated positive history, final010/011 opportunity and whole successor-root capacity. Do not treat dormant facts as admission. |

Scope verified: HEAD and branch match bootstrap; five manifest hashes pass. Existing dirty files are `task009b-checks.md` and `task009b-evidence.md`; neither read during blind pass. CCE/cross-session retrieval unused. No prior review reports or execution artifacts opened. Canonical plan was initially requested too broadly; tool output truncated, so Task009 refinement was subsequently read exactly. Historical summaries embedded in canonical plan/docs are not used as verification evidence.

Ledger above preserved from blind pass; final disposition appears before it.
