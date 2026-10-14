# Independent review — PR187
Review instance: 1 of 9; round1 maximum3; recovery0/2.
Exact head: d9db9e05967ddc7f8c2eca20129aee2fc5c3133d.
Base: 42503c570294e5e4823ff1995bbfdcadc298bf07.
Branch: codex/actual-reaction-combat-entry.

## Neutral preliminary ledger (recorded before author explanation/evidence)
Canonical spec/schema, implementation plan, tests and new native/reference code inspected before author packet. No confirmed actionable findings in first pass. Checks still needed: independent reference/native parity, invariants through all cuts, authentication of canonical prefix versus trusted ledgers, exact typed port correspondence and retry occurrence handling. Reviewed code copies source/current/ledgers before dependency callback; authenticates embedded input equality; strictly replays creation-rooted ReactionClosure; constructs only typed suffix transitions; reads cache claims by exact replay comparison. Ported lifecycle matches frozen bodies in bounded profile; Breakdown exact Command equality subsumes actor/action/creation/cycle authorization. Tests cover both first owners and three closure reasons, five cuts, original suffix retries, forgery, fresh campaign identity and callback byte mutation. No parent TASK017-019/full72 closure inferred. Graph coverage reports new paths not_tracked, so exact checkout source fallback used. Preserved untracked .serena/ only. Preliminary concerns are verification questions, not reported defects.

## Findings
No actionable findings identified in bounded additive contract/native consumer. No heavy pivot required. Source evidence: CampaignCombatActualReactionCombatEntry.Replay, ApplyTrustedSource, Emit, EmitBreakdown; Codec.Parse, Inputs, CompareInput, SerializeControl, Dependencies; both new test classes and canonical verifier.

Full replay authenticates root and all 13 prefix records through existing strict HistoryReplay/direct ReactionClosure. Independently supplied canonical ledgers must match embedded inputs exactly; hashes and cached projections never grant authority. Exact typed Command equality protects actor, action, creation/cycle, version, position and capability; matching prior occurrence accepts only byte-identical authenticated retries, including terminal retries. Prefix count cannot bypass typed predecessor admission. Final candidate uses current CP4 World, and native certification compares all other initial profile facts. Owned byte arrays are captured before callback and returned through copies. Source/records bounded at1MiB, structural depth32 and arrays512 plus13/3/1 family counts; loops bounded. No added dependencies or I/O in engine.

## Plan Review
Implementation conforms to additive eight-primary-file objective, fixture copy and warranted README/design/naming changes. Actual complete diff17 files includes retained plan/review/evidence text; frozen predecessors unchanged. Commit order independently inspected: 38887a9 test scaffold,1508764 behavioral RED,ec8a0a4 contract freeze,d3332db consumer/reference/docs,d9db9e0 review evidence. Historic RED outcome is author evidence; this review did not rebuild historic commit. Consumer reuses original strict replay/models/commands/serializers and ports inaccessible transition bodies within bounded profile. Inspection of frozen MovementLifecycle Authorize/Emit/WithinTwo and Breakdown Emit supports semantic correspondence; extra guards constrain supported path. Owned canonical Control composition is documented plan adjustment, derives only authenticated closure and typed suffix, and does not weaken ordinary-only frozen serializer. Plan's final pending self-review wording is superseded by present self-review artifact; no delivery gap inferred.

Parent TASK017-019, full72/firstrelease, generic composer, public/host/Exercise/Runner activation, downstream selection/paid/finish, laterII/consumed/Engaged/capture/general seeds and retention architecture remain explicitly outside this verdict. Entry18/CP4 cannot satisfy existing fixed downstream versions/CP assumptions; docs state explicit successor needed.

