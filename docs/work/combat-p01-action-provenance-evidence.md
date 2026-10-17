# P01 AC-003 bounded participant and action provenance

**Status:** private, read-only, provisional. This is a structural evidence slice, not an
AC-003 support certificate, legal-action generator, selection opening, or native admission.
Every row stays `unsupported` with `admitted=false` and no public action.

Run `python3 docs/specs/test-combat-p01-action-provenance-v1.py` for fail-sensitive
samples and `python3 docs/specs/verify-combat-p01-action-provenance-v1.py
/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence` for the authentic
300-row audit. The latter verifies retained source, errata, summary, manifest, predicate
audit and compressed-chunk SHA-256 pins. For each row it replays the original
CreationRequest plus eleven distinct R03 parent ledgers, compares the exact terminal
Control vector, and checks the selected content against the authenticated creation
scope. The retained predicate audit's 44/256 partition is checked against the
independently computed provisional physical shape, never used as an authority input.

## Checked bounded facts

- Original and active acting side agree. The sole acting movement member, both
  MovementEnd membership lists, World elements, selected Content elements and
  representations bind to the same two creation-scoped original units.
- Each selected side has exactly one independent infantry battalion/component.
  Representation IDs are unique, bind one-to-one to the original elements and
  match their current World locations. The current MovementEnd locations match
  the World; the original ordinal-one MovementEnd remains a distinct history.
- The target hex contains exactly the selected opposing defender; the actor is
  elsewhere. A provisional positive also has an adjacent feature-free edge,
  no current or prior segment target use, no attack history, no carried future
  obligation and no movement exclusion. A paid row must have no current segment
  target use and exactly one prior use, attack-history record and settlement.
  Their target, segment, commitment, original-cycle ID, actors and stage must
  agree with the authenticated current defender and original-unit identities.
  That prior paid attack cannot be reinterpreted as a fresh loaded attack.
- Pinned selected Content has only two infantry components, so no alternate
  **selected-content unit or weapon component** can enter this bounded pair.
  One battalion on each occupied hex is below the mandatory ZOC assault
  threshold of Land §10.11 and its stacking chart; this bounded interpretation
  is tied to the retained Land PDF pin.
- All six selected hexes map to weather area `a`. This is an area assignment,
  not a Weather determination.

## Open gates

The authenticated P01 terminal and its eleven ancestor inputs carry **no
current operation-stage Weather kind** for the armed rows. Area `a` cannot prove
`normal`; hot, sandstorm and rainstorm exclusions therefore remain unverified.
The selected-content check also cannot establish absence of every source-legal
action mode (including source-permitted voluntary surrender, which policy
`CMB-POL-009` excludes from this initial RC exercise). It proves only the bounded
participant/component alternatives stated above. The verifier exposes
`currentWeatherKindProved=false` and `sourceLegalActionExhaustive=false` on every
row. A later authenticated Weather input and source-backed action decision must
close these gates before AC-003 can be certified or P01 admitted. Automatic
surrender and decline-only RBA remain separate AC-006 gates.

This slice must not change the 44 provisional positive / 256 provisional empty
partition, publish a supported-empty result, or activate P02/P03.

## Local result

- `python3 docs/specs/test-combat-p01-action-provenance-v1.py`: six tests passed,
  including mutated creation/cycle identity, movement membership, representation,
  current/prior paid target and settlement history, alternate component and
  Weather-area negatives.
- `python3 docs/specs/verify-combat-p01-action-provenance-v1.py
  /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence`:
  300 authenticated rows; 44 provisional positive, 256 provisional empty;
  current Weather kind unproved, source-legal action exhaustiveness unproved,
  zero admitted.
