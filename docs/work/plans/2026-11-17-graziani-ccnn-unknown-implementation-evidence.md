# 1st CCNN HQ: unknown-state implementation evidence

**November 17, 2026.** Source-to-content handoff for an implementation agent. This note selects no interpretation, changes no ruling or data, and does not establish readiness. It synthesizes the [source check](2026-11-17-graziani-blocker-ccnn-hq.md), [independent review](2026-11-17-graziani-blocker-ccnn-hq-review.md), and [policy critique](2026-11-17-graziani-ccnn-gun-capacity-policy-review.md); these review the same scanned source set, not independent editions. The absence of a finding is bounded to those checks.

## Minimum facts and provenance to carry

The following are semantic requirements for a machine-readable representation, not a prescribed schema or new source values. Bind each fact or unresolved assertion to its source locator and scanned-document identity; preserve raw text alongside any normalized meaning. A machine-readable state must distinguish explicitly unknown from zero, absent, invalid, or omitted data.

| Subject | Source-grounded content | Locator and boundary |
| --- | --- | --- |
| HQ OA row | Unit `1 CCNN`; characteristics ID `g`; raw TOE and weapons entry `N`; arrival `D`. | [Axis OA](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf), PDF p. 17. Preserve `N` verbatim; it is not a weapon model. |
| OA key | For non-tank/non-gun units, `N` denotes possession of the characteristics maximum on arrival/deployment. The same key requires gun TOE points, including those assigned to other unit types, to be listed by number and weapon system. | Axis PDF p. 4. Preserve both instructions and their unresolved tension; do not silently turn either into an initial count. |
| Characteristics | ID `g` has maximum TOE capacity 1 and permits assignment of one artillery TOE point; no weapon system is named. | Axis §4.46b, PDF p. 26; maximum definition PDF p. 27. Capacity and permission are distinct from initial presence. Artillery is the permitted TOE category, not an identified weapon system or its characteristics. |
| Initial assignment | Presence/count remains unresolved; a literal `N` plus `g` reading supports one present point, while the missing required listing leaves assignment unresolved. Weapon identity remains unknown under either reading. | Axis PDF pp. 4, 17, 26–27. Neither zero nor one present point is an uncontested printed assignment. |
| Corroborating checks | Counter face and checked errata do not settle the assignment; scenario deploys the division but does not identify its HQ weapon or point. | [Counter](https://spigames.net/PDFv10/CNA_Counters.pdf) PDF p. 2; [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) PDF pp. 1–4; [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) PDF p. 3 §60.31. These checks do not prove no other clarification exists. |

Source identity for the reviewed scans: Axis SHA-256 `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`; counter `bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9`; errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`; scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`. Inspection scope and limitations appear in the source check.

## Selection boundaries

- **Source fact:** raw `N`, its key meaning, capacity and permitted category. None identifies a weapon or proves a zero-point assignment.
- **Explicit unresolved state:** initial presence/count and weapon identity are unknown. Preserve the possible one-point reading and the competing unresolved-assignment reading as evidence, not as simultaneous current counts.
- **Optional project interpretation:** one present, unidentified artillery point is a possible *inference*, not a printed count. Selecting it requires separate review and a versioned policy; this note does not select it. Do not assign 65/17 from the separate Legion HQ rows, zero, a generic usable gun, or weapon-dependent values.
- **Admission versus operation:** accepting an unknown into content is distinct from authorizing an action or clearing readiness. Until an applicable reviewed policy defines safe behavior, consumers needing unknown facts remain unresolved. This includes combat, movement, supply, losses, transfer, counts/capacity, targeting, replacement, and visibility. No outcome or fallback is prescribed here.
- **Identity and history:** retain evidence and any separately selected policy as distinct, version-bound inputs. A changed interpretation must not silently reinterpret existing saves or replay identity; migration and later evidence need explicit treatment. Unknown representation must not disclose hidden opposing state.

## Focused source-to-content test matrix

These are acceptance checks for whichever representation and policy are separately approved, not claims about existing tests or a decision to clear a blocker.

| Case | Evidence/input | Required assertion |
| --- | --- | --- |
| Raw provenance | OA row and key | Round-trip `N` unchanged with row/key locators and source identity; it is not parsed as a weapon model. Preserve both key instructions. |
| Capacity versus presence | `g`, maximum 1, permitted artillery point | Capacity and permission survive independently of initial count; neither alone creates an occupied point or known-zero assignment. |
| Unknown initial assignment | Bounded source set, no selected policy | Explicit unknown count/presence and unknown weapon survive load and round-trip; neither becomes zero, absent, one, or an invented weapon. Preserve possible one-point reading and conflict. |
| Possible selection, if separately approved | Versioned one-present-point interpretation | Selection records one as an inference with scope and policy identity; source evidence remains unchanged, and weapon identity and characteristics remain unknown. No implicit generic weapon or derived rating. |
| No cross-unit borrowing | Separate Legion HQ `65/17` entries | Division HQ gains no weapon or rating from those entries. |
| Unknown-dependent consumers | Unknown assignment or unknown weapon attributes | Each affected operation follows its separately defined safe policy; unknown never silently becomes zero or an operational value. Cover combat, movement, supply, losses, transfer, counts/capacity, targeting, replacement and readiness. If no such policy exists, retain unresolved status. |
| Serialization and replay | Same content, unknown state and version-bound policy | Round-trip preserves distinctions and provenance; same inputs reproduce identity and outcome. A policy/content change is detectable, and existing saves are not silently reinterpreted. Test any explicit migration separately. |
| Visibility | Friendly and opposing projections with unknown source state | Unknown handling preserves applicable fog-of-war boundaries; no hidden opposing state leaks through representation or errors. |
| Evidence revision | Later supported evidence or separately reviewed selection | New evidence/policy is distinguishable from old source and prior campaign identity; changes follow explicit version and migration behavior rather than rewriting history. |

No runtime or pinned content changed; no gameplay tests ran.
