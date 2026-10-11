# DAY-B retained experimental evidence

Date:2026-10-10 America/Toronto. Baseline34a494c46ab7b8665e381f9e531c7b8365cb3efe. Assigned research worktree only. No runtime/frozen source edits; no .NET suite. These are copied temporary probe sources/results for durable reproduction, not production test tools or admission APIs.

## Exact temporary artifact manifest

| Artifact | SHA256 |
| --- | --- |
| `cycle-sequence.log` | `17ecde8583d089b5c161c8c68853473cd0d687808a189e44fb435d62a6fc5916` |
| `inherited-breakdown-completion.log` | `b74c24e87d0c1d878c6d04453e2a4abed764bc3175d6a0599f00184288fb6e2c` |
| `inherited-snapshot.log` | `d956367025f5223dd6f0049a95f5fe7d4ed498ac9998a388b55d1845e825fb2a` |
| `maintenance_probe.json` | `5a099c32f1716bafdd387c3c9eb54b3de01df4b67223c4897824e394c2576cef` |
| `maintenance_probe.py` | `c2f24b972e77c341d3803a7f67981ae5e496496cea76f7f16a081dcbf39c4b75` |
| `oracles.json` | `ef4d146a46d04bfae790ad9c4f6f150c27174834c6f53b2167744e99fff191f9` |
| `oracles.py` | `cbe7c87c30a228afc151340e2661f2c3888f03b65157af559e569952292a2f94` |
| `outward-composition.log` | `d4941cec6bb37b3ac37a50a41cfa237ac51256945ed7cf6059af42f49fb72c9a` |
| `probe.json` | `fd8f3fa967659475d4062fdb723e88c34ab73414da26444c451a06224e97d599` |
| `probe.py` | `1757f735e769f116e596d2daa214559d88cbb8ec1c9f7add48d9bef9317a5ef4` |
| `projection_probe.json` | `7ddeed9ef5e9d64306f516f73d8b03576bb73fb4691e1755e5bfa505c2aaeb82` |
| `projection_probe.py` | `13ef489ff24731dfe4248f7dc7cb3e12b3cce35d08d57ab89bd3e8b8eae7462c` |
| `surface.json` | `84310743b9cf0cf1047b9ef7b2de320461e786588bfe1bc7bb73b83d8445f117` |
| `surface.py` | `4f8ee32b77b4523eb45132742a7a4b558687ef26668ebafaef33960ba56660ba` |

## probe: retained source

```python
import importlib.util,json,pathlib,sys,hashlib,platform,time
sys.dont_write_bytecode=True
root=pathlib.Path('/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable')
def load(name,path):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
start=time.monotonic()
are=load('are',root/'docs/specs/verify-combat-actual-round-entry-v1.py')
rng=load('rng',root/'docs/research/verify-combat-rng.py')
res=load('res',root/'docs/specs/verify-combat-result-settlement-v2.py')
fixture=json.loads(are.FIXTURE.read_bytes());observed=[]
for row in fixture['cases']:
 if row['cancellation'] is not None:continue
 source=are.parse(row['source']['canonicalUtf8'].encode('ascii'),'ActualRoundSource')
 sl=[are.selection.parse(x['canonicalUtf8'].encode('ascii'),'Input') for x in row['trustedSelectionInputs']]
 rl=[are.parse(x['canonicalUtf8'].encode('ascii'),'RoundInput') for x in row['trustedRoundInputs']]
 b,s=are.replay(source,sl,rl)
 assert are.raw(s,'RoundControl')==row['controls'][-1]['canonicalUtf8'].encode('ascii')
 r=s['randomState'];labels,dice,ends,consumed,differential,captured=rng.trace(r['seed'],r['nextByteCursor'])
 facts=res.world.result_facts(differential,10*dice[4]+dice[5],10*dice[6]+dice[7],dice[-1] if captured else None)
 roles=[]
 for slot in s['slots']:
  e=next(e for e in s['world']['elements'] if e['elementId']==slot['allocation']['unit']['elementId'])
  cp=e['operationalState']['capabilityPointsExpended'];am=e['ammunition']['points'];cost=5 if slot['role']=='attacker' else 3
  roles.append(dict(role=slot['role'],owner=slot['owner'],beforeCp=cp,proposedAfterCp=cp['numerator']+cost,beforeAmmo=am,proposedAfterAmmo=am-10))
 rejection={}
 for label,call in [('oldRoundBase',lambda:res.rnd.read_base(are.raw(b,'Base'),None)),('newCommit',lambda:are.apply(source,sl,rl,are.trusted(are.command(b,s,'commit-attack'))))]:
  try:call();rejection[label]='ACCEPTED (unexpected)'
  except Exception as e:rejection[label]=str(e)
 observed.append(dict(owner=row['owner'],firstSeal=row['firstSeal'],stateVersion=s['stateVersion'],status=s['status'],stepIndex=s['stepIndex'],randomState=r,roles=roles,arithmeticProbe=dict(labels=labels,dice=dice,endCursor=ends[-1],consumedHex=consumed,differential=differential,capturedRole=captured,facts=facts),admissionRejections=rejection))
output=dict(base='34a494c46ab7b8665e381f9e531c7b8365cb3efe',python=sys.version,platform=platform.platform(),seconds=time.monotonic()-start,notes='ARE replay is genuine admission; rule/RNG/cost calculations are separate pure probes, no paid/result source constructed.',observed=observed)
print(json.dumps(output,indent=2))
```

## probe: observed output

