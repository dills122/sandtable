# RED evidence correction

The original pre-behavior RED checkpoint e9ff5db and verbatim log6cdba83 are preserved.
It replayed all four predecessor ledgers and supplied the empty fifth Movement ledger from the
semantic test, but the stub did not explicitly validate that fifth ledger. It is not evidence
for rejection of a malformed or nonempty fifth ledger.

After GREEN WIP existed, author identified that precision gap. A supplemental reconstruction of
the original stub added exactly one root guard: require the independent fifth ledger to be an
empty list and the source's new event array empty before missing-consumer007. GREEN WIP was saved
verbatim to /private/tmp/cs04-green-before-red-supplement.py, the reconstructed stub temporarily
ran at the original spec path, then GREEN WIP was restored. The retained dated supplemental
script has only a ROOT relocation adjustment so it can run from reviews; the guard is identical.
No GREEN behavior was used to produce the supplemental missing-consumer failure. This is
post-GREEN evidence strengthening, not a new claim about original chronological TDD.

Command: python3 -B -u docs/specs/verify-combat-actual-repeated-movement-v1.py under the bounded
runner,120s. Four genuine CMB-RMV-007 failures after full PAC replay and explicit empty-root
fifth-ledger validation; exit1, no timeout,6.888s, logSHA
88ce6f0da876e9d468b1d17c813c1ad83362a5da83bfc77ce3cbf421371b5523.
Receipt/log retained verbatim beside this note. Brain accepted the correction without resetting
review counters or changing scope. The GREEN grammar group explicitly checks None fifth ledger001,
nonempty ledger at empty Source004, and empty ledger at a one-event cut004. Those focused tests,
causal/reordered/count-mismatch checks and full normal establish actual rejection behavior.
