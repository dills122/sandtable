#!/usr/bin/env python3
"""Creation-rooted inherited Reaction-trigger contract oracle; no runtime admission."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent

def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

imv = load("inherited_movement", "verify-combat-inherited-movement-v1.py")
world, encode, sha = imv.world, imv.encode, imv.sha
INVENTORY = json.loads((ROOT / "combat-inherited-reaction-trigger-v1.schema.json").read_text())
FIXTURE = ROOT / "fixtures/combat-inherited-reaction-trigger-v1.json"
LOCAL = {k: [tuple(field.split(":")) for field in value.split()]
    for k, value in INVENTORY["objects"].items()}
SCHEMA = imv.SCHEMA | LOCAL
SOURCE_PATHS = {
    "docs/specs/combat-inherited-reaction-trigger-v1.schema.json",
    "docs/specs/combat-inherited-movement-v1.md",
    "docs/specs/combat-inherited-movement-v1.schema.json",
    "docs/specs/verify-combat-inherited-movement-v1.py",
    "docs/specs/fixtures/combat-inherited-movement-v1.json",
    "src/Cna.Core/Campaigns/CampaignElementMovedV3Factory.cs",
    "src/Cna.Core/Campaigns/CampaignReactionIdentity.cs",
    "src/Cna.Core/Campaigns/CampaignReactionWindow.cs",
    "src/Cna.Core/Campaigns/CampaignV11CanonicalCodec.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownCodec.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownMoveEventSerializer.cs",
}

class Invalid(ValueError):
    def __init__(self, code):
        self.code = f"CMB-IRT-{code:03}"
        super().__init__(self.code)

def require(ok, code):
    if not ok:
        raise Invalid(code)

def typed(value, kind, depth=0):
    require(depth <= INVENTORY["limits"]["depth"], 1)
    if kind == "null": require(value is None, 3)
    elif kind == "empty": require(type(value) is list and not value, 3)
    elif kind.endswith("?"):
        if value is not None: typed(value, kind[:-1], depth)
    elif kind in SCHEMA:
        require(type(value) is dict and list(value) == [key for key, _ in SCHEMA[kind]], 1)
        for key, child in SCHEMA[kind]: typed(value[key], child, depth + 1)
    elif kind.endswith("[]"):
        require(type(value) is list and len(value) <= INVENTORY["limits"]["arrayItems"], 1)
        for item in value: typed(item, kind[:-2], depth + 1)
    else:
        try: imv.typed(value, kind, depth)
        except imv.Invalid as error: raise Invalid(1) from error

def canonical(value, kind):
    if kind.endswith("?"): return None if value is None else canonical(value, kind[:-1])
    if kind in SCHEMA: return {key: canonical(value[key], child) for key, child in SCHEMA[kind]}
    if kind.endswith("[]"):
        child = kind[:-2]; result = [canonical(item, child) for item in value]
        if child in imv.rd.pre.env.KEYS:
            result.sort(key=lambda item: tuple(item[key] for key in imv.rd.pre.env.KEYS[child]))
        elif child == "FrozenReactionOpportunity": result.sort(key=lambda item: item["opportunityId"])
        elif kind in ("id[]", "hash[]"): result.sort()
        return result
    return value

def raw(value, kind):
    typed(value, kind); data = encode(canonical(value, kind))
    require(len(data) <= INVENTORY["limits"]["bytes"], 1)
    return data

def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= INVENTORY["limits"]["bytes"], 1)
    def pairs(items):
        result = {}
        for key, value in items:
            require(key not in result, 1); result[key] = value
        return result
    try:
        value = json.loads(data.decode("utf8"), object_pairs_hook=pairs,
            parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error: raise Invalid(1) from error
    require(raw(value, kind) == data, 8)
    return value

def opposite(side): return "commonwealth" if side == "axis" else "axis"

def profile(side):
    return dict(actor=side, elementId=f"{side}-assault-battalion",
        assault="assault-west" if side == "axis" else "assault-east", rear=f"{side}-rear",
        opponentElementId=f"{opposite(side)}-assault-battalion",
        opponentAssault="assault-east" if side == "axis" else "assault-west")

def initial(request, created, preamble, weather_events, stage_events, reserve_events, movement_events):
    require(type(movement_events) is list and len(movement_events) == 1, 3)
    try:
        state = imv.replay(request, created, preamble, weather_events, stage_events,
            reserve_events, movement_events)
        moved = imv.read_event(movement_events[0])
    except (ValueError, KeyError, TypeError) as error: raise Invalid(4) from error
    expected = profile(state["firstActingSide"])
    element = next(item for item in state["world"]["elements"] if item["elementId"] == expected["elementId"])
    require(state["stateVersion"] == 12 and moved["stateVersion"] == 12
        and moved["priorStateVersion"] == 11 and moved["actingSide"] == expected["actor"], 4)
    require((moved["originLocationId"], moved["destinationLocationId"])
        == (expected["assault"], expected["rear"]), 3)
    require(element["currentLocationId"] == expected["rear"]
        and element["operationalState"]["capabilityPointsExpended"] == world.cp(2)
        and element["operationalState"]["cohesionLevel"] == 0
        and element["operationalState"]["movementEnded"] is None, 3)
    require(state["tracks"] == [dict(unit=copy.deepcopy(state["members"][0]["unit"]),
        route=[expected["assault"], expected["rear"]])], 4)
    require(state["breakdownFlow"] == moved["breakdownFlowAfter"]
        and state["breakdownFlow"]["kind"] == "moving", 4)
    return state

def command(state): return imv.command(state, profile(state["firstActingSide"])["assault"])

def authorize(state, inp):
    try: imv.authorize(state, inp)
    except imv.Invalid as error: raise Invalid(5 if error.code.endswith("005") else 4) from error
    cmd, expected = inp["command"], profile(state["firstActingSide"])
    require(cmd["expectedPriorVersion"] == 12 and cmd["originLocationId"] == expected["rear"]
        and cmd["destinationLocationId"] == expected["assault"], 5)

def representation(value):
    return dict(representationId=value["representationId"], currentLocationId=value["currentLocationId"],
        bindingKind=value["bindingKind"], boundElementIds=sorted(value["boundElementIds"]))

def identity(domain, fields):
    value = {"domain": domain}; value.update(fields)
    return sha(encode(value))

def receipt(event):
    unsigned = {key: value for key, value in canonical(event, "TriggerEvent").items() if key != "receiptId"}
    digest = hashlib.sha256(INVENTORY["domains"]["receipt"].encode() + b"\0" + encode(unsigned)).hexdigest()
    return "irt." + digest

def _emit(state, inp):
    authorize(state, inp); cmd, actor = inp["command"], inp["actor"]; expected = profile(actor)
    require(state["stateVersion"] < 2**63 - 1 and len(state["actualProgressRefs"]) < 32, 7)
    element = next(item for item in state["world"]["elements"] if item["elementId"] == expected["elementId"])
    operational = element["operationalState"]
    require(element["reserveStatus"] == "none" and operational["vehicleBreakdownState"] is None
        and operational["movementEnded"] is None and element["ammunition"]["points"] == 10
        and all(component["currentToe"] > 0 for component in element["components"]), 3)
    facts = next(item for item in world.PACK["elements"] if item["elementId"] == expected["elementId"])
    require(facts["mobilityId"] == "land.mobility.non-motorized"
        and facts["breakdownVehicleCohort"] is None
        and facts["combatClassificationId"] == "land.combat-classification.combat-unit"
        and facts["placementMode"] == "independent", 3)
    require(not any(state["world"][key] for key in
        ("relationships", "guards", "brokenVehicleLots", "futureObligations", "settlements")), 3)
    origin, destination = cmd["originLocationId"], cmd["destinationLocationId"]
    require(destination in world.GRAPH[origin]
        and all(item["currentLocationId"] != destination for item in state["world"]["elements"]), 5)
    moving = [item for item in state["world"]["representations"]
        if item["bindingKind"] == "independent-element" and item["boundElementIds"] == [expected["elementId"]]]
    require(len(moving) == 1 and moving[0]["currentLocationId"] == origin, 4)
    adjacent = [item for item in state["world"]["representations"]
        if item["currentLocationId"] in world.GRAPH[destination] and item["boundElementIds"]
        and all(world.SIDES[element_id] == opposite(actor) for element_id in item["boundElementIds"])]
    require(len(adjacent) == 1, 3); reacting = adjacent[0]
    require(reacting["bindingKind"] == "independent-element"
        and reacting["boundElementIds"] == [expected["opponentElementId"]]
        and reacting["currentLocationId"] == expected["opponentAssault"], 3)
    reacting_element = next(item for item in state["world"]["elements"]
        if item["elementId"] == expected["opponentElementId"])
    reacting_facts = next(item for item in world.PACK["elements"]
        if item["elementId"] == expected["opponentElementId"])
    require(reacting_facts["combatClassificationId"] == "land.combat-classification.combat-unit"
        and reacting_facts["placementMode"] == "independent" and reacting_element["reserveStatus"] == "none"
        and reacting_element["operationalState"]["cohesionLevel"] > -26
        and reacting_element["operationalState"]["movementEnded"] is None, 3)
    try:
        terrain = imv.mov.terrain_cost(origin, destination); spent = operational["capabilityPointsExpended"]["numerator"]
        after_cp, dp = imv.mov.cost(10, spent, [], terrain=terrain)
    except imv.mov.Invalid as error: raise Invalid(int(error.code[-3:])) from error
    require((terrain, spent, after_cp, dp) == (2, 2, 4, 0), 3)
    version = state["stateVersion"] + 1
    trigger_rep = representation(moving[0]); trigger_rep["currentLocationId"] = destination
    route = copy.deepcopy(state["breakdownFlow"]["route"])
    require(route["owner"] == actor and route["currentLocationId"] == origin
        and route["originLocationId"] == destination and route["cohortIds"] == [], 4)
    route["currentLocationId"] = destination
    reacting_position = dict(suspendedMovementPosition=copy.deepcopy(state["sequencePosition"]),
        phasingSide=actor, reactingSide=opposite(actor))
    window_id = identity(INVENTORY["domains"]["window"], dict(campaignId=state["campaignId"],
        rulesetHash=state["rulesetHash"], moveContractVersion=4, committedStateVersion=version,
        triggeringRepresentation=trigger_rep, originLocationId=origin,
        destinationLocationId=destination, reactingSide=opposite(actor)))
    reacting_rep = representation(reacting)
    opportunity_id = identity(INVENTORY["domains"]["opportunity"],
        dict(windowId=window_id, reactingRepresentation=reacting_rep))
    window = dict(reactionWindowId=window_id, triggerCommittedStateVersion=version,
        phasingSide=actor, reactingSide=opposite(actor), reactingPosition=reacting_position,
        triggerAuthority=dict(moveContractVersion=4, elementId=expected["elementId"],
            triggeringRepresentation=trigger_rep, originLocationId=origin, destinationLocationId=destination),
        apparentTrigger=dict(apparentRepresentationId=trigger_rep["representationId"],
            originLocationId=origin, destinationLocationId=destination),
        frozenOpportunities=[dict(opportunityId=opportunity_id, reactingRepresentation=reacting_rep,
            adjacencyEvidence=dict(triggerLocationId=reacting_rep["currentLocationId"],
                committedDestinationLocationId=destination, isAdjacent=True,
                sources=[dict(sourceId="spi-1979-land-rules", locator="8.51")]))],
        resolvedOpportunityIds=[], activeOpportunityId=None)
    flow = dict(kind="reacting", phasingContinuation=dict(kind="resume-route", route=route), reactorRoute=None)
    sources = [dict(sourceId="spi-1979-map-a", locator="8.37")]
    event = dict(contractVersion=4, eventType="element-moved", campaignId=state["campaignId"],
        stateVersion=version, priorStateVersion=state["stateVersion"],
        fromPositionId=state["sequencePosition"]["positionId"], gameTurn=1, operationStage=1,
        actingSide=actor, elementId=expected["elementId"], representationId=trigger_rep["representationId"],
        originLocationId=origin, destinationLocationId=destination, mobilityId=facts["mobilityId"],
        mobilitySources=sources, cost=dict(destinationTerrainId="land.terrain.clear",
            destinationTerrainCost=world.cp(2), destinationTerrainSources=copy.deepcopy(sources),
            routeAdjustment=None, crossedHexsideCosts=[], totalCost=world.cp(2)),
        capabilityPointsExpendedBefore=world.cp(2), capabilityPointsExpendedAfter=world.cp(4),
        cohesionBefore=0, cohesionAfter=0, movementEndedAfter=None,
        sequencePosition=copy.deepcopy(state["sequencePosition"]), openedReactionWindow=window,
        rulesetHash=state["rulesetHash"], breakdownAccounting=[], breakdownFlowAfter=flow,
        configurationHash=state["configurationHash"], creationBinding=state["creationBinding"],
        creationEventHash=state["creationEventHash"], cycleId=state["cycleId"],
        openingBaseHash=state["openingBaseHash"], completionReceiptId=state["completionReceiptId"],
        priorPrefix=state["prefix"], input=copy.deepcopy(inp), receiptId="pending")
    event["receiptId"] = receipt(event); data = raw(event, "TriggerEvent")
    after = copy.deepcopy(state)
    moved = next(item for item in after["world"]["elements"] if item["elementId"] == expected["elementId"])
    moved["currentLocationId"] = destination
    moved["operationalState"]["capabilityPointsExpended"] = world.cp(4)
    next(item for item in after["world"]["representations"]
        if item["representationId"] == trigger_rep["representationId"])["currentLocationId"] = destination
    after["members"][0]["spentCp"] = world.cp(4); after["tracks"][0]["route"].append(destination)
    after.update(stateVersion=version, prefix=imv.rd.pre.env.sequence.prefix_event(state["prefix"], data),
        currentPosition=dict(kind="reaction", reactingPosition=copy.deepcopy(reacting_position)),
        reactionWindow=copy.deepcopy(window), breakdownFlow=copy.deepcopy(flow))
    rid = event["receiptId"]
    after["receipts"].append(dict(commandHash=sha(raw(inp, "InheritedInput")), eventHash=sha(data),
        receiptId=rid, actor=actor, stateVersion=version))
    after["actualProgressRefs"].append(dict(eventType="element-moved", receiptId=rid, eventHash=sha(data)))
    return canonical(after, "TriggerState"), data

def read_event(data):
    event = parse(data, "TriggerEvent")
    require(event["contractVersion"] == 4 and event["eventType"] == "element-moved"
        and event["openedReactionWindow"]["triggerAuthority"]["moveContractVersion"] == 4
        and event["breakdownFlowAfter"]["kind"] == "reacting", 3)
    require(event["receiptId"] == receipt(event), 4)
    return event

def replay(request, created, preamble, weather_events, stage_events, reserve_events, movement_events, events):
    require(type(events) is list and len(events) <= 1, 7)
    state = initial(request, created, preamble, weather_events, stage_events, reserve_events, movement_events)
    for data in events:
        event = read_event(data); state, expected = _emit(state, event["input"]); require(data == expected, 6)
    return state

def apply(request, created, preamble, weather_events, stage_events, reserve_events, movement_events, events, inp):
    base = initial(request, created, preamble, weather_events, stage_events, reserve_events, movement_events)
    authorize(base, inp)
    state = replay(request, created, preamble, weather_events, stage_events, reserve_events, movement_events, events)
    if events:
        accepted = read_event(events[0])["input"]
        require(raw(inp, "InheritedInput") == raw(accepted, "InheritedInput"), 6)
        return state, events[0], True
    after, data = _emit(base, inp)
    return after, data, False

def read_state(data, request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, events):
    value = parse(data, "TriggerState")
    expected = replay(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, events)
    require(events and data == raw(expected, "TriggerState"), 6)
    return value

def source_trace(case):
    result = imv.trace(case); require(imv.goldens(result) == case["goldens"], 9)
    args, _, movement_events, states = result
    return args + (movement_events[:1],), states[1]

def trace(case):
    movement_case = next(item for item in json.loads(imv.FIXTURE.read_text())["cases"]
        if item["name"] == case["predecessorCase"])
    args, before = source_trace(movement_case); state0 = initial(*args); require(state0 == before, 4)
    inp = command(state0); state, event, duplicate = apply(*args, [], inp); assert not duplicate
    decoded, expected = read_event(event), case["expected"]
    assert expected == profile(expected["actor"])
    assert ((decoded["actingSide"], decoded["originLocationId"], decoded["destinationLocationId"])
        == (expected["actor"], expected["rear"], expected["assault"]))
    assert decoded["stateVersion"] == 13 and decoded["priorStateVersion"] == 12
    assert decoded["capabilityPointsExpendedBefore"] == world.cp(2)
    assert decoded["capabilityPointsExpendedAfter"] == world.cp(4)
    assert decoded["cohesionBefore"] == decoded["cohesionAfter"] == 0
    assert decoded["movementEndedAfter"] is None and decoded["breakdownAccounting"] == []
    window = decoded["openedReactionWindow"]; opportunity = window["frozenOpportunities"][0]
    assert len(window["frozenOpportunities"]) == 1
    assert opportunity["reactingRepresentation"]["boundElementIds"] == [expected["opponentElementId"]]
    assert opportunity["reactingRepresentation"]["currentLocationId"] == expected["opponentAssault"]
    assert window["resolvedOpportunityIds"] == [] and window["activeOpportunityId"] is None
    expected_window_id = identity(INVENTORY["domains"]["window"], dict(
        campaignId=state["campaignId"], rulesetHash=state["rulesetHash"], moveContractVersion=4,
        committedStateVersion=13, triggeringRepresentation=window["triggerAuthority"]["triggeringRepresentation"],
        originLocationId=expected["rear"], destinationLocationId=expected["assault"],
        reactingSide=opposite(expected["actor"])))
    assert window["reactionWindowId"] == expected_window_id
    assert opportunity["opportunityId"] == identity(INVENTORY["domains"]["opportunity"], dict(
        windowId=expected_window_id, reactingRepresentation=opportunity["reactingRepresentation"]))
    assert state["currentPosition"] == dict(kind="reaction", reactingPosition=window["reactingPosition"])
    assert state["reactionWindow"] == window and state["breakdownFlow"] == decoded["breakdownFlowAfter"]
    assert state["tracks"][0]["route"] == [expected["assault"], expected["rear"], expected["assault"]]
    assert state["prefix"] == imv.rd.pre.env.sequence.prefix_event(before["prefix"], event)
    moved = next(item for item in state["world"]["elements"] if item["elementId"] == expected["elementId"])
    assert moved["currentLocationId"] == expected["assault"]
    assert moved["operationalState"]["capabilityPointsExpended"] == world.cp(4)
    expected_world = copy.deepcopy(before["world"])
    expected_element = next(item for item in expected_world["elements"] if item["elementId"] == expected["elementId"])
    expected_element["currentLocationId"] = expected["assault"]
    expected_element["operationalState"]["capabilityPointsExpended"] = world.cp(4)
    next(item for item in expected_world["representations"]
        if item["representationId"] == decoded["representationId"])["currentLocationId"] = expected["assault"]
    assert state["world"] == expected_world
    expected_members = copy.deepcopy(before["members"]); expected_members[0]["spentCp"] = world.cp(4)
    assert state["members"] == expected_members and state["receipts"][:-1] == before["receipts"]
    assert state["actualProgressRefs"][:-1] == before["actualProgressRefs"]
    changed_top = {"stateVersion", "prefix", "world", "receipts", "members", "tracks",
        "actualProgressRefs", "breakdownFlow"}
    assert all(state[key] == before[key] for key in before if key not in changed_top)
    assert read_state(raw(state, "TriggerState"), *args, [event]) == state
    retried, old, duplicate = apply(*args, [event], inp)
    assert duplicate and old == event and retried == state
    return args, inp, event, state

def goldens(result):
    args, inp, event, state = result
    return {name: dict(bytes=len(data), sha256=sha(data)) for name, data in (
        ("movement-prefix", args[-1][0]), ("input", raw(inp, "InheritedInput")),
        ("event", event), ("state", raw(state, "TriggerState")))}

def rejected(fn):
    try: fn()
    except Invalid: return
    raise AssertionError("expected CMB-IRT rejection")

def changed(value, path, replacement):
    result = copy.deepcopy(value); node = result
    for key in path[:-1]: node = node[key]
    node[path[-1]] = replacement
    return result

def leaves(value, path=()):
    if type(value) is dict:
        for key, child in value.items(): yield from leaves(child, path + (key,))
    elif type(value) is list:
        if not value: yield path, value
        for index, child in enumerate(value): yield from leaves(child, path + (index,))
    else: yield path, value

def different(value):
    if value is None: return "unexpected"
    if type(value) is bool: return not value
    if type(value) is int: return value + 1
    if type(value) is list: return ["unexpected"]
    if value.startswith("sha256:"):
        return "sha256:" + ("f" if value != "sha256:" + "f" * 64 else "e") * 64
    return value + "x"

def verify(result):
    args, inp, event, state = result
    counts = dict(cuts=0, retries=1, mutations=0, raw=0, boundaries=0)
    assert replay(*args, []) == initial(*args); counts["cuts"] += 1
    assert replay(*args, [event]) == state; counts["cuts"] += 1
    actor, other = inp["actor"], opposite(inp["actor"])
    cases = [(("actor",), other), (("actor",), "system"),
        (("command", "expectedPriorVersion"), 11), (("command", "expectedPriorVersion"), 13),
        (("command", "destinationLocationId"), profile(actor)["rear"]),
        (("command", "destinationLocationId"), profile(other)["rear"]),
        (("command", "originLocationId"), profile(actor)["assault"]),
        (("command", "kind"), "react-move"),
        (("command", "creationEventHash"), "sha256:" + "f" * 64)]
    for path, value in cases:
        rejected(lambda path=path, value=value: apply(*args, [], changed(inp, path, value)))
        counts["boundaries"] += 1
    rejected(lambda: initial(*args[:-1], [])); counts["boundaries"] += 1
    rejected(lambda: initial(*args[:-1], args[-1] + args[-1])); counts["boundaries"] += 1
    rejected(lambda: apply(*args, [event], imv.command(state, profile(actor)["rear"]))); counts["boundaries"] += 1
    decoded = read_event(event)
    for path, value in leaves(decoded):
        bad = changed(decoded, path, different(value))
        try:
            if path != ("receiptId",): bad["receiptId"] = receipt(bad)
            data = raw(bad, "TriggerEvent")
        except Invalid: data = encode(bad)
        rejected(lambda data=data: replay(*args, [data])); counts["mutations"] += 1
    for opportunities in ([], decoded["openedReactionWindow"]["frozenOpportunities"] * 2):
        bad = changed(decoded, ("openedReactionWindow", "frozenOpportunities"), opportunities)
        bad["receiptId"] = receipt(bad); data = raw(bad, "TriggerEvent")
        rejected(lambda data=data: replay(*args, [data])); counts["boundaries"] += 1
    state_paths = [("stateVersion",), ("prefix",), ("currentPosition", "kind"),
        ("reactionWindow", "frozenOpportunities"), ("breakdownFlow", "kind"),
        ("world", "elements", 0, "currentLocationId"), ("members", 0, "spentCp", "numerator"),
        ("actualProgressRefs", 1, "eventHash")]
    for path in state_paths:
        node = state
        for key in path: node = node[key]
        bad = changed(state, path, different(node))
        try: data = raw(bad, "TriggerState")
        except Invalid: data = encode(bad)
        rejected(lambda data=data: read_state(data, *args, [event])); counts["mutations"] += 1
    for kind, value in (("TriggerEvent", decoded), ("InheritedInput", inp), ("TriggerState", state)):
        data = raw(value, kind)
        contract_version = value.get("contractVersion", value.get("command", {}).get("contractVersion"))
        variants = [data + b" ", b" " + data, b"\xef\xbb\xbf" + data, b"\xff", data[:-1], b"{}",
            b'{"unknown":0,' + data[1:], encode(dict(reversed(list(value.items())))),
            data.replace(b'"contractVersion":', b'"contractVersion": ', 1),
            data.replace(b'"contractVersion":' + str(contract_version).encode(),
                b'"contractVersion":true', 1)]
        for bad in variants:
            rejected(lambda bad=bad, kind=kind: parse(bad, kind)); counts["raw"] += 1
    return counts

def fixture_shape(fixture):
    require(fixture["contractVersion"] == 1
        and fixture["contract"] == "combat-inherited-reaction-trigger-v1", 9)
    require(len(fixture["cases"]) == 2
        and {case["expected"]["actor"] for case in fixture["cases"]} == {"axis", "commonwealth"}, 9)
    require(set(fixture["sourceHashes"]) == SOURCE_PATHS, 9)

def main():
    fixture = json.loads(FIXTURE.read_text()); fixture_shape(fixture); repo = ROOT.parent.parent
    for path, digest in fixture["sourceHashes"].items(): require(sha((repo / path).read_bytes()) == digest, 9)
    results, totals = [], dict(cuts=0, retries=0, mutations=0, raw=0, boundaries=0)
    for case in fixture["cases"]:
        result = trace(case); results.append(result); require(goldens(result) == case["goldens"], 9)
        for key, value in verify(result).items(): totals[key] += value
    for index, result in enumerate(results):
        foreign = results[1 - index]
        rejected(lambda result=result, foreign=foreign:
            replay(*result[0][:-1], foreign[0][-1], [result[2]]))
        totals["boundaries"] += 1
    print("CMB-IRT PASS", json.dumps(dict(traces=len(results), triggers=len(results),
        sourcePins=len(fixture["sourceHashes"]), **totals), sort_keys=True))

if __name__ == "__main__": main()
