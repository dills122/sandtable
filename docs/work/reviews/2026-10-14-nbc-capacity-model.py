import json,hashlib,xml.etree.ElementTree as E
from pathlib import Path
from datetime import datetime
sources={}
def load(path):
    sources[str(path)]=hashlib.sha256(path.read_bytes()).hexdigest();tree=E.parse(path);asm=tree.find('.//assembly');classes={}
    for t in tree.findall('.//test'):
        assert t.attrib['result']=='Pass';k=t.attrib['type'];s=datetime.fromisoformat(t.attrib['start-rtf']);f=datetime.fromisoformat(t.attrib['finish-rtf']);x=classes.setdefault(k,dict(start=s,finish=f,total=0,rows=0));x['start']=min(x['start'],s);x['finish']=max(x['finish'],f);x['total']+=float(t.attrib['time']);x['rows']+=1
    out={k:dict(rows=x['rows'],summedSeconds=x['total'],spanSeconds=(x['finish']-x['start']).total_seconds(),cost=max(x['total'],(x['finish']-x['start']).total_seconds()),start=x['start'].isoformat()) for k,x in classes.items()};return float(asm.attrib['time']),out
bwall,b=load(next(Path('/private/tmp/b02-ci-final/core-b/results').glob('*.xunit.xml')));cwall,c=load(next(Path('/private/tmp/b02-ci-final/core-c/results').glob('*.xunit.xml')));lwall,l=load(Path('docs/work/reviews/2026-10-13-nrc-final.xunit.xml'));nwall,n=load(Path(json.load(open('/private/tmp/b03-final.json'))['xmlPath']))
ratios={k:c[k]['cost']/l[k]['cost'] for k in c};forecast={k:v['cost']*ratios[k.replace('Breakdown','Movement')] for k,v in n.items()}
order=sorted(b,key=lambda k:b[k]['start'])
def schedule(pairs):
    workers=[0.0]*4
    for k,cost in pairs:
        worker=min(range(4),key=lambda i:workers[i]);workers[worker]+=cost
    return max(workers),workers
baseline,workers=schedule([(k,b[k]['cost']) for k in order]);overB=958-bwall;overC=440-cwall
modes={}
for mode,sequence in [('append',[(k,b[k]['cost']) for k in order]+list(forecast.items())),('prepend',list(forecast.items())+[(k,b[k]['cost']) for k in order]),('alphabetical',sorted([(k,b[k]['cost']) for k in order]+list(forecast.items())))]:
    wall,w=schedule(sequence);modes[mode]=dict(projectedAssembly=wall,projectedJob=wall+overB,headroomTo1200=1200-wall-overB,workers=w)
cmax=max([x['cost'] for x in c.values()]+list(forecast.values()));sensitivity={str(f):dict(projectedJob=f*cmax+overC,headroomTo900=900-f*cmax-overC) for f in [1,1.25,1.5,2]}
out=dict(parentHead='be149cb5c2fd0226c1ca90a5c630af0e3aa10b73',parentRun=38517061509,nativeProduct='bfc4cfe357529b4bdfe5d997b09d70367824edd4',parentB=dict(job=958,assembly=bwall,overhead=overB,modelAssembly=baseline,errorPercent=100*(baseline/bwall-1),classes=b),parentC=dict(job=440,assembly=cwall,overhead=overC,classes=c),oldLocalRMC=dict(assembly=lwall,classes=l),newLocalNBC=dict(assembly=nwall,classes=n),observedClassHostLocalRatios=ratios,projectedNativeClassCosts=forecast,retainB=modes,moveFullClassesToC=dict(fourCollectionCriticalCost=cmax,projectedJob=cmax+overC,sensitivity=sensitivity),sourceHashes=sources,note='Read-only observed-class schedule/sensitivity; not actual hosted NBC execution, no future latency guarantee or host resource cause established.')
Path('/private/tmp/b03-capacity-proposal.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps({k:v for k,v in out.items() if k in ['observedClassHostLocalRatios','projectedNativeClassCosts','retainB','moveFullClassesToC']}))
