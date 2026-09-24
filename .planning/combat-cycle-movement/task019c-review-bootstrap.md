# Fresh Review Bootstrap

Review instance: 1 of 3.

## Objective and scope
Review native Task019C inherited guarded repeat/finish against frozen3k for both actual owners.
Repository `/Users/dsteele/repos/sandtable`, branch `codex/combat-guarded-repeat-finish`,
base/HEAD `5a8bc416698c21c8760fbd54a77d915ab9b3d9d0`. Review working-tree delta, including three new
engine/codec/test files below. Five primary files include Core.Tests.csproj and canonical plan.
README, tech-design, naming-overview, roadmap, delivery plan and Task019C evidence are administrative.
Exclude unrelated `.serena/` and `docs/work/handoffs/2026-09-20-combat-delivery-stop.md`.

## Requirements
Read `docs/specs/combat-inherited-cycle-control-v1.md`, its schema/fixture/oracle, underlying
`combat-cycle-control-v1` semantics and Task019C dispatch in canonical implementation plan.
Predecessors are017C Release and019B armed proof; no frozen files or existing APIs changed.

## Verification
Initial compiled RED: four NotImplementedException failures. Initial/golden four tests passed.
Expanded twelve cases passed in49.963s. Current pipeline formats, runs focused tests/build/full
suite; logs `/tmp/task019c-{format,focused,build,full}.log`. Four direct oracle logs under
`/tmp/task019c-{inherited-cycle-control,armed-continuation,inherited-release,cycle-control}.log`.
Full suite may still run. Do not build/test concurrently or edit files.

## Independent workflow
Read code/tests/specs first and record preliminary concerns before reading
`.planning/combat-cycle-movement/task019c-author.md`. Read-only review; no additional agents.
Review implementation and plan, report findings with evidence and verdict. No fixes or new streams.

## Frozen source/project hashes
- `src/Cna.Core/Campaigns/CampaignCombatInheritedCycleControl.cs`: `783fe55a141fb977005df699d62fe83ea186f2964dd4273e6c792d6c3896e3a4`
- `src/Cna.Core/Campaigns/CampaignCombatInheritedCycleControlCodec.cs`: `3d914c5bbb83c968eea08f3c952509879efab934892e4b600db080a8fc37d4ab`
- `tests/Cna.Core.Tests/Campaigns/CombatInheritedCycleControlTests.cs`: `9690d80cd5a402df747630c19dbb31806b4bdee3b825c0b0a8c04597e42b00d0`
- `tests/Cna.Core.Tests/Cna.Core.Tests.csproj`: `d3e9a776d099c5fed185c86af290a6f088f6afd15df3234a1b53738c3b8bf237`
