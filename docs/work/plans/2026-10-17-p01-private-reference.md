# P01 private reference implementation slice

Base: reviewed P01 contract head `ac151cb`. Scope: additive schema, read-only reference and
focused authentic tests. No native or public consumer, selection transition, settlement or
support certificate.

1. RED: authenticate genuine paid and armed R03 terminals from frozen replay; assert provisional
   44/256 physical partition for representative rows, retained state/RNG, strict ledger rejection,
   and absence of action publication. Test must fail while P01 module is missing.
2. GREEN: define closed, versioned assessment schema. Reuse R03 `replay` with original
   CreationRequest and all eleven independent ledgers. Require exact one-event R03 terminal,
   ordinal-two Combat Position suffix, unchanged original and active proof identities, and
   pinned source profile. Compute provisional physical shape from authenticated World and selected
   content, not supplied row labels or parsed state. Derive private candidate identity only from
   authenticated authority and participant bindings. Return `unsupported` admission for positive
   shapes until complete result, geometry and privacy proof exists; return no executable action.
3. Check representative genuine roots, physical negatives, ledger corruption, deterministic
   identity and state/RNG equality. Publish exact tests and unclosed gates. Full 300-row replay,
   exhaustive result/geometry/privacy proof and P02/P03 remain follow-ups.

Stop if R03 replay cannot authenticate original ledgers on this base, selected content shape cannot
be derived from trusted source, or a test would need caller-supplied World/hash as authority.

## Local execution evidence

- RED: direct focused test failed with `FileNotFoundError` for the absent P01 verifier before
  implementation.
- GREEN: `python3 docs/specs/test-combat-actual-moved-repeat-position-v1.py` passes six focused
  tests on genuine R03 paid/armed roots, both acting sides, deterministic identity, ledger/source/
  nonterminal rejections and physical fail-closed variants.
- Retained-vector-only classification of all 11 pinned compressed chunks gives 44 provisional
  positive and 256 provisional empty, zero mismatches. This cross-check reads authenticated R03
  *output vectors* without rerunning 300 independent ancestors; it is not P01-AC-001 or P01-AC-008.
- All assessment results remain `unsupported`, `admitted=false`, with empty public actions. The
  companion JSON fixture is a compact evidence index, not a forged Source or support certificate.
