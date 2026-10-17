#!/usr/bin/env python3
"""Focused R03 reference negatives against authentic R02 roots."""
import copy
import importlib.util
import json
import unittest
from pathlib import Path
from unittest.mock import patch

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "r03_reference", ROOT / "verify-combat-actual-moved-repeat-breakdown-completion-v1.py"
)
r03 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r03)


class ActualMovedRepeatBreakdownTests(unittest.TestCase):
    def test_representative_vectors(self):
        fixture = json.loads(
            (ROOT / "fixtures/combat-actual-moved-repeat-breakdown-completion-v1.json").read_bytes()
        )
        self.assertEqual(300, fixture["rowCount"])
        self.assertEqual([0, 2, 36, 41], fixture["selectedOrdinals"])
        self.assertEqual(fixture["cases"], r03.verify(fixture["selectedOrdinals"]))

    @classmethod
    def setUpClass(cls):
        cls.row = r03.r02.semantic_rows()[0]
        cls.parent, cls.args = r03.parent_terminal(cls.row)
        cls.source = r03.source_for(cls.parent)
        _, cls.context, cls.initial = r03.replay(cls.source, cls.args, [])
        cls.input = r03.raw(r03.command(cls.context, cls.initial), "Input")

    def reject(self, code, call):
        with self.assertRaises(r03.Invalid) as caught:
            call()
        self.assertEqual(f"CMB-MRB-{code:03}", caught.exception.code)

    def test_publication_and_exact_retry_preserve_parent(self):
        result, ledger, event, receipt = r03.apply(self.source, self.args, [], self.input)
        self.assertIsNotNone(event)
        source, context, state = r03.replay(result, self.args, ledger)
        self.assertTrue(state["closed"])
        self.assertEqual(state["activeCycle"], context["parent"]["activeCycle"])
        self.assertEqual(state["movementEnd"], context["parent"]["movementEnd"])
        self.assertEqual(state["originalMovementEnd"], context["parent"]["originalMovementEnd"])
        self.assertEqual((result, ledger, None, receipt),
                         r03.apply(result, self.args, ledger, self.input, False))
        self.assertEqual(state, r03.read_control(r03.raw(state, "Control"), result, self.args, ledger))
        proof = r03.proof(result, self.args, ledger)
        self.assertEqual(proof, r03.read_proof(r03.raw(proof, "Proof"), result, self.args, ledger))
        self.assertEqual(1, len(source["breakdownEventCanonicalUtf8"]))

    def test_actor_action_and_context_precede_retry(self):
        result, ledger, _, _ = r03.apply(self.source, self.args, [], self.input)
        wrong_actor = r03.parse(self.input, "Input")
        wrong_actor["actor"] = "axis"
        self.reject(4, lambda: r03.apply(result, self.args, ledger,
                                         r03.raw(wrong_actor, "Input"), False))
        wrong_action = r03.parse(self.input, "Input")
        wrong_action["command"]["actionId"] = "sha256:" + "0" * 64
        self.reject(5, lambda: r03.apply(result, self.args, ledger,
                                         r03.raw(wrong_action, "Input"), False))
        wrong_context = r03.parse(self.input, "Input")
        wrong_context["command"]["cycleId"] = "sha256:" + "0" * 64
        self.reject(4, lambda: r03.apply(result, self.args, ledger,
                                         r03.raw(wrong_context, "Input"), False))

    def test_nonterminal_parent_and_altered_ledger_reject(self):
        packet = r03.r02.parse(self.parent, "Source")
        packet["lifecycleEventCanonicalUtf8"] = []
        nonterminal = r03.source_for(r03.r02.source_bytes(packet))
        old_args = copy.deepcopy(self.args)
        old_args[-1] = []
        self.reject(3, lambda: r03.replay(nonterminal, old_args, []))
        altered = copy.deepcopy(self.args)
        altered[-1] = []
        self.reject(4, lambda: r03.replay(self.source, altered, []))

    def test_event_claim_and_canonical_framing_reject(self):
        result, ledger, _, _ = r03.apply(self.source, self.args, [], self.input)
        packet = r03.parse(result, "Source")
        event = json.loads(packet["breakdownEventCanonicalUtf8"][0])
        event["effect"]["movementCompletionReceiptId"] = "forged"
        packet["breakdownEventCanonicalUtf8"][0] = r03.raw(event, "Event").decode("ascii")
        self.reject(6, lambda: r03.replay(r03.raw(packet, "Source"), self.args, ledger))
        self.reject(1, lambda: r03.replay(b'{"contractVersion":1,"contractVersion":1}',
                                          self.args, []))

    def test_forged_readbacks_and_dependency_pin_reject(self):
        result, ledger, _, _ = r03.apply(self.source, self.args, [], self.input)
        _, _, control = r03.replay(result, self.args, ledger)
        altered = copy.deepcopy(control)
        altered["world"]["elements"][0]["currentLocationId"] = "forged"
        self.reject(6, lambda: r03.read_control(r03.raw(altered, "Control"),
                                                result, self.args, ledger))
        proof = r03.proof(result, self.args, ledger)
        proof["base"]["movedRepeatMovementCompletionProof"]["sourceHash"] = "sha256:" + "0" * 64
        self.reject(6, lambda: r03.read_proof(r03.raw(proof, "Proof"),
                                              result, self.args, ledger))
        pinned = "docs/specs/verify-combat-actual-moved-repeat-movement-completion-v1.py"
        with patch.dict(r03.PINS, {pinned: "0" * 64}):
            self.reject(9, lambda: r03.replay(self.source, self.args, []))


if __name__ == "__main__":
    unittest.main()
