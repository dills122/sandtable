# N1 independent test-vector preparation

Prepared 2026-10-14. Status: read-only preparation, not implementation, TDD evidence, independent implementation review, or acceptance. Owner: integration_public_audit; only this file is owned by this lane. No product/test/project/fixture files changed and no suite run. I01 is independently Ready at `d9db9e05967ddc7f8c2eca20129aee2fc5c3133d`; CI/main acceptance remains a dependency. N1 implementation GO/interface freeze belongs to Brain.

## Evidence and interpretation

Expected behavior was derived from I01 `docs/specs/combat-actual-reaction-combat-entry-v1.md` at `d9db9e0`, then unchanged downstream requirements/source at `42503c570294e5e4823ff1995bbfdcadc298bf07`, then compared with `/private/tmp/i01-next-author-plan.md`. All product paths below are relative to `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`; citations refer to immutable revisions, not a mutable author branch.

* Selection: `docs/specs/combat-actual-selection-v1.md`, positive/fallback contract and lines 124–149; `CampaignCombatActualSelection.cs:157–280` under `src/Cna.Core/Campaigns`; literal fixture has 2 positive and 14 fallback rows.
* Round: `docs/specs/combat-actual-round-entry-v1.md:94–144`; `CampaignCombatActualRoundEntry.cs:292–434` and `EmptyAa:435–475`; literal fixture has 4 positive and 30 cancellation rows.
* Paid: `docs/specs/combat-actual-paid-assault-v1.md:36–70`; `CampaignCombatActualPaidAssaultSupport.cs:113–129` is the old CP0 assertion/reset which the new cumulative unit must replace honestly. The complete support requirement is spec line 58, rather than merely an arithmetic digest.
* I01 native result: `CampaignCombatActualReactionCombatEntry.cs:40–54` returns copied Source/Control/Proof/Command/Event and separate prefix/lifecycle/Breakdown input ledgers, typed World/Candidate, and structural `FinalBreakdownEventBytes`. Replay APIs are lines 61–105. Do not guess a second PositiveEntry event.
* Support inventory is `/private/tmp/integration-continuation-support.md`, SHA256 `0e5fcff469daa510feea1be266547056f6acfee12195acc07a31b1a45da8925e`. Its synthetic kernel classes are support evidence only.

Graph coverage for newer Actual files was previously nottracked; immutable source fallback was used. This prep does not claim new graph coverage, fresh source admission, native result verification, or a new canonical dependency manifest.

## Twelve genuine root contexts

All rows must start from independently replayed I01 terminal18, original creation request, its three separately authenticated ledgers and pinned dependency bytes. A closure reason is part of the genuine causal history; it is not a flag substituted into a Source. A=attacker/original first owner; D=defender/opposite owner. Role-ordered slots stay A,D even when D seals first.

| ID | First owner / A | D | Actual Reaction closure | Causal seal order |
|---|---|---|---|---|
| P01 | axis | commonwealth | player-decline | A then D |
| P02 | axis | commonwealth | player-decline | D then A |
| P03 | axis | commonwealth | scripted-unavailable | A then D |
| P04 | axis | commonwealth | scripted-unavailable | D then A |
| P05 | axis | commonwealth | timeout | A then D |
| P06 | axis | commonwealth | timeout | D then A |
| P07 | commonwealth | axis | player-decline | A then D |
| P08 | commonwealth | axis | player-decline | D then A |
| P09 | commonwealth | axis | scripted-unavailable | A then D |
| P10 | commonwealth | axis | scripted-unavailable | D then A |
| P11 | commonwealth | axis | timeout | A then D |
| P12 | commonwealth | axis | timeout | D then A |

For each row retain actual bytes from accepted native/reference execution after freeze. Do not create or renumber event/receipt/hash goldens now. Required independent expected literals:

