#!/usr/bin/env python3
"""Private all-44 selected-assault RBA-open cold readback and typed test decline."""
import argparse
import base64
from concurrent.futures import ThreadPoolExecutor, as_completed
import hashlib
import importlib.util
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent
CHILD = ROOT / "combat-p01-all44-rba-decline-fresh-child-v1.py"
FIXTURE = ROOT / "fixtures/combat-p01-all44-rba-decline-fresh-v1.json"


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


cuts = load("rba_all44_paid_cuts", "verify-combat-p01-paid-all44-cuts-v2.py")
selected = load("rba_all44_selected", "verify-combat-p01-selected-assault-v1.py")


def require(ok, label):
    if not ok:
        raise AssertionError(label)


def b64(raw):
    return base64.b64encode(raw).decode("ascii")


def digest(raw):
    return "sha256:" + hashlib.sha256(raw).hexdigest()


def ledger_hash(ledger):
    return digest(selected.encode([b64(x) for x in ledger]))


def pinned_rows():
    cuts.check_retained_pins()
    prior = cuts.FIXTURE["roots"]
    require(len(prior) == 44 and [x["index"] for x in prior] ==
            list(range(36, 300, 6)) and
            [x["actingSide"] for x in prior] ==
            ["axis"] * 22 + ["commonwealth"] * 22 and
            cuts.FIXTURE["candidateAbsent"] == 256 and
            cuts.FIXTURE["admitted"] is False and
            cuts.FIXTURE["publicActions"] == [], "reviewed 300-row partition")
    return prior


def build(row):
    root = cuts.paid.fixture.fixture.terminal(row["index"])
    r03_source, parents, r03 = root
    require(digest(r03_source) == row["r03SourceHash"] and
            len(parents) == 11 and len(r03) == 1,
            "pinned original CreationRequest and R03 source")
    source = selected.source_for(r03_source)
    ledger = []
    state = selected.initial(r03_source, parents, r03)
    attacker = state["candidate"]["attacker"]["originalSide"]
    defender = state["candidate"]["defender"]["originalSide"]
    require(attacker == row["actingSide"] and defender != attacker,
            "original actor orientation")
    for kind, actor, now in (
        ("open-selection", "system", 1000),
        ("select-assault", attacker, 1001),
        ("complete-step", "system", None),
        ("complete-step", "system", None),
        ("open-rba", "system", 1002),
    ):
        value = selected.expected_input(state, kind, actor=actor, now=now)
        source, ledger, event, _ = selected.apply(
            source, parents, r03, ledger, selected.input_bytes(value))
        require(event is not None, "five distinct canonical events")
        state = selected.replay(source, parents, r03, ledger)
    require(len(ledger) == 5 and state["rbaOpenReceiptId"] ==
            state["lastReceiptId"] and state["rbaOutcome"] is None and
            state["admitted"] is False and state["publicActions"] == [],
            "trusted defender-owned opening")
    # The decline is a supplied, typed TEST input. There is no authenticated
    # recorded owner decision in the original eleven-ledger R03 evidence.
    decline = selected.input_bytes(selected.expected_input(
        state, "decline-rba", actor=defender, now=1003))
    next_source, next_ledger, event, receipt = selected.apply(
        source, parents, r03, ledger, decline)
    after = selected.replay(next_source, parents, r03, next_ledger)
    world = selected.encode(state["parent"]["world"])
    random = selected.encode(state["parent"]["randomState"])
    require(after["rbaOutcome"] == "declined" and
            after["parent"] == state["parent"] and
            selected.encode(after["parent"]["world"]) == world and
            selected.encode(after["parent"]["randomState"]) == random and
            after["admitted"] is False and after["publicActions"] == [],
            "decline resource conservation")
    completion = selected.input_bytes(selected.expected_input(
        after, "complete-step", actor="system"))
    final_source, final_ledger, final_event, final_receipt = selected.apply(
        next_source, parents, r03, next_ledger, completion)
    final = selected.replay(final_source, parents, r03, final_ledger)
    require(final_event is not None and len(final_ledger) == 7 and
            final["closed"] and final["stepIndex"] == 3 and
            final["position"]["positionId"].endswith(".force-assignment") and
            final["rbaOutcome"] == "declined" and
            final["declineReceiptId"] == receipt and
            final["lastReceiptId"] == final_receipt and
            final["parent"] == state["parent"] and
            selected.encode(final["parent"]["world"]) == world and
            selected.encode(final["parent"]["randomState"]) == random and
            final["admitted"] is False and final["publicActions"] == [],
            "canonical same-slot Force Assignment completion")
    claim = dict(source=b64(source), creationRequest=b64(selected.encode(parents[0])),
                 parentLedgers=[[b64(x) for x in group] for group in parents[1:]],
                 r03Ledger=[b64(x) for x in r03],
                 ownLedger=[b64(x) for x in ledger],
                 expectedControl=b64(selected.control_bytes(state)),
                 expectedWorld=b64(world), expectedRandom=b64(random),
                 expectedReceiptId=state["lastReceiptId"],
                 retryInput=b64(ledger[4]), declineInput=b64(decline),
                 completionInput=b64(completion))
    expected = dict(nextSource=b64(next_source),
                    nextLedger=[b64(x) for x in next_ledger],
                    nextEvent=b64(event), nextReceiptId=receipt,
                    nextControl=b64(selected.control_bytes(after)),
                    nextPrefix=after["prefix"], nextVersion=after["stateVersion"],
                    finalSource=b64(final_source),
                    finalLedger=[b64(x) for x in final_ledger],
                    finalEvent=b64(final_event), finalReceiptId=final_receipt,
                    finalControl=b64(selected.control_bytes(final)),
                    finalPrefix=final["prefix"], finalVersion=final["stateVersion"],
                    finalPositionId=final["position"]["positionId"])
    return dict(claim=claim, expected=expected, state=state,
                source=source, ledger=ledger, r03=r03,
                event=event, receipt=receipt, after=after,
                finalSource=final_source, finalLedger=final_ledger,
                finalEvent=final_event, finalReceipt=final_receipt, final=final,
                world=world, random=random)


