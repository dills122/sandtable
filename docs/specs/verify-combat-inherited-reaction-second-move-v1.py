#!/usr/bin/env python3
"""Creation-rooted inherited active Reaction second-move oracle; no runtime admission or writes."""
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


irl = load("inherited_reaction_lifecycle", "verify-combat-inherited-reaction-lifecycle-v1.py")
encode, sha = irl.encode, irl.sha
INVENTORY = json.loads((ROOT / "combat-inherited-reaction-second-move-v1.schema.json").read_text())
FIXTURE = ROOT / "fixtures/combat-inherited-reaction-second-move-v1.json"
LOCAL = {key: [tuple(field.split(":")) for field in value.split()]
    for key, value in INVENTORY["objects"].items()}
SCHEMA = irl.SCHEMA | LOCAL
KIND = "move-reacting-element"
EVENT = "reacting-element-moved"
TYPE = "SecondMoveEvent"
STATE = "SecondMoveState"
INPUT = "SecondMoveInput"
SOURCE_PATHS = {
    "docs/design/zoc-reaction-v1.md",
    "docs/specs/zoc-reaction-v1.md",
    "docs/specs/combat-inherited-reaction-second-move-v1.schema.json",
    "docs/specs/combat-inherited-reaction-lifecycle-v1.md",
    "docs/specs/combat-inherited-reaction-lifecycle-v1.schema.json",
    "docs/specs/fixtures/combat-inherited-reaction-lifecycle-v1.json",
    "docs/specs/verify-combat-inherited-reaction-lifecycle-v1.py",
    "src/Cna.Core/Actions/CampaignObservationV6ActionCandidates.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownMoveEventSerializer.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownRecords.cs",
    "src/Cna.Core/Campaigns/CampaignReactingElementMovedV2.cs",
    "src/Cna.Core/Campaigns/CampaignReactingElementMovedV2Factory.cs",
    "src/Cna.Core/Campaigns/CampaignV11MoveProjector.cs",
    "src/Cna.Core/Observations/CampaignObservationV6DisclosureIdentity.cs",
}


class Invalid(ValueError):
    def __init__(self, code):
        self.code = f"CMB-IRS-{code:03}"
        super().__init__(self.code)


def require(ok, code):
    if not ok:
        raise Invalid(code)


def typed(value, kind, depth=0):
    require(depth <= INVENTORY["limits"]["depth"], 1)
    if kind == "null":
        require(value is None, 3)
    elif kind == "empty":
        require(type(value) is list and not value, 3)
    elif kind.endswith("?"):
        if value is not None:
            typed(value, kind[:-1], depth)
    elif kind in SCHEMA:
        require(type(value) is dict and list(value) == [key for key, _ in SCHEMA[kind]], 1)
        for key, child in SCHEMA[kind]:
            typed(value[key], child, depth + 1)
    elif kind.endswith("[]"):
        require(type(value) is list and len(value) <= INVENTORY["limits"]["arrayItems"], 1)
        for item in value:
            typed(item, kind[:-2], depth + 1)
    else:
        try:
            irl.typed(value, kind, depth)
        except irl.Invalid as error:
            raise Invalid(1) from error


def canonical(value, kind):
    if kind.endswith("?"):
        return None if value is None else canonical(value, kind[:-1])
    if kind in SCHEMA:
        return {key: canonical(value[key], child) for key, child in SCHEMA[kind]}
    if kind.endswith("[]"):
        child = kind[:-2]
        result = [canonical(item, child) for item in value]
        if child in irl.imv.rd.pre.env.KEYS:
            result.sort(key=lambda item: tuple(item[key] for key in irl.imv.rd.pre.env.KEYS[child]))
        elif child == "FrozenReactionOpportunity":
            result.sort(key=lambda item: item["opportunityId"])
        elif kind in ("id[]", "hash[]"):
            result.sort()
        return result
    return value


