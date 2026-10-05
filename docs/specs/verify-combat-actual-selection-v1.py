#!/usr/bin/env python3
"""Private actual selection consistency oracle; independently trusted ledger required."""
import copy, hashlib, importlib.util, json, sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
REPOSITORY = ROOT.parent.parent

class Invalid(ValueError):
    def __init__(self, code, path=''):
        self.code, self.path = f'CMB-ASE-{code:03}', path
        super().__init__(f'{self.code} {path or "/"}')

def require(ok, code, path=''):
    if not ok: raise Invalid(code, path)

def dependency_bytes(path): return (REPOSITORY / path).read_bytes()

def verify_dependencies():
    for path, expected in DEPENDENCIES.items():
        try: actual = hashlib.sha256(dependency_bytes(path)).hexdigest()
        except OSError as error: raise Invalid(9, path) from error
        require(actual == expected, 9, path)

def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module); return module

DEPENDENCIES = {
    'docs/specs/combat-positive-entry-v1.schema.json': '73724384ba3cf61b462ef7b47ae70e5108281a6bbaf0b80878c55247e4e6fa36',
    'docs/specs/fixtures/combat-positive-entry-v1.json': 'eb59146f34bfdc47753b9f0aff7f40709eba4266aff522655ac6886be15b951e',
    'docs/specs/verify-combat-positive-entry-v1.py': 'b461a8bc6a194def5f03767783b49b5797a5630b6a7c26a986f7bfe2ee97c172',
    'docs/specs/combat-reserve-designation-v1.schema.json': 'f324fb22e53ff6d9e084c2f65532e5af89277f632087549756b7df1dcb1710d9',
    'docs/specs/fixtures/combat-reserve-designation-v1.json': '6c8abb2038bb61c739e3bd3cab5c8adf00689c265ec3c6bc6ea29bebe66b12c2',
    'docs/specs/verify-combat-reserve-designation-v1.py': 'fa40ec6b10d0d5ecc113aefba47ff4df44ae616851acdc827e9f676f2412e465',
    'docs/specs/verify-combat-stage-entry-v1.py': '85cd2b32f9ad104ec29034180775e1ac1905e5aa7f78caa3c1a26e6675901435',
    'docs/specs/verify-combat-inherited-movement-lifecycle-v1.py': 'f2ee2292df3fe370ace289dcd01747153dac6d78b82c924b2f3da27df1d81920',
    'docs/specs/combat-inherited-movement-lifecycle-v1.schema.json': 'aac4e07f9546d457defd9042ae701434683e7c22fab2071fe88f872d8d6bc072',
    'docs/specs/verify-combat-inherited-breakdown-completion-v1.py': 'db903e019bb457c02930876e42a70b9204387494012419b0b97572c3c9e12eba',
    'docs/specs/combat-inherited-breakdown-completion-v1.schema.json': '1ff841c704e1aa46e71513ead562a0ca1c54c05b35bdf97018d6854d8773fec3',
    'docs/specs/verify-combat-inherited-selection-v1.py': '99e0cdb1cc78089e4af93b8e995ba04483f6bdab86edaf7c6e2e251442cd0bb7',
    'docs/specs/verify-combat-selection-steps-v1.py': 'dc037282f74c34246d350faebeea55e81677d27dd181de858b8270e4eaa73c86',
    'docs/specs/combat-selection-steps-v1.schema.json': 'ef24e125012ab390bb0c932a09472b8220d7e8c95c1b19c29bdb611be532162d',
    'docs/specs/verify-combat-sealed-round-v2.py': 'd1091b5d1d6fb1d9ac88da45313c88fcbc922f763e62d01af44311abc8656bf9',
    'docs/specs/verify-combat-result-settlement-v2.py': 'a6a781976f26b78a9dc098ebfbb3b516284aa1d5873744d96d8fa23aa50d83a2',
}
verify_dependencies()
pe = load('actual_entry', 'verify-combat-positive-entry-v1.py')
st = pe.steps
seq, inputs, env = st.seq, st.inputs, st.env
encode, sha, digest = st.encode, st.sha, st.digest
command, trusted, edge = st.command, st.trusted, st.edge
candidate, active_window, admitted_timing = st.candidate, st.active_window, st.admitted_timing
INVENTORY = json.loads((ROOT / 'combat-actual-selection-v1.schema.json').read_text())
require(INVENTORY['sourceHashes'] == DEPENDENCIES, 9)
OWN_SCHEMA = {k: [tuple(f.split(':')) for f in fields.split()] for k, fields in INVENTORY['objects'].items()}
SCHEMA = st.SCHEMA | pe.SCHEMA | OWN_SCHEMA
TAGS, KINDS = INVENTORY['effectTags'], st.KINDS
FIXTURE = ROOT / 'fixtures/combat-actual-selection-v1.json'
_ENTRY_CACHE = {}
_PROOF_CACHE = {}

def typed(value, kind, path='', depth=0):
    require(depth <= 32, 1, path)
    if kind.endswith('?'):
        if value is not None: typed(value, kind[:-1], path, depth)
    elif kind == 'utf8': require(type(value) is str and 0 < len(value) <= 1048576 and value.isascii(), 1, path)
    elif kind == 'Effect':
        require(type(value) is dict and type(value.get('kind')) is str and value['kind'] in TAGS, 3, path)
        typed(value, TAGS[value['kind']], path, depth)
    elif kind in pe.SCHEMA and kind not in OWN_SCHEMA:
        try: pe.typed(value, kind, depth)
        except ValueError as error: raise Invalid(1, path) from error
    elif kind in SCHEMA:
        require(type(value) is dict and set(value) == {k for k, _ in SCHEMA[kind]}, 1, path)
        for k, child in SCHEMA[kind]: typed(value[k], child, path+'/'+k, depth+1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value) <= 512, 1, path)
        for i, item in enumerate(value): typed(item, kind[:-2], f'{path}/{i}', depth+1)
    else:
        try: st.typed(value, kind, path, depth)
        except st.Invalid as error: raise Invalid(int(error.code[-3:]), error.path) from error

def canonical(value, kind):
    if kind.endswith('?'): return None if value is None else canonical(value, kind[:-1])
    if kind == 'Effect': return canonical(value, TAGS[value['kind']])
    if kind in OWN_SCHEMA: return {k: canonical(value[k], child) for k, child in OWN_SCHEMA[kind]}
    if kind.endswith('[]'): return [canonical(item, kind[:-2]) for item in value]
    if kind in pe.SCHEMA: return pe.canonical(value, kind)
    return st.canonical(value, kind)

