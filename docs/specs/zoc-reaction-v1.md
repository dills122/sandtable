# ZOC and Reaction v1 Specification

**Status:** Approved — `ZOR-TASK-002A` is the active first production slice

**Date:** 2026-08-30

**Roadmap capability:** `ZOC-REACTION-001`

**Rules target:** `cna-1979.1`

**Predecessor:** [Movement Foundation v1](movement-foundation-v1.md)

**Research:** [Contact, Reaction, and ZOC ruling lock](../research/contact-reaction-zoc-source-ruling-lock.md),
[Contact, Reaction, and ZOC spike](../research/contact-reaction-zoc-spike.md), and
[Sprint 4-5 research gates](../research/sprint-4-5-research-gates.md)

**Technical design:** [ZOC and Reaction v1](../design/zoc-reaction-v1.md)

## Approval boundary

This document converts the five accepted CONTACT-001 rulings into the approved production
contract. The rulings themselves are fixed. The owner approved this specification and its technical
design together after PR #80 merged, authorizing `ZOR-TASK-002A` as the first bounded slice.

Approval authorizes `ZOR-TASK-002A` as the first production slice. It does not authorize all tasks
as one change, and it does not authorize Breakdown adjudication, Contact, Engaged, Combat, losses,
ammunition, Morale, Reserve Release, or later continual-cycle behavior.

## Objective

Derive source-faithful Zones of Control from Rules, Content, Campaign state, and topology; make an
accepted phasing combat-element move adjacent to an authoritative non-phasing represented combat
element open a persisted,
opponent-owned Reaction decision; execute or deterministically close that decision through typed,
side-safe, replayable transitions; and resume the exact suspended Movement position.

## Capability boundary

ZOC and Reaction v1 includes:

- Rules-owned ZOC qualification, exclusion, topology, and Reaction predicates with citations;
- Content-owned combat/component primitives and immutable defensive Close Assault ratings;
- Campaign-owned component current TOE and Umpire-derived current raw defensive capability;
- one positive and the named negative ZOC fixtures;
- an atomic move-to-window transition and a frozen trigger-time opportunity universe;
- player-selected, multi-step participant episodes and one whole-window decline;
- exact existing CP and Operation-Stage BP accounting for Reaction movement;
- persisted empty-window, unavailable, timeout, participant-completion, close, and resume paths;
- side-safe observations, actions, redacted history, current-membership enforcement, and replay; and
- checked Exercise evidence covering order, repetition, fog, invalid submissions, and reconstruction.

It excludes:

- deriving Contact or Engaged, entering Combat, Combat assignment/result/loss resolution, or Reserve
  Release;
- Breakdown rolls, outcome-table normalization, vehicle losses, broken-location consequences, or
  RNG mutation;
- over-CPA Disorganization, reorganization, Fuel, attachments, dummy formations, Patrol, or
  reconnaissance knowledge;
- published content, the full historical unit taxonomy, or every rules exception beyond the
  admitted synthetic fixture; and
- model-backed route invention or any external I/O inside authoritative execution.

V1 also omits a phasing-player Close Assault declaration. Rule 8.53(b)'s conditional comparison to
the triggering mover's CPA is therefore inactive and deferred to the Sprint 5 Combat-cycle contract.
The accepted fog rule still governs that fact if the later declaration capability activates it.

## Normative decisions

The first five decisions are accepted in the ruling lock. Decisions 006-012 are the approved
implementation consequences accepted with this package.

