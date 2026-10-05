# Fresh Review Bootstrap

## Review Objective

Review CMB-019D3 native Result2 settled control against the accepted019D2 contract and N1 overnight plan. Review implementation and plan independently. Review instance: coordinator-assigned set1/pass1 of3 within the user-approved milestone state machine; coordinator owns the count. Reviewer GPT-6.1-sol medium, fresh chat with no author history. Read this bootstrap first; record preliminary findings before opening the separate author explanation.

## Repository And Worktree

`/Users/dsteele/.codex/worktrees/native-settled-control/sandtable`. Primary checkout must not be edited. Work read-only. AGENTS instructions are in primary `/Users/dsteele/repos/sandtable/AGENTS.md`; worktree AGENTS agrees. Activate exact Serena worktree, use a unique Codebase Memory project and coverage/source fallback.

## Base, Head, Branch, And Dirty State

Base/head `907f41403ed159d65024f193f3e1f730a23b9bbb` on `codex/native-settled-control`; explicit working-tree review boundary. Implementation is uncommitted, no in-scope commits yet. `.serena/` is untracked tool-generated configuration excluded from review/publication. Review artifacts themselves and the later handoff are administrative only. Do not stage or commit to make the target cleaner. Hashes are refreshed when final gates complete.

## In-Scope Commits And Paths

Five primary files frozen before semantic RED:
- `src/Cna.Core/Campaigns/CampaignCombatSettledControl.cs`
- `src/Cna.Core/Campaigns/CampaignCombatSettledControlCodec.cs`
- `tests/Cna.Core.Tests/Campaigns/CombatSettledControlTests.cs`
- `tests/Cna.Core.Tests/Cna.Core.Tests.csproj`
- `docs/design/combat-cycle-implementation-plan.md`

Administrative synchronized status paths:
- `README.md`
- `tech-design.md`
- `naming-overview.md`
- `docs/roadmap/pre-alpha-roadmap.md`
- `docs/design/combat-cycle-movement-delivery-plan.md`

## Canonical Requirements And Plan

`docs/specs/combat-settled-control-v1.md`, its ordered schema, frozen fixture and oracle; unchanged settled-continuation019D0 spec/schema/fixture/oracle and native019D1 implementation; implementation plan Task019D3; primary ignored `.planning/combat-overnight/session-plan.md` N1 execution limits. Contract and source fixtures are evidence, never runtime authority.

## Explicit Exclusions

No changed old frozen spec/schema/fixture/oracle, shared runtime refactor, public/Snapshot/transport activation, actual positive-history entry, ordinary repeated Movement execution, Convoy execution/cost, later-II/consumed lineage or parent017–019 closure. Earlier trust remains synthetic-pre-combat. No reviewer subdispatch or fixes. Coordinator controls full-suite lease and publication.

## Verification Commands Available To Reviewer

`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatSettledControlTests' -bl:/private/tmp/cmb019d3-review-<unique>.binlog`

`python3 -B docs/specs/verify-combat-settled-control-v1.py`

`just check` only with coordinator full-suite lease; final author logs `/private/tmp/cmb019d3-final-check.log`, focused logs `/private/tmp/cmb019d3-final-focused.log`, oracle logs `/private/tmp/cmb019d3-oracle-control.log` and `/private/tmp/cmb019d3-upstream-oracles.log`. Missing logs/checks must not be treated as passed.

## Author Explanation Location Or Delivery Step

After recording preliminary findings, read `docs/work/reviews/2026-10-05-native-settled-control-author.md`. Use independent-review in reviewer mode, verify claims against exact scope, return evidence-backed verdict to coordinator. Do not implement fixes, spawn further reviewers or split into new workstreams.

## Frozen Implementation Hashes

Review target frozen for coordinator dispatch; final gate running, verdict held until completion.

- `src/Cna.Core/Campaigns/CampaignCombatSettledControl.cs`: `8b167d88c07c8506e0b43e571c81117209747e0c2d7721b5135e0c91265793d1`
- `src/Cna.Core/Campaigns/CampaignCombatSettledControlCodec.cs`: `792707d1bf72214d209ab8a609e384ec02e123e9aeceaeae907246c9df827832`
- `tests/Cna.Core.Tests/Campaigns/CombatSettledControlTests.cs`: `799c02435c69c760b20d42243a31f54d33363b56ce2e6de6e481c45b4a5827e6`
- `tests/Cna.Core.Tests/Cna.Core.Tests.csproj`: `37fa69bd8e923e356b27d5024c1c3769e185f086ea52aae1de71b95f1d6e926b`
- `docs/design/combat-cycle-implementation-plan.md`: `08b70421d8998ba20694b99d5444878def27a02d236ebcc291c9aec25bbee774`
- `README.md`: `afdece8498391a5d094aae42789f1d2b66d1154f8fcbcb2db3cd39102e92e266`
- `tech-design.md`: `95041bcd262aad157cbd0fb422355272d4c60a2684f5798b8b2965cdc2273eb4`
- `naming-overview.md`: `9f4ade6f8dd94c886703c035bcaf822922c4b0e6e5c1f4cbe56e269b1ae73113`
- `docs/roadmap/pre-alpha-roadmap.md`: `04692da03a988851ea389e7d93c15b74e21d0de1c5dbc81694a0e23ce01a7d3d`
- `docs/design/combat-cycle-movement-delivery-plan.md`: `4dc5434297029c7de4bccda5e160e5ec5f3fe8ac953dc72974c454b62f348f38`

## Post-Review Metadata

Ready set1/pass1,total1of9,max3per set, no findings. Final exact-byte gate completed
2512/81pass; lease released. Frozen hash table above remains the review boundary.
Only canonical-plan execution status and administrative gate/review/publication
records are reconciled afterward; behavior/test/project bytes do not change.
