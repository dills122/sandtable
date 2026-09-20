# G2 independent review

Review instance: 2 of 3

## Preliminary ledger — before author packet

Reviewed canonical Breakdown completion specification, schema/oracle entry points, G2 scope, implementation/test source, inherited lifecycle replay, and tracked documentation diff. Branch codex/combat-task008-breakdown-completion, HEAD/base e177a4be23ea9205e02fca1c123a556ad07d31e2; all five frozen source hashes pass. Four primary source/test files untracked; csproj and documentation tracked modifications.

No concrete correctness defect identified. Replay reconstructs complete lifecycle and recomputes event bytes; cache cannot authorize state. Authentication/identity checks precede retry. Completion preserves actual proof and inherited state while adding one receipt. Event sources derive from predecessor; successor remains first-acting-side with null activeSide. Eight vector rows cover both sides and movement/cohesion/exclusion boundaries.

Follow-up checks: reconcile independent oracle evidence and full-suite outcome; compare copied state writer with predecessor; inspect plan exclusions and author claims. Documentation still marks G2 in progress, appropriate before delivery closeout.

Independence note: CCE discovery incidentally exposed first lines of g2-evidence.md (base/scope/canonical baseline heading), but no author rationale or prior review report read. CCE expand_chunk returned Chunk not found for discovered paths; direct source reads used for complete review. Cross-session recall intentionally omitted to avoid importing prior reviewer conclusions.

## Findings

No actionable findings. No blockers or heavy pivot required.

Evidence: `CampaignCombatBreakdownCompletion.Replay` requires exactly three genuine lifecycle records and at most one suffix event, reconstructs predecessor through existing lifecycle reader, checks idle/null-interrupt/proof boundary, and re-emits event for byte equality. `Apply` authorizes before duplicate return; `Emit` requires exact history-derived command and capacity. `ReadState` accepts only equality with independently replayed state. Codec derives sources from prior Breakdown position and keeps Reserve, Movement and new Breakdown identities separate. New types remain internal; no public dispatcher, proto or persistence activation changed.

`FrozenSystemCompletionEntersCombatAndPreservesActualProof` checks canonical predecessor digest, command, event and both state cuts for both owners at 1/5/6/7 moves; verifies version, prefix, receipt hashes, ownership, actual exclusions, CP/cohesion, full unchanged-field equality and legacy-reader rejection. Remaining four tests exercise identity/retry rejection, missing/reordered/foreign lifecycle histories, raw/scalar/cache forgery rejection and buffer independence. Re-signed sources and Movement receipt are tested explicitly. Full event recomputation covers other coherent field alterations even where C# tests use primitive mutation rather than re-signing every leaf.

## Plan Review

G2 fits dependency graph: accepted E2/G1 produces actual Movement-end proof; this child appends System Breakdown completion into first Combat Position Determination. Five-primary-file limit met (three implementation files, test file, fixture project link). Supporting map/design/naming/roadmap updates retain dormant gameplay and unfinished parent gates. No canonical fixture or contract edits. F Reaction, positive vehicles, later cycles, general Snapshot12/restore, public activation, Runner and HOST-PUB-001 remain explicitly deferred; reaching Combat entry does not close them.

Copied state layout is bounded maintenance cost rather than present defect: independently normalized both serializers and proved exact equality after replacing lifecycle-wrapper access and removing new nullable receipt field. Frozen parity vectors guard future drift. No rollout/migration required for internal dormant family. In-progress delivery labels should be advanced only by lead when review/delivery completes.

## Author-Claim Reconciliation

| Claim | Evidence inspected | Status / consequence |
| --- | --- | --- |
| Full actual creation-to-lifecycle replay required | Replay delegation, exact count, existing lifecycle Emit, partial/foreign-history tests | Confirmed; no proof/cache shortcut |
| System/action/creation/cycle authorized before retry | Authorize call before Completion branch; exact record equality | Confirmed; changed retries reject |
| Prior Breakdown sources and catalog Combat successor | SerializeEvent, Emit, frozen event digests, explicit source/position assertions | Confirmed |
| All inherited state preserved; no new material progress | Retained Lifecycle object; normalized serializer comparison; ImmutableFields check and vector digests | Confirmed |
| Eight traces, eight events, sixteen cuts, forty artifacts | Exact fixture matrix and five CheckGolden calls per theory row | Confirmed |
| Focused 24 and independent canonical baseline pass | /tmp/g2-final.log and /tmp/g2-breakdown-baseline.log | Confirmed as retained execution evidence, not reviewer reruns |
| Integration initially pending | /tmp/g2-suite.log now 2,044 pass; root reports build/format pass | Superseded by successful integration evidence |
| No public Combat/Snapshot12/durable publication claim | Internal classes, no public registration diff, legacy-reader negative assertions, retained plan gates | Confirmed |

## Verification Performed

- Executed `git status --short`, `git branch --show-current`, `git rev-parse HEAD`; branch/base verified and dirty/untracked scope identified.
- Executed `shasum -a 256 -c .planning/combat-task008-delivery/g2-source.sha256` twice: all five files OK.
- Executed `git diff --check`: exit 0.
- Executed read-only Python assertions: complete state-writer equality after lifecycle wrapper/new-field normalization; exactly eight unique owner/move fixture cases. Exit 0.
- Inspected canonical spec/schema and Python oracle, primary C# files/tests, predecessor lifecycle and state writer, plan and documentation diff.
- Inspected /tmp/g2-final.log: 24 passed, 0 failed/skipped.
- Inspected /tmp/g2-breakdown-baseline.log: 8 traces/events, 16 cuts, 8 retries, 426 mutations, 92 raw, 298 boundaries, 10 source pins passed.
- Inspected /tmp/g2-suite.log: 2,044 passed, 0 failed/skipped, 3m29s472ms. Root separately confirmed build zero warnings/errors and format exit 0. Reviewer did not rerun builds/tests or treat reported checks as personally executed.

## Open Questions And Residual Risks

Trusted System actor wrapper remains caller-authenticated at future adapter boundary, as specified; this internal API does not authenticate network identities. Frozen supported profile does not prove positive vehicle/Reaction/general-cycle behavior. Retained baseline proves oracle boundary corpus; C# tests cover selected re-signed mutations plus broad raw/leaf rejection, not an identical exhaustive mutation corpus. Bounded copied codec requires attention if predecessor fields evolve. No remaining question blocks this scope.

## Verdict

**Ready** for bounded G2 child. Implementation and plan consistent; no actionable defects found.

## Recommended Next Actions

Lead completes remaining authorized review/delivery bookkeeping, preserves parent gates, and updates delivery labels with final evidence. No code changes requested. Review instance 2 of 3 complete; no reviewer-created follow-on review or workstream.
