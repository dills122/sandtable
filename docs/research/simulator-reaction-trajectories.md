# Checked Reaction trajectories

**Status:** ZOR-TASK-007A implemented; ZOR-TASK-007B final gate and independent review in progress.

The [checked Maneuver](../../scenarios/maneuvers/rules-lab.reaction.serial.v2.json) runs fifteen
fresh campaigns through public Core creation, legal-action query, submission, strict evidence
readback, reconstruction, and fresh-session re-adjudication. Every child ends at first-side
Breakdown Determination with no open Reaction window. This is synthetic rules-laboratory evidence,
not a combat, historical-balance, or campaign-quality claim.

## Controller contract

All nine explicit `reaction-*` policies choose Act First, designate no reserves, and move each
phasing element at most once in stable element/destination order. During Reaction, System may
coexist with exactly one reacting player. Only the exact current Reaction kinds admit that
arbitration. Other audience conflicts fail closed.

Player selection uses public action IDs and kinds, plus counts of already accepted moves in the
current episode and completions in the current window. It never reads the real representation
binding, hidden eligibility inputs, or authority event payloads. Ascending/descending policies
choose their next current move by action ID; participants normally complete after one move.
`reaction-two-steps` allows two moves, or completes sooner when no move remains. The decline,
one-then-decline, unavailable, timeout, active-unavailable, and active-timeout policies exercise
their named closure paths. System reason selection is an explicit trusted harness policy; no
clock, inference, network request, or hosting scheduler is added.

Original Movement controller identities retain their earlier trajectories and fail-closed Reaction
boundary. The new policy names are admitted consistently in Exercise manifests, Maneuver manifests,
and controller-configuration identities.

## Fixture and trajectory matrix

The seven new synthetic Content v5 packs preserve existing Movement content identities. Each pack
has two phasing elements, two nearby potential reactors, and two remote combat elements. Current
TOE seeds keep phasing raw defensive strength below the ZOC threshold. Remote elements remain
connected through an intermediate location but are individually nonadjacent to the trigger.

| Child suffix | Accepted actions | Reaction moves | Windows | Checked outcome |
| --- | ---: | ---: | ---: | --- |
| adjacent.all-by-action-id | 21 | 3 | 2 | First window selects A then B; A participates again in later window |
| adjacent.all-by-descending-action-id | 23 | 4 | 2 | A/B then B/A participant orders retained exactly |
| adjacent.two-steps | 27 | 8 | 2 | Four episodes each take exactly two moves |
| adjacent.decline | 15 | 0 | 2 | Player closes both unresolved opportunities in each window |
| adjacent.one-then-decline | 19 | 2 | 2 | First window closes remaining one after a completed participant |
| adjacent.unavailable | 15 | 0 | 2 | Reason-specific System closure before any move |
| adjacent.timeout | 15 | 0 | 2 | Reason-specific System timeout before any move |
| adjacent.active-unavailable | 17 | 2 | 2 | Each active window closes immediately after one accepted move |
| adjacent.active-timeout | 17 | 2 | 2 | Each active window times out after one accepted move |
| positive-zoc.all-by-action-id | 23 | 4 | 2 | Two stacked battalions total raw defense 10; entry ends both phasing movers |
| low-defense.all-by-action-id | 23 | 4 | 2 | Same stacked source has raw defense 8; neither mover ends from entry |
| remote-zoc.all-by-action-id | 21 | 3 | 2 | Remote positive ZOC controls only remote neighbor and does not suppress local moves |
| headquarters.all-by-action-id | 15 | 0 | 2 | Combat adjacency opens empty windows; next state closes each without a route |
| noncombat.all-by-action-id | 13 | 0 | 0 | Noncombat-only adjacency does not trigger |
| recurrence.all-by-action-id | 21 | 3 | 2 | Same participant reacts in two distinct windows using then-current eligibility |

