#!/usr/bin/env python3
"""C3c.1 contract experiment, not a production engine or durable recovery reader."""
import copy
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent

def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m

rnd=load('round_contract',ROOT/'verify-combat-sealed-round-v1.py')
steps=rnd.steps;env=steps.env;world=env.world
rng=load('rng',ROOT.parent/'research/verify-combat-rng.py')
FIXTURE=ROOT/'fixtures/combat-result-settlement-v1.json'
INVENTORY=json.loads((ROOT/'combat-result-settlement-v1.schema.json').read_text())
SCHEMA=rnd.SCHEMA|{k:[tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}
encode,sha=rnd.encode,rnd.sha

class Invalid(ValueError):
    def __init__(self,code,path=''):
        self.code,self.path=f'CMB-RES-{code:03}',path
        super().__init__(self.code+' '+(path or '/'))

def require(ok,code,path=''):
    if not ok:raise Invalid(code,path)

def typed(v,kind,path='',depth=0):
    require(depth<=32,1,path)
    if kind.endswith('?'):
        if v is not None:typed(v,kind[:-1],path,depth)
    elif kind=='Route':typed(v,'id[]',path,depth)
    elif kind=='futureTurn':require(type(v) is int and 1<=v<=115,2,path)
    elif kind=='ResultEffect':
        require(type(v) is dict and v.get('kind') in INVENTORY['effectTags'],3,path)
        typed(v,INVENTORY['effectTags'][v['kind']],path,depth)
    elif kind in SCHEMA:
        require(type(v) is dict and set(v)=={k for k,_ in SCHEMA[kind]},1,path)
        for k,t in SCHEMA[kind]:typed(v[k],t,path+'/'+k,depth+1)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v)<=512,1,path)
        for i,x in enumerate(v):typed(x,kind[:-2],f'{path}/{i}',depth+1)
    else:
        try:rnd.typed(v,kind,path,depth)
        except rnd.Invalid as e:raise Invalid(int(e.code[-3:]),e.path) from e

