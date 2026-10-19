# Private paid-commit fresh-process readback v1

**Scope:** two authentic selected positive roots, one for each actor orientation.
This adds a fresh Python process readback proof for the prepared cut and the
atomic paid-commit cut in the [private paid reference](combat-p01-paid-commit-v1.md).
It does not implement durable host persistence, public/native admission,
result settlement or a complete Combat loop.

The test retains the exact original canonical CreationRequest, ten separate
pre-R03 input ledgers and the R03 ledger (eleven parent ledgers total), selected-assault ledger, Force
Assignment ledger, paid source and paid input ledger in a temporary cut file. A new interpreter imports
the reference, replays every ancestor from those bytes, and compares complete
canonical Control and World bytes and the last receipt with the stored cut.
It reports CP/ammunition, history and target-use state. Reissuing the accepted
paid input after the committed cut must return the original receipt and no
new event or changed source/ledger. Changed parent, source, own ledger or
conflicting retry rejects closed.

The readback envelope checks the 6 MiB paid Source, 8 KiB embedded paid Event,
64 KiB stored Control and 16 KiB stored World claims before replaying parent
ledgers. The canonical paid reference independently enforces its input and
projected-World bounds during replay. These caps describe the retained private
profile; they are not general host capacity or persistence guarantees.

    python3 -B docs/specs/test-combat-p01-paid-fresh-readback-v1.py
    python3 -B docs/specs/verify-combat-p01-paid-commit-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence

Full AC-008 all-row cuts, physical negatives and durable host restart/readback
remain open. AC-003 action exhaustiveness, AC-006 complete surrender/result/RBA
continuation, AC-007 outward privacy and full P01 support certificate also
remain open. All assessed states remain `admitted=false` with no public actions.
