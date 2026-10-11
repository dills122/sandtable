#!/usr/bin/env python3
"""Private admission-only actual assault assessment. No paid transition or new cache."""
import argparse
import copy
import hashlib
import importlib.util
import itertools
import json
import re
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
REPOSITORY = ROOT.parent.parent
INVENTORY = json.loads((ROOT/'combat-actual-assault-v1.schema.json').read_text())
DEPENDENCIES = INVENTORY['runtimeSourceHashes']
TEST_DEPENDENCIES = INVENTORY['testSourceHashes']
FIXTURE = ROOT/'fixtures/combat-actual-assault-v1.json'
MAX_BYTES = INVENTORY['limits']['bytes']
COORDINATES = tuple(10*a+b for a in range(1,7) for b in range(1,7))
DIFFERENTIALS = tuple(range(-2,3))

class Invalid(ValueError):
    def __init__(self, code, path=''):
        self.code, self.path = f'CMB-AAD-{code:03}', path
        super().__init__(self.code+' '+(path or '/'))

def require(ok, code, path=''):
    if not ok: raise Invalid(code,path)

def encode(value):
    return json.dumps(value,ensure_ascii=True,separators=(',',':')).encode('ascii')

def sha(data): return 'sha256:'+hashlib.sha256(data).hexdigest()
def dependency_bytes(path): return (REPOSITORY/path).read_bytes()

def consume(path, hashes=None):
    hashes = DEPENDENCIES if hashes is None else hashes
    try: data=dependency_bytes(path)
    except (OSError,KeyError) as error: raise Invalid(9,path) from error
    require(path in hashes and sha(data)=='sha256:'+hashes[path],9,path)
    return data

def verify_dependencies():
    for path in DEPENDENCIES: consume(path)

verify_dependencies()
spec=importlib.util.spec_from_file_location('actual_round_entry',ROOT/'verify-combat-actual-round-entry-v1.py')
are=importlib.util.module_from_spec(spec);spec.loader.exec_module(are)
require(all(DEPENDENCIES.get(p)==h for p,h in are.DEPENDENCIES.items()),9,'/inheritedPins')
SCHEMA={k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
RECORD_SCHEMA={k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['catalogue'].items()}
GRAMMAR=are.SCHEMA|SCHEMA|RECORD_SCHEMA
VERSION_RULES=INVENTORY['versions']
FLAGS=('paidAdmissionEnabled','nativeSettlementCertified','authenticSettlementCertified')
KINDS={'morale':'MoraleRow','assault-cell':'AssaultCell','capture-refusal':'VariantRow'}

def typed(value,kind,path='',depth=0,legacy=False):
    require(depth<=32,1,path)
    legacy=legacy or (kind in are.selection.pe.SCHEMA and kind not in are.OWN_SCHEMA
                     and kind not in are.selection.OWN_SCHEMA and kind not in SCHEMA and kind not in RECORD_SCHEMA)
    grammar=are.selection.pe.SCHEMA if legacy else GRAMMAR
    if kind.endswith('?'):
        if value is not None: typed(value,kind[:-1],path,depth,legacy)
    elif kind in ('RoundEffect','Effect'):
        tags=are.INVENTORY['effectTags'] if kind=='RoundEffect' else are.selection.TAGS
        require(type(value) is dict and type(value.get('kind')) is str,1,path)
        require(value['kind'] in tags,3,path)
        typed(value,tags[value['kind']],path,depth)
    elif kind in grammar:
        fields=grammar[kind]
        require(type(value) is dict and set(value)=={k for k,_ in fields},1,path)
        for key,child in fields: typed(value[key],child,path+'/'+key,depth+1,legacy)
        version_keys={key for key,child in fields if key in ('contractVersion','schemaVersion','catalogueVersion')}
        require(version_keys==set(VERSION_RULES.get(kind,{})),1,path+'/versionGrammar')
        for key,expected in VERSION_RULES.get(kind,{}).items():
            require(value[key]==expected,3,path+'/'+key)
        if kind=='ArithmeticAssessment':
            require(value['scopeKind']==INVENTORY['scopeKind'] and all(value[x] is False for x in FLAGS),2,path)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value)<=512,1,path)
        for index,child in enumerate(value):typed(child,kind[:-2],f'{path}/{index}',depth+1,legacy)
    elif kind in ('int','long','ulong','utc','futureTurn'):
        bounds={'int':(-2**31 if legacy else 0,2**31-1),
                'long':(-2**63 if legacy else 0,2**63-1),'ulong':(0,2**64-1),
                'utc':(0,are.MAX_UTC),'futureTurn':(1,115)}
        lo,hi=bounds[kind];require(type(value) is int,1,path);require(lo<=value<=hi,2,path)
    elif kind in ('adjustment','differential','die'):
        lo,hi={'adjustment':(-1,1),'differential':(-2,2),'die':(1,6)}[kind]
        require(type(value) is int,1,path);require(lo<=value<=hi,2,path)
    else:
        try:
            if legacy and kind in ('id','hash','rawHash','locator','text','string','side','bool'):
                are.selection.env.typed(value,kind,path,depth)
            elif legacy:are.selection.pe.typed(value,kind,depth)
            else:are.typed(value,kind,path,depth)
        except ValueError as error:
            code=int(error.code[-3:]) if hasattr(error,'code') else 1
            raise Invalid(code if code in (1,2,3) else 1,path) from error

