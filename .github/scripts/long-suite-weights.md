# Refreshing long-suite scheduling weights

## Private proof runner pairing (October 20, 2026)

The paid fresh-process and RBA decline/cancellation proof matrices pair their
four physical shards onto two runners each. They retain the original `--jobs 4`
and shard IDs 0–3, four distinct JSON files, physical negatives, and frozen
four-file aggregates. The slower 300-root absence and 12,116-cut prefix-rejection
matrices and all 17 complete-test jobs remain unchanged. Those complete-test
jobs require fresh canonical process directories and cannot safely share one
runner without changing the evidence contract. The complete CI graph falls
from 48 to 42 jobs; no test row, root, cap, or proof assertion is removed.

This pairing uses one observational baseline: docs-only PR #309, CI run
`39494124557`. Its 43 completed jobs at inspection had median runner queue
25.25 minutes (maximum 48.58) and median execution 5.25 minutes (maximum 13.68).
The longest individual prefix-rejection shard took 13.68 minutes, which is why
that matrix stays separate. These are queue/contention observations, not an
accepted latency or capacity result. Compare complete exact-head hosted runs
before claiming a throughput improvement; any failed, skipped, or missing shard
still blocks the required `verify` and all-checks-success merge gate.

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

## Permanent contact Core rows (PR #314)

The 18 new rows in `CampaignContactJournalTests`, `CampaignContactKnowledgeTests`, and
`CampaignContactOwnerMovementCutTests` were measured by three passing focused
macOS arm64 .NET 10 Release Microsoft.Testing.Platform XML runs on source head
`ac82837757f07b2ba6b7f97da37710c9379d920c`. Each class uses one ordinary
suite part. Values are local scheduling hints, not hosted timing or admission
evidence. Existing measured rows and baseline metadata remain unchanged.

| Class | Row hash prefixes and elapsed milliseconds |
| --- | --- |
| `CampaignContactJournalTests` | `efa5b3b92858` 334; `6c524d3cd49a` 20; `c13540b4245c` 216; `c182ffed3df9` 291; `0dd787ea3ae7` 324; `c26be8ecbcb2` 69 |
| `CampaignContactKnowledgeTests` | `000957e018b8` 1; `d1fc33f17b1b` 2; `22aa2e5143a4` 6; `3bb45012c133` 1; `132105f2141f` 1; `e5b02d332f38` 13 |
| `CampaignContactOwnerMovementCutTests` | `749f92d5ebdb` 28; `8855b5d1eb9a` 674; `d04eb9a57ff3` 45; `0cf66e3695cf` 11; `b3704b64db90` 30; `ac2b56ad9ffb` 77 |

Command for each class: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --no-restore --filter-class Cna.Core.Tests.Observations.<class> --report-xunit-xml --results-directory /private/tmp/sandtable-pr314-timing-<class> --progress off`. Each run passed 6/6. Release Core test DLL SHA-256 `9366a99b46b31f718ae917ab241ff10b391f450620b6ae9c1eacd711aed42799`; retained XML SHA-256 by table class order: `b089df0405571139a788987561e3c22013f54edecfac103906fb43217cd7f036`, `3f59755ee068843ae8ae28a7d3f362325be174163107970167ead98928a7f765`, `806325285ed453f7addb7a99ac05b5c903cb0aa9dc2cffb78d0d3e05d2bebf01`. Exact-head local discovery found 18 missing rows, zero stale rows; after refresh, all 3,264 rows allocate once across 61 groups and the same 12 jobs. PR #314's first hosted `prepare` failed on the missing-row guard; this update requires a new exact-head hosted run.

## Native Combat Core rows

Release row discovery on local source head `f05e9c5719f26deeaa277d68368fa451f066c633`
found 3,275 ordinary Core rows: 11 new identities and no stale identities relative
to the prior manifest. Four focused macOS arm64 .NET 10 Release MTP XML runs
passed every selected case (6/6 contact selection, 3/3 content source, 1/1
reserve release, 21/21 catalog). The Release Core DLL SHA-256 was
`ef9f5bb9899f8e5e9c95a18454523208fc39aaa1926f83e82723bcd7eb5e8ae0`.
The retained XML SHA-256 values in that order were
`47ed99885cdf3bed75eca56e6df42ad7618b8dd16ca6f14aacac0f8d3ea3eb1b`,
`cd6d2aa4666286ceca85f58ae4881174c5541b3946dcbc3e384c8ecc3ddd9f65`,
`e06d372f85c1bddbad42b3b627c42fb343e658cae913128ff80feae2e89e4723`, and
`74ee1f48b350da6d43032da1a1236bc7a2b4b6c452952b0f4804b0a4e7dcda41`.

| Class | New row hash prefixes and measured milliseconds |
| --- | --- |
| `CampaignReserveReleaseRulesTests` | `f1743f6f3f65` 411 |
| `BreakdownRunnerCatalogTests` | `05cd233556f8` 12 |
| `ContentCombatSourceTests` | `3faf3e9cc3aa` 3; `5aa6b175d05a` 2; `75d3d3045b85` 94 |
| `CampaignContactCombatSelectionCutTests` | `980c66a3e663` 4527; `75bdb39b0df1` 1; `5e52cc4c8c43` 83; `772c5bb44de3` 2039; `e212130fc60e` 83; `5f5db497f01a` 1 |

Existing measured rows and baseline metadata remain unchanged. Three new
classes use one part each; the catalog class keeps its existing part. The
refreshed manifest allocates all 3,275 ordinary rows once across 61 groups
and the same 12 Core jobs. The 28 long-suite supervisor and 33 R02 partition
tests passed locally. These are scheduling hints measured concurrently with
the full Debug Core suite, not hosted timing or Combat admission evidence.

## Native persisted RBA cancellation (October 24, 2026)

Six new Core rows measured in passing Release focused run (17 total tests).
Existing row weights and baseline metadata remain unchanged. Local scheduling
hints only; hosted CI still requires complete exact-head coverage.
Report: `artifacts/rc-rba-core/dsteele_Dylans-MacBook-Pro_2026-10-24_21_36_49.205213.xunit.xml`.

| New row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `Cna.Core.Tests.Campaigns.CampaignNativeRbaCancellationTests.RepeatedRuntimeCancelsAtItsActualOrdinalWithoutRandomDraw(ordinal: 2)` | `45335cbcb195` | 480 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRbaCancellationTests.RepeatedRuntimeCancelsAtItsActualOrdinalWithoutRandomDraw(ordinal: 3)` | `a73e548c3343` | 335 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRbaCancellationTests.DeadlineCancelsWithoutDeclineAndReconstructsUnchangedWorld` | `4fffba6abaca` | 995 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRbaCancellationTests.StrictReplayRejectsChangedBarrageBindingAndUnknownFields` | `190724b637c8` | 986 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRbaCancellationTests.AcceptedRetreatRemainsAppliedAndDeadlineDoesNotRestart` | `d0f67f95834e` | 1342 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRbaCancellationTests.RejectionAndClockBoundariesDoNotMutateRba` | `a5b2104d951a` | 386 |

