# Evidence revisions independent review

Review instance: 6 of 6 (human-approved limit).

## Findings

No open actionable implementation defects found in final reviewed scope.

**Closed P2 — authenticate direct movement-completion derivation.** Initial patch changed `CampaignCombatActualRepeatedMovementCompletion.Derive` to compute its manifest from an authenticated snapshot, but direct callers still supplied raw callbacks. This would throw `Authenticated dependency snapshot required` in existing contract tests. Author accepted finding and added capture at `src/Cna.Core/Campaigns/CampaignCombatActualRepeatedMovementCompletion.cs:121`, before ancestor replay. Both affected `GenuineInheritedClockVariant*` tests passed after rebuild. Reviewed correction matches adjacent derivation boundaries; no architectural pivot or extra review instance required.

Preliminary concerns were sent before reading author explanation: downstream legacy parity coverage, recursive physical revalidation cost/semantics, and pending final generated identities. Legacy R03 tests exercise paid and armed ancestry with exact before/after controls, proofs, events, receipt and retry comparisons. Physical revalidation preserves existing drift rejection. No performance regression was established; repeated copies and callback reads remain a maintenance cost.

## Plan Review

Reviewed dirty working tree on `codex/rc-reserve-crash-recovery`, base and HEAD `3e2d4336b1505d9f308529841361e5413cf70f74`. Neutral bootstrap and canonical revision contract inspected before author packet. No inherited implementation discussion was supplied. Review remained read-only except this report; no builds/tests were started alongside author's active runs.

Final inventory contains 89 changed/new files; every file matched captured SHA256, with no unmanifested dirty paths before adding this report. Inventory: `artifacts/rc-source-pins/review6-final-manifest.json`, SHA256 `9657da3ea847d59ef4efa61e618290c884622ca8637e689d39a6b075b2a4809e`. Delta: `review6-final-delta.json`, SHA256 `2dc8cd22ddc2563f62f4494be760d96dd05821cb3067bd7f14178670e2b5518b`.

Expanded compatibility plan is implemented coherently: code-owned complete revisions, copied registrations and buffers, whole-set authentication, nested revision propagation, runtime-only manifest preimages, strict submitted-proof comparison, and exact retained legacy dependencies. No arbitrary manifest admission or identity translation found. Domain rules and wire versions remain unchanged. README, design and naming docs identify new internal helpers without promoting public support.

Original full-owning-oracle criterion is **not closed**. Final validation handoff explicitly retains full final-state Side/Exercise Occurrence/Exercise Child runs, full native-family Python scripts, full Core and exact-head CI as outstanding gates. Focused final checks are reasonable evidence for this bounded implementation checkpoint; they do not establish complete source-oracle reconciliation or release readiness. Snapshot and Outward remain excluded failures.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status / consequence |
| --- | --- | --- |
| Complete frozen revisions reject unknown/mixed dependencies | Snapshot acquisition, registry, family codecs, snapshot and mixed-set tests | Confirmed; selection is request-local and inherited by nested snapshots. |
| Legacy dependencies preserve identity | ZIP comparison, moved-assault legacy cuts, two R03 vectors and retry assertions, successful legacy test log | Confirmed for retained tested histories; not exhaustive legacy coverage of all 300 R03 rows. |
| Refreshed dependencies bind different provenance | Dependency-aware manifests, proof equality readers, native parity logs | Confirmed; delivery pins excluded from runtime digest preimages. |
| Changes preserve gameplay data and oracle logic | Independent recursive JSON comparison and Python AST comparison | Confirmed for inspected final fixtures: shapes, numeric/boolean values unchanged; changed strings are classified provenance/pins. Oracle ASTs differ only in digest literals. |
| Native/boundary checks pass | Final native summary, underlying logs and follow-up driver | Confirmed as author-executed evidence: 659 selected cases; 561 before isolated correction, 98 after rebuild. |
| Final downstream evidence passes | Final provenance receipts, B2 driver/original functions, B3 original main output | Confirmed within reported scope; B2 selected groups and fixture check, B3 all seven groups. |
| Full gates remain open | Validation handoff, checklist, Snapshot/Outward failed receipts | Confirmed; no full-final-state or RC pass inferred. |

