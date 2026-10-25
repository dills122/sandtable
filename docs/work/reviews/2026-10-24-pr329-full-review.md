# PR329 full independent review

Review instance **4 of 4**, explicitly requested by user after earlier3-instance
allowance was exhausted; count was not reset. Reviewer `/root/pr329_full_review`
started with no inherited conversation and neutral bootstrap. Preliminary ledger
recorded before author explanation. Read-only review, no further delegation.

Target `fda44fad30b0dcb21b729cf12f3e3031d80e4867`, merge-base
`16d76448a008ea4f16299402b8ef44e8c96a4843`, branch
`codex/rc-overnight-plan-20261023`. Initial tree clean. Entire121-file PR reviewed:
runtime, contracts, persistence, tests, tooling, CI and documentation.

## Findings and disposition

**P3:** native-retreat-timeout-v1.md26–27 said Selection no-selection deadline
remains open, contradicting V38/schema14 in same PR. **Accept:** replaced with
link to native-selection-timeout-v1.md and bounded-scope statement in this
review-followthrough commit. No confirmed P0–P2 source findings.

## Plan review and claim reconciliation

Bounded reliability plans followed: Core owns source-legal System fallback;
Host owns immutable entry deadlines, immediate transactions, accepted retries
and cold replay. Schemas5–13 deliberately read-only; no invented timestamps or
implicit migration. Genuine repeated-history witnesses strengthen earlier
synthetic fixtures. Separate child-process recovery is correctly distinguished
from SQLite saved-copy reopen. Local test tooling reuses CI partition definitions.

| Author claim | Evidence reviewed | Result |
| --- | --- | --- |
| Seven entry deadlines | Initializers, submissions and lane verifiers | Confirmed |
| Invalid proposals precede deadline mutation | Revision/map/HMAC and Core transition validation | Confirmed source/regression design |
| Source-legal System fallback | V33–V38 constructors, replay regeneration and existing projections | Confirmed |
| Genuine repeated history | Repeated Custody/Retreat, third-cycle Reserve, integrated tests | Confirmed |
| Exact-entry failure preserves paid history | EntryClockFailureAtCustodyOrRetreatEntryKeepsPaidHistory | Confirmed test design |
| Bounded fresh-process coverage | Child protocol: initial Retreat, repeated Selection/RBA/Force | Confirmed; other cuts open |
| Historical suite passes prove current head | Author explicitly disclaims this | Correct; final CI required |

## Verification

Reviewer: merge-base/head/full diff inventory matched; diff --check passed;
local_tests_test.py9/9, r02_partition_test.py33/33, test_partition_test.py133/133
passed. Host dry-run emitted correct Release command. long_suite_partition_test.py
executed28 with1error; diagnostic wrapper identified resource guard failure:
`[Errno 1] Operation not permitted: 'ps'`. Unchanged supervisor/guard sources;
not counted as reviewer pass. Coordinator reran with sandbox restriction lifted:
**28/28 passed, exit0,1.049s**, log `artifacts/pr329-merge/long-suite-helper.log`.
No .NET/full-suite rerun by reviewer; full final-head CI remains merge condition.

Graph coverage checked all changed paths and tooling dependencies. Partial
CampaignExercises.cs/CampaignHostAdapter.cs coverage handled by direct affected
source/diff/context reads; no completeness inference. CCE unavailable.

Coordinator also reran original Breakdown/cycle Python oracles: both exit1 on
Cna1979LandSequence.cs pin, receipts in artifacts/pr329-merge. Relevant source
and oracle files unchanged from base; baseline already recorded same failures.
These remain release/certification blockers, not demonstrated PR regressions.

## Residual risks and follow-ups

[Tracker330](https://github.com/dills122/sandtable/issues/330) retains owners/pass
criteria for remaining process/crash proofs, source-pin reconciliation, public
certificates/Runner evidence, dependencies and playable six-turn product gates.
Scheduler reconstructs history under immediate transaction every tick; benchmark
sweep latency/lock duration across long histories and >16 campaigns per lane
before broader hosting. Preserve verification guarantees during optimization.
Historical schemas need explicit future migration or retained readers. No claim
that all permutations/new tests were independently executed. No public admission.

## Verdict

**Ready with non-blocking follow-ups** for bounded engine PR, contingent on P3
correction and successful exact-head CI. Coordinator accepted P3 and residual
follow-ups. Not public certification or release approval. Review loop stops4/4.
No material runtime change in documentation followthrough; final CI must bind
that resulting head. No merge performed by this task.
