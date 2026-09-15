#!/usr/bin/env python3
"""D2c.4 creation-rooted authority composition oracle; contract evidence only."""

from __future__ import annotations

import copy
import hashlib
import importlib.util
import json
import sys
from collections import Counter
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
REPO = ROOT.parent.parent
FIXTURE = ROOT / "fixtures/combat-authority-composition-v1.json"
INVENTORY = json.loads((ROOT / "combat-authority-composition-v1.schema.json").read_text())
SCHEMA = {
    name: [tuple(field.split(":", 1)) for field in fields.split()]
    for name, fields in INVENTORY["objects"].items()
}
SOURCE_PATHS = [
    "docs/specs/combat-world-settlement-v1.schema.json",
    "docs/specs/combat-authority-envelope-v1.schema.json",
    "docs/specs/combat-cycle-sequence-v1.schema.json",
    "docs/specs/combat-snapshot-composition-v1.schema.json",
]
FAMILIES = [
    "combat-inherited-no-attack-v1",
    "combat-inherited-reaction-closure-v1",
    "combat-inherited-reaction-active-fallback-v1",
    "combat-inherited-reaction-movement-completion-v1",
    "combat-inherited-reserve-cycle-v1",
    "combat-inherited-reserve-release-v1",
    "combat-inherited-armed-continuation-v1",
    "combat-inherited-cycle-control-v1",
    "combat-inherited-reserve-movement-completion-v1",
]
for family in FAMILIES:
    SOURCE_PATHS.extend([
        f"docs/specs/{family}.schema.json",
        f"docs/specs/fixtures/{family}.json",
        f"docs/specs/verify-{family}.py",
    ])


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot load {filename}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


noattack = load("composition_noattack", "verify-combat-inherited-no-attack-v1.py")
direct = load("composition_direct", "verify-combat-inherited-reaction-closure-v1.py")
fallback = load("composition_fallback", "verify-combat-inherited-reaction-active-fallback-v1.py")
reaction_complete = load("composition_reaction_complete",
    "verify-combat-inherited-reaction-movement-completion-v1.py")
reserve_cycle = load("composition_reserve_cycle", "verify-combat-inherited-reserve-cycle-v1.py")
reserve_release = load("composition_reserve_release",
    "verify-combat-inherited-reserve-release-v1.py")
armed = load("composition_armed", "verify-combat-inherited-armed-continuation-v1.py")
cycle_control = load("composition_cycle", "verify-combat-inherited-cycle-control-v1.py")
reserve_movement = load("composition_reserve_movement",
    "verify-combat-inherited-reserve-movement-completion-v1.py")
CORE = reaction_complete.irl


class Invalid(ValueError):
    def __init__(self, code):
        self.code = f"CMB-ACM-{code:03}"
        super().__init__(self.code)


def require(ok, code):
    if not ok:
        raise Invalid(code)


