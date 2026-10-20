#!/usr/bin/env python3
"""Verify proposed A2 schema, canonical goldens and selected invariants.

This is a proposal verifier, not a runtime authority or admission proof.
"""

from __future__ import annotations

import copy
import hashlib
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parent
SCHEMA = ROOT / "combat-p01-a2-private-owner-contract-v1.schema.json"
GOLDENS = ROOT / "fixtures/combat-p01-a2-private-owner-contract-v1.json"
OWNER_GOLDENS = ROOT / "fixtures/combat-p01-owner-decision-proposal-v1.json"
KINDS = (
    "importBinding", "seatGrant", "actorFacts", "ownerOpening",
    "privateOpeningPayload", "ownerInput", "journalRecord", "ownerReceipt",
    "retryBinding",
)
CAPS = {
    "importBinding": 1024, "seatGrant": 1024, "actorFacts": 1536,
    "ownerOpening": 4096, "privateOpeningPayload": 8192,
    "ownerInput": 1024, "journalRecord": 2048,
    "ownerReceipt": 1024, "retryBinding": 1024,
}


def no_duplicate_keys(pairs: list[tuple[str, object]]) -> dict[str, object]:
    result: dict[str, object] = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate key: {key}")
        result[key] = value
    return result


def parse(raw: bytes) -> object:
    if len(raw) > 65536:
        raise ValueError("JSON document cap")
    return json.loads(raw.decode("ascii"), object_pairs_hook=no_duplicate_keys)


def resolve(schema: dict, spec: dict) -> dict:
    if "$ref" not in spec:
        return spec
    name = spec["$ref"].removeprefix("#/$defs/")
    return schema["$defs"][name]


def validate(schema: dict, spec: dict, value: object, path: str = "$") -> None:
    spec = resolve(schema, spec)
    if "anyOf" in spec:
        for arm in spec["anyOf"]:
            try:
                validate(schema, arm, value, path)
                return
            except ValueError:
                pass
        raise ValueError(f"{path}: no union arm")
    if "const" in spec and (type(value) is not type(spec["const"]) or value != spec["const"]):
        raise ValueError(f"{path}: constant")
    if "enum" in spec and not any(type(value) is type(item) and value == item for item in spec["enum"]):
        raise ValueError(f"{path}: enum")
    for condition in spec.get("allOf", []):
        if not isinstance(value, dict):
            raise ValueError(f"{path}: conditional object")
        matched = True
        for field, field_spec in condition["if"]["properties"].items():
            try:
                validate(schema, field_spec, value[field], f"{path}.{field}")
            except (KeyError, ValueError):
                matched = False
        branch = condition.get("then" if matched else "else", {})
        for field, field_spec in branch.get("properties", {}).items():
            validate(schema, field_spec, value[field], f"{path}.{field}")
    typ = spec.get("type")
    if typ == "object":
        if not isinstance(value, dict):
            raise ValueError(f"{path}: object")
        fields = spec["x-field-order"]
        if list(value) != fields or set(spec["required"]) != set(fields):
            raise ValueError(f"{path}: missing, unknown or reordered field")
        if spec.get("additionalProperties") is not False:
            raise ValueError(f"{path}: schema must be closed")
        for field in fields:
            validate(schema, spec["properties"][field], value[field], f"{path}.{field}")
    elif typ == "array":
        if not isinstance(value, list):
            raise ValueError(f"{path}: array")
        if not spec.get("minItems", 0) <= len(value) <= spec.get("maxItems", 2**63):
            raise ValueError(f"{path}: array cap")
        if spec.get("uniqueItems") and len({json.dumps(v, sort_keys=True) for v in value}) != len(value):
            raise ValueError(f"{path}: duplicate array member")
        for i, item in enumerate(value):
            validate(schema, spec["items"], item, f"{path}[{i}]")
    elif typ == "string":
        if not isinstance(value, str) or not value.isascii():
            raise ValueError(f"{path}: ASCII string")
        if len(value) > spec.get("maxLength", 2**63):
            raise ValueError(f"{path}: string cap")
        if "pattern" in spec and not re.fullmatch(spec["pattern"], value):
            raise ValueError(f"{path}: string form")
    elif typ == "integer":
        if type(value) is not int:
            raise ValueError(f"{path}: integer")
        if not spec.get("minimum", -(2**63)) <= value <= spec.get("maximum", 2**63 - 1):
            raise ValueError(f"{path}: integer range")
    elif typ == "boolean" and type(value) is not bool:
        raise ValueError(f"{path}: boolean")
    elif typ == "null" and value is not None:
        raise ValueError(f"{path}: null")


