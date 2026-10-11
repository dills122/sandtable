# Author Explanation — private native actual round entry

**SELF_REVIEW_COMPLETE** for frozen primary checkpoint `64439f79356fd6303408e6a9b3579e530235b9d6`. This is author engineering testimony, not independent approval. **Required Release CI remains unmet: verify cancelled at its unchanged 15-minute limit.** Brain authorized review preparation after complete local acceptance while retaining that delivery blocker. Implementation review counter is 0 of 9; recovery 0 of 2.

Inspect the separate neutral bootstrap and implementation before reading this explanation. Primary hashes are in `2026-10-10-native-actual-round-entry-primary-hashes.json`. The administrative commit containing these packets does not change those five primary files.

## Intent and success criteria

Implement the merged `combat-actual-round-entry-v1` contract as a private Core engine and closed codec. Callers retain the original actual-selection source and independently trusted original and round input ledgers, and supply typed trusted creation context plus physical dependency bytes. Success means exact literal replay/readback and fresh command acceptance for opening, both owner seals, Force Assignment and certified empty AA. Prepared history stops before positive Close Assault. Cancellation follows no-attack steps to Release. World, RNG and paid attack history remain unchanged.

This private adapter is not public gameplay activation or a persistence implementation.

## Plan-to-implementation traceability

