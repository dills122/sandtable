# Task017B bounded contract audit

Read-only contract research for native Result2 → empty Release adapter. Source/spec inspection plus standard-library JSON fixture inspection only; no verifier imports, oracle execution, tests, source edits, or implementation review. Root owns C# API inspection. This note owns no production design decision.

## Exact derivation

Authority: `docs/specs/verify-combat-result-cycle-finish-v1.py`, `checked_source`, `derive`, `apply`; corresponding Markdown and ordered schema. Derive only after complete native Result2 replay and source admission.

| ReleaseBase field | Exact source/value |
| --- | --- |
| contractVersion / profile | `1` / `settled-empty-release` |
| cycle / firstActingSide | Round2 base boundary, copied unchanged |
| positionId | Cycle catalog release position for boundary operationStage/playerPhaseSlot |
| priorVersion / priorPrefix | Final Result2 `stateVersion` / `prefix` |
| combatCompletionReceiptId | Final Result2 `caCompletionReceiptId` |
| retainedWorldHash | SHA256 of canonical final Result2 World, including `sha256:` prefix |
| randomState | Final Result2 RNG, copied unchanged |
| acceptedHighWater | `null`, independently initialized Release scope |
| members | Exactly one member, actual selection attacker element found in settled World |
| attackHistory | Authenticated committed RoundState2 attack history, copied unchanged |

Member unit uses settled World's creation binding and element's original side/ID. Require settled member reserve status `none`; status `none`, pinned `baseCpa=10`, exact settled `operationalState.capabilityPointsExpended` rational. History scope copies gameTurn/operationStage/playerPhaseSlot/actingSide. All nine remaining history fields null: designationReceiptId, conversionReceiptId, releasedType, releaseReceiptId, releaseCycle, cpaBasis, voluntaryCeiling, offensiveCommitmentId, nextMovement. Empty Release means empty pending work, **not** empty members.

Native Release `open`, then `complete`: System actor, `admittedAt=null`, `clockAvailable=true`, no decision/unit/choice. Preserve native event bytes, receipts, versions and prefix chain. Initial Release version/prefix equal final Result2; two native events add exactly two authority versions. No wrapper authority turn. Open yields pending empty/no timing; complete retains empty dispositions and produces native completion receipt. World projection remains byte-identical to settled World; preserve RNG, attack history, future obligations, guards and entitlements. No stage housekeeping.

## Supported source admission

32 source names = eight branches × acting side `axis`/`commonwealth` × first seal `attacker`/`defender`. Full name is `<branch>.<actingSide>.<firstSeal>`. Branches:

- ordinary
- zero-retreat
- refusal-loss-dp
- zero-engaged
- defender-capture-guard
- defender-capture-escape
- attacker-capture-guard-cp-limit
- attacker-capture-escape

Named-case lineage must match exact expected canonical `roundBase`, predecessor, roundInputs, roundEvents and committed RoundState2. Source round includes closed native Round2, exact pre-combat World/step history and both seals. Do not reconstruct Result1 or replace native lineage with final World equality.

Require ResultInput sequence length equal named branch (≤32), and each `(command.kind, command.choice, actor)` tuple equal branch. Result2 reader replays complete supplied suffix and authenticates final state; timestamps may differ from fixture when valid native replay admits them. Source final requires `closed=true`, `status=closed`, window null, non-null CA completion and round closure receipts, and every World settlement's disposition/losses/retreat/relationships non-null. Future obligations remain permitted.

For axis-attacking branch, choices are: ordinary/zero-engaged none; zero-retreat defender `retreat`; refusal-loss-dp defender `refuse-retreat`; defender-capture variants attacker custody `relocate-and-guard`/`leave-unguarded`; attacker-capture variants defender `retreat`, then defender custody `relocate-and-guard`/`leave-unguarded`. Swap sides for commonwealth attacker. All other inputs are System resolve/advance; preserve their exact sequence, not merely choice subsequence.

Fallback admission is narrower than valid Result2 replay. After native replay require every event effect reason absent/null, `owner-choice`, or `not-required`. For **every choose input**, additionally require event author equal submitted actor, effect reason `owner-choice`, effect kind `disposition-recorded` or `custody-settled`, and payload kind equal submitted choice. This rejects accepted clock/controller/deadline fallback and opening-clock fallback even if final World matches named branch. Existing verifier specifically covers 24 authenticated custody/opening forks, eight with identical final World; World-only comparison is insufficient. Do not reject benign retiming: documented 24 altered owner-time audits remain admitted; eight no-window sources have no owner time to alter.

## Frozen evidence reuse

