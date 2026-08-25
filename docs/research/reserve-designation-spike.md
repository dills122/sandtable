# Reserve Designation v1 Source and Contract Spike

**Status:** Implemented through `RES-TASK-015`; final repository gate and independent review in
progress

**Date:** 2026-08-24

**Decision owner:** Project owner

**Rules target:** `cna-1979.1`

**Roadmap capability:** `RESERVE-001`

## Executive conclusion

The smallest honest capability after Operation-Stage Entry is the first acting side's Reserve
Designation decision. The acting player may designate zero or more of their currently represented
units as Reserve I, one unit at a time, then explicitly complete the phase. Completion advances to
the unchanged catalog first-side Movement position and stops before Movement behavior.

The recommended interaction is incremental rather than a submitted batch. Each eligible element
is a legal-action candidate whose canonical semantics include `elementId`; a separate completion
candidate supports zero selections. Every accepted designation emits one replayable event, updates
one authoritative element from `None` to `ReserveI`, and remains at the same sequence position.
Completion emits its own event and advances exactly once. This fits the existing opaque-action-ID
submission and receipt contracts without teaching them a second payload channel.

Reserve status belongs in authoritative campaign element state. It is projected only on the owning
side's existing `ownElements` observation and never to the opponent or Intelligence plane. The
merged Exercise/Maneuver harness is acceptance evidence, using an explicit Reserve-aware controller
rather than relying on incidental candidate sort order.

## Decision question

> What is the smallest source-faithful, deterministic Reserve Designation capability that advances
> the current first acting side from Reserve to Movement without deleting the player's choice,
> leaking opposing state, or prematurely implementing the Reserve lifecycle?

## Scope and stop condition

This spike covers:

- the original Reserve Designation rule and official errata relevant to it;
- the two admitted synthetic setups at Operation Stage 1;
- zero-or-more first-side Reserve I designations and explicit completion;
- authority state, legal actions, commands, events, replay, observations, and validation;
- coordinated contract/version consequences; and
- checked single-Exercise and serial-Maneuver acceptance evidence.

It does not implement Movement, Reserve Release, Reserve II creation, Reserve movement/combat
restrictions, second-side Reserve Designation, Capability Point effects beyond the no-cost
designation invariant, pinned-state behavior,
positive Organization/Fleet mechanics, UI, Needle, persistence, or model-backed decisions.

The original spike stop condition was this decision packet, its proposed specification, and its
technical task graph. The project owner subsequently granted the required implementation approval
on 2026-08-24; the delivered scope remains bounded by the exclusions above.

## Source method and evidence

Sources were evaluated in this order:

1. approved Sandtable architecture and current implemented contracts;
2. the original 1979 Land Rules and Sequence of Play;
3. the official September 1979 errata; and
4. repository behavior at the merged Reserve checkpoint.

No scan or copied rules prose is committed. Stable locators and short conclusions are retained.

### Documented source facts

| Fact | Stable source reference |
| --- | --- |
| Player A designates reserves after Fleet Repair and before Movement/Combat. | `spi-1979-land-rules:5.2.reserve-designation` |
| Any units belonging to the phasing player may be placed in Reserve; the non-phasing player may not designate units. | `spi-1979-land-rules:18.11` |
| Assignment occurs during the Reserve Designation phase and is represented by Reserve I. | `spi-1979-land-rules:18.12` |
| Reserve assignment occurs only during Reserve Designation. | `spi-1979-land-rules:18.15` |
| Reserve has Reserve I and Reserve II statuses. | `spi-1979-land-rules:18.21` |
| Designation and later release do not cost Capability Points. | `spi-1979-land-rules:18.26` |
| The official errata contains no indexed Section 18 correction. Its only Reserve-related text redirects the definition of Pinned under air bombardment to Section 41.9. | `spi-1979-errata:3.0`; `spi-1979-errata:41.9` |

The source permits units to be designated but does not state a minimum count. V1 therefore needs a
recorded interpretation for an empty selection rather than inventing a mandatory designation.

