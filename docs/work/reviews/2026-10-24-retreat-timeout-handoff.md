# Retreat timeout and PR handoff

Branch `codex/rc-overnight-plan-20261023`; parent `22c03209`.
Ruflo task `task-1792884853053-r9n6av`, namespace `sandtable-rc-20261023`.
Implementation commit is the commit introducing this report (`git log -1 --
docs/specs/native-retreat-timeout-v1.md`).

## Delivered behavior

Required post-assault Retreat now starts an immutable persisted round at accepted
result entry. Native V37/accepted-step31 records explicit System `refuse-retreat`,
with existing source refusal losses applied during settlement. No defender move,
owner action, extra RNG draw or duplicate ammunition debit is fabricated.
Schema13 adds a sixth independent bounded background lane. Restart/query does not
renew deadlines. Late invalid proposals leave authority unchanged. Owner receipts
remain idempotent. Fallback, round closure and settlement commit atomically;
missing/altered rows reject cold verification. Schema5–12 retain historical
verification and read-only recovery.

[Audit and plan](../plans/2026-10-24-retreat-selection-deadline-audit.md) records
selected gap and deferred Selection paths. [RC checklist](../plans/2026-10-24-rc-checklist.md)
sets explicit remaining pass criteria.

## Validation

Retained local receipts/logs: `artifacts/rc-retreat-timeout/` and
`artifacts/rc-retreat-host/`. Commands, exits and durations are in JSON receipts.

- Release solution build: exit0, 5.961s.
- Format verification: exit0, 35.182s.
- Core timeout/Custody:18/18, exit0,5.921s; includes9 new Retreat rows.
- Existing Combat selection/retreat consumer:6/6,exit0,7.061s.
- Host focused Retreat:11/11,exit0,41.216s.
- Boundary:81/81,exit0,9.463s.
- Game/Intelligence contracts:8/8+10/10,exit0,2.930s combined.
- CI Python helper unit tests:220/220,exit0,3.667s.
- Runner:470/470,exit0,131.443s.
- Whole Host:198/198,exit0,307.872s. Command: `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore`.

Exact focused Core commands use `dotnet test --project
tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build
--no-restore --filter-class` with `Cna.Core.Tests.Campaigns.CampaignNativeRetreatTimeoutTests`,
`Cna.Core.Tests.Campaigns.CampaignNativeCustodyTimeoutTests`, and separately
`Cna.Core.Tests.Observations.CampaignContactCombatSelectionCutTests`.
Initial command mistakenly supplied the consumer's namespace as Campaigns;
18 passed rows did not include it. Corrected separate invocation passed6.
Consumer suites use `python3 .github/scripts/local_tests.py <suite> --no-build`.
Python helpers use `python3 -m unittest discover -s .github/scripts -p '*_test.py'`.

Initial Core seed5 fixture failed its required-retreat precondition; established
seed6 passed. Initial Host run9/11 passed: rollback trigger named the wrong event,
and real second assault had no required retreat. Corrected fixture expectations;
no product behavior changed to silence failures. Original Host failed log and
receipt retained under `artifacts/rc-retreat-host/focused-first-failure*`.
No dynamic preimplementation RED run is claimed.

## Review and limits

Coordinator reviewed changed runtime/adapter/replay/transaction source and test
coverage. Graph freshness was changed/untracked and Exercises partial; direct
source verification used. No new independent review instance: earlier allowance
3/3 exhausted. Independent release review remains required.

Synthetic repeated native runtime fixtures prove V37 dispatch at ordinals2/3.
Real Host history proves initial timeout then second assault/cold replay; that
second assault has no required retreat. Genuine repeated Host required-retreat
expiry remains unproven. Exact Host clock failure at Retreat entry is not injected;
open-round clock faults and Core unavailable-entry evidence are covered.

Full Core, current original Python source oracles and Maproom checks were not run
for this packet. Historical Breakdown/cycle source-pin failures remain unresolved.
Original P01 inventory44 provisional positive/256 absent/0 admitted is unchanged.
Selection deadlines, integrated proof gaps, dependency remediation, public
certificates and playable product acceptance remain open. No public RC claim.

## PR scope

Accumulated branch preserves separate commits for creation/recovery, local test
suites, RBA, Force entry/background, cycle-control, Reserve Release, Custody and
this Retreat packet. PR will be draft pending exact-head CI and review. No merge
or older PR closure authorized. Local focused evidence is not a full-suite pass.

Actual Release discovery:3640 Core rows =3315 ordinary+325 frozen R02;
all nine new rows reconciled with scheduling manifest. Dry-run exit0,49.221s;
retained inventory `artifacts/local-tests/fast-01-zy47l2iw/discovery/r02-inventory.json`.
No fast-01 tests were executed by that dry-run.

All local test processes completed; worker ownership released. No full Core
process remains active. No source changes after final common build.
