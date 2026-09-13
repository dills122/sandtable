#!/usr/bin/env python3
"""Creation-rooted inherited direct Reaction closure contract oracle."""
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
irt = irl.irt
encode, sha = irl.encode, irl.sha
INVENTORY = json.loads((ROOT / "combat-inherited-reaction-closure-v1.schema.json").read_text())
FIXTURE = ROOT / "fixtures/combat-inherited-reaction-closure-v1.json"
LOCAL = {key: [tuple(field.split(":")) for field in value.split()]
    for key, value in INVENTORY["objects"].items()}
SCHEMA = irl.SCHEMA | LOCAL
KINDS = list(INVENTORY["commandReasons"])
REASONS = INVENTORY["commandReasons"]
EVENT = "reaction-window-closed"
EVENT_VERSION = 3
SOURCE_PATHS = {
    "docs/design/zoc-reaction-v1.md",
    "docs/specs/zoc-reaction-v1.md",
    "docs/specs/combat-inherited-reaction-closure-v1.schema.json",
    "docs/specs/combat-inherited-reaction-trigger-v1.md",
    "docs/specs/combat-inherited-reaction-trigger-v1.schema.json",
    "docs/specs/fixtures/combat-inherited-reaction-trigger-v1.json",
    "docs/specs/verify-combat-inherited-reaction-trigger-v1.py",
    "docs/specs/combat-inherited-reaction-lifecycle-v1.schema.json",
    "docs/specs/verify-combat-inherited-reaction-lifecycle-v1.py",
    "src/Cna.Core/Actions/CampaignObservationV6ActionCandidates.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownLifecycleCodec.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownLifecycleEvents.cs",
    "src/Cna.Core/Campaigns/CampaignBreakdownLifecycleFactory.cs",
    "src/Cna.Core/Campaigns/CampaignV11BreakdownProjector.cs",
}


class Invalid(ValueError):
    def __init__(self, code):
        self.code = f"CMB-IRC-{code:03}"
        super().__init__(self.code)


def require(ok, code):
    if not ok:
        raise Invalid(code)


def typed(value, kind, depth=0):
    require(depth <= INVENTORY["limits"]["depth"], 1)
    if kind.endswith("?"):
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
        if kind in ("id[]", "hash[]"):
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
        movement_events, trigger_events):
    require(type(trigger_events) is list and len(trigger_events) == 1, 3)
    try:
        state = irl.initial(request, created, preamble, weather_events, stage_events, reserve_events,
            movement_events, trigger_events)
    except (ValueError, KeyError, TypeError) as error:
        raise Invalid(4) from error
    window = state["reactionWindow"]
    require(state["stateVersion"] == 13 and state["currentPosition"]["kind"] == "reaction"
        and window is not None and len(window["frozenOpportunities"]) == 1
        and window["resolvedOpportunityIds"] == [] and window["activeOpportunityId"] is None
        and state["breakdownFlow"]["kind"] == "reacting"
        and state["breakdownFlow"]["reactorRoute"] is None
        and state["breakdownFlow"]["phasingContinuation"]["kind"] == "resume-route", 4)
    return canonical(state, "LifecycleState")


def public_window(state):
    return irl.public_window(state)


def command(state, kind):
    require(kind in KINDS, 3)
    window = state["reactionWindow"]
    require(state["currentPosition"]["kind"] == "reaction" and window is not None
        and len(window["frozenOpportunities"]) == 1 and window["resolvedOpportunityIds"] == []
        and window["activeOpportunityId"] is None and state["breakdownFlow"]["kind"] == "reacting"
        and state["breakdownFlow"]["reactorRoute"] is None, 6)
    handle = public_window(state)
    action = dict(contractVersion=1, kind=kind, windowId=handle)
    cmd = dict(contractVersion=2, kind=kind, windowId=handle, actionId=sha(encode(action)),
        creationBinding=state["creationBinding"], creationEventHash=state["creationEventHash"],
        cycleId=state["cycleId"], expectedPriorVersion=state["stateVersion"],
        expectedPositionId=state["sequencePosition"]["positionId"])
    actor = window["reactingSide"] if kind == KINDS[0] else "system"
    return dict(command=cmd, actor=actor)


