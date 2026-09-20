# E1 ordinary Movement verification and review evidence

Baseba58c43/019A PR131; branch codex/combat-task008-inherited-movement.
Scope e-scope.md. E1 only Move4; E2/G1 route lifecycle thenG2 Breakdowncompletion explicitly owned
in canonicalplan. All2traces14moves16cuts48frozenartifacts required, no syntheticstate/idle admission.
Canonical baseline: PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-movement-v1.py
exit0 /tmp/e1-movement-baseline.log:2traces14moves16cuts384mutations66raw14retries81boundaries18pins.
WorkerRED missingimplementation, firstgreen7/7 all48fingerprints. AddedtypedforgedWorld/DP/bounds
probe, final31/31pass (8E1+23opening),0failed/skipped; /tmp/e1-final.log.
Exactcommand: dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore
--filter-class '*CombatInheritedMovementTests' --filter-class '*CombatReserveOpeningTests'
'-bl:/tmp/e1-final-{}.binlog'. Binlogs /tmp/e1-final-20260919-233954--65464--3PtDZY.binlog
and /tmp/e1-final-20260919-233957--65464--27QDul-dotnet-test.binlog.
Scopedformat/diffcheck pass. Devreview complete, no remainingfindings. Fivefiles frozen e1-source.sha256.
Rootbuild0warnings/errors (/tmp/e1-build.log). Command: dotnet build Sandtable.slnx --no-restore
'-bl:/tmp/e1-build-{}.binlog'. Added-line4localtargets pass; repositoryformat exit0 (/tmp/e1-format.log), gitdiffcheck pass; fullsuite2020/2020pass,0failed/skipped,3m23s088ms including81boundarychecks
(/tmp/e1-suite.log); command dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/e1-suite-{}.binlog'; independentround1 Ready/no findings, independently31pass; Independentround2 Ready/no findings, independently31pass; Independentround3 Ready/no findings, frozenhashes/canonicalandretained31+2020verified.
Allthree accepted; no conditionalexperiment needed.
Formatcommand: dotnet format Sandtable.slnx --verify-no-changes --no-restore.
