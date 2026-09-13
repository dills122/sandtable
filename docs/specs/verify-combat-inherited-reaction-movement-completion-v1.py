#!/usr/bin/env python3
"""Creation-rooted inherited Reaction movement-completion oracle; no runtime admission or writes."""
import copy
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
irs = load("inherited_reaction_second_move", "verify-combat-inherited-reaction-second-move-v1.py")
encode, sha = irl.encode, irl.sha
INVENTORY = json.loads(
    (ROOT / "combat-inherited-reaction-movement-completion-v1.schema.json").read_text())
FIXTURE = ROOT / "fixtures/combat-inherited-reaction-movement-completion-v1.json"
KINDS = irl.KINDS[1:]
EVENTS = irl.EVENTS[1:]
TYPES = irl.TYPES[1:]
VERSIONS = irl.VERSIONS[1:]
SOURCE_PATHS = irs.SOURCE_PATHS | {
    "docs/specs/combat-inherited-reaction-movement-completion-v1.schema.json",
    "docs/specs/combat-inherited-reaction-second-move-v1.md",
    "docs/specs/combat-inherited-reaction-second-move-v1.schema.json",
    "docs/specs/fixtures/combat-inherited-reaction-second-move-v1.json",
    "docs/specs/verify-combat-inherited-reaction-second-move-v1.py",
}
_INITIAL_CACHE = {}


class Invalid(ValueError):
    def __init__(self, code):
        self.code = f"CMB-IRMC-{code:03}"
        super().__init__(self.code)


def require(ok, code):
    if not ok:
        raise Invalid(code)


def initial(case):
    cache_key = case["predecessorCase"]
    if cache_key in _INITIAL_CACHE:
        state, predecessor_input, predecessor_event = _INITIAL_CACHE[cache_key]
        return copy.deepcopy(state), copy.deepcopy(predecessor_input), predecessor_event
    fixture = json.loads(irs.FIXTURE.read_text())
    predecessor = next(item for item in fixture["cases"]
        if item["name"] == case["predecessorCase"])
    result = irs.trace(predecessor)
    require(irs.goldens(result) == predecessor["goldens"], 9)
    _, inputs, events, states = result
    state = irl.canonical(states[-1], "LifecycleState")
    require(irl.raw(state, "LifecycleState") == irs.raw(states[-1], irs.STATE), 4)
    window = state["reactionWindow"]
    route = state["breakdownFlow"]["reactorRoute"]
    p = irl.profile(state["firstActingSide"])
    element = next(item for item in state["world"]["elements"]
        if item["elementId"] == p["reactingElementId"])
    track = next(item for item in state["tracks"]
        if item["unit"]["elementId"] == p["reactingElementId"])
    require(case["actor"] == state["firstActingSide"], 5)
    require(state["stateVersion"] == 15 and state["currentPosition"]["kind"] == "reaction"
        and window is not None and len(window["frozenOpportunities"]) == 1
        and window["resolvedOpportunityIds"] == []
        and window["activeOpportunityId"] == window["frozenOpportunities"][0]["opportunityId"]
        and state["breakdownFlow"]["kind"] == "reacting" and route is not None
        and route["firstMoveStateVersion"] == 14 and route["owner"] == p["reacting"]
        and route["originLocationId"] == p["reactingAssault"]
        and route["currentLocationId"] == p["reactingSupply"] and route["cohortIds"] == []
        and element["currentLocationId"] == p["reactingSupply"]
        and element["operationalState"]["capabilityPointsExpended"] == irl.world.cp(4)
        and track["route"] == [p["reactingAssault"], p["reactingRear"], p["reactingSupply"]]
        and irl.move_options(state) == [], 4)
    _INITIAL_CACHE[cache_key] = (copy.deepcopy(state), copy.deepcopy(inputs[0]), events[0])
    return state, inputs[0], events[0]


def command(state, kind):
    require(kind in KINDS, 3)
    try:
        return irl.command(state, kind)
    except (irl.Invalid, KeyError, TypeError) as error:
        raise Invalid(6) from error


