#!/usr/bin/env python3
"""Independent literal framing checks; native source authenticity audited separately."""
import copy
import hashlib
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
SPEC=ROOT/'docs/specs'
fixture=json.loads((SPEC/'fixtures/combat-exercise-occurrence-v1.json').read_bytes())
schema=json.loads((SPEC/'combat-exercise-occurrence-v1.schema.json').read_bytes())
def encoded(value):return json.dumps(value,ensure_ascii=True,separators=(',',':')).encode('ascii')
def hashed(domain,record):return 'sha256:'+hashlib.sha256(domain.encode('ascii')+b'\0'+encoded(record)).hexdigest()
entries={e['sourceId']:e for e in fixture['entries']}
assert len(entries)==len(fixture['entries'])==134
assert sum(len(e['cuts']) for e in entries.values())==2576
for e in entries.values():
 assert len(e['cuts'])==e['executionTransitions']+1
 assert e['cuts'][0]==e['initialCheckpointHash'] and e['cuts'][-1]==e['finalCheckpointHash']
for path,value in fixture['sourcePins'].items():
 assert 'sha256:'+hashlib.sha256((SPEC/path).read_bytes()).hexdigest()==value
checks=0
for golden in fixture['goldens']:
 source=json.loads(golden['sourceCanonicalJson']);initial=json.loads(golden['initialCheckpointCanonicalJson']);final=json.loads(golden['finalCheckpointCanonicalJson']);request=json.loads(golden['requestCanonicalJson'])
 entry=entries[source['sourceId']];ref=json.loads(source['reference']['canonicalJson'])
 assert encoded(source).decode()==golden['sourceCanonicalJson']
 assert len(encoded(source))==entry['sourceBytes']
 assert hashed(schema['domains']['checkpoint'],initial)==entry['initialCheckpointHash']
 assert hashed(schema['domains']['checkpoint'],final)==entry['finalCheckpointHash']
 cursor=source['executionStart'];fragments=copy.deepcopy(source['fragments'][:cursor['fragmentIndex']+1]);last=fragments[-1]
 last['inputs']=last['inputs'][:cursor['localCut']];last['events']=last['events'][:cursor['localCut']]
 last['terminalJson']=initial['control']['canonicalJson']
 seed=dict(lineageKind=source['lineageKind'],executionProfile=source['executionProfile'],sourceSurface=source['reference']['surface'],sourceFamily=ref['family'],root=source['root'],fragments=fragments,executionStart=cursor)
 assert hashed(schema['domains']['source'],seed)==source['sourceLineageHash']==initial['sourceLineageHash']==final['sourceLineageHash']==request['sourceLineageHash']
 keys=[field.split(':')[0] for field in schema['objects']['ReferenceSeed'].split()]
 material={key:source[key] for key in keys}
 assert hashed(schema['domains']['reference'],material)==source['referenceTranscriptHash']==request['referenceTranscriptHash']
 for cp in (initial,final):
  n=cp['executedTransitions'];execution=dict(inputs=[encoded(i).decode() for i in ref['inputs'][:n]],events=ref['events'][:n])
  assert hashed(schema['domains']['execution'],execution)==cp['executionPrefixHash']
 assert request['initialCheckpointHash']==entry['initialCheckpointHash']
 assert request['initialOccurrence']==initial['sourceOccurrence']
 assert request['requestedTerminal']==entry['requestedTerminal']
 if source['executionProfile']=='historical-terminal-checkpoint':
  assert golden['initialCheckpointCanonicalJson']==golden['finalCheckpointCanonicalJson'] and request['maximumAcceptedTransitions']==0
 checks+=11
print(json.dumps(dict(goldenProfiles=len(fixture['goldens']),literalChecks=checks,entryCutBindings=2576,sourcePins=len(fixture['sourcePins']),status='PASS')))
