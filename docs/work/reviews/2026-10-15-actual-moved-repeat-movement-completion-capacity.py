#!/usr/bin/env python3
"""R02 encoding arithmetic only; constructed vectors are never authenticated roots."""
import json
from pathlib import Path
R=Path(__file__).resolve().parents[3];schema=json.loads((R/'docs/specs/combat-actual-moved-repeat-movement-completion-v1.schema.json').read_bytes());old=json.loads((R/'docs/specs/combat-actual-moved-repeat-movement-v1.schema.json').read_bytes())
def encode(x):return json.dumps(x,ensure_ascii=True,separators=(',',':')).encode()
envelope=len(encode(dict(contractVersion=1,movementSourceCanonicalUtf8='',lifecycleEventCanonicalUtf8=['']*3)));assert envelope<=512
source=2*old['encodedUpperBounds']['Source']+3*2*65536+512;assert source==128857600
# Six encoded bytes per128-character identifier, plus quotes. Side and hashes have fixed smaller syntax.
id_bytes=128*6+2;unit=2*id_bytes+14+80;track=unit+6*id_bytes+128;stop=track+2*id_bytes+19+10+8+256;flow=stop+128;authority=16*id_bytes+512
catalog=json.loads(json.loads((R/'docs/specs/fixtures/combat-cycle-sequence-v1.json').read_bytes())['predecessor']['canonicalUtf8']);selected=[p for p in catalog['positions']+catalog['interruptPositions'] if p['positionId'] in ['land.position.breakdown-stop','land.position.operation-1.first-player.movement-and-combat.movement','land.position.operation-1.first-player.movement-and-combat.breakdown-determination']];assert len(selected)==3
for p in selected:p['contractVersion']=5
position=max(map(lambda p:len(encode(p)),selected));assert position<=1024
context=authority+1024+71+256;endproof=1024+2*(unit+id_bytes+128)+2*unit+id_bytes+19+256
assert track<=8192 and flow<=16384 and authority<=16384 and context<=24576 and endproof<=16384
receipts=6*512;headers=4096;allocated=16384+24576+16384+receipts+headers;assert allocated==64512<=65536
control=old['encodedUpperBounds']['Control']+65536;proof=control+4096+4096;event=16384+24576+1024+6144+4096+256
assert control==786432 and proof==794624 and event<=65536
model=dict(Source=source,Control=control,Proof=proof,World=old['encodedUpperBounds']['World'],Event=65536,Input=4096)
assert model==schema['encodedUpperBounds'];assert source<schema['limits']['sourceBytes'] and control<schema['limits']['controlBytes'] and proof<schema['limits']['proofBytes']
print(json.dumps(dict(scope='reachable profile arithmetic; arbitrary framing not authority',model=model,sourceEnvelope=envelope,components=dict(unit=unit,track=track,flow=flow,authority=authority,position=position,context=context,newEndProof=endproof,receiptCopies=receipts,headerAllowance=headers,allocatedMetadata=allocated,event=event)),indent=2))
