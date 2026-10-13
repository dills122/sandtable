import copy, hashlib, importlib.util, json, pathlib, subprocess, sys
sys.dont_write_bytecode = True
REPO = pathlib.Path('/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable')
REV = '44cf98656350b29e2f1074f2a9a1b1e634234faa'
SNAP = pathlib.Path('/private/tmp/cs02-44cf986-baseline')
def original(path):
    return subprocess.check_output(['git', 'show', REV + ':' + path], cwd=REPO)
schema_path = 'docs/specs/combat-actual-post-assault-continuation-v1.schema.json'
oracle_path = 'docs/specs/verify-combat-actual-post-assault-continuation-v1.py'
schema = json.loads(original(schema_path))
runtime = set(schema['runtimeSourceHashes']) | {schema_path, oracle_path}
test_paths = {'docs/specs/fixtures/combat-actual-post-assault-continuation-v1.json', 'docs/specs/fixtures/combat-actual-paid-assault-v1.json'}
for path in sorted(runtime | test_paths):
    target = SNAP / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(original(path))
spec = importlib.util.spec_from_file_location('cs02_baseline_pac', SNAP / oracle_path)
pac = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pac)
fixtures = json.loads((SNAP / 'docs/specs/fixtures/combat-actual-post-assault-continuation-v1.json').read_bytes())['traces']
paid = json.loads((SNAP / 'docs/specs/fixtures/combat-actual-paid-assault-v1.json').read_bytes())['cases']
output = {'baseline': REV, 'snapshot': str(SNAP), 'candidateRuntimePaths': len(runtime), 'observations': []}
for row in [r for r in fixtures if r['outcome'] == 'repeat']:
    upstream = next(r for r in paid if (r['owner'], r['firstSeal']) == (row['owner'], row['firstSeal']))
    source, context, terminal, _ = pac.replay(row['sourceCanonicalUtf8'].encode('ascii'), upstream['selectionLedger'], upstream['roundLedger'], upstream['paidLedger'], row['continuationLedger'])
    assert pac.raw(terminal, 'Control').decode('ascii') == row['controlCanonicalUtf8'][-1]
    assert terminal['stateVersion'] == 37 and terminal['status'] == 'repeated' and terminal['activeCycle']['ordinal'] == 2
    owner = row['owner']; own = terminal['members'][0]['unit']; world = terminal['world']
    proof = json.loads(row['proofCanonicalUtf8'])
    position = copy.deepcopy(next(p for p in pac.steps.seq.expected_catalog()['positions'] if p['positionId'] == terminal['positionId']))
    base = dict(contractVersion=1, profile='recon-private-kernel-only', settledStateHash=pac.sha(pac.raw(terminal, 'Control')), settlementCaReceiptId=proof['base']['paidProof']['control']['caCompletionReceiptId'], cycle=terminal['activeCycle'], firstActingSide=context['boundary']['firstActingSide'], position=position, stateVersion=37, prefix=terminal['prefix'], world=copy.deepcopy(world), randomState=terminal['randomState'], attackHistory=terminal['attackHistory'])
    state = dict(contractVersion=1, baseHash=pac.mov.sha(pac.mov.raw(base, 'MovementBase')), stateVersion=37, prefix=terminal['prefix'], world=copy.deepcopy(world), randomState=copy.deepcopy(terminal['randomState']), attackHistory=copy.deepcopy(terminal['attackHistory']), tracks=[], receipts=[])
    moves = []
    adjacency_unsafe = 'assault-west' if owner == 'axis' else 'assault-east'
    safe_destinations = [owner+'-rear', owner+'-supply', owner+'-rear', owner+'-supply']
    for dest in safe_destinations:
        before = copy.deepcopy(state)
        assert not any(r['currentLocationId'] in pac.world.GRAPH[dest] for r in before['world']['representations'] if any(pac.world.SIDES[u] != owner for u in r['boundElementIds']))
        inp = pac.mov.command(base, state, own, dest)
        state, event, receipt = pac.mov.transition(base, state, inp)
        assert before['world']['settlements'] == state['world']['settlements']
        assert before['randomState'] == state['randomState']
        assert before['attackHistory'] == state['attackHistory']
        value = json.loads(event); own_after = next(e for e in state['world']['elements'] if e['elementId'] == own['elementId'])
        moves.append(dict(version=state['stateVersion'], destination=dest, costs=value['effect']['costs'], endedCount=len(value['effect']['endedMemberships']), sourceWorldFitsPaidGrammar=True, cohesion=own_after['operationalState']['cohesionLevel']))
        pac.paid.raw(state['world'], 'World')
        retry, retry_event, retry_receipt = pac.mov.transition(base, state, inp)
        assert retry_event is None and retry_receipt == receipt and retry == state
    rejected = None
    try:
        pac.mov.transition(base, state, pac.mov.command(base, state, own, owner+'-rear'))
    except pac.mov.Invalid as error:
        rejected = error.code
    assert rejected == 'CMB-MOV-005'
    assert state['tracks'][0]['route'] == [('assault-west' if owner == 'axis' else 'assault-east')] + safe_destinations
    assert any(r['currentLocationId'] in pac.world.GRAPH[adjacency_unsafe] for r in world['representations'] if any(pac.world.SIDES[u] != owner for u in r['boundElementIds']))
    output['observations'].append(dict(owner=owner, firstSeal=row['firstSeal'], sourceBytes=len(row['sourceCanonicalUtf8'].encode('ascii')), proofBytes=len(row['proofCanonicalUtf8'].encode('ascii')), repeatPrefix=terminal['prefix'], originalMovementReceipt=terminal['movementEnd']['completionReceiptId'], memberCpRemainsFiveInKernel=True, moves=moves, fifthMoveRejected=rejected, unsafeAssaultReturnRequiresReactionDiscovery=True, safeRouteHasNoAdjacentOpposingRepresentation=True, finalCauseCount=len(state['world']['cohesionCauses'])))
pathlib.Path('/private/tmp/cs02-repeated-movement-probe.json').write_text(json.dumps(output, indent=2) + '\n')
print(json.dumps(output, separators=(',', ':')))
