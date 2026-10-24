# RC certificate status — October 24, 2026

Baseline `16d76448a008ea4f16299402b8ef44e8c96a4843`. Bounded gate4 inventory;
no certificate issued, no original row promoted, no fixture/pin regeneration.
Native work elsewhere on this branch is separate from these Python checks.

## Executed checks

All commands ran from repository root with Python3. Results are execution receipts,
not claims of a full support certificate. Subsequent two-root and all300-root
ancestry runs are recorded below; mutation/cut/recovery/support limits remain.

| Exact command | Exit / result | Limit |
| --- | --- | --- |
| `python3 -B docs/specs/verify-combat-p01-a2-private-owner-contract-v1.py` | 0; two golden cases, eleven closed shapes and negatives pass | Synthetic private proposal only; no authenticated owner path. |
| `python3 -B docs/specs/test-combat-p01-owner-projection-v0.py` | 0; five tests, 0.000s | Disconnected owner-alias scaffold; not authentic hidden-fork histories. |
| `python3 -` with the retained inline program below | 0; AST parsed57 Python files; three metadata pins, eleven compressed chunk pins and300 summary rows pass | Syntax and retained-byte integrity only; no authority replay, source-PDF validation or candidate classification. |

Reproducible inline program:

```python
import ast, hashlib, json
from pathlib import Path
files = sorted(set(Path('docs/specs').glob('*p01*.py')) |
               set(Path('docs/specs').glob('*actual-moved-repeat-position*.py')))
for path in files:
    ast.parse(path.read_text(), filename=str(path))
print('AST parse PASS:', len(files), 'Python files; execution not performed')
evidence = Path('/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence')
pins = {
    'summary.json': '847972f5eb9c12be2e7a1774d2acd61caa1dc603b43413f7fb9d30cbec80c45b',
    'manifest.json': '39cae77c405b83a793b3d806fcf4a67860b9ff8514e59c9bfa83384d7dfc2b35',
    'bounded-profile-audit.json': 'ba60a05a14ab907bb5be1e06509a5a24646a97759e564bcd525bdd7515bef429',
}
for name, pin in pins.items():
    assert hashlib.sha256((evidence / name).read_bytes()).hexdigest() == pin, name
manifest = json.loads((evidence / 'manifest.json').read_bytes())
for chunk in manifest['chunks']:
    assert hashlib.sha256((evidence / chunk['name']).read_bytes()).hexdigest() == chunk['sha256'], chunk['name']
summary = json.loads((evidence / 'summary.json').read_bytes())
assert summary['count'] == len(summary['rows']) == 300
assert len(manifest['chunks']) == 11
print('Retained inventory pin check PASS:3 metadata pins,11 compressed chunk pins,300 summary rows; no authority replay')
```

