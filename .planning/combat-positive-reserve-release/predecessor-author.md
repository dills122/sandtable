# Author Explanation

Intent: authenticate actual held-I first opening and execute ten frozen no-move/no-attack events to
Reserve Release. No release occurs in this slice. Retained snapshots alone never admit history.

Approach: separate dormant CampaignCombatInheritedReserveCycle nested immutable state/input/result,
full CampaignCombatReserveOpening replay, strict supported initial profile, exact expected command
per occurrence, native receipt/prefix derivation and byte-for-byte event replay. ReadControl and cache
compare against recomputed state. Dedicated codec writes frozen3h grammar; parses input only as
syntax, with complete event bytes checked against re-emission. Existing ordinary Movement remains
unchanged because it rejects held Reserves and requires actual moves.

Evidence: meaningful compiling RED2/2 failed at missing direct Movement completion. Full trace tests
match both predecessor digest lists/bases,20 event hashes and22 Control hashes/lengths. Every retry at
every later cut, cross-owner input/history, changed commands/effects, raw/canonical mutation, forged
control and unsupported predecessor/weather tests pass7/7 (23.541s). Initial negative assertions
incorrectly required exact JsonException instead of its JsonReaderException subtype; corrected tests.
Format applied; full build/test gate pending. Original frozen oracle running independently.

Invariants: World,CP,ammo,TOE,Cohesion,Reserve designation,Weather,RNG,scope preserved through base;
only receipts/version/prefix/position/proof/selection advance. New arrays own bytes; outward events
copy on read. This two-unit certified opening has no ordinary exclusions: verify within two edges
before serializing empty exclusion lists. No generalized Movement policy is inferred.

Challenge points: whether replay admits exactly intended inherited domain, frozen position/receipt
framing, unsupported weather/shape rejection, every preserved field, input/raw canonical handling,
proximity exclusion assumption and tests that might silently miss cases. Full source gates still
pending; source frozen by hash. Parent017 remains open; no positive Release bridge or public gameplay.
