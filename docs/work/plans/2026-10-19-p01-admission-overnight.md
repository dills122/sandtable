# P01 admission proof: overnight execution packet

**Status:** working plan, not a support certificate or admission decision.
**Baseline:** `228aec8ce65e1d901fe667f3c5f47e1511002a14` (#283).
**Authority:** [P01 acceptance matrix](../../specs/combat-actual-moved-repeat-position-v1.md), accepted `CMB-POL-001`, `004`, `009`, and the pinned original CreationRequest plus eleven R03 ledgers.

## Decision this run must enable

Determine whether the bounded initial-RC P01 profile has a complete, source-backed support certificate for its original 300 authenticated rows. Preserve the current `44` provisional positive / `256` candidate-absent classification and `0` admitted actions until every gate below passes on one reviewed head. A provisional absence is never a supported-empty result.

| Gate | Current evidence | Overnight proof or explicit blocker |
| --- | --- | --- |
| AC-003 action and provenance | Authenticated R03 ancestry, Weather bridge, action matrix and selected private continuations. Matrix states `sourceLegalActionExhaustive=false`. | Enumerate all relevant source actions and accepted policy exclusions. Prove no alternate action inside the bounded profile with fail-sensitive tests; identify any case that needs new typed authority. Do not describe the profile as fully source-legal while voluntary surrender and movement RBA are excluded. |
| AC-006 surrender and RBA | Private explicit defender decline/cancellation, selected-assault and Force Assignment paths. | Authenticate automatic-surrender trigger and settlement, or show exact missing source/input path. Timeouts cannot synthesize decline. Current 300 roots may not contain a positive trigger; a negative classification alone cannot close this gate. |
| AC-007 outward privacy | Private `p01.` candidate hash and disconnected owner-projection v0 scaffold. | Prepare exact owner-visible fields, scope, revision, alias and rejection contract for owner review. Then authenticate two complete equal-visible histories with different hidden facts and compare candidate bytes, IDs, acceptance and rejection. Synthetic twins and denial-only output do not close the gate. |
| AC-008 replay and resources | #281 two-root fresh-process readback and #283 all-44 in-process prepared/committed cut matrix. | Prove all 44 paid roots cross a genuine fresh process boundary at both cuts with byte-equal Control, World and receipt, accepted retry without second debit, physical negatives and frozen caps. Extend to the remaining 300-row and nonterminal-cut requirements before calling AC-008 complete. |

The first overnight source audits found two concrete limits. AC-003's current wording cannot certify every source-legal action while accepted policy excludes Probe/partial allocation, no-assault/no-allocation, movement RBA and voluntary surrender. The next contract decision should demand exhaustive **bounded-profile classification**—supported, condition-absent, policy-excluded, or unsupported/fail-closed—for each source family, while retaining `sourceLegalActionExhaustive=false`. The action-matrix tests passed 6/6 and the original 300-row audit remained 44/256/0; no diagnostic-only PR was produced.

AC-006's existing 300 roots contain no automatic-surrender trigger. The 44 selected assaults start at ammo 10/Cohesion 0, the 12 adjacent ammunitionless rows cannot select an assault, and the 244 nonadjacent rows have no assault trigger. The source-condition and boundary tests passed 12/12 and the pinned RBA audit passed. A positive proof needs a new independently source-authenticated legal scenario or predecessor with an armed attacker and a legally ammunitionless or sufficiently low-Cohesion defender, an actual assault trigger, complete ancestor replay, and typed full-TOE/custody settlement. Do not hand-edit an R03 World or infer surrender from ammo spent by that same attack.

The follow-on feasibility probe confirmed that the current certified profile cannot supply that
positive trigger: defender initial ammo `0` fails Content7 validation at
`CMB-CNT-007 /scenarios/0/initialPlacements/1/initialAmmunition/points`; creation Cohesion `-17`
with a recomputed setup hash fails `CMB-INI-006 /combatInitialization/cohesionLevel`. Both existing
units spend ammo `10→0` in the paid attack, so its resulting `0/0` state cannot place an armed
attacker against an ammunitionless defender. Choose a separately versioned, source-approved
Content/Creation profile that permits the asymmetric trigger, or authenticate a later predecessor
that produces it without violating current validators. Either path needs fresh source, ancestry and
settlement review before any AC-006 positive claim. No mutation of the current profile is authority.

## Work order and stop rules

1. Resume the saved all-44 fresh-process worker without replacing its worktree or untracked scripts. Benchmark four roots, then the full 44; stop without a PR if the proof cannot fit the measured CI budget or a fresh process is not real for every root.
2. Run independent AC-003 and AC-006 source audits in disjoint worktrees. Produce code only for behavior-bearing, fail-sensitive proof. If current authenticated inputs cannot support a positive branch, retain a precise blocker rather than adding another diagnostic-only PR.
3. Draft the AC-007 owner decision packet before seeking approval. No public or native candidate is issued from the private digest. The proposal must pin campaign/window scope, owner-visible participant and target references, visible revision, alias encoding, and side-safe rejection behavior; authentic twin histories are a separate executable requirement.
4. For each reviewable slice: focused deterministic tests, pinned full audit, draft PR, fresh independent review, exact-head CI, unchanged clean mergeable head, then merge. Re-run review and CI after every fix. Record observed outcome in Ruflo, not an inferred completion.
5. Only after AC-003/006/007/008 and the complete P01 support certificate pass on one reviewed head, consider P02 selection, P03 native consumption, public activation, Exercise and Runner. These are separate gates. Do not claim a full Combat loop or playable Maproom from private proofs.

## Proposed AC-007 owner scope for review

The [owner projection v0](../../specs/combat-p01-owner-projection-v0.md) proposes four alias inputs: side, visible decision revision, capability kind and apparent target reference. It deliberately has no approved owner observation or positive outward action. Before contract freeze, present a concrete proposal covering:

| Proposed field | Source and rule |
| --- | --- |
| `campaignId`, `audienceSide` | Trusted active campaign and authenticated seat, never browser-selected. |
| `ownerDecisionRef` | Proposed reuse of the accepted side-projection `decisionId` pattern, not an existing P01 production field: a new versioned P01 public domain over typed `IdentitySeed3={context,cycleRef,positionId,openingRevision,kind,participantRef,targetRef}`. `cycleRef` uses the frozen public cycle tuple (including ordinal), and `openingRevision` comes from the authenticated owner-visible opening. Define the P01 decision kind and codec explicitly. Distinct visible openings in one history must differ; equal-visible hidden forks must match; restart must replay the same reference. No pre-open decision is fabricated on cancellation. Freeze golden fixtures for repeated same-position openings, both sides, hidden forks and restart before issuing an alias. |
| `openingVisibleRevision` | Per-audience revision captured when the owner decision opens and held fixed in its reference and retries. Current observation revision is separate; advance it only when the approved owner-visible projection or window lifecycle changes. Hidden-only bookkeeping does not change it. |
| `capabilityKind` | Closed `close-assault-full` value for this bounded profile. |
| `ownParticipantRef` | Existing owner-visible own formation reference from the same authorized observation cut; never an internal UnitKey unless that exact value is already approved outward. |
| `apparentTargetRef` | The authorized context/location target reference from the same observation cut, following the accepted side projection's `{context,locationId}` target seed. Close Assault targets a hex; Umpire retains and authenticates the exact frozen defender set privately. Do not salt this reference with hidden contact identity or count. |

After `ownerDecisionRef` is frozen, the proposed alias is `p01o.` followed by lowercase SHA-256 of ASCII domain `sandtable.combat.p01.owner-candidate.v1`, one NUL byte and compact canonical ASCII JSON in the table's field order. The Umpire retains an exact alias-to-private-candidate map scoped to the authenticated campaign, seat and opened decision. An alias is an identity, not authorization: each submission authenticates the audience and validates exact retained membership against the frozen combat base and **only** the permitted bookkeeping event suffix. A combat-fact change cannot silently alter legality under the same visible decision revision: it invalidates the decision and requires an owner-visible terminal or reopened boundary before another submission. Duplicate outward aliases and equal-visible histories with differing acceptance outcomes block publication.

Proposed side-safe rejection shape: authenticate the seat before any alias lookup; syntactically malformed requests can be `invalid-request`, and an obsolete **visible** decision revision can be `stale-view` only when the owner-visible boundary actually changed. Until a new outward rejection contract is approved, hidden-dependent non-membership or profile failure uses the existing generic `not-current-or-not-admissible` shape with no blocker detail. An unchanged visible revision must not become stale solely because opponent-only bookkeeping advanced. Prove equal-visible histories yield byte-equal candidates **and** equal accept/reject outcomes, including post-opening hidden bookkeeping and invalidation boundaries. Do not return a private digest, raw state version, hidden unit identity, event count or source hash in any outward payload or log. Exact retry identity remains the host's separate contract.

These fields and rejection classes are **proposals for owner decision**, not approved runtime contracts. The accepted side projection proves an `IdentitySeed3` reference for its own bounded evidence but allocates no P01 production field, protobuf, host route or durable publication. The owner decision packet is incomplete until the P01 domain/kind/codec, opening lifecycle and repeated-window/restart fixtures are specified. The existing four-field v0 scaffold remains disconnected until that packet is accepted and a versioned schema, canonical golden bytes, authenticated hidden-variant pair and acceptance/rejection comparison pass independent review.

Approval of wording does not replace the authenticated equal-visible hidden-history test. If the accepted fields cannot yield such a pair from original sources, AC-007 remains open and P01 remains unsupported.
