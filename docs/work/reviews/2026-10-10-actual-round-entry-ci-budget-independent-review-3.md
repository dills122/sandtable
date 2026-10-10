Review instance: **3 of 9**. Verdict: **Ready for the bounded research recommendation and proposed experiment only**. No actionable findings remain in that scope. This does not approve native implementation, certify hosted runtime, or authorize replacing the required gate.

## Preliminary ledger

Recorded and sent to Brain before reading either author note:

- Product snapshot `64439f7` differs from research checkpoint `4c5162b`; the experiment must use the actual product/experiment SHA.
- Full Debug macOS ARM results and hosted Release cancellation are distinct evidence.
- Balanced summed test durations cannot predict shard wall time.
- Coverage requires argument-bearing row identities and fail-closed handling of deferred or ambiguous rows.
- `verify` must fail for unsuccessful dependencies, missing evidence, filter errors, or incomplete module coverage.

The recommendation explicitly addresses these concerns.

## Findings

None actionable for the research/experiment plan.

The source scope is verifiable: branch and HEAD match the bootstrap; the two new notes match their supplied hashes; `.serena/` remains excluded. The native files absent from the research checkout were inspected through immutable product-head snapshots.

## Plan review

Two complementary Core method partitions are a credible small experiment. They split the native serial collection while preserving whole theory methods, current collection concurrency, unfiltered ExerciseRunner/Contracts, and existing build/format obligations. This avoids a product change whose aggregate savings remain unmeasured.

Acceptance is appropriately conditional:

- Same-head Release discovery and executed-row reconciliation precede adoption.
- Counts alone cannot establish coverage.
- Missing, unexpected, duplicate, skipped, NotRun, or ambiguous generated rows fail.
- Every direct preparation/test dependency must explicitly succeed.
- The existing `verify` identity must execute after unsuccessful prerequisites and reject their statuses.
- Leaf timeout remains failed evidence.

The plan does not promise that two partitions suffice or establish a whole-pipeline 15-minute SLA.

## Author-claim reconciliation

| Claim | Verification | Status |
|---|---|---|
| Full Debug 2770 PASS, zero skips/failures | Parsed all three retained XML reports and full-run log: Core 2291, ExerciseRunner 469, Contracts 10 | Confirmed |
| Hosted Release timed out without Core completion | CI log and manifest show SDK 10.0.401, other two module completions, cancellation, empty artifact inventory | Confirmed from retained evidence |
| Rehearsal preserves all Core rows | Reconstructed XML identities; A/B overlap 0, missing 0, unexpected 0; exact selector maps to A | Confirmed |
| A has 79 rows, B 2212; 1194 methods total | Independent XML/JSON computation | Confirmed |
| Native standalone/full rows match despite regenerated IDs | 177 identities match; raw-ID intersection 0 | Confirmed |
| Collection concurrency serializes native rows | XML collection and official xUnit configuration documentation | Confirmed |
| Multi-value include/exclude method filters are supported | Official xUnit MTP documentation | Confirmed at documented interface level; executable behavior remains an experiment gate |
| Required skipped gate can satisfy protection | Official GitHub protected-branch documentation | Confirmed |
| Hosted timing fit and stable Release row mapping | No completed experiment exists | Unverified, accurately labelled |

Official references support the configuration and gate requirements: [xUnit MTP options](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform), [parallel configuration](https://xunit.net/docs/config-xunit-runner-json), [MTP discovery/version limits](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-mtp), [XML identities](https://xunit.net/docs/format-xml-v2), [direct dependency statuses](https://docs.github.com/en/actions/reference/workflows-and-actions/contexts#needs-context), and [required-check semantics](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#require-status-checks-before-merging).

## Verification performed

- Read the independent-review skill, neutral manifest, current configuration, retained logs/XML, and rehearsal before the recommendation.
- Ran `git status --short`, `git branch --show-current`, and `git rev-parse HEAD`.
- SHA256-verified the neutral manifest, all 15 immutable source snapshots, all 13 retained artifacts, and both new notes.
- Parsed XML using Python `xml.etree.ElementTree`; recomputed totals, timings, unique identities, selector membership, partition union/intersection, and native cross-run identity agreement.
- Inspected product-head test excerpts, including the exact 58-callback assertion, local-memo concurrency test, mutation theory, and explicitly isolated collection.
- Ran `git diff --check`: passed.
- No .NET runs, CI launch, edits, commits, or additional reviewers.

## Residual risks and next action

Cold process caches, repeated setup cost, hosted CPU scheduling, Release/generated-row discovery, actual two-shard headroom, and implemented failure propagation remain unproven. The earlier aggregation anomaly and inherited verifier failures remain unresolved and unwaived.

Brain can reconcile this report and select the bounded CI experiment. Adoption requires its complete same-head Release coverage and failure-propagation evidence; native implementation review remains separate.
