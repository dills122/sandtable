import subprocess,time,json,hashlib,sys
from pathlib import Path
name=sys.argv[1];budget=int(sys.argv[2]);cmd=['python3','-B','-u','docs/specs/verify-combat-actual-repeated-movement-v1.py']+sys.argv[3:];start=time.monotonic();timed=False
with Path('/private/tmp/cs04-'+name+'.log').open('wb') as log:
 p=subprocess.Popen(cmd,stdout=log,stderr=subprocess.STDOUT)
 try:code=p.wait(timeout=budget)
 except subprocess.TimeoutExpired:p.kill();p.wait();code=p.returncode;timed=True
out=dict(command=cmd,budgetSeconds=budget,exitCode=code,timedOut=timed,elapsedSeconds=round(time.monotonic()-start,3),logSha256=hashlib.sha256(Path('/private/tmp/cs04-'+name+'.log').read_bytes()).hexdigest());Path('/private/tmp/cs04-'+name+'.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out));print(Path('/private/tmp/cs04-'+name+'.log').read_text()[-5000:]);sys.exit(1 if timed else code)
