#!/usr/bin/env python3
"""Fail-closed private ordinal-two no-attack boundary; emits no P01 event."""
import importlib.util
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SCHEMA = json.loads((ROOT / "combat-p01-no-attack-v0.schema.json").read_text())
spec = importlib.util.spec_from_file_location(
    "p01_no_attack_parent", ROOT / "verify-combat-actual-moved-repeat-position-v1.py")
p01 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p01)


class Invalid(ValueError):
    pass


def require(condition, reason):
    if not condition:
        raise Invalid("CMB-P01-NA-" + reason)


def opening_input_bytes(value):
    """Encode a proposed input only; syntactic validity grants no authority."""
    fields = SCHEMA["zeroCandidateOpeningInputFields"]
    require(type(value) is dict and list(value) == fields, "001")
    fixed = SCHEMA["zeroCandidateOpeningInputValues"]
    require(all(value[key] == expected and type(value[key]) is type(expected)
                for key, expected in fixed.items()), "002")
    require(type(value["expectedPriorVersion"]) is int
            and 0 <= value["expectedPriorVersion"] < 2**63, "002")
    require(all(type(value[key]) is str and re.fullmatch(r"sha256:[0-9a-f]{64}", value[key])
                for key in ("sourceHash", "cycleId", "expectedPriorPrefix",
                            "supportCertificateHash")), "002")
    require(all(type(value[key]) is str and value[key].isascii()
                and 0 < len(value[key]) <= 256
                for key in ("creationBinding", "positionId", "predecessorReceiptId")), "002")
    data = json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("ascii")
    require(len(data) <= SCHEMA["limits"]["inputBytes"], "001")
    return data


def parse_opening_input(data):
    require(type(data) is bytes and 0 < len(data) <= SCHEMA["limits"]["inputBytes"], "001")

    def pairs(items):
        value = {}
        for key, child in items:
            require(key not in value, "001")
            value[key] = child
        return value

    try:
        value = json.loads(data.decode("ascii"), object_pairs_hook=pairs,
                           parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error:
        raise Invalid("CMB-P01-NA-001") from error
    require(opening_input_bytes(value) == data, "003")
    return value


def inspect(source, parent_ledgers, r03_ledger, twelfth_ledger):
    """Reauthenticate genuine R03 and refuse all current ordinal-two issuance.

    The pinned 300-row partition is provisional. No row has the supported-empty
    certificate or twelfth-ledger event authority needed for no-attack traversal.
    """
    assessment = p01.assess(source, parent_ledgers, r03_ledger)
    require(type(twelfth_ledger) is list
            and len(twelfth_ledger) <= SCHEMA["limits"]["twelfthLedgerEvents"]
            and all(type(item) is bytes for item in twelfth_ledger), "001")
    require(assessment["status"] == "unsupported" and assessment["admitted"] is False
            and assessment["publicActions"] == [], "004")
    # A caller's empty list, asserted certificate hash, or syntactically valid
    # opening cannot promote a provisional-empty row to supported-empty.
    require(not twelfth_ledger, "004")
    return {
        "contractVersion": SCHEMA["contractVersion"],
        "status": "unsupported",
        "physicalShape": assessment["physicalShape"],
        "sourceHash": assessment["sourceHash"],
        "worldHash": assessment["worldHash"],
        "randomHash": assessment["randomHash"],
        "activeCycleId": assessment["activeCycleId"],
        "positionId": assessment["positionId"],
        "admitted": False,
        "twelfthLedger": [],
        "publicActions": [],
    }
