# Private all-44 selected-assault RBA decline cold readback

**Status:** bounded test proof only. No P01 admission, public/native action,
durable host claim, full AC-006/008 closure, or authentic recorded defender
decision is established here.

The verifier reconstructs each of the original 44 provisional-positive R03
roots (22 Axis, 22 Commonwealth) from the original CreationRequest, ten
separate earlier input ledgers, and the frozen R03 ledger. Each reconstructed
R03 Source hash must match the reviewed all-44 paid-cut fixture. The existing
private selected-assault reference then admits a typed TEST attacker selection,
two no-work steps through Barrage, and a defender-owned RBA-open event. These
test inputs are supplied by this verifier at fixed trusted test times 1000,
1001, and 1002; they are not an original recorded owner choice.

At the RBA-open cut, a separate Python interpreter parses the physically
retained Source, CreationRequest, all eleven predecessor ledgers, five selected
inputs, canonical Control, World, RNG, and receipt. It replays the entire
ancestry and asserts byte identity. Repeating the exact open input returns its
receipt without an event. The interpreter then applies one typed TEST decline
at trusted time 1003, which is before the derived deadline. Actor, participant,
decision, previous receipt, clock, cycle, prefix and version are checked by
the selected-assault reference. The emitted event, updated Source and ledger,
receipt, Control, version and prefix must match the parent interpreter byte for
byte. World, RNG, CP, ammunition and other parent resources remain unchanged.

The frozen 44-row output commits to each original root, orientation, R03
Source/ledger, RBA-open Source/ledger/Control/receipt, declined event,
Source/ledger/Control/receipt, and unchanged World/RNG. Four 11-root shard jobs
are checked against the same frozen aggregate and exact Git commit SHA. The
original 300-row private audit still reports 44 candidate positive, 256
candidate absent, and zero admitted/public.

Focused negatives reject wrong actor, decision, participant, receipt, deadline
equality, unavailable clock, tampered retained Source/ledger/Control and a
nonidentical retry. This tests typed admission and replay, not a production
source of trusted actor identity or clock time; neither is added here. It also
does not prove durable Orleans restart or genuine owner-supplied decisions.

Run focused checks:

```sh
python3 -B docs/specs/test-combat-p01-all44-rba-decline-fresh-v1.py
python3 -B docs/specs/verify-combat-p01-all44-rba-decline-fresh-v1.py --jobs 4
python3 -B docs/specs/verify-combat-p01-selected-assault-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
```
