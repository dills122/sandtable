# Reserve Release control and retained history

Status: CMB-TASK-003D2b.1 complete as a bounded private contract checkpoint, input `89eac24`. Governing [plan](../design/combat-cycle-implementation-plan.md),
[cycle design](../design/continual-cycle-reserve-composition-v1.md#reserve-release-window), and accepted
[CMB-POL-007/008](../design/combat-cycle-policy-reconciliation.md). [Schema](combat-reserve-release-v1.schema.json),
[fixture](fixtures/combat-reserve-release-v1.json), [oracle](verify-combat-reserve-release-v1.py).
Private contract arm only; no new policy, production event registration or independent-review round.

## Trust and ownership boundary

ReleaseBase is the independently admitted CA-to-Reserve-Release boundary. It carries D1 Authority,
resolved stage order, exact Reserve Release position, version/prefix, Combat completion receipt,
retained World hash/RNG and stage attack history, accepted clock high-water and complete ordered own
Reserve membership/history. D2c must derive this input from actual accepted history, prove complete
membership and absence of immediate Reaction/Breakdown/round/settlement work, and embed this arm in
Snapshot12. Supplied hashes alone authenticate neither a World nor its history.

The ledger-probe fixtures use synthetic prefix/completion/designation/conversion/release receipts.
Multiple own members and CPA9 examples are isolated contract tests, not Content7/runtime admission.
Separately, settled-empty fixtures derive World/RNG, one own member and the actual CA completion
receipt by replaying C3c guard/escape cases. They prove no-disposition composition and retained
obligations from that cut; C3a's earlier synthetic pre-Combat lineage remains unproved. Positive
Reserve history is not silently added to a participant that just performed the selected assault.

The arm stores retainedWorldHash rather than duplicating every World resource. A composed reader
must verify that hash against independently admitted World and project only reserveStatus changes
for matched original own UnitKeys. The oracle's composition check does this for settled-empty
fixtures. It cannot provide a full positive-Reserve World/Snapshot12 reader before D2c. Membership
facts such as CPA/spent CP are frozen decision inputs; they are not a second mutable resource ledger.

## Membership and stage history

Own members sort by `(creationBinding, originalSide, elementId)` without duplicates, max32. All
keys share admitted creation and acting side. Scope is exact turn/stage/relative slot/acting side;
resolve the actual side from firstActingSide, including second-slot cases. CPA is positive; spent
CP is a reduced nonnegative rational. Neither release, conversion nor retention modifies spent CP.

| Current status at opening | Legal work |
| --- | --- |
| I, friendly ordinal1 | release-I to none, or convert-to-II. I cannot retain. |
| II, later friendly ordinal | release-II to none, or retain-II. |
| none | No unit decision. Preserve any prior release history. |
| II on first opening, I on later opening | Reject unsupported/inconsistent history; never normalize. |

History retains scope, designation receipt, optional conversion receipt and released type, release
receipt/cycle, CPA basis/ceiling, optional offensive commitment and next-Movement exception. Existing
II requires a prior conversion; released II requires that conversion too. Unreleased members have
no release fields. Released members have status none, complete release fields and the original CPA
basis. I ceiling is CPA; II is floor(CPA/2). Never grant a fresh allowance: CPA9/spent3/II has only1
voluntary CP left; already-spent7 with ceiling5 remains7 and permits no further voluntary spend.

Release creates a pending exception for the same scope and immediately next ordinal only. It does
not execute Movement, waive CP/terrain/control/relations, reset previous exclusions or reopen a
completed exception. Existing expired exceptions retain their completion receipt. D2b.2 owns use
and expiry on next Movement completion/phase finish; D2c binds actual predecessor scopes. Overflow
is a checked contract fault before offering an unrepresentable release, never automatic retention.

Release itself never consumes the one offensive allowance. An existing offensiveCommitmentId must
link to the unit's retained attacker record; release work preserves it. Actual released-Reserve
assault admission, the II pre-Morale DP and consumption of that link remain excluded from the current
Combat profile and belong to its explicit extension. Defensive participation is not offensive use.

## Ordered control and one deadline

An explicit system opening event exists even when there is no work. Pending members are exactly
the initial I/II queue. Open one decisionId and one Config-pinned `reserve-release` Timing only if
that queue is nonempty. Empty opening has no decision/deadline. No automatic release is inferred
from arriving at the structural position.

Only the current pending original UnitKey can be chosen. Each disposition consumes it once, records
before/after status and choice, then advances that fixed queue. A converted II cannot be revisited
in the same segment. Later retain-II changes no status/history and therefore is not material
progress. I-to-none, I-to-II and II-to-none are actual changes; D2b.2 derives progress from those
accepted effects, never from receipt count or a caller boolean.

At later occurrences, authenticated `complete-release` is available while optional II work remains.
One system-authored completion event records that intent and all implicitly retained units; it does
not emit per-unit empty events. At first release it cannot skip I. When no work remains, only system
`complete` is valid. Completion stays at Reserve Release and supplies one immutable completion
receipt to cycle control; it does not repeat, finish the phase or advance the catalog.

All owner input must be before the same deadline; equality is late and rejects without mutation.
Accepted reliable timestamps advance a persisted high-water but never change opening/budget/deadline.
Clock regression/unavailability on current input forces system fallback. Early expiry is a no-op;
stale decision timers after closure or another decision are harmless. Controller unavailability is
an explicit system input, not inferred from private choice content.

Fallback converts unresolved first-occurrence I in canonical order, or retains all remaining later
II in one completion. It locks in the first fallback disposition/completion; no late choice can
interleave. `fallback-step` drains remaining mandatory I and finally completes. Prior accepted
choices remain. Exact duplicate fallback commands return their old receipt, not the next step.
Opening clock loss/overflow records no fabricated Timing; owner actions are not offered. A queued
owner command can only trigger system fallback, and the next fallback event locks conversion/retention. Expiry after the last accepted disposition but before completion emits just completion, preserving
all choices. Recovery never renews a budget or requires model I/O.

## Commands, events and canonical readback

ReleaseCommand fields are closed: version/kind/releaseId, expected prior version (except timers),
decisionId (absent only for open), unit/choice only for choose. ReleaseInput adds authenticated actor,
admitted timestamp and clock-confidence flag. Open/complete/fallback-step/expire/unavailable require
system; choose/complete-release require the cycle owner. These checks precede exact-retry lookup.

Release identity is `rel.` plus domain-separated SHA256 of canonical `{baseHash,cycleId,positionId}`
under `sandtable.combat.reserve-release.v1`. Decision ID appends `.decision`. Event contractVersion1
uses `reserve-release-opened`, `reserve-unit-disposition-recorded`, `reserve-release-completed` and
closed typed effects. Envelope binds full Rules/configuration, campaign, cycle/position, base and
release IDs, prior/result version, prior prefix, exact input, effect and receiptId.

Receipt ID is `rr.` plus SHA256 of ASCII `sandtable.combat.reserve-release-receipt.v1`, zero byte,
and canonical event fields before receiptId. Projection then links release/conversion history to
that receipt, so the event never hashes its own derived history. Extend D1 prefix with complete
canonical event bytes. Persist C3a command receipts and the original authenticated actor, including
when a side input loses clock confidence and the actual event is system-authored fallback.

Exact canonical command plus actor retry returns the original receipt before stale-version rejection,
including after subsequent dispositions/completion. Changed stale commands reject. State readback
replays the suffix against separately verified base and compares complete bytes, including ordered
members/history, pending queue, dispositions, timing/fallback, retained World/RNG/history and receipts.
Missing history, reopened decisions, deadline renewal and reordered/doubled suffixes fail.

Canonical bytes are compact ASCII UTF-8 JSON with fixed inventory field order, mandatory nulls and
ordered arrays; reject BOM/newline, alternate escapes/numbers, duplicate/missing/unknown properties,
float/bool-as-int and unknown tags. Limits:1MiB per complete event/state, depth32,512 ordinary array
values,32 members and34 events (open + at most32 dispositions + complete). Capacity fails before
mutation. This fragment bound is not proof of full Snapshot12 capacity; D2c/009 retain that gate.
Private errors CMB-RRL-001 shape/size,002 numeric/primitive,003 unsupported family/history,
004 authority/base,005 deadline/action,006 replay/order,007 overflow/capacity,008 alternate bytes.

## Traceability and remaining gates

Advances CYCLE-COMP-AC-003/004, retained-ceiling part of005, exception creation part of006 and
private readback part of010. Full ACs remain mapped to D2b.2/D2c/004 and runtime017–019/022–024.
D2b.2 still owes supported continuation, material progress, repeat/finish and exception expiry.
D2c still owes positive World/Snapshot composition, actual first opening/inherited adapters and
complete CON-002–004 reconciliation. All25 task IDs,72 AC IDs and8 policies remain stable.


## Author verification

`python3 docs/specs/verify-combat-reserve-release-v1.py` passes13 literal cases across48 side/slot
traces:188 initial/event state cuts,2368 event/state/base mutations,840 malformed-byte checks,
20 timing/recovery cases and27 boundary checks. These include the exact32-member/34-event bound,
canonical order, invalid first/later history, rational/cumulative CPA limits, retained offensive
commitment links, ordinal/version overflow and a locked fallback with multiple unresolved I units.

Expiry after the last accepted unit choice but before completion was reproduced as a failing cut
and fixed: early expiry remains a no-op; due expiry/unavailability emits only completion, retaining
all dispositions/history. Exact expiry retry after closure returns its prior receipt; a distinct
stale timer produces no event. The distinction does not renew or progress a finished window.

Fixture pins48 base hashes,188 full-state byte lengths/hashes and140 event hashes; three canonical
terminal event strings cover midway fallback, owner bulk retention and settled-empty composition.
These are regression vectors recorded after literal assertions, not independent serializer parity.
Normal verification requires frozen vectors/source hashes and never regenerates them. All fourteen
predecessor/research oracles pass, including full Snapshot12 and D2a movement. All pre-existing
contract bytes and runtime source/tests/scenarios remain unchanged from `89eac24`; no .NET tests or
new independent-review round ran. ParentD2b stays open forD2b.2; production and actual inherited
campaign/Snapshot12 proof remain later gates.
