# 005A ordinary review — APPROVE

2026-09-19 UTC. Four-file dormant arithmetic/type slice reviewed against packet005,
source-audit005, frozen RulesInput1 schema/golden and selected optical source fixture.
No open findings. Ordinary review; no formal independent-review pass. Approval
covers005A only; strict codec005B and final integration gate remain required.

## Assessment

- Correctness/source fidelity: complete typed RulesInput1 metadata, five references,
  four document hashes, three-cell amendment, all numerical/procedure/policy fields
  match retained golden. Production manual bands remain independent from optical
  expected arrays. Only defender+2 coordinates34/35/36 receive amendment10%; only34
  retreats. Capture die3 retains literal33/100, not Breakdown's1/3 convention.
- Pure arithmetic validates both d6 digits for all four ordered coordinates,
  enum role, differential[-2,2], capture die presence/range exactly when triggered
  and refusal only on retreat. Attacker ceiling, defender floor after refusal,
  capture ceiling/subset, total-loss DP and TOE conservation are correct. Raw
  Engaged stays separate from retreat. Fixed selected inputs bound checked math.
- Readability/design: small internal records and pure explicit-dice helper. Table
  factory and lookup assumptions remain closed by immutable static definition;
  no caller-supplied table participates in Resolve. Domain result exposes original
  percentages, raw effects and losses separately.
- Boundaries/immutability: all nested arrays copied into read-only collections;
  nested metadata is sealed immutable records/strings. Existing RuleReference is
  get-only. Returned lists cannot mutate authoritative definition. No World/RNG,
  clock, live campaign admission or registration changes.
- Performance: one static definition; bounded direct table lookups and tiny
  effect sets. Analyzer-only final changes use six explicit capture-share records
  and cached test options/constant arrays; no arithmetic change.

## Evidence

Author report `/private/tmp/005a-verification.json` and corresponding logs reviewed.
Initial build passed; targeted RED had10 genuine assertion failures, exit2.
Final build exit0, zero warnings/errors,3.56 seconds. Final focused command:

```text
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class 'Cna.Core.Tests.Rules.CombatSelectedRulesTests'
```

Final log `/private/tmp/005a-green-tests.log`:19 passed,0 failed,0 skipped,
exit0,1.371 seconds. Tests assert36 Morale cells,360 optical loss cells/3 amended
gaps,1296 Morale pairs,6480 joint coordinates,8840 capture/refusal rows and44208
weighted capture paths. Literal rounding/refusal/capture examples,33-percent
source share, all invalid input classes and nested-copy/write protection included.
Full definition comparison uses retained RulesInput1 golden; no expected loss
table generated through production bands. Existing fixture files linked by csproj.

Reviewer statically reviewed all four files, final analyzer changes and logs;
independent `shasum -a 256` exit0 matches all four frozen hashes below. No duplicate
native test run. Author scoped whitespace format/diff checks pass. Root independently
reports six legacy Rules/registration files retain baseline hashes. Rules9 remains
registered; selected artifact absent from current manifest. Root owns final full
`just check` after005B; no full gate claimed for005A alone.

## Frozen SHA-256

| File | SHA-256 |
| --- | --- |
| src/Cna.Core/Rules/CombatSelectedRules.cs | 3d45c5baf2c7db9d4f4924cb9e49cb6bd7fc3ef4d797a85d9267e91bcbc54bfc |
| src/Cna.Core/Rules/Cna1979CombatAdjudication.cs | 6dc28a87d30cdfaa37599babf610b13012c04e3f5de6bab99f09bbd5038ba6eb |
| tests/Cna.Core.Tests/Rules/CombatSelectedRulesTests.cs | c08852e21099bcedb9662754e0165aaff25ace54f4d6dd1db2a5e6812efddb6d |
| tests/Cna.Core.Tests/Cna.Core.Tests.csproj | 511743ed9525726b1875b68bbd08a2f3d0c6e23bd8e231d98e4fc3dd36e042d7 |