def read_event(data):
    try:
        event = irl.read_event(data)
    except (irl.Invalid, KeyError, TypeError) as error:
        raise Invalid(4) from error
    require(event["eventType"] in EVENTS
        and event["input"]["command"]["kind"] in KINDS, 3)
    return event


def emit(state, inp):
    try:
        irl.typed(inp, "LifecycleInput")
        require(inp["command"]["kind"] in KINDS, 3)
        return irl._emit(state, inp)
    except (irl.Invalid, KeyError, TypeError) as error:
        raise Invalid(6) from error


def replay(case, events):
    require(type(events) is list and len(events) <= INVENTORY["limits"]["eventsPerCase"], 7)
    state, _, _ = initial(case)
    for data in events:
        event = read_event(data)
        state, expected = emit(state, event["input"])
        require(data == expected, 6)
    return state


def apply(case, events, inp):
    state = replay(case, events)
    try:
        irl.authorize(state, inp)
    except (irl.Invalid, KeyError, TypeError) as error:
        raise Invalid(5) from error
    require(inp["command"]["kind"] in KINDS, 3)
    for data in events:
        accepted = read_event(data)["input"]
        if accepted["command"]["expectedPriorVersion"] == inp["command"]["expectedPriorVersion"]:
            require(irl.raw(inp, "LifecycleInput") == irl.raw(accepted, "LifecycleInput"), 6)
            return state, data, True
    after, data = emit(state, inp)
    return after, data, False


def read_state(data, case, events):
    try:
        value = irl.parse(data, "LifecycleState")
    except (irl.Invalid, KeyError, TypeError) as error:
        raise Invalid(1) from error
    expected = replay(case, events)
    require(data == irl.raw(expected, "LifecycleState"), 6)
    return value


def trace(case):
    before, predecessor_input, predecessor_event = initial(case)
    state = before
    states = [state]
    inputs = []
    events = []
    p = irl.profile(before["firstActingSide"])
    baseline = copy.deepcopy(before)
    for offset, kind in enumerate(KINDS, 1):
        prior = copy.deepcopy(state)
        inp = command(state, kind)
        state, data, duplicate = apply(case, events, inp)
        require(not duplicate, 9)
        event = read_event(data)
        inputs.append(inp)
        events.append(data)
        states.append(state)
        require(event["contractVersion"] == VERSIONS[offset - 1]
            and state["stateVersion"] == 15 + offset
            and state["prefix"] == irl.imv.rd.pre.env.sequence.prefix_event(prior["prefix"], data)
            and state["receipts"][-1] == dict(commandHash=sha(irl.raw(inp, "LifecycleInput")),
                eventHash=sha(data), receiptId=event["receiptId"], actor=inp["actor"],
                stateVersion=15 + offset), 6)
        if offset == 1:
            authority = prior["reactionWindow"]["activeOpportunityId"]
            require(inp["actor"] == p["reacting"]
                and inp["command"]["windowId"] == predecessor_input["command"]["windowId"]
                and inp["command"]["opportunityId"] != predecessor_input["command"]["opportunityId"]
                and irl.move_options(prior) == []
                and event["opportunityId"] == authority
                and event["reactionWindowAfter"]["activeOpportunityId"] is None
                and event["reactionWindowAfter"]["resolvedOpportunityIds"] == [authority]
                and event["breakdownFlowAfter"]["stop"]["recordedStateVersion"] == 16
                and event["breakdownFlowAfter"]["stop"]["reason"] == "reaction-completed"
                and event["breakdownFlowAfter"]["stop"]["weatherKind"] == "normal"
                and event["breakdownFlowAfter"]["stop"]["cohortInputs"] == []
                and event["breakdownFlowAfter"]["stop"]["route"]
                    == prior["breakdownFlow"]["reactorRoute"], 6)
        elif offset == 2:
            completion = read_event(events[0])
            require(inp["actor"] == "system"
                and event["stop"] == completion["breakdownFlowAfter"]["stop"]
                and inp["command"]["stopId"] != event["stop"]["stopId"]
                and event["checks"] == event["createdLots"] == []
                and event["randomStateBefore"] == event["randomStateAfter"] == prior["randomState"]
                and state["currentPosition"]["kind"] == "reaction"
                and state["reactionWindow"] == prior["reactionWindow"], 6)
        else:
            require(inp["actor"] == "system" and event["actingSide"] is None
                and event["reason"] == "no-eligible-reactor"
                and event["closedOpportunityIds"] == []
                and event["suspendedSequencePosition"] == before["sequencePosition"]
                and state["reactionWindow"] is None
                and state["currentPosition"] == dict(kind="sequence",
                    sequencePosition=before["sequencePosition"])
                and state["breakdownFlow"] == dict(kind="moving",
                    route=before["breakdownFlow"]["phasingContinuation"]["route"]), 6)
        require(read_state(irl.raw(state, "LifecycleState"), case, events) == state, 6)
    element = next(item for item in state["world"]["elements"]
        if item["elementId"] == p["reactingElementId"])
    require(element["currentLocationId"] == p["reactingSupply"]
        and element["operationalState"]["capabilityPointsExpended"] == irl.world.cp(4)
        and state["world"] == baseline["world"] and state["tracks"] == baseline["tracks"]
        and state["actualProgressRefs"] == baseline["actualProgressRefs"]
        and state["randomState"] == baseline["randomState"]
        and state["members"] == baseline["members"], 6)
    for index, inp in enumerate(inputs):
        retried, old, duplicate = apply(case, events, inp)
        require(duplicate and old == events[index] and retried == state, 6)
    return predecessor_event, inputs, events, states


