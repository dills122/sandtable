#!/usr/bin/env python3
"""Creation-rooted inherited active Reaction fallback contract oracle."""
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
INVENTORY = json.loads((ROOT / "combat-inherited-reaction-active-fallback-v1.schema.json").read_text())
FIXTURE = ROOT / "fixtures/combat-inherited-reaction-active-fallback-v1.json"
LOCAL = {key: [tuple(field.split(":")) for field in value.split()]
    for key, value in INVENTORY["objects"].items()}
SCHEMA = irl.SCHEMA | LOCAL
CLOSE_KINDS = list(INVENTORY["commandReasons"])
RESOLVE_KIND = "resolve-breakdown-stop"
KINDS = CLOSE_KINDS + [RESOLVE_KIND]
EVENTS = ["reaction-window-closed", "breakdown-stop-resolved"]
TYPES = ["FallbackCloseEvent", "FallbackResolveEvent"]
VERSIONS = [3, 2]
DOMAIN_KEYS = ["closeReceipt", "resolveReceipt"]
SOURCE_PATHS = {
    "docs/design/zoc-reaction-v1.md",
    "docs/specs/zoc-reaction-v1.md",
    "docs/specs/combat-inherited-reaction-active-fallback-v1.schema.json",
    "docs/specs/combat-inherited-reaction-closure-v1.schema.json",
    "docs/specs/verify-combat-inherited-reaction-closure-v1.py",
    "docs/specs/combat-inherited-reaction-lifecycle-v1.md",
    "docs/specs/combat-inherited-reaction-lifecycle-v1.schema.json",
    "docs/specs/fixtures/combat-inherited-reaction-lifecycle-v1.json",
    "docs/specs/verify-combat-inherited-reaction-lifecycle-v1.py",
    "src/Cna.Core/Actions/CampaignObservationV6ActionCandidates.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownLifecycleCodec.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownLifecycleEvents.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownLifecycleFactory.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownStopResolvedCodec.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownStopResolvedFactory.cs",
    "src/Cna.Core/Campaigns/CampaignV11BreakdownProjector.cs",
    "src/Cna.Core/Observations/CampaignObservationV6DisclosureIdentity.cs",
}


class Invalid(ValueError):
    def __init__(self, code):
        self.code = f"CMB-IRF-{code:03}"
        super().__init__(self.code)


def require(ok, code):
    if not ok:
        raise Invalid(code)


def variant(value, union):
    require(type(value) is dict and type(value.get("kind")) is str, 1)
    require(value["kind"] in INVENTORY["unions"][union], 3)
    return INVENTORY["unions"][union][value["kind"]]


def typed(value, kind, depth=0):
    require(depth <= INVENTORY["limits"]["depth"], 1)
    if kind == "null":
        require(value is None, 3)
    elif kind == "empty":
        require(type(value) is list and not value, 3)
    elif kind in INVENTORY["unions"]:
        typed(value, variant(value, kind), depth)
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
    if kind in INVENTORY["unions"]:
        return canonical(value, variant(value, kind))
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
        moved = irl.read_event(participant_events[0])
    except (ValueError, KeyError, TypeError) as error:
        raise Invalid(4) from error
    window = state["reactionWindow"]
    require(moved["eventType"] == "reacting-element-moved" and state["stateVersion"] == 14
        and state["currentPosition"]["kind"] == "reaction" and window is not None
        and len(window["frozenOpportunities"]) == 1
        and window["resolvedOpportunityIds"] == []
        and window["activeOpportunityId"] == window["frozenOpportunities"][0]["opportunityId"]
        and state["breakdownFlow"]["kind"] == "reacting"
        and state["breakdownFlow"]["reactorRoute"] is not None
        and state["breakdownFlow"]["phasingContinuation"]["kind"] == "resume-route", 4)
    return canonical(state, "FallbackState")


def public_window(state):
    return irl.public_window(state)


def public_stop(state):
    return irl.identity(INVENTORY["domains"]["stopCapability"], dict(
        campaignId=state["campaignId"], rulesetHash=state["rulesetHash"],
        stateVersion=state["stateVersion"], audience="system"))


