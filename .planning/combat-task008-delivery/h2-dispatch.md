# H2 Movement/Breakdown history routing

Base64cd011 (H1 code d684be8, acceptance metadata only afterward). H1 dev/three independent
rounds/local and exact-head remote gates passed. Existing main-based draft PR136.

Worker h2_movement_history owns four primary paths: CampaignCombatHistoryReplay.cs,
CampaignCombatHistoryModels.cs, CombatHistoryReplayTests.cs, new CombatMovementHistoryReplayTests.cs.
See h2-candidate-scope.md for observable scope and exclusions. Root owns docs, integration,
full gate, dev review and three sequential fresh-context independent reviews before H3.
No predecessor production/schema/oracle/fixture mutation. H1 retention ownership unchanged.

TDD focused command uses nativeMTP --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj,
--filter-class '*HistoryReplayTests', --no-restore, unique -bl:/tmp/h2-focused-{}.binlog.
Escalated local IPC and login:false. Worker finishes all processes before handing back.

Root confirms H0 family names E1 (16 vectors), E2G1 (32), G2 (16). Count overlap/shared cuts
by exact creation hash and ordered event hashes; do not equate 64 vectors with64unique histories.
Actual first opening must be retained for prefixes; Movement requires Normal/NONE and cannot
be instantiated prematurely for otherwise valid H1 histories. F1 shares Move4 grammar but actual
E1 effect validation rejects positive Reaction. Full literal root and disabled admission remainH4.

Initial broad filter with two wildcards selectedzero tests/exit5; corrected single-wildcard filter.
Root H0 inventory64vectors/48uniquehistories,2sharedwithH1,46new; cumulativeunion254.
