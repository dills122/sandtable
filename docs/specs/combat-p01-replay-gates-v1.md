# P01 bounded replay gate diagnostic v1

This is a private, offline AC-008 diagnostic over the retained authentic 300-row
CreationRequest plus eleven-ledger R03 inventory. It does not define a P01 input,
event, supported action, selection, native consumer, or admission path.

`verify-combat-p01-replay-gates-v1.py` checks the pinned source PDFs, manifest,
summary, profile audit, all eleven compressed chunks, contiguous unique row keys,
source vectors, and each original R03 replay before assessing every row. It freezes
the SHA-256 of the ordered 300 canonical assessment hashes and sample hashes for
four rows in `fixtures/combat-p01-replay-gates-v1.json`. It checks the maximum
observed canonical assessment byte count against the fixture, both actor sides,
private-only output, 44 provisional positives, 256 provisional empties, and zero
admitted rows. The fixture is a reviewed output regression baseline, not an
independent rules proof.

Four genuine rows are retried, with exact assessment hashes and unchanged inputs.
For one positive row from each actor side, the verifier rejects every nonterminal
cut of each nonempty parent ledger, mutations in all eleven independent parent
inputs, missing or duplicate R03 input, a distinct nonempty parent-ledger
argument swap, source byte changes, an injected dependency failure, and a changed
content pin. Classification is patched to fail if reached after an invalid replay.
Focused tests mutate outward fields to confirm the checker fails rather than
merely reporting expected counts.

Private P01 assessment now enforces schema-frozen byte limits before R03 replay:
1,048,576 bytes for raw Source, 32,768 bytes for aggregate parent inputs,
and 1,024 bytes for the single raw R03 input. Parent aggregate counts compact
`ensure_ascii` JSON bytes of CreationRequest plus raw bytes of every input in
the ten parent ledgers; it excludes list framing and does not reserialize
ledger inputs. A canonical assessment over 1,024 bytes raises before any result
is returned. Size checking stops when the next JSON fragment exceeds a limit.
String lengths and container work are bounded before JSON encoding,
so one oversized string cannot allocate an unbounded encoded fragment. Empty
parent input bytes reject before R03 replay, including a long ledger of empty
entries. The retained 300-row inventory measured maxima of 885,181 Source
bytes, 25,862 aggregate parent bytes (741 CreationRequest plus 25,121 ledger
bytes), 580 R03 input bytes, and 695 canonical assessment bytes. Focused tests
cover each oversized ingress class before replay, exact boundaries, oversized
egress, unchanged inputs, both acting sides, and retries. Assessment fields,
serialization, and frozen output hashes are unchanged.

**Open AC-008 work:** The parent-cut and mutation matrix covers two positive
rows, not every cut of every retained row. It does not prove restart/readback,
R03 native versus reference equivalence for a P01 transition, all physical
negative cases, or exact supported
P01 event output. All current assessments remain `unsupported`, with
`admitted=false` and no public actions. Full P01 certificate, P02 selection, and
P03 native consumption remain closed.

These limits close the private assessment cap slice only. AC-008 remains open
until the other replay, negative-case, and exact-output obligations above are
proved on a reviewed head; no P01 admission follows from this diagnostic.

Run from repository root:

```sh
python3 docs/specs/test-combat-p01-replay-gates-v1.py
python3 docs/specs/verify-combat-p01-replay-gates-v1.py
```

The verifier expects the retained private evidence at
`/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence` unless
`--evidence` is passed. Source pins are hard-coded in the merged P01 evidence
verifier and rechecked on every run.

Local verification on 2026-10-17: four focused tests passed on the final test source in 125.784 seconds.
The complete private 300-row replay reported 44 provisional positives, 256 provisional
empties, zero admitted, 100 bounded mutation/cut rejections on two positive
actor-side samples, four exact retries, ordered output-hash root
`23480d1a1b90d587e43f1aaf8fd0555a58e5fe37b10a069ff449506d6c3e89b0`, and
695 maximum observed canonical assessment bytes. In hosted CI, the focused
sample and mutation tests run from in-repository authentic source synthesis;
the full corpus test reports an explicit skip when the private retained evidence
is unavailable. Local private evidence is necessary for the full 300-row claim.

Local verification on 2026-10-18 with the retained private archive: 17 P01
reference tests and four replay-gate tests passed. Full 300-row CLI replay
retained the same ordered output-hash root, 44/256 shape split, zero admitted
rows, 100 bounded mutation/cut rejections, four exact retries, and 695 maximum
canonical assessment bytes under the frozen caps.