| Cut/action | Version/count expectation | Resource / RNG / progress expectation |
|---|---|---|
| I01 Combat entry | 18; original ordinal1, idle/null interrupt | A CP4/1, D CP0/1; ammo10/10; TOE10/10; seed1/cursor2; exactly two authentic Movement progress refs |
| Selection/RBA positive | 7 accepted events; endpoint25, stepIndex3; 3 completed steps, accepted defender decline | World byte-equivalent to entry where represented canonically; no spend/draw/progress addition |
| Open assignment | 25→26, floor3000, deadline33000 | Full current candidate/World, each sole infantry component, committedTOE10, A,D slot order |
| Two seals | 26→27→28; Prepared after second | Both orders at4000 then3500 accept. Private first seal time cannot raise shared floor. No World/RNG/material progress delta |
| FA then empty AA | 28→29→30, stepIndex5; 5 step receipts | AA certificate binds pinned Content, **actual CP4 World**, current candidate, A,D allocations and actual FA receipt; no paid mutation |
| Commit | 30→31; one event / commitment | A CP **4→9**; D CP **0→3**; both ammo **10→0**; TOE10/10; no debit-induced DP/RP/Cohesion change (CPA10); seed1/cursor2 unchanged; two original progress refs plus one genuine commitment progress ref |
| Resolve | One event, role-ordered purposes | Native deterministic algorithm and retained draw/rejection trace; existing seed1/cursor2 ordinary path expects cursor10, A loss0/D loss1 and Contact. Validate against genuine replay; do not inject this outcome or seed |
| Paid closure | 8 mandatory paid events; ordinary endpoint38; optional windows/custody permit up to11 events / endpoint41 | CA completion binds AA and round-closure receipts; no pending immediate window/lot; future duties retained; original cycle/proof/progress intact |
| N1 terminal | Same-cycle Reserve Release **ENTRY** | No completed Release, PAC, ordinal2, finish or public/Exercise/Runner success claim |

At commit assert literal cost preimages `(A,beforeCp=4,afterCp=9,beforeAmmo=10,afterAmmo=0)` and `(D,0,3,10,0)` without computing expectations through the production debit helper. The wrong reset to CP5 must fail. Existing outward/return Movement costs and causes remain, including operational/readiness/provenance fields. Resolve/settlement must not later recompute a CP0 paidWorld and erase CP4. Attack history, target use, costs and commitment publish atomically once. Check every genuine cut and restore suffix against independent trusted inputs, rather than comparing only the terminal Control.

## Forty-four fallback rows

Rows below transfer frozen causal cases to the new authenticated entry; versions are **event-count expectations**, not literal bytes or admission evidence. Each ID is distinct. These 44 cases are the base fallback inventory (22 per owner); execute over real I01 histories, with all six owner/closure roots represented in fallback coverage. This does not claim 44 alone is the complete closure-reason Cartesian product.

Every row retains CP4/0, ammo10/10, TOE10/10, original operational fields/custody/future duties, seed1/cursor2, original MovementEndProof and the two movement progress refs. No attack/target-use/commitment/cost/result/progress is manufactured. Closed stepIndex6 has six total step receipts, ending at pending Reserve ENTRY. "finish" below is the **Selection command variant** which declines attack; it is not campaign finish. No fallback is relabelled empty/exhausted ammo.

| ID | Owner | Selection/RBA variant | Selection events | Expected N1 version |
|---|---|---|---|---|
| S01 | axis | opening-unavailable | 8 | 26 |
| S02 | axis | finish | 8 | 26 |
| S03 | axis | selection-expired | 8 | 26 |
| S04 | axis | selection-unavailable | 8 | 26 |
| S05 | axis | rba-before-open-unavailable | 9 | 27 |
| S06 | axis | rba-expired | 10 | 28 |
| S07 | axis | rba-unavailable | 10 | 28 |
| S08 | commonwealth | opening-unavailable | 8 | 26 |
| S09 | commonwealth | finish | 8 | 26 |
| S10 | commonwealth | selection-expired | 8 | 26 |
| S11 | commonwealth | selection-unavailable | 8 | 26 |
| S12 | commonwealth | rba-before-open-unavailable | 9 | 27 |
| S13 | commonwealth | rba-expired | 10 | 28 |
| S14 | commonwealth | rba-unavailable | 10 | 28 |