def raw(value, kind):
    typed(value, kind); data = encode(canonical(value, kind)); require(len(data) <= 1048576, 1); return data

def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= 1048576, 1)
    def pairs(items):
        result = {}
        for k, v in items:
            require(k not in result, 1); result[k] = v
        return result
    try:
        value = json.loads(data.decode('utf8'), object_pairs_hook=pairs,
                           parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
        require(raw(value, kind) == data, 8)
    except (UnicodeError, RecursionError) as error: raise Invalid(1) from error
    except ValueError as error:
        if isinstance(error, Invalid): raise
        raise Invalid(1) from error
    return value

def source_bytes(source):
    data = raw(source, 'ActualSelectionSource'); require(source['contractVersion'] == 1, 3)
    require(len(source['selectionEventCanonicalUtf8']) <= 16, 1); return data

def entry_proof(source):
    verify_dependencies(); source_bytes(source)
    data = source['positiveEntrySourceCanonicalUtf8'].encode('ascii')
    if data not in _ENTRY_CACHE:
        try:
            packet = pe.read_source(data); expected = pe.proof(packet)
        except ValueError as error: raise Invalid(4, '/positiveEntrySourceCanonicalUtf8') from error
        require(expected['candidate'] is not None and expected['entry']['stateVersion'] == 13
                and len(packet['entryEventCanonicalUtf8']) == 2, 4)
        _ENTRY_CACHE[data] = pe.raw(expected, 'PositiveEntryProof')
    return pe.parse(_ENTRY_CACHE[data], 'PositiveEntryProof')

def boundary(source):
    p = entry_proof(source); state = p['entry']; packet = json.loads(source['positiveEntrySourceCanonicalUtf8'])
    weather_data = packet['weatherEventCanonicalUtf8'][0].encode('ascii'); weather = json.loads(weather_data)
    breakdown_data = packet['entryEventCanonicalUtf8'][-1].encode('ascii')
    value = dict(contractVersion=1, positiveEntrySourceId=p['sourceId'], positiveEntrySourceHash=p['sourceHash'],
        creationBinding=state['creationBinding'], creationEventHash=state['creationEventHash'], cycle=state['cycle'],
        cycleId=state['cycleId'], firstActingSide=state['firstActingSide'], priorVersion=state['stateVersion'],
        priorPrefix=state['prefix'], reserveCompletionReceiptId=state['completionReceiptId'],
        movementCompletionReceiptId=state['movementEnd']['completionReceiptId'],
        breakdownCompletionReceiptId=state['breakdownCompletionReceiptId'], breakdownCompletionEventHash=sha(breakdown_data),
        position=state['sequencePosition'], world=state['world'], randomState=state['randomState'],
        weather=dict(gameTurn=1, operationStage=1, attackerKind=state['operationStageWeather'][0]['kind'],
            defenderKind=state['operationStageWeather'][0]['kind'], weatherReceiptId=weather['receiptId'], weatherEventHash=sha(weather_data)),
        breakdownFlow=state['breakdownFlow'], reactionWindow=None, movementEnd=state['movementEnd'], candidate=p['candidate'])
    return parse(raw(value, 'ActualSelectionBoundary'), 'ActualSelectionBoundary')

def initial(b):
    return dict(contractVersion=1, boundaryHash=sha(raw(b, 'ActualSelectionBoundary')),
        segmentId='asg.'+digest(INVENTORY['domains']['segment'], raw(b, 'ActualSelectionBoundary')),
        stateVersion=b['priorVersion'], prefix=b['priorPrefix'], stepIndex=0, selectionOutcome='unopened', selection=None,
        selectionReceiptId=None, declineReceiptId=None, cancellationReceiptId=None, selectionWindow=None, rbaWindow=None,
        stepReceipts=[], receipts=[], closed=False)

def window(state, b, kind, now):
    try: timing = inputs.make_timing(env.Context().config, kind, now)
    except inputs.Invalid as e: raise Invalid(5, '/admittedAt') from e
    side = b['cycle']['actingSide']; owner = side if kind == 'selection' else ('commonwealth' if side == 'axis' else 'axis')
    return dict(decisionId=state['segmentId']+'.'+kind, owner=owner, timing=timing)


def active_window(s):
    if s['selectionOutcome'] == 'pending': return s['selectionWindow']
    if s['selectionOutcome'] == 'selected' and s['rbaWindow'] is not None and s['declineReceiptId'] is None: return s['rbaWindow']
    return None


def admitted_timing(w, inp):
    t = copy.deepcopy(w['timing']); now = inp['admittedAt']
    if now is not None: t['highWaterUnixMilliseconds'] = max(now, t['highWaterUnixMilliseconds'])
    return t


def transition(b, prior, inp):
    """Immutable contract transition from independently trusted boundary + authenticated input."""
    typed(inp, 'Input'); cmd = inp['command']; kind = cmd['kind']; actor = inp['actor']
    require(cmd['contractVersion'] == 1 and kind in KINDS, 3)
    require(cmd['segmentId'] == prior['segmentId'], 4)
    allowed = {'open-segment': {'expectedPriorVersion'}, 'close-empty-selection': {'expectedPriorVersion'},
        'choose-selection': {'decisionId', 'choice', 'candidate'}, 'decline-rba': {'decisionId', 'participant'},
        'complete-step': {'fromPositionId', 'expectedPriorVersion'}, 'open-rba': {'fromPositionId', 'expectedPriorVersion'},
        'expire-window': {'decisionId'}, 'controller-unavailable': {'decisionId'}}[kind]
    for k in ('decisionId', 'fromPositionId', 'expectedPriorVersion', 'choice', 'candidate', 'participant'):
        require(k in allowed or cmd[k] is None, 3, '/command/'+k)
    system = kind not in ('choose-selection', 'decline-rba')
    require(actor == 'system' if system else actor in ('axis', 'commonwealth'), 4, '/actor')
    ch = sha(encode(canonical(cmd, 'Command')))
    duplicate = next((r for r in prior['receipts'] if r['commandHash'] == ch), None)
    if duplicate:
        require(duplicate['actor'] == actor, 4, '/actor')
        return copy.deepcopy(prior), None, duplicate['receiptId']
    w = active_window(prior)
    before_rba_unavailable = (kind == 'controller-unavailable' and prior['selectionOutcome'] == 'selected'
        and prior['stepIndex'] == 2 and prior['rbaWindow'] is None and cmd['decisionId'] == prior['segmentId']+'.rba')
    if not before_rba_unavailable and kind in ('expire-window', 'controller-unavailable') and (w is None or cmd['decisionId'] != w['decisionId']):
        return copy.deepcopy(prior), None, None
    require(not prior['closed'], 6)
    require(len(prior['receipts']) < 16 and prior['stateVersion'] < 2**63-1, 6)
    s = copy.deepcopy(prior); choices = candidate(b); step = s['stepIndex']; positions = edge(b)['combatPositionIds']
    if kind in ('open-segment', 'close-empty-selection', 'complete-step', 'open-rba'):
        require(cmd['expectedPriorVersion'] == prior['stateVersion'], 6)
    if kind in ('complete-step', 'open-rba'): require(cmd['fromPositionId'] == positions[step], 6)
    if kind in ('complete-step', 'close-empty-selection'):
        require(inp['admittedAt'] is None and inp['clockAvailable'], 5)
    if kind == 'open-segment':
        require(s['selectionOutcome'] == 'unopened' and not s['receipts'], 6)
        require(inp['admittedAt'] is not None if choices and inp['clockAvailable'] else True, 5)
        require(choices is not None or inp['admittedAt'] is None, 5)
        opens_window = choices is not None and inp['clockAvailable']
        s['selectionOutcome'] = 'pending' if opens_window else 'system-no-selection'
        s['selectionWindow'] = window(s, b, 'selection', inp['admittedAt']) if opens_window else None
        effect = dict(kind='segment-opened', boundaryHash=s['boundaryHash'], window=s['selectionWindow'])
    elif kind == 'close-empty-selection':
        require(s['selectionOutcome'] == 'system-no-selection' and s['selectionWindow'] is None, 6)
        effect = dict(kind='selection-closed', outcome='no-selection', candidate=None, timing=None)
    elif kind == 'choose-selection':
        require(s['selectionOutcome'] == 'pending' and w is not None and cmd['decisionId'] == w['decisionId'], 6)
        require(actor == w['owner'], 4)
        require(cmd['choice'] in ('select-close-assault', 'finish-without-attack'), 3)
        chosen = cmd['choice'] == 'select-close-assault'
        require(cmd['candidate'] == choices if chosen else cmd['candidate'] is None, 4)
        require(inputs.clock_gate(w['timing'], inp['admittedAt'], inp['clockAvailable']) == 'before-deadline', 5)
        effect = dict(kind='selection-closed', outcome='selected' if chosen else 'no-selection', candidate=choices if chosen else None,
                      timing=admitted_timing(w, inp))
    elif kind == 'open-rba':
        require(step == 2 and s['selectionOutcome'] == 'selected' and s['rbaWindow'] is None, 6)
        require(inp['clockAvailable'] and inp['admittedAt'] is not None, 5)
        require(inp['admittedAt'] >= s['selectionWindow']['timing']['highWaterUnixMilliseconds'], 5)
        s['rbaWindow'] = window(s, b, 'rba', inp['admittedAt'])
        effect = dict(kind='rba-opened', selectionReceiptId=s['selectionReceiptId'], window=s['rbaWindow'])
    elif kind == 'decline-rba':
        require(step == 2 and s['selectionOutcome'] == 'selected' and w is not None and cmd['decisionId'] == w['decisionId'], 6)
        require(actor == w['owner'] and cmd['participant'] == s['selection']['defender']['unit'], 4)
        require(inputs.clock_gate(w['timing'], inp['admittedAt'], inp['clockAvailable']) == 'before-deadline', 5)
        effect = dict(kind='rba-declined', selectionReceiptId=s['selectionReceiptId'], participant=cmd['participant'], timing=admitted_timing(w, inp))
    elif kind in ('expire-window', 'controller-unavailable'):
        if before_rba_unavailable:
            effect = dict(kind='selection-cancelled', selectionReceiptId=s['selectionReceiptId'], timing=None)
        else:
            require(w is not None, 6)
            gate = inputs.clock_gate(w['timing'], inp['admittedAt'], inp['clockAvailable'])
            if kind == 'expire-window' and gate == 'before-deadline': return copy.deepcopy(prior), None, None
            if s['selectionOutcome'] == 'pending':
                effect = dict(kind='selection-closed', outcome='no-selection', candidate=None, timing=admitted_timing(w, inp))
            else: effect = dict(kind='selection-cancelled', selectionReceiptId=s['selectionReceiptId'], timing=admitted_timing(w, inp))
    else:
        require(s['selectionOutcome'] in ('selected', 'no-selection', 'cancelled'), 6)
        no_attack = s['selectionOutcome'] != 'selected'
        require(no_attack or step < 3, 7)  # Prepared/committed gates belong to C3b/c.
        if not no_attack and step == 2: require(s['declineReceiptId'] is not None, 6)
        disposition = s['cancellationReceiptId'] or (s['declineReceiptId'] if step >= 2 else None) or s['selectionReceiptId']
        effect = dict(kind='step-completed', fromPositionId=positions[step],
            toPositionId=positions[step+1] if step < 5 else edge(b)['releasePositionId'],
            previousStepReceiptId=s['stepReceipts'][-1] if s['stepReceipts'] else s['receipts'][0]['receiptId'],
            dispositionReceiptId=disposition, proofKind='no-attack' if no_attack else ['no-gun-positions', 'no-barrage-work', 'accepted-decline'][step])
    event = dict(contractVersion=1, eventType='actual-combat-'+effect['kind'], campaignId=b['cycle']['campaignId'], rulesetHash=b['cycle']['rulesetHash'],
        configurationHash=b['cycle']['admittedPolicyBundleDigest'], positiveEntrySourceHash=b['positiveEntrySourceHash'], cycleId=b['cycleId'],
        segmentId=s['segmentId'], priorVersion=prior['stateVersion'], stateVersion=prior['stateVersion']+1, priorPrefix=prior['prefix'],
        input=canonical(inp, 'Input'), effect=effect)
    rid = 'asc.'+digest(INVENTORY['domains']['receipt'], encode(event)); event['receiptId'] = rid
    event_bytes = raw(event, 'Event'); require(len(event_bytes) <= 1048576, 1)
    if effect['kind'] == 'selection-closed':
        s.update(selectionOutcome=effect['outcome'], selection=effect['candidate'], selectionReceiptId=rid)
        if s['selectionWindow'] is not None: s['selectionWindow']['timing'] = effect['timing']
    elif effect['kind'] == 'rba-declined': s['declineReceiptId'] = rid; s['rbaWindow']['timing'] = effect['timing']
    elif effect['kind'] == 'selection-cancelled':
        s.update(selectionOutcome='cancelled', cancellationReceiptId=rid)
        if s['rbaWindow'] is not None: s['rbaWindow']['timing'] = effect['timing']
    elif effect['kind'] == 'step-completed':
        s['stepReceipts'].append(rid); s['stepIndex'] += 1; s['closed'] = s['stepIndex'] == 6
    s['stateVersion'] = event['stateVersion']; s['prefix'] = seq.prefix_event(prior['prefix'], event_bytes)
    s['receipts'].append(dict(commandHash=ch, eventHash=sha(event_bytes), receiptId=rid, actor=actor, stateVersion=s['stateVersion']))
    typed(s, 'Control'); require(len(encode(s)) <= 1048576, 1)
    return s, event_bytes, rid


def owned_ledger(trusted_inputs, count):
    require(type(trusted_inputs) is list and len(trusted_inputs) == count and count <= 16, 1)
    return [parse(raw(inp, 'Input'), 'Input') for inp in trusted_inputs]

def read_event(data, b, prior, trusted_input):
    parse(data, 'Event'); after, expected, receipt = transition(b, prior, trusted_input)
    require(expected is not None and data == expected, 6); return after

def replay(source, trusted_inputs):
    verify_dependencies(); source = parse(source_bytes(source), 'ActualSelectionSource')
    ledger = owned_ledger(trusted_inputs, len(source['selectionEventCanonicalUtf8']))
    b = boundary(source); state = initial(b)
    for text, inp in zip(source['selectionEventCanonicalUtf8'], ledger): state = read_event(text.encode('ascii'), b, state, inp)
    return parse(raw(state, 'Control'), 'Control')

def apply(source, trusted_history_inputs, trusted_current_input):
    verify_dependencies(); inp = parse(raw(trusted_current_input, 'Input'), 'Input')
    source = parse(source_bytes(source), 'ActualSelectionSource'); prior = replay(source, trusted_history_inputs)
    state, event, receipt = transition(boundary(source), prior, inp)
    if event is None and receipt is not None:
        event = next(text.encode('ascii') for text in source['selectionEventCanonicalUtf8'] if json.loads(text)['receiptId'] == receipt)
    return parse(raw(state, 'Control'), 'Control'), event, receipt

def proof(source, trusted_inputs):
    verify_dependencies(); data = source_bytes(source)
    ledger = owned_ledger(trusted_inputs,len(source['selectionEventCanonicalUtf8']))
    # Cache only already replayed complete bytes AND independently supplied complete ledger.
    # Digest-only/source-only keys would violate the trust precondition.
    key = (data,tuple(raw(inp,'Input') for inp in ledger))
    if key not in _PROOF_CACHE:
        state = replay(source,ledger); value = digest(INVENTORY['domains']['source'],data)
        result = raw(dict(contractVersion=1,sourceId='asrc.'+value,sourceHash='sha256:'+value,
            positiveEntryProof=entry_proof(source),boundary=boundary(source),control=state),'ActualSelectionProof')
        if len(_PROOF_CACHE)>=128: _PROOF_CACHE.clear()
        _PROOF_CACHE[key] = result
    return parse(_PROOF_CACHE[key],'ActualSelectionProof')


def read_source(data, trusted_inputs):
    verify_dependencies(); source = parse(data, 'ActualSelectionSource'); replay(source, trusted_inputs); return source

def read_proof(data, source, trusted_inputs):
    verify_dependencies(); value = parse(data, 'ActualSelectionProof')
    require(data == raw(proof(source, trusted_inputs), 'ActualSelectionProof'), 6); return value

def read_control(data, source, trusted_inputs):
    verify_dependencies(); value = parse(data, 'Control')
    require(data == raw(replay(source, trusted_inputs), 'Control'), 6); return value

def original_source(side):
    row = next(c for c in json.loads(pe.FIXTURE.read_text())['cases'] if c['actor'] == side)
    return dict(contractVersion=1, positiveEntrySourceCanonicalUtf8=row['sourceCanonicalUtf8'],
                selectionEventCanonicalUtf8=[])


def trace(side, variant='selected'):
    source = original_source(side); ledger = []; sources = [copy.deepcopy(source)]
    control = replay(source, ledger); controls = [control]; inputs = []; events = []
    b = boundary(source)
    def accept(kind, actor='system', now=None, available=True, **fields):
        nonlocal source, control
        inp = trusted(command(control, kind, **fields), actor, now, available)
        control, event, receipt = apply(source, ledger, inp)
        assert event is not None and receipt == json.loads(event)['receiptId']
        ledger.append(copy.deepcopy(inp)); inputs.append(copy.deepcopy(inp)); events.append(event)
        source['selectionEventCanonicalUtf8'].append(event.decode('ascii'))
        sources.append(copy.deepcopy(source)); controls.append(copy.deepcopy(control))
    accept('open-segment', now=None if variant == 'opening-unavailable' else 1000,
           available=variant != 'opening-unavailable', expectedPriorVersion=13)
    if variant == 'opening-unavailable':
        accept('close-empty-selection', expectedPriorVersion=14)
    elif variant == 'finish':
        accept('choose-selection', actor=side, now=1100, decisionId=control['segmentId']+'.selection',
               choice='finish-without-attack')
    elif variant == 'selection-expired':
        accept('expire-window', now=31000, decisionId=control['segmentId']+'.selection')
    elif variant == 'selection-unavailable':
        accept('controller-unavailable', available=False, decisionId=control['segmentId']+'.selection')
    else:
        accept('choose-selection', actor=side, now=1100, decisionId=control['segmentId']+'.selection',
               choice='select-close-assault', candidate=b['candidate'])
    for step in range(6):
        if step == 2 and control['selectionOutcome'] == 'selected':
            if variant == 'rba-before-open-unavailable':
                accept('controller-unavailable', available=False, decisionId=control['segmentId']+'.rba')
            else:
                accept('open-rba', now=2000, expectedPriorVersion=control['stateVersion'],
                       fromPositionId=edge(b)['combatPositionIds'][2])
                if variant == 'rba-expired':
                    accept('expire-window', now=32000, decisionId=control['segmentId']+'.rba')
                elif variant == 'rba-unavailable':
                    accept('controller-unavailable', available=False, decisionId=control['segmentId']+'.rba')
                else:
                    accept('decline-rba', actor=b['candidate']['defender']['unit']['originalSide'], now=2100,
                           decisionId=control['segmentId']+'.rba', participant=b['candidate']['defender']['unit'])
        accept('complete-step', expectedPriorVersion=control['stateVersion'], fromPositionId=edge(b)['combatPositionIds'][step])
        if variant == 'selected' and step == 2: break
    return sources, controls, inputs, events


VARIANTS = ('selected', 'opening-unavailable', 'finish', 'selection-expired', 'selection-unavailable',
            'rba-before-open-unavailable', 'rba-expired', 'rba-unavailable')


def semantic_expectations(side, variant):
    sources, controls, ledger, events = trace(side, variant)
    b = boundary(sources[0]); entry = pe.replay(json.loads(sources[0]['positiveEntrySourceCanonicalUtf8']))
    assert b['world'] == entry['world'] and b['randomState'] == entry['randomState']
    assert b['position']['activeSide'] is None and len(entry['receipts']) == 12
    assert b['priorVersion'] == 13 and b['priorPrefix'] == entry['prefix']
    assert controls[0]['stateVersion'] == 13 and controls[1]['stateVersion'] == 14
    assert [s['stateVersion'] for s in controls] == list(range(13, 14+len(events)))
    if variant == 'selected':
        assert len(events) == 7 and [s['selectionOutcome'] for s in controls[:3]] == ['unopened','pending','selected']
        assert controls[-2]['stateVersion'] == 19 and controls[-2]['declineReceiptId']
        assert controls[-1]['stateVersion'] == 20 and controls[-1]['stepIndex'] == 3 and not controls[-1]['closed']
        assert len(controls[-1]['stepReceipts']) == 3
        assert ledger[1]['actor'] == side and ledger[5]['actor'] != side and ledger[5]['actor'] != 'system'
        assert controls[-1]['selectionWindow']['timing']['deadlineUnixMilliseconds'] == 31000
        assert controls[-1]['rbaWindow']['timing']['deadlineUnixMilliseconds'] == 32000
        assert controls[-1]['selectionWindow']['timing']['highWaterUnixMilliseconds'] == 1100
        assert controls[-1]['rbaWindow']['timing']['highWaterUnixMilliseconds'] == 2100
    else:
        assert controls[-1]['stepIndex'] == 6 and controls[-1]['closed'] and len(controls[-1]['stepReceipts']) == 6
        assert controls[-1]['declineReceiptId'] is None
        assert json.loads(events[-1])['effect']['toPositionId'] == edge(b)['releasePositionId']
    for source in sources:
        assert source['positiveEntrySourceCanonicalUtf8'] == sources[0]['positiveEntrySourceCanonicalUtf8']
        p = proof(source, ledger[:len(source['selectionEventCanonicalUtf8'])])
        assert p['boundary'] == b and p['positiveEntryProof']['entry']['world'] == entry['world']
        assert p['positiveEntryProof']['entry']['randomState'] == entry['randomState']
        assert len(p['positiveEntryProof']['entry']['receipts']) == 12
    return sources, controls, ledger, events


def semantic_red():
    failures = []
    for side in ('axis','commonwealth'):
        for variant in VARIANTS:
            try: semantic_expectations(side, variant)
            except ValueError as error: failures.append((side,variant,str(error)))
    if failures:
        for row in failures: print('RED:', *row)
        raise AssertionError(f'{len(failures)} semantic actual-owner traces failed before implementation')
    print('PASS: 16 semantic actual-owner traces; selected FA20 and seven Reserve Release fallbacks per owner')

def reject(label, call, counts, code=None):
    try: call()
    except Invalid as error:
        if code is not None: assert error.code == f'CMB-ASE-{code:03}', (label,error.code,code,error.path)
        group = label.split('/')[0]; counts[group] = counts.get(group, 0)+1
        return error.code
    raise AssertionError('unexpected actual-selection acceptance: '+label)


def replacement(value):
    if type(value) is bool: return not value
    if type(value) is int: return value+1
    if type(value) is str:
        if value.startswith('sha256:'): return 'sha256:'+('0' if value != 'sha256:'+'0'*64 else '1')*64
        if len(value) == 64 and all(c in '0123456789abcdef' for c in value): return '0'*64 if value != '0'*64 else '1'*64
        return 'tampered' if value != 'tampered' else 'other'
    if type(value) is list: return [None]
    return 1


def signed_event(value):
    unsigned = {k:v for k,v in value.items() if k != 'receiptId'}
    value['receiptId'] = 'asc.'+digest(INVENTORY['domains']['receipt'], encode(unsigned))
    return encode(value)


def malformed_spellings(data, kind):
    value = json.loads(data); key = next(iter(value))
    return [data+b' ', data+b'\n', b'\xef\xbb\xbf'+data, data[:-1], b'\xff', data+b'{}',
        b'{"'+key.encode()+b'":1,'+data[1:], b'{"provider":null,'+data[1:],
        encode({k:v for k,v in value.items() if k != key}), encode(dict(reversed(list(value.items())))),
        data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1),
        data.replace(b'"contractVersion":1',b'"contractVersion":1e0',1),
        data.replace(b'"contractVersion":1',b'"contractVersion":true',1),
        data.replace(b'contractVersion',b'contract\\u0056ersion',1), b'['*34+b'0'+b']'*34]


