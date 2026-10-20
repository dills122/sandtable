# Private P01 paid-commit all-44 fresh-process readback v1

**Scope:** prepared and committed paid cuts for all 44 selected positive roots
in the retained 300-row P01 profile, 22 per actor orientation. This extends
the [two-root fresh-process witness](combat-p01-paid-fresh-readback-v1.md) and
the [all-44 in-process cut matrix](combat-p01-paid-all44-cuts-v2.md). Each root
is written to two temporary physical files and read by a new Python
interpreter. The proof is private: no P01 row becomes admitted, no public or
native action is issued, and no durable host restart is exercised.

The parent rebuilds each original CreationRequest, ten pre-R03 ledgers and
R03 ledger, then the selected-assault and Force Assignment ledgers. It
derives the prepared cut and one accepted System paid input. The independent
interpreter reads both retained files, enforces the input and stored-output
byte caps, replays the original ancestry, and compares complete canonical
Control and World bytes, source/event binding and receipt IDs at both cuts.
It applies the accepted paid input again after the committed cut; the retry
must return the same Source, own ledger and receipt, and no new event.

The parent compares the fresh process's CP and ammunition counters,
attack-history and target-use edges, state version, receipt, canonical
Source/ledger hashes and private gate against its own independently computed
states. The existing all-44 conservation assertions check the exact 5/3 CP
and 10-point ammunition debits, unchanged TOE/location/other World fields,
unchanged RNG and parent Control, and one linked history/target-use edge.
The fixture pins all 44 authentic R03 source hashes, row keys, actor sides,
the original 300-row partition, the predecessor ordered output hash and
maximum retained byte sizes.

The complete fresh-process ordered output is 52,790 sorted-key JSON bytes,
fixed at
`sha256:c4e549455ecce399473751aa2d616565bba1bd52f816fc31a345fb57c220f49b`.
Its measured maxima are Source 5,627,669 bytes, input 1,004, event 5,978,
Control 44,589 and World 3,326. The aggregate independently reconstructs
the predecessor's canonical field order before comparing its earlier digest;
the two digest encodings are intentionally distinct.

CI runs four independent Python-only jobs, each reading 11 fixed roots with
at most four child processes at once. Artifacts include the tested Git SHA
and run attempt. An aggregate job requires shards 0–3 exactly once, their
ordered authentic root identities and artifact hashes, all 44 prepared and
committed cuts, the predecessor ordered output hash and exact measured cap
maxima. A local Python 3.12 benchmark of 11 roots took 175 seconds; a full
local four-shard run took 193, 186, 194 and 187 seconds per shard. No .NET
restore/build runs inside these 15-minute proof jobs. The focused tests
physically alter retained Control, ancestry, World and paid input files and
require fail-closed readback. They also reject missing or duplicate shards.

The CI-local proof compares adopted manifest and partition pins. The physical
300-row source audit must separately hash the retained evidence files; run:

    python3 -B docs/specs/test-combat-p01-paid-all44-fresh-v1.py
    python3 -B docs/specs/verify-combat-p01-paid-all44-cuts-v2.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence

This proves a genuine fresh interpreter boundary for these 44 private paid
cuts. It does not prove all-row cut coverage, durable host storage/restart,
the full AC-008 gate, or the complete P01 support certificate. AC-003 action
exhaustiveness, AC-006 full surrender/result/RBA continuation, and AC-007
outward privacy also remain open.
