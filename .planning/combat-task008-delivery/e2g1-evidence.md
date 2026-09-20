# E2/G1 route lifecycle verification and review evidence

Basee641bd3/E1 PR132; branch codex/combat-task008-movement-lifecycle.
Scope e2g1-scope.md. FullE1history actualstop/emptySystemresolution/Movementcompletion,8traces24events
32cuts88artifacts. Mandatorycapturedcontext/proof, no newprogress/syntheticidle. G2Combatentry remains.
Canonical baseline PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-movement-lifecycle-v1.py
exit0 /tmp/e2g1-lifecycle-baseline.log:8traces24events32cuts24retries946mutations152raw304boundaries16pins.
WorkerRED missingimplementation; analyzerCA1822/CA1861 corrected beforegreen. First12/12 lifecycle
pass, all88artifacts. Final20/20 (12lifecycle+8E1)0failed/skipped /tmp/e2g1-final.log.
Command: dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore
--filter-class '*CombatMovementLifecycleTests' --filter-class '*CombatInheritedMovementTests'
'-bl:/tmp/e2g1-final-{}.binlog'. Binlogs /tmp/e2g1-final-20260920-000246--68341--uVUHYT.binlog
and /tmp/e2g1-final-20260920-000249--68341--sFXys9-dotnet-test.binlog.
Scopedformat/diffcheck pass. Devreview complete, no remainingfindings. Added coherentcontext/progress
forgeries and outputbuffer probes beforefreeze. Fiveprimaryfiles frozen e2g1-source.sha256.
Rootbuildpassed0warnings/errors (/tmp/e2g1-build.log).
Command: dotnet build Sandtable.slnx --no-restore '-bl:/tmp/e2g1-build-{}.binlog'.
Gitdiffcheck/added-line localtargetpass. Repositoryformat exit0 (/tmp/e2g1-format.log), command dotnet format Sandtable.slnx --verify-no-changes --no-restore.
Fullsuite2032/2032pass,0failed/skipped,3m25s998ms including81boundarychecks (/tmp/e2g1-suite.log).
Command: dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/e2g1-suite-{}.binlog'; Independentround1 Ready/no findings, independently20pass; Independentround2 Ready/no findings, independently20pass; Independentround3 Ready/no findings, frozenhashes/retained20+2032verified.
Allthree accepted; no conditionalexperiment needed.
