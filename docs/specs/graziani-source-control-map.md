# Graziani map control: source and implementation boundary

This note separates three meanings of control needed by the real scenario.
It does not certify the map, enable campaign start, or infer the controller
of an unrepresented location.

## Land zones of control

Land rules §§10.0–10.28 (original rules PDF pp. 17–18) define controlled
hexes as the six adjacent hexes into which a unit exerts a zone of control
(ZOC). A hex can be in both sides' ZOCs. A friendly combat unit in a hex
negates the enemy ZOC's effect for movement; it does not erase the underlying
enemy ZOC. Qualification depends on unit classification, more than one
stacking point in the hex, cohesion, and at least ten raw defensive close
assault points. All-sea, major-river, lake, and escarpment hexsides block
projection, as does a destination the projecting unit cannot enter (§10.21).
These are dynamic properties of the active force, terrain and hexsides,
not a blanket political assignment of hexes by country.

Original Land §10.16, independently rechecked on printed page 18, requires
the non-phasing player to disclose whether a particular unit exerts ZOC when
it could affect a hex in which a phasing unit begins a Movement Segment or
into which it moves. The implementation therefore needs a journaled,
movement-triggered disclosure at that limited point. Neither a blanket
pre-movement query of hidden positions nor silently withholding a required
ZOC answer implements the rule. Source counter identity, eligibility,
geometry, timing and player-visible payload still require explicit authority.
Under §§10.22–10.24, entering an enemy-controlled hex is not itself forbidden:
the unit must stop, and movement directly from one enemy-controlled hex to
another is restricted. A legal destination list must therefore not hide an
otherwise legal entry merely because an undisclosed enemy ZOC exists. The
accepted move, stop consequence and required disclosure need one replayable
authoritative transition, with the friendly-combat-unit exception applied
according to §10.26.

Stacking must use the current position and source counter rules. Land §9.16(a)
says garrison counters marked `G` stack freely in their assigned city or
village, and gives Fort Maddalena's Italian heavy-weapons battalion as the
example: printed one, effective zero at Fort Maddalena, printed one elsewhere.
The original counter sheet marks Scheferzen, Fort Maddalena, el Grein and
Mechili garrisons `G`, and prints their assigned locations on the counters.
Their printed stacking values are respectively 0, 1, 0 and 1; their effective
values at their assigned initial sites are all zero. This rule concerns
stacking; it does not independently determine their combat strength or ZOC.

## Facilities and victory are separate

Scenario §60.5 assigns **air facilities** in Egypt to Commonwealth and those
in Libya to Italy initially. It does not assign every hex or every city.
Land §30.56 defines port control for that naval transport rule as the last
player to occupy the port. The first scenario's victory conditions in §60.81
require specified locations to be held by qualifying units and supplied; they
cannot be decided from initial country boundaries alone. These categories
must retain their own rule-specific authority and change as play proceeds.

## Existing engine and missing integration

`Cna1979Zoc` supplies source qualification and hexside projection rules.
`CampaignElementMovedV3Factory.DeriveZocAuthority` applies them to a
`CampaignWorldSnapshotV6` with independent representations, combat component
facts, locations, edges and mobility. It explicitly rejects grouped counters,
unknown attachments and incomplete combat or terrain facts. The real source
campaign uses World8 plus `CampaignGroupedWorld`, so it cannot safely reuse
that world-level projector unchanged. `CampaignSourceLocalMapWorld` currently
retains only two Map C neighborhoods; its terrain and hexside effects remain
unknown and its complete-map and movement flags are false.

To make real source movement and contact depend on authoritative control:

1. Retain the source-verified locations, reciprocal edges, terrain, transport
   and hexside effects under a versioned map content identity; resolve or
   explicitly block every cross-section edge. The §37.42 distance chart is
   not a legal land-route or terrain certificate.
2. Derive each grouped or independent force's effective stacking, cohesion,
   classification, current TOE and raw defensive points from its actual
   live composition and attachments, including site-specific garrison rules.
   Unknown facts must remain unknown; no missing force may be treated as
   absent or as zero strength.
3. Apply `Cna1979Zoc` qualification and projection to the current authoritative
   World8/grouped state and verified map edges, then apply movement's friendly
   occupation exception separately. Recompute after movement, combat, losses,
   formation changes and arrivals; ensure owner-visible legal actions do not
   disclose hidden opposing state.
4. Bind the resulting input content, world version and event effects to the
   normal journal and replay. Verify partial-data rejection, reciprocal edges,
   exact threshold cases, garrison at and away from its site, simultaneous
   ZOCs and movement exception with real-source data.

The existing independently represented V6 path and its
`GrazianiZocSourceTests` are useful rule evidence, not proof that grouped
Graziani World8 control or a complete playable map is implemented.

