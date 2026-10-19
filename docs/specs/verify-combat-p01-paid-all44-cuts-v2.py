#!/usr/bin/env python3
"""Bounded, in-process cut readbacks for all 44 authentic private paid roots."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
FIXTURE = json.loads((ROOT / "fixtures/combat-p01-paid-all44-cuts-v2.json").read_text())


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


paid = load("all44_paid_test", "test-combat-p01-paid-commit-v1.py")
ref = paid.ref
RETAINED_FILES = dict(manifestSha256="manifest.json",
                      boundedProfileAuditSha256="bounded-profile-audit.json")


def digest(raw):
    return "sha256:" + hashlib.sha256(raw).hexdigest()


def require(condition, label):
    if not condition:
        raise AssertionError(label)


def check_retained_pins(evidence=None):
    expected = {field: ref.result.PINS[name]
                for field, name in RETAINED_FILES.items()}
    require(FIXTURE["retainedPins"] == expected, "retained fixture pins")
    if evidence is not None:
        evidence = Path(evidence)
        for field, name in RETAINED_FILES.items():
            require(digest((evidence / name).read_bytes()) ==
                    "sha256:" + expected[field], "retained file pin: " + name)
    return expected


def resource_by_id(world):
    return {element["elementId"]: element for element in world["elements"]}


def assert_conservation(before, after, event):
    prior = resource_by_id(before["world"])
    current = resource_by_id(after["world"])
    costs = json.loads(event)["effect"]["costs"]
    require(set(prior) == set(current), "element membership")
    require([cost["role"] for cost in costs] == ["attacker", "defender"], "cost roles")
    paid_ids = set()
    for cost in costs:
        element_id = cost["unit"]["elementId"]
        require(element_id in prior and element_id not in paid_ids, "paid element identity")
        paid_ids.add(element_id)
        old, new = prior[element_id], current[element_id]
        old_cp = old["operationalState"]["capabilityPointsExpended"]
        new_cp = new["operationalState"]["capabilityPointsExpended"]
        require(cost["beforeCp"] == old_cp and cost["afterCp"] == new_cp,
                "event CP conservation")
        require(new_cp["denominator"] == old_cp["denominator"] == 1 and
                new_cp["numerator"] - old_cp["numerator"] == cost["cpCost"] ==
                (5 if cost["role"] == "attacker" else 3), "CP debit")
        old_ammo, new_ammo = old["ammunition"]["points"], new["ammunition"]["points"]
        require((old_ammo, new_ammo) == (10, 0) and
                (cost["beforeAmmo"], cost["ammoCost"], cost["afterAmmo"]) ==
                (old_ammo, old_ammo - new_ammo, new_ammo), "ammunition debit")
        require(old["components"] == new["components"] and
                old["currentLocationId"] == new["currentLocationId"],
                "TOE and location conservation")
        preserved = {key for key in old if key not in ("ammunition", "operationalState")}
        require(all(old[key] == new[key] for key in preserved), "element conservation")
        require({key: value for key, value in old["operationalState"].items()
                 if key != "capabilityPointsExpended"} ==
                {key: value for key, value in new["operationalState"].items()
                 if key != "capabilityPointsExpended"}, "operational conservation")
    require(all(prior[key] == current[key] for key in set(prior) - paid_ids),
            "nonparticipant conservation")
    require({key: value for key, value in before["world"].items()
             if key != "elements"} ==
            {key: value for key, value in after["world"].items()
             if key != "elements"}, "World conservation")
    require(after["randomState"] == before["randomState"] and
            after["position"] == before["position"] and
            after["parent"] == before["parent"], "Control conservation")
    require(before["attackHistory"] == before["targetUses"] == [] and
            len(after["attackHistory"]) == len(after["targetUses"]) == 1,
            "one history and target use")
    history, target = after["attackHistory"][0], after["targetUses"][0]
    require(history["commitmentId"] == target["commitmentId"] ==
            after["commitmentId"] and history["segmentId"] == target["segmentId"] and
            history["targetLocationId"] == target["targetLocationId"],
            "history target-use binding")
    require(before["admitted"] is after["admitted"] is False and
            before["publicActions"] == after["publicActions"] == [],
            "private unsupported cuts")


def verify_row(row, semantic_rows):
    index, side = row["index"], row["actingSide"]
    require(semantic_rows[index]["rowKey"] == row["rowKey"], "row key")
    root = paid.fixture.fixture.terminal(index)
    require(digest(root[0]) == row["r03SourceHash"], "authentic R03 source hash")
    require(len(root[1]) == 11 and len(root[2]) == 1 and
            type(root[1][0]) is dict and
            all(type(ledger) is list and all(type(item) is bytes for item in ledger)
                for ledger in [*root[1][1:], root[2]]),
            "CreationRequest and eleven parent ledgers")
    selected_source, selected_ledger = paid.fixture.selected_path(root)
    assignment_source, assignment_ledger = paid.prepared(
        root, selected_source, selected_ledger, ("attacker", "defender"))
    source = ref.source_for(assignment_source)
    parents, r03 = root[1], root[2]
    before = ref.replay(source, parents, r03, selected_ledger,
                        assignment_ledger, [])
    require(before["parent"]["parent"]["parent"]["activeCycle"]["actingSide"] == side,
            "acting side")
    prepared_control = ref.control_bytes(before)
    prepared_world = ref.encode(before["world"])
    prepared_receipt = before["lastReceiptId"]
    prepared_readback = ref.replay(source, parents, r03, selected_ledger,
                                  assignment_ledger, [])
    require((ref.control_bytes(prepared_readback),
             ref.encode(prepared_readback["world"]),
             prepared_readback["lastReceiptId"]) ==
            (prepared_control, prepared_world, prepared_receipt),
            "prepared canonical cut readback")
    command = ref.input_bytes(ref.expected_input(before))
    intended, event, receipt = ref.transition(before, ref.parse_input(command))
    packet = ref.parse_source(source)
    packet["eventCanonicalUtf8"].append(event.decode("ascii"))
    committed_source, own = ref.encode(packet), [command]
    after = ref.replay(committed_source, parents, r03,
                       selected_ledger, assignment_ledger, own)
    committed_control = ref.control_bytes(intended)
    committed_world = ref.encode(intended["world"])
    require((ref.control_bytes(after), ref.encode(after["world"]),
             after["lastReceiptId"]) ==
            (committed_control, committed_world, receipt),
            "committed canonical cut readback")
    require(after["status"] == "committed" and
            after["lastReceiptId"] == receipt and
            after["stateVersion"] == before["stateVersion"] + 1 and
            ref.parse_source(committed_source)["eventCanonicalUtf8"] ==
            [event.decode("ascii")], "committed canonical cut readback")
    assert_conservation(before, after, event)
    retry_source, retry_own, retry_event, retry_receipt = ref.apply(
        committed_source, parents, r03, selected_ledger,
        assignment_ledger, own, command)
    require((retry_source, retry_own, retry_event, retry_receipt) ==
            (committed_source, own, None, receipt), "accepted retry is idempotent")
    sizes = dict(sourceBytes=len(committed_source), inputBytes=len(command),
                 eventBytes=len(event), controlBytes=len(committed_control),
                 worldBytes=max(len(prepared_world), len(committed_world)))
    limits = ref.SCHEMA["limits"]
    for field, limit in (("sourceBytes", "sourceBytes"), ("inputBytes", "inputBytes"),
                         ("eventBytes", "eventBytes"), ("controlBytes", "controlBytes"),
                         ("worldBytes", "projectedWorldBytes")):
        require(sizes[field] <= limits[limit], field + " cap")
    return dict(index=index, rowKey=row["rowKey"], actingSide=side,
                preparedControlHash=digest(prepared_control),
                preparedWorldHash=digest(prepared_world),
                preparedReceiptId=prepared_receipt,
                committedControlHash=digest(committed_control),
                committedWorldHash=digest(committed_world),
                committedReceiptId=receipt, eventHash=digest(event), sizes=sizes)


def verify():
    check_retained_pins()
    rows = FIXTURE["roots"]
    require(len(rows) == 44 and len({x["index"] for x in rows}) == 44 and
            [x["index"] for x in rows] == list(range(36, 300, 6)) and
            [x["actingSide"] for x in rows] == ["axis"] * 22 +
            ["commonwealth"] * 22, "ordered 44-root partition")
    semantic_rows = paid.fixture.fixture.r03.r02.semantic_rows()
    require(len(semantic_rows) == 300 and FIXTURE["candidateAbsent"] == 256 and
            FIXTURE["admitted"] is False and FIXTURE["publicActions"] == [],
            "300-root private partition")
    outputs = [verify_row(row, semantic_rows) for row in rows]
    ordered = ref.encode(outputs)
    output_hash = digest(ordered)
    maxima = {field: max(row["sizes"][field] for row in outputs)
              for field in outputs[0]["sizes"]}
    return dict(catalogueRows=300, preparedCuts=44, committedCuts=44,
                candidateAbsent=256, admittedRows=0, publicActions=0,
                orientations={"axis": 22, "commonwealth": 22},
                orderedOutputHash=output_hash, orderedOutputBytes=len(ordered),
                maxima=maxima, rows=outputs)


def audit_retained(evidence):
    check_retained_pins(evidence)
    summary = ref.audit(Path(evidence))
    require(summary["authenticatedRows"] == 300 and
            summary["privatePaidCommits"] == 44 and
            summary["candidateAbsent"] == 256 and
            summary["orientations"] == {"axis": 22, "commonwealth": 22} and
            summary["admittedRows"] == summary["publicActions"] == 0,
            "retained physical partition")
    return summary


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("evidence", nargs="?", type=Path)
    args = parser.parse_args()
    output = verify() if args.evidence is None else audit_retained(args.evidence)
    print(json.dumps(output, sort_keys=True, separators=(",", ":")))