```json
{
  "base": "34a494c46ab7b8665e381f9e531c7b8365cb3efe",
  "python": "3.14.6 (main, Jun 10 2026, 10:03:53) [Clang 21.0.0 (clang-2100.0.123.102)]",
  "platform": "macOS-26.6.2-arm64-arm-64bit-Mach-O",
  "seconds": 4.502144290992874,
  "notes": "ARE replay is genuine admission; rule/RNG/cost calculations are separate pure probes, no paid/result source constructed.",
  "observed": [
    {
      "owner": "axis",
      "firstSeal": "attacker",
      "stateVersion": 25,
      "status": "prepared",
      "stepIndex": 5,
      "randomState": {
        "contractVersion": 1,
        "algorithmId": "sandtable.sha256-counter.v1",
        "seed": 1,
        "nextByteCursor": 2
      },
      "roles": [
        {
          "role": "attacker",
          "owner": "axis",
          "beforeCp": {
            "numerator": 0,
            "denominator": 1
          },
          "proposedAfterCp": 5,
          "beforeAmmo": 10,
          "proposedAfterAmmo": 0
        },
        {
          "role": "defender",
          "owner": "commonwealth",
          "beforeCp": {
            "numerator": 0,
            "denominator": 1
          },
          "proposedAfterCp": 3,
          "beforeAmmo": 10,
          "proposedAfterAmmo": 0
        }
      ],
      "arithmeticProbe": {
        "labels": [
          "attacker.morale.tens",
          "attacker.morale.ones",
          "defender.morale.tens",
          "defender.morale.ones",
          "attacker.assault.tens",
          "attacker.assault.ones",
          "defender.assault.tens",
          "defender.assault.ones"
        ],
        "dice": [
          5,
          2,
          6,
          1,
          6,
          2,
          2,
          2
        ],
        "endCursor": 10,
        "consumedHex": "10375f90a18525c7",
        "differential": 0,
        "capturedRole": null,
        "facts": {
          "differential": 0,
          "attackerCoordinate": 62,
          "defenderCoordinate": 22,
          "captureDie": null,
          "attackerPercent": 0,
          "defenderPercent": 15,
          "rawEngaged": false,
          "requiredRetreat": 0,
          "capturedRole": null,
          "captureShare": 0
        }
      },
      "admissionRejections": {
        "oldRoundBase": "CMB-RND2-001",
        "newCommit": "CMB-ARE-007 /continuation"
      }
    },
    {
      "owner": "axis",
      "firstSeal": "defender",
      "stateVersion": 25,
      "status": "prepared",
      "stepIndex": 5,
      "randomState": {
        "contractVersion": 1,
        "algorithmId": "sandtable.sha256-counter.v1",
        "seed": 1,
        "nextByteCursor": 2
      },
      "roles": [
        {
          "role": "attacker",
          "owner": "axis",
          "beforeCp": {
            "numerator": 0,
            "denominator": 1
          },
          "proposedAfterCp": 5,
          "beforeAmmo": 10,
          "proposedAfterAmmo": 0
        },
        {
          "role": "defender",
          "owner": "commonwealth",
          "beforeCp": {
            "numerator": 0,
            "denominator": 1
          },
          "proposedAfterCp": 3,
          "beforeAmmo": 10,
          "proposedAfterAmmo": 0
        }
      ],
      "arithmeticProbe": {
        "labels": [
          "attacker.morale.tens",
          "attacker.morale.ones",
          "defender.morale.tens",
          "defender.morale.ones",
          "attacker.assault.tens",
          "attacker.assault.ones",
          "defender.assault.tens",
          "defender.assault.ones"
        ],
        "dice": [
          5,
          2,
          6,
          1,
          6,
          2,
          2,
          2
        ],
        "endCursor": 10,
        "consumedHex": "10375f90a18525c7",
        "differential": 0,
        "capturedRole": null,
        "facts": {
          "differential": 0,
          "attackerCoordinate": 62,
          "defenderCoordinate": 22,
          "captureDie": null,
          "attackerPercent": 0,
          "defenderPercent": 15,
          "rawEngaged": false,
          "requiredRetreat": 0,
          "capturedRole": null,
          "captureShare": 0
        }
      },
      "admissionRejections": {
        "oldRoundBase": "CMB-RND2-001",
        "newCommit": "CMB-ARE-007 /continuation"
      }
    },
    {
      "owner": "commonwealth",
      "firstSeal": "attacker",
      "stateVersion": 25,
      "status": "prepared",
      "stepIndex": 5,
      "randomState": {
        "contractVersion": 1,
        "algorithmId": "sandtable.sha256-counter.v1",
        "seed": 1,
        "nextByteCursor": 2
      },
      "roles": [
        {
          "role": "attacker",
          "owner": "commonwealth",
          "beforeCp": {
            "numerator": 0,
            "denominator": 1
          },
          "proposedAfterCp": 5,
          "beforeAmmo": 10,
          "proposedAfterAmmo": 0
        },
        {
          "role": "defender",
          "owner": "axis",
          "beforeCp": {
            "numerator": 0,
            "denominator": 1
          },
          "proposedAfterCp": 3,
          "beforeAmmo": 10,
          "proposedAfterAmmo": 0
        }
      ],
      "arithmeticProbe": {
        "labels": [
          "attacker.morale.tens",
          "attacker.morale.ones",
          "defender.morale.tens",
          "defender.morale.ones",
          "attacker.assault.tens",
          "attacker.assault.ones",
          "defender.assault.tens",
          "defender.assault.ones"
        ],
        "dice": [
          5,
          2,
          6,
          1,
          6,
          2,
          2,
          2
        ],
        "endCursor": 10,
        "consumedHex": "10375f90a18525c7",
        "differential": 0,
        "capturedRole": null,
        "facts": {
          "differential": 0,
          "attackerCoordinate": 62,
          "defenderCoordinate": 22,
          "captureDie": null,
          "attackerPercent": 0,
          "defenderPercent": 15,
          "rawEngaged": false,
          "requiredRetreat": 0,
          "capturedRole": null,
          "captureShare": 0
        }
      },
      "admissionRejections": {
        "oldRoundBase": "CMB-RND2-001",
        "newCommit": "CMB-ARE-007 /continuation"
      }
    },
    {
      "owner": "commonwealth",
      "firstSeal": "defender",
      "stateVersion": 25,
      "status": "prepared",
      "stepIndex": 5,
      "randomState": {
        "contractVersion": 1,
        "algorithmId": "sandtable.sha256-counter.v1",
        "seed": 1,
        "nextByteCursor": 2
      },
      "roles": [
        {
          "role": "attacker",
          "owner": "commonwealth",
          "beforeCp": {
            "numerator": 0,
            "denominator": 1
          },
          "proposedAfterCp": 5,
          "beforeAmmo": 10,
          "proposedAfterAmmo": 0
        },
        {
          "role": "defender",
          "owner": "axis",
          "beforeCp": {
            "numerator": 0,
            "denominator": 1
          },
          "proposedAfterCp": 3,
          "beforeAmmo": 10,
          "proposedAfterAmmo": 0
        }
      ],
      "arithmeticProbe": {
        "labels": [
          "attacker.morale.tens",
          "attacker.morale.ones",
          "defender.morale.tens",
          "defender.morale.ones",
          "attacker.assault.tens",
          "attacker.assault.ones",
          "defender.assault.tens",
          "defender.assault.ones"
        ],
        "dice": [
          5,
          2,
          6,
          1,
          6,
          2,
          2,
          2
        ],
        "endCursor": 10,
        "consumedHex": "10375f90a18525c7",
        "differential": 0,
        "capturedRole": null,
        "facts": {
          "differential": 0,
          "attackerCoordinate": 62,
          "defenderCoordinate": 22,
          "captureDie": null,
          "attackerPercent": 0,
          "defenderPercent": 15,
          "rawEngaged": false,
          "requiredRetreat": 0,
          "capturedRole": null,
          "captureShare": 0
        }
      },
      "admissionRejections": {
        "oldRoundBase": "CMB-RND2-001",
        "newCommit": "CMB-ARE-007 /continuation"
      }
    }
  ]
}
```

