# Independent review PR182 CI delta

Review instance: 2 of 9; round1/pass2; recovery0/2.
Exact head: 3f8682b71ca23592b50afef01a15c5f2e2760b1b.
Delta base: c20a84c5d84ca96852ba2bb9d7e1f88b6b668c4e.
Full base: c43b9caec452c152a086412c779bb6495ef1a0ad.

## Preliminary assessment before author explanation

Neutral packet, applicable AGENTS, exact Git state, delta code/tests, and dated investigation read before separate author self-review. Checkout is correct head, only untracked .serena directory. Operational delta is one tests-job timeout expression and two policy tests. The expression selects Core-B20 and preserves other shards15. No defect apparent on blind inspection. Deadline policy tests check all four shards and all other job caps. Need execute bounded policy/helper/actionlint checks, authenticate retained receipts and immutable artifact preservation, and reconcile updated accepted plan. Core-B cancellation evidence justifies investigating a bounded timeout extension, but projected completion cannot substitute for hosted exact-head CI. Author explanation remains unread at this checkpoint.

## Findings

No actionable P0–P3 findings identified. No heavy pivot required. This review approves the scoped CI capacity change at the exact head; it does not assert hosted execution success or native completion readiness.

## Scope and ground truth

Worktree `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`; branch `codex/actual-repeated-movement-completion`; PR182. HEAD independently verified before and after checks as `3f8682b71ca23592b50afef01a15c5f2e2760b1b`. Only untracked `.serena/`; no tracked dirty changes. Delta from c20 comprises 21 paths: CI workflow, helper policy tests, synchronized README/canonical and accepted plans/handoff, investigation/analyzer/result and dated review evidence. One operational workflow line changes; no product/reference implementation changes. Full base remains c43b9caec452c152a086412c779bb6495ef1a0ad.

Applied independent-review and code-review-and-quality skills; Serena manual read. Neutral packet, exact diff/tests, investigation, applicable AGENTS and prior independent review evidence read before separate author self-review. Preliminary assessment written to this report before author explanation. Prior review is used only as retained evidence for unchanged contract artifacts. No structural exploration of product modules was needed; exact Git diffs and physical hashes establish unchanged boundaries. No repository edits, commits, subagents, broad .NET run, contract regeneration/full-normal rerun or hosted retry performed. A read-tool hook rejected one batch with a consecutive-read warning, explicitly permitting retry after resetting its counter; retry succeeded. No approval/safety rejection or unresolved tool limitation.

## Implementation assessment

`.github/workflows/ci.yml:65` uses `${{ matrix.shard == 'core-b' && 20 || 15 }}` against the fixed four-entry matrix at line69. Supported matrix context at job timeout confirmed from official GitHub context table; positive numeric20 is truthy and selects correctly, with15 fallback. String equality behavior matches the test's casefold comparison. Independent actionlint exited0. Official sources: [context availability](https://docs.github.com/en/actions/reference/workflows-and-actions/contexts), [expression literals/operators](https://docs.github.com/en/actions/reference/workflows-and-actions/expressions).

`.github/scripts/test_partition_test.py:278` introduces two deterministic policy tests. First verifies the complete shard set against partition.MODULES, recognizes the exact conditional format or old literal, and asserts every shard's budget. Second asserts prepare/verify15 and dependency-review5. Recognizer is intentionally constrained; an unknown expression fails rather than guessing. Existing text-based workflow checks provide the local convention. Genuine immutable RED864c381 has the old literal15 and retained log showing only Core-B policy mismatch, with other job policy passing. Current tests pass. No generalized expression evaluator or new dependency introduced.

One deadline increase adds at most300seconds to Core-B's cap. Matrix, fail-fast setting, row filters, dependencies, execution command, uploaded artifact naming, SHA/attempt binding and final fail-closed coverage remain unchanged in the exact diff. No secret, permissions, remote-input, continue-on-error or assertion bypass introduced. Readability and maintenance cost are proportional to the requirement; no runtime algorithm changes or additional native rows. Known future scaling concern is documented rather than treated as permanent latency assurance.

## Plan Review

Accepted C02 plan and canonical implementation plan both append identical recovery policy and leave original MC01–MC07 requirements intact. README and durable handoff explain Core-B20, all other caps, evidence and hosted validation requirement. Recovery remains CI-only after two old-cap cancellations; no native extension, downstream consumer, fixture promotion or changed contract authority. Conditional native work still requires accepted merge and sufficient remaining time. Old review0 progress text is clearly historical and followed by current recovery/review2 requirement, not a gate waiver.

