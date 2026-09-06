#!/usr/bin/env python3
"""Executable Content 7 contract oracle; not a production Content reader."""
import copy
import hashlib
import json
import re
from collections import deque
from pathlib import Path

ROOT = Path(__file__).resolve().parent
ID = r"[a-z0-9]+(?:[-.][a-z0-9]+)*"
LOCATOR = r"[A-Za-z0-9][A-Za-z0-9._:-]*"
CAPABILITIES = ["land.close-assault-inputs", "land.combat-components", "land.element-mobility",
                "land.formations", "land.hex-topology", "land.initial-deployment", "land.weather-areas"]
# Object declarations also define canonical property order. [] means an empty array.
SCHEMA = {
    "Root": "schemaVersion:int formatId:id packId:id rulesetId:id capabilities:id[] capabilityProfileId:id sourceIndex:Source[] locations:Location[] weatherAreaAssignments:Weather[] edges:Edge[] formations:Formation[] elements:Element[] scenarios:Scenario[]",
    "Source": "sourceId:id kind:string",
    "Reference": "sourceId:id locator:locator",
    "Origin": "kind:string references:Reference[]",
    "Location": "locationId:id kind:string terrainId:id sourceCoordinate:null origin:Origin",
    "Weather": "locationId:id weatherArea:string origin:Origin",
    "Edge": "firstLocationId:id secondLocationId:id features:[] origin:Origin",
    "Formation": "formationId:id sideId:id parentFormationId:null organizationId:id basicMorale:int basicMoraleOrigin:Origin origin:Origin",
    "Component": "componentId:id componentClassId:id maximumToe:int offensiveCloseAssaultRating:int defensiveCloseAssaultRating:int origin:Origin",
    "Element": "elementId:id sideId:id parentFormationId:id organizationId:id mobilityId:id baseCapabilityPointAllowance:int placementMode:string combatClassificationId:id combatOrigin:Origin components:Component[] breakdownVehicleCohort:null origin:Origin",
    "Boundary": "gameTurn:int operationStage:int",
    "ToeSeed": "componentId:id currentToe:int origin:Origin",
    "AmmoSeed": "points:int origin:Origin",
    "ReadinessSeed": "gameTurn:int operationStage:int waterStatus:string storesStatus:string pinned:bool origin:Origin",
    "Placement": "elementId:id locationId:id initialComponentToes:ToeSeed[] initialAmmunition:AmmoSeed initialReadiness:ReadinessSeed origin:Origin",
    "RetreatAnchor": "sideId:id locationId:id kind:string origin:Origin",
    "Scenario": "scenarioId:id start:Boundary end:Boundary initialPlacements:Placement[] retreatSupplyAnchors:RetreatAnchor[] origin:Origin",
}
SCHEMA = {k: [tuple(f.split(":")) for f in v.split()] for k, v in SCHEMA.items()}
KEYS = {"Source": ("sourceId",), "Reference": ("sourceId", "locator"),
        "Location": ("locationId",), "Weather": ("locationId",),
        "Edge": ("firstLocationId", "secondLocationId"), "Formation": ("formationId",),
        "Element": ("elementId",), "Component": ("componentId",), "Scenario": ("scenarioId",),
        "Placement": ("elementId", "locationId"), "ToeSeed": ("componentId",),
        "RetreatAnchor": ("sideId",)}
RANGES = {"maximumToe": (1, 2147483647), "baseCapabilityPointAllowance": (1, 2147483647),
          "offensiveCloseAssaultRating": (0, 2147483647), "defensiveCloseAssaultRating": (0, 2147483647),
          "currentToe": (0, 2147483647), "points": (0, 2147483647), "basicMorale": (-3, 3),
          "gameTurn": (1, 111), "operationStage": (1, 3)}


class Invalid(ValueError):
    def __init__(self, code, path):
        self.code, self.path = f"CMB-CNT-{code:03}", path
        super().__init__(f"{self.code} {path or '/'}")


def require(condition, code, path):
    if not condition:
        raise Invalid(code, path)


def shape(value, kind="Root", path="", depth=0):
    require(depth <= 32, 1, path)
    if kind.endswith("[]"):
        require(type(value) is list, 1, path)
        if kind == "[]":
            require(not value, 4, path)
        else:
            for i, item in enumerate(value):
                shape(item, kind[:-2], f"{path}/{i}", depth + 1)
    elif kind in SCHEMA:
        require(type(value) is dict, 1, path)
        for key, child in SCHEMA[kind]:
            require(key in value, 1, f"{path}/{key}")
            shape(value[key], child, f"{path}/{key}", depth + 1)
        require(set(value) == {k for k, _ in SCHEMA[kind]}, 1, path)
    else:
        expected = {"int": int, "bool": bool, "null": type(None), "id": str,
                    "locator": str, "string": str}[kind]
        require(type(value) is expected, 1, path)


