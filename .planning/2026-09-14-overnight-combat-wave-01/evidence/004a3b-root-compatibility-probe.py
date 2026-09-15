"""Read-only frozen A3b compatibility check against accepted A3a/nav commit."""
import ast,hashlib,json,subprocess
from pathlib import Path
BASE='5fea52176a358121927b12f35c70ae333fca9ba0'
FILES=['docs/specs/combat-side-projection-v1.schema.json','docs/specs/fixtures/combat-side-projection-v1.json']
def old(path):return subprocess.check_output(['git','show',BASE+':'+path])
def encoded(value):return json.dumps(value,ensure_ascii=True,separators=(',',':')).encode()
counts=dict(schema_leaves=0,fixture_sections=0,unchanged_ast=0,extension_ast=0)
def preserve(a,b,path):
    if isinstance(a,dict):
        assert isinstance(b,dict),path
        for key,value in a.items():
            assert key in b,path+'.'+key
            preserve(value,b[key],path+'.'+key)
    else:
        assert encoded(a)==encoded(b),path
        counts['schema_leaves']+=1
old_schema=json.loads(old(FILES[0]));new_schema=json.loads(Path(FILES[0]).read_bytes())
preserve(old_schema,new_schema,'schema')
old_types=set()
for key,value in old_schema.items():
    if isinstance(value,dict) and key.startswith(('objects','enums','integerBounds','types')):old_types.update(value)
assert not (set(new_schema.get('enums3b',{})) & old_types),'new enum shadows accepted type'

a=json.loads(old(FILES[1]));b=json.loads(Path(FILES[1]).read_bytes())
for key,value in a.items():
    assert key in b and encoded(value)==encoded(b[key]),'fixture.'+key
    counts['fixture_sections']+=1
p='docs/specs/verify-combat-side-projection-v1.py'
def definitions(data):
    return {v.name:ast.dump(v,include_attributes=False) for v in ast.parse(data).body if isinstance(v,(ast.FunctionDef,ast.AsyncFunctionDef,ast.ClassDef))}
a=definitions(old(p));b=definitions(Path(p).read_bytes())
for name,value in a.items():
    assert name in b,name
    if value==b[name]:counts['unchanged_ast']+=1
    else:
        assert name in {'generated_fixture','test_a1_preserved','test_a3_preserved','typed3','main'},name
        counts['extension_ast']+=1
print('PASS accepted A3a compatibility',counts)
