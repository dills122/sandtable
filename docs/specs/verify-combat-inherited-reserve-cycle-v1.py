#!/usr/bin/env python3
"""Creation-rooted inherited Reserve cycle evidence; no release or runtime admission."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "reserve_designation", ROOT / "verify-combat-reserve-designation-v1.py")
rd = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rd)
encode, sha = rd.encode, rd.sha
INVENTORY = json.loads((ROOT / "combat-inherited-reserve-cycle-v1.schema.json").read_text())
FIXTURE = ROOT / "fixtures/combat-inherited-reserve-cycle-v1.json"
SCHEMA = rd.SCHEMA | {
    name: [tuple(field.split(":")) for field in fields.split()]
    for name, fields in INVENTORY["objects"].items()
}
TAGS = INVENTORY["effectTags"]
CATALOG = rd.opening.steps.seq.expected_catalog()
EDGE = next(item for item in CATALOG["cycles"]
    if item["operationStage"] == 1 and item["playerPhaseSlot"] == "first-acting-side")
POSITION_IDS = [EDGE["movementPositionId"], EDGE["breakdownPositionId"],
    *EDGE["combatPositionIds"], EDGE["releasePositionId"]]
POSITIONS = [copy.deepcopy(next(position for position in CATALOG["positions"]
    if position["positionId"] == position_id)) for position_id in POSITION_IDS]
KINDS = ["complete-movement-segment", "complete-breakdown-segment",
    "open-combat-selection", "close-empty-selection"] + ["complete-combat-step"] * 6
EVENT_TYPES = ["movement-segment-completed", "breakdown-segment-completed",
    "combat-selection-opened", "combat-selection-closed"] + ["combat-step-completed"] * 6


class Invalid(ValueError):
    def __init__(self, code):
        self.code = f"CMB-IRC-{code:03}"
        super().__init__(self.code)


def require(ok, code):
    if not ok:
        raise Invalid(code)


def typed(value, kind, depth=0):
    require(depth <= INVENTORY["limits"]["depth"], 1)
    if kind == "Effect":
        require(type(value) is dict and type(value.get("kind")) is str
            and value["kind"] in TAGS, 3)
        typed(value, TAGS[value["kind"]], depth)
    elif kind.endswith("?"):
        if value is not None:
            typed(value, kind[:-1], depth)
    elif kind in SCHEMA:
        require(type(value) is dict and set(value) == {key for key, _ in SCHEMA[kind]}, 1)
        for key, child in SCHEMA[kind]:
            typed(value[key], child, depth + 1)
    elif kind.endswith("[]"):
        require(type(value) is list and len(value) <= 512, 1)
        for child in value:
            typed(child, kind[:-2], depth + 1)
    else:
        try:
            rd.typed(value, kind, depth)
        except rd.Invalid as error:
            raise Invalid(1) from error


def canonical(value, kind):
    if kind == "Effect":
        return canonical(value, TAGS[value["kind"]])
    if kind.endswith("?"):
        return None if value is None else canonical(value, kind[:-1])
    if kind in SCHEMA:
        return {key: canonical(value[key], child) for key, child in SCHEMA[kind]}
    if kind.endswith("[]"):
        return [canonical(child, kind[:-2]) for child in value]
    return rd.canonical(value, kind)


def raw(value, kind):
    typed(value, kind)
    data = encode(canonical(value, kind))
    require(len(data) <= INVENTORY["limits"]["bytes"], 1)
    return data


def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= INVENTORY["limits"]["bytes"], 1)

    def pairs(items):
        value = {}
        for key, child in items:
            require(key not in value, 1)
            value[key] = child
        return value

    try:
        value = json.loads(data.decode("utf8"), object_pairs_hook=pairs,
            parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error:
        raise Invalid(1) from error
    require(raw(value, kind) == data, 8)
    return value


def digest(domain, data):
    return hashlib.sha256(domain.encode("ascii") + b"\0" + data).hexdigest()


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
        return "sha256:" + ("f" if value != "sha256:" + "f" * 64 else "e") * 64
    return value + "x"


def unit_key(unit):
    return rd.opening.rel.key(unit)


def release_scope(cycle):
    return dict(gameTurn=cycle["gameTurn"], operationStage=cycle["operationStage"],
        playerPhaseSlot=cycle["playerPhaseSlot"], actingSide=cycle["actingSide"])


def end_locations(base):
    values = [dict(unit=rd.world.unit(element, base["creationBinding"]),
        locationId=element["currentLocationId"]) for element in base["world"]["elements"]]
    return sorted(values, key=lambda item: unit_key(item["unit"]))


def distance(origin, destination):
    return len(rd.world.init.content.route(rd.world.GRAPH, origin, destination)) - 1


def ordinary_exclusions(base):
    locations = end_locations(base)
    side = base["cycle"]["actingSide"]
    enemies = [item["locationId"] for item in locations if item["unit"]["originalSide"] != side]
    excluded = [copy.deepcopy(item["unit"]) for item in locations
        if item["unit"]["originalSide"] == side
        and not any(distance(item["locationId"], enemy) <= 2 for enemy in enemies)]
    return sorted(excluded, key=unit_key)


def position_after(event_count):
    if event_count <= 2:
        return POSITIONS[event_count]
    if event_count <= 4:
        return POSITIONS[2]
    return POSITIONS[event_count - 2]


def expected_segment(base):
    return "seg." + digest(INVENTORY["domains"]["segment"], rd.raw(base, "ReserveState"))


def validate(state):
    typed(state, "Control")
    base = state["base"]
    count = len(state["receipts"])
    require(state["contractVersion"] == 1 and count <= INVENTORY["limits"]["events"], 3)
    require(state["baseHash"] == sha(rd.raw(base, "ReserveState")), 4)
    require(base["stateVersion"] == 12 and len(base["receipts"]) == 11
        and base["sequencePosition"] == POSITIONS[0], 4)
    require(base["cycle"] is not None and base["cycleId"] is not None
        and base["cycle"]["ordinal"] == 1 and base["cycle"]["actingSide"] == base["firstActingSide"], 4)
    require(len(base["members"]) == 1 and base["members"][0]["status"] == "I"
        and base["members"][0]["history"]["designationReceiptId"] is not None, 5)
    own = next(element for element in base["world"]["elements"]
        if rd.world.SIDES[element["elementId"]] == base["firstActingSide"])
    require(own["reserveStatus"] == "I"
        and own["operationalState"]["capabilityPointsExpended"] == rd.world.cp(0), 5)
    require(base["operationStageWeather"][0]["kind"] == "normal", 5)
    require(state["stateVersion"] == base["stateVersion"] + count
        and state["position"] == position_after(count), 6)
    require(state["breakdownFlow"] == {"kind": "idle"}, 5)
    require(state["selection"]["segmentId"] == expected_segment(base)
        and state["selection"]["candidateIds"] == [], 4)
    require(state["stepIndex"] == max(0, count - 4)
        and state["stepReceipts"] == [item["receiptId"] for item in state["receipts"][4:]], 6)
    require(state["closed"] == (count == 10), 6)
    require(all(item["receiptId"].startswith("irc.") for item in state["receipts"]), 4)
    require(all(item["actor"] == (base["firstActingSide"] if index == 0 else "system")
        and item["stateVersion"] == base["stateVersion"] + index + 1
        for index, item in enumerate(state["receipts"])), 6)
    require(len({item["commandHash"] for item in state["receipts"]}) == count
        and len({item["eventHash"] for item in state["receipts"]}) == count, 6)
    if count == 0:
        require(state["prefix"] == base["prefix"] and state["movementEnd"] is None, 4)
    else:
        proof = state["movementEnd"]
        require(proof is not None and proof["scope"] == release_scope(base["cycle"])
            and proof["ordinal"] == 1
            and proof["completionReceiptId"] == state["receipts"][0]["receiptId"], 4)
        require(proof["endLocations"] == end_locations(base)
            and proof["excludedBefore"] == ordinary_exclusions(base), 5)
    selection = state["selection"]
    if count < 3:
        require(selection["outcome"] == "unopened" and selection["openingReceiptId"] is None
            and selection["selectionReceiptId"] is None, 6)
    elif count == 3:
        require(selection["outcome"] == "system-no-selection"
            and selection["openingReceiptId"] == state["receipts"][2]["receiptId"]
            and selection["selectionReceiptId"] is None, 6)
    else:
        require(selection["outcome"] == "no-selection"
            and selection["openingReceiptId"] == state["receipts"][2]["receiptId"]
            and selection["selectionReceiptId"] == state["receipts"][3]["receiptId"], 6)


def initial(base):
    typed(base, "ReserveState")
    state = canonical(dict(contractVersion=1, base=copy.deepcopy(base),
        baseHash=sha(rd.raw(base, "ReserveState")), stateVersion=base["stateVersion"],
        prefix=base["prefix"], position=copy.deepcopy(POSITIONS[0]),
        breakdownFlow={"kind": "idle"}, movementEnd=None,
        selection=dict(segmentId=expected_segment(base), candidateIds=[], openingReceiptId=None,
            selectionReceiptId=None, outcome="unopened"),
        stepIndex=0, stepReceipts=[], receipts=[], closed=False), "Control")
    validate(state)
    return state


def command(state):
    validate(state)
    index = len(state["receipts"])
    require(index < len(KINDS), 6)
    prior_receipt = state["base"]["completionReceiptId"] if index == 0 else state["receipts"][-1]["receiptId"]
    return dict(contractVersion=2, kind=KINDS[index],
        creationBinding=state["base"]["creationBinding"],
        creationEventHash=state["base"]["creationEventHash"], cycleId=state["base"]["cycleId"],
        expectedPriorVersion=state["stateVersion"], expectedPositionId=state["position"]["positionId"],
        priorReceiptId=prior_receipt)


def trusted(state, actor=None):
    expected_actor = state["base"]["firstActingSide"] if not state["receipts"] else "system"
    return dict(command=command(state), actor=expected_actor if actor is None else actor)


def receipt(event):
    typed(event, "Event")
    unsigned = {key: value for key, value in canonical(event, "Event").items() if key != "receiptId"}
    return "irc." + digest(INVENTORY["domains"]["receipt"], encode(unsigned))


def effect(state, index):
    if index == 0:
        return dict(kind="movement-completed", breakdownFlowAfter={"kind": "idle"},
            interruptContextAfter=None, endLocations=end_locations(state["base"]),
            excludedUnits=ordinary_exclusions(state["base"]), progress=[])
    if index == 1:
        return dict(kind="breakdown-completed", breakdownFlowAfter={"kind": "idle"},
            movementCompletionReceiptId=state["movementEnd"]["completionReceiptId"])
    if index == 2:
        return dict(kind="selection-opened", candidateCount=0, decisionId=None)
    if index == 3:
        return dict(kind="selection-closed", outcome="no-selection",
            openingReceiptId=state["selection"]["openingReceiptId"])
    return dict(kind="step-completed", fromPositionId=state["position"]["positionId"],
        toPositionId=position_after(index + 1)["positionId"],
        previousStepReceiptId=(state["stepReceipts"][-1] if state["stepReceipts"]
            else state["selection"]["openingReceiptId"]),
        dispositionReceiptId=state["selection"]["selectionReceiptId"], proofKind="no-attack")


def _transition(prior, inp):
    validate(prior)
    typed(inp, "Input")
    command_hash = sha(raw(inp, "Input"))
    duplicate = next((item for item in prior["receipts"] if item["commandHash"] == command_hash), None)
    if duplicate is not None:
        require(duplicate["actor"] == inp["actor"], 5)
        return copy.deepcopy(prior), None, duplicate["receiptId"]
    require(not prior["closed"] and len(prior["receipts"]) < INVENTORY["limits"]["events"], 7)
    expected = trusted(prior)
    require(raw(inp, "Input") == raw(expected, "Input"), 6)
    require(prior["stateVersion"] < 2**63 - 1, 7)
    index = len(prior["receipts"])
    version = prior["stateVersion"] + 1
    event = dict(contractVersion=INVENTORY["eventVersions"][EVENT_TYPES[index]],
        eventType=EVENT_TYPES[index], campaignId=prior["base"]["campaignId"],
        rulesetHash=prior["base"]["rulesetHash"], configurationHash=prior["base"]["configurationHash"],
        creationBinding=prior["base"]["creationBinding"],
        creationEventHash=prior["base"]["creationEventHash"], cycleId=prior["base"]["cycleId"],
        openingBaseHash=prior["base"]["openingBaseHash"],
        openingCompletionReceiptId=prior["base"]["completionReceiptId"],
        gameTurn=prior["base"]["cycle"]["gameTurn"],
        operationStage=prior["base"]["cycle"]["operationStage"],
        actingSide=prior["base"]["firstActingSide"],
        priorVersion=prior["stateVersion"], stateVersion=version,
        fromPositionId=prior["position"]["positionId"], sequencePosition=position_after(index + 1),
        sources=copy.deepcopy(prior["position"]["sources"]), priorPrefix=prior["prefix"],
        input=copy.deepcopy(inp), effect=effect(prior, index), receiptId="pending")
    event["receiptId"] = receipt(event)
    data = raw(event, "Event")
    after = copy.deepcopy(prior)
    after["stateVersion"] = version
    after["prefix"] = rd.pre.env.sequence.prefix_event(prior["prefix"], data)
    after["position"] = copy.deepcopy(event["sequencePosition"])
    after["receipts"].append(dict(commandHash=command_hash, eventHash=sha(data),
        receiptId=event["receiptId"], actor=inp["actor"], stateVersion=version))
    if index == 0:
        after["movementEnd"] = dict(scope=release_scope(after["base"]["cycle"]), ordinal=1,
            completionReceiptId=event["receiptId"], endLocations=end_locations(after["base"]),
            excludedBefore=ordinary_exclusions(after["base"]))
    elif index == 2:
        after["selection"].update(outcome="system-no-selection", openingReceiptId=event["receiptId"])
    elif index == 3:
        after["selection"].update(outcome="no-selection", selectionReceiptId=event["receiptId"])
    elif index >= 4:
        after["stepIndex"] += 1
        after["stepReceipts"].append(event["receiptId"])
    after["closed"] = len(after["receipts"]) == 10
    validate(after)
    return canonical(after, "Control"), data, event["receiptId"]


def read_event(data, prior, inp):
    event = parse(data, "Event")
    require(event["receiptId"] == receipt(event), 4)
    state, expected, _ = _transition(prior, inp)
    require(expected is not None and data == expected, 6)
    return state


def base_from(history):
    try:
        return rd.replay(*history)
    except rd.Invalid as error:
        raise Invalid(4) from error


def replay(history, events):
    require(type(events) is list and len(events) <= INVENTORY["limits"]["events"], 7)
    state = initial(base_from(history))
    for data in events:
        event = parse(data, "Event")
        state = read_event(data, state, event["input"])
    return state


def apply(history, events, inp, cached_control=None):
    state = replay(history, events)
    if cached_control is not None:
        parse(cached_control, "Control")
        require(cached_control == raw(state, "Control"), 6)
    after, data, receipt_id = _transition(state, inp)
    if data is None:
        accepted = next((candidate for candidate in events
            if parse(candidate, "Event")["receiptId"] == receipt_id), None)
        require(accepted is not None, 6)
        return after, accepted, True
    return after, data, False


def read_control(data, history, events):
    value = parse(data, "Control")
    require(data == raw(replay(history, events), "Control"), 6)
    return value


def source_trace(case):
    parent = next(row for row in json.loads(rd.FIXTURE.read_text())["cases"]
        if (row["seed"], row["choice"], row["reserve"]) ==
        (case["seed"], case["choice"], case["reserve"]))
    result = rd.trace(parent)
    assert rd.goldens(result) == parent["goldens"]
    return result


def trace(case):
    parent = source_trace(case)
    request, created, preamble, weather, stage, _, reserve_events, _ = parent
    history = (request, created, preamble, weather, stage, reserve_events)
    base = base_from(history)
    state = initial(base)
    states = [state]
    inputs = []
    events = []
    for _ in range(10):
        inp = trusted(state)
        state, data, duplicate = apply(history, events, inp, raw(state, "Control"))
        require(not duplicate, 6)
        inputs.append(inp)
        events.append(data)
        states.append(state)
    assert base["firstActingSide"] == case["actor"] and base["members"][0]["status"] == "I"
    assert base["operationStageWeather"][0]["kind"] == "normal"
    assert all(element["operationalState"]["capabilityPointsExpended"] == rd.world.cp(0)
        for element in base["world"]["elements"])
    assert state["base"] == base and state["base"]["world"] == states[0]["base"]["world"]
    assert state["base"]["randomState"] == states[0]["base"]["randomState"]
    assert state["movementEnd"]["endLocations"] == end_locations(base)
    assert state["movementEnd"]["excludedBefore"] == []
    assert state["selection"]["candidateIds"] == [] and state["selection"]["outcome"] == "no-selection"
    assert state["closed"] and state["stepIndex"] == 6 and state["position"] == POSITIONS[-1]
    assert [parse(event, "Event")["eventType"] for event in events] == EVENT_TYPES
    assert [parse(event, "Event")["contractVersion"] for event in events] == [3, 2, 2, 2] + [2] * 6
    assert [parse(event, "Event")["effect"]["kind"] for event in events] == [
        "movement-completed", "breakdown-completed", "selection-opened", "selection-closed"] + [
        "step-completed"] * 6
    assert all(parse(event, "Event")["effect"].get("proofKind") == "no-attack"
        for event in events[4:])
    assert read_control(raw(state, "Control"), history, events) == state
    return history, parent, states, inputs, events


def goldens(result):
    history, _, states, _, events = result
    request, created, preamble, weather, stage, reserve_events = history
    records = [rd.pre.env.encode(request), created, *preamble, *weather, *stage, *reserve_events]
    values = [encode([sha(record) for record in records]), rd.raw(states[0]["base"], "ReserveState"),
        *events, *[raw(state, "Control") for state in states]]
    return dict(bytes=[len(value) for value in values], sha256=[sha(value) for value in values])


def rejected(call):
    try:
        call()
    except Invalid:
        return
    raise AssertionError("expected CMB-IRC rejection")


def verify(result, deep):
    history, _, states, inputs, events = result
    frozen = copy.deepcopy(result)
    counts = dict(cuts=11, retries=10, mutations=0, raw=0, boundaries=0)
    for index, state in enumerate(states):
        assert replay(history, events[:index]) == state
        assert read_control(raw(state, "Control"), history, events[:index]) == state
    final = states[-1]
    for inp, data in zip(inputs, events):
        same, accepted, duplicate = apply(history, events, inp, raw(final, "Control"))
        assert same == final and accepted == data and duplicate
    for state, inp in zip(states[:-1], inputs):
        alternate = "commonwealth" if inp["actor"] == "axis" else "axis"
        for path, value in ((('actor',), alternate), (('command', 'contractVersion'), 1),
            (('command', 'kind'), 'unsupported'), (('command', 'creationBinding'), 'foreign.creation'),
            (('command', 'creationEventHash'), 'sha256:' + '0' * 64),
            (('command', 'cycleId'), 'sha256:' + '1' * 64),
            (('command', 'expectedPriorVersion'), state["stateVersion"] - 1),
            (('command', 'expectedPositionId'), 'land.position.foreign'),
            (('command', 'priorReceiptId'), 'irc.' + '2' * 64)):
            bad = rd.pre.changed(inp, path, value)
            rejected(lambda candidate=bad, prior=state: _transition(prior, candidate))
            counts["boundaries"] += 1
    for sequence in (events[1:], [events[0], events[0]], list(reversed(events)), events + [events[-1]]):
        rejected(lambda candidate=sequence: replay(history, candidate))
        counts["boundaries"] += 1
    forged_cache = rd.pre.changed(states[1], ('prefix',), 'sha256:' + '3' * 64)
    rejected(lambda: apply(history, events[:1], inputs[1], raw(forged_cache, "Control")))
    counts["boundaries"] += 1
    rejected(lambda: apply(history, events, trusted(final), raw(final, "Control")))
    counts["boundaries"] += 1
    if deep:
        for index, data in enumerate(events):
            value = parse(data, "Event")
            for path, leaf in rd.pre.leaves(value):
                bad = rd.pre.changed(value, path, different(leaf))
                try:
                    if path != ('receiptId',):
                        bad["receiptId"] = receipt(bad)
                    encoded = raw(bad, "Event")
                except Invalid:
                    encoded = encode(bad)
                rejected(lambda candidate=encoded, prior=states[index], inp=inputs[index]:
                    read_event(candidate, prior, inp))
                counts["mutations"] += 1
        for state, length in ((states[0], 0), (states[-1], 10)):
            for path, leaf in rd.pre.leaves(state):
                bad = rd.pre.changed(state, path, different(leaf))
                try:
                    encoded = raw(bad, "Control")
                except Invalid:
                    encoded = encode(bad)
                rejected(lambda candidate=encoded, count=length:
                    read_control(candidate, history, events[:count]))
                counts["mutations"] += 1
        other_case = dict(actor="commonwealth", seed=1, choice="act-last", reserve="I")
        other = trace(other_case)
        rejected(lambda: replay(other[0], events))
        counts["boundaries"] += 1
        short = list(copy.deepcopy(history))
        short[-1] = short[-1][:-1]
        rejected(lambda: replay(tuple(short), events))
        counts["boundaries"] += 1
        for data, kind in ((events[0], "Event"), (events[-1], "Event"),
            (raw(states[0], "Control"), "Control"), (raw(states[-1], "Control"), "Control")):
            value = json.loads(data)
            first_key = next(iter(value))
            version = b'"contractVersion":3' if b'"contractVersion":3' in data else b'"contractVersion":2'
            if b'"contractVersion":1' in data:
                version = b'"contractVersion":1'
            variants = [data + b' ', b' ' + data, b'\xef\xbb\xbf' + data,
                data.replace(b':', b': ', 1), b'{"unknown":0,' + data[1:],
                b'{' + encode(first_key) + b':null,' + data[1:],
                encode(dict(reversed(list(value.items())))),
                data.replace(b'contractVersion', b'contractVersio\\u006e', 1),
                data.replace(version, version + b'.0', 1),
                data.replace(version, b'"contractVersion":true', 1)]
            for bad in variants:
                assert bad != data
                rejected(lambda candidate=bad, target=kind: parse(candidate, target))
                counts["raw"] += 1
        for bad in (b'', b'x' * 1048577, b'NaN', b'null', b'[]'):
            rejected(lambda candidate=bad: parse(candidate, "Event"))
            counts["raw"] += 1
        bad = json.loads(events[0])
        bad["effect"]["kind"] = {}
        rejected(lambda: parse(encode(bad), "Event"))
        counts["raw"] += 1
        for field, value in (("stateVersion", 2**63 - 1), ("receipts", states[-2]["receipts"] * 2)):
            bad_state = copy.deepcopy(states[-2])
            bad_state[field] = value
            rejected(lambda candidate=bad_state: _transition(candidate, trusted(states[-2])))
            counts["boundaries"] += 1
    assert result == frozen, "verification mutated accepted history/state"
    return counts


PIN_PATHS = [
    "docs/specs/combat-reserve-designation-v1.schema.json",
    "docs/specs/verify-combat-reserve-designation-v1.py",
    "docs/specs/fixtures/combat-reserve-designation-v1.json",
    "docs/specs/combat-inherited-movement-lifecycle-v1.schema.json",
    "docs/specs/verify-combat-inherited-movement-lifecycle-v1.py",
    "docs/specs/combat-inherited-breakdown-completion-v1.schema.json",
    "docs/specs/verify-combat-inherited-breakdown-completion-v1.py",
    "docs/specs/combat-inherited-selection-v1.schema.json",
    "docs/specs/verify-combat-inherited-selection-v1.py",
    "docs/specs/combat-inherited-no-attack-v1.schema.json",
    "docs/specs/verify-combat-inherited-no-attack-v1.py",
    "docs/specs/combat-cycle-sequence-v1.schema.json",
    "src/Cna.Core/Campaigns/CampaignBreakdownLifecycleFactory.cs"
]


def check_fixture(fixture):
    require(set(fixture) == {"contractVersion", "cases", "sourcePins"}
        and fixture["contractVersion"] == 1, 9)
    require(len(fixture["cases"]) == 2 and {(case["actor"], case["seed"], case["choice"], case["reserve"])
        for case in fixture["cases"]} == {("axis", 1, "act-first", "I"),
            ("commonwealth", 1, "act-last", "I")}, 9)
    for case in fixture["cases"]:
        require(set(case) == {"actor", "seed", "choice", "reserve", "goldens"}, 9)
        if case["goldens"]:
            require(set(case["goldens"]) == {"bytes", "sha256"}
                and len(case["goldens"]["bytes"]) == len(case["goldens"]["sha256"]) == 23, 9)
    require([pin["path"] for pin in fixture["sourcePins"]] == PIN_PATHS, 9)
    for pin in fixture["sourcePins"]:
        require(sha((ROOT.parent.parent / pin["path"]).read_bytes()) == pin["sha256"], 9)


def semantic_gate(result):
    _, _, states, _, events = result
    assert len(events) == 10 and states[0]["base"]["members"][0]["status"] == "I"
    assert states[0]["base"]["world"] == states[-1]["base"]["world"]
    assert states[-1]["position"]["positionId"] == EDGE["releasePositionId"]
    assert states[-1]["movementEnd"]["excludedBefore"] == []
    assert states[-1]["selection"]["candidateIds"] == [] and states[-1]["closed"]


def main():
    fixture = json.loads(FIXTURE.read_text())
    check_fixture(fixture)
    total = dict(cuts=0, retries=0, mutations=0, raw=0, boundaries=0)
    computed = []
    results = []
    for case in fixture["cases"]:
        result = trace(case)
        semantic_gate(result)
        current = goldens(result)
        results.append(result)
        computed.append(dict(actor=case["actor"], seed=case["seed"], choice=case["choice"],
            reserve=case["reserve"], goldens=current))
        if case["goldens"] and "--goldens" not in sys.argv:
            assert current == case["goldens"], case["actor"] + " golden drift"
    if "--goldens" in sys.argv:
        print(json.dumps(computed, indent=2))
        return
    for case, result in zip(fixture["cases"], results):
        for key, value in verify(result, case["actor"] == "axis").items():
            total[key] += value
    assert all(case["goldens"] for case in fixture["cases"]), "literal goldens missing"
    print("PASS inherited Reserve cycle entry: " + str(len(fixture["cases"])) +
        " traces/20 events, " + ", ".join(str(value) + " " + key for key, value in total.items()) +
        ", " + str(len(PIN_PATHS)) + " source pins")


if __name__ == "__main__":
    main()
