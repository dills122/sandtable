# Private ordinal-two P01 no-attack transition boundary v0

**Status:** typed twelfth-ledger inventory and fail-closed private reference. No P01 event is
issuable yet. This packet does not close `P01-AC-002`, `AC-003`, `AC-006`, or `AC-008`, admit a
Combat action, or activate P02/P03. The [closed inventory](combat-p01-no-attack-v0.schema.json),
[reference](verify-combat-p01-no-attack-v0.py), and [tests](test-combat-p01-no-attack-v0.py) are
local diagnostics, not a registered runtime schema.

## Predecessor and authority

The only predecessor is the authenticated R03 ordinal-two Combat Position terminal, reconstructed
from the original CreationRequest, eleven separate ordered parent input ledgers, and R03's own
Breakdown input ledger. A decoded terminal row, cached Control, private candidate digest, caller
supplied empty list, or hash copied from a report is not an authority credential. Source pins and
the exact R03 suffix, cycle, position, state version, prefix, World/RNG and Breakdown receipt must
be rechecked by the frozen R03/P01 references. Ordinal-one and ordinal-two MovementEnd proofs stay
distinct. The proposed twelfth ledger is a new ordered list of canonical P01 **input** bytes; it
must never be spliced into an R03 parent ledger or used to rewrite the predecessor.

`zeroCandidateOpeningInputFields` fixes an initial System opening command. It binds the original
creation identity, authenticated R03 Source hash, active ordinal-two cycle and Position
Determination, exact state version/prefix and R03 Breakdown receipt. The `supportCertificateHash`
must later name a separately reviewed, source-backed supported-empty certificate for this exact
state. The field cannot certify itself. Zero-candidate opening has no player/model decision, trusted
time, or deadline. The later `selectionCloseInputFields` closes no-selection using the opening
receipt. `stepInputFields` binds each exact prior version/prefix, from-position, selection receipt
and immediately preceding step receipt. All are System-authored. The inventory also fixes one
closed Event field order and no-attack step effect fields, but v0 issues **none** of these events.

The proposed ordered event path has eight events: opening, no-selection closure, then six
`combat-step-completed` events from Position Determination through Close Assault. The six successors
are Barrage, Retreat Before Assault, Force Assignment, Anti-Armor, Close Assault, and the same
cycle/slot's Reserve Release. Only the final step closes this Combat occurrence. Arriving at
Reserve Release releases no unit and does not reset history or advance the cycle. Each future event
must carry one adjacent authority version, the prior prefix, canonical input and effect, and a
domain-separated receipt derived from the unsigned event. Accepted exact retries must read back
the original event bytes and receipt; stale/reordered/duplicate/cross-cycle inputs must not append
an event. World, RNG, CP, ammo, TOE, attacked history, target use, Weather, and original R03
predecessor bytes must remain identical throughout a no-attack path.

The frozen [step policy](../design/combat-step-transitions-v1.md) and
[ordinal-one no-attack reference](combat-inherited-no-attack-v1.md) supply order and regression
patterns, **not** authority for ordinal two. A defender decline cannot be synthesized from a
no-selection or RBA timeout. The currently proposed private input parser checks exact key order,
closed fixed values, integer/identifier/hash types, duplicate keys and canonical bytes. Syntactic
validity never bypasses P01 support or creates a receipt.

## Current fail-closed result

The authentic 300-row R03 catalogue has 44 provisional positive and 256 provisional empty
physical shapes. Every P01 assessment still says `unsupported` and `admitted=false` with
`publicActions=[]`. Physical absence, including 244 nonadjacent and 12 adjacent paid/ammunitionless
rows, is **not** a certified absence of all source-legal Combat work or mandatory obligations. No
reviewed supported-empty certificate, approved zero-candidate snapshot, actual ordinal-two
segment-open/selection-close event, or twelfth-ledger event/receipt vector exists. The v0 `inspect`
boundary therefore accepts only an empty twelfth input list for diagnosis and rejects any proposed
opening or step with `CMB-P01-NA-004`. It returns no event and changes no state. Even a syntactically
valid System input with an asserted certificate hash rejects. This is a missing-proof result, not
an ordinal-two no-attack completion.

Before a future executable v1 can issue one event, independently close the complete `P01-AC-003`
action/obligation proof and supported-empty classification for each admitted shape, freeze the
exact twelfth event/Control codecs and receipt golden vectors, prove trusted actor/time/deadline
rules for any nonempty decision, and replay genuine ordinal-two inputs at every cut with mutation,
retry, restart, readback and capacity negatives. Positive selection, RBA, automatic surrender,
result/settlement, outward mapping and native/public activation remain separate gates. Do not
convert provisional-empty rows into supported-empty for test convenience.

## Local verification

```text
python3 -B docs/specs/test-combat-p01-no-attack-v0.py
python3 -B docs/specs/verify-combat-actual-moved-repeat-position-evidence-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
```

The focused tests use genuine R03 reconstruction for both actor orientations, paid and armed
states. They check canonical input negatives, forged issuance, mutated ancestry, and unchanged
unsupported output. The full audit authenticates all 300 original roots and verifies the existing
44/256/0 partition; it does not invent a twelfth event or certify no attack.