| Decision | Status | Normative requirement |
| --- | --- | --- |
| `ZOR-DEC-001` | Accepted | The reacting player chooses participant order. Canonical candidate order is not execution order; one selected participant completes its episode before another starts. |
| `ZOR-DEC-002` | Accepted | One participant may take multiple steps in one episode, cannot reopen in that window after completion, and may react to a later trigger in the same stage if then eligible. |
| `ZOR-DEC-003` | Accepted | One action closes all unresolved opportunities. Player decline and scripted unavailable/timeout use distinct internal reasons but the same no-cost, no-BP, no-RNG, no-route closure behavior. |
| `ZOR-DEC-004` | Accepted | The phasing side sees generic waiting and retains its own Movement facts. The reacting side sees exact own opportunities and only apparent trigger identity, origin, and destination. |
| `ZOR-DEC-005` | Accepted | Rules own predicates and exclusions, Content owns primitives, Campaign owns current state, and the Umpire derives ZOC. Content never stores `exertsZoc`. |
| `ZOR-DEC-006` | Accepted | A triggering accepted move is one atomic semantic event whose successor includes the opened window; a separate move then open-event batch is forbidden. |
| `ZOR-DEC-007` | Accepted | One nullable snapshot-level Reaction window owns the interrupt, freezes trigger-time eligibility, stores the exact suspended Movement position, and restores it on close. |
| `ZOR-DEC-008` | Accepted | Current raw defensive Close Assault capability is derived with checked arithmetic from Campaign component current TOE and Content immutable defensive ratings; it is not persisted as a duplicate total. |
| `ZOR-DEC-009` | Accepted | Implementation performs one clean-cut Rules/Content/World/Snapshot/Creation/Observation/sequence identity migration and rejects mixed legacy/current authority. |
| `ZOR-DEC-010` | Accepted | Reaction uses an explicit reacting-side interrupt position outside normal sequence traversal; closing restores the stored position rather than calling generic `GetNext`. |
| `ZOR-DEC-011` | Accepted | V1 has no Close Assault declaration, so Rule 8.53(b)'s conditional CPA restriction is deferred rather than applied unconditionally; its accepted non-disclosure policy is preserved for the later capability. |
| `ZOR-DEC-012` | Accepted | Observation 6 exposes one canonical aggregate set of apparent enemy-controlled location IDs, without source mapping or rationale, so local Movement legality remains derivable from side-safe facts. |

## Contract requirements

### `ZOR-REQ-001` — Rules-owned ZOC vocabulary and derivation

The ruleset must own closed combat-category, qualification, Cohesion, stacking, raw defensive Close
Assault, topology, and enterability predicates with retained rule provenance. The Umpire derives
whether a represented force is a qualifying source and the unique adjacent locations it controls
from exact current authority. Multiple qualifying sources into one location remain non-additive.

Unsupported categories, edges, topology, component facts, arithmetic, or bindings reject rather
than defaulting. Base CPA, organization, mobility, or an outward apparent flag must not stand in for
combat qualification.

### `ZOR-REQ-002` — Content primitives and current capability

Admitted synthetic combat elements must carry explicit combat classification and component records
with stable component identity, maximum TOE, and immutable defensive Close Assault rating. Campaign
state owns component-keyed current TOE. Current raw defensive Close Assault capability is the
checked sum of current TOE multiplied by the compatible immutable ratings.

Each scenario initial placement must declare one provenance-bearing current-TOE seed for every
component of the placed element. The seed collection participates in Content scenario identity;
campaign creation copies it into the creation event and initial World. Maximum TOE is only an upper
bound and must never be inferred as current TOE. Missing, duplicate, unknown, negative, or over-
maximum seeds reject before campaign creation.

Content stores only the scenario's provenance-bearing initial seed, never post-creation current TOE,
current raw totals, ZOC results, Reaction eligibility, or participant state. Campaign must not
duplicate immutable ratings. This minimal combat foundation does not authorize offensive resolution
or losses.

### `ZOR-REQ-003` — Stable trigger and opportunity identities

The canonical Reaction-window identity must bind at least campaign and ruleset identity, triggering
move contract identity, trigger committed state version, authoritative triggering representation,
origin, destination, and reacting side. One opportunity identity binds the window and authoritative
reacting representation. Outward action identities additionally bind their complete current
semantics.

Canonical arrays order by ordinal stable identity. Event order records actual player choices and
must not be replaced by canonical candidate order. Real bindings remain internal.

### `ZOR-REQ-004` — Trigger, Movement-ended state, and atomic opening

Every committed phasing combat-element move whose destination is adjacent to at least one
authoritative non-phasing represented combat element creates a Reaction window. Triggering depends on
adjacency, not on positive ZOC or surviving eligibility. A move with no such adjacency creates no
window.

The triggering move increments state once and atomically projects its ordinary location, CP, BP,
representation, and ledger changes plus the opened interrupt position/window. The system must never
publish an intermediate committed-move state that requires a second event to open its mandatory
window.

If the destination lies in authoritative enemy ZOC, the same event records that the triggering
element's movement has ended for the exact current Movement Segment. That element receives no later
ordinary Movement candidate in the segment after Reaction closes. Adjacency to a represented combat
element that does not exert ZOC still opens the Reaction window but does not, by itself, end the
mover.

### `ZOR-REQ-005` — Frozen opportunity universe

Opening freezes the stable participant universe and sufficient trigger-time authority evidence to
replay it. Current candidate generation revalidates each unresolved opportunity against current
world state and may remove one that is no longer legal. Later mutation must never add an originally
ineligible participant or rewrite historical eligibility.

