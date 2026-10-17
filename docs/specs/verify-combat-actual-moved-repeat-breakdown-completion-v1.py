#!/usr/bin/env python3
"""Private R03 reference: authentic moved-repeat Breakdown completion."""
import copy
import gzip
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
I = json.loads((ROOT / (NAME + ".schema.json")).read_bytes())
S = {k: [tuple(part.split(":")) for part in value.split()] for k, value in I["objects"].items()}
L = I["limits"]
PINS = I["runtimeSourceHashes"]
spec = importlib.util.spec_from_file_location(
    "r03_parent_r02", ROOT / "verify-combat-actual-moved-repeat-movement-completion-v1.py"
)
r02 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r02)


class Invalid(ValueError):
    def __init__(self, number):
        self.code = f"CMB-MRB-{number:03}"
        super().__init__(self.code)


def require(ok, number):
    if not ok:
        raise Invalid(number)


def encode(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("ascii")


def sha(data):
    return "sha256:" + hashlib.sha256(data).hexdigest()


def domain(kind, data):
    return hashlib.sha256(I["domains"][kind].encode("ascii") + b"\0" + data).hexdigest()


def inherited(call):
    try:
        return call()
    except ValueError as error:
        code = re.search(r"CMB-[A-Z]+-(\d{3})", str(error))
        if code:
            raise Invalid(int(code[1])) from error
        raise


def verify_dependencies():
    require(len(PINS) == len(set(PINS)) and all(
        type(path) is str and path.isascii() and not path.startswith("/")
        and "\\" not in path and all(part not in ("", ".", "..") for part in path.split("/"))
        and type(expected) is str and re.fullmatch(r"[0-9a-f]{64}", expected)
        for path, expected in PINS.items()
    ), 9)
    for path, expected in PINS.items():
        try:
            data = (REPOSITORY / path).read_bytes()
        except OSError as error:
            raise Invalid(9) from error
        require(type(data) is bytes, 9)
        require(hashlib.sha256(data).hexdigest() == expected, 9)
    for path, expected in OWN.items():
        try:
            data = path.read_bytes()
        except OSError as error:
            raise Invalid(9) from error
        require(type(data) is bytes, 9)
        require(sha(data) == expected, 9)


def limit(kind):
    return {
        "Source": L["sourceBytes"], "Proof": L["proofBytes"], "Control": L["controlBytes"],
        "Event": L["eventBytes"], "Input": L["inputBytes"], "Command": L["inputBytes"]
    }.get(kind, L["proofBytes"])


def typed(value, kind, depth=0, parent="", field=""):
    require(depth <= L["depth"], 1)
    if kind.endswith("?"):
        if value is not None:
            typed(value, kind[:-1], depth, parent, field)
    elif kind.endswith("[]"):
        bound = I["arrayBounds"].get(parent + "." + field, L["arrayItems"])
        require(type(value) is list and len(value) <= bound, 1)
        for item in value:
            typed(item, kind[:-2], depth + 1)
    elif kind in S:
        fields = S[kind]
        require(type(value) is dict and set(value) == {name for name, _ in fields}, 1)
        for name, nested in fields:
            typed(value[name], nested, depth + 1, kind, name)
        if kind in I["versionRules"]:
            require(value["contractVersion"] == I["versionRules"][kind], 3)
    elif kind == "R02Proof":
        inherited(lambda: r02.typed(value, "Proof"))
    elif kind in ("parentCarrier", "eventCarrier"):
        cap = L["parentSourceBytes"] if kind == "parentCarrier" else L["eventBytes"]
        require(type(value) is str and value.isascii() and 0 < len(value) <= cap, 1)
    else:
        inherited(lambda: r02.typed(value, kind))


def canonical(value, kind):
    if kind.endswith("?"):
        return None if value is None else canonical(value, kind[:-1])
    if kind.endswith("[]"):
        return [canonical(item, kind[:-2]) for item in value]
    if kind in S:
        return {name: canonical(value[name], nested) for name, nested in S[kind]}
    if kind == "R02Proof":
        return inherited(lambda: r02.canonical(value, "Proof"))
    if kind in ("parentCarrier", "eventCarrier"):
        return value
    return inherited(lambda: r02.canonical(value, kind))


def bounds(value, depth=0):
    require(depth <= L["depth"], 1)
    if type(value) is dict:
        require(len(value) <= L["objectFields"], 1)
        for nested in value.values():
            bounds(nested, depth + 1)
    elif type(value) is list:
        require(len(value) <= L["arrayItems"], 1)
        for nested in value:
            bounds(nested, depth + 1)


def raw(value, kind):
    bounds(value)
    typed(value, kind)
    data = encode(canonical(value, kind))
    require(len(data) <= limit(kind), 1)
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
            require(len(stack) < L["depth"] + 1, 1)
            stack.append([byte, 0, False])
        elif byte in (125, 93):
            require(bool(stack) and (stack[-1][0], byte) in ((123, 125), (91, 93)), 1)
            tag, commas, occupied = stack.pop()
            require(commas + int(occupied) <= (L["arrayItems"] if tag == 91 else L["objectFields"]), 1)
        elif byte == 44 and stack:
            stack[-1][1] += 1
            require(stack[-1][1] < (L["arrayItems"] if stack[-1][0] == 91 else L["objectFields"]), 1)
        elif stack:
            stack[-1][2] = True
    require(not quoted and not stack, 1)


def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= limit(kind) and data.isascii(), 1)
    preparse(data)

    def pairs(items):
        result = {}
        for key, value in items:
            require(key not in result, 1)
            result[key] = value
        return result

    try:
        value = json.loads(data.decode("ascii"), object_pairs_hook=pairs,
                           parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, RecursionError, UnicodeError) as error:
        if isinstance(error, Invalid):
            raise
        raise Invalid(1) from error
    require(raw(value, kind) == data, 8)
    return value


