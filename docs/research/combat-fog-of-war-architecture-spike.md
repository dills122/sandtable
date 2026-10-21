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

Core emits typed `observed`, `updated`, and `no-longer-observed` contact events
while adjudicating creation, movement, detection, combat and information sharing.
Events bind campaign, side, observing formation, position/state cut, source
event, rule version and public contact handle. Replay reconstructs knowledge
deterministically. The outward contact handle is side-scoped and cannot encode
an opponent ID. A contact becomes stale only when the observer can establish
loss of sight or contact: its own sight changes, an enemy leaves a continuously
observed area, or a public observation interval expires. An enemy moving while
unobserved causes no side-visible event, revision, marker change, or action-set
change. When the side later observes the last-known hex empty, Core may retire
that marker based on the new observation. New sight may update or replace a
contact; client code never infers one from private World state.

Specify this as a four-transition rule before freezing schemas: initial
detection, continued observation, audience-observable loss, and rediscovery.
For each transition identify observer, source event, visible evidence and
timing. Pair two histories with the same owner-visible observations but
different unseen enemy movement; they must yield the same visible contact
state and revision until an actual new observation distinguishes them.

The permanent detection policy should use source-backed movement/contact and
combat events first. Adding LOS, weather, concealment, sensors and C3 sharing
means extending the versioned visibility rules used by the same event
pipeline. A rule branch without a defined result must close the affected
action; it must not silently reveal everything. Published terrain remains
available to movement even where enemy presence is unknown.

### Combat behavior

For a **voluntary direct assault**, the acting formation needs a current
actionable contact at the apparent target hex and the normal Combat
resources/rules.
The player chooses a contact/hex intent; Core privately resolves and
adjudicates the actual defender. A stale contact is displayed as last-known
intelligence but is not a direct-assault target. Movement toward an unknown or
stale hex remains possible and may generate a new contact or surprise
engagement through normal adjudication. If the game later supports area fire
or blind assault, those are distinct permanent action rules, not a way to
pretend stale knowledge is current sight.

Fog cannot erase a source-required action. Core determines mandatory
obligations from truth before generating voluntary choices. A ZOC or other
forced-assault trigger needs its own source-backed transition: disclose only
the contact/location and obligation the actor must know, then present the
required choice or resolve a System step only if that rule actually permits
one. Surprise engagement interrupts movement at the detected cut, records
the contact, and then uses the same Combat decision path. Hidden participants
remain private bindings behind the apparent contact. The exact Land-rule
trigger, disclosure and action for each family must be pinned in the ruleset
contract before its capability is enabled. If a family is not yet supported,
reject that capability at campaign activation; never reach a live turn and
silently omit its mandatory action. This is capability coverage within the
single game rule, not a second visibility mode.

Core derives the observation and its action set at one authoritative cut.
Chronicle durably stores the contact transition and Host's owner opening
before a player sees an action. The opening binds the side, seat, cut,
observation digest, public aliases and deadline. Its private fence carries
the authority cut; the outward opening carries only a side-audience revision
and stable public decision/action references. A hidden authority transition
may advance the private cut without changing the audience revision, opening,
notification or available actions. On submission, Host authenticates and
fences the request against the retained opening. An offered, timely action is
accepted against that side-visible opening; hidden truth is adjudicated
inside Core, not returned as a new stale/illegal rejection category. Only a
source-authorized, actually observed result may distinguish the histories.
Retry reads back the same opening or receipt. Test byte-identical outward
openings, notifications, offers and generic rejections for equal-visible
histories with different hidden transitions. Maproom displays current
contacts, stale markers and available intents from that one owner view;
Intelligence receives only the same side-safe information.

### Capability admission

At campaign activation, Core freezes a closed capability manifest for the
selected Land rules/config: movement and ZOC families, surprise/forced
engagements, unit and weapon classes, terrain/weather/detection rules,
information-sharing rules and any optional variants. The manifest includes
all families that can become reachable later from that configuration, not
just conditions true in the starting World. Each enabled family must map to
a versioned contact/disclosure transition, a defined owner or System action,
and an adjudication handler. Unknown or uncovered families reject activation
with a generic owner-safe result. A rules/config change or restored campaign
must revalidate the same closure before it can produce another turn; it cannot
silently enable a new family mid-campaign. Tests cover every declared family,
the first state where each becomes reachable, and an unknown-family negative.

