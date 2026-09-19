# Result2 to cycle-finish feasibility probe

Research only; not accepted bridge implementation. In-memory probe executed successfully (exit0)
before retaining this source/output summary. No further run performed while saving artifacts.
No repository contract changes, old reader reconstruction, monkeypatches or Git mutations.

Exact executed source retained at `/private/tmp/sandtable-result-cycle-feasibility.py`.
It expects repository root as working directory and was executed using `python3 -B`.

## Observed results

| Native Result2 branch | Acting side | Seal order | Mode | Witnesses | Authority | Result audit maximum | World bytes | Final cycle-state bytes |
| --- | --- | --- | --- | ---: | --- | ---: | ---: | ---: |
| defender-capture-guard | axis | attacker, defender | owner-choice | 1 | 42→46 | 10001 | 12858 | 16867 |
| defender-capture-guard | commonwealth | defender, attacker | owner-choice | 1 | 42→46 | 10001 | 12890 | 16955 |
| attacker-capture-escape | axis | attacker, defender | owner-choice | 2 | 43→47 | 11002 | 12072 | 16395 |
| attacker-capture-escape | commonwealth | defender, attacker | owner-choice | 2 | 43→47 | 11002 | 12104 | 16499 |

All four traces accepted exact four-event suffix:

```text
reserve-release-opened
reserve-release-completed
movement-combat-control-opened
movement-combat-phase-finished
```

Cycle opened at3500 and owner finished at3501, independently of retained Result2 audit maximum.
Release had no pending unit, decision or timer. Final control status was `finished`, active cycle
null, position exact same-slot `land.position.operation-1.first-player.truck-convoy-movement`.

Exact canonical World bytes, RNG, future obligations, attack history and target uses remained
unchanged. Guard cases retained one guard and `guard-priority-upkeep`; escape cases retained one
entitlement and `replacement-training-gate`. No obligation execution or maturity crossing occurred.

| Branch / side | Preserved World SHA256 |
| --- | --- |
| defender-capture-guard / axis | `c08ff9d0823c8acf21d8397e0128815a000013aebca5e7a37d7d26775f06968d` |
| defender-capture-guard / commonwealth | `4d0ff6b96197f459d58cafa75ebb3b494471a67e67e46cb1aa946e50dc98c0b4` |
| attacker-capture-escape / axis | `0355f3805665f9caecc97f2140eb1693c37ed4577d841d7162f6528028b90624` |
| attacker-capture-escape / commonwealth | `e720ff28a177fc7fc25cd7305822eb8861e74a72af1cd36b8f402b09142a16ff` |

## Exact API/field derivation

1. Load native `verify-combat-result-settlement-v2.py` and pinned
   `verify-combat-cycle-control-v1.py`; use latter's existing `rel` kernel. Imported historical
   modules remain unchanged. Never invoke their `base_for` or `read_base` to reconstruct Result1.
2. Select literal guard/escape case, set acting side and seal order, invoke Result2 `trace_case`,
   then `read_state(raw(final,'ResultState'),ctx,inputs,events)` to authenticate native lineage.
3. Copy `ctx.base.boundary.cycle`, current `final.world`, `final.randomState` and
   `ctx.committed.attackHistory`. Select actual acting unit from authenticated selected attacker
   UnitKey. Its Reserve status must be `none`; use current exact CP, CPA10 and
   `rel.empty_history(cycle)` for this certified no-Reserve synthetic profile.
4. Derive ReleaseBase with `contractVersion=1`, `profile=settled-empty-release`, exact cycle/
   firstActingSide/catalog release position, `priorVersion=final.stateVersion`,
   `priorPrefix=final.prefix`, `combatCompletionReceiptId=final.caCompletionReceiptId`,
   `retainedWorldHash=SHA256(canonical World)`, current RNG, sole actual member and attack history.
   Set `acceptedHighWater=null` for new clock scope; retain original Result2 audit in lineage.
5. `rel.validate_base` → `initial` → System `command/trusted/transition/read_event` for `open`
   and `complete`, both null time/available confidence. Retain exact inputs/events. Verify no
   pending/decision/timing and `project_world` byte equality.
6. Derive synthetic MovementEndProof: exact cycle scope/ordinal; fixed explicit
   `probe.result2.movement-completed`; `endLocations=cyc.locations(ctx.base.boundary.world)`;
   `excludedBefore=[]`. These are pre-retreat boundary locations, not final World locations.
7. Derive CycleControlBase with `contractVersion=1`, `profile=settled-control`, exact ReleaseBase,
   accepted release inputs/events, current World, above proof, actual Round2 commitment event's
   type/receipt/hash as sole progress reference, and `ctx.committed.targetUses`.
8. `cyc.assess` → `control_mode` → `initial` → `command/trusted/transition/read_event` for System
   `open`3500 and owner `finish`3501. Check position/closure, four-version advance and exact retained
   World/RNG/obligation/history equality.

## Limitations and bridge implementation obligations

Synthetic Movement-end certificate is intentionally not a creation-rooted Movement receipt.
New bridge must explicitly name, pin and authenticate that certificate plus all derived base fields;
kernel shape validation alone is not full bridge admission. This probe supplies no new fullSnapshot,
production registration, public candidate set, controller schedule or runtime support.

Four feasibility traces only: not full32 branch×side×order coverage, raw/tamper/privacy testing,
all clock faults, admission/unsupported-terminal matrix or repository full gate. This proves kernel
compatibility and observed suffix shape; it is not bridge acceptance. Historical files stay frozen.