## Verification Performed

Reviewer directly ran read-only Git, Python comparison and hash checks:

- Verified branch/HEAD, initial inventory, final 89-file inventory, final delta and complete dirty-path coverage. `git diff --check` passed.
- All 27 registry refreshed hashes match current files; all legacy counterparts match archive bytes. All 145 archived payloads equal `git show HEAD:<path>` byte-for-byte; ZIP's own manifest is separate metadata.
- Independently compared all 27 changed fixture JSON trees against HEAD, recursively decoding embedded canonical JSON. No key/type/array-count changes or changed numeric/boolean values. Repeated comparison for final B2/B3 artifacts found only documented hash, fingerprint and source-pin fields.
- Schema comparison found digest-map changes only; grammar and limits unchanged. Python AST comparison after normalizing SHA256 literals found no logic changes in either modified oracle.
- Checked 1,387 live path/hash slots during review; only excluded Snapshot runtime-source pins mismatched. Final registered native files remained exact after downstream generation.
- Codebase-memory coverage checked all 33 changed source/test paths, with final recheck of corrected derivation. No recorded gaps; best-effort coverage is not proof of completeness. Source/diff inspection supplied review evidence where class-level graph tracing was uninformative. CCE tools were unavailable.

Author-executed checks inspected rather than rerun:

- Release Core builds and changed-file `dotnet format Sandtable.slnx --verify-no-changes --no-restore --include ...`: exit 0.
- Selected native tests via `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --no-restore -- --filter-method ...`, snapshot class selection, and `just test-suite boundary`: 659 passes, including 300 complete trace/retry cases, two corrected direct-entry cases, 11 snapshot cases and 81 boundary cases. Exact selections and timings are retained in validation handoff and `final-native-summary.json`.
- Final original Cycle and inherited Breakdown scripts: exit 0. Earlier original Selection, Armed, NoAttack, CycleControl, ReserveMovement/completion and AuthorityComposition runs: exit 0.
- Final Side preserved-subset checks and pin checks: exit 0. Final B2 original boundary/representative/forgery/identity groups and fixture check: exit 0, 134 summaries/12 goldens. Final B3 original main: exit 0, seven groups, 12 children/10 parents. Final pin closure changed zero slots.
- Final Snapshot and Outward scripts: exit 1, retained failures. Snapshot first fails `CampaignCurrentEventRuntime.cs`; separate `CampaignElementMovedV3Factory.cs` mismatch also remains. Outward requirement-anchor failures are documented as predating base; source-pin gate also remains open.

## Open Questions And Residual Risks

Legacy parity sampling does not establish every historical external-store scenario; adapters must retain matching dependency bytes. Concurrency tests cover snapshot isolation rather than full concurrent native histories, supported by absence of mutable revision selection state. Recursive physical revalidation adds allocation/I/O cost; no performance claim made.

Complete final-state owning-oracle runs, full Core, all 300 R03 hashes and hosted exact-head CI remain unrun. Ignored local execution artifacts should remain available with this review packet; committed validation handoff preserves selections/results but not every raw log. Public admission, absent roots, persistence migration, Snapshot and Outward closure remain outside approval.

## Verdict

**Ready with non-blocking follow-ups** for bounded private dependency-revision implementation and retained local evidence checkpoint. No open implementation finding remains after final delta check. This verdict does not close full source-oracle reconciliation, authorize merge without exact-head CI, certify public admission, or declare RC readiness.

## Recommended Next Actions

Preserve archive and validation receipts with patch. Complete required final-head checks before merge/release; retain excluded failures as explicit gates. Assess Snapshot and Outward separately under their contracts. Review allowance exhausted at 6/6; no further fresh review instance starts without human authorization.
