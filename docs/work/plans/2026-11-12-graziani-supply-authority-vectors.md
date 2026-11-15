# Graziani abstract supply: candidate authority vectors

**Status:** source-derived test design, not an implementation contract or adopted
scenario. Original [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 43–44 were visually checked by one reader; [scenario source register](2026-11-12-graziani-source-register.md)
and [accounting pilot](2026-11-12-graziani-supply-accounting-pilot.md) retain scope
and source limits. Independent source review, ruleset identity and precise typed
command/event schemas precede any implementation. Values below describe bounded
test hypotheses and are not evidence that a public path exists.

## State and invariants to freeze

An accepted design needs stable supply-unit and location IDs, owner, real/dummy
classification, separate ammo/fuel amounts, carrying/attachment state, capability
budgets, current stage, plan/arrival identity and source/ruling hashes. Private
opponent information and RNG remain outside observation and legal-action payloads.
Supply units are distinct from combat counters, physical trucks, motorization points
and victory-supply witnesses. Cross-type conversion or a hidden-state-derived
outward action requires an explicit authority and disclosure contract.

## Candidate source vectors

| ID | Source boundary | Positive vector | Negative or fork vector |
| --- | --- | --- | --- |
| SV-01 | §32.13 capture | Enemy combat unit enters a hex containing a real supply unit; apply the sourced capture/use transition once. | A nonqualifying entrant or unrelated adjacent unit must not capture; replay cannot duplicate transfer. Resolve dummy interaction separately. |
| SV-02 | §32.14 and §59.65 stacking | Source prints a major-city maximum, a three-unit rule using the phrase “other type of hex off-map,” and unlimited Tripoli/Tunisia boxes. The off-map wording needs reconciliation before a general other-hex rule is adopted. | Test at/above the accepted limits and legitimate scenario-specific unavoidable setup exception; distinguish on-map, off-map, dummies and later mutations. |
| SV-03 | §32.15 capacity and depletion | At most 40 ammo and 60 fuel per regular unit; source removes a unit when both are exhausted. | One component zero is not both zero; over-capacity, negative balances or duplicate removal reject. Do not reinterpret the source's Derna dash without a ruling. |
| SV-04 | §32.15 organization redistribution | Two eligible same-hex supply units exchange amounts in the source-defined phase without creating resources. | Different location/phase, invalid target capacity or total mismatch rejects without any partial transfer. |
| SV-05 | §32.16 source access | Unit has eligible friendly supply within half its CPA using the source-specified path and movement type. | One cost beyond range, impassable terrain, unoccupied hostile ZOC, wrong owner or no source: reject. Clarify exact comparison and rounding before golden values. |
| SV-06 | §32.17 attempted destruction | Eligible attempt spends the specified capability, rolls once and records success/failure under source restrictions. | No double attempt in same subsegment, no movement-before-attempt bypass, no RNG or debit on rejected command; cover modifier and rounding cases. |
| SV-07 | §32.18 and §60.92 dummies | An allowed dummy follows its permitted movement/discovery behavior without becoming a real supply unit. | Scenario's listed dummy groups versus general allowance need a ruling; equal-authorized histories do not expose dummy identity through actions, logs or errors. |
| SV-08 | §§32.21–32.22 combat ammo | Eligible phasing/non-phasing units pay their source-defined amount at the combat instant. | Test each combat role and applicable action, exact sufficiency versus shortage, simultaneous competing demands and rollback; do not debit rejected or already applied history. |
| SV-09 | §§32.23–32.25 movement fuel | Stage-bound movement pays fuel under the source's unit-type rules; verify the threshold when CPA is exceeded. | Test no movement, repeat movement, exact threshold, excess and types/activities exempted by source; no double charge after replay or retry. |
| SV-10 | §§32.31–32.33 motorized transport | Attach sufficient motorization in Organization; move eligible real or dummy supply using an allowed phase. | One below required amount, illegal phase, both movement paths in a stage, and loss of required friendly stacking reject. Verify non-phasing exception separately. |
| SV-11 | §§32.34–32.37 other transport | Exercise each source-permitted rail, sea or abstract-air route in a separately certified profile. | Test source/destination, quantity, capacity, timing, occupancy, stage and conflicting movement; no double use or unauthorized declassification. |
| SV-12 | §§32.41–32.47 planning | Record side-specific planned arrivals with source table row, planning turn and arrival turn. | Reject missing/changed prior plan, wrong timing or replayed random draw; distinguish a legal zero result from absent/unavailable evidence. |
| SV-13 | §§32.51–32.59 motorization | Preserve assignment, movement and losses under the abstract mode, including source-defined motorization exceptions. | No physical-truck Breakdown rule by analogy; resolve [monthly chart identity and date ambiguity](2026-11-12-graziani-abstract-logistics-crosswalk.md#monthly-motorization-loss-chart-identity-and-date-ambiguity); verify losses, capture, reassignment and conservation with tamper/retry cases. |
| SV-14 | §§32.61–32.66 convoy | Source-valid lane, capacity, cargo and bombing inputs produce one auditable result and delayed arrival. | Test forbidden lane, excess capacity, missing location/roll, each applicable table coordinate, rounding and double-arrival rejection. |
| SV-15 | §§32.71–32.9 other abstract effects | Source-reachable fleet, AA and road/track modifications execute within their actual eligibility. | Verify no unsupported omission, illegal extra action, incorrect mode or weakened non-road stacking; preserve information boundaries. |
| SV-16 | §60.81 victory vs §32.16 access | Accepted rule explicitly distinguishes supply draw from scenario victory supply. | A valid local supply draw alone cannot certify a victory trace; both/no winners and overlapping levels require explicit ruling and tests. |

## Review protocol

Independently check each cited case and relevant errata against the original scan;
split a vector when rules have different owners, timing or failure modes. For each
selected implementation packet, define event ordering and exact expected results,
exercise boundary tables and preserve existing historical identities. Test names or
synthetic fixtures alone are not evidence of a complete scenario path. No claim is
made that these 16 vectors exhaust the source rules or all failure paths.
