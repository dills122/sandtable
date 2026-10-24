# Independent RC packet review

Review instance: 1 of 3. Reviewer mode; source read-only.

## Findings

No actionable implementation defect found in bounded packet. Reviewed working-tree
delta, including untracked creation tests, against base/HEAD
`16d76448a008ea4f16299402b8ef44e8c96a4843` on
`codex/rc-overnight-plan-20261023`. No implementation commit exists. No source
fixes, staging, builds, or duplicate test runs performed by reviewer.

Bootstrap, contracts, plan, diff and tests inspected before author explanation.
Preliminary ledger sent to coordinator first: no runtime finding; direct RNG
cursor equality unproved; final third-cycle and integrated gate pending then.
Author explanation subsequently confirmed that RNG limitation. Coordinator
accepted narrowing baseline/plan wording to match actual proof.

Evidence supporting source acceptance:

- `src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.cs:182` begins an
  immediate transaction before lookup/admission; lines 192–197 fully load retained
  authority, compare request record and canonical timing, then return current
  session. Disposal releases read-only retry transaction. Fresh admission/insert
  remains atomic, with no schema or transport change.
- `src/Cna.Core/Campaigns/CampaignAuthority.cs:3` defines a sealed record with all
  nine creation fields, so equality compares immutable identity values, rather
  than object identity. `CampaignDurableAuthorityJournal.cs:316` verifies retained
  sequence, hashes, Core import and force rounds before returning Loaded.
- `src/Cna.OrleansHost/Api/CampaignDurableOutwardApi.cs:46` authenticates operator
  before body processing and campaign lookup; retained retry keeps existing 201
  acknowledgement. Generic conflict mapping remains at line 273.
- `tests/Cna.OrleansHost.Tests/CampaignDurableCreationRetryTests.cs:14` exercises
  advanced retained head and unchanged full campaign/accepted-step evidence;
  remaining cases cover all changed non-key identity fields, exact/absent timing,
  corrupt retained snapshot, and independent identical/conflicting creators.
- `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs:215`
  injects abort specifically at automatic Barrage insertion, verifies unchanged
  head/count/checkpoint and absent selection receipt, then cold retries once and
  confirms duplicate receipt and exactly two committed transitions.
- Same test file, lines 600–777, retains positive/finish cases and adds third-cycle
  zero-/one-seal timeout. Tests reject late seals, retain unchanged World hash
  including ammunition, add no assault-result event, cold reconstruct and finish.
  Cursor-presence assertion at line 758 is not cursor-byte equality.

## Plan Review

Bounded plan is justified: existing generic Core continuation needed proof, not
new speculative runtime. Creation retry addresses concrete durable publication
gap. Canonical journal contract now matches exact identity/timing retry behavior
and v7-only creation restriction. README, roadmap and design distinguish bounded
local implementation from public certification and playable scenario delivery.

Original P01, HOST-PUB-001, public 020–025 and six-turn product obligations remain
open. Canonical authority-envelope publication matrix requires substantially more
than current tests, including ambiguous commit/read failure, process crash,
disabled-admission recovery and measured retention. Packet correctly claims none
of those closures. All300 ancestry rerun does not close all-row cut/recovery proof.

Gate 5 explicitly requires integrated `just check`; coordinator has not completed
that changed-tree gate at review cutoff. Exact-head CI also remains required before
merge. Source approval must not be reported as completed delivery acceptance.

## Author-Claim Reconciliation

| Author claim | Inspected evidence | Status / consequence |
| --- | --- | --- |
| Exact retry returns verified current head without writes | Journal CreateCampaign/Load, request record, advanced-head tests | Confirmed |
| Changed identity/timing conflicts; null timing differs | Record equality, byte comparison, identity/timing tests | Confirmed |
| Independent creators serialize | Immediate SQLite transaction and two-connection barrier test | Confirmed for tested provider scenario; not process-crash proof |
| Selection/Barrage rollback is atomic | Insert trigger and unchanged durable evidence assertions | Confirmed for injected pre-commit failure |
| Third-cycle cancellation avoids new result/World debit and cold replays | Four-case theory, result count, World hash, retained-cursor marker, Finish | Confirmed within bounded history; direct RNG-byte equality unproved and disclosed |
| Baseline 4,195 plus 81 tests passed | Retained baseline log and baseline evidence | Confirmed baseline only |
| Creation 14 and outward 1 pass | Final retained focused logs | Confirmed |
| All300 ancestry passes with zero admission | Command receipt and all300 result log | Confirmed execution receipt; reviewer did not rerun Python or establish certificate completeness |
| Integrated final gate remains due | Author explanation and coordinator status | Confirmed; acceptance still pending |

