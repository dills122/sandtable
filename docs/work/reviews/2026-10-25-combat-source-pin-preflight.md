# Combat source-pin reconciliation preflight — 2026-10-25

Status: experimental docs-only repair rejected by review 5 and fully restored. Native compatibility decision required; no repair accepted. Original pins, oracle logic, runtime behavior, and historical evidence remain unchanged.

Base: `3e2d4336b1505d9f308529841361e5413cf70f74` on `codex/rc-reserve-crash-recovery`. Ruflo task: `task-1792935925249-7e07md`, namespace `sandtable-rc-20261023`.

## Observed evidence

- Original Breakdown completion oracle: exit 1, 0.361 seconds. Original cycle sequence oracle: exit 1, 0.045 seconds. Both reject `Cna1979LandSequence.cs` source drift.
- Pinned source SHA256: `c5426245a156367eac85fc5e61792ece1ae124651a0416793fde00213641b938`.
- Current source SHA256: `d019a3bc2941ad788b69550f5d7fc01e33fa586a0bad6a374c08550b3cb6a79a`.
- Source matches cache commit `2e17f60767cceff6db950db532d9432f5b81a9cd`. Compared with pinned parent `c22346d521973ed2c7c29de7841de0677a8b3dfe`, delta adds a volatile last-turn cache; catalog construction and validation remain unchanged.
- Release Core test-project build passed. Focused `LandSequenceCacheTests`: 8/8 passed. This supports cache invariance, not complete downstream fixture equivalence.
- All 21 hashes listed in historical maintenance audit still match (20 maintenance paths and comparison-only runtime source).
- Current digest-reference scan finds 35 candidate dependent paths. Adding same-family generator relationships produces 46 candidate paths and 176 edges. Eleven additional fixture candidates require owning-generator confirmation. This is a conservative inventory, not an approved edit whitelist or proof of complete closure.

Evidence receipts, original RED logs, exact source diff, and machine-readable inventories: local ignored `artifacts/rc-source-pins/`. Historical audit: `docs/research/combat-verification-pin-maintenance.md`.

## Proposed bounded repair and pass criteria

1. Confirm generator edges and freeze exact edit whitelist before edits. Revalidate independent Content-document pin drift; classify separately or retain as explicit unresolved failure.
2. Capture canonical semantic payloads before refreshing root pins. Process confirmed consumers in dependency order. Never globally replace digests, weaken validation, or alter historical migration evidence.
3. Pre-B2 gameplay payloads and golden hashes must remain byte-identical. For B2/B3/outward and newer generated evidence, enumerate each allowed source/build/provenance-derived field change and its causal predecessor. Unclassified gameplay, ownership, RNG, privacy, grammar, or capacity changes block acceptance.
4. Run every affected owning oracle unmodified to completion; record failures/timeouts honestly. Add isolated pin/semantic tamper checks, including cache-warm rejection. Rebuild copied fixtures and run affected focused Core tests. Full release gate and exact-head CI remain later acceptance gates.
5. Independent review of scope before edits and final patch after validation. Existing review allowance is 4/4 consumed; request two additional instances (5/6 scope, 6/6 final). No reviewer dispatched and no review allowance silently reset.

This repair clears evidence freshness only. It does not admit absent original-300 rows, certify unsupported combat families, or establish public RC readiness.

## Frozen candidate inventory

Each hash below is current pre-edit content. Membership alone does not authorize mutation; same-family generator candidates require confirmation.

