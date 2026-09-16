import sys, runpy, struct, hashlib
from pathlib import Path
sys.dont_write_bytecode=True
m=runpy.run_path(str(Path('docs/specs/verify-combat-side-projection-v1.py')))
u32=lambda n:struct.pack('>I',n)
u64=lambda n:struct.pack('>Q',n)
def string(value):
    data=value if isinstance(value,bytes) else value.encode('utf-8')
    return u32(len(data))+data
def h(value):
    assert value.startswith('sha256:') and len(value)==71
    return bytes.fromhex(value[7:])
def digest(domain,payload):return hashlib.sha256(domain.encode('ascii')+b'\x00'+payload).digest()
counts=dict(live_views=0,ledger_views=0,sets=0,actions=0)
def check(view,kind):
    counts[kind]+=1
    d=view['decision']
    if d is None:return
    c=view['context']; actions=d['actions']
    candidates=[m['raw3'](a['candidate'],'CycleCandidate3') for a in actions]
    assert candidates==sorted(candidates) and len(candidates)==len(set(candidates))
    payload=u32(1)+string(c['campaignId'])+string(c['audience'])+h(view['cycleRef'])+string(d['kind'])+u64(d['openingRevision'])+string(c['capabilityPolicyId'])+u32(len(candidates))+b''.join(string(v) for v in candidates)
    expected=digest('sandtable.cycle.actions.v1',payload)
    assert h(d['actionSetId'])==expected
    counts['sets']+=1
    for i,a in enumerate(actions):
        assert h(a['actionId'])==digest('sandtable.cycle.action.v1',u32(1)+expected+u32(i))
        counts['actions']+=1
for source in m['source_cases3']():
    for audience in m['SIDES']:
        for view in m['views3'](source,audience):check(view,'live_views')
for source in m['ledger_cases3']():
    for audience in m['SIDES']:
        for view in m['ledger_views3'](source,audience):check(view,'ledger_views')
print('Independent manual BE binary identity reconstruction PASS',counts)
