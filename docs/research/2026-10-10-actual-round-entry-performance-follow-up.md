# Actual round entry: Stage 1 performance follow-up

Date: 2026-10-10 America/Toronto. Read-only diagnosis, timebox 15:25–15:45 Toronto. Decision owner: Brain chat `01a0c9dc-00bc-78a3-800d-3cb36859e422`. Product snapshot: PR169, `f836a04c7314dd6fdb028e7bc4f077dac6a39a98`. This separate note does not amend the [frozen first investigation](2026-10-10-actual-round-entry-performance-design.md) or its [disposition](../work/handoffs/2026-10-10-actual-round-entry-performance-disposition.md). DAY-A remains implementation review 0/9, recovery 0/2. No implementation approval, Ready verdict or gate waiver is issued here.

## Decision

**Recommend one small follow-on within the existing successful replay memo: return its retained canonical read-only result before hydrating a mutable Frame.** Keep the existing source/round-input canonical guards, full raw evidence key, 57-pin preflight and consumed-Content check in their current order. Use this path only for `ReplayTrustedSource`; `ApplyTrustedSource` continues to hydrate owned state and run the fresh current-input transition. Keep reader claim parsing and full-byte comparison unchanged. This removes identifiable redundant work without introducing another admission authority or cache.

This is the smallest guard-preserving candidate, not a prediction that it will meet CI. A more aggressive early raw-key lookup before source/input parsing might remove additional warm work, but needs a distinct proof that every malformed argument misses and follows exactly the original error path. It is unnecessary for the first proposed change and is not bundled into this recommendation.

**Defer a separate derived-original Base memo.** Unique round-event/input misses do repeat predecessor replay, and this is a plausible larger opportunity. Existing probe stages overlap, vary widely and do not measure its share of the complete suite. The frozen recovery rule explicitly describes a cache keyed by full round source and both complete ledgers. A predecessor memo would intentionally share work across different round histories; Brain must resolve whether that narrower internal derivation cache fits the unchanged contract before authorizing it. The current research does not expand the already-approved full-frame cache into that scope.

The essential remaining existing evidence is complete local Core XML/method aggregation. It will show whether the mutation group, other native round groups or predecessor tests dominate the full run. It cannot provide hosted Release attribution or separate miss costs inside a method. If it confirms a miss-heavy mutation bottleneck and a larger cache is being considered, a small author-owned follow-up diagnostic must distinguish full-history hits/misses/evictions and original derivation from new round parsing/replay, under the same binary and environment. The current samples are insufficient for that choice. No new profiler, build or test process is started or requested now by this lane. Any next measurement or code change belongs to the author under Brain's authorization and the sole .NET lease.

## Observations and limits

The Stage 1 required CI check `114290360966`, run `38078496884`, failed with the annotation **“The job has exceeded the maximum execution time of 15m0s”** and cancellation. Job 19:06:50–19:22:05 UTC; test step 19:08:16–19:22:03 UTC. Observed command: `dotnet test --solution Sandtable.slnx --configuration Release --no-build --report-xunit-xml --results-directory artifacts/test-results`. Hosted net10.0/x64 Contracts passed in 470ms and ExerciseRunner in 1m55.399s; Core did not finish. Author reports artifact API `[]`, so no hosted Core method report survived. The seven other checks passing does not satisfy the required failed check.

Local Debug/net10.0/arm64 native acceptance passed **171/171**, zero failures/skips, in **17m12.877s** (module 17m12.383s). The assertion-bearing mutation method passed case0/17/2 in 28.5065789/25.7683748/21.7137705s. Original case0/17 were 72.7189261/74.2256305s; Stage 0 locals were 81.2524671/43.7427868s. The Stage 1 observed prepared-case times are substantially lower; these single observations are not a stable ratio or whole-suite forecast. No original cancelled-case baseline exists.

Corrected full Debug solution started 19:05:27 UTC and remained active at the 19:34 UTC observation checkpoint. Completed XML reports: Contracts 10/10, 1.152s; ExerciseRunner 469/469, 179.863s. Core completion/method XML was unavailable. The original full-run ExerciseRunner `AggregationFailed` remains unexplained and retained despite isolated 1/1, complete module 469/469 and paired-route 8.392s passing observations. It is not waived here.

The author supplied existing BCL/reflection probes using Debug Core DLL SHA256 `b92a1827dd9b5d77668858ffe79749c50e0d842dabad3980a9b80ec2098eca53` and fixture SHA256 `fc17cda07618bdd539a56b8c296037bbf36f37c6eb2ce6258c2b789b765f640b`. Mutation mode calls the existing compiled public acceptance method with all original assertions and default tiered compilation. Residual profile mode used `DOTNET_TieredCompilation=0`, two warmups/eight samples, except one cold replay. Both modes were run by the author, not this lane.