| Candidate path | SHA256 |
| --- | --- |
| `docs/specs/combat-actual-assault-v1.schema.json` | `a16d852d5a4463a295e315c824e5298f4ff22b979a7c307187c2a8d926136f0d` |
| `docs/specs/combat-actual-moved-assault-v1.schema.json` | `21097270c395bd1a3937d6787db2a992b8c145e42e06aec080652800b9e19e4b` |
| `docs/specs/combat-actual-moved-continuation-v1.schema.json` | `0a2a0d8089398246b38c84d043ea343a0e3d72d84378b90f0d6da155c2fe3986` |
| `docs/specs/combat-actual-moved-cycle-control-v1.schema.json` | `91f235388faa66ef397a9f466f503075da8443148489aef1e7ac27f2dd1751e8` |
| `docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.schema.json` | `26909e67501f59a0d0892181b9c9499099552c721585369983c3dbde5784c955` |
| `docs/specs/combat-actual-moved-repeat-movement-completion-v1.schema.json` | `3a80a1cd94b41e8a88590d8eadddcc4c821bfefa7b0021489ad4e7cfe50af8ef` |
| `docs/specs/combat-actual-moved-repeat-movement-v1.schema.json` | `eb1cf39788cc78376b6d798a13a4a3272906217c51ff02b90310843654122468` |
| `docs/specs/combat-actual-paid-assault-v1.schema.json` | `be1c5fcc36641ae1ee9f58b4b1c73baca098e7a8cde43d8dc568197f59399324` |
| `docs/specs/combat-actual-post-assault-continuation-v1.schema.json` | `e948daf499acbb365d9be52b083a10d1692ccbb89fd4bf94e0b4a13601f41c36` |
| `docs/specs/combat-actual-repeated-breakdown-completion-v1.schema.json` | `df4ad5e51e683ebddd0d185cb3042042132d8312447c08faf1f237ecb2274680` |
| `docs/specs/combat-actual-repeated-movement-completion-v1.schema.json` | `77ef1573e24e131fdddbe4daf733a4472a6dd79b15c6eb227be545ae7c687a2c` |
| `docs/specs/combat-actual-repeated-movement-v1.schema.json` | `a3ebd21e0fd1b1d85b7335ceb2afd304511a2efd5e84bf59710a786526dfd900` |
| `docs/specs/combat-actual-round-entry-v1.schema.json` | `677b1c671bebe5f23cf4b80f03078c55000c60e45416701757e2552cbd05edb8` |
| `docs/specs/combat-exercise-child-evidence-v1.schema.json` | `cc8a7c9e89068b9a78f3abb4044230ccc9ef359665198aaa7ef6db5b15782bf6` |
| `docs/specs/combat-exercise-parent-evidence-v1.schema.json` | `00d1fcd2cb7d06b6f25d9224ff0839308f23fd2052b42dab24c98ea378b30820` |
| `docs/specs/combat-outward-composition-v1.schema.json` | `6e8ec1103315238c0c5520c6cff9b31b20d6de2386e197944265a48168021344` |
| `docs/specs/fixtures/combat-actual-assault-v1.json` | `9a486f77d0463c071c55af9196511ec2f1214763ed2278ba89962b30746aba09` |
| `docs/specs/fixtures/combat-actual-moved-assault-v1.json` | `603dd2d31f3ef384955130b5125dde6341bffdbb643400592aaa0df388aa14a0` |
| `docs/specs/fixtures/combat-actual-moved-continuation-v1.json` | `2eff230735c50acd87651a298eb0293bbed0498e4b5c2fde43325287c3595c9a` |
| `docs/specs/fixtures/combat-actual-moved-cycle-control-v1.json` | `be7832bbcdecb7517d9e3cd85f07ec3b0978222a2c1485561e6f5627336d38f9` |
| `docs/specs/fixtures/combat-actual-moved-repeat-breakdown-completion-v1.json` | `4d573d4a9f27442467b69de9996e4043840497deba4513e1c7be7aa4d3dfc35e` |
| `docs/specs/fixtures/combat-actual-moved-repeat-movement-completion-v1.json` | `7c887ebbfe8889279fcf5d9269cef2538af6fb38cacf0279a420f2e3496f2258` |
| `docs/specs/fixtures/combat-actual-moved-repeat-movement-v1.json` | `481f93d483d15dd7425f2d025c8d823cfe680ebc36acd3b408ae32bb64e20b8d` |
| `docs/specs/fixtures/combat-actual-paid-assault-v1.json` | `80dfa0157542d70660f78d0e2326564fcb9095a71f84ecab2deaf061c5e7b203` |
| `docs/specs/fixtures/combat-actual-post-assault-continuation-v1.json` | `9cd49e98ea011228bc63a296aede917b8b517926b3d50df244df139e7d9f2679` |
| `docs/specs/fixtures/combat-actual-repeated-breakdown-completion-v1.json` | `97937286ce27286f8a2d3431fcb2ca2b6e928cb852c1b185fca37372cfe29652` |
| `docs/specs/fixtures/combat-actual-repeated-movement-completion-v1.json` | `1cba27ef5f08e4fa72695d4fc10676aadc3bb172179664c9d758d5f861b8e86a` |
| `docs/specs/fixtures/combat-actual-repeated-movement-v1.json` | `d77c4b1e0a3e3750df79ad351dd1cef3c8966834eb0984a77b62938e57b87501` |
| `docs/specs/fixtures/combat-actual-round-entry-v1.json` | `fc17cda07618bdd539a56b8c296037bbf36f37c6eb2ce6258c2b789b765f640b` |
| `docs/specs/fixtures/combat-authority-composition-v1.json` | `a90f88de9c05b4b1bd7081e7828abb7553e3e0537a6efa8cad042c87a6df50b1` |
| `docs/specs/fixtures/combat-cycle-sequence-v1.json` | `de89ebcd86f171119d206d0a90553cdeb815e7b8b6fc8903c9dfcacbd1db5b3f` |
| `docs/specs/fixtures/combat-exercise-child-evidence-v1.json` | `5587cb17e53c48a9ab393532ebaa186f3d1384632eb13b105e1af8a698e799b6` |
| `docs/specs/fixtures/combat-exercise-occurrence-v1.json` | `aabc20a39229ac121703a6cda077c0ebeffbfdd7bc3142a3d0c1d0805af7a38e` |
| `docs/specs/fixtures/combat-exercise-parent-evidence-v1.json` | `08316dae3d4f12298c35bfe05f640dcd2559ae41cbf4144430cd52b121847a9f` |
| `docs/specs/fixtures/combat-inherited-armed-continuation-v1.json` | `d86603ff7f07f2e0a3624e8748fedcf4406e2d873360f4e56a30e5952c645b1f` |
| `docs/specs/fixtures/combat-inherited-breakdown-completion-v1.json` | `7d6ee76d454bb047e03d104310758224d9470a0e9aabd30ce841abb32ffc54d1` |
| `docs/specs/fixtures/combat-inherited-cycle-control-v1.json` | `e2f2c76539a3509310fa2f1822fd1d84e1346f62cfc274ce12682866d02ba236` |
| `docs/specs/fixtures/combat-inherited-no-attack-v1.json` | `4e8f8a9fa0d1e973d6a5d7046b3e013f4270bd31c53afa24ed7595f732be6bc1` |
| `docs/specs/fixtures/combat-inherited-reserve-movement-completion-v1.json` | `e336d9b5afe498b3886318509958cb2e50ae860676ee1c4603c55540a41847d3` |
| `docs/specs/fixtures/combat-inherited-reserve-movement-v1.json` | `4feb5dc5e178d9242b8a6ef3b42f8101b3810e60bfe722506fa8838356d6567f` |
| `docs/specs/fixtures/combat-inherited-selection-v1.json` | `76240f2409f341757deea0f6ab5d346699de5a4a33690f95ccb7e74e160c1f4f` |
| `docs/specs/fixtures/combat-inherited-snapshot-v1.json` | `9dab9b8f72c3643fd50c62fd6e97335e5bc719ad1f61f4a15a910597224c862f` |
| `docs/specs/fixtures/combat-outward-composition-v1.json` | `28e37f48c80ed6ca6b7f7fabb4fb72b5d3433305ad662e091e2625a017bc1c24` |
| `docs/specs/fixtures/combat-side-projection-v1.json` | `07139da2ff125345c3007aa6bd9a5760eec44e4d4f25d0ec85c7e06fed4da44f` |
| `docs/specs/verify-combat-exercise-occurrence-v1.py` | `9e46db1943514629754cdc227fdc1a415c53c8b5e573b8fb0afd89c870f1fb53` |
| `docs/specs/verify-combat-side-projection-v1.py` | `964f4b6a6901e446e5d0e0a6c49e79e778677ae458947681034a4ddd5d848a0d` |

