#!/usr/bin/env python3
"""Private ordinal-two paid-commit reference; no public or native admission.

Actor, source and structural-step authority are trusted caller inputs. Every
readback independently replays the original CreationRequest, eleven parent
ledgers, R03, selected assault and Force Assignment before this one event.
"""
import copy
import functools
import hashlib
import importlib.util
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SCHEMA = json.loads((ROOT / "combat-p01-paid-commit-v1.schema.json").read_text())


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


assignment = load("paid_commit_assignment", "verify-combat-p01-force-assignment-v1.py")
result = load("paid_commit_results", "verify-combat-p01-result-gates-v1.py")
geometry = load("paid_commit_geometry", "verify-combat-p01-geometry-gates-v1.py")
p01 = assignment.p01


class Invalid(ValueError):
    pass


def require(ok, code):
    if not ok:
        raise Invalid("CMB-P01-PAID-" + code)


def encode(value):
    return assignment.encode(value)


def sha(raw):
    return assignment.sha(raw)


def digest(kind, value):
    raw = SCHEMA["receiptDomains"][kind].encode("ascii") + b"\0" + encode(value)
    return SCHEMA["receiptPrefixes"][kind] + hashlib.sha256(raw).hexdigest()


def ident(value, prefix):
    return type(value) is str and re.fullmatch(
        re.escape(prefix) + r"[0-9a-f]{64}", value) is not None


def bounded(value, depth=0):
    require(depth <= SCHEMA["limits"]["depth"], "001")
    if type(value) is dict:
        require(len(value) <= 32, "001")
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
        raise Invalid("CMB-P01-PAID-001") from error
    bounded(value)
    require(type(value) is dict and list(value) == fields and encode(value) == raw, "001")
    return value


def check_input(value):
    require(type(value) is dict and list(value) == SCHEMA["inputFields"]
            and value["contractVersion"] == 1
            and type(value["contractVersion"]) is int
            and value["kind"] == "commit"
            and value["actor"] == "system"
            and ident(value["cycleId"], "sha256:")
            and type(value["expectedPriorVersion"]) is int
            and 0 <= value["expectedPriorVersion"] < 2**63
            and ident(value["expectedPriorPrefix"], "sha256:")
            and type(value["fromPositionId"]) is str
            and re.fullmatch(r"land\.position\.[a-z0-9.-]{1,240}",
                             value["fromPositionId"])
            and ident(value["previousReceiptId"], "p01y.")
            and ident(value["roundId"], "p01f.")
            and ident(value["candidateId"], "p01a.")
            and type(value["assignmentReceiptIds"]) is list
            and len(value["assignmentReceiptIds"]) == 5
            and ident(value["assignmentReceiptIds"][0], "p01u.")
            and all(ident(item, "p01v.") for item in value["assignmentReceiptIds"][1:3])
            and all(ident(item, "p01y.") for item in value["assignmentReceiptIds"][3:]),
            "002")
    require(len(encode(value)) <= SCHEMA["limits"]["inputBytes"], "001")
    return value


def input_bytes(value):
    return encode(check_input(value))


def parse_input(raw):
    return check_input(parse(raw, SCHEMA["inputFields"],
                             SCHEMA["limits"]["inputBytes"]))


def source_for(assignment_source):
    require(type(assignment_source) is bytes and assignment_source.isascii(), "001")
    assignment.parse_source(assignment_source)
    raw = encode(dict(contractVersion=1,
                      forceAssignmentSourceCanonicalUtf8=assignment_source.decode("ascii"),
                      eventCanonicalUtf8=[]))
    require(len(raw) <= SCHEMA["limits"]["sourceBytes"], "001")
    return raw


def parse_source(raw):
    value = parse(raw, SCHEMA["sourceFields"], SCHEMA["limits"]["sourceBytes"])
    require(type(value["contractVersion"]) is int and value["contractVersion"] == 1
            and type(value["forceAssignmentSourceCanonicalUtf8"]) is str
            and value["forceAssignmentSourceCanonicalUtf8"].isascii()
            and type(value["eventCanonicalUtf8"]) is list
            and len(value["eventCanonicalUtf8"]) <= SCHEMA["limits"]["ledgerEvents"]
            and all(type(x) is str and x.isascii()
                    for x in value["eventCanonicalUtf8"]), "001")
    assignment.parse_source(
        value["forceAssignmentSourceCanonicalUtf8"].encode("ascii"))
    return value


