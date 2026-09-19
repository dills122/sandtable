# Task004A source audit

Agent `task004_evidence`, read-only, source audit complete; implementation unassessed.
Parent004/B open. No policy ambiguity found. Full agent return retained in task conversation.

## Source-derived coverage

| Stage | Required facts/actions | Source |
| --- | --- | --- |
| Selection | own select-close-assault/apparent target or finish-without-attack; zero candidates no decision | combat-step-transitions-v1.md:70–77 |
| Structural steps | shared authorized progress, generic waiting/closed; no player advance/private receipts | same:91–117 |
| RBA | defender-only decline, pinned deadline and own receipt, no synthetic decline | same:121–150 |
| Assignment | exact own full-close-assault participant/component, committedToe10, own sealed receipt; opposite unchanged | combat-sealed-decision-protocol-v1.md:76–144,225–230 |
| Retreat | own one-hex route/refusal/cost; zero-distance no decision | combat-settlement-disclosure-v1.md:64–78 |
| Custody | own relocate-and-guard/leave-unguarded, guard/prisoner facts; original owner escape entitlement | same:187–211; combat-world-settlement-v1.md:110–111,140–152 |
| Reserve/cycle | firstI release/convert; laterII release/retain; complete-release; repeat/finish only supported continuation/progress | continual-cycle-reserve-composition-v1.md:100–134,193–205 |

Sources above under docs/design except combat-world-settlement under docs/specs.
POL006 explicitly accepted allowlist/inferences in
`docs/design/combat-cycle-policy-reconciliation.md:27`.

## Codec and privacy checklist

- Closed property/escape/tag/type/identifier bounds; duplicate and alternate-byte rejection.
- Public hashes use approved facts only; exact authority map remains private and authenticated.
- Exact candidate payload equality; changed10→9 rejects. No authority version/prefix/hash/seed/roll,
  opposing raw identity/resources or private receipt/count in any outward artifact.
- Opposite seal preserves own bytes/revision/set/deadline and acceptance, both owners.
- Equal authorized histories preserve candidates/order/IDs/history/errors AND outcomes; generic
  errors alone insufficient. Hidden-dependent profile rejects before active decisions.
- Cycle candidate bytes exclude derived IDs, sort unsigned-byte lexicographically, reject duplicates.
  Cycle domains/tuple order frozen by continual-cycle-reserve-composition-v1.md:67–98.
- Exact duplicate reads own receipt; never accepts twice. No empty side events for private steps.
- Deadline equality expires, clock regression/restart cannot renew; stale/current map checks full
  frozen facts and allowed suffix. Unsupported state never means successful empty continuation.
- Both seal orders yield equal final gameplay/results but may retain different authority snapshots.

## Version inventory

Observation5/6/7: CampaignObservation.cs:8, CampaignObservationV6.cs:262,
CampaignObservationV7.cs:111 under src/Cna.Core/Observations.
LegalActionSet2/current policyv3/historicalv2: Actions/CampaignLegalActionSet.cs:8.
Candidate1: Actions/CampaignActionCandidate.cs:9. Submission1/receipt1:
Actions/CampaignActionSubmission.cs:15,31. Projected history2:
Observations/CampaignProjectedDecisionHistoryV2.cs:8. Existing disclosure manifest2:
docs/specs/user-space-disclosure-manifest.v2.json. No production version reservation by inference.

Exact Task003 handoff: Content7/Setup7/World7/Rules10/Created11/Snapshot12/Sequence5/cycleCodec1,
three bindings, six requirement ranges,11 exclusions, six runtime lanes,28 ordered traces and
composition digest.31 pins remain protected.28 traces omit positive assault/settlement; use frozen
positive packets too. Protobuf generic plan fields are not typed Combat support; adapter deferred.

## Executed evidence

`python3 -B docs/specs/verify-combat-authority-composition-v1.py` exit0:
28traces/9families/31pins/225readbacks/203mutations/6raw/8boundaries.
Brain independently reconciled same baseline command/result. No runtime, .NET, or004A claim.

## Limits and next action

High confidence source checklist/version inventory. Exact new fields/bounds/domain suffixes belong
to004 freeze. Next: reconcile A1 retained schema/oracle against sources and run focused/full gates.
