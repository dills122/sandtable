# P01 AC-005 bounded geometry proof

**Status:** draft executable proof for review; P01 admission remains unsupported. This checks
the selected six Clear hex infantry exercise, not arbitrary campaign terrain or all legal Combat.
The verifier emits aggregate counts only and does not create a command, event, action or public
candidate.

## Authority and sources

Run `python3 docs/specs/verify-combat-p01-geometry-gates-v1.py --evidence /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence`.
The proof first authenticates all 300 original CreationRequests plus eleven separate parent
ledgers and the R03 terminal through the frozen reference. It hashes the pinned summary, manifest,
predicate audit, eleven compressed chunks and both physical rule PDFs. Each of the 44 selected
positive terminals is independently replayed again and byte-compared with its retained Control;
current World, original participants, independent representations and ordinal-two actor side are
read from that authenticated state. The 256 others remain **provisional empty**, not
`supported-empty`.

Printed constraints: Land §15.82 (PDF p24) supplies the one-hex retreat away from attacker toward
friendly supply and its one CP; Land §§28.11–28.25 (PDF p40) supply prisoner relocation up to three
hexes, guard TOE, and escape reunion within eight CP. September errata §28.17 (PDF p2) corrects
the moving guard ratio to 1:5. The fixed pre-retreat capture origin, sole guard rendezvous and
immediate unguarded escape are adopted digital choices under `CMB-POL-004`/`005` and
`CMB-SET-004`, not claims that
the printed rules specify those transaction boundaries. Source PDF and selected Content7 SHA-256
pins are checked before geometry; the result gate checks the selected 360 chart cells including
the three-cell amendment.

## Bounded checks

- All 44 authentic positives have one original independent infantry battalion per side, exact
  representation/location binding, TOE 10, ammo 10, Cohesion 0, fresh target history and current
  CP 4/0. The two one-stacking-point battalions have no mandatory ZOC attack. Any added occupant,
  different resource, active custody/obligation, feature or terrain change rejects.
- The pinned graph has one defender retreat hex for either actor orientation. That destination is
  unoccupied, moves farther from attacker, moves one edge toward the defender's only friendly
  supply anchor, and adds one CP after defense cost without crossing CPA 10.
- For each of the 44 rows, both defender dispositions and both capture roles yield four placement
  shapes: 88 Axis and 88 Commonwealth checks, eight distinct orientation/placement cases. The
  independent graph walk bounds guard relocation at two Clear hexes and escape reunion at one CP.
  Guard paths cannot traverse the victim's final occupied hex.
- The pinned AC-004 source/result matrix is rerun on this head: 9,728 projected settlement Worlds
  per actor orientation. Its exact loss/settlement comparison is required before geometry relies
  on the chart bounds. The normalized selected chart conservatively bounds loss at three TOE,
  survivor donor at seven TOE, captive lot at three TOE; one TOE guard covers that lot under the
  corrected moving 1:5 rule. Sixteen source-reachable guard/escape projection witnesses compare
  actual settlement routes with independently walked paths. The geometry-only combination of
  defender capture **and** required defender retreat lacks a source chart witness; its placement
  path is tested as a policy shape, never asserted reachable.
- Ten fail-sensitive state/content mutations reject (edge feature, terrain, supply anchor,
  alternate edge, added blocker, donor TOE, pre-assault ammo, surrender-threshold Cohesion,
  representation and retreat CP). Focused tests also reject overlong guard or reunion paths and
  an occupied intermediary. In particular, post-commit attacker ammo zero is not mistaken for
  a fresh automatic ammunitionless surrender trigger.

## Gate limit

This proves only the selected **physical** and projected placement paths. It does not implement
owner retreat/custody decisions, timeout fallback, complete automatic-surrender handling,
outward identity, hidden-state privacy equivalence, durable obligations, P02 selection or P03
native consumption. `CMB-POL-009` excludes voluntary surrender for the initial RC bounded
exercise and does not make all source-legal Combat actions available. P01 `status=unsupported`,
`admitted=false` and empty public actions remain unchanged. Full support certificate and
independent review are still required before any freeze or activation.

## Local verification

On this branch:

```text
PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/test-combat-p01-geometry-gates-v1.py
PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-p01-geometry-gates-v1.py --evidence /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
```

Local result on this branch: **5 tests passed**; full proof returned 300 authentic rows,
44 provisional positives, 256 provisional empties, zero admitted, 88 placement checks per
acting side, 16 source-reachable projected custody routes, two-hex maximum guard route,
one-CP maximum reunion, source quantity bounds of three lost/captive TOE and seven surviving
donor TOE, the complete 9,728 settlement Worlds per side, and ten rejected state/content
mutations. Record the commit and exact-head CI result in the PR.
