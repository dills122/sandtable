# Selection timeout handoff

Branch `codex/rc-overnight-plan-20261023`, base `38c17170`, PR329.
Ruflo task `task-1792886193779-aw4muf`, namespace `sandtable-rc-20261023`.
Implementation commit is the commit introducing this report (`git log -1 --
docs/specs/native-selection-timeout-v1.md`).

## Delivered scope

Initial and repeated Combat Selection get immutable persisted entry deadlines.
Native V38/accepted-step32 records System `no-selection`, with no serialized
owner action, actor element, target, commitment or invented owner Finish.
Native closure includes Position Determination progression; V13/V32 then completes
remaining no-work steps into same-slot Reserve Release. This combined native
transition does not assert equivalence to isolated C3a or original P01 proof.
World and actual RNG cursor remain unchanged. Existing owner bytes remain intact.

Schema14 adds a seventh independently bounded background lane. Owner openings,
submissions and generic Append share deadline enforcement. Rejected proposals
validate before clock mutation. A missing clock/configuration at entry triggers
explicit unavailable fallback with absent clock evidence; no deadline is invented.
Round closure, no-attack continuation and Reserve entry are atomic. Historical
schema5–13 journals remain read-only with their original verification thresholds.
Append's returned receipt describes its accepted input; ReadVerified returns
current durable authority including automatic successor events.

Initial scope uses shared Core eligibility for entry initialization and replay
verification. Unsupported native no-attack profiles are not admitted. Repeated
proof must follow a real first assault and accepted repeat/Breakdown, because
initial no-selection has no result cursor and offers only Finish under current
cycle rules. No eligibility expansion is part of this packet.

## Verification

Final local command/exit/count receipts are retained under
`artifacts/rc-selection-timeout/` and `artifacts/rc-selection-host/`.
No source changes after the final common Release build. No full Core iteration.

| Check | Result | Receipt elapsed |
| --- | --- | ---: |
| Release solution build, no restore | exit0,0 warnings/errors | 5.646s |
| Format verify, no restore | exit0 | 41.351s |
| Core Selection/Retreat/existing Combat selection consumer |24/24,exit0 |12.016s |
| Host focused Selection |15/15,exit0 |37.019s |
| Boundary |81/81,exit0 |10.195s |
| Game + Intelligence contracts |8/8+10/10,exit0 |3.249s combined |

Core command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj
--configuration Release --no-build --no-restore --filter-class
Cna.Core.Tests.Campaigns.CampaignNativeRetreatTimeoutTests
Cna.Core.Tests.Observations.CampaignContactCombatSelectionCutTests
Cna.Core.Tests.Campaigns.CampaignNativeSelectionTimeoutTests
--report-xunit-xml --results-directory artifacts/rc-selection-timeout/core-results`.
Host focused command: `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj
-c Release --no-build --no-restore -- --filter-method '*SelectionTimeout*'`.
Consumer commands: `python3 .github/scripts/local_tests.py <suite> --no-build`
for `runner`, `boundary`, `contracts`. Exact arrays retained in receipts.
Runner470/470 passed,exit0,138.531s. Whole Host213/213 passed,exit0,354.513s;
actual discovery details follow.

Relevant independent Python contract oracles passed:

- `python3 docs/specs/verify-combat-selection-steps-v1.py`:exit0,3.566s;
  5 traces,41 cuts,246 event mutations,164 raw rejections; isolated only.
- `python3 docs/specs/verify-combat-actual-moved-repeat-position-v1.py`:exit0,2.451s.
- `python3 docs/specs/verify-combat-inherited-reserve-cycle-v1.py`:exit0,56.864s;
  2 traces/20 events,22 cuts,20 retries,1103 mutations,46 raw rejections,
  196 boundaries,13 source pins.

Original Breakdown/cycle-sequence source-pin failures from earlier packets remain
unresolved; passing these three other oracles does not replace them. No source
pins or public support rows changed.

## Review and remaining gates

Coordinator source review, graph coverage checks and focused validation; no new
independent reviewer instance. Existing review allowance3/3 exhausted; independent
release review remains open. Parent checked metadata and read changed/partial
source directly; graph completeness is not claimed.

All seven window implementations now exist in bounded scope. Integrated genuine
campaign proofs still need third-cycle Reserve expiry, repeated captured-lot
Custody and required-Retreat expiry, and exact Custody/Retreat-entry clock faults.
Public original-root certificates, dependency remediation and playable six-turn
product gates remain open. Original inventory44 provisional/256 absent/0 admitted
is unchanged. Exact-head CI and review required before merge; PR remains draft.

See [implementation plan](../plans/2026-10-24-selection-deadline-plan.md) and
[RC checklist](../plans/2026-10-24-rc-checklist.md) for explicit pass criteria.

During test development, seed5 did not provide repeat eligibility and converting
Reserve I through timeout also prevented the intended repeat witness. Replaced
those invalid fixtures with established seed6/double-contact accepted history
and an explicit owner Reserve release. No seed search, fabricated ancestry or
runtime rule relaxation. Core repeated assertion uses actual post-assault RNG
rather than unchanged initial snapshot cursor.

First Host focused run passed13/15. Failures were fixture assumptions: seat grant
was attempted with the deliberately unavailable clock, and generic action query
at native Selection correctly returned no actions. Preserve failed log/receipt;
verify generic native-proposal rejection and explicit timer progression rather
than claiming an unavailable generic legal action was accepted.

Actual Release discovery via `python3 .github/scripts/local_tests.py fast-01
--dry-run --no-build`:exit0,50.299s;3649 total Core rows=3324 ordinary+325
frozen R02. Nine new measured Selection rows reconciled; prior weights unchanged.
Inventory: `artifacts/local-tests/fast-01-pssybjql/discovery/r02-inventory.json`.
Dry-run did not execute fast-01. Full Core and Maproom were not run locally;
previous helper220 pass belongs to the prior packet, not a new claim here.

Whole Host command: `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore`.
All local test processes completed; worker ownership released. No full Core
process started. Commit/push updates PR329; draft status and exact-head CI/review
requirements remain unchanged.
