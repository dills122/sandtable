import sys,json,time,importlib.util,hashlib
from pathlib import Path
sys.dont_write_bytecode=True
repo=Path(__file__).resolve().parents[3];seen=set()
def audit(event,args):
 if event=='open' and isinstance(args[0],(str,bytes)):
  try:
   p=Path(args[0]).resolve()
   if p.is_relative_to(repo):seen.add(p.relative_to(repo).as_posix())
  except (OSError,ValueError):pass
sys.addaudithook(audit);start=time.monotonic()
p=repo/'docs/specs/verify-combat-actual-paid-assault-v1.py';s=importlib.util.spec_from_file_location('paid_closure',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
if sys.argv[1]=='core':
 m.manifest_tests();m.preparse_tests();m.rng_tests()
 for owner in ('axis','commonwealth'):
  m.window_tests(owner);m.support_tests(owner);m.grammar_tests(owner);m.mechanics_tests(owner);m.audit_isolation_tests(owner)
  for seal in ('attacker','defender'):
   args,ctx,cuts,inputs=m.genuine_trace(owner,seal)
   assert cuts[-1][2]['closed']
else:
 m.dependency_tests();m.recovery_tests('axis','attacker')
runtime=set(m.DEPENDENCIES);tests=set(m.INVENTORY['testSourceHashes']);owned={x.relative_to(repo).as_posix() for x in m.OWN_DELIVERY}
unknown=seen-runtime-tests-owned
out=dict(mode=sys.argv[1],elapsedSeconds=round(time.monotonic()-start,3),runtimeCount=len(runtime),testCount=len(tests),ownedCount=len(owned),physicalReads=sorted(seen),unknown=sorted(unknown),runtimeNotObserved=sorted(runtime-seen),runtimeSourceHashes=m.DEPENDENCIES,testSourceHashes=m.INVENTORY['testSourceHashes'],dependencyManifestHash=m.dependency_manifest_hash())
Path('/private/tmp/paid-final-closure-'+sys.argv[1]+'.json').write_text(json.dumps(out,indent=2)+'\n')
assert not unknown and not runtime-seen
print('PASS final closure',sys.argv[1],out['elapsedSeconds'],len(seen))
