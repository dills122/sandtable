# Graziani source compact checkpoint v30

`source-compact-checkpoint-admitted` is an explicit, system-authorized
successor available immediately after source authority begins, including v4.
It does not change the game position, complete setup, consume randomness, or
admit gameplay. The event binds the prior checkpoint hash, successor full
projection hash, and successor compact checkpoint hash.

The compact checkpoint records campaign and setup identities, state version,
source setup position and null game clock, active content and world hashes,
RNG algorithm/seed/cursor, and the version and SHA-256 hash of the complete
canonical legacy projection. That projection includes all retained source
ledgers, hidden choices, battlefield, current world, deferred arrival groups,
and content identities. Later source decisions remain compact; the projection
hash changes with each authoritative mutation. Compact checkpoint bytes and
full projection data are trusted journal material, not player-facing views.

This is a journal checkpoint, **not a standalone restore snapshot**. Recovery
requires retained source genesis and every accepted event, in order. Replay
re-adjudicates each event, reconstructs the complete source authority, and
compares the compact checkpoint and full projection hash. Missing, truncated,
altered or out-of-order history, a mismatched head, or an unrecognized source
profile fails recovery. A compact hash cannot be used to reconstruct state if
the journal or pinned source artifacts are missing.

Existing v3–v29 checkpoint bytes and event semantics remain unchanged. An
existing retained journal may migrate explicitly; old entries are not rewritten.
Early adoption avoids repeatedly storing large embedded source worlds in new
checkpoint rows, although historic rows and nested content artifacts remain
present and full replay still carries their computational cost.
