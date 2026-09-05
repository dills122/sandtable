# ZOC and Reaction v1 Technical Design

**Status:** Approved — `ZOR-TASK-002A`-`006C` implemented; `007A` next

**Date:** 2026-08-30

**Capability:** `ZOC-REACTION-001`

**Specification:** [ZOC and Reaction v1](../specs/zoc-reaction-v1.md)

**Research:** [Contact, Reaction, and ZOC ruling lock](../research/contact-reaction-zoc-source-ruling-lock.md)
and [Combat static-schema spike](../research/combat-content-static-schema-spike.md)

## Design summary

ZOC/Reaction is a persisted Umpire interrupt, not a blocking callback inside Movement and not a
new service. The triggering move atomically enters an opponent-owned Reaction position. Exact
authority freezes the opportunity universe, side-safe projections publish only current approved
facts, and each submitted step, participant completion, or close becomes one replayable event.

```text
Rules predicates + Content components + Campaign current TOE + topology
                              |
                              v
                 Umpire derives current ZOC
                              |
phasing move -> atomic moved/window-open event -> Reaction interrupt position
                                                  |
                         +------------------------+-----------------------+
                         |                        |                       |
                  choose participant       system/player close     generic waiting
                         |                        |                 for phasing side
                         v                        v
                  Reaction step(s) ------> exact suspended Movement position
                         |
                  complete participant
                         |
                  next participant or close
```

No authoritative transition waits on a human, worker, model, network call, timeout, or retry.

## Architecture ownership

### Rules

`src/Cna.Core/Rules` owns closed combat categories, ZOC qualification/exclusion predicates,
component-rating arithmetic, topology projection/enterability rules, Reaction restrictions, and
source provenance. Pure Rules functions accept explicit facts and return typed qualified,
not-qualified, or unsupported results.

### Content

`src/Cna.Core/Content` owns explicit combat classification and immutable component definitions:
component ID, maximum TOE, and defensive Close Assault rating. Each scenario placement also owns
provenance-bearing initial current-TOE seeds keyed by component. These facts participate in strict
Content identity. Content owns neither post-creation current TOE nor a ZOC boolean.

### Campaign authority

`src/Cna.Core/Campaigns` owns component-keyed current TOE, representations/bindings, current world,
the nullable Reaction window, commands/events, exact suspended/current positions, projection, and
replay. The Umpire derives current raw defense and ZOC from admitted Rules/Content/Campaign truth.

### Observation and actions

`src/Cna.Core/Observations` publishes audience-specific Reaction state. `src/Cna.Core/Actions`
derives exact current candidates from one admitted observation and re-queries current membership on
submission. Authority may calculate the reacting side's own frozen eligibility before projection;
candidate generation must not reach around that projection for additional hidden facts.

Observation 6 additionally projects one aggregate ordinal set of apparent enemy-controlled
location IDs. It deliberately does not map controlled locations back to apparent presences. This is
the minimum side-safe input that lets ordinary Movement actions derive local ZOC entry/exit without
hidden source mobility.

### Exercise Harness

`src/Cna.ExerciseRunner` adds bounded deterministic policies only after Core publication. It chooses
from exact current candidates and uses the ordinary submission path. It owns no ZOC, eligibility,
movement, close, fallback, ledger, or replay rule.

## Contract and identity plan

The expected clean-cut migration is:

| Contract | Current | Proposed successor | Reason |
| --- | ---: | ---: | --- |
| ruleset | 7 | 8 | combat/ZOC vocabulary and predicates |
| Land sequence contract/catalog | 2 | 3 | reacting-side interrupt identity |
| Content schema/canonical format | 4 / 3 | 5 / 4 | combat components and defensive ratings |
| Campaign World | 4 | 5 | component current TOE and scoped Movement-ended state |
| Campaign Snapshot | 9 | 10 | nullable Reaction window/current interrupt position |
| Campaign creation event | 8 | 9 | replay-complete seeded current TOE |
| Campaign Observation | 5 | 6 | bounded Reaction projection and new policy |
| `ElementMoved` | 1 | 2 | atomic optional opened-window result |

