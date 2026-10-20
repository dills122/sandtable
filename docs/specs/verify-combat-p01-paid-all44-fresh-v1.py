#!/usr/bin/env python3
"""All-44 private paid cuts rehydrated by independent Python interpreters."""
import argparse
import base64
from concurrent.futures import ThreadPoolExecutor, as_completed
import importlib.util
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent
CHILD = ROOT / "combat-p01-paid-all44-fresh-child-v1.py"
FIXTURE = json.loads((ROOT / "fixtures/combat-p01-paid-all44-cuts-v2.json").read_text())
FRESH_FIXTURE = json.loads((ROOT / "fixtures/combat-p01-paid-all44-fresh-v1.json").read_text())


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


cuts = load("all44_inprocess_for_fresh", "verify-combat-p01-paid-all44-cuts-v2.py")
fresh_v1 = load("two_root_fresh_for_all44", "test-combat-p01-paid-fresh-readback-v1.py")
ref = cuts.ref


def require(condition, label):
    if not condition:
        raise AssertionError(label)


def artifact_bytes(value):
    return json.dumps(value, sort_keys=True, separators=(",", ":"),
                      ensure_ascii=True).encode("ascii")


def predecessor_row(row):
    # JSON artifacts sort object keys. Rebuild the predecessor's *ordered*
    # canonical byte shape before comparing its immutable all-44 digest.
    keys = ("index", "rowKey", "actingSide", "preparedControlHash",
            "preparedWorldHash", "preparedReceiptId", "committedControlHash",
            "committedWorldHash", "committedReceiptId", "eventHash", "sizes")
    extra = ("preparedSourceHash", "preparedLedgerHash",
             "committedSourceHash", "committedLedgerHash")
    require(set(row) == set(keys + extra), "fresh row field set")
    sizes = row["sizes"]
    size_keys = ("sourceBytes", "inputBytes", "eventBytes", "controlBytes",
                 "worldBytes")
    require(set(sizes) == set(size_keys), "measured byte field set")
    return {key: ({size: sizes[size] for size in size_keys}
                  if key == "sizes" else row[key]) for key in keys}


def b64(value):
    return base64.b64encode(value).decode("ascii")


def ledger_hash(ledger):
    return cuts.digest(ref.encode([b64(item) for item in ledger]))


def child(mode, claim, path):
    require(mode in ("prepared", "committed"), "fresh cut mode")
    path.write_bytes(ref.encode(claim))
    process = subprocess.run(
        [sys.executable, "-B", str(CHILD), mode, str(path)],
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False, timeout=300)
    require(process.returncode == 0, "fresh process exit: " +
            process.stderr.decode("utf-8", "replace")[:1000])
    require(len(process.stdout) <= 1024 * 1024, "fresh output cap")
    result = json.loads(process.stdout)
    require(type(result) is dict and "rejected" not in result,
            "fresh readback rejected: " + str(result))
    return result


def compare_cut(observed, claim, state, source, own, label):
    require(observed["control"] == claim["expectedControl"] ==
            b64(ref.control_bytes(state)), label + " canonical Control")
    require(observed["world"] == claim["expectedWorld"] ==
            b64(ref.encode(state["world"])), label + " canonical World")
    require(observed["receiptId"] == claim["expectedReceiptId"] ==
            state["lastReceiptId"], label + " receipt")
    require(observed["sourceHash"] == cuts.digest(source) and
            observed["ledgerHash"] == ledger_hash(own),
            label + " canonical Source/ledger")
    require((observed["status"], observed["stateVersion"], observed["prefix"]) ==
            (state["status"], state["stateVersion"], state["prefix"]),
            label + " state identity")
    require(observed["attackHistory"] == state["attackHistory"] and
            observed["targetUses"] == state["targetUses"],
            label + " history and target uses")
    require(observed["resources"] == [
        dict(cp=element["operationalState"]["capabilityPointsExpended"],
             ammunition=element["ammunition"]["points"])
        for element in state["world"]["elements"]], label + " resources")
    require(observed["admitted"] is False and observed["publicActions"] == [],
            label + " private gate")


