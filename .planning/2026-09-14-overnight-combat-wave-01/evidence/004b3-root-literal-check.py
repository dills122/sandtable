#!/usr/bin/env python3
"""Independent B3 retained bytes/count/divergence audit; no native oracle imports."""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];S=ROOT/'docs/specs'
b1=json.loads((S/'combat-exercise-occurrence-v1.schema.json').read_text())
b2=json.loads((S/'combat-exercise-child-evidence-v1.schema.json').read_text())
b3=json.loads((S/'combat-exercise-parent-evidence-v1.schema.json').read_text())
objects={**b1['objects'],**b2['objects'],**b3['objects']}
def ordered(value,kind):
    if kind.endswith('?'):return None if value is None else ordered(value,kind[:-1])
    if kind.endswith('[]'):return [ordered(x,kind[:-2]) for x in value]
    if kind in objects:
        fields=[x.split(':') for x in objects[kind].split()]
        assert set(value)=={k for k,_ in fields},kind
        return {k:ordered(value[k],t) for k,t in fields}
    return value
def raw(value,kind):return json.dumps(ordered(value,kind),ensure_ascii=True,separators=(',',':')).encode('ascii')
def digest(domain,data):return 'sha256:'+hashlib.sha256(domain.encode()+b'\0'+data).hexdigest()
fixture=json.loads((S/'fixtures/combat-exercise-parent-evidence-v1.json').read_bytes())
for path,h in b3['sourcePins'].items():assert 'sha256:'+hashlib.sha256((S/path).read_bytes()).hexdigest()==h
assert fixture['sourcePins']==b3['sourcePins']
children={x['plan']['childId']:json.loads(x['childCanonicalJson']) for x in fixture['children']}
assert len(children)==len(fixture['children'])
parents=compared=unavailable=valid=0
for record in fixture['parents']:
    parent=json.loads(record['parentCanonicalJson']);assert raw(parent,'Parent').decode()==record['parentCanonicalJson']
    d=parent['deterministic'];entries=d['entries'];counts=d['counts'];slots=d['manifest']['children']
    assert parent['fingerprint']==digest(b3['domains']['parent'],raw(d,'Deterministic'))
    assert [x['ordinal'] for x in entries]==list(range(len(entries)))
    assert len(entries)==counts['expected']==len(slots)
    for entry,slot in zip(entries,slots):
        assert entry['childId']==slot['plan']['childId'] and entry['sourceId']==slot['plan']['sourceId']
        if entry['validation']=='validated':
            child=children[entry['childId']];valid+=1
            assert raw(child['manifest'],'Manifest').decode()==slot['manifestCanonicalJson']
            assert entry['childHash']==digest(b2['domains']['child'],raw(child,'Child'))
            expected_actions=[dict(ordinal=i,audience=x['semanticAction']['audience'],actionId=x['semanticAction']['actionId']) for i,x in enumerate(child['steps'])]
            assert entry['actions']==expected_actions and entry['acceptedTransitions']==len(expected_actions)
            assert entry['childStatus']==child['result']['status'] and entry['failure']==child['result']['failure']
        else:
            assert all(v is None for k,v in entry.items() if k not in ('ordinal','childId','sourceId','validation'))
    for state in ('validated','missing','invalid'):assert counts[state]==sum(x['validation']==state for x in entries)
    for state in ('succeeded','failed'):assert counts[state]==sum(x['childStatus']==state for x in entries)
    assert counts['acceptedTransitions']==sum(x['acceptedTransitions'] or 0 for x in entries)
    for pair in d['comparisons']:
        if pair['status']=='unavailable':
            unavailable+=1
            assert all(pair[k] is None for k in ('initialEvidenceHash','baselineActions','candidateActions','firstDivergence','acceptedStepCountDelta','terminalOutcomeEqual','failureCategoryEqual'))
            continue
        compared+=1;left=pair['baselineActions'];right=pair['candidateActions'];first=None
        assert left==entries[pair['baselineEntryOrdinal']]['actions'] and right==entries[pair['candidateEntryOrdinal']]['actions']
        for i in range(max(len(left),len(right))):
            a=left[i] if i<len(left) else None;b=right[i] if i<len(right) else None
            if a!=b:first=dict(ordinal=i,baseline=a,candidate=b);break
        assert pair['firstDivergence']==first
        assert pair['acceptedStepCountDelta']==len(right)-len(left)
    assert counts['comparedPairs']==sum(x['status']=='compared' for x in d['comparisons'])
    assert counts['unavailablePairs']==sum(x['status']=='unavailable' for x in d['comparisons'])
    parents+=1
print('PASS independent B3 literals:',dict(parents=parents,children=len(children),validatedEntries=valid,comparedPairs=compared,unavailablePairs=unavailable,pins=len(b3['sourcePins'])))