Every frozen participant must be an authoritative non-phasing represented combat element whose
trigger-time location is adjacent to the triggering mover's committed destination. Adjacency for one
participant cannot admit a remote otherwise-eligible participant. Frozen evidence retains that
participant's trigger-time location and exact adjacency result for replay.

Eligibility uses hidden exact authority for the admitted v1 restrictions. Outward membership
discloses only the approved own opportunity, never hidden strength, failed alternatives, bindings,
or exclusion reasons. The conditional Rule 8.53(b) CPA comparison is absent until a later approved
Close Assault declaration makes it applicable.

### `ZOR-REQ-006` — Persisted interrupt state machine

The snapshot-level window records stable identity, source version, phasing/reacting sides, exact
suspended Movement position, trigger truth, bounded apparent trigger projection, frozen opportunity
truth, resolved participants, and an optional active participant. The current structural position
identifies reacting-side authority and cannot be entered through normal sequence traversal.

Campaign element operational state records Movement-ended eligibility against an exact
Game-Turn/Operation-Stage/Movement-Segment identity rather than an unscoped permanent flag.

Every accepted Reaction step, participant completion, or close emits exactly one semantic event and
increments state once. No authoritative turn remains open while waiting on a user, worker, model,
deadline, or retry.

### `ZOR-REQ-007` — Participant episodes and later triggers

The reacting player selects any exact current unresolved opportunity by submitting its first legal
Reaction movement step. That one atomic event both chooses the participant and moves it, so every
episode contains one or more accepted steps. Only the active participant may take later steps;
explicit completion is unavailable before its first accepted step, and another participant cannot
start until completion. A resolved participant cannot reopen in the same window.

A later qualifying phasing move creates a new window and recalculates from its committed state. A
previous participant may appear again only if all then-current CP, BP, Cohesion, position, ZOC,
category, attachment, and other admitted restrictions pass.

### `ZOR-REQ-008` — Close, fallback, and empty windows

One close action resolves all remaining opportunities for that trigger and restores the exact
suspended Movement position. Player decline and scripted unavailable/timeout have distinct
authoritative close reasons but consume no CP, BP, or RNG and synthesize no route.

Player decline is available only when no participant episode is active. A trusted System-audience
unavailable/timeout close may end any open window, including an active episode after one or more
accepted steps; it atomically resolves that active opportunity and all remaining opportunities
without undoing prior step costs. Close reasons are closed reason-specific action identities, not a
caller-supplied string or player field. Unknown, player-authored, or mismatched reasons reject.

An empty frozen universe still exists as the generic window at version `N+1`. The next transition
is one deterministic system-authored close at `N+2`, with internal reason
`no-eligible-reactor`, no decision request, no cost, and no RNG. Neither player projection exposes
the empty count or internal reason.

### `ZOR-REQ-009` — Reaction movement and accounting

Reaction movement uses the same normalized Movement cost, stacking, enterability, exact CP,
Operation-Stage cumulative BP, Sandstorm attribution, rules provenance, checked arithmetic, and
atomic projector/replay paths as ordinary Movement. It adds only source-defined Reaction
restrictions, including that a reactor cannot enter enemy ZOC.

This package must replace Movement's temporary global positive-ZOC fail-closed rule with
topology-local entry/exit derivation before any positive ZOC is published. A remote positive ZOC
that neither controls the tested destination nor affects the tested edge must not suppress an
otherwise legal move. Positive entry ends that mover as specified in `ZOR-REQ-004`; source-defined
exit and Reaction restrictions apply only to the locally tested topology.

Observation 6 supplies the acting side one ordinal, duplicate-free
`apparentEnemyControlledLocationIds` set. It contains only apparent controlled location IDs, with no
mapping to a source representation, hidden mobility/category/strength, rule provenance, or failure
rationale. Public Movement entry/exit candidate derivation uses that aggregate set plus public
topology. Authority derives the set exactly; a hidden change that changes actual control changes the
observation only by the corresponding location membership.

No Reaction-local CP/BP approximation or reset is permitted. A rejected step emits zero events and
changes no world, window, ledger, or random state.

### `ZOR-REQ-010` — Current membership, stale use, and replay

Every submission binds campaign, expected state version, current interrupt position/window,
audience, opportunity/action identity, and exact current membership. Stale, duplicate,
wrong-window, wrong-side, forged, changed-route, completed-participant, closed-window, or otherwise
no-longer-current submissions emit zero events and no receipt or successor.

Creation through triggering move, steps, completion, closure, and resumed Movement must reconstruct
byte-identical snapshots and side projections. Replaying an accepted event twice is invalid because
its prior version/window transition no longer matches.

### `ZOR-REQ-011` — Side-safe observation and history

