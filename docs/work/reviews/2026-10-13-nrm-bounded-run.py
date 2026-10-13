import subprocess,time,json,hashlib,sys,os,signal
from pathlib import Path
name=sys.argv[1];budget=int(sys.argv[2]);cmd=sys.argv[3:];log=Path('/private/tmp/nrm-'+name+'.log');start=time.monotonic();timed=False
with log.open('wb') as f:
 p=subprocess.Popen(cmd,stdout=f,stderr=subprocess.STDOUT,start_new_session=True)
 try:code=p.wait(timeout=budget)
 except subprocess.TimeoutExpired:os.killpg(p.pid,signal.SIGKILL);p.wait();code=p.returncode;timed=True
out=dict(command=cmd,budgetSeconds=budget,exitCode=code,timedOut=timed,elapsedSeconds=round(time.monotonic()-start,3),logSha256=hashlib.sha256(log.read_bytes()).hexdigest());Path('/private/tmp/nrm-'+name+'.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out));print(log.read_text()[-8000:]);sys.exit(1 if timed else code)
