# Independent review — Task019A

Review instance: 3 of 3.

## Preliminary ledger — before author packet

- Scope verified: branch `codex/combat-task019a-first-opening`, HEAD/base `fa5e7236aa1f314ac94a370d43878ac95143ac51`; five primary hashes match. Four new primary files and one narrowly changed completion writer are present, plus scoped documentation/status changes.
- Read canonical Reserve specification/schema, inherited successor schema, scope plan, implementation, tests, predecessor designation/completion paths, documentation diff and retained oracle baseline. Full history admission delegates to existing creation-rooted validators; completion retains original predecessor while deriving terminal position/version/cycle/prefix/receipt from same accepted event.
- Both Apply overloads validate input shape and trusted owner before occurrence lookup. Exact designation retry after completion returns terminal state; changed inputs and cross-family consumed versions reject. No actionable defect found in preliminary pass.
- Tests independently check literal artifact hashes, cycle identity, receipt framing and prefix framing; leaf/raw mutation coverage and coherent forged cycle exercise strict replay rather than cache trust. Pending: reconcile author claims and inspect root verification logs.
- Independence limitation: required CCE search returned short snippets of prior report headings/scope ledgers and author opening summary. No prior reports or full author explanation were opened before this ledger; conclusions above derive from source/spec/test inspection.

## Findings

No actionable findings. Reviewed target remains frozen; all five hashes rechecked after inspection.

`CampaignCombatReserveOpening.Replay` admits only empty, designation, completion or designation+completion suffixes. Each accepted record reaches D1/D2 canonical recomputation from actual Created11 and predecessor records. Untyped kind dispatch does not admit noncanonical payloads: downstream exact byte comparison rejects them. `ReadState` likewise admits only complete replay's exact serialization.

`CampaignCombatReserveOpeningState` derives terminal version/position/cycle from validated completion and computes prefix plus one new receipt from that event's bytes. Existing predecessor retains World, members/history, Weather, RNG, initiative and orders. Completion advances empty histories to version11/ten receipts and designated histories to version12/eleven receipts without modifying D1's precompletion model.

Both `Apply` overloads serialize/validate input and authorize resolved first side before accepted-occurrence lookup. Exact whole-record equality yields original canonical event plus current replayed state. Changed input, cross-family version reuse, and fresh commands after completion reject. This matches canonical oracle `apply`, including designation retry after completion.

## Plan Review

Implementation meets bounded `019A` objective and five-primary-file scope. Only existing completion change exposes unchanged `WriteCycle` internally. No contract, fixture, historical codec, registered action or production persistence change appears in diff.

`019a-scope.md` acceptance criteria map directly to `CombatReserveOpeningTests`: sixteen histories cover four Weather seeds, both actors and empty/I; eight empty histories each check eight artifacts and eight designated histories each check eleven, totaling152. Each case checks every state cut, totaling40. Terminal field invariants, independent receipt/prefix/authority framing, current-state retries, actor rejection, cross-family conflicts, malformed/foreign/missing histories, coherent cycle-cache forgery and caller-buffer ownership are exercised.

Dependency order D1→D2→019A→inherited Movement is appropriate. Plan retains Task008 parent, general Snapshot12, gameplay, later cycles and HOST-PUB-001 gates. README, technical design, naming and roadmap describe private first-opening projection consistently. Execution/plan rows remain in progress pending lead closeout; this is expected review-time status, not false completion. No migration/rollback mechanism is needed for dormant internal-only projection; production publication/recovery stays with named later owner.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Full history establishes authority; no cache/evidence admission API | Opening `Replay`/`ReadState`, completion `DeriveBase`/`ReadEvent`, designation `Replay`, canonical oracle `initial`/`replay` | Confirmed | No shortcut bypass found |
| Same completion event atomically establishes terminal fields and receipt | Opening state constructor, codec, completion event model, terminal fixture assertions | Confirmed | Version/position/cycle/prefix/receipt remain coherent |
| Exact retry returns original event and current terminal state | Both `Apply` overloads; every-cut theory and conflict theory | Confirmed | Designation retry retains terminal version12 |
| All16 traces/40cuts/152 artifacts covered | Fixture enumeration and `CheckGolden` call structure; focused log | Confirmed | Runtime parity extends through terminal state |
| World/RNG/history preserved and output buffers cannot mutate state | Predecessor-backed serializer; terminal preservation assertions; buffer test | Confirmed | No resource reset or mutable retained event bytes |
| Focused67/full2012 and clean build | `/tmp/019a-final.log`, `/tmp/019a-suite.log`, `/tmp/019a-build.log` | Confirmed from retained logs | Strong scoped and integration evidence; not reexecuted by this reviewer |
| Repository format passed | Evidence packet records exit0; `/tmp/019a-format.log` is empty | Recorded exit status not independently reproduced | No formatting concern in inspected diff; avoid treating empty output alone as proof |
| No general restore/publication/gameplay claim | Canonical plan, roadmap, implementation boundaries | Confirmed | Future gates remain open |

## Verification Performed

Reviewer executed `git status --short`, `git rev-parse HEAD`, `git branch --show-current`, `git diff --check`, and `shasum -a 256 -c .planning/combat-task008-delivery/019a-source.sha256` twice. Branch/base match bootstrap; diff check exits0; all five hash checks pass both times. Inspected all primary changes, relevant predecessor implementation, canonical specs/schemas/oracle logic and changed documentation.

Retained execution evidence inspected, not rerun:

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatReserveOpeningTests' --filter-class '*CombatReserveCompletionTests' --filter-class '*CombatReserveDesignationTests' '-bl:/tmp/019a-final-{}.binlog'`:67 passed,0 failed/skipped,6s930ms.
- `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/019a-build-{}.binlog'`: succeeded,0 warnings/errors.
- `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/019a-suite-{}.binlog'`:2012 passed,0 failed/skipped,3m27s821ms.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: exit0 recorded by lead; empty retained log inspected.
- `/tmp/d-reserve-baseline.log`: canonical oracle reports16 traces,40 cuts,2342 leaf mutations,733 raw rejections,1578 boundary/retry checks,4 parity cases and14 source pins. Source inspection found no reason to repeat unchanged oracle.

No build, test rerun, code edit, commit or delegation performed. Only this report written.

## Open Questions And Residual Risks

No unresolved blocker. Internal constructors remain callable by trusted assembly code; subsequent Movement adapter must use full-history admission rather than trust fabricated wrappers. Authenticated ingress, published-head comparison, durable commit ambiguity/recovery, arbitrary campaign capacity, later slots and general snapshots are not certified here. Existing closed-profile bounded replay repeats predecessor work; no broader performance claim made.

## Verdict

**Ready** for bounded Task019A. Evidence supports implementation and plan; this does not close parent Task008 or authorize production activation.

## Recommended Next Actions

Lead records acceptance and updates review-time status during closeout, then proceeds through existing inherited Movement dependency. Review instance3 of3 complete; no further review or conditional experiment initiated. No blocker requires conditional experiment/final review.
