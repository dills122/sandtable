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
V16 event. It returns to a defender cut at the RBA position. Repeat movement and any
unseen encounter policy remain to complete before an outward opening.
The first V1 defender-visible retreat draft contains contact observation, own unit,
map, initial-adjacency fact, move costs and explicit decline. Core action IDs and
authority version stay in the trusted Host adapter, separate from visible bytes.
An explicit defender decline now records a V15 owner event from the Barrage cut to
same-slot Force Assignment. Normal Host binds it to the defender seat, retains the
opening/ref and accepted receipt atomically, and replays the exact event after restart.
No query or timeout fabricates a decline. Further move-offer acceptance, unseen encounter
resolution, and subsequent assignment/adjudication remain to implement on this
production branch. The normal Host now accepts the first move through the defender
seat, retains its receipt, and cold replays the updated World/contact journal. It also
accepts an explicit stop from the next cut. Further move offers at that cut must be
implemented before this branch is published as a complete defender RBA path.

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
