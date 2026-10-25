# RoundEntry final independent review

Review instance: 8 of 8. Read-only review of `3bab87b1460e078116dac327443780ff1936a5c0..91b313c33252b217ded7bb0738b9fa0d1b4751af`, branch `codex/rc-reserve-crash-recovery`. HEAD and clean tracked tree verified before inspection. Scope excludes unrelated Snapshot/Outward repair, public RC certification, and merge.

## Preliminary findings ledger — before author explanation

- Canonical RoundEntry contract explicitly requires physical Content recheck after preflight, including warm recovery. Changed `PinnedBytes` dispatch restores that path through owned snapshots.
- `ReadRevalidated` walks recognized snapshot delegates to original provider, copies current bytes before comparing, checks every acquired ancestor, and wraps expected provider/parent failures with consumer error. Existing immutable `Read` behavior remains unchanged.
- Existing consumption tests distinguish two passing preflights from failing third read, and warm preflight from failing second read; new two-revision test covers three nested snapshots, independent returned bytes, same-length mutation, missing file, and unknown-path no-I/O.
- No actionable implementation defect identified in initial source/test pass. Pending verification: physical pin closure, generated fixture semantic differences, legacy compatibility and retained test/oracle evidence. Large generated evidence diff needs substantive inspection before verdict.
- Graph coverage clean for changed runtime/test files except pre-existing parse gap at `CombatActualRoundEntryTests.cs:488`; direct source fallback required. Graph calls are best-effort, not completeness proof.

Author explanation and evidence not read before recording this ledger.

## Findings

No actionable findings in reviewed base..head. Previous RoundEntry at-use failure is repaired by explicit consumer behavior, without changing original regression assertions or generic acquisition policy.

`CampaignCombatActualRoundEntry.cs:478` selects `ReadRevalidated` only for authenticated snapshot readers. `CombatDependencySnapshot.cs:49` recursively reaches original provider, validates current owned copy against every ancestor snapshot, and maps expected I/O and nested JSON failures to consuming snapshot error. Warm replay checks before returning cached results (`CampaignCombatActualRoundEntry.cs:209`); fresh empty-AA transition uses same pinned reader (`:435`). Existing immutable reads and registered-revision selection remain intact. Same-length mutation, disappearance, unknown path, nested ownership and both revisions are exercised by `CombatDependencySnapshotTests.cs:45`.

## Plan Review

Approved repair and regeneration plan is coherent and completed within scope: repair consumption first; preserve original tests and legacy registrations; propagate refreshed source pins through dependent schemas; regenerate moved-family artifacts in predecessor order; retain separate original and refreshed catalogues; check affected consumers; preserve exact-head CI gate. No new service, authority boundary, wire schema, gameplay rule, or implicit family policy introduced.

Reviewed all 26 changed paths, including six schemas, seven fixture/catalogue files, scheduling metadata, docs and prior review record. Schema changes are hash slots only. Direct recursive comparison of parsed fixtures, including embedded canonical JSON, found no shape/order/membership changes or altered gameplay values. Changes match recorded provenance hashes, IDs and prefixes. Original child and parent exercise fixtures also remain byte-identical.

Generation helper sources call owning oracle functions, retain ordered row indices, assert membership/counts, and assemble complete outputs. Retained four oracle outputs equal all 300 committed refreshed catalogue rows. This verifies retained generation evidence; generation was not rerun by reviewer. Review does not reinterpret historical failure/timeouts as passes.

README, technical design, revision contract and checklist describe consumption policy consistently. Naming overview unchanged appropriately: no names or domain boundaries changed. Release, external archive, Snapshot/Outward and hosted-CI gates remain explicit. No heavy pivot or extra workstream required.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status / consequence |
| --- | --- | --- |
| Original five regressions and two nested snapshot rows pass | Independent focused execution, unchanged original RoundEntry test Git blob | Confirmed, 7/7 |
| Full snapshot class and legacy replay remain valid | Independent snapshot/P01 combined execution and Legacy-method execution | Confirmed, 13 snapshot + 10 Core P01 and 33 Legacy rows |
| Nested errors retain consumer code and owned bytes | Source, new tests, original missing/changed warm/fresh regressions | Confirmed |
| 22 protected files and 27 legacy values unchanged | SHA-256 and base Git byte comparison; parsed registration tuples | Confirmed |
| Nine refreshed registrations match current files | Independent physical SHA-256 comparison | Confirmed |
| Source-pin propagation complete for recorded changes | 25 chronological entries validated as 24 final slots, including intermediate update chain | Confirmed; initial single-step comparison needed chronological interpretation, not an artifact defect |
| Six regenerated fixtures preserve semantics | Independent recursive parsed diff, exact field counts matching ledger, helper source inspection | Confirmed for stored records; hash-only cuts additionally rely on oracle/native parity evidence |
| All 177 RoundEntry / 300 refreshed R03 rows passed | Eight retained XML reports compared against exact receipt selections; no duplicates or missing identities, all Pass | Confirmed retained evidence, not fresh full executions |
| All 300 refreshed catalogue rows generated by owning oracle | Four retained oracle outputs exactly equal committed catalogue by rowKey; worker source | Confirmed retained evidence |
| Child/parent exercise evidence unchanged and checked | Base-byte comparison and retained generation logs with child checks and seven parent groups | Confirmed retained evidence |
| Original weights unchanged; two new rows measured | Full original metadata comparison, canonical module/method/display key hashes, additions receipt | Confirmed |
| Release build and format pass | Retained command/exit receipts, successful build log, empty format log | Confirmed retained evidence, not freshly rerun |
| P01 and boundary checks pass | Independent focused executions | Confirmed: Core 10, Host 46, boundary 81 |
| Strict committed-head inventory follows checkpoint | Coordinator result and exact-head discovery log | Confirmed retained coordinator execution: 3342 ordinary Core rows, 61 long processes, 17 total matrix jobs |

