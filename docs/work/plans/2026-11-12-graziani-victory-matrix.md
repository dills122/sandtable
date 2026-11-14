# Graziani six-turn victory evidence and contract questions

**Status:** one-reader transcription and analysis for contract design, not an
accepted rule or scenario fixture. Original scenario p. 28 §60.81 was visually
checked; [RUL-GRA-02](2026-11-12-graziani-ruling-decisions.md) selects that
section as the working correction for the §60.22 cross-reference. Independent
source review, exact adjudication time and supply abstraction remain open.

A [blind victory source packet](graziani-victory-review/README.md) now targets
the cross-reference and six printed categories; independent entry and the
result-policy decisions remain open.

## Condition decomposition

These are individual predicates, not an assumed ranking or an executable winner
algorithm. Place names below retain the scenario's naming; canonical map-location
IDs, control, occupation and unit eligibility still need source-backed contracts.

| Side and printed level | Location predicates | Printed supply reference | Contract questions |
| --- | --- | --- | --- |
| Italian strategic | Hold either Alexandria or Cairo, any hex, with a combat unit. | That unit can be supplied by convoy from any hex on map D. | Meaning of a qualifying convoy and route after Land-only abstraction; area-to-hex mapping; whether “any hex” denotes a selectable qualifying origin or requires a stronger condition. |
| Italian decisive | Take and hold both Mersa Matruh and Siwa with a combat unit. | Supplied by convoy from Tobruk. | Whether wording requires one or a qualifying unit at each location; origin, route and control. |
| Italian tactical | Take and hold Sidi Barrani; retain Sollum, Fort Maddalena and Giarabub. | In supply, as above, to Tobruk. | Scope of supply requirement across locations, retain versus occupy, and direction of trace. |
| Commonwealth tactical | Take and hold Sollum, Halfaya Pass and Siwa. | Trace supply by truck convoy back to Alexandria or Cairo. | Supply subject and timing; truck convoy under §32 abstraction. |
| Commonwealth decisive | Take and hold Bardia, Fort Maddalena, Sidi Omar, Sollum and Siwa. | Supply as above. | Whether all locations and eligible units must satisfy trace; no explicit Halfaya requirement in this row. |
| Commonwealth strategic | Take and hold Tobruk. | Supply as above. | No explicit other location requirement in this row; port and route changes. |

The original spelling `Sidi Birani` in §60.81 is apparently the same location as
`Sidi Barrani` in the scenario's location list, C4131; treat this as a candidate
normalization requiring independent map/source check, not silent content admission.
The same source identifies Sollum C4021, Halfaya Pass C3922, Siwa C0127 and
Fort Maddalena C3019. Location identity alone does not settle unit or possession
rules. The §60.82 points table belongs to the different, longer Italian Campaign;
it is not a fallback tie-breaker for §60.81.

The original Air/Logistics package's summary of important locations (PDF p. 26)
lists Halfaya Pass as C3922/C3821, while §60.81 specifically prints C3922. It
also lists Alexandria as E3613/E3714 and Cairo as five hexes. Resolve whether
the broader place identity or a scenario-specific hex controls each condition;
do not automatically enlarge a printed single-hex victory objective. The same
summary and its land-distance chart (PDF p. 25) list Benghazi as A4827, while
§60.92(B)'s initial supply table prints B4827. This is a separate setup-location
conflict; it cannot be fixed by choosing whichever spelling or coordinate is
convenient. The [map pilot](2026-11-12-graziani-map-identity-pilot.md) now
records an original Map A check, and RUL-GRA-05 selects A4827 for planning;
independent review and versioned acceptance remain open.

### Overlap counterexamples to test after semantics are accepted

- The Commonwealth decisive row requires Bardia, Fort Maddalena, Sidi Omar,
  Sollum and Siwa; the tactical row additionally names Halfaya Pass. A synthetic
  predicate input with the decisive locations and required supply but without
  Halfaya can satisfy the printed decisive location list without satisfying the
  tactical location list. A result algorithm must not require every lower label
  as a prerequisite unless an explicit new ruling adds that requirement.
- Italian strategic requires one of Alexandria/Cairo; decisive instead names
  both Mersa Matruh and Siwa. A synthetic input can satisfy one location list
  without automatically satisfying the other. Supply and combat-unit predicates
  must be checked separately in either direction.
- Italian tactical and Commonwealth tactical/decisive each mention Sollum. If
  accepted hold/possession semantics are exclusive for that single hex, those
  particular combinations cannot both satisfy their location predicates. That
  conditional observation does not prove all cross-side victories exclusive or
  decide what happens when other categories overlap.

These are logical test inputs under stated assumptions, not demonstrations that
the corresponding board positions or qualifying supply routes are reachable in
six turns. Do not infer a global ranking or no-winner policy from them.

The Land §3.1 glossary and §3.23 describe combat-unit classification with
situation-specific qualifications for some HQs, engineers and dummy formations.
This narrows the source questions for the Italian rows that explicitly name a
combat unit, but does not prove that every such unit qualifies for an objective
or that the Commonwealth rows have identical wording. Resolve each predicate
against the detailed cases and the accepted scenario policy.

## Required decision boundaries

1. **Snapshot and terminal timing:** identify the exact state after turn 6, stage 3,
   including whether all cleanup has occurred before the verdict. Prevent any
   later game-state mutation after terminalization. Do not infer timing from a
   fixture that only covers stage entry.
2. **Per-predicate semantics:** specify take, hold, retain, friendly control,
   required combat-unit presence, multi-hex locations and supply scope. A player
   may hold an objective but fail its qualifying supply condition.
3. **Supply abstraction:** select a source-backed, versioned way to represent
   §60.81's convoy trace using §59.6/§32. A local §32.16 supply draw is not
   automatically equivalent. Define origins, connectivity, enemy interference,
   timing and checkpoint/replay; current sources do not settle these here.
4. **Result policy:** evaluate all six predicates before applying a separately
   accepted policy for overlap, both sides qualifying and no match. Row order and
   labels do not themselves establish precedence or mutual exclusivity.
5. **Evidence:** record qualifying locations and route/supply evidence in a
   deterministic result event. Do not expose opposing hidden state through player
   views; keep authority and view filtering separate.

## Contract vectors before acceptance

- Six positive vectors: one for each listed level, built only after its location,
  combat-unit and supply semantics are accepted. Preserve every matched predicate
  separately even if a later policy chooses one displayed outcome.
- For each row, remove each required location or qualifying supply in turn; the
  corresponding predicate must fail. Vary multi-hex and wrong-unit cases where
  applicable. Do not impose unprinted locations from adjacent rows.
- Test a location held without the required trace; a trace without the required
  location; a broken or enemy-interfered route; and a route changing at the
  accepted boundary. Supply examples must use the adopted abstraction.
- For Italian strategic supply, compare one valid map-D origin with other
  disconnected or enemy-controlled map-D hexes. Resolve the source's “any hex”
  wording before choosing an existential or stronger route predicate; do not
  treat a single synthetic route as proof of the quantifier or reachability.
- Test overlaps within one side, both sides qualifying, neither side qualifying,
  and a result-policy version change. These are policy inputs; the source matrix
  alone does not promise that every combination is reachable in real play.
- Test before/at/after terminal boundary, checkpoint restoration, deterministic
  replay and idempotent terminalization. Verify player-facing redaction separately.

All vectors are design obligations, not passing tests. Formation provenance,
setup placement, map identity, supply decisions and result policy remain gates
before a six-turn playable release can claim historical victory adjudication.
