"""Merge-friendly long-suite timing manifest (directory of JSON Lines entry files).

Layout of `.github/scripts/long-suite-weights/`:

  meta.json          schema_version, baseline_*, weight_units, supplemental_measurements.
                     Rarely changes; never touched when a PR only adds test rows.
  *.jsonl            Entry files, one self-contained JSON object per line:
                       {"class": "<fully qualified class>", "parts": <int>}
                       {"row": "<64 hex row_key>", "ms": <int>}
                       {"row": "<64 hex row_key>", "ms": <int>, "provisional": "<note>"}
                     Files are merged in name order; a class or row may appear in exactly one
                     file. `baseline.jsonl` holds the migrated historical rows. A PR that adds
                     tests adds ONE NEW file (`python3 long_suite_weights.py add ...`), so
                     independent PRs never edit the same lines and git merges them cleanly. Every
                     line is valid on its own, so an auto-merge cannot produce invalid JSON.

`load()` rebuilds exactly the dict the old single `long-suite-weights.json` held, so
`long_suite_partition.make_plan` (and every guard in it) is unchanged. A plain `.json` file path is
still accepted by `load()` for transition/test use.
"""
import argparse
import json
import re
import sys
from pathlib import Path

DEFAULT_DIR = Path(__file__).with_name('long-suite-weights')
KEY = re.compile('[a-f0-9]{64}')
NAME = re.compile(r'[a-z0-9][a-z0-9._-]*\.jsonl')
BASELINE = 'baseline.jsonl'


def _dump(value):
    return json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=False)


def parse_records(path):
    records = []
    for number, line in enumerate(Path(path).read_text().splitlines(), 1):
        if not line.strip():
            raise ValueError(f'{path}:{number}: blank line')
        try:
            record = json.loads(line)
        except json.JSONDecodeError as error:
            raise ValueError(f'{path}:{number}: invalid JSON: {error}') from error
        if not isinstance(record, dict):
            raise ValueError(f'{path}:{number}: record is not an object')
        keys = set(record)
        if keys == {'class', 'parts'} and isinstance(record['class'], str) and type(record['parts']) is int:
            pass
        elif keys in ({'row', 'ms'}, {'row', 'ms', 'provisional'}) and isinstance(record['row'], str) and KEY.fullmatch(record['row']) and type(record['ms']) is int and (keys == {'row', 'ms'} or isinstance(record['provisional'], str)):
            pass
        else:
            raise ValueError(f'{path}:{number}: unsupported record shape: {line[:120]}')
        records.append(record)
    return records


def load(path=DEFAULT_DIR):
    """Return the legacy-shaped weights dict from a manifest directory (or a legacy .json file)."""
    path = Path(path)
    if path.is_file():
        return json.loads(path.read_text())
    meta = path / 'meta.json'
    if not meta.is_file():
        raise ValueError(f'{path}: missing meta.json')
    weights = json.loads(meta.read_text())
    if not isinstance(weights, dict) or {'classes', 'rows', 'provisional_rows'} & set(weights):
        raise ValueError('meta.json must not carry classes/rows/provisional_rows')
    weights.update({'classes': {}, 'rows': {}, 'provisional_rows': {}})
    where = {}
    for entry in sorted(path.glob('*.jsonl')):
        if not NAME.fullmatch(entry.name):
            raise ValueError(f'{entry.name}: entry file names must match {NAME.pattern}')
        for record in parse_records(entry):
            if 'class' in record:
                section, key, value = 'classes', record['class'], {'parts': record['parts']}
            else:
                section, key, value = 'rows', record['row'], record['ms']
            if key in where.get(section, {}):
                raise ValueError(f'Duplicate {section[:-1]} {key} in {where[section][key]} and {entry.name}')
            where.setdefault(section, {})[key] = entry.name
            weights[section][key] = value
            if 'provisional' in record:
                weights['provisional_rows'][key] = record['provisional']
    for key in weights['provisional_rows']:
        if key not in weights['rows']:
            raise ValueError(f'provisional row {key} has no measured weight')
    return weights


def records_of(weights):
    """Canonical (sorted) record list for a legacy-shaped dict."""
    records = [{'class': name, 'parts': value['parts']} for name, value in sorted(weights['classes'].items())]
    for key, ms in sorted(weights['rows'].items()):
        record = {'row': key, 'ms': ms}
        if key in weights.get('provisional_rows', {}):
            record['provisional'] = weights['provisional_rows'][key]
        records.append(record)
    return records


def write_entry_file(path, records):
    Path(path).write_text(''.join(_dump(record) + '\n' for record in records))


