#!/usr/bin/env python3
"""Bounded, private P01 replay diagnostics. Never admits an action."""
import argparse
import copy
import gzip
import hashlib
import importlib.util
import json
from collections import Counter
from pathlib import Path
from unittest import mock

ROOT = Path(__file__).resolve().parent
EVIDENCE = Path('/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence')
FIXTURE = ROOT / 'fixtures/combat-p01-replay-gates-v1.json'


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


p01 = load('p01_replay_reference', 'verify-combat-actual-moved-repeat-position-v1.py')
ancestry = load('p01_replay_ancestry', 'verify-combat-actual-moved-repeat-position-evidence-v1.py')


def require(condition, label):
    if not condition:
        raise ValueError('CMB-P01-REPLAY ' + label)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def source_rows(evidence=EVIDENCE):
    """Yield only after pins, catalogue and compressed-chunk hashes are checked."""
    evidence = Path(evidence)
    for name, expected in ancestry.PINS.items():
        require(sha((evidence / name).read_bytes()) == expected, 'pin ' + name)
    summary = json.loads((evidence / 'summary.json').read_bytes())
    manifest = json.loads((evidence / 'manifest.json').read_bytes())
    partitions = json.loads((evidence / 'bounded-profile-audit.json').read_bytes())
    require(len(summary['rows']) == len(partitions['rows']) == summary['count'] == 300, 'catalogue')
    require(len(manifest['chunks']) == 11, 'chunk count')
    require({entry['name'] for entry in manifest['chunks']} == set(summary['chunkSha256'])
            == {p.name for p in evidence.glob('rows-*.jsonl.gz')}, 'chunk names')
    rows = []
    for chunk in manifest['chunks']:
        name = chunk['name']
        data = (evidence / name).read_bytes()
        require(sha(data) == chunk['sha256'] == summary['chunkSha256'][name], 'chunk hash')
        rows.extend(json.loads(line) for line in gzip.decompress(data).splitlines())
    require(len(rows) == 300, 'row count')
    keys = set()
    for index, row in enumerate(rows):
        require(row['index'] == index, 'row order')
        key = row['r03']['rowKey']
        require(key == summary['rows'][index]['rowKey'] == partitions['rows'][index]['rowKey']
                and key not in keys, 'row key')
        keys.add(key)
        vectors = row['r03']['vectors']
        source = vectors['sourceCanonicalUtf8'].encode('ascii')
        require(sha(source) == row['r03']['sourceHash'].removeprefix('sha256:'), 'source vector')
        yield index, key, partitions['rows'][index]['partition'], source, ancestry.original_ledgers(
            vectors['parentInputLedgersCanonicalUtf8']), [vectors['inputCanonicalUtf8'].encode('ascii')]


def check_assessment(assessment, source_hash, shape):
    require(list(assessment) == p01.SCHEMA['assessmentFields'], 'assessment fields')
    require(assessment['contractVersion'] == 1 and assessment['status'] == 'unsupported'
            and assessment['physicalShape'] == shape and assessment['admitted'] is False
            and assessment['publicActions'] == [] and assessment['sourceHash'] == source_hash,
            'fail-closed assessment')
    require(assessment['placementShapeVerified'] is (shape == 'provisional-positive'),
            'placement shape')
    require((assessment['privateCandidateId'] is not None) is (shape == 'provisional-positive'),
            'private identity')


def reject_before_shape(source, ledgers, own):
    with mock.patch.object(p01, 'physical_shape', side_effect=AssertionError('classified before authentication')):
        try:
            p01.assess(source, ledgers, own)
        except ValueError:
            return
    raise AssertionError('accepted invalid replay')


