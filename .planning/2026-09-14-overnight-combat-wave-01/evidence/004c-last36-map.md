Static mapping only; no oracle runs. `H` = historical result/round lineage. `P` = partial contract evidence. Runtime task numbers mean `CMB-TASK-NNN`; hosted durability/Dispatch remains separately deferred.

Evidence aliases resolve spec, oracle, and same-stem fixture:

- `R1`: [combat-result-settlement-v1](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-result-settlement-v1.py)
- `Q1`: [combat-sealed-round-v1](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-sealed-round-v1.py)
- `Q2`: [combat-sealed-round-v2](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-sealed-round-v2.py)
- `W`: [combat-world-settlement-v1](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-world-settlement-v1.py)
- `I`: [combat-rules-inputs-v1](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-rules-inputs-v1.py)
- `S`: [combat-selection-steps-v1](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-selection-steps-v1.py)
- `N`: [combat-snapshot-composition-v1](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-snapshot-composition-v1.py)
- `L`: [combat-reserve-release-v1](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-reserve-release-v1.py)
- `C`: [combat-cycle-control-v1](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-cycle-control-v1.py)
- `IC`: [combat-inherited-cycle-control-v1](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-inherited-cycle-control-v1.py)
- `IM`: [combat-inherited-reserve-movement-completion-v1](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-inherited-reserve-movement-completion-v1.py)
- `SEQ`: [combat-cycle-sequence-v1](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-cycle-sequence-v1.py)
- `COMP`: [combat-authority-composition-v1](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-authority-composition-v1.py)
- `SRC`: [source normalization](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/research/verify-combat-source-freeze.py)
- `RNG`: [research RNG vectors](/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/research/verify-combat-rng.py)