def command(state, kind):
    require(kind in KINDS, 3)
    action = dict(contractVersion=1, kind=kind)
    cmd = dict(contractVersion=2, kind=kind)
    if kind in CLOSE_KINDS:
        window = state["reactionWindow"]
        require(state["currentPosition"]["kind"] == "reaction" and window is not None
            and len(window["frozenOpportunities"]) == 1
            and window["activeOpportunityId"] == window["frozenOpportunities"][0]["opportunityId"]
            and window["resolvedOpportunityIds"] == []
            and state["breakdownFlow"]["kind"] == "reacting"
            and state["breakdownFlow"]["reactorRoute"] is not None, 6)
        action["windowId"] = public_window(state)
        cmd["windowId"] = action["windowId"]
    else:
        require(state["currentPosition"]["kind"] == "breakdown-stop"
            and state["reactionWindow"] is None
            and state["breakdownFlow"]["kind"] == "reactor-stop-closed", 6)
        action["stopId"] = public_stop(state)
        cmd["stopId"] = action["stopId"]
    cmd.update(actionId=sha(encode(action)), creationBinding=state["creationBinding"],
        creationEventHash=state["creationEventHash"], cycleId=state["cycleId"],
        expectedPriorVersion=state["stateVersion"],
        expectedPositionId=state["sequencePosition"]["positionId"])
    return dict(command=cmd, actor="system")


def authorize(state, inp):
    typed(inp, "FallbackInput")
    cmd = inp["command"]
    require(cmd["contractVersion"] == 2 and cmd["kind"] in KINDS, 3)
    require(inp["actor"] == "system", 5)
    require(cmd["creationBinding"] == state["creationBinding"]
        and cmd["creationEventHash"] == state["creationEventHash"]
        and cmd["cycleId"] == state["cycleId"], 4)


def receipt(event, index):
    unsigned = {key: value for key, value in canonical(event, TYPES[index]).items()
        if key != "receiptId"}
    digest = hashlib.sha256(INVENTORY["domains"][DOMAIN_KEYS[index]].encode()
        + b"\0" + encode(unsigned)).hexdigest()
    return ("irl." if index == 0 else "iml.") + digest


def commit(state, after, inp, event, index):
    event["receiptId"] = receipt(event, index)
    data = raw(event, TYPES[index])
    after.update(stateVersion=event["stateVersion"],
        prefix=irl.imv.rd.pre.env.sequence.prefix_event(state["prefix"], data))
    after["receipts"].append(dict(commandHash=sha(raw(inp, "FallbackInput")),
        eventHash=sha(data), receiptId=event["receiptId"], actor=inp["actor"],
        stateVersion=event["stateVersion"]))
    return canonical(after, "FallbackState"), data