| Existing profile stage | Prepared median ms [min, max] | Cancelled median ms [min, max] |
| --- | --- | --- |
| 57-pin preflight | 4.48085 [3.6806, 5.4659] | 5.48435 [3.7682, 52.7444] |
| Original selection replay | 42.4065 [22.6021, 106.4808] | 56.4403 [22.7833, 120.2062] |
| Warm full round replay | 7.38935 [6.0276, 20.4583] | 6.24515 [5.9176, 15.8935] |
| Warm correct proof readback | 13.6128 [12.206, 26.9905] | 30.5298 [13.0212, 107.9921] |
| Changed round-input miss | 38.88145 [37.1138, 57.0419] | 90.6188 [51.5504, 112.48] |
| Changed round-event miss | 118.6624 [91.9666, 251.7074] | 73.21845 [36.4033, 168.9123] |

Cold full replay was a single prepared 102.2072ms/cancelled 60.3047ms observation. Input miss changes the first `admittedAt` by +1; event miss changes first event `stateVersion` by +1 and re-canonicalizes the source. Both expect CMB-ARE-006. They include pin acquisition/hashing, source/input parsing, key work, unchanged original selection replay and new round authentication until rejection. Correct readback includes both reader and replay preflights and claimed-proof parsing. **Do not add or subtract these medians to assign component CPU shares**, or multiply them into a promised CI runtime. The cancelled proof-readback outliers especially prevent a reliable parsing-cost inference. Earlier 23ms predecessor median is not a stable component inside these later 42–56ms measurements.

The previous exact static inventory remains relevant: the principal mutation method has 39,652 public calls: 8,016 event replay, 2,510 input replay, 23,522 proof readers and 5,604 control readers. Major loop subtotal is 52,924 calls, including clocks, cuts/readbacks, suffix Apply, exact/conflicting retries, original-history leaves and terminals. These count calls, not successful replays/cache hits. Some malformed calls fail early. The 10,526 event/input mutations describe opportunities to repeat unchanged predecessor work; they do not establish the cache-miss count or time share. Failed histories must remain unretained.

## Committed path analysis

Source was read from immutable `git show f836a04:...` snapshots, not the evolving author worktree. Codebase Memory project `sandtable-actual-selection-contract` is indexed 2026-10-05; the three cited new paths are `not_tracked` (coverage check 3/3, no pagination). Direct focused committed-source reads cover that limitation; no reindex or agent was started.

