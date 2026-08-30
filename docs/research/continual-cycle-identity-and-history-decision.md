# Continual-Cycle Identity and Attacked-Target History Decision

**Status:** Decision-ready research; cycle control semantics frozen; no production contract or
implementation authorized

**Date:** 2026-08-29

**Decision owner:** Project owner

**Research work item:** `CYCLE-RSH-001`

**Parent research:** [Combat and Continual-Cycle Source Inventory](combat-cycle-source-inventory.md)

## Executive decision

The first cycle of each acting-side Movement and Combat Phase has ordinal `1`. A repeat increments
that ordinal by exactly one inside the same game turn, Operation Stage, and first- or second-acting
player slot. The ordinal is not a state version, event count, combat-opportunity number, or duplicate
Land-sequence position. One authoritative cycle identity binds the ordinal to campaign, ruleset,
setup, Content, side/order, and opening-state provenance. A separate audience-safe cycle reference
omits hidden authority revision and configuration inputs so equal visible histories remain equal.

After all current Reserve Release obligations are resolved, the acting player may finish. The player
may repeat only when current authority proves both a legal next-cycle Movement or Combat continuation
and material progress in the cycle being closed. Repeat returns to the same acting-side Movement
segment and traverses Breakdown, Combat, and Reserve Release again. Finish advances to the existing
post-Movement-and-Combat successor. No-continuation and unavailable-controller cases close through a
deterministic finish event; they never auto-repeat.

The Umpire retains an ordered, Operation-Stage-scoped attacked-target history. Rule 8.25 expressly
permits the same Friendly unit to attack the same Enemy unit more than once in an Operations Stage,
even if neither moves, subject to current ammunition and Capability Point requirements. History is
therefore evidence, not a uniqueness constraint. For the admitted Close Assault vertical, each
irreversible attack commitment atomically appends every canonical attacking-unit/target-unit
occurrence before random resolution. Repetition appends a new occurrence; it never rejects merely
because the same pair appeared earlier. Chronicle retains the complete history, while an Archives
snapshot retains the complete active-stage context required by this decision. Player observations and
histories expose only side-safe derived legality and already-authorized apparent facts, never the
authoritative occurrence collection, real opposing identities, hidden counts, or closure reason.

These rulings define the deterministic boundary for later `CYCLE-DES-001`. They deliberately do not
choose production type names, serialized fields, combat-opportunity identity, Reserve I/II release
eligibility, combat-result contracts, or Reserve Release events.

## Decision question and boundary

This packet answers:

> What cycle identity, repeat/finish transition, and attacked-target history must a later design
> preserve so that one continual combat cycle can repeat, close, snapshot, and replay without
> duplicating one committed attack occurrence, leaking fog, reopening sealed decisions, or
> depending on model availability?

In scope:

- a stable 1-based cycle ordinal and exact semantic identity inputs;
- repeat, voluntary finish, zero-opportunity closure, and unavailable-controller fallback;
- a monotonic progress rule that prevents replayable no-op loops;
- directional attacked-target history for the first admitted Close Assault vertical;
- active-snapshot, full-Chronicle, replay, stale, duplicate, fog, and provenance invariants; and
- traceability into later design, implementation, and acceptance work.

Out of scope:

- production command, action, event, snapshot, observation, protobuf, Runner, or rules types;
- Contact-derived combat-opportunity, target, participant, Contact, or Engaged identity;
- Barrage, Anti-Armor, loss, retreat, capture, or result contracts;
- Reserve I/II release eligibility, ordering, history, or exact movement-exception contract;
- Breakdown adjudication or Breakdown/Retreat continuity changes; and
- any edit to Core, Signals, Movement, Contact/ZOC, Runner, or shared architecture/roadmap files.

The packet is decision-ready when later design can derive one cycle occurrence, distinguish a
source-legal repeated attacker/target use from a duplicate submission/event, choose repeat versus
finish without ambiguity, and reconstruct those answers from Chronicle or an active-stage snapshot.

## Method and source precedence

The official scans were downloaded to a temporary directory, rendered with Poppler, and visually
checked at the cited PDF pages. No scan, chart, source prose, or artwork is committed. Repository
production code and tests were inspected read-only as architecture and compatibility evidence.

