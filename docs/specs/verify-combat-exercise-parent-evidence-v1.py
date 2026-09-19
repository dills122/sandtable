#!/usr/bin/env python3
"""Private B3 parent aggregation over authenticated checkpoint-scoped B2 children."""
from __future__ import annotations
import copy,hashlib,importlib.util,json,sys,time
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent
FIXTURE=ROOT/'fixtures/combat-exercise-parent-evidence-v1.json'
INVENTORY=json.loads((ROOT/'combat-exercise-parent-evidence-v1.schema.json').read_bytes())
SCHEMA={k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
if 'combat_parent_child' in sys.modules:c=sys.modules['combat_parent_child']
else:
    spec=importlib.util.spec_from_file_location('combat_parent_child',ROOT/'verify-combat-exercise-child-evidence-v1.py')
    c=importlib.util.module_from_spec(spec);sys.modules[spec.name]=c;spec.loader.exec_module(c)
b=c.b
class Invalid(ValueError):pass
def require(value):
    if not value:raise Invalid('CMB-PARENT-REJECTED')
def typed(value,kind,depth=0):
    require(depth<=52)
    if kind.endswith('?'):
        if value is not None:typed(value,kind[:-1],depth+1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value)<=128)
        for item in value:typed(item,kind[:-2],depth+1)
    elif kind in SCHEMA:
        require(type(value) is dict and set(value)=={k for k,_ in SCHEMA[kind]})
        for key,t in SCHEMA[kind]:typed(value[key],t,depth+1)
    elif kind in INVENTORY['enums']:require(type(value) is str and value in INVENTORY['enums'][kind])
    elif kind=='delta':require(type(value) is int and -64<=value<=64)
    else:c.typed(value,kind,depth)
def canonical(value,kind):
    if kind.endswith('?'):return None if value is None else canonical(value,kind[:-1])
    if kind.endswith('[]'):return [canonical(x,kind[:-2]) for x in value]
    if kind in SCHEMA:return {k:canonical(value[k],t) for k,t in SCHEMA[kind]}
    return c.canonical(value,kind)
def raw(value,kind):
    typed(value,kind);data=c.encode(canonical(value,kind));require(len(data)<=8388608);return data
def parse(data,kind):
    require(type(data) is bytes and len(data)<=8388608)
    def pairs(items):
        value={}
        for key,item in items:require(key not in value);value[key]=item
        return value
    value=json.loads(data.decode('ascii'),object_pairs_hook=pairs,parse_constant=lambda _:require(False))
    require(raw(value,kind)==data);return value
def identity(domain,value,kind):return c.sha(domain.encode('ascii')+b'\0'+raw(value,kind))
def pins():
    require(all(c.sha((ROOT/p).read_bytes())==h for p,h in INVENTORY['sourcePins'].items()));c.pins()
def fixture_bytes(value):return (json.dumps(value,ensure_ascii=True,indent=2)+'\n').encode('ascii')
def rejected(fn):
    try:fn()
    except (ValueError,KeyError,TypeError,StopIteration,OverflowError):return
    raise AssertionError('invalid parent evidence accepted')

def divergence(left,right):
    for stream in (left,right):
        raw(stream,'Action[]');require(len(stream)<=64 and all(x['ordinal']==i for i,x in enumerate(stream)))
    for i in range(max(len(left),len(right))):
        a=left[i] if i<len(left) else None;z=right[i] if i<len(right) else None
        if a!=z:return copy.deepcopy(dict(ordinal=i,baseline=a,candidate=z))
    return None

_SOURCES=None
_VALIDATED={}
def source_for(source_id):
    global _SOURCES
    pins()
    if _SOURCES is None:_SOURCES={x['sourceId']:x for x in b.source_cases()}
    require(source_id in _SOURCES);source=copy.deepcopy(_SOURCES[source_id]);b.authenticate_source(source);return source
