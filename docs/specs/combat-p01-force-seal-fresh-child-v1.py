#!/usr/bin/env python3
"""Replay one retained Force Assignment seal in a new Python interpreter."""
import base64
import binascii
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "force_seal_fresh_reference", ROOT / "verify-combat-p01-force-assignment-v1.py")
ref = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ref)


def require(condition, label):
    if not condition:
        raise ValueError(label)


def raw(value):
    require(type(value) is str, "base64 field type")
    result = base64.b64decode(value, validate=True)
    require(base64.b64encode(result).decode("ascii") == value, "base64 canonical")
    return result


def encoded(value):
    return base64.b64encode(value).decode("ascii")


def digest(value):
    return "sha256:" + hashlib.sha256(value).hexdigest()


def parse_claim(path):
    require(path.stat().st_size <= 16 * 1024 * 1024, "claim byte cap")
    incoming = path.read_bytes()

    def pairs(items):
        result = {}
        for key, value in items:
            require(key not in result, "duplicate claim key")
            result[key] = value
        return result

    claim = json.loads(incoming, object_pairs_hook=pairs)
    require(type(claim) is dict and list(claim) == [
        "source", "creationRequest", "parentInputLedgers", "r03Ledger",
        "selectedLedger", "ownLedger", "expectedControl", "expectedWorld",
        "expectedRandom", "expectedReceiptId", "retryInput", "secondInput"],
        "claim fields")
    require(json.dumps(claim, ensure_ascii=True, separators=(",", ":")).encode()
            == incoming, "claim canonical")
    source = raw(claim["source"])
    require(0 < len(source) <= ref.SCHEMA["limits"]["sourceBytes"],
            "source byte cap")
    creation_raw = raw(claim["creationRequest"])
    creation = json.loads(creation_raw)
    require(ref.encode(creation) == creation_raw, "CreationRequest canonical")
    parents = claim["parentInputLedgers"]
    require(type(parents) is list and len(parents) == 10 and
            all(type(ledger) is list for ledger in parents), "parent ledgers")
    parents = [creation, *[[raw(item) for item in ledger] for ledger in parents]]
    r03 = [raw(item) for item in claim["r03Ledger"]]
    selected = [raw(item) for item in claim["selectedLedger"]]
    own = [raw(item) for item in claim["ownLedger"]]
    require(len(own) == 2, "opening and one seal required")
    for item in own:
        ref.parse_input(item)
    retry = raw(claim["retryInput"])
    second = raw(claim["secondInput"])
    ref.parse_input(retry)
    ref.parse_input(second)
    return (source, parents, r03, selected, own, raw(claim["expectedControl"]),
            raw(claim["expectedWorld"]), raw(claim["expectedRandom"]),
            claim["expectedReceiptId"], retry, second)


def result(path):
    try:
        (source, parents, r03, selected, own, control, world, random,
         receipt, retry, second) = parse_claim(path)
        state = ref.replay(source, parents, r03, selected, own)
        require(state["status"] == "collecting" and
                sum(slot["sealedReceiptId"] is not None for slot in state["slots"]) == 1,
                "one-owner Collecting cut")
        require(ref.control_bytes(state) == control and
                ref.encode(state["parent"]["parent"]["world"]) == world and
                ref.encode(state["parent"]["parent"]["randomState"]) == random and
                state["lastReceiptId"] == receipt and
                state["admitted"] is False and state["publicActions"] == [],
                "retained cut mismatch")
        retry_source, retry_ledger, retry_event, retry_receipt = ref.apply(
            source, parents, r03, selected, own, retry)
        require((retry_source, retry_ledger, retry_event, retry_receipt) ==
                (source, own, None, receipt), "same-seal retry changed cut")
        next_source, next_ledger, event, next_receipt = ref.apply(
            source, parents, r03, selected, own, second)
        require(event is not None and len(next_ledger) == 3 and
                next_ledger[:2] == own and next_ledger[2] == second,
                "second owner seal append")
        after = ref.replay(next_source, parents, r03, selected, next_ledger)
        require(after["status"] == "prepared" and
                after["admitted"] is False and after["publicActions"] == [] and
                ref.encode(after["parent"]["parent"]["world"]) == world and
                ref.encode(after["parent"]["parent"]["randomState"]) == random,
                "second seal resource or admission change")
        return dict(status=state["status"], control=encoded(control),
                    worldHash=digest(world), randomHash=digest(random),
                    sourceHash=digest(source), receiptId=receipt,
                    stateVersion=state["stateVersion"], prefix=state["prefix"],
                    retryReceiptId=retry_receipt, retryEvent=None,
                    nextSource=encoded(next_source),
                    nextLedger=[encoded(item) for item in next_ledger],
                    nextEvent=encoded(event), nextReceiptId=next_receipt,
                    nextControl=encoded(ref.control_bytes(after)),
                    nextPrefix=after["prefix"], nextVersion=after["stateVersion"],
                    admitted=False, publicActions=[])
    except (ValueError, TypeError, KeyError, IndexError, OverflowError,
            binascii.Error, UnicodeError) as error:
        return dict(rejected="fresh-readback", error=str(error))


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print(json.dumps(dict(rejected="preflight", error="one claim path required")))
    else:
        print(json.dumps(result(Path(sys.argv[1])), ensure_ascii=True,
                         separators=(",", ":")))
