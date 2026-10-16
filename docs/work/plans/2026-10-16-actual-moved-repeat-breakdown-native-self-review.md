# Author engineering self-review: private native moved-repeat Breakdown

Status: unmerged private implementation on `codex/combat-moved-repeat-breakdown`, based on `5d0417b150a7b022bf79ea3ecfc3dfe3fa2d1c5d`. The reviewed wire target is `099c397084c1a5603293fd45f5e1c91006d3fc3d`; its schema and verifier were not changed by native work. This is an author review, not an independent readiness verdict.

## Intent and flow

The consumer in `src/Cna.Core/Campaigns/CampaignCombatActualMovedRepeatBreakdownCompletion.cs` advances a fully replayed R02 ordinal-two Movement terminal through one trusted System Breakdown completion to adjacent Combat Position Determination. It owns caller Source, the original CreationRequest and eleven independent ledgers before physical callbacks, verifies 108 pinned material paths, then replays R02. Only after that does it inspect the own Breakdown ledger. It derives Base, position, capability, event and receipt from the authenticated R02 state. The resulting own Control embeds the complete canonical R02 Control as an immutable string; no new Combat adjudication, cost, loss or RNG draw occurs.

The test `tests/Cna.Core.Tests/Campaigns/CombatActualMovedRepeatBreakdownCompletionTests.cs` compares four boundary paths and all 300 authentic terminal cases against explicit reference-generated fixture bytes. It also checks retry with admission disabled, malformed ancestry and own ledger, changed material at all 108 pins, drift on second acquisition, malformed primitive input, disabled fresh admission, tampered recorded event and closed claim readback. The fixture generator is write-once and refuses an existing output. The canonical fixture hash is `4125311a1bd5779082762e89ee44990129df72881af0d3b9b6d730709fd911ee`.

## TDD and verification

The first native test failed compilation because `CombatActualMovedRepeatBreakdownCompletionResult` did not exist. After implementation and two compiler/analyzer corrections, four reference-matching paid/armed boundary cases passed. The first 300-case native sweep passed in 4m23s. A dependency-drift test then failed because the original callback was acquired only once; replay now verifies a second acquisition and the test passed. The public readback test failed compilation until Source, Control and Proof claim methods were added; they now fail closed while `closureFrozen=false`. A malformed-actor test first received `InvalidOperationException`; primitive shape validation now returns a contract `JsonException`. The pre-review focused class passed 11/11 in 4m58s. After the three independent-review repairs, each targeted test passed and the complete focused class passed **14/14 in 5m01s** on the current C# snapshot. Scoped `dotnet format whitespace --verify-no-changes` and `git diff --check` passed. Exact-head hosted CI remains open.

## Independent review instance 3 disposition

The fresh reviewer returned **Not ready** with three P2 findings. I accept all three. No contract schema, verifier, or fixture bytes changed in this repair.

1. **Malformed own primitives:** required null actor and malformed version/long values could leak `NullReferenceException`, `FormatException`, or a later transition code. `MalformedOwnPrimitiveValuesUseFrozenErrorFamilies` failed RED on null actor. Required own members now reject null, contract-version and long numbers must have integer JSON spelling and range, and malformed values return frozen `001`/`002`; the focused test passes GREEN.
2. **Recorded event and inherited error family:** a malformed recorded event previously returned comparison code `006`, and the parent exception wrapper discarded the ancestor numeric suffix. `RecordedEventAndAncestorPreserveFrozenErrorFamilies` failed RED with `006` rather than `001`. Replay now parses each recorded event before transition comparison and preserves a recognized inherited suffix; the focused test passes GREEN for malformed object/null, whitespace canonicality, and malformed ancestor.
3. **Own ledger ordered too early:** own null entries previously failed before physical callbacks and parent replay. `PhysicalAndAncestorFailuresPrecedeMalformedOwnLedger` failed RED with `001` rather than physical `009`. Replay now snapshots the own ledger without validating it, verifies dependencies and parent first, then enforces own bounds and event count; the focused test passes GREEN.

These changes materially affect parsing and replay order, so another fresh independent review is required. This author disposition is not a readiness verdict.

## Self-review findings and limits

Correctness: the happy path is tied to the original R02 authority, not a transformed B02 ancestor. Event equality is checked on replay; a recorded effect cannot be re-signed independently. Input and ledger buffers are copied before material acquisition. The duplicate path validates command context and actor before returning the original receipt, and a new event requires admission. Claim readers stay closed.

Maintainability: this consumer embeds a compact own-shape canonicalizer and delegates authoritative ancestor reconstruction to R02. The repair tightens required-null and numeric forms but the own canonicalizer still deserves a fresh review of every nested field's type/error family; positive fixtures alone cannot prove exhaustive grammar coverage. The 300-case sweep currently occupies one long test method; its future CI row accounting and separate long-suite allocation need design before publication. No existing shared code was refactored.

Capacity: the current fixtures are sub-megabyte examples of an allowed Source cap of 256 MiB. Local RSS samples during the final 300-case process were 525,648 and 569,648 KiB, not a peak and not near-limit proof. JSON document/node/canonical-byte materialization can multiply memory. Native near-limit memory/latency and a reviewed hosted allocation remain blocking. The 4m58s focused local duration alone does not demonstrate the desired 5–6 minute whole-CI target.

Delivery: `closureFrozen=false`, exact-reviewed-head hosted CI, actual-row/SHA/attempt/process reconciliation, fresh independent native review and verified main merge remain open. This checkpoint is private WIP. It makes no public Combat, full72 or release-candidate claim. The raw archive `27d8293` is not in this payload and remains publication-blocked.

Challenge the 108-path snapshot and second-acquisition semantics; callback mutation after acquisition; own canonical grammar and error families; receipt/capability preconditions before retry; near-limit memory; the test's actual-row reporting; and whether the next release-candidate contract can consume this Control without fabricating state.