def authorize(state, inp):
    typed(inp, "ReactionCloseInput")
    cmd = inp["command"]
    require(cmd["contractVersion"] == 2 and cmd["kind"] in KINDS, 3)
    expected_actor = irl.opposite(state["firstActingSide"]) if cmd["kind"] == KINDS[0] else "system"
    require(inp["actor"] == expected_actor, 5)
    require(cmd["creationBinding"] == state["creationBinding"]
        and cmd["creationEventHash"] == state["creationEventHash"]
        and cmd["cycleId"] == state["cycleId"], 4)


def receipt(event):
    unsigned = {key: value for key, value in canonical(event, "DirectCloseEvent").items()
        if key != "receiptId"}
    digest = hashlib.sha256(INVENTORY["domains"]["closeReceipt"].encode()
        + b"\0" + encode(unsigned)).hexdigest()
    return "irl." + digest


def _emit(state, inp):
    authorize(state, inp)
    cmd = inp["command"]
    require(cmd["expectedPriorVersion"] == state["stateVersion"]
        and cmd["expectedPositionId"] == state["sequencePosition"]["positionId"], 6)
    require(state["stateVersion"] < 2**63 - 1 and len(state["receipts"]) < 512, 7)
    window = state["reactionWindow"]
    require(state["currentPosition"]["kind"] == "reaction" and window is not None
        and len(window["frozenOpportunities"]) == 1 and window["resolvedOpportunityIds"] == []
        and window["activeOpportunityId"] is None and state["breakdownFlow"]["kind"] == "reacting"
        and state["breakdownFlow"]["reactorRoute"] is None
        and state["breakdownFlow"]["phasingContinuation"]["kind"] == "resume-route", 6)
    require(raw(inp, "ReactionCloseInput") == raw(command(state, cmd["kind"]), "ReactionCloseInput"), 5)
    version = state["stateVersion"] + 1
    flow = dict(kind="moving", route=copy.deepcopy(
        state["breakdownFlow"]["phasingContinuation"]["route"]))
    event = dict(contractVersion=EVENT_VERSION, eventType=EVENT,
        campaignId=state["campaignId"], stateVersion=version,
        priorStateVersion=state["stateVersion"],
        fromPositionId=state["sequencePosition"]["positionId"],
        actingSide=inp["actor"] if cmd["kind"] == KINDS[0] else None,
        actionId=cmd["actionId"], submittedWindowId=cmd["windowId"],
        windowId=window["reactionWindowId"], reason=REASONS[cmd["kind"]],
        closedOpportunityIds=[window["frozenOpportunities"][0]["opportunityId"]],
        suspendedSequencePosition=copy.deepcopy(state["sequencePosition"]),
        rulesetHash=state["rulesetHash"], breakdownFlowAfter=flow,
        configurationHash=state["configurationHash"], creationBinding=state["creationBinding"],
        creationEventHash=state["creationEventHash"], cycleId=state["cycleId"],
        openingBaseHash=state["openingBaseHash"], completionReceiptId=state["completionReceiptId"],
        priorPrefix=state["prefix"], input=copy.deepcopy(inp), receiptId="pending")
    event["receiptId"] = receipt(event)
    data = raw(event, "DirectCloseEvent")
    after = copy.deepcopy(state)
    after.update(stateVersion=version,
        prefix=irl.imv.rd.pre.env.sequence.prefix_event(state["prefix"], data),
        currentPosition=dict(kind="sequence", sequencePosition=copy.deepcopy(state["sequencePosition"])),
        reactionWindow=None, breakdownFlow=copy.deepcopy(flow))
    after["receipts"].append(dict(commandHash=sha(raw(inp, "ReactionCloseInput")),
        eventHash=sha(data), receiptId=event["receiptId"], actor=inp["actor"], stateVersion=version))
    return canonical(after, "LifecycleState"), data


