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
retains partial source TOE for 89 and explicitly unknown TOE for 21. Some group
members and tank splits, HQ and engineer details, attachments, mobility, and
Land-only anti-air treatment remain unresolved. It is not a complete Axis
order of battle or a claim that all recorded counters are active on the map.

The `source-axis-force-supplement-admitted` event is ordered as
`contractVersion` (5), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`system`),
`priorAuthoritySetupHash`, `supplementHash`, `supplementalProfileHash`, and
`autoblindaSetupHash`. The action ID is the SHA-256
of UTF-8 `graziani.source.axis-forces.supplement.v1`. The supplement identity is
`sha256:0646362c46b0354969caaf61a955fbde111664c29f4f072dd4ffb30d9bcc76fc`;
changed source data requires a new identity rather than reinterpretation of
retained history. (This identity changed from the prior
`sha256:1a34826c4d5e1a3fe20891f9064b61e8e6b58f5ecee7935d5ac076839e083372`
under RUL-GRA-26, which gives 1-ccnn-hq and 4-ccnn-hq a known one-point
artillery TOE with an explicitly unidentified weapon; see
docs/work/plans/2026-11-12-graziani-ruling-decisions.md.)

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
`sha256:84313115e7200cc53c732ff870945d7f48dbe7371bcea4f3d5e209f85ed6ad16`
(changed from `sha256:4c17bd5e47bf43324c77f01e48574206ea7c4e31bb68bc3dca6a62b65911c7e1`
under RUL-GRA-26, which changes the upstream Axis supplement identity it derives from).

The `source-axis-minor-garrisons-admitted` event contains, in order,
`contractVersion` (9), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`system`), `priorProfileHash`,
`axisGarrisonsHash` and `axisGarrisonsProfileHash`. The action ID hashes the
UTF-8 text `graziani.source.axis-garrisons.admit.v1`. The version 9 checkpoint
retains the preceding fields and appends `axisGarrisonsProfileHash` and
`axisGarrisonsBase64`. This does not create a complete active battlefield.

The original Axis OA page 14 and §4.46b identify four independently deployed
minor infantry counters: Scheferzen (`c3419`, code `dd`, initial TOE 1), Fort
Maddalena (`c3019`, `q`, TOE 2), El Grein (`c1715`, `ee`, TOE 1), and
Mechili (`b4921`, `r`, TOE 2). Each has CPA 15. The original Air counter
section 2 prints their respective stack values as 0, 1, 0, and 1. At version 9
these remain source evidence, not active World8 counters. Promoting them requires a versioned
native content and world successor, authoritative anchor, checkpoint and replay;
the unresolved cross-section seam must not be treated as certified.

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
`e1730`, `e1830` and `e1930` exclude unknown initial occupants. Version 41
later permits owner confirmation of the 16th Brigade grouping at `e1829`, but
does not add that hex to these placement choices. The Air/Logistics important
locations summary (PDF p. 26) confirms `e1931` as a Cairo major-city hex;
the printed map label itself is obscured.
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
`sha256:3a8832098cac32c35d7ec7076273c0903e093a04d4529da973a52b8c3255c4aa`
(changed from `sha256:92f5c0d2dd6abf930bf7d4dabc1d7c348ac93c9481051700e359f0d9207a3f20`
under RUL-GRA-26, which changes the upstream Axis supplement identity it embeds).
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
`sha256:4067e7396531806ff5ffed83e8ba6dd2866092ed36b84ec9f31dc13a6ac0f524`
(changed under RUL-GRA-26, which changes the upstream Axis supplement identity it embeds via the independent catalog).
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
`sha256:785a8643296f50d54de993c6c82b99445a7f1700f3c744ec92ebafe9ee6c8145`
(changed under RUL-GRA-26).
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
`sha256:aa31213766422d8a634ce09e92d1cdaaa97f3a3674785cc2b83e196294415852`
(changed under RUL-GRA-26).
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
`sha256:05aafa2092adebf7d50d7d3f02d1d1dd68ccf75098390ba03e68a0e81edf3fb7`
(changed under RUL-GRA-26).
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

The full real-source Host journal test through versions 39–42 passed in 9m 39s
on macOS arm64 using an isolated .NET 10 Release build. This is the complete
multi-request test, not the latency of a single cold replay. The prior hosted
CI Orleans test step on PR 345 took 21m 44s against its 25-minute step timeout;
the local and hosted measurements are not comparable. Keep the existing timeout
and coverage guard, and check exact-head CI headroom after further changes.

