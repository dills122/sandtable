# 019A verification and review evidence

Basefa5e723/D2 PR130; branch codex/combat-task019a-first-opening.
Research scope retained019a-scope.md. No new prerequisite. Same completion2 event atomicprojection;
all16traces40cuts152artifacts and current-terminal-state retries required. D1precompletion untouched.
Worker RED missingimplementation, final67/67pass (23D1+21D2+23new019A),0failed/skipped.
Exact command: dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore
--filter-class '*CombatReserveOpeningTests' --filter-class '*CombatReserveCompletionTests'
--filter-class '*CombatReserveDesignationTests' '-bl:/tmp/019a-final-{}.binlog'.
Log /tmp/019a-final.log; binlogs /tmp/019a-final-20260919-231853--62895--E5L3AG.binlog
and /tmp/019a-final-20260919-231853--62895--V_xNhc-dotnet-test.binlog.
All152artifacts/40cuts match. Scopedformat/diffcheck passed. Devreview complete, no remainingfindings;
outputbuffer and D1terminal-reader probes added beforefreeze. Fivefiles frozen019a-source.sha256.
Root build passed0warnings/errors (/tmp/019a-build.log), repositoryformat exit0 (/tmp/019a-format.log).
Commands: dotnet build Sandtable.slnx --no-restore '-bl:/tmp/019a-build-{}.binlog';
dotnet format Sandtable.slnx --verify-no-changes --no-restore.
Gitdiffcheck and added-line localtargets pass. Fullsuite2012/2012pass,0failed/skipped,3m27s821ms including81boundarychecks (/tmp/019a-suite.log).
Command: dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/019a-suite-{}.binlog'. Independentround1 Ready, nofindings, independently67/67pass; Independentround2 Ready, nofindings, independently67/67pass; Independentround3 Ready, nofindings, sourcehashes/retained67+2012verified.
Allthree reviews accepted; no conditionalexperiment needed.
Canonical Reserveoracle unchanged baseline /tmp/d-reserve-baseline.log passed16traces40cuts,
2342leaf733raw1578boundary4parity14pins. Runtime evidence must independently establish all cuts.