[Task019F3](../../design/combat-cycle-implementation-plan.md#task019f3--private-native-actual-round-entry-rel-aud-02c--day-a) owns only the five primary paths. The canonical contract/schema/fixture/verifier and all 57 dependency pins remain unchanged. The engine implements their 17 closed record shapes and exact bytes; tests retain all 34 literal traces, 224 cuts, 630 suffixes, 2,520 retries, 39,652 mutation API invocations and 5,904 lifecycle API invocations, plus privacy, error, ownership and capacity matrices.

All original 171 acceptance cases remain. Six additional result-absent/present rows bring the complete native total to 177. The primary plan records the publication checkpoint when the final full solution was pending; the completed 2,770-test pass below supplements that historical status. No parent017–019 completion is inferred.

Brain authorized per-call authenticated facts, then one bounded successful-history memo, then a guard-preserving same-entry read-only Result shortcut after diagnostic research. Research reviews approved bounded experiments; they are not independent implementation reviews.

## Technical approach and flow

Cold replay checks all physical dependencies, parses the closed source and independently supplied round inputs, then derives Base through unchanged original actual-selection replay. It requires selected version20, step3, decline, three ordered receipts, seven original events and an open lifecycle. It deterministically rebuilds each round event against the trusted ordered ingress ledger. Embedded inputs are consistency evidence, not authority.

Initial state and transitions reuse per-call authenticated Base/clock hashes and route. Current Apply input, actor, command identity, version, clock, admission flag and lifecycle remain fresh validation. Exact retries recover the original accepted event/actor receipt before current clock/lifecycle decisions. No costs or draws are introduced. Unsupported paid/result/future operations reject with007.

One bounded memo retains only completely successful history under an unambiguous length/count/domain-framed key: full raw source, both complete ordered raw input ledgers, typed Setup/configuration serialization and RulesetHash. SHA selects a candidate, then full evidence equality establishes a hit. Eligible input snapshots are used for both keying and authentication. Ineligible or oversized keys authenticate cold.

Every call still performs all existing preflights and canonical/source/version/count/round-input guards. A history that consumed AA rechecks pinned Content after its preflight on every hit. Read-only Replay may return a retained Result before reconstructing mutable Base, Control, events and frame facts. Result-absent hits retain the original hydration/Result path. Apply always uses frame recovery and a fresh transition. Readers still parse and compare each new supplied claim.

## Changed-component walkthrough

| Primary path | Responsibility |
| --- | --- |
| `CampaignCombatActualRoundEntry.cs` | Private deterministic transition/replay/proof construction, per-call derived facts and bounded successful-history reuse. |
| `CampaignCombatActualRoundEntryCodec.cs` | Closed canonical syntax, byte/depth/history limits and trusted readback APIs; inherited shapes delegate to the existing predecessor codec. |
| `CombatActualRoundEntryTests.cs` | Independent literal parity, exhaustive mutations, temporal/error/privacy matrices, ownership and memo authority/resource/concurrency checks. |
| `Cna.Core.Tests.csproj` | Content link for the unchanged literal fixture; no new package or framework. |
| `combat-cycle-implementation-plan.md` | Private slice, checkpoint evidence and explicit future exclusions. |

## Decisions and rejected alternatives

The source-driven private adapter follows existing repository patterns. A decoded Base or Control cannot substitute for the independently retained original history. Per-call facts remove repeated computation without introducing authority. One complete successful-frame key avoids a second Base-cache authority/invalidation rule; exceptions, partial histories, unsupported predecessors and current Apply outcomes are not cached.

The retained read-only Result was already bounded within that same entry. Its shortcut only skips frame reconstruction after the original guards. An early lookup before those guards, a separate Base memo, predecessor/shared edits, timeout increases and assertion reductions are outside the approved correction. Dependency acquisition remains a caller precondition outside authoritative turns; no service, host route, model call or remote provider is added.

## Five-axis author engineering review

### Correctness

The frozen shape inventory, ordered pins and literal outcomes match the native family. Four prepared and 30 cancelled traces, full original-history authentication, both independent ledgers, cancellation routing, exact receipt recovery, error precedence and unsupported boundaries are exercised. Source/round snapshots used for memo keying and authentication own their buffers. Cached results originate only after complete successful replay and original proof construction.

The read-only overload returns a null Frame only after assigning a retained Result; its caller selects that Result. Apply's wrapper always passes `readOnly:false` and receives a Frame. A result-absent hit reconstructs state and computes the proof normally. Six new rows establish both behaviors, complete literal bytes, ownership, canonical/version/count errors and fresh duplicate Apply.

### Readability and simplicity

The engine, closed codec and tests remain one private family. The shortcut adds a private overload/flag/out-result rather than another cache or shared abstraction. Comments identify the ordering invariant and why Apply must retain frame recovery. Isolated memo policies expose the same internal engine paths to deterministic cap/eviction tests, without mutable production-global test knobs. No orphaned production code or unrelated cleanup was identified.

The family and exhaustive tests are substantial because the already merged closed contract requires retained-history parity and negative matrices. The memo adds real synchronization/retention complexity; it should be reviewed as authority-bearing behavior, not assumed harmless because it is an optimization.

### Architecture

Authoritative mechanics stay in Core. Contracts, host/grain activation, dispatch, intelligence, Snapshot/Archives and Runner remain unchanged. No decoded external cache becomes an admission authority. No remote I/O or model inference is introduced. The implementation plan preserves the private boundary and all deferred parent gates. The existing project/design naming and public topology remain unchanged.

### Security and data integrity

All 57 physical pins precede any reuse. Previously consumed AA Content is freshly checked after preflight, and fresh pre-AA completion still checks Content at use after Apply/Replay preflights. Reader claims remain fresh syntax and byte comparisons. Full raw ledgers and typed context prevent another owner, clock configuration, Setup or ordered ingress history from borrowing successful state.

Memo key/Base/Control/event/result buffers are owned. Transition clones state; output getters copy byte arrays and clone JsonElements. Caller/result mutation, both owners, prepared seal orders, cancellation, concurrent hits/misses/eviction and reentrant callbacks are tested. Eviction does not clear an in-flight entry. No callback, I/O, full replay, context serialization or SHA computation occurs under dictionary locks.

### Performance and resources

Default caps are128 entries,16MiB retained byte accounting,1MiB per entry and2MiB framed key size. Oversized valid candidates follow cold authentication; budget failure does not become a new admission error. Byte accounting includes retained payloads and explicit record/route allowances, not an exact CLR-heap measurement. Entry/history caps bound additional metadata. Result attachment and eviction share the lock and update accounting atomically; concurrent misses may legitimately compute twice.

Stage1b native acceptance passed14m18.036s versus prior17m12.877s; full Debug solution passed25m00.482s versus prior29m56.179s. Single unchanged mutation cases0/17/2 passed17.039/18.450/16.300s versus prior28.507/25.768/21.714s. These are local Debug observations, not controlled causal attribution or a hosted completion forecast. Temporary optimized-JIT Debug probes retain eight timing/allocation samples; prepared warm replay/readback medians6.037/12.226ms, cancelled5.987/12.041ms. Physical preflights, fresh parsing and misses remain costly.

The unchanged Release verify job still exceeded15 minutes. That unresolved operational gate remains the delivery blocker; Brain is reconciling read-only scheduling/remaining-cost research. No additional optimization is authorized automatically.

## Verification performed and results

At frozen source `64439f7`, Core DLL SHA256 is `c836f5e521eca026e0fb2a86a32680b4babb034b92022a8a489cd93ac293bc69`.

| Check | Observed result |
| --- | --- |
| Solution build, `--no-restore` | Passed; zero warnings/errors,4.52s. |
| Full solution format verify | Passed. |
| Four round-entry artifact hashes and57 physical dependency hashes | Unchanged and matched. |
| New result recovery/ownership/guards baseline |6/6 passed before the performance change. |
| Focused memo tests after correction |27/27 passed,10.346s. |
| Complete native acceptance |177/177 passed,0failed/skipped,14m18.036s. |
| Complete Debug solution |2770/2770 passed,0failed/skipped,25m00.482s; Core2291/EXR469/Contracts10. |
| Exact-head Release verify |Cancelled at unchanged15m limit, run38082003227/job114300669399; Core incomplete. |
| Other exact-head PR checks |Seven succeeded. |

Complete commands, log/XML hashes, earlier gates and failures are in the separate dated handoff. The native case-duration sum1195.113s from the full solution is not an additive wall/component share across overlapping collections. No additional heavy run followed the completed solution; lease is idle.

## TDD sensitivity and harness reconciliation

Initial semantic stub RED34 preceded literal GREEN34. Null original-ledger runtime leak was observed before the004 correction. ASCII carrier spelling exposed008 versus reference004 and was corrected with all128 ASCII values exercised. The prospective warm prepared Replay target58 callbacks initially observed74, then passed after memoization; it is an optimization sensitivity test, not a frozen universal Apply contract law.

A foreign Setup test initially retained a stale SetupHash; the harness was corrected using typed `Create`. Reversed-ledger harness expectations were corrected from006 to existing004 identity precedence. Stage1b's first new harness expected001 for noncanonical RoundInput; the unchanged codec returns008, so only the test was corrected. Its corrected six-case baseline passed before the shortcut; no invented product RED is claimed for this equivalence-preserving optimization.

Zero-test runner filters, compile/timer/ASCII harness mistakes and diagnostic reflection ambiguity remain distinguishable from genuine behavior failures. They are not counted as passing acceptance or product regressions.

## Risks, tradeoffs and maintenance costs

The static memo introduces bounded retention, LRU locking and isolated policy test seams. Hits still perform physical preflight, source/input validation and complete key framing. Misses and malformed claims remain expensive. Local runtime/architecture/coactivity variance limits comparisons, and hosted CI cannot be assumed from local timings.

The unresolved Release timeout must be reconciled before merge. Original local aggregation failure remains unexplained despite later passes. Canonical dependency acquisition remains outside authoritative grain turns when this private family is eventually integrated.

## Deviations, deferrals and known gaps

Paid commitment/results/settlement, full Snapshot/Archives process restart, repeated Movement, later-II/consumed lineage, Exercise/Runner activation and parent017–019 completion are excluded. Four inherited Python verifier failures remain separately recorded and unwaived. The original stopped incomplete Core run, original ExerciseRunner failure and all three hosted cancellations are retained. Later passes do not erase them.

No independent implementation verdict or merge approval is issued by this author packet. SELF_REVIEW_COMPLETE means the documented five-axis author audit and local acceptance are complete; the explicit CI blocker remains.

## Challenge points for the reviewer

Check memo authority and error ordering independently; both historical/fresh AA use; result-absent recovery; caller/result ownership; concurrent insertion, eviction and accounting; disabled-admission exact retries; and preservation of every original adversarial assertion. Verify plan status against checkpoint dates and the later evidence supplement. Treat research approval and this explanation as testimony rather than product approval.