OWN = {path: sha(path.read_bytes()) for path in
       (ROOT / (NAME + ".schema.json"), Path(__file__).resolve())}
MANIFEST = encode(dict(sorted(PINS.items())))
KIND = "complete-breakdown-segment"
ACTION = sha(encode(dict(contractVersion=1, kind=KIND, operationStage=1)))
SEQUENCE = r02.mrm.n3.n2.n1.seq


def derive(frame, state, completion):
    cycle = state["activeCycle"]
    require(state["profile"] in ("paid", "armed") and cycle["gameTurn"] == 1
            and cycle["operationStage"] == 1 and cycle["playerPhaseSlot"] == "first-acting-side"
            and cycle["ordinal"] == 2, 3)
    require(state["phase"] == "breakdown" and state["closed"]
            and state["flow"] == dict(kind="idle") and state["interruptContext"] is None
            and state["movementEnd"] is not None, 3)
    require(state["movementEnd"]["ordinal"] == 2
            and state["originalMovementEnd"]["ordinal"] == 1
            and state["movementEnd"]["completionReceiptId"] == state["lifecycleReceipts"][-1]["receiptId"]
            and len(completion) == (3 if state["tracks"] else 1), 4)
    catalog = SEQUENCE.expected_catalog()["positions"]
    boundary = state["sequencePosition"]
    index = next((i for i, position in enumerate(catalog) if position == boundary), None)
    require(index is not None and index + 1 < len(catalog), 4)
    successor = catalog[index + 1]
    require(successor["positionId"].endswith(".combat.position-determination"), 4)
    parent_proof = r02.proof(frame["packet"], frame, state)
    inherited(lambda: r02.raw(parent_proof, "Proof"))
    base = dict(contractVersion=1,
                dependencyManifestHash="sha256:" + domain("dependency-manifest", MANIFEST),
                movedRepeatMovementCompletionProof=parent_proof)
    raw(base, "Base")
    require(state["stateVersion"] < 2**63 - 1, 7)
    return dict(parent=copy.deepcopy(state), base=base, baseHash="sha256:" + domain("base", raw(base, "Base")),
                sourceHash=sha(r02.source_bytes(frame["packet"])), boundary=copy.deepcopy(boundary),
                successor=copy.deepcopy(successor), cycle=copy.deepcopy(cycle), cycleId=state["cycleId"])


