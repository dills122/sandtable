"""Estimate schedule changes from measured class costs; projections are not execution."""
from pathlib import Path
import json,heapq
p=Path('/private/tmp/b00-analysis.json');data=json.loads(p.read_text());chosen=set(data['candidateClasses']);models={}
for label,job in [('pr',1176),('main',1197)]:
 classes=data[label]['core-b']['classes'];ordered=sorted(classes,key=lambda k:(classes[k]['firstStart'],k))
 def model(names):
  workers=[(0.0,i) for i in range(4)];heapq.heapify(workers);schedule=[]
  for name in names:
   start,worker=heapq.heappop(workers);duration=max(classes[name]['seconds'],classes[name]['span']);finish=start+duration;schedule.append(dict(cls=name,worker=worker,start=start,finish=finish,seconds=duration));heapq.heappush(workers,(finish,worker))
  return dict(seconds=max(x[0] for x in workers),schedule=schedule)
 original=model(ordered);b=model([c for c in ordered if c not in chosen]);c=model([cl for cl in ordered if cl in chosen]);observed=data[label]['core-b']['assemblySeconds'];overhead=job-observed
 models[label]=dict(observedOriginal=observed,originalModel=original,modelErrorPercent=(original['seconds']/observed-1)*100,remainingB=b,newC=c,jobOverhead=overhead,projectedBJob=b['seconds']+overhead,projectedCJob=c['seconds']+overhead)
data['models']=models;p.write_text(json.dumps(data,indent=2)+'\n')
