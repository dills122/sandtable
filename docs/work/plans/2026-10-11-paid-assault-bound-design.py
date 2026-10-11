import sys
import json,importlib.util
from pathlib import Path
repo=(Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[3]).resolve()
p=repo/'docs/specs/verify-combat-actual-assault-v1.py'
s=importlib.util.spec_from_file_location('aad_bounds',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
schema=json.loads((repo/'docs/specs/combat-actual-paid-assault-v1.schema.json').read_text())
grammar=m.GRAMMAR|{k:[tuple(x.split(':')) for x in v.split()] for k,v in schema['objects'].items()}
# Semantic graph: commit,resolve, optional retreat-open, disposition, losses,
# retreat, optional custody-open, optional custody, relations, round-close, CA.
paths={}
for retreat in (False,True):
 for custody in (False,True):
  for ropen in ((False,True) if retreat else (False,)):
   for copen in ((False,True) if custody else (False,)):
    phases=['commit','resolve']+(['retreat-open'] if ropen else [])+['disposition','losses','retreat']+(['custody-open'] if copen else [])+(['custody'] if custody else [])+['relationships','round-close','ca-complete']
    paths[str((retreat,custody,ropen,copen))]=phases
maxevents=max(map(len,paths.values()))
arrays={'CommitEffect.allocations':2,'CommitEffect.costs':2,'AssaultResult.draws':9,'Draw.consumed':512,'CloseEffect.proofReceipts':5,'PaidControl.attackHistory':1,'PaidControl.targetUses':1,'PaidControl.receipts':maxevents,'World.elements':2,'World.representations':2,'World.brokenVehicleLots':0,'World.cohesionCauses':4,'World.relationships':1,'World.custodyLots':1,'World.guards':1,'World.replacementEntitlements':1,'World.futureObligations':1,'World.settlements':1,'Settlement.preLossElements':2,'Losses.roles':2,'Element.components':1,'Origin.references':16,'Representation.boundElementIds':1}
primitives={'byte':3,'id':130,'hash':73,'rawHash':66,'locator':130,'string':1538,'text':1538,'side':14,'actor':14,'role':10,'bool':5,'null':4,'int':11,'long':20,'ulong':20,'utc':15,'futureTurn':3,'adjustment':2,'differential':2,'die':1,'Route':9*131+1,'LegacyBrokenVehicleLot':0,'empty':2}
# Preserve upstream parser byte bounds for entire authenticated predecessor proof.
cache={}
def size(kind,parent='',field=''):
 if kind.endswith('?'):return max(4,size(kind[:-1],parent,field))
 if kind.endswith('[]'):
  count=arrays[parent+'.'+field]
  childsize=3 if parent=='Draw' and field=='consumed' else size(kind[:-2])
  return 2+max(0,count-1)+count*childsize
 if kind=='AdmissionProof':return 1048576
 if kind=='PaidEffect':return max(size(k) for k in schema['effectTags'].values())
 if kind in primitives:return primitives[kind]
 fields=grammar[kind]
 return 2+max(0,len(fields)-1)+sum(len(json.dumps(k))+1+size(t,kind,k) for k,t in fields)
results={k:size(k) for k in ('PaidInput','PaidEvent','PaidControl','PaidProof','AssaultResult','World')}
# Nested canonical ASCII JSON escapes at most twice, plus quotes.
source_bound=2+len('"contractVersion":1,"admissionSourceCanonicalUtf8":,"paidEventCanonicalUtf8":')+2*1048576+2+2+maxevents*(2*results['PaidEvent']+2)+maxevents-1
results['PaidSource']=source_bound
results['PaidLedger']=2+maxevents*results['PaidInput']+maxevents-1
out=dict(status='author structural bound proof; not a paid trace; GREEN sensitivity and final consumption remain',maxAcceptedEvents=maxevents,acceptedPhasePaths=paths,arrayBounds=arrays,primitiveEncodedUpperBounds=primitives,encodedUpperBounds=results,candidateCaps={k:1<<(v-1).bit_length() for k,v in results.items()},remainingVerification=['GREEN handler/framing/capacity failure sensitivity','final cold/warm positive/negative/retry/helper consumption closure before literals/public paid proof acceptance'],scopeJustification=['immutable admitted origin references1, conservatively dominated by16','owned readonly/enum strings dominated by256; inherited IDs/locators128','PositiveEntry exact opening hashes and seed1/cursor2, ARE no RNG advancement','byte grammar0..255,9draws/512consumed dominates isolated support; genuine admitted resolution8single-byte draws','eight mandatory publications plus at most3 optional, retry/no-op does not append'])
Path('/private/tmp/paid-bound-design.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps({k:v for k,v in out.items() if k not in ('acceptedPhasePaths','arrayBounds','primitiveEncodedUpperBounds')},indent=2))
