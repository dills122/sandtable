# Fresh Review Bootstrap
Review instance: 1 of 3.
Review objective: Task018B isolated ordinary Movement command/event/replay against current settled Result2 sources.
Repository /Users/dsteele/repos/sandtable; branch codex/combat-cycle-movement-plan; base and HEAD 0dfea4d. Review working-tree delta plus untracked new engine/codec/tests.
Primary scope: four files below and docs/design/combat-cycle-implementation-plan.md Task018B manifest. Administrative README, tech-design, naming-overview, roadmap, delivery-plan and review evidence updates accompany scope.
Canonical requirements: docs/specs/combat-ordinary-movement-v1.md and schema; docs/design/combat-cycle-movement-delivery-plan.md and implementation plan. Frozen historical fixtures remain unchanged.
Exclude unrelated .serena/ and docs/work/handoffs/2026-09-20-combat-delivery-stop.md; prior018A commit is baseline.
Verification: 66 focused tests pass; build clean; full suite running in /tmp/task018b-full.log. Do not run builds/tests concurrently; read logs and use static checks.
Read source/spec/tests and record preliminary concerns before reading task018b-author.md in this directory. Read-only review; no edits or extra reviewers.
Frozen SHA256 source targets:
- src/Cna.Core/Campaigns/CampaignWorldV7.cs: 22ca0dc94acb4761717a13dcaca2a7528c479139729a5046752b361cdb4614d7
- src/Cna.Core/Campaigns/CampaignCombatCycleMovement.cs: b0513d4184f7639e76b59e9edee0d84ccfb2e127b5d9ebed82e6655d9ac93450
- src/Cna.Core/Campaigns/CampaignCombatCycleMovementCodec.cs: a9d7d90a8246f4fa13ae5bc3398bd97d0865afca5e81b3903085bf88f18ccdd3
- tests/Cna.Core.Tests/Campaigns/CombatCycleMovementTests.cs: d03449d3583278cf36bb891f934dd3e20b563236127415ca26a028c191696342
