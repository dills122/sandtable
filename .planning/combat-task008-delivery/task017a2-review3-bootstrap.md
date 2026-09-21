# Fresh Review Bootstrap

Review instance: 3 of3. Frozen candidate; fresh context required.

## Objective And Repository
Independent implementation and plan review of Task017A2 isolated native Reserve Release lifecycle.
Integration /Users/dsteele/.codex/worktrees/8fcc/sandtable. Create own clean detached clone under /tmp.
Base32a038c322c561f642af2aedafe37165147f4939; candidatea4b4f630d1250fd00d61ebae486f18813ffedd01.
Ancestry-only rebase from3811202: candidate tree unchanged; previous base tree equals new base tree.
Original /Users/dsteele/repos/sandtable working tree remains read-only. No presumption of readiness.

## Scope And Canonical Requirements
Four source/test files: CampaignCombatReserveReleaseModels.cs, CampaignCombatReserveReleaseCodec.cs,
CampaignCombatReserveRelease.cs under src/Cna.Core/Campaigns; CombatReserveReleaseTests.cs under
tests/Cna.Core.Tests/Campaigns. Also root plan docs/design/combat-cycle-implementation-plan.md,
README/tech-design/naming-overview/roadmap and dispatch/author records. Exact diff from base.
Read AGENTS, canonical Task017 refinement, frozen docs/specs/combat-reserve-release-v1.md/schema/
fixture/oracle and relevant cycle/configuration contracts. Inspect plan/tests before implementation.
Do not read task017a2-author.md or prior reports/check/dev material until preliminary ledger written.

44 isolated traces target132event hashes/176state hashes+lengths/two literal terminal events.
Four historical Result1 cases excluded. A1 initial-base validation preserved; lifecycle state is
replay-derived. Separately trusted expected base/request and admitted inputs/events are required.
No World projection, native settled adapter, positive provenance, public admission, Movement,
cycle control or parent017 acceptance. Snapshot/durable publication outside scope.

## Independence And Ownership
Use independent-review skill in reviewer mode, fresh blind first pass. NO CCE/context_search/
session_recall/memory/prior-review access; explicit restriction supersedes retrieval guidance.
No source edits, commits, subagents or additional review instances/workstreams. Not alone; preserve
concurrent integration work. Own only task017a2-review3-preliminary.md and task017a2-review3-report.md
in integration planning directory; logs/build outputs in own clone/tmp. Read-only review otherwise.

## Verification And Return
Run proportionate .NET checks using MTP --project/--solution; login:false. Approved restore/IPC
escalation as needed. Repeated --filter-class arguments; Boundary=UserSpace is a trait on Core tests.
This reviewer MUST independently restore and rebuild the FULL solution in clean clone, then run
Release focused tests and Boundary=UserSpace. Use dotnet build Sandtable.slnx --no-restore
(or explicit Rebuild); no reliance on integration artifacts. Full solution test suite stays root-owned. No global build-server shutdown; close owned
commands before returning. Persist preliminary ledger BEFORE author packet, then reconcile claims,
review implementation AND plan, report exact checks and one evidence-backed verdict. Final report
must include review instance3of3 and no overclaim of authentic campaign lineage.
Author packet: integration .planning/combat-task008-delivery/task017a2-author.md.
