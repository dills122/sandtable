"""Check research transcription structure and proposed arithmetic, not Core authority."""
from fractions import Fraction
from hashlib import sha256
import json
from pathlib import Path

path = Path(__file__).parent / 'fixtures/breakdown-outcome-bounds.v1.json'
artifact = json.loads(path.read_text())
coordinates = [10 * first + second for first in range(1, 7) for second in range(1, 7)]
assert artifact['coordinateDomain'] == coordinates
labels = artifact['printedPercentageLabels']
assert labels == [0, 10, 25, 33, 50, 75]
assert [row['bandId'] for row in artifact['bands']] == [
    '0-3', '4-10', '11-20', '21-30', '31-40', '41-50', '51-60', '61-70', '71-plus']
expanded = []
for row in artifact['bands']:
    result = {}
    previous = 0
    assert 1 <= len(row['inclusiveUpperBounds']) <= len(labels)
    for label, bound in zip(labels, row['inclusiveUpperBounds']):
        if bound is None:
            continue
        assert bound in coordinates and bound > previous
        for coordinate in coordinates:
            if previous < coordinate <= bound:
                assert coordinate not in result
                result[coordinate] = label
        previous = bound
    assert set(result) == set(coordinates)
    expanded.append(result)
for coordinate in coordinates:
    assert all(a[coordinate] <= b[coordinate] for a, b in zip(expanded, expanded[1:]))
# Independently selected visible cells: minimum, transitions, maximum and rule example.
assert expanded[0][66] == 0
assert expanded[1][42] == 0 and expanded[1][43] == 10
assert expanded[1][65] == 25 and expanded[1][66] == 33
assert expanded[3][33] == 10
assert expanded[6][61] == 33
assert expanded[7][11] == 10 and expanded[8][66] == 75

# BRK-DEC-004 proposal: label 33 is exact one-third. Not an approved Core rule.
def proposed_loss(points, label):
    if points == 1 and label == 10:
        return 0
    fraction = Fraction(1, 3) if label == 33 else Fraction(label, 100)
    amount = points * fraction
    return (amount.numerator + amount.denominator - 1) // amount.denominator

assert proposed_loss(30, 10) == 3
assert proposed_loss(20, 33) == 7
assert proposed_loss(1, 10) == 0 and proposed_loss(1, 25) == 1
assert proposed_loss(100, 33) == 34  # Distinguishes 1/3 from literal 33/100.
assert all(0 <= proposed_loss(n, label) <= n for n in range(101) for label in labels)
print('PASS: 324 coordinate/band cells; complete unique ranges; monotone columns; 9 source-cell probes; 606 bounded proposed-loss combinations.')
print('Transcription sha256:' + sha256(path.read_bytes()).hexdigest())
