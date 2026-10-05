# Handoff: native actual positive entry

Date:2026-10-05 America/Toronto. Task019E1/N3; administrative closeout after independent pass2 Ready with non-blocking follow-ups.

## Objective and boundary

Private Core adapter for accepted019E0 exact seed1/Normal/ordinary NONE creation-rooted openings,
both owners. Full Request/Created11 plus4 preamble/1 Weather/4 stage/1 Reserve events; owner idle
Movement11→12, System empty Breakdown12→13; actual original-unit Movement-end proof and supported
candidate only at13 before selection. Preserve World/resources/RNG/Weather/order/cycle and original
synthetic content-origin labels. No C3a/Round2/Result2 bridge, later-II/consumed/repeat, public/Snapshot/
transport/host/Runner or parent017–019 closure. Five primary manifest unchanged.

## Canonical sources

- `docs/specs/combat-positive-entry-v1.md` and matching schema/fixture/oracle.
- `docs/design/combat-cycle-implementation-plan.md` task019E1.
- `docs/research/combat-positive-entry-feasibility.md`.
- `docs/work/reviews/2026-10-05-positive-entry-review.md` and019E0 contract handoff.
- Coordinator ledger `/Users/dsteele/repos/sandtable/.planning/combat-overnight/session-plan.md`.

## Current repository state

Worktree: `/Users/dsteele/.codex/worktrees/native-positive-entry/sandtable`.
Branch: `codex/native-positive-entry`.
Base: `84f1fac861cef7c8ffaf8f36dd75b1b7f4dedc83`, merged PR155.
Initial implementation checkpoint: `a1c8ef225b7386344a00636262e6c68252f1be06`.
That checkpoint predates the narrowly corrected retained-event receipt/authorization ordering.
Final corrected implementation checkpoint: `b0a7308f6b440507436f152d8bea4e325100f833`.
The administrative review head is supplied by final coordinator dispatch;
verify Git HEAD/branch and hash inventory before review. No PR/push/merge by this author.
Only local generated `.serena/` remains outside retained scope after final commit.

## Completed work and evidence

Native engine/models and closed codec authenticate full source, byte-match both actual events, bind
all causal receipts/prefixes and full27-field entry/proof, and certify supported initial facts at13.
All source/state/proof/event outputs are owned copies. Original-byte retry after terminal13 returns
current state and original event. Clock/deadline/cache-only/foreign/stale/re-signed/malformed histories
reject. Independent native construction reproduces both exact admitted original source packets.

Final focused corrected gate:19tests passed,0fail/skips. All six cut proofs, source/commands/events/
full state bytes match frozen literal fixture. Tests mutate source/input/event/proof leaves; include
re-signed events, exact raw/signed event error-code vectors independently computed from unchanged
oracle (56+51vectors per owner), counts/canonical/control escapes/caches/actors/scope/ownership.
Old route/public/Snapshot admission stays closed. No production fixture/schema/Python authority I/O.

Semantic RED evidence retained:

- `/private/tmp/n3-gates/red.log`: both owners expected13, actual11 after successful opening.
- `error-red.log`: suffix tag expected004, actual003.
- `control-red.log`: canonical lowercase ASCII control escapes rejected008 before correction.
- `receipt-order-red.log`: retained receipt expected004, actual006 before correction.

`matrix.log` and `matrix-fixed.log` retain early test-source compile/analyzer errors; these are not
semantic RED. Corrected focused GREEN: `/private/tmp/n3-gates/corrected-focused.log`.
Earlier18focused/2530full gate at initial checkpoint remains historical evidence, not final gate.

Final corrected actual `just check` PASS:2531solution tests, Boundary81,0fail/skips,0build
warnings/errors and clean restore/format. Core runtime8m05s, Runner2m19s.
`/private/tmp/n3-gates/corrected-just-check.log` and unique corrected-gate binlogs retain evidence.
The source/project hashes frozen before this gate match final corrected bytes.

Oracle commands (all unchanged): new positive entry and seven predecessors PASS; original Breakdown
FAILED. Logs in `/private/tmp/n3-gates/<oracle-name>.log`, status in `oracles-status.json`.

