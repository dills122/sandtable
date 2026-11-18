# Land-only supply capacity: decision input

**Disposition:** RUL-GRA-08 already selects an expanded three-unit limit for ordinary on-map non-major-city hexes **for planning**. The source's printed “off-map” wording does not expressly provide that on-map cap, and the literal alternative leaves the on-map count unspecified rather than unlimited. This comparison selects no new rule or runtime behavior.

## Alternatives and distinguishing state

| Question | RUL-GRA-08 planning baseline | Literal-scope alternative |
| --- | --- | --- |
| Ordinary on-map, non-major-city hex | Apply a three-Supply-Unit count limit. | §32.14's printed three-unit clause does not expressly establish a limit here; leave count limit unspecified pending a separate interpretation. |
| Major-city hex | Retain the printed five-unit limit. | Same printed five-unit limit; applicability and interactions still need a contract. |
| Other off-map hex | Retain printed three-unit wording, subject to classifying the location and reconciling any applicable box exemption. | Same. Neither choice alone resolves §8.87 or whether a particular off-map facility is an eligible hex. |
| Tripoli/Tunisia region boxes | Retain explicit unlimited Supply Units. | Same express exception; exact box identities and interaction with other box wording still need definition. |

Minimal synthetic check: take a certified ordinary on-map non-major-city hex with **three regular** Supply Units; request placement of a fourth, with all other action, control and placement conditions independently satisfied and no scenario exception. The baseline rejects the fourth on its count limit. The literal alternative cannot decide that count-limit question from §32.14 alone: it is **unknown**, not automatically legal. A companion control case uses an independently certified eligible **other off-map hex**, outside any applicable unlimited-box exemption, with the same three regular units and fourth request: both readings apply the printed three-unit clause, subject to the accepted location and box contract. These hypothetical states do not prove such a location exists, that an action is available, or scenario reachability.

§32.14 gives each Supply Unit zero ordinary stacking value; that fact is distinct from its separate counter-count wording. It does not alone authorize unlimited counters or source-prove how dummies count. RUL-GRA-09 selects counting both real and dummy Supply Units under RUL-GRA-08 for planning; this interprets, rather than settles, §32.14 and is not runtime admission. Do not infer one regular Supply Unit consumes one ordinary stacking point.

## Scenario boundary and unresolved cases

Graziani §60.92(C) fixes five regular Supply Units at Matruh. Under RUL-GRA-08's selected on-map cap and its planning application of §59.65, preserve those five as an unavoidable scenario-distribution exception; prohibit additions while noncompliant, and enforce all normal restrictions once compliant. This is a per-hex transition, not permission to create optional excess elsewhere. Under the literal alternative, the fixed five do not themselves prove an on-map three-unit excess or establish unlimited capacity. Determine separately whether a proposed optional placement is unavoidable and whether any scenario-specific instruction changes its treatment.

Desert Fox §61.72(D)'s maximum two regular units at each location in one named setup distribution is a separate scenario-specific rule, not a general cap or an amendment to Graziani's fixed Matruh units. It does not expressly include dummies in that two-regular restriction. The scope of other scenario-specific overrides remains unverified; do not generalize either setup exception to all fixed or optional placements.

Before any admission, define ordinary on-map/off-map identity, major-city and box classification, §8.87's In Transit exemption versus §32.14's separate count, carried units and real/dummy counting, and the unavoidable-excess lifecycle. Apply RUL-GRA-09’s selected real-and-dummy count in planning; preserve unknown interactions and unverified placements as fail-closed pending their own contracts and runtime admission. A missing location, category or exception is not proof of either capacity or legality.

## Evidence, replay and disclosure

[Source review](2026-11-17-graziani-supply-stacking-source-scope-review.md) visually checked the original Land, scenario, map and September errata scans and found no direct §32.14 correction in that bounded packet. [Later-scenario review](2026-11-18-graziani-supply-capacity-source-next-review.md) checks the Desert Fox comparison. These are same-edition readings, not an exhaustive errata search or a rights grant.

If implemented, bind the selected capacity, location and exception lifecycle to an immutable ruleset and replay the fixed setup, additions, reduction to compliance and subsequent re-entry deterministically. Dummy Supply Units have a concealment purpose; if count, rejection, action availability or timing depends on hidden dummy identity, define authorized projections and compare otherwise indistinguishable histories before exposing the result. This is a conditional software admission concern, not proof that a particular rejected move leaks information or that hidden identity must count. No gameplay tests, policy changes, runtime or data edits.