def goldens(result):
    predecessor, inputs, events, states = result
    values = [("second-move", predecessor)]
    values += [(f"input-{index + 1}", irl.raw(value, "LifecycleInput"))
        for index, value in enumerate(inputs)]
    values += [(f"event-{index + 1}", value) for index, value in enumerate(events)]
    values += [(f"state-{index}", irl.raw(value, "LifecycleState"))
        for index, value in enumerate(states)]
    return {name: dict(bytes=len(data), sha256=sha(data)) for name, data in values}


def rejected(fn):
    try:
        fn()
    except (Invalid, irl.Invalid, irs.Invalid):
        return
    raise AssertionError("expected CMB-IRMC rejection")


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


def action_id(inp):
    cmd = inp["command"]
    fields = ("kind", "windowId", "opportunityId", "stopId")
    action = {key: value for key, value in cmd.items() if key in fields}
    return sha(encode({"contractVersion": 1, **action}))


def verify(result, case, deep=True):
    _, inputs, events, states = result
    counts = dict(cuts=0, retries=len(inputs), mutations=0, raw=0, boundaries=0)
    for index, state in enumerate(states):
        require(replay(case, events[:index]) == state, 6)
        counts["cuts"] += 1
    bad_sequences = ([events[1]], [events[2]], events[1:], [events[0], events[2]],
        list(reversed(events)), [events[0], events[0]], events + [events[-1]])
    for bad in bad_sequences:
        rejected(lambda bad=bad: replay(case, bad))
        counts["boundaries"] += 1
    for index, inp in enumerate(inputs):
        wrong_actor = states[0]["firstActingSide"] if inp["actor"] != "system" else "axis"
        for path, value in ((('actor',), wrong_actor),
                (('command', 'creationBinding'), 'foreign.creation'),
                (('command', 'creationEventHash'), 'sha256:' + 'a' * 64),
                (('command', 'cycleId'), 'sha256:' + 'b' * 64),
                (('command', 'expectedPriorVersion'), 1),
                (('command', 'expectedPositionId'), 'foreign.position'),
                (('command', 'actionId'), 'sha256:' + 'd' * 64)):
            rejected(lambda inp=inp, path=path, value=value, index=index:
                apply(case, events[:index], changed(inp, path, value)))
            counts["boundaries"] += 1
    authority_cases = [
        (0, "windowId", states[0]["reactionWindow"]["reactionWindowId"]),
        (0, "opportunityId", states[0]["reactionWindow"]["activeOpportunityId"]),
        (1, "stopId", states[1]["breakdownFlow"]["stop"]["stopId"]),
        (2, "windowId", states[2]["reactionWindow"]["reactionWindowId"]),
    ]
    for index, field, value in authority_cases:
        bad = changed(inputs[index], ("command", field), value)
        bad["command"]["actionId"] = action_id(bad)
        rejected(lambda bad=bad, index=index: apply(case, events[:index], bad))
        counts["boundaries"] += 1
    rejected(lambda: command(states[0], irl.KINDS[0]))
    counts["boundaries"] += 1
    version_overflow = copy.deepcopy(states[0])
    version_overflow["stateVersion"] = 2**63 - 1
    rejected(lambda: emit(version_overflow, command(version_overflow, KINDS[0])))
    counts["boundaries"] += 1
    receipt_overflow = copy.deepcopy(states[0])
    receipt_overflow["receipts"] = [copy.deepcopy(states[0]["receipts"][0])] * 512
    rejected(lambda: emit(receipt_overflow, command(receipt_overflow, KINDS[0])))
    counts["boundaries"] += 1
    if deep:
        for index, data in enumerate(events):
            event = read_event(data)
            for path, value in leaves(event):
                bad = changed(event, path, different(value))
                try:
                    if path != ("receiptId",):
                        bad["receiptId"] = irl.receipt(bad, index + 1)
                    encoded = irl.raw(bad, TYPES[index])
                except (Invalid, irl.Invalid, KeyError, TypeError):
                    encoded = encode(bad)
                rejected(lambda encoded=encoded, index=index:
                    replay(case, events[:index] + [encoded]))
                counts["mutations"] += 1
        for index, state in enumerate(states[1:], 1):
            for path in (("prefix",), ("stateVersion",), ("breakdownFlow", "kind"),
                    ("world", "elements", 0, "currentLocationId"),
                    ("randomState", "nextByteCursor"), ("actualProgressRefs",)):
                node = state
                for key in path:
                    node = node[key]
                bad = changed(state, path, different(node))
                try:
                    encoded = irl.raw(bad, "LifecycleState")
                except irl.Invalid:
                    encoded = encode(bad)
                rejected(lambda encoded=encoded, index=index:
                    read_state(encoded, case, events[:index]))
                counts["mutations"] += 1
        for index, data in enumerate(events):
            value = read_event(data)
            variants = [data + b" ", b" " + data, b"\xef\xbb\xbf" + data, b"\xff",
                data[:-1], b"{}", b'{"unknown":0,' + data[1:],
                encode(dict(reversed(list(value.items())))),
                data.replace(b'"contractVersion":', b'"contractVersion": ', 1)]
            for bad in variants:
                rejected(lambda bad=bad: read_event(bad))
                counts["raw"] += 1
    return counts


