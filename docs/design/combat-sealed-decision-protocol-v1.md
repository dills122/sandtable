# Combat Sealed Decisions, Events, Readback, and Fallback

**Status:** `CMB-DES-002` design packet independently reviewed; no production contract activation.
**Date:** 2026-09-06. **Input checkpoint:** `0363d0c` (`CMB-DES-001`).

This protocol lets Umpire persist private choices against one frozen combat base, resume after a
restart, and commit an attack exactly once. It continues the
[identity design](combat-opportunity-identity-v1.md) and
[Combat/cycle inventory](../research/combat-cycle-source-inventory.md). It selects the bounded
singleton infantry protocol; `CMB-DES-003` next composes the six Combat steps around it.

## Authority, scope, and current gaps

The [cycle decision](../research/continual-cycle-identity-and-history-decision.md), especially
`CYCLE-DEC-002`, `007`-`011`, requires separate public/internal bindings, pre-RNG attack history,
sealed-choice closure before cycle advance, and a fallback that never invents a sealed combat
choice. `CMB-ID-001`-`008` supplies unit, target, opportunity, and disclosure constraints. The
[source inventory](../research/combat-cycle-source-inventory.md#private-and-simultaneous-choice-boundary)
requires trusted-Umpire sealing; cryptographic commit/reveal is unnecessary. Timeout and digital
receipt rules here are Sandtable proposals, not physical-rules claims or blanket research approval.

The selected [result surface](../research/combat-rules-result-surface-spike.md) has one independent,
non-Reserve infantry battalion per side, one 10-TOE component each, full commitment and an already
declined Retreat Before Assault. The public Breakdown profile has empty ZOC. Only voluntary
opportunities with no outstanding mandatory attack obligation enter this protocol. Broader choices,
mandatory ZOC attacks, actual Retreat Before Assault, multiple targets, Probe, Barrage and Anti-Armor
participants require later admission; their absence cannot be chosen by inspecting a future roll.

Current code provides patterns, not this lifecycle:

- [Submission mapping](../../src/Cna.Core/Actions/CampaignObservationV7ActionDerivation.cs),
  `MapSubmission`, checks audience-visible revision and exact candidate membership. The existing
  [submission v1](../../src/Cna.Core/Actions/CampaignActionSubmission.cs) has no round/slot binding.
- [PendingDecision](../../src/Cna.Core/Decisions/PendingDecision.cs) and
  [proposal validator](../../src/Cna.Core/Decisions/DecisionProposalValidator.cs) check one plan's
  ID/version/rules hash. They do not implement two private sides, deadlines, or combat provenance.
- [Signals protobuf](../../src/Cna.Intelligence.Contracts/Protos/intelligence.proto) lacks a
  configuration binding and typed Combat payload. Its generic parameters map is not this protocol.
- [Host](../../src/Cna.OrleansHost/Program.cs) configures Orleans/endpoints;
  [Worker](../../src/Cna.DecisionWorker/Program.cs) registers a gRPC client. Persistent campaign
  dispatch, deadline recovery, outbox and authorization are future implementation requirements.

## Decisions and state model

| ID | Decision |
| --- | --- |
| `CMB-PRO-001` | Persist one force-assignment round per selected opportunity. Two required role slots, attacker and defender, share one frozen combat base. Sealing changes bookkeeping only. |
| `CMB-PRO-002` | Bind public proposals to an authenticated audience, visible decision revision, exact action set, round and slot references. Retain the full authority mapping privately; never send a combat-base hash or authority version to a player or model. |
| `CMB-PRO-003` | Accept each slot once. Accepting one side does not change the other side's visible revision, candidate set, deadline, or frozen base. Sealed choices cannot be edited or withdrawn by the submitter. |
| `CMB-PRO-004` | An incomplete voluntary round expires or closes on trusted controller-unavailable input. Cancel the whole opportunity without synthesizing either slot, combat costs, history, target-hex use or RNG. A prepared round proceeds without controllers. |
| `CMB-PRO-005` | At Close Assault, revalidate the frozen base, retained choices and exact permitted transition trace; atomically commit the attack, unit history and target-hex use before any result RNG. After commitment, cancellation is unavailable. |
| `CMB-PRO-006` | Chronicle records each semantic transition once; Archives retains all open-round evidence required for replay. Readback exposes separate authority and audience projections, never raw event envelopes in user-space artifacts. |
| `CMB-PRO-007` | Pin protocol, codec, policy and configuration versions at opening. Historical contracts remain unchanged; activate successors only after the combined design/specification and compatibility freeze. |

States are a closed union, not independently toggleable flags:

| State | Retained evidence and allowed successor |
| --- | --- |
| `Collecting` | Opening context and zero or one sealed slot. Next valid seal yields `Collecting` or `Prepared`; trusted expiry/unavailability yields `Closed(cancelled)`. |
| `Prepared` | Both slots sealed. No further submission, expiry or user cancellation. Carry through the certified empty Anti-Armor transition, then commit at Close Assault. |
| `Committed` | Commitment/history/hex-use evidence, retained assignments and pre-result RNG cursor. Only the resolver/settlement path can advance. |
| `Settling` | DES-004/005 result checkpoints and remaining mandatory effects. Resume deterministically; cannot reopen choices or finish the cycle. |
| `Closed` | Exactly one terminal outcome: cancelled before commitment, or completed after all settlement. Retain terminal receipt/history bindings; cannot reopen this occurrence. |

`Prepared` is derived by the second seal, not a third optional event. `Settling` is driven by
DES-004/005 events, whose payloads are not frozen here. An empty legal opportunity universe creates
no player/model round. This does not authorize skipping structural Combat steps or obligations.

## Binding and typed payload

The following closed semantic records are the inputs to the later versioned wire freeze. Names and
domain tags here are proposed v1 protocol names; historical Snapshot/Observation/event version
numbers and protobuf field numbers will be assigned in the combined contract package.

| Record | Required fields / rule |
| --- | --- |
| `RoundAuthority` | `protocolVersion`, `opportunityId`, `cycleIdentity`, `openingPosition`, `combatBaseDigest`, `openingAuthorityVersion`, `openingHistoryPrefixDigest`, full rules/setup/Content provenance, canonical participant/target bindings, `requiredSlots`, pinned policy and timing inputs. All DES-001 identity inputs remain required. |
| `SlotAuthority` | Role, owning side, authority slot ID, audience round/decision/slot references, opening visible revision, action-set binding, canonical allowed own payload, and optional sealed record. Exactly one slot per role/side; no hidden-sized slot list. |
| `CombatChoiceSubmission` | Protocol version, campaign ID, authorized rules/config references, audience, audience round/decision/slot references, expected visible revision, action-set binding, action ID and typed choice. No raw authority preimage, version, digest or opponent unit ID. |
| `FullCloseAssaultChoice` | Tag `full-close-assault`; exact own participant reference and one own component allocation containing its approved component reference and `committedToe = 10`. Exact equality with the published candidate is required, not merely an in-range number. |
| `SealedChoice` | Authority slot ID, canonical submission bytes/digest, normalized binding to own rules unit/component, accepting event/version, and accepted own receipt. No provider narrative or arbitrary parameters. |
| `RoundTerminal` | Round/commitment binding as applicable, terminal outcome, closure event/version, and retained audience receipt mappings. Internal cancellation cause stays private. |

Opening requires certified full-result support, exact singleton membership, selected resources and
the retained prior-step proof: Position/Barrage closed without relevant effects and defender's
Retreat Before Assault decline. Decline is a prerequisite produced by DES-003, not a default inferred
from a missing defender response here. Assignment changes after Barrage or an actual retreat need
their own newly validated base; this protocol does not carry a stale opportunity through them.

Canonical encoding uses UTF-8 JSON with fixed property order, closed string tags, integer quantities,
ordinally ordered identity arrays and duplicate rejection. Reject unknown/duplicate properties,
unknown tags, missing required values, floats in integer fields and noncanonical IDs. No permissive
parse-and-drop path may hash different bytes as one accepted submission. The combined freeze must
publish exact schemas plus positive/negative canonical-byte fixtures before any serializer ships.

Domain-separated SHA-256 identities use `sandtable.combat.round.v1` for the authority round and
`sandtable.combat.slot.v1` for authority slots. The round preimage includes all `RoundAuthority`
opening inputs; it excludes its own computed ID and subsequent sealed/terminal fields. Slot identity
binds that round and role. The combat-base digest covers the canonical pre-round campaign snapshot;
the history prefix stops before the opening event. Neither hash recursively contains itself.
DES-001 supplies unit/opportunity semantics; CYCLE-DES-001 still owns the cycle/prefix codecs.

Public round, decision, slot, action and set hashes use distinct `sandtable.observation.combat.*.v1`
domains and only the canonical audience projection: campaign, audience-safe cycle/opportunity and
position references, approved rules/config references, visible revision, own role/capability and
typed candidate. The exact suffix/field order is a required wire-freeze fixture, not an implicit
alias of an authority hash. Config references exposed here cover only approved public policy/rules;
the Umpire retains a separate binding to full setup/Content hashes when those encode hidden facts.

## Submission and revision semantics

Authenticate before resolving references; an asserted audience string is never authorization.
Then require the supported schema, exact current audience slot/action-set binding and byte-exact
candidate payload. Resolve through the retained authority mapping and revalidate membership,
resources and frozen combat facts. Normalize only according to the pinned canonical codec.

The frozen snapshot's authority version is deliberately not the current authority version after
one seal. Only these bookkeeping deltas are allowed while collecting: the other slot's seal and
its own audience receipt/revision. No location, TOE, CP, Cohesion, ammo, Reserve, relation, rules,
Content, RNG or target-use change is allowed. Compare against the retained base and permitted
event suffix, not a hand-maintained subset of convenient combat fields.

Each audience revision advances once per event that changes its authorized projection. The owning
side's seal consumes its action set and publishes its own accepted receipt; the opponent's bytes
remain unchanged. Common opening and terminal/result boundaries advance each affected audience
once. Hidden bookkeeping cannot stale an otherwise identical opposing proposal. Decision IDs bind
the opening visible revision and remain stable for retries of that slot; current readback revision
may advance after acceptance. Never compute visible revision by subtracting a guessed hidden count.

A repeated, changed, old-round, wrong-side, consumed-slot or otherwise stale submission is rejected
with zero semantic events and no RNG. Ambiguous transport success is recovered through own-receipt
readback; it is not permission to submit again under a fresh decision ID. A transport adapter may
return that stored receipt for an exact duplicate after authentication; Umpire never accepts twice.
Different content under the same decision ID cannot overwrite the receipt. Rejection uses the same
side-safe `not-current-or-not-admissible` shape without internal membership/base/cause detail.
Equal visible histories must also have equal accept/reject outcomes, not just identical labels;
DES-005 must close any profile where hidden legality could violate this requirement.

Two arrival orders may produce different full Chronicle order and private intermediate receipts.
With identical final choices and no expiry, they must have identical frozen assignments, resource
effects, ordered result draws and final audience-visible result facts. Their authority commitment
IDs/history prefixes may differ because the actual accepted event order differs. Replay of each
individual history must be byte-identical; cross-order full snapshot equality is not promised.

## Deadline, fallback, and race policy

Opening pins an explicit positive finite `decisionBudgetMilliseconds`, policy ID/version and
trusted UTC opening instant; checked addition yields one shared deadline for both slots. Missing
policy, invalid/overflowing time or an unsupported controller mode rejects before opening. No
implicit deployment default or deadline extension on retry is allowed. Numeric budgets belong to
versioned scenario/controller configuration, not to a combat-strength rule. The shared deadline is
published at opening to both audiences; later private traffic cannot change it.

Core consumes trusted time values as command inputs, not wall-clock I/O during replay. A future
host samples its trusted clock inside serialized command admission. Proposal acceptance requires
admission time strictly before the retained deadline; equality belongs to expiry. Chronicle retains
accepted admission-time evidence for strict replay validation. A fake clock supplies these values
in Exercise tests. Retain the high-water UTC instant from accepted opening/seal/terminal records;
rejected/read-only calls do not mutate it. A detected regression below that instant, or loss of
trusted-clock confidence, takes the controller-unavailable path for a collecting round. Do not
extend its deadline or wait for a regressed clock to catch up. Prepared/committed recovery proceeds
without a clock-dependent choice. The opening/deadline context is never recreated on restart.

A timer wakeup carries the exact round binding. If `Collecting` and trusted admission time reaches
the deadline, emit one cancellation. Explicit controller-unavailable input may cancel earlier;
only a trusted host/controller adapter can assert it, never a model response or opposing player.
A malformed model proposal is rejected and may be retried under the same decision ID until the
fixed deadline; it cannot reset the budget. Model timeout/unavailability can produce the trusted
unavailable input without waiting for a provider on a grain turn.

The serialized winning transition determines a race: second valid seal before deadline produces
`Prepared`, making later expiry/unavailability a no-op; expiry while collecting cancels, making
the late seal stale. First-side acceptance alone does not prevent whole-round cancellation. Its
sealed evidence and own receipt remain in history, marked by the round's generic terminal closure.
This is system cancellation, not an opponent-authorized withdrawal or a fabricated default choice.
Do not expose which side timed out, slot counts, retries or backend diagnostics in side history.

Cancellation disposes this opportunity for the current Combat Segment; it cannot be reopened with
a new deadline or count as material progress that enables auto-repeat. Preserve CP/BP/Cohesion,
TOE/ammo, relations, history, target-hex use and RNG. DES-003 advances through the remaining empty
structural steps; CYCLE-DES-001 later applies its own repeat/finish rule after all obligations clear.
Mandatory-attack profiles remain closed until a source-faithful non-cancelling fallback exists.

Once `Prepared`, no deadline can discard valid choices. Core needs no controller to commit; once
committed, provider loss cannot cancel, reroll or undo an attack. Resolver/settlement recovery uses
retained inputs and the campaign stream. Unsupported result/settlement cases must be excluded by
certification before opening, not converted to a successful pass after commitment.

## Events, atomicity, and strict readback

Each event retains the standard campaign/rules/config provenance, prior/result authority versions,
round and causal bindings, plus a closed payload. Each transition increments authority once.
State and Chronicle publication are atomic; side effects dispatch only after durable publication.

| Proposed semantic event | Required evidence and invariant |
| --- | --- |
| `CombatChoiceRoundOpened` | Full opening context, two slot mappings, base/prefix digests, prior-step proof, pinned timing/policy. No costs, attack history or RNG. |
| `CombatChoiceSealed` | One slot, canonical proposal and normalized own allocation, trusted admission instant and own receipt. Recompute from base plus allowed suffix; second seal derives `Prepared`. |
| `CombatChoiceRoundCancelled` | Exact collecting round, trusted trigger/time and terminal mappings. Incomplete round only; no attack or resource effect. |
| `CombatAttackCommitted` | Prepared choices, certified path from Force Assignment through empty Anti-Armor to Close Assault, current base-equivalence proof, unique commitment and singleton history edge, target-hex-use update, pre-result cursor. Atomic append; no roll. |
| `CombatChoiceRoundClosed` | Commitment and completed DES-004/005 settlement evidence, no unresolved loss/retreat/custody/relation obligations. Publishes completed terminal state; cannot close from `Collecting` or `Prepared`. |

The opening snapshot contains the Force Assignment position. Advancing to the Close Assault
position is a permitted structural change only through DES-003's certified no-effect transition
trace; raw whole-snapshot equality at commit would incorrectly reject that advance. Any extra
combat-fact change invalidates the proof. Replay rejects such an illegal suffix; cancellation must
not launder a corrupt history. DES-004 owns combat cost timing and resolution events, and must use
the committed allocation exactly once with no window for another authoritative action to spend it.

No Movement, Reaction, Reserve/cycle advance or second combat decision may interleave with an open
selected round. Persisted wait yields control to the host without holding a grain turn. Prepared
structural advance and committed result/settlement are system-owned continuations. The host must
recover those continuations after restart as well as pending timers; they are not dependent on
another user submission waking the campaign.

Archives retains the frozen base or a verified retrievable checkpoint, full opening/slot evidence,
accepted admission times, clock high-water value, deadline/policy, receipts, current lifecycle,
history/target-use and RNG/settlement continuation. No active evidence is pruned until its replay
obligations are met. Readback validates state-specific field presence, both-role uniqueness, hash
preimages, transitions, prior-step proof and derived history/hex-use against Chronicle. Tampering
with a slot, deadline, base, position trace, cursor or terminal cause must fail strict authority
readback; loading a snapshot does not mint fresh IDs or extend time.

Audience readback is a separately versioned projection: `choice-required` exposes only own candidate
and deadline, `own-choice-sealed` exposes the own receipt with no remaining-opponent count,
`waiting` is generic, and `closed` reveals no cancellation cause. Other side sealing leaves the
waiting audience's bytes unchanged. Result disclosure remains DES-005's gate; the sealed payload
does not become public merely because both slots are filled. Apply the same projection to Runner
bundles, diagnostics, receipts and model input. Timing/traffic analysis is a separate hosted review
requirement; canonical content equivalence does not claim constant response latency.

## Compatibility and implementation handoff

New protocol types must enter a successor campaign snapshot/event/observation/action family with
explicit Rules/Content/profile capability admission. Old codecs and retained Exercise bundles
continue to validate their historical bytes. Unknown Combat fields never fall through old parsers.
Rollback disables new campaign admission; an old binary cannot resume a new-version open round.
Keep the compatible reader/resolver available for those campaigns or restore a reviewed backup;
never erase pending choices or downgrade their snapshot to make rollback appear successful.

Signals must gain typed, versioned Combat request/response contracts before generated clients or
adapters. Carry audience-safe decision/state/config/rules references and only own legal choices;
the trusted host retains full authority provenance. Preserve old protobuf numbers. Provider trace
and commentary remain advisory metadata outside authority payloads. Dispatch deduplicates by the
same decision ID, uses cancellation/deadlines, and validates late replies at Umpire. Future durable
outbox/recovery must reconcile persisted slots before redelivery and never perform inference inside
an authoritative turn. None of that hosted integration is claimed implemented by this document.

| Owner | Required next output |
| --- | --- |
| DES-003 | Exact six-position events, prerequisite decline, no-effect trace, cancellation advance and voluntary opportunity selection/closure. No forced choice from a missing response. |
| DES-004 | Cost/commit/result ordering and deterministic resolver consuming the bound assignments/cursor once. No simultaneous AA/Barrage expansion without a new protocol admission. |
| DES-005 | All result/relationship/custody settlement, authorized target/choice disclosure and hidden-legality equivalence; ambiguous profiles rejected before active decision. |
| CYCLE-DES-001 | Cycle/prefix codec, disposed-opportunity/progress accounting, active-stage history, repeat/finish and snapshot composition. |
| Combined specification/contract freeze | Exact byte schemas/domain suffixes, field numbers, version migration matrix, bounded configuration and size limits, error/receipt contracts and negative fixtures; independent plan review before implementation tasks. |

## Acceptance and verification plan

These cases are protocol-design obligations, not passing runtime tests. Future tests use fake time,
seeded RNG and in-process authority, including failure injection at durable publication boundaries.

| Case | Required result | Decisions |
| --- | --- | --- |
| `CMB-PRO-AC-001` | Either seal order preserves the other side's base, action set and revision; equal final choices yield equal gameplay effects/draws. Each history independently replays byte-identically. | 001-003/005 |
| `CMB-PRO-AC-002` | Exact duplicate emits nothing; own readback recovers prior receipt. Changed payload under the same decision ID cannot replace it. | 002-003/006 |
| `CMB-PRO-AC-003` | Wrong audience/round/slot/config, old revision, changed target/resources or unknown fields reject without combat events/RNG or hidden diagnostics. | 002-003/007 |
| `CMB-PRO-AC-004` | Clock at deadline rejects the seal and permits one cancellation; just-before second seal makes expiry a no-op. Restart and backward clock cannot extend the deadline. | 003-004 |
| `CMB-PRO-AC-005` | Timeout/unavailability with zero or one seal cancels voluntarily without filling slots, costs, history, target use or RNG. Late timers/replies cannot reopen it. | 004/006 |
| `CMB-PRO-AC-006` | Prepared/committed restart continues without controller availability; commitment appends once before RNG; provider failure cannot undo or reroll. | 004-006 |
| `CMB-PRO-AC-007` | Certified empty Anti-Armor advance permits commit; forged/extra structural or combat-fact transition fails strict replay. No other action can spend committed allocations. | 001/005-006 |
| `CMB-PRO-AC-008` | Cut/restart at opening, either seal, commit, each settlement boundary and terminal closure reproduces receipts, bytes, history/hex use and RNG; no unresolved obligation permits completed closure. | 001-006 |
| `CMB-PRO-AC-009` | Alter base, slot, accepted time, deadline, codec/policy, cursor or terminal cause in snapshot/event evidence: strict readback fails. | 005-007 |
| `CMB-PRO-AC-010` | Hidden opponent identity/choice/acceptance-order/cause variations with equal visible histories preserve side artifacts and identical submission outcomes. No raw hashes/versions in Dispatch or Runner output. | 002-003/006 |
| `CMB-PRO-AC-011` | Ambiguous or mandatory-attack profile fails trusted certification before any active decision; cancellation never masks obligations or enables a same-opportunity retry/automatic repeat loop. | 004/007 |
| `CMB-PRO-AC-012` | Historical codecs/bundles unchanged; old reader rejects new open rounds; disabling admission preserves the compatible recovery path. | 007 |

Design completion requires review of those transitions and handoffs plus local link/ID/diff checks.
Production readiness additionally requires the remaining design gates, accepted policy choices,
the combined contract freeze and implemented tests above. Model-backed service availability is
never a prerequisite for recovering an already committed combat.

**Executed checks (2026-09-06, `893aeec`):** local-link validation resolved 175 targets across the two design
packets and three navigation documents. All 15 decision rows and 24 acceptance rows have unique,
ordered IDs. `git diff --check` passed. These checks validate documentation structure; state/race
semantics still require engineering review and the future runtime tests above. No .NET build or
tests were run for this documentation-only change.

[Independent review 1](../reviews/combat-identity-protocol-review-1.md) returned Ready for the
bounded DES-001/DES-002 design inputs at `893aeec`, with no actionable findings. Pending policy,
disclosure, exact contract and implementation gates remain unchanged.
