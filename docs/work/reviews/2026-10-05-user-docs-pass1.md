Review instance: **D1 set1/pass1, total1of9**, maximum three sets of three; recovery remains coordinator-owned.

Target: branch `codex/sandtable-user-docs-refresh`, PR160, base `96596dde066b0d8c9a0110eba50fcfcb01d99a46`, head `c7d434e1691c329cbfb9f5ee5f229bd59c2661c2`. Head remained stable throughout review. Exactly seven authorized files changed; only excluded `.serena/` remains untracked. No repository edits made.

## Findings

**No actionable findings.**

The changed summaries accurately distinguish public Rules9 through Combat entry, private settlement/control adapters, and two actual opening histories reaching a candidate before selection. They preserve synthetic provenance and leave actual selection/results, public Combat, authentic full-cycle proof, hosting, Maproom and model-backed play open.

A preliminary ledger was recorded before reading the author explanation.

## Plan Review

The implementation meets the documentation-only acceptance boundary:

- Updates four user-facing files and three dated administrative packets.
- Leaves runtime, contracts, fixtures, canonical roadmap and Combat plan unchanged.
- Describes the reviewed Force Assignment bridge as future contract/implementation work.
- Corrects stale custody and Reserve Release status without promoting synthetic settled contexts into actual reachability.
- Separates `.NET just check`, original Python oracle failures, supplemental successes and historical timeouts.
- Preserves the static website layout, assets, JavaScript and Pages publication workflow.

Onboarding commands agree with `global.json` and `justfile`, including native MTP `--solution` and `--project` flags. No architecture or vocabulary amendment is required for these prose changes.

## Author-Claim Reconciliation

| Claim | Evidence inspected | Status |
| --- | --- | --- |
| Private adapters and actual-entry boundaries are accurately summarized | Canonical roadmap/Combat plan, positive-entry and settled-control contracts, internal native adapter types and focused test source | Confirmed |
| Selection bridge remains future work | Bridge research and coordinator dependency disposition | Confirmed at reviewed head |
| Product/runtime/protected paths remain unchanged | Exact base-to-head path list and diff | Confirmed |
| Scoped links have zero errors | Independent Lychee run | Confirmed |
| Full Markdown corpus has 51 unchanged failures | Independent base archive/head runs; normalized failure records identical | Confirmed |
| Website remains raw static Pages content | `.github/workflows/pages.yml` uploads `site/` directly | Confirmed |
| Revised copy renders and navigation/palette work | Independent local browser inspection, desktop and 390×844 viewport | Confirmed |
| No runtime gate was rerun by author | Author testimony; consistent with scope | Not independently auditable as a historical negative |

The reported Lychee archive checksum verification was not independently repeated. No finding depends on that historical claim.

## Verification Performed

All commands ran from the exact review worktree.

- `git diff --check 96596dde066b0d8c9a0110eba50fcfcb01d99a46 HEAD`: **PASS**.
- `node --check site/app.js`: **PASS**.
- Scoped Lychee command from bootstrap: **322 total, 175 unique, 303 OK, 0 errors, 19 excluded**.
- `python3 /private/tmp/d1-site-check.py`: **PASS** — 24 unique IDs, 14 local assets/anchors, six repository targets.
- Full tracked Markdown, using pinned Lychee and repository configuration:
  - Base: **2360 total, 876 unique, 1881 OK, 51 errors, 428 excluded; exit2**.
  - Head: **2364 total, 876 unique, 1885 OK, 51 errors, 428 excluded; exit2**.
  - All 51 normalized failure records identical. Evidence: `/private/tmp/d1-review-_ypgzda9/`.
- Original `python3 -B docs/specs/verify-combat-…-v1.py` commands independently reproduced:
  - `cycle-sequence`: **FAIL**, `Cna1979LandSequence.cs` source hash.
  - `inherited-breakdown-completion`: **FAIL**, same source drift.
  - `inherited-snapshot`: **FAIL**, declared sequence-source pin.
  - `outward-composition`: **FAIL**, rejected source inventory; canonical digest comparison identifies the separate `combat-content-v7.md` mismatch.
- Browser: revised copy observed; frontier navigation, palette filtering and Escape dismissal work; no captured console warnings/errors. Desktop and narrow-screen rendering showed no defect; mobile page width equals viewport width. Table row/column header scopes remain intact. Temporary viewport reset and review tab closed.

No .NET suite ran. Existing timeouts were not converted into passing evidence.

Serena activated the exact worktree. Codebase-memory searches exhausted relevant results; cited native paths reported matching coverage metadata. Documentation is excluded from the graph and was inspected directly.

## Open Questions And Residual Risks

Full-corpus link debt and Combat pin/admission failures remain real pre-existing failures. This change reports them accurately.

Offline checks establish repository targets, not remote availability. Deployed Pages and exact-head CI were not verified. Browser inspection is proportionate regression evidence, not an exhaustive accessibility audit.

A subsequent S3 merge could stale the reviewed capability wording.

## Verdict

**Ready with non-blocking follow-ups.**

Implementation and plan are supported by independent evidence. No corrective edit or heavy pivot is required.

## Recommended Next Actions

Coordinator should reconcile any newly merged S3 capability immediately before merge/publication, check exact-head CI, and retain separate ownership of historical link debt and deferred pin maintenance. No further reviewer instance is warranted for this unchanged head.
