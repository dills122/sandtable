# C02 CI recovery author self-review and rationale

Delta basec20a84c5d84ca96852ba2bb9d7e1f88b6b668c4e, PR182; initial configuration RED864c381.
Ownership limited to CI timeout/config invariant checks and incident research/docs/evidence.
Not an independent readiness verdict; Brain owns fresh review2/9/exact-head CI/merge.

## Cause, decision and implementation

Read finalized investigation before editing; report SHA256
4f828fbbe9511a3b166e8331848ed088a144d5de1fc6155431d045fbf04d8726, retained verbatim.
Current2329row Core-B continued successful work until15minute cancellation:2290pass/0fail/skip,
one active row at ordinary prior duration,38notstarted. Exactly matched2290rows +18.915% summed
time versus prior green; sums are not wall time. Whole-job16–17minute estimate includes incomplete
selection tail/setup/reporting. Underlying CPU/I/O/cache/GC/VM resource submechanism is unmeasured.
No added native rows or new RMC fixture acquisition; no evidence justifies product behavior edits.

One workflow line sets `${{ matrix.shard == 'core-b' && 20 || 15 }}`. The fixed trusted matrix
still contains core-a/core-b/exercise/contracts. Core-B20 alone; other shards15, prepare/verify15,
dependency-review5. No partition/filter/coverage/process/attempt/assertion/report changes.
Existing test helper receives2small deadline policy tests, recognizing this numeric conditional
or prior numeric literal and comparing outcomes for every shard. Positive numeric truthy branches
prevent zero-fallback surprises; actionlint validates actual GitHub expression/context syntax.
No custom general expression evaluator or new dependency. Existing whole-helper tests continue to
exercise disjoint/full-row filters and fail-closed acceptance; policy tests also guard other jobcaps.

Official source: [job timeout matrix context](https://docs.github.com/en/actions/reference/workflows-and-actions/contexts)
and [numeric literals/operators](https://docs.github.com/en/actions/reference/workflows-and-actions/expressions).
Matrix-dependent job timeout is supported; logical operator semantics select numeric20or15.
A matrix include/multiple jobs would broaden the change; rebalance/Core-C requires new workload/
partition proofs and can transfer tight margin to Core-A. Another unchanged15minute retry supplies
no mitigation.20minutes remains bounded; expected16–17minute envelope has3–4minutes headroom.

## Five-axis author check

Correctness: inspect actual workflow delta and new tests. Genuine focused RED0.129s/120 has1policy
failure (actual Core-B15 vs20), with other-jobcaps check passing; not an import/harness error.
After one-line fix, GREEN2/2 0.134s/120; all98helpertests0.384s/120; actionlint1.7.12 exit0,0.069s/120.
All receipts no timeout; logs retained. Hosted full Core-B2329/final aggregate2879 still required.
Readability: single bounded numeric conditional follows existing workflow expressions; tests fit
neighboring workflow-text protocol checks. No generalized config parser and no product refactor.
Architecture: CI capacity only. Native/src/tests/global/build/package and partition implementation
byte-diffs against c20 are empty. Frozen oracle/schema/fixture match author finalnormal pre/postSHA.
Security: fixed matrix and numeric literals only; no untrusted interpolation/secrets/permissions
changes, no continue-on-error or disabled assertions/rows. Network publication uses Keychain safely.
Performance: max job budget rises300seconds only for Core-B; no algorithmic or test workload change.
20minutes is not permanent scaling/performance assurance. Later measured shard balancing is a
separate planning gate. Underlying host cause remains unknown; this fixes immediate budget mismatch.

## Verification limits and next gate

No broad local.NET or Python contract rerun; unchanged product gate remains exact-identity evidence.
Report/analyzer/analysisJSON retained; original CI artifacts remain Brain's separate preservation
responsibility. Reviewer must inspect this delta and prior reviewed scope as needed, then Brain
must reconcile one fresh exact-head hosted run, all owning jobs and final exhaustive coverage.
No partial pass/discovery acceptance. If20minute cap still expires or an active row becomes stationary,
stop for targeted CPU/I/O/allocation/hang diagnostics instead of another blind deadline increase.
No native implementation; accepted predecessor merge and>=155min remain prerequisites.
