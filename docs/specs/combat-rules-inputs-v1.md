# Combat rules inputs and decision-time configuration

**Status:** `CMB-TASK-003C1`, contract-only checkpoint at input `8bbea59`, 2026-09-06.
Parent003C remains open. No Rules10 registration, command handler, snapshot reader or simulator
activation. [Combined plan](../design/combat-cycle-implementation-plan.md) records the dependency
refinement:003C1 inputs →003D1 sequence/cycle bytes →003C2 full Rules10/creation/snapshot →003C3
command/event envelopes →003D2 movement/history reconciliation. Checkpoint B precedes consumers.

The [accepted policies](../design/combat-cycle-policy-reconciliation.md),
[source normalization](../research/combat-source-freeze-v1.md),
[sealed protocol](../design/combat-sealed-decision-protocol-v1.md),
[cycle composition](../design/continual-cycle-reserve-composition-v1.md),
[Setup7](combat-creation-ledger-v1.md) and [World7](combat-world-settlement-v1.md) govern this slice.
The exact [schema inventory](combat-rules-inputs-v1.schema.json) and
[literal fixtures](fixtures/combat-rules-inputs-v1.json) are normative with this text. The inventory
is a descriptor consumed by the local oracle, not a claim of JSON Schema validator compatibility.

## Objects and canonical bytes

| Object | Purpose / exact property order |
| --- | --- |
| `RulesInput` | `schemaVersion, artifactId, profileId, sources, sourceEvidence, amendment, morale, losses, effects, captureShares, procedure, costs, settlement, policies` |
| `Config` | `schemaVersion, configId, profileId, rulesInputHash, codecId, timingPolicyId, windows` |
| `Timing` | `contractVersion, configHash, kind, decisionBudgetMilliseconds, openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds` |

Every nested field/type/order is in the schema inventory. All fields are mandatory; null is never
a substitute. Objects reject duplicate, missing and unknown properties. `int` is signed32-bit;
`utc` is an integer Unix UTC millisecond value from0 through253402300799999 (last complete
millisecond of9999-12-31). This is real admission-clock time, not game-calendar scope. Bool, float,
exponent and numeric-string spellings never stand in for integers. Checked addition must remain
within the UTC range. No conversion to floating-point milliseconds or implicit timezone is allowed.

IDs contain1–128 ASCII characters matching `[A-Za-z0-9][A-Za-z0-9._:-]{0,127}`. Hash is `sha256:`
plus64 lowercase hex. Source locator `text` is1–512 printable ASCII characters. This intentionally
narrow new codec does not alter historical string/escaping contracts. Canonical JSON has fixed
inventory property order, no whitespace/BOM/trailing newline, and standard JSON quoting/backslash
escapes; printable ASCII requires no alternate Unicode spelling. Readers reject alternative
escapes even when decoded text is equal. No normalization, case-folding, field dropping or repair.

All byte inputs are at most1,048,576 bytes; maximum typed depth32 and array length512. Semantic
membership constraints below are tighter. Identity arrays sort by inventory keys, ordinally for
strings and numerically for numbers; duplicate keys reject. Numeric sets sort ascending with no
duplicates. Procedure purpose arrays retain the specified order: never alphabetize or sort dice.
Writers may receive shuffled construction inputs but must emit canonical order. Readers accept
only those canonical bytes. Historical readers/fixtures remain unchanged.

## Selected Rules input artifact

`schemaVersion=1`, `artifactId=cna-1979.1.combat-selected-inputs.v1`, and profile exactly
`sandtable.capability.combat-cycle-infantry.v1`. The artifact contains immutable rule/profile
inputs, not current unit balances, deployment, seed, controller mode or a new campaign snapshot.
Its SHA-256 is over **all canonical bytes**, including provenance and amendment; no self-hash field
or mutable review-status prose participates. The hash is an artifact content hash, not Rules10's
full `rulesetHash`.

