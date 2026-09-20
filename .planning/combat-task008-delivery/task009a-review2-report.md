**Review instance: 2 of 3. Verdict: Not ready.**

**Findings**

**P2 — AdmissionBoundary validates shape/canonical spelling after trusted-history access.**

[CampaignCombatIdentityCodec.cs:95](/Users/dsteele/repos/sandtable/src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs:95) calls generic `ValidateBounds`, then `Admit`, then compares canonical bytes. `ValidateBounds` accepts JSON values such as `null`, `{}`, duplicate-field objects, and whitespace-suffixed valid objects within size limits.

Consequently, malformed Boundary bytes reach retained-history access and replay. With inaccessible history, history exceptions escape before malformed-value rejection. With valid history, unnecessary replay precedes rejection.

This differs from inherited contract oracle’s [read_boundary:124](/Users/dsteele/repos/sandtable/docs/specs/verify-combat-inherited-selection-v1.py:124), which parses typed canonical Boundary before admission. Original [selection contract:77](/Users/dsteele/repos/sandtable/docs/specs/combat-selection-steps-v1.md:77) also explicitly orders shape/bounds, canonical spelling, then trusted-state semantics.

[CombatIdentityTests.cs:252](/Users/dsteele/repos/sandtable/tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs:252) currently expects history access for a malformed partial Boundary containing512 entries, preserving this mismatch.

Smallest correction: validate closed Boundary shape, primitive types, and canonical spelling before `Admit`; retain whole-byte replay comparison afterward. Add inaccessible-history regressions for malformed/noncanonical Boundary inputs. Preserve512/513 boundary testing independently of malformed-object acceptance.

No malformed-input acceptance or authoritative-state corruption found; defect concerns contract ordering and failure behavior. No other actionable findings identified.

**Plan review**

Implementation fits [canonical009 refinement](/Users/dsteele/repos/sandtable/docs/design/combat-cycle-implementation-plan.md:862):

- Admission requires retained creation plus actual completedG2 replay, exact position, supported cycle, and six/seven moves.
- Weather, adjacency, currentCP, and voluntary ceilings derive from replayed state. Unsupported histories reject before returning empty candidates.
- Stable UnitKey/ComponentKey remain separate from current representation/location. Relation probes bind original endpoints; replacement occupants gain no authority.
- Complete entry, history, receipts, prefix, World, and RNG remain retained.
- Internal Core scope and two fixture links introduce no events, public actions, or services.
- README, technical design, naming guide, and roadmap preserve009B/010/011 dependencies and open publication scope.

Positive certification, opportunity mechanics, selection, sealed rounds, settlement, public privacy, and durable publication remain excluded. No parent009 or72AC closure supported. No architectural pivot needed.

**Author-claim reconciliation**

| Claim | Evidence inspected | Status / consequence |
|---|---|---|
| Four actual histories reproduce frozen boundaries | `Admit`; four-case test; frozen fixture lengths/hashes | Source-supported; runtime equality not independently executed |
| Every assessment predicate derives from actual state | `CampaignCombatCertification.Admit` | Confirmed |
| Participant syntax precedes trusted binding | `ReadParticipant`, parser, null-context regressions | Confirmed |
| Boundary reader conforms to frozen contract | `ReadBoundary`; inherited oracle | Contradicted on validation ordering; P2 above |
| Identity continuity grants no eligibility | Binder, stable keys, movement/representation/depletion/relation probes | Confirmed within bounded profile |
| Values own buffers; all arrays capped512 | Participant copies, retained-history capture, codec bounds, ownership tests | Source-supported |
| Nine focused tests pass | Author/checks packet; seven Facts plus two Theory rows | Execution claim unverified independently |
| Final build/full gates establish readiness | Checks packet | Build reported; corrected full-suite acceptance not independently verified |

**Verification performed**

Branch: `codex/combat-task008-reaction-lifecycle`. HEAD/base: `f84b31291afe0b41f4f327223f3c538dcf966b94`. Candidate remains explicit working-tree change.

- `shasum -a 256 -c .planning/combat-task008-delivery/task009a-source.sha256`: all five paths match, including final recheck.
- Read-only Python `-B -c` audit: manifest5/5; fixture links2/2 exist with `PreserveNewest`; four owner/move combinations present; nine test rows identified.
- Frozen Boundary lengths: Axis6/7 =15,809/16,474; Commonwealth6/7 =16,049/16,722.
- Direct Git `diff --exit-code f84b312 -- <inspected contract/schema/oracle/fixture paths>`: no changes.
- Direct Git `diff --check`: clean tracked diff. Newly untracked source inspected separately.
- Source review covered five frozen paths, predecessor replay/ownership models, fixture construction, contracts, oracle admission/readback, and supporting docs.

No .NET commands, TRX writes, oracle rerun, source edits, Git mutations, agents, or extra rounds. CCE retrieval/recall blocked by approval policy; local read-only fallback used. Preliminary ledger recorded in conversation before author/checks access and recall attempt. Prior reports, worker/aggregate artifacts, and execution-history contents not opened.

**Residual risks and next actions**

Runtime behavior remains independently unexecuted under root’s active gate. Hypothetical identity probes prove key continuity only; broader membership/profile admission remains future work.

Return P2 to root for disposition and bounded correction. Root retains final gate and remaining review orchestration. This round starts no additional review.