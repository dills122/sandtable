# Graziani victory route candidate: independent review

**Disposition:** Retain `gra-victory-route-v1` as an unadmitted candidate; do
not select it automatically. Its row-specific subjects and route rules are
explicit project interpretations, not a source-complete victory algorithm.
Resolve direction, quantified proof, privacy and the policy questions below
before treating its vectors as expected gameplay results. This review inspects
the same source edition, not an independent edition or official clarification.

## Findings

1. **Printed subjects and quantifiers remain ambiguous.** Scenario §60.81
   strategic Italian victory requires holding either Alexandria or Cairo, any
   hex, with a combat unit that can be supplied by convoy from any map-D hex.
   Existential choice of source is reasonable but an interpretation of “any”;
   the matrix correctly retains that question. Decisive Italian victory names
   *both* Matruh and Siwa and says “with any combat unit” that can be supplied
   from Tobruk; the proposed unit and route at each location is stricter than a
   possible single-subject reading. Tactical Italian text distinguishes taking
   and holding Sidi Birani from retaining possession of three other places; it
   does not explicitly say each must independently trace. Commonwealth tactical
   text lists three locations and a truck-convoy trace to Alexandria or Cairo;
   decisive and strategic say supply “as above.” A route from each location,
   separate source choice, and particular combat-occupancy requirements are
   additional choices. The candidate acknowledges these, but admission should
   version each row's possession and supply quantifiers rather than treating
   the route graph as settling them. Preserve the strategic `or`, decisive
   `both`, tactical retain distinction, and the separate Commonwealth lists;
   never import §60.82's longer-campaign combat-unit definition by default.

2. **Traversal direction is underspecified for directed edges.** The proposed
   subjects section asks for a route *from each objective to* Tobruk or a map-D
   source; the printed Italian strategic/decisive conditions say supply *from*
   the source, while Commonwealth tactical says trace *back to* its source.
   Item 4 allows reverse traversal only where truck rules permit, but does not
   specify which way directed edges are tested, nor whether a trace back proves
   outbound delivery or an actual round trip. Land §8.35 distinguishes up and
   down escarpments, and §§8.31–.33 make terrain and crossings material. Even
   if a particular crossing is merely more costly in one direction and the
   candidate ignores cost, prove direction-dependent **passability** from the
   applicable chart rather than assuming it. Add a synthetic one-way graph with
   objective-to-source possible but source-to-objective impossible, the inverse,
   and a separately specified return-trip policy. Decide and version direction
   independently for each printed phrasing; do not claim source mandates either.

3. **Positive witnesses and negative proofs need explicit completeness.** A
   certified path is a positive witness only when every edge's direction and
   terrain/weather status, each vertex's enemy occupancy and all relevant
   *incoming* enemy ZOC, endpoints and existential source eligibility are
   established on the accepted snapshot. A path through apparently empty hexes
   next to an uncertified outside region is not a valid witness merely because
   the path hexes themselves are known. Conversely, an irrelevant unknown seam
   cannot invalidate a fully certified witness. `no-match` requires a sound finite
   exhaustion proof over *all* potentially eligible sources and continuation
   space, including uncertified areas or external boundaries, not exhaustion of
   the currently loaded graph. Uncertain named-city membership or source control,
   map-D membership, source blockers or any potentially useful unknown edge
   must yield `unknown` unless a different certain witness or independently
   false conjunct determines the predicate. Unknown enemy-ZOC projection or
   impassability must never be normalized to clear, blocked or absent. Test
   witness with unknown incoming ZOC, valid alternate witness, no witness with
   unknown outside route, all sources certified blocked, and known-false
   possession with unknown route. For a conjunction, known false dominates
   unknown; for existential source choice, one known true dominates unknown,
   while all known false plus any possible unknown remains unknown.

4. **Privacy must cover result and proof metadata, not just blocker identity.**
   The authoritative search must use complete state, but an opponent-facing
   `no-match`, `unknown`, route witness, blocker reason, source identity,
   route length, progress timing or stable event identifier may disclose a
   hidden unit, dummy, ZOC, controlled location or coverage fact. A terminal
   verdict or its publication timing may itself be informative; decide what
   each audience may learn and when, including unresolved-result behavior.
   Record detailed witness/reasons in authoritative evidence and project only
   policy-authorized information to each player and the intelligence plane.
   Test equal-authorized histories with hidden blockers/dummies, including
   event ordering, reason codes and witness metadata; also test any explicitly
   permitted terminal disclosure. The result proposal's all-six-rows-known gate
   is a separate conservative publication policy, not source text. A known
   winning row must not silently override an unknown row under that proposal.

5. **Several concrete policies need impact decisions, not only citation.**
   Candidate choices include controlled named city sources; no control required
   for map-D sources; endpoint obstruction and zero-edge routes; any enemy unit
   as an occupancy blocker; *combat-unit-only* friendly ZOC neutralization;
   terminal-weather truck traversability; infinite route length; no truck,
   stock or actual shipment; and extending §8.19's Commonwealth Land movement
   restriction to hypothetical route vertices. §32.16's exception says friendly
   **units**, not friendly combat units; §8.19 governs Commonwealth Land unit
   movement, not expressly an abstract trace. These choices can change winners
   and some narrow or loosen different alternatives, so “conservative” does
   not imply historically correct or universally more restrictive. Review
   counterexamples for each choice, especially a friendly supply unit in enemy
   ZOC, a hostile-controlled source, a map-D source with unknown ownership,
   an otherwise valid zero-edge route, and a trace west of Marble Arch.
   §32.58's CPA 15 applies to motorization transporting a supply unit; rejecting
   that limit for a hypothetical route is a choice, not proof no limit applies
   to the scenario's intended convoy. Possession, city control, transport,
   weather and terminal cleanup must each have accepted versioned inputs.

## Source evidence and scope

Visually inspected hash-verified originals:

- [Scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
  PDF p. 4 / printed p. 28, §§60.81–.82 and §60.92, and PDF p. 3 / printed
  p. 27, §60.22. SHA-256
  `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
  §60.22 ends the six-turn scenario at the conclusion of turn 6, OpStage 3;
  its §60.71 reference conflicts with the named §60.81 heading. RUL-GRA-02 is
  a working interpretation, not a printed correction.
- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF and
  printed pp. 12, 14, 43–44, especially §§5.2, 8.13–.19, 8.31–.35,
  8.41, 32.16, 32.32–.33 and 32.58. SHA-256
  `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
  §32.16 specifies local draw, not a full §60.81 convoy trace.

Compared the candidate with the six rows and counterexamples in the existing
[victory matrix](2026-11-12-graziani-victory-matrix.md) and the tri-state
[result proposal](2026-11-17-graziani-victory-independent-decision.md). This
bounded review did not recheck all terrain charts, source-control rules, errata,
map extent, discovery rules or six-turn reachability. Proposed tests are design
obligations, not executed results; no runtime, readiness or content changes.
