# H1 independent review

Review instance: 1 of 3.

## Blind preliminary ledger

Recorded before author packet, retained test evidence, or session recall.

- Frozen five-path SHA-256 manifest verified: all five match.
- Canonical plan H execution refinement separates H1 pre-cycle routing from H2/H3 later families and H4 literal root/disabled-admission restore. Code implements only that H1 boundary.
- Source inspection: capture checks count and all event lengths/aggregate before byte copies; owned input/output buffers. Router uses fixed profile offsets only to propose partitions, each subsequently checked by actual strict causal readers. Stage terminal cut goes through Reserve reader and materializes membership at state10. Reserve reader rejects tails after completion.
- Tests inspected first: 42 selected cases across seeds/owners/designation branches, complete frozen pre-cycle identity coverage, predecessor goldens, omitted/duplicate/reordered events, version/type/state/canonical-byte mutations, forged canonical Weather RNG, future Movement tail, capacity and buffer mutation checks.
- Preliminary concerns to verify: selected-case identity test truly covers every pre-cycle fixture identity; whether retained focused execution matches frozen source; predecessor projection collections remain owned; documentation avoids full restore claim.
- No actionable defect found in blind source pass. No runtime tests executed by reviewer; root owns active full-suite run.

## Findings

No actionable findings. Fixed partition sizes do not bypass causal validation: `CampaignCombatHistoryReplay.Replay` validates preamble, Weather and stage through actual existing readers before proceeding; `CampaignCombatReserveOpening.Replay` checks remaining kinds/counts and canonical event authority. State10 dispatches to Reserve even with empty local Reserve history. No suffix is silently dropped.

## Plan Review

Ready for bounded H1 scope. Canonical `docs/design/combat-cycle-implementation-plan.md:819,831,833` and inherited Snapshot12 contract distinguish this router from later H2/H3 families and H4 root serialization/admission-disabled restore. Dependencies and selected-profile limits agree with code. Supporting README, tech-design and naming-overview retain full-restore incompleteness and internal-only evidence boundaries. No new persistence, wire schema, registration or host behavior is introduced. Later cumulative runtime proof and HOST-PUB-001 remain open as intended.

Using fixed 4/1/4 gates is justified by accepted predecessor contracts; actual event readers reject wrong successors, canonical mismatches and causal RNG changes. Repeated predecessor replay is a bounded maintenance/performance cost acknowledged by author, not a demonstrated defect for at most11 accepted H1 events.

## Author-Claim Reconciliation

| Claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Exact independently retained Created11 required | Capture length checks; creation/preamble replay; missing and wrong creation tests | Confirmed | No regeneration fallback |
| Owned transcript and exported copies | Capture holds validated references then copies; Created/Events/CopyRange clone arrays; mutation test | Confirmed | No caller byte-buffer alias survives replay |
| Actual strict causal partitions | Existing Preamble/Weather/Stage/Reserve replay and canonical reserialization; new router | Confirmed | Count selects candidate boundary, not authority |
| State10 materializes membership | Router count9 goes to ReserveOpening with zero local events; H0 and exact predecessor goldens | Confirmed | Correct shared stage/Reserve cut |
| Complete pre-cycle selection | Identity-equality test plus independent Python fixture audit | Confirmed | 256 vectors / 208 distinct histories; cuts0–11 |
| Focused53 pass | Retained `/tmp/h1-green-4.log` reports total53, succeeded53, failed0, skipped0 | Confirmed as retained evidence | Reviewer did not rerun .NET concurrently |
| Canonical re-signed Weather forgery rejects | Test serializes accepted event with wrong RNG; existing Weather reader recomputes RNG and full canonical event | Confirmed | Negative exercises causal authority, not only malformed JSON |
| Full root/disabled admission not delivered | New API returns history + typed family projection only; docs and plan keep H4 open | Confirmed | No inflated initial-H completion claim |

## Verification Performed

- `shasum -a 256 -c .planning/combat-task008-delivery/h1-source.sha256`: five paths OK.
- Read `.git/HEAD` and branch ref: `codex/combat-task008-reaction-lifecycle`, HEAD `4139675fc9b775c5e7c0d355c06756c17ea1f2f9`. No Git command, mutation, build, or .NET test run performed by reviewer.
- Inspected all five primary paths, canonical H plan and contract, supporting documentation, existing strict predecessor replay and projection ownership code.
- Recorded blind ledger before author/evidence/recall; subsequently called Sandtable session recall and reconciled author packet.
- Read `/tmp/h1-green-4.log`:53 passed/0 failed/0 skipped,6s923ms. `/tmp/h1-format.log` empty; scoped-format exit0 retained author evidence, not independently rerun.
- Independent `python3 -B` fixture audit: selected pre-cycle rows by actual cycle state, found256 vectors/208 distinct retained histories. All selected rows satisfy stateVersion=eventCount+1, receipt count=event count, receipt event hashes equal complete ordered event hashes, and non-null Reserve/cycle state exactly from state10. Event-count inventory:0:20,1:20,2:20,3:20,4:40,5:46,6:12,7:12,8:12,9:28,10:18,11:8.

## Open Questions And Residual Risks

- Runtime verification here relies on retained focused execution; initiating task owns full-suite/format/boundary checks. No concurrent runtime process started, per bootstrap.
- Scope is explicit frozen working-tree paths; branch/head and hashes verified. Full Git diff/status independently unqueried under no-Git instruction.
- Trusted transcript commitment and truncated-head authentication remain caller/archive responsibilities; legal shorter prefixes are intentionally accepted. This internal router alone does not establish authenticated persistence.
- H2/H3 later routing and H4 full-root restore remain unimplemented; review gives no approval for those gates.

## Verdict

Ready — H1 implementation and plan align; no actionable blocker found. Integration still requires initiating task's full gate and remaining mandated independent reviews.

## Recommended Next Actions

Retain review evidence, complete lead integration checks and configured review rounds, then update H1 status only. Keep H2/H3/H4 and publication gates open.
