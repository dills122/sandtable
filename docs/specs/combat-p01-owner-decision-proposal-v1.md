# P01 AC-007 owner decision proposal v1

**Status:** exact proposed private byte contract for owner review. It is not an approved
observation, action set, submission endpoint, P01 admission, or P02/P03 activation.
The [owner projection v0](combat-p01-owner-projection-v0.md) remains disconnected and
continues to return `unsupported`, `admitted=false`, and `publicActions=[]`.

## Requested owner decision

Approve or revise the entire proposed *owner-visible* tuple and rejection policy below as
one versioned scope. The accepted C3 [`IdentitySeed3`](combat-side-projection-v1.md) is a
pattern, not an already assigned P01 public ID. P01's private `p01.` digest,
`p01a.` paid candidate, `p01d.` decision, and `p01o.` opening receipts never become
outward aliases. This proposal uses `pub.` for a new-domain owner decision and the
distinct `p01-owner-v1.` for a proposed outward candidate. Neither is emitted today.

| Field or boundary | Exact proposed rule |
| --- | --- |
| `context` | Ordered C3-style `campaignId`, `audience`, `cycle`, `rulesRef`, `configRef`, `capabilityPolicyId`. Campaign and audience come from authenticated host/seat; public rules/config/capability refs must be approved and frozen for P01 before issuance. No browser-selected campaign, seat, hash, or policy. |
| `cycle` / `cycleRef` | Cycle is ordered `gameTurn`, `operationStage`, `playerPhaseSlot`, `phasingSide`, `ordinal`. `cycleRef` is the independently verified frozen public cycle codec1 digest, `sha256:` plus 64 lowercase hex. It includes ordinal and is never computed from a private Source/World hash. A new ordinal cannot reuse the previous decision. |
| `positionId` / window | Exact authenticated structural P01 position, including the reopened window's visible structural state. Two distinct openings at the same position are separated by monotonic owner-visible opening revision; no private event count or raw state version is exposed. No offer exists before a typed owner decision actually opens. |
| `openingRevision` | Represented R03 boundary starts owner-visible revision **0**. A separately visible first selection opening advances it to **1**; terminal closure advances to 2, and a later opening at the same position advances to 3. Every subsequent owner-visible opening/terminal advances by one. Hidden-only bookkeeping never ticks it. Retained side history, not process uptime, determines revision after restart. This is a proposed new boundary rule; v0's `>=1` alias input is compatible but supplies no opening authority. |
| `kind` / `capabilityKind` | Decision identity uses **`selection`** as the decision kind. The candidate uses **`close-assault-full`** as its capability kind. They are distinct closed tags; a capability cannot masquerade as a decision kind. |
| `participantRef` | Exact owner-visible own formation ref from the same approved observation cut. It is not a private UnitKey, creation hash, or source-native id unless separately approved as outward. |
| `targetRef` | `pub.` SHA-256 of `sandtable.combat.p01.owner-target.v1`, one NUL byte, and canonical ordered `{"context":Context,"locationId":id}`. `locationId` is the **approved owner-visible hex** at that cut. The defender set, hidden contact identity/count, and target-use history remain private. This follows C3's public context+location target shape, but has a separate P01 domain. |

### Frozen proposed codec

The accompanying [schema](combat-p01-owner-decision-proposal-v1.schema.json),
[pure verifier](verify-combat-p01-owner-decision-proposal-v1.py), and
[golden cases](fixtures/combat-p01-owner-decision-proposal-v1.json) specify exact order and
bytes. JSON is compact ASCII: UTF-8/ASCII characters, `ensure_ascii=true`, no spaces,
ordered keys, lowercase `true`/`false`/`null`, no duplicate keys, and no floats. IDs are
1–128 visible non-space ASCII bytes; `pub.` and `sha256:` refs have exactly 64 lowercase hex.
Unknown fields, unapproved enum values, malformed refs and noncanonical bytes reject.
The actual public cycle digest must be authenticated by its own codec; the pure proposal
verifier checks shape and golden serialization, not source authenticity.

```text
OwnerDecisionSeed = {
  context, cycleRef, positionId, openingRevision,
  kind:"selection", participantRef, targetRef
}
ownerDecisionRef = "pub." + hexlower(SHA256(
  ASCII("sandtable.combat.p01.owner-decision.v1") || 0x00 ||
  canonical(OwnerDecisionSeed)))

OwnerAliasSeed = {
  decisionRef:ownerDecisionRef, openingRevision,
  capabilityKind:"close-assault-full", ownParticipantRef,
  apparentTargetRef:targetRef
}
ownerCandidateRef = "p01-owner-v1." + hexlower(SHA256(
  ASCII("sandtable.combat.p01.owner-candidate.v1") || 0x00 ||
  canonical(OwnerAliasSeed)))
```

The order shown is byte order. The alias is scoped by the decision's context, cycle,
ordinal, position, owner, target and opening revision. An identical retry or process
restart after reading the same persisted visible history returns byte-identical IDs;
a later reopening rotates both. A different seat, visible hex, public policy or ordinal
rotates both. Private facts, hidden opponent bindings, event count and Source/Proof
hashes are excluded. The golden fixture contains two seats, same-position reopening,
ordinal change, restart, and an illustrative hidden-only fork. Its fork is **synthetic**,
not the required authentic equal-visible R03 pair.

## Retained private binding and rejection

Only Umpire may retain `{campaign,authenticatedSeat,ownerDecisionRef,
openingRevision,ownerCandidateRef,privateCandidateId,frozenCombatBase,
permittedBookkeepingSuffix}`. It must reject duplicate aliases or ambiguous mappings.
Alias existence is not authorization. On every proposed submission, authenticate seat
before lookup, validate exact offered decision/action membership, opening revision and
approved owner-visible fields, then re-authenticate the frozen combat base and only the
permitted bookkeeping suffix. An altered combat fact must terminate the visible offer
or reopen with a new revision before another submission; hidden-only bookkeeping may
not silently make equal-visible histories accept differently.

Unauthenticated requests never reach alias lookup. For authenticated requests,
`invalid-request` is reserved for syntax/codec failure, `stale-view` requires an
actual owner-visible revision change, and hidden-dependent non-membership, profile
failure or no-longer-admissible state uses one generic
`not-current-or-not-admissible` rejection with no blocker or private identity. An
unchanged visible revision cannot become stale from opponent-only bookkeeping.
Timing and traffic equivalence require separate review. Existing private denial
bytes remain the only outward P01 output until approval and implementation.

## Remaining hard gates

1. Owner approves the exact visible fields, source of public rules/config/policy refs,
   revision lifecycle, codec domains/prefixes, retained mapping scope, and rejection
   classes. These are not approved by merging this proposal.
2. Authenticate **two independent CreationRequest plus eleven-ledger R03 histories**
   with equal approved visible projection and differing hidden bindings. Verify
   byte-equal output, order, IDs, window lifecycle, acceptance and rejection for
   both histories, including post-opening hidden bookkeeping. Synthetic twins do
   not substitute. If legality differs, fail closed before the offer exists.
3. Independently review a versioned runtime observation/submission contract and
   host/transport behavior, including restart identity, exact retry, privacy and
   timing. No public/native issue or P01 admission precedes the full support
   certificate and separate P02/P03 gates.

Failure of any gate leaves AC-007 open and all P01 assessments unsupported.
