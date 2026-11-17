# MVP-GATE-01 scenario-freeze: independent-reading backlog — November 17, 2026

**Update (2026-11-17, later the same day):** codex completed this backlog —
and went further than requested. All 7 originally-listed packets now have a
genuinely independent second reading, with real comparisons recorded in
[`2026-11-17-graziani-independent-reviews.md`](2026-11-17-graziani-independent-reviews.md).
Codex also independently read several packets this handoff never asked for
(rules, victory, fortification, port, weather, patrol, French Marine,
Cirene/Marmarica formation/relationships) — see that same disposition doc.
This file is kept as a record of what was originally asked for and the
verified outcome; see "Outcome" below instead of treating the sections after
it as still open.

## Outcome

Checked every reported "disagreement" across all 7 original packets myself
before trusting this: every one is cosmetic (comma vs semicolon, en-dash vs
hyphen, abbreviation vs full phrase, whitespace around `×`). Zero are a real
factual disagreement about what the source says. The independent-reading
requirement is substantively satisfied for:

- **`graziani-supply-review/`** — all 50 fields agree, 15 locator-wording differences only.
- **`graziani-truck-review/`** — all 96 fields agree, 32 locator-wording differences only.
- **`graziani-arrival-review/`** (both the schedule and member manifests) — values agree, wording/whitespace differences only.
- **`graziani-victory-review/`** — values agree; one genuine spelling variant worth keeping (`Grazianni` in the §60.81 heading vs `Graziani` in §60.22) — preserve both as printed, don't normalize.
- **`graziani-convoy-review/`, `graziani-rail-review/`, `graziani-transport-review/`** — see "Genuinely new findings" below; these have real substantive content beyond wording, not just confirmation.

**Genuinely new findings worth a human's attention** (not closed, these are new real items, not wording noise):

- **Rules** (not originally in this handoff, codex read it anyway): Land §20.15's Benghazi diversion condition depends on more preconditions (no Commonwealth land-combat unit west of Mersa Matruh inclusive, stacking, port efficiency, unit-equivalent and transport limits) than the first reading's destination-only field captured. Separately, Land §32.42's "one supply unit per three received may go to Alexandria" ratio isn't unambiguously encoded by the first reading's `units_per_alternative: 3` field.
- **Port** (also not originally requested): §55.14's port-capacity-reduction rounding direction is **upward** to the nearest ton — the first reading's "nearest ton" alone lost that direction. The Tobruk 7/5/3-level source conflict flagged earlier this session remains genuinely unresolved.
- **Weather** (also not originally requested): a real representation gap — boolean vs count for the foul-weather location die — plus a confirmed distinction between errata §29.1 and §29.61's different spring-boundary examples that must both be preserved.
- **Convoy**: Axis chart §32.66 requires the *qualifying* Allied division itself to be complete/non-shell, in addition to the already-known requirement that another non-shell division be in the specified relative position — the first reading only captured the latter. Real rule gap, not wording.
- **Transport**: the independent reader deliberately preserved the printed `Torbruk` spelling (Land §32.36) distinct from the canonical `Tobruk` port identity — not a typo to fix, a source-spelling-vs-game-identity distinction to keep separate when implementing.
- **Patrol** (also not originally requested): Land p.45's §16.51 addendum says the patroller does not identify constituent TOE-point types — confirmed by a follow-up independent check; the original `null` field stands correctly, not an oversight.

None of this closes `MVP-GATE-01` by itself — the inventory doc's reachability
pass and victory/content/rights decisions remain separate, larger work. This
backlog's own job (independent reading) is done; what's listed above is new
input to that larger work, not a reopening of this handoff.

---

## Original ask (for reference — see Outcome above for current state)

Handoff packet for a session with access to the original source scans. Not a
new research direction — the next unworked slice of `MVP-GATE-01`'s own first
requirement ("independent double entry... in two independent passes;
reconcile IDs and counts," per the
[six-turn surface inventory](../../research/2026-10-18-graziani-six-turn-surface-inventory.md)).
Ground rule, same as always: don't invent a value.

Packets originally listed as needing their first independent reading:
`graziani-arrival-review/`, `graziani-convoy-review/`, `graziani-rail-review/`,
`graziani-victory-review/`, `graziani-transport-review/`. Packets listed as
needing a genuinely blind reading (prior "second" reading wasn't blind):
`graziani-supply-review/`, `graziani-truck-review/`.
