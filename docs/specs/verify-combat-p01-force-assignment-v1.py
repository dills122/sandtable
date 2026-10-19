#!/usr/bin/env python3
"""Private P01 Force Assignment from an authenticated selected-assault RBA decline.

Input actor and time are trusted authority facts, not client-supplied credentials.
Every replay authenticates the original CreationRequest, eleven parent ledgers,
the R03 ledger, and the separate selected-assault ledger before this family.
"""
import copy
import hashlib
import importlib.util
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SCHEMA = json.loads((ROOT / "combat-p01-force-assignment-v1.schema.json").read_text())


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


selected = load("force_assignment_selected", "verify-combat-p01-selected-assault-v1.py")
p01 = selected.p01


class Invalid(ValueError):
    pass


def require(ok, code):
    if not ok:
        raise Invalid("CMB-P01-FA-" + code)


def encode(value):
    return selected.encode(value)


def sha(raw):
    return selected.sha(raw)


def digest(kind, value):
    raw = SCHEMA["receiptDomains"][kind].encode("ascii") + b"\0" + encode(value)
    return SCHEMA["receiptPrefixes"][kind] + hashlib.sha256(raw).hexdigest()


def ident(value, prefix):
    return type(value) is str and re.fullmatch(
        re.escape(prefix) + r"[0-9a-f]{64}", value) is not None


def bounded(value, depth=0):
    require(depth <= SCHEMA["limits"]["depth"], "001")
    if type(value) is dict:
        require(len(value) <= 24, "001")
        for item in value.values():
            bounded(item, depth + 1)
    elif type(value) is list:
        require(len(value) <= 16, "001")
        for item in value:
            bounded(item, depth + 1)