def read_event(data):
    value = parse(data, "DirectCloseEvent")
    require(value["contractVersion"] == EVENT_VERSION and value["eventType"] == EVENT, 3)
    kind = value["input"]["command"]["kind"]
    require(kind in KINDS and value["reason"] == REASONS[kind]
        and value["actingSide"] == (value["input"]["actor"] if kind == KINDS[0] else None)
        and value["receiptId"] == receipt(value), 4)
    return value


def replay(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, events):
    require(type(events) is list and len(events) <= INVENTORY["limits"]["closeEvents"], 7)
    state = initial(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events)
    for data in events:
        event = read_event(data)
        state, expected = _emit(state, event["input"])
        require(data == expected, 6)
    return state


def apply(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, events, inp):
    state = replay(request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, events)
    authorize(state, inp)
    for data in events:
        accepted = read_event(data)["input"]
        if accepted["command"]["expectedPriorVersion"] == inp["command"]["expectedPriorVersion"]:
            require(raw(inp, "ReactionCloseInput") == raw(accepted, "ReactionCloseInput"), 6)
            return state, data, True
    after, data = _emit(state, inp)
    return after, data, False


def read_state(data, request, created, preamble, weather_events, stage_events, reserve_events,
        movement_events, trigger_events, events):
    try:
        value = irl.parse(data, "LifecycleState")
        expected = replay(request, created, preamble, weather_events, stage_events, reserve_events,
            movement_events, trigger_events, events)
    except (irl.Invalid, ValueError, KeyError, TypeError) as error:
        raise Invalid(6) from error
    require(events and data == irl.raw(expected, "LifecycleState"), 6)
    return value


def source_trace(case):
    predecessor = next(item for item in json.loads(irt.FIXTURE.read_text())["cases"]
        if item["name"] == case["predecessorCase"])
    result = irt.trace(predecessor)
    require(irt.goldens(result) == predecessor["goldens"], 9)
    args, _, trigger, state = result
    return args + ([trigger],), state


def trace(case):
    args, before = source_trace(case)
    state = initial(*args)
    require(state == before, 4)
    inp = command(state, case["kind"])
    require(inp["actor"] == case["actor"] and REASONS[case["kind"]] == case["reason"], 9)
    after, event, duplicate = apply(*args, [], inp)
    require(not duplicate, 9)
    decoded = read_event(event)
    window = before["reactionWindow"]
    assert decoded["stateVersion"] == 14 and decoded["priorStateVersion"] == 13
    assert decoded["actingSide"] == (case["actor"] if case["reason"] == "player-decline" else None)
    assert decoded["reason"] == case["reason"]
    assert decoded["submittedWindowId"] != decoded["windowId"]
    assert decoded["closedOpportunityIds"] == [window["frozenOpportunities"][0]["opportunityId"]]
    assert decoded["suspendedSequencePosition"] == before["sequencePosition"]
    assert decoded["breakdownFlowAfter"] == dict(kind="moving",
        route=before["breakdownFlow"]["phasingContinuation"]["route"])
    assert after["stateVersion"] == 14 and after["reactionWindow"] is None
    assert after["currentPosition"] == dict(kind="sequence", sequencePosition=before["sequencePosition"])
    assert after["breakdownFlow"] == decoded["breakdownFlowAfter"]
    for key in ("world", "randomState", "initiativeHolder", "operationStageOrders",
            "operationStageWeather", "members", "cycle", "cycleId", "openingBaseHash",
            "completionReceiptId", "tracks", "actualProgressRefs"):
        assert after[key] == before[key]
    assert after["receipts"][:-1] == before["receipts"] and after["prefix"] != before["prefix"]
    assert read_state(irl.raw(after, "LifecycleState"), *args, [event]) == after
    retried, original, duplicate = apply(*args, [event], inp)
    assert duplicate and original == event and retried == after
    return args, inp, event, before, after


