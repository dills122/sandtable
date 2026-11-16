# Graziani replay stress observations

Local investigation based on `1344d7b6`, macOS arm64 and .NET SDK 10.0.400.
Measurements describe the source setup path, not a playable GT1 or a release
performance guarantee. The baseline and candidate used copies of the same
real-source journal; every sample retained full request-level verification.

| Journal | Operation | Baseline median | Candidate median |
| --- | --- | ---: | ---: |
| 40 accepted steps | Fresh Host replay | 297.5 ms | 303.1 ms |
| 40 accepted steps | Commonwealth opening | 570.7 ms | 296.8 ms |
| 40 accepted steps | Commonwealth submission | 584.4 ms | 298.9 ms |
| 80 accepted steps | Fresh Host replay | 714.6 ms | 591.1 ms |
| 80 accepted steps | Commonwealth opening | 1,399.6 ms | 603.6 ms |
| 80 accepted steps | Commonwealth submission | 1,328.8 ms | 608.2 ms |

Each median uses three warm samples after one warm-up. The 80-step journal
extends a real setup using repeated valid owner motorization adjustments: it
is a stress input, not representative player behavior. Each replay opens a fresh
Host object, but runs in an existing process with potentially warm filesystem
caches; this is not a cold-process or cold-storage restart benchmark. Its slower baseline
replay indicates load variability; do not treat the percentage as a controlled
universal speedup. The candidate reuses an already verified journal head only
inside the same request transaction. Each new request still verifies history;
full replay remains dependent on journal length. The 80-step fixture had a
576 KiB main database, approximately 3.9 MiB WAL and 32 KiB SHM during the run.

For repeatable comparison of committed revisions, run
`python3 docs/research/graziani-host-replay-bench.py --baseline <commit> --candidate <commit> --output /absolute/new/result.json`.
It creates isolated source archives and build artifacts, generates the same 40-
and 80-step histories, and records per-operation wall/CPU timings. Its memory
measurements describe the test process, not an individual request; when the
runtime does not supply peak working set, the result records it as unavailable.
HEAD-versus-HEAD smoke passed before integration; compare a committed candidate
and baseline on a quiet machine for independent measurements.

Real-source stress checks also exercised both owner interleavings and stale,
forged, duplicate and conflicting actions; transaction rollback on SQLite
exceptions around the journal head update; child termination before source
writes and after commit before response; and fresh-process replay. The child
tests do not kill a process between event insertion and commit. An opt-in
seeded Core soak is described in `docs/runbooks/local-testing.md`.

The integrated local `just check` gate passed 4,635 tests, including the
complete Host suite (20m 23s) and Core suite (1h 04m 42s). It also passed
formatting, build and 82 user-space boundary tests. The build emitted the
existing ASPIRE010 warning. During the Core suite, one process-memory sample was
approximately 3.6 GiB and a later sample was lower; neither is a measured peak
or evidence of a leak. These local results do not replace exact-head hosted CI.
