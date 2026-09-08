# CIH-IMP-004: offline Markdown link baseline

Status: local baseline and acceptance checks pass; observational CI implemented, first hosted run
pending. Required-check promotion remains a separate owner decision.

Date: 2026-09-08. Input: `4c10ede`. Scope: five maintenance files; no product or contract changes.

## Reproduce

Install the appropriate binary from the official [Lychee 0.24.2 release](https://github.com/lycheeverse/lychee/releases/tag/lychee-v0.24.2)
on `PATH`, verifying its published asset digest. Git, Bash and the repository's existing `just`
runner are prerequisites. The command rejects a different Lychee version:

```sh
lychee --version # must print: lychee 0.24.2
just docs-links
```

The [local recipe](../../justfile) and [CI workflow](../../.github/workflows/docs-links.yml) enumerate
the same `git -c core.quotepath=false ls-files -- '*.md'` input list, including tracked hidden
paths. Newly authored Markdown enters this list when added to Git's index. Both invoke Lychee
with the shared [configuration](../../.lychee.toml), repository root and `--files-from` manifest.
One invocation checks the whole list; failures cannot disappear behind a later successful batch.

The installed checker needs no network connection. CI checkout and initial tool download still
use GitHub; offline mode applies to link validation, not workflow provisioning.

## Baseline and exclusions

At input `4c10ede`, all 158 tracked `.md` files were supplied. Lychee reported 1,533 total links,
510 unique, 1,206 successful, 327 excluded and zero errors, unsupported links or timeouts.
Successful/excluded counts are link occurrences, not file counts. Local command completed its
link checks in 14 ms on macOS arm64; this is one local observation, not a CI performance promise.

| Classification | Treatment and evidence |
| --- | --- |
| Tracked ordinary Markdown | All included, including specs, research, reviews and root documentation. |
| Tracked hidden Markdown | All three included: `.codex/steering/repository-steering.md`, `.codex/steering/testing-quality-gates-steering.md`, `.github/PULL_REQUEST_TEMPLATE.md`. A broken hidden fixture fails. |
| External links | All 327 exclusions at the input commit are HTTP(S) links blocked by `offline = true`; no requests are made to validate them. |
| Local URLs or anchors | No exclusions, remaps, accepted-error overrides or ignore entries. Zero baseline fixes required. |
| Generated/untracked/local-only Markdown inputs | Absent from Git's tracked list; excluded from input enumeration rather than suppressed by path regex. A broken untracked/ignored fixture passes until the untracked document is added to Git, then fails. |
| Targets outside the Markdown input list | Still checked when linked. Input selection does not waive target existence or anchor checks. |

No `.planning`, `artifacts`, local skill copies or untracked reports are added as extra scan roots.
No broad exclusion masks missing tracked references. Future exceptions need an exact target or
source, reason and review; a new baseline failure is not permission to add an ignore rule.

## Fragment behavior and acceptance checks

Pinned [Lychee CLI documentation](https://github.com/lycheeverse/lychee/blob/lychee-v0.24.2/README.md#commandline-parameters)
defines `--offline` as local-only validation and `--include-fragments=anchor-only` as anchor
validation. Offline alone does not establish anchor coverage. Configuration enables both, plus
`hidden = true` so explicitly enumerated hidden inputs are checked.

| Check through `just docs-links` | Result |
| --- | --- |
| Valid relative and repository-root-relative local targets | Pass, exit 0. |
| Markdown heading, duplicate-heading suffix and explicit HTML anchor | Pass, exit 0. |
| Missing target in tracked hidden Markdown | Fail, exit 2. |
| Existing Markdown target with missing fragment | Fail, exit 2. |
| Corrected fragment | Pass, exit 0. |
| Broken untracked and ignored generated Markdown inputs | Pass, exit 0; not in tracked input list. |
| Same broken untracked document after `git add` | Fail, exit 2. |

Temporary Git fixtures exercise input selection and real Lychee behavior; no custom Markdown
parser was added. Checks cover static Markdown/HTML anchors. JavaScript-generated anchors,
external page anchors, text fragments and arbitrary non-Markdown target fragment formats are
outside this check's claim. Code/verbatim examples retain Lychee's default treatment.

## CI observation boundary

The separate `Offline documentation links` workflow runs on pull requests, pushes to `main` and
manual dispatch, without path filtering. Permission is `contents: read`; checkout does not
persist credentials. PR supersession cancels earlier PR runs; `main` validation is preserved.

The Lychee action retains its Markdown job summary and numeric result. `fail: false` makes link
failures observational; a following step emits a visible warning for nonzero results. Setup or
empty-input failures still surface as workflow failures. No ruleset or existing required `verify`
job changed. A clean baseline permits observation, not automatic required-check promotion.

The pinned official action entrypoint was run locally with a deliberately broken input: summary
named the missing target, output recorded `exit_code=2`, and `fail=false` returned exit 0. This
checks observation semantics; it does not claim a hosted Actions run. `actionlint` 1.7.12 and
`git diff --check` pass. No .NET gate was run for this tooling-only change.

## Version and pin evidence

- Official [action v2.9.0 tag reference](https://api.github.com/repos/lycheeverse/lychee-action/git/ref/tags/v2.9.0)
  resolves to `e7477775783ea5526144ba13e8db5eec57747ce8`.
- Pinned [action metadata](https://github.com/lycheeverse/lychee-action/blob/e7477775783ea5526144ba13e8db5eec57747ce8/action.yml)
  supports `lycheeVersion`, `args`, empty `token`, report output and observation flag; its
  [entrypoint](https://github.com/lycheeverse/lychee-action/blob/e7477775783ea5526144ba13e8db5eec57747ce8/entrypoint.sh)
  exports the actual checker exit code and writes the job summary.
- Official [Lychee release metadata](https://api.github.com/repos/lycheeverse/lychee/releases/tags/lychee-v0.24.2)
  supplied the macOS arm64 archive digest, verified before executing the binary:
  `c9d3740ea2d891854d37116c9fba840f37b6e7c89d330e7db84ac333631c4977`.
- Runtime output confirmed `lychee 0.24.2`. CI explicitly selects `v0.24.2`; no floating tool version.

Local evidence retained under `.planning/cih-imp-004/` in the primary checkout: tracked manifest,
raw JSON/log, local command log, temporary fixture driver/results/logs and action observation log.
These local files are not publication prerequisites. Baseline counts above remain tied to input
commit; added documentation changes totals. First hosted result and any later required-check
decision should be appended here without rewriting that historical baseline.
