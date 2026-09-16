import runpy,sys,json,hashlib,struct
sys.dont_write_bytecode=True
m=runpy.run_path('docs/specs/verify-combat-side-projection-v1.py')
sources=m['source_cases3b']();byname={s['name']:s for s in sources}
counts=dict(native_pairs=0,identity_checks=0,prefix_equality=0,clock_outcomes=0,retries=0,terminal_rejects=0,native_member_checks=0)
reject=dict(contractVersion=3,status='rejected',receipt=None)
def string(s):
    b=s if isinstance(s,bytes) else s.encode('utf-8')
    return struct.pack('>I',len(b))+b
def digest(domain,payload):return hashlib.sha256(domain.encode()+b'\x00'+payload).digest()
for fallback in [s for s in sources if s['name'].endswith('.fallback')]:
    core=byname[fallback['name'].removesuffix('.fallback')]
    owner=m['source_context3b'](core)['base']['boundary']['cycle']['actingSide']
    native=json.loads(json.loads(fallback['events'][-1])['nativeEvent'])
    original=json.loads(json.loads(core['events'][-1])['nativeEvent'])
    assert fallback['inputs'][-1]['actor']==owner and (fallback['inputs'][-1]['admittedAt'] is None or fallback['inputs'][-1]['clockAvailable'] is False)
    assert native['author']=='system' and native['effect']['kind']=='phase-finished' and native['effect']['reason']=='clock-unavailable'
    assert original['author']==owner and original['effect']['reason']=='owner-choice'
    counts['native_pairs']+=1
    for audience in m['SIDES']:
        c=m['views3b'](core,audience);f=m['views3b'](fallback,audience)
        assert m['raw3'](c[-2],'Observation3')==m['raw3'](f[-2],'Observation3')
        assert f[-1]['ownReceipts']==f[-2]['ownReceipts']
        counts['prefix_equality']+=1
        if audience!=owner:
            assert m['raw3'](c[-1],'Observation3')==m['raw3'](f[-1],'Observation3')
            continue
        view=c[-2];decision=view['decision'];actions=decision['actions']
        assert decision['decisionFamily']=='cycle' and [a['candidate']['kind'] for a in actions]==['finish']
        context=view['context'];candidate=json.dumps(actions[0]['candidate'],ensure_ascii=True,separators=(',',':')).encode()
        payload=struct.pack('>I',1)+string(context['campaignId'])+string(owner)+bytes.fromhex(view['cycleRef'][7:])+string('cycle-control')+struct.pack('>Q',decision['openingRevision'])+string(context['capabilityPolicyId'])+struct.pack('>I',1)+string(candidate)
        h=digest('sandtable.cycle.actions.v1',payload)
        assert decision['actionSetId']=='sha256:'+h.hex()
        assert actions[0]['actionId']=='sha256:'+digest('sandtable.cycle.action.v1',struct.pack('>I',1)+h+struct.pack('>I',0)).hex()
        counts['identity_checks']+=2
        raw=m['raw3'](m['submission3b'](view),'Submission3')
        assert len(c[-1]['ownReceipts'])==len(c[-2]['ownReceipts'])+1
        accepted=dict(contractVersion=3,status='accepted',receipt=c[-1]['ownReceipts'][-1])
        opening=decision['deadlineUnixMilliseconds']-30000
        for source in (core,fallback):
            current=m['prefix'](source,len(source['events'])-1)
            for now,available,ok in ((opening+1,True,True),(opening-1,True,False),(opening+30000,True,False),(None,False,False),(opening+1,False,False)):
                assert m['admit_a3b'](current,owner,raw,now,available)==(accepted if ok else reject)
                counts['clock_outcomes']+=1
        assert m['admit_a3b'](core,owner,raw,None,False)==accepted
        assert m['admit_a3b'](fallback,owner,raw,None,False)==reject
        counts['retries']+=2
for source in [s for s in sources if s['family']=='historical-terminal']:
    for audience in m['SIDES']:
        view=m['views3b'](source,audience)[0]
        assert view['decision'] is None and view['visibleRevision']==0 and len(view['history'])==1 and view['ownReceipts']==[]
        assert m['admit_a3b'](source,audience,b'{}',None,False)==reject
        counts['terminal_rejects']+=1
        if source['name'].endswith(audience+'-reserve-I-movement-completion'):
            member=m['composition3b'].reserve_movement.trace(audience)[1][-1]['releaseMember']
            own=view['ownReserve'];assert own is not None
            assert own['participantRef']==view['own']['participantRef']
            assert own['status']==member['status']=='none' and own['baseCpa']==member['baseCpa']==10
            assert own['spentCp']==member['spentCp']==dict(numerator=2,denominator=1)
            release=own['release'];native=member['history']
            assert release['releasedType']==native['releasedType']=='I' and release['releaseOrdinal']==native['releaseCycle']==1
            assert release['cpaBasis']==native['cpaBasis']==10 and release['voluntaryCeiling']==native['voluntaryCeiling']==10
            assert release['nextMovement']['ordinal']==native['nextMovement']['ordinal']==2
            assert release['nextMovement']['status']==native['nextMovement']['status']=='expired'
            assert view['lifecycle']['activeOrdinal']==2 and view['lifecycle']['status']=='active'
            counts['native_member_checks']+=1
print('PASS independent native finish/fallback, binary identity and terminal checks',counts)
