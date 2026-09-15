import copy,hashlib,importlib.util,json,struct,time,sys,subprocess
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path('/Users/dsteele/.codex/worktrees/0b59/sandtable');D=ROOT/'docs/specs';START=time.monotonic()
OUT=Path('/private/tmp/004b2-native-source-audit-results.json')
def sha(x):return 'sha256:'+hashlib.sha256(x).hexdigest()
def enc(x):return json.dumps(x,ensure_ascii=True,separators=(',',':')).encode('ascii')
def frame(xs):return b''.join(struct.pack('>i',len(x))+x for x in xs)
def log(*a):print(round(time.monotonic()-START,3),*a,flush=True)
expected={'combat-exercise-child-evidence-v1.md':'0d2b1e040354495563a40fad47f24441cce9ce65d1e9202a91336fec97255fd8','combat-exercise-child-evidence-v1.schema.json':'1087fd68aaff499421c66f4b73cbfa0471afc08738ccbf70b4fca3c901af5550','verify-combat-exercise-child-evidence-v1.py':'3730e835087a83003d2893b6514aa2e1ff59fcdc1be808a3c765e67fe18d6538'}
for p,h in expected.items():assert sha((D/p).read_bytes())=='sha256:'+h,p
sp=importlib.util.spec_from_file_location('audit_b2',D/'verify-combat-exercise-child-evidence-v1.py');m=importlib.util.module_from_spec(sp);sp.loader.exec_module(m);b=m.b;s=b.side
R={'initialHashes':expected,'checks':{},'sources':[],'limits':['Representative source audit only; author owns exhaustive134-source registry.','No full oracle/gate execution.','Historical588-event preflight retained from B1; no duplicate execution attribution.']}
def add(k,n=1):R['checks'][k]=R['checks'].get(k,0)+n
def save():OUT.write_text(json.dumps(R,indent=2)+'\n')
log('imported frozen B2; warming native B1 catalog')
sources={x['sourceId']:x for x in b.source_cases()};assert len(sources)==134;R['catalogSeconds']=round(time.monotonic()-START,3);log('catalog ready',len(sources))
for p,h in m.INVENTORY['sourcePins'].items():
 assert sha((D/p).read_bytes())==h
 assert subprocess.check_output(['git','show','HEAD:docs/specs/'+p],cwd=ROOT)==(D/p).read_bytes()
 add('historicalDirectPins')
