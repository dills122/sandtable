# Task005 source baseline — 2026-09-15

Read-only root checks before implementation, both exit 0:
- python3 -B docs/research/verify-combat-source-freeze.py: 36 Morale coordinates, 360 normalized
  loss coordinates, 6480 joint coordinates, 8840 settlement cases, 44208 weighted capture paths,
  12 seeded checks, five calendar and eight Movement vectors. Only accepted three-cell amendment.
- python3 -B docs/specs/verify-combat-rules-inputs-v1.py: three canonical goldens, 47 mutations,
  39 raw rejections, seven clock cases, 14 kind/budget/UTC boundaries; no runtime admission.

005-preimplementation-baseline.json hashes six existing Combat/Ruleset files for preservation.
005 remains gated on accepted004/checkpointB. These source checks are not C# implementation tests.
