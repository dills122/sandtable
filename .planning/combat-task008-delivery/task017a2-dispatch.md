# Task017A2 dispatch — 2026-09-21

User approved 75–90 minute Release slice. Start22:45UTC; target00:00UTC, hard stop00:15UTC.
Base a1cd425; branch codex/combat-reserve-release-lifecycle. AcceptedA1 source unchanged at dispatch.

Exact implementation manifest:
- src/Cna.Core/Campaigns/CampaignCombatReserveReleaseModels.cs
- src/Cna.Core/Campaigns/CampaignCombatReserveReleaseCodec.cs
- src/Cna.Core/Campaigns/CampaignCombatReserveRelease.cs (new)
- tests/Cna.Core.Tests/Campaigns/CombatReserveReleaseTests.cs (new)
- docs/design/combat-cycle-implementation-plan.md (root plan)

Administrative status/evidence: README.md, tech-design.md, naming-overview.md, roadmap,
this dispatch, A2 check/review/evidence/handoff records. No fixture/schema modifications.

Implement full isolated A2 per frozen combat-reserve-release-v1 contract: opening, ordered choices,
pinned timing, deterministic fallback, retry, explicit completion, canonical event/state replay.
Entry replays separately trusted inputs/events from independently retained expected base/request.
No caller state authentication, World projection, native settled adapter, cycle advancement,
public gameplay or actual positive lineage claim. Four historical Result1 rows excluded.
Proof target44 isolated traces/132event hashes/176state hashes+lengths/2terminal literals.
Meaningful behavioral RED, focused negatives, full build/test/Boundary/format, dev review,
three fresh sequential independent reviews (third rebuild), exact CI before acceptance.
If timebox ends before gates, preserve reviewed/untested distinctions and do not mark accepted.
