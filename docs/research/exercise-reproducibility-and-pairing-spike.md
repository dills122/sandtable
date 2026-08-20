# Exercise Reproducibility And Pairing Spike

**Status:** Complete; decisions adopted by the proposed specification

**Date:** 2026-08-19

**Decision owner:** Sandtable project owner

**Governing specification:** [Exercise Harness v1](../specs/exercise-harness-v1.md)

**Delivery plan:** [Exercise Harness v1 technical design](../design/exercise-harness-v1.md)

## Decision question

What versioned seed derivation, executable-build identity, and paired-comparison semantics make
single Exercises and Maneuvers reproducible without overstating common randomness after controller
trajectories diverge?

## Adopted decisions

| ID | Decision |
| --- | --- |
| `DEC-009` | Baseline eligibility requires empty full porcelain status, resolved commit/tree identity, and hashes of the assemblies that actually execute. |
| `DEC-010` | Explicit dirty exploration is nonbaseline/nonreproducible and records hashes without automatically retaining patch, source, or untracked content. |
| `DEC-015` | Seed derivation uses fixed canonical material, SHA-256, and the first eight digest bytes interpreted unsigned big-endian. |
| `DEC-016` | Paired variants share declared initial conditions and initial role/domain streams only; reports make no post-divergence synchronization, causal, balance, or significance claim. |

## Why this matters now

Root seeds alone do not identify independent authoritative, controller, sampling, and diagnostic
streams. Git commit alone does not identify dirty or untracked executed code. Same-seed comparisons
can also be misinterpreted as synchronized random draws even when policies consume the
authoritative stream differently after divergent actions.

## Authorized scope and prohibited actions

In scope:

- current Sandtable RNG contract and hashing conventions;
- Git/build facts available locally;
- Liar's Dice exact seed ledger and paired-run observations;
- deterministic domain separation, build eligibility, and report claims.

Out of scope:

- changing Cna.Core RNG behavior;
- introducing synchronized entity/domain random streams;
- implementing batch execution or statistical significance testing;
- modifying reference repositories.

## Source hierarchy

1. Current Sandtable RNG and canonical hashing contracts.
2. Official cryptographic/platform documentation only where necessary.
3. Liar's Dice and Reef implementation observations, labeled non-normative.

## Decision criteria

- deterministic and versioned seed derivation with explicit domains;
- diagnostics/sampling never consume authoritative randomness;
- exact seed ledger is sufficient to rerun each Exercise;
- baseline eligibility fails closed for unrecoverable dirty builds;
- report language distinguishes shared initial conditions from synchronized future draws;
- controller determinism and Umpire re-adjudication remain separate claims;
- no unnecessary new dependency.

## Stop condition

Stop when seed-scheme inputs/outputs, domain labels, ledger contents, clean/dirty build eligibility,
assembly/source identity, paired-run interpretation, and verification vectors are concrete enough
for the governing spec and tasks.

## Evidence

### Repository facts

- `SandtableRandom` is already a versioned SHA-256 counter stream with an explicit ASCII domain,
  big-endian seed/block encoding, and a recorded byte cursor. See
  `src/Cna.Core/Randomness/SandtableRandom.cs`.
- Campaign creation accepts one `ulong` seed and retains algorithm, seed, and cursor in canonical
  creation event/snapshot bytes.
- Repository canonical identities already use explicit `Utf8JsonWriter` input followed by SHA-256.
  No additional cryptographic dependency is required for deterministic seed derivation.
