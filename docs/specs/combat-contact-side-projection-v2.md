# Combat contact side projection v2

Status: implementation contract for the permanent campaign path. This version
supersedes the World-derived apparent-enemy fields of the historical
[v1 projection](combat-side-projection-v1.md) for new campaigns. It is not an
alternative fog mode. Freeze this contract with the native Combat action and
normal Host route before any outward `CMB-TASK-020` activation.

## One authority cut

Core derives the owner observation and the complete offered action set from
one committed campaign cut. The observation contains published map topology,
own formations and resources, and contact knowledge accessible to the acting
formation. Current contacts carry opaque observer-scoped handles and apparent
locations. Stale contacts remain last-known markers and cannot be direct
assault targets. No opposing element, representation, exact strength, event
source, or private authority version appears in the outward record.

An owner action set is addressed by authenticated campaign, seat, acting
formation, side-audience revision, decision reference, and stable action
references. Host retains the private mapping and authority fence. Public
references derive only from approved owner-visible values and Host-issued
opening identity; a private state hash or hidden event count cannot enter
their preimage. An unchanged visible cut and action set preserve opening
bytes, references, notification behavior, and generic rejection behavior
across hidden authority advances.

## Combat selection

At an admitted Combat Position Determination cut, Core may offer voluntary
`select-close-assault` only for an acting own participant and a current,
accessible contact whose apparent hex satisfies the source geometry and whose
own visible resources satisfy the selected rules/configuration. The submitted
intent names the public contact handle and apparent hex. Core resolves the
private bound defender only after authenticating the retained opening and
rechecking the current same-cut mapping. Selection never accepts a caller's
opposing ID or private snapshot.

`finish-without-attack` is offered only when the rules/configuration permits
voluntary closure and no source-required attack obligation is open. An empty
candidate set is not an implicit no-attack decision. Unsupported mandatory
obligations block campaign activation; they cannot disappear from the action
set at runtime. The selected or no-attack event advances ordinary Core
authority and has a durable command, event, state, and receipt schema.

The first internal selection implementation retains a V12 selected-contact event and
checkpoint in normal Exercise evidence. Host import replays its accepted-step kind and
checks the private defender binding against independently replayed contact authority.
The event records the exact sequence successor, Barrage, and cold replay rejects a
different route; subsequent Combat position handlers still need implementation.
The internal cut now checks voluntary-finish eligibility against opposing ZOC
authority. Current V6 campaign worlds certify no positive ZOC source, while
unsupported future worlds fail closed. Both selected-assault and voluntary-finish
choices now append a canonical V12 owner decision through the same normal Core/Host
receipt and cold-replay path. A finish carries no target binding. Host atomically appends
a V13 System completion with the five exact remaining no-attack Combat successors,
from Barrage through same-slot Reserve Release entry. Core replay checks each successor
and leaves World, resources and contact records unchanged; release itself remains separate.
Core's trusted Combat owner draft serializes current contact handles, apparent hexes and
assault offers in a V2 visible cut; its action IDs, authority fence and defender binding
remain private to Host/Core. The draft is not a public route.
The normal Host now retains a seat-bound internal Combat opening, accepted selection,
System completion and idempotent owner receipt across cold replay. Assault adjudication remains open;
there is no public Combat route.
For a selected infantry-only assault, a V14 System event records the no-effect Barrage
successor to same-slot Retreat Before Assault. Core replay rechecks Content capability and
the exact successor; Host commits selection and Barrage in one transaction and retains the
owner receipt. Defender retreat choice and subsequent assault stages remain open.
Core now derives the first defender RBA candidate cut from that retained Barrage state.
It identifies the bound defender privately, reads only that defender's own formation,
current contacts, published map and exact movement costs for move-offer presence, and
always includes an explicit decline. Known occupied contact hexes cannot be retreat
destinations; hidden opposing occupancy cannot silently remove an offer. Land §§13.1,
13.21–13.26 govern eligibility, cost, initial adjacency and stopping. This candidate
cut is still internal. The first offered move now commits through Core movement rules,
updates the authoritative World and both contact audiences, and retains a replayable
V16 event. It returns to a defender cut at the RBA position. Further offered moves
use the same durable authority path and append ordered V16 events until Core cost and
stopping rules remove them or the defender explicitly stops. An unseen encounter
policy remains to complete before an outward opening.
The first V1 defender-visible retreat draft contains contact observation, own unit,
map, initial-adjacency fact, move costs and explicit decline. Core action IDs and
authority version stay in the trusted Host adapter, separate from visible bytes.
An explicit defender decline now records a V15 owner event from the Barrage cut to
same-slot Force Assignment. Normal Host binds it to the defender seat, retains the
opening/ref and accepted receipt atomically, and replays the exact event after restart.
No query or timeout fabricates a decline. Unseen encounter
resolution, and subsequent assignment/adjudication remain to implement on this
production branch. The normal Host now accepts the first move through the defender
seat, retains its receipt, and cold replays the updated World/contact journal. It also
accepts further offered moves or an explicit stop from each next cut. This is the
normal campaign owner path, still internal until later Combat stages and outward
visibility contracts are complete.

