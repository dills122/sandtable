# PR metadata

Branch: `codex/combat-task008-reserve-designation`; base `codex/combat-task008-stage-entry`.

Title: `Add dormant Combat Reserve designation replay`

Combat Reserve designation now derives its owner from complete accepted creation-to-stage history and changes only that side’s original infantry from none to Reserve I. Canonical event bytes bind the actual designation receipt into own member history; exact authorized retries preserve event bytes and current state. A bounded typed World writer rejects non-history-derived mutations while the initial-only reader stays strict.

Refine Task008 D into D1 designation and D2 completion codec, then019A applies the same completion2 event atomically. Full terminal Reserve replay and Movement handoff remain open until019A. This PR covers24 pre-completion cuts, not completion/cycle opening, generic restore or public activation. Stacks on #128.

Validation:23 focused tests;16 fixture rows,24 cuts,88 frozen fingerprints; actor/retry/forgery/canonical-byte/bounds and typed World mutation checks. Full solution1,968 passed, zero failures/skips; build/format/diff/local links passed. Frozen Reserve oracle passed separately. Dev review and three independent reports retained in `.planning/combat-task008-delivery/`.
