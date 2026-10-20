# P01 automatic-surrender source profile proposal v1

**Status:** owner decision requested. This is a dormant design packet, not an accepted policy,
registered Content/Setup, typed event, support certificate, public action, or native activation.
The original 300 CreationRequest + eleven-ledger R03 roots remain unchanged and unadmitted.

## Decision requested

Approve or reject a **separate, versioned synthetic singleton-infantry exercise** whose attacker
starts with ten carried Ammo Points and whose defender starts with **zero accessible ammunition**.
Keep both at ten TOE, rating 1/1, Basic Morale 0, Cohesion 0, Clear certified adjacent geometry,
normal stage-scoped readiness, and no accessible ammunition dump, truck, or other stock in the
defender's hex. Require a genuinely selected Close Assault against that defender and an accepted
defender-owned RBA **decline** before an automatic-surrender trigger can be certified. Actual RBA
movement remains outside this bounded proposal; unavailable/expired RBA cancels without a
fabricated decline. This is an alternative to accepted `CMB-POL-001`, which fixes **ten Ammo on
both sides**; it does not amend that profile or its original roots. `CMB-POL-009` still excludes
voluntary surrender, not source-required automatic surrender.

Owner approval here would authorize only contract and proof work for the proposed source profile.
It would not approve the exact event schema, settlement policy, privacy projection, P01 admission,
or public/native use. If the owner prefers to keep the initial RC strictly on `CMB-POL-001`, leave
automatic-surrender proof as a later version and keep its trigger fail closed in P01.

## Source and current authority boundary