`docs/specs/fixtures/combat-result-cycle-finish-v1.json` contains 32 traces, each with exact `releaseBase` canonical string and four wrapper inputs/events. First two events contain **64 existing native Release event literals**; extract `nativeEvent` without reserialization. Native input can be obtained from native event's `input`, or wrapper nativeCommand plus actor/admittedAt/clockAvailable. Compare native Release outputs to these literals, not wrapper event bytes. Remaining two native events are cycle-control scope, outside this slice.

Bridge fixture has base/source/state hashes and five **wrapper** replay-cut hashes; these are not native Release state hashes. Do not label them Release state fixture evidence. Native Release state can be replayed through production API at zero/one/two-event cuts and checked against its derived authority invariants. Source native World and complete native input/event/state evidence already exist in `combat-result-settlement-v2.json`, joined by trace `name == caseId`. Its fields are `baseCanonicalUtf8`, predecessor object, roundInputs objects, roundEventCanonicalUtf8 strings, committedCanonicalUtf8, resultInputs objects, resultEventCanonicalUtf8 strings, stateCanonicalUtf8. No fixture regeneration or Python subprocess dependency needed.

## API / shape recommendations and pitfalls

- Accept authenticated replay context plus complete Result2 suffix, or equivalent typed native source envelope. A caller-provided Result2 snapshot/ReleaseBase cannot establish source authority alone. Derive ReleaseBase internally and compare every supplied field if serialized base admission exists.
- Keep native Release API reusable; do not route through historical fixture-case `base_for`/`read_base` constructors. Their settled-source path reconstructs Result1. Native base structural validation and source-authenticating adapter are separate responsibilities.
- Preserve canonical ordered wire bytes and native contract versions. Source schema stores native frames as canonical JSON **strings**, not open embedded objects. Raw readers reject missing/unknown/duplicate/reordered fields, floating integers, BOM, trailing bytes and nonfinite numbers. Structured values normalize before hashing. Bridge frame limits: 1 MiB, depth32, arrays512, Result2 events32; full wrapper suffix4. Native tighter bounds still apply.
- Source authentication cache, if introduced, must key exact accepted source bytes and return copies; mutable cached context/final state must not escape. Native result audit high-water remains intact in source even though new Release high-water is null.
- Native Release projection verifies hash binding and complete acting-member coverage, then only changes reserve status. Current supported sources have one acting member. Avoid broadening to arbitrary multi-member sources without new policy/evidence.
- No synthetic Movement certificate is needed to emit native Release events alone. If retaining full bridge shape for later cycle work, certificate must bind pre-retreat boundary World, exact scope/ordinal and sorted boundary element locations, empty exclusions, `probe.result2.movement-completed`, policy `sandtable.combat.synthetic-pre-retreat-movement.v1`, provenance `synthetic`. Never relabel as creation-rooted receipt or substitute settled/post-retreat locations.
- Full bridge policy is `sandtable.combat.native-result2-cycle-finish.v1`; its IDs/receipts use distinct wrapper domains and four-event scope. A narrower production adapter should not claim full bridge/cycle completion merely by reproducing its first two native events.
- Retry same native command/actor should preserve original native receipt and state without RNG/event/version changes. Source admission must precede any Release transition; reject wrong actor, stage, source identity, receipt, stale version and tampered native bytes through native readers.

Bounded recommendation: implement source-authenticating native Result2 adapter, derive exact ReleaseBase, reuse existing native Release lifecycle, prove all 32 bases and 64 native event bytes against frozen evidence, add explicit authenticated fallback rejection and valid-retiming acceptance coverage. Defer cycle control, Movement certification API, repeat, runtime registration and fullSnapshot composition.

## Follow-up: committed-state hash admission

Root proposed pinning independently replayed committed RoundState2 hashes instead of whole upstream tuple. Hash chain binds canonical authority: RoundState2 `baseHash` hashes whole canonical Round2 Base; Base contains boundary and replayed selection Control; Control prefix and receipt event hashes bind selection event bytes with embedded canonical inputs; RoundState2 prefix and receipt event hashes bind Round2 event bytes with embedded canonical inputs. Assuming collision resistance, no canonical authoritative input escapes this chain.

Literal frozen bridge policy is stricter. Its expected predecessor is exact JSON text; historical selection replay reads predecessor boundary/inputs/events and only each event's `canonicalUtf8`, ignoring extra envelope/event metadata. Fixture predecessor events currently contain only `canonicalUtf8`, but supplied extras could be ignored by native replay while committed hash stays fixed; wrapper's exact source comparison rejects them. Therefore committed hash alone proves canonical authority, not arbitrary supplied predecessor serialization. Production may safely use hash pins if its typed upstream context closes all envelope shapes and canonicalizes accepted input; otherwise pin full canonical upstream tuple/hash too. Source admission should document this distinction rather than claim exact raw tuple equivalence without verifying closure. No C# implementation inspected for this finding.