def fixture_shape(fixture):
    require(set(fixture) == {"contractVersion", "contract", "sourceHashes", "cases"}
        and fixture["contractVersion"] == 1
        and fixture["contract"] == "combat-inherited-reaction-movement-completion-v1", 9)
    require(len(fixture["cases"]) == 2
        and {case["actor"] for case in fixture["cases"]} == {"axis", "commonwealth"}, 9)
    require(set(fixture["sourceHashes"]) == SOURCE_PATHS, 9)


def generated_fixture():
    predecessor = json.loads(irs.FIXTURE.read_text())
    repo = ROOT.parent.parent
    cases = []
    for source in predecessor["cases"]:
        case = dict(name=source["actor"] + "-reaction-movement-completion",
            predecessorCase=source["name"], actor=source["actor"])
        case["goldens"] = goldens(trace(case))
        cases.append(case)
    return dict(contractVersion=1, contract="combat-inherited-reaction-movement-completion-v1",
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
        for key, value in verify(result, case).items():
            totals[key] += value
    for index, result in enumerate(results):
        foreign_case = fixture["cases"][1 - index]
        rejected(lambda result=result, foreign_case=foreign_case:
            replay(foreign_case, result[2]))
        totals["boundaries"] += 1
    print("CMB-IRMC PASS", json.dumps(dict(traces=2, events=6,
        sourcePins=len(SOURCE_PATHS), **totals), sort_keys=True))


if __name__ == "__main__":
    main()