- `positive-entry-v1`:2owners/4events/6literal proofs/6cuts/6retries/3966rejection probes PASS.
- `reserve-designation-v1`, `stage-entry-v1`, `inherited-movement-lifecycle-v1`,
  `inherited-selection-v1`, `selection-steps-v1`, `sealed-round-v2`, `result-settlement-v2`:PASS.
- `inherited-breakdown-completion-v1`:FAILED on unchanged Cna1979LandSequence.cs source pin.
- `breakdown-supplement.log`:separate8retained traces/goldens,16cuts/8retries/426mutations/
  92raw/298boundaries PASS. Original command remains FAILED; no pin edit/skip/monkeypatch.

Protection audit: all208 specs and686 existing source/test files byte-identical to exact base.
`/private/tmp/n3-gates/protected.log` and final manifest/hash validation retain scope evidence.
Fresh source/test graph coverage at06:19:01Z reports metadata_match/no_recorded_issue;
`sandtable-native-positive-entry`, fast mode16474nodes/112644edges. Docs excluded and read directly;
initial untracked new files used source fallback. This is best-effort, not completeness certification.
Serena activated exact worktree and manual read; CCE tools unavailable. Generated tooling excluded.

## Decisions and rationale

New source reader preserves strict old route/held-I readers and exact two-source pin admission.
Reuse native event/state/input serializers, receipt domains and Chronicle framing; implement only
two bounded private transitions without widening predecessor kernels. Facts certification follows
full source authentication. Proof is complete replay evidence, never authoritative supplied state.
Canonical carrier escapes and retained receipt-before-authorization order follow executable oracle.
The last ordering defect was discovered after initial freeze and corrected with semantic RED/GREEN;
coordinator granted one refreshed sole full-suite lease. No scope/review-budget expansion occurred.

## Blockers and limitations

Independent review not dispatched by author; no Ready verdict claimed. Coordinator owns fresh
GPT-6.1medium review, publication, exact-head CI and merge. Review budget maximum3sets×3passes,
max2high recovery spikes; no reset, heavy pivot requires user gate. Pass1 consumed (set1/pass1,total1of9) and returned Not ready; next is set1/pass2,total2of9.
Original Breakdown pin failure is accepted bounded pre-existing limitation from N2 and separately
coordinator-owned follow-up; unchanged dependency evidence and supplementary checks do not repair it.
Downstream actual positive selection/round/result compatibility remains separately required.

## Immediate next actions

1. Coordinator verifies final head/hash/scope, then creates fresh reviewer from neutral bootstrap.
2. Reviewer records preliminary findings before separate author explanation; reviews code and plan.
3. Author reconciles findings; material corrections require counted fresh pass and affected gates.
4. Only independent Ready plus exact-head required CI permits coordinator publication/merge.