def bounded_mutations(sample):
    _, _, _, source, ledgers, own = sample
    count = 0
    for index in range(11):
        changed = copy.deepcopy(ledgers)
        if index == 0:
            changed[0]['campaignId'] += '.mutated'
        elif changed[index]:
            changed[index][0] = b'{}'
        else:
            changed[index] = [b'{}']
        reject_before_shape(source, changed, own)
        count += 1
    for index, ledger in enumerate(ledgers[1:], 1):
        for cut in range(len(ledger)):
            changed = copy.deepcopy(ledgers)
            changed[index] = changed[index][:cut]
            reject_before_shape(source, changed, own)
            count += 1
    reject_before_shape(source, ledgers, [])
    count += 1
    for mutate in (lambda s: s[:-1] + b' ', lambda s: s + b'\n'):
        reject_before_shape(mutate(source), ledgers, own)
        count += 1
    changed = copy.deepcopy(ledgers)
    changed[7], changed[8] = changed[8], changed[7]
    require(changed[7] != ledgers[7] and changed[8] != ledgers[8], 'distinct ledgers')
    reject_before_shape(source, changed, own)
    count += 1
    for changed_own in ([b'{}'], own + own):
        reject_before_shape(source, ledgers, changed_own)
        count += 1
    with mock.patch.object(p01.r03, 'verify_dependencies', side_effect=ValueError('dependency fault')):
        reject_before_shape(source, ledgers, own)
        count += 1
    with mock.patch.dict(p01.r03.PINS, {p01.SCHEMA['contentPath']: '0' * 64}):
        reject_before_shape(source, ledgers, own)
        count += 1
    return count


def audit(evidence=EVIDENCE, expected=None):
    expected = expected if expected is not None else json.loads(FIXTURE.read_bytes())
    outputs = []
    counts = Counter()
    max_bytes = 0
    samples = {}
    for index, key, shape, source, ledgers, own in source_rows(evidence):
        before = copy.deepcopy((source, ledgers, own))
        assessment = p01.assess(source, ledgers, own)
        check_assessment(assessment, 'sha256:' + sha(source), shape)
        require(before == (source, ledgers, own), 'input mutation')
        payload = p01.canonical(assessment)
        max_bytes = max(max_bytes, len(payload))
        outputs.append(sha(payload))
        counts[shape] += 1
        if index in (0, 36, 168, 299):
            samples[index] = (index, key, shape, source, ledgers, own)
    require(counts == {'provisional-positive': 44, 'provisional-empty': 256}, 'partition')
    require({samples[36][2], samples[168][2]} == {'provisional-positive'}, 'positive sides')
    sides = set()
    retries = mutations = 0
    for index, _, shape, source, ledgers, own in samples.values():
        state = p01.r03.replay(source, ledgers, own)[2]
        sides.add(state['activeCycle']['actingSide'])
        result = p01.assess(source, ledgers, own)
        require(sha(p01.canonical(result)) == outputs[index], 'retry hash')
        require(result['status'] == 'unsupported' and result['admitted'] is False, 'retry status')
        retries += 1
        if index in (36, 168):
            mutations += bounded_mutations((index, '', shape, source, ledgers, own))
    require(sides == {'axis', 'commonwealth'}, 'actor orientations')
    report = {'rows': 300, 'provisionalPositive': 44, 'provisionalEmpty': 256,
              'admittedRows': 0, 'rowHashRoot': sha(p01.canonical(outputs)),
              'maximumCanonicalAssessmentBytes': max_bytes,
              'sampleHashes': {str(index): outputs[index] for index in samples},
              'sampleRetries': retries, 'sampleMutationRejections': mutations,
              'parentCutScope': 'all nonterminal cuts of two authentic positive rows; other rows unproved',
              'status': 'partial AC-008 diagnostic; no P01 admission'}
    if expected:
        for field in ('rows', 'provisionalPositive', 'provisionalEmpty', 'admittedRows',
                      'rowHashRoot', 'maximumCanonicalAssessmentBytes', 'sampleHashes',
                      'sampleRetries', 'sampleMutationRejections'):
            require(report[field] == expected[field], 'fixture ' + field)
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--evidence', type=Path, default=EVIDENCE)
    parser.add_argument('--bootstrap-fixture', action='store_true', help='print initial fixture for review; never writes it')
    args = parser.parse_args()
    print(json.dumps(audit(args.evidence, {} if args.bootstrap_fixture else None), indent=2, sort_keys=True))
