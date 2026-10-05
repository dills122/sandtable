**Ready for S1a acceptance. No actionable P0–P3 findings.**

Review instance: **S1a set1/pass1,total1of9**. No additional reviewers, recovery spikes, implementation, commits or publication were initiated.

## Findings

Verified branch `codex/combat-pin-inventory`, base `afa396ad5094fae7b8f054c60a9df9e03f83dfd5`, and head `cb16f676f09d81e124fcdcc7d7533cf2ad9a199c`.

The diff contains exactly four Markdown files, totaling493 added lines. All four match the retained packet manifest. Only `.serena/` is untracked; HEAD remained stable.

The packet’s detached/uncommitted wording is historical and superseded by the supplied committed head. It did not prevent verification of the actual scope.

A preliminary ledger was recorded before reading the separate author explanation. The research report already exposed its recommendation, so the recommendation itself could not receive a fully blind assessment.

## Plan Review

The [maintenance inventory](/Users/dsteele/.codex/worktrees/combat-pin-inventory/sandtable/docs/research/combat-verification-pin-maintenance.md:53) fulfills S1a’s research boundary:

- Independent traversal confirmed **20 live files:15fixtures,3schemas,2Python readers**.
- Fixture-only closure is **14 Breakdown-rooted /15sequence-source-rooted**.
- All65 documented pin pointers/literal locations resolve correctly.
- The proposed order violates no documented hash dependency.
- Schema-to-fixture and reader-to-fixture generation relationships explain the additional ordering constraints.

The causal maintenance assessment is sound. [B2’s build descriptor](/Users/dsteele/.codex/worktrees/combat-pin-inventory/sandtable/docs/specs/verify-combat-exercise-child-evidence-v1.py:54) includes its schema hash and predecessor pins. Their refresh changes build identity, manifest/child evidence, B3 materialization and aggregate evidence, then outward readbacks and capacities. Replacing visible pin fields alone cannot certify the resulting evidence.

The independent Content-document drift is confirmed. PR116 changes status/follow-up prose; those bytes are nevertheless enforced by outward pin checks. Sequence maintenance alone would leave that limitation unresolved.

**Maintenance disposition:** NO-GO under the current three-file allowance. Deferral is a reasonable session decision given the ungenerated derived evidence and unmeasured verification cost. A separately authorized20-file maintenance slice remains technically credible; this review does not establish that broader maintenance is intrinsically unsafe or impossible.

No heavy architectural or gameplay pivot is required.

## Author-Claim Reconciliation

| Claim | Assessment |
|---|---|
| Earlier11/12fixture counts omitted specification-relative Exercise pins | Confirmed independently |
| Full live closure contains20files and65pin edges | Confirmed within the declared tracked-text/JSON scope |
| PR146 changes catalog caching while preserving constructed values and validation | Confirmed by exact Git diff, copied-source comparison and standalone probe |
| Breakdown semantic/golden success does not repair the original failing oracle | Confirmed; separate probe passed while original command failed |
| B2/B3/outward require derived-evidence reconciliation | Confirmed by source predicates and generation flow |
| Three original downstream commands passed; others hit stated bounds | Confirmed as retained execution evidence; not all independently rerun |
| Positive-entry can operate without old Breakdown fixture admission | Confirmed by source and a fresh complete positive-entry oracle pass |
| Preservation/branch creation remained blocked | Historical; superseded by verified committed head |
| S3 can proceed unconditionally | **Not claimed and not established** |

The cache-equivalence evidence supports catalog values, successor/error behavior and immutability. Allocation and object-reference reuse intentionally differ; the evidence should not be generalized beyond that contract.

## Verification Performed

Executed against the exact worktree:

| Check | Outcome |
|---|---|
| Git scope, HEAD, status and packet hashes | Matched |
| Working-tree and committed-diff `git diff --check` | Passed |
| Independent tracked-file pin traversal | Matched20-file closure and14/15fixture counts |
| All documented hashes and65edge locations | Matched |
| Proposed dependency order | No violations |
| Additional scan for54B2/B3 build/manifest/child/parent identities | No live holders outside the maintenance scope found |
| Original Breakdown command | Exit1: sequence-source drift |
| Original cycle-sequence command | Exit1: same source drift |
| Snapshot `source_pins()` | Rejected the same stale root |
| Outward `pins()` | Rejected; independent Content drift confirmed |
| Original positive-entry command | **Passed completely** |
| Separate Breakdown semantic/golden probe | Passed8cases,16cuts,8retries,426mutations,92raw rejects,298boundary probes |
| Supplied standalone catalog executable | Passed cold/warm/evicted comparisons, successors/errors, immutability and128parallel requests |
| B1 representative in-memory pin tamper | Rejected before sentinel catalog access; exact restoration resumed reads |

The positive-entry pass reproduced two actual owners, two events each, six literal proofs, six cuts/retries and3,966 rejection probes. This is new reviewer evidence; it does not retroactively change the author’s accurately reported12/60-second timeouts.

Copied current/baseline sequence sources and supporting types matched repository/Git bytes. The baseline comparison accounted only for its test-only class rename.

One supplemental harness invocation initially used an incorrect `trace()` signature. Source inspection localized that harness error; the corrected probe passed. It was not classified as a repository failure.

Serena was activated for the exact worktree. Codebase Memory generation `2026-10-05T12:49:20Z` supplied C# discovery and cited-path coverage. `docs` is excluded, so source reads and tracked-file scans supplied contract/fixture evidence. Proposed .NET syntax was checked against local nativeMTP/xUnit configuration using run-tests/platform-detection guidance.

No repository .NET build, focused/full/Boundary suite, fixture regeneration or hosted CI was executed.

## Open Questions And Residual Risks

The inventory is complete within its stated scanning method, not proof against every future dynamic consumer. Generated outputs remain unproduced and unreviewed.

The16frozen actual-entry inputs match current bytes. They are a necessary contract dependency boundary, not certification of a future native bridge. In particular, imported positive-entry replay helpers do not themselves execute the complete verifier’s predecessor-hash loop; the new bridge must enforce its admitted dependency pins before using replay or cached data, as the packet requires.

S2 must still establish authenticated selection inputs, actors, clocks, receipts, versions and canonical mapping. Existing C3a/Round2/Result2 compatibility, broad reachability, synthetic promotion, repeat/later-II/consumed lineage and public activation remain unproved.

Temporary evidence availability remains limited. Historical results retain their original status.

## Verdict

**Ready for the S1a research packet and conditional maintenance-deferral disposition.**

**S3 may proceed with the failed baseline unchanged only after accepted/merged S2 and an explicit coordinator freeze demonstrate that the chosen bridge consumes actual positive-entry admission without invoking blocked historical Breakdown fixture admission, recursive Snapshot validation or outward readback.**

Those failures must remain individually reported. This verdict grants no automatic oracle waiver or S3 implementation approval.

## Recommended Next Actions

Accept S1a, record its deferral disposition, then reconcile it with the independently reviewed S2 dependency manifest before S3 dispatch. Require S3’s own executable acceptance, completed predecessor checks, fresh counted review and exact-head CI.

Preserve the review count and original evidence statuses; no count reset or broader repair is warranted by this report alone.
