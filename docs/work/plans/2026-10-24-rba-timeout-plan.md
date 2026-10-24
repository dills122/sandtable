# Persisted native RBA timeout

Baseline: `7554bb22`, branch `codex/rc-overnight-plan-20261023`.
Ruflo namespace `sandtable-rc-20261023`, task `task-1792877172812-3loke8`.
User authorizes implementation with focused local suites, rather than repeated
full-solution validation after each edit.

## Intended behavior

Configured `rba = cancel-without-decline` must advance an unanswered native RBA
decision to same-slot Reserve Release without a fabricated owner decline, Force
seal, ammunition debit, result roll or surrender. Current World/contact state and
RNG, including already accepted RBA retreat movement, remain authoritative.
Initial, ordinal-two and generic post-repeat assaults need the same behavior.

## Frozen responsibilities

- Core worker owns versioned cancellation event/command, strict canonical
  validation and replay, session integration and focused Core tests.
  New cancellation atomically records structural no-attack completion and exact
  entry/exit cursors. Original Barrage and latest accepted retreat bind the event.
- Host worker owns schema 8 `rba_rounds`, atomic entry/deadline/closure, submission
  and opening gates, background ticking and focused Host tests. Legacy 5/6/7
  stores remain readable; no implicit mutation upgrade or invented historical
  opening timestamp.
- Coordinator owns Host adapter integration, high-level docs, CI row-weight updates for added
  Core tests, validation receipts, review handoff and final commits.

Persist a round inside the transaction that appends automatic Barrage, before an
owner opening is exposed. Reopening or retreat movement must never renew the
deadline. Valid decline closes RBA before Force continuation. Explicit pre-opening
configuration/clock failure requires a distinct validated cancellation shape with
no invented timing values, following existing Force fallback principles.

Authenticating an invalid, stale or foreign owner offer must fail before clock
mutation. Accepted receipt retries recover prior evidence. Valid late offers may
trigger the same atomic System cancellation as background ticking.

A bounded, cancellation-aware background service uses the same injected
TimeProvider as the journal. It runs when the journal is configured, independent
of outward API enablement. Fair pagination must not starve later campaigns;
corruption is logged per campaign and isolated, never counted as a successful
timeout. No remote/model work enters authoritative turns.

## Validation gates

1. Freeze Core and journal contracts before implementation.
2. Focused deterministic tests: exact deadline, clock loss/regression, deadline
   persistence across reopen and accepted retreat, duplicate tick/retry, rejected
   late/forged submissions, rollback, malformed/foreign cancellation evidence,
   unchanged World/RNG and same-slot Reserve Release for all native assault paths.
3. Fake-clock background tick test proves progress without a fresh owner request.
4. Serialize build leases; use selected Core test classes, Host RBA/Force tests,
   complete affected Host suite, boundary checks and formatting/build. Broaden
   only for observed dependencies or failures. Retain exact passed/unrun evidence.
5. Refresh CI timing manifest from successful Release XML for new Core identities;
   rerun actual-row discovery so exhaustive CI still accounts for every test.
6. Fresh-context independent review before final commit. Earlier RC flow used two
   of its three review instances; use remaining instance with neutral bootstrap
   first and fix material findings. No automatic public admission or merge.

## Exclusions

Original P01 root/certificate status, public HOST-PUB/020–025 admission, automatic
surrender policy, other missing decision fallbacks and legacy fixture source-pin
repair remain separate. A passing native timeout packet does not close those gates.
Do not modify unrelated rules, refresh old pins or substitute native contact
history for original P01 ancestry.
