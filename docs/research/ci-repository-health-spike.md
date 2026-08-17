# CIH-001 Repository Health and CI Expansion Spike

**Status:** Decision accepted; Now-stage implementation complete; observation gates open

**Date:** 2026-08-16

**Decision owner:** Project owner

**Evidence commit:** `743a6043d6e71482f0e6f27e0be0843edf892a7a`

## Executive recommendation

Keep Sandtable's current `verify` job as the only required build-and-test gate for now. It is fast,
healthy, and proportionate to the pre-alpha repository: the 23 successful public CI runs complete
in 36 to 55 seconds, averaging 48.1 seconds. The latest `main` run passed, and the same Release
build and native Microsoft.Testing.Platform (MTP) suite passed locally with 184 tests and no
skips.

Expand health controls in three deliberately small stages:

1. **Now:** harden the existing workflow, preserve every `main` validation, log the selected SDK,
   and add a non-required pull-request dependency-review job at a `moderate` vulnerability
   threshold. Pin every action to its official full commit SHA and disable checkout credential
   persistence. Observe dependency review before making it required.
2. **Now, repository settings:** confirm Dependabot alerts and security updates, enable secret
   scanning and push protection, enable CodeQL default setup for C#, and enable private
   vulnerability reporting. Keep CodeQL non-required until its first clean baseline. After action
   pinning lands, require full-SHA action references at repository level.
3. **Next:** decide SDK/dependency lockstep before adding NuGet lock files; add local-only Markdown
   link validation before any network link gate; and collect an informational MTP coverage
   baseline without a percentage threshold. Split NuGet audit warnings from required PR health
   only if advisory churn demonstrates that the current warnings-as-errors policy is noisy.

Do not add an OS matrix, mutation testing, coverage thresholds, package/publish validation,
container scans, browser tests, deployment gates, or artifact attestations yet. Sandtable has no
platform-specific client, publishable release, persistence provider, container contract, or web
surface for those jobs to validate. Add each when its owning capability arrives.

