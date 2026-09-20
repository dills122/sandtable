# Task013 independent review

Review instance: 1 of 3.

## Preliminary blind ledger

Persisted before reading task013-author.md or task013-checks.md. CCE unused; no prior reviews, aggregate evidence, dev notes, proposals, or execution history consulted. Source/Git read-only; only this report written.

Target: branch codex/combat-task008-reaction-lifecycle; base and initial HEAD f2c004635568a1fb51e86171a310abf3a7ca3d0a. Explicit working-tree boundary: all six task013-source.sha256 entries match, plus current README.md, docs/roadmap/pre-alpha-roadmap.md and tech-design.md. Five material paths and mechanical fixture link match requested scope.

No actionable defect identified in blind pass. Examined complete resolution engine/codec, full new test class, narrow shared diffs, Task013/checkpoint F and Task014 boundary, Result2 spec/schema and targeted oracle resolution/context logic. Preliminary observations:

- Replay rebuilds paid Task012 through separately supplied Boundary/C3a/Round inputs/events. Result event.input cannot substitute for trusted input; complete expected event bytes must match.
- Eight role-ordered accepted dice plus conditional ninth retain rejection bytes. UInt64 overflow throws before immutable candidate publication. Both roles use authenticated paid pre-loss World.
- Tests compare 32 literal first resolve events and 64 initial/resolved state HASH cuts; no claim of literal intermediate JSON. Corpus has 16 eight-draw,16 nine-draw,12 rejection traces and no block crossings; separate rebuilt cursor30 history checks crossing and exact bytes/dice.
- Pending singleton settlement preserves paid facts and null consequence receipts. Factory retains legacy public constructor checks; named Result grammar enables Route only in its profile and continues forbidding legacy broken-vehicle values.
- Discard/retry checks prove Core candidate preservation and original-byte recovery, not host transactions or durable lost-reply recovery.
- Raw grammar/bounds precede trusted history; replay supplies semantic/provenance checks. Source ownership uses copies/read-only wrappers. Later consequence events parse but are rejected as unsupported by Task013 transition.

Remaining checks: reconcile author/checks packets after this ledger, execute proportionate focused/shared tests only after /tmp/task013-after-boundary-20260920 exists, reverify frozen pins. Full-table arithmetic coverage belongs to existing Rules suite; new Result tests cover retained selected branches rather than every table cell. Root owns full gates and acceptance.

Initial checks: shasum -a 256 -c .planning/combat-task008-delivery/task013-source.sha256 passed six files; git diff --check passed. No .NET process started; clearance marker absent at last check.

## Findings

No actionable findings from code, tests, canonical plan, or claim reconciliation. No P0–P3 issue retained.

## Code and plan assessment

Task013 satisfies its bounded initial-to-resolved Result2 responsibility. `CampaignCombatResolution.Replay` authenticates Task012 through the existing sealed-round replay, checks complete paid status, and recomputes each result event from separately trusted inputs. `Transition` validates bindings and primitive shapes before retry handling, preserves original bytes for duplicate commands, and never accepts later settlement transitions. `Resolve` draws into local state and emits one result/cursor candidate. Overflow cannot publish a partial result. `CampaignCombatResolutionCodec.ReadState` verifies whole canonical state bytes against causal replay.

Task013/checkpoint F relies on Task005's existing selected-table adjudicator: `CombatSelectedRulesTests` independently checks all36 morale coordinates,360 loss cells,1296 morale pairs and reachable assault/capture paths. The new ten-test class checks integration across retained32 branches and both roles/seal orders. This division is proportionate; it does not represent32 traces as exhaustive table coverage.

`CampaignCombatSettlementState.CreateResolvedResultV2` changes only occurrence identity construction; opposing-unit, paid-state and scope checks still run. Legacy constructor preserves synthetic identity convention. Pending World equality is checked before serialization of owned paid bytes, so altered typed state cannot disappear through serialization. Result grammar's explicit profile forwards UInt64, route semantics and futureTurn bounds without admitting LegacyBrokenVehicleLot or changing C3a's Route rejection.

