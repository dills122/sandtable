# Actual Combat selection bridge feasibility

Status: S2 research recommendation for independent review, 2026-10-05 America/Toronto.
Source base: `afa396ad5094fae7b8f054c60a9df9e03f83dfd5`.
Decision owner: session-2 coordinator `01a0c9dc-00bc-78a3-800d-3cb36859e422`.
This research does not amend a governing contract or authorize native implementation.

## Decision

**GO, conditional on independent review and the coordinator's S1 disposition, for a new private
actual-selection contract and then a separate native adapter.** Consume the complete accepted
019E1 source, stop the positive path at Force Assignment after an authenticated defender RBA
decline, and retain deterministic no-attack/cancellation traversal to Reserve Release.
Use a distinct source/boundary/event identity family. Preserve historical C3a, Round2 and Result2
readers, pins, fixtures and trust labels.

**NO-GO for direct C3a reuse, Round2/Result2 admission, a combined selection-through-result slice,
or public activation.** Authentic entry already exists, but no admitted native selection consumer
for it was established. Seven existing selection mechanics can operate on those facts in a
controlled pure-kernel probe. That observation supports the next contract; it does not bypass an
admission reader or certify resulting scratch bytes as Chronicle authority.

| Boundary | Evidence and disposition |
| --- | --- |
| Adopted legal selection mechanics | Existing C3a rules select the certified candidate, complete Position/Barrage, accept defender decline and reach FA. Scratch replay succeeds for both actual sources. |
| Currently admitted actual lineage | 019E1 admits creation through idle Movement/Breakdown to candidate-before-selection at version13. Fresh native21/21 and the unchanged entry oracle pass. |
| New actual selection admission | Not implemented. Requires full source replay, independently trusted input ledger, new framing, owned bytes and frozen literal evidence. |
| Round2 and Result2 | Existing authenticated readers reject the actual request. Positive FA completion is also unsupported in C3a. Separate actual-round and actual-result gates remain. |
| Public product and authentic repeat loop | Not reached. Parent017–019, later-II/consumed, ordinary repeat, Snapshot/public/host/Runner and roadmap acceptance remain open. |

A stop after only segment opening would be smaller, but would leave the actual selected/declined
handoff unresolved. The seven-event endpoint is the smallest useful bridge to the next existing
round boundary; it introduces no new gameplay rule, allocation, resource charge or RNG draw.
If its executable contract or native parity cannot fit the five-primary-file manifests below,
return a scope decision before editing another primary file. A material architecture/gameplay
pivot requires human synchronization.

## Method and bounded scope

Question: can an authentic candidate-before-selection enter a useful legal selection consumer
without seed, receipt, owner, position, clock or history substitutions? Why now: session2 has an
accepted 019E1 entry prerequisite but lacks actual Combat consumption.

Authorized work is research, bounded temporary probes and four retained research/admin documents
in a managed worktree. No runtime/spec/fixture/pin edits, primary-checkout edits, full-suite lease,
new gameplay or publication activation. Compare primary canonical contracts, actual native
predicates/tests and causal byte replay. Stop at one decision and bounded manifests.

Worktree: `/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable`;
branch: `codex/combat-actual-bridge-research`. Serena activated that exact checkout.
Codebase Memory project `sandtable-actual-bridge-research`, fast generation
2026-10-05T12:47:25Z, 16,475 nodes/112,664 edges. The selection/entry/round search exhausted32
rows; the boundary/certification search exhausted5. Both-direction exact-qualified
ValidateBoundary trace exhausted6 callers/8 callees at depth1. It identifies SealedRound's
AuthenticateBase as a direct dependent. Resolver edges are best-effort, not authority evidence.

Coverage checked all ten initial evidence paths and Campaigns/docs-spec scopes. Six native paths
reported no_recorded_issue/metadata_match; docs/specs is excluded from this index. Exact source
reads and oracle execution supplied the documentation/Python fallback. No completeness claim is
made from clean graph metadata. Supporting paths/hashes are retained in the dated handoff.
The first short-name trace failed symbol resolution; the qualified trace succeeded.
No external rule interpretation or web research was necessary.

