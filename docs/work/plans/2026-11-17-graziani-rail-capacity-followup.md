# Graziani rail: initial stock and capacity interaction

**Disposition:** The fixed Land-only Matruh stock is five regular Supply Units,
not five independent trainloads. A conditional `5 → 3 → 1 → 0` evacuation needs
three Operations Stages under the proposed stage-wide two-counter rail cap;
that arithmetic does not establish an available or legal historical journey.
A simultaneous opposite-direction supply delivery is a useful discriminator
between capacity interpretations, not an established legal alternative.

## Source check

I visually reread the same hash-matched original scans, with the previous
[rail policy review](2026-11-17-graziani-rail-policy-review.md) and [stacking
scope review](2026-11-17-graziani-supply-stacking-source-scope-review.md)
known beforehand. This is not blind transcription, another edition, or an
exhaustive search of all later corrections.

- [Scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
  PDF pp. 4–5 / printed pp. 28–29, §§60.7 and 60.92(C): Land-only replaces
  §60.44's initial supply status. Five regular Supply Units start at Mersa
  Matruh D3714, each with 40 ammunition and 60 fuel. Alexandria and Cairo
  each have three regular units with those same contents. One group of three
  real units and one group of three dummies have the separate map-D-west-of-
  Matruh placement instruction. The railroad ends at Matruh initially.
  Stock locations are not evidence of completed individual paths or counter
  availability after play begins.
- [Land](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), pp. 15–16,
  §§8.72, 8.74–.77: Commonwealth has one rail movement per OpStage, one stack
  in each direction; maximum rail-carried stacking points are two. Units may
  be picked up and dropped off along the line while observing stacking,
  direction and CP restrictions. Section 8.75 points to detailed supply
  maximums; for this mode §32.34 separately provides two real and/or dummy
  Supply Units in one direction as an OpStage's total load, with normal rail
  movement rules. Section 32.14 gives Supply Units zero stacking value, so the
  numerical two-stacking-point limit alone does not count supply counters.
- The same §32.14 prints a five-unit major-city limit and a three-unit clause
  for other hex types **off-map**. It does not expressly impose three in an
  ordinary on-map non-major-city hex. Under the separate proposed on-map
  three-unit interpretation, §59.65's unavoidable initial excess matters for
  the printed five at Matruh; under the literal off-map reading, that excess
  is not established. Section 59.65 forbids additional supply units while
  excess remains and restores normal restrictions when compliant; optional
  additions are not automatically unavoidable. Map classification and exact
  exception scope are discussed in the linked stacking review.
- [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
  p. 1, §§8.71 and 8.73: Axis rail use is referenced to §54.4, and a unit
  traveling by rail cannot earn reorganization points in that stage. Neither
  establishes an Axis entitlement under the Commonwealth-specific §32.34 or
  resolves the timing of already-earned points. No capacity correction to the
  inspected cases appears on that page; this is not a claim about all errata.

Verified SHA-256: Land
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`;
errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.

## Distinguishing vectors, not executed journeys

Assume five distinct starting Matruh regular units, no other changes to that
stock, and separately certified rail phase, eligible origin and destination,
path, control, occupancy, CP and destination capacity. Their stated original
contents remain attached to each selected counter unless independently changed
under applicable rules. These assumptions do not certify any map edge or trip.

| Operation | Proposed shared-service policy | Question exposed |
| --- | --- | --- |
| Two Matruh units move east in stage 1; two more east in stage 2; final one east in stage 3. | Origin counts `5 → 3 → 1 → 0`; each stage carries at most two. Counts alone do not authorize movements or combine them into one stage. | Under the proposed three-on-map cap, first removal reaches three and ends an applicable initial-excess condition; under literal off-map reading this transition has no established three-unit threshold. Independently check receiving capacity after each delivery. |
| In a single stage, two Matruh units move east and one Alexandria unit moves west. | Reject under the proposed combined one-direction, two-counter supply ceiling. | A separate-service or other precedence interpretation could differ. General §8.72 provides two directional stacks but does not explicitly reconcile §32.34's total one-direction load with mixed services. |
| In one stage, two Matruh units move east and an otherwise legal non-supply stack moves west. | Supply cap alone does not reject the westbound stack; apply its own two-stacking-point and all other constraints. | Whether mixed loads, sequential ordering and route interaction are legal still needs a complete transport contract. |
| Eastbound rail stack drops one supply unit at an intermediate point and picks another up later. | Count both selected Supply Unit identities against the proposed stage total even if simultaneous aboard count stays one. | This is a conservative interpretation of “total load,” not an explicit source definition of cumulative boarding. Compare with peak-on-board interpretation before adoption; §8.77 permits pickup/dropoff subject to restrictions. |
| At Matruh with five, try adding a supply unit before or after a hypothetical first eastbound two-unit departure. | Under the proposed on-map three-unit cap and applicable §59.65 exception, addition before departure is barred; at three, a further addition must satisfy normal capacity and remains over three. | Exact action ordering, atomicity and whether initial excess ends at or after compliance require accepted state-transition rules. Do not apply this conclusion as a printed on-map cap. |

For real/dummy mixtures, test both as counted units under §32.34 while preserving
owner and opposing-player authorized observations; counter identity and source
composition cannot leak through action availability or rejection. Replaying a
committed movement must not consume capacity or cargo twice. Empty and declined
orders require separate phase semantics, not a fictitious cargo movement.

## Remaining gates

Choose and version §8.72/§32.34 precedence and whether “total load” counts
unique boarded counters or peak onboard; define one-stack batching and pickup
order, stage rollover, stacking scope and exception transitions. Establish
source-backed route topology, initial rail control, legal endpoints, damaged or
unbuilt track, ZOC/occupancy and information disclosure, CP history, and
reorganization timing before exposing a rail order. This review did not certify
route reachability, enable actions, change runtime or content, or run gameplay
tests.