def _emit(state, inp):
    authorize(state, inp)
    cmd = inp["command"]
    kind = cmd["kind"]
    require(cmd["expectedPriorVersion"] == state["stateVersion"]
        and cmd["expectedPositionId"] == state["sequencePosition"]["positionId"], 6)
    require(state["stateVersion"] < 2**63 - 1 and len(state["receipts"]) < 512, 7)
    require(raw(inp, "FallbackInput") == raw(command(state, kind), "FallbackInput"), 5)
    version = state["stateVersion"] + 1
    after = copy.deepcopy(state)
    common = dict(configurationHash=state["configurationHash"],
        creationBinding=state["creationBinding"], creationEventHash=state["creationEventHash"],
        cycleId=state["cycleId"], openingBaseHash=state["openingBaseHash"],
        completionReceiptId=state["completionReceiptId"], priorPrefix=state["prefix"],
        input=copy.deepcopy(inp))
    if kind in CLOSE_KINDS:
        window = state["reactionWindow"]
        require(state["stateVersion"] == 14 and state["currentPosition"]["kind"] == "reaction"
            and window is not None and len(window["frozenOpportunities"]) == 1
            and window["resolvedOpportunityIds"] == []
            and window["activeOpportunityId"] == window["frozenOpportunities"][0]["opportunityId"]
            and state["breakdownFlow"]["kind"] == "reacting"
            and state["breakdownFlow"]["reactorRoute"] is not None
            and state["breakdownFlow"]["phasingContinuation"]["kind"] == "resume-route", 6)
        stop = dict(stopId="pending", recordedStateVersion=version,
            route=copy.deepcopy(state["breakdownFlow"]["reactorRoute"]),
            reason=INVENTORY["stopReasons"][kind], weatherKind="normal", cohortInputs=[])
        stop_identity = dict(domain=INVENTORY["domains"]["stop"],
            campaignId=state["campaignId"], rulesetHash=state["rulesetHash"])
        stop_identity.update({key: value for key, value in stop.items() if key != "stopId"})
        stop["stopId"] = sha(encode(stop_identity))
        flow = dict(kind="reactor-stop-closed",
            phasingContinuation=copy.deepcopy(state["breakdownFlow"]["phasingContinuation"]),
            stop=stop)
        event = dict(contractVersion=VERSIONS[0], eventType=EVENTS[0],
            campaignId=state["campaignId"], stateVersion=version,
            priorStateVersion=state["stateVersion"],
            fromPositionId=state["sequencePosition"]["positionId"], actingSide=None,
            actionId=cmd["actionId"], submittedWindowId=cmd["windowId"],
            windowId=window["reactionWindowId"], reason=INVENTORY["commandReasons"][kind],
            closedOpportunityIds=[window["activeOpportunityId"]],
            suspendedSequencePosition=copy.deepcopy(state["sequencePosition"]),
            rulesetHash=state["rulesetHash"], breakdownFlowAfter=flow, **common,
            receiptId="pending")
        after.update(currentPosition=dict(kind="breakdown-stop",
            sequencePosition=copy.deepcopy(state["sequencePosition"])),
            reactionWindow=None, breakdownFlow=copy.deepcopy(flow))
        return commit(state, after, inp, event, 0)
    require(kind == RESOLVE_KIND and state["stateVersion"] == 15
        and state["currentPosition"]["kind"] == "breakdown-stop"
        and state["reactionWindow"] is None
        and state["breakdownFlow"]["kind"] == "reactor-stop-closed", 6)
    stop = copy.deepcopy(state["breakdownFlow"]["stop"])
    flow = dict(kind="moving",
        route=copy.deepcopy(state["breakdownFlow"]["phasingContinuation"]["route"]))
    sources = [dict(sourceId="spi-1979-land-rules", locator="21.24-21.26")]
    event = dict(contractVersion=VERSIONS[1], eventType=EVENTS[1],
        campaignId=state["campaignId"], stateVersion=version,
        priorStateVersion=state["stateVersion"], rulesetHash=state["rulesetHash"],
        fromPositionId="land.position.breakdown-stop", actionId=cmd["actionId"], stop=stop,
        randomStateBefore=copy.deepcopy(state["randomState"]), checks=[], createdLots=[],
        randomStateAfter=copy.deepcopy(state["randomState"]), breakdownFlowAfter=flow,
        sources=sources, **common, sequencePosition=copy.deepcopy(state["sequencePosition"]),
        interruptContextAfter=None, receiptId="pending")
    after.update(currentPosition=dict(kind="sequence",
        sequencePosition=copy.deepcopy(state["sequencePosition"])),
        reactionWindow=None, breakdownFlow=copy.deepcopy(flow))
    return commit(state, after, inp, event, 1)


def read_event(data):
    require(type(data) is bytes and data, 1)
    try:
        value = json.loads(data)
    except (ValueError, UnicodeError, RecursionError) as error:
        raise Invalid(1) from error
    require(type(value) is dict and value.get("eventType") in EVENTS, 3)
    index = EVENTS.index(value["eventType"])
    value = parse(data, TYPES[index])
    kind = value["input"]["command"]["kind"]
    require(value["contractVersion"] == VERSIONS[index]
        and (kind == RESOLVE_KIND if index == 1 else kind in CLOSE_KINDS), 3)
    if index == 0:
        require(value["actingSide"] is None
            and value["reason"] == INVENTORY["commandReasons"][kind]
            and value["breakdownFlowAfter"]["stop"]["reason"]
                == INVENTORY["stopReasons"][kind], 4)
    require(value["receiptId"] == receipt(value, index), 4)
    return value


def replay(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, participant_events, events):
    require(type(events) is list and len(events) <= INVENTORY["limits"]["fallbackEvents"], 7)
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
            require(raw(inp, "FallbackInput") == raw(accepted, "FallbackInput"), 6)
            return state, data, True
    after, data = _emit(state, inp)
    return after, data, False