Evidence labels below mean **Fact** (direct source), **Observation** (executed), **Inference**
(recommendation from evidence), or **Unknown** (remaining proof).

Primary sources:

- [Canonical roadmap](../roadmap/pre-alpha-roadmap.md) and
  [implementation plan](../design/combat-cycle-implementation-plan.md), especially019E0/E1 and
  the final overnight reconciliation; [prior handoff](../work/handoffs/2026-10-05-overnight-session.md).
- [Previous positive-entry research](combat-positive-entry-feasibility.md);
  [019E0 contract](../specs/combat-positive-entry-v1.md), ordered inventory, oracle and fixture;
  [019E1 evidence](../work/handoffs/2026-10-05-native-positive-entry.md).
- [C3a contract](../specs/combat-selection-steps-v1.md), inventory and oracle;
  [DES003](../design/combat-step-transitions-v1.md);
  [Round2](../specs/combat-sealed-round-v2.md) and [Result2](../specs/combat-result-settlement-v2.md).
- Native CampaignCombatPositiveEntry/Codec, CampaignCombatSelectionSteps/Codec,
  CampaignCombatCertification.CertifyInitialProfileFacts, CampaignCombatSealedRound.AuthenticateBase;
  CombatPositiveEntryTests and Core test project.

## Actual source facts and exact mapping

**Fact F1 / Observation O1:** both pinned Normal/NONE openings replay Request/Created11, four
preamble, one Weather, four stage-entry, one Reserve completion, and two actual entry completions.
The entry oracle independently reconstructs those sources and matches every literal proof;
the focused native tests compare source/proof/event bytes for both owners and every entry cut.

Shared creationBinding is
`creation.c228f73a59deeb83e45d94e240f42b8fd1d24020d4a90c5fe29e9666216f90d9`;
creationEventHash is
`sha256:fc41759ff3220519ce4cfee40b7662eaa4d4f07ce9d788563e90d8dbbfe68ad8`.
Request seed is1/cursor0. Actual Weather consumes two bytes and leaves seed1/cursor2; both
applicable Weather kinds are Normal, scope none, with an empty affected-area set.
Weather determiningSide remains Axis even in the Commonwealth-first order source.
The selected actor is derived from the retained order/cycle, never from that Weather field.

| Field | Axis source | Commonwealth source |
| --- | --- | --- |
| Original opening name | normal-act-first-none | normal-act-last-none |
| Final pe sourceHash | sha256:17717ee586b08c7f82c514d3402f3539d047a6d8cfe9e517457599db2f2f1817 | sha256:db0912627c482f3a1d7ab0e63bade0ce433e59dfdc8b25b2cd9b3699314592ca |
| Entry prefix at13 | sha256:4412509a9ce8fd2de3c1a72523c100c3b17ff24acf48e5ea412d7ffa7fbdb210 | sha256:830e3a7537290d36454d786773db6733a2792ff2014e1977e9ba91b8e724709e |
| cycleId | sha256:dfe28ef6295bf7ca7190e7facfd4629cbcba79484792d029ef9de14dace96d76 | sha256:fa826877086915da611f1c9cf24cc91dea084476cef3ceed9a7395afc919db75 |
| Breakdown receipt ID | ibc.5b51672e8e238f240cb8faa5591c9a2108d2ad8ef3dee7bf49e770714ee950b8 | ibc.60b2498db9e4d03aa32918d126b4bb50422d2bb4ccaad3175ebc3f0b07bf31ea |
| Full Breakdown event hash | sha256:09ec862e5e0716702eed41ecb54121f2557d7bbe05f445e79e7fce245e389ec8 | sha256:6dd578a9df98b834ca81add16cd1c4955c6976370d6c5dd5902c04136f500742 |
| Weather receipt ID | wth.30406522aaf5fabb1648c4a64f6e103532fa888bcae77f4e8c10c653f152f116 | wth.d28c1cfa4cc68c4b59f2251bf2d9e7ff2422e6aee1a215e26d766a59015ade77 |
| Full Weather event hash | sha256:ccc0d5d277149f991cec9b12b35a9180b5212be8b62493324df0234b02844e83 | sha256:51495df4db64ceaad4af84d049579c67d792f3c4c510914d553a12a988e8f5b6 |
| Canonical entry-source size | 35,523 bytes | 35,554 bytes |

