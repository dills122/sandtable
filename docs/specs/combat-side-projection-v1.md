# Combat side projection v1

Status: `CMB-TASK-004A1` **corrected-profile accepted checkpoint**; contract evidence only. Shared
codec, selection, RBA and sealed assignment use the accepted versioned clock correction below.
`004A2` settlement and `004A3a` live Reserve/cycle projection are **accepted checkpoints** below.
`004A3b` complete Task003 terminal handoff and corrected bridge projection remain open. This packet
does not freeze complete CON-005, close parent004 or activate runtime.

[Combined plan](../design/combat-cycle-implementation-plan.md),
[ordered schema](combat-side-projection-v1.schema.json),
[retained vectors](fixtures/combat-side-projection-v1.json), and
[oracle](verify-combat-side-projection-v1.py) define this bounded checkpoint.

## Clock correction and preserved historical counterexample

The [owner disposition](../design/combat-cycle-policy-reconciliation.md#clock-privacy-correction--owner-decision-2026-09-15)
keeps strict equal-outcome privacy and selects the accepted
[sealed-round v2](combat-sealed-round-v2.md) successor. Its public opening instant is an immutable
regression floor; private accepted seal times never raise it. Opening3000, hidden seal4000 and
own proposal3500 now yield the same accepted public receipt before and after the opposite seal,
for either acting side and either seal order. Available times below opening, unavailable confidence,
null time and deadline equality retain their deterministic authority outcomes and common outward
rejection. Exact own accepted proposals recover the same receipt before clock handling. Opening is derivable
from published deadline minus the approved fixed30000ms budget; no hidden timestamp enters that bound.

Historical v1 remains unchanged. The
[original diagnostic](../../.planning/2026-09-14-overnight-combat-wave-01/evidence/clock-high-water-counterexample.py)
still uses original source names and `rnd=v1`; expected exit1 retains both accepted/rejected
counterexamples with high-water3000 versus4000. Its failure is historical evidence, not a claim
that the corrected profile passes through the old authority path. No privacy guarantee is made
for the legacy diagnostic admission path under clock regression.

`submit` remains an offline proposal oracle for historical or corrected named sources.
`admit_current` requires a separately authenticated corrected source profile before invoking that
oracle; every original legacy profile rejects through this entry point. Neither helper is a
production host registration. Current-profile test results and prospective A1 admission claims
apply only to corrected profiles. Runtime and remaining CON-005 families still require later work.

## Accepted A1 authority and exact admission boundary

Governing decisions are [DES-001 identity](../design/combat-opportunity-identity-v1.md),
[DES-002 protocol](../design/combat-sealed-decision-protocol-v1.md),
[DES-003 steps](../design/combat-step-transitions-v1.md),
[DES-004 resolution](../design/combat-cost-resolution-order-v1.md),
[DES-005 disclosure](../design/combat-settlement-disclosure-v1.md), and
[CYCLE-DES-001](../design/continual-cycle-reserve-composition-v1.md), under accepted
[POL-001–008](../design/combat-cycle-policy-reconciliation.md). No gameplay or source-policy choice
changes here. POL-006 permits apparent target geometry, own facts and phase progress, including
assignment implying RBA decline. It does not permit enemy resources, hidden cause or slot counts.

The executable corpus has two explicit source families, each observed by Axis and Commonwealth:

- Nine original legacy sources: five frozen [C3a selection/step](combat-selection-steps-v1.md)
  transcripts and four [C3b round](combat-sealed-round-v1.md) transcripts. Their names, five-field
  Source shape, authority bytes and all18 retained audience traces are preserved exactly.
- Twenty corrected sources named `clock-v2.<actingSide>.<case>`. Ten selection/step sources mirror
  each of the five C3a cases for both acting sides; ten round sources consume the accepted v2
  fixture's exact authenticated inputs/events. Mirroring changes phasing identity, selected
  participants and corresponding ordinary CP values; the no-candidate actor still has spentCP6.
  These are explicit synthetic bounded vectors, not additional certified gameplay profiles.

Internal family tags are `steps`, `round`, `steps-clock-v2`, and `round-clock-v2`. Trusted registry
membership selects the reader; a caller cannot select a different codec by editing a family tag.
Source keeps exactly `{name,family,base,inputs,events}`. Inputs may stop at any exact retained prefix.
Base and structured input comparisons preserve integer/boolean/float distinctions through encoded
bytes; events must be exact bytes. Each event also passes its predecessor reader. Reader caches
are keyed only after this authentication and return copies, never caller-mutable shared state.
Historical regenerated events/state must match historical goldens; corrected rounds use v2 replay.

Each corrected round inherits its acting-side-matched `clock-v2.<actingSide>.accepted-decline`
projection, including exact context, history, visible revision and own receipts. Corrected public
clock policy enters configRef at the first selection frame, so it never rotates silently at the
Force Assignment handoff. Original and corrected profiles have different public config references.

This is synthetic C3 lineage. C3a's trusted boundary/Weather/Breakdown probes do not become genuine
creation-to-Combat history through this projection. `Source` is internal experiment context, not an
outward wire record or production authentication API. Named corpus admission cannot validate an
unseen otherwise-legal history. Later production must authenticate Chronicle and controller inputs,
replay full causal history, certify the complete admitted profile, and derive its private mapping.
A caller-supplied audience string, snapshot hash or event with its own alleged actor proves nothing.

[Task003 composition](combat-authority-composition-v1.md) remains byte-preserved and hash-pinned.
Its 28 creation-rooted traces and exact handoff are not replaced by these positive synthetic traces.
A3 must consume the handoff and combine both evidence families without widening certified gameplay.

## Closed outward records

The schema is an ordered field/type descriptor, not JSON Schema. All listed fields are mandatory;
nullable fields contain explicit null. The following field allowlist is complete for A1:

| Record | Meaning |
| --- | --- |
| `Context` | Campaign, authenticated audience, visible turn/stage/slot/phasing side/ordinal, approved rules and public timing references. |
| `OwnParticipant` | One stable own participant/component reference; own location, current TOE, spent CP, ammunition and Cohesion. No original authority creation key or provenance receipt. |
| `ApparentEnemy` | One approved apparent location and target reference. No enemy unit/component key, exact strength, resources or number of hidden defenders. |
| `Observation` | Context, canonical public cycle, current structural position, audience revision, own/apparent facts, nullable own round/decision, generic status, retained own receipts and side progress history. |
| `Decision` | Own decision kind, opening audience revision, nullable own slot reference, published deadline, exact action-set reference and ordered own actions. No internal high-water or timing config object. |
| `OwnReceipt` | Original public decision/action references and public acceptance receipt. No authority receipt, accepted event version, timestamp, prefix or cancellation reason. |
| `SideChange` | Audience revision, structural position, generic status and nullable newly accepted own receipt reference. Compact progress log; it is not an authoritative event stream or full state reconstruction format. |
| `Submission` | Explicit version, campaign, approved rules/config references, audience, nullable round/slot, decision, opening revision, set/action and typed candidate. |
| `Outcome` | Exactly version1, `accepted` with own receipt, or `rejected` with null receipt. No diagnostic text, JSON path, provider detail or hidden failure category. |

Core/player/model/side Runner/War Diary consumers must use these typed projections. Trusted raw
Exercise evidence remains private. No arbitrary dictionary, narrative field, passthrough raw event,
seat-independent spectator or authority audit audience is admitted by this codec.

### Closed decisions and statuses

| Window / audience | Exact candidates in semantic order |
| --- | --- |
| Selection / phasing side | `select-close-assault` with exact own participant, apparent target and target location; then `finish-without-attack` with no extra fields. |
| Selection / other side | No decision; generic waiting. Private selection acceptance does not advance that side. |
| RBA / selected defender | `decline-retreat-before-assault` with exact own participant. No actual retreat route or pass alias. |
| RBA / attacker | No decision; generic waiting. |
| Assignment / each unsealed owner | `full-close-assault` with exact own participant, own component and `committedToe=10`. No edited quantity, partial assignment or opponent allocation. |
| Assignment / sealed owner | `own-choice-sealed` and retained own receipt, with no action or remaining-opponent count. |
| Prepared / both owners | Preserve each owner's sealed view until approved structural progress changes it; the second private seal does not announce preparation. |
| Cancelled/closed | Generic `closed`, no timed-out party, internal reason or outstanding count. Accepted own receipts remain recoverable. |
| Structural progress | Exact catalog positions; System-only completions never become player candidates. Non-choice work projects `waiting`. |

No-candidate and voluntary no-attack histories traverse all six steps. Positive A1 ends at the
existing commitment cut; result, retreat, custody, relationships and final settlement disclosure
remain A2. Reaching Reserve Release does not offer release or cycle actions in A1. Unsupported
histories reject; they never become a successful empty candidate set.

## Revisions, references and canonical bytes

Audience revision starts at0 at the admitted C3a boundary. Every accepted event that changes the
allowlisted facts or adds an accepted own receipt advances it once. Events that change no
allowlisted facts leave the entire observation and side-history bytes identical. No subtraction
from authority version, hidden-count offset, empty numbered side event or new round-local counter
is allowed. The round inherits the exact terminal C3a observation before its first event.

Decision identity retains its opening audience revision. An opponent's seal cannot rotate or stale
that decision, action set, slot, round, candidate or own submission. Acceptance consumes only the
owner's action set. Readback keeps the original own public receipt even after steps, commitment or
cancellation; an altered payload cannot replace it.

Canonical values are ASCII JSON in inventory property order, with no whitespace, BOM, newline,
duplicate/unknown/missing fields, alternate escapes, floats, exponent notation or repair. Integers
must be ordinary integers, never booleans. UTF-8 outside ASCII rejects in this bounded family.
Arrays preserve semantic order, with duplicate/action-membership checks supplied by exact replay.
The independent literal assignment candidate vectors pin both owners' bytes in the oracle.

For combat-local references:

```text
pub.<lowercase hex SHA256(ASCII(domain) || 0x00 || canonical payload)>
```

Exact domains appear in the inventory. Preimages are closed:

| Domain suffix | Canonical payload |
| --- | --- |
| `participant`, `component` | `UnitSeed`: campaign, audience, own original element ID, null or exact own component ID. No mutable location/version or authority creation hash. |
| `target` | `{context:Context,locationId:id}` in that order. The admitted singleton apparent marker has no hidden ID/count salt. |
| `rules` | `{rulesetHash,profile,policy,candidateCodec}` in that order: exact full public Rules10 raw64 identity, `singleton-infantry-close-assault`, `CMB-POL-006`, integer1. |
| `config` | Historical sources retain `{selectionBudgetMilliseconds:30000,rbaBudgetMilliseconds:30000,assignmentBudgetMilliseconds:30000}`. Corrected sources use `ClockConfigSeed`: those three fields followed by `assignmentClockPolicyId:"sandtable.combat.public-opening-clock.v2"`. The schema fixes all values and order; no private configuration hash is exposed. |
| `round`, `decision` | `IdentitySeed`: context, canonical public cycle, position, opening audience revision, decision kind, own participant and apparent target. Round uses `force-assignment`. |
| `slot` | `SlotSeed`: own round, audience and attacker/defender role. |
| `action` | `ActionSeed`: public decision and exact typed candidate. |
| `set` | `SetSeed`: public decision, opening audience revision and ordered actions. |
| `receipt` | `ReceiptSeed`: original public decision and accepted action. |

The canonical public cycle is **not** a combat-local JSON hash. It is the exact frozen
[cycle codec1 Public tuple](combat-cycle-sequence-v1.md), domain `sandtable.cycle.public.v1`,
with `sha256:` display prefix. Preserve full Rules10 raw64 `rulesetHash` in its original field;
never substitute the digest of `rulesRef`. These are the already-approved audience-invariant
sequence facts. Rules10 contains public artifacts/rulings/policies, excluding mutable state and
configuration. Its identity is distinct from private authority state/base/prefix/Setup/Content
hashes, which never enter the outward family.

Limits:65,536 bytes/value, depth16, generic arrays64, at most2 actions,3 own receipts and32 side
changes. IDs are1–128 printable ASCII characters; combat-local refs are `pub.` plus64 lowercase
hex. Revisions fit nonnegative signed64; turn1–111, stage1–3, ordinal1–2,147,483,647; timestamps
0–253,402,300,799,999 milliseconds. This C3 corpus has own TOE/ammo/CP/Cohesion0–10 and full
assignment exactly10. Bounds do not expand actual admission. Overflow rejects rather than truncates.

## Submission and private mapping

Authenticate seat first. Decode exact Submission; require explicit audience and every public
campaign/rules/config/round/slot/decision/revision/set/action binding to equal an actually offered
candidate. Selection/RBA round and slot references are explicit null. Recover an already accepted
exact proposal through its original own receipt; reject any changed payload using that decision.
Otherwise require the current action set still equals the offered set.

The internal mapping consists of the retained source base, complete authenticated prefix and exact
public offered record. It resolves the choice back to original unit/component/target/decision/slot
before invoking the predecessor transition at the current authority cut. Opposing private seals
are permitted by replay; changed frozen World/RNG/base/config or forged receipt/event suffix is not.
Unknown, partial, reordered and foreign source history rejects before declassification.

This oracle adjudicates proposals without retaining side effects; it does not claim a durable
publication API. It proves allowed proposals reach the actual predecessor transition and produce
the required acceptance effect. A clock-loss cancellation is not reported as an accepted seal.
At deadline equality, unaccepted choices reject; an exact previously accepted receipt remains
recoverable after deadline. Existing predecessor authority owns clock/fallback events and RNG.
All outward rejection reasons share identical Outcome bytes.

## A2 successor profile: result and settlement

`004A2` is a separate local version2 codec in this packet. `objects2`, `candidateTags2`,
`integerBounds2`, `enums2`, `domains2` and `limits2` define it; original A1 descriptors, domains,
source names and retained literals remain byte-equivalent. The old decoder rejects every version2
observation/submission/candidate. `admit_a2` rejects A1 and historical profiles; `admit_current`
continues to admit only accepted A1 corrected profiles. Neither entry point is registered in runtime.

### Exact source composition and clock binding

The core corpus contains all32 accepted [Result2](combat-result-settlement-v2.md) traces:
eight retained result branches, both acting sides and both seal orders. Each source carries the
exact authenticated C3a boundary/input/event predecessor, accepted Round2 base/input/event and
committed bytes, then Result2 inputs/events. No creation-rooted positive lineage is inferred.
The first visible frame is the same source's C3a boundary; C3a→Round2 and Round2→Result2 handoffs
add no synthetic event or revision and preserve all earlier history and own receipts. The private
result-resolution event itself leaves both views unchanged.

Source names are `settlement-v2.<result-case>.<acting-side>.<first-seal-role>`. Eight additional
`.prior-time-10001` / `.prior-time-11000` sources replay actual same-owner retreat choices, then
independently open custody at10500 and choose guard at10600. Eight `.fallback-retreat` /
`.fallback-custody` sources replay a valid owner proposal with null time/unavailable confidence
and retain the resulting System fallback. These16 supplemental sources are causal histories,
not altered-state probes. The resulting48 sources yield96 audience traces; core coverage remains
32×2. All names and every allowed prefix are closed in the authenticated registry.

Result2's context is exactly `{base,predecessor,roundInputs,roundEvents,committed}` and passes its
accepted reader before projection. The Source wrapper retains the existing five-field shape;
its `base` holds this context. Exact context/input bytes and exact event byte prefixes select a
registered source. No ignored key, float/bool alias, reordered event, arbitrary extension, foreign
context or independently asserted snapshot becomes trusted history. Caches return copies.

Both corrected policies are present in public `configRef` from the first version2 frame:
`ClockConfigSeed2` has selection, RBA, assignment, retreat and custody budgets of30000ms, then
`assignmentClockPolicyId:"sandtable.combat.public-opening-clock.v2"`,
`resultClockPolicyId:"sandtable.combat.mandatory-window-clock.v2"`, and `candidateCodec:2`.
It uses the new config domain; configRef never rotates at a fragment handoff. Original Config1
budgets/fallbacks and full Rules10 identity remain unchanged trusted evidence. A2 rulesRef uses
its new rules domain and the same public Rules10 identity/profile/POL-006 tuple with candidateCodec2.
The canonical public cycle keeps its original tuple codec and full public Rules10 identity.

Assignment retains Round2's public opening floor. Every new mandatory Result2 window opens from
its independently trusted valid opening instant and checked budget. A previous private accepted
time cannot gate that opening. A live own window uses its own published opening/deadline gate;
null/unavailable/below-opening valid choices cause the unchanged System fallback, while deadline
equality rejects unaccepted choices. Global accepted-time maxima remain private audit evidence.
Own accepted receipts recover before clocks; System fallback receipts never recover as acceptance.

### Closed version2 records and choices

`Observation2`, `Submission2`, `Outcome2`, `Decision2`, `Action2`, `OwnReceipt2` and **every**
Candidate2 arm carry explicit integer `contractVersion:2`. Observation2 adds nullable Settlement2;
Submission2 and Decision2 add nullable own `settlementRef`. The remaining explicit campaign,
rules/config/audience/cycle/round/slot/decision/opening-revision/set/action bindings keep their
meanings. Slot is non-null only for assignment; settlementRef is non-null only for own mandatory
settlement decisions. No private authority version, raw result ID or hidden substage is copied.

| Version2 record | Exact authorized meaning |
| --- | --- |
| `OwnParticipant2` | Existing own fields, with CP represented by numerator/denominator1 and signed Cohesion. |
| `Loss2` | Own component reference, committed/lost/captured/other/remaining TOE. No enemy quantity, percentages, differential or roll. |
| `CohesionCause2` | Own ordered cause reference, `loss-dp`, `retreat-excess-dp` or `assault-victory-rp`, points, own before/after Cohesion and turn/stage. |
| `Retreat2` | Own retreat/refusal intent, ordered route and planned distance; actual completed distance, before/after CP and excess DP appear only when settled. |
| `Custody2` | Captor's own lot reference/count, approved prisoner origin side/class/location, current own custody location/status, own guard reference/route and donor transfer. No opposing original unit/component identity or hash. Escape route is withheld from captor. |
| `Guard2` | Own guard/custody/donor refs, location, one TOE, CPA10, offensive0/defensive1, own CP/Cohesion, ammunition0 and current water/stores/readiness. No initial provenance metadata. |
| `Entitlement2` | Original owner's component/entitlement refs, captured quantity, reunion location, earned scope,12-operation-stage delay, future eligible scope and awaiting-eligibility/training state. No immediate TOE credit. |
| `Obligation2` | Own subject/ref, `guard-priority-upkeep` or `replacement-training`, earned/due scope and game-facing `pending` state. Private activation-gate names and implementation status are excluded. |
| `Relation2` | Original pair's authorized Contact/Engaged kind and apparent target ref. No transitive guard relation. |
| `Settlement2` | Own nullable loss/retreat/custody/relation plus ordered own causes, guards, entitlements and obligations. Absent until an authorized own fact or decision exists. |

| Owner window | Exact semantic candidate order |
| --- | --- |
| Selection | Version2 `select-close-assault`, then `finish-without-attack`, with unchanged gameplay fields. |
| Defender RBA | Version2 `decline-retreat-before-assault`. |
| Each assignment owner | Version2 `full-close-assault`, own component, committedTOE10. |
| Retreat | `retreat` with own participant, exact certified ordered route, required distance1, cost1 and exact resulting excess-CP DP; then `refuse-retreat` with own participant. |
| Custody | `relocate-and-guard` with own custody/donor refs, exact approved route and guardTOE1; then `leave-unguarded` with own custody ref. |

Nonowners receive no private mandatory-window information. Private openings, retreat intention,
guard choice and hidden result resolution produce no empty revision tick. A victim's newly earned
escape/replacement entitlement is an authorized difference. Successful owner-choice effects alone
mint own receipts: C3a owner selection/decline, Round2 `choice-sealed`, or Result2 owner-authored
`owner-choice`. Result2 can retain the initiating owner in its private ledger while authoring the
fallback as System; that case has no public owner acceptance receipt. Conflicting, stale or no-longer
available proposals return only generic version2 rejection with null receipt.

The apparent opposing original participant location follows the established
[observation policy](campaign-observation-v1.md), not a new line-of-sight rule: use its current
approved independent-element representation location. The oracle requires exactly one matching
representation and equality with the original element's current location before declassification.
Own retreat can therefore change the opponent's approved apparent marker. Opposing guard location,
prisoner relocation route and hidden resources remain excluded. Representation/element mismatch
rejects instead of silently choosing one source. This implements DES-005's delegation to the
existing observation policy; the C3 certificate alone does not grant extra disclosure.

### Version2 references, limits and evidence

All new domains use `sandtable.observation.combat.<name>.v2`; exact names live in `domains2`.
Own participant/component aliases retain the existing approved UnitSeed/domain, which contain only
own public identity. New round/decision identities use IdentitySeed2; action/set use ActionSeed2 /
SetSeed2; receipt payload is `{decisionId,actionId}`. Slot payload remains own round/audience/role.
A settlement ref hashes `{context,roundRef,participantRef}`; each singleton custody/guard/entitlement/
obligation/relation ref hashes `{settlementRef,participantRef}` in its own domain. Own cause refs
hash `{settlementRef,ownOrdinal,kind}` with zero-based **own filtered** order. No source name,
raw opposing key, hidden global ordinal, result ID, random cursor, authority hash or timestamp
enters any outward reference preimage.

Version2 preserves the same strict canonical ASCII JSON rule. Limits are65,536 bytes/value,
depth20, generic arrays64, actions2, receipts8, history64 and one own guard/entitlement/obligation.
Retreat routes contain1–2 nodes; custody guard routes at most4; general route capacity9. Actual
retained histories use at most4 own receipts. CP is a nonnegative signed64 numerator with fixed
denominator1; Cohesion ranges from signed32 minimum through10. These are source-native numeric
bounds from the [creation ledger](combat-creation-ledger-v1.md), not fixture maxima. Decoder capacity does
not authorize unseen histories, additional units, Reserve/cycle choices or different gameplay.

The fixture retains the exact A1 root object and adds only `successor2`, with separate Result2
source pins, every composed observation cut's digest/length, representative canonical observations
and every distinct offered Candidate2/Submission2 byte string. The oracle fingerprints the complete
original A1 schema and retained fixture sections. Exact deterministic fixture bytes reject missing
fixtures, booleans/floats substituted for integers, duplicate keys or formatting changes.

A2 checks cover all32 core traces for both audiences, every cut, both seal orders and arbitrary
supported clock classes; real prior-time and fallback histories; current/old/cross-profile rejection;
exact offer membership, context/reference and candidate mutations; recovered receipts after every
later cut; private guard/opening/resolution equality; authorized escape differences; signed numeric,
array/depth/byte bounds; and representation consistency. Literal candidate vectors separately pin
canonical field order. Pure altered-state declassifier checks are labelled non-admission probes;
they do not count as authenticated histories.

No complete CON-005 or parent004 completion follows from A2. Reserve I/laterII, repeat/finish,
complete Task003 handoff, runtime activation and hosted timing/traffic privacy remain deferred.
Future transport must carry the explicit version2 settlement binding in addition to the A1 mapping,
and must select an admitted profile before parsing; this local codec reserves no production field.

## A3a successor profile: inherited Reserve and cycle control

`004A3a` is accepted after 52 oracle groups, fresh ordinary review, source/literal audit and root
`just check` (81 boundary tests, 1670 full tests, zero skipped). It adds disjoint local codec3 under `objects3`, `candidateTags3`, `recordTags3`,
`integerBounds3`, `enums3`, `domains3` and `limits3`. Accepted A1/A2 records, numeric bounds,
references, domains, entry points and fixture sections remain exact and fingerprinted. This
checkpoint covers live inherited release/control and separate canonical ledger behavior.
Complete003 terminal projection and continuous corrected Combat/Result2/finish composition remain
`004A3b`; no complete CON-005 freeze or production registration follows from A3a.

### Explicit capability and represented history

`Context3` adds `capabilityPolicyId`. Four implemented public values identify supported evidence:

| Policy ID | Meaning |
| --- | --- |
| `sandtable.side.inherited-release-I.v3` | Actual inherited release-I owner choice plus deterministic System fallback/completion. |
| `sandtable.side.inherited-release-control.v3` | Same release history followed by actual inherited armed repeat/finish control. |
| `sandtable.side.reserve-ledger.v3` | Standalone canonical Reserve ledger; no fullWorld admission. |
| `sandtable.side.cycle-ledger.v3` | Standalone canonical cycle-control ledger; no fullWorld admission. |

Two additional values, `sandtable.side.corrected-composition.v3` and
`sandtable.side.historical-terminal.v3`, reserve typed-only A3b capability identities. They have no
current source, profile, positive fixture or admission support. Reserving them does not reinterpret
current records or authorize either future adapter.

Both live capabilities begin at accepted inherited Reserve entry, authority22, with audience
revision0, one represented boundary change and no fabricated prior side receipts. Exact inherited
creation provenance authenticates that boundary; it does not reconstruct unrepresented public
history from private authority counts. Control sources retain every represented release frame,
receipt and side change before control opens. Their capability is fixed from that first frame;
source name, private proof hash, event prefix and accepted-time maxima never enter outward identity.

`ClockConfigSeed3` contains reserve and cycle budgets30000, the explicit bounded
`sandtable.side.inherited-retained-clock.v3` policy, capability ID and candidateCodec3. Both budgets
and policy are bound in version3 configRef before any represented event. The inherited clock policy
means exact accepted-reader semantics, including retained high-water behavior; it does not claim
corrected arbitrary-history clock support. Version3 rulesRef binds full public Rules10 identity,
existing singleton profile/POL-006 and candidateCodec3. It carries no private configuration hash.

Ten named sources produce20 audience traces: two inherited release lineages and two real
owner-initiated clock-loss fallback forks; two owners each have repeat, finish and control fallback
histories. Internal five-field Source retains exact case/base, inputs and event bytes. Names are
`inherited-reserve.<side>[.fallback]` and `inherited-control.<side>.<repeat|finish|fallback>`.
Only exact retained prefixes enter `views3`, `read_observation3` or `admit_a3`. Authenticate whole
source and immutable dependency pins before cached readback; cached outputs return copies.
Inherited release uses its wrapper, which rejects owner conversion. Inherited control uses its
certified armed assessment and wrapper, never the private exhausted-ammunition assessment.

`admit_a3` is an offline proposal oracle with generic version3 accepted/rejected outcomes. It
checks seat, codec, full public offer, current set and candidate, then executes the actual wrapper.
Exact accepted proposals recover original own receipt before clock handling; altered or stale
proposals reject. System fallback is not accepted player action. No source caller can gain admission
by relabeling a private ledger or by supplying an allegedly matching checkpoint hash.

### Closed codec3 facts and decisions

Every new candidate, decision, action, receipt, observation and submission carries integer version3.
`Candidate3` has eight version3 Combat arms with the same semantic payloads as A2, plus seven
cycle arms: release-I, convert-to-II, release-II, retain-II, complete-release, repeat and finish.
Combat arms are typed future composition capacity; A3a does not offer them through live admission.
The first four Reserve arms carry only kind, version and own participantRef; completion/repeat/finish
carry only kind and version. No private unit creation key or authority completion identifier enters candidate payloads.

`Decision3`, `Submission3` and `OwnReceipt3` use explicit `decisionFamily` tags: `combat` and
`cycle`. Each variant has a closed descriptor. Combat actions/sets remain `pub.` JSON-domain refs;
cycle actions/sets require frozen `sha256:` binary hashes. No permissive mixed-reference scalar
widens A2. Cycle submissions additionally bind the exact public cycle; Combat submissions retain
explicit nullable round/slot/settlement fields. Unknown families and wrong-family candidates reject.

`Observation3` retains typed own participant, apparent enemy, nullable round/settlement and own
receipts/history; adds nullable `OwnReserve3` and explicit `Lifecycle3`. Own Reserve contains own
participantRef, status, baseCPA, source-native spentCP and nullable release record. A release record
contains released type/ordinal, CPA basis, voluntary ceiling, own offensive-use flag and nullable
next-Movement exception scope/ordinal/status. Only authenticated member records supply these facts.
Raw designation, conversion, release, commitment and completion IDs remain private. Never infer
release merely from status None or manufacture release history when absent.

Lifecycle keeps approved source cycle/ordinal and nullable current cycle/ordinal. Repeat exposes
actual active ordinal2; finish retains source occurrence while current cycle becomes null.
`SideChange3` binds cycleRef and ordinal along with visible revision/position/status/own receipt.
Audience revision advances once per authorized fact or accepted own receipt change. Private release
opening/choice/completion and control opening do not tick the other audience. Adapter handoffs do
not themselves add side history. Structural repeat/finish is public progress.

### Frozen cycle set/action codec

Use exact [binary tuple framing](../design/continual-cycle-reserve-composition-v1.md): U32/U64
unsigned big-endian, S=U32 byte length followed by exact bytes, H=32 raw hash bytes; domain ASCII
plus one zero byte before payload. Cycle public identity remains frozen codec1 with original public
Rules10 raw64 identity. It is independent of version3 Combat JSON domains.

```text
set: sandtable.cycle.actions.v1
U32(1); S(campaignId); S(audience); H(publicCycle); S(windowKind);
U64(openingAudienceRevision); S(capabilityPolicyId); U32(candidateCount);
each S(canonicalCandidateBytes)

action: sandtable.cycle.action.v1
U32(1); H(setDigest); U32(zeroBasedCandidateIndex)
```

Candidates use strict ordered canonical ASCII JSON from their closed Candidate3 descriptors. Sort
as unsigned bytes; reject duplicates before assigning indices. First-I conversion sorts before
release-I. Later-II complete-release sorts before release-II and retain-II. Action IDs are excluded
from candidate payloads. Independent literal vectors pin a three-candidate set digest and all three
index-bound action digests, alongside canonical candidate bytes and field order.

### Separate canonical ledger evidence

`LedgerObservation3` is explicitly separate: context, cycle/position/revision/status, nullable cycle
decision, own Reserve array, lifecycle, receipts and history. It has no World participant, apparent
enemy or settlement record. `ledger_cases3`, `ledger_views3` and `admit_ledger3` authenticate a
separate registry containing all13 Reserve and19 cycle cases, expanded to112 side/slot traces.
Synthetic membership, retained World hashes and Movement certificates remain private ledger evidence;
no ledger source enters fullWorld registry. All actual canonical choices execute through native
transitions and check resulting status/history, receipts, clock fallback, stale proposals and retries.

Later-II `complete-release` is authenticated owner intent whose accepted native completion event
has System author. It counts as own acceptance only after exact native replay and offer matching:
owner command `complete-release`, event `release-completed`, reason `owner-complete-release`, and
fallbackLocked false. Native replay checks optional-II ordinal, bound decision/version, completed
state, completion receipt and owner command receipt. Ordinary System `complete` and clock fallback
completion produce no own acceptance. Exact owner completion retry recovers original public receipt.
The inherited release-I profile offers neither completion intent nor conversion.

### Capacity, compatibility and verification

Version3 limits:65,536 bytes/value, depth20, generic arrays64, actions3, own receipts8 and side
history64. FullWorld retains singleton own member; standalone ledgers allow32 own members. Source-native
CP has nonnegative signed64 numerator and denominator1; Cohesion spans signed32 minimum through10.
Codec probes include CP14/Cohesion−4; actual28 historical terminal vectors remain A3b. Settlement
asset/route bounds and future-turn115 capacity remain unchanged. Excess rejects; never truncate.

Fixture adds only `successor3`, retaining source pins, all live and ledger cut hashes/lengths,
representative canonical observations and every distinct candidate/submission. Focused checks cover
closed versions/families, independent binary literals, all canonical ledger sets, actual wrapper
admission, clock classes, recovery, context/source/raw mutations, hidden-only equality, cache-copy
isolation and bounds. A1/A2 complete schema/fixture fingerprints prevent accidental reinterpretation.

Run the side oracle plus direct inherited release/control and private Reserve/cycle predecessors.
A3b still owns exact28-terminal Task003 handoff projection, continuous corrected bridge history,
combined privacy comparisons and final corpus capacity reconciliation. No earlier accepted byte or
runtime reader is replaced here.

## Compatibility and future Dispatch

Current production contracts remain unchanged: Observation5/6/7; legal action set2 with current
policy `sandtable.legal-actions.v3` and historical v2; candidate/submission/receipt1; projected
history1/2. This packet's local version1 allocates no successor production version, protobuf field,
registry entry, handler, transport capability or migration. Old readers and historical bytes retain
their original meanings; runtime migration/admission/rollback remain later tasks.

Future typed transport must map and echo these bindings explicitly before generated code exists:

| Transport meaning | Outward value / trusted private mapping |
| --- | --- |
| `decision_id` | Public `decisionId`; host maps to exact private selection/RBA/round/slot decision. |
| `state_version` | Decision `openingRevision`, the audience version bound by that offer; never current private authority event count. Current observation revision is separate. |
| Rules binding | Approved exact full Rules10 identity with matching public `rulesRef`; no state/base/history hash. |
| Configuration binding | Public `configRef` for approved budgets/policy only; host retains separate full private configuration identity. |
| Audience | Authenticated controller/seat, checked against explicit audience; never trust payload assertion as authentication. |
| Round/slot/set/candidate | Exact outward fields copied without rebinding; private host map restores original authority references. |

Selection, RBA and assignment remain separate finite decision contexts. Cancellation, deadlines,
deduplication, durable outbox reconciliation and late reply validation stay required in future
Dispatch. Model I/O stays outside authoritative turns. Content-byte equality here does not certify
host timing/traffic privacy, constant latency, restart scheduling or publication atomicity.

## Traceability and verification

| Requirement | A1 evidence | Remaining owner |
| --- | --- | --- |
| CON-005 closed choices/errors/bytes | Four candidate arms, explicit submission context, canonical vectors, common rejection outcome and strict readback mutations | A2/A3 complete remaining arms;020–021 runtime |
| ID-AC-006/009/010; PRO-AC-001/002/003/010 | Public domains/cycle codec, actual opposing seal equality, unchanged success/receipt bytes, changed proposal and authority-source rejection |020–021; hosted traffic deferred |
| STEP-AC-004/005/006/009/010/012 | Five historical and ten corrected selection/step traces, own-only selection/decline receipts, generic closure, all six no-attack positions, inherited C3a→round public history |009–011/020–021 |
| PRO-AC-012 | No production edits;23 source pins preserve predecessor goldens and exact Task003 files |008/020–024 integration and migration |
| Full CON-005; SET/RES/CYCLE projections | Explicitly incomplete in A1 |004A2/004A3; Task004C maps all72 criteria |

Run:

```sh
python3 -B docs/specs/verify-combat-side-projection-v1.py
python3 -B docs/specs/verify-combat-selection-steps-v1.py
python3 -B docs/specs/verify-combat-sealed-round-v1.py
python3 -B docs/specs/verify-combat-sealed-round-v2.py
python3 -B docs/specs/verify-combat-result-settlement-v2.py
python3 -B docs/specs/verify-combat-cycle-sequence-v1.py
```

Default verification requires the retained fixture and exact deterministic UTF-8 file bytes
(two-space JSON indentation and a final newline); it never recreates missing or changed goldens.
Float/integer substitutions, boolean/integer substitutions, duplicate keys and CRLF changes reject.
Every retained observation cut has byte count/digest; representative canonical observations retain
all status/decision shapes. Expected own assignment candidate bytes are also literal independent
vectors. Actual equivalent histories cover both opposing seal orders and empty-versus-private-seal
cancellation. Hidden seed/cursor/identity/resource/prefix perturbations are separately labelled
**declassifier-only non-admission probes**; forged source variants reject and never count as valid
campaign histories. Accepted configuration variants, unseen profiles, settlement and full Task003
projection are not claimed by these probes.
