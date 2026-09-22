# Task017B preliminary independent review

Review instance: 1 of 3.

Frozen scope verified: base 201395c3e3faf06735424a8bc70658a2af75f081 to candidate 3c9aad3b71a1eed1f862bfdd4e286e8d8f5ffd67. Clean detached shared clone /tmp/sandtable-task017b-review1. No author explanation, worker/dev/audit/check report, CCE or other reviewer report read before this ledger. Root supplied separate in-flight check status after initial inspection; that status is not independent verification evidence.

Read applicable AGENTS, Task017 plan refinement and native result-cycle-finish contract, oracle source authentication/derivation, tests before new implementation, upstream Result2 replay and Release lifecycle/codec, and changed public documentation.

Preliminary findings: no actionable defect established. New code replays native upstream inputs/events, pins committed state and semantic source command signature, explicitly rejects accepted fallback effects, derives base from settled World, and delegates two-event Release lifecycle. Frozen base/event expectations come from existing fixture; wrapper states are not claimed as native states. Settled-empty profile validation stays separate from lifecycle state checks. Source arrays are copied; nested typed records appear immutable through existing model contracts.

Remaining checks: focused native adapter and Release tests; frozen contract oracle; author claim reconciliation. Test restore initially failed NU1900 because sandbox could not reach NuGet vulnerability service; explicit escalated restore succeeded. Focused test execution restarted with --no-restore. No full-suite claim made.

Plan review preliminary: bounded child preserves parent acceptance requirements and explicitly defers positive Reserve provenance, Movement execution, cycle control and public admission. No architecture pivot indicated. Hardcoded compatibility catalogue is appropriate to frozen synthetic profile, but future source expansion must remain separately reviewed.