## Approved attempt and superseding decision gate

Human approved expanded docs reconciliation and review allowance 6 on 2026-10-25. Scope review 5 initially permitted bounded maintenance; amended verdict below supersedes that permission. Final review 6 has not started.

The experiment changed only 33 candidate files' digest slots; no grammar, runtime source or oracle control flow changed. Review independently confirmed PR243 added advisory P01 prose only to opportunity-identity and settlement-disclosure documents; their existing Side Projection pin fields were included. Outward schema and fixture remained excluded.

Observed Python results on experimental bytes:

| Oracle | Exit | Seconds |
| --- | --- | --- |
| cycle-sequence | 0 | 0.769 |
| inherited-breakdown-completion | 0 | 56.518 |
| inherited-selection | 0 | 72.351 |
| inherited-snapshot | 1 | 1.440 |
| inherited-armed-continuation | 0 | 8.358 |

Snapshot reports an additional independent declared source-pin mismatch for `src/Cna.Core/Campaigns/CampaignCurrentEventRuntime.cs`. No automatic refresh attempted. No-attack diagnostic was interrupted at review's stop; remaining batch checks unrun. These experimental passes do not describe restored HEAD and do not close any gate.

A full tracked src/tests digest scan identified 12 native source consumers missed by the docs-only inventory. Native authentication immediately rejects refreshed dependency bytes. Focused reproduction after rebuild:

`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-build --no-restore -- --filter-method '*AuthenticMovedSourceAdmitsInitialSelectionWithoutResettingCp'`

- Experimental docs: 2 failed, 0 passed, exit 2, `CMB-MAS-009`; build exit 0.
- Restored exact pre-edit docs, rebuilt: 2 passed, 0 failed, exit 0; build exit 0.

All 33 edits restored from verified pre-edit SHA256 backups. Patch retained at `artifacts/rc-source-pins/partial-pin-refresh.patch`; detailed mismatch inventory and RED/restored receipts in same directory. No runtime changes, commits, push or PR created for this repair.

### Review 5 amended outcome

Fresh-context reviewer `/root/pin_scope_review`: **Not ready — heavy-pivot stop required**. Earlier bounded-ready verdict superseded. Review count remains 5/6; clarifications used same instance.

P1: `CampaignCombatActualMovedAssaultCodec.SnapshotDependencies` checks hardcoded Pins and Delivery. R03 additionally enforces FrozenManifestHash and embeds manifest-derived identity into basis, commands and events. Updating native constants is therefore a compatibility decision, not merely documentation maintenance. Previously retained evidence cannot be assumed readable after replacement.

Proposed next scope, requiring human approval: inventory full tracked-repository dependency closure, reconcile native pin/manifest handling, and preserve prior evidence through explicitly versioned, exact frozen manifests. Do not accept arbitrary manifests or weaken hash validation. Define old/new replay expectations and focused negative tests before implementation. Treat unrelated Snapshot runtime-source drift and outward doc drift as explicit separate failures until independently justified. Run final review 6 on expanded implementation and evidence.

Alternative requiring explicit product decision: intentionally retire old private evidence and refresh one current manifest. Not recommended without confirming persistence exposure and providing rejection/migration behavior.

### Native consumers discovered

- `src/Cna.Core/Campaigns/CampaignCombatActualMovedAssaultCodec.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualMovedContinuationCodec.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualMovedCycleControlCodec.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualMovedRepeatBreakdownCompletionCodec.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualMovedRepeatMovementCodec.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualMovedRepeatMovementCompletionCodec.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualPaidAssaultSupport.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualPostAssaultContinuationCodec.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualRepeatedBreakdownCompletionCodec.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualRepeatedMovementCodec.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualRepeatedMovementCompletionCodec.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualRoundEntry.cs`


## Approved native revision implementation (2026-10-25)

Human approved expanded native scope after review 5. Two complete code-owned dependency revisions now preserve old replay identities while permitting reviewed physical pin reconciliation. Legacy archive contains 145 exact base files; full paid/armed R03 vectors retain old retry/event/proof identities. New snapshot owns bytes and registrations, rejects mixed/unknown sets and propagates revision through nested readers. Original legacy registrations remain; refreshed map explicitly lists reviewed replacements.

Verification and final status live in [author explanation](2026-10-25-evidence-revisions-author.md), [semantic comparison](2026-10-25-evidence-revisions-semantics.md), and final independent-review report (instance 6/6). Earlier restored/failed experiments above remain historical evidence.

Validation-plan clarification: run focused affected native replay, negative and compatibility checks locally. Original complete oracle runs and their exact input state must be distinguished from final metadata-only reruns of selected original groups. The original criterion requiring every affected owning oracle on final release head remains an open release checkpoint unless explicitly demonstrated; generation alone is never a passing oracle. Full solution and exact-head CI are not implied by this bounded packet.

Separate Snapshot source drift includes both `CampaignCurrentEventRuntime.cs` and `CampaignElementMovedV3Factory.cs`; no repinning of either. Outward drift, public certification, unsupported ancestry and product gates remain open.