def encode(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("ascii")


def sha(data):
    return "sha256:" + hashlib.sha256(data).hexdigest()


def typed(value, kind, depth=0):
    require(depth <= INVENTORY["limits"]["depth"], 1)
    if kind in SCHEMA:
        require(type(value) is dict and set(value) == {key for key, _ in SCHEMA[kind]}, 1)
        for key, child in SCHEMA[kind]:
            typed(value[key], child, depth + 1)
    elif kind.endswith("[]"):
        require(type(value) is list
            and len(value) <= INVENTORY["limits"]["arrayItems"], 1)
        for child in value:
            typed(child, kind[:-2], depth + 1)
    elif kind in ("int", "long"):
        require(type(value) is int and 0 <= value < 2**63, 2)
    elif kind == "bool":
        require(type(value) is bool, 2)
    elif kind == "side":
        require(value in ("axis", "commonwealth"), 2)
    elif kind == "rawHash":
        require(type(value) is str and len(value) == 64
            and all(char in "0123456789abcdef" for char in value), 2)
    elif kind == "hash":
        require(type(value) is str and value.startswith("sha256:") and len(value) == 71
            and all(char in "0123456789abcdef" for char in value[7:]), 2)
    elif kind == "id":
        require(type(value) is str and 0 < len(value) <= 512
            and value.isascii() and all(ord(char) >= 32 for char in value), 2)
    elif kind == "ascii":
        require(type(value) is str and value.isascii()
            and len(value.encode("ascii")) <= INVENTORY["limits"]["bytes"], 2)
    elif kind in ("World", "Random"):
        try:
            CORE.typed(value, kind, depth)
        except (ValueError, KeyError, TypeError) as error:
            raise Invalid(1) from error
    else:
        raise Invalid(1)


def canonical(value, kind):
    if kind in SCHEMA:
        return {key: canonical(value[key], child) for key, child in SCHEMA[kind]}
    if kind.endswith("[]"):
        return [canonical(child, kind[:-2]) for child in value]
    if kind in ("World", "Random"):
        return CORE.canonical(value, kind)
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
        value = json.loads(data.decode("ascii"), object_pairs_hook=pairs,
            parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error:
        raise Invalid(1) from error
    require(raw(value, kind) == data, 8)
    return value


def fragment(contract, kind, data):
    require(type(data) is bytes and 0 < len(data) <= INVENTORY["limits"]["bytes"], 1)
    try:
        value = json.loads(data.decode("ascii"),
            parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error:
        raise Invalid(1) from error
    require(encode(value) == data, 8)
    fragment_depth, fragment_array = measure(value)
    require(fragment_depth <= INVENTORY["limits"]["depth"]
        and fragment_array <= INVENTORY["limits"]["arrayItems"], 1)
    result = dict(contract=contract, type=kind, canonicalJson=data.decode("ascii"),
        bytes=len(data), sha256=sha(data))
    typed(result, "CanonicalFragment")
    return result


def value_fragment(contract, kind, value):
    return fragment(contract, kind, encode(value))


def measure(value, depth=0):
    maximum_depth = depth
    maximum_array = len(value) if type(value) is list else 0
    children = value.values() if type(value) is dict else value if type(value) is list else []
    for child in children:
        child_depth, child_array = measure(child, depth + 1)
        maximum_depth = max(maximum_depth, child_depth)
        maximum_array = max(maximum_array, child_array)
    return maximum_depth, maximum_array


def collect_receipts(value):
    found = {}

    def visit(node):
        if type(node) is dict:
            receipts = node.get("receipts")
            if type(receipts) is list:
                for receipt in receipts:
                    if type(receipt) is dict and type(receipt.get("receiptId")) is str:
                        found[receipt["receiptId"]] = copy.deepcopy(receipt)
            for child in node.values():
                visit(child)
        elif type(node) in (list, tuple):
            for child in node:
                visit(child)

    visit(value)
    return sorted(found.values(), key=lambda item: (item.get("stateVersion", -1), item["receiptId"]))


def obligations(world):
    keys = ("brokenVehicleLots", "cohesionCauses", "relationships", "custodyLots", "guards",
        "replacementEntitlements", "futureObligations", "settlements")
    return {key: copy.deepcopy(world[key]) for key in keys}


def arms_from(value, **extra):
    keys = ("members", "tracks", "actualProgressRefs", "reactionWindow", "breakdownFlow",
        "movementEnd", "attackHistory", "targetUses", "nextCycleProgress", "releaseMember",
        "release", "selection", "stepIndex", "stepReceipts", "status", "closure", "closed")
    result = {key: copy.deepcopy(value[key]) for key in keys if key in value}
    result.update(copy.deepcopy(extra))
    return result


def source(contract, family, actor, case_id, state_type, terminal, terminal_data, root,
        state_version, prefix, world, random_state, cycle, position, arms, receipt_source):
    return dict(contract=contract, family=family, actor=actor, caseId=case_id,
        stateType=state_type, terminal=copy.deepcopy(terminal), terminalData=terminal_data,
        root=copy.deepcopy(root), stateVersion=state_version, prefix=prefix,
        world=copy.deepcopy(world), randomState=copy.deepcopy(random_state),
        cycle=copy.deepcopy(cycle), position=copy.deepcopy(position),
        arms=copy.deepcopy(arms), receipts=collect_receipts(receipt_source))


def verify_golden(module, result, case):
    expected = case.get("goldens", case.get("golden"))
    actual = module.goldens(result) if hasattr(module, "goldens") else module.golden(result)
    require(actual == expected, 4)


def noattack_sources():
    fixture = json.loads(noattack.FIXTURE.read_text())
    result = []
    for case in fixture["cases"]:
        traced = noattack.trace(case)
        verify_golden(noattack, traced, case)
        terminal = traced[3][-1]
        entry = terminal["selection"]["boundary"]["entry"]
        receipts = (entry["receipts"], terminal["selection"]["receipts"], terminal["receipts"])
        case_id = f'{case["actor"]}-cp{case["expectedCp"]}-cohesion{case["expectedCohesion"]}'
        result.append(source("combat-inherited-no-attack-v1", "ordinary-no-attack",
            case["actor"], case_id, "Control", terminal, noattack.raw(terminal, "Control"), entry,
            terminal["stateVersion"], terminal["prefix"], entry["world"], entry["randomState"],
            entry["cycle"], terminal["position"], arms_from(terminal,
                inheritedBreakdownFlow=entry["breakdownFlow"], members=entry["members"],
                movementEnd=entry["movementEnd"], progress=entry["actualProgressRefs"]), receipts))
    return result


def direct_sources():
    fixture = json.loads(direct.FIXTURE.read_text())
    result = []
    for case in fixture["cases"]:
        traced = direct.trace(case)
        verify_golden(direct, traced, case)
        terminal = traced[-1]
        actor = terminal["firstActingSide"]
        result.append(source("combat-inherited-reaction-closure-v1", "reaction-direct-close",
            actor, case["name"], "LifecycleState", terminal,
            direct.irl.raw(terminal, "LifecycleState"), terminal, terminal["stateVersion"],
            terminal["prefix"], terminal["world"], terminal["randomState"], terminal["cycle"],
            terminal["currentPosition"], arms_from(terminal, closeReason=case["reason"]), terminal))
    return result


def fallback_sources():
    fixture = json.loads(fallback.FIXTURE.read_text())
    result = []
    for case in fixture["cases"]:
        traced = fallback.trace(case)
        verify_golden(fallback, traced, case)
        terminal = traced[3][-1]
        actor = terminal["firstActingSide"]
        result.append(source("combat-inherited-reaction-active-fallback-v1",
            "reaction-active-fallback", actor, case["name"], "FallbackState", terminal,
            fallback.raw(terminal, "FallbackState"), terminal, terminal["stateVersion"],
            terminal["prefix"], terminal["world"], terminal["randomState"], terminal["cycle"],
            terminal["currentPosition"], arms_from(terminal, closeReason=case["reason"]), terminal))
    return result


def reaction_completion_sources():
    fixture = json.loads(reaction_complete.FIXTURE.read_text())
    result = []
    for case in fixture["cases"]:
        traced = reaction_complete.trace(case)
        verify_golden(reaction_complete, traced, case)
        terminal = traced[3][-1]
        result.append(source("combat-inherited-reaction-movement-completion-v1",
            "reaction-second-move-completion", case["actor"], case["name"], "LifecycleState",
            terminal, reaction_complete.irl.raw(terminal, "LifecycleState"), terminal,
            terminal["stateVersion"], terminal["prefix"], terminal["world"],
            terminal["randomState"], terminal["cycle"], terminal["currentPosition"],
            arms_from(terminal), terminal))
    return result


def reserve_cycle_sources():
    fixture = json.loads(reserve_cycle.FIXTURE.read_text())
    result = []
    for case in fixture["cases"]:
        traced = reserve_cycle.trace(case)
        verify_golden(reserve_cycle, traced, case)
        terminal = traced[2][-1]
        root = terminal["base"]
        result.append(source("combat-inherited-reserve-cycle-v1", "reserve-cycle-entry",
            case["actor"], case["actor"] + "-reserve-I-cycle-entry", "Control", terminal,
            reserve_cycle.raw(terminal, "Control"), root, terminal["stateVersion"], terminal["prefix"],
            root["world"], root["randomState"], root["cycle"], terminal["position"],
            arms_from(terminal, members=root["members"]), (root, terminal)))
    return result


def release_parts(actor):
    case = next(item for item in json.loads(reserve_release.FIXTURE.read_text())["cases"]
        if item["actor"] == actor)
    traced = reserve_release.trace(case)
    verify_golden(reserve_release, traced, case)
    base, parent, states, _, _ = traced
    terminal = states[-1]
    inherited_control = parent[2][-1]
    root = inherited_control["base"]
    world = reserve_release.release.project_world(root["world"], base["releaseBase"],
        terminal["release"])
    return traced, terminal, inherited_control, root, world


def reserve_release_sources():
    result = []
    for actor in ("axis", "commonwealth"):
        traced, terminal, inherited, root, world = release_parts(actor)
        state = terminal["release"]
        result.append(source("combat-inherited-reserve-release-v1", "reserve-release", actor,
            actor + "-reserve-I-release", "Control", terminal,
            reserve_release.raw(terminal, "Control"), root, state["stateVersion"], state["prefix"],
            world, state["randomState"], root["cycle"], inherited["position"],
            arms_from(terminal, movementEnd=inherited["movementEnd"], members=state["members"]), traced))
    return result


def armed_sources():
    fixture = json.loads(armed.FIXTURE.read_text())
    result = []
    for case in fixture["cases"]:
        actor = case["actor"]
        traced = armed.source_trace(actor)
        proof = armed.proof_from(actor, traced)
        require(proof == case["proof"] and armed.golden(proof) == case["golden"], 4)
        base, parent, states, _, _ = traced
        terminal = states[-1]
        inherited = parent[2][-1]
        root = inherited["base"]
        world = armed.irr.release.project_world(root["world"], base["releaseBase"], terminal["release"])
        state = terminal["release"]
        result.append(source("combat-inherited-armed-continuation-v1", "armed-continuation",
            actor, actor + "-armed-continuation", "Proof", proof, armed.raw(proof, "Proof"), root,
            state["stateVersion"], state["prefix"], world, state["randomState"], proof["currentCycle"],
            inherited["position"], arms_from(terminal, armedProof=proof,
                movementEnd=inherited["movementEnd"], members=state["members"]), traced))
    return result


def cycle_root(actor):
    bundle = cycle_control.derived(actor)
    armed_result = bundle[1]
    return bundle, armed_result[1][2][-1]["base"]


def cycle_sources():
    fixture = json.loads(cycle_control.FIXTURE.read_text())
    result = []
    for case in fixture["cases"]:
        actor, action = case["actor"], case["action"]
        traced = cycle_control.trace(actor, action)
        verify_golden(cycle_control, traced, case)
        base, states, _, _ = traced
        terminal = states[-1]
        bundle, root = cycle_root(actor)
        cycle_value = dict(sourceCycle=base["controlBase"]["releaseBase"]["cycle"],
            activeCycle=terminal["activeCycle"])
        result.append(source("combat-inherited-cycle-control-v1", "cycle-control", actor,
            actor + "-" + action, "CycleControlState", terminal,
            cycle_control.cyc.raw(terminal, "CycleControlState"), root, terminal["stateVersion"],
            terminal["prefix"], terminal["world"], terminal["randomState"], cycle_value,
            terminal["positionId"], arms_from(terminal), (bundle, traced)))
    return result


def reserve_movement_sources():
    fixture = json.loads(reserve_movement.FIXTURE.read_text())
    result = []
    for case in fixture["cases"]:
        actor = case["actor"]
        traced = reserve_movement.trace(actor)
        verify_golden(reserve_movement, traced, case)
        _, states, _, _ = traced
        terminal = states[-1]
        bundle, root = cycle_root(actor)
        result.append(source("combat-inherited-reserve-movement-completion-v1",
            "reserve-second-movement-completion", actor, actor + "-reserve-I-movement-completion",
            "InheritedReserveMovementCompletionState", terminal,
            reserve_movement.raw(terminal, "InheritedReserveMovementCompletionState"), root,
            terminal["stateVersion"], terminal["prefix"], terminal["world"], terminal["randomState"],
            terminal["cycle"], terminal["sequencePosition"], arms_from(terminal), (bundle, traced)))
    return result


def sources():
    for family in FAMILIES:
        verify_family_sources(json.loads((ROOT / f"fixtures/{family}.json").read_text()))
    result = []
    for factory in (noattack_sources, direct_sources, fallback_sources,
            reaction_completion_sources, reserve_cycle_sources, reserve_release_sources,
            armed_sources, cycle_sources, reserve_movement_sources):
        result.extend(factory())
    require(len(result) == INVENTORY["limits"]["traces"]
        and len({item["contract"] + ":" + item["caseId"] for item in result}) == len(result), 7)
    return result


def build_snapshot(item):
    root = item["root"]
    header = dict(initiativeHolder=root["initiativeHolder"], firstActingSide=root["firstActingSide"],
        operationStageOrders=root["operationStageOrders"],
        operationStageWeather=root["operationStageWeather"])
    snapshot = dict(contractVersion=1,
        traceId=item["contract"] + ":" + item["caseId"], actor=item["actor"],
        sourceContract=item["contract"], campaignId=root["campaignId"],
        rulesetHash=root["rulesetHash"], configurationHash=root["configurationHash"],
        creationBinding=root["creationBinding"], creationEventHash=root["creationEventHash"],
        stateVersion=item["stateVersion"], prefix=item["prefix"],
        header=value_fragment("combat-authority-composition-v1", "AuthorityHeader", header),
        world=copy.deepcopy(item["world"]), randomState=copy.deepcopy(item["randomState"]),
        cycle=value_fragment(item["contract"], "Cycle", item["cycle"]),
        position=value_fragment(item["contract"], "Position", item["position"]),
        authorityArms=value_fragment(item["contract"], "AuthorityArms", item["arms"]),
        commandReceipts=value_fragment("combat-authority-composition-v1", "CommandReceipts",
            item["receipts"]),
        obligations=value_fragment("combat-world-settlement-v1", "RetainedObligations",
            obligations(item["world"])),
        terminal=fragment(item["contract"], item["stateType"], item["terminalData"]))
    typed(snapshot, "RootSnapshot")
    return canonical(snapshot, "RootSnapshot")


def build_trace(item):
    snapshot = build_snapshot(item)
    terminal_value = json.loads(item["terminalData"])
    snapshot_value = json.loads(raw(snapshot, "RootSnapshot"))
    terminal_depth, terminal_array = measure(terminal_value)
    snapshot_depth, snapshot_array = measure(snapshot_value)
    capacity = dict(terminalBytes=len(item["terminalData"]),
        snapshotBytes=len(raw(snapshot, "RootSnapshot")),
        maxDepth=max(terminal_depth, snapshot_depth), maxArrayItems=max(terminal_array, snapshot_array),
        worldCauseCount=len(item["world"]["cohesionCauses"]), receiptCount=len(item["receipts"]),
        futureObligationCount=len(item["world"]["futureObligations"]), withinLimits=True)
    limits = INVENTORY["limits"]
    capacity["withinLimits"] = (capacity["terminalBytes"] <= limits["bytes"]
        and capacity["snapshotBytes"] <= limits["bytes"] and capacity["maxDepth"] <= limits["depth"]
        and capacity["maxArrayItems"] <= limits["arrayItems"]
        and capacity["worldCauseCount"] <= limits["worldCauses"]
        and capacity["receiptCount"] <= limits["arrayItems"])
    require(capacity["withinLimits"], 7)
    return canonical(dict(traceId=snapshot["traceId"], family=item["family"], actor=item["actor"],
        caseId=item["caseId"], sourceContract=item["contract"], sourceStateType=item["stateType"],
        snapshot=snapshot, capacity=capacity), "TraceWitness")


def source_pins():
    return [dict(path=path, sha256=sha((REPO / path).read_bytes())) for path in sorted(SOURCE_PATHS)]


def verify_family_sources(fixture):
    pins = fixture.get("sourcePins")
    hashes = fixture.get("sourceHashes")
    require((type(pins) is list) != (type(hashes) is dict), 4)
    entries = ((pin["path"], pin["sha256"]) for pin in pins) if type(pins) is list \
        else hashes.items()
    seen = set()
    for path, digest in entries:
        require(type(path) is str and path not in seen and type(digest) is str, 4)
        candidate = (REPO / path).resolve()
        require(REPO == candidate or REPO in candidate.parents, 4)
        require(candidate.is_file() and sha(candidate.read_bytes()) == digest, 4)
        seen.add(path)


def binding(owner, artifact_ids, schema_paths, pins):
    selected = [pin for pin in pins if pin["path"] in schema_paths]
    require(len(selected) == len(schema_paths), 4)
    return dict(owner=owner, artifactIds=artifact_ids, schemaPaths=schema_paths,
        digest=sha(encode(selected)))


def requirement_bindings():
    return [
        dict(requirementRange="CMB-ID-AC-001-012", contractOwners=["CMB-CON-003", "CMB-CON-005"],
            implementationTasks=["CMB-TASK-006", "CMB-TASK-009", "CMB-TASK-016", "CMB-TASK-018",
                "CMB-TASK-019", "CMB-TASK-020", "CMB-TASK-021", "CMB-TASK-022"],
            evidenceOwner="CMB-TASK-025"),
        dict(requirementRange="CMB-PRO-AC-001-012", contractOwners=["CMB-CON-003", "CMB-CON-005", "CMB-CON-006"],
            implementationTasks=["CMB-TASK-008", "CMB-TASK-010", "CMB-TASK-011", "CMB-TASK-012",
                "CMB-TASK-013", "CMB-TASK-020", "CMB-TASK-021", "CMB-TASK-022", "CMB-TASK-023", "CMB-TASK-024"],
            evidenceOwner="CMB-TASK-025"),
        dict(requirementRange="CMB-STEP-AC-001-012", contractOwners=["CMB-CON-003", "CMB-CON-005", "CMB-CON-006"],
            implementationTasks=["CMB-TASK-009", "CMB-TASK-010", "CMB-TASK-011", "CMB-TASK-016",
                "CMB-TASK-020", "CMB-TASK-021", "CMB-TASK-022", "CMB-TASK-023", "CMB-TASK-024"],
            evidenceOwner="CMB-TASK-025"),
        dict(requirementRange="CMB-RES-AC-001-012", contractOwners=["CMB-CON-002", "CMB-CON-003", "CMB-CON-005", "CMB-CON-006"],
            implementationTasks=["CMB-TASK-005", "CMB-TASK-006", "CMB-TASK-007", "CMB-TASK-008",
                "CMB-TASK-012", "CMB-TASK-013", "CMB-TASK-014", "CMB-TASK-015", "CMB-TASK-016",
                "CMB-TASK-020", "CMB-TASK-022", "CMB-TASK-023", "CMB-TASK-024"],
            evidenceOwner="CMB-TASK-025"),
        dict(requirementRange="CMB-SET-AC-001-012", contractOwners=["CMB-CON-002", "CMB-CON-003", "CMB-CON-005", "CMB-CON-006"],
            implementationTasks=["CMB-TASK-007", "CMB-TASK-008", "CMB-TASK-009", "CMB-TASK-014",
                "CMB-TASK-015", "CMB-TASK-016", "CMB-TASK-020", "CMB-TASK-021", "CMB-TASK-022",
                "CMB-TASK-023", "CMB-TASK-024"], evidenceOwner="CMB-TASK-025"),
        dict(requirementRange="CYCLE-COMP-AC-001-012", contractOwners=["CMB-CON-004", "CMB-CON-005", "CMB-CON-006"],
            implementationTasks=["CMB-TASK-008", "CMB-TASK-017", "CMB-TASK-018", "CMB-TASK-019",
                "CMB-TASK-020", "CMB-TASK-021", "CMB-TASK-022", "CMB-TASK-023", "CMB-TASK-024"],
            evidenceOwner="CMB-TASK-025"),
    ]


def handoff(traces, pins):
    bindings = [
        binding("CMB-CON-002", ["setup7", "world7", "settlement-receipts-v1"],
            ["docs/specs/combat-world-settlement-v1.schema.json"], pins),
        binding("CMB-CON-003", ["created11", "snapshot12", "combat-command-event-v1"], [
            "docs/specs/combat-authority-envelope-v1.schema.json",
            "docs/specs/combat-snapshot-composition-v1.schema.json",
            "docs/specs/combat-inherited-no-attack-v1.schema.json"], pins),
        binding("CMB-CON-004", ["sequence5", "cycle-codec1", "inherited-cycle-v1"], [
            "docs/specs/combat-cycle-sequence-v1.schema.json",
            "docs/specs/combat-inherited-reaction-closure-v1.schema.json",
            "docs/specs/combat-inherited-reaction-active-fallback-v1.schema.json",
            "docs/specs/combat-inherited-reaction-movement-completion-v1.schema.json",
            "docs/specs/combat-inherited-reserve-cycle-v1.schema.json",
            "docs/specs/combat-inherited-reserve-release-v1.schema.json",
            "docs/specs/combat-inherited-armed-continuation-v1.schema.json",
            "docs/specs/combat-inherited-cycle-control-v1.schema.json",
            "docs/specs/combat-inherited-reserve-movement-completion-v1.schema.json"], pins),
    ]
    exclusions = [
        dict(capability="multiple-opportunity-reaction", disposition="post-B-extension",
            reason="selected-profile-has-exactly-one-opportunity"),
        dict(capability="positive-vehicle-breakdown", disposition="post-B-extension",
            reason="selected-profile-breakdown-cohorts-are-empty"),
        dict(capability="two-runtime-assaults", disposition="deferred",
            reason="selected-profile-freezes-one-bounded-assault"),
        dict(capability="general-resupply", disposition="deferred", reason="outside-combat-package"),
        dict(capability="real-rba-movement", disposition="deferred", reason="decline-only-boundary"),
        dict(capability="released-reserve-offensive-combat", disposition="deferred",
            reason="candidate-proof-is-not-runtime-execution"),
        dict(capability="motorized-retreat", disposition="deferred", reason="infantry-profile-only"),
        dict(capability="gun-armor-combat", disposition="deferred", reason="infantry-profile-only"),
        dict(capability="mandatory-attacks", disposition="deferred", reason="voluntary-profile-only"),
        dict(capability="new-phase-slot-execution", disposition="deferred", reason="same-slot-boundary-only"),
        dict(capability="hosted-intelligence", disposition="deferred", reason="in-process-contract-only"),
    ]
    runtime = [
        dict(lane="codec-and-restore", taskIds=["CMB-TASK-008"],
            requiredEvidence="historical-reader-isolation-and-every-cut-restore"),
        dict(lane="combat-authority", taskIds=["CMB-TASK-009", "CMB-TASK-010", "CMB-TASK-011",
            "CMB-TASK-012", "CMB-TASK-013", "CMB-TASK-014", "CMB-TASK-015", "CMB-TASK-016"],
            requiredEvidence="selection-through-atomic-settlement-replay"),
        dict(lane="cycle-authority", taskIds=["CMB-TASK-017", "CMB-TASK-018", "CMB-TASK-019"],
            requiredEvidence="release-repeat-finish-and-inherited-path-replay"),
        dict(lane="public-core", taskIds=["CMB-TASK-020", "CMB-TASK-021"],
            requiredEvidence="side-safe-projection-and-authenticated-submission"),
        dict(lane="exercise-runner", taskIds=["CMB-TASK-022", "CMB-TASK-023", "CMB-TASK-024"],
            requiredEvidence="strict-reconstruction-readjudication-and-repeatability"),
        dict(lane="closeout", taskIds=["CMB-TASK-025"],
            requiredEvidence="all-72-AC-readback-and-authentic-loop"),
    ]
    trace_ids = [trace["traceId"] for trace in traces]
    return canonical(dict(owner="CMB-TASK-003", profile="combat-infantry-reserve-I-v1",
        versions=dict(content=7, setup=7, world=7, rules=10, createdEvent=11, snapshot=12,
            sequence=5, cycleCodec=1), contractBindings=bindings,
        requirementBindings=requirement_bindings(), exclusions=exclusions, runtimeOwners=runtime,
        selectedTraceIds=trace_ids, compositionDigest=sha(raw(traces, "TraceWitness[]"))),
        "Task004Handoff")


def build_composition():
    traces = [build_trace(item) for item in sources()]
    pins = source_pins()
    capacities = [trace["capacity"] for trace in traces]
    summary = dict(traceCount=len(traces),
        totalSnapshotBytes=sum(item["snapshotBytes"] for item in capacities),
        maxSnapshotBytes=max(item["snapshotBytes"] for item in capacities),
        maxTerminalBytes=max(item["terminalBytes"] for item in capacities),
        maxDepth=max(item["maxDepth"] for item in capacities),
        maxArrayItems=max(item["maxArrayItems"] for item in capacities),
        maxWorldCauses=max(item["worldCauseCount"] for item in capacities),
        maxReceipts=max(item["receiptCount"] for item in capacities),
        withinLimits=all(item["withinLimits"] for item in capacities))
    value = dict(contractVersion=1, profile="combat-infantry-reserve-I-v1",
        handoff=handoff(traces, pins), traces=traces, capacity=summary)
    typed(value, "AuthorityComposition")
    require(summary["traceCount"] == INVENTORY["limits"]["traces"]
        and summary["withinLimits"], 7)
    return canonical(value, "AuthorityComposition")


def trace_golden(trace):
    snapshot_data = raw(trace["snapshot"], "RootSnapshot")
    return dict(traceId=trace["traceId"], family=trace["family"], actor=trace["actor"],
        caseId=trace["caseId"], sourceContract=trace["sourceContract"],
        stateVersion=trace["snapshot"]["stateVersion"], prefix=trace["snapshot"]["prefix"],
        worldHash=sha(CORE.raw(trace["snapshot"]["world"], "World")),
        terminalHash=trace["snapshot"]["terminal"]["sha256"],
        snapshotBytes=len(snapshot_data), snapshotHash=sha(snapshot_data),
        receiptCount=trace["capacity"]["receiptCount"], capacity=trace["capacity"])


def golden(value):
    data = raw(value, "AuthorityComposition")
    return dict(bytes=len(data), sha256=sha(data),
        compositionDigest=value["handoff"]["compositionDigest"],
        familyCounts=dict(sorted(Counter(trace["family"] for trace in value["traces"]).items())),
        capacity=value["capacity"])


def generated_fixture():
    composition = build_composition()
    return dict(contractVersion=1, contract="combat-authority-composition-v1",
        sourcePins=source_pins(), handoff=composition["handoff"],
        traces=[trace_golden(trace) for trace in composition["traces"]],
        composition=golden(composition),
        goldenMethod="canonical ASCII JSON; SHA-256 with sha256: prefix; actual predecessor replay")


def rejected(call):
    try:
        call()
    except (Invalid, ValueError, KeyError, TypeError):
        return
    raise AssertionError("invalid authority composition admitted")


def changed(value, path, replacement):
    result = copy.deepcopy(value)
    target = result
    for step in path[:-1]:
        target = target[step]
    target[path[-1]] = replacement
    return result


def read_composition(data, expected):
    value = parse(data, "AuthorityComposition")
    require(data == raw(expected, "AuthorityComposition"), 6)
    require(value["handoff"]["compositionDigest"] == sha(raw(value["traces"], "TraceWitness[]")), 6)
    require(value["handoff"]["selectedTraceIds"] == [trace["traceId"] for trace in value["traces"]], 6)
    require(value["capacity"]["traceCount"] == len(value["traces"]), 7)
    return value


def verify(value):
    data = raw(value, "AuthorityComposition")
    require(read_composition(data, value) == value, 6)
    counts = dict(readbacks=1, mutations=0, raw=0, boundaries=0)
    for index, trace in enumerate(value["traces"]):
        snapshot = trace["snapshot"]
        snapshot_data = raw(snapshot, "RootSnapshot")
        require(parse(snapshot_data, "RootSnapshot") == snapshot, 6)
        counts["readbacks"] += 1
        mutations = [
            (("stateVersion",), snapshot["stateVersion"] + 1),
            (("prefix",), "sha256:" + "f" * 64),
            (("world", "elements", 0, "currentLocationId"), "foreign.location"),
            (("randomState", "nextByteCursor"), snapshot["randomState"]["nextByteCursor"] + 1),
            (("terminal", "sha256"), "sha256:" + "e" * 64),
            (("commandReceipts", "bytes"), snapshot["commandReceipts"]["bytes"] + 1),
            (("obligations", "sha256"), "sha256:" + "d" * 64),
        ]
        for path, replacement in mutations:
            altered_snapshot = changed(snapshot, path, replacement)
            altered = changed(value, ("traces", index, "snapshot"), altered_snapshot)
            rejected(lambda altered=altered: read_composition(raw(altered,
                "AuthorityComposition"), value))
            counts["mutations"] += 1
        for fragment_value in (snapshot["header"], snapshot["cycle"], snapshot["position"],
                snapshot["authorityArms"], snapshot["commandReceipts"], snapshot["obligations"],
                snapshot["terminal"]):
            payload = fragment_value["canonicalJson"].encode("ascii")
            require(fragment(fragment_value["contract"], fragment_value["type"], payload)
                == fragment_value, 6)
            counts["readbacks"] += 1
    handoff_paths = [
        (("versions", "snapshot"), 13),
        (("contractBindings", 0, "digest"), "sha256:" + "c" * 64),
        (("requirementBindings", 0, "implementationTasks"), ["CMB-TASK-025"]),
        (("exclusions", 0, "disposition"), "admitted"),
        (("runtimeOwners", 0, "taskIds"), ["CMB-TASK-025"]),
        (("selectedTraceIds",), value["handoff"]["selectedTraceIds"][:-1]),
        (("compositionDigest",), "sha256:" + "b" * 64),
    ]
    for path, replacement in handoff_paths:
        altered_handoff = changed(value["handoff"], path, replacement)
        altered = changed(value, ("handoff",), altered_handoff)
        rejected(lambda altered=altered: read_composition(raw(altered, "AuthorityComposition"), value))
        counts["mutations"] += 1
    for altered_traces in (value["traces"][:-1], value["traces"][1:],
            list(reversed(value["traces"])), value["traces"] + [value["traces"][-1]]):
        altered = copy.deepcopy(value)
        altered["traces"] = altered_traces
        rejected(lambda altered=altered: read_composition(raw(altered, "AuthorityComposition"), value))
        counts["boundaries"] += 1
    overflow = copy.deepcopy(value)
    overflow["traces"][0]["snapshot"]["commandReceipts"]["canonicalJson"] = "[" + "0," * 524288 + "0]"
    rejected(lambda: raw(overflow, "AuthorityComposition"))
    counts["boundaries"] += 1
    rejected(lambda: fragment("combat-authority-composition-v1", "OversizeArray",
        encode([0] * (INVENTORY["limits"]["arrayItems"] + 1))))
    counts["boundaries"] += 1
    family_fixture = json.loads(noattack.FIXTURE.read_text())
    family_fixture["sourcePins"][0]["sha256"] = "sha256:" + "0" * 64
    rejected(lambda: verify_family_sources(family_fixture))
    counts["boundaries"] += 1
    family_fixture = json.loads(noattack.FIXTURE.read_text())
    family_fixture["sourcePins"][0]["path"] = "../outside-repository"
    rejected(lambda: verify_family_sources(family_fixture))
    counts["boundaries"] += 1
    for bad in (data + b"\n", b" " + data, b"\xef\xbb\xbf" + data, data[:-1],
            b'{"unknown":0,' + data[1:],
            data.replace(b'"contractVersion":1', b'"contractVersion":1e0', 1)):
        rejected(lambda bad=bad: read_composition(bad, value))
        counts["raw"] += 1
    return counts


def check_fixture(fixture, value):
    require(set(fixture) == {"contractVersion", "contract", "sourcePins", "handoff", "traces",
        "composition", "goldenMethod"} and fixture["contractVersion"] == 1
        and fixture["contract"] == "combat-authority-composition-v1", 4)
    require(fixture["sourcePins"] == source_pins()
        and len(fixture["sourcePins"]) == INVENTORY["limits"]["sourcePins"], 4)
    require(fixture["handoff"] == value["handoff"], 4)
    require(fixture["traces"] == [trace_golden(trace) for trace in value["traces"]], 4)
    require(fixture["composition"] == golden(value), 4)


def main():
    if sys.argv[1:] == ["--generate-fixture"]:
        print(json.dumps(generated_fixture(), indent=2))
        return
    fixture = json.loads(FIXTURE.read_text())
    composition = build_composition()
    check_fixture(fixture, composition)
    counts = verify(composition)
    print("CMB-ACM PASS", json.dumps(dict(traces=len(composition["traces"]),
        families=len(Counter(trace["family"] for trace in composition["traces"])),
        sourcePins=len(source_pins()), **counts), sort_keys=True))


if __name__ == "__main__":
    main()
