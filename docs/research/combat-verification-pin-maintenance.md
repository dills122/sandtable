# Combat verifier pin maintenance decision

Status: S1a research complete at the requested baseline; maintenance is **NO-GO under the current three-file allowance and NO-GO as a bounded full repair in this session**. This is an author recommendation awaiting independent review and coordinator disposition, not implementation approval.

The complete live sequence-source closure contains **20 files: 15 fixtures, three schemas, and two Python readers**. The prior 11 Breakdown-rooted/12 source-rooted fixture counts omitted three Exercise fixtures reached through specification-relative pin keys. The normalized fixture-only closures are 14 Breakdown-rooted and 15 source-rooted. Literal reader pins and schemas add five more live files. Refreshing only Breakdown would introduce failures in immediate and transitive descendants. No pin or fixture bytes were changed.

The cache-only source delta is proven equivalent within the catalog contract and reproduced golden cases. That does not make a full evidence refresh trivial: B2 build identity hashes its own schema bytes and predecessor pins; B2 child and B3 parent evidence must consequently be regenerated and checked. Outward composition also has an independent stale Content-document pin. Recommend retaining the honest baseline failures and allowing a private S3 bridge only when reviewed S2 establishes that it consumes actual positive entry without invoking the blocked historical fixture admission, Snapshot, or outward composition. A failed oracle remains failed.

## Decision packet

Question: what complete maintenance scope can restore the pre146 pin evidence without changing gameplay, weakening rejection, or introducing unreported downstream failures? Decision owner: session2 coordinator; an independent high-effort reviewer must assess this packet first. S1b needs separate explicit authorization after review. This research owns only this report and its dated handoff/bootstrap/author files. Runtime, contracts, fixtures, pins, canonical plans and public activation are excluded. Temporary controlled probes are allowed; no heavy solution-suite lease was used.

Success criteria: original failure reproduced; source semantics compared to the exact pre146 bytes; every live structured/literal hash dependency inventoried; generation order, enforcing readers, downstream verification and failure-sensitive tamper plan frozen; S3 impact separated from historical/public evidence. Stop condition: a bounded maintenance decision, not an exhaustive campaign or repaired baseline.

Source hierarchy: exact Git objects and source predicates; reproduced local observations; canonical roadmap/Combat plan and prior handoff; explicit inference. No external product/API claim required internet evidence.

## Exact repository and environment

Baseline/head: `afa396ad5094fae7b8f054c60a9df9e03f83dfd5`.
Research worktree: `/Users/dsteele/.codex/worktrees/combat-pin-inventory/sandtable`.
Pre146 parent: `c22346d521973ed2c7c29de7841de0677a8b3dfe`.
Cache commit: `2e17f60767cceff6db950db532d9432f5b81a9cd` (PR146).
Host: macOS arm64; Python3.14.6; .NET SDK10.0.400. global.json selects native Microsoft.Testing.Platform; SDK-style Core tests use xUnit v3 MTP.

The app initially returned “Not a git repository” for managed worktree creation. The coordinator subsequently created the exact managed worktree. Shell worktree/branch creation attempts were not executed because automatic approval review reported its selected model at capacity. At packet creation the supplied worktree is detached at the baseline; only Serena-created `.serena/` is untracked. Research/admin artifacts are retained under `/private/tmp/combat-pin-inventory-evidence/retained/` for coordinator preservation. No publication or outbound-message retry was attempted. A branch/commit must be recorded after preservation before treating this as a committed review target.

Serena manual read; exact-worktree project activated. Codebase Memory indexed this worktree as `sandtable-combat-pin-inventory`, generation2026-10-05T12:49:20Z, fast/nonpersistent. Coverage found no recorded issue for the sequence source and cache tests, but excludes `docs` and `.planning`; all contract/fixture statements use focused source reads and tracked-file scans. Search pages were exhausted; best-effort graph coverage is not completeness proof.

## Facts and observations

**Documented fact:** the only PR146 sequence-source edits are a private last-turn cache, a lookup after argument validation, and publishing the unchanged constructed read-only array through Volatile. CreateTurn's construction loop, constants, source lists, GetNext and every serialized field are unchanged. Current source equals the PR146 source exactly. Removing precisely those three edits reconstructs the exact parent bytes; this is a comparison probe, not a proposed semantic pin normalizer.

| Object | SHA256 |
| --- | --- |
| Pre146/pinned sequence source | `c5426245a156367eac85fc5e61792ece1ae124651a0416793fde00213641b938` |
| Current sequence source | `d019a3bc2941ad788b69550f5d7fc01e33fa586a0bad6a374c08550b3cb6a79a` |

**Observation:** original `python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py` exits1 at check_fixture line290: source drift for `src/Cna.Core/Rules/Cna1979LandSequence.cs`. Original cycle-sequence also exits1 on that source. Snapshot's recursive source_pins() preflight rejects the same source before returning a usable pin inventory. The independently frozen Snapshot root pin and cycle-sequence root pin must both be addressed; updating Breakdown alone cannot repair them.

**Observation:** a compiled standalone probe compares copied pre/current sequence sources with identical supporting types. Every catalog contains112 positions and serializes identically cold, warm and after eviction. Every in-turn successor and valid rollover matches; invalid turns, null/forged positions and max-turn overflow agree; shared catalogs and nested source lists reject mutation. Parallel comparison of128 requested turns passes. The sampled hashes reproduce the existing LandSequenceCacheTests baseline literals:

| Turn | Serialized catalog SHA256 |
| --- | --- |
| 1 | `a858da5acc6c1d1f2cef4cc4ce4cbda231ffd08ee8d0aa28b345efaa2d2605aa` |
| 2 | `3cdcab05c9d55bba1ee101fd65bc203b8bd4f2be35fa62b8cdf9f4f2980a9ef1` |
| 6 | `9ab7d2a023ec3be24ee3ee0570cc102fbfb33e2be6c4ade8911adebb82dccc78` |
| int.MaxValue | `5473d52cdbeba3c293c194463259c99aefbb4f889ffee49a852a8ef1afa63f64` |

**Observation:** a separate Breakdown semantic/golden probe calls semantic_red(), trace(), goldens() and verify() for all8 retained cases. Golden equality passes with16cuts,8retries,426mutations,92raw rejects and298boundary probes. It deliberately does not call the failing check_fixture. This is separate evidence and never reported as a passing original oracle.

