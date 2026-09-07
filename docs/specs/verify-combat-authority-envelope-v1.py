#!/usr/bin/env python3
"""003C2 contract oracle; no production creation, persistence, or replay."""
import copy
import hashlib
import importlib.util
import json
import re
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
FIXTURE = ROOT / 'fixtures/combat-authority-envelope-v1.json'
MAX_BYTES = 1048576


def module(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


world = module('world_envelope_dependency', 'verify-combat-world-settlement-v1.py')
inputs = module('inputs_envelope_dependency', 'verify-combat-rules-inputs-v1.py')
sequence = module('sequence_envelope_dependency', 'verify-combat-cycle-sequence-v1.py')
init = world.init
INVENTORY = json.loads((ROOT / 'combat-authority-envelope-v1.schema.json').read_text())
SCHEMA = world.SCHEMA | {k: inputs.SCHEMA[k] for k in ('Config', 'WindowPolicy')} | {
    'Position': sequence.SCHEMA['Position']} | {
    k: [tuple(field.split(':')) for field in v.split()] for k, v in INVENTORY['objects'].items()}
KEYS = world.KEYS | {'Artifact': ['artifactId'], 'Ruling': ['rulingId'], 'Reference': ['sourceId', 'locator'], 'WindowPolicy': ['kind']}


class Invalid(ValueError):
    def __init__(self, code, path=''):
        self.code, self.path = f'CMB-ENV-{code:03}', path
        super().__init__(f'{self.code} {path or "/"}')


def require(ok, code, path=''):
    if not ok: raise Invalid(code, path)


def encode(v): return json.dumps(v, ensure_ascii=True, separators=(',', ':')).encode('ascii')
def sha(raw): return 'sha256:' + hashlib.sha256(raw).hexdigest()


def typed(v, kind, path='', depth=0):
    require(depth <= 32, 1, path)
    if kind.endswith('?'):
        if v is not None: typed(v, kind[:-1], path, depth)
    elif kind in SCHEMA:
        require(type(v) is dict, 1, path)
        for k, child in SCHEMA[kind]:
            require(k in v, 1, path + '/' + k)
            typed(v[k], child, path + '/' + k, depth + 1)
        require(set(v) == {k for k, _ in SCHEMA[kind]}, 1, path)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v) <= 512, 1, path)
        for i, child in enumerate(v): typed(child, kind[:-2], f'{path}/{i}', depth + 1)
    elif kind in ('LegacyBrokenVehicleLot', 'Route'):
        require(False, 1, path)  # No noninitial external values admitted by this cut.
    elif kind in ('null', 'empty'):
        require(v is None if kind == 'null' else type(v) is list and len(v) == 0, 1, path)
    elif kind in ('int', 'long', 'ulong', 'utc', 'futureTurn'):
        require(type(v) is int, 1, path)
        bounds = {'int': (-2**31, 2**31-1), 'long': (-2**63, 2**63-1),
                  'ulong': (0, 2**64-1), 'utc': (0, 253402300799999), 'futureTurn': (1, 115)}
        lo, hi = bounds[kind]
        require(lo <= v <= hi, 2, path)
    elif kind == 'bool': require(type(v) is bool, 1, path)
    else:
        require(type(v) is str, 1, path)
        pattern = {'id': r'[A-Za-z0-9][A-Za-z0-9._:-]{0,127}', 'hash': r'sha256:[0-9a-f]{64}',
                   'rawHash': r'[0-9a-f]{64}', 'locator': r'[ -~]{1,512}', 'text': r'[ -~]{1,512}',
                   'string': r'[ -~]{1,512}', 'side': r'axis|commonwealth'}[kind]
        require(re.fullmatch(pattern, v) is not None, 2, path)


def canonical(v, kind):
    if kind.endswith('?'): return None if v is None else canonical(v, kind[:-1])
    if kind in SCHEMA: return {k: canonical(v[k], c) for k, c in SCHEMA[kind]}
    if kind.endswith('[]'):
        child = kind[:-2]; values = [canonical(x, child) for x in v]
        if child in KEYS: values.sort(key=lambda x: tuple(x[k] for k in KEYS[child]))
        elif kind == 'id[]': values.sort()
        return values
    return v


