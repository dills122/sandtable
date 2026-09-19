# Native Result2 to same-slot cycle finish

Status: W01-CLOCK-CYCLE **accepted bounded private contract**. Acceptance tracked in
[governing plan](../design/combat-cycle-implementation-plan.md);
[source audit](../../.planning/2026-09-14-overnight-combat-wave-01/source-audit-result-cycle-bridge.md)
records its authority limits. Parent004/B,005 and runtime remain incomplete.

## Contract and authority

Version1 [ordered schema](combat-result-cycle-finish-v1.schema.json) wraps exact native Result2,
ReserveRelease1 and CycleControl1 bytes. `native` is a canonical UTF-8 JSON string, not an open
object: its named native reader authenticates closed shape, types, values and replay. No historical
constructor or reader which reconstructs Result1 participates. Existing kernels stay immutable.

`BridgeBase` binds source policy `sandtable.combat.native-result2-cycle-finish.v1`, full source
lineage and synthetic Movement certificate. Bridge ID hashes canonical base with its own domain;
wrapper receipt hashes its canonical event without receipt, with separate receipt domain. Native
events retain native authority versions, prefixes and receipts; wrapping adds no authority turn.

Source names one of32 pinned Result2 cases: eight branches × two acting sides × two seal orders.
Exact pre-combat boundary, step history, RoundState2 and seals authenticate through Result2/Round2
readers and supported-case context. Full Result2 suffix and final state replay before admission.
Require closed status, CA completion, no immediate settlement work and no pending owner window.
Alternative valid Result2 acceptance times remain admissible; full audit is retained in source.
Source command kinds, choices and actors match named branch; unsupported source fallback traces
are outside this bridge. After native replay, each accepted `choose` event must retain owner author,
`owner-choice` reason and payload kind equal to submitted choice. All source clock/controller/deadline
fallback effects reject, including fallback which leaves World identical to named branch.
Exact accepted source bytes key authentication caches; cached state is
copied before exposure to callers.

Certificate policy `sandtable.combat.synthetic-pre-retreat-movement.v1`, provenance `synthetic`,
receipt `probe.result2.movement-completed`, exact boundary World hash, pre-retreat end locations,
cycle scope/ordinal and empty prior exclusions are bound byte-for-byte. This is synthetic evidence,
never a creation-rooted Movement completion receipt. Post-retreat locations cannot replace it.

## Native derivation and sequence

1. Derive `settled-empty-release` ReleaseBase from exact Result2 cycle/version/prefix/CA receipt,
   World hash, RNG, committed attack history and actual acting member. Require member Reserve
   status `none`; retain exact spent CP, pinned CPA10 and synthetic empty Reserve history.
   Empty means no pending release work, not zero members. New scope `acceptedHighWater=null`;
   original Result2 state and audit remain immutable in source.
2. System Reserve `open` then `complete`, both without clock or decision. Native World projection
   equals settled World byte-for-byte. No owner release command is admitted.
3. Derive `settled-control` CycleControlBase from authenticated release inputs/events and exact
   settled World, certificate, actual Round2 attack-commitment receipt/hash and target uses.
4. System cycle `open` independently supplies its opening time. Native assessment determines
   owner-choice or certified automatic finish. Owner `finish` or existing System clock/controller
   fallback reaches exact same-slot TruckConvoy position, active cycle null. No repeat admitted.

All suffix stages retain World/RNG/history/target uses; guards, replacement entitlements and future
obligations survive without guard action, upkeep, training, maturity or stage housekeeping. Future
obligation presence is permitted; unclosed immediate settlement work is rejected. No fullSnapshot,
public candidates, scheduler or runtime registration is allocated.

## Readers and limits

Base admission derives and compares every certificate/base field before kernel execution. State
and event readers replay complete bounded suffix under authenticated base; snapshots cannot assert
authority from caller-provided native state. Retry same command/actor returns original wrapper
receipt without event/version/RNG changes. Native stale timer no-ops remain no-ops. Wrong actor,
stage, version, identity, source, receipt or canonical bytes reject; repeat rejects before execution.
Canonical JSON rejects unknown/missing/duplicate properties, reordered fields, floats for integers,
nonfinite numbers, BOM, trailing bytes and oversized/deep frames. Bounds:1MiB/frame, depth32,
512 array items,32 Result2 events,4 suffix events; native tighter bounds also apply.

## Traceability and verification

| Requirement | Verification | Evidence |
| --- | --- | --- |
| Native lineage/certificate admission | supported-source and tamper groups | [oracle](verify-combat-result-cycle-finish-v1.py) |
| All32 combat-then-finish continuations | exact suffix, five replay cuts, retry groups | [fixture](fixtures/combat-result-cycle-finish-v1.json) |
| State retention and same-slot successor | canonical World/RNG/history/target-use equality | oracle and fixture |
| Independent cycle clock and native fallback | prior-time pairs, clock loss/deadline/bounds | oracle |
| Immutable historical kernels | literal source SHA256 pins | fixture |

Run `python3 -B docs/specs/verify-combat-result-cycle-finish-v1.py`. Frozen fixture is required;
normal verification never creates or updates evidence. Fresh review/root gate precede acceptance.

Retained evidence:32 owner-choice traces,160 replay cuts,128 retries,8 guard and8 entitlement
lineages;192 independent-opening comparisons across all32 sources, including24 changed source
audits. Eight no-mandatory-window sources have no accepted owner time to perturb.14 literal source
pins cover Result2/Round2 and immutable Release/Cycle/Movement kernels; side adapters are consumers,
not bridge dependencies. Source World bytes live in pinned Result2 fixture; bridge fixture retains
source/base/state hashes, exact certificate/release base and full four-event suffix.

Author TDD first failed on absent bridge API and frozen-evidence reader. Focused regressions also
failed on mutable cached authority and noncanonical structured-input receipt hashing; both fixed
before freeze. Serialized raw input still requires canonical property order; structured objects
normalize before identity/receipt hashing. No production implementation or full-snapshot claim.

Fresh-review regression reproduced16 authenticated custody-choice clock faults wrongly admitted
under guard/escape branch names;8 escape forks preserved identical World. Semantic source admission
now rejects those16 plus8 authenticated opening faults; timestamp-only retiming remains admissible.
