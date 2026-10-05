# Handoff: native actual positive entry

Date:2026-10-05 America/Toronto. Task019E1/N3; author checkpoint, independent verdict pending.

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
max2high recovery spikes; no reset, heavy pivot requires user gate. No pass consumed at author handoff.
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
Final source/project and gate log/binlog hashes follow. Final corrected HEAD is coordinator dispatch
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
