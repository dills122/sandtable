#!/usr/bin/env python3
"""Creation-rooted inherited Reserve Release evidence; no repeat or runtime admission."""

from __future__ import annotations

import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
FIXTURE = ROOT / "fixtures" / "combat-inherited-reserve-release-v1.json"
PARENT_FIXTURE = ROOT / "fixtures" / "combat-inherited-reserve-cycle-v1.json"
INVENTORY = json.loads((ROOT / "combat-inherited-reserve-release-v1.schema.json").read_text())
SOURCE_PATHS = [
    "docs/specs/combat-inherited-reserve-cycle-v1.schema.json",
    "docs/specs/verify-combat-inherited-reserve-cycle-v1.py",
    "docs/specs/fixtures/combat-inherited-reserve-cycle-v1.json",
    "docs/specs/combat-reserve-release-v1.schema.json",
    "docs/specs/verify-combat-reserve-release-v1.py",
    "docs/specs/combat-cycle-control-v1.schema.json",
    "docs/specs/verify-combat-cycle-control-v1.py",
]


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot load {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


parent = load_module("combat_inherited_reserve_cycle_v1",
    ROOT / "verify-combat-inherited-reserve-cycle-v1.py")
release = load_module("combat_reserve_release_v1",
    ROOT / "verify-combat-reserve-release-v1.py")
encode, sha = release.encode, release.sha
SCHEMA = {
    name: [tuple(field.split(":")) for field in fields.split()]
    for name, fields in INVENTORY["objects"].items()
}


class Invalid(ValueError):
    def __init__(self, code):
        self.code = f"CMB-IRR-{code:03}"
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
        require(type(value) is dict and set(value) == {key for key, _ in SCHEMA[kind]}, 1)
        for key, child in SCHEMA[kind]:
            typed(value[key], child, depth + 1)
    elif kind.endswith("[]"):
        require(type(value) is list and len(value) <= 512, 1)
        for child in value:
            typed(child, kind[:-2], depth + 1)
    else:
        try:
            release.typed(value, kind, depth)
        except release.Invalid as error:
            raise Invalid(int(error.code[-3:])) from error


def canonical(value, kind):
    if kind.endswith("?"):
        return None if value is None else canonical(value, kind[:-1])
    if kind in SCHEMA:
        return {key: canonical(value[key], child) for key, child in SCHEMA[kind]}
    if kind.endswith("[]"):
        return [canonical(child, kind[:-2]) for child in value]
    return release.canonical(value, kind)


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


def different(value):
    if value is None:
        return "unexpected"
    if type(value) is bool:
        return not value
    if type(value) is int:
        return value + 1
    if type(value) is str:
        if value.startswith("sha256:"):
            return "sha256:" + ("0" * 64 if value != "sha256:" + "0" * 64 else "1" * 64)
        if value.startswith("rr."):
            return "rr." + ("0" * 64 if value != "rr." + "0" * 64 else "1" * 64)
        if value.startswith("rel."):
            return "rel." + ("0" * 64 if value != "rel." + "0" * 64 else "1" * 64)
        return value + ".changed"
    if type(value) is list:
        return value + [copy.deepcopy(value[0])] if value else ["unexpected"]
    raise TypeError(type(value))


def translated(call):
    try:
        return call()
    except release.Invalid as error:
        raise Invalid(int(error.code[-3:])) from error


def parent_case(case):
    return next(row for row in json.loads(PARENT_FIXTURE.read_text())["cases"]
        if (row["actor"], row["seed"], row["choice"], row["reserve"]) ==
        (case["actor"], case["seed"], case["choice"], case["reserve"]))


def source_trace(case):
    inherited = parent_case(case)
    result = parent.trace(inherited)
    assert parent.goldens(result) == inherited["goldens"]
    terminal = result[2][-1]
    assert terminal["closed"] and terminal["stateVersion"] == 22
    assert terminal["position"]["segmentId"] == "land.segment.reserve-release"
    return result


def derive_base(case):
    result = source_trace(case)
    terminal = result[2][-1]
    inherited = terminal["base"]
    member = inherited["members"][0]
    release_base = dict(
        contractVersion=1,
        profile="inherited-reserve-cycle",
        cycle=copy.deepcopy(inherited["cycle"]),
        firstActingSide=inherited["firstActingSide"],
        positionId=terminal["position"]["positionId"],
        priorVersion=terminal["stateVersion"],
        priorPrefix=terminal["prefix"],
        combatCompletionReceiptId=terminal["stepReceipts"][-1],
        retainedWorldHash=sha(parent.rd.raw(inherited["world"], "World")),
        randomState=copy.deepcopy(inherited["randomState"]),
        acceptedHighWater=None,
        members=copy.deepcopy(inherited["members"]),
        attackHistory=[],
    )
    translated(lambda: release.raw(release_base, "ReleaseBase"))
    require(release_base["cycle"]["ordinal"] == 1
        and release_base["cycle"]["actingSide"] == case["actor"], 4)
    require(release_base["firstActingSide"] == case["actor"], 4)
    require(member["status"] == "I" and member["baseCpa"] == 10
        and member["spentCp"] == {"numerator": 0, "denominator": 1}, 4)
    require(member["history"]["designationReceiptId"] is not None
        and member["history"]["releasedType"] is None, 4)
    require(release_base["priorVersion"] == 22 and release_base["acceptedHighWater"] is None, 4)
    return dict(contractVersion=1,
        predecessorHash=sha(parent.raw(terminal, "Control")), releaseBase=release_base), result


def read_base(data, case, expected=None):
    value = parse(data, "Base")
    result = None
    if expected is None:
        expected, result = derive_base(case)
    require(data == raw(expected, "Base"), 4)
    return value, result


def initial(base):
    release_state = translated(lambda: release.initial(base["releaseBase"]))
    value = dict(contractVersion=1, baseHash=sha(raw(base, "Base")),
        release=release_state, progress=[], closed=False)
    validate(value, base)
    return value


def validate(value, base):
    typed(value, "Control")
    state = value["release"]
    release_base = base["releaseBase"]
    require(value["contractVersion"] == 1 and value["baseHash"] == sha(raw(base, "Base")), 4)
    require(state["baseHash"] == sha(release.raw(release_base, "ReleaseBase")), 4)
    require(state["retainedWorldHash"] == release_base["retainedWorldHash"]
        and state["randomState"] == release_base["randomState"]
        and state["attackHistory"] == release_base["attackHistory"], 4)
    delta = state["stateVersion"] - release_base["priorVersion"]
    require(0 <= delta <= INVENTORY["limits"]["events"]
        and len(state["receipts"]) == delta <= INVENTORY["limits"]["receipts"], 6)
    require(value["closed"] == (state["status"] == "completed"), 6)
    require(len(value["progress"]) <= INVENTORY["limits"]["progress"], 6)
    member = state["members"][0]
    if value["progress"]:
        require(len(state["dispositions"]) == 1 and len(value["progress"]) == 1, 6)
        disposition = state["dispositions"][0]
        receipt = next((item for item in state["receipts"]
            if item["receiptId"] == disposition["receiptId"]), None)
        progress = value["progress"][0]
        require(receipt is not None and progress == dict(
            eventType="reserve-unit-disposition-recorded",
            receiptId=disposition["receiptId"], eventHash=receipt["eventHash"]), 6)
        if disposition["choice"] == "release-I":
            history = member["history"]
            expected_scope = release.scope(release_base["cycle"])
            require(member["status"] == "none" and history["releasedType"] == "I"
                and history["releaseCycle"] == 1 and history["cpaBasis"] == 10
                and history["voluntaryCeiling"] == 10, 4)
            require(history["nextMovement"] == dict(scope=expected_scope, ordinal=2,
                status="pending", completionReceiptId=None), 4)
        else:
            require(disposition["choice"] == "convert-to-II" and member["status"] == "II"
                and member["history"]["conversionReceiptId"] == disposition["receiptId"]
                and member["history"]["nextMovement"] is None, 4)
    else:
        require(not state["dispositions"] and member["status"] == "I"
            and member["history"]["releasedType"] is None, 4)
    return value


def transition(base, prior, inp):
    validate(prior, base)
    typed(inp, "ReleaseInput")
    command = inp["command"]
    require(command["kind"] in ("open", "choose", "complete", "expire",
        "unavailable", "fallback-step"), 3)
    if command["kind"] == "choose":
        require(command["choice"] == "release-I", 3)
    try:
        next_release, event_data, receipt_id = release.transition(
            base["releaseBase"], prior["release"], inp)
    except release.Invalid as error:
        raise Invalid(int(error.code[-3:])) from error
    if event_data is None:
        return prior, None, receipt_id
    after = copy.deepcopy(prior)
    after["release"] = next_release
    event = release.parse(event_data, "ReleaseEvent")
    if event["eventType"] == "reserve-unit-disposition-recorded":
        after["progress"].append(dict(eventType=event["eventType"],
            receiptId=event["receiptId"], eventHash=sha(event_data)))
    after["closed"] = next_release["status"] == "completed"
    validate(after, base)
    return after, event_data, receipt_id


def replay(base, inputs, events, length=None):
    require(type(inputs) is list and type(events) is list and len(inputs) == len(events)
        and len(events) <= INVENTORY["limits"]["events"], 1)
    require(length is None or type(length) is int and 0 <= length <= len(events), 2)
    state = initial(base)
    for inp, event_data in zip(inputs[:length], events[:length]):
        state = read_event(event_data, base, state, inp)
    return state


def read_event(data, base, prior, inp):
    translated(lambda: release.parse(data, "ReleaseEvent"))
    state, expected, _ = transition(base, prior, inp)
    require(data == expected, 6)
    return state


def read_control(data, base, inputs, events, length=None):
    value = parse(data, "Control")
    expected = replay(base, inputs, events, length)
    require(data == raw(expected, "Control"), 6)
    return value


def apply(case, inputs, events, inp, cached_control=None):
    expected, _ = derive_base(case)
    base, _ = read_base(raw(expected, "Base"), case, expected)
    prior = replay(base, inputs, events)
    if cached_control is not None:
        require(cached_control == raw(prior, "Control"), 6)
    return transition(base, prior, inp)


def trace(case):
    base, source = derive_base(case)
    state = initial(base)
    states = [state]
    inputs = []
    events = []

    def send(kind, actor, now=None, choice=None, available=True):
        nonlocal state
        command = release.command(state["release"], kind, choice)
        inp = release.trusted(command, actor, now, available)
        before = copy.deepcopy(state)
        state, event_data, _ = transition(base, state, inp)
        assert before == states[-1] and event_data is not None
        inputs.append(inp)
        events.append(event_data)
        states.append(state)

    send("open", "system", case["openingAt"])
    send("choose", case["actor"], case["choiceAt"], "release-I")
    send("complete", "system")
    inherited = source[2][-1]["base"]
    assert state["closed"] and state["release"]["stateVersion"] == 25
    assert len(state["progress"]) == 1
    assert state["release"]["acceptedHighWater"] == case["choiceAt"]
    assert state["release"]["members"][0]["status"] == "none"
    projected = release.project_world(inherited["world"], base["releaseBase"], state["release"])
    changed = next(item for item in projected["elements"]
        if item["elementId"] == state["release"]["members"][0]["unit"]["elementId"])
    assert changed["reserveStatus"] == "none"
    before_world = copy.deepcopy(inherited["world"])
    next(item for item in before_world["elements"]
        if item["elementId"] == changed["elementId"])["reserveStatus"] = "none"
    assert projected == before_world
    assert [release.parse(event, "ReleaseEvent")["eventType"] for event in events] == [
        "reserve-release-opened", "reserve-unit-disposition-recorded",
        "reserve-release-completed"]
    return base, source, states, inputs, events


def goldens(result):
    base, _, states, _, events = result
    frames = [dict(cut=index, bytes=len(raw(state, "Control")), sha256=sha(raw(state, "Control")))
        for index, state in enumerate(states)]
    return dict(predecessorHash=base["predecessorHash"], baseHash=sha(raw(base, "Base")),
        frames=frames, eventHashes=[sha(event) for event in events],
        terminalEvent=events[-1].decode("ascii"))


def rejected(call, code=None):
    try:
        call()
    except Invalid as error:
        if code is not None:
            assert error.code == f"CMB-IRR-{code:03}", (error.code, code)
    else:
        raise AssertionError("invalid inherited Reserve Release accepted")


def verify_case(case, result):
    base, _, states, inputs, events = result
    counts = dict(cuts=0, retries=0, mutations=0, raw=0, boundaries=0, recovery=0)
    for path, leaf in parent.rd.pre.leaves(base):
        altered = parent.rd.pre.changed(base, path, different(leaf))
        try:
            encoded = raw(altered, "Base")
        except Invalid:
            encoded = encode(altered)
        rejected(lambda encoded=encoded: read_base(encoded, case, base))
        counts["mutations"] += 1
    for index, state in enumerate(states):
        assert read_control(raw(state, "Control"), base, inputs, events, index) == state
        counts["cuts"] += 1
    final = states[-1]
    for inp, receipt in zip(inputs, final["release"]["receipts"]):
        same, event_data, receipt_id = transition(base, final, inp)
        assert same is final and event_data is None and receipt_id == receipt["receiptId"]
        counts["retries"] += 1
    prior = states[0]
    for index, (inp, event_data) in enumerate(zip(inputs, events)):
        event = release.parse(event_data, "ReleaseEvent")
        for field in ("priorVersion", "stateVersion", "priorPrefix", "configurationHash",
                "baseHash", "cycleId", "releaseId", "receiptId"):
            altered = copy.deepcopy(event)
            altered[field] = different(altered[field])
            rejected(lambda altered=altered, prior=prior, inp=inp:
                read_event(release.raw(altered, "ReleaseEvent"), base, prior, inp), 6)
            counts["mutations"] += 1
        altered = copy.deepcopy(event)
        altered["input"]["actor"] = "axis" if event["input"]["actor"] != "axis" else "commonwealth"
        rejected(lambda altered=altered, prior=prior, inp=inp:
            read_event(release.raw(altered, "ReleaseEvent"), base, prior, inp), 6)
        counts["mutations"] += 1
        for bad in (event_data + b"\n", b"\xef\xbb\xbf" + event_data,
                b" " + event_data, json.dumps(event, sort_keys=True).encode("ascii")):
            rejected(lambda bad=bad: translated(lambda: release.parse(bad, "ReleaseEvent")))
            counts["raw"] += 1
        prior = states[index + 1]
    state_mutations = [
        lambda value: value.__setitem__("baseHash", "sha256:" + "0" * 64),
        lambda value: value["release"].__setitem__("prefix", "sha256:" + "0" * 64),
        lambda value: value["release"].__setitem__("stateVersion", 24),
        lambda value: value["release"]["members"][0].__setitem__("status", "I"),
        lambda value: value["release"]["members"][0]["history"]["nextMovement"].__setitem__("ordinal", 3),
        lambda value: value["progress"][0].__setitem__("receiptId", "changed.receipt"),
        lambda value: value.__setitem__("closed", False),
    ]
    for mutate in state_mutations:
        altered = copy.deepcopy(final)
        mutate(altered)
        rejected(lambda altered=altered: read_control(raw(altered, "Control"),
            base, inputs, events), 6)
        counts["mutations"] += 1
    for index, (inp, event_data) in enumerate(zip(inputs, events)):
        event = release.parse(event_data, "ReleaseEvent")
        for path, leaf in parent.rd.pre.leaves(event):
            altered = parent.rd.pre.changed(event, path, different(leaf))
            if path != ("receiptId",):
                unsigned = copy.deepcopy(altered)
                unsigned.pop("receiptId")
                altered["receiptId"] = "rr." + release.digest("receipt", unsigned)
            try:
                encoded = release.raw(altered, "ReleaseEvent")
            except release.Invalid:
                encoded = encode(altered)
            rejected(lambda encoded=encoded, prior=states[index], inp=inp:
                read_event(encoded, base, prior, inp))
            counts["mutations"] += 1
    for state, length in ((states[0], 0), (states[-1], 3)):
        for path, leaf in parent.rd.pre.leaves(state):
            altered = parent.rd.pre.changed(state, path, different(leaf))
            try:
                encoded = raw(altered, "Control")
            except Invalid:
                encoded = encode(altered)
            rejected(lambda encoded=encoded, length=length:
                read_control(encoded, base, inputs, events, length))
            counts["mutations"] += 1
    opened = states[1]
    wrong = release.trusted(release.command(opened["release"], "choose", "release-I"),
        "commonwealth" if case["actor"] == "axis" else "axis", case["choiceAt"])
    rejected(lambda: transition(base, opened, wrong), 4)
    converted = release.trusted(release.command(opened["release"], "choose", "convert-to-II"),
        case["actor"], case["choiceAt"])
    rejected(lambda: transition(base, opened, converted), 3)
    late = release.trusted(release.command(opened["release"], "choose", "release-I"),
        case["actor"], opened["release"]["timing"]["deadlineUnixMilliseconds"])
    rejected(lambda: transition(base, opened, late), 5)
    stale_command = release.command(opened["release"], "choose", "release-I")
    stale_command["expectedPriorVersion"] -= 1
    rejected(lambda: transition(base, opened,
        release.trusted(stale_command, case["actor"], case["choiceAt"])), 6)
    repeated = copy.deepcopy(events) + [events[-1]]
    rejected(lambda: replay(base, inputs + [inputs[-1]], repeated), 1)
    bad_cache = raw(states[0], "Control")
    rejected(lambda: apply(case, inputs[:1], events[:1], inputs[1], bad_cache), 6)
    counts["boundaries"] += 6
    rejected(lambda: replay(base, [inputs[1]], [events[1]]), 6)
    rejected(lambda: replay(base, inputs[:2] + [inputs[1]],
        events[:2] + [events[1]]), 6)
    unknown = copy.deepcopy(inputs[1])
    unknown["command"]["choice"] = "unknown-choice"
    rejected(lambda: transition(base, opened, unknown), 3)
    for kind in ("complete-release", "repeat", "move", "snapshot", "runtime"):
        unsupported = release.trusted(release.command(opened["release"], kind), "system")
        rejected(lambda unsupported=unsupported: transition(base, opened, unsupported), 3)
    counts["boundaries"] += 8
    for mode in ("unavailable", "opening-clock-loss", "clock-regression",
            "clock-unavailable", "expiry"):
        expected_reason = {
            "unavailable": "controller-unavailable",
            "opening-clock-loss": "opening-clock-unavailable",
            "clock-regression": "clock-unavailable",
            "clock-unavailable": "clock-unavailable",
            "expiry": "deadline",
        }[mode]
        state = initial(base)
        open_input = release.trusted(release.command(state["release"], "open"), "system",
            None if mode == "opening-clock-loss" else case["openingAt"],
            mode != "opening-clock-loss")
        state, _, _ = transition(base, state, open_input)
        if mode == "unavailable":
            next_input = release.trusted(release.command(state["release"], "unavailable"),
                "system", case["choiceAt"])
        elif mode == "expiry":
            next_input = release.trusted(release.command(state["release"], "expire"), "system",
                state["release"]["timing"]["deadlineUnixMilliseconds"])
        elif mode == "clock-regression":
            next_input = release.trusted(release.command(state["release"], "choose", "release-I"),
                case["actor"], case["openingAt"] - 1)
        elif mode == "clock-unavailable":
            next_input = release.trusted(release.command(state["release"], "choose", "release-I"),
                case["actor"], case["choiceAt"], False)
        else:
            next_input = release.trusted(release.command(state["release"], "fallback-step"),
                "system")
        state, _, _ = transition(base, state, next_input)
        finish = release.trusted(release.command(state["release"], "fallback-step"), "system")
        state, _, _ = transition(base, state, finish)
        assert state["closed"] and state["release"]["fallbackLocked"]
        assert state["release"]["fallbackReason"] == expected_reason
        assert state["release"]["members"][0]["status"] == "II"
        assert state["release"]["members"][0]["history"]["nextMovement"] is None
        counts["recovery"] += 1
    overflow_base = copy.deepcopy(base)
    overflow_base["releaseBase"]["priorVersion"] = 2**63 - 1
    overflow = initial(overflow_base)
    overflow_input = release.trusted(release.command(overflow["release"], "open"),
        "system", case["openingAt"])
    rejected(lambda: transition(overflow_base, overflow, overflow_input), 7)
    counts["boundaries"] += 1
    return counts


def fixture_shape(fixture, require_goldens=True):
    require(set(fixture) == {"contractVersion", "cases", "sourcePins"}
        and fixture["contractVersion"] == 1, 9)
    require(len(fixture["cases"]) == 2 and {
        (case["actor"], case["seed"], case["choice"], case["reserve"])
        for case in fixture["cases"]
    } == {("axis", 1, "act-first", "I"), ("commonwealth", 1, "act-last", "I")}, 9)
    for case in fixture["cases"]:
        require(set(case) == {"actor", "seed", "choice", "reserve", "openingAt",
            "choiceAt", "goldens"} and case["openingAt"] < case["choiceAt"], 9)
        if require_goldens:
            require(bool(case["goldens"]), 9)
    require([pin["path"] for pin in fixture["sourcePins"]] == SOURCE_PATHS, 9)
    repo = ROOT.parent.parent
    for pin in fixture["sourcePins"]:
        digest = "sha256:" + hashlib.sha256((repo / pin["path"]).read_bytes()).hexdigest()
        require(pin["sha256"] == digest, 9)


def main():
    fixture = json.loads(FIXTURE.read_text())
    generating = "--goldens" in sys.argv
    fixture_shape(fixture, not generating)
    results = [trace(case) for case in fixture["cases"]]
    counts = dict(cuts=0, retries=0, mutations=0, raw=0, boundaries=0, recovery=0)
    for case, result in zip(fixture["cases"], results):
        observed = verify_case(case, result)
        for key, value in observed.items():
            counts[key] += value
    other_base = results[1][0]
    rejected(lambda: read_control(raw(results[0][2][-1], "Control"), other_base,
        results[0][3], results[0][4]), 4)
    counts["boundaries"] += 1
    if generating:
        print(json.dumps({"goldens": [goldens(result) for result in results]}, indent=2))
        return
    for case, result in zip(fixture["cases"], results):
        assert case["goldens"] == goldens(result)
    print("PASS inherited Reserve Release: " + str(len(results)) + " traces, "
        + str(len(results) * 3) + " events, " + str(counts["cuts"]) + " cuts, "
        + str(counts["retries"]) + " retries, " + str(counts["mutations"]) + " mutations, "
        + str(counts["raw"]) + " raw rejections, " + str(counts["boundaries"])
        + " boundaries, " + str(counts["recovery"]) + " recovery paths, "
        + str(len(SOURCE_PATHS)) + " source pins")


if __name__ == "__main__":
    main()
