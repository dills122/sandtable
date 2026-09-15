# Current draft PR metadata

## Branch and accepted clock commit

```text
codex/overnight-combat-wave-01
3fcd8229f077521759fdb5ae2aaa942aa91f89ac
```

## PR title

```text
Add privacy-preserving Combat clock contracts
```

## PR description

```markdown
Private Combat seal timestamps previously changed whether an opponent’s unchanged proposal succeeded after a clock regression. Add an explicit v2 round/clock configuration whose acceptance floor stays at the published opening instant. Keep the original deadline, deterministic fallback, receipt recovery and Prepared ordering; preserve historical v1 contracts and their failing privacy diagnostic.

The clock packet is accepted. The retained side-projection candidate is still being integrated; Task004/checkpoint B and Task005 remain incomplete. This draft contains contract evidence only and activates no Combat runtime. No merge requested.

## Validation

- Round-v2 oracle:12semantic groups,10traces,68cuts,610replay mutations,340raw rejects,288clock comparisons/retries,480lifecycle retries,30invalid proposals.
- Ordinary fresh-context review approved after exact fixture-byte validation fixed numeric type coercion.
- Independent source audit:136canonical literals,58events,6pins and derived identities;15historical files unchanged.
- Root independent admission check:88equal-outcome comparisons; historicalv1 counterexample retained.
- Root `just check`:format/build clean,81boundary tests,1670full tests,0skipped.

Clock packet commit:3fcd822. Exact logs, scope limits, owner privacy disposition and renewed eight-hour deadline are retained under `.planning/2026-09-14-overnight-combat-wave-01/`.
```

PR: https://github.com/dills122/sandtable/pull/115

Clock packet accepted; side integration in progress;004/B and005 incomplete. No merge.
