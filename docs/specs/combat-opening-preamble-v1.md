# Creation-rooted opening preamble v1

`CMB-TASK-003D2c.2a`, input `387445b`. [Schema inventory](combat-opening-preamble-v1.schema.json),
[retained vectors](fixtures/combat-opening-preamble-v1.json),
[oracle](verify-combat-opening-preamble-v1.py), [combined plan](../design/combat-cycle-implementation-plan.md).
This packet freezes four inherited successor payloads and derives their authority from validated
Created11 bytes. Its terminal is **Weather entry**, not Weather resolution, Reserve or Combat.
“Creation-rooted” means an actual prospective contract/oracle chain; C# codecs, runtime replay,
general Snapshot12 admission and simulator support remain unimplemented behind004/checkpoint B.

## Scope and inherited compatibility

The selected C2 context is Rules10/Setup7/Content7/World7 with sequence5. Only first-turn,
first-stage opening under the selected predetermined-Axis setup and its explicit no-convoy-obligation
policy is admitted. Campaign ID and unsigned64 seed are caller inputs validated by C2; setup,
content, configuration and rules identities must match the independently supplied certified context.
No inferred migration, invented prior prefix or external saved-state authority is admitted.

| Current event → successor | Current command → successor | Result position / retained effect |
| --- | --- | --- |
| `initiative-determined`2 →3 | `ResolveInitiative`2 →3 | Naval Convoy Schedule; retain predetermined Axis outcome and RNG algorithm/before/after cursors |
| `no-obligation-naval-convoy-schedule-resolved`1 →2 | `ResolveNoObligationNavalConvoySchedule`1 →2 | Tactical Shipping; certify explicit no-obligation policy |
| `no-obligation-tactical-shipping-resolved`1 →2 | `ResolveNoObligationTacticalShipping`1 →2 | Stage1 Initiative Declaration; preserve holder/resources |
| `initiative-order-declared`1 →2 | `DeclareInitiativeOrder`1 →2 | Weather Determination; retain holder, first side, second side and stage1 order |

