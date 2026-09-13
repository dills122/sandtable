# Creation-rooted inherited direct Reaction closure v1

Status: `CMB-TASK-003D2c.3n` contract checkpoint. This specification, its ordered inventory,
retained vectors, and executable oracle freeze direct closure of the exact
[3f Reaction trigger](combat-inherited-reaction-trigger-v1.md). Evidence is prospective contract
authority, not production registration, Snapshot composition, public activation, host scheduling,
or simulation.

## Admission and trace matrix

Every case reconstructs one of the two literal both-owner `3f` terminals: one accepted phasing
infantry return move has opened a Reaction window with exactly one unresolved opposing infantry
opportunity, no active participant route, and an exact suspended phasing Movement route. Caller
state never substitutes for predecessor replay.

Each terminal admits exactly three mutually exclusive one-event successors:

| Command kind | Authenticated actor | Event reason | Result |
| --- | --- | --- | --- |
| `decline-reaction-window` | reacting side | `player-decline` | Close the unresolved opportunity and resume phasing Movement. |
| `close-reaction-window-scripted-unavailable` | System | `scripted-unavailable` | Same state transition with distinct fallback authority. |
| `close-reaction-window-timeout` | System | `timeout` | Same state transition with distinct timeout authority. |

Core owns no clock or timeout scheduler. The timeout command proves exact reason-specific System
authority only; OrleansHost/DecisionWorker scheduling, deadlines, automatic submission, and clock
confidence remain deferred.

## Command and action authority

Commands use contract version 2 and bind command kind, disclosed window handle, action ID,
creation binding/event hash, cycle ID, expected prior version, and exact suspended sequence
position. Actor comes from authenticated dispatch. Player decline requires the replayed reacting
side; both fallback commands require System. No caller-supplied reason field exists.

The disclosed window handle uses ordered domain `sandtable.observation.reaction-window.v1` over
campaign ID, ruleset hash, trigger-committed state version, and reacting side. It must differ from
the authoritative window ID. Each action ID hashes contract version 1, its exact reason-specific
kind, and that public handle. Reusing another reason's action ID or submitting the authoritative
window ID rejects.

## Event compatibility and receipts

All six forks emit `reaction-window-closed` contract version 3. This is the same compatible event
family frozen for `3g` no-eligible closure: ordered legacy fields remain campaign/version pair,
from-position, nullable acting side, action and submitted public-window IDs, authoritative window
ID, close reason, sorted closed opportunity IDs, suspended sequence position, ruleset hash, and
Breakdown flow after. The appended configuration/creation/cycle/opening/completion/prefix/input/
receipt suffix is unchanged.

Player decline records the reacting side; System fallbacks record null. `closedOpportunityIds`
contains the sole exact unresolved authority opportunity. `breakdownFlowAfter` is the exact
`moving` phasing continuation from the trigger. No direct close creates a reactor Breakdown stop.

Receipt identity reuses ordered domain
`sandtable.combat.inherited-reaction-window-closed-receipt.v3`, UTF-8 NUL separation, and canonical
unsigned event bytes, with the established `irl.` prefix. Chronicle prefix advances once from the
accepted event bytes. The command receipt binds exact command hash, event hash, receipt, actor, and
resulting version.

## State transition and retention

One accepted event advances authority 13→14, clears the Reaction window and interrupt position,
restores the suspended Movement sequence position, and resumes the exact phasing route. World,
RNG, initiative, Weather/order, Reserve state, cycle/opening authority, tracks, material-progress
references, ammunition, TOE, Cohesion, CP, locations, and all predecessor receipts remain
byte-equivalent to the trigger terminal. Closure consumes no CP/BP/RNG and adds no material
progress.

The phasing battalion therefore remains at its assault location with CP4 and its two-move route;
the reacting battalion remains at its assault location with CP0. The resumed route remains owned
by the phasing side at that assault location.

## Replay, retry, and rejection

Strict compact typed JSON requires declared field order, no unknown or duplicate properties,
canonical numbers/strings, booleans distinct from integers, sorted identity arrays, depth 32,
1 MiB per document, and at most 512 retained items. Exact authenticated retry returns original
event bytes and reconstructed terminal state without another version, receipt, prefix, or effect.

Reject altered creation/Movement/trigger history; stale, future, cross-owner, cross-cycle,
cross-history, or wrong-position commands; player-authored fallback or System-authored decline;
kind/action/reason mismatch; authority ID used as public handle; active/resolved/empty/multiple
opportunity state; changed closed IDs, route, World, RNG, progress, prefix, receipts, or retained
authority; noncanonical bytes; a second close; and receipt/version/capacity overflow. A lawful
alternate reason from the same predecessor is a fork, never a sequential successor.

## Scope closure

This closes direct player/fallback Reaction exits for the exact one-opportunity ordinary-infantry
trigger. It does not alter `3g` participant lifecycle bytes and does not admit active-participant
fallback, multiple opportunities, a chosen second move, positive vehicle Breakdown, broader
Reserve profiles, D2c.4 composition, Task004/checkpoint B, runtime, public actions, host scheduling,
or simulator behavior.