These pe source hashes use the existing domain plus NUL plus full canonical PositiveSource,
including the two suffix events. The opening-admission pins instead hash the packet with an empty
entry suffix. Those two hash meanings must not be conflated.

**Fact F2:** actual cycle retains turn1/stage1/first-acting-side/ordinal1/openedAuthorityVersion11,
its own original openingPrefix, admitted Config1 hash
`sha256:3de30c451ba7e81d4fdde6493e07d4d06110209a89035d9e59bba738f0aeba34`,
and resolved first acting side. Current priorVersion13/prefix are later segment facts, not the cycle
opening version/prefix. Position is catalog5 Position Determination with activeSide=null.
World is unchanged from that actual Created11, CP0/0, Cohesion0/0, ammunition10/10, TOE10/10,
ordinary NONE, full original component/representation provenance and adjacent original positions.
Existing synthetic content/resource-origin labels remain unchanged; actual event replay does not
turn those source labels into published scenario evidence.

Actual Reserve, Movement and Breakdown receipt IDs are distinct. The original MovementEndProof
contains full sorted original-unit locations, excludedBefore=[], actual iml receipt, scope and
ordinal1. Preserve this full proof and original predecessor histories through selection.
The bridge neither reconstructs it from a later World nor substitutes the rc receipt.

## Compatibility failures and forbidden substitutions

**Fact F3 / Observation O2:** C3a ValidateBoundary pins the seed0 creation
`creation.dc1c1bff9db6122758ab2b06360ba2131231fe2b63871780e416c8fd6ba4490b`.
Its oracle derives the same retained request, checks seed against that request, materializes
Position.activeSide=cycle owner, and types completedBreakdownReceipt as a sha256 hash.
Its supplied weatherReceiptHash is caller-trusted evidence; shape-valid replacement with an
unbacked all-zero hash still passes the isolated Boundary validator. This is its documented
trust boundary, not authentication of actual Weather.

Independent probes reproduce seed1 rejection, catalog-null-position rejection and receipt-ID in
hash-field rejection; actual source boundaries reject CMB-STP-004. Native source predicates confirm
the same incompatible creation and owner-materialized position assumptions. Round2 read_base
calls the old C3a authenticated reader; native AuthenticateBase calls old ReplayTrustedBoundary
and ValidateBoundary. Result2 verify_context calls Round2 read_base and complete committed replay.
Actual-source scratch handoffs therefore reject CMB-RND2-004 and CMB-RES2-004.

| Origin | Required new mapping | Forbidden shortcut |
| --- | --- | --- |
| Request/Created | Retain exact seed1 Request, independently retained Created11 and creation IDs | Rewrite seed/binding to seed0 or use another Created event |
| World/candidate | Replay019E1; use its complete candidate and actual unchanged World | CP/cursor substitution to select a synthetic Result2 branch; facts certification alone as history proof |
| Weather | Derive kinds/scope from replay; retain both actual wth ID and full event hash | Caller-provided Normal, an invented hash, receipt suffix treated as event hash |
| Breakdown | Retain actual ibc ID and SHA256 of the full accepted event | Put ibc into a hash field, strip its prefix or substitute iml/rc |
| Position/owner | Keep actual catalog activeSide=null; derive actor from retained relative slot/order/cycle | Mutate authoritative catalog position to satisfy legacy C3a |
| Version/prefix/cycle | Start13/current entry prefix; keep original opening11/prefix/ordinal1 | Borrow synthetic version, cycle identity or opening prefix |
| Clock | First real selection opening supplies trusted instant; fixed Config1 budgets | Derive wall time from source hash, invent a deadline at entry, carry a private opponent-seal high-water |
| Trusted inputs | Independently authenticated/retained ledger separate from event payload | Treat an event's self-asserted actor/time as authentication |

