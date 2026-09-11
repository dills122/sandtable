# Creation-rooted inherited Reaction lifecycle v1

Status: CMB-TASK-003D2c.3g contract checkpoint. This specification and its
[ordered inventory](combat-inherited-reaction-lifecycle-v1.schema.json) govern the retained
[vectors](fixtures/combat-inherited-reaction-lifecycle-v1.json) and
[oracle](verify-combat-inherited-reaction-lifecycle-v1.py). Evidence is prospective contract
authority, not production registration, Snapshot composition, public activation or simulation.

## Admission and exact trace

Every entry reconstructs actual creation through the complete accepted
[3f Reaction trigger](combat-inherited-reaction-trigger-v1.md). Only its two literal both-owner
histories are admitted: one phasing infantry move to rear, return to assault, exactly one frozen
opposing infantry opportunity, normal Weather, no Reserve/vehicle/guard/relationship/settlement or
future-obligation state. Caller-supplied cached state never substitutes for replay.

Exactly four accepted command2/event successors close this profile:

| Command | Actor | Event | Required result |
| --- | --- | --- | --- |
| `move-reacting-element` | reacting side | `reacting-element-moved`3 | Select sole opportunity; move assault→own rear; active reactor route. |
| `complete-reaction-participant` | reacting side | `reaction-participant-completed`3 | Resolve participant; open Reaction-completed stop. |
| `resolve-breakdown-stop` | System | `breakdown-stop-resolved`2 | Resolve empty cohort; return to inactive Reaction. |
| `close-reaction-window-no-eligible-reactor` | System | `reaction-window-closed`3 | Remove exhausted window; resume suspended phasing route. |

Completion never implies closure. Even zero cohorts require the distinct stop-resolution event;
closure before it, or direct close after trigger, cannot establish this trace. Player decline,
timeout and scripted-unavailable are valid wider-system families but unsupported here.

## Public handles and command authority

Commands bind creationBinding, creationEventHash, cycleId, expectedPriorVersion and exact current
position. Actor comes from authenticated dispatch, not a player-claim field. Reacting commands use
the replayed opposite side; resolution and no-eligible close require System.

The public window hashes ordered domain `sandtable.observation.reaction-window.v1`, campaignId,
rulesetHash, triggerCommittedStateVersion=13 and reactingSide. At each state, derive the participant's
complete sorted move-option inventory. After the admitted first move this includes the still-legal
rear→supply option even though this trace deliberately chooses completion. Its capability key hashes domain
`sandtable.observation.reaction-capability.v1` and ordered moveOptions. Public opportunity hashes
domain `sandtable.observation.reaction-opportunity.v2`, public window, current stateVersion and that
capability key. These public IDs are not authoritative window/opportunity IDs and the opportunity
handle rotates after movement.

Action IDs retain version1 candidate semantics. Move hashes kind, both public handles, origin,
destination and the exact public cost breakdown. Completion hashes kind plus both current public
handles. Resolution uses the System stop capability from
`sandtable.action.breakdown-stop.v1`; close hashes its kind plus current public window. Commands add
the creation-rooted envelope without rewriting these action identities.

## Ordered events, receipts and state

Move3, completion3 and close3 retain their Rules9 legacy fields in exact codec order before appended
configuration, creation, cycle, opening-base, original Reserve-completion receipt, prior Chronicle
prefix, accepted input and event receipt. Resolution2 is the already-frozen inherited empty-stop
successor: its first15 fields, source set, appended authority/input, suspended sequence position and
null interrupt context remain compatible with the 3b contract.

Receipts are lowercase SHA256 over event-specific domain UTF8, NUL and canonical unsigned event
bytes. `irl.` prefixes move/completion/close; compatible resolution retains `iml.`. Chronicle prefix
uses the established sequence5 prior-prefix/full-event codec. Receipts retain exact command/event
hashes, actor and resulting version. Fresh commands replay all predecessor bytes; exact authenticated
retries return original accepted bytes and current reconstructed terminal state.

## Material transitions

The reacting battalion moves from its assault location to its own rear over one Clear2 edge. CP
changes 0→2; Cohesion and movement-ended stay unchanged; no Breakdown accounting appears. First move
selects the sole authority opportunity, creates a reactor route at the committed successor version
and preserves the phasing `resume-route` byte-for-byte. Only this move adds one actual progress ref.

Completion clears activeOpportunityId, appends the authority opportunity to resolvedOpportunityIds,
and records a stop at its committed version with reason `reaction-completed`, normal Weather and no
cohort inputs. Resolution has empty checks/lots, equal before/after RNG, exact rule source
21.24–21.26, consumes the reactor route and returns to the Reaction position with the same
window inactive. No-eligible close has null actor and empty closedOpportunityIds because the only
frozen opportunity is already resolved. It removes the window, restores exact suspended Movement
sequence position and resumes the original phasing route at the phasing assault location.

Final World retains both material moves: phasing battalion CP4 at its assault location and reacting
battalion CP2 at its rear. Original phasing track contains rear then return; reacting track is added
for assault→rear. RNG, initiative, Weather/order, Reserve members, cycle/opening authority and all
unrelated World collections remain unchanged. Completion, stop resolution and closure add no
material-progress entries and do not enter Breakdown Determination or Combat.

## Replay and rejection

Strict compact typed JSON requires declared field order, no unknown/duplicate fields, booleans
distinct from integers, canonical numbers/strings and sorted declared identity arrays. Limits are
1MiB, depth32, 512 items, exactly one 3f trigger and at most four lifecycle events.

Reject altered creation/trigger history; stale/cross-owner/cross-cycle commands; authority IDs used
as public handles; changed option/cost/action identities; skipped, duplicated, reordered or fifth
events; second move/participant; vehicle/Reserve/armed state; changed stop/window/route/World/RNG/
progress effects; noncanonical bytes; forged cached state. Both owners retain every cut, retry every
command after terminal and pin exact input/event/state bytes and source hashes.

## Scope closure

This closes one actual ordinary Reaction participant episode only. Positive vehicle Breakdown,
multiple opportunities or a chosen second move, decline/timeout/unavailable closure, Reserve movement, armed Combat
continuation, D2c.4 composition, Tasks004/021/022 and runtime/simulator work remain gated.