def materialize(plan):
    raw(plan,'ChildPlan');require(plan['maximumAcceptedTransitions']<=64)
    source=source_for(plan['sourceId']);manifest=c.manifest_for(source,plan['childId'])
    manifest['request']['maximumAcceptedTransitions']=plan['maximumAcceptedTransitions']
    manifest['fault']=copy.deepcopy(plan['fault']);manifest['expectedFailure']=plan['expectedFailure']
    c.raw(manifest,'Manifest');return source,manifest
def expected(plan):
    _,manifest=materialize(plan);return dict(plan=copy.deepcopy(plan),manifestCanonicalJson=c.raw(manifest,'Manifest').decode('ascii'))
def admit_manifest(manifest):
    pins();raw(manifest,'ParentManifest');children=manifest['children'];pairs=manifest['pairs']
    require(1<=len(children)<=32 and len(pairs)<=16)
    ids=[x['plan']['childId'] for x in children];require(len(ids)==len(set(ids)))
    require(len({p['pairId'] for p in pairs})==len(pairs))
    for pair in pairs:require(pair['baselineChildId'] in ids and pair['candidateChildId'] in ids and pair['baselineChildId']!=pair['candidateChildId'])
    for slot in children:
        require(c.raw(c.parse(slot['manifestCanonicalJson'].encode('ascii'),'Manifest'),'Manifest')==c.raw(materialize(slot['plan'])[1],'Manifest'))
    return copy.deepcopy(manifest)
def validate_input(data,source,manifest):
    pins();require(type(data) is bytes);c.parse(data,'Child');b.authenticate_source(source)
    key=(data,c.raw(manifest,'Manifest'),b.raw(source,'Source'))
    if key not in _VALIDATED:_VALIDATED[key]=c.validate_child(data,source,manifest)
    return copy.deepcopy(_VALIDATED[key])
def actions(child):
    return [dict(ordinal=i,**copy.deepcopy(step['semanticAction'])) for i,step in enumerate(child['steps'])]
def pair_key(child,source):
    manifest=child['manifest'];request=manifest['request']
    require(child['initialCheckpointCanonicalJson'] is not None)
    return dict(lineageKind=source['lineageKind'],executionProfile=source['executionProfile'],root=copy.deepcopy(source['root']),
        sourceLineageHash=source['sourceLineageHash'],executionStart=copy.deepcopy(source['executionStart']),initialCheckpointCanonicalJson=child['initialCheckpointCanonicalJson'],
        **{k:copy.deepcopy(manifest[k]) for k in ('evidenceScope','confidentiality','setupId','setupHash','contentPackId','contentHash','scenarioId','buildKind','buildHash','seedKind','seedHash')},
        **{k:copy.deepcopy(request[k]) for k in ('initialOccurrence','rulesetHash','configurationHash','requestedTerminal','maximumAcceptedTransitions')},
        clockPolicy=request['schedule']['clockPolicy'],fault=copy.deepcopy(manifest['fault']),expectedFailure=manifest['expectedFailure'])
def different_binding(left,right):
    raw(left,'PairKey');raw(right,'PairKey')
    return next((k for k,t in SCHEMA['PairKey'] if raw(left[k],t)!=raw(right[k],t)),None)
def actual_terminal(child):
    value=b.parse(child['finalCheckpointCanonicalJson'].encode('ascii'),'Checkpoint')
    return dict(occurrence=value['activeOccurrence'] or value['sourceOccurrence'],positionId=value['positionId'],closure=value['closure'])
