import importlib.util,json,pathlib,collections,hashlib,sys
sys.dont_write_bytecode=True
r=pathlib.Path('/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs')
sp=importlib.util.spec_from_file_location('lineage_probe',r/'verify-combat-authority-composition-v1.py');c=importlib.util.module_from_spec(sp);sp.loader.exec_module(c)
rd=c.reserve_cycle.rd; report=[]
cases=lambda mod:json.loads(mod.FIXTURE.read_bytes())['cases']
def root(h):
 assert len(h)>=6
 s=rd.replay(*h[:6]);assert rd.read_state(rd.raw(s,'ReserveState'),*h[:6])==s
 return {'preamble':len(h[2]),'weather':len(h[3]),'stage-entry':len(h[4]),'reserve-designation':len(h[5])}
def add(item,h,parts,final,world=None,rng=None,cycle=None,proofs=0):
 assert final==item['terminal'];assert world==item['world'];assert rng==item['randomState'];assert cycle==item['cycle']
 p=root(h)|parts
 # Every retained event is canonical native bytes; no synthetic events or duplicate embedded transcripts counted.
 report.append(dict(family=item['family'],case=item['caseId'],creationArtifacts=2,eventFragments=p,nativeEventCount=sum(p.values()),proofOnlyFragments=proofs,rootCycleOrdinal=(item['root'].get('cycle') or {}).get('ordinal'),sourceCycle=item['cycle'],position=item['position'],worldHash='sha256:'+hashlib.sha256(c.encode(world)).hexdigest()))
 print('DONE',item['family'],item['caseId'],p,flush=True)
for case,item in zip(cases(c.noattack),c.noattack_sources()):
 h,se,_,states,inp,ev=c.noattack.trace(case);m=c.noattack;end=m.read_control(m.raw(states[-1],'Control'),h,se,ev);entry=end['selection']['boundary']['entry']
 add(item,h,{'movement':len(h[6]),'movement-lifecycle':len(h[7]),'breakdown-entry':len(h[8]),'selection':len(se),'noattack':len(ev)},end,entry['world'],entry['randomState'],entry['cycle'])
for case,item in zip(cases(c.direct),c.direct_sources()):
 m=c.direct;args,inp,ev,before,end=m.trace(case);actual=m.read_state(m.irl.raw(end,'LifecycleState'),*args,[ev]);add(item,args,{'phasing-movement':len(args[6]),'reaction-trigger':len(args[7]),'reaction-close':1},actual,actual['world'],actual['randomState'],actual['cycle'])
for case,item in zip(cases(c.fallback),c.fallback_sources()):
 m=c.fallback;args,inp,ev,states=m.trace(case);end=m.read_state(m.raw(states[-1],'FallbackState'),*args,ev);add(item,args,{'phasing-movement':len(args[6]),'reaction-trigger':len(args[7]),'participant-start':len(args[8]),'reaction-fallback':len(ev)},end,end['world'],end['randomState'],end['cycle'])
for case,item in zip(cases(c.reaction_complete),c.reaction_completion_sources()):
 m=c.reaction_complete;pc=next(p for p in cases(m.irs) if p['name']==case['predecessorCase']);args,pi,pe,ps=m.irs.trace(pc)
 previous=m.irs.read_state(m.irs.raw(ps[-1],m.irs.STATE),*args,pe);prior_event,inp,ev,states=m.trace(case)
 assert prior_event==pe[-1] and m.irl.raw(states[0],'LifecycleState')==m.irs.raw(previous,m.irs.STATE)
 end=m.read_state(m.irl.raw(states[-1],'LifecycleState'),case,ev)
 add(item,args,{'phasing-movement':len(args[6]),'reaction-trigger':len(args[7]),'participant-start':len(args[8]),'reaction-second-move':len(pe),'reaction-completion':len(ev)},end,end['world'],end['randomState'],end['cycle'])