| Source | Exact locator | Decision use |
| --- | --- | --- |
| [Original 1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF page 12; 5.2.G, Movement and Combat Phase | Four-segment cycle, optional repetition, and the requirement that every repetition includes Movement, Breakdown Determination, Combat, and Reserve Release |
| [Original 1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF page 14; 8.21-8.25 | Continual Movement, movement/combat sequencing, distance-based continuation, Reserve exception, and permission for repeated same-unit/same-target combat with current ammunition and CP |
| [Original 1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF page 28; 18.13-18.26 | Mandatory first-release resolution, Reserve I/II persistence, subsequent release, ensuing-segment movement/combat restrictions, and no release CP cost |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | PDF page 1; 8.23 | Corrects Rule 8.23's Reserve exception self-reference from 8.22 to 8.23 |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | PDF page 1; 12.44 | Barrage fire targets a specific battalion-level equivalent rather than a hex; evidence is limited to that combat surface |
| [`Cna1979LandSequence`](../../src/Cna.Core/Rules/Cna1979LandSequence.cs) | `CreateTurn`, `GetNext`, and `AddPlayerPhase` | Current catalog has one structural Movement/Breakdown/Combat/Reserve Release pass and stable first-/second-acting slots, but no repeated occurrence identity |
| [`CampaignSnapshot`](../../src/Cna.Core/Campaigns/CampaignSnapshot.cs) and [serializer](../../src/Cna.Core/Campaigns/CampaignSnapshotSerializer.cs) | contract 9; canonical root fields and setup/world/position encoding | Current authority carries campaign ID, state version, ruleset hash, setup/content selection, world, RNG cursor, and one sequence position |
| [`CampaignSetupSnapshot`](../../src/Cna.Core/Campaigns/CampaignSetupSnapshot.cs) | setup ID/hash and `Content` selection | Current configuration provenance contains setup identity plus Content Pack ID/hash and scenario ID |
| [`CampaignActionSubmission`](../../src/Cna.Core/Actions/CampaignActionSubmission.cs) and [`CampaignActionExecution`](../../src/Cna.Core/Actions/CampaignActionExecution.cs) | exact campaign/version/position/audience/action membership checks | Current stale/membership behavior to preserve through a later audience-safe binding; raw authority-version equality is not safe after hidden events |
| [`CampaignProjector`](../../src/Cna.Core/Campaigns/CampaignProjector.cs) and [replay preparation tests](../../tests/Cna.Core.Tests/Campaigns/CampaignReplayPreparationTests.cs) | event recomputation; `CanonicalCompleteHistoryFreshReplaysWithIdenticalActions` | Current replay requires canonical events to reproduce identical snapshots and per-audience legal actions |
| [`CampaignObservationProjector`](../../src/Cna.Core/Observations/CampaignObservationProjector.cs) | `ProjectStateVersion` | Current fog projection removes hidden opposing Reserve events from audience-visible revision; internal state version is not a safe outward identity input |
| [Exercise harness design](../design/exercise-harness-v1.md) | paired campaign identity and post-divergence limitation | Paired variants deliberately share campaign ID and creation inputs but may diverge, so equal state-version counts cannot disambiguate authority branches |
| [CONTACT-001 source/ruling lock](contact-reaction-zoc-source-ruling-lock.md) | stable identity; stale/duplicate/replay; truth and fog-safe projections | Accepted persisted-window, current-membership, deterministic closure, and redacted-history patterns |
| [Breakdown continuity decision](breakdown-continuity-spike.md) | minimum continuity boundary and acceptance vectors | BP, Sandstorm-attributed BP, and highest checked band persist across the Operation Stage rather than resetting per cycle |
| [Combat result-surface research](combat-rules-result-surface-spike.md) | selected Close Assault vector and deferred identity/state boundaries | Bounds the first attacked-target use to the admitted Close Assault vertical without freezing its production contracts |

Repository architecture has precedence for authority, replay, fog, and compatibility. The official
rules and errata have precedence for the adopted `cna-1979.1` sequence and eligibility facts. Digital
identity, stale retries, automatic fallback, snapshot compaction, and observation redaction are
explicit Sandtable rulings, not claims about the physical rules text.

## Decision table

`Accept` freezes a source interpretation or digital semantic for later design. `Reject` forbids an
alternative. `Escalate` assigns an exact downstream choice whose answer is not required to close
this research item.

| ID | Disposition | Frozen decision | Evidence | Limitation or deferral |
| --- | --- | --- | --- | --- |
| `CYCLE-DEC-001` | **Accept** | Cycle ordinal is positive and 1-based. It resets to `1` for each `(gameTurn, operationStage, playerPhaseSlot)` and increments only when an accepted repeat opens the next cycle. | 5.2.G repeats the same acting player's four segments; repository positions already distinguish game turn, stage, and first-/second-acting slot. | The source has no ordinal; 1-based numbering is a digital replay ruling. |
| `CYCLE-DEC-002` | **Accept** | Use two linked identifiers: an internal authoritative digest containing full canonical provenance, including opening authority version and a digest of the authoritative Chronicle prefix immediately before the cycle-opening transition, and an outward cycle reference containing only audience-invariant sequence facts. Never expose the authoritative digest. | Current snapshot/setup retain full authority provenance; paired Exercise variants can share campaign ID and event count after diverging; current observation projection removes hidden opponent revisions; CONTACT-001 separates authoritative and public identity. | Chronicle-prefix canonicalization, hash algorithms, wire field names, contract versions, and mapping representation remain `CYCLE-DES-001` choices. |
| `CYCLE-DEC-003` | **Accept** | Repeat is legal only after Reserve Release obligations clear, when at least one legal Movement or Combat continuation exists and the closing cycle has material progress. It increments ordinal once and returns to the same acting-side Movement segment. | 5.2.G requires all four segments in every repetition; 8.22 orders movement before combat; 18.23-18.25 can create ensuing Movement/Combat eligibility. | Exact continuation predicates consume later Contact/ZOC, combat, and Reserve Release contracts. |
| `CYCLE-DEC-004` | **Accept** | Voluntary finish is legal after Reserve Release obligations clear even when repeat is also legal. It advances to the structural successor after the acting side's Movement and Combat Phase. | 5.2.G and 8.22 make repetition optional and controlled by the phasing player. | The later event/action names and precise successor lookup are design concerns. |
| `CYCLE-DEC-005` | **Accept** | With no legal continuation, authority closes through deterministic system finish without requesting a player/model choice. With an unavailable or timed-out controller, deterministic fallback also finishes. Neither path consumes RNG or mutates combat, CP, BP, or Reserve facts. | Repository reliability requires scripted fallback; CONTACT-001 uses explicit deterministic closure; source repetition is optional. | Timeout duration and host scheduling are not Umpire contracts and remain operational design. |
| `CYCLE-DEC-006` | **Accept** | A repeat requires at least one material cycle-progress fact. Empty position completions, waiting, rejection, fallback, and mere offer of an action do not count. This prevents unbounded no-op repeats without removing a reachable game state. | The source permits repetition to continue movement/combat; deterministic replay cannot admit an infinite sequence with no rules effect. | The exact event allowlist is frozen only after Breakdown, combat, Reaction, and Reserve Release events exist. |
| `CYCLE-DEC-007` | **Accept** | Attacked-target history is an ordered, append-only occurrence sequence scoped to the active Operation Stage. The admitted one-attacker/one-target attack records one directional occurrence; the same pair may appear repeatedly, even without movement, when current ammunition, CP, and other legality pass. | 8.25 expressly identifies a Friendly unit attacking a given Enemy unit and permits that attack more than once in an Operations Stage. | Canonical participant continuity and any multi-unit relation/expansion model belong entirely to `CMB-DES-001`. |
| `CYCLE-DEC-008` | **Accept** | History appends atomically at the irreversible attack-commitment boundary, after membership validates and before RNG or result resolution. A committed zero-effect, retreating, or fallback-resolved attack is still historical evidence; an opened opportunity, rejected submission, or first sealed envelope alone is not. | 8.25 makes repeat attacks possible and conditions them on current resources, not prior pair absence; replay still requires one exact commitment boundary against frozen state. | `CMB-DES-002` must name the exact commitment event; non-Close-Assault attack categories remain escalated below. |
| `CYCLE-DEC-009` | **Accept** | Chronicle retains every cycle open/close and attack-history append permanently. A restartable snapshot retains the open cycle record and complete current-Operation-Stage occurrence sequence for stage-local evidence and suffix equivalence; after stage closure, older entries may be omitted from future snapshots but never from Chronicle. | Snapshot is a replay checkpoint; Chronicle is authoritative permanent history; cycle research and Rule 8.25 both reason across repeated cycles in an Operations Stage. | History does not drive pair exclusion; Archives compaction format is not selected here. |
| `CYCLE-DEC-010` | **Accept** | Side-safe observation exposes an audience-safe cycle reference/ordinal and derived current legal actions, not the authoritative cycle digest, opening authority version, hidden configuration inputs, full attack history, real opposing identity, hidden occurrence count, sealed envelope, or automatic-closure reason. Player history is a redacted Chronicle projection. | Current observation projection removes hidden authority events from visible revision; Umpire/fog boundary and CONTACT-001 projection rulings require separate public identity. | Exact apparent target readback waits for combat disclosure design in `CMB-DES-005`. |
| `CYCLE-DEC-011` | **Accept** | Internal cycle/combat decisions bind authoritative identity and frozen base state. Side cycle-control freshness and membership use an opaque audience-safe binding, never raw authority version or another hidden-input digest. Fog-equivalent authorities must issue equivalent bindings/action sets and produce the same externally observable admission outcome for the same bound cycle-control action. Accepted use becomes audience-visible and consumes the binding; stale/duplicate retry emits zero events. | Current projection already derives an audience revision that omits hidden opposing Reserve events, while current raw authority-version execution shows the boundary later design must clean-cut; CONTACT-001 requires authoritative/public separation. | This freezes behavioral invariants only. `CYCLE-DES-001` owns the binding fields, codec, storage, and migration; `CMB-DES-005` must rule on any combat action whose legality can diverge on hidden facts before it is offered. |
| `CYCLE-DEC-012` | **Reject** | Do not use state version, sequence position ID, event index, combat opportunity, Contact/Engaged relation, target hex, representation enumeration order, or a duplicated catalog position as the ordinal or sole identity. Do not expose an identity derived from hidden authority revision. Do not treat prior attacker/target occurrence as a source prohibition. | Current versions increment several times at one position and can include audience-hidden events; 5.2.G repeats structural segments; 8.25 identifies units and permits the same pair repeatedly. | Later design must preserve both identity layers and repeat-permitting history even if it chooses different field names. |
| `CYCLE-DEC-013` | **Escalate** | `CMB-DES-001` must define continuity-stable attacking and target rules-unit identities, every multi-unit participation/relation and per-record identity rule, and whether later Barrage or Anti-Armor participation enters attacked-target history. | 8.25 names Friendly and Enemy units and permits repeat; 12.44 clarifies a Barrage target but does not settle multi-unit or every combat-participation category. | This packet freezes only the one-attacker/one-target selected Close Assault occurrence. |
| `CYCLE-DEC-014` | **Escalate** | `RESREL-RSH-001` must freeze mandatory release/flip obligations and release history; `CYCLE-DES-001` must compose those results with current Movement/Combat continuation. | 18.13-18.25 governs Reserve I/II release and ensuing-segment exceptions. | This packet freezes the close/repeat truth table, not the Reserve eligibility inputs that feed it. |

No owner choice remains open inside `CYCLE-RSH-001`. The two escalations are explicit input contracts
owned by later gates and do not authorize those gates or their production work.

## Exact cycle identity model

### Scope and ordinal

The semantic scope is:

```text
cycleScope = (gameTurn, operationStage, playerPhaseSlot)
playerPhaseSlot = first-acting-side | second-acting-side
cycleOrdinal = 1, 2, 3, ... within that scope
```

`actingSide` is resolved from the retained Operation-Stage order and is recorded as corroborating
identity. It does not replace `playerPhaseSlot`: the same side can occupy a different slot in a
different stage. A new game turn, Operation Stage, or acting-side slot begins a new scope at ordinal
`1`. Reserve Designation is before the scope; the successor after finish is outside it.

The first cycle opens when authority first makes that slot's Movement segment current. A repeated
cycle opens only through one accepted repeat transition. Traversing positions, reconstructing a UI,
or querying legal actions never creates a cycle.

### Authoritative identity and audience-safe reference

Later design must define a versioned internal canonical encoding with these exact semantic inputs:

```text
authoritativeCycleIdentityContractVersion
campaignId
rulesetHash
setupId
setupHash
contentPackId
contentHash
scenarioId
gameTurn
operationStage
playerPhaseSlot
actingSide
cycleOrdinal
openedAuthorityStateVersion
openingAuthorityHistoryPrefixDigest
```

The internal authoritative cycle ID is a cryptographic digest of that encoding. Canonical encoding,
field order, hash prefix, and algorithm are deferred to design; omission or reordering must fail
admission once chosen. `openedAuthorityStateVersion` is the committed authority revision at which
Movement becomes current, not a substitute for the ordinal.
`openingAuthorityHistoryPrefixDigest` binds the exact canonical authoritative Chronicle prefix that
exists immediately before the transition that opens this cycle. It excludes that transition or any
cycle-open event that first carries the derived identity, preventing recursive digest inclusion. It
distinguishes paired Exercise variants or other authority forks that share campaign ID,
configuration, ordinal, and event count after their histories diverge; replay from creation must
reproduce it, and a restartable snapshot must retain it. The remaining inputs make an internal cycle
unambiguous across campaign forks, rules/content migrations, side-order changes, and snapshot
restore. This digest and its preimage never leave the Umpire/Chronicle authority boundary.

The outward audience-safe cycle reference uses a separate versioned encoding:

```text
publicCycleReferenceContractVersion
campaignId
rulesetHash
gameTurn
operationStage
playerPhaseSlot
actingSide
cycleOrdinal
```

These are common sequence facts already authorized for both participants. The public reference
excludes authority state version, setup/Content hashes unless separately approved as public, event
count, sealed-decision count, and any other hidden input. It therefore stays byte-identical when two
authorities differ only in hidden pre-cycle events such as opposing Reserve designations. The Umpire
retains a validated internal mapping from the public reference to exactly one current authoritative
cycle. Internal events and sealed decisions use the authoritative ID.

Side-facing concurrency cannot reuse raw authority `stateVersion`, because hidden opposing events
may advance it without advancing that audience's authorized revision. Later design must provide an
opaque audience-safe action-set binding whose semantics:

- bind the campaign, audience, public cycle/window, policy, current audience revision or an
  equivalent public freshness fact, and exact current candidate membership;
- depend only on facts authorized to that audience, never raw authority version or another digest
  whose changes disclose hidden authority history;
- are equivalent for fog-equivalent authorities, including the binding bytes, action identifiers,
  candidate membership, and the externally observable admission result for the same submission; and
- make one accepted use audience-visible and consumed so its retry emits no second semantic event.

This list freezes required behavior, not a field list, canonical preimage, token format, wire
contract, or storage shape. `CYCLE-DES-001` chooses that representation and migration.

For cycle-control actions, the Umpire regenerates the side-safe action set from authorized facts,
checks the binding and exact candidate membership, then maps the admitted choice to the current
authoritative cycle. A hidden-only authority revision does not by itself stale the binding and
cannot change accept versus reject when the two authorities remain fog-equivalent. If hidden combat
legality could change that observable result, `CMB-DES-005` must first define the apparent fact or a
non-oracular interaction protocol; this packet does not freeze the combat-submission contract.

Persisted sealed windows prevent unrelated authority mutation from silently changing a frozen
pre-state while envelopes are collected. Their internal base binding remains authoritative, but any
side-visible response must satisfy the same fog-equivalent-outcome invariant rather than reveal which
hidden fact changed.

Current Campaign authority has no single generic configuration hash. Its canonical internal
configuration provenance is `setupId/setupHash` plus `contentPackId/contentHash/scenarioId`; these
values and the `rulesetHash` are therefore mandatory in authoritative identity. If Dispatch or
Exercise later carries an additional configuration hash, it must travel as internal
decision/evidence provenance, but it cannot replace or override the Umpire inputs above and cannot
enter a player-visible reference without a separate fog approval.

An open-cycle record also needs non-identity continuation facts: start position, progress evidence,
close state, and links to pending combat/Reserve obligations. Those facts can change while the
cycle identity remains fixed.

## Repeat and finish state machine

```text
Movement (cycle k)
    -> Breakdown Determination
    -> Combat steps / any persisted combat decisions
    -> Reserve Release / mandatory obligations
    -> continuation assessment
         |
         +-- obligations pending -----------------> remain in Reserve Release
         |
         +-- no legal continuation ---------------> system finish
         |
         +-- continuation + no cycle progress ----> finish only
         |
         +-- continuation + cycle progress --------> player repeat or finish
                                                       |             |
                                                       |             +-> structural successor
                                                       +-> Movement (cycle k+1)
```

The assessment is an authoritative, deterministic value calculated from the exact current state. It
has these semantic inputs:

- whether every current Reserve Release release/flip/closure obligation is resolved;
- whether at least one legal next-cycle Movement continuation exists, including any approved
  Reserve-release exception;
- whether at least one legal next-cycle Combat continuation exists under current participant,
  ammunition, CP, position, relationship, and other approved combat rules; prior attack history is
  evidence and is not by itself an exclusion;
- whether the closing cycle contains material progress; and
- exact cycle/rules/setup/Content/state provenance.

It does not infer eligibility from a player observation, Intelligence response, earlier candidate
list, current map enumeration, or favorable future random draw.

| Obligations clear | Legal Movement or Combat continuation | Material cycle progress | Controller state | Authoritative result |
| --- | --- | --- | --- | --- |
| no | any | any | any | Neither repeat nor finish; resolve the obligations first |
| yes | no | any | any | One deterministic system finish; no player/model decision |
| yes | yes | no | available | Finish only; repeat would be a no-op-loop risk |
| yes | yes | yes | available | Offer exact current repeat and finish actions |
| yes | yes | yes | unavailable/timeout | One deterministic scripted finish |

An explicit finish and a fallback finish share state consequences but have distinct authoritative
internal reasons. The acting player's explicit finish remains knowable through its own submission
and receipt. The internal no-continuation, unavailable, or timeout reason is absent from both sides'
observations and projected Chronicle unless a later disclosure decision explicitly authorizes it.

Repeat increments state version once, closes cycle `k`, opens cycle `k+1`, and points authority at
the same slot's Movement segment. Finish increments once, closes cycle `k`, and advances to the
catalog successor after Reserve Release. Neither transition consumes RNG, repeats Reserve
Designation, clears CP/BP/Cohesion, clears Reserve state/history, or clears attacked-target history.

### Material cycle progress

Progress is a monotonic authoritative fact derived from accepted semantic events in the current
cycle. Qualifying categories are:

- committed Movement/Reaction/Retreat that changes authoritative position or CP/BP state;
- a Breakdown resolution that changes check/history/working-or-broken state;
- an irreversible attack commitment, attacked-history append, or combat-result mutation; and
- a Reserve Release or Reserve I/II state/history mutation.

Pure queries, rejected/stale submissions, opened waiting states, sealed envelopes that have not
reached attack commitment, empty structural completions, and cycle finish/fallback do not qualify.
The later design must replace these categories with an exhaustive event allowlist. The check is not
a new source eligibility rule; it is a digital liveness invariant that removes only repetitions
with no authoritative rules effect.

## Authoritative attacked-target history

### Stage-scoped ordered occurrences

The semantic occurrence record is:

```text
attackHistoryOccurrence = (
  historyEntryIdentity,
  attackCommitmentIdentity,
  gameTurn,
  operationStage,
  authoritativeCycleIdentity,
  cycleOrdinal,
  attackOrOpportunityIdentity,
  attackingRulesUnitIdentity,
  targetedRulesUnitIdentity,
  committedStateVersion,
  rulesetAndConfigurationProvenance)
```

History is an ordered sequence of these occurrences, not a uniqueness set. For the admitted
one-attacker/one-target vertical, `historyEntryIdentity` is the unique entry for the exact
`attackCommitmentIdentity` and must not be derived only from the attacker/target pair. The same
directional `A -> B` pair may appear again in the same cycle or a later cycle, including when
neither unit moves, if current ammunition, CP, membership, position, and other combat rules still
permit the new attack. `B -> A` is separately directional history, but the distinction imposes no
pair exclusion in either direction.

For the selected first vertical there is exactly one Close Assault attacker and one target, so the
single history entry and its two rules-unit identities can be frozen. A target hex or opaque map
representation cannot replace that selected target identity. Multi-unit participation may not be
admitted until `CMB-DES-001` defines its relations, expansion (if any), per-entry identity, and
continuity through organization, attachment, loss, split/merge, and representation changes. This
packet chooses no Cartesian or other multi-unit model.

### Append boundary and sealed decisions

Opening a combat opportunity freezes eligibility evidence but does not itself append attacked
history. Accepting one side's sealed envelope also does not append it and cannot alter the base
eligibility presented to the other side. History appends once, atomically, when the Umpire has:

1. validated the same frozen campaign/cycle/opportunity/pre-state provenance;
2. obtained or deterministically supplied every required sealed input;
3. validated final attacker and target membership against current authority and all current
   resource/position/combat rules, without rejecting a pair merely because it appears in history;
4. crossed the point after which the attack cannot be withdrawn; and
5. emitted the attack-commitment event before any attack RNG or result event.

From that point, the occurrence remains evidence even if the effect is zero, the defender
subsequently retreats, the attack yields Contact/Engaged without loss, or later result handling uses
deterministic fallback. A stale or malformed submission rejected before commitment appends nothing.
Replaying the commitment twice is invalid because its prior state version, cycle/opportunity state,
and history-entry/commitment identities are no longer reproducible. A later source-legal commitment
of the same pair is not a duplicate: it has new commitment and history-entry identities plus current
resource validation.

Whether Barrage or Anti-Armor participation enters the same attacked-target evidence history
requires the source and participant decision in `CMB-DES-001`; later design must not silently
generalize the selected Close Assault boundary.

## Chronicle, snapshot, replay, and fog boundaries

| Fact | Chronicle authority | Restartable snapshot | Acting-side projection | Opposing-side projection |
| --- | --- | --- | --- | --- |
| Cycle identity and ordinal | Full authoritative identity/preimage, opening-history-prefix digest, public-reference mapping, open/close event, internal close reason | Current authoritative identity and opening-history-prefix digest plus audience-safe reference and close facts needed to continue | Audience-safe cycle reference/ordinal and current decision state | Same audience-safe occurrence/sequence facts; no private decision payload |
| Progress evidence | Exact qualifying events and derived progress | Current-cycle progress sufficient to validate repeat | No raw hidden evidence; only exact current legal actions | Absent unless already authorized by ordinary result projection |
| Attacked-target history | Every ordered occurrence append with commitment and provenance | Complete active-Operation-Stage occurrence sequence | No real occurrence collection/count; own prior receipt and source-authorized apparent history only | No real occurrence collection/count; only source-authorized apparent combat history/results |
| Sealed submissions | Exact trusted-Umpire envelopes and fallback evidence | Every still-pending envelope/base binding needed to continue | Own envelope receipt/status only | Own envelope receipt/status only |
| Closure reason | Exact explicit/no-continuation/unavailable/timeout reason | Current close fact needed for validation | Explicit own action remains known; automatic internal reason absent | Internal reason absent |

Chronicle replay from creation must reproduce byte-identical authoritative snapshots, internal
cycle identities, audience-safe references, current attacked history, RNG cursor, and side
projections at every event boundary. Restoring an
Archives snapshot and replaying its suffix must produce the same result. The snapshot cannot
recalculate active attacked history from mutable world state or a side-visible history.

Attacked history is not a prior-pair legality input. When the Operation Stage ends, a later snapshot
may omit its closed-stage occurrences because Chronicle retains their events and no future
continuation decision depends on the compacted copy. The snapshot's stage boundary must be explicit
and validated. A snapshot taken anywhere inside the stage retains the complete occurrence sequence
for stage-local audit and suffix equivalence even if every recorded attacker or target has moved,
retreated, changed relationship, reorganized, or been removed from the current map.

Two authorities with the same facts authorized for an audience must produce byte-identical
observations and action sets for that audience. Hidden occurrence history, target binding, sealed
choice, fallback reason, or internal opening version must not change audience-safe cycle references,
unrelated candidate IDs, public revision fields, array lengths, or diagnostic text. Exact target
candidate membership can be visible to the acting side only after `CMB-DES-005` approves the
corresponding apparent-target fact; absence carries no authoritative exclusion reason.
For the same bound cycle-control submission, fog-equivalent authorities must also produce the same
externally observable acceptance or rejection. A generic rejection label is insufficient if its
presence differs.

## Stale, duplicate, fallback, and closure invariants

- Every side cycle-control submission carries the opaque current audience-safe action-set binding
  and one exact action identity. It carries no raw authoritative version. The Umpire regenerates
  side-safe membership from authorized facts and maps an admitted choice to the authoritative cycle.
- The exact combat-submission binding is deferred. Its internal resolution must eventually bind the
  authoritative opportunity, commitment identities, and frozen pre-state/hash without creating a
  hidden-state oracle; `CMB-DES-005` owns the missing apparent-fact or interaction ruling.
- A cycle-control submission with an older or consumed binding, older cycle, wrong actor/side, wrong
  position/window, closed cycle, already-committed occurrence identity, changed candidate
  membership, or mismatched authorized provenance emits zero events and changes neither state nor
  RNG. A new same-pair opportunity/occurrence is not a duplicate.
- Retrying a previously accepted repeat/finish submission with its consumed binding is stale. It
  never returns the earlier success by mutating again. A hidden-only authority revision does not by
  itself stale an otherwise equivalent audience binding.
- Transport-level idempotent delivery may read back an already persisted receipt outside the
  Umpire, but the Umpire itself never emits a second semantic event for the duplicate.
- An empty continuation universe closes automatically; it does not create an empty player or model
  decision. An unavailable controller always finishes and never fabricates a move, target, sealed
  combat choice, Reserve release, or random result.
- Fallback policy/version is frozen in the authoritative decision context and carried in Chronicle
  provenance. Changing it changes configuration provenance; replay never consults a newer policy.
- Repeat and finish cannot close a cycle while a combat opportunity, sealed choice, retreat/loss
  decision, Breakdown obligation, or Reserve Release obligation remains pending.

## Interaction and non-predecision ledger

| Neighboring boundary | Frozen interaction | Explicit non-decision |
| --- | --- | --- |
| CONTACT/ZOC and Reaction | Cycle identity survives persisted opponent windows. Movement/Reaction state may change later continuation eligibility. Immediate enemy-ZOC entry is not Contact; Contact and Engaged identities do not define a cycle. | No Contact opportunity, participant, waiting, or resumption contract is changed here. |
| Breakdown | Every repeat traverses Breakdown Determination. Existing Operation-Stage cumulative BP, Sandstorm-attributed BP, highest checked band, CP, and Cohesion continue across ordinals. | No Breakdown roll, result, broken-vehicle, or Retreat-BP behavior is selected. |
| Mutable combat state | Loss, pin, ammunition, fuel, position, retreat, Contact, Engaged, and Disorganization may change continuation legality but cannot rewrite or erase a committed historical occurrence. Prior pair occurrence alone does not bar a new attack. | No field, loss allocation, retreat path, result, prisoner, or captured-equipment contract is selected. |
| Sealed combat decisions | Every envelope binds the same cycle/opportunity/pre-state provenance; repeat/finish waits until the opportunity closes. | No envelope type, deadline, required participant set, or fallback choice is selected. |
| `RESREL-RSH-001` | Mandatory release/Reserve-II disposition completes before close assessment. A release can create the immediately subsequent Movement exception that makes repeat meaningful. | No Reserve I/II candidate, ordering, history key, restriction, or event is selected. |
| Intelligence/Dispatch | Any advisory request carries decision ID, state version, ruleset and configuration provenance, cycle/opportunity identity, and only side-safe inputs. Timeout routes to the frozen deterministic Umpire fallback. | Intelligence never decides authority, receives hidden opposing state, or keeps the authoritative turn open. |

## Repository evidence and explicit inferences

### Documented source facts

- Rule 5.2.G defines Movement, Breakdown Determination, Combat, and Reserve Release as the four
  segments and requires every repetition to include all four.
- Rules 8.21-8.22 permit repeated Movement/Combat sequences and require combat after movement has
  ceased within each sequence.
- Rule 8.23 restricts ordinary next-segment movement by enemy proximity and identifies Reserve as
  the exception; September errata corrects the internal reference to Rule 8.23.
- Rule 8.25 permits a Friendly unit to attack a given Enemy unit more than once in an Operations
  Stage, including the case where neither has moved, provided current ammunition exists and the
  required Capability Points can be spent.
- Rules 18.13-18.14 require Reserve I release or conversion and retain Reserve II until later release
  or stage end. Rules 18.23-18.25 govern released units in ensuing Movement/Combat segments.
- Errata 12.44 confirms only that Barrage fire targets a specific battalion-level equivalent rather
  than a hex. It does not establish the selected Close Assault history model; Rule 8.25's unit-to-
  unit language supplies that evidence.

### Repository observations

- `Cna1979LandSequence` materializes one linear pass. Reusing its same structural Movement and
  Reserve Release positions cannot distinguish cycle occurrences.
- `CampaignSnapshot` contract 9 retains campaign/state/ruleset/setup/content/world/RNG/position but
  has no cycle ordinal, open-cycle state, attacked-target occurrence history, combat opportunity, or sealed
  decision.
- `CampaignWorldSnapshot` contract 4 retains elements and opaque map representations. It has no
  combat participant continuity or history collection.
- Current submissions bind campaign, exact authority state version, position, audience, and opaque
  action ID; `CampaignActionExecution` regenerates membership before dispatch. That raw-version
  seam makes accepted retries stale but can also reject after a hidden event that does not advance
  the audience projection, so later design must replace it with the frozen audience-safe
  binding invariants rather than copy it unchanged.
- Current semantic events advance state one version at a time and projector replay recomputes
  expected events. Replay preparation tests require canonical histories to reproduce identical
  snapshots and legal actions for every audience.
- Current operational Breakdown state is keyed to game turn and Operation Stage, not a Movement
  cycle. Resetting it on repeat would contradict the approved continuity decision.

### Adopted digital inferences

- A 1-based ordinal scoped to the acting-side phase is the smallest stable occurrence discriminator
  that does not duplicate source positions or overload state version.
- Binding the ordinal to campaign/rules/setup/Content/side/open-version provenance and the canonical
  opening Chronicle-prefix digest prevents valid internal bytes from being replayed in another
  authority fork or configuration. A separately derived public reference omits hidden
  version/configuration/history inputs and preserves fog equivalence.
- A material-progress gate removes only no-effect repeats and guarantees that deterministic
  controllers cannot create an infinite Chronicle with unchanged rules state.
- Recording ordered directional occurrences at irreversible commitment preserves the exact repeat
  history that Rule 8.25 allows without turning that history into a prohibition. Stable unit
  membership prevents mutable group/location names from rewriting prior evidence.
- Active-stage history must be snapshot state because recalculating it from mutable world or a
  redacted player history makes snapshot continuation incomplete.
- Automatic no-op closure and finish fallback preserve the source's optional repeat, guarantee
  liveness, consume no random result, and avoid asking Intelligence to invent authoritative play.

## Traceability into later work

The IDs below are proposed planning slices, not authorized production tasks.

| Later work | Inputs from this packet | Required output before implementation |
| --- | --- | --- |
| `CMB-DES-001` | `CYCLE-DEC-007`, `008`, `013` | Continuity-stable unit/target membership, every multi-unit participation/relation and per-record identity rule, and attack-category ruling |
| `CMB-DES-002` | `CYCLE-DEC-008`, `011` | Exact sealed opportunity/commit boundary and fallback provenance that cannot duplicate one occurrence while permitting a later valid repeated pair |
| `CMB-DES-005` | `CYCLE-DEC-009`-`011` | Redacted player Chronicle, apparent-target legality, and a non-oracular combat interaction outcome without authoritative occurrence/count or internal-ID leakage |
| `RESREL-RSH-001` | `CYCLE-DEC-003`, `004`, `014` | Mandatory release/flip closure, stage-scoped Reserve history, and ensuing-Movement exception inputs |
| `CYCLE-DES-001` | all accepted decisions | Reviewed specification/design for cycle state, identity codec, close assessment, actions/events, snapshot/replay, fog, and Exercise/Maneuver evidence |
| proposed `CYCLE-IMP-001` | `CYCLE-DEC-001`, `002`, `006`, `009`, `011` | Dormant authoritative cycle/open-progress/history values plus strict canonical validation; no public actions |
| proposed `CYCLE-IMP-002` | `CYCLE-DEC-007`, `008`, `013` after combat design | Atomic attacked-history append integrated with admitted combat commitment, repeated-pair evidence, and replay |
| proposed `CYCLE-IMP-003` | `CYCLE-DEC-003`-`005`, `010`, `014` after Reserve Release design | Repeat/finish/automatic-close actions and events with side-safe membership and deterministic fallback |
| proposed `CYCLE-IMP-004` | all accepted decisions | Exercise/Maneuver replay vectors, snapshot-suffix proof, fog equivalence, strict readback, and independent review |

Production work must remain serial across shared snapshot/event/observation identities. Research and
design may proceed independently only while they preserve the escalated ownership above.

## Acceptance vectors for design and implementation

1. **Initial identity:** entering first-side Movement opens ordinal `1`; the authoritative identity
   contains every internal input exactly once and changes when any input changes, while the public
   reference contains only its listed audience-invariant inputs. Two variants with equal campaign,
   configuration, ordinal, and state-version count but divergent Chronicle prefixes have distinct
   authoritative IDs; replay and snapshot restore retain those IDs.
2. **Relative-slot identity:** the same physical side acting first in one stage and second in another
   receives distinct scopes; replay resolves the same side/order and IDs.
3. **One repeat:** after material Movement or combat progress and a legal continuation, repeat closes
   ordinal `1`, opens ordinal `2`, returns to the same slot's Movement, and preserves BP, CP,
   Cohesion, Reserve history, attacked history, and RNG cursor.
4. **Repeated cycles:** three accepted repeats produce ordinals `1,2,3,4` with no skipped/reused ID;
   every cycle traverses Movement, Breakdown, all structural Combat steps, and Reserve Release.
5. **Voluntary finish:** with both actions legal, finish advances once to the exact structural
   successor and does not open ordinal `k+1`.
6. **Zero opportunity:** after mandatory Reserve Release closure, no legal Movement or Combat
   continuation produces one system finish, no player/model request, no RNG/CP/BP/combat mutation,
   and no leaked eligibility count/reason.
7. **No-progress repeat:** unchanged rules state with a theoretically still-offered continuation has
   finish only; forged repeat rejects with zero events and unchanged snapshot/RNG.
8. **Unavailable fallback:** controller timeout/unavailability with repeat otherwise legal emits one
   deterministic finish using the frozen policy/version; replay does not consult wall clock or a
   model.
9. **Pair reuse across cycle:** `A -> T` committed in ordinal `1` may be committed again in ordinal
   `2` when current ammunition, CP, position, membership, and other rules pass. The second attack
   appends a distinct occurrence and does not rewrite the first, even if neither unit moved.
10. **Directional history:** `A -> T`, a later repeated `A -> T`, and a source-legal `T -> A` produce
    three ordered, separately identified occurrences with their exact cycles and versions.
11. **Multi-unit boundary:** the first admitted vertical accepts only one attacker and one target.
    Any multi-unit submission is unavailable or rejects with zero events until `CMB-DES-001`
    defines participation relations, per-record identity, and atomic append semantics; this vector
    assumes no Cartesian expansion.
12. **Commit boundary:** opening an opportunity and receiving one sealed envelope add no history;
    irreversible commitment appends the occurrence once before RNG; a zero-loss or retreat result
    leaves it present, and a later valid same-pair commitment appends a new occurrence.
13. **Stale and duplicate:** consumed/older audience binding, wrong-cycle, wrong-position, wrong-side,
    closed-cycle, duplicate occurrence/commitment identity, changed candidate membership, and
    mismatched internal-hash submissions emit zero events. Replaying a commitment or repeat event
    twice is invalid history; a new valid same-pair occurrence is accepted.
14. **Snapshot suffix:** a snapshot taken in ordinal `2` retains its open-cycle/progress facts and
    every current-stage occurrence; suffix replay equals replay from creation byte-for-byte.
15. **Stage boundary:** second-side cycles in the same Operation Stage retain the ordered stage
    history; the next Operation Stage snapshot may begin with empty active history while Chronicle
    still retains prior occurrences.
16. **Fog equivalence:** hidden target binding, occurrence count, sealed choice, fallback reason,
    authority opening version, and pre-cycle hidden Reserve-designation count permutations that
    preserve authorized audience facts and candidates produce equivalent audience revisions, public
    cycle references, action-set bindings, canonical action sets, observations, projected history,
    and action IDs. The same bound cycle-control submission has the same externally observable
    admission outcome in either authority; combat waits for `CMB-DES-005` if that cannot hold.
17. **Provenance rejection:** altered ruleset, setup, Content, scenario, ordinal, acting slot/side,
    authority opening version, or opening Chronicle prefix fails internal identity/history admission
    before mutation; hidden internal changes do not alter the audience-safe reference unless an
    authorized public input changes.
18. **Neighbor continuity:** Reaction/Retreat Movement and Breakdown contributions remain in the
    Operation-Stage ledgers across repeat; cycle control neither creates Contact/Engaged nor clears
    mutable combat facts.

## Rejected alternatives

| Alternative | Reason rejected |
| --- | --- |
| Duplicate the six combat positions for a fixed number of cycles | Places an arbitrary limit in the catalog and still lacks occurrence/history authority |
| Use state version as ordinal | Several semantic events occur inside one cycle and one position; versions are concurrency provenance, not source sequence identity |
| Use `positionId + stateVersion` as the only cycle ID | Omits acting scope and rules/setup/Content provenance and overloads mutable cursor state |
| Put raw authority state version in a side action-freshness binding | A hidden opposing event can advance authority without advancing that audience's projected revision, creating a fog-dependent stale result |
| Treat a prior attacker/target pair as a one-per-stage exclusion | Directly contradicts Rule 8.25, which permits repeated attacks even if neither unit moves |
| Reset or rewrite attacked history on repeat, movement, retreat, or Contact loss | Loses replay/audit evidence and lets mutable names rewrite what previously committed |
| Key selected Close Assault history by target hex, map representation, opportunity, or group only | Rule 8.25 speaks in attacking/target units; mutable/group identities cannot accurately retain unit participation evidence. Errata 12.44 separately rejects hex targeting for Barrage only |
| Append history when an opportunity opens or the first sealed envelope arrives | Lets one private submission change shared history and records attacks that remain withdrawable |
| Append history only after a non-zero result | Makes historical truth depend on favorable dice and omits committed zero-effect attacks |
| Auto-repeat when a controller is unavailable | Can create unbounded loops and fabricates a strategic choice; finish is always source-compatible |
| Recalculate active history from Chronicle on every snapshot restore | Makes Archives snapshots non-self-sufficient and couples continuation to full prior-history availability |
| Publish the authoritative occurrence history or internal cycle ID to both players | Leaks real identities, target membership, hidden event counts/configuration, withheld participation, and internal reasoning |

## Limitations, deferrals, and reopen triggers

Confidence is high in the four-segment repeat boundary, 1-based phase-local ordinal, optional finish,
Rule 8.25's permission for repeated same-target attacks, ordered stage-scoped history, and the
current repository replay/fog requirements. Those conclusions are directly supported by Rules
5.2.G and 8.21-8.25 plus current authority patterns.

Confidence is medium in deciding which non-Close-Assault categories enter attacked-target history
because the source uses broad `attack` language while different combat steps assign participants
and targets differently. `CMB-DES-001` must resolve that question before those categories are
admitted. The
exact point of irreversible commitment must be named by `CMB-DES-002`; this packet freezes its
semantics, not an event name.

Reopen this decision if primary evidence changes Rule 8.25's repeated-attack permission; if later combat
identity cannot preserve a rules unit across organization/attachment changes; if Reserve Release
proves that a repeat may begin somewhere other than Movement; or if a source-legal state change can
occur in a repeat while none of the approved material-progress categories changes. Do not reopen it
merely because a later design uses different type or field names.

`CYCLE-RSH-001` closes with no production authorization. Its remaining choices are deliberately
owned by `CMB-DES-001`, `CMB-DES-002`, `CMB-DES-005`, `RESREL-RSH-001`, and `CYCLE-DES-001`.
