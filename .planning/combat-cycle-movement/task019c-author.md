# Author explanation

## Intent and plan
Task019C converts authenticated released-I progress and armed support into a native repeat/finish
control decision. Exact frozen3k both-owner admission is retained; this is not general cycle control.
Five primary files match dispatch. No schema/fixture changes, public registration or predecessor edits.

## Flow and responsibilities
Engine file owns typed source/commands/state/results and transition logic. Source constructor copies
three separately admitted Release inputs/events. Each Replay/Apply replays019B, thereby full creation,
held-I no-move Combat and017C Release. Nested base carries exact source history and proof digest;
control identity hashes that base. No persisted state or caller proof bypasses source authentication.
Replay reconstructs each of at most two events from independent inputs and compares exact bytes.
Apply optionally compares cache after replay, then transitions. Duplicate command+actor returns the
original retained event bytes; actor and control/cycle binding precede duplicate lookup.

Codec owns frozen canonical base/state/event encoding. It composes existing canonical predecessor
serializers, deriving a single typed Combat witness from certified candidate and actual progress.
Historical pre-release World belongs in the base; projected Reserve status belongs in state.
Only pending exception status/receipt changes on finish; no World resources change. ASCII canonical
bounds, duplicate keys, depth32, array512 and 1MiB limits precede comparisons. ReadBase/ReadState
rederive authenticated values; parsed JSON never supplies authoritative fields.

Tests own actual creation-to-Release setup and four literal golden traces, per-cut replay/readback,
retries, timing/fallbacks, mutation and ownership checks. Project file links only frozen3k fixture.
Plans and navigation identify exact completed boundary and follow-ups.

## Choices and invariants
Kept specialized engine/codec instead of widening generic dormant cycle control or mutating historical
contracts. Reuses native configuration timing, cycle identity, prefix hashing, receipts, Reserve
history and source certification. Transition remains private: only replayed state enters it. Generic
long/ordinal overflow guards are retained, but this adapter admits only authority25/ordinal1; forged
persisted capacity values are rejected by source-bound readback/cache checks, not used as authority.

Repeat opens ordinal2 at same-slot Movement using resulting authority27 and prior prefix26, resets
empty occurrence-local uses/progress and retains full stage/Release history, World and RNG. Finish
enters Truck Convoy and binds pending-exception expiry to closure receipt. No housekeeping or side
change. Deadline equality rejects owner input. Clock loss/regression/expiry/controller loss produces
system finish with deterministic reason. Opening-clock loss fabricates no Timing. Early/stale timers
are no-ops; retries cannot renew budget. Unsupported history rejects before opening.

## Verification
Compiled RED: four failures at stub after test analyzer fixes. Initial GREEN four tests passed.
Golden four tests passed with every frozen base/state/event length/hash, terminal receipt/prefix and
successor. Expanded twelve passed, including full deep-leaf/alternate byte mutations, re-signed
receipt and proof-digest forgery, every cut, wrong actors/versions/history, early/late/lost clocks,
original bytes and retained-source ownership. Final focused/build/full, format and four direct
oracles are running; evidence will record results. No remote CI claim.

## Limits and challenge points
Source replay is deliberately bounded and repeated rather than optimized with trusted caches.
Internal projection records are trusted in-process carriers; serializer output must still pass
source-bound readback before admission. Verify canonical World/status projection, receipt domain,
pre-event successor identity, timer/actor ordering and clock-high-water retention especially.
Settled source/progress integration, actual repeated Movement, later-II/consumed lineage, full
Snapshot and public actions remain open. Parent017–019 is not closed by this child.