## Verification commands

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatPositiveEntryTests' '/bl:/private/tmp/n3-review-{}.binlog'
python3 -B docs/specs/verify-combat-positive-entry-v1.py
git diff --check
```

Actual full gate used `PATH=/private/tmp/n3-dotnet:$PATH just check`. Temporary dotnet wrapper calls
`/opt/homebrew/bin/dotnet` unchanged except appending unique `/bl:/private/tmp/n3-gates/corrected-gate-{}.binlog`
to every restore/build/test MSBuild invocation. `just check` executes restore, format verification,
build, Boundary81 and solution suite in repository order. SDK10.0.400, native MTP/xUnit v3.
Do not repeat unchanged heavy gates without new code/failure/concern; coordinator serializes lease.

## Delivery metadata

PR title: `Add private native creation-rooted positive Combat entry`.
PR body: `/private/tmp/n3-pr-body.md` (prepared after final gate).
Neutral bootstrap: `docs/work/reviews/2026-10-05-native-positive-entry-bootstrap.md`.
Separate author explanation: `docs/work/reviews/2026-10-05-native-positive-entry-author.md`.
Historical pass1 source/project and gate log/binlog hashes follow; final pass2 inventory is appended below. Final corrected HEAD is coordinator dispatch
and Git truth; administrative follow-up contains no behavior beyond its named corrected checkpoint.

```text
{
  "src/Cna.Core/Campaigns/CampaignCombatPositiveEntry.cs": "6637c77c1f9986bb7f9912ba0f3a028f997a2a00704e96e14f12cc88a9787106",
  "src/Cna.Core/Campaigns/CampaignCombatPositiveEntryCodec.cs": "f0ad5791684fdb46a202a600152132c129ae6ac4ae61a541df6ae2c01fcc375f",
  "tests/Cna.Core.Tests/Campaigns/CombatPositiveEntryTests.cs": "e1af9a08679a443ba64bc1cbd6a8050e11731cd7334fb000b018054c950db8b6",
  "tests/Cna.Core.Tests/Cna.Core.Tests.csproj": "d1641cea314043ee63d3fb3bec3b4ffdfd69c49b713fbf5ed54756ee011f6f88"
}
e0b7333b65cf6561069e490908e24cf9577ebed94fdc48db94d49a4fd44d9472  /private/tmp/n3-gates/corrected-focused-20261005-061603--36057--REQ_fu.binlog
374b96ff8f52b65d45d4108eeed40ca508f605907be1b9bd5ba1704ce140dbb5  /private/tmp/n3-gates/corrected-focused-20261005-061605--36057--nMESY9-dotnet-test.binlog
6dbcbe04fc33cd494dc6de8bc5d8cdb54b40114c08f66288ff9dedd479ec6e7a  /private/tmp/n3-gates/corrected-focused.log
e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855  /private/tmp/n3-gates/corrected-format.log
aca26dfc28961f26259201617066370f17f4da0b57701294302e8871c24b1d7c  /private/tmp/n3-gates/corrected-gate-20261005-061714--38215--3foout.binlog
7e19ac386a40b9d720bb5dfe88dd1c7ced528b40c12b4714cad42bf2cfa98d94  /private/tmp/n3-gates/corrected-gate-20261005-061738--38828--RS4kgS.binlog
7a0f412fa76e536b6b9b5cd0189fa400ff89e0b2c27100deec16e60a4d439b65  /private/tmp/n3-gates/corrected-gate-20261005-061740--38853--pn_NTf-dotnet-test.binlog
00e4e041d67d908264a9324a71a48a90ad509f30a2e57e55cc28bb5f173dd784  /private/tmp/n3-gates/corrected-gate-20261005-061748--39056--yPah0i-dotnet-test.binlog
1ca58937cda8e4c21751622950da11632f21f06b7da51d6acfcb7601f9ef7be6  /private/tmp/n3-gates/corrected-just-check.log
```

## Pass1 correction and pass2 handoff

Accepted the independent P2 finding. `OrderedLocations` now preserves traversal order and revisits
while identity arrays remain sorted. Exact both-owner RED expected006/actual008 is retained in
`/private/tmp/n3-gates/route-red.log`; regression covers three ordered/revisited routes, authority
comparison006, exact canonical route bytes and sorted identity arrays. Focused21 GREEN is in
`route-focused.log`. Final actual `just check` PASS:2533 solution tests, Boundary81,0fail/skips,0build warnings/errors; restore/build/format clean. Core8m09s943ms, solution8m10s116ms. Evidence `/private/tmp/n3-gates/route-just-check.log` and unique route-gate binlogs. All four frozen source/project hashes match final bytes.

Prior19/2531 evidence and hashes above are historical, superseded by this correction's gate.
Final source/project freeze: `/private/tmp/n3-gates/route-freeze-hashes.json`. Specs/oracles and
all protected dependencies remain byte-equivalent, so unchanged oracle outcomes are reused with
original Breakdown FAILED distinguished from the passing supplement. Graph generation06:19:01Z
predates this correction; changed source/test lines were read directly and reviewers must check
freshness or use source fallback. The sole full-suite lease is released after gate completion.

## Full independent report: N3 set1/pass1

The following report is retained with wording unchanged and trailing whitespace normalized; its Not ready verdict applies to prior HEAD67712bf.

## Findings

**P2 — Preserve ordered routes when canonicalizing proof projections.**
[CampaignCombatPositiveEntryCodec.cs:103](../../../src/Cna.Core/Campaigns/CampaignCombatPositiveEntryCodec.cs) sorts every array whose child type is `id`, including `OrderedLocations`. The inherited contract explicitly preserves route order and revisits.

Reproduced against the pinned, already-built assemblies:

- Start with an authentic terminal proof.
- Add an `InheritedTrack` using the authentic attacker’s UnitKey and route `["z","a","z"]`.
- The unchanged oracle accepts its canonical syntax, then rejects full proof comparison with **`CMB-PEN-006`**.
- Native `ReadProof` rejects it with **`CMB-PEN-008`**, because canonicalization sorts the route.

Accepted idle histories retain empty tracks, so this does not admit forged authority or change successful completion bytes. It does violate required canonical/error parity for adversarial proof readback.

Smallest correction: preserve `OrderedLocations` sequence while continuing to sort `id[]` identity arrays. Add a regression proving ordered routes retain revisits and this forged proof reaches error006.

No other actionable findings identified.

## Plan Review

Review instance: **N3 set1/pass1, total1 of9; maximum3 per set**.

Reviewed branch `codex/native-positive-entry`, base `84f1fac861cef7c8ffaf8f36dd75b1b7f4dedc83`, HEAD `67712bfff0ca2e73d1ac26dc12979fa0b057592f`. HEAD remained stable; only `.serena/` was untracked.

Independently reconstructed the exact 12-path diff:

- `src/Cna.Core/Campaigns/CampaignCombatPositiveEntry.cs`
- `src/Cna.Core/Campaigns/CampaignCombatPositiveEntryCodec.cs`
- `tests/Cna.Core.Tests/Campaigns/CombatPositiveEntryTests.cs`
- `tests/Cna.Core.Tests/Cna.Core.Tests.csproj`
- `docs/design/combat-cycle-implementation-plan.md`
- `README.md`
- `tech-design.md`
- `naming-overview.md`
- `docs/roadmap/pre-alpha-roadmap.md`
- `docs/work/reviews/2026-10-05-native-positive-entry-bootstrap.md`
- `docs/work/reviews/2026-10-05-native-positive-entry-author.md`
- `docs/work/handoffs/2026-10-05-native-positive-entry.md`

The implementation follows task019E1’s bounded architecture: full original opening replay and exact source pins precede two causal completions. Movement completion derives original-unit locations, exclusions, receipt and Movement-end proof. Breakdown completion advances to Position Determination; candidate certification receives actual Request, Created11, replayed World, cycle, owner and Weather.

Scope exclusions remain intact. No heavy pivot or decision gate is needed. The identified correction fits the existing manifest.

## Author-Claim Reconciliation

The preliminary ledger was recorded before reading the separate author explanation: no actionable defect then identified, with verification and canonical parity still pending.

| Author claim | Evidence inspected | Status / consequence |
|---|---|---|
| Full creation-rooted source authentication | `ReplayAuthority`, predecessor replay, exact opening pins, independent construction tests | Confirmed |
| Exact successful bytes, cuts and retries | Literal proof/state/event/input comparisons; focused19 pass | Confirmed |
| Exact adversarial canonical/error parity | Codec, oracle, scratch ordered-route probe | **Contradicted for OrderedLocations** |
| Owned immutable projections | Source/state/event copies, cloned JSON projections, ownership tests | Confirmed |
| Actual Movement-end proof and candidate only at13 | `Emit`, `Replay`, existing certification implementation | Confirmed |
| Corrected full gate passes2531/Boundary81 | Retained logs and matching hashes | Confirmed as retained evidence |
| Original Breakdown oracle remains FAILED | Status inventory and traceback; unchanged protected files | Confirmed; no new N3 source-pin impact |

## Verification Performed

Executed from the exact worktree:

```sh
python3 -B docs/specs/verify-combat-positive-entry-v1.py
```

**PASS:** two actual owners, four completion events, six literal proofs, six cuts/retries and 3,966 rejection probes.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build \
  --filter-class '*CombatPositiveEntryTests' \
  '/bl:/private/tmp/n3-review-{}.binlog'
```

