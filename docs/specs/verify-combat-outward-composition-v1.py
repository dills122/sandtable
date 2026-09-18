#!/usr/bin/env python3
"""Bounded C integration index; accepted readers, no runtime admission."""
from __future__ import annotations
import copy,hashlib,importlib.util,json,re,struct,sys,time
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent;REPO=ROOT.parent.parent
FIXTURE=ROOT/'fixtures/combat-outward-composition-v1.json'
INVENTORY=json.loads((ROOT/'combat-outward-composition-v1.schema.json').read_bytes())
SCHEMA={k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
if 'combat_parent' in sys.modules:p=sys.modules['combat_parent']
else:
    spec=importlib.util.spec_from_file_location('combat_parent',ROOT/'verify-combat-exercise-parent-evidence-v1.py')
    p=importlib.util.module_from_spec(spec);sys.modules[spec.name]=p;spec.loader.exec_module(p)
c=p.c;b=c.b;s=c.s
class Invalid(ValueError):pass
def require(value):
    if not value:raise Invalid('CMB-OUTWARD-COMPOSITION-REJECTED')
def typed(value,kind,depth=0):
    require(depth<=56)
    if kind=='Requirement' and type(value) is dict and 'deferredComponent' in value:kind='DeferredRequirement'
    if kind.endswith('?'):
        if value is not None:typed(value,kind[:-1],depth+1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value)<=512)
        for item in value:typed(item,kind[:-2],depth+1)
    elif kind in SCHEMA:
        require(type(value) is dict and set(value)=={k for k,_ in SCHEMA[kind]})
        for key,t in SCHEMA[kind]:typed(value[key],t,depth+1)
    elif kind in INVENTORY['enums']:require(type(value) is str and value in INVENTORY['enums'][kind])
    elif kind=='text':require(type(value) is str and 0<len(value)<=65536 and all(ord(x)>=32 and not 0xD800<=ord(x)<=0xDFFF for x in value))
    else:c.typed(value,kind,depth)
def canonical(value,kind):
    if kind=='Requirement' and 'deferredComponent' in value:kind='DeferredRequirement'
    if kind.endswith('?'):return None if value is None else canonical(value,kind[:-1])
    if kind.endswith('[]'):return [canonical(x,kind[:-2]) for x in value]
    if kind in SCHEMA:return {k:canonical(value[k],t) for k,t in SCHEMA[kind]}
    return c.canonical(value,kind)
def raw(value,kind):
    typed(value,kind);data=c.encode(canonical(value,kind));require(len(data)<=2097152);return data
def parse(data,kind):
    require(type(data) is bytes and len(data)<=2097152)
    def pairs(items):
        value={}
        for k,v in items:require(k not in value);value[k]=v
        return value
    value=json.loads(data.decode('ascii'),object_pairs_hook=pairs,parse_constant=lambda _:require(False));require(raw(value,kind)==data);return value
def pins():
    require(all(c.sha((REPO/path).read_bytes())==expected for path,expected in INVENTORY['sourcePins'].items()));p.pins()
def rejected(fn):
    try:fn()
    except (ValueError,KeyError,TypeError,OverflowError,RecursionError,AssertionError):return
    raise AssertionError('invalid integration evidence accepted')
def validate_requirements(rows):
    raw(rows,'Requirement[]');require(len(rows)==72 and raw(rows,'Requirement[]')==raw(INVENTORY['requirements'],'Requirement[]'))
    aliases={a['alias'] for a in INVENTORY['aliases']}
    require(len({r['id'] for r in rows})==72)
    for row in rows:
        require(row['source'] in INVENTORY['sourcePins'])
        line=(REPO/row['source']).read_text().splitlines()[row['line']-1];require(row['id'] in line)
        require(row['verificationStatus']=='planned; runtime test not implemented')
        require(set(re.findall(r'\b([A-Z][A-Z0-9]*):',row['contractEvidenceDraft']['evidence']))<=aliases)
    return copy.deepcopy(rows)

