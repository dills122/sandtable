# Reserve Designation v1 Specification

**Status:** Implemented, repository-verified, independently reviewed, and merged through
`RES-TASK-016`

**Date:** 2026-08-24

**Capability:** `RESERVE-001`

**Research:** [Reserve Designation v1 source and contract spike](../research/reserve-designation-spike.md)

**Technical design:** [Reserve Designation v1](../design/reserve-designation-v1.md)

**Predecessors:** [Operation-Stage Entry v1](operation-stage-entry-v1.md),
[Legal Actions v1](legal-actions-v1.md), [Campaign Observation v1](campaign-observation-v1.md),
[Exercise Harness v1](exercise-harness-v1.md)

## Objective

Let the resolved first acting side designate zero or more eligible represented units as Reserve I
during the current Operation Stage 1 Reserve Designation checkpoint, then explicitly complete the
phase and advance to the exact first-side Movement position.

Every choice must use the current side-safe legal-action path, emit exactly one authoritative event,
replay to byte-equivalent state, reject stale or forged choices without mutation, and preserve
opponent observation bytes. Movement and the later Reserve lifecycle remain unsupported.

## Approval boundary

The project owner accepted `RES-DEC-001` through `RES-DEC-012` as written on 2026-08-24. The final
independent planning review passed after all findings were reconciled; no unresolved P0/P1/P2
planning finding remains. `RES-TASK-001` through `RES-TASK-016` are complete, including both
authoritative transitions, the coordinated Harness v2 evidence lane, repository verification, and
the final independent implementation review.

## Functional requirements

| ID | Requirement |
| --- | --- |
| `RES-REQ-001` | At the current Reserve checkpoint, only the resolved first acting side receives Reserve candidates. System and non-acting-side sets are empty. |
| `RES-REQ-002` | The acting side always receives one explicit completion candidate. |
| `RES-REQ-003` | The acting side receives one designation candidate for each own independent represented element whose status is `None`. |
| `RES-REQ-004` | A designation candidate canonically binds `elementId` into its action ID; existing candidate semantics and IDs do not change. |
| `RES-REQ-005` | Accepted designation changes exactly one element `None → ReserveI`, increments state version once, and remains at the same Reserve sequence position. |
| `RES-REQ-006` | An accepted completion changes no element, increments state version once, and advances to the exact catalog first-side Movement successor. |
| `RES-REQ-007` | Zero designations followed by completion is accepted. Every eligible element may be designated at most once. |
| `RES-REQ-008` | Opposing, unknown, attached/non-represented, already-reserve, stale-state, wrong-position, and wrong-audience submissions reject with zero events and unchanged authority. |
| `RES-REQ-009` | Replay recomputes and verifies designation and completion events from prior authority and rejects forged element, side, status, source, position, or successor data. |
| `RES-REQ-010` | The owner observes each own element's reserve status; no opposing element or reserve status enters the observation or side legal-action bytes. |
| `RES-REQ-011` | Initial world state explicitly records `None`; v1 never produces `ReserveII`. |
| `RES-REQ-012` | Snapshot/checkpoint validation permits only the finite Reserve paths derivable from the current acting side's eligible element count. |
| `RES-REQ-013` | The ruleset manifest hashes normalized Reserve Designation authority and the adopted empty-selection interpretation with stable primary-source references. |
| `RES-REQ-014` | Checked Exercise and two-setup serial Maneuver runs use a semantic, stateless Reserve controller to designate every eligible element, reach first-side Movement, and prove trusted event replay plus fresh-session re-adjudication. |
| `RES-REQ-015` | No Movement candidate, Reserve Release behavior, Capability Point mutation, pinned behavior, model call, or remote I/O is introduced. |

## Domain contract

### Authoritative element state

Each represented campaign element carries one closed value:

| Numeric value | Canonical value | Meaning in v1 |
| ---: | --- | --- |
| `0` | `none` | Not assigned to Reserve. |
| `1` | `reserve-i` | Assigned during the current Reserve Designation phase. |
| `2` | `reserve-ii` | Reserved for the later lifecycle; invalid as a v1 transition result. |

Initial world creation sets every element to `None`. Reserve status is mutable authority and is not
added to Content Pack or setup identity.

### Owner-visible reserve status