## surface: retained source

```python
import importlib.util,pathlib,json,sys,collections,hashlib
sys.dont_write_bytecode=True
root=pathlib.Path('/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable');spec=root/'docs/specs'
def load(n,p):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
w=load('world',spec/'verify-combat-world-settlement-v1.py');a=load('are',spec/'verify-combat-actual-round-entry-v1.py')
coords=w.source.COORDS;morale_counts=collections.Counter((1 if ac==11 else -1 if ac==66 else 0)-(1 if dc==11 else -1 if dc==66 else 0) for ac in coords for dc in coords);assert sorted(morale_counts)==w.source.DIFFS;rows=0;variants=0;count=collections.Counter();uniques=set();max_loss=max_captured=0
for diff in w.source.DIFFS:
 for ac in coords:
  for dc in coords:
   rows+=1;capture=ac//10+ac%10 in w.DATA['attacker_capture_sums'][str(diff)] or dc//10+dc%10 in w.DATA['defender_capture_sums'][str(diff)]
   for die in (range(1,7) if capture else (None,)):
    f=w.result_facts(diff,ac,dc,die)
    for refuse in (range(2) if f['requiredRetreat'] else range(1)):
     variants+=1;lossA=(10*f['attackerPercent']+99)//100;lossD=10*(f['defenderPercent']+10*refuse)//100
     capturedToe=((lossA if f['capturedRole']=='attacker' else lossD)*f['captureShare']+99)//100 if capture else 0
     max_loss=max(max_loss,lossA,lossD);max_captured=max(max_captured,capturedToe)
     assert all(0<=x<=3 for x in (lossA,lossD,capturedToe))
     unique=(diff,f['attackerPercent'],f['defenderPercent'],bool(f['rawEngaged']),f['requiredRetreat'],bool(refuse),f['capturedRole'],f['captureShare'],lossA,lossD,capturedToe)
     uniques.add(unique)
     count['retreat-required' if f['requiredRetreat'] else 'no-retreat']+=1
     count['positive-capture' if capturedToe else 'no-positive-capture']+=1
     if f['rawEngaged'] and not f['requiredRetreat'] and lossA==lossD==0:count['zero-loss-engaged']+=1
     if capture and capturedToe==0:count['capture-trigger-zero-rounded-loss']+=1
current=a.DEPENDENCIES
added=['docs/specs/combat-actual-round-entry-v1.md','docs/specs/combat-actual-round-entry-v1.schema.json','docs/specs/fixtures/combat-actual-round-entry-v1.json','docs/specs/verify-combat-actual-round-entry-v1.py','docs/specs/combat-result-settlement-v2.md','docs/specs/combat-result-settlement-v2.schema.json','docs/specs/fixtures/combat-result-settlement-v2.json','docs/specs/combat-sealed-round-v2.md','docs/specs/combat-world-settlement-v1.md','docs/design/combat-cost-resolution-order-v1.md','docs/design/combat-settlement-disclosure-v1.md']
out=dict(kind='pure selected-surface and dependency preflight; no source admission extension or settlement event generated',moralePairs=sum(morale_counts.values()),moraleDifferentialCounts=dict(sorted(morale_counts.items())),jointAssaultPairs=rows,refusalCaptureVariants=variants,distinctSemanticRecords=len(uniques),classificationCounts=dict(count),maximumLoss=max_loss,maximumPositiveCapturedToe=max_captured,areDependencies=len(current),arePinMismatches=[p for p,h in current.items() if hashlib.sha256((root/p).read_bytes()).hexdigest()!=h],candidateAdditionalDependencies=[dict(path=p,sha256=hashlib.sha256((root/p).read_bytes()).hexdigest()) for p in added if p not in current],areInventory=[dict(path=p,sha256=h) for p,h in current.items()])
print(json.dumps(out,indent=2))
```

## surface: observed output