def child(claim):
    incoming = selected.encode(claim)
    require(len(incoming) <= 16 * 1024 * 1024, "claim byte cap")
    with tempfile.TemporaryDirectory(prefix="p01-rba-decline-") as directory:
        path = Path(directory) / "claim.json"
        path.write_bytes(incoming)
        process = subprocess.run([sys.executable, "-B", str(CHILD), str(path)],
                                 stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                 check=False, timeout=180)
    require(process.returncode == 0 and not process.stderr and
            len(process.stdout) <= 4 * 1024 * 1024,
            "fresh process status/stderr/output cap: " +
            process.stderr.decode("utf-8", "replace")[:500])
    observed = json.loads(process.stdout)
    require(type(observed) is dict, "fresh output object")
    return observed


def verify_row(row):
    case = build(row)
    observed = child(case["claim"])
    require("rejected" not in observed, "fresh readback rejected: " + str(observed))
    require(all(observed.get(key) == value for key, value in case["expected"].items()),
            "fresh decline/completion differs byte-for-byte from parent")
    state = case["state"]
    require(observed["control"] == case["claim"]["expectedControl"] and
            observed["sourceHash"] == digest(case["source"]) and
            observed["ledgerHash"] == ledger_hash(case["ledger"]) and
            observed["worldHash"] == digest(case["world"]) and
            observed["randomHash"] == digest(case["random"]) and
            observed["receiptId"] == state["lastReceiptId"] and
            observed["prefix"] == state["prefix"] and
            observed["version"] == state["stateVersion"] and
            observed["retryReceiptId"] == state["rbaOpenReceiptId"] and
            observed["retryEvent"] is None and
            observed["admitted"] is False and observed["publicActions"] == [],
            "fresh RBA-open cut differs byte-for-byte from parent")
    return dict(index=row["index"], rowKey=row["rowKey"],
                actingSide=row["actingSide"],
                r03SourceHash=row["r03SourceHash"],
                r03LedgerHash=ledger_hash(case["r03"]),
                openedSourceHash=digest(case["source"]),
                openedLedgerHash=ledger_hash(case["ledger"]),
                openedControlHash=digest(selected.control_bytes(state)),
                openedReceiptId=state["rbaOpenReceiptId"],
                declinedSourceHash=digest(base64.b64decode(observed["nextSource"])),
                declinedLedgerHash=ledger_hash([base64.b64decode(x)
                                                for x in observed["nextLedger"]]),
                declinedEventHash=digest(case["event"]),
                declinedControlHash=digest(selected.control_bytes(case["after"])),
                declinedReceiptId=case["receipt"],
                finalSourceHash=digest(case["finalSource"]),
                finalLedgerHash=ledger_hash(case["finalLedger"]),
                finalEventHash=digest(case["finalEvent"]),
                finalControlHash=digest(selected.control_bytes(case["final"])),
                finalReceiptId=case["finalReceipt"],
                finalPositionId=case["final"]["position"]["positionId"],
                worldHash=digest(case["world"]), randomHash=digest(case["random"]),
                admitted=False, publicActions=[])


def artifact_bytes(value):
    return json.dumps(value, sort_keys=True, ensure_ascii=True,
                      separators=(",", ":")).encode("ascii")


def exact_sha(value):
    require(type(value) is str and re.fullmatch(r"[0-9a-f]{40}", value),
            "exact proof source SHA")


