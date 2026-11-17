# Abstract fleet bombardment: stage-order proposal

**Status:** `gra-fleet-bombardment-order-v1-proposed` is a bounded planning
choice, not an official correction, admitted action or demonstrated six-turn
attack. The inspected Land and Air outlines do not name §32.73's **Tactical
Naval Movement Segment**. They support an ordering analogy, not a definitive
replacement. Retain an unsupported gate until this order, startup, target and
privacy rules are reviewed and versioned.

## Sources and sequence conflict

Visually rechecked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 12, 42 and 44 §§5.2, 30.34–.37 and 32.71–.75;
[Air/Logistics rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf)
PDF/printed p. 3 §33.0 and PDF/printed p. 18 abstract-mode discussion;
[scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 4 / printed p. 28 §§60.45–.46 and
[September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF p. 3 §60.45. SHA-256 values respectively match
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
`9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`, and
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
This is a prompted same-edition source check, not exhaustive search of every
rule, later erratum or independent edition. Fleet chart p. 31 and §41.5 were
checked in the separate [fleet review](2026-11-17-graziani-abstract-aa-fleet-review.md).

Land-only §5.2 orders Weather, Organization, Naval Convoy Arrival, then the
Commonwealth Fleet Phase with **Assignment** and **Repair** segments, before
player A's Reserve and Movement/Combat phases. It does not separately list a
Tactical Naval Movement Segment. §32.73 nonetheless permits at most one
abstract Axis fleet attack per OpStage, secretly plotted during Fleet
Assignment and occurring in the Tactical Naval Movement Segment. §32.75 bars
the attack when map section E has a rainstorm or sandstorm. §32.74 uses the
Air Bombardment table and allocates damage to ships in Alexandria Harbor;
allowance and damage-column rolls are separate, with Campaign modifiers if
applicable. It does not explicitly resolve the missing segment's place.

The Land/Air §33.0 outline also has Fleet Assignment then Fleet Repair, followed
by Land Support Air mission assignment/deployment, combat and **Mission
Completion** before Reserve. It likewise does not label a Tactical Naval
Movement Segment. Detailed §30.43 puts the distinct Chariot Raid at Air
Mission Completion, but that rule's Campaign and timing conditions do not
thereby govern the §32.7 abstract attack. The Air/Logistics abstract-mode
page also says to ignore §32.7 when the Air rules handle fleet bombardment.
That reinforces a mode boundary: §33.0 can inform a placement analogy, not
silently import aircraft, detailed bombardment, Chariot prerequisites or a
proof of the missing segment's intended location.

Errata §60.45 retains the scenario's first paragraph and discards its later
list beginning “The following units are available.” The retained paragraph
says initial ships are deployed per the Fleet Schedule, need no repairs, and
cannot move until the first OpStage of GT2. The schedule permits one of three
cruisers and **two** of seven destroyers to deploy at Malta; see the fleet
review. These setup facts do not place the missing segment or prove that a
particular attack is taken. GT1 stage-entry and setup ordering remain a
separate question.

## Proposed versioned sequence

For a stage where this feature and its source-state prerequisites have been
accepted:

1. Resolve stage weather and earlier phases under their own contracts. If
   section E's weather is a known rainstorm or sandstorm, no attack is offered
   for execution in this stage. An unknown section-E result is unsupported,
   not clear; do not use the weather of an arbitrarily selected Alexandria
   hex as a replacement for §32.75's section-level condition.
2. At Fleet Assignment, obtain the Commonwealth's legal assignment and the
   Axis's optional secret attack plot. As a project choice, seal both choices
   against disclosure until committed so the Axis cannot condition its plot on
   an assignment that was not yet authorized for it to observe. §32.73 says
   *during* the segment, but does not itself prescribe this sealed-choice
   protocol. Do not move a GT1 initially immobile ship in order to create a
   target or assume every scheduled ship is in Alexandria.
3. Complete Fleet Assignment and then Fleet Repair, respecting the applicable
   repair-progress rules. Insert one **abstract fleet attack resolution**
   boundary immediately after Fleet Repair and before the first player's
   Reserve Designation. This deliberately borrows the relative location of
   Air-mode Mission Completion as an analogy and leaves §5.2's existing
   assignment→repair order intact; no printed case expressly places the
   Land-only attack there. Snapshot ships actually in Alexandria Harbor at
   that boundary. The choice means a repair completion in the same stage is
   applied before this attack; that effect is part of the ruling, not a
   source-proven fact.
4. If a valid plot and nonempty eligible harbor are present, consume at most
   one scenario allowance, execute the accepted bomb-column die and sequential
   damage dice, then let the Axis allocate actual damage to eligible ships
   under a separately accepted allocation contract. A table zero is still an
   executed attack and consumes its allowance under this proposed accounting.
   If the harbor is empty, **leave unsupported** pending a reviewed target and
   consumption rule; do not silently charge, refund, invent damage or resolve
   an attack against Malta. A voluntary no-plot consumes no allowance. Storm
   prevents the attack; treatment of a previously recorded invalid plot must
   be separately decided rather than guessed.
5. Commit plot, assignment, repair and attack in their respective authoritative
   boundaries with hidden choices, weather, rule version, accepted snapshots,
   damage and RNG cursor replayable. If any necessary source/state, allowance,
   repair, target or allocation is unknown, do not fabricate an empty outcome
   or apply partial damage. Enforce exactly once across retries and preserve
   owner/opponent/intelligence visibility of plot, allowance and targets under
   an explicit disclosure policy.

Alternatives include resolving immediately after Assignment and before Repair,
or modeling a separately sourced naval movement step elsewhere. They differ
when a ship moves, a repair completes, a target disappears, or later operations
change its status. Neither the source's segment name nor its reference to the
Chariot **result table** proves one of those placements. A strict defer-until-
clarified policy remains viable and avoids taking a provisional ordering as
historical fact. Determine legality of the plot, allowance exhaustion, storm,
empty target, repair and startup independently before executing this choice.

## Distinguishing checks

Unexecuted synthetic tests: a ship assigned away from Alexandria versus kept
there; a ship finishing six-stage repair in the same stage (before/after
ordering alternatives); storm in section E versus clear and unknown; no plot,
known zero-damage attack, exhausted allowance and empty harbor; GT1 stage 1
versus GT2 movement under the corrected initial fleet restrictions; one versus
two Malta destroyer choices; first/second-player roles. Verify Campaign
modifier on the **two distinct single dice** (scenario allowance and bomb
column), not on the two sequential damage-result dice; verify valid ordered
damage rolls, no reroll after
blocked target or crash, no duplicate allowance/debit, and equal-authorized
views with different hidden plots. No gameplay tests, code changes or readiness
changes were made.
An [independent source review](2026-11-17-graziani-fleet-sequence-review.md)
confirms the missing-segment boundary and distinguishes the two modified
single-die rolls from the separate sequential damage dice. It also flags
recovery across Assignment and Repair commits as a separate design gate.
