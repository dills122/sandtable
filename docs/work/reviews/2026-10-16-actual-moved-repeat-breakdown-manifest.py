#!/usr/bin/env python3
"""Reject mutable pin-map deletion/substitution after manifest capture."""
import hashlib
import importlib.util
from pathlib import Path

script = Path(__file__).resolve().parents[2] / "specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py"
spec = importlib.util.spec_from_file_location("r03_manifest", script)
r03 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r03)
r03.verify_dependencies()
original = dict(r03.PINS)
first = next(iter(original))
replacement = "docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.md"
assert replacement not in original

def rejects():
    try:
        r03.verify_dependencies()
    except r03.Invalid as error:
        assert error.code == "CMB-MRB-009", error
    else:
        raise AssertionError("altered pin map accepted")

try:
    del r03.PINS[first]
    rejects()
    r03.PINS[replacement] = hashlib.sha256((r03.REPOSITORY / replacement).read_bytes()).hexdigest()
    rejects()
finally:
    r03.PINS.clear()
    r03.PINS.update(original)
r03.verify_dependencies()
print("PASS pin-map deletion and valid-file substitution both reject CMB-MRB-009")