**Inference:** exact unchanged construction/validation/successor code plus the compiled comparisons and retained Breakdown goldens justify treating this source delta as cache-only for maintenance. They do not prove arbitrary future cache changes, every runtime behavior, full .NET parity or the safety of downstream report regeneration.

## Complete live maintenance manifest

Scope of completeness: all git-tracked UTF-8 files at the baseline, recursive `{path,sha256}`/`{path,hash}` pins, dictionaries keyed by repository paths or paths relative to docs/specs, nested declarations including successor3/3b, and literal occurrences of current tracked-file digests plus the old sequence-source digest. This yields65 live structured/literal dependency edges. Five additional inspected generator relationships determine ordering. The scan was performed on actual bytes, not solely sourcePins arrays. Historical snapshots of review evidence are classified separately and retained unchanged.

The following20 paths are the complete source-rooted live hash closure under that method. They are a frozen proposed maintenance manifest, not authorized edits. Hashes identify exact input bytes; output hashes do not exist until an authorized refresh and review. Order groups account for generator inputs as well as hash references; source row0 is comparison-only and must remain unchanged.

| Order | Path | Current SHA256 | Permitted future delta |
| --- | --- | --- | --- |
| 0 | `src/Cna.Core/Rules/Cna1979LandSequence.cs` | `d019a3bc2941ad788b69550f5d7fc01e33fa586a0bad6a374c08550b3cb6a79a` | None: already-cached source, comparison root only |
| 1 | `docs/specs/fixtures/combat-cycle-sequence-v1.json` | `de89ebcd86f171119d206d0a90553cdeb815e7b8b6fc8903c9dfcacbd1db5b3f` | Only declared predecessor pin metadata; every retained semantic golden byte/hash unchanged |
| 1 | `docs/specs/fixtures/combat-inherited-breakdown-completion-v1.json` | `7d6ee76d454bb047e03d104310758224d9470a0e9aabd30ce841abb32ffc54d1` | Only declared predecessor pin metadata; every retained semantic golden byte/hash unchanged |
| 2 | `docs/specs/fixtures/combat-inherited-selection-v1.json` | `76240f2409f341757deea0f6ab5d346699de5a4a33690f95ccb7e74e160c1f4f` | Only declared predecessor pin metadata; every retained semantic golden byte/hash unchanged |
| 2 | `docs/specs/fixtures/combat-inherited-snapshot-v1.json` | `9dab9b8f72c3643fd50c62fd6e97335e5bc719ad1f61f4a15a910597224c862f` | Only declared predecessor pin metadata; every retained semantic golden byte/hash unchanged |
| 3 | `docs/specs/fixtures/combat-inherited-armed-continuation-v1.json` | `d86603ff7f07f2e0a3624e8748fedcf4406e2d873360f4e56a30e5952c645b1f` | Only declared predecessor pin metadata; every retained semantic golden byte/hash unchanged |
| 3 | `docs/specs/fixtures/combat-inherited-no-attack-v1.json` | `4e8f8a9fa0d1e973d6a5d7046b3e013f4270bd31c53afa24ed7595f732be6bc1` | Only declared predecessor pin metadata; every retained semantic golden byte/hash unchanged |
| 4 | `docs/specs/fixtures/combat-inherited-cycle-control-v1.json` | `e2f2c76539a3509310fa2f1822fd1d84e1346f62cfc274ce12682866d02ba236` | Only declared predecessor pin metadata; every retained semantic golden byte/hash unchanged |
| 5 | `docs/specs/fixtures/combat-inherited-reserve-movement-v1.json` | `4feb5dc5e178d9242b8a6ef3b42f8101b3810e60bfe722506fa8838356d6567f` | Only declared predecessor pin metadata; every retained semantic golden byte/hash unchanged |
| 6 | `docs/specs/fixtures/combat-inherited-reserve-movement-completion-v1.json` | `e336d9b5afe498b3886318509958cb2e50ae860676ee1c4603c55540a41847d3` | Only declared predecessor pin metadata; every retained semantic golden byte/hash unchanged |
| 7 | `docs/specs/fixtures/combat-authority-composition-v1.json` | `a90f88de9c05b4b1bd7081e7828abb7553e3e0537a6efa8cad042c87a6df50b1` | Only declared predecessor pin metadata; every retained semantic golden byte/hash unchanged |
| 8 | `docs/specs/verify-combat-side-projection-v1.py` | `964f4b6a6901e446e5d0e0a6c49e79e778677ae458947681034a4ddd5d848a0d` | Only literal predecessor digest values; pin checks, parsing, control flow and rejects unchanged |
| 9 | `docs/specs/fixtures/combat-side-projection-v1.json` | `07139da2ff125345c3007aa6bd9a5760eec44e4d4f25d0ec85c7e06fed4da44f` | Only declared predecessor pin metadata; every retained semantic golden byte/hash unchanged |
| 10 | `docs/specs/verify-combat-exercise-occurrence-v1.py` | `9e46db1943514629754cdc227fdc1a415c53c8b5e573b8fb0afd89c870f1fb53` | Only literal predecessor digest values; pin checks, parsing, control flow and rejects unchanged |
| 11 | `docs/specs/fixtures/combat-exercise-occurrence-v1.json` | `aabc20a39229ac121703a6cda077c0ebeffbfdd7bc3142a3d0c1d0805af7a38e` | Only declared predecessor pin metadata; every retained semantic golden byte/hash unchanged |
| 12 | `docs/specs/combat-exercise-child-evidence-v1.schema.json` | `cc8a7c9e89068b9a78f3abb4044230ccc9ef359665198aaa7ef6db5b15782bf6` | Only sourcePins digest values; grammar, limits, contract versions and object fields unchanged |
| 13 | `docs/specs/fixtures/combat-exercise-child-evidence-v1.json` | `5587cb17e53c48a9ab393532ebaa186f3d1384632eb13b105e1af8a698e799b6` | Regenerated provenance/build/report-derived bytes; exact authoritative event/world projections unchanged |
| 14 | `docs/specs/combat-exercise-parent-evidence-v1.schema.json` | `00d1fcd2cb7d06b6f25d9224ff0839308f23fd2052b42dab24c98ea378b30820` | Only sourcePins digest values; grammar, limits, contract versions and object fields unchanged |
| 15 | `docs/specs/fixtures/combat-exercise-parent-evidence-v1.json` | `08316dae3d4f12298c35bfe05f640dcd2559ae41cbf4144430cd52b121847a9f` | Regenerated provenance/build/report-derived bytes; exact authoritative event/world projections unchanged |
| 16 | `docs/specs/combat-outward-composition-v1.schema.json` | `6e8ec1103315238c0c5520c6cff9b31b20d6de2386e197944265a48168021344` | Only sourcePins digest values; grammar, limits, contract versions and object fields unchanged |
| 17 | `docs/specs/fixtures/combat-outward-composition-v1.json` | `28e37f48c80ed6ca6b7f7fabb4fb72b5d3433305ad662e091e2625a017bc1c24` | Regenerated artifact digest/byte metadata, derived readbacks/capacities; classify Content drift separately |

