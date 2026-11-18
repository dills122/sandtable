# Graziani high-impact policy proposals

**Status: November 17, 2026.** Recommended project planning choices for
Land-only Graziani, informed by the linked independent reviews. These are not
official errata, decisions already entered in the ruling ledger, executable
contracts, reachability proofs, or readiness changes. Each choice is bounded:
admission requires the gates below, and an unsupported or unknown case must
not silently become a legal action, zero loss, or a negative victory finding.
All cited independent reviews use the same source editions as their candidates.

## Convoy 5%: bounded group ceiling

**Recommend** selecting `gra-axis-convoy-5pct-v1-proposed` for planning, for
one already-established attacked Axis convoy whose result is 5%. Apply
`ceil(count / 20)` separately to each nonempty accepted group, capped by the
carried quantity. Supported groups: real Supply Units; Motorization Points;
and Italian tanks, German tanks, Italian guns, and German guns as four
nationality-and-class replacement groups, pooling models inside each group.
Empty groups contribute zero. Other cargo, uncertain classification or dummy
status remains unsupported. Multiple model allocations remain pending until a
separate chooser rule is accepted; a singleton allocation needs no chooser.
This is an analogy to detailed Air rounding combined with Land's abstract
cargo and nationality distinction, not a claim that Air rules govern Land.

**Consequences and alternatives.** One Supply Unit plus one Motorization
Point loses both under this choice; the printed Land 10% branch excludes
Supply Units and rounds motorization differently, so 5% can be more destructive
than 10%. This non-monotonic result is a deliberate, reviewable consequence,
not historical evidence. A floor or no-loss policy avoids that example but
can erase 5% entirely; combining equipment groups changes small-load losses;
extending Land's 10% exception to 5% changes which cargo is affected. None is
explicitly specified for 5%. Decline this recommendation if monotonic outcomes
are a required design constraint. Land's 20%-or-greater branch includes 25%;
the selected 5% treatment does not modify 10% or 20%+.

**Admission blockers.** Establish convoy legality, an immutable cargo and lane
snapshot, cargo classification and dummy treatment, legal model selection and
authority, qualifying divisions, threshold orientation and equality, map
geometry, ordered dice and timing. Establish atomic draw/loss/delivery,
durable result and RNG cursor, idempotent retry and side-safe responses and
timing before effects; never reroll or deliver a partially adjudicated load.
Unknown cargo is not an empty group. A printed 5% cell is not evidence of a
reachable six-turn convoy. Synthetic checks must distinguish ceiling from
floor and pooled groups, 5% from 10% and 25%, unknown from empty, and replay
after a draw. No such tests are reported as run here.

**Evidence.** [5% proposal and arithmetic](2026-11-17-graziani-five-percent-next.md),
[mode comparison](2026-11-17-graziani-convoy-mode-rounding.md), and
[independent reachability review](2026-11-17-graziani-five-percent-reachability-review.md).
The reviews confirm the printed 5% cell and the missing Land branch, not a
source-selected grouping, rounding or actual legal load.

## Rail: movement effect and occupancy

**Recommend** a versioned planning interpretation applying §10.26's friendly
combat-unit negation of enemy ZOC effects to §8.73 rail movement. At origin,
the traveling combat unit itself may satisfy presence; at each entered hex,
require a separate friendly combat unit already present before entry. A
non-combat traveler cannot supply its own exception. These origin and entry
choices reflect literal presence and avoid circular entry; the entry timing is
a project choice, not a rail rule explicitly imported from §10.29.

**Consequences and alternatives.** A combat traveler may start in enemy ZOC
without another friendly unit, while a non-combat traveler needs one; neither
may enter a ZOC hex solely by arriving there. A stricter rail-specific reading
bars the move despite friendly combat presence; requiring another unit at
origin or allowing the traveler to self-neutralize on entry changes outcomes.
Section 8.73's separate enemy-occupation ban remains, as do destroyed or
unbuilt track, connectivity, pickup/dropoff, stacking and the Axis-occupied
rail restriction. Negating a movement effect does not erase ZOC or control:
§24.65 separately bars rail construction or rebuilding in an enemy-controlled
or enemy-occupied hex.

**Admission blockers.** Define occupancy snapshot and simultaneous movement,
combat-unit classification, full directional rail and map coverage, enemy ZOC
projection, stage-wide CP and reorganization limits including later CP
expenditure, and pickup/dropoff. Decide §10.16's applicability to rail and
audience-safe legality, event, rejection and timing behavior. Unknown track,
incoming ZOC or occupant status is not clear. Test origin, intermediate and
destination for no occupant, separate friendly combat occupant and non-combat
occupant, both traveler types, enemy occupation, construction and replay;
these are proposed synthetic checks, not reachability or passing tests.

**Evidence.** [Rail interaction](2026-11-17-graziani-rules-rail-zoc.md) and
[independent source review](2026-11-17-graziani-rules-rail-zoc-review.md).
Both identify competing precedence readings; neither declares self-occupancy
or entry timing source-certain.

## Victory: witness and proof boundaries

**Recommend** retaining existing RUL-GRA-07 result composition and
RUL-GRA-22's one-city-hex, same-holder Italian strategic choice as planning
decisions. For any future existential route evaluation, bind objective,
eligible subject hex, qualifying unit where required, relevant possession or
history, source and directed path to one immutable terminal snapshot. A
certified witness proves its existential predicate even if unrelated options
are unknown; a blocked path proves false only when every eligible candidate
and continuation is certified false. In conjunction, a known false required
predicate determines no-match; otherwise missing required evidence remains
unknown. These are proof semantics, not an admitted route algorithm or
permission to publish a result.

**Consequences and alternatives.** A unit elsewhere in Alexandria cannot
complete another hex's holder-and-supply witness; a blocked path plus an
unknown alternate stays unknown. Alternatives include a Cairo-only scope for
the parenthesis, requiring all city hexes, separate holder and supply
witnesses, and different source, direction or route rules. The selected
strategic condition is capability to be supplied, not proof of an actual
shipment. Commonwealth source wording has its own unresolved quantifiers.
The proposed route-v2 choices, including hypothetical unlimited truck
connectivity, are not selected wholesale by this recommendation.

**Admission blockers.** Set terminal order and cleanup; city membership,
control, take/hold and retain history and substitution; source membership and
control; route directions, traversability, ZOC and occupancy, endpoint and
zero-edge semantics; and finite-domain and incoming-boundary completeness
proofs. Define ruleset identity, replay, evidence correction and terminal
state freeze. Most importantly, authorize outcome availability and timing for
each audience, including the intelligence plane, or define a protocol whose
observable behavior does not depend on secret-dependent completion. Identical
pending text alone does not prevent a published-versus-pending leak, and a
fixed deadline alone does not fix conditional publication. Paired-history
checks must include polling, events, later actions and evidence repair. No
route, terminal protocol, runtime change or gameplay test is admitted here.

**Evidence.** [Independent victory policy review](2026-11-17-graziani-victory-policy-review.md),
[independent city scope review](2026-11-17-graziani-victory-city-hex-scope-review.md),
and [existing bounded decisions](2026-11-12-graziani-ruling-decisions.md).
The reviews support witness binding and three-valued proof discipline while
identifying disclosure and route gaps; they do not approve route-v2 admission.
