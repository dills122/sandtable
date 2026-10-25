# Native cycle-control timeout v1

Native event version34, `movement-combat-cycle-timeout`, records System fallback
`finish-phase` after completed same-cut Reserve Release. It enters same-slot
Truck Convoy Movement, preserving World, contact facts, RNG and cycle ordinal.
It never creates an owner Finish command, Repeat choice or movement witness.

Canonical order: contractVersion, eventType, campaignId, priorStateVersion,
priorEventHash (completed Reserve Release head hash), priorCycleOrdinal,
resultWorldHash, nextPositionId, fallback (`finish-phase`), configurationHash,
openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds,
optional cancelledAtUnixMilliseconds, reason. Version advances once.

Timed reasons: deadline, clock-unavailable, clock-regression, controller-unavailable.
Opening must be nonnegative, deadline greater than opening, high-water between
opening inclusive and deadline exclusive. Deadline expires at equality; regression
requires nonnegative observed time below high-water. Unavailable clock/controller
has no observed cancellation time. Configuration must match campaign rules/content
and exact `cycle-control` budget at trusted command application/import.

Pre-opening `cycle-control-unavailable` has all clock fields -1 and no observed
time. Configuration hash is actual timing hash when supplied, otherwise an explicit
zero SHA-256 sentinel. No old or invented timestamp is accepted as entry evidence.
Host persists and verifies deadline authority, including opening at V27 completion.

V34 shares V28's internal Finish projection and runtime control slot, but retains a
separate canonical event type/version and System accepted-step kind. V28 owner
bytes remain unchanged. Canonical replay reconstructs V34 from the completed
release and exact retained clock proof, rejecting unknown/noncanonical fields,
changed World/release/ordinal/cursor bindings and early expiry.

Host adapter: QueryNativeCycleControlRoundBinding; TimeoutNativeCycleControl;
NativeCycleControlTimeoutCommand; NoTimingCycleControlTimeoutHash. Accepted-step
kind `native-cycle-control-timeout`, accepted-step contract version28 (distinct
from native event version34). Public certificates and original P01 ancestry remain
outside this native runtime contract.
