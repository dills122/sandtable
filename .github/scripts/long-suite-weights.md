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

The private connected accepted-outcome source adds five `CampaignAudienceOutcomeSourceTests` rows. Their weights are observed from a passing local macOS arm64 .NET 10 Release MTP XML run on 2026-10-19 UTC. This is scheduling evidence only; hosted CI remains the acceptance gate. Existing row weights and baseline metadata remain unchanged; the new class uses one ordinary suite part.

| New row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `ChangedCommandReceiptEventOrHeadCannotProduceTypedProof` | `bd46817c4f3c` | 222 |
| `CurrentBreakdownMoveStopAndSystemResolutionKeepBothCutsBound` | `810b6b4edfaa` | 545 |
| `OwnEntryCandidateRemainsUnavailableAndNeverCarriesRawElement` | `3c1a35e6eb0b` | 323 |
| `ReactionAcceptedEventsRequireExactActorAndReasonPairing` | `2733eed1f294` | 904 |
| `TwelveCurrentStepsProveBothSidesWithoutMutatingPredecessor` | `ada23885f690` | 728 |

Command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --no-restore --filter-class Cna.Core.Tests.Hosting.CampaignAudienceOutcomeSourceTests --report-xunit-xml --results-directory /private/tmp/sandtable-connected-outcome-release-timing`. Result: 5 passed, 0 failed, 0 skipped. Release Core DLL SHA-256 `dfd24b9bad11e24c018cf6de860b48ef29e99ecd91db0930c13cfbb57d0fbeb9`; XML SHA-256 `8723645b379af032988d1cb1a3fe0fcd6bc917bdae56f5e0ce1741967fbf11c7`. Exact Release discovery found 3,228 ordinary Core rows, including precisely these five new rows and no stale manifest rows.

The private A1 R03 checkpoint adds three `P01R03ValidationCheckpointTests` rows. A passing
local macOS arm64 .NET 10 Release MTP XML run on 2026-10-20 measured the elapsed times below.
They are scheduling hints, not hosted latency evidence. Existing row weights and baseline
metadata remain unchanged; this class uses one ordinary suite part.

| New row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `ClosedImportAndCheckpointRejectMalformedOrClaimedBytes` | `3dc7daf99a9b` | 3045 |
| `AuthenticRootProducesDeterministicUnissuableCheckpoint` | `82d8383f8f1c` | 954 |
| `MutatedSourceLedgerOrderAndCreationRequestCannotProduceCheckpoint` | `3a0ec0c84f29` | 1643 |

Command: `dotnet artifacts/bin/Cna.Core.Tests/release/Cna.Core.Tests.dll --filter-class Cna.Core.Tests.Campaigns.P01R03ValidationCheckpointTests --report-xunit-xml --results-directory /private/tmp/sandtable-a1-release-timing-2c2ffb8 --progress off`. Result: 3 passed, 0 failed, 0 skipped. Release Core DLL SHA-256 `d37d51f5e9a76d48aa9697b4cc702b4d44435d9d739481f12897cbbda276f008`; XML SHA-256 `4d33c234ee8b7e661678f271a0116cd13f7b85a8e385b9911a7a2736a03b18ac`. Source head at measurement: `2c2ffb89939b9a9ecf7540e3baee0f003136eda5`.
The failed prepare job in run `39465573467` retained complete Release discovery: 3,231
ordinary Core rows plus 325 frozen R02 rows. Independent local Core discovery at the
same source matched all 3,556 hosted Core identities. With only the three measured A1
rows added, the manifest reconciles exactly and the existing allocation remains 61
groups across 12 jobs. The failed run is not a passing CI claim; the updated head
requires a fresh exact-head hosted run.

The private A1 same-cut attestation adds two
`P01R03SameCutSourceAttestationTests` rows. A passing local macOS arm64 .NET 10
Release MTP XML run on 2026-10-20 measured the elapsed times below. These are
scheduling hints, not hosted latency evidence. Existing weights and baseline
metadata remain unchanged; the class uses one ordinary suite part.

| New row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `AuthenticatedTerminalCutAttestsPrivateSourceFactsOnly` | `28abbb391aec` | 1737 |
| `ForeignControlCutImportAndClaimCannotAttest` | `01620a55a93b` | 1009 |

Command: `dotnet artifacts/bin/Cna.Core.Tests/release/Cna.Core.Tests.dll --filter-class Cna.Core.Tests.Campaigns.P01R03SameCutSourceAttestationTests --report-xunit-xml --results-directory /private/tmp/sandtable-a1s-release-timing-b143eb2 --progress off`. Result: 2 passed, 0 failed, 0 skipped. Release Core DLL SHA-256 `2c42f764dc7719948825c352d76cc5eeda0be25f97c8724b38a3ea7559776b53`; XML SHA-256 `356171a19df39b41fc8ceb32c82360db6d97e6a318ded84f47c58166bc8de4f2`. Source head at measurement: `b143eb2bdb534a3d0883b2b158265222f1e454aa`. The failed prepare job in run `39478676567` reported an unknown/missing measured row; it is not a passing CI claim. This updated head requires a fresh exact-head hosted run.

Unknown, missing or renamed Core rows deliberately fail preparation with a clear error. Do not delete tests or guess a matching name to clear this gate. Adding a case requires an explicit manifest refresh; changing the runtime of an existing case may also justify new measurements.

The private Content8/Setup8 Core source reader adds five `CombatAutoSurrenderSourceTests` rows. A passing local macOS arm64 .NET 10 Release MTP XML run on 2026-10-20 measured only these new rows; all prior weights and baseline metadata remain unchanged. This is a scheduling observation, not a hosted CI or admission claim.

| New row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `AlternateCanonicalBytesOrOriginalContent7CannotMasqueradeAsContent8` | `b957e63ba0ee` | 10 |
| `FrozenTripletReadsOnlyApprovedOneOrientationSource` | `41ed21cd1778` | 6 |
| `OneByteTamperInAnyArtifactFailsClosed(index: 0)` | `447587703f25` | 1 |
| `OneByteTamperInAnyArtifactFailsClosed(index: 1)` | `a3e6709f08f5` | 1 |
| `OneByteTamperInAnyArtifactFailsClosed(index: 2)` | `33db1b23cfb6` | 1 |

Command: `/opt/homebrew/bin/dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-restore --filter-class Cna.Core.Tests.Campaigns.CombatAutoSurrenderSourceTests --report-xunit-xml --results-directory /private/tmp/sandtable-p01-content8-source-reader-release-timing`. Result: 5 passed, 0 failed, 0 skipped. Release Core test DLL SHA-256 `8eedfa8a0907cb2fdf1f9a0e547396867a31a4c6fb8525172e731b307bdcb028`; retained XML SHA-256 `12811b4107a7f4f6914ab0b4ba662c7bf98e751c5294a721179192b2e0543c5c`. Exact-head `r02_partition.py discover` found 3,233 ordinary Core rows: precisely these five keys were missing from the prior manifest, with no stale keys. The updated manifest covers 3,233 rows and 199 classes; `make_plan` allocates each once across 61 groups and all 12 dedicated jobs. The 28 partition supervisor tests pass outside the local sandbox; hosted exact-head CI remains the acceptance gate.

The private Content8 pre-issue seal adds three `CombatAutoSurrenderPreIssueSourceTests` rows. A passing local macOS arm64 .NET 10 Release MTP XML run on 2026-10-20 measured these new rows on source head `4f238939417a5dfe5c3272f8c10e7924eb008750`. Existing weights and baseline metadata remain unchanged. This is scheduling evidence only; the refreshed head still needs exact-head hosted CI.

| New row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `SealRetainsExactPinnedSourceAndClosedInventoryAfterCallerMutation` | `f81746f5bad8` | 13 |
| `LegacyContent7CannotBecomeAnAsymmetricSourceByRehashingTheEnvelope` | `04b45e2c1974` | 7 |
| `CrossProfileSpliceAndAlteredClaimsFailClosed` | `45df0c978d9d` | 6 |

Command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --no-restore --filter-class Cna.Core.Tests.Campaigns.CombatAutoSurrenderPreIssueSourceTests --report-xunit-xml --results-directory /private/tmp/sandtable-p01-content8-preissue-release-timing`. Result: 3 passed, 0 failed, 0 skipped. Release Core test DLL SHA-256 `82fb6eea9ce7b9d3c69a029be2934db6264814f6ae205c8a47dae47adbf98e98`; retained XML SHA-256 `e065d43857a2008d6274f4633bd1969cd324f0364487a3487132b459eb0d2345`. The old-head hosted prepare failure identified the three missing timing identities; it is not a passing CI claim. Local exact-head Release discovery on manifest commit `86433db9df96c6f9a557da1d50da4abb2f4e1265` found 3,239 ordinary Core rows plus 325 frozen R02 rows; the manifest contains exactly those 3,239 keys across 201 classes. `make_suite_proof` allocated 61 processes across all 12 dedicated jobs. The 28 partition-supervisor and 33 R02 regression tests passed; hosted exact-head CI remains the acceptance gate.

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

