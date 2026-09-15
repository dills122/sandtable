"""Read-only regression: equal side histories must yield equal submission outcomes.

Expected exit 1 until accepted clock/privacy policies are reconciled. No authority writes.
"""
import importlib.util
import json
from pathlib import Path

root = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location(
    "side_counterexample", root / "docs/specs/verify-combat-side-projection-v1.py")
side = importlib.util.module_from_spec(spec)
spec.loader.exec_module(side)
catalog = {source["name"]: source for source in side.source_cases()}
failures = []
for name, audience in (("attacker-first", "commonwealth"), ("defender-first", "axis")):
    before, after = (side.prefix(catalog[name], cut) for cut in (1, 2))
    view = side.views(before, audience)[-1]
    equal_bytes = side.raw(view, "Observation") == side.raw(
        side.views(after, audience)[-1], "Observation")
    assert equal_bytes, "Probe requires genuine equal authorized histories"
    proposal = side.raw(side.submission(view), "Submission")
    outcomes = [side.submit(source, audience, proposal, 3500) for source in (before, after)]
    waters = [side.replay_states(source)[-1]["timing"]["highWaterUnixMilliseconds"]
              for source in (before, after)]
    row = dict(source=name, audience=audience, equalObservationBytes=equal_bytes,
               trustedNow=3500, highWaterBefore=waters[0], highWaterAfter=waters[1],
               outcomeBefore=outcomes[0]["status"], outcomeAfter=outcomes[1]["status"])
    print(json.dumps(row, sort_keys=True))
    if outcomes[0] != outcomes[1]:
        failures.append(name)
assert not failures, "CMB-PRO-AC-010 equal-outcome conflict: " + ", ".join(failures)