def artifact(data): return dict(canonicalUtf8=data.decode('ascii'), byteCount=len(data), sha256=sha(data))


def fixture_case(side, variant, result):
    sources, controls, ledger, events = result
    return dict(owner=side, variant=variant, source=artifact(source_bytes(sources[-1])),
        trustedInputs=[artifact(raw(i,'Input')) for i in ledger], events=[artifact(e) for e in events],
        controls=[artifact(raw(c,'Control')) for c in controls],
        proofs=[artifact(raw(proof(s,ledger[:i]),'ActualSelectionProof')) for i,s in enumerate(sources)],
        expected=dict(stateVersion=controls[-1]['stateVersion'], stepIndex=controls[-1]['stepIndex'],
                      closed=controls[-1]['closed'], prefix=controls[-1]['prefix']))


def verify_case(side, variant, result, counts):
    sources, controls, ledger, events = result; b=boundary(sources[0])
    for cut, source in enumerate(sources):
        assert replay(source,ledger[:cut]) == controls[cut]
        assert read_control(raw(controls[cut],'Control'),source,ledger[:cut]) == controls[cut]
        p=proof(source,ledger[:cut]); assert read_proof(raw(p,'ActualSelectionProof'),source,ledger[:cut]) == p
        assert read_source(source_bytes(source),ledger[:cut]) == source
        counts['cuts'] = counts.get('cuts',0)+1
        suffix=copy.deepcopy(controls[cut])
        for index in range(cut,len(events)): suffix=read_event(events[index],b,suffix,ledger[index])
        assert suffix == controls[-1]
        for index in range(cut):
            for now, available in ((ledger[index]['admittedAt'],ledger[index]['clockAvailable']), (None,False),(999999,True)):
                retry=copy.deepcopy(ledger[index]); retry.update(admittedAt=now,clockAvailable=available)
                same,event,receipt=apply(source,ledger[:cut],retry)
                assert same == controls[cut] and event == events[index] and receipt == json.loads(event)['receiptId']
                counts['retries']=counts.get('retries',0)+1
            invalid=copy.deepcopy(ledger[index]); invalid['admittedAt']=True
            reject('retry-primitive/bool',lambda:apply(source,ledger[:cut],invalid),counts,1)
    for index,event in enumerate(events):
        value=json.loads(event)
        for path,leaf in pe.leaves(value):
            forged=pe.changed(value,path,replacement(leaf))
            # Every leaf including embedded input/effects is checked after a local receipt recomputation.
            forged_bytes = signed_event(forged) if path != ('receiptId',) else encode(forged)
            reject('event/'+str(path),lambda:read_event(forged_bytes,b,controls[index],ledger[index]),counts)
        for bad in malformed_spellings(event,'Event'):
            reject('raw/event',lambda:read_event(bad,b,controls[index],ledger[index]),counts)
        forged_source=copy.deepcopy(sources[-1]); forged_source['selectionEventCanonicalUtf8'][index]=events[(index+1)%len(events)].decode('ascii')
        reject('history/reordered-event',lambda:replay(forged_source,ledger),counts)
        forged_ledger=copy.deepcopy(ledger);forged_ledger[index]['actor']='axis' if ledger[index]['actor'] != 'axis' else 'commonwealth'
        reject('ledger/actor',lambda:replay(sources[-1],forged_ledger),counts)
        forged_ledger=copy.deepcopy(ledger);forged_ledger[index]['admittedAt']=1101 if ledger[index]['admittedAt'] != 1101 else 1102
        reject('ledger/time',lambda:replay(sources[-1],forged_ledger),counts)
    for extra in (ledger[:-1],ledger+[ledger[0]],list(reversed(ledger))):
        reject('ledger/count-order',lambda:replay(sources[-1],extra),counts)
    p=proof(sources[-1],ledger)
    for path,leaf in pe.leaves(p):
        forged=pe.changed(p,path,replacement(leaf))
        reject('proof/'+str(path),lambda:read_proof(encode(forged),sources[-1],ledger),counts)
    for kind,data,reader in (
        ('ActualSelectionSource',source_bytes(sources[-1]),lambda d:read_source(d,ledger)),
        ('Control',raw(controls[-1],'Control'),lambda d:read_control(d,sources[-1],ledger)),
        ('ActualSelectionProof',raw(p,'ActualSelectionProof'),lambda d:read_proof(d,sources[-1],ledger))):
        for bad in malformed_spellings(data,kind): reject('raw/'+kind,lambda:reader(bad),counts)
    # Returned projections and caller buffers cannot mutate retained cache/proof/retry evidence.
    frozen=source_bytes(sources[-1]); frozen_ledger=copy.deepcopy(ledger); original=raw(p,'ActualSelectionProof')
    p['boundary']['world']['elements'].clear(); p['control']['receipts'].clear()
    caller=copy.deepcopy(sources[-1]); caller['selectionEventCanonicalUtf8'].clear()
    returned=read_source(frozen,frozen_ledger);returned['selectionEventCanonicalUtf8'].clear()
    returned_control=replay(sources[-1],ledger);returned_control['stepReceipts'].clear()
    assert raw(proof(sources[-1],ledger),'ActualSelectionProof') == original
    assert source_bytes(sources[-1]) == frozen and ledger == frozen_ledger
    counts['ownership']=counts.get('ownership',0)+4