def assignment_receipts(parent):
    by_role = {x["role"]: x["sealedReceiptId"] for x in parent["slots"]}
    return [parent["openingReceiptId"], by_role["attacker"], by_role["defender"],
            *parent["stepReceipts"]]


def initial(assignment_source, parent_ledgers, r03_ledger,
            selected_ledger, assignment_ledger):
    parent = assignment.replay(assignment_source, parent_ledgers, r03_ledger,
                               selected_ledger, assignment_ledger)
    require(parent["status"] == "prepared" and parent["stepIndex"] == 5
            and not parent["closed"]
            and parent["position"]["positionId"].endswith(".close-assault")
            and len(parent["receipts"]) == 5
            and parent["receipts"][-1]["receiptId"] == parent["lastReceiptId"]
            and len(parent["stepReceipts"]) == 2
            and all(x["sealedReceiptId"] is not None for x in parent["slots"])
            and parent["admitted"] is False and parent["publicActions"] == [],
            "003")
    require(all(actual["allocation"] == expected["allocation"]
                for actual, expected in zip(parent["slots"],
                                            assignment.exact_slots(parent["parent"]))),
            "003")
    r03 = parent["parent"]["parent"]
    require(r03["targetUses"] == r03["priorCycleTargetUses"] ==
            r03["attackHistory"] == [], "003")
    state = dict(parent=parent, assignmentSourceHash=sha(assignment_source),
                 stateVersion=parent["stateVersion"], prefix=parent["prefix"],
                 position=copy.deepcopy(parent["position"]), status="prepared",
                 world=copy.deepcopy(r03["world"]),
                 randomState=copy.deepcopy(r03["randomState"]),
                 attackHistory=[], targetUses=[], commitmentId=None,
                 preflightHash=None, lastReceiptId=parent["lastReceiptId"],
                 receipts=[], admitted=False, publicActions=[])
    control_bytes(state)
    return state


def control_bytes(state):
    require(type(state) is dict and state["status"] in ("prepared", "committed")
            and state["admitted"] is False and state["publicActions"] == []
            and len(state["receipts"]) <= 1 and ident(state["prefix"], "sha256:")
            and state["randomState"] == state["parent"]["parent"]["parent"]["randomState"]
            and state["parent"]["admitted"] is False, "007")
    raw = encode(state)
    require(len(raw) <= SCHEMA["limits"]["controlBytes"], "001")
    return raw


def expected_input(state):
    parent = state["parent"]
    r03 = parent["parent"]["parent"]
    fa_receipts = parent["receipts"]
    require(len(fa_receipts) == 5
            and parent["openingReceiptId"] == fa_receipts[0]["receiptId"]
            and set(assignment_receipts(parent)[1:3]) ==
            {fa_receipts[1]["receiptId"], fa_receipts[2]["receiptId"]}
            and parent["stepReceipts"] ==
            [fa_receipts[3]["receiptId"], fa_receipts[4]["receiptId"]]
            and parent["lastReceiptId"] == fa_receipts[-1]["receiptId"],
            "006")
    return dict(contractVersion=1, kind="commit", actor="system",
                cycleId=r03["cycleId"],
                expectedPriorVersion=state["stateVersion"],
                expectedPriorPrefix=state["prefix"],
                fromPositionId=state["position"]["positionId"],
                previousReceiptId=state["lastReceiptId"],
                roundId=parent["roundId"],
                candidateId=parent["parent"]["candidateId"],
                assignmentReceiptIds=assignment_receipts(parent))