The dormant observation policy is `sandtable.observation.zoc-reaction-side-safe.v1`. Exact hashes
and final tokens are implementation goldens. A parent task may revise a number before its first
merge only by updating the spec, design, compatibility tests, and all dependent branches together.
No implementation accepts a partial legacy/current mixture.

Legal-action-set, submission, receipt, and candidate envelopes retain their versions only if their
serialized field shapes remain byte-for-byte unchanged. Their strict readers must explicitly admit
new closed kinds. New Reaction command/event types start at contract 1.

### Static and current combat foundation

The minimum new shapes are conceptually:

```text
ContentCombatComponent
  componentId
  maximumToe
  defensiveCloseAssaultRating

ContentInitialComponentToe
  componentId
  currentToe
  origin

CampaignComponentState
  componentId
  currentToe

CampaignElementMovementState
  movementSegmentIdentity
  movementEnded
```

Content scenario admission requires exactly one seed for every component of each initially placed
element and rejects missing, duplicate, unknown, negative, or over-maximum values. Maximum TOE is
never a default. Campaign creation copies the canonical ordered seed collection and provenance into
creation truth, then World owns the mutable values. Replay recreates the same handoff from the
retained compatible Content identity and event bytes.

`ZOR-TASK-002B` freezes this as the direct-only `ContentPackV5Definition` envelope with schema 5,
format `sandtable.content-json.v4`, and mandatory capability `land.combat-components`. Each element
extension carries `combatClassificationId`, `combatOrigin`, and canonical `components` with
`componentId`, `componentClassId`, `maximumToe`, `defensiveCloseAssaultRating`, and `origin`. Each
initial placement carries canonical `initialComponentToes` rows with `componentId`, `currentToe`,
and `origin`. The v5 serializer/artifact emits and hashes one complete successor document derived
from a fully validated schema-4 definition; the active schema-4 reader continues to reject those
bytes. `ZOR-TASK-002C` adds strict direct-only v5 readback by separating successor fields from one
complete document, delegating inherited schema validation to the existing schema-4 reader, then
revalidating the reconstructed v5 definition and byte-comparing the original v5 input with canonical
v5 reserialization. Equivalent whitespace, escaping, property order, or collection order therefore
rejects as `content.noncanonical-json` without tightening the intentionally normalizing legacy
reader. Its checked positive golden round-trips canonical
bytes and identity, derives stacking `2` and current raw defense `10`, and retains every named
independent negative plus non-additive controlled-location evidence.

Admission proves component identity uniqueness, `0 <= currentToe <= maximumToe`, compatible
Content/rules identity, and checked arithmetic. Current raw defensive capability is derived as the
checked sum of each `currentToe * defensiveCloseAssaultRating`. No standalone current total is
serialized. Offensive ratings, Combat assignment, losses, ammunition, and Morale are absent.

The Movement-ended marker is scoped to an exact Game Turn, Operation Stage, and Movement Segment
identity. It is not a permanent unit flag and is not Contact/Engaged state. Positive enemy-ZOC entry
sets it in the atomic triggering move; non-ZOC adjacency alone does not.

### Reaction position and window

`LandActorRole.ReactingSide` is a closed successor role. A deterministic interrupt position binds
the suspended Movement position and reacting side. It is not emitted by `CreateTurn` or `GetNext`.

The nullable snapshot-level window conceptually contains:

```text
reactionWindowId
triggerCommittedStateVersion
phasingSide
reactingSide
suspendedMovementPosition
triggerAuthority              // internal binding, origin, destination, move identity
apparentTrigger               // apparent ID, origin, destination
frozenOpportunities[]         // stable authority identity + replay evidence; internal
resolvedOpportunityIds[]
activeOpportunityId?
```

Arrays use authority-identity order. The active participant is null before selection and after its
completion. Closure removes the window and restores `suspendedMovementPosition`; it never advances
the sequence.

Each frozen opportunity retains the reacting representation's trigger-time location and proof that
it was individually adjacent to the committed destination. The trigger's existence cannot make a
remote participant eligible.

### Canonical identity preimages