def admission_checks(counts):
    for side in ('axis','commonwealth'):
        source=original_source(side); packet=json.loads(source['positiveEntrySourceCanonicalUtf8'])
        # All six retained legal entry cuts remain entry-readable; only two completed cuts select.
        row=next(c for c in json.loads(pe.FIXTURE.read_text())['cases'] if c['actor']==side)
        for cut in range(3):
            earlier=copy.deepcopy(packet);earlier['entryEventCanonicalUtf8']=earlier['entryEventCanonicalUtf8'][:cut]
            data=pe.packet_bytes(earlier); p=pe.proof(pe.read_source(data))
            assert pe.raw(p,'PositiveEntryProof').decode('ascii') == row['proofCanonicalUtf8'][cut]
            counts['entry-cuts']=counts.get('entry-cuts',0)+1
            bad=copy.deepcopy(source);bad['positiveEntrySourceCanonicalUtf8']=data.decode('ascii')
            if cut<2: reject('entry/premature',lambda:replay(bad,[]),counts,4)
            else: assert replay(bad,[])['stateVersion']==13
        for key in pe.HISTORY_KEYS+('entryEventCanonicalUtf8',):
            for action in ('missing','duplicate','reverse'):
                bad=copy.deepcopy(packet)
                if action=='missing':bad[key].pop()
                elif action=='duplicate':bad[key].append(bad[key][0])
                else:
                    if len(bad[key])<2:continue
                    bad[key].reverse()
                candidate_source=copy.deepcopy(source);candidate_source['positiveEntrySourceCanonicalUtf8']=pe.packet_bytes(bad).decode('ascii')
                reject('entry/'+key+action,lambda:replay(candidate_source,[]),counts,4)
        # Mutate every original Request/Created/history leaf, with complete original records retained.
        for key in ('requestCanonicalUtf8','createdCanonicalUtf8')+pe.HISTORY_KEYS+('entryEventCanonicalUtf8',):
            records=[packet[key]] if type(packet[key]) is str else packet[key]
            for index,text in enumerate(records):
                value=json.loads(text)
                for path,leaf in pe.leaves(value):
                    forged=pe.changed(value,path,replacement(leaf));bad=copy.deepcopy(packet)
                    if type(bad[key]) is str:bad[key]=encode(forged).decode('ascii')
                    else:bad[key][index]=encode(forged).decode('ascii')
                    candidate_source=copy.deepcopy(source);candidate_source['positiveEntrySourceCanonicalUtf8']=pe.packet_bytes(bad).decode('ascii')
                    reject('entry-leaf/'+key+str(path),lambda:replay(candidate_source,[]),counts,4)
        foreign=original_source('commonwealth' if side=='axis' else 'axis')
        a=trace(side);bad=copy.deepcopy(a[0][-1]);bad['positiveEntrySourceCanonicalUtf8']=foreign['positiveEntrySourceCanonicalUtf8']
        reject('entry/foreign-with-events',lambda:replay(bad,a[2]),counts)
    # Current historical family readers remain incompatible with the new record grammar.
    source,controls,ledger,events=trace('axis');new=source[-1]
    for old in (json.loads(st.FIXTURE.read_text())['cases'][0], {'sourceHash':proof(new,ledger)['sourceHash']},controls[-1]):
        reject('family/old-or-cache-only',lambda:read_source(encode(old),ledger),counts)
    for field in ('publicAction','observation','provider','snapshot','hiddenSeal','transport'):
        for value,kind in ((new,'ActualSelectionSource'),(proof(new,ledger),'ActualSelectionProof'),(controls[-1],'Control'),(json.loads(events[1]),'Event'),(ledger[1],'Input')):
            bad=copy.deepcopy(value);bad[field]=None
            reject('privacy/'+field,lambda:parse(encode(bad),kind),counts,1)


