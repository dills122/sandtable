#!/usr/bin/env python3
"""Synthetic CMB-RSH-003 arithmetic evidence; not a Combat resolver or table oracle."""

import json
from fractions import Fraction
from itertools import product


def ceil_ratio(numerator, denominator):
    return (numerator + denominator - 1) // denominator


def loss(side, percent, unfulfilled=0, capture_percent=0, toe=10):
    """One fully committed rating-1 component; default 10 TOE, plus a 9-TOE probe."""
    if toe not in (9, 10):
        raise ValueError("outside research strength probes")
    if side not in ("attacker", "defender"):
        raise ValueError("unknown side")
    if percent not in (0, 5, 10, 15, 20, 25):
        raise ValueError("outside selected loss envelope")
    if unfulfilled not in (0, 1, 2) or (side == "attacker" and unfulfilled):
        raise ValueError("outside selected retreat envelope")
    if capture_percent not in (0, 10, 25, 33, 50, 75):
        raise ValueError("outside selected capture envelope")
    numerator = toe * (percent + 10 * unfulfilled)
    lost = ceil_ratio(numerator, 100) if side == "attacker" else numerator // 100
    captured = ceil_ratio(lost * capture_percent, 100)
    return (lost, captured, lost - captured, toe - lost, 3 if lost * 100 >= 30 * toe else 0)


def check(actual, expected, label):
    if actual != expected:
        raise AssertionError(f"{label}: expected {expected!r}, got {actual!r}")


def main():
    # Independently stated tuples: loss, captured, other loss, remaining, loss DP.
    vectors = [
        ("attacker", 25, 0, 0, 10, (3, 0, 3, 7, 3)),
        ("defender", 25, 0, 0, 10, (2, 0, 2, 8, 0)),
        ("defender", 15, 1, 0, 10, (2, 0, 2, 8, 0)),
        ("defender", 25, 2, 0, 10, (4, 0, 4, 6, 3)),
        ("attacker", 25, 0, 33, 10, (3, 1, 2, 7, 3)),
        ("defender", 20, 1, 75, 10, (3, 3, 0, 7, 3)),
        ("defender", 0, 0, 75, 10, (0, 0, 0, 10, 0)),
        # Outside the selected 10-TOE chart surface; detects separate rounding.
        ("defender", 15, 1, 0, 9, (2, 0, 2, 7, 0)),
    ]
    for side, percent, unfulfilled, captured, toe, expected in vectors:
        check(loss(side, percent, unfulfilled, captured, toe), expected, "boundary vector")

    checked = 0
    for side in ("attacker", "defender"):
        for percent, unfulfilled, share in product(
            (0, 5, 10, 15, 20, 25),
            (0,) if side == "attacker" else (0, 1, 2),
            (0, 10, 25, 33, 50, 75),
        ):
            lost, captured, other, remaining, dp = loss(side, percent, unfulfilled, share)
            # Fraction-based oracle independently expresses source rounding.
            raw_loss = Fraction(10 * (percent + 10 * unfulfilled), 100)
            expected_loss = int(raw_loss)
            if side == "attacker" and raw_loss != expected_loss:
                expected_loss += 1
            raw_capture = Fraction(expected_loss * share, 100)
            expected_capture = int(raw_capture) + (raw_capture != int(raw_capture))
            check(lost, expected_loss, "loss oracle")
            check(captured, expected_capture, "capture oracle")
            check(remaining + other + captured, 10, "TOE conservation")
            check(0 <= captured <= lost <= (3 if side == "attacker" else 4), True, "bounds")
            check(dp, 3 if Fraction(lost, 10) >= Fraction(3, 10) else 0, "DP threshold")
            checked += 1
    check(checked, 144, "envelope size")

    print(json.dumps({
        "status": "passed", "boundary_vectors": len(vectors),
        "conditional_arithmetic_combinations": checked,
        "coverage": "synthetic arithmetic only; no chart correlation or runtime proof",
    }, indent=2))


if __name__ == "__main__":
    main()
