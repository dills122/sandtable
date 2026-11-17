# Land-only road and track stacking: source check

**Finding:** Land-only §32.9 removes Section 9's road/track stacking-point
restrictions. It does not remove terrain-based hex capacity, other movement
restrictions, or the need for a connecting road or track. This is a mode-wide
rule, not evidence that a particular opening movement exceeds five points or
is legal.

## Sources and scope

- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
  printed/PDF p. 43 §32.0 and p. 45 §32.9, SHA-256
  `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
  §32.0 applies when Land is played without both Air and Logistics; check the
  actual selected game mode before applying this result. §32.9 says to ignore
  road and track **only** stacking-point restrictions listed in §9.0.
- Same rules, p. 17 §§9.14, 9.29, 9.3–9.35; pp. 14–15 §§8.31–8.35,
  8.41–8.42; and p. 18 §10.21. Original
  [map/terrain chart](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 1,
  SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`,
  terrain-effects chart and notes 6–11.
- [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), PDF p. 1,
  SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
  Its §8.37 correction says tracks halve terrain cost as chart note 8 specifies;
  the fixed one-CP track entry is wrong. It also corrects a §9.16 label; neither
  correction makes hex stacking generally disappear. §32.0 errata cautions
  that abstract rules were not tested thoroughly.

## Consequences and boundaries

- §9.33's five-point road/track restriction on a motorized unit moving through
  friendly units, including its road-space count for a unit ending movement
  after road entry, is ignored in this mode. Its congestion-based instruction
  to move around does not independently recreate the waived limit. §9.34 is
  not expressly deleted; its prohibition on breaking up a unit *to evade that
  road/track limit* cannot by itself recreate the waived limit. This does not give a blanket right to split,
  detach, or recombine formations: independently applicable rules still govern.
- §9.14 and §9.3 retain terrain-dependent hex stacking at the stated movement
  and Operations Stage checkpoints. §9.31 says units may never cease movement
  in a hex in violation; §9.32 permits moving through friendly units subject to
  its exceptions. §9.35 addresses involuntary retreats and excess units. Use
  the chart's terrain and any applicable exceptions; do not replace hex capacity
  with an unlimited capacity. §9.29 has specific attached-truck and convoy
  treatment; waiving its road-space constraint does not itself rewrite its
  distinct hex-stacking exception or establish physical convoys in Land-only
  play.
- §8.33 requires a road/track connection through the shared side for the
  road/track movement benefit. §8.41 and chart note 10 still restrict wadi
  crossings in rainstorm to road or railroad crossings, with +2 CP; railroad is
  treated as road for that purpose. For motorized major-river crossing, chart
  note 11 requires a road or railroad at the side; a qualifying crossing adds
  no CP or breakdown points. Do not extend that restriction to all infantry:
  §8.31 gives infantry crossing the Nile without a bridge as an example.
- Chart notes 6 and 8 retain distinct road and track cost effects, including
  the track exception for moving a vehicle down an escarpment. §8.42 prohibits
  vehicles moving up an escarpment and specifies the track, +8 CP and +6
  breakdown for moving down; consult the applicable Land-only breakdown rule
  for the specific entity (§32.55 treats motorization points specially).
  Removing congestion does not manufacture a crossing or erase that prohibition.
  §10.21 separately limits ZOC projection across major river, sea, lake and
  escarpment sides; a road crossing is not proof of ZOC projection.

## Conditional checks

These are synthetic rule vectors, not claims about a scenario position. Assume
friendly motorized units, a connected road, clear terrain with six-point hex
capacity, correct unit point values, and no other restriction:

1. Four points already on the road in the destination plus a two-point mover:
   six road points exceed the ordinary §9.33 five-point limit; §32.9 removes
   that obstacle, and six fits the clear-hex capacity. Entry and stopping still
   require every other applicable condition.
2. Five points already there plus a two-point mover: seven exceeds clear-hex
   capacity. Ignoring road congestion does not permit the mover to cease
   movement there; moving through depends on remaining movement and all other
   restrictions. This example assumes no applicable special stacking exception.
3. A motorized unit faces a rainstorm wadi side with no road or railroad
   crossing: §32.9 does not make the side passable. A qualifying crossing still
   incurs the chart's +2 CP and requires a legal route.

The separately inspected original Map D shows road crossings at
D3615–D3715 and D3715–D3714; see
[local source check](2026-11-17-graziani-fourth-indian-edges-next.md) for
scan coordinates and qualifications. Those marks alone do not establish
occupancy, point totals, orders, a complete path, weather, ZOC, or successful
movement. No current opening position or six-turn sequence is certified here.
Actual reachability needs the selected mode, initial and subsequent stacks,
formation values and exceptions, legal stage/segment timing, movement budgets,
terrain, weather, ZOC, and intervening actions. The waiver applies whenever
§32.0's mode applies; the source does not create a separate per-turn activation
or justify assuming that a bottleneck will be encountered within six turns.
An [independent source check](2026-11-17-graziani-road-stacking-review.md)
confirms the waiver and examples while distinguishing §9.34's retained text
and requiring the full Land-only mode condition.
