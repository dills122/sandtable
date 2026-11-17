# Graziani victory routes: next provisional policy

**Status: proposal for review, not an admitted ruling or runtime contract.**
`gra-victory-route-v2-proposed` makes the choices left open by the
[v1 candidate](2026-11-17-graziani-victory-route-followup.md) and its
[independent review](2026-11-17-graziani-victory-route-review.md). It does not
change source facts, readiness, saved games or the fail-closed victory gate.
Adopt only as a versioned package after review of its prerequisites. Its choices
can change winners; none is presented as an official correction.

The [independent policy review](2026-11-17-graziani-victory-policy-review.md)
found a publication-timing leak and underspecified binding of location, unit
and route witnesses. Publication below is a candidate, not an authorized
disclosure: a pending-versus-published distinction can itself reveal whether
all rows are decidable. A future contract must settle this audience-specific
leak and bind each witness tuple to the same objective and snapshot before
admission; identical pending messages alone do not prevent the timing leak.

## Evidence boundary

Rechecked the original scenario scan, PDF p. 4 / printed p. 28 §60.81, and
Land scan p. 43 §32.16 and §§32.32–.33 visually. Their SHA-256 hashes match
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`
and `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
The scenario distinguishes Italian supply **from** map D or Tobruk from the
Commonwealth trace **back to** Alexandria or Cairo. Its tactical Italian row
also distinguishes taking Sidi Birani from retaining the other places. Land
§32.16 measures local draw, not this victory trace, and says enemy ZOC
unoccupied by friendly units blocks that draw. Sections 32.32–.33 retain
motorization and convoy movement. The September errata copy hash matches
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`;
the [prior review](2026-11-17-graziani-victory-independent-decision.md) reports
no §60.81 or §60.22 correction in its four-page text search. That is not an
exhaustive review of every related rule or edition. These are the same source
scans, not an independent edition. The reference correction in RUL-GRA-02 and
all choices below are project interpretations.

## Proposed contract

Evaluate all rows at one immutable snapshot after turn 6 OpStage 3 and its
accepted cleanup, once terminal ordering is defined. Use hypothetical
medium-truck connectivity: no actual shipment, available trucks, motorization,
stock, local supply-unit draw, CP budget or route-length limit. This is a
practical abstraction, not an inference that convoy movement is absent. Use
terminal weather and certified directional terrain and hexside traversability;
road crossings require certified connecting edges. Apply the Commonwealth
western movement limit to route vertices as a deliberate extrapolation from
Land §8.19. Do not import §60.82's longer-campaign trace length or combat-unit
definition.

For each entry below, `route(a,b)` means a directed route from `a` to `b`,
not merely connectivity in an undirected graph. A location denotes its
scenario-specific eligible hex set under accepted identity rules. Possession,
take/hold and retain are separate inputs, including any required history; do
not equate them with a route. A subject is the qualifying occupied hex for an
explicit combat-unit condition, otherwise an eligible hex satisfying the row's
separately defined take/hold or retain predicate. A location succeeds if at
least one such subject hex has the required route. Do not require every unit or
hex at a multi-hex location to trace.

| Row | Proposed location and supply quantifiers |
| --- | --- |
| Italian strategic | Existentially choose Alexandria **or** Cairo, a qualifying Italian combat unit and its objective hex, and an eligible map-D hex `d`; require `route(d, unit hex)`. |
| Italian decisive | Both Matruh and Siwa must satisfy take/hold; at **each**, existentially choose a qualifying Italian combat unit and its hex and require `route(Tobruk, unit hex)`. The two choices may differ. |
| Italian tactical | Sidi Birani must satisfy take/hold; Sollum, Fort Maddalena and Giarabub must satisfy retain. Require a route from Tobruk to a subject at **each** of all four. No additional combat-unit occupancy follows solely from this route policy. |
| Commonwealth tactical | Take/hold each of Sollum, Halfaya Pass C3922 and Siwa C0127. Require a route from a subject at **each** to an eligible Alexandria or Cairo source. |
| Commonwealth decisive | Take/hold each of Bardia, Fort Maddalena, Sidi Omar, Sollum and Siwa. Require a route from a subject at **each** to an eligible Alexandria or Cairo source. Do not add Halfaya. |
| Commonwealth strategic | Take/hold Tobruk and require a route from its subject to an eligible Alexandria or Cairo source. Do not add other rows' locations. |

For every Commonwealth location, choose Alexandria or Cairo independently;
no common source is required. Italian map D “any hex” is existential, not
universal. Each named source is a certified hex in the accepted city set,
controlled by the tracing side; map-D sources need no ownership test. For
Tobruk and Alexandria/Cairo choose one eligible source hex per route; no route
is allowed through a blocked source. These source-control and quantifier choices
are additions, and city identity and control are separately gated inputs.

The Italian direction models delivery from the stated origin; Commonwealth
direction models the stated trace back. Neither implies a return journey. For a
synthetic one-way edge `source -> subject`, Italian route succeeds and the
reverse-only edge fails; for Commonwealth the reverse is true. An actual
round-trip policy would require both directions and can fail in either case.
No claim is made that a real source-chart crossing has one-way *passability*;
verify that independently rather than infer it from asymmetric cost.

Every vertex, including both endpoints, must be certified free of enemy units.
Enemy occupancy blocks even in a mixed stack. Enemy ZOC blocks unless the hex
contains a friendly **combat** unit; supply units, dummies and markers alone do
not cancel it. This narrows §32.16's friendly-unit wording by policy and is not
a transcription of that rule. A zero-edge path succeeds only if its one hex is
both an eligible subject and source and passes all endpoint and incoming-ZOC
checks. Enemy control alone does not block an intermediate hex unless another
accepted traversal rule says so; named city source control is an extra condition.

## Three-valued proof and publication

Evaluate required conjunctions in three-valued logic: a known false conjunct
makes the row `no-match`; otherwise any unknown makes it `unknown`. For each
existential choice a certified witness makes it true; if every candidate is
proved false it is false; otherwise it is unknown. A witness requires certified
subject and source eligibility, every directed edge and applicable weather,
occupancy, and complete incoming enemy-ZOC coverage for every vertex on the
same snapshot. A valid witness survives unrelated unknown seams. A seemingly
empty path adjacent to an unexplored region does not prove absence of incoming
ZOC. Never normalize unknown to clear, blocked or outside the board.

A negative route result requires a finite exhaustion certificate covering all
potential eligible sources and subjects, all possible continuation space,
external boundaries and incoming ZOC, with every possible route excluded by a
known blocker. An unresolved seam, city membership, weather, source control or
outside continuation that could change the answer produces `unknown`. If finite
coverage cannot be established, no-path remains unknown. A known false
possession conjunct can still determine a row despite unknown routes.

Keep detailed row results, paths, candidate sources, blockers, evidence and
stable identifiers in authoritative restricted storage. Until an authorized
terminal result is publishable, expose no victory evaluation, progress, reason,
path, length, candidate identity, timing or identifier to either player or the
intelligence plane. In particular, unresolved versus no-match is not a
player-visible diagnostic. After all six rows are known, apply the proposed
[result policy](2026-11-17-graziani-victory-independent-decision.md): publish
only the terminal outcome and qualifying side/level labels to both players;
this is an explicit terminal disclosure, not proof that it reveals no hidden
state. No witness or individual row diagnostics are published. If any row is
unknown, keep the authoritative game pending for adjudication; expose the same
nonterminal interface regardless of which row or hidden blocker caused it.
Operational access to missing-evidence diagnostics must be restricted separately.
Bind route, result, map, control and snapshot versions to the verdict and replay;
never silently reinterpret an existing save.

## Distinguishing vectors and admission gates

These are proposed expected outcomes, not executed tests or reachable-history
claims:

- One decisive Italian location connected and one blocked: row `no-match`;
  an unknown second route instead yields `unknown`. One strategic map-D origin
  works despite another blocked or unknown: route true. A supplied Sidi Birani
  with retained but disconnected Giarabub fails tactical under this proposal;
  the narrower reading of “in supply” could differ.
- Commonwealth objectives connected to different eligible cities satisfy the
  independent-source choice; one disconnected from both fails. A single common
  source rule could fail the former. Known false possession dominates an
  unknown route; one valid existential witness dominates other unknown sources.
- Friendly supply unit alone in enemy ZOC blocks under this policy, while the
  broader §32.16 analogy might allow it. A hostile-controlled named city is
  ineligible; an otherwise clear uncontrolled map-D origin remains eligible.
  A valid zero-edge case succeeds, while an obstructed endpoint fails. A route
  west of Marble Arch fails for Commonwealth even if otherwise passable.
- An unknown incoming ZOC on a candidate path makes that witness unknown;
  an independent fully certified path proves success. No known path plus a
  potentially useful unknown outside seam remains unknown; fully certified
  blocked alternatives give false. Test source-to-subject and inverse one-way
  synthetic edges, and separately compare a round-trip policy.
- Compare local draw without named-source connectivity, connectivity without
  nearby supply or transport, and a path beyond one stage: proposed outcomes
  follow connectivity, while actual-shipment and local-draw alternatives differ.
  Test terminal cleanup, all six known/unknown combinations, simultaneous and
  no-match outcomes, checkpoint replay and old-version isolation. Equal
  authorized histories differing only in hidden units or dummy identity must
  expose identical pre-publication events, order, timing and metadata; separately
  test the expressly authorized final outcome disclosure.

Before admission: independently review row interpretations and direction;
resolve subject, combat-unit, take/hold/retain, city identity and control
contracts; certify traversability, incoming ZOC and finite route-space coverage;
settle weather, western limit and terminal cleanup; review terminal disclosure;
version contracts before implementation. Source ambiguities remain: “any” on
map D, scope of tactical supply, decisive unit quantifier, common versus separate
Commonwealth sources, actual versus hypothetical convoy, blocker classes and
route length. None is resolved by a passing synthetic test. Affected work spans
scenario and map contracts, Core victory/state, Host replay and view/privacy
tests. No gameplay tests or runtime changes were made for this proposal.