def read_state(data, request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, participant_events, events):
    value = parse(data, "FallbackState")
    expected = replay(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, participant_events, events)
    require(events and data == raw(expected, "FallbackState"), 6)
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
    state = initial(*args)
    require(state == before, 4)
    states = [state]
    inputs = []
    events = []
    for index, kind in enumerate((case["kind"], RESOLVE_KIND)):
        prior = copy.deepcopy(state)
        inp = command(state, kind)
        state, event, duplicate = apply(*args, events, inp)
        require(not duplicate, 9)
        decoded = read_event(event)
        inputs.append(inp)
        events.append(event)
        states.append(state)
        assert decoded["stateVersion"] == 15 + index
        assert decoded["priorStateVersion"] == 14 + index
        assert state["prefix"] == irl.imv.rd.pre.env.sequence.prefix_event(prior["prefix"], event)
        assert state["receipts"][-1] == dict(commandHash=sha(raw(inp, "FallbackInput")),
            eventHash=sha(event), receiptId=decoded["receiptId"], actor="system",
            stateVersion=15 + index)
        if index == 0:
            assert decoded["actingSide"] is None and decoded["reason"] == case["reason"]
            assert decoded["submittedWindowId"] != decoded["windowId"]
            assert decoded["closedOpportunityIds"] == [before["reactionWindow"]["activeOpportunityId"]]
            assert decoded["suspendedSequencePosition"] == before["sequencePosition"]
            stop = decoded["breakdownFlowAfter"]["stop"]
            assert (stop["recordedStateVersion"], stop["reason"], stop["weatherKind"],
                stop["cohortInputs"]) == (15, case["stopReason"], "normal", [])
            assert stop["route"] == before["breakdownFlow"]["reactorRoute"]
            assert state["reactionWindow"] is None
            assert state["currentPosition"]["kind"] == "breakdown-stop"
            assert state["breakdownFlow"] == decoded["breakdownFlowAfter"]
        else:
            assert decoded["stop"] == read_event(events[0])["breakdownFlowAfter"]["stop"]
            assert inp["command"]["stopId"] != decoded["stop"]["stopId"]
            assert decoded["checks"] == decoded["createdLots"] == []
            assert decoded["randomStateBefore"] == decoded["randomStateAfter"] == prior["randomState"]
            assert decoded["sources"] == [dict(sourceId="spi-1979-land-rules", locator="21.24-21.26")]
            assert state["reactionWindow"] is None
            assert state["currentPosition"] == dict(kind="sequence",
                sequencePosition=before["sequencePosition"])
            assert state["breakdownFlow"] == dict(kind="moving",
                route=before["breakdownFlow"]["phasingContinuation"]["route"])
        for key in ("world", "randomState", "initiativeHolder", "operationStageOrders",
                "operationStageWeather", "members", "cycle", "cycleId", "openingBaseHash",
                "completionReceiptId", "tracks", "actualProgressRefs"):
            assert state[key] == before[key]
        assert state["receipts"][:len(before["receipts"])] == before["receipts"]
        assert read_state(raw(state, "FallbackState"), *args, events) == state
    p = irl.profile(before["firstActingSide"])
    reactor = next(item for item in state["world"]["elements"]
        if item["elementId"] == p["reactingElementId"])
    phasing = next(item for item in state["world"]["elements"] if item["elementId"] == p["elementId"])
    assert reactor["currentLocationId"] == p["reactingRear"]
    assert reactor["operationalState"]["capabilityPointsExpended"] == irl.world.cp(2)
    assert phasing["currentLocationId"] == p["assault"]
    assert phasing["operationalState"]["capabilityPointsExpended"] == irl.world.cp(4)
    for index, inp in enumerate(inputs):
        retried, original, duplicate = apply(*args, events, inp)
        assert duplicate and original == events[index] and retried == state
    return args, inputs, events, states


def goldens(result):
    args, inputs, events, states = result
    values = [("participant", args[-1][0])]
    values += [(f"input-{index + 1}", raw(value, "FallbackInput"))
        for index, value in enumerate(inputs)]
    values += [(f"event-{index + 1}", value) for index, value in enumerate(events)]
    values += [(f"state-{index}", raw(value, "FallbackState"))
        for index, value in enumerate(states)]
    return {name: dict(bytes=len(data), sha256=sha(data)) for name, data in values}


def rejected(fn):
    try:
        fn()
    except (Invalid, irl.Invalid):
        return
    raise AssertionError("expected CMB-IRF rejection")


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


