#!/usr/bin/env python3
"""Independent reference replay for the additive actual Reaction-resolved entry."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
def load(name, file):
    spec = importlib.util.spec_from_file_location(name, ROOT/file)
    mod = importlib.util.module_from_spec(spec); spec.loader.exec_module(mod); return mod
cl = load('rce_closure', 'verify-combat-inherited-reaction-closure-v1.py')
pen = load('rce_inventory', 'verify-combat-positive-entry-v1.py')
lc, bd = pen.lc, pen.bd
encode, sha = pen.encode, pen.sha
NAME = 'combat-actual-reaction-combat-entry-v1'
SCHEMA = json.loads((ROOT/(NAME+'.schema.json')).read_text())
FIXTURE = ROOT/'fixtures'/(NAME+'.json')
def require(ok, reason):
    if not ok: raise ValueError(reason)
def text(value):
    require(type(value) is str and 0 < len(value) <= 1048576 and value.isascii(), 'text-bound')
    return value.encode('ascii')
def parse_source(data):
    require(type(data) is bytes and 0 < len(data) <= 1048576, 'byte-bound')
    def pairs(items):
        out = {}
        for k,v in items:
            require(k not in out, 'duplicate-key'); out[k] = v
        return out
    packet = json.loads(data, object_pairs_hook=pairs)
    require(list(packet) == SCHEMA['sourceFields'] and type(packet['contractVersion']) is int and packet['contractVersion'] == 1, 'source-inventory')
    text(packet['createdCanonicalUtf8'])
    for key, count in [('prefixEventCanonicalUtf8',13),('lifecycleEventCanonicalUtf8',3),('breakdownEventCanonicalUtf8',1)]:
        values = packet[key]; require(type(values) is list and len(values) <= count, 'count-bound')
        for v in values: text(v)
    require(len(packet['prefixEventCanonicalUtf8']) == 13, 'prefix-count')
    require(not packet['breakdownEventCanonicalUtf8'] or len(packet['lifecycleEventCanonicalUtf8']) == 3, 'suffix-order')
    require(encode(packet) == data, 'noncanonical'); return packet
def authenticate(events, trusted):
    require(type(trusted) is list and len(events) == len(trusted), 'trusted-count')
    for e,i in zip(events,trusted):
        require(type(i) is bytes and encode(json.loads(e)['input']) == i, 'trusted-input')
def replay(data, request, prefix_inputs, lifecycle_inputs, breakdown_inputs):
    packet = parse_source(data)
    prefix = [text(v) for v in packet['prefixEventCanonicalUtf8']]
    le = [text(v) for v in packet['lifecycleEventCanonicalUtf8']]
    be = [text(v) for v in packet['breakdownEventCanonicalUtf8']]
    authenticate(prefix,prefix_inputs);authenticate(le,lifecycle_inputs);authenticate(be,breakdown_inputs)
    require(request['randomState']['seed'] == 1, 'seed-profile')
    state = cl.replay(request, text(packet['createdCanonicalUtf8']), prefix[:4], prefix[4:5],prefix[5:9],prefix[9:10],prefix[10:11],prefix[11:12],prefix[12:13])
    require(state['stateVersion'] == 14 and state['reactionWindow'] is None and state['breakdownFlow']['kind'] == 'moving', 'closure-profile')
    require(state['randomState']['seed'] == 1 and state['randomState']['nextByteCursor'] == 2 and state['operationStageWeather'][0]['kind'] == 'normal', 'rng-weather')
    state = copy.deepcopy(state);state.pop('currentPosition');state.pop('reactionWindow')
    state.update(interruptContext=None,movementEnd=None,breakdownCompletionReceiptId=None)
    for ev,inp in zip(le,lifecycle_inputs):
        state,expected = lc._emit(state, lc.parse(inp,'LifecycleInput'))
        state['breakdownCompletionReceiptId'] = None
        require(expected == ev, 'lifecycle-event')
    if be:
        state,expected = bd._emit(state,bd.parse(breakdown_inputs[0],'EntryInput'))
        require(expected == be[0], 'breakdown-event')
    control = pen.raw(state,'CombatEntryState')
    candidate = None
    if be:
        candidate = pen.steps.candidate(dict(cycle=state['cycle'],world=state['world'],creationBinding=state['creationBinding'],weather=dict(attackerKind='normal',defenderKind='normal')))
        require(candidate is not None,'positive-geometry')
        candidate = pen.canonical(candidate,'Candidate')
    proof = encode(dict(contractVersion=1,sourceHash=sha(data),entry=json.loads(control),candidate=candidate,
        finalBreakdownEventCanonicalUtf8=be[0].decode('ascii') if be else None,
        finalBreakdownEventHash=sha(be[0]) if be else None,
        finalBreakdownReceiptId=state['breakdownCompletionReceiptId']))
    command = None
    if not be:
        inp = lc.command(state,lc.KINDS[len(le)]) if len(le)<3 else bd.command(state)
        command = lc.raw(inp,'LifecycleInput') if len(le)<3 else bd.raw(inp,'EntryInput')
    return state,control,proof,command
def generate():
    cases=[]
    for case in json.loads(cl.FIXTURE.read_text())['cases']:
        result=cl.trace(case);require(cl.goldens(result)==case['goldens'],'closure-golden')
        args,inp,event,_,_ = result;request,created,*groups = args
        prefix=[e for group in groups for e in group]+[event]
        inputs=[encode(json.loads(e)['input']) for e in prefix]
        packet=dict(contractVersion=1,createdCanonicalUtf8=created.decode('ascii'),
            prefixEventCanonicalUtf8=[e.decode('ascii') for e in prefix],lifecycleEventCanonicalUtf8=[],breakdownEventCanonicalUtf8=[])
        cuts=[]; lifecycle=[]; breakdown=[]; commands=[];events=[]
        for i in range(5):
            data=encode(packet);state,control,proof,command=replay(data,request,inputs,lifecycle,breakdown)
            cuts.append(dict(sourceCanonicalUtf8=data.decode('ascii'),controlCanonicalUtf8=control.decode('ascii'),proofCanonicalUtf8=proof.decode('ascii')))
            if i==4:break
            commands.append(command.decode('ascii'))
            if i<3:
                next_state,ev=lc._emit(state,lc.parse(command,'LifecycleInput'))
                packet['lifecycleEventCanonicalUtf8'].append(ev.decode('ascii'));lifecycle.append(command)
            else:
                next_state,ev=bd._emit(state,bd.parse(command,'EntryInput'))
                packet['breakdownEventCanonicalUtf8'].append(ev.decode('ascii'));breakdown.append(command)
            events.append(ev.decode('ascii'))
        cases.append(dict(name=case['name'],requestCanonicalUtf8=encode(request).decode('ascii'),prefixInputCanonicalUtf8=[i.decode('ascii') for i in inputs],
            inputCanonicalUtf8=commands,eventCanonicalUtf8=events,cuts=cuts))
    return dict(contract=NAME,contractVersion=1,cases=cases)
def negatives(fixture):
    rejected=0
    def reject(fn):
        nonlocal rejected
        try: fn()
        except ValueError: rejected += 1
        else: raise AssertionError('forged authority admitted')
    for case in fixture['cases']:
        request=json.loads(case['requestCanonicalUtf8'])
        prefix=[text(v) for v in case['prefixInputCanonicalUtf8']]
        inputs=[text(v) for v in case['inputCanonicalUtf8']]
        for i,cut in enumerate(case['cuts']):
            data=text(cut['sourceCanonicalUtf8']);le=inputs[:min(i,3)];be=inputs[3:] if i==4 else []
            reject(lambda:replay(data,request,prefix[:-1],le,be))
            reject(lambda:replay(data,request,[b'{}']+prefix[1:],le,be))
            bad=parse_source(data);bad['world']={}
            reject(lambda:replay(encode(bad),request,prefix,le,be))
            reject(lambda:replay(b' '+data,request,prefix,le,be))
            if le: reject(lambda:replay(data,request,prefix,[],be))
            if be: reject(lambda:replay(data,request,prefix,le,[]))
    return rejected
def main():
    repo=ROOT.parent.parent
    for path,digest in SCHEMA['dependencies'].items():
        require(hashlib.sha256((repo/path).read_bytes()).hexdigest()==digest,'dependency:'+path)
    fixture=generate()
    if '--write' in sys.argv:
        FIXTURE.write_bytes(encode(fixture)+b'\n')
    else:
        require(json.loads(FIXTURE.read_bytes())==fixture,'fixture-parity')
    count=negatives(fixture)
    print(f'PASS actual Reaction Combat entry: 6 traces,30 cuts,24 events,{count} rejects; reference only')
if __name__=='__main__':main()
