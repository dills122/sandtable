# Author Explanation

## Intent And Success Criteria

CMB-019D3 implements private native Result2 settled control from the accepted019D2 contract. Success requires full packet admission through native019D1,32 default owner/seal contexts plus4 authenticated synthetic descriptors,80 exact control traces, receipt/hash/source identity, owner/forced/fallback/timing policy and every replay cut/original-byte retry. It changes no public campaign service. This is author testimony, not a readiness verdict.

## Plan-To-Implementation Traceability

Five-file native manifest frozen in Task019D3 before semantic RED. Engine/models, closed codec, focused tests, fixture link and canonical plan match that manifest. README/design/naming/roadmap/Movement administrative summaries track the private capability without closing parents017–019. Earlier trust remains synthetic-pre-combat. Explicit unrelated gameplay boundaries stay open.

## Technical Approach And Flow

The sole wire admission entry is `CampaignCombatSettledControlCodec.ReadBase`: strict ordered base parsing, complete packet replay by019D1 `ReadProof`, exact regenerated proof comparison, owned immutable base/proof bytes and pinned clock configuration. No retained fixture or Python execution enters runtime authority. The source object stores bytes plus immutable configuration hash/budget; its proof projection is freshly parsed.

`Replay` starts from the admitted proof's Release terminal and runs at most two accepted inputs, comparing every regenerated event byte. `Apply` reconstructs state from that retained suffix, admits any supplied state cache by complete replay comparison, then processes the new command. Actor/control/cycle binding precedes command+actor duplicate lookup. Duplicate returns the original retained event bytes even after closure. Timers with stale decisions or early available deadlines are no-ops.

Opening assessment copies independently derived commitment receipt AND event hash, witnesses and original descriptor completion receipt. Supported empty witnesses force immediate finish. Material progress plus supported witnesses opens one owner repeat/finish decision. Missing/regressed/unavailable clock or timeout gives System finish; opening clock failure has no invented Timing and permits fallback-step. Equality with deadline rejects available owner input. Original retries never retime retained events.

Repeat establishes successor authority at same-slot Movement, using the pre-repeat prefix and repeat result version, then resets only targetUses and nextCycleProgress. Finish enters same-slot Truck Convoy. Neither executes successor work or charges resources. The entire World/resources/RNG, members/Reserve history, attack history and future duties remain unchanged. Assessment continues to describe the closed old ordinal and is not successor admission. Pending exception expiry follows established policy, although admitted empty Release never creates an exception.

## Changed-Component Walkthrough

- `CampaignCombatSettledControl.cs`: immutable owned source/state/result models; guarded command lifecycle, suffix replay/cache admission, original-byte retries, assessment and successor position. Reuses existing command/input record types, authority identity primitive and catalogue.
- `CampaignCombatSettledControlCodec.cs`:019D2 closed compound grammar, bounds, canonical ASCII UTF8 strings, primitive syntax reused from existing authoritative codecs; complete proof admission and state readback. No runtime schema-file loading.
- `CombatSettledControlTests.cs`: semantic opening test,80 traces/236 cuts/464 retries, preservation,1067 control event/state leaf mutations, every proof leaf and source attacks, explicit fully re-signed Result2/Release attack, owner/version/control/cycle/deadline/timer/clock faults, capacity/canonical/ownership assertions, eight literal arithmetic/receipt contexts and independent policy probes.
- Test project: one frozen fixture link.
- Plan/status documents: frozen manifest, acceptance scope and implementation capability.

## Decisions And Rejected Alternatives

Direct proof projections avoid expanding public Snapshot/domain APIs for a deliberately private fixed profile. The authoritative019D1 reader is reused rather than duplicating full Round2/Result2 lineage replay. Control owns its compound shape inventory; old frozen bytes and runtime code remain unchanged. Historical Result1 control is a policy/reference pattern, not an admission source. JSON projections are local transient values; immutable owned bytes prevent mutable caller caches from supplying authority. A general successor campaign engine was rejected because actual positive-history entry and ordinary repeated Movement are separate gates.

## Invariants And Boundary Conditions

Versioned domains `sctl.`/`scc.` differ from historical `ctl.`/`cc.`. Full base bytes bind control identity. Actor and source cycle remain tied to original admission, even after repeat. A forged source, caller progress/World, altered original locations/exclusions, stale or foreign command, malformed/canonical variant or forged event/state cannot supply authority. Bounds:1MiB, depth32, arrays512, members32 and two accepted control events; stricter upstream limits remain. Overflow/capacity rejects before an accepted event is returned. No remote I/O, clocks or RNG reads exist in this pure control implementation.

## Verification Performed And Results

Semantic RED `/private/tmp/cmb019d3-red.log` failed1 test with the intended missing owner-control decision after valid native proof admission. Initial GREEN passed1. A build analyzer diagnostic CA1869 was corrected by caching serializer options. A strict-canonical test failure exposed pretty fixture input serialization; the test now uses the accepted ordered input grammar. A fixture key failure exposed descriptor probes lacking top-level sourceId; the lookup now derives source identity from their proof. No assertion was skipped or weakened.

