#!/usr/bin/env python3
"""Private P01 AC-008 terminal-Source ancestor-cut rejection proof; no admission."""
import argparse
import copy
import gzip
import hashlib
import importlib.util
import json
import re
import subprocess
from pathlib import Path
from unittest import mock

ROOT = Path(__file__).resolve().parent
FIXTURE = ROOT / "fixtures/combat-p01-all-prefix-reject-v1.json"
REVIEWED_ROOT = "fde84107d285ac5be6824bef4a939bf642eddbdd1624ff6d2a29f50ce4d5dede"
LEDGER_VECTOR_ROOT = "c925de99654e4a353a8f226aef01c540f5b8db3cbcb6b21925856b21e392a824"
RETAINED_SUMMARY_SHA = "847972f5eb9c12be2e7a1774d2acd61caa1dc603b43413f7fb9d30cbec80c45b"
SHARD_PREFIX_COUNTS = (2896, 3006, 2902, 3012)


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


p01 = load("all_prefix_p01", "verify-combat-actual-moved-repeat-position-v1.py")
r03 = p01.r03
ancestry = load("all_prefix_ancestry", "verify-combat-actual-moved-repeat-position-evidence-v1.py")


def require(condition, label):
    if not condition:
        raise AssertionError("CMB-P01-ALL-PREFIX " + label)


def encode(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("ascii")


def digest(value):
    return hashlib.sha256(value).hexdigest()


def result_digest(records):
    """Artifact object key order is irrelevant; row order remains authoritative."""
    return digest(json.dumps(records, ensure_ascii=True, sort_keys=True,
                             separators=(",", ":")).encode("ascii"))


def tested_head(value=None):
    actual = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT,
                                     text=True).strip()
    require(re.fullmatch(r"[0-9a-f]{40}", actual) is not None, "Git HEAD")
    if value is not None:
        require(re.fullmatch(r"[0-9a-f]{40}", value) is not None and value == actual,
                "exact tested Git head")
    return actual


def reviewed_rows():
    fixture = json.loads(FIXTURE.read_bytes())
    fields = ("index", "rowKey", "sourceHash", "controlHash", "proofHash")
    rows = fixture["rows"]
    require(fixture["contractVersion"] == 1 and len(rows) == 300 and
            fixture["sourceSummarySha256"] == RETAINED_SUMMARY_SHA,
            "frozen catalogue")
    require(all(list(row) == list(fields) and row["index"] == index
                for index, row in enumerate(rows)), "frozen row field order")
    require(fixture["orderedReviewedRootSha256"] == REVIEWED_ROOT ==
            digest(encode(rows)), "frozen reviewed summary root")
    vectors = fixture["ledgerVectorRootsSha256"]
    require(len(vectors) == 300 and
            all(re.fullmatch(r"[0-9a-f]{64}", value) for value in vectors) and
            fixture["orderedLedgerVectorRootSha256"] == LEDGER_VECTOR_ROOT ==
            digest(encode(vectors)), "frozen eleven-ledger vector root")
    return fixture, rows


def check_retained(evidence):
    """Optional local check that the committed pins came from reviewed private bytes."""
    evidence = Path(evidence)
    fixture, frozen = reviewed_rows()
    for name, expected in ancestry.PINS.items():
        require(digest((evidence / name).read_bytes()) == expected,
                "retained evidence pin " + name)
    summary = json.loads((evidence / "summary.json").read_bytes())
    manifest = json.loads((evidence / "manifest.json").read_bytes())
    require(len(summary["rows"]) == len(frozen) == 300 and
            len(manifest["chunks"]) == 11, "retained catalogue")
    index = 0
    for chunk in manifest["chunks"]:
        data = (evidence / chunk["name"]).read_bytes()
        require(digest(data) == chunk["sha256"] ==
                summary["chunkSha256"][chunk["name"]], "retained chunk hash")
        for line in gzip.decompress(data).splitlines():
            item = json.loads(line)
            vectors = item["r03"]["vectors"]
            row = frozen[index]
            require(item["index"] == row["index"] and
                    item["r03"]["rowKey"] == row["rowKey"] ==
                    summary["rows"][index]["rowKey"], "retained row identity")
            for field, vector, retained in (
                    ("sourceHash", "sourceCanonicalUtf8", "sourceHash"),
                    ("controlHash", "afterControlCanonicalUtf8", "afterControlHash"),
                    ("proofHash", "afterProofCanonicalUtf8", "afterProofHash")):
                require(row[field] == summary["rows"][index][field] ==
                        item["r03"][retained] ==
                        "sha256:" + digest(vectors[vector].encode("ascii")),
                        "retained vector " + field)
            require(digest(encode([vectors["parentInputLedgersCanonicalUtf8"],
                                   vectors["inputCanonicalUtf8"]])) ==
                    fixture["ledgerVectorRootsSha256"][index],
                    "retained eleven-ledger vector")
            index += 1
    require(index == 300, "retained complete rows")
    return dict(rows=index, sourceSummarySha256=RETAINED_SUMMARY_SHA,
                orderedReviewedRootSha256=REVIEWED_ROOT,
                orderedLedgerVectorRootSha256=LEDGER_VECTOR_ROOT)


