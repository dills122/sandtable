**Ready for the private S3 executable-contract scope. No actionable findings remain. Hosted delivery remains pending corrected-head CodeQL and required CI.**

Review instance: **set2/pass1, total4of9**; recovery **R1ofmax2** consumed, no count reset.

Branch: `codex/combat-actual-selection-contract`  
Base: `96596dde066b0d8c9a0110eba50fcfcb01d99a46`  
Reviewed head: **`3cc2bcc298e19df3f9075bc2e5f29a22d873ea05`**  
Behavior checkpoint: `970fa4184b6aed787b545a6c3c7b751010add1fc`

**Findings and plan review.** The five primary paths and three administrative packets match authorized scope. Both owners reach defender decline19 and positive FA20 without completion; seven fallbacks per owner reach Reserve Release. Full original provenance, separate trusted ledger, canonical framing, retries, ownership, bounded caches and dependency admission remain intact.

The correction in [state_clock_precedence_checks](../../specs/verify-combat-actual-selection-v1.py) changes only the final diagnostic block. Comparisons, mismatch collection and nonempty rejection remain unchanged. Print and assertion now contain count/fixed text. Reduced diagnostic detail is a reasonable bounded tradeoff. No trust rename, suppression, CI change, schema change or fixture regeneration occurred. No heavy pivot is needed.

Preliminary concerns were recorded before reading the author packet and prior reports.

**R1 assessment.** Independently inspected the old/new source flow, pinned dictionary-only `trusted()` helper, fixed command construction and output sinks. The research diagnosis is supported: the matching CodeQL [name heuristic](https://raw.githubusercontent.com/github/codeql/6e9f9e38390175c41b99070a423c875f450759ca/shared/concepts/codeql/concepts/internal/SensitiveDataHeuristics.qll) includes `trusted`, and its [Python model](https://raw.githubusercontent.com/github/codeql/6e9f9e38390175c41b99070a423c875f450759ca/python/ql/lib/semmle/python/dataflow/new/SensitiveDataSources.qll) follows such functions. No secret acquisition exists in this fixed helper. The repair removes raw output paths rather than obscuring their source.

Hosted SARIF/check metadata was retained research evidence, not freshly queried here. A clean corrected-head CodeQL result remains unverified.

**Author-claim reconciliation.**

| Claim | Independent result |
|---|---|
| Only diagnostic tail changed since pass3 | Confirmed by diff and full-AST equivalence after restoring the old tail |
| Failure detectable without sentinel exposure | Confirmed in diagnostic suffix and whole-function probes |
| Both earlier ordering defects remain detectable | Confirmed by separate in-memory mutations |
| Successful bytes/counts unchanged | Confirmed by direct oracle and output comparison |
| Sixteen pins, independent ledger and cache boundary preserved | Confirmed by source, hashes and executed acceptance |
| Production authentication/full-suite/CI proved | Not claimed; remain pending |

**Verification performed.**

- `python3 -B docs/specs/verify-combat-actual-selection-v1.py` — **exit0**, empty stderr. Reproduced16 semantic/literal traces,152cuts,1,962retries,1,512entry-leaf mutations,4,456event mutations,8,266proof mutations,128pin rejects,2cold-separation checks,328earlier ordering probes and15,510state/clock probes.
- Successful stdout exactly matches retained pass3/R1 output. SHA256: `0ec2ecc01734e319a6abd590a7bf1484cf5569d657874981e893a6c93d8cc874`.
- Sentinel RED/GREEN — old block exposes sentinel through stdout/stderr/assertion; corrected block and whole function fail detectably with sentinel absent from all three.
- Ordering mutations — arm/segment regression rejects; state/clock regression produces **910 count-only mismatches** and rejects.
- Independent literal framing — **PASS**,136event receipts,16source/segment identities and576artifact metadata checks. Both current primary manifests match.
- All16dependency hashes match; spec/schema/fixture byte-identical to `e5645c4`; **898 existing spec/runtime/test paths unchanged** against base.
- `git diff --check 96596dde HEAD` and `git diff --exit-code HEAD` — **exit0**.

**Evidence explicitly reused, not freshly executed:** original positive-entry/C3a/Round2/Result2 passes; separate Breakdown/cycle-sequence sequence-pin failures, Snapshot recursive-pin failure and outward Content-drift failure. Prior standalone proof-cache capacity and supplemental compatibility evidence remains applicable because those implementations and inputs are unchanged. Historical timeouts remain unverified.

**Residual risks and next actions.** No .NET/full-suite/Boundary/format lease or hosted CI was run; none is waived or reported passing. Native parity, production seat/clock/store authentication, actual Round/result/repeat admission and outward privacy remain separate gates.

Coordinator reconciliation and corrected exact-head CodeQL/required CI must precede merge and S4. Final head stayed exact; only `.serena/` is untracked. No fixes, commits, publication, merge or subreviewers occurred. Serena used the supplied checkout; Codebase Memory excludes `docs/`, so focused source reads supplied coverage.
