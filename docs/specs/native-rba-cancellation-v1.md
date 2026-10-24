# Native unanswered RBA cancellation v1

Native event contract version 33, `combat-rba-cancelled`, closes an unanswered
Retreat Before Assault decision. This is native runtime behavior, not original
300-root/P01 evidence or public certificate admission.

Entry: accepted contact-assault selection and Barrage, optionally followed by
accepted defender retreat moves, with current cursor at that Barrage's RBA
position. A real accepted decline, later decision, or prior cancellation closes
this entry. Exit: same turn/operation-slot Reserve Release. One atomic event
skips Force Assignment, Anti Armor and Close Assault without fabricating their
choices, seals or outcomes. No decline, roll, ammunition debit, surrender or
custody event is generated. Current World (including accepted retreat movement)
and selection RNG remain unchanged.

Canonical fields, in order: contractVersion, eventType, campaignId,
priorStateVersion, decisionActionId, positionId, nextPositionId,
configurationHash, openedAtUnixMilliseconds, deadlineUnixMilliseconds,
highWaterUnixMilliseconds, optional cancelledAtUnixMilliseconds, reason,
barrageEventHash, optional retainedRetreatHash. State version advances once.
Original Barrage hash is immutable across retreat moves; retainedRetreatHash
binds latest accepted move. Replay rebuilds event from retained authority and
requires byte equality; unknown, missing, duplicate or noncanonical fields fail.

Host persists original opening/deadline in same transaction as automatic Barrage.
Budget is timing configuration's `rba` budget; ruleset hash must match campaign.
For timed rounds: opening >= 0, deadline > opening, high-water >= opening and
strictly below deadline. `deadline` requires observed cancellation >= deadline
(equality expires); `clock-regression` requires nonnegative observed time below
high-water; `clock-unavailable` and `controller-unavailable` require no observed
time. Host verifies trusted timestamps against persisted round; Core verifies
shape, budget and current authority. Retreat moves never reset deadline.

Pre-opening `rba-unavailable` is distinct: opening/deadline/high-water all -1,
no cancellation time and no accepted retreat. Configuration hash is actual
configuration hash when available, otherwise SHA-256 zero sentinel. Mixed real
and unavailable timestamps fail. Host may use this only before a round exists,
for unavailable configuration or opening clock; it creates no invented budget.

HostAdapter exposes QueryNativeCombatRbaRoundBinding, CancelNativeCombatRba and
NativeCombatRbaCancellationCommand. Accepted command is System-scoped and binds
prior checkpoint plus SHA-256 canonical cancellation event. Accepted-step replay
and persistence retain exact event/receipt, supporting idempotent recovery.
