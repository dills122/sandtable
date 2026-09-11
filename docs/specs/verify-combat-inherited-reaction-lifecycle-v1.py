#!/usr/bin/env python3
"""Creation-rooted inherited Reaction lifecycle oracle; no runtime admission or writes."""
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
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module

irt = load("inherited_reaction_trigger", "verify-combat-inherited-reaction-trigger-v1.py")
iml = load("inherited_movement_lifecycle", "verify-combat-inherited-movement-lifecycle-v1.py")
imv, world, encode, sha = irt.imv, irt.world, irt.encode, irt.sha
INVENTORY = json.loads((ROOT / "combat-inherited-reaction-lifecycle-v1.schema.json").read_text())
FIXTURE = ROOT / "fixtures/combat-inherited-reaction-lifecycle-v1.json"
LOCAL = {key: [tuple(field.split(":")) for field in value.split()]
    for key, value in INVENTORY["objects"].items()}
SCHEMA = irt.SCHEMA | iml.SCHEMA | LOCAL
KINDS = ["move-reacting-element", "complete-reaction-participant", "resolve-breakdown-stop",
    "close-reaction-window-no-eligible-reactor"]
EVENTS = ["reacting-element-moved", "reaction-participant-completed", "breakdown-stop-resolved",
    "reaction-window-closed"]
TYPES = ["MoveEvent", "CompleteEvent", "ResolveEvent", "CloseEvent"]
VERSIONS = [3, 3, 2, 3]
DOMAIN_KEYS = ["moveReceipt", "completeReceipt", "resolveReceipt", "closeReceipt"]
SOURCE_PATHS = {
    "docs/specs/combat-inherited-reaction-lifecycle-v1.schema.json",
    "docs/specs/combat-inherited-reaction-trigger-v1.md",
    "docs/specs/combat-inherited-reaction-trigger-v1.schema.json",
    "docs/specs/verify-combat-inherited-reaction-trigger-v1.py",
    "docs/specs/fixtures/combat-inherited-reaction-trigger-v1.json",
    "docs/specs/combat-inherited-movement-lifecycle-v1.schema.json",
    "src/Cna.Core/Campaigns/CampaignReactingElementMovedV2Factory.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownLifecycleFactory.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownStopResolvedFactory.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownMoveEventSerializer.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownLifecycleCodec.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownStopResolvedCodec.cs",
    "src/Cna.Core/Campaigns/CampaignV11BreakdownProjector.cs",
    "src/Cna.Core/Campaigns/CampaignV11MoveProjector.cs",
    "src/Cna.Core/Observations/CampaignObservationV6DisclosureIdentity.cs",
    "src/Cna.Core/Actions/CampaignObservationV6ActionCandidates.cs",
}

class Invalid(ValueError):
    def __init__(self, code):
        self.code = f"CMB-IRL-{code:03}"; super().__init__(self.code)

def require(ok, code):
    if not ok: raise Invalid(code)

def variant(value, union):
    require(type(value) is dict and type(value.get("kind")) is str, 1)
    require(value["kind"] in INVENTORY["unions"][union], 3)
    return INVENTORY["unions"][union][value["kind"]]

def typed(value, kind, depth=0):
    require(depth <= INVENTORY["limits"]["depth"], 1)
    if kind == "null": require(value is None, 3)
    elif kind == "empty": require(type(value) is list and not value, 3)
    elif kind in INVENTORY["unions"]: typed(value, variant(value, kind), depth)
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
    if kind in INVENTORY["unions"]: return canonical(value, variant(value, kind))
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
    require(len(data) <= INVENTORY["limits"]["bytes"], 1); return data

