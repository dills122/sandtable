# Private P01 all-prefix terminal-Source rejection proof (partial AC-008)

This proof checks one narrow replay failure boundary. It never issues a Combat
action, admits a P01 row, creates a supported-empty certificate, or tests an
actual matching midstream restart. Every ancestor-prefix trial keeps the
authentic final R03 Source and own input unchanged while truncating **one**
ancestor input ledger. The separate missing-own-input trials retain final
Source but intentionally remove the sole R03 own input.
The expected outcome is rejection before physical-shape classification. This
terminal-Source tamper test cannot prove that a Source saved at a matching
intermediate cut can later resume, nor that a host can durably restore state.

## Frozen corpus and independent generation

The original 300 CreationRequest plus 11-ledger R03 corpus is reviewed in
`/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence`. Its pinned
`summary.json` has SHA-256
`847972f5eb9c12be2e7a1774d2acd61caa1dc603b43413f7fb9d30cbec80c45b`.
The five ordered fields `index`, `rowKey`, `sourceHash`, `controlHash`, and
`proofHash` for all 300 rows are frozen in
`fixtures/combat-p01-all-prefix-reject-v1.json`; their canonical ASCII JSON
root is `fde84107d285ac5be6824bef4a939bf642eddbdd1624ff6d2a29f50ce4d5dede`.
The fixture also pins each original CreationRequest, eleven parent input
ledgers, and R03 own input as a byte-exact canonical vector hash. The ordered
300-vector root is
`c925de99654e4a353a8f226aef01c540f5b8db3cbcb6b21925856b21e392a824`;
the retained compressed chunks are checked against their frozen hashes when
the fixture is constructed.
No local retained-evidence path is required in CI. Each shard regenerates its
75 original terminal R03 roots from the in-repository semantic rows and compares
the canonical Source, Control, Proof, and input-vector hashes to those frozen
fields before
cut checks. The full uncut root must still assess as private
`unsupported/admitted=false/publicActions=[]`.
The optional `--retained-evidence` check independently verifies all retained
pins, 11 compressed chunk hashes, and all 300 original vectors against the
committed fixture before CI runs without those private files.

For each root, the verifier tests every strict nonterminal prefix of each of
the ten event ledgers. The CreationRequest remains unchanged. It also removes
the sole R03 own input. Four `index % 4` shards cover 300 roots; the aggregate
requires 11,816 ancestor-prefix rejections and 300 missing-own-input
rejections, all before `physical_shape`. The ordered row-result root is pinned
in the fixture as
`056d7e607d848dd0b76ad139f24247ef6ed2e39edaca7a33b21ab00de4efae93`.
Result object keys are sorted for that hash; original row order is preserved.
That expected root was derived from the reviewed retained ledger lengths and
private partitions; only a passing replay run proves the rejections. Shard
artifacts are bound to the exact PR head and current
workflow attempt; aggregate refuses missing, duplicate, wrong-head, or
wrong-shard records.
After a failed shard, rerun the full workflow matrix. A failed-only rerun
cannot borrow successful shards from another attempt and fails closed.

This is **partial AC-008 only**. Original authentic P01 inventory remains 44
provisional positive, 256 provisional empty, zero admitted or public. Source
conditions, action exhaustiveness, privacy, full cuts and mutations, actual
midstream readback, dependency faults, resource caps, and durable host restart
remain separate gates for the complete support certificate.

## Local verification

```sh
python3 -B docs/specs/test-combat-p01-all-prefix-reject-v1.py
python3 -B docs/specs/verify-combat-p01-all-prefix-reject-v1.py \
  --retained-evidence /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
for shard in 0 1 2 3; do
  python3 -B docs/specs/verify-combat-p01-all-prefix-reject-v1.py \
    --shard "$shard" > "/tmp/p01-prefix-shard-$shard.json"
done
python3 -B docs/specs/verify-combat-p01-all-prefix-reject-v1.py \
  --combine /tmp/p01-prefix-shard-{0,1,2,3}.json
python3 -B docs/specs/verify-combat-actual-moved-repeat-position-evidence-v1.py \
  /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
```