def canonical(schema: dict, kind: str, value: object) -> bytes:
    validate(schema, schema["$defs"][kind], value)
    raw = json.dumps(value, ensure_ascii=True, separators=(",", ":"), allow_nan=False).encode("ascii")
    if len(raw) > CAPS[kind]:
        raise ValueError(f"{kind}: canonical byte cap")
    return raw


def digest(kind: str, raw: bytes) -> str:
    domain = f"sandtable.combat.p01.a2.{kind}.v1".encode("ascii")
    return "sha256:" + hashlib.sha256(domain + b"\0" + raw).hexdigest()


def check_chain_fields(bind: dict, checkpoint: dict, opening_record: dict, record: dict) -> None:
    if any(row["campaignId"] != bind["campaignId"] for row in (checkpoint, opening_record)):
        raise ValueError("checkpoint/opening campaign mismatch")
    if checkpoint["privatePayloadDigest"] != bind["checkpointHash"]:
        raise ValueError("checkpoint import binding mismatch")
    if (
        checkpoint["resultStateVersion"] != opening_record["priorStateVersion"]
        or checkpoint["resultPrefix"] != opening_record["priorPrefix"]
    ):
        raise ValueError("checkpoint-to-opening game state mismatch")
    if (
        opening_record["resultStateVersion"] != record["priorStateVersion"]
        or opening_record["resultPrefix"] != record["priorPrefix"]
    ):
        raise ValueError("opening-to-input game state mismatch")