Exact counts are asserted by `ReactionManeuverTests`. `ReactionRunnerContentTests` separately checks
canonical content readback, public creation, and initial controlled-location sets: positive ZOC
controls exactly `center`, `north-east`, and `south-east`; remote ZOC controls exactly
`remote-neighbor`; all other initial sets are empty. Existing Core static vectors retain the
independent category, stacking, cohesion, terrain/hexside, enterability, and non-additive overlap
negatives.

## Accounting and strict evidence

Final per-element CP equals the exact rational sum of all accepted ordinary and Reaction move
costs. Completion and closure preserve committed costs. Existing vehicle-Breakdown state and
provenance remain intact; this package does not add Breakdown rolls or losses. The final RNG cursor
equals the last pre-Movement random event cursor, proving the checked Movement/Reaction paths add
no random draws. Current Movement completion uses Snapshot 10 directly: predecessor validation
would incorrectly reject moved non-phasing elements after an otherwise legal Reaction.

Runner admits exact Reaction move/completion/close field sets and contract versions, derives event
checkpoint positions from the retained suspended or resumed position, then re-adjudicates every
accepted submission against fresh Core authority. Tests reject extra, duplicate, reordered, and
legacy fields for all three event kinds. Rehashed event/window/opportunity substitutions and a
System close reason inconsistent with its submitted action still fail semantic bundle admission.

## Acceptance traceability

| Acceptance IDs | Checked evidence |
| --- | --- |
| ZOR-AC-001, 002, 003 | Fifteen-child matrix ordering/two-step/recurrence assertions; `CampaignSuccessorActivationTests` and `CampaignReactionParticipantReplayTests` cover public ordering, recurrence restrictions, and one episode per participant/window |
| ZOR-AC-004, 005, 017 | Decline/subset/unavailable/timeout/active-close children; `CampaignReactionWindowClosedReplayTests` and `CampaignSuccessorActivationTests` cover zero-cost closure and invalid audience/reason rejection |
| ZOR-AC-006, 016 | Headquarters/noncombat/adjacent children and remote-member exclusion; public trigger tests retain exact empty/nonempty transition and local adjacency proof |
| ZOR-AC-007, 008, 012, 018 | `CampaignObservationV6ActionDerivationTests`, Observation 6 and projected-history suites, disclosure manifest and mandatory `just boundary-check`; Runner controllers receive no new player-visible authority data |
| ZOR-AC-009 | Public activation and participant/closure replay rejection suites; rehashed Runner binding/reason tamper probes |
| ZOR-AC-010, 013, 015 | Positive/low-defense/remote-ZOC children, exact initial control assertions, `ZocStaticFixtureTests`, `ZocRulesTests`, and public topology/fog-equivalence suites |
| ZOR-AC-011 | Exact final CP sums, preserved vehicle-Breakdown state/provenance and RNG cursor, current Movement completion, participant replay suites |
| ZOR-AC-014 | Seven strict content/creation cases plus existing Content v5/current-TOE seed, World, creation, and replay rejection matrices |

Together these retain the requirement mappings in the [specification](../specs/zoc-reaction-v1.md)
for ZOR-REQ-001 through ZOR-REQ-014. Hosting deadlines/dispatch, Contact/Engaged, Combat, and
Breakdown adjudication remain separate roadmap gates.

## Reproduce

```bash
dotnet build Sandtable.slnx --no-restore --disable-build-servers -m:1 /bl:/tmp/zor-build.binlog
dotnet test --solution Sandtable.slnx --no-build
dotnet run --no-build --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- maneuver run --manifest scenarios/maneuvers/rules-lab.reaction.serial.v2.json --artifact-root artifacts/simulator/reaction
just check
```

Use a second artifact root for a repeated run. Compare validated `reportFingerprint` values,
canonical event streams, accepted actions, final snapshots, and proof bytes; run IDs, paths,
timings, and build working-tree diagnostics are intentionally outside deterministic report identity.