The initial-contact projector can prove separation from the four supported
Matruh-area setup hexes for the exact chart endpoints Soluch `a4130`, Benghazi
`a4827` and Derna `b5925`. Air/Logistics §37.42 gives their shortest distances
to Matruh `d3714` as 85, 91 and 66 hexes; the source-read Matruh neighbor
paths reach each supported setup hex in at most four steps. This is a geometric
nonadjacency proof used by the normal source contact path, not a terrain,
movement, ZOC, control or cross-section identity certificate. In particular,
it does not silently change the scenario's conflicting `b4827` entry.

The source contact projector also recognizes the Map C hexside `c1014`–`c1015`
at Giarabub in both directions. This verifies adjacency for presence contacts
only; it does not establish terrain, movement legality, supply or ZOC.

## Shortest route to a source-backed playable map

The repository now embeds a pinned third-party historical map transcription;
its provenance and coverage limits are recorded in
`graziani-historical-map-transcription.md`. It is not certified as complete
movement or ZOC authority. The five-sheet original scan used for
this review is currently available at `/private/tmp/sandtable-maps.pdf`, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
That temporary path is not a durable project input. Record an accessible,
verified source copy and its rights before relying on continued availability;
the identity above allows a future copy to be checked. Scans should stay outside
Git unless their rights are resolved, as specified in the map-identity pilot.

The original Land §4.1 describes a 01xx/39xx section overlap. The original
Air/Logistics §37.42 distance chart says identical coordinates on adjoining
sections are 33 hexes apart. A single-reader comparison of a candidate 33-column
translation against 190 chart pairs matches 179 and differs on 11. The precise
pairs, printed values and candidate values are in
`docs/work/plans/2026-11-12-graziani-map-identity-pilot.md` under “Full
distance-chart comparison”. Neither the candidate nor the chart alone proves
overlapping cell identity, cross-section edges, terrain or a traversable route.
These facts need independent reconciliation against the original sheets and
applicable corrections before granting seam authority.

Use the existing content and runtime paths; the smallest complete production
sequence is:

1. **Freeze source scope and transcribe.** Obtain a durable verified copy of
   each required sheet and corrections. Inventory setup, arrivals, objectives,
   facilities and every hex and edge within the supported playable area. Enter
   section/printed coordinate, terrain, weather area, transport features,
   coasts and blocking hexsides with source references. Use image/grid tooling
   to assist extraction, but review the transcription against the scans and
   independently check discrepancies. The imported transcription is not yet
   certified as complete, and the repository does not supply its own historical
   map digitizer. The required extent and counts are not yet established.
2. **Resolve identity and completeness.** Compare overlapping map art and labels,
   rule wording, errata and the 11 chart disagreements. Record each verified
   same-hex identity and cross-section neighbor; do not merge two labels merely
   because a distance candidate suggests proximity. For every admitted cell,
   distinguish a verified edge without features from an unreviewed edge, and
   distinguish verified map boundary from a missing neighboring cell. Block
   movement and ZOC derivation wherever those facts remain unknown. A deliberately
   scoped playable area is viable only if legal movement and incoming ZOC at its
   boundary are accounted for, rather than treating omitted terrain or units as
   absent.
3. **Publish versioned content.** Populate existing `ContentHex`,
   `ContentSourceCoordinate`, `ContentHexEdge` and `ContentEdgeFeature` records
   with stable source-neutral IDs and source origins; bind the canonical data
   and coverage to a new content identity, preserving prior identities for
   replay. `ContentHex` requires a terrain ID and `ContentHexEdge.Features` can
   be empty, so the present topology records alone do not distinguish unknown
   from verified clear. Add explicit, versioned coverage semantics or an
   equivalent fail-closed admission contract before consuming partial data.
   Keep unresolved seams distinct and unavailable to movement.
4. **Connect actual play.** Load verified map content into the normal source
   world, derive grouped World8 ZOCs and legal movement from current units and
   map facts, and journal events with the exact content identity. Remove the
   readiness blockers in `CampaignSourcePlayableWorld` only when their stated
   domains are supported, not when a small excerpt has been added. Existing
   Cairo city and Fourth Indian road excerpts already support their limited
   consumers and do not provide general map coverage.
5. **Validate admission and replay.** Check scenario references, unique
   coordinate/identity mappings, six-neighbor limits, reciprocal edges, terrain
   and feature completeness, physical boundaries, source-supported seam joins,
   and known sites and road/river/escarpment examples. Include rejection of
   missing/conflicting coverage, boundary ZOC, and changed content on replay;
   then exercise actual Graziani creation, movement and Host journal replay.

Source inventory, transcription, discrepancy checking, importer/validation
design and fail-closed runtime integration can proceed without choosing a
provisional historical ruling. If source evidence does not settle a seam, it
remains blocked until an explicit ruling is made and recorded in the content
identity; no candidate translation silently becomes historical fact.