Investigation is appropriately bounded: old attempt2 completed2290 of2329 rows with no failed/skipped rows, one active and38 not started. Current/prior raw execution and diagnostic SHA256 all match retained analysis. Independent raw execution parser exactly reproduced all2290 current and2329 prior identities/statuses/durations. Current row set is a subset of prior with39 absent; independently summed matched durations reproduce2516.631 prior versus2992.662 current,18.915407% increase. Job logs show tested merge e0d67dcb054d4dc7490e433cccd91b18fd54d487, attempts1/2, and cancellation during unchanged execute step. Evidence supports bounded capacity mitigation; effective host cause and future completion are explicitly unmeasured. Merge ancestry and exact platform timeout origin remain Brain-supplied reconciliation, not independently re-fetched here. Projected16–17minute envelope remains estimate rather than demonstrated20minute success.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Core-B alone20; other shards/prepare/verify15; dependency5 | Exact workflow diff, matrix, independent2policy tests, official context/operator docs, actionlint | Confirmed | Scoped trusted numeric configuration |
| Genuine RED on old Core-B15; GREEN2/2; helper98/98 | Immutable864c381 workflow and failure log, matching receipts; independent execution | Confirmed | Tests are failure-sensitive to old policy |
| No workload/partition/native/reference change | Exact delta paths; empty Git diffs for src/tests/global/build/package/partition against c20 and fullbase | Confirmed | Prior product review evidence remains applicable only to unchanged artifacts |
| Cancellation while2290passed and cumulative slowdown | Raw execution parser, raw log/diag hashes, retained per-row analysis, job cancellation logs | Confirmed with platform/ancestry limit |20minute cap justified as validating experiment |
| Frozen artifacts match824.354s finalnormal | Current oracle/schema/fixture SHA equals receipt pre/post and prior report; retained fullnormal log hash matches | Confirmed retained evidence | No fresh normal gate claimed |
| Future host cause unknown and hosted full coverage still required | Investigation, accepted plan, handoff/self-review | Confirmed | No partial pass or discovery success waiver |

## Verification Performed

Independent commands executed serially with subprocess timeout120seconds each; no timeout:

- `python3 -B .github/scripts/test_partition_test.py WorkflowDeadlineTests -v`: exit0,2/2,0.093s elapsed.
- `python3 -B .github/scripts/test_partition_test.py`: exit0,98/98,0.352s elapsed.
- `actionlint .github/workflows/ci.yml`: exit0, no diagnostics,0.056s elapsed.
- `git diff --check c20a84c HEAD`: exit0,0.028s elapsed.

Read-only identity checks:

- All4CI receipt/log pairs hash-match; helper gzip decompressed bytes hash-match exactly (1085bytes). RED exit1 is genuine assertion evidence; other3exit0. No trailing newline normalization.
- Current investigation SHA `4f828fbbe9511a3b166e8331848ed088a144d5de1fc6155431d045fbf04d8726` matches author packet.
- Four current/prior raw execution and diagnostic hashes match retained analysis. Independent execution-row parsing equals retained row maps and exact matched timing sum.
- `git diff --exit-code c20a84c HEAD -- src tests global.json Directory.Build.props Directory.Packages.props .github/scripts/test_partition.py`: exit0; same command using c43b9ca: exit0.
- Frozen oracle SHA `7e465e06f3f9500a244575abc6f95faf10a93391634571f17ffdfdf0913015a8`; schema `77ef1573e24e131fdddbe4daf733a4472a6dd79b15c6eb227be545ae7c687a2c`; fixture `1cba27ef5f08e4fa72695d4fc10676aadc3bb172179664c9d758d5f861b8e86a`: all match finalnormal pre/post identities and prior report.
- Retained finalnormal log SHA `6feb055f3377c0c8514e2893a8f9576ef701ff6e8453f9fc0c7a001ed8e3ffe8` matches824.354s exit0/no-timeout receipt.
- Final HEAD/status rechecked: exact reviewed head unchanged; only .serena untracked.

## Open Questions And Residual Risks

Hosted exact-head CI is not reconciled by this reviewer and remains required before merge: successful owning jobs/processes, current attempt binding, full2329Core-B and aggregate2879 prepared coverage. Prior2290partial successes are failure evidence, not pass credit. Additional environmental slowdown may consume new headroom; if new cap expires or a row becomes stationary, collect targeted diagnostics and return decision gate rather than increase cap again without evidence. Fullnormal was not rerun because product artifacts are unchanged; prior independently inspected review and authenticated author fullnormal remain scoped evidence. Original raw CI artifact preservation and GitHub merge-parent verification are Brain responsibilities. No heavy pivot necessary.

## Verdict

**Ready** for scoped CI delta and unchanged private executable contract artifacts at exact head `3f8682b71ca23592b50afef01a15c5f2e2760b1b`, with no actionable findings. Engineering review disposition is complete; hosted exact-head validation and merge remain separate Brain gates.

## Recommended Next Actions

Brain should retain this report and reconcile one fresh exact-head hosted validation with all owning jobs and exact attempt/exhaustive coverage before merge. No fix, pivot, further reviewer or native extension requested. Review instance2of9, round1/pass2, recovery0of2 unchanged.
