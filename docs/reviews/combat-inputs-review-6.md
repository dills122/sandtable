# Combat rules inputs independent review6

**Review instance:6 of7. Verdict: Ready.** Date2026-09-06.
Fresh-context read-only reviewer `combat_inputs_review_6` (`gpt-5.6-sol`, high effort), no inherited
author conversation. Preliminary ledger delivered before reading the separate author explanation.
No repository files modified by reviewer.

Target: base/HEAD `8bbea59bbcc40d35beb17d7068d269e6834c1288`, branch
`codex/cmb-task-003c-authority-envelopes`, plus one modified plan and four untracked contract files.
No commits after base and nothing staged. Exact five-file hashes verified before and after review.
See retained [bootstrap](combat-inputs-review-6-bootstrap.md),
[manifest](combat-inputs-review-6-manifest.json) and [author packet](combat-inputs-review-6-author.md).
Status-only navigation synchronization was explicitly outside the frozen target.

## Findings

**No actionable P0–P3 findings.**

## Plan Review

The dependency refinement correctly orders003C1→003D1→003C2→003C3→003D2. This prevents placeholder
Rules10 hashes and keeps parent003C/D plus checkpoint B open. Five-primary-file cap met exactly.

Correctness, readability, architecture, security and performance review found no blocking issue.
Decoder bounds bytes, depth, arrays, integers, UTC values, IDs and hashes. No package dependency,
network, runtime or authority-boundary change introduced. Existing bounded source/profile semantics
remain intact; the plan does not mark full003C complete.

## Author-Claim Reconciliation

| Author claim | Evidence inspected / status | Consequence |
| --- | --- | --- |
| Dependency split needed before Rules10 assembly | Confirmed: plan refinement and spec opening | Sequence/cycle artifact precedes full manifest. |
| Three literal goldens and independent calculation path | Confirmed: fixture hashes and `expected_rules()` source-range reconstruction | Same source-research lineage, as disclosed; no new visual audit. |
| Seven explicit windows and accepted fallbacks | Confirmed: fixture versus POL-004/007/008 and DES/CYCLE | First-release I conversion and cycle finish fallback preserved. |
| Timing copies budget/config, checks overflow, expires at equality and preserves probe purity | Confirmed: `make_timing`, `validate_timing`, `clock_gate`; seven clock cases/fourteen boundaries | Value contract supported; lifecycle remains later work. |
| Raw ruleset hash distinct from prefixed artifact/config hash | Confirmed: spec matches `RulesetManifest.CalculateHash()` | No Rules9 alias or placeholder full hash. |
| No Rules10/runtime activation | Confirmed: exact Git scope | Contract-only Ready verdict. |
| Earlier RED/GREEN and token-correction history | Unverified: no retained transcript in frozen target | Current corrected behavior independently verified; no readiness consequence. |
| Config variants bind exact artifact | Confirmed within stated boundary: reader receives validated expected rule hash | Registry admission/authentication explicitly deferred. |

## Verification Performed

Reviewer executed:

| Command | Result |
| --- | --- |
| `python3 docs/specs/verify-combat-rules-inputs-v1.py` | PASS:3 canonical goldens,47 mutations,39 raw-byte rejections,7 clock cases,14 kind/budget/UTC boundaries;360 loss and36 Morale coordinates. |
| `python3 docs/research/verify-combat-source-freeze.py` | PASS:360 normalized loss coordinates,357 source values plus3 accepted amendments. |
| `python3 docs/specs/verify-combat-content-v7.py` | PASS:10,339 canonical bytes,70 rejection vectors. |
| `python3 docs/specs/verify-combat-creation-ledger-v1.py` | PASS:Setup/initial-element hashes,63 rejection vectors. |
| `python3 docs/specs/verify-combat-world-settlement-v1.py` | PASS:6 World goldens,57 vectors,112 cuts,8,840 arithmetic cases. |
| `git diff --check` | PASS. |

`git rev-parse HEAD`, `git branch --show-current`, empty `git log BASE..HEAD`, and
`git status --short --untracked-files=all` matched the bootstrap. Initial `shasum -a 256` and final
manifest comparison matched all five frozen hashes. No .NET build/test or runtime replay executed.

The new canonical artifacts are:

| Object | Bytes | SHA-256 |
| --- | ---: | --- |
| RulesInput | 30,395 | `fafb24792c9e3f774c368f85c02d1d068f84c9c78e67bf8324723257d0f13029` |
| Config | 955 | `3de30c451ba7e81d4fdde6493e07d4d06110209a89035d9e59bba738f0aeba34` |
| Timing | 267 | `aae6214307d4d48fb8bd7b25a525a7d357b123185e6f388b33ce37aec659e26a` |

## Open Questions And Residual Risks

Full Rules10, Created11/Snapshot12, commands/events and sequence/cycle bytes remain future slices.
Production C# byte parity, durable replay, atomic publication, restart, registry admission, archive
authentication, outward privacy mapping and trusted host clocks are not proved by this oracle.
No new independent visual source audit. These boundaries are assigned to later tasks and do not
block003C1. Future full-rules changes must rebind dependent golden identities explicitly.

## Verdict

**Ready** for contract-only003C1. Parent003C remains open.

## Recommended Next Actions

Accept003C1, apply planned status/navigation synchronization, then proceed to003D1.
No material fix or additional review pass warranted. Cumulative review use: **6of7**.

## Author disposition and closeout

**Accept.** No findings require correction. Update plan/current summaries to completed003C1 and
next003D1; preserve full Rules10/parent003C and runtime gates. Reconcile pre-existing broad roadmap
Breakdown status with the already-implemented public/Runner boundary and retained evidence.
Contract/spec/schema/golden/verifier bytes remain identical to the reviewed manifest; only plan
status, navigation and review retention change after review. Those closeout edits do not constitute
an additional independent verdict. Feature-branch commits retain separate contract, review and
navigation groups.
