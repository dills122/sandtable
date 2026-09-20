# Task009B dispatch proposal — research checkpoint, not implementation authorization

Status: research refinement after009A corrected candidate39dccfe; final gate/review3 pending. No production/test edits or .NET runs in this research. Dependency remains009A→009B→010→011. Parent009 remains open until009B accepted.

## Concrete five-path scope

1. `src/Cna.Core/Campaigns/CampaignCombatCertification.cs`: add pure positive-profile fact certification beside actual inherited `Admit`; reuse Content7 validation, existing identity binding and selected Rules definition. Keep actual retained-history admission unchanged and empty.
2. `src/Cna.Core/Campaigns/CampaignCombatIdentityModels.cs`: existing frozen Candidate value only (attacker, defender, targetLocationId, basis); immutable ownership, no invented wire certificate or profile.
3. `src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs`: strict Candidate codec and pure current Round2 opportunity preimage/digest calculator.
4. `tests/Cna.Core.Tests/Campaigns/CombatIdentityTests.cs`: positive fact/geometry/resource/identity proofs and frozen v2 digest vectors; reuse existing exhaustive Rules tests.
5. `tests/Cna.Core.Tests/Cna.Core.Tests.csproj`: link existing sealed-round-v2 fixture as needed, no fixture changes.

This is dormant Core mechanism work, not positive history admission or Task010 controls. Freeze this
internal method on existing `CampaignCombatCertification`:

```csharp
public static CampaignCombatCandidate? CertifyInitialProfileFacts(
    CampaignCombatCreationRequest request,
    ReadOnlySpan<byte> retainedCreated,
    CampaignWorldSnapshotV7 currentWorld,
    CampaignCombatCycleAuthority cycle,
    LandSide firstActingSide,
    WeatherKind attackerApplicableWeather,
    WeatherKind defenderApplicableWeather);
```

`CampaignCombatCandidate` is the existing frozen Candidate shape newly represented as an immutable
Core value: `CampaignCombatParticipant Attacker`, `CampaignCombatParticipant Defender`,
`string TargetLocationId`, `string Basis`. Basis is `voluntary-adjacent`; target must be frozen
Defender.LocationId. The method never returns an admitted opportunity or certificate. Unsupported
facts throw JsonException; supported initial profile with non-Normal applicable Weather or CP over
voluntary ceilings returns null, only after structural/all-result support is established. Success
returns the provisional Candidate used by later trusted selection code.

Provenance contract is explicit and non-transferable:

- Request.Context is independently retained registry/setup/config context, whose constructor already
  validates exact Rules10, Content7 and Setup7/config (`CampaignCombatCreationRequest.cs:11–35`).
  Require supplied Created bytes via `CampaignCreatedV11Serializer.Deserialize(bytes, request):44`;
  reuse returned InitialWorld as exact initial-value/provenance baseline. Never accept caller-created
  context reconstructed from Boundary bytes, synthesize missing Created or trigger fresh creation.
- currentWorld, cycle, firstActingSide and both applicable Weather kinds must be independently trusted
  caller projections. This method checks local consistency; it does **not** authenticate those facts
  by comparing their names or hashes. Caller remains responsible for complete prior history,
  completed Breakdown/no Reaction, current position/version/prefix, retained first-side order and
  Weather receipt/area applicability, exactly as selection-v1:21–31,46–52 requires. Tests using synthetic
  C3a values label them synthetic; passing this API never establishes actual creation-rooted admission.
- Check cycle's campaign/Rules/Setup/Content/scenario/config against request; scope turn1/stage1,
  first-relative slot/ordinal1; ActingSide equals firstActingSide. Reject unknown enum values. Scope
  validation is consistency, not proof of cycle occurrence. Use existing cycle type
  `CampaignCombatReserveCompletionModels.cs:16–19`, not a new boundary/certificate model.
- Use applicable `WeatherKind` arguments deliberately: C3a already defines projected kinds, not a new
  dice result. Valid enum set Normal/Hot/Sandstorm/Rainstorm. No recomputation from fabricated dice;
  caller must derive applicable kinds from validated Weather history and area assignments. Explicit
  documentation says these are trusted inputs, never a user-selected forecast.
- Compare every current World field with trusted initial baseline except each original unit's CP,
  which must be integral0..10 and is retained. Use typed fields/equality; do not call initial-world
  Deserialize on noninitial CP or normalize caller JSON. Exact baseline enforces no relations,
  custody, guards, causes or other outstanding state. Ordinary spending within CPA10 causes no
  extra CP DP. BindParticipant remains separate identity helper.

No current production call site can honestly supply positive actual G2 facts; all four actual009A
terminal histories still return empty from `Admit`. Dormant tests provide substantive positive
runtime proof of this fact predicate;010 will consume it only under its own independently trusted
Boundary. No API claims to verify a complete synthetic or actual selection history.

