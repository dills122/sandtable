#!/usr/bin/env python3
"""Private draft of an owner-visible P01 alias and Umpire-only reverse map.

This is not called by P01 assessment or any public/native consumer. Callers must
first supply independently approved and authenticated owner-visible facts.
"""

import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SCHEMA = json.loads((ROOT / "combat-p01-owner-projection-v0.schema.json").read_bytes())


def require(condition):
    if not condition:
        raise ValueError("CMB-P01-OWNER-PROJECTION")


def canonical(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("ascii")


def draft_alias(visible_facts):
    """Hash only an explicit visible-fact allowlist, never the private candidate."""
    require(type(visible_facts) is dict
            and list(visible_facts) == SCHEMA["visibleFactFields"])
    side = visible_facts["audienceSide"]
    revision = visible_facts["visibleDecisionRevision"]
    capability = visible_facts["capabilityKind"]
    target = visible_facts["apparentTargetRef"]
    require(side in ("axis", "commonwealth")
            and type(revision) is int and revision >= 1
            and capability == SCHEMA["capabilityKind"]
            and type(target) is str and 1 <= len(target) <= 128
            and re.fullmatch(r"[a-z0-9][a-z0-9._:-]*", target) is not None)
    preimage = SCHEMA["domain"].encode("ascii") + b"\0" + canonical(visible_facts)
    return "p01-owner-draft." + hashlib.sha256(preimage).hexdigest()


def private_mapping(bindings):
    """Retain exact private targets behind unique visible capabilities.

    This mapping is Umpire-only. It does not return a public action or authorize
    submissions. An ambiguous visible capability fails closed.
    """
    require(type(bindings) is list and len(bindings) <= 1)
    result = {}
    for binding in bindings:
        require(type(binding) is dict
                and list(binding) == ["ownerVisibleFacts", "privateCandidateId"])
        private_id = binding["privateCandidateId"]
        require(type(private_id) is str
                and re.fullmatch(SCHEMA["privateCandidateIdPattern"], private_id) is not None)
        alias = draft_alias(binding["ownerVisibleFacts"])
        require(alias not in result)
        result[alias] = private_id
    return result


def compare_trial_visibility(first_visible, second_visible, first_legal, second_legal):
    """Fail closed on a synthetic hidden-world legality oracle.

    This does not authenticate histories or establish that any offer is legal.
    """
    require(type(first_legal) is bool and type(second_legal) is bool)
    first_alias = draft_alias(first_visible)
    second_alias = draft_alias(second_visible)
    require(canonical(first_visible) == canonical(second_visible)
            and first_alias == second_alias and first_legal == second_legal)
    return first_alias


def denial_bytes():
    """The only current outward P01 bytes; proposed aliases stay private."""
    return canonical(dict(contractVersion=1, status=SCHEMA["outputStatus"],
                          admitted=False, publicActions=[]))
