# Draft PR115 metadata

## Branch

```text
codex/overnight-combat-wave-01
```

## Title

```text
Add privacy-preserving Combat clock contracts
```

## Description

```markdown
Private Combat seal timestamps previously changed whether an opponent’s unchanged proposal succeeded after a clock regression. Add an explicit v2 round/clock configuration whose acceptance floor stays at the published opening instant. Integrate authenticated side profiles that preserve identical observations, candidate identities and outcomes across private seals and clock faults.

Round-v2 and Task004A1 are accepted. Historical v1 contracts, all18 original audience traces and their failing clock diagnostic remain exact. Result/settlement integration, remaining side families, Exercise contracts and Task005 remain incomplete. This draft contains contract evidence only; no Combat runtime activation or merge requested.

## Validation

- Round-v2 oracle:12 semantic groups,10 traces,68 cuts,610 replay mutations,340 raw rejects,288 clock comparisons/retries,480 lifecycle retries.
- Side oracle:23 semantic groups,58 traces/466 cuts,207 submissions,969 mutations,580 raw rejects,69 receipt/stale bindings,672 clock comparisons/retries.
- Ordinary fresh-context reviews approved both packets; source audits verified retained literals, pins and historical bytes.
- Root independently checked88 round outcomes and144 serialized side outcomes;17 legacy profile admission rejects. Historical v1 counterexample retained.
- Root `just check` at each accepted boundary: format/build clean,81 boundary tests,1670 full tests,0 skipped. Latest full-test duration3m25.329s.

Accepted behavior commits:3fcd822 and8cb1cef. Exact logs, scope limits, owner privacy disposition and renewed work deadline are retained under `.planning/2026-09-14-overnight-combat-wave-01/`.
```

PR: https://github.com/dills122/sandtable/pull/115