The smallest complete current-source refresh therefore exceeds three files even before administration. Root fixes alone require Breakdown, cycle-sequence and Snapshot, then selection's pinned Breakdown bytes require a fourth file. There is no compliant <=3-file alternative that restores current-source verification and all descendants while preserving exact-byte checks. Reverting PR146 would change runtime/performance outside this task; checking an old Git blob, skipping the source pin or accepting normalized semantics changes the trust boundary. None is approved or recommended.

Fixture-only count reconciliation: normalized Breakdown-root closure is14 fixtures and sequence-root closure15. Removing B1 occurrence, B2 child and B3 parent fixtures produces the plan reviewers'11/12 counts. The extra reach comes from relative paths in their sourcePins, not new implementation. Three schemas and two literal-pin readers enlarge the full live closure to20 paths.

## Exact dependency edges and enforcing readers

JSON pointers below use RFC6901 escaping. Unprefixed digest literal edges cite one-based source lines. Every target's exact digest is in the manifest above.

| Pin holder | Pointer or line | Pinned target |
| --- | --- | --- |
| `docs/specs/combat-exercise-child-evidence-v1.schema.json` | `/sourcePins/verify-combat-exercise-occurrence-v1.py` | `docs/specs/verify-combat-exercise-occurrence-v1.py` |
| `docs/specs/combat-exercise-child-evidence-v1.schema.json` | `/sourcePins/fixtures~1combat-exercise-occurrence-v1.json` | `docs/specs/fixtures/combat-exercise-occurrence-v1.json` |
| `docs/specs/combat-exercise-parent-evidence-v1.schema.json` | `/sourcePins/combat-exercise-child-evidence-v1.schema.json` | `docs/specs/combat-exercise-child-evidence-v1.schema.json` |
| `docs/specs/combat-exercise-parent-evidence-v1.schema.json` | `/sourcePins/fixtures~1combat-exercise-child-evidence-v1.json` | `docs/specs/fixtures/combat-exercise-child-evidence-v1.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1combat-exercise-child-evidence-v1.schema.json` | `docs/specs/combat-exercise-child-evidence-v1.schema.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1combat-exercise-parent-evidence-v1.schema.json` | `docs/specs/combat-exercise-parent-evidence-v1.schema.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1fixtures~1combat-authority-composition-v1.json` | `docs/specs/fixtures/combat-authority-composition-v1.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1fixtures~1combat-cycle-sequence-v1.json` | `docs/specs/fixtures/combat-cycle-sequence-v1.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1fixtures~1combat-exercise-child-evidence-v1.json` | `docs/specs/fixtures/combat-exercise-child-evidence-v1.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1fixtures~1combat-exercise-occurrence-v1.json` | `docs/specs/fixtures/combat-exercise-occurrence-v1.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1fixtures~1combat-exercise-parent-evidence-v1.json` | `docs/specs/fixtures/combat-exercise-parent-evidence-v1.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1fixtures~1combat-inherited-armed-continuation-v1.json` | `docs/specs/fixtures/combat-inherited-armed-continuation-v1.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1fixtures~1combat-inherited-cycle-control-v1.json` | `docs/specs/fixtures/combat-inherited-cycle-control-v1.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1fixtures~1combat-inherited-no-attack-v1.json` | `docs/specs/fixtures/combat-inherited-no-attack-v1.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1fixtures~1combat-inherited-reserve-movement-completion-v1.json` | `docs/specs/fixtures/combat-inherited-reserve-movement-completion-v1.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1fixtures~1combat-inherited-selection-v1.json` | `docs/specs/fixtures/combat-inherited-selection-v1.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1fixtures~1combat-side-projection-v1.json` | `docs/specs/fixtures/combat-side-projection-v1.json` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1verify-combat-exercise-occurrence-v1.py` | `docs/specs/verify-combat-exercise-occurrence-v1.py` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `/sourcePins/docs~1specs~1verify-combat-side-projection-v1.py` | `docs/specs/verify-combat-side-projection-v1.py` |
| `docs/specs/fixtures/combat-authority-composition-v1.json` | `/sourcePins/13/sha256` | `docs/specs/fixtures/combat-inherited-armed-continuation-v1.json` |
| `docs/specs/fixtures/combat-authority-composition-v1.json` | `/sourcePins/14/sha256` | `docs/specs/fixtures/combat-inherited-cycle-control-v1.json` |
| `docs/specs/fixtures/combat-authority-composition-v1.json` | `/sourcePins/15/sha256` | `docs/specs/fixtures/combat-inherited-no-attack-v1.json` |
| `docs/specs/fixtures/combat-authority-composition-v1.json` | `/sourcePins/20/sha256` | `docs/specs/fixtures/combat-inherited-reserve-movement-completion-v1.json` |
| `docs/specs/fixtures/combat-cycle-sequence-v1.json` | `/sourceHashes/src~1Cna.Core~1Rules~1Cna1979LandSequence.cs` | `src/Cna.Core/Rules/Cna1979LandSequence.cs` |
| `docs/specs/fixtures/combat-exercise-child-evidence-v1.json` | `/sourcePins/verify-combat-exercise-occurrence-v1.py` | `docs/specs/verify-combat-exercise-occurrence-v1.py` |
| `docs/specs/fixtures/combat-exercise-child-evidence-v1.json` | `/sourcePins/fixtures~1combat-exercise-occurrence-v1.json` | `docs/specs/fixtures/combat-exercise-occurrence-v1.json` |
| `docs/specs/fixtures/combat-exercise-occurrence-v1.json` | `/sourcePins/fixtures~1combat-side-projection-v1.json` | `docs/specs/fixtures/combat-side-projection-v1.json` |
| `docs/specs/fixtures/combat-exercise-occurrence-v1.json` | `/sourcePins/verify-combat-side-projection-v1.py` | `docs/specs/verify-combat-side-projection-v1.py` |
| `docs/specs/fixtures/combat-exercise-occurrence-v1.json` | `/sourcePins/fixtures~1combat-authority-composition-v1.json` | `docs/specs/fixtures/combat-authority-composition-v1.json` |
| `docs/specs/fixtures/combat-exercise-parent-evidence-v1.json` | `/sourcePins/combat-exercise-child-evidence-v1.schema.json` | `docs/specs/combat-exercise-child-evidence-v1.schema.json` |
| `docs/specs/fixtures/combat-exercise-parent-evidence-v1.json` | `/sourcePins/fixtures~1combat-exercise-child-evidence-v1.json` | `docs/specs/fixtures/combat-exercise-child-evidence-v1.json` |
| `docs/specs/fixtures/combat-inherited-armed-continuation-v1.json` | `/sourcePins/5/sha256` | `docs/specs/fixtures/combat-inherited-selection-v1.json` |
| `docs/specs/fixtures/combat-inherited-breakdown-completion-v1.json` | `/sourcePins/9/sha256` | `src/Cna.Core/Rules/Cna1979LandSequence.cs` |
| `docs/specs/fixtures/combat-inherited-cycle-control-v1.json` | `/sourcePins/5/sha256` | `docs/specs/fixtures/combat-inherited-armed-continuation-v1.json` |
| `docs/specs/fixtures/combat-inherited-no-attack-v1.json` | `/sourcePins/3/sha256` | `docs/specs/fixtures/combat-inherited-selection-v1.json` |
| `docs/specs/fixtures/combat-inherited-reserve-movement-completion-v1.json` | `/sourcePins/2/sha256` | `docs/specs/fixtures/combat-inherited-reserve-movement-v1.json` |
| `docs/specs/fixtures/combat-inherited-reserve-movement-v1.json` | `/sourcePins/2/sha256` | `docs/specs/fixtures/combat-inherited-cycle-control-v1.json` |
| `docs/specs/fixtures/combat-inherited-selection-v1.json` | `/sourcePins/2/sha256` | `docs/specs/fixtures/combat-inherited-breakdown-completion-v1.json` |
| `docs/specs/fixtures/combat-inherited-snapshot-v1.json` | `/sourcePins/docs~1specs~1fixtures~1combat-cycle-sequence-v1.json` | `docs/specs/fixtures/combat-cycle-sequence-v1.json` |
| `docs/specs/fixtures/combat-inherited-snapshot-v1.json` | `/sourcePins/docs~1specs~1fixtures~1combat-inherited-breakdown-completion-v1.json` | `docs/specs/fixtures/combat-inherited-breakdown-completion-v1.json` |
| `docs/specs/fixtures/combat-inherited-snapshot-v1.json` | `/sourcePins/src~1Cna.Core~1Rules~1Cna1979LandSequence.cs` | `src/Cna.Core/Rules/Cna1979LandSequence.cs` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/16/sha256` | `docs/specs/combat-exercise-child-evidence-v1.schema.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/20/sha256` | `docs/specs/combat-exercise-parent-evidence-v1.schema.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/53/sha256` | `docs/specs/fixtures/combat-authority-composition-v1.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/56/sha256` | `docs/specs/fixtures/combat-cycle-sequence-v1.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/57/sha256` | `docs/specs/fixtures/combat-exercise-child-evidence-v1.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/58/sha256` | `docs/specs/fixtures/combat-exercise-occurrence-v1.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/59/sha256` | `docs/specs/fixtures/combat-exercise-parent-evidence-v1.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/60/sha256` | `docs/specs/fixtures/combat-inherited-armed-continuation-v1.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/61/sha256` | `docs/specs/fixtures/combat-inherited-cycle-control-v1.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/62/sha256` | `docs/specs/fixtures/combat-inherited-no-attack-v1.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/63/sha256` | `docs/specs/fixtures/combat-inherited-reserve-movement-completion-v1.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/64/sha256` | `docs/specs/fixtures/combat-inherited-selection-v1.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/73/sha256` | `docs/specs/fixtures/combat-side-projection-v1.json` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/81/sha256` | `docs/specs/verify-combat-exercise-occurrence-v1.py` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `/artifacts/96/sha256` | `docs/specs/verify-combat-side-projection-v1.py` |
| `docs/specs/fixtures/combat-side-projection-v1.json` | `/sourcePins/7/sha256` | `docs/specs/fixtures/combat-authority-composition-v1.json` |
| `docs/specs/fixtures/combat-side-projection-v1.json` | `/sourcePins/10/sha256` | `docs/specs/fixtures/combat-cycle-sequence-v1.json` |
| `docs/specs/fixtures/combat-side-projection-v1.json` | `/successor3/sourcePins/fixtures~1combat-inherited-cycle-control-v1.json` | `docs/specs/fixtures/combat-inherited-cycle-control-v1.json` |
| `docs/specs/fixtures/combat-side-projection-v1.json` | `/successor3b/sourcePins/fixtures~1combat-authority-composition-v1.json` | `docs/specs/fixtures/combat-authority-composition-v1.json` |
| `docs/specs/verify-combat-exercise-occurrence-v1.py` | `line 258` | `docs/specs/fixtures/combat-side-projection-v1.json` |
| `docs/specs/verify-combat-exercise-occurrence-v1.py` | `line 258` | `docs/specs/fixtures/combat-authority-composition-v1.json` |
| `docs/specs/verify-combat-exercise-occurrence-v1.py` | `line 258` | `docs/specs/verify-combat-side-projection-v1.py` |
| `docs/specs/verify-combat-side-projection-v1.py` | `line 1594` | `docs/specs/fixtures/combat-inherited-cycle-control-v1.json` |
| `docs/specs/verify-combat-side-projection-v1.py` | `line 2364` | `docs/specs/fixtures/combat-authority-composition-v1.json` |

Enforcement predicates:

- Breakdown check_fixture() lines288–290 and inherited-selection check_fixture() lines377–379 compare exact declared file bytes. No recursive semantic success repairs a stale immediate digest.
- cycle-sequence main() line233 compares sourceHashes; inherited Snapshot source_pins() lines407–425 follows sourceHashes/sourcePins recursively and checks every declared predecessor before returning its inventory. Snapshot's top-level frozen inventory is also compared at line683.
- inherited no-attack check_fixture(), cycle-control main()/source_pins(), reserve-movement main()/source_pins(), reserve-movement-completion check_fixture() and authority-composition check_fixture()/source_pins() enforce their immediate fixture pins. Armed-continuation main verifies its generated sourcePins against the retained fixture.
- Side Projection source_pins3() lines1596–1598 and source_pins3b() lines2367–2369 enforce PINS3/PINS3B literal dictionaries; its generated_fixture()/main reconcile the top-level and successor inventories against retained evidence.
- Exercise Occurrence source_pins() lines259–261 checks literal PINS and both Side Projection pin sets before source_cases()/authenticate_source access catalog data. Its test_fixture() checks exact generated fixture bytes.
- B2 pins() lines51–52 checks schema sourcePins and calls B1's check. build_descriptor()/build_hash() lines54–56 hashes its reader, its schema and predecessors; admission()/validation bind buildHash (lines84–90). B2 source/report identities therefore change when schema or predecessor bytes change. Merely replacing visible sourcePins in the B2 fixture is insufficient.
- B3 pins() line49 checks schema sourcePins and calls B2 pins(); read_fixture() compares exact newly generated child/parent evidence.
- Outward pins() line49 checks schema INVENTORY sourcePins and calls parent pins; build_index() records actual artifact digests and sizes. read_fixture()/test_fixture() demand exact generated index bytes. Refresh schema first, fixture last; never handwave changed readbacks or capacities.

Generation dependencies in addition to raw hash edges: Side Projection reader→its fixture (successor pin dictionaries); B1 reader→its fixture (literal PINS); B2 schema→its fixture (buildHash/provenance); B3 schema→its fixture (source inventory and derived reports); outward schema→its fixture (index artifact list). These explain why two same-level hash nodes cannot always be refreshed in arbitrary order.

## Independent stale roots and historical references

The three live sequence-root pins are the sourceHashes slot in cycle-sequence, sourcePins[9] in Breakdown and the keyed sourcePins slot in Snapshot. There is also a direct old sequence-source entry in `docs/specs/breakdown-fixture-migration.v1.json#/inheritedSchemaSources/2/sha256`. That file is a historical migration manifest: BreakdownFixtureMigrationTests.MigrationFiles() reads scenarioFiles and its test enforces historical scenario bytes, not inheritedSchemaSources; the historical source section must not be refreshed. Focused searches over src/tests/docs/specs/.github/justfile found no enforcing current reader for that inherited section. Its other historical source entries also predate later implementations. This is bounded source-search evidence, not a universal claim about external users.