def fixture_bytes(value):return (json.dumps(value,ensure_ascii=True,indent=2)+'\n').encode('ascii')
def tree_limits(value,depth=0):
    if type(value) is dict:
        children=[tree_limits(v,depth+1) for v in value.values()]
    elif type(value) is list:
        children=[tree_limits(v,depth+1) for v in value]
    else:children=[]
    return max([depth]+[d for d,_ in children]),max([len(value) if type(value) is list else 0]+[a for _,a in children])
def capacity(profile,kind,records,parser,limits):
    require(records);maximum=0;depth=0;arrays=0;digest=hashlib.sha256(INVENTORY['domains']['records'].encode()+b'\0')
    for data in records:
        value=parser(data,kind);d,a=tree_limits(value);maximum=max(maximum,len(data));depth=max(depth,d);arrays=max(arrays,a)
        digest.update(struct.pack('>i',len(data)));digest.update(data)
    require(maximum<=limits['bytes'] and depth<=limits['depth'] and arrays<=limits['arrayItems'])
    return dict(profile=profile,payloadType=kind,records=len(records),maximumBytes=maximum,byteLimit=limits['bytes'],maximumDepth=depth,depthLimit=limits['depth'],maximumArrayItems=arrays,arrayLimit=limits['arrayItems'],recordsHash='sha256:'+digest.hexdigest())
def retained_artifacts():
    return {name:json.loads((ROOT/'fixtures'/f'{name}.json').read_bytes()) for name in ('combat-side-projection-v1','combat-exercise-occurrence-v1','combat-exercise-child-evidence-v1','combat-exercise-parent-evidence-v1')}
def profiles(artifacts):
    side=artifacts['combat-side-projection-v1'];rows=[]
    def add(name,version,kind,traces,admission,scope,capability=None,policies=()):
        rows.append(dict(id=name,contractVersion=version,payloadType=kind,capability=capability,sourceCount=len({t['source'] for t in traces}),traceCount=len(traces),admission=admission,clockPolicies=list(policies),scope=scope))
    add('a1-historical',1,'Observation',[t for t in side['traces'] if not t['source'].startswith('clock-v2.')],'historical-diagnostic-only','Original nine sources and historical counterexample; no current admission.')
    add('a1-corrected',1,'Observation',[t for t in side['traces'] if t['source'].startswith('clock-v2.')],'current-local','Corrected local selection/round only.',policies=(s.rnd2.POLICY,))
    add('a2-corrected',2,'Observation2',side['successor2']['traces'],'current-local','Corrected local settlement; no Reserve/control.',policies=(s.rnd2.POLICY,s.res2.POLICY))
    for family,capability in (('inherited-reserve',s.CAP_RELEASE3),('inherited-control',s.CAP_CONTROL3)):
        add(family,3,'Observation3',[t for t in side['successor3']['traces'] if t['source'].startswith(family+'.')],'current-local','Exact inherited full-World sources and bounded retained-clock semantics.',capability,('sandtable.side.inherited-retained-clock.v3',))
    for family,capability in (('ledger-reserve',s.CAP_RESERVE_LEDGER3),('ledger-cycle',s.CAP_CYCLE_LEDGER3)):
        add(family,3,'LedgerObservation3',[t for t in side['successor3']['ledgerTraces'] if t['source'].startswith(family+'.')],'standalone-ledger-only','Canonical ledger behavior; not a full-World source or B1/B2 child.',capability,('sandtable.side.inherited-retained-clock.v3',))
    add('a3b-corrected',3,'Observation3',[t for t in side['successor3b']['traces'] if t['family']=='corrected-composition'],'current-local','Full corrected C3 through Result2, empty Reserve and finish-only bridge.',s.CAP_CORRECTED3B,(s.rnd2.POLICY,s.res2.POLICY,s.bridge3b.POLICY))
    add('a3b-historical-terminal',3,'Observation3',[t for t in side['successor3b']['traces'] if t['family']=='historical-terminal'],'projection-only','Exact Task003 terminal; no fabricated earlier public history/actions.',s.CAP_TERMINAL3B,('sandtable.side.inherited-retained-clock.v3',))
    rows.extend([
        dict(id='b1-private',contractVersion=1,payloadType='Checkpoint',capability=None,sourceCount=len(artifacts['combat-exercise-occurrence-v1']['entries']),traceCount=len(artifacts['combat-exercise-occurrence-v1']['goldens']),admission='current-local',clockPolicies=['native-retained-trusted-clock.v1'],scope='Three explicit private execution scopes; source prefix and full reference identities remain distinct.'),
        dict(id='b2-private',contractVersion=1,payloadType='Child',capability=None,sourceCount=len(artifacts['combat-exercise-child-evidence-v1']['summaries']),traceCount=len(artifacts['combat-exercise-child-evidence-v1']['goldens']),admission='current-local',clockPolicies=[],scope='Selected actual current-build children plus134 authenticated reference summaries; not134 repeated executions.'),
        dict(id='b3-private',contractVersion=1,payloadType='Parent',capability=None,sourceCount=len(artifacts['combat-exercise-parent-evidence-v1']['children']),traceCount=len(artifacts['combat-exercise-parent-evidence-v1']['parents']),admission='current-local',clockPolicies=[],scope='Validated aggregation/pairs; unequal-stream prefix unit tests do not claim an admitted unequal-length pair.')])
    return rows
