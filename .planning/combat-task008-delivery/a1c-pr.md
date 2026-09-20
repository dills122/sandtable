# PR metadata

Branch: `codex/combat-task008-creation-binding`

Commit message / PR title: `feat(combat): bind dormant creation requests to Created11`

## Summary

Task008 A1c adds dormant canonical creation requests and Created11 readback. Trusted Rules10,
Setup7, Content7 and configuration determine exact bytes; request-derived identity binds initial
World7 without an event-hash cycle. Retained exact retries precede fresh-admission checks, while
changed requests reject.

## Validation

- 52 focused C# tests,81 disclosure-boundary tests; final full solution1,840 pass, zero failed/skipped.
- Frozen envelope, creation-ledger, rules-input and World-settlement Python oracles pass.
- Solution build passes without warnings; format verification passes after targeted fixes.
- Dev review and three independent rounds complete. Round2 ID-length finding fixed; round3 Ready.

## Scope

Internal creation slice only. Snapshot12, inherited replay, atomic publication, provider selection
and public gameplay activation remain later gates. No claim of completed parent Task008.
