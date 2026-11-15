# Graziani source force supplement, version 5

The source authority may migrate from version 4 to version 5 through an
operator-initiated system transition. This adds immutable, source-linked Axis
initial-force evidence; it is not a player's option to remove historical forces
and does not admit those forces to the playable World8 battlefield.

The development Host exposes the operator-authenticated, idempotent
`POST /api/v2/campaigns/{campaignId}/source-axis-supplement` for this migration.
It uses the existing durable journal and its loopback/same-origin restrictions.

The supplement records 110 distinct candidate counters in ten source groups:
Cirene, Marmarica, the 1st and 2nd Libyan divisions, Maletti, Catanzaro, the
1st and 4th CCNN divisions, Aresca, and a Tripoli tank-regiment subset. It
retains partial source TOE for 87 and explicitly unknown TOE for 23. Some group
members and tank splits, HQ and engineer details, attachments, mobility, and
Land-only anti-air treatment remain unresolved. It is not a complete Axis
order of battle or a claim that all recorded counters are active on the map.

The `source-axis-force-supplement-admitted` event is ordered as
`contractVersion` (5), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`system`),
`priorAuthoritySetupHash`, `supplementHash`, `supplementalProfileHash`, and
`autoblindaSetupHash`. The action ID is the SHA-256
of UTF-8 `graziani.source.axis-forces.supplement.v1`. The supplement identity is
`sha256:1a34826c4d5e1a3fe20891f9064b61e8e6b58f5ecee7935d5ac076839e083372`;
changed source data requires a new identity rather than reinterpretation of
retained history.

The version 5 checkpoint carries the version 4 fields with contract version 5,
then appends `axisSupplementHash`, `axisSupplementBase64`,
`supplementalProfileHash`, and `autoblindaSetupBase64` after
`sourceSnapshotBase64`. Existing setup actions remain available after the
transition. Replay derives the supplement from its pinned source policy and
re-adjudicates the system event; old version 4 event and checkpoint bytes stay
unchanged. This state still has no game clock or Polish arrival.

The Axis owner can make the scenario's Autoblinda-40 choice: two trained and
fueled replacement points at Tobruk, or one point attached to each of two
distinct eligible units. The current provisional interpretation treats those
as alternatives and does not offer a mixed disposition. Attachment targets are
a source-checked subset; recorded attachments and replacements do not yet
implement the corresponding combat or replacement mechanics. The
`source-autoblinda-choice` event contains, in order, `contractVersion` (5),
`eventType`, `campaignId`, `priorStateVersion`, `priorCheckpointHash`,
`actionId`, `actor` (`axis`), `choice`, `target`, and `autoblindaSetupHash`.
The development Host uses the existing `/openings` and `/actions` routes with
`stage: source-autoblinda` and `actingElementId: land.graziani-axis-autoblinda`.

## Commonwealth source supplement, version 6

An operator/system transition after version 5 records 12 Commonwealth counters
explicitly listed on map C in §60.41, together with their partial source TOE.
It retains the French Motor Marines scenario/errata conflict and does not infer
mobility, training, attachments or Land-only disposition. The source content is
pinned to `sha256:a8fa59467db15c42e8f811caae738efc39e5ffb0976d50cf0e071406a03d0407`.
The development Host exposes operator-authenticated
`POST /api/v2/campaigns/{campaignId}/source-commonwealth-supplement` for it.

The `source-commonwealth-force-supplement-admitted` event contains, in order,
`contractVersion` (6), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`system`),
`priorSupplementalProfileHash`, `supplementHash`, and
`commonwealthSupplementalProfileHash`. The version 6 checkpoint keeps the
version 5 fields and appends `commonwealthSupplementHash`,
`commonwealthSupplementBase64` and `commonwealthSupplementalProfileHash`.
This is source evidence, not activation of a complete playable battlefield.

## Priority motorization, version 7