def genuine_root(index, row, semantic_rows, vector_pin):
    """Regenerate in-repo, then compare full canonical Source, Control and Proof pins."""
    semantic = semantic_rows[index]
    require(semantic["rowKey"] == row["rowKey"], "semantic row key")
    parent_source, ledgers = r03.parent_terminal(semantic)
    source = r03.source_for(parent_source)
    _, context, before = r03.replay(source, ledgers, [])
    command = r03.raw(r03.command(context, before), "Input")
    source, own, event, _ = r03.apply(source, ledgers, [], command)
    require(event is not None and len(ledgers) == 11 and len(own) == 1,
            "authentic terminal ledger shape")
    vectors = [ledgers[0], *[[item.decode("ascii") for item in ledger]
                             for ledger in ledgers[1:]]]
    vector_hash = digest(encode([vectors, own[0].decode("ascii")]))
    require(vector_hash == vector_pin,
            "frozen authentic CreationRequest, eleven ledgers, and own input " + str(index))
    _, _, state = r03.replay(source, ledgers, own)
    observed = {"sourceHash": r03.sha(source),
                "controlHash": r03.sha(r03.raw(state, "Control")),
                "proofHash": r03.sha(r03.raw(r03.proof(source, ledgers, own), "Proof"))}
    require(all(observed[field] == row[field] for field in observed),
            "frozen authentic Source/Control/Proof row " + str(index))
    assessment = p01.assess(source, ledgers, own)
    require(assessment["status"] == "unsupported" and
            assessment["admitted"] is False and assessment["publicActions"] == [] and
            assessment["sourceHash"] == row["sourceHash"],
            "authentic root remains private and unsupported")
    return source, ledgers, own, assessment["physicalShape"], vector_hash


def reject_cut(source, ledgers, own, ledger_index, cut):
    parents = ledgers.copy()
    if ledger_index == 11:
        require(cut == 0 and len(own) == 1, "missing own input")
        candidate_own = []
    else:
        require(1 <= ledger_index <= 10 and 0 <= cut < len(ledgers[ledger_index]),
                "strict nonterminal parent prefix")
        parents[ledger_index] = ledgers[ledger_index][:cut]
        candidate_own = own
    try:
        p01.assess(source, parents, candidate_own)
    except ValueError:
        return
    raise AssertionError("CMB-P01-ALL-PREFIX accepted truncated ledger")


def verify_row(index, row, semantic_rows, vector_pin):
    source, ledgers, own, shape, vector_hash = genuine_root(
        index, row, semantic_rows, vector_pin)
    before = copy.deepcopy((ledgers, own))
    cuts = []
    # The frozen final Source and the own R03 input stay unchanged while each
    # ancestor ledger is truncated at every nonterminal prefix independently.
    with mock.patch.object(p01, "physical_shape",
                           side_effect=AssertionError("classified before replay authentication")):
        for ledger_index in range(1, 11):
            for cut in range(len(ledgers[ledger_index])):
                reject_cut(source, ledgers, own, ledger_index, cut)
                cuts.append((ledger_index, cut))
        reject_cut(source, ledgers, own, 11, 0)
        cuts.append((11, 0))
    require((ledgers, own) == before, "authentic input mutation")
    return dict(index=index, rowKey=row["rowKey"], ledgerVectorHash=vector_hash,
                sourceHash=row["sourceHash"],
                controlHash=row["controlHash"], proofHash=row["proofHash"],
                physicalShape=shape, ancestorPrefixRejections=len(cuts) - 1,
                ownInputMissingRejections=1, orderedCutRoot=digest(encode(cuts)),
                admitted=False, publicActions=[])


