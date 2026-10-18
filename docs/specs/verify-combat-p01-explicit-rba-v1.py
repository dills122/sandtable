#!/usr/bin/env python3
"""Private, creation-rooted P01 nonadjacent no-assault/RBA continuation.

Inputs are trusted authority records, not public commands. No native or public
consumer may treat this reference as a P01 admission certificate.
"""
import copy
import hashlib
import importlib.util
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SCHEMA = json.loads((ROOT / "combat-p01-explicit-rba-v1.schema.json").read_text())


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


p01 = load("p01_explicit_parent", "verify-combat-actual-moved-repeat-position-v1.py")
steps = load("p01_explicit_c3a", "verify-combat-selection-steps-v1.py")


class Invalid(ValueError):
    pass


def require(ok, code):
    if not ok:
        raise Invalid("CMB-P01-RBA-" + code)


def encode(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("ascii")


def sha(value):
    return "sha256:" + hashlib.sha256(value).hexdigest()


def receipt(kind, unsigned):
    raw = SCHEMA["receiptDomains"][kind].encode("ascii") + b"\0" + encode(unsigned)
    return SCHEMA["receiptPrefixes"][kind] + hashlib.sha256(raw).hexdigest()


def bounded(value, depth=0):
    require(depth <= SCHEMA["limits"]["depth"], "001")
    if type(value) is dict:
        require(len(value) <= 24, "001")
        for child in value.values():
            bounded(child, depth + 1)
    elif type(value) is list:
        require(len(value) <= SCHEMA["limits"]["ledgerEvents"], "001")
        for child in value:
            bounded(child, depth + 1)


def parse(data, fields, limit):
    require(type(data) is bytes and 0 < len(data) <= limit and data.isascii(), "001")

    def pairs(items):
        result = {}
        for key, child in items:
            require(key not in result, "001")
            result[key] = child
        return result

    try:
        value = json.loads(data.decode("ascii"), object_pairs_hook=pairs,
                           parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error:
        raise Invalid("CMB-P01-RBA-001") from error
    bounded(value)
    require(type(value) is dict and list(value) == fields and encode(value) == data, "001")
    return value


def identifier(value, prefix):
    return type(value) is str and re.fullmatch(re.escape(prefix) + r"[0-9a-f]{64}", value) is not None


def hash_value(value):
    return identifier(value, "sha256:")


def check_input(value):
    require(type(value) is dict and list(value) == SCHEMA["inputFields"], "002")
    require(value["contractVersion"] == 1 and type(value["contractVersion"]) is int
            and value["kind"] in SCHEMA["inputKinds"] and type(value["kind"]) is str
            and value["actor"] in ("system", "axis", "commonwealth")
            and hash_value(value["cycleId"]) and hash_value(value["expectedPriorPrefix"])
            and type(value["expectedPriorVersion"]) is int
            and 0 <= value["expectedPriorVersion"] < 2**63
            and type(value["fromPositionId"]) is str
            and re.fullmatch(r"land\.position\.[a-z0-9.-]{1,240}", value["fromPositionId"])
            and (value["previousReceiptId"] is None or any(
                identifier(value["previousReceiptId"], prefix)
                for prefix in ("mrbc.", "p01s.", "p01n.", "p01o.", "p01r.", "p01c.")))
            and (value["decisionId"] is None or identifier(value["decisionId"], "p01d."))
            and (value["admittedAtUnixMilliseconds"] is None or
                 type(value["admittedAtUnixMilliseconds"]) is int and
                 0 <= value["admittedAtUnixMilliseconds"] < 2**63)
            and type(value["clockAvailable"]) is bool, "002")
    participant = value["participant"]
    require(participant is None or
            type(participant) is dict and list(participant) == SCHEMA["participantFields"]
            and identifier(participant["creationBinding"], "creation.")
            and participant["originalSide"] in ("axis", "commonwealth")
            and type(participant["elementId"]) is str
            and re.fullmatch(r"[a-z0-9.-]{1,240}", participant["elementId"]), "002")
    require(len(encode(value)) <= SCHEMA["limits"]["inputBytes"], "001")
    return value


def input_bytes(value):
    return encode(check_input(value))


def parse_input(data):
    return check_input(parse(data, SCHEMA["inputFields"], SCHEMA["limits"]["inputBytes"]))


def source_for(predecessor_source):
    require(type(predecessor_source) is bytes and predecessor_source.isascii(), "001")
    value = dict(contractVersion=1,
                 predecessorSourceCanonicalUtf8=predecessor_source.decode("ascii"),
                 eventCanonicalUtf8=[])
    raw = encode(value)
    require(len(raw) <= SCHEMA["limits"]["sourceBytes"], "001")
    return raw


def parse_source(data):
    value = parse(data, SCHEMA["sourceFields"], SCHEMA["limits"]["sourceBytes"])
    require(value["contractVersion"] == 1 and type(value["contractVersion"]) is int
            and type(value["predecessorSourceCanonicalUtf8"]) is str
            and type(value["eventCanonicalUtf8"]) is list
            and len(value["eventCanonicalUtf8"]) <= SCHEMA["limits"]["ledgerEvents"]
            and all(type(item) is str and item.isascii() for item in value["eventCanonicalUtf8"]), "001")
    return value


def zero_assault_from_replayed(source, state):
    """Prove the narrow fact only after a caller has authenticated the R03 replay."""
    require(SCHEMA["physicalSourceHashes"] == p01.SCHEMA["physicalSourceHashes"], "003")
    require(state["closed"] and state["phase"] == "breakdown"
            and state["activeCycle"]["ordinal"] == 2
            and state["originalMovementEnd"]["ordinal"] == 1
            and state["movementEnd"]["ordinal"] == 2
            and state["sequencePosition"]["positionId"].endswith(".combat.position-determination")
            and state["breakdownCompletionReceiptId"] == state["breakdownReceipts"][0]["receiptId"]
            and state["flow"] == {"kind": "idle"} and state["interruptContext"] is None,
            "003")
    weather = p01.current_weather_fact(source, state)
    content = p01.selected_content()
    pair = p01.exact_selected_profile(state, content)
    require(pair is not None and weather["kind"] == "normal", "003")
    actor, defender = pair
    static = {item["elementId"]: item for item in content["elements"]}
    require(len(static) == 2 and {item["sideId"] for item in static.values()}
            == {"axis", "commonwealth"}
            and all(item["organizationId"] == "land.organization.battalion"
                    and item["combatClassificationId"] == "land.combat-classification.combat-unit"
                    and len(item["components"]) == 1
                    and item["components"][0]["componentClassId"] == "land.combat-component.infantry"
                    and item["components"][0]["maximumToe"] == 10
                    for item in static.values())
            and state["activeCycle"]["actingSide"] == static[actor["elementId"]]["sideId"]
            and state["targetUses"] == [] and state["world"]["futureObligations"] == []
            and state["world"]["brokenVehicleLots"] == [], "003")
    edges = {frozenset((edge["firstLocationId"], edge["secondLocationId"]))
             for edge in content["edges"] if edge["features"] == []}
    locations = {item["locationId"]: item for item in content["locations"]}
    retreat_options = sorted(
        (edge["secondLocationId"] if edge["firstLocationId"] == defender["currentLocationId"]
         else edge["firstLocationId"])
        for edge in content["edges"]
        if defender["currentLocationId"] in
        (edge["firstLocationId"], edge["secondLocationId"])
        and edge["features"] == []
        and actor["currentLocationId"] not in
        (edge["firstLocationId"], edge["secondLocationId"]))
    defender_cp = defender["operationalState"]["capabilityPointsExpended"]
    require(len(content["edges"]) == 5 and all(edge["features"] == [] for edge in content["edges"])
            and frozenset((actor["currentLocationId"], defender["currentLocationId"])) not in edges
            and actor["currentLocationId"] != defender["currentLocationId"]
            and static[defender["elementId"]]["baseCapabilityPointAllowance"] == 10
            and defender_cp["denominator"] == 1 and defender_cp["numerator"] <= 9
            and retreat_options
            and all(locations[place]["terrainId"] == "land.terrain.clear"
                    for place in retreat_options)
            and p01.physical_shape(state, content)[0] == "provisional-empty", "003")
    # Land §11 permits only adjacent phasing Close Assault; §§10.11/10.31 do not
    # force an assault for this one-battalion-per-side content. Land §13.1 still
    # permits defender RBA after Barrage: this is NOT an empty Combat certificate.
    fact = dict(contractVersion=1, kind="zero-assault-only",
                creationBinding=state["world"]["creationBinding"],
                predecessorSourceHash=sha(p01.r03.raw(source, "Source")),
                predecessorReceiptId=state["breakdownCompletionReceiptId"],
                cycleId=state["cycleId"], positionId=state["sequencePosition"]["positionId"],
                stateVersion=state["stateVersion"], prefix=state["prefix"],
                worldHash=sha(encode(state["world"])), randomHash=sha(encode(state["randomState"])),
                weatherReceiptId=weather["receiptId"], weatherEventHash=weather["eventHash"],
                actingElementId=actor["elementId"], defenderElementId=defender["elementId"],
                actorLocationId=actor["currentLocationId"],
                defenderLocationId=defender["currentLocationId"],
                rbaClearRetreatLocations=retreat_options,
                sourceRules=SCHEMA["zeroAssaultRuleLocators"],
                rbaStillAvailable=True)
    identifier_value = receipt("zeroAssault", fact)
    return fact, identifier_value, state


def zero_assault_certificate(predecessor_source, parent_ledgers, r03_ledger):
    """Authenticate original CreationRequest and all twelve predecessor ledgers first."""
    require(type(parent_ledgers) is list and len(parent_ledgers) == 11
            and type(r03_ledger) is list and len(r03_ledger) == 1, "003")
    source, _, state = p01.r03.replay(predecessor_source, parent_ledgers, r03_ledger)
    return zero_assault_from_replayed(source, state)


def positions(state):
    catalog = p01.r03.SEQUENCE.expected_catalog()["positions"]
    index = next((i for i, entry in enumerate(catalog) if entry == state["sequencePosition"]), None)
    require(index is not None and index + 6 < len(catalog), "003")
    result = catalog[index:index + 7]
    expected = SCHEMA["stepNames"]
    require(all(result[i]["positionId"].endswith(".combat." + name)
                for i, name in enumerate(expected))
            and result[-1]["positionId"].endswith(".reserve-release"), "003")
    return result


def control_bytes(state):
    require(type(state) is dict and list(state) == SCHEMA["controlFields"]
            and state["admitted"] is False and state["publicActions"] == []
            and type(state["closed"]) is bool
            and type(state["stateVersion"]) is int and 0 <= state["stateVersion"] < 2**63
            and hash_value(state["prefix"])
            and type(state["stepIndex"]) is int and -1 <= state["stepIndex"] <= 6
            and identifier(state["certificateId"], "p01z.")
            and type(state["receipts"]) is list
            and len(state["receipts"]) <= SCHEMA["limits"]["ledgerEvents"], "006")
    raw = encode(state)
    require(len(raw) <= SCHEMA["limits"]["controlBytes"], "001")
    return raw


def initial(predecessor_source, parent_ledgers, r03_ledger):
    fact, cert, parent = zero_assault_certificate(predecessor_source, parent_ledgers, r03_ledger)
    require(parent["activeCycle"]["admittedPolicyBundleDigest"] == steps.env.Context().config_hash,
            "003")
    chain = positions(parent)
    state = dict(parent=parent, certificate=fact, certificateId=cert,
                positions=chain, stateVersion=parent["stateVersion"], prefix=parent["prefix"],
                position=copy.deepcopy(chain[0]), stepIndex=-1,
                selectionReceiptId=None, barrageReceiptId=None,
                rbaDecision=None, rbaOpenReceiptId=None, rbaOutcome=None,
                declineReceiptId=None, cancellationReceiptId=None,
                lastReceiptId=parent["breakdownCompletionReceiptId"],
                receipts=[], closed=False, admitted=False, publicActions=[])
    control_bytes(state)
    return state


def expected_input(state, kind, *, actor="system", now=None, available=True):
    return dict(contractVersion=1, kind=kind, actor=actor,
                cycleId=state["parent"]["cycleId"],
                expectedPriorVersion=state["stateVersion"],
                expectedPriorPrefix=state["prefix"],
                fromPositionId=state["position"]["positionId"],
                previousReceiptId=state["lastReceiptId"],
                decisionId=state["rbaDecision"]["decisionId"] if kind in
                ("decline-rba", "cancel-rba") and state["rbaDecision"] else None,
                participant=defender_ref(state) if kind == "decline-rba" else None,
                admittedAtUnixMilliseconds=now, clockAvailable=available)


def defender_ref(state):
    fact = state["certificate"]
    side = "commonwealth" if state["parent"]["activeCycle"]["actingSide"] == "axis" else "axis"
    return dict(creationBinding=fact["creationBinding"], originalSide=side,
                elementId=fact["defenderElementId"])


def transition(prior, value):
    """Apply one independently trusted input to derived private Control."""
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
    decision = s["rbaDecision"]
    if kind == "close-no-assault-selection":
        require(s["stepIndex"] == -1 and not s["receipts"]
                and value["actor"] == "system" and value["decisionId"] is None
                and value["participant"] is None and now is None, "004")
        event_type, receipt_kind, proof = "combat-selection-closed", "selection", "zero-assault-only"
        to_position = s["position"]
        s["stepIndex"] = 0
    elif kind == "complete-no-assault-step":
        step = s["stepIndex"]
        require(0 <= step < 6 and value["actor"] == "system"
                and value["decisionId"] is None and value["participant"] is None
                and now is None, "004")
        if step == 2:
            require(s["rbaOutcome"] in ("declined", "cancelled"), "005")
        elif step > 2:
            require(s["rbaOutcome"] in ("declined", "cancelled"), "005")
        event_type = "combat-step-completed:" + SCHEMA["stepNames"][step]
        receipt_kind, proof = "step", "no-assault"
        to_position = s["positions"][step + 1]
        s["stepIndex"] += 1
        s["position"] = copy.deepcopy(to_position)
        if step == 5:
            s["closed"] = True
    elif kind == "open-rba-decision":
        require(s["stepIndex"] == 2 and s["selectionReceiptId"] is not None
                and s["barrageReceiptId"] == s["lastReceiptId"]
                and decision is None and value["actor"] == "system"
                and value["decisionId"] is None and value["participant"] is None
                and type(now) is int and value["clockAvailable"], "004")
        timing = steps.inputs.make_timing(steps.env.Context().config, "rba", now)
        decision_id = receipt("decision", dict(certificateId=s["certificateId"],
                                               cycleId=s["parent"]["cycleId"],
                                               selectionReceiptId=s["selectionReceiptId"],
                                               barrageReceiptId=s["barrageReceiptId"],
                                               defender=defender_ref(s)))
        s["rbaDecision"] = dict(decisionId=decision_id, owner=defender_ref(s)["originalSide"],
                                 participant=defender_ref(s), timing=timing,
                                 barrageReceiptId=s["barrageReceiptId"])
        event_type, receipt_kind, proof = "combat-retreat-decision-opened", "rbaOpen", "rba-choice-open"
        to_position = s["position"]
    elif kind == "decline-rba":
        require(s["stepIndex"] == 2 and decision is not None and s["rbaOutcome"] is None
                and value["decisionId"] == decision["decisionId"]
                and value["actor"] == decision["owner"]
                and value["participant"] == decision["participant"]
                and s["barrageReceiptId"] == decision["barrageReceiptId"]
                and value["clockAvailable"] and type(now) is int
                and steps.inputs.clock_gate(decision["timing"], now, True) == "before-deadline", "005")
        s["rbaOutcome"] = "declined"
        event_type, receipt_kind, proof = "combat-retreat-before-assault-declined", "decline", "accepted-decline"
        to_position = s["position"]
    elif kind == "cancel-rba":
        require(s["stepIndex"] == 2 and s["rbaOutcome"] is None
                and value["actor"] == "system" and value["participant"] is None, "005")
        if decision is None:
            require(s["barrageReceiptId"] == s["lastReceiptId"]
                    and s["selectionReceiptId"] is not None
                    and s["rbaOpenReceiptId"] is None
                    and value["decisionId"] is None
                    and not value["clockAvailable"] and now is None, "005")
            event_type, receipt_kind, proof = (
                "combat-retreat-decision-preopen-cancelled", "preOpenCancel",
                "clock-unavailable-before-open")
        else:
            require(value["decisionId"] == decision["decisionId"]
                    and ((not value["clockAvailable"] and now is None) or
                         (value["clockAvailable"] and type(now) is int and
                          steps.inputs.clock_gate(decision["timing"], now, True)
                          in ("unavailable", "expired"))), "005")
            event_type, receipt_kind, proof = (
                "combat-retreat-decision-cancelled", "decline", "cancel-without-decline")
        s["rbaOutcome"] = "cancelled"
        to_position = s["position"]
    else:
        raise Invalid("CMB-P01-RBA-002")
    before = prior["parent"]
    effect = dict(proofKind=proof, fromPositionId=prior["position"]["positionId"],
                  toPositionId=to_position["positionId"],
                  selectionReceiptId=prior["selectionReceiptId"],
                  previousReceiptId=prior["lastReceiptId"],
                  decisionId=s["rbaDecision"]["decisionId"] if s["rbaDecision"] else None,
                  deadlineUnixMilliseconds=(s["rbaDecision"]["timing"]["deadlineUnixMilliseconds"]
                                            if s["rbaDecision"] else None),
                  acceptedAtUnixMilliseconds=now,
                  worldHash=prior["certificate"]["worldHash"],
                  randomHash=prior["certificate"]["randomHash"])
    cycle = before["activeCycle"]
    unsigned = dict(contractVersion=1, eventType=event_type,
                    campaignId=cycle["campaignId"], rulesetHash=cycle["rulesetHash"],
                    configurationHash=cycle["admittedPolicyBundleDigest"],
                    cycleId=before["cycleId"], priorVersion=prior["stateVersion"],
                    stateVersion=prior["stateVersion"] + 1, priorPrefix=prior["prefix"],
                    input=copy.deepcopy(value), effect=effect)
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
                               inputHash=sha(input_bytes(value)), receiptId=receipt_id,
                               eventHash=sha(raw), stateVersion=s["stateVersion"]))
    if kind == "close-no-assault-selection":
        s["selectionReceiptId"] = receipt_id
    elif kind == "complete-no-assault-step" and prior["stepIndex"] == 1:
        s["barrageReceiptId"] = receipt_id
    elif kind == "open-rba-decision":
        s["rbaOpenReceiptId"] = receipt_id
    elif kind == "decline-rba":
        s["declineReceiptId"] = receipt_id
    elif kind == "cancel-rba":
        s["cancellationReceiptId"] = receipt_id
    require(s["parent"] == prior["parent"] and s["certificate"] == prior["certificate"]
            and s["admitted"] is False and s["publicActions"] == [], "006")
    control_bytes(s)
    return s, raw, receipt_id