Selection closures carry their actual selection/cancellation receipt; no accepted defender decline is invented. Each subsequent no-attack completion binds that disposition and ordered previous-step receipt. Equality at exclusive Selection/RBA deadline rejects player action; appropriate System expiry closes. Invalid participant/candidate is rejected before clock faults.

Assignment rows start at positive Selection25. Empty cancellation: 5 round events, version30, zero accepted seals. A-only or D-only: 6 events, version31, exactly that role's accepted seal audit retained. All three subsequent FA/AA/CA no-attack completions bind the cancellation receipt and ordered previous-step receipts. Causes map as follows: deadline=System expiry at/after33000; controller=explicit System unavailable; confidence=valid own seal with unavailable confidence; null=valid own seal with no admitted time; below-floor=valid own seal time<3000. The last three publish System-authored clock-unavailable cancellation but retain the original player actor in trusted input/receipt identity. Bad owner/allocation/context rejects before this fallback.

| ID | Owner | Partial accepted seal | Cause | Events | Version |
|---|---|---|---|---|---|
| R01 | axis | empty | deadline | 5 | 30 |
| R02 | axis | empty | controller | 5 | 30 |
| R03 | axis | empty | confidence | 5 | 30 |
| R04 | axis | empty | null | 5 | 30 |
| R05 | axis | empty | below-floor | 5 | 30 |
| R06 | axis | A-only | deadline | 6 | 31 |
| R07 | axis | A-only | controller | 6 | 31 |
| R08 | axis | A-only | confidence | 6 | 31 |
| R09 | axis | A-only | null | 6 | 31 |
| R10 | axis | A-only | below-floor | 6 | 31 |
| R11 | axis | D-only | deadline | 6 | 31 |
| R12 | axis | D-only | controller | 6 | 31 |
| R13 | axis | D-only | confidence | 6 | 31 |
| R14 | axis | D-only | null | 6 | 31 |
| R15 | axis | D-only | below-floor | 6 | 31 |
| R16 | commonwealth | empty | deadline | 5 | 30 |
| R17 | commonwealth | empty | controller | 5 | 30 |
| R18 | commonwealth | empty | confidence | 5 | 30 |
| R19 | commonwealth | empty | null | 5 | 30 |
| R20 | commonwealth | empty | below-floor | 5 | 30 |
| R21 | commonwealth | A-only | deadline | 6 | 31 |
| R22 | commonwealth | A-only | controller | 6 | 31 |
| R23 | commonwealth | A-only | confidence | 6 | 31 |
| R24 | commonwealth | A-only | null | 6 | 31 |
| R25 | commonwealth | A-only | below-floor | 6 | 31 |
| R26 | commonwealth | D-only | deadline | 6 | 31 |
| R27 | commonwealth | D-only | controller | 6 | 31 |
| R28 | commonwealth | D-only | confidence | 6 | 31 |
| R29 | commonwealth | D-only | null | 6 | 31 |
| R30 | commonwealth | D-only | below-floor | 6 | 31 |

Additional boundary checks are not among the44: unavailable/null/overflow assignment **opening rejects with zero events**, rather than making an empty cancellation; player seal at33000/after rejects, followed by separate System expiry; live System timer before33000 is no-op; callbacks after Prepared (including unavailable) are no-op, and accepted intent defeats late timers.

## Integrity, retry, ownership and support matrix

Apply negative vectors to a genuine source copied from an accepted cut. Re-signing a mutated event/claim is a consistency attack, not an authentic source. Keep independently supplied trusted ledgers unchanged when testing forged retained evidence. Never manufacture a matching "trusted" ledger from event payloads to bless the forgery.

