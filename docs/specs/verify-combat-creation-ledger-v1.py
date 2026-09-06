#!/usr/bin/env python3
"""TASK-003A Setup/initial-value contract oracle; no live campaign implementation."""
import copy
import hashlib
import importlib.util
import json
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
loader = importlib.util.spec_from_file_location('content_oracle', ROOT / 'verify-combat-content-v7.py')
content = importlib.util.module_from_spec(loader)
loader.loader.exec_module(content)
SCHEMA = {
    'Setup': 'schemaVersion:int setupId:id setupHash:hash isSynthetic:bool capabilityProfileId:id initialGameTurn:int initialInitiative:Initiative openingPreamble:Policy weather:Policy stageEntry:StageEntry combatInitialization:Initialization content:Selection sources:Reference[]',
    'Initiative': 'kind:string holder:side',
    'Policy': 'contractVersion:int kind:string sources:Reference[]',
    'StageEntry': 'contractVersion:int gameTurn:int operationStage:int organization:string navalConvoyArrival:string fleetAssignment:string fleetRepair:string sources:Reference[]',
    'Initialization': 'contractVersion:int gameTurn:int operationStage:int capabilityPointsExpended:Cp cohesionLevel:int reserveStatus:string origin:Origin',
    'Selection': 'schemaVersion:int formatId:id packId:id rulesetId:id hash:hash scenarioId:id',
    'Cp': 'numerator:long denominator:int',
    'Reference': 'sourceId:id locator:locator', 'Origin': 'kind:string references:Reference[]',
    'Element': 'elementId:id currentLocationId:id reserveStatus:string operationalState:Operational components:Component[] sourceParentFormationId:id currentParentFormationId:id ammunition:Ammunition readiness:Readiness',
    'Operational': 'ledgerGameTurn:int ledgerOperationStage:int capabilityPointsExpended:Cp cohesionLevel:int vehicleBreakdownState:null movementEnded:null initialLedgerOrigin:Origin',
    'Component': 'componentId:id currentToe:int initialToeOrigin:Origin',
    'Ammunition': 'points:int initialAmmunitionOrigin:Origin',
    'Readiness': 'gameTurn:int operationStage:int waterStatus:string storesStatus:string pinned:bool initialReadinessOrigin:Origin',
}
SCHEMA = {k: [tuple(f.split(':')) for f in v.split()] for k, v in SCHEMA.items()}
KEYS = {'Element': ('elementId',), 'Component': ('componentId',), 'Reference': ('sourceId', 'locator')}
POLICIES = {'openingPreamble': ('no-opening-naval-convoy-obligations', 'opening-preamble.no-naval-convoy-obligations.v1'),
            'weather': ('no-immediate-weather-effect-subjects', 'weather.no-immediate-effect-subjects.v1')}


class Invalid(ValueError):
    def __init__(self, code, path):
        self.code, self.path = f'CMB-INI-{code:03}', path
        super().__init__(f'{self.code} {path or "/"}')


def require(ok, code, path):
    if not ok:
        raise Invalid(code, path)


def shape(value, kind, path='', depth=0):
    require(depth <= 32, 1, path)
    if kind in SCHEMA:
        require(type(value) is dict, 1, path)
        for key, child in SCHEMA[kind]:
            require(key in value, 1, path + '/' + key)
            shape(value[key], child, path + '/' + key, depth + 1)
        require(set(value) == {k for k, _ in SCHEMA[kind]}, 1, path)
    elif kind.endswith('[]'):
        require(type(value) is list, 1, path)
        for i, item in enumerate(value):
            shape(item, kind[:-2], f'{path}/{i}', depth + 1)
    else:
        expected = {'int': int, 'long': int, 'bool': bool, 'null': type(None),
                    'id': str, 'hash': str, 'locator': str, 'string': str, 'side': str}[kind]
        require(type(value) is expected, 1, path)


