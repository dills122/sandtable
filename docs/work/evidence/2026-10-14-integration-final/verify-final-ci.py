import pathlib,json,xml.etree.ElementTree as E,collections,hashlib
p=pathlib.Path('/private/tmp/n3-budget-ci-final'); i=json.loads((p/'inventory.json').read_text()); local=json.loads(pathlib.Path('/private/tmp/n3-budget-ci-inventory/inventory.json').read_text());sha='3509712155065622358eb144b2b674aac13884ae'; assert i['tested_sha']==sha
counts={'core-a':71,'core-b':2511,'core-c':130,'core-d':129,'exercise':469,'contracts':10}; report={}
assert set(i['shards'])==set(counts)
assert i['run_attempt']=='1' and i['configuration']=='Release' and i['sdk']==local['sdk']
assert i==local
for k,n in counts.items():
 st=json.loads((p/k/'status.json').read_text());assert st['exit_code']==0 and st['tested_sha']==sha and st['run_attempt']=='1' and st['configuration']=='Release'
 assert st['sdk']==i['sdk'] and st['shard']==k and st['module']==i['shards'][k]['module']
 rows=collections.Counter(map(tuple,i['shards'][k]['rows']));assert len(rows)==n and sum(rows.values())==n
 assert rows==collections.Counter(map(tuple,local['shards'][k]['rows']))
 actual=collections.Counter();files=list((p/k/'results').glob('*.xml'));assert len(files)==1
 for f in files:
  root=E.parse(f).getroot()
  for a in root.iter('assembly'):
   assert all(int(a.attrib.get(z,'0'))==0 for z in ['errors','failed','not-run','skipped'])
  for t in root.iter('test'):
   assert t.attrib['result']=='Pass';actual[(i['shards'][k]['module'],t.attrib['type']+'.'+t.attrib['method'],json.loads(chr(34)+t.attrib['name']+chr(34)))]+=1
 assert actual==rows,(k,len(actual),len(rows))
 report[k]={'passed':n,'failed':0,'skipped':0,'xml_sha256':{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in files}}
out={'pr':190,'head':'37b372a07f743409e2efae6a44635b834b043a2c','base':'2e18cfebf33a69ea9204fccd96cf7df86a9aa26e','tested_merge':sha,'tested_tree':'6f485236021b0fb91dc7bb122b5114708f804ff7','run':38656131921,'total_passed':sum(counts.values()),'exact_inventory_and_execution_match':True,'shards':report}
pathlib.Path('/private/tmp/n3-budget-ci-final-receipt.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out,indent=2))
