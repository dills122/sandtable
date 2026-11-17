# Graziani Commonwealth supply: Alexandria placement candidate

**Status:** bounded policy proposal `gra-alexandria-placement-v1-proposed`,
not an official clarification, accepted ruling, or runtime change. This is a
fresh source check but **not an independent audit of the earlier schedule's
authorship**: the same researcher drafted both documents. Independent review
and acceptance remain separate gates.

## Printed facts and schedule check

Land §32.42, printed/PDF p. 43, directs Commonwealth Supply Units to Cairo;
only when **more than three are received in a single Game Turn** may one per
three received be placed in Alexandria. It says *may*, preserving owner choice,
and specifies neither fractional rounding nor how to place earlier receipts
when later same-turn receipts are possible. §32.31 permits a supply unit to
move by land or rail in its arrival OpStage, subject to other restrictions.
These are separate provisions; neither mandates a particular batch or proves
that all placement may be deferred to the end of the Game Turn.

Scenario §60.92(C), PDF p. 5 / printed p. 29, independently lists initial
supplies including three regular units each in Alexandria and Cairo. Those are
scenario setup, not ordinary §32.42 table deliveries. Section 59.66(C) and
dummy entitlements require separate reconciliation. Commonwealth table §32.47
period I yields 1, 1, 2, 2, 3, 3 for one die numbered 1–6; it covers 1940.
The [six-turn matrix](2026-11-17-graziani-supply-schedule-next.md) correctly
distinguishes starting setup, pregame uncertainty and later availability from
receipts. It correctly says **one** period-I table result alone cannot exceed
three. This conditional fact does not prove that the full turn's qualifying
receipts cannot exceed three; whether other sources or delayed deliveries
qualify and whether they can actually occur remain open. Do not count availability
as receipt, add setup stocks to the turn total, or assert an Alexandria arrival
in historical play from these synthetic examples.

## Proposed restricted contract

Admit this rule only once the arrival contract identifies authoritative,
complete, nonnegative whole-unit **qualifying receipt batches**, their Game
Turn and arrival OpStage, and the receipt types that count. For now the only
proposed positive category is an actual Commonwealth §32.47 supply-unit
delivery; the table roll or entitlement alone is not receipt. Treat
§60.92(C) scenario setup, including Alexandria's initial three, as separate and
outside the receipt count. Any other candidate source, delayed entitlement,
dummy or setup-derived transfer has unknown applicability until classified by
an accepted rule: do not silently include it or certify that it cannot occur.
Ordinary movement, capture, redistribution or another observation of a unit
already received is not a second receipt under this candidate. Reject or block
evaluation when a potentially qualifying receipt's classification, quantity,
date or identity is unknown; do not default unknown to zero.

For each Game Turn maintain `R`, number of qualifying units actually received
so far, and `A`, number of those units previously placed in Alexandria under
this rule. Both start at zero after separate setup. On each atomic batch of `n`
new units set `R_next = R + n`, and define the running cap as `0` if
`R_next <= 3`, otherwise `floor(R_next / 3)`. The player chooses `k` units
from **this batch** for Alexandria, where `0 <= k <= min(n, cap - A)`; place
the others in Cairo. Set `A_next = A + k` with the receipt and placements in
one atomic event. The floor is a whole-groups **project interpretation** of
“one for every three,” not a printed rounding direction. Receipt batches
simultaneous under the accepted arrival sequence must be combined before
offering a choice; a caller cannot split a simultaneous batch to manipulate
the result. This policy never relocates a unit already placed, and never
creates or carries an unplaced unit to a later stage merely to await a higher
ratio. Owner may choose fewer than the cap. Preserve normal city identity,
placement capacity and legality as separate admission gates; a blocked or
unknown destination does not automatically destroy, redirect or duplicate a
unit.

The running rule conservatively avoids granting a placement before the
threshold is actually met, while letting units move in their arrival stage.
It is **not equivalent** to calculating the final turn total first. For batches
of three then one, it forces the first three to Cairo and allows the fourth to
Alexandria; a policy with an authoritative known future total could allow an
earlier unit there instead. Other alternatives include reserving allowance
against a guaranteed final total or deferring placement to turn end. The former
requires accepted guarantees about future receipts and interruption; the latter
can affect §32.31 arrival-stage movement. Neither is selected. If an unknown
future batch later becomes known to belong at an earlier simultaneous boundary,
do not retroactively edit prior state: resolve ordering/provenance before
admission or block that history. This is a deterministic planning choice, not
proof of the only source-consistent result.

## Distinguishing vectors

All numbers below are synthetic qualifying receipts, not a six-turn result.

| Same-turn batch sequence | Proposed Alexandria choices | What it checks |
| --- | --- | --- |
| 3 | 0 | Strict more-than-three threshold. |
| 4 in one batch | 0 or 1 | Owner choice; floor cap 1. |
| 5 in one batch | 0 or 1 | Remainder does not create another unit. |
| 6 or 7 in one batch | 0, 1 or 2 | Complete-group cap; 7 does not produce 3. |
| 3 then 1 | First batch 0; second 0 or 1 | Running threshold, no retroactive relocation. |
| 4 then 2 | First at most 1; total at most 2 | Cumulative cap rather than per-batch cap; if first declined, second can use two. |
| 3 then turn changes then 1 | Each turn 0 | Reset window; no carry of ratio allowance. |
| Setup 3 at Alexandria, then receipt 3 | No new Alexandria placement | Setup excluded; do not erase the specific setup. |

Also test simultaneous four as one batch versus two sequential twos, zero
units, duplicate event/replay, invalid allocation, incomplete potentially
qualifying receipt, unavailable city, conditional qualifying later arrival,
and crash between receipt and placement. No partial state, extra placement or
second receipt on retry. Protect private future plans and allocation choices
under the accepted side-specific visibility rules. Replay must preserve source,
policy version, batch boundary, owner choice and identities without recalculating
earlier allocations using future knowledge. Ensure any accepted turn-end rule
preserves or explicitly expires **only** the ratio allowance; it does not erase
placed stock, decide undelivered entitlement carryover or manufacture a zero.

## Source scope

Rechecked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 43–44, §§32.31 and 32.41–.45 (SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`),
[scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 5 / printed p. 29, §60.92(C) (SHA-256
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`), and
[Commonwealth chart](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf)
PDF p. 31, §32.47 (SHA-256
`f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`).
The prior schedule records a bounded September errata text search, not proof
that all later or unindexed corrections are absent. City capacity, all other
receipt categories, exact batch timing and historical reachability were not
exhaustively audited. No gameplay tests were run.