`combat-selection-steps-v1.md:21–59` defines retained Boundary input, independently trusted caller and initial inventory/positions with current integral CP0..10. All four actual009A G2 terminals are nonadjacent with actor CP12/14, hence cannot supply positive facts. Existing synthetic C3a probes remain explicitly synthetic. No replacement C3a lifecycle parser, invented certificate or second Content authority is proposed.

## Production support proof

Require exact selected Content7/Rules/config, two original independent singleton infantry, original component inventory, full TOE10/Ammo10/provenance/Cohesion0/readiness and permitted ordinary integer CP. Reuse ContentPackV7Validator's exact six-location Clear line, two infantry, endpoints/anchors and no hidden blockers; reject unsupported current memberships, relations, custody, guards and obligations. Bind current representations with009A binder. Positive actor≤5/defender≤7, Normal Weather and adjacency retain actual cumulative CP.

Production geometry checks cover both role assignments, accepted/refused retreat and either captured role: unique one-hex defender retreat farther from attacker toward own anchor; no entered enemy occupant/control; capture origin stays victim pre-retreat location while captor destination uses post-retreat location; bounded relocation≤3 hexes with origin-only exemption; escape reunion≤8CP at surviving original unit. One TOE guard supports≤3 prisoners; original survives≥7 before guard transfer. Guard/new arrival does not inherit original relation membership. See `combat-settlement-disclosure-v1.md:29–43,121–154,223`.

## Finite support enumeration — implementation choice frozen

Build one private immutable result catalogue lazily in `CampaignCombatCertification`, pinned by the
already validated canonical Rules10 selected-input artifact. It is an implementation cache of pure
rules outcomes, never persisted, accepted from caller or treated as authority for world facts.
Store `CombatSelectedResult[]` privately; do not add a new result/certificate wire shape or expose
mutable arrays. Use existing record structural equality for deduplication after complete coverage.

1. Generate ordered d6 coordinates as tens1..6 × ones1..6 (36 values). Visit all1296 morale pairs;
   call `Cna1979CombatAdjudication.CalculateDifferential(am, dm)` and retain first representative pair
   per differential. Require complete supported set{-2,-1,0,1,2}. This establishes finite reduction;
   no seed or cursor participates. Do not hardcode representative morale pairs or duplicate table bands.
2. For each resulting differential and36×36 assault pairs, select its existing
   `Definition.Effects` row. Determine whether capture die is required using that row's attacker and
   defender capture-sum sets; require never both. Determine whether refusal branch exists using
   DefenderRetreatOneHexSums. This only constructs legal arguments for Resolve; it does not calculate
   casualty percentages, rounding, captured shares, DP or survival itself.
3. Enumerate refusal=false plus true exactly when retreat exists; enumerate captureDie1..6 exactly
   when either capture flag exists, otherwise null once. Call existing
   `Resolve(am, dm, ac, dc, refusal, captureDie)` for **every** case. Expected coverage6480 joint
   coordinates/8840 resolved rows; tests assert counters. Deduplicate complete immutable
   `CombatSelectedResult` values only afterward, preserving Differential, RawEngaged, retreat/refusal,
   captured role/share and both complete CombatRoleLoss records. No unsafe projection dropping a
   field before proof; no source table reconstructed in009B.
4. For every distinct result, check both roles' conservation/remaining strength and possible guard
   donor using returned TotalLoss/CapturedLoss/DestroyedLoss/RemainingToe/LossDp. Enforce remaining
   TOE≥7 before guard transfer, positive lot≤3, donor≥1; zero captured loss produces no custody
   branch even if capture flag was set. Guard oneTOE capacity5 derives from accepted erratum/design,
   not a new adjudication formula. This checks representability of outputs, not recalculating Resolve.
5. Per certification call, apply every result to a **read-only support calculation** over current
   topology/roles. Check accepted-retreat final position versus refusal/no-retreat; original capture
   origin versus captor final position; both guarded route and unguarded reunion for every positive
   lot. Both side orientations are tested; current call uses cycle.ActingSide for role assignment.
   Enforce unique approved Clear retreat, farther-from-attacker and toward own anchor, bounded
   relocation/reunion and no entered blockers. Reuse Content7 graph/anchors, never seeded goldens.
6. Check role-cost and mandatory-retreat arithmetic on scratch typed operational values with existing
   `CampaignCombatSpending.ChargeOrdinary:62` and `ChargeMandatoryRetreat:83` where appropriate.
   Apply catalogue loss DP by checked subtraction from Cohesion before hypothetical retreat; fixed scratch
   receipt/cause IDs are local proof inputs, never returned as authority. CP10→11 has mandatory
   excess-CP DP; no ordinary ceiling applied to retreat. Do not create settlement/guard receipts,
   decrement authoritative World, publish events or invoke future lifecycle methods. Numeric/cause
   capacity support follows existing helpers; shape equality means input cause ledger is empty.

