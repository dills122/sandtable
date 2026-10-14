#!/usr/bin/env python3
"""Bounded read-only C01 probe; never implements or admits successor completion."""
import copy, hashlib, importlib.util, json, platform, subprocess, sys, time
from pathlib import Path
sys.dont_write_bytecode = True
REPO = '/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable'
COMMIT = 'c43b9caec452c152a086412c779bb6495ef1a0ad'
SNAPSHOT = Path('/private/tmp/movement-completion-c43b9ca-baseline')
RESULT = Path('/private/tmp/movement-completion-probe.json')
def git_bytes(path):
    return subprocess.check_output(['git', '-C', REPO, 'show', COMMIT + ':' + path])
schema_path = 'docs/specs/combat-actual-repeated-movement-v1.schema.json'
oracle_path = 'docs/specs/verify-combat-actual-repeated-movement-v1.py'
inventory = json.loads(git_bytes(schema_path))
paths = sorted(set(inventory['runtimeSourceHashes']) | set(inventory['testArtifactHashes']) | {
    schema_path, oracle_path, 'docs/specs/fixtures/combat-actual-repeated-movement-v1.json'})
for path in paths:
    target = SNAPSHOT/path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(git_bytes(path))
spec = importlib.util.spec_from_file_location('c01_armv', SNAPSHOT/oracle_path)
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
fixture = json.loads((SNAPSHOT/'docs/specs/fixtures/combat-actual-repeated-movement-v1.json').read_bytes())
roots = m.roots()
rows = fixture['traces']
out = dict(commit=COMMIT, python=sys.version, platform=platform.platform(), snapshot=str(SNAPSHOT),
           exportedFiles=len(paths), externalPins=len(inventory['runtimeSourceHashes']),
           method='Immutable retained sources, independent five ledgers, existing public ARMV replay; pure expiry kernel only. No successor ingress or lifecycle authority claimed.', observations=[])
started = time.monotonic()
for index, (row, upstream) in enumerate(roots):
    retained = next(r for r in rows if (r['owner'],r['firstSeal']) == (row['owner'],row['firstSeal']))
    full_source = json.loads(retained['sourceCanonicalUtf8'])
    counts = range(5)
    for count in counts:
        source = copy.deepcopy(full_source)
        source['movementEventCanonicalUtf8'] = source['movementEventCanonicalUtf8'][:count]
        source_bytes = m.raw(source,'Source')
        args = m.arguments(row,upstream)
        _, ctx, state, ledger = m.replay(source_bytes,*args,retained['movementLedger'][:count])
        assert m.raw(state,'Control').decode('ascii') == retained['controlCanonicalUtf8'][count]
        expiry_input = dict(contractVersion=1,cycle=copy.deepcopy(state['activeCycle']),
            firstActingSide=ctx['firstActingSide'],priorProof=copy.deepcopy(state['movementEnd']),
            completionReceiptId='probe.actual-completion',endLocations=m.policy.locations(state['world']),
            members=copy.deepcopy(state['members']))
        expiry = m.policy.expire_movement(expiry_input)
        assert expiry['members'] == state['members']
        assert expiry['proof']['ordinal'] == 2
        assert expiry['proof']['completionReceiptId'] == 'probe.actual-completion'
        assert len(expiry['proof']['endLocations']) == len(state['movementEnd']['endLocations']) == 2
        element = next(e for e in state['world']['elements'] if e['elementId']==state['members'][0]['unit']['elementId'])
        out['observations'].append(dict(owner=row['owner'],firstSeal=row['firstSeal'],moves=count,
            stateVersion=state['stateVersion'],positionId=state['positionId'],location=element['currentLocationId'],
            cp=state['members'][0]['spentCp'],cohesion=element['operationalState']['cohesionLevel'],
            tracks=state['tracks'],receiptCount=len(state['receipts']),progressCount=len(state['nextCycleProgress']),
            oldProof=state['movementEnd'],previewProof=expiry['proof'],membersUnchanged=True,
            sourceBytes=len(source_bytes),controlBytes=len(m.raw(state,'Control'))))
        RESULT.write_text(json.dumps(out,indent=2)+'\n')
out['wallSeconds']=round(time.monotonic()-started,3)
out['note']='probe.actual-completion is an explicit pure-kernel marker, never an accepted completion receipt; lifecycle transitions are not implemented or asserted.'
RESULT.write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps(dict(result=str(RESULT),snapshot=str(SNAPSHOT),rows=len(out['observations']),wallSeconds=out['wallSeconds'],externalPins=out['externalPins'],exportedFiles=out['exportedFiles'])))