A legacy-shaped scratch projection uses the actual full Breakdown event hash in its hash field.
It is not a receipt-ID conversion or an admitted boundary. The recommended new family stores both
values explicitly and never calls the legacy admission reader.

## Controlled causal probe

**Observation O3:** the retained author appendix contains the exact temporary Python probe and
deterministic stdout. It reconstructs authentic source packets with unchanged019E0, compares literal
source/proof bytes, then calls the existing pure selection transition kernel directly.
It intentionally does not call C3a boundary/replay admission for those mechanics. Actual catalog
activeSide remains null in the probe. C3a event framing used by the probe is scratch evidence,
not proposed new goldens; new framing will produce different segment/receipt/event/prefix bytes.

| New causal record | Trusted actor | Version | Position/outcome |
| --- | --- | --- | --- |
| open-segment, trusted1000 | System |13→14 | Position; selection pending, owner=actual phasing side |
| choose-selection,1100 | Phasing side |14→15 | exact candidate selected; no charge/RNG |
| complete Position | System, null time/available |15→16 | Barrage, no-gun-positions |
| complete Barrage | System, null time/available |16→17 | RBA, no-barrage-work |
| open-rba,2000 | System |17→18 | defender window |
| decline-rba,2100 | Opposing original side |18→19 | exact defender UnitKey and selection receipt |
| complete RBA | System, null time/available |19→20 | Force Assignment, accepted-decline, three ordered step receipts |

Config1 selection opens1000/deadline31000/highWater1100; RBA opens2000/deadline32000/highWater2100.
These are simulated independently supplied trusted instants, not live-host evidence.
No clock existed in the two untimed entry commands. Selection and RBA preserve C3a's fixed
30,000ms budgets, exclusive deadline, within-window regression/unavailability semantics and
RBA opening not earlier than accepted selection high-water. No Round2 clock binding is introduced.

At20 selected intent/decline exist, stepIndex3, closed=false. World/resources/RNG remain identical
to the authenticated entry. There are seven new selection receipts and the twelve entry receipts
remain retained; no cumulative history is truncated. Positive FA completion rejects CMB-STP-007.

Executed probe totals: six entry cuts,24 rejection attempts involving original history/admission,
16 positive selection cuts,56 command/actor retry checks,22 clock/actor/positive-FA rejects,
14 locally re-signed event forgeries,14 noncanonical-event rejects,14 fallback traces and three
independent mapping rejects, plus eight re-signed event actor/time claims against an unchanged
trusted input ledger. Those are bounded probe counts, not the future contract's acceptance
matrix. Each cut is reconstructed from the same independently trusted source plus input/event
prefix; exact retry finds the original receipt and leaves the cut byte-equal. Original event
bytes remain in the ledger. Changed consumed event/input and full proof attacks still belong to
the new executable contract.

**Observation O4:** explicit finish, selection expiry, unavailable opening, and unavailable live
selection each reach no-selection Reserve Release at21 with8 new receipts. RBA expiry/unavailable
live reaches cancelled Release23 with10; pre-window RBA unavailability reaches cancelled Release22
with9. No branch fabricates an accepted decline or a committed attack. Both owners behave alike.
These fallback variants are necessary acceptance cases, not optional later gameplay.

A shortened entry suffix is a valid recovery cut at11/12 with null candidate. Its entry reader
must continue to admit that cut. New selection admission must reject it as premature.
Missing/reordered original history, duplicate suffix, foreign full source and hash-only completed
cache are separate malformed/unsupported-source cases.

## Who authenticates the selection input ledger

**Fact F4:** current C3a `Input` is trusted admission evidence, not an authentication protocol.
`trusted()` in its oracle only constructs a dictionary. Native
`CampaignCombatSelectionStepsCodec.ReadInput` expressly decodes without authenticating actor/time;
`ApplyTrustedBoundary` and `ReplayTrustedBoundary` require those values from the authority caller.
The current actual019E1 reader authenticates the two pinned entry histories but supplies no
selection action ingress or selection ledger. **No production component currently authenticates
an actual-selection ledger**, and no live seat/clock/persistence-authentication implementation is
claimed by this research.

