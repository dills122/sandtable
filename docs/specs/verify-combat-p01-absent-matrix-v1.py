#!/usr/bin/env python3
"""Physical, creation-rooted P01 absent-row issuance and one-event replay proof.

This private audit is limited to the frozen 300 R03 roots. It does not certify
source-legal action exhaustiveness, supported-empty Combat, or P01 admission.
"""

import argparse
import gzip
import hashlib
import importlib.util
import json
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent
GOLDEN = ROOT / "fixtures/combat-p01-absent-matrix-v1.json"


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


result = load("absent_result", "verify-combat-p01-result-gates-v1.py")
selected = load("absent_selected", "verify-combat-p01-selected-assault-v1.py")
rba = load("absent_rba", "verify-combat-p01-explicit-rba-v1.py")
matrix = load("absent_action_matrix", "verify-combat-p01-action-matrix-v1.py")


def require(ok, label):
    if not ok:
        raise ValueError("CMB-P01-ABSENT " + label)


def digest(raw):
    return "sha256:" + hashlib.sha256(raw).hexdigest()


def canonical(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("ascii")


def root_from(record):
    vectors = record["vectors"]
    source = vectors["sourceCanonicalUtf8"].encode("ascii")
    parents = result.ancestry.original_ledgers(
        vectors["parentInputLedgersCanonicalUtf8"])
    own = [vectors["inputCanonicalUtf8"].encode("ascii")]
    require(digest(source) == record["sourceHash"], "original Source hash")
    require(digest(vectors["eventCanonicalUtf8"].encode("ascii")) ==
            record["eventHash"], "original event hash")
    source_value = rba.p01.r03.parse(source, "Source")
    require(source_value["breakdownEventCanonicalUtf8"][-1] ==
            vectors["eventCanonicalUtf8"], "original event vector")
    original_event = json.loads(vectors["eventCanonicalUtf8"])
    require(original_event["receiptId"] == record["receiptId"],
            "original receipt vector")
    return source, parents, own, source_value


def classify_record(record, partition):
    """Reconstruct R03 first; never decide issuance from catalogue labels."""
    source, parents, own, source_value = root_from(record)
    state = result.require_control_vector(record,
                                          result.authenticated_control(record))
    request = record["vectors"]["parentInputLedgersCanonicalUtf8"][0]
    content = rba.p01.selected_content()
    classification = matrix.classify(state, content, request)
    require(classification["physicalShape"] == partition and
            classification["admitted"] is False and
            classification["publicActions"] == [], "authenticated partition")

    common = dict(rowKey=record["rowKey"],
                  originalSourceHash=record["sourceHash"],
                  originalInputHash=digest(own[0]),
                  originalEventHash=record["eventHash"],
                  originalReceiptId=record["receiptId"],
                  originalControlHash=record["afterControlHash"],
                  actorSide=state["activeCycle"]["actingSide"],
                  assaultShape=classification["assaultShape"])
    try:
        candidate, candidate_id = selected.candidate_from_replayed(
            source_value, state, request)
    except selected.Invalid as error:
        require(str(error) == "CMB-P01-SELECT-003" and
                partition == "provisional-empty", "selected issuance failure")
        try:
            selected.replay(selected.source_for(source), parents, own, [])
        except selected.Invalid as replay_error:
            require(str(replay_error) == "CMB-P01-SELECT-003",
                    "selected initial replay failure")
        else:
            raise ValueError("CMB-P01-ABSENT unexpected selected initial issuance")
    else:
        require(partition == "provisional-positive" and
                classification["assaultShape"] == "provisional-adjacent" and
                selected.ident(candidate_id, "p01a.") and
                candidate["attacker"]["originalSide"] ==
                state["activeCycle"]["actingSide"], "positive selected control")
        try:
            rba.zero_assault_from_replayed(source_value, state)
        except rba.Invalid as error:
            require(str(error) == "CMB-P01-RBA-003", "positive RBA exclusion")
        else:
            raise ValueError("CMB-P01-ABSENT unexpected positive RBA issuance")
        return dict(**common, issuance="selected-positive-control",
                    selectedCandidateId=candidate_id)

    if classification["assaultShape"] == "no-loaded-acting-infantry":
        try:
            rba.replay(rba.source_for(source), parents, own, [])
        except rba.Invalid as error:
            require(str(error) == "CMB-P01-RBA-003", "paid RBA issuance failure")
        else:
            raise ValueError("CMB-P01-ABSENT unexpected paid RBA issuance")
        return dict(**common, issuance="paid-adjacent-no-issuance")

    require(classification["assaultShape"] == "no-adjacent-opposing-target",
            "nonadjacent physical shape")
    initial = rba.initial(source, parents, own)
    require(initial["certificate"]["rbaStillAvailable"] is True and
            initial["parent"] == state and initial["admitted"] is False and
            initial["publicActions"] == [] and initial["rbaDecision"] is None,
            "nonadjacent initial invariant")
    private_source = rba.source_for(source)
    raw_input = rba.input_bytes(rba.expected_input(
        initial, "close-no-assault-selection"))
    after, raw_event, receipt_id = rba.transition(
        initial, rba.parse_input(raw_input))
    require(after["parent"] == initial["parent"] and
            after["parent"]["world"] == state["world"] and
            after["parent"]["randomState"] == state["randomState"] and
            after["parent"]["targetUses"] == state["targetUses"] and
            after["parent"]["attackHistory"] == state["attackHistory"] and
            after["certificate"]["rbaStillAvailable"] is True and
            after["stepIndex"] == 0 and after["rbaOutcome"] is None and
            after["rbaDecision"] is None and after["declineReceiptId"] is None and
            after["admitted"] is False and after["publicActions"] == [],
            "nonadjacent transition conservation")
    event = json.loads(raw_event)
    require(event["eventType"] == "combat-selection-closed" and
            event["effect"]["proofKind"] == "zero-assault-only" and
            event["receiptId"] == receipt_id == after["lastReceiptId"] and
            event["effect"]["previousReceiptId"] ==
            state["breakdownCompletionReceiptId"] and
            event["effect"]["worldHash"] == digest(canonical(state["world"])) and
            event["effect"]["randomHash"] == digest(canonical(state["randomState"])),
            "nonadjacent event binding")
    source_value = rba.parse_source(private_source)
    source_value["eventCanonicalUtf8"].append(raw_event.decode("ascii"))
    updated = rba.encode(source_value)
    replayed = rba.replay(updated, parents, own, [raw_input])
    require(replayed == after and rba.control_bytes(replayed) ==
            rba.control_bytes(after), "nonadjacent one-event replay")
    return dict(**common, issuance="zero-assault-only-rba-choice-open",
                zeroAssaultCertificateId=initial["certificateId"],
                privateSourceHash=digest(updated),
                privateInputHash=digest(raw_input),
                privateEventHash=digest(raw_event),
                privateReceiptId=receipt_id,
                privateControlHash=digest(rba.control_bytes(after)),
                privateSourceBytes=len(updated),
                privateInputBytes=len(raw_input),
                privateEventBytes=len(raw_event),
                privateControlBytes=len(rba.control_bytes(after)))


def evidence_rows(evidence):
    evidence = Path(evidence)
    for name, expected in result.PINS.items():
        require(hashlib.sha256((evidence / name).read_bytes()).hexdigest() ==
                expected, "physical pin " + name)
    manifest = json.loads((evidence / "manifest.json").read_text())
    summary = json.loads((evidence / "summary.json").read_text())
    partition = json.loads((evidence / "bounded-profile-audit.json").read_text())
    chunks = manifest["chunks"]
    check_catalogue(chunks, summary, partition,
                    {p.name for p in evidence.glob("rows-*.jsonl.gz")})
    index = 0
    keys = set()
    for chunk in chunks:
        data = (evidence / chunk["name"]).read_bytes()
        require(hashlib.sha256(data).hexdigest() == chunk["sha256"] ==
                summary["chunkSha256"][chunk["name"]], "chunk pin")
        for line in gzip.decompress(data).splitlines():
            row = json.loads(line)
            record = row["r03"]
            key = record["rowKey"]
            check_row_identity(index, row, summary, partition, keys)
            keys.add(key)
            yield record, partition["rows"][index]["partition"]
            index += 1
    require(index == len(keys) == 300, "all physical rows")


def check_catalogue(chunks, summary, partition, physical_names):
    require(len(chunks) == 11 and summary["count"] ==
            len(summary["rows"]) == len(partition["rows"]) == 300 and
            len({chunk["name"] for chunk in chunks}) == 11 and
            {chunk["name"] for chunk in chunks} == set(summary["chunkSha256"]) ==
            physical_names, "complete physical catalogue")


def check_row_identity(index, row, summary, partition, seen):
    key = row["r03"]["rowKey"]
    require(row["index"] == index and key not in seen and
            key == summary["rows"][index]["rowKey"] ==
            partition["rows"][index]["rowKey"] and
            row["classification"] == summary["rows"][index]["classification"],
            "ordered row identity")


def audit(evidence, *, frozen=True, progress=None):
    rows = []
    counts = Counter()
    caps = {name: 0 for name in ("privateSourceBytes", "privateInputBytes",
                                "privateEventBytes", "privateControlBytes")}
    for record, partition in evidence_rows(evidence):
        output = classify_record(record, partition)
        rows.append(output)
        counts[output["issuance"]] += 1
        for name in caps:
            caps[name] = max(caps[name], output.get(name, 0))
        if progress:
            progress(len(rows), output["rowKey"], output["issuance"])
    require(len(rows) == 300 and counts == {
        "selected-positive-control": 44,
        "paid-adjacent-no-issuance": 12,
        "zero-assault-only-rba-choice-open": 244,
    } and {row["actorSide"] for row in rows
           if row["issuance"] == "selected-positive-control"} ==
            {"axis", "commonwealth"}, "complete issuance partition")
    output = dict(authenticatedRows=300, selectedPositive=44,
                  candidateAbsent=256, paidAdjacentNoIssuance=12,
                  zeroAssaultOnlyRbaChoiceOpen=244, admittedRows=0,
                  publicActionCount=0, maxBytes=caps,
                  orderedOutputHash=digest(canonical(rows)))
    if frozen:
        require(output == json.loads(GOLDEN.read_text()), "frozen output")
    return output


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("evidence", type=Path)
    parser.add_argument("--unfrozen", action="store_true")
    parser.add_argument("--progress", action="store_true")
    args = parser.parse_args()
    print(json.dumps(audit(args.evidence, frozen=not args.unfrozen,
                           progress=(lambda *items: print(*items, flush=True))
                           if args.progress else None), sort_keys=True))
