#!/usr/bin/env python3
"""Prospective Combat rules inputs/timing oracle; no Rules10 or campaign admission."""
import copy
import hashlib
import importlib.util
import json
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
INVENTORY = json.loads((ROOT / 'combat-rules-inputs-v1.schema.json').read_text())
SCHEMA = {k: [tuple(f.split(':')) for f in v.split()] for k, v in INVENTORY['objects'].items()}
KEYS = INVENTORY['sortedArrays']
LIMITS = INVENTORY['limits']
PROFILE = 'sandtable.capability.combat-cycle-infantry.v1'
FALLBACKS = dict(custody='leave-unguarded', **{'force-assignment': 'cancel-incomplete-voluntary-round'},
                rba='cancel-without-decline', **{'reserve-release': 'convert-unresolved-i-retain-ii', 'cycle-control': 'finish-phase'},
                retreat='refuse-retreat', selection='no-selection')


class Invalid(ValueError):
    def __init__(self, code, path=''):
        self.code, self.path = f'CMB-INP-{code:03}', path
        super().__init__(f'{self.code} {path or "/"}')


def require(condition, code, path=''):
    if not condition:
        raise Invalid(code, path)


def encode(value):
    return json.dumps(value, ensure_ascii=True, separators=(',', ':')).encode('ascii')


def digest(raw):
    return 'sha256:' + hashlib.sha256(raw).hexdigest()


def typed(value, kind, path='', depth=0):
    require(depth <= LIMITS['depth'], 1, path)
    if kind in SCHEMA:
        require(type(value) is dict, 1, path)
        for key, child in SCHEMA[kind]:
            require(key in value, 1, path + '/' + key)
            typed(value[key], child, path + '/' + key, depth + 1)
        require(set(value) == {k for k, _ in SCHEMA[kind]}, 1, path)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value) <= LIMITS['arrayItems'], 1, path)
        for i, item in enumerate(value):
            typed(item, kind[:-2], f'{path}/{i}', depth + 1)
    elif kind in ('int', 'utc'):
        require(type(value) is int, 1, path)
        lo, hi = (-2147483648, 2147483647) if kind == 'int' else (LIMITS['utcMinimum'], LIMITS['utcMaximum'])
        require(lo <= value <= hi, 2, path)
    else:
        require(type(value) is str, 1, path)
        if kind == 'hash':
            require(re.fullmatch(r'sha256:[0-9a-f]{64}', value) is not None, 2, path)
        elif kind == 'id':
            require(re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9._:-]{0,127}', value) is not None, 2, path)
        else:
            require(kind == 'text' and 1 <= len(value) <= 512 and all(32 <= ord(c) <= 126 for c in value), 2, path)


def canonical(value, kind):
    if kind in SCHEMA:
        return {k: canonical(value[k], child) for k, child in SCHEMA[kind]}
    if kind.endswith('[]'):
        child = kind[:-2]
        items = [canonical(v, child) for v in value]
        if child in KEYS:
            return sorted(items, key=lambda v: tuple(v[k] for k in KEYS[child]))
        return sorted(items) if child == 'int' else items
    return value


