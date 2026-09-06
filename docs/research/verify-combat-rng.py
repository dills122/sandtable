#!/usr/bin/env python3
"""CMB-RSH-004 research oracle; sampled chart facts, not a production Combat table."""

import hashlib
import json
from collections import Counter
from itertools import product

DOMAIN = b"sandtable.random.v1\0"
MAX_U64 = (1 << 64) - 1
LABELS = ("attacker.morale.tens", "attacker.morale.ones",
          "defender.morale.tens", "defender.morale.ones",
          "attacker.assault.tens", "attacker.assault.ones",
          "defender.assault.tens", "defender.assault.ones")


def block(seed, index):
    return hashlib.sha256(DOMAIN + seed.to_bytes(8, "big") + index.to_bytes(8, "big")).digest()


def roll(seed, cursor):
    if not 0 <= seed <= MAX_U64 or not 0 <= cursor <= MAX_U64:
        raise ValueError("unsigned-64 state required")
    consumed = []
    while True:
        if cursor == MAX_U64:
            raise OverflowError("cursor exhausted before byte consumption")
        value = block(seed, cursor // 32)[cursor % 32]
        cursor += 1
        consumed.append(value)
        if value < 252:
            return value % 6 + 1, cursor, bytes(consumed)


def morale(pair):
    return 1 if pair == (1, 1) else -1 if pair == (6, 6) else 0


def capture_side(differential, attacker, defender):
    if differential == -2 and attacker == (1, 1):
        return "attacker"
    if differential >= 0 and defender == (1, 1):
        return "defender"
    return None


def trace(seed, cursor):
    labels, dice, ends, raw = [], [], [], b""
    for label in LABELS:
        value, cursor, consumed = roll(seed, cursor)
        labels.append(label); dice.append(value); ends.append(cursor); raw += consumed
    differential = morale(tuple(dice[:2])) - morale(tuple(dice[2:4]))
    side = capture_side(differential, tuple(dice[4:6]), tuple(dice[6:8]))
    if side is not None:
        value, cursor, consumed = roll(seed, cursor)
        labels.append(f"{side}.capture.share"); dice.append(value); ends.append(cursor); raw += consumed
    return labels, tuple(dice), ends, raw.hex(), differential, side


def check(actual, expected, label):
    if actual != expected:
        raise AssertionError(f"{label}: expected {expected!r}, got {actual!r}")


# Literal expected bytes/dice: independently compared with openssl SHA-256 and existing Core goldens.
# Chart facts: manually checked common charts 15.79/15.89; no general loss table is embedded.
# Fields: seed, cursor, dice, bytes, differential, A%, D%, raw Engaged, retreat, unfulfilled,
# capture share (0 = no trigger), expected (A loss, D loss, A captured, D captured, A DP, D DP, A RP).
VECTORS = [
    (0, 0, "66241223", "8311c7e78aaf9de6", -1, 20, 10, False, 0, 0, 0, (2, 1, 0, 0, 0, 0, 0)),
    (7, 0, "54565626", "acb75efd838ef5f765", 0, 0, 10, False, 0, 0, 0, (0, 1, 0, 0, 0, 0, 0)),
    (15, 0, "53661522", "f40efb4d9cd62b13", 1, 15, 15, False, 1, 0, 0, (2, 1, 0, 0, 0, 0, 3)),
    (18, 0, "65525532", "65c46ae5fa64f243", 0, 0, 10, True, 1, 0, 0, (0, 1, 0, 0, 0, 0, 3)),
    (26, 0, "36541166", "9ef5fa5dc04ee9e9", 0, 25, 0, False, 0, 0, 0, (3, 0, 0, 0, 3, 0, 0)),
    (47, 0, "223514112", "5bf7b6a6067b3cc69d", 0, 15, 20, False, 0, 0, 25, (2, 2, 0, 1, 0, 0, 0)),
    (208, 0, "21665513", "c7e42911a034d87a", 1, 0, 20, True, 1, 1, 0, (0, 3, 0, 0, 0, 3, 0)),
    (1296, 0, "11665665", "6660d795881dfda164", 2, 0, 0, True, 2, 0, 0, (0, 0, 0, 0, 0, 0, 3)),
    (4983, 0, "66113545", "bf1d3c7e2028ab46", -2, 10, 0, False, 1, 0, 0, (1, 0, 0, 0, 0, 0, 3)),
    (31707, 0, "661111635", "3b11728a60307de082", -2, 25, 0, False, 1, 0, 50, (3, 0, 2, 0, 3, 0, 3)),
    (0, 30, "66443222", "4dd79fabb6070dc7", -1, 10, 10, False, 0, 0, 0, (1, 1, 0, 0, 0, 0, 0)),
    (0, 129, "21636133", "fee57211e0ef72202c", 0, 0, 5, False, 1, 0, 0, (0, 0, 0, 0, 0, 0, 3)),
]


def main():
    check(block(0, 0).hex(), "8311c7e78aaf9de64d3301fb0ed4839c4082a57438da962a518d7fcc22b04dd7", "Core block 0")
    check(block(1, 1).hex(), "05709046ad6157be8e40c1567beef84d7175d8d0a8ce3cd9fd1ff6bc7098dcc4", "Core block 1")
    for vector in VECTORS:
        seed, start, expected_dice, expected_raw, diff, ap, dp, engaged, retreat, refused, share, expected = vector
        labels, dice, ends, raw, actual_diff, side = trace(seed, start)
        check("".join(map(str, dice)), expected_dice, "accepted dice")
        check(raw, expected_raw, "consumed bytes including rejection")
        check(actual_diff, diff, "Morale differential")
        expected_ends = [start + i + 1 for i, b in enumerate(bytes.fromhex(expected_raw)) if b < 252]
        check(ends, expected_ends, "per-die cursors")
        expected_labels = [f"{role}.{purpose}.{place}" for purpose in ("morale", "assault")
                           for role in ("attacker", "defender") for place in ("tens", "ones")]
        check(labels[:8], expected_labels, "role/purpose order")
        check(len(dice), 9 if share else 8, "conditional count")
        if share:
            check(labels[-1], f"{side}.capture.share", "capture purpose")
            check((10, 25, 33, 50, 50, 75)[dice[-1] - 1], share, "capture share")
        # Only source-derived sum flags are generalized here; percentage facts remain sampled inputs.
        a_sum, d_sum = sum(dice[4:6]), sum(dice[6:8])
        check(a_sum in {-2: (10, 11), -1: (10, 11, 12), 0: (9, 10, 12),
                        1: (9, 10, 11), 2: (9, 10, 11, 12)}[diff], engaged, "raw Engaged")
        one_hex = {-2: (9,), -1: (8,), 0: (5, 6), 1: (4, 5, 6), 2: (5, 6, 7)}
        check(2 if diff == 2 and d_sum == 11 else int(d_sum in one_hex[diff]), retreat, "retreat distance")
        a_loss, d_loss = (10 * ap + 99) // 100, 10 * (dp + 10 * refused) // 100
        a_cap = (a_loss * share + 99) // 100 if side == "attacker" else 0
        d_cap = (d_loss * share + 99) // 100 if side == "defender" else 0
        result = (a_loss, d_loss, a_cap, d_cap, 3 if a_loss >= 3 else 0,
                  3 if d_loss >= 3 else 0, 3 if retreat > 0 and refused == 0 else 0)
        check(result, expected, "fixed loss/capture/DP/RP oracle")
        # Resume every accepted-die boundary, including conditional capture, without reseeding.
        for index, end in enumerate(ends[:-1]):
            check(roll(seed, end)[0:2], (dice[index + 1], ends[index + 1]), "cursor continuation")

    pairs = list(product(range(1, 7), repeat=2))
    weights = Counter(morale(a) - morale(d) for a, d in product(pairs, repeat=2))
    check(dict(weights), {-2: 1, -1: 68, 0: 1158, 1: 68, 2: 1}, "Morale domain weights")
    capture_paths = 0
    coordinates = set()
    for diff, a, d in product(range(-2, 3), pairs, pairs):
        coordinates.add((diff, a, d))
        if capture_side(diff, a, d):
            capture_paths += weights[diff]
    check(len(coordinates), 6480, "conditional joint coordinate domain")
    check(capture_paths, 44208, "weighted conditional capture paths")
    check(36 ** 4 - capture_paths + capture_paths * 6, 1900656, "expanded draw leaves")
    try:
        roll(0, MAX_U64)
    except OverflowError:
        pass
    else:
        raise AssertionError("cursor overflow accepted")
    print(json.dumps({"status": "passed", "seeded_vectors": len(VECTORS),
                      "joint_coordinate_domain": len(coordinates), "base_draw_paths": 36 ** 4,
                      "capture_paths": capture_paths, "expanded_leaves": 1900656,
                      "limit": "research RNG/arithmetic; sampled loss facts, no full table or runtime proof"}, indent=2))


if __name__ == "__main__":
    main()