These event versions match [D2c.1's declarations](combat-inherited-successors-v1.md). This packet
supersedes the four rows' **declared-only status**, not their assigned versions. D2c.1 remains a
frozen historical inventory; its eighteen uncompleted payloads at that checkpoint now have these
four successors frozen. The other fourteen payloads still await their family packets. Created11
remains [C2-owned](combat-authority-envelope-v1.md); its bytes and historical fixtures are unchanged.

The source audit pins current `CampaignV11Preamble`, its sequence3/4 wrapper, current dispatcher,
command definitions, Initiative resolver/factory, opening factory/serializer and setup policy.
Predetermined Initiative returns the same RNG state and uses setup sources plus Land7.12/7.15.
Convoy events use the certified policy source plus Land5.2. Declaration uses Land5.2/7.11/7.14/7.16.
These semantics and source references are retained; no new gameplay ruling is introduced.
The current wrapper accepts only sequence3/4, so these sequence5/version successors require new
codecs. Source inspection is compatibility evidence, not execution of the future C# readers.

## Exact command and event boundary

Schema strings specify every property, order, type and nullability. `PreambleCommand` is a tagged
schema inventory for the four versioned commands, not an additional runtime command. It includes
kind, creation binding/event hash, expected prior version and position. Only declaration carries
nonnull `operationStage=1`, `declaringSide=axis` and choice `act-first` or `act-last`; the other
commands require all three fields to be null. Unknown fields and old command versions reject.

`PreambleInput.actor` is trusted submission metadata: `system` for the three automatic transitions,
and the replay-derived Initiative holder for declaration. It is not a field a remote caller may
use to authenticate itself. Task004 still owns public audience/action mapping. No asynchronous
window, timer, deadline, model call or fallback is introduced for these synchronous transitions.

Each event retains its old semantic payload and exact source/sequence data, with explicit
campaign/rules/configuration, creation binding/event hash, prior/result authority versions,
pre-event prefix, accepted input and receipt ID. Initiative retains the typed predetermined outcome
and RNG fields; declaration retains operation stage, declaring holder and the two ordered sides.
Every value is recomputed from accepted predecessors and the selected context during readback.
Position source references and relative actor roles come from D1's exact catalog; catalog
`activeSide` remains null. The retained order resolves the first side without changing Initiative.

The receipt is `pre.` plus lowercase SHA-256 of UTF-8 domain
`sandtable.combat.opening-preamble-receipt.v1`, one NUL byte, then canonical event bytes with only
`receiptId` omitted. Event hash includes the receipt. Chronicle prefix uses D1
`prefix_event(priorPrefix, finalCanonicalEventBytes)`. The initial prefix is D1 `prefix_creation`
over the exact C2-validated Created11 bytes. No event or receipt hashes itself.

Canonical JSON is compact ASCII with escaped strings and schema property order. Source/value
arrays use the existing C2 canonical ordering; receipt and history arrays retain chronological
order. Readers reject duplicate/unknown/reordered properties, noncanonical arrays, BOMs, whitespace,
wrong types, unsupported tags, booleans/fractions in integer fields and malformed identities.
Limit:1MiB per record, depth32, arrays512, at most four opening events after one Created11 record.
The whole input chain is therefore bounded by five individually limited records. This is local
fragment capacity, not whole-campaign admission capacity; D2c.4 remains responsible for composition.

## Replay, retry and next-family handoff

`initial(request, created)` validates the request against the fixed registry context and validates
Created11 with C2 before deriving state1 and its creation prefix. `replay(request, created, events)`
then admits only the four exact edges, producing versions2–5. There is no caller-controlled prior
version, prefix, holder, World, RNG or order seed. Missing/replaced creation, a different campaign
or seed, skipped/reordered/duplicate events, or unsupported future event rejects atomically.

`PreambleState`1 is a private oracle projection, **not Snapshot12**. It retains the complete World7,
RNG, creation binding/hash, holder, stage orders, prefix and C3-shaped command receipts.
`read_state` compares supplied canonical bytes against complete replay of the separately supplied
request/creation/suffix. A cached projection or hash cannot certify its own history. Replay proves
that the supplied chain is internally admissible, not that it was published; trusted archive-head
verification and authenticated command admission remain production responsibilities. A wholly
recomputed legal declaration can form another valid branch. On every accepted edge CP/ammo/TOE/
provenance and all World bytes remain identical
to Created11; predetermined Initiative and the other three events leave cursor0 unchanged.

`apply` authenticates the actor before receipt lookup. Exact previously accepted input returns the
original event/receipt with `duplicate=true` and the **current replayed state**, even after later
opening transitions. It emits no new event. A changed command for an already consumed prior-version
occurrence is a conflicting retry. New input must match the current expected version, position,
command kind, actor and choice. Caller objects remain unchanged on success, retry and rejection.
Both valid order choices can fork at declaration, with distinct order/prefix/receipt; a cached state
from one fork cannot be restored against the other. Choice legality does not imply duplicate status.

D2c.2b must consume the accepted request, Created11 bytes and all four opening events, reconstruct
state5 with this reader, and derive Weather from its unchanged RNG and retained holder. It must not
promote a lone `PreambleState` or a supplied prefix to authority. State entry alone makes no claim
about the coming Weather result. Stage entry, Reserve designation/completion, cycle opening and
movement are still outside this packet. ParentD2c.2 stays open until2d composes the full chain.

Errors:001 shape/type/bytes,003 unsupported kind/version/choice,004 creation or predecessor identity,
005 actor,006 history/conflicting retry/cache mismatch,008 canonical bytes,009 source/declaration drift.
These are private oracle diagnostics; public error disclosure remains Task004.

## Verification

`python3 docs/specs/verify-combat-opening-preamble-v1.py` passes six traces: both order choices for
seeds0,12345,unsigned64 max. Thirty replay/state cuts,1050 event mutations,3348 state mutations,
192 raw rejections and204 boundary/retry checks pass. Sixty retained creation/event/state artifact
entries are checked after literal expected positions, versions, first/second sides, holder and RNG/
World assertions. Normal verification never regenerates fixtures. Nine current source hashes and
four command/event declarations are pinned. The creation-rooted regression failed against the
initial non-advancing stub, then passed with four accepted transitions and exact final retry.

This evidence does not certify contested Initiative, later turns/stages, positive convoy obligations,
Weather effects, a live Reserve/Combat boundary, C# byte parity, hosted persistence, public privacy
or simulator execution. The existing contracts and accepted source decisions remain unchanged.
