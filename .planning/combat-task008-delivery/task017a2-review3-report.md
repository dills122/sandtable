# Task017A2 independent review

Review instance: 3 of 3.
Candidate: `a4b4f630d1250fd00d61ebae486f18813ffedd01`.
Base: `32a038c322c561f642af2aedafe37165147f4939`.
Clean detached clone: `/tmp/sandtable-a2-review3` (physical path `/private/tmp/sandtable-a2-review3`).

## Findings

No actionable findings. Review covered actual 11-file diff: four implementation/test paths,
root plan, four status/design documents, dispatch and author packet. No fixture/schema churn.
Initial A1 base codec remains unchanged; new lifecycle state does not weaken entry validation.

Blind preliminary ledger written before author explanation. No prior reports/check ledgers/dev
notes, CCE or cross-session memory used. Source/tests/plan inspected independently. No source edits,
commits, further reviewers or workstreams created.

`CampaignCombatReserveRelease.Replay` validates owned event bytes before retained context, then
reexecutes each separately admitted input from independently expected base/request and compares
complete event bytes. `Apply` never accepts caller state as authority. `ReadState` compares complete
canonical bytes with reconstructed state. Identity/actor/command shape precede retry; exact retries
preserve original receipt, while stale timer identities are harmless. Fixed canonical queue,
one configuration budget, equality rejection, clock-loss fallback, conversion/retention ordering,
explicit completion and receipt-linked history agree with frozen specification and oracle.

Focused tests exercise malformed syntax before trusted context, state/event tampering, actor/input
substitution, suffix loss/duplication/reordering, deadline and high-water boundaries, overflow,
32-member/34-event capacity, immutable output ownership, prior offensive history and cumulative CP.
44 isolated traces pin 132 event hashes, 176 state hashes/lengths and two terminal literals; four
historical settled Result1 rows remain excluded from native evidence.

## Plan Review

Task017 refinement/dispatch correctly bound A2 to isolated lifecycle after accepted A1. Exact
implementation manifest honored. Native Result2 adapter017B, positive held-I predecessor and bridge,
later-II/consumed authentic history, World projection, Movement/cycle control, Snapshot/publication
and public admission remain explicit separate gates. Parent017 remains open. README, naming,
tech-design and roadmap describe implementation with acceptance pending; no playable-loop claim.
No architectural pivot or dependency correction needed for this slice. Root-owned full-suite/CI
acceptance remains required by dispatch before acceptance status changes.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Replay-derived authority with separate trusted inputs | Replay, Apply, ReadState and forgery tests | Confirmed | No candidate state or event claims promoted to authority |
| A1 entry checks preserved | Codec diff starts after existing A1 helpers; six A1 tests pass | Confirmed | Converted/pending lifecycle states validated through replay |
| Pinned timing, deterministic fallback, retries and explicit completion | Transition branches, frozen oracle, focused tests | Confirmed | Contract parity established within isolated scope |
| 44/132/176/2 native evidence, four historical rows excluded | FortyFourTracesMatchEveryFrozenEventStateAndTerminalLiteral; passing run | Confirmed | Exact isolated byte evidence supported |
| Owned collections/output bytes and capacity boundaries | Models, Replay copies, CapacityOverflowAndOwnedCollectionsHaveAtomicBoundaries | Confirmed | No exposed mutable state collection or event-byte alias found |
| No World projection or authentic campaign provenance | Private internal kernel and public diff/plan | Confirmed | Parent017 remains gated |
| Earlier RED/fix/format history | Author testimony only; deliberately did not read old checks | Unverified historical claim | No reliance; current source and independent build/tests checked |

## Verification Performed

Commands ran with `login:false` from own clean clone. Approved escalation supplied network/compiler/test
IPC access. No integration build artifacts used. Full solution restored and built in Release from
fresh checkout; `--disable-build-servers` avoided persistent reviewer build servers.

- `dotnet restore Sandtable.slnx` — PASS, all11 projects restored. Log `/tmp/a2-review3-restore-escalated.log`.
- `dotnet build Sandtable.slnx -c Release --no-restore --disable-build-servers` — PASS; zero warnings/errors; 13.90s. Log `/tmp/a2-review3-build.log`.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --no-restore -- --filter-class Cna.Core.Tests.Campaigns.CombatReserveReleaseTests --filter-class Cna.Core.Tests.Campaigns.CombatReserveReleaseBaseTests` — PASS16, failed0, skipped0; 3.810s. Log `/tmp/a2-review3-focused.log`.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --no-restore -- --filter-trait Boundary=UserSpace` — PASS81, failed0, skipped0; 8.600s. Log `/tmp/a2-review3-boundary.log`.
- `python3 docs/specs/verify-combat-reserve-release-v1.py` — PASS13 literal cases/48 side-slot traces;188 cuts,2368 mutations,840 raw rejects,20 timing and27 boundary checks. Historical oracle counts are separate from44-trace native subset. Log `/tmp/a2-review3-oracle.log`.
- `git diff --check 32a038c322c561f642af2aedafe37165147f4939 HEAD` — PASS.
- `git status --short` in clone — clean after checks.

Initial sandboxed restore stalled without output; requested termination of owned PID94932 and
retried with escalation. Only escalated successful restore used as rebuild evidence. All owned
restore/build/test/oracle tool sessions completed. Final process inspection found no clone restore,
build or test command remaining; no global build-server shutdown used.

## Open Questions And Residual Risks

No blocking question. Trust inputs are deliberately assumed independently admitted; matching hashes
and isolated fixture parity do not authenticate campaign lineage or complete membership against World.
Public activation must retain those independent admission requirements. Full suite, format and exact
candidate CI are root-owned gates, not independently claimed here. No durability, provider behavior,
actual positive Reserve lineage or six-turn campaign acceptance established by this review.

## Verdict

**Ready** for bounded Task017A2 isolated lifecycle scope, supported by source/plan review, independent
full solution rebuild and passing focused/boundary checks. This verdict does not close parent017 or
replace remaining root acceptance gates.

## Recommended Next Actions

Root records full-suite/format/exact-CI results and publishes bounded acceptance evidence. Preserve
native adapter/provenance exclusions. Review count exhausted at3of3; no further review instance started.
