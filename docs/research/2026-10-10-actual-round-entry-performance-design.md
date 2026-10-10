# Actual round entry: bounded performance and cache design investigation

Date: 2026-10-10 America/Toronto. Status: read-only diagnostic recommendation for Brain; no implementation approval or independent-review verdict. DAY-A review remains 0 of 9, recovery 0 of 2; this is a pre-review investigation, not a failed-review recovery. Timebox: 30 minutes starting about14:06 Toronto; evidence sufficient before expiry.

## Decision and smallest recommendation

**Decision owner:** Brain chat `01a0c9dc-00bc-78a3-800d-3cb36859e422`. Question: does repeated actual-round replay plausibly explain PR169's 15-minute CI timeout and incomplete45-minute local Core run, and what smallest correction preserves the frozen contract and every acceptance assertion?

**Inference:** yes. Exact mutation inventory and two measured assertion-preserving cases identify a substantial repeat workload, rather than relying on a single microbenchmark. The mutation method alone has39,652 public calls; prepared cases0/17 take72.719/74.226 seconds for1,148 calls each. Their average extrapolated across that inventory is42.3 minutes. This is a sensitivity estimate, not measurement of cancelled cases, a CPU attribution percentage or the complete suite. It makes the observed unfinished local run credible; it does not prove the corrected suite will fit CI.

**Recommended staged decision:** authorize, if Brain accepts this note, a small local optimization in the two new native files first: compute authenticated BaseHash, ClockHash and Route once per replay/frame, and reuse test-local decoded `prior.Control` where repeated. Retain all inputs, cases, public-API calls and assertions. Measure the existing mutation cases0/17 again before another full suite. If the remaining work is insufficiently reduced—as the large repeated readback workload suggests—authorize **one bounded successful replay-frame memo** in the new engine, optionally retaining already-produced canonical read-only result bytes in that same entry. Re-measure hit/miss work and the same cases. A separate derived-original Base memo is a third step only if measured misses still dominate; it is not needed to demonstrate correctness of the first cache and is not approved by this research.

The evidence is already sufficient to choose that staged correction. No broader profiler capture or extra .NET run by this lane is essential. Further measurements are implementation checkpoints under the author's sole lease, not grounds to widen this research. No timeout increase, dropped assertions, representative-only replacement, predecessor/shared-file change or pin refresh is recommended. If those scoped corrections cannot satisfy the full suite and exact-head CI, return the measured constraint to Brain rather than relaxing the gate.

## Scope, method and source index

Author owns product/test/canonical-plan files and the .NET lease. This lane writes only this dated note in `/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable`, new branch `codex/actual-round-entry-performance-research`, from preserved DAY-B commit `eca7739fd09a3c15132f38a4b826fbf2931df6be`. Original frozen DAY-B report remains unchanged. No build/test/profile process, agent, implementation, commit, push, PR or merge was started here.

Inspected author worktree `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`, branch `codex/native-actual-round-entry`, product commit `a37fbd8ea0fa9a138c369eea1d55929bee41b951`. Observed pending test diff adds73 lines for prospective cache work/ownership/Content checks and14 original fallback variants. Those additions were unbuilt initially; subsequent prospective RED was relayed by Brain. They are not product cache implementation or passing domain acceptance. Engine/codec bytes still match the committed head.

Source hierarchy: frozen repository contract/oracle and committed native source; retained author logs and exact inventory; independent cheap fixture/source arithmetic; labelled inference. No external framework claim or new architecture authority is inferred. Serena was activated for the assigned author checkout. Codebase Memory project `sandtable-actual-selection-contract` is ready but dated2026-10-05; graph lookup returned no new round symbols, and all five cited round/selection paths report `not_tracked`. Direct symbolic/source reads supply those gaps; no reindex was started against the active author's checkout.

