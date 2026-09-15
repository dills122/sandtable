import importlib.util,json,pathlib,sys
sys.dont_write_bytecode=True
root=pathlib.Path('/Users/dsteele/.codex/worktrees/0b59/sandtable')
spec=importlib.util.spec_from_file_location('root_bridge',root/'docs/specs/verify-combat-result-cycle-finish-v1.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
counts=dict(lineages=0,finish=0,deadline=0,retry=0,wrongActor=0,earlyTimer=0,openingFallback=0)
for case in m.r2.cases():
 b=m.base_for(case); counts['lineages']+=1
 prior=m.to_cycle(b); owner=case['attackerSide'];other='commonwealth' if owner=='axis' else 'axis'
 op=m.trusted(m.command(b,prior,'cycle','open'),now=100)
 opened,_,_=m.transition(b,prior,op)
 native=json.loads(opened['kernelState']);assert native['timing']['deadlineUnixMilliseconds']==30100
 world=m.r2.raw(json.loads(b['source']['resultState'])['world'],'World')
 cmd=m.command(b,opened,'cycle','finish')
 for now,available,author in ((99,True,'system'),(100,True,owner),(101,True,owner),(10000,True,owner),(30099,True,owner),(None,True,'system'),(101,False,'system')):
  inp=m.trusted(cmd,owner,now,available); final,event,receipt=m.transition(b,opened,inp)
  evt=json.loads(json.loads(event)['nativeEvent'])
  assert evt['author']==author and final['phase']=='finished' and receipt
  assert m.read_event(event,b,opened,inp)==final
  assert m.r2.raw(json.loads(final['kernelState'])['world'],'World')==world
  counts['finish']+=1
  for later,confidence in ((None,False),(30100,True),(253402300799999,True)):
   retry,extra,rid=m.transition(b,final,m.trusted(cmd,owner,later,confidence))
   assert m.raw(retry,'BridgeState')==m.raw(final,'BridgeState') and extra is None and rid==receipt
   counts['retry']+=1
 for now in (30100,30101):
  try:m.transition(b,opened,m.trusted(cmd,owner,now))
  except m.Invalid:pass
  else:raise AssertionError('late finish admitted')
  counts['deadline']+=1
 try:m.transition(b,opened,m.trusted(cmd,other,None,False))
 except m.Invalid:pass
 else:raise AssertionError('wrong owner caused clock fallback')
 counts['wrongActor']+=1
 unchanged,event,rid=m.transition(b,opened,m.trusted(m.command(b,opened,'cycle','expire'),now=30099))
 assert m.raw(unchanged,'BridgeState')==m.raw(opened,'BridgeState') and event is None and rid is None
 counts['earlyTimer']+=1
 for now,available in ((None,True),(100,False)):
  failed,_,_=m.transition(b,prior,m.trusted(op['command'],now=now,available=available))
  assert json.loads(failed['kernelState'])['openingClockFailure']
  done,event,rid=m.transition(b,failed,m.trusted(m.command(b,failed,'cycle','fallback-step')))
  assert done['phase']=='finished' and json.loads(json.loads(event)['nativeEvent'])['author']=='system'
  assert m.r2.raw(json.loads(done['kernelState'])['world'],'World')==world
  counts['openingFallback']+=1
assert counts==dict(lineages=32,finish=224,deadline=64,retry=672,wrongActor=32,earlyTimer=32,openingFallback=64),counts
print('PASS:',counts)