```json
{
  "kind": "pure selected-surface and dependency preflight; no source admission extension or settlement event generated",
  "moralePairs": 1296,
  "moraleDifferentialCounts": {
    "-2": 1,
    "-1": 68,
    "0": 1158,
    "1": 68,
    "2": 1
  },
  "jointAssaultPairs": 6480,
  "refusalCaptureVariants": 8840,
  "distinctSemanticRecords": 637,
  "classificationCounts": {
    "no-retreat": 5560,
    "positive-capture": 888,
    "retreat-required": 3280,
    "no-positive-capture": 7952,
    "zero-loss-engaged": 498
  },
  "maximumLoss": 3,
  "maximumPositiveCapturedToe": 3,
  "areDependencies": 57,
  "arePinMismatches": [],
  "candidateAdditionalDependencies": [
    {
      "path": "docs/specs/combat-actual-round-entry-v1.md",
      "sha256": "ed4fd69b4f599acae7c4850270557363a546df36d0b984107c50c08c29304548"
    },
    {
      "path": "docs/specs/combat-actual-round-entry-v1.schema.json",
      "sha256": "677b1c671bebe5f23cf4b80f03078c55000c60e45416701757e2552cbd05edb8"
    },
    {
      "path": "docs/specs/fixtures/combat-actual-round-entry-v1.json",
      "sha256": "fc17cda07618bdd539a56b8c296037bbf36f37c6eb2ce6258c2b789b765f640b"
    },
    {
      "path": "docs/specs/verify-combat-actual-round-entry-v1.py",
      "sha256": "2596531cb50ddbae7f1e606284f3d845b224d4f1fc10f478b717843d1062990c"
    },
    {
      "path": "docs/specs/combat-result-settlement-v2.md",
      "sha256": "a040dbd69c8c5e978bbce79b8765088ab2efdc33e8ef95ced22a5f35680fbc68"
    },
    {
      "path": "docs/specs/combat-result-settlement-v2.schema.json",
      "sha256": "719634c4720b4a92669b2e83e60df58cd32d0e556ec9ef66c546aed1f9c7e9d6"
    },
    {
      "path": "docs/specs/fixtures/combat-result-settlement-v2.json",
      "sha256": "6a1f6cda74424680669539d73583fa3ba5affc612380b3a3efe2e8986a09804a"
    },
    {
      "path": "docs/specs/combat-sealed-round-v2.md",
      "sha256": "9fb337bdbfeebf0db367f860aafeb6a37b72af0d602036aec41da82678332bc5"
    },
    {
      "path": "docs/specs/combat-world-settlement-v1.md",
      "sha256": "1e6b0e5f02e0c90211a3a9436ae3f849f0828f4f6f7ec4452bb80ebc382fb87f"
    },
    {
      "path": "docs/design/combat-cost-resolution-order-v1.md",
      "sha256": "ffea13f61eecdf5baae9e6777d85386fe4971875a8c75274ab2df7bd6e36c2b0"
    },
    {
      "path": "docs/design/combat-settlement-disclosure-v1.md",
      "sha256": "84d85139ed0c7111b70c464bde47837963ed7f19571adbc5279e2f8cb1572616"
    }
  ],
  "areInventory": [
    {
      "path": "docs/specs/combat-positive-entry-v1.schema.json",
      "sha256": "73724384ba3cf61b462ef7b47ae70e5108281a6bbaf0b80878c55247e4e6fa36"
    },
    {
      "path": "docs/specs/fixtures/combat-positive-entry-v1.json",
      "sha256": "eb59146f34bfdc47753b9f0aff7f40709eba4266aff522655ac6886be15b951e"
    },
    {
      "path": "docs/specs/verify-combat-positive-entry-v1.py",
      "sha256": "b461a8bc6a194def5f03767783b49b5797a5630b6a7c26a986f7bfe2ee97c172"
    },
    {
      "path": "docs/specs/combat-reserve-designation-v1.schema.json",
      "sha256": "f324fb22e53ff6d9e084c2f65532e5af89277f632087549756b7df1dcb1710d9"
    },
    {
      "path": "docs/specs/fixtures/combat-reserve-designation-v1.json",
      "sha256": "6c8abb2038bb61c739e3bd3cab5c8adf00689c265ec3c6bc6ea29bebe66b12c2"
    },
    {
      "path": "docs/specs/verify-combat-reserve-designation-v1.py",
      "sha256": "fa40ec6b10d0d5ecc113aefba47ff4df44ae616851acdc827e9f676f2412e465"
    },
    {
      "path": "docs/specs/verify-combat-stage-entry-v1.py",
      "sha256": "85cd2b32f9ad104ec29034180775e1ac1905e5aa7f78caa3c1a26e6675901435"
    },
    {
      "path": "docs/specs/verify-combat-inherited-movement-lifecycle-v1.py",
      "sha256": "f2ee2292df3fe370ace289dcd01747153dac6d78b82c924b2f3da27df1d81920"
    },
    {
      "path": "docs/specs/combat-inherited-movement-lifecycle-v1.schema.json",
      "sha256": "aac4e07f9546d457defd9042ae701434683e7c22fab2071fe88f872d8d6bc072"
    },
    {
      "path": "docs/specs/verify-combat-inherited-breakdown-completion-v1.py",
      "sha256": "db903e019bb457c02930876e42a70b9204387494012419b0b97572c3c9e12eba"
    },
    {
      "path": "docs/specs/combat-inherited-breakdown-completion-v1.schema.json",
      "sha256": "1ff841c704e1aa46e71513ead562a0ca1c54c05b35bdf97018d6854d8773fec3"
    },
    {
      "path": "docs/specs/verify-combat-inherited-selection-v1.py",
      "sha256": "99e0cdb1cc78089e4af93b8e995ba04483f6bdab86edaf7c6e2e251442cd0bb7"
    },
    {
      "path": "docs/specs/verify-combat-selection-steps-v1.py",
      "sha256": "dc037282f74c34246d350faebeea55e81677d27dd181de858b8270e4eaa73c86"
    },
    {
      "path": "docs/specs/combat-selection-steps-v1.schema.json",
      "sha256": "ef24e125012ab390bb0c932a09472b8220d7e8c95c1b19c29bdb611be532162d"
    },
    {
      "path": "docs/specs/verify-combat-sealed-round-v2.py",
      "sha256": "d1091b5d1d6fb1d9ac88da45313c88fcbc922f763e62d01af44311abc8656bf9"
    },
    {
      "path": "docs/specs/verify-combat-result-settlement-v2.py",
      "sha256": "a6a781976f26b78a9dc098ebfbb3b516284aa1d5873744d96d8fa23aa50d83a2"
    },
    {
      "path": "docs/research/fixtures/combat-selected-source-v1.json",
      "sha256": "e225bc650c4bb942e41245a33da20c21c576ab117807a06805e2558a2c6468df"
    },
    {
      "path": "docs/research/verify-combat-rng.py",
      "sha256": "f3c7c468f2be0efa583e44c4d04b8fe6d20eef51aae01554bfa535b0ecc0ea30"
    },
    {
      "path": "docs/research/verify-combat-source-freeze.py",
      "sha256": "f21a97150e344337f6ee4c2c1a1752ac7d41b8b8c8bf6a61a06a1fe5a0483e7e"
    },
    {
      "path": "docs/specs/combat-actual-selection-v1.schema.json",
      "sha256": "da6256deb94bb6061e8e98e2448f915c4474373b7f0de6bb76eded2ae88c39b1"
    },
    {
      "path": "docs/specs/combat-authority-envelope-v1.schema.json",
      "sha256": "91f90ba47cdfff088b3d8e5dba55c6c283d41c96381e56c7bba115bfb5390550"
    },
    {
      "path": "docs/specs/combat-cycle-control-v1.schema.json",
      "sha256": "8c517db6daa734d415bd1d474280c2cf6e65013490c4a2568795d50e6112f7da"
    },
    {
      "path": "docs/specs/combat-cycle-sequence-v1.schema.json",
      "sha256": "6e09dc5bc6ca79d8580db6ed1e76ad5007c26a30df11b4dd9da0a9c7b5f5e0c5"
    },
    {
      "path": "docs/specs/combat-inherited-movement-v1.schema.json",
      "sha256": "a9ad67ee9b9302f17582fb5087f6dd6e7e62574741e6cea7b5a3e58bf7fe2069"
    },
    {
      "path": "docs/specs/combat-inherited-successors-v1.schema.json",
      "sha256": "474d8b5b22457921082c1799c89a42d9ec448b5c6ea131a3458f364ff980babc"
    },
    {
      "path": "docs/specs/combat-opening-preamble-v1.schema.json",
      "sha256": "d01bd4eb60ce3a83a4d119631ac72ccce082ff13cdf92f861d42fb83566e7d59"
    },
    {
      "path": "docs/specs/combat-ordinary-movement-v1.schema.json",
      "sha256": "039d5f3cdb908dc761540b4809bfb9c6e77c9ab6efd8c965e69955fbbcf0728a"
    },
    {
      "path": "docs/specs/combat-reserve-release-v1.schema.json",
      "sha256": "10ba1ca46e05fbed52c6eef5174c2ac84c4282df0957c44eab6e86079342bc32"
    },
    {
      "path": "docs/specs/combat-result-settlement-v1.schema.json",
      "sha256": "f65c13687bb2fe3fbabdbd82d48298430330dab756583fbfda17dae50bc73ab0"
    },
    {
      "path": "docs/specs/combat-rules-inputs-v1.schema.json",
      "sha256": "1477a9e755a091bbf4539d00184711b18eab2f6289f643596d841b8c314f4797"
    },
    {
      "path": "docs/specs/combat-sealed-round-v1.schema.json",
      "sha256": "5556404caa296ce41020654f8fb64c0349fd13051edabf6b4934e47d675c32db"
    },
    {
      "path": "docs/specs/combat-stage-entry-v1.schema.json",
      "sha256": "5d0f85fec61f219e7fd3575037894c147580ec9fe1570603d621c9d76b38889e"
    },
    {
      "path": "docs/specs/combat-weather-v1.schema.json",
      "sha256": "44f2fa44af30bf723ecac88db08fc9efac85e9880624635fc73f4dc52bd9459f"
    },
    {
      "path": "docs/specs/combat-world-settlement-v1.schema.json",
      "sha256": "f892545f5dac693c55d1180ad771d95ffeec508176a0d6139b82752ed46bcf0e"
    },
    {
      "path": "docs/specs/fixtures/combat-actual-selection-v1.json",
      "sha256": "019d1a3ff0f121d83f377ddfb19d274b4a8aa89172bad8aeb289c3b98228e604"
    },
    {
      "path": "docs/specs/fixtures/combat-authority-envelope-v1.json",
      "sha256": "adf7b05e15f863b08f366809adf91c97b39480e404ce2efff15e838fa4043eaa"
    },
    {
      "path": "docs/specs/fixtures/combat-content-v7.canonical.json",
      "sha256": "ee4fde9638ceb61ec08612fe32f9ac81db05572aac5ca20941e402d2fed25847"
    },
    {
      "path": "docs/specs/fixtures/combat-creation-ledger-v1.json",
      "sha256": "4b7f87f73a5882800e4e4befb4c06c20bf19c419f92edba5028b355e37442684"
    },
    {
      "path": "docs/specs/fixtures/combat-cycle-sequence-v1.json",
      "sha256": "de89ebcd86f171119d206d0a90553cdeb815e7b8b6fc8903c9dfcacbd1db5b3f"
    },
    {
      "path": "docs/specs/fixtures/combat-rules-inputs-v1.json",
      "sha256": "5401cd9a690be8b81f19795f1f66c0c6fe24f5055a06cda5facd6193b3e0a29a"
    },
    {
      "path": "docs/specs/verify-combat-actual-selection-v1.py",
      "sha256": "f37a7cfa26b60469168d9f4424465b14f1fe45666f4222d69c840c2527d2e66f"
    },
    {
      "path": "docs/specs/verify-combat-authority-envelope-v1.py",
      "sha256": "a12d8ce1ab142b9a7f1c58e07e5b0d7928c4693d869423aa24ade81cb21e6231"
    },
    {
      "path": "docs/specs/verify-combat-content-v7.py",
      "sha256": "eaab770988e8d2b0f11cb9a8a39b45f961977aa9048f95de8bf32f46e361bcbd"
    },
    {
      "path": "docs/specs/verify-combat-creation-ledger-v1.py",
      "sha256": "04777d27347b943cbb4ec06efeee6f3006694d1acd88b9bc8e25eb5dcb5841e4"
    },
    {
      "path": "docs/specs/verify-combat-cycle-control-v1.py",
      "sha256": "9039f9e1067b2cdd2e0a57ae16e711350b4bbdceb500d22bbbd715f39e8ccc9f"
    },
    {
      "path": "docs/specs/verify-combat-cycle-sequence-v1.py",
      "sha256": "d43ae974e1f6e2c30067efe4635df655c6d2e4791968ece00dfd473145c3cc8a"
    },
    {
      "path": "docs/specs/verify-combat-inherited-movement-v1.py",
      "sha256": "c10c51cd97fca3f3f04fc0983e90d474a35a6b58f4a2cce396272916910579e1"
    },
    {
      "path": "docs/specs/verify-combat-inherited-successors-v1.py",
      "sha256": "f9cbf73f2045c304c96fc8dcf4d09ba10026db87ff5446c751ad22e19e4fe5de"
    },
    {
      "path": "docs/specs/verify-combat-opening-preamble-v1.py",
      "sha256": "50f75c1032f5b17000accb288107a1e047940cadedd5c8d30c469db167c0e5d0"
    },
    {
      "path": "docs/specs/verify-combat-ordinary-movement-v1.py",
      "sha256": "cc75e52a6d160d4b0c6c880dc43bc65742068f83ef85cc7fb4babbf8756e0dff"
    },
    {
      "path": "docs/specs/verify-combat-reserve-release-v1.py",
      "sha256": "105ef18d9db6364ce42f9831afd3c33010fccc71b0ae60af78cec7c185da892b"
    },
    {
      "path": "docs/specs/verify-combat-result-settlement-v1.py",
      "sha256": "1f7dff5b87ce036c406562686f4dc1ce983a4a8dfa15b2401eae72a0d9879ffd"
    },
    {
      "path": "docs/specs/verify-combat-rules-inputs-v1.py",
      "sha256": "ffb6daf98db7097d307ac7c185bf7d68a2ff1d0ac9a357ea063a746aab42081d"
    },
    {
      "path": "docs/specs/verify-combat-sealed-round-v1.py",
      "sha256": "497e295e71c4b4da43539ee9b6c30de6d33f409a2f14b6cc24d9030390525ae0"
    },
    {
      "path": "docs/specs/verify-combat-weather-v1.py",
      "sha256": "c5f92de66c8c341553fe204a5ec1b0acffb162e61ee90332c14fd608f986ade8"
    },
    {
      "path": "docs/specs/verify-combat-world-settlement-v1.py",
      "sha256": "d5f49e5a4165ec568f0a8896ea491341a639da256bcfd573998f529e80ef9f25"
    },
    {
      "path": "tests/Cna.Core.Tests/Rules/Fixtures/cna-1979.1.weather-tables.v1.golden.json",
      "sha256": "7f134b9c5dcc356d48a9dfc51976a166b60f02fa93e73b4c0ccfdd354380c182"
    }
  ]
}
```