def shard_report(shard, *, tested_sha=None):
    require(type(shard) is int and 0 <= shard < 4, "shard index")
    head = tested_head(tested_sha)
    fixture, frozen = reviewed_rows()
    semantic_rows = r03.r02.semantic_rows()
    require(len(semantic_rows) == 300, "complete semantic catalogue")
    rows = [verify_row(index, frozen[index], semantic_rows,
                       fixture["ledgerVectorRootsSha256"][index])
            for index in range(shard, 300, 4)]
    require(len(rows) == 75 and all(row["index"] % 4 == shard for row in rows),
            "complete shard mapping")
    require(sum(row["ancestorPrefixRejections"] for row in rows) ==
            SHARD_PREFIX_COUNTS[shard] and
            sum(row["ownInputMissingRejections"] for row in rows) == 75,
            "frozen shard cut count")
    return dict(contractVersion=1, testedSha=head, shard=shard,
                orderedReviewedRootSha256=REVIEWED_ROOT, rows=rows)


def combine(paths, *, tested_sha=None):
    require(len(paths) == 4, "four shard artifacts required")
    head = tested_head(tested_sha)
    fixture, frozen = reviewed_rows()
    records = []
    for shard, path in enumerate(paths):
        artifact = json.loads(Path(path).read_bytes())
        require(artifact["contractVersion"] == 1 and artifact["testedSha"] == head
                and artifact["shard"] == shard and
                artifact["orderedReviewedRootSha256"] == REVIEWED_ROOT,
                "same-head same-shard artifact")
        rows = artifact["rows"]
        require(len(rows) == 75 and
                [row["index"] for row in rows] == list(range(shard, 300, 4)),
                "full shard index coverage")
        records.extend(rows)
    records.sort(key=lambda row: row["index"])
    require(len(records) == 300 and
            [row["index"] for row in records] == list(range(300)),
            "all original roots exactly once")
    require(all(row["ledgerVectorHash"] ==
                fixture["ledgerVectorRootsSha256"][index] and
                all(row[field] == frozen[index][field]
                    for field in ("index", "rowKey", "sourceHash", "controlHash", "proofHash"))
                for index, row in enumerate(records)),
            "all regenerated roots agree with reviewed Source/Control/Proof")
    parent_cuts = sum(row["ancestorPrefixRejections"] for row in records)
    own_cuts = sum(row["ownInputMissingRejections"] for row in records)
    require(parent_cuts == 11816 and own_cuts == 300,
            "all 12,116 nonterminal and own cuts")
    require(sum(row["physicalShape"] == "provisional-positive" for row in records) == 44
            and sum(row["physicalShape"] == "provisional-empty" for row in records) == 256
            and all(row["admitted"] is False and row["publicActions"] == []
                    for row in records), "44/256/0 fail-closed partition")
    report = dict(contractVersion=1, testedSha=head, rows=300,
                  parentPrefixRejections=parent_cuts,
                  ownInputMissingRejections=own_cuts,
                  totalRejections=parent_cuts + own_cuts,
                  provisionalPositive=44, provisionalEmpty=256, admittedRows=0,
                  orderedReviewedRootSha256=REVIEWED_ROOT,
                  orderedResultRootSha256=result_digest(records),
                  status="partial private AC-008 terminal-Source tamper rejection; no admission")
    expected = fixture["expected"]
    require(set(expected) == {"parentPrefixRejections", "ownInputMissingRejections",
                              "totalRejections", "orderedResultRootSha256"} and
            expected == {key: report[key] for key in expected},
            "frozen ordered result fixture")
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--shard", type=int)
    group.add_argument("--combine", nargs=4, type=Path)
    group.add_argument("--retained-evidence", type=Path)
    parser.add_argument("--tested-sha")
    args = parser.parse_args()
    if args.retained_evidence is not None:
        output = check_retained(args.retained_evidence)
    elif args.shard is not None:
        output = shard_report(args.shard, tested_sha=args.tested_sha)
    else:
        output = combine(args.combine, tested_sha=args.tested_sha)
    print(json.dumps(output, indent=2, sort_keys=True))
