# Independent Review

Review instance: 2 of 3.

## Preliminary ledger — recorded before author explanation

Scope verified: branch `codex/combat-task008-creation-binding`, HEAD/base `e64bed90d60375f281c65e20c982005a26b57e92`, five tracked documentation edits plus four untracked C# files. No author packet or previous review report read. CCE recall exposed unrelated historical decisions; focused code search exposed no A1c author rationale. CCE contract retrieval missed requested specification; exact local-file fallback used.

- Exact request/Created11 goldens, canonical reconstruction, retained-evidence-before-admission ordering, independently supplied Rules/Setup/Content/config binding, uint64 seed and preamble turn behavior appear sound.
- Potential P2: new request exports trusted `setupId` without envelope `id` grammar validation. Existing Setup7 constructor uses `RequireStableId`, which lacks 128-character bound; an otherwise valid 129-character lowercase setup ID therefore passes context roundtrip and request serialization/readback, though Request schema declares `setupId:id` and envelope contract caps IDs at128. Underlying Setup7 issue predates change, but Request1 newly exposes invalid ID. Same prior issue exists for embedded configuration IDs. Need reconcile scope and author claims before verdict.
- Tests cover campaign ID grammar but not trusted setup/config ID upper bounds. No test execution yet.
- Publication/atomic uniqueness and Snapshot12 are explicitly later gates, not missing A1c functionality.

## Findings

### P2 — Validate trusted Setup/configuration ID bounds before creating envelopes

Location: `src/Cna.Core/Campaigns/CampaignCombatCreationRequest.cs:25–27`, `CampaignCombatCreationContext`.

New context treats predecessor Setup7/configuration roundtrip as sufficient envelope validation. Both predecessor constructors use `ContentContractGuards.RequireStableId`, which lacks a length limit. Consequently, valid lowercase identifiers of129 characters pass context creation. Request1/Created11 export those IDs and Created11 readback accepts them through exact reconstruction. Frozen envelope contract's `id` primitive permits at most128 characters; its independent validator rejects emitted bytes.

Concrete reproduction, run against current compiled assemblies with isolated `/tmp` harness:

1. Obtain ordinary valid context, replace only `SetupId` with `new string('a',129)` via `CampaignSetupSnapshotV7.Create`, preserving remaining inputs; construct new `CampaignCombatCreationContext`.
2. Create request `("probe",0UL,context)`; call `CampaignCombatCreationCut.Decide(null,request,true)` followed by `Decide(createdBytes,request,false)`.
3. Repeat using original Setup and new `CombatDecisionConfiguration(new string('a',129), original.RulesInputHash, original.Windows)`.

Observed:

```text
SetupId: 129-character ID accepted; Created11 7668 bytes; closed-admission retry RequiresPublication=False
ConfigId: 129-character ID accepted; Created11 7579 bytes; closed-admission retry RequiresPublication=False
SetupId: frozen Created11 rejects CMB-ENV-002 /setup/setupId
ConfigId: frozen Created11 rejects CMB-ENV-002 /configuration/configId
```

Harness source `/tmp/a1c-review2-probe/Program.cs`; corresponding retained bytes `/tmp/a1c-review2-probe/SetupId.json` and `ConfigId.json`. No repository implementation changed. Underlying permissive guard predates patch, but newly implemented envelope acceptance exposes it directly. This produces contract-invalid creation evidence that a conforming reader rejects, undermining canonical creation/restore compatibility.

Smallest correction: validate both trusted IDs against envelope primitive bound in new context before accepting it (existing `RequireSourceAtom` supplies required bound/grammar). Add focused accepted128/rejected129 boundary cases for both fields; avoid broad predecessor refactor.

## Plan Review

A1c scope correctly bounded to request/Created11 and pure creation/retry decision. Implementation derives request binding before World, uses supported Rules10 and independently provided Setup/Content/configuration, preserves full uint64 seed and cursor0, retains catalog5 preamble position, and does not alter historical codecs or activate gameplay. Exact goldens and mutation tests support these claims.