def comparison(plan,entries,trusted):
    lookup={x['childId']:x for x in entries};a=lookup[plan['baselineChildId']];z=lookup[plan['candidateChildId']]
    value=dict(pairId=plan['pairId'],baselineEntryOrdinal=a['ordinal'],candidateEntryOrdinal=z['ordinal'],status='unavailable',reason=None,mismatchField=None,
        **{k:None for k in ('initialEvidenceHash','baselineActions','candidateActions','firstDivergence','acceptedStepCountDelta','terminalOutcomeEqual','failureCategoryEqual')})
    for entry in (a,z):
        if entry['validation']!='validated':value['reason']='child-'+entry['validation'];return value
    left,ls=trusted[a['childId']];right,rs=trusted[z['childId']]
    if left['initialCheckpointCanonicalJson'] is None or right['initialCheckpointCanonicalJson'] is None:value['reason']='initial-evidence-unavailable';return value
    lk=pair_key(left,ls);rk=pair_key(right,rs);mismatch=different_binding(lk,rk)
    if mismatch:value.update(reason='initial-binding-mismatch',mismatchField=mismatch);return value
    la=actions(left);ra=actions(right)
    value.update(status='compared',initialEvidenceHash=identity(INVENTORY['domains']['initial'],lk,'PairKey'),baselineActions=la,candidateActions=ra,firstDivergence=divergence(la,ra),
        acceptedStepCountDelta=len(ra)-len(la),terminalOutcomeEqual=actual_terminal(left)==actual_terminal(right),failureCategoryEqual=left['result']['failure']==right['result']['failure'])
    return value
def aggregate(manifest,payloads):
    manifest=admit_manifest(manifest);require(type(payloads) is dict and set(payloads)<={x['plan']['childId'] for x in manifest['children']})
    entries=[];trusted={}
    for ordinal,slot in enumerate(manifest['children']):
        plan=slot['plan'];child_id=plan['childId'];source,expected_manifest=materialize(plan);data=payloads.get(child_id)
        entry=dict(ordinal=ordinal,childId=child_id,sourceId=plan['sourceId'],validation='missing' if child_id not in payloads else 'invalid',
            **{k:None for k in ('childHash','manifestHash','childStatus','failure','expectedFailureMatch','acceptedTransitions','initialCheckpointHash','finalCheckpointHash','actions')})
        if child_id in payloads:
            try:child=validate_input(data,source,expected_manifest)
            except (ValueError,KeyError,TypeError,OverflowError,RecursionError):pass
            else:
                trusted[child_id]=(child,source);result=child['result']
                entry.update(validation='validated',childHash=c.child_hash(child),manifestHash=c.identity(c.INVENTORY['domains']['manifest'],expected_manifest,'Manifest'),childStatus=result['status'],failure=result['failure'],
                    expectedFailureMatch=result['expectedFailureMatch'],acceptedTransitions=len(child['steps']),initialCheckpointHash=result['initialCheckpointHash'],finalCheckpointHash=result['finalCheckpointHash'],actions=actions(child))
        entries.append(entry)
    # All inputs have reached strict validation before any aggregate count or fingerprint.
    comparisons=[comparison(p,entries,trusted) for p in manifest['pairs']]
    counts=dict(expected=len(entries),validated=sum(x['validation']=='validated' for x in entries),missing=sum(x['validation']=='missing' for x in entries),invalid=sum(x['validation']=='invalid' for x in entries),
        succeeded=sum(x['childStatus']=='succeeded' for x in entries),failed=sum(x['childStatus']=='failed' for x in entries),acceptedTransitions=sum(x['acceptedTransitions'] or 0 for x in entries),
        comparedPairs=sum(x['status']=='compared' for x in comparisons),unavailablePairs=sum(x['status']=='unavailable' for x in comparisons))
    status='succeeded' if counts['succeeded']==counts['expected'] and not counts['unavailablePairs'] else 'failed'
    deterministic=dict(manifest=manifest,status=status,counts=counts,entries=entries,comparisons=comparisons,interpretation=INVENTORY['enums']['interpretation'][0])
    result=dict(contractVersion=1,deterministic=deterministic,fingerprint=identity(INVENTORY['domains']['parent'],deterministic,'Deterministic'));raw(result,'Parent');return copy.deepcopy(result)
def validate_parent(data,manifest,payloads):
    pins();value=parse(data,'Parent');require(raw(value['deterministic']['manifest'],'ParentManifest')==raw(manifest,'ParentManifest'))
    require(data==raw(aggregate(manifest,payloads),'Parent'));return copy.deepcopy(value)

