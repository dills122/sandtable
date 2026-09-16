import ast,copy,hashlib,json,struct,time
from pathlib import Path
R=Path('/Users/dsteele/.codex/worktrees/0b59/sandtable');D=R/'docs/specs';E=R/'.planning/2026-09-14-overnight-combat-wave-01/evidence';start=time.monotonic()
def enc(v):return json.dumps(v,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(v):return 'sha256:'+hashlib.sha256(v).hexdigest()
def framed(xs):return b''.join(struct.pack('>i',len(x))+x for x in xs)
schema=json.loads((D/'combat-exercise-child-evidence-v1.schema.json').read_bytes());b1schema=json.loads((D/'combat-exercise-occurrence-v1.schema.json').read_bytes())
objects={**b1schema['objects'],**schema['objects']}
def ordered(x,t):
 if t.endswith('?'):return None if x is None else ordered(x,t[:-1])
 if t.endswith('[]'):return [ordered(v,t[:-2]) for v in x]
 if t in objects:return {k:ordered(x[k],a) for k,a in (f.split(':') for f in objects[t].split())}
 return x
def raw(x,t):return enc(ordered(x,t))
def digest(domain,x,t):return sha(domain.encode()+b'\0'+raw(x,t))
fixturedata=(D/'fixtures/combat-exercise-child-evidence-v1.json').read_bytes();f=json.loads(fixturedata);assert len(f['summaries'])==134 and len(f['goldens'])==12,'final134summary/12childfixture absent'
assert fixturedata==(json.dumps(f,indent=2,ensure_ascii=True)+'\n').encode('ascii')
b1=json.loads((D/'fixtures/combat-exercise-occurrence-v1.json').read_bytes());entries={x['sourceId']:x for x in b1['entries']};summaries={x['sourceId']:x for x in f['summaries']};assert list(summaries)==list(entries)
checks={}
def add(k,n=1):checks[k]=checks.get(k,0)+n
for x in f['summaries']:
 assert list(x)==[k.split(':')[0] for k in schema['objects']['SourceSummary'].split()]
 e=entries[x['sourceId']]
 for k in ('sourceLineageHash','referenceTranscriptHash'):assert x[k]==e[k]
 assert x['retainedTransitions']==e['executionTransitions'];assert 'childHash' not in x;add('B1SummaryCorrespondence')
for p,h in schema['sourcePins'].items():assert sha((D/p).read_bytes())==h;add('directSourcePins')
assert f['sourcePins']==schema['sourcePins']
old=ast.parse((E/'004b2-pre-admission-oracle.py').read_text());new=ast.parse((D/'verify-combat-exercise-child-evidence-v1.py').read_text())
functions=lambda t:{n.name:n for n in t.body if isinstance(n,ast.FunctionDef)}
a,b=functions(old),functions(new);unchanged=[k for k in a if k in b and ast.dump(a[k],include_attributes=False)==ast.dump(b[k],include_attributes=False)];assert len(unchanged)==44
# Normal native execution unchanged; only injected FailedAttempt representation differs.
x=copy.deepcopy(b['execute']);assigns=[n for n in ast.walk(x) if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='attempt' for t in n.targets)]
oldassign=next(n for n in ast.walk(a['execute']) if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='attempt' for t in n.targets) and isinstance(n.value,ast.Call))
for n in assigns:
 if isinstance(n.value,ast.Call):n.value=copy.deepcopy(oldassign.value)