| Ref | Evidence and responsibility |
| --- | --- |
| F1 | [Frozen ARE contract](../specs/combat-actual-round-entry-v1.md), Admission and transitions84–91, Recovery165–168; [oracle](../specs/verify-combat-actual-round-entry-v1.py), replay284–296 and apply300 onward. Specifies57 pins before any lookup and Content at use, including warm recovery; immutable full-source/two-ledger cache is explicitly permitted. |
| F2 | [New engine at a37fbd8](https://github.com/dills122/sandtable/blob/a37fbd8ea0fa9a138c369eea1d55929bee41b951/src/Cna.Core/Campaigns/CampaignCombatActualRoundEntry.cs), Result58–70, Replay72–90, DeriveBase92–116, Transition149–289, EmptyAa291–322, BaseHash/ClockHash/Route328–330. |
| F3 | [New codec at a37fbd8](https://github.com/dills122/sandtable/blob/a37fbd8ea0fa9a138c369eea1d55929bee41b951/src/Cna.Core/Campaigns/CampaignCombatActualRoundEntryCodec.cs), Parse14–28, canonical30–36, readbacks39–58. Readers independently parse claimed bytes before comparing a replayed result. |
| F4 | [Committed tests](https://github.com/dills122/sandtable/blob/a37fbd8ea0fa9a138c369eea1d55929bee41b951/tests/Cna.Core.Tests/Campaigns/CombatActualRoundEntryTests.cs), exact cuts/retries, every leaf, independent original history, lifecycle/clock matrix and dependency sensitivity; current uncommitted additions are distinguished below. |
| F5 | CreationContext owns RulesetHash/Setup/Configuration in `CampaignCombatCreationRequest.cs`; trusted Setup/config codecs serialize the full supported records, including artifact identity/scenario and configuration windows. Content artifact creates owned canonical bytes and derives identity from them. These are existing typed trust boundaries, not a newly supplied Base. |
| O1 | `/private/tmp/native-round-entry-profile/Program.cs`, author reflection wrapper loading existing test/Core binaries; stages use two warmups/eight samples. Mutation mode invokes the existing test method with all assertions. Logs bind Core DLL `b4e40978edf9a9d198734ee539f86c87dd09b4b566d2dde17e313a7680f28d86` and fixture `fc17cda07618bdd539a56b8c296037bbf36f37c6eb2ce6258c2b789b765f640b`. |
| O2 | Author `mutation-call-inventory.json`, two `mutation-case-*-before.log`, profile logs, original CI/full-solution logs, runner module log and prospective cache RED; hashes retained below. Observations were inspected/relayed, not rerun by this lane. |
| O3 | Independent static fixture enumeration with the test's Leaves semantics, plus source/commit hash comparison and physical pin-file sizes. Temporary result `static-counts.json`; detailed arithmetic below is durable. |

## Measurements and workload

**Observed author measurements:** macOS26.6.2 arm64, existing Debug binaries. With `TieredCompilation=0`, two warmups/eight samples:57-pin preflight median4.4953 ms; original selection replay23.0255 ms; prepared round replay62.6618 ms. Stages overlap, so their medians must not be added. Default-tiered samples were noisy (prepared median68.3637 ms, old selection77.10155 ms); they do not prove a separate77 ms component inside a68 ms operation.

| Existing mutation test | API calls | Complete method seconds | Result |
| --- | --- | --- | --- |
| Case0, prepared Axis |1,148|72.7189261|All retained assertions pass|
| Case17, prepared Commonwealth |1,148|74.2256305|All retained assertions pass|

**Independent static arithmetic:** the same leaf recursion traverses every object field/nonempty array; null and empty arrays each contribute one leaf. It matches the author's exact mutation inventory:

| Major loop family | Public calls |
| --- | ---: |
| Mutation event Replay |8,016|
| Mutation round-input Replay |2,510|
| Mutation proof ReadProof |23,522|
| Mutation control ReadControl |5,604|
| **Mutation subtotal** |**39,652**|
| Lifecycle clock table (2 owners×2,952) |5,904|
| Four reads per original cut (224 cuts) |896|
| Original suffix Apply |630|
| Four exact retry variants |2,520|
| Malformed/foreign retry overlays |1,260|
| Original history/selection leaf Replay (2 roots) |2,028|
| Terminal Replay |34|
| **Named-loop subtotal** |**52,924**|

This is invocation inventory, not instrumented successful Replay count. It excludes focused grammar, timers, privacy, terminal/fallback additions and other methods; early syntax failures may avoid full replay. Proof/control mutations nevertheless reuse the same authentic source/context/ledgers across thousands of independent supplied-byte comparisons, making them the clearest frame-hit opportunity. Event/input mutations usually require new frame authentication, but share an unchanged original predecessor. Cancelling cases differ; no measured prepared case is promoted to complete cancelled-row coverage.

Mean measured prepared-case time73.4722783s /1,148 calls ×39,652 =2,537.74s (~42.3min). Named calls ×prepared replay median =55.3min is a weaker sensitivity, not a duration forecast. The lifecycle matrix alone at that median would be~370s, again overlapping/different-case evidence rather than a sum.

Every full57 preflight reads/hashes6,867,272 physical bytes; largest pin is the5,640,101-byte actual-selection fixture. Removing only one redundant4.4953 ms preflight from every named call would save~238s on this local arithmetic, far less than the mutation estimate. Mandatory pin validation cannot be skipped or inferred from unchanged paths/mtimes. A successful memo still pays preflight, key/snapshot/parse and ownership costs; uncached savings do not establish a fast hit.

**CI:** original log starts tests17:12:06.854UTC and cancels17:25:02.033UTC (~775s test allowance after setup/build). Contracts pass; ExerciseRunner passes3m39.052s; Core is unfinished. Previous DAY-B exact-head verify took14m13s and passed2,593 tests, leaving little total-job margin. Different platforms/configuration/scheduling mean local timing cannot simply be added to that CI duration.

**Other failure:** original local full-solution log retains ExerciseRunner `AggregationFailed` while Core remained incomplete and the session was cancelled. Isolated1/1 and whole module469/469 now pass (module3m22.061s, summary3m22.295s). This does not diagnose or waive the original aggregation failure. The native process sample has many unresolved managed frames (2,879 unknown-binary lines); it does not attribute a CPU percentage to replay, locks or GC.

## Options and staged correction

| Option | Benefit and limitations | Disposition |
| --- | --- | --- |
| Test-local immutable fixture/dependency snapshots and decoded prior.Control | Avoids repeated canonical parsing by result getters; existing `DependencyLookup` already returns owned in-memory copies. All assertions and public calls remain. No global fixture/delegate sharing that contaminates tamper tests. | Small complementary cleanup; cannot remove29,126 source-authenticating proof/control calls. |
| Per-call authenticated BaseHash/ClockHash/Route facts | Initial/Transition repeatedly canonicalize/hash the same derived Base and enumerate the same route. Compute only after existing authentication, at the same semantic validation stage; keep immutable facts and owned mutable state. No cross-call invalidation or shared trust boundary. | First narrow product step if authorized; measure cases0/17 before escalating. Do not claim sufficient speedup in advance. |
| Deduplicate repeated preflights | Some Apply/readbacks validate57 pins twice; changing internal call routing could avoid a repeat. Cannot bypass preflight before malformed claimed bytes or cache lookup; at-use Content remains separate. Current Apply at-use test explicitly relies on two passing preflights followed by consumption. | Not the first correction. Does not address most replay work and risks error/callback-order changes. Preserve current regression assertions; do not rewrite them to fit a desired count. |
| One successful replay-frame memo, optional already-produced read-only result bytes in same entry | Eliminates repeated complete history authentication for identical source+ledgers+context. ReadProof/ReadControl must still parse and compare each fresh supplied claim; Apply still executes its fresh transition. Canonical frame bytes allow per-call owned state without sharing mutable JsonObjects. | Second staged step, strongly justified if locals leave major cost. Brain approval and full acceptance tests required. |
| Independent derived-original Base memo | Benefits unique forged round/event/input misses that share complete genuine original selection source/ledger/context/clock. More key, ownership, capacity and invalidation logic; must not retain fallback/unselected Base or infer round authority. | Defer until remaining miss measurements justify it; no predecessor/shared implementation change. Per-call facts may eliminate some need. |
| Global result reuse by hash/World, callback identity, mtime, sampled tests, longer timeout | Conflates authority, loses mutable-dependency sensitivity or reduces acceptance. Current-input/clock/admission outcomes cannot be cached by historical frame key. | Reject. |

## Is the prospective58-callback RED justified?

The author observed74 callbacks on successful warm Prepared Replay:57 new-family pins,16 original-selection pins,one AA Content consumption. Prospective test expects58 and fails/exit2,3.255s (`cache-work-red.log`). This is an optimization RED, not a preexisting domain regression, and its other assertions may not have executed after the first failure.

**Inference grounded in F1:**58 is a justified successful warm `ReplayTrustedSource` work target **if** all57 physical pins run first, a hit identifies exactly the previously successful complete source/two raw ledgers/trusted context, and one explicit at-use Content check preserves AA sensitivity. The original16 are covered by the ordered57 preflight; identical owned authority/context has already passed the predecessor guards. The frozen oracle likewise skips predecessor replay only after complete-key admission. Returning to cold authentication on every miss preserves those guards.

The number is not an independent requirement merely because a test asserts it. It is not universal across cold replay, Apply, malformed readers or non-AA frames. Preserve the existing source/ledger/config/version/capacity precedence. Preserve Apply's two-preflight then Content-consumption regression unless Brain approves a separately justified internal routing change that retains every sensitivity assertion. Warm AA evidence is determined from authenticated retained transitions; a prepared state before AA consumption does not acquire an AA flag merely from its status.

## Cache correctness conditions

1. **Complete owned key.** Include full canonical raw source, both complete ordered raw ingress ledgers and typed trusted Setup/configuration serialization plus RulesetHash. Setup serialization binds Content identity/scenario; audit every replay-reachable context field and prove its binding rather than keying only a supplied hash, Base or world. Use unambiguous domain/length/count framing; if a digest indexes entries, retain/compare full framed evidence. Do not normalize malformed raw ledgers into a valid key. The dependency callback identity is not authority; its current physical bytes are independently checked every call.
2. **Existing order first.**57 ordered checks precede even warm lookup and cheap malformed-source/readback rejection. Apply parses its current input before history replay as today; readers parse claimed Control/Proof before their replay. New key construction must not move raw ledger/context errors earlier, expose runtime exceptions, or allocate unbounded buffers before existing limits. If key eligibility is uncertain, follow the unchanged cold path; do not invent a new admission rejection merely to satisfy cache policy.
3. **Only authenticated success.** No exceptions, partial transitions or unsupported fallback predecessors are retained as successes. Avoid new proof serialization/size validation before the point where the current API already performs it; doing so could move a size error before current-command lifecycle rejection. Optional memoized result bytes are captured only after their original successful production point. No cache of Apply outcomes by frame key: current input, unavailable/deadline clocks, admissionEnabled and timer/owner races remain fresh work.
4. **At-use dependencies.** Mark consumed AA from authenticated events. On warm recovery of such frames check exact Content bytes again after passing preflight, including changed/missing bytes. On a warm pre-AA frame whose fresh transition completes AA, EmptyAa still performs the real at-use read. Dependency failure009 must defeat a cache hit and malformed/conflicting claim as the contract requires.
5. **Ownership.** Store private canonical bytes/immutable facts. Rehydrate or clone mutable per-call Packet/Base/Control/events; Transition must never mutate retained cache objects. All returned proof/control/source/event and retry buffers remain owned copies. Do not retain JsonElements backed by disposed documents. Snapshot caller source and list items once and use that same owned evidence for key and authentication; do not borrow mutable arrays or normalize caller mutations.
6. **Concurrency.** Short locks cover dictionary lookup/insertion/eviction and accounting only. No dependency callback, I/O, full replay, context serialization or arbitrary external function under the lock. Concurrent misses may compute twice; only complete owned entries are atomically published, with cap recheck on insertion. Hit/eviction races cannot return partially cleared buffers. Reentrant callbacks must not deadlock. Keep changes in the new family, without broad shared cache infrastructure.
7. **Memory is bounded.** Bound entry count, total retained key+value bytes, per-entry size, and transient key construction. An oversize entry bypasses memoization and follows valid cold admission; cache policy must not narrow the accepted contract. Eviction changes performance only, including disabled-fresh-admission recovery.224 fixture cuts alone retain~33.4MB if source, ledgers, Base, Control, proof and events are all duplicated, before context/framing/object/string overhead. A256-entry limit alone is not a meaningful byte bound. Measure actual retained buffers and avoid duplicate storage; do not freeze a budget from this estimate.

## Exact acceptance and measurement gates

**A — locals stage:** retain all34 independent terminal literals, every original cut/suffix/retry, byte conservation/no draws/no costs, all39,652 mutation assertions and5,904 lifecycle outcomes. Re-run cases0/17 with the same binary mode, assertion count, environment and before/after DLL/source hashes. Optional test-local decoding must still compare full outputs and every hidden-fork outcome; no direct kernel substitution for public admission tests.

**B — memo stage TDD:** prospective warm58 target with complete returned-byte equality and Content-last callback; cold/warm all57 tamper/missing pins before malformed source/readback/retry; warm altered or null/truncated/reordered original and round ledgers; full-source mutation and cross-owner histories; valid foreign Setup/config/window/rules context cannot hit. Test fresh claimed proof/control mutations after a warmed genuine frame, including re-signed Base and correct local hashes. Existing current input/version/owner/clock/unknown-effect conflicting error matrix must remain exact. Cases require both owners/seal orders and cancelled cuts, not only two prepared cases.

**C — at-use/ownership:** preserve both current post-two-preflight AA tests; warm prepared and warm pre-AA completion with changed/missing Content, cold and repeated; never return a stale certificate. Mutate every returned buffer, caller source and both ledgers, then reproduce the original independently supplied source. Retry ownership and no-op/version/prefix remain exact. Keep the14 original fallback variants×five APIs rejection test.

**D — concurrency/capacity:** parallel same-key hits/misses and different owners/ledgers/contexts must match independent literals; returned mutation in one caller must not corrupt another. Bound byte/entry/per-entry accounting under concurrent insertion/eviction and valid supported clock variants; no seed/profile expansion. Force eviction then recover every representative cut with fresh admission disabled, checking unchanged output/error precedence. Oversize-but-valid cache candidates still authenticate cold; malformed capacities remain contract errors.

**E — performance evidence:** retain deterministic work counts and before/after wall-time observations, not timing-sensitive unit assertions. Measure cold hit/miss, warmed prepared/cancelled Replay, readback comparisons and unique event/input misses separately. Split remaining preflight/key/parse/clone/result cost before considering the second memo. The two prepared methods are sufficient to assess the initial correction; one cancelled representative is needed before claiming broad speedup, and full workload/suite is the actual gate. No predicted savings replace exact corrected-head .NET/build/format/full solution, independent review and required15-minute CI. Only the author runs those commands under the existing lease and skills.

## Confidence, unresolved items and self-review

High confidence: complete contract cache constraints, repeat workload, literal source equality, observed timings, and the usefulness of avoiding repeated source authentication. Moderate confidence: the staged local/frame strategy. Unknown: per-stage allocation/CPU shares, cold malformed fraction, cancelled-row timing, warmed key/clone cost and final CI margin. No isolated Runner pass explains its original fullrun aggregation failure; retain that failed observation through final acceptance.

Author self-review of this research: checked source/commit distinction, graph coverage limits, overlapping medians, exact counts/estimates, two prepared versus cancelled evidence, prospective RED versus domain failure,58's restricted interpretation, owner/cap/error-order hazards, and no implementation authority. Corrected the initial broad sensitivity into separately labelled measured and inferred evidence; full-target timing remains unknown. Administrative scope is one new dated note; previous frozen report/canonical plan/product files unchanged. Local note links/whitespace and branch/file boundaries are checked at save. No Ready verdict is issued. Brain owns the staged authorization and DAY-A acceptance/review counters.

## Retained evidence hashes and reproduction boundary

Temporary availability is not assumed. The exact observations, source hashes, workload table and method are retained above; author artifacts below permit audit/reproduction. This lane only ran cheap Python arithmetic/source checks and read-only repository tools. Author wrapper mode `mutation <index>` invokes the named compiled test, preserving all assertions; author must freeze exact build/run commands through repository skills before rerun.

| Source at a37fbd8 | SHA256 | Observed status |
| --- | --- | --- |
| `src/Cna.Core/Campaigns/CampaignCombatActualRoundEntry.cs` | `66a63bf0797da50aa653ea5bba9bfafd0eb9ffb70b375547a55758f2f0be5cbf` | Unchanged |
| `src/Cna.Core/Campaigns/CampaignCombatActualRoundEntryCodec.cs` | `e224bb57183b1b0febf9b90a297744e9ce99cf3b4124ce5d320eb3fb60e48065` | Unchanged |
| `tests/Cna.Core.Tests/Campaigns/CombatActualRoundEntryTests.cs` | `04948365c4737eab71e14c81025218a109c8ef4226b5b9aa153a4d2aa2c8d7ca` | Uncommitted additions, initially unbuilt; observed hash b080cfa71926d363291ca00e8cb1d7f49b5e2cf5895fea23dc5e3f3120d01f6d |
| `docs/specs/combat-actual-round-entry-v1.md` | `ed4fd69b4f599acae7c4850270557363a546df36d0b984107c50c08c29304548` | Unchanged |
| `docs/specs/verify-combat-actual-round-entry-v1.py` | `2596531cb50ddbae7f1e606284f3d845b224d4f1fc10f478b717843d1062990c` | Unchanged |
| `src/Cna.Core/Campaigns/CampaignCombatCreationRequest.cs` | `40de99a7e8a0fe9e334ae326971ec1ae49547d90f577cf7373562409abe58074` | Unchanged |
| `src/Cna.Core/Campaigns/CampaignSetupV7Codec.cs` | `4884414f3dac6c28429029b678936a718d700acaf75e0b3c861f0c62aff61e1a` | Unchanged |
| `src/Cna.Core/Rules/CombatDecisionConfigurationCodec.cs` | `21350f63871cbddd6471c39d71937b911a9388f34b2cdeab71a1e20e5c6a20cd` | Unchanged |

| Retained artifact | SHA256 |
| --- | --- |
| `/private/tmp/native-round-entry-profile/Program.cs` | `062521ed1fddf73c9753d1b8a240050d0a2d2f47f41b52e687c618036a35555e` |
| `/private/tmp/native-round-entry-gates/profile-before-optimized-jit.log` | `c14e001badbe96acf45f4d9a8ca9a8bf8fcf6ff1b1b159d92496275cc4785b10` |
| `/private/tmp/native-round-entry-gates/profile-before.log` | `0736ae2651b26e57fe7325ffd44549f5d4fe60a70674b73f606c6d82a575ea70` |
| `/private/tmp/native-round-entry-gates/mutation-call-inventory.json` | `fd9f0e8d8578425f1ab296820ff4e75590d0acedc9e7da4de1b6415fdc7ce6a6` |
| `/private/tmp/native-round-entry-gates/ci-original-head.log` | `34a87a347e42ed92cd68e7c5d29ea0a0fa90eefd372dd4e8b9597a510baa01e8` |
| `/private/tmp/native-round-entry-gates/full-solution.log` | `fafdabb06987f5c75524dec1ff2aeda344add12ecef7ec39d3fa3f8a68491e0f` |
| `/private/tmp/native-round-entry-gates/aggregation-module.log` | `fad7fe20fd814dc2a332f7ceb841e78f91130cbb257b76a2c1a90c14ca665a60` |
| `/private/tmp/native-round-entry-gates/core-native-active.sample` | `720312ebe436ed0dd53780d34029c96a7f2c02652e9681ad3f5e41d4a153ec8b` |
| `/private/tmp/native-round-entry-gates/mutation-case-0-before.log` | `dddfda0052ad10032223bbabc29ed6383dd8feec1ae3380d80d402ef252c8e62` |
| `/private/tmp/native-round-entry-gates/mutation-case-17-before.log` | `c29d6d0bf108679ab8fe859afe34b275ef46c83557bd5e5b0af84e237f917fe0` |
| `/private/tmp/native-round-entry-gates/cache-work-red.log` | `5252cc1180717f319480699735597c646b1d2c880da5f2cde29d91e3510e31e8` |

Canonical fixture hash is `fc17cda07618bdd539a56b8c296037bbf36f37c6eb2ce6258c2b789b765f640b`. New after-build logs may bind different test assemblies while the product DLL stays unchanged; this note does not conflate them. Author tests/worktree continue evolving under Brain, so verify exact hashes before using this snapshot as implementation evidence.
