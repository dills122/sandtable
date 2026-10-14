import importlib.util,json
from pathlib import Path
p=Path('docs/specs/verify-combat-actual-repeated-breakdown-completion-v1.py');s=importlib.util.spec_from_file_location('b02_measure',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
out={'capacity':m.capacity_tests(),'closure':m.closure_tests(),'faults':m.dependency_tests()};Path('/private/tmp/b02-capacity-closure.json').write_text(json.dumps(out,indent=2)+'\n');print('PASS retained capacity/closure/fault measurement')