The public observation property has the exact type
`CampaignObservationReserveStatus`, a distinct observation-owned enum:

```text
None = 0
ReserveI = 1
ReserveII = 2
```

`ObservedOwnElement.ReserveStatus` uses that type; authoritative
`CampaignElementReserveStatus` is not public. Construction and serialization reject undefined
numeric values. Canonical strings are `none`, `reserve-i`, and `reserve-ii`; record equality and
hashing include the property. Campaign Observation contract v4 owns this shape and the
audience-visible revision policy described below. V1 projections emit only `None` or `ReserveI`.

### Legal-action candidates

The new candidate kinds are exact:

```text
designate-reserve
complete-reserve-designation
```

`DesignateReserveAction` carries one stable `ElementId`. Its canonical semantic property order is
`contractVersion`, `kind`, `elementId`; `ActionId` is the SHA-256 identity of those bytes. The
completion candidate has no subject payload and retains the existing `contractVersion`, `kind`
semantic shape.

Candidate sets remain canonically ordered by kind then action ID. Clients and Exercise controllers
must select by candidate kind/subject semantics, never by incidental position in the array.
Legal-action-set contract v2 and policy `sandtable.legal-actions.v2` define `stateVersion` as the
audience-visible revision; candidate, submission, and receipt contracts remain v1.

### Commands and events

```text
DesignateReserveElement
  -> ReserveElementDesignated

CompleteReserveDesignation
  -> ReserveDesignationCompleted
```

Both commands carry expected state version, expected position ID, and acting side. Designation also
carries element ID. Both events retain campaign ID, next state version, from-position ID, acting
side, game turn, operation stage, canonical source references, and sequence position. The
designation event also retains element ID, prior status, and resulting status.

V1 event invariants are exact:

- prior status is `None`;
- resulting status is `ReserveI`;
- designation sequence position equals the current Reserve position;
- completion successor equals `Cna1979LandSequence.GetNext(current)` and is first-side Movement; and
- neither event changes location, initiative, operation-stage order, Weather, random state, setup,
  content selection, or ruleset identity.

## Eligibility and completion policy

An element is eligible only when all conditions hold:

1. authority is at Operation Stage 1, first-player Reserve Designation;
2. the submitting audience resolves to the retained first acting side;
3. the Content Pack element belongs to that side;
4. it is independently represented in the current world;
5. its authoritative status is `None`; and
6. the action ID is a member of the current legal-action set.

Completion is legal at the same checkpoint regardless of selected count, including zero. It does
not auto-designate, clear, release, or convert any unit.

## Observation and fog requirements

`ObservedOwnElement` adds `reserveStatus` after `currentLocationId` in canonical JSON. The observer
sees only their own elements exactly as before; the opponent's world state is not present. For each
observer, changing only opposing reserve status must leave the complete canonical observation and
legal-action bytes unchanged.

Campaign Observation contract v4 and policy `sandtable.observation.own-elements-only.v2` preserve
the authoritative snapshot revision for the owner. At the delivered hidden Reserve and Movement
checkpoints, the opponent receives an audience-visible revision equal to the authoritative revision
minus the opposing Reserve-I count: exactly 10 throughout Reserve and 11 at Movement. Empty opposing
legal-action sets copy that same audience revision. The global authoritative revision, event
versions, receipts, replay, and acting-side submissions remain unchanged and internal to authority.

Reserve status must not be sent to `Cna.Intelligence.Gateway`, stored in an advisory contract, or
accepted from a model proposal in v1.

## Structural and authoritative validation

Snapshot parsing remains context-free and performs strict structural validation. It verifies exact
contract/property shapes, defined reserve-status values, unique stable element IDs, stable location
IDs, no `ReserveII` at the admitted Reserve/Movement checkpoints, and status-count/state-version
arithmetic bounded by the total represented world count. A structurally decoded snapshot is not an
authority handle and may not prove content ownership.

Context-authoritative validation additionally receives `CampaignContentContext`. It verifies exact
world/content membership, represented independent placement, retained first-side resolution, and
the owner-specific bound. At both Reserve and its Movement successor, only represented independent
elements owned by the resolved first side may be `ReserveI`; every other element is `None`; no
element is `ReserveII`. Action execution, event projection, replay, observation projection, and
authority-handle construction require this layer.

