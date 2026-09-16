import copy, importlib.util, json, pathlib, sys
sys.dont_write_bytecode=True
root=pathlib.Path('/Users/dsteele/.codex/worktrees/0b59/sandtable')
spec=importlib.util.spec_from_file_location('root_a2',root/'docs/specs/verify-combat-side-projection-v1.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
checks=0; fallback_count=0; mutation_checks=0
for source in m.source_cases2():
    if '.fallback-' not in source['name']: continue
    fallback_count+=1
    owner=source['inputs'][-1]['actor']; event=json.loads(source['events'][-1])
    assert owner in m.SIDES and event['author']=='system'
    before=m.prefix(source,len(source['events'])-1)
    view=m.views2(before,owner)[-1]; final=m.views2(source,owner)[-1]
    decision=view['decision']; assert decision is not None
    assert all(r['decisionId']!=decision['decisionId'] for r in final['ownReceipts'])
    for index in range(len(decision['actions'])):
        data=m.raw2(m.submission2(view,index),'Submission2')
        for now in (None,0,10001,10500,11000,39999,253402300799999):
            for available in (False,True):
                result=m.admit_a2(source,owner,data,now,available)
                assert m.encode(result)==b'{"contractVersion":2,"status":"rejected","receipt":null}'
                checks+=1
        assert m.admit_a2(before,owner,data,None,False)['receipt'] is None
        checks+=1
    # Returned nested state/observation copies cannot mutate authentication caches.
    original=m.raw2(final,'Observation2')
    final['history'].clear(); final['ownReceipts'].clear(); final['own']['currentToe']=999
    assert m.raw2(m.views2(source,owner)[-1],'Observation2')==original; mutation_checks+=1
    frames=m.replay_frames2(source); frames[-1][1]['world']['elements'].clear()
    assert m.raw2(m.views2(source,owner)[-1],'Observation2')==original; mutation_checks+=1
    forged=copy.deepcopy(source); forged['inputs'][-1]['clockAvailable']=0
    try: m.views2(forged,owner)
    except (m.Invalid,m.res2.Invalid,ValueError): pass
    else: raise AssertionError('Boolean-number source forgery admitted')
    mutation_checks+=1
assert fallback_count==8 and checks==240 and mutation_checks==24,(fallback_count,checks,mutation_checks)
print(f'PASS: {fallback_count} authenticated System-fallback histories; {checks} complete receipt rejection/retry outcomes; {mutation_checks} cache isolation and exact-source forgery checks')
