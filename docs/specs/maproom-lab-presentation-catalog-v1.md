# Rules-lab Maproom presentation catalog v1

Owner: `Cna.OrleansHost/Api/TemporaryPlayerViewProjection.cs`. Catalog version:
`breakdown-truck-maproom.v1`. Approved scope: development-only `host-rsh-001` initial cut.
**Proposed MAP-10a expansion, awaiting owner approval:** the exact early successor cuts
below from the same truck fixture. The implementation remains draft until that public
scope is approved. This is
public presentation data, not movement topology or adjudication authority. The layout below
is an editorial coordinate arrangement for the nine published fixture locations; Core content
has no source coordinates. The adapter requires ruleset hash
`17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a`,
`breakdown-truck-lab` scenario, setup hash
`sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3`,
and content hash `sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee`.
Unknown pins or missing locations fail closed.

| Location | Label | x | y |
| --- | --- | ---: | ---: |
| center | Center | 0 | 0 |
| east | East | 1 | 0 |
| north | North | 0 | -1 |
| north-east | North East | 1 | -1 |
| north-west | North West | -1 | -1 |
| south | South | 0 | 1 |
| south-east | South East | 1 | 1 |
| south-west | South West | -1 | 1 |
| west | West | -1 | 0 |

The four fixture formations have side-specific labels (`Axis infantry`, `Axis truck`,
`Commonwealth infantry`, `Commonwealth truck`) and kinds (`Infantry` or `Truck convoy`).
Only own formations present in the permitted side cut are projected. Their outward IDs are
session-scoped HMAC handles. `None`, `ReserveI`, and `ReserveII` are the only admitted
reserve/order labels (`No reserve designation`, `Reserve I`, `Reserve II`). Contacts use the generic label
`Unidentified presence` and tracker-issued opaque IDs. The catalog never supplies unit
location, contact visibility, legal action, or hidden identity.

The approved initial cut has an empty War Diary and cursor `0`. Creation precedes trusted
System progression, so it has no human actions and reports `unsupported-phase`.

## Proposed bounded successor read

The host-private tracker must first authenticate every accepted step, exact fixture
source, both side cuts, receipt and common committed head. The public adapter admits
only the following `normal` turn-one cut shapes. Each row has the exact position,
phase and stage shown; an alternate position, state version, source or decision kind
returns audience-neutral `503 unavailable`.

| State version | Position suffix after `land.position.` | Phase suffix after `land.phase.` | Stage suffix after `land.stage.` | Axis actions | Commonwealth actions |
| ---: | --- | --- | --- | --- | --- |
| 2 | `naval-convoy.schedule` | `naval-convoy-schedule` | `naval-convoy` | none | none |
| 3 | `naval-convoy.tactical-shipping` | `tactical-shipping` | `naval-convoy` | none | none |
| 4 | `operation-1.initiative-declaration` | `initiative-declaration` | `operation` | `act-first`, `act-last` | none |
| 5 | `operation-1.weather-determination` | `weather-determination` | `operation` | none | none |
| 6 | `operation-1.organization` | `organization` | `operation` | none | none |
| 7 | `operation-1.naval-convoy-arrival` | `naval-convoy-arrival` | `operation` | none | none |
| 8 | `operation-1.commonwealth-fleet.assignment` | `commonwealth-fleet` | `operation` | none | none |
| 9 | `operation-1.commonwealth-fleet.repair` | `commonwealth-fleet` | `operation` | none | none |
| 10 | `operation-1.first-player.reserve-designation` | `reserve-designation` | `operation` | `complete-reserve-designation`, `designate-reserve` | none |

Versions 2–9 cover the authenticated early truck positions after their accepted
steps; the version-10 action shape admits the Axis-first reserve cut only. This
does not publish a generic successor or alternate scenario. The exact v1
source tuple above remains mandatory. At versions 2–4 both diaries have cursor
`0`. At versions 5–10 Axis has exactly one fixed
`diary.initiativeOrder` entry and cursor `1`; Commonwealth stays at cursor
`0`. The title/body must match the approved `en-CA` diary v2 catalog,
and the outward entry ID is session scoped. No source event text, private
formation ID, receipt or hidden contact identity is copied into the response.
Actions use only existing opaque, session-scoped IDs and are **display data**:
there is no public action submission or receipt route.

The adapter projects the side's own formations, apparent contacts, action set,
diary and cursor from one tracker cut; it does not combine separately refreshed
fields. State 11 and later, alternate action sets, malformed diary/cursor, missing
formation facts, wrong seat or changed session generation fail closed. No terminal
result is inferred. The proposal does not establish browser orders, later phase
views, terminal classification, or durable recovery.