def migrate(source, directory):
    """One-command migration: old single JSON file -> directory. Proves zero semantic change."""
    source, directory = Path(source), Path(directory)
    original = json.loads(source.read_text())
    if any(set(value) != {'parts'} for value in original['classes'].values()):
        raise ValueError('class entries carry fields other than parts; extend the format first')
    if not set(original['provisional_rows']) <= set(original['rows']):
        raise ValueError('provisional_rows outside rows; extend the format first')
    directory.mkdir(parents=True, exist_ok=True)
    meta = {key: value for key, value in original.items() if key not in ('classes', 'rows', 'provisional_rows')}
    (directory / 'meta.json').write_text(json.dumps(meta, indent=2, ensure_ascii=False) + '\n')
    write_entry_file(directory / BASELINE, records_of(original))
    loaded = load(directory)
    if loaded != original or _dump(loaded) != _dump(original):
        raise ValueError('Migration changed the loaded weight table')
    return {'rows': len(loaded['rows']), 'classes': len(loaded['classes']), 'provisional_rows': len(loaded['provisional_rows']), 'canonical_sha256': _sha(loaded)}


def _sha(weights):
    import hashlib
    return hashlib.sha256(_dump(weights).encode()).hexdigest()


def reconcile(weights, core_rows):
    """Return (missing_keys, stale_keys, missing_classes, stale_classes) versus actual Release rows."""
    from long_suite_partition import row_key
    actual = {row_key(row): row for row in core_rows}
    classes = {row[1].rsplit('.', 1)[0] for row in core_rows}
    return ({key: actual[key] for key in set(actual) - set(weights['rows'])}, sorted(set(weights['rows']) - set(actual)),
            sorted(classes - set(weights['classes'])), sorted(set(weights['classes']) - classes))


def measured_ms(xml_directory):
    """row display name -> elapsed ms from MTP/xUnit XML reports (same extraction as the refresh recipe)."""
    import xml.etree.ElementTree as ET
    observed = {}
    for path in Path(xml_directory).rglob('*.xml'):
        assembly = ET.parse(path).getroot().find('assembly')
        if assembly is None:
            continue
        for test in assembly.findall('.//test'):
            name = json.loads('"' + test.attrib['name'] + '"')
            observed[(test.attrib['type'] + '.' + test.attrib['method'], name)] = max(1, round(float(test.attrib['time']) * 1000))
    return observed


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest='command', required=True)
    p = sub.add_parser('migrate', help='convert the old single JSON manifest into the directory format')
    p.add_argument('source'); p.add_argument('directory', nargs='?', default=str(DEFAULT_DIR))
    p = sub.add_parser('check', help='list rows/classes that differ from an r02_partition.py discover proof directory')
    p.add_argument('--proof', required=True); p.add_argument('--weights', default=str(DEFAULT_DIR))
    p = sub.add_parser('add', help='write a NEW entry file registering every missing row/class (do not edit baseline.jsonl)')
    p.add_argument('--proof', required=True, help='directory from `r02_partition.py discover` holding r02-inventory.json')
    p.add_argument('--name', required=True, help='entry file topic, e.g. issue-578-cursor-contract (becomes <name>.jsonl)')
    p.add_argument('--note', required=True, help='provenance recorded as the provisional note on each new row')
    p.add_argument('--xml', help='directory of MTP XML reports measuring the new rows; unmeasured rows use --default-ms')
    p.add_argument('--default-ms', type=int, default=1000)
    p.add_argument('--weights', default=str(DEFAULT_DIR))
    p = sub.add_parser('export', help='print the legacy-shaped JSON (for diffing/inspection)')
    p.add_argument('--weights', default=str(DEFAULT_DIR))
    args = parser.parse_args(argv)
    if args.command == 'migrate':
        print(json.dumps(migrate(args.source, args.directory)))
        return 0
    weights = load(args.weights)
    if args.command == 'export':
        print(json.dumps(weights, indent=2, sort_keys=True))
        return 0
    sys.path.insert(0, str(Path(__file__).parent))
    core = [tuple(row) for row in json.loads((Path(args.proof) / 'r02-inventory.json').read_text())['legacy_core_rows']]
    missing, stale, new_classes, gone_classes = reconcile(weights, core)
    print(f'missing rows: {len(missing)}; stale rows: {len(stale)}; new classes: {len(new_classes)}; stale classes: {len(gone_classes)}')
    if args.command == 'check':
        for key, row in sorted(missing.items(), key=lambda item: item[1]): print('  missing', key[:12], row[2])
        for key in stale: print('  stale', key)
        return 1 if missing or stale or new_classes or gone_classes else 0
    if stale or gone_classes:
        print('Stale rows/classes need a manual edit of the file that holds them (remove only those lines); not auto-removed.', file=sys.stderr)
    name = args.name if args.name.endswith('.jsonl') else args.name + '.jsonl'
    if not NAME.fullmatch(name):
        raise SystemExit(f'--name must match {NAME.pattern}')
    target = Path(args.weights) / name
    if target.exists():
        raise SystemExit(f'{target} exists; choose a new --name (one new file per PR keeps merges conflict-free)')
    observed = measured_ms(args.xml) if args.xml else {}
    records = [{'class': name_, 'parts': 1} for name_ in new_classes]
    for key, row in sorted(missing.items()):
        records.append({'row': key, 'ms': observed.get((row[1], row[2]), args.default_ms), 'provisional': args.note})
    write_entry_file(target, sorted(records, key=lambda r: (('row' in r), r.get('class') or r['row'])))
    load(args.weights)  # re-validate the merged result
    print(f'wrote {target} ({len(records)} records)')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
