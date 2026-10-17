#!/usr/bin/env python3
"""Provisional private Breakdown successor to authentic R02 moved-repeat Movement."""

import copy
import hashlib
import importlib.util
import json
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
REPOSITORY = ROOT.parent.parent
NAME = "combat-actual-moved-repeat-breakdown-completion-v1"
INVENTORY = json.loads((ROOT / (NAME + ".schema.json")).read_bytes())
SCHEMA = {kind: [tuple(field.split(":")) for field in fields.split()] for kind, fields in INVENTORY["objects"].items()}
LIMITS = INVENTORY["limits"]
DEPENDENCIES = INVENTORY["runtimeSourceHashes"]
KIND = "complete-breakdown-segment"

spec = importlib.util.spec_from_file_location("authentic_r02_parent", ROOT / "verify-combat-actual-moved-repeat-movement-completion-v1.py")
parent = importlib.util.module_from_spec(spec)
spec.loader.exec_module(parent)


class Invalid(ValueError):
    def __init__(self, number):
        super().__init__(f"CMB-MRB-{number:03}")
        self.number = number


def require(condition, number):
    if not condition:
        raise Invalid(number)


def encode(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("ascii")


def sha(data):
    return "sha256:" + hashlib.sha256(data).hexdigest()


def identity(kind, data):
    return hashlib.sha256(INVENTORY["domains"][kind].encode("ascii") + b"\0" + data).hexdigest()


def inherited(call):
    try:
        return call()
    except ValueError as error:
        code = re.search(r"CMB-[A-Z]+-(\d{3})", str(error))
        if code:
            raise Invalid(int(code[1])) from error
        raise


def byte_limit(kind):
    return {
        "Source": LIMITS["sourceBytes"],
        "Control": LIMITS["controlBytes"],
        "Proof": LIMITS["proofBytes"],
        "Event": LIMITS["eventBytes"],
        "Input": LIMITS["inputBytes"],
        "Command": LIMITS["inputBytes"],
    }.get(kind, LIMITS["proofBytes"])


def typed(value, kind, depth=0, owner="", field=""):
    require(depth <= LIMITS["depth"], 1)
    if kind.endswith("?"):
        if value is not None:
            typed(value, kind[:-1], depth, owner, field)
    elif kind.endswith("[]"):
        require(type(value) is list and len(value) <= INVENTORY["arrayBounds"].get(owner + "." + field, LIMITS["arrayItems"]), 1)
        for member in value:
            typed(member, kind[:-2], depth + 1)
    elif kind in SCHEMA:
        fields = SCHEMA[kind]
        require(type(value) is dict and set(value) == {name for name, _ in fields}, 1)
        for name, member_kind in fields:
            typed(value[name], member_kind, depth + 1, kind, name)
        if kind in ("Source", "Base", "Command", "Event", "Control", "Proof"):
            require(value["contractVersion"] == 1, 3)
    elif kind in ("parentCarrier", "controlCarrier", "eventCarrier"):
        cap = {"parentCarrier": LIMITS["parentSourceBytes"], "controlCarrier": LIMITS["parentControlBytes"], "eventCarrier": LIMITS["eventBytes"]}[kind]
        require(type(value) is str and value.isascii() and 0 < len(value) <= cap, 1)
    elif kind == "kind":
        require(type(value) is str and value == KIND, 3)
    elif kind == "phase":
        require(type(value) is str and value in ("breakdown", "combat"), 3)
    elif kind == "profile":
        require(type(value) is str and value in ("paid", "armed"), 3)
    elif kind in ("int", "long"):
        ceiling = 2**31 if kind == "int" else 2**63
        require(type(value) is int and 0 <= value < ceiling, 2)
    else:
        inherited(lambda: parent.typed(value, kind))


def canonical(value, kind):
    if kind.endswith("?"):
        return None if value is None else canonical(value, kind[:-1])
    if kind.endswith("[]"):
        return [canonical(member, kind[:-2]) for member in value]
    if kind in SCHEMA:
        return {name: canonical(value[name], member_kind) for name, member_kind in SCHEMA[kind]}
    if kind in ("parentCarrier", "controlCarrier", "eventCarrier", "kind", "phase", "profile"):
        return value
    return inherited(lambda: parent.canonical(value, kind))


def bounds(value, depth=0):
    require(depth <= LIMITS["depth"], 1)
    if type(value) is dict:
        require(len(value) <= LIMITS["objectFields"], 1)
        for member in value.values():
            bounds(member, depth + 1)
    elif type(value) is list:
        require(len(value) <= LIMITS["arrayItems"], 1)
        for member in value:
            bounds(member, depth + 1)


def raw(value, kind):
    bounds(value)
    typed(value, kind)
    data = encode(canonical(value, kind))
    require(len(data) <= byte_limit(kind), 1)
    return data


def preparse(data):
    stack = []
    quoted = escaped = False
    for byte in data:
        if quoted:
            if escaped:
                escaped = False
            elif byte == 92:
                escaped = True
            elif byte == 34:
                quoted = False
            continue
        if byte in (9, 10, 13, 32):
            continue
        if byte == 34:
            if stack:
                stack[-1][2] = True
            quoted = True
        elif byte in (123, 91):
            if stack:
                stack[-1][2] = True
            require(len(stack) <= LIMITS["depth"], 1)
            stack.append([byte, 0, False])
        elif byte in (125, 93):
            require(bool(stack) and (stack[-1][0], byte) in ((123, 125), (91, 93)), 1)
            opening, commas, occupied = stack.pop()
            require(commas + int(occupied) <= (LIMITS["arrayItems"] if opening == 91 else LIMITS["objectFields"]), 1)
        elif byte == 44 and stack:
            stack[-1][1] += 1
            require(stack[-1][1] < (LIMITS["arrayItems"] if stack[-1][0] == 91 else LIMITS["objectFields"]), 1)
        elif stack:
            stack[-1][2] = True
    require(not quoted and not stack, 1)


def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= byte_limit(kind) and data.isascii(), 1)
    preparse(data)

    def pairs(items):
        result = {}
        for key, value in items:
            require(key not in result, 1)
            result[key] = value
        return result

    try:
        value = json.loads(data.decode("ascii"), object_pairs_hook=pairs, parse_constant=lambda _: (_ for _ in ()).throw(Invalid(1)))
    except (ValueError, RecursionError, UnicodeError) as error:
        if isinstance(error, Invalid):
            raise
        raise Invalid(1) from error
    require(raw(value, kind) == data, 8)
    return value


