#!/usr/bin/env python3
"""Verify proposed A2 schema, canonical goldens and selected invariants.

This is a proposal verifier, not a runtime authority or admission proof.
"""

from __future__ import annotations

import copy
from datetime import datetime, timezone
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
    "acceptedTransitionPayload", "retryBinding", "retryAuthFacts",
)
CAPS = {
    "importBinding": 1024, "seatGrant": 1024, "actorFacts": 1536,
    "ownerOpening": 4096, "privateOpeningPayload": 8192,
    "ownerInput": 1024, "journalRecord": 2048,
    "ownerReceipt": 1024, "acceptedTransitionPayload": 65536,
    "retryBinding": 1024,
    "retryAuthFacts": 1536,
    "coreInputV1": 8192, "coreEventV1": 8192, "coreReceiptV1": 8192,
}
CORE_KINDS = {"input": "coreInputV1", "event": "coreEventV1", "receipt": "coreReceiptV1"}


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
        if len(value) < spec.get("minLength", 0):
            raise ValueError(f"{path}: string minimum")
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


def decode_core(schema: dict, kind: str, hex_bytes: str) -> dict:
    if kind not in CORE_KINDS:
        raise ValueError("unknown Core byte kind")
    if not re.fullmatch(r"(?:[0-9a-f]{2})+", hex_bytes) or len(hex_bytes) > 16384:
        raise ValueError("Core byte encoding/cap")
    raw = bytes.fromhex(hex_bytes)
    value = parse(raw)
    if canonical(schema, CORE_KINDS[kind], value) != raw:
        raise ValueError("Core bytes not typed canonical v1")
    return value


def core_digest(schema: dict, kind: str, hex_bytes: str) -> str:
    decode_core(schema, kind, hex_bytes)
    domain = f"sandtable.combat.p01.a2.core-{kind}.v1".encode("ascii")
    return "sha256:" + hashlib.sha256(domain + b"\0" + bytes.fromhex(hex_bytes)).hexdigest()