def primitives(value, kind, path=''):
    if kind in SCHEMA:
        for key, child in SCHEMA[kind]:
            primitives(value[key], child, path + '/' + key)
    elif kind.endswith('[]'):
        for i, item in enumerate(value):
            primitives(item, kind[:-2], f'{path}/{i}')
    elif kind in ('int', 'long'):
        hi = 9223372036854775807 if kind == 'long' else 2147483647
        lo = -hi - 1
        field = path.rsplit('/', 1)[-1]
        if field in ('gameTurn', 'ledgerGameTurn', 'initialGameTurn'): lo, hi = 1, 111
        elif field in ('operationStage', 'ledgerOperationStage'): lo, hi = 1, 3
        elif field in ('points', 'currentToe', 'numerator'): lo = 0
        elif field == 'denominator': lo = 1
        elif field == 'cohesionLevel': hi = 10
        require(lo <= value <= hi, 2, path)
    elif kind in ('id', 'locator', 'hash'):
        grammar = {'id': content.ID, 'locator': content.LOCATOR, 'hash': r'sha256:[0-9a-f]{64}'}[kind]
        require(1 <= len(value) <= 128 and re.fullmatch(grammar, value), 2, path)
    elif kind == 'side':
        require(value in ('axis', 'commonwealth'), 2, path)


def canonical(value, kind):
    if kind in SCHEMA:
        return {k: canonical(value[k], c) for k, c in SCHEMA[kind]}
    if kind.endswith('[]'):
        child = kind[:-2]
        return sorted([canonical(v, child) for v in value], key=lambda v: tuple(v[k] for k in KEYS[child]))
    return value


def encode(value):
    return json.dumps(value, ensure_ascii=True, separators=(',', ':')).encode()


def digest(raw):
    return 'sha256:' + hashlib.sha256(raw).hexdigest()


def setup_hash(setup):
    return digest(encode({k: v for k, v in canonical(setup, 'Setup').items() if k != 'setupHash'}))


def refs(locator):
    return [dict(sourceId='sandtable-rules-lab', locator=locator)]


def origin(locator):
    return dict(kind='synthetic', references=refs('combat.close-assault-positive.v1:' + locator))


def selection(pack):
    return dict(schemaVersion=pack['schemaVersion'], formatId=pack['formatId'], packId=pack['packId'],
                rulesetId=pack['rulesetId'], hash=digest(content.encode(content.canonical(pack))),
                scenarioId=pack['scenarios'][0]['scenarioId'])


def validate_setup(setup, pack):
    content.validate(pack)
    shape(setup, 'Setup')
    primitives(setup, 'Setup')
    for key, expected in [('schemaVersion', 7), ('isSynthetic', True),
                          ('capabilityProfileId', 'sandtable.capability.combat-cycle-infantry.v1')]:
        require(setup[key] == expected, 3, '/' + key)
    expected_selection = selection(pack)
    for key, value in expected_selection.items():
        require(setup['content'][key] == value, 3, '/content/' + key)
    start = pack['scenarios'][0]['start']
    require(setup['initialGameTurn'] == start['gameTurn'], 4, '/initialGameTurn')
    require(start['operationStage'] == 1, 4, '/stageEntry/operationStage')
    require(setup['initialInitiative']['kind'] == 'predetermined', 4, '/initialInitiative/kind')
    for key, (kind, locator) in POLICIES.items():
        require(setup[key]['contractVersion'] == 1, 3, '/' + key + '/contractVersion')
        require(setup[key]['kind'] == kind, 4, '/' + key + '/kind')
        require(setup[key]['sources'] == refs(locator), 5, '/' + key + '/sources')
    for name in ('stageEntry', 'combatInitialization'):
        value = setup[name]
        require(value['contractVersion'] == 1, 3, '/' + name + '/contractVersion')
        for key in ('gameTurn', 'operationStage'):
            require(value[key] == start[key], 4, '/' + name + '/' + key)
    stage = setup['stageEntry']
    for key in ('organization', 'navalConvoyArrival', 'fleetAssignment', 'fleetRepair'):
        require(stage[key] == 'explicit-none', 4, '/stageEntry/' + key)
    require(stage['sources'] == refs('stage-entry.no-obligations.v1'), 5, '/stageEntry/sources')
    init = setup['combatInitialization']
    for key, expected in [('capabilityPointsExpended', dict(numerator=0, denominator=1)),
                          ('cohesionLevel', 0), ('reserveStatus', 'none')]:
        require(init[key] == expected, 6, '/combatInitialization/' + key)
    require(init['origin'] == origin('initial-ledger'), 5, '/combatInitialization/origin')
    require(setup['sources'] == refs('combat.close-assault-positive.v1:setup'), 5, '/sources')
    require(setup['setupHash'] == setup_hash(setup), 7, '/setupHash')