## Verification Performed

Reviewer ran `git status --short`, `git branch --show-current`, `git rev-parse HEAD`,
`git diff --stat`, targeted `git diff`, and `git diff --check`; last command exited
0. Read canonical journal, authority-envelope, contact-projection and original P01
requirements, selected plan, baseline/certificate/dependency reports and relevant
source context. No further test suite launched while shared build lease active.

Codebase-memory Tier 2: index generation `2026-10-24T03:15:54Z`, recorded
`2026-10-24T03:16:18Z`. Searched CreateCampaign, traced both directions and read
exact snippet; direct source verified transaction/record/API semantics. Coverage
checks for journal, all three changed test files, creation record and outward API
reported `metadata_match` / `no_recorded_issue`; best-effort signal, not proof of
complete graph edges. CCE context_search/session_recall unavailable in tool catalog;
no author memory read before preliminary pass.

Retained execution evidence inspected, not rerun by reviewer:

| Log under `/tmp/` | Result |
| --- | --- |
| `sandtable-rc-check-20261023-unrestricted.log` | Baseline 4,195/4,195 solution tests, plus baseline evidence 81/81 boundary tests |
| `sandtable-rc-creation-final.log` | 14/14, zero failed/skipped, 2.176s |
| `sandtable-rc-creation-outward-final.log` | 1/1, zero failed/skipped, 3.412s |
| `sandtable-rc-barrage-rollback.log` | 1/1, zero failed/skipped, 6.200s |
| `sandtable-rc-third-cycle-final.log` | 4/4, zero failed/skipped, 1m26.724s |
| `sandtable-rc-proof-build-final.log` | Build succeeded, zero warnings/errors, 5.13s |
| `sandtable-rc-p01-all300.json` and corresponding `.log` | Original verifier exit 0, 101.651s, 300 roots, 44 positive/256 absent, zero admitted |

Focused logs provide counts/results, not echoed command lines. Coordinator
confirmed final runs and no production source edits after 03:16Z. Baseline commands
and ancestry command are recorded in their corresponding evidence documents.

## Open Questions And Residual Risks

Integrated changed-tree gate still pending. Full transcript verification holds a
write-reserving transaction and grows with history; author discloses this tradeoff.
Cold reopen is not fresh-process crash recovery. Direct RNG-byte equality is not
asserted. Original source-pin failures remain explicit; frontend dependency triage
is an inventory, not remediation or exploit validation. These limits do not reveal
a defect in scoped retry change or authorize broader certification.

## Verdict

**Not ready** for completed packet acceptance solely because required integrated
changed-tree gate remains pending. Source review passes with no blocking code
finding. This verdict is not a request for redesign or another independent review;
coordinator can close pending gate with actual successful evidence.

## Recommended Next Actions

Finish integrated `just check`, preserve exact commands/results in handoff, and
retain exact-head CI requirement before merge. Narrow planned RNG wording to
measured assertions, as coordinator accepted. Keep all certificate, crash-recovery,
dependency and playable-RC gaps explicit. No further review instance warranted
unless subsequent fixes materially change reviewed behavior.

## Coordinator acceptance gate closure — October 24, 2026

Source verdict above remains unchanged. Required integrated gate is now closed:
coordinator polled original execution session `32680` at 19:33 UTC and received
exit code 0. `just check` log retained at
`artifacts/rc-overnight/integrated-check-recovered.log`.
Restore and format verification passed; solution build had zero warnings/errors
(5.36s). Boundary checks passed 81/81, zero failed/skipped (7.727s).
Full solution passed 4,212/4,212, zero failed/skipped (1h 01m 16.743s);
Core assembly passed in 1h 01m 16.427s. Host, Runner and both contract assemblies
also passed. This evidence closes the sole pending acceptance condition identified
by review instance 1, so this bounded packet is accepted. Earlier session `26801`
has no available completion evidence and is not claimed as passing.

No public admission, HOST-PUB closure, source-pin repair or RC claim follows.
Exact-head CI remains required before merge. The separately reviewed Force deadline
fix has not yet been applied to this accepted tree; it requires its own validation.
