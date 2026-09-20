Review instance: **1 of 3**. **Verdict: Ready with non-blocking follow-ups.**

**Findings**

**P2 — Participant reader reverses required validation order.** [CampaignCombatIdentityCodec.cs:28](/Users/dsteele/repos/sandtable/src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs:28) checks JSON syntax and bounds, then binds trusted World before checking Participant shape and canonical spelling. [Selection-v1 contract:77](/Users/dsteele/repos/sandtable/docs/specs/combat-selection-steps-v1.md:77) explicitly requires shape/bounds → canonical spelling → trusted-state semantics.

For `null`, missing fields, duplicate properties, or trailing whitespace, binding runs before malformed Participant bytes reject. With foreign unit context, provenance failure masks malformed-input failure. Invalid bytes still cannot be accepted; impact is validation-order conformance and unnecessary trusted-state work.

Smallest correction: validate closed Participant shape and canonical bytes before `BindParticipant`; add malformed-input cases paired with invalid binding context to prove precedence. Existing tests prove rejection, not ordering.

No P0/P1 findings. No authority bypass, unsupported-history-to-empty conversion, or original-participant retargeting found.

**Plan review**

Implementation matches [canonical009 refinement](/Users/dsteele/repos/sandtable/docs/design/combat-cycle-implementation-plan.md:862):

- Admission starts with retained Created11 and complete replay, then requires completed G2, exact cycle/position, six/seven moves, and supported CP12/14 predicates.
- Assessment derives Weather, adjacency, locations, CP and ceilings from replayed state and certified Content.
- Boundary retains existing entry serialization and full owned history. No synthetic reset or caller-supplied assessment authority.
- Unit/component keys remain separate from current representation/location. Contact/Engaged probes preserve original endpoints; probes grant no admission.
- Internal Core scope and two fixture links fit planned five-path slice. Supporting documentation preserves pending009B,010/011 and public-publication boundaries.

Parent009, positive certification, mutable membership transitions, public privacy, settlement and72AC closure remain unproven and unclaimed.

**Claim ledger**

| Author claim | Evidence inspected | Status |
|---|---|---|
| Four actual histories produce frozen boundaries | `Admit`, actual-history test construction, frozen hashes, independent Python oracle | Confirmed implementation/test design; C# execution not independently rerun |
| Unsupported history cannot become empty assessment | Typed replay gate, explicit predicate guards, negative tests | Confirmed by source |
| Stable identity survives movement/representation changes | Binder, immutable Participant, relocation/new-occupant tests | Confirmed within selected profile |
| Full history and buffers remain owned | Retained-history copying, immutable projections, mutation tests | Confirmed by source |
| Arrays512, depth32,1MiB enforced | Recursive codec checks and bounds sentries | Confirmed; World4096 allowance not imported |
| Exact malformed/noncanonical bytes reject | Whole-byte comparison and negative tests | Confirmed; validation-order finding remains |
| Eight focused tests passed | Author/checks packet only | Unverified independently; worker logs not consulted |
| Root build succeeded | Retained root build log | Confirmed log reports zero warnings/errors |
| Full gate/CI complete | No completed evidence inspected | Unverified; no completion claim adopted |

Blind preliminary ledger recorded in conversation before author/checks/session recall. Filesystem prevented report persistence.

**Exact verification**

- `shasum -a 256 -c .planning/combat-task008-delivery/task009a-source.sha256` — all five paths matched, checked twice.
- Git inspection — branch `codex/combat-task008-reaction-lifecycle`; HEAD/base `f84b31291afe0b41f4f327223f3c538dcf966b94`. Four new source/test files untracked; project and supporting docs modified. Historical execution content excluded.
- `python3 -B docs/specs/verify-combat-inherited-selection-v1.py` — exit0: **4 traces,8 events,12 cuts,8 retries,1,126 mutations,46 raw checks,72 boundary checks,6 source pins**. Contract-oracle evidence, not C# execution.
- Read-only Python XML/JSON audit — both fixture links resolve, correct output paths and `PreserveNewest`, valid JSON.
- `git --no-optional-locks diff --check f84b312 -- README.md tech-design.md naming-overview.md tests/Cna.Core.Tests/Cna.Core.Tests.csproj` — exit0; no whitespace errors.
- Read `/tmp/task009a-build.log` — reports build success,0 warnings,0 errors,3.91s.
- All shell calls used `login:false`. No .NET invocation, TRX writes, source edits, Git mutations, agents or further rounds. Oracle process completed.

**Risks and next actions**

Runtime confidence remains limited by prohibited independent .NET execution; root must finish its active gate before delivery. Python success cannot substitute for C# tests. Positive profiles, broader membership changes and future successor-root capacity require their assigned later slices.

Correct validation ordering as bounded follow-up; retain focused/runtime gate evidence against frozen source hashes. Suggested eventual PR title: `Derive inherited Combat admission and participant bindings`.

Automatic approval review rejected CCE search and subsequent session recall because approval policy is `never`. Read-only file inspection supplied review evidence; no approval bypass attempted.
Lead retention note: reviewed source manifest preserved as task009a-source-review1.sha256 before
accepted P2 correction; current task009a-source.sha256 will identify corrected candidate for rounds2/3.
