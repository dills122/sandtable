#!/usr/bin/env python3
"""R03 structural encoding arithmetic; authentic replay is a separate gate."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[3] / "docs/specs"
parent = json.loads((root / "combat-actual-moved-repeat-movement-completion-v1.schema.json").read_bytes())
current = json.loads((root / "combat-actual-moved-repeat-breakdown-completion-v1.schema.json").read_bytes())
p = parent["encodedUpperBounds"]
l = current["limits"]

# A JSON string can double every ASCII byte through escaping. The fixed Source
# envelope, one event, and one ledger slot cannot exceed these allocations.
source = 2 * p["Source"] + 2 * l["eventBytes"] + 512
assert source == 257_846_784 < l["sourceBytes"]
assert p["Source"] < l["parentSourceBytes"]

# R03 Control adds two preserved R02 hashes, one cumulative receipt copy,
# one local receipt copy, and one completion ID.
# Its Base embeds the full R02 Proof; the final Proof also embeds R03 Control.
control = p["Control"] + 4_096
proof = p["Proof"] + control + 4_096
assert control == 790_528 < l["controlBytes"]
assert proof == 1_589_248 < l["proofBytes"]

# Maximum escaped 128-character ID is 770 encoded bytes. Four source
# references, one position, command, and fixed event metadata fit this cap.
escaped_id = 6 * 128 + 2
reference = 2 * escaped_id + 64
event = 4 * reference + 4_096 + 1_024 + 8_192
assert event == 19_728 < l["eventBytes"]
assert l["depth"] >= parent["limits"]["depth"] + 2
assert l["arrayItems"] >= parent["limits"]["arrays"]
assert l["objectFields"] >= parent["limits"]["objectFields"]

print(json.dumps(dict(scope="structural encoding only; physical delivery and authentic rows separate",
                      source=source, control=control, proof=proof, event=event,
                      parentSource=p["Source"], depth=l["depth"]), indent=2))