def initial(context):
    state = copy.deepcopy(context["parent"])
    state["r02BaseHash"] = state["baseHash"]
    state["r02SourceHash"] = context["sourceHash"]
    state["baseHash"] = context["baseHash"]
    state["breakdownReceipts"] = []
    state["breakdownCompletionReceiptId"] = None
    state["closed"] = False
    return canonical(state, "Control")


def capability(context, state):
    binding = dict(baseHash=context["baseHash"], cycleId=context["cycleId"], kind=KIND,
                   stateVersion=state["stateVersion"], sequencePosition=canonical(state["sequencePosition"], "Position"),
                   movementCompletionReceiptId=state["movementEnd"]["completionReceiptId"])
    return "sha256:" + domain("capability", encode(binding))


def command(context, state):
    return dict(command=dict(contractVersion=1, kind=KIND, operationStage=1, actionId=ACTION,
                             baseHash=context["baseHash"], cycleId=context["cycleId"],
                             expectedPositionId=context["boundary"]["positionId"],
                             expectedPriorVersion=state["stateVersion"], capabilityId=capability(context, state)),
                actor="system")


def transition(context, prior, input_value, admission=True):
    raw(prior, "Control")
    raw(input_value, "Input")
    cmd = input_value["command"]
    require(cmd["kind"] == KIND and cmd["operationStage"] == 1, 3)
    require(cmd["baseHash"] == prior["baseHash"] == context["baseHash"]
            and cmd["cycleId"] == prior["cycleId"] == context["cycleId"]
            and cmd["expectedPositionId"] == context["boundary"]["positionId"]
            and prior["activeCycle"] == context["cycle"], 4)
    require(input_value["actor"] == "system", 4)
    require(cmd["actionId"] == ACTION, 5)
    command_hash = sha(raw(cmd, "Command"))
    old = next((receipt for receipt in prior["breakdownReceipts"]
                if receipt["commandHash"] == command_hash and receipt["actor"] == "system"), None)
    if old:
        return copy.deepcopy(prior), None, old["receiptId"]
    require(admission and not prior["closed"] and not prior["breakdownReceipts"]
            and prior["stateVersion"] < 2**63 - 1, 7)
    require(cmd["expectedPriorVersion"] == prior["stateVersion"]
            and prior["sequencePosition"] == context["boundary"]
            and prior["flow"] == dict(kind="idle") and prior["interruptContext"] is None
            and prior["breakdownCompletionReceiptId"] is None, 6)
    require(cmd["capabilityId"] == capability(context, prior), 5)
    effect = dict(kind="breakdown-completed",
                  movementCompletionReceiptId=prior["movementEnd"]["completionReceiptId"],
                  sources=copy.deepcopy(context["boundary"]["sources"]),
                  successorPosition=copy.deepcopy(context["successor"]))
    cycle = context["cycle"]
    event = dict(contractVersion=1, eventType="combat-actual-moved-repeat-breakdown-completed",
                 author="system", campaignId=cycle["campaignId"], rulesetHash=cycle["rulesetHash"],
                 configurationHash=cycle["admittedPolicyBundleDigest"], profile=prior["profile"],
                 movementCompletionSourceHash=prior["r02SourceHash"], baseHash=context["baseHash"],
                 cycleId=context["cycleId"], positionId=prior["sequencePosition"]["positionId"],
                 priorVersion=prior["stateVersion"], stateVersion=prior["stateVersion"] + 1,
                 priorPrefix=prior["prefix"], input=canonical(input_value, "Input"),
                 effect=canonical(effect, "Effect"))
    receipt_id = "mrbc." + domain("receipt", encode(event))
    event["receiptId"] = receipt_id
    event_bytes = raw(event, "Event")
    after = copy.deepcopy(prior)
    after["stateVersion"] = event["stateVersion"]
    after["prefix"] = SEQUENCE.prefix_event(prior["prefix"], event_bytes)
    after["sequencePosition"] = copy.deepcopy(context["successor"])
    after["breakdownCompletionReceiptId"] = receipt_id
    after["closed"] = True
    after["breakdownReceipts"].append(dict(commandHash=command_hash, eventHash=sha(event_bytes),
                                            receiptId=receipt_id, actor="system",
                                            stateVersion=after["stateVersion"]))
    raw(after, "Control")
    return after, event_bytes, receipt_id


