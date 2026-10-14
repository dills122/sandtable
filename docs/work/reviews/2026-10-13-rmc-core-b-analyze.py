import collections, datetime, hashlib, json, re
from pathlib import Path
CURRENT=Path('/private/tmp/rmc-core-b-attempt2/core-b')
PRIOR=Path('/private/tmp/rmc-prior-main-ci/ci-core-b-c43b9caec452c152a086412c779bb6495ef1a0ad-1/core-b')
def duration(text):
    return sum(float(value)*{'h':3600,'m':60,'s':1,'ms':0.001}[unit] for value,unit in re.findall(r'(\d+(?:\.\d+)?)\s*(ms|h|m|s)',text))
def read(folder):
    rows={}; classes=collections.defaultdict(lambda:dict(count=0,seconds=0,maxSeconds=0))
    for line in (folder/'execution.log').read_text().splitlines():
        m=re.fullmatch(r'(passed|failed|skipped) (.*) \(([^()]*)\)',line)
        if not m:continue
        status,name,time=m.groups(); seconds=duration(time)
        if name in rows:raise ValueError('duplicate '+name)
        rows[name]=dict(status=status,seconds=seconds)
        cls=name.split('(',1)[0].rsplit('.',1)[0]; group=classes[cls]
        group['count']+=1;group['seconds']+=seconds;group['maxSeconds']=max(group['maxSeconds'],seconds)
    timestamp=None; starts={}; ends={}; states={}; class_times=collections.defaultdict(list)
    for line in next((folder/'diagnostics').glob('*.diag')).read_text().splitlines():
        if re.match(r'^\d{4}-\d\d-\d\dT',line):
            timestamp=datetime.datetime.fromisoformat(line.split(' ',1)[0]);continue
        m=re.search(r'TestNode = TestNode \{ Uid = TestNodeUid \{ Value = ([^ ]+) \}, DisplayName = (.*?), Properties = \[',line)
        if not m:continue
        uid,name=m.groups()
        if 'InProgressTestNodeStateProperty' in line:
            starts[name]=timestamp;states[name]='in-progress'
        elif any(s+'TestNodeStateProperty' in line for s in ('Passed','Failed','Skipped')):
            ends[name]=timestamp;states[name]='complete'
    for name,start in starts.items():
        cls=name.split('(',1)[0].rsplit('.',1)[0];class_times[cls].append((start,ends.get(name)))
    spans={}
    for cls,times in class_times.items():
        start=min(x[0] for x in times);end=max((x[1] for x in times if x[1]),default=None)
        spans[cls]=dict(firstStart=start.isoformat(),lastEnd=end.isoformat() if end else None,
            elapsedToLastCompletion=(end-start).total_seconds() if end else None,
            started=len(times),finished=sum(x[1] is not None for x in times))
    active={n:s.isoformat() for n,s in starts.items() if n not in ends}
    return dict(rows=rows,classes=dict(classes),spans=spans,active=active,
        firstStart=min(starts.values()).isoformat(),lastEnd=max(ends.values()).isoformat(),
        completedRows=len(rows),startedRows=len(starts),summedRowSeconds=sum(x['seconds'] for x in rows.values()),
        sourceHashes={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in [folder/'execution.log',next((folder/'diagnostics').glob('*.diag'))]})
a=read(CURRENT);b=read(PRIOR)
shared=set(a['rows'])&set(b['rows']);missing=sorted(set(b['rows'])-set(a['rows']))
comparisons=[]
for cls,x in a['classes'].items():
    names=[n for n in shared if n.split('(',1)[0].rsplit('.',1)[0]==cls]
    old=sum(b['rows'][n]['seconds'] for n in names);new=sum(a['rows'][n]['seconds'] for n in names)
    comparisons.append(dict(cls=cls,currentCount=x['count'],baselineCount=b['classes'][cls]['count'],
        matchedCount=len(names),currentMatchedSeconds=new,baselineMatchedSeconds=old,ratio=new/old if old else None,
        currentSpan=a['spans'].get(cls),priorSpan=b['spans'].get(cls)))
comparisons.sort(key=lambda x:x['currentMatchedSeconds'],reverse=True)
out=dict(current=a,prior=b,matchedRows=len(shared),missingRows=missing,
    baselineMissingSeconds=sum(b['rows'][n]['seconds'] for n in missing),
    currentMatchedSeconds=sum(a['rows'][n]['seconds'] for n in shared),
    priorMatchedSeconds=sum(b['rows'][n]['seconds'] for n in shared),comparisons=comparisons)
Path('/private/tmp/rmc-core-b-analysis.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps({k:v for k,v in out.items() if k not in ('current','prior','comparisons','missingRows')},indent=2))
for tag,data in [('current',a),('prior',b)]:
    print(tag,data['completedRows'],data['startedRows'],data['summedRowSeconds'],data['firstStart'],data['lastEnd'],'active',data['active'])
for row in comparisons[:12]:print(json.dumps(row))
print('missing rows',len(missing),'baseline summedseconds',out['baselineMissingSeconds'])
