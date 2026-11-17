# MVP-GATE-01 scenario-freeze: independent-reading backlog — November 17, 2026

**Status:** handoff packet for a session with access to the original source
scans. This is not a new research direction — it's the next unworked slice of
`MVP-GATE-01`'s own first requirement ("independent double entry... in two
independent passes; reconcile IDs and counts," per the
[six-turn surface inventory](../../research/2026-10-18-graziani-six-turn-surface-inventory.md)).
The repo already has a working double-entry pipeline for this
(`docs/work/plans/graziani-*-review/`: a `README.md` with exact source
citations, `reading-a.json` as the first pass, `reading-template.json` as a
blank copy for the next reader, and — once two readings exist —
`comparison*.json` reconciling them field by field). This handoff just lists
which packets in that pipeline still need their next reading, so codex can
work the backlog instead of inventing a new approach.

**Ground rule, same as always:** don't invent a value. If a reading can't
determine a printed field, record it as such (`null`/`not-listed`/the literal
printed mark) per each packet's own README — never a guessed number. Do not
open `reading-a.json` (or `reading-b.json` where present) before filling your
own copy of `reading-template.json` — the whole point is a blind second (or
third) opinion.

## Packets with no independent entry yet (highest value — these haven't been double-checked at all)

- **`docs/work/plans/graziani-arrival-review/`** — six Commonwealth arrival
  schedule rows + eight grouped OA checks (21 candidate printed OA rows,
  scenario pp. per its README). No independent entry exists for the whole
  manifest yet; only a narrower earlier pilot (6th NZ schedule/OA conflict)
  has been checked, which doesn't cover the rest.
- **`docs/work/plans/graziani-convoy-review/`** — 11 grouped records: Axis
  convoy restriction, cargo, lane limits, attack, chart input, loss rounding,
  qualifying-division definition, one 5% chart cell, plus the detailed Air
  rule and its erratum. Blank second entry.
- **`docs/work/plans/graziani-rail-review/`** — 10 grouped records: scenario
  railhead, Land rail, errata, construction, detailed Axis rail, simplified
  supply rail. Blank second entry.
- **`docs/work/plans/graziani-victory-review/`** — 8 grouped rows covering
  the scenario's printed victory cross-reference, named victory heading, and
  all six victory categories (§60.81). Directly feeds the "Termination and
  victory" row of the surface inventory, which is otherwise unstarted. Blank
  second entry.
- **`docs/work/plans/graziani-transport-review/`** — 9 grouped records:
  Land §32.14, §§32.31–32.37, and scenario §59.66(C). Blank second entry.

## Packets with a second reading, but not a blind one (need a genuinely independent third pass)

- **`docs/work/plans/graziani-supply-review/`** — 13 fixed §60.92(B–C) supply
  rows + 2 grouped placement/dummy statements. Reading B (Nov 16) matched
  Reading A on all 50 fields, but Reading B's own note says the reader had
  prior planning context, so it doesn't count as independent — 15
  locator-wording differences and the independent-review gate are still open.
- **`docs/work/plans/graziani-truck-review/`** — 32 rows covering explicit
  first-line truck counts (§§60.31/60.41) and all seven second/third-line
  pools (§§60.33/60.43). Same situation: Reading B matched all 96 fields but
  wasn't blind; 32 locator-wording differences still open.

For these two, the next reading should be done by someone who has **not**
seen either `reading-a.json` or `reading-b.json`/`comparison-b.json`, or this
session's supply/truck planning docs — otherwise it's a fourth non-independent
pass, not the independent check the gate needs.

## What NOT to do here

Don't re-derive scope, don't invent a new source matrix, and don't touch
anything outside the packets above — each `README.md` already states exactly
what rows to check, which page/section to read, and how to encode an
unreadable field. If a packet's README and its target source conflict with
what's described here, the README is authoritative (it's closer to the
source).

## Handoff notes

- These 7 packets don't block each other or the forces-incomplete/CCNN/Matruh
  work already handed off separately — independent track, can run in
  parallel.
- Once a packet gets its qualifying independent reading, the next step is
  generating/checking its `comparison*.json` (see the supply/truck packets'
  existing `comparison-b.json` for the expected shape) and updating its
  README's status line — not implementation. Implementation and gate closure
  stay a separate, later step per the inventory doc's own 5-item work list.
- This list does not itself close `MVP-GATE-01` — the inventory doc's
  reachability pass (item 2) and victory/content/rights decisions (items
  3–4) are separate, larger pieces of work, not attempted here.