def canonical(value,kind):
    if kind.endswith('?'): return None if value is None else canonical(value,kind[:-1])
    if kind in SCHEMA or kind in RECORD_SCHEMA:
        return {k:canonical(value[k],t) for k,t in (SCHEMA|RECORD_SCHEMA)[kind]}
    return are.canonical(value,kind)

def raw(value,kind):
    typed(value,kind);data=encode(canonical(value,kind));require(len(data)<=MAX_BYTES,1);return data

def parse(data,kind):
    require(type(data) is bytes and 0<len(data)<=MAX_BYTES,1)
    def pairs(items):
        value={}
        for key,child in items:
            require(key not in value,1,'/'+key);value[key]=child
        return value
    try:
        value=json.loads(data.decode('ascii'),object_pairs_hook=pairs,
                        parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as error:
        if isinstance(error,Invalid):raise
        raise Invalid(1) from error
    require(raw(value,kind)==data,8);return value

def manifest_hash(): return sha(encode(dict(sorted(DEPENDENCIES.items()))))
def source_digest(source):
    return hashlib.sha256(INVENTORY['domains']['source'].encode('ascii')+b'\0'+raw(source,'AdmissionSource')).hexdigest()

def ledgers(selection_ledger,round_ledger,assault_ledger):
    for label,ledger in (('selection',selection_ledger),('round',round_ledger)):
        require(type(ledger) is list and len(ledger)<=16,1,'/'+label+'Ledger')
    require(type(assault_ledger) is list,1,'/assaultLedger')
    require(not assault_ledger,4,'/assaultLedger')
    owned=[]
    for label,ledger,kind in (('selection',selection_ledger,'Input'),('round',round_ledger,'RoundInput')):
        items=[]
        for index,item in enumerate(ledger):
            typed(item,kind,f'/{label}Ledger/{index}',1)
            items.append(canonical(item,kind))
        require(len(encode(items))<=MAX_BYTES,1,'/'+label+'Ledger')
        owned.append(copy.deepcopy(items))
    return tuple(owned)

def admitted(data,selection_ledger,round_ledger,assault_ledger):
    verify_dependencies();source=parse(data,'AdmissionSource')
    selection,round_inputs=ledgers(selection_ledger,round_ledger,assault_ledger)
    try:
        original=are.read_source(source['actualRoundSourceCanonicalUtf8'].encode('ascii'),selection,round_inputs)
        prior=are.proof(original,selection,round_inputs)
    except OSError as error:
        verify_dependencies()
        raise Invalid(9,'/actualRoundSourceCanonicalUtf8') from error
    except ValueError as error:
        if str(getattr(error,'code','')).endswith('-009'):
            raise Invalid(9,getattr(error,'path','/actualRoundSourceCanonicalUtf8')) from error
        verify_dependencies()
        raise Invalid(4,'/actualRoundSourceCanonicalUtf8') from error
    control=prior['control']
    require(control['stateVersion']==25 and control['stepIndex']==5 and control['status']=='prepared'
            and not control['closed'] and len(control['slots'])==2
            and all(x['sealedReceiptId'] is not None for x in control['slots'])
            and len(control['stepReceipts'])==5 and len(original['roundEventCanonicalUtf8'])==5
            and control['cancellationReceiptId'] is None and control['commitmentId'] is None
            and not control['attackHistory'] and not control['targetUses'],4,'/preparedBoundary')
    # ARE warm recovery must not hide physical Content changes at this material boundary.
    consume('docs/specs/fixtures/combat-content-v7.canonical.json')
    verify_dependencies()
    return source,prior

def normalized_rules():
    # The raw source and grammar are enforced immediately at use, even with inherited module caches.
    consume('docs/research/fixtures/combat-selected-source-v1.json')
    consume('docs/specs/verify-combat-rules-inputs-v1.py')
    consume('docs/specs/combat-rules-inputs-v1.schema.json')
    inputs=are.selection.st.inputs
    try:
        rules=inputs.expected_rules();inputs.typed(rules,'RulesInput')
        data=inputs.encode(inputs.canonical(rules,'RulesInput'))
    except OSError as error:
        verify_dependencies()
        raise Invalid(9,'/rulesInput') from error
    except (ValueError,AssertionError) as error:
        verify_dependencies()
        raise Invalid(6,'/rulesInput') from error
    verify_dependencies()
    require(sha(data)==INVENTORY['rulesInputHash']==are.selection.env.Context().config['rulesInputHash'],6,'/rulesInputHash')
    return rules

def catalogue_header(rules):
    return dict(kind='pure-selected-arithmetic-catalogue-v1',catalogueVersion=1,
                rulesInputHash=INVENTORY['rulesInputHash'],dependencyManifestHash=manifest_hash(),
                moralePairs=1296,assaultPairs=6480,captureRefusalVariants=8840,totalRecords=16616)

def catalogue_records(rules):
    """Indexed pure arithmetic only; no World or actual settlement materialization."""
    morale={x['coordinate']:x['adjustment'] for x in rules['morale']}
    losses={(x['role'],x['differential'],x['coordinate']):x['lossPercent'] for x in rules['losses']}
    effects={x['differential']:x for x in rules['effects']}
    shares={x['die']:x['percent'] for x in rules['captureShares']}
    ordinal=0
    for ac,dc in itertools.product(COORDINATES,repeat=2):
        yield dict(kind='morale',ordinal=ordinal,attackerMoraleCoordinate=ac,defenderMoraleCoordinate=dc,
                   attackerAdjustment=morale[ac],defenderAdjustment=morale[dc],differential=morale[ac]-morale[dc]);ordinal+=1
    def cell(diff,ac,dc):
        effect=effects[diff];asum=ac//10+ac%10;dsum=dc//10+dc%10
        ca=asum in effect['attackerCaptureSums'];cd=dsum in effect['defenderCaptureSums']
        require(not(ca and cd),6,'/captureRole')
        return dict(differential=diff,attackerCoordinate=ac,defenderCoordinate=dc,
                    attackerPercent=losses[('attacker',diff,ac)],defenderPercent=losses[('defender',diff,dc)],
                    rawEngaged=asum in effect['attackerEngagedSums'],
                    requiredRetreat=int(dsum in effect['defenderRetreatOneHexSums']),
                    capturedRole='attacker' if ca else 'defender' if cd else None)
    for diff,ac,dc in itertools.product(DIFFERENTIALS,COORDINATES,COORDINATES):
        facts=cell(diff,ac,dc)
        yield dict(kind='assault-cell',ordinal=ordinal,**facts,captureTriggered=facts['capturedRole'] is not None);ordinal+=1
    for diff,ac,dc in itertools.product(DIFFERENTIALS,COORDINATES,COORDINATES):
        values=cell(diff,ac,dc)
        for die in range(1,7) if values['capturedRole'] else (None,):
            facts=dict(differential=diff,attackerCoordinate=ac,defenderCoordinate=dc,captureDie=die,
                       attackerPercent=values['attackerPercent'],defenderPercent=values['defenderPercent'],
                       rawEngaged=values['rawEngaged'],requiredRetreat=values['requiredRetreat'],
                       capturedRole=values['capturedRole'],captureShare=shares[die] if die else 0)
            for refusal in range(facts['requiredRetreat']+1):
                a=(10*facts['attackerPercent']+99)//100;d=10*(facts['defenderPercent']+10*refusal)//100
                ca=(a*facts['captureShare']+99)//100 if facts['capturedRole']=='attacker' else 0
                cd=(d*facts['captureShare']+99)//100 if facts['capturedRole']=='defender' else 0
                require(0<=ca<=a<=3 and 0<=cd<=d<=3,6,'/conservation')
                yield dict(kind='capture-refusal',ordinal=ordinal,differential=diff,attackerCoordinate=ac,
                    defenderCoordinate=dc,captureDie=die,refusalHexes=refusal,facts=facts,
                    attackerLossToe=a,defenderLossToe=d,attackerCapturedToe=ca,defenderCapturedToe=cd,
                    attackerOtherLossToe=a-ca,defenderOtherLossToe=d-cd,attackerRemainingToe=10-a,
                    defenderRemainingToe=10-d,attackerLossDp=3 if a>=3 else 0,defenderLossDp=3 if d>=3 else 0);ordinal+=1

def catalogue_hash(rules,records=None):
    """Stream, and compare any supplied test stream to independently indexed expected records."""
    header=catalogue_header(rules);typed(header,'header')
    digest=hashlib.sha256(INVENTORY['domains']['catalogue'].encode('ascii')+b'\0'+encode(header)+b'\n')
    expected=catalogue_records(rules);candidate=None if records is None else iter(records);end=object()
    counts={k:0 for k in KINDS}
    for ordinal,row in enumerate(expected):
        if candidate is not None:
            supplied=next(candidate,end);require(supplied is not end,6,'/catalogue/missing')
            require(type(supplied) is dict and type(supplied.get('kind')) is str
                    and supplied['kind'] in KINDS,1,'/catalogue')
            typed(supplied,KINDS[supplied['kind']]);require(supplied==row,6,'/catalogue/row')
        typed(row,KINDS[row['kind']]);data=encode(canonical(row,KINDS[row['kind']]))
        require(row['ordinal']==ordinal and len(data)<=4096,6,'/catalogue/bounds')
        digest.update(data+b'\n');counts[row['kind']]+=1
    require(counts==dict(morale=1296,**{'assault-cell':6480,'capture-refusal':8840}),6,'/catalogue/coverage')
    if candidate is not None:require(next(candidate,end) is end,6,'/catalogue/extra')
    return 'sha256:'+digest.hexdigest()

def read_source(data,selection_ledger,round_ledger,assault_ledger):
    source,_=admitted(data,selection_ledger,round_ledger,assault_ledger)
    return parse(raw(source,'AdmissionSource'),'AdmissionSource')

def proof(data,selection_ledger,round_ledger,assault_ledger):
    source,prior=admitted(data,selection_ledger,round_ledger,assault_ledger)
    rules=normalized_rules();catalogue=catalogue_hash(rules);verify_dependencies()
    digest=source_digest(source)
    assessment=dict(contractVersion=1,scopeKind=INVENTORY['scopeKind'],actualRoundSourceHash=prior['sourceHash'],
        dependencyManifestHash=manifest_hash(),rulesInputHash=INVENTORY['rulesInputHash'],
        moralePairs=1296,assaultPairs=6480,captureRefusalVariants=8840,arithmeticCatalogueHash=catalogue,
        paidAdmissionEnabled=False,nativeSettlementCertified=False,authenticSettlementCertified=False)
    return parse(raw(dict(contractVersion=1,sourceId='aaadm.'+digest,sourceHash='sha256:'+digest,
        actualRoundProof=prior,arithmeticAssessment=assessment),'AdmissionProof'),'AdmissionProof')

def read_proof(claim,data,selection_ledger,round_ledger,assault_ledger):
    # Authenticate ingress before any supplied claim, then validate shape before costly arithmetic.
    admitted(data,selection_ledger,round_ledger,assault_ledger)
    value=parse(claim,'AdmissionProof')
    expected=proof(data,selection_ledger,round_ledger,assault_ledger)
    require(claim==raw(expected,'AdmissionProof'),6,'/proof')
    return value

# Executable verification below: no golden generation or fixture-based admission.
def original_cases():
    path='docs/specs/fixtures/combat-actual-round-entry-v1.json'
    fixture=json.loads(consume(path,TEST_DEPENDENCIES))
    return [x for x in fixture['cases'] if x['cancellation'] is None]

def case_args(case):
    return (encode(dict(contractVersion=1,actualRoundSourceCanonicalUtf8=case['source']['canonicalUtf8'])),
            [json.loads(x['canonicalUtf8']) for x in case['trustedSelectionInputs']],
            [json.loads(x['canonicalUtf8']) for x in case['trustedRoundInputs']],[])
def at_use_error_tests():
    args=case_args(original_cases()[0]);old=are.verify_dependencies
    path='docs/specs/fixtures/combat-content-v7.canonical.json'
    def late_failure():raise are.Invalid(9,path)
    try:
        are.verify_dependencies=late_failure
        assert rejected(lambda:read_source(*args),9).path==path
    finally:are.verify_dependencies=old
    old=are.read_source
    def unavailable_source(*args):raise FileNotFoundError('material predecessor input unavailable')
    try:
        are.read_source=unavailable_source
        rejected(lambda:read_source(*args),9)
    finally:are.read_source=old
    inputs=are.selection.st.inputs;old=inputs.expected_rules
    def unavailable():raise FileNotFoundError('material rules input unavailable')
    try:
        inputs.expected_rules=unavailable
        rejected(normalized_rules,9)
    finally:inputs.expected_rules=old
    print('PASS: inherited at-use dependency009 and material I/O009 stay distinct from trust004.')

def ledger_shape_tests():
    args=case_args(original_cases()[0])
    for index in (1,2):
        supplied=list(args);supplied[index]=copy.deepcopy(args[index]);supplied[index][0]=None
        rejected(lambda supplied=supplied:read_source(*supplied),1)
        supplied=list(args);supplied[index]=copy.deepcopy(args[index]);supplied[index][0]['clockAvailable']=0
        rejected(lambda supplied=supplied:read_source(*supplied),1)
    rejected(lambda:read_source(b'x'*(MAX_BYTES+1),*args[1:]),1)
    print('PASS: ledger primitive/record shape rejects001 before trusted-source validation; carrier cap.')

def grammar_nodes(value,kind,path=(),legacy=False):
    """Walk the declared inherited grammar, not a handpicked list of failing paths."""
    kind=kind.rstrip('?')
    legacy=legacy or (kind in are.selection.pe.SCHEMA and kind not in are.OWN_SCHEMA
                     and kind not in are.selection.OWN_SCHEMA and kind not in SCHEMA and kind not in RECORD_SCHEMA)
    yield ('signed-'+kind if legacy and kind in ('int','long') else kind),path,value
    grammar=are.selection.pe.SCHEMA if legacy else GRAMMAR
    if value is None:return
    if kind.endswith('[]'):
        for index,child in enumerate(value):yield from grammar_nodes(child,kind[:-2],path+(index,),legacy)
    elif kind in grammar:
        for key,child in grammar[kind]:yield from grammar_nodes(value[key],child,path+(key,),legacy)


def claim_boundary_tests():
    global dependency_bytes
    counts=dict(version=0,integer=0,public=0,overlays=0,dependency=0);types=set()
    bounds={'int':(0,2**31-1),'long':(0,2**63-1),'signed-int':(-2**31,2**31-1),
            'signed-long':(-2**63,2**63-1),'ulong':(0,2**64-1),'utc':(0,are.MAX_UTC),'futureTurn':(1,115)}
    for case in original_cases():
        args=case_args(case);baseline=proof(*args);public_types=set()
        for kind,path,value in grammar_nodes(baseline,'AdmissionProof'):
            if path and path[-1] in ('contractVersion','schemaVersion'):
                changed=replace_leaf(baseline,path,value+1);data=encode(changed)
                rejected(lambda data=data:parse(data,'AdmissionProof'),3);counts['version']+=1
                owner=next(k for k,p,v in grammar_nodes(baseline,'AdmissionProof') if p==path[:-1]);types.add(owner)
                if owner not in public_types:
                    rejected(lambda data=data:read_proof(data,*args),3);counts['public']+=1;public_types.add(owner)
                    rejected(lambda data=data:read_proof(b' '+data,*args),3);counts['overlays']+=1
                    overlay=copy.deepcopy(changed);overlay['unknown']=None
                    rejected(lambda overlay=overlay:read_proof(encode(overlay),*args),1);counts['overlays']+=1
                    rejected(lambda data=data:read_proof(data,args[0],[],args[2],[]),4);counts['overlays']+=1
            if kind in bounds:
                minimum,maximum=bounds[kind]
                for replacement,code in ((minimum-1,2),(maximum+1,2),(True,1),(1.0,1)):
                    changed=replace_leaf(baseline,path,replacement);data=encode(changed)
                    rejected(lambda data=data:parse(data,'AdmissionProof'),code);counts['integer']+=1
                # Every numeric field also exercises the authenticated public reader's bound path.
                changed=replace_leaf(baseline,path,maximum+1);data=encode(changed)
                rejected(lambda data=data:read_proof(b' '+data,*args),2);counts['public']+=1
        for index,kind in ((1,'Input'),(2,'RoundInput')):
            for row_index,row in enumerate(args[index]):
                for child,path,value in grammar_nodes(row,kind):
                    if child in bounds:
                        minimum,maximum=bounds[child]
                        supplied=list(args);supplied[index]=copy.deepcopy(args[index])
                        supplied[index][row_index]=replace_leaf(row,path,maximum+1)
                        rejected(lambda supplied=supplied:read_source(*supplied),2);counts['integer']+=1
                    if path and path[-1] in ('contractVersion','schemaVersion'):
                        supplied=list(args);supplied[index]=copy.deepcopy(args[index])
                        supplied[index][row_index]=replace_leaf(row,path,value+1)
                        rejected(lambda supplied=supplied:read_source(*supplied),3);counts['version']+=1
        # Physical preflight outranks an invalid version plus canonical spelling on the claim.
        version_path=next(path for kind,path,value in grammar_nodes(baseline,'AdmissionProof')
                          if path and path[-1]=='contractVersion')
        changed=replace_leaf(baseline,version_path,2);data=b' '+encode(changed)
        original_reader=dependency_bytes;target=next(iter(DEPENDENCIES))
        def missing(path):
            if path==target:raise FileNotFoundError(path)
            return original_reader(path)
        try:
            dependency_bytes=missing
            assert rejected(lambda:read_proof(data,*args),9).path==target;counts['dependency']+=1
        finally:dependency_bytes=original_reader
    print('PASS: systematic exact-code inherited/owned claim and ledger boundaries;',counts,'version types=',sorted(types),flush=True)


def admission_tests():
    cases=original_cases()
    assert len(cases)==4
    maxima={'sourceBytes':0,'proofBytes':0,'selectionBytes':0,'roundBytes':0,'containerDepth':0}
    def shape(value,depth=0):
        if type(value) is dict:return max([depth]+[shape(v,depth+1) for v in value.values()])
        if type(value) is list:
            assert len(value)<=512
            return max([depth]+[shape(v,depth+1) for v in value])
        return depth
    for case in cases:
        args=case_args(case);before=copy.deepcopy(args)
        value=proof(*args);expected=json.loads(case['proofs'][-1]['canonicalUtf8'])
        assert value['actualRoundProof']==expected and value['actualRoundProof']['control']['stateVersion']==25
        assert value['actualRoundProof']['control']['randomState']['nextByteCursor']==2
        assert all(value['arithmeticAssessment'][x] is False for x in FLAGS)
        assert value['arithmeticAssessment']['captureRefusalVariants']==8840
        assert read_proof(raw(value,'AdmissionProof'),*args)==value
        assert read_source(*args)==json.loads(args[0]) and args==before
        lengths=dict(sourceBytes=len(args[0]),proofBytes=len(raw(value,'AdmissionProof')),
                     selectionBytes=len(encode(args[1])),roundBytes=len(encode(args[2])))
        assert all(n<=MAX_BYTES for n in lengths.values())
        lengths['containerDepth']=max(shape(json.loads(args[0])),shape(value),shape(args[1]),shape(args[2]))
        assert lengths['containerDepth']<=32
        for key,n in lengths.items():maxima[key]=max(maxima[key],n)
    print('PASS: 4 original owner/seal admission/readback proofs, exact immutable ARE25; typed bounds=',maxima)

def rejected(call,code=None):
    try:call()
    except Invalid as error:
        assert code is None or error.code==f'CMB-AAD-{code:03}',(error.code,code,error.path)
        return error
    raise AssertionError('Invalid evidence accepted')

def mechanics_tests():
    from fractions import Fraction
    rules=normalized_rules();records=catalogue_records(rules);world=are.selection.env.world
    counts={k:0 for k in KINDS};seen=set();maxima={k:0 for k in KINDS};size=0
    for index,row in enumerate(records):
        assert row['ordinal']==index;counts[row['kind']]+=1
        kind=row['kind'];data=encode(canonical(row,KINDS[kind]));maxima[kind]=max(maxima[kind],len(data));size+=len(data)+1
        if kind=='morale':
            ac,dc=row['attackerMoraleCoordinate'],row['defenderMoraleCoordinate']
            # Independent retained literal rule: ordered11→+1,66→−1, other coordinates0.
            am=1 if ac==11 else -1 if ac==66 else 0;dm=1 if dc==11 else -1 if dc==66 else 0
            assert (row['attackerAdjustment'],row['defenderAdjustment'],row['differential'])==(am,dm,am-dm)
            key=(kind,ac,dc)
        elif kind=='assault-cell':
            diff,ac,dc=row['differential'],row['attackerCoordinate'],row['defenderCoordinate']
            trigger=row['captureTriggered'];f=world.result_facts(diff,ac,dc,1 if trigger else None)
            assert all(row[k]==f[k] for k in ('attackerPercent','defenderPercent','rawEngaged','requiredRetreat','capturedRole'))
            key=(kind,diff,ac,dc)
        else:
            f=world.result_facts(row['differential'],row['attackerCoordinate'],row['defenderCoordinate'],row['captureDie'])
            assert row['facts']==f
            # Fraction-based independent rounding and complete role conservation, not generator integer formula.
            av=Fraction(f['attackerPercent'],10);dv=Fraction(f['defenderPercent']+10*row['refusalHexes'],10)
            losses={'attacker':-(-av.numerator//av.denominator),'defender':dv.numerator//dv.denominator}
            for role,loss in losses.items():
                cap=Fraction(loss*f['captureShare'],100) if f['capturedRole']==role else Fraction(0)
                captured=-(-cap.numerator//cap.denominator)
                assert row[role+'LossToe']==loss and row[role+'CapturedToe']==captured
                assert row[role+'OtherLossToe']==loss-captured and row[role+'RemainingToe']==10-loss
                assert row[role+'LossDp']==(3 if loss>=3 else 0)
                assert 10==row[role+'RemainingToe']+row[role+'CapturedToe']+row[role+'OtherLossToe']
            key=(kind,row['differential'],row['attackerCoordinate'],row['defenderCoordinate'],row['captureDie'],row['refusalHexes'])
        assert key not in seen;seen.add(key)
    assert counts==dict(morale=1296,**{'assault-cell':6480,'capture-refusal':8840}) and len(seen)==16616
    baseline=catalogue_hash(rules)
    assert catalogue_hash(rules,catalogue_records(rules))==baseline
    # Streams remain generator-based; no giant required record array is constructed.
    rejected(lambda:catalogue_hash(rules,itertools.islice(catalogue_records(rules),16615)),6)
    def repeated():
        it=catalogue_records(rules);first=next(it);yield first;yield first;yield from itertools.islice(it,1,None)
    rejected(lambda:catalogue_hash(rules,repeated()),6)
    def reordered():
        it=catalogue_records(rules);a,b=next(it),next(it);yield b;yield a;yield from it
    rejected(lambda:catalogue_hash(rules,reordered()),6)
    def extra():
        yield from catalogue_records(rules);yield next(catalogue_records(rules))
    rejected(lambda:catalogue_hash(rules,extra()),6)
    rejected(lambda:catalogue_hash(rules,itertools.chain(catalogue_records(rules),[None])),6)
    def changed():
        it=catalogue_records(rules);first=next(it);first['differential']=1;yield first;yield from it
    rejected(lambda:catalogue_hash(rules,changed()),6)
    def wrongtype():
        it=catalogue_records(rules);first=next(it);first['ordinal']=False;yield first;yield from it
    rejected(lambda:catalogue_hash(rules,wrongtype()),1)
    for bad_kind in ([],{},None,False):
        rejected(lambda bad_kind=bad_kind:catalogue_hash(rules,[{'kind':bad_kind}]),1)
    print(f'PASS: independently checked 16616 indexed arithmetic records, coverage forgeries; maxima={maxima}, JSONL bytes={size}.')


def changed_leaves(value,path=()):
    if type(value) is dict:
        for k,v in value.items():yield from changed_leaves(v,path+(k,))
    elif type(value) is list:
        for k,v in enumerate(value):yield from changed_leaves(v,path+(k,))
        if not value:yield path,[None]
    else:
        replacement=not value if type(value) is bool else value+1 if type(value) is int else 'x' if value is None else value+'x'
        yield path,replacement

def replace_leaf(value,path,replacement):
    result=copy.deepcopy(value);parent=result
    for key in path[:-1]:parent=parent[key]
    parent[path[-1]]=replacement;return result


def recovery_tests():
    global dependency_bytes
    case=original_cases()[0];args=case_args(case);baseline=proof(*args);claim=raw(baseline,'AdmissionProof')
    original_bytes=tuple(encode(x) if type(x)is not bytes else x for x in args)
    # Raw grammar, primitive and source-family competing errors.
    rejected(lambda:read_source(b' '+args[0],*args[1:]),8)
    rejected(lambda:read_source(b'\xef\xbb\xbf'+args[0],*args[1:]),1)
    rejected(lambda:read_source(args[0]+b'{}',*args[1:]),1)
    rejected(lambda:read_source(b'{"contractVersion":1,"contractVersion":1,"actualRoundSourceCanonicalUtf8":"x"}',*args[1:]),1)
    for replacement,code in ((True,1),(1.0,1),(-1,2),(2,3)):
        source=json.loads(args[0]);source['contractVersion']=replacement
        rejected(lambda source=source:read_source(encode(source),*args[1:]),code)
    for data in (b'',b'{}',b'[]',b'"x"',b'x',b'0',b'null',b'{"kind":"commit-attack"}'):
        rejected(lambda data=data:read_source(data,*args[1:]),1)
    for inputs in (None,(),{},True):
        rejected(lambda inputs=inputs:read_source(args[0],inputs,args[2],[]),1)
        rejected(lambda inputs=inputs:read_source(args[0],args[1],inputs,[]),1)
        rejected(lambda inputs=inputs:read_source(*args[:3],inputs),1)
    for assault in ([None],[False],list(range(10000))):
        rejected(lambda assault=assault:read_source(*args[:3],assault),4)
    try:read_source(*args[:3])
    except TypeError:pass
    else:raise AssertionError('Missing invocation argument accepted')
    rejected(lambda:read_source(args[0],args[1]*3,args[2],[]),1)
    for index in (1,2):
        for mutation in (args[index][:-1],list(reversed(args[index])),args[index]+args[index][:1]):
            supplied=list(args);supplied[index]=mutation
            rejected(lambda supplied=supplied:read_source(*supplied),4)
        # Every input leaf, including actor/time/availability and complete command arms.
        for path,replacement in changed_leaves(args[index]):
            supplied=list(args);supplied[index]=replace_leaf(args[index],path,replacement)
            rejected(lambda supplied=supplied:read_source(*supplied))
    # Earlier/cancelled cuts and every event input/effect/receipt leaf reject coherent local re-signatures.
    original=json.loads(json.loads(args[0])['actualRoundSourceCanonicalUtf8']);event_count=0
    for cut in range(5):
        source=copy.deepcopy(original);source['roundEventCanonicalUtf8']=source['roundEventCanonicalUtf8'][:cut]
        carrier=encode(dict(contractVersion=1,actualRoundSourceCanonicalUtf8=are.source_bytes(source).decode('ascii')))
        rejected(lambda carrier=carrier,cut=cut:read_source(carrier,args[1],args[2][:cut],[]),4)
    for index,text in enumerate(original['roundEventCanonicalUtf8']):
        event=json.loads(text)
        for path,replacement in changed_leaves(event):
            forged=replace_leaf(event,path,replacement)
            # Retain all changed values and locally re-sign when the inherited grammar permits it.
            try:
                are.typed(forged,'RoundEvent')
                ordered=are.canonical(forged,'RoundEvent');ordered.pop('receiptId')
                if path!=('receiptId',): forged['receiptId']='arc.'+are.digest('receipt',ordered)
                carrier_event=are.raw(forged,'RoundEvent').decode('ascii')
            except ValueError:carrier_event=encode(forged).decode('ascii')
            assert carrier_event!=text, ('Mutation restored original event',index,path)
            source=copy.deepcopy(original);source['roundEventCanonicalUtf8'][index]=carrier_event
            carrier=encode(dict(contractVersion=1,actualRoundSourceCanonicalUtf8=encode(source).decode('ascii')))
            rejected(lambda carrier=carrier:read_source(carrier,*args[1:]),4);event_count+=1
    cancelled=json.loads(consume('docs/specs/fixtures/combat-actual-round-entry-v1.json',TEST_DEPENDENCIES))
    for row in cancelled['cases']:
        if row['cancellation'] is not None:rejected(lambda row=row:read_source(*case_args(row)),4)
    # Ingress authenticates before claim shape. Fixed-false flags are encoded-value errors.
    rejected(lambda:read_proof(b'{}',args[0],[],args[2],[]),4)
    for flag in FLAGS:
        changed=copy.deepcopy(baseline);changed['arithmeticAssessment'][flag]=True
        rejected(lambda changed=changed:read_proof(encode(changed),*args),2)
    claim_count=0
    for path,replacement in changed_leaves(baseline):
        changed=replace_leaf(baseline,path,replacement)
        rejected(lambda changed=changed:read_proof(encode(changed),*args));claim_count+=1
        if claim_count%200==0:print(f'PASS: {claim_count} full-proof mutation probes.',flush=True)
    returned=read_proof(claim,*args);returned['actualRoundProof']['control']['world']['elements'].clear()
    returned['arithmeticAssessment']['moralePairs']=0
    assert read_proof(claim,*args)==baseline
    returned_source=read_source(*args);returned_source['actualRoundSourceCanonicalUtf8']='x'
    assert proof(*args)==baseline
    # Every external runtime path: cold/warm physical mismatch and disappearance. No repository edits.
    original_reader=dependency_bytes;probe_count=0
    try:
        for path in DEPENDENCIES:
            are._CACHE.clear()
            def missing(p,target=path):
                if p==target:raise FileNotFoundError(p)
                return original_reader(p)
            dependency_bytes=missing
            assert rejected(lambda:read_source(*args),9).path==path;probe_count+=1
            dependency_bytes=original_reader
            read_source(*args);assert are._CACHE
            def changed(p,target=path):return original_reader(p)+b' ' if p==target else original_reader(p)
            dependency_bytes=changed
            assert rejected(lambda:read_proof(b'{}',*args),9).path==path;probe_count+=1
        dependency_bytes=original_reader
        # At-use Content (second read), normalized source (third read) changes after a successful preflight.
        for path,occurrence in [('docs/specs/fixtures/combat-content-v7.canonical.json',2),('docs/research/fixtures/combat-selected-source-v1.json',3)]:
            seen=0
            def race(p,target=path,at=occurrence):
                nonlocal seen
                if p==target:
                    seen+=1
                    if seen>=at:return original_reader(p)+b' '
                return original_reader(p)
            dependency_bytes=race
            assert rejected(lambda:proof(*args),9).path==path;probe_count+=1
    finally:dependency_bytes=original_reader
    assert proof(*args)==baseline and original_bytes==tuple(encode(x) if type(x)is not bytes else x for x in args)
    # Old family retains its no-paid behavior, not candidate RED.
    old_source=original
    b,s=are.replay(old_source,args[1],args[2]);command=are.command(b,s,'commit-attack');inp=are.trusted(command)
    try:are.apply(old_source,args[1],args[2],inp)
    except are.Invalid as error:assert error.code=='CMB-ARE-007'
    else:raise AssertionError('Predecessor paid continuation accepted')
    print(f'PASS: recovery/grammar/ledger/claim ownership; {claim_count} proof leaves; {event_count} re-signed event leaves; {probe_count} dependency probes.')


def artifact_bytes(value):
    assert type(value)is dict and set(value)=={'canonicalUtf8','byteCount','sha256'}
    data=value['canonicalUtf8'].encode('ascii')
    assert type(value['byteCount'])is int and value['byteCount']==len(data) and value['sha256']==sha(data)
    return data


def verify_literals():
    verify_dependencies();fixture=json.loads(FIXTURE.read_text())
    assert fixture['contractVersion']==1 and fixture['contract']=='combat-actual-assault-v1' and fixture['checkpoint']=='admission-only'
    assert fixture['runtimeSourceHashes']==DEPENDENCIES and fixture['testSourceHashes']==TEST_DEPENDENCIES
    cases=original_cases();assert len(fixture['cases'])==len(cases)==4
    for retained,original in zip(fixture['cases'],cases):
        assert (retained['owner'],retained['firstSeal'])==(original['owner'],original['firstSeal'])
        assert retained['trustedSelectionInputs']==original['trustedSelectionInputs']
        assert retained['trustedRoundInputs']==original['trustedRoundInputs'] and retained['trustedAssaultInputs']==[]
        args=case_args(original);assert artifact_bytes(retained['source'])==args[0]
        expected=proof(*args);claim=artifact_bytes(retained['proof'])
        assert raw(expected,'AdmissionProof')==claim and read_proof(claim,*args)==expected
    print('PASS: four frozen literal source/ledger/proof records; no golden regeneration.')


def main():
    parser=argparse.ArgumentParser()
    for name in ('admission','mechanics','recovery'):parser.add_argument('--check-'+name,action='store_true')
    args=parser.parse_args();selected=any(vars(args).values())
    for name,check in [('admission',admission_tests),('mechanics',mechanics_tests),('recovery',recovery_tests)]:
        if not selected or getattr(args,'check_'+name):
            if name=='admission':
                ledger_shape_tests();at_use_error_tests();claim_boundary_tests()
            check()
    if not selected:verify_literals()

if __name__=='__main__':main()