| Stage | Who supplies or verifies it | What that proves |
| --- | --- | --- |
| Actual entry source | Unchanged019E1 full replay and exact original-source pins | Narrow entry provenance, owner/order/cycle/Weather/candidate at13 |
| Scratch selection ledger | This research's controlled harness separately supplies System/phasing/defender inputs and simulated timestamps | Mechanics under the stated trusted-input precondition; not real seat/clock authentication |
| Proposed dormant native apply/replay | An authoritative Core caller must independently supply and own the ledger; new reader validates shape, ownership, capability, clock policy and exact event equality | Causal consistency relative to trusted inputs; an event's embedded input cannot establish that trust |
| Future public action/clock ingress | Not implemented for this source. Host/seat-controller integration must authenticate seat-to-side identity, independently obtain trusted clock confidence and retain authority-approved inputs before invoking Core | Required before live admission; reserved public/host/persistence gate, not a property of JSON actor/time fields |

The proposed API must preserve this separation: `ReplayTrustedSource(source, trustedInputs)`,
`ApplyTrustedSource(source, trustedHistoryInputs, trustedCurrentInput)` and proof readback all
receive the ledger independently of event bytes. A syntax decoder must never rename raw decoded
JSON into authenticated input merely because actor/UTC values are well-formed. Persisted replay
needs an independently trusted retained ledger; authenticating an Archives store/restart or public
caller is a later integration obligation. Do not introduce a byte-only read_source/read_proof API
that invents trusted inputs from retained events.

**Observation O6:** eight new probe cases change event-embedded actor or admittedAt at selection/
decline, locally recompute the receipt, and replay against the unchanged separately supplied
trusted input. All reject. Thus local signing and actor/time claims cannot replace that ledger.
This proves consistency, not that the harness has authenticated a real human. If an attacker can
replace the alleged trusted ledger with another valid coherent ledger, the authentication
precondition has been violated; internal causal replay cannot distinguish that from an authorized
alternative action history. Public/host authentication remains an explicit unknown and NO-GO
for activation. The recommended S3/S4 scope is a private dormant consumer under the same explicit
authority-caller trust model as C3a, rather than a new authentication subsystem.



## Alternatives

| Alternative | Evidence, cost and failure mode | Disposition |
| --- | --- | --- |
| Pass actual entry straight into C3a/Round2 | Incompatible creation/seed/position/receipt grammar; current readers reject | NO-GO |
| Widen old C3a gates or refresh old pins | Would change frozen synthetic trust and recursive consumers; disallowed in this research | NO-GO |
| New private actual-selection reader, positive stop atFA and existing fallback traversals | Full actual source already admitted; bounded seven-event mechanism; two new native files plus tests/link/plan | Recommend, conditional contract-first GO |
| Opening-only actual selection | One event, but no actual selected/declined consumer handoff | Smaller fallback scope only if coordinator explicitly chooses less useful endpoint |
| One actual selection+Round2+Result2 adapter | Additional prepared/commit/result contracts, World/RNG effects, independent clocks and legacy authenticity dependencies | NO-GO for this slice; separate downstream gates |
| New route/Reaction/released-I source | Adds gameplay/history and consumes a different provenance profile | Outside minimal two-source bridge |

**Inference I1:** frozen per-family selection mechanics in two new native files are more reversible
than broadening historical admission or extracting shared kernels across frozen readers.
Native C3a Transition is private; no legal public pure-kernel entry currently exists. Therefore the
native bridge cannot simply call ApplyTrustedBoundary. It must implement the reviewed bounded
mechanism in its own family, using unchanged syntax/facts helpers only where compatible.
That duplication has review/maintenance cost: behavior parity must be explicit for both owners,
error order, canonical arrays, all fallback branches and the positive FA stop.

