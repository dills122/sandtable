#!/usr/bin/env python3
"""Private, unregistered B2 checkpoint-scoped child execution/proof oracle."""
from __future__ import annotations
import copy,hashlib,importlib.util,json,struct,sys,time,types
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent
FIXTURE=ROOT/'fixtures/combat-exercise-child-evidence-v1.json'
INVENTORY=json.loads((ROOT/'combat-exercise-child-evidence-v1.schema.json').read_bytes())
SCHEMA={k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
spec=importlib.util.spec_from_file_location('child_occurrence',ROOT/'verify-combat-exercise-occurrence-v1.py');b=importlib.util.module_from_spec(spec);spec.loader.exec_module(b)
s=b.side
class Invalid(ValueError):pass
def require(v):
    if not v:raise Invalid('CMB-CHILD-REJECTED')
def encode(v):return json.dumps(v,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(v):return 'sha256:'+hashlib.sha256(v).hexdigest()
def typed(v,t,depth=0):
    require(depth<=48)
    if t.endswith('?'):
        if v is not None:typed(v,t[:-1],depth+1)
    elif t.endswith('[]'):
        require(type(v) is list and len(v)<=1024)
        for x in v:typed(x,t[:-2],depth+1)
    elif t in SCHEMA:
        require(type(v) is dict and set(v)=={k for k,_ in SCHEMA[t]})
        for k,a in SCHEMA[t]:typed(v[k],a,depth+1)
    elif t in INVENTORY['enums']:require(type(v) is str and v in INVENTORY['enums'][t])
    elif t in b.SCHEMA:b.typed(v,t,depth)
    elif t=='utc':require(type(v) is int and 0<=v<=253402300799999)
    else:b.typed(v,t,depth)
def canonical(v,t):
    if t.endswith('?'):return None if v is None else canonical(v,t[:-1])
    if t.endswith('[]'):return [canonical(x,t[:-2]) for x in v]
    if t in SCHEMA:return {k:canonical(v[k],a) for k,a in SCHEMA[t]}
    return b.canonical(v,t)
def raw(v,t):typed(v,t);data=encode(canonical(v,t));require(len(data)<=33554432);return data
def parse(data,t):
    require(type(data) is bytes and len(data)<=33554432)
    def pairs(items):
        v={}
        for k,x in items:require(k not in v);v[k]=x
        return v
    value=json.loads(data.decode('ascii'),object_pairs_hook=pairs,parse_constant=lambda _:require(False));require(raw(value,t)==data);return value
def identity(domain,v,t):return sha(domain.encode()+b'\0'+raw(v,t))
def framed(records):
    require(type(records) is list and len(records)<=1024)
    require(all(type(x) is bytes and len(x)<=2147483647 for x in records))
    return b''.join(struct.pack('>i',len(x))+x for x in records)
def stream_hash(domain,records):return sha(domain.encode()+b'\0'+framed(records))
def pins():
    require(all(sha((ROOT/p).read_bytes())==h for p,h in INVENTORY['sourcePins'].items()));b.source_pins()
def fixture_bytes(v):return (json.dumps(v,ensure_ascii=True,indent=2)+'\n').encode('ascii')
def build_descriptor():
    return dict(contractVersion=1,kind='oracle-source-pins',oracleHash=sha(Path(__file__).read_bytes()),schemaHash=sha((ROOT/'combat-exercise-child-evidence-v1.schema.json').read_bytes()),predecessors=[dict(path=p,hash=h) for p,h in sorted(INVENTORY['sourcePins'].items())])
def build_hash():return identity(INVENTORY['domains']['build'],build_descriptor(),'BuildDescriptor')
def rejected(fn):
    try:fn()
    except (ValueError,KeyError,TypeError,StopIteration,OverflowError,AssertionError):return
    raise AssertionError('invalid child evidence accepted')

# This fixture-time mapping selects configuration before any controller invocation.
def preference_for(source):
    name=source['sourceId']
    return dict(contractVersion=1,retreatChoice='refuse-retreat' if 'refusal-loss-dp' in name else 'retreat',custodyChoice='escape' if 'capture-escape' in name else 'guard',reserveChoice='release-I',cycleChoice='repeat' if name.endswith('.repeat') else 'finish')
def role_for(source):
    ref=b.decode_reference(source)
    if source['reference']['surface'] in ('corrected-v3','fallback-v2'):
        ctx=s.source_context3b(ref) if source['reference']['surface']=='corrected-v3' else ref['base'];actor=ctx['base']['boundary']['cycle']['actingSide']
        seals=[i for i in ref['inputs'] if i['command'].get('kind')=='seal-choice']
        return 'attacker-first' if not seals or seals[0]['actor']==actor else 'defender-first'
    return 'attacker-first'
def source_provenance(source):
    if source['root']['requestJson']:
        request=json.loads(source['root']['requestJson']);content=request['content']
        return dict(setupId=request['setupId'],setupHash=request['setupHash'],contentPackId=content['packId'],contentHash=content['hash'],scenarioId=content['scenarioId'])
    cycle=json.loads(source['root']['boundaryJson'])['cycle']
    return {k:cycle[k] for k in ('setupId','setupHash','contentPackId','contentHash','scenarioId')}
def manifest_for(source,child_id=None):
    pins();b.authenticate_source(source);initial=b.checkpoint(source);ref=b.decode_reference(source)
    request=b.make_request(source,role_for(source))
    return dict(contractVersion=1,childId=child_id or 'child.'+source['sourceId'],evidenceScope='checkpoint-scoped-contract-evidence',confidentiality='private-authority',sourceId=source['sourceId'],
        **source_provenance(source),buildKind='oracle-source-pins',seedKind='initial-rng-state',
        seedHash=sha(b.raw(initial['randomState'],'Random')),buildHash=build_hash(),request=request,controller=preference_for(source),
        trustedSchedule=[dict(actor=i['actor'],admittedAt=i['admittedAt'],clockAvailable=i['clockAvailable']) for i in ref['inputs']],
        fault=dict(kind='none',atTransition=0),expectedFailure='step-limit-exceeded' if source['reference']['surface']=='fallback-v2' else None)
def admit_manifest(manifest,source):
    pins();raw(manifest,'Manifest');b.authenticate_source(source);b.admit_request(b.raw(manifest['request'],'Request'),source)
    require(manifest['sourceId']==source['sourceId']);expected=manifest_for(source,manifest['childId'])
    for field in ('setupId','setupHash','contentPackId','contentHash','scenarioId','seedHash','buildHash','trustedSchedule'):require(manifest[field]==expected[field])
    fault=manifest['fault'];cut=fault['atTransition']
    if fault['kind'] in ('none','reconstruction','readjudication'):require(cut==0)
    else:
        require(cut<min(manifest['request']['maximumAcceptedTransitions'],len(b.decode_reference(source)['events'])))
        if fault['kind']=='controller-invalid':require(any(v['decision'] for v in b.observe(source,cut).values()))
    return copy.deepcopy(manifest)

def controller(view,configuration):
    typed(configuration,'Preferences');require(view['decision'] is not None)
    custody='relocate-and-guard' if configuration['custodyChoice']=='guard' else 'leave-unguarded'
    wanted={'select-close-assault','decline-retreat-before-assault','full-close-assault',configuration['retreatChoice'],custody,configuration['reserveChoice'],configuration['cycleChoice']}
    indexes=[i for i,a in enumerate(view['decision']['actions']) if a['candidate']['kind'] in wanted];require(len(indexes)==1)
    return indexes[0]
def schedule_views(views,order):
    eligible=[a for a in s.SIDES if views[a]['decision'] is not None];dual=len(eligible)==2
    if dual:
        require(all(views[a]['decision']['kind']=='force-assignment' for a in eligible));actor=views[eligible[0]]['context']['cycle']['phasingSide'];other=next(a for a in s.SIDES if a!=actor)
        eligible=[actor,other] if order=='attacker-first' else [other,actor]
    return dict(contractVersion=1,eligibleAudiences=eligible,selectedAudience=eligible[0] if eligible else 'system',dualSlot=dual)
def context(source,ref):return s.source_context3b(ref) if source['reference']['surface']=='corrected-v3' else ref['base']
def start_state(source,ref):
    surface=source['reference']['surface']
    if surface=='historical-v3':return 'terminal',copy.deepcopy(ref['base'])
    if surface=='inherited-v3':return 'reserve',s.irr3.initial(ref['base']['release'])
    return 'steps',s.steps.initial(context(source,ref)['base']['boundary'])
def kernel(source,ref,cut,family,state):
    surface=source['reference']['surface']
    if surface=='inherited-v3':
        if cut==3 and ref['base']['control'] is not None:family='cycle';state=s.icc3.initial(ref['base']['control'])
        return (s.irr3,ref['base']['release'],family,state) if family=='reserve' else (s.icc3,ref['base']['control'],family,state)
    ctx=context(source,ref);a=len(ctx['predecessor']['inputs']);z=a+len(ctx['roundInputs']);r=len(ref['events'])-4
    if cut==a:family='round';state=s.rnd2.initial(ctx['base'])
    if cut==z:family='result';state=s.res2.initial(ctx)
    if surface=='corrected-v3' and cut==r:family='bridge';state=s.bridge3b.initial(ref['base'])
    module,base={'steps':(s.steps,ctx['base']['boundary']),'round':(s.rnd2,ctx['base']),'result':(s.res2,ctx),'bridge':(s.bridge3b,ref['base'])}[family]
    return module,base,family,state

def fresh_views(source,ref,frames,inputs,events):
    surface=source['reference']['surface'];current=copy.deepcopy(ref);current.update(inputs=inputs,events=events)
    if surface=='historical-v3':return {a:s.terminal_view3b(current,a) for a in s.SIDES}
    if surface=='inherited-v3':return {a:s.project_frames3(frames,inputs,events,a,lambda family,state,audience:s.live_facts3(current,family,state,audience))[-1] for a in s.SIDES}
    # Execute the pinned pure projector body with explicit fresh frames. No shared-global mutation,
    # no accepted-side cache, and no retained future event participates in these projections.
    fn=s.full_views3b.__wrapped__ if surface=='corrected-v3' else s.cached_views2.__wrapped__
    env=dict(fn.__globals__)
    if surface=='corrected-v3':
        ctx=context(source,ref);facts_env=dict(s.corrected_facts3b.__globals__);facts_env['source_context3b']=lambda _:ctx
        facts=types.FunctionType(s.corrected_facts3b.__code__,facts_env)
        env.update(source_catalog3b=lambda:[current],full_frames3b=lambda _:frames,source_context3b=lambda _:ctx,corrected_facts3b=facts)
    else:env.update(source_catalog2=lambda:[current],cached_frames2=lambda _n,_c:frames)
    projector=types.FunctionType(fn.__code__,env)
    return {a:(projector(current['name'],a) if surface=='corrected-v3' else projector(current['name'],len(events),a))[-1] for a in s.SIDES}

def proposal_for(source,view,index):
    fn=s.submission2 if source['reference']['surface']=='fallback-v2' else s.submission3 if source['reference']['surface']=='inherited-v3' else s.submission3b
    return fn(view,index)
def public_raw(source,value,kind):return s.raw2(value,kind+'2') if source['reference']['surface']=='fallback-v2' else s.raw3(value,kind+'3')
def public_admit(source,ref,cut,actor,proposal,clock):
    fn=s.admit_a2 if source['reference']['surface']=='fallback-v2' else s.admit_a3 if source['reference']['surface']=='inherited-v3' else s.admit_a3b
    return fn(s.prefix(ref,cut),actor,public_raw(source,proposal,'Submission'),clock['admittedAt'],clock['clockAvailable'])
def owner_input(source,ref,module,base,family,state,actor,proposal,clock):
    kind=proposal['candidate']['kind']
    if family=='steps':
        if kind=='select-close-assault':cmd=s.steps.command(state,'choose-selection',decisionId=state['selectionWindow']['decisionId'],choice=kind,candidate=s.steps.candidate(base))
        else:cmd=s.steps.command(state,'decline-rba',decisionId=state['rbaWindow']['decisionId'],participant=state['selection']['defender']['unit'])
    elif family=='round':cmd=s.rnd2.command(base,state,'seal-choice','attacker' if actor==base['boundary']['cycle']['actingSide'] else 'defender')
    elif family=='result':cmd=s.res2.command(base,state,'choose',kind)
    elif family=='bridge':cmd=s.bridge3b.command(base,state,'cycle',kind)
    elif family=='reserve':cmd=s.rel3.command(state['release'],'choose',kind)
    else:cmd=s.cyc3.command(base['controlBase'],state,kind)
    return dict(command=cmd,actor=actor,admittedAt=clock['admittedAt'],clockAvailable=clock['clockAvailable'])
def native_event(event):
    value=json.loads(event)
    return json.loads(value['nativeEvent']) if 'nativeEvent' in value else value

def system_key(family,event):
    e=native_event(event);inp=e.get('input',{});cmd=inp.get('command',{});effect=e.get('effect',{})
    tags={'steps':s.steps.INVENTORY['effectTags'],'round':s.rnd2.INVENTORY['effectTags'],'result':s.res2.INVENTORY['effectTags'],'reserve':s.rel3.INVENTORY['effectTags'],'cycle':s.cyc3.INVENTORY['effectTags'] if 'effectTags' in s.cyc3.INVENTORY else {'control-opened':None,'cycle-repeated':None,'phase-finished':None}}
    allowed=tags['reserve'] if family=='bridge' and 'releaseId' in e else tags['cycle'] if family=='bridge' else tags[family]
    require(effect.get('kind') in allowed)
    scope=e.get('roundId') if family in ('round','result') else e.get('releaseId') if family=='reserve' or 'releaseId' in e else e.get('controlId') if 'controlId' in e else e.get('segmentId') or e.get('cycleId')
    disposition=None
    if family=='result' and effect['kind'] in ('disposition-recorded','custody-settled'):disposition=effect['payload']['kind']
    elif family in ('reserve','bridge') and effect['kind']=='unit-disposition':disposition=effect['choice']
    return dict(authorityFamily=family,scopeId=scope,decisionId=cmd.get('decisionId'),effectKind=effect.get('kind'),reason=effect.get('reason'),disposition=disposition)
def semantic_action(family,event,proposal,outcome):
    if proposal is not None and outcome['status']=='accepted':return dict(audience=proposal['audience'],actionId=proposal['actionId'])
    return dict(audience='system',actionId=identity(INVENTORY['domains']['system'],system_key(family,event),'SystemKey'))
def outcome_for(source,view,proposal,event,inp):
    v=2 if source['reference']['surface']=='fallback-v2' else 3
    choice=s.successful_choice2(inp,event,proposal['audience']) if v==2 else s.success3(inp,event,proposal['audience']) if source['reference']['surface']=='inherited-v3' else s.native_choice3b(inp,event,proposal['audience'])
    accepted=choice==proposal['candidate']['kind']
    receipt=(s.receipt2(proposal['decisionId'],proposal['actionId']) if v==2 else s.receipt3(view['decision'],next(a for a in view['decision']['actions'] if a['actionId']==proposal['actionId']))) if accepted else None
    return dict(contractVersion=v,status='accepted' if accepted else 'rejected',receipt=receipt)
CHECKS=('query-system','query-axis','query-commonwealth','cardinality','membership','event-cardinality','continuity')
def check(kind,ordinal,passed=True):return dict(kind=kind,ordinal=ordinal,passed=passed)

def execute(manifest,source):
    ref=b.decode_reference(source);family,state=start_state(source,ref);frames=[(family,copy.deepcopy(state))];ins=[];evs=[];steps=[];checks=[];attempt=None;failure=None
    initial=b.checkpoint(source);limit=manifest['request']['maximumAcceptedTransitions'];fault=manifest['fault'];end=len(ref['events'])
    for cut in range(min(limit,end)):
        if fault['kind']=='cancel' and cut==fault['atTransition']:failure='cancelled';break
        views=fresh_views(source,ref,frames,ins,evs);schedule=schedule_views(views,manifest['request']['schedule']['roleOrder']);actor=schedule['selectedAudience'];clock=manifest['trustedSchedule'][cut]
        module,base,family,state=kernel(source,ref,cut,family,state);proposal=outcome=None;view=None
        try:
            require(clock['actor']==actor)
            if actor!='system':
                view=views[actor];proposal=proposal_for(source,view,controller(view,manifest['controller']))
                inp=owner_input(source,ref,module,base,family,state,actor,proposal,clock)
                if fault['kind']=='controller-invalid' and cut==fault['atTransition']:
                    bad=copy.deepcopy(proposal);bad['actionId']=bad['actionId'][:-64]+'0'*64
                    rejected_outcome=public_admit(source,ref,cut,actor,bad,clock);require(rejected_outcome['status']=='rejected' and rejected_outcome['receipt'] is None)
                    attempt=dict(atTransition=cut,initiator=actor,trustedInputJson=b.native(inp),sideProposalJson=public_raw(source,bad,'Submission').decode(),outcomeJson=public_raw(source,rejected_outcome,'Outcome').decode())
                    checks.extend(check(k,cut,k!='membership') for k in CHECKS[:5]);failure='execution-rejected';break
            else:inp=copy.deepcopy(ref['inputs'][cut])
            after,event,receipt=module.transition(base,state,inp)
            require(event is not None and receipt is not None)
            retry,extra,retry_receipt=module.transition(base,after,inp);require(retry==after and extra is None and retry_receipt==receipt)
            require(encode(inp)==encode(ref['inputs'][cut]) and event==ref['events'][cut])
            expected=b.native_state(source['reference']['surface'],ref,cut+1);require(expected==(family,after))
            if proposal is not None:
                outcome=public_admit(source,ref,cut,actor,proposal,clock);require(outcome==outcome_for(source,view,proposal,event,inp))
            cp=b.checkpoint(source,cut+1);e=native_event(event)
            steps.append(dict(contractVersion=1,ordinal=cut,initiator=actor,schedule=schedule,trustedInputJson=b.native(inp),sideProposalJson=public_raw(source,proposal,'Submission').decode() if proposal else None,outcomeJson=public_raw(source,outcome,'Outcome').decode() if outcome else None,
                semanticAction=semantic_action(family,event,proposal,outcome),authorityFamily=family,eventAuthor=e.get('author'),authorityReceiptId=receipt,eventCanonicalJson=event.decode(),successorCheckpointHash=b.checkpoint_hash(cp)))
            checks.extend(check(k,cut) for k in CHECKS);ins.append(inp);evs.append(event);state=after;frames.append((family,copy.deepcopy(state)))
        except (ValueError,KeyError,TypeError,StopIteration):
            failure='execution-rejected';checks.extend(check(k,cut,k!='membership') for k in CHECKS[:5]);break
    final=b.checkpoint(source,len(steps));terminal=b.satisfies(final,manifest['request']['requestedTerminal'])
    if failure is None and not terminal:failure='step-limit-exceeded'
    checks.append(check('terminal',None,failure is None and terminal))
    return dict(steps=steps,checks=checks,failedAttempt=attempt,failure=failure,initial=initial,final=final)

def reconstruct(source,steps):
    # Full B1 source authentication includes the entire creation/synthetic prefix, not a snapshot.
    b.read_source(b.raw(source,'Source'));ref=b.decode_reference(source);family,state=start_state(source,ref)
    for ordinal,step in enumerate(steps):
        require(step['ordinal']==ordinal);module,base,family,state=kernel(source,ref,ordinal,family,state)
        inp=json.loads(step['trustedInputJson']);event=step['eventCanonicalJson'].encode()
        after,actual,receipt=module.transition(base,state,inp)
        require(actual==event and receipt==step['authorityReceiptId']);require((family,after)==b.native_state(source['reference']['surface'],ref,ordinal+1))
        require(b.checkpoint_hash(b.checkpoint(source,ordinal+1))==step['successorCheckpointHash']);state=after
    return b.checkpoint(source,len(steps))
def proof(kind,manifest,source,execution):
    return dict(contractVersion=1,kind=kind,evidenceScope='checkpoint-scoped-contract-evidence',manifestHash=identity(INVENTORY['domains']['manifest'],manifest,'Manifest'),sourceLineageHash=source['sourceLineageHash'],referenceTranscriptHash=source['referenceTranscriptHash'],
        initialCheckpointHash=b.checkpoint_hash(execution['initial']),finalCheckpointHash=b.checkpoint_hash(execution['final']),acceptedTransitions=len(execution['steps']),
        verified=True,failure=None,attemptedTranscriptHash=stream_hash(INVENTORY['domains']['transcript'],[raw(x,'AcceptedStep') for x in execution['steps']]),observedTranscriptHash=stream_hash(INVENTORY['domains']['transcript'],[raw(x,'AcceptedStep') for x in execution['steps']]),observedFinalCheckpointHash=b.checkpoint_hash(execution['final']),transcriptHash=stream_hash(INVENTORY['domains']['transcript'],[raw(x,'AcceptedStep') for x in execution['steps']]),eventHash=stream_hash(INVENTORY['domains']['events'],[x['eventCanonicalJson'].encode() for x in execution['steps']]))
def artifact_inventory(child):
    records=[('run-manifest.json','manifest',raw(child['manifest'],'Manifest')),('result.json','result',raw(child['result'],'Result')),('checks.bin','checks',framed([raw(x,'Check') for x in child['checks']])),('accepted-steps.bin','transcript',framed([raw(x,'AcceptedStep') for x in child['steps']]))]
    for field,path,scheme in (('sourceCanonicalJson','source.json','source'),('initialCheckpointCanonicalJson','initial-checkpoint.json','checkpoint'),('finalCheckpointCanonicalJson','final-checkpoint.json','checkpoint')):
        if child[field] is not None:records.append((path,scheme,child[field].encode()))
    if child['failedAttempt'] is not None:records.append(('failed-attempt.json','attempt',raw(child['failedAttempt'],'FailedAttempt')))
    for field in ('reconstruction','readjudication'):
        if child[field] is not None:records.append((field+'.json',field,raw(child[field],'Proof')))
    return dict(contractVersion=1,entries=[dict(path=p,scheme=scheme,bytes=len(data),hash=sha(data)) for p,scheme,data in records])
def run(manifest,source):
    raw(manifest,'Manifest');rp=ap=None
    try:admit_manifest(manifest,source)
    except (ValueError,KeyError,TypeError,StopIteration):execution=dict(steps=[],checks=[],failedAttempt=None,failure='admission-rejected',initial=None,final=None)
    else:
        execution=execute(manifest,source)
        if execution['failure'] is None:
            try:
                records=copy.deepcopy(execution['steps'])
                if manifest['fault']['kind']=='reconstruction':
                    if records:records[0]['eventCanonicalJson']+=' '
                    else:raise Invalid('injected reconstruction failure')
                require(reconstruct(source,records)==execution['final']);rp=proof('reconstruction',manifest,source,execution);execution['checks'].append(check('reconstruction',None))
            except (ValueError,KeyError,TypeError):
                execution['failure']='reconstruction-failed';execution['checks'].append(check('reconstruction',None,False))
                rp=proof('reconstruction',manifest,source,execution);rp.update(verified=False,failure='reconstruction-failed',attemptedTranscriptHash=stream_hash(INVENTORY['domains']['transcript'],[raw(x,'AcceptedStep') for x in records]),observedTranscriptHash=None,observedFinalCheckpointHash=None)
        if execution['failure'] is None:
            try:
                fresh=execute(manifest,source);comparison=copy.deepcopy(execution['steps'])
                if manifest['fault']['kind']=='readjudication':
                    require(comparison);comparison[0]['semanticAction']['actionId']='injected-mismatch'
                require(raw(fresh['steps'],'AcceptedStep[]')==raw(comparison,'AcceptedStep[]') and fresh['final']==execution['final'])
                ap=proof('readjudication',manifest,source,execution);execution['checks'].append(check('readjudication',None))
            except (ValueError,KeyError,TypeError):
                execution['failure']='readjudication-failed';execution['checks'].append(check('readjudication',None,False))
                ap=proof('readjudication',manifest,source,execution);ap.update(verified=False,failure='readjudication-failed',attemptedTranscriptHash=stream_hash(INVENTORY['domains']['transcript'],[raw(x,'AcceptedStep') for x in comparison]),observedTranscriptHash=stream_hash(INVENTORY['domains']['transcript'],[raw(x,'AcceptedStep') for x in fresh['steps']]),observedFinalCheckpointHash=b.checkpoint_hash(fresh['final']))
    failure=execution['failure'];initial=execution['initial'];final=execution['final']
    result=dict(contractVersion=1,status='succeeded' if failure is None else 'failed',failure=failure,expectedFailureMatch=failure is not None and failure==manifest['expectedFailure'],acceptedTransitions=len(execution['steps']),initialCheckpointHash=b.checkpoint_hash(initial) if initial else None,finalCheckpointHash=b.checkpoint_hash(final) if final else None)
    child=dict(contractVersion=1,manifest=copy.deepcopy(manifest),sourceCanonicalJson=b.raw(source,'Source').decode() if initial else None,initialCheckpointCanonicalJson=b.raw(initial,'Checkpoint').decode() if initial else None,finalCheckpointCanonicalJson=b.raw(final,'Checkpoint').decode() if final else None,
        steps=execution['steps'],failedAttempt=execution['failedAttempt'],checks=execution['checks'],result=result,reconstruction=rp,readjudication=ap,artifactManifest=dict(contractVersion=1,entries=[]))
    child['artifactManifest']=artifact_inventory(child);raw(child,'Child');return child

def validate_child(data,source,expected_manifest=None):
    pins();child=parse(data,'Child');b.authenticate_source(source)
    if expected_manifest is not None:require(raw(child['manifest'],'Manifest')==raw(expected_manifest,'Manifest'))
    require(child['artifactManifest']==artifact_inventory(child))
    expected=run(child['manifest'],source);require(data==raw(expected,'Child'));return copy.deepcopy(child)
def child_hash(child):return identity(INVENTORY['domains']['child'],child,'Child')

def completion_witnesses():
    witnesses=[]
    for source in s.ledger_cases3():
        if source['family']!='ledger-reserve' or source['base']['case']['name']!='later-complete-intent':continue
        owner=source['base']['native']['cycle']['actingSide'];s.validate_ledger3(source)
        views=s.ledger_views3(source,owner)
        for cut,view in enumerate(views[:-1]):
            if not view['decision'] or not any(a['candidate']['kind']=='complete-release' for a in view['decision']['actions']):continue
            index=next(i for i,a in enumerate(view['decision']['actions']) if a['candidate']['kind']=='complete-release');proposal=s.submission3(view,index);current=s.prefix(source,cut);clock=source['inputs'][cut]
            inp,after,event=s.attempt_ledger3(current,owner,proposal,clock['admittedAt'],clock['clockAvailable']);outcome=s.admit_ledger3(current,owner,s.raw3(proposal,'Submission3'),clock['admittedAt'],clock['clockAvailable'])
            require(inp==clock and event==source['events'][cut] and outcome['status']=='accepted' and outcome['receipt'] is not None)
            require(native_event(event)['author']=='system' and native_event(event)['effect']['reason']=='owner-complete-release')
            action=semantic_action('reserve',event,proposal,outcome);require(action==dict(audience=owner,actionId=proposal['actionId']))
            retried=s.admit_ledger3(s.prefix(source,cut+1),owner,s.raw3(proposal,'Submission3'),None,False);require(retried==outcome)
            _,extra,_=s.rel3.transition(source['base']['native'],after,inp);require(extra is None)
            witnesses.append(dict(sourceId=source['name'],scope='isolated-ledger-classifier-only',trustedInputJson=b.native(inp),proposalJson=s.raw3(proposal,'Submission3').decode(),outcomeJson=s.raw3(outcome,'Outcome3').decode(),eventCanonicalJson=event.decode(),semanticAction=action,retryEmitsEvent=False))
    require(len(witnesses)==4);return witnesses

RUNS={}
def test_boundary():
    assert 'run' in globals()
    assert framed([b'a',b'bc'])==b'\0\0\0\1a\0\0\0\2bc'
    assert stream_hash('d',[b'a',b'bc'])!='sha256:'+hashlib.sha256(b'd\0a\nbc\n').hexdigest()
    for v in (True,1.0,-1,2**63):rejected(lambda:typed(v,'count'))
    for bad in (b'{"contractVersion":1,"contractVersion":1}',b'{"contractVersion":true}') :rejected(lambda:parse(bad,'Preferences'))
    descriptor=build_descriptor();assert [p['path'] for p in descriptor['predecessors']]==sorted(INVENTORY['sourcePins'])
    for field in ('oracleHash','schemaHash'):
        mutant=copy.deepcopy(descriptor);mutant[field]='sha256:'+'0'*64;assert identity(INVENTORY['domains']['build'],mutant,'BuildDescriptor')!=build_hash()
    return 10

def test_registry_execution():
    totals=dict(sources=0,succeeded=0,failed=0,steps=0,ownerFallbacks=0,historical=0)
    for source in b.source_cases():
        manifest=manifest_for(source);child=run(manifest,source);RUNS[source['sourceId']]=child;totals['sources']+=1;totals[child['result']['status']]+=1;totals['steps']+=len(child['steps'])
        assert child['result']['failure']==('step-limit-exceeded' if source['reference']['surface']=='fallback-v2' else None),(source['sourceId'],child['result'])
        if source['reference']['surface']=='historical-v3':assert not child['steps'] and child['initialCheckpointCanonicalJson']==child['finalCheckpointCanonicalJson'];totals['historical']+=1
        for step in child['steps']:
            if step['initiator']!='system' and step['semanticAction']['audience']=='system':
                outcome=json.loads(step['outcomeJson']);assert outcome['status']=='rejected' and outcome['receipt'] is None and step['eventAuthor']=='system';totals['ownerFallbacks']+=1
        if totals['sources']%16==0:print('registry progress',totals,flush=True)
    assert totals['sources']==134 and totals['historical']==28 and totals['failed']==8;return totals

def test_failures_and_forgery():
    source=next(x for x in b.source_cases() if x['reference']['surface']=='corrected-v3' and '.fallback' not in x['sourceId']);manifest=manifest_for(source);base=RUNS.get(source['sourceId']) or run(manifest,source);checks=0
    for kind,at,failure in (('cancel',0,'cancelled'),('controller-invalid',1,'execution-rejected'),('reconstruction',0,'reconstruction-failed'),('readjudication',0,'readjudication-failed')):
        m=copy.deepcopy(manifest);m['fault']=dict(kind=kind,atTransition=at);m['expectedFailure']=failure;child=run(m,source);assert child['result']['failure']==failure and child['result']['status']=='failed' and child['result']['expectedFailureMatch'];validate_child(raw(child,'Child'),source,m);RUNS['failure.'+kind]=child;checks+=1
    m=copy.deepcopy(manifest);m['request']['requestedTerminal']['occurrence']['ordinal']+=1;m['expectedFailure']='admission-rejected';child=run(m,source);assert child['result']['failure']=='admission-rejected' and not child['checks'] and child['reconstruction'] is None;RUNS['failure.admission']=child;checks+=1
    for kind,cut in (('none',1),('reconstruction',1),('readjudication',1),('cancel',manifest['request']['maximumAcceptedTransitions']),('controller-invalid',0)):
        m=copy.deepcopy(manifest);m['fault']=dict(kind=kind,atTransition=cut);rejected(lambda:admit_manifest(m,source));checks+=1
    for mutate in (lambda x:x['steps'][0]['semanticAction'].__setitem__('audience','axis'),lambda x:x['checks'].reverse(),lambda x:x['checks'].pop(),lambda x:x['result'].__setitem__('status','failed'),lambda x:x['reconstruction'].__setitem__('eventHash','sha256:'+'0'*64),lambda x:x['artifactManifest']['entries'][0].__setitem__('path','../manifest.json')):
        bad=copy.deepcopy(base);mutate(bad);rejected(lambda:validate_child(raw(bad,'Child'),source,manifest));checks+=1
    data=raw(base,'Child')
    for bad in (data+b'\n',data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1),data.replace(b'"contractVersion":1',b'"contractVersion":true',1)) :rejected(lambda:validate_child(bad,source));checks+=1
    assert validate_child(data,source,manifest)==base;return checks

def test_identity_and_controller():
    source=next(x for x in b.source_cases() if x['reference']['surface']=='corrected-v3');child=RUNS[source['sourceId']];systems=[x for x in child['steps'] if x['semanticAction']['audience']=='system'];assert systems
    step=systems[0];event=step['eventCanonicalJson'].encode();key=system_key(step['authorityFamily'],event);assert set(key)=={'authorityFamily','scopeId','decisionId','effectKind','reason','disposition'}
    mutated=json.loads(event);mutated['input']['admittedAt']=999999;mutated['priorPrefix']='sha256:'+'0'*64
    assert system_key(step['authorityFamily'],encode(mutated))==key
    assert semantic_action(step['authorityFamily'],encode(mutated),None,None)==step['semanticAction']
    views=b.observe(source,1);view=next(v for v in views.values() if v['decision']);config=child['manifest']['controller'];index=controller(view,config)
    other=copy.deepcopy(view);other['own']['spentCp']['numerator']=14;other['own']['cohesion']=-4;assert controller(other,config)==index
    mapped=0
    for child in RUNS.values():
        for step in child['steps']:
            event=step['eventCanonicalJson'].encode();effect=native_event(event)['effect'];family=step['authorityFamily']
            if family=='result' and effect['kind'] in ('disposition-recorded','custody-settled'):
                assert system_key(family,event)['disposition']==effect['payload']['kind'];mapped+=1
            if family in ('reserve','bridge') and effect['kind']=='unit-disposition':assert system_key(family,event)['disposition']==effect['choice'];mapped+=1
    assert mapped>0
    return dict(systemKeyFields=6,nativeDispositionMappings=mapped,completionWitnesses=len(completion_witnesses()))

def generated_fixture():
    require(RUNS)
    summaries=[dict(sourceId=name,childHash=child_hash(child),status=child['result']['status'],failure=child['result']['failure'],steps=len(child['steps']),transcriptHash=child['reconstruction']['transcriptHash'] if child['reconstruction'] else None) for name,child in RUNS.items()]
    selected=[];seen=set()
    for name,child in RUNS.items():
        key=child['result']['failure'] or child['manifest']['request']['requestedTerminal']['closure']
        if key not in seen:seen.add(key);selected.append(dict(name=name,childCanonicalJson=raw(child,'Child').decode()))
    return dict(contractVersion=1,scope='private checkpoint-scoped contract evidence; no runtime registration or disk publication',sourcePins=INVENTORY['sourcePins'],summaries=summaries,goldens=selected,isolatedCompletionWitnesses=completion_witnesses())
def test_fixture():
    expected=fixture_bytes(generated_fixture());require(FIXTURE.read_bytes()==expected);return dict(bytes=len(expected),summaries=len(RUNS))
def main():
    started=time.monotonic()
    for test in (test_boundary,test_registry_execution,test_failures_and_forgery,test_identity_and_controller,test_fixture):print(test.__name__,test(),round(time.monotonic()-started,3),flush=True)
    print('B2 PASS 5 groups',round(time.monotonic()-started,3),flush=True)
if __name__=='__main__':main()