def goldens(result):
    args, inp, event, before, after = result
    values = [("trigger", args[-1][0]), ("input", raw(inp, "ReactionCloseInput")),
        ("event", event), ("state-before", irl.raw(before, "LifecycleState")),
        ("state-after", irl.raw(after, "LifecycleState"))]
    return {name: dict(bytes=len(data), sha256=sha(data)) for name, data in values}


def rejected(fn):
    try:
        fn()
    except (Invalid, irl.Invalid):
        return
    raise AssertionError("expected CMB-IRC rejection")


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
    args, inp, event, before, after = result
    counts = dict(cuts=2, retries=1, mutations=0, raw=0, boundaries=0)
    assert replay(*args, []) == before and replay(*args, [event]) == after
    for bad in ([event, event], [event, b"{}"]):
        rejected(lambda bad=bad: replay(*args, bad))
        counts["boundaries"] += 1
    for path, value in ((('actor',), 'system' if inp['actor'] != 'system' else before['firstActingSide']),
            (('command', 'creationBinding'), 'foreign.creation'),
            (('command', 'creationEventHash'), 'sha256:' + 'a' * 64),
            (('command', 'cycleId'), 'sha256:' + 'b' * 64),
            (('command', 'expectedPriorVersion'), 1),
            (('command', 'expectedPositionId'), 'foreign.position'),
            (('command', 'actionId'), 'sha256:' + 'd' * 64)):
        bad = changed(inp, path, value)
        rejected(lambda bad=bad: apply(*args, [], bad))
        counts["boundaries"] += 1
    authority = before["reactionWindow"]["reactionWindowId"]
    bad = changed(inp, ("command", "windowId"), authority)
    bad["command"]["actionId"] = sha(encode(dict(contractVersion=1,
        kind=bad["command"]["kind"], windowId=authority)))
    rejected(lambda: apply(*args, [], bad))
    counts["boundaries"] += 1
    for kind in KINDS:
        if kind == inp["command"]["kind"]:
            continue
        alternate = command(before, kind)
        rejected(lambda alternate=alternate: apply(*args, [event], alternate))
        counts["boundaries"] += 1
    rejected(lambda: initial(*args[:-1], []))
    rejected(lambda: initial(*args[:-1], args[-1] * 2))
    counts["boundaries"] += 2
    shape_cases = []
    active = copy.deepcopy(before)
    active["reactionWindow"]["activeOpportunityId"] = active["reactionWindow"]["frozenOpportunities"][0]["opportunityId"]
    shape_cases.append(active)
    resolved = copy.deepcopy(before)
    resolved["reactionWindow"]["resolvedOpportunityIds"] = [resolved["reactionWindow"]["frozenOpportunities"][0]["opportunityId"]]
    shape_cases.append(resolved)
    empty = copy.deepcopy(before)
    empty["reactionWindow"]["frozenOpportunities"] = []
    shape_cases.append(empty)
    multiple = copy.deepcopy(before)
    extra = copy.deepcopy(multiple["reactionWindow"]["frozenOpportunities"][0])
    extra["opportunityId"] = "sha256:" + "e" * 64
    multiple["reactionWindow"]["frozenOpportunities"].append(extra)
    shape_cases.append(multiple)
    for bad_state in shape_cases:
        rejected(lambda bad_state=bad_state: _emit(bad_state, inp))
        counts["boundaries"] += 1
    overflow = copy.deepcopy(before)
    overflow["stateVersion"] = 2**63 - 1
    overflow_input = changed(inp, ("command", "expectedPriorVersion"), 2**63 - 1)
    rejected(lambda: _emit(overflow, overflow_input))
    counts["boundaries"] += 1
    capacity = copy.deepcopy(before)
    capacity["receipts"] = capacity["receipts"] + [copy.deepcopy(capacity["receipts"][-1])
        for _ in range(512 - len(capacity["receipts"]))]
    rejected(lambda: _emit(capacity, inp))
    counts["boundaries"] += 1
    decoded = read_event(event)
    for path, value in leaves(decoded):
        altered = changed(decoded, path, different(value))
        try:
            if path != ("receiptId",):
                altered["receiptId"] = receipt(altered)
            data = raw(altered, "DirectCloseEvent")
        except (Invalid, KeyError, TypeError):
            data = encode(altered)
        rejected(lambda data=data: replay(*args, [data]))
        counts["mutations"] += 1
    for path in (("prefix",), ("stateVersion",), ("breakdownFlow", "kind"),
            ("world", "elements", 0, "currentLocationId"),
            ("randomState", "nextByteCursor"), ("actualProgressRefs", 0, "eventHash")):
        node = after
        for key in path:
            node = node[key]
        altered = changed(after, path, different(node))
        try:
            data = irl.raw(altered, "LifecycleState")
        except irl.Invalid:
            data = encode(altered)
        rejected(lambda data=data: read_state(data, *args, [event]))
        counts["mutations"] += 1
    samples = [(event, "DirectCloseEvent"), (raw(inp, "ReactionCloseInput"), "ReactionCloseInput"),
        (irl.raw(after, "LifecycleState"), "LifecycleState")]
    for data, kind in samples:
        value = json.loads(data)
        version = value.get("contractVersion", value.get("command", {}).get("contractVersion", 2))
        variants = [data + b" ", b" " + data, b"\xef\xbb\xbf" + data, b"\xff", data[:-1], b"{}",
            b'{"unknown":0,' + data[1:], encode(dict(reversed(list(value.items())))),
            data.replace(b'"contractVersion":', b'"contractVersion": ', 1),
            data.replace(b'"contractVersion":' + str(version).encode(), b'"contractVersion":true', 1)]
        for malformed in variants:
            if kind == "LifecycleState":
                rejected(lambda malformed=malformed: read_state(malformed, *args, [event]))
            else:
                rejected(lambda malformed=malformed, kind=kind: parse(malformed, kind))
            counts["raw"] += 1
    return counts


