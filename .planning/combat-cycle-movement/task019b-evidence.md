# Task019B verification
Base5bdecbf on codex/combat-cycle-movement-plan. Actual inherited released-I armed proof only.

TDD: /tmp/task019b-red.log compiled2 failures at planned Derive stub. /tmp/task019b-green.log passed2 with exact frozen proof parity. Expanded /tmp/task019b-expanded.log passed4/failed4 because malformed JSON throws JsonReaderException subtype; changed assertions to ThrowsAny<JsonException> for parse failures, and isolated missing-history checks before corruption. /tmp/task019b-expanded2.log passed8.

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatArmedContinuationTests' --filter-class '*CombatInheritedReserveReleaseTests' --filter-class '*CombatContinuationTests' /bl:/tmp/task019b-focused-{}.binlog`:46 passed,0 skipped; /tmp/task019b-focused.log.
- `dotnet build Sandtable.slnx --no-restore /bl:/tmp/task019b-build-{}.binlog`:0 warnings/errors; /tmp/task019b-build.log.
- `dotnet test --solution Sandtable.slnx --no-build /bl:/tmp/task019b-full-{}.binlog`:2,437 passed,0 failed,0 skipped in 11m 12s 903ms; exit0; /tmp/task019b-full.log.
- Frozen oracles armed-continuation, inherited-reserve-release, selection-steps, sealed-round, result-settlement and snapshot-composition (allv1):all six passed; /tmp/task019b-<contract>.log.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`:exit0; /tmp/task019b-format-verify.log.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' /bl:/tmp/task019b-boundary-{}.binlog`:81 passed,0 skipped; /tmp/task019b-boundary.log.
- `git diff --check`, plan local links and four frozen source/project hashes:passed.
- Independent review1of3:Ready with non-blocking follow-ups,no actionable findings. Full-suite condition fulfilled with 2,437 passing tests; see task019b-review-report.md.

Both proofs match all canonical bytes and golden sizes/hashes (3905/3945bytes). Tests bind actual progress event/hash, priorMovement receipt,candidate,all four support hashes/counts and supportDigest; retainWorld/Release/RNG/CP/ammo/TOE/exception and unchangedauthority25/ordinal1. Both-owner leaf/alternate/raw proof edits, rehashedsupport, foreign/incomplete/fallback/changedtiming/altered/re-signed history and source ownership checked. Alternate validtimings deliberately outside frozen3j scope. No repeat/finish or new campaign admission claimed.

Frozen oracle details:
- `python3 docs/specs/verify-combat-inherited-armed-continuation-v1.py`: combat inherited armed continuation v1: 2 proofs, 2 readbacks, 180 deep mutations, 8 raw variants, 18 boundary rejects
- `python3 docs/specs/verify-combat-inherited-reserve-release-v1.py`: PASS inherited Reserve Release: 2 traces, 6 events, 8 cuts, 6 retries, 658 mutations, 24 raw rejections, 31 boundaries, 10 recovery paths, 7 source pins
- `python3 docs/specs/verify-combat-selection-steps-v1.py`: PASS: 5 literal traces; 41 event/control cuts; 246 event mutations; 164 raw rejections; exact retries, deadline/regression/unavailability, RBA races and FA gate. Isolated boundary probes only; no full campaign replay.
- `python3 docs/specs/verify-combat-sealed-round-v1.py`: PASS: 4 traces; 23 replay/state cuts; 276 event/state mutations; 138 raw rejections; seal orders, retries, deadlines, cancellation, CP ceilings and commitment guards. Isolated contract evidence only.
- `python3 docs/specs/verify-combat-result-settlement-v1.py`: PASS: 8 literal cases, mirrored roles/reversed seals; 68 replay cuts, 728 mutations, 340 raw rejections, 96 timing checks; overflow/closure guards. Contract evidence only.
- `python3 docs/specs/verify-combat-snapshot-composition-v1.py`: PASS: 17 traces/149 full Snapshot12 cuts, 1358 mutations, 596 raw rejections; inherited ledger and full pre-round binding preserved. Synthetic pre-Combat lineage only.