def utc(value: str) -> datetime:
    return datetime.strptime(value, "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)


def check_clock(grant: dict, actor: dict, opening_payload: dict) -> None:
    if actor["clockAvailable"] is not True:
        raise ValueError("trusted clock unavailable for owner acceptance")
    now = utc(actor["clockAtUtc"])
    if not utc(grant["issuedAtUtc"]) <= now < utc(grant["expiresAtUtc"]):
        raise ValueError("grant clock window")
    if now >= utc(opening_payload["deadlineAtUtc"]):
        raise ValueError("owner opening deadline passed")


def check_acceptance_clock(grant: dict, actor: dict, opening_payload: dict, record: dict, accepted_payload: dict) -> None:
    if actor["clockAvailable"] is not True:
        raise ValueError("trusted acceptance clock unavailable")
    if record["acceptanceClockSource"] != "local-store-transaction-utc-v1":
        raise ValueError("untrusted acceptance clock source")
    if (
        accepted_payload["acceptanceClockSource"] != record["acceptanceClockSource"]
        or accepted_payload["acceptedAtUnixMilliseconds"] != record["acceptedAtUnixMilliseconds"]
    ):
        raise ValueError("acceptance timestamp readback mismatch")
    accepted = record["acceptedAtUnixMilliseconds"]
    to_ms = lambda value: int(utc(value).timestamp()) * 1000
    if accepted < to_ms(actor["clockAtUtc"]):
        raise ValueError("acceptance precedes trusted preflight")
    if not to_ms(grant["issuedAtUtc"]) <= accepted < to_ms(grant["expiresAtUtc"]):
        raise ValueError("acceptance outside grant window")
    if accepted >= to_ms(opening_payload["deadlineAtUtc"]):
        raise ValueError("acceptance past persisted opening deadline")


def check_current_retry_auth(current: dict, historical_grant: dict, record: dict, campaign: str) -> None:
    if current["authenticated"] is not True or current["clockAvailable"] is not True:
        raise ValueError("current retry authentication unavailable")
    if (
        current["campaignId"] != campaign
        or current["side"] != historical_grant["side"]
        or current["seatAuthorityId"] != record["seatAuthorityId"]
    ):
        raise ValueError("current retry seat authority mismatch")
    if (
        current["seatEpoch"] != current["storeRevocationEpoch"]
        or current["seatEpoch"] < record["seatEpoch"]
        or current["grantGeneration"] < historical_grant["grantGeneration"]
    ):
        raise ValueError("current retry grant generation rollback")
    now = utc(current["clockAtUtc"])
    if not utc(current["grantIssuedAtUtc"]) <= now < utc(current["grantExpiresAtUtc"]):
        raise ValueError("current retry grant clock window")


def check_terminal_history(records: list[dict]) -> None:
    if len(records) != 3:
        raise ValueError("v1 journal must contain exactly three records")
    if [(row["sequence"], row["kind"]) for row in records] != [
        (0, "r03-import-checkpoint"), (1, "owner-opening"), (2, "owner-accepted-input")
    ]:
        raise ValueError("v1 journal order or terminal kind")


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
    if any(row["acceptedAtUnixMilliseconds"] is not None or row["acceptanceClockSource"] is not None for row in (checkpoint, opening_record)):
        raise ValueError("nonaccepted record carries acceptance clock")

    bind, grant, actor, opening, payload, command, record, receipt, accepted_payload, retry, current_retry = values.values()
    check_terminal_history([checkpoint, opening_record, record])
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
    check_clock(grant, actor, payload)
    check_acceptance_clock(grant, actor, payload, record, accepted_payload)
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
    if (
        accepted_payload["campaignId"] != campaign
        or accepted_payload["seatAuthorityId"] != grant["seatAuthorityId"]
        or accepted_payload["seatEpoch"] != actor["seatEpoch"]
        or accepted_payload["openingRecordHash"] != prior_records[1]["digest"]
        or accepted_payload["privateOpeningPayloadHash"] != expected["privateOpeningPayload"]["digest"]
        or accepted_payload["historicalSeatGrant"] != grant
        or accepted_payload["historicalActorFacts"] != actor
        or accepted_payload["ownerInput"] != command
        or accepted_payload["ownerReceipt"] != receipt
    ):
        raise ValueError("accepted payload authority/readback mismatch")
    if retry["inputDigest"] != expected["ownerInput"]["digest"]:
        raise ValueError("retry input digest mismatch")
    if retry["receiptDigest"] != expected["ownerReceipt"]["digest"]:
        raise ValueError("retry receipt digest mismatch")
    if record["requestDigest"] != expected["ownerInput"]["digest"]:
        raise ValueError("record request digest mismatch")
    core_input = decode_core(schema, "input", accepted_payload["coreInputHex"])
    core_event = decode_core(schema, "event", accepted_payload["coreEventHex"])
    core_receipt = decode_core(schema, "receipt", accepted_payload["coreReceiptHex"])
    private_candidate = next((row["privateCandidateId"] for row in payload["candidateBindings"] if row["ownerCandidateRef"] == command["candidateRef"]), None)
    if core_input != {
        "contractVersion": 1,
        "kind": "select-close-assault" if command["choiceKind"] == "select" else "finish-without-attack",
        "campaignId": campaign, "decisionRef": command["decisionRef"], "requestId": command["requestId"],
        "openingRecordHash": prior_records[1]["digest"], "sourceCutHash": payload["sourceCutHash"],
        "actorSide": grant["side"], "privateCandidateId": private_candidate,
        "priorStateVersion": record["priorStateVersion"], "priorPrefix": record["priorPrefix"],
        "acceptedAtUnixMilliseconds": record["acceptedAtUnixMilliseconds"],
    }:
        raise ValueError("Core input typed owner binding mismatch")
    if (
        core_event["campaignId"] != campaign or core_event["requestId"] != command["requestId"]
        or core_event["inputDigest"] != record["inputDigest"]
        or core_event["priorStateVersion"] != record["priorStateVersion"]
        or core_event["priorPrefix"] != record["priorPrefix"]
        or core_event["resultStateVersion"] != record["resultStateVersion"]
        or core_event["resultPrefix"] != record["resultPrefix"]
        or core_event["outcome"] != ("selected" if command["choiceKind"] == "select" else "no-selection")
        or core_event["privateCandidateId"] != private_candidate
        or core_event["acceptedAtUnixMilliseconds"] != record["acceptedAtUnixMilliseconds"]
    ):
        raise ValueError("Core event typed transition mismatch")
    if (
        core_receipt["campaignId"] != campaign or core_receipt["requestId"] != command["requestId"]
        or core_receipt["inputDigest"] != record["inputDigest"]
        or core_receipt["eventDigest"] != record["eventDigest"]
        or core_receipt["resultStateVersion"] != record["resultStateVersion"]
        or core_receipt["resultPrefix"] != record["resultPrefix"]
    ):
        raise ValueError("Core receipt typed transition mismatch")
    if record["inputDigest"] != core_digest(schema, "input", accepted_payload["coreInputHex"]):
        raise ValueError("record Core input digest mismatch")
    if record["eventDigest"] != core_digest(schema, "event", accepted_payload["coreEventHex"]):
        raise ValueError("record Core event digest mismatch")
    if record["receiptDigest"] != core_digest(schema, "receipt", accepted_payload["coreReceiptHex"]):
        raise ValueError("record Core receipt digest mismatch")
    if record["privatePayloadDigest"] != expected["acceptedTransitionPayload"]["digest"]:
        raise ValueError("record accepted payload digest mismatch")
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
    check_current_retry_auth(current_retry, grant, record, campaign)


def check_negatives(schema: dict, case: dict) -> None:
    def refreeze(mutated: dict) -> None:
        for kind, item in mutated["values"].items():
            raw = canonical(schema, kind, item)
            mutated["expected"][kind] = {"bytes": len(raw), "digest": digest(kind, raw)}
        for row in mutated["priorRecords"]:
            raw = canonical(schema, "journalRecord", row["value"])
            row["bytes"] = len(raw)
            row["digest"] = digest("journalRecord", raw)

    value = case["values"]["ownerInput"]
    for mutation in (
        {"extra": "bad"},
        {"contractVersion": 2},
        {"requestId": "req." + "a" * 1000},
        {"candidateRef": None if value["choiceKind"] == "select" else case["values"]["ownerOpening"]["candidateRefs"][0]},
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
    declined["candidateRef"] = case["values"]["ownerOpening"]["candidateRefs"][0]
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
    accepted = case["values"]["journalRecord"]
    for changed_records in (
        [checkpoint, opening_record, accepted, copy.deepcopy(accepted)],
        [checkpoint, opening_record, copy.deepcopy(accepted)],
    ):
        if len(changed_records) == 3:
            changed_records[2]["sequence"] = 3
        try:
            check_terminal_history(changed_records)
        except ValueError:
            pass
        else:
            raise AssertionError("repeated or out-of-order terminal input accepted")
    changed_record = copy.deepcopy(accepted)
    changed_record["sequence"] = 3
    try:
        canonical(schema, "journalRecord", changed_record)
    except ValueError:
        pass
    else:
        raise AssertionError("schema accepted another v1 owner input")
    grant = case["values"]["seatGrant"]
    actor = copy.deepcopy(case["values"]["actorFacts"])
    opening_payload = case["values"]["privateOpeningPayload"]
    actor["clockAvailable"] = False
    try:
        check_clock(grant, actor, opening_payload)
    except ValueError:
        pass
    else:
        raise AssertionError("unavailable clock accepted")
    unavailable = copy.deepcopy(case)
    unavailable["values"]["actorFacts"]["clockAvailable"] = False
    unavailable["values"]["acceptedTransitionPayload"]["historicalActorFacts"]["clockAvailable"] = False
    unavailable["values"]["journalRecord"]["privatePayloadDigest"] = digest(
        "acceptedTransitionPayload",
        canonical(schema, "acceptedTransitionPayload", unavailable["values"]["acceptedTransitionPayload"]),
    )
    refreeze(unavailable)
    try:
        check_case(schema, unavailable)
    except ValueError as exc:
        if "trusted clock unavailable" not in str(exc):
            raise AssertionError(f"clock negative failed for wrong reason: {exc}") from exc
    else:
        raise AssertionError("refrozen unavailable clock accepted")
    actor["clockAvailable"] = True
    actor["clockAtUtc"] = opening_payload["deadlineAtUtc"]
    try:
        check_clock(grant, actor, opening_payload)
    except ValueError:
        pass
    else:
        raise AssertionError("past-deadline owner input accepted")
    expired = copy.deepcopy(case)
    expired_values = expired["values"]
    expired_values["privateOpeningPayload"]["deadlineAtUtc"] = expired_values["actorFacts"]["clockAtUtc"]
    opening_bytes = canonical(schema, "privateOpeningPayload", expired_values["privateOpeningPayload"])
    opening_hash = digest("privateOpeningPayload", opening_bytes)
    expired["priorRecords"][1]["value"]["privatePayloadDigest"] = opening_hash
    opening_record_bytes = canonical(schema, "journalRecord", expired["priorRecords"][1]["value"])
    opening_record_hash = digest("journalRecord", opening_record_bytes)
    expired_values["actorFacts"]["expectedRecordHash"] = opening_record_hash
    expired_values["journalRecord"]["priorRecordHash"] = opening_record_hash
    expired_values["acceptedTransitionPayload"]["openingRecordHash"] = opening_record_hash
    expired_values["acceptedTransitionPayload"]["privateOpeningPayloadHash"] = opening_hash
    accepted_bytes = canonical(schema, "acceptedTransitionPayload", expired_values["acceptedTransitionPayload"])
    expired_values["journalRecord"]["privatePayloadDigest"] = digest("acceptedTransitionPayload", accepted_bytes)
    refreeze(expired)
    try:
        check_case(schema, expired)
    except ValueError as exc:
        if "owner opening deadline passed" not in str(exc):
            raise AssertionError(f"deadline negative failed for wrong reason: {exc}") from exc
    else:
        raise AssertionError("refrozen expired opening accepted")
    late = copy.deepcopy(case)
    late_values = late["values"]
    late_ms = int(utc(late_values["privateOpeningPayload"]["deadlineAtUtc"]).timestamp()) * 1000
    late_values["journalRecord"]["acceptedAtUnixMilliseconds"] = late_ms
    late_values["acceptedTransitionPayload"]["acceptedAtUnixMilliseconds"] = late_ms
    late_values["journalRecord"]["privatePayloadDigest"] = digest(
        "acceptedTransitionPayload", canonical(schema, "acceptedTransitionPayload", late_values["acceptedTransitionPayload"])
    )
    refreeze(late)
    try:
        check_case(schema, late)
    except ValueError as exc:
        if "acceptance past persisted opening deadline" not in str(exc):
            raise AssertionError(f"committed-time negative failed for wrong reason: {exc}") from exc
    else:
        raise AssertionError("refrozen late committed acceptance accepted")
    late = copy.deepcopy(case)
    late_values = late["values"]
    late_ms = int(utc(late_values["seatGrant"]["expiresAtUtc"]).timestamp()) * 1000
    late_values["journalRecord"]["acceptedAtUnixMilliseconds"] = late_ms
    late_values["acceptedTransitionPayload"]["acceptedAtUnixMilliseconds"] = late_ms
    late_values["journalRecord"]["privatePayloadDigest"] = digest(
        "acceptedTransitionPayload", canonical(schema, "acceptedTransitionPayload", late_values["acceptedTransitionPayload"])
    )
    refreeze(late)
    try:
        check_case(schema, late)
    except ValueError as exc:
        if "acceptance outside grant window" not in str(exc):
            raise AssertionError(f"grant-expiry negative failed for wrong reason: {exc}") from exc
    else:
        raise AssertionError("refrozen expired-grant acceptance accepted")
    spoofed_source = copy.deepcopy(case["values"]["journalRecord"])
    spoofed_source["acceptanceClockSource"] = "owner-supplied-clock"
    try:
        canonical(schema, "journalRecord", spoofed_source)
    except ValueError:
        pass
    else:
        raise AssertionError("caller acceptance-clock source accepted")
    before_preflight = copy.deepcopy(case)
    before_values = before_preflight["values"]
    before_ms = int(utc(before_values["actorFacts"]["clockAtUtc"]).timestamp()) * 1000 - 1
    before_values["journalRecord"]["acceptedAtUnixMilliseconds"] = before_ms
    before_values["acceptedTransitionPayload"]["acceptedAtUnixMilliseconds"] = before_ms
    before_values["journalRecord"]["privatePayloadDigest"] = digest(
        "acceptedTransitionPayload", canonical(schema, "acceptedTransitionPayload", before_values["acceptedTransitionPayload"])
    )
    refreeze(before_preflight)
    try:
        check_case(schema, before_preflight)
    except ValueError as exc:
        if "acceptance precedes trusted preflight" not in str(exc):
            raise AssertionError(f"backdated acceptance failed for wrong reason: {exc}") from exc
    else:
        raise AssertionError("backdated acceptance accepted")
    current = case["values"]["retryAuthFacts"]
    historical = case["values"]["journalRecord"]
    if not (
        utc(current["clockAtUtc"]) > utc(case["values"]["privateOpeningPayload"]["deadlineAtUtc"])
        and current["seatEpoch"] > historical["seatEpoch"]
    ):
        raise AssertionError("golden lacks post-deadline, post-revocation authenticated retry")
    for changed_field, replacement, expected_error in (
        ("authenticated", False, "current retry authentication unavailable"),
        ("clockAvailable", False, "current retry authentication unavailable"),
        ("seatEpoch", 0, "current retry grant generation rollback"),
        ("seatAuthorityId", "seat." + "0" * 64, "current retry seat authority mismatch"),
        ("clockAtUtc", "2026-10-21T00:00:00Z", "current retry grant clock window"),
    ):
        changed = copy.deepcopy(case)
        changed["values"]["retryAuthFacts"][changed_field] = replacement
        refreeze(changed)
        try:
            check_case(schema, changed)
        except ValueError as exc:
            if expected_error not in str(exc):
                raise AssertionError(f"current retry negative failed for wrong reason: {exc}") from exc
        else:
            raise AssertionError(f"invalid current retry accepted: {changed_field}")
    accepted_payload = copy.deepcopy(case["values"]["acceptedTransitionPayload"])
    accepted_payload["coreInputHex"] = "00"
    try:
        decode_core(schema, "input", accepted_payload["coreInputHex"])
    except (ValueError, UnicodeError):
        pass
    else:
        raise AssertionError("arbitrary Core input bytes accepted")
    accepted_payload = copy.deepcopy(case["values"]["acceptedTransitionPayload"])
    accepted_payload["coreEventHex"] = b'{"kind":"selected-assault-event"}'.hex()
    try:
        decode_core(schema, "event", accepted_payload["coreEventHex"])
    except ValueError:
        pass
    else:
        raise AssertionError("kind-only Core event bytes accepted")
    for kind, field in (("input", "coreInputHex"), ("event", "coreEventHex"), ("receipt", "coreReceiptHex")):
        changed = copy.deepcopy(case["values"]["acceptedTransitionPayload"])
        core_value = decode_core(schema, kind, changed[field])
        core_value["contractVersion"] = 2
        changed[field] = json.dumps(core_value, ensure_ascii=True, separators=(",", ":")).encode("ascii").hex()
        try:
            decode_core(schema, kind, changed[field])
        except ValueError:
            pass
        else:
            raise AssertionError(f"unknown Core {kind} version accepted")
        reordered = dict(list(core_value.items())[1:] + list(core_value.items())[:1])
        reordered["contractVersion"] = 1
        changed[field] = json.dumps(reordered, ensure_ascii=True, separators=(",", ":")).encode("ascii").hex()
        try:
            decode_core(schema, kind, changed[field])
        except ValueError:
            pass
        else:
            raise AssertionError(f"reordered Core {kind} bytes accepted")
    wrong_effect = copy.deepcopy(case)
    wrong_values = wrong_effect["values"]
    wrong_payload = wrong_values["acceptedTransitionPayload"]
    wrong_event = decode_core(schema, "event", wrong_payload["coreEventHex"])
    wrong_event["privateCandidateId"] = "p01a." + "0" * 64
    wrong_payload["coreEventHex"] = canonical(schema, "coreEventV1", wrong_event).hex()
    wrong_record = wrong_values["journalRecord"]
    wrong_record["eventDigest"] = core_digest(schema, "event", wrong_payload["coreEventHex"])
    wrong_receipt = decode_core(schema, "receipt", wrong_payload["coreReceiptHex"])
    wrong_receipt["eventDigest"] = wrong_record["eventDigest"]
    wrong_payload["coreReceiptHex"] = canonical(schema, "coreReceiptV1", wrong_receipt).hex()
    wrong_record["receiptDigest"] = core_digest(schema, "receipt", wrong_payload["coreReceiptHex"])
    wrong_record["privatePayloadDigest"] = digest(
        "acceptedTransitionPayload", canonical(schema, "acceptedTransitionPayload", wrong_payload)
    )
    refreeze(wrong_effect)
    try:
        check_case(schema, wrong_effect)
    except ValueError as exc:
        if "Core event typed transition mismatch" not in str(exc):
            raise AssertionError(f"rebound wrong effect failed for wrong reason: {exc}") from exc
    else:
        raise AssertionError("rebound wrong Core effect accepted")
    accepted_payload = copy.deepcopy(case["values"]["acceptedTransitionPayload"])
    accepted_payload["ownerInput"]["requestId"] = "req." + "0" * 64
    changed_request = digest(
        "ownerInput", canonical(schema, "ownerInput", accepted_payload["ownerInput"])
    )
    if changed_request == case["values"]["retryBinding"]["inputDigest"]:
        raise AssertionError("changed owner input retained retry binding")
    accepted_payload.pop("coreEventHex")
    try:
        canonical(schema, "acceptedTransitionPayload", accepted_payload)
    except ValueError:
        pass
    else:
        raise AssertionError("missing durable Core event accepted")
    accepted_payload = copy.deepcopy(case["values"]["acceptedTransitionPayload"])
    accepted_payload["coreReceiptHex"] = "00" * 8193
    try:
        canonical(schema, "acceptedTransitionPayload", accepted_payload)
    except ValueError:
        pass
    else:
        raise AssertionError("over-cap Core receipt accepted")
    accepted_payload = copy.deepcopy(case["values"]["acceptedTransitionPayload"])
    accepted_payload["ownerReceipt"]["resultClass"] = "stale-view"
    try:
        canonical(schema, "acceptedTransitionPayload", accepted_payload)
    except ValueError:
        pass
    else:
        raise AssertionError("nonaccepted receipt in accepted transition")
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