## projection_probe: retained source

```python
import importlib.util,pathlib,json,sys,copy
sys.dont_write_bytecode=True
root=pathlib.Path('/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable');spec=root/'docs/specs'
def load(n,p):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
are=load('are',spec/'verify-combat-actual-round-entry-v1.py');res=load('res',spec/'verify-combat-result-settlement-v2.py')
f=json.loads(are.FIXTURE.read_bytes());out=[]
def diff(a,b,p=''):
 if type(a)!=type(b):return [dict(path=p,actual=a,synthetic=b)]
 if isinstance(a,dict):
  if set(a)!=set(b):return [dict(path=p,actualKeys=list(a),syntheticKeys=list(b))]
  return [d for k in a for d in diff(a[k],b[k],p+'/'+k)]
 if isinstance(a,list):
  if len(a)!=len(b):return [dict(path=p,actualLength=len(a),syntheticLength=len(b))]
  return [d for i,(x,y) in enumerate(zip(a,b)) for d in diff(x,y,p+'/'+str(i))]
 return [] if a==b else [dict(path=p,actual=a,synthetic=b)]
for row in f['cases']:
 if row['cancellation'] is not None or row['firstSeal']!='attacker':continue
 s=json.loads(row['controls'][-1]['canonicalUtf8']);actual=copy.deepcopy(s['world'])
 for e in actual['elements']:
  e['operationalState']['capabilityPointsExpended']['numerator']+=5 if e['elementId']==s['slots'][0]['allocation']['unit']['elementId'] else 3;e['ammunition']['points']=0
 case=dict(settlementId='probe',creationBinding=actual['creationBinding'],attackerSide=row['owner'],preAssaultCp=[0,0],differential=0,attackerCoordinate=62,defenderCoordinate=22,captureDie=None,retreatChoice='not-required',custodyChoice=None)
 synthetic=res.world.world_at(case,'resolved');synthetic['settlements']=[]
 ds=diff(actual,synthetic)
 out.append(dict(owner=row['owner'],differenceCount=len(ds),differences=ds))
print(json.dumps(dict(note='Scratch payment and synthetic world factory comparison only; no admitted actual committed/context/result source created.',comparisons=out),indent=2))
```

