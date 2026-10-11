import sys
import json,importlib.util,time
from pathlib import Path
repo=(Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[3]).resolve()
started=time.monotonic()
p=repo/'docs/specs/verify-combat-actual-assault-v1.py'
s=importlib.util.spec_from_file_location('aad_shape_probe',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
f=json.loads(m.FIXTURE.read_bytes());world=m.are.selection.env.world
maximum={};origins=[];readonly_strings=[];maxdepth=0;observations=[]
def depth(v):
 return 1+max((depth(x) for x in (v.values() if isinstance(v,dict) else v)),default=0) if isinstance(v,(dict,list)) else 0
for c in f['cases']:
 proof=json.loads(c['proof']['canonicalUtf8']);w=proof['actualRoundProof']['control']['world']
 maxdepth=max(maxdepth,depth(proof))
 def walk(v):
  if isinstance(v,dict):
   if 'references' in v and 'kind' in v:origins.append(len(v['references']))
   for x in v.values():walk(x)
  elif isinstance(v,list):
   for x in v:walk(x)
  elif isinstance(v,str):readonly_strings.append(len(v))
 walk(w)
 t=time.monotonic();count=0
 # Sample each distinct outcome summary once; this is old isolated comparator
 # grammar sizing, never an actual paid source or proof.
 unique={}
 for row in m.catalogue_records(m.normalized_rules()):
  if row['kind']=='capture-refusal':
   facts=row['facts'];key=tuple(facts[x] for x in ('attackerPercent','defenderPercent','rawEngaged','requiredRetreat','capturedRole','captureShare'))+(row['refusalHexes'],)
   unique[key]=row
 for row in unique.values():
  facts=row['facts'];ret='not-required' if not facts['requiredRetreat'] else 'refuse-retreat' if row['refusalHexes'] else 'retreat'
  capture=row['attackerCapturedToe']+row['defenderCapturedToe']>0
  for custody in ('relocate-and-guard','leave-unguarded') if capture else (None,):
   case=dict(settlementId='set.'+'f'*64,creationBinding=w['creationBinding'],attackerSide=c['owner'],preAssaultCp=[0,0],differential=facts['differential'],attackerCoordinate=facts['attackerCoordinate'],defenderCoordinate=facts['defenderCoordinate'],captureDie=facts['captureDie'],retreatChoice=ret,custodyChoice=custody)
   for cut in world.CUTS:
    if cut=='custody' and not capture:continue
    value=world.world_at(case,cut);world.typed(value,'World');b=world.encode(value);maximum[cut]=max(maximum.get(cut,0),len(b));count+=1
   if time.monotonic()-started>110:raise RuntimeError('bounded120second budget exceeded')
 observations.append(dict(owner=c['owner'],firstSeal=c['firstSeal'],distinctSizeSummaries=len(unique),isolatedWorldShapes=count,elapsedSeconds=round(time.monotonic()-t,3)))
out=dict(scope='unchanged historical mechanical World shape sizing only; not actual paid proofs',cases=observations,largestOriginReferences=max(origins),largestRetainedWorldString=max(readonly_strings),admissionProofDepth=maxdepth,isolatedWorldByteMaxima=maximum)
Path('/private/tmp/paid-valid-shape-observation.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps(out,indent=2))