The reacting side sees a stable public window identity, current own opportunities, its current
episode state, and the trigger's apparent representation ID, origin, and destination only. The
phasing side sees generic waiting and retains its already-authorized own Movement candidate/cost and
post-move ledger facts.

Outside a Reaction window, each side-safe Observation 6 also carries the aggregate apparent enemy-
controlled location set required by `ZOR-REQ-009`. Existing apparent-presence identity, location,
and `exertsZoc` remain, but no presence-to-controlled-location mapping is exposed.

Neither side receives real bindings, enemy CPA or raw strength, conditional comparisons, failed
alternatives, eligibility count, exclusion reason, hidden path/cost details, or internal closure
reason. Chronicle retains complete authority; player history uses separate redacted projections.

For each audience, byte-identical projected facts must produce byte-identical observations and
action sets. A hidden eligibility change may alter only the reacting side's exact own opportunity
and matching candidate membership; it must not change the phasing waiting shape.

### `ZOR-REQ-012` — Clean-cut canonical compatibility

Implementation must version all authority shapes whose semantics change, use strict canonical
readers, reserve or reject legacy fields as appropriate, and reject partial legacy/current
mixtures. Preparatory task slices may add dormant successor artifacts while the complete legacy set
remains the only active identity set; one coordinated activation commit must switch the entire
coupled successor set, and Tasks 002A-006C must land through one PR/merge. The expected successors
are ruleset 8, Land sequence contract/catalog 3, Content schema 5 and canonical format 4, World 5,
Snapshot 10, Creation event 9, Observation 6, and a new side-safe policy identity. Exact versions
may change only through explicit approval before the first successor-contract checkpoint.

Candidate/action-set envelopes may retain their versions only if their serialized shapes do not
change and strict closed-kind readers explicitly admit the new kinds. New commands/events begin at
contract 1 except the triggering move, which requires a clean-cut successor of `ElementMoved`.

### `ZOR-REQ-013` — Fixture and checked evidence

The positive fixture is two independently represented combat battalion-equivalents in one permitted
hex, each stacking value 1, whose derived current raw defensive Close Assault total is at least 10.
It must control exactly the permitted adjacent locations.

Independent negative vectors must cover a lone battalion, aggregate stacking no greater than 1,
raw defense below 10, Cohesion `-26`, excluded/non-combat category, unattached HQ, All-Sea/Major
River/Lake, Escarpment, and a neighbor the source force cannot enter. Checked evidence must also
cover non-additive overlap, every state-machine path, strict readback, replay, fog equivalence, and
fresh-session re-adjudication.

## Acceptance criteria

| ID | Acceptance behavior |
| --- | --- |
| `ZOR-AC-001` | Two or more eligible reactors appear in canonical candidate order; two valid player selection orders emit their chosen deterministic orders and replay exactly. |
| `ZOR-AC-002` | One participant takes multiple legal steps in one episode and cannot begin a second episode in that window after completion. |
| `ZOR-AC-003` | The same participant may react to a later trigger in the same stage when current restrictions pass and is absent when any admitted restriction fails. |
| `ZOR-AC-004` | Close before any Reaction or after a subset resolves only the remaining opportunities, preserves CP/BP/RNG, and restores the suspended position. |
| `ZOR-AC-005` | Scripted unavailable/timeout closure is deterministic, replay-identical to its retained event, and never invents a route. |
| `ZOR-AC-006` | Adjacency to a non-ZOC-exerting represented combat element still triggers; zero eligible reactors produce the generic `N+1` window then one no-cost/no-RNG system close at `N+2`, without count or reason leakage; noncombat-only adjacency does not trigger. |
| `ZOR-AC-007` | Hidden eligible-reactor permutations/counts leave phasing bytes unchanged; a hidden v1 eligibility change alters reacting bytes only through exact own opportunity and matching action membership, without revealing the deciding hidden fact. |
| `ZOR-AC-008` | The reacting trigger projection contains only apparent ID, origin, and destination; phasing retains its own prior Movement facts; neither projection gains hidden route, cost, binding, or reason. |
| `ZOR-AC-009` | Stale, duplicate, wrong-window, wrong-side, forged-identity, changed-route, completed-participant, and closed-window submissions emit zero events. |
| `ZOR-AC-010` | The positive fixture controls exactly permitted neighbors and every qualification/topology negative fails independently; overlap remains non-additive. |
| `ZOR-AC-011` | Reaction deltas reuse exact CP/BP amounts, provenance, stage ledger, atomic projection, and replay with no parallel accounting path. |
| `ZOR-AC-012` | Authority retains bindings/rule evidence while both redacted player histories contain no hidden eligibility or fallback reason. |
| `ZOR-AC-013` | A positive ZOC affects only locally controlled destinations/edges: a remote positive apparent presence does not suppress an otherwise identical legal move. |
| `ZOR-AC-014` | Provenance-bearing scenario current-TOE seeds round-trip through strict Content identity, creation truth, World, and replay; missing, duplicate, unknown, negative, over-maximum, or maximum-as-default seeds reject. |
| `ZOR-AC-015` | Observation 6 canonically projects only the aggregate apparent enemy-controlled location set; same set/topology yields identical Movement actions despite hidden source differences, while a changed controlled location changes only its membership and dependent local candidates. |
| `ZOR-AC-016` | Every frozen opportunity is individually adjacent at trigger time; two adjacent eligible participants are admitted, while an otherwise-equivalent remote eligible participant and noncombat-only neighbor are absent. |
| `ZOR-AC-017` | Player close is absent during an active episode; reason-specific System unavailable/timeout close can resolve an active window without undoing prior CP/BP, and forged/unknown/wrong-audience reasons emit zero events. |

