# Graziani: remaining source research and evidence queue

**Status:** November 17, 2026 inventory for a future research/coding session.
This consolidates open work identified in the current Graziani source register,
review packets, map and readiness handoffs. It is not a claim that every
reachable six-turn obligation has been discovered, that every listed item is
a current runtime blocker, or that prior research was independently approved.
Use the linked detailed packets for source locators, alternative readings and
negative cases. No source facts, runtime state, content hashes or readiness flags
are changed by this queue.

## Already answered; implementation or review remains

- Australian ratings and five Matruh battalion characteristics are transcribed;
  Essex/Durham's source-listed Matruh attachment stands despite later 23rd
  Brigade assignment. See [source handoff §§1–2](2026-11-16-graziani-source-material-research.md).
  Apply the scoped ruling without inventing initial assigned parents or erasing
  separate Matruh setup conflicts. OA `N`/`D` marks indicate six starting TOE
  points for each Matruh battalion, subject to scenario corrections; an
  [independent chart check](2026-11-17-graziani-unit-characteristics-independent.md)
  corroborates the characteristics, while later actual strengths remain unknown.
- CCNN 4th HQ and 201st/204th engineer chart facts are transcribed with
  conditional ratings. Preserve conditions; implementation remains. An
  [independent source review](2026-11-17-graziani-ccnn-independent-review.md)
  corroborates the chart reading. September errata §23.11 adds an engineer-specific
  friendly-combat-unit restriction requiring reconciliation with the chart key.
  A [later source review](2026-11-17-graziani-engineer-conditions-followup.md)
  confirms the restriction regardless of the friendly combat unit's ammo and
  distinguishes it from attack and chart-key conditions; mixed non-combat
  stacks and the interaction between source clauses need an explicit policy.
  [RUL-GRA-12](2026-11-12-graziani-ruling-decisions.md#rul-gra-12-engineer-parenthesized-defense-planning-interpretation)
  selects an independently reviewed bounded interpretation for planning;
  attachment representation, other combat prerequisites and admission remain
  open.
  The 1st HQ's weapon and initial gun strength are still unknown.
- `battlefieldComplete` and `controlCertified` remain false pending the five-step
  [playable-map work](../../specs/graziani-source-control-map.md#shortest-route-to-a-source-backed-playable-map).
  That decision is not a finished coverage/control contract.
- Isolating readiness blockers and verifying full setup, adding sourced facts,
  combat-obligation design and Host replay are implementation/verification
  work, not evidence that another source value has been established. Follow
  [readiness plan §1](2026-11-16-graziani-readiness-and-combat-plan.md#1-the-eight-current-readinessblockers-at-the-real-fixture).

## Source facts and decisions still needed

Each deliverable must cite exact source and page/case, distinguish printed facts
from a project interpretation, record conflicts and unknowns, and name affected
content and tests. Do not substitute a plausible historical value for missing
rules evidence.

1. **1st CCNN HQ artillery — source lookup completed with a confirmed gap.**
   Hash-matched Axis charts now establish `N` as
   maximum-strength notation, not a weapon type. OA p. 4's rule that guns are
   listed by weapon system conflicts with the p. 17 HQ row's `N`; chart §4.46b
   p. 26 permits one gun point but does not prove it is initially present.
   Actual deployment and weapon type are not determinable from the inspected
   original charts, relevant rules and errata; seek a further authoritative
   clarification if one becomes available. See
   [CCNN follow-up](2026-11-16-graziani-source-material-research.md#3-ccnn-headquarters-and-engineers).
   September 1979 errata §4.44b/§4.46 yielded no such correction.
   A [bounded follow-up search](2026-11-17-graziani-addenda-search-followup.md)
   also checked in-book addenda and other public leads without finding a
   verified official answer; unsearched later material may exist.
   A newly reacquired [counter-sheet check](2026-11-17-graziani-ccnn-counter-next.md)
   locates the 1st CCNN HQ but finds no weapon model or gun TOE there; its
   `(5)` is stacking notation. An [independent visual check](2026-11-17-graziani-ccnn-counter-review.md)
   corroborates those counter observations.
   This research result is **unknown**, not a placeholder value; retain it in
   any implementation and do not copy the legion HQ's 65/17.
2. **Matruh organization.** Establish initial assigned parent, if any, for
   Essex and Durham from sources; decide consequences of §59.2 setup
   reorganization. Reconcile errata §4.44b assigning Buffs and Hampshire with
   the OA's no-assignment statement, then check whether each relationship uses
   attachment capacity and which legal actions and costs apply. [RUL-GRA-03](2026-11-12-graziani-ruling-decisions.md#rul-gra-03-matruh-initial-assignment-working-interpretation)
   now records a scoped planning interpretation for assigned units and initial
   reorganization; independent review and versioned admission remain. The
   provisional errata exception is not an official correction of every
   interaction. Sources and competing readings: [formation conflicts](2026-11-12-graziani-formation-setup-conflicts.md)
   and [setup reconciliation](2026-11-13-graziani-setup-reconciliation.md).
   Chart §19.5's Matruh row has now been source-checked: six units for turns
   1–12, no tank and one gun class, with a separate two-company modification.
   A [reacquired counter-sheet check](2026-11-17-graziani-matruh-company-next.md)
   reads the single `Selby / Matruh` counter as `X`, `G`, `(3)`, strengthening
   nominal brigade-sized evidence but not resolving current shell status or
   the free-company exception. An [independent counter review](2026-11-17-graziani-matruh-counter-review.md)
   corroborates the marks; the chart's attachment limits are separate from
   that parenthesized stacking value. A further [company-modifier review](2026-11-17-graziani-matruh-company-review.md)
   preserves unknown eligibility and flags the chart's full-strength `Unit`
   definition versus the modifier's non-shell-company wording.
3. **Map identity, seams and coverage.** Five-sheet map reacquired and
   hash-matched. Independently record grid labels, edge features, terrain and candidate joins.
   The [blind 190-cell transcription](2026-11-17-graziani-distance-transcription.md)
   matches prior numeric values and independently reproduces 24 triangle
   violations. Verify coordinate labels and candidate geometry; resolve or
   block all 11 model discrepancies, including B5504–D2903 (80 printed, 78 candidate).
   The printed Barce–Qatrani distance 80 exceeds Barce–Tobruk 40 plus
   Tobruk–Qatrani 38, so at least one of these three shortest-distance cells
   cannot fit a consistent hex-grid metric; see [chart check](2026-11-16-graziani-seam-supply-review.md#map-seam-bounded-observations).
   Reconcile scenario Benghazi `B4827` with map/location evidence `A4827`,
   and independently review the existing working coordinate ruling. A [fresh
   source reading](2026-11-17-graziani-map-independent-review.md) confirms both
   source spellings and the three chart cells but did not approve the ruling or
   certify any seams. A same-stream
   November 17 recheck of Map A's location summary confirms it prints A4827;
   the Benghazi symbol obscures the underlying hex number. Define
   source-backed playable extent, missing-edge versus clear-edge semantics,
   boundary incoming ZOC, and distinct coverage/control predicates. Deliver
   reviewed per-edge evidence, source hierarchy and unresolved exclusions;
   [Provisional map decision](2026-11-17-graziani-map-distance-decision.md)
   defines fail-closed seam handling and preserves two chart contradictions;
   [edge reconnaissance](2026-11-17-graziani-seam-inventory.md) catalogs
   source labels and feature leads without certifying crossings. Independent
   [A/B](2026-11-17-graziani-seam-ab-independent.md) and
   [B/C](2026-11-17-graziani-seam-bc-independent.md),
   [C/D](2026-11-17-graziani-seam-cd-independent.md) and
   [D/E](2026-11-17-graziani-seam-de-independent.md) strip readings corroborate
   several feature leads but leave partial-cell ownership, line endings and
   crossings unproved. A `0433` has an apparent gap before the boundary; A
   `2634` is a vicinity locator rather than a confirmed outer label. C `3934`'s
   last dash also appears short of the boundary. Full
   per-edge coverage and admission are not completed.
   A [300-dpi C/D railway check](2026-11-17-graziani-cd-rail-followup.md)
   finds unfinished-rail marks at both candidate sides but different visible
   intersection positions. It retains the existing bounded policy without
   claiming precise registration or a usable crossing.
   A separate [C3533/D3501 track inspection](2026-11-17-graziani-cd-track-next.md)
   and [independent recheck](2026-11-17-graziani-cd-track-review.md) confirm
   local track marks while leaving sheet overlap and dash continuity unresolved.
   The September erratum adding the D2228/D2328 escarpment has an
   [independent source check](2026-11-17-graziani-map-remaining-independent.md);
   the pinned data already carries that feature, while its direction and
   rule-specific effects were not verified. A [direction check](2026-11-17-graziani-d2228-escarpment-direction-next.md)
   and [independent review](2026-11-17-graziani-d2228-escarpment-direction-review.md)
   find the erratum does not state the high side; the pinned D2328 direction
   is a contour-continuity inference requiring an explicit policy.
   A separate
   [Living Rules lead](2026-11-17-graziani-living-rules-lead.md) documents an
   external geometry interpretation, not independent primary map evidence.
   [Pinned geometry comparison](2026-11-17-graziani-seam-geometry-comparison.md)
   finds the same 33-column candidate and all 11 model distances, but broader
   generated admission and feature-union assumptions; it does not resolve the
   printed §4.1 wording or certify additional crossings.
   A [pinned-data inventory](2026-11-17-graziani-map-coverage-inventory.md)
   calculates 7,073 candidate hex records and 20,365 same-sheet neighbor
   pairs, of which 16,139 have no explicit side-feature record. Those missing
   records are not independently verified clear edges. The inventory exposes
   no per-edge provenance or verified-clear/unknown field and admits no seam.
   Two previously unknown slope directions now have [original-map evidence](2026-11-17-graziani-slope-directions-next.md)
   and an [independent check](2026-11-17-graziani-slope-directions-review.md):
   B1706 is the high side of both B1705/B1706 and B1706/B1805. A future
   versioned data correction and replay review remain implementation work.
   Its [independent review](2026-11-17-graziani-map-coverage-review.md) also
   identifies a static Malta column-00 mismatch between the candidate-pair
   count and runtime neighbor enumeration; impact on Graziani is unverified
   and needs a focused coding-session test.
   [Fourth Indian local edges](2026-11-17-graziani-fourth-indian-edges-next.md)
   and [independent review](2026-11-17-graziani-fourth-indian-edges-review.md)
   support two road/rail crossings and two nearby slope directions without
   certifying the wider route or junction. A later [D3615 junction check](2026-11-17-graziani-d3615-east-junction-next.md)
   and [independent review](2026-11-17-graziani-d3615-east-junction-review.md)
   identify a possible railway-side mismatch in the pinned data; overprinting
   prevents a certified correction. A [D3516/D3517 rail check](2026-11-17-graziani-d3516-d3517-rail-next.md)
   and [independent review](2026-11-17-graziani-d3516-d3517-rail-review.md)
   corroborate a visible railway crossing absent from the pinned feature
   records, without certifying route use or nearby road membership. The adjacent
   [D3517/D3518 check](2026-11-17-graziani-d3517-d3518-rail-next.md) and
   [independent review](2026-11-17-graziani-d3517-d3518-rail-review.md)
   corroborate both road and rail marks already recorded on that side.
   A [six-side railway batch](2026-11-17-graziani-matruh-east-rail-batch-next.md)
   and [independent review](2026-11-17-graziani-matruh-east-rail-batch-review.md)
   find the pinned railway marks consistent with the scan, with vertex and
   overprint qualifications; other side features remain unverified.
   The [Fuka–El Daba batch](2026-11-17-graziani-fuka-east-rail-batch-next.md)
   and [independent review](2026-11-17-graziani-fuka-east-rail-batch-review.md)
   corroborate road and railway features on seven more sides, not route use.
   Geometric
   distances alone cannot certify movement. See [seam recheck](2026-11-16-graziani-seam-supply-review.md)
   and [map identity pilot](2026-11-12-graziani-map-identity-pilot.md).
4. **Victory and supply trace.** Independently review original scenario, Land
   rules and errata; choose and version Land-only route semantics, source and
   unit predicates, map D quantifier, endpoints, ZOC/occupancy, terrain, route
   length, terminal timing, outcome precedence and simultaneous/no-match cases.
   Confirm the working §60.81 reference ruling separately. No inspected source
   supplies a complete replacement algorithm; retain alternatives and a
   fail-closed gate until decided. See [victory supply review](2026-11-16-graziani-victory-supply-source-review.md)
   and [victory matrix](2026-11-12-graziani-victory-matrix.md). An
   [independent proposal](2026-11-17-graziani-victory-independent-decision.md)
   specifies candidate result composition; [RUL-GRA-07](2026-11-12-graziani-ruling-decisions.md#rul-gra-07-bounded-facility-and-victory-planning-policies)
   selects it for planning. Route predicates and terminal ordering still
   require acceptance and source-backed definitions. A separate
   [route candidate](2026-11-17-graziani-victory-route-followup.md) makes
   proposed endpoint, occupancy, ZOC, weather and incomplete-map behavior
   explicit; its extrapolations are not adopted or official source facts.
   [Independent review](2026-11-17-graziani-victory-route-review.md) flags
   route direction, proof completeness, hidden-state disclosures and
   row-specific quantifiers as remaining admission questions. A [v2 policy
   proposal](2026-11-17-graziani-victory-policy-next.md) specifies candidate
   directions and row choices, but its [independent review](2026-11-17-graziani-victory-policy-review.md)
   identifies a terminal publication-timing leak and witness-binding gaps;
   it remains unadmitted. A [convoy direction check](2026-11-17-graziani-victory-convoy-direction-next.md)
   and [independent review](2026-11-17-graziani-victory-convoy-direction-review.md)
   distinguish the scenario's source-to-objective and back-to-source wording
   from actual convoy movement; whether physical convoy restrictions apply to
   a hypothetical victory trace remains a policy question.
5. **Initial supply, trucks and abstract logistics.** Fresh-context supply and
   truck entries match all 146 values; 47 raw locator differences and their
   source-specific dispositions are retained in [independent reviews](2026-11-17-graziani-independent-reviews.md).
   Verify
   allocations, dummies, scenario replacement versus double counting, initial
   loads and errata. [RUL-GRA-08](2026-11-12-graziani-ruling-decisions.md#rul-gra-08-abstract-supply-stacking-bounded-planning-interpretation)
   selects a bounded planning interpretation for §32.14's “off-map” wording;
   [independently reviewed](2026-11-17-graziani-supply-stacking-review.md);
   resolve dummy interactions before admission. Resolve
   motorization loss chart/month and dash, supply planning horizons and initial
   exception, and sea/rail/land transport, convoy loss and port interactions.
   [Independent motorization and dummy review](2026-11-17-graziani-motorization-dummy-review.md)
   strongly identifies common chart §58.5, but September's dash, lookup month,
   rounding and removal shortfall remain open. Dummy markers are called supply
   units; their treatment under the selected cap and initial scenario grants
   requires an explicit interpretation.
   [RUL-GRA-09](2026-11-12-graziani-ruling-decisions.md#rul-gra-09-monthly-motorization-and-dummy-supply-bounded-planning)
   selects the source chart and previous-month key, preserves dash as unknown,
   and selects bounded dummy-count and initial-allotment interpretations for
   planning; the separately reviewed [round-down candidate](2026-11-17-graziani-rules-next.md)
   is now selected for planning, not admission. Future entitlement, assigned
   points and other listed gates remain open. [Rounding review](2026-11-17-graziani-rounding-review.md)
   distinguishes the full source denominator from the restricted removal set.
   [Independent review](2026-11-17-graziani-rul09-review.md) flags disclosure,
   timing and partial-placement questions. The [first-month source check](2026-11-17-graziani-first-month-review.md)
   identifies turn 1 as September III and turn 3 as October I, but the
   pre-operation convoy and first-month loss trigger remain interpretive.
   A [six-turn supply matrix](2026-11-17-graziani-supply-schedule-next.md)
   records planning horizons, conditional table outcomes and pregame gaps;
   [independent review](2026-11-17-graziani-supply-schedule-review.md)
   confirms its table values, calendar and offsets. The start-stage
   interpretation remains required.
   [RUL-GRA-11](2026-11-12-graziani-ruling-decisions.md#rul-gra-11-alexandria-supply-receipt-placement-planning-interpretation)
   selects a reviewed running placement cap for planning, while receipt
   boundaries, category classification, placement failures and privacy remain
   admission gates.
   [Capture research](2026-11-17-graziani-supply-capture-next.md) and its
   [independent check](2026-11-17-graziani-supply-capture-independent.md)
   distinguish enemy-entry and possible ZOC capture, establish capture of
   validly assigned motorization, and leave immediate-use accounting open.
   [Destruction research](2026-11-17-graziani-supply-destruction-next.md) and
   [independent review](2026-11-17-graziani-supply-destruction-review.md)
   confirm the per-unit/per-subsegment attempt limit, die and modifier; CP
   allocation and mapping the source's subsegment remain open.
   [Source expenditure matrix](2026-11-17-graziani-supply-expenditure-next.md)
   and its [independent check](2026-11-17-graziani-supply-expenditure-review.md)
   record printed ammunition/fuel amounts; repeated thresholds, multiple loads,
   source selection and shortages remain open. [Initial loaded-point rules](2026-11-17-graziani-loaded-motorization-next.md)
   and [review](2026-11-17-graziani-loaded-motorization-review.md) distinguish
   optional carried ammo/fuel from supply units and preserve mixed-draw ambiguity.
   [RUL-GRA-18](2026-11-12-graziani-ruling-decisions.md#rul-gra-18-anti-armor-only-ammunition-bounded-planning-interpretation)
   selects the printed Assault rate for an anti-armor-only participant as a
   bounded planning interpretation; mixed/repeated combat costs remain open.
   [RUL-GRA-17](2026-11-12-graziani-ruling-decisions.md#rul-gra-17-local-supply-range-planning-interpretation)
   selects a reviewed ceiling-of-half range for planning, preserving the
   possible example tension, direction and source/coverage gates.
   [Axis initial motorization accounting](2026-11-17-graziani-initial-motorization-next.md)
   and its [independent check](2026-11-17-graziani-initial-motorization-review.md)
   find 610 from explicitly listed non-air medium/heavy figures; this is a
   bounded subtotal, not a certified complete setup or placement.
   The corresponding [Commonwealth accounting](2026-11-17-graziani-cw-motorization-next.md)
   and [independent check](2026-11-17-graziani-cw-motorization-review.md)
   find an explicitly listed 147 attached plus 115 unattached, subtotal 262,
   with the same completeness and placement limits.
   [RUL-GRA-16](2026-11-12-graziani-ruling-decisions.md#rul-gra-16-commonwealth-replacement-planning-bounded-interpretation)
   selects a reviewed four-turn/planning-period interpretation for truck and
   infantry production and a pre-November rate-based replacement ban; actual
   plans, scenario reinforcement conversion and other categories remain open.
   Source check: §60.92(C) puts five supply units at Matruh, which the map
   summary does not classify as a major city; §59.65 addresses unavoidable
   setup overstacking. This is a required test case, not a general resolution
   of §32.14. See [Matruh stacking analysis](2026-11-12-graziani-abstract-logistics-crosswalk.md#matruh-supply-stacking-interaction).
   See [seam/supply review](2026-11-16-graziani-seam-supply-review.md),
   [logistics crosswalk](2026-11-12-graziani-abstract-logistics-crosswalk.md),
   [planning grid](2026-11-13-graziani-supply-planning.md) and
   [convoy matrix](2026-11-13-graziani-abstract-convoy-matrix.md).
   A [convoy loss comparison](2026-11-17-graziani-convoy-loss-groups-next.md)
   distinguishes detailed Air's nationality/class pools from the Land-only
   tank example, which does not decide combined tank/gun grouping. It also
   identifies a printed 5% result outside the explicitly described 10% and
   20%-plus branches; applicability and rounding remain open.
6. **Facilities and fortifications.** Reconcile Tobruk repair facility
   temporary/major sources, Commonwealth initial repair facilities under
   §60.92(C), repair resource costs in abstract mode, Tobruk port efficiency
   7 versus maximum 5 and obstruction, and whether scenario §60.7 removes
   inherent city fortifications. Establish reachability and cite decisions
   separately; port and repair facility conflicts are distinct. See
   [working rulings](2026-11-12-graziani-ruling-decisions.md) and
   [repair matrix](2026-11-13-graziani-repair-resource-matrix.md). An
   [independent planning review](2026-11-17-graziani-facilities-independent-decision.md)
   proposes bounded facility, fortification and repair-supply choices; RUL-GRA-07
   selects them for planning. An [independent policy review](2026-11-17-graziani-selected-policies-review.md)
   identifies guard conditions. Tobruk facility type after control change
   remains unresolved, and all choices need admission review.
   [Independent port source review](2026-11-17-graziani-tobruk-port-independent.md)
   confirms that scenario 7 and chart 5 are efficiency levels, not throughput;
   chart 5 less the three-level obstruction suggests 2 but conflicts with the
   explicit scenario start. No official correction was found in the inspected
   September errata; competing external house rulings do not settle the source.
   [RUL-GRA-10](2026-11-12-graziani-ruling-decisions.md#rul-gra-10-tobruk-initial-port-efficiency-planning-interpretation)
   selects 2 of 5 as a planning interpretation, retaining the conflict and
   pending admission and operation-specific rules; independent review is complete.
7. **Order of battle and arrivals.** Independently compare Axis formation
   members and relationships, corps-artillery duplication, initial attachment
   and strengths, Commonwealth reinforcement rows and the 6th NZ Brigade
   timing conflict. Verify French Motor Marine counter/TOE identity and later
   formation relationship; errata `D` is a working initial-deployment ruling,
   not proof of every identity detail. A same-stream OA/chart check now gives
   the company `v`, maximum and OA starting TOE 1, CPA 10+, and close assault
   1/1; [arrival scope](2026-11-12-graziani-arrival-scope.md#french-motor-marine-company-characteristics)
   retains its source and qualifications. Review applicable schedules and
   corrections before declaring any arrival category empty. A
   [later-counter comparison](2026-11-12-graziani-arrival-scope.md#later-similarly-named-french-units-bounded-source-comparison)
   and [independent source check](2026-11-17-graziani-marine-lifecycle-review.md)
   distinguish the initial company from two later chart rows; the inspected
   source does not specify a lifecycle transition between them. An [arrival
   audit](2026-11-17-graziani-arrivals-audit-next.md) verifies the six track
   rows and 21 corresponding OA identities, and limits errata §8.86 to the
   Tripoli/Tunisia-specific case; §20.12 supplies the general arrival-stage
   movement rule. See
   [arrival scope](2026-11-12-graziani-arrival-scope.md),
   [formation conflicts](2026-11-12-graziani-formation-setup-conflicts.md)
   and their linked review manifests.
   [Corps-artillery source audit](2026-11-17-graziani-corps-artillery-next.md)
   and [independent check](2026-11-17-graziani-corps-artillery-review.md)
   resolve the explicitly corrected XXI duplicate to one identity, distinct
   from XXVI. A separate [4/1AR source audit](2026-11-17-graziani-4-1ar-next.md)
   and [independent review](2026-11-17-graziani-4-1ar-review.md) support one
   provisional battalion identity from the OA and counter; the scenario's
   repeated text and missing punctuation remain uncorrected by the inspected
   errata. Runtime admission requires full source reconciliation.
   [Benghazi diversion analysis](2026-11-17-graziani-axis-benghazi-arrival-next.md)
   and [independent review](2026-11-17-graziani-axis-benghazi-arrival-review.md)
   establish the first ordinary Axis track entry at GT9 stage 2, outside the
   scenario, without excluding other arrival categories. The diversion's port
   capacity interaction and package frequency remain unresolved.
8. **Remaining applicable rules.** Classify each scenario-reachable turn and
   stage action against original rules and errata: organization/training,
   movement and compulsory combat, weather, patrol disclosure, rail, repair,
   supply and convoy actions, abstract air/fleet effects, and terminal timing.
   Record source-proven empty cases separately from unknowns. The 67-row
   [source register](2026-11-12-graziani-source-register.md) and
   [Land sequence crosswalk](2026-11-13-graziani-land-sequence-crosswalk.md)
   are bounded starting inventories, not proof that the full surface is known.
   [Initiative source check](2026-11-17-graziani-initiative-ratings-next.md)
   and [independent review](2026-11-17-graziani-initiative-ratings-review.md)
   establish the GT1 fixed holder, GT1–6 Commonwealth rating and conditional
   Axis chart values while retaining Rommel's separate absence rule.
   [RUL-GRA-13](2026-11-12-graziani-ruling-decisions.md#rul-gra-13-arrival-and-training-order-planning-interpretation)
   selects reviewed arrival/training sequencing for planning, while replacement
   instructor, completion and interruption consequences remain separate gates.
   [Patrol details](2026-11-17-graziani-patrol-details-next.md) and their
   [independent review](2026-11-17-graziani-patrol-details-independent.md)
   record source limits and disclosures; stage-history, dummy and loss-table
   interactions remain open.
   [Road stacking research](2026-11-17-graziani-road-stacking-next.md) and
   its [independent review](2026-11-17-graziani-road-stacking-review.md) confirm
   the Land-only road/track congestion waiver while retaining ordinary hex
   capacity and other movement restrictions.
   [Motorized supply escort and reaction](2026-11-17-graziani-supply-escort-reaction-next.md)
   and its [independent review](2026-11-17-graziani-supply-escort-reaction-review.md)
   identify a special CPA 35 restriction, a conditional general CPA 21
   restriction and size-based pinning exceptions. Escort interaction and rule
   hierarchy remain open.
   [Vehicle-marker secrecy](2026-11-17-graziani-vehicle-marker-fog-next.md)
   and its [independent review](2026-11-17-graziani-vehicle-marker-fog-review.md)
   distinguish map-counter inspection from secret contents, but leave captor
   knowledge and destroyed-marker relocation interpretation open.
   [Abandoned-vehicle source analysis](2026-11-17-graziani-abandoned-vehicle-source-next.md)
   and [independent review](2026-11-17-graziani-abandoned-vehicle-source-review.md)
   identify a detailed salt-marsh recovery procedure and competing vehicle
   categories and timing readings. Land-only application and disclosure rules
   require separate decisions.
   [Compulsory-combat source follow-up](2026-11-17-graziani-turn-obligation-followup.md)
   identifies the phasing-side ZOC obligation, source exceptions and remaining
   timing/retreat decisions; it does not establish six-turn reachability. A
   [bounded initial-contact check](2026-11-17-graziani-initial-contact-followup.md)
   finds the specifically listed C3920/C3922 stacks are not adjacent; it does
   not exclude contact from other starting placements or subsequent movement.
   A [local Halfaya edge check](2026-11-17-graziani-halfaya-edges-next.md)
   identifies the C3921/C3922 escarpment and its two-way ZOC restriction;
   [border-mark analysis](2026-11-17-graziani-border-mark-next.md) interprets
   the adjacent blue overprint as a national boundary while keeping its
   independent rule effects unverified.
   [Six-side C3921 comparison](2026-11-17-graziani-c3921-six-sides-next.md)
   and [independent review](2026-11-17-graziani-c3921-six-sides-review.md)
   corroborate two escarpments and unfinished-rail marks, while preserving
   uncertain track contact and unrecorded blue boundary details.
   A [weather schedule](2026-11-17-graziani-weather-schedule-next.md) records
   each possible stage and the selected fall-row boundaries while preserving
   startup, location-table and Land-only effect gates. An [Axis convoy lookup](2026-11-17-graziani-axis-convoy-next.md)
   records September lane thresholds and conditional six-turn inputs without
   asserting a qualifying division pair or actual bomb result. Its [independent
   check](2026-11-17-graziani-axis-convoy-review.md) confirms those cells,
   records the other lanes and flags the shell-brigade exclusion. The
   [initial division audit](2026-11-17-graziani-convoy-divisions-next.md) and
   [independent review](2026-11-17-graziani-convoy-divisions-review.md) give
   conditional source-baseline shell counts for three Commonwealth divisions;
   a separate [Australian baseline](2026-11-17-graziani-australia-shell-next.md)
   and [independent review](2026-11-17-graziani-australia-shell-review.md)
   find non-shell status under co-located default attachments. Actual setup
   choices, substitutes and convoy snapshots remain material. A [conditional
   pair analysis](2026-11-17-graziani-convoy-pair-next.md), independently
   [reviewed](2026-11-17-graziani-convoy-pair-review.md), finds the Australian
   division qualifies using 4th Indian only under stated non-shell, location and
   map-geometry assumptions; it does not determine an actual bomb column. The
   [weather source review](2026-11-17-graziani-weather-schedule-review.md)
   locates common-chart §29.7 and confirms all six foul-weather area mappings;
   region membership and Land-only effects still need admission. [Hot-weather
   fuel analysis](2026-11-17-graziani-hot-supply-next.md) and its
   [independent review](2026-11-17-graziani-hot-supply-review.md) preserve
   competing abstraction readings, loaded-holdings and RNG/privacy gates, and
   the unresolved 2.5-point example from a 50-fuel unit.
   [Rail scope](2026-11-17-graziani-rail-scope-next.md) records the Land-only
   real/dummy supply cap, initial railhead and unresolved interaction with the
   general rail movement allowance. [RUL-GRA-14](2026-11-12-graziani-ruling-decisions.md#rul-gra-14-commonwealth-rail-supply-capacity-planning-interpretation)
   selects a bounded capacity interpretation for planning while leaving the
   ZOC interaction and usable routes unadmitted.
   [Abstract fleet/AA inventory](2026-11-17-graziani-abstract-aa-fleet-next.md)
   records side-specific removals, the scenario-wide fleet attack allowance and
   remaining timing, damage and initial-deployment questions. The [independent
   review](2026-11-17-graziani-abstract-aa-fleet-review.md) corrects the Malta
   allowance to two destroyers and flags conflicting AA damage wording.
   [RUL-GRA-15](2026-11-12-graziani-ruling-decisions.md#rul-gra-15-abstract-fleet-attack-order-planning-interpretation)
   selects a reviewed post-repair attack boundary for planning; target,
   weather, damaged-ship and disclosure contracts remain unadmitted.
   [AA lifecycle analysis](2026-11-17-graziani-aa-lifecycle-next.md) and its
   [independent review](2026-11-17-graziani-aa-lifecycle-review.md) establish
   GT2 as the earliest ordinary Italian Light AA plan and GT4 as its nominal
   arrival, while retaining the monthly-limit and event-timing conflicts.
   For any new ambiguity, record source excerpts/locators and competing
   interpretations before implementation.
9. **Provenance and rights.** Establish chain of custody and permission for
   original scans, corrected formation addenda, historical-map transcription,
   derived facts and redistribution. The source register notes no authoritative
   physical formation-addenda copy was obtained; the scanned September errata
   includes relevant OA sheets but is not an independent physical verification.
   The [rights evidence status](2026-11-17-graziani-source-rights-status.md)
   records observed copyright and source descriptions, without claiming present
   ownership or permission. An [independent rights review](2026-11-17-graziani-rights-independent.md)
   identifies Decision Games as a possible contact, and a third-party Living
   Rules license lead whose underlying rights remain unverified. Identify gaps and alternatives without assuming a
   matching hash grants rights. The user will handle sending the prepared
   inquiry; no transmission or reply has been confirmed to this research
   session. See [contact status and prepared message](2026-11-17-graziani-rights-contact.md).

## Independent review queue

[Review status](2026-11-13-graziani-review-status.md) indexes 17 bounded
packets and their manifests. All 17 have attributed second entries when supplied
as explicit overrides; the default inputs still select blank templates for
some packets. Supply and trucks have 146/146 matching fields with 47 raw
locator differences, and the Matruh/NZ/formation pilot retains locator
differences. These entries are not approval of all rulings. Inspect the
manifest, current comparator and [disposition](2026-11-17-graziani-independent-reviews.md)
before assigning status.
Independent source entry, discrepancy disposition, accepted ruling and legal
rights are separate gates. Do not manufacture independence by copying existing
readings or relabeling the same scan as another edition.

**Earlier November 17 baseline:** the aggregate reviewer was run with all nine
hash-verified source packages and explicit supply/truck `reading-b.json`
overrides. It exited 1, as expected: pilot, supply and trucks retain 11, 15
and 32 raw locator differences respectively; the other 14 packets retain blank
second-entry templates. This predates the attributed overrides described above.
No source-hash validation errors occurred in that run. Counts are
packet-level status, not a measure of full source coverage or independence.

**Follow-up:** fresh-context entries for all 17 bounded packets are now
retained, including the earlier pilot. [Independent review disposition](2026-11-17-graziani-independent-reviews.md)
lists raw differences and limits. The [aggregate result](graziani-independent-review-status.json)
was run against all nine verified sources with explicit overrides; it exits 1
for unresolved raw differences and the original patrol unknown. A separate
attributed addendum check resolves that specific source question without rewriting
the original independent entry. The baseline's blank-template count is historical;
these bounded entries do not establish complete source scope or accept rulings.
The [comparison audit](2026-11-17-graziani-comparison-audit.md) enumerates all
75 field disagreements and identifies source omissions and outstanding checks;
subsequent targeted follow-ups are recorded in the independent review disposition.

## Source availability checked this session

These are local temporary discovery paths, not durable or licensed project
inputs. Reverify SHA-256 before each source-based transcription. The
[reacquisition guide](2026-11-13-graziani-review-status.md#reacquiring-source-files)
links original packages and manifests; keep scans outside Git pending rights.

| Source | Local status on November 17 | Expected SHA-256 |
|---|---|---|
| Axis charts | `/private/tmp/sandtable-axis-charts.pdf` reacquired November 17 and matched | `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca` |
| Five map sheets | `/private/tmp/sandtable-maps.pdf` reacquired November 17 and matched | `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030` |
| Commonwealth charts | `/private/tmp/sandtable-cw-charts.pdf` present and matched | `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981` |
| Scenarios | `/private/tmp/sandtable-scenarios.pdf` present and matched | `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66` |
| September errata | `/private/tmp/sandtable-cna-errata.pdf` present and matched | `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb` |
| Land rules | `/private/tmp/sandtable-land-rules.pdf` present and matched | `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` |
| Air/Logistics rules | `/private/tmp/sandtable-air-rules.pdf` present and matched | `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4` |
| Counter sheets | `/private/tmp/sandtable-counters.pdf` newly reacquired from [SPI resources](https://spigames.net/PDFv10/CNA_Counters.pdf); initial local hash recorded, no prior baseline established | `bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9` |

Common charts and compiled archival package also exist locally; compare their
hashes to the applicable manifests before use. Compiled and separate scans are
not independent editions. Axis and map PDFs were reacquired from the original
URLs in the guide; matching hashes do not establish licensing or independent
source status.

## Suggested next session

Use the reacquired sources while their temporary copies remain available. Continue
the unresolved source, geometry and rights tasks above; do not treat completed
bounded independent readings as full coverage. Implement only accepted facts
with contract/version and focused tests, isolate readiness blockers at the real
fixture, and record actual results. No runtime readiness tests were run for
this research inventory.
