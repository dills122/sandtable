# F1 independent review
Review instance: 1 of 3.

## Preliminary ledger — recorded before author/evidence packet
Reviewed frozen five primary files, canonical trigger spec, tests, F execution research and canonical-plan diff. Source SHA-256 manifest verified: five OK. Branch matches bootstrap; working-tree target based on G2 includes four new source/test files, fixture link and supporting documentation.

No actionable defect established. Implementation replays exactly one E1 move; derives return move, opponent adjacency, eligibility, persisted identities, CP4, route continuation and canonical bytes. Typed World writer rederives expected World and checks structural equality. Tests cover both owners, golden artifacts, retries, actors, raw mutations, re-signed history/cache, unsupported predecessors, buffer ownership and typed resource forgery.

Points for final reconciliation: confirm retained focused/integration evidence; verify F3–F6 selected-history dependencies and progressive H wording against canonical authority composition. Coverage limited to closed frozen profile; no runtime/public/persistence claim supported. CCE search inadvertently surfaced first few introductory author lines, so initial pass was not wholly blind; full author packet/evidence not yet read. No prior review report read.

## Findings
No actionable findings. Canonical equality is enforced against newly derived event/state rather than caller hashes; actor authorization precedes retry handling. E1 adjacency rejection and historical position/Move2–3 constructor restrictions remain unchanged. No code or canonical contract edits made during review.

## Plan Review
F1 correctly branches from first E1 move, even though delivery branch includes G2. F2 lifecycle and F3 direct closure consume F1; F4/F5 consume F2 first-participant cut, F6 consumes F5. Canonical authority-composition selected table includes direct closure6, active fallback4 and second-move completion2, so those children are necessary rather than optional expansion. Five-primary-file constraint remains explicit with causal splitting when needed.

Initial H scope is consistent with existing Checkpoint D paragraph: recover states whose handlers exist; later lifecycle tasks add cuts. Plan retains all28 cumulative target, disabled fresh admission, Tasks009–019 ownership and HOST-PUB-001 publication obligation. No dependency inversion or premature parent/runtime closure found.

## Author-Claim Reconciliation
| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Genuine creation through first E1 move required | Replay, Initial, inherited Movement Normal/NONE guard; missing/foreign predecessor tests | Confirmed; no synthetic state admission |
| Actual return Move4 freezes persisted opportunity | Emit adjacency-before-eligibility; CampaignReactionIdentity; both-owner golden/preimage tests | Confirmed; historical/public identity domains not conflated |
| Route/CP/World and history preserved | Emit, ExpectedWorld, WriteWorld; canonical state fingerprints and typed opponent-resource forgery test | Confirmed for frozen profile |
| Strict canonical, retry, cache and buffer boundaries | Replay/Apply/ReadState plus mutation/re-signed/window/cache/buffer tests | Confirmed; authority rebuilt from history |
| Focused15 passed | Inspected /tmp/f1-final.log, 15 succeeded, zero failed/skipped | Confirmed retained result; not reviewer-executed .NET run |
| Build/format complete, full suite running | /tmp/f1-build.log zero warnings/errors; /tmp/f1-format.log empty; /tmp/f1-suite.log partial | Build confirmed; format exit0 author-reported, empty log corroborates no diagnostic; full suite not yet a pass claim |
| No participant/public/restore/publication claim | Source scope and synchronized README/tech-design/naming/roadmap/plan | Confirmed |

## Verification Performed
- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`: branch codex/combat-task008-reaction-trigger, HEAD b9cb26f2c4f2b46b8ac8f369e0e773f5f7e2ed80, explicit dirty/untracked target matches packet.
- `shasum -a 256 -c .planning/combat-task008-delivery/f1-source.sha256`: all five OK, checked before and after review.
- `git diff --check`: exit0.
- Read canonical spec/schema, oracle derivation, fixture assertions, source/tests, predecessor and identity helpers, plan and all supporting doc diffs.
- Retained canonical baseline: 2 traces/2 triggers, 4 cuts, 2 retries, 270 mutations, 60 raw, 30 boundaries, 11 source pins.
- Retained worker focused command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatReactionTriggerTests' --filter-class '*CombatInheritedMovementTests' --no-restore '-bl:/tmp/f1-final-{}.binlog'`: 15/15 pass in inspected log.
- No builds, .NET reruns, delegation, commits or code edits by reviewer.

## Open Questions And Residual Risks
Integration full suite remains lead-owned and was incomplete when inspected. Runtime, participant lifecycle, closure, multiple opportunities, positive vehicles, Snapshot12 composition and durable publication remain outside this review. Internal bounded state/World layout duplicates predecessor writer, so future family expansion must preserve exact bytes and guard absent fields. No current defect follows from that bounded tradeoff.

Initial pass independence qualified: CCE search surfaced introductory author excerpt; full author/evidence packets read only after preliminary ledger. No prior review reports consulted.

## Verdict
Ready for bounded F1 slice. Final integration gate completion and remaining sequential reviews remain lead responsibilities; this verdict does not close parent F/H or authorize public admission.

## Recommended Next Actions
Retain final integration evidence and continue requested review sequence. Advance F2 only after lead accepts completed F1 gate.

Reviewer-executed oracle: `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-reaction-trigger-v1.py` exited0; CMB-IRT PASS with 2traces/2triggers/4cuts/2retries/270mutations/60raw/30boundaries/11pins. Matches retained baseline.