OWN = {path: sha(path.read_bytes()) for path in (ROOT / (NAME + ".schema.json"), Path(__file__).resolve())}
PACKAGED_MANIFEST = encode(dict(sorted(DEPENDENCIES.items())))


def dependencies():
    require(encode(dict(sorted(DEPENDENCIES.items()))) == PACKAGED_MANIFEST, 9)
    for name, expected in DEPENDENCIES.items():
        require(type(name) is str and name.isascii() and not name.startswith("/") and "\\" not in name and all(part not in ("", ".", "..") for part in name.split("/")), 9)
        require(type(expected) is str and len(expected) == 64 and all(char in "0123456789abcdef" for char in expected), 9)
        try:
            data = (REPOSITORY / name).read_bytes()
        except OSError as error:
            raise Invalid(9) from error
        require(type(data) is bytes, 9)
        require(hashlib.sha256(data).hexdigest() == expected, 9)
    for path, expected in OWN.items():
        try:
            data = path.read_bytes()
        except OSError as error:
            raise Invalid(9) from error
        require(type(data) is bytes and sha(data) == expected, 9)


ACTION = sha(encode(dict(contractVersion=1, kind=KIND, operationStage=1)))


def source_for(predecessor_source):
    require(type(predecessor_source) is bytes and predecessor_source.isascii(), 1)
    return raw(dict(contractVersion=1, movementCompletionSourceCanonicalUtf8=predecessor_source.decode("ascii"), breakdownEventCanonicalUtf8=[]), "Source")


def derive(predecessor_source, frame, prior):
    cycle = prior["activeCycle"]
    require(cycle["gameTurn"] == 1 and cycle["operationStage"] == 1 and cycle["playerPhaseSlot"] == "first-acting-side" and cycle["ordinal"] == 2, 3)
    require(prior["phase"] == "breakdown" and prior["closed"] and prior["flow"] == dict(kind="idle") and prior["interruptContext"] is None, 3)
    require(prior["movementEnd"] is not None and prior["movementEnd"]["ordinal"] == 2 and prior["originalMovementEnd"]["ordinal"] == 1, 4)
    require(prior["closure"] is not None and prior["closure"]["ordinal"] == 1 and prior["closure"]["outcome"] == "repeated", 4)
    require(prior["movementEnd"]["completionReceiptId"] == prior["lifecycleReceipts"][-1]["receiptId"], 4)
    require(len(prior["lifecycleReceipts"]) == (3 if frame["moveCount"] else 1), 4)
    catalog = parent.mrm.n3.n2.n1.seq.expected_catalog()["positions"]
    boundary = prior["sequencePosition"]
    require(boundary == parent.position(frame, "breakdown"), 3)
    at = next((index for index, position in enumerate(catalog) if position == boundary), -1)
    require(0 <= at < len(catalog) - 1, 4)
    successor = catalog[at + 1]
    require(successor["positionId"].endswith(".combat.position-determination"), 4)
    predecessor_control = parent.raw(prior, "Control")
    predecessor_proof = parent.raw(parent.proof(frame["packet"], frame, prior), "Proof")
    base = dict(contractVersion=1, movementCompletionSourceHash=sha(predecessor_source), movementCompletionControlHash=sha(predecessor_control), movementCompletionProofHash=sha(predecessor_proof), dependencyManifestHash="sha256:" + identity("dependency-manifest", PACKAGED_MANIFEST), movementCompletionReceiptId=prior["movementEnd"]["completionReceiptId"], cycleId=prior["cycleId"], configurationHash=cycle["admittedPolicyBundleDigest"])
    base_hash = "sha256:" + identity("base", raw(base, "Base"))
    require(prior["stateVersion"] < 2**63 - 1, 7)
    return dict(base=base, baseHash=base_hash, prior=copy.deepcopy(prior), predecessorControl=predecessor_control.decode("ascii"), cycle=copy.deepcopy(cycle), boundary=copy.deepcopy(boundary), successor=copy.deepcopy(successor))


