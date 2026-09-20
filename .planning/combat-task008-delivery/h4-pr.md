Task008 now replays one owned, creation-rooted retained event stream through selected pre-cycle,
Movement and Reaction paths, then writes and restores literal inherited Snapshot12 roots.

H4 derives the root only from causal replay through existing strict typed readers. Restore requires
independently retained Created11 and complete event history, invokes the creation cut with fresh
admission disabled, and compares exact canonical root bytes. Missing history, conflicting forks,
forged effects, malformed roots and oversized inputs reject. Actual Reaction position and suspended
Movement position remain distinct; creation-only Snapshot12 compatibility is preserved.

All 368 reference roots across 286 histories match exact bytes, lengths and hashes. Eleven focused
tests pass, including full disabled-admission restore, ledger/prefix checks, causal-slot mutations,
pre-history bounds and defensive ownership. Build and full format pass. Full solution 2,244/2,244 and boundary 81/81 pass. All three independent reviews returned Ready; round 3 independently reran 11 focused tests.
Exact-head GitHub verification and CodeQL passed at30e9921. H4 / Initial H Core codec/replay gate
is accepted; publication evidence remains open. Task009A inherited assessment/bindings is next.

Prior F2–F6 and H0–H3 acceptance evidence is retained. H0 includes one bounded CodeQL experiment
and final fourth review. This is one main-based draft PR. Scope is bounded Initial H; later gameplay,
public activation, all 28 runtime traces and HOST-PUB-001 durable publication remain open.
