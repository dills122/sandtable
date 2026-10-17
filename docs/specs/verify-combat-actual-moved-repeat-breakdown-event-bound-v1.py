#!/usr/bin/env python3
"""Provisional source-size model; its cardinality assumptions require review.

This does not certify native memory, latency, ancestry, or closureFrozen. Every
array reachable from a moved Assault Event must have an explicit bound below.
"""

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent

# Source: CampaignCombatActualMovedAssault SelectionTransition, RoundTransition,
# PaidTransition; the frozen two-element/one-component content pack; and the
# moved Assault schema's draws, consumed bytes, and route limits. A reviewer
# must verify each count against every accepted transition branch.
ARRAY_COUNTS = {
    "Participant.componentIds": 1,
    "RoundOpen.slots": 2,
    "RoundStep.proofReceipts": 2,
    "CloseEffect.proofReceipts": 5,
    "CommitEffect.allocations": 2,
    "CommitEffect.costs": 2,
    "AssaultResult.draws": 9,
    "Draw.consumed": 512,
    "Losses.roles": 2,
}

SAFE_STRING_LENGTHS = {
    "id": 128, "hash": 71, "rawHash": 64, "actor": 12,
    "side": 12, "role": 12,
}
NUMERIC_TYPES = {"int", "long", "ulong", "utc", "signedInt", "adjustment", "differential"}


def encoded(value):
    return json.dumps(value, separators=(",", ":"), ensure_ascii=True).encode("ascii")


def largest(values):
    return max(values, key=lambda value: len(encoded(value)))


def maximal_value(type_name, objects, effect_tags, parent="", field=""):
    optional = type_name.endswith("?")
    if optional:
        type_name = type_name[:-1]
    if type_name.endswith("[]"):
        path = parent + "." + field
        if path not in ARRAY_COUNTS:
            raise ValueError("missing reviewed array cardinality: " + path)
        item = maximal_value(type_name[:-2], objects, effect_tags)
        return [item for _ in range(ARRAY_COUNTS[path])]
    if type_name in effect_tags:
        return largest(maximal_value(name, objects, effect_tags) for name in effect_tags[type_name].values())
    if type_name == "Route":
        return ["\u0000" * SAFE_STRING_LENGTHS["id"] for _ in range(9)]
    if type_name in objects:
        result = {}
        for part in objects[type_name].split():
            name, member_type = part.split(":", 1)
            result[name] = maximal_value(member_type, objects, effect_tags, type_name, name)
        return result
    if type_name in SAFE_STRING_LENGTHS:
        # The inherited codec can escape every UTF-16 code unit as six bytes.
        # This safely overbounds even IDs and hashes pending leaf-grammar audit.
        return "\u0000" * SAFE_STRING_LENGTHS[type_name]
    if type_name == "string":
        # The codec counts Unicode scalar values. A supplementary scalar is two
        # UTF-16 code units, each emitted as a six-byte JSON escape.
        return "\U0001f600" * 256
    if type_name in NUMERIC_TYPES:
        return -(2**63)
    if type_name == "byte":
        return 255
    if type_name == "bool":
        return False
    if type_name == "null":
        return None
    raise ValueError("unreviewed member type: " + type_name)


def calculate(moved_schema):
    objects = moved_schema["objects"]
    effect_tags = moved_schema["effectTags"]
    event_bounds = {
        name: len(encoded(maximal_value(name, objects, effect_tags)))
        for name in ("SelectionEvent", "RoundEvent", "PaidEvent")
    }
    limits = moved_schema["limits"]
    event_count = sum(limits[name] for name in ("selectionEvents", "roundEvents", "paidEvents"))
    root_cap = limits["parentSourceBytes"]
    generic = 2 * root_cap + 2 * event_count * limits["eventBytes"] + 512
    if generic != moved_schema["encodedUpperBounds"]["PaidSource"]:
        raise ValueError("moved Assault structural recurrence changed")
    current = 2 * root_cap + 2 * event_count * max(event_bounds.values()) + 512
    predecessor_model = generic
    successor_names = (
        "combat-actual-moved-continuation-v1",
        "combat-actual-moved-cycle-control-v1",
        "combat-actual-moved-repeat-movement-v1",
        "combat-actual-moved-repeat-movement-completion-v1",
        "combat-actual-moved-repeat-breakdown-completion-v1",
    )
    for name in successor_names:
        stage = json.loads((ROOT / (name + ".schema.json")).read_bytes())
        model = stage["encodedUpperBounds"]["Source"]
        addition = model - 2 * predecessor_model
        if addition < 0:
            raise ValueError("successor structural recurrence changed: " + name)
        current = 2 * current + addition
        predecessor_model = model
    return {"eventBounds": event_bounds, "breakdownSourceBound": current}


def main():
    schema = json.loads((ROOT / "combat-actual-moved-assault-v1.schema.json").read_bytes())
    result = calculate(schema)
    cap = json.loads((ROOT / "combat-actual-moved-repeat-breakdown-completion-v1.schema.json").read_bytes())["limits"]["sourceBytes"]
    if result["breakdownSourceBound"] > cap:
        raise ValueError("provisional child Source bound exceeds frozen cap")
    print(json.dumps({**result, "frozenSourceCap": cap, "status": "provisional cardinality audit required"}, sort_keys=True))


if __name__ == "__main__":
    main()
