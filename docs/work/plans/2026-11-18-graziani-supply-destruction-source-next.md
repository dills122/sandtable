# Supply destruction: prior expenditure case

**Result:** Land §6.14's printed 1st Royal Northumberland Fusiliers example gives a concrete cross-player-portion CP history. It constrains a destruction calculation that starts a player's portion with a fresh allowance or substitutes remaining CP for CPA. It does not settle §32.17 timing, multi-unit aggregation, payer allocation, legality of overrun, or hidden-state disclosure. No actual destruction attempt is established.

## Printed example and conditional comparison

[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF p. 12 §6.14, describe the first Operations Stage of the September III 1940 turn. The non-motorized 1st RNF has CPA 8; after an Italian attack it retreats three hexes before assault for 6 CP. The example states that when the Commonwealth portion starts, the battalion has **2 CP remaining before it exceeds its CPA**. The Italian attackers have not spent CP on close assault because the retreat preceded it. This is the rulebook's illustrative history, not proof that those events occurred in a particular Sandtable run.

Section 32.17, p. 43, states a charge of one-half the CPA of all units in the hex, rounded upwards. For a **hypothetical** later otherwise eligible attempt where the relevant charge basis is only that CPA-8 unit, half CPA is 4, while its illustrated remaining headroom is 2. Treating its remaining 2 as a fresh CPA would instead produce 1; treating its stage expenditure as reset would misstate headroom as 8. This distinguishes the printed allowance from unused headroom and preserves previous cross-portion spending in any future debit. It does not prove the 1st RNF is co-located with supply, is the sole unit in the hex, may initiate this action, or can legally incur that expenditure. Nor does the example prescribe how a hex-wide charge is assigned among units; no pooling follows from the arithmetic.

The prospective total of 10 CP would exceed CPA by 2 **if** the entire 4 CP were validly charged to this unit. It is not a ruling that the attempt is allowed, or that the resulting cohesion and other constraints can be ignored: §6.0 acknowledges overrun consequences, while corrected §8.17 and other conditions require separate application. The 1st RNF's original retreat is not a destruction attempt and does not define §32.17's sub-segment, its movement cutoff, or a subsequent cycle. Its printed prior-portion sequence is compatible with a later opportunity only if separately established timing and actor conditions permit it.

## Disclosure and remaining gates

The example is public rulebook text; it does not make an actual game's CP history, dummy identity or per-target outcomes public. It supplies no disclosure protocol or evidence that a destruction attempt discovers a dummy. Any proposed offer, rejection, cost, visible event, timing, or subsequent RNG trace must be checked against authorized observations under paired hidden histories. This is a software verification requirement, not a printed Land rule. See [timing review](2026-11-17-graziani-destruction-timing-policy-review.md) and [target review](2026-11-17-graziani-destruction-target-scope-review.md).

This example adds a concrete source case for a distinction already recognized in the queue; it does **not** resolve the primary aggregation or timing ambiguity. A versioned policy still needs target scope, payer allocation and rounding, phase/cycle identity, overrun admission, dummy handling and side-safe observability. Preserve unknown inputs instead of inferring a legal attempt.

## Provenance

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF pp. 12 and 43, §§5.2.G, 6.0, 6.14–.16 and 32.17–.18. Local SHA-256 checked: `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`. September errata local SHA-256 checked: `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`; the §8.17 correction relies on the linked earlier review, not a fresh errata inspection. Same scan as earlier work, not independent edition or exhaustive source search. Documentation only; no runtime changes or gameplay tests.