def capacities(artifacts):
    side=artifacts['combat-side-projection-v1'];result=[]
    for name,kind,traces,parser,limits in (
        ('a1','Observation',side['traces'],s.parse,s.INVENTORY['limits']),
        ('a2','Observation2',side['successor2']['traces'],s.parse2,s.INVENTORY['limits2']),
        ('a3-inherited','Observation3',side['successor3']['traces'],s.parse3,s.INVENTORY['limits3']),
        ('a3-ledger','LedgerObservation3',side['successor3']['ledgerTraces'],s.parse3,s.INVENTORY['limits3']),
        ('a3b-corrected','Observation3',[t for t in side['successor3b']['traces'] if t['family']=='corrected-composition'],s.parse3,s.INVENTORY['limits3']),
        ('a3b-historical','Observation3',[t for t in side['successor3b']['traces'] if t['family']=='historical-terminal'],s.parse3,s.INVENTORY['limits3'])):
        records=[g['canonicalJson'].encode('ascii') for t in traces for g in t['goldens']];result.append(capacity(name,kind,records,parser,limits))
    first=artifacts['combat-exercise-occurrence-v1']['goldens']
    result.append(capacity('b1-source','Source',[x['sourceCanonicalJson'].encode() for x in first],b.parse,b.INVENTORY['limits']))
    result.append(capacity('b1-checkpoint','Checkpoint',[x[k].encode() for x in first for k in ('initialCheckpointCanonicalJson','finalCheckpointCanonicalJson')],b.parse,b.INVENTORY['limits']))
    result.append(capacity('b2-child','Child',[x['childCanonicalJson'].encode() for x in artifacts['combat-exercise-child-evidence-v1']['goldens']],c.parse,c.INVENTORY['limits']))
    result.append(capacity('b3-parent','Parent',[x['parentCanonicalJson'].encode() for x in artifacts['combat-exercise-parent-evidence-v1']['parents']],p.parse,p.INVENTORY['limits']))
    comp=s.composition_evidence3b();native=s.composition3b
    result.append(capacity('task003-snapshot','RootSnapshot',[native.raw(x['snapshot'],'RootSnapshot') for x in comp['traces']],native.parse,native.INVENTORY['limits']))
    return result

