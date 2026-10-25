# Author Explanation

## Intent and success criteria

Preserve durable campaign authority when an authenticated creator retries a
request, and strengthen bounded third-cycle/atomic-Combat evidence. Exact create
retry must recover retained current state without rewriting creation or accepted
steps; changed identity/timing must conflict, and corruption must not be acknowledged.

## Plan-to-implementation traceability

Gate1 reconciled stale documentation and identified concrete gaps. Gate2 is proof
closure, not a speculative Core implementation: third-cycle zero-/one-seal timeout,
stale seal rejection, no additional result/World debit and cold reconstruction.
Gate3 implements creation retry and tests concurrency/corruption; additional
Barrage-trigger fault test covers transaction rollback. Gate4 inventories and
measures retained certificate evidence but does not claim full certificate closure.
Gate5 independent review, integrated tests and final handoff remain acceptance steps.

## Technical approach and flow

CreateCampaign now begins an immediate SQLite transaction before retained lookup
or fresh Core creation. Existing identity loads through full journal verification;
all request fields and exact timing bytes must match before returning that verified
session. The transaction is disposed without writes on retained return. Absent
identity follows existing timing binding validation and Core admission/insert.
Independent connections serialize the lookup/insert decision under SQLite locking.

## Changed-component walkthrough

Journal's private Loaded record additionally retains request and raw timing bytes
for comparison; no wire/schema representation changes. New creation tests cover
advanced head, differing identity/timing, corruption and same/different concurrent
creators. Outward test covers existing acknowledgements/conflict/authentication.
Existing durable journal tests extend third-cycle history and inject an SQLite
insert abort after owner selection but before automatic Barrage completion.
Canonical docs now distinguish bounded durable implementation from public gates.

## Decisions and rejected alternatives

No insert-and-ignore, unconditional overwrite, or retry after an unverified unique
constraint failure: those paths could acknowledge differing/corrupt authority.
No new error envelope or public admission. No Core production edit was justified
by audit. Closed-round operations already reject via InvalidOperationException,
mapped to generic HTTP409; initial new tests incorrectly assumed result-false/no-op
semantics and were corrected to test existing rejection and unchanged authority.

## Invariants and boundary conditions

Exact identity includes all request fields; null timing differs from explicit
timing. Fresh validation remains required for absent campaigns. Legacy schema5/6
creation restrictions remain. Route authenticates operator before lookup. Retained
records must fully replay before acknowledgement; no test bypasses canonical
verification. Public Combat and original P01 admission remain disabled.

## Verification performed and results

Baseline full local gate: exit0, 4,195 full-suite tests plus81 boundary tests.
Creation RED:14 total,7 failed/7 passed against unconditional insertion.
Final creation suite14/14 and outward1/1 passed after validation-order correction;
logs `creation-final` and `creation-outward-final` under `/tmp/sandtable-rc-*`.
Final third-cycle theory4/4 passed in1m26.724s; rollback1/1 passed in6.200s.
Logs: `/tmp/sandtable-rc-third-cycle-final.log` and
`/tmp/sandtable-rc-barrage-rollback.log`; final Host build0warnings/errors5.13s
in `/tmp/sandtable-rc-proof-build-final.log`.
Integrated full gate must run after review fixes. Original ancestry verifier passed
all300 roots in101.651s (44 provisional positive,256 absent,0 admitted), with
unchanged metadata/PDF/chunk pins. Two original Python oracles still fail
LandSequence source pins; ancestry verification does not erase those failures or
establish the full support/recovery certificate.

## Risks, tradeoffs, and maintenance costs

Exact retry verifies the whole retained history under a write-reserving transaction;
cost grows with history, preserving current journal verification semantics. No new
retention bounds or true process-kill recovery proof. Concurrency tests use distinct
journal connections with a barrier, not timing sleeps. New proof cases add full
bounded histories to Host test runtime; no new Core CI row inventory is introduced.

## Deviations, deferrals, and known gaps

Direct RNG cursor-byte equality is not exposed by public Host checkpoints: proof
checks retained cursor presence, no Result22, unchanged World/ammo and replay,
without claiming exact cursor-byte equality. Cold reopen is not process crash.
Full HOST-PUB faults/retention, original P01 certificate, public Runner adoption,
six-turn scenario and dependency remediation remain open.

## Challenge points

Check retained lookup ordering, all identity/timing comparisons, exception mapping,
transaction lifetime/concurrency, mutation sensitivity of tests, third-cycle
branch reachability, and whether docs overstate bounded evidence or plan completion.
Historical tests and green baseline cannot substitute for integrated changed-head
validation or independent acceptance.