## Native cycle-control timeout (October 24, 2026)

Seven new Core rows measured in passing Release focused run. Existing weights
and baseline metadata unchanged; local scheduling hints, not hosted performance
or admission evidence.
Report: `artifacts/rc-cycle-core-final/dsteele_Dylans-MacBook-Pro_2026-10-24_22_43_57.721650.xunit.xml`.

| New row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `Cna.Core.Tests.Campaigns.CampaignNativeCycleControlTimeoutTests.ClockAndBudgetRejectionsLeavePendingDecisionUnchanged` | `cb8142df884a` | 1225 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCycleControlTimeoutTests.UnavailableOpeningHasNoInventedClock` | `26a6e1e107cb` | 2051 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCycleControlTimeoutTests.DeadlineFinishesAsSystemAndReconstructsWithoutOwnerChoice` | `4dedc3f28676` | 1606 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCycleControlTimeoutTests.OwnerFinishRemainsVersion28AndTimeoutBytesStayDistinct` | `14ae1048256e` | 463 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCycleControlTimeoutTests.RepeatedRuntimeRetainsExplicitTimeoutAtActualOrdinal(ordinal: 2)` | `0d9b107b1551` | 359 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCycleControlTimeoutTests.RepeatedRuntimeRetainsExplicitTimeoutAtActualOrdinal(ordinal: 3)` | `79ed01ea0c1c` | 408 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCycleControlTimeoutTests.StrictReplayRejectsChangedReleaseWorldOrdinalAndUnknownFields` | `1d06f88e601b` | 246 |

## Native Reserve Release timeout (October 24, 2026)

Nine new rows measured in local Release focused test run. Existing weights and
baseline metadata unchanged. Local scheduling hints; hosted calibration pending.
Report: `artifacts/rc-reserve-timeout/core-results/dsteele_Dylans-MacBook-Pro_2026-10-24_23_04_49.899164.xunit.xml`.

| New row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `Cna.Core.Tests.Campaigns.CampaignNativeReserveReleaseTimeoutTests.LaterCycleRetainsIIAndResolvesEveryRemainingMemberOnce(ordinal: 3)` | `405779577887` | 159 |
| `Cna.Core.Tests.Campaigns.CampaignNativeReserveReleaseTimeoutTests.LaterCycleRetainsIIAndResolvesEveryRemainingMemberOnce(ordinal: 2)` | `adfcb31d07a5` | 46 |
| `Cna.Core.Tests.Campaigns.CampaignNativeReserveReleaseTimeoutTests.ReplayRejectsChangedMembersAnchorWorldAndUnknownFields` | `7428d18cb285` | 52 |
| `Cna.Core.Tests.Campaigns.CampaignNativeReserveReleaseTimeoutTests.SessionTimeoutValidatesTimingAndReconstructsSystemCheckpoint` | `c6170c5b92f6` | 905 |
| `Cna.Core.Tests.Campaigns.CampaignNativeReserveReleaseTimeoutTests.TimeoutConvertsUnresolvedIWithoutMovementAllowanceOrOwnerEvent` | `848b17945a63` | 60 |
| `Cna.Core.Tests.Campaigns.CampaignNativeReserveReleaseTimeoutTests.UnavailableOpeningHasNoInventedClockAndLegacyChoiceBytesStayVersion27` | `62310510c061` | 60 |
| `Cna.Core.Tests.Campaigns.CampaignNativeReserveReleaseTimeoutTests.LargeBatchCodecDispatchesBeforeLegacyByteLimit` | `b5caea3cfd60` | 58 |
| `Cna.Core.Tests.Campaigns.CampaignNativeReserveReleaseTimeoutTests.AcceptedOwnerReleaseSurvivesTimeoutAndEntryAnchorDoesNotMove(ordinal: 2)` | `5fbd7868a35b` | 53 |
| `Cna.Core.Tests.Campaigns.CampaignNativeReserveReleaseTimeoutTests.AcceptedOwnerReleaseSurvivesTimeoutAndEntryAnchorDoesNotMove(ordinal: 1)` | `9c6ce4516e19` | 52 |