def selected_readbacks(artifacts):
    witnesses=[]
    def record(name,reader,source,cut,audience,kind,data,version):
        witnesses.append(dict(name=name,reader=reader,sourceId=source,cut=cut,audience=audience,payloadType=kind,contractVersion=version,bytes=len(data),sha256=c.sha(data)))
    comp=copy.deepcopy(s.composition_evidence3b());native=s.composition3b
    comp_data=native.raw(comp,'AuthorityComposition');require(native.read_composition(comp_data,comp)==comp)
    require(comp['handoff']==artifacts['combat-side-projection-v1']['successor3b']['task003Handoff']==artifacts['combat-exercise-occurrence-v1']['task003Handoff'])
    require(len(comp['traces'])==28 and comp['handoff']['selectedTraceIds']==[x['traceId'] for x in comp['traces']])
    record('task003-composition','read_composition','task003',None,None,'AuthorityComposition',comp_data,1)
    selected=[('a1',next(x for x in s.source_cases() if x['name']=='clock-v2.axis.accepted-decline'),s.views,s.raw,s.read_observation,'Observation',1),
        ('a2',next(x for x in s.source_cases2() if 'defender-capture-guard.axis.attacker' in x['name'] and not x['name'].endswith(('.fallback-retreat','.fallback-custody','.prior-time'))),s.views2,s.raw2,s.read_observation2,'Observation2',2),
        ('a3-live',next(x for x in s.source_cases3() if x['name']=='inherited-reserve.axis'),s.views3,s.raw3,s.read_observation3,'Observation3',3),
        ('a3b-corrected',next(x for x in s.source_cases3b() if x['name']=='corrected-composition.defender-capture-guard.axis.attacker'),s.views3b,s.raw3,s.read_observation3b,'Observation3',3)]
    for label,source,project,writer,reader,kind,version in selected:
        for audience in s.SIDES:
            views=project(source,audience);cuts={0,len(views)-1};seen=set()
            for cut,view in enumerate(views):
                key=view['decision']['kind'] if view['decision'] else view['status']
                if key not in seen:cuts.add(cut);seen.add(key)
            for cut in sorted(cuts):
                data=writer(views[cut],kind);require(reader(data,s.prefix(source,cut),audience)==views[cut])
                record(label+'.'+audience+'.'+str(cut),reader.__name__,source['name'],cut,audience,kind,data,version)
    terminals=[x for x in s.source_cases3b() if x['family']=='historical-terminal' and 'combat-inherited-reserve-movement-completion-v1:' in x['name']]
    require(len(terminals)==2)
    for source in terminals:
        for audience in s.SIDES:
            view=s.views3b(source,audience)[0];data=s.raw3(view,'Observation3');require(s.read_observation3b(data,source,audience)==view)
            require(len(view['history'])==1 and not view['ownReceipts'] and view['decision'] is None)
            record('terminal.'+source['name']+'.'+audience,'read_observation3b',source['name'],0,audience,'Observation3',data,3)
    ledger=next(x for x in s.ledger_cases3() if x['family']=='ledger-reserve' and x['base']['case']['name']=='later-complete-intent' and x['base']['side']=='axis')
    for cut,view in enumerate(s.ledger_views3(ledger,'axis')):
        data=s.raw3(view,'LedgerObservation3');require(s.parse3(data,'LedgerObservation3')==s.ledger_views3(s.prefix(ledger,cut),'axis')[-1])
        record('ledger.'+str(cut),'parse3+ledger_views3',ledger['name'],cut,'axis','LedgerObservation3',data,3)
    corpus=p.corpus();source,manifest=p.materialize(corpus['guard']['plan']);source_data=b.raw(source,'Source');require(b.read_source(source_data)==source)
    record('b1-source','read_source',source['sourceId'],None,None,'Source',source_data,1)
    for cut in (0,len(b.decode_reference(source)['events'])):
        cp=b.checkpoint(source,cut);data=b.raw(cp,'Checkpoint');require(b.read_checkpoint(data,source)==cp)
        record('b1-checkpoint.'+str(cut),'read_checkpoint',source['sourceId'],cut,None,'Checkpoint',data,1)
    for name in ('guard','failed-guard','fallback-a'):
        source,manifest=p.materialize(corpus[name]['plan']);data=corpus[name]['data'];value=c.validate_child(data,source,manifest)
        require(value['result']['status']==('succeeded' if name=='guard' else 'failed'))
        record('b2-'+name,'validate_child',source['sourceId'],None,None,'Child',data,1)
    for name,left,right in (('guard-escape','guard','escape'),('validated-failures','failed-guard','failed-escape'),('limit-mismatch','base','limit')):
        manifest,payloads=p.case_for(name,left,right);value=p.aggregate(manifest,payloads);data=p.raw(value,'Parent');require(p.validate_parent(data,manifest,payloads)==value)
        record('b3-'+name,'validate_parent',manifest['parentId'],None,None,'Parent',data,1)
    handoff=comp['handoff'];h=dict(owner=handoff['owner'],traceIds=copy.deepcopy(handoff['selectedTraceIds']),versionsCanonicalJson=native.raw(handoff['versions'],'VersionSet').decode(),compositionDigest=handoff['compositionDigest'],canonicalHash=c.sha(native.raw(handoff,'Task004Handoff')))
    return witnesses,h

