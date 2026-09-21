# Task017A2 isolated Reserve Release lifecycle evidence

Status: accepted2026-09-21 after root gates, three fresh Ready reviews and exact-candidate CI.
Candidate `a4b4f630d1250fd00d61ebae486f18813ffedd01`; base `32a038c322c561f642af2aedafe37165147f4939`.
Original candidate3811202/basea1cd425 have identical respective trees; ancestry-only rebase after
PR138 squash merge removed PR conflicts. [Draft PR139](https://github.com/dills122/sandtable/pull/139).

## Scope and proof

Exactly four implementation/test paths in [dispatch](task017a2-dispatch.md), plus root plan and
administrative status/evidence. Owned typed command/input/state/disposition/result values, frozen
command/event/state codecs and private deterministic transition via trusted-history replay.
Explicit opening, fixed queue, first-I release/convert, later-II release/retain/owner bulk completion,
pinned timing and high-water, deterministic locked fallback, retry/stale handling and completion.
Receipt-linked conversion/release history and pending next-Movement exception; no Movement executed.

44isolated fixture traces across both sides and relative slots;132event hashes,176state hashes and
byte lengths,two terminal event literals. Four historical Result1 rows excluded explicitly. These
are not132literal event strings or176literal state strings. Native Result2 adapter and positive
campaign lineage remain separate. Initial A1 base semantics unchanged; current lifecycle state is
validated by reconstruction, not by relaxing initial-base histories. No schema/fixture changes.

## Verification

- Meaningful RED: compiled no-op kernel failed first-I opening; expected open, actual unopened.
- Negative regression: numeric effect kind failed with InvalidOperationException; fixed to typed
  JsonException before trusted-context access, then16lifecycle/base tests passed.
- Full solution build:0warnings/0errors3.69s.
- Full solution tests:2,347passed/0failed/0skipped9m46.822s.
- Boundary=UserSpace:81passed/0failed/0skipped10.912s.
- Full format and diff checks:pass.
- A2four source pins and Task016eight pins:pass. Existing A1six tests unchanged and passing.
- Exact candidate CI:all8checks successful; verify run35665875884/job106551403293
  confirms2,347passed/0failed/0skipped9m55.443s. Log /tmp/017a2-ci.log.

[Check ledger](task017a2-checks.md) distinguishes initial IPC sandbox failures and intermediate
compile error from behavioral RED and passing gates. [Development review](task017a2-dev-review.md)
records root inspection; [author packet](task017a2-author.md) remains frozen testimony.

## Independent reviews

Three sequential fresh-context instances, each own clean detached clone, neutral bootstrap and
blind preliminary ledger before author explanation. No CCE/memory/prior-review access. Root read
all preliminary/final reports and reconciled claims; no actionable finding or source fix requested.

| Instance | Candidate | Verdict | Independent checks |
| --- | --- | --- | --- |
| [1](task017a2-review1-report.md) | 3811202, identical tree | Ready |16focused, frozen oracle, diff |
| [2](task017a2-review2-report.md) | a4b4f63 | Ready |16focused, frozen oracle, diff |
| [3](task017a2-review3-report.md) | a4b4f63 | Ready |FULL Release solution restore/build13.90s0warnings/errors;16focused3.810s;81Boundary8.600s;oracle/diff |

Oracle checks cover48historical contract traces/188cuts/2368mutations/840raw rejects/20timing/
27boundary checks. Native A2 claim remains44isolated traces. All reviewer-owned commands completed.

## Remaining gates

Parent017 remains open. Task017B actual retained Result2-to-empty-Release adapter, positive held-I
no-move predecessor and Release bridge, later-II/consumed actual lineage, relation-aware Movement,
cycle control, Snapshot/durable publication and public admission remain unimplemented in this slice.
Retained World hash is not World authentication; callers must independently admit base and inputs.
No full playable loop or six-turn readiness claim follows from isolated A2 acceptance.