def dependency_checks(counts):
    global dependency_bytes, _ENTRY_CACHE, _PROOF_CACHE
    source=original_source('axis'); b=boundary(source)
    proof_data=raw(proof(source,[]),'ActualSelectionProof');control_data=raw(replay(source,[]),'Control')
    inp=trusted(command(initial(b),'open-segment',expectedPriorVersion=13),now=1000)
    source_data=source_bytes(source);original_bytes=dependency_bytes;original_cache=_ENTRY_CACHE;original_proof_cache=_PROOF_CACHE
    class Sentinel(dict):
        def __contains__(self,key): raise AssertionError('dependency check reached cache before rejection')
    try:
        _ENTRY_CACHE=Sentinel(original_cache);_PROOF_CACHE=Sentinel(original_proof_cache)
        for path in DEPENDENCIES:
            dependency_bytes=lambda candidate,p=path:original_bytes(candidate)+(b' ' if candidate==p else b'')
            for call in (lambda:replay(source,[]),lambda:proof(source,[]),lambda:read_source(source_data,[]),
                         lambda:apply(source,[],inp),lambda:read_control(control_data,source,[]),
                         lambda:read_proof(proof_data,source,[]),lambda:boundary(source),lambda:entry_proof(source)):
                reject('pins/'+path,call,counts,9)
    finally: dependency_bytes=original_bytes;_ENTRY_CACHE=original_cache;_PROOF_CACHE=original_proof_cache