## Author-Claim Reconciliation
| Claim | Evidence | Status | Consequence |
|---|---|---|---|
| Six closure traces through30 cuts/24 events | Reference verifier independently executed; native contract test iterates six cases/five cuts | Confirmed | Both owners and all three direct closure reasons covered |
| Fourteen focused native rows pass | Fresh focused MTP run14/14 | Confirmed | Native build/analyzers and scoped behavior verified |
| Strict independently trusted creation/prefix/lifecycle/Breakdown inputs; caches never authority | Replay, CompareInput, ReadControl/ReadProof; substitution/forgery tests | Confirmed | No authority bypass found |
| CP4/RNG/resources/cycle/progress/full receipts preserved | Certification prerequisite; native byte parity; independent fixture invariant check | Confirmed | Typed World not rewritten; exact World/RNG/cycle/progress/tracks preserved30 cuts |
| No root fixture hash allowlist | FreshAuthenticCampaignIdentity test and strict root replay | Confirmed | Campaign identity binds to trusted request |
| Copies precede dependency callback; exported bytes owned | Replay ordering and callback/export mutation tests | Confirmed | Alias protection verified for supplied evidence bytes |
| Frozen predecessor inventory preserved | Verifier validates12 declared dependency hashes; Git diff only additive/new files and fixture copy/docs | Confirmed | No frozen contract weakening found |
| Product file evidence hashes reflect target | Independently SHA256 checked every13 listed product/document file | Confirmed | d3332db product evidence matches exact review head |
| Historic behavioral RED and scoped format passed | Commit ordering inspected; evidence/self-review describes results | Historic result/format not independently re-executed | Not used as substitute for current native/reference checks |
| Dormant/private status and no downstream/full72 closure | Internal classes; complete diff; canonical docs/exclusions | Confirmed | Readiness only for scoped additive deliverable |

## Verification Performed
All commands executed from /Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable with login:false.
- `git rev-parse HEAD`, branch/status, complete base..head diff/stat/log: exact requested target, only untracked preserved.serena/.
- `python3 docs/specs/verify-combat-actual-reaction-combat-entry-v1.py`: PASS6 traces,30 cuts,24 events,150 rejects; validates12 declared predecessor hashes.
- `git diff --check 42503c570294e5e4823ff1995bbfdcadc298bf07..HEAD`: exit0.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class 'Cna.Core.Tests.Campaigns.CombatActualReactionCombatEntry*' /bl:/private/tmp/i01-reaction-entry-review-{}.binlog`: initial sandbox build-cache write denial (MSB3491), then same authorized scoped command with approved escalation: exit0, total14, succeeded14, failed0, skipped0,7.935s. Log/private/tmp/i01-reaction-entry-tests.log. Binlogs retained only/private/tmp; verified present. No full suite or hosted CI duplicated.
- Read-only Python assertion checked13 evidence file hashes; all30 fixture cuts exact World/RNG/cycle/progress/tracks preservation and cumulative original13 receipts: PASS. Native contract tests independently equate actual native Control/Proof/Source/events to these fixtures, so this assertion connects preserved resource evidence to native output.
- After verification status unchanged: only.serena/ untracked.

## Open Questions And Residual Risks
Graph list_projects/check_index_coverage inspected target; generation2026-10-05 reports new paths not_tracked. Exact source fallback used; no graph absence/exhaustiveness claim. Scoped format historical claim not repeated. HostedCI/full-suite status remains Brain-owned external gate. No stress/near-limit memory or production restart/integration proof claimed. Full replay allocates; bounded private ports duplicate frozen implementations and require contract-pin/version reconciliation for future predecessor changes. Dependency callback/input authentication remains explicit trusted-caller contract; public integration must preserve those boundaries. None warrants scope expansion in this unit.

## Verdict
Ready — for exact-head bounded private actual Reaction closure→Movement lifecycle→Breakdown→certified positive Combat entry contract/native consumer only. This verdict does not close parent tasks, full72, public activation, downstream paid/finish joins or hostedCI gates.

## Recommended Next Actions
Brain should reconcile this report with exact-head hostedCI, retain scope exclusions, and handle later downstream joins through separately certified successors. No corrective product change requested by this review. Review budget remains Brain-owned; no new reviewer or subagent spawned.