def pinned_rules():
    """Recheck adopted canonical RulesInput and its source/amendment pins."""
    golden = next(x for x in json.loads(
        (ROOT / "fixtures/combat-rules-inputs-v1.json").read_text())["goldens"]
                  if x["kind"] == "RulesInput")
    raw = golden["canonicalUtf8"].encode("ascii")
    require(len(raw) == golden["byteCount"]
            and sha(raw) == golden["sha256"], "004")
    rules = json.loads(raw)
    require(rules == result.inputs.expected_rules(), "004")
    hashes = {x["sourceId"]: x["sha256"].removeprefix("sha256:")
              for x in rules["sourceEvidence"]}
    require(hashes["spi-1979-land-rules"] ==
            result.PINS["CNA_LandGameRules.pdf"]
            and hashes["spi-1979-september-errata"] ==
            result.PINS["ERR_CampaignforNorthAfrica.pdf"]
            and rules["amendment"] == dict(
                rulingId="CMB-SRC-RUL-001", role="defender", differential=2,
                coordinates=[34, 35, 36], lossPercent=10), "004")
    return rules, sha(raw)


@functools.lru_cache(maxsize=8)
def outcome_support(rules_hash, creation_binding, actor_side):
    """Enumerate every adopted selected result, disposition, custody and World cut.

    This cache is only for the immutable pinned RulesInput and normalized World7
    fixture. Each transition still validates its exact authenticated World,
    geometry, receipts and resources before it can use these counts.
    """
    rules, actual_hash = pinned_rules()
    require(rules_hash == actual_hash and actor_side in ("axis", "commonwealth"),
            "004")
    representative = dict(world=dict(creationBinding=creation_binding))
    matrix = result.verify_matrix(rules, [(0, representative, None, None)])
    require(matrix == dict(
        moralePairs=1296, jointCoordinates=6480,
        resultCases={"axis": 8840, "commonwealth": 8840},
        settlementWorlds={"axis": 9728, "commonwealth": 9728},
        branchKindsPerOrientation=39), "004")
    coordinates = [10 * t + o for t in range(1, 7) for o in range(1, 7)]
    maximum = 0
    cases = 0
    cuts = 0
    for differential in range(-2, 3):
        for attacker in coordinates:
            for defender in coordinates:
                base = result.independent_result(
                    rules, differential, attacker, defender,
                    1 if result_capture(rules, differential, attacker, defender)
                    else None)
                for disposition in (("retreat", "refuse-retreat")
                                    if base["requiredRetreat"] else ("not-required",)):
                    for die in (range(1, 7) if base["capturedRole"] else (None,)):
                        facts = result.independent_result(
                            rules, differential, attacker, defender, die)
                        for custody in (("relocate-and-guard", "leave-unguarded")
                                        if facts["capturedRole"] else (None,)):
                            case = dict(
                                settlementId="p01.proof", creationBinding=creation_binding,
                                attackerSide=actor_side, preAssaultCp=[4, 0],
                                differential=differential, attackerCoordinate=attacker,
                                defenderCoordinate=defender, captureDie=die,
                                retreatChoice=disposition, custodyChoice=custody)
                            cases += 1
                            for cut in ("resolved", "disposition", "losses", "retreat",
                                        "custody", "relationships"):
                                if cut == "custody" and not facts["capturedRole"]:
                                    continue
                                projected = result.world.world_at(case, cut)
                                count = len(encode(projected))
                                require(count <= SCHEMA["limits"]["projectedWorldBytes"],
                                        "005")
                                maximum = max(maximum, count)
                                cuts += 1
    require(cases == 9728 and cuts > cases * 5 and maximum > 0, "005")
    return dict(**matrix, projectedCases=cases, projectedCuts=cuts,
                maximumProjectedWorldBytes=maximum)