def parse(raw, fields, limit):
    require(type(raw) is bytes and 0 < len(raw) <= limit and raw.isascii(), "001")

    def pairs(items):
        value = {}
        for key, item in items:
            require(key not in value, "001")
            value[key] = item
        return value

    try:
        value = json.loads(raw.decode("ascii"), object_pairs_hook=pairs,
                           parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error:
        raise Invalid("CMB-P01-FA-001") from error
    bounded(value)
    require(type(value) is dict and list(value) == fields and encode(value) == raw, "001")
    return value


def check_allocation(value):
    require(type(value) is dict and list(value) == SCHEMA["allocationFields"]
            and value["kind"] == "full-close-assault"
            and type(value["unit"]) is dict
            and list(value["unit"]) == selected.SCHEMA["participantFields"]
            and ident(value["unit"]["creationBinding"], "creation.")
            and value["unit"]["originalSide"] in ("axis", "commonwealth")
            and type(value["unit"]["elementId"]) is str
            and re.fullmatch(r"[a-z0-9.-]{1,240}", value["unit"]["elementId"])
            and type(value["componentId"]) is str
            and re.fullmatch(r"[a-z0-9.-]{1,240}", value["componentId"])
            and type(value["committedToe"]) is int
            and value["committedToe"] == 10, "002")


def check_input(value):
    require(type(value) is dict and list(value) == SCHEMA["inputFields"], "002")
    require(type(value["contractVersion"]) is int and value["contractVersion"] == 1
            and value["kind"] in SCHEMA["inputKinds"]
            and value["actor"] in ("system", "axis", "commonwealth")
            and ident(value["cycleId"], "sha256:")
            and type(value["expectedPriorVersion"]) is int
            and 0 <= value["expectedPriorVersion"] < 2**63
            and ident(value["expectedPriorPrefix"], "sha256:")
            and type(value["fromPositionId"]) is str
            and re.fullmatch(r"land\.position\.[a-z0-9.-]{1,240}",
                             value["fromPositionId"])
            and any(ident(value["previousReceiptId"], prefix)
                    for prefix in ("p01n.", "p01u.", "p01v.", "p01x.", "p01y."))
            and (value["roundId"] is None or ident(value["roundId"], "p01f."))
            and (value["slotId"] is None or ident(value["slotId"], "p01t."))
            and (value["admittedAtUnixMilliseconds"] is None or
                 type(value["admittedAtUnixMilliseconds"]) is int and
                 0 <= value["admittedAtUnixMilliseconds"] <=
                 SCHEMA["limits"]["maximumUtcMilliseconds"])
            and type(value["clockAvailable"]) is bool, "002")
    if value["allocation"] is not None:
        check_allocation(value["allocation"])
    require(len(encode(value)) <= SCHEMA["limits"]["inputBytes"], "001")
    return value


def input_bytes(value):
    return encode(check_input(value))


def parse_input(raw):
    return check_input(parse(raw, SCHEMA["inputFields"], SCHEMA["limits"]["inputBytes"]))


def source_for(selected_source):
    require(type(selected_source) is bytes and selected_source.isascii(), "001")
    raw = encode(dict(contractVersion=1,
                      selectedSourceCanonicalUtf8=selected_source.decode("ascii"),
                      eventCanonicalUtf8=[]))
    require(len(raw) <= SCHEMA["limits"]["sourceBytes"], "001")
    return raw


def parse_source(raw):
    value = parse(raw, SCHEMA["sourceFields"], SCHEMA["limits"]["sourceBytes"])
    require(type(value["contractVersion"]) is int and value["contractVersion"] == 1
            and type(value["selectedSourceCanonicalUtf8"]) is str
            and type(value["eventCanonicalUtf8"]) is list
            and len(value["eventCanonicalUtf8"]) <= SCHEMA["limits"]["ledgerEvents"]
            and all(type(item) is str and item.isascii()
                    for item in value["eventCanonicalUtf8"]), "001")
    return value


def exact_slots(parent):
    """Derive both full allocations from selected Content and unchanged World."""
    content = p01.selected_content()  # Rechecks the pinned physical bytes at use.
    require(parent["candidate"]["worldHash"] == sha(encode(parent["parent"]["world"]))
            and parent["candidate"]["randomHash"] ==
            sha(encode(parent["parent"]["randomState"])), "003")
    slots = []
    for role in ("attacker", "defender"):
        unit = parent["candidate"][role]
        element = next((x for x in content["elements"]
                        if x["elementId"] == unit["elementId"]), None)
        current = next((x for x in parent["parent"]["world"]["elements"]
                        if x["elementId"] == unit["elementId"]), None)
        require(element is not None and current is not None
                and element["sideId"] == unit["originalSide"]
                and len(element["components"]) == len(current["components"]) == 1,
                "003")
        component, live = element["components"][0], current["components"][0]
        require(component["componentClassId"] == "land.combat-component.infantry"
                and component["maximumToe"] == live["currentToe"] == 10
                and live["componentId"] == component["componentId"]
                and current["currentLocationId"] ==
                (parent["candidate"]["targetLocationId"] if role == "defender"
                 else current["currentLocationId"]), "003")
        allocation = dict(kind="full-close-assault", unit=copy.deepcopy(unit),
                          componentId=component["componentId"], committedToe=10)
        slots.append(dict(role=role, owner=unit["originalSide"],
                          slotId=None, allocation=allocation,
                          sealedReceiptId=None, sealedAtUnixMilliseconds=None))
    require(slots[0]["owner"] != slots[1]["owner"]
            and parent["candidate"]["attackerToe"] ==
            parent["candidate"]["defenderToe"] == 10, "003")
    return slots


def initial(selected_source, parent_ledgers, r03_ledger, selected_ledger):
    parent = selected.replay(selected_source, parent_ledgers, r03_ledger,
                             selected_ledger)
    require(parent["closed"] and parent["rbaOutcome"] == "declined"
            and parent["stepIndex"] == 3
            and parent["position"]["positionId"].endswith(".force-assignment")
            and parent["declineReceiptId"] is not None
            and parent["cancellationReceiptId"] is None
            and parent["receipts"][-1]["receiptId"] == parent["lastReceiptId"]
            and len(parent["receipts"]) == 7
            and parent["admitted"] is False
            and parent["publicActions"] == [], "003")
    selected_packet = selected.parse_source(selected_source)
    declines = [json.loads(raw) for raw in selected_packet["eventCanonicalUtf8"]
                if json.loads(raw)["eventType"] ==
                "combat-retreat-before-assault-declined"]
    require(len(declines) == 1 and type(declines[0]["effect"]
                                       ["acceptedAtUnixMilliseconds"]) is int,
            "003")
    state = dict(parent=parent, selectedSourceHash=sha(selected_source),
                 declineAcceptedAtUnixMilliseconds=declines[0]["effect"]
                 ["acceptedAtUnixMilliseconds"],
                 slots=exact_slots(parent),
                 stateVersion=parent["stateVersion"], prefix=parent["prefix"],
                 position=copy.deepcopy(parent["position"]), stepIndex=3,
                 status="unopened", roundId=None, timing=None,
                 openingReceiptId=None, cancellationReceiptId=None,
                 stepReceipts=[], lastReceiptId=parent["lastReceiptId"],
                 receipts=[], closed=False, admitted=False, publicActions=[])
    control_bytes(state)
    return state


def control_bytes(state):
    require(type(state) is dict and state["admitted"] is False
            and state["publicActions"] == [] and len(state["slots"]) == 2
            and len(state["receipts"]) <= SCHEMA["limits"]["ledgerEvents"]
            and state["status"] in ("unopened", "collecting", "prepared", "cancelled")
            and 3 <= state["stepIndex"] <= 6 and type(state["closed"]) is bool
            and ident(state["prefix"], "sha256:"), "006")
    raw = encode(state)
    require(len(raw) <= SCHEMA["limits"]["controlBytes"], "001")
    return raw


def expected_input(state, kind, *, actor="system", role=None, now=None,
                   available=True):
    slot = next((x for x in state["slots"] if x["role"] == role), None)
    return dict(contractVersion=1, kind=kind, actor=actor,
                cycleId=state["parent"]["parent"]["cycleId"],
                expectedPriorVersion=state["stateVersion"],
                expectedPriorPrefix=state["prefix"],
                fromPositionId=state["position"]["positionId"],
                previousReceiptId=state["lastReceiptId"],
                roundId=state["roundId"],
                slotId=slot["slotId"] if kind == "seal-assignment" and slot else None,
                allocation=copy.deepcopy(slot["allocation"])
                if kind == "seal-assignment" and slot else None,
                admittedAtUnixMilliseconds=now, clockAvailable=available)


def empty_aa(state):
    """Certificate from exact source bytes, current World, two seals, and FA receipt."""
    slots = state["slots"]
    require(all(x["sealedReceiptId"] is not None for x in slots)
            and state["stepReceipts"] and state["stepIndex"] == 4, "005")
    for expected, actual in zip(exact_slots(state["parent"]), slots):
        require(actual["role"] == expected["role"]
                and actual["owner"] == expected["owner"]
                and actual["allocation"] == expected["allocation"], "005")
    certificate = dict(contractVersion=1,
                       contentHash="sha256:" + p01.SCHEMA["contentSha256"],
                       worldHash=state["parent"]["candidate"]["worldHash"],
                       candidateId=state["parent"]["candidateId"],
                       allocations=[copy.deepcopy(x["allocation"]) for x in slots],
                       assignmentReceiptId=state["stepReceipts"][-1],
                       componentIds=[x["allocation"]["componentId"] for x in slots])
    return digest("emptyAa", certificate)


def transition(prior, value):
    check_input(value)
    s = copy.deepcopy(prior)
    require(len(s["slots"]) == 2 and
            all(actual["allocation"] == expected["allocation"]
                for actual, expected in zip(s["slots"], exact_slots(s["parent"]))),
            "003")
    kind, now = value["kind"], value["admittedAtUnixMilliseconds"]
    require(not s["closed"] and len(s["receipts"]) < SCHEMA["limits"]["ledgerEvents"]
            and s["stateVersion"] < 2**63 - 1, "004")
    require(value["cycleId"] == s["parent"]["parent"]["cycleId"]
            and value["expectedPriorVersion"] == s["stateVersion"]
            and value["expectedPriorPrefix"] == s["prefix"]
            and value["fromPositionId"] == s["position"]["positionId"]
            and value["previousReceiptId"] == s["lastReceiptId"]
            and value["roundId"] == s["roundId"], "004")
    slot = next((x for x in s["slots"] if x["slotId"] == value["slotId"]), None)
    to_position = s["position"]
    proof_receipts = []
    certificate_hash = None
    if kind == "open-assignment":
        require(s["status"] == "unopened" and not s["receipts"]
                and value["actor"] == "system" and value["slotId"] is None
                and value["allocation"] is None and type(now) is int
                and now >= s["declineAcceptedAtUnixMilliseconds"]
                and value["clockAvailable"] and now +
                SCHEMA["limits"]["budgetMilliseconds"] <=
                SCHEMA["limits"]["maximumUtcMilliseconds"], "004")
        timing = dict(openedAtUnixMilliseconds=now,
                      deadlineUnixMilliseconds=now + SCHEMA["limits"]["budgetMilliseconds"])
        s["timing"] = timing
        s["roundId"] = digest("round", dict(
            selectedSourceHash=s["selectedSourceHash"],
            candidateId=s["parent"]["candidateId"],
            selectionReceiptId=s["parent"]["selectionReceiptId"],
            declineReceiptId=s["parent"]["declineReceiptId"],
            openingAuthorityVersion=s["stateVersion"],
            openingHistoryPrefix=s["prefix"], timing=timing))
        for item in s["slots"]:
            item["slotId"] = digest("slot", dict(roundId=s["roundId"],
                                                  role=item["role"]))
        s["status"] = "collecting"
        event_type, receipt_kind, proof = ("combat-force-assignment-opened",
                                           "opening", "full-ten-toe-slots")
    elif kind == "seal-assignment":
        require(s["status"] == "collecting" and s["stepIndex"] == 3
                and slot is not None and slot["owner"] == value["actor"]
                and slot["sealedReceiptId"] is None
                and value["allocation"] == slot["allocation"], "004")
        require(value["clockAvailable"] and type(now) is int
                and s["timing"]["openedAtUnixMilliseconds"] <= now <
                s["timing"]["deadlineUnixMilliseconds"], "005")
        event_type, receipt_kind, proof = ("combat-force-assignment-sealed",
                                           "seal", "owner-full-ten-toe")
    elif kind == "cancel-assignment":
        require(s["status"] == "collecting" and s["stepIndex"] == 3
                and value["actor"] == "system" and value["slotId"] is None
                and value["allocation"] is None, "004")
        expired = (value["clockAvailable"] and type(now) is int
                   and now >= s["timing"]["deadlineUnixMilliseconds"])
        unavailable = not value["clockAvailable"] and now is None
        require(expired or unavailable, "005")
        proof = "deadline" if expired else "clock-or-controller-unavailable"
        s["status"] = "cancelled"
        event_type, receipt_kind = ("combat-force-assignment-cancelled", "cancel")
        proof_receipts = [x["sealedReceiptId"] for x in s["slots"]
                         if x["sealedReceiptId"] is not None]
    elif kind == "complete-step":
        require(value["actor"] == "system" and value["slotId"] is None
                and value["allocation"] is None and now is None
                and value["clockAvailable"] and 3 <= s["stepIndex"] <= 5
                and s["status"] in ("prepared", "cancelled"), "004")
        index = s["stepIndex"]
        if s["status"] == "prepared":
            require(index < 5 and all(x["sealedReceiptId"] is not None
                                      for x in s["slots"]), "005")
            proof_receipts = [x["sealedReceiptId"] for x in s["slots"]]
            proof = "prepared-full-assignment" if index == 3 else "certified-empty-aa"
            if index == 4:
                certificate_hash = empty_aa(s)
        else:
            proof_receipts = [s["cancellationReceiptId"]]
            require(proof_receipts[0] is not None, "005")
            proof = "cancelled-no-attack"
        to_position = (s["parent"]["positions"][index + 1] if index < 5
                       else s["parent"]["positions"][6])
        s["stepIndex"] += 1
        s["position"] = copy.deepcopy(to_position)
        if index == 5:
            s["closed"] = True
        event_type, receipt_kind = ("combat-step-completed:" +
                                    selected.SCHEMA["stepNames"][index], "step")
    else:
        raise Invalid("CMB-P01-FA-002")
    parent = s["parent"]["parent"]
    cycle = parent["activeCycle"]
    effect = dict(proofKind=proof,
                  fromPositionId=prior["position"]["positionId"],
                  toPositionId=to_position["positionId"],
                  previousReceiptId=prior["lastReceiptId"],
                  candidateId=s["parent"]["candidateId"],
                  selectionReceiptId=s["parent"]["selectionReceiptId"],
                  declineReceiptId=s["parent"]["declineReceiptId"],
                  roundId=s["roundId"], slotId=value["slotId"],
                  openingReceiptId=s["openingReceiptId"],
                  proofReceipts=proof_receipts, certificateHash=certificate_hash,
                  worldHash=s["parent"]["candidate"]["worldHash"],
                  randomHash=s["parent"]["candidate"]["randomHash"])
    unsigned = dict(contractVersion=1, eventType=event_type,
                    campaignId=cycle["campaignId"], rulesetHash=cycle["rulesetHash"],
                    configurationHash=cycle["admittedPolicyBundleDigest"],
                    cycleId=parent["cycleId"], priorVersion=prior["stateVersion"],
                    stateVersion=prior["stateVersion"] + 1,
                    priorPrefix=prior["prefix"], input=copy.deepcopy(value),
                    effect=effect)
    require(list(unsigned) == SCHEMA["eventFields"][:-1]
            and list(effect) == SCHEMA["effectFields"], "006")
    receipt_id = digest(receipt_kind, unsigned)
    raw = encode({**unsigned, "receiptId": receipt_id})
    require(len(raw) <= SCHEMA["limits"]["eventBytes"], "001")
    if kind == "open-assignment":
        s["openingReceiptId"] = receipt_id
    elif kind == "seal-assignment":
        slot = next(x for x in s["slots"] if x["slotId"] == value["slotId"])
        slot["sealedReceiptId"] = receipt_id
        slot["sealedAtUnixMilliseconds"] = now
        if all(x["sealedReceiptId"] is not None for x in s["slots"]):
            s["status"] = "prepared"
    elif kind == "cancel-assignment":
        s["cancellationReceiptId"] = receipt_id
    else:
        s["stepReceipts"].append(receipt_id)
    s["stateVersion"] = unsigned["stateVersion"]
    s["prefix"] = p01.r03.SEQUENCE.prefix_event(prior["prefix"], raw)
    s["lastReceiptId"] = receipt_id
    s["receipts"].append(dict(kind=kind, actor=value["actor"],
                               inputHash=sha(input_bytes(value)),
                               receiptId=receipt_id, eventHash=sha(raw),
                               stateVersion=s["stateVersion"]))
    require(s["parent"] == prior["parent"]
            and s["selectedSourceHash"] == prior["selectedSourceHash"]
            and s["declineAcceptedAtUnixMilliseconds"] ==
            prior["declineAcceptedAtUnixMilliseconds"]
            and s["admitted"] is False and s["publicActions"] == []
            and s["parent"]["parent"]["world"] == prior["parent"]["parent"]["world"]
            and s["parent"]["parent"]["randomState"] ==
            prior["parent"]["parent"]["randomState"], "006")
    control_bytes(s)
    return s, raw, receipt_id


def replay(source_bytes, parent_ledgers, r03_ledger, selected_ledger, own_ledger):
    source = parse_source(source_bytes)
    predecessor = source["selectedSourceCanonicalUtf8"].encode("ascii")
    state = initial(predecessor, parent_ledgers, r03_ledger, selected_ledger)
    require(type(own_ledger) is list
            and len(own_ledger) == len(source["eventCanonicalUtf8"])
            and all(type(item) is bytes for item in own_ledger), "001")
    for raw_input, recorded in zip(own_ledger, source["eventCanonicalUtf8"]):
        value = parse_input(raw_input)
        state, expected, _ = transition(state, value)
        parse(recorded.encode("ascii"), SCHEMA["eventFields"],
              SCHEMA["limits"]["eventBytes"])
        require(expected == recorded.encode("ascii"), "006")
    return state


def apply(source_bytes, parent_ledgers, r03_ledger, selected_ledger,
          own_ledger, raw_input):
    state = replay(source_bytes, parent_ledgers, r03_ledger,
                   selected_ledger, own_ledger)
    value = parse_input(raw_input)
    old = next((x for x in state["receipts"]
                if x["inputHash"] == sha(raw_input)), None)
    if old is not None:
        require(old["actor"] == value["actor"], "004")
        return source_bytes, own_ledger, None, old["receiptId"]
    after, event, receipt_id = transition(state, value)
    source = parse_source(source_bytes)
    source["eventCanonicalUtf8"].append(event.decode("ascii"))
    updated = encode(source)
    require(len(updated) <= SCHEMA["limits"]["sourceBytes"], "001")
    ledger = [*own_ledger, raw_input]
    require(replay(updated, parent_ledgers, r03_ledger,
                   selected_ledger, ledger) == after, "006")
    return updated, ledger, event, receipt_id


def audit(evidence_path):
    result = selected.audit(evidence_path)
    require(result == dict(authenticatedRows=300, selectedCandidates=44,
                           candidateAbsent=256, admittedRows=0,
                           publicActionCount=0), "007")
    return result


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("evidence", type=Path)
    print(audit(parser.parse_args().evidence))