def initial(context):
    prior = context["prior"]
    return canonical(dict(contractVersion=1, profile=prior["profile"], baseHash=context["baseHash"], parentSourceHash=context["base"]["movementCompletionSourceHash"], predecessorControlCanonicalUtf8=context["predecessorControl"], stateVersion=prior["stateVersion"], prefix=prior["prefix"], sequencePosition=copy.deepcopy(context["boundary"]), phase="breakdown", activeCycle=copy.deepcopy(context["cycle"]), cycleId=prior["cycleId"], movementCompletionReceiptId=context["base"]["movementCompletionReceiptId"], breakdownCompletionReceiptId=None, receipts=[], closed=False), "Control")


def capability(context, state):
    value = dict(baseHash=context["baseHash"], cycleId=state["cycleId"], kind=KIND, stateVersion=state["stateVersion"], sequencePosition=canonical(state["sequencePosition"], "Position"), movementCompletionReceiptId=state["movementCompletionReceiptId"])
    return "sha256:" + identity("capability", encode(value))


def command(context, state):
    value = dict(contractVersion=1, kind=KIND, actionId=ACTION, baseHash=context["baseHash"], cycleId=state["cycleId"], expectedPositionId=context["boundary"]["positionId"], expectedPriorVersion=state["stateVersion"], capabilityId=capability(context, state))
    return dict(command=value, actor="system")


def transition(context, before, action, admission=True, handlers=None, capacities=None):
    raw(before, "Control")
    raw(action, "Input")
    cmd = action["command"]
    require(cmd["baseHash"] == before["baseHash"] == context["baseHash"] and cmd["cycleId"] == before["cycleId"] == context["prior"]["cycleId"] and cmd["expectedPositionId"] == context["boundary"]["positionId"] and before["activeCycle"] == context["cycle"], 4)
    require(action["actor"] == "system", 4)
    require(cmd["actionId"] == ACTION, 5)
    command_hash = sha(raw(cmd, "Command"))
    old = next((receipt for receipt in before["receipts"] if receipt["commandHash"] == command_hash and receipt["actor"] == "system"), None)
    if old is not None:
        return copy.deepcopy(before), None, old["receiptId"]
    require(cmd["expectedPriorVersion"] == before["stateVersion"] and before["sequencePosition"] == context["boundary"] and before["phase"] == "breakdown", 6)
    require(not before["closed"] and not before["receipts"] and before["breakdownCompletionReceiptId"] is None and before["stateVersion"] < 2**63 - 1, 7)
    require(cmd["capabilityId"] == capability(context, before), 5)
    if admission:
        require(set(handlers if handlers is not None else INVENTORY["supportHandlers"]) >= set(INVENTORY["supportHandlers"]), 7)
        offered = capacities if capacities is not None else LIMITS
        require(all(offered.get(name, -1) >= value for name, value in LIMITS.items()), 7)
    else:
        raise Invalid(7)
    effect = dict(kind="breakdown-completed", movementCompletionReceiptId=before["movementCompletionReceiptId"], sources=copy.deepcopy(context["boundary"]["sources"]), successorPosition=copy.deepcopy(context["successor"]))
    cycle = context["cycle"]
    event = dict(contractVersion=1, eventType="combat-actual-moved-repeat-breakdown-completed", author="system", campaignId=cycle["campaignId"], rulesetHash=cycle["rulesetHash"], configurationHash=cycle["admittedPolicyBundleDigest"], profile=before["profile"], movementCompletionSourceHash=before["parentSourceHash"], baseHash=context["baseHash"], cycleId=before["cycleId"], positionId=before["sequencePosition"]["positionId"], priorVersion=before["stateVersion"], stateVersion=before["stateVersion"] + 1, priorPrefix=before["prefix"], input=canonical(action, "Input"), effect=canonical(effect, "Effect"))
    receipt_id = "mrb." + identity("receipt", encode(event))
    event["receiptId"] = receipt_id
    event_bytes = raw(event, "Event")
    after = copy.deepcopy(before)
    after.update(stateVersion=event["stateVersion"], prefix=parent.mrm.n3.n2.n1.seq.prefix_event(before["prefix"], event_bytes), sequencePosition=copy.deepcopy(context["successor"]), phase="combat", breakdownCompletionReceiptId=receipt_id, closed=True)
    after["receipts"].append(dict(commandHash=command_hash, eventHash=sha(event_bytes), receiptId=receipt_id, actor="system", stateVersion=after["stateVersion"]))
    raw(after, "Control")
    return after, event_bytes, receipt_id


