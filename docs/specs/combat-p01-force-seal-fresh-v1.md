# Private P01 Force Assignment one-seal fresh readback v1

**Status:** bounded private proof only. This evidence does not issue a public
action, admit P01, close an assault, or restart an Orleans host. It demonstrates
two original positive roots through one owner seal and a second owner seal in a
new Python interpreter. The complete AC-003, AC-006, AC-007 and AC-008 support
certificate remains open.

The exact original CreationRequest and eleven independent parent ledgers open
the R03 position. A seven-input selected-assault ledger binds an attacker
selection and defender-owned RBA decline. The Force Assignment Source is
derived from that immutable selected Source. The two retained Force Assignment
inputs are the System opening and one authenticated owner seal. The private
reference checks the pinned Content, participant/slot/allocation, actor, clock,
prior version/prefix, previous receipt and resource conservation at each
transition. The child process receives those exact Source and input bytes plus
canonical retained Control, World, RNG and receipt; it does not receive a live
interpreter state.

The two examples are original catalogue rows `r012-c01` and `r034-c01`,
covering opposite acting-side orientations and opposite first-seal roles.
The new interpreter replays the full ancestry to **Collecting**, checks the
retained Control/World/RNG/version/prefix/receipt, retries the same first seal
without a new event, then appends the other owner seal to **Prepared**. The
parent independently derives and compares exact second Source, input ledger,
event, receipt and Control bytes. Every accepted cut has `admitted=false`,
`publicActions=[]`, and unchanged World/RNG/resources.

The golden fixture freezes source, ledger, event, receipt, Control and prefix
hashes and the ordered two-row output hash. Negative cases mutate Source alone,
ledger alone, missing/duplicate event, first/second owner, clock and previous
receipt, plus expected Control/World/RNG. Each must reject in the cold reader.
The local full pinned 300-row audit remains 44 provisional selected candidates,
256 absent, zero admitted or public actions.

This proves a Python process boundary, not durable campaign storage or a host
restart. It covers two roots, not all 44 positive Force Assignment cuts, the
256 candidate-absent roots, all nonterminal cuts, physical dependency faults,
or every mutation. A later all-row certificate must retain exact source and
ledger ancestry, retry conservation and byte caps, and cannot infer public
support from this proof.

```sh
python3 -B docs/specs/test-combat-p01-force-seal-fresh-v1.py
python3 -B docs/specs/verify-combat-p01-force-seal-fresh-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
```