```text
reactionWindowId = SHA-256(
  domain, campaignId, rulesetIdentity, triggerMoveContract,
  triggerCommittedStateVersion, triggerRepresentationId,
  originLocationId, destinationLocationId, reactingSide)

reactionOpportunityId = SHA-256(
  domain, reactionWindowId, reactingRepresentationId)
```

Action IDs continue to hash their complete typed side-safe semantics. IDs never depend on array
index, current enumeration order, human text, or hidden data that is not part of the documented
authority identity.

## Transition model

| Transition | Actor | One-event result |
| --- | --- | --- |
| non-triggering `MoveElement` | phasing side | ordinary Movement successor, no window |
| triggering `MoveElement` | phasing side | moved element/ledger plus opened window and Reaction position |
| first Reaction step | reacting side | participant selected and atomically moved; episode becomes active |
| later Reaction step | active participant's reacting side | atomic location, representation, CP/BP, and window continuation |
| complete participant | reacting side | active cleared and opportunity resolved; no CP/BP/RNG |
| player decline | reacting side | unresolved opportunities closed; exact Movement position restored |
| unavailable/timeout | system | same closure effect, distinct internal reason |
| empty universe | system | deterministic `N+2` close after persisted generic `N+1` window |

`MoveElement` remains one command and `CampaignActionExecution.Complete` remains exactly one event.
The `ElementMoved` successor carries an optional canonical opened-window result. The factory derives
that result from the prior snapshot, compatible Content/rules, topology, and accepted movement;
the projector recomputes and byte-compares the entire event before replacing state atomically.
The same event carries the exact stage/segment-scoped Movement-ended result when the destination is
in authoritative enemy ZOC.

Suggested new command/event pairs are:

- `MoveReactingElement` / `ReactingElementMoved`, whose first accepted step also starts the episode;
- `CompleteReactionParticipant` / `ReactionParticipantCompleted`; and
- `CloseReactionWindow` / `ReactionWindowClosed`.

Exact names remain implementation details if their responsibilities, identities, and one-event
transitions remain unchanged.

A player-close action exists only while `activeOpportunityId` is null. Closed, reason-specific
System action kinds represent `scripted-unavailable`, `timeout`, and the internally derived
`no-eligible-reactor` path; callers cannot supply an arbitrary reason. Unavailable/timeout may close
an active episode after at least one accepted step, resolving it and the remaining universe without
reversing already committed CP/BP/location changes. Only the System audience can submit those exact
current action identities.

## Trigger and eligibility calculation

After validating an ordinary phasing combat-element move, the factory:

1. derives its exact post-move authority and ledger truth;
2. queries authoritative adjacency for represented non-phasing combat elements;
3. opens no window when none are adjacent;
4. otherwise derives the stable window identity regardless of positive ZOC;
5. derives and records whether positive enemy-ZOC entry ends the mover for this Movement Segment;
6. restricts the participant pool to represented non-phasing combat elements individually adjacent
   to the committed destination, retaining each trigger-time location/adjacency proof;
7. calculates each such participant's remaining opportunity eligibility from exact
   Rules/Content/Campaign/topology truth;
8. freezes canonical opportunity identities and replay evidence, including an empty collection; and
9. returns one canonical `ElementMoved` successor containing the entire resulting window truth.

The factory resolves the first-acting side from the unique retained order for the current turn and
Operation Stage, then requires both materialized sequence position and replay input to match it.
Because World/Content v5 has no exact HQ attachment relation, HQs remain adjacency triggers but are
excluded from ZOC-source and frozen-participant authority; co-location supplies no attachment fact.

Current membership later intersects unresolved frozen opportunities with current legal state.
This can remove but never add. The public action set is regenerated after every accepted event.
Because v1 contains no Close Assault declaration, Rule 8.53(b)'s conditional CPA comparison is not
an eligibility input. The accepted hidden-fact projection policy remains reserved for a later
declaration-aware contract.

## Reaction movement validation

Validation follows the existing Movement order, extended by the interrupt:

1. canonical contract/version/action identity;
2. campaign, expected state version, interrupt position, window, audience, and exact membership;
3. unresolved/active participant state and authoritative representation binding; the first step
   selects an unresolved participant, while later steps must use the active participant;
