# Native paid-assault independent review

Review instance: 1 of 9. Round 1, pass 1, recovery 0 of 2.
Target: codex/native-paid-assault-consumer, base 6334a7c87446ef5c7694ac325f7650819d891413, head 661ba68d46c77c17bf599e449cc7cc5d9efd70e4, PR177.

## Preliminary ledger (recorded before author explanation)
- Scope verified: exact dispatched head; only unrelated untracked .serena/. Four additive production files, two tests, fixture-copy link, canonical docs and delivery evidence. Runtime/test diff against faab04e and canonical-doc diff against ac20312 are empty.
- Read neutral bootstrap, AGENTS, canonical paid spec, NP01–NP10 plan, all new production files and tests, relevant native ARE ingress, and reference AAD ledger parser. Serena manual read. Graph generation 2026-10-05T14:03:21Z; six new paths not_tracked; direct source fallback used, no graph absence claims.
- Positive evidence: full authenticated ARE-to-AAD derivation, original World clones, native normalized arithmetic catalogue, independent paid replay, owned results, fresh-only support certification, native RNG, mandatory local windows, reference fixture parity assertions, complete isolated settlement matrices.
- Concern P2 candidate: CampaignCombatActualAssaultAdmission.Admit catches every predecessor JsonException except009 and converts it to004 without independently typing the supplied selection/round ledgers first. Canonical paid spec explicitly requires malformed supplied independent ledgers retain001/002/003. Reference AAD ledgers() types them before predecessor admission. Existing tests check null/missing/reordered ledgers and malformed paid input, not malformed non-null predecessor inputs. Reproduction pending.
- No core mechanical or authority finding yet. Fixed seed1/cursor2 means generalized RNG states remain isolated and are not ingress.
- Focused test first invocation blocked by sandbox IPC (exit134); escalated wildcard spanning both class names selected zero tests (exit5), no verification credit. Correct genuine-class invocation now running; exact results pending.

## Findings

### P2 — Validate independent predecessor ledger primitives before mapping replay errors

Evidence: `src/Cna.Core/Campaigns/CampaignCombatActualAssaultAdmission.cs:17–20`; upstream `CampaignCombatActualPaidAssault.Replay` checks ledger/null ownership but parses only paid inputs. `Admit` submits selection and round byte lists directly to ARE, then maps every JsonException other than ARE009 to APA004. ARE itself collapses selection replay failures into ARE004. Therefore simply forwarding ARE error suffixes would still leave the selection-ledger defect.

Failing scenario: with an unchanged genuine PaidSource, authentic creation context and all physical pins, replace the first independently supplied selection or round input's `admittedAt` with boolean true, with -1, or its command version with2. A scratch C# probe invoked the same native AAD adapter from the frozen Release assembly. All six cases returned CMB-APA-004. Round cases retained inner ARE001/002/003; selection cases retained inner ARE004.

Required behavior: canonical paid spec's Diagnostics section explicitly says malformed supplied independent ledgers retain001/002/003 rather than being hidden as004. Immutable reference `verify-combat-actual-paid-assault-v1.py:175–176` calls AAD `ledgers()` and forwards its diagnostic before predecessor comparison; `verify-combat-actual-assault-v1.py:145–158` types both ledgers first. Expected codes are001 for boolean time,002 for negative time,003 for version2.

Impact: fail-closed rejection still occurs, but native read/apply ingress violates the frozen diagnostic contract and NP01/NP09. This is not a new authority bypass or lost paid resources. Correct by validating and owning both independent predecessor ledger shapes/versions/primitives before calling inherited replay, preserving source/context mismatches as004. Add six regressions through native paid read/apply entry points, including dependency009 precedence and coherent mismatch004. No predecessor/shared engine change or heavy architecture pivot is required.

No other actionable defect found in this pass.

## Plan Review

NP01–NP03: native AAD proof derives full authentic ARE Prepared25 lineage and selected rules/catalogue; full paid histories and genuine cuts match immutable event/source/final proof bytes. Retained semantic RED log confirms four actual007 failures rather than compilation failure. NP01 primitive error acceptance remains incomplete due to finding above.

NP04–NP07: support runs before fresh debit, covers exact16616 ordered native facts and all summarized equivalent mechanical outcomes; fixed production handlers/capacities and failure probes fail before owned-state mutation. Original World projection matches immutable oracle mechanics, preserves origins/component binding/representation changes/future duties, applies asymmetric loss/capture/refusal/guard conservation, and never invokes synthetic Round2 authority. Both9728 matrices, twelve RNG vectors, conditional ninth draw/rejection/block crossing/atomic exhaustion, both owner windows and local versus audit clocks have independent assertions. Actual seed1/cursor2 remains enforced at ingress.

NP08–NP10: reconstruction regenerates each event from separately retained paid inputs, validates context before retry/fallback, handles disabled-fresh recovery and valid stale timers, and returns copied byte arrays and immutable JSON clones. Grammar has closed shapes, explicit nulls/unions, inherited versions, preparse depth/array and byte limits. All64 physical acquisition paths retain before-ingress and before-publication rechecks. Wire62 and AAD59 identities remain unchanged; the two owned delivery pins are distinct. Native grammar coverage misses malformed non-null predecessor inputs, as described above.

Canonical README/tech-design/Task019F6 remain consistent with private immediate scope and preserve production ingress, Release execution, later/repeat/finish/consumed Reserve, public/host/Exercise, Snapshot/Archives and parent closure exclusions. No package/workflow/reference schema/oracle/fixture change in base-to-head diff. Significant implementation size is contained in one existing private contract slice with separate codec/support/admission/transition responsibilities; no scope-splitting gate is warranted.

