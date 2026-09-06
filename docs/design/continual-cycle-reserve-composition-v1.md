# Continual Cycle and Reserve Release Composition

**Status:** `CYCLE-DES-001` bounded design complete for review; production remains gated.
**Date:** 2026-09-06. **Input checkpoint:** `2e7fbe0`.

This packet composes the [accepted cycle semantics](../research/continual-cycle-identity-and-history-decision.md),
[proposed Reserve rulings](../research/reserve-release-history-spike.md),
[Combat steps](combat-step-transitions-v1.md) and
[settlement/disclosure](combat-settlement-disclosure-v1.md). It defines cycle identities, release
records, continuation and retained evidence. It completes the bounded design series, not policy
approval, a production schema, a playable repeat loop or simulator adoption.

## Source and current boundary

| Evidence | Consequence |
| --- | --- |
| [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF p12, 5.2.G-H | Every repeat traverses Movement, Breakdown, Combat and Reserve Release. Finish advances to the same acting side's Truck Convoy Movement. |
| Land rules, PDF p14, 8.23-8.25; [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), 8.23 | Continued movement depends on prior Movement-end proximity, with the Reserve exception. Repeated attacks on the same unit are possible subject to current legality. |
| Land rules, PDF p28, 18.13-18.26 | First friendly release resolves I; later II may release. Ceilings, one offensive use and next-Movement exception survive the status change; release costs no CP. |
| `CYCLE-DEC-001`-`014` | Ordinal, authority/public separation, material progress, no-continuation finish and stage history are accepted semantic inputs. |
| `RESREL-DEC-001`-`004` | Friendly-relative first release, fallback, cumulative ceilings and exception scope remain proposed policies requiring owner approval. |

Land pages12/14/28 and errata8.23 were checked. Digital event boundaries and codecs below are
proposals. Current [sequence V4](../../src/Cna.Core/Rules/Cna1979LandSequenceV4.cs) stops supported
checkpoints at first-side stage1 Combat entry; its catalog includes later positions without their
execution authority. [Reserve completion](../../src/Cna.Core/Campaigns/CampaignV11Preamble.cs)
currently advances once into Movement. [Movement](../../src/Cna.Core/Campaigns/CampaignElementMovedV3Factory.cs)
requires status None and implements neither release history nor subsequent-cycle proximity.
Existing enum values do not imply a complete Reserve lifecycle.

## Composition decisions

| ID | Decision |
| --- | --- |
| `CYCLE-COMP-001` | Open ordinal1 atomically with the transition entering a friendly Movement/Combat phase; repeat closes k and opens k+1 in one transition. Resolve side from retained relative-slot order. |
| `CYCLE-COMP-002` | Use separate domain-separated authority, public occurrence and audience action bindings. Prefix evidence excludes the opening event, avoiding recursive identity. |
| `CYCLE-COMP-003` | Reserve Release has an explicit bounded decision window. First I must release/convert; later II may release/retain. One segment deadline cannot renew per unit. |
| `CYCLE-COMP-004` | Retain release provenance, cumulative voluntary ceiling, offensive-use link and one-segment movement exception independently of current Reserve status. |
| `CYCLE-COMP-005` | Repeat requires closed immediate obligations, material progress and a source-legal, supported continuation witness. Finish uses the exact Truck Convoy successor; missing implementation is not no legal continuation. |
| `CYCLE-COMP-006` | Derive progress from a closed semantic event allowlist and preserve Movement-end eligibility evidence, resources, stage history and future obligations. Never infer progress from event count. |
| `CYCLE-COMP-007` | Snapshot/replay validates complete active-stage history and current-cycle receipts; phase finish is not stage end. Unsupported future obligation boundaries halt without claiming completion. |
| `CYCLE-COMP-008` | Exercise/Maneuver proof must show actual shared-Core transitions, terminal scope and honest pairing. Existing research checks and a single paid assault do not prove a repeating campaign. |

## Cycle identity and opening

Scope is `(gameTurn, operationStage, playerPhaseSlot)`; slot is first-acting-side or second-acting-side.
Ordinal is positive and starts at1 per scope. Actual side corroborates retained stage order; it
cannot replace relative slot. The later version of `ReserveDesignationCompleted` must atomically
create the first open-cycle record while entering Movement. Do not add a second cycle-open event
that increments authority at the same checkpoint. Historical versions keep their existing bytes.

`MovementCombatCycleRepeated` closes k, increments authority once, opens k+1 and returns to the
exact same slot/stage/turn Movement catalog entry with its resolved actor. No new designation occurs.
The new opening version is that event's result version; its opening prefix is the Chronicle prefix
immediately before it. Closed-cycle receipt and new-cycle record persist together. A failed publish
leaves k open; a lost reply reads back the accepted receipt without a second repeat.

Proposed identity codec1 is a small binary tuple, separate from production event JSON. `U32`/`U64`
are unsigned big-endian; `S` is U32 byte length followed by exact UTF-8; `H` is raw32-byte SHA-256.
Strings retain admitted spelling with no normalization or case folding. Numeric range/length limits
are checked before encoding; no truncation, locale text or implicit null is accepted. Digests display
as `sha256:` plus lowercase hex. Versioned schema freeze must supply golden byte vectors and input
limits before allocating any production contract version.

`D(domain, payload) = SHA256(ASCII(domain) || 0x00 || payload)`. Fixed domains and tuple order:

| Digest | Payload in exact order |
| --- | --- |
| Authority cycle, domain `sandtable.cycle.authority.v1` | U32(1); S(campaignId); S(rulesetHash); S(setupId); S(setupHash); S(contentPackId); S(contentHash); S(scenarioId); U64(gameTurn); U64(operationStage); S(playerPhaseSlot); S(actingSide); U64(ordinal); U64(openedAuthorityVersion); H(openingPrefix); H(admittedPolicyBundleDigest). |
| Public cycle, domain `sandtable.cycle.public.v1` | U32(1); S(campaignId); S(rulesetHash); U64(gameTurn); U64(operationStage); S(playerPhaseSlot); S(actingSide); U64(ordinal). |
| Audience set, domain `sandtable.cycle.actions.v1` | U32(1); S(campaignId); S(audience); H(publicCycle); S(windowKind); U64(audienceRevision); S(approvedPublicPolicyId); U32(candidateCount); each S(canonicalCandidateBytes). |
| Action, domain `sandtable.cycle.action.v1` | U32(1); H(audienceSet); U32(zeroBasedCandidateIndex). |

Candidate bytes are UTF-8 canonical JSON under a fixed per-window writer, containing a closed
choice tag and authorized own-unit/reference payload only; exclude derived action IDs to avoid
cycles. Sort candidate bytes lexicographically as unsigned bytes, reject duplicates, then assign
indices. Window kinds are `reserve-release` and `cycle-control`. Fixed JSON property/escape rules
and bounds are combined-schema obligations; this codec proposal does not silently reuse arbitrary
serializer output. Public policy ID denotes an approved shared policy, not its private config hash.
All authority provenance remains internal; public occurrence fields are already authorized sequence
facts. Authentication and exact candidate membership are still required: a digest is not permission.

Chronicle prefix uses the existing canonical creation/event bytes, including their own versions.
Let P0 be `D(sandtable.cycle.prefix.creation.v1, U64(creationByteLength) || creationBytes)`.
For each canonical event E, Pnext is `D(sandtable.cycle.prefix.event.v1, Pprior || U64(length(E)) || E)`.
No JSONL LF framing or diagnostic file enters this chain. Canonical readers must reject alternate
encodings before hashing. Derive a cycle from Pprior, then hash its opening event to obtain Pnext;
never include Pnext in the identity it would define. Retain exact bytes/provenance for replay from
creation. A checkpoint digest alone is not authentication for arbitrary imported snapshots.

Umpire maps public cycle/set/action to the current authority cycle. Hidden-only events cannot stale
an unchanged audience set or alter observable success. After an accepted own release or cycle
choice, its authorized revision changes and consumes that set. Rejected/duplicate calls emit no
semantic event; own receipt lookup recovers prior acceptance. Prefixes distinguish paired authority
forks with equal campaign ID/config/event count; public references stay equal until authorized facts
differ. Overflow is a checked operational/contract fault, never a fabricated finish reason.

## Reserve Release window

After DES-003's exact CA->Reserve Release receipt, `ReserveReleaseOpened` records cycle/slot,
completed Combat proof, own-unit membership/status/history and a single pinned finite decision
budget. Require no open Reaction, Breakdown route/stop, sealed round or immediate settlement work.
The window exists even when empty, but an empty membership creates no player decision/deadline.
No releases are inferred merely from reaching the structural segment.

| Own status / occurrence | Legal resolution |
| --- | --- |
| I, first friendly release (ordinal1) | `release-I` to None or `convert-to-II`; retaining I is forbidden. |
| II, later friendly release | `release-II` to None or `retain-II`; segment completion may retain all remaining II. |
| None | No per-unit work. |
| II at fresh first release, or unresolved I later | Invalid history under proposed RESREL-DEC-001; no silent normalization. |

Use canonical own-unit order and one current unit at a time, not a power set of release subsets.
`ReserveUnitDispositionRecorded` consumes that unit's binding and retains exact before/after status,
release/retention cause and stage restriction evidence. Conversion/retention cannot be reconsidered
in the same segment; newly converted II cannot release until a later cycle. A later explicit retain
records no rules change and no material progress. No re-designation or repeat release restores rights.

`ReserveReleaseCompleted` is one system receipt at the same structural position. It validates every
mandatory I disposition and the terminal optional-II intent; later explicit completion retains any
unprocessed II without per-unit empty events. An empty segment completes with a proved empty set.
Only afterward may cycle control decide repeat/finish. First-release completion cannot bypass I.
In a later segment, typed `complete-release` is offered alongside the current own-unit choices;
its authenticated intent is consumed directly by the completion event. Once no per-unit work
remains, only system completion is available. This is not a generic structural advance command.

Apply DES-002 trusted-time/receipt rules to the single window deadline. Timeout/unavailability or
clock confidence loss locks fallback: convert each unresolved I toII in canonical own-unit order,
retain II later, then complete. Persist fallback mode in the first fallback disposition (or
completion if no I remains); no late player choice can interleave with its remaining work. Accepted
prior choices remain. Recovery does not renew budget per unit or wait for a new model request.
The fallback is a proposed policy; it never auto-releases or auto-repeats.

## Release restrictions and continuation assessment

A release record retains original rules-unit key, stage/slot, released type, release cycle/receipt,
CPA basis and voluntary ceiling, offensive-commitment link, and the next-Movement exception scope.
For unchanged CPA C, I ceiling is C; II is floor(C/2). Test cumulative `spent + newCost <= ceiling`,
never grant a fresh budget. Already over-ceiling mandatory expenditure is valid history; it blocks
further voluntary spend without refund. Preserve Cohesion/TOE/ammo/BP and checked Breakdown bands.

A released unit gets at most one offensive Close Assault (including Probe) in the stage. The
irreversible offensive commitment consumes it once; defensive participation, empty sealing,
cancellation or release itself does not. Link to Combat history rather than maintaining a divergent
counter. II voluntary combat adds its source DP before Morale; do not force Cohesion back to0.
DES-001/004/005's combat profile excludes these Reserve-history participants; admitting their attack
requires a reviewed extension, including II Morale and broader participation incidence. Movement-only
release exercises do not prove released-unit combat.

At each Movement completion, retain own-unit end locations, source8.23 proximity/eligibility proof
and any earlier phase-local exclusion. Ordinary continuation uses that Movement-end evidence,
not a fresh proximity test after Combat retreats. A unit that lost ordinary continuation cannot
regain it merely because an enemy moved closer later. Apply the approved release exception only
through the immediately next cycle's Movement occurrence, for its whole segment. Expire it on that
Movement completion or on phase finish without repeat; replay in another slot/cycle cannot revive it.
The exception waives only proximity, not CP, terrain, stacking, relations or enemy-control rules.
Reserve I's separate pre-release one-hex move is outside this exercise profile; II still cannot move.

At Reserve Release completion derive three closed assessment values:

1. immediate obligations clear, with all causal receipts and future obligations retained;
2. material progress in this cycle from the allowlist below;
3. at least one source-legal **and supported** next Movement or Combat continuation witness.

Movement witness requires at least one actual legal move from next-cycle entry after resetting only
segment-local route/stop bookkeeping and applying the release/proximity rules. Combat witness
requires that empty Movement/Breakdown and presteps can legally lead to an admitted opportunity,
with current resources, participant/relationship and full-result support. It does not assert a
future player will select/decline/seal, and consumes no RNG or choices. Both assessments use Umpire
facts while projections obey DES-005's equal-visibility requirements. Unsupported or ambiguous
legality is a capability failure, not a false value meaning no source-legal action exists. A public
profile must certify the required decision surface before execution; it cannot hide missing rules
behind automatic finish.

| Immediate obligations | Legal supported continuation | Material progress | Outcome |
| --- | --- | --- | --- |
| Pending | Any | Any | Neither repeat nor finish; resolve live obligation first. |
| Clear | None (proved) | Any | System `MovementCombatPhaseFinished`; no controller wait. |
| Clear | Present | No | Finish only; system executes singleton finish without a wait. |
| Clear | Present | Yes | Offer repeat or finish; unavailable/expired controller deterministically finishes. |

`MovementCombatControlOpened` opens one pinned decision context only for the last row; its receipt retains
assessment and deadline at Reserve Release. Accepted repeat/finish consumes the context; duplicate
and stale timers cannot close the new cycle. Fallback uses DES-002 time/clock rules, no fresh budget.
`MovementCombatPhaseFinished` closes k and moves once to the same slot's exact Truck Convoy Movement
catalog successor, without RNG or a second release completion. Finish does not start the opponent,
reset stage state or claim the Truck Convoy phase is implemented.

## Material progress, resets and history

Progress is monotonic evidence derived from valid events in the current cycle, not a caller boolean.
For this composed surface the allowlist is closed:

| Event | Qualifying rule effect |
| --- | --- |
| `ElementMovedV3`, `ReactingElementMovedV2` | Actual position or CP/BP change; only under their separately certified capability. |
| `BreakdownStopResolved`, `BreakdownSegmentCompleted` | A nonempty validated check batch or changed checked-band/working/broken state; empty completion does not count. |
| `CombatAttackCommitted`, `CombatAssaultResolved`, `CombatLossesSettled` | Irreversible attack/use/result/loss evidence; commitment counts even when final losses are zero. |
| `CombatRetreatSettled`, `CombatCustodySettled`, `CombatRelationshipsSettled` | Actual movement/resource/custody/entitlement/relation change; validated no-change receipt does not count. |
| `ReserveUnitDispositionRecorded` | I->None, I->II or II->None with actual status/history mutation. Retain-II is not progress. |

Queries, rejected calls, private seals, selection/decline, decision openings, cancellation, ordinary
empty step/segment receipts, cycle open/close and finish causes never count. Each listed event must
also pass its own legality and once-only identity; merely repeating a record cannot create progress.
Additional event kinds need explicit allowlist and capability review.

Repeat resets cycle progress, disposed selection/opportunity context, target-hex use for the new
Combat Segment, and completed segment-local movement/reaction controls. It preserves positions,
CP/Cohesion, current TOE/ammo/readiness, BP/Sandstorm-attributed BP/highest checked bands/broken lots,
original Contact/Engaged, full active-stage attack history, release ceilings/usage and future
custody/feeding/replacement obligations. No unresolved control is reset. DES-001's once-per-segment
hex use and stage-long repeated-unit history are distinct: a fresh cycle clears the former only.
One-use ammo or lost strength usually excludes a second DES-004 fixture assault; never synthesize
resupply, repair or a favorable column to make a repeat demonstration succeed.

Phase finish expires its unused next-Movement exception and closes cycle controls, but preserves
stage restrictions and history. Stage end needs its own explicit supported transition to clear
ReserveII and stage-only permissions/history copies, expire Engaged and perform source housekeeping;
Cohesion and Chronicle persist. This design does not introduce that transition. Snapshot compaction
may omit closed-stage attack entries only after that validated boundary, never inside a live stage.
Guarded custody and delayed replacement entitlements survive both phase and stage changes until
their own legal discharge; a resource cap or runner step limit is not a discharge.

## Exercise and Maneuver evidence

Extend [Exercise](exercise-harness-v1.md) only after shared Core contracts exist. Retain ordinary/
Exercise parity, one accepted semantic event per step, Core-owned history reconstruction and second-
session re-adjudication. Controllers select approved action IDs; they do not implement cycle truth.
New cycles need an occurrence-aware terminal: position ID alone can match both first entry and repeat.
Terminal evidence must bind expected scope/ordinal, structural position, closure status and required
obligation state. Historical manifests/readers keep their old meaning under explicit versions.

Required future evidence suites:

| Suite | Evidence required / claim boundary |
| --- | --- |
| Empty/no-progress cycle | Traverse four segments, mandatory releases clear, one proved finish; no fake attack or repeat. |
| Movement/release repeat | First I release or conversion, later II release/retention, exact next-Movement exception expiry, legal material move and all four segments of each repeated occurrence. Pre-release I movement remains excluded. |
| Combat then finish | Real decline/two seals/commit/result; all selected loss/retreat/capture branches; immediate settlement closes and future obligations persist at Truck Convoy entry. This is a bounded phase-entry terminal, not whole-game success. |
| History and recovery | Same-pair repeat permission tested with genuinely certified resupply/strength extension; before that, unit-history semantic tests plus rejection of exhausted fixture, without claiming two runtime assaults. Cuts at every release/control/settlement boundary. |
| Paired/privacy runs | Equal creation/seed/initial bytes; explicit first divergence. Equal counts with different private prefixes produce different internal cycle IDs but equal authorized public refs. Never claim synchronized random purposes after divergence. |

Guard/prisoner actions become relevant at Truck Convoy Movement. A guarded or replacement-bearing
terminal retains them and stops before unsupported actions/upkeep/calendar maturity; it cannot be
labelled a complete playable cycle campaign. New capability flags, strict evidence variants and
bounds must reject unsupported requested terminals at admission, not run until a missing action is
mislabelled success. Step-limit/cancellation/invariant failures remain failures even in negative
Maneuver assertions. Every child bundle and parent report must pass strict readback; raw trusted
bundles remain authoritative/private, not side-safe exports. Two clean runs compare the existing
simulation-evidence subset, with diagnostics outside equality and honest build identity.

## Acceptance and combined-freeze gate

| Case | Required observation | Decisions |
| --- | --- | --- |
| `CYCLE-COMP-AC-001` | First entry opens1; repeat opensk+1 once in same slot and finish targets that slot's Truck Convoy phase. Other stages/slots start1 only through their own supported entry. | 001/005 |
| `CYCLE-COMP-AC-002` | Prefix/order/length/domain tampering changes internal ID or rejects; opening event never hashes itself. Equal hidden forks preserve public refs/action admission. | 002 |
| `CYCLE-COMP-AC-003` | First I resolves release/convert; convertedII cannot release in same segment. LaterII release/retain and empty completion work; invalid first/later statuses reject history. | 003 |
| `CYCLE-COMP-AC-004` | Timeout midway through first release preserves accepted choices, converts remainingI deterministically and completes; restart/late replies cannot renew deadline or auto-release/repeat. | 003 |
| `CYCLE-COMP-AC-005` | CPA9/II/spent3 gives voluntary ceiling4, not fresh4. Mandatory overspend persists. Offensive commitment consumes one allowance, defense does not; II DP precedes Morale and requires broader combat admission. | 004 |
| `CYCLE-COMP-AC-006` | Next-Movement exception spans exactly one occurrence and expires even unused. Prior Movement-end proximity survives combat movement; finish and other scopes cannot revive rights. | 004/006 |
| `CYCLE-COMP-AC-007` | Exercise each continuation truth-table row. Missing capability/hidden-dependent legality cannot masquerade as proved no-continuation; accepted fallback finish emits no RNG. | 005 |
| `CYCLE-COMP-AC-008` | Only allowlisted actual effects set progress; empty Breakdown/retention/private seals/cancelled selections do not. No-progress repeats reject and cannot synthesize a new release opportunity. | 006 |
| `CYCLE-COMP-AC-009` | Repeat resets target-hex use and closed segment controls, preserving stage attack/release/resource/BP/relation/future-obligation facts. Zero ammo never replenishes. | 004/006-007 |
| `CYCLE-COMP-AC-010` | Every checkpoint/suffix reproduces full active-stage history, identity, pending owner and side actions. Omitted history, reopened control, duplicate repeat or premature stage reset fails strict readback. | 002/007 |
| `CYCLE-COMP-AC-011` | Occurrence-aware terminal distinguishes Movement1 fromMovement2; child and Maneuver proofs preserve authority parity, replay and first-divergence limits. Trusted bundles remain private. | 008 |
| `CYCLE-COMP-AC-012` | Truck Convoy entry retains guard/feeding/replacement work; unsupported terminal, step-limit or maturity crossing is not game success. All pending policy/schema/implementation gates remain visible. | 007-008 |

Before production work, reconcile one versioned specification from DES-001..005 and this packet:
approved Reserve/custody/fallback/disclosure policies; complete normalized tables; exact command/
event/snapshot/observation/Dispatch schemas and migration matrix; canonical codec goldens and size/
numeric limits; repeat-aware Movement/Breakdown and guard/CP/entitlement authority; current and
future obligation admission; occurrence-aware Exercise terminals; and hosted recovery/privacy tests.
Then cut implementation-sized tasks and obtain the separately authorized combined-plan review.
The earlier independent-review budget is exhausted at3of3 and does not cover this subsequent packet.
No new independent instance or production approval is implied by design completion. Old readers
must reject new cycle state; a snapshot without cycle evidence cannot be upgraded by guessing its
ordinal from position/version. Any activation migration needs authenticated history and an explicit
validated mapping, while old artifacts and replay readers remain available.

**Checks (2026-09-06):** Reserve research verifier passed7 release cases/8 CP vectors/7 scope
vectors/4 offensive-use cases. Combat research verifiers passed12 RNG vectors and8 loss boundaries/
108 combinations/3 guard probes. Seven temporary tuple/prefix probes checked fork, framing,
ordering, domain and pre/post-opening distinctions; these are design probes, not production codec
fixtures. Local checks resolved187 links across five task documents and validated8 ordered decisions/
12 acceptance IDs. Source/semantic self-checks and `git diff --check` passed. No .NET build/tests or
additional independent review ran for this documentation task.