def initial_elements(setup, pack):
    validate_setup(setup, pack)
    elements = {e['elementId']: e for e in pack['elements']}
    init = setup['combatInitialization']
    result = []
    for p in pack['scenarios'][0]['initialPlacements']:
        source = elements[p['elementId']]
        ready = p['initialReadiness']
        result.append(dict(elementId=p['elementId'], currentLocationId=p['locationId'], reserveStatus=init['reserveStatus'],
            operationalState=dict(ledgerGameTurn=init['gameTurn'], ledgerOperationStage=init['operationStage'],
                capabilityPointsExpended=init['capabilityPointsExpended'], cohesionLevel=init['cohesionLevel'],
                vehicleBreakdownState=None, movementEnded=None, initialLedgerOrigin=init['origin']),
            components=[dict(componentId=c['componentId'], currentToe=c['currentToe'], initialToeOrigin=c['origin']) for c in p['initialComponentToes']],
            sourceParentFormationId=source['parentFormationId'], currentParentFormationId=source['parentFormationId'],
            ammunition=dict(points=p['initialAmmunition']['points'], initialAmmunitionOrigin=p['initialAmmunition']['origin']),
            readiness=dict(gameTurn=ready['gameTurn'], operationStage=ready['operationStage'], waterStatus=ready['waterStatus'],
                           storesStatus=ready['storesStatus'], pinned=ready['pinned'], initialReadinessOrigin=ready['origin'])))
    return copy.deepcopy(canonical(result, 'Element[]'))


def compare(actual, expected, path=''):
    if isinstance(expected, dict):
        for key in expected:
            compare(actual[key], expected[key], path + '/' + key)
    elif isinstance(expected, list):
        require(len(actual) == len(expected), 6, path)
        for i, (a, b) in enumerate(zip(actual, expected)):
            compare(a, b, f'{path}/{i}')
    else:
        require(actual == expected, 6, path)