def release_chain(actor):
 m=c.reserve_release;case=next(x for x in cases(m) if x['actor']==actor);base,parent,states,inp,ev=m.trace(case);h,_,pstates,pi,pe=parent
 assert c.reserve_cycle.read_control(c.reserve_cycle.raw(pstates[-1],'Control'),h,pe)==pstates[-1]
 m.read_base(m.raw(base,'Base'),case);end=m.read_control(m.raw(states[-1],'Control'),base,inp,ev)
 world=m.release.project_world(pstates[-1]['base']['world'],base['releaseBase'],end['release'])
 return (base,parent,states,inp,ev),h,{'reserve-cycle':len(pe),'reserve-release':len(ev)},world
for case,item in zip(cases(c.reserve_cycle),c.reserve_cycle_sources()):
 m=c.reserve_cycle;h,parent,states,inp,ev=m.trace(case);end=m.read_control(m.raw(states[-1],'Control'),h,ev);add(item,h,{'reserve-cycle':len(ev)},end,end['base']['world'],end['base']['randomState'],end['base']['cycle'])
for item in c.reserve_release_sources():
 result,h,parts,world=release_chain(item['actor']);end=result[2][-1];s=end['release'];add(item,h,parts,end,world,s['randomState'],result[1][2][-1]['base']['cycle'])
for item in c.armed_sources():
 result,h,parts,world=release_chain(item['actor']);m=c.armed;proof=m.proof_from(item['actor'],result);end=m.read_proof(m.raw(proof,'Proof'),item['actor']);add(item,h,parts,end,world,result[2][-1]['release']['randomState'],end['currentCycle'],proofs=1)
def cycle_chain(actor,action):
 result,h,parts,world=release_chain(actor);m=c.cycle_control;base,states,inp,ev=m.trace(actor,action);bundle=m.read_base(m.raw(base,'InheritedCycleControlBase'))
 assert bundle[1]==result;c.armed.read_proof(c.armed.raw(bundle[2],'Proof'),actor)
 end=m.read_state(m.cyc.raw(states[-1],'CycleControlState'),base,inp,ev)
 return (base,states,inp,ev),h,parts|{'cycle-control':len(ev)},end
for item in c.cycle_sources():
 action=item['caseId'].rsplit('-',1)[1];result,h,parts,end=cycle_chain(item['actor'],action);base=result[0];cy=dict(sourceCycle=base['controlBase']['releaseBase']['cycle'],activeCycle=end['activeCycle']);add(item,h,parts,end,end['world'],end['randomState'],cy,proofs=1)
for item in c.reserve_movement_sources():
 actor=item['actor'];cres,h,parts,ce=cycle_chain(actor,'repeat');m=c.reserve_movement;pm=m.previous;mb,ms,mi,me=pm.trace(actor)
 pm.read_base(pm.raw(mb,'InheritedReserveMovementBase'));assert mb['predecessorBase']==cres[0] and mb['predecessorInputs']==cres[2] and mb['predecessorEvents']==[json.loads(e) for e in cres[3]]
 mend=pm.read_state(pm.raw(ms[-1],'InheritedReserveMovementState'),mb,mi,me)
 base,states,inp,ev=m.trace(actor);m.read_base(m.raw(base,'InheritedReserveMovementCompletionBase'));assert base['predecessorBase']==mb and base['predecessorInputs']==mi and base['predecessorEvents']==[json.loads(e) for e in me]
 end=m.read_state(m.raw(states[-1],'InheritedReserveMovementCompletionState'),base,inp,ev)
 add(item,h,parts|{'reserve-movement':len(me),'reserve-movement-completion':len(ev)},end,end['world'],end['randomState'],end['cycle'],proofs=1)
assert len(report)==28
path=pathlib.Path('/private/tmp/004b-lineage-preflight-results.json');path.write_text(json.dumps(report,indent=2)+'\n');print('RESULT',path,'SHA',hashlib.sha256(path.read_bytes()).hexdigest(),flush=True)