## projection_probe: observed output

```json
{
  "note": "Scratch payment and synthetic world factory comparison only; no admitted actual committed/context/result source created.",
  "comparisons": [
    {
      "owner": "axis",
      "differenceCount": 0,
      "differences": []
    },
    {
      "owner": "commonwealth",
      "differenceCount": 0,
      "differences": []
    }
  ]
}
```

## maintenance_probe: retained source

```python
import pathlib,json,re,hashlib,subprocess
root=pathlib.Path('/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable')
report=(root/'docs/research/combat-verification-pin-maintenance.md').read_text()
manifest=[]
for line in report.splitlines():
 if re.match(r'\| \d+ \| `docs/',line):manifest.append(line.split('`')[1])
assert len(manifest)==20,len(manifest)
tracked=subprocess.check_output(['git','ls-files','docs/specs'],cwd=root,text=True).splitlines();files={p:(root/p).read_bytes() for p in tracked};hashes={p:hashlib.sha256(v).hexdigest() for p,v in files.items()}
changes=set(manifest);edges=[]
# Retained complete earlier20 closure is seed. Find new live digest holders in tracked specs.
while True:
 added=set()
 for target in changes:
  digest=hashes[target]
  for holder,data in files.items():
   if holder==target:continue
   if digest.encode() in data:
    edges.append((target,holder))
    added.add(holder)
 new=added-changes
 if not new:break
 changes|=new
out=dict(method='Start accepted historical20 path closure; recursive current digest-holder scan of all tracked docs/specs bytes. This identifies additive live successors; inherited generator relationships are retained from original report, not newly executed regeneration.',historicalPathCount=len(manifest),currentPathCount=len(changes),additivePaths=sorted(changes-set(manifest)),newEdges=[dict(target=t,holder=h) for t,h in sorted(set(edges)) if h not in manifest],maintenanceHashes=[dict(path=p,sha256=hashes[p]) for p in sorted(changes)])
print(json.dumps(out,indent=2))
```

## maintenance_probe: observed output

