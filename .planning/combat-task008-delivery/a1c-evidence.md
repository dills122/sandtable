# A1c verification and dev review

Target: working-tree A1c changes over `e64bed9`, branch
`codex/combat-task008-creation-binding`.

## Frozen baseline checks

All commands run from repository root, exit 0:

| Command | Observed result |
| --- | --- |
| `python3 docs/specs/verify-combat-authority-envelope-v1.py` | 4 goldens; 67 mutations; 36 raw-byte rejections; 9 recovery boundaries; 12 identity/context forks; 2 turn boundaries; 693 nested type rejections |
| `python3 docs/specs/verify-combat-creation-ledger-v1.py` | Setup 1,655 bytes; initial elements 2,685 bytes; 63 rejection vectors; holder/provenance/stage/shuffle checks |
| `python3 docs/specs/verify-combat-rules-inputs-v1.py` | 3 goldens; 47 mutations; 39 raw-byte rejections; 7 clock cases; 14 boundaries; 360 loss and 36 morale coordinates |
| `python3 docs/specs/verify-combat-world-settlement-v1.py` | 6 World goldens; 57 rejection vectors; 112 receipt cuts/20 scenarios; 8,840 arithmetic cases; 8 calendar boundaries |

These verify retained contract oracles, not production C# creation or replay.

## Runtime evidence

Worker RED: focused `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj
--filter-class '*CombatCreationBindingTests' --no-restore` failed CS0246 missing context type.
Worker GREEN: same command with `'-bl:/tmp/a1c-green-{}.binlog'`:48 passed, zero failed/skipped.
Named-pipe sandbox failure required approved execution; one intermediate test assertion changed
to accept JsonReaderException as JsonException subtype. No runtime failure suppressed.

Lead checks (all repository root):

- `dotnet restore Sandtable.slnx '-bl:/tmp/a1c-restore-{}.binlog'`: passed.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: initially failed whitespace/imports;
  targeted formatter fixed four new files; repeat passed, `/tmp/a1c-format-final.log` empty, exit0.
- `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/a1c-build-{}.binlog'`: passed, zero warnings/errors.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait
  'Boundary=UserSpace' '-bl:/tmp/a1c-boundary-{}.binlog'`:81 passed, zero failed/skipped.
- `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/a1c-suite-{}.binlog'`:1,836 passed,
  zero failed/skipped,3m23s886ms. `/tmp/a1c-suite.log`. Finished during round1 inspection.

## Dev review

Initial code inspection: three internal production files, one focused test file; no existing public
registration or historical codec edited. Request reader reads only campaign ID/seed, then reconstructs
all other bytes from validated context. Created11 reader compares entire derived event against
separately supplied request. Context reuses Setup/configuration strict readers; SourceAtom grammar
matches frozen ID bounds. Pure retry cut copies retained byte array and consults admission only when
absent. No I/O or authoritative mutable state in cut. Final verdict awaits focused evidence.

Requested stronger test coverage: same-ID Content provenance, Setup holder/turn variants, and assert
every raw mutation differs from original before checking rejection.

Final review checks exact 741-byte request and 7,520-byte Created11 literals; domain-separated
nonrecursive binding; independent trusted context/request; immutable inputs and unsigned seed
bounds; canonical bytes/shape rejection; retained retry before admission; conflict preservation;
historical/public boundaries. Review test failure sensitivity and five quality axes.

## Independent reviews

Dev verdict: no actionable findings after requested test improvements and formatter fixes.
Correctness/boundaries follow frozen derived-byte contract; all new APIs internal, no remote I/O,
authority activation or legacy edits. Readability and bounded construction acceptable.48 focused
tests include34 frozen mutations,28 raw mutations,12 identity forks and turn/seed boundaries.
Full solution and all equivalent `just check` constituent gates passed.

Round1 Ready, no findings; lead accepts. Round2 Not ready, P2 trusted Setup/configuration ID length;
lead Accept. Added envelope length guard without changing predecessor codecs. Four new128/129 tests:

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-method
  '*TrustedInputIdsRespectEnvelopeBounds' '-bl:/tmp/a1c-id-red-{}.binlog'`: RED4 total,2 expected129
  failures (no exception),2 accepted128 passes. `/tmp/a1c-id-red.log`.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class
  '*CombatCreationBindingTests' '-bl:/tmp/a1c-id-green-{}.binlog'`: GREEN52 passed, zero failed/skipped.

Lead reviewed bounded guard and test failure sensitivity; no remaining dev findings. Post-fix
full integration refresh completed. Round3 Ready, no findings; lead accepts. No round4 needed.

Final post-fix evidence:

- `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/a1c-final-build-{}.binlog'`: zero warnings/errors.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: exit0, empty final-format log.
- `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/a1c-final-suite-{}.binlog'`:1,840 passed,
  zero failed/skipped,3m11s324ms. Full suite includes disclosure-boundary cases.
- `git diff --check`: pass. Final C# hashes retained in `a1c-source.sha256`.

A1c accepted. Parent Task008, publication proof and A2 remain separate gates.
