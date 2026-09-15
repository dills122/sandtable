#004A3 implementation source map

Read-only task004_evidence; no edits/tests/new review pass. CCE unavailable, known-file fallback.

##003 normalized terminals

verify-combat-authority-composition-v1.py sources():425 returns28 items across9 families;
source():232 yields current world/randomState/cycle/position/arms/receipts/root/terminalData.
build_snapshot():438 retains creation/header bindings; build_composition():602 and
read_composition():668 reconstruct exact handoff; check_fixture():751 verifies31pins/goldens.
Reuse normalized current item.world, not stale pre-release root.world. Cycle-control has
sourceCycle/activeCycle; finish activeCycle null, retain source occurrence and terminal position.
Private receipt count/order never becomes side revision/history.

## Live inherited release

verify-combat-inherited-reserve-release-v1.py derive_base159/read_base192/read_control294.
trace(case) -> base,parentTrace,states,inputs,events; root=parentTrace[2][-1].base;
releaseState=states[cut].release; currentWorld=release.project_world(root.world,base.releaseBase,state).
Use authenticated inherited transition251/apply301 after release.command and release.trusted.
Wrapper restricts owner choice to release-I despite underlying choices(I,1) including conversion.
Other arms remain standalone ledger evidence unless a supported authority successor is separately
scoped. Root requested clarification on explicit full-legal-set freeze requirements before assignment.
Own receipts only actual accepted owner dispositions; System fallback has no owner receipt.

## Live inherited control

verify-combat-inherited-cycle-control-v1.py derived(actor),trace(actor,repeat|finish).
Bundle[0] is base; state.world current. authenticate146/read_base152/read_state199 preserve exact
release+armed predecessor. Command via cyc.command(base.controlBase,state,choice),cyc.trusted,
then inherited transition186 with bundle. assessment156 supplies certified armed continuation;
ordinary cyc.assess is wrong ammo-zero adapter. Module temporarily overrides assessment: no
concurrent transitions using shared module instance. State has exact World/members/ordinal/expiry.

## Own Reserve facts

ReleaseState.members, CycleControlState.members or normalized arms.members: own public unit ref,
status/baseCpa/spentCp; history releasedType/releaseCycle/cpaBasis/voluntaryCeiling;
offensiveCommitmentId presence -> own used flag; nextMovement scope/ordinal/status.
Never copy private designation/conversion/release/completion IDs or invent absent history.

## Separate ledger evidence

Reserve13: base_for/initial/choices/command/trusted/transition.
Cycle19: base_for/assess/control_mode/command/trusted/transition.
Synthetic member provenance/World hashes do not qualify as fullWorld/current registry sources.
Prove codec/order/fallback/stale/retry/progress arms separately. complete-release is owner command,
not System complete. Frozen binary public cycle action/action-set framing required.

## Suggested registry tags

inherited-reserve and inherited-control authenticate retained base/input/event prefixes;
task003-terminal is projection-only reconstructed traceId. Ledger tests outside registry.
Historical003 evidence is independent of legacy C3 sealed profiles: explicit registry dispatch,
not blanket corrected-clock family check. Preserve A1/Result2 config references. Authenticate
name/tag/base/inputs/events and source pins before caches/declassification; terminal hash insufficient.
Projected revision/history comes from represented authorized cuts, never hidden receipt/event count.

## Scope conclusion

Bounded split satisfies current004 evidence scope (plan65,158..161), provided ledger tests execute
actual full canonical sets and transitions, not codec recognition only. Existing inherited spec19..36,
119..125 and plan1282..1291 explicitly retain release-I-only positive profile and defer alternatives.
Canonical design108..127/AC003 still requires release/conversion and laterII release/retain/completion.
Public capability identity must distinguish limited inherited admission from canonical legal set.
Do not bypass wrapper transition through bare kernel to pretend owner conversion is inherited support.
