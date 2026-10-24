# Background expiry of persisted Force rounds

Base `cbc6954d`, branch `codex/rc-overnight-plan-20261023`.
Ruflo task `task-1792878995710-3j4qn7`, namespace `sandtable-rc-20261023`.

## Concrete blocker

`TickForceRound` implements durable cancellation, but its only callers are tests.
The hosted timeout service sweeps RBA alone. Once a Force round is persisted,
disconnected owners can leave it collecting indefinitely without another request.

## Bounded change

Extend hosted combat deadline scheduling to existing collecting Force rounds.
Use independent bounded pagination for RBA and Force, retain per-campaign error
isolation, cancellation and the injected clock. Reuse existing Core V19/V20/V21
cancellation and journal transactions; no fabricated seal or result.

Keep lazy Force opening unchanged: this fix applies to already-persisted rounds.
Creation of Force deadlines at decision entry remains a separate contract/persistence
change. No schema, Core event, public certificate, original P01 or other fallback
window changes belong here.

## Validation

Prove regression before fix with fake-clock scheduler test for persisted Force
expiry. Cover zero/one seal, restart/replay and RBA/Force scheduling independence
where fixture permits. Run focused Host cases then affected Host suite; solution
format/build checks. No full Core run for Host-only scheduling changes.

Prior RC independent-review budget exhausted (3/3). Do not reset it or claim a new
independent acceptance. Coordinator source/diff review and deterministic regression
evidence validate this bounded packet; hosted exact-head CI still required before merge.