Completion preserving the exact prior world is proven by recomputing and projecting
`ReserveDesignationCompleted`; a stateless snapshot validator proves the resulting state shape but
does not claim historical preservation by itself.

## Acceptance scenarios

| ID | Scenario | Required evidence |
| --- | --- | --- |
| `RES-AC-001` | Query at Reserve for both setups. | Only the resolved first side receives two element candidates plus completion; other audiences receive none. |
| `RES-AC-002` | Complete immediately. | One completion event advances from state 10 to state 11 Movement with every status `None`. |
| `RES-AC-003` | Designate one element, then complete. | One designation event remains at Reserve; candidate disappears; owner sees `reserve-i`; completion reaches Movement. |
| `RES-AC-004` | Designate every eligible element. | Each produces one event and unique action ID; final completion reaches Movement within the derived bound. |
| `RES-AC-005` | Submit stale, repeated, opposing, unknown, wrong-audience, or forged action. | Typed rejection, zero events, byte-identical prior snapshot. |
| `RES-AC-006` | Replay accepted zero-, one-, and all-selection histories. | Canonical events and final snapshots are byte-equivalent; forged histories fail closed. |
| `RES-AC-007` | Adjudicate zero, one, and all opposing designations as separate valid histories at Reserve and after completion. | Complete observation and legal-action bytes for the opponent are identical at each public checkpoint; owner bytes retain exact own status. |
| `RES-AC-008` | Serialize/deserialize world, snapshot, creation event, Reserve events, and observations. | Exact canonical order, current-version acceptance, old/malformed/unknown-value rejection. |
| `RES-AC-009` | Run checked standalone Reserve Exercise. | Success at first-side Movement after designating every eligible element through current semantic candidates, with trusted replay and fresh re-adjudication proofs. |
| `RES-AC-010` | Run checked serial Maneuver for predetermined and contested setups. | Both children reach Movement; report/fingerprint is deterministic and success counts are exact. |
| `RES-AC-011` | Query at resulting Movement checkpoint. | No legal candidate for any audience; Movement remains explicitly unsupported. |
| `RES-AC-012` | Run repository gates. | Format clean, warning-free solution build, all tests green, and `just check` passes. |

## Verification commands

Use native .NET 10 Microsoft.Testing.Platform mode:

```bash
dotnet restore Sandtable.slnx
dotnet build Sandtable.slnx --no-restore
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build
dotnet test --project tests/Cna.ExerciseRunner.Tests/Cna.ExerciseRunner.Tests.csproj --no-build
dotnet test --solution Sandtable.slnx --no-build
dotnet format Sandtable.slnx --verify-no-changes --no-restore
just check
```

The checked standalone profile is
`scenarios/exercises/rules-lab.reserve-designation.v2.json`; its baseline twin differs only by
build mode. The checked two-setup profile is
`scenarios/maneuvers/rules-lab.reserve-designation.serial.v2.json`, whose deterministic report
fingerprint is
`sha256:9621ee95f7b944f3cea226a9f00f63d782cc417f094543e34f8c36c683f68e1e`.

## Traceability

| Requirement group | Acceptance evidence |
| --- | --- |
| `RES-REQ-001`–`004` | `RES-AC-001`, `RES-AC-003`, `RES-AC-005`, `RES-AC-008` |
| `RES-REQ-005`–`009` | `RES-AC-002`–`006` |
| `RES-REQ-010`–`013` | `RES-AC-006`–`008` |
| `RES-REQ-014` | `RES-AC-009`, `RES-AC-010`, `RES-AC-012` |
| `RES-REQ-015` | `RES-AC-011`, public-surface tests, repository review |

## Owner approval gate

The owner confirmed all five gates below on 2026-08-24:

1. zero selection is allowed;
2. incremental designation plus explicit completion is the desired interaction;
3. owner-only reserve-status visibility is the intended fog boundary;
4. the coordinated clean-cut contract migration is acceptable; and
5. Reserve v1 stops at Movement with no Movement action.

The planning gate is closed by the reconciled final review. Production implementation never
begins while a P0/P1 planning finding remains unresolved. Implementation checkpoint findings block
their dependent task until corrected and independently re-reviewed.
