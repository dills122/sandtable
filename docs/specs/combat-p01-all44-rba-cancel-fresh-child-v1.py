#!/usr/bin/env python3
"""Cold-read one private selected-assault RBA-open cut and System cancellation."""
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
            "expectedReceiptId", "retryInput", "continuationInputs"],
                "claim fields")
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
        require(type(claim["continuationInputs"]) is list and
                len(claim["continuationInputs"]) == 5,
                "cancellation and four canonical completions")
        continuations = [unb64(x) for x in claim["continuationInputs"]]
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
        opening_retry_receipt = retry_receipt
        value = ref.parse_input(continuations[0])
        require(value["kind"] == "cancel-rba" and value["actor"] == "system" and
                value["participant"] is None and value["clockAvailable"] and
                ref.steps.inputs.clock_gate(state["rbaDecision"]["timing"],
                                             value["admittedAtUnixMilliseconds"],
                                             True) in ("unavailable", "expired"),
                "trusted TEST System cancellation after deadline")
        transitions = []
        next_source, next_ledger = source, own
        for ordinal, next_input in enumerate(continuations):
            prior = ref.replay(next_source, parents, r03, next_ledger)
            input_value = ref.parse_input(next_input)
            require(input_value["kind"] == ("cancel-rba" if ordinal == 0
                                             else "complete-step") and
                    input_value["actor"] == "system" and
                    input_value["participant"] is None and
                    input_value["previousReceiptId"] == prior["lastReceiptId"],
                    "canonical ordered System input")
            next_source, next_ledger, event, receipt = ref.apply(
                next_source, parents, r03, next_ledger, next_input)
            after = ref.replay(next_source, parents, r03, next_ledger)
            require(event is not None and len(next_ledger) == 6 + ordinal and
                    next_ledger[-1] == next_input and
                    after["rbaOutcome"] == "cancelled" and
                    after["declineReceiptId"] is None and
                    after["lastReceiptId"] == receipt and
                    after["parent"] == state["parent"] and
                    ref.encode(after["parent"]["world"]) == world and
                    ref.encode(after["parent"]["randomState"]) == random and
                    after["admitted"] is False and after["publicActions"] == [],
                    "fresh cut changed parent/resources/admission")
            retry_source, retry_ledger, retry_event, retry_receipt = ref.apply(
                next_source, parents, r03, next_ledger, next_input)
            require((retry_source, retry_ledger, retry_event, retry_receipt) ==
                    (next_source, next_ledger, None, receipt),
                    "accepted-input retry issued another event")
            transitions.append(dict(source=b64(next_source),
                                    ledger=[b64(x) for x in next_ledger],
                                    event=b64(event), receiptId=receipt,
                                    control=b64(ref.control_bytes(after)),
                                    prefix=after["prefix"],
                                    version=after["stateVersion"],
                                    positionId=after["position"]["positionId"],
                                    outcome=after["rbaOutcome"],
                                    closed=after["closed"]))
        final = after
        require(final["closed"] and final["stepIndex"] == 6 and
                final["position"]["positionId"].endswith(".reserve-release") and
                final["cancellationReceiptId"] == transitions[0]["receiptId"] and
                final["lastReceiptId"] == transitions[-1]["receiptId"],
                "fresh same-slot Reserve Release completion")
        return dict(control=b64(control), sourceHash=digest(source),
                    ledgerHash=digest(ref.encode([b64(x) for x in own])),
                    worldHash=digest(world), randomHash=digest(random),
                    receiptId=state["lastReceiptId"],
                    prefix=state["prefix"], version=state["stateVersion"],
                    retryReceiptId=opening_retry_receipt, retryEvent=None,
                    transitions=transitions,
                    admitted=False, publicActions=[])
    except (ValueError, TypeError, KeyError, IndexError, OverflowError,
            binascii.Error, UnicodeError) as error:
        return dict(rejected="fresh-rba-cancel", error=str(error))


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("one claim path required")
    print(json.dumps(result(Path(sys.argv[1])), separators=(",", ":")))
