#!/usr/bin/env python3
"""Sequence5/cycle byte contract oracle; no live cycle or Rules10 admission."""
import copy
import hashlib
import itertools
import json
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
REPO = ROOT.parent.parent
FIXTURE = ROOT / 'fixtures/combat-cycle-sequence-v1.json'
SCHEMA = {k:[tuple(f.split(':')) for f in v.split()] for k,v in json.loads((ROOT/'combat-cycle-sequence-v1.schema.json').read_text())['objects'].items()}
STEPS = ['position-determination','barrage','retreat-before-assault','force-assignment','anti-armor','close-assault']
SLOTS = ['first-acting-side','second-acting-side']
SIDES = ['axis','commonwealth']
MAX_BYTES = 1048576


class Invalid(ValueError):
    def __init__(self, code, path=''):
        self.code,self.path=f'CMB-SEQ-{code:03}',path
        super().__init__(f'{self.code} {path or "/"}')


def require(test, code, path=''):
    if not test: raise Invalid(code,path)


def encode(value):return json.dumps(value,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(raw):return 'sha256:'+hashlib.sha256(raw).hexdigest()


AUTH_FIELDS=[('contractVersion','U32'),('campaignId','S'),('rulesetHash','S'),('setupId','S'),('setupHash','S'),('contentPackId','S'),('contentHash','S'),('scenarioId','S'),('gameTurn','U64'),('operationStage','U64'),('playerPhaseSlot','S'),('actingSide','S'),('ordinal','U64'),('openedAuthorityVersion','U64'),('openingPrefix','H'),('admittedPolicyBundleDigest','H')]
PUBLIC_FIELDS=[(n,e) for n,e in AUTH_FIELDS if n in ('contractVersion','campaignId','rulesetHash','gameTurn','operationStage','playerPhaseSlot','actingSide','ordinal')]
DOMAINS={k:'sandtable.cycle.'+v+'.v1' for k,v in [('authority','authority'),('public','public'),('creationPrefix','prefix.creation'),('eventPrefix','prefix.event')]}
OCC_FIELDS=[('contractVersion','U32'),('cycleId','H'),('positionId','S')]


def expected_catalog():
    predecessor=json.loads(FIXTURE.read_text())['predecessor']
    raw=predecessor['canonicalUtf8'].encode()
    require(sha(raw)=='sha256:42b4e721252ddf5f1107927d12b3a7f0c7ed956b08c766a3b9e2d28e3601ab9c',4)
    catalog=json.loads(raw);catalog['schemaVersion']=5
    for p in catalog['positions']+catalog['interruptPositions']:p['contractVersion']=5
    cycles=[]
    # Derive graph endpoints from actual catalog membership, not string-built golden endpoints.
    for stage in range(1,4):
        for slot in SLOTS:
            positions=[p for p in catalog['positions'] if p['operationStage']==stage and p['actorRole']==slot]
            phase=lambda token:next(p['positionId'] for p in positions if p['phaseId']=='land.phase.'+token)
            segment=lambda token:next(p['positionId'] for p in positions if p['segmentId']=='land.segment.'+token)
            combat=[p['positionId'] for p in positions if p['segmentId']=='land.segment.combat']
            cycles.append(dict(operationStage=stage,playerPhaseSlot=slot,entryFromPositionId=phase('reserve-designation'),movementPositionId=segment('movement'),breakdownPositionId=segment('breakdown-determination'),combatPositionIds=combat,releasePositionId=segment('reserve-release'),finishPositionId=phase('truck-convoy-movement')))
    catalog['cycles']=cycles
    return catalog


def expected_codec():
    fields=lambda items:[dict(name=n,encoding=e) for n,e in items]
    return dict(schemaVersion=1,artifactId='cna-1979.1.cycle-identity.v1',identityVersion=1,domains=DOMAINS,
                authorityFields=fields(AUTH_FIELDS),publicFields=fields(PUBLIC_FIELDS),occurrenceFields=fields(OCC_FIELDS),
                creationPrefixFields=['U64(canonicalCreationByteLength)','canonicalCreationBytes'],
                eventPrefixFields=['H(priorPrefix)','U64(canonicalEventByteLength)','canonicalEventBytes'],
                bounds=dict(identifierBytes=128,canonicalRecordBytes=MAX_BYTES,gameTurnMinimum=1,gameTurnMaximum=111,operationStageMinimum=1,operationStageMaximum=3,ordinalMinimum=1,ordinalMaximum=2147483647,authorityVersionMinimum=1,authorityVersionMaximum=9223372036854775807),
                openingPrefix='before-opening-event',sources=[dict(sourceId='sandtable-rules-lab',locator='CYCLE-DES-001:CYCLE-COMP-001-002')]+[dict(sourceId='spi-1979-land-rules',locator=l) for l in ['5.2','7.11','7.14']])


def typed(v,kind,path='',depth=0):
    require(depth<=32,1,path)
    if kind.endswith('?'):
        if v is not None:typed(v,kind[:-1],path,depth)
    elif kind in SCHEMA:
        require(type(v)is dict,1,path)
        for k,child in SCHEMA[kind]:
            require(k in v,1,path+'/'+k);typed(v[k],child,path+'/'+k,depth+1)
        require(set(v)=={k for k,_ in SCHEMA[kind]},1,path)
    elif kind.endswith('[]'):
        require(type(v)is list and len(v)<=512,1,path)
        for i,item in enumerate(v):typed(item,kind[:-2],f'{path}/{i}',depth+1)
    elif kind in ('int','long'):
        require(type(v)is int,1,path)
        bits=32 if kind=='int' else 64
        require(-(2**(bits-1))<=v<2**(bits-1),2,path)
    else:
        require(type(v)is str,1,path)
        pattern={'id':r'[A-Za-z0-9][A-Za-z0-9._:-]{0,127}','hash':r'sha256:[0-9a-f]{64}','rawHash':r'[0-9a-f]{64}','text':r'[ -~]{1,512}'}[kind]
        require(re.fullmatch(pattern,v)is not None,2,path)


def compare(actual,expected,path=''):
    require(type(actual)is type(expected),4 if actual is None or expected is None else 1,path)
    if type(expected)is dict:
        require(set(actual)==set(expected),1,path)
        for key,value in expected.items():compare(actual[key],value,path+'/'+key)
    elif type(expected)is list:
        require(len(actual)==len(expected),4,path)
        for i,(a,e) in enumerate(zip(actual,expected)):compare(a,e,f'{path}/{i}')
    else:require(actual==expected,4,path)


def read_artifact(raw,kind):
    require(kind in ('Catalog','Codec'),3)
    require(type(raw)is bytes and 0<len(raw)<=MAX_BYTES,1)
    def pairs(items):
        result={}
        for k,v in items:
            require(k not in result,1);result[k]=v
        return result
    try:value=json.loads(raw.decode('utf-8'),object_pairs_hook=pairs,parse_constant=lambda _:require(False,1))
    except (ValueError,UnicodeError,RecursionError) as e:raise Invalid(1) from e
    if kind=='Catalog':typed(value,kind)
    require(type(value)is dict and 'schemaVersion' in value,1)
    require(type(value['schemaVersion'])is int,1,'/schemaVersion')
    require(value['schemaVersion']==(5 if kind=='Catalog' else 1),3,'/schemaVersion')
    expected=expected_catalog() if kind=='Catalog' else expected_codec()
    compare(value,expected)
    require(raw==encode(expected),8)
    return value


def validate_identity(value,kind,first_side):
    require(kind in ('Authority','Public'),3)
    typed(value,kind)
    require(value['contractVersion']==1,3,'/contractVersion')
    for key,lo,hi in [('gameTurn',1,111),('operationStage',1,3),('ordinal',1,2147483647)]:
        require(lo<=value[key]<=hi,2,'/'+key)
    if kind=='Authority':require(value['openedAuthorityVersion']>=1,2,'/openedAuthorityVersion')
    require(first_side in SIDES,3)
    require(value['playerPhaseSlot'] in SLOTS,3,'/playerPhaseSlot')
    side=first_side if value['playerPhaseSlot']==SLOTS[0] else SIDES[1-SIDES.index(first_side)]
    require(value['actingSide']==side,3,'/actingSide')


def number(value,width):return value.to_bytes(width,'big')
def domain(key,payload):return DOMAINS[key].encode('ascii')+b'\x00'+payload


def tuple_bytes(value,fields):
    parts=[]
    for name,encoding in fields:
        v=value[name]
        if encoding in ('U32','U64'):parts.append(number(v,4 if encoding=='U32' else 8))
        elif encoding=='H':parts.append(bytes.fromhex(v[7:]))
        else:
            raw=v.encode('utf-8');parts.extend([number(len(raw),4),raw])
    return b''.join(parts)


def identity(value,kind,first_side):
    validate_identity(value,kind,first_side)
    return domain(kind.lower(),tuple_bytes(value,AUTH_FIELDS if kind=='Authority' else PUBLIC_FIELDS))


def read_identity(raw,kind,first_side):
    require(kind in ('Authority','Public'),3)
    tag=domain(kind.lower(),b'')
    require(type(raw)is bytes and len(raw)<=MAX_BYTES and raw.startswith(tag),1)
    offset=len(tag)
    def take(length):
        nonlocal offset
        require(offset+length<=len(raw),1)
        part=raw[offset:offset+length];offset+=length;return part
    value={}
    for name,encoding in AUTH_FIELDS if kind=='Authority' else PUBLIC_FIELDS:
        if encoding in ('U32','U64'):v=int.from_bytes(take(4 if encoding=='U32' else 8),'big')
        elif encoding=='H':v='sha256:'+take(32).hex()
        else:
            length=int.from_bytes(take(4),'big');require(1<=length<=128,2,'/'+name)
            try:v=take(length).decode('utf-8')
            except UnicodeError as e:raise Invalid(1,'/'+name) from e
        value[name]=v
    require(offset==len(raw),1)
    validate_identity(value,kind,first_side)
    return value


def record_bytes(raw):require(type(raw)is bytes and 0<len(raw)<=MAX_BYTES,1)


def prefix_creation(raw):
    record_bytes(raw)
    return sha(domain('creationPrefix',number(len(raw),8)+raw))


def prefix_event(prior,raw):
    typed(prior,'hash');record_bytes(raw)
    return sha(domain('eventPrefix',bytes.fromhex(prior[7:])+number(len(raw),8)+raw))


def materialize(position,turn,holder,first_side):
    typed(position,'Position');typed(turn,'int')
    require(1<=turn<=111,2)
    require(holder in SIDES and first_side in SIDES,3)
    template=next((p for p in expected_catalog()['positions'] if p['positionId']==position['positionId']),None)
    require(template is not None and position==template,4)
    roles={'none':None,'commonwealth':'commonwealth','initiative-holder':holder,'first-acting-side':first_side,'second-acting-side':SIDES[1-SIDES.index(first_side)]}
    return position|dict(gameTurn=turn,activeSide=roles[position['actorRole']])


def occurrence(value,cycle,first_side):
    typed(value,'Occurrence');require(value['contractVersion']==1,3)
    require(value['cycleId']==sha(identity(cycle,'Authority',first_side)),5,'/cycleId')
    edge=next(e for e in expected_catalog()['cycles'] if e['operationStage']==cycle['operationStage'] and e['playerPhaseSlot']==cycle['playerPhaseSlot'])
    positions=[edge[k] for k in ['movementPositionId','breakdownPositionId','releasePositionId','finishPositionId']]+edge['combatPositionIds']+['land.position.breakdown-stop']
    require(value['positionId'] in positions,5,'/positionId')
    return tuple_bytes(value,OCC_FIELDS)


def next_ordinal(value):
    typed(value,'int');require(1<=value<2147483647,2)
    return value+1


def rejects(call,code=None):
    try:call()
    except Invalid as e:
        assert code is None or e.code==code,(e.code,code,e.path)
        return
    raise AssertionError('invalid input accepted')


def set_path(obj,path,value):
    parts=path.strip('/').split('/'); node=obj
    for p in parts[:-1]:node=node[int(p)] if type(node)is list else node[p]
    node[int(parts[-1]) if type(node)is list else parts[-1]]=value


def main():
    f=json.loads(FIXTURE.read_text())
    for path,h in f['sourceHashes'].items():assert sha((REPO/path).read_bytes())==h,path
    old=f['predecessor']; oldraw=old['canonicalUtf8'].encode()
    assert len(oldraw)==old['byteCount'] and sha(oldraw)==old['sha256']=='sha256:42b4e721252ddf5f1107927d12b3a7f0c7ed956b08c766a3b9e2d28e3601ab9c'
    values={}
    for g in f['goldens']:
        raw=g['canonicalUtf8'].encode();assert len(raw)==g['byteCount'] and sha(raw)==g['sha256']
        values[g['kind']]=read_artifact(raw,g['kind'])
    assert len(values['Catalog']['positions'])==112 and len(values['Catalog']['cycles'])==6
    base=f['frames'][0]['input'];public=f['frames'][1]['input']
    for g in f['frames']:
        raw=identity(g['input'],g['kind'].title(),'axis')
        assert raw.hex()==g['preimageHex'] and sha(raw)==g['sha256']
        assert read_identity(raw,g['kind'].title(),'axis')==g['input']
        for bad in [raw+b'\x00',raw[:-1],b'\x00'+raw,raw.replace(b'.v1\x00',b'.v2\x00',1),b'\xff'*10]:
            rejects(lambda:read_identity(bad,g['kind'].title(),'axis'))
    authority_raw=identity(base,'Authority','axis')
    length_offset=len(domain('authority',b''))+4
    for size in [0,4294967295]:
        bad=authority_raw[:length_offset]+number(size,4)+authority_raw[length_offset+4:]
        rejects(lambda:read_identity(bad,'Authority','axis'),'CMB-SEQ-002')
    bad=authority_raw[:length_offset+4]+b'\xff'+authority_raw[length_offset+5:]
    rejects(lambda:read_identity(bad,'Authority','axis'),'CMB-SEQ-001')
    for length in [1,128]:
        v=base|{'campaignId':'a'*length,'openedAuthorityVersion':9223372036854775807}
        assert read_identity(identity(v,'Authority','axis'),'Authority','axis')==v
    rejects(lambda:identity(base|{'unknown':1},'Authority','axis'),'CMB-SEQ-001')
    rejects(lambda:identity({k:v for k,v in base.items() if k!='setupHash'},'Authority','axis'),'CMB-SEQ-001')
    for v in f['negativeVectors']:
        obj=copy.deepcopy(values['Catalog'] if v['kind']=='Catalog' else base if v['kind']=='Authority' else public)
        set_path(obj,v['path'],v['value'])
        rejects(lambda:read_artifact(encode(obj),'Catalog') if v['kind']=='Catalog' else identity(obj,v['kind'],'axis'),v['error'])
    for kind,obj in values.items():
        raw=encode(obj)
        for bad in [raw+b'\n',b'\xef\xbb\xbf'+raw,raw+b'{}',b'\xff',b'{"schemaVersion":5,'+raw[1:],raw.replace(b':5,',b':5.0,',1) if kind=='Catalog' else raw.replace(b':1,',b':1e0,',1),encode(dict(reversed(list(obj.items())))),b' '*1048577]:
            rejects(lambda:read_artifact(bad,kind))
    probe=f['framingOnly'];assert sha((REPO/probe['legacyIdentityPath']).read_bytes())==probe['legacyIdentityHash']
    creation=probe['creationUtf8'].encode();events=[s.encode() for s in probe['eventsUtf8']]
    p0=prefix_creation(creation);p1=prefix_event(p0,events[0]);p2=prefix_event(p1,events[1])
    assert [p0,p1,p2]==probe['prefixes']
    assert prefix_event(prefix_event(p0,events[1]),events[0])!=p2
    assert prefix_event(p0,events[0]+events[1])!=p2
    assert prefix_creation(events[0])!=prefix_event(p0,events[0])
    # Different private prefixes affect authority identity, never public occurrence.
    fork=base|{'openingPrefix':p2}
    assert identity(fork,'Authority','axis')!=identity(base,'Authority','axis')
    assert identity({k:fork[k] for k in public},'Public','axis')==identity(public,'Public','axis')
    for key,value in [('setupHash',sha(b'other-setup')),('contentHash',sha(b'other-content')),('openedAuthorityVersion',13),('admittedPolicyBundleDigest',sha(b'other-policy'))]:
        altered=base|{key:value};assert identity(altered,'Authority','axis')!=identity(base,'Authority','axis')
        assert identity({k:altered[k] for k in public},'Public','axis')==identity(public,'Public','axis')
    for raw in [b'',b'x'*(MAX_BYTES+1),'text']:
        rejects(lambda:prefix_creation(raw));rejects(lambda:prefix_event(p0,raw))
    assert len(prefix_creation(b'x'*MAX_BYTES))==71
    oc=f['occurrence'];assert occurrence(oc['input'],base,'axis').hex()==oc['bytesHex']
    rejects(lambda:occurrence(oc['input']|{'cycleId':sha(b'other')},base,'axis'))
    rejects(lambda:occurrence(oc['input']|{'positionId':values['Catalog']['cycles'][1]['movementPositionId']},base,'axis'))
    rejects(lambda:identity(base|{'actingSide':'commonwealth'},'Authority','axis'))
    # Every scope and role mapping plus first/repeat/max ordinal has distinct public identity.
    hashes=set();scope_count=0
    for turn,stage,slot,first,ordinal in itertools.product(range(1,112),range(1,4),SLOTS,SIDES,[1,2,2147483647]):
        side=first if slot==SLOTS[0] else SIDES[1-SIDES.index(first)]
        v=public|dict(gameTurn=turn,operationStage=stage,playerPhaseSlot=slot,actingSide=side,ordinal=ordinal)
        b=identity(v,'Public',first);assert read_identity(b,'Public',first)==v
        hashes.add(sha(b));scope_count+=1
    assert len(hashes)==scope_count==3996
    assert next_ordinal(1)==2 and next_ordinal(2147483646)==2147483647
    rejects(lambda:next_ordinal(2147483647),'CMB-SEQ-002')
    positions=values['Catalog']['positions']; materialized=0
    for turn,holder,first,p in itertools.product([1,111],SIDES,SIDES,positions):
        actual=materialize(p,turn,holder,first)
        assert actual['gameTurn']==turn and actual['contractVersion']==5
        if p['actorRole']=='first-acting-side':assert actual['activeSide']==first
        if p['actorRole']=='second-acting-side':assert actual['activeSide']!=first
        materialized+=1
    assert materialized==896
    for edge in values['Catalog']['cycles']:
        release=next(i for i,p in enumerate(positions) if p['positionId']==edge['releasePositionId'])
        assert positions[release+1]['positionId']==edge['finishPositionId']
        assert positions[release-8]['positionId']==edge['movementPositionId']
    print(f'PASS 112 catalog positions, 1 interrupt, 6 cycle edges; 2 artifact and 2 identity goldens; '
          f'{len(f["negativeVectors"])} mutations; {scope_count} scope/ordinal identities; '
          f'{materialized} actor materializations; prefix forks/framing and occurrence negatives. No runtime replay.')


if __name__=='__main__':main()