def raw(value, kind):
    typed(value, kind)
    data = encode(canonical(value, kind))
    require(len(data) <= INVENTORY["limits"]["bytes"], 1)
    return data


def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= INVENTORY["limits"]["bytes"], 1)

    def pairs(items):
        result = {}
        for key, value in items:
            require(key not in result, 1)
            result[key] = value
        return result

    try:
        value = json.loads(data.decode("utf8"), object_pairs_hook=pairs,
            parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error:
        raise Invalid(1) from error
    require(raw(value, kind) == data, 8)
    return value


def initial(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, participant_events):
    require(type(participant_events) is list
        and len(participant_events) == INVENTORY["limits"]["participantEvents"], 3)
    try:
        state = irl.replay(request, created, preamble, weather_events, stage_events, reserve_events,
            movement_events, trigger_events, participant_events)
        first = irl.read_event(participant_events[0])
    except (ValueError, KeyError, TypeError) as error:
        raise Invalid(4) from error
    window = state["reactionWindow"]
    route = state["breakdownFlow"]["reactorRoute"]
    p = irl.profile(state["firstActingSide"])
    options = irl.move_options(state)
    require(first["eventType"] == EVENT and state["stateVersion"] == 14
        and state["currentPosition"]["kind"] == "reaction" and window is not None
        and len(window["frozenOpportunities"]) == 1 and window["resolvedOpportunityIds"] == []
        and window["activeOpportunityId"] == window["frozenOpportunities"][0]["opportunityId"]
        and state["breakdownFlow"]["kind"] == "reacting" and route is not None
        and route["firstMoveStateVersion"] == 14 and route["owner"] == p["reacting"]
        and route["originLocationId"] == p["reactingAssault"]
        and route["currentLocationId"] == p["reactingRear"] and route["cohortIds"] == []
        and options == [dict(originLocationId=p["reactingRear"],
            destinationLocationId=p["reactingSupply"], costBreakdown=irl.reaction_cost())], 4)
    return canonical(state, STATE)


def command(state):
    try:
        value = irl.command(state, KIND)
    except (irl.Invalid, KeyError, TypeError) as error:
        raise Invalid(6) from error
    return canonical(value, INPUT)


def authorize(state, inp):
    typed(inp, INPUT)
    cmd = inp["command"]
    require(cmd["contractVersion"] == 2 and cmd["kind"] == KIND, 3)
    require(inp["actor"] == irl.opposite(state["firstActingSide"]), 5)
    require(cmd["creationBinding"] == state["creationBinding"]
        and cmd["creationEventHash"] == state["creationEventHash"]
        and cmd["cycleId"] == state["cycleId"], 4)


def receipt(event):
    unsigned = {key: value for key, value in canonical(event, TYPE).items() if key != "receiptId"}
    digest = hashlib.sha256(INVENTORY["domains"]["moveReceipt"].encode()
        + b"\0" + encode(unsigned)).hexdigest()
    return "irl." + digest


def _emit(state, inp):
    authorize(state, inp)
    cmd = inp["command"]
    require(cmd["expectedPriorVersion"] == state["stateVersion"]
        and cmd["expectedPositionId"] == state["sequencePosition"]["positionId"], 6)
    require(state["stateVersion"] < 2**63 - 1 and len(state["receipts"]) < 512
        and len(state["actualProgressRefs"]) < 512, 7)
    require(raw(inp, INPUT) == raw(command(state), INPUT), 5)
    window = state["reactionWindow"]
    flow_before = state["breakdownFlow"]
    route_before = flow_before["reactorRoute"]
    p = irl.profile(state["firstActingSide"])
    require(state["stateVersion"] == 14 and state["currentPosition"]["kind"] == "reaction"
        and window is not None and len(window["frozenOpportunities"]) == 1
        and window["resolvedOpportunityIds"] == []
        and window["activeOpportunityId"] == window["frozenOpportunities"][0]["opportunityId"]
        and flow_before["kind"] == "reacting" and route_before is not None
        and flow_before["phasingContinuation"]["kind"] == "resume-route"
        and route_before["firstMoveStateVersion"] == 14
        and route_before["owner"] == p["reacting"]
        and route_before["originLocationId"] == p["reactingAssault"]
        and route_before["currentLocationId"] == p["reactingRear"]
        and route_before["cohortIds"] == [], 6)
    opportunity = window["frozenOpportunities"][0]
    frozen_rep = opportunity["reactingRepresentation"]
    element = next(item for item in state["world"]["elements"]
        if item["elementId"] == p["reactingElementId"])
    representation = next(item for item in state["world"]["representations"]
        if item["representationId"] == frozen_rep["representationId"])
    tracks = [item for item in state["tracks"]
        if item["unit"]["elementId"] == element["elementId"]]
    require(frozen_rep["currentLocationId"] == p["reactingAssault"]
        and frozen_rep["representationId"] == representation["representationId"]
        and frozen_rep["bindingKind"] == representation["bindingKind"]
        and frozen_rep["boundElementIds"] == representation["boundElementIds"]
        and route_before["elementId"] == element["elementId"]
        and route_before["representationId"] == representation["representationId"]
        and element["currentLocationId"] == representation["currentLocationId"] == p["reactingRear"]
        and element["operationalState"]["capabilityPointsExpended"] == irl.world.cp(2)
        and len(tracks) == 1 and tracks[0]["route"] == [p["reactingAssault"], p["reactingRear"]]
        and len(tracks[0]["route"]) < 512, 4)
    version = state["stateVersion"] + 1
    after = copy.deepcopy(state)
    route_after = copy.deepcopy(route_before)
    route_after["currentLocationId"] = p["reactingSupply"]
    flow_after = dict(kind="reacting",
        phasingContinuation=copy.deepcopy(flow_before["phasingContinuation"]),
        reactorRoute=route_after)
    sources = [dict(sourceId="spi-1979-map-a", locator="8.37")]
    common = dict(configurationHash=state["configurationHash"],
        creationBinding=state["creationBinding"], creationEventHash=state["creationEventHash"],
        cycleId=state["cycleId"], openingBaseHash=state["openingBaseHash"],
        completionReceiptId=state["completionReceiptId"], priorPrefix=state["prefix"],
        input=copy.deepcopy(inp))
    event = dict(contractVersion=3, eventType=EVENT, campaignId=state["campaignId"],
        stateVersion=version, priorStateVersion=state["stateVersion"],
        fromPositionId=state["sequencePosition"]["positionId"], gameTurn=1, operationStage=1,
        actingSide=p["reacting"], actionId=cmd["actionId"],
        submittedWindowId=cmd["windowId"], submittedOpportunityId=cmd["opportunityId"],
        windowId=window["reactionWindowId"], opportunityId=opportunity["opportunityId"],
        elementId=element["elementId"], representationId=representation["representationId"],
        originLocationId=p["reactingRear"], destinationLocationId=p["reactingSupply"],
        mobilityId="land.mobility.non-motorized", mobilitySources=sources,
        cost=dict(destinationTerrainId="land.terrain.clear",
            destinationTerrainCost=irl.world.cp(2), destinationTerrainSources=copy.deepcopy(sources),
            routeAdjustment=None, crossedHexsideCosts=[], totalCost=irl.world.cp(2)),
        capabilityPointsExpendedBefore=irl.world.cp(2),
        capabilityPointsExpendedAfter=irl.world.cp(4), cohesionBefore=0, cohesionAfter=0,
        reactionWindowAfter=copy.deepcopy(window), rulesetHash=state["rulesetHash"],
        breakdownAccounting=[], breakdownFlowAfter=flow_after, **common, receiptId="pending")
    moved = next(item for item in after["world"]["elements"]
        if item["elementId"] == element["elementId"])
    moved["currentLocationId"] = p["reactingSupply"]
    moved["operationalState"]["capabilityPointsExpended"] = irl.world.cp(4)
    next(item for item in after["world"]["representations"]
        if item["representationId"] == representation["representationId"])["currentLocationId"] = p["reactingSupply"]
    track_after = next(item for item in after["tracks"]
        if item["unit"]["elementId"] == element["elementId"])
    track_after["route"].append(p["reactingSupply"])
    after.update(reactionWindow=copy.deepcopy(window), breakdownFlow=copy.deepcopy(flow_after))
    event["receiptId"] = receipt(event)
    data = raw(event, TYPE)
    after["actualProgressRefs"].append(dict(eventType=EVENT,
        receiptId=event["receiptId"], eventHash=sha(data)))
    after.update(stateVersion=version,
        prefix=irl.imv.rd.pre.env.sequence.prefix_event(state["prefix"], data))
    after["receipts"].append(dict(commandHash=sha(raw(inp, INPUT)), eventHash=sha(data),
        receiptId=event["receiptId"], actor=inp["actor"], stateVersion=version))
    return canonical(after, STATE), data


def read_event(data):
    value = parse(data, TYPE)
    require(value["contractVersion"] == 3 and value["eventType"] == EVENT
        and value["input"]["command"]["kind"] == KIND
        and value["receiptId"] == receipt(value), 4)
    return value


def replay(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, participant_events, events):
    require(type(events) is list and len(events) <= INVENTORY["limits"]["secondMoveEvents"], 7)
    state = initial(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, participant_events)
    for data in events:
        event = read_event(data)
        state, expected = _emit(state, event["input"])
        require(data == expected, 6)
    return state


def apply(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, participant_events, events, inp):
    state = replay(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, participant_events, events)
    authorize(state, inp)
    for data in events:
        accepted = read_event(data)["input"]
        if accepted["command"]["expectedPriorVersion"] == inp["command"]["expectedPriorVersion"]:
            require(raw(inp, INPUT) == raw(accepted, INPUT), 6)
            return state, data, True
    after, data = _emit(state, inp)
    return after, data, False


def read_state(data, request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, participant_events, events):
    value = parse(data, STATE)
    expected = replay(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, participant_events, events)
    require(events and data == raw(expected, STATE), 6)
    return value


def source_trace(case):
    predecessor = next(item for item in json.loads(irl.FIXTURE.read_text())["cases"]
        if item["name"] == case["predecessorCase"])
    result = irl.trace(predecessor)
    require(irl.goldens(result) == predecessor["goldens"], 9)
    args, _, events, states = result
    return args + ([events[0]],), states[1]


def trace(case):
    args, before = source_trace(case)
    require(initial(*args) == before, 4)
    inp = command(before)
    after, event, duplicate = apply(*args, [], inp)
    require(not duplicate, 9)
    decoded = read_event(event)
    p = irl.profile(before["firstActingSide"])
    assert decoded["stateVersion"] == 15 and decoded["priorStateVersion"] == 14
    assert decoded["originLocationId"] == p["reactingRear"]
    assert decoded["destinationLocationId"] == p["reactingSupply"]
    assert decoded["submittedWindowId"] != decoded["windowId"]
    assert decoded["submittedOpportunityId"] != decoded["opportunityId"]
    assert decoded["submittedWindowId"] == irl.read_event(args[-1][0])["input"]["command"]["windowId"]
    assert decoded["submittedOpportunityId"] != irl.read_event(args[-1][0])["input"]["command"]["opportunityId"]
    assert decoded["capabilityPointsExpendedBefore"] == irl.world.cp(2)
    assert decoded["capabilityPointsExpendedAfter"] == irl.world.cp(4)
    assert decoded["reactionWindowAfter"] == before["reactionWindow"] == after["reactionWindow"]
    route_before = before["breakdownFlow"]["reactorRoute"]
    route_after = after["breakdownFlow"]["reactorRoute"]
    assert {key: value for key, value in route_after.items() if key != "currentLocationId"} == {
        key: value for key, value in route_before.items() if key != "currentLocationId"}
    assert route_after["currentLocationId"] == p["reactingSupply"]
    expected_world = copy.deepcopy(before["world"])
    next(item for item in expected_world["elements"]
        if item["elementId"] == p["reactingElementId"])["currentLocationId"] = p["reactingSupply"]
    next(item for item in expected_world["elements"]
        if item["elementId"] == p["reactingElementId"])["operationalState"]["capabilityPointsExpended"] = irl.world.cp(4)
    reacting_rep = route_before["representationId"]
    next(item for item in expected_world["representations"]
        if item["representationId"] == reacting_rep)["currentLocationId"] = p["reactingSupply"]
    assert after["world"] == expected_world
    expected_tracks = copy.deepcopy(before["tracks"])
    next(item for item in expected_tracks
        if item["unit"]["elementId"] == p["reactingElementId"])["route"].append(p["reactingSupply"])
    assert after["tracks"] == expected_tracks
    assert after["actualProgressRefs"][:-1] == before["actualProgressRefs"]
    assert after["actualProgressRefs"][-1] == dict(eventType=EVENT,
        receiptId=decoded["receiptId"], eventHash=sha(event))
    for key in ("currentPosition", "randomState", "initiativeHolder", "operationStageOrders",
            "operationStageWeather", "members", "cycle", "cycleId", "openingBaseHash",
            "completionReceiptId"):
        assert after[key] == before[key]
    assert after["breakdownFlow"]["phasingContinuation"] == before["breakdownFlow"]["phasingContinuation"]
    assert after["prefix"] == irl.imv.rd.pre.env.sequence.prefix_event(before["prefix"], event)
    assert after["receipts"][:-1] == before["receipts"]
    assert after["receipts"][-1] == dict(commandHash=sha(raw(inp, INPUT)), eventHash=sha(event),
        receiptId=decoded["receiptId"], actor=inp["actor"], stateVersion=15)
    assert read_state(raw(after, STATE), *args, [event]) == after
    retried, original, duplicate = apply(*args, [event], inp)
    assert duplicate and original == event and retried == after
    return args, [inp], [event], [before, after]


def goldens(result):
    args, inputs, events, states = result
    values = [("participant", args[-1][0]), ("input", raw(inputs[0], INPUT)),
        ("event", events[0]), ("state-0", raw(states[0], STATE)),
        ("state-1", raw(states[1], STATE))]
    return {name: dict(bytes=len(data), sha256=sha(data)) for name, data in values}


def rejected(fn):
    try:
        fn()
    except (Invalid, irl.Invalid):
        return
    raise AssertionError("expected CMB-IRS rejection")


def changed(value, path, replacement):
    result = copy.deepcopy(value)
    node = result
    for key in path[:-1]:
        node = node[key]
    node[path[-1]] = replacement
    return result


def different(value):
    if value is None:
        return "unexpected"
    if type(value) is bool:
        return not value
    if type(value) is int:
        return value + 1
    if type(value) is list:
        return ["unexpected"]
    if value.startswith("sha256:"):
        return "sha256:" + "f" * 64
    return value + "x"


def leaves(value, path=()):
    if type(value) is dict:
        for key, child in value.items():
            yield from leaves(child, path + (key,))
    elif type(value) is list:
        if not value:
            yield path, value
        for index, child in enumerate(value):
            yield from leaves(child, path + (index,))
    else:
        yield path, value


def action_id(cmd):
    action = {key: value for key, value in cmd.items() if key in (
        "kind", "windowId", "opportunityId", "originLocationId", "destinationLocationId")}
    action = {"contractVersion": 1, **action, "costBreakdown": irl.reaction_cost()}
    return sha(encode(action))


def verify(result):
    args, inputs, events, states = result
    inp, event = inputs[0], events[0]
    before, after = states
    counts = dict(cuts=2, retries=1, mutations=0, raw=0, boundaries=0)
    assert replay(*args, []) == before and replay(*args, events) == after
    for bad in ([event, event], [b"{}"]):
        rejected(lambda bad=bad: replay(*args, bad))
        counts["boundaries"] += 1
    for path, value in ((('actor',), before['firstActingSide']),
            (('command', 'creationBinding'), 'foreign.creation'),
            (('command', 'creationEventHash'), 'sha256:' + 'a' * 64),
            (('command', 'cycleId'), 'sha256:' + 'b' * 64),
            (('command', 'expectedPriorVersion'), 1),
            (('command', 'expectedPositionId'), 'foreign.position'),
            (('command', 'actionId'), 'sha256:' + 'd' * 64),
            (('command', 'originLocationId'), 'foreign.origin'),
            (('command', 'destinationLocationId'), 'foreign.destination')):
        rejected(lambda path=path, value=value: apply(*args, [], changed(inp, path, value)))
        counts["boundaries"] += 1
    predecessor_input = irl.read_event(args[-1][0])["input"]
    for field, value in (("windowId", before["reactionWindow"]["reactionWindowId"]),
            ("opportunityId", before["reactionWindow"]["activeOpportunityId"]),
            ("opportunityId", predecessor_input["command"]["opportunityId"])):
        bad = changed(inp, ("command", field), value)
        bad["command"]["actionId"] = action_id(bad["command"])
        rejected(lambda bad=bad: apply(*args, [], bad))
        counts["boundaries"] += 1
    shape_cases = []
    inactive = copy.deepcopy(before)
    inactive["reactionWindow"]["activeOpportunityId"] = None
    shape_cases.append(inactive)
    resolved = copy.deepcopy(before)
    resolved["reactionWindow"]["resolvedOpportunityIds"] = [resolved["reactionWindow"]["activeOpportunityId"]]
    resolved["reactionWindow"]["activeOpportunityId"] = None
    shape_cases.append(resolved)
    no_route = copy.deepcopy(before)
    no_route["breakdownFlow"]["reactorRoute"] = None
    shape_cases.append(no_route)
    multiple = copy.deepcopy(before)
    extra = copy.deepcopy(multiple["reactionWindow"]["frozenOpportunities"][0])
    extra["opportunityId"] = "sha256:" + "e" * 64
    multiple["reactionWindow"]["frozenOpportunities"].append(extra)
    shape_cases.append(multiple)
    wrong_route = copy.deepcopy(before)
    wrong_route["breakdownFlow"]["reactorRoute"]["currentLocationId"] = "foreign.location"
    shape_cases.append(wrong_route)
    wrong_version = copy.deepcopy(before)
    wrong_version["breakdownFlow"]["reactorRoute"]["firstMoveStateVersion"] = 13
    shape_cases.append(wrong_version)
    no_track = copy.deepcopy(before)
    no_track["tracks"] = [item for item in no_track["tracks"]
        if item["unit"]["elementId"] != irl.profile(before["firstActingSide"])["reactingElementId"]]
    shape_cases.append(no_track)
    for bad_state in shape_cases:
        rejected(lambda bad_state=bad_state: _emit(bad_state, inp))
        counts["boundaries"] += 1
    overflow = copy.deepcopy(before)
    overflow["stateVersion"] = 2**63 - 1
    overflow_input = changed(inp, ("command", "expectedPriorVersion"), 2**63 - 1)
    rejected(lambda: _emit(overflow, overflow_input))
    counts["boundaries"] += 1
    capacity = copy.deepcopy(before)
    capacity["receipts"] += [copy.deepcopy(capacity["receipts"][-1])
        for _ in range(512 - len(capacity["receipts"]))]
    rejected(lambda: _emit(capacity, inp))
    counts["boundaries"] += 1
    decoded = read_event(event)
    for path, value in leaves(decoded):
        altered = changed(decoded, path, different(value))
        try:
            if path != ("receiptId",):
                altered["receiptId"] = receipt(altered)
            data = raw(altered, TYPE)
        except (Invalid, KeyError, TypeError):
            data = encode(altered)
        rejected(lambda data=data: replay(*args, [data]))
        counts["mutations"] += 1
    for path in (("prefix",), ("stateVersion",), ("breakdownFlow", "kind"),
            ("breakdownFlow", "reactorRoute", "routeId"),
            ("world", "elements", 0, "currentLocationId"),
            ("randomState", "nextByteCursor"), ("tracks", 0, "route"),
            ("actualProgressRefs", -1, "eventHash")):
        node = after
        for key in path:
            node = node[key]
        altered = changed(after, path, different(node))
        try:
            data = raw(altered, STATE)
        except Invalid:
            data = encode(altered)
        rejected(lambda data=data: read_state(data, *args, events))
        counts["mutations"] += 1
    samples = [(event, TYPE), (raw(inp, INPUT), INPUT), (raw(after, STATE), STATE)]
    for data, kind in samples:
        value = json.loads(data)
        version = value.get("contractVersion", value.get("command", {}).get("contractVersion", 2))
        variants = [data + b" ", b" " + data, b"\xef\xbb\xbf" + data, b"\xff", data[:-1],
            b"{}", b'{"unknown":0,' + data[1:], encode(dict(reversed(list(value.items())))),
            data.replace(b'"contractVersion":', b'"contractVersion": ', 1),
            data.replace(b'"contractVersion":' + str(version).encode(), b'"contractVersion":true', 1)]
        for malformed in variants:
            if kind == STATE:
                rejected(lambda malformed=malformed: read_state(malformed, *args, events))
            else:
                rejected(lambda malformed=malformed, kind=kind: parse(malformed, kind))
            counts["raw"] += 1
    return counts


def fixture_shape(fixture):
    require(set(fixture) == {"contractVersion", "contract", "sourceHashes", "cases"}
        and fixture["contractVersion"] == 1
        and fixture["contract"] == "combat-inherited-reaction-second-move-v1", 9)
    actual = [(case["name"].split("-active-", 1)[0], case["actor"])
        for case in fixture["cases"]]
    require(actual == [("axis", "axis"), ("commonwealth", "commonwealth")]
        and set(fixture["sourceHashes"]) == SOURCE_PATHS, 9)


def generated_fixture():
    predecessor = json.loads(irl.FIXTURE.read_text())
    repo = ROOT.parent.parent
    cases = []
    for source in predecessor["cases"]:
        side = source["name"].split("-", 1)[0]
        case = dict(name=f"{side}-active-second-move", predecessorCase=source["name"], actor=side)
        case["goldens"] = goldens(trace(case))
        cases.append(case)
    return dict(contractVersion=1, contract="combat-inherited-reaction-second-move-v1",
        sourceHashes={path: sha((repo / path).read_bytes()) for path in sorted(SOURCE_PATHS)},
        cases=cases)


def main():
    if sys.argv[1:] == ["--generate-fixture"]:
        print(json.dumps(generated_fixture(), indent=2))
        return
    fixture = json.loads(FIXTURE.read_text())
    fixture_shape(fixture)
    repo = ROOT.parent.parent
    for path, digest in fixture["sourceHashes"].items():
        require(sha((repo / path).read_bytes()) == digest, 9)
    results = []
    totals = dict(cuts=0, retries=0, mutations=0, raw=0, boundaries=0)
    for case in fixture["cases"]:
        result = trace(case)
        results.append(result)
        require(goldens(result) == case["goldens"], 9)
        for key, value in verify(result).items():
            totals[key] += value
    for index, result in enumerate(results):
        foreign = results[1 - index]
        rejected(lambda result=result, foreign=foreign: replay(*foreign[0], result[2]))
        totals["boundaries"] += 1
    print("CMB-IRS PASS", json.dumps(dict(traces=2, events=2,
        sourcePins=len(SOURCE_PATHS), **totals), sort_keys=True))


if __name__ == "__main__":
    main()
