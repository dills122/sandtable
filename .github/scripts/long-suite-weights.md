# Refreshing long-suite scheduling weights

`long-suite-weights.json` contains derived timing hints and hashes of actual runner row identities. It contains no authoritative replay results. The current measurements come from main121baa56820fffce42316b48ac551cb24c012a6c / run38830848442 attempt1. Row duration includes contention and is not CPU time or a capacity bound. Zero-resolution observations receive a1ms scheduling floor.

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
