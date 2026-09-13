# Creation-rooted inherited Reaction movement completion v1

Status: `CMB-TASK-003D2c.3q` contract checkpoint. This specification, ordered inventory, retained
vectors, and executable oracle freeze explicit completion of each exact active participant after
its [second Reaction move](combat-inherited-reaction-second-move-v1.md), mandatory empty-cohort
Breakdown stop resolution, no-eligible window closure, and exact suspended phasing-Movement
resumption. Evidence is prospective contract authority, not production registration, Snapshot
composition, public activation, or simulation.

## Objective and exact admission

Each case creation-replays one literal both-owner `.3p` terminal at authority version 15. The
opposing ordinary infantry participant is at its own supply with cumulative CP4, its sole authority
opportunity remains active, its reactor route is open from assault through rear to supply, and no
further move option exists. Caller state never substitutes for predecessor replay; `.3p` fixture
goldens must reproduce before extension.

Exactly three accepted command/event successors close this profile:

| Command | Actor | Event | Authority | Required result |
| --- | --- | --- | --- | --- |
| `complete-reaction-participant` | reacting side | `reaction-participant-completed`3 | 15→16 | Resolve participant and open Reaction-completed stop. |
| `resolve-breakdown-stop` | System | `breakdown-stop-resolved`2 | 16→17 | Resolve empty cohort and return to inactive Reaction. |
| `close-reaction-window-no-eligible-reactor` | System | `reaction-window-closed`3 | 17→18 | Remove exhausted window and resume suspended phasing route. |

Completion never implies closure. Zero cohorts still require the distinct stop-resolution event;
closure before resolution cannot establish this trace.

## Public handles and command authority

Commands retain `.3g` contract-version-2 envelopes: creation binding, creation-event hash, cycle ID,
expected prior version, and exact suspended sequence position. Actor comes from authenticated
dispatch. Completion requires the reacting owner; stop resolution and no-eligible closure require
System.

Public window identity remains stable because it binds campaign, ruleset, trigger-committed version
13, and reacting side rather than current state. Completion derives a newly rotated public
opportunity at authority version 15 from the complete sorted move-option inventory. That inventory
is empty at supply, so this handle differs from both first-move and second-move handles while the
underlying authority opportunity remains unchanged. Submitted public handles never equal
authoritative window, opportunity, or stop IDs.

Action IDs retain version-1 candidate semantics. Completion hashes kind plus current public window
and opportunity. Resolution hashes the System stop capability from
`sandtable.action.breakdown-stop.v1`. Close hashes kind plus current public window.

## Compatible events and receipts

Completion3, resolution2, and close3 reuse the exact `.3g` ordered wire shapes. Completion and close
retain their Rules9 legacy fields before appended configuration, creation, cycle, opening-base,
original Reserve-completion receipt, prior Chronicle prefix, accepted input, and receipt. Resolution
retains the inherited empty-stop first-15-field and appended authority/input/sequence layout.

Receipts remain lowercase SHA-256 over event-specific domain UTF-8, NUL, and canonical unsigned
event bytes. Completion and close use `irl.`; compatible stop resolution uses `iml.`. Chronicle
prefix and command receipt advance once per accepted event. Exact authenticated retries return the
original event bytes and terminal state without a second effect.

## Material transition and retention

Completion clears active opportunity, appends the authority opportunity to resolved identities,
and records a `reaction-completed` stop at version 16. Stop route preserves exact route ID,
first-move version 14, participant identities, owner, assault origin, supply current location, and
empty cohort IDs. Normal Weather and empty cohort inputs are mandatory.

Resolution performs no checks or lots, leaves RNG unchanged, consumes reactor route, and returns to
the same inactive Reaction position. No-eligible closure has null acting side and empty closed IDs
because the sole frozen opportunity is already resolved. It removes the window, restores the exact
suspended sequence position, and resumes the original phasing route at the phasing assault.

Final World retains phasing infantry at assault with CP4 and reacting infantry at supply with CP4.
The reacting track remains assault→rear→supply. Completion, resolution, and closure do not change
World, tracks, material-progress references, RNG, initiative, Weather/orders, Reserve state,
ammunition, TOE, resources, cycle/opening authority, or predecessor receipts except their defined
authority, prefix, receipt, position, window, and flow transitions.

## Replay, retry, rejection, and limits

Strict compact typed JSON requires declared field order, no unknown or duplicate fields, canonical
numbers/strings, booleans distinct from integers, sorted declared identity arrays, depth 32, 1 MiB
per document, and at most 512 retained items. Replay re-derives exact command, event, and state from
creation-rooted predecessor history.

Reject altered creation/Movement/trigger/participant/second-move history; stale, future,
cross-owner, cross-cycle, cross-history, wrong-position, wrong-actor, wrong-action, or authority-ID
submissions; nonempty terminal move options; missing/mismatched route; inactive, resolved,
no-route, multiple-opportunity, vehicle, or unsupported states; skipped, duplicated, reordered, or
fourth events; changed event, stop, window, World, route, track, RNG, progress, prefix, receipt, or
retained authority; noncanonical bytes; and item/byte/depth/version overflow. A different accepted
event from the same predecessor is a competing fork, not later authority.

## Acceptance traceability and scope closure

| Acceptance ID | Normative proof | Executable evidence |
| --- | --- | --- |
| `3Q-AC-01` | Exact admission above. | Predecessor fixture/source pins and state-0 golden. |
| `3Q-AC-02` | Public handles and completion transition. | Input/event/state assertions and boundary matrix. |
| `3Q-AC-03` | Ordered three-event lifecycle. | All replay cuts plus missing/reordered/duplicate rejection. |
| `3Q-AC-04` | Material retention and exact resumption. | World/track/progress/RNG/route terminal assertions. |
| `3Q-AC-05` | Retry/rejection/limits. | Retry, mutation, raw-byte, cross-owner, and boundary suites. |
| `3Q-AC-06` | This scope closure. | Navigation and combined-plan status checks. |

This closes only explicit completion of the exact second-move one-opportunity ordinary-infantry
participant. It does not admit multiple opportunities, positive vehicle Breakdown, wider Reaction
profiles, D2c.4/Task004/checkpoint B composition, runtime/public actions, or simulator execution.