In addition to the ruling-lock consequences admitted by v1, `ZOR-REQ-004` requires a focused source rule
test: positive enemy-ZOC entry ends the mover for the current Movement Segment, while adjacency to a
non-ZOC-exerting representation still triggers Reaction without ending the mover solely for that
adjacency. `ZOR-AC-013` proves replacement of the temporary global fail-closed policy. The
Rule 8.53(b) threshold portion of the original consequence 7 is explicitly deferred by
`ZOR-DEC-011`; it is not claimed as v1 evidence.

## Traceability

| Requirement | Decisions | Tasks | Acceptance evidence |
| --- | --- | --- | --- |
| `ZOR-REQ-001`-`002` | `ZOR-DEC-005`, `008` | `ZOR-TASK-002A`-`003A` | `ZOR-AC-010`-`011`, `014` |
| `ZOR-REQ-003`-`005` | `ZOR-DEC-001`, `005`-`007`, `011` | `ZOR-TASK-003A`-`005` | `ZOR-AC-001`, `006`-`010`, `016` |
| `ZOR-REQ-006`-`008` | `ZOR-DEC-001`-`003`, `006`-`007`, `010` | `ZOR-TASK-003B`, `004B`, `006A`, `006C` | `ZOR-AC-001`-`006`, `009`, `017` |
| `ZOR-REQ-009` | `ZOR-DEC-002`, `005`, `012` | `ZOR-TASK-004A`-`006C` | `ZOR-AC-002`-`003`, `011`, `013`, `015` |
| `ZOR-REQ-010`-`011` | `ZOR-DEC-004`, `006`-`007`, `012` | `ZOR-TASK-004A`-`006C` | `ZOR-AC-005`-`009`, `012`, `015`, `017` |
| `ZOR-REQ-012` | `ZOR-DEC-006`, `009`-`010`, `012` | `ZOR-TASK-002A`-`006C`, `007A`-`007B` | dormant/current/activation compatibility and replay matrices across every new or activated command, event, action-kind, authority, and observation contract |
| `ZOR-REQ-013` | `ZOR-DEC-005`, `008` | `ZOR-TASK-002C`, `007A`-`007B` | `ZOR-AC-010` plus checked bundle/readback evidence |

The technical design defines each lettered task's dependency and proof obligation without changing
the stable parent IDs retained by the ruling lock and roadmap.

## Owner approval record

The owner approved the complete package after PR #80 merged, including these consequences:

1. derive current raw defensive Close Assault capability from component current TOE and immutable
   Content ratings rather than persisting a duplicate total, with provenance-bearing current TOE
   declared per component by each scenario initial placement;
2. use one snapshot-level Reaction window and explicit reacting-side interrupt position;
3. represent a triggering move and opened window in one atomic `ElementMoved` successor event;
4. perform the clean-cut identity migration described by `ZOR-REQ-012`; and
5. execute the dependency-ordered task slices in the technical design while Breakdown adjudication
   remains a separately owned research/design lane; and
6. defer Rule 8.53(b)'s CPA comparison until a later approved Close Assault declaration exists,
   rather than importing Combat declaration state or applying the restriction unconditionally; and
7. add an aggregate source-unmapped apparent enemy-controlled-location set to Observation 6 as the
   minimum disclosure that keeps topology-local Movement actions side-safe and deterministic.

That approval opens only the dependency-ordered first slice, `ZOR-TASK-002A`. Later slices remain
dependency-gated and must not be treated as one authorized or implemented change.