**PASS:19 tests, zero failures/skips.** Initial sandbox execution terminated on MTP named-pipe permission; the authorized retry passed.

Reviewer evidence:

- Focused log (historical local artifact: `/private/tmp/n3-review-focused.log`), SHA256 `3dab43be7c21018daba44ac351ad28c85e862a19bff14b441b46a91c85f4de4d`
- Focused binlog (historical local artifact: `/private/tmp/n3-review-20261005-063915--64427--SjV1uz-dotnet-test.binlog`), SHA256 `9418823d6418037d554e55cfa81b63c2cfa646009af3940c953c278e83929849`

Additional checks:

- `git diff --check <base> HEAD`: PASS.
- All **208 spec files** and **686 protected source/test files** match base bytes.
- Source/test bytes match corrected checkpoint `b0a7308`.
- All four source/project hashes and nine retained log/binlog hashes match the handoff.
- Retained corrected `just check` records **2531 solution tests, Boundary81, zero failures/skips and zero build warnings/errors**, with clean restore/format.
- Ordered-route scratch probe independently reproduced native008 versus oracle006. Probe (historical local artifact: `/private/tmp/n3-review-probe/probe.cs`), proof input (historical local artifact: `/private/tmp/n3-review-probe/proof.json`). Initial file-mode probe lacked its context fixture; the isolated project invocation supplied it and succeeded.