After version 6, an operator/system transition enables a bounded Commonwealth
first-line assignment: up to six points may be assigned or removed for each of
1 KRRC at `c3922` and 2 Rifle Brigade at `c3320`, using its own initial truck pool.
The source identifies both as historically motorized 10+ infantry under the
§32.54 priority. The profile uses §54.2's one infantry TOE per medium truck
point and §32.51's medium-equivalent motorization as a working ruling. Six
points cover each known six-TOE infantry component; full unit mobility and
global priority for all initial forces remain unproven.

The development Host exposes operator-authenticated
`POST /api/v2/campaigns/{campaignId}/source-priority-motorization`; Commonwealth
uses `/openings` and `/actions` with `stage: source-priority-motorization` and
`actingElementId: land.graziani-commonwealth-priority-motorization`. The journal
replays the migration and owner decisions; the campaign remains in setup.

## Axis priority motorization, version 8

The source profile records Maletti motor infantry, their shared 35-point pool
and source-derived lift. New detachment and unit-level allocation choices are
currently **blocked**: §59.2 requires legal initial stacking, and C3617's full
formation hierarchy, shell status and other placement choices are not yet
certified. Earlier v8 histories, if present, must retain their original replay
semantics and cannot be treated as proof of a legal completed setup.

The development Host exposes operator-authenticated
`POST /api/v2/campaigns/{campaignId}/source-axis-priority-motorization`; Axis
uses `/openings` and `/actions` with `stage: source-axis-priority-motorization`
and `actingElementId: land.graziani-axis-priority-motorization`.

## Axis minor garrisons, version 9

An operator/system transition adds 13 source-candidate Giarabub and minor
garrison counters. Giarabub includes an 8+ historically motorized machine-gun
unit; its organization/strength interpretation and available transport remain
unresolved, and no pregame transfer from an unattached pool is assumed. The
garrison source identity is
`sha256:4c17bd5e47bf43324c77f01e48574206ea7c4e31bb68bc3dca6a62b65911c7e1`.

The `source-axis-minor-garrisons-admitted` event contains, in order,
`contractVersion` (9), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`system`), `priorProfileHash`,
`axisGarrisonsHash` and `axisGarrisonsProfileHash`. The action ID hashes the
UTF-8 text `graziani.source.axis-garrisons.admit.v1`. The version 9 checkpoint
retains the preceding fields and appends `axisGarrisonsProfileHash` and
`axisGarrisonsBase64`. This does not create a complete active battlefield.

## Source battlefield and Cairo setup, versions 10–11

Version 10 merges retained initial World8 entries, the source force evidence,
and deferred arrival groups into a single duplicate-checked source battlefield.
Pending groups retain side, turn, stage, entry area and member IDs. The initial
battlefield is pinned to
`sha256:5004b5cd07299c479c6fc77bd2421fa751b5b3081e3e95ae1657d27a047a70b8`.
Source-evidence entries are not treated as combat-ready or proof of control;
the battlefield and control status remain explicitly incomplete.

The system event `source-battlefield-admitted` contains, in order,
`contractVersion` (10), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`system`), `priorProfileHash`,
`battlefieldHash`, and `battlefieldProfileHash`. Its checkpoint appends
`battlefieldProfileHash` and `battlefieldBase64` to the preceding state.

Version 11 adds the four source-listed Cairo counters as unplaced Commonwealth
setup entries. The owner chooses a supported city hex per counter; the source
placement ledger and source battlefield change together. The curated options
`e1730`, `e1830` and `e1930` exclude unknown initial occupants; `e1829` has
unresolved 16th Brigade stacking and the `e1931` map label is inferred.
Interpreting the scenario's “Cairo” instruction as a city-hex choice is a
provisional ruling. Other Cairo forces, map coverage, combat readiness and
global setup remain incomplete. Initial setup evidence is pinned to
`sha256:8ddb59d9f8be09ce4aff2ee7a1a88d71142bfa75760ecca274a1c4002a9902f3`.