def test_divergence():
    a=dict(ordinal=0,audience='axis',actionId='a');s=dict(ordinal=1,audience='system',actionId='s')
    assert divergence([a],[copy.deepcopy(a)]) is None
    assert divergence([a],[dict(a,audience='commonwealth')])==dict(ordinal=0,baseline=a,candidate=dict(a,audience='commonwealth'))
    assert divergence([a],[dict(a,actionId='b')])['ordinal']==0
    assert divergence([a],[a,s])==dict(ordinal=1,baseline=None,candidate=s)
    assert divergence([a,s],[a])==dict(ordinal=1,baseline=s,candidate=None)
    assert divergence([],[]) is None
    return 6

def test_missing_and_null():
    plan=dict(childId='null-boundary',sourceId='corrected-composition.ordinary.axis.attacker',maximumAcceptedTransitions=32,fault=dict(kind='none',atTransition=0),expectedFailure=None)
    manifest=dict(contractVersion=1,parentId='null-boundary',children=[expected(plan)],pairs=[])
    missing=aggregate(manifest,{})['deterministic'];invalid=aggregate(manifest,{plan['childId']:None})['deterministic']
    assert missing['entries'][0]['validation']=='missing' and missing['counts']['missing']==1
    assert invalid['entries'][0]['validation']=='invalid' and invalid['counts']['invalid']==1
    assert missing['counts']['validated']==invalid['counts']['validated']==0
    for report in (missing,invalid):
        assert all(value is None for key,value in report['entries'][0].items() if key not in ('ordinal','childId','sourceId','validation'))
    return 2

_CORPUS=None
def corpus():
    global _CORPUS
    if _CORPUS is None:
        ordinary='corrected-composition.ordinary.axis.attacker'
        guard='corrected-composition.defender-capture-guard.axis.attacker'
        escape='corrected-composition.defender-capture-escape.axis.attacker'
        fallback='settlement-v2.attacker-capture-escape.axis.attacker.fallback-retreat'
        historical=next(x['sourceId'] for x in b.source_cases() if x['reference']['surface']=='historical-v3')
        configs=[('base',ordinary,32,'none',None),('twin',ordinary,32,'none',None),
            ('order','corrected-composition.ordinary.axis.defender',32,'none',None),
            ('guard',guard,32,'none',None),('escape',escape,32,'none',None),
            ('failed-guard',guard,32,'reconstruction','reconstruction-failed'),('failed-escape',escape,32,'reconstruction','reconstruction-failed'),
            ('fallback-a',fallback,16,'none','step-limit-exceeded'),('fallback-b',fallback,16,'none','step-limit-exceeded'),
            ('limit',ordinary,31,'none',None),('early',fallback,17,'none','admission-rejected'),('historical',historical,0,'none',None)]
        result={}
        for name,source_id,limit,fault,expected_failure in configs:
            plan=dict(childId='parent-child.'+name,sourceId=source_id,maximumAcceptedTransitions=limit,fault=dict(kind=fault,atTransition=0),expectedFailure=expected_failure)
            source,manifest=materialize(plan);child=c.run(manifest,source);data=c.raw(child,'Child')
            validate_input(data,source,manifest)
            result[name]=dict(plan=plan,data=data)
            print('B3 authenticated child',name,child['result']['status'],len(child['steps']),flush=True)
        _CORPUS=result
    return copy.deepcopy(_CORPUS)
def case_for(name,left,right=None):
    data=corpus();names=[left] if right is None else [left,right];slots=[expected(data[n]['plan']) for n in names]
    pair=[] if right is None else [dict(pairId='pair.'+name,baselineChildId=slots[0]['plan']['childId'],candidateChildId=slots[1]['plan']['childId'])]
    manifest=dict(contractVersion=1,parentId='parent.'+name,children=slots,pairs=pair)
    payloads={data[n]['plan']['childId']:data[n]['data'] for n in names};return manifest,payloads
CASE_ROWS=[('identical','base','twin'),('seal-order','base','order'),('guard-escape','guard','escape'),('validated-failures','failed-guard','failed-escape'),
    ('system-fallback','fallback-a','fallback-b'),('initial-mismatch','base','guard'),('limit-mismatch','base','limit'),('fault-mismatch','guard','failed-guard'),('early-unavailable','early','fallback-a'),('historical-zero','historical',None)]