def replay(source_bytes, parent_args, own_ledger):
    verify_dependencies()
    source = parse(bytes(source_bytes), "Source")
    require(type(parent_args) is list and len(parent_args) == 11, 1)
    parent_args = copy.deepcopy(parent_args)
    frame, state = inherited(lambda: r02.replay(
        source["movementCompletionSourceCanonicalUtf8"].encode("ascii"), *parent_args))
    context = derive(frame, state, parent_args[-1])
    require(type(own_ledger) is list and len(own_ledger) <= 1
            and all(type(item) is bytes for item in own_ledger), 1)
    owned = [parse(bytes(item), "Input") for item in own_ledger]
    require(len(encode(owned)) <= L["breakdownLedgerBytes"], 1)
    require(len(owned) == len(source["breakdownEventCanonicalUtf8"]), 4)
    current = initial(context)
    raw(current, "Control")
    for actual, input_value in zip(source["breakdownEventCanonicalUtf8"], owned):
        event = parse(actual.encode("ascii"), "Event")
        current, expected, _ = transition(context, current, input_value)
        require(expected is not None and raw(event, "Event") == expected, 6)
    verify_dependencies()
    return source, context, current


def apply(source_bytes, parent_args, own_ledger, input_bytes, admission=True):
    source, context, state = replay(source_bytes, parent_args, own_ledger)
    input_value = parse(bytes(input_bytes), "Input")
    after, event, receipt = transition(context, state, input_value, admission)
    if event is None:
        return raw(source, "Source"), copy.deepcopy(own_ledger), None, receipt
    source["breakdownEventCanonicalUtf8"].append(event.decode("ascii"))
    ledger = [*own_ledger, bytes(input_bytes)]
    require(len(encode([parse(item, "Input") for item in ledger])) <= L["breakdownLedgerBytes"], 1)
    result = raw(source, "Source")
    _, _, rebuilt = replay(result, parent_args, ledger)
    require(after == rebuilt, 6)
    return result, ledger, event, receipt


def proof(source_bytes, parent_args, own_ledger):
    source, context, state = replay(source_bytes, parent_args, own_ledger)
    data = raw(source, "Source")
    output = dict(contractVersion=1, sourceId="mrbcomp." + domain("source", data),
                  sourceHash=sha(data), base=copy.deepcopy(context["base"]),
                  control=copy.deepcopy(state))
    return parse(raw(output, "Proof"), "Proof")


def read_control(claim, source_bytes, parent_args, own_ledger):
    _, _, state = replay(source_bytes, parent_args, own_ledger)
    require(parse(claim, "Control") == state, 6)
    return state


def read_proof(claim, source_bytes, parent_args, own_ledger):
    expected = proof(source_bytes, parent_args, own_ledger)
    require(parse(claim, "Proof") == expected, 6)
    return expected


def source_for(parent_source):
    return raw(dict(contractVersion=1, movementCompletionSourceCanonicalUtf8=parent_source.decode("ascii"),
                    breakdownEventCanonicalUtf8=[]), "Source")


def parent_terminal(row):
    packet, args = r02.root(row)
    source = r02.source_bytes(packet)
    frame, state = r02.replay(source, *args, [])
    ledger = []
    for kind in (r02.I["commands"] if row["movementCut"] > 1 else ["complete-movement-segment"]):
        input_bytes = r02.raw(r02.command(frame, state, kind), "Input")
        source, state, event, _ = r02.apply(source, args, ledger, input_bytes)
        require(event is not None, 6)
        ledger.append(input_bytes)
    require(state["closed"] and state["phase"] == "breakdown", 4)
    return source, [*args, ledger]


