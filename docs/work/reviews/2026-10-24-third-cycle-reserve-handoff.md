# Genuine third-cycle Reserve timeout handoff

Status: focused validation passed; no production defect exposed. Base
`4bcd9f11990813fb0715bedea2af14a258b63473`; branch
`codex/rc-overnight-plan-20261023`; [PR329](https://github.com/dills122/sandtable/pull/329).
Ruflo task `task-1792888650056-h3c4p4`.

## Required authority witness

Use established genuine triple-contact setup and actual accepted history. First
Reserve timeout converts unresolved I to II; genuine third-cycle timeout retains
II. V35 forbids converted IDs after ordinal 1, so a nonempty converted list at 3
would be invalid evidence, not stronger coverage. Assert exact unit sets,
original timing, accepted owner choice preservation, no new debit/roll/result,
verified replay, idempotence and atomic next Cycle entry.

No synthetic ordinal edits, altered native snapshots, fabricated eligibility or
P01 ancestry. Minimal optional test-helper parameters may designate actual setup
reserves while preserving existing fixture defaults. No runtime change without
a demonstrated failure.

## Flow under test

| Stage | Actual accepted history / expected effect |
| --- | --- |
| Initial setup | Established triple-contact seed 12345; three real units designated Reserve I through setup decisions |
| Cycle 1 | Paid initial assault; owner converts `axis-infantry` to II, releases `axis-infantry-second`; timeout converts unresolved `axis-infantry-third` to II |
| Cycle 2 | Accept Repeat/Movement, actual west assault and both Force seals; owner retains `axis-infantry` II then releases `axis-infantry-third`; Reserve completes normally |
| Cycle 3 | Accept second Repeat/Movement and third Selection Finish; saved-copy reopen with Reserve pending; exact deadline retains `axis-infantry` II, with no converted IDs |
| Completion | Duplicate expiry adds no event; final Cycle offers Finish and replay verifies |

System timeout events occur at ordinals1 and3; four owner Reserve choices remain
distinct from System records. System conversion at1 affects third unit; retention
at3 affects first unit, which owner had converted at1.

Two variants cover normal expiry and injected Cycle-row insertion failure. Fault
variant requires unchanged campaign/accepted-step/Reserve/Cycle rows after failure,
then successful expiry after removing trigger. Exactly two paid assault results
remain; ordinal 3 is not a claim of three paid assaults. Retained World hash must remain unchanged; completed third Reserve checkpoint
must mint no next-movement allowance. RNG evidence is
unchanged paid-result cursor plus verified replay, not a newly exported terminal
RNG object.

## Validation

All 18 focused tests passed. Final Host test-project build has zero warnings/errors;
full solution format passed. No production source changed. Exact commands and
wall times below come from exit receipts under `artifacts/rc-third-cycle-reserve/`
(local ignored artifacts, not Git-tracked). The commit containing this handoff
carries validated source; `source-manifest.json` records test-source SHA256 hashes.

| Check | Command | Result | Wall time | Receipt |
| --- | --- | --- | --- | --- |
| New third-cycle expiry/rollback | `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '*ThirdCycleReserveTimeout*'` | 2/2, exit0 | 36.237s | `focused-receipt.json` |
| Existing Reserve timeout | `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '*.ReserveTimeout*'` | 12/12, exit0 | 32.314s | `reserve-neighbor-receipt.json` |
| Existing genuine third-contact | `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '*SecondAssaultCanRepeatIntoThirdContactCombatAfterColdRestart*'` | 4/4, exit0 | 65.779s | `third-contact-neighbor-receipt.json` |
| Host test-project build | `dotnet build tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-restore` | exit0; 0 warnings/errors | 3.711s | `build-receipt.json` |
| Solution format | `dotnet format Sandtable.slnx --verify-no-changes --no-restore` | exit0 | 30.943s | `format-receipt.json` |

Retained failed attempts:

- Initial build: CA1861 constant-array analyzer errors; changed test constants to
  static readonly fields. `build-initial-failure.log` and receipt retained.
- Initial focused2/2 and diagnostic: fixture queried later Reserve unit before
  resolving earlier element-ID choice. Checkpoint26 was valid Reserve; corrected
  ordered owner decisions. `focused-first-failure` and `diagnostic` logs/receipts.
- Ordered fixture reached actual ordinal3 but tried to read `completed` from
  compact V32 replay event. Assertions now use persisted Reserve snapshot for
  completed state/no minted movement. `focused-ordered-fixture-failure` log/receipt.

No runtime defect found, no source rule changed, no seed search. Final selected
runs all passed. No further source edits after final green build/tests/format.
Not run: full Core/Host/Runner, boundary/contracts, Python oracles or frontend
checks. Prior passes remain historical; release-head partitioned CI is required.
No active local workers or test processes remain after validation.

## Review

Coordinator checked actual paid-result/repeat counts, ordered owner-versus-System
choices, exact retained unit, unchanged World/owner records, deadline boundary,
rollback equality and final Finish-only control. No material source finding;
no production source changed. Optional fixture parameter defaults preserve prior
call behavior. Graph coverage checked changed test paths; source serialization
checked where compact replay differs from retained snapshot.

## Open gates

Coordinator review only; prior independent packet allowance exhausted. Exact-head
CI and independent release review remain required before merge. No merge.
Repeated captured-lot Custody/required Retreat, exact entry clock-fault tests,
fresh-process third-cycle Reserve and other remaining process cuts, abrupt crash
recovery, original source-pin failures,
dependencies, public certificates and playable six-turn RC gates remain open.
Original ancestry inventory remains 44 provisional positive/256 absent/0 admitted.

## Next bounded task

Genuine repeated captured-lot Custody expiry or required Retreat expiry, with
actual accepted history and exact entry clock-fault evidence. Existing fixed-seed
second-assault proof does not produce a captive lot. Keep these distinct from
fresh-process Reserve and crash-atomicity gates. PR329 remains draft pending
exact-head CI and independent release review; no merge performed.