Historical .planning evidence, review manifests and dated handoffs record earlier heads and must retain their original hashes. The scan records37 historical reference-holder paths; they are not repair candidates. Rewriting those artifacts would erase what previous reviewers examined.

Outward has two additional stale Content-document entries, in its schema sourcePins and fixture artifacts[10]. Expected `9ebe2882b2d8b41fde015b06fa3d5ff0eaad2283150d06bcf5301280ef391b78`; current `79dbefd44a55ca66976e1299e1f74acbe495bbc4289af9fe9ea9befc29e16e4a`. The expected bytes equal the parent of PR116 (`806d2d6`), whose diff changes implementation-status/follow-up paragraphs only. This explains the outward baseline failure and presents a possible separate documentation-pin maintenance decision; it does not authorize it. Those two fields are in the existing20-path manifest but outside the sequence-root edge set. A sequence-only repair must still report outward FAILED. A complete all-oracle GREEN repair needs explicit inclusion and review of these Content fields.

## Original commands and bounded observations

Original commands were launched from the exact worktree with python3 -B and a12-second per-process bound. Timeouts are unverified, not failures of the contract semantics and never passes.

| Original command | Result | Seconds |
| --- | --- | --- |
| `python3 -B docs/specs/verify-combat-cycle-sequence-v1.py` | `1` | 0.049 |
| `python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py` | `1` | 0.377 |
| `python3 -B docs/specs/verify-combat-inherited-selection-v1.py` | `TIMEOUT12s` | 12.005 |
| `python3 -B docs/specs/verify-combat-inherited-no-attack-v1.py` | `TIMEOUT12s` | 12.005 |
| `python3 -B docs/specs/verify-combat-inherited-armed-continuation-v1.py` | `0` | 8.904 |
| `python3 -B docs/specs/verify-combat-inherited-cycle-control-v1.py` | `0` | 11.058 |
| `python3 -B docs/specs/verify-combat-inherited-reserve-movement-v1.py` | `0` | 10.092 |
| `python3 -B docs/specs/verify-combat-inherited-reserve-movement-completion-v1.py` | `TIMEOUT12s` | 12.005 |
| `python3 -B docs/specs/verify-combat-authority-composition-v1.py` | `TIMEOUT12s` | 12.006 |
| `python3 -B docs/specs/verify-combat-positive-entry-v1.py` | `TIMEOUT12s` | 12.005 |