def own_ledger_snapshot(value):
    if type(value) is not list:
        return None
    return [bytes(item) if type(item) is bytes else None for item in value]


def replay(source, arguments, breakdown):
    require(type(source) is bytes and type(arguments) is list and len(arguments) == 11, 1)
    owned_source = bytes(source)
    owned_arguments = copy.deepcopy(arguments)
    owned_breakdown = own_ledger_snapshot(breakdown)
    dependencies()
    packet = parse(owned_source, "Source")
    predecessor_source = packet["movementCompletionSourceCanonicalUtf8"].encode("ascii")
    frame, prior = inherited(lambda: parent.replay(predecessor_source, *owned_arguments))
    context = derive(predecessor_source, frame, prior)
    require(type(owned_breakdown) is list and len(owned_breakdown) <= 1 and all(type(item) is bytes for item in owned_breakdown), 1)
    ledger = [parse(item, "Input") for item in owned_breakdown]
    require(len(encode(ledger)) <= LIMITS["breakdownLedgerBytes"] and len(ledger) == len(packet["breakdownEventCanonicalUtf8"]), 4)
    state = initial(context)
    for recorded, action in zip(packet["breakdownEventCanonicalUtf8"], ledger):
        parse(recorded.encode("ascii"), "Event")
        state, expected, _ = transition(context, state, action)
        require(expected is not None and expected == recorded.encode("ascii"), 6)
    dependencies()
    return context, state, owned_breakdown


def apply(source, arguments, breakdown, action, admission=True, handlers=None, capacities=None):
    require(type(source) is bytes and type(arguments) is list, 1)
    owned_source = bytes(source)
    owned_arguments = copy.deepcopy(arguments)
    owned_breakdown = own_ledger_snapshot(breakdown)
    owned_action = copy.deepcopy(action)
    owned_handlers = copy.deepcopy(handlers)
    owned_capacities = copy.deepcopy(capacities)
    context, state, ledger = replay(owned_source, owned_arguments, owned_breakdown)
    input_bytes = raw(owned_action, "Input")
    after, event, receipt_id = transition(context, state, owned_action, admission, owned_handlers, owned_capacities)
    if event is None:
        return owned_source, ledger, receipt_id
    packet = parse(owned_source, "Source")
    packet["breakdownEventCanonicalUtf8"].append(event.decode("ascii"))
    result = raw(packet, "Source")
    next_ledger = [*ledger, input_bytes]
    _, rebuilt, _ = replay(result, owned_arguments, next_ledger)
    require(after == rebuilt, 6)
    return result, next_ledger, receipt_id


def _proof_candidate(source, context, state):
    digest = "sha256:" + identity("source", source)
    return canonical(dict(contractVersion=1, sourceId="mrbcomp." + digest[7:], sourceHash=digest, base=context["base"], control=state), "Proof")


def proof(source, arguments, breakdown):
    context, state, _ = replay(source, arguments, breakdown)
    require(INVENTORY["closureFrozen"], 7)
    return _proof_candidate(source, context, state)


def read_source(source, arguments, breakdown):
    replay(source, arguments, breakdown)
    require(INVENTORY["closureFrozen"], 7)
    return bytes(source)


def read_control(claim, source, arguments, breakdown):
    context, state, _ = replay(source, arguments, breakdown)
    require(INVENTORY["closureFrozen"], 7)
    require(parse(claim, "Control") == state, 6)
    return copy.deepcopy(state)


def read_proof(claim, source, arguments, breakdown):
    expected = proof(source, arguments, breakdown)
    require(INVENTORY["closureFrozen"], 7)
    require(parse(claim, "Proof") == expected, 6)
    return copy.deepcopy(expected)
