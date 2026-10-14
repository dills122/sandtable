"""One leased native scoped gate; authenticate exact rows and frozen source/assembly bytes."""
import datetime,gzip,hashlib,json,os,signal,subprocess,time,xml.etree.ElementTree as ET
from pathlib import Path
repo=Path(__file__).resolve().parents[3]
sourcePaths=['src/Cna.Core/Campaigns/CampaignCombatActualRepeatedMovementCompletion.cs','src/Cna.Core/Campaigns/CampaignCombatActualRepeatedMovementCompletionCodec.cs','tests/Cna.Core.Tests/Campaigns/CombatActualRepeatedMovementCompletionTests.cs','tests/Cna.Core.Tests/Campaigns/CombatActualRepeatedMovementCompletionContractTests.cs','tests/Cna.Core.Tests/Cna.Core.Tests.csproj']
sourcePaths+=['docs/specs/combat-actual-repeated-movement-completion-v1'+suffix for suffix in ['.md','.schema.json']]+['docs/specs/verify-combat-actual-repeated-movement-completion-v1.py','docs/specs/fixtures/combat-actual-repeated-movement-completion-v1.json']
sourcePaths+=['artifacts/bin/Cna.Core.Tests/release/'+name for name in ['Cna.Core.Tests.dll','Cna.Core.dll','xunit.v3.common.dll','xunit.v3.core.dll']]
def hashes():return {p:hashlib.sha256((repo/p).read_bytes()).hexdigest() for p in sourcePaths}
inventory=json.loads(Path('/private/tmp/nrc-row-inventory.json').read_text());expected=inventory['scopedRows'];assert len(expected)==len(set(expected))==64
results=Path('/private/tmp/nrc-final-results');results.mkdir(exist_ok=False)
command=['dotnet','test','--project','tests/Cna.Core.Tests/Cna.Core.Tests.csproj','-c','Release','--no-build','--no-ansi','--filter-class','*CombatActualRepeatedMovementCompletion*','--output','Detailed','--report-xunit-xml','--results-directory',str(results),'/bl:/private/tmp/nrc-final-{}.binlog']
budget=900;before=hashes();start=time.monotonic();started=datetime.datetime.now(datetime.timezone.utc).isoformat();log=Path('/private/tmp/nrc-final.log');timed=False
with log.open('wb') as stream:
 process=subprocess.Popen(command,cwd=repo,stdout=stream,stderr=subprocess.STDOUT,start_new_session=True)
 try:code=process.wait(timeout=budget)
 except subprocess.TimeoutExpired:os.killpg(process.pid,signal.SIGKILL);process.wait();code=process.returncode;timed=True
after=hashes();gateError=None;actual=[];counts={};reports=list(results.glob('*.xml'))
try:
 assert code==0 and not timed and before==after,'Process timeout/failure or artifact drift'
 assert len(reports)==1,'Missing/duplicate xUnit report'
 document=ET.parse(reports[0]).getroot();assert document.tag=='assemblies' and len(document.findall('assembly'))==1
 assembly=document.find('assembly');assert Path(assembly.attrib['name'].replace('\\','/')).stem=='Cna.Core.Tests' and assembly.attrib['target-framework']=='.NETCoreApp,Version=v10.0'
 counts={k:int(assembly.attrib[k]) for k in ['total','passed','failed','skipped','not-run','errors']};assert counts['total']==counts['passed']==64 and counts['failed']==counts['skipped']==counts['not-run']==counts['errors']==0 and not document.findall('.//error')
 for test in assembly.findall('.//test'):
  assert test.attrib['result']=='Pass';name=json.loads('"'+test.attrib['name']+'"');method=test.attrib['type']+'.'+test.attrib['method'];assert name==method or name.startswith(method+'(');actual.append(name)
 assert len(actual)==len(set(actual))==64 and set(actual)==set(expected),'Exact scoped rows differ'
except (AssertionError,KeyError,ValueError,ET.ParseError) as error:gateError=str(error) or type(error).__name__
receipt=dict(command=command,budgetSeconds=budget,startedUtc=started,exitCode=code,timedOut=timed,elapsedSeconds=round(time.monotonic()-start,3),preHashes=before,postHashes=after,artifactsUnchanged=before==after,expectedRows=expected,executedRows=sorted(actual),counts=counts,gateError=gateError,gatePassed=gateError is None,reportPaths=[str(p) for p in reports],reportHashes={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in reports},logSha256=hashlib.sha256(log.read_bytes()).hexdigest())
Path('/private/tmp/nrc-final.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({k:receipt[k] for k in ['exitCode','timedOut','elapsedSeconds','artifactsUnchanged','counts','gatePassed','gateError','logSha256']}));print(log.read_text()[-1600:]);raise SystemExit(0 if receipt['gatePassed'] else 1)
