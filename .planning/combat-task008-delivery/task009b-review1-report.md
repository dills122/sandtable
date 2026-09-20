# Task009B independent review

Review instance: 1 of 3. Base: `652c49a29642e2cd9d5d8b321341fae5484a58ff`. Branch: `codex/combat-task008-reaction-lifecycle`. Frozen boundary: five paths in `task009b-source.sha256`.

## Findings

No actionable findings. Source, canonical-contract, independent oracle checks and focused tests support bounded Task009B readiness. Residual integration limits below remain explicit.

## Plan Review

Implementation matches Task009 refinement at `docs/design/combat-cycle-implementation-plan.md:862`: dormant positive profile/result/geometry certification plus Candidate and opportunity-v2 mechanics. Five primary paths only; README, tech-design, naming and roadmap changes accurately retain in-progress status and authentication limits. No contract/schema/package changes or public registration.

`CampaignCombatCertification.CertifyInitialProfileFacts` (line59) authenticates retained Created11 against trusted request, validates cycle provenance/scope, rejects nonintegral/out-of-range CP, and compares every other typed World field with initial state. Exact Content7 validation supplies complete two-unit infantry inventory, Clear line geometry, stage readiness and absence of hidden blockers. Supported nonnormal weather or excessive voluntary CP returns no candidate; unsupported state throws before that absence can conceal it.

`CreateSupportCatalogue` (line103) visits1296 morale pairs. Reduction to five differentials is valid because inspected `Cna1979CombatAdjudication.Resolve` depends on morale only through differential. Enumeration retains every assault-coordinate pair, legal refusal and conditional capture die before deduplicating complete immutable results. `ProveSupport` (line129) applies both role-labelled costs to scratch state, verifies casualty conservation/survival and all custody/retreat branches. Captured attacker origin remains attacker pre-retreat location while defender captor destination follows actual retreat. Entered guarded-route hexes exclude victim survivor; escape distance uses original surviving victim. Both orientations tested.

Original UnitKey/component identity remains separate from representation/current location. Candidate is provisional and immutable; reader requires exact canonical syntax before trusted expected comparison. Opportunity v2 binds ordered baseHash/cycleId/positionId/candidate/declineReceiptId under correct domain. No changed World, event, RNG or receipt escapes certification.

Deferrals are justified dependencies:010 authenticates selection/decline and011 authenticates Base2/final opportunity; cursor/version and complete successor-root capacity require later actual composition. Existing four G2 histories remain empty. This review does not close parent009, Task008 publication evidence or public activation gates.

## Author-Claim Reconciliation

| Claim | Independently inspected evidence | Status / consequence |
| --- | --- | --- |
| Complete support:1296/6480/8840,637 distinct | Catalogue loops; Resolve dependency reduction; independent Python expansion of frozen source bands/effects | Confirmed;637 tuples include141 retreats,141 refusals,45 attacker captures,125 defender captures. |
| CP preserved; paid defender10→retreat11 supported | Initial comparison copies only; actual original CP passed to spending; `CampaignCombatSpending.ChargeMandatoryRetreat`; threshold tests | Confirmed by source; helper adds1 CP and excess1 DP. |
| Unsupported inventory, scope and obligations reject | Full typed comparisons, Created11 reader, Content7 validator, mutation tests including1/512/4096 cohesion causes | Confirmed; no unsupported input silently becomes empty assessment. |
| Both roles and custody geometry covered | Role-swapped tests; six-location line validation; per-result origin/destination/reunion checks | Confirmed within selected public profile. |
| Candidate parsing precedes trust; exact v2 opportunity | Reader order, null-context sentries, malformed vectors, ten literal Round2 opportunities | Confirmed by source, frozen oracle checks and independently executed focused tests. |
| No positive history admission or final authentication | Internal APIs, call-site search, unchanged Admit path, source comments and docs | Confirmed; all new call sites are focused tests. |
| Prior TDD, build, format and full-suite results | Author/checks packet only | Not independently claimed; prior logs and aggregate evidence intentionally not opened. |

## Verification Performed

