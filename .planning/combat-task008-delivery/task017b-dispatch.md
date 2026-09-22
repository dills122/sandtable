# Task017B dispatch — 2026-09-22

User authorizes next Combat-to-Release slice with same orchestration/review flow and PR/branch reuse.
Start14:19UTC; target75–90minutes. Clean base201395c3e3faf06735424a8bc70658a2af75f081.
Branch codex/combat-reserve-release-lifecycle; PR139 open/draft/mergeable, all8final A2checks passed.
Main32a038c unchanged. Original /Users/dsteele/repos/sandtable working tree remains read-only.

## Exact primary manifest
- src/Cna.Core/Campaigns/CampaignCombatResultRelease.cs (new adapter and owned source/projection models)
- src/Cna.Core/Campaigns/CampaignCombatReserveReleaseCodec.cs (narrow settled-empty-release profile shape)
- tests/Cna.Core.Tests/Campaigns/CombatResultReleaseTests.cs (new)
- tests/Cna.Core.Tests/Cna.Core.Tests.csproj (existing frozen fixture link only)
- docs/design/combat-cycle-implementation-plan.md (root plan)

Administrative README/tech-design/naming/roadmap, Task017B dispatch/audit/author/check/dev/review/
evidence/source-pin files and handoff additionally counted. No fixture/schema/oracle changes.

## Behavior and proof
Full Created/independently trusted positive boundary/Selection/Round2/Result2 tuple replay before
Release derivation. Admit exactly frozen32selected source contexts with reliable owner-choice
semantics; reject valid fallback sources even if World unchanged. Permit reliable Result2 timestamp
variation. Derive exact settled-empty-release native base, complete own membership, CPA/spent CP,
World hash/RNG/attack history, same-slot Release position and actual CA receipt; new high-water null.
Replay native explicit System open/complete only; no owner reserve action or cycle advancement.
Readback must independently replay source and Release suffix, never accept caller state/base hash.

32native base literals and64native event literals,96native replay cuts without claiming frozen native
state hashes from wrapper-domain cuts. Retain authentic Result2 ancestry but explicit synthetic
pre-Combat boundary provenance; not creation-rooted positive gameplay. Preserve World/resources,
relationships/custody/entitlements/future duties, RNG and stage attack history byte-for-byte.
Parent017/positive3h/3i/Movement/cycle control/public admission/Snapshot/publication stay open.

Meaningful compiling behavioral RED required. Root development review, full solution build/test,
Boundary/format, exact source-candidate CI and three fresh sequential independent reviews; third
independent full solution rebuild. Reviewer reports returned/read before next reviewer starts.
Independent contract audit is research, not one of three reviews. Freeze candidate before reviews.