### Repository observations

- The merged path reaches
  `land.position.operation-1.first-player.reserve-designation` at state version 10.
- The position stores `ActorRole=FirstActingSide` and null `ActiveSide`; the retained operation-stage
  order resolves the actual acting side for observations and legal actions.
- Each synthetic setup has four independent combat elements: two Axis and two Commonwealth. The
  existing content record already carries side ownership.
- `CampaignElementState` currently contains only element identity and location. Reserve is mutable
  authority and does not belong in the Content Pack.
- Side legal actions are generated from a side-safe observation containing only the observer's own
  independent elements.
- Submissions and acceptance receipts identify a choice only by opaque `actionId`. Candidate
  semantics therefore must bind the selected element.
- Snapshot/world and observation decoders are strict. Adding reserve status is an intentional
  versioned contract change.
- The current checkpoint validator assumes one event per sequence position and directly maps state
  versions 1–10 to the first ten positions. Reserve introduces multiple accepted events at one
  position, so that temporary shortcut must be generalized.
- The current Exercise controller receives only opaque action IDs and is stateless. A Reserve-aware
  policy cannot inspect candidate kind or element subject until the runner gains a closed semantic
  candidate view.
- Context-free snapshot deserialization currently calls local checkpoint validation, while Reserve
  ownership bounds require `CampaignContentContext`. Structural decoding and authoritative
  context validation must become explicit separate layers.

### Inferences

- Incremental element designation plus explicit completion preserves the original decision and
  supports an empty subset without an exponential candidate set.
- A batch submission would require a second semantic payload in submission/receipt/re-adjudication
  evidence. It is broader and less compatible with the current authority path.
- A designation candidate's action ID can safely include `elementId`; stale membership re-checking
  then rejects repeated, forged, opposing, or no-longer-current choices.
- Reserve status should be visible to the owner for confirmation and absent from every opposing
  observation. Legal actions can remain observation-derived and fog-safe.
- The state validator can remain closed and bounded by deriving the maximum number of designation
  events from the acting side's represented elements.
- Reserve II belongs in the state enum so serialized identity will not need another shape change,
  but Reserve v1 must never create it.

## Options considered

| Option | Result | Decision |
| --- | --- | --- |
| Automatically advance with no Reserve choice | Deletes an authentic player decision. | Reject |
| Offer every possible subset as one candidate | Exponential candidate growth and poor UI/evidence shape. | Reject |
| Add selected element IDs to the action submission | Duplicates candidate semantics and broadens receipt/re-adjudication contracts. | Reject |
| Submit one batch command containing all selected IDs | Deterministic but creates a new payload channel and delays feedback until completion. | Defer |
| Implement the full Reserve I/II/release/movement lifecycle | Crosses Movement/Combat and several unnormalized rules. | Defer |
| Designate one eligible element per action, then explicitly complete | Bounded, replayable, stale-safe, UI-friendly, and compatible with opaque action IDs. | **Recommend** |

## Proposed owner decisions