| Source or frozen contract | Consequence |
| --- | --- |
| [Land §15.88](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF p24-25; [September 1979 errata §15.88](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), PDF p2 | Cohesion at or below −17 causes automatic surrender **when assaulted**. Enemy movement adjacent instead needs the distinct −26 threshold. Neither condition is present at Cohesion 0 in this proposal. |
| [September errata §50.12](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), PDF p3 | An ammunitionless unit surrenders only when actually assaulted, close or anti-armor. Adjacency, ZOC, and Barrage alone do not trigger it. The proposal uses the close-assault branch only. |
| [Full rules §§50.0–50.17](https://www.spigames.net/PDFv2/CampaignNorthAfrica.pdf), PDF p67-68; September errata §50.2 | Accessible supply and ammunition at use matter. Infantry spends one Ammo Point per TOE used. Zero carried points is insufficient proof if accessible ammunition exists in the same hex. |
| [Accepted policy](combat-cycle-policy-reconciliation.md) `CMB-POL-001/004/009` | Existing profile has ten Ammo per side and decline-only RBA; timeout cancels without decline; voluntary surrender is excluded. A new asymmetric profile needs a separately recorded source-scope choice. |
| [Content7](../specs/combat-content-v7.md), [Setup7/creation](../specs/combat-creation-ledger-v1.md) | Content7's selected-profile validator requires ten initial Ammo per placement (`CMB-CNT-007`); Setup7/creation requires Cohesion 0 (`CMB-INI-006`). Mutating an old fixture or R03 World is not an authentic new source. |
| [Current P01 diagnostic](../specs/combat-p01-surrender-rba-boundary-v0.md) | It classifies a trigger but cannot settle it. Every output remains private, `admitted=false`, `publicActions=[]`. |

The existing paid-use path depletes **both** ten-point balances after their defensive/offensive
uses. Its post-cost zero is not a new pre-assault ammunitionless trigger. No original R03 root
contains an authenticated automatic-surrender trigger. Selecting a convenient seed or changing
only one reconstructed World would break the ancestry proof.

## Proposed identity and freeze sequence

The following strings are **proposed allocations**, not registered aliases. Check the complete
registry for collisions before freezing the contract:

| Item | Proposed identity / rule |
| --- | --- |
| Capability | `sandtable.capability.combat-auto-surrender-infantry.v1` |
| Content successor | schema 8, `sandtable.content-json.v7`, pack `rules-lab.content.close-assault-ammunitionless-defender.v1`, scenario `close-assault-ammunitionless-defender-v1` |
| Setup successor | schema 8, setup `rules-lab.combat.close-assault-ammunitionless-defender.v1`; select only the new Content identity/hash |
| Source scope | `sandtable-rules-lab` synthetic origins for each changed ammo/readiness fact; pinned Land §15.88, Logistics §§50.0–50.17, September errata §§15.88/50.12/50.2, and the new owner decision in a **new** Rules/config digest |
| Existing identities | Preserve all Content7, Setup7, Rules10, original CreationRequest, R03 ledgers, canonical bytes and hashes |

Do not invent a hash before canonical bytes exist. Freeze a Content8 field order and canonical
UTF-8 codec; compute its identity as `sha256:` plus lowercase SHA-256 of those complete canonical
bytes. Freeze a Setup8 codec; compute `setupHash` from canonical Setup bytes **omitting only
`setupHash`**, as Setup7 does. Freeze a separate Rules/config artifact whose canonical bytes bind
the exact owner decision and source locators, then hash it with its versioned codec. Persist all
three byte strings and hashes in the new CreationRequest; authenticate them before producing any
later ledger. If a version or identity collides with another reserved successor, allocate a new
one before fixture generation; do not repurpose a historical version.

The new synthetic Content must carry independent explicit origins for attacker Ammo 10 and
defender Ammo 0. The zero must be **accessible** zero, not just a carried balance with a nearby
dump. Its closed geometry/supply inventory must prove no other source of ammunition is available
to the defender at the trigger. Setup starts both Cohesion values at 0; the −17/−26 branch is a
separate future profile, not a mutation of this one. Generate a fresh creation and all predecessor
ledgers from that new source. The original 300-root audit remains a regression gate, never a
fixture generator for the new profile.

## Required typed continuation before a positive certificate

1. Authenticate the new creation source, Rules/config, selected Content and Setup hashes, actor,
   original participants, weather receipt, slot/ordinal and every predecessor receipt. Prove
   that the attacker is armed, the defender has zero **accessible** Ammo before assault, and
   neither unit/relationship has an unaccounted prior commitment or settlement.
2. Derive a legal selected adjacent Close Assault, accept the acting-side selection, complete
   Position Determination and Barrage with exact no-effect evidence, then open RBA for the
   defender. Only an authenticated timely defender decline permits this bounded assault.
   Timeout/unavailable clock publishes a distinct cancellation and no surrender.
3. Specify the Force Assignment variant. The attacker commits its legal TOE once. The
   ammunitionless defender cannot be silently assigned ten paid defensive TOE by reusing the
   current `CMB-POL-001` assignment contract. A source-backed typed record must distinguish
   its compulsory surrender from a voluntary zero allocation and prove the exact trigger.
4. Decide and freeze CP/ammunition charging order relative to the mandatory surrender. The
   attacker debit, any defender combat CP exposure, history/target-use update and surrender
   trigger must be one replayable causal chain. No result-chart roll or second paid use may be
   inferred until a source review proves which effects the surrender preempts.
5. Freeze the automatic-surrender event and settlement: all ten defender TOE leave the original
   unit exactly once, an origin-bearing custody lot/guard or source-required escape obligation is
   recorded, final location/relationships and any victory/DP/RP consequences are explicit, and
   World/Control/receipt/RNG are conserved at every cut. No ordinary assault result is substituted
   for a compulsory surrender. Existing `CMB-DES-005` custody policy must be reconciled with a
   full-unit surrender before reuse.
6. Replay accepted command, duplicate command, precommit cut, postcommit cut, cancellation,
   settlement cut and fresh-process readback byte-for-byte. Failed/malformed/stale submissions
   cannot debit twice, draw RNG, disclose hidden fields, or fabricate a decline. Retain deadlines,
   owner-visible fields and hidden-history twin evidence for `P01-AC-007`; check frozen per-record
   and aggregate byte caps for `P01-AC-008`.

## Minimum negative and compatibility matrix

| Mutation or cut | Required result |
| --- | --- |
| Original Content7 defender Ammo `10→0`, or Setup7 Cohesion `0→−17` | Existing `CMB-CNT-007` / `CMB-INI-006` rejects; original 300 roots unchanged. |
| New profile defender has accessible dump/truck stock, changed source origin, forged Content/Setup/Rules hash, altered predecessor ledger | Reject before selection; never infer ammunitionlessness from carried zero alone. |
| Existing adjacency, ZOC or Barrage without a real accepted assault | No automatic surrender; no settlement event. |
| RBA timeout/unavailable/actor swap/late decline | Typed cancellation or rejection, not a synthetic decline and not surrender. |
| Forced ten-TOE defensive paid use or a second attacker debit | Reject as wrong profile/duplicate publication. |
| Full TOE removed without custody/relationship/obligation conservation, or a chart RNG draw when the frozen branch specifies no roll | Reject replay; preserve the prior authority. |
| Either acting side, automatic-surrender trigger and every reachable downstream settlement/choice outcome | Complete source-backed support matrix before any row can be admitted. |

Keep the present 300-root audit (44 provisional positives, 256 candidate-absent, zero admitted)
as a separate exact-byte regression. A new-profile positive must be generated from the new
Content+Setup, not counted among those original 44. Do not alter `P01-AC-003` action coverage,
`P01-AC-007` outward projection or `P01-AC-008` full audit status on the strength of this packet.

## Unresolved owner and engineering choices

The owner must first decide **whether this asymmetric exercise belongs in initial RC scope**.
If yes, contract work still must answer: whether the surrender preempts the ordinary result chart;
the exact combat CP and attacker Ammo charge at the trigger; defender Force Assignment shape;
the full-unit custody/guard/escape settlement and victory consequences; the public owner-visible
revision/alias/scope; and the compatible Rules/Content/Setup version IDs. Any unresolved choice
blocks a positive support certificate. Until those are frozen and proved, keep every P01 assessment
`unsupported`, `admitted=false`, and `publicActions=[]`.