A second positive-entry original invocation with a60-second bound also timed out with no output; no fresh complete positive-entry oracle pass is claimed. Historical S0 passes are historical evidence only. All13 declared positive-entry sourceHash values were checked against actual baseline bytes and match. No native full solution suite, Boundary suite, remote provider, hosted CI or fixture regeneration was run.

Cheap preflight calls isolate pin enforcement; these are not complete oracle runs:

| Oracle | Function | Result |
| --- | --- | --- |
| `inherited-selection` | `check_fixture` | PASS pin preflight only |
| `inherited-no-attack` | `check_fixture` | PASS pin preflight only |
| `inherited-cycle-control` | `fixture_pins` | PASS pin preflight only |
| `inherited-reserve-movement` | `fixture_pins` | PASS pin preflight only |
| `inherited-reserve-movement-completion` | `check_fixture` | PASS pin preflight only |
| `authority-composition` | `fixture_pins` | PASS pin preflight only |
| `inherited-snapshot` | `source_pins` | FAIL pin preflight — `Invalid: declared source pin: src/Cna.Core/Rules/Cna1979LandSequence.cs` |
| `side-projection` | `source_pins3` | PASS pin preflight only |
| `side-projection` | `source_pins3b` | PASS pin preflight only |
| `exercise-occurrence` | `source_pins` | PASS pin preflight only |
| `exercise-child-evidence` | `pins` | PASS pin preflight only |
| `exercise-parent-evidence` | `pins` | PASS pin preflight only |
| `outward-composition` | `pins` | FAIL pin preflight — `Invalid: CMB-OUTWARD-COMPOSITION-REJECTED` |

