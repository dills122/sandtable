#!/usr/bin/env python3
"""Private selected-assault/RBA continuation from authentic ordinal-two R03 roots.

The inputs are trusted authority records, never client commands. This reference
does not admit a Combat loop, issue public actions, or resolve an assault.
"""
import copy
import hashlib
import importlib.util
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SCHEMA = json.loads((ROOT / "combat-p01-selected-assault-v1.schema.json").read_text())


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


parent_ref = load("selected_assault_rba", "verify-combat-p01-explicit-rba-v1.py")
action = load("selected_assault_action", "verify-combat-p01-action-provenance-v1.py")
p01 = parent_ref.p01
steps = parent_ref.steps


class Invalid(ValueError):
    pass


def require(ok, code):
    if not ok:
        raise Invalid("CMB-P01-SELECT-" + code)


def encode(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("ascii")


def sha(raw):
    return "sha256:" + hashlib.sha256(raw).hexdigest()


def receipt(kind, unsigned):
    raw = SCHEMA["receiptDomains"][kind].encode("ascii") + b"\0" + encode(unsigned)
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
        require(len(value) <= SCHEMA["limits"]["ledgerEvents"], "001")
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
        raise Invalid("CMB-P01-SELECT-001") from error
    bounded(value)
    require(type(value) is dict and list(value) == fields and encode(value) == raw, "001")
    return value


def check_input(value):
    require(type(value) is dict and list(value) == SCHEMA["inputFields"], "002")
    require(type(value["contractVersion"]) is int and value["contractVersion"] == 1
            and value["kind"] in SCHEMA["inputKinds"]
            and value["actor"] in ("system", "axis", "commonwealth")
            and ident(value["cycleId"], "sha256:")
            and ident(value["expectedPriorPrefix"], "sha256:")
            and type(value["expectedPriorVersion"]) is int
            and 0 <= value["expectedPriorVersion"] < 2**63
            and type(value["fromPositionId"]) is str
            and re.fullmatch(r"land\.position\.[a-z0-9.-]{1,240}",
                             value["fromPositionId"]) is not None
            and (value["previousReceiptId"] is None or any(
                ident(value["previousReceiptId"], prefix)
                for prefix in ("mrbc.", "p01q.", "p01s.", "p01n.",
                               "p01o.", "p01r.", "p01c.")))
            and (value["decisionId"] is None or ident(value["decisionId"], "p01d."))
            and (value["targetLocationId"] is None or type(value["targetLocationId"]) is str
                 and re.fullmatch(r"[a-z0-9.-]{1,240}",
                                  value["targetLocationId"]) is not None)
            and (value["admittedAtUnixMilliseconds"] is None or
                 type(value["admittedAtUnixMilliseconds"]) is int and
                 0 <= value["admittedAtUnixMilliseconds"] < 2**63)
            and type(value["clockAvailable"]) is bool, "002")
    participant = value["participant"]
    require(participant is None or type(participant) is dict
            and list(participant) == SCHEMA["participantFields"]
            and ident(participant["creationBinding"], "creation.")
            and participant["originalSide"] in ("axis", "commonwealth")
            and type(participant["elementId"]) is str
            and re.fullmatch(r"[a-z0-9.-]{1,240}",
                             participant["elementId"]) is not None, "002")
    require(len(encode(value)) <= SCHEMA["limits"]["inputBytes"], "001")
    return value


def input_bytes(value):
    return encode(check_input(value))


def parse_input(raw):
    return check_input(parse(raw, SCHEMA["inputFields"], SCHEMA["limits"]["inputBytes"]))


def source_for(predecessor_source):
    require(type(predecessor_source) is bytes and predecessor_source.isascii(), "001")
    value = dict(contractVersion=1,
                 predecessorSourceCanonicalUtf8=predecessor_source.decode("ascii"),
                 eventCanonicalUtf8=[])
    raw = encode(value)
    require(len(raw) <= SCHEMA["limits"]["sourceBytes"], "001")
    return raw


def parse_source(raw):
    value = parse(raw, SCHEMA["sourceFields"], SCHEMA["limits"]["sourceBytes"])
    require(type(value["contractVersion"]) is int and value["contractVersion"] == 1
            and type(value["predecessorSourceCanonicalUtf8"]) is str
            and type(value["eventCanonicalUtf8"]) is list
            and all(type(item) is str and item.isascii()
                    for item in value["eventCanonicalUtf8"]), "001")
    return value


def candidate_from_replayed(source, state, request):
    """Derive one source-shaped 10-TOE target; caller has replayed all R03 inputs."""
    require(SCHEMA["physicalSourceHashes"] == p01.SCHEMA["physicalSourceHashes"],
            "003")
    require(state["closed"] and state["phase"] == "breakdown"
            and state["activeCycle"]["ordinal"] == 2
            and state["originalMovementEnd"]["ordinal"] == 1
            and state["movementEnd"]["ordinal"] == 2
            and state["sequencePosition"]["positionId"].endswith(
                ".combat.position-determination")
            and state["flow"] == {"kind": "idle"}
            and state["interruptContext"] is None, "003")
    content = p01.selected_content()
    fact = action.check_selected_structure(state, content, request)
    shape, pair = p01.physical_shape(state, content)
    weather = p01.current_weather_fact(source, state)
    require(shape == fact["physicalShape"] == "provisional-positive"
            and pair is not None and weather["kind"] == "normal"
            and state["targetUses"] == state["priorCycleTargetUses"] == []
            and state["attackHistory"] == []
            and state["world"]["futureObligations"] == []
            and len(content["elements"]) == 2
            and all(item["components"][0]["maximumToe"] == 10
                    for item in content["elements"]), "003")
    attacker, defender = pair
    require(attacker["components"][0]["currentToe"] == 10
            and defender["components"][0]["currentToe"] == 10
            and attacker["ammunition"]["points"] == defender["ammunition"]["points"] == 10
            and defender["currentLocationId"] == fact["targetLocationId"], "003")
    side = state["activeCycle"]["actingSide"]
    other = "commonwealth" if side == "axis" else "axis"
    binding = state["world"]["creationBinding"]
    attacker_ref = dict(creationBinding=binding, originalSide=side,
                        elementId=attacker["elementId"])
    defender_ref = dict(creationBinding=binding, originalSide=other,
                        elementId=defender["elementId"])
    locations = {item["locationId"]: item for item in content["locations"]}
    retreats = sorted(
        edge["secondLocationId"] if edge["firstLocationId"] ==
        defender["currentLocationId"] else edge["firstLocationId"]
        for edge in content["edges"]
        if defender["currentLocationId"] in
        (edge["firstLocationId"], edge["secondLocationId"])
        and attacker["currentLocationId"] not in
        (edge["firstLocationId"], edge["secondLocationId"])
        and edge["features"] == [])
    defender_cp = defender["operationalState"]["capabilityPointsExpended"]
    require(retreats and defender_cp["denominator"] == 1
            and defender_cp["numerator"] <= 9
            and all(locations[place]["terrainId"] == "land.terrain.clear"
                    for place in retreats), "003")
    candidate = dict(contractVersion=1, kind="ten-toe-adjacent-assault",
                     creationBinding=binding,
                     predecessorSourceHash=sha(p01.r03.raw(source, "Source")),
                     predecessorReceiptId=state["breakdownCompletionReceiptId"],
                     cycleId=state["cycleId"],
                     positionId=state["sequencePosition"]["positionId"],
                     stateVersion=state["stateVersion"], prefix=state["prefix"],
                     worldHash=sha(encode(state["world"])),
                     randomHash=sha(encode(state["randomState"])),
                     weatherReceiptId=weather["receiptId"],
                     weatherEventHash=weather["eventHash"],
                     attacker=attacker_ref, defender=defender_ref,
                     targetLocationId=defender["currentLocationId"],
                     attackerToe=10, defenderToe=10,
                     rbaClearRetreatLocations=retreats,
                     sourceRules=["Land 11.0", "Land 13.0-13.24",
                                  "CMB-POL-001", "CMB-POL-004"])
    return candidate, receipt("candidate", candidate)


def initial(predecessor_source, parent_ledgers, r03_ledger):
    require(type(parent_ledgers) is list and len(parent_ledgers) == 11
            and type(r03_ledger) is list and len(r03_ledger) == 1, "003")
    p01.check_ingress_bytes(predecessor_source, parent_ledgers, r03_ledger)
    source, _, parent = p01.r03.replay(predecessor_source,
                                       parent_ledgers, r03_ledger)
    candidate, candidate_id = candidate_from_replayed(
        source, parent, parent_ledgers[0])
    require(parent["activeCycle"]["admittedPolicyBundleDigest"] ==
            steps.env.Context().config_hash, "003")
    positions = parent_ref.positions(parent)
    state = dict(parent=parent, candidate=candidate, candidateId=candidate_id,
                 positions=positions, stateVersion=parent["stateVersion"],
                 prefix=parent["prefix"], position=copy.deepcopy(positions[0]),
                 stepIndex=-1, selectionDecision=None, selectionReceiptId=None,
                 selectionAcceptedAt=None, barrageReceiptId=None,
                 rbaDecision=None, rbaOutcome=None, rbaOpenReceiptId=None,
                 declineReceiptId=None, cancellationReceiptId=None,
                 lastReceiptId=parent["breakdownCompletionReceiptId"],
                 receipts=[], closed=False, admitted=False, publicActions=[])
    control_bytes(state)
    return state


def control_bytes(state):
    require(type(state) is dict and state["admitted"] is False
            and state["publicActions"] == [] and type(state["closed"]) is bool
            and len(state["receipts"]) <= SCHEMA["limits"]["ledgerEvents"]
            and -1 <= state["stepIndex"] <= 6
            and ident(state["candidateId"], "p01a.")
            and ident(state["prefix"], "sha256:"), "006")
    raw = encode(state)
    require(len(raw) <= SCHEMA["limits"]["controlBytes"], "001")
    return raw


def expected_input(state, kind, *, actor="system", now=None, available=True):
    decision = (state["selectionDecision"] if kind == "select-assault"
                else state["rbaDecision"] if kind in ("decline-rba", "cancel-rba")
                else None)
    participant = (state["candidate"]["attacker"] if kind == "select-assault"
                   else state["candidate"]["defender"] if kind == "decline-rba"
                   else None)
    return dict(contractVersion=1, kind=kind, actor=actor,
                cycleId=state["parent"]["cycleId"],
                expectedPriorVersion=state["stateVersion"],
                expectedPriorPrefix=state["prefix"],
                fromPositionId=state["position"]["positionId"],
                previousReceiptId=state["lastReceiptId"],
                decisionId=decision["decisionId"] if decision else None,
                participant=participant,
                targetLocationId=(state["candidate"]["targetLocationId"]
                                  if kind == "select-assault" else None),
                admittedAtUnixMilliseconds=now, clockAvailable=available)


def transition(prior, value):
    """One trusted admission fact; every branch keeps original World and RNG."""
    check_input(value)
    s = copy.deepcopy(prior)
    kind = value["kind"]
    require(not s["closed"] and len(s["receipts"]) < SCHEMA["limits"]["ledgerEvents"]
            and s["stateVersion"] < 2**63 - 1, "004")
    require(value["cycleId"] == s["parent"]["cycleId"]
            and value["expectedPriorVersion"] == s["stateVersion"]
            and value["expectedPriorPrefix"] == s["prefix"]
            and value["fromPositionId"] == s["position"]["positionId"]
            and value["previousReceiptId"] == s["lastReceiptId"], "004")
    now = value["admittedAtUnixMilliseconds"]
    participant = value["participant"]
    decision = s["rbaDecision"]
    target = value["targetLocationId"]
    if kind == "open-selection":
        require(s["stepIndex"] == -1 and not s["receipts"]
                and value["actor"] == "system" and participant is None
                and target is None and value["decisionId"] is None
                and type(now) is int and value["clockAvailable"], "004")
        timing = steps.inputs.make_timing(steps.env.Context().config,
                                          "selection", now)
        selection_id = receipt("decision", dict(kind="selection",
                                                candidateId=s["candidateId"],
                                                cycleId=s["parent"]["cycleId"]))
        s["selectionDecision"] = dict(decisionId=selection_id,
                                      owner=s["candidate"]["attacker"]["originalSide"],
                                      participant=s["candidate"]["attacker"],
                                      timing=timing)
        event_type, receipt_kind, proof = ("combat-selection-opened",
                                           "selectionOpen", "ten-toe-candidate")
        to_position = s["position"]
    elif kind == "select-assault":
        window = s["selectionDecision"]
        require(s["stepIndex"] == -1 and window is not None
                and s["selectionReceiptId"] is None
                and value["actor"] == window["owner"]
                and participant == window["participant"]
                and target == s["candidate"]["targetLocationId"]
                and value["decisionId"] == window["decisionId"]
                and value["clockAvailable"] and type(now) is int
                and steps.inputs.clock_gate(window["timing"], now, True)
                == "before-deadline", "004")
        s["selectionAcceptedAt"] = now
        s["stepIndex"] = 0
        event_type, receipt_kind, proof = ("combat-assault-selected",
                                           "selection", "trusted-attacker-selection")
        to_position = s["position"]
    elif kind == "complete-step":
        index = s["stepIndex"]
        require(0 <= index < 6 and value["actor"] == "system"
                and participant is None and target is None
                and value["decisionId"] is None and now is None, "004")
        if index == 2:
            require(s["rbaOutcome"] in ("declined", "cancelled"), "005")
        elif index > 2:
            require(s["rbaOutcome"] == "cancelled", "005")
        event_type = "combat-step-completed:" + SCHEMA["stepNames"][index]
        receipt_kind = "step"
        proof = ("no-gun-positions" if index == 0 and s["rbaOutcome"] is None
                 else "no-barrage-work" if index == 1 and s["rbaOutcome"] is None
                 else "accepted-decline" if index == 2 and s["rbaOutcome"] == "declined"
                 else "cancelled-no-attack")
        to_position = s["positions"][index + 1]
        s["stepIndex"] += 1
        s["position"] = copy.deepcopy(to_position)
        if index == 2 and s["rbaOutcome"] == "declined" or index == 5:
            s["closed"] = True
    elif kind == "open-rba":
        require(s["stepIndex"] == 2 and s["selectionReceiptId"] is not None
                and s["barrageReceiptId"] == s["lastReceiptId"]
                and decision is None and value["actor"] == "system"
                and participant is None and target is None
                and value["decisionId"] is None and type(now) is int
                and value["clockAvailable"]
                and now >= s["selectionAcceptedAt"], "004")
        timing = steps.inputs.make_timing(steps.env.Context().config, "rba", now)
        defender = s["candidate"]["defender"]
        decision_id = receipt("decision", dict(kind="rba",
                                               candidateId=s["candidateId"],
                                               selectionReceiptId=s["selectionReceiptId"],
                                               barrageReceiptId=s["barrageReceiptId"],
                                               defender=defender))
        s["rbaDecision"] = dict(decisionId=decision_id,
                                 owner=defender["originalSide"],
                                 participant=defender, timing=timing,
                                 barrageReceiptId=s["barrageReceiptId"])
        event_type, receipt_kind, proof = ("combat-retreat-decision-opened",
                                           "rbaOpen", "defender-owned-rba")
        to_position = s["position"]
    elif kind == "decline-rba":
        require(s["stepIndex"] == 2 and decision is not None
                and s["rbaOutcome"] is None
                and value["actor"] == decision["owner"]
                and participant == decision["participant"]
                and target is None and value["decisionId"] == decision["decisionId"]
                and value["clockAvailable"] and type(now) is int
                and steps.inputs.clock_gate(decision["timing"], now, True)
                == "before-deadline", "005")
        s["rbaOutcome"] = "declined"
        event_type, receipt_kind, proof = ("combat-retreat-before-assault-declined",
                                           "decline", "accepted-decline")
        to_position = s["position"]
    elif kind == "cancel-rba":
        require(s["stepIndex"] == 2 and s["rbaOutcome"] is None
                and s["selectionReceiptId"] is not None
                and value["actor"] == "system"
                and participant is None and target is None, "005")
        if decision is None:
            require(s["barrageReceiptId"] == s["lastReceiptId"]
                    and value["decisionId"] is None
                    and not value["clockAvailable"] and now is None, "005")
            proof = "clock-unavailable-before-rba-open"
        else:
            require(value["decisionId"] == decision["decisionId"]
                    and ((not value["clockAvailable"] and now is None) or
                         (value["clockAvailable"] and type(now) is int and
                          steps.inputs.clock_gate(decision["timing"], now, True)
                          in ("unavailable", "expired"))), "005")
            proof = "cancel-selected-intent-without-decline"
        s["rbaOutcome"] = "cancelled"
        event_type, receipt_kind = ("combat-selected-assault-cancelled", "cancel")
        to_position = s["position"]
    else:
        raise Invalid("CMB-P01-SELECT-002")
    before = prior["parent"]
    effect = dict(proofKind=proof,
                  fromPositionId=prior["position"]["positionId"],
                  toPositionId=to_position["positionId"],
                  selectionReceiptId=prior["selectionReceiptId"],
                  previousReceiptId=prior["lastReceiptId"],
                  decisionId=s["rbaDecision"]["decisionId"] if s["rbaDecision"]
                  else s["selectionDecision"]["decisionId"] if s["selectionDecision"]
                  else None,
                  deadlineUnixMilliseconds=(
                      s["rbaDecision"]["timing"]["deadlineUnixMilliseconds"]
                      if s["rbaDecision"] else
                      s["selectionDecision"]["timing"]["deadlineUnixMilliseconds"]
                      if s["selectionDecision"] else None),
                  acceptedAtUnixMilliseconds=now,
                  candidateId=s["candidateId"],
                  worldHash=s["candidate"]["worldHash"],
                  randomHash=s["candidate"]["randomHash"])
    cycle = before["activeCycle"]
    unsigned = dict(contractVersion=1, eventType=event_type,
                    campaignId=cycle["campaignId"], rulesetHash=cycle["rulesetHash"],
                    configurationHash=cycle["admittedPolicyBundleDigest"],
                    cycleId=before["cycleId"], priorVersion=prior["stateVersion"],
                    stateVersion=prior["stateVersion"] + 1,
                    priorPrefix=prior["prefix"], input=copy.deepcopy(value),
                    effect=effect)
    require(list(unsigned) == SCHEMA["eventFields"][:-1]
            and list(effect) == SCHEMA["effectFields"], "006")
    receipt_id = receipt(receipt_kind, unsigned)
    event = {**unsigned, "receiptId": receipt_id}
    raw = encode(event)
    require(len(raw) <= SCHEMA["limits"]["eventBytes"], "001")
    s["stateVersion"] = event["stateVersion"]
    s["prefix"] = p01.r03.SEQUENCE.prefix_event(prior["prefix"], raw)
    s["lastReceiptId"] = receipt_id
    s["receipts"].append(dict(kind=kind, actor=value["actor"],
                               inputHash=sha(input_bytes(value)),
                               receiptId=receipt_id, eventHash=sha(raw),
                               stateVersion=s["stateVersion"]))
    if kind == "select-assault":
        s["selectionReceiptId"] = receipt_id
    elif kind == "complete-step" and prior["stepIndex"] == 1:
        s["barrageReceiptId"] = receipt_id
    elif kind == "open-rba":
        s["rbaOpenReceiptId"] = receipt_id
    elif kind == "decline-rba":
        s["declineReceiptId"] = receipt_id
    elif kind == "cancel-rba":
        s["cancellationReceiptId"] = receipt_id
    require(s["parent"] == prior["parent"] and s["candidate"] == prior["candidate"]
            and s["admitted"] is False and s["publicActions"] == [], "006")
    control_bytes(s)
    return s, raw, receipt_id


def replay(source_bytes, parent_ledgers, r03_ledger, own_ledger):
    source = parse_source(source_bytes)
    predecessor = source["predecessorSourceCanonicalUtf8"].encode("ascii")
    state = initial(predecessor, parent_ledgers, r03_ledger)
    require(type(own_ledger) is list
            and len(own_ledger) <= SCHEMA["limits"]["ledgerEvents"]
            and all(type(item) is bytes for item in own_ledger)
            and len(own_ledger) == len(source["eventCanonicalUtf8"]), "001")
    for raw_input, recorded in zip(own_ledger, source["eventCanonicalUtf8"]):
        value = parse_input(raw_input)
        state, expected, _ = transition(state, value)
        parse(recorded.encode("ascii"), SCHEMA["eventFields"],
              SCHEMA["limits"]["eventBytes"])
        require(expected == recorded.encode("ascii"), "006")
    return state


def apply(source_bytes, parent_ledgers, r03_ledger, own_ledger, raw_input):
    state = replay(source_bytes, parent_ledgers, r03_ledger, own_ledger)
    value = parse_input(raw_input)
    input_hash = sha(raw_input)
    old = next((item for item in state["receipts"]
                if item["inputHash"] == input_hash), None)
    if old is not None:
        require(old["actor"] == value["actor"], "004")
        return source_bytes, own_ledger, None, old["receiptId"]
    after, event, receipt_id = transition(state, value)
    source = parse_source(source_bytes)
    source["eventCanonicalUtf8"].append(event.decode("ascii"))
    updated = encode(source)
    require(len(updated) <= SCHEMA["limits"]["sourceBytes"], "001")
    ledger = [*own_ledger, raw_input]
    require(replay(updated, parent_ledgers, r03_ledger, ledger) == after, "006")
    return updated, ledger, event, receipt_id


def audit(evidence_path):
    """Authenticate all original roots and prove only the 44 candidate shapes."""
    import gzip
    result = load("selected_assault_result", "verify-combat-p01-result-gates-v1.py")
    evidence = Path(evidence_path)
    for name, expected in result.PINS.items():
        require(hashlib.sha256((evidence / name).read_bytes()).hexdigest() ==
                expected, "007")
    for name, expected in (
        ("CNA_LandGameRules.pdf", SCHEMA["physicalSourceHashes"]["landRulesPdf"]),
        ("ERR_CampaignforNorthAfrica.pdf",
         SCHEMA["physicalSourceHashes"]["septemberErrataPdf"])
    ):
        require(hashlib.sha256((evidence / name).read_bytes()).hexdigest() ==
                expected, "007")
    manifest = json.loads((evidence / "manifest.json").read_text())
    summary = json.loads((evidence / "summary.json").read_text())
    partition = json.loads((evidence / "bounded-profile-audit.json").read_text())
    require(len(manifest["chunks"]) == 11 and len(summary["rows"]) ==
            len(partition["rows"]) == summary["count"] == 300, "007")
    positive = empty = index = 0
    seen = set()
    for chunk in manifest["chunks"]:
        data = (evidence / chunk["name"]).read_bytes()
        require(hashlib.sha256(data).hexdigest() == chunk["sha256"] ==
                summary["chunkSha256"][chunk["name"]], "007")
        for line in gzip.decompress(data).splitlines():
            row = json.loads(line)
            record = row["r03"]
            key = record["rowKey"]
            require(row["index"] == index and key not in seen
                    and key == summary["rows"][index]["rowKey"] ==
                    partition["rows"][index]["rowKey"], "007")
            seen.add(key)
            state = result.require_control_vector(
                record, result.authenticated_control(record))
            source = p01.r03.parse(
                record["vectors"]["sourceCanonicalUtf8"].encode("ascii"), "Source")
            request = record["vectors"]["parentInputLedgersCanonicalUtf8"][0]
            shape, _ = p01.physical_shape(state, p01.selected_content())
            if shape == "provisional-positive":
                fact, candidate_id = candidate_from_replayed(source, state, request)
                require(ident(candidate_id, "p01a.")
                        and fact["attackerToe"] == fact["defenderToe"] == 10
                        and partition["rows"][index]["partition"] ==
                        "provisional-positive", "007")
                positive += 1
            else:
                require(shape == partition["rows"][index]["partition"] ==
                        "provisional-empty", "007")
                empty += 1
            index += 1
    require(index == len(seen) == 300 and positive == 44 and empty == 256, "007")
    return dict(authenticatedRows=300, selectedCandidates=44,
                candidateAbsent=256, admittedRows=0, publicActionCount=0)


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("evidence", type=Path)
    print(audit(parser.parse_args().evidence))