def parse(raw, kind):
    require(len(raw) <= 65536 and not raw.startswith(b'\xef\xbb\xbf'), 1, '')
    try:
        value = json.loads(raw.decode('utf-8'), object_pairs_hook=content.pairs,
                           parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error:
        raise Invalid(1, '') from error
    shape(value, kind)
    primitives(value, kind)
    return value


def read_setup(raw, pack):
    value = parse(raw, 'Setup')
    validate_setup(value, pack)
    require(encode(canonical(value, 'Setup')) == raw, 8, '')
    return value


def read_elements(raw, setup, pack):
    value = parse(raw, 'Element[]')
    # Compare in canonical order so input ordering alone is a canonical-byte failure.
    compare(canonical(value, 'Element[]'), initial_elements(setup, pack))
    require(encode(canonical(value, 'Element[]')) == raw, 8, '')
    return value


def mutate(value, vector):
    result = copy.deepcopy(value)
    parts = vector['path'].strip('/').split('/')
    parent = result
    for key in parts[:-1]:
        parent = parent[int(key)] if isinstance(parent, list) else parent[key]
    key = int(parts[-1]) if isinstance(parent, list) else parts[-1]
    if vector['op'] == 'remove': del parent[key]
    else: parent[key] = vector['value']
    return result


def main():
    pack_raw = (ROOT / 'fixtures/combat-content-v7.canonical.json').read_bytes()
    content_vectors = json.loads((ROOT / 'fixtures/combat-content-v7.vectors.json').read_text())
    assert digest(pack_raw) == content_vectors['canonicalHash']
    pack = content.read(pack_raw)
    manifest = json.loads((ROOT / 'fixtures/combat-creation-ledger-v1.json').read_text())
    sr, er = (manifest[key]['canonicalUtf8'].encode() for key in ('setup', 'initialElements'))
    for key, raw in [('setup', sr), ('initialElements', er)]:
        assert len(raw) == manifest[key]['byteCount'] and digest(raw) == manifest[key]['sha256']
    setup = read_setup(sr, pack)
    elements = read_elements(er, setup, pack)
    for vector in manifest['negativeVectors']:
        raw = sr if vector['target'] == 'setup' else er
        if vector['op'] == 'append': candidate = raw + vector['value'].encode()
        elif vector['op'] == 'raw-replace':
            old = vector['old'].encode()
            assert raw.count(old) == 1, vector['name']
            candidate = raw.replace(old, vector['new'].encode(), 1)
        else:
            value = mutate(setup if vector['target'] == 'setup' else elements, vector)
            if vector.get('rehash'): value['setupHash'] = setup_hash(value)
            candidate = encode(value)
        try:
            if vector['target'] == 'setup': read_setup(candidate, pack)
            else: read_elements(candidate, setup, pack)
        except Invalid as error:
            assert (error.code, error.path) == (vector['code'], vector['errorPath']), (vector['name'], str(error))
        else: raise AssertionError('Accepted negative vector: ' + vector['name'])
    # Valid changes propagate identity; compare against retained literal positive bytes.
    alternate = copy.deepcopy(setup)
    alternate['initialInitiative']['holder'] = 'commonwealth'
    alternate['setupHash'] = setup_hash(alternate)
    ar = encode(canonical(alternate, 'Setup'))
    read_setup(ar, pack)
    assert ar.decode() == manifest['alternateHolderCanonicalUtf8'] and ar != sr
    changed_pack = copy.deepcopy(pack)
    changed_pack['scenarios'][0]['initialPlacements'][0]['initialAmmunition']['origin']['references'][0]['locator'] += '.v2'
    content.read(content.encode(content.canonical(changed_pack)))
    try: validate_setup(setup, changed_pack)
    except Invalid as error: assert (error.code, error.path) == ('CMB-INI-003', '/content/hash')
    else: raise AssertionError('Stale Content hash admitted')
    changed_setup = copy.deepcopy(setup)
    changed_setup['content'] = selection(changed_pack)
    changed_setup['setupHash'] = setup_hash(changed_setup)
    changed_elements = initial_elements(changed_setup, changed_pack)
    assert changed_setup['setupHash'] != setup['setupHash']
    assert encode(changed_elements) != er
    assert changed_elements[0]['ammunition']['initialAmmunitionOrigin'] == changed_pack['scenarios'][0]['initialPlacements'][0]['initialAmmunition']['origin']
    # Valid Content stage2 remains representable, but cannot initialize this stage1-only Setup.
    later = copy.deepcopy(pack)
    for boundary in ('start', 'end'): later['scenarios'][0][boundary]['operationStage'] = 2
    for p in later['scenarios'][0]['initialPlacements']: p['initialReadiness']['operationStage'] = 2
    content.validate(later)
    later_setup = copy.deepcopy(setup)
    later_setup['content'] = selection(later)
    for key in ('stageEntry', 'combatInitialization'): later_setup[key]['operationStage'] = 2
    later_setup['setupHash'] = setup_hash(later_setup)
    try: validate_setup(later_setup, later)
    except Invalid as error: assert (error.code, error.path) == ('CMB-INI-004', '/stageEntry/operationStage')
    else: raise AssertionError('Unsupported creation stage admitted')
    def reverse(v):
        if isinstance(v, dict): return {k: reverse(val) for k, val in reversed(list(v.items()))}
        if isinstance(v, list): return [reverse(val) for val in reversed(v)]
        return v
    assert encode(canonical(reverse(setup), 'Setup')) == sr
    assert encode(canonical(reverse(elements), 'Element[]')) == er
    print(f"PASS: Setup {len(sr)} bytes/{digest(sr)}; initial elements {len(er)} bytes/{digest(er)}; "
          f"{len(manifest['negativeVectors'])} rejection vectors; alternate holder, provenance binding, stage2 refusal and shuffled construction.")


if __name__ == '__main__':
    main()
