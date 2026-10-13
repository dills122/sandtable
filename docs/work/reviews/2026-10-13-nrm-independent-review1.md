# Independent review: NRM01 / PR 181

Review instance: 1 of 9. Round 1/pass 1. Recovery 0/2.

Exact reviewed head: `854d23eda197ca8e46590583114e89d173019fcf`.
Base: `1ef2d038125eb03ed39b6e08aeea5e3ada75983c`.
Branch: `codex/native-actual-repeated-movement`.
Worktree: `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`.

## Findings

No actionable findings. No heavy pivot indicated.

Reviewed both new native files, all three new test classes, fixture copy link, canonical documentation changes, frozen spec/schema and relevant ordinary-movement/cycle-control oracle kernels. No product, documentation, staging or commit edits made. Only untracked `.serena/` was present; excluded from review.

## Plan Review

NM01–NM08 remain a coherent bounded slice. Git history shows the dated plan and genuine semantic RED in `fea1162`, implementation in `bca68dd`, then administrative review/inventory documentation in the exact published head. RED engine authenticated native PAC ancestry, required fifth-ledger shape/size/count, parsed the new input, then deliberately failed 007. Retained RED log records four genuine 007 failures, rather than compile failure being presented as behavioral RED.

NM03 parity tests check four genuine owner/seal roots, sixteen independently reconstructed events, twenty Control cuts, final Source/Proof bytes, and expected Proof at every cut. NM04 tests cover every prior command at all later cuts, original receipt, no new event, unchanged ledger/Source/Control/Proof and owned returned values. Resource tests assert CP5/9/11/13/15, incremental DP0/1/2/2, signed cohesion0/0/-1/-3/-5, first Contact termination, route/progress and preservation. NM05 covers context/grammar/causal claims, authentic changed round clock with regenerated native lineage, Reaction and fifth CP17 rejection, and all68 physical deliveries. NM06 dedicated tests exercise all older commands across8 repeat/finish roots and32 later/terminal cuts; timed commands vary valid clocks while untimed commands retain valid shape. No predecessor runtime changes.

NM07 author build/format/discovery evidence is retained and scoped. NM08 independent review and exact-head CI/merge remain Brain responsibilities; discovery is explicitly distinguished from test execution. Documentation preserves public/lifecycle/Reaction/remaining-lineage exclusions and does not infer parent017–025/all72 completion.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Full native ancestry before fifth-ledger/claim validation | Engine Replay/Derive invokes native PAC replay with context and four retained ledgers, then validates fifth inputs; ReadControl/ReadProof replay before claim parsing | Confirmed |
| Context and typed command checks precede every retry; retries precede current legality/version | Transition canonicalizes state/input, binds kind/Base/cycle/position/actor/UnitKey, searches all retained receipts, then tests new-command version/location | Confirmed |
| Ordinary policy port preserves frozen behavior | Compared port with frozen `mov.affected`, `terrain_cost`, `cost` and `policy.excluded`: supported Clear terrain2, max breakoff2/4, ceiling15, incremental excess DP, sorted relationship endings and original proof distance/exclusions | Confirmed within authenticated bounded profile; no broader profile claim |
| No fixture/Python/synthetic runtime authority | Exact source consumes PAC replay and pinned content, regenerates events; new paid/PAC/Movement fixtures occur only in tests | Confirmed |
| Original authority/proof/resources remain while only new moves count as progress | Initial clones retained provenance; Transition changes owned state locations/CP/cohesion/causes/Contact/track/version/prefix/receipt/progress only | Confirmed by source and independent scoped tests |
| Wire66 + own delivery2, all232 frozen specs unchanged | Independent static hash acquisition; compared external manifest map; byte comparison of all baseline docs/specs paths | Confirmed:68 valid pins,66 matching wire pins,232 unchanged files |
| Ownership and repeated physical validation | Result constructor/getters copy byte arrays and ledger entries; JsonElement clones; acquisition copies before hash; dependency checks surround replay/publication | Confirmed |
| Final36 new scoped rows pass | Independently executed three class filters below | Confirmed independently; author outcomes not substituted |
| Release build/format/full discovery | Inspected retained receipts and exact source identities | Retained testimony; not independently rerun, no broad execution claim |

## Verification Performed

Applied independent-review, code-review-and-quality, codebase-memory, run-tests and binlog-generation guidance. Read Serena initial instructions. Graph generation2026-10-05 predates all five changed source/test paths; coverage reported not-tracked and excluded docs. Exact-source fallback used; CCE unavailable. Preliminary blind ledger `/private/tmp/nrm-independent-review-1-preliminary.md` written before rationale or self-review was read.

