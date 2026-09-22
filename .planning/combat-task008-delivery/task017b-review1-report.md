# Task017B independent review

Review instance: 1 of 3.

Reviewed base `201395c3e3faf06735424a8bc70658a2af75f081` to candidate `3c9aad3b71a1eed1f862bfdd4e286e8d8f5ffd67` in clean detached clone `/tmp/sandtable-task017b-review1`. Original repository untouched. Preliminary ledger saved before author explanation; no CCE, other reviewer reports, worker/development review or contract-audit packet used. Root supplied in-flight gate status during inspection; independent conclusions below rely on inspected implementation, canonical requirements and executed checks.

## Findings

No actionable findings.

Inspected new adapter, codec changes, full new tests and fixture inclusion, surrounding native Result2 replay and Release kernel, canonical bridge source authentication/derivation, immutable World/round models, and documentation diff. Source authentication performs native causal replay before checking committed-state pin, command/choice/actor signature and accepted effect semantics. Committed hash includes Base2 hash, native prefix, seals, World and history; Base2 binds replayed selection and canonical boundary. Native fallback traces cannot pass merely by matching settled World. Singleton acting membership and CPA10 follow frozen profile. Full native result is retained, while Release progresses independently through explicit System open/complete.

Raw base/state syntax precedes source replay; suffix event syntax precedes source derivation. Replay checks complete canonical bytes rather than trusting imported state. Defensive array copying and existing immutable nested domain values preserve ownership. New settled-empty base restriction does not weaken existing isolated profile or reinterpret post-transition states as initial bases.

## Plan Review

Task017B implementation matches exact primary manifest: new adapter, narrow codec extension, focused tests, existing fixture copy link, root plan. Remaining changes are listed administrative documentation/evidence. No fixture/schema/oracle changes or generated artifact churn. Canonical native Result2-to-empty-Release requirements are met for bounded 32 contexts; acceptance evidence correctly separates 32 literal bases and 64 literal events from 96 replay-derived native state cuts.

Dependency ordering follows accepted Result2/Release work. Plan retains positive held-I/3h/3i and later-II provenance, Movement execution, cycle control, public admission, Snapshot/publication and parent017 completion as open gates. No new persistence/public deployment surface needs rollout or migration. Existing native history remains unchanged. Compatibility catalogue maintenance is explicit, bounded cost; expansion requires separate contract/scope review. No heavy pivot or plan correction required.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Native upstream replay plus pins authenticates canonical authority | `CampaignCombatResultRelease.Derive`, `CampaignCombatResolution.Replay`, `CombatResolutionContext`, `CombatRoundBase`, Round2 state serialization | Confirmed | No caller-supplied final state grants authority; raw predecessor JSON equivalence is not claimed. |
| 32 bases, 64 events, 96 cuts retain settled result | `ThirtyTwoNativeBasesSixtyFourEventsAndNinetySixReplayCutsPreserveSettledWorld`, existing frozen fixture, successful focused suite | Confirmed | Literal and derived-state proof correctly distinguished. |
| Valid owner retiming accepted; fallback rejected even with equal World | `ReliableSourceRetimingPreservesAuditAndStartsIndependentEmptyReleaseClock`, `AuthenticatedSourceFallbackRejectsEvenWhenSettledWorldMatchesOwnerChoice`; oracle semantic admission | Confirmed | 24 retimed cases, 16 custody faults, 8 opening faults, 8 equal-World custody examples tested in .NET. |
| World/RNG/history preserved and Release clock independent | Derivation/projection, all-cut serialization assertions, null timing/high-water assertions | Confirmed | No stage housekeeping or cycle advancement occurs. |
| Reads reject forged bases/states and suffix; source/output arrays owned | Raw-first and leaf mutation tests; source constructor/accessors; immutable World collections | Confirmed | Canonical readback uses independently derived full state. |
| Behavioral RED and author/root full gates | Author testimony only; not reproduced as historical events | Unverified independently | No historical RED or full-suite claim added by this reviewer; focused and oracle evidence below is independently observed. |
| No broader completion claim | Plan, README, tech-design, naming and roadmap changes | Confirmed | Ready verdict limited to Task017B. |

## Verification Performed

All commands executed in detached clone with `login:false`.

- `git rev-parse HEAD` → exact candidate above; `git status --short` empty before and after checks.
- `git diff --check 201395c3e3faf06735424a8bc70658a2af75f081..HEAD` → exit 0.
- Initial `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatResultReleaseTests' --filter-class '*CombatReserveRelease*'` → restore failed NU1900 because sandbox could not reach NuGet vulnerability service. No passing claim from attempt.
- Escalated `dotnet restore tests/Cna.Core.Tests/Cna.Core.Tests.csproj` → exit 0; `/tmp/task017b-review1-restore.log`.
- Sandboxed no-restore test attempt → MTP local named-pipe/socket bind denied; `/tmp/task017b-review1-tests.log`.
- Escalated `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatResultReleaseTests' --filter-class '*CombatReserveRelease*'` → exit 0, 23 passed, 0 failed/skipped, 24.827s; `/tmp/task017b-review1-tests-escalated.log`. Covers adapter7 + Release base6 + lifecycle10.
- `python3 -B docs/specs/verify-combat-result-cycle-finish-v1.py` → exit 0; `/tmp/task017b-review1-oracle.log`. All printed groups passed: frozen pins/tampering, 32 lineages/160 wrapper cuts/128 retries, authentication31, commands/events/states85, raw84, clock17, prior-time192 with24 changed source audits, ownership2, native frame/scope24, structured order2, semantic fallback24. Wrapper counts are oracle evidence, not implementation claims.

## Open Questions And Residual Risks

No blocking open questions. Scope is private, dormant and synthetic upstream; checks establish bounded adapter correctness, not authentic positive Reserve lineage or playable campaign completion. Repeated full source replay may cost CPU; runtime performance and caching are deliberately outside this acceptance. This reviewer did not execute full solution, format or Boundary suite; those remain root/reviewer3 gates. No external CI verdict inferred.

## Verdict

**Ready** for bounded Task017B implementation and plan, subject to separately required root acceptance gates and remaining sequential independent reviews. Parent017 remains open.

## Recommended Next Actions

Retain frozen source candidate; complete configured remaining review/gate flow. Preserve explicit synthetic-boundary and parent017 limitations in final acceptance records. No code correction requested.
