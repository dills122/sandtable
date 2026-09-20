# PR metadata

Branch: `codex/combat-task008-opening-preamble`; base `codex/combat-task008-creation-snapshot`.

Title: `Add dormant Combat opening-preamble replay`

Opening successors could not previously replay from Combat Created11. Add a dormant four-transition adapter that reconstructs every event and private state cut from trusted creation, rejects forged history, and returns original event bytes with current state on exact retries. Both initiative orders end at Weather entry; World and RNG remain unchanged.

Task008 B is explicitly split into B1 opening and B2 stage-entry, with C Weather between them because stage-entry requires actual Weather history. Parent Task008 and HOST-PUB-001 actual publication proof remain open. This PR stacks on #125.

Validation: six frozen traces, 60 exact-byte fingerprints and 30 state cuts; focused12 passed; full solution1,886 passed, zero failures/skips; build zero warnings/errors; format and local doc links passed. Frozen oracle passed. Dev review complete; three sequential independent reports retained in `.planning/combat-task008-delivery/`.
