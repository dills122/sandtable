# Refreshing long-suite scheduling weights

`long-suite-weights.json` contains derived timing hints and hashes of actual runner row identities. It contains no authoritative replay results. The current measurements come from main121baa56820fffce42316b48ac551cb24c012a6c / run38830848442 attempt1. Row duration includes contention and is not CPU time or a capacity bound. Zero-resolution observations receive a1ms scheduling floor.

MAP-06 adds four `CampaignHostAdapterTests` rows. Their provisional weights are measured from a passing macOS arm64 .NET 10 Debug MTP XML run on 2026-10-17 (`/private/tmp/map06-host-timing`), with a concurrent full Core run. The four row hashes appear in `provisional_rows`; they retain the original baseline metadata and require Release CI calibration before performance conclusions. The new class uses one ordinary suite part and does not add a matrix job.

| MAP-06 row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `TwelveStepHostTraceMatchesDirectCoreAtEveryCut` | `fa28d0993dcb` | 1487 |
| `AcceptedStepAndImportMatchDirectExerciseAndRejectTampering` | `04159b9654fc` | 228 |
| `RejectedStepLeavesAllCutsAndCheckpointUnchanged` | `284b0df70fd0` | 87 |
| `CreationAndEachAudienceCutMatchDirectCore` | `ecef32257299` | 77 |

Command: `/opt/homebrew/bin/dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Hosting.CampaignHostAdapterTests --report-xunit-xml --results-directory /private/tmp/map06-host-timing`. Result: 4 passed, 0 failed, 0 skipped; XML is retained in that local directory for review.

MAP-07's `Cna.OrleansHost.Tests` is outside this Core-only timing manifest and its fixed
partition. CI runs it as a separate required `orleans` job and checks its MTP XML with
`verify_orleans_results.py`; no Orleans row hashes are added to `long-suite-weights.json`.
A passing macOS arm64 .NET 10 Release run on 2026-10-17 at
`/private/tmp/map07-host-release-timing` measured these provisional elapsed times. These are
local scheduling observations, not Release CI performance claims.

| MAP-07 row (class `Cna.OrleansHost.Tests.TemporaryCampaignGrainTests`) | Measured ms |
| --- | ---: |
| `TwelveStepTraceStopsAtUnsupportedCombatWithoutGuessingTerminal` | 1344 |
| `InProcessHostMatchesDirectCoreAndDeduplicatesConcurrentRequests` | 460 |
| `SystemProgressionStopsForHumanAndRestartLosesTemporaryCampaign` | 172 |

Command: `/opt/homebrew/bin/dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj --configuration Release --no-build --no-restore --report-xunit-xml --results-directory /private/tmp/map07-host-release-timing`. Result: 3 passed, 0 failed, 0 skipped; exact row identities verified by `.github/scripts/verify_orleans_results.py`.

Unknown, missing or renamed Core rows deliberately fail preparation with a clear error. Do not delete tests or guess a matching name to clear this gate. Adding a case requires an explicit manifest refresh; changing the runtime of an existing case may also justify new measurements.

1. Commit the proposed source on a feature branch. Build Release and the ReleaseTestRows helper using the selected SDK and normal binlog flags. Obtain actual discovery with the retained `r02_partition.py discover` command, using a fresh output directory, exact HEAD and a fresh attempt. That command does not require the long-suite timing manifest; it still proves deferred providers and all R02 semantic rows. No hosted run is implied or authorized by this procedure.
2. Measure the new/changed cases with the installed MTP runner and literal selectors. Keep every assertion, strict XML, process exit, source/attempt/SDK and resource evidence. For a complete refresh use an already verified full artifact whose prepared inventory and all actual rows reconcile. Never derive weights from a failed, skipped, truncated or unbound report. Retain the prior manifest and observation as evidence.
3. The following lightweight recipe recreates a fully measured manifest from such a complete proof directory. Set `PROOF` to the local selected artifact directory containing `r02-inventory.json` and all execution XML. Run from the repository root; save/review the output before replacing the tracked file.