```tsv
id	evidence_function_or_trace	contract_status_and_pending	runtime_owners
CMB-RES-AC-001	Q1.main:432-443/resource_boundary_base;S.boundary/candidate;I.main	P:threshold/resource contracts;corrected result/profile integration and A2 pending;no complete outward certification	006,009,012
CMB-RES-AC-002	Q1.main:369-397/attacker-first,defender-first;Q2.test_replay_tamper	H+Q2:single commit event/cost/replay model;publication-failure injection absent	012
CMB-RES-AC-003	S.main/no-selection branches;Q2.test_expiry_recovery;C.trace/exhausted-ammo-still-moves	P:no-charge cancellation/idempotent replay/no-ammo continuation;A2/A3 bridges pending	012
CMB-RES-AC-004	Q1.main:428-430;R1.main:348-353/mirrored attackerSide and reversed seals	H:gameplay/role-order/replay evidence;corrected Result2 integration pending	012,013
CMB-RES-AC-005	R1.resolve;R1.projected_world:133-134;W.main:327-340	H/P:frozen preloss and exhaustive arithmetic;explicit casualty-order permutation assertion absent;Result2 pending	005,013
CMB-RES-AC-006	I.main/RulesInput golden and negativeVectors;SRC.main:136-168	covered:36 Morale/360 normalized loss cells;C# artifact/table parity pending005	005
CMB-RES-AC-007	RNG.VECTORS;R1.behavioral/ordinary,refusal-loss-dp,zero-engaged,defender-capture-guard,attacker-capture-guard-cp-limit	H:seeded numerical and settlement traces;corrected Result2 and A2 pending	005,013,014
CMB-RES-AC-008	R1.checkpoint_checks:295-298;R1.resolve;RNG.VECTORS	H:role purposes/die/cursor readback and research rejection/block crossing;Result2 pending	013
CMB-RES-AC-009	Q2.test_replay_tamper,test_expiry_recovery;R1.checkpoint_checks;R1.main:354-355;N.compose_tests	H/P:every-cut replay and pure overflow no-mutation;durable restart/publication-failure atomicity unproved	008,012,013
CMB-RES-AC-010	R1.transition,checkpoint_checks/zero-retreat,zero-engaged,refusal-loss-dp,capture cases	H:pending settlement and closure guards;Result2/A2 pending	013,014,016
CMB-RES-AC-011	S.main/accepted-decline,no-attack traces;Q1.behavioral_checks;Q2.test_replay_tamper	P:empty ordered stages and closed singleton admission;broader stages/second assault remain excluded	006,009,010
CMB-RES-AC-012	R1.checkpoint_checks;N.compose_tests	H/P:authority cost/base/result/pending tamper rejection;settlement side equality and no-raw-receipt proof awaitA2/B	008,020
CMB-SET-AC-001	W.main:327-340;W.result_facts,world_at;R1.behavioral/all8 cases	H/P:8840 numerical combinations plus certified synthetic route branches;full side admission awaitsA2;unsupported geometry remains rejected	006,009,014,015
CMB-SET-AC-002	SRC.main:157-163,172-190;W.main:327-340;R1.behavioral/refusal-loss-dp	H:refusal-before-rounding/conservation;Result2/A2 pending	005,014
CMB-SET-AC-003	W.assert_conservation:274-288;R1.behavioral/attacker-capture-guard-cp-limit	H:separate guard transfer and loss/capture accounting;Result2/A2 pending	014,015
CMB-SET-AC-004	W.world_at:170-206;R1.behavioral/attacker-capture-guard-cp-limit,refusal-loss-dp	H:retreat CP/DP and retained cause sequence;Result2/A2 pending	014,016
CMB-SET-AC-005	R1.checkpoint_checks:279-284;R1.timing_checks:330-340;R1.trace_case/zero-engaged	H:no required window plus exact retry/stale timer model;corrected Result2/A2 pending;real lost-reply delivery deferred	014
CMB-SET-AC-006	R1.behavioral/defender-capture-guard,defender-capture-escape,attacker-capture-guard-cp-limit,attacker-capture-escape;W.world_at:210-237	H:both-role custody routes/guard/escape;Result2/A2 pending	015
CMB-SET-AC-007	W.assert_conservation;W.main/calendarVectors;R1.behavioral/*capture-escape,*capture-guard*	H:entitlement/upkeep retained without TOE restoration;Result2/A2 pending;upkeep/maturity execution excluded	015
CMB-SET-AC-008	R1.timing_checks:309-342	H only:historical deadline/fallback/retry checks;Result2 independent window-opening clock correction active and unaccepted;A2 pending	014,015
CMB-SET-AC-009	R1.behavioral/zero-engaged,zero-retreat,refusal-loss-dp,*capture-guard*;W.world_at:239-247	H/P:selected original-participant relation reconstruction;Result2/A2 pending;new-arrival extension not certified	016
CMB-SET-AC-010	R1.checkpoint_checks;N.compose_tests;W.main/negativeVectors	H:all retained event/state cuts and field/order tamper;corrected Result2 persistence composition pending	008,014,015,016
CMB-SET-AC-011	none for settlement side projections;R1/W provide private inputs only	pending:A2 settlement side histories/candidates/outcomes;B Runner/Dispatch/War Diary evidence;A1 does not cover this row	020,021,023
CMB-SET-AC-012	R1.behavioral and transition closure guards;L.checkpoint_checks/settled-guard-empty,settled-escape-empty	H/P:closed settlement retains future work;corrected Result2/A2/A3 bridge pending;repeat/runtime readiness not implied	016
CYCLE-COMP-AC-001	SEQ.main;IC.verify_case/axis-repeat,axis-finish,commonwealth-repeat,commonwealth-finish;COMP.verify/cycle-control	covered selected same-slot authority;A3 side projection and B occurrence evidence pending;other-slot execution excluded	018,019
CYCLE-COMP-AC-002	SEQ.main/identity,prefix,occurrence vectors;C.boundary_checks:451-455;IC.verify_case	P:authority framing/self-hash avoidance covered;cycle hidden-fork public action equality awaitsA3	008,019,020
CYCLE-COMP-AC-003	L.checkpoint_checks/first-release,first-convert,later-release-retain,later-complete-intent,empty;L.boundary_checks:413-429	covered private membership/disposition contract;A3 projection pending	017
CYCLE-COMP-AC-004	L.timing_checks:367-410/first-timeout-midway;L.checkpoint_checks	P:single-budget ordered fallback/retry model;corrected public-profile bridge awaitsA3;durable restart deferred	017
CYCLE-COMP-AC-005	L.boundary_checks:430-457;C.trace/later-release-cumulative-ceiling,mandatory-overspend-retained	P:cumulative CPA9/II ceiling and commitment history covered;released-II offensive debit/DP-before-Morale execution extension-gated	007,012,017,018
CYCLE-COMP-AC-006	IM.verify_case/axis,commonwealth;C.boundary_checks:456-475;C.trace/first-release-finish-expiry,prior-exclusion-finish	covered accepted completion-bound expiry/proximity history;A3 projected continuity pending	018,019
CYCLE-COMP-AC-007	C.verification/control_mode truth table;C.trace/engaged-repeat,later-release-insufficient-cp,no-progress-finish;C.boundary_checks:425-427,476-485	P:continuation proof/unsupported distinction and216 CP coordinates;actual move atomicity and A3/B pending	018,019
CYCLE-COMP-AC-008	C.boundary_checks:429-431;C.verification:516-518;C.trace/no-progress-finish,later-retain-not-progress;COMP.verify	covered bounded progress allowlist and no-progress rejection;A3/B projected and Runner evidence pending	018,019
CYCLE-COMP-AC-009	C.trace:339-350/engaged-repeat,exhausted-ammo-still-moves,guard-finish;IC.verify_case	P:selected repeat state preservation/reset covered;settled World traces historical pending correctedA2/A3 bridge	019
CYCLE-COMP-AC-010	IC.verify_case;IM.verify_case;COMP.verify;C.checkpoint_checks,boundary_checks	P:authority suffix/full-history/reopen tamper covered;side actions/current projected checkpoints awaitA3/B	008,018,019,020
CYCLE-COMP-AC-011	SEQ.main/occurrence vectors;COMP.verify/28 authority traces	P:occurrence identity and private authority handoff only;004B child/Maneuver parity/replay/divergence contracts pending	022,023,024
CYCLE-COMP-AC-012	C.trace/guard-finish,escape-timeout;L.checkpoint_checks/settled-guard-empty,settled-escape-empty;COMP.handoff	H/P:future obligations and exclusions retained;004B terminal/step-limit/maturity failure semantics pending;004C gate readback pending	022,023
```

No row implies004C completion. Result2 acceptance must precede corrected result references; A2/A3/B remain pending.