def canonical(v,kind):
    if kind.endswith('?'):return None if v is None else canonical(v,kind[:-1])
    if kind=='ResultEffect':return canonical(v,INVENTORY['effectTags'][v['kind']])
    if kind in SCHEMA:return {k:canonical(v[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'):return [canonical(x,kind[:-2]) for x in v]
    return v

def raw(v,kind):
    typed(v,kind);out=encode(canonical(v,kind));require(len(out)<=1048576,1);return out

def parse(data,kind):
    require(type(data) is bytes and 0<len(data)<=1048576,1)
    def pairs(items):
        out={}
        for k,v in items:require(k not in out,1);out[k]=v
        return out
    try:v=json.loads(data.decode('utf8'),object_pairs_hook=pairs,parse_constant=lambda _:(_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as e:raise Invalid(1) from e
    require(raw(v,kind)==data,8);return v

def digest(kind,v):return steps.digest(INVENTORY['domains'][kind],encode(v))

def context(case):
    base,old=rnd.start();b=base['boundary'];b['randomState']['nextByteCursor']=case['cursor']
    side=case['attackerSide'];b['cycle']['actingSide']=side;b['firstActingSide']=side;b['position']['activeSide']=side
    for e in b['world']['elements']:
        role=0 if world.SIDES[e['elementId']]==side else 1
        e['operationalState']['capabilityPointsExpended']['numerator']=case['preAssaultCp'][role]
    steps.boundary(encode(b));control=steps.initial(b);ins=[];events=[]
    for old_input in old['inputs']:
        inp=copy.deepcopy(old_input);cmd=inp['command'];kind=cmd['kind'];cmd['segmentId']=control['segmentId']
        if cmd['expectedPriorVersion'] is not None:cmd['expectedPriorVersion']=control['stateVersion']
        if cmd['decisionId'] is not None:cmd['decisionId']=control['segmentId']+'.'+('selection' if kind=='choose-selection' else 'rba')
        if kind=='choose-selection':cmd['candidate']=steps.candidate(b);inp['actor']=side
        if kind=='decline-rba':cmd['participant']=steps.candidate(b)['defender']['unit'];inp['actor']=cmd['participant']['originalSide']
        control,event,_=steps.transition(b,control,inp);ins.append(inp);events.append(dict(canonicalUtf8=event.decode()))
    predecessor=dict(boundary=b,inputs=ins,events=events)
    base=dict(contractVersion=1,boundary=b,steps=control)
    rnd.read_base(raw(base,'Base'),predecessor)
    committed,ri,re=rnd.trace_case(base,dict(seals=case.get('seals',['attacker','defender']),terminal='committed'))
    rnd.read_state(raw(committed,'RoundState'),base,ri,re)
    return dict(base=base,predecessor=predecessor,roundInputs=ri,roundEvents=re,committed=committed)

def initial(ctx):
    c=ctx['committed'];require(c['status']=='committed' and c['stepIndex']==5 and len(c['stepReceipts'])==5,6)
    return dict(contractVersion=1,committedHash=sha(raw(c,'RoundState')),stateVersion=c['stateVersion'],prefix=c['prefix'],
        status='committed',result=None,settlementId=None,window=None,acceptedHighWater=c['timing']['highWaterUnixMilliseconds'],
        world=copy.deepcopy(c['world']),randomState=copy.deepcopy(c['randomState']),roundClosureReceiptId=None,caCompletionReceiptId=None,receipts=[],closed=False)

def resolve(ctx):
    c=ctx['committed'];before=c['randomState'];cursor=before['nextByteCursor'];draws=[];dice=[]
    rules=steps.inputs.expected_rules()
    try:
        labels,values,ends,_,_,role=rng.trace(before['seed'],cursor)
        for label in labels:
            die,end,consumed=rng.roll(before['seed'],cursor)
            draws.append(dict(purpose=label,beforeCursor=cursor,afterCursor=end,die=die,consumed=list(consumed)))
            dice.append(die);cursor=end
    except (ValueError,OverflowError) as e:raise Invalid(7,'/randomState') from e
    require(tuple(dice)==values and cursor==ends[-1] and labels[:8]==rules['procedure']['orderedPurposes'],6)
    ac,dc=10*dice[0]+dice[1],10*dice[2]+dice[3]
    lookup={v['coordinate']:v['adjustment'] for v in rules['morale']}
    am,dm=lookup[ac],lookup[dc]
    facts=world.result_facts(am-dm,10*dice[4]+dice[5],10*dice[6]+dice[7],dice[-1] if role else None)
    v=dict(commitmentId=c['commitmentId'],rulesInputHash=env.Context().config['rulesInputHash'],
        procedureId=rules['procedure']['procedureId'],beforeRandomState=copy.deepcopy(before),
        afterRandomState=before|dict(nextByteCursor=cursor),draws=draws,attackerMoraleCoordinate=ac,
        defenderMoraleCoordinate=dc,attackerMorale=am,defenderMorale=dm,basicDifferential=0,facts=facts)
    return dict(resultId='res.'+digest('result',v),**v)

def projected_world(ctx,s,cut,retreat=None,custody=None):
    c=ctx['committed'];selection=ctx['base']['steps']['selection'];facts=s['result']['facts']
    role_elements=[next(e for e in c['world']['elements'] if e['elementId']==selection[r]['unit']['elementId']) for r in ('attacker','defender')]
    existing=s['world']['settlements'][0] if s['world']['settlements'] else None
    ret=retreat or (existing['disposition']['kind'] if existing and existing['disposition'] else 'not-required')
    cust=custody or (existing['custody']['kind'] if existing and existing['custody'] else None)
    case=dict(settlementId=s['settlementId'],creationBinding=c['world']['creationBinding'],attackerSide=selection['attacker']['unit']['originalSide'],
        preAssaultCp=[role_elements[0]['operationalState']['capabilityPointsExpended']['numerator']-5,
                      role_elements[1]['operationalState']['capabilityPointsExpended']['numerator']-3],
        differential=facts['differential'],attackerCoordinate=facts['attackerCoordinate'],defenderCoordinate=facts['defenderCoordinate'],
        captureDie=facts['captureDie'],retreatChoice=ret,custodyChoice=cust)
    w=world.world_at(case,cut);settlement=w['settlements'][0]
    settlement['commitmentId']=c['commitmentId'];settlement['resultId']=s['result']['resultId']
    if settlement['disposition']:settlement['disposition']['predecessorReceiptId']=s['result']['resultId']
    if cut=='resolved':
        unwrapped=copy.deepcopy(w);unwrapped['settlements']=[];require(unwrapped==c['world'],6,'/preLossWorld')
    return w

def command(ctx,s,kind,choice=None):
    return dict(contractVersion=1,kind=kind,roundId=ctx['committed']['roundId'],commitmentId=ctx['committed']['commitmentId'],
        expectedPriorVersion=s['stateVersion'] if kind in ('resolve','advance') else None,
        decisionId=s['window']['decisionId'] if s['window'] else None,choice=choice)

def trusted(cmd,actor='system',now=None,available=True):return dict(command=cmd,actor=actor,admittedAt=now,clockAvailable=available)

def transition(ctx,prior,inp):
    typed(prior,'ResultState');typed(inp,'ResultInput');cmd=inp['command'];kind=cmd['kind'];actor=inp['actor'];c=ctx['committed'];b=ctx['base']['boundary']
    require(prior['committedHash']==sha(raw(c,'RoundState')),4)
    require(cmd['contractVersion']==1 and kind in ('resolve','advance','choose','expire','unavailable'),3)
    require((cmd['roundId'],cmd['commitmentId'])==(c['roundId'],c['commitmentId']),4)
    require(actor in ('axis','commonwealth') if kind=='choose' else actor=='system',4)
    require((cmd['choice'] is not None)==(kind=='choose'),3)
    require((cmd['expectedPriorVersion'] is not None)==(kind in ('resolve','advance')),3)
    ch=sha(raw(cmd,'ResultCommand'))
    retained=next((r for r in prior['receipts'] if r['commandHash']==ch and r['actor']==actor),None)
    if retained:return prior,None,retained['receiptId']
    window=prior['window']
    if kind in ('expire','unavailable') and (window is None or cmd['decisionId']!=window['decisionId']):return prior,None,None
    require(not prior['closed'] and len(prior['receipts'])<32 and prior['stateVersion']<2**63-1,7)
    require(cmd['expectedPriorVersion']==prior['stateVersion'] if kind in ('resolve','advance') else True,6)
    require(cmd['decisionId'] is None if kind in ('resolve','advance') else window is not None and cmd['decisionId']==window['decisionId'],6)
    s=copy.deepcopy(prior);author=actor;effect=None;now=inp['admittedAt']
    def record_choice(choice,reason,timing):
        nonlocal effect
        wk=window['kind'] if window else ('retreat' if s['status']=='resolved' else 'custody')
        cut='disposition' if wk=='retreat' else 'custody'
        s['world']=projected_world(ctx,s,cut,retreat=choice if wk=='retreat' else None,custody=choice if wk=='custody' else None)
        effect=dict(kind='disposition-recorded' if wk=='retreat' else 'custody-settled',reason=reason,timing=timing,payload=s['world']['settlements'][0][cut])
        s['status']=cut;s['window']=None
    if kind in ('choose','expire','unavailable'):
        require(window is not None,6)
        choices=('retreat','refuse-retreat') if window['kind']=='retreat' else ('relocate-and-guard','leave-unguarded')
        if kind=='choose':require(actor==window['owner'] and cmd['choice'] in choices,4)
        t=copy.deepcopy(window['timing'])
        lost=not inp['clockAvailable'] or now is None or now<s['acceptedHighWater']
        late=now is not None and now>=t['deadlineUnixMilliseconds']
        if kind=='expire' and not lost and not late:return prior,None,None
        if kind=='choose' and not lost:require(not late,5)
        fallback=kind!='choose' or lost
        if not lost:s['acceptedHighWater']=max(s['acceptedHighWater'],now);t['highWaterUnixMilliseconds']=s['acceptedHighWater']
        reason=('clock-unavailable' if lost else 'controller-unavailable' if kind=='unavailable' else 'deadline') if fallback else 'owner-choice'
        if fallback:author='system'
        record_choice(choices[-1] if fallback else cmd['choice'],reason,t)
    elif kind=='resolve':
        require(prior['status']=='committed' and now is None and inp['clockAvailable'],6)
        result=resolve(ctx);s['result']=result;s['randomState']=result['afterRandomState']
        s['settlementId']='set.'+digest('settlement',dict(commitmentId=c['commitmentId'],resultId=result['resultId']))
        s['world']=projected_world(ctx,s,'resolved');s['status']='resolved'
        effect=dict(kind='assault-resolved',result=result,settlementId=s['settlementId'])
    else:
        status=s['status'];require(window is None,6)
        needs_retreat=status=='resolved' and s['result']['facts']['requiredRetreat']>0
        needs_custody=status=='retreat' and len(s['world']['custodyLots'])>0
        if needs_retreat or needs_custody:
            wk='retreat' if needs_retreat else 'custody';owner=ctx['base']['steps']['selection']['defender']['unit']['originalSide'] if needs_retreat else s['world']['custodyLots'][0]['captor']['originalSide']
            lost=not inp['clockAvailable'] or now is None or now<s['acceptedHighWater']
            try:t=None if lost else steps.inputs.make_timing(env.Context().config,wk,now)
            except steps.inputs.Invalid:t=None
            if t is None:record_choice('refuse-retreat' if wk=='retreat' else 'leave-unguarded','opening-clock-unavailable',None)
            else:
                s['acceptedHighWater']=now;s['window']=dict(decisionId=s['settlementId']+'.choice.'+wk,kind=wk,owner=owner,timing=t)
                s['status']='waiting-'+wk;effect=dict(kind='choice-opened',window=s['window'])
        else:
            require(now is None and inp['clockAvailable'],3)
            if status=='resolved':record_choice('not-required','not-required',None)
            elif status in ('disposition','losses','retreat','custody'):
                cut={'disposition':'losses','losses':'retreat','retreat':'relationships','custody':'relationships'}[status]
                s['world']=projected_world(ctx,s,cut);s['status']=cut
                effect=dict(kind={'losses':'losses-settled','retreat':'retreat-settled','relationships':'relationships-settled'}[cut],payload=s['world']['settlements'][0][cut])
            elif status=='relationships':
                st=s['world']['settlements'][0];proof=[st[k]['receiptId'] for k in ('disposition','losses','retreat','custody','relationships') if st[k]]
                s['status']='round-closed';effect=dict(kind='round-closed',settlementId=s['settlementId'],proofReceipts=proof)
            elif status=='round-closed':
                s['status']='closed';s['closed']=True
                effect=dict(kind='ca-completed',fromPositionId=steps.edge(b)['combatPositionIds'][5],toPositionId=steps.edge(b)['releasePositionId'],
                    previousStepReceiptId=c['stepReceipts'][-1],roundClosureReceiptId=s['roundClosureReceiptId'])
            else:raise Invalid(6,'/status')
    event=dict(contractVersion=1,eventType='combat-result-'+effect['kind'],author=author,campaignId=b['cycle']['campaignId'],rulesetHash=b['cycle']['rulesetHash'],
        configurationHash=b['cycle']['admittedPolicyBundleDigest'],cycleId=rnd.cycle_id(ctx['base']),segmentId=ctx['base']['steps']['segmentId'],
        roundId=c['roundId'],commitmentId=c['commitmentId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,
        priorPrefix=prior['prefix'],input=canonical(inp,'ResultInput'),effect=canonical(effect,'ResultEffect'))
    rid='cmb.'+digest('receipt',event);event['receiptId']=rid;data=raw(event,'ResultEvent')
    if effect['kind']=='round-closed':s['roundClosureReceiptId']=rid
    if effect['kind']=='ca-completed':s['caCompletionReceiptId']=rid
    s['stateVersion']=event['stateVersion'];s['prefix']=steps.seq.prefix_event(prior['prefix'],data)
    s['receipts'].append(dict(commandHash=ch,eventHash=sha(data),receiptId=rid,actor=actor,stateVersion=s['stateVersion']))
    raw(s,'ResultState');return s,data,rid

def read_event(data,ctx,prior,inp):
    parse(data,'ResultEvent');s,expected,_=transition(ctx,prior,inp);require(data==expected,6);return s

def replay(ctx,inputs,events,length=None):
    require(type(inputs) is list and type(events) is list and len(inputs)==len(events)<=32,1)
    require(length is None or type(length) is int and 0<=length<=len(events),2)
    verify_context(ctx);s=initial(ctx)
    for inp,event in zip(inputs[:length],events[:length]):s=read_event(event,ctx,s,inp)
    return s

def read_state(data,ctx,inputs,events,length=None):
    parse(data,'ResultState');s=replay(ctx,inputs,events,length);require(raw(s,'ResultState')==data,6);return s

def trace_case(case):
    ctx=context(case);s=initial(ctx);inputs=[];events=[]
    while not s['closed']:
        kind='resolve' if s['status']=='committed' else 'choose' if s['window'] else 'advance'
        choice=case['retreatChoice' if s['window']['kind']=='retreat' else 'custodyChoice'] if s['window'] else None
        actor=s['window']['owner'] if s['window'] else 'system';now=None
        if s['window']:now=s['window']['timing']['openedAtUnixMilliseconds']+1
        elif (s['status']=='resolved' and s['result']['facts']['requiredRetreat']) or (s['status']=='retreat' and s['world']['custodyLots']):now=max(10000,s['acceptedHighWater']+1000)
        inp=trusted(command(ctx,s,kind,choice),actor,now);s,data,_=transition(ctx,s,inp)
        inputs.append(inp);events.append(data)
    return ctx,s,inputs,events

def run_case(case):return trace_case(case)[1]

def behavioral(case,state):
    elements=state['world']['elements'];attacker=next(e for e in elements if world.SIDES[e['elementId']]==case['attackerSide'])
    defender=next(e for e in elements if e is not attacker);e=case['expected']
    assert [x['components'][0]['currentToe'] for x in (attacker,defender)]==e['toe']
    assert [x['operationalState']['cohesionLevel'] for x in (attacker,defender)]==e['cohesion']
    assert [x['operationalState']['capabilityPointsExpended']['numerator'] for x in (attacker,defender)]==e['cp']
    assert [r['kind'] for r in state['world']['relationships']]==([] if e['relation'] is None else [e['relation']])
    assert len(state['world']['guards'])==e['guards'] and len(state['world']['replacementEntitlements'])==e['entitlements']
    assert state['closed'] and state['status']=='closed'

def rejected(call,code=None):
    try:call()
    except Invalid as e:
        if code is not None:assert e.code==f'CMB-RES-{code:03}',(e.code,code)
    else:raise AssertionError('invalid result contract accepted')

def verify_context(ctx):
    try:
        rnd.read_base(raw(ctx['base'],'Base'),ctx['predecessor'])
        rnd.read_state(raw(ctx['committed'],'RoundState'),ctx['base'],ctx['roundInputs'],ctx['roundEvents'])
    except (rnd.Invalid,steps.Invalid) as e:raise Invalid(4,'/committedPredecessor') from e

def checkpoint_checks(case):
    ctx,final,inputs,events=trace_case(case);s=initial(ctx);cuts=mutations=raws=0
    verify_context(ctx)
    for i,(inp,data) in enumerate(zip(inputs,events)):
        after=read_event(data,ctx,s,inp);stored=raw(after,'ResultState')
        assert read_state(stored,ctx,inputs,events,i+1)==after
        retry,new,rid=transition(ctx,after,inp)
        assert retry is after and new is None and rid==after['receipts'][-1]['receiptId']
        for field in ('priorVersion','stateVersion','priorPrefix','commitmentId','receiptId','configurationHash'):
            v=json.loads(data);v[field]=v[field]+1 if type(v[field]) is int else ('sha256:' if field in ('priorPrefix','configurationHash') else 'x.')+'0'*64
            rejected(lambda:read_event(encode(v),ctx,s,inp));mutations+=1
        for field in ('status','prefix','committedHash'):
            v=copy.deepcopy(after);v[field]='changed' if field=='status' else 'sha256:'+'0'*64
            rejected(lambda:read_state(raw(v,'ResultState'),ctx,inputs,events,i+1));mutations+=1
        v=copy.deepcopy(after);v['world']['elements'][0]['components'][0]['currentToe']+=1
        rejected(lambda:read_state(raw(v,'ResultState'),ctx,inputs,events,i+1));mutations+=1
        for invalid in (data+b'\n',b'\xef\xbb\xbf'+data,data[:-1],data.replace(b'"contractVersion":1',b'"contractVersion":1.0',1),data.replace(b'{',b'{"contractVersion":1,',1)):
            rejected(lambda:read_event(invalid,ctx,s,inp));raws+=1
        event=json.loads(data)
        if event['effect']['kind']=='assault-resolved':
            for field in ('die','afterCursor','purpose'):
                v=copy.deepcopy(event);d=v['effect']['result']['draws'][0];d[field]=d[field]+1 if type(d[field]) is int else 'defender.assault.tens'
                rejected(lambda:read_event(raw(v,'ResultEvent'),ctx,s,inp));mutations+=1
        s=after;cuts+=1
    assert final==s
    assert final['randomState']==final['result']['afterRandomState']
    assert all(e['ammunition']['points']==0 for e in final['world']['elements'])
    if len(events)>1:
        rejected(lambda:replay(ctx,inputs,events[::-1]));rejected(lambda:replay(ctx,inputs[1:],events[1:]));mutations+=2
    bad=copy.deepcopy(ctx);bad['committed']['world']['elements'][0]['ammunition']['points']=1
    rejected(lambda:verify_context(bad),4);mutations+=1
    return cuts,mutations,raws

def timing_checks(case):
    ctx,_,inputs,events=trace_case(case);s=initial(ctx);checks=0
    for inp,data in zip(inputs,events):
        s=read_event(data,ctx,s,inp)
        if not s['window']:continue
        w=s['window'];deadline=w['timing']['deadlineUnixMilliseconds'];choice='retreat' if w['kind']=='retreat' else 'relocate-and-guard'
        def call(kind,now,actor='system',available=True,selected=None):
            return transition(ctx,s,trusted(command(ctx,s,kind,selected),actor,now,available))
        chosen=call('choose',deadline-1,w['owner'],selected=choice)[0]
        assert chosen['window'] is None
        rejected(lambda:call('choose',deadline,w['owner'],selected=choice),5)
        rejected(lambda:call('choose',deadline+1,w['owner'],selected=choice),5)
        other='axis' if w['owner']=='commonwealth' else 'commonwealth'
        rejected(lambda:call('choose',None,other,False,choice),4)
        assert call('expire',deadline-1)==(s,None,None)
        for now,available,kind in [(deadline,True,'expire'),(None,False,'expire'),(w['timing']['openedAtUnixMilliseconds']-1,True,'choose'),(None,False,'choose'),(deadline-1,True,'unavailable')]:
            after,event,_=call(kind,now,w['owner'] if kind=='choose' else 'system',available,choice if kind=='choose' else None)
            e=parse(event,'ResultEvent');assert e['author']=='system'
            assert e['effect']['payload']['kind']==('refuse-retreat' if w['kind']=='retreat' else 'leave-unguarded')
            assert after['randomState']==s['randomState'] and after['window'] is None
            checks+=1
        stale=trusted(command(ctx,s,'expire'),now=deadline)
        assert transition(ctx,chosen,stale)==(chosen,None,None)
        checks+=6
    # Required-window opening with a lost clock records the fallback without fabricating timing.
    s=initial(ctx)
    for inp,data in zip(inputs,events):
        if json.loads(data)['effect']['kind']=='choice-opened':
            lost=copy.deepcopy(inp);lost['admittedAt']=None;lost['clockAvailable']=False
            after,e,_=transition(ctx,s,lost)
            assert after['window'] is None and json.loads(e)['effect']['timing'] is None
            assert after['randomState']==s['randomState'];checks+=1
        s=read_event(data,ctx,s,inp)
    return checks

def main():
    f=json.loads(FIXTURE.read_text());totals=[0,0,0];timers=0
    for name,digest_value in f.get('sourceHashes',{}).items():assert sha((ROOT/name).read_bytes())==digest_value
    assert 'sourceHashes' in f and 'goldens' in f and len(f['goldens'])==len(f['cases'])
    for case in f['cases']:
        behavioral(case,run_case(case))
        totals=[a+b for a,b in zip(totals,checkpoint_checks(case))]
        timers+=timing_checks(case)
        mirrored=copy.deepcopy(case);mirrored['attackerSide']='commonwealth';behavioral(mirrored,run_case(mirrored))
        reversed_seals=copy.deepcopy(case);reversed_seals['seals']=['defender','attacker'];behavioral(reversed_seals,run_case(reversed_seals))
    extreme=copy.deepcopy(f['cases'][0]);extreme['cursor']=2**64-1;ctx=context(extreme);s=initial(ctx);before=raw(s,'ResultState')
    rejected(lambda:transition(ctx,s,trusted(command(ctx,s,'resolve'))),7);assert before==raw(s,'ResultState')
    rejected(lambda:transition(ctx,s,trusted(command(ctx,s,'advance'))),6)
    for length in (-1,True,33):rejected(lambda:replay(ctx,[],[],length),2)
    for invalid in (b'null',b'[]',b'{"x":NaN}',b' '*1048577):rejected(lambda:parse(invalid,'ResultState'))
    if 'goldens' in f:
        for case,g in zip(f['cases'],f['goldens']):
            _,state,inputs,events=trace_case(case)
            assert g['inputs']==inputs and g['events']==[e.decode() for e in events] and g['state']==raw(state,'ResultState').decode()
    print(f'PASS: {len(f["cases"])} literal cases, mirrored roles/reversed seals; {totals[0]} replay cuts, {totals[1]} mutations, {totals[2]} raw rejections, {timers} timing checks; overflow/closure guards. Contract evidence only.')

if __name__=='__main__':main()
