# P01 AC-003 selected-content action matrix

**Status:** private, read-only, provisional. The matrix does not certify complete
source-legal action coverage, issue a public action, admit a P01 row, or open P02/P03.
It applies only to the pinned two-independent-infantry selected Content7 and the
300 authentic ordinal-two R03 terminals. In particular, it does not treat a
printed legal choice outside the accepted initial-RC exercise as illegal.

Run `python3 docs/specs/test-combat-p01-action-matrix-v1.py` and
`python3 docs/specs/verify-combat-p01-action-matrix-v1.py
/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence`.
The verifier pins the Land Rules PDF (`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`),
September errata (`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`),
summary, manifest, predicate audit and every compressed row chunk. It replays
each original CreationRequest plus eleven independent R03 ledgers, compares the
authenticated Control vector, then applies the existing participant/segment
proof before this matrix. No row label or supplied parsed World is authority.

| Source choice or obligation | Locator and selected-content fact | Matrix disposition |
| --- | --- | --- |
| Voluntary no-selection | Land §§15.21–15.29; `CMB-STEP-002/006`. Source permits an attacker to assign no assaulting unit where combat is voluntary. | Later typed closure open; no synthetic assault or empty supported result is emitted. |
| Loaded Close Assault | Land §§15.0–15.89; accepted `CMB-POL-001`. Exactly two original infantry components, one per side; 44 fresh adjacent armed shapes. | Provisional physical shape only. Weather carry-forward, complete action coverage, typed ordinal-two choice and later steps remain open. |
| Gun position and Barrage | Land §11.0 and §§12.0–12.6. Selected Content has no gun/Barrage component. | Structurally absent for these selected rows; a future gun profile fails the bounded gate. The structural steps still exist. |
| Mandatory ZOC attack and Holding Off | Land §§10.11, 10.31–10.36, common chart §9.4. Each independent battalion is one stacking point, below the more-than-one threshold. | No selected ZOC obligation; does not generalize to a different stack or content pack. |
| Retreat Before Assault | Land §§13.0–13.28; `CMB-STEP-004`, `CMB-POL-004`. The defender may choose movement or decline after Barrage. | Explicit decline is accepted policy but its ordinal-two typed receipt is unproved. Actual movement is unsupported in this bounded profile; timeout cancels rather than inventing decline. |
| Force assignment | Land §14.0 and §§15.1–15.29; `CMB-POL-001`. The selected exercise proposes both full 10-TOE Close Assault assignments. | Later sealed round open. Partial/withheld or alternate assignment is not silently mapped to full assignment; it needs its own design. |
| Anti-Armor | Land §§14.0–14.6. Selected Content has no armor or Anti-Armor component. | Structurally absent here; a future armor profile fails the bounded gate. |
| Voluntary surrender | Land §15.88; owner decision `CMB-POL-009`. | Source-legal but deliberately excluded from the initial RC bounded exercise. No complete source-legal Combat claim. |
| Automatic surrender | Land §15.88 and September errata §§15.88, 50.12. It is event-triggered; existing adjacency or post-cost zero ammunition alone is insufficient. | No surrender event at Combat Position. An actual triggering assault or enemy move requiring settlement fails closed until typed ordinal-two handling is proved. |
| Probe adjustment | Land assault modifier surface; selected-source research and `CMB-POL-001`. | Unsupported modifier, not a proven additional independent choice in these terminals. |

The authenticated rows divide into 44 provisional adjacent armed shapes, 12
adjacent paid shapes with zero acting ammunition, and 244 nonadjacent shapes.
The 12 paid shapes retain their prior assault/settlement history and cannot be
relabelled as a fresh loaded current-segment assault. Zero ammunition at the
current Position is not automatic surrender. Both actor orientations remain
represented. All 300 assessments remain `unsupported`, `admitted=false`, with
no public actions; this matrix cannot change the established 44/256 physical
partition.

The matrix's list of source modes is **bounded and non-exhaustive**. It does
not establish that every source-legal action, modifier, participant combination,
Weather effect, or mandatory obligation has been enumerated. The authenticated
original Weather determination still lacks a reviewed carry-forward bridge to
the active ordinal-two position on this branch; that work is independent. AC-003
also needs a reviewed exhaustive selected-content action argument. AC-006 typed
automatic surrender/RBA, AC-007 outward privacy, and AC-008 complete replay
gates remain separate. Any changed content, new blocker, unknown choice, or
unproved branch returns `unsupported` under the P01 contract.

## Local verification

- `python3 docs/specs/test-combat-p01-action-matrix-v1.py`: five tests passed.
  Both actor sides and all three shape classes are sampled; tests reject a
  changed paid actor ammunition balance, fresh current-segment target use,
  alternate selected unit and changed Weather-area assignment. They also
  distinguish adjacency from an actual automatic-surrender trigger.
- `python3 docs/specs/verify-combat-p01-action-matrix-v1.py
  /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence`:
  300 authenticated rows; 44 provisional positives, 256 provisional empties;
  action shapes 44/12/244; zero admitted and zero public actions.