## Axis September supply planning, version 38

Scenario §60.37 requires Axis prestart convoy planning for the remainder of
September. The map calendar places GT1 in September III and GT2 in September IV;
GT3 and GT4 are October I and October II. Land §32.43 allows planning supply
further in advance, so this version offers optional September preplanning for
those October arrivals. It must not be treated as a requirement imposed by
§60.37. After retained Commonwealth prestart plans finish, Axis can roll and
explicitly finish separate GT3 and GT4 supply plans in that order. The provisional random
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
§32.83. Axis §20.66 permits light AA replacement planning beginning in GT2,
so this pregame supply plan does not schedule those replacements. No GT1 clock
or playable campaign start is created by these plans.

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
Axis setup action replay from retained authority. The operator-authenticated Host
route `POST /api/v2/campaigns/{campaignId}/source-italian-divisions-grouped-world`
records the source admission in the ordinary durable journal; the owner uses the
normal source setup actions to confirm each group. Replay re-adjudicates the
system admission and both owner choices. The admitted journal step is version 94,
kind `source-italian-divisions-grouped-world`, with the normal system command,
event, receipt and checkpoint; this leaves older journal bytes unchanged.

## Commonwealth grouped formations, versions 40–41

The operator can admit 7th Armored Division at `d3612` using
`POST /api/v2/campaigns/{campaignId}/source-seventh-armored-grouped-world`.
It retains seven scenario-present members and the existing ten-point attached
motor pool. Commonwealth confirms the proposed hierarchy via normal source
actions. The formation is a division shell; the confirmed shell stacking value
of five is an explicit provisional ruling. The pinned content identity is
`sha256:b28f2a61dd3969ee0e7f9d2d61d62c2fbd661fb220d7405dc2eff4c2a4c78928`.
Common Charts §9.4 explicitly gives a division shell a stacking value of three.
The versioned `source-seventh-shell-stack-corrected` system event migrates the
legacy five-point state to three without rewriting its existing event history.
If Commonwealth had finalized setup, the correction reopens that owner only.
Corrected source checkpoints use 51/52, or 53/54 when owner-finalization
checkpoint wrapping is active. Uncorrected shell stacking blocks source start.

The operator can admit 16th Infantry Brigade at Cairo `e1829` using
`POST /api/v2/campaigns/{campaignId}/source-sixteenth-brigade-grouped-world`.
It retains its HQ and three infantry battalions, existing twenty-point motor
pool and World8 component strengths. After Commonwealth confirms the source
hierarchy, its full, non-shell brigade counter stacks as two. Its four World8
representations are retained in history but suppressed from effective occupancy
in favor of the one group. The pinned content identity is
`sha256:30af5d33f6bc2d03d895b70f0ed088f3147af76dce9de249f465176a82b7f724`.
Both Host system steps and owner choices are re-adjudicated from the retained
journal on replay; they do not start the clock or implement grouped movement.

## Catanzaro grouped source world, version 42

The operator can admit 64 Catanzaro Division at `c4707` using
`POST /api/v2/campaigns/{campaignId}/source-catanzaro-grouped-world`. Its 13
source members and existing 35-point attached motor pool are retained. Axis
confirms the proposed source-default hierarchy through normal source actions.
The proposed stack is unknown until confirmation, then provisionally five for
the full, non-shell printed division counter. The owner confirmation and
source-default hierarchy are provisional; engineer TOE remains unresolved, and the existing
global historical-motorization priority has not been completed. Land §32.83
retains the divisional artillery's Light AA component. The pinned content identity is
`sha256:79aed024e9f7f51375b9401d2968c132a8768dcfa54daa341578bc57de175719`.
The Host's version 97 system step and subsequent owner choice replay against
the prior source authority. This does not create a game clock, movement,
combat or arrival execution.

## Source turn and owner finalization

Each side can explicitly finish or reopen its own source setup through the
normal source action journal. A finished side cannot change its setup without
reopening; the other side's choices and visible opening do not disclose that
decision. System profile changes require both sides to be open. Finalization
checkpoints use successor versions 43 (legacy) or 44 (compact); earlier
checkpoint bytes remain unchanged.