def result_capture(rules, differential, attacker, defender):
    effects = next(x for x in rules["effects"]
                   if x["differential"] == differential)
    return (attacker // 10 + attacker % 10 in effects["attackerCaptureSums"]
            or defender // 10 + defender % 10 in effects["defenderCaptureSums"])


def preflight(state):
    """Join AC-004 all-outcome and AC-005 physical proofs before mutation."""
    require(state["status"] == "prepared" and state["commitmentId"] is None
            and state["attackHistory"] == state["targetUses"] == [], "006")
    parent = state["parent"]
    r03 = parent["parent"]["parent"]
    require(state["world"] == r03["world"]
            and state["randomState"] == r03["randomState"]
            and state["prefix"] == parent["prefix"]
            and state["stateVersion"] == parent["stateVersion"]
            and state["lastReceiptId"] == parent["lastReceiptId"]
            and parent["parent"]["candidate"]["worldHash"] ==
            sha(encode(r03["world"])), "006")
    require(assignment.selected.receipt(
        "candidate", parent["parent"]["candidate"]) ==
        parent["parent"]["candidateId"], "006")
    require(len(r03["world"]["elements"]) == 2
            and result.normalized_positive_world(r03) ==
            result.world.world_at(result.initial_case(r03), "initial"),
            "006")
    side = r03["activeCycle"]["actingSide"]
    members = {x["elementId"]: x for x in r03["world"]["elements"]}
    attacker = parent["parent"]["candidate"]["attacker"]["elementId"]
    defender = parent["parent"]["candidate"]["defender"]["elementId"]
    require(set(members) == {attacker, defender}
            and members[attacker]["operationalState"]
            ["capabilityPointsExpended"] == dict(numerator=4, denominator=1)
            and members[defender]["operationalState"]
            ["capabilityPointsExpended"] == dict(numerator=0, denominator=1),
            "006")
    rules, rules_hash = pinned_rules()
    require(rules["costs"]["attackerCapabilityPoints"] == 5
            and rules["costs"]["defenderCapabilityPoints"] == 3, "004")
    content = p01.selected_content()
    require(parent["parent"]["candidate"]["worldHash"] ==
            sha(encode(r03["world"])) and
            parent["parent"]["candidate"]["randomHash"] ==
            sha(encode(r03["randomState"])), "006")
    shapes = geometry.certify_row(r03, content)
    require(len(shapes) == 4 and
            {(x["defenderMoves"], x["victimSide"]) for x in shapes} ==
            {(False, "axis"), (True, "axis"),
             (False, "commonwealth"), (True, "commonwealth")}, "006")
    quantities = geometry.source_quantity_bounds(rules)
    paths = geometry.check_projected_paths(r03, rules, content, {})
    require(paths == 8 and quantities == dict(
        maxLossToe=3, minimumSurvivingDonorToe=7,
        maxCaptiveToe=3, movingGuardCapacityToe=5), "006")
    support = outcome_support(rules_hash, r03["world"]["creationBinding"], side)
    require(support["maximumProjectedWorldBytes"] <=
            SCHEMA["limits"]["projectedWorldBytes"], "005")
    summary = dict(contractVersion=1, rulesInputHash=rules_hash,
                   contentHash="sha256:" + p01.SCHEMA["contentSha256"],
                   worldHash=sha(encode(r03["world"])),
                   randomHash=sha(encode(r03["randomState"])),
                   actorSide=side, matrix=support, placementShapes=4,
                   projectedPaths=paths, quantityBounds=quantities)
    return summary


def paid_world(prior):
    parent = prior["parent"]
    candidate = parent["parent"]["candidate"]
    world = copy.deepcopy(prior["world"])
    costs = []
    for role, cp_cost in (("attacker", 5), ("defender", 3)):
        slot = next(x for x in parent["slots"] if x["role"] == role)
        unit = candidate[role]
        require(slot["allocation"]["unit"] == unit
                and slot["allocation"]["committedToe"] == 10
                and slot["sealedReceiptId"] is not None, "006")
        element = next(x for x in world["elements"]
                       if x["elementId"] == unit["elementId"])
        before = element["operationalState"]["capabilityPointsExpended"]
        require(before == dict(numerator=4 if role == "attacker" else 0,
                               denominator=1)
                and element["ammunition"]["points"] == 10
                and element["components"][0]["currentToe"] == 10
                and element["operationalState"]["cohesionLevel"] == 0, "006")
        after = before["numerator"] + cp_cost
        require(after <= 10, "006")
        element["operationalState"]["capabilityPointsExpended"] = dict(
            numerator=after, denominator=1)
        element["ammunition"]["points"] = 0
        costs.append(dict(role=role, unit=copy.deepcopy(unit),
                          componentId=slot["allocation"]["componentId"],
                          committedToe=10, beforeCp=copy.deepcopy(before),
                          cpCost=cp_cost, afterCp=dict(numerator=after, denominator=1),
                          beforeAmmo=10, ammoCost=10, afterAmmo=0,
                          cohesionBefore=0, cohesionAfter=0))
    return world, costs


def transition(prior, value):
    check_input(value)
    require(prior["status"] == "prepared" and not prior["receipts"]
            and prior["stateVersion"] < 2**63 - 1
            and value == expected_input(prior), "006")
    # Every failure below precedes mutation. The caller must atomically publish
    # returned source + ledger + event; there is no separate debit operation.
    proof = preflight(prior)
    paid, costs = paid_world(prior)
    parent = prior["parent"]
    r03 = parent["parent"]["parent"]
    cycle = r03["activeCycle"]
    candidate = parent["parent"]["candidate"]
    allocations = [copy.deepcopy(x["allocation"]) for x in parent["slots"]]
    preimage = dict(cycleId=r03["cycleId"], priorVersion=prior["stateVersion"],
                    priorPrefix=prior["prefix"], candidateId=parent["parent"]["candidateId"],
                    selectionReceiptId=parent["parent"]["selectionReceiptId"],
                    declineReceiptId=parent["parent"]["declineReceiptId"],
                    assignmentReceiptIds=assignment_receipts(parent),
                    worldHash=sha(encode(prior["world"])),
                    randomHash=sha(encode(prior["randomState"])),
                    allocations=allocations, preflightHash=digest("preflight", proof))
    commitment_id = digest("commitment", preimage)
    segment_id = digest("segment", dict(
        cycleId=r03["cycleId"], gameTurn=cycle["gameTurn"],
        operationStage=cycle["operationStage"]))
    history = dict(commitmentId=commitment_id, cycleId=r03["cycleId"],
                   segmentId=segment_id,
                   attacker=copy.deepcopy(candidate["attacker"]),
                   defender=copy.deepcopy(candidate["defender"]),
                   targetLocationId=candidate["targetLocationId"],
                   gameTurn=cycle["gameTurn"],
                   operationStage=cycle["operationStage"])
    target_use = dict(commitmentId=commitment_id, segmentId=segment_id,
                      targetLocationId=candidate["targetLocationId"])
    effect = dict(commitmentId=commitment_id,
                  selectionReceiptId=parent["parent"]["selectionReceiptId"],
                  declineReceiptId=parent["parent"]["declineReceiptId"],
                  assignmentReceiptIds=assignment_receipts(parent),
                  candidateId=parent["parent"]["candidateId"],
                  allocations=allocations, preflight=proof,
                  preflightHash=digest("preflight", proof), costs=costs,
                  attackHistoryEdge=history, targetUse=target_use,
                  beforeWorldHash=sha(encode(prior["world"])),
                  afterWorldHash=sha(encode(paid)),
                  randomHash=sha(encode(prior["randomState"])),
                  previousReceiptId=prior["lastReceiptId"])
    unsigned = dict(contractVersion=1, eventType="combat-attack-committed",
                    campaignId=cycle["campaignId"], rulesetHash=cycle["rulesetHash"],
                    configurationHash=cycle["admittedPolicyBundleDigest"],
                    cycleId=r03["cycleId"], priorVersion=prior["stateVersion"],
                    stateVersion=prior["stateVersion"] + 1,
                    priorPrefix=prior["prefix"], input=copy.deepcopy(value),
                    effect=effect)
    require(list(unsigned) == SCHEMA["eventFields"][:-1]
            and list(effect) == SCHEMA["effectFields"], "007")
    receipt_id = digest("event", unsigned)
    event = encode({**unsigned, "receiptId": receipt_id})
    require(len(event) <= SCHEMA["limits"]["eventBytes"], "001")
    after = copy.deepcopy(prior)
    after["world"] = paid
    after["status"] = "committed"
    after["commitmentId"] = commitment_id
    after["preflightHash"] = effect["preflightHash"]
    after["attackHistory"] = [history]
    after["targetUses"] = [target_use]
    after["stateVersion"] = unsigned["stateVersion"]
    after["prefix"] = p01.r03.SEQUENCE.prefix_event(prior["prefix"], event)
    after["lastReceiptId"] = receipt_id
    after["receipts"] = [dict(kind="commit", actor="system",
                              inputHash=sha(input_bytes(value)),
                              receiptId=receipt_id, eventHash=sha(event),
                              stateVersion=after["stateVersion"])]
    require(after["randomState"] == prior["randomState"]
            and after["parent"] == prior["parent"]
            and after["position"] == prior["position"]
            and after["admitted"] is False and after["publicActions"] == []
            and all(a["components"] == b["components"]
                    and a["currentLocationId"] == b["currentLocationId"]
                    for a, b in zip(after["world"]["elements"],
                                    prior["world"]["elements"])), "007")
    control_bytes(after)
    return after, event, receipt_id


def replay(source_bytes, parent_ledgers, r03_ledger, selected_ledger,
           assignment_ledger, own_ledger):
    packet = parse_source(source_bytes)
    source = packet["forceAssignmentSourceCanonicalUtf8"].encode("ascii")
    state = initial(source, parent_ledgers, r03_ledger,
                    selected_ledger, assignment_ledger)
    require(type(own_ledger) is list and
            len(own_ledger) == len(packet["eventCanonicalUtf8"])
            and all(type(x) is bytes for x in own_ledger), "001")
    for raw_input, recorded in zip(own_ledger, packet["eventCanonicalUtf8"]):
        value = parse_input(raw_input)
        state, expected, _ = transition(state, value)
        parse(recorded.encode("ascii"), SCHEMA["eventFields"],
              SCHEMA["limits"]["eventBytes"])
        require(expected == recorded.encode("ascii"), "007")
    return state


def apply(source_bytes, parent_ledgers, r03_ledger, selected_ledger,
          assignment_ledger, own_ledger, raw_input):
    state = replay(source_bytes, parent_ledgers, r03_ledger,
                   selected_ledger, assignment_ledger, own_ledger)
    value = parse_input(raw_input)
    old = next((x for x in state["receipts"]
                if x["inputHash"] == sha(raw_input)), None)
    if old is not None:
        require(old["actor"] == value["actor"], "006")
        return source_bytes, own_ledger, None, old["receiptId"]
    after, event, receipt_id = transition(state, value)
    packet = parse_source(source_bytes)
    packet["eventCanonicalUtf8"].append(event.decode("ascii"))
    updated = encode(packet)
    require(len(updated) <= SCHEMA["limits"]["sourceBytes"], "001")
    ledger = [*own_ledger, raw_input]
    require(replay(updated, parent_ledgers, r03_ledger, selected_ledger,
                   assignment_ledger, ledger) == after, "007")
    return updated, ledger, event, receipt_id


def prepared_trace(r03_source, parent_ledgers, r03_ledger):
    """Build only canonical private predecessors from authenticated root bytes."""
    selected = assignment.selected
    selected_source = selected.source_for(r03_source)
    selected_state = selected.initial(r03_source, parent_ledgers, r03_ledger)
    selected_ledger = []
    attacker = selected_state["candidate"]["attacker"]["originalSide"]
    defender = selected_state["candidate"]["defender"]["originalSide"]
    for kind, actor, now in (
            ("open-selection", "system", 1000),
            ("select-assault", attacker, 1001),
            ("complete-step", "system", None),
            ("complete-step", "system", None),
            ("open-rba", "system", 1002),
            ("decline-rba", defender, 1003),
            ("complete-step", "system", None)):
        value = selected.expected_input(selected_state, kind, actor=actor, now=now)
        selected_state, event, _ = selected.transition(selected_state, value)
        packet = selected.parse_source(selected_source)
        packet["eventCanonicalUtf8"].append(event.decode("ascii"))
        selected_source = encode(packet)
        selected_ledger.append(selected.input_bytes(value))
    fa_source = assignment.source_for(selected_source)
    fa_state = assignment.initial(
        selected_source, parent_ledgers, r03_ledger, selected_ledger)
    fa_ledger = []
    for kind, role, now in (
            ("open-assignment", None, 2000),
            ("seal-assignment", "attacker", 2001),
            ("seal-assignment", "defender", 2002),
            ("complete-step", None, None),
            ("complete-step", None, None)):
        owner = next((x["owner"] for x in fa_state["slots"]
                      if x["role"] == role), "system")
        value = assignment.expected_input(fa_state, kind, actor=owner,
                                          role=role, now=now)
        fa_state, event, _ = assignment.transition(fa_state, value)
        packet = assignment.parse_source(fa_source)
        packet["eventCanonicalUtf8"].append(event.decode("ascii"))
        fa_source = encode(packet)
        fa_ledger.append(assignment.input_bytes(value))
    require(fa_state["status"] == "prepared" and fa_state["stepIndex"] == 5,
            "008")
    return selected_ledger, fa_source, fa_ledger


def audit(evidence):
    """Original retained 300-root provenance plus all 44 private paid cuts."""
    import gzip

    summary = assignment.audit(evidence)
    require(summary == dict(authenticatedRows=300, selectedCandidates=44,
                            candidateAbsent=256, admittedRows=0,
                            publicActionCount=0), "008")
    pinned = result.pinned_rules(evidence)
    require(pinned == pinned_rules()[0], "008")
    positives = result.positive_worlds(evidence)
    require(len(positives) == 44, "008")
    authenticated = {index: state for index, state, _, _ in positives}
    manifest = json.loads((evidence / "manifest.json").read_text())
    committed = 0
    counts = {"axis": 0, "commonwealth": 0}
    max_sizes = dict(sourceBytes=0, controlBytes=0, eventBytes=0)
    for chunk in manifest["chunks"]:
        raw = (evidence / chunk["name"]).read_bytes()
        require(hashlib.sha256(raw).hexdigest() == chunk["sha256"], "008")
        for line in gzip.decompress(raw).splitlines():
            row = json.loads(line)
            if row["index"] not in authenticated:
                continue
            vectors = row["r03"]["vectors"]
            r03_source = vectors["sourceCanonicalUtf8"].encode("ascii")
            parents = result.ancestry.original_ledgers(
                vectors["parentInputLedgersCanonicalUtf8"])
            r03_ledger = [vectors["inputCanonicalUtf8"].encode("ascii")]
            selected_ledger, fa_source, fa_ledger = prepared_trace(
                r03_source, parents, r03_ledger)
            own_source = source_for(fa_source)
            before = initial(fa_source, parents, r03_ledger,
                             selected_ledger, fa_ledger)
            require(before["parent"]["parent"]["parent"]["world"] ==
                    authenticated[row["index"]]["world"], "008")
            after, event, receipt = transition(before, expected_input(before))
            require(after["status"] == "committed"
                    and after["lastReceiptId"] == receipt
                    and after["admitted"] is False
                    and after["publicActions"] == [], "008")
            packet = parse_source(own_source)
            packet["eventCanonicalUtf8"].append(event.decode("ascii"))
            updated = encode(packet)
            max_sizes["sourceBytes"] = max(max_sizes["sourceBytes"], len(updated))
            max_sizes["controlBytes"] = max(
                max_sizes["controlBytes"], len(control_bytes(after)))
            max_sizes["eventBytes"] = max(max_sizes["eventBytes"], len(event))
            side = authenticated[row["index"]]["activeCycle"]["actingSide"]
            counts[side] += 1
            committed += 1
    require(committed == 44 and counts == {"axis": 22, "commonwealth": 22}
            and max_sizes["sourceBytes"] <= SCHEMA["limits"]["sourceBytes"]
            and max_sizes["controlBytes"] <= SCHEMA["limits"]["controlBytes"]
            and max_sizes["eventBytes"] <= SCHEMA["limits"]["eventBytes"],
            "008")
    return dict(authenticatedRows=300, privatePaidCommits=44,
                candidateAbsent=256, admittedRows=0, publicActions=0,
                orientations=counts, measuredBytes=max_sizes,
                resultMatrix=dict(resultCases=8840, settlementWorlds=9728),
                physicalPlacementShapes=4)


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("evidence", type=Path)
    print(json.dumps(audit(parser.parse_args().evidence), sort_keys=True))