def replay(source_bytes, parent_ledgers, r03_ledger, own_ledger):
    source = parse_source(source_bytes)
    predecessor = source["predecessorSourceCanonicalUtf8"].encode("ascii")
    # R03 checks all original creation/eleven-ledger ancestry and source pins.
    state = initial(predecessor, parent_ledgers, r03_ledger)
    require(type(own_ledger) is list and len(own_ledger) <= SCHEMA["limits"]["ledgerEvents"]
            and all(type(item) is bytes for item in own_ledger)
            and len(own_ledger) == len(source["eventCanonicalUtf8"]), "001")
    for raw_input, recorded in zip(own_ledger, source["eventCanonicalUtf8"]):
        value = parse_input(raw_input)
        state, expected, _ = transition(state, value)
        actual = parse(recorded.encode("ascii"), SCHEMA["eventFields"],
                       SCHEMA["limits"]["eventBytes"])
        require(actual["contractVersion"] == 1 and actual["receiptId"] ==
                receipt("selection" if value["kind"] == "close-no-assault-selection"
                        else "step" if value["kind"] == "complete-no-assault-step"
                        else "rbaOpen" if value["kind"] == "open-rba-decision"
                        else "preOpenCancel" if actual["eventType"] ==
                        "combat-retreat-decision-preopen-cancelled"
                        else "decline", {k: actual[k] for k in SCHEMA["eventFields"][:-1]})
                and expected == recorded.encode("ascii"), "006")
    return state


