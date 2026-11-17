# D2228–D2328 escarpment: source review

**Disposition:** feature presence is explicitly corrected; direction is not
established by the checked erratum or an unambiguous full symbol on that side.
The proposed `up = D2328` is plausible continuity, not a printed fact. Preserve
unknown direction separately from known escarpment presence.

Visually rechecked [Map D](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 4
crop around 2227–2229 and 2328–2329, scan SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
[September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF p. 1 MAPS entry, `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`;
and [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 14–15 and 18 §§8.35, 8.42, 10.21,
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
This uses the same source edition and an existing crop, not an independent
physical copy or a search for every later map correction.

## Findings

* The erratum explicitly adds an escarpment to Map D hexside **2228/2328**;
  it does not state high side. The crop locates 2328 above 2228 across their
  diagonal side and shows a substantial unmarked stretch of that side. The
  dark mark at its right-hand vertex joins a nearby printed feature; it is
  not an unambiguous completed splash along the corrected edge. Qualify any
  absolute statement that the edge has no dark ink at all: no usable complete
  direction glyph is established on the corrected side.
* On neighboring 2328/2227, the printed splash extends into 2227, so §8.35
  identifies 2227 as the down side of **that** edge. This supports a plausible
  continuity hypothesis with 2328 high on the corrected edge, but does not
  prove it; the crop cannot itself provide the erratum's missing direction.
* §10.21(b) says ZOCs do not extend **through escarpment hexsides**, without
  requiring an up/down choice. For this corrected edge, that specific
  no-projection result holds in both directions once geometry and source
  correction are admitted; it does not establish no other enemy ZOC in either
  hex, unit occupancy, or actual route reachability.
* §8.42 prohibits vehicles moving **up** an escarpment; a track crossing permits
  vehicles to move **down**, at the stated +8 CP and six Breakdown Points.
  The local scan does not show a track crossing the corrected shared side;
  the visible dashed track in 2328 is not itself a crossing of that side.
  Independent edge/terrain coverage still matters. Do not apply a downward
  track cost as if the source had established such a connection, and do not
  infer that unknown direction alone always settles vehicle travel legality.

## Tests before map admission

1. Correct both reciprocal edge records to escarpment, preserving the erratum
   locator and `up = unknown`; check direct ZOC projection is blocked in both
   directions without suppressing ZOCs originating through other sides.
2. Compare hypothetical high-side choices against a genuinely proven
   connecting track and without one; verify up movement prohibition and the
   conditional down-track cost. Do not treat a neighboring dashed line as a
   connection without exact endpoint and side evidence.
3. If directional movement or combat needs the high side, return unsupported
   until a reviewed inference or further source fixes it. Test exact side,
   adjacent-side splash, symmetric feature provenance, and replay.

No runtime changes or tests; not a full terrain, later-errata or route audit.
