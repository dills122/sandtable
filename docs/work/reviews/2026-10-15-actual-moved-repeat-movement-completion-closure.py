#!/usr/bin/env python3
"""Own grammar/framing probes; synthetic vectors are never authority."""
import sys,json,copy,importlib.util
from pathlib import Path
sys.dont_write_bytecode=True
R=Path(__file__).resolve().parents[3];spec=importlib.util.spec_from_file_location('r02_closure',R/'docs/specs/verify-combat-actual-moved-repeat-movement-completion-v1.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
row=next(r for r in m.semantic_rows() if r['rootOrdinal']==12 and r['movementCut']==6);case,packet,args=m.example(row);inputs=[x.encode() for x in case['inputCanonicalUtf8']];source=m.source_bytes(dict(packet,lifecycleEventCanonicalUtf8=case['eventCanonicalUtf8']));f,s=m.replay(source,*args,inputs);checks=0
for kind,value in [('Source',f['packet']),('Control',s),('Input',m.command(f,m.initial(f),'stop-element-movement')),('Proof',m.proof(f['packet'],f,s))]:
 good=m.raw(value,kind)
 for bad in [good+b'\n',b' '+good,b'\xef\xbb\xbf'+good,good.replace(b'{',b'{"unknown":null,',1)]:m.reject(lambda:m.parse(bad,kind));checks+=1
 for key in value:
  bad=copy.deepcopy(value);bad.pop(key);m.reject(lambda:m.parse(m.encode(bad),kind));checks+=1
 m.reject(lambda:m.parse(b' '*(m.limit(kind)+1),kind));checks+=1
for bad in [b'{"contractVersion":1,"contractVersion":1}',b'['*42+b'0'+b']'*42,b'['+b','.join([b'0']*513)+b']',b'{'+b','.join(('"k'+str(i)+'":0').encode() for i in range(129))+b'}']:m.reject(lambda:m.parse(bad,'Source'));checks+=1
for field,value in [('contractVersion',True),('contractVersion',2),('expectedPriorVersion',True),('expectedPriorVersion',-1),('expectedPriorVersion',2**63),('kind','foreign')]:
 bad=m.command(f,m.initial(f),'stop-element-movement');bad['command'][field]=value;m.reject(lambda:m.parse(m.encode(bad),'Input'));checks+=1
bad=copy.deepcopy(packet);bad['movementSourceCanonicalUtf8']='{}';m.reject(lambda:m.read_control(b'{}',m.source_bytes(bad),args,[]));checks+=1
for index in range(9):
 bad=copy.deepcopy(args);bad[index+1]=None;m.reject(lambda:m.replay(m.source_bytes(packet),*bad,[]));checks+=1
claim=m.proof(f['packet'],f,s);returned=m.read_proof(m.raw(claim,'Proof'),source,args,inputs);returned['control']['world']['elements'].clear();m.require(m.read_proof(m.raw(claim,'Proof'),source,args,inputs)==claim,6);checks+=1
framing=dict(contractVersion=1,movementSourceCanonicalUtf8='"'*64231936,lifecycleEventCanonicalUtf8=['"'*65536]*3);encoded=m.source_bytes(framing);m.require(len(encoded)==128857183 and len(encoded)<m.I['encodedUpperBounds']['Source'],6);m.reject(lambda:m.replay(encoded,*args,inputs),1);checks+=1
world=copy.deepcopy(s['world']);e=world['elements'][0];refs=[dict(sourceId='x'*128,locator='x'*128)]*512
for origin in [e['readiness']['initialReadinessOrigin'],e['ammunition']['initialAmmunitionOrigin'],e['operationalState']['initialLedgerOrigin'],e['components'][0]['initialToeOrigin']]:origin['references']=copy.deepcopy(refs)
w=m.raw(world,'World');m.require(524288<len(w)<1048576,6);forged=copy.deepcopy(s);forged['world']=world;m.reject(lambda:m.read_control(m.raw(forged,'Control'),source,args,inputs),6);checks+=1
m.require(s['world']==f['parent']['world'] and s['actualProgressRefs']==f['parent']['actualProgressRefs'] and len(s['actualProgressRefs'])==5,6)
print('PASS own grammar/framing/ownership checks',checks,'Source framing',len(encoded),'World framing',len(w),'actual max moves',len(s['actualProgressRefs']),flush=True)