| IDs | Vector / expected behavior |
|---|---|
| I01–I04 | Foreign request campaign/config/content/seed, foreign first owner/cycle/ordinal, copied old entry13/Selection20/Prepared25, or guessed PositiveEntry Breakdown: reject before admitting new choice/debit. New source family cannot make old closed readers admit it. |
| I05–I08 | Missing/excess/reordered event or independent input tail, cross-owner/closure root splice, changed actor/time in retained ledger, locally re-signed event/World/candidate/receipt/progress/Movement proof/AA certificate: reject without state/ledger/RNG change. All three I01 ancestor ledgers plus Selection/Round/Paid ledgers must be replayed. |
| I09–I12 | Unknown/duplicate/missing keys, alternate order/whitespace/BOM/trailing JSON/escape, bool-as-int/float/overflow, unsupported arm/version: closed grammar/primitive error before retry/fallback. Claim parsing occurs only after successful ingress replay. Freeze exact N1 error mapping rather than copying old family labels. |
| I13–I16 | Physical dependency missing/changed/unreadable/callback buffer changed after first read, cold/warm/cache/retry/at-use path: reject using owned exact bytes before authority/cache use. Physical acquisition closure must cover new ports and transitive helpers. An arithmetic catalogue hash or root hash allowlist is insufficient. |
| O01–O04 | Mutate every caller input/source/dependency buffer after admission; mutate returned Source/Control/Proof/Event and returned ledgers. Previously owned result/replay is unchanged; later ingress of changed caller bytes rejects. Snapshot-before-callback ordering and no alias through typed World are measured. |
| T01–T04 | Exact accepted canonical command+actor retry at immediate/later/terminal cut returns original accepted event/receipt with unchanged **current** Control/prefix/version/ledgers; no debit/draw/progress twice. Changed command/context/actor cannot borrow receipt; invalid primitives reject before duplicate lookup. |
| T05 | **Selection/Round/Paid** command+actor identity permits changed *valid* current trusted time/confidence to recover original receipt before live clock/lifecycle/admission gates. Retained source ledger time remains immutable. **I01** retains exact-entire-input identity; do not impose one policy across both families. |
| T06–T08 | Stale valid timers before live deadline or after completed/foreign decision no-op as defined; malformed/foreign actor/invalid primitive never gains no-op immunity. Fresh admission disabled blocks new open/commit, while accepted Prepared/Paid histories, original retries and mandatory completion remain recoverable. |
| C01–C04 | Selection floor/high-water, RBA cannot open before accepted Selection high-water, exclusive deadlines, checked UTC addition; correct rejection/fallback distinction. Round floor3000 never changes after private seal4000; second seal3500 valid for either order. |
| C05–C08 | Required retreat owned by D; positive captured lot owned by captor. New local window high-water derives own opening, not previous audit maximum. Retreat10001 vs11000 then custody opening10500/choice10600 must produce equal gameplay despite audit maxima. Null/unavailable/overflow opening fallback, deadline equality, below-local-time choice fallback, malformed time rejection, live-window structural bypass rejection. |
| Q01–Q04 | Corrupt/remove/duplicate a selected catalogue row; missing disposition/loss/retreat/custody/relationship/closure handler; insufficient source/control/proof/window capacity: complete support rejects **before fresh commit**, with CP4/0 and ammo10/10 unchanged. Do not introduce a seed filter, synthetic zero result, refund or unsupported-after-cost branch. Inject isolated certification inputs, not mutable global flags or counterfeit actual sources. |
| Q05–Q08 | Complete normalized records/order/digest and all handlers are certified on the **same cumulative CP9/3 pre-loss World** used by runtime. Preserve creation/origins/membership/Weather/representation/causes and unrelated collections, all original receipts/progress/proof; actual Resource deltas only. Full closure has no pending immediate custody/window and retains future duties. |
| Q09–Q12 | Dominating ordinary/optional branch envelopes measured before capacity freeze; max-valid ingress and just-over-bound negatives, quoting/depth/cumulative ledgers, owned bytes, source cut/readback/restore checked. Old I01 bounds remain unchanged. No numeric N1 envelope limits invented by this packet. |