All focused commands used the repository bounded runner,120-second process limit, Release/native MTP/xunit4.0.2, sequential execution and unique binlogs. Command template:

```text
python3 docs/work/reviews/2026-10-13-nrm-bounded-run.py NAME 120 dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --filter-class FILTER /bl:/private/tmp/NAME-{}.binlog
```

- `NAME=independent-1-movement-escalated`, `FILTER=*CombatActualRepeatedMovementTests`:16 passed,0 failed/skipped,57.462s. Receipt `/private/tmp/nrm-independent-1-movement-escalated.json`; log same stem `.log`, SHA256 `d9ab0efcbfb200eaefe15483639e24ebc587702dfc74a4885aa99f3a57e6589c`.
- `NAME=independent-1-contract`, `FILTER=*CombatActualRepeatedMovementContractTests`:12 passed,0 failed/skipped,59.218s. Receipt `/private/tmp/nrm-independent-1-contract.json`; log same stem `.log`, SHA256 `a856aceff22f899b594a54b400870f240596f61ae8188516c78255db99fea24f`.
- `NAME=independent-1-p3`, `FILTER=*CombatActualPostAssaultContinuationRetryTests`:8 passed,0 failed/skipped,62.094s. Receipt `/private/tmp/nrm-independent-1-p3.json`; log same stem `.log`, SHA256 `86209834d98301298d6e1457f5c34882f8f3a98e05bba163e09e4007661778c9`.

Initial sandbox Movement attempt failed before test execution with local named-pipe/socket Permission denied, exit -6,0.630s. Retained `/private/tmp/nrm-independent-1-movement.json` and `.log`. Approved escalation resolved the environmental restriction; no product failure inferred.

`git diff --check BASE..HEAD` passed. Six source/project identity records independently matched retained product snapshot. Static evidence `/private/tmp/nrm-independent-1-static.json` and `/private/tmp/nrm-independent-1-source-identities.json` records hashes, pin comparison and232-file comparison. Baseline continuation source/codec have empty diff; administrative commit changes no product/test code. Test binaries used SHA256 Core=`bb7e04c485713d3e3836c407e5d35df3c380dbd999c941b364b9a26555728553`, Core.Tests=`77e304384e94e1a5d8383ddeb07bc64fa174e927a73ab75598442492f2508b8a`.

## Open Questions And Residual Risks

No unresolved correctness question in authorized slice. Full ancestry replay and repeated hashing/cloning are expensive; scope remains dormant/private and makes no production latency promise. Bounds checked against frozen grammar; this review did not construct exhaustive near-limit authenticated ancestor variants. Build/format/broad suite/Python normal were not rerun. Exact-head CI remains a separate required delivery gate, with historic owning failures unwaived. Public ingress, stop/completion and broader profiles remain explicit future contract work.

## Verdict

**Ready** for the frozen private native move-only slice. No actionable finding; exact-head CI remains the separate Brain delivery gate.

## Recommended Next Actions

Brain should reconcile this report and finish exact-head CI/publication gates. No remediation or additional reviewer dispatched by this reviewer.

## Exact executed focused commands and binlogs

```text
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --filter-class '*CombatActualRepeatedMovementTests' '/bl:/private/tmp/nrm-independent-1-movement-{}.binlog'
```

Binlog: `/private/tmp/nrm-independent-1-movement-20261013-133420--80597--PLmSdk-dotnet-test.binlog`, `/private/tmp/nrm-independent-1-movement-escalated-20261013-133440--80968--Z_y+Q5-dotnet-test.binlog`

```text
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --filter-class '*CombatActualRepeatedMovementTests' '/bl:/private/tmp/nrm-independent-1-movement-escalated-{}.binlog'
```

Binlog: `/private/tmp/nrm-independent-1-movement-escalated-20261013-133440--80968--Z_y+Q5-dotnet-test.binlog`

```text
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --filter-class '*CombatActualRepeatedMovementContractTests' '/bl:/private/tmp/nrm-independent-1-contract-{}.binlog'
```

Binlog: `/private/tmp/nrm-independent-1-contract-20261013-133555--81958--evhtV0-dotnet-test.binlog`

```text
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --filter-class '*CombatActualPostAssaultContinuationRetryTests' '/bl:/private/tmp/nrm-independent-1-p3-{}.binlog'
```

Binlog: `/private/tmp/nrm-independent-1-p3-20261013-133704--82703--QjJM9w-dotnet-test.binlog`

