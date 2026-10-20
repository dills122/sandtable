#!/usr/bin/env python3
"""Private Content8/Setup8 source oracle. It does not issue a CreationRequest."""

import copy
import hashlib
import importlib.util
import json
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
FIXTURES = ROOT / "fixtures"


def load_module(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


v7 = load_module("combat_content_v7_oracle", "verify-combat-content-v7.py")
creation7 = load_module("combat_creation_v7_oracle", "verify-combat-creation-ledger-v1.py")

PROFILE = "sandtable.capability.combat-auto-surrender-infantry.v1"
PACK = "rules-lab.content.close-assault-ammunitionless-defender.v1"
SCENARIO = "close-assault-ammunitionless-defender-v1"
SETUP = "rules-lab.combat.close-assault-ammunitionless-defender.v1"
RULES = "cna-1979.1.combat-auto-surrender-source.v1"
SOURCE = "sandtable-rules-lab"
LOCATOR = "combat.close-assault-ammunitionless-defender.v1"

CONTENT_SCHEMA = copy.deepcopy(v7.SCHEMA)
CONTENT_SCHEMA["Root"].append(("ammunitionAccessInventory", "AccessInventory"))
CONTENT_SCHEMA["AccessInventory"] = [
    ("scope", "string"), ("externalSources", "ExternalSource[]"), ("origin", "Origin")]
CONTENT_SCHEMA["ExternalSource"] = [
    ("sourceId", "id"), ("ownerSideId", "id"), ("locationId", "id"),
    ("kind", "string"), ("points", "int"), ("origin", "Origin")]

SETUP_SCHEMA = copy.deepcopy(creation7.SCHEMA)
SETUP_SCHEMA["Setup"].insert(-1, ("rulesSource", "RulesSelection"))
SETUP_SCHEMA["RulesSelection"] = [
    ("schemaVersion", "int"), ("artifactId", "id"), ("hash", "hash")]

RULES_SCHEMA = {
    "Root": [("schemaVersion", "int"), ("artifactId", "id"),
             ("profileId", "id"), ("ownerDecision", "Decision"),
             ("sourceLocators", "Reference[]"),
             ("sourceEvidence", "Evidence[]"), ("trigger", "Trigger")],
    "Decision": [("proposalCommit", "string"), ("scope", "string")],
    "Reference": [("sourceId", "id"), ("locator", "locator")],
    "Evidence": [("sourceId", "id"), ("sha256", "hash")],
    "Trigger": [("attackKind", "string"), ("acceptedSelectionRequired", "bool"),
                ("defenderRbaDeclineRequired", "bool"),
                ("defenderAccessibleAmmo", "int"), ("defenderCohesion", "int"),
                ("condition", "string"), ("settlementStatus", "string")],
}


class Invalid(ValueError):
    def __init__(self, code, path):
        self.code, self.path = code, path
        super().__init__(f"{code} {path or '/'}")


def require(ok, code, path):
    if not ok:
        raise Invalid(code, path)


def pairs(items):
    result = {}
    for key, value in items:
        require(key not in result, "SRC-001", "/")
        result[key] = value
    return result


def shape(value, kind, schema, path="", depth=0):
    require(depth <= 32, "SRC-001", path)
    if kind.endswith("[]"):
        require(type(value) is list, "SRC-001", path)
        if kind != "[]":
            for i, item in enumerate(value):
                shape(item, kind[:-2], schema, f"{path}/{i}", depth + 1)
        else:
            require(not value, "SRC-002", path)
    elif kind in schema:
        require(type(value) is dict, "SRC-001", path)
        require(set(value) == {key for key, _ in schema[kind]}, "SRC-001", path)
        for key, child in schema[kind]:
            shape(value[key], child, schema, f"{path}/{key}", depth + 1)
    else:
        expected = {"int": int, "bool": bool, "null": type(None), "id": str,
                    "hash": str, "locator": str, "string": str, "side": str,
                    "long": int}[kind]
        require(type(value) is expected, "SRC-001", path)
        if kind in ("id", "locator", "hash"):
            pattern = {"id": v7.ID, "locator": v7.LOCATOR,
                       "hash": r"sha256:[0-9a-f]{64}"}[kind]
            require(1 <= len(value) <= 128 and re.fullmatch(pattern, value), "SRC-002", path)
        if kind in ("int", "long"):
            bound = 2**63 if kind == "long" else 2**31
            require(-bound <= value < bound, "SRC-002", path)
        if kind == "side":
            require(value in ("axis", "commonwealth"), "SRC-002", path)


def canonical(value, kind, schema):
    if kind in schema:
        return {key: canonical(value[key], child, schema) for key, child in schema[kind]}
    if kind.endswith("[]") and kind != "[]":
        child = kind[:-2]
        items = [canonical(item, child, schema) for item in value]
        keys = {**v7.KEYS, **creation7.KEYS,
                "ExternalSource": ("sourceId",), "Evidence": ("sourceId",)}
        if child in keys:
            items.sort(key=lambda item: tuple(item[key] for key in keys[child]))
        return items
    return value


def encode(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("utf-8")


def digest(raw):
    return "sha256:" + hashlib.sha256(raw).hexdigest()


def decode(raw, kind, schema):
    require(len(raw) <= 65536 and not raw.startswith(b"\xef\xbb\xbf"), "SRC-001", "/")
    try:
        value = json.loads(raw.decode("utf-8"), object_pairs_hook=pairs,
                           parse_constant=lambda _: (_ for _ in ()).throw(Invalid("SRC-001", "/")))
    except (UnicodeError, ValueError, RecursionError) as error:
        if isinstance(error, Invalid):
            raise
        raise Invalid("SRC-001", "/") from error
    shape(value, kind, schema)
    require(encode(canonical(value, kind, schema)) == raw, "SRC-008", "/")
    return value


def origin(locator):
    return {"kind": "synthetic", "references": [{"sourceId": SOURCE, "locator": locator}]}


def validate_content(pack):
    shape(pack, "Root", CONTENT_SCHEMA)
    for key, expected in (("schemaVersion", 8), ("formatId", "sandtable.content-json.v7"),
                          ("packId", PACK), ("rulesetId", "cna-1979.1"),
                          ("capabilityProfileId", PROFILE)):
        require(pack[key] == expected, "SRC-003", "/" + key)
    require(len(pack["scenarios"]) == 1 and pack["scenarios"][0]["scenarioId"] == SCENARIO,
            "SRC-003", "/scenarios")
    inventory = pack["ammunitionAccessInventory"]
    require(inventory["scope"] == "complete-initial-synthetic-inventory" and
            inventory["externalSources"] == [] and
            inventory["origin"] == origin(LOCATOR + ":closed-ammunition-inventory"),
            "SRC-004", "/ammunitionAccessInventory")
    placements = pack["scenarios"][0]["initialPlacements"]
    require({p["elementId"] for p in placements} ==
            {"axis-assault-battalion", "commonwealth-assault-battalion"},
            "SRC-004", "/scenarios/0/initialPlacements")
    for i, placement in enumerate(placements):
        side = "axis" if placement["elementId"].startswith("axis-") else "commonwealth"
        expected = 10 if side == "axis" else 0
        ammo = placement["initialAmmunition"]
        require(ammo["points"] == expected and
                ammo["origin"] == origin(LOCATOR + ":initial-ammo." + side),
                "SRC-004", f"/scenarios/0/initialPlacements/{i}/initialAmmunition")
    locators = []

    def inspect_origins(value, path=""):
        if isinstance(value, dict):
            if value.get("kind") == "synthetic" and "references" in value:
                refs = value["references"]
                require(len(refs) == 1 and refs[0]["sourceId"] == SOURCE and
                        refs[0]["locator"].startswith(LOCATOR + ":"),
                        "SRC-006", path)
                locators.append(refs[0]["locator"])
            for key, child in value.items():
                inspect_origins(child, path + "/" + key)
        elif isinstance(value, list):
            for i, child in enumerate(value):
                inspect_origins(child, f"{path}/{i}")

    inspect_origins(pack)
    require(len(locators) == len(set(locators)), "SRC-006", "/sourceIndex")
    # Reuse the certified Content7 shared structural/geometry validator on a
    # private normalized copy. Only identity and the new ammunition seed are
    # normalized; Content8-specific facts are checked above, never issued as V7.
    common = copy.deepcopy(pack)
    del common["ammunitionAccessInventory"]
    common.update(schemaVersion=7, formatId="sandtable.content-json.v6",
                  packId="rules-lab.content.close-assault.v1",
                  capabilityProfileId="sandtable.capability.combat-cycle-infantry.v1")
    for placement in common["scenarios"][0]["initialPlacements"]:
        placement["initialAmmunition"]["points"] = 10
    try:
        v7.validate(common)
    except v7.Invalid as error:
        raise Invalid("SRC-005", error.path) from error


def validate_rules(rules):
    shape(rules, "Root", RULES_SCHEMA)
    for key, expected in (("schemaVersion", 2), ("artifactId", RULES), ("profileId", PROFILE)):
        require(rules[key] == expected, "SRC-003", "/" + key)
    require(rules["ownerDecision"] == {
        "proposalCommit": "c228d82ff32d70864f8a2f855a78e47fb0c063cd",
        "scope": "approved-private-asymmetric-source-only"},
        "SRC-006", "/ownerDecision")
    expected_locators = [
        {"sourceId": SOURCE, "locator": "CMB-POL-004-009.owner-pr-289"},
        {"sourceId": "spi-1979-compilation", "locator": "Logistics50.0-50.17"},
        {"sourceId": "spi-1979-land-rules", "locator": "15.88"},
        {"sourceId": "spi-1979-september-errata", "locator": "15.88-50.12-50.2"},
    ]
    require(rules["sourceLocators"] == expected_locators, "SRC-006", "/sourceLocators")
    original_rules = json.loads((FIXTURES / "combat-rules-inputs-v1.json").read_text())
    original_evidence = json.loads(next(g["canonicalUtf8"] for g in original_rules["goldens"]
                                        if g["kind"] == "RulesInput"))["sourceEvidence"]
    require(rules["sourceEvidence"] == [e for e in original_evidence if e["sourceId"] in
            {"spi-1979-compilation", "spi-1979-land-rules", "spi-1979-september-errata"}],
            "SRC-006", "/sourceEvidence")
    require(rules["trigger"] == {
        "attackKind": "close-assault", "acceptedSelectionRequired": True,
        "defenderRbaDeclineRequired": True, "defenderAccessibleAmmo": 0,
        "defenderCohesion": 0, "condition": "ammunitionless-when-assaulted",
        "settlementStatus": "unsupported"}, "SRC-007", "/trigger")


def setup_hash(setup):
    canonical_setup = canonical(setup, "Setup", SETUP_SCHEMA)
    return digest(encode({key: value for key, value in canonical_setup.items()
                          if key != "setupHash"}))


def validate_setup(setup, pack, rules, content_raw, rules_raw):
    shape(setup, "Setup", SETUP_SCHEMA)
    for key, expected in (("schemaVersion", 8), ("setupId", SETUP),
                          ("capabilityProfileId", PROFILE), ("isSynthetic", True)):
        require(setup[key] == expected, "SRC-003", "/" + key)
    expected_content = {"schemaVersion": 8, "formatId": "sandtable.content-json.v7",
                        "packId": PACK, "rulesetId": "cna-1979.1",
                        "hash": digest(content_raw), "scenarioId": SCENARIO}
    require(setup["content"] == expected_content, "SRC-009", "/content")
    require(setup["rulesSource"] == {"schemaVersion": 2, "artifactId": RULES,
                                     "hash": digest(rules_raw)}, "SRC-009", "/rulesSource")
    require(setup["combatInitialization"]["cohesionLevel"] == 0,
            "SRC-004", "/combatInitialization/cohesionLevel")
    require(setup["combatInitialization"]["origin"] == origin(LOCATOR + ":initial-ledger") and
            setup["sources"] == [{"sourceId": SOURCE, "locator": LOCATOR + ":setup"}],
            "SRC-006", "/sources")
    common = copy.deepcopy(setup)
    del common["rulesSource"]
    common.update(schemaVersion=7, setupId="rules-lab.combat.close-assault.v1",
                  capabilityProfileId="sandtable.capability.combat-cycle-infantry.v1")
    common["content"] = creation7.selection(json.loads((FIXTURES / "combat-content-v7.canonical.json").read_text()))
    common["combatInitialization"]["origin"] = creation7.origin("initial-ledger")
    common["sources"] = creation7.refs("combat.close-assault-positive.v1:setup")
    common["setupHash"] = creation7.setup_hash(common)
    try:
        creation7.validate_setup(common, json.loads((FIXTURES / "combat-content-v7.canonical.json").read_text()))
    except creation7.Invalid as error:
        raise Invalid("SRC-005", error.path) from error
    require(setup["setupHash"] == setup_hash(setup), "SRC-009", "/setupHash")


def validate_all(content_raw, rules_raw, setup_raw):
    pack = decode(content_raw, "Root", CONTENT_SCHEMA)
    rules = decode(rules_raw, "Root", RULES_SCHEMA)
    setup = decode(setup_raw, "Setup", SETUP_SCHEMA)
    validate_content(pack)
    validate_rules(rules)
    validate_setup(setup, pack, rules, content_raw, rules_raw)
    return pack, rules, setup


def main():
    require(digest((FIXTURES / "combat-content-v7.canonical.json").read_bytes()) ==
            "sha256:ee4fde9638ceb61ec08612fe32f9ac81db05572aac5ca20941e402d2fed25847",
            "SRC-010", "/originalContent7")
    original_creation = json.loads((FIXTURES / "combat-creation-ledger-v1.json").read_text())
    require(original_creation["setup"]["sha256"] ==
            "sha256:1ca736568214fc1100ab07f729cb6b5d601a80b524e51933bd754d8e0317d3fd",
            "SRC-010", "/originalSetup7")
    manifest = json.loads((FIXTURES / "combat-auto-surrender-source-v1.manifest.json").read_text())
    names = ("combat-auto-surrender-content-v8.canonical.json",
             "combat-auto-surrender-rules-source-v1.canonical.json",
             "combat-auto-surrender-setup-v8.canonical.json")
    raw = [(FIXTURES / name).read_bytes() for name in names]
    for name, data in zip(names, raw):
        require(manifest["artifacts"][name] == {"byteCount": len(data), "sha256": digest(data)},
                "SRC-010", "/artifacts/" + name)
    pack, rules, setup = validate_all(*raw)
    require(manifest["status"] == "private-source-only-unissuable" and
            manifest["creationRequest"] is None and manifest["predecessorLedgers"] == [],
            "SRC-011", "/status")
    for vector in manifest["negativeVectors"]:
        changed = [bytearray(x) for x in raw]
        target = names.index(vector["artifact"])
        data = json.loads(changed[target])
        node = data
        parts = vector["path"].strip("/").split("/")
        for part in parts[:-1]:
            node = node[int(part)] if isinstance(node, list) else node[part]
        last = int(parts[-1]) if isinstance(node, list) else parts[-1]
        if vector["op"] == "remove":
            del node[last]
        else:
            node[last] = vector["value"]
        changed[target] = encode(canonical(data, ("Root", "Root", "Setup")[target],
                                           (CONTENT_SCHEMA, RULES_SCHEMA, SETUP_SCHEMA)[target]))
        try:
            validate_all(*changed)
        except Invalid as error:
            require(error.code == vector["code"], "SRC-012", vector["name"])
        else:
            raise AssertionError("accepted mutation: " + vector["name"])
    print(f"PASS source-only: {len(manifest['negativeVectors'])} negatives; "
          f"Content8 {digest(raw[0])}; Rules {digest(raw[1])}; Setup8 {digest(raw[2])}; "
          "zero CreationRequests, ledgers, surrender events or admitted rows")


if __name__ == "__main__":
    main()