4. current location, Cohesion, category, attachment, and other admitted eligibility restrictions;
5. adjacent destination and topology direction;
6. source-defined Reaction restrictions, including no entry into enemy ZOC;
7. mobility, terrain, edge, stacking, exact CP cost, and stage-cumulative BP delta;
8. exact resulting ledger and window state; and
9. complete canonical event/successor invariants.

Participant completion is a current member only after at least one accepted step. No mutation occurs
until all validation succeeds. External rejection remains coarse enough not to
reveal hidden eligibility evidence.

The same topology-local ZOC service must replace
`CampaignMovementActionDerivation`'s temporary global positive-ZOC suppression before publication.
Ordinary Movement candidate tests cover entry, exit, exact local edges, and remote-positive-ZOC
noninterference. The side-safe deriver consumes public topology plus Observation 6's aggregate
`apparentEnemyControlledLocationIds`; command adjudication recomputes exact authority and can reject
a stale candidate without adding outward detail.

## Observation, history, and fallback

Observation 6 adds a closed decision-state union rather than optional Reaction fields scattered
across elements and one root-level, ordinal, duplicate-free
`apparentEnemyControlledLocationIds` collection. The collection reveals neither controlling source
nor predicate rationale. The reacting projection contains only an audience-safe window handle,
exact own unresolved/current state-scoped capability handles, their closed current move-option/cost
capabilities, active own participant state, and the apparent trigger triple. Core derives those
options from exact movement, ledger, Cohesion, Reserve, mobility, organization, and stacking truth,
then discards those raw ingredients at the user-space boundary. It omits identity-bearing root
owner-element rows and never publishes the authoritative window/opportunity identities or
representation-to-element binding; later command validation resolves that binding inside the
Umpire. The stable window handle derives only from facts visible to both sides. Opportunity handles
derive from the current audience-visible state version and canonical published move-option/cost
bytes. Canonical capability ordering makes the outward set independent of authority identity/order.
Construction and canonical readback recompute that handle rather than trusting supplied hashes.
They also require each cost breakdown to be an exact route/hexside traversal of its selected
published edge for one supported mobility, including feature classification, direction, and rule
amounts, and correlate the decision discriminator with `Observer` and `Position.ActiveSide`.
Two authority opportunities with indistinguishable published capabilities are rejected at this
boundary until a separately approved grouped-selection contract exists; hidden authority order is
never used to choose between them. Opportunity handles intentionally change with state or
capability so they cannot become cross-state representation joins.
The phasing projection contains the same audience-safe window handle plus generic waiting and retains its ordinary own
Movement/post-move facts.

Complete authority events retain real bindings, eligibility evidence, provenance, and internal
closure reason. Projected history uses a separate strict audience-keyed schema that omits those
facts. An explicit player knows its submitted close through the normal action/receipt path, not
through a published internal reason.

V1 delivers a deterministic Core system-close command whose identity binds the stable window ID and
whose closed action kind distinguishes unavailable/timeout. Core owns no clock or timeout scheduler.
Only the System audience may submit an exact current reason-specific action; players cannot supply a
reason field, and a stale or mismatched action finds no membership and emits nothing. Actual
OrleansHost/DecisionWorker pending-decision scheduling, deadlines, activation, and
automatic submission remain held behind a later hosting/dispatch task. This package therefore does
not contradict the roadmap's scaffold-only service boundary.

## Replay and compatibility

Every event factory has a pure reconstruction path. Replay begins from strict creation truth,
recomputes trigger/opportunity evidence from the historical pre-state, byte-compares the event, and
projects once. Later mutable authority cannot recalculate the historical frozen universe.

Compatibility tests must prove:

- exact version constants and canonical byte round trips;
- missing/extra/duplicate/reordered fields reject;
- legacy and mixed-identity authority reject at every admission/readback seam;
- changed trigger/opportunity/action preimage components change or invalidate identity;
- reordered/duplicate controlled-location rows and any source-mapped/injected control detail reject;
- rehashed semantic tampering still rejects through coherence validation; and
- replay produces byte-identical snapshot, observation, action-set, and redacted-history bytes.

