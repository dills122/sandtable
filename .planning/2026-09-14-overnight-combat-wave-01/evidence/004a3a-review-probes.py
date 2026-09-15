import importlib.util, hashlib, struct, json, copy
from pathlib import Path
p=Path('/Users/dsteele/.codex/worktrees/0b59/sandtable/docs/specs/verify-combat-side-projection-v1.py')
spec=importlib.util.spec_from_file_location('review_side',p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
counts=dict(binary=0,history=0,source=0,malformed=0,completion=0,fallback=0,privacy=0)
def S(b):return struct.pack('>I',len(b))+b
def digest(domain,payload):return hashlib.sha256(domain.encode('ascii')+b'\0'+payload).digest()
reject={'contractVersion':3,'status':'rejected','receipt':None}
for sources,project,admit,kind in ((m.source_cases3(),m.views3,m.admit_a3,'Observation3'),(m.ledger_cases3(),m.ledger_views3,m.admit_ledger3,'LedgerObservation3')):
 for src in sources:
  owner=src['base'].get('side') or src['base']['case']['actor']; vs=project(src,owner)
  for i,v in enumerate(vs):
   if i:
    prev=vs[i-1]
    assert v['history'][:len(prev['history'])]==prev['history']
    assert v['ownReceipts'][:len(prev['ownReceipts'])]==prev['ownReceipts']
    assert v['visibleRevision']-prev['visibleRevision'] in (0,1)
    assert (m.raw3(v,kind)==m.raw3(prev,kind)) == (v['visibleRevision']==prev['visibleRevision'])
    counts['history']+=1
   if not v['decision']:continue
   d=v['decision'];ctx=v['context'];cs=[json.dumps(a['candidate'],ensure_ascii=True,separators=(',',':')).encode('ascii') for a in d['actions']]
   assert cs==sorted(cs) and len(cs)==len(set(cs))
   payload=struct.pack('>I',1)+S(ctx['campaignId'].encode())+S(owner.encode())+bytes.fromhex(v['cycleRef'][7:])+S(d['kind'].encode())+struct.pack('>Q',d['openingRevision'])+S(ctx['capabilityPolicyId'].encode())+struct.pack('>I',len(cs))+b''.join(S(c) for c in cs)
   h=digest('sandtable.cycle.actions.v1',payload);assert d['actionSetId']=='sha256:'+h.hex()
   for index,a in enumerate(d['actions']):
    assert a['actionId']=='sha256:'+digest('sandtable.cycle.action.v1',struct.pack('>I',1)+h+struct.pack('>I',index)).hex();counts['binary']+=1
   if i!=next(j for j,w in enumerate(vs) if w['decision']):continue
   current=m.prefix(src,i);proposal=m.submission3(v);raw=m.raw3(proposal,'Submission3')
   # Warm then mutate every source envelope independently.
   assert project(current,owner)[-1]==v
   for fld,val in [('name',[]),('family',None),('base',{}),('inputs',[{}]*i),('events',[b'{}']*i)]:
    bad=copy.deepcopy(current);bad[fld]=val
    assert admit(bad,owner,raw)==reject;counts['source']+=1
   out=project(current,owner);out[-1]['context']['capabilityPolicyId']='forged';out[-1]['history'][0]['positionId']='forged'
   assert project(current,owner)[-1]==v;counts['source']+=1
   for changed in (raw+b' ',raw.replace(b'"contractVersion":3',b'"contractVersion":true'),raw.replace(b'"decisionFamily":"cycle"',b'"decisionFamily":"combat"'),raw.replace(b'"audience":"'+owner.encode()+b'"',b'"audience":null'),b'['*1000+b']'*1000):
    assert admit(current,owner,changed)==reject;counts['malformed']+=1
  if src['family']=='ledger-reserve' and src['base']['case']['name']=='later-complete-intent':
   ci=next(i for i,x in enumerate(src['inputs']) if x['command']['kind']=='complete-release');event=json.loads(src['events'][ci]);assert event['author']=='system'
   view=vs[ci];idx=next(i for i,a in enumerate(view['decision']['actions']) if a['candidate']['kind']=='complete-release');raw=m.raw3(m.submission3(view,idx),'Submission3')
   assert admit(m.prefix(src,ci),owner,raw,2100)['status']=='accepted'
   receipt=admit(src,owner,raw,None,False);assert receipt['status']=='accepted' and receipt['receipt']==vs[-1]['ownReceipts'][-1]
   assert admit(m.prefix(src,ci),owner,raw,None,False)==reject;counts['completion']+=1
  if src['name'].endswith('.fallback'):
   assert len(vs[-1]['ownReceipts'])==(1 if src['family']=='inherited-control' else 0);counts['fallback']+=1
  if src['family'].startswith('inherited-'):
   enemy=next(s for s in m.SIDES if s!=owner);evs=project(src,enemy)
   through=len(evs)-1 if src['family']=='inherited-reserve' else len(evs)-2
   for v in evs[:through+1]:assert m.raw3(v,kind)==m.raw3(evs[0],kind);counts['privacy']+=1
print('INDEPENDENT PROBES PASS',counts,flush=True)
