# Graziani victory route v2: independent policy review

**Disposition:** retain `gra-victory-route-v2-proposed` for revision and
review; do not admit as an executable or privacy-complete contract. The six
printed objective lists and the candidate's explicit direction choices agree
with this bounded reading. Its row quantifiers, route abstraction and result
composition are project interpretations, not source corrections. The
publication timing gap below needs an explicit audience decision before
admission. This review uses the same scan edition, not an independent edition.

## Findings

1. **Publication timing leaks the all-known gate.** The candidate publishes
   immediately when all six rows are known and otherwise keeps the game pending.
   Identical pending interfaces hide *which* row is unknown, but observers can
   distinguish an all-known state from an unresolved state by whether and when
   a verdict appears. If route knowledge or coverage depends on hidden state,
   this discloses at least that distinction in addition to the expressly
   authorized final outcome. The candidate says no timing is exposed and also
   demands identical timing for equal authorized histories; those claims cannot
   hold across an immediate-publication versus unresolved pair. Specify whether
   terminal availability/timing itself is authorized disclosure, to whom, and
   whether the intelligence plane learns anything. Alternatively design a
   release/adjudication process whose externally visible behavior is independent
   of secret-dependent completion; a fixed time alone still leaks if only the
   known state publishes then. Test paired histories where one has six known
   rows and the other has an unknown hidden route, including terminal response,
   polling, time, event ordering and later evidence repair. Retaining the same
   unresolved message for every cause remains useful but does not close this
   distinction.

2. **Bind location, unit and route witnesses explicitly.** For strategic
   Italian victory, §60.81 requires a combat unit taking and holding an eligible
   Alexandria or Cairo hex that can be supplied from map D. The candidate's
   prose identifies a qualifying occupied subject hex, but its table separately
   says take/hold and choose a unit and objective hex without explicitly binding
   the selected unit to the selected hex satisfying that location's take/hold
   predicate. Define a single witness tuple `(objective, subject hex, unit,
   source, path)` and require all predicates on the same snapshot and tuple.
   Apply the same binding to each decisive Italian location, and link each
   tactical/Commonwealth subject to its own take/hold or retain predicate. A
   route from a different hex, or a combat unit elsewhere in a multi-hex city,
   cannot complete an otherwise split witness. Preserve three-valued evaluation
   when a candidate's membership or history is unknown. This tightens the
   candidate's apparent intent; it does not decide the still-open meaning of
   take/hold, retain, or a location's eligible hex set.

3. **Qualify false-route vectors and proof inputs.** “One decisive Italian
   location connected and one blocked” proves `no-match` only if *all*
   eligible subject/source/path combinations for the blocked location are
   certified false, or another required conjunct is known false. A single
   blocked path or subject is not a negative proof. Conversely, one fully
   certified witness establishes its existential route even if another path or
   seam is unknown. Add explicit test data for one blocked candidate plus an
   unknown alternate (`unknown`), a known alternate (`true`), and complete
   exhaustion (`false`). Include unknown membership of possible source/subject
   candidates and unknown incoming ZOC, not merely unknown edges. The draft's
   three-valued rules otherwise state the correct false-dominates-conjunction
   and true-dominates-existential behavior.

4. **Endpoint and external-space policies need implementable proof rules.**
   The candidate requires every vertex to pass occupancy and incoming-ZOC
   checks, including map-D and named-city source endpoints, and allows a
   zero-edge path. That is internally coherent only when both endpoint roles,
   directional traversal and source-control checks are applied to the *same*
   hex in the zero-edge case. Define how an admitted finite search domain
   certifies all potential continuations and incoming ZOC across map boundaries;
   an absent edge or outside unit must not be assumed nonexistent. A valid
   path may succeed despite unrelated unknown areas, whereas a negative search
   cannot become false simply because the loaded graph is exhausted. The
   candidate identifies these gates; the proof format and map contract remain
   open, so its vectors do not yet constitute executable acceptance criteria.

5. **Keep policy choices separable from source facts.** The inspected source
   does not settle hypothetical versus actual convoy, unlimited range,
   directional route, per-objective supply, source control, combat-unit-only
   ZOC neutralization, enemy non-combat blockers, weather or extension of the
   western movement restriction. The candidate identifies these as choices;
   acceptance should bind each to the versioned ruleset, record alternative
   outcomes, and preserve the existing fail-closed gate until map, control,
   history, terminal boundary and transport semantics are admitted. In
   particular, §32.16's friendly-*unit* wording is broader than this candidate's
   friendly-*combat-unit* rule and concerns local draw rather than a victory
   trace. No claim of historical correctness follows from the candidate's
   stricter ZOC condition.

## Source check and limits

Visually inspected [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 4 / printed p. 28 §60.81 and its adjoining §60.82; the local scan SHA-256
is `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
The Italian strategic row names either Alexandria or Cairo, a combat unit and
supply from any map-D hex; decisive names both Matruh and Siwa and supply from
Tobruk; tactical distinguishes Sidi Birani from retaining the other three
places. Commonwealth lists tactical Sollum, Halfaya and Siwa; decisive Bardia,
Fort Maddalena, Sidi Omar, Sollum and Siwa; strategic Tobruk. The last two
refer to supply as above. Section 60.82 is a separate longer-campaign condition.
These facts do not settle the candidate's existential, per-location or
directional interpretations.

Compared [v2 candidate](2026-11-17-graziani-victory-policy-next.md),
[v1 route review](2026-11-17-graziani-victory-route-review.md), and
[result proposal](2026-11-17-graziani-victory-independent-decision.md).
Land source comparison here relies on those cited reviews; no full independent
terrain, errata, map, control or reachability audit was performed. Findings
are review obligations, not executed gameplay tests. No runtime, content,
readiness, or other person's files changed.