T05 corrects a potential overbroad reading of the author plan's "changed clock rejects/no-ops as specified": frozen Selection spec144–147 and Round126–128 explicitly recover exact command+actor with changed valid time. Changed retained evidence still rejects. If N1 deliberately changes this policy, freeze the additive difference and its rationale before tests; it is not a mechanical source adaptation.

## Complete selected support cases, kept separate from authentic paths

For each owner certify all16,616 records (1,296 Morale +6,480 cells +8,840 capture/refusal rows) and all261 mechanical keys, not merely a selected seed1 witness. The isolated inventory yielded96 distinct settled mechanical Worlds /391 disposition-custody combinations per owner:

| Support class | Mechanical Worlds per owner | Combinations per owner | Independent checks |
|---|---|---|---|
| No capture / no relation | 12 | 43 | Disposition, surviving original members, no invented custody |
| No capture / Contact | 24 | 73 | Adjacent survivors/original relation endpoints |
| No capture / Engaged | 6 | 15 | Raw Retreat suppression and original relation membership |
| A captured / guard | 15 | 45 | Captured subset; oneTOE donor transfer separately conserved; ≤3hex geometry and feeding duty |
| A captured / escape | 15 | 45 | No TOE restoration; ≤8CP reunion,12-stage replacement/training gate |
| D captured / guard | 12 | 85 | Same conservation/duty checks with roles reversed |
| D captured / escape | 12 | 85 | Same escape provenance/delay checks with roles reversed |
| Armed ordinary-NONE noattack | 1 resource class | 44 frozen rows total | CP4/0, ammo10/10, TOE10/10, progress2; pending Release only |

Independent arithmetic expectations: A loss=`ceil(10×Apercent/100)`; D loss=`floor(10×(Dpercent+10×unfulfilledRetreatHexes)/100)` (**refusal added before rounding**); captured subset=`ceil(roleLoss×captureShare/100)`, not another loss debit; loss≥3→3DP; guard transfer does not create another loss threshold; mandatory retreat moves cost1CP with incremental over-CPA DP/RP from the currently charged World. Preserve total operational/guard/captured/other-loss TOE20. Original participant relations never attach to guards. Future duties survive Reserve entry without execution.

These classes are coverage selectors for isolated native support tests, not fabricated campaign Source/Proof/Event cases. The genuine roots remain P01–P12 with unmodified native seed/cursor. Support certificate completeness and actual causal replay must both pass; neither substitutes for the other. The inventory's World size13069bytes is not a new source-envelope cap or a max-valid input proof.

## Handoff and use after freeze

The author plan agrees with these costs, ancestry, full support gate,44 base fallback cases and Reserve ENTRY limit. Outstanding freeze values: exact N1 APIs/tagged terminal shape, own identity domains/error map, input-ledger framing, dependency manifest and measured capacity bounds. Fill test bindings and capture **actual** generated literal bytes only after that coherent contract freezes and Brain grants test writer ownership. Independent accounting/lineage tests can own new dedicated files if explicitly assigned; author keeps eight proposed consumer/spec/fixture files plus csproj/canonical docs. This lane has no writer ownership there.

Smallest useful RED: replay one genuine I01 CP4 source through Prepared, then assert literal CP4→9 /0→3 and ammo10→0 on exactly one accepted commit, cursor2 and progress preservation, plus missing-support precommit atomic rejection. Follow with both role orders and all12 contexts; full selected support certification remains required before fresh choice/debit admission. Compile failure, fixture generation, research probe and zero-selected-tests are not behavioral RED.

No PAC/public test binding or acceptance activation follows from this prep. All-reachable settlement support, authenticated armed noattack continuation, actual repeated player Movement/contact, public own histories/admission/projection, Exercise/Runner seed/identity, laterII/consumed and all72/fullCombat/skeleton remain separate gates. Preparation confidence high for literals/fallback contract; N1 integration/support/capacity acceptance remains conditional on implementation and gates.
