# E2/G1 independent review

Review instance: 1 of 3.

## Preliminary ledger — recorded before author explanation

- Frozen five-file SHA256 manifest verified. Branch `codex/combat-task008-movement-lifecycle`; HEAD `e641bd3b4b14fd1798f607a44a713df2aba47b54`. Four untracked source/test files plus tracked test project fixture link form implementation scope; lead-owned documentation changes remain visible.
- Canonical lifecycle spec/schema, E1 predecessor implementation, E2/G1 scope, execution sequence and lifecycle tests inspected before author explanation. CCE retrieval used for discovery; incidental search snippets included previous report titles but previous reports were not opened or relied on. Session recall omitted to preserve requested isolation from prior review testimony.
- Replay reconstructs full predecessor through E1, rejects zero moves, derives three exact transitions and compares complete bytes. Apply authorizes actor and creation/cycle before exact retry matching; retained input equality covers remaining identity/capability fields. Retry returns historical event with reconstructed current state.
- Stop captures exact cycle and Movement position; empty resolution consumes actual stop and restores captured position. Completion creates all-unit final locations, own exclusions, new completion receipt and unchanged Move4 progress; Reserve completion receipt remains separate.
- Tests pin eight fixture cases/88 golden artifacts, restore every cut, retry earlier commands at later cuts, compare immutable fields, test trusted actors, provenance, scalar mutations and selected coherently re-signed forgeries. Canonical oracle baseline reports 8 traces/24 events/32 cuts/24 retries/946 mutations/152 raw/304 boundaries/16 pins.
- Preliminary concern to reconcile: exclusion helper is a bounded local BFS port; confirm exact canonical predicate and explicit scope rationale. Fixture case inventory protection appears delegated to unchanged oracle rather than runtime test enumeration itself. Neither observation yet establishes a defect.
- Plan retains causal E1 → E2/G1 → G2 sequence and open Reaction, full restore, production and publication gates. No code-level blocker identified on preliminary pass.

## Findings

No actionable findings. Preliminary exclusion concern resolved: `CampaignCombatMovementLifecycle.WithinTwo` implements shortest undirected edge distance ≤2, equivalent to canonical `verify-combat-cycle-control-v1.py:81` exclusion predicate with empty prior exclusions. Repository contains no reusable C# cycle-control exclusion kernel; private port is appropriate for admitted first ordinal.

Fixture inventory concern resolved within retained verification boundary: unchanged lifecycle oracle `check_fixture` requires exactly eight unique owner/count cases, exact expected CP/cohesion/proximity outcomes and all sixteen source pins. Runtime tests compare all eleven golden byte lengths/hashes per case. This is complementary evidence, not a claim that runtime case enumeration alone detects a removed fixture case.

## Plan Review

Implementation follows joint E2/G1 scope and five-primary-file budget. Actual Stop → System resolution → owner completion is necessary causal sequence before G2; shared Movement/Breakdown ownership avoids dependency cycle. Existing typed Breakdown route/stop/flow and writers are reused. New private models/codec preserve inherited authority while adding only lifecycle context and proof.