The first temporary preflight helper mistakenly called three absent/incompatible helper APIs. AST/source inspection identified the correct source_pins comparisons; the corrected helper passes those preflights. Those tooling errors are not classified as repository oracle failures. Initial outputs remain in the tool history; the retained corrected script identifies the reproducible method.

A failure-sensitive representative probe replaced one B1 PINS entry in memory with zeros and supplied a sentinel catalog to observe order. Valid pins allow one catalog access; a mismatched Side Projection fixture digest raises B1 Invalid before any catalog access; restoring the exact pin resumes reads. No repository file bytes were edited. This proves the representative reader's check-before-cache ordering; it does not substitute for all-target tamper verification after maintenance.

## Required future maintenance gate

Before edits: freeze this20-path manifest and exact base, explicit Content disposition, review count and owner; get independent review and coordinator authorization. Capture a RED baseline, all original fixtures and canonical semantic payloads. No auto-refresh script gets an unrestricted path whitelist. Existing versions, grammar, rejection control flow and source trust categories stay fixed.

Refresh in the manifest order: root fixture pins; selection/Snapshot; armed/no-attack; cycle control; Reserve Movement/completion; authority composition; Side Projection literals then fixture; B1 literals then fixture; B2 schema then regenerated children; B3 schema then regenerated parents; outward schema then index fixture. Refresh a child hash only after its predecessor bytes and semantic comparison are final. Historical evidence stays unchanged. No change to bd/selection readers is needed, so actual-positive-entry's predecessor script hashes need not move.

Every affected owning oracle must then be run unmodified to completion:

```sh
python3 -B docs/specs/verify-combat-cycle-sequence-v1.py
python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py
python3 -B docs/specs/verify-combat-inherited-selection-v1.py
python3 -B docs/specs/verify-combat-inherited-snapshot-v1.py
python3 -B docs/specs/verify-combat-inherited-no-attack-v1.py
python3 -B docs/specs/verify-combat-inherited-armed-continuation-v1.py
python3 -B docs/specs/verify-combat-inherited-cycle-control-v1.py
python3 -B docs/specs/verify-combat-inherited-reserve-movement-v1.py
python3 -B docs/specs/verify-combat-inherited-reserve-movement-completion-v1.py
python3 -B docs/specs/verify-combat-authority-composition-v1.py
python3 -B docs/specs/verify-combat-side-projection-v1.py
python3 -B docs/specs/verify-combat-exercise-occurrence-v1.py
python3 -B docs/specs/verify-combat-exercise-child-evidence-v1.py
python3 -B docs/specs/verify-combat-exercise-parent-evidence-v1.py
python3 -B docs/specs/verify-combat-outward-composition-v1.py
python3 -B docs/specs/verify-combat-positive-entry-v1.py
```

The unchanged Result2/settled-continuation/settled-control predecessor oracles required by the accepted bridge must also pass; this packet does not select that bridge. Capture separate historical failures rather than absorbing unrelated repairs. Rebuild before no-build tests so copied JSON fixtures match source bytes. Core test consumers explicitly reference these fixture families in CombatBreakdownCompletionTests, CombatInheritedStepsTests, CombatInheritedCycleControlTests, CombatInheritedReserveMovementTests, CombatInheritedSnapshotTests, CombatHistoryReplayTests, CombatArmedContinuationTests and identity/reaction/movement history tests. Core csproj copies fixture bytes; no stale bin reuse.

Proposed .NET gates, not executed here, use SDK10 native MTP/xUnit v3 and the run-tests/binlog skills:

```sh
dotnet restore Sandtable.slnx /bl:{}
dotnet build Sandtable.slnx -c Release --no-restore /bl:{}
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --filter-class 'Cna.Core.Tests.Campaigns.Combat*' /bl:{}
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --filter-class 'Cna.Core.Tests.Rules.LandSequenceCacheTests' /bl:{}
dotnet test --solution Sandtable.slnx -c Release --no-build /bl:{}
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --filter-trait "Boundary=UserSpace" /bl:{}
dotnet format Sandtable.slnx --verify-no-changes --no-restore
```

The coordinator owns the single full-suite lease. Require fresh independent Ready and successful CI at the exact final head before merge; never count a timeout as a pass.

Semantic comparisons: every pre-B2 game event/input/world/state/projection canonical byte and golden hash must remain exact. For B2/B3/outward, allow only explicitly classified source/build/provenance/report-derived changes. Record causal field paths: B2 schemaHash/predecessor hashes→buildHash→request/child identities→B3 input/materialization/parent evidence→outward artifact/readback hashes and capacities. Compare all authoritative event streams, RNG, clocks, decisions, controller choices, ownership/privacy and lineage classifications independently. Any changed gameplay field, unclassified capacity/size shift, new schema grammar or unexpected file forces a stop and new scope decision. Never globally search-and-replace every64-character digest.

Tamper strategy after authorized GREEN: in an isolated throwaway copy, mutate exactly one target at a time without refreshing its parent; invoke every edge's enforcing reader to show source drift/rejection. Restore exact bytes before the next case. Cover all three root pins, nested successor3/3b pins, both Side Projection literal dictionaries, all B1 literal entries, B2/B3 schema pin maps, B2 buildHash forgery, outward schema inventory and fixture artifact digests/byte sizes. Test both cold and prewarmed catalog/Index caches; stale dependencies must reject before cached data is returned. Mutate/reorder duplicate/unknown/noncanonical raw fixture payloads and canonical semantic event bytes separately, preserving existing rejection coverage. A pin-only tamper success cannot replace semantic forgery tests. Log intended mutation, observing reader, exception, cache-access order, restored SHA256 and success after restoration. Run all affected original oracles again on final restored bytes, not the tampered copy.

## S3 impact and option comparison

| Option | Result | Reason |
| --- | --- | --- |
| Refresh Breakdown alone / any <=3 current-source repair | NO-GO | Immediate selection and independent Snapshot/cycle roots plus transitive/literal/schema pins cannot fit; introduces failures |
| Revert cache / normalize or skip pins / trust old Git blob | NO-GO | Runtime/performance or accepted evidence trust boundary changes outside the task; no pin weakening |
| Full20-file mechanical maintenance now | NO-GO for this session | Derived B2/B3 evidence identities and independent outward Content pin require reviewed larger scope and complete gates; bounded probes do not certify regeneration |
| Retain baseline failures, proceed with isolated actual-entry bridge after reviewed S2 disposition | Recommended conditional path | Actual-positive-entry declares13 matching source hashes and does not pin the failed bd fixture/source root; source-only semantic dependency remains usable |