```json
{
  "method": "Start accepted historical20 path closure; recursive current digest-holder scan of all tracked docs/specs bytes. This identifies additive live successors; inherited generator relationships are retained from original report, not newly executed regeneration.",
  "historicalPathCount": 20,
  "currentPathCount": 22,
  "additivePaths": [
    "docs/specs/combat-actual-round-entry-v1.schema.json",
    "docs/specs/fixtures/combat-actual-round-entry-v1.json"
  ],
  "newEdges": [
    {
      "target": "docs/specs/fixtures/combat-cycle-sequence-v1.json",
      "holder": "docs/specs/combat-actual-round-entry-v1.schema.json"
    },
    {
      "target": "docs/specs/fixtures/combat-cycle-sequence-v1.json",
      "holder": "docs/specs/fixtures/combat-actual-round-entry-v1.json"
    }
  ],
  "maintenanceHashes": [
    {
      "path": "docs/specs/combat-actual-round-entry-v1.schema.json",
      "sha256": "677b1c671bebe5f23cf4b80f03078c55000c60e45416701757e2552cbd05edb8"
    },
    {
      "path": "docs/specs/combat-exercise-child-evidence-v1.schema.json",
      "sha256": "cc8a7c9e89068b9a78f3abb4044230ccc9ef359665198aaa7ef6db5b15782bf6"
    },
    {
      "path": "docs/specs/combat-exercise-parent-evidence-v1.schema.json",
      "sha256": "00d1fcd2cb7d06b6f25d9224ff0839308f23fd2052b42dab24c98ea378b30820"
    },
    {
      "path": "docs/specs/combat-outward-composition-v1.schema.json",
      "sha256": "6e8ec1103315238c0c5520c6cff9b31b20d6de2386e197944265a48168021344"
    },
    {
      "path": "docs/specs/fixtures/combat-actual-round-entry-v1.json",
      "sha256": "fc17cda07618bdd539a56b8c296037bbf36f37c6eb2ce6258c2b789b765f640b"
    },
    {
      "path": "docs/specs/fixtures/combat-authority-composition-v1.json",
      "sha256": "a90f88de9c05b4b1bd7081e7828abb7553e3e0537a6efa8cad042c87a6df50b1"
    },
    {
      "path": "docs/specs/fixtures/combat-cycle-sequence-v1.json",
      "sha256": "de89ebcd86f171119d206d0a90553cdeb815e7b8b6fc8903c9dfcacbd1db5b3f"
    },
    {
      "path": "docs/specs/fixtures/combat-exercise-child-evidence-v1.json",
      "sha256": "5587cb17e53c48a9ab393532ebaa186f3d1384632eb13b105e1af8a698e799b6"
    },
    {
      "path": "docs/specs/fixtures/combat-exercise-occurrence-v1.json",
      "sha256": "aabc20a39229ac121703a6cda077c0ebeffbfdd7bc3142a3d0c1d0805af7a38e"
    },
    {
      "path": "docs/specs/fixtures/combat-exercise-parent-evidence-v1.json",
      "sha256": "08316dae3d4f12298c35bfe05f640dcd2559ae41cbf4144430cd52b121847a9f"
    },
    {
      "path": "docs/specs/fixtures/combat-inherited-armed-continuation-v1.json",
      "sha256": "d86603ff7f07f2e0a3624e8748fedcf4406e2d873360f4e56a30e5952c645b1f"
    },
    {
      "path": "docs/specs/fixtures/combat-inherited-breakdown-completion-v1.json",
      "sha256": "7d6ee76d454bb047e03d104310758224d9470a0e9aabd30ce841abb32ffc54d1"
    },
    {
      "path": "docs/specs/fixtures/combat-inherited-cycle-control-v1.json",
      "sha256": "e2f2c76539a3509310fa2f1822fd1d84e1346f62cfc274ce12682866d02ba236"
    },
    {
      "path": "docs/specs/fixtures/combat-inherited-no-attack-v1.json",
      "sha256": "4e8f8a9fa0d1e973d6a5d7046b3e013f4270bd31c53afa24ed7595f732be6bc1"
    },
    {
      "path": "docs/specs/fixtures/combat-inherited-reserve-movement-completion-v1.json",
      "sha256": "e336d9b5afe498b3886318509958cb2e50ae860676ee1c4603c55540a41847d3"
    },
    {
      "path": "docs/specs/fixtures/combat-inherited-reserve-movement-v1.json",
      "sha256": "4feb5dc5e178d9242b8a6ef3b42f8101b3810e60bfe722506fa8838356d6567f"
    },
    {
      "path": "docs/specs/fixtures/combat-inherited-selection-v1.json",
      "sha256": "76240f2409f341757deea0f6ab5d346699de5a4a33690f95ccb7e74e160c1f4f"
    },
    {
      "path": "docs/specs/fixtures/combat-inherited-snapshot-v1.json",
      "sha256": "9dab9b8f72c3643fd50c62fd6e97335e5bc719ad1f61f4a15a910597224c862f"
    },
    {
      "path": "docs/specs/fixtures/combat-outward-composition-v1.json",
      "sha256": "28e37f48c80ed6ca6b7f7fabb4fb72b5d3433305ad662e091e2625a017bc1c24"
    },
    {
      "path": "docs/specs/fixtures/combat-side-projection-v1.json",
      "sha256": "07139da2ff125345c3007aa6bd9a5760eec44e4d4f25d0ec85c7e06fed4da44f"
    },
    {
      "path": "docs/specs/verify-combat-exercise-occurrence-v1.py",
      "sha256": "9e46db1943514629754cdc227fdc1a415c53c8b5e573b8fb0afd89c870f1fb53"
    },
    {
      "path": "docs/specs/verify-combat-side-projection-v1.py",
      "sha256": "964f4b6a6901e446e5d0e0a6c49e79e778677ae458947681034a4ddd5d848a0d"
    }
  ]
}
```

## oracles: retained source

```python
import pathlib,subprocess,time,json,concurrent.futures,hashlib
root=pathlib.Path('/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable')
out=pathlib.Path('/private/tmp/actual-combat-result-gap-evidence')
names=['inherited-breakdown-completion','cycle-sequence','inherited-snapshot','outward-composition']
def run(name):
 path=f'docs/specs/verify-combat-{name}-v1.py';cmd=['python3','-B',path];t=time.monotonic()
 try:
  r=subprocess.run(cmd,cwd=root,capture_output=True,timeout=45);data=r.stdout+r.stderr;code=r.returncode
 except subprocess.TimeoutExpired as e:data=(e.stdout or b'')+(e.stderr or b'');code='TIMEOUT45'
 (out/(name+'.log')).write_bytes(data)
 return dict(command=' '.join(cmd),exit=code,seconds=time.monotonic()-t,log=str(out/(name+'.log')),sha256=hashlib.sha256(data).hexdigest(),lastLines=data.decode().splitlines()[-4:])
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:results=list(pool.map(run,names))
print(json.dumps(results,indent=2))
```