## Author-Claim Reconciliation

| Claim | Evidence | Status | Consequence |
| --- | --- | --- | --- |
| Native full lineage and three independent ledgers; no supplied proof authority | AAD admission and paid Replay/ReadControl/ReadProof; fixture parity/recovery | Confirmed | No authority bypass found |
| Stable001–009 errors and independent-ledger error precedence | Admit catch, canonical spec/reference and six scratch cases | Contradicted | P2 correction required |
| Support before debit; native actual World and RNG | CertifySupport/PaidWorld/Resolve/Project; executed branch/support/RNG tests | Confirmed | Fits private scope |
| All25 focused tests pass; tests prove exhaustive isolated mechanics rather than alternate genuine histories | Executed8+17 tests; test source | Confirmed | Passing suite misses reported malformed-ledger cases |
| Runtime/test blobs equal faab04e; canonical docs equal ac20312 | Empty corresponding Git diffs; six current SHA256 values match focused receipt | Confirmed | Candidate scope frozen |
| Complete Release inventory2795 and historical RED/build/format gates | Retained inventory/receipt/log, not independently rerun full inventory/build/format | Documentary evidence only | No broad gate or CI approval inferred |
| Physical64 closure with wire62 and predecessor59 unchanged | Source pins/delivery maps, dependency callback tests, physical closure receipt | Confirmed within scoped checks | No new external acquisition found |
| Parent/public completion remains open | Canonical docs and private internal APIs | Confirmed | No parent waiver |

## Verification Performed

Worktree `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`. SDK10.0.400; SDK-style net10.0, native Microsoft.Testing.Platform, xunit.v3.mtp-v2 4.0.2. Applied independent-review, code-review-and-quality, codebase-memory, run-tests/platform-detection and binlog-generation instructions. Read Serena manual; CCE tools unavailable, stale graph coverage handled with direct source.

1. `git status --short`; `git rev-parse HEAD`; full base-to-head diff/stat and canonical docs; `git diff faab04e510c5a60df938fa75528837aff92d4a82 HEAD -- src tests`; `git diff ac2031203b8e56bf6d7dc893302f6589dd6a07be HEAD -- README.md tech-design.md docs/design/combat-cycle-implementation-plan.md`. Exact head verified, both checkpoint comparisons empty; only unrelated `.serena/` untracked throughout.
2. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --no-restore --filter-class '*CombatActualPaidAssault*Tests' --no-ansi '/bl:/private/tmp/native-paid-review-1-{}.binlog'`: sandbox IPC bind denied, exit134; escalated retry ran zero tests, exit5. Harness/environment observations only, no pass credit.
3. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --no-restore --filter-class '*CombatActualPaidAssaultTests' --no-ansi '/bl:/private/tmp/native-paid-review-1-genuine-{}.binlog'`: exit0,8passed/0failed/0skipped,41.648s, below120s. Binlog `/private/tmp/native-paid-review-1-genuine-20261011-204536--10167--UhmLPN-dotnet-test.binlog`.
4. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --no-restore --filter-class '*CombatActualPaidAssaultContractTests' --no-ansi '/bl:/private/tmp/native-paid-review-1-contract-{}.binlog'`: exit0,17passed/0failed/0skipped,49.466s, below120s. Binlog `/private/tmp/native-paid-review-1-contract-20261011-204647--11441--hzEz0O-dotnet-test.binlog`.
5. Scratch probe only under `/private/tmp/native-paid-review-1-probe`, no checkout edits. `dotnet build /private/tmp/native-paid-review-1-probe/Probe.csproj --configuration Release '/bl:/private/tmp/native-paid-review-1-probe-{}.binlog' --output /private/tmp/native-paid-review-1-probe`: initial harness CS5001 because output-root default exclusion omitted Program.cs,1.36s; explicit Compile item corrected harness, exit0/0warnings/0errors,21.54s. Binlogs `native-paid-review-1-probe-20261011-204635--11258--xNF2rd.binlog` and `native-paid-review-1-probe-20261011-204708--11788--TPRkpJ.binlog`.
6. `dotnet /private/tmp/native-paid-review-1-probe/Probe.dll`: exit0,0.298s. Boolean time / negative time / wrong version in each of selection and round ledger all returned APA004. Program.cs and Probe.csproj retained for reproducibility; adapter invoked from unchanged Release Core assembly using test helper's authentic creation context and immutable fixture case0.
7. Current six source/test SHA256 values equal author focused receipt. No broad suites, Python verifier/full gate, fixture regeneration, production edits, commits, pushes or new reviewers/workstreams performed. GitHub full.NET CI is Brain's separate gate.

## Open Questions And Residual Risks

Fresh exact-head GitHub CI not claimed here. Historical owning failures, ExerciseRunner aggregation and CoreB139 remain unwaived. Existing passing tests plus source review establish scoped private native behavior, not production authentication or full-cycle reachability. Catalogue/JSON duplication and repeated bounded ingress reconstruction are maintenance costs explicitly documented; no speculative defect elevated from them. This reviewer did not re-run the historical full owning chains or complete inventory discovery.

## Verdict

**Not ready** — one confirmed P2 violation of the immutable native diagnostic contract. All25 existing focused rows pass. Remediation is small and within accepted scope; no heavy-pivot gate.

## Recommended Next Actions

Brain should return this finding to author for Accept / Dispute with evidence / Defer with owner and rationale. Validate predecessor ledgers before replay and retain focused regression evidence, then decide whether material changed behavior needs review instance2/9. Return to Brain before any next review; this instance is complete and counters are not reset.
