# S2 actual-selection bridge research handoff

Date:2026-10-05 America/Toronto. Status: prepared for independent research review.
No independent verdict, publication, CI or implementation is claimed.

## Scope And Exact Checkout

Base afa396ad5094fae7b8f054c60a9df9e03f83dfd5.
Worktree /Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable.
Branch codex/combat-actual-bridge-research. Exact committed review head accompanies REVIEW_READY
delivery and must be supplied by coordinator to the fresh reviewer; verify equality before review.
Four new research/admin documents only; no primary/source/spec/fixture/pin changes.
Untracked .serena/ is generated exact-worktree tooling, not retained research or a reviewed artifact.
No full-suite lease taken. Research started12:46UTC; exact final freeze time is in delivery.

## Decision And Dependencies

[Report](../../research/combat-actual-selection-bridge-feasibility.md) recommends conditional GO
for new actual selection through accepted defender RBA decline at Force Assignment, with existing
no-attack/cancellation paths; current C3a/Round2/Result2 remain incompatible.
Coordinator reports S1a preliminary deferral because closure reaches privateExercise identities
beyond three files; review pending. S1a reviewed disposition must be frozen before S3 evidence pins. Original Breakdown failure is
reproduced and not waived. No S1 runtime/fixture ownership, public activation or parent closure.

## Neutral Review And Separate Author Packet

[Bootstrap](../reviews/2026-10-05-actual-selection-bridge-bootstrap.md) must precede the
[author explanation](../reviews/2026-10-05-actual-selection-bridge-author.md).
Coordinator creates the fresh high reviewer; author creates no reviewer or subagent.
S2 review policy maximum3sets ×3passes,9total,2high recovery sets; stop on Ready or heavy pivot.

## Executed Commands And Results

Environment: macOS arm64, Python3.14.6, SDK10.0.400, SDK-style native MTP/xUnit v3.
global.json10.0.302 rolls forward latestFeature; runner explicitly Microsoft.Testing.Platform.

Commands ran from the exact worktree; redirection only retains stdout/stderr in temporary logs.
The native command required authorized worktree write/build access; no approval rejection occurred.

| Command | Result |
| --- | --- |
| dotnet --version |10.0.400 |
| dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class Cna.Core.Tests.Campaigns.CombatPositiveEntryTests /bl:/private/tmp/s2-positive-entry-20261005.binlog | exit0,21passed,0failed/skipped,13.122s |
| python3 -B docs/specs/verify-combat-positive-entry-v1.py | exit0,2owners/6proofs; detailed stdout below |
| python3 -B docs/specs/verify-combat-selection-steps-v1.py | exit0,5traces/41cuts/246mutations/164raw |
| python3 -B docs/specs/verify-combat-sealed-round-v2.py | exit0,12groups/10traces/68cuts/610mutations/340raw |
| python3 -B docs/specs/verify-combat-result-settlement-v2.py | exit0,10groups/32synthetic traces/304cuts/3728mutations/1360raw |
| python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py | exit1,source drift: src/Cna.Core/Rules/Cna1979LandSequence.cs |
| python3 -B /private/tmp/s2-bridge-probe.py /Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable | exit0,mechanics-only/negative probes; exact code/stdout retained in author |
| Python Git-show byte comparison of40source paths against baseline | all40exactly equal |

The durable probe was also extracted from the author Markdown and rerun with byte-identical stdout.
No production actual-selection input-ledger authenticator exists. The harness supplies simulated
trusted inputs; eight re-signed event actor/time claims reject against its independent ledger.
The future Core API requires authoritative caller input arguments; public/host authentication
remains a separate gate.

Original successful oracle stdout:

```text
PASS positive entry: 2 actual owners, 2 events each, 6 literal proofs; {"canonical": 168, "capacity": 26, "clock": 156, "cuts": 6, "event": 394, "input": 258, "order": 14, "proof": 1524, "retries": 6, "source": 1410, "unsupported": 16}
```

```text
PASS: 5 literal traces; 41 event/control cuts; 246 event mutations; 164 raw rejections; exact retries, deadline/regression/unavailability, RBA races and FA gate. Isolated boundary probes only; no full campaign replay.
```

```text
PASS: sealed-round v2: 12 semantic groups, 10 retained traces, 68 cuts, 610 replay mutations, 340 raw rejects, 288 clock comparisons/retries, 480 lifecycle retries, 30 invalid proposals
```

```text
PASS: result v2: 10 semantic groups; {'traces': 32, 'cuts': 304, 'mutations': 3728, 'rawRejects': 1360, 'timing': 384}; 200 same-owner prior-time isolation comparisons
```

Breakdown failure is the original command failure, not corrected or counted as a pass.
No full solution/Boundary/build/format/CI run for new behavior is claimed by this research.

## Temporary Evidence Hashes

The durable author appendix contains the exact final probe and stdout; temporary log/binlog files
may disappear after host cleanup. Their hashes are recorded for audit; missing logs reduce direct
historical inspection and do not authorize silently replacing old results with new runs.

