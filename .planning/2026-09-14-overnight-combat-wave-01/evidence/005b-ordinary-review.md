# 005B ordinary review — APPROVE

2026-09-19 UTC. Reviewed new codec/tests and dormant artifact factory against
packet005, accepted005A and frozen RulesInput1. No open findings. Ordinary
review; no formal independent-review pass. Scoped code approval; root's final
full integration gate remains required before parent Task005 acceptance.

## Five-axis assessment

- Correctness: canonical writer emits every RulesInput1 metadata/provenance,
  amendment, numeric table, procedure, cost, settlement and policy field. Identity
  arrays sort by schema keys; numeric sets sort numerically; duplicate keys reject.
  Procedure-purpose arrays retain order. Only exact complete authority can pass.
  Authority initialization checks frozen hash; no new artifact version introduced.
- Readability/quality: explicit writer helpers keep field ordering reviewable.
  Structured normalization and strict raw admission remain separate. Codec
  returns shared immutable definition only after exact expected-hash/byte checks.
- Architecture: new internal dormant codec and factory do not register artifact,
  activate Rules10 or alter existing CombatRules/Cna1979Combat behavior. No static
  initialization cycle: selected definition does not depend on codec initialization.
- Security/boundaries: byte equality against closed authority rejects unknown,
  missing, duplicate, reordered, escaped, alternate numeric and malformed input
  before any caller-controlled JSON tree allocation. Byte/depth/array violations
  necessarily differ from authority and reject. Structured arrays bounded512,
  writer depth32 and size1MiB; scalar validators reject invalid roles/IDs/hashes.
  Caller self-hashes cannot override frozen identity. Byte outputs are fresh;
  sorted copies preserve constructor order; returned definition remains immutable.
- Performance: one cached authority buffer and lowercase hex search set; bounded
  sorting/serialization only for typed construction. Raw reader compares spans
  without allocating input JSON trees. No new dependency or registration path.

## Evidence

Initial focused RED: successful build,5 real assertion failures, exit2,1.122s.
Final build exit0,zero warnings/errors,1.26s. Final focused command:

```text
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class 'Cna.Core.Tests.Rules.CombatRulesInputArtifactTests'
```

`/private/tmp/005b-tests.log`:52 passed,0 failed,0 skipped,exit0,1.369s.
Tests compare exact30,395 golden bytes/hash; reversed structured arrays/sets
normalize without mutating inputs;9 duplicate families and2 purpose-order cases
reject. Complete authority/provenance mutations reject both original and forged
expected hashes;17 raw spelling/framing mutations, scalar/depth/array/byte bounds,
historical/Config/Timing boundaries and defensive copies/factory nonregistration
are covered. Scoped formatting and diff check pass.

Reviewer statically reviewed all implementation/test bodies and final analyzer
changes (cached SearchValues, nullable test annotation, whitespace). Final
standard-library `python3 -B` check exit0 (tool chunk7d25ee) independently matches
all3 file hashes to `/private/tmp/005b-verification.json` and hashes retained
30,395-byte RulesInput1 golden to
`sha256:fafb24792c9e3f774c368f85c02d1d068f84c9c78e67bf8324723257d0f13029`.
Final native test log reviewed directly; reviewer did not duplicate native suite.
Root final full gate was running when this scoped review completed.

## Frozen SHA-256

| File | SHA-256 |
| --- | --- |
| src/Cna.Core/Rules/CombatRulesInputArtifactCodec.cs | dcd5e3c5a5f1e19fb1129d4463960aa7055b2df7f1e710df468004b6a91f7118 |
| src/Cna.Core/Rules/Cna1979CombatAdjudication.cs | 35d702bdd4dfd59995c0b581cc0525f31a9092cb66b604a27173231f247109f4 |
| tests/Cna.Core.Tests/Rules/CombatRulesInputArtifactTests.cs | 9f67d158fe4ffe2545d67c52a87d2b14cea5e02ec02dee9e3eb0de7f74238067 |
