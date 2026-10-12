import sys,json,importlib.util,time,hashlib
from pathlib import Path
repo=Path.cwd().resolve();seen=set()
def audit(event,args):
 if event!='open' or not isinstance(args[0],(str,bytes)):return
 try:p=Path(args[0]).resolve();relative=p.relative_to(repo)
 except (ValueError,OSError):return
 if p.is_file() and (not isinstance(args[1],str) or 'r' in args[1]):seen.add(str(relative))
sys.addaudithook(audit);sys.dont_write_bytecode=True
path=repo/'docs/specs/verify-combat-actual-post-assault-continuation-v1.py';spec=importlib.util.spec_from_file_location('acquisition_actual_continuation',path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
start=time.monotonic();m.dependency_tests();m.policy_probe_tests();elapsed=time.monotonic()-start
runtime=set(m.DEPENDENCIES);owned={str(p.relative_to(repo)) for p in m.OWN_DELIVERY};tests={'docs/specs/fixtures/combat-actual-paid-assault-v1.json'}
unknown=sorted(seen-runtime-owned-tests);unobserved=sorted(runtime-seen);out={'mode':'cold/adversarial dependency and isolated policy paths','elapsedSeconds':round(elapsed,3),'runtimeCount':len(runtime),'runtime':sorted(runtime),'ownedDelivery':sorted(owned),'testOnly':sorted(seen&tests),'observed':sorted(seen),'unknown':unknown,'unobserved':unobserved,'hashes':{p:hashlib.sha256((repo/p).read_bytes()).hexdigest() for p in sorted(runtime|owned|tests)}}
Path('/private/tmp/pac01-acquisition-adversarial.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps({k:v for k,v in out.items() if k not in ['hashes','runtime','observed']}));assert not unknown and not unobserved