def separation_checks(counts):
    # Cold replay must not read blocked historical fixtures/readers or invoke Breakdown's fixture gate.
    original_read_bytes,original_read_text=Path.read_bytes,Path.read_text
    forbidden=('fixtures/combat-inherited-breakdown-completion-v1.json','verify-combat-inherited-snapshot-v1.py',
               'fixtures/combat-inherited-snapshot-v1.json','verify-combat-outward-composition-v1.py',
               'fixtures/combat-outward-composition-v1.json')
    def allowed(path):
        assert not any(str(path).endswith(name) for name in forbidden),str(path)
    def bytes_guard(path):allowed(path);return original_read_bytes(path)
    def text_guard(path,*args,**kwargs):allowed(path);return original_read_text(path,*args,**kwargs)
    before_main,before_check=pe.bd.main,pe.bd.check_fixture
    def forbidden_call(*args,**kwargs):raise AssertionError('blocked historical Breakdown admission invoked')
    try:
        Path.read_bytes,Path.read_text=bytes_guard,text_guard
        pe.bd.main=pe.bd.check_fixture=forbidden_call;_ENTRY_CACHE.clear();_PROOF_CACHE.clear()
        for side in ('axis','commonwealth'):
            source=original_source(side);assert replay(source,[])['stateVersion']==13
            counts['separation']=counts.get('separation',0)+1
    finally:Path.read_bytes,Path.read_text=original_read_bytes,original_read_text;pe.bd.main,pe.bd.check_fixture=before_main,before_check


