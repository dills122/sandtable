# S3 CodeQL recovery R1 — decision packet

Status: **research complete; source correction recommended; merge remains blocked**.
Prepared 2026-10-05, evidence collected 15:48–15:56 UTC. Decision owner: session-two coordinator; repair owner: original S3 author. This is recovery R1 of at most two, not an independent engineering review pass.

## Exact target and scope

- Checkout: `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`
- Branch: `codex/combat-actual-selection-contract`
- Reviewed head: `e5645c4e4056aa0bd8f9f64de4a76e5c93203782`
- PR: [161](https://github.com/dills122/sandtable/pull/161)
- Failed aggregate check: [111848049126](https://github.com/dills122/sandtable/runs/111848049126)
- Alert: [2](https://github.com/dills122/sandtable/security/code-scanning/2), `py/clear-text-logging-sensitive-data`, high.
- Python analysis: `1894591269`, `refs/pull/161/head`, exact head above; one result, four SARIF paths, no analysis error.
- Oracle SHA-256: `63031bcbc89acc422436f0c4ca1691aa46e9d85e3bc28f19cc3a07c1ba1a9e1a`.

Authorized question: does the alert identify real secret disclosure, and what smallest source repair preserves oracle failure sensitivity? Allowed: exact source/policy/history reads, authenticated GitHub GETs, official query documentation, static file hashing, this report. Prohibited: repository edits, suppression/dismissal, publication, merge, reviewer dispatch, new streams, dynamic tests/builds/PoCs, full-suite lease. Stop condition: complete exact source/sink explanation and a reviewable minimal recommendation. No implementation authority inferred.

## Decision

**Security triage: `not_actionable`, high confidence, for the specific claimed secret disclosure on this fixed private oracle.** This is a name-heuristic false positive: `trusted(...)` constructs test input; it does not retrieve a secret. Exploitability rank queue/rank: null. The CI failure is real and remains actionable as a delivery blocker. This verdict does not authorize dismissal or merge.

Recommend original author replace the failure diagnostic block with count-only output and remove raw rows from the assertion message. Keep collection of every mismatch and the final `assert not failures`. This removes the actual output path without renaming trust concepts, changing comparison semantics, or weakening acceptance. Fresh corrected-head review and CodeQL are required.

## Evidence and exact flow

**Observation — GitHub.** Check 111848049126 is completed/failure at the exact head, with one high alert. All four analysis jobs succeeded. Python CodeQL version is 2.27.1; SARIF query pack is `codeql/python-queries` 1.8.11 and library pack `codeql/python-all` 7.2.6, both carrying source revision `6e9f9e38390175c41b99070a423c875f450759ca`. Analysis success therefore does not mean the security gate passed.

**Observation — SARIF, not a guessed flow.** Its source is `trusted(command(state,kind,**fields),actor)` at oracle line 817, columns 14–56; sink is `row` at line 890, columns 60–62. All four paths follow that source through `state_clock_commands` (835), `original`, `kind`, and generated `prefix` (836), a case label passed to `check` (840, 844, 847, or 849), the mismatch tuple appended at 826, and `failures[:12]`/`row` at 890. The reported path concerns the **label** component of the printed tuple. It does not identify an environment read, credential API, literal password, or external secret.

**Documented fact — exact query-pack source.** The matching [CodeQL heuristic source](https://github.com/github/codeql/blob/6e9f9e38390175c41b99070a423c875f450759ca/shared/concepts/codeql/concepts/internal/SensitiveDataHeuristics.qll#L54) includes `trusted` in its secret-name regular expression. The [Python source model](https://github.com/github/codeql/blob/6e9f9e38390175c41b99070a423c875f450759ca/python/ql/lib/semmle/python/dataflow/new/SensitiveDataSources.qll#L58) follows functions with such names and treats their calls as sensitive sources. This is inference by identifier semantics, not discovery of credential bytes. The [query help](https://codeql.github.com/codeql-query-help/python/py-clear-text-logging-sensitive-data/) describes potential exposure through logs and recommends avoiding sensitive output.

**Observation — repository source.** Oracle line 51 aliases `trusted` from the pinned selection-steps helper. `docs/specs/verify-combat-selection-steps-v1.py:153–154` defines it as a dictionary constructor with fields `command`, `actor`, `admittedAt`, and `clockAvailable`. It has no secret acquisition or I/O. `state_clock_commands:808–817` supplies eight literal command kinds and fixed field constructions. The case prefix at 836 consists of literal owner, one of eight literal variants, numeric cut, and command kind. `trace:328–370` constructs these histories from retained seed1 owner fixture sources with fixed clocks. State and clock mutations are explicit test values.

The mismatch row contains label/expected/actual. Expected/actual are acceptance categories, error numbers, null, or deterministic receipt IDs. Receipt IDs are content digests generated at 256; duplicate return at 187 uses those IDs. The failing assertion at 891 also includes raw rows and can expose them through a traceback, even though only `print` is flagged. Both output channels should be narrowed together.

**Observation — static provenance.** Parsed the `DEPENDENCIES` assignment with `ast.literal_eval`, then SHA-256 hashed its sixteen files without importing/running the oracle. All sixteen matched. No manifest/schema/fixture regeneration is needed for the proposed diagnostic-only change. Focused reference search found no consumer of this verifier in `src`, `tests`, or CI workflow definitions; its documented entrypoint is the Python command below.

**Boundary assessment.** Surface: private executable contract test/fixture, not production telemetry. Source trust: fixed repository developer inputs under admitted hash checks. Applicable root `SECURITY.md` forbids secrets, complete prompts, and hidden state in telemetry. The executable spec/schema explicitly limit this oracle to two fixed original seed1 Normal/NONE owner histories and separate trusted-ledger consistency; they do not claim production authentication. No supported lower-trust secret source or confidentiality boundary crossing was established in the exact alert. Same-privilege arbitrary modification of the test program is not evidence of the reported secret disclosure.

**Inference.** The exact `trusted` heuristic, matching SARIF origin, trivial constructor, and fixed label grammar jointly explain the false positive. Do not generalize this finding to future live campaign integration or other log sinks.

## Minimal repair recommendation, not applied

Replace only the final diagnostic block of `state_clock_precedence_checks`. Three preceding context lines are shown; the following lines are replacement text:

```python
                        participant_input=copy.deepcopy(original);participant_input.update(admittedAt=None,clockAvailable=False)
                        participant_input['command']['participant']['elementId']='foreign.element'
                        check('gate-participant-clock/'+prefix,lambda:transition(b,state,participant_input),('error',4),state)
    if failures:
        print('STATE/CLOCK MISMATCH COUNT:',len(failures))
    assert not failures,(len(failures),'state/clock precedence mismatches')
```

The author should remove the existing group/raw-row print block and raw-row assertion payload when applying this replacement. Mismatch equality checks, appended failures, test loops, success counts, and nonempty-list rejection stay intact. Reduced failure detail is the tradeoff; count and fixed assertion text still identify the gate. Richer diagnostics can be added later using independently constructed safe case ordinals, after proving output safety.

| Option | Assessment |
|---|---|
| Count-only diagnostics and assertion payload | Recommended: one function, no raw values leave through this block, failure sensitivity retained |
| Remove raw `print` only | Incomplete: assertion traceback still contains raw rows |
| Rename/alias `trusted`, encode rows, or alter QL configuration | Avoid: obscures the source or preserves values rather than eliminating output; may fail through tracked aliases |
| Suppress/dismiss alert or bypass CI | Prohibited; not needed |

Expected source repair: one authorized primary file, `docs/specs/verify-combat-actual-selection-v1.py`. No runtime/public-contract/schema/fixture or predecessor pin changes recommended. Current static reference search found no downstream verifier pinning this newly added oracle; the author must recheck against its current head before editing.

## Reproduce the evidence without running the oracle

From the exact checkout, use Keychain-backed GitHub CLI outside the sandbox, unsetting only inherited token overrides for every GET. Never extract token bytes.

```sh
git rev-parse HEAD
git diff --exit-code HEAD
env -u GH_TOKEN -u GITHUB_TOKEN gh api repos/dills122/sandtable/check-runs/111848049126
env -u GH_TOKEN -u GITHUB_TOKEN gh api repos/dills122/sandtable/check-runs/111848049126/annotations
env -u GH_TOKEN -u GITHUB_TOKEN gh api 'repos/dills122/sandtable/code-scanning/alerts?ref=refs/pull/161/head&per_page=100'
env -u GH_TOKEN -u GITHUB_TOKEN gh api -H 'Accept: application/sarif+json' repos/dills122/sandtable/code-scanning/analyses/1894591269
```

Inspect the one SARIF result, its four paths and `relatedLocations`; compare lines 817/822/826/835–849/890–891 with helper lines 153–154. Earlier queries on feature-branch and merge refs returned empty arrays; the authoritative ref is `refs/pull/161/head`. Those empty results were not treated as absence of the alert.

## Required verification for the repair owner

These are recommendations, **not executed in R1**:

1. Run `python3 -B docs/specs/verify-combat-actual-selection-v1.py` on the corrected head. Require exit0 and unchanged successful counts/literals.
2. Reuse the prior independent sensitivity methodology in a disposable/in-memory copy: restore the forbidden-arm/segment ordering defect and the state/clock ordering defect separately. Require each mutation to produce mismatches and fail. Pass3 previously documented 910 mismatches for the exact pass2 state/clock mutation; that number is retained evidence, not a new R1 observation.
3. Force a mismatch with a harmless sentinel in label/receipt payload; capture stdout, stderr, and assertion text. Require failure remains detectable while sentinel/raw rows are absent. Include the assertion channel, not just `print`.
4. Verify only diagnostic output changed; dependency admission, independent trusted-ledger equality, sixteen source pins, framing, schema and golden bytes remain intact. Check `git diff --check` and relevant byte equivalence.
5. Fresh counted engineering review of the corrected immutable head; then exact-head CodeQL and required CI. Do not infer a clean security gate from successful analysis jobs or this static recommendation.

## Review ledger, limitations, and handoff

Read coordinator dependency disposition and `docs/work/reviews/2026-10-05-actual-selection-contract-pass3.md` in `/Users/dsteele/.codex/worktrees/combat-session-two-sync/sandtable`. Retained status: S3 set1/pass3 Ready at this head; total **3 of 9** engineering reviews consumed. Source correction requires original-author follow-up and **set2/pass1, total4of9**, dispatched by coordinator. R1 neither counts as that review nor dispatches it.

Historical Breakdown/cycle-sequence/Snapshot pin failures and outward Content drift remain separate retained failures; original bounded timeouts remain unverified. Diagnostic correction does not repair or waive them. S4 remains conditional on corrected S3 review and exact-head CI/merge. No full-suite lease was taken.

All conclusions are static except observed GitHub check metadata and static hash comparisons. No oracle/test/build/application/PoC or local CodeQL run occurred. Thus clearing the alert after the proposed change remains unverified. At 15:54 UTC CI `verify` was still in progress; dependency review and observational links succeeded. This report does not claim all CI passed.

Serena activated the exact checkout; its C# server does not cover Python. Codebase Memory generation 2026-10-05T14:03:21Z excludes `docs`; exact source reads and focused `rg` supplied evidence. Graph coverage also reports partial C# ranges, so absence of runtime references relies on bounded literal searches rather than graph completeness. CCE tools advertised in checkout AGENTS were unavailable. No reindexing or persistent memory mutation was performed.

Final preservation check: head unchanged; `git diff --exit-code HEAD` exit0; only pre-existing `.serena/` untracked. No source edits, credential extraction, dismissal/suppression, commits, publication, merge, reviewer dispatch, new stream, or outbound message was performed. Coordinator should retain this report, request the narrow author correction, and own subsequent review/publication. Original closeout18:29:14UTC and hardstop19:29:14UTC remain unchanged.