def verify_row(row, semantic_rows):
    index, side = row["index"], row["actingSide"]
    require(semantic_rows[index]["rowKey"] == row["rowKey"], "row identity")
    root = cuts.paid.fixture.fixture.terminal(index)
    require(cuts.digest(root[0]) == row["r03SourceHash"], "R03 source pin")
    require(len(root[1]) == 11 and len(root[2]) == 1 and
            type(root[1][0]) is dict and
            all(type(ledger) is list and all(type(item) is bytes for item in ledger)
                for ledger in [*root[1][1:], root[2]]),
            "original CreationRequest and eleven R03 ancestor ledgers")
    selected_source, selected_ledger = cuts.paid.fixture.selected_path(root)
    assignment_source, assignment_ledger = cuts.paid.prepared(
        root, selected_source, selected_ledger, ("attacker", "defender"))
    source = ref.source_for(assignment_source)
    before = ref.replay(source, root[1], root[2], selected_ledger,
                        assignment_ledger, [])
    require(before["parent"]["parent"]["parent"]["activeCycle"]
            ["actingSide"] == side, "actor orientation")
    command = ref.input_bytes(ref.expected_input(before))
    intended, event, receipt = ref.transition(before, ref.parse_input(command))
    packet = ref.parse_source(source)
    packet["eventCanonicalUtf8"].append(event.decode("ascii"))
    committed_source, own = ref.encode(packet), [command]
    cuts.assert_conservation(before, intended, event)
    limits = ref.SCHEMA["limits"]
    sizes = dict(sourceBytes=len(committed_source), inputBytes=len(command),
                 eventBytes=len(event), controlBytes=len(ref.control_bytes(intended)),
                 worldBytes=max(len(ref.encode(before["world"])),
                                len(ref.encode(intended["world"]))))
    for field, cap in (("sourceBytes", "sourceBytes"),
                       ("inputBytes", "inputBytes"),
                       ("eventBytes", "eventBytes"),
                       ("controlBytes", "controlBytes"),
                       ("worldBytes", "projectedWorldBytes")):
        require(sizes[field] <= limits[cap], field + " cap")
    before_claim = fresh_v1.payload(root, selected_ledger, assignment_ledger,
                                    source, [], before)
    after_claim = fresh_v1.payload(root, selected_ledger, assignment_ledger,
                                   committed_source, own, intended, retry=command)
    ancestry = ("creationRequest", "parentInputLedgers", "r03Ledger",
                "selectedLedger", "assignmentLedger")
    require(all(before_claim[key] == after_claim[key] for key in ancestry),
            "cuts have different original ancestry")
    with tempfile.TemporaryDirectory(prefix="p01-all44-fresh-") as folder:
        prepared_result = child("prepared", before_claim,
                                Path(folder) / "prepared.json")
        committed_result = child("committed", after_claim,
                                 Path(folder) / "committed.json")
    require(set(prepared_result) == {"cut"} and
            set(committed_result) == {"cut", "retry"},
            "independent cut process outputs")
    prepared, committed = prepared_result["cut"], committed_result["cut"]
    compare_cut(prepared, before_claim, before, source, [], "prepared")
    compare_cut(committed, after_claim, intended, committed_source, own,
                "committed")
    require(prepared["status"] == "prepared" and
            prepared["attackHistory"] == prepared["targetUses"] == [] and
            committed["status"] == "committed" and
            len(committed["attackHistory"]) ==
            len(committed["targetUses"]) == 1, "cut status and history")
    retry = committed_result["retry"]
    require(retry == dict(receiptId=receipt, event=None,
                          sourceHash=cuts.digest(committed_source),
                          ledgerHash=ledger_hash(own)),
            "fresh accepted retry did not preserve source/ledger/receipt")
    return dict(index=index, rowKey=row["rowKey"], actingSide=side,
                preparedControlHash=cuts.digest(ref.control_bytes(before)),
                preparedWorldHash=cuts.digest(ref.encode(before["world"])),
                preparedSourceHash=prepared["sourceHash"],
                preparedLedgerHash=prepared["ledgerHash"],
                preparedReceiptId=prepared["receiptId"],
                committedControlHash=cuts.digest(ref.control_bytes(intended)),
                committedWorldHash=cuts.digest(ref.encode(intended["world"])),
                committedSourceHash=committed["sourceHash"],
                committedLedgerHash=committed["ledgerHash"],
                committedReceiptId=receipt, eventHash=cuts.digest(event), sizes=sizes)


def exact_sha(value):
    require(type(value) is str and re.fullmatch(r"[0-9a-f]{40}", value),
            "tested Git SHA")
    actual = subprocess.check_output(["git", "rev-parse", "HEAD"],
                                     cwd=ROOT, text=True).strip()
    require(actual == value, "checkout differs from tested Git SHA")
    return value