def test_native_parents():
    results={}
    for name,left,right in CASE_ROWS:
        manifest,payloads=case_for(name,left,right);parent=aggregate(manifest,payloads)
        assert validate_parent(raw(parent,'Parent'),manifest,payloads)==parent
        assert raw(aggregate(manifest,payloads),'Parent')==raw(parent,'Parent')
        report=parent['deterministic'];results[name]=report
    for name in ('identical','seal-order','guard-escape','validated-failures','system-fallback'):
        assert results[name]['comparisons'][0]['status']=='compared'
    assert results['identical']['comparisons'][0]['firstDivergence'] is None
    assert results['seal-order']['comparisons'][0]['firstDivergence'] is not None
    assert results['guard-escape']['comparisons'][0]['firstDivergence'] is not None
    assert results['validated-failures']['counts']['failed']==2 and results['validated-failures']['status']=='failed'
    assert all(x['expectedFailureMatch'] for x in results['validated-failures']['entries'])
    assert results['system-fallback']['counts']['failed']==2
    assert results['system-fallback']['entries'][0]['actions'][-1]['audience']=='system'
    for name in ('initial-mismatch','limit-mismatch','fault-mismatch','early-unavailable'):
        comparison=results[name]['comparisons'][0];assert comparison['status']=='unavailable'
        assert all(comparison[k] is None for k in ('initialEvidenceHash','baselineActions','candidateActions','firstDivergence','acceptedStepCountDelta','terminalOutcomeEqual','failureCategoryEqual'))
    assert results['limit-mismatch']['comparisons'][0]['mismatchField']=='maximumAcceptedTransitions'
    assert results['fault-mismatch']['comparisons'][0]['mismatchField']=='fault'
    assert results['early-unavailable']['comparisons'][0]['reason']=='initial-evidence-unavailable'
    assert results['historical-zero']['counts']['acceptedTransitions']==0 and results['historical-zero']['status']=='succeeded'
    stream=results['identical']['entries'][0]['actions'];prefix=stream[:5]
    assert divergence(prefix,stream)==dict(ordinal=5,baseline=None,candidate=stream[5])
    assert divergence(stream,prefix)==dict(ordinal=5,baseline=stream[5],candidate=None)
    assert divergence([],stream)==dict(ordinal=0,baseline=None,candidate=stream[0])
    assert divergence(stream,[])==dict(ordinal=0,baseline=stream[0],candidate=None)
    assert divergence(stream,copy.deepcopy(stream)) is None
    return dict(children=len(corpus()),parents=len(results),authenticatedPrefixUnitCases=5)

