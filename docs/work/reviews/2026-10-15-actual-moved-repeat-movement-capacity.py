#!/usr/bin/env python3
"""Arithmetic envelope checks; extreme strings are framing, never admitted authority."""
import json
from pathlib import Path
root=Path(__file__).resolve().parents[3]/'docs/specs'
schema=json.loads((root/'combat-actual-moved-repeat-movement-v1.schema.json').read_bytes())
parent=json.loads((root/'combat-actual-moved-cycle-control-v1.schema.json').read_bytes())
def wire(v):return json.dumps(v,ensure_ascii=True,separators=(',',':')).encode('ascii')
# Canonical ASCII JSON payloads contain no raw controls. Each embedded byte is
# unchanged or escaped as two bytes (quote/backslash). Controls inside values
# have already become printable escape sequences in the payload.
source_overhead=len(wire(dict(contractVersion=1,controlSourceCanonicalUtf8='',movementEventCanonicalUtf8=['']*6)))
assert source_overhead==102
source=2*parent['encodedUpperBounds']['Source']+6*2*schema['limits']['eventBytes']+512
assert source==schema['encodedUpperBounds']['Source']==64231936
# Worst permitted ASCII string encoding is six bytes per character. IDs used
# below deliberately overapproximate syntax, while fixed hashes cannot escape.
cause=dict(causeId='mrm.'+'f'*64+'.dp',ordinal=512,receiptId='mrm.'+'f'*64,elementId='\x00'*128,gameTurn=2147483647,operationStage=2147483647,kind='ordinary-movement-excess-cp-dp',points=2147483647,before=-2147483648,after=-2147483648)
cause_bytes=len(wire(cause));assert cause_bytes+1<=1536 # comma for append
# At most5 appends. Final replacement sizes, not cumulative moves, determine
# root growth. Two currentLocationId strings, CP numerator and cohesion only.
world_delta=5*1536+2*128*6+32+128
assert world_delta==9376 and world_delta<=10240
world=524288+10240
assert world==schema['encodedUpperBounds']['World']==534528<schema['limits']['worldBytes']
# Relationship ending: null->fixed receipt, null->fixed label, true->false.
ending_growth=len(wire('mrm.'+'f'*64))+len(wire('ordinary-break-off'))-2*len(b'null')+1
assert ending_growth<=128
print(json.dumps(dict(status='pass',sourceParent=parent['encodedUpperBounds']['Source'],sourceEvents=6,sourceEventBytes=schema['limits']['eventBytes'],sourceEnvelope=source_overhead,sourceEnvelopeReserve=512,sourceUpper=source,causeSerialization=cause_bytes,causeWithCommaReserve=1536,worldCauseReserve=7680,worldLocationsReserve=1536,worldNumericReserve=32,worldRelationshipReserve=128,worldEndingGrowth=ending_growth,worldGrowth=world_delta,worldGrowthReserve=10240,worldUpper=world)))
