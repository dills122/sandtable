#!/usr/bin/env python3
"""D2c.1 declarations and isolated first-opening oracle; no runtime admission."""
import copy
import importlib.util
import json
import re
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('release', ROOT/'verify-combat-reserve-release-v1.py')
rel = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rel)
r, steps, world = rel.r, rel.steps, rel.world
encode, sha = rel.encode, rel.sha
INVENTORY = json.loads((ROOT/'combat-inherited-successors-v1.schema.json').read_text())
FIXTURE = ROOT/'fixtures/combat-inherited-successors-v1.json'
SCHEMA = rel.SCHEMA | {k: [tuple(f.split(':')) for f in v.split()] for k, v in INVENTORY['objects'].items()}

class Invalid(ValueError):
    def __init__(self, code):
        self.code = f'CMB-INH-{code:03}'
        super().__init__(self.code)

def require(ok, code):
    if not ok: raise Invalid(code)

def typed(v, kind, depth=0):
    require(depth <= 32, 1)
    if kind.endswith('?'):
        if v is not None: typed(v, kind[:-1], depth)
    elif kind in SCHEMA:
        require(type(v) is dict and set(v) == {k for k, _ in SCHEMA[kind]}, 1)
        for k, t in SCHEMA[kind]: typed(v[k], t, depth+1)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v) <= 512, 1)
        for x in v: typed(x, kind[:-2], depth+1)
    else:
        try: rel.typed(v, kind, depth)
        except rel.Invalid as error: raise Invalid(1) from error

def canonical(v, kind):
    if kind.endswith('?'): return None if v is None else canonical(v, kind[:-1])
    if kind in SCHEMA: return {k: canonical(v[k], t) for k, t in SCHEMA[kind]}
    if kind.endswith('[]'): return [canonical(x, kind[:-2]) for x in v]
    return v

def raw(v, kind):
    typed(v, kind); data = encode(canonical(v, kind))
    require(len(data) <= 1048576, 1)
    return data