At Force Assignment, Core derives each owner's allocation candidate from that
owner's current component TOE and pinned Content at the retained post-RBA cut.
The candidate action ID binds role, own element, component and committed TOE;
neither opponent allocation nor private authority version enters its semantics.
The current infantry path offers its full current TOE, including a five-TOE
unit. Core V17 retains the two owner-only seals as separate canonical events.
Both keep the Force Assignment position and the other owner's visible cut
unchanged. A separate V18 System completion advances to Anti Armor after both
seals. Normal Host retains each under its seat, opaque ref and cold-replay
receipt and appends V18 atomically with the second.
Normal campaign creation can pin canonical seven-window timing bytes to its
durable journal head, bound to its ruleset and content hashes. Absent timing
configuration blocks Force Assignment opening and seals; the pinned budget
is now enforced by a normal Host shared Force round. SQLite schema 7 retains
one trusted deadline/high-water gate for both seats and verifies its source,
status and cancellation evidence on cold replay. Core V19 validates System
cancellation, preserves zero or one owner seal, and moves to the same-slot
Anti Armor step. Core V20 completes that step with a canonical no-effect
event only when the retained campaign content contains infantry components
alone; it binds the prior Force outcome, advances to same-slot Close Assault,
and normal Host retains it atomically. Cancellation is marked no-attack and
cannot draw a result. Core V21 completes that cancelled path through same-slot
Close Assault to Reserve Release without RNG or settlement, with an independent
canonical event and normal Host cold replay. A positive two-seal path now
retains a V22 ordered Core result and source-derived ammunition spend in the
normal Host, with exact event and checkpoint cold replay. V22 also binds canonical
prior and paid World hashes; Core retains the paid World projection after charging
both roles' capability points, and cold replay re-derives that projection from
the sealed input cut. V24 now settles World TOE/cohesion and an accepted
retreat, retaining captured TOE as pending custody; custody disposition and
outward issuance remain. A
versioned exact-pack retreat-direction supplement supplies one friendly anchor
per side in the normal 10-TOE campaign. Core derives post-result defender
one-hex offers from strictly decreasing distance to that anchor on the
published graph, excluding current contact targets and own combat occupancy;
explicit refusal is always present. Core V23 accepts an offered route or
explicit refusal against the retained V22 result, paid World hash, exact-pack
source hash, and contact authority cut. A no-retreat result advances by a
separate System choice; silence cannot become refusal. V23 event bytes and
checkpoint are verified on ordinary Exercise reconstruction. Normal Host now
retains a defender-only opening, private action map, accepted V23 submission,
receipt, and cold replay; no-retreat V23 is appended as a System step. No
public route issues these offers yet. A following System V24 event binds the
V23 choice to the resulting World hash and pending captives. Exercise and
normal Host cold replay independently derive its World and contact effects.
The defender-visible cut carries the source hash, side contact revision, own
element and route/refusal semantics; private authority version and Core action
IDs stay in the Host fence rather than the visible bytes.
An internal post-V24 captor cut now derives guarded relocation and unguarded
escape candidates from accepted World, current captor contact, and published
routes/terrain. It binds the V24 settlement hash and authority version privately.
Core V25 now accepts the cut choice as a canonical event, World update, and
typed guard or delayed replacement asset; replay independently reprojects it.
Normal Host appends the no-captive System disposition and retains a captor-only
visible opening, accepted choice, and cold receipt for an authenticated capture.
Core action IDs stay stable under private settlement-binding changes with the
same visible offers. Broader equal-visible history coverage, public routing,
and the next versioned campaign position remain before admission.
An internal V26 successor event now binds accepted V25 custody to a derived
same-slot Reserve Release position, settled World, retained Core RNG, and
custody state. V11 snapshot admission remains at its historical Combat-entry
boundary. Exercise and normal Host retain and cold-replay the V26 checkpoint;
the following ordinary action and outward opening remain unwired.
The normal Reserve Release rule derives only due own members and their legal
I/II choices from the settled World. It carries resulting World and next-cycle
movement allowance. Internal V27 events bind ordered choices and completion
to V26 authority with exact Exercise and Host cold replay. The normal Host
offers the next owned reserve member with a side-safe opening and private
action map, accepts a fenced choice, and appends System completion when the
queue empties. Completion stays at Reserve Release. V28 carries a separate
authenticated side-safe repeat-or-finish choice: repeat needs a source-legal
released-unit next-Movement witness and enters the same-side Movement ordinal;
finish enters same-slot Truck Convoy. Core Exercise and normal Host cold-replay
that choice and retain its receipt. Internal V29 accepts one released-unit
repeated Movement action from the same-cut side contact offer, applies its
release ceiling to World, advances both contact ledgers and retains the normal
Host receipt across restart. V30 records the cohortless Breakdown System
determination and same-slot Combat entry after that move with normal Host cold
replay. Vehicle accounting, repeated Combat/Reserve Release, other legal
continuation witnesses, and public routing remain pending.
contact bound to a unit with no combat component
has no Force Assignment candidate; normal contact selection must gain a
source-backed non-infantry disposition before outward Combat issuance.