The R03 private Core branch adds 307 exact discovered row identities in two classes. The 2,912 original baseline rows plus four MAP-06 provisional rows and all 195 existing
class entries are preserved. The 300-row native parity
class measured 536,031 ms in a successful local Release xUnit XML report and is split into five
process groups; seven focused acceptance/rejection cases most recently measured 12,854 ms and remain one class.
The merged manifest now has 3,223 rows and 197 classes. These measurements are local timing hints,
not hosted latency acceptance. R03 added no provisional rows; the four MAP-06 rows remain labelled.

- Runner: .NET 10 Microsoft.Testing.Platform, Release Core DLL SHA-256
  `c307c80634f4682fdd9892d2aa40fbab7a071bd33fdc76fe42a06b24a1870e11`.
- Parity command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~CombatActualMovedRepeatBreakdownParityTests' --report-xunit-xml --results-directory /tmp/r03-native-parity-xml -v:q`. Result: 300/300 passed, 8m58s wall. XML SHA-256
  `74e86dee0b6a44a33a67131c680ba61403fa1e580c47b8146483633b1781d7af`.
- Focused command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~CombatActualMovedRepeatBreakdownCompletionTests' --report-xunit-xml --results-directory /tmp/r03-native-focused-release-xml -v:q`. Result on the reviewed numeric-fix branch: 7/7 passed, 14.5s wall. XML SHA-256
  `df8867becd7587b091fdf61fb29b5ee3dd46743965ab01febb5e35aee3e54e6b`.