def test_invalid_children_and_cache():
    manifest,payloads=case_for('invalid','base','twin');child_id=manifest['children'][0]['plan']['childId'];data=payloads[child_id]
    child=c.parse(data,'Child');mutants=[]
    for mutate in (lambda x:x['result'].__setitem__('status','failed'),lambda x:x['steps'][0]['semanticAction'].__setitem__('audience','axis'),
                   lambda x:x['reconstruction'].__setitem__('eventHash','sha256:'+'0'*64),lambda x:x['manifest'].__setitem__('buildHash','sha256:'+'0'*64),
                   lambda x:x['manifest']['controller'].__setitem__('custodyChoice','escape')):
        bad=copy.deepcopy(child);mutate(bad);bad['artifactManifest']=c.artifact_inventory(bad);mutants.append(c.raw(bad,'Child'))
    mutants.extend((data+b'\n',data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1),b'['*1100+b']'*1100,corpus()['guard']['data']))
    for bad in mutants:
        supplied=dict(payloads);supplied[child_id]=bad;report=aggregate(manifest,supplied)['deterministic'];entry=report['entries'][0]
        assert entry['validation']=='invalid' and all(entry[k] is None for k in ('childHash','manifestHash','childStatus','failure','expectedFailureMatch','acceptedTransitions','initialCheckpointHash','finalCheckpointHash','actions'))
        assert report['counts']['validated']==1 and report['counts']['invalid']==1 and report['counts']['acceptedTransitions']==len(child['steps'])
        assert report['comparisons'][0]['reason']=='child-invalid'
    missing=dict(payloads);del missing[child_id];report=aggregate(manifest,missing)['deterministic']
    assert report['counts']['missing']==1 and report['comparisons'][0]['reason']=='child-missing'
    source,expected_manifest=materialize(manifest['children'][0]['plan']);returned=validate_input(data,source,expected_manifest)
    returned['steps'].clear();assert len(validate_input(data,source,expected_manifest)['steps'])==len(child['steps'])
    pin=next(iter(INVENTORY['sourcePins']));old=INVENTORY['sourcePins'][pin]
    try:INVENTORY['sourcePins'][pin]='sha256:'+'0'*64;rejected(lambda:validate_input(data,source,expected_manifest))
    finally:INVENTORY['sourcePins'][pin]=old
    wrong=copy.deepcopy(expected_manifest);wrong['childId']='foreign';rejected(lambda:validate_input(data,source,wrong))
    foreign=source_for(corpus()['guard']['plan']['sourceId']);rejected(lambda:validate_input(data,foreign,expected_manifest))
    return dict(invalidChildren=len(mutants),missing=1,cacheIsolationChecks=4)

def test_materialization_and_bindings():
    manifest,payloads=case_for('bindings','base','twin');slot=manifest['children'][0]
    for field,value in (('buildHash','sha256:'+'0'*64),('seedHash','sha256:'+'0'*64),('expectedFailure','cancelled')):
        bad=copy.deepcopy(manifest);expected_manifest=c.parse(slot['manifestCanonicalJson'].encode(),'Manifest');expected_manifest[field]=value
        bad['children'][0]['manifestCanonicalJson']=c.raw(expected_manifest,'Manifest').decode();rejected(lambda:aggregate(bad,payloads))
    for mutate in (lambda x:x['children'].append(copy.deepcopy(x['children'][0])),lambda x:x['pairs'][0].__setitem__('candidateChildId',slot['plan']['childId']),
                   lambda x:x['pairs'][0].__setitem__('baselineChildId','unknown'),lambda x:x['pairs'].append(copy.deepcopy(x['pairs'][0]))):
        bad=copy.deepcopy(manifest);mutate(bad);rejected(lambda:admit_manifest(bad))
    rejected(lambda:aggregate(manifest,dict(payloads,unexpected=b'{}')))
    source,expected_manifest=materialize(slot['plan']);child=validate_input(payloads[slot['plan']['childId']],source,expected_manifest);key=pair_key(child,source)
    changes={'sourceLineageHash':'sha256:'+'0'*64,'initialCheckpointCanonicalJson':key['initialCheckpointCanonicalJson']+' ',
        'seedHash':'sha256:'+'0'*64,'buildHash':'sha256:'+'0'*64,'configurationHash':'sha256:'+'0'*64,'maximumAcceptedTransitions':31,
        'fault':dict(kind='reconstruction',atTransition=0),'expectedFailure':'cancelled',
        'requestedTerminal':dict(key['requestedTerminal'],closure='entry'),'executionProfile':'historical-terminal-checkpoint'}
    for field,value in changes.items():
        other=copy.deepcopy(key);other[field]=value;assert different_binding(key,other)==field
    assert different_binding(key,copy.deepcopy(key)) is None
    return dict(materializationRejects=8,pairBindingMismatches=len(changes))

