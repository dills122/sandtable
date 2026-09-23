# Positive Reserve Release bridge — bounded next slice

Dependency: predecessor3h implementation accepted after source gates and independent review.
Scope: Task017C from frozen combat-inherited-reserve-release-v1; both-owner3i positive release-I,
three native events, fallback/recovery and status-only World projection. Does not close parent017.

Exact five primary paths proposed:
1. src/Cna.Core/Campaigns/CampaignCombatInheritedReserveRelease.cs — full-history source wrapper,
   source-derived native ReleaseBase and wrapper Base/Control bytes, Apply/Replay/Read boundaries,
   status-only World projection and material-progress receipt. Keep tiny wrapper codec here because
   extending existing Release codec is also necessary; no independent second subsystem.
2. src/Cna.Core/Campaigns/CampaignCombatReserveReleaseCodec.cs — allow frozen inherited-reserve-cycle
   base profile with strict first-slot/first-cycle/CP0/CPA10/single held-I/history/no-clock constraints.
   Schema acceptance remains distinct from full-history adapter admission.
3. tests/Cna.Core.Tests/Campaigns/CombatInheritedReserveReleaseTests.cs — genuine predecessor generated
   through production APIs; match both frozen predecessor/base identities,6 event hashes,8 wrapper
   Control frames,2 literal terminal events. No frozen fixture generation.
4. tests/Cna.Core.Tests/Cna.Core.Tests.csproj — link retained3i fixture.
5. docs/design/combat-cycle-implementation-plan.md — owner dispatch/acceptance and parent gates.

Acceptance: replay creation through all3h events before deriving ReleaseBase; new clock starts null;
only release-I owner choice in this profile; expiry/unavailable/clock loss uses existing first-I
conversion fallback. All native prefix cuts/retries, re-signed event tampering, supplied cache/base
forgery, wrong owner/history, deadline and callback paths tested. World differs only in matched own
reserveStatus (I→none positive or I→II fallback); preserve resources, memberships and future duties.
Material progress only disposition, exact event hash/receipt. Remain ordinal1 at same Release position.

RED: actual held-I terminal exists but native profile adapter absent. GREEN: frozen native events and
wrapper states plus independent World equality/provenance/cumulative prefix evidence.
Run focused new and existing Release suites, full build/format/test gate, then fresh independent
review. Separate commit/checkpoint from predecessor. Canonical README/roadmap/tech/naming summaries
updated in administrative publication alongside retained checks/author/review artifacts.

Later-II/consumed campaign lineage depends on real repeat/Movement/history and remains open. No
synthetic conversion or later-II ledger probe may be presented as accepted actual campaign lineage.