def apply(source_bytes, parent_ledgers, r03_ledger, own_ledger, raw_input):
    state = replay(source_bytes, parent_ledgers, r03_ledger, own_ledger)
    value = parse_input(raw_input)
    input_hash = sha(raw_input)
    old = next((item for item in state["receipts"] if item["inputHash"] == input_hash), None)
    if old is not None:
        require(old["actor"] == value["actor"], "004")
        return source_bytes, own_ledger, None, old["receiptId"]
    after, event, receipt_id = transition(state, value)
    source = parse_source(source_bytes)
    source["eventCanonicalUtf8"].append(event.decode("ascii"))
    updated = encode(source)
    require(len(updated) <= SCHEMA["limits"]["sourceBytes"], "001")
    ledger = [*own_ledger, raw_input]
    rebuilt = replay(updated, parent_ledgers, r03_ledger, ledger)
    require(rebuilt == after, "006")
    return updated, ledger, event, receipt_id


def audit(evidence_path):
    """Authenticate all 300 original roots and count only narrow zero-assault facts."""
    import gzip
    result = load("p01_explicit_result", "verify-combat-p01-result-gates-v1.py")
    evidence = Path(evidence_path)
    for name, expected in result.PINS.items():
        require(hashlib.sha256((evidence / name).read_bytes()).hexdigest() == expected,
                "007")
    for name, expected in (("CNA_LandGameRules.pdf", SCHEMA["physicalSourceHashes"]["landRulesPdf"]),
                           ("ERR_CampaignforNorthAfrica.pdf",
                            SCHEMA["physicalSourceHashes"]["septemberErrataPdf"])):
        require(hashlib.sha256((evidence / name).read_bytes()).hexdigest() == expected, "007")
    manifest = json.loads((evidence / "manifest.json").read_text())
    summary = json.loads((evidence / "summary.json").read_text())
    partition = json.loads((evidence / "bounded-profile-audit.json").read_text())
    require(len(manifest["chunks"]) == 11 and len(summary["rows"]) ==
            len(partition["rows"]) == summary["count"] == 300, "007")
    zero_assault = 0
    excluded = 0
    keys = set()
    index = 0
    for chunk in manifest["chunks"]:
        data = (evidence / chunk["name"]).read_bytes()
        require(hashlib.sha256(data).hexdigest() == chunk["sha256"] ==
                summary["chunkSha256"][chunk["name"]], "007")
        for line in gzip.decompress(data).splitlines():
            row = json.loads(line)
            record = row["r03"]
            key = record["rowKey"]
            require(row["index"] == index and key not in keys
                    and key == summary["rows"][index]["rowKey"] ==
                    partition["rows"][index]["rowKey"], "007")
            keys.add(key)
            state = result.require_control_vector(record, result.authenticated_control(record))
            source = p01.r03.parse(record["vectors"]["sourceCanonicalUtf8"].encode("ascii"),
                                   "Source")
            try:
                fact, cert, _ = zero_assault_from_replayed(source, state)
            except Invalid:
                excluded += 1
            else:
                require(identifier(cert, "p01z.") and fact["rbaStillAvailable"] is True
                        and partition["rows"][index]["partition"] == "provisional-empty",
                        "007")
                zero_assault += 1
            index += 1
    require(index == len(keys) == 300 and zero_assault == 244 and excluded == 56, "007")
    # This audit classifies predecessor roots only. Admission and public-action
    # invariants belong to executable twelfth-ledger trace tests, not this count.
    return dict(authenticatedRows=300, zeroAssaultOnly=244, otherRows=56)


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("evidence", type=Path)
    print(audit(parser.parse_args().evidence))
