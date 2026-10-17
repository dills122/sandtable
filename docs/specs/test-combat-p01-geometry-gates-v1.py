#!/usr/bin/env python3
"""Focused authenticated-row tests for the bounded P01 geometry proof."""

import gzip
import hashlib
import importlib.util
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    'p01_geometry', ROOT / 'verify-combat-p01-geometry-gates-v1.py')
geometry = importlib.util.module_from_spec(spec)
spec.loader.exec_module(geometry)


class P01GeometryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        evidence = geometry.result.EVIDENCE
        cls.rules = geometry.result.pinned_rules(evidence)
        manifest = json.loads((evidence / 'manifest.json').read_text())
        rows = []
        for chunk in manifest['chunks']:
            if chunk['name'] != 'rows-036-067.jsonl.gz':
                continue
            raw = (evidence / chunk['name']).read_bytes()
            assert hashlib.sha256(raw).hexdigest() == chunk['sha256']
            rows = [json.loads(line) for line in gzip.decompress(raw).splitlines()]
            break
        record = next(row['r03'] for row in rows if row['index'] == 36)
        replayed = geometry.result.authenticated_control(record)
        cls.state = geometry.result.require_control_vector(record, replayed)
        vectors = record['vectors']
        assessment = geometry.p01.assess(
            vectors['sourceCanonicalUtf8'].encode('ascii'),
            geometry.ancestry.original_ledgers(vectors['parentInputLedgersCanonicalUtf8']),
            [vectors['inputCanonicalUtf8'].encode('ascii')])
        assert assessment['physicalShape'] == 'provisional-positive'
        assert assessment['status'] == 'unsupported' and not assessment['admitted']
        cls.content = geometry.p01.selected_content()

    def test_all_four_axis_placement_shapes_have_unique_bounded_routes(self):
        shapes = geometry.certify_row(self.state, self.content)
        self.assertEqual(len(shapes), 4)
        self.assertEqual({(s['defenderMoves'], s['victimSide']) for s in shapes},
                         {(False, 'axis'), (False, 'commonwealth'),
                          (True, 'axis'), (True, 'commonwealth')})
        self.assertEqual(max(len(s['guard']) - 1 for s in shapes), 2)
        self.assertEqual(max(len(s['reunion']) - 1 for s in shapes), 1)

    def test_source_quantity_and_corrected_guard_capacity(self):
        self.assertEqual(geometry.source_quantity_bounds(self.rules), dict(
            maxLossToe=3, minimumSurvivingDonorToe=7,
            maxCaptiveToe=3, movingGuardCapacityToe=5))

    def test_reachable_settlement_paths_match_independent_graph(self):
        self.assertEqual(geometry.check_projected_paths(
            self.state, self.rules, self.content, None), 8)

    def test_ten_mutations_fail_closed(self):
        self.assertEqual(geometry.mutation_checks(self.state, self.content), 10)

    def test_overlong_or_blocked_custody_paths_reject(self):
        def chain(length):
            graph = {str(n): set() for n in range(length)}
            for n in range(length - 1):
                graph[str(n)].add(str(n + 1))
                graph[str(n + 1)].add(str(n))
            return graph

        with self.assertRaisesRegex(ValueError, 'guard and reunion limits'):
            geometry.bounded_custody_routes(chain(5), '0', '0', '4')
        with self.assertRaisesRegex(ValueError, 'guard and reunion limits'):
            geometry.bounded_custody_routes(chain(10), '0', '9', '0')
        with self.assertRaisesRegex(ValueError, 'one unobstructed least-cost path'):
            geometry.bounded_custody_routes(chain(3), '0', '1', '2')


if __name__ == '__main__':
    unittest.main()
