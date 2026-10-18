# Private P01 ordinal-two no-assault continuation with explicit RBA v1

**Status:** executable private reference for the authenticated, nonadjacent singleton-infantry
subset only. This is a versioned successor to the frozen, unissuable
[no-attack v0 inventory](combat-p01-no-attack-v0.md); it does not change v0 bytes. It does not
admit a public or native P01 action, activate P02/P03, or close full `P01-AC-003`, `006`, `007`,
or `008`. [Schema](combat-p01-explicit-rba-v1.schema.json),
[reference](verify-combat-p01-explicit-rba-v1.py), and
[tests](test-combat-p01-explicit-rba-v1.py) specify the bounded path. The
[r000-c02 vector](fixtures/combat-p01-explicit-rba-v1.json) freezes all nine receipts and the
terminal prefix.

## Source and policy boundary

The retained Land PDF SHA-256 is
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Land §11.0 (printed p18) permits the phasing side to attack adjacent enemy hexes and requires
combat for adjacent opposing controlled hexes; §10.11/§10.31 constrain ZOC attacks. The pinned
two-infantry content has one unit per side and no adjacent opposing target in this slice. Its
closed content graph, complete original-unit/representation binding, current ordinal-two
target-use and obligation absence, authenticated Normal Weather, exact R03 suffix and source
pins provide a **zero-assault-only** certificate. This certificate does **not** say that Combat
has no remaining work. Land §11 procedure and §§13.0–13.24 (printed p18, p21) allow the
unpinned non-phasing defender to Retreat Before Assault after Barrage even if not initially
adjacent. The certificate also checks defender CP headroom and a clear unoccupied retreat edge;
it does not perform movement. The defender must have a real choice. The 12 adjacent paid/ammunitionless rows are
outside this certificate; no blanket conversion of all 256 provisional-empty rows occurs.
The pinned original 300-root audit replays every ancestor and finds exactly 244 qualifying
nonadjacent rows and 56 excluded rows (44 adjacent armed and 12 adjacent paid). This audit
classifies predecessor roots; executable trace tests check no admission or public actions.

`CMB-POL-001` limits this bounded exercise to an explicit RBA **decline**; actual RBA movement
remains unsupported. `CMB-POL-004` makes a missing/expired/unavailable defender response cancel
without a synthetic decline. The accepted voluntary-surrender exclusion `CMB-POL-009` remains;
automatic surrender still fails closed at an actual trigger. This path has no assault trigger,
resource payment, attack commitment, result draw or settlement.

## Authority and transition

The only predecessor is original CreationRequest plus eleven **separate**, ordered R03 parent
input ledgers and the R03 Breakdown ledger. The reference reconstructs them with the frozen R03
verifier before issuing or reading any twelfth-ledger event. Its `Source` carries the exact R03
source bytes and canonical own events. Its separate own input ledger is independently retained;
each replay derives the expected event from the trusted input and compares exact bytes. A
decoded R03 row, copied hash, claimed empty candidate list, or the zero-assault certificate ID
alone is never authority.

The input `actor` is a trusted admission record supplied by the authority layer, **not** an
untrusted client's self-declared side. This private reference checks the defender original side
and original UnitKey against the authenticated World. It supplies no hosted seat, transport or
public action. Clock availability and timestamps are also trusted admission facts, never
client-selected cancellation flags. The RBA deadline uses the pinned timing configuration whose
hash equals the
active cycle policy digest. It opens once at the actual RBA position, after the exact Barrage
receipt. Accepted decline requires the exact decision ID, own participant, defender actor and a
trusted monotonic time strictly before the deadline. A wrong-side, changed-participant, late,
stale, duplicate-different or movement command cannot create a decline. A System expiry at or
after the deadline, or trusted clock unavailability or regression below the persisted RBA
opening timestamp, creates a distinct cancellation receipt;
there is no inferred decline. The deadline cannot be refreshed on replay or retry.

The schema fixes three distinct event orders: nine-event `acceptedDecline` has an explicit
defender decline at event five; nine-event `cancelledDecision` has a System cancellation there;
eight-event `preOpenCancelled` cancels immediately after Barrage if the trusted clock is
unavailable before the decision can open. No decision ID, deadline, or synthetic opening is
created on the pre-opening path. The paths share these ordered steps:

1. System closes zero-assault selection using the authenticated certificate.
2. System completes Position Determination and Barrage as no-assault structural steps.
3. System opens the defender-owned RBA decision at the RBA position when trusted clock is
   available. If it is unavailable before opening, System records pre-opening cancellation.
4. For an opened decision, defender explicitly declines RBA, or System cancels on
   deadline/unavailability.
5. System completes RBA, Force Assignment, Anti-Armor and Close Assault, arriving at the **same
   cycle and slot's Reserve Release**.

Structural System step completions carry no timestamp and may proceed after clock unavailability;
they cannot create a defender decline or renew a decision window.

There is no second segment-completed event. Selection and each step have separate `p01s.` and
`p01n.` receipts; RBA opening and decline/cancellation use `p01o.` and `p01r.`. Receipt hashes
are domain-separated SHA-256 of the canonical unsigned event. Pre-opening cancellation has its
own `p01c.` receipt. One accepted event increments
the state version and exact prefix once. Every input binds cycle, prior version/prefix, current
position and preceding receipt. Accepted identical input retry returns the original receipt and
adds no event. Source and input replay must agree at every cut. World, RNG, CP, ammo, TOE,
Weather, custody, attack history, target use, original R03 bytes, and all other resource facts
remain identical. Arriving at Reserve Release does not release a unit or advance the cycle.

Both `cancel-rba` paths reach the same structural no-assault end with a cancellation receipt and
**no decline receipt**. A pending decision cannot complete RBA. Movement is never silently
converted to decline. The reference output remains `admitted=false` and `publicActions=[]` for
every state, including completed private traces.

## Remaining gates

This bounded path is not the complete P01 support certificate. In particular, source-legal
action exhaustiveness beyond the chosen nonadjacent subset, actual ordinal-two automatic
surrender and selected-assault RBA, approved outward owner fields/equal-authorized hidden
histories, full 300-row twelfth-ledger cuts/mutations/restart/readback and enforced resource caps
remain open. No P02 selection or P03 native consumer may activate from this private proof.

## Local verification

```text
python3 -B docs/specs/test-combat-p01-explicit-rba-v1.py
python3 -B docs/specs/verify-combat-p01-explicit-rba-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
```

The first command exercises both authentic actor orientations, explicit decline, timeout
cancellation before and after decision opening, receipt retry, nonterminal replay, ancestry and event mutations, wrong actor,
late decision, attempted RBA movement and adjacent positive rejection. The second replays the
original pinned 300-row R03 catalogue with source PDF pins, then checks the narrower 244/56
certificate partition. Neither command proves full P01 admission.
