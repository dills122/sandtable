#!/usr/bin/env python3
"""Cold-read one private selected-assault RBA-open cut and typed test decline."""
import base64
import binascii
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "all44_rba_selected", ROOT / "verify-combat-p01-selected-assault-v1.py")
ref = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ref)


def require(ok, label):
    if not ok:
        raise ValueError(label)


def b64(raw):
    return base64.b64encode(raw).decode("ascii")


def unb64(value):
    require(type(value) is str, "base64 type")
    raw = base64.b64decode(value, validate=True)
    require(b64(raw) == value, "base64 canonical")
    return raw


def digest(raw):
    return "sha256:" + hashlib.sha256(raw).hexdigest()


def result(path):
    try:
        require(path.stat().st_size <= 16 * 1024 * 1024, "claim cap")
        raw_claim = path.read_bytes()

        def pairs(items):
            value = {}
            for key, item in items:
                require(key not in value, "duplicate claim key")
                value[key] = item
            return value

        claim = json.loads(raw_claim, object_pairs_hook=pairs)
        require(type(claim) is dict and list(claim) == [
            "source", "creationRequest", "parentLedgers", "r03Ledger",
            "ownLedger", "expectedControl", "expectedWorld", "expectedRandom",
            "expectedReceiptId", "retryInput", "declineInput",
            "completionInput"], "claim fields")
        require(ref.encode(claim) == raw_claim, "canonical claim")
        creation_raw = unb64(claim["creationRequest"])
        creation = json.loads(creation_raw, object_pairs_hook=pairs)
        require(ref.encode(creation) == creation_raw, "creation canonical")
        require(type(claim["parentLedgers"]) is list and
                len(claim["parentLedgers"]) == 10 and
                all(type(x) is list for x in claim["parentLedgers"]),
                "eleven separate parent inputs")
        parents = [creation, *[[unb64(x) for x in group]
                               for group in claim["parentLedgers"]]]
        r03 = [unb64(x) for x in claim["r03Ledger"]]
        own = [unb64(x) for x in claim["ownLedger"]]
        require(len(r03) == 1 and len(own) == 5, "R03/RBA-open ledgers")
        source = unb64(claim["source"])
        control = unb64(claim["expectedControl"])
        world = unb64(claim["expectedWorld"])
        random = unb64(claim["expectedRandom"])
        retry = unb64(claim["retryInput"])
        decline = unb64(claim["declineInput"])
        completion = unb64(claim["completionInput"])
        state = ref.replay(source, parents, r03, own)
        require(state["stepIndex"] == 2 and state["rbaOutcome"] is None and
                state["rbaDecision"] is not None and
                state["rbaOpenReceiptId"] == state["lastReceiptId"] ==
                claim["expectedReceiptId"] and
                ref.control_bytes(state) == control and
                ref.encode(state["parent"]["world"]) == world and
                ref.encode(state["parent"]["randomState"]) == random and
                state["admitted"] is False and state["publicActions"] == [],
                "authentic RBA-open cut")
        require(retry == own[4], "retry must repeat opening input")
        retry_source, retry_ledger, retry_event, retry_receipt = ref.apply(
            source, parents, r03, own, retry)
        require((retry_source, retry_ledger, retry_event, retry_receipt) ==
                (source, own, None, state["rbaOpenReceiptId"]),
                "RBA-open retry emitted event")
        value = ref.parse_input(decline)
        require(value["kind"] == "decline-rba" and
                value["actor"] == state["rbaDecision"]["owner"] and
                value["participant"] == state["rbaDecision"]["participant"],
                "typed defender test input")
        next_source, next_ledger, event, receipt = ref.apply(
            source, parents, r03, own, decline)
        after = ref.replay(next_source, parents, r03, next_ledger)
        require(event is not None and len(next_ledger) == 6 and
                next_ledger[:5] == own and next_ledger[5] == decline and
                after["rbaOutcome"] == "declined" and
                after["declineReceiptId"] == receipt and
                after["parent"] == state["parent"] and
                ref.encode(after["parent"]["world"]) == world and
                ref.encode(after["parent"]["randomState"]) == random and
                after["admitted"] is False and after["publicActions"] == [],
                "decline cut changed parent/resources/admission")
        completion_value = ref.parse_input(completion)
        require(completion_value["kind"] == "complete-step" and
                completion_value["actor"] == "system" and
                completion_value["admittedAtUnixMilliseconds"] is None and
                completion_value["participant"] is None,
                "canonical system completion input")
        final_source, final_ledger, final_event, final_receipt = ref.apply(
            next_source, parents, r03, next_ledger, completion)
        final = ref.replay(final_source, parents, r03, final_ledger)
        require(final_event is not None and len(final_ledger) == 7 and
                final_ledger[:6] == next_ledger and
                final_ledger[6] == completion and
                final["closed"] and final["stepIndex"] == 3 and
                final["position"]["positionId"].endswith(".force-assignment") and
                final["rbaOutcome"] == "declined" and
                final["declineReceiptId"] == receipt and
                final["lastReceiptId"] == final_receipt and
                final["parent"] == state["parent"] and
                ref.encode(final["parent"]["world"]) == world and
                ref.encode(final["parent"]["randomState"]) == random and
                final["admitted"] is False and final["publicActions"] == [],
                "fresh same-slot Force Assignment completion")
        return dict(control=b64(control), sourceHash=digest(source),
                    ledgerHash=digest(ref.encode([b64(x) for x in own])),
                    worldHash=digest(world), randomHash=digest(random),
                    receiptId=state["lastReceiptId"],
                    prefix=state["prefix"], version=state["stateVersion"],
                    retryReceiptId=retry_receipt, retryEvent=None,
                    nextSource=b64(next_source),
                    nextLedger=[b64(x) for x in next_ledger],
                    nextEvent=b64(event), nextReceiptId=receipt,
                    nextControl=b64(ref.control_bytes(after)),
                    nextPrefix=after["prefix"], nextVersion=after["stateVersion"],
                    finalSource=b64(final_source),
                    finalLedger=[b64(x) for x in final_ledger],
                    finalEvent=b64(final_event), finalReceiptId=final_receipt,
                    finalControl=b64(ref.control_bytes(final)),
                    finalPrefix=final["prefix"], finalVersion=final["stateVersion"],
                    finalPositionId=final["position"]["positionId"],
                    admitted=False, publicActions=[])
    except (ValueError, TypeError, KeyError, IndexError, OverflowError,
            binascii.Error, UnicodeError) as error:
        return dict(rejected="fresh-rba-decline", error=str(error))


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("one claim path required")
    print(json.dumps(result(Path(sys.argv[1])), separators=(",", ":")))