## oracles: observed output

```json
[
  {
    "command": "python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py",
    "exit": 1,
    "seconds": 1.1534465000004275,
    "log": "/private/tmp/actual-combat-result-gap-evidence/inherited-breakdown-completion.log",
    "sha256": "b74c24e87d0c1d878c6d04453e2a4abed764bc3175d6a0599f00184288fb6e2c",
    "lastLines": [
      "  File \"/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-inherited-breakdown-completion-v1.py\", line 290, in check_fixture",
      "    assert sha((ROOT.parent.parent/pin['path']).read_bytes())==pin['sha256'],'source drift: '+pin['path']",
      "           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^",
      "AssertionError: source drift: src/Cna.Core/Rules/Cna1979LandSequence.cs"
    ]
  },
  {
    "command": "python3 -B docs/specs/verify-combat-cycle-sequence-v1.py",
    "exit": 1,
    "seconds": 0.1279661250009667,
    "log": "/private/tmp/actual-combat-result-gap-evidence/cycle-sequence.log",
    "sha256": "17ecde8583d089b5c161c8c68853473cd0d687808a189e44fb435d62a6fc5916",
    "lastLines": [
      "  File \"/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-cycle-sequence-v1.py\", line 233, in main",
      "    for path,h in f['sourceHashes'].items():assert sha((REPO/path).read_bytes())==h,path",
      "                                                   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^",
      "AssertionError: src/Cna.Core/Rules/Cna1979LandSequence.cs"
    ]
  },
  {
    "command": "python3 -B docs/specs/verify-combat-inherited-snapshot-v1.py",
    "exit": 1,
    "seconds": 4.368848375001107,
    "log": "/private/tmp/actual-combat-result-gap-evidence/inherited-snapshot.log",
    "sha256": "d956367025f5223dd6f0049a95f5fe7d4ed498ac9998a388b55d1845e825fb2a",
    "lastLines": [
      "    ~~~~~~~^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^",
      "  File \"/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-inherited-snapshot-v1.py\", line 58, in require",
      "    raise Invalid(message)",
      "Invalid: declared source pin: src/Cna.Core/Rules/Cna1979LandSequence.cs"
    ]
  },
  {
    "command": "python3 -B docs/specs/verify-combat-outward-composition-v1.py",
    "exit": 1,
    "seconds": 13.877353625008254,
    "log": "/private/tmp/actual-combat-result-gap-evidence/outward-composition.log",
    "sha256": "d4941cec6bb37b3ac37a50a41cfa237ac51256945ed7cf6059af42f49fb72c9a",
    "lastLines": [
      "  File \"/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-outward-composition-v1.py\", line 18, in require",
      "    if not value:raise Invalid('CMB-OUTWARD-COMPOSITION-REJECTED')",
      "                 ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^",
      "Invalid: CMB-OUTWARD-COMPOSITION-REJECTED"
    ]
  }
]
```

## Original inherited-breakdown-completion failure log

```text
Traceback (most recent call last):
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-inherited-breakdown-completion-v1.py", line 301, in <module>
    if __name__=='__main__': main()
                             ~~~~^^
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-inherited-breakdown-completion-v1.py", line 293, in main
    fixture=json.loads(FIXTURE.read_text()); check_fixture(fixture); semantic_red()
                                             ~~~~~~~~~~~~~^^^^^^^^^
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-inherited-breakdown-completion-v1.py", line 290, in check_fixture
    assert sha((ROOT.parent.parent/pin['path']).read_bytes())==pin['sha256'],'source drift: '+pin['path']
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
AssertionError: source drift: src/Cna.Core/Rules/Cna1979LandSequence.cs
```

## Original cycle-sequence failure log

```text
Traceback (most recent call last):
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-cycle-sequence-v1.py", line 316, in <module>
    if __name__=='__main__':main()
                            ~~~~^^
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-cycle-sequence-v1.py", line 233, in main
    for path,h in f['sourceHashes'].items():assert sha((REPO/path).read_bytes())==h,path
                                                   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
AssertionError: src/Cna.Core/Rules/Cna1979LandSequence.cs
```

## Original inherited-snapshot failure log

```text
H0 import inherited-reaction-movement-completion
H0 import inherited-movement-lifecycle
H0 import inherited-breakdown-completion
H0 import inherited-reaction-closure
H0 import inherited-reaction-active-fallback
Traceback (most recent call last):
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-inherited-snapshot-v1.py", line 698, in <module>
    main()
    ~~~~^^
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-inherited-snapshot-v1.py", line 683, in main
    require(frozen['sourcePins'] == source_pins(), 'source pins changed')
                                    ~~~~~~~~~~~^^
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-inherited-snapshot-v1.py", line 420, in source_pins
    require(sha(target.read_bytes()) == digest, 'declared source pin: ' + name)
    ~~~~~~~^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-inherited-snapshot-v1.py", line 58, in require
    raise Invalid(message)
Invalid: declared source pin: src/Cna.Core/Rules/Cna1979LandSequence.cs
```

## Original outward-composition failure log

```text
test_requirements {'requirements': 72, 'mutationRejects': 7} 0.232
Traceback (most recent call last):
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-outward-composition-v1.py", line 284, in <module>
    if __name__=='__main__':main(generate='--write-fixture' in sys.argv)
                            ~~~~^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-outward-composition-v1.py", line 276, in main
    for test in TESTS:print(test.__name__,test(),round(time.monotonic()-started,3),flush=True)
                                          ~~~~^^
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-outward-composition-v1.py", line 196, in test_integrated_readback
    value=build_index();data=raw(value,'Index');require(read_index(data)==value)
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-outward-composition-v1.py", line 177, in build_index
    pins()
    ~~~~^^
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-outward-composition-v1.py", line 49, in pins
    require(all(c.sha((REPO/path).read_bytes())==expected for path,expected in INVENTORY['sourcePins'].items()));p.pins()
    ~~~~~~~^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable/docs/specs/verify-combat-outward-composition-v1.py", line 18, in require
    if not value:raise Invalid('CMB-OUTWARD-COMPOSITION-REJECTED')
                 ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Invalid: CMB-OUTWARD-COMPOSITION-REJECTED
```
