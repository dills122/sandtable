#!/usr/bin/env python3
"""Independent retained B2 bytes/hash audit; no native oracle imports."""
import hashlib,json,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
SPEC=ROOT/'docs/specs'
def load(name):return json.loads((SPEC/name).read_bytes())
b1=load('combat-exercise-occurrence-v1.schema.json')
b2=load('combat-exercise-child-evidence-v1.schema.json')
objects={**b1['objects'],**b2['objects']}
def ordered(value,kind):
    if kind.endswith('?'):return None if value is None else ordered(value,kind[:-1])
    if kind.endswith('[]'):return [ordered(item,kind[:-2]) for item in value]
    if kind in objects:
        fields=[item.split(':') for item in objects[kind].split()]
        assert set(value)=={name for name,_ in fields},kind
        return {name:ordered(value[name],field) for name,field in fields}
    return value
def raw(value,kind):return json.dumps(ordered(value,kind),ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(data):return 'sha256:'+hashlib.sha256(data).hexdigest()
def framed(records):return b''.join(struct.pack('>i',len(record))+record for record in records)
def digest(domain,data):return sha(domain.encode('ascii')+b'\0'+data)
fixture=load('fixtures/combat-exercise-child-evidence-v1.json')
descriptor={'contractVersion':1,'kind':'oracle-source-pins','oracleHash':sha((SPEC/'verify-combat-exercise-child-evidence-v1.py').read_bytes()),'schemaHash':sha((SPEC/'combat-exercise-child-evidence-v1.schema.json').read_bytes()),'predecessors':[{'path':path,'hash':value} for path,value in sorted(b2['sourcePins'].items())]}
expected_build=digest(b2['domains']['build'],raw(descriptor,'BuildDescriptor'))
counts=dict(sourceSummaries=0,sourceEvents=0,goldens=0,proofs=0,inventory=0,steps=0,pins=0)
for path,value in b2['sourcePins'].items():
    assert sha((SPEC/path).read_bytes())==value
    counts['pins']+=1
summaries={item['sourceId']:item for item in fixture['summaries']}
assert len(summaries)==len(fixture['summaries'])==134
prior={item['sourceId']:item for item in load('fixtures/combat-exercise-occurrence-v1.json')['entries']}
assert set(prior)==set(summaries)
for name,item in summaries.items():
    assert item['sourceLineageHash']==prior[name]['sourceLineageHash']
    assert item['referenceTranscriptHash']==prior[name]['referenceTranscriptHash']
    assert item['retainedTransitions']==prior[name]['executionTransitions']
    raw(item,'SourceSummary');counts['sourceSummaries']+=1
for golden in fixture['goldens']:
    child=json.loads(golden['childCanonicalJson']);assert raw(child,'Child').decode()==golden['childCanonicalJson']
    summary=summaries[child['manifest']['sourceId']]
    assert digest(b2['domains']['child'],raw(child,'Child'))==golden['childHash']
    manifest=child['manifest'];steps=child['steps'];result=child['result']
    assert manifest['buildHash']==expected_build
    if child['sourceCanonicalJson'] is not None:
        source=json.loads(child['sourceCanonicalJson']);root=source['root']
        reference=json.loads(source['reference']['canonicalJson'])
        assert len(reference['events'])==summary['retainedTransitions']
        assert digest(b2['domains']['events'],framed([event.encode('ascii') for event in reference['events']]))==summary['referenceEventHash']
        counts['sourceEvents']+=len(reference['events'])
        if root['requestJson'] is not None:
            provenance=json.loads(root['requestJson']);content=provenance['content']
            expected={'setupId':provenance['setupId'],'setupHash':provenance['setupHash'],'contentPackId':content['packId'],'contentHash':content['hash'],'scenarioId':content['scenarioId']}
        else:
            cycle=json.loads(root['boundaryJson'])['cycle'];expected={key:cycle[key] for key in ('setupId','setupHash','contentPackId','contentHash','scenarioId')}
        assert all(manifest[key]==value for key,value in expected.items())
        for field,target in [('initialCheckpointCanonicalJson','initialCheckpointHash'),('finalCheckpointCanonicalJson','finalCheckpointHash')]:
            assert digest(b1['domains']['checkpoint'],child[field].encode('ascii'))==result[target]
    assert len(steps)==result['acceptedTransitions']<=summary['retainedTransitions']
    assert [step['ordinal'] for step in steps]==list(range(len(steps)))
    assert result['expectedFailureMatch']==(result['failure'] is not None and result['failure']==manifest['expectedFailure'])
    records=[raw(step,'AcceptedStep') for step in steps];events=[step['eventCanonicalJson'].encode('ascii') for step in steps]
    transcript=digest(b2['domains']['transcript'],framed(records));event_hash=digest(b2['domains']['events'],framed(events))
    manifest_hash=digest(b2['domains']['manifest'],raw(manifest,'Manifest'))
    for name in ['reconstruction','readjudication']:
        proof=child[name]
        if proof is None:continue
        assert proof['kind']==name and proof['manifestHash']==manifest_hash
        assert proof['acceptedTransitions']==len(steps)
        assert proof['transcriptHash']==transcript and proof['eventHash']==event_hash
        assert proof['initialCheckpointHash']==result['initialCheckpointHash']
        assert proof['finalCheckpointHash']==result['finalCheckpointHash']
        source=json.loads(child['sourceCanonicalJson'])
        assert proof['sourceLineageHash']==source['sourceLineageHash']
        assert proof['referenceTranscriptHash']==source['referenceTranscriptHash']
        if proof['verified']:
            assert proof['failure'] is None and proof['attemptedTranscriptHash']==transcript
            assert proof['observedTranscriptHash']==transcript
            assert proof['observedFinalCheckpointHash']==proof['finalCheckpointHash']
        else:assert proof['failure']==result['failure']
        counts['proofs']+=1
    files={'run-manifest.json':raw(manifest,'Manifest'),'result.json':raw(result,'Result'),'checks.bin':framed([raw(item,'Check') for item in child['checks']]),'accepted-steps.bin':framed(records)}
    for field,path in [('sourceCanonicalJson','source.json'),('initialCheckpointCanonicalJson','initial-checkpoint.json'),('finalCheckpointCanonicalJson','final-checkpoint.json')]:
        if child[field] is not None:files[path]=child[field].encode('ascii')
    if child['failedAttempt'] is not None:
        attempt=child['failedAttempt'];assert attempt['atTransition']==len(steps)
        assert attempt['trustedClock']==manifest['trustedSchedule'][attempt['atTransition']]
        assert attempt['initiator']==attempt['trustedClock']['actor']
        assert 'trustedInputJson' not in attempt
        outcome=json.loads(attempt['outcomeJson']);assert outcome['status']=='rejected' and outcome['receipt'] is None
        files['failed-attempt.json']=raw(attempt,'FailedAttempt')
    for name in ['reconstruction','readjudication']:
        if child[name] is not None:files[name+'.json']=raw(child[name],'Proof')
    entries=child['artifactManifest']['entries']
    assert len(entries)==len(files) and {item['path'] for item in entries}==set(files)
    assert 'artifact-manifest.json' not in files
    schemes={'run-manifest.json':'manifest','result.json':'result','checks.bin':'checks','accepted-steps.bin':'transcript','source.json':'source','initial-checkpoint.json':'checkpoint','final-checkpoint.json':'checkpoint','failed-attempt.json':'attempt','reconstruction.json':'reconstruction','readjudication.json':'readjudication'}
    for item in entries:
        data=files[item['path']];assert item['bytes']==len(data) and item['hash']==sha(data)
        assert item['scheme']==schemes[item['path']]
        counts['inventory']+=1
    if result['status']=='succeeded':
        assert result['failure'] is None and child['reconstruction']['verified'] and child['readjudication']['verified']
    if result['failure'] in ['step-limit-exceeded','cancelled']:assert child['failedAttempt'] is None and child['reconstruction'] is None and child['readjudication'] is None
    if result['failure']=='admission-rejected':assert not steps and not child['checks'] and child['failedAttempt'] is None and child['reconstruction'] is None and child['readjudication'] is None
    if result['failure']=='reconstruction-failed':assert not child['reconstruction']['verified'] and child['readjudication'] is None
    if result['failure']=='readjudication-failed':assert child['reconstruction']['verified'] and not child['readjudication']['verified']
    counts['steps']+=len(steps);counts['goldens']+=1
print('PASS independent B2 canonical records, domain/framed hashes, proof bindings and exact inventory',counts)