```sh
PROOF=/absolute/path/to/verified-proof
PYTHONDONTWRITEBYTECODE=1 python3 - "$PROOF" > /private/tmp/refreshed-long-suite-weights.json <<'PY'
import json, math, sys
from pathlib import Path
import xml.etree.ElementTree as ET
sys.path.insert(0, '.github/scripts')
from long_suite_partition import row_key
from test_partition import report_rows, require
root = Path(sys.argv[1])
proof = json.loads((root / 'r02-inventory.json').read_text())
expected = {tuple(row) for row in proof['legacy_core_rows']}
observed = {}
for path in root.rglob('*.xml'):
    document = ET.parse(path)
    assembly = document.getroot().find('assembly')
    module = Path(assembly.attrib['name'].replace('\\', '/')).stem
    if module != 'Cna.Core.Tests':
        continue
    report_rows(path.parent.parent, module)
    for test in assembly.findall('.//test'):
        name = json.loads('"' + test.attrib['name'] + '"')
        row = (module, test.attrib['type'] + '.' + test.attrib['method'], name)
        if row not in expected:
            continue  # Exact separately frozen R02 rows remain in their own proof.
        require(row not in observed, 'Duplicate measured row')
        observed[row] = max(1, round(float(test.attrib['time']) *1000))
require(set(observed) == expected, 'Incomplete actual measured Core rows')
classes = {}
for row, milliseconds in observed.items():
    classes.setdefault(row[1].rsplit('.',1)[0], []).append(milliseconds)
manifest = {
    'schema_version':1,
    'baseline_sha':proof['binding']['tested_sha'],
    'baseline_run_id':proof['binding']['run_id'],
    'baseline_attempt':proof['binding']['run_attempt'],
    'weight_units':'observed per-row elapsed milliseconds; contention included, not CPU',
    'classes':{name:{'parts':min(len(values),math.ceil(sum(values)/120000))
               if sum(values)>180000 else 1} for name,values in sorted(classes.items())},
    'rows':dict(sorted((row_key(row),value) for row,value in observed.items()))}
print(json.dumps(manifest,indent=2))
PY
```

4. When only new cases were measured, preserve existing measured entries and merge only the exact new identity hashes and their class metadata. If temporary conservative estimates are necessary for local development, label every affected hash in `provisional_rows`, explain the estimate and source, and keep measured/provisional provenance separate. Such estimates require explicit review and do not establish the latency goal. Do not silently label estimates as run38830848442 measurements.
5. Run the allocation/process regression suite, installed discovery and complete actual-row reconciliation. Inspect the generated plan: all rows exactly once, no empty matrix jobs, every long/ordinary suite bounded, R02 support isolation unchanged. A changed group/job count requires a matching reviewed workflow and cost model. The recipe is not permission to increase concurrency, memory or timeouts. Freeze the source/manifest together, perform author self-review and fresh review before a separately authorized hosted observation. Full required CI wall, in-DAG queue and runner-minute tradeoffs decide performance acceptance.

## R03 native supplemental measurement (October 17, 2026)

The R03 private Core branch adds 307 exact discovered row identities in two classes. The 2,912
existing row weights and 194 class entries are byte-for-byte preserved. The 300-row native parity
class measured 536,031 ms in a successful local Release xUnit XML report and is split into five
process groups; seven focused acceptance/rejection cases measured 14,550 ms and remain one class.
The manifest now has 3,219 rows and 196 classes. These measurements are local timing hints, not
hosted latency acceptance. No provisional rows were added.

- Runner: .NET 10 Microsoft.Testing.Platform, Release Core DLL SHA-256
  `c307c80634f4682fdd9892d2aa40fbab7a071bd33fdc76fe42a06b24a1870e11`.
- Parity command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~CombatActualMovedRepeatBreakdownParityTests' --report-xunit-xml --results-directory /tmp/r03-native-parity-xml -v:q`. Result: 300/300 passed, 8m58s wall. XML SHA-256
  `74e86dee0b6a44a33a67131c680ba61403fa1e580c47b8146483633b1781d7af`.
- Focused command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~CombatActualMovedRepeatBreakdownCompletionTests' --report-xunit-xml --results-directory /tmp/r03-native-focused-release-xml -v:q`. Result: 7/7 passed, 15.7s wall. XML SHA-256
  `eec23f4d489ee076a39d5ee93388d600245d4555871789413e2ee8c330ad7a77`.
- Single Release parity process sampled about 442–601 MiB RSS while running. This is observed
  process memory, not a peak or hosted capacity proof. Existing CI job count (12) and resource
  limits remain unchanged; five new process groups increase the prepared group count to 61.

The exact installed discovery and allocation reconciliation are recorded in the R03 native review
evidence; hosted CI remains a separate acceptance gate.

The exact-head installed discovery at commit `2baccb6409781093b30c6bf8eb73f3d6a852ff92`
found 3,219 ordinary Core rows plus 325 frozen R02 rows in 20 groups. All 307 new R03 rows
were discovered (300 parity, seven focused); the manifest has no missing or extra row hashes.
`make_plan` assigned every ordinary Core row exactly once into 61 groups across the unchanged
12 CI jobs, including five R03 parity groups of 59/61/60/60/60 rows.