## Native Custody timeout (October 24, 2026)

Nine new rows measured in local Release focused test run. Existing weights and
baseline metadata unchanged. Local scheduling hints; hosted calibration pending.
Report: `artifacts/rc-custody-timeout/core-results/dsteele_Dylans-MacBook-Pro_2026-10-24_23_21_07.475443.xunit.xml`.

| New row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `Cna.Core.Tests.Campaigns.CampaignNativeCustodyTimeoutTests.AcceptedOwnerDispositionCannotBeOverriddenAndUnsupportedFallbackRejects` | `5e262a9daf4b` | 672 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCustodyTimeoutTests.TamperedLotRouteAndTimingEvidenceRejectReplay` | `b8eaa3f30ed6` | 505 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCustodyTimeoutTests.ClockFailuresKeepExplicitSystemReason(reason: "clock-unavailable", observed: null)` | `1d1ea0ebabd6` | 1179 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCustodyTimeoutTests.ClockFailuresKeepExplicitSystemReason(reason: "clock-regression", observed: 999)` | `6a83120d2967` | 1323 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCustodyTimeoutTests.ClockFailuresKeepExplicitSystemReason(reason: "controller-unavailable", observed: null)` | `9e28832f5784` | 899 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCustodyTimeoutTests.NativeSingleLotEscapeUsesAtMostOneClearEdgeWithinSourceBudget` | `9723abeb0908` | 371 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCustodyTimeoutTests.UnavailableEntryHasNoInventedClock` | `9893b8c1de4c` | 560 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCustodyTimeoutTests.InvalidTimingDoesNotMutatePendingCustody` | `7a1aba970e55` | 226 |
| `Cna.Core.Tests.Campaigns.CampaignNativeCustodyTimeoutTests.DeadlineLeavesCaptivesUnguardedWithSystemEvidenceAndColdReplay` | `9b492a919108` | 534 |

## Native Retreat timeout (October 24, 2026)

Nine new rows measured in local Release focused run. Prior weights and baseline
metadata unchanged. Local scheduling hints; hosted calibration pending.
Report: `artifacts/rc-retreat-timeout/core-results/dsteele_Dylans-MacBook-Pro_2026-10-24_23_39_49.870403.xunit.xml`.

| New row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `Cna.Core.Tests.Campaigns.CampaignNativeRetreatTimeoutTests.DeadlineRefusesWithSourceLossesSystemEvidenceAndColdReplay` | `330003decb9c` | 1300 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRetreatTimeoutTests.TamperedResultWorldAndTimingEvidenceRejectReplay` | `70fc7507bbaa` | 271 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRetreatTimeoutTests.ClockFailuresKeepExplicitSystemReason(reason: "clock-regression", observed: 999)` | `a8b6fba6dd06` | 456 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRetreatTimeoutTests.ClockFailuresKeepExplicitSystemReason(reason: "clock-unavailable", observed: null)` | `0a42334f1ad3` | 466 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRetreatTimeoutTests.ClockFailuresKeepExplicitSystemReason(reason: "controller-unavailable", observed: null)` | `b7d457cb891b` | 443 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRetreatTimeoutTests.UnavailableEntryHasNoInventedClock` | `6505b24d65b3` | 448 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRetreatTimeoutTests.InvalidTimingDoesNotMutatePendingRetreat` | `95587ecc917f` | 235 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRetreatTimeoutTests.RepeatedRuntimeRetainsActualOrdinalAndRefusalEvidence(ordinal: 3)` | `92fef5576bc3` | 229 |
| `Cna.Core.Tests.Campaigns.CampaignNativeRetreatTimeoutTests.RepeatedRuntimeRetainsActualOrdinalAndRefusalEvidence(ordinal: 2)` | `2d9ab141866f` | 202 |

## Native Selection timeout (October 24, 2026 America/Toronto)

Nine new rows measured in local Release focused run. Prior weights and baseline
metadata unchanged. Local scheduling hints; hosted calibration pending.
Report: `artifacts/rc-selection-timeout/core-results/dsteele_Dylans-MacBook-Pro_2026-10-25_00_04_28.000262.xunit.xml`.

