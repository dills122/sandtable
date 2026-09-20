# Author Explanation — C Weather

## Intent and plan
Implement dormant Task008 C after accepted B1, preserving canonical Weather successor2 and private
WeatherState1 bytes. C resolves one turn1/stage1 event through Organization entry; B2 consumes actual
accepted Weather next. No public gameplay or general Snapshot12 admission.

## Flow and components
CampaignCombatWeatherModels owns immutable command/input/event and projection with accepted-event
receipt. CampaignCombatWeather.Replay requires exactlyfour B1 events plus zero-or-one Weather record;
B1 revalidates trusted creation. Fixed Weather artifact hash, Rules10 manifest sources and explicit
no-immediate-effect-subjects policy checked before initial projection. Emit uses existing
Cna1979Weather.Resolve and SandtableRandom; derived Weather/position/sources/cursor are serialized,
then entire imported event compared. ReadState compares full reconstruction. Apply authorizes System
before exact retained-input lookup; retry returns original event/current state without extra draw.
CampaignCombatWeatherCodec owns compact canonical input/event/state order, strict input decode and
wth receipt derivation. Reuses accepted D1 prefix framing and immutable nested Weather1/receipt types.
Tests and fixture project link provide34 frozen creation-rooted chains and68 replay cuts.

## Choices and invariants
Reusing established Rules/table/RNG avoids inferred tables and preserves rejection sampling. Source
artifact pinned to frozen hash and manifest; supported context remains fixed via B1. Axis determines
Weather for both initiative choices. All outcomes retained, including Hot/foul. No immediate subjects
means explicit zero counts, not inferred absence. World/holder/order survive unchanged; five receipts
remain chronological, current cursor and prefix advance once. Imported history is bounded to one
creation/fourprelude/oneWeather,1MiB per record/depth32; exact comparison rejects extraneous structure.
Caller bytes copied before retained event reconstruction; no mutable arrays kept as authority.

## Verification and challenge points
See c-evidence.md for final executed results. Tests compare306 frozen byte-count/SHA fingerprints,
separate literal dice/kind/location/cursor expectations, all12 foul-location pairs and rejected-byte
seeds81/max. Independent receipt/prefix formulas, wrongactor/changed-occurrence, partial/foreign
prelude, raw/scalar/valid-format forgery and caller-buffer cases included. A plausible seed1 Normal
payload transplanted into seed0 Rainstorm authority is re-signed along with cache/prefix and still
must reject. Creation-only Snapshot12 and legacy readers reject successor/private-state evidence.
Lead dev review found no actionable source defect; requested explicit Snapshot12 and additional raw
reorder/number negatives before freeze. Worker RED/GREEN and full integration results recorded when
complete. Apply extra skepticism to actual RNG-derived authority, fixed artifact sources, ActLast
holder, preserved non-Normal results, receipt ordering and coherent forged histories.

## Costs, limits and deferrals
Full bounded replay on each call is intentional; no cache can establish authority alone. Several
small canonical writers mirror inherited private shapes; do not generalize a Snapshot reader here.
Checksums are not authentication, actor metadata requires trusted ingress, and provider published
head/durability remain HOST-PUB-001. Positive immediate effects, later turns/stages, B2/Reserve and
full retained-history restore remain explicit later owners. Canonical oracle passing is contract
evidence, not substitute for .NET runtime tests. No old source files or fixture goldens changed.