- Single Release parity process sampled about 442–601 MiB RSS while running. This is observed
  process memory, not a peak or hosted capacity proof. Existing CI job count (12) and resource
  limits remain unchanged; five R03 process groups are added.

The exact installed discovery and allocation reconciliation are recorded in the R03 native review
evidence; hosted CI remains a separate acceptance gate.

The latest exact-head installed discovery at review-fix commit
`fe814c71b6196ae918e25bac15e2f928e118d65d` found 3,223 ordinary Core rows plus 325
frozen R02 rows in 20 groups. All 307 R03 rows were discovered (300 parity, seven focused);
the merged manifest has no missing or extra row hashes, including the four labelled MAP-06
provisional rows. `make_plan` assigned every ordinary Core row exactly once into 61 groups across
the unchanged 12 CI jobs, including five R03 parity groups of 59/61/60/60/60 rows.

After the independent review found numeric error-code drift, the native codec now classifies all
six R03 numeric leaves directly and traverses borrowed R02 Proof numeric leaves before delegated
canonicalization. The seven focused rows were remeasured from the successful Release XML above;
only those seven R03 weights changed. Four MAP-06 provisional rows and all unrelated weights were
preserved through the rebase onto main `f0266be`.

## P01 private owner-visible codec supplemental measurement (October 20, 2026)