| Section | Closed contents and validation |
| --- | --- |
| Sources | Five exact source/locator records in the fixture; four primary-document hashes match the retained source manifest. Rules-lab provenance marks digital policy and the amendment. No claim of independent designer intent. |
| Amendment | Exactly `CMB-SRC-RUL-001`, defender differential+2, ordered coordinates34/35/36, loss10%. Never overwrite another source cell. |
| Morale | Exactly36 ordered d6 coordinates. At Cohesion0:11→+1,66→−1, all remaining coordinates→0. Basic Morale0 and both pre-roll Cohesion0 are profile admission constraints. |
| Losses | Exactly360 cells: two roles × five differentials−2…+2 ×36 coordinates. Preserve357 defined values; fill only the three accepted gaps. Reject missing/extra/duplicate cells and valid-looking values that differ from the retained source normalization. |
| Effects | Exactly five differential rows. Engaged uses attacker pair sum; one-hex Retreat uses defender pair sum; each capture trigger uses the affected role's same pair. Retain raw Engaged separately; required Retreat takes precedence at settlement, including refusal. |
| Capture shares | Exactly six die→percentage rows from chart15.89. Captured TOE rounds up from the eventual loss subset; it is not a second loss debit. |
| Procedure | `sandtable.combat.role-ordered-d6.v1` on existing `sandtable.sha256-counter.v1`; accept bytes below252, map modulo6+1. Eight accepted purposes: attacker Morale tens/ones, defender Morale tens/ones, attacker assault tens/ones, defender assault tens/ones. Conditional ninth purpose is attacker or defender capture share only if triggered. Both cannot trigger on this surface. Continue campaign cursor; do not seed/reset/reserve an unused draw. |
| Costs | Ten TOE per role, ratings1/1, Basic Morale0, required Cohesion0, CPA10. Atomic attacker5/defender3 CP and one ammunition point per committed TOE. Costs precede RNG; current ammo/CP stay solely in World7. Eligibility requires pre-cost CP≤5/7 and carried ammo10. |
| Settlement | Exact scalar values/rounding tokens in inventory/fixture reproduce003B: attacker ceiling, defender floor, capture ceiling; refusal adds10 percentage points per hex; loss≥3 TOE gives3 DP; proved victory gives3 RP capped at Cohesion10; selected Clear retreat costs1 CP per hex. Guard TOE1/CPA10/attack0/defense1/ammo0, guarded path≤3 hexes, escape path≤8 CP, eligibility delay12 operation stages with3 stages per turn. |
| Policies | Exactly `CMB-POL-001`–`008`, contract1, selected behavior tokens below. Tokens identify accepted semantics; their full constraints remain in the policy register and design. They do not implement those rules. |

| Policy | Selected behavior token |
| --- | --- |
| 001 | `closed-singleton-infantry` |
| 002 | `explicit-content-seeds-and-provenance` |
| 003 | `single-ledger-atomic-costs-role-ordered-rng` |
| 004 | `persisted-deadline-fallback` |
| 005 | `atomic-custody-rendezvous-and-delayed-escape` |
| 006 | `side-disclosure-allowlist` |
| 007 | `relative-release-and-cumulative-history` |
| 008 | `supported-cycle-continuation-to-truck-entry` |

The full Rules10 artifact set must additionally carry the exact successor sequence/cycle and
movement semantics; policy tokens alone cannot bind unfrozen fields.003D1 supplies that required
sequence input before003C2 assembles the manifest.003D2's movement/history contracts must reconcile
against the assembled rules before checkpoint B; any changed rule bytes require refreezing the
manifest and all dependent goldens, never silently changing the meaning of an existing hash.

## Decision configuration

`schemaVersion=1`, profile as above, `codecId=sandtable.combat.inputs-json.v1`,
`timingPolicyId=sandtable.combat.fixed-deadline.v1`. `rulesInputHash` binds the validated selected
artifact. `configId` is an explicit admitted identifier, not a hash alias; reusing it with different
budgets still yields different configuration bytes/hash. All seven window kinds occur exactly
once in the following order. Each positive `decisionBudgetMilliseconds` lies in1…2147483647.
Values are explicit per kind, with no implicit/default budget or absent-window shorthand.

| Kind | Fixed fallback token / consequence |
| --- | --- |
| `custody` | `leave-unguarded`: certified escape/entitlement path; never synthesize a guard. |
| `cycle-control` | `finish-phase`: when the supported repeat/finish choice exists and expires, finish once to same-slot Truck Convoy entry. |
| `force-assignment` | `cancel-incomplete-voluntary-round`: preserve accepted own receipts, fabricate no missing seal, spend no costs/RNG. |
| `rba` | `cancel-without-decline`: missing response cannot manufacture Retreat Before Assault decline. |
| `reserve-release` | `convert-unresolved-i-retain-ii`: first release converts remainingI toII in canonical own-unit order; laterII retains. Lock fallback mode once; preserve prior accepted choices; never auto-release. |
| `retreat` | `refuse-retreat`: record source refusal and ensuing loss consequences. |
| `selection` | `no-selection`: close once without attack. |

Config lists **potential** windows, not always-created decisions. Empty selections, no required
retreat/capture, empty release membership and system-only cycle outcomes create no player window
or synthetic timer. Force Assignment uses one shared opening/deadline for both slots. Reserve
Release uses one deadline for the entire segment, never per unit. Cycle control opens only when
immediate obligations are clear, supported continuation exists and material progress permits the
repeat/finish choice. Those lifecycle guards remain003C3/003D2 and runtime tasks010–019.

Golden config ID is `rules-lab.combat-timing.v1`, with30,000ms for each kind. That is an explicit
synthetic fixture configuration, not a physical-game timing rule, recommended hosted service budget,
or hard-coded production default. Admitted variants may change ID/budgets; reader must validate
both bound artifact and full config hash. Approved public policy references will be projected by
004; neither raw full Rules/config authority envelopes nor hidden Content/Setup bindings become
public merely because a configuration contains no hidden deployment.

## Persisted Timing value and trusted clock gate

Opening copies the exact selected budget, sets `openedAtUnixMilliseconds` from trusted input,
computes checked `deadline=opened+budget`, and initializes `highWater=opened`. `configHash` is
SHA-256 of the complete canonical Config bytes, including `rulesInputHash`. Retain that exact
configuration with recovery data; a same-ID replacement config cannot reinterpret a live window.