def fixture_shape(fixture):
    require(set(fixture) == {"contractVersion", "contract", "sourceHashes", "cases"}
        and fixture["contractVersion"] == 1
        and fixture["contract"] == "combat-inherited-reaction-closure-v1", 9)
    expected = [(side, kind, REASONS[kind]) for side in ("axis", "commonwealth") for kind in KINDS]
    actual = [(case["name"].split("-", 1)[0], case["kind"], case["reason"])
        for case in fixture["cases"]]
    require(actual == expected and set(fixture["sourceHashes"]) == SOURCE_PATHS, 9)


def generated_fixture():
    predecessor = json.loads(irt.FIXTURE.read_text())
    repo = ROOT.parent.parent
    cases = []
    for source in predecessor["cases"]:
        side = source["expected"]["actor"]
        reacting = irl.opposite(side)
        for kind in KINDS:
            reason = REASONS[kind]
            case = dict(name=f"{side}-{reason}", predecessorCase=source["name"], kind=kind,
                reason=reason, actor=reacting if kind == KINDS[0] else "system")
            case["goldens"] = goldens(trace(case))
            cases.append(case)
    return dict(contractVersion=1, contract="combat-inherited-reaction-closure-v1",
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
        rejected(lambda result=result, foreign=foreign: replay(*foreign[0], [result[2]]))
        totals["boundaries"] += 1
    print("CMB-IRC PASS", json.dumps(dict(traces=len(results), events=len(results),
        sourcePins=len(SOURCE_PATHS), **totals), sort_keys=True))


if __name__ == "__main__":
    main()