The isolated #308 branch adds three ordinary Core rows in one class. They were measured from a
successful local .NET 10 Microsoft.Testing.Platform Release xUnit XML report; all three passed,
none failed or skipped. Existing row weights, baseline metadata, and provisional labels were
preserved. These are elapsed-time scheduling hints, not hosted latency acceptance.

| New row | SHA-256 row identity | Measured ms |
| --- | --- | ---: |
| `NoncanonicalAndDuplicateBytesRejectAtEverySeedBoundary` | `37d02e755f705659755a76f99fd8fec0e14b658ed624cd9ad40ea425394b0a82` | 11 |
| `AllSixApprovedPrivateVectorsMatchExactCanonicalBytesAndReferences` | `c4ad597a195715996613ff849fe165f661ee9d071190e1cef071cc85f5a43c46` | 6 |
| `TypedBoundariesAndBindingRejectInvalidOrCrossSeatInputs` | `3e76c6f9ae1eac1a1f299d4b8f03981ba7fb8b5efd8ab839697f0a7506c3d72d` | 1 |

- Command: `DOTNET_ROOT=/opt/homebrew/Cellar/dotnet/10.0.400/libexec artifacts/bin/Cna.Core.Tests/release/Cna.Core.Tests --filter-class Cna.Core.Tests.Campaigns.P01OwnerVisibleCodecV1Tests --report-xunit-xml --results-directory /private/tmp/sandtable-p01-owner-codec-release-timing --no-ansi`.
- Release Core DLL SHA-256: `75df2cb79c41a6cb15c532eb89e14a809c8f2b422bb1826cd7626b27bae44762`.
- Retained XML SHA-256: `97303c2e315cb8c945e3abcf71bc823738d8b07f662a1627ff7aae3269561797`.
- After rebasing onto merged #304 and #307 main, the manifest has 3,244 rows and 203 classes:
  their five new rows and two classes remain intact alongside these three #308 rows and one class.
  The prior #308 local discovery at `5482b08287f0e9f979435c389e5407c179488aa0` covered its
  then-current 3,239 rows; this rebased head requires fresh exact-head discovery and hosted CI.

## Private A1 physical host import Core rows

The two `P01A1PhysicalHostImportTests` rows were measured in a passing local
macOS arm64 .NET 10 Release Microsoft.Testing.Platform XML run on #310 head
`f578beae43cc432089fee0f6b8490022d9d1ee7b`. They are scheduling weights,
not hosted CI or Combat admission evidence. Existing measured rows and baseline
metadata remain unchanged; this class uses one ordinary suite part.

| New row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `RetainedPinnedBytesRevalidateWithoutOpeningAnAction` | `d0d2f2fb14c0` | 1968 |
| `RetainedBytesAndForeignRootsFailClosed` | `08e4fca9b2d2` | 2 |

Command: `dotnet artifacts/bin/Cna.Core.Tests/release/Cna.Core.Tests.dll --filter-class Cna.Core.Tests.Campaigns.P01A1PhysicalHostImportTests --report-xunit-xml --results-directory /private/tmp/sandtable-p01-a1-import-core-timing --progress off`. Result: 2 passed, 0 failed, 0 skipped. Release Core DLL SHA-256 `ec7c59a5181d4e2c6ef3436d7dac219ebad69755a98774ff31d8eb816f76f60e`; XML SHA-256 `83062d9167ebea63a500dd2c76b09d3d4b4c10f19af115047ed3b964a5d058aa`. The failed prepare job in run `39504461958` reported unknown or missing measured rows on the synthetic merge. The coordinated rebase retains the three already measured `P01OwnerVisibleCodecV1Tests` rows from #308 main. This local timing note is not a passing exact-head hosted run.
