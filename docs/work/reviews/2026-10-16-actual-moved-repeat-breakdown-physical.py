#!/usr/bin/env python3
"""Exercise all R03 physical dependency acquisitions and five fault modes."""
import importlib.util
import json
from pathlib import Path
from unittest.mock import patch

script = Path(__file__).resolve().parents[2] / "specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py"
spec = importlib.util.spec_from_file_location("r03_physical", script)
r03 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r03)
r03.verify_dependencies()
targets = [r03.REPOSITORY / name for name in r03.PINS] + list(r03.OWN)
assert len(r03.PINS) == 108 and len(targets) == 110
original = Path.read_bytes
checks = 0
for target in targets:
    for mode in ("changed", "missing", "unreadable", "null", "drift"):
        seen = [0]
        def delivery(path):
            if path == target:
                seen[0] += 1
                if mode == "missing":
                    raise FileNotFoundError(str(path))
                if mode == "unreadable":
                    raise PermissionError(str(path))
                if mode == "null":
                    return None
                if mode == "changed" or seen[0] > 1:
                    return b"changed dependency"
            return original(path)
        with patch.object(Path, "read_bytes", delivery):
            if mode == "drift":
                r03.verify_dependencies()
            try:
                r03.verify_dependencies()
            except r03.Invalid as error:
                assert error.code == "CMB-MRB-009", (target, mode, error)
            else:
                raise AssertionError((target, mode, "accepted"))
        checks += 1
assert checks == 550
print(json.dumps(dict(status="PASS", pinnedPaths=len(r03.PINS), ownPaths=len(r03.OWN),
                      faultModes=5, rejectedFaults=checks)))