The first delivery slice, `CIH-IMP-001`, merged through pull request
[`#12`](https://github.com/dills122/sandtable/pull/12). It changed only CI workflow behavior:
immutable action pins, `persist-credentials: false`, `ubuntu-24.04`, pull-request-only
cancellation, SDK logging, and one observational dependency-review job. It did not alter product
code, packages, or repository rules.

## Execution index

| Field | CIH-001 boundary |
| --- | --- |
| Objective | Decide which repository-health checks should be added, staged, or rejected without creating expensive or noisy gates. |
| Research scope | Repository and public host evidence, official primary documentation, read-only experiments, and this research artifact. |
| Research exclusions | No workflow, package, source, test, setting, roadmap, design, or product-spec changes; no pull request during the research task. |
| Research completion boundary | Current-state inventory, decision matrix, Now/Next/Later proposal, exact candidates, owner choices, stable implementation IDs, acceptance, verification, and retained sources. |
| Stop condition | A project owner can accept, reject, or revise each candidate without more discovery. |
| Owned artifact | `docs/research/ci-repository-health-spike.md` only. |
| Decision state | `CIH-IMP-001`, `CIH-SET-001`, and `CIH-SET-002` accepted and completed on 2026-08-16 (America/Toronto). |
| Next gate | Observe dependency review for 30 days and one dependency-changing pull request; observe CodeQL across five representative pull requests before considering either check required. |

## Decision and implementation follow-through

These actions postdate the evidence commit. Historical current-state observations below remain the
audit snapshot; this section is the current execution record.

| ID | Outcome | Verification |
| --- | --- | --- |
| `CIH-IMP-001` | Complete. Pull request [`#12`](https://github.com/dills122/sandtable/pull/12) merged as `7689f48b7d867de3f72a4b9c09d5b4f63644138e`. The workflow uses immutable action SHAs, Ubuntu 24.04, non-persisted checkout credentials, SDK logging, PR-only cancellation, and PR-only dependency review at `moderate`. | `verify` passed in 51 seconds. Dependency review initially exposed disabled dependency graph/alerts, then passed in 7 seconds after the prerequisite was enabled. See [run `31982048692`](https://github.com/dills122/sandtable/actions/runs/31982048692). |
| `CIH-SET-001` | Complete. Dependency graph, Dependabot alerts and security updates, secret scanning, push protection, private vulnerability reporting, CodeQL default setup, read-only workflow permissions, and full-SHA action enforcement are enabled. CodeQL remains non-required. | API readback confirmed every setting. Initial CodeQL default setup analyzed Actions in 44 seconds and C# in 1 minute 59 seconds with zero open alerts. See [run `31982906040`](https://github.com/dills122/sandtable/actions/runs/31982906040). |
| `CIH-SET-002` | Complete. The active `main` ruleset now requires review-thread resolution. It preserves zero approvals, loose required checks, squash-only merging, signed commits, deletion/non-fast-forward protection, and required `verify`. | Ruleset `20898169` readback returned `required_review_thread_resolution: true`, `required_approving_review_count: 0`, and `strict_required_status_checks_policy: false`. |
| `CIH-IMP-006` | Deferred at its observation gate. Neither dependency review nor CodeQL is required yet. | Promote dependency review only after 30 days and one dependency-changing pull request. Promote CodeQL only after five representative pull requests, a clean alert baseline, and acceptable timing. |

## Decision question and scope

This spike asks:

> Which additional CI jobs and repository controls provide useful, reproducible signal for
> Sandtable now, which should wait for later capabilities, and what is the smallest safe first
> implementation slice?

In scope:

- existing build, format, analyzer, test, dependency, and workflow behavior;
- branch and pull-request enforcement;
- SDK and dependency reproducibility;
- dependency freshness, vulnerability auditing, and supply-chain controls;
- test reporting, coverage usefulness, artifact validation, documentation checks, CodeQL, and
  secret scanning;
- cost, false-positive risk, permissions, settings, and operational ownership; and
- feature triggers for future web, persistence, container, and deployment gates.

Out of scope:

- changing any workflow or repository setting in this task;
- adding packages, tests, source, or build configuration;
- selecting deployment platforms or container topology;
- treating coverage percentage as a proxy for deterministic or contract correctness; and
- claiming private settings that the public API and unavailable owner authentication could not
  verify.

## Method and evidence labels

The audit used the repository-local `research-to-decision` and `source-driven-development` skills.
It inspected the requested repository files, all project files, test layout, analyzer inputs,
public GitHub Actions history, the active public branch ruleset, official action tags, and current
official GitHub, .NET, MSBuild, NuGet, MTP, xUnit, and candidate-tool documentation.

Conclusions use these labels:

- **Fact:** behavior explicitly documented by an official primary source.
- **Observation:** behavior reproduced or read from the named repository/environment.
- **Inference:** a recommendation reasoned from facts and observations.
- **Unknown:** material state that could not be verified and requires an owner check.

Local observations were made on macOS 26.5 arm64 with .NET SDK `10.0.400`. The repository's
`global.json` requested `10.0.302` with `latestFeature`, so selecting `10.0.400` is expected
roll-forward behavior, not a version mismatch.

## Decision criteria

| Priority | Criterion |
| --- | --- |
| 1 | Protect deterministic adjudication, contracts, replay, and fog-of-war boundaries. |
| 2 | Block defects with high confidence and low false-positive or external-service risk. |
| 3 | Keep required pull-request feedback near the current sub-minute baseline. |
| 4 | Make SDK, dependency, action, and runner inputs auditable and reproducible. |
| 5 | Prefer built-in .NET and GitHub capabilities before adding third-party tools. |
| 6 | Introduce a gate only when the repository contains the capability it can validate. |
| 7 | Give every non-trivial check an owner, escalation path, and removal condition. |

## Current-state inventory

### Repository and stack

| Area | Repository evidence | Assessment |
| --- | --- | --- |
| Solution | `Sandtable.slnx` contains seven source and two test projects, all targeting `net10.0`. | Appropriate single-solution scope. |
| SDK | `global.json` requests `10.0.302`, disallows prerelease SDKs, uses `latestFeature`, and selects MTP. | .NET 10 compatible but permits feature-band drift. |
| Tests | `xunit.v3.mtp-v2` `4.0.0` resolves MTP `2.3.3`; two executable test projects use `UseMicrosoftTestingPlatformRunner`. | Correct native .NET 10/MTP shape. |
| Packages | Central Package Management defines stable versions; `NuGet.Config` clears inherited sources and maps every package to one HTTPS nuget.org source. | Strong source and version baseline. |
| Locking | No `packages.lock.json` exists. | Exact transitive graphs are not retained. |
| Build policy | Deterministic builds, nullable, C# 14, warnings-as-errors, code style in build, and `latest-recommended` analysis are repository-wide. | Strong built-in analyzer policy; analyzer set moves with SDK. |
| Artifacts | `ArtifactsPath` places build output under ignored `artifacts/`; generated protobuf output is not committed. | Clean build tree; no distributable artifact exists. |
| Documentation | README, contributing, security, roadmap, design, specs, and substantial research are present. | Internal-link checking has value; network link gating has outage risk. |
| Releases | README explicitly states that no license or release has been selected and the product is pre-alpha. | Pack, SBOM, signing, and release attestations are premature. |

### Existing CI behavior

**Observation:** `.github/workflows/ci.yml` has one `verify` job on `ubuntu-latest`, triggered for
every pull request and every push to `main`. It has a 15-minute timeout and repository-wide
`contents: read` permission. The job performs:

```text
dotnet restore Sandtable.slnx
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet build Sandtable.slnx --configuration Release --no-restore
dotnet test --solution Sandtable.slnx --configuration Release --no-build
```

This is compatible with .NET 10 native MTP: the repository correctly uses `--solution` instead of
passing the solution as a positional test argument.

**Observation:** public GitHub history contained 24 CI runs at the evidence cutoff: 12 pull-request
runs and 12 `main` push runs. Twenty-three succeeded. One `main` run was cancelled when two merges
arrived close together because workflow concurrency uses `cancel-in-progress: true` for every
event. Successful runs took 36 to 55 seconds, average 48.1 seconds. The latest run took 48 seconds:
9 seconds to set up .NET, 9 seconds to restore, 14 seconds to format, 10 seconds to build, and
3 seconds to test.

**Inference:** cancellation is useful for superseded pull-request commits but inappropriate for
`main`, where every accepted commit should retain a completed post-merge result. The exact
low-cost fix is:

```yaml
concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: ${{ github.event_name == 'pull_request' }}
```

GitHub permits expressions in `cancel-in-progress`. No new job or runner minute is added except
when closely spaced `main` pushes would previously have cancelled an in-flight run.

### Local verification baseline

The following observations were reproduced at the evidence commit:

| Check | Result |
| --- | --- |
| `dotnet restore Sandtable.slnx --force-evaluate --verbosity minimal` | Passed; all nine projects restored from `https://api.nuget.org/v3/index.json`. |
| `dotnet package list --project Sandtable.slnx --vulnerable --include-transitive --no-restore` | Passed; no known vulnerable package in any project. |
| `dotnet package list --project Sandtable.slnx --outdated --no-restore` | Passed; no stable updates in any project. |
| `dotnet format Sandtable.slnx --verify-no-changes --no-restore` | Passed with no diagnostics. |
| `dotnet build Sandtable.slnx --configuration Release --no-restore` | Passed in 7.75 seconds; zero warnings and zero errors. |
| `dotnet test --solution Sandtable.slnx --configuration Release --no-build` | Passed in 1.097 seconds; 184 succeeded, zero failed, zero skipped. |

The initial sandboxed MTP invocation could not create its local IPC pipe. Repeating the exact test
command with local IPC permission passed. This was an execution-environment restriction, not a
repository failure.

### Branch and pull-request health

**Observation:** the public `main` ruleset is active and applies to the default branch. It:

- prevents deletion and non-fast-forward updates;
- requires signed commits;
- requires a pull request and allows squash merge only;
- requires the GitHub Actions `verify` status from integration ID `15368`; and
- uses loose status checks, so a passing branch need not be rebuilt after every unrelated merge.

It requires zero approvals, does not require code-owner review, and does not require review-thread
resolution. All eleven public pull requests were merged through `main`; the latest head and its
post-merge `verify` check succeeded.

**Inference:** keep loose status checks while the repository is small and the post-merge run is
reliable. Requiring strict up-to-date checks would add repeat builds without evidence of merge
conflicts. Enable required review-thread resolution now. Require one approval only when a second
maintainer is consistently available; requiring it for a single-maintainer repository creates an
administrative bypass path rather than meaningful review.

**Observation:** `SECURITY.md` tells reporters to use private vulnerability reporting when
available, but the public repository endpoint reports `enabled: false`. Enabling it closes a real
policy/configuration gap with no CI cost.

**Unknown:** owner-level endpoints for default Actions permissions, Dependabot alerts/security
updates, secret scanning, push protection, and CodeQL default setup required valid owner
authentication. The configured Keychain-backed GitHub CLI credential was invalid, so this audit
did not claim those settings. The absence of CodeQL runs is suggestive, but not proof, that default
setup is disabled.

## Health findings by concern

### Reproducibility and SDK selection

**Fact:** `.NET global.json` `latestFeature` selects the highest installed feature band and patch
within the requested major/minor. `latestPatch` stays in the requested feature band, while
`disable` requires an exact SDK. Microsoft now notes that repositories using package lock files
should set `rollForward` to `disable` so SDK and dependency graph remain in lockstep.

**Observation:** Sandtable requested `10.0.302` but selected `10.0.400` locally. CI uses
`actions/setup-dotnet` with `global-json-file`, but the workflow does not print the SDK actually
selected after setup. `AnalysisLevel` is `latest-recommended`, so a feature-band change can also
change analyzer findings.

**Inference:** add `dotnet --version` to CI now. Do not add lock files until the owner chooses one
of these coherent policies:

| Option | Benefit | Cost/failure mode | Decision |
| --- | --- | --- | --- |
| Exact SDK (`rollForward: disable`) plus lock files | Strongest input repeatability; matches current Microsoft guidance | Contributors and Dependabot must update the SDK pin deliberately; conflicts with README's current “later .NET 10 feature band” promise | Preferred if reproducibility outranks local convenience; owner decision required |
| Feature-band SDK plus lock files | Retains contributor flexibility and exact package graph | Microsoft documents SDK/lockfile edge cases; analyzer and restore behavior can still move | Defer unless a repository experiment proves it stable |
| Current feature-band policy without locks | Lowest maintenance | Transitive graph and analyzer set can drift | Accept temporarily; log selected SDK and keep Dependabot active |

Pinning `AnalysisLevel` to a .NET 10 recommended set can reduce analyzer drift, but it should be
decided with the SDK policy rather than changed independently.

### Supply chain and dependency health

**Observation:** the current baseline is stronger than it first appears:

- Central Package Management pins every direct NuGet package version.
- `NuGet.Config` clears inherited sources, uses one HTTPS source, and maps `*` to that source.
- Dependabot checks NuGet and GitHub Actions weekly. Its first NuGet group updated 13 packages, and
  its first action pull requests updated checkout and setup-dotnet successfully.
- `net10.0` restore audits direct and transitive packages by default. With repository-wide
  `TreatWarningsAsErrors`, NuGet audit warnings can fail restore.
- The live audit found no known vulnerabilities and the live freshness check found no stable
  updates.

**Fact:** NuGet's default audit level is `low`; findings use `NU1901` through `NU1904`. Microsoft
documents how `TreatWarningsAsErrors` elevates those warnings and how to isolate an audit pipeline
with `WarningsNotAsErrors` in normal builds and `WarningsAsErrors` in a dedicated run.

**Inference:** do not run `dotnet package list --vulnerable` on every pull request. It duplicates
restore's .NET 10 transitive audit. Also do not add a scheduled `--outdated` job; Dependabot already
owns freshness and opens actionable pull requests.

Add dependency review in observation mode because it evaluates the dependency diff rather than
only the resolved current graph, produces pull-request context, and will extend to later manifest
ecosystems. Use `moderate`, disable license enforcement until the project has an approved license
policy, and leave the check non-required until it has run successfully on at least one real
dependency pull request.

Keep the current audit failure behavior until it produces actual noise. If an advisory unrelated
to a pull request begins blocking all work, implement `CIH-IMP-002`: normal restores report audit
warnings, while a scheduled/manual health job fails independently. Security alerts and Dependabot
security updates must be confirmed before decoupling the required gate.

Package signature allow-list enforcement is deferred. NuGet supports
`signatureValidationMode=require` and trusted signers, but introducing certificate policy without
a rotation owner can turn certificate maintenance into an availability gate. The single-source
mapping and immutable dependency plan have higher value first.

### Analyzers and formatting

**Observation:** SDK analyzers are enabled through `AnalysisLevel=latest-recommended`, code style
is enforced in build, warnings are errors, `.editorconfig` defines repository conventions, xUnit
ships its analyzers transitively, and Orleans brings its own analyzer package. Format verification
passes. No suppression was found outside the single global warnings-as-errors policy.

**Inference:** no third-party general analyzer suite is justified now. StyleCop, Sonar analyzers,
or `latest-all` would mostly expand policy surface and diagnostic churn while the built-in
recommended set is clean. Add a specialized analyzer only when an owning framework documents one
or a concrete defect class escapes existing checks.

### Test quality and coverage

**Observation:** 184 tests pass in about one second locally, with no skip markers. The suite has
deterministic RNG vectors, replay and canonical serialization tests, content validation and golden
fixtures, campaign authority tests, and protobuf contract tests. This matches the repository's
current authoritative Core and contract maturity.

There are no test projects for OrleansHost, DecisionWorker, Gateway, ServiceDefaults, or AppHost.
Those projects are scaffolds; the roadmap explicitly defers their substantive behavior. CI cannot
substitute for missing behavior tests, so feature work must add focused tests when those boundaries
become real.

The test projects do not reference `Microsoft.Testing.Extensions.CodeCoverage` or a TRX report
extension. xUnit `4.0.0` resolves MTP `2.3.3`. Microsoft's current coverage extension
`18.9.0` supports MTP `>= 2.3.0` and auto-registers through MTP MSBuild.

**Inference:** collect a baseline later, but do not set a percentage threshold. Line coverage
cannot prove replay equivalence, fog-of-war non-disclosure, stale-proposal rejection, or protobuf
wire compatibility. A useful first report should:

- exclude test assemblies, which the Microsoft extension does by default;
- report Core and contract projects separately;
- retain Cobertura output as a short-lived artifact; and
- be reviewed for untested authoritative branches before any threshold is proposed.

Do not enable automatic test retries. Retrying would hide nondeterminism, which is itself a defect
for Sandtable. Add TRX/hang-dump tooling only after a real diagnosis need appears; current console
output and one-second runtime are sufficient.

### Artifact and release validation

**Observation:** the build produces ignored intermediate binaries and generated protobuf code.
There is no NuGet package, release archive, container image, installer, migration bundle, or
deployment manifest.

**Inference:** `dotnet pack`, blanket `dotnet publish`, SBOM generation, image scanning, signing,
and artifact attestations would validate artifacts Sandtable does not distribute. Defer them until
the first release or deployment contract identifies exact subjects. GitHub explicitly advises
attesting released software, not frequent test builds. At that point publish with `dotnet publish`,
generate provenance/SBOM attestations, and verify them as part of the release workflow.

### Documentation and link health

**Observation:** documentation is a first-class delivery artifact and already contains many local
cross-links and external primary sources. No Markdown linter or link checker is configured.

**Inference:** relative-link validation is high signal and independent of the network. Use Lychee
`0.24.2` through immutable `lycheeverse/lychee-action` `v2.9.0` first with `--offline`. Run it in
observation mode against the full Markdown corpus, fix or explicitly exclude the baseline, then
make only the offline check required. External links should run weekly/manual and remain
non-required because rate limits, bot blocking, and transient sites are not repository defects.

Do not add Markdown style linting until the owner approves a repository style configuration. A
default ruleset across the existing research corpus would create formatting churn without
protecting architecture or behavior.

### Secret and security scanning

**Fact:** for public repositories GitHub recommends Dependabot alerts, secret scanning, push
protection, and code scanning; CodeQL default setup automatically chooses supported languages and
events. Dependency review and CodeQL are available to public repositories.

**Inference:** enable those built-in settings now. They are a better security boundary than adding
third-party scanners to the required .NET job. CodeQL should establish a clean C# baseline before
becoming required. Secret scanning and push protection should be enabled immediately; any bypass
must record a reason. Private vulnerability reporting should be enabled to match `SECURITY.md`.

No workflow should use `pull_request_target` to build pull-request code. The current
`pull_request` event, read-only permission, and absence of workflow secrets are appropriate for
public contributions.

### Workflow maintenance

**Observation:** `actions/checkout@v7` and `actions/setup-dotnet@v6` are current official major
versions, and Dependabot maintains both. The references are mutable tags. Checkout credentials are
persisted by default even though no later step performs authenticated Git operations.

**Fact:** GitHub states that a full-length commit SHA is the only immutable action reference and
that Dependabot can update SHA-pinned actions when the version comment is on the same line.

**Inference:** replace the tags with these official release SHAs:

| Action | Official release | Immutable reference |
| --- | --- | --- |
| `actions/checkout` | `v7.0.1` | `3d3c42e5aac5ba805825da76410c181273ba90b1` |
| `actions/setup-dotnet` | `v6.0.0` | `a98b56852c35b8e3190ac28c8c2271da59106c68` |
| `actions/dependency-review-action` | `v5.0.0` | `a1d282b36b6f3519aa1f3fc636f609c47dddb294` |
| `lycheeverse/lychee-action` (Next) | `v2.9.0` | `e7477775783ea5526144ba13e8db5eec57747ce8` |

Use inline release comments so Dependabot keeps the human-readable version. Set
`persist-credentials: false`. Pin `runs-on` to `ubuntu-24.04`, which is the current target behind
`ubuntu-latest`, to avoid an unplanned major-OS migration; hosted image contents will still update
weekly. Review the explicit runner label annually or when GitHub announces deprecation.

Do not add dependency caching yet. Setup-dotnet requires lock files for its cache key, and current
restore is only nine seconds. Reconsider after lock policy is accepted and three cold restores
exceed 15 seconds.

## Candidate decision matrix

Scores are relative: signal and fit range from 1 (low) to 5 (high); cost and noise range from
1 (low) to 5 (high).

| ID | Candidate | Stage | Signal | Fit | Cost | Noise | Decision |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| CIH-C01 | Preserve current Release format/build/MTP `verify` as required | Now | 5 | 5 | 2 | 1 | Accept |
| CIH-C02 | Full-SHA pins, no checkout credentials, explicit Ubuntu 24.04, SDK log | Now | 4 | 5 | 1 | 1 | Accept |
| CIH-C03 | Cancel superseded PR runs but never `main` runs | Now | 4 | 5 | 1 | 1 | Accept |
| CIH-C04 | PR dependency review at `moderate`, license check off, initially non-required | Now | 4 | 4 | 2 | 2 | Accept with observation gate |
| CIH-C05 | GitHub secret scanning, push protection, CodeQL default setup | Now/settings | 5 | 5 | 2 | 2 | Accept; CodeQL initially non-required |
| CIH-C06 | Dependabot alerts/security updates and private vulnerability reporting | Now/settings | 5 | 5 | 1 | 1 | Accept/confirm |
| CIH-C07 | Require review-thread resolution | Now/settings | 3 | 5 | 1 | 1 | Accept |
| CIH-C08 | Require one human approval | Conditional | 4 | 4 | 2 | 1 | Defer until a second maintainer exists |
| CIH-C09 | Exact SDK plus NuGet lock files and locked restore | Next | 5 | 5 | 3 | 2 | Owner decision; preferred coherent lock policy |
| CIH-C10 | Setup-dotnet NuGet cache | Next/conditional | 2 | 3 | 2 | 2 | Defer until locks and restore-cost trigger |
| CIH-C11 | Offline Markdown link validation | Next | 4 | 4 | 2 | 2 | Accept after baseline |
| CIH-C12 | Weekly external link validation | Next | 2 | 3 | 2 | 4 | Accept only as non-required monitor |
| CIH-C13 | MTP Cobertura coverage baseline | Next | 3 | 4 | 3 | 2 | Accept informationally; no threshold |
| CIH-C14 | Dedicated NuGet audit health job | Conditional | 3 | 4 | 2 | 2 | Add only when decoupling audit from PR failures |
| CIH-C15 | `dotnet package list --vulnerable` on every PR | Never now | 1 | 2 | 2 | 2 | Reject as duplicate of .NET 10 restore audit |
| CIH-C16 | Scheduled `--outdated` job | Never now | 1 | 2 | 2 | 2 | Reject; Dependabot is actionable and working |
| CIH-C17 | OS matrix for every PR | Later | 2 | 2 | 5 | 2 | Defer until platform-specific code exists |
| CIH-C18 | Coverage percentage gate | Later/conditional | 2 | 2 | 3 | 4 | Reject until baseline and defect correlation |
| CIH-C19 | Mutation testing | Later/targeted | 3 | 2 | 5 | 4 | Defer to a small authoritative resolver only |
| CIH-C20 | Third-party general analyzer suite | Never now | 2 | 2 | 3 | 4 | Reject; built-in analyzers are clean and strict |
| CIH-C21 | Pack/publish/container/SBOM/attestation gate | Later | 5 | 1 now | 5 | 2 | Trigger on first distributable artifact |
| CIH-C22 | Web lint, accessibility, browser, and end-to-end tests | Later | 5 | 1 now | 5 | 3 | Trigger when Maproom exists |
| CIH-C23 | Database migrations and provider-realistic persistence tests | Later | 5 | 1 now | 5 | 2 | Trigger when Archives selects a provider |
| CIH-C24 | Hosted deployment smoke and environment approvals | Later | 5 | 1 now | 5 | 2 | Trigger when a deployment target is approved |

## Now / Next / Later proposal

### Now

1. Implement `CIH-IMP-001`, the workflow-hardening and observational dependency-review slice.
2. Apply `CIH-SET-001`, the built-in GitHub security baseline, through repository settings.
3. Apply `CIH-SET-002`: require review-thread resolution; preserve loose required checks; defer
   one approval until a second maintainer is available.
4. Keep the existing `verify` job required and unchanged in functional scope.
5. Keep Dependabot's weekly grouped NuGet updates and weekly GitHub Actions updates.

### Next

1. Run `CIH-IMP-003`, an SDK/lockfile experiment, then accept exact SDK lockstep or retain the
   current unlocked policy explicitly.
2. Baseline offline Markdown links with `CIH-IMP-004`; make it required only after the initial
   corpus is clean and exclusions are reviewed.
3. Add `CIH-IMP-005`, a manual/scheduled coverage baseline, only if the owner will review the
   report and convert specific gaps into tests.
4. Implement `CIH-IMP-002` only if NuGet advisory churn demonstrates that blocking every PR is
   harmful. Never silently suppress an advisory; change whether it blocks, not whether it is seen.
5. Promote dependency review and CodeQL to required checks only after the observation acceptance
   boundary is met.

### Later capability triggers

| Trigger | Add then |
| --- | --- |
| Orleans campaign behavior or asynchronous dispatch | In-process and multi-silo tests; cancellation, stale proposal, and grain-turn isolation checks. |
| Gateway provider routing or remote I/O | In-process gRPC tests, deadlines, malformed output, deduplication, fallback, and redaction negatives. |
| Archives persistence provider and migrations | Provider-realistic round trips, migration upgrade/rollback checks, snapshot/event replay, and backup-restore smoke. |
| Maproom web client | Frontend type/lint/unit gates, accessibility checks, browser end-to-end smoke, and side-safe network assertions. |
| OCI image or hosted service | Container build, image startup/health smoke, vulnerability scan, least-privilege runtime checks, and deployment environment approvals. |
| First public binary/package/image | `dotnet publish`/pack validation, SBOM, checksum/signing policy, GitHub artifact attestation, and consumer-side attestation verification. |
| Long or parallel simulations | Scheduled determinism soak, performance budgets, and seed/event replay comparison; never per-PR until bounded. |
| Platform-specific client behavior | Add only the owning OS to a focused matrix; do not matrix the entire solution by default. |

## Minimal first implementation slice

### CIH-IMP-001 candidate patch shape

The first slice should change `.github/workflows/ci.yml` only. This excerpt is exact for the
candidate versions researched on 2026-08-16:

```yaml
concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: ${{ github.event_name == 'pull_request' }}

jobs:
  verify:
    runs-on: ubuntu-24.04
    timeout-minutes: 15

    steps:
      - name: Check out repository
        uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
        with:
          persist-credentials: false

      - name: Set up .NET
        uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
        with:
          global-json-file: global.json

      - name: Report selected SDK
        run: dotnet --version

      # Keep the existing restore, format, Release build, and MTP test steps unchanged.

  dependency-review:
    if: github.event_name == 'pull_request'
    runs-on: ubuntu-24.04
    timeout-minutes: 5

    steps:
      - name: Check out repository
        uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
        with:
          persist-credentials: false

      - name: Review dependency changes
        uses: actions/dependency-review-action@a1d282b36b6f3519aa1f3fc636f609c47dddb294 # v5.0.0
        with:
          fail-on-severity: moderate
          license-check: false
```

Keep top-level `permissions: contents: read`. Do not grant `pull-requests: write`; summaries in
workflow output are sufficient. Do not add `dependency-review` to the required ruleset in the
same change.

Acceptance for the slice:

- existing `verify` remains the required status and passes the exact current commands;
- the log prints the resolved SDK;
- a superseded pull-request run cancels, while two consecutive `main` pushes both complete;
- action references are full 40-character official release SHAs;
- checkout does not persist credentials;
- dependency review runs only for pull requests and requires only `contents: read`;
- the first real NuGet or action update pull request produces a usable dependency result; and
- no product, package, source, test, or design file changes.

Promotion gate: after 30 days and at least one dependency-changing pull request, make
`dependency-review` required if there were no unexplained snapshot failures and its added median
duration remains under 15 seconds. Otherwise keep it informational or remove it.

### CIH-SET-001 settings slice

This slice is an owner action and should not be hidden inside a code pull request:

1. Confirm default `GITHUB_TOKEN` permissions are restricted/read-only.
2. Confirm Dependabot alerts and Dependabot security updates are enabled.
3. Enable secret scanning and push protection; require a recorded reason for bypass.
4. Enable CodeQL default setup for C#; leave it non-required through a clean baseline and at least
   five representative pull requests.
5. Enable private vulnerability reporting so repository behavior matches `SECURITY.md`.
6. After `CIH-IMP-001` merges, enable the repository policy requiring full-SHA action pins.
7. Record the setting review date and owner; repeat quarterly during pre-alpha.

CodeQL promotion gate: no unresolved baseline alerts, no unexplained build failures, and a
five-run median acceptable to the owner. Then add the CodeQL status to the ruleset.

## Exact Next candidates

### Lockfile experiment (`CIH-IMP-003`)

If the owner accepts exact SDK lockstep, the candidate flow is:

```text
# Change global.json rollForward to disable in the implementation task.
# Add RestorePackagesWithLockFile centrally.
dotnet restore Sandtable.slnx --force-evaluate
dotnet restore Sandtable.slnx --locked-mode
dotnet build Sandtable.slnx --configuration Release --no-restore
dotnet test --solution Sandtable.slnx --configuration Release --no-build
```

Commit every project `packages.lock.json`. CI must use `--locked-mode`. Do not enable caching in
the same change; first compare three cold restore timings with the current nine-second baseline.
Acceptance requires a deliberate dependency edit to fail locked restore until lock files are
regenerated, and an unchanged checkout to restore identically on CI and one developer machine.

### NuGet audit policy separation (`CIH-IMP-002`)

Use the official Microsoft pattern only if audit churn becomes a demonstrated merge blocker:

```xml
<PropertyGroup>
  <NuGetAuditCodes>NU1900;NU1901;NU1902;NU1903;NU1904;NU1905</NuGetAuditCodes>
  <WarningsAsErrors Condition="'$(AuditPipeline)' == 'true'">
    $(WarningsAsErrors);$(NuGetAuditCodes)
  </WarningsAsErrors>
  <WarningsNotAsErrors Condition="'$(AuditPipeline)' != 'true'">
    $(WarningsNotAsErrors);$(NuGetAuditCodes)
  </WarningsNotAsErrors>
</PropertyGroup>
```

The dedicated scheduled/manual job runs:

```text
dotnet restore Sandtable.slnx -p:AuditPipeline=true
```

It is intentionally not a required pull-request check. Confirm GitHub alerts and security updates
before merging this policy, and document any `NuGetAuditSuppress` by advisory URL, scope analysis,
owner, and expiry.

### Offline documentation links (`CIH-IMP-004`)

Candidate job after a clean baseline:

```yaml
  docs-links:
    runs-on: ubuntu-24.04
    timeout-minutes: 5
    steps:
      - name: Check out repository
        uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
        with:
          persist-credentials: false

      - name: Check local documentation links
        uses: lycheeverse/lychee-action@e7477775783ea5526144ba13e8db5eec57747ce8 # v2.9.0
        with:
          lycheeVersion: v0.24.2
          args: --offline --no-progress --root-dir "${{ github.workspace }}" './**/*.md'
```

Keep online link checking in a separate weekly/manual, non-required job with bounded retries and a
small reviewed ignore file. An external outage must not block a product pull request.

### Coverage baseline (`CIH-IMP-005`)

Add central package version `Microsoft.Testing.Extensions.CodeCoverage` `18.9.0` and a private
test-project reference to both test projects. Run projects separately to avoid output collisions:

```text
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build -- --coverage --coverage-output-format cobertura --coverage-output Cna.Core.Tests.cobertura.xml
dotnet test --project tests/Cna.Intelligence.Contracts.Tests/Cna.Intelligence.Contracts.Tests.csproj --configuration Release --no-build -- --coverage --coverage-output-format cobertura --coverage-output Cna.Intelligence.Contracts.Tests.cobertura.xml
```

The implementation task must verify the exact output paths under MTP `2.3.3`, retain reports only
briefly, and summarize authoritative branch gaps. No threshold is accepted by CIH-001.

## Implementation work items

| ID | Work item | Depends on | Acceptance | Verification |
| --- | --- | --- | --- | --- |
| CIH-IMP-001 | Harden workflow and add observational dependency review | Owner accepts SHA pins, Ubuntu label, and `moderate` threshold | Meets the minimal-slice boundary above | PR runs `verify` and `dependency-review`; two close `main` pushes both finish; public run timing recorded |
| CIH-SET-001 | Enable/confirm built-in security settings and private reporting | Repository admin | Every setting has recorded state; private reporting enabled; CodeQL baseline created | Readback in Settings/API and links to first CodeQL result |
| CIH-SET-002 | Refine `main` ruleset | Repository admin; maintainer availability decision | Thread resolution required; loose checks retained; approval count matches actual maintainers | Public ruleset readback and one test pull request |
| CIH-IMP-002 | Separate NuGet audit health from required PR health | Demonstrated advisory-blocking incident; CIH-SET-001 alerts confirmed | Advisories remain visible; scheduled/manual audit fails; unrelated PR gate does not | Controlled warning test or reviewed fixture, then full gate |
| CIH-IMP-003 | Decide and implement SDK/dependency lockstep | Owner SDK policy | Locked restore fails drift and succeeds unchanged; docs match supported SDK | `--locked-mode`, Release build, MTP suite, cross-machine comparison |
| CIH-IMP-004 | Add offline local-link check | Baseline scan and exclusion review | All repository-local Markdown links pass; no network dependency | Lychee offline run plus a temporary broken-link negative test |
| CIH-IMP-005 | Produce informational coverage baseline | Owner commits to reviewing gaps | Both test projects emit readable Cobertura; no threshold | Exact per-project MTP commands and report inspection |
| CIH-IMP-006 | Promote dependency review/CodeQL if observation gates pass | CIH-IMP-001 and CIH-SET-001 observation periods | Stable results and acceptable median time | Ruleset readback and protected test PR |
| CIH-IMP-007 | Add capability-triggered integration/release gates | Owning roadmap capability implemented | Gate validates a real contract and has an owner | Feature-specific design and acceptance evidence |

## Required permissions and settings

| Control | Minimum permission/setting | Reason |
| --- | --- | --- |
| Existing `verify` | `contents: read` | Checkout and official .NET setup need read-only source access. |
| Dependency review | `contents: read`; dependency graph/Code Security available | Official basic configuration; no PR comments or write permission. |
| CodeQL default setup | Enable Code Security/default setup in repository settings | GitHub owns language/event selection; no custom workflow required now. |
| Secret scanning/push protection | Enable Secret Protection for the public repository | Detect and prevent supported secret patterns. |
| Private vulnerability reporting | Enable repository private reporting | Aligns host behavior with `SECURITY.md`. |
| Full-SHA enforcement | Enable Actions policy only after every action is pinned | Prevents future mutable-tag workflow changes. |
| Future artifact attestation | `contents: read`, `id-token: write`, `attestations: write`; add `packages: write` only for registry push | Official GitHub provenance flow; not authorized now. |

Do not grant `pull-requests: write`, `contents: write`, `packages: write`, secrets, or cloud identity
to PR validation jobs. Future deployment credentials should use environment-scoped OIDC and
approval, never long-lived secrets in the general CI job.

## Cost and false-positive controls

| Check | Expected cost | Main false-positive/noise source | Control |
| --- | --- | --- | --- |
| Existing `verify` | 36–55 seconds observed | SDK/runner drift; new analyzer diagnostics | Log SDK, pin runner major, keep action updates reviewed |
| Dependency review | Expected seconds; measure before promotion | Missing dependency snapshots, advisory metadata | Initially non-required, `moderate`, no license policy, five-minute timeout |
| CodeQL default setup | Likely minutes; measure baseline | Generated code, framework patterns | Default queries first, triage baseline, require only after stable |
| Secret push protection | No CI minutes | Test/example values resembling secrets | Use documented bypass with recorded reason; never blanket-disable |
| NuGet locked restore | Small runtime cost; review/churn cost is larger | Legitimate manifest changes without regenerated locks | Exact update recipe and Dependabot verification |
| Offline links | Expected seconds plus action download | Anchors/parser differences | Clean baseline and reviewed exclusions before required |
| Online links | Network-dependent | 403/429, bot blocking, outages, moved official pages | Weekly/manual only, cache/retry, non-required |
| Coverage | Instrumentation and artifact storage | Misleading percentage pressure | Informational baseline, targeted gap review, no threshold |
| OS matrix | Multiplies current job cost | Platform image differences unrelated to current code | Add only for owning platform-specific capability |
| Containers/deployment | Minutes plus registry/infra cost | Registry, daemon, service, or cloud outages | Add only after contract exists; separate build from deploy |

## Accepted, rejected, and deferred candidates

### Accepted

- Preserve the current required Release verification job.
- Immutable official action pins, no persisted checkout credential, explicit current Ubuntu LTS,
  selected-SDK logging, and PR-only cancellation.
- Observational PR dependency review at `moderate` severity.
- Built-in GitHub secret, dependency, code-scanning, and private-reporting settings.
- Required review-thread resolution.
- Offline Markdown links and informational MTP coverage as separately baselined Next work.

### Rejected now

- Per-PR `dotnet package list --vulnerable` or scheduled freshness reports that duplicate
  NuGet Audit and Dependabot.
- Required external-network link checking.
- Coverage thresholds, flaky-test retries, mutation testing, and general third-party analyzer
  suites.
- Always-on Windows/macOS matrices before platform-specific code.
- Pack, publish, container, SBOM, signing, and attestation checks before a distributable artifact.
- `pull_request_target` execution of pull-request code or broader workflow token permissions.
- Dependency caching before lock files and a measured restore-cost trigger.

### Deferred with explicit triggers

- Exact SDK plus dependency lock files: owner must resolve reproducibility versus contributor
  feature-band flexibility.
- One required review: a second maintainer must be available.
- NuGet audit separation: a real advisory-blocking incident plus confirmed GitHub alert coverage.
- CodeQL and dependency review as required: clean observation windows.
- Web, persistence, container, release, and deployment jobs: their owning capability must exist.

## Owner decisions

| Decision | Recommended choice | Alternative/consequence |
| --- | --- | --- |
| D1: First slice | Approve `CIH-IMP-001` exactly as bounded | Keep current workflow; leaves mutable action refs and cancellable `main` validation |
| D2: Dependency-review threshold | `moderate`, license check off, initially non-required | `low` is noisier; license enforcement lacks a project policy |
| D3: Built-in security settings | Enable/confirm all in `CIH-SET-001`; enable private reporting immediately | Deferral leaves unverified secret/code/dependency detection and a known reporting-policy mismatch |
| D4: Ruleset review behavior | Require thread resolution; keep zero approvals until a second maintainer; keep loose status | Strict status adds rebuilds; one approval without a reviewer encourages bypass |
| D5: SDK/lock policy | Prefer exact SDK plus lock files if reproducibility is the priority | Retain `latestFeature` without locks explicitly if contributor convenience wins |
| D6: NuGet audit failure policy | Keep current behavior until noise is observed, then use `CIH-IMP-002` | Immediate separation reduces merge blockage but depends more heavily on alert ownership |
| D7: Coverage | Approve only an informational baseline with a named reviewer | Reject entirely if nobody will turn gaps into focused tests |
| D8: Documentation links | Approve offline required after baseline; online weekly/manual only | No checker leaves link rot to review; online required creates external flakes |

## Confidence, limitations, and evidence that would change the recommendation

**Confidence: high** in the workflow, package, test, action-version, public run-history, settings,
and ruleset findings. They were read directly or reproduced. **Confidence: medium** in the
long-term cost and noise of the new GitHub-managed checks: their first runs succeeded, but the
defined observation windows are not complete.

Limitations:

- Owner-level GitHub settings could not be read during the research capture because the available
  GitHub CLI credential was invalid. The post-decision implementation used a valid Keychain-backed
  credential, applied the accepted settings, and verified their current state as recorded above.
- The package vulnerability and freshness results are a point-in-time observation. Advisory and
  package feeds can change after 2026-08-16.
- Local verification used SDK `10.0.400`, not the exact `10.0.302` baseline, because
  `latestFeature` selected the installed feature band. CI does not currently log its resolved SDK.
- Dependency review and CodeQL have only first-run timing data; promotion gates still require the
  defined observation samples.
- The research task itself changed no workflow, setting, package, source, test, or design file.
  The post-decision tasks implemented only the accepted workflow and repository-setting slices.

Revisit the recommendation if the required job exceeds a three-run median of two minutes, if
dependency review or CodeQL produces repeated infrastructure failures, if a second maintainer
joins, if private feeds are introduced, or when a persistence, web, container, or release contract
lands.

## Verification of this research artifact

Required before delivery:

```text
# Audit every retained HTTP source and repository-local link.
# Run Markdown consistency/lint validation on this file.
git diff --check -- docs/research/ci-repository-health-spike.md
git status --short
```

The final delivery report records the exact commands, versions, and results.

## Source index

### Repository and observed host evidence

- Repository files at evidence commit: `AGENTS.md`, `.github/workflows/ci.yml`,
  `.github/dependabot.yml`, `.github/PULL_REQUEST_TEMPLATE.md`, `Directory.Build.props`,
  `Directory.Packages.props`, `NuGet.Config`, `global.json`, `justfile`, `README.md`,
  `CONTRIBUTING.md`, `SECURITY.md`, `Sandtable.slnx`, all project files, test files, and
  `docs/roadmap/pre-alpha-roadmap.md`.
- [Latest successful CI run](https://github.com/dills122/sandtable/actions/runs/31979990045)
- [`CIH-IMP-001` pull request](https://github.com/dills122/sandtable/pull/12)
- [First dependency-review run](https://github.com/dills122/sandtable/actions/runs/31982048692)
- [Initial CodeQL default-setup run](https://github.com/dills122/sandtable/actions/runs/31982906040)
- [Active public `main` ruleset](https://api.github.com/repos/dills122/sandtable/rulesets/20898169)
- [Public pull-request history](https://github.com/dills122/sandtable/pulls?q=is%3Apr+is%3Aclosed)
- [Private vulnerability reporting state](https://api.github.com/repos/dills122/sandtable/private-vulnerability-reporting)

### GitHub Actions, repository health, and security

- [GitHub Actions secure use reference](https://docs.github.com/en/actions/reference/security/secure-use)
- [Workflow syntax: concurrency](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#concurrency)
- [Protected branches and required status checks](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [Repository security quickstart](https://docs.github.com/en/code-security/getting-started/quickstart-for-securing-your-repository)
- [Repository security and analysis settings](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/enabling-features-for-your-repository/managing-security-and-analysis-settings-for-your-repository)
- [Repository security-setting REST endpoints](https://docs.github.com/en/rest/repos/repos)
- [Code scanning default-setup REST endpoints](https://docs.github.com/en/rest/code-scanning/code-scanning)
- [GitHub Actions permissions REST endpoints](https://docs.github.com/en/rest/actions/permissions)
- [Repository ruleset REST endpoints](https://docs.github.com/en/rest/repos/rules)
- [Dependency review configuration](https://docs.github.com/en/code-security/tutorials/secure-your-dependencies/customize-dependency-review-action)
- [Dependabot updates for GitHub Actions](https://docs.github.com/en/code-security/how-tos/secure-your-supply-chain/secure-your-dependencies/auto-update-actions)
- [`actions/checkout` v7.0.1](https://github.com/actions/checkout/releases/tag/v7.0.1)
- [`actions/setup-dotnet` v6.0.0](https://github.com/actions/setup-dotnet/releases/tag/v6.0.0)
- [`actions/dependency-review-action` v5.0.0](https://github.com/actions/dependency-review-action/releases/tag/v5.0.0)
- [GitHub-hosted runner images](https://github.com/actions/runner-images#available-images)
- [Artifact attestation guidance](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations)

### .NET, MSBuild, NuGet, MTP, and xUnit

- [.NET `global.json` selection and roll-forward policies](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)
- [.NET SDK MSBuild properties](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#continuousintegrationbuild)
- [.NET code analysis configuration](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options)
- [.NET 10 `dotnet test` and MTP selection](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)
- [MTP code coverage](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-code-coverage)
- [xUnit v3 with MTP v2](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [xUnit MTP coverage command shape](https://xunit.net/docs/getting-started/v3/code-coverage-with-mtp)
- [NuGet audit and CI policy](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages)
- [PackageReference dependency locking](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#locking-dependencies)
- [.NET 10 `dotnet package list`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-package-list)
- [NuGet configuration, source mapping, and signature policy](https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file)
- [`Microsoft.Testing.Extensions.CodeCoverage` 18.9.0](https://www.nuget.org/packages/Microsoft.Testing.Extensions.CodeCoverage/18.9.0)

### Deferred documentation tooling

- [`lycheeverse/lychee-action` usage](https://github.com/lycheeverse/lychee-action)
- [`lycheeverse/lychee-action` v2.9.0](https://github.com/lycheeverse/lychee-action/releases/tag/v2.9.0)
- [Lychee `0.24.2` command-line behavior](https://github.com/lycheeverse/lychee/tree/lychee-v0.24.2)