The full suite was not rerun. Seven predecessor passes and the separate eight-trace Breakdown supplement were inspected as retained evidence. The original Breakdown command remains **FAILED** on its pre-existing sequence-source pin; supplementary semantics do not turn it green.

Serena activated the exact worktree. Codebase-memory generation `2026-10-05T06:19:01Z` matched HEAD; relevant searches were fully paginated and cited paths reported metadata-match/no-recorded-issue. Six unrelated partial source/test ranges were read directly. Graph results remain best-effort evidence.

## Open Questions And Residual Risks

The frozen transitive grammar is duplicated in the native codec; successful goldens and leaf mutations do not exhaust all shape-only canonical cases. The ordered-route defect illustrates that limit.

Two-source admission, repeated bounded replay, and downstream selection/result incompatibility are intentional constraints. No current C3a/Result2 consumption, public activation, or parent017–019 closure is established.

## Verdict

**Not ready**, due to the reproducible P2 canonical/error-parity defect. All other inspected implementation and plan evidence supports the bounded adapter.

## Recommended Next Actions

Correct `OrderedLocations` ordering and add the focused regression. Reconcile the finding through the coordinator, then run affected verification and any required counted review. Publication, final CI and merge remain coordinator-owned.

No repository edits, commits, subreviewers or additional review instances were created.

Unchanged executable oracle additionally confirmed all six new both-owner ordered/revisited route
probes: canonical syntax accepted, full proof comparison rejected006. Evidence:
`/private/tmp/n3-gates/route-oracle-probes.log`. This is supplementary targeted evidence; retained
full oracle command outcomes above keep their original PASS/FAILED status.

Route-order correction implementation checkpoint: `423bf4b818d5380250fd9f9509a5846b117b5b10`.
Final administrative head follows in dispatch after all evidence is committed.

## Final pass2 source/project and evidence hash inventory