def trace(row, full=False):
    parent_source, parent_args = parent_terminal(row)
    source = source_for(parent_source)
    _, context, before = replay(source, parent_args, [])
    input_bytes = raw(command(context, before), "Input")
    after_source, ledger, event, receipt = apply(source, parent_args, [], input_bytes)
    require(event is not None and len(ledger) == 1, 6)
    _, _, after = replay(after_source, parent_args, ledger)
    require(after["breakdownCompletionReceiptId"] == receipt and after["closed"]
            and after["activeCycle"]["ordinal"] == 2, 6)
    retained = ("world", "randomState", "originalMovementEnd", "movementEnd",
                "priorCycleProgress", "actualProgressRefs", "members", "attackHistory",
                "priorCycleTargetUses", "targetUses", "assessment", "pendingRepeatIntent",
                "pendingIntentReceiptId", "publicationReceiptId", "originalCycle",
                "activeCycle", "closure", "tracks", "movementReceipts", "lifecycleReceipts")
    require(all(after[key] == before[key] for key in retained), 6)
    retry = apply(after_source, parent_args, ledger, input_bytes, False)
    require(retry == (after_source, ledger, None, receipt), 6)
    result = dict(rowKey=row["rowKey"], rootOrdinal=row["rootOrdinal"], movementCut=row["movementCut"],
                profile=row["profile"], parentSourceHash=sha(parent_source),
                sourceHash=sha(after_source), eventHash=sha(event),
                beforeControlHash=sha(raw(before, "Control")),
                afterControlHash=sha(raw(after, "Control")),
                beforeProofHash=sha(raw(proof(source, parent_args, []), "Proof")),
                afterProofHash=sha(raw(proof(after_source, parent_args, ledger), "Proof")),
                receiptId=receipt)
    if full:
        def ascii_tree(value):
            if type(value) is bytes:
                return value.decode("ascii")
            if type(value) is list:
                return [ascii_tree(item) for item in value]
            if type(value) is dict:
                return {key: ascii_tree(item) for key, item in value.items()}
            return value
        result["vectors"] = dict(parentSourceCanonicalUtf8=parent_source.decode("ascii"),
                                 parentInputLedgersCanonicalUtf8=ascii_tree(parent_args),
                                 inputCanonicalUtf8=input_bytes.decode("ascii"),
                                 eventCanonicalUtf8=event.decode("ascii"),
                                 sourceCanonicalUtf8=after_source.decode("ascii"),
                                 beforeControlCanonicalUtf8=raw(before, "Control").decode("ascii"),
                                 afterControlCanonicalUtf8=raw(after, "Control").decode("ascii"),
                                 beforeProofCanonicalUtf8=raw(proof(source, parent_args, []), "Proof").decode("ascii"),
                                 afterProofCanonicalUtf8=raw(proof(after_source, parent_args, ledger), "Proof").decode("ascii"))
    return result


def verify(rows=None, progress=None, full=False):
    all_rows = r02.semantic_rows()
    require(len(all_rows) == 300 and sum(x["profile"] == "paid" for x in all_rows) == 36
            and sum(x["profile"] == "armed" for x in all_rows) == 264, 6)
    chosen = all_rows if rows is None else [all_rows[index] for index in rows]
    results = []
    for index, row in enumerate(chosen, 1):
        result = trace(row, full)
        results.append(result)
        if progress is not None:
            progress(index, len(chosen), result)
    return results


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--rows", nargs="*", type=int)
    parser.add_argument("--progress", action="store_true", help="print each completed authentic row")
    parser.add_argument("--evidence-gzip", type=Path,
                        help="append full canonical vectors as gzip JSONL, one member per completed row")
    options = parser.parse_args()
    def emit(count, total, row):
        if options.evidence_gzip:
            with gzip.open(options.evidence_gzip, "at", encoding="ascii") as stream:
                stream.write(json.dumps(row, ensure_ascii=True, separators=(",", ":")) + "\n")
        if options.progress:
            print("VERIFIED", count, "/", total, row["rowKey"], flush=True)
    results = verify(options.rows, emit if options.progress or options.evidence_gzip else None,
                     full=bool(options.evidence_gzip))
    print("PASS", len(results), "authentic R03 traces")