| New row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `Cna.Core.Tests.Campaigns.CampaignNativeSelectionTimeoutTests.BindingRejectsOutsidePendingSelectionWithoutMutatingAuthority` | `8463d4267e17` | 568 |
| `Cna.Core.Tests.Campaigns.CampaignNativeSelectionTimeoutTests.DeadlineClosesActualSelectionWithoutOwnerChoiceOrNewDraw(repeated: False)` | `5f32fb110bce` | 1435 |
| `Cna.Core.Tests.Campaigns.CampaignNativeSelectionTimeoutTests.DeadlineClosesActualSelectionWithoutOwnerChoiceOrNewDraw(repeated: True)` | `f0e72c774675` | 1801 |
| `Cna.Core.Tests.Campaigns.CampaignNativeSelectionTimeoutTests.UnavailableEntryHasNoInventedClockAndTamperingRejectsReplay` | `a04d8d0a6fc1` | 564 |
| `Cna.Core.Tests.Campaigns.CampaignNativeSelectionTimeoutTests.EarlyForeignConfigurationAndAlreadySelectedRejectWithoutMutation(repeated: True)` | `0a7e3ee9d7b9` | 531 |
| `Cna.Core.Tests.Campaigns.CampaignNativeSelectionTimeoutTests.EarlyForeignConfigurationAndAlreadySelectedRejectWithoutMutation(repeated: False)` | `6fdc6541be3f` | 210 |
| `Cna.Core.Tests.Campaigns.CampaignNativeSelectionTimeoutTests.FailureReasonsHaveExplicitReplayableEvidence(reason: "clock-unavailable", observed: null)` | `fba4410da237` | 1190 |
| `Cna.Core.Tests.Campaigns.CampaignNativeSelectionTimeoutTests.FailureReasonsHaveExplicitReplayableEvidence(reason: "clock-regression", observed: 999)` | `6c100195ec54` | 500 |
| `Cna.Core.Tests.Campaigns.CampaignNativeSelectionTimeoutTests.FailureReasonsHaveExplicitReplayableEvidence(reason: "controller-unavailable", observed: null)` | `13212da9e28e` | 596 |

## Evidence revision compatibility (October 25, 2026 America/Toronto)

Sixteen new Core rows measured from passing local Release MTP XML. Prior weights and baseline metadata retained; snapshot class uses one ordinary suite part. These are scheduling hints, not hosted performance claims. Commands: `dotnet exec artifacts/bin/Cna.Core.Tests/release/Cna.Core.Tests.dll --filter-method '*Legacy*' --report-xunit-xml --results-directory <output>` and equivalent `--filter-class '*CombatDependencySnapshotTests'`. XML retained under `artifacts/pr334-ci/timings`. Only newly discovered rows added; no coverage guard relaxed.

| Row | Hash prefix | Measured ms |
| --- | --- | ---: |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.IndividuallyRegisteredBytesFromDifferentRevisionsAreRejected` | `7775778eb492` | 11 |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.CompleteRevisionOwnsBytesAndReturnsIndependentCopies(revision: "refreshed")` | `6daed8308087` | 2 |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.CompleteRevisionOwnsBytesAndReturnsIndependentCopies(revision: "legacy")` | `2a559df78733` | 1 |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.NestedAcquisitionPreservesRootRevisionForIdenticalSubset` | `9c688173e2c6` | 1 |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.ConcurrentRevisionsNeverShareSelectionState` | `8aab92e64a0f` | 14 |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.ExplicitRevisionCannotFallBackToAnotherRegisteredRevision` | `3c037cc492a7` | 1 |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.CallerCannotChangeRegistrationDuringAcquisition` | `ae24f2801b98` | 1 |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.InvalidAcquisitionRejectsBeforeExposingSnapshot(fault: "null")` | `32672044482a` | 1 |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.InvalidAcquisitionRejectsBeforeExposingSnapshot(fault: "oversized")` | `9ed7ce1d3208` | 1 |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.InvalidAcquisitionRejectsBeforeExposingSnapshot(fault: "missing")` | `a1d81891f712` | 1 |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.InvalidAcquisitionRejectsBeforeExposingSnapshot(fault: "unknown")` | `fc884e0fc64d` | 1 |
| `Cna.Core.Tests.Campaigns.CombatActualMovedAssaultContractTests.LegacyRepeatedBreakdownPreservesCommandEventProofAndRetryIdentity(index: 1)` | `bb472bbe434d` | 2523 |
| `Cna.Core.Tests.Campaigns.CombatActualMovedAssaultContractTests.LegacyRepeatedBreakdownPreservesCommandEventProofAndRetryIdentity(index: 0)` | `fb2370b255f6` | 2213 |
| `Cna.Core.Tests.Campaigns.CombatActualMovedAssaultContractTests.LegacyAndRefreshedFilesCannotBeMixedAfterSuccessfulReplay` | `fa6bb576c453` | 158 |
| `Cna.Core.Tests.Campaigns.CombatActualMovedAssaultContractTests.LegacyRevisionReplaysEveryRetainedCutWithoutIdentityMigration(name: "axis-player-decline-attacker-selected")` | `6faa8f47da8b` | 3326 |
| `Cna.Core.Tests.Campaigns.CombatActualMovedAssaultContractTests.LegacyRevisionReplaysEveryRetainedCutWithoutIdentityMigration(name: "commonwealth-player-decline-attacker-selected")` | `3ef243b8d707` | 2878 |

RoundEntry consumption recheck correction adds two measured nested-snapshot rows (legacy/refreshed), from Release MTP XML; previous rows unchanged.

| Row | SHA256 | ms |
| --- | --- | ---: |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.ConsumptionRevalidationTraversesParentsAndOwnsCurrentBytes(revision: "refreshed")` | `2aa4edf314ed8d636578b2e5492867086c8fd6121a39c25cb7657c597ca002b5` | 5 |
| `Cna.Core.Tests.Campaigns.CombatDependencySnapshotTests.ConsumptionRevalidationTraversesParentsAndOwnsCurrentBytes(revision: "legacy")` | `ba85c38a95545606a6e2a2363b3dabe40794758a4092e82cd2676226a32c131a` | 1 |

