Review instance: **1 of 9**, round1/pass1; recovery **0 of 2**.

## Findings

**No actionable code or implementation-plan defect identified.**

**Known delivery blocker remains unresolved:** required Release verify run38082003227 cancelled at the unchanged 15-minute limit. Retained log shows Contracts and ExerciseRunner completed, Core remained unfinished, and execution was cancelled. No complete hosted Core result exists. See [CI evidence handoff](/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/docs/work/handoffs/2026-10-10-native-actual-round-entry.md:52). This is distinct from a code finding and prevents merge readiness.

## Preliminary Ledger

Recorded to Brain **before reading the author explanation**:

| Item | Independent result |
| --- | --- |
| Scope | Correct worktree, branch, administrative HEAD92d2076, primary64439f7, base34a494c |
| Dirty state | Only excluded `.serena/` untracked |
| Primary hashes | All five matched manifest |
| Immutable inputs | Four round artifacts unchanged; 57 literal pins matched schema and physical bytes |
| Contract/code/tests/plan | No actionable defect identified |
| Remaining investigation | Trusted-context key completeness; retained evidence; historical pending plan statements |
| Delivery | Required CI cancellation retained as blocker |

Author explanation was subsequently read; its SHA256 matched the supplied pin.

## Plan Review

Task019F3 implements the stated private boundary: authenticated actual-selection20 through Prepared CA25, or cancelled no-attack Release. Paid operations, public activation, persistence restart, later lineage, and parent completion remain excluded.

Historical pending statements in the frozen plan are explicitly supplemented by later administrative evidence. They do not falsely establish independent approval or passing Release CI.

The substantial test surface follows the merged contract’s history, grammar, mutation, and temporal requirements. The memo adds bounded synchronization complexity, but I found no authority bypass:

- [ReplayMemo](/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/src/Cna.Core/Campaigns/CampaignCombatActualRoundEntry.cs:36) serializes insertion, eviction, result attachment, and accounting.
- [SnapshotEvidence](/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/src/Cna.Core/Campaigns/CampaignCombatActualRoundEntry.cs:103) frames owned source, both ordered ledgers, serialized trusted Setup/configuration, and RulesetHash; hash lookup also compares complete evidence.
- [Replay](/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/src/Cna.Core/Campaigns/CampaignCombatActualRoundEntry.cs:186) retains dependency and canonical guards before reuse, including consumed-AA Content checks.
- [Transition](/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/src/Cna.Core/Campaigns/CampaignCombatActualRoundEntry.cs:292) independently validates current Apply commands, retry actor identity, lifecycle, allocation, clocks, and admission.
- Readback claims receive fresh parsing and complete byte comparison. Returned buffers are copies.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status |
| --- | --- | --- |
| Administrative commit changes no primary files | Git diff64439f7..92d2076; five hashes | Confirmed |
| 17 shapes and 57 dependency pins match frozen inputs | Automated inventory/literal comparison and physical hashes | Confirmed |
| 34 traces, 224 cuts, 630 suffixes, 2,520 retries | Fixture counts and test loops | Confirmed |
| 5,904 lifecycle invocations | Two representative-owner rows ×2,952 assertions | Confirmed |
| 39,652 mutation invocations | Mutation loops inspected; exact aggregate not independently recounted | Unverified exact count |
| Owned, bounded memo; fresh Apply validation | Full implementation and focused tests | Confirmed |
| Native177 and solution2770 pass | XML contents, case counts, hashes, logs | Confirmed retained evidence |
| Build zero warnings/errors | Retained build log; Core DLL hash | Confirmed |
| Full format passed | Empty retained log plus testimony; hosted log advances beyond format | Consistent; local exit status not independently reproduced |
| Required ReleaseCI incomplete | Retained CI log hash and module/cancellation lines | Confirmed |
| Four inherited failures unwaived | Scope and handoff explicitly retain them | Confirmed documentation; not rerun |

## Verification Performed

Read-only checks included Git scope/history/status, complete primary source and tests, canonical requirements/schema/verifier, fixture inventory, full changed-path classification, and `git diff --check` **passed**.

Verified retained XML hashes and actual test elements:

| Suite | Passed | Failed/skipped/errors |
| --- | ---: | ---: |
| Native acceptance |177 |0 |
| Full Core |2291 |0 |
| ExerciseRunner |469 |0 |
| Contracts |10 |0 |
| Full solution |2770 |0 |

Core DLL hash matched `c836f5e…3bc69`. Bootstrap and author hashes matched supplied values. Final checkout status/head remained unchanged.

**No .NET build/test or long Python oracle rerun was executed by this reviewer.** Existing retained evidence was used; no lease was assumed.

## Open Questions And Residual Risks

Codebase-memory listed the correct worktree, but its October5 generation did not track the new files. Coverage/search confirmed that limitation; source and `rg` fallback supported this review.

Retained evidence does not establish current remote PR/check state. Original aggregation failure remains unexplained despite later complete passes. Local Debug timings cannot predict hosted Release completion. Caller authentication of context and ledgers remains an explicit integration precondition.

## Verdict

**Not ready for delivery**, solely because required exact-head Release CI remains unmet. Independent code/plan review found no actionable defect in the frozen implementation.

## Recommended Next Actions

Brain should reconcile the existing CI blocker within authorized scope, preserve all assertions and pins, and obtain a passing required Release gate for the eventual delivery head. Material implementation changes require another independent pass within the existing review budget.