def clock_order_capacity_checks(counts):
    for side in ('axis','commonwealth'):
        sources,controls,ledger,events=trace(side);b=boundary(sources[0])
        for index in (1,5):
            inp=ledger[index];w=active_window(controls[index]);floor=w['timing']['highWaterUnixMilliseconds'];deadline=w['timing']['deadlineUnixMilliseconds']
            for now,available in ((None,True),(floor-1,True),(floor,False),(deadline,True),(deadline+1,True)):
                bad=copy.deepcopy(inp);bad.update(admittedAt=now,clockAvailable=available)
                reject('clock/player',lambda:apply(sources[index],ledger[:index],bad),counts,5)
                bad['actor']='commonwealth' if inp['actor']=='axis' else 'axis'
                reject('order/wrong-owner-before-clock',lambda:apply(sources[index],ledger[:index],bad),counts,4)
            for now in (floor,deadline-1):
                good=copy.deepcopy(inp);good['admittedAt']=now
                assert apply(sources[index],ledger[:index],good)[1] is not None
                counts['clock-accepted']=counts.get('clock-accepted',0)+1
            for now,code in ((True,1),(1.5,1),(-1,2),(253402300800000,2)):
                bad=copy.deepcopy(inp);bad['admittedAt']=now
                reject('clock/primitive',lambda:apply(sources[index],ledger[:index],bad),counts,code)
            # Stale decision/segment/participant/candidate errors precede equality/unavailable time.
            for now,available in ((deadline,True),(None,False)):
                for field in ('decisionId','segmentId','candidate' if index==1 else 'participant'):
                    bad=copy.deepcopy(inp);bad.update(admittedAt=now,clockAvailable=available)
                    if field=='candidate':bad['command'][field]['targetLocationId']='axis-supply'
                    elif field=='participant':bad['command'][field]['originalSide']=side
                    else:bad['command'][field]='foreign'
                    reject('order/'+field,lambda:apply(sources[index],ledger[:index],bad),counts,6 if field=='decisionId' else 4)
        for index in (0,4):
            inp=ledger[index]
            for now in (253402300769998,253402300769999):
                good=copy.deepcopy(inp);good['admittedAt']=now;assert apply(sources[index],ledger[:index],good)[1]
            for now,available in ((None,True),(253402300770000,True),(253402300799999,True)):
                bad=copy.deepcopy(inp);bad.update(admittedAt=now,clockAvailable=available)
                reject('clock/open',lambda:apply(sources[index],ledger[:index],bad),counts,5)
            stale=copy.deepcopy(inp);stale['command']['expectedPriorVersion']-=1;stale['admittedAt']=253402300799999
            reject('order/stale-version-before-clock',lambda:apply(sources[index],ledger[:index],stale),counts,6)
        before=copy.deepcopy(ledger[4]);before['admittedAt']=1099
        reject('clock/rba-before-floor',lambda:apply(sources[4],ledger[:4],before),counts,5)
        final=controls[-1];cmd=command(final,'complete-step',expectedPriorVersion=20,fromPositionId=edge(b)['combatPositionIds'][3])
        reject('positive/no-fa-completion',lambda:apply(sources[-1],ledger,trusted(cmd)),counts,7)
        for index,state in enumerate(controls):
            for decision in (state['segmentId']+'.obsolete',state['segmentId']+'.selection'):
                if active_window(state) and active_window(state)['decisionId']==decision:continue
                timer=trusted(command(state,'expire-window',decisionId=decision),now=999999)
                same,event,receipt=apply(sources[index],ledger[:index],timer)
                assert same==state and event is None and receipt is None
                counts['no-op']=counts.get('no-op',0)+1
        timer=trusted(command(controls[1],'expire-window',decisionId=controls[1]['segmentId']+'.selection'),now=30999)
        assert apply(sources[1],ledger[:1],timer)==(controls[1],None,None)
        for mutated in (copy.deepcopy(sources[-1]),):
            mutated['selectionEventCanonicalUtf8']*=3
            reject('capacity/events17',lambda:replay(mutated,ledger*3),counts,1)
        reject('capacity/ledger17',lambda:replay(sources[-1],ledger*3),counts,1)
        for kind,value in (('ActualSelectionSource',sources[0]),('Event',json.loads(events[0])),('Control',controls[-1]),('ActualSelectionProof',proof(sources[-1],ledger))):
            reject('capacity/bytes',lambda:parse(raw(value,kind)+b' '*1048576,kind),counts,1)
        reject('capacity/arrays513',lambda:typed(['x']*513,'id[]'),counts,1)
        reject('capacity/depth33',lambda:typed(None,'null',depth=33),counts,1)
        # Private kernel checks capacities unreachable from the bounded public profile without forgery.
        full=copy.deepcopy(controls[1]);full['receipts']*=16
        reject('capacity/receipts16',lambda:transition(b,full,ledger[1]),counts,6)
        overflow=copy.deepcopy(controls[1]);overflow['stateVersion']=2**63-1
        reject('capacity/version-overflow',lambda:transition(b,overflow,ledger[1]),counts,6)
        bad=copy.deepcopy(ledger[0]);bad['command']['expectedPriorVersion']=2**63
        reject('capacity/int64',lambda:apply(sources[0],[],bad),counts,2)
        # Canonicalization never sorts causal arrays, even when syntactically parseable.
        reversed_control=copy.deepcopy(final);reversed_control['stepReceipts'].reverse()
        assert parse(raw(reversed_control,'Control'),'Control')['stepReceipts']==reversed_control['stepReceipts']
        reject('history/step-order',lambda:read_control(raw(reversed_control,'Control'),sources[-1],ledger),counts,6)
        bad=copy.deepcopy(sources[-1]);bad['selectionEventCanonicalUtf8'].reverse()
        assert parse(source_bytes(bad),'ActualSelectionSource')==bad
        reject('history/event-order',lambda:replay(bad,ledger),counts)