- [Git porcelain status](https://git-scm.com/docs/git-status) is intended for stable script parsing,
  ignores user color/relative-path configuration, and supports NUL-delimited filenames.

### Reference-repository observations

- Liar's Dice generates an exact list of 64-bit game seeds from one root and reuses that same list
  for baseline and candidate runs. It records every per-game seed and reports both aggregate win
  differences and game-level swings. This is a useful experimental shape.
- Its inspected derivation uses Python's `random.Random(root + scope * 1_000_003)`. The paired
  runner is currently modified, so this is working-tree evidence and is not a suitable Sandtable
  cross-language contract.
- Its SQLite ReplayDB preserves exact per-game seeds but mixes a wall-clock `created_at` into run
  metadata. Sandtable should keep operational time outside canonical run identity.
- Reef records scenario/run/seed/policy/build facts and hashes artifacts, reinforcing the need to
  pin inputs rather than infer them from an output directory name.

### Inferences

- The repository's existing explicit canonical-JSON-plus-SHA-256 pattern is adequate for a
  deterministic seed derivation contract; an HMAC/HKDF package would add complexity without a
  secret-key security requirement.
- Sharing an Umpire seed gives paired variants identical starting state and authoritative random
  stream. If accepted actions diverge, later cursor consumption may diverge. Reports must not call
  later draws synchronized or use a common-random-number statistical claim.
- Assembly hashes identify what executed but cannot reconstruct unavailable uncommitted source.
  Automatically storing dirty patches/untracked files risks retaining secrets and expands the
  artifact trust boundary.

## Options and recommendation

### Seed derivation contract

Define `sandtable.exercise-seeds.v1` using the repository's explicit codec:

```text
seed material = canonical UTF-8 JSON with fixed fields:
  contractVersion = 1
  schemeId = "sandtable.exercise-seeds.v1"
  rootSeed = unsigned 64-bit integer
  maneuverId = stable ID
  exerciseOrdinal = non-negative integer
  pairKey = stable ID or null
  domain = closed lower-kebab token
  role = closed lower-kebab token or null

digest = SHA-256(seed material)
derivedSeed = first 8 digest bytes interpreted unsigned big-endian
```

Initial domains:

- `umpire`, role null;
- `controller`, role `system`, `axis`, or `commonwealth`;
- `artifact-sampling`, role null;
- `diagnostic-sampling`, role null.

The variant/controller implementation ID is not an input to a paired controller-role seed. A
baseline and candidate occupying the same role therefore begin with the same controller stream;
their consumption may diverge after different choices. Every controller must receive only its
own stream and cannot access the Umpire stream.

The Umpire-derived seed is the only value supplied to `CampaignCreationRequest.Seed`. Artifact and
diagnostic sampling never call `SandtableRandom` and cannot advance authoritative cursor state.

Golden vectors must fix exact seed-material bytes, digest, and derived `ulong` for multiple roots,
ordinals, domains, roles, pair/null cases, cultures, and input-order attempts.

Standalone and batch identity is exact:

- a standalone Exercise uses `maneuverId = "standalone." + exerciseId`, ordinal 0, null pair key,
  and the root seed from its Exercise manifest;
- a Maneuver manifest owns the sole root seed and nested entries cannot override it;
- an unpaired Maneuver entry uses its zero-based manifest ordinal and a null pair key;
- both variants in pair repetition N use the same pair key and pair-local ordinal N; execution
  order and variant/controller identities are excluded from seed material.

Campaign ID is derived rather than caller supplied. Scheme
`sandtable.exercise-campaign-id.v1` hashes fixed canonical material containing contract version,
scheme ID, Maneuver/standalone ID, exercise ordinal, and nullable pair key, then formats
`exercise-` plus the lowercase SHA-256 digest. Variant/controller identity is excluded. Paired
variants therefore begin with byte-identical `CampaignCreationRequest` values, including campaign
ID and the Umpire-derived seed.

### Exact Maneuver seed ledger

The canonical Maneuver manifest/ledger records:

- seed scheme ID and root seed;
- Maneuver ID and scenario/setup/ruleset/controller identities;
- Exercise ordinal and deterministic Exercise ID;
- pair key and variant (`baseline`, `candidate`, or `unpaired`);
- every derived domain/role seed;
- exact campaign creation inputs;
- controller configuration hash;
- build eligibility/identity reference.

Files are the source of truth. A later SQLite index is disposable and must reproduce ledger rows
byte-for-byte from manifests.

### Build identity and eligibility

Use two explicit statuses:

1. `baseline-eligible-clean`
   - `git status --porcelain=v1 -z --untracked-files=all` is empty before build/run;
   - record resolvable HEAD commit and tree ID; detached HEAD is valid and common in worktrees;
   - record SHA-256 of the executed runner and relevant Core assemblies plus .NET runtime/SDK facts;
   - a change in any pinned identity produces a different comparison cohort.
2. `exploratory-dirty`
   - record HEAD commit, `dirty = true`, SHA-256 of the exact NUL-delimited porcelain bytes, and
     executed assembly hashes;
   - never label reproducible or baseline eligible;
   - exclude from checked-in baselines, regression gates, and baseline/candidate claims;
   - do not automatically retain patch content or untracked files.

If Git identity cannot be resolved or relevant assembly hashes cannot be read, fail a requested
baseline-eligible run before Exercise creation. An explicit exploratory mode may continue with
`identity-unverified`, but it remains non-baseline and the status is prominent in every report.

### Paired-comparison claim

A valid pair requires equal:

- scenario/setup/content/ruleset inputs;
- Umpire seed and initial campaign canonical bytes;
- target controller role and its initial controller seed;
- harness/build cohort unless the explicit purpose is cross-build regression comparison;
- evidence/check schema versions.

The two variants may differ only in the named candidate policy/configuration/build dimension.
Reports state:

> Paired by identical declared initial conditions and initial RNG streams. Action trajectories and
> subsequent random consumption may diverge after the first differing choice.

V1 outputs descriptive per-pair deltas, outcome swings, invariant/rejection counts, and runtime
diagnostics. It makes no balance, causal, or statistical-significance claim.

### Options rejected

| Option | Failure mode | Decision |
| --- | --- | --- |
| Use one mutable RNG for Umpire, controllers, and sampling | Diagnostics/policy calls perturb authority | Reject |
| Derive seeds with runtime `Random` implementation | Weak cross-version contract | Reject |
| Include controller implementation ID in paired role seed | Baseline/candidate start with different controller randomness | Reject |
| Call dirty runs reproducible using assembly hash alone | Executed source cannot be reconstructed | Reject |
| Automatically archive dirty patches/untracked files | Secret retention and broader artifact boundary | Reject for v1 |
| Claim common random numbers after divergent actions | Statistically misleading | Reject |

## Limitations and unknowns

- V1 does not redesign Core RNG into independent mechanic/entity substreams. Such a design could
  improve future variance reduction but would change authoritative rules identity and replay.
- Controller RNG consumption remains a runner/controller contract. Controller transcript
  repeatability needs its own test and is not proven by Umpire re-adjudication.
- Cross-build comparisons are allowed only when the manifest declares that experimental dimension;
  they are not interchangeable with same-build policy comparisons.
- Exploratory dirty bundles may diagnose current behavior but cannot guarantee source
  reconstruction from retained evidence.

## Implementation consequences and next gate

Before batching, implement and golden-test the seed codec/derivation independently of process
parallelism. Then prove a small serial Maneuver writes a stable ledger and reproduces every
Exercise individually. Add paired comparison only after single-Exercise replay and artifact gates
pass. Parallel scheduling, significance tests, and persistent indexes remain deferred.
