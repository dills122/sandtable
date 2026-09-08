#!/usr/bin/env python3
"""D2c.2b prospective Weather contract; no runtime activation or golden regeneration."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('weather_preamble', ROOT/'verify-combat-opening-preamble-v1.py')
pre = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pre)
INVENTORY = json.loads((ROOT/'combat-weather-v1.schema.json').read_text())
FIXTURE = ROOT/'fixtures/combat-weather-v1.json'
SCHEMA = pre.SCHEMA | {k: [tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}
encode, sha = pre.encode, pre.sha
MAX = 2**64-1

class Invalid(ValueError):
    def __init__(self, code):
        self.code = f'CMB-WTH-{code:03}'
        super().__init__(self.code)

def require(ok, code):
    if not ok: raise Invalid(code)

def typed(v, kind, depth=0):
    require(depth<=32,1)
    if kind.endswith('?'):
        if v is not None: typed(v,kind[:-1],depth)
    elif kind in SCHEMA:
        require(type(v) is dict and set(v)=={k for k,_ in SCHEMA[kind]},1)
        for k,t in SCHEMA[kind]: typed(v[k],t,depth+1)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v)<=512,1)
        for item in v: typed(item,kind[:-2],depth+1)
    else:
        try: pre.typed(v,kind,depth)
        except pre.Invalid as error: raise Invalid(1) from error

def canonical(v,kind):
    if kind.endswith('?'): return None if v is None else canonical(v,kind[:-1])
    if kind in SCHEMA: return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):
        child=kind[:-2]; out=[canonical(x,child) for x in v]
        if child in pre.env.KEYS: out.sort(key=lambda x:tuple(x[k] for k in pre.env.KEYS[child]))
        elif kind=='id[]': out.sort()
        return out
    return v

def raw(v,kind):
    typed(v,kind); data=encode(canonical(v,kind)); require(len(data)<=1048576,1)
    return data

def parse(data,kind):
    require(type(data) is bytes and 0<len(data)<=1048576,1)
    def pairs(items):
        out={}
        for k,v in items: require(k not in out,1); out[k]=v
        return out
    try: value=json.loads(data.decode('utf8'),object_pairs_hook=pairs,
        parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as error: raise Invalid(1) from error
    require(raw(value,kind)==data,8)
    return value

def weather_rules():
    row=INVENTORY['weatherArtifact']
    data=(ROOT.parent.parent/row['path']).read_bytes().removesuffix(b'\n')
    value=json.loads(data)
    require(encode(value)==data and sha(data)==row['contentHash'],9)
    artifacts=pre.env.expected_manifest()['artifacts']
    require([a for a in artifacts if a['artifactId']==row['artifactId']]==[
        dict(artifactId=row['artifactId'],contentHash=sha(data),sources=value['sources'])],9)
    return value

RULES = weather_rules()

def classify(turn,first,second):
    require(type(turn) is int and 1<=turn<=110,2)
    require(type(first) is int and type(second) is int and 1<=first<=6 and 1<=second<=6,2)
    tables=[t for t in RULES['seasons'] if any(r['first']<=turn<=r['last'] for r in t['gameTurnRanges'])]
    require(len(tables)==1,9)
    outcomes=[r['kind'] for r in tables[0]['outcomes'] if r['firstD66']<=first*10+second<=r['lastD66']]
    require(len(outcomes)==1,9)
    return tables[0]['season'],outcomes[0]

def roll(rng):
    typed(rng,'Random')
    require(rng['contractVersion']==1 and rng['algorithmId']=='sandtable.sha256-counter.v1',3)
    state=copy.deepcopy(rng)
    while True:
        cursor=state['nextByteCursor']; require(cursor<MAX,2)
        data=b'sandtable.random.v1\0'+state['seed'].to_bytes(8,'big')+(cursor//32).to_bytes(8,'big')
        value=hashlib.sha256(data).digest()[cursor%32]
        state['nextByteCursor']=cursor+1
        if value<252: return value%6+1,state

def resolve(rng):
    first,state=roll(rng); second,state=roll(state)
    season,kind=classify(1,first,second)
    location=None; areas=[]; scope='global' if kind=='hot' else 'none'
    if kind in ('sandstorm','rainstorm'):
        location,state=roll(state); scope='listed-areas'
        areas=copy.deepcopy(next(r['areas'] for r in RULES['foulWeatherLocations'] if r['die']==location))
    return dict(season=season,firstDie=first,secondDie=second,kind=kind,scope=scope,
        locationDie=location,affectedAreas=areas),state

def references(kind):
    base=[dict(sourceId='cna-1979.1.ruling.weather-season-boundary',locator='selected-behavior'),
        dict(sourceId='sandtable-rules-lab',locator='weather.no-immediate-effect-subjects.v1'),
        dict(sourceId='spi-1979-common-charts',locator='29.61'),
        dict(sourceId='spi-1979-errata',locator='29.1'),dict(sourceId='spi-1979-errata',locator='29.61')]+pre.land('29.0','29.1')
    extra={'normal':pre.land('29.2'),'hot':pre.land('29.31','29.34'),
        'sandstorm':pre.land('29.41','29.47','38.5'),'rainstorm':pre.land('29.53')}[kind]
    if kind in ('sandstorm','rainstorm'): extra+=[dict(sourceId='spi-1979-common-charts',locator='29.7')]
    return pre.sources(base,extra)

def initial(request,created,preamble):
    require(type(preamble) is list and len(preamble)==4,6)
    try: state=pre.replay(request,created,preamble)
    except pre.Invalid as error: raise Invalid(4) from error
    require(state['stateVersion']==5 and state['sequencePosition']==pre.position(4),4)
    require(pre.CONTEXT.setup['weather']==dict(contractVersion=1,kind='no-immediate-weather-effect-subjects',
        sources=[dict(sourceId='sandtable-rules-lab',locator='weather.no-immediate-effect-subjects.v1')]),3)
    require(state['initiativeHolder']=='axis' and len(state['operationStageOrders'])==1
        and state['operationStageOrders'][0]['gameTurn']==state['operationStageOrders'][0]['operationStage']==1,4)
    state['operationStageWeather']=[]
    return canonical(state,'WeatherState')

def command(state):
    return dict(command=dict(contractVersion=2,kind='resolve-weather',creationBinding=state['creationBinding'],
        creationEventHash=state['creationEventHash'],expectedPriorVersion=state['stateVersion'],
        expectedPositionId=state['sequencePosition']['positionId']),actor='system')

def authorize(inp):
    typed(inp,'WeatherInput')
    require(inp['command']['contractVersion']==2 and inp['command']['kind']=='resolve-weather',3)
    require(inp['actor']=='system',5)

def _emit(state,inp):
    # State comes only from complete accepted predecessor replay, never from caller cache.
    authorize(inp)
    require(state['stateVersion']==5 and state['sequencePosition']==pre.position(4)
        and not state['operationStageWeather'],6)
    require(inp==command(state),4)
    outcome,rng=resolve(state['randomState'])
    weather=dict(contractVersion=1,gameTurn=1,operationStage=1,determiningSide=state['initiativeHolder'],
        **outcome,fuelWaterReductionSubjectCount=0,restoredWellCount=0,damagedGroundedAircraftCount=0)
    event=dict(contractVersion=2,eventType='weather-determined',campaignId=state['campaignId'],
        rulesetHash=state['rulesetHash'],configurationHash=state['configurationHash'],creationBinding=state['creationBinding'],
        creationEventHash=state['creationEventHash'],priorVersion=5,stateVersion=6,priorPrefix=state['prefix'],
        fromPositionId=state['sequencePosition']['positionId'],input=copy.deepcopy(inp),
        **{k:v for k,v in weather.items() if k!='contractVersion'},randomAlgorithmId=rng['algorithmId'],
        randomCursorBefore=state['randomState']['nextByteCursor'],randomCursorAfter=rng['nextByteCursor'],
        sequencePosition=pre.position(5),sources=references(outcome['kind']),receiptId='pending')
    unsigned={k:v for k,v in canonical(event,'WeatherEvent').items() if k!='receiptId'}
    event['receiptId']='wth.'+hashlib.sha256(INVENTORY['domains']['receipt'].encode()+b'\0'+encode(unsigned)).hexdigest()
    data=raw(event,'WeatherEvent'); after=copy.deepcopy(state)
    after.update(stateVersion=6,prefix=pre.env.sequence.prefix_event(state['prefix'],data),
        sequencePosition=copy.deepcopy(event['sequencePosition']),randomState=rng,operationStageWeather=[weather])
    after['receipts'].append(dict(commandHash=sha(raw(inp,'WeatherInput')),eventHash=sha(data),
        receiptId=event['receiptId'],actor=inp['actor'],stateVersion=6))
    return after,data

def replay(request,created,preamble,events):
    state=initial(request,created,preamble)
    require(type(events) is list and len(events)<=1,6)
    for data in events:
        value=parse(data,'WeatherEvent'); state,expected=_emit(state,value['input'])
        require(data==expected,6)
    return state

def apply(request,created,preamble,events,inp):
    state=replay(request,created,preamble,events); authorize(inp)
    if events:
        accepted=parse(events[0],'WeatherEvent')['input']
        require(raw(inp,'WeatherInput')==raw(accepted,'WeatherInput'),6)
        return state,events[0],True
    after,data=_emit(state,inp)
    return after,data,False

def read_state(data,request,created,preamble,events):
    value=parse(data,'WeatherState'); expected=replay(request,created,preamble,events)
    require(data==raw(expected,'WeatherState'),6)
    return value

def rejected(fn):
    try: fn()
    except Invalid: return
    raise AssertionError('expected CMB-WTH rejection')

def trace(case,choice):
    q=pre.CONTEXT.request(seed=case['seed']); created=pre.created_for(q)
    state=pre.initial(q,created); opening=[]
    for i in range(4):
        state,data,_=pre.apply(q,created,opening,pre.command(state,choice if i==3 else None)); opening.append(data)
    before=initial(q,created,opening); inp=command(before)
    after,data,duplicate=apply(q,created,opening,[],inp); event=parse(data,'WeatherEvent')
    expected=case['expected']
    assert not duplicate and before['stateVersion']==5 and after['stateVersion']==6
    for k in ('firstDie','secondDie','kind','scope','locationDie','affectedAreas'): assert event[k]==expected[k],(case['seed'],k)
    assert event['season']=='fall' and event['gameTurn']==event['operationStage']==1
    assert event['randomCursorBefore']==0 and event['randomCursorAfter']==expected['cursor']
    assert after['randomState']==dict(before['randomState'],nextByteCursor=expected['cursor'])
    assert event['determiningSide']==before['initiativeHolder']==after['initiativeHolder']=='axis'
    assert before['operationStageOrders']==after['operationStageOrders']==[dict(contractVersion=1,gameTurn=1,
        operationStage=1,firstSide='axis' if choice=='act-first' else 'commonwealth',
        secondSide='commonwealth' if choice=='act-first' else 'axis')]
    assert event['sequencePosition']==after['sequencePosition']==pre.position(5)
    assert after['sequencePosition']['positionId']=='land.position.operation-1.organization'
    assert after['sequencePosition']['phaseId']=='land.phase.organization' and after['sequencePosition']['activeSide'] is None
    assert event['contractVersion']==2 and event['eventType']=='weather-determined'
    assert all(event[k]==0 for k in ('fuelWaterReductionSubjectCount','restoredWellCount','damagedGroundedAircraftCount'))
    assert after['world']==before['world']==pre.initial(q,created)['world']
    assert after['prefix']==pre.env.sequence.prefix_event(before['prefix'],data) and event['priorPrefix']==before['prefix']
    assert before['prefix']==pre.replay(q,created,opening)['prefix']
    assert after['receipts'][:-1]==before['receipts'] and len(after['receipts'])==5
    value={k:event[k] for k,_ in SCHEMA['WeatherValue']};value['contractVersion']=1
    assert after['operationStageWeather']==[value]
    return q,created,opening,inp,data,before,after

def goldens(result):
    _,created,opening,inp,data,before,after=result
    values=[('creation',created)]+[(f'preamble-{i+1}',e) for i,e in enumerate(opening)]+[
        ('input',raw(inp,'WeatherInput')),('weather',data),('before',raw(before,'WeatherState')),('after',raw(after,'WeatherState'))]
    return {k:dict(bytes=len(v),sha256=sha(v)) for k,v in values}

def verify_sources(f):
    for name,digest in f['sourceHashes'].items(): require(sha((ROOT.parent.parent/name).read_bytes())==digest,9)
    require(weather_rules()==RULES,9)
    declarations=json.loads((ROOT/'combat-inherited-successors-v1.schema.json').read_text())['inheritedEvents']
    row=next(x for x in declarations if x['eventType']=='weather-determined')
    require(row['currentVersion']==1 and row['successorVersion']==INVENTORY['event']['version']==2,9)
    require(INVENTORY['command']==dict(kind='resolve-weather',currentVersion=1,version=2,actor='system'),9)
    require('version is not (3 or 4)' in (ROOT.parent.parent/'src/Cna.Core/Campaigns/CampaignV11PreambleCodec.cs').read_text(),9)

def main():
    f=json.loads(FIXTURE.read_text()); verify_sources(f)
    kinds=dict(N='normal',H='hot',S='sandstorm',R='rainstorm')
    seasons=['fall']*12+['winter']*12+['spring']*12+['summer']*12
    seasons=seasons*2+['fall']*12+['winter']*2
    coordinates=0
    for turn,season in enumerate(seasons,1):
        labels=f['tableKinds'][season].replace(' ',''); assert len(labels)==36
        for index,(a,b) in enumerate((a,b) for a in range(1,7) for b in range(1,7)):
            assert classify(turn,a,b)==(season,kinds[labels[index]]); coordinates+=1
    assert [r['areas'] for r in RULES['foulWeatherLocations']]==f['locationAreas']
    expected_foul={(kind,die) for kind in ('sandstorm','rainstorm') for die in range(1,7)}
    assert {(c['expected']['kind'],c['expected']['locationDie']) for c in f['cases'] if c['expected']['locationDie']}==expected_foul
    assert {0,1,2,3,12345,MAX}<={c['seed'] for c in f['cases']}
    assert any(c['expected']['rejectedBytes']>0 for c in f['cases'])
    cuts=mutations=raw_rejects=boundaries=0
    for case in f['cases']:
        expected=case['expected']; consumed=bytes.fromhex(expected['consumedHex'])
        assert len(consumed)==expected['cursor'] and sum(x>=252 for x in consumed)==expected['rejectedBytes']
        block=hashlib.sha256(b'sandtable.random.v1\0'+case['seed'].to_bytes(8,'big')+bytes(8)).digest()
        assert block[:len(consumed)]==consumed
        accepted=[x%6+1 for x in consumed if x<252]
        assert accepted==[expected['firstDie'],expected['secondDie']]+([] if expected['locationDie'] is None else [expected['locationDie']])
        for choice in ('act-first','act-last'):
            result=trace(case,choice);q,created,opening,inp,data,before,after=result
            assert goldens(result)==case['goldens'][choice]
            retained=copy.deepcopy(result)
            for events,state in (([],before),([data],after)):
                encoded=raw(state,'WeatherState')
                assert replay(q,created,opening,events)==state and read_state(encoded,q,created,opening,events)==state;cuts+=1
                for path,old in pre.leaves(state):
                    bad=pre.changed(state,path,old+1 if type(old) is int else 'wrong')
                    rejected(lambda:read_state(encode(bad),q,created,opening,events));mutations+=1
            assert apply(q,created,opening,[data],inp)==(after,data,True);boundaries+=1
            event=parse(data,'WeatherEvent')
            for path,old in pre.leaves(event):
                bad=pre.changed(event,path,old+1 if type(old) is int else 'wrong')
                rejected(lambda:replay(q,created,opening,[encode(bad)]));mutations+=1
            # A self-consistent receipt cannot authenticate invented Weather evidence.
            forged=copy.deepcopy(event);forged['firstDie']=event['firstDie']%6+1
            unsigned={k:v for k,v in canonical(forged,'WeatherEvent').items() if k!='receiptId'}
            forged['receiptId']='wth.'+hashlib.sha256(INVENTORY['domains']['receipt'].encode()+b'\0'+encode(unsigned)).hexdigest()
            rejected(lambda:replay(q,created,opening,[raw(forged,'WeatherEvent')]));boundaries+=1
            for field,values in [('sources',[event['sources'][:-1],event['sources']+[event['sources'][0]],
                    list(reversed(event['sources']))]),('affectedAreas',[['a']*513,['a','a'],['z']])]:
                for value in values:
                    rejected(lambda:replay(q,created,opening,[encode(dict(event,**{field:value}))]));boundaries+=1
            for bad in (dict(after,operationStageWeather=[]),dict(after,operationStageWeather=after['operationStageWeather']*2),
                    dict(after,receipts=after['receipts'][:-1]),dict(after,receipts=list(reversed(after['receipts'])))):
                rejected(lambda:read_state(encode(canonical(bad,'WeatherState')),q,created,opening,[data]));boundaries+=1
            for kind,value in (('WeatherEvent',event),('WeatherState',after),('WeatherInput',inp)):
                encoded=raw(value,kind)
                for bad in (b'\xef\xbb\xbf'+encoded,encoded+b' ',encoded[:-1]+b',"extra":null}',
                        encode(dict(reversed(list(value.items())))),b'{"contractVersion":true,'+encoded[1:],
                        encoded.replace(b'"contractVersion":1',b'"contractVersion":1.0',1) if kind=='WeatherState'
                        else encoded.replace(b'"contractVersion":2',b'"contractVersion":2.0',1),
                        b'\xff',b'{"contractVersion":NaN}',b'['*40+b'0'+b']'*40):
                    rejected(lambda:parse(bad,kind));raw_rejects+=1
            for path,value in [(('command','expectedPriorVersion'),4),(('command','expectedPriorVersion'),6),
                    (('command','contractVersion'),1),(('command','contractVersion'),True),
                    (('command','kind'),'resolve-initiative'),(('command','expectedPositionId'),after['sequencePosition']['positionId']),
                    (('command','creationBinding'),'creation.forged'),(('command','creationEventHash'),sha(b'wrong')),
                    (('actor',),'axis'),(('actor',),'commonwealth')]:
                bad=pre.changed(inp,path,value)
                for events in ([],[data]): rejected(lambda:apply(q,created,opening,events,bad));boundaries+=1
            for suffix in (opening[:-1],opening[1:],list(reversed(opening)),opening+[opening[-1]]):
                rejected(lambda:replay(q,created,suffix,[data]));boundaries+=1
            for events in ([data,data],[opening[-1]]): rejected(lambda:replay(q,created,opening,events));boundaries+=1
            for bad_created in (b'',created+b' ',created.replace(b'"contractVersion":11',b'"contractVersion":10',1)):
                rejected(lambda:replay(q,bad_created,opening,[data]));boundaries+=1
            fork=pre.CONTEXT.request(campaign='weather.other',seed=case['seed']);fork_created=pre.created_for(fork)
            rejected(lambda:replay(fork,fork_created,opening,[data]));boundaries+=1
            for path,value in [(('rulesetHash',),'0'*64),(('configurationHash',),sha(b'wrong')),
                    (('randomState','seed'),(case['seed']+1)%(MAX+1)),(('randomState','nextByteCursor'),1)]:
                rejected(lambda:replay(pre.changed(q,path,value),created,opening,[data]));boundaries+=1
            opposite=trace(case,'act-last' if choice=='act-first' else 'act-first')
            assert json.loads(opposite[4])['firstDie']==event['firstDie'] and opposite[-1]['prefix']!=after['prefix']
            rejected(lambda:replay(q,created,opposite[2],[data]));boundaries+=1
            rejected(lambda:read_state(raw(after,'WeatherState'),q,created,opposite[2],[opposite[4]]));boundaries+=1
            assert result==retained  # Every accepted/rejected call leaves caller-owned data unchanged.
    for turn in (0,111,True,-1): rejected(lambda:classify(turn,1,1));boundaries+=1
    for first,second in ((0,1),(1,7),(True,1),(1,2.0)): rejected(lambda:classify(1,first,second));boundaries+=1
    rng=copy.deepcopy(pre.CONTEXT.request()['randomState'])
    for path,value in [(('contractVersion',),2),(('algorithmId',),'other'),(('seed',),-1),(('seed',),MAX+1),
            (('nextByteCursor',),MAX),(('nextByteCursor',),MAX+1),(('nextByteCursor',),True)]:
        rejected(lambda:roll(pre.changed(rng,path,value)));boundaries+=1
    # Isolated byte-cursor boundary probes; these are not admitted creation-rooted states.
    for cursor in (31,32,63,MAX-1):
        probe=dict(rng,nextByteCursor=cursor);byte=hashlib.sha256(b'sandtable.random.v1\0'+bytes(8)+(cursor//32).to_bytes(8,'big')).digest()[cursor%32]
        if byte<252:
            die,after=roll(probe);assert die==byte%6+1 and after['nextByteCursor']==cursor+1
        else:
            if cursor==MAX-1: rejected(lambda:roll(probe))
            else: assert roll(probe)[1]['nextByteCursor']>cursor+1
        assert probe==dict(rng,nextByteCursor=cursor);boundaries+=1
    for kind in ('WeatherInput','WeatherEvent','WeatherState'):
        rejected(lambda:parse(b' '*1048577,kind));raw_rejects+=1
    print(f'PASS Weather: {len(f["cases"])*2} creation-rooted traces, {cuts} replay/state cuts, {mutations} leaf mutations, '
        f'{raw_rejects} raw rejections, {boundaries} boundary/retry checks, {coordinates} Rules coordinates. '
        'Organization-entry contract only; no runtime/Snapshot12 admission.')

if __name__=='__main__': main()