**Documented fact:** positive-entry uses bd reader/schema but not the failed Breakdown fixture in its declared sourceHashes. Importing bd does not run its main/check_fixture; actual positive entry reconstructs its own narrow creation history. Its source pins need not change under the frozen20-file maintenance manifest. Current native positive-entry acceptance stops before selection; complete Result2 consumption is not established here.

**Inference:** the stale original pins can remain an honest baseline limitation for a new private bridge if its accepted S2/S3 dependency manifest uses those unchanged reader/schema functions and actual positive-entry admission only. A bridge that invokes historical bd fixture admission, inherited Snapshot, or outward composition remains blocked. This is a condition for coordinator/reviewer disposition, not an automatic waiver. Old fixture-backed oracles must still be rerun and their real outcomes recorded. No synthetic lineage is promoted.

## Frozen S3 disposition if maintenance is deferred

**Yes, deferral permits a new private actual-selection executable contract, conditionally.** The permission is bounded by accepted/merged S2, a fresh review of this S1 disposition, and the coordinator freezing the bridge dependency. This report supplies no unconditional implementation approval. The new contract must authenticate the two existing actual positive-entry sources, preserve their literal creation/opening and Movement/Breakdown proofs, and own any new actual-selection bytes separately. It must not import or reinterpret old fixture-backed admission as current source proof. Nothing here certifies existing C3a/Round2/Result2 direct consumption or broad reachability.

Freeze at dispatch:

1. Exact source baseline and accepted S2 research/review head, contract/native manifests and acceptance boundary. S2's final mapping is not supplied to this author; coordinator must reconcile it rather than infer agreement.
2. The three positive-entry contract files below plus all13 declared predecessor sourceHash entries. Preserve every old reader/schema byte; do not rewrite a predecessor oracle to accommodate the new bridge. A new bridge must explicitly pin whichever admitted predecessors it consumes and verify their expected source hashes before relying on cached replay/catalog data.
3. The entire old20-path closure/current digest table, current sequence source `d019a3bc...`, and the historical sequence pin `c5426245...` as a known failed baseline relationship. Do not silently refresh an old fixture or count a generated/fallback semantic probe as the original command.
4. Both owner packets' exact canonical request/created/preamble/Weather/stage/Reserve fields, Movement11→12 and Breakdown12→13 event/input/state bytes, receipts, World/RNG, resolved owner, stateVersion and before-selection boundary. New selection clock/action/round fields are S2/S3-owned additions that need their own authenticated mapping and rejects; no fabricated receipt/Weather/seed translation or synthetic promotion.
5. The independent stale Content relationship in outward, and required oracle results individually. Deferral keeps Breakdown/cycle/Snapshot/outward limitations visible. If the chosen S3 API calls check_fixture for the old Breakdown fixture, Snapshot source_pins(), or outward readback, this disposition is **NO-GO** until that dependency is separately repaired/reviewed or the accepted bridge scope is legitimately revised.

| Frozen actual-entry input path | Expected SHA256 at baseline |
| --- | --- |
| `docs/specs/combat-positive-entry-v1.schema.json` | `73724384ba3cf61b462ef7b47ae70e5108281a6bbaf0b80878c55247e4e6fa36` |
| `docs/specs/fixtures/combat-positive-entry-v1.json` | `eb59146f34bfdc47753b9f0aff7f40709eba4266aff522655ac6886be15b951e` |
| `docs/specs/verify-combat-positive-entry-v1.py` | `b461a8bc6a194def5f03767783b49b5797a5630b6a7c26a986f7bfe2ee97c172` |
| `docs/specs/combat-reserve-designation-v1.schema.json` | `f324fb22e53ff6d9e084c2f65532e5af89277f632087549756b7df1dcb1710d9` |
| `docs/specs/fixtures/combat-reserve-designation-v1.json` | `6c8abb2038bb61c739e3bd3cab5c8adf00689c265ec3c6bc6ea29bebe66b12c2` |
| `docs/specs/verify-combat-reserve-designation-v1.py` | `fa40ec6b10d0d5ecc113aefba47ff4df44ae616851acdc827e9f676f2412e465` |
| `docs/specs/verify-combat-stage-entry-v1.py` | `85cd2b32f9ad104ec29034180775e1ac1905e5aa7f78caa3c1a26e6675901435` |
| `docs/specs/verify-combat-inherited-movement-lifecycle-v1.py` | `f2ee2292df3fe370ace289dcd01747153dac6d78b82c924b2f3da27df1d81920` |
| `docs/specs/combat-inherited-movement-lifecycle-v1.schema.json` | `aac4e07f9546d457defd9042ae701434683e7c22fab2071fe88f872d8d6bc072` |
| `docs/specs/verify-combat-inherited-breakdown-completion-v1.py` | `db903e019bb457c02930876e42a70b9204387494012419b0b97572c3c9e12eba` |
| `docs/specs/combat-inherited-breakdown-completion-v1.schema.json` | `1ff841c704e1aa46e71513ead562a0ca1c54c05b35bdf97018d6854d8773fec3` |
| `docs/specs/verify-combat-inherited-selection-v1.py` | `99e0cdb1cc78089e4af93b8e995ba04483f6bdab86edaf7c6e2e251442cd0bb7` |
| `docs/specs/verify-combat-selection-steps-v1.py` | `dc037282f74c34246d350faebeea55e81677d27dd181de858b8270e4eaa73c86` |
| `docs/specs/combat-selection-steps-v1.schema.json` | `ef24e125012ab390bb0c932a09472b8220d7e8c95c1b19c29bdb611be532162d` |
| `docs/specs/verify-combat-sealed-round-v2.py` | `d1091b5d1d6fb1d9ac88da45313c88fcbc922f763e62d01af44311abc8656bf9` |
| `docs/specs/verify-combat-result-settlement-v2.py` | `a6a781976f26b78a9dc098ebfbb3b516284aa1d5873744d96d8fa23aa50d83a2` |

