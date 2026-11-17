# Graziani Commonwealth rail: Land-only scope

**Status:** bounded source research and conditional vectors, not an admitted
rail model or proof of a six-turn trip. The supply-specific allowance is
explicit; its relationship to the ordinary passenger/unit rail allowance and
several stage, spatial and abstract-resource rules remain gates.

## Sources and initial extent

Visually rechecked hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 12, 15, 37 and 43 §§5.2, 8.71–.73, 24.61–.67 and 32.34;
[scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 4 / printed p. 28 §60.7; and
[September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
p. 1 §§8.71 and 8.73. SHA-256 respectively
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`,
and `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
This is a prompted reading of the same scans as the earlier
[rail review](2026-11-13-graziani-rail-scope.md), not an independent edition or
exhaustive review of all movement and rail errata.

Scenario §60.7 says the railroad runs to Mersa Matruh D3714 and ends there at
setup. Land §8.71 identifies the usable Alexandria–Mersa Matruh line; §24.6
also describes a Nile Delta rail complex and a separate defunct Benghazi–Barce
line included for historical purposes. This supports a starting Matruh
railhead, **not** a certified per-hex network, starting control of each hex or
a completed westward extension. Land §24.67 says unbuilt railroad hexes do not
function and westward construction from the completed railhead cannot skip
hexes. Exact source-backed edges, junctions, damage and map seams remain inputs.

## Capacity and normal eligibility

- Land §32.34 explicitly allows the Commonwealth to transport **any two
  supply units, real and/or dummy, in one direction as the total load for one
  OpStage**, subject to normal railroad rules. Two real, one real plus one
  dummy, or two dummies satisfy the quantity/type ceiling; a third supply unit
  exceeds it. One outbound plus one inbound is **not** one-direction use of
  this allowance. The allowance is per OpStage, not per Game Turn, per city,
  per direction or separately for real and dummy counters. Empty and one-unit
  loads do not grant a second independent movement under its stated total.
  This is a cap on eligible supply counters, not proof that a particular dummy
  has been placed, that a route exists, or that every other rule permits it.
- Land §8.72 separately states Commonwealth rail movement once per OpStage,
  one stack in each direction, of units and/or supplies along the finished
  line. Do **not** silently add §32.34's two supply counters as a second rail
  service, assume it replaces all other cargo, or interpret it as two supply
  units **in each direction**. Whether supply consumes a general stack slot,
  whether non-supply cargo may accompany it, and how the abstract one-direction
  limitation interacts with an opposing-direction general stack require an
  explicit versioned precedence decision. No source passage inspected
  expressly settles that interaction.
- Under §8.73, a traveling unit starts the OpStage on rail, has not expended
  CP for another reason during that OpStage, and may not be in an enemy ZOC;
  it may not enter an enemy ZOC or enemy-occupied hex at any time in the rail
  move. Rail travel is along the line to a stopping hex. These requirements
  need exact start/current state and path, not geometric proximity to a drawn
  but unbuilt rail. Land §10.26's friendly-combat-unit treatment of enemy ZOC
  for movement and its relationship to §8.73 need review; do not assume an
  unqualified exception or override. Determine how §8.73's stage-wide CP
  wording applies to later CP spending, arrival placement and represented
  cargo before admitting actions.
- Section 8.77 explicitly permits picking up and dropping off units anywhere
  along the rail line while always observing the stacking and directional
  restrictions and the CP requirement. How that general rule composes with
  §32.34's total supply load and owner choices remains a separate capacity and
  batching question; do not assume every passenger must travel the whole route.
- Section 8.78 additionally bars Commonwealth use of any rail hex west of a
  rail hex occupied by an Axis combat unit. This is separate from entry into an
  enemy-occupied hex or its ZOC; its west relation, snapshot and information
  effects require an accepted map and observation contract.
- Errata §8.73 adds that units travelling by railroad may not **earn
  reorganization points** in that stage. This is distinct from declaring that
  every Organization/Reorganization action is forbidden or retroactively
  undoing earlier phase actions. The Land sequence places the Commonwealth
  rail phase after that player's Movement/Combat and convoy phases; roles A/B
  change with initiative. Bind stage-wide history and effect timing explicitly.
  A successful synthetic rail move does not prove its stage is reached in a
  historical game.

Land §8.71's original Commonwealth-only sentence is corrected by errata §8.71,
which points to Axis use under Air/Logistics §54.4. The correction does not
make §32.34 an Axis allowance or revive the defunct Benghazi–Barce line. Axis
control, rolling stock, phase and Land-only resource treatment are separate
unresolved matters.

## Extension and unresolved interactions

Land §24.61 names the 10th and 13th New Zealand railroad construction companies
as the only new-track builders; §24.62 gives one company two OpStages per hex
or both companies in one hex one OpStage. Other eligible engineers can repair
destroyed track under distinct conditions. §24.65 prohibits construction or
rebuilding in enemy-controlled or occupied hexes; §24.64 prints a stores cost
whose treatment under §32.11's Land-only abstraction requires a separate
ruling. No claim is made here that these companies arrive, reach the railhead,
construct a given hex or receive resources during the six-turn window.

Initial railhead, scenario supply placement and this abstract allowance do not
prove an eligible supply unit starts on an undamaged controlled rail path, has
available capacity, or can reach a selected destination. Verify city/rail hex
identity, attachment and staging of supply counters, movement and stack
semantics, initial rail control and later destruction, seam coverage, ZOC and
fog, and any other applicable restrictions before an authoritative command is
admitted. Unknown route or state is not a no-op, clear rail or proof of failure.

## Proposed focused vectors

These are unexecuted synthetic inputs, not historical playthroughs:

1. In a legal certified path, exercise two real, real-plus-dummy and two dummy
   counters in one direction; reject three under §32.34. For two opposing
   supply movements, test the chosen §32.34/§8.72 precedence rather than
   assuming both must consume the same allowance. Test
   one versus two OpStages independently, source/destination and counter
   conservation, and no dummy-identity disclosure to an unauthorized audience.
2. Compare one ordinary unit stack in each direction with the mixed
   supply/general-stack cases only after a precedence policy is chosen. Do not
   turn currently unsupported combinations into expected legal actions.
3. Reject or block travel beginning off rail, entering enemy-occupied rail,
   using unbuilt/destroyed track or exceeding the accepted stage-wide CP
   constraint. Test an enemy-ZOC path with and without a friendly combat unit
   under the separately accepted §10.26 interaction, and unknown edge/ZOC as
   unknown rather than clear. Test rail travel and reorganization-point denial
   without assuming all reorganization actions are forbidden.
4. Verify Matruh railhead at setup, a not-yet-built western hex, construction
   completion and damage/repair only after their timing/resource rules are
   admitted; test adjacent but disconnected rails and map seams. Check replay,
   duplicate commands, stage rollover and ruleset version identity.

No gameplay tests were run, and no runtime or readiness flags changed.
The [independent review](2026-11-17-graziani-rail-scope-review.md) confirms
the bounded cap and Matruh railhead and flags the §10.26 and opposite-direction
precedence qualifications incorporated in the vectors above.
