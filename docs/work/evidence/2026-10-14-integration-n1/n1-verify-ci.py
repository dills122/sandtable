import pathlib,json,xml.etree.ElementTree as E,collections,hashlib
p=pathlib.Path('/private/tmp/n1-ci-final'); i=json.loads((p/'inventory.json').read_text()); local=json.loads(pathlib.Path('/private/tmp/n1-ci-inventory/inventory.json').read_text());sha='d77e097cf6e094d3620591622ade01c7d0316d9a'; assert i['tested_sha']==sha
counts={'core-a':71,'core-b':2441,'core-c':130,'exercise':469,'contracts':10}; report={}
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
out={'pr':188,'head':'33120fe9cecb15449f7dfe443721f165cc02e736','base':'f59ffcdbcbcadfbdb20b7e7f8796d477533b792f','tested_merge':sha,'tested_tree':'6d1175501abffe25b979dd3c8a50264a5f9cb00a','run':38612754520,'total_passed':sum(counts.values()),'exact_inventory_and_execution_match':True,'shards':report}
pathlib.Path('/private/tmp/n1-ci-final-receipt.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out,indent=2))
