# B2 independent review

Review instance: 1 of 3.

## Preliminary ledger (before author explanation)

- Verified branch codex/combat-task008-stage-entry and HEAD a0babdb157c28d048601e3b3a1cceaa28cfe849c; working-tree paths match bootstrap, including four untracked source/test files.
- Independently inspected frozen stage-entry spec/schema, predecessor Weather/opening/authority/sequence contracts, execution and canonical plan rows, all three implementation files, and focused test source.
- Replay derives state6 from complete Weather history; four source-backed edges preserve authority and reach unmaterialized Reserve. Apply validates actor/tag before retry. ReadState compares against independent full replay. No confirmed correctness findings.
- Investigating primitive ID grammar alignment in input codec; acceptance at Apply remains strictly tied to reconstructed expected command.
- Tests cover all twelve traces/sixty cuts, actor mismatch, altered predecessors, changed retry, canonical raw rejection, semantic forgeries, immutable fields and buffer isolation. Execution evidence still pending.
- B1→C→B2→D ordering matches canonical dependencies; generic Snapshot12/publication/public activation correctly excluded.
- CCE used first; broad search exposed only introductory b2-evidence excerpt, without implementation rationale or prior review reports. Direct known-path reads used because retrieval was noisy. Author packet remains unread at this checkpoint.

## Findings

No actionable findings. Preliminary ID-bound concern cleared: `ContentContractGuards.RequireSourceAtom` enforces exactly 1–128 safe ASCII characters, consistent with inherited authority grammar.

## Plan Review

Task008 B2 acceptance criteria align with implementation: full creation/opening/Weather replay precedes four explicit-none gates; state6→10 and receipts5→9 preserve all four Weather outcomes, World, order, holder and advanced RNG. Canonical plan dependencies B1→C→B2→D are causally necessary. Parent B remains open until B2 acceptance, while Task008/H and publication stay open. Reserve designation/first opening, positive obligations, generic Snapshot12 and public activation exclusions are consistent with frozen B2 scope. Five primary files plus synchronized project-map/design/roadmap edits introduce no unrelated runtime changes. No plan pivot needed.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Full predecessor replay, never cached authority | `CampaignCombatStageEntry.Replay`, `ReadState`; predecessor/cross-order and cache-forgery tests | Confirmed | Authority preserved |
| Four exact source-backed transitions | `Edges`, `Position`, canonical schema and frozen event/source/position assertions | Confirmed | Correct chronology and terminal boundary |
| Twelve traces, sixty cuts, 228 fingerprints | `Cases`, `FrozenTracesMatchEveryCutAndPreserveWeatherAuthority`, frozen fixture; focused test run | Confirmed | Both order choices and six retained seeds covered |
| System authorized before retry; current state returned | `Apply`, `Authorize`, every-cut retry/actor tests | Confirmed | Stale exact retry does not roll state backward |
| Fleet Commonwealth and terminal null-side handoff | Exact position fixture checks plus final role/side/order assertions | Confirmed | ActLast handoff retains proper first side |
| No World/RNG mutation or buffer authority | `SerializeState`, immutable-field assertions, buffer-isolation test | Confirmed | Stage entry draws no RNG and changes no resources |
| Build and format passed | `/tmp/b2-build.log` zero warnings/errors, author retained format result | Build confirmed; format outcome author-reported | Reviewer did not rebuild or reformat |
| Full suite running | `/tmp/b2-suite.log` shows Core and Contracts passed, Runner pending at inspection | Unverified completion | Lead must retain final integration result |

## Verification Performed

- `git status --short`, branch/HEAD inspection, `git diff`, `git diff --check`: expected scope and no whitespace errors.
- `shasum -a 256 src/Cna.Core/Campaigns/CampaignCombatStageEntry*.cs tests/Cna.Core.Tests/Campaigns/CombatStageEntryTests.cs`: all four hashes match `b2-source.sha256`.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatStageEntryTests' '-bl:/tmp/b2-review1-{}.binlog'`: initial sandbox attempt exited134 because MTP could not bind local IPC pipe. Same command with authorized sandbox escalation passed17, failed0, skipped0 in5.354s. Binlog `/tmp/b2-review1-20260919-222316--56042--sUCIDi-dotnet-test.binlog` exists. No build executed by reviewer.
- Frozen stage-entry oracle result recorded below after completion.

## Open Questions And Residual Risks

No unresolved B2 correctness questions. Focused checks use lead-built assembly; unchanged source hashes and inspected successful build support applicability. Full integration completion remains lead-owned. This bounded internal adapter does not establish durable published-head trust, general campaign capacity, hosted ingress authentication or later-state Snapshot12 support; those are explicit downstream gates.

## Verdict

**Ready** for B2 scope, subject to normal lead integration gate and remaining requested sequential review rounds. No blocker or heavy pivot identified.

## Recommended Next Actions

Retain final full-suite result, complete requested rounds2–3, then accept B2/parent B and proceed to D using complete state10 history. No source correction requested.

### Completed oracle evidence

`PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-stage-entry-v1.py`: exit0; PASS12 creation-rooted traces,60 replay/state cuts,11,052 leaf mutations,1,299 raw rejections,3,552 boundary/retry checks. Normal verification only; fixtures unchanged. Verdict remains **Ready**.