Reuse independent source certification in `CombatSelectedRulesTests.cs:102–171`:1296 morale,
6480 joint,8840 settlement cases and44208 weighted capture paths.009B tests assert catalogue coverage
and world support, not percentages against another copied oracle. `Cna1979CombatAdjudication.cs:23–87`
is sole arithmetic implementation; returned types are `CombatSelectedRules.cs:156–164`. The catalogue
is substantive runtime certification code called before producing Candidate, not unused speculative
model. Cumulative Core gate must retain Task005 tests plus009A and009B tests; this slice adds no
new claim that012–016 events ran.

## Exact current identity dependency

Use v2, not historical v1. `docs/specs/combat-sealed-round-v2.md:22,141–142`, `combat-sealed-round-v2.schema.json:30–31`, and `verify-combat-sealed-round-v2.py:140,167,259–262` define Base2 and ordered opportunity preimage `{baseHash,cycleId,positionId,candidate,declineReceiptId}` under `sandtable.combat.opportunity.v2`, prefixed `opp.`. Test existing literal `baseCanonicalUtf8` and round-opened evidence. Pure digest computation authenticates nothing. Task010 must supply accepted selection/decline; Task011 authenticates Base2 and creates final FA opportunity. No outward reference, selection receipt or seal lifecycle in009B.

## Capacity boundary and honest completion

Check representable CP/DP and relevant supplied-value array/cause limits without truncation. This fact API receives no current RNG cursor or complete Boundary/root; therefore it cannot claim cursor/version headroom or full-envelope capacity. Those checks belong to later opening/composition with actual retained values, not invented synthetic budget arguments. Selected Boundary explicitly caps arrays512; external World4096 causes does not silently widen Boundary. Exact future whole Snapshot12 successor-root capacity cannot be proved by existing H4 inherited writer or old synthetic CycleFrame snapshots. D2 `combat-authority-composition-v1.md` RootSnapshot witnesses (largest54181 bytes, depth12, maximum array21) demonstrate only their traced composition, not arbitrary future assault roots. Parent plan row009 refinement explicitly leaves whole successor-root capacity and later actual assault composition as integration gates. Do not invent reservation byte constants or a wire capacity certificate. Future actual writer must measure complete canonical root before positive opening/publication. This is a blocker to claiming live positive admission, not to bounded009B mechanism implementation.

## Required focused evidence

- Both actor directions; CP5/7 boundaries, over/fractional/no-reset cases; Normal/non-Normal; full inventory/provenance and unsupported current-state rejection.
- All support signatures from selected Rules cover accepted/refused retreat, both capture roles, guarded/escape choices, original survivors and guard capacity. Dynamic geometry changes rejected; topology certificate reuse explicit.
- Stable identity under relocation/depletion versus new occupant, original relation endpoints, immutable Candidate buffers and strict shape→canonical→trusted validation.
- Literal current v2 opportunity vectors; each preimage field mutation including hidden-prefix/base fork changes internal digest; no outward privacy claim.
- Numeric/cause capacity edges, no events/RNG/history mutation, and explicit remaining exact whole-root integration gate.

No contradiction with canonical009 refinement (`combat-cycle-implementation-plan.md:862–869`): it expressly permits positive synthetic mechanism fixtures with stated provenance, reserves final opportunity for010/011, and retains whole successor-root capacity/actual assault composition as integration gates. A stronger claim that this method authenticates a positive history or proves future full-root capacity would contradict that refinement and is excluded. No genuine blocker to this five-path dormant predicate/value implementation identified. No scope split/broadening proposed.


## Exact identity calculator signature

```csharp
public static string CalculateOpportunityId(
    string baseHash, string cycleId, string positionId,
    CampaignCombatCandidate candidate, string declineReceiptId);
```

Place on existing IdentityCodec. Validate hash/ID primitives and Candidate grammar, then write exactly
ordered preimage and domain-separated digest from current Round2 oracle:259–262. Base hash is supplied
by later authenticated Base2; this calculator does not accept arbitrary Base JSON or imply hash
proves authenticity. Require caller to provide FA position and genuine decline receipt in010/011;
009B checks primitive spelling only, so digest fixtures may use explicitly synthetic values. Candidate
codec follows shape/bounds→canonical→trusted expected Candidate comparison, reusing participant
syntax/binding without acquiring authority from parsed data. Proposed reader signature is
`ReadCandidate(ReadOnlySpan<byte> bytes, CampaignCombatCandidate trustedExpected)`; caller must
supply the independently certified expected Candidate, never parse it from the same untrusted bytes. Keep whole-root and Base2 authentication
out of this pure calculator.