def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= 1048576, 1)
    def pairs(items):
        out = {}
        for k, v in items:
            require(k not in out, 1); out[k] = v
        return out
    try: v = json.loads(data.decode('utf8'), object_pairs_hook=pairs,
        parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error: raise Invalid(1) from error
    require(raw(v, kind) == data, 8)
    return v

def edge(): return steps.seq.expected_catalog()['cycles'][0]

def position(name):
    return copy.deepcopy(next(p for p in steps.seq.expected_catalog()['positions'] if p['positionId'] == edge()[name]))

def authority(b):
    q = b['creationRequest']
    return dict(contractVersion=1, campaignId=q['campaignId'], rulesetHash=q['rulesetHash'],
        setupId=q['setupId'], setupHash=q['setupHash'], contentPackId=q['content']['packId'],
        contentHash=q['content']['hash'], scenarioId=q['content']['scenarioId'], gameTurn=1, operationStage=1,
        playerPhaseSlot='first-acting-side', actingSide=b['firstActingSide'], ordinal=1,
        openedAuthorityVersion=b['priorVersion']+1, openingPrefix=b['priorPrefix'],
        admittedPolicyBundleDigest=q['configurationHash'])

def base_for(case):
    q = rel.CONTEXT.request(); w = r.env.initial_world(q, rel.CONTEXT)
    b = dict(contractVersion=1, profile='isolated-first-opening', creationRequest=q, firstActingSide=case['side'],
        position=position('entryFromPositionId'), priorVersion=case['priorVersion'],
        priorPrefix=sha(encode(dict(isolatedPrefix=case['name']))), world=w,
        randomState=dict(q['randomState'], nextByteCursor=case['cursor']), members=[])
    for e in w['elements']:
        if world.SIDES[e['elementId']] != case['side']: continue
        e['reserveStatus'] = case['reserve']; e['operationalState']['capabilityPointsExpended'] = world.cp(case['spent'])
        h = rel.empty_history(authority(b))
        if case['reserve'] == 'I': h['designationReceiptId'] = 'probe.designation.'+case['side']
        b['members'].append(dict(unit=world.unit(e,w['creationBinding']), status=e['reserveStatus'],
            baseCpa=10, spentCp=copy.deepcopy(e['operationalState']['capabilityPointsExpended']), history=h))
    return b

def command(b):
    return dict(command=dict(contractVersion=2,kind='complete-reserve-designation',baseHash=sha(raw(b,'OpeningBase')),
        expectedPriorVersion=b['priorVersion'],expectedPositionId=b['position']['positionId']),actor=b['firstActingSide'])

def validate_base(b):
    typed(b, 'OpeningBase')
    require(b['contractVersion'] == 1 and b['profile'] == 'isolated-first-opening', 3)
    q = rel.CONTEXT.request()
    require(b['creationRequest'] == q, 4)
    require(b['position'] == position('entryFromPositionId'), 4)
    require(10 <= b['priorVersion'] < 2**63-1, 7)
    rng = b['randomState']
    require({k: v for k, v in rng.items() if k != 'nextByteCursor'} ==
        {k: v for k, v in q['randomState'].items() if k != 'nextByteCursor'}, 4)
    expected = r.env.initial_world(q, rel.CONTEXT)
    own = [e for e in expected['elements'] if world.SIDES[e['elementId']] == b['firstActingSide']]
    require(len(b['members']) == len(own) == 1, 4)
    for e, m in zip(own, b['members']):
        require(m['unit'] == world.unit(e, expected['creationBinding']) and m['baseCpa'] == 10, 4)
        require(m['status'] in ('none', 'I'), 3)
        require(m['spentCp']['denominator'] == 1 and 0 <= m['spentCp']['numerator'] <= 10, 3)
        history = rel.empty_history(authority(b))
        if m['status'] == 'I':
            require(m['history']['designationReceiptId'] is not None, 4)
            history['designationReceiptId'] = m['history']['designationReceiptId']
        require(m['history'] == history, 4)
        e['reserveStatus'] = m['status']
        e['operationalState']['capabilityPointsExpended'] = copy.deepcopy(m['spentCp'])
    # This is a closed synthetic World profile, not a general World7 validator.
    require(b['world'] == expected, 3)
    return b

def initial(b):
    validate_base(b)
    return dict(contractVersion=1, baseHash=sha(raw(b, 'OpeningBase')), stateVersion=b['priorVersion'],
        prefix=b['priorPrefix'], position=copy.deepcopy(b['position']), cycle=None,
        world=copy.deepcopy(b['world']), randomState=copy.deepcopy(b['randomState']),
        members=copy.deepcopy(b['members']), receipts=[])

def generate(b, inp):
    typed(inp, 'OpeningInput')
    require(inp['actor'] == b['firstActingSide'], 5)
    require(inp == command(b), 4)
    c = authority(b)
    event = dict(contractVersion=2, eventType='reserve-designation-completed', campaignId=c['campaignId'],
        rulesetHash=c['rulesetHash'], configurationHash=c['admittedPolicyBundleDigest'],
        baseHash=sha(raw(b,'OpeningBase')), priorVersion=b['priorVersion'], stateVersion=c['openedAuthorityVersion'],
        priorPrefix=b['priorPrefix'], fromPositionId=b['position']['positionId'],
        sources=copy.deepcopy(INVENTORY['completionSources']), input=copy.deepcopy(inp), sequencePosition=position('movementPositionId'),
        cycle=c, cycleId=sha(steps.seq.identity(c, 'Authority', b['firstActingSide'])), receiptId='pending')
    unsigned = {k: v for k, v in canonical(event,'OpeningEvent').items() if k != 'receiptId'}
    event['receiptId'] = 'rc.'+steps.digest(INVENTORY['domains']['receipt'], encode(unsigned))
    data = raw(event, 'OpeningEvent')
    state = initial(b)
    state.update(stateVersion=event['stateVersion'], prefix=steps.seq.prefix_event(b['priorPrefix'], data),
        position=copy.deepcopy(event['sequencePosition']), cycle=c,
        receipts=[dict(commandHash=sha(raw(inp,'OpeningInput')),eventHash=sha(data),receiptId=event['receiptId'],
            actor=inp['actor'],stateVersion=event['stateVersion'])])
    return state, data

def replay(b, events):
    state = initial(b)
    require(type(events) is list and len(events) <= 1, 6)
    for data in events:
        event = parse(data, 'OpeningEvent')
        state, expected = generate(b, event['input'])
        require(data == expected, 6)
    return state

def apply(b, events, inp):
    state = replay(b, events)
    typed(inp, 'OpeningInput')
    require(inp['actor'] == b['firstActingSide'], 5)
    if events:
        # Actor binding precedes retry; exact accepted input precedes stale-version rejection.
        require(inp == parse(events[0], 'OpeningEvent')['input'], 6)
        return state, events[0], True
    after, data = generate(b, inp)
    return after, data, False

def verify_inventory(f):
    repo = ROOT.parent.parent
    for path, digest in f['sourceHashes'].items():
        require(sha((repo/path).read_bytes()) == digest, 9)
    def source(name): return (repo/'src/Cna.Core/Campaigns'/name).read_text()
    rows = INVENTORY['inheritedEvents']
    require(len(rows) == 20 and len({x['eventType'] for x in rows}) == 20, 9)
    require(sha(encode(rows)) == f['inventoryHash'], 9)
    require(sha(encode(INVENTORY['owners'])) == f['ownersHash'], 9)
    preamble = [x for x in rows if x['family'] in ('preamble','weather','reserve')]
    require({x['currentType'] for x in preamble} == set(re.findall(r'(\w+) e => new ',source('CampaignV11PreambleCodec.cs'))), 9)
    dispatch_body = source('CampaignEventSerializer.cs').split('CampaignEvent campaignEvent = eventType switch',1)[1].split('};',1)[0]
    parsed = set(re.findall(r'"([a-z-]+)" => Parse',dispatch_body))
    require({x['eventType'] for x in preamble} == parsed-{'campaign-created','element-moved','movement-segment-completed'}, 9)
    successors = [x for x in rows if x['family'] in ('movement','reaction','breakdown')]
    require({(x['eventType'],x['currentVersion']) for x in successors} ==
        {(name,int(v)) for name,v in re.findall(r'\("([a-z-]+)", (\d+)\)',source('CampaignBreakdownEventSerializer.cs'))}, 9)
    dispatch = set(re.findall(r'"([a-z]+-[a-z-]+)"',source('CampaignCurrentEventRuntime.cs')))
    require(dispatch == {x['eventType'] for x in successors}|{'campaign-created','invalid-breakdown-authority'}, 9)
    for name, version in [('InitiativeDetermined.cs',2),('OpeningPreambleEvents.cs',1),
            ('WeatherDetermined.cs',1),('StageEntryEvents.cs',1),('CampaignReserveEvents.cs',1)]:
        require(f': base({version}, campaignId, stateVersion)' in source(name), 9)
    require('CurrentContractVersion = 10' in source('CampaignCreationV10.cs'), 9)
    require('version is not (3 or 4)' in source('CampaignV11PreambleCodec.cs'), 9)
    done = {'007'}
    for owner in INVENTORY['owners']:
        require(owner['id'] not in done and set(owner['after']) <= done and owner['maxPrimaryFiles'] <= 5, 9)
        done.add(owner['id'])
    require('019A' in next(o for o in INVENTORY['owners'] if o['id']=='008H')['after'], 9)

def rejected(fn):
    try: fn()
    except Invalid: return
    raise AssertionError('expected CMB-INH rejection')

def leaves(value, path=()):
    if type(value) is dict:
        for k,v in value.items(): yield from leaves(v,path+(k,))
    elif type(value) is list:
        for k,v in enumerate(value): yield from leaves(v,path+(k,))
    else: yield path,value

def changed(value, path, replacement):
    out=copy.deepcopy(value); node=out
    for key in path[:-1]: node=node[key]
    node[path[-1]]=replacement
    return out

def trace(case):
    b=base_for(case); before=initial(b); inp=command(b)
    after,data,duplicate=apply(b,[],inp)
    assert not duplicate
    # Literal semantic expectations are checked before the retained hashes.
    assert after['stateVersion']==case['expectedVersion'] and after['cycle']['ordinal']==1
    assert after['cycle']['openedAuthorityVersion']==case['expectedVersion']
    assert after['position']['positionId']==case['expectedPosition']
    assert after['cycle']['openingPrefix']==b['priorPrefix'] and after['prefix']!=b['priorPrefix']
    assert after['cycle']['actingSide']==case['side'] and after['cycle']['playerPhaseSlot']=='first-acting-side'
    assert after['world']==b['world'] and after['randomState']==b['randomState'] and after['members']==b['members']
    assert after['randomState']['nextByteCursor']==case['cursor']
    assert all(e['ammunition']['points']==10 for e in after['world']['elements'])
    assert len(after['receipts'])==1 and before['cycle'] is None
    assert replay(b,[])==before and replay(b,[data])==after
    assert apply(b,[data],inp)==(after,data,True)
    assert b==base_for(case)  # No input mutation on success or retry.
    return b,inp,before,after,data

def main():
    f=json.loads(FIXTURE.read_text()); verify_inventory(f)
    assert {(c['side'],c['reserve']) for c in f['cases']} == {(s,r) for s in ('axis','commonwealth') for r in ('none','I')}
    assert len(f['cases'])==4
    mutations=raw_rejects=boundaries=0
    for case in f['cases']:
        b,inp,before,after,data=trace(case)
        artifacts={'base':raw(b,'OpeningBase'),'input':raw(inp,'OpeningInput'),'initial':raw(before,'OpeningState'),
            'event':data,'final':raw(after,'OpeningState')}
        assert {k:dict(bytes=len(v),sha256=sha(v)) for k,v in artifacts.items()}==case['goldens']
        event=parse(data,'OpeningEvent')
        for path,old in leaves(event):
            replacement = old+1 if type(old) is int else ('axis' if old=='commonwealth' else 'wrong')
            forged=changed(event,path,replacement)
            rejected(lambda:replay(b,[encode(forged)])); mutations+=1
        for bad in (b'\xef\xbb\xbf'+data,data+b' ',b' '+data,data.replace(b'"contractVersion":2',b'"contractVersion":2,"contractVersion":2',1),
                data.replace(b'"contractVersion":2',b'"contractVersion":2.0',1),data.replace(b'"contractVersion":2',b'"contractVersion":true',1),
                data[:-1]+b',"unknown":null}',encode(dict(reversed(list(event.items()))))):
            rejected(lambda:replay(b,[bad])); raw_rejects+=1
        for path,replacement in [
            (('contractVersion',),2),(('profile',),'runtime'),(('position','contractVersion'),4),
            (('position','activeSide'),case['side']),(('position','operationStage'),2),
            (('creationRequest','configurationHash'),sha(b'wrong')), (('creationRequest','rulesetHash'),'0'*64),
            (('priorVersion',),9),(('priorVersion',),2**63-1),(('members',),[]),
            (('members',0,'status'),'II'),(('members',0,'unit','originalSide'),'wrong'),
            (('members',0,'history','scope','playerPhaseSlot'),'second-acting-side'),
            (('members',0,'history','conversionReceiptId'),'forged'),
            (('world','elements',0,'ammunition','points'),11),(('randomState','algorithmId'),'wrong')]:
            bad=changed(b,path,replacement); rejected(lambda:initial(bad)); boundaries+=1
        other='commonwealth' if case['side']=='axis' else 'axis'
        for suffix in ([],[data]):
            rejected(lambda:apply(b,suffix,dict(inp,actor=other))); boundaries+=1
            bad=changed(inp,('command','expectedPriorVersion'),b['priorVersion']+1)
            rejected(lambda:apply(b,suffix,bad)); boundaries+=1
        rejected(lambda:replay(b,[data,data])); boundaries+=1
        # A different trusted prefix is a valid isolated fork, but cannot import this event.
        fork=dict(b,priorPrefix=sha(b'another trusted predecessor'))
        fork_state,fork_event,_=apply(fork,[],command(fork))
        assert fork_state['cycle']!=after['cycle'] and parse(fork_event,'OpeningEvent')['cycleId']!=event['cycleId']
        rejected(lambda:replay(fork,[data])); boundaries+=1
        assert b==base_for(case)
    print(f'PASS inherited successors: 20 source-bound declarations, 9 bounded owners, {len(f["cases"])} opening traces, '
        f'{len(f["cases"])*2} replay cuts, {mutations} event mutations, {raw_rejects} raw rejections, {boundaries} boundary checks')

if __name__=='__main__': main()
