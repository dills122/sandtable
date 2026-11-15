#!/usr/bin/env python3
"""Check research transcription arithmetic; this does not validate source truth."""

import json
from pathlib import Path

path = Path(__file__).with_name("2026-11-12-graziani-supply-pilot.json")
document = json.loads(path.read_text(encoding="utf-8"))
assert document["status"] == "research-only-single-reader"
assert document["source"] == "CNA1979:60.92(B-C)"
rows = document["rows"]
assert len(rows) == 13
assert len({row["id"] for row in rows}) == len(rows)
summary = {}
for side in ("axis", "commonwealth"):
    subset = [row for row in rows if row["side"] == side]
    for row in subset:
        assert type(row["count"]) is int and row["count"] > 0
        assert type(row["fuel_each"]) is int and 0 <= row["fuel_each"] <= 60
        assert row["ammo_each"] is None or (
            type(row["ammo_each"]) is int and 0 <= row["ammo_each"] <= 40
        )
    summary[side] = {
        "units": sum(row["count"] for row in subset),
        "known_ammo": sum(
            row["count"] * row["ammo_each"]
            for row in subset
            if row["ammo_each"] is not None
        ),
        "unknown_ammo_rows": sum(row["ammo_each"] is None for row in subset),
        "fuel": sum(row["count"] * row["fuel_each"] for row in subset),
    }
assert len(rows) == sum(row["side"] in summary for row in rows)
assert summary == {
    "axis": {"units": 22, "known_ammo": 375, "unknown_ammo_rows": 1, "fuel": 1036},
    "commonwealth": {"units": 15, "known_ammo": 600, "unknown_ammo_rows": 0, "fuel": 810},
}
print(json.dumps(summary, sort_keys=True))
print("Arithmetic only: independent source verification and scenario admission remain open.")