`docs/design/combat-cycle-implementation-plan.md:819` keeps G2, Reaction F and full restore H separate. README, tech-design, naming overview and roadmap diffs describe actual lifecycle and retain dormant/publication boundaries. No unsupported promotion to Combat entry, positive vehicle handling, general Snapshot12 or durable publication found. Parent integration/full suite remains lead-owned and unfinished in evidence packet at this pass; this verdict covers frozen E2/G1 implementation and its bounded plan.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Full creation-to-E1 prefix required | `CampaignCombatMovementLifecycle.Replay`; `CampaignCombatInheritedMovement.Replay/Initial`; missing-history tests | Confirmed | No cache-only or fabricated idle admission. |
| Actor/provenance before retry; old event/current state | `Apply` and `Authorize`; `FrozenLifecycleCapturesScopeResolvesAndDerivesActualEndProof`; identity tests | Confirmed | Wrong actor, changed creation/cycle and consumed-occurrence changes reject before duplicate success. |
| Capabilities distinct from persisted IDs; retained action identity | `Codec.Capability/Action`; independent `CheckCapabilityAndAction`; frozen input/event goldens | Confirmed | Owner/version and System/version bindings preserved. |
| Captured interrupt restored through real empty resolution | `Emit` stop/resolve branches; resolve codec fields; cut assertions and re-signed context mutation | Confirmed | Generic interrupt cannot replace captured cycle/Movement context. |
| All original units, own distance exclusions, actual receipt | `Emit` completion branch and `WithinTwo`; proof tests both owners at rear/supply; canonical predicate | Confirmed | First proof includes both sides; excludes only own units beyond two; Reserve receipt remains distinct. |
| No World/DP/progress changes | Immutable E1 wrapper; `SerializeState`; immutable-field comparison for all cuts; progress golden and forgery test | Confirmed | All prior material history survives; three lifecycle records add no progress references. |
| Exact strict bytes and causal readback | `DeserializeInput`, `ReadEventInput`, replay full-byte compare, `ReadState`; raw/scalar tests | Confirmed | Noncanonical, forged-effect and mismatched cache bytes reject. State readback uses replay equality rather than independent parser. |
| Focused 20 tests pass | Original `/tmp/e2g1-final.log` plus independent no-build rerun | Confirmed | Twelve lifecycle cases/tests and eight E1 tests pass; zero failures/skips. |
| Build/full integration status | Author packet names root build and pending full suite/format | Partially verified | No reviewer build; full integration remains lead responsibility. No completed full-suite claim accepted. |

## Verification Performed

- `git branch --show-current`, `git rev-parse HEAD`, `git status --short`: scope/base confirmed, untracked files included explicitly.
- `shasum -a 256 -c .planning/combat-task008-delivery/e2g1-source.sha256`: all five match before and after inspection.
- `git diff --check`: exit 0.
- Reviewed canonical lifecycle spec/schema, oracle source/control inventory and unchanged `/tmp/e2g1-lifecycle-baseline.log`; did not rerun oracle. Baseline reports 8 traces, 24 events, 32 cuts, 24 retries, 946 mutations, 152 raw variants, 304 boundaries and 16 source pins.
- `dotnet --version`: `10.0.400`; global.json native MTP, SDK-style net10.0 project and xUnit MTP configuration verified.
- Independent approved no-build command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatMovementLifecycleTests' --filter-class '*CombatInheritedMovementTests' '-bl:/tmp/e2g1-review1-{}.binlog'`. Exit 0; 20 passed, 0 failed, 0 skipped; duration 17s 512ms. Binlog exists at `/tmp/e2g1-review1-20260920-000534--68864--fBD2h2-dotnet-test.binlog`.
- No build, source/test edits, commit, delegation or prior-report review performed. Only this report written.

## Open Questions And Residual Risks

No blocking question. Private serializers repeat inherited layout; frozen goldens constrain current drift but successor work must keep compatibility checks. Runtime tamper suite uses broad invalid-scalar mutations plus selected valid re-signed changes; canonical oracle provides broader well-typed mutation coverage. Verdict does not extend to future multi-route/repeated proofs, positive vehicle/Reaction cohorts, production authentication, general restore or publication durability. G2 must consume retained history rather than trust internal wrappers.

## Verdict

**Ready** for frozen bounded E2/G1 implementation. Code, canonical bytes, focused independent tests and causal delivery plan agree. Parent delivery still requires lead-owned integration checks and remaining configured review rounds.

## Recommended Next Actions

Lead completes full integration evidence and prescribed remaining review rounds, retaining current frozen scope. Continue G2 only after E2/G1 acceptance; preserve open F/H/public/HOST-PUB-001 gates.