def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= INVENTORY["limits"]["bytes"], 1)
    def pairs(items):
        result = {}
        for key, value in items: require(key not in result, 1); result[key] = value
        return result
    try:
        value = json.loads(data.decode("utf8"), object_pairs_hook=pairs,
            parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error: raise Invalid(1) from error
    require(raw(value, kind) == data, 8); return value

def opposite(side): return irt.opposite(side)

def profile(phasing):
    base = irt.profile(phasing); reacting = opposite(phasing)
    return base | dict(phasing=phasing, reacting=reacting, reactingElementId=base["opponentElementId"],
        reactingAssault=base["opponentAssault"], reactingRear=f"{reacting}-rear",
        reactingSupply=f"{reacting}-supply")

def initial(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events):
    require(type(trigger_events) is list and len(trigger_events) == 1, 3)
    try:
        state = irt.replay(request, created, preamble, weather_events, stage_events, reserve_events,
            movement_events, trigger_events)
        trigger = irt.read_event(trigger_events[0])
    except (ValueError, KeyError, TypeError) as error: raise Invalid(4) from error
    require(state["stateVersion"] == 13 and trigger["stateVersion"] == 13
        and len(state["reactionWindow"]["frozenOpportunities"]) == 1
        and state["reactionWindow"]["activeOpportunityId"] is None
        and state["reactionWindow"]["resolvedOpportunityIds"] == []
        and state["breakdownFlow"]["kind"] == "reacting"
        and state["breakdownFlow"]["reactorRoute"] is None, 4)
    return canonical(state, "LifecycleState")

def reaction_cost():
    return dict(destinationTerrainId="land.terrain.clear", destinationTerrainCost=world.cp(2),
        routeAdjustment=None, crossedHexsideCosts=[], totalCost=world.cp(2))

def move_options(state):
    window = state["reactionWindow"]
    if window is None or window["resolvedOpportunityIds"]: return []
    p = profile(state["firstActingSide"])
    element = next(item for item in state["world"]["elements"] if item["elementId"] == p["reactingElementId"])
    origin = element["currentLocationId"]
    destination = p["reactingRear"] if origin == p["reactingAssault"] else (
        p["reactingSupply"] if origin == p["reactingRear"] else None)
    if destination is None: return []
    return [dict(originLocationId=origin, destinationLocationId=destination,
        costBreakdown=reaction_cost())]

def identity(domain, fields):
    value = {"domain": domain}; value.update(fields); return sha(encode(value))

def public_window(state):
    window = state["reactionWindow"]; require(window is not None, 6)
    return identity(INVENTORY["domains"]["windowCapability"], dict(campaignId=state["campaignId"],
        rulesetHash=state["rulesetHash"], committedStateVersion=window["triggerCommittedStateVersion"],
        reactingSide=window["reactingSide"]))

def public_opportunity(state):
    capability = identity(INVENTORY["domains"]["capability"], dict(moveOptions=move_options(state)))
    return identity(INVENTORY["domains"]["opportunityCapability"], dict(windowId=public_window(state),
        stateVersion=state["stateVersion"], capabilityKey=capability))

def command(state, kind):
    require(kind in KINDS, 3); window = state["reactionWindow"]
    command_value = dict(contractVersion=2, kind=kind); action = dict(contractVersion=1, kind=kind)
    if kind == KINDS[0]:
        options = move_options(state); require(len(options) == 1 and window is not None, 6); option = options[0]
        fields = dict(windowId=public_window(state), opportunityId=public_opportunity(state),
            originLocationId=option["originLocationId"], destinationLocationId=option["destinationLocationId"])
        action.update(fields); action["costBreakdown"] = copy.deepcopy(option["costBreakdown"]); command_value.update(fields)
    elif kind == KINDS[1]:
        require(window is not None and window["activeOpportunityId"] is not None, 6)
        fields = dict(windowId=public_window(state), opportunityId=public_opportunity(state)); action.update(fields); command_value.update(fields)
    elif kind == KINDS[2]:
        require(state["breakdownFlow"]["kind"] == "reactor-stop-open", 6)
        stop_capability = identity(INVENTORY["domains"]["stopCapability"], dict(campaignId=state["campaignId"],
            rulesetHash=state["rulesetHash"], stateVersion=state["stateVersion"], audience="system"))
        action["stopId"] = stop_capability; command_value["stopId"] = stop_capability
    else:
        require(window is not None and window["activeOpportunityId"] is None
            and set(window["resolvedOpportunityIds"]) == {item["opportunityId"] for item in window["frozenOpportunities"]}
            and state["breakdownFlow"]["kind"] == "reacting" and state["breakdownFlow"]["reactorRoute"] is None, 6)
        action["windowId"] = public_window(state); command_value["windowId"] = action["windowId"]
    command_value.update(actionId=sha(encode(action)), creationBinding=state["creationBinding"],
        creationEventHash=state["creationEventHash"], cycleId=state["cycleId"],
        expectedPriorVersion=state["stateVersion"], expectedPositionId=state["sequencePosition"]["positionId"])
    actor = state["reactionWindow"]["reactingSide"] if kind in KINDS[:2] else "system"
    return dict(command=command_value, actor=actor)

def authorize(state, inp):
    typed(inp, "LifecycleInput"); cmd = inp["command"]; kind = cmd["kind"]
    require(cmd["contractVersion"] == 2, 3)
    expected_actor = opposite(state["firstActingSide"]) if kind in KINDS[:2] else "system"
    require(inp["actor"] == expected_actor, 5)
    require(cmd["creationBinding"] == state["creationBinding"]
        and cmd["creationEventHash"] == state["creationEventHash"] and cmd["cycleId"] == state["cycleId"], 4)

def receipt(event, index):
    unsigned = {key: value for key, value in canonical(event, TYPES[index]).items() if key != "receiptId"}
    digest = hashlib.sha256(INVENTORY["domains"][DOMAIN_KEYS[index]].encode() + b"\0" + encode(unsigned)).hexdigest()
    return ("iml." if index == 2 else "irl.") + digest

def commit(state, after, inp, event, index):
    event["receiptId"] = receipt(event, index); data = raw(event, TYPES[index]); rid = event["receiptId"]
    after.update(stateVersion=event["stateVersion"], prefix=imv.rd.pre.env.sequence.prefix_event(state["prefix"], data))
    after["receipts"].append(dict(commandHash=sha(raw(inp, "LifecycleInput")), eventHash=sha(data),
        receiptId=rid, actor=inp["actor"], stateVersion=event["stateVersion"]))
    return canonical(after, "LifecycleState"), data

def _emit(state, inp):
    authorize(state, inp); kind = inp["command"]["kind"]; index = KINDS.index(kind); cmd = inp["command"]
    require(cmd["expectedPriorVersion"] == state["stateVersion"]
        and cmd["expectedPositionId"] == state["sequencePosition"]["positionId"], 6)
    require(state["stateVersion"] < 2**63 - 1 and len(state["receipts"]) < 512, 7)
    require(raw(inp, "LifecycleInput") == raw(command(state, kind), "LifecycleInput"), 5)
    after = copy.deepcopy(state); version = state["stateVersion"] + 1
    common = dict(configurationHash=state["configurationHash"], creationBinding=state["creationBinding"],
        creationEventHash=state["creationEventHash"], cycleId=state["cycleId"],
        openingBaseHash=state["openingBaseHash"], completionReceiptId=state["completionReceiptId"],
        priorPrefix=state["prefix"], input=copy.deepcopy(inp), receiptId="pending")
    p = profile(state["firstActingSide"]); window = state["reactionWindow"]
    if index == 0:
        require(state["currentPosition"]["kind"] == "reaction" and window is not None
            and state["breakdownFlow"]["kind"] == "reacting" and state["breakdownFlow"]["reactorRoute"] is None, 6)
        opportunity = window["frozenOpportunities"][0]; rep = opportunity["reactingRepresentation"]
        element = next(item for item in state["world"]["elements"] if item["elementId"] == p["reactingElementId"])
        actual_rep = next(item for item in state["world"]["representations"] if item["representationId"] == rep["representationId"])
        require(rep == irt.representation(actual_rep) and element["currentLocationId"] == p["reactingAssault"], 4)
        route = dict(routeId="pending", firstMoveStateVersion=version, elementId=element["elementId"],
            representationId=actual_rep["representationId"], owner=p["reacting"], originLocationId=p["reactingAssault"],
            currentLocationId=p["reactingRear"], cohortIds=[])
        route_identity = dict(domain=imv.INVENTORY["domains"]["route"], campaignId=state["campaignId"],
            rulesetHash=state["rulesetHash"])
        route_identity.update({key: value for key, value in route.items() if key not in ("routeId", "currentLocationId")})
        route["routeId"] = sha(encode(route_identity))
        window_after = copy.deepcopy(window); window_after["activeOpportunityId"] = opportunity["opportunityId"]
        flow = dict(kind="reacting", phasingContinuation=copy.deepcopy(state["breakdownFlow"]["phasingContinuation"]), reactorRoute=route)
        sources = [dict(sourceId="spi-1979-map-a", locator="8.37")]
        event = dict(contractVersion=3, eventType=EVENTS[index], campaignId=state["campaignId"],
            stateVersion=version, priorStateVersion=state["stateVersion"], fromPositionId=state["sequencePosition"]["positionId"],
            gameTurn=1, operationStage=1, actingSide=p["reacting"], actionId=cmd["actionId"],
            submittedWindowId=cmd["windowId"], submittedOpportunityId=cmd["opportunityId"],
            windowId=window["reactionWindowId"], opportunityId=opportunity["opportunityId"],
            elementId=element["elementId"], representationId=actual_rep["representationId"],
            originLocationId=p["reactingAssault"], destinationLocationId=p["reactingRear"],
            mobilityId="land.mobility.non-motorized", mobilitySources=sources,
            cost=dict(destinationTerrainId="land.terrain.clear", destinationTerrainCost=world.cp(2),
                destinationTerrainSources=copy.deepcopy(sources), routeAdjustment=None, crossedHexsideCosts=[], totalCost=world.cp(2)),
            capabilityPointsExpendedBefore=world.cp(0), capabilityPointsExpendedAfter=world.cp(2),
            cohesionBefore=0, cohesionAfter=0, reactionWindowAfter=window_after, rulesetHash=state["rulesetHash"],
            breakdownAccounting=[], breakdownFlowAfter=flow, **common)
        moved = next(item for item in after["world"]["elements"] if item["elementId"] == element["elementId"])
        moved["currentLocationId"] = p["reactingRear"]; moved["operationalState"]["capabilityPointsExpended"] = world.cp(2)
        next(item for item in after["world"]["representations"] if item["representationId"] == actual_rep["representationId"])["currentLocationId"] = p["reactingRear"]
        after["tracks"].append(dict(unit=dict(creationBinding=state["creationBinding"], originalSide=p["reacting"],
            elementId=element["elementId"]), route=[p["reactingAssault"], p["reactingRear"]]))
        after.update(reactionWindow=copy.deepcopy(window_after), breakdownFlow=copy.deepcopy(flow))
        event["receiptId"] = receipt(event, index); data = raw(event, TYPES[index]); rid = event["receiptId"]
        after["actualProgressRefs"].append(dict(eventType=EVENTS[index], receiptId=rid, eventHash=sha(data)))
        after.update(stateVersion=version, prefix=imv.rd.pre.env.sequence.prefix_event(state["prefix"], data))
        after["receipts"].append(dict(commandHash=sha(raw(inp, "LifecycleInput")), eventHash=sha(data), receiptId=rid,
            actor=inp["actor"], stateVersion=version))
        return canonical(after, "LifecycleState"), data
    if index == 1:
        require(window is not None and window["activeOpportunityId"] is not None
            and state["breakdownFlow"]["kind"] == "reacting" and state["breakdownFlow"]["reactorRoute"] is not None, 6)
        opportunity_id = window["activeOpportunityId"]; window_after = copy.deepcopy(window)
        window_after["resolvedOpportunityIds"].append(opportunity_id); window_after["activeOpportunityId"] = None
        stop = dict(stopId="pending", recordedStateVersion=version, route=copy.deepcopy(state["breakdownFlow"]["reactorRoute"]),
            reason="reaction-completed", weatherKind="normal", cohortInputs=[])
        stop_identity = dict(domain=INVENTORY["domains"]["stop"], campaignId=state["campaignId"], rulesetHash=state["rulesetHash"])
        stop_identity.update({key: value for key, value in stop.items() if key != "stopId"}); stop["stopId"] = sha(encode(stop_identity))
        flow = dict(kind="reactor-stop-open", phasingContinuation=copy.deepcopy(state["breakdownFlow"]["phasingContinuation"]), stop=stop)
        event = dict(contractVersion=3, eventType=EVENTS[index], campaignId=state["campaignId"], stateVersion=version,
            priorStateVersion=state["stateVersion"], fromPositionId=state["sequencePosition"]["positionId"],
            actingSide=p["reacting"], actionId=cmd["actionId"], submittedWindowId=cmd["windowId"],
            submittedOpportunityId=cmd["opportunityId"], windowId=window["reactionWindowId"],
            opportunityId=opportunity_id, reactionWindowAfter=window_after, rulesetHash=state["rulesetHash"],
            breakdownFlowAfter=flow, **common)
        after.update(currentPosition=dict(kind="breakdown-stop", sequencePosition=copy.deepcopy(state["sequencePosition"])),
            reactionWindow=copy.deepcopy(window_after), breakdownFlow=copy.deepcopy(flow))
        return commit(state, after, inp, event, index)
    if index == 2:
        require(window is not None and state["currentPosition"]["kind"] == "breakdown-stop"
            and state["breakdownFlow"]["kind"] == "reactor-stop-open", 6)
        stop = copy.deepcopy(state["breakdownFlow"]["stop"])
        flow = dict(kind="reacting", phasingContinuation=copy.deepcopy(state["breakdownFlow"]["phasingContinuation"]), reactorRoute=None)
        sources = [dict(sourceId="spi-1979-land-rules", locator="21.24-21.26")]
        event = dict(contractVersion=2, eventType=EVENTS[index], campaignId=state["campaignId"], stateVersion=version,
            priorStateVersion=state["stateVersion"], rulesetHash=state["rulesetHash"],
            fromPositionId="land.position.breakdown-stop", actionId=cmd["actionId"], stop=stop,
            randomStateBefore=copy.deepcopy(state["randomState"]), checks=[], createdLots=[],
            randomStateAfter=copy.deepcopy(state["randomState"]), breakdownFlowAfter=flow, sources=sources,
            **{key: value for key, value in common.items() if key != "receiptId"},
            sequencePosition=copy.deepcopy(state["sequencePosition"]), interruptContextAfter=None, receiptId="pending")
        after.update(currentPosition=dict(kind="reaction", reactingPosition=copy.deepcopy(window["reactingPosition"])),
            breakdownFlow=copy.deepcopy(flow))
        return commit(state, after, inp, event, index)
    require(window is not None and state["currentPosition"]["kind"] == "reaction"
        and state["breakdownFlow"]["kind"] == "reacting" and state["breakdownFlow"]["reactorRoute"] is None, 6)
    flow = dict(kind="moving", route=copy.deepcopy(state["breakdownFlow"]["phasingContinuation"]["route"]))
    event = dict(contractVersion=3, eventType=EVENTS[index], campaignId=state["campaignId"], stateVersion=version,
        priorStateVersion=state["stateVersion"], fromPositionId=state["sequencePosition"]["positionId"],
        actingSide=None, actionId=cmd["actionId"], submittedWindowId=cmd["windowId"],
        windowId=window["reactionWindowId"], reason="no-eligible-reactor", closedOpportunityIds=[],
        suspendedSequencePosition=copy.deepcopy(state["sequencePosition"]), rulesetHash=state["rulesetHash"],
        breakdownFlowAfter=flow, **common)
    after.update(currentPosition=dict(kind="sequence", sequencePosition=copy.deepcopy(state["sequencePosition"])),
        reactionWindow=None, breakdownFlow=copy.deepcopy(flow))
    return commit(state, after, inp, event, index)

def read_event(data):
    require(type(data) is bytes and data, 1)
    try: value = json.loads(data)
    except (ValueError, UnicodeError, RecursionError) as error: raise Invalid(1) from error
    require(type(value) is dict and value.get("eventType") in EVENTS, 3)
    index = EVENTS.index(value["eventType"]); value = parse(data, TYPES[index])
    require(value["contractVersion"] == VERSIONS[index]
        and value["input"]["command"]["kind"] == KINDS[index], 3)
    require(value["receiptId"] == receipt(value, index), 4); return value

def replay(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, events):
    require(type(events) is list and len(events) <= 4, 7)
    state = initial(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events)
    for data in events:
        event = read_event(data); state, expected = _emit(state, event["input"]); require(data == expected, 6)
    return state

def apply(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, events, inp):
    state = replay(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, events); authorize(state, inp)
    for data in events:
        accepted = read_event(data)["input"]
        if accepted["command"]["expectedPriorVersion"] == inp["command"]["expectedPriorVersion"]:
            require(raw(inp, "LifecycleInput") == raw(accepted, "LifecycleInput"), 6)
            return state, data, True
    after, data = _emit(state, inp); return after, data, False

def read_state(data, request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, events):
    value = parse(data, "LifecycleState")
    expected = replay(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, events)
    require(events and data == raw(expected, "LifecycleState"), 6); return value

def source_trace(case):
    predecessor = next(item for item in json.loads(irt.FIXTURE.read_text())["cases"] if item["name"] == case["predecessorCase"])
    result = irt.trace(predecessor); require(irt.goldens(result) == predecessor["goldens"], 9)
    args, _, trigger, state = result; return args + ([trigger],), state

def trace(case):
    args, before = source_trace(case); state = initial(*args); require(state == before, 4)
    states = [state]; inputs = []; events = []; p = profile(before["firstActingSide"])
    for index, kind in enumerate(KINDS):
        prior = copy.deepcopy(state); inp = command(state, kind)
        state, event, duplicate = apply(*args, events, inp); assert not duplicate
        decoded = read_event(event); inputs.append(inp); events.append(event); states.append(state)
        assert decoded["contractVersion"] == VERSIONS[index] and state["stateVersion"] == 14 + index
        assert state["prefix"] == imv.rd.pre.env.sequence.prefix_event(prior["prefix"], event)
        assert state["receipts"][-1] == dict(commandHash=sha(raw(inp, "LifecycleInput")), eventHash=sha(event),
            receiptId=decoded["receiptId"], actor=inp["actor"], stateVersion=14 + index)
        if index == 0:
            authority = prior["reactionWindow"]["frozenOpportunities"][0]["opportunityId"]
            assert inp["command"]["windowId"] != prior["reactionWindow"]["reactionWindowId"]
            assert inp["command"]["opportunityId"] != authority
            assert decoded["reactionWindowAfter"]["activeOpportunityId"] == authority
            assert decoded["breakdownFlowAfter"]["reactorRoute"]["firstMoveStateVersion"] == 14
            assert decoded["capabilityPointsExpendedBefore"] == world.cp(0)
            assert decoded["capabilityPointsExpendedAfter"] == world.cp(2)
        elif index == 1:
            authority = prior["reactionWindow"]["activeOpportunityId"]
            assert inp["command"]["opportunityId"] != inputs[0]["command"]["opportunityId"]
            assert move_options(prior) == [dict(originLocationId=p["reactingRear"],
                destinationLocationId=p["reactingSupply"], costBreakdown=reaction_cost())]
            assert decoded["reactionWindowAfter"]["activeOpportunityId"] is None
            assert decoded["reactionWindowAfter"]["resolvedOpportunityIds"] == [authority]
            stop = decoded["breakdownFlowAfter"]["stop"]
            assert (stop["recordedStateVersion"], stop["reason"], stop["weatherKind"], stop["cohortInputs"]) == (
                15, "reaction-completed", "normal", [])
        elif index == 2:
            assert decoded["stop"] == read_event(events[1])["breakdownFlowAfter"]["stop"]
            assert inp["command"]["stopId"] != decoded["stop"]["stopId"]
            assert decoded["checks"] == decoded["createdLots"] == []
            assert decoded["randomStateBefore"] == decoded["randomStateAfter"] == prior["randomState"]
            assert decoded["sources"] == [dict(sourceId="spi-1979-land-rules", locator="21.24-21.26")]
            assert state["currentPosition"]["kind"] == "reaction" and state["reactionWindow"] == prior["reactionWindow"]
        else:
            assert decoded["actingSide"] is None and decoded["reason"] == "no-eligible-reactor"
            assert decoded["closedOpportunityIds"] == [] and decoded["suspendedSequencePosition"] == before["sequencePosition"]
            assert state["reactionWindow"] is None and state["breakdownFlow"]["kind"] == "moving"
        assert read_state(raw(state, "LifecycleState"), *args, events) == state
    moved = next(item for item in state["world"]["elements"] if item["elementId"] == p["reactingElementId"])
    phasing = next(item for item in state["world"]["elements"] if item["elementId"] == p["elementId"])
    assert moved["currentLocationId"] == p["reactingRear"] and moved["operationalState"]["capabilityPointsExpended"] == world.cp(2)
    assert phasing["currentLocationId"] == p["assault"] and phasing["operationalState"]["capabilityPointsExpended"] == world.cp(4)
    assert state["reactionWindow"] is None and state["currentPosition"] == dict(kind="sequence", sequencePosition=before["sequencePosition"])
    assert state["breakdownFlow"] == dict(kind="moving", route=before["breakdownFlow"]["phasingContinuation"]["route"])
    assert len(state["actualProgressRefs"]) == len(before["actualProgressRefs"]) + 1
    assert state["randomState"] == before["randomState"] and state["members"] == before["members"]
    for index, inp in enumerate(inputs):
        retried, old, duplicate = apply(*args, events, inp)
        assert duplicate and old == events[index] and retried == state
    return args, inputs, events, states

def goldens(result):
    args, inputs, events, states = result; values = [("trigger", args[-1][0])]
    values += [(f"input-{index + 1}", raw(value, "LifecycleInput")) for index, value in enumerate(inputs)]
    values += [(f"event-{index + 1}", value) for index, value in enumerate(events)]
    values += [(f"state-{index}", raw(value, "LifecycleState")) for index, value in enumerate(states)]
    return {name: dict(bytes=len(data), sha256=sha(data)) for name, data in values}

def rejected(fn):
    try: fn()
    except Invalid: return
    raise AssertionError("expected CMB-IRL rejection")

def changed(value, path, replacement):
    result = copy.deepcopy(value); node = result
    for key in path[:-1]: node = node[key]
    node[path[-1]] = replacement; return result

def different(value):
    if value is None: return "unexpected"
    if type(value) is bool: return not value
    if type(value) is int: return value + 1
    if type(value) is list: return ["unexpected"]
    if value.startswith("sha256:"): return "sha256:" + "f" * 64
    return value + "x"

def leaves(value, path=()):
    if type(value) is dict:
        for key, child in value.items(): yield from leaves(child, path + (key,))
    elif type(value) is list:
        if not value: yield path, value
        for index, child in enumerate(value): yield from leaves(child, path + (index,))
    else: yield path, value

def verify(result, deep=True):
    args, inputs, events, states = result; counts = dict(cuts=0, retries=4, mutations=0, raw=0, boundaries=0)
    for index, state in enumerate(states): assert replay(*args, events[:index]) == state; counts["cuts"] += 1
    for bad in ([events[1]], [events[2]], [events[3]], events[1:], [events[0], events[2]],
            [events[0], events[1], events[3]], list(reversed(events)), events + [events[-1]]):
        rejected(lambda bad=bad: replay(*args, bad)); counts["boundaries"] += 1
    for index, inp in enumerate(inputs):
        for path, value in ((('actor',), 'system' if inp['actor'] != 'system' else states[0]['firstActingSide']),
                (('command','creationBinding'), 'foreign.creation'), (('command','cycleId'), 'sha256:' + 'f' * 64),
                (('command','expectedPriorVersion'), 1), (('command','expectedPositionId'), 'foreign.position'),
                (('command','actionId'), 'sha256:' + 'd' * 64)):
            rejected(lambda inp=inp, path=path, value=value, index=index:
                apply(*args, events[:index], changed(inp, path, value))); counts["boundaries"] += 1
    # This packet chooses completion after one move even though its rotated capability still lists
    # rear-to-supply; authoritative IDs never substitute for disclosed handles.
    rejected(lambda: apply(*args, events[:1], command(states[1], KINDS[0]))); counts["boundaries"] += 1
    authority_cases = [
        (0, "windowId", states[0]["reactionWindow"]["reactionWindowId"]),
        (1, "opportunityId", states[1]["reactionWindow"]["activeOpportunityId"]),
        (2, "stopId", states[2]["breakdownFlow"]["stop"]["stopId"]),
        (3, "windowId", states[3]["reactionWindow"]["reactionWindowId"]),
    ]
    for index, field, value in authority_cases:
        bad = copy.deepcopy(inputs[index]); bad["command"][field] = value
        action = {key: child for key, child in bad["command"].items() if key in (
            "kind", "windowId", "opportunityId", "originLocationId", "destinationLocationId", "stopId")}
        action = {"contractVersion": 1, **action}
        if index == 0: action["costBreakdown"] = reaction_cost()
        bad["command"]["actionId"] = sha(encode(action))
        rejected(lambda bad=bad, index=index: apply(*args, events[:index], bad)); counts["boundaries"] += 1
    if deep:
        for index, data in enumerate(events):
            event = read_event(data)
            for path, value in leaves(event):
                bad = changed(event, path, different(value))
                try:
                    if path != ("receiptId",): bad["receiptId"] = receipt(bad, index)
                    encoded = raw(bad, TYPES[index])
                except (Invalid, KeyError, TypeError): encoded = encode(bad)
                rejected(lambda encoded=encoded, index=index: replay(*args, events[:index] + [encoded])); counts["mutations"] += 1
        for index, state in enumerate(states[1:], 1):
            for path in (("prefix",), ("stateVersion",), ("breakdownFlow", "kind"),
                    ("world", "elements", 0, "currentLocationId"), ("randomState", "nextByteCursor")):
                node = state
                for key in path: node = node[key]
                bad = changed(state, path, different(node))
                try: encoded = raw(bad, "LifecycleState")
                except Invalid: encoded = encode(bad)
                rejected(lambda encoded=encoded, index=index: read_state(encoded, *args, events[:index])); counts["mutations"] += 1
        for index, data in enumerate(events):
            value = read_event(data)
            variants = [data + b" ", b" " + data, b"\xef\xbb\xbf" + data, b"\xff", data[:-1], b"{}",
                b'{"unknown":0,' + data[1:], encode(dict(reversed(list(value.items())))),
                data.replace(b'"contractVersion":', b'"contractVersion": ', 1)]
            for bad in variants: rejected(lambda bad=bad, kind=TYPES[index]: parse(bad, kind)); counts["raw"] += 1
    return counts

def fixture_shape(fixture):
    require(set(fixture) == {"contractVersion", "contract", "sourceHashes", "cases"}
        and fixture["contractVersion"] == 1 and fixture["contract"] == "combat-inherited-reaction-lifecycle-v1", 9)
    require(len(fixture["cases"]) == 2 and {case["actor"] for case in fixture["cases"]} == {"axis", "commonwealth"}, 9)
    require(set(fixture["sourceHashes"]) == SOURCE_PATHS, 9)

def generated_fixture():
    predecessor = json.loads(irt.FIXTURE.read_text()); repo = ROOT.parent.parent; cases = []
    for source in predecessor["cases"]:
        case = dict(name=source["expected"]["actor"] + "-one-participant-reaction-lifecycle",
            predecessorCase=source["name"], actor=source["expected"]["actor"])
        case["goldens"] = goldens(trace(case)); cases.append(case)
    return dict(contractVersion=1, contract="combat-inherited-reaction-lifecycle-v1",
        sourceHashes={path: sha((repo / path).read_bytes()) for path in sorted(SOURCE_PATHS)}, cases=cases)

def main():
    if sys.argv[1:] == ["--generate-fixture"]:
        print(json.dumps(generated_fixture(), indent=2)); return
    fixture = json.loads(FIXTURE.read_text()); fixture_shape(fixture); repo = ROOT.parent.parent
    for path, digest in fixture["sourceHashes"].items(): require(sha((repo / path).read_bytes()) == digest, 9)
    results = []; totals = dict(cuts=0, retries=0, mutations=0, raw=0, boundaries=0)
    for case in fixture["cases"]:
        result = trace(case); results.append(result); require(goldens(result) == case["goldens"], 9)
        for key, value in verify(result).items(): totals[key] += value
    for index, result in enumerate(results):
        foreign = results[1 - index]
        rejected(lambda result=result, foreign=foreign: replay(*foreign[0], result[2])); totals["boundaries"] += 1
    print("CMB-IRL PASS", json.dumps(dict(traces=2, events=8, sourcePins=len(SOURCE_PATHS), **totals), sort_keys=True))

if __name__ == "__main__": main()
