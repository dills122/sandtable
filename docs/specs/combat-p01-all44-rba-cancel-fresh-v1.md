# Private all-44 selected-assault System cancellation cold readback

**Status:** bounded test proof only. No P01 admission, native/public Combat,
durable host restart, full AC-006/008 closure, or hosted clock result. The
cancellation uses supplied trusted **TEST** time at the recorded deadline.
It never invents a defender decline.

The verifier reconstructs each original 44 provisional-positive R03 roots
(22 Axis, 22 Commonwealth) from CreationRequest, ten separate parent input
ledgers, and frozen R03 ledger. It checks original R03 Source hashes against
the reviewed paid-cut fixture. The private selected-assault reference supplies
attacker TEST selection, two System steps through Barrage, and defender-owned
RBA opening. No original owner choice or recorded defender decision is claimed.

At the RBA-open cut, a separate Python interpreter replays retained Source,
CreationRequest, eleven predecessor ledgers, five selected inputs, canonical
Control, World, RNG and receipt. Exact opening retry returns the same receipt
without event. Trusted TEST System input at deadline cancels RBA without
synthesizing decline. Four ordered no-work System steps close at same-slot
Reserve Release. Each input retries without new event or byte mutation. Fresh
interpreter checks actor, decision, clock gate, previous receipt, cycle,
version, prefix, source pins, event order, and immutable parent World/RNG and
resources. Parent and child compare Source, ledger, event, receipt, Control,
prefix, version and position bytes at all five continuation cuts. All states
remain `admitted=false`, `publicActions=[]`.

Frozen output binds each original root, orientation, R03 Source/ledger,
RBA-open Source/ledger/Control/receipt, cancellation receipt, five ordered
continuation cuts, unchanged World/RNG. Four 11-root shard jobs retain
same-attempt artifacts; aggregate checks all roots against one frozen hash and
exact tested Git SHA. Original 300-root audit remains 44 selected candidates,
256 candidate absent, zero admitted/public.

Focused negatives reject wrong System actor, decision, premature time,
unavailable-flag mismatch, mutated retained Source/Control/ancestry,
nonidentical retry, skipped cancellation receipt, wrong intermediate actor,
and wrong terminal predecessor receipt. No real host clock, durable restart,
owner-supplied defender response or public action is introduced.

Run focused checks:

```sh
python3 -B docs/specs/test-combat-p01-all44-rba-cancel-fresh-v1.py
python3 -B docs/specs/verify-combat-p01-all44-rba-cancel-fresh-v1.py --jobs 4
python3 -B docs/specs/verify-combat-p01-selected-assault-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
```
