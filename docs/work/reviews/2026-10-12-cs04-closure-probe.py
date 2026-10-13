"""Physical acquisition observation; runtime paths are never inferred from fixtures."""
import sys,json,time,importlib.util,hashlib
from pathlib import Path
sys.dont_write_bytecode=True
repo=Path(__file__).resolve().parents[3];seen={};start=time.monotonic()
def audit(event,args):
 if event=='open' and isinstance(args[0],(str,bytes)):
  try:
   p=Path(args[0]).resolve()
   if p.is_relative_to(repo) and p.is_file():seen[p.relative_to(repo).as_posix()]=seen.get(p.relative_to(repo).as_posix(),0)+1
  except (OSError,ValueError):pass
sys.addaudithook(audit)
p=repo/'docs/specs/verify-combat-actual-repeated-movement-v1.py';spec=importlib.util.spec_from_file_location('rmv_closure',p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
cold=set(seen);mode=sys.argv[1]
if mode=='core':m.preservation_tests('axis','attacker');m.grammar_tests()
elif mode=='adversarial':m.adversarial_tests('axis','attacker')
elif mode=='dependencies':m.dependency_tests()
else:raise ValueError(mode)
runtime=set(m.DEPENDENCIES);tests=set(m.INVENTORY['testArtifactHashes']);owned={x.relative_to(repo).as_posix() for x in m.OWN_DELIVERY};unknown=set(seen)-runtime-tests-owned
out=dict(mode=mode,elapsedSeconds=round(time.monotonic()-start,3),runtimeCount=len(runtime),testCount=len(tests),ownedCount=len(owned),coldRuntimeNotObserved=sorted(runtime-cold),runtimeNotObserved=sorted(runtime-set(seen)),physicalReads=seen,unknown=sorted(unknown),runtimeSourceHashes=m.DEPENDENCIES,dependencyManifestHash='sha256:'+m.identity('dependency-manifest',dict(sorted(m.DEPENDENCIES.items()))))
Path('/private/tmp/cs04-closure-'+mode+'.json').write_text(json.dumps(out,indent=2)+'\n')
assert len(runtime)==66 and not unknown and not runtime-set(seen) and not runtime-cold
print('PASS physical closure',mode,out['elapsedSeconds'],len(seen))
