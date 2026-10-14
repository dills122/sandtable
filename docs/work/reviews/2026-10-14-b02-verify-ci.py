import pathlib,json,xml.etree.ElementTree as E,collections,hashlib
p=pathlib.Path('/private/tmp/b02-ci-final'); i=json.loads((p/'inventory.json').read_text()); local=json.loads(pathlib.Path('/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/docs/work/reviews/2026-10-14-b00-inventory.json').read_text());sha='4e725eba3bcbb412977856340d94d06c52f71f71'; assert i['tested_sha']==sha
counts={'core-a':71,'core-b':2329,'core-c':64,'exercise':469,'contracts':10}; report={}
for k,n in counts.items():
 st=json.loads((p/k/'status.json').read_text());assert st['exit_code']==0 and st['tested_sha']==sha and st['run_attempt']=='1' and st['configuration']=='Release'
 rows=collections.Counter(map(tuple,i['shards'][k]['rows']));assert len(rows)==n and sum(rows.values())==n
 assert rows==collections.Counter(map(tuple,local['shards'][k]['rows']))
 actual=collections.Counter();files=list((p/k/'results').glob('*.xml'));assert files
 for f in files:
  root=E.parse(f).getroot()
  for a in root.iter('assembly'):
   assert all(int(a.attrib.get(z,'0'))==0 for z in ['errors','failed','not-run','skipped'])
  for t in root.iter('test'):
   assert t.attrib['result']=='Pass';actual[(i['shards'][k]['module'],t.attrib['type']+'.'+t.attrib['method'],json.loads(chr(34)+t.attrib['name']+chr(34)))]+=1
 assert actual==rows,(k,len(actual),len(rows))
 report[k]={'passed':n,'failed':0,'skipped':0,'xml_sha256':{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in files}}
out={'pr':185,'head':'be149cb5c2fd0226c1ca90a5c630af0e3aa10b73','base':'4191d5efd86622b181a7c1f309a36e41206b5668','tested_merge':sha,'tested_tree':'542f7c52e303a9f85a9070d0d24cabbc63ad8134','run':38517061509,'total_passed':sum(counts.values()),'exact_inventory_and_execution_match':True,'shards':report}
pathlib.Path('/private/tmp/b02-ci-final-receipt.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out,indent=2))