def expected_rules():
    # Independent path from golden generation: expand retained source ranges, not optical cells.
    path = ROOT.parent / 'research/verify-combat-source-freeze.py'
    spec = importlib.util.spec_from_file_location('source_inputs', path)
    source = importlib.util.module_from_spec(spec); spec.loader.exec_module(source)
    data = json.loads(source.FIXTURE.read_text())
    tables = source.normalize(source.tables(data), data['source_ruling'])
    morale = source.expand(data['cohesion_zero_morale_bands'])
    references = [
        ('spi-1979-common-charts', '15.79;15.89;17.4'),
        ('spi-1979-land-rules', '6.21-6.24;11.21-11.27;15.61-15.87;20.21;28.24'),
        ('spi-1979-september-errata', '15.27;20.72;50.2;50.12'),
        ('spi-1979-compilation', 'Logistics50.0-50.17'),
        ('sandtable-rules-lab', 'CMB-POL-001-008;CMB-SRC-RUL-001')]
    policies = ['closed-singleton-infantry', 'explicit-content-seeds-and-provenance',
                'single-ledger-atomic-costs-role-ordered-rng', 'persisted-deadline-fallback',
                'atomic-custody-rendezvous-and-delayed-escape', 'side-disclosure-allowlist',
                'relative-release-and-cumulative-history', 'supported-cycle-continuation-to-truck-entry']
    value = dict(schemaVersion=1, artifactId='cna-1979.1.combat-selected-inputs.v1', profileId=PROFILE,
        sources=[dict(sourceId=s, locator=l) for s, l in references],
        sourceEvidence=[dict(sourceId=s['id'], sha256='sha256:'+s['sha256']) for s in data['sources']],
        amendment=dict(rulingId='CMB-SRC-RUL-001', role='defender', differential=2,
                       coordinates=[34, 35, 36], lossPercent=10),
        morale=[dict(coordinate=c, adjustment=morale[c]) for c in source.COORDS],
        losses=[dict(role=role, differential=d, coordinate=c, lossPercent=tables[(role,d)][c])
                for role in ['attacker', 'defender'] for d in source.DIFFS for c in source.COORDS],
        effects=[dict(differential=d, attackerEngagedSums=data['attacker_engaged_sums'][str(d)],
                      defenderRetreatOneHexSums=data['defender_retreat_one_hex_sums'][str(d)],
                      attackerCaptureSums=data['attacker_capture_sums'][str(d)],
                      defenderCaptureSums=data['defender_capture_sums'][str(d)]) for d in source.DIFFS],
        captureShares=[dict(die=i+1, percent=p) for i,p in enumerate(data['capture_share_percent_by_die'])],
        procedure=dict(procedureId='sandtable.combat.role-ordered-d6.v1',
                       streamAlgorithm='sandtable.sha256-counter.v1', acceptedByteUpperExclusive=252, faces=6,
                       orderedPurposes=[f'{role}.{step}.{digit}' for step in ['morale', 'assault']
                                        for role in ['attacker', 'defender'] for digit in ['tens', 'ones']],
                       conditionalCapturePurposes=['attacker.capture.share', 'defender.capture.share']),
        costs=dict(committedToePerRole=10, ratingPerRole=1, basicMoralePerRole=0,
                   requiredCohesionPerRole=0, baseCapabilityPointAllowance=10,
                   attackerCapabilityPoints=5, defenderCapabilityPoints=3, ammunitionPointsPerToe=1),
        settlement=dict(attackerLossRounding='ceiling', defenderLossRounding='floor', capturedLossRounding='ceiling',
                        refusalPercentPerHex=10, lossDpThresholdToe=3, lossDpPoints=3, victoryRpPoints=3,
                        cohesionUpperCap=10, clearRetreatCapabilityPointsPerHex=1, guardToe=1,
                        guardCapabilityPointAllowance=10, guardAttackRating=0, guardDefenseRating=1,
                        guardInitialAmmunition=0, guardedPathMaximumHexes=3, escapePathMaximumCapabilityPoints=8,
                        replacementDelayOperationStages=12, operationStagesPerGameTurn=3),
        policies=[dict(policyId=f'CMB-POL-{i+1:03}', contractVersion=1, selectedBehaviorId=v)
                  for i,v in enumerate(policies)])
    return canonical(value, 'RulesInput')


def compare(actual, expected, path=''):
    require(type(actual) is type(expected), 4, path)
    if isinstance(expected, dict):
        for key, value in expected.items():
            compare(actual[key], value, path+'/'+key)
    elif isinstance(expected, list):
        require(len(actual)==len(expected), 4, path)
        for i, (a, e) in enumerate(zip(actual, expected)):
            compare(a, e, f'{path}/{i}')
    else:
        require(actual==expected, 4, path)


def validate_config(value, rules_hash):
    require(value['schemaVersion']==1, 3, '/schemaVersion')
    for key, expected in [('profileId', PROFILE), ('codecId', 'sandtable.combat.inputs-json.v1'),
                          ('timingPolicyId', 'sandtable.combat.fixed-deadline.v1')]:
        require(value[key]==expected, 3, '/'+key)
    require(rules_hash is not None and value['rulesInputHash']==rules_hash, 5, '/rulesInputHash')
    windows = value['windows']
    require(len(windows)==len(FALLBACKS) and {v['kind'] for v in windows}==set(FALLBACKS), 5, '/windows')
    for i, window in enumerate(windows):
        require(window['fallback']==FALLBACKS[window['kind']], 5, f'/windows/{i}/fallback')
        require(window['decisionBudgetMilliseconds'] > 0, 2, f'/windows/{i}/decisionBudgetMilliseconds')


