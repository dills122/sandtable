#!/usr/bin/env python3
"""Audit BRK-001's frozen references and migration obligations; not a Core test."""
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / 'docs/specs/breakdown-fixture-migration.v1.json'


def require(condition, message):
    if not condition:
        raise SystemExit(f'FAIL: {message}')


def baseline_bytes(commit, path):
    return subprocess.check_output(['git', 'show', f'{commit}:{path}'], cwd=ROOT)


def unique(values, label):
    require(len(values) == len(set(values)), f'duplicate {label}')


def audit():
    data = json.loads(CATALOG.read_text())
    require(data['schemeId'] == 'sandtable.spec.breakdown-fixture-migration.v1', 'scheme')
    require(data['status'] == 'frozen-obligations-not-implementation-evidence', 'status')
    base = data['baselineCommit']
    require(re.fullmatch('[0-9a-f]{40}', base), 'immutable baseline commit')
    paths = subprocess.check_output(
        ['git', 'ls-tree', '-r', '--name-only', base, 'scenarios'], cwd=ROOT, text=True
    ).splitlines()
    paths = {p for p in paths if p.endswith('.json')}
    rows = data['scenarioFiles']
    unique([r['path'] for r in rows], 'fixture path')
    require({r['path'] for r in rows} == paths, 'missing/extra baseline fixture disposition')
    for row in rows:
        old = baseline_bytes(base, row['path'])
        require(hashlib.sha256(old).hexdigest() == row['sha256'], row['path'] + ' baseline hash')
        require((ROOT / row['path']).read_bytes() == old, row['path'] + ' historical bytes changed')
        require(row['originalDisposition'] == 'historical-at-activation', 'historical disposition')
        require(row['capabilityDisposition'] in {'successor', 'partial-successor'}, 'fixture disposition')
        require(row['successorPath'] != row['path'], 'relabeling old fixture')
        require(row['requiredEvidence'] and row['ownerTasks'] == [
            'BRK-TASK-003', 'BRK-TASK-006', 'BRK-TASK-007'], 'unowned migration')
    unique([r['successorPath'] for r in rows], 'successor path')
    allowed_paths = paths | {r['successorPath'] for r in rows}
    # Task 007 records new public-evidence scenarios separately; the frozen baseline
    # inventory and every original byte remain unchanged.
    results_path = ROOT / 'docs/research/breakdown-runner-migration-results.json'
    if results_path.exists():
        results = json.loads(results_path.read_text())
        require(results['sourceInventory'] == str(CATALOG.relative_to(ROOT)), 'closeout inventory source')
        supplemental = results['supplementalScenarioFiles']
        unique([row['path'] for row in supplemental], 'supplemental fixture')
        for row in supplemental:
            require(row['path'] not in allowed_paths, 'supplemental fixture relabels baseline/successor')
            require(row['requiredEvidence'] and set(row['requiredEvidence']) <= set(data['newPublicEvidence']),
                    'supplemental fixture has unknown public evidence')
            require((ROOT / row['path']).is_file(), 'missing supplemental fixture')
            allowed_paths.add(row['path'])
    current_paths = {str(p.relative_to(ROOT)) for p in (ROOT / 'scenarios').rglob('*.json')}
    require(current_paths <= allowed_paths, 'current fixture omitted from migration inventory')

    reaction = json.loads(baseline_bytes(base, 'scenarios/maneuvers/rules-lab.reaction.serial.v2.json'))
    children = data['reactionChildren']
    unique([c['exerciseId'] for c in children], 'Reaction child')
    require({c['exerciseId'] for c in children} == {
        e['exerciseId'] for e in reaction['exercises']}, 'missing/extra Reaction child')
    for child in children:
        deferred = child['capabilityDisposition'] == 'deferred-public'
        require(child['capabilityDisposition'] in {'successor', 'deferred-public'}, 'child disposition')
        require(deferred == (child['successorExerciseId'] is None), 'deferred child relabeled')
        require(child['requiredEvidence'], 'child evidence absent')
        positive_zoc = any(x in child['exerciseId'] for x in ('positive-zoc', 'remote-zoc'))
        require(deferred == positive_zoc, 'positive ZOC conflicts with certified size bound')
    unique([c['successorExerciseId'] for c in children if c['successorExerciseId']], 'successor child')

    sources = data['inheritedSchemaSources']
    unique([s['path'] for s in sources], 'inherited codec')
    for source in sources:
        require(hashlib.sha256(baseline_bytes(base, source['path'])).hexdigest() ==
                source['sha256'], source['path'] + ' inherited schema hash')

    spec = ROOT / 'docs/specs/breakdown-adjudication-v1.md'
    wire = ROOT / 'docs/specs/breakdown-wire-contract-v1.md'
    text = spec.read_text()
    require(set(re.findall(r'^### (BRK-REQ-\d{3})', text, re.M)) == {
        f'BRK-REQ-{n:03}' for n in range(1, 11)}, 'requirement set')
    ac = re.findall(r'^\| (BRK-AC-\d{3}) \| (.*?) \| (.*?) \|', text, re.M)
    unique([r[0] for r in ac], 'acceptance ID')
    require({r[0] for r in ac} == {f'BRK-AC-{n:03}' for n in range(1, 13)}, 'acceptance set')
    for _, references, tasks in ac:
        require(all(1 <= int(n) <= 10 for n in re.findall(r'REQ-(\d+)', references)), 'requirement ref')
        require(all(2 <= int(n) <= 7 for n in tasks.split(',')), 'task ref')
    require(data['capabilityProfileId'] in text and data['capabilityProfileId'] in wire.read_text(),
            'inconsistent profile ID')

    # Local file links only: anchors/external URLs deliberately outside this audit.
    docs = [spec, wire, ROOT / 'docs/design/breakdown-adjudication-v1.md',
            ROOT / 'docs/research/breakdown-adjudication-spike.md']
    link_count = 0
    for doc in docs:
        for target in re.findall(r'\[[^\]]*\]\(([^)]+)\)', doc.read_text()):
            if '://' in target or target.startswith('#'):
                continue
            require((doc.parent / target.split('#', 1)[0]).exists(), f'broken link {doc.name}: {target}')
            link_count += 1
    deferred_count = sum(c['capabilityDisposition'] == 'deferred-public' for c in children)
    print(f'PASS: {len(rows)} historical fixture hashes; {len(children)} Reaction children '
          f'({len(children) - deferred_count} successor obligations, {deferred_count} deferred); '
          f'{len(sources)} inherited schema hashes; 10 requirements / 12 acceptance mappings; '
          f'{link_count} local links.')
    print('Specification audit only; no production acceptance criterion executed.')


if __name__ == '__main__':
    audit()