```text
{
  "src/Cna.Core/Campaigns/CampaignCombatPositiveEntry.cs": "6637c77c1f9986bb7f9912ba0f3a028f997a2a00704e96e14f12cc88a9787106",
  "src/Cna.Core/Campaigns/CampaignCombatPositiveEntryCodec.cs": "ee0ea68aed557b358f2624ccf12fe9da2b88a8fc7a12fab0e0a40c28a3969e54",
  "tests/Cna.Core.Tests/Campaigns/CombatPositiveEntryTests.cs": "d5bb19cb9fc2410daf0a81c19ed9231ec92f590cde8052553cd6062c05af8c58",
  "tests/Cna.Core.Tests/Cna.Core.Tests.csproj": "d1641cea314043ee63d3fb3bec3b4ffdfd69c49b713fbf5ed54756ee011f6f88"
}
812b5d7966ec5f290490e0fef1ea6028d75675863a16d486952c216ac117bfb0  /private/tmp/n3-gates/route-focused-20261005-065314--83146--gePgCb.binlog
465e020a9ce65c1b0eb8f3540cf74486cdcc92a8c77311bd7ab9e9862667b021  /private/tmp/n3-gates/route-focused-20261005-065319--83146--48rRx1-dotnet-test.binlog
79761e42de57dcec4578e69c2aad2ea167c80ead3a620f8e3d0be6f40517a602  /private/tmp/n3-gates/route-focused.log
d72ec95091556dfbfda1880686d7e51138265453b318e1cce583c4178e9abfa6  /private/tmp/n3-gates/route-gate-20261005-065350--83826--TsJHKh.binlog
bfcaf5dc0b1a7c874ebb94e88f3fd93f9003e21fd84add52eeea926fbee50cae  /private/tmp/n3-gates/route-gate-20261005-065415--84410--zaG2nd.binlog
2d846eb20ed7c3bab0a7a721657b1b4c858899c20e0f3851ab2b901f2e8e7c1e  /private/tmp/n3-gates/route-gate-20261005-065420--84442--r98wyr-dotnet-test.binlog
707ec7b5ae59629a90221c9449755d50f1e19bb85c75e2bbbecdfa65faffc7b2  /private/tmp/n3-gates/route-gate-20261005-065427--84646--xG2XTz-dotnet-test.binlog
9dfe0348c08dbffc69c10dfae789a1c0c41d4646917d7be5ba7e2ff37c0e5b9c  /private/tmp/n3-gates/route-just-check.log
5a71c948e9055ce53798928ca699785d1f61453ac1d7f9d907712361ade29aba  /private/tmp/n3-gates/route-oracle-probes.log
c17aab9e4ed946f3c131d5491e3643d6ba644f0288a746f07d7240f02833292c  /private/tmp/n3-gates/route-protected.log
49bd1301e242c43c47b5cc47a7c6ddf27f3f026211b64077b41016d9e3e3d8f3  /private/tmp/n3-gates/route-red-20261005-065249--82670--Ol2s7q.binlog
cadab285dcb971c24e8e0138849059b8eab18269007b81460a4e21a9f6c60f7e  /private/tmp/n3-gates/route-red-20261005-065258--82670--fJCza_-dotnet-test.binlog
c2f251d88ace12fa15a4913a9923d6ed7f3521aaf13e61c1fc63a4f311424a43  /private/tmp/n3-gates/route-red.log
```

Full-suite lease RELEASED after successful gate. No further behavior edits or gate reruns planned.
Automatic approval review rejected coordinator status messaging for lack of verified direct user
authorization. A request for user authorization is pending; review-ready packet is complete locally.

## Final independent acceptance and administrative closeout

N3 set1/pass2,total2of9 reviewed exact `5c847895bc269d06e9d4e8d86bf90f9acd473f31` and returned
**Ready with non-blocking follow-ups**, with no actionable findings. Author accepts the report.
Coordinator accepts the original Breakdown source-pin maintenance follow-up and frozen grammar/
two-source limits; these remain separate work with no scope expansion or parent017–019 closure.
All four runtime/test/project freeze hashes and reviewed bytes remain unchanged. Gate evidence
is retained without reruns. Prior pending-review statements above are historical checkpoints.
Coordinator owns publication and required final-head CI/merge. No outbound message or publication
is needed from this author. After this administrative commit, hold all files stable.

## Full independent report: N3 set1/pass2

Report wording follows unchanged (trailing whitespace normalized).

**Ready with non-blocking follow-ups.** No actionable findings remain in the bounded native positive-entry implementation or plan. The prior P2 route-order defect is corrected.

Review instance: **N3 set1/pass2, total2 of9; maximum3 per set**.