def parse(raw, kind):
    require(type(raw) is bytes and 0 < len(raw) <= MAX_BYTES, 1)
    def pairs(items):
        v = {}
        for k, x in items:
            require(k not in v, 1); v[k] = x
        return v
    try:
        v = json.loads(raw.decode('utf-8'), object_pairs_hook=pairs,
                       parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as e: raise Invalid(1) from e
    typed(v, kind)
    return v


def compare(v, expected, code=6, path=''):
    require(type(v) is type(expected), code, path)
    if type(v) is dict:
        require(set(v) == set(expected), code, path)
        for k in expected: compare(v[k], expected[k], code, path + '/' + k)
    elif type(v) is list:
        require(len(v) == len(expected), code, path)
        for i, (a, b) in enumerate(zip(v, expected)): compare(a, b, code, f'{path}/{i}')
    else: require(v == expected, code, path)


def expected_manifest():
    predecessor = json.loads(FIXTURE.read_text())['predecessor']['canonicalUtf8'].encode()
    require(sha(predecessor) == 'sha256:17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a', 4)
    m = json.loads(predecessor); m['contractVersion'] = 10
    catalog = sequence.expected_catalog(); codec = sequence.expected_codec(); rules = inputs.expected_rules()
    artifacts = {a['artifactId']: a for a in m['artifacts']}
    refs = {(s['sourceId'], s['locator']) for p in catalog['positions'] + catalog['interruptPositions'] for s in p['sources']}
    artifacts['cna-1979.1.land-sequence'] = dict(artifactId='cna-1979.1.land-sequence',
        contentHash=sha(sequence.encode(catalog)), sources=[dict(sourceId=s, locator=l) for s, l in sorted(refs)])
    for value, raw in [(rules, inputs.encode(inputs.canonical(rules, 'RulesInput'))), (codec, sequence.encode(codec))]:
        artifacts[value['artifactId']] = dict(artifactId=value['artifactId'], contentHash=sha(raw), sources=value['sources'])
    m['artifacts'] = list(artifacts.values())
    m['rulings'].append(dict(rulingId='cna-1979.1.ruling.combat-source-gap', conflictId='CMB-SRC-GAP-001',
        alternativeIds=['defer-positive-assault', 'fill-defender-plus2-34-35-36-with10'],
        selectedBehaviorId='fill-defender-plus2-34-35-36-with10', protectingTestIds=['combat-envelope.source-ruling'],
        sources=[dict(sourceId='sandtable-rules-lab', locator='CMB-SRC-RUL-001'),
                 dict(sourceId='spi-1979-common-charts', locator='15.79')]))
    return canonical(m, 'Manifest')


def read_manifest(raw):
    v = parse(raw, 'Manifest'); require(v['contractVersion'] == 10, 3, '/contractVersion')
    compare(canonical(v, 'Manifest'), expected_manifest(), 4)
    require(raw == encode(canonical(v, 'Manifest')), 8)
    return v


class Context:
    """Inputs supplied by trusted registry/archive, never extracted from untrusted envelopes."""
    def __init__(self, setup=None, pack=None, config=None):
        self.pack = copy.deepcopy(world.PACK if pack is None else pack)
        self.setup = copy.deepcopy(world.SETUP if setup is None else setup)
        manifest = expected_manifest(); self.rules_hash = sha(encode(manifest))[7:]
        init.read_setup(init.encode(init.canonical(self.setup, 'Setup')), self.pack)
        default = json.loads((ROOT / 'fixtures/combat-rules-inputs-v1.json').read_text())['goldens'][1]['canonicalUtf8']
        self.config = inputs.read(default.encode() if config is None else inputs.encode(inputs.canonical(config, 'Config')),
                                  'Config', rules_hash=sha(inputs.encode(inputs.canonical(inputs.expected_rules(), 'RulesInput'))))
        self.config_hash = sha(inputs.encode(inputs.canonical(self.config, 'Config')))

    def request(self, campaign='rules-lab.combat-creation.1', seed=0):
        r = dict(contractVersion=1, campaignId=campaign, rulesetHash=self.rules_hash,
                 setupId=self.setup['setupId'], setupHash=self.setup['setupHash'], content=copy.deepcopy(self.setup['content']),
                 configurationHash=self.config_hash, randomState=dict(contractVersion=1,
                 algorithmId='sandtable.sha256-counter.v1', seed=seed, nextByteCursor=0))
        return read_request(encode(r), self)


def read_request(raw, context):
    v = parse(raw, 'Request'); require(v['contractVersion'] == 1, 3, '/contractVersion')
    for key, expected in [('rulesetHash', context.rules_hash), ('setupId', context.setup['setupId']),
                          ('setupHash', context.setup['setupHash']), ('content', context.setup['content']),
                          ('configurationHash', context.config_hash)]: compare(v[key], expected, 5, '/' + key)
    rng = v['randomState']
    for key, value in [('contractVersion', 1), ('algorithmId', 'sandtable.sha256-counter.v1'), ('nextByteCursor', 0)]:
        require(rng[key] == value, 5, '/randomState/' + key)
    require(raw == encode(canonical(v, 'Request')), 8)
    return v


def binding(request):
    # Caller validates Request and trusted context first. No World/event/prefix in preimage.
    return 'creation.' + hashlib.sha256(b'sandtable.combat.creation-request.v1\0' + encode(canonical(request, 'Request'))).hexdigest()


def initial_world(request, context):
    elements = init.initial_elements(context.setup, context.pack)
    result = {k: [] for k, _ in world.SCHEMA['World']}
    result.update(contractVersion=7, creationBinding=binding(request), elements=elements,
        representations=[dict(representationId=f'map-representation.{i+1:04}', currentLocationId=e['currentLocationId'],
                         bindingKind='independent-element', boundElementIds=[e['elementId']]) for i, e in enumerate(elements)])
    return world.canonical(result, 'World')


def make_created(request, context):
    read_request(encode(request), context)
    position = copy.deepcopy(sequence.expected_catalog()['positions'][0])
    position['gameTurn'] = context.setup['initialGameTurn']
    return dict(contractVersion=11, eventType='campaign-created', campaignId=request['campaignId'], stateVersion=1,
        rulesetHash=context.rules_hash, setup=context.setup, configuration=context.config, creationRequest=request,
        creationBinding=binding(request), initialWorld=initial_world(request, context), randomState=request['randomState'],
        sequencePosition=position, breakdownFlow=dict(kind='idle'))


def read_created(raw, request, context):
    read_request(encode(request), context)
    v = parse(raw, 'Created'); require(v['contractVersion'] == 11, 3, '/contractVersion')
    expected = make_created(request, context)
    compare(canonical(v, 'Created'), canonical(expected, 'Created'))
    require(raw == encode(canonical(expected, 'Created')), 8)
    return v


def make_snapshot(created_raw, request, context):
    c = read_created(created_raw, request, context)
    return dict(contractVersion=12, campaignId=c['campaignId'], stateVersion=1, rulesetHash=c['rulesetHash'],
        setup=c['setup'], world=c['initialWorld'], initiativeHolder=None, operationStageOrders=[], operationStageWeather=[],
        randomState=c['randomState'], currentPosition=dict(kind='sequence', sequencePosition=c['sequencePosition']),
        reactionWindow=None, breakdownFlow=c['breakdownFlow'], configuration=c['configuration'],
        creationReceipt=dict(contractVersion=1, creationRequest=request, creationBinding=c['creationBinding'], creationEventHash=sha(created_raw)),
        chroniclePrefix=sequence.prefix_creation(created_raw), cycleState=None, combatState=None, commandReceipts=[])


def read_snapshot(raw, created_raw, request, context):
    require(type(created_raw) is bytes and len(created_raw) > 0, 7)
    v = parse(raw, 'Snapshot'); require(v['contractVersion'] == 12, 3, '/contractVersion')
    expected = make_snapshot(created_raw, request, context)
    compare(canonical(v, 'Snapshot'), canonical(expected, 'Snapshot'))
    require(raw == encode(canonical(expected, 'Snapshot')), 8)
    return v


def creation_cut(stored, request, context, admission_enabled, reader_available=True):
    """Pure contract decision at creation boundary; not durable storage or CAS implementation."""
    require(type(admission_enabled) is bool and type(reader_available) is bool, 1)
    require(reader_available, 7)
    if stored is not None:
        read_created(stored, request, context)
        return stored, False  # Existing receipt wins even if fresh admission has since closed.
    require(admission_enabled, 7)
    return encode(canonical(make_created(request, context), 'Created')), True


def rejected(call, code):
    try: call()
    except Invalid as error:
        assert error.code == f'CMB-ENV-{code:03}', (error.code, code, error.path)
    else: raise AssertionError('invalid authority envelope accepted')


def main():
    f = json.loads(FIXTURE.read_text()); context = Context(); request = context.request()
    for path, digest in f['sourceHashes'].items(): assert sha((ROOT.parent.parent / path).read_bytes()) == digest, path
    predecessor = f['predecessor']; assert len(predecessor['canonicalUtf8'].encode()) == predecessor['byteCount']
    assert sha(predecessor['canonicalUtf8'].encode()) == predecessor['sha256']
    assert f['request'] == request
    raw = {k: v['canonicalUtf8'].encode() for k, v in f['goldens'].items()}
    readers = {'manifest': read_manifest, 'request': lambda b: read_request(b, context),
               'created': lambda b: read_created(b, request, context),
               'snapshot': lambda b: read_snapshot(b, raw['created'], request, context)}
    for kind, data in raw.items():
        g = f['goldens'][kind]; assert len(data) == g['byteCount'] and sha(data) == g['sha256']
        readers[kind](data)
    assert sha(raw['manifest'])[7:] == f['rulesetHash'] == context.rules_hash
    assert binding(request) == f['creationBinding']
    assert bytes.fromhex(f['requestPreimageHex']) == b'sandtable.combat.creation-request.v1\0' + raw['request']
    assert sequence.prefix_creation(raw['created']) == f['creationPrefix']
    for vector in f['negativeVectors']:
        v = json.loads(raw[vector['target']]); parts = vector['path'].strip('/').split('/'); parent = v
        for part in parts[:-1]: parent = parent[int(part)] if type(parent) is list else parent[part]
        key = int(parts[-1]) if type(parent) is list else parts[-1]
        if vector['op'] == 'delete': del parent[key]
        else: parent[key] = vector['value']
        rejected(lambda: readers[vector['target']](encode(v)), int(vector['code'][-3:]))
    raw_checks = 0
    for kind, data in raw.items():
        for bad, code in [(data + b'\n', 8), (b'\xef\xbb\xbf' + data, 1), (data[:-1], 1),
                          (data + b'\xff', 1), (b'{"contractVersion":1,' + data[1:], 1),
                          (data.replace(b'"contractVersion":', b'"contractVersion": ', 1), 8),
                          (data.replace(b'"contractVersion":' + str(json.loads(data)['contractVersion']).encode(), b'"contractVersion":1e1', 1), 1),
                          (b' ' * (MAX_BYTES+1), 1)]:
            rejected(lambda: readers[kind](bad), code); raw_checks += 1
        reversed_value = dict(reversed(list(json.loads(data).items())))
        rejected(lambda: readers[kind](encode(reversed_value)), 8); raw_checks += 1
    # Nine creation/recovery boundary outcomes, with no real publication or replay claim.
    created, publish = creation_cut(None, request, context, True); assert publish and created == raw['created']
    assert creation_cut(created, request, context, True) == (created, False)
    assert creation_cut(created, request, context, False) == (created, False)
    read_snapshot(raw['snapshot'], created, request, context)
    rejected(lambda: creation_cut(None, request, context, False), 7)
    rejected(lambda: creation_cut(created, request, context, False, False), 7)
    rejected(lambda: read_snapshot(raw['snapshot'], None, request, context), 7)
    rejected(lambda: creation_cut(created, context.request(seed=1), context, True), 6)
    later = json.loads(raw['snapshot']); later['stateVersion'] = 2
    rejected(lambda: readers['snapshot'](encode(later)), 6)
    # Independently trusted request/context required even when all embedded hashes are changed.
    forks = [Context(), Context(setup=json.loads(world.CREATION['alternateHolderCanonicalUtf8']))]
    changed_config = copy.deepcopy(context.config); changed_config['windows'][0]['decisionBudgetMilliseconds'] += 1
    forks.append(Context(config=changed_config))
    rebound_pack = copy.deepcopy(context.pack)
    # Existing003A provenance fork: change only supplied seed provenance, then explicitly rebind Setup.
    rebound_pack['scenarios'][0]['initialPlacements'][0]['initialAmmunition']['origin']['references'][0]['locator'] += '.fork'
    rebound_setup = copy.deepcopy(context.setup); rebound_setup['content'] = init.selection(rebound_pack)
    rebound_setup['setupHash'] = init.setup_hash(rebound_setup)
    forks.append(Context(setup=rebound_setup, pack=rebound_pack))
    identities = set(); count = 0
    for ctx in forks:
        for seed in (0, 1, 2**64-1):
            req = ctx.request(seed=seed); cr = encode(canonical(make_created(req, ctx), 'Created'))
            sn = encode(canonical(make_snapshot(cr, req, ctx), 'Snapshot'))
            read_created(cr, req, ctx); read_snapshot(sn, cr, req, ctx)
            identities.add(binding(req)); count += 1
            if req != request: rejected(lambda: read_created(cr, request, context), 6)
    assert len(identities) == count
    # Turn boundaries propagate through certified Content, Setup, element ledgers and preamble.
    for turn in (1, 111):
        pack = copy.deepcopy(context.pack); setup = copy.deepcopy(context.setup)
        pack['scenarios'][0]['start']['gameTurn'] = turn
        pack['scenarios'][0]['end']['gameTurn'] = turn
        for placement in pack['scenarios'][0]['initialPlacements']:
            placement['initialReadiness']['gameTurn'] = turn
        setup['initialGameTurn'] = turn
        setup['stageEntry']['gameTurn'] = turn
        setup['combatInitialization']['gameTurn'] = turn
        setup['content'] = init.selection(pack); setup['setupHash'] = init.setup_hash(setup)
        ctx = Context(setup=setup, pack=pack); req = ctx.request()
        cr = encode(canonical(make_created(req, ctx), 'Created'))
        assert read_created(cr, req, ctx)['sequencePosition']['gameTurn'] == turn
        sn = encode(canonical(make_snapshot(cr, req, ctx), 'Snapshot'))
        read_snapshot(sn, cr, req, ctx)
    for value in ({}, 'unsupported-lot'):
        bad = json.loads(raw['snapshot']); bad['world']['brokenVehicleLots'] = [value]
        rejected(lambda: readers['snapshot'](encode(bad)), 1)
    for campaign in ('a', 'z'*128): read_request(encode(context.request(campaign=campaign)), context)
    # Wrong-type coverage protects nested imported shape boundaries, including empty arrays.
    def leaves(v, path=()):
        if type(v) is dict:
            for k, x in v.items(): yield from leaves(x, path + (k,))
        elif type(v) is list and v:
            for i, x in enumerate(v): yield from leaves(x, path + (i,))
        else: yield path, v
    type_checks = 0
    for kind, data in raw.items():
        value = json.loads(data)
        for path, leaf in leaves(value):
            changed = copy.deepcopy(value); parent = changed
            for part in path[:-1]: parent = parent[part]
            parent[path[-1]] = {} if leaf is not None else 0
            rejected(lambda: readers[kind](encode(changed)), 1)
            type_checks += 1
    # No checksum-only laundering of unsupported rules, even with a caller recomputed hash.
    changed = json.loads(raw['manifest']); changed['artifacts'][0]['contentHash'] = 'sha256:'+'1'*64
    rejected(lambda: read_manifest(encode(changed)), 4)
    print(f'PASS: 4 canonical goldens; 11 Rules artifacts/11 rulings; {len(f["negativeVectors"])} mutations; '
          f'{raw_checks} raw-byte rejections; 9 recovery boundary checks; {count} identity/context forks; 2 turn boundaries. '
          f'{type_checks} nested type rejections. Creation-cut oracle only; no runtime replay.')


if __name__ == '__main__': main()
