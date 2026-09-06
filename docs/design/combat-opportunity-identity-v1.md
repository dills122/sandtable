# Combat Opportunity, Target, and Participant Identity

**Status:** `CMB-DES-001` design packet complete for review; production remains gated.
**Date:** 2026-09-06. **Repository baseline:** `c210bb2` (merged PR #89).

This packet selects identity semantics for the first one-attacker/one-defender infantry Close
Assault. It advances the [Combat/cycle inventory](../research/combat-cycle-source-inventory.md)
using merged research as design inputs. It does not approve every pending research proposal,
freeze a wire schema, activate combat, or admit multi-unit combat. `CMB-DES-002` is next.

## Governing evidence and scope

Repository authority/replay/fog rules govern digital identities. Source rules govern participation
and timing. The decisions below are Sandtable design choices, not claims that the printed rules
define hashes, snapshots, or capabilities.

| Evidence | Locator and consequence |
| --- | --- |
| [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF p14, 8.14-8.15 and 8.25: ZOC entry stops movement; breaking off and repeated attacks are separate rules. |
| Land rules | PDF p15, 8.61-8.68: Movement-entry Contact, result-created Engaged, break-off and original marker membership. New arrivals do not inherit a marker. |
| Land rules | PDF p18, 10.3 and 11.0 procedure: mandatory attacks against qualifying enemy ZOC sources; other adjacent enemy-occupied hexes may be attacked. Contact is not a universal prerequisite. |
| Land rules | PDF p19, 11.25; p22, 15.12-15.16: CP exposure, strength contribution and casualty exposure differ. Close Assault targets an adjacent hex, with no second Close Assault against that hex in the same Combat Segment. |
| Land rules | PDF p24, 15.74, 15.76, 15.81: Retreat takes priority over Engaged; zero loss can still produce either result; Engaged affects involved units and expires at stage end. Previously assaulting units still adjacent are in Contact if not Engaged. |
| [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | 10.3 applies only to the phasing player; 12.44 distinguishes a battalion-equivalent Barrage target from a hex. Do not reuse a Close Assault target model for Barrage. |
| [CONTACT-001 lock](../research/contact-reaction-zoc-source-ruling-lock.md) | Accepted ZOR decisions: persisted windows, frozen membership, latest-authority validation, and separate outward identity. Immediate ZOC entry is not Contact. |
| [Cycle identity/history decision](../research/continual-cycle-identity-and-history-decision.md) | `CYCLE-DEC-002`, `007`-`009`, `011`, `013`: full internal provenance, stable unit history, once-only commit append, and fog-safe references. |
| [Result surface](../research/combat-rules-result-surface-spike.md), [mutable state](../research/combat-mutable-state-spike.md), [Reserve history](../research/reserve-release-history-spike.md) | One independent non-Reserve 10-TOE infantry battalion per side; full commitment; reachable losses/capture/one-hex Retreat/Engaged still require settlement. Reserve offensive-use history is a separate stage constraint. |

The cited Land pages were rendered and visually checked; the errata text was checked. No source
scan or table is added. Broad source features in this table explain extension boundaries; they do
not expand the first admitted fixture.

Current authority confirms the boundary:

- [Map representation](../../src/Cna.Core/Campaigns/CampaignMapRepresentationState.cs) binds
  exactly one independent element. Representation ID and current location are mutable context,
  not the historical identity of a rules unit.
- [Reaction identity](../../src/Cna.Core/Campaigns/CampaignReactionIdentity.cs) deliberately hashes
  representation/location into an opportunity. Combat may reuse this binding pattern, but cannot
  use that digest as its persistent unit identity.
- [Current Movement authority](../../src/Cna.Core/Campaigns/CampaignElementMovedV3Factory.cs),
  `DeriveControlledLocationIds`, certifies empty ZOC for the public Breakdown profile: at most one
  combat/HQ battalion per own hex cannot satisfy the greater-than-one stacking threshold.
- [Disclosure identity](../../src/Cna.Core/Observations/CampaignObservationV6DisclosureIdentity.cs),
  `CreateAliases`, rejects indistinguishable capabilities without a group-selection policy. Its
  current revision parameters are not a ready-made Combat identity contract.

The first combat fixture can therefore begin with voluntary adjacency and no Contact. Positive-ZOC
Contact requires a separately certified public profile; no fabricated Contact or changed ZOC rule
is needed to make the first assault possible. Result-created relationships still need settlement.

## Identity decisions

These semantic names are design notation. `CMB-DES-002` and `CYCLE-DES-001` will freeze canonical
encodings, domain/version tags, contract fields and migrations before implementations use them.

| ID | Decision |
| --- | --- |
| `CMB-ID-001` | A rules unit is keyed by campaign creation scope, original side and stable Content element ID. Component identity additionally includes that element's stable component ID. Current TOE, location, representation, parent, cycle and authority version are not unit identity inputs. |
| `CMB-ID-002` | A participant binding is a frozen occurrence of a rules unit in a specific combat opportunity, with role, representation/bound-element evidence, location and component inventory. Identity continuity and current eligibility are separate checks. |
| `CMB-ID-003` | A Close Assault target retains both the selected hex and its exact frozen defender-unit set. The admitted set is one unit. History records the unit; per-segment target-use accounting records the hex. Neither substitutes for the other. |
| `CMB-ID-004` | Contact and Engaged are provenance-bearing relations between original participants. They are not a hex flag, connected-component combat group, generic status, Reaction window or combat opportunity. |
| `CMB-ID-005` | Each opportunity binds one authoritative cycle occurrence, one structural combat position, one frozen combat base and exact participants/target/basis. Its internal digest is never an outward action or target reference. |
| `CMB-ID-006` | Irreversible commitment creates a distinct commitment occurrence and appends the selected directional attacker-to-defender history entry exactly once, before RNG. Opening, a private envelope, cancellation or pre-commit rejection appends nothing. |
| `CMB-ID-007` | Outward opportunity/target references derive solely from approved audience-visible facts and visible decision revision. Umpire retains the mapping to exact authority. Hidden identities, versions, counts and hashes cannot salt or order those references. |
| `CMB-ID-008` | The first admission is one independent infantry unit per side, one component each, one target hex and full Close Assault assignment. Multi-unit/partial assignments, attachments, Probe, Barrage and Anti-Armor identity/history admission remain explicitly closed. |

### Unit continuity and membership

The creation scope includes the retained campaign creation binding and setup/Content provenance;
it is not merely an element display name or a new ID generated on every event. Content element IDs
identify unit instances within that scope, not a shared infantry class. A rules/config migration
must explicitly preserve that creation binding or supply a reviewed mapping; it cannot reinterpret
old records using replacement Content.

Moving, retreating, losing TOE or changing an apparent representation preserves the rules-unit
key. Each new opportunity binds the new representation/location/current facts. A changed binding
invalidates an uncommitted opportunity; it never silently retargets the old choice. Elimination
retires live eligibility while retaining historical identity. Captured personnel retain origin
through the custody ledger; they are not reissued as the same combat unit on the captor's side.

Attachment, detachment, split, merge, replacement or transfer of a rules unit is outside this
admission. A future extension must map old/new unit and component identities explicitly, preserve
all old occurrences, and specify relation/resource continuity. Relabelling a representation or
changing organization must never clear attacked history or Reserve offensive-use limits.

### Relationship membership and combat basis

Retain each relation's kind, creation occurrence, participating unit keys and directional evidence,
plus active/ended membership and ending cause. For the selected assault this is a single cross-side
pair. A later multi-unit relation must retain explicit endpoint membership; sharing an intermediate
unit does not create a transitive relation or authorize a combined assault.

Contact has two source bases that must remain distinguishable:

1. **Movement-entry ZOC:** evaluate 8.62 at the relevant Movement Segment opening. Retain the
   contacted unit and the qualifying opposing source membership/evidence at that boundary.
   Aggregated ZOC evidence must not invent a claim that each source component qualifies alone.
2. **Prior assault adjacency:** 15.81 supplies the Contact obligation for surviving original
   assault participants still adjacent when they are not Engaged. This is a required `CMB-DES-005`
   settlement input even for the no-ZOC fixture. It does not make arbitrary adjacency Contact.

Engaged instead links the original assault participants through its result occurrence, including a
zero-loss result. `CMB-DES-005` must apply Retreat priority, elimination and stage-end transitions
before deriving current relation state. It must evaluate the surviving-adjacency Contact obligation
when Engaged ends; clearing an Engaged marker does not prove unrestricted movement.

Breaking off ends the relevant participant membership, not unrelated pairs. When all counterparts
have broken off, the residual participant cannot remain constrained by a relation to nobody
(8.67). An arriving unit does not inherit an old relation merely by occupying its hex (8.68).
Relation multiplicity does not imply additive break-off CP; cost precedence and the exact atomic
movement/Retreat Before Assault transition remain `CMB-DES-003`/`005` work. Reaction retains its
separate source exceptions and lifecycle.

Each opportunity retains one or more actual basis records: voluntary adjacent target, current ZOC
attack obligation, and applicable retained Contact/Engaged evidence. Historical Contact alone does
not establish current attack eligibility; absence of Contact does not prohibit voluntary adjacent
Close Assault. A stale or ended relation cannot be used as a live basis.

### Frozen opportunity and target

Required internal binding inputs are:

- full campaign/rules/setup/Content provenance and the authoritative cycle identity;
- game turn, Operation Stage, first/second player slot, ordinal and phasing side, validated against
  that cycle; current structural Combat position and combat category;
- opening authority version, canonical authoritative history-prefix binding and combat-base
  snapshot digest (excluding the opportunity record being constructed, avoiding recursive hashes);
- target hex, exact defender-unit membership, exact attacker membership, participant bindings and
  rule/provenance evidence for admission, adjacency, relations and mandatory obligations.

The history-prefix binding distinguishes divergent Exercise histories that share campaign ID and
event count. Arrays use ordinal stable-key order with duplicate rejection. Canonical serialization
order does not choose the player's order for separate source-legal combats.

Only one such Close Assault opportunity is active in the first admission. First/second private
envelopes refer to the same frozen combat base. Persisting an envelope may advance authority but
must not rewrite that base or append combat history. `CMB-DES-002` must distinguish permitted
envelope bookkeeping from combat-fact mutations; a changed participant, target occupancy, relation,
resource or provenance cannot be accepted merely because the outward reference still matches.
Opening an unsupported opportunity or accepting a stale choice emits no combat mutation or RNG.

Three memberships stay distinct even when equal in the selected fixture: eligible units, units/TOE
committed to contribute strength, and units exposed to results or CP. Source 11.25 and 15.12-15.16
prevent deriving the third solely from the second. The target occupancy set is frozen as evidence;
it is not permission to omit withheld defenders from later result accounting. Any expansion beyond
the selected equal singleton sets requires a reviewed assignment/exposure design before admission.

### Commitment, repetition and attack-category boundary

For one attacker `A` and one defender `B`, history entry identity binds the new commitment
occurrence and the directional edge `A -> B`. Both endpoints must belong to that commitment's
frozen sets. A commitment is reproducible from its opportunity, accepted assignment evidence and
commit transition provenance; replay cannot allocate a fresh ID to disguise a duplicate append.
Append, commitment state and target-hex use are atomic. Result RNG follows this commit boundary.

Maintain a distinct consumed target-hex set scoped by authoritative cycle/Combat Segment occurrence
and phasing side. Rule 15.16 prohibits assaulting the same hex twice in that segment even with a
different attacker or a changed occupant. A committed zero-loss outcome still consumes the target.
A rejected or withdrawn pre-commit choice does not. A new cycle gets a new segment occurrence and
an empty target-use set; active-stage attacked-unit history remains intact.

This qualifies the cycle research's conditional permission for a repeated pair in the same cycle:
the independent 15.16 check still applies. For the selected stationary pair it requires a later
cycle, current resources and current legality. Identity permits repetition; it does not waive
ammunition, CP, Reserve limits or target-hex restrictions. The one-use ammo research fixture itself
does not prove a second fueled assault.

There is no Cartesian history expansion in this admission. Any second attacker, second defender,
split component assignment or second target is unsupported before commitment. For future work:

| Category | Required identity distinction; current history admission |
| --- | --- |
| Close Assault | Hex plus frozen unit sets; singleton `A -> B` only. Multi-unit allocation, exposure, edge expansion and atomic per-record IDs need a separate extension. |
| Barrage/Holding Off | Specific battalion-equivalent targets under errata 12.44; target-type disclosure and blind selection under 12.23-12.24 need their own ruling. No entries in the selected Close Assault history. |
| Anti-Armor | Target hex/armor exposure and simultaneous allocation differ from Close Assault. No entries in the selected Close Assault history. |
| Probe | Related source procedure but distinct resource/result surface. Not admitted or silently counted as ordinary Close Assault; future design must also consume the source-required Reserve offensive allowance. |

Empty Barrage/Anti-Armor structural steps create neither participant records nor attacks. The
category boundary resolves `CYCLE-DEC-013` for the selected vertical only; it does not claim to settle
every later combat category. `CMB-DES-003`/`004` must keep those categories dormant until extended.

## Audience boundary and downstream contracts

Use the audience-safe cycle reference, visible decision revision and canonical approved capability
facts for outward identity. Authority resolves that reference through its retained mapping and
regenerates current membership before accepting a submission. The mapping and hidden binding stay
inside Umpire/Chronicle/Archives. Neither a raw authority hash nor an opaque hash of hidden facts
is safe merely because its preimage is unreadable.

The acting audience may reference its approved own participant capability and an apparent enemy
target/location already permitted by disclosure policy. The waiting audience receives a generic
decision/waiting shape with no opposing choice, withheld forces, hidden candidates or reason.
Exact opposing unit/component IDs, TOE, ratings, ammunition, relations' hidden source sets and
Chronicle-prefix hashes cannot reach Dispatch, legal-action payloads, errors or side histories.

Two authorities with equal approved visible histories must produce identical outward references,
ordering, observations, action sets and projected history. Do not sort by hidden unit ID or issue
one alias per hidden defender. If two hidden bindings are indistinguishable, public admission stays
closed until an explicit group-selection/disclosure policy exists; no hidden-ID tie-break is allowed.
This gate belongs in scenario/profile admission before an active decision, not an exposed runtime
exception that reveals hidden multiplicity. `CMB-DES-005` must also resolve every hidden-dependent
legality distinction before public actions are offered.

Archives must retain the exact open opportunity/base, relation membership, mapping, consumed target
hexes and active-stage history needed after restart. Full Chronicle retains creation/end/commit
evidence; replay reconstructs the same bindings without rebinding against today's representation.
`CMB-DES-002` owns sealed state, action-set binding, revisions, cancellation/fallback, events and
strict codecs. `CMB-DES-003` owns pre-resolution choice transitions; `004` owns resolution ordering;
`005` owns relation settlement and disclosure. `CYCLE-DES-001` owns repeat/finish and snapshot
composition. None may infer a general cycle or combat contract from these semantic names alone.

## Acceptance and verification handoff

These are design acceptance cases checked against the decisions/source locators above. Runtime
tests do not exist for this packet and must be implemented with the eventual governing contracts.

| Case | Required observation | Decisions / next owner |
| --- | --- | --- |
| `CMB-ID-AC-001` | Move or change representation: same unit key, new binding; old uncommitted choice fails without events/RNG. Elimination preserves history. | 001-002; DES-002/005 |
| `CMB-ID-AC-002` | Same component name on another unit/campaign does not alias. Duplicate bindings and foreign provenance reject. | 001-002/005; DES-002 |
| `CMB-ID-AC-003` | Adjacent singleton infantry with empty ZOC can have a voluntary opportunity and no initial Contact. Positive-ZOC admission remains gated. | 004/008; DES-003 |
| `CMB-ID-AC-004` | Later arrival in a marked hex inherits no relation; ending one of two independent counterpart relations leaves the other intact. No transitive grouping. | 004; DES-005 extension vector |
| `CMB-ID-AC-005` | Zero-loss Engaged still binds original participants; Retreat priority prevents that Engaged outcome. Adjacent prior assault survivors require the 15.81 Contact evaluation. | 004; DES-005 |
| `CMB-ID-AC-006` | Same campaign ID/version but divergent authority prefix changes internal opportunity; equal visible histories leave outward IDs unchanged. | 005/007; DES-002/005 |
| `CMB-ID-AC-007` | Two private envelopes share one base; envelope persistence adds no attacked entry. Combat-fact mutation invalidates the old opportunity. | 005-006; DES-002 |
| `CMB-ID-AC-008` | Commit singleton `A -> B` once before RNG; duplicate replay fails. Same target hex cannot be assaulted again that segment, even with a new attacker. | 003/006; DES-002/004 |
| `CMB-ID-AC-009` | Later cycle allows a new pair occurrence only with current resources; stage history persists and target-use resets. Reserve limits remain independent. | 006; CYCLE-DES-001 |
| `CMB-ID-AC-010` | Multi-unit, partial assignment, attachment, Probe, Barrage and Anti-Armor remain closed; empty structural steps produce no attacks. | 008; DES-003/004 |
| `CMB-ID-AC-011` | Hidden identity/TOE/count permutations do not alter equal visible artifacts. Indistinguishable capabilities cannot be ordered by authority ID. | 007; DES-005 |
| `CMB-ID-AC-012` | Restart before either envelope, after commitment and after relation settlement reproduces IDs, mappings, history and target-use without duplicate RNG. | 001-007; DES-002/005, CYCLE-DES-001 |

Completion means the bounded identity proposal and explicit handoffs are reviewable, not that the
whole Contact/Combat design gate is approved. Remaining production blockers are DES-002 through
005, CYCLE-DES-001, selected research rulings, complete reachable-result settlement, a governing
specification/contract freeze, implementation-sized tasks and independent plan review.
