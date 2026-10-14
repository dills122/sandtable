import pathlib,json,xml.etree.ElementTree as E,collections,hashlib
p=pathlib.Path('/private/tmp/n2-ci-final'); i=json.loads((p/'inventory.json').read_text()); local=json.loads(pathlib.Path('/private/tmp/n2-ci-inventory/inventory.json').read_text());sha='b7ac12ee4c78baeb18a7c7872bc498e888ee20fe'; assert i['tested_sha']==sha
counts={'core-a':71,'core-b':2511,'core-c':130,'exercise':469,'contracts':10}; report={}
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
out={'pr':189,'head':'7be10e5600a1833a2aa7cfcc68dfa883bf59992b','base':'4521148a1d2faabf3b3e24ecc21f8b8ef3502761','tested_merge':sha,'tested_tree':'15771f6d7e87a72fb02ae2bff467683f185948b6','run':38630698865,'total_passed':sum(counts.values()),'exact_inventory_and_execution_match':True,'shards':report}
pathlib.Path('/private/tmp/n2-ci-final-receipt.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out,indent=2))