Reviewed branch `codex/native-positive-entry`, base `84f1fac861cef7c8ffaf8f36dd75b1b7f4dedc83`, final HEAD `5c847895bc269d06e9d4e8d86bf90f9acd473f31`. HEAD remained stable; only generated `.serena/` was untracked.

**Findings and plan review**

The five primary paths and minimal documentation changes match task019E1’s manifest. Full Request/Created11 and opening history authentication precede owner Movement completion `11→12`, System Breakdown completion `12→13`, and candidate certification. Actual Movement-end evidence, distinct receipts, prefixes, ownership, retries and unchanged World/resources/RNG are supported by source inspection and verification.

The [codec correction](../../../src/Cna.Core/Campaigns/CampaignCombatPositiveEntryCodec.cs) preserves ordered routes and revisits while retaining identity sorting. Both-owner regressions now reach proof-mismatch error `006`, matching the unchanged oracle.

No heavy pivot or scope expansion is warranted. Selection/result consumption, repeat, public activation and parent017–019 closure remain outside this implementation.

**Author-claim reconciliation**

Preliminary ledger was recorded before reading the separate author packet. Dispatch had already disclosed the prior finding and baseline limitation.

| Claim | Assessment |
|---|---|
| Full creation-rooted source authentication | Confirmed by predecessor replay, exact opening pins and independently constructed openings |
| Successful byte parity, all cuts and original-byte retries | Confirmed by literal comparisons and focused tests |
| Route-order correction preserves canonical/error parity | Confirmed by both-owner regressions and six independent oracle probes |
| Owned source/state/proof/event bytes | Confirmed by copy boundaries, cloned projections and ownership tests |
| Actual Movement-end proof; candidate only at13 | Confirmed by transition and certification inspection |
| Final full gate:2533 solution /81 Boundary | Confirmed as retained evidence through logs and matching hashes |
| Original Breakdown oracle remains FAILED | Confirmed; unchanged baseline limitation, with no new source-pin impact |

**Executed verification**

- Focused native MTP tests: **21 passed, zero failures/skips**. Initial sandbox invocation failed on named-pipe permission; approved retry passed.
- `python3 -B docs/specs/verify-combat-positive-entry-v1.py`: **PASS**, two owners, four events, six literal proofs, six cuts/retries and3,966 rejection probes.
- Independent oracle route probes: **all six matched `CMB-PEN-006`**.
- Native codec inventory: **all61 object shapes matched transitive oracle schemas**.
- `git diff --check <base> HEAD`: **PASS**.
- All208 spec files and686 protected existing source/test files: **byte-identical to base**.
- Final retained source/project and evidence inventory: **17 hashes matched**.
- Retained final `just check`: **2533 solution tests,81 Boundary tests, zero failures/skips, clean format and zero build warnings/errors**. Full suite was not rerun.

Reviewer evidence: focused log (historical local artifact: `/private/tmp/n3-pass2-focused-approved.log`), binlog (historical local artifact: `/private/tmp/n3-pass2-review-20261005-071544--11865--tOUQWp-dotnet-test.binlog`).

**Residual risks and next actions**

The accepted original Breakdown source-pin failure remains a separate maintenance follow-up; supplementary checks do not convert that command into a pass. Frozen grammar duplication and two-source admission require explicit review when extended.

Serena was unavailable. Exact-worktree graph coverage was checked; stale codec/test coverage used direct source fallback. Graph evidence remains best-effort.

Coordinator may proceed to publication and required exact-head CI/merge gates. No repository edits, commits, subreviewers or outbound messages were created.

Reviewer report SHA256: `786e95c1ba03f779266af81e1e05618b62a27c8cc79e9e52464577a04d8139a9`.
Reviewer evidence SHA256: `30b09ec3c893468af234a61f2bfcd9a977de016446aed18a4a13b71f1a8143d3` — `/private/tmp/n3-pass2-focused-approved.log`.
Reviewer evidence SHA256: `af8ef6ba7a8f2261858d9833e8e25f0e6313adba56e782c10bbfba06eb1ef37a` — `/private/tmp/n3-pass2-review-20261005-071544--11865--tOUQWp-dotnet-test.binlog`.
