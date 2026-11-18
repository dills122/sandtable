# Land-only supply destruction: §32.17

**Result:** The printed action is a pre-movement, CP-costed, per-Supply-Unit die attempt, with an explicit one-attempt-per-unit-per-subsegment limit. It is separate from §32.13 capture, §32.15 both-zero depletion and §32.18 dummy discovery. Source does not completely specify target selection, CP allocation or dummy interaction; no attempted destruction, map contact or die result is inferred for Graziani.

**Later source qualification:** §32.12 represents abstract Supply Units with
Supply Dump counters, so the common §6.3 one-third-CPA dump action cannot be
excluded by terminology alone. The relationship between it and §32.17's
one-half-CPA instruction needs an explicit precedence decision. See the
[follow-up](2026-11-17-graziani-destruction-subsegment-source.md) and
[independent review](2026-11-17-graziani-destruction-subsegment-source-review.md).
The half-CPA values below quote §32.17. [RUL-GRA-21](2026-11-12-graziani-ruling-decisions.md#rul-gra-21-land-only-supply-destruction-cost-bounded-planning)
selects that procedure for Land-only planning, but allocation, rounding and
runtime admission remain open.

## Printed action and exact boundaries

| Element | Source-backed reading |
| --- | --- |
| Actor / location | §32.17 says players may attempt to destroy Supply Units **they are with**. Preserve acting side, exact hex and the actual units with the target. It does not explicitly define remote destruction, movement into the hex, whether every co-located friendly or enemy unit contributes to cost, or how to select among indistinguishable real/dummy counters. |
| CP | The player expends **one-half of the CPA of all units in the hex, rounding upwards**. This is an action expenditure, not a one-half-CPA supply trace or a fixed cost per supply counter. Wording does not specify whether to round separately for each unit or after an aggregate, nor resolve parent/counter aggregation or units with special CPA. Record the CP basis and cost policy before numerical debit. |
| Dice | Roll **one die for each Supply Unit in the hex**. An outcome of **1, 2 or 3** destroys that Supply Unit; any other result leaves it unaffected. The rule's plural wording does not explicitly say a player may omit eligible units, or how dummies are treated; do not assume target subset semantics or invent rolls. |
| Modifier | If the **sum of Stacking Points in the hex during the attempt is one or less**, add **one** to the die roll. On an ordinary six-sided die this means raw 1–2 succeed and 3–6 do not; with a sum greater than one, raw 1–3 succeed. Do not silently clamp the adjusted 7 or alter the given threshold. The source does not expressly define every accounting case for shell formations, represented children, noncombat counters, ownership or dummies; apply separately sourced stacking rules to a shared snapshot. |
| Timing / repeat | Attempt occurs **prior to any movement in a given sub-segment of a Movement/Combat Segment**. The exact last sentence limits **one attempt per Supply Unit per sub-segment**. It does not say one attempt per hex, per player, per full Operations Stage, or per entire game. §5.2 uses Movement and Combat Phase with repeatable Movement, Breakdown, Combat and Reserve Release Segments; the precise mapping of §32.17's “sub-segment” to the outline needs a versioned timing interpretation. Do not infer a retry merely from a new player order. |
| Outcome | Destroyed and unaffected are the printed results. §32.17 does not explicitly itemize the surviving stock, handling of each assigned Motorization Point, action-event visibility, or a dummy's fate. A destroyed real unit must not continue to supply, but exact resource and carrier transitions need an accepted conservation and ownership policy. A failed attempt does not itself consume the unit's ammo/fuel. |

The CP instruction's aggregation ambiguity is observable: two independent 15-CPA units yield 15 if half their total is rounded once, but 8 + 8 = 16 if each half is rounded before adding. This arithmetic contrasts interpretations; it does not establish which units are valid payers, how a parent formation is represented, whether such a stack is legal, or that both units actually exist in one scenario hex. §6.0 allows exceeding CPA in some circumstances with cohesion consequences; do not substitute a blanket prohibition on exceeding remaining allowance for the printed expenditure rule.

An [independent review](2026-11-17-graziani-supply-destruction-review.md)
confirms the modifier and per-unit limit, and flags §6.16's prohibition on CP
transfers: an aggregate result is incomplete until a source-backed assignment
of individual payers is defined. A [second source check](2026-11-17-graziani-supply-destruction-review2.md)
also calls out represented-parent stacking and carrier accounting.

## Triggers, identity and privacy

The ordinary friendly-supply draw and capacity rules do not automatically authorize destruction from a distance. Capturing a real Supply Unit on enemy combat entry (§32.13) and destruction by die are separate transitions: neither implies the other. §32.18 removes a dummy on discovery; it does not describe exactly when an attempt discovers one, whether a dummy receives a roll, or how no-loss outcomes interact with hidden identity. Assigned Motorization Points are governed separately by §32.56, including capture when their assigned unit is captured; whether “destroyed” here equates to “eliminated” for motorization loss needs an explicit interpretation, rather than silently converting every carried point or treating it as captured.

Represent attempt identity using authoritative scenario/game, operation-stage and resolved subsegment identity, actor, hex, actual target unit identities, participating unit/CPA and stacking snapshot. Track attempted `(supply unit, subsegment)` to prevent a second attempt even if commands are retried; permit later attempts only when source timing and a later subsegment are independently established. Store die and adjusted result per resolved target in deterministic order, with distinct failure, destruction, capture and dummy-discovery events. Do not expose hidden dummy identity, actual balances, secret IDs, per-target action availability or unequal error/timing behavior to unauthorized opponents or intelligence-plane consumers. That implementation vocabulary is a proposal, not additional source text.

## Distinguishing vectors

1. Stacking total exactly 1: raw 2 becomes 3 and succeeds; raw 3 becomes 4 and fails. Total 2: raw 3 succeeds and raw 4 fails. Treat the stacking figure as a supplied source-certified snapshot, not a dummy-only counter count.
2. Two real Supply Units in the same hex: test the printed one-die-for-each language and independent outcomes; retry same unit in same subsegment is rejected without another die. A different unit in the same subsegment is not barred by a fabricated one-per-hex rule; any subset policy must be explicit.
3. Attempt before first movement versus after movement in the same identified subsegment, then a genuinely new subsegment. Preserve unsupported if the engine cannot establish the source's timing unit. Check actor/hex, no valid target, stack changes and actual CP cost before RNG.
4. Exercise one 15-CPA payer and two 15-CPA payers under both aggregate/per-unit rounding alternatives. Do not assume target stock or size establishes CP cost; distinguish already-spent CP and cohesion rules.
5. Real supply with 15 ammo/50 fuel, its assigned motorization, and a mixed real/dummy candidate stack: trace failure, destruction and any authorized dummy discovery without duplicating or exposing hidden resources. Compare equal authorized views and deterministic replay. Do not assert a specific carrier loss or discovery order before those policies are reviewed.

## Provenance

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF p. 43 §§32.12–.18, 32.32 and p. 44 §32.56; p. 12 §§5.2, 6.0–.17, SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`. September 1979 errata PDF pp. 1–2 contains a general §32.0 note but no explicit §32.17 correction on those pages, SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Same source edition as prior audits; no exhaustive errata or full combat/stacking proof. No runtime changes or gameplay tests.
