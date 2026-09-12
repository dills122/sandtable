#!/usr/bin/env python3
"""Creation-rooted released-I armed continuation evidence; no cycle repeat."""

from __future__ import annotations

import copy
import importlib.util
import json
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
FIXTURE = ROOT / "fixtures" / "combat-inherited-armed-continuation-v1.json"
INVENTORY = json.loads((ROOT / "combat-inherited-armed-continuation-v1.schema.json").read_text())
SOURCE_PATHS = [
    "docs/specs/combat-inherited-reserve-release-v1.schema.json",
    "docs/specs/verify-combat-inherited-reserve-release-v1.py",
    "docs/specs/fixtures/combat-inherited-reserve-release-v1.json",
    "docs/specs/combat-inherited-selection-v1.schema.json",
    "docs/specs/verify-combat-inherited-selection-v1.py",
    "docs/specs/fixtures/combat-inherited-selection-v1.json",
    "docs/specs/combat-selection-steps-v1.schema.json",
    "docs/specs/verify-combat-selection-steps-v1.py",
    "docs/specs/fixtures/combat-selection-steps-v1.json",
    "docs/specs/combat-sealed-round-v1.schema.json",
    "docs/specs/verify-combat-sealed-round-v1.py",
    "docs/specs/fixtures/combat-sealed-round-v1.json",
    "docs/specs/combat-result-settlement-v1.schema.json",
    "docs/specs/verify-combat-result-settlement-v1.py",
    "docs/specs/fixtures/combat-result-settlement-v1.json",
    "docs/specs/combat-snapshot-composition-v1.schema.json",
    "docs/specs/verify-combat-snapshot-composition-v1.py",
    "docs/specs/fixtures/combat-snapshot-composition-v1.json",
]
SUPPORT = [
    ("combat-selection-steps-v1", "combat-selection-steps-v1.json", 5),
    ("combat-sealed-round-v1", "combat-sealed-round-v1.json", 4),
    ("combat-result-settlement-v1", "combat-result-settlement-v1.json", 8),
    ("combat-snapshot-composition-v1", "combat-snapshot-composition-v1.json", 17),
]


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot load {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


irr = load_module("combat_inherited_reserve_release_v1",
    ROOT / "verify-combat-inherited-reserve-release-v1.py")
selection = load_module("combat_inherited_selection_v1",
    ROOT / "verify-combat-inherited-selection-v1.py")
encode, sha = irr.encode, irr.sha
SCHEMA = {
    name: [tuple(field.split(":")) for field in fields.split()]
    for name, fields in INVENTORY["objects"].items()
}


class Invalid(ValueError):
    def __init__(self, code):
        self.code = f"CMB-IAC-{code:03}"
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
        require(type(value) is list
            and len(value) <= INVENTORY["limits"]["arrayItems"], 1)
        for child in value:
            typed(child, kind[:-2], depth + 1)
    elif kind == "CandidateAssessment":
        try:
            selection.typed(value, kind, depth)
        except selection.Invalid as error:
            raise Invalid(1) from error
    else:
        try:
            irr.typed(value, kind, depth)
        except irr.Invalid as error:
            raise Invalid(1) from error


def canonical(value, kind):
    if kind.endswith("?"):
        return None if value is None else canonical(value, kind[:-1])
    if kind in SCHEMA:
        return {key: canonical(value[key], child) for key, child in SCHEMA[kind]}
    if kind.endswith("[]"):
        return [canonical(child, kind[:-2]) for child in value]
    if kind == "CandidateAssessment":
        return selection.canonical(value, kind)
    return irr.canonical(value, kind)


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


def leaves(value, path=()):
    if type(value) is dict:
        for key, child in value.items():
            yield from leaves(child, path + (key,))
    elif type(value) is list:
        for index, child in enumerate(value):
            yield from leaves(child, path + (index,))
    else:
        yield path, value


def changed(value, path, replacement):
    result = copy.deepcopy(value)
    target = result
    for step in path[:-1]:
        target = target[step]
    target[path[-1]] = replacement
    return result


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
        return value + ".changed"
    raise TypeError(type(value))


def release_case(actor):
    fixture = json.loads(irr.FIXTURE.read_text())
    return next(case for case in fixture["cases"] if case["actor"] == actor)


def source_trace(actor):
    case = release_case(actor)
    result = irr.trace(case)
    require(irr.goldens(result) == case["goldens"], 4)
    base, _, states, inputs, events = result
    terminal = states[-1]
    require(irr.read_control(irr.raw(terminal, "Control"), base, inputs, events) == terminal, 4)
    require(terminal["closed"] and terminal["release"]["stateVersion"] == 25, 4)
    return result


def support_contracts():
    values = []
    for contract_id, fixture_name, count in SUPPORT:
        data = (ROOT / "fixtures" / fixture_name).read_bytes()
        fixture = json.loads(data)
        actual = (sum(fixture["expected"].values())
            if contract_id == "combat-snapshot-composition-v1" else len(fixture["cases"]))
        require(actual == count, 4)
        values.append(dict(contractId=contract_id, fixtureHash=sha(data), evidenceCount=count))
    require(len(values) == INVENTORY["limits"]["supportContracts"], 7)
    return values


def element_for(world, actor):
    return next(element for element in world["elements"]
        if selection.WORLD.SIDES[element["elementId"]] == actor)


def proof_from(actor, result=None, verify_source=True):
    require(actor in ("axis", "commonwealth"), 2)
    result = source_trace(actor) if result is None else result
    base, source, states, inputs, events = result
    terminal = states[-1]
    inherited_control = source[2][-1]
    inherited = inherited_control["base"]
    if verify_source:
        require(irr.read_control(irr.raw(terminal, "Control"), base, inputs, events) == terminal, 4)
    cycle = inherited["cycle"]
    member = terminal["release"]["members"][0]
    progress = terminal["progress"]
    release_receipt = terminal["release"]["completionReceiptId"]
    movement_end = inherited_control["movementEnd"]
    movement_receipt = movement_end["completionReceiptId"]
    history = member["history"]
    next_movement = history["nextMovement"]
    require(terminal["closed"] and terminal["release"]["status"] == "completed"
        and release_receipt is not None, 6)
    require(cycle["ordinal"] == 1 and cycle["actingSide"] == actor
        and cycle["playerPhaseSlot"] == "first-acting-side", 5)
    require(len(progress) == 1 and progress[0]["eventType"] == "reserve-unit-disposition-recorded"
        and progress[0]["receiptId"] == history["releaseReceiptId"], 5)
    require(member["unit"]["originalSide"] == actor and member["status"] == "none"
        and member["baseCpa"] == 10
        and member["spentCp"] == {"numerator": 0, "denominator": 1}, 5)
    require(history["releasedType"] == "I" and history["releaseCycle"] == 1
        and history["cpaBasis"] == 10 and history["voluntaryCeiling"] == 10
        and history["offensiveCommitmentId"] is None, 5)
    require(next_movement == dict(scope=history["scope"], ordinal=2, status="pending",
        completionReceiptId=None) and history["scope"] == {
            "gameTurn": cycle["gameTurn"], "operationStage": cycle["operationStage"],
            "playerPhaseSlot": cycle["playerPhaseSlot"], "actingSide": actor}, 5)
    require(movement_end["scope"] == history["scope"] and movement_end["ordinal"] == 1
        and movement_end["excludedBefore"] == []
        and inherited_control["selection"]["candidateIds"] == []
        and inherited_control["selection"]["outcome"] == "no-selection", 5)
    projected = irr.release.project_world(inherited["world"], base["releaseBase"], terminal["release"])
    acting = element_for(projected, actor)
    defender_actor = "commonwealth" if actor == "axis" else "axis"
    defending = element_for(projected, defender_actor)
    require(acting["elementId"] == member["unit"]["elementId"]
        and acting["reserveStatus"] == "none" and acting["ammunition"]["points"] == 10, 5)
    require(sum(component["currentToe"] for component in acting["components"]) == 10
        and sum(component["currentToe"] for component in defending["components"]) == 10, 5)
    require({(item["unit"]["elementId"], item["locationId"])
        for item in movement_end["endLocations"]} == {
            (element["elementId"], element["currentLocationId"]) for element in projected["elements"]}, 5)
    require(acting["operationalState"]["vehicleBreakdownState"] is None
        and not acting["readiness"]["pinned"], 5)
    require(terminal["release"]["attackHistory"] == []
        and all(projected[key] == [] for key in ("brokenVehicleLots", "custodyLots", "guards",
            "futureObligations", "settlements")), 5)
    entry = dict(cycle=copy.deepcopy(cycle), world=projected,
        operationStageWeather=copy.deepcopy(inherited["operationStageWeather"]))
    assessment = selection.assessment(entry)
    require(assessment["actingSide"] == actor and assessment["normalWeather"]
        and assessment["adjacent"] and assessment["actingWithinVoluntaryCeiling"]
        and assessment["defendingWithinVoluntaryCeiling"]
        and len(assessment["candidateIds"]) == 1, 5)
    support = support_contracts()
    proof = dict(contractVersion=1,
        predecessorHash=sha(irr.raw(terminal, "Control")),
        currentCycle=copy.deepcopy(cycle), nextOrdinal=2,
        releaseCompletionReceiptId=release_receipt,
        movementCompletionReceiptId=movement_receipt,
        progress=copy.deepcopy(progress[0]), member=copy.deepcopy(member),
        retainedWorldHash=terminal["release"]["retainedWorldHash"],
        randomStateHash=sha(irr.release.raw(inherited["randomState"], "Random")),
        actingAmmunition=acting["ammunition"]["points"],
        actingToe=sum(component["currentToe"] for component in acting["components"]),
        defendingToe=sum(component["currentToe"] for component in defending["components"]),
        assessment=assessment, emptyMovementSupported=True, emptyBreakdownSupported=True,
        emptyPrestepsSupported=True, targetUseAvailable=True, offensiveUseAvailable=True,
        immediateObligationsClear=True, support=support,
        supportDigest=sha(raw(support, "SupportContract[]")), supported=True)
    return canonical(proof, "Proof")


def read_proof(data, actor, expected=None):
    value = parse(data, "Proof")
    expected = proof_from(actor) if expected is None else expected
    require(data == raw(expected, "Proof"), 6)
    return value


def rejected(call, code=None):
    try:
        call()
    except ValueError as error:
        if code is not None:
            assert isinstance(error, Invalid) and error.code == f"CMB-IAC-{code:03}"
    else:
        raise AssertionError("invalid armed-continuation evidence accepted")


def source_pins():
    repo = ROOT.parent.parent
    return [dict(path=path, sha256=sha((repo / path).read_bytes())) for path in SOURCE_PATHS]


def golden(proof):
    data = raw(proof, "Proof")
    return dict(bytes=len(data), sha256=sha(data), predecessorHash=proof["predecessorHash"],
        candidateId=proof["assessment"]["candidateIds"][0],
        supportDigest=proof["supportDigest"])


def source_mutations(actor, result):
    counts = 0

    def bad(mutator):
        nonlocal counts
        altered = copy.deepcopy(result)
        mutator(altered)
        rejected(lambda: proof_from(actor, altered, False))
        counts += 1

    def inherited(altered):
        return altered[1][2][-1]["base"]

    def acting_element(altered):
        return element_for(inherited(altered)["world"], actor)

    def sync_world(altered):
        world_hash = sha(irr.release.r.raw(inherited(altered)["world"], "World"))
        altered[0]["releaseBase"]["retainedWorldHash"] = world_hash
        altered[2][-1]["release"]["retainedWorldHash"] = world_hash

    def ammunition(altered):
        acting_element(altered)["ammunition"]["points"] = 9
        sync_world(altered)

    def location(altered):
        acting_element(altered)["currentLocationId"] = (
            "assault-east" if actor == "axis" else "assault-west")
        sync_world(altered)

    bad(lambda altered: inherited(altered)["operationStageWeather"][0].__setitem__("kind", "sandstorm"))
    bad(ammunition)
    bad(lambda altered: inherited(altered)["world"]["futureObligations"].append({}))
    bad(location)
    bad(lambda altered: altered[2][-1]["release"]["members"][0]["history"].__setitem__(
        "offensiveCommitmentId", "commitment.used"))
    bad(lambda altered: altered[2][-1]["release"]["members"][0]["history"]["nextMovement"].__setitem__(
        "status", "expired"))
    bad(lambda altered: altered[2][-1]["release"]["attackHistory"].append({}))
    return counts


def verify_case(case):
    actor = case["actor"]
    result = source_trace(actor)
    proof = proof_from(actor, result)
    require(case["proof"] == proof and case["golden"] == golden(proof), 6)
    data = raw(proof, "Proof")
    require(read_proof(data, actor) == proof, 6)
    counts = dict(readbacks=1, mutations=0, raw=0, boundaries=0)
    for path, leaf in leaves(proof):
        altered = changed(proof, path, different(leaf))
        try:
            encoded = raw(altered, "Proof")
        except Invalid:
            encoded = encode(altered)
        rejected(lambda encoded=encoded: read_proof(encoded, actor, proof))
        counts["mutations"] += 1
    for bad in (data + b"\n", b"\xef\xbb\xbf" + data, b" " + data,
            json.dumps(proof, sort_keys=True).encode("ascii")):
        rejected(lambda bad=bad: read_proof(bad, actor, proof))
        counts["raw"] += 1
    other = "commonwealth" if actor == "axis" else "axis"
    rejected(lambda: read_proof(data, other), 6)
    oversized = copy.deepcopy(proof)
    oversized["support"] = oversized["support"] * 129
    rejected(lambda: raw(oversized, "Proof"), 1)
    counts["boundaries"] += 2 + source_mutations(actor, result)
    return proof, counts


def generated_fixture():
    cases = []
    for actor in ("axis", "commonwealth"):
        proof = proof_from(actor)
        cases.append(dict(actor=actor, proof=proof, golden=golden(proof)))
    return dict(contract="combat-inherited-armed-continuation-v1", cases=cases,
        sourcePins=source_pins(), goldenMethod="canonical ASCII JSON; SHA-256 with sha256: prefix")


def main():
    if "--goldens" in sys.argv:
        print(json.dumps(generated_fixture(), indent=2))
        return
    fixture = json.loads(FIXTURE.read_text())
    require(set(fixture) == {"contract", "cases", "sourcePins", "goldenMethod"}
        and fixture["contract"] == "combat-inherited-armed-continuation-v1", 1)
    require([case["actor"] for case in fixture["cases"]] == ["axis", "commonwealth"]
        and len(fixture["cases"]) <= INVENTORY["limits"]["proofs"], 7)
    require(fixture["sourcePins"] == source_pins(), 4)
    totals = dict(readbacks=0, mutations=0, raw=0, boundaries=0)
    proofs = []
    for case in fixture["cases"]:
        proof, counts = verify_case(case)
        proofs.append(proof)
        for key, value in counts.items():
            totals[key] += value
    require(proofs[0]["assessment"]["candidateIds"]
        != proofs[1]["assessment"]["candidateIds"], 5)
    print("combat inherited armed continuation v1: "
        f"2 proofs, {totals['readbacks']} readbacks, {totals['mutations']} deep mutations, "
        f"{totals['raw']} raw variants, {totals['boundaries']} boundary rejects")


if __name__ == "__main__":
    main()
