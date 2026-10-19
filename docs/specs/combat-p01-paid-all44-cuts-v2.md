# Private P01 paid-commit all-44 cut matrix v2

**Scope:** deterministic in-process prepared and committed cut readback for all
44 selected positive roots in the retained 300-row P01 profile (22 per actor
orientation). This extends the two-root fresh-interpreter proof in
[paid fresh readback v1](combat-p01-paid-fresh-readback-v1.md); it does not make
all 44 cuts a fresh-process or durable-host readback.

The [fixture](fixtures/combat-p01-paid-all44-cuts-v2.json) fixes row order,
row keys and each authentic R03 source hash from the pinned retained profile.
For each root, the [verifier](verify-combat-p01-paid-all44-cuts-v2.py) rebuilds
the original CreationRequest, ten pre-R03 ledgers and the R03 ledger, then
generates the accepted selected-assault and Force Assignment ledgers. It
replays the prepared source, commits its exact System input, and replays the
committed source plus one-event own ledger. Canonical Control and World bytes
and receipt IDs agree at each cut. An accepted retry returns identical source,
ledger and receipt, and no event.

Independent assertions account for each role's CP and ammunition debit,
unchanged TOE/location/other World fields, unchanged RNG and parent Control,
one linked attack-history edge and target use, and zero admission/public
actions. The ordered output digest covers all 44 root IDs, prepared and
committed Control/World hashes, receipts, event hashes and measured byte sizes.
It is `sha256:07dce9fe0ab8666bb580b6cb06d6eee0d13ec098e1679eb0d5badb07be32cd50`
over 35,982 canonical bytes. Maximum retained source is 5,627,669 bytes,
input 1,004, event 5,978, Control 44,589 and World 3,326. All fall within the
private schema's 6 MiB, 4 KiB, 8 KiB, 64 KiB and 16 KiB caps respectively.
These are measured on this pinned profile, not host persistence limits.

The physical-source audit remains the authority for the 300-row partition:
44 selected positives, 256 candidate-absent rows, zero admitted or published.
The CI-local matrix fixes that partition and authenticates its 44 positive
sources; it does not reclassify all 256 negative roots. Fixture manifest and
partition SHA-256 pins must match the paid reference's adopted pins in every
CI-local run. The physical audit mode also hashes both retained files before
the 300-row audit starts. Run both checks:

    python3 -B docs/specs/test-combat-p01-paid-all44-cuts-v2.py
    python3 -B docs/specs/verify-combat-p01-paid-all44-cuts-v2.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence

The second command requires the retained private evidence directory and pins
its physical source files, manifest and compressed row chunks. AC-008 durable
host restart/readback and full all-row cut coverage remain open. AC-003 action
exhaustiveness, AC-006 full surrender/result/RBA continuation, AC-007 outward
privacy and the full P01 support certificate remain open. No native or public
P01 action is enabled.
