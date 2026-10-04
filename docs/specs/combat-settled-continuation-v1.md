# Result2 settled continuation evidence v1

CMB-019D0 is a private, unregistered executable contract bridge. It authenticates the bounded
native Round2/Result2/empty Release1 suffix and exports immutable continuation evidence. Earlier
Movement and pre-Combat admission remain **synthetic-pre-combat**. Created11 proves the creation
identity/configuration, not a causal history from creation to this settled boundary. Parents017–019,
actual positive campaign-history admission, repeat authority, Snapshot/publication and public
activation remain open. This contract has no C# implementation or transport registration.

Canonical requirements: [dispatch packet](../design/combat-cycle-post-movement-dispatch.md#frozen-next-dispatch-cmb-019d0-result2-settled-continuation-contract-bridge)
and [Checkpoint H](../design/combat-cycle-implementation-plan.md#checkpoint-h--reserve-release-and-real-continuation).
The [ordered inventory](combat-settled-continuation-v1.schema.json),
[retained vectors](fixtures/combat-settled-continuation-v1.json) and
[oracle](verify-combat-settled-continuation-v1.py) define exact bytes. This schema follows the
repository's ordered type-inventory convention; it is not a general JSON Schema validator.

## Admission and source replay

`SettledPacket` contains a complete `SettledSource`, the derived Release base bytes, two separately
retained Release inputs and two canonical Release event strings. No caller progress, World projection,
repeat flag, witness or terminal hash supplies trust. The oracle reads the unchanged
[Result2 fixture](fixtures/combat-result-settlement-v2.json) as an independently admitted synthetic
catalogue: eight literal cases × two owners × two private-seal orders =32 contexts. Exact source
identity includes every accepted input (actor/time/clock availability), predecessor byte and descriptor.
Full replay happens before extraction; matching catalogue bytes happens after replay. Unsupported IDs,
clock variants, profiles, scope or earlier-history descriptions reject rather than return no witnesses.
The new fixture is regression evidence, not its own admission authority.

The source carries canonical Request and Created11 bytes, Base2, the complete selection/RBA
predecessor, separately retained Round2 inputs and events, committed RoundState2, separately retained
Result2 inputs and events, and closed ResultState2. Readers regenerate Created11 and selection,
round and settlement events/states, compare all original bytes, then check the exact catalogue.
Round2 commitment proof and Result2 independent mandatory-window timing are preserved. Immediate
settlements must be complete, closed, without a pending window, and carry round/CA completion receipts.
All32 admitted cases retain owner-selected custody/retreat choices, with no fallback admission.

The synthetic Movement descriptor is separately pinned by source ID plus descriptor ID. Its label,
receipt source, full original-unit membership, ordered locations, earlier exclusions, scope, ordinal
and `synthetic.movement-completed` receipt are exact catalogue data. The default descriptor uses the
trusted pre-Combat boundary's locations; `distant-original` uses original side anchors and
`prior-exclusion` retains the original own exclusion. These two are explicitly synthetic evidence
probes, not additional reachable campaign histories. Nothing derives earlier proof from settled or
post-retreat World. An arbitrary syntactically valid receipt cannot authenticate Movement.

## Source-to-field mapping

| Output field | Independently replayed source / invariant |
| --- | --- |
| `sourceIdentity` | Hashes of full source, Request/Created11, boundary, complete selection predecessor, Base2, committed RoundState2, closed ResultState2, derived ReleaseBase1/ReleaseState1 and the pinned Movement descriptor; retains Round2 clock hash and Result2 policy ID. |
| `cycle` | Original Base2 boundary Authority1, with acting owner, first relative slot, original ordinal and all content/setup/rules/configuration identities retained. |
| `stateVersion`, `prefix`, `positionId` | Completed Release1 suffix terminal. Exactly two accepted Release events advance authority from the closed result; extracting this proof creates no new event/version or catalogue advance. Position remains the original Reserve Release position. |
| `world`, `randomState` | Closed ResultState2; empty Release's status-only projection equals the entire settled World and RNG. CP/BP/bands/TOE/ammo/Cohesion, positions, relations, representations, broken lots, custody, guards, replacement entitlements and future duties remain byte-equal. |
| `members`, `attackHistory`, `targetUses` | Every original own unit (one in this profile), its cumulative settled CP and empty Reserve history; committed offensive history and target-use records remain unchanged. |
| `releaseCompletionReceiptId` | The replayed `reserve-release-completed` event receipt, never a caller receipt or combat ID. |
| `movementEnd` | Entire separately pinned descriptor including its synthetic provenance and earlier exclusions. |
| `progress` | Exactly one actual `combat-attack-committed` event receipt and hash, matched to committed receipt history, commitment ID, selected attacker and game-turn/stage/owner scope. |
| `witnesses` | Pure relation-aware Clear2 movement assessment using original distance/exclusions, settled World, cumulative CP, occupancy/guards and existing cost/spending rules. No costs are applied. |

Progress binds **receipt ID and canonical event hash**. A commitment ID is a distinct domain identity.
Empty Release opening/completion has zero progress. Private seals, selection cancellation, timers,
retention-only and caller booleans cannot substitute for the required commitment. Broader no-attack
truth-table admission belongs to a separate cycle-control contract.

Empty Release admits only untimed System `open`, then `complete`, with clock available and null
decision/unit/choice. Release audit high-water begins null, independently of the fully retained
Result2 clock history, matching the native017B adapter. Membership, World/RNG and offensive history
cannot drift. There is no release exception, resource spend, repeat permission or successor action.

## Witness outcomes and literal arithmetic

Every one of the32 retained contexts is explicitly `supported`, with null unsupported reason. Both
seal orders and owners have the row's counts. Unsupported inputs reject with CMB-SCT-003/004;
an unsupported profile is never silently represented by an empty legal-move list. The only supported
empty lists in retained descriptor probes result from authenticated original-distance/exclusion rules.
The closed admission catalogue prevents untested armed, Weather, stacked, motorized, vehicle, other
slot/ordinal or released-Reserve offensive profiles from borrowing this result.

| Result2 case | Witness count | Settled CP + terrain + maximum break-off → CP / incremental DP | Guard / replacement / future-duty counts |
| --- | --- | --- | --- |
| ordinary Contact | 1 | 5+2+2 →9 /0 | 0 /0 /0 |
| zero-retreat | 2 | 5+2+0 →7 /0 | 0 /0 /0 |
| refusal-loss-dp Contact | 1 | 5+2+2 →9 /0 | 0 /0 /0 |
| zero-engaged, zero loss | 1 | 5+2+4 →11 /1 | 0 /0 /0 |
| defender-capture-guard | 1 | 5+2+2 →9 /0 | 1 /0 /1 |
| defender-capture-escape | 1 | 5+2+2 →9 /0 | 0 /1 /1 |
| attacker-capture-guard-cp-limit | 2 | 10+2+0 →12 /2 | 1 /0 /1 |
| attacker-capture-escape | 2 | 5+2+0 →7 /0 | 0 /1 /1 |

Guard occupancy and custody are evaluated using each source's own World. Guard-priority upkeep
remains unimplemented and due at its retained activation gate. Escape replacement training retains
eligible turn5/stage1 and its activation gate; it is not matured by this bridge. Regression hashes
follow these literal arithmetic, progress and obligation assertions rather than replacing them.

Separate **independent rule probes**, not admitted histories, check spent9+4+2=15/DP5, reject16,
ordinary incremental DP without refund, strict released-I/II ceilings and checked overflow. These
probes create no packet or campaign reachability claim. Both-owner Engaged witnesses remain available
despite ammunition0. Current retreat/proximity changes cannot remove earlier exclusions or substitute
current distance for original Movement-end distance.

## Compatibility ledger

| Concern | Historical settled-control | This new bridge |
| --- | --- | --- |
| Source authority | `verify-combat-cycle-control-v1.py` imports historical Result1/Round1 through Release1. | Actual retained Round2/Result2 source catalogue, exact native predecessor replay. |
| Clocks | Historical input/occurrence/clock framing. | Explicit Round2 public-opening clock configuration hash and Result2 mandatory-window policy, separately retained actor/clock inputs. |
| World/occurrence identity | Historical hashes and IDs remain frozen. | New native settlement/commitment/receipt/source hashes; old hashes do not authenticate new source bytes. World field vocabulary is reused, not historical state identity. |
| Earlier Movement | `probe.movement-completed`, fabricated historical proof. | Separately pinned `synthetic-pre-combat` descriptor with explicit original-unit locations/exclusions/receipt source; no claim of actual completion admission. |
| Control | Historical owner-choice/repeat/finish oracle. | New `sct.` proof domain only; no control window, outcome or permission. |
| Empty Release | Historical settled audit high-water inherited. | Native017B null Release audit high-water and untimed System suffix. |

Existing versions, domains, readers and frozen fixture bytes are unchanged. Readers of older
contracts reject the new closed shape; this proof cannot be supplied as a historical CycleControlBase.
No new Snapshot, command, event, action, gameplay predecessor type or protobuf field is registered.

## Canonical bytes, ownership and limits

`bridge(canonical_packet_bytes)` returns owned immutable canonical proof bytes. `read_proof` accepts
only canonical bytes exactly regenerated from an independently admitted full packet. A SHA digest or
self-consistent proof alone never supplies trust. `proof_id` identifies canonical bytes in
`sandtable.combat.settled-continuation.v1`; it is not an admission function. The successful cache uses
complete immutable canonical packet bytes, returns only immutable bytes and holds no caller objects.
Callers can mutate their parsed projections without mutating an earlier result or cached proof.

Ordered object fields follow the inventory; ASCII JSON has no whitespace, duplicate/unknown fields,
NaN, partial coercions or booleans used as integers. Whole packets/proofs and embedded upstream values
are bounded to1MiB; depth32, arrays512, round/result events16, Release events exactly2. Source envelopes
with extra, missing or ignored metadata fail exact catalogue comparison. Integers obey imported
32/64-bit contracts. Upstream transitions check authority increments; movement cost addition is
checked at the signed64-bit boundary before ceiling rejection, and Cohesion DP overflow rejects.

CMB-SCT codes:001 shape/capacity,003 unsupported version/profile,004 source/trust mismatch,
005 incomplete or invalid empty Release,006 proof mismatch,008 noncanonical bytes. Imported upstream
failures are normalized at the source/Release boundary. No rejected packet can publish partial proof.

## Verification and next bounded action

Run `python3 -B docs/specs/verify-combat-settled-continuation-v1.py`. Semantic RED preceded implementation:
an importable missing bridge failed literal costs and receipt/hash progress assertions. GREEN requires
all semantic groups, literal32-row checks, new frozen byte readback/pins and preservation/rejection
checks. Existing Result2, sealed-round2, Reserve Release, ordinary Movement, cycle-control and3m
oracles must remain unchanged and pass. Repository `just check` is regression evidence; .NET success
does not implement this Python contract. Exact executed evidence belongs in the
[handoff](../work/handoffs/2026-10-04-settled-continuation-contract.md).

Following acceptance, freeze a bounded native settled-proof adapter using017B source replay and019A
assessment, retaining this synthetic trust label. Settled control and genuine creation-to-positive
Combat/ordinary repeat admission remain separate contracts. Converted-II/consumed offensive and
public gameplay remain gated; no parent017–019 closure is asserted here.
