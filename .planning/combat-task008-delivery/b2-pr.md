# PR metadata

Branch: `codex/combat-task008-stage-entry`; base `codex/combat-task008-weather`.

Title: `Add dormant Combat stage-entry replay`

Accepted Combat Weather history now supports four dormant explicit-none stage-entry transitions through Reserve entry. Validate the full policy before predecessor replay, reconstruct every event from accepted input, and preserve World, Weather, RNG, holder and orders. Authorized historical retries return original event bytes with current state.

Fleet positions retain Commonwealth ActiveSide; final first-acting-side Reserve position remains null and retains the actual side order. This completes Task008 B1/B2 adapter scope; Reserve, first-cycle opening, full restore, public activation and publication remain gated. Stacks on #127.

Validation:17 focused tests,12 chains,60 cuts and228 frozen fingerprints; policy, actor, retry, coherent-forgery, canonical-byte and buffer-isolation negatives. Full solution1,945 passed, zero failures/skips; build/format/diff/local links passed. Frozen stage-entry oracle passed. Dev review and three independent review reports retained in `.planning/combat-task008-delivery/`.
