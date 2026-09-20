# Task010B independent review

Review instance: 1 of 3. Reviewer mode; fresh independent context.
Base: `6e6dd6fad206e886f2ceeba5c9c031158e5fdfa7`.
Candidate: `762468e5a2744957b5b720905b45d455721d3037`.
Branch: `codex/combat-task008-reaction-lifecycle`.
Initial Git status clean; HEAD equals candidate. All five task010b-source.sha256 entries independently match working files. Diff limited to five source/test files, four supporting docs, and delivery metadata. Restricted delivery metadata contents excluded.

## Findings

No actionable findings. Reviewed candidate satisfies bounded010B implementation and canonical plan refinement. This review does not accept parent010 or subsequent gates.

## Preliminary blind ledger — persisted before author/checks packets

No actionable finding established by blind source/contract/test inspection. Runtime verdict pending independent checks.

| Concern | Blind evidence | Disposition |
| --- | --- | --- |
| Event payload authenticates itself | SelectionSteps.Replay copies and syntax-checks events, independently captures trusted inputs, regenerates each event and compares complete bytes | No defect found; caller provenance remains explicit prerequisite |
| Positive admission silently broadened | ValidateBoundary pins C2 creation binding; existing009B CertifyInitialProfileFacts compares full initial profile except integral current CP, scope and geometry; boundary retains distinct opening/current prefixes | No defect found; no actual positive-history claim |
| Timing/retry ordering | Transition field/actor checks precede command-hash duplicate recovery; ClockGate preserves historical high-water/deadline policy; stale callbacks precede closed/version checks | Matches frozen oracle, including unavailable pre-RBA cancellation |
| Positive flow overruns prepared gate | Selected stepIndex >=3 rejects; decline required at RBA; cancelled/no-selection completes six steps | Matches010B scope; Prepared remains011 |
| Canonical grammar bridge changes old contracts | IdentityCodec new preserveArrayOrder parameter defaults false; C3a only opts in; Route/LegacyBrokenVehicleLot refused before context | No defect found; shared-code regression tests warranted |
| Mutable inputs/results | Candidate/Participant own component lists; Control copies receipt lists; result owns event bytes; replay snapshots supplied buffers before trusted-list callbacks | No defect found in supported typed surface |
| Evidence inflation | Tests assert41 original literal events/five final controls;82 side-loop executions/92 cuts separately described as supplemental | Counts distinguished; mirrors do not become new literal traces |

Inspected all five scoped source/test files; C3a prose/schema/oracle and fixture use; canonical plan Task010 row and A/B/C refinement; existing009B certification and identity model; supporting README/design/naming/roadmap diff. No prior review reports, aggregate evidence, dev/worker notes, proposal rationale, execution history or cross-session retrieval read. CCE intentionally unused.

## Plan Review

Canonical plan lines859 and875–883 correctly separate010A actual empty history,010B dormant timed mechanisms,010C cumulative routing, and011 Prepared. Implementation follows010B dependency/scope and does not close parent010, authenticate synthetic positive boundaries, extend Snapshot12, or activate hosting/public actions. Existing contracts/fixtures/oracles unchanged by candidate. Structural no-attack progress means six catalog transitions without attack/resource progress. No migration/rollout required for internal dormant types.

## Author-Claim Reconciliation

Author/checks packets read only after preliminary ledger persisted. Author claims assessed against earlier blind evidence:

| Author claim | Evidence | Status / consequence |
| --- | --- | --- |
| Separately trusted Boundary/Input authorize every event | Replay/Transition and rehashed-forgery tests | Confirmed; imported input is never promoted into trusted history |
| Exact C2 pin and current CP-only profile | ValidateBoundary,009B certification, baseline writer, foreign-request tests | Confirmed |
| Fixed historical clock policy and Command-only retry identity | Oracle transition compared with implementation; deadline and actor tests | Confirmed |
| Owned storage and replay-only state authority | Models, retained buffers, mutation tests, Apply signature | Confirmed |
|41 literal events/five final Controls; intermediate states derived | Fixture counted independently; literal-cut test | Confirmed;46 cuts includes five initial states |
| Supplemental mirrors and five Commonwealth hashes | Independent read-only Python invocation of frozen mirror_steps regenerated all five hashes | Confirmed;82 events/92 cuts include Axis repeats |
| Canonical grammar bridge isolated from inherited behavior | Default preserveArrayOrder=false and explicit C3a entry point inspected | Confirmed by source and21/21 independent shared regression tests |
| Author-reported RED/GREEN, formatting, build/full-suite results | Test/checks packet testimony only | Not independently reproduced; no acceptance credit assigned to historical counts |
|49 descriptor parity | Recursive read-only Python walk from Boundary/Input/Event/Control through frozen oracle SCHEMA and C# descriptor dictionaries | Confirmed:49/49 reachable descriptors match |
| Parent010, Prepared, publication and extended Snapshot12 remain open | Canonical refinement and changed docs | Confirmed; no plan closure inflation |

Author's description of prior Route correction treated as testimony only; current guard and sentry test independently inspected.

## Verification Performed

- Read-only Git identity/status/diff and Python SHA-256 verification: five of five source hashes match.
- `python3 -B docs/specs/verify-combat-selection-steps-v1.py`: exit0;5 literal traces,41 event/control cuts,246 event mutations,164 raw rejections, retries/clock/RBA/FA checks passed.
- Read-only Python schema comparison: all16 direct C3a descriptors match frozen schema; fixture independently counted5 cases/41 events/5 final Controls.
- Read-only Python imports frozen side-projection oracle, invokes `mirror_steps` for five Commonwealth cases, replays inputs, compares final SHA-256 to test constants: exit0,5/5 match.
- `git diff --quiet 762468e5a2744957b5b720905b45d455721d3037 -- src tests README.md tech-design.md naming-overview.md docs`: exit0; reviewed source/docs unchanged.
- Boundary marker absent throughout blind review and Python checks. Waits bounded to45 seconds. Marker existence confirmed before first .NET command.
- Recursive descriptor parity Python check: exit0,49 reachable descriptors match.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatStepsTests' '-bl:/tmp/task010b-review1-focused-{}.binlog'`: exit0;12 passed,0 failed,0 skipped;3.748 seconds.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class 'Cna.Core.Tests.Campaigns.CombatIdentityTests' --filter-class 'Cna.Core.Tests.Campaigns.CombatInheritedStepsTests' '-bl:/tmp/task010b-review1-shared-{}.binlog'`: exit0;21 passed,0 failed,0 skipped;41.359 seconds.
- Both .NET commands used `login:false` and approved local IPC escalation. Unique binlogs verified present:
  - `/tmp/task010b-review1-focused-20260920-092455--20417--MVxqyZ-dotnet-test.binlog` (654008 bytes).
  - `/tmp/task010b-review1-shared-20260920-092508--20440--uYU5Lb-dotnet-test.binlog` (654165 bytes).
- Final SHA-256 recheck: all five frozen sources unchanged. Root concurrently modified only checks/evidence delivery metadata in observed Git status; aggregate evidence not opened. This reviewer wrote only this report, apart from authorized test-generated artifacts/binlogs.
- All started Python and .NET processes completed. No build, full suite or formatting command run by reviewer; independent tests use existing root-built candidate output.

## Open Questions And Residual Risks

No unresolved010B correctness concern identified. Actual positive history, transport authentication, publication/recovery and extended Snapshot12 remain later gates. Focused runs verify existing root-built output, not a separate reviewer rebuild. Oracle lineage is shared with frozen fixtures; exact byte compatibility does not prove actual campaign-history authentication. Runtime inputs must remain independently trusted when future adapters are added. Root full-suite/format/CI and other review instances remain root responsibilities; no aggregate evidence or prior review read.

## Verdict

**Ready** — bounded dormant Task010B candidate only. Root controls acceptance and remaining review budget.

## Recommended Next Actions

Return this review to root for acceptance workflow. Preserve010C,011, actual positive-history, publication and extended Snapshot12 exclusions. No fixes, commits, agents or further review instances initiated.
