# Task009B independent review

Review instance: 3 of 3.
Candidate: `0f3ec7b3714686ec669624a9462d0618d38003d4`; base: `652c49a`.

## Findings

No actionable findings. No P0–P3 defects established in reviewed candidate.

Verdict: **Ready** for bounded dormant Task009B. This does not certify positive history admission, public activation, or parent009 delivery gates owned by initiating task.

## Plan Review

Reviewed canonical `docs/design/combat-cycle-implementation-plan.md` lines855–875 only. Implementation fits refinement: accepted009A bindings remain intact;009B supplies positive trusted-facts certification, exact Candidate bytes and opportunity-v2 identity. Tasks010/011 retain selection/decline and authenticated Base2 responsibility. Actual successor-root capacity, cursor/version limits and later assault composition remain explicit integration gates, not omitted009B implementation.

Scope verified against base `652c49a`, exact HEAD `0f3ec7b3714686ec669624a9462d0618d38003d4`, branch `codex/combat-task008-reaction-lifecycle`. Five primary paths match manifest before and after verification. Supporting README/design/naming/roadmap changes describe dormant status consistently. Other changed paths are delivery metadata; excluded prior reports, aggregate evidence and execution history were not read. Pre-existing dirty `task009b-checks.md` and `task009b-evidence.md` remained untouched.

Evidence supporting readiness:

- `CampaignCombatCertification.cs:59` reconstructs Created11 from trusted request; validates cycle/config/side/weather scope; permits only integral CP0..10. Lines80–87 compare all World7 fields against initial profile except CP. Equality implementation in `CampaignCombatWorldV7.cs:144` includes element resources, operational state, components and provenance. Unsupported state throws before eligibility null path.
- `CampaignCombatCertification.cs:99` visits1296 morale pairs, reduces only by differential, then6480 joint assault coordinates and8840 legal refusal/capture cases. `Cna1979CombatAdjudication.Resolve` depends on morale only through differential, so reduction loses no outcomes. Distinct records include raw effects, refusal, capture role/share and role losses. Independent frozen-source reduction confirmed637 results,141 accepted retreats,141 refusals,45 attacker-capture and125 defender-capture tuples.
- `CampaignCombatCertification.cs:129` proves unique retreat away from attacker/toward defender anchor, survivor/capture limits, guarded relocation from pre-retreat victim origin to post-retreat captor, exclusion of victim-occupied entered hexes, guard capacity and escape reunion. Existing Content7 validation fixes two units on featureless Clear line with no additional inventory; profile equality excludes hidden blockers, custody, relations and obligations. Both role orientations are exercised.
- Scratch spending retains current CP, applies ordinary costs and loss DP, and checks mandatory retreat through existing helper. CP5/7 boundary therefore includes defender paidCP10→retreatCP11. Scratch results do not escape; no state, RNG, event or receipt publication added.
- `CampaignCombatIdentityModels.cs:62` retains immutable Participant values and bounded target/basis. `CampaignCombatIdentityCodec.cs:83` checks bounded grammar, canonical spelling, then trusted expected bytes. Null trusted-expected sentries demonstrate ordering. Unit/component identity remains separate from representation/location binding.
- `CampaignCombatIdentityCodec.cs:103` writes exact ordered Round2 preimage and domain. Ten retained Round2 literal opportunities match; each outer preimage field has sensitivity coverage. Search of production call sites found no activation caller for new primitives.

## Author-Claim Reconciliation

Author/checks packets read only after preliminary ledger persisted. Claims below treated as testimony, checked against source and independent runs.

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Trusted facts only; actual G2 assessments stay empty | Certification methods, caller search, existing four-history identity test | Confirmed | No positive-history admission claim inferred |
| Complete result coverage1296/6480/8840,637 distinct | Catalogue/Resolve source, exhaustive independent-source rules test, separate Python reduction | Confirmed | Differential reduction and deduplication sound for frozen rules |
| Both roles, retreat/custody routes, survivors and current CP supported | ProveSupport, Content7 validator, World equality, focused identity and World tests | Confirmed | Bounded geometry and scratch arithmetic justified |
| Candidate grammar/canonical checks precede trusted semantics | Codec and null-context rejection tests | Confirmed | Malformed input cannot borrow expected value to bypass decoding |
| Exact opportunity-v2 mechanics, without authentication | Round2 spec/schema/oracle, literal fixture test, digest implementation | Confirmed | Authentication remains010/011 obligation |
| No schema/package/public activation changes | Base-to-candidate diff and production caller search | Confirmed | Scope remains five primary paths plus supporting documentation |
| Earlier TDD, fullsuite2258, boundary81, formatting and remote CI success | Author/checks testimony only | Unverified independently | Not used as independent pass evidence; this review ran proportionate checks below |
| Future capacity and full composition remain open | Canonical refinement and method input/output boundaries | Confirmed | Explicit integration gate, not readiness claim for wider feature |

## Verification Performed

All commands ran with `login:false`. .NET clearance supplied by user; local IPC escalation approved. Source/Git unchanged. SDK-style net10.0, native MTP and xUnit v3 confirmed from global/project/package configuration.