descriptor=dict(contractVersion=1,kind='oracle-source-pins',oracleHash=sha((D/'verify-combat-exercise-child-evidence-v1.py').read_bytes()),schemaHash=sha((D/'combat-exercise-child-evidence-v1.schema.json').read_bytes()),predecessors=[dict(path=p,hash=h) for p,h in sorted(m.INVENTORY['sourcePins'].items())])
expectedbuild=sha(m.INVENTORY['domains']['build'].encode()+b'\0'+enc(descriptor));assert expectedbuild==m.build_hash();add('buildDescriptor')
chosen=['corrected-composition.ordinary.axis.attacker','corrected-composition.ordinary.axis.defender','corrected-composition.defender-capture-guard.axis.attacker','corrected-composition.defender-capture-escape.axis.attacker','corrected-composition.defender-capture-guard.axis.attacker.prior-time','corrected-composition.defender-capture-guard.axis.attacker.fallback','settlement-v2.attacker-capture-escape.commonwealth.defender.fallback-retreat','settlement-v2.attacker-capture-escape.commonwealth.defender.fallback-custody','inherited-reserve.axis','inherited-reserve.axis.fallback','inherited-control.commonwealth.repeat','inherited-control.commonwealth.finish','inherited-control.commonwealth.fallback','historical-terminal.combat-inherited-cycle-control-v1:axis-repeat','historical-terminal.combat-inherited-reserve-movement-completion-v1:commonwealth-reserve-I-movement-completion']
children={}
for name in chosen:
 src=sources[name];surface=src['reference']['surface'];ref=b.decode_reference(src);man=m.manifest_for(src);t=time.monotonic();child=m.run(man,src);children[name]=child
 assert child['result']['failure']==('step-limit-exceeded' if surface=='fallback-v2' else None),(name,child['result'])
 assert len(child['steps'])==len(ref['events'])
 req=json.loads(src['root']['requestJson']) if src['root']['requestJson'] else None
 provenance=dict(setupId=req['setupId'],setupHash=req['setupHash'],contentPackId=req['content']['packId'],contentHash=req['content']['hash'],scenarioId=req['content']['scenarioId']) if req else {k:json.loads(src['root']['boundaryJson'])['cycle'][k] for k in ('setupId','setupHash','contentPackId','contentHash','scenarioId')}
 assert all(man[k]==v for k,v in provenance.items());assert man['buildHash']==expectedbuild;add('nativeProvenance')
 initial=b.checkpoint(src);final=b.checkpoint(src,len(ref['events']));assert child['initialCheckpointCanonicalJson'].encode()==b.raw(initial,'Checkpoint') and child['finalCheckpointCanonicalJson'].encode()==b.raw(final,'Checkpoint');assert child['sourceCanonicalJson'].encode()==b.raw(src,'Source');add('nativeSourceCheckpointLiterals',3)
 assert man['seedHash']==sha(b.raw(initial['randomState'],'Random'));add('nativeSeed')
 if surface=='historical-v3':
  assert not child['steps'] and initial==final;assert all(v['decision'] is None for v in b.observe(src,0).values());add('historicalZeroExecution');
 else:
  ctx=s.source_context3b(ref) if surface=='corrected-v3' else ref['base'] if surface=='fallback-v2' else None
  if ctx:a=len(ctx['predecessor']['inputs']);z=a+len(ctx['roundInputs'])
  for cut,step in enumerate(child['steps']):
   # Source expected state/family comes from accepted B1/native readers, never B2 kernel.
   family,expectedafter=b.native_state(surface,ref,cut+1)
   if surface=='inherited-v3':
    if cut<3:module=s.irr3;base=ref['base']['release'];prior=module.initial(base) if cut==0 else b.native_state(surface,ref,cut)[1]
    else:module=s.icc3;base=ref['base']['control'];prior=module.initial(base) if cut==3 else b.native_state(surface,ref,cut)[1]
   else:
    if cut<a:module=s.steps;base=ctx['base']['boundary'];begin=0
    elif cut<z:module=s.rnd2;base=ctx['base'];begin=a
    elif surface=='corrected-v3' and cut>=len(ref['events'])-4:module=s.bridge3b;base=ref['base'];begin=len(ref['events'])-4
    else:module=s.res2;base=ctx;begin=z
    prior=module.initial(base) if cut==begin else b.native_state(surface,ref,cut)[1]
   inp=json.loads(step['trustedInputJson']);assert inp==ref['inputs'][cut];assert step['authorityFamily']==family and step['ordinal']==cut
   after,event,rid=module.transition(base,prior,inp);assert after==expectedafter and event==ref['events'][cut] and event.decode()==step['eventCanonicalJson'] and rid==step['authorityReceiptId'];add('nativeTransitions')
   assert step['successorCheckpointHash']==b.checkpoint_hash(b.checkpoint(src,cut+1));add('successorCheckpointBindings')
   wrapped=json.loads(event);e=json.loads(wrapped['nativeEvent']) if 'nativeEvent' in wrapped else wrapped
   assert step['eventAuthor']==e.get('author')
   if family=='bridge':assert rid==wrapped['receiptId'] and rid.startswith('rcfr.') and rid!=e['receiptId'];add('bridgeOuterReceipt')
   effect=e['effect'];cmd=e['input']['command'];scope=e.get('roundId') if family in ('round','result') else e.get('releaseId') if 'releaseId' in e else e.get('controlId') if 'controlId' in e else e.get('segmentId',e.get('cycleId'))
   disposition=effect['payload']['kind'] if family=='result' and effect['kind'] in ('disposition-recorded','custody-settled') else effect['choice'] if effect['kind']=='unit-disposition' else None
   key=dict(authorityFamily=family,scopeId=scope,decisionId=cmd.get('decisionId'),effectKind=effect['kind'],reason=effect.get('reason'),disposition=disposition)
   assert m.system_key(family,event)==key;add('nativeSystemKeys');
   if disposition is not None:add('nativeDispositions')
   views=b.observe(src,cut);eligible=[owner for owner in s.SIDES if views[owner]['decision'] is not None]
   if len(eligible)==2:
    actor=views[eligible[0]]['context']['cycle']['phasingSide'];eligible=[actor,next(x for x in s.SIDES if x!=actor)]
    if man['request']['schedule']['roleOrder']=='defender-first':eligible.reverse()
   assert step['schedule']==dict(contractVersion=1,eligibleAudiences=eligible,selectedAudience=eligible[0] if eligible else 'system',dualSlot=len(eligible)==2);add('nativePublicSchedule')
   if step['sideProposalJson']:
    proposal=json.loads(step['sideProposalJson']);outcome=json.loads(step['outcomeJson']);owner=step['initiator'];view=views[owner]
    act=next(x for x in view['decision']['actions'] if x['actionId']==proposal['actionId']);assert proposal['candidate']==act['candidate'] and owner==inp['actor']
    fn=s.admit_a2 if surface=='fallback-v2' else s.admit_a3 if surface=='inherited-v3' else s.admit_a3b
    assert fn(s.prefix(ref,cut),owner,step['sideProposalJson'].encode(),inp['admittedAt'],inp['clockAvailable'])==outcome;add('nativeOwnerOutcome')
    if outcome['status']=='accepted':assert step['semanticAction']==dict(audience=owner,actionId=proposal['actionId']) and outcome['receipt'] is not None;add('ownerAccepted')
    else:assert outcome['receipt'] is None and e['author']=='system' and step['semanticAction']['audience']=='system';add('ownerFallback')
   else:assert step['initiator']=='system' and step['outcomeJson'] is None;add('systemInitiator')
   if step['semanticAction']['audience']=='system':assert step['semanticAction']['actionId']==sha(m.INVENTORY['domains']['system'].encode()+b'\0'+enc(key));add('independentSystemHash')
 assert b.satisfies(final,man['request']['requestedTerminal'])==(surface!='fallback-v2');add('nativeTerminal')
 for kind in ('reconstruction','readjudication'):
  proof=child[kind]
  if surface=='fallback-v2':assert proof is None
  else:
   assert proof['verified'] and proof['failure'] is None
   assert proof['transcriptHash']==sha(m.INVENTORY['domains']['transcript'].encode()+b'\0'+frame([m.raw(x,'AcceptedStep') for x in child['steps']]))
   assert proof['eventHash']==sha(m.INVENTORY['domains']['events'].encode()+b'\0'+frame([x['eventCanonicalJson'].encode() for x in child['steps']]))
   add('independentFramedProof')
 R['sources'].append(dict(sourceId=name,status=child['result']['status'],steps=len(child['steps']),seconds=round(time.monotonic()-t,3),childHash=m.child_hash(child)));save();log('representative passed',name,len(child['steps']))
