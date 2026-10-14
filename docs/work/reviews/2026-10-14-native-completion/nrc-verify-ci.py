import pathlib,json,xml.etree.ElementTree as E,collections,hashlib
p=pathlib.Path('/private/tmp/nrc-ci-final'); i=json.loads((p/'inventory.json').read_text()); local=json.loads(pathlib.Path('/private/tmp/nrc-local-reconciliation-inventory.json').read_text());sha='1eb26347695f21c9e9393d5a7c34581457b062e1'; assert i['tested_sha']==sha
counts={'core-a':71,'core-b':2393,'exercise':469,'contracts':10}; report={}
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
out={'pr':183,'head':'3491be42ce1a7b3361e47d0445d7dd85df4d2fb2','base':'a2a21cdfa8511f55efdde56eb11d9bad09537c4a','tested_merge':sha,'tested_tree':'964b97762eb7dcc6149f58450dee50fde3882105','run':38503950210,'total_passed':sum(counts.values()),'exact_inventory_and_execution_match':True,'shards':report}
pathlib.Path('/private/tmp/nrc-ci-final-receipt.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out,indent=2))
