#!/usr/bin/env python3
"""RESREL-RSH-001 logical research probes; no campaign resolver or source-table artifact."""

from fractions import Fraction
from itertools import product


def choices(status, first):
    if status == "none":
        return ()
    if status == "I" and first:
        return ("release", "convert-II")
    if status == "II" and not first:
        return ("release", "retain-II")
    raise ValueError("invalid status for release history")


def can_spend(kind, cpa, spent, cost):
    if kind not in ("I", "II") or cpa <= 0 or spent < 0 or cost < 0:
        raise ValueError("invalid released-unit ledger")
    ceiling = cpa if kind == "I" else cpa // 2
    return spent + cost <= ceiling


def next_movement_exception(release_scope, release_cycle, movement_scope, movement_cycle, open_segment):
    return open_segment and release_scope == movement_scope and movement_cycle == release_cycle + 1


def can_commit_offensive(previous_offensive, budget_and_other_legality):
    return previous_offensive == 0 and budget_and_other_legality


def check(actual, expected, label):
    if actual != expected:
        raise AssertionError(f"{label}: expected {expected!r}, got {actual!r}")


def main():
    for first in (True, False):
        check(choices("none", first), (), "no reserve obligation")
    check(choices("I", True), ("release", "convert-II"), "mandatory first disposition")
    check(choices("II", False), ("release", "retain-II"), "later voluntary release")
    for status, first in (("II", True), ("I", False), ("unknown", True)):
        try:
            choices(status, first)
        except ValueError:
            pass
        else:
            raise AssertionError("invalid release history accepted")
    ledger_vectors = [
        ("I", 10, 3, 7, True), ("I", 10, 3, 8, False),
        ("II", 9, 3, 1, True), ("II", 9, 3, 2, False),
        ("II", 9, 5, 1, False), ("II", 1, 0, 1, False),
        ("II", 9, Fraction(7, 2), Fraction(1, 2), True),
        ("II", 9, Fraction(7, 2), 1, False),
    ]
    for kind, cpa, spent, cost, expected in ledger_vectors:
        check(can_spend(kind, cpa, spent, cost), expected, "cumulative voluntary ceiling")
    release_scope = (1, 1, "first")
    scope_vectors = [
        ((1, 1, "first"), 3, True, True),
        ((1, 1, "first"), 3, False, False),
        ((1, 1, "first"), 2, True, False),
        ((1, 1, "first"), 4, True, False),
        ((1, 1, "second"), 3, True, False),
        ((1, 2, "first"), 3, True, False),
        ((2, 1, "first"), 3, True, False),
    ]
    for scope, cycle, open_segment, expected in scope_vectors:
        check(next_movement_exception(release_scope, 2, scope, cycle, open_segment), expected,
              "immediately subsequent same-phase movement")
    for used, other in product((0, 1), (False, True)):
        check(can_commit_offensive(used, other), (used, other) == (0, True), "offensive allowance")
    print("passed: 7 release cases, 8 CP vectors, 7 scope vectors, 4 offensive-use cases; research only")


if __name__ == "__main__":
    main()