Verification that **cannot** be claimed from this packet: a fresh complete positive-entry oracle pass (12/60s timeouts); a pass for original Breakdown, cycle-sequence, Snapshot or outward; full native/Core/solution/Boundary tests; fixture regeneration equivalence; integrated new-selection/round/result authority; hosted CI; S3 independence/review approval; public/host/Snapshot/Maproom/Runner acceptance. Only named completed original oracles, corrected pin preflights, source/catalog comparisons, separate8Breakdown semantics/goldens and representative pin-order tamper are passing observations. S3 must run its own executable acceptance, original predecessor gates to completion, classify these baseline failures honestly and obtain fresh counted review and exact-head CI. No baseline waiver is implicit.

## Confidence, unknowns and next owner

High confidence in the exact source change, original two failures, recursive Snapshot failure, normalized hash closure and absence of a compliant three-file refresh. Moderate confidence in a future20-file maintenance path: all visible hash edges/generator relationships are frozen, but no regenerated B2/B3/index output has been produced or semantically reconciled. Full oracle durations and native/CI verification costs were not measured;12/60-second timeouts limit claims. Runtime public/host/Snapshot/Maproom/Runner acceptance remains out of scope. No work on the unresolved historical Runner aggregation incident occurred.

Coordinator must independently review this research, freeze the S1 disposition together with S2's actual chosen bridge, decide whether to defer S1b, and preserve/commit the four research/admin files on the authorized branch. A later expanded repair needs its own frozen edit manifest and evidence classification, then semantic RED/GREEN, tamper sensitivity, all original owning oracles, native gates, independent review and exact-head CI. No implementation starts from this report alone.

## Evidence retention and executed probe commands

Temporary controlled artifacts are at `/private/tmp/combat-pin-inventory-evidence/`; they may be cleaned later. Their content hashes below permit identity checks while present, not availability guarantees. Probe source and outputs are temporary, not additional committed runtime or fixture files.

Executed source comparisons: `git show --stat 2e17f60`; `git diff 2e17f60^ 2e17f60 -- src/Cna.Core/Rules/Cna1979LandSequence.cs`; `git diff 2e17f60 HEAD -- src/Cna.Core/Rules/Cna1979LandSequence.cs` (empty); `git diff 806d2d6^ 806d2d6 -- docs/specs/combat-content-v7.md`; `git diff --check` (PASS).

Executed reproducible retained probes:

```sh
python3 -B /private/tmp/combat-pin-inventory-evidence/inventory_probe.py
python3 -B /private/tmp/combat-pin-inventory-evidence/source_equivalence_probe.py
python3 -B /private/tmp/combat-pin-inventory-evidence/oracle_probe.py
python3 -B /private/tmp/combat-pin-inventory-evidence/pin_preflight.py
NUGET_PACKAGES=/private/tmp/combat-pin-inventory-evidence/packages dotnet build /private/tmp/combat-pin-inventory-evidence/catalog-probe/Probe.csproj -c Release --configfile /private/tmp/combat-pin-inventory-evidence/catalog-probe/NuGet.Config '/bl:/private/tmp/combat-pin-inventory-evidence/catalog-build-{}.binlog'
dotnet /private/tmp/combat-pin-inventory-evidence/catalog-probe/bin/Release/net10.0/Probe.dll
```

The source-equivalence probe removes only the three exact cache additions and asserts equality to parent bytes. The inventory probe normalizes path roots and scans all tracked text; generation-order relationships were added from inspected source afterward. Original-oracle wrapper records per-process12-second timeouts. Additional stdin probes for semantic/goldens, representative pin/cache tamper,13positive pins and the60-second original positive timeout are described above; their successful outputs are retained and hashed. Standalone compile succeeded0warnings/0errors with a produced binlog; it is not a repository build or native suite result.

| Temporary artifact | SHA256 |
| --- | --- |
| `inventory_probe.py` | `d6efce5b6935299de49fe5edd435158a400c8fc93e0f51d49de331e2501d11a2` |
| `complete-inventory.json` | `0c818e8e0bbbae4f5b8d430b19e1c13e0f5e64cf1b332b17a8641a602c05728f` |
| `topological-order.json` | `617df4f7718d4b9377d94d51124239bebd8c8bd218643e6d4e386e5d376de8cf` |
| `source_equivalence_probe.py` | `99a8bdd3d0b3418ce6471b0972dfd131d579eaa424debfd81d93007e7c832316` |
| `source_equivalence_probe.json` | `35d86770bd44445f4b69091e0e7d8a81e1dc64be7189bd76da873275fd8f7f15` |
| `catalog-probe/Program.cs` | `8ab8d40164616da1b62f793c3a8a81348ab4b3a27d2738943993a131c718428c` |
| `catalog-probe/Probe.csproj` | `38e201f047792df530db8c7d157c2b6b19d1ed2e4134dba5da5c59e97d3b0da2` |
| `catalog-result.json` | `287500d32f6c29370b7c01d467ad94e1fb712cc27a53e5ce69e518705933cd7c` |
| `catalog-build.log` | `48998fc8df5a828218a049bdbda444ba9e97af8b47e2b0b521a30e405e561365` |
| `breakdown-goldens.json` | `710a0f481bbcb696a6334da479a6156234da5dd30ac1aec30f351b93882adde9` |
| `oracle_probe.py` | `027200e6d3da833c0be3b2fc01841aa604348cd75eb6bbce0e1a8afb92df3441` |
| `original-oracles.json` | `7252a8bf4530e0f090f379c8b50bd3b42eb75d0390badd9bab9d54880c723719` |
| `pin_preflight.py` | `0f6a3b113e57ced742e34cbd4c952744863d9be3035e1fa84ec75c61f176c238` |
| `pin-preflights.json` | `e63daed9de98c204d249c71d304e8a0aa6cd9e5a27327fe9aafddd0855feb08e` |
| `tamper-result.json` | `706c4a0921366e8598ec8e8232744b0bba5cc67d8c39e8e1337521f59bd3cbfd` |
| `positive-pin-result.json` | `481b31e5531ad6caa120db84690b3f24fa56363d063c73d7b1b9e7b7c7a90d27` |
| `content-drift.json` | `dd05898a663447e5489b97c2ffcbcc1866e6ab912d732a911e82d1dc40b19faa` |
| `catalog-build-20261005-125307--84017--gFzWwJ.binlog` | `ee59c0b04818859fef667aaa38ef790f3501dfd32af82c7ba954a1c95844e1ab` |