assert ast.dump(x,include_attributes=False)==ast.dump(a['execute'],include_attributes=False);add('normalExecutionAST')
for k in ('kernel','fresh_views','owner_input','system_key','outcome_for','reconstruct','controller','run','source_provenance'):assert k in unchanged
sources={}
for g in b1['goldens']:sources[g['sourceId']]=json.loads(g['sourceCanonicalJson'])
goldens=[]
for g in f['goldens']:
 c=json.loads(g['childCanonicalJson']);assert raw(c,'Child')==g['childCanonicalJson'].encode();assert digest(schema['domains']['child'],c,'Child')==g['childHash'];add('canonicalChildIdentity')
 if c['sourceCanonicalJson'] is None:
  assert c['result']['failure']=='admission-rejected' and not c['steps'] and c['initialCheckpointCanonicalJson'] is None and c['finalCheckpointCanonicalJson'] is None
 else:
  src=json.loads(c['sourceCanonicalJson']);assert raw(src,'Source')==c['sourceCanonicalJson'].encode();sources[src['sourceId']]=src
  e=entries[src['sourceId']];count=len(c['steps']);assert c['result']['acceptedTransitions']==count
  for field,cut in (('initialCheckpointCanonicalJson',0),('finalCheckpointCanonicalJson',count)):
   cp=json.loads(c[field]);assert raw(cp,'Checkpoint')==c[field].encode();assert digest(b1schema['domains']['checkpoint'],cp,'Checkpoint')==e['cuts'][cut];add('B1CheckpointCutBinding')
  req=json.loads(src['root']['requestJson']) if src['root']['requestJson'] else None
  provenance=dict(setupId=req['setupId'],setupHash=req['setupHash'],contentPackId=req['content']['packId'],contentHash=req['content']['hash'],scenarioId=req['content']['scenarioId']) if req else {k:json.loads(src['root']['boundaryJson'])['cycle'][k] for k in ('setupId','setupHash','contentPackId','contentHash','scenarioId')}
  assert all(c['manifest'][k]==v for k,v in provenance.items());add('literalNativeProvenance')
  ref=json.loads(src['reference']['canonicalJson'])
  for i,step in enumerate(c['steps']):
   assert step['ordinal']==i and step['eventCanonicalJson']==ref['events'][i] and json.loads(step['trustedInputJson'])==ref['inputs'][i]
   assert step['successorCheckpointHash']==e['cuts'][i+1];add('retainedStepNativeReference')
 for proof in ('reconstruction','readjudication'):
  p=c[proof]
  if p is None:continue
  assert p['eventHash']==sha(schema['domains']['events'].encode()+b'\0'+framed([x['eventCanonicalJson'].encode() for x in c['steps']]))
  assert p['transcriptHash']==sha(schema['domains']['transcript'].encode()+b'\0'+framed([raw(x,'AcceptedStep') for x in c['steps']]))
  assert p['verified']==(p['failure'] is None);add('retainedFramedProof')
 goldens.append(dict(name=g['name'],sourceId=c['manifest']['sourceId'],status=c['result']['status'],failure=c['result']['failure'],steps=len(c['steps'])))
for name,src in sources.items():
 material={k:v for k,v in src.items() if k not in ('sourceLineageHash','referenceTranscriptHash')};assert digest(b1schema['domains']['reference'],material,'ReferenceSeed')==summaries[name]['referenceTranscriptHash']
 ref=json.loads(src['reference']['canonicalJson']);assert len(ref['events'])==summaries[name]['retainedTransitions']
 assert summaries[name]['referenceEventHash']==sha(schema['domains']['events'].encode()+b'\0'+framed([e.encode('ascii') for e in ref['events']]));add('fullLiteralSourceReferenceAndEventHash')
files=['combat-exercise-child-evidence-v1.md','combat-exercise-child-evidence-v1.schema.json','verify-combat-exercise-child-evidence-v1.py','fixtures/combat-exercise-child-evidence-v1.json']
result=dict(status='PASS bounded final source/literal reconciliation',checks=checks,unchangedFunctions=unchanged,sourceLiteralIds=list(sources),goldens=goldens,hashes={p:sha((D/p).read_bytes()) for p in files},fixtureBytes=len(fixturedata),elapsedSeconds=round(time.monotonic()-start,3),limitations=['No new cold134-source native execution; prior15-source/209-transition native audit applies by AST correspondence.','All134 summary identities/counts compared with accepted B1 literals; event hashes independently recomputed only for complete source literals listed here.','Author final focused native reader checks own exhaustive134-summary generation; root owns independent full literal/proof inventory audit.'])
Path('/private/tmp/004b2-final-source-reconciliation-results.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2))
