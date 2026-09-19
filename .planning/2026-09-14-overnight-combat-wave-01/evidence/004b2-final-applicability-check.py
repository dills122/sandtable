#!/usr/bin/env python3
"""Recheck retained native-run applicability without executing native catalogs."""
import ast
import hashlib
import json
from pathlib import Path

EVIDENCE = Path(__file__).resolve().parent
ROOT = EVIDENCE.parents[2]
baseline = json.loads((EVIDENCE / '004b2-predecessor-baseline.json').read_text())
for name, expected in baseline['files'].items():
    assert hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected, name

comparison = json.loads((EVIDENCE / '004b2-root-ast-compatibility.json').read_text())
for name, expected in comparison['baselineArtifacts'].items():
    assert hashlib.sha256((EVIDENCE / name).read_bytes()).hexdigest() == expected, name

old = ast.parse((EVIDENCE / '004b2-pre-admission-oracle.py').read_text())
current_path = ROOT / 'docs/specs/verify-combat-exercise-child-evidence-v1.py'
current = ast.parse(current_path.read_text())
old_functions = {node.name: ast.dump(node) for node in old.body if isinstance(node, ast.FunctionDef)}
current_functions = {node.name: ast.dump(node) for node in current.body if isinstance(node, ast.FunctionDef)}
shared = old_functions.keys() & current_functions.keys()
changed = sorted(name for name in shared if old_functions[name] != current_functions[name])
assert changed == comparison['changedFunctions'], changed
assert sum(old_functions[name] == current_functions[name] for name in shared) == 44

# Exact reviewed code/schema hashes bind the previously reviewed narrow deltas too.
expected_current = {
    'docs/specs/verify-combat-exercise-child-evidence-v1.py':
        'a7385011125ca9105a1fbd3197c2b0b7b1825bf921e479ce9f953a4950343c92',
    'docs/specs/combat-exercise-child-evidence-v1.schema.json':
        'cc8a7c9e89068b9a78f3abb4044230ccc9ef359665198aaa7ef6db5b15782bf6',
}
for name, expected in expected_current.items():
    assert hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected, name
old_schema = json.loads((EVIDENCE / '004b2-pre-admission-schema.json').read_text())
new_schema = json.loads((ROOT / 'docs/specs/combat-exercise-child-evidence-v1.schema.json').read_text())
preserved = [name for name, value in old_schema['objects'].items()
             if new_schema['objects'].get(name) == value]
assert len(preserved) == 15, preserved
assert set(new_schema['objects']) - set(old_schema['objects']) == {'SourceSummary'}
print('PASS final applicability:', len(baseline['files']),
      'predecessors;44 function bodies;15 schema definitions;exact reviewed oracle/schema hashes')