### Migration and publication

Inventory every outward reader before enabling a fog-enabled campaign:

- `CampaignObservationV7Projector` and its V6/legacy relatives: identify
  every opposing-position read from World and every caller-supplied apparent
  fact. Replace these with replayed Core contact knowledge for new campaigns.
- `CampaignLegalActions` and observation-derived action code: derive the
  offered set from the exact same side cut, including mandatory obligations.
- `CampaignAudienceOutcomeSource`, the Host's process-local
  `TemporaryAudienceTracker`, and `/api/v1/campaigns/{campaignId}/view`:
  replace temporary contact continuity with the durable Core projection for
  successor cuts before public use.
- Maproom, War Diary, DecisionWorker/Intelligence, replay and Exercise
  adapters: consume only the versioned side projection, with no fallback to
  raw World or a separate contact list.

Existing snapshot and observation versions remain readable for historical
replay. New activations select the new visibility-policy version; they must
not combine a new contact action set with an old World-derived observation.
The frozen [Combat side projection v1](../specs/combat-side-projection-v1.md)
uses an apparent opponent location declassified from World. It remains a
historical/private P01 codec, not the new campaign visibility contract. Before
`CMB-TASK-020` implements any outward path, freeze a **versioned successor to
`CMB-CON-005`** with the contact lifecycle, side-audience revision, public
opening and submission rules above. Add it as an explicit Task 020 dependency
in the [Combat delivery plan](../design/combat-cycle-implementation-plan.md).
Existing V7/V6 observation consumers and the frozen P01 codec remain
historical readers only; new campaign observation, actions and Host/Maproom
publication consume the successor together.
The publication gate is a same-head Core observation/action pair, source-legal
forced/surprise cases, two-side privacy tests, durable opening/receipt restart,
and a normal Maproom read/submit path. Until every outward reader is on this
path or explicitly gated as historical, do not call it the production default.

## Build the feature in the real path

1. Freeze the `CMB-CON-005` successor and versioned **campaign visibility
   rule** before Task 020; write contact/event schemas into Core/Rules and
   Core/Observations. Specify initial detection from
   authentic movement/contact/combat events, continued sight, observer-visible
   loss, rediscovery, identity levels and information sharing. Pin forced ZOC,
   surprise engagement and hidden-participant transitions against source rules
   before enabling those capabilities. Freeze the complete activation
   capability manifest and reachability check. Do not add an alternate RC
   profile.
2. Make one Core observation projector consume replayed contact knowledge,
   replacing caller-supplied apparent target facts for new campaigns. Generate
   voluntary direct-assault intents from current actionable contacts and
   mandatory Combat choices from the source-backed forced transitions. Keep
   private target mapping inside Core. Reject unsupported capability families
   at campaign activation rather than stranding a live turn.
3. Integrate the same observation and action set with Host's durable
   seat/opening/accepted-input journal, then Maproom's normal campaign flow.
   Replace process-local contact tracking. Inventory and migrate or gate all
   outward observation, legal-action, Intelligence, diary and replay readers
   listed above. No separate test client is the product path.
4. Test concrete behavior: unseen defender absent from player view; current
   contact appears; observable loss makes it stale; unseen enemy movement
   changes no owner-visible revision, marker, offer or rejection; stale marker
   cannot be selected for direct assault; source-required ZOC and surprise
   cases reach their defined choices; hidden participants stay private; two
   seats see only their own knowledge; restart returns the same opening;
   accepted input resolves once. Check authority-cut/public-revision
   separation and byte-identical openings, notifications and generic
   rejections. Include paired equal-visible histories with
   different hidden movement as a focused regression, without turning the
   implementation into a separate synthetic proof program.
5. Capture new authenticated campaign histories from this actual feature and
   run the Combat support certificate on them. Keep the old 300 roots as
   replay regressions, without treating them as visibility proof.

This is one engine rule and one user path. The first implemented contacts can
come from adjudicated movement and combat; later detection inputs extend the
same rule system. There is no throwaway visibility mode to remove before RC.