1. `git rev-parse HEAD`, `git branch --show-current`, `git status --short`, `git diff --stat 652c49a 0f3ec7b3714686ec669624a9462d0618d38003d4`, targeted source/docs diff: exact scope confirmed.
2. `shasum -a 256 -c .planning/combat-task008-delivery/task009b-source.sha256`: all five OK before and after checks.
3. `dotnet build tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore -nodeReuse:false -p:UseSharedCompilation=false '-bl:/tmp/task009b-review3-build-{}.binlog'`: exit0;0 warnings,0 errors;7.52s. Binlog `/tmp/task009b-review3-build-20260920-080035--14680--fxBqtX.binlog`.
4. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatIdentityTests' --filter-class '*CombatSelectedRulesTests' --filter-class '*CombatWorldTests' '-bl:/tmp/task009b-review3-{}.binlog'`: exit0;62 passed,0 failed,0 skipped;12.413s. Binlog `/tmp/task009b-review3-20260920-080111--14710--MgGqwO-dotnet-test.binlog`.
5. `/opt/homebrew/bin/python3 -B docs/specs/verify-combat-selection-steps-v1.py`: exit0;5 literal traces,41 cuts,246 event mutations,164 raw rejections; retries, clock/RBA races and FA gate passed.
6. `/opt/homebrew/bin/python3 -B docs/specs/verify-combat-sealed-round-v2.py`: exit0;12 semantic groups,10 traces,68 cuts,610 replay mutations,340 raw rejects,288 clock comparisons/retries,480 lifecycle retries,30 invalid proposals.
7. `/opt/homebrew/bin/python3 -B -` inline independent reduction: imported `expected_rules()` from frozen rules oracle; enumerated all five differentials and36×36 assault coordinates, legal refusal branches and capture shares; independently computed rounded losses, captured quantities and complete distinct tuples. Asserted `(cases, distinct, acceptedRetreat, refusedRetreat, attackerCapture, defenderCapture) == (8840,637,141,141,45,125)` and survivor≥7/captured≤3 throughout. Exit0. This probe consumed canonical source oracle, not author or prior-review evidence.
8. `git diff --check 652c49a HEAD`: exit0.
9. `ps -axo pid,ppid,command | rg 'dotnet|Cna.Core.Tests|verify-combat-(selection-steps-v1|sealed-round-v2)'`: no matching active work beyond inspection command itself. Every spawned build/test/oracle session returned exit0; no background review process remains.

Initial exact-path discovery attempted two spec paths under `docs/design`; files reside under `docs/specs`. Correct paths subsequently read. No behavioral check failed. No full suite, format gate, hosted/public scenario or remote CI rerun claimed.

## Open Questions And Residual Risks

No blocking open question for this slice. Caller-trusted Weather, history and current-position inputs are not authenticated by these primitives. Whole-root capacity, authority/cursor exhaustion and actual movement-to-positive-assault composition cannot be proved by synthetic initial facts; retain planned integration gates before activation.

Tests cover selected frozen profile and source support, not arbitrary future Rules/Content. A future capability expansion must revise profile comparison and support proofs together. No source documents were fetched or reinterpreted; review checks repository-frozen contracts and source oracle.

## Verdict

**Ready** — exact candidate satisfies bounded Task009B refinement, with no actionable findings and independently passing focused checks. Parent acceptance and later activation remain initiating task responsibilities.

## Recommended Next Actions

Return report to initiating task for bounded delivery reconciliation. Carry010/011 authentication and later whole-root/composition gates forward. Review instance3 of3 complete; no additional review, agent, fix or commit started.

## Preserved Preliminary Ledger — Blind Source Pass

Persisted before reading author/checks packets or executing behavioral checks.

- HEAD and branch match bootstrap; all five source manifest hashes match. Existing dirty files: `task009b-checks.md`, `task009b-evidence.md`. Neither read during blind pass. Delivery metadata excluded from behavior review.
- No actionable defect established from source. Canonical plan read only at lines855–875; 009B explicitly excludes positive history admission, final authenticated opportunity, cursor/version and whole successor-root composition.
- Result reduction appears sound: Resolve uses morale coordinates only through differential; enumerator includes all five differentials, both assault coordinates, legal refusal and capture-die branches. Pending independent execution/count validation.
- Content7 validator pins two full infantry units, featureless Clear line, initial adjacency and endpoint supply directions; Created11 exact reconstruction and current-world equality reject other inventory/geometry/history. No hidden-world-dependent positive fork identified.
- Current CP is retained for scratch spending; eligibility thresholds5/7 and weather checks occur after profile/support validation. Paid retreat and custody paths appear bounded for both role orientations. Pending focused test execution.
- Candidate parsing checks shape/bounds and exact canonical bytes before trusted comparison. Opportunity preimage/domain matches Round2 specification; pending literal oracle execution.
- Remaining review probes: verify source-built test output, literal contract oracles, exact tuple reduction, and reconcile author claims without consuming prior reviews or aggregate evidence.