def verify(result):
    args, inputs, events, states = result
    before, closed, after = states
    counts = dict(cuts=3, retries=2, mutations=0, raw=0, boundaries=0)
    assert replay(*args, []) == before
    assert replay(*args, events[:1]) == closed
    assert replay(*args, events) == after
    for bad in ([events[1]], events + [events[1]], [events[0], events[0]], events + [b"{}"]):
        rejected(lambda bad=bad: replay(*args, bad))
        counts["boundaries"] += 1
    for index, inp in enumerate(inputs):
        state = states[index]
        for path, value in ((('actor',), state["firstActingSide"]),
                (('command', 'creationBinding'), 'foreign.creation'),
                (('command', 'creationEventHash'), 'sha256:' + 'a' * 64),
                (('command', 'cycleId'), 'sha256:' + 'b' * 64),
                (('command', 'expectedPriorVersion'), 1),
                (('command', 'expectedPositionId'), 'foreign.position'),
                (('command', 'actionId'), 'sha256:' + 'd' * 64)):
            bad = changed(inp, path, value)
            rejected(lambda bad=bad, index=index: apply(*args, events[:index], bad))
            counts["boundaries"] += 1
    authority = before["reactionWindow"]["reactionWindowId"]
    bad = changed(inputs[0], ("command", "windowId"), authority)
    bad["command"]["actionId"] = sha(encode(dict(contractVersion=1,
        kind=bad["command"]["kind"], windowId=authority)))
    rejected(lambda: apply(*args, [], bad))
    counts["boundaries"] += 1
    stop_authority = closed["breakdownFlow"]["stop"]["stopId"]
    bad = changed(inputs[1], ("command", "stopId"), stop_authority)
    bad["command"]["actionId"] = sha(encode(dict(contractVersion=1,
        kind=RESOLVE_KIND, stopId=stop_authority)))
    rejected(lambda: apply(*args, events[:1], bad))
    counts["boundaries"] += 1
    alternate = command(before, next(kind for kind in CLOSE_KINDS
        if kind != inputs[0]["command"]["kind"]))
    rejected(lambda: apply(*args, events, alternate))
    counts["boundaries"] += 1
    for forbidden in ("decline-reaction-window", "close-reaction-window-no-eligible-reactor"):
        bad = changed(inputs[0], ("command", "kind"), forbidden)
        rejected(lambda bad=bad: authorize(before, bad))
        counts["boundaries"] += 1
    rejected(lambda: initial(*args[:-1], []))
    rejected(lambda: initial(*args[:-1], args[-1] * 2))
    counts["boundaries"] += 2
    shape_cases = []
    inactive = copy.deepcopy(before)
    inactive["reactionWindow"]["activeOpportunityId"] = None
    shape_cases.append(inactive)
    completed = copy.deepcopy(before)
    completed["reactionWindow"]["resolvedOpportunityIds"] = [
        completed["reactionWindow"]["activeOpportunityId"]]
    completed["reactionWindow"]["activeOpportunityId"] = None
    completed["breakdownFlow"]["reactorRoute"] = None
    shape_cases.append(completed)
    no_route = copy.deepcopy(before)
    no_route["breakdownFlow"]["reactorRoute"] = None
    shape_cases.append(no_route)
    no_window = copy.deepcopy(before)
    no_window["reactionWindow"] = None
    shape_cases.append(no_window)
    multiple = copy.deepcopy(before)
    extra = copy.deepcopy(multiple["reactionWindow"]["frozenOpportunities"][0])
    extra["opportunityId"] = "sha256:" + "e" * 64
    multiple["reactionWindow"]["frozenOpportunities"].append(extra)
    shape_cases.append(multiple)
    for bad_state in shape_cases:
        rejected(lambda bad_state=bad_state: _emit(bad_state, inputs[0]))
        counts["boundaries"] += 1
    overflow = copy.deepcopy(before)
    overflow["stateVersion"] = 2**63 - 1
    overflow_input = changed(inputs[0], ("command", "expectedPriorVersion"), 2**63 - 1)
    rejected(lambda: _emit(overflow, overflow_input))
    counts["boundaries"] += 1
    capacity = copy.deepcopy(before)
    capacity["receipts"] += [copy.deepcopy(capacity["receipts"][-1])
        for _ in range(512 - len(capacity["receipts"]))]
    rejected(lambda: _emit(capacity, inputs[0]))
    counts["boundaries"] += 1
    for event_index, event in enumerate(events):
        decoded = read_event(event)
        for path, value in leaves(decoded):
            altered = changed(decoded, path, different(value))
            try:
                if path != ("receiptId",):
                    altered["receiptId"] = receipt(altered, event_index)
                data = raw(altered, TYPES[event_index])
            except (Invalid, KeyError, TypeError):
                data = encode(altered)
            candidate = events[:event_index] + [data]
            rejected(lambda candidate=candidate: replay(*args, candidate))
            counts["mutations"] += 1
    for path in (("prefix",), ("stateVersion",), ("breakdownFlow", "kind"),
            ("world", "elements", 0, "currentLocationId"),
            ("randomState", "nextByteCursor"), ("actualProgressRefs", 0, "eventHash")):
        node = after
        for key in path:
            node = node[key]
        altered = changed(after, path, different(node))
        try:
            data = raw(altered, "FallbackState")
        except Invalid:
            data = encode(altered)
        rejected(lambda data=data: read_state(data, *args, events))
        counts["mutations"] += 1
    samples = [(events[index], TYPES[index]) for index in range(2)]
    samples += [(raw(value, "FallbackInput"), "FallbackInput") for value in inputs]
    samples += [(raw(after, "FallbackState"), "FallbackState")]
    for data, kind in samples:
        value = json.loads(data)
        version = value.get("contractVersion", value.get("command", {}).get("contractVersion", 2))
        variants = [data + b" ", b" " + data, b"\xef\xbb\xbf" + data, b"\xff", data[:-1], b"{}",
            b'{"unknown":0,' + data[1:], encode(dict(reversed(list(value.items())))),
            data.replace(b'"contractVersion":', b'"contractVersion": ', 1),
            data.replace(b'"contractVersion":' + str(version).encode(), b'"contractVersion":true', 1)]
        for malformed in variants:
            if kind == "FallbackState":
                rejected(lambda malformed=malformed: read_state(malformed, *args, events))
            else:
                rejected(lambda malformed=malformed, kind=kind: parse(malformed, kind))
            counts["raw"] += 1
    return counts


