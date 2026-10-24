# Maproom dependency triage — October 24, 2026

Baseline `16d76448a008ea4f16299402b8ef44e8c96a4843`; no dependency changes.
`npm --prefix src/Cna.Maproom ci` succeeded, installing 529 packages.
`npm --prefix src/Cna.Maproom run typecheck` succeeded. This is dependency
inventory triage, not exploit validation or a security-fix acceptance result.

`npm --prefix src/Cna.Maproom audit --json` exited 1 and reported:

| Package | Severity | Installed path / dependency | Disposition |
| --- | --- | --- | --- |
| `vite` | High | `@angular/build@22.2.3` pins nested `vite@8.3.0` | Root `vite@8.3.4` does not replace this nested copy. A compatible builder update or reviewed override needs separate validation. |
| `fast-uri` | Moderate | Transitive `node_modules/fast-uri`; audit affected range 3.0.0–3.1.8 | Determine every consumer and validate a compatible patched lockfile before claiming remediation. |
| `@angular/build` | Low | Direct development dependency | Audit suggests 22.1.9; automatic downgrade is not accepted as a compatibility decision. |

`npm --prefix src/Cna.Maproom explain vite --json` confirmed both installed Vite
copies and the builder's exact nested pin. Advisories include development-server
file-serving and editor-launch paths. This session did not prove exploitability,
production reachability, or absence of exposure; local loopback configuration
alone is not a waiver. Retain this as a distribution follow-up until a reviewed
dependency patch and relevant frontend/browser checks pass.

Evidence: `/tmp/sandtable-rc-npm-audit-20261023.json`,
`/tmp/sandtable-rc-vite-explain.json`,
`/tmp/sandtable-rc-maproom-typecheck-20261023.log`.
Ruflo task: `task-1792807038337-bb2s0g`.

Next bounded packet: verify supported Angular builder patch versions and their
Vite pins; choose a compatible remediation, regenerate lockfile, rerun audit,
`just maproom-check`, and relevant connected-read/browser tests. Do not apply
`npm audit fix --force` or treat a clean audit alone as runtime verification.
