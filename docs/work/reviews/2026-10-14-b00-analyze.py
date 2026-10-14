import collections,datetime,hashlib,json,re,xml.etree.ElementTree as ET
from pathlib import Path
roots={'pr':Path('/private/tmp/nrc-ci-final'),'main':Path('/private/tmp/b00-main-ci')}
result={}
for label,root in roots.items():
 result[label]={}
 for shard in ['core-a','core-b']:
  folder=root/shard if label=='pr' else root/('ci-'+shard+'-ed443bdf2dfb5e6d8e3333364f52b49b9d939c83-1')/shard
  xml=next((folder/'results').glob('*.xml'));document=ET.parse(xml).getroot();a=document.find('assembly');rows={};classes=collections.defaultdict(lambda:dict(rows=0,seconds=0.0));methods=collections.defaultdict(lambda:dict(rows=0,seconds=0.0))
  for t in a.findall('.//test'):
   assert t.attrib['result']=='Pass';name=json.loads('"'+t.attrib['name']+'"');assert name not in rows;cl=t.attrib['type'];method=cl+'.'+t.attrib['method'];seconds=float(t.attrib['time']);rows[name]=dict(method=method,cls=cl,seconds=seconds)
   for d,k in [(classes,cl),(methods,method)]:d[k]['rows']+=1;d[k]['seconds']+=seconds
  diag=next((folder/'diagnostics').glob('*.diag'));stamp=None;starts={};ends={}
  for line in diag.read_text().splitlines():
   if re.match(r'^\d{4}-\d\d-\d\dT',line):stamp=datetime.datetime.fromisoformat(line.split(' ',1)[0]);continue
   m=re.search(r'TestNode = TestNode \{ Uid = TestNodeUid \{ Value = [^ ]+ \}, DisplayName = (.*?), Properties = \[',line)
   if not m:continue
   name=m.group(1)
   if 'InProgressTestNodeStateProperty' in line:starts[name]=stamp
   elif any(s+'TestNodeStateProperty' in line for s in ['Passed','Failed','Skipped']):ends[name]=stamp
  if set(rows)!=set(starts) or set(rows)!=set(ends):
   print('name delta',label,shard,'missingstarts',list(set(rows)-set(starts))[:2],'extrastarts',list(set(starts)-set(rows))[:2],'missingends',list(set(rows)-set(ends))[:2]);raise SystemExit(2)
  first=min(starts.values());last=max(ends.values())
  for cl,group in classes.items():
   names=[n for n,v in rows.items() if v['cls']==cl];group['firstStart']=(min(starts[n] for n in names)-first).total_seconds();group['lastEnd']=(max(ends[n] for n in names)-first).total_seconds();group['span']=group['lastEnd']-group['firstStart']
  status=json.loads((folder/'status.json').read_text());assert status['exit_code']==0 and status['run_attempt']=='1'
  result[label][shard]=dict(status=status,xmlCounters={k:int(a.attrib[k]) for k in ['total','passed','failed','skipped','not-run','errors']},assemblySeconds=float(a.attrib['time']),firstToLastSeconds=(last-first).total_seconds(),summedSeconds=sum(v['seconds'] for v in rows.values()),classes=dict(classes),methods=dict(methods),rows=rows,sourceHashes={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in [xml,diag,folder/'status.json',folder/'execution.log']})
  assert not any(result[label][shard]['xmlCounters'][k] for k in ['failed','skipped','not-run','errors'])
  print(label,shard,'rows',len(rows),'assembly',float(a.attrib['time']),'wall',(last-first).total_seconds(),'sum',round(result[label][shard]['summedSeconds'],3))
  for cl,g in sorted(classes.items(),key=lambda x:-x[1]['seconds'])[:6]:print(cl.rsplit('.',1)[-1],{k:round(v,3) if isinstance(v,float) else v for k,v in g.items()})
assert set(result['pr']['core-a']['rows'])==set(result['main']['core-a']['rows']);assert set(result['pr']['core-b']['rows'])==set(result['main']['core-b']['rows'])
for label in roots:
 b=result[label]['core-b'];chosen={n:v for n,v in b['rows'].items() if v['cls'] in ['Cna.Core.Tests.Campaigns.CombatActualRepeatedMovementCompletionTests','Cna.Core.Tests.Campaigns.CombatActualRepeatedMovementCompletionContractTests']};assert len(chosen)==64
 print(label,'candidate C64',sum(v['seconds'] for v in chosen.values()),'retainedB',len(b['rows'])-len(chosen),sum(v['seconds'] for n,v in b['rows'].items() if n not in chosen))
result['candidateClasses']=['Cna.Core.Tests.Campaigns.CombatActualRepeatedMovementCompletionTests','Cna.Core.Tests.Campaigns.CombatActualRepeatedMovementCompletionContractTests'];Path('/private/tmp/b00-analysis.json').write_text(json.dumps(result,indent=2)+'\n')
