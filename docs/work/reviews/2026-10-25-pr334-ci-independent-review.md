# PR334 CI correction independent review

Review instance: 7 of 7. Explicit user-approved final instance.

Target: `0d4e5b726677ee5ece5f919d553f40b9dfa8e368..3bab87b1460e078116dac327443780ff1936a5c0`, branch `codex/rc-reserve-crash-recovery`. Initial and final source worktree clean; this report is sole reviewer write. All 20 changed paths reviewed. No source edits, commits, pushes, or delegated reviews.

## Findings

No actionable defect found in the 20-file correction itself. Original P01 archive selection, original evidence preservation, separate refreshed catalogue, and strict scheduling additions withstand focused verification.

**P1 — Broader PR remains blocked by lost RoundEntry at-use Content revalidation.** Coordinator supplied new exact-head CI failures during review. Independently reproduced three selected failures at this head. `CampaignCombatActualRoundEntry.cs:437` and `:478` consume through authenticated snapshot `Read`; `CombatDependencySnapshot.cs:42` returns owned bytes without rereading original source. Thus provider disappearance/change after successful preflights is no longer observed at consumption. `docs/specs/combat-actual-round-entry-v1.md:88` explicitly requires rejection in that scenario, including warm recovery. `CombatActualRoundEntryTests.cs:136` fails for both missing/changed variants; `:153` fails on 57 versus required 58 physical reads before reaching remaining ownership/forgery assertions.

These production/test files are unchanged within corrective base..head: inherited from preceding snapshot implementation, not newly introduced by this correction. Nonetheless exact-head delivery cannot pass. Preserve existing tests and restore physical Content revalidation at use while retaining owned authenticated bytes. Explicit RoundEntry consumption fix changes a frozen refreshed source pin and requires consistent downstream evidence regeneration. Hidden family-specific behavior in generic snapshot acquisition is not established as an equivalent small repair.

## Plan Review

Blind preliminary ledger recorded before author explanation: archive provenance, unchanged original pins, refreshed catalogue completeness, fallback authentication, and strict CI row coverage. No initial confirmed defect. Read author report only afterward.

Original correction plan is bounded and appropriate: preserve original P01 identity, select original oracle explicitly, authenticate bundled original dependencies, retain generic current readers, and add measured scheduling rows without relaxing CI checks. README, technical design, naming overview, revision spec, physical adapter spec, and RC checklist consistently retain private/unadmitted status and open release gates.

Broader plan now needs explicit remediation decision for RoundEntry source pin and dependent evidence. Initial inference that callback-count tests were obsolete was withdrawn after checking canonical at-use requirement. Existing tests encode required behavior and must remain.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Original archive/catalogue/import/Control unchanged | Byte comparison against base Git blobs | Confirmed; original evidence not rewritten |
| Supplemental archive contains 115 exact original files | Every ZIP payload compared with `git show 3e2d4336:<path>` | Confirmed |
| Authentication precedes extraction | Python helper source and both archive-negative subtests | Confirmed |
| Fixed original adapter selects original dependencies | `ValidateRetained`, graph caller trace, Core/Host P01 reruns | Confirmed; no public admission |
| Refreshed 300-row catalogue is separate and complete | Same original key set; exact equality with retained four owning-oracle outputs; all 300 native XML identities passed after normalizing MTP quote escaping | Confirmed retained evidence; independently regenerated row 0 also matches |
| Existing weights unchanged, 16 additions | Full original row-map comparison and strict discovery rerun | Confirmed |
| Core P01 10/10, Host P01 46/46, partition 28/28 | Independent focused reruns | Confirmed |
| Complete original P01 shards/full Core not locally claimed | Author report and RC checklist explicitly retain gaps | Confirmed honest limit; new CI failure is additional blocker |
| Other prior boundary/build/format/representative checks passed | Author report only; not all independently repeated | Unverified as fresh executions; not used to override reproduced failures |

## Verification Performed

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --no-restore -- --filter-class '*P01*'`: exit 0, 10 passed.
- `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-class '*P01*'`: exit 0, 46 passed. External physical archive not configured; no external-gate claim.
- `python3 -B docs/specs/test-combat-p01-force-seal-fresh-v1.py LegacyDependencyArchiveTests`: exit 0, 1 test passed, both archive variants exercised.
- `python3 -B -m unittest discover -s .github/scripts -p long_suite_partition_test.py`: exit 0, 28 passed with approved process access. Initial sandbox execution had one process-supervisor error; initial `dotnet test` attempts failed named-pipe binding. Those environmental failures are not passing results.
- `python3 .github/scripts/long_suite_partition.py discover --sha 3bab87b1460e078116dac327443780ff1936a5c0 --attempt 7 --directory /private/tmp/pr334-review7-discovery`: exit 0; 3340 ordinary Core rows, 61 long processes, 17 total matrix jobs. This does not mean 17 ordinary-suite jobs.
- `dotnet exec artifacts/bin/Cna.Core.Tests/release/Cna.Core.Tests.dll --filter-method '*ContentIsRecheckedAtConsumptionAfterTwoPassingPreflights' '*WarmReplayOwnsBytesAndCannotBorrowEitherLedgerOrTrustedContext'`: exit 2, 3 failed, independently reproducing at-use and work-bound failures.
- Inline read-only Python comparisons: 115 archive payloads match original Git; unchanged original archive/catalogue/import/Control; all previous weights unchanged, exactly 16 additions; identical 300 parity keys; refreshed catalogue equals retained owning-oracle outputs; native XML has all 300 selected identities and all Pass. Initial exact-name comparison exposed XML quote escaping; normalization fixed comparison without changing test data.
- Inline Python import of current owning oracle and `verify([0])`: exit 0, independently generated row equals refreshed catalogue. Remaining 299 oracle rows assessed through retained outputs, not freshly regenerated.

Graph verification: indexed project `Users-dsteele-.codex-worktrees-3d3c-sandtable`; discovery, caller/callee trace, exact snippets, and coverage checks used. Nine correction code/test paths reported matching metadata/no recorded gaps. Additional RoundEntry/snapshot/revision paths checked; RoundEntry test coverage reports missed line 488, read directly with surrounding source. Graph signals are best effort. CCE `context_search`/`session_recall` unavailable in supplied tool inventory.

## Open Questions And Residual Risks

Exact-head hosted CI remains coordinator-owned and cannot be treated as green. Full Core, all original P01 shards, all owning oracles, external physical archive, Snapshot/Outward reconciliation, and full RC certification not independently rerun or certified here. Ignored local artifacts supply retained diagnostic evidence, not durable hosted certification.

RoundEntry source bytes are themselves frozen in revision registration. Production repair must preserve original legacy pins and propagate refreshed identities consistently. A central family-specific consuming delegate would need explicit treatment of revision inheritance, `delegate.Target` identity, nested acquisition/revalidation, both revisions, copy ownership, and callback bounds; current proposal lacks completed evidence.

## Verdict

**Not ready** for delivery at `3bab87b1460e078116dac327443780ff1936a5c0`: original corrective diff passes bounded review, but newly confirmed broader-PR RoundEntry regression violates canonical requirement and exact-head CI. No approval of an unimplemented workaround or future head.

## Recommended Next Actions

Return remediation scope/evidence decision to user: explicit RoundEntry at-use repair with required refreshed-pin regeneration, or separately specified and justified central policy. Preserve failing tests. Complete chosen repair and fresh exact-head CI before merge; retain unrelated RC gates. Review limit 7/7 exhausted; no further reviewer instance starts automatically. Any material architecture/scope change or additional independent review requires explicit user decision.