def check_case(schema: dict, case: dict) -> None:
    values, expected = case["values"], case["expected"]
    if tuple(values) != KINDS:
        raise ValueError("case kind set/order")
    for kind, value in values.items():
        raw = canonical(schema, kind, value)
        if digest(kind, raw) != expected[kind]["digest"] or len(raw) != expected[kind]["bytes"]:
            raise ValueError(f"{kind}: frozen vector mismatch")
        if parse(raw) != value or canonical(schema, kind, parse(raw)) != raw:
            raise ValueError(f"{kind}: canonical round trip")

    prior_records = case["priorRecords"]
    if len(prior_records) != 2:
        raise ValueError("first opening requires checkpoint and metadata records")
    for sequence, row in enumerate(prior_records):
        record = row["value"]
        raw = canonical(schema, "journalRecord", record)
        if len(raw) != row["bytes"] or digest("journalRecord", raw) != row["digest"]:
            raise ValueError(f"prior record {sequence}: frozen vector mismatch")
        if record["sequence"] != sequence:
            raise ValueError("prior record sequence")
    checkpoint, opening_record = (row["value"] for row in prior_records)
    if checkpoint["kind"] != "r03-import-checkpoint" or opening_record["kind"] != "owner-opening":
        raise ValueError("prior record kind")
    if opening_record["priorRecordHash"] != prior_records[0]["digest"]:
        raise ValueError("opening record chain")
    if checkpoint["seatAuthorityId"] is not None or checkpoint["seatEpoch"] is not None:
        raise ValueError("checkpoint has seat authority")
    if any(checkpoint[field] is not None for field in ("openingRevision", "requestDigest")):
        raise ValueError("checkpoint has owner opening")
    if opening_record["resultStateVersion"] != opening_record["priorStateVersion"] or opening_record["resultPrefix"] != opening_record["priorPrefix"]:
        raise ValueError("opening metadata changed game state")
    if any(opening_record[field] is not None for field in ("requestDigest", "inputDigest", "eventDigest", "receiptDigest")):
        raise ValueError("opening metadata contains game input/event")

    bind, grant, actor, opening, payload, command, record, receipt, retry = values.values()
    check_chain_fields(bind, checkpoint, opening_record, record)
    approved_owner_cases = parse(OWNER_GOLDENS.read_bytes())["cases"]
    owner_case = next(
        (row for row in approved_owner_cases if row["expected"]["decisionRef"] == opening["decisionRef"]),
        None,
    )
    if owner_case is None:
        raise ValueError("opening not anchored to approved owner codec golden")
    seed = owner_case["decisionSeed"]
    if (
        opening["context"] != seed["context"]
        or opening["cycleRef"] != seed["cycleRef"]
        or opening["positionId"] != seed["positionId"]
        or opening["participantRef"] != seed["participantRef"]
        or opening["targetRef"] != owner_case["expected"]["targetRef"]
        or opening["candidateRefs"] != [owner_case["expected"]["alias"]]
    ):
        raise ValueError("opening changed owner-approved vector")
    if payload["visibleOpening"] != opening:
        raise ValueError("private payload lost exact visible opening")
    if payload["campaignId"] != bind["campaignId"] or payload["seatAuthorityId"] != grant["seatAuthorityId"]:
        raise ValueError("private payload authority mismatch")
    if payload["seatEpoch"] != grant["revocationEpoch"] or payload["openingRevision"] != opening["openingRevision"]:
        raise ValueError("private payload opening fence mismatch")
    if [row["ownerCandidateRef"] for row in payload["candidateBindings"]] != opening["candidateRefs"]:
        raise ValueError("private mapping does not match offered aliases")
    if len({row["privateCandidateId"] for row in payload["candidateBindings"]}) != len(payload["candidateBindings"]):
        raise ValueError("ambiguous private candidate mapping")
    campaign = bind["campaignId"]
    if any(row["campaignId"] != campaign for row in (grant, actor, record, retry)):
        raise ValueError("campaign mismatch")
    if bind["journalKey"] != actor["journalKey"]:
        raise ValueError("journal key mismatch")
    if any(row["seatAuthorityId"] != grant["seatAuthorityId"] for row in (actor, record, retry)):
        raise ValueError("seat authority mismatch")
    if grant["side"] != actor["side"] or opening["context"]["audience"] != grant["side"]:
        raise ValueError("seat side mismatch")
    if actor["seatEpoch"] != grant["revocationEpoch"] or record["seatEpoch"] != actor["seatEpoch"]:
        raise ValueError("seat epoch mismatch")
    if actor["grantGeneration"] != grant["grantGeneration"]:
        raise ValueError("grant generation mismatch")
    if not grant["issuedAtUtc"] <= actor["clockAtUtc"] < grant["expiresAtUtc"]:
        raise ValueError("grant clock window")
    if opening["context"]["campaignId"] != campaign:
        raise ValueError("visible campaign mismatch")
    if opening["decisionRef"] != command["decisionRef"] or opening["decisionRef"] != receipt["decisionRef"]:
        raise ValueError("decision ref mismatch")
    if opening["openingRevision"] != command["openingRevision"] or opening["openingRevision"] != receipt["openingRevision"]:
        raise ValueError("opening revision mismatch")
    if command["choiceKind"] == "select":
        if command["candidateRef"] not in opening["candidateRefs"]:
            raise ValueError("candidate not offered")
    elif command["candidateRef"] is not None:
        raise ValueError("decline has candidate")
    if command["requestId"] != receipt["requestId"] or command["requestId"] != retry["requestId"]:
        raise ValueError("request ID mismatch")
    if retry["inputDigest"] != expected["ownerInput"]["digest"]:
        raise ValueError("retry input digest mismatch")
    if retry["receiptDigest"] != expected["ownerReceipt"]["digest"]:
        raise ValueError("retry receipt digest mismatch")
    if record["requestDigest"] != expected["ownerInput"]["digest"]:
        raise ValueError("record request digest mismatch")
    if record["receiptDigest"] != expected["ownerReceipt"]["digest"]:
        raise ValueError("record receipt digest mismatch")
    if record["sequence"] != retry["recordSequence"] or actor["expectedSequence"] + 1 != record["sequence"]:
        raise ValueError("sequence mismatch")
    if actor["expectedRecordHash"] != record["priorRecordHash"]:
        raise ValueError("head hash mismatch")
    if actor["expectedRecordHash"] != prior_records[1]["digest"]:
        raise ValueError("head does not bind opening record")
    if actor["expectedSequence"] != opening_record["sequence"]:
        raise ValueError("expected sequence mismatch")
    if opening_record["privatePayloadDigest"] != expected["privateOpeningPayload"]["digest"]:
        raise ValueError("opening payload mismatch")
    if opening_record["openingRevision"] != opening["openingRevision"]:
        raise ValueError("opening revision record mismatch")
    if opening_record["seatAuthorityId"] != grant["seatAuthorityId"] or opening_record["seatEpoch"] != actor["seatEpoch"]:
        raise ValueError("opening seat mismatch")
    if actor["expectedStateVersion"] != record["priorStateVersion"] or actor["expectedPrefix"] != record["priorPrefix"]:
        raise ValueError("game head mismatch")
    if record["kind"] != "owner-accepted-input" or receipt["resultClass"] != "accepted":
        raise ValueError("golden requires accepted owner input")


