# Source-content campaign creation, version 1

**Status: initialization-only.** This is not an admitted campaign.

The normal Core creation entry point and durable Host journal can retain a
source-content campaign's initial state with its exact scheduled-combat content
bytes. This is an **initialization-only** capability: it does not establish
that a scenario is complete, independently reviewed, playable, or approved for
release. Existing version-1 creation, snapshots, actions and readers are unchanged.

The version-2 creation request binds campaign ID, ruleset hash, seed, setup ID
and hash, pack ID and hash, and scenario ID. Its setup hash also binds the
explicit initial operational policy. The source content's canonical bytes,
format identity and exact hash are retained in the creation event. Core validates
the artifact structure, source scenario, request identity and World8 initial
state. The initial checkpoint contains the canonical placed World and pending
arrival group IDs. Pending members are absent from placed World and observation.

The creation event is a canonical JSON object, in this field order:
`contractVersion`, `eventType`, `campaignId`, `rulesetHash`, `seed`, `setupId`,
`setupHash`, `formatId`, `packId`, `rulesetId`, `contentHash`, `scenarioId`,
`initialization`, `contentBase64`. Its event type is
`source-campaign-initialized`. The initialization object carries
`capabilityNumerator`, `capabilityDenominator`, `cohesion` and `reserve` in that
order; only `reserve: none` is supported. The initial checkpoint contains
`contractVersion` 8, `campaignId`, `contentHash`, `scenarioId`, `world` and
`pendingArrivalGroupIds`, in that order. Group IDs are sorted ordinally. The
checkpoint carries no gameplay position or random cursor; the seed remains bound
in the creation event. Replay reconstructs the state and compares exact bytes.

The existing `campaigns` and `accepted_steps` journal records remain the sole
durable store. A cold read verifies the hash chain and recreates World8 from the
retained content and request, byte-comparing the resulting creation and
checkpoint. Repeated identical creation returns the retained state; conflicting
content or initialization cannot replace it. Existing campaign records and
their legacy replay path retain their frozen behavior. No command is accepted
for an initialization-only campaign until a later, versioned authoritative
transition implements it. Replay also rejects any retained seat, owner opening,
owner action or decision-round row for an initialization-only campaign.

The development-only local Host exposes operator-authenticated
`POST /api/v2/campaigns/source`, accepting canonical source-content bytes, their
expected hash, scenario ID, seed, ruleset hash and explicit operational values.
`GET /api/v2/campaigns/{campaignId}/source-initialization` is operator-only and
returns identity, initialization status and pending-group count, not formation details.
The normal seat-grant route refuses these records, and seat redemption rejects
them even if a stray grant row is present. A successfully created record is
durable source initialization, not an offer to play the scenario.

The next playable successor must extend `CampaignExercises` and the normal
`CampaignHostAdapter.Import` event dispatch with a source-capable content context,
gameplay position, random state and pending ledger. Host `accepted_steps` replay
must then verify those events and their checkpoints; the present source branch
explicitly rejects all accepted steps. Existing V11 saves retain their current
validation and replay behavior.
The separate version 3 [pre-game supply cut](graziani-pregame-supply-v1.md) adds
seeded, Axis-private partial planning and ordinary Host accepted-step replay on
the source excerpt. It is not the playable successor described above: it cannot
complete setup, advance to the campaign's opening turn, or execute arrivals.
Initial broken equipment also needs a successor representation: current
`CampaignBrokenVehicleLot` requires a later breakdown check and cannot encode
the vehicles already broken at scenario start without inventing that history.

The [version 4 source authority](graziani-source-authority-v4.md) follows the
version 3 pregame by committing the convoy plan and recording bounded source
setup decisions for both sides. Its retained initial-force evidence and supply
ledgers remain incomplete; it does not yet provide that playable successor or
an authoritative game clock.

This contract enables preserving real source-backed inputs and deferred units
in the normal journal. It does not itself supply a complete Graziani artifact,
perform source review, decide setup choices, create a playable scenario, or
execute an arrival.

When an arrival successor is implemented, the schedule's group identifies its
source formation; it does not force every member into one hex. The Commonwealth
arrival chart permits same-parent units to share a hex subject to stacking and
allows the player to choose attachments. The transition must validate each
member's chosen entry location, any chosen relationships and applicable stacking
against the admitted world. The initialization record does not preselect them.

The separate source-content format preserves `land.terrain.major-city` for
source-backed Cairo hexes. It does not add that terrain to the frozen current
Movement ruleset or make it traversable. Legacy scheduled-content formats
continue to reject it.
The original Map A terrain-effects chart §8.37 lists Major City entry costs of
1 CP for non-motorized units and 1/2 CP for motorized units, with stacking limit
8. Those readings are not implemented by the current movement artifact; city
fortification and other applicable effects also need a versioned rules capability.
The same chart identifies Alexandria and Cairo as Level Three fortifications,
so a city movement cost alone would not complete Cairo gameplay support.
Source initialization may also retain disconnected known hexes while map edges
are untranscribed. This is incomplete source data, not playable topology; all
other topology consistency checks still apply, and legacy formats retain their
connected-map requirement.

The production `Cna1979GrazianiCairoSourceExcerpt` builder supplies the same
partial source artifact used by the tests. It is not a complete or distributable
Graziani content pack, and its availability does not admit a playable campaign. Its counter
allowances join OA ID codes to the printed Commonwealth unit-characteristics
chart: OA PDF pages 9, 15 and 21 identify the codes, and PDF pages 22–23 give
their allowances. This includes the Polish brigade scheduled for turn 1,
stage 3, and the 7th Indian brigade scheduled for turn 5, stage 1. These facts are
retained in `Cna1979GrazianiCounterFacts`; mobility and operational initialization
remain unresolved. This reading is not independent review or certification of
a complete historical setup. The inspected Commonwealth chart bytes match the
SHA-256 pinned in the [arrival review manifest](../work/plans/graziani-arrival-review/manifest.json).
The scenario PDF's page 3, §60.41, places the 16th Infantry Brigade at E1829
with 20 medium truck points; its bytes match the
[setup review manifest](../work/plans/graziani-source-review/manifest.json).
The same source-data class retains nine further printed base-allowance readings
from OA pages 11, 15 and 18. They do not decide motorization or resolve the
conflicting 6th NZ Brigade arrival timing.
It also retains readings for ten 4th Indian Division and seven 7th Armored
Division setup members, including the printed fixed-allowance marker for the
relevant armored headquarters. Central India Horse remains unresolved because
its starting training state changes the applicable characteristics. The current
source content envelope does not yet carry the fixed-allowance marker; do not
interpret a numeric allowance in that envelope as implementing this exception.