## Implementation graph

The stable parent IDs remain those in the accepted ruling lock. Lettered slices are bounded,
dependency-ordered implementation and review checkpoints. A verified checkpoint may merge
independently to `main` only while its successor artifacts remain dormant and every active
admission/readback path stays on the complete legacy identity set. That staged delivery rule
applies to checkpoints 002A-005; later checkpoints remain separately dependency-gated.
Task 006C performs one coordinated activation commit that switches every coupled version and
identity together. No partial Rules, Content, World, Snapshot, Creation, sequence, Observation,
policy, command, event, or action-kind successor may become active on `main`.

```text
ZOR-TASK-001 accepted ruling lock [complete]
        |
        v
002A Rules vocabulary/predicates
        |
        v
002B Content component facts + scenario TOE seeds + compatibility
        |
        v
002C positive/negative fixtures + identity goldens
        |
        v
003A Campaign current TOE + Movement-ended + sequence/window identity types
        |
        v
003B snapshot/open-event/replay contracts
        |
        v
004A Observation 6 + side-safe policy
        |
        v
004B dormant actions/readback/fog equivalence
        |
        v
004C declassification manifest/capabilities/transcript gate
        |
        v
005 internal move-to-window trigger (public trigger path still dormant)
        |
        v
006A decline/system/empty-window closure
        |
        v
006B participant selection/episode/completion + Reaction CP/BP movement
        |
        v
006C atomic publication, Core system-close exposure, and Movement resumption
        |
        v
007A Runner controllers + checked Maneuver fixture
        |
        v
007B strict evidence, docs, full gate, and independent review
```

### `ZOR-TASK-002` — Freeze Rules, Content, and fixtures

- **002A (implemented):** prepare the dormant ruleset successor; add closed combat/ZOC vocabulary, predicates,
  topology exclusions, provenance, checked arithmetic, and table-driven unit tests without changing
  the active ruleset identity.
- **002B (implemented):** prepare the dormant Content successor; add strict component definitions/ratings and
  provenance-bearing scenario current-TOE seeds, admission, canonical identity, legacy/mixed
  rejection, and no derived ZOC or post-creation current state without admitting the successor on
  the active path.
- **002C (implemented):** add the positive stack, every named independent negative, non-additive overlap, and
  identity/readback goldens without changing Campaign runtime yet.

**Gate:** Rules/Content portions of `ZOR-REQ-001`-`002`, `012`-`013` are proven before state work:
component/seed identity, provenance, strict Content admission, static ZOC vectors, and Content-only
seed rejection. This gate does not claim creation, World, snapshot, or replay proof.

### `ZOR-TASK-003` — Freeze Campaign state, event, and replay contracts

- **003A (implemented):** prepare dormant World and creation successors; copy exact scenario component-TOE seeds
  and provenance into creation truth and mutable World; add checked current-raw derivation,
  stage/segment-scoped Movement-ended state, reacting-side position identity, window/opportunity
  IDs, and validation. The direct-only World 5 / CampaignCreated 9 seam derives the positive
  two-representation raw total as `10`, retains only mutable current TOE plus seed provenance in
  Campaign state, and leaves all active identities and serializers unchanged.
- **003B (implemented):** prepare dormant Snapshot and `ElementMoved` successors; add nullable
  window, suspended/current-position union, frozen adjacency evidence, distinct empty-window state,
  scoped Movement-ended result, canonical serializers/readers, reconstruction-before-projection,
  and checkpoint replay tests. The direct-only Snapshot 10 / `ElementMoved` v2 seam retains exact
  v5 current-TOE provenance and leaves every active identity unchanged.

**Gate:** every authoritative transition shape is replay-complete before any outward action exists.
Full `ZOR-AC-014` and the creation/World/replay portions of `ZOR-REQ-002` and `012` complete here,
not in Task 002.

### `ZOR-TASK-004` — Freeze dormant side-safe contracts

- **004A (implemented):** prepare dormant Observation 6/policy successors; add the aggregate
  source-unmapped apparent enemy-controlled location set, dormant reacting/phasing decision
  projections, strict readback, and projected-history shapes. The active public path remains on
  Observation 5.
