# Fog of war for Sandtable's real campaign engine

**Status:** research recommendation for the permanent game feature, 2026-10-21.
This document selects one architecture for campaign visibility and Combat. It
does not introduce a special RC visibility profile or turn private evidence
into an admitted action. No code or public route is changed by this research.

## What other games do

| Game | Relevant behavior | Lesson for Sandtable |
| --- | --- | --- |
| Wesnoth | Fog is side-specific. A seen hex can reveal a unit, and fog returns when sight is lost. Visibility filters explicitly take a viewing side. | Visibility is game state for an audience, not a Maproom drawing effect. |
| 0 A.D. | Explored terrain and last-seen objects can remain on the map, while enemy units under fog disappear. | Separate map knowledge, current enemy contact, and stale information. |
| Freeciv | Server tracks seen state per player, keeps last-seen tile knowledge, and notifies clients when units leave sight. | Core/host send a side-scoped projection; clients never decide what is visible. |
| Combat Mission | Units distinguish possible contacts from confirmed enemies. Last-known locations can persist; spotting may be shared through command communications rather than instantly across all units. | Preserve observer and sharing provenance, even when the commander sees a combined map. Do not treat a stale marker as a confirmed attack target. |
| OpenRA | Shroud/fog, visibility effects, and remembered actors are reusable game-system traits. | Implement one reusable rule path for campaigns, not a Combat-only override. |

Primary sources: [Wesnoth fog](https://wiki.wesnoth.org/Fog_of_war) and
[viewing-side filter](https://wiki.wesnoth.org/FilterWML);
[0 A.D. exploration manual](https://play0ad.com/6-exploring-the-map/);
[Freeciv server vision code](https://files.freeciv.org/nightly/weekly/main/doxygen/html/d1/d14/maphand_8c_source.html);
[Combat Mission manual, spotting and C3, pp. 29–30](https://ftp.matrixgames.com/pub/CombatMissionShockForce2/CMShockForce2BaseGameManual.pdf);
[OpenRA shroud and visibility traits](https://docs.openra.net/en/playtest/traits/).
These show design patterns, not the authority for Sandtable's own game rules.

## Fit to Sandtable

Sandtable already publishes campaign locations and edges in its observation
contracts. For the current game, fog's essential job is to control **enemy
force information**, not to hide map geography. Own units and published map
remain known. Enemy units, exact locations, identity, strength and intent are
separate disclosure decisions.

`CampaignObservationV7Projector` currently requires valid Snapshot 11 authority,
accepts caller-supplied apparent-control/ZOC facts, and constructs apparent
opposing positions from World representations. `CampaignLegalActions` requires
admitted snapshot authority. Neither is a proven P01 side-contact source. The
original 300 R03 roots contain private World and replay inputs but no
authenticated, same-cut record of what the attacker observed. They remain
44 provisional positive, 256 absent, zero admitted. Building the real
visibility system creates new authoritative contact events; it does not
retroactively add them to old records.

## Decision: one campaign visibility model

Make **contact-based fog** the default campaign rule and the only Combat
visibility path. Core owns four related states:

1. **Truth:** actual World positions, identity, resources and hidden Combat
   details. Only Umpire may read this for adjudication and detection.
2. **Knowledge:** side-scoped contact history, each with observer formation,
   source event, cut, apparent hex, identification level and sharing status.
   A last-seen contact is explicitly stale; it is not current enemy position.
3. **Observation:** the current player view projected from knowledge and own
   state. It contains only approved contact fields and public map topology.
4. **Available intents:** the actions offered to the acting formation from its
   accessible contacts and own state. Core resolves private targets and
   revalidates on submission; Maproom never calculates legal targets.

Use an opaque, side-scoped contact handle. Do not send opposing element IDs,
raw World positions, actual strength, or private source hashes to clients or
Intelligence. The contact record can retain private opponent binding inside
Core; its visible form contains apparent location and identification level.
Store which formation observed a contact and how it reached the acting
formation. The commander may see aggregated side knowledge, while action
eligibility can require local observation or explicit information sharing.
This accommodates command-and-control rules without a later data-model rewrite.

### Contact lifecycle

Core emits typed `observed`, `updated`, and `lost` contact events while
adjudicating creation, movement, detection, combat and information sharing.
Events bind campaign, side, observing formation, position/state cut, source
event, rule version and public contact handle. Replay reconstructs knowledge
deterministically. Loss changes a current contact into an explicitly stale
last-known marker. New sight can update or replace it; client code never
infers contact from a private World field or a nearby marker.

The permanent detection policy should use source-backed movement/contact and
combat events first. Adding LOS, weather, concealment, sensors and C3 sharing
means extending the versioned visibility rules used by the same event
pipeline. A rule branch without a defined result must close the affected
action; it must not silently reveal everything. Published terrain remains
available to movement even where enemy presence is unknown.

### Combat behavior

For a direct assault, the acting formation needs a **current actionable
contact** at the apparent target hex and the normal Combat resources/rules.
The player chooses a contact/hex intent; Core privately resolves and
adjudicates the actual defender. A stale contact is displayed as last-known
intelligence but is not a direct-assault target. Movement toward an unknown or
stale hex remains possible and may generate a new contact or surprise
engagement through normal adjudication. If the game later supports area fire
or blind assault, those are distinct permanent action rules, not a way to
pretend stale knowledge is current sight.

Core derives the observation and its action set at one authoritative cut.
Chronicle durably stores the contact transition and Host's owner opening
before a player sees an action. The opening binds the side, seat, cut,
observation digest, public aliases and deadline. On submission, Host
authenticates and fences the request; Core verifies the contact/action is
still current and commits one result and receipt. Retry reads back the same
opening or receipt. Maproom displays current contacts, stale markers and
available intents from that one owner view; Intelligence receives only the
same side-safe information.

## Build the feature in the real path

1. Write the versioned **campaign visibility rule** and contact/event schemas
   into Core/Rules and Core/Observations. Specify initial detection from
   authentic movement/contact/combat events, contact loss, identity levels
   and information sharing. Do not add an alternate RC profile.
2. Make one Core observation projector consume replayed contact knowledge,
   replacing caller-supplied apparent target facts for the affected campaign
   path. Generate direct-assault intents from that projection. Keep private
   target mapping inside Core.
3. Integrate the same observation and action set with Host's durable
   seat/opening/accepted-input journal, then Maproom's normal campaign flow.
   No separate test client is the product path.
4. Test concrete behavior: unseen defender absent from player view; current
   contact appears; lost contact becomes stale; stale marker cannot be
   selected for direct assault; two seats see only their own knowledge;
   restart returns the same opening; accepted input resolves once. Include
   paired hidden-state privacy tests, but do not delay basic implementation
   for a large synthetic proof program.
5. Capture new authenticated campaign histories from this actual feature and
   run the Combat support certificate on them. Keep the old 300 roots as
   replay regressions, without treating them as visibility proof.

This is one engine rule and one user path. The first implemented contacts can
come from adjudicated movement and combat; later detection inputs extend the
same rule system. There is no throwaway visibility mode to remove before RC.