## Proposed exact acceptance boundary

These are proposed field inventories to be frozen by S3 after independent review, not newly
accepted bytes. Every projection below is derived after full019E1 replay.

- ActualSelectionSource order: contractVersion:int, positiveEntrySourceCanonicalUtf8:utf8,
  selectionEventCanonicalUtf8:utf8[]. Version1. Full original entry source is mandatory.
  Accepted selection input ledger is an independently trusted separate replay argument, owned
  and canonicalized before use, with exactly one input per retained event. Embedded event input
  must equal that trusted value. Fixture retains both input and event bytes.
- ActualSelectionBoundary order: contractVersion:int, positiveEntrySourceId:id,
  positiveEntrySourceHash:hash, creationBinding:id, creationEventHash:hash, cycle:Authority,
  cycleId:hash, firstActingSide:side, priorVersion:long, priorPrefix:hash,
  reserveCompletionReceiptId:id, movementCompletionReceiptId:id,
  breakdownCompletionReceiptId:id, breakdownCompletionEventHash:hash, position:Position,
  world:World, randomState:Random, weather:ActualApplicableWeather, breakdownFlow:Idle,
  reactionWindow:null, movementEnd:MovementEndProof, candidate:Candidate. Version1.
- ActualApplicableWeather order: gameTurn:int, operationStage:int, attackerKind:id,
  defenderKind:id, weatherReceiptId:id, weatherEventHash:hash.
- Command/Input, candidate, timing, window, closed effect arms and ordered Control fields retain
  C3a's compatible grammar/semantics, including explicit nulls and commandVersion1.
  SelectionEvent order: contractVersion:int, eventType:id, campaignId:id,
  rulesetHash:rawHash, configurationHash:hash, positiveEntrySourceHash:hash, cycleId:hash,
  segmentId:id, priorVersion:long, stateVersion:long, priorPrefix:hash, input:Input,
  effect:Effect, receiptId:id. Version1; eventType=actual-combat- plus the existing closed effect kind.
- ActualSelectionProof order: contractVersion:int, sourceId:id, sourceHash:hash,
  positiveEntryProof:PositiveEntryProof, boundary:ActualSelectionBoundary, control:Control.
  Version1; readback requires full source plus independently trusted input ledger and exact replay.

Define D(domain,value)=SHA256(ASCII domain, NUL, canonical JSON). Source uses
sandtable.combat.actual-selection.source.v1, sourceId=asrc.+digest and sourceHash=sha256:+digest,
over full ordered ActualSelectionSource including every accepted event. Boundary hash is ordinary
SHA256 of complete new boundary; segment uses sandtable.combat.actual-selection.segment.v1,
segmentId=asg.+digest. Decisions append .selection/.rba. Receipt uses
sandtable.combat.actual-selection.receipt.v1, receiptId=asc.+digest over complete SelectionEvent
with final receiptId omitted. New event includes prior prefix only; fold existing Sequence framing
over complete accepted bytes afterward. Source digest is a cache binding, never authority without
the independently trusted inputs and replay. No digest includes itself.

Historical event reader rejects the extra source field/new tag; old control readback cannot
authenticate the new segment/boundary/events. A new source reader rejects old synthetic C3a,
Round2 or Result2 bytes, mixed families, digest-only cache and source-less control. Use explicit
private CMB-ASE errors with oracle-frozen ordering, preserving no-op/duplicate distinctions.
This does not allocate outward error codes or revise protobuf/public schemas.

## Required RED/GREEN, history, forgery and privacy evidence

S3 must start with semantic expectations for both actual sources before literal golden freeze:
pending selection14, selected15, actual defender decline19 and positive FA20; World/RNG byte
preservation; all no-selection/cancellation variants to their single Reserve Release successor.
S4 must independently reproduce those commands/events/proofs byte-for-byte.

Required verification, with no generated-in-place repair or fixture admission:

1. Full original Request/Created/history/source pins; six legal entry cuts retained. Selection
   rejects11/12 premature cuts, all missing/reordered/duplicate/foreign/re-signed entry sources,
   hidden substituted World/weather/cycle/owner, borrowed receipts and synthetic settled contexts.
