# Author explanation — CI01

## Intent and success criteria

Core B dominates latest required gate. Move five measured complete collection-per-class groups from B to underused C/D while preserving existing six jobs and exhaustive admission. Speed acceptance requires two comparable full observations with at least20% wall improvement and at most10% runner-minute increase; not yet met or claimed.

## Plan-to-implementation traceability

Baseline20 recent runs completed; failure classification and measured class timings in `docs/work/ci-runtime-optimization-2026-10-15.md`. Small static duration-informed redistribution implemented and Python TDD checked. No build-once cache or hosted experiment. Fresh review, Brain performance budget, full candidate execution/actual runner discovery and exact-head gates remain pending.

## Technical approach and changed components

`test_partition.py` adds three C and two D exact class names to existing constants. Existing core_partition derives exhaustive sets; B remains complement. Existing partition_filters emits C/D class inclusion and B complement. Discover obtains fresh actual runner selection and checks equality/disjoint union. Execute reconstructs expected partition from same-attempt inventory. Verify requires all process/status/XML memberships. No verifier weakening and no new scheduler.

`test_partition_test.py` extends historical class fixture expectations and removes fixed D command length. Four new checks cover whole moved classes including two methods and multiple arguments, deterministic reversed input, complement filters, lookalike fallback and selected missing-class rejection. Existing malformed/overlap/process/provenance regressions remain required.

## Decisions and rejected alternatives

Static mapping chosen over dynamic scheduler. Build-once deferred because latest B restore+build74s is5.35% workflow before transfer; measured time dominated by execution. Keep established RoundEntry split: combining observed class collection times creates1329/1509s serial work. Do not combine include method/class filters to extend A because filter intersection would change coverage. Route moves only to C/D using existing exact class policies.

## Invariants and boundaries

No source/domain/test-fixture change, no dependencies or secret handling, no workflow or deadline change. Original eight transferred argument rows stay B. New unknown classes enter B; future methods in moved classes follow class. Default class isolation preserved; no shared collection declarations on moved classes found in source. Candidate counts A71/B2326/C233/D211/Exercise469/Contracts10 at base, union3320. Concurrent Combat work may change count/load and requires fresh discovery at combined head.

## Verification evidence

Focused RED against old helper: four tests, three failed with11 assertions/subtests. Initial full candidate run137 tests with one obsolete D slice failure. Fixed exact command-length assertion; final full137 pass. `git diff --check` passed. Real completed inventory predicate replay conserves exact union and is order-independent. Old original process/XML evidence validates against old inventory, but same evidence fails candidate inventory with missing/unexpected argument-bearing rows. No .NET build/test or hosted candidate run performed. Historical report evidence covers current suite twice, different SHA, not matched repeat experiments.

## Author self-review

Reviewed test sensitivity, exact class name membership and complementary filter consumption, actual-discovery and prepared-inventory reconstruction, unchanged exit/provenance/XML/dependency gates. Six production lines; no extra abstraction. Maintenance cost is static timing allocation drift. No claimed performance result. Fresh reviewer must independently inspect both code and requirements before accepting this explanation.

## Risks, gaps and challenge points

Class elapsed totals overlap across four threads; sums are not job wall. Candidate scheduling can alter CPU contention and class costs, especially new Combat classes falling into B. Main/pr suite historical timing variance substantial. Hosted paired comparisons still needed; source-only exact set equality cannot prove runner filter semantics by itself. Challenge timing comparability, selected class completeness, isolation assumptions, actual runner discovery, preservation of old row-transfer policy and all fail-closed gates. Do not treat code readiness as speed target achieved or merge readiness.