def checked_config(config):
    typed(config, 'Config')
    validate_config(config, digest(encode(expected_rules())))
    require(config==canonical(config, 'Config'), 8)


def make_timing(config, kind, opened):
    checked_config(config)
    require(kind in FALLBACKS, 5, '/kind')
    typed(opened, 'utc', '/openedAtUnixMilliseconds')
    budget = next(v['decisionBudgetMilliseconds'] for v in config['windows'] if v['kind']==kind)
    require(opened <= LIMITS['utcMaximum']-budget, 6, '/deadlineUnixMilliseconds')
    return dict(contractVersion=1, configHash=digest(encode(canonical(config, 'Config'))), kind=kind,
                decisionBudgetMilliseconds=budget, openedAtUnixMilliseconds=opened,
                deadlineUnixMilliseconds=opened+budget, highWaterUnixMilliseconds=opened)


def validate_timing(value, config):
    require(value['contractVersion']==1, 3, '/contractVersion')
    checked_config(config)
    require(value['configHash']==digest(encode(canonical(config, 'Config'))), 5, '/configHash')
    require(value['kind'] in FALLBACKS, 5, '/kind')
    expected = make_timing(config, value['kind'], value['openedAtUnixMilliseconds'])
    for key in ['decisionBudgetMilliseconds', 'deadlineUnixMilliseconds']:
        require(value[key]==expected[key], 6, '/'+key)
    require(value['highWaterUnixMilliseconds'] >= value['openedAtUnixMilliseconds'], 6, '/highWaterUnixMilliseconds')


def read(raw, kind, rules_hash=None, config=None):
    require(kind in ('RulesInput', 'Config', 'Timing'), 3)
    require(type(raw) is bytes and 0 < len(raw) <= LIMITS['bytes'], 1)
    def pairs(items):
        value = {}
        for key, child in items:
            require(key not in value, 1)
            value[key] = child
        return value
    try:
        value = json.loads(raw.decode('utf-8'), object_pairs_hook=pairs,
                           parse_constant=lambda _: require(False, 1))
    except (UnicodeError, ValueError, RecursionError) as error:
        raise Invalid(1) from error
    typed(value, kind)
    version = 'contractVersion' if kind=='Timing' else 'schemaVersion'
    require(value[version]==1, 3, '/'+version)
    if kind=='RulesInput':
        compare(canonical(value, kind), expected_rules())
    elif kind=='Config':
        validate_config(value, rules_hash)
    else:
        validate_timing(value, config)
    require(raw==encode(canonical(value, kind)), 8)
    if kind=='RulesInput' and rules_hash is not None:
        require(digest(raw)==rules_hash, 7)
    return value


def clock_gate(timing, now, available):
    typed(timing, 'Timing')
    require(type(available) is bool, 1)
    if now is not None:
        typed(now, 'utc')
    if not available or now is None or now < timing['highWaterUnixMilliseconds']:
        return 'unavailable'
    return 'expired' if now >= timing['deadlineUnixMilliseconds'] else 'before-deadline'


def rejected(call, expected=None):
    try:
        call()
    except Invalid as error:
        assert expected is None or error.code == expected, (error.code, expected, error.path)
        return
    raise AssertionError('invalid contract accepted')


def set_path(value, path, replacement):
    tokens = path.strip('/').split('/')
    current = value
    for token in tokens[:-1]:
        current = current[int(token)] if isinstance(current, list) else current[token]
    key = int(tokens[-1]) if isinstance(current, list) else tokens[-1]
    current[key] = copy.deepcopy(replacement)