2. Every accepted selection state/event cut and suffix replay, both owners, all fallback branches;
   original-command/actor retry after later steps/terminal outcomes returns original owned event
   bytes before clock/status gates. Entry exact-entire-input retry semantics remain unchanged.
   Selection preserves C3a command+actor retry identity; valid changed trusted time alone does
   not renew a window or rewrite original evidence. Invalid primitives still reject first.
3. Trusted input ledger count/order and owned copies; event-embedded actor/time does not authenticate
   itself. Mutate each event/effect/input/proof/receipt leaf and locally recompute hashes.
   Event-input tampering with unchanged trusted ledger must reject; changing alleged trusted ledger
   crosses authentication ingress and cannot be treated as a public byte-reader capability.
4. Wrong actor/participant/candidate/segment/cycle/stale structural version must reject before any
   clock-driven fallback. Test null/unavailable/below-floor/equality/overflow/invalid primitive time,
   before-window RBA failure, stale System timers, duplicate receipts and no-op byte preservation.
5. Preserve catalog position and causal step order; semantic history/route arrays never sort.
   Identity arrays retain their prior ordering rules. Explicit nulls, property order, duplicate/
   unknown keys, alternate escaping, whitespace/BOM/floats/bool-as-int reject.1MiB per packet/
   proof/record, depth32, arrays512, selection input/event/receipt16, checked Int64 version.
   Preserve twelve entry plus all new selection records; no cumulative truncation.
6. Privacy boundary: new source/proof/control remain private Core authority, never intelligence,
   observation, public action, transport or Snapshot payloads. Reject smuggled public/projection/
   provider fields in closed private records; opponent actor cannot select or decline as the other
   seat. No allocation/seal exists in this slice. Round2 hidden-seal clock equivalence and full
   A2 outward privacy are subsequent gates, not proved by private owner checks here.
7. Immutable capture of original source/Created/events/input/candidate/component arrays. Mutating
   caller buffers or returned projections cannot alter replay, retries or later proof reads.
8. Run unchanged predecessor oracles with honest failures, new oracle, build/focused/Boundary/full/
   format and fresh independent review. Coordinator serializes full-suite work; require exact-head
   CI before merge. Administrative prose has no fabricated TDD claim.

Unknown U1: new event/proof byte parity, complete adversarial error ordering, full capacity closure
and native duplication cost remain S3/S4 proof obligations. The scratch probe alone is insufficient.

## S1 evidence dependency and implementation manifests

**Observation O5:** unchanged Breakdown oracle fails on
src/Cna.Core/Rules/Cna1979LandSequence.cs source drift at this exact base. Expected source SHA256 is
c5426245a156367eac85fc5e61792ece1ae124651a0416793fde00213641b938; current observed
SHA256 is d019a3bc2941ad788b69550f5d7fc01e33fa586a0bad6a374c08550b3cb6a79a. No pin changed here.
Entry/C3a/Round2/Result2 original oracle commands all pass separately. They do not waive that failure.

The bridge's runtime source replay uses019E1's current actual admission and does not execute the
Breakdown verifier's main source-pin gate. Native21/21 and actual entry oracle demonstrate this
narrow separation. Its retained evidence still imports/references the predecessor stack; a future
new fixture's sourceHashes must name the exact coordinator-approved predecessor bytes.
S1's recursive fixture maintenance may change hash-bearing fixture bytes while keeping authority
strings identical. Do not freeze S3 pins before that disposition; do not widen an old runtime gate
to solve an evidence-pin mismatch.

Coordinator reports S1a's preliminary recommendation is to defer broad maintenance because
the closure reaches privateExercise-derived identities beyond three files; its review is pending.
This is planning context, not an accepted waiver or final S3 dependency disposition.
Coordinator must reconcile S1a's reviewed complete closure before dispatch:
if maintenance is approved, freeze its merged predecessor head/hashes and re-execute relevant
original/new oracles; if deferred, explicitly record the failed original command and downstream
evidence consequence. A deferral is not an automatic passing gate. This research does not decide
or edit S1's repair, fixtures or canonical plan.