Tracked documentation reflects feature-branch implementation with review pending. A2 Snapshot12, receipt/P0, inherited histories, public activation and actual atomic campaign-ID publication remain explicitly open. Publication evidence ownership is named prerequisite before A2/parent acceptance; no persistence proof inferred from pure cut.

One acceptance gap remains: malformed trusted ID bounds above. No heavy pivot required.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Context is validated through strict predecessor readers | Context lines25–27, Setup7 constructor/codec, configuration constructor/codec, concrete129-character probe | Contradicted for envelope ID bounds | P2 above; exact byte comparison cannot make malformed trusted inputs valid |
| Request/Created11 match literal741/7520 bytes | Focused golden tests;48 test run passed | Confirmed | Canonical happy path supported |
| Campaign ID grammar matches frozen bound | SourceAtom guard; campaign ID tests | Confirmed | Gap concerns trusted Setup/config IDs, not campaign ID |
| Changed identity/configuration/Content and noncanonical input reject | Frozen mutation, raw mutation,12 identity fork tests; request reconstruction and Created comparison | Confirmed for tested input domain | Boundary validation must precede reconstruction |
| Exact retained retry precedes admission switch | `CampaignCombatCreationCut.Decide`, closed-admission tests and probe | Confirmed | No new publication for matching retained bytes; malformed trusted context still accepted |
| No historical/public activation or persistence guarantee | New APIs internal; actual diff; documented exclusions | Confirmed | No further A1c scope expansion requested |
| Full solution/build/format passed | Author evidence packet only; independent full rerun intentionally omitted | Unverified independently | Distinct from independent48-test result; no claim of fresh full-suite execution |

## Verification Performed

- Repository scope: branch/base/HEAD/status and full tracked diff; all four untracked C# files read. No other source changes observed.
- `dotnet --version`: `10.0.400`. Inspected global.json, SDK-style test project, Directory.Build.props and Directory.Packages.props: native MTP/xUnit v3.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCreationBindingTests' '-bl:/tmp/a1c-review2-{}.binlog'`: first sandbox attempt failed named-pipe bind permission before tests; approved escalation rerun passed48, failed0, skipped0. Binlog `/tmp/a1c-review2-20260919-210019--40560--P67w4x-dotnet-test.binlog`.
- `python3 docs/specs/verify-combat-authority-envelope-v1.py`: exit0; four goldens,67 mutations,36 raw-byte rejections,nine recovery boundaries,12 forks,two turn boundaries,693 nested type rejections. Contract-only evidence.
- `git diff --check`: exit0.
- Authorized isolated `/tmp` C# reflection probe compiled against existing assemblies; first harness build failed CS5001 because output-directory default exclusion hid source. Explicit compile item corrected harness only. `dotnet build /tmp/a1c-review2-probe/Probe.csproj --no-restore '-bl:/tmp/a1c-review2-probe/build-{}.binlog' --nologo` then `dotnet /tmp/a1c-review2-probe/Probe.dll`: exit0, zero warnings/errors; demonstrated both acceptance gaps above. Successful binlog `/tmp/a1c-review2-probe/build-20260919-210143--40685--PT4c_g.binlog`.
- Python import of retained envelope verifier followed by `parse(raw,'Created')` on each probe artifact: rejected both with exact diagnostic paths above.

## Open Questions And Residual Risks

No open question blocks bounded fix. Runtime publication/recovery remains later work, not validated here. Review covers creation-only contracts, not later authority state. Full integration checks reported by author were not independently rerun.

## Verdict

**Not ready.** One concrete P2 canonical-contract acceptance defect; fix bounded trusted-ID validation before accepting A1c.

## Recommended Next Actions

Lead accepts/disputes finding, applies bounded correction and deterministic128/129 boundary tests, then runs remaining authorized review instance against changed behavior. Reviewer did not edit implementation, stage, commit, spawn, or start further reviews.