Snapshot reconciliation adds one measured row; existing weights unchanged.

| Test | Key prefix | Observed ms |
| --- | --- | ---: |
| `Cna.Core.Tests.Campaigns.CombatActualMovedAssaultContractTests.MergedPr334RevisionRetainsExactReplayAndRejectsMixedSnapshotDependencies` | `6e8267a4bbcb` | 5007 |
## PR 345: Graziani setup and arrival coverage

The PR 345 hosted prepare artifact for merge SHA `80781740235e9837bfaf40a16f6d65293febaeb1`
discovered 212 Core rows absent from the existing manifest and no removed rows.
All 212 exact display names were selected from that inventory and measured on
macOS arm64 with .NET 10 Release, MTP xUnit XML and two collection threads:
`python3 /private/tmp/sandtable-pr345-measure.py`. The run passed 212/212 in
1m 03s; the two longest source-authority rows measured 33,038 and 27,814 ms.
The Release test DLL SHA-256 was
`bcfb60e719e0addf2f365a6b2c32994aed1beac5ca975bf9f14755dcdfaaac9f`;
the local report under `artifacts/pr345-ci-timing` had SHA-256
`4135d0937c5af0c39995e8d4a2a68b4b8b691e64c71a71ab0c23f96f109a6936`.
Prior weights remain unchanged; new rows are marked provisional pending hosted
calibration. The complete discovered inventory still allocates to 61 groups.
These timings do not establish hosted performance or exact-head CI success.
## Graziani replay stress session