The system event `source-cairo-setup-admitted` records, in order,
`contractVersion` (11), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`system`), `priorProfileHash`,
`cairoSetupHash`, `battlefieldHash` and `cairoSetupProfileHash`. The owner event
`source-cairo-unit-placed` records `contractVersion`, `eventType`, `campaignId`,
`priorStateVersion`, `priorCheckpointHash`, `actionId`, `actor`
(`commonwealth`), `elementId`, `locationId`, `cairoSetupHash` and
`battlefieldHash`. The checkpoint appends `cairoSetupProfileHash` and
`cairoSetupBase64` after the battlefield fields. Replaying each placement
rechecks the owner, source unit, prior occupancy and pinned policy.
The development Host exposes operator-authenticated
`POST /api/v2/campaigns/{campaignId}/source-battlefield` and
`POST /api/v2/campaigns/{campaignId}/source-cairo-setup`. Commonwealth uses
`/openings` and `/actions` with `stage: source-cairo-setup` and
`actingElementId: land.graziani-commonwealth-cairo-setup`.
`POST /api/v2/campaigns/{campaignId}/source-profiles` applies the available
source-profile migrations as separately journaled system steps in one transaction;
it does not choose player actions or start the game.

## Active Cairo source world, version 12

After all four Cairo setup units are placed, an operator/system transition
creates an expanded canonical source artifact with those units as unassigned,
setup-pending definitions. Their parent formation and mobility remain null;
the existing base source bytes remain embedded and unchanged. The expanded
content hash, independent of chosen locations, is
`sha256:3a7fddd510854c81b408e07742dd837f7be416206117ca58ea79357357a11ddd`.
The transition adds four actual element states and map representations to
World6/World8 using the retained explicit initialization and player placements.
Existing units and the scheduled Polish and 7th Indian groups are retained.
This is a partial active world: it does not supply native movement, full setup,
a game clock, or an executed arrival.

The `source-cairo-world-admitted` system event contains, in order,
`contractVersion` (12), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`system`), `priorProfileHash`,
`activeContentHash`, `activeWorldHash`, and `cairoWorldProfileHash`. The action
ID hashes `graziani.source.cairo-world.admit.v1`. The checkpoint appends
`cairoWorldProfileHash`, `activeContentHash`, `activeContentBase64`, and
`activeWorldBase64`. Replay rebuilds both content and world from the pinned
source setup and retained choices and compares exact canonical evidence; a
structurally valid altered artifact is not proof of source facts.
The development Host exposes operator-authenticated
`POST /api/v2/campaigns/{campaignId}/source-cairo-world` after all four choices.

## Active Maletti source world, version 13

The historical version 13 transition records promotion of owner-detached Maletti
infantry into World8. New promotions are blocked until the v8 detachment and
stacking issue above is resolved. A pinned
source artifact retains the exact Cairo artifact and describes the newly active
subset and source location `c3617`. Counter scans give each of the three a
stacking value of one; the total hex stack, terrain, topology and movement remain
unresolved. The original world and pending arrival groups are preserved. A unit
cannot undo its independent setup choice while its active representation exists.
Any future game-start authority must revalidate or remediate previously recorded
Maletti detachments and active representations against the complete C3617 stack
and hierarchy. Successful historical replay alone does not certify them as legal.
The tank-command OA's LX(L) company attachment conflicts with the scenario's
listed attached II(M); its split, relationship and effect on the formation
limits under §19.5 must be settled before the C3617 stack can be certified.

Versions 23–24 provide a separate, explicitly provisional path: after the
four 1AR and two corps artillery candidates have been placed and promoted away
from C3617, Axis may select a source-bound baseline Maletti hierarchy. This
profile follows the scenario's II(M) attachment instead of the OA LX(L) split,
treats the repeated 4/1AR as one counter, and preserves the provisional XXVI
identification. It validates the recorded parent relationships, known TOE,
free-force reservations and C3617 occupancy before allowing at most one motor
infantry battalion to detach. The full division then remains stack five and
the detached battalion adds one against the six-point clear-hex limit. Every
dependent action revalidates that proof. This is a limited working ruling, not
certification of the entire historical setup.

