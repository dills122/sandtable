#!/usr/bin/env python3
"""Pure proposed P01 owner ID codec. No authority, offer, or public adapter uses it."""

import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SPEC = json.loads((ROOT / "combat-p01-owner-decision-proposal-v1.schema.json").read_text())
PUB = re.compile(r"pub\.[0-9a-f]{64}\Z")
HEX = re.compile(r"sha256:[0-9a-f]{64}\Z")
IDENT = re.compile(r"[\x21-\x7e]{1,128}\Z")


def require(ok):
    if not ok:
        raise ValueError("CMB-P01-OWNER-PROPOSAL")


def fields(value, names):
    require(type(value) is dict and list(value) == names)


def ident(value):
    require(type(value) is str and IDENT.fullmatch(value) is not None)


def pub(value):
    require(type(value) is str and PUB.fullmatch(value) is not None)


def canonical(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":"), allow_nan=False).encode("ascii")


def digest(kind, payload, prefix):
    return prefix + hashlib.sha256(SPEC["domains"][kind].encode("ascii") + b"\0" + canonical(payload)).hexdigest()


def context(value):
    fields(value, SPEC["contextFields"])
    ident(value["campaignId"])
    require(value["audience"] in ("axis", "commonwealth"))
    fields(value["cycle"], SPEC["cycleFields"])
    cycle = value["cycle"]
    require(type(cycle["gameTurn"]) is int and cycle["gameTurn"] >= 1
            and type(cycle["operationStage"]) is int and 1 <= cycle["operationStage"] <= 3
            and cycle["playerPhaseSlot"] in ("first-acting-side", "second-acting-side")
            and cycle["phasingSide"] in ("axis", "commonwealth")
            and type(cycle["ordinal"]) is int and 1 <= cycle["ordinal"] <= SPEC["maxOrdinal"])
    pub(value["rulesRef"])
    pub(value["configRef"])
    ident(value["capabilityPolicyId"])


def target_ref(owner_context, location_id):
    context(owner_context)
    ident(location_id)
    # Only an independently approved owner-visible hex may be supplied here.
    return digest("target", dict(context=owner_context, locationId=location_id), SPEC["targetPrefix"])


def decision_ref(seed):
    fields(seed, SPEC["decisionSeedFields"])
    context(seed["context"])
    require(type(seed["cycleRef"]) is str and HEX.fullmatch(seed["cycleRef"]) is not None)
    ident(seed["positionId"])
    require(type(seed["openingRevision"]) is int
            and SPEC["firstOpeningRevision"] <= seed["openingRevision"] <= SPEC["maxRevision"])
    require(seed["kind"] == SPEC["decisionKind"])
    pub(seed["participantRef"])
    pub(seed["targetRef"])
    return digest("decision", seed, SPEC["decisionPrefix"])


def alias(seed):
    fields(seed, SPEC["aliasSeedFields"])
    pub(seed["decisionRef"])
    require(type(seed["openingRevision"]) is int
            and SPEC["firstOpeningRevision"] <= seed["openingRevision"] <= SPEC["maxRevision"])
    require(seed["capabilityKind"] == SPEC["capabilityKind"])
    pub(seed["ownParticipantRef"])
    pub(seed["apparentTargetRef"])
    return digest("alias", seed, SPEC["aliasPrefix"])


def validate_opening(decision_seed, alias_seed, owner_location_id):
    """Check a proposed binding, not the authenticity of any opening or location."""
    require(decision_seed["targetRef"] == target_ref(decision_seed["context"], owner_location_id)
            and alias_seed["decisionRef"] == decision_ref(decision_seed)
            and alias_seed["openingRevision"] == decision_seed["openingRevision"]
            and alias_seed["ownParticipantRef"] == decision_seed["participantRef"]
            and alias_seed["apparentTargetRef"] == decision_seed["targetRef"])
    return alias(alias_seed)


def reject_bytes():
    """Generic shape only; transport/status policy requires separate approval."""
    return canonical(dict(status=SPEC["genericReject"]))