[Engine at f836a04](https://github.com/dills122/sandtable/blob/f836a04c7314dd6fdb028e7bc4f077dac6a39a98/src/Cna.Core/Campaigns/CampaignCombatActualRoundEntry.cs), Replay179–217, performs the 57-pin check, source ownership/canonical parsing, version/count checks and complete round-input ownership/canonical parsing before building full evidence. On a successful memo hit it checks Content at use when needed, parses retained Base and control into fresh JsonObjects, copies route/events and constructs a Frame. Then Result160–177 sees a retained result and returns owned canonical Control/Proof copies. The parsed Base/control, copied events and Frame are unnecessary for this read-only return. They remain necessary for fresh Apply transition behavior. Cheap fixture extraction gives representative prepared/cancelled source lengths about 80–81kB, retained Base about 24kB and control about 7.4kB; eliminating their hydration is identifiable but does not establish a large time saving. These are ASCII compact JSON buffer lengths, not allocations or CPU measurements.

The same entry already retains complete successful evidence: length-framed raw round source; trusted serialized Setup/configuration and rules hash; full ordered independently supplied old ledger and round ledger. Lookup uses evidence hash plus complete byte equality. No failed replay, new command outcome or supplied proof/control is retained. Entry count/total/per-entry/key bounds are 128/16MiB/1MiB/2MiB, with oversize/disabled candidates following cold validation. Result retention is optional; a hit without a result must continue existing computation and may retain one later. Eviction can leave an owned entry reference usable; buffers are never cleared or borrowed.

[Codec at f836a04](https://github.com/dills122/sandtable/blob/f836a04c7314dd6fdb028e7bc4f077dac6a39a98/src/Cna.Core/Campaigns/CampaignCombatActualRoundEntryCodec.cs), readers46–59, still preflight, parse the freshly supplied claimed bytes, call public replay (its own preflight), and compare every claimed byte against the authentic result. A cached authentic result cannot authorize a forged claim. Comparing expected bytes before grammar/canonical validation would change the error matrix and is excluded. Dropping a preflight, caching pin validation or changing shared/predecessor parsing is excluded.

DeriveBase219–244 validates current supplemental clock context then calls unchanged original selection replay on the full original source and old ledger. Every different new event/input evidence misses the existing full-history memo. A possible future predecessor cache could retain only successful selected derivation facts, keyed by complete raw original source, complete old ledger, trusted full context and exact clock context; the new round source/ledger would still authenticate from the initial round state. Null/truncated/reordered/mutated old histories, foreign context, seven fallback variants and non-selected/earlier cuts must remain cold/rejected. This is a design alternative, not approved implementation, and its scope must first be reconciled with the frozen full-source/two-ledger recovery rule.

The codec also splits new-family shape metadata during canonical writes; precomputing immutable field descriptors could remove small allocation work. No timing assigns material cost to it, and predecessor fallback canonicalization is outside allowed files. Increasing cache caps is similarly unsupported: the earlier ~33.4MB estimate for all 224 fixture cuts does not prove that LRU churn occurs inside heavy methods. Neither is the first recommendation.

## Acceptance for the proposed same-entry result path

Preserve all 171 existing acceptance cases and all existing public-API mutation assertions. Existing memo tests at [committed tests](https://github.com/dills122/sandtable/blob/f836a04c7314dd6fdb028e7bc4f077dac6a39a98/tests/Cna.Core.Tests/Campaigns/CombatActualRoundEntryTests.cs) cover both complete raw ledgers/caller ownership198, cold/warm all57 pins238, eviction/oversize261, concurrency/reentrant callbacks296 and warm prepared Content175. Apply remains on the current frame path; at-consumption tests125 and the full retry/lifecycle/clock/error matrix stay intact.

Any focused additions should prove read-only warm results retain literal full bytes and owned getters; warmed proof/control readers still reject fresh malformed and canonical re-signed claims with their exact original codes; result-absent entries, eviction and disabled/oversize policies still compute cold; every altered context/ledger/source cannot hit. Preserve exact preflight counts and post-preflight Content disappearance/tamper rejection. Test concurrent readers and caller-returned mutation without timing-sensitive assertions. No synthetic prior Control/Base admission, abbreviated source identity, cached failures, hidden current-input authority or new public recovery token is introduced.

Implementation measurement should reuse the same assertion-bearing prepared cases0/17 and cancelled2 with recorded binary/environment hashes, then complete the unchanged full native/full-solution gates and required exact-head Release CI. Per-component deterministic counts may accompany observations; a predicted improvement cannot replace passing CI. If the small change remains insufficient, report the constraint to Brain with completed method timings and actual hit/miss/eviction counts before selecting another cache. Brain retains review/recovery counters and optimization authorization.

## Evidence fingerprints and self-review

Author worktree is `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`; retained author logs are under `/private/tmp/native-round-entry-gates`. Probe source is `/private/tmp/native-round-entry-profile/Program.cs`. No .NET process, code/test/plan/pin edit or shared-file change was made by this lane. Only this new dated note is saved on existing branch `codex/actual-round-entry-performance-research` in the research worktree.

| Artifact | SHA256 |
| --- | --- |
| Committed engine | `c8b1b6d8d94c5d895bda4a486e3422c176b8eee494b3bfa6be37e1f9a01c3fd3` |
| Committed codec | `e224bb57183b1b0febf9b90a297744e9ce99cf3b4124ce5d320eb3fb60e48065` |
| Committed tests | `025799e539ccd9cf66322ac8fd5e4af9dbe630a51db3b98089a7052d2a8b9da2` |
| Probe source | `f0d5cb534936d5fe45cd510db06b3c40ebb87d8559f53b52eba7319ba27cb8ae` |
| Prepared profile log | `f31e1f9ade0034f2222c04d48da68cd3b1b76e7c32318a43e27542caf980c3e7` |
| Cancelled profile log | `73e04bde30010e148a15de988df5963bfb1c117c675fb50c5f112dffc7802bc7` |
| Stage1 case0 log | `c9e5c4d05d1abf253f08c7b34fec173de30bf7202c2f3f06a6a32bc6a9e84b04` |
| Stage1 case17 log | `e56eb91f4e367efd4542b816de3fd93d78d2917341b0cf6aa7b07d8ebd90f00a` |
| Stage1 case2 log | `ea64794f19380d1a03a3e3cd1929f4330a7eff639f501e43df2b341510947dda` |
| Complete native log | `8aa11ad9200f2756e414bd1fd9a1a1fccc6fcc0984c84ee7401c7557e4b94b63` |
| Cancelled CI log | `24abdb7579fc7dd81eb8dc47f7719a22045ae96853f34f51a392ae8354cd7551` |

Self-review: checked committed/live-source distinction, stale graph coverage, environment differences, overlapping probe costs/outliers, single cold observations, missing cancelled baseline/Core XML, failed CI versus passing native acceptance, and retained unexplained aggregation failure. Checked narrower predecessor-cache scope against the frozen contract rather than silently extending permission. The recommendation keeps every guard and key before lookup and removes only read-only hydration. The prior note/disposition hashes, local links, whitespace and new-note-only status are verified at save. This is a conditional engineering recommendation for Brain, not an independent implementation review.