Coordinator separately reproduced two original verifier failures, preserved in
[baseline evidence](2026-10-24-rc-baseline-evidence.md#historical-evidence-and-source-pins):
inherited Breakdown and cycle-sequence both exit1 on `Cna1979LandSequence.cs`
source drift. They were not rerun or repaired here. Full local .NET baseline green
does not supersede those failures.

## Two-root ancestry measurement

Coordinator authorized one measured follow-up using the existing row filter:

```sh
python3 -B docs/specs/verify-combat-actual-moved-repeat-position-evidence-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence --rows 36 168 --progress
```

Exit0 in **5.100 seconds**. Original root36 (`r012-c01`) and root168
(`r034-c01`) both replayed as `provisional-positive`. Output:

```text
36 r012-c01 provisional-positive
168 r034-c01 provisional-positive
{'verifiedRows': 2, 'provisionalPositive': 2, 'provisionalEmpty': 0, 'admittedRows': 0}
```

Log: `/tmp/sandtable-rc-p01-two-root.log`; command/exit/timing receipt:
`/tmp/sandtable-rc-p01-two-root.json`. Existing verifier checked all five pins
(three metadata files and two physical-source PDFs), eleven compressed chunks and
300 unique contiguous catalogue rows. It authenticated original ancestry and
assessment only for the two selected roots, retaining unsupported status,
`admitted=false` and empty public actions. No pin/admission failure occurred.
This is selected original-root evidence, not a full300-row or44-positive proof,
and not durable-host recovery. No .NET build or fixture update was involved.

## All300-root ancestry result

After the pilot, coordinator authorized the original ancestry verifier without
`--rows`. Rough linear planning estimate was13 minutes; actual elapsed time was
**101.651 seconds**, exit0. Estimate was not a capacity claim.

```sh
python3 -B docs/specs/verify-combat-actual-moved-repeat-position-evidence-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence --progress
```

```text
{'verifiedRows': 300, 'provisionalPositive': 44, 'provisionalEmpty': 256, 'admittedRows': 0}
```

Log: `/tmp/sandtable-rc-p01-all300.log`; command/exit/timing receipt:
`/tmp/sandtable-rc-p01-all300.json`. All300 original roots replayed through
unchanged original verifier with all five metadata/PDF pins and eleven chunk
hashes. Each assessment remained unsupported, unadmitted and without public
actions. `provisionalEmpty` is the historical verifier label for256 candidate-
absent roots; it does not mean supported-empty. This closes the current-head
terminal ancestry/classification rerun gap only. It does not close full AC-001/002,
all-row matching midstream recovery, action exhaustiveness, authentic owner privacy,
HOST-PUB or support certification. No fixture writes, pin refresh, downstream44-root
job or .NET build performed.

## Other applicable full-profile commands, not executed here

These commands preserve the retained private evidence path. They were not run
in this packet; downstream fresh/cut proofs repeat reconstruction per selected
root/cut and differ from the measured terminal ancestry run. Measure their scope
before parallel full runs, retaining unchanged pins and exact output digests.

```sh
python3 -B docs/specs/verify-combat-p01-action-matrix-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
python3 -B docs/specs/verify-combat-p01-privacy-gates-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
python3 -B docs/specs/verify-combat-p01-replay-gates-v1.py --evidence /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
python3 -B docs/specs/verify-combat-p01-selected-assault-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
python3 -B docs/specs/verify-combat-p01-force-assignment-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
python3 -B docs/specs/verify-combat-p01-paid-commit-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
python3 -B docs/specs/verify-combat-p01-paid-all44-cuts-v2.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
```

This is a selected applicable inventory, not every historical oracle. Separate
all-44 fresh-interpreter decline/cancellation and all-prefix rejection proofs
remain applicable according to their individual contracts. Their TEST-only and
terminal-versus-midstream boundaries remain intact. In particular,
[all-44 paid cuts](../../specs/combat-p01-paid-all44-cuts-v2.md) explicitly distinguishes
in-process cut readback from fresh-process/durable-host recovery and does not
reclassify the256 absent roots.

## Certificate closure matrix

[Canonical AC matrix](../../specs/combat-actual-moved-repeat-position-v1.md)
remains authority; checks above close no full AC.

| Obligation | Status after this packet | Required evidence still missing |
| --- | --- | --- |
| AC-001 | All300 original terminal ancestors replayed; metadata/PDF/chunk pins match | Complete authenticated ancestry mutations/cuts/retries, not merely retained-file integrity. |
| AC-002 | All300 original assessments rerun:44 provisional positive /256 absent, zero admitted | Full admitted classification after all support gates; absence is not supported-empty. |
| AC-003 | Existing bounded action audit identified; not executed here | Complete participant/action/obligation exhaustiveness, including non-assault work. |
| AC-004 | Prior bounded result/settlement slice retained; not rerun | Applicable exact-head proof and integration into full certificate; no seed filtering. |
| AC-005 | Prior bounded geometry slice retained; not rerun | Applicable current-root geometry/placement proof and certificate integration. |
| AC-006 | Private decline/cancellation/Force/paid successors identified | Full automatic-surrender source trigger and settlement; full adopted RBA/profile authority. Voluntary surrender remains excluded by policy. |
| AC-007 | Five scaffold tests and synthetic A2 canonical shapes pass | Authentic owner-visible mapping with equal-authorized histories, hidden variants, decisions and accept/reject behavior. |
| AC-008 | Syntax/integrity checks pass; prior bounded cut proofs remain distinct | All-row matching nonterminal recovery, dependency faults, caps and exact outputs; durable-host restart/readback. |
| AC-009 | No support review or admission attempted | Full support review on one head, separate P02 selection/P03 native gates and exact-head CI. |
|020–021 | Local Development path remains separate | Reviewed certified public profile, safe projection/submission, recovery registration. |
|022 | Existing native/contract checks insufficient | Exercise must consume same admitted public actions and terminal evidence. |
|023 | No new Combat Runner trace | Strict reconstruction/readjudication of certified history, including required repeat/finish. |
|024 | No repeated clean Combat study | Retained independent clean runs with empty/settled-assault/Movement-repeat coverage. |
|025 | Open | All72 reconciliation plus actual accepted loop; preserve HOST-PUB and remaining MVP obligations. |

## Minimal next evidence packet

The two-root pilot and all300 terminal ancestry run passed. Repeated-cut matrices
remain a different workload; do not extrapolate their capacity from terminal replay. Next choose
one missing AC-007/008 owner/recovery proof, freeze its exact authority interface
and root/cut mapping, and prove it with authenticated history. A normal contact
campaign history cannot silently replace an original P01 root. Keep all256 absent
rows unsupported and all44 positive rows provisional until complete support review.

No source-policy question requires an immediate owner decision for this inventory.
Full certificate closure is blocked by missing evidence/contract coverage, not by
unavailability of retained catalogue files. Historical source drift remains a
separate maintenance/review decision; no waiver is inferred.

## Method and limits

Used codebase-memory Tier2 search, replay-audit outbound trace and source snippet;
selected script coverage returned `no_recorded_issue` / `metadata_match`, generation
`2026-10-24T03:07:43Z`. Direct source reads verified command entrypoints and avoided
launching full matrices inadvertently. A guessed A2 test path did not exist; the
actual A2 verifier was located and run instead. Scope coverage flags two unrelated
large repeated-Movement/Breakdown fixture parse timeouts; no graph-based completeness
claim is made for those fixtures. Source and canonical documents were read directly.
The follow-up evidence script also had clean best-effort coverage, generation
`2026-10-24T03:10:28Z`; its complete entrypoint/audit source was inspected before
launch. No .NET job, fixture regeneration, public action or runtime modification performed.