Six physical source/test files reflect five material components plus fixture link; no unrelated production expansion. README/roadmap retain implementation-under-verification status; tech-design explicitly limits proof to Core candidate atomicity. Task014 and later remain responsible for consequences, closure and release. HOST-PUB-001 remains separate; no host durability inferred from discard/lost-reply simulations.

## Author-claim reconciliation

Packets read only after preliminary ledger was persisted.

| Claim | Inspected evidence | Status and consequence |
| --- | --- | --- |
| Independent paid Round2 authority; no caller committed cache | Replay, CombatSealsTests.Case and Result tests' predecessor/input mutations | Confirmed by source; whole event comparison closes self-authentication path. |
| 32 literal events /64 HASH cuts | First test's loop plus fixture inspection | Confirmed scope; hashes are not literal intermediate JSON. |
| Eight/nine ordered draws and retained rejected bytes | Resolve, literal comparison, fixture/Python audit | Confirmed:16 eight,16 nine,12 rejection traces. |
| Supplemental authenticated block crossing and overflow | RebuildCursor rebuilds C3a and Round histories; cursor30 and UInt64 tests | Confirmed source; independent SHA256 check reproduces exact cursor30 bytes/dice. Literal32 corpus contains zero crossings. |
| Paid World and singleton pending settlement | PendingWorld, WorldValue, factory and World mutation tests | Confirmed; no extra debit or consequence receipt. |
| Narrow shared compatibility/strict syntax/ownership | Named Result profile, legacy factory tests, raw sentries, immutable copies | Confirmed source and focused test assertions. |
| Atomicity and retry | Immutable candidate construction; discard/replay and duplicate-byte assertions | Confirmed only for dormant Core seam. Host transaction, persistence and actual process recovery unproved and unclaimed. |
| Author focused10/shared100, root build/oracle/format | task013-checks.md testimony | Reported, not independently executed by reviewer at this point; not promoted into reviewer observations. |
| Full Result2 oracle establishes Task013 runtime completeness | Author explicitly disclaims this inference | Correct limitation: oracle includes later Task014–016. |

## Verification performed so far

- `shasum -a 256 -c .planning/combat-task008-delivery/task013-source.sha256`: six matches before and after root commit.
- `git diff --check` and `git diff f2c0046 --check`: pass.
- Inline `python3 -B` descriptor/fixture audit: all18 Result2 schema objects match codec declarations;32 literal first events and64 selected hash entries present;16/16 draw counts,12 rejection traces,zero literal crossings; every consumed-byte length, accepted/rejected range, die mapping and consecutive cursor checked.
- Independent inline `python3 -B` SHA256 domain/seed/block computation: cursor30 bytes `4dd79fabb6070dc7`, dice `[6,6,4,4,3,2,2,2]`, matching supplemental test.
- Root committed identical source as `1c514640b38e43abf72f1e9725ec8d7e7e8a8e8c`; all six frozen pins still match. Unrelated root-owned execution/checks metadata changes excluded from code assessment; execution content not read.

## Open questions and residual risks

No code/plan blocker found. Independent .NET execution remains pending root clearance marker. Source/fixture checks are not substitutes for executed tests. Full gates, CI, acceptance and publication belong to root. No broad history/reviews search, CCE, agents, fixes or commits used. No reviewer background process currently exists.

## Final verification limits

Final pin check: all six match; HEAD remains `1c514640b38e43abf72f1e9725ec8d7e7e8a8e8c`; base-relative whitespace check passes. Clearance marker remained absent after bounded waits (each at most45 seconds). No .NET command was started, so reviewer claims no independently executed C# tests, build, format or Boundary suite. Root's ongoing full gates were neither interrupted nor duplicated. All reviewer shell/Python commands exited; no reviewer process left running. Test-runner/binlog skills were read in preparation, but no runner invocation or binlog was produced.

## Verdict

**Ready** for the explicitly frozen, dormant Task013 code-and-plan scope. No actionable finding; source, canonical contract, fixture checks and focused test design support the author claims within stated limits. This verdict is not root acceptance or a claim that pending full gates passed.

## Recommended next actions

Root completes and reconciles its existing full gates/Boundary/CI before acceptance. Retain host publication, actual positive campaign provenance, extended Snapshot12/public activation and Task014–016 consequences as separate gates. No fixes, additional review instance or new workstream requested by this reviewer.
