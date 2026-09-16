#!/usr/bin/env python3
"""B1 replay-backed occurrence/checkpoint contract. No runtime registration."""
from __future__ import annotations
import copy, hashlib, importlib.util, json, sys, time
from functools import lru_cache
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent
FIXTURE=ROOT/'fixtures/combat-exercise-occurrence-v1.json'
INVENTORY=json.loads((ROOT/'combat-exercise-occurrence-v1.schema.json').read_bytes())
SCHEMA={k:[tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}
sp=importlib.util.spec_from_file_location('occurrence_side',ROOT/'verify-combat-side-projection-v1.py')
side=importlib.util.module_from_spec(sp);sp.loader.exec_module(side)
c=side.composition3b

class Invalid(ValueError):pass
def require(value):
    if not value:raise Invalid('CMB-OCC-REJECTED')
def encode(value):return json.dumps(value,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(data):return 'sha256:'+hashlib.sha256(data).hexdigest()
def digest(domain,value,kind=None):return sha(domain.encode()+b'\0'+(raw(value,kind) if kind else encode(value)))
def native(value):
    if type(value) is bytes:return value.decode('ascii')
    if type(value) is str:return value
    return encode(value).decode('ascii')
def cases(module):return json.loads(module.FIXTURE.read_bytes())['cases']
def native_raw(module,value,kind):
    if hasattr(module,'raw'):return module.raw(value,kind)
    module.typed(value,kind);return module.encode(module.canonical(value,kind))
def typed(v,t,depth=0):
    require(depth<=40)
    if t.endswith('?'):
        if v is not None:typed(v,t[:-1],depth+1)
    elif t.endswith('[]'):
        require(type(v) is list and len(v)<=256)
        for x in v:typed(x,t[:-2],depth+1)
    elif t in SCHEMA:
        require(type(v) is dict and set(v)=={k for k,_ in SCHEMA[t]})
        for k,a in SCHEMA[t]:typed(v[k],a,depth+1)
    elif t in INVENTORY['enums']:require(type(v) is str and v in INVENTORY['enums'][t])
    elif t in ('World','Random'):c.CORE.typed(v,t,depth)
    elif t in ('count','positive','version'):require(type(v) is int and (v==1 if t=='version' else (1 if t=='positive' else 0)<=v<2**63))
    elif t=='bool':require(type(v) is bool)
    elif t in ('hash','rawHash'):
        require(type(v) is str);s=v[7:] if t=='hash' and v.startswith('sha256:') else v if t=='rawHash' else ''
        require(len(s)==64 and all(x in '0123456789abcdef' for x in s))
    elif t in ('ascii','id'):require(type(v) is str and v.isascii() and 0<len(v)<= (16777216 if t=='ascii' else 512) and (t!='id' or all(ord(x)>=32 for x in v)))
    else:require(False)
def canonical(v,t):
    if t.endswith('?'):return None if v is None else canonical(v,t[:-1])
    if t.endswith('[]'):return [canonical(x,t[:-2]) for x in v]
    if t in SCHEMA:return {k:canonical(v[k],a) for k,a in SCHEMA[t]}
    if t in ('World','Random'):return c.CORE.canonical(v,t)
    return v
def raw(v,t):
    typed(v,t);b=encode(canonical(v,t));require(len(b)<=16777216);return b
def parse(data,t):
    require(type(data) is bytes and 0<len(data)<=16777216)
    def pairs(items):
        v={}
        for k,x in items:require(k not in v);v[k]=x
        return v
    try:v=json.loads(data.decode('ascii'),object_pairs_hook=pairs,parse_constant=lambda _:require(False))
    except (UnicodeError,RecursionError) as e:raise Invalid('CMB-OCC-REJECTED') from e
    require(raw(v,t)==data);return v

def fragment(kind,events=(),inputs=None,base=None,terminal=None,proof=None):
    ev=[native(e) for e in events]
    ins=[native(i) for i in inputs] if inputs is not None else [native(json.loads(e)['input']) for e in ev]
    require(len(ins)==len(ev))
    return dict(kind=kind,baseJson=None if base is None else native(base),proofJson=None if proof is None else native(proof),inputs=ins,events=ev,terminalJson=None if terminal is None else native(terminal))

def creation(h):
    rd=c.reserve_cycle.rd;s=rd.replay(*h[:6]);require(rd.read_state(rd.raw(s,'ReserveState'),*h[:6])==s)
    root=dict(kind='creation-history',requestJson=native(rd.pre.env.encode(rd.pre.env.canonical(h[0],'Request'))),createdJson=native(h[1]),boundaryJson=None)
    fragments=[fragment(k,ev) for k,ev in zip(('preamble','weather','stage-entry','reserve-designation'),h[2:6])]
    fragments[-1]['terminalJson']=native(rd.raw(s,'ReserveState'))
    return root,fragments

@lru_cache(maxsize=2)
def release_chain(actor):
    m=c.reserve_release;case=next(x for x in cases(m) if x['actor']==actor);result=m.trace(case)
    base,parent,states,inputs,events=result;h,_,pstates,pi,pe=parent
    root,frags=creation(h)
    require(c.reserve_cycle.read_control(c.reserve_cycle.raw(pstates[-1],'Control'),h,pe)==pstates[-1])
    frags.append(fragment('reserve-cycle',pe,pi,terminal=c.reserve_cycle.raw(pstates[-1],'Control')))
    prefix=copy.deepcopy(frags)
    m.read_base(m.raw(base,'Base'),case);end=m.read_control(m.raw(states[-1],'Control'),base,inputs,events)
    frags.append(fragment('reserve-release',events,inputs,m.raw(base,'Base'),m.raw(end,'Control')))
    return result,root,frags,prefix

@lru_cache(maxsize=4)
def cycle_chain(actor,action):
    result,root,frags,_=release_chain(actor);m=c.cycle_control;base,states,inputs,events=m.trace(actor,action)
    bundle=m.read_base(m.raw(base,'InheritedCycleControlBase'));require(bundle[1]==result)
    proof=c.armed.read_proof(c.armed.raw(bundle[2],'Proof'),actor)
    frags=copy.deepcopy(frags)+[fragment('armed-proof',proof=c.armed.raw(proof,'Proof'))]
    end=m.read_state(m.cyc.raw(states[-1],'CycleControlState'),base,inputs,events)
    frags.append(fragment('cycle-control',events,inputs,m.raw(base,'InheritedCycleControlBase'),m.cyc.raw(end,'CycleControlState')))
    return (base,states,inputs,events),root,frags

@lru_cache(maxsize=1)
def historical_material():
    material={}
    def add(item,root,frags,end,world,rng,cycle):
        require(end==item['terminal'] and world==item['world'] and rng==item['randomState'] and cycle==item['cycle'])
        key=item['contract']+':'+item['caseId'];material[key]=dict(root=root,fragments=frags,item=item)
    for case,item in zip(cases(c.noattack),c.noattack_sources()):
        m=c.noattack;h,se,_,states,inputs,events=m.trace(case);root,frags=creation(h)
        frags += [fragment(k,ev) for k,ev in zip(('movement','movement-lifecycle','breakdown-entry'),h[6:])]
        frags.append(fragment('selection',se));end=m.read_control(m.raw(states[-1],'Control'),h,se,events)
        frags.append(fragment('noattack',events,inputs,terminal=m.raw(end,'Control')));entry=end['selection']['boundary']['entry']
        add(item,root,frags,end,entry['world'],entry['randomState'],entry['cycle'])
    for case,item in zip(cases(c.direct),c.direct_sources()):
        m=c.direct;args,inp,ev,_,end=m.trace(case);root,frags=creation(args)
        frags += [fragment(k,e) for k,e in zip(('phasing-movement','reaction-trigger'),args[6:])]
        end=m.read_state(m.irl.raw(end,'LifecycleState'),*args,[ev]);frags.append(fragment('reaction-close',[ev],[inp],terminal=m.irl.raw(end,'LifecycleState')))
        add(item,root,frags,end,end['world'],end['randomState'],end['cycle'])
    for case,item in zip(cases(c.fallback),c.fallback_sources()):
        m=c.fallback;args,inputs,events,states=m.trace(case);root,frags=creation(args)
        frags += [fragment(k,e) for k,e in zip(('phasing-movement','reaction-trigger','participant-start'),args[6:])]
        end=m.read_state(m.raw(states[-1],'FallbackState'),*args,events);frags.append(fragment('reaction-fallback',events,inputs,terminal=m.raw(end,'FallbackState')))
        add(item,root,frags,end,end['world'],end['randomState'],end['cycle'])
    for case,item in zip(cases(c.reaction_complete),c.reaction_completion_sources()):
        m=c.reaction_complete;pc=next(p for p in cases(m.irs) if p['name']==case['predecessorCase']);args,pi,pe,ps=m.irs.trace(pc)
        before=m.irs.read_state(m.irs.raw(ps[-1],m.irs.STATE),*args,pe);prior,inputs,events,states=m.trace(case)
        require(prior==pe[-1] and m.irl.raw(states[0],'LifecycleState')==m.irs.raw(before,m.irs.STATE))
        root,frags=creation(args);frags += [fragment(k,e) for k,e in zip(('phasing-movement','reaction-trigger','participant-start'),args[6:])]
        frags.append(fragment('reaction-second-move',pe,pi,base=native(pc),terminal=m.irs.raw(before,m.irs.STATE)))
        end=m.read_state(m.irl.raw(states[-1],'LifecycleState'),case,events);frags.append(fragment('reaction-completion',events,inputs,base=native(case),terminal=m.irl.raw(end,'LifecycleState')))
        add(item,root,frags,end,end['world'],end['randomState'],end['cycle'])
    for case,item in zip(cases(c.reserve_cycle),c.reserve_cycle_sources()):
        m=c.reserve_cycle;h,_,states,inputs,events=m.trace(case);root,frags=creation(h)
        end=m.read_control(m.raw(states[-1],'Control'),h,events);frags.append(fragment('reserve-cycle',events,inputs,terminal=m.raw(end,'Control')))
        add(item,root,frags,end,end['base']['world'],end['base']['randomState'],end['base']['cycle'])
    for item in c.reserve_release_sources():
        result,root,frags,_=release_chain(item['actor']);base,parent,states,_,_=result;end=states[-1]
        world=c.reserve_release.release.project_world(parent[2][-1]['base']['world'],base['releaseBase'],end['release'])
        add(item,root,frags,end,world,end['release']['randomState'],base['releaseBase']['cycle'])
    for item in c.armed_sources():
        result,root,frags,_=release_chain(item['actor']);proof=c.armed.proof_from(item['actor'],result);end=c.armed.read_proof(c.armed.raw(proof,'Proof'),item['actor'])
        frags=copy.deepcopy(frags)+[fragment('armed-proof',proof=c.armed.raw(end,'Proof'))]
        world=c.reserve_release.release.project_world(result[1][2][-1]['base']['world'],result[0]['releaseBase'],result[2][-1]['release'])
        add(item,root,frags,end,world,result[2][-1]['release']['randomState'],end['currentCycle'])
    for item in c.cycle_sources():
        result,root,frags=cycle_chain(item['actor'],item['caseId'].rsplit('-',1)[1]);base,states,_,_=result;end=states[-1]
        add(item,root,frags,end,end['world'],end['randomState'],dict(sourceCycle=base['controlBase']['releaseBase']['cycle'],activeCycle=end['activeCycle']))
    for item in c.reserve_movement_sources():
        actor=item['actor'];cres,root,frags=cycle_chain(actor,'repeat');m=c.reserve_movement;pm=m.previous;mb,ms,mi,me=pm.trace(actor)
        pm.read_base(pm.raw(mb,'InheritedReserveMovementBase'));require(mb['predecessorBase']==cres[0] and mb['predecessorInputs']==cres[2] and mb['predecessorEvents']==[json.loads(e) for e in cres[3]])
        mend=pm.read_state(pm.raw(ms[-1],'InheritedReserveMovementState'),mb,mi,me)
        frags=copy.deepcopy(frags)+[fragment('reserve-movement',me,mi,pm.raw(mb,'InheritedReserveMovementBase'),pm.raw(mend,'InheritedReserveMovementState'))]
        base,states,inputs,events=m.trace(actor);m.read_base(m.raw(base,'InheritedReserveMovementCompletionBase'))
        require(base['predecessorBase']==mb and base['predecessorInputs']==mi and base['predecessorEvents']==[json.loads(e) for e in me])
        end=m.read_state(m.raw(states[-1],'InheritedReserveMovementCompletionState'),base,inputs,events)
        frags.append(fragment('reserve-movement-completion',events,inputs,m.raw(base,'InheritedReserveMovementCompletionBase'),m.raw(end,'InheritedReserveMovementCompletionState')))
        add(item,root,frags,end,end['world'],end['randomState'],end['cycle'])
    require(len(material)==28 and sum(len(f['events']) for x in material.values() for f in x['fragments'])==588)
    evidence=side.composition_evidence3b();require(set(material)==set(evidence['handoff']['selectedTraceIds']))
    return material


def reference_json(source):
    value=copy.deepcopy(source);value['events']=[native(e) for e in source['events']]
    if source['family']=='settlement-v2':value['base']['roundEvents']=[native(e) for e in source['base']['roundEvents']]
    return native(value)

def context_json(ctx):
    # ctx has already passed the pinned native source reader. Preserve exact closed encoding.
    return encode(dict(base=ctx['base'],predecessor=ctx['predecessor'],roundInputs=ctx['roundInputs'],roundEvents=[native(e) for e in ctx['roundEvents']],committed=ctx['committed']))
def decode_reference(source):
    s=json.loads(source['reference']['canonicalJson']);s['events']=[e.encode('ascii') for e in s['events']]
    if source['reference']['surface']=='fallback-v2':s['base']['roundEvents']=[e.encode('ascii') for e in s['base']['roundEvents']]
    return s

def reference_value(surface,data):
    ref=json.loads(data);ref['events']=[e.encode('ascii') for e in ref['events']]
    if surface=='fallback-v2':ref['base']['roundEvents']=[e.encode('ascii') for e in ref['base']['roundEvents']]
    return ref

@lru_cache(maxsize=256)
def _native_frames_full(surface,data):
    ref=reference_value(surface,data)
    return side.replay_frames3b(ref) if surface in ('corrected-v3','historical-v3') else side.replay_frames2(ref) if surface=='fallback-v2' else side.replay_frames3(ref)
def native_frames(surface,ref,cut=None):
    source_pins();frames=_native_frames_full(surface,reference_json(ref))
    limit=len(frames)-1 if cut is None else cut;require(type(limit) is int and 0<=limit<len(frames))
    return copy.deepcopy(frames[:limit+1])
def native_state(surface,ref,cut):
    source_pins();frames=_native_frames_full(surface,reference_json(ref));require(type(cut) is int and 0<=cut<len(frames))
    return copy.deepcopy(frames[cut])

@lru_cache(maxsize=256)
def _observations_full(surface,data):
    ref=reference_value(surface,data)
    fn=side.views3b if surface in ('corrected-v3','historical-v3') else side.views2 if surface=='fallback-v2' else side.views3
    return {a:fn(ref,a) for a in side.SIDES}
def observations(surface,ref,cut=None):
    source_pins();views=_observations_full(surface,reference_json(ref));limit=len(ref['events']) if cut is None else cut
    require(type(limit) is int and 0<=limit<len(views['axis']))
    return {a:copy.deepcopy(views[a][limit]) for a in side.SIDES}
def native_control(surface,ref,cut):
    family,state=native_state(surface,ref,cut)
    if surface=='historical-v3':
        item=historical_material()[ref['base']['traceId']]['item']
        tags={'ordinary-no-attack':'historical-noattack','reaction-direct-close':'historical-direct','reaction-active-fallback':'historical-fallback','reaction-second-move-completion':'historical-reaction-completion','reserve-cycle-entry':'historical-reserve-cycle','reserve-release':'historical-reserve-release','armed-continuation':'historical-armed','cycle-control':'historical-cycle','reserve-second-movement-completion':'historical-reserve-movement'}
        return dict(kind=tags[item['family']],canonicalJson=native(item['terminalData']))
    module,kind,tag=({'steps':(side.steps,'Control','steps-v2'),'round':(side.rnd2,'RoundState','round-v2'),'result':(side.res2,'ResultState','result-v2'),'bridge':(side.bridge3b,'BridgeState','result-cycle-bridge'),'reserve':(side.irr3,'Control','live-reserve'),'cycle':(side.cyc3,'CycleControlState','live-cycle')})[family]
    return dict(kind=tag,canonicalJson=native(native_raw(module,state,kind)))


def prefix_seed(source):
    cursor=source['executionStart'];frags=copy.deepcopy(source['fragments'][:cursor['fragmentIndex']+1]);f=frags[-1]
    require(f['kind']==cursor['fragmentKind'] and cursor['localCut']<=len(f['events']))
    f['inputs']=f['inputs'][:cursor['localCut']];f['events']=f['events'][:cursor['localCut']]
    f['terminalJson']=native_control(source['reference']['surface'],decode_reference(source),0)['canonicalJson']
    # This current native base is prefix-only; later fragment bases/proofs never participate.
    return dict(lineageKind=source['lineageKind'],executionProfile=source['executionProfile'],sourceSurface=source['reference']['surface'],sourceFamily=decode_reference(source)['family'],root=source['root'],fragments=frags,executionStart=cursor)

def finish_source(source):
    source['sourceLineageHash']=digest(INVENTORY['domains']['source'],prefix_seed(source),'SourcePrefixSeed')
    material={k:v for k,v in source.items() if k not in ('sourceLineageHash','referenceTranscriptHash')}
    source['referenceTranscriptHash']=digest(INVENTORY['domains']['reference'],material,'ReferenceSeed')
    raw(source,'Source');return source

@lru_cache(maxsize=1)
def catalog():
    rows=[];historical=historical_material();handoff=sha(encode(side.composition_evidence3b()['handoff']))
    refs=[('corrected-v3',s) for s in side.source_cases3b() if s['family']=='corrected-composition']
    refs += [('fallback-v2',s) for s in side.source_cases2() if '.fallback-' in s['name']]
    refs += [('inherited-v3',s) for s in side.source_cases3()]
    refs += [('historical-v3',s) for s in side.source_cases3b() if s['family']=='historical-terminal']
    for surface,ref in refs:
        if surface=='historical-v3':
            h=historical[ref['base']['traceId']];root=copy.deepcopy(h['root']);frags=copy.deepcopy(h['fragments']);start=dict(fragmentIndex=len(frags)-1,fragmentKind=frags[-1]['kind'],localCut=len(frags[-1]['events']))
            lineage='creation-rooted-handoff';profile='historical-terminal-checkpoint'
        elif surface=='inherited-v3':
            _,root,_,prefix=release_chain(ref['base']['case']['actor']);root=copy.deepcopy(root);frags=copy.deepcopy(prefix)
            start=dict(fragmentIndex=len(frags),fragmentKind='reserve-release',localCut=0)
            frags.append(fragment('reserve-release',ref['events'][:3],ref['inputs'][:3],side.irr3.raw(ref['base']['release'],'Base'),native_control(surface,ref,3)['canonicalJson']))
            if len(ref['events'])>3:frags.append(fragment('cycle-control',ref['events'][3:],ref['inputs'][3:],side.icc3.raw(ref['base']['control'],'InheritedCycleControlBase'),native_control(surface,ref,len(ref['events']))['canonicalJson']))
            lineage='creation-rooted-handoff';profile='live-inherited-release-control'
        else:
            ctx=side.source_context3b(ref) if surface=='corrected-v3' else ref['base'];boundary=ctx['base']['boundary']
            root=dict(kind='synthetic-boundary',requestJson=None,createdJson=None,boundaryJson=native(native_raw(side.steps,boundary,'Boundary')))
            a=len(ctx['predecessor']['inputs']);b=a+len(ctx['roundInputs']);end=len(ref['events']);r=end-4 if surface=='corrected-v3' else end
            frags=[]
            for lo,hi,tag,base in ((0,a,'steps-v2',native_raw(side.steps,boundary,'Boundary')),(a,b,'round-v2',side.rnd2.raw(ctx['base'],'Base')),(b,r,'result-v2',context_json(ctx))):
                frags.append(fragment(tag,ref['events'][lo:hi],ref['inputs'][lo:hi],base,native_control(surface,ref,hi)['canonicalJson']))
            if surface=='corrected-v3':frags.append(fragment('result-cycle-bridge',ref['events'][r:],ref['inputs'][r:],side.bridge3b.raw(ref['base'],'BridgeBase'),native_control(surface,ref,end)['canonicalJson']))
            start=dict(fragmentIndex=0,fragmentKind='steps-v2',localCut=0);lineage='synthetic-combat';profile='native-c3-through-bridge'
        s=dict(contractVersion=1,sourceId=ref['name'],lineageKind=lineage,executionProfile=profile,root=root,fragments=frags,
            reference=dict(surface=surface,sourceName=ref['name'],canonicalJson=reference_json(ref)),executionStart=start,sourceLineageHash='',referenceTranscriptHash='',task003HandoffHash=handoff if lineage=='creation-rooted-handoff' else None)
        rows.append(finish_source(s))
    require(len(rows)==134);return rows

# Pins are literal accepted dependencies; each public read checks before cached catalog access.
PINS={'combat-side-projection-v1.schema.json': 'sha256:fbca44fef91b806906b3287bb851e5efb92e735876af7a6b755d2873f625b664', 'fixtures/combat-side-projection-v1.json': 'sha256:07139da2ff125345c3007aa6bd9a5760eec44e4d4f25d0ec85c7e06fed4da44f', 'verify-combat-side-projection-v1.py': 'sha256:964f4b6a6901e446e5d0e0a6c49e79e778677ae458947681034a4ddd5d848a0d', 'combat-authority-composition-v1.schema.json': 'sha256:47210ccd00d3eba301c97d2190c16354626265707a4f6109754150e30406bd42', 'fixtures/combat-authority-composition-v1.json': 'sha256:a90f88de9c05b4b1bd7081e7828abb7553e3e0537a6efa8cad042c87a6df50b1', 'verify-combat-authority-composition-v1.py': 'sha256:97f9500f5b0566c9f8b295a1a7d524fdf53387038655872ea0d8005abe8702b7', 'combat-result-cycle-finish-v1.schema.json': 'sha256:cbd61da864d102a08389a166486cd0cf5728a0db5feae002e3c8fb177b52d2d5', 'fixtures/combat-result-cycle-finish-v1.json': 'sha256:a1b6cb5f611586da62667fd4315365aa3a38c34af8afe016314eba4c7c1057c2', 'verify-combat-result-cycle-finish-v1.py': 'sha256:9eb64ccaf6788b60f04662f9569bac23265816d8fba86bdd8078eae27eeac4a9'}
def source_pins():
    require(PINS and all(sha((ROOT/p).read_bytes())==h for p,h in PINS.items()))
    side.source_pins3b();side.source_pins3();return copy.deepcopy(PINS)
def source_cases():source_pins();return copy.deepcopy(catalog())
def authenticate_source(source):
    source_pins();raw(source,'Source');expected=next((x for x in catalog() if x['sourceId']==source['sourceId']),None)
    require(expected is not None and raw(source,'Source')==raw(expected,'Source'));return copy.deepcopy(expected)

def occurrence(cycle):return {k:cycle[k] for k in ('campaignId','gameTurn','operationStage','playerPhaseSlot','actingSide','ordinal')}
def position_id(value):
    if type(value) is str:return value
    return value['sequencePosition']['positionId'] if 'sequencePosition' in value else value['positionId']

def checkpoint_fields(source,cut):
    surface=source['reference']['surface'];ref=decode_reference(source);require(type(cut) is int and 0<=cut<=len(ref['events']))
    current=side.prefix(ref,cut);family,state=native_state(surface,ref,cut);control=native_control(surface,ref,cut)
    obs=observations(surface,ref,cut);view=obs['axis'];closure='open'
    if surface=='historical-v3':
        item=historical_material()[ref['base']['traceId']]['item'];cycles=item['cycle'];cycle=cycles.get('sourceCycle',cycles);active=cycles.get('activeCycle',cycle)
        world=item['world'];rng=item['randomState'];version=item['stateVersion'];prefix=item['prefix'];position=position_id(item['position'])
        closure={'ordinary-no-attack':'entry','reaction-direct-close':'resumed','reaction-active-fallback':'resumed','reaction-second-move-completion':'resumed','reserve-cycle-entry':'entry','reserve-release':'completed','armed-continuation':'proof-only','cycle-control':'entry' if active is not None else 'phase-finished','reserve-second-movement-completion':'completed'}[item['family']]
    elif surface=='inherited-v3':
        rb=ref['base']['release']['releaseBase'];cycle=rb['cycle'];state_body=state['release'] if family=='reserve' else state
        world=side.rel3.project_world(side.inherited_world3(ref['base']['case']['actor']),rb,state_body) if family=='reserve' else state['world']
        rng=state_body['randomState'];version=state_body['stateVersion'];prefix=state_body['prefix'];active=state_body.get('activeCycle',cycle);position=view['positionId']
        if family=='reserve' and state_body['status']=='completed':closure='completed'
        elif family=='cycle' and state_body['status']=='finished':closure='phase-finished'
        elif family=='cycle' and state_body['status']=='repeated':closure='entry'
    else:
        ctx=side.source_context3b(ref) if surface=='corrected-v3' else ref['base'];boundary=ctx['base']['boundary'];cycle=boundary['cycle'];active=cycle
        state_body=json.loads(state['kernelState']) if family=='bridge' else state
        retained=json.loads(ref['base']['source']['resultState']) if family=='bridge' else boundary
        world=state_body.get('world',retained['world']);rng=state_body.get('randomState',retained['randomState']);version=state_body['stateVersion'];prefix=state_body['prefix'];position=view['positionId']
        if family=='result' and state['closed']:closure='completed'
        if family=='bridge' and state['phase']=='finished':active=None;closure='phase-finished'
    config=cycle.get('admittedPolicyBundleDigest') or cycle.get('configurationHash')
    require(config is not None)
    return dict(contractVersion=1,lineageKind=source['lineageKind'],executionProfile=source['executionProfile'],sourceLineageHash=source['sourceLineageHash'],executionStart=copy.deepcopy(source['executionStart']),executedTransitions=cut,
        executionPrefixHash=digest(INVENTORY['domains']['execution'],dict(inputs=[native(i) for i in current['inputs']],events=[native(e) for e in current['events']]),'ExecutionSeed'),
        sourceOccurrence=occurrence(cycle),activeOccurrence=occurrence(active) if active is not None else None,positionId=position,closure=closure,
        authorityVersion=version,authorityPrefix=prefix,rulesetHash=cycle['rulesetHash'],configurationHash=config,world=copy.deepcopy(world),randomState=copy.deepcopy(rng),control=control)

@lru_cache(maxsize=4096)
def cached_checkpoint(name,cut):
    source=next(s for s in catalog() if s['sourceId']==name);cp=checkpoint_fields(source,cut);raw(cp,'Checkpoint');return cp

def checkpoint(source,cut=0):authenticate_source(source);return copy.deepcopy(cached_checkpoint(source['sourceId'],cut))
def checkpoint_hash(cp):return digest(INVENTORY['domains']['checkpoint'],canonical(cp,'Checkpoint'))
def read_source(data):
    s=parse(data,'Source');authenticate_source(s);return s
def read_checkpoint(data,source):
    cp=parse(data,'Checkpoint');require(data==raw(checkpoint(source,cp['executedTransitions']),'Checkpoint'));return cp

def supported_terminal(source):
    ref=decode_reference(source)
    if source['reference']['surface']=='fallback-v2':
        name=ref['name'].rsplit('.fallback-',1)[0];full=next(x for x in side.source_cases2() if x['name']==name);state=side.replay_frames2(full)[-1][1];view=side.views2(full,'axis')[-1]
        cycle=full['base']['base']['boundary']['cycle'];require(state['closed']);occ=occurrence(cycle);position=view['positionId'];closure='completed'
    else:
        cp=checkpoint(source,len(ref['events']));occ=cp['activeOccurrence'] or cp['sourceOccurrence'];position=cp['positionId'];closure=cp['closure']
    return dict(occurrence=copy.deepcopy(occ),positionId=position,closure=closure,futureObligations='retain-without-maturity')

def make_request(source,role_order='attacker-first'):
    cp=checkpoint(source);ref=decode_reference(source)
    return dict(contractVersion=1,sourceLineageHash=source['sourceLineageHash'],referenceTranscriptHash=source['referenceTranscriptHash'],executionStart=copy.deepcopy(source['executionStart']),initialCheckpointHash=checkpoint_hash(cp),initialOccurrence=cp['sourceOccurrence'],rulesetHash=cp['rulesetHash'],configurationHash=cp['configurationHash'],requestedTerminal=supported_terminal(source),schedule=dict(contractVersion=1,roleOrder=role_order,clockPolicy='native-retained-trusted-clock.v1'),maximumAcceptedTransitions=len(ref['events']))
def admit_request(data,source):
    request=parse(data,'Request');authenticate_source(source);require(request['maximumAcceptedTransitions']<=64)
    expected=make_request(source,request['schedule']['roleOrder']);expected['maximumAcceptedTransitions']=request['maximumAcceptedTransitions'];require(raw(request,'Request')==raw(expected,'Request'))
    if source['executionProfile']=='historical-terminal-checkpoint':require(request['maximumAcceptedTransitions']==0 and satisfies(checkpoint(source),request['requestedTerminal']))
    return request

def satisfies(cp,terminal):
    typed(terminal,'Terminal');actual=cp['activeOccurrence'] or cp['sourceOccurrence']
    return actual==terminal['occurrence'] and cp['positionId']==terminal['positionId'] and cp['closure']==terminal['closure'] and terminal['futureObligations']=='retain-without-maturity'

def observe(source,cut):
    authenticate_source(source);ref=decode_reference(source);return observations(source['reference']['surface'],ref,cut)
def schedule(source,cut,configuration):
    typed(configuration,'Schedule');views=observe(source,cut);eligible=[a for a in side.SIDES if views[a]['decision'] is not None]
    dual=len(eligible)==2
    if dual:
        require(all(views[a]['decision']['kind']=='force-assignment' and views[a]['roundRef'] is not None for a in eligible))
        require(views[eligible[0]]['context']['cycle']==views[eligible[1]]['context']['cycle'] and views[eligible[0]]['positionId']==views[eligible[1]]['positionId'])
        actor=views[eligible[0]]['context']['cycle']['phasingSide'];defender=next(a for a in side.SIDES if a!=actor)
        eligible=[actor,defender] if configuration['roleOrder']=='attacker-first' else [defender,actor]
    result=dict(contractVersion=1,eligibleAudiences=eligible,selectedAudience=eligible[0] if eligible else 'system',dualSlot=dual);raw(result,'ScheduleDecision');return result

def continue_from(source,data,inputs,events):
    cp=read_checkpoint(data,source);ref=decode_reference(source);start=cp['executedTransitions'];end=start+len(events)
    require(type(inputs) is list and type(events) is list and len(inputs)==len(events) and end<=len(ref['events']))
    require(encode(inputs)==encode(ref['inputs'][start:end]) and events==ref['events'][start:end])
    return checkpoint(source,end)


def rejected(fn):
    try:fn()
    except (ValueError,KeyError,TypeError,StopIteration,OverflowError):return
    raise AssertionError('invalid input accepted')

def test_boundary():
    assert 'source_cases' in globals()
    literal=b'{"contractVersion":1,"roleOrder":"attacker-first","clockPolicy":"native-retained-trusted-clock.v1"}'
    assert raw(dict(contractVersion=1,roleOrder='attacker-first',clockPolicy='native-retained-trusted-clock.v1'),'Schedule')==literal
    for value in (True,1.0,-1,2**63):rejected(lambda:typed(value,'count'))
    rejected(lambda:typed(['x']*257,'ascii[]'));rejected(lambda:typed('x'*16777217,'ascii'))

def test_sources():
    rows=source_cases();assert len(rows)==134;counts={k:sum(s['reference']['surface']==k for s in rows) for k in INVENTORY['enums']['surface']}
    assert counts=={'corrected-v3':88,'fallback-v2':8,'inherited-v3':10,'historical-v3':28}
    h=historical_material();assert sum(len(f['events']) for x in h.values() for f in x['fragments'])==588
    for s in rows:assert read_source(raw(s,'Source'))==s
    return counts

def test_terminals():
    counts=dict(success=0,fallback=0,historical=0);maximum=0
    for s in source_cases():
        request=make_request(s);assert admit_request(raw(request,'Request'),s)==request
        ref=decode_reference(s);initial=checkpoint(s);final=checkpoint(s,len(ref['events']));maximum=max(maximum,len(raw(final,'Checkpoint')))
        if s['reference']['surface']=='fallback-v2':
            assert not satisfies(final,request['requestedTerminal']);assert len(ref['events'])==(16 if s['sourceId'].endswith('fallback-retreat') else 20);assert final['closure']=='open';counts['fallback']+=1
        else:assert satisfies(final,request['requestedTerminal']);counts['success']+=1
        if s['executionProfile']=='historical-terminal-checkpoint':
            assert raw(initial,'Checkpoint')==raw(final,'Checkpoint');assert not ref['events'];counts['historical']+=1
            assert final['authorityVersion']==1+sum(len(f['events']) for f in s['fragments'])
            assert all(v['decision'] is None for v in observe(s,0).values())
        for path in ('ordinal','actingSide','operationStage'):
            bad=copy.deepcopy(request);x=bad['requestedTerminal']['occurrence'];x[path]=x[path]+1 if type(x[path]) is int else ('axis' if x[path]=='commonwealth' else 'commonwealth')
            rejected(lambda:admit_request(raw(bad,'Request'),s))
            assert not satisfies(final,bad['requestedTerminal'])
        bad=copy.deepcopy(request);bad['requestedTerminal']['closure']='proof-only' if request['requestedTerminal']['closure']!='proof-only' else 'completed';rejected(lambda:admit_request(raw(bad,'Request'),s))
        bad=copy.deepcopy(request);bad['requestedTerminal']['futureObligations']='mature';rejected(lambda:admit_request(raw(bad,'Request'),s))
    return counts|dict(maxCheckpointBytes=maximum)

def test_bridge_world():
    cuts=0
    for s in source_cases():
        if s['reference']['surface']!='corrected-v3':continue
        ref=decode_reference(s);retained=json.loads(ref['base']['source']['resultState'])
        for cut in range(len(ref['events'])-3,len(ref['events'])+1):
            cp=checkpoint(s,cut)
            assert cp['world']==retained['world'] and cp['randomState']==retained['randomState'],(s['sourceId'],cut,'retained Result2 World/RNG')
            cuts+=1
    assert cuts==352;return cuts


def test_continuation_schedule():
    cuts=dual=live=0
    for s in source_cases():
        ref=decode_reference(s);end=len(ref['events']);order_counts=set();initial=checkpoint(s);final=checkpoint(s,end);final_views=observe(s,end)
        for cut in range(end+1):
            cp=checkpoint(s,cut);data=raw(cp,'Checkpoint');assert read_checkpoint(data,s)==cp
            assert cp['authorityVersion']==initial['authorityVersion']+cut
            restored=continue_from(s,data,ref['inputs'][cut:],ref['events'][cut:]);assert restored==final
            assert observe(s,restored['executedTransitions'])==final_views;cuts+=1
            for order in ('attacker-first','defender-first'):
                d=schedule(s,cut,dict(contractVersion=1,roleOrder=order,clockPolicy='native-retained-trusted-clock.v1'))
                if d['dualSlot']:order_counts.add(d['selectedAudience']);dual+=1
            if s['reference']['surface']=='inherited-v3':
                decision=any(v['decision'] for v in observe(s,cut).values())
                assert decision==(cut==1 or cut==4 and s['sourceId'].startswith('inherited-control.'));live+=1
        if order_counts:assert len(order_counts)==2
    return dict(cuts=cuts,dualSchedules=dual,liveCuts=live)

def test_prefix_identity():
    rows=source_cases();pairs=0;different=0
    core=[s for s in rows if s['reference']['surface']=='corrected-v3' and not s['sourceId'].endswith(('.fallback','.prior-time'))]
    for left in core:
        if left['sourceId'].endswith('.attacker'):
            right=next(s for s in core if s['sourceId']==left['sourceId'].removesuffix('.attacker')+'.defender')
            assert left['referenceTranscriptHash']!=right['referenceTranscriptHash']
            assert left['sourceLineageHash']==right['sourceLineageHash'] and raw(checkpoint(left),'Checkpoint')==raw(checkpoint(right),'Checkpoint');pairs+=1
    for s in rows:
        if s['reference']['surface']=='corrected-v3' and s['sourceId'].endswith(('.fallback','.prior-time')):
            name=s['sourceId'].removesuffix('.fallback').removesuffix('.prior-time');base=next(x for x in core if x['sourceId']==name)
            assert s['sourceLineageHash']==base['sourceLineageHash'] and checkpoint_hash(checkpoint(s))==checkpoint_hash(checkpoint(base));pairs+=1
    for actor in side.SIDES:
        dg=next(s for s in core if s['sourceId']==f'corrected-composition.defender-capture-guard.{actor}.attacker')
        de=next(s for s in core if s['sourceId']==f'corrected-composition.defender-capture-escape.{actor}.attacker')
        assert dg['sourceLineageHash']==de['sourceLineageHash'] and checkpoint_hash(checkpoint(dg))==checkpoint_hash(checkpoint(de));pairs+=1
        guard=next(s for s in core if s['sourceId']==f'corrected-composition.attacker-capture-guard-cp-limit.{actor}.attacker')
        escape=next(s for s in core if s['sourceId']==f'corrected-composition.attacker-capture-escape.{actor}.attacker')
        assert checkpoint_hash(checkpoint(guard))!=checkpoint_hash(checkpoint(escape));different+=1
    for actor in side.SIDES:
        release=next(s for s in rows if s['sourceId']=='inherited-reserve.'+actor)
        control=next(s for s in rows if s['sourceId']=='inherited-control.'+actor+'.finish')
        assert checkpoint(release)['control']==checkpoint(control)['control']
        assert release['sourceLineageHash']!=control['sourceLineageHash'] and checkpoint_hash(checkpoint(release))!=checkpoint_hash(checkpoint(control))
    return dict(equalInitialPairs=pairs,differentInitialPairs=different,differentInitialCapabilityPairs=2)

def test_rejection():
    checks=0
    for surface in INVENTORY['enums']['surface']:
        s=next(s for s in source_cases() if s['reference']['surface']==surface)
        returned=authenticate_source(s);returned['fragments'].clear();assert authenticate_source(s)==s;checks+=1
        rejected(lambda:authenticate_source(returned));checks+=1
        for mutate in (lambda x:x['fragments'].pop(0),lambda x:x['executionStart'].__setitem__('localCut',1),lambda x:x['reference'].__setitem__('canonicalJson',x['reference']['canonicalJson']+' '),lambda x:x.__setitem__('sourceLineageHash','sha256:'+'0'*64)):
            bad=copy.deepcopy(s);mutate(bad);rejected(lambda:read_source(raw(bad,'Source')));checks+=1
        cp=checkpoint(s);data=raw(cp,'Checkpoint');rejected(lambda:read_checkpoint(data+b'\n',s));checks+=1
        rejected(lambda:side.parse3(data,'Observation3'));rejected(lambda:c.parse(data,'RootSnapshot'));checks+=2
        for field in ('authorityVersion','executedTransitions'):
            bad=copy.deepcopy(cp);bad[field]+=1;rejected(lambda:read_checkpoint(raw(bad,'Checkpoint'),s));checks+=1
        bad=copy.deepcopy(cp);bad['world']['elements'][0]['operationalState']['capabilityPointsExpended']['numerator']+=1;rejected(lambda:read_checkpoint(raw(bad,'Checkpoint'),s));checks+=1
        for bad in (data.replace(b'"contractVersion":1',b'"contractVersion":true',1),data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1),b'{"contractVersion":1,"contractVersion":1}'):
            rejected(lambda:parse(bad,'Checkpoint'));checks+=1
        ref=decode_reference(s)
        if ref['events']:
            rejected(lambda:continue_from(s,data,ref['inputs'][:1]*2,ref['events'][:1]*2));checks+=1
        cp['world']['elements'].clear();assert checkpoint(s)['world']['elements'];checks+=1
    return checks

def generated_fixture():
    entries=[];goldens=[];seen=set()
    for s in source_cases():
        ref=decode_reference(s);end=len(ref['events']);initial=checkpoint(s);final=checkpoint(s,end)
        entries.append(dict(sourceId=s['sourceId'],surface=s['reference']['surface'],sourceLineageHash=s['sourceLineageHash'],referenceTranscriptHash=s['referenceTranscriptHash'],sourceBytes=len(raw(s,'Source')),initialCheckpointHash=checkpoint_hash(initial),finalCheckpointHash=checkpoint_hash(final),executionTransitions=end,requestedTerminal=supported_terminal(s),cuts=[checkpoint_hash(checkpoint(s,i)) for i in range(end+1)]))
        if s['executionProfile'] not in seen:
            seen.add(s['executionProfile']);goldens.append(dict(sourceId=s['sourceId'],sourceCanonicalJson=raw(s,'Source').decode(),initialCheckpointCanonicalJson=raw(initial,'Checkpoint').decode(),finalCheckpointCanonicalJson=raw(final,'Checkpoint').decode(),requestCanonicalJson=raw(make_request(s),'Request').decode()))
    return dict(contractVersion=1,scope='B1 checkpoint-scoped contract evidence; unregistered; B2 execution proofs separate',sourcePins=source_pins(),task003Handoff=side.composition_evidence3b()['handoff'],entries=entries,goldens=goldens)
def fixture_bytes(value):return (json.dumps(value,ensure_ascii=True,indent=2)+'\n').encode('ascii')
def check_fixture_bytes(data,expected):require(type(data) is bytes and data==expected)
def test_fixture():
    assert FIXTURE.is_file(),'retained B1 fixture absent'
    expected=fixture_bytes(generated_fixture());check_fixture_bytes(FIXTURE.read_bytes(),expected)
    reordered=fixture_bytes(dict(reversed(list(json.loads(expected).items()))))
    mutations=[expected+b'\n',reordered]
    mutations += [expected.replace(b'"contractVersion": 1',value,1) for value in (b'"contractVersion": true',b'"contractVersion": 1.0',b'"contractVersion": 1, "contractVersion": 1')]
    for bad in mutations:rejected(lambda:check_fixture_bytes(bad,expected))
    return dict(exactBytes=len(expected),rejectedMutations=len(mutations))
def main():
    t=time.monotonic();tests=(test_boundary,test_sources,test_terminals,test_bridge_world,test_continuation_schedule,test_prefix_identity,test_rejection,test_fixture)
    for test in tests:print(test.__name__,test(),flush=True)
    print('B1 PASS',len(tests),'groups',round(time.monotonic()-t,3),'seconds',flush=True)
if __name__=='__main__':main()