The `source-maletti-hierarchy-admitted` system event and
`source-maletti-hierarchy-changed` Axis event carry contract version 23, prior
checkpoint/action/actor identity, hierarchy and priority hashes and the profile
hash. Version 24's `source-certified-maletti-world-promoted` system event binds
the prior profile, active source content/world, battlefield and new profile
hashes. The detached source unit enters World8 without claiming native movement
or a game clock; older uncertified v8 histories remain replay-only.

## Matruh source formation, versions 25–26

The Commonwealth owner can select a provisional Matruh garrison baseline with
the Selby HQ and five source-listed infantry units represented as a grouped
formation in source authority. The policy preserves the distinction between
scenario attachments and the errata's Buffs/Hampshire assignment, and records
the Essex/Durham historical-assignment conflict. It treats co-location as an
explicit setup ruling. Source counter evidence gives the HQ a garrison `G (3)`;
at its assigned city §9.16 permits it to stack freely. The bounded proof allows
at most one infantry detachment, keeping room for the three scenario-permitted
free gun units even in the worst case. This does not certify the full scenario.
Any later setup placement that can affect `d3714` must revalidate the retained
Matruh stack and this bound before it commits.

The version 25 `source-matruh-formation-admitted` system and
`source-matruh-formation-changed` Commonwealth events bind the formation,
battlefield and source-profile hashes. Version 26's
`source-matruh-detached-world-promoted` system event binds active source
content/world and battlefield hashes, adding the selected detached unit to
World8 while retaining the grouped source formation and pending arrivals. The
Axis Maletti and Matruh transitions can be applied in either order. Native
World6 still has no grouped-counter movement or combat support.

The `source-maletti-world-promoted` event contains, in order,
`contractVersion` (13), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`system`), `priorProfileHash`,
`activeContentHash`, `activeWorldHash`, `battlefieldHash`, and
`malettiWorldProfileHash`. The action ID hashes UTF-8
`graziani.source.maletti-world.promote.v1`. The checkpoint appends
`malettiWorldProfileHash`, `malettiActiveContentHash`,
`malettiActiveContentBase64`, and `malettiActiveWorldBase64` after the retained
version 12 fields. Replay re-derives the selected source subset and checks the
world, source battlefield and pinned content identity.
The development Host exposes operator-authenticated
`POST /api/v2/campaigns/{campaignId}/source-maletti-world` for this transition.

## Independent source catalog, version 14

The version 14 system transition activates the source-detached 1 KRRC and 2
Rifle Brigade in World8. New Maletti promotion is blocked as described above. It
can follow version 12 directly or a version 13 world. A fixed canonical source
catalog describes all five supported counters independent of which are active;
its identity is
`sha256:92f5c0d2dd6abf930bf7d4dabc1d7c348ac93c9481051700e359f0d9207a3f20`.
The event retains the actual predecessor world and choices, and each promotion
checks source location and TOE, preserves existing elements and representations,
and leaves both deferred arrival groups pending. Terrain, topology, movement and
the full initial force remain incomplete.

The `source-independent-world-promoted` event contains, in order,
`contractVersion` (14), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`system`), `priorProfileHash`,
`activeContentHash`, `activeWorldHash`, `battlefieldHash` and
`independentWorldProfileHash`. The action ID hashes UTF-8
`graziani.source.independent-world.promote.v1`. The checkpoint appends
`independentWorldProfileHash`, `independentActiveContentHash`,
`independentActiveContentBase64` and `independentActiveWorldBase64`.
The development Host exposes operator-authenticated
`POST /api/v2/campaigns/{campaignId}/source-independent-world`.

## Local source map, version 15

An operator/system transition binds source-verified geometric neighborhoods for
`c3617` and `c3320` to the active source content and rebinds the existing
World8 without moving units. The map artifact is pinned to
`sha256:d60465ab4a1fdf9799b8f49e8a22768b6ddb69e305e8e854f3c524d0295caf35`.
The original Map C scan supports the six adjacent hexes around each location;
terrain, route and hexside effects remain unknown. These edges do not grant a
legal movement action or establish a complete map. Later independent-unit
promotions retain and rebind the same map evidence.