The source-turn authority retains the grouped world, pending arrivals, setup
checkpoint and random stream, with a monotonic journal version and an
independent sequence position. The operator routes
`POST /api/v2/campaigns/{campaignId}/source-turn/start` and
`POST /api/v2/campaigns/{campaignId}/source-turn/initiative` use Host system
steps 98 and 99 and replay by re-adjudicating their events. Start rejects
incomplete battlefield, control, topology, mobility, force and owner setup
evidence. The current historical source world does not pass this gate.

At GT1 convoy scheduling, the source-turn implementation can retain an earlier
GT3 supply plan or roll the September B table and allocate supply to lanes 2,
3 and 6 by operation stage. Lanes 4 and 5 are unavailable until May 1941
(Air/Logistics §56.14), and Bizerta is unavailable in this period (§55.17).
For GT1 planning toward GT3, the source production charts admit no Axis
replacement or motorization cargo. Commonwealth must also roll GT5 supply
under the four-turn rule. Once those plans are complete, the system can record
the scheduling transition to Tactical Shipping using Host step 100. Bounded
GT1 operation-stage-one lane 2 and lane 3 delivery paths require an explicit
Commonwealth decline of attack; lane 3 uses provisional RUL-GRA-05 to correct
the Benghazi coordinate in its successor. Lane 6 can deliver one or two supply
counters to Tobruk after Commonwealth declines attack; a three-counter arrival
remains blocked pending an overflow ruling. Commonwealth's view does not
disclose the hidden quantity or delivery outcome. Attacks remain unsupported.
Both owners can
explicitly decline tactical shipping; shipping plans themselves are unsupported.
Axis can choose to act first or last in stage 1, and Host step 101 records
source-backed weather using the retained random stream. These bounded transitions
still require complete setup admission; their production Host replay cannot yet
be exercised from the current partial source world.

At GT1 operation stage 3, the bounded Polish choice retains eight source
members as one three-point brigade in a source-verified Cairo major-city hex.
The world, battlefield and pending-arrival indexes change atomically; unknown
truck quantities remain unknown. Offers use owner-known occupancy, with
opposing mixed occupancy prohibited under a provisional application of Land
§8.13. Splitting the formation across Cairo hexes is not implemented. Neither
the source turn start nor the stage-three arrival is reachable with current
incomplete historical data; this is not an end-to-end playable arrival.

Scenario §60.41 places the 6th Australian Division, less 19th Brigade and
the two named field artillery units, in training in Cairo and/or Helwan. The
Commonwealth order of appearance p.13 identifies ten initial counters and gives
trained and untrained basic morale as +1 and −3. Neither the exact placement
nor the prior qualifying stages and current morale are specified there. Land
§17.31–36 requires six qualifying operation stages for each morale point,
an eligible area, no capability-point expenditure, and a qualified instructor
present for each stage. Land §4.45 and the Commonwealth unit chart establish
full starting TOE for the infantry battalions and both brigades, so the
colocated division is not a shell and uses its printed stack value of five.
Commonwealth can place this ten-member group together in a source-permitted
Cairo or Helwan hex through the normal setup action and journal. Split
placement is unsupported. The source-turn checkpoint retains an explicitly
unknown training ledger; Commonwealth Organization reports that a starting
ruling and instructor evidence are needed. Both owners can independently
approve or withdraw a provisional untrained/zero-progress baseline; only
consensus sets it. Instructor validation and training progress remain
unimplemented. Commonwealth may interrupt training without assigning an
unknown previous morale or progress.

Land §17.36 makes training optional for individual units. The current source
Organization action interrupts training for the entire colocated Australian
formation; it does not offer independent battalion or cavalry choices. An
interruption does not supply the unknown pre-scenario morale or progress, and
this bounded action does not certify a subsequent Organization transition.

One possible instructor is a battalion of 16th Infantry Brigade: the original
Commonwealth OA p.9 lists 2 Queens, 2 Leicesters and 1 Argyll under 70th
Division's printed +1 basic morale, with normal TOE, and scenario §60.41 places
the brigade at E1829. Land §17.1, however, qualifies inherited parent morale
when a unit is attached elsewhere or has operated on its own; OA note d says
the brigade was originally attached to 4th Indian Division. The live source
state does not yet establish the required effective morale and attachment
history. Even where the Australian group is placed at E1829, same-hex presence
alone does not certify an instructor. Land §17.35 also requires a tank
battalion for a unit with tanks; the source envelope does not yet establish the
Australian divisional cavalry's equipment well enough to certify that one
infantry instructor qualifies the whole group. Eligible instructor identity,
at least half assigned TOE, and presence throughout each completed stage must
be derived from authoritative state before training progress can advance.

