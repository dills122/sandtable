# H3 independent review

Review instance: 1 of 3.

## Preliminary blind ledger

Recorded before reading h3-author.md, h3-checks.md, or session recall.

- Scope verified: HEAD fdd2a4fa348db658af55fbd4d347206beef193f3; four frozen primary hashes match, including untracked CombatReactionHistoryReplayTests.cs. Supporting documentation changes match bootstrap.
- Inspected H3 diff, entire router/projection model and tests, retained-history ownership, F1–F6 reader predecessor gates and exact-byte comparisons, inherited Snapshot12 contract, canonical H execution refinement and supporting docs.
- No actionable defect identified. Candidate risks checked: duplicate/forged hints; unconsumed tails; omitted stop or close; wrong sibling tail; actual versus suspended positions; shared-cut uniqueness. Readers bind predecessor, actor/command and exact emitted bytes; selected terminal readers consume full remainder with strict count limits.
- Test design independently compares Apply-built bytes to frozen F-family hashes and H0 fields, checks complete receipt ledger and every selected cut, owned buffers, raw mutations, re-signed actor/effect forgeries, and fork splices. Existing H1/H2 tests retained with explicit legitimate Reaction admission update.
- Remaining verification: independently count fixture identities, reconcile author/check claims, inspect retained focused results; no concurrent .NET processes started.
- Plan boundary sound: H3 composes already implemented F1–F6; H4 literal-root/disabled-admission restore, host publication and later gameplay remain open.

## Findings

No actionable findings. No code or plan correction requested.

Evidence: CampaignCombatHistoryReplay.ReplayReaction admits strict F1 trigger before dispatching F3 or F2; admits strict F2 participant before F4/F5; admits strict F5 second move before F6. F2 remaining lifecycle, F3 direct closure, F4 fallback and F6 completion each receive the entire remaining suffix. Each reader checks predecessor shape/count and re-emits full event bytes before accepting the next state. A changed hint/tag can only select another strict reader; it cannot bypass canonical effect or predecessor verification.

## Plan Review

Ready for bounded H3 scope. Four primary paths add no transport, public activation, storage or domain transition changes. Closed typed arms retain accepted predecessor state for H4. Actual currentPosition and suspended sequencePosition remain separately tested against frozen H0 roots. README, technical design, naming and execution index consistently describe H3 in progress and preserve H4, Task008 parent and HOST-PUB-001 obligations. Ordering follows actual F1/F2 predecessor cuts rather than treating F-family numeric order as a sequential transcript.

No full-root restore or disabled-admission completion inferred. This report is review 1 of 3; lead retains remaining independent rounds and acceptance bookkeeping.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Hints select candidates only; strict readers retain authority | Router and F1–F6 Replay/initialization gates; full canonical comparisons | Confirmed | No dispatch bypass identified |
| One retained stream owns creation/events; returned arrays do not alias authority | CampaignCombatRetainedHistory.Capture/Created/Events/CopyRange; every Reaction cut mutation tests | Confirmed | Existing ownership boundary retained |
| Both owners and all selected seven forks/intermediate cuts | Cases/Trace/EveryReactionForkAndIntermediateCutMatchesStrictReadersAndFrozenRoots; event/state predecessor golden checks | Confirmed | Selected F1–F6 coverage complete |
| 50 vectors, 34 histories, 2 shared H2, 32 new | Independent fixture identity computation and inventory test source/log | Confirmed | No omitted selected F-family root identity |
| Complete receipts, world, positions, window, flow, route and progress retained | CheckH0; exact serialized-state comparison; independent all-root event-hash receipt ledger check | Confirmed | Consistent inherited state available to later H4 |
| Omission/duplicate/reorder/foreign/raw/forged-hint and re-signed effects rejected | Negative tests, serializer-generated forgeries, strict reader comparisons; retained129-pass log | Confirmed | Relevant router failure modes covered |
| Combined frozen inventory contains368vectors/286histories | Independent raw fixture count | Confirmed for frozen inventory | Does not establish literal runtime root codec or restore |
| Full root/disabled-admission, durable publication and later gameplay remain open | Contract, implementation index, docs, internal-only router scope | Confirmed | H3 readiness does not close those gates |

## Verification Performed

Reviewer-executed:

- `git status --short`, `git rev-parse HEAD`, scoped `git diff` and `git diff --stat`: exact bootstrap scope, HEAD fdd2a4fa348db658af55fbd4d347206beef193f3, four primary paths plus listed support docs.
- `shasum -a 256 -c .planning/combat-task008-delivery/h3-source.sha256`: all four OK.
- `git diff --check`: exit0, no output.
- `python3 -B -` read-only fixture check:368roots/286unique histories; F1–F6:50roots/34unique histories/2ordinary overlaps/32new. Family rows: F1=4,F2=10,F3=12,F4=12,F5=4,F6=8. For every368root, stateVersion equals event count+1, receipt count equals event count, receipt event-hash sequence equals exact retained eventHashes.
- Read targeted implementation, tests, H0 contract and plan before author/check packet. Preliminary ledger persisted first. Required CCE recall performed afterward; recall treated solely as contextual testimony. No prior review reports or aggregate review evidence opened.

Retained execution evidence inspected, not rerun:

- `/tmp/h3-focused-final.log`: command recorded in h3-checks.md as `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*HistoryReplayTests' --no-restore '-bl:/tmp/h3-focused-{}.binlog'`;129passed,0failed,0skipped,1m58s066ms.
- `/tmp/h3-inventory.log`: final additional inventory assertions;1passed,0failed,0skipped,2s841ms.
- Author reports prior intermediate test-generator error corrected; final tests inspected include unchanged-mutation exclusion and avoid moving terminal event onto its unchanged terminal position. No production workaround found.

No reviewer .NET process launched because lead cumulative gate runs concurrently. No oracle rerun claimed; no independent runtime rerun claimed. All reviewer shell/Python checks completed; no outstanding process.

## Open Questions And Residual Risks

No blocking question. Readiness depends on selected, already bounded F1–F6 profile; arbitrary later continuations remain unsupported by design. Repeated predecessor replay remains bounded and intentionally favors reuse of verified readers; no campaign-scale performance claim. Snapshot authenticity still depends on trusted retained history supplied by caller. Full suite/format/boundary completion belongs to lead gate and is not claimed by this report.

## Verdict

Ready — bounded H3 implementation and plan supported by source, frozen-vector checks and retained focused runtime evidence.

## Recommended Next Actions

Lead complete cumulative gate and remaining two user-mandated review rounds. Preserve H4 full-root/disabled-admission and HOST-PUB-001 boundaries during acceptance bookkeeping.