def check_negatives(schema: dict, case: dict) -> None:
    value = case["values"]["ownerInput"]
    for mutation in (
        {"extra": "bad"},
        {"contractVersion": 2},
        {"requestId": "req." + "a" * 1000},
        {"candidateRef": None},
    ):
        changed = copy.deepcopy(value)
        changed.update(mutation)
        try:
            canonical(schema, "ownerInput", changed)
        except ValueError:
            pass
        else:
            raise AssertionError(f"negative unexpectedly valid: {mutation}")
    declined = copy.deepcopy(value)
    declined["choiceKind"] = "decline"
    declined["candidateRef"] = None
    canonical(schema, "ownerInput", declined)
    declined["candidateRef"] = value["candidateRef"]
    try:
        canonical(schema, "ownerInput", declined)
    except ValueError:
        pass
    else:
        raise AssertionError("decline with candidate accepted")
    opening_record = copy.deepcopy(case["priorRecords"][1]["value"])
    opening_record["eventDigest"] = "sha256:" + "a" * 64
    try:
        canonical(schema, "journalRecord", opening_record)
    except ValueError:
        pass
    else:
        raise AssertionError("metadata opening emitted game event")
    opening = case["values"]["ownerOpening"]
    for field, wrong in (("operationStage", 0), ("operationStage", 4), ("playerPhaseSlot", "arbitrary-slot")):
        changed = copy.deepcopy(opening)
        changed["context"]["cycle"][field] = wrong
        try:
            canonical(schema, "ownerOpening", changed)
        except ValueError:
            pass
        else:
            raise AssertionError(f"bad cycle accepted: {field}={wrong}")
    bool_version = copy.deepcopy(value)
    bool_version["contractVersion"] = True
    try:
        canonical(schema, "ownerInput", bool_version)
    except ValueError:
        pass
    else:
        raise AssertionError("boolean accepted as contract version one")
    payload = copy.deepcopy(case["values"]["privateOpeningPayload"])
    payload["candidateBindings"][0]["privateCandidateId"] = "p01a." + "0" * 64
    if digest("privateOpeningPayload", canonical(schema, "privateOpeningPayload", payload)) == case["expected"]["privateOpeningPayload"]["digest"]:
        raise AssertionError("changed private mapping retained opening digest")
    bind = case["values"]["importBinding"]
    checkpoint = case["priorRecords"][0]["value"]
    opening_record = case["priorRecords"][1]["value"]
    accepted = case["values"]["journalRecord"]
    changed_checkpoint = copy.deepcopy(checkpoint)
    changed_checkpoint["campaignId"] = "different-campaign"
    try:
        check_chain_fields(bind, changed_checkpoint, opening_record, accepted)
    except ValueError:
        pass
    else:
        raise AssertionError("checkpoint campaign drift accepted")
    changed_checkpoint = copy.deepcopy(checkpoint)
    changed_checkpoint["resultStateVersion"] += 1
    try:
        check_chain_fields(bind, changed_checkpoint, opening_record, accepted)
    except ValueError:
        pass
    else:
        raise AssertionError("checkpoint result version drift accepted")
    changed_checkpoint = copy.deepcopy(checkpoint)
    changed_checkpoint["resultPrefix"] = "sha256:" + "0" * 64
    try:
        check_chain_fields(bind, changed_checkpoint, opening_record, accepted)
    except ValueError:
        pass
    else:
        raise AssertionError("checkpoint result prefix drift accepted")
    raw = canonical(schema, "ownerInput", value)
    duplicate = raw.replace(b'"requestId":', b'"requestId":"req.' + b'0' * 64 + b'","requestId":', 1)
    for bad in (raw + b" ", duplicate):
        try:
            parsed = parse(bad)
            if canonical(schema, "ownerInput", parsed) == bad:
                raise AssertionError("noncanonical/duplicate JSON accepted")
        except (ValueError, UnicodeError):
            pass


def main() -> None:
    schema = parse(SCHEMA.read_bytes())
    goldens = parse(GOLDENS.read_bytes())
    if goldens["contract"] != schema["$id"] or goldens["status"] != "synthetic proposal only; no authenticated owner path":
        raise ValueError("fixture status")
    for case in goldens["cases"]:
        check_case(schema, case)
        check_negatives(schema, case)
    print(f"A2 private contract proposal: {len(goldens['cases'])} golden case(s), {len(KINDS)} closed shapes and negatives pass")


if __name__ == "__main__":
    main()