The `source-local-map-admitted` event contains, in order, `contractVersion`
(15), `eventType`, `campaignId`, `priorStateVersion`, `priorCheckpointHash`,
`actionId`, `actor` (`system`), `priorProfileHash`, `activeContentHash`,
`activeWorldHash` and `localMapWorldProfileHash`. The development Host exposes
operator-authenticated `POST /api/v2/campaigns/{campaignId}/source-local-map`.

## Expanded independent source catalog, version 16

The source catalog adds three scenario-listed independent Commonwealth units:
3 Coldstream Guards at `c3922`, 2 Scots Guards at `c4131` and 1 Royal
Northumberland Fusiliers MG at `c3721`. Original scenario, OA, characteristics
and counter evidence establish their locations, TOE, CPA and stack values. The
new fixed content identity is
`sha256:526b73bfc5933689b7bb90b5362ccb482b33e23dc7e85ad52047ea05e706f714`.
It retains the version 15 map artifact, earlier active units and pending groups;
new Axis detachments remain blocked pending stack certification.
It does not certify movement or game-start readiness.

The `source-expanded-independent-world-promoted` system event records, in order,
`contractVersion` (16), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor`, `priorProfileHash`,
`activeContentHash`, `activeWorldHash`, `battlefieldHash` and
`expandedIndependentWorldProfileHash`. The checkpoint appends that profile
hash, `expandedIndependentActiveContentHash`,
`expandedIndependentActiveContentBase64` and
`expandedIndependentActiveWorldBase64`.
The development Host exposes operator-authenticated
`POST /api/v2/campaigns/{campaignId}/source-expanded-independent-world`.

## Attached initial loads, version 17

The Commonwealth owner can explicitly attach idle first-line points from the
two supported independent priority units' own pools, then choose one ammunition
or three fuel points per idle point under §59.66B. The infantry's six-point
lift must be reserved first; the same point cannot carry both infantry and a
load. Pool conservation is enforced, loaded points cannot be detached until
corrected during setup, and lift cannot be reduced while dependent idle points
remain. A provisional policy permits these idle points on the same priority
unit while global infantry priority is still unresolved.

The state retains the rule that these supplies must be consumed first and cannot
be transferred to a supply unit, but gameplay consumption is not yet implemented.
The policy identity is
`sha256:e123f7be606c898b7c51ecbb874882e6e28c9008c75bd81587acf91853d991ab`.
The `source-attached-initial-loads-admitted` system event and
`source-attached-initial-loads-changed` Commonwealth event carry contract
version 17, campaign and prior-checkpoint identity, action and actor, optional
choice and element, the load-state hash and profile hash. The checkpoint appends
`attachedInitialLoadsProfileHash` and `attachedInitialLoadsBase64`.

## Scenario-initial damaged vehicles, version 18

The source lists two A9 and one A10 tank TOE already broken in Alexandria at
scenario start. A separate source-initial stock ledger records these as existing
damage; it does not invent a prior breakdown event. Commonwealth may choose
`e3613` or `e3714` for each under an explicitly provisional interpretation of
the scenario's Alexandria location. This choice does not certify repair or
stacking legality. The initial source policy is pinned to
`sha256:23517165282c3ca112e5c17eace4496f7ef881cc1f5ae13e113977f8b3efa964`.

The `source-initial-wrecks-admitted` system event and
`source-initial-wreck-placed` Commonwealth event carry contract version 18,
campaign and prior-checkpoint identity, action and actor, optional wreck ID and
location, `initialWrecksHash`, and `initialWrecksProfileHash`. The checkpoint
appends `initialWrecksProfileHash` and `initialWrecksBase64`. Later repair must
support this source provenance explicitly.
The development Host exposes operator-authenticated
`POST /api/v2/campaigns/{campaignId}/source-initial-wrecks`; Commonwealth uses
`stage: source-initial-wrecks` and
`actingElementId: land.graziani-commonwealth-initial-wrecks` with its existing
`/openings` and `/actions` routes.

## Axis free artillery setup and world, versions 19–20

The Axis owner may place the four source-listed 1AR artillery battalions at
the restricted verified Libya sites `c4407` and `c4607`, at most two per site.
The source ledger and battlefield update together. Other free-force locations
are not covered; the repeated `4/1AR` and XXI/XXVI Corps wording remains an
explicit source conflict. Supply/dummy counters have zero combat stacking
value and are not disclosed by the placement decision.

The scenario order of battle identifies the four counters as independent,
each with three gun TOE and 15 CPA. After placement, a system transition can
promote selected units into the actual World8; it may be repeated as additional
units are placed. The fixed source content identity is
`sha256:730ff4b2080bc8f255178e7fcba1b74df96918cee46ed9efb146d2fbd7c8904b`.
The transition retains prior units, map evidence and pending arrivals without
claiming native movement or a complete battlefield.

The version 19 `source-axis-free-setup-admitted` system and
`source-axis-free-unit-placed` Axis events carry campaign/prior-checkpoint
identity, action/actor, optional unit and location, the setup and battlefield
hashes, and `axisFreeSetupProfileHash`; its checkpoint appends that profile hash
and `axisFreeSetupBase64`. The version 20
`source-axis-free-world-promoted` system event binds prior profile, active
content/world and battlefield hashes plus `axisFreeWorldProfileHash`; its
checkpoint appends that profile hash, `axisFreeWorldContentHash`,
`axisFreeWorldContentBase64` and `axisFreeWorldBase64`.
The development Host exposes operator-authenticated
`POST /api/v2/campaigns/{campaignId}/source-axis-free-setup` and
`POST /api/v2/campaigns/{campaignId}/source-axis-free-world`. Axis places units
using `stage: source-axis-free-setup` and
`actingElementId: land.graziani-axis-free-setup` on `/openings` and `/actions`.

## Fourth Indian grouped setup

The source authority can propose the 4th Indian Division at `d3615` as a
single grouped counter, retaining its eleven source members, their parent
relationships, source component strengths and the pregame initialization's
CP and cohesion values. Commonwealth must confirm the proposed grouping;
other initial dispositions remain unsupported. The 7th Indian Brigade remains
pending for its scheduled arrival. Central India Horse starts as untrained
motorized infantry under an explicit provisional application of OA note c to
the scenario's predeployed unit; its actual pre-scenario training is uncertain.
Motor points reserved for its transport duties come from the existing
parent-controlled `cw-4-indian-attached` pool of 22, not a second pool.

The original Commonwealth chart §19.31 (PDF page 24) gives an infantry division
three brigade slots and an ordinary infantry brigade three infantry battalions.
With the confirmed 5th and 11th Brigades each retaining their three full
battalions, two of the division's three brigade slots are attached. Under Land
§§9.26–9.27 the division is not a shell and its printed counter stacks as five.
The proposed grouping's value remains unknown until the owner confirms it;
future detachment must recalculate shell status. This source authority does not
yet admit grouped movement, combat, a game clock or the Polish arrival.

The same grouped world can retain the previously confirmed Matruh garrison and
Maletti formations. An owner-selected detached battalion is represented once:
either as an interim independent occupant or, after promotion, in World8. Its
source-assigned motor points continue to count against the original parent pool.
Admission revalidates the retained battlefield and source hierarchy; it does not
make the grouped world a native gameplay snapshot. Maletti's Sahariano members
retain their source reconnaissance classification and the OA note that the
regiment was camel in name only; its mobility remains unresolved.

The same source world can propose the 1st and 2nd Libyan Divisions at `c4020`
and `c3920`, respectively. Axis confirms each grouping separately. Both retain
their two three-battalion regiments and their original attached 15-point motor
pools. The 1st includes its antitank battalion; the 2nd includes IX(L) tanks
and its artillery's two distinct gun types. The Axis organization chart §19.32
and Land §§9.26–9.27 establish that the confirmed two-regiment formations are
not shells. Their printed counters each have stacking value five (Air/Logistics
counter section 2, printed page 40). Proposed formations have unknown stacking
until confirmed. These setup choices do not yet establish transport allocation,
fuel availability, movement, or a playable GT1. Their pinned source content
identity is `sha256:206abadd895a3c867e35897897aa42c0cdb2fd8894b621005fe69dd3f1735676`.

## Axis corps artillery, versions 21–22

After the 1AR setup and promotions, Axis may place the XXI and XXVI Corps
artillery candidates at the restricted verified sites `c4307` and `c4308`,
one counter per site. The original errata confirms XXI Corps artillery was
listed twice; the unqualified XXVI scenario label is provisionally matched to
the unique XXVI artillery counter. The separate repeated `4/1AR` remains
unresolved. Under Land §32.83(3), the printed light AA components on these
`kk` artillery units are retained; this does not assert full combat support.

Version 21 journals the source-bound owner placements and battlefield changes;
version 22 promotes placed units into World8 with their source gun/AA TOE,
preserving the prior world and pending arrivals. Its source content identity is
`sha256:36ce3f5a3be45942e7823156638f35020c8ad6187bff714803ea40db8d6b56ed`.
Neither transition completes setup, establishes control or starts the clock.

The source journal currently retains bounded canonical checkpoint evidence for
each accepted step, including nested source content. Source replay streams and
re-adjudicates one journal row at a time, retaining the per-checkpoint bound
without the former aggregate decoded-history cap. Repeated content still grows
the journal and replay time, and the session still retains prior event history;
this is not yet a scalable full-campaign storage
design. A future compatible content-addressed representation must preserve exact
content identity and re-adjudication rather than relying on current checkpoint sizes.
One local v22 test journal held 69 accepted steps and approximately 50.4 MB of
step evidence, with a 2.88 MB latest checkpoint. This measurement is setup
coverage, not a forecast of an entire game.

## Axis September supply planning, version 38

Scenario §60.37 requires Axis prestart planning for the remainder of September.
After retained Commonwealth prestart plans finish, Axis can roll and explicitly
finish separate GT3 and GT4 supply plans in that order. The provisional random
draw order is Axis GT2, Axis GT1, Commonwealth GT1–GT4, then Axis GT3 and GT4;
all draws continue from the retained stream. The September B-chart outcomes are
0, 1, 1, 2, 2, and 3 supply units. Owner allocations use convoy lanes 2, 3, and
6 and operational stages 1–3, with at most three units per lane and stage.

Land §§32.43–32.44 leave tension between receipt on permitted turns and the
player's option to bring in the rolled amount. This version provisionally allows
a player to finish with zero allocated units even after a positive roll; the
finish is an explicit choice. Availability and allocation do not constitute
delivery or settle convoy attacks. GT4 non-supply cargo remains unresolved,
including Italian light AA replacement eligibility under Axis §20.66 and Land
§32.83. No GT1 clock or playable campaign start is created by these plans.

## Cirene and Marmarica grouped source world, version 39

The Core source authority can admit the 63 Cirene division at C4120 and the
62 Marmarica division at C3918 as two proposed grouped counters, retaining
their 15 and 14 source members and the existing grouped world and journal.
Axis confirms each provisional source-default hierarchy separately. Their
printed stack is unknown until confirmed, then five under the sourced
formation and shell rules. The 50- and 45-point parent motor pools remain
unassigned; global historical-motorization priority remains unresolved.

The pinned content identity is
`sha256:0884578e9d4b10bb0a0a9112456a8735079923cf0bceef68182315626a8afca1`.
It retains the scenario-attached trained LXIII(L) and LXII(L) tank units,
the errata's mixed artillery and Light AA retained under Land §32.83(3),
and null engineer TOE where not established. Mobility, complete battlefield,
terrain, control and game clock remain unavailable. The Core event and normal
Axis setup action replay from retained authority; the separate Host system
admission route and Host replay coverage for this version are not implemented.