## Independent Axis minor garrisons

The source setup can activate four evidenced minor garrisons as separate
native World8 counters: Scheferzen at C3419 (TOE 1, printed stack 0), Fort Maddalena
at C3019 (TOE 2, printed stack 1), El Grein at C1715 (TOE 1, printed stack 0), and Mechili at
B4921 (TOE 2, printed stack 1). Each has 15 capability points. Axis OA p.14,
Italian chart §4.46b and the Axis counter sheet establish these characteristics;
unknown mobility and parentage remain unknown. These counters are not a
formation, and the remaining garrison evidence is not activated by this step.
All four have a G counter marking. Land §9.16(a) gives them effective stack 0
at their own assigned city or village; their printed values apply elsewhere.
The content retains both values and the location restriction. General native
movement and stacking enforcement for these units remain incomplete.

The `sandtable.source-axis-minor-garrisons.v1` content successor retains the
prior native content and source component facts. The source setup system event
`source-axis-minor-garrisons-activated` updates native elements, independent
representations and battlefield statuses together. It preserves grouped
formations, pending arrivals and random state. A version 60 checkpoint wraps
the retained setup checkpoint and binds successor content by hash; legacy
checkpoints also embed the content, while compact checkpoints regenerate it
from the verified event journal. Earlier checkpoint bytes remain unchanged.
Host step 106 journals this transition and re-adjudicates it
on replay. Activation requires the free-gun world and at least one grouped
formation, with both owners' setup still open.

In one real-source local capture the compact version 60 checkpoint measured
1,561 bytes, including a 926-byte base checkpoint; its canonical content artifact
measured 2,571,417 bytes. Compact rows retain its hash, avoiding repeated
embedding of the artifact's full predecessor chain. The world and legacy
projection hashes stream their existing canonical bytes to SHA-256 rather than
materializing the complete outer serialization. Nested content artifacts and
the legacy projection's base64 world field still allocate; this is not a claim
that all replay allocations are eliminated. Immutable CW free-gun world instances
memoize their canonical hash with thread-safe first computation; successor
instances compute their own hashes and retain the same serialized bytes.

These four counters do not complete the historical world or certify map seams,
control, movement, combat or campaign start. Mechili's B/C seam and other
unrepresented source units remain unresolved.

The version 61 successor activates six additional independent Axis counters:
Giarabub infantry, machine-gun, anti-tank and artillery, and Soluch anti-tank
and artillery. Original Axis OA p.14 supplies their
locations and initial strengths, the Italian unit chart pp.26–27 supplies
their capability allowances, and counter section 2 p.40 supplies the G marks
and printed stacks. The Soluch map A hex A4130 is a village. These counters
retain their separate identities and source component facts. Their G marking
gives effective stack 0 at the assigned site under Land §9.16(a); Soluch
artillery and Giarabub MG each have printed stack 1, which applies elsewhere.
The motor MG remains evidence
only because its `8+` motorization/vehicle handling is unresolved. The Giarabub
and Benina light AA counters have Italian ID code `uu` and are explicitly
removed from play under Land §32.83(1); they remain recorded source evidence,
not native counters or observers. Host system step 107 retains and replays the versioned successor;
it requires the existing compact-checkpoint transition and both owners' setup
to be open. The compact version 61 checkpoint binds the pinned content hash and
reconstructs content from the retained event stream. This avoids duplicating
the large content predecessor chain in every row. It does not certify readiness,
complete the map, or advance the game clock.

The optional version 62 setup successor retains Giarabub's motor MG separately
at C1014 with one infantry TOE and the counter's printed stack value of zero.
The original OA calls it a battalion, while its `hh` chart row is company
equivalent and marked `8+`. The `+` documents historical motorization but
does not supply a scenario pool or truck count. The successor retains `8+`
as printed evidence and records current capability allowance, mobility, pool
and assigned motorization points as unknown. It creates no motor points and
does not resolve historical-motorization priority. Source-turn start remains
blocked on that priority and the unknown allowance/mobility. Its separately
represented, source-pinned counter can be an independent adjacency-contact
observer: observation requires active identity and location, not a resolved
motor assignment or capability allowance. This does not establish movement
or ZOC. Host
step 108 replays this setup event; it does not advance the game clock.

