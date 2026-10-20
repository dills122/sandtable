#!/usr/bin/env python3
"""Two authentic P01 Force Assignment one-seal cold readbacks, no admission."""
import base64
import argparse
import copy
import hashlib
import importlib.util
import json
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent
CHILD = ROOT / "combat-p01-force-seal-fresh-child-v1.py"
FIXTURE = ROOT / "fixtures/combat-p01-force-seal-fresh-v1.json"


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


ref = load("force_seal_fresh_reference", "verify-combat-p01-force-assignment-v1.py")
r03 = ref.selected.p01.r03


def require(condition, label):
    if not condition:
        raise ValueError(label)


def b64(value):
    return base64.b64encode(value).decode("ascii")


def digest(value):
    return "sha256:" + hashlib.sha256(value).hexdigest()


def ledger_hash(ledger):
    return digest(ref.encode([b64(item) for item in ledger]))


def root_for(index):
    row = r03.r02.semantic_rows()[index]
    parent_source, parents = r03.parent_terminal(row)
    source = r03.source_for(parent_source)
    _, context, prior = r03.replay(source, parents, [])
    command = r03.raw(r03.command(context, prior), "Input")
    source, own, _, _ = r03.apply(source, parents, [], command)
    return row, source, parents, own


def selected_path(source, parents, r03_ledger):
    selected_source = ref.selected.source_for(source)
    ledger = []
    state = ref.selected.initial(source, parents, r03_ledger)
    attacker = state["candidate"]["attacker"]["originalSide"]
    defender = state["candidate"]["defender"]["originalSide"]
    for kind, actor, now in (
        ("open-selection", "system", 1000),
        ("select-assault", attacker, 1001),
        ("complete-step", "system", None),
        ("complete-step", "system", None),
        ("open-rba", "system", 1002),
        ("decline-rba", defender, 1003),
        ("complete-step", "system", None),
    ):
        value = ref.selected.expected_input(state, kind, actor=actor, now=now)
        raw_input = ref.selected.input_bytes(value)
        state, event, _ = ref.selected.transition(state, value)
        packet = ref.selected.parse_source(selected_source)
        packet["eventCanonicalUtf8"].append(event.decode("ascii"))
        selected_source = ref.encode(packet)
        ledger.append(raw_input)
    require(state["closed"] and state["rbaOutcome"] == "declined" and
            state["stepIndex"] == 3, "selected RBA predecessor")
    return selected_source, ledger


def append(source, ledger, state, value):
    raw_input = ref.input_bytes(value)
    after, event, receipt = ref.transition(state, value)
    packet = ref.parse_source(source)
    packet["eventCanonicalUtf8"].append(event.decode("ascii"))
    return ref.encode(packet), [*ledger, raw_input], after, event, receipt


def build(index, first_role):
    row, r03_source, parents, r03_ledger = root_for(index)
    selected_source, selected_ledger = selected_path(
        r03_source, parents, r03_ledger)
    source = ref.source_for(selected_source)
    opened = ref.initial(selected_source, parents, r03_ledger, selected_ledger)
    world = ref.encode(opened["parent"]["parent"]["world"])
    random = ref.encode(opened["parent"]["parent"]["randomState"])
    source, own, opened, _, opening_receipt = append(
        source, [], opened, ref.expected_input(
            opened, "open-assignment", now=2000))
    first = next(item for item in opened["slots"] if item["role"] == first_role)
    first_input = ref.expected_input(
        opened, "seal-assignment", actor=first["owner"],
        role=first_role, now=2001)
    source, own, collecting, first_event, first_receipt = append(
        source, own, opened, first_input)
    require(len(own) == 2 and collecting["status"] == "collecting" and
            collecting["lastReceiptId"] == first_receipt and
            collecting["parent"]["parent"]["world"] ==
            opened["parent"]["parent"]["world"] and
            collecting["parent"]["parent"]["randomState"] ==
            opened["parent"]["parent"]["randomState"],
            "first seal changed resources")
    second_role = "defender" if first_role == "attacker" else "attacker"
    second = next(item for item in collecting["slots"]
                  if item["role"] == second_role)
    second_input = ref.expected_input(
        collecting, "seal-assignment", actor=second["owner"],
        role=second_role, now=2002)
    next_source, next_ledger, prepared, second_event, second_receipt = append(
        source, own, collecting, second_input)
    require(prepared["status"] == "prepared" and
            prepared["admitted"] is False and prepared["publicActions"] == [] and
            ref.encode(prepared["parent"]["parent"]["world"]) == world and
            ref.encode(prepared["parent"]["parent"]["randomState"]) == random,
            "second seal changed resources")
    claim = dict(
        source=b64(source), creationRequest=b64(ref.encode(parents[0])),
        parentInputLedgers=[[b64(item) for item in ledger] for ledger in parents[1:]],
        r03Ledger=[b64(item) for item in r03_ledger],
        selectedLedger=[b64(item) for item in selected_ledger],
        ownLedger=[b64(item) for item in own],
        expectedControl=b64(ref.control_bytes(collecting)),
        expectedWorld=b64(world), expectedRandom=b64(random),
        expectedReceiptId=first_receipt,
        retryInput=b64(own[1]), secondInput=b64(ref.input_bytes(second_input)))
    expected = dict(nextSource=b64(next_source),
                    nextLedger=[b64(item) for item in next_ledger],
                    nextEvent=b64(second_event), nextReceiptId=second_receipt,
                    nextControl=b64(ref.control_bytes(prepared)),
                    nextPrefix=prepared["prefix"],
                    nextVersion=prepared["stateVersion"])
    return dict(row=row, firstRole=first_role, claim=claim,
                expected=expected, openingReceiptId=opening_receipt,
                firstEvent=first_event, firstReceiptId=first_receipt,
                source=source, own=own, selectedSource=selected_source,
                selectedLedger=selected_ledger, r03Source=r03_source,
                r03Ledger=r03_ledger, parents=parents,
                collecting=collecting, prepared=prepared)