## Verification Performed

Reviewer executed against retained Release binaries after verifying immutable HEAD and source tree:

- `dotnet exec artifacts/bin/Cna.Core.Tests/release/Cna.Core.Tests.dll --filter-method '*ContentIsRecheckedAtConsumptionAfterTwoPassingPreflights' '*WarmReplayOwnsBytesAndCannotBorrowEitherLedgerOrTrustedContext' '*WarmPreparedReplayRechecksContentAfterItsPassingPreflight' '*ConsumptionRevalidationTraversesParentsAndOwnsCurrentBytes'`: exit 0; 7 passed, 0 skipped.
- `dotnet exec artifacts/bin/Cna.Core.Tests/release/Cna.Core.Tests.dll --filter-method '*Legacy*'`: exit 0; 33 passed, 0 skipped.
- `dotnet exec artifacts/bin/Cna.Core.Tests/release/Cna.Core.Tests.dll --filter-class '*CombatDependencySnapshotTests' '*P01*'`: exit 0; 23 passed, 0 skipped (13 snapshot, 10 P01).
- `dotnet exec artifacts/bin/Cna.OrleansHost.Tests/release/Cna.OrleansHost.Tests.dll --filter-class '*P01*'`: exit 0; 46 passed, 0 skipped. External physical archive not configured; no external-gate claim.
- `dotnet exec artifacts/bin/Cna.Core.Tests/release/Cna.Core.Tests.dll --filter-trait 'Boundary=UserSpace'`: exit 0; 81 passed, 0 skipped.
- `git diff --check 3bab87b1 91b313c3`: exit 0.
- Inline read-only Python comparisons: protected bytes, registration hashes, chronological physical pin changes, parsed fixture semantic deltas, exact XML selections, unique oracle catalogue membership, original scheduling metadata, canonical two new timing keys and unchanged child/parent fixtures: passed. Initial exploratory pin assertion assumed every ledger entry was final; follow-up verified chained intermediate entries. Initial timing hash assertion assumed display-only identity; corrected check used repository's canonical module/method/display tuple. Neither required source/evidence changes.

Graph verification used indexed project `Users-dsteele-.codex-worktrees-3d3c-sandtable`, symbol search, bidirectional trace, exact snippet and coverage checks. Runtime paths/new test path have matching metadata with no recorded gaps. RoundEntry test parse gap line 488 read directly. Ignored generation helpers are excluded from graph, so inspected directly in full. Graph includes imprecise inferred external callees; conclusions rely on verified source, not those edges. CCE `context_search`/`session_recall` unavailable in supplied tool inventory.

Full suites, full owning-oracle regeneration and strict discovery were deliberately not duplicated. Retained checks remain distinguished from reviewer executions. Sole reviewer write: this report; source, fixtures, plan and Git refs untouched.

## Open Questions And Residual Risks

Exact-head hosted CI still required before merge; local review and inventory do not certify it. Full solution test gate, all owning-oracle scripts and external physical archive not independently rerun here. Complete 177-row RoundEntry run preceded final refreshed-registration update; unchanged RoundEntry fixture/test/source behavior is supported by final focused regressions and legacy checks, while final R03 parity covers propagated identities. Retained logs/XML are ignored local diagnostics, not durable hosted certificates.

Consumption checks protect bytes actually returned and copied; they do not promise atomic filesystem snapshots across arbitrary concurrent external changes. Nested traversal recognizes repository's direct snapshot delegate convention, consistent with existing revision inheritance. No new concurrency guarantee or public activation inferred.

Unrelated Snapshot/Outward source reconciliation, broader RC/product/certificate gates and merge remain outside review scope.

## Verdict

**Ready** for scoped RoundEntry consumption repair and associated evidence regeneration at `91b313c33252b217ded7bb0738b9fa0d1b4751af`. Implementation and plan satisfy reviewed requirements; no actionable defect remains. This verdict does not authorize merge or certify broader RC readiness. Exact-head hosted CI remains required.

## Recommended Next Actions

Coordinator should retain this report and obtain required exact-head hosted CI before merge, preserving separate RC gates. Review instance 8 of 8 exhausted; no further reviewer instance starts automatically. Any material remediation or scope pivot returns to user for explicit decision.
