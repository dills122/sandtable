# Task009A — inherited assessment and identity bindings

Status: prepared; implementation starts only after root accepts H4. Base then-current H4 acceptance
checkpoint on codex/combat-task008-reaction-lifecycle; same main-based PR136. Internal subagent;
root owns integration, docs, full gates and three independent reviews. You share the codebase:
preserve other edits. Do not commit/push, delegate, activate public gameplay or start Task010.

Read first: AGENTS.md; canonical row009 and progressive evidence in
 docs/design/combat-cycle-implementation-plan.md; task009-scope-proposal.md;
 docs/specs/combat-inherited-selection-v1.md/.schema.json and corresponding verify script;
 selection-v1 Participant schema; corrected sealed-round-v2 version boundary; existing retained
history/G2/H4, CampaignCombatObligations identities and ContentPackV7Validator.
Use TDD, run-tests/platform/binlog skills; CCE first for exploration.

Outcome: actual retained Created11 plus complete ordered history yields the frozen inherited
AdmissionBoundary through existing causal replay. Require typed completed G2 authority and exact
supported six/seven-move CP12/14 cuts, both owners. Derive every assessment predicate from current
validated World/Content/Weather; unsupported histories reject, never become guessed no-work.
Bind immutable Participants using existing UnitKey/ComponentKey; bindings reflect current
representation/location/components and grant no combat eligibility. Retain full entry/ledger/prefix.

Own exactly five primary paths:
- src/Cna.Core/Campaigns/CampaignCombatIdentityModels.cs (new)
- src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs (new)
- src/Cna.Core/Campaigns/CampaignCombatCertification.cs (new)
- tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs (new)
- tests/Cna.Core.Tests/Cna.Core.Tests.csproj (existing fixture links only as needed)
No predecessor source/fixtures/contracts changes. Root owns README/tech-design/naming/canonicalplan.

Acceptance: four actual G2 terminal witnesses match literal AdmissionBoundary bytes/hash and all
assessment predicates. Full inherited state, RNG, CP/Cohesion, receipts and prefix preserved. Reuse
existing SnapshotHistories enumerators. No events, windows, selection/round/opportunity admission.
Prove missing/foreign Created/request, wrong/unfinished Breakdown, active Reaction, mutated/reordered/
shortened history, synthetic substitution and unsupported G2 cut reject. Exact Participant canonical
bytes; original unit/component identity continuity/non-aliasing and stale/foreign/duplicate binding
rejection. Original Contact/Engaged endpoints never retarget to a new occupant; probe must not claim
new profile admission. Strict raw-byte shape, bounds and defensive ownership. Don't implement unused
final-round types or generic certification duplicating Content7.

Parent009 remains open:009B owns positive all-result geometry/capacity certification, Candidate and
opportunity-v2 primitives. Final admitted opportunity remains010/011; public privacy remains020.
Actual initial inherited path is empty. Synthetic positive mechanism fixtures remain explicitly
synthetic. No claim all72ACs or full parentTask008 publication are closed.

Verification: first focused RED, then implementation and focused GREEN. Use login:false and
require_escalated for local .NET IPC; unique '-bl:/tmp/task009a-focused-{}.binlog'. Exact command
 dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore
 --filter-class '*CombatIdentityTests' '-bl:/tmp/task009a-focused-{}.binlog'.
No full suite (root owns it); do not leave running processes on return. If exact contract or five-path
scope is insufficient, report evidence before widening. Return full source manifest, exact checks,
failures/fixes, limitations and recommendations. Append worker evidence only to
.planning/combat-task008-delivery/task009a-worker-evidence.md (outside five primary source paths).
