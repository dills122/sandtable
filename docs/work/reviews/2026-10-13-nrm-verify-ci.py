import pathlib,json,xml.etree.ElementTree as E,collections,hashlib
p=pathlib.Path('/private/tmp/nrm-ci-final'); i=json.loads((p/'inventory.json').read_text()); local=json.loads(pathlib.Path('/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/docs/work/reviews/2026-10-13-nrm-local-inventory.json').read_text());sha='d2e37160120ccbf6b2a9ea467fc2bc59727d1abb'; assert i['tested_sha']==sha
counts={'core-a':71,'core-b':2329,'exercise':469,'contracts':10}; report={}
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
out={'pr':181,'head':'854d23eda197ca8e46590583114e89d173019fcf','base':'1ef2d038125eb03ed39b6e08aeea5e3ada75983c','tested_merge':sha,'tested_tree':'0c2693a3fc39f53557baad32607753c52e94256c','run':38405445795,'total_passed':sum(counts.values()),'exact_inventory_and_execution_match':True,'shards':report}
pathlib.Path('/private/tmp/nrm-ci-final-receipt.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out,indent=2))
