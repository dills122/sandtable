# Graziani source authority, version 4

The version 4 source authority carries a committed Axis GT2 convoy plan forward
from the version 3 pregame session. It remains in setup: it does not certify a
complete scenario, advance the Land clock, deliver cargo, or place an arrival.
The previously retained version 3 genesis and actions keep their original
meaning and their existing legal-action policy.

The Axis owner may finish the GT2 supply plan after the availability roll. An
unallocated result remains unused availability; it does not assert that any
other cargo or first-turn supply is absent. The committed plan retains the die,
maximum, unused amount and lane/stage allocations in canonical order. Its hash
binds the source-linked route and provisional planning policy inherited from
the pregame profile.

The canonical `source-authority-started` event contains, in order:
`contractVersion` (4), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`axis`), `contentHash`,
`authorityPolicyId`, `authoritySetupHash`,
`convoyPlanHash`, `axisSupplyWorldHash`, `commonwealthSupplyWorldHash`,
`axisInitialForcesHash`, `commonwealthInitialForcesHash`, `axisMotorizationHash`,
`commonwealthMotorizationHash` and `openingConvoyPlanHash`. The action ID is the SHA-256 digest of the UTF-8 text
`graziani.pregame.gt2-supply.finish.v1`, encoded as
`sha256:1d9b9b7bbda3223bca1ca3d1da34ba387d943fb2a5fc88c7928839033987cfa6`.
The prior checkpoint hash binds the
committed decision to the actual pregame state; replay must re-adjudicate the
decision rather than accepting an externally supplied plan or clock.

The canonical checkpoint contains, in order: `contractVersion` (4),
`campaignId`, `stateVersion`, `rulesetHash`, `contentHash`, `setupHash`,
`authorityPolicyId`, `authoritySetupHash`,
`positionId` (`land.graziani-source-setup`), `status` (`setup-in-progress`),
`gameClock` (null), `pregameCheckpointBase64`, `convoyPlanBase64`,
`axisSupplyWorldBase64`, `commonwealthSupplyWorldBase64`,
`axisInitialForcesBase64`, `commonwealthInitialForcesBase64`,
`axisMotorizationBase64`, `commonwealthMotorizationBase64`, `openingConvoyPlanBase64`,
`commonwealthSupplyPlansBase64` (null until Axis GT1 planning finishes) and
`sourceSnapshotBase64`. The retained pregame checkpoint binds the random seed,
cursor, original source snapshot and pending arrival groups. The committed plan
and source world must be carried into any later setup and game transitions.
The supply state includes the fixed Axis supply counters and two pending dumps;
Derna's printed ammunition dash remains unknown. The supported dump sites are
a restricted, source-checked subset, not a complete map or a declaration that
all setup is finished. Each Axis placement adds three regular units at one
supported site. Currently the two supported sites are `c4507` and `c4707`;
the dumps cannot share a site under this profile's provisional three-unit
non-city limit. The wording of §32.14 refers to an “off-map” hex; applying
that limit to these on-map sites remains a ruling to resolve.
Scenario-provided dummy groups have separate private choices described below.

The version 4 authority policy records a provisional interpretation that the
scenario's dummy groups may be omitted. Its setup hash binds that policy, the
version 3 setup and content hashes, and the supply and force policy identities.
The general dummy allowance remains a separate unresolved question.

The source motorization ledgers retain selected initial pools and private,
editable placement allocations. Attached motorization requiring unresolved
unit mobility or assignment priority remains pending; the ledger does not
automatically load or attach supply. The `source-motorization-adjusted` event
contains, in order, `contractVersion` (4), `eventType`, `campaignId`,
`priorStateVersion`, `priorCheckpointHash`, `actionId`, `actor`, `poolId`,
`locationId`, `delta`, and `motorizationHash`. Its action ID hashes the UTF-8
NUL-separated values `graziani.setup.motorization.v1`, side, pool, location,
and invariant decimal delta. The authority setup identity binds the initial
ledgers and their policy, not the player's later allocations.

The bounded Land-only profile records Axis 180 points at Tripoli and 125 for
Libya, with five Tobruk attached points pending eligibility. Commonwealth pools
record 50 Cairo, 20 Alexandria and 45 anywhere, with 20 attached to the 16th
Brigade pending eligibility. Other explicitly quantified first-line truck rows
are retained as pending attached pools. Eligible destinations are a curated
subset; optional loads, unit eligibility and assignment priorities remain unresolved.
Medium and heavy points are included under §59.63; light and air-facility trucks
are not added to these motorization pools.

Axis may also plan game-turn 1 supply using a provisional September-I B-column
availability roll, followed by private lane and operation-stage allocations and
an explicit finish. This treats the starting-turn exception as allowing a plan,
not as proof of a guaranteed shipment. The GT1 roll is the next recorded random
draw after the retained GT2 roll; the ruling and order are bound to authority
identity. A `source-gt1-supply-planned` event contains, in order,
`contractVersion` (4), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`axis`), `choice`, `lane`,
`operationStage`, `delta`, `randomCursorBefore`, `randomCursorAfter` and
`openingConvoyPlanHash`. This plan does not deliver cargo or decide other
convoy categories.