# Fixture-independent record hashes. No generated_fixture/exhaustive registry execution.
log('representative native checks complete; waiting retained fixture if needed')
waitstart=time.monotonic()
while not m.FIXTURE.exists():
 assert time.monotonic()-waitstart<900,'fixture did not arrive within15min'
 time.sleep(15)
fixturedata=m.FIXTURE.read_bytes();fixture=json.loads(fixturedata)
assert fixturedata==(json.dumps(fixture,ensure_ascii=True,indent=2)+'\n').encode('ascii');assert fixture['sourcePins']==m.INVENTORY['sourcePins']
summaries={x['sourceId']:x for x in fixture['summaries']}
for name,child in children.items():assert summaries[name]['childHash']==m.child_hash(child);add('retainedRepresentativeSummary')
for golden in fixture['goldens']:
 child=m.parse(golden['childCanonicalJson'].encode(),'Child');src=sources[child['manifest']['sourceId']]
 assert m.child_hash(child)==summaries[golden['name']]['childHash'];add('goldenChildHash')
 assert child['manifest']['buildHash']==expectedbuild
 if child['sourceCanonicalJson'] is not None:
  assert child['sourceCanonicalJson'].encode()==b.raw(src,'Source')
  assert child['initialCheckpointCanonicalJson'].encode()==b.raw(b.checkpoint(src),'Checkpoint')
  assert child['finalCheckpointCanonicalJson'].encode()==b.raw(b.checkpoint(src,len(child['steps'])),'Checkpoint');add('goldenSourceCheckpointLiterals',3)
 for item in child['artifactManifest']['entries']:
  path=item['path']
  if path=='run-manifest.json':content=m.raw(child['manifest'],'Manifest')
  elif path=='result.json':content=m.raw(child['result'],'Result')
  elif path=='checks.bin':content=frame([m.raw(x,'Check') for x in child['checks']])
  elif path=='accepted-steps.bin':content=frame([m.raw(x,'AcceptedStep') for x in child['steps']])
  elif path=='source.json':content=child['sourceCanonicalJson'].encode()
  elif path=='initial-checkpoint.json':content=child['initialCheckpointCanonicalJson'].encode()
  elif path=='final-checkpoint.json':content=child['finalCheckpointCanonicalJson'].encode()
  elif path=='failed-attempt.json':content=m.raw(child['failedAttempt'],'FailedAttempt')
  else:content=m.raw(child[path.removesuffix('.json')],'Proof')
  assert item['bytes']==len(content) and item['hash']==sha(content);add('independentArtifactEntry')
 assert len({x['path'] for x in child['artifactManifest']['entries']})==len(child['artifactManifest']['entries']);assert 'artifact-manifest.json' not in {x['path'] for x in child['artifactManifest']['entries']};add('artifactSelfExclusion')
R['fixture']=dict(sha256=sha(fixturedata),bytes=len(fixturedata),summaries=len(summaries),goldens=len(fixture['goldens']),isolatedWitnesses=len(fixture['isolatedCompletionWitnesses']),maxGoldenBytes=max(len(x['childCanonicalJson'].encode()) for x in fixture['goldens']))
R['finalHashes']={p:sha((D/p).read_bytes()) for p in expected};assert R['finalHashes']=={p:'sha256:'+h for p,h in expected.items()}
R['elapsedSeconds']=round(time.monotonic()-START,3);R['status']='PASS: no unresolved native source discrepancy in representative bounded audit';save();log('FINAL PASS',R['checks'],R['fixture'])