- `shasum -a 256 -c .planning/combat-task008-delivery/task009b-source.sha256`: all five paths OK before review and after oracle checks.
- Scoped `git diff --check 652c49a --` covering five primary paths and four supporting documentation files: exit0.
- `python3 -B docs/specs/verify-combat-selection-steps-v1.py`: exit0;5 literal traces,41 cuts,246 event mutations,164 raw rejections.
- `python3 -B docs/specs/verify-combat-sealed-round-v2.py`: exit0;12 semantic groups,10 traces,68 cuts,610 mutations,340 raw rejects,288 clock comparisons/retries,480 lifecycle retries,30 invalid proposals.
- Independent inline `python3 -B` source reduction: read `docs/research/fixtures/combat-selected-source-v1.json`; expand frozen loss bands with accepted defender differential2 amendment at34/35/36; enumerate effects/refusal/capture shares and independent integer casualty arithmetic; assert8840 rows/637 distinct/141 accepted retreats/141 refusals/45 attacker captures/125 defender captures and minimum7 survivors, maximum3 captured per role. Passed. No repository writes.
- Runtime configuration inspected: SDK-style net10.0; global native MTP runner; xunit.v3.mtp-v2 and runner enabled. After clearance marker appeared, `dotnet --version` returned10.0.400 (allowed latestFeature roll-forward). No .NET invocation before clearance marker.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatIdentityTests' '-bl:/tmp/task009b-review1-{}.binlog'`: independently executed with `login:false` and approved local IPC after marker appeared; exit0,14 passed,0 failed,0 skipped;12.081s total. Used existing built candidate per bootstrap; no reviewer rebuild/full-suite rerun. Binlog exists: `/tmp/task009b-review1-20260920-075242--13936--_E8Y5B-dotnet-test.binlog`.
- Final source-manifest recheck: all five paths OK after focused test. Git status lists only root-owned `task009b-checks.md` modification; candidate source unchanged.
- All three reviewer execution sessions (two Python oracles and focused .NET run) exited0. Final `pgrep -fl 'task009b-review1|Cna.Core.Tests|MSBuild.dll|VBCSCompiler'` found no matching process. No reviewer process left running.

## Open Questions And Residual Risks

No unresolved source defect. Trusted-facts API deliberately cannot establish actual completed Breakdown, Weather receipt/application, first-side order or full retained history; future authoritative caller must supply authentication. Digest primitive does not authenticate its preimage. Selected support proves arithmetic/geometry availability, not serialization capacity or publication of future successor roots. Focused tests and oracles cannot establish production hosting or end-to-end assault activation.

Read-only source/Git review; no fixes, agents, commits or extra review instances. Root committed during review; observed HEAD `0f3ec7b3714686ec669624a9462d0618d38003d4`. Frozen source manifest, rather than mutable HEAD label, defines reviewed candidate.

## Verdict

**Ready** for frozen, dormant Task009B scope. Verdict does not admit positive histories, authenticate final opportunities, close parent009 or authorize public activation.

## Recommended Next Actions

Return this completed instance to root. No fixes requested. Root retains remaining acceptance gates and review scheduling; no additional review started here.

## Preliminary ledger — blind pass, persisted before author/checks

- No confirmed actionable defect found in initial source/test pass.
- Exhaustiveness: 36×36 morale pairs collapse by differential; Resolve depends only on that differential, both assault coordinates, retreat refusal and conditional capture die. Catalogue enumerates all five differentials and joint coordinates, both retreat dispositions, all six capture dice, then deduplicates complete immutable results. Check independent oracle cardinality before final verdict.
- Geometry/custody: creation context validates Content7's complete two-unit Clear line; exact initial-world comparison permits only current integer CP changes. Both orientations require unique retreat farther from attacker and nearer own supply anchor. Every result checks casualty conservation, surviving guard donor, guarded route avoiding opposing survivor, and escape reunion distance. Check source-oracle and inherited contracts before final verdict.
- CP: support proof uses original current CP; comparison-only copies normalize CP for inventory equality. Candidate ceilings 5/7 are checked after support arithmetic; permitted input 0..10 stays inside ordinary 15-CP scratch ceiling. Paid defender CP10 mandatory retreat reaches11. No scratch state escapes.
- Parsing: Candidate shape/bounds and canonical bytes precede trusted-expected comparison. Opportunity primitive binds five v2 fields and domain; no final authentication claim. Existing original-unit/component bindings remain separate from location/representation.
- Test limits: four new tests cover both role orientations, CP thresholds, nonnormal weather, unsupported current state/scope, malformed Candidate bytes, and ten literal Round2 opportunities. Cardinalities alone do not prove result contents; source inspection and independent oracle verification still required.
- Plan: refined009B is dormant mechanism work. Actual positive history, selection/decline, authenticated Base2, cursor/version and successor-root capacity remain future integration gates; no blocker inferred from explicitly deferred work.
- Scope: initial HEAD equals base; five primary changes plus README/design/naming/roadmap documentation. `execution.md` dirty but excluded and not opened. No prior reports, author packet, checks packet or cross-session retrieval consulted. Broad canonical-plan read included embedded historical status prose incidentally; no separate historical evidence/report consulted or used as verification.
- .NET not started; clearance marker must exist first.