Only accepted opening/seal/terminal evidence may advance high-water. Rejected submissions, receipt
reads and timer probes leave it unchanged. Stored high-water cannot precede opening; it may follow
deadline when terminal acceptance is late. This value has no duplicate lifecycle flag or slot count.
The containing event/history contract must derive the exact accepted high-water, identify the
window/round and prove its state; shape/arithmetic checks alone cannot authenticate it.

For an already validated collecting window, compare trusted admission input in this order:

1. Unavailable clock, absent trusted instant, or instant below accepted high-water → unavailable
   path; do not renew or extend the deadline.
2. Instant at or beyond deadline → expired path. Equality never accepts another player choice.
3. Otherwise → before-deadline; remaining submission/membership checks still apply.

The comparison is pure and never mutates high-water. It is **not** full submission admission.
Prepared/Committed/Settling have system-owned continuation; choice expiry cannot cancel them.
Closed windows cannot reopen, and stale probes cannot act on another occurrence.003C3 owns accepted
transition receipts, exact duplicates versus conflicting retries, race order, unavailable evidence
and strict replay. C1 tests do not claim those lifecycle properties are implemented.

## Full Rules and authority envelope handoff

Retain current [RulesetManifest](../../src/Cna.Core/Rules/RulesetManifest.cs) canonical framing:
`rulesetId, contractVersion, artifacts, rulings`; artifacts sort by artifact ID and rulings by ruling
ID, with existing nested ordering. Its hash remains **64 raw lowercase hex**. Artifact content and
this Config/Timing use **`sha256:`-prefixed hashes**. Never substitute one hash domain for another.
Rules10 remains reserved: do not register the selected input as the whole ruleset, copy Rules9's
hash into a new snapshot or put an all-zero placeholder in golden creation bytes.

| Remaining owner | Required result |
| --- | --- |
| 003D1 | Exact sequence/cycle artifact, relative occurrence identity and prefix framing. |
| 003C2 | Full manifest bytes/hash from retained Rules9 artifacts/rulings plus exact successors and accepted new artifacts/rulings; Created11/Snapshot12 bind full Rules, Config, Setup, Content and creation request without recursion. Distinguish input artifact hash from full rules identity. |
| 003C3 | Exact commands/events, slot/step/commit/result/settlement receipt identities, accepted-time suffix, frozen-base equality and cursor proofs; compare stored World plus context against trusted creation/history, including simultaneous context+World tampering. |
| 003D2 | Release and movement-history receipts, current versus historical CP/DP truth, membership changes and full CON-002–004 reconciliation. |
| 004 | Audience-safe configuration mapping, candidate bytes and all design-AC/evidence coverage; raw authority hashes never used as outward IDs. |
| 005/008/010–019 | C# byte parity, actual durable atomicity, rollback/recovery, clocks and lifecycle tests before activation. |

No existing generated contract, reader, historical fixture or source artifact is modified. A new
supported configuration requires explicit admission; disabling that admission must leave the exact
compatible reader/config available to already-created campaigns. Digest equality is integrity
binding, not authentication. The future trusted archive/registry supplies the expected identity;
rehashing a semantically unsupported rule must never make it admissible.

## Errors and checked evidence

| Code | First failing condition |
| --- | --- |
| `CMB-INP-001` | JSON/UTF-8, missing/duplicate/unknown property, type/depth/size failure; decoder failures use empty JSON Pointer. |
| `CMB-INP-002` | Primitive grammar/range or nonpositive budget. |
| `CMB-INP-003` | Unsupported top-level version/profile/codec/timing-policy identity. |
| `CMB-INP-004` | Rule/table/policy/provenance differs from closed accepted inputs. |
| `CMB-INP-005` | Wrong rule/config binding or missing/extra/duplicate window/fallback membership. |
| `CMB-INP-006` | Deadline overflow, changed copied budget/deadline or high-water before opening. |
| `CMB-INP-007` | Expected RulesInput content hash mismatch after semantic validation. |
| `CMB-INP-008` | Noncanonical bytes after semantic validation. |

Shape/bounds precede semantic validation, canonical spelling, then expected artifact hash. Semantic
rule comparison traverses canonical sorted values; ordering-only tampering reaches008. Config and
Timing binding checks occur before their deadline/canonical checks. Error codes are private oracle
contracts;004 will define the side-safe rejection projection.

[Verifier](verify-combat-rules-inputs-v1.py) reads retained literal bytes and expected hashes without
rewriting fixtures. Golden generation used the optical coordinate transcription; validation expands
the independently retained source ranges plus the accepted three-cell amendment. Both rely on the
same reviewed source research; this is not a new visual audit or proof of original designer intent.
It checks all loss/Morale coordinates and fixed effects/procedure/policy constants, missing/duplicate
membership, shuffled construction, rehashed unsupported inputs, config substitution and finite
clock boundaries. Full source/Content/creation/World oracles retain their own evidence.