def verify(*, jobs=4, shard=None, tested_sha=None):
    if tested_sha is not None:
        exact_sha(tested_sha)
    cuts.check_retained_pins()
    rows = FIXTURE["roots"]
    require(len(rows) == 44 and [row["index"] for row in rows] ==
            list(range(36, 300, 6)) and
            [row["actingSide"] for row in rows] ==
            ["axis"] * 22 + ["commonwealth"] * 22,
            "ordered all-44 actor partition")
    require(FIXTURE["candidateAbsent"] == 256 and
            FIXTURE["admitted"] is False and FIXTURE["publicActions"] == [],
            "300-root private partition")
    semantic_rows = cuts.paid.fixture.fixture.r03.r02.semantic_rows()
    require(len(semantic_rows) == 300, "authentic catalogue")
    require(type(jobs) is int and 1 <= jobs <= 4, "bounded child concurrency")
    require(shard is None or (type(shard) is int and 0 <= shard < 4),
            "four-way shard index")
    if shard is not None:
        rows = rows[shard::4]
    with ThreadPoolExecutor(max_workers=jobs) as executor:
        futures = {executor.submit(verify_row, row, semantic_rows): row["index"]
                   for row in rows}
        output_by_index = {futures[future]: future.result()
                           for future in as_completed(futures)}
    output = [output_by_index[row["index"]] for row in rows]
    # Strip the extra source/ledger fields to prove this is the same pinned
    # prepared/committed result matrix as the in-process predecessor.
    prior = [predecessor_row(row) for row in output]
    if shard is None:
        require(cuts.digest(ref.encode(prior)) == FIXTURE["orderedOutputHash"],
                "all-44 in-process matrix parity")
    encoded = artifact_bytes(output)
    maxima = {field: max(row["sizes"][field] for row in output)
              for field in output[0]["sizes"]}
    if shard is None:
        require(maxima == FIXTURE["maxima"], "measured cap parity")
        require(cuts.digest(encoded) == FRESH_FIXTURE["orderedOutputHash"] and
                len(encoded) == FRESH_FIXTURE["orderedOutputBytes"],
                "all-44 fresh ordered output pin")
    return dict(shard=shard, testedSha=tested_sha, catalogueRows=300,
                freshPreparedCuts=len(output), freshCommittedCuts=len(output),
                candidateAbsent=256, admittedRows=0, publicActions=0,
                orientations={side: sum(row["actingSide"] == side for row in output)
                              for side in ("axis", "commonwealth")},
                orderedOutputHash=cuts.digest(encoded),
                orderedOutputBytes=len(encoded), maxima=maxima, rows=output)


def combine(paths, tested_sha=None):
    if tested_sha is not None:
        exact_sha(tested_sha)
    require(len(paths) == 4, "four independent shard artifacts required")
    shards = [json.loads(Path(path).read_bytes()) for path in paths]
    require({item["shard"] for item in shards} == set(range(4)),
            "each four-way shard exactly once")
    by_index = {}
    for item in shards:
        shard = item["shard"]
        require(item["testedSha"] == tested_sha, "shard exact Git SHA")
        expected = FIXTURE["roots"][shard::4]
        rows = item["rows"]
        require(item["catalogueRows"] == 300 and
                item["candidateAbsent"] == 256 and
                item["freshPreparedCuts"] == item["freshCommittedCuts"] ==
                len(expected) and item["admittedRows"] == item["publicActions"] == 0,
                "shard private partition")
        require([row["index"] for row in rows] ==
                [row["index"] for row in expected] and
                all(row["rowKey"] == claim["rowKey"] and
                    row["actingSide"] == claim["actingSide"]
                    for row, claim in zip(rows, expected)),
                "shard ordered authentic roots")
        require(item["orderedOutputHash"] == cuts.digest(artifact_bytes(rows)) and
                item["orderedOutputBytes"] == len(artifact_bytes(rows)),
                "shard output artifact integrity")
        for row in rows:
            require(row["index"] not in by_index, "duplicate fresh root")
            by_index[row["index"]] = row
    output = [by_index[row["index"]] for row in FIXTURE["roots"]]
    prior = [predecessor_row(row) for row in output]
    require(cuts.digest(ref.encode(prior)) == FIXTURE["orderedOutputHash"],
            "all-44 in-process matrix parity")
    maxima = {field: max(row["sizes"][field] for row in output)
              for field in output[0]["sizes"]}
    require(maxima == FIXTURE["maxima"], "all-44 measured cap parity")
    encoded = artifact_bytes(output)
    require(cuts.digest(encoded) == FRESH_FIXTURE["orderedOutputHash"] and
            len(encoded) == FRESH_FIXTURE["orderedOutputBytes"] and
            maxima == FRESH_FIXTURE["maxima"], "all-44 fresh evidence pin")
    return dict(testedSha=tested_sha, catalogueRows=300,
                freshPreparedCuts=44, freshCommittedCuts=44,
                candidateAbsent=256, admittedRows=0, publicActions=0,
                orientations={"axis": 22, "commonwealth": 22},
                orderedOutputHash=cuts.digest(encoded),
                orderedOutputBytes=len(encoded), maxima=maxima)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--jobs", type=int, default=4)
    parser.add_argument("--shard", type=int)
    parser.add_argument("--combine", nargs="*", metavar="SHARD_JSON")
    parser.add_argument("--tested-sha")
    args = parser.parse_args()
    result = combine(args.combine, tested_sha=args.tested_sha) if args.combine is not None else verify(
        jobs=args.jobs, shard=args.shard, tested_sha=args.tested_sha)
    print(json.dumps(result, sort_keys=True, separators=(",", ":")))
