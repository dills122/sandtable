# Creation-rooted inherited active Reaction fallback v1

Status: `CMB-TASK-003D2c.3o` contract checkpoint. This specification, ordered inventory, retained
vectors, and executable oracle freeze reason-specific System fallback from exact active-participant
[3g Reaction lifecycle](combat-inherited-reaction-lifecycle-v1.md) authority. Evidence is
prospective contract authority, not production registration, Snapshot composition, public
activation, host scheduling, or simulation.

## Admission and trace matrix

Every case reconstructs one of the two literal both-owner `3g` states immediately after its first
accepted participant move. Authority is version 14: the opposing ordinary infantry participant has
moved from its assault area to its rear for 2 CP, its sole opportunity remains active, one legal
rear-to-supply move remains, and the exact phasing Movement route is suspended. Caller state never
substitutes for predecessor replay, and predecessor fixture goldens must reproduce before extension.

Each active state admits exactly two mutually exclusive System-authored fallback forks:

| Close command kind | Close reason | Recorded stop reason |
| --- | --- | --- |
| `close-reaction-window-scripted-unavailable` | `scripted-unavailable` | `reaction-unavailable` |
| `close-reaction-window-timeout` | `timeout` | `reaction-timeout` |

Core owns no clock or timeout scheduler. Timeout proves exact reason-specific System authority;
OrleansHost/DecisionWorker scheduling, deadlines, automatic submission, and clock confidence remain
deferred.

## Commands and action authority

Both commands use contract version 2 and authenticated System actor. The close command binds its
reason-specific kind, disclosed window handle, action ID, creation binding/event hash, cycle ID,
expected prior version, and exact suspended sequence position. No caller-supplied reason or stop
exists. Its action ID hashes contract version 1, exact command kind, and disclosed window handle.

The disclosed window handle uses ordered domain `sandtable.observation.reaction-window.v1` over
campaign ID, ruleset hash, trigger-committed state version, and reacting side; it differs from the
authoritative window ID. Player decline and no-eligible closure are invalid while the participant
route is active.

After closure, one `resolve-breakdown-stop` version-2 command binds the disclosed System stop
capability, action ID, same creation/cycle authority, version 15, and exact suspended position. Its
action ID hashes contract version 1, kind, and disclosed stop capability. The capability uses domain
`sandtable.action.breakdown-stop.v1` over campaign ID, ruleset hash, state version, and System
audience and differs from the authoritative stop ID.

## Event compatibility and receipts

First event is `reaction-window-closed` contract version 3. It preserves inherited close legacy
field order and appended configuration/creation/cycle/opening/completion/prefix/input/receipt
suffix. Acting side is null; reason matches command kind; `closedOpportunityIds` contains exact
active authority opportunity; suspended sequence position remains exact. `breakdownFlowAfter` is
`reactor-stop-closed`, retaining exact phasing continuation and participant route inside one
reason-specific empty-cohort stop recorded at version 15.

Second event is `breakdown-stop-resolved` contract version 2. It preserves inherited stop-resolution
field order and lineage suffix. Stop equals the close event's recorded stop; checks, created lots,
and interrupt context are empty/null; RNG before and after is identical; source is SPI land rules
21.24–21.26. `breakdownFlowAfter` is exact retained phasing `moving` route.

Close receipt reuses domain
`sandtable.combat.inherited-reaction-window-closed-receipt.v3` and `irl.` prefix. Resolution receipt
reuses `sandtable.combat.inherited-breakdown-stop-resolved-receipt.v2` and `iml.` prefix. Each hashes
canonical unsigned event bytes with UTF-8 NUL separation. Chronicle prefix and command receipt
advance exactly once per event.

## State transition and retention

Close advances authority 14→15, clears Reaction window and active participant authority, retains
suspended sequence data, enters Breakdown-stop position, and records `ReactorStopClosed`. It does
not resume phasing Movement. Mandatory resolution advances 15→16, clears closed stop, restores
Sequence position, and resumes exact suspended phasing route. No later no-eligible close exists.

Participant remains at own rear with CP2; phasing battalion remains at its assault location with
CP4. World, RNG, initiative, Weather/order, Reserve state, cycle/opening authority, tracks,
material-progress references, ammunition, TOE, Cohesion, locations, routes, and predecessor receipts
remain byte-equivalent except two appended receipts and defined version/prefix/Reaction/stop flow
transitions. Neither fallback event adds material progress.

## Replay, retry, and rejection

Strict compact typed JSON requires declared field order, no unknown or duplicate properties,
canonical numbers/strings, booleans distinct from integers, sorted identity arrays, depth 32, 1 MiB
per document, and at most 512 retained items. Exact authenticated retry returns original event bytes
and reconstructed terminal state without another version, receipt, prefix, or effect.

Reject altered creation/Movement/trigger/participant history; stale, future, cross-owner,
cross-cycle, cross-history, wrong-position, wrong-actor, player-authored, kind/action/reason/stop
mismatch; authoritative IDs used as public handles; inactive, completed, no-route, empty, or
multiple-opportunity states; changed closed IDs, stop, route, World, RNG, progress, prefix, receipts,
or retained authority; noncanonical bytes; out-of-order/extra events; and receipt/version/capacity
overflow. Alternate reason from same predecessor is a competing fork, never sequential authority.

## Scope closure

This closes System unavailable/timeout exits for exact one-opportunity ordinary-infantry active
Reaction after its first move. It does not change `3g` bytes and does not admit player decline while
active, another participant move, multiple opportunities, positive vehicle Breakdown, broader
Reserve profiles, D2c.4 composition, Task004/checkpoint B, runtime, public actions, host scheduling,
or simulator behavior.