def main():
    fixture = json.loads((ROOT / 'fixtures/combat-rules-inputs-v1.json').read_text())
    goldens = {v['kind']: v for v in fixture['goldens']}
    rule_hash = goldens['RulesInput']['sha256']
    config = json.loads(goldens['Config']['canonicalUtf8'])
    values = {}
    for kind, golden in goldens.items():
        raw = golden['canonicalUtf8'].encode()
        assert len(raw) == golden['byteCount'] and digest(raw) == golden['sha256']
        values[kind] = read(raw, kind, rule_hash, config)
    for vector in fixture['negativeVectors']:
        value = copy.deepcopy(values[vector['kind']])
        set_path(value, vector['path'], vector['value'])
        rejected(lambda: read(encode(value), vector['kind'], rule_hash, config), vector['error'])
    # Raw spelling is part of canonical identity; malformed bytes never get repaired by hashing.
    malformed = 0
    for kind, golden in goldens.items():
        raw = golden['canonicalUtf8'].encode()
        first = next(iter(values[kind]))
        spellings = [raw+b'\n', b'\xef\xbb\xbf'+raw, raw+b'{}', b'\xff',
                     b'{"'+first.encode()+b'":1,'+raw[1:],
                     raw.replace(b':1,', b':1.0,', 1), raw.replace(b':1,', b':1e0,', 1),
                     raw.replace(b':1,', b':true,', 1),
                     b'{"unknown":0,'+raw[1:], encode({k:v for k,v in values[kind].items() if k!=first}),
                     encode(dict(reversed(list(values[kind].items())))), b'['*34+b'0'+b']'*34,
                     b' '*1048577]
        for mutated in spellings:
            rejected(lambda: read(mutated, kind, rule_hash, config))
            malformed += 1
    for kind, value in values.items():
        shuffled = dict(reversed(list(value.items())))
        assert encode(canonical(shuffled, kind)) == goldens[kind]['canonicalUtf8'].encode()
    for kind, field in [('RulesInput', 'losses'), ('RulesInput', 'policies'), ('Config', 'windows')]:
        for replacement in [values[kind][field][:-1], values[kind][field]+[values[kind][field][0]]]:
            bad = copy.deepcopy(values[kind]); bad[field] = replacement
            rejected(lambda: read(encode(bad), kind, rule_hash, config))
    assert make_timing(config, 'force-assignment', 1000) == values['Timing']
    for case in fixture['clockCases']:
        timing = values['Timing'] | {'highWaterUnixMilliseconds': case['highWater']}
        before = copy.deepcopy(timing)
        assert clock_gate(timing, case['now'], case['available']) == case['expected'], case['name']
        assert timing == before, 'clock gate must not mutate accepted high-water evidence'
    # Every kind has an explicit independently variable positive budget; config identity changes,
    # and a window cannot be recovered against a replacement config even with the same display ID.
    boundary_count = 0
    for policy in config['windows']:
        for budget in [1, 2147483647]:
            variant = copy.deepcopy(config)
            next(v for v in variant['windows'] if v['kind']==policy['kind'])['decisionBudgetMilliseconds'] = budget
            checked = read(encode(variant), 'Config', rule_hash)
            assert digest(encode(checked)) != digest(encode(config))
            timing = make_timing(checked, policy['kind'], LIMITS['utcMaximum']-budget)
            assert timing['deadlineUnixMilliseconds'] == LIMITS['utcMaximum']
            read(encode(timing), 'Timing', rule_hash, checked)
            rejected(lambda: read(encode(timing), 'Timing', rule_hash, config), 'CMB-INP-005')
            rejected(lambda: make_timing(checked, policy['kind'], LIMITS['utcMaximum']-budget+1), 'CMB-INP-006')
            boundary_count += 1
    # Rehashing a modified rule cannot turn an unsupported rule or historical source change valid.
    altered = copy.deepcopy(values['RulesInput']); altered['amendment']['lossPercent'] = 5
    rejected(lambda: read(encode(altered), 'RulesInput', digest(encode(altered))), 'CMB-INP-004')
    for bad_time in [-1, True, 1.5, LIMITS['utcMaximum']+1]:
        rejected(lambda: make_timing(config, 'selection', bad_time))
        rejected(lambda: clock_gate(values['Timing'], bad_time, True))
    rejected(lambda: clock_gate(values['Timing'], 1000, 1), 'CMB-INP-001')
    # Full source shape and history bind later; malformed/missing configuration never defaults.
    for bad in [None, {}, config | {'windows': []}]:
        rejected(lambda: make_timing(bad, 'selection', 1000))
    print(f'PASS 3 canonical goldens; {len(fixture["negativeVectors"])} mutations; {malformed} raw-byte rejections; '
          f'{len(fixture["clockCases"])} clock cases; {boundary_count} kind/budget/UTC boundaries; '
          '360 source loss coordinates and 36 Morale coordinates; no Rules10/runtime admission.')


if __name__ == '__main__':
    main()