EXCLUSIONS=[('runtime','Hosted Dispatch, provider, Runner, War Diary and persistence activation remain planned; no production registration.'),
    ('unsupported-profiles','Multiple-opportunity Reaction, positive vehicle Breakdown, arbitrary geometry and broader attachment/Probe/Barrage/Anti-Armor families remain unadmitted.'),
    ('historical-clock','Original historical diagnostic/counterexample is retained; it is not a passing current-profile acceptance test.'),
    ('calendar','Future upkeep/replacement obligations retained; maturity execution and successful Game terminal excluded.'),
    ('ledger','Standalone canonical ledgers and four isolated owner-completion classifier witnesses are not full-World children.'),
    ('pairing','Unequal-length null-arm algorithm uses authenticated prefixes only; no current comparable unequal-length child pair or post-divergence RNG alignment claimed.'),
    ('task005','Production/dormant C# arithmetic, RulesInput provenance and table parity remain Task005 work; no implementation claim made here.')]
_INDEX_BYTES=None
def build_index():
    global _INDEX_BYTES
    pins()
    if _INDEX_BYTES is None:
        artifacts=retained_artifacts();requirements=validate_requirements(INVENTORY['requirements']);readbacks,handoff=selected_readbacks(artifacts)
        value=dict(contractVersion=1,scope=INVENTORY['enums']['scopeIndex'][0],requirements=requirements,aliases=copy.deepcopy(INVENTORY['aliases']),
            artifacts=[dict(path=path,sha256=digest,bytes=(REPO/path).stat().st_size) for path,digest in INVENTORY['sourcePins'].items()],profiles=profiles(artifacts),handoff=handoff,
            capacities=capacities(artifacts),readbacks=readbacks,exclusions=[dict(id=k,disposition=v) for k,v in EXCLUSIONS]);_INDEX_BYTES=raw(value,'Index')
    return parse(_INDEX_BYTES,'Index')
def read_index(data):
    pins();value=parse(data,'Index');validate_requirements(value['requirements']);require(data==raw(build_index(),'Index'));return copy.deepcopy(value)
def read_fixture(data):
    value=build_index();require(type(data) is bytes and data==fixture_bytes(value));return copy.deepcopy(value)
def test_requirements():
    validate_requirements(INVENTORY['requirements'])
    for mutation in (lambda x:x.pop(),lambda x:x.append(copy.deepcopy(x[0])),lambda x:x.reverse(),lambda x:x[0].__setitem__('id','UNKNOWN-AC-001'),
        lambda x:x[0].__setitem__('plannedTest','Forged.Test'),lambda x:x[0]['contractEvidenceDraft'].__setitem__('runtimeOwners','999'),lambda x:x[0].__setitem__('requirement','changed')):
        rows=copy.deepcopy(INVENTORY['requirements']);mutation(rows);rejected(lambda:validate_requirements(rows))
    return dict(requirements=72,mutationRejects=7)

def test_integrated_readback():
    value=build_index();data=raw(value,'Index');require(read_index(data)==value)
    profiles_by_id={x['id']:x for x in value['profiles']}
    for key,count in (('a1-historical',9),('a1-corrected',20),('a2-corrected',48),('inherited-reserve',4),('inherited-control',6),('a3b-corrected',88),('a3b-historical-terminal',28),('b1-private',134),('b2-private',134),('b3-private',12)):
        assert profiles_by_id[key]['sourceCount']==count,(key,profiles_by_id[key])
    assert sum(profiles_by_id[k]['sourceCount'] for k in ('ledger-reserve','ledger-cycle'))==112
    assert len(json.loads(s.rel3.FIXTURE.read_bytes())['cases'])==13 and len(json.loads(s.cyc3.FIXTURE.read_bytes())['cases'])==19
    assert len(value['handoff']['traceIds'])==28 and len(set(value['handoff']['traceIds']))==28
    assert all(x['maximumBytes']<=x['byteLimit'] for x in value['capacities'])
    copied=read_index(data);copied['requirements'].clear();assert read_index(data)==value
    return dict(requirements=len(value['requirements']),pins=len(value['artifacts']),profiles=len(value['profiles']),readbacks=len(value['readbacks']),handoff=28,capacityGroups=len(value['capacities']),canonicalIndexBytes=len(data))

