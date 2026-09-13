# Creation-rooted inherited active Reaction second move v1

Status: `CMB-TASK-003D2c.3p` contract checkpoint. This specification, ordered inventory, retained
vectors, and executable oracle freeze one second movement step by each exact active participant from
the [3g Reaction lifecycle](combat-inherited-reaction-lifecycle-v1.md). Evidence is prospective
contract authority, not production registration, Snapshot composition, public activation, or
simulation.

## Admission and trace matrix

Every case reconstructs one of the two literal both-owner `3g` states immediately after its first
accepted participant move. Authority is version 14: the opposing ordinary infantry participant is
at its own rear with cumulative CP2, its sole opportunity remains active, its original reactor
route is open, and exactly one legal rear-to-supply Clear2 move remains. Caller state never
substitutes for predecessor replay, and predecessor fixture goldens must reproduce before extension.

Each state admits exactly one owner-authored `move-reacting-element` command. Actor is the reacting
side: Commonwealth for Axis-phasing history and Axis for Commonwealth-phasing history. The two
owner cases are isolated histories, never sequential events or interchangeable authority.

## Command and disclosed authority

Command uses contract version 2 and binds exact move kind, stable public window and newly rotated
opportunity handles, rear origin, supply destination, action ID, creation binding/event hash, cycle
ID, expected version 14, and exact suspended sequence position. Action ID hashes contract version 1,
kind, handles, origin, destination, and exact Clear2 cost disclosure.

Window handle uses ordered domain `sandtable.observation.reaction-window.v1` over campaign ID,
ruleset hash, trigger-committed version, and reacting side. Opportunity handle uses
`sandtable.observation.reaction-opportunity.v2` over window handle, current version 14, and
`sandtable.observation.reaction-capability.v1` hash of the sole rear-to-supply option. Both handles
differ from authoritative IDs. Window handle remains stable because its domain omits current state;
opportunity handle differs from first-step disclosure because state version and move option changed.

## Event compatibility and receipt

Accepted command emits one `reacting-element-moved` contract-version-3 derived event. It preserves
the inherited legacy movement field order and appended configuration/creation/cycle/opening/
completion/prefix/input/receipt suffix. Event binds authority 14→15, reacting actor, exact public
submissions, authoritative window/opportunity IDs, participant element/representation, rear origin,
supply destination, non-motorized mobility, SPI map 8.37 source, Clear2 cost, cumulative CP2→4,
unchanged Cohesion0, empty Breakdown accounting, and exact ruleset hash.

Receipt reuses `sandtable.combat.inherited-reacting-element-moved-receipt.v3` and `irl.` prefix. It
hashes canonical unsigned event bytes with UTF-8 NUL separation. Chronicle prefix and command
receipt advance exactly once; exact authenticated retry returns original event bytes without a
second effect.

## State transition and retention

World element and representation move together from reacting rear to reacting supply. Cumulative
CP becomes 4. Existing participant track appends supply, producing assault→rear→supply without a
second track. Reactor route retains route ID, first-move version 14, participant IDs, reacting owner,
assault origin, and empty cohorts; only current location becomes supply.

Reaction window and active authoritative opportunity remain open for later explicit completion.
Its frozen opportunity, resolved set, trigger authority/apparent trigger, and suspended phasing
position remain exact. Phasing continuation, phasing element at assault with CP4, RNG, initiative,
Weather/orders, Reserve state, cycle/opening authority, ammunition, TOE, resources, and predecessor
history remain byte-equivalent except defined participant World/route/track/version/prefix/receipt/
progress transitions. Move appends one material-progress reference for its event.

## Replay, retry, and rejection

Strict compact typed JSON requires declared field order, no unknown or duplicate properties,
canonical numbers/strings, booleans distinct from integers, sorted identity arrays, depth 32, 1 MiB
per document, and at most 512 retained items. Replay re-derives exact command/event/state from
creation-rooted predecessor history.

Reject altered creation/Movement/trigger/first-participant history; stale, future, cross-owner,
cross-cycle, cross-history, wrong-position, wrong-actor, wrong-origin/destination/cost/action;
authoritative IDs or first-step disclosed opportunity used as current opportunity; missing/mismatched route;
inactive/completed/no-route/multiple-opportunity/unsupported states; changed event, World, route,
track, RNG, progress, prefix, receipt, or retained authority; noncanonical bytes; missing/extra/
out-of-order events; and receipt/version/item/byte/depth overflow. A different accepted event from
the same predecessor is a competing fork, not later authority.

## Scope closure

This closes only the second movement step by the exact one-opportunity ordinary-infantry active
Reaction participant. It does not change `3g` bytes and does not complete the participant, resolve
its mandatory Breakdown stop, close the window, admit multiple opportunities, add positive vehicle
Breakdown, compose D2c.4/Task004/checkpoint B, implement runtime or public actions, or exercise the
simulator.