| Path | Bytes | SHA256 |
| --- | --- | --- |
| /private/tmp/s2-bridge-probe.py | 11665 | f096a07b1ea779a8d4fa70c60fd4b38ef5fef9c57cdaf74944d19f7747ab3cc0 |
| /private/tmp/s2-bridge-probe.json | 11292 | c1ec75db3d42aadd2a07589df14a9ac5d33ce96af77eb1b16c8995f0c9ba18f2 |
| /private/tmp/s2-positive-entry-20261005.log | 424 | 4e202f24496fa82b79ab8dcee560cfa63cf0b0638c855f368f9101b6e9cdb6de |
| /private/tmp/s2-positive-entry-20261005.binlog | 542194 | 0c565ef066a6dc3171efecf04891982526594ce92357b49f675b6061a88d79da |
| /private/tmp/s2-oracle-positive-entry.log | 236 | 37988e248972aed56c4b2f9112a3cb8b55e94c023add34a1635e4e31b4359199 |
| /private/tmp/s2-oracle-selection.log | 218 | a6d1fb28ec2d5487bfd23e5cd0c4dc208c4bda1147d027ad9ded9c0639777691 |
| /private/tmp/s2-oracle-round2.log | 186 | 7497cdcac2a85a116e5b2eaaf43be2313d90522fddc1da852312b388eedd527c |
| /private/tmp/s2-oracle-result2.log | 168 | 2a034604581df3937100289a18e23f5b3270ba57d6ff345cb0f5c2e9f3b400bb |
| /private/tmp/s2-oracle-breakdown.log | 1013 | b74c24e87d0c1d878c6d04453e2a4abed764bc3175d6a0599f00184288fb6e2c |
| /private/tmp/s2-source-inventory.json | 6930 | d4f343908d633c71b9f6a82a94a4594664c6262d6d32c1a6f6746ec7768ee8ff |

## Exact Source Inventory

All40rows were byte-compared with Git objects at baseline. Hashes describe observed source,
not future S3 pins. docs/specs coverage uses source reads because the graph excludes docs.
Supplemental native coverage found no recorded gap for codec/models/tests/sequence paths;
test-project graph metadata is untracked and direct source reads supplied that evidence.