| ID | Proposed decision | Status |
| --- | --- | --- |
| `RES-DEC-001` | Support only the current first-acting-side Reserve Designation checkpoint in the two admitted synthetic setups. | Accepted 2026-08-24 |
| `RES-DEC-002` | Allow zero or more designations and require an explicit completion action; record the zero-selection interpretation in the ruleset manifest. | Accepted 2026-08-24 |
| `RES-DEC-003` | Represent element status as the closed enum `None=0`, `ReserveI=1`, `ReserveII=2`; v1 may perform only `None → ReserveI`. | Accepted 2026-08-24 |
| `RES-DEC-004` | Generate one `designate-reserve` candidate per eligible acting-side element plus one `complete-reserve-designation` candidate. | Accepted 2026-08-24 |
| `RES-DEC-005` | Bind `elementId` into designation candidate canonical semantics/action ID; do not change submission or receipt shapes. | Accepted 2026-08-24 |
| `RES-DEC-006` | Eligibility is an independent represented element owned by the resolved first side and currently in `None`; opposing, absent, attached, already-reserve, and forged IDs reject. | Accepted 2026-08-24 |
| `RES-DEC-007` | Emit distinct `ReserveElementDesignated` and `ReserveDesignationCompleted` events and recompute both during replay. | Accepted 2026-08-24 |
| `RES-DEC-008` | Project reserve status only in the owner's `ObservedOwnElement`; preserve opponent byte invariance. | Accepted 2026-08-24 |
| `RES-DEC-009` | Completion advances to the exact catalog first-side Movement position and implements no Movement legal action. | Accepted 2026-08-24 |
| `RES-DEC-010` | Clean-cut the world, snapshot, creation, observation, and ruleset identities; keep setup/content, Land sequence, legal-action set, candidate base, submission, and receipt versions unchanged. | Accepted 2026-08-24; observation/action-set portions superseded by Task 016 fog remediation after `RDV1-001` proved global set revision leaked the hidden count. |
| `RES-DEC-011` | Replace the one-event-per-position validator shortcut with explicit bounded Reserve checkpoint invariants derived from acting-side ownership and selected count. | Accepted 2026-08-24 |
| `RES-DEC-012` | Add a deterministic Reserve-aware Exercise controller that designates every eligible offered element before completion, and make both checked setups reach Movement with trusted replay and fresh re-adjudication proof in the first implementation cycle. | Accepted 2026-08-24; clarified by review 1 |

## Recommended contract boundary

The project owner accepted `RES-DEC-001` through `RES-DEC-012` as written on 2026-08-24, and the
planning review gate passed after reconciliation. Production work implements the rules,
world/snapshot, observation, candidate, command-mapping, bounded Reserve/Movement checkpoints,
designation/completion transitions, replay, and checked Movement-terminal harness evidence.

The acting side sees a closed legal-action set shaped conceptually as:

```text
complete-reserve-designation
designate-reserve(elementId = <eligible own element>)
designate-reserve(elementId = <eligible own element>)
```

After one designation, that element's candidate disappears, its owner-visible status becomes
`reserve-i`, and the position remains Reserve Designation. Completion remains legal throughout.
After completion, the world is preserved and the sequence moves to first-side Movement.

The canonical designation candidate semantics are:

```text
contractVersion, kind, elementId
```

The completion candidate remains payload-free:

```text
contractVersion, kind
```

Existing candidates retain their current bytes and action IDs. The new kind is a new discriminated
variant, so the base candidate contract does not need to change.

For trusted Exercise instrumentation, the controller receives a separate closed semantic view of
each current candidate: action ID, kind, and optional element ID. The Reserve policy is stateless:
while any `designate-reserve` candidate exists it chooses the ordinally first element; once none
remain it chooses `complete-reserve-designation`. Current legal-action regeneration is the progress
signal, so the controller retains no fixture-specific action count or hidden authority.

The authority path is:

```text
side-safe observation
  -> current Reserve legal candidates
  -> opaque action-ID submission
  -> current membership re-check
  -> mechanic-specific command
  -> Umpire validation
  -> exactly one event
  -> projection + replay validation
```

## Confidence and change triggers

Confidence is high in the phase order, phasing-side ownership rule, Reserve I assignment, current
repository seams, and incremental action fit. Confidence is medium-high in allowing zero selected
units because the rule grants permission and states no minimum; the interpretation is made explicit
for that reason.

Reopen this decision if primary evidence mandates at least one unit, if attached/non-independent
elements enter the current world before implementation, if Reserve status must be publicly visible
to an opponent under an adopted fog policy, or if Movement requires a materially different state
representation.

## Sources

- [Original 1979 Land Rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
- [Official September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
- [Official SPI rules-download index](https://spigames.net/rules_downloads.htm)
- [Operation-Stage Entry research](operation-stage-entry-spike.md)
- [Exercise Harness v1 specification](../specs/exercise-harness-v1.md)