def test_raw_and_report_forgery():
    manifest,payloads=case_for('forgery','base','twin');parent=aggregate(manifest,payloads);data=raw(parent,'Parent')
    for mutate in (lambda x:x['deterministic']['counts'].__setitem__('succeeded',0),lambda x:x['deterministic']['entries'].reverse(),
        lambda x:x['deterministic']['comparisons'][0].__setitem__('firstDivergence',dict(ordinal=0,baseline=None,candidate=x['deterministic']['entries'][0]['actions'][0])),
        lambda x:x['deterministic']['comparisons'][0].__setitem__('acceptedStepCountDelta',1)):
        bad=copy.deepcopy(parent);mutate(bad);bad['fingerprint']=identity(INVENTORY['domains']['parent'],bad['deterministic'],'Deterministic')
        rejected(lambda:validate_parent(raw(bad,'Parent'),manifest,payloads))
    bad=copy.deepcopy(parent);bad['fingerprint']='sha256:'+'0'*64;rejected(lambda:validate_parent(raw(bad,'Parent'),manifest,payloads))
    for bad in (data+b'\n',data.replace(b'"contractVersion":1',b'"contractVersion":true',1),data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1),
        data.replace(b'{"contractVersion":1',b'{"contractVersion":1,"contractVersion":1',1),b'\xff',b' '*8388609):rejected(lambda:parse(bad,'Parent'))
    reordered=dict(reversed(list(parent.items())));rejected(lambda:parse(c.encode(reordered),'Parent'))
    for value in (True,1.0,-65,65):rejected(lambda:typed(value,'delta'))
    rejected(lambda:typed([{}]*129,'Action[]'));rejected(lambda:typed(None,'Parent',53))
    bad=copy.deepcopy(manifest);bad['children']*=17;rejected(lambda:admit_manifest(bad))
    bad=copy.deepcopy(manifest);bad['pairs']*=17;rejected(lambda:admit_manifest(bad))
    literal=raw(dict(ordinal=0,audience='system',actionId='sample'),'Action')
    assert literal==b'{"ordinal":0,"audience":"system","actionId":"sample"}'
    expected_hash='sha256:'+hashlib.sha256(INVENTORY['domains']['parent'].encode()+b'\0'+raw(parent['deterministic'],'Deterministic')).hexdigest()
    assert parent['fingerprint']==expected_hash
    result=validate_parent(data,manifest,payloads);result['deterministic']['entries'].clear();assert validate_parent(data,manifest,payloads)==parent
    return dict(forgedReports=5,rawRejects=7,boundaryRejects=8,independentHash=1,returnedCopy=1)

def generated_fixture():
    rows=corpus();reports=[]
    for name,left,right in CASE_ROWS:
        manifest,payloads=case_for(name,left,right);value=aggregate(manifest,payloads)
        reports.append(dict(name=name,parentCanonicalJson=raw(value,'Parent').decode()))
    return dict(contractVersion=1,scope='private B3 contract evidence; selected actual children only; unequal-stream prefix tests are algorithm-only',
        sourcePins=INVENTORY['sourcePins'],children=[dict(name=name,plan=item['plan'],childCanonicalJson=item['data'].decode()) for name,item in rows.items()],parents=reports)
def read_fixture(data):
    pins();require(type(data) is bytes);value=generated_fixture();require(data==fixture_bytes(value));return copy.deepcopy(value)
def test_fixture():
    data=FIXTURE.read_bytes();value=read_fixture(data)
    for bad in (data+b' ',data.replace(b'"contractVersion": 1',b'"contractVersion": 1.0',1),data.replace(b'"contractVersion": 1',b'"contractVersion": true',1),
                data.replace(b'"contractVersion": 1,',b'"contractVersion": 1, "contractVersion": 1,',1)):
        rejected(lambda:read_fixture(bad))
    return dict(bytes=len(data),children=len(value['children']),parents=len(value['parents']),fixtureMutationRejects=4,largestParentBytes=max(len(x['parentCanonicalJson'].encode()) for x in value['parents']))
TESTS=(test_divergence,test_missing_and_null,test_native_parents,test_invalid_children_and_cache,test_materialization_and_bindings,test_raw_and_report_forgery)
def main():
    started=time.monotonic()
    for test in TESTS:print(test.__name__,test(),round(time.monotonic()-started,3),flush=True)
    print('test_fixture',test_fixture(),flush=True);print('B3 PASS',round(time.monotonic()-started,3),flush=True)
if __name__=='__main__':main()
