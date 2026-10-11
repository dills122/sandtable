# Review1 disposition

Accepted the confirmed P2: malformed independently supplied selection/round ledgers were collapsed
into APA004 by inherited replay. Review1 examined head661ba68d46c77c17bf599e449cc7cc5d9efd70e4;
its verbatim report is retained beside this disposition (SHA256
550cf3b91086ddadf71c5f847bb83b5017ddc61d7b95fb55d6eca24d7df0c965).
Counters: review1/9 completed, round1 pass1, research recovery0/2. Brain owns review2 dispatch.

The six semantic RED rows reached the native paid read/apply boundary with valid dependencies and
failed because actual APA004 differed from required APA001/002/003. Each row first passed its
APA009 dependency-precedence checks. Exit2,6failed/0passed/0skipped,2.129s under120s;
primary logSHA51255c75a75b1749248453e478749946ed9dc041a333a215c5b74ea8ca97c255.
These are behavior failures against unchanged production, not build or harness failures.

Correction owns copies of both predecessor ledgers and validates their closed syntax, versions,
primitives, depth32, count16 and aggregate1MiB limit before inherited replay. Paid grammar remains
depth34. Command primitive validation precedes version comparison. The private admission adapter
also validates direct callers. Coherent source/context mismatches still map toAPA004; physical
failures remainAPA009. Shared/predecessor code and immutable contract bytes are unchanged.
Six diagnostic rows cover both predecessor ledgers and all four read/apply entry points; two
coherent mismatch control rows cover the same APIs. No new deferred test factory or partition change.

Focused GREEN:33passed/0failed/0skipped,exit0,78.040s under120s; logSHA
 e87787fb0e0aa23393097de8571fab7ed21848159034de0fd37b9ed19f7e3d3a.
Release focused build:0warnings/errors,14.39s. Self-review checked dependency-first ingress,
closed shapes and unchanged field order, primitive-before-version diagnostics, owned replay bytes,
ledger depth/count/aggregate limits, unchanged shared codecs and fail-closed coherent mismatch004.
The eight new rows contain no MemberData/deferred factory. The duplicate bounded validation in the
private adapter preserves safety for direct callers. No additional finding identified by author
self-review; independent review2 and exact-head CI remain Brain's gates.

Full format verification passed,exit0,29.633s/120. Git diff --check passed.