- **004B (implemented):** add dormant topology-local ordinary-Movement ZOC entry/exit derivation plus dormant
  first/later Reaction step, complete, and close candidates, system close membership, canonical
  IDs, unpublished submission mappings, controlled-set local derivation, remote-ZOC noninterference,
  same-control/different-hidden-source equivalence, and audience fog-equivalence tests. Observation
  6 carries canonical owner-visible Movement-ended element IDs outside reacting decisions and
  state-scoped own capability handles during reacting decisions. Active Observation
  5/action/execution paths remain closed.
- **004C (implemented):** preserve real binding secrecy by replacing copied reacting Movement and
  anonymous stacking inputs with closed current move-option/cost capabilities. Reject
  identity-bearing owner rows semantically in reacting construction/readback, register the exact
  dormant output type/member surface in the versioned disclosure manifest, add retained-transcript
  regression evidence, and make the boundary suite a named mandatory `just check` gate.

**Gate:** byte-identical audience facts imply byte-identical observation/action bytes; retained
transcripts cannot reconstruct the real owner binding from copied raw inputs; all dormant outward
members are manifest-registered; public triggering Movement does not yet enter the window.

### `ZOR-TASK-005` — Implement the internal trigger

Add authoritative combat-element adjacency trigger, atomic `ElementMoved` successor
projection/replay, and exact authority-side topology-local Movement ZOC entry/exit adjudication.
Exercise the internal path and dormant Observation 6/action derivation directly while leaving the
active public Observation 5/Movement path unchanged.

**Status:** Implemented direct-only. Production successor reconstruction now derives the atomic
move, local control, scoped Movement-ended result, adjacency window, and frozen local opportunity
universe from admitted Content v5 / Snapshot 10 authority; default successor replay recomputes the
  same canonical event before projection. Public activation is completed by `ZOR-TASK-006C`.

**Gate:** triggering/non-triggering combat adjacency, multiple individually adjacent participants,
remote otherwise-eligible exclusion, noncombat-only adjacency, positive/nonpositive ZOC, local
entry/exit, remote-positive noninterference, and empty/nonempty cases produce
exactly one event, one state increment, correct Movement-ended membership, and byte-identical
reconstruction with no observable intermediate state.

### `ZOR-TASK-006` — Publish the complete Reaction vertical

- **006A:** implement player, unavailable/timeout, and empty system closure with exact resumption and
  zero-cost/no-RNG proofs; player close is absent while active, reason-specific exact System actions
  can close active windows, unknown/wrong-audience reasons reject, and no scheduler is included.
  **Implemented direct-only:** canonical `ReactionWindowClosed` reconstruction validates exact
  current action membership and audience, resolves active plus remaining frozen authority, restores
  the suspended Movement position, and preserves committed World/random state. Public activation
  is completed by `006C`.
- **006B:** implement first-step participant selection, one-or-more-step active movement, participant
  completion, existing CP/BP/provenance, and all current-membership rejection classes.
  **Implemented direct-only:** canonical participant move/completion events resolve the exact
  state-scoped capability to frozen authority, reuse ordinary Movement cost/provenance and World
  projection, enforce active-participant sequencing, preserve Breakdown/RNG truth, and reconstruct
  the identical event before replay projection. Public activation is completed by `006C`.
- **006C:** atomically switch to Observation 6 and the topology-local ordinary-Movement/Reaction
  action surface; bump and switch the complete Rules, sequence, Content, World, Creation, Snapshot,
  event, Observation, policy, command, and closed action-kind identity set in one activation commit;
  publish trigger membership and Core system-close submission by stable window ID; close/resume exact
  Movement; and prove later-trigger repeat eligibility. Hosting/worker timeout orchestration remains
  a later separately approved task. No earlier task activates a successor identity or publishes
  positive ZOC into the active Movement action path.
  **Implemented:** public Core creation, authority/session state, observations, legal actions,
  submissions, checkpoint serialization, and replay use the complete successor identity set.
  Unchanged pre-Movement semantic events advance Snapshot 10 through checked predecessor
  reconstruction and lifting; current readers reject legacy creation and `ElementMoved` roots.