The remaining Combat sequence must use the same authority: Barrage, Retreat
Before Assault, Force Assignment, Anti Armor, Close Assault, result, retreat,
loss, custody, and Reserve Release. Each enabled family needs an exact owner
or System action and deterministic fallback. No campaign is admitted with a
reachable family that lacks an adjudication handler and side projection.

## Forced and surprise contact

A source-required ZOC or other forced assault uses a typed Core transition
that records the observation which makes the obligation knowable to the
acting side. It presents the required action or an expressly authorized
System resolution. A surprise engagement interrupts movement at its
adjudicated cut, emits the contact/disclosure event, and enters the same
Combat decision path. Private participants stay behind contact handles.
Detection, forced choice, and interruption behavior must be pinned to the
selected Land rules/configuration before the capability is enabled. Neither
private World presence nor a rejected hidden move may silently create an
offer or a distinguishable error.

## Submission, replay, and publication

### Normal durable HTTP contract

`/api/v2/campaigns` is the version-2 route for the same normal Core/Host
authority. It is configured with `CampaignJournal:Path` and a 64-lowercase-hex
`CampaignJournal:OperatorSecret`; `CampaignJournal:EnableOutwardApi=true` maps
it in local Development hosting. It is off by default, accepts loopback callers
only, requires same-origin browser requests, limits JSON bodies to 8 KiB, and
returns `Cache-Control: no-store`. This deployment guard does not select a
different fog rule or campaign engine.

Every request body has `contractVersion:2` and exactly the named fields.
`POST /` takes the nine `CampaignCreationRequest` fields (`campaignId`,
`rulesetHash`, `seed`, `setupId`, `setupHash`, `contentPackId`, `contentHash`,
`scenarioId`) plus a canonical seven-window `timing` object; it requires
`X-Sandtable-Operator`. `POST /{campaignId}/grants/{axis|commonwealth}`
requires the same operator header and returns an expiring single-use grant.
`POST /{campaignId}/seats` takes that grant and returns a rotating seat
credential. Grant and credential envelopes carry campaign ID, audience,
epoch, 64-hex secret, and expiry; the secret is never put in a URL.

`POST /{campaignId}/openings` takes the credential, stage, and acting element
ID. It returns canonical side-visible cut bytes as base64, owner revision, and
ordered public action references. Supported stage names are `movement`,
`combat-selection`, `retreat-before-assault`, `force-assignment`,
`post-assault-retreat`, `custody`, `reserve-release`, `cycle-control`, and
`repeat-movement`. `POST /{campaignId}/actions` takes the same credential,
stage, acting element, opening revision, and public action reference.
Retreat-before-assault submissions use `rba-decline` or `rba-move`; other
submission names match opening names. The response acknowledges the public
reference and owner revision only. It never serializes the retained internal
Core receipt, private action ID, authority version, or hidden-state hash.
Exact accepted retries return the same outward acknowledgement. Invalid
or stale actions use one generic rejection envelope.

This route uses the normal durable SQLite journal and seat fence. It does not
make a public Combat claim: a full repeated Combat cycle, source-legal
terminal cases, normal campaign continuation, public hosting, and the support
certificate still gate activation.

Host authenticates the seat and retains the opening, accepted input, public
receipt, Core event, and canonical contact transitions in one durable history.
It replays Core authority and contact records on cold read. If private history
advances while owner-visible bytes and mapped offer remain equal, a timely
retained action stays valid against the current cut. Exact retries return the
original receipt. Rejections share one side-safe envelope and do not expose
hidden cause, stale private version, or which opponent changed.

New campaign readers, Maproom, War Diary, and Intelligence consume this
versioned side projection together. Historical V6/V7 and private P01 readers
remain replayable but are never a fallback for a new campaign. Public Combat
activation requires two-side equal-visible hidden-fork tests, source-legal
forced/surprise and terminal coverage, durable restart, and a closed support
certificate for the selected capability manifest.