The optional version 63 successor admits 1 RTR at C3520 and 11 Hussars at
C3020 as separate Commonwealth counters, with respectively seven A9 TOE and
six reconnaissance TOE. Their original printed stack values are one; chart
allowances are 25 and 45. The reconnaissance chart specifies armor 1; the
11 Hussars vehicle model and breakdown profile remain unknown. Scenario §59.2 identifies `R` as reconnaissance;
parent labels identify the counters and do not establish current attachments.
Existing attached motor pools of five and three points are retained without
creating or transferring trucks. Optional initial loads remain unresolved and
continue to block source-turn start. The separate Alexandria broken-vehicle
ledger is preserved and is not subtracted from 1 RTR's active strength. Host
step 109 records this setup transition for deterministic replay; it does not
advance the game clock or certify map, movement, combat or overall readiness.

The frontline-load setup extension uses the existing Commonwealth setup
opening. Once the version 63 counters are active, the owner may assign the
five C3520 and three C3020 medium-truck points to their respective counters,
and choose ammunition, fuel, or an empty load for each point. Source scenario
§§59.42, 59.63(A), 59.66(B), and 60.41 establish these pools and choices.
The owner explicitly finishes the choice after assigning all eight points.
Frontline changes have their own versioned event and load-state identity;
older attached-load events and checkpoint bytes retain their original meaning.
This records setup only: consume-first and loaded detachment are not yet wired
for these counters. The frontline load blocker clears only after the owner
assigns all eight points and finishes with every point empty. It remains in
force for incomplete choices or any ammunition or fuel load. Other map,
movement, force and Organization blockers still prevent game start.

The `source-frontline-initial-loads-changed` event has contract version 64 and
records campaign, prior state version and checkpoint hash, action ID, actor,
choice kind, element ID, `frontlinePolicyId`, load-state hash and existing
attached-load profile hash, in that order. Kinds are `frontline-attach`,
`frontline-detach`, `frontline-ammo`, `frontline-fuel`,
`frontline-unload-ammo`, `frontline-unload-fuel`, `frontline-finish` and
`frontline-reopen`; finish and reopen use element ID `frontline`. Load-state
contract version 2 adds `frontlineFinished` and the policy ID
`graziani.cw-attached-initial-loads.frontline.v2` to the existing target
records. Version 1 load-state bytes remain unchanged until the first frontline
choice. The existing journal re-adjudicates each choice from the retained
predecessor and checks the full canonical event.
The Commonwealth-only load opening uses contract version 2 after the frontline
counters are active and reports each pool's total and assigned points alongside
the load choices and completion state. The prior version 1 opening is unchanged
before frontline activation.

## GT1 opening organization ruling

The opening Organization phase uses a provisional ruling approved for this
campaign: construction projects are not assumed to have been in progress before
GT1 unless the scenario explicitly lists them. This bounded path requires the
Commonwealth to interrupt the Australian training and both sides to decline
their remaining voluntary organization choices before the operator completes it.
The source-turn event records the ruling's versioned action ID and replay
re-adjudicates both choices. The interruption remains in the training ledger;
continuing training and its later progress require additional authority.
This transition does not remove campaign-start readiness blockers.

The GT1 stage-one convoy-arrival phase has a journaled completion step when
all due Axis lanes have actually been delivered, the Commonwealth explicitly
skipped its opening supply plan, and no unit arrival remains due in either
retained schedule. A rolled Commonwealth opening plan, undelivered convoy or
due unit keeps the phase open. Commonwealth supply allocation and general
convoy attack and overflow handling remain incomplete.
Following that bounded arrival path, one journaled step passes the GT1
stage-one fleet assignment and repair segments: scenario §60.45 starts the
Commonwealth ships undamaged and prohibits moving them until GT2. This path
does not resolve fleet use in later turns or attacks during convoy arrival.
At the subsequent first-player reserve designation, the acting owner may
decline reserves. This normal owner action materializes that player's movement
segment and is replayed from its event. Under source-turn contract 3, the
acting owner may finish that movement segment; the successor is breakdown
determination, with world, supply, movement expenditure and contacts retained.
The action does not skip breakdown or combat. Designating reserves and the
Axis opening movement remain unsupported. Campaign-start readiness remains blocked.
The first breakdown segment may be completed only when retained source state
proves that no counter moved or that the sole move was the exact 4th Indian
opening move, which contains no breakdown-eligible vehicle cohort. Any other
movement or nonzero initial movement expenditure remains blocked. The step
retains the random stream and advances to combat position determination.