def test_native_rejections():
    corpus=p.corpus();source,manifest=p.materialize(corpus['guard']['plan']);cp=b.checkpoint(source)
    wrong=copy.deepcopy(cp);wrong['sourceOccurrence']['ordinal']+=1;rejected(lambda:b.read_checkpoint(b.raw(wrong,'Checkpoint'),source))
    bad_source=copy.deepcopy(source);bad_source['sourceLineageHash']='sha256:'+'0'*64;rejected(lambda:b.read_source(b.raw(bad_source,'Source')))
    for name,mutate in (('fallback-a',lambda x:x['result'].__setitem__('status','succeeded')),('guard',lambda x:x.__setitem__('reconstruction',None))):
        source,manifest=p.materialize(corpus[name]['plan']);bad=c.parse(corpus[name]['data'],'Child');mutate(bad);bad['artifactManifest']=c.artifact_inventory(bad)
        rejected(lambda:c.validate_child(c.raw(bad,'Child'),source,manifest))
    manifest,payloads=p.case_for('guard-escape','guard','escape');parent=p.aggregate(manifest,payloads);parent['deterministic']['counts']['succeeded']=0
    parent['fingerprint']=p.identity(p.INVENTORY['domains']['parent'],parent['deterministic'],'Deterministic')
    rejected(lambda:p.validate_parent(p.raw(parent,'Parent'),manifest,payloads))
    return dict(occurrence=1,source=1,childSuccessAndProof=2,parent=1)

def test_profile_separation():
    corrected=next(x for x in s.source_cases3b() if x['name']=='corrected-composition.defender-capture-guard.axis.attacker')
    view=s.views3b(corrected,'axis')[0];v3=s.raw3(view,'Observation3')
    rejected(lambda:s.parse(v3,'Observation'));rejected(lambda:s.parse2(v3,'Observation2'))
    wrong=copy.deepcopy(view);wrong['context']['capabilityPolicyId']=s.CAP_RELEASE3
    rejected(lambda:s.read_observation3b(s.raw3(wrong,'Observation3'),s.prefix(corrected,0),'axis'))
    ledger=next(x for x in s.ledger_cases3() if x['family']=='ledger-reserve' and x['base']['case']['name']=='later-complete-intent' and x['base']['side']=='axis')
    ledger_view=s.ledger_views3(ledger,'axis')[0];ledger_data=s.raw3(ledger_view,'LedgerObservation3')
    rejected(lambda:s.parse3(ledger_data,'Observation3'));rejected(lambda:s.validate_source3(ledger));rejected(lambda:s.validate_source3b(ledger))
    for cut,offered in enumerate(s.ledger_views3(ledger,'axis')):
        if offered['decision']:
            proposal=s.raw3(s.submission3(offered),'Submission3')
            outcome=s.admit_a3b(s.prefix(ledger,cut),'axis',proposal)
            assert outcome['status']=='rejected' and outcome['receipt'] is None
            break
    retimed=next(x for x in s.source_cases3b() if x['name']==corrected['name']+'.prior-time')
    for audience in s.SIDES:
        left=s.views3b(corrected,audience);right=s.views3b(retimed,audience)
        assert [s.raw3(x,'Observation3') for x in left]==[s.raw3(x,'Observation3') for x in right]
        assert len({x['context']['configRef'] for x in left})==1
    s.test_a3b_clock_configuration()
    return dict(crossVersionProfileRejects=7,equalPublicHistoryAudiences=2,firstFrameClockConfiguration=1)

