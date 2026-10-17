# Rules-lab Maproom presentation catalog v1

Owner: `Cna.OrleansHost/Api/TemporaryPlayerViewProjection.cs`. Catalog version:
`breakdown-truck-maproom.v1`. Scope: development-only `host-rsh-001` initial cut. This is
public presentation data, not movement topology or adjudication authority. The layout below
is an editorial coordinate arrangement for the nine published fixture locations; Core content
has no source coordinates. The adapter requires the exact ruleset, `breakdown-truck-lab`
scenario, setup hash `sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3`,
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

No accepted-step diary source is approved. The initial cut has an empty War Diary and cursor
`0`. Creation precedes trusted System progression, so the initial cut has no human actions and
reports `unsupported-phase` to the browser. No terminal result is inferred. Any successor,
unsupported action/state, missing formation fact, or source mismatch returns
audience-neutral `503 unavailable`. This slice does not establish browser campaign creation,
post-step view, action submission, terminal classification, or durable recovery.