def verify(*, shard=None, jobs=4, tested_sha=None, freeze=False):
    if tested_sha is not None:
        exact_sha(tested_sha)
    require(shard is None or type(shard) is int and 0 <= shard < 4,
            "four-way shard")
    require(type(jobs) is int and 1 <= jobs <= 4, "bounded concurrency")
    rows = pinned_rows()
    if shard is not None:
        rows = rows[shard::4]
    with ThreadPoolExecutor(max_workers=jobs) as executor:
        futures = {executor.submit(verify_row, row): row["index"] for row in rows}
        by_index = {futures[future]: future.result()
                    for future in as_completed(futures)}
    output = [by_index[row["index"]] for row in rows]
    require(len(output) == len(rows), "complete selected subset")
    if shard is None:
        require(len(output) == 44 and
                [x["actingSide"] for x in output] ==
                ["axis"] * 22 + ["commonwealth"] * 22,
                "all 44 original positives")
    ordered = artifact_bytes(output)
    result = dict(shard=shard, testedSha=tested_sha, catalogueRows=300,
                  freshOpenCuts=len(output), typedTestDeclines=len(output),
                  candidateAbsent=256, admittedRows=0, publicActions=0,
                  orientations={side: sum(x["actingSide"] == side for x in output)
                                for side in ("axis", "commonwealth")},
                  orderedOutputHash=digest(ordered),
                  orderedOutputBytes=len(ordered), rows=output)
    if shard is None and not freeze:
        fixture = json.loads(FIXTURE.read_text())
        require(fixture == dict(orderedOutputHash=result["orderedOutputHash"],
                                orderedOutputBytes=result["orderedOutputBytes"],
                                roots=[dict(index=x["index"], rowKey=x["rowKey"],
                                            actingSide=x["actingSide"])
                                       for x in output]), "frozen all-44 evidence")
    return result


def combine(paths, tested_sha=None):
    if tested_sha is not None:
        exact_sha(tested_sha)
    require(len(paths) == 4, "four shard artifacts")
    shards = [json.loads(Path(path).read_bytes()) for path in paths]
    require({x["shard"] for x in shards} == set(range(4)), "unique four shards")
    expected = pinned_rows()
    by_index = {}
    for item in shards:
        rows = item["rows"]
        claims = expected[item["shard"]::4]
        require(item["testedSha"] == tested_sha and
                item["catalogueRows"] == 300 and
                item["freshOpenCuts"] == item["typedTestDeclines"] == len(claims)
                and item["candidateAbsent"] == 256 and
                item["admittedRows"] == item["publicActions"] == 0 and
                [x["index"] for x in rows] == [x["index"] for x in claims] and
                all(x["rowKey"] == c["rowKey"] and
                    x["actingSide"] == c["actingSide"] and
                    x["r03SourceHash"] == c["r03SourceHash"]
                    for x, c in zip(rows, claims)) and
                item["orderedOutputHash"] == digest(artifact_bytes(rows)) and
                item["orderedOutputBytes"] == len(artifact_bytes(rows)),
                "shard identity and integrity")
        for row in rows:
            require(row["index"] not in by_index, "duplicate original root")
            by_index[row["index"]] = row
    output = [by_index[x["index"]] for x in expected]
    fixture = json.loads(FIXTURE.read_text())
    require(digest(artifact_bytes(output)) == fixture["orderedOutputHash"] and
            len(artifact_bytes(output)) == fixture["orderedOutputBytes"] and
            fixture["roots"] == [dict(index=x["index"], rowKey=x["rowKey"],
                                       actingSide=x["actingSide"]) for x in output],
            "frozen 44-root aggregate")
    return dict(testedSha=tested_sha, catalogueRows=300,
                freshOpenCuts=44, typedTestDeclines=44, candidateAbsent=256,
                admittedRows=0, publicActions=0,
                orientations={"axis": 22, "commonwealth": 22},
                orderedOutputHash=fixture["orderedOutputHash"],
                orderedOutputBytes=fixture["orderedOutputBytes"])


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--jobs", type=int, default=4)
    parser.add_argument("--shard", type=int)
    parser.add_argument("--combine", nargs="*", metavar="SHARD_JSON")
    parser.add_argument("--tested-sha")
    parser.add_argument("--freeze", action="store_true")
    args = parser.parse_args()
    require(not args.freeze or args.shard is None and args.combine is None,
            "freeze full matrix only")
    answer = (combine(args.combine, tested_sha=args.tested_sha)
              if args.combine is not None else
              verify(shard=args.shard, jobs=args.jobs,
                     tested_sha=args.tested_sha, freeze=args.freeze))
    print(json.dumps(answer, sort_keys=True, separators=(",", ":")))