Once Axis GT1 planning finishes, Commonwealth privately decides whether to
skip or roll for starting-turn supply and rolls for turns 2, 3 and 4 in order.
The provisional policy records the GT1 exception and RNG order; later rolls
use the period-I chart values 1, 1, 2, 2, 3 and 3 and retain the entire result
as future supply. A `source-commonwealth-supply-planned` event contains, in
order, `contractVersion` (4), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor`, `choice`, `targetGameTurn`,
`randomCursorBefore`, `randomCursorAfter`, and `commonwealthSupplyPlansHash`.
These decisions do not place or deliver the supplies.

Each side can privately decide its scenario-provided dummy groups: place all
three counters together at a supported site, or omit the group under this
provisional policy. Real and dummy supply counters both count toward site
capacity. A `source-dummy-group-decided` event records, in order,
`contractVersion` (4), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor`, `dummyId`, `locationId` (null for
omission), and `supplyWorldHash`. Its action ID hashes UTF-8
`graziani.setup.dummy-choice.v1`, a zero byte, the side, a zero byte, the
group ID, a zero byte and the location ID or `omit`. The journal stores this
private evidence; opponent views must not disclose the decision or destination.

The initial Axis force evidence includes 13 Tobruk infantry-group counters,
nine Tobruk artillery/AA counters, and eleven Bardia garrison candidates, with
source TOE for 29 non-headquarters counters. The Bardia/Tobruk 30th versus
32nd GaF source conflict remains explicitly marked on affected identities.
Headquarters TOE, attachments, mobility, some support combat rules, other Axis
forces and a complete playable map remain unresolved. The
evidence is part of this authority's checkpoint; it does not imply a complete
World8 battlefield or certify combat or control.
The printed force evidence includes anti-air units and components; their
Land-only treatment under §32.83 remains unresolved and they are not certified
as active campaign forces.
Commonwealth force evidence also retains the source-listed 4th Indian and 7th
Armoured initial groups, including Central India Horse with unknown training and
TOE. Other forces and their relationships are not implied by these entries.

The canonical `source-axis-supply-dump-placed` event contains, in order:
`contractVersion` (4), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`axis`), `dumpId`, `locationId`
and `axisSupplyWorldHash`. Its action ID hashes the UTF-8 bytes of
`graziani.axis-setup.place-dump.v1`, a zero byte, the dump ID, another zero byte
and the location ID, using the usual `sha256:` encoding. Replaying the action
revalidates the owner, pending dump and supported destination against the prior
world; the checkpoint retains the resulting supply state.

Commonwealth setup retains its fixed Matruh and Sidi Barrani supplies and
requires separate choices for the three Cairo counters, three Alexandria
counters and the three-unit dump. City counters may be split among supported
city hexes. The map-D dump destination is currently restricted to the
source-checked `d3612`; the scenario's dummy group has a separate private choice.
The `e1931` Cairo hex identity remains inferred from neighboring numbering and
the location summary, rather than a directly legible map number. The printed
Benghazi `b4827` and map/location-summary `a4827` conflict remains unresolved.
An Axis player cannot inspect Commonwealth choices or execute their actions.

The `source-commonwealth-supply-placed` event contains, in order,
`contractVersion` (4), `eventType`, `campaignId`, `priorStateVersion`,
`priorCheckpointHash`, `actionId`, `actor` (`commonwealth`), `placementId`,
`locationId`, and `commonwealthSupplyWorldHash`. Its action ID hashes the UTF-8
bytes of `graziani.commonwealth-setup.place-supply.v1`, a zero byte, the
placement ID, another zero byte, and the location ID. Replay re-adjudicates
the decision against the retained source state.

## Development Host openings

The existing authenticated `/api/v2/campaigns/{campaignId}/openings` and
`/actions` routes expose side-owned opaque references for these setup cuts:

| `stage` | `actingElementId` | Owner |
| --- | --- | --- |
| `pregame-axis-supply-finish` | `land.pregame-axis-supply-finish` | Axis |
| `source-axis-dump` | `land.graziani-axis-dumps` | Axis |
| `source-commonwealth-supply` | `land.graziani-commonwealth-supply` | Commonwealth |
| `source-dummy` | `land.graziani-dummy-groups` | Either, for its own groups |
| `source-motorization` | `land.graziani-motorization` | Either, for its own pools |
| `source-axis-gt1-supply` | `land.graziani-axis-gt1-supply` | Axis |
| `source-commonwealth-supply-plan` | `land.graziani-commonwealth-supply-plan` | Commonwealth |

The finish opening is separate from the unchanged version 3 supply-adjustment
opening. A Commonwealth seat is issued only after the source-authority
transition; each seat receives only its own choices. The normal accepted-step
journal retains and re-adjudicates each action during cold replay.

Neither a caller-supplied phase nor an incomplete source excerpt can establish
that the Polish group is due.

Outstanding work includes the remaining initial-world and setup obligations,
source-capable game positions and rules, and a player-controlled arrival with
stacking and control checks. Version 4 is an intermediate authoritative setup
state, not a playable Graziani admission.