def fixture_shape(fixture):
    require(set(fixture) == {"contractVersion", "contract", "sourceHashes", "cases"}
        and fixture["contractVersion"] == 1
        and fixture["contract"] == "combat-inherited-reaction-active-fallback-v1", 9)
    expected = [(side, kind, INVENTORY["commandReasons"][kind],
        INVENTORY["stopReasons"][kind])
        for side in ("axis", "commonwealth") for kind in CLOSE_KINDS]
    actual = [(case["name"].split("-active-", 1)[0], case["kind"], case["reason"],
        case["stopReason"]) for case in fixture["cases"]]
    require(actual == expected and set(fixture["sourceHashes"]) == SOURCE_PATHS, 9)


def generated_fixture():
    predecessor = json.loads(irl.FIXTURE.read_text())
    repo = ROOT.parent.parent
    cases = []
    for source in predecessor["cases"]:
        side = source["name"].split("-", 1)[0]
        for kind in CLOSE_KINDS:
            reason = INVENTORY["commandReasons"][kind]
            case = dict(name=f"{side}-active-{reason}", predecessorCase=source["name"],
                kind=kind, reason=reason, stopReason=INVENTORY["stopReasons"][kind])
            case["goldens"] = goldens(trace(case))
            cases.append(case)
    return dict(contractVersion=1, contract="combat-inherited-reaction-active-fallback-v1",
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
        results.append((case, result))
        require(goldens(result) == case["goldens"], 9)
        for key, value in verify(result).items():
            totals[key] += value
    for case, result in results:
        foreign = next(other for other_case, other in results
            if other_case["kind"] == case["kind"]
            and other_case["name"].split("-", 1)[0] != case["name"].split("-", 1)[0])
        rejected(lambda result=result, foreign=foreign: replay(*foreign[0], result[2]))
        totals["boundaries"] += 1
    print("CMB-IRF PASS", json.dumps(dict(traces=len(results), events=len(results) * 2,
        sourcePins=len(SOURCE_PATHS), **totals), sort_keys=True))


if __name__ == "__main__":
    main()