def child(claim):
    incoming = json.dumps(claim, ensure_ascii=True,
                          separators=(",", ":")).encode("ascii")
    require(len(incoming) <= 16 * 1024 * 1024, "claim byte cap")
    with tempfile.TemporaryDirectory(prefix="p01-force-seal-") as directory:
        path = Path(directory) / "claim.json"
        path.write_bytes(incoming)
        result = subprocess.run([sys.executable, "-B", str(CHILD), str(path)],
                                capture_output=True, text=True, check=True,
                                timeout=120)
    require(not result.stderr, "child stderr: " + result.stderr)
    return json.loads(result.stdout)


def verify_row(index, first_role):
    case = build(index, first_role)
    observed = child(case["claim"])
    require("rejected" not in observed, "cold readback: " + str(observed))
    require(all(observed[key] == value for key, value in case["expected"].items()),
            "second seal differs across interpreters")
    state = case["collecting"]
    require(observed["control"] == case["claim"]["expectedControl"] and
            observed["worldHash"] == digest(ref.encode(
                state["parent"]["parent"]["world"])) and
            observed["randomHash"] == digest(ref.encode(
                state["parent"]["parent"]["randomState"])) and
            observed["sourceHash"] == digest(case["source"]) and
            observed["receiptId"] == case["firstReceiptId"] and
            observed["stateVersion"] == state["stateVersion"] and
            observed["prefix"] == state["prefix"] and
            observed["retryReceiptId"] == case["firstReceiptId"] and
            observed["retryEvent"] is None and
            observed["admitted"] is False and observed["publicActions"] == [],
            "cold Collecting readback mismatch")
    row = case["row"]
    # The physical catalogue row is independently indexed by the original
    # CreationRequest; no modified Content or selected candidate is supplied.
    result = dict(rowKey=row["rowKey"], firstRole=first_role,
                  creationHash=digest(ref.encode(case["parents"][0])),
                  r03SourceHash=digest(case["r03Source"]),
                  r03LedgerHash=ledger_hash(case["r03Ledger"]),
                  selectedSourceHash=digest(case["selectedSource"]),
                  selectedLedgerHash=ledger_hash(case["selectedLedger"]),
                  firstSourceHash=digest(case["source"]),
                  firstLedgerHash=ledger_hash(case["own"]),
                  openingReceiptId=case["openingReceiptId"],
                  firstEventHash=digest(case["firstEvent"]),
                  firstReceiptId=case["firstReceiptId"],
                  firstControlHash=digest(ref.control_bytes(state)),
                  secondSourceHash=digest(base64.b64decode(observed["nextSource"])),
                  secondLedgerHash=ledger_hash([base64.b64decode(item)
                                                for item in observed["nextLedger"]]),
                  secondEventHash=digest(base64.b64decode(observed["nextEvent"])),
                  secondReceiptId=observed["nextReceiptId"],
                  secondControlHash=digest(base64.b64decode(observed["nextControl"])),
                  finalPrefix=observed["nextPrefix"],
                  finalVersion=observed["nextVersion"],
                  admitted=False, publicActions=[])
    return result


def verify(evidence_path=None):
    semantic_rows = r03.r02.semantic_rows()
    require(len(semantic_rows) == 300 and
            semantic_rows[36]["rowKey"] == "r012-c01" and
            semantic_rows[168]["rowKey"] == "r034-c01",
            "original 300-row catalogue")
    results = [verify_row(36, "attacker"), verify_row(168, "defender")]
    summary = dict(contractVersion=1, authenticRows=2,
                   independentInterpreterCuts=2, acceptedSameSealRetries=2,
                   secondOwnerSeals=2, admittedRows=0, publicActionCount=0,
                   rows=results)
    require(summary == json.loads(FIXTURE.read_text()), "frozen two-row evidence")
    audit = None
    if evidence_path is not None:
        audit = ref.audit(evidence_path)
        require(audit == dict(authenticatedRows=300, selectedCandidates=44,
                              candidateAbsent=256, admittedRows=0,
                              publicActionCount=0), "pinned 300-row audit")
    return dict(summary=summary, audit=audit,
                orderedHash=digest(ref.encode(summary)))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("evidence_root", nargs="?", type=Path)
    parser.add_argument("--ci", action="store_true",
                        help="verify retained roots without a local private evidence directory")
    args = parser.parse_args()
    if args.ci == (args.evidence_root is not None):
        parser.error("choose exactly one of EVIDENCE_ROOT or --ci")
    print(json.dumps(verify(args.evidence_root), ensure_ascii=True,
                     separators=(",", ":")))