def trust_and_family_checks(counts):
    for side in ('axis','commonwealth'):
        sources,controls,ledger,events=trace(side)
        # Coherent replacement ledger demonstrates the authentication precondition's limit.
        alternate=original_source(side);alternate_ledger=[]
        for index,original in enumerate(ledger):
            inp=copy.deepcopy(original)
            if index==1:inp['admittedAt']+=1
            after,event,receipt=apply(alternate,alternate_ledger,inp)
            alternate['selectionEventCanonicalUtf8'].append(event.decode('ascii'));alternate_ledger.append(inp)
        assert replay(alternate,alternate_ledger)['stateVersion']==20
        assert proof(alternate,alternate_ledger)['sourceHash']!=proof(sources[-1],ledger)['sourceHash']
        reject('trust/replacement-chain-original-ledger',lambda:replay(alternate,ledger),counts,6)
        # Borrowed/re-signed original entry evidence cannot be laundered through new framing.
        packet=json.loads(sources[0]['positiveEntrySourceCanonicalUtf8'])
        for record_index in range(2):
            bad=copy.deepcopy(packet);event=json.loads(bad['entryEventCanonicalUtf8'][record_index])
            event['receiptId']=json.loads(bad['entryEventCanonicalUtf8'][1-record_index])['receiptId']
            bad['entryEventCanonicalUtf8'][record_index]=encode(event).decode('ascii')
            source=copy.deepcopy(sources[0]);source['positiveEntrySourceCanonicalUtf8']=pe.packet_bytes(bad).decode('ascii')
            reject('entry/borrowed-receipt',lambda:replay(source,[]),counts,4)
        # Domain re-signing alone cannot make a historical tag/source grammar actual authority.
        for index,event_bytes in enumerate(events):
            value=json.loads(event_bytes);value['eventType']='combat-'+value['effect']['kind']
            reject('family/legacy-tag-resigned',lambda:read_event(signed_event(value),boundary(sources[0]),controls[index],ledger[index]),counts,6)
        for data in (source_bytes(sources[-1]),raw(proof(sources[-1],ledger),'ActualSelectionProof'),events[0]):
            try:st.parse(data,'Event')
            except st.Invalid:counts['legacy-reject']=counts.get('legacy-reject',0)+1
            else:raise AssertionError('legacy Event reader admitted actual grammar')
    # Exact primitive packet limit and one-byte overflow; grammar admission remains separate.
    value='x'*1048574;data=raw(value,'utf8');assert len(data)==1048576 and parse(data,'utf8')==value
    reject('capacity/one-byte-over',lambda:parse(data+b' ','utf8'),counts,1)


def main():
    counts={};verify_dependencies();semantic_red()
    frozen=json.loads(FIXTURE.read_text())
    assert frozen['sourceHashes']==DEPENDENCIES
    assert frozen['schemaHash']==sha((ROOT/'combat-actual-selection-v1.schema.json').read_bytes())
    for side in ('axis','commonwealth'):
        for variant in VARIANTS:
            result=semantic_expectations(side,variant)
            expected=next(c for c in frozen['cases'] if c['owner']==side and c['variant']==variant)
            assert fixture_case(side,variant,result)==expected,(side,variant,'literal mismatch')
            verify_case(side,variant,result,counts)
    admission_checks(counts);separation_checks(counts);clock_order_capacity_checks(counts);trust_and_family_checks(counts)
    dependency_checks(counts)
    print('PASS: 16 literal traces; '+json.dumps(counts,sort_keys=True)+'; full original actual entry retained; separate trusted ledger; private FA stop only')


if __name__ == '__main__': main()