Proposed S3 contract, five primary paths:

1. docs/specs/combat-actual-selection-v1.md
2. docs/specs/combat-actual-selection-v1.schema.json
3. docs/specs/fixtures/combat-actual-selection-v1.json
4. docs/specs/verify-combat-actual-selection-v1.py
5. docs/design/combat-cycle-implementation-plan.md

Proposed S4 native adapter, five primary paths:

1. src/Cna.Core/Campaigns/CampaignCombatActualSelection.cs — owned source/models and replay/apply.
2. src/Cna.Core/Campaigns/CampaignCombatActualSelectionCodec.cs — new closed grammar/proof readback.
3. tests/Cna.Core.Tests/Campaigns/CombatActualSelectionTests.cs
4. tests/Cna.Core.Tests/Cna.Core.Tests.csproj — new fixture link only.
5. docs/design/combat-cycle-implementation-plan.md

Dated handoff/bootstrap/author/review evidence may be administrative paths. Canonical plan ownership
stays serialized with the coordinator. Existing README/naming/design architecture remains unchanged;
if implementation changes architecture/setup/names rather than this dormant private admission,
return an explicit documentation/manifest scope gate before expanding the five-file slice.

## Verification, confidence and remaining gates

Executed at the exact source base in the research worktree on macOS arm64, Python3.14.6,
.NET SDK10.0.400 selected by latestFeature roll-forward from global10.0.302, native MTP/xUnit v3:

- Focused native command:
  dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class
  Cna.Core.Tests.Campaigns.CombatPositiveEntryTests /bl:/private/tmp/s2-positive-entry-20261005.binlog.
  PASS21/21,0failed/skipped,13.122s; stdout and binlog hashes in dated handoff.
- python3 -B docs/specs/verify-combat-positive-entry-v1.py: PASS two owners/two events each/
  six proofs; cuts6/retries6/source1410/event394/input258/proof1524/canonical168/capacity26/
  clock156/order14/unsupported16.
- python3 -B docs/specs/verify-combat-selection-steps-v1.py: PASS5literal traces/41cuts/
  246mutations/164raw rejects.
- python3 -B docs/specs/verify-combat-sealed-round-v2.py: PASS12semantic groups/10traces/
  68cuts/610mutations/340raw/288clock comparisons-retries/480lifecycle retries/30invalid proposals.
- python3 -B docs/specs/verify-combat-result-settlement-v2.py: PASS10semantic groups/32traces/
  304cuts/3728mutations/1360raw/384timing/200same-owner clock comparisons.
- python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py: FAIL exit1,
  original sequence-source pin drift; unchanged and not waived.
- python3 -B /private/tmp/s2-bridge-probe.py <exact worktree>: PASS controlled mechanics and
  rejection matrix described above. Exact durable script/stdout in author packet.

No full repository gate, new executable contract, new native selection tests, public privacy proof,
CI or independent verdict is claimed here. High confidence in existing-source incompatibility and
the bounded selection mechanics; medium confidence in five-file implementation fit until S3's
executable acceptance is reviewed. Reconsider GO if preserving ownership/full-history/canonical
semantics requires old admission changes or another primary file.

Next gates: independently reviewed S2 plus explicit S1 disposition → S3 actual-selection executable
contract → independent review/exact CI/merge → S4 private native consumer → independent review/
exact CI/merge. Then separately research/freeze actual Round admission over the new full selected
source, preserve Round2 public-opening-floor clock semantics and allocation privacy, prove actual
commitment/RNG/resource effects, and define actual result/settlement provenance. Result2's32
synthetic branches, settled-control evidence and its earlier source remain synthetic. Actual result
to Reserve/cycle/repeat, later-II/consumed, full Snapshot/A2/public/host/restart/Runner and an authentic
start-to-repeat-or-finish demonstration are subsequent roadmap gates.