def walk(value, kind="Root", path=""):
    yield value, kind, path
    if kind in SCHEMA:
        for key, child in SCHEMA[kind]:
            yield from walk(value[key], child, f"{path}/{key}")
    elif kind.endswith("[]") and kind != "[]":
        for i, child in enumerate(value):
            yield from walk(child, kind[:-2], f"{path}/{i}")


def canonical(value, kind="Root"):
    if kind in SCHEMA:
        return {key: canonical(value[key], child) for key, child in SCHEMA[kind]}
    if kind.endswith("[]") and kind != "[]":
        child = kind[:-2]
        values = [canonical(v, child) for v in value]
        key = (lambda v: tuple(v[k] for k in KEYS[child])) if child in KEYS else None
        return sorted(values, key=key)
    return value


def encode(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("utf-8")


def unique(items, key, path):
    result = {}
    for i, item in enumerate(items):
        require(item[key] not in result, 5, f"{path}/{i}/{key}")
        result[item[key]] = item
    return result


def route(graph, start, end):
    pending = deque([[start]])
    visited = {start}
    while pending:
        path = pending.popleft()
        if path[-1] == end:
            return path
        for neighbor in sorted(graph[path[-1]] - visited):
            visited.add(neighbor)
            pending.append(path + [neighbor])
    return []


def geometry(doc):
    graph = {v["locationId"]: set() for v in doc["locations"]}
    for edge in doc["edges"]:
        a, b = edge["firstLocationId"], edge["secondLocationId"]
        graph[a].add(b)
        graph[b].add(a)
    require(sorted(map(len, graph.values())) == [1, 1, 2, 2, 2, 2], 4, "/edges")
    require(all(route(graph, next(iter(graph)), end) for end in graph), 4, "/edges")
    scenario = doc["scenarios"][0]
    sides = {e["elementId"]: e["sideId"] for e in doc["elements"]}
    initial = {sides[p["elementId"]]: p["locationId"] for p in scenario["initialPlacements"]}
    anchors = {a["sideId"]: a["locationId"] for a in scenario["retreatSupplyAnchors"]}
    for i, anchor in enumerate(scenario["retreatSupplyAnchors"]):
        side, node = anchor["sideId"], anchor["locationId"]
        require(len(graph[node]) == 1 and len(route(graph, initial[side], node)) == 3,
                4, f"/scenarios/0/retreatSupplyAnchors/{i}/locationId")
    require(len(route(graph, initial["axis"], initial["commonwealth"])) == 2,
            4, "/scenarios/0/initialPlacements")
    probes = 0
    for attacker, defender in [("axis", "commonwealth"), ("commonwealth", "axis")]:
        retreat = route(graph, initial[defender], anchors[defender])[1]
        require(len(route(graph, initial[attacker], retreat)) == 3, 4, "/edges")
        for retreats in [False, True]:
            post = dict(initial)
            if retreats:
                post[defender] = retreat
            for victim, captor in [(attacker, defender), (defender, attacker)]:
                relocation = route(graph, initial[victim], post[captor])
                escape = route(graph, initial[victim], post[victim])
                require(0 < len(relocation) <= 4 and post[victim] not in relocation[1:], 4, "/edges")
                require(0 < len(escape) <= 9, 4, "/edges")
                probes += 1
    return probes


def validate(doc):
    shape(doc)
    for key, expected in [("schemaVersion", 7), ("formatId", "sandtable.content-json.v6"),
                          ("rulesetId", "cna-1979.1"),
                          ("capabilityProfileId", "sandtable.capability.combat-cycle-infantry.v1")]:
        require(doc[key] == expected, 2, f"/{key}")
    require(sorted(doc["capabilities"]) == CAPABILITIES, 2, "/capabilities")
    nodes = list(walk(doc))
    for value, kind, path in nodes:
        if kind in ("id", "locator"):
            require(1 <= len(value) <= 128 and re.fullmatch(ID if kind == "id" else LOCATOR, value), 3, path)
        elif kind == "int":
            lo, hi = RANGES.get(path.rsplit("/", 1)[-1], (-2147483648, 2147483647))
            require(lo <= value <= hi, 3, path)
    sources = unique(doc["sourceIndex"], "sourceId", "/sourceIndex")
    require(list(sources) == ["sandtable-rules-lab"], 6, "/sourceIndex")
    require(doc["sourceIndex"][0]["kind"] == "repository-synthetic", 6, "/sourceIndex/0/kind")
    for value, kind, path in nodes:
        if kind == "Origin":
            require(value["kind"] == "synthetic", 6, path + "/kind")
            require(len(value["references"]) == 1, 6, path + "/references")
            require(value["references"][0]["sourceId"] in sources, 6, path + "/references/0/sourceId")
    locations = unique(doc["locations"], "locationId", "/locations")
    weather = unique(doc["weatherAreaAssignments"], "locationId", "/weatherAreaAssignments")
    require(weather.keys() == locations.keys(), 5, "/weatherAreaAssignments")
    edge_keys = set()
    for i, edge in enumerate(doc["edges"]):
        for key in ("firstLocationId", "secondLocationId"):
            require(edge[key] in locations, 5, f"/edges/{i}/{key}")
        pair = tuple(sorted([edge["firstLocationId"], edge["secondLocationId"]]))
        require(pair[0] != pair[1] and pair not in edge_keys, 5, f"/edges/{i}")
        edge_keys.add(pair)
        require(edge["firstLocationId"] < edge["secondLocationId"], 4, f"/edges/{i}")
    formations = unique(doc["formations"], "formationId", "/formations")
    elements = unique(doc["elements"], "elementId", "/elements")
    components = set()
    parents = set()
    for i, form in enumerate(doc["formations"]):
        require(form["sideId"] in ("axis", "commonwealth"), 5, f"/formations/{i}/sideId")
    for i, element in enumerate(doc["elements"]):
        parent = formations.get(element["parentFormationId"])
        require(parent is not None and parent["sideId"] == element["sideId"] and
                element["parentFormationId"] not in parents, 5, f"/elements/{i}/parentFormationId")
        parents.add(element["parentFormationId"])
        for j, comp in enumerate(element["components"]):
            require(comp["componentId"] not in components, 5, f"/elements/{i}/components/{j}/componentId")
            components.add(comp["componentId"])
    require(parents == formations.keys(), 5, "/formations")
    unique(doc["scenarios"], "scenarioId", "/scenarios")
    for i, scenario in enumerate(doc["scenarios"]):
        p = f"/scenarios/{i}"
        placements = unique(scenario["initialPlacements"], "elementId", p + "/initialPlacements")
        require(placements.keys() == elements.keys(), 5, p + "/initialPlacements")
        for j, placement in enumerate(scenario["initialPlacements"]):
            q = f"{p}/initialPlacements/{j}"
            require(placement["locationId"] in locations, 5, q + "/locationId")
            toes = unique(placement["initialComponentToes"], "componentId", q + "/initialComponentToes")
            owned = {c["componentId"] for c in elements[placement["elementId"]]["components"]}
            require(toes.keys() == owned, 5, q + "/initialComponentToes")
        anchors = unique(scenario["retreatSupplyAnchors"], "sideId", p + "/retreatSupplyAnchors")
        require(set(anchors) == {"axis", "commonwealth"}, 5, p + "/retreatSupplyAnchors")
        for j, anchor in enumerate(scenario["retreatSupplyAnchors"]):
            require(anchor["locationId"] in locations, 5, f"{p}/retreatSupplyAnchors/{j}/locationId")
    for key, count in [("locations", 6), ("edges", 5), ("formations", 2), ("elements", 2), ("scenarios", 1)]:
        require(len(doc[key]) == count, 4, f"/{key}")
    require(sorted(f["sideId"] for f in doc["formations"]) == ["axis", "commonwealth"], 4, "/formations")
    exact = {"Location": {"kind": "hex", "terrainId": "land.terrain.clear"}, "Weather": {"weatherArea": "a"},
             "Formation": {"organizationId": "land.organization.battalion", "basicMorale": 0},
             "Component": {"componentClassId": "land.combat-component.infantry", "maximumToe": 10,
                           "offensiveCloseAssaultRating": 1, "defensiveCloseAssaultRating": 1},
             "Element": {"organizationId": "land.organization.battalion", "mobilityId": "land.mobility.non-motorized",
                         "baseCapabilityPointAllowance": 10, "placementMode": "independent",
                         "combatClassificationId": "land.combat-classification.combat-unit"},
             "RetreatAnchor": {"kind": "friendly-supply-direction"}}
    for value, kind, path in nodes:
        for key, expected in exact.get(kind, {}).items():
            require(value[key] == expected, 4, path + "/" + key)
        if kind == "Element":
            require(len(value["components"]) == 1, 4, path + "/components")
    scenario = doc["scenarios"][0]
    require(scenario["start"] == scenario["end"], 7, "/scenarios/0/end")
    for i, placement in enumerate(scenario["initialPlacements"]):
        p = f"/scenarios/0/initialPlacements/{i}"
        require(placement["initialComponentToes"][0]["currentToe"] == 10, 7, p + "/initialComponentToes/0/currentToe")
        require(placement["initialAmmunition"]["points"] == 10, 7, p + "/initialAmmunition/points")
        readiness = placement["initialReadiness"]
        for key in ("gameTurn", "operationStage"):
            require(readiness[key] == scenario["start"][key], 7, p + "/initialReadiness/" + key)
        for key in ("waterStatus", "storesStatus"):
            require(readiness[key] == "distributed-for-stage", 7, p + "/initialReadiness/" + key)
        require(not readiness["pinned"], 7, p + "/initialReadiness/pinned")
    return geometry(doc)


def pairs(items):
    result = {}
    for key, value in items:
        require(key not in result, 1, "")  # Decoder failures use document root.
        result[key] = value
    return result


def read(raw):
    require(len(raw) <= 65536 and not raw.startswith(b"\xef\xbb\xbf"), 1, "")
    try:
        doc = json.loads(raw.decode("utf-8"), object_pairs_hook=pairs,
                         parse_constant=lambda _: (_ for _ in ()).throw(Invalid(1, "")))
    except (UnicodeError, ValueError, RecursionError) as error:
        if isinstance(error, Invalid):
            raise
        raise Invalid(1, "") from error
    validate(doc)
    require(encode(canonical(doc)) == raw, 8, "")
    return doc


def mutate(doc, vector):
    result = copy.deepcopy(doc)
    parts = vector["path"].strip("/").split("/")
    parent = result
    for key in parts[:-1]:
        parent = parent[int(key)] if isinstance(parent, list) else parent[key]
    key = int(parts[-1]) if isinstance(parent, list) else parts[-1]
    if vector["op"] == "remove":
        del parent[key]
    else:
        parent[key] = vector["value"]
    return encode(result)


def main():
    raw = (ROOT / "fixtures/combat-content-v7.canonical.json").read_bytes()
    vectors = json.loads((ROOT / "fixtures/combat-content-v7.vectors.json").read_text())
    doc = read(raw)
    digest = "sha256:" + hashlib.sha256(raw).hexdigest()
    assert len(raw) == vectors["canonicalByteCount"] and digest == vectors["canonicalHash"]
    for vector in vectors["negativeVectors"]:
        if vector["op"] == "raw-replace":
            old = vector["old"].encode()
            assert raw.count(old) == 1, vector["name"]
            candidate = raw.replace(old, vector["new"].encode(), 1)
        elif vector["op"] == "append":
            candidate = raw + vector["value"].encode()
        elif vector["op"] == "prepend":
            candidate = vector["value"].encode() + raw
        else:
            candidate = mutate(doc, vector)
        try:
            read(candidate)
        except Invalid as error:
            assert (error.code, error.path) == (vector["code"], vector["errorPath"]), (vector["name"], str(error))
        else:
            raise AssertionError(f"Accepted negative vector: {vector['name']}")
    # Construction order cannot change canonical identity. Reader still rejects unsorted bytes.
    def reverse(value):
        if isinstance(value, dict):
            return {k: reverse(v) for k, v in reversed(list(value.items()))}
        if isinstance(value, list):
            return [reverse(v) for v in reversed(value)]
        return value
    shuffled = reverse(doc)
    validate(shuffled)
    assert encode(canonical(shuffled)) == raw
    changed = copy.deepcopy(doc)
    changed["formations"][0]["basicMoraleOrigin"]["references"][0]["locator"] += ".v2"
    changed_raw = encode(canonical(changed))
    read(changed_raw)
    assert hashlib.sha256(changed_raw).hexdigest() != digest.removeprefix("sha256:")
    print(f"PASS: {len(raw)} canonical bytes; {digest}; {len(vectors['negativeVectors'])} rejection vectors; "
          f"{geometry(doc)} custody/escape probes across both retreat directions; shuffled construction and provenance hash sensitivity.")


if __name__ == "__main__":
    main()