Focused eight-test gate passed `/private/tmp/cmb019d3-focused3.log`; explicit arithmetic cases were added afterward and require refreshed final focused/full gates. Frozen control oracle passed10 groups/80 traces `/private/tmp/cmb019d3-oracle-control.log`. All seven unchanged predecessor/policy oracles passed `/private/tmp/cmb019d3-upstream-oracles.log`. The first just-check gate started before the last literal tests and grammar cleanup; it is not final-head evidence. Final gate outcomes are appended only after completion.

## Risks, Tradeoffs, And Maintenance Costs

Local canonical JsonNode projection mirrors the accepted closed wire inventory and entails shape strings in the codec. This is bounded private-profile work, not a generalized public control abstraction. Every mutable tree is locally owned. Admission runs full upstream replay, which is intentionally more expensive than hash-only admission; no runtime throughput claim is made. The source constructor is private. Its Admit factory always performs complete packet replay before creating the immutable source; wire consumers use ReadBase. Input/event bounds are checked before ownership copies.

## Deviations, Deferrals, And Known Gaps

No gameplay or architecture scope deviation. All earlier synthetic provenance and future actual-history/public gates remain explicit. No-progress/no-attack rows are independent policy probes, never fabricated campaign histories. Prior intermittent Runner aggregation cause remains unconfirmed; report any recurrence rather than weakening tests. Coordinator messaging was rejected by automatic approval review because it did not accept delegated/transcript permission as trusted; the coordinator now retrieves status through commentary/final. No reviewer was self-spawned. The independent Ready verdict and coordinator-authorized draft publication are recorded below; merge remains pending.

## Challenge Points For The Reviewer

Verify the complete upstream replay and proof comparison, actor/source binding before duplicate lookup, exact original-byte retry, all80 byte traces, pure preservation/reset semantics, equality-at-deadline and clock loss, source scope/ordinal/prefix attacks, primitive/array/member bounds and documentation honesty. Review all in-scope code/plan independently; these suggestions do not restrict review.

## Final Focused Evidence

Final focused native gate passed16 cases,0 failures/skips in21.377s: `/private/tmp/cmb019d3-final-focused.log`, binlog `/private/tmp/cmb019d3-final-focused.binlog`. Source constructor is private and Admit performs complete proof replay; bounds are checked before suffix copies. Retry coverage was subsequently extended to resubmission with changed/unavailable admission clocks; source remains unchanged, and refreshed focused/full evidence is required.204 old spec files remain byte-identical to907f414, changed-doc local paths and diff whitespace checks pass. Graph generation02:55:07Z reported no recorded issues on predecessor paths; new untracked paths are not tracked in that generation and were inspected from exact source. Serena was activated on the exact worktree; its initial new-worktree language configuration was empty, so no Serena diagnostics claim is made. Final full gate is still pending and must be appended after completion.

## Frozen REVIEW_READY Boundary

Coordinator requested scope/test freeze while the final exact-byte gate completes. No further behavior or coverage additions absent a concrete failure or review finding. Final focused16cases including464changed-clock/original retries passed0fail/skips in22.165s; log `/private/tmp/cmb019d3-final-focused.log`, binlog `/private/tmp/cmb019d3-final-retry-focused.binlog`. Previous pre-retry full2512pass is preserved in `/private/tmp/cmb019d3-pre-retry-final-check.log`. Current full gate `/private/tmp/cmb019d3-final-check.log` is RUNNING, with format/build/Boundary81 passed. Reviewer may inspect the frozen code/plan now but must hold readiness verdict until that gate completes. Host-wide full-suite lease remains held; Runner's short interlude should wait for release. Graph refreshed03:24:45Z,26302nodes/140015edges; source/codec/tests and Campaigns scope have no recorded issues at that refresh. Four added retry assertion lines after that refresh were verified from exact source.

## Review And Final Gate Reconciliation

Independent review Ready, set1/pass1,total1of9,max3per set, no findings; author Accept. Retained `2026-10-05-native-settled-control-review.md`. Final exact-byte just check passed2512tests/Boundary81,0fail/skips,0warnings/errors, full-suite duration8m14.873s. Log SHA256593b01773adf44a8f2cdea7e53cddfc8ef94e12c98872258350bcb6fd79b2d33. Lease released by coordinator and author. No additional gate, behavior/test change or rebase. Subsequent plan changes only reconcile completed evidence; reviewed behavior/test/project hashes remain unchanged. Main868126d contains docs-only PR152; publication keeps the reviewed907f414 ancestry and imports no unrelated work.

## Local Commits And Resolved Publication

Implementation/evidence commit `a95b21166aaef18c754c60cee2ac6bb8eee89a17`; initial publication-blocker metadata commit `ac00bed4d98046243b10cfb04e63c6a0401793fd`. The child push/draft PR command was initially rejected before execution. Safe read-only Keychain checks established authenticated `dills122`, PUBLIC `dills122/sandtable`, and ADMIN permission. Coordinator direct human authorization plus that evidence resolved automatic approval review: coordinator pushed ac00bed and created [draft PR154](https://github.com/dills122/sandtable/pull/154), main target. No further permission is required; the pending child prompt is superseded. PR is attached to this chat.

This follow-up changes only administrative handoff/author publication records. Source/test/project hashes remain identical to independent review; final16focused/2512solution/81Boundary tests and all eight oracle results remain unchanged. No behavior change, rebase, gate rerun or additional review instance. Full-suite lease released. Coordinator verifies exact-final-head CI and owns merge. Latest metadata head is reported in final status and available from the PR.