| Path | Bytes | SHA256 |
| --- | --- | --- |
| docs/research/combat-positive-entry-feasibility.md | 32468 | 463342c425603342c8892292fcde27e36739cc053bb015ee473db77d220eb060 |
| docs/roadmap/pre-alpha-roadmap.md | 89762 | 57448b47b58535b6ba602aee88bfc5b0b897eca65cf78f12bee3a28b764a1553 |
| docs/design/combat-cycle-implementation-plan.md | 207696 | 9051eefb10dfd62e3833da3c66229da6d96ba62e6c8652dc0479adfc0670e918 |
| docs/work/handoffs/2026-10-05-native-positive-entry.md | 27309 | cdfb35d7ef1b4006e7f93baa728567651d7fbba36b23172aede4694a238ed2da |
| docs/work/handoffs/2026-10-05-overnight-session.md | 11140 | a557c3374a1ff98444fd1a4c83961022ac17dd3192765e9465eebbaaaaef2341 |
| src/Cna.Core/Rules/Cna1979LandSequence.cs | 9346 | d019a3bc2941ad788b69550f5d7fc01e33fa586a0bad6a374c08550b3cb6a79a |
| src/Cna.Core/Campaigns/CampaignCombatPositiveEntry.cs | 16675 | 6637c77c1f9986bb7f9912ba0f3a028f997a2a00704e96e14f12cc88a9787106 |
| src/Cna.Core/Campaigns/CampaignCombatPositiveEntryCodec.cs | 20159 | ee0ea68aed557b358f2624ccf12fe9da2b88a8fc7a12fab0e0a40c28a3969e54 |
| src/Cna.Core/Campaigns/CampaignCombatSelectionSteps.cs | 20207 | 32551f54656c63f69d2517f7ab446fec4951679ff311154a26df84940c009efb |
| src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs | 23179 | 90ef43ea5e10be1c8473c0991c591304c16df218ef3f593117ea64b436e09d30 |
| src/Cna.Core/Campaigns/CampaignCombatSelectionStepsModels.cs | 4340 | 7e9ed3131d672ff67f02332331ac4aab5f68736e3204a4a2e19933baa5b3a1d8 |
| src/Cna.Core/Campaigns/CampaignCombatSealedRound.cs | 18980 | 904df6123bdd210aac4d9ea1bcc90d52646680498c89ff49fe478c464aba5b3c |
| src/Cna.Core/Campaigns/CampaignCombatSealedRoundCodec.cs | 27995 | 6a771eb6204d607e634db567842719cb42fe69dea0c7c2813e1ff40b1f832a82 |
| src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs | 42938 | fa86081d9a73a6e7f97bdd33b12d2a5c27bc94ea36d674ed7c11eb0f34652285 |
| src/Cna.Core/Campaigns/CampaignCombatCertification.cs | 19335 | 2d3859c248783d07772753b5105848c4555cd7b7e146ea7cd9d740316cb705da |
| tests/Cna.Core.Tests/Campaigns/CombatPositiveEntryTests.cs | 35760 | d5bb19cb9fc2410daf0a81c19ed9231ec92f590cde8052553cd6062c05af8c58 |
| tests/Cna.Core.Tests/Cna.Core.Tests.csproj | 9924 | d1641cea314043ee63d3fb3bec3b4ffdfd69c49b713fbf5ed54756ee011f6f88 |
| global.json | 171 | be842c5463f5bc9a6f38da23933c12d1f031a7ec13a042ba6257845d4f759fcb |
| Directory.Build.props | 548 | db9e173414b22b95407f703e22bf9e486b8814d92e7ad50fee6270f8ed52cbd2 |
| Directory.Packages.props | 1331 | 188154a6bccb21b838a96620750bfe7f474d918cd64105bc9c763a8a4ca9bba4 |
| docs/specs/combat-positive-entry-v1.md | 12933 | 44c12e0aef7908b6b2174c9034666cb1365fbc8f8d84b7755d38d28a2d9d05df |
| docs/specs/combat-positive-entry-v1.schema.json | 1420 | 73724384ba3cf61b462ef7b47ae70e5108281a6bbaf0b80878c55247e4e6fa36 |
| docs/specs/fixtures/combat-positive-entry-v1.json | 173815 | eb59146f34bfdc47753b9f0aff7f40709eba4266aff522655ac6886be15b951e |
| docs/specs/verify-combat-positive-entry-v1.py | 30135 | b461a8bc6a194def5f03767783b49b5797a5630b6a7c26a986f7bfe2ee97c172 |
| docs/specs/combat-selection-steps-v1.md | 15092 | d1414a4f080c3f92fe48909bebb359adcb46ea39052b31ee915983af2b6a5c6e |
| docs/specs/combat-selection-steps-v1.schema.json | 2871 | ef24e125012ab390bb0c932a09472b8220d7e8c95c1b19c29bdb611be532162d |
| docs/specs/fixtures/combat-selection-steps-v1.json | 138224 | 151c8da5037dfd5fa921abf1f675ef1a10211f8b84a4e1fb385d6beb62b1d0de |
| docs/specs/verify-combat-selection-steps-v1.py | 26298 | dc037282f74c34246d350faebeea55e81677d27dd181de858b8270e4eaa73c86 |
| docs/specs/combat-sealed-round-v2.md | 12000 | 9fb337bdbfeebf0db367f860aafeb6a37b72af0d602036aec41da82678332bc5 |
| docs/specs/combat-sealed-round-v2.schema.json | 3748 | 365283c1c51b9e155aa1651c253759b271bbda75f613f52f511ceeb0dec12c7c |
| docs/specs/fixtures/combat-sealed-round-v2.json | 1051587 | 8200354f49bef2fd4a976dd9c24e4e485f8c06ad903d8c3deb06d5a9db509a95 |
| docs/specs/verify-combat-sealed-round-v2.py | 39247 | d1091b5d1d6fb1d9ac88da45313c88fcbc922f763e62d01af44311abc8656bf9 |
| docs/specs/combat-result-settlement-v2.md | 12547 | a040dbd69c8c5e978bbce79b8765088ab2efdc33e8ef95ced22a5f35680fbc68 |
| docs/specs/combat-result-settlement-v2.schema.json | 2934 | 719634c4720b4a92669b2e83e60df58cd32d0e556ec9ef66c546aed1f9c7e9d6 |
| docs/specs/fixtures/combat-result-settlement-v2.json | 3658583 | 6a1f6cda74424680669539d73583fa3ba5affc612380b3a3efe2e8986a09804a |
| docs/specs/verify-combat-result-settlement-v2.py | 37568 | a6a781976f26b78a9dc098ebfbb3b516284aa1d5873744d96d8fa23aa50d83a2 |
| docs/specs/combat-inherited-breakdown-completion-v1.md | 7527 | 737636b92e78f74e340074dc49cf08ad3594b751f9b5b34880a4a4ef246708a8 |
| docs/specs/combat-inherited-breakdown-completion-v1.schema.json | 2023 | 1ff841c704e1aa46e71513ead562a0ca1c54c05b35bdf97018d6854d8773fec3 |
| docs/specs/fixtures/combat-inherited-breakdown-completion-v1.json | 7020 | 7d6ee76d454bb047e03d104310758224d9470a0e9aabd30ce841abb32ffc54d1 |
| docs/specs/verify-combat-inherited-breakdown-completion-v1.py | 18595 | db903e019bb457c02930876e42a70b9204387494012419b0b97572c3c9e12eba |

## Next Actions And Preservation

Coordinator independently reviews the frozen research/plan, accepts/disputes/defers findings,
then owns push/PR/exact CI/merge. Suggested PR title:
Research authentic Combat selection provenance and bounded handoff.
PR body prepared separately in /private/tmp/s2-pr-body.md after commit.
S3/S4 only follow reviewed decision and explicit S1 dependency disposition.
Heavy gameplay/architecture pivots require human sync; no automatic old-pin refresh or waiver.
Current complete retained work is the four committed documents. No runtime WIP exists.