def test_capacity_and_compatibility():
    value=build_index()
    for row in value['capacities']:
        assert row['records']>0 and row['maximumBytes']<=row['byteLimit'] and row['maximumDepth']<=row['depthLimit'] and row['maximumArrayItems']<=row['arrayLimit']
    s.test_a3b_preserved()
    rejected(lambda:s.parse3(b' '*65537,'Observation3'))
    rejected(lambda:s.typed3([None]*65,'OwnReceipt3[]'))
    rejected(lambda:b.parse(b' '*16777217,'Checkpoint'))
    rejected(lambda:c.parse(b' '*33554433,'Child'))
    rejected(lambda:p.parse(b' '*8388609,'Parent'))
    return dict(measuredRecords=sum(x['records'] for x in value['capacities']),groups=len(value['capacities']),immutablePredecessorSections=3,boundaryRejects=5)

def test_index_authentication():
    value=build_index();data=raw(value,'Index')
    for mutate in (lambda x:x['artifacts'][0].__setitem__('sha256','sha256:'+'0'*64),lambda x:x['handoff']['traceIds'].pop(),
        lambda x:x['profiles'][0].__setitem__('admission','current-local'),lambda x:x['capacities'][0].__setitem__('maximumBytes',1),
        lambda x:x['readbacks'][0].__setitem__('sha256','sha256:'+'0'*64),lambda x:x['exclusions'].clear()):
        bad=copy.deepcopy(value);mutate(bad);rejected(lambda:read_index(raw(bad,'Index')))
    key=next(iter(INVENTORY['sourcePins']));old=INVENTORY['sourcePins'][key]
    try:INVENTORY['sourcePins'][key]='sha256:'+'0'*64;rejected(lambda:read_index(data))
    finally:INVENTORY['sourcePins'][key]=old
    for bad in (data+b'\n',data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1),data.replace(b'"contractVersion":1',b'"contractVersion":true',1),
                data.replace(b'{"contractVersion":1',b'{"contractVersion":1,"contractVersion":1',1),b'\xff',b' '*2097153,c.encode(dict(reversed(list(value.items()))))):rejected(lambda:parse(bad,'Index'))
    rejected(lambda:typed(['x']*513,'id[]'));rejected(lambda:typed(None,'Index',57));rejected(lambda:typed('\ud800','text'));rejected(lambda:typed('line\nbreak','text'))
    return dict(indexMutations=6,pinBeforeCache=1,rawRejects=7,boundaryRejects=4)

def test_fixture():
    data=FIXTURE.read_bytes();value=read_fixture(data)
    for bad in (data+b' ',data.replace(b'"contractVersion": 1',b'"contractVersion": 1.0',1),data.replace(b'"contractVersion": 1',b'"contractVersion": true',1),data.replace(b'"contractVersion": 1,',b'"contractVersion": 1, "contractVersion": 1,',1)):
        rejected(lambda:read_fixture(bad))
    return dict(bytes=len(data),requirements=len(value['requirements']),readbacks=len(value['readbacks']),rawMutationRejects=4)
TESTS=(test_requirements,test_integrated_readback,test_native_rejections,test_profile_separation,test_capacity_and_compatibility,test_index_authentication)
def main(generate=False):
    started=time.monotonic()
    for test in TESTS:print(test.__name__,test(),round(time.monotonic()-started,3),flush=True)
    if generate:
        try:test_fixture()
        except (FileNotFoundError,ValueError):print('EXPECTED RED missing/stale C fixture',flush=True)
        FIXTURE.write_bytes(fixture_bytes(build_index()))
    print('test_fixture',test_fixture(),flush=True)
    hashes={name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in ('combat-outward-composition-v1.md','combat-outward-composition-v1.schema.json','fixtures/combat-outward-composition-v1.json','verify-combat-outward-composition-v1.py')}
    print('C_FOCUSED_GREEN',json.dumps(dict(seconds=round(time.monotonic()-started,3),hashes=hashes)),flush=True)
if __name__=='__main__':main(generate='--write-fixture' in sys.argv)