**Gate:** `ZOR-AC-001`-`009`, `011`-`013`, and `015`-`017` pass through the ordinary public
submission path. Static fixture evidence remains owned by `ZOR-AC-010`; creation/World seed
round-trip evidence remains owned by `ZOR-AC-014`.

### `ZOR-TASK-007` — Adopt and close the package

- **007A:** add bounded Runner controllers and checked Maneuver children for ordering, subset close,
  active/system close, empty/unavailable, repeat trigger, adjacent/remote participant selection,
  positive/negative/local/remote ZOC, CP/BP, and final resumed Movement.
- **007B:** add strict bundle/report readback, reconstruction, fresh-session re-adjudication,
  matching clean-run fingerprints, documentation synchronization, full `just check`, and independent
  review reconciliation.

**Gate:** all `ZOR-REQ-*` and `ZOR-AC-*` map to checked evidence and no unresolved P0/P1 finding.

## Activation compatibility matrix

| Repository state | Active identity set | Successor visibility | Admission/readback rule |
| --- | --- | --- | --- |
| `main` before the ZOR delivery | Complete legacy set only | None | Legacy current identities accepted; successor and mixed identities reject. |
| `main` during dormant checkpoints 002A-006B | Complete legacy set only | Dormant successor types, codecs, fixtures, and direct tests only | Verified checkpoints may merge independently, but public campaign creation, bundle admission, replay, observations, and submissions remain legacy-only; dormant successor tests cannot flow through an active session. |
| 006C activation commit | Complete successor set only | Public and authoritative | Every coupled identity changes together; legacy-only and every partial legacy/successor matrix reject at creation, admission, readback, replay, and submission. |
| `main` after 006C activation merges | Complete successor set only | Public and authoritative | Only the canonical successor set is current; no dual-current or downgrade mode exists. |

Each dormant checkpoint must build and pass its direct tests before it merges, and it remains a
non-deployable compatibility state even when retained on `main`. The activation commit and final PR
gate must exercise the complete legacy-only, successor-only, and partial-mixture matrix. If any
checkpoint requires a partial successor identity to become active before 006C, implementation must
stop for a new owner-approved migration design rather than weakening `ZOR-REQ-012`.

## Parallel work boundary

ZOR production remains serial in dependency order through 006C because identities and shared
authority shapes are coupled. Verified dormant checkpoints may merge independently under the
compatibility matrix above, but their shared Core contract work cannot proceed concurrently. The
safe parallel lane is Breakdown adjudication research/design:
source-lock the percentage outcome table, action/result/loss vocabulary, RNG evidence, and decision
packet against the already implemented BP state. It may not modify shared Core contract files or
claim implementation approval while ZOR contract versions are moving. The Campaign seam was frozen
by 003B, so that design may now reconcile against it while remaining outside this implementation
train.

006C now exposes complete authoritative Reaction outcomes. `007A` turns those outcomes into checked
simulator evidence before behavior tuning claims rely on them.

## Review focus

Independent review should challenge atomic move/window semantics, frozen-versus-current membership,
clean-cut compatibility, component/current-TOE derivation, audience equivalence, empty/fallback
closure, exact sequence resumption, and whether any later Combat or Breakdown authority leaked into
this package.

## Implementation conventions and verification commands

Keep closed domain tokens in the existing `land.*` namespace, canonical JSON property order
explicit, arithmetic checked, collections ordinally sorted, and validators adjacent to the contract
they protect. Use small immutable records, exhaustive closed-kind switches, seeded/fake inputs, and
table-driven deterministic tests. Do not add a service where a Core domain module is sufficient,
and do not generate artifacts merely to restate hand-written canonical contracts.

Each slice runs its focused Core or Runner project tests using .NET 10 Microsoft.Testing.Platform
`--project`. Each parent-task merge runs the repository commands from `AGENTS.md`; package closeout
runs:

```bash
dotnet restore Sandtable.slnx
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
dotnet format Sandtable.slnx --verify-no-changes --no-restore
just check
git diff --check
```