The default source-authority theory now exercises real setup legal choices and
seeded interleaving; one Core issued-action binding test is new. A local macOS
arm64 .NET 10 Release run of both classes passed 3/3. The existing seed 0 and
seed 14 rows measured 63,337 ms and 24,540 ms; the new binding row measured
11 ms. The optional `SANDTABLE_GRAZIANI_SOAK=1` workload was not enabled for
these CI weights. Command:
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --artifacts-path artifacts/graziani-session-final --filter-class '*CampaignSourceIssuedActionBindingTests*' '*GrazianiSourceAuthorityTests*' --report-xunit-xml --results-directory artifacts/graziani-session-final/ci-timings -p:NuGetAudit=false`.
DLL SHA-256 `1b93e7c8c9027c6673f8210600ff20732a5e0f6195779e2ced08c016a8ce7f2d`;
XML SHA-256 `b73820099b8633553d78adb773b0810d6e2e600fa845ef1f0c228bcb95b8436f`.
The measured rows remain provisional pending hosted calibration.

## Graziani 16th Brigade retained-world occupancy

The new `GrazianiRetainedFormationTests.CairoBrigadeUsesOneCounterAndRetainsOriginalWorld` row passed on macOS arm64, .NET 10 Release, using the direct MTP test DLL with `--filter-class '*GrazianiRetainedFormationTests*' --report-xunit-xml`. Its XML reported 75 ms; row key prefix `dc60686d42d6`. Test DLL SHA-256 `be54c7a9c131fcfff0f673b4a0d7d48aa21db71fd9424ed89e0151eedb3481b3`; XML SHA-256 `447f0572de3dc46a39e701788cae34515272b0fdcdb9cb6727aa1e09301ca646`. Timing remains provisional pending hosted calibration.

The expanded source-authority theory passed 2/2 in the same build, selected with `--filter-class '*GrazianiSourceAuthorityTests*' --report-xunit-xml`. Seed 0 measured 81,156 ms and seed 14 measured 38,533 ms; updated existing weights include concurrent local load. XML SHA-256 `1ee26e7e0bf53a7ed03fa451a35fb17a841f43b777bee4b7a437aaaa697feca9`.

After v42 Catanzaro coverage and the v41 altered-event regression, the source-authority theory again passed 2/2 with the same selector and XML reporting. Seed 0 measured 78,751 ms and seed 14 measured 46,140 ms; these replace the earlier provisional source-theory weights. Concurrent local workload was present. DLL SHA-256 `79ec0e53c48ce2d945be6b5030adf1217f6c78b585ad8d6bd467c13e97a62b3d`; XML SHA-256 `b3e857661eef55fd4cd662f172639f9a9b403bb98944ec5cbcf6e779c32cd024`.

After source-backed Polish retained-counter integration, the retained-formation
row measured 101 ms. Test DLL SHA-256
`cc08563a8e1be770066ace56b8a038c0da57a9675bb545f48eec5e9d0380f046`;
XML SHA-256 `26c126115904d25dce033324eae42c13152b2f9947f1a5d603bef225172b0a32`.
This macOS measurement is provisional pending hosted calibration.

After the bounded Polish arrival contact projection was integrated, the same
retained-formation row measured 97 ms on macOS arm64, .NET 10 Release. Test DLL
SHA-256 `3aab04b02079843940ec26b8c6cc52b2707ffbbd8b0f4cf137246dd25d84626b`;
XML SHA-256 `d60394c98d0824782549b561a47c4dc21cc7c481eec3d0f46aa2d82545928f0a`.
Hosted calibration remains outstanding.

After source owner-finalization checkpoints, side-lock/replay checks and the
real Polish atomic-successor regression, the source-authority theory passed
2/2 on macOS arm64, .NET 10 Release. Seed 0 measured 76,084 ms and seed 14
39,141 ms under concurrent local work. Test DLL SHA-256
`d67f88656fbb18f3f49bd81edf3bb228e2a5c8a0348b9eeb86bd4ae74588b5c0`;
XML SHA-256 `2580bec72fe0f1877b125736d2d751dab1aa62d014d293dab7dc2e968304c2db`.
Hosted exact-head timing remains unverified.

After source-arrival contact projection, optional-plan readiness checks and
the two-order GT1 supply-roll regression, the real-source theory again passed
2/2 on macOS arm64, .NET 10 Release. Seed 0 measured 76,313 ms and seed 14
45,636 ms. Test DLL SHA-256
`c6b5608acf166b430392cebed34fbfc292e327203ecba3a70ec3e218f4151c44`;
XML SHA-256 `d865429c8b8f46c2ec784a541c6b76aed66f007279baa374a547c4c087d1234c`.
Concurrent local test activity was present; hosted timing remains unverified.

The source-turn issued-action binding row passed on macOS arm64, .NET 10 Release,
with `--filter-class '*CampaignSourceIssuedActionBindingTests*' --report-xunit-xml`.
The Polish destination/version binding row measured 10 ms (row key prefix
`afec9a1b1607`); both selected tests passed. Core test DLL SHA-256
`fed68b1a6635030fcf1981fe564e71be079b22e8ae41a588c6b638ef2044a176`;
XML SHA-256 `f5a1b64d1ac507d87312c0496996bd9af864c3367c69b7fad503a953a2b5f7a6`.
Timing remains provisional pending hosted calibration.

The same source-turn binding row was rerun after adding GT1 supply, tactical
shipping and initiative choices. Both selected tests passed; the extended row
measured 9 ms. Test DLL SHA-256
`d734fd723219f2fb720ee21756650af661154099e8b282d24d2d1ecb3e284a64`;
XML SHA-256 `ffdf2b05241fd0f00108937349114de0992abe4227ca20a91e0e4d6d5a368782`.

The expanded real-source theory including Australian placement, source replay,
and CW gun stacking passed 2/2 on macOS arm64, .NET 10 Release. The direct MTP
XML recorded 116,587 ms (seed 0) and 51,548 ms (seed 14). Test DLL SHA-256
`e4eaaa5fed578125db235c6f2daee25d0fa26c72c8790cbe905fc9ad18f81007`; XML SHA-256
`159975f3e235c5cc0cd5ac32d8a298c93513e19128b91057b24ac79b934d26de`.
Command: `dotnet artifacts/source-training/bin/Cna.Core.Tests/release/Cna.Core.Tests.dll --filter-method '*AxisFinalizesRealSupplyPlanAndReplaysWithoutStartingGame*' --report-xunit-xml --results-directory artifacts/source-training/results`.
These timings precede subsequent source-turn edits and remain provisional pending
hosted calibration.

The real-source theory including sole-lane Benghazi, fourth-Indian source
successors and the Axis batch passed 2/2 on macOS arm64, .NET 10 **Debug**,
before the subsequent seventh-Armored shell correction. The direct MTP XML
recorded 104,495 ms (seed 0) and 55,951 ms (seed 14). The seed 0 weight
retains the higher prior Release measurement; the seed 14 weight uses the
Debug measurement as a provisional estimate. Test DLL
SHA-256 `e471367797daf5480f2f7be0ba8a20c0ae8364f885241847facbdbae58977df8`,
Core DLL SHA-256
`7379f1bfde929a546a93eff768cf99fd4fec0390da008a4e57b5a631c7fa8b11`,
XML SHA-256 `0c7f8288bf04b44bae64fff1b9719c028256ae512fcd9954f742bc68bf62e4c0`.
Command: `dotnet artifacts/polish-engine-session/final-capture/bin/Cna.Core.Tests.dll --filter-method '*AxisFinalizesRealSupplyPlanAndReplaysWithoutStartingGame' --report-xunit-xml --results-directory artifacts/polish-engine-session/final-capture/results --no-ansi --progress off`.
Rerun in Release after the shell correction; hosted exact-head timing remains
unverified.

The post-shell, pre-Aresca source theory then passed 2/2 on macOS arm64,
.NET 10 Release. Seed 0 measured 112,711 ms and seed 14 measured 73,291 ms.
The test DLL SHA-256 was
`ebc508aeb2cb0ef828765fd3d75608636775a68032bd5be2f9a0942d40719bfb`,
Core DLL SHA-256
`f2d938ba760522ce4cc3d9e7d0d384cecd97f33f4e3c304b3cfbfa89c2c65a1c`,
and XML SHA-256
`75361048e46fc2cfaf0547a3fabe02226a7346bfe60d954c1884eafc71a19ffc`.
Command: `dotnet artifacts/polish-engine-session/post-shell-final-release/bin/Cna.Core.Tests.dll --filter-method '*AxisFinalizesRealSupplyPlanAndReplaysWithoutStartingGame' --report-xunit-xml --results-directory artifacts/polish-engine-session/post-shell-final-release/results --no-ansi --progress off`.
Subsequent Aresca and multi-lane changes are outside this capture; hosted
exact-head timing remains unverified.

The post-seam-safety, pre-candidate Release source theory passed 2/2 on
macOS arm64. Seed 0 measured 121,191 ms and seed 14 measured 83,779 ms.
The test DLL SHA-256 was
`f7b7f635259ca95a0f7118aed6bb7cb2b71ab7c3ad5392758a739caea087afe0`,
Core DLL SHA-256
`99c59bc10c91d345e1a91b661d087c054dd361ee640af0bfbd2a60049f3767ab`,
and XML SHA-256
`be61ec8c8c5e4217700abae02057b3cba07d8e723762248d688638558e4e9b93`.
Command: `dotnet artifacts/polish-engine-session/seam-safe-release/bin/Cna.Core.Tests.dll --filter-method '*AxisFinalizesRealSupplyPlanAndReplaysWithoutStartingGame' --report-xunit-xml --results-directory artifacts/polish-engine-session/seam-safe-release/results --no-ansi --progress off`.
This includes Aresca and multi-lane source tests but precedes the provisional
seam-candidate veto; hosted exact-head timing remains unverified.

The chart-pair and lane6 Release source theory passed 2/2 on macOS arm64.
Seed 0 measured 124,350 ms and seed 14 measured 96,830 ms. The test DLL
SHA-256 was `3a240d51f8b5491e33dfe3803c5086ce1628b7fd424df1996108314de689d46d`,
Core DLL SHA-256
`c2ee4bfbab9be8d443f3e87c68df30b4b0b5f7524789fbedc37163bdcf20172d`,
and XML SHA-256
`fff50eb29dce6f005e63e2668125a1c36feb54dbf76320cad8052449c768ae42`.
Command: `dotnet artifacts/polish-engine-session/chart-pairs-lane6-release/bin/Cna.Core.Tests.dll --filter-method '*AxisFinalizesRealSupplyPlanAndReplaysWithoutStartingGame' --report-xunit-xml --results-directory artifacts/polish-engine-session/chart-pairs-lane6-release/source-results --no-ansi --progress off`.
Later garrison work is outside this capture; hosted exact-head timing remains
unverified.

The new Australian provisional-baseline consensus class passed 3/3 in a local
macOS arm64 .NET 10 Release direct MTP run with concurrent work. The codec row
measured 25 ms; the two owner-order rows measured 3 ms and below 1 ms (stored as
1 ms). XML SHA-256 `b2bc4ede977cef2c09cc5627c2d1cd20bd790933d1ead53e9681845f2177272a`;
test DLL SHA-256 `93e9ca4dd056dd32550050ec53df95ec8d4e7c4c8042afdde18ffccb89208022`.
These are provisional scheduling observations, not hosted CI or live Host replay evidence.

The two real-source Horse setup consensus/replay rows passed locally on macOS
arm64, .NET 10 Release, measuring 214 ms and 51 ms with concurrent work.
XML SHA-256 `4fda3c52cab2e0acfa0105b38461eafd16d599249e5721c4190f145c97d4a3e2`;
test DLL SHA-256 `a1979e8475d10524d03469ef0ebe6c5f52e62562c3da39f7b0292c4eba354393`.
These are provisional local scheduling observations; hosted CI remains required.

After adding unrelated Axis dump and owner-finalization/reopen propagation to
those same Horse rows, a second local Release run passed 2/2 at 237 ms and
73 ms. XML SHA-256 `7981ae36c55b3aa325fba446be471346cc874612509e056465e58611338b3b54`;
test DLL SHA-256 `c1df53a3df961c1e5a70448f2ed4767ef0a23d134bc720cb889c14d9be9270ef`.
Timing remains provisional and is not an admitted-turn or hosted-CI claim.

After adding both owners' dummy choices and Axis motorization to the real Horse
setup/replay regression, the two rows passed locally on macOS arm64, .NET 10
Release, measuring 296 ms (Axis first) and 121 ms (Commonwealth first). Test DLL
SHA-256 `bb5e28e11ddaaa9de1bbf6b5511bb164ce39c1ce60ac4a2d379a572b11e489cc`,
Core DLL SHA-256 `81fe6d82f26a6310b84a3b7c93211511b2c03e9fcd0def524384fce216f7eee5`,
XML SHA-256 `23c7d30ee00dbabc438b2b0ca15d1ef37a897961f72274f40efd20214ec70062`.
Later observer-v2 edits are excluded; hosted timing remains unverified.

The real-source theory including independent Axis garrison activation, event
tampering and both subsequent Maletti/Matruh promotion orders passed 2/2 on
macOS arm64, .NET 10 Release, under concurrent local load. Seed 0 measured
147,818 ms and seed 14 measured 87,520 ms. Test DLL SHA-256
`bb5e28e11ddaaa9de1bbf6b5511bb164ce39c1ce60ac4a2d379a572b11e489cc`,
Core DLL SHA-256 `81fe6d82f26a6310b84a3b7c93211511b2c03e9fcd0def524384fce216f7eee5`,
XML SHA-256 `ad7bc8719fc81017f47cc668d1ed7a336051caa646004ec147446e8c99dcc113`.
This capture predates later observer-v2 edits and is not hosted CI evidence.

The subsequent source theory including source-backed v2 observer pairs and the
compact version-60 checkpoint fix passed 2/2 on macOS arm64, .NET 10 Release.
Seed 0 measured 127,181 ms and seed 14 measured 81,757 ms. Test DLL SHA-256
`68a1a301aa813293a5021f9ec1dc4d6604e7ba2d789f72ade9ca00a3c124d0b2`,
Core DLL SHA-256 `6afd41ebe3319b54aa50e5e572e95b776450fb2da07eb6d170767e8d282e707d`,
XML SHA-256 `88fa3070f43bebd4f829e077e7f60e9f4932ffe2a8c36945271f72ee0155df96`.
These are local timings; hosted CI remains unverified.

The version-61 garrison successor and streaming legacy-projection hash source
theory passed 2/2 on macOS arm64, .NET 10 Release. Seed 0 measured 155,781 ms
and seed 14 measured 97,592 ms. Test DLL SHA-256
`a48f56b8c14565486fe3b991596fdc5e40eba0cf7d3e2edcfa15f4d284b2ec19`,
Core DLL SHA-256 `b2d3a83e7e51786c14aed781e29c82df876ba729b6cf991c8e0e22ecc64737d6`,
XML SHA-256 `7579cfdb75464c8e7c662a68a5b0d077541fd658b3ac0aba8d41e1a466f9784d`.
This capture predates later v61 observer-selection coverage and is not hosted CI.

The corrected v61 Land-only capture (six active garrisons, two `uu` AA units
removed, independent-counter observers) passed both source-theory seeds on
macOS arm64, .NET 10 Release. Seed 0 measured 166,841 ms and seed 14 measured
103,543 ms. Test DLL SHA-256
`62769d670ebb6881d02987882a8acb1dc8f6514f64d2998ca9e91364b744cd8f`,
Core DLL SHA-256 `d7a086133747aebc79d0c16a1f268ad98257a76a3e84a7b06232e116d64627dd`,
XML SHA-256 `fbfc460e53ef23dceb78c4e430531a033a61cd04d9c826ae48aa7ad4ef38dd7a`.
This capture precedes the optional v62 HH setup successor and is not hosted CI.


The final v63 hash-cache real-source theory passed 2/2 on macOS arm64, .NET 10
Release. Seed 0 measured 148,998 ms and seed 14 measured 114,078 ms, under
concurrent local load; these measurements do not establish a performance
improvement. Test DLL SHA-256
`c405c95498bd41e985cb455c693fcb18716d15148eab568def23a69edd016f9b`,
Core DLL SHA-256 `c3dfbc6364a974884cd76c3449f12a398017e67087e3808a1708eb61f2262c40`,
XML SHA-256 `6d670bb2c97e93f81f904f66cc69eaf21481d1d9ec5e40c7f5e833962b298bf1`.
No hosted CI evidence is represented by this local capture.

The source theory with the versioned Giarabub motor-placement policy passed
2/2 on macOS arm64, .NET 10 Release, using frozen binaries under concurrent
local load. Seed 0 measured 157,163 ms (row key
`7a1fb0272a61572cde2270476c855b901dd58d877f8c3e6488633cf747e8c083`);
seed 14 measured 104,903 ms (row key
`8d72e2f0723c7d2d45ac667dcca0139281ce46660e43f387f75432bd80bd6d0b`).
Test DLL SHA-256 `5d656c6ced7e0d6c79d3511efc7066a6e5be51d14790eea00a150d9469da054a`,
Core DLL SHA-256 `0b05432f945a16040965b764c31587ba8608538ce6d2cd3a82dd7d3b93633944`,
and shared XML SHA-256 `b08cbbf18d95840bd5072c846c454034599dee5449c115fc76c21bd2398f2de6`.
Command: `dotnet /private/tmp/graziani-core-policy-test-capture/Cna.Core.Tests.dll --filter-method '*AxisFinalizesRealSupplyPlanAndReplaysWithoutStartingGame*' --report-xunit-xml --results-directory /private/tmp/graziani-core-policy-timing --no-ansi --progress off`.
This local capture is not hosted CI evidence.
