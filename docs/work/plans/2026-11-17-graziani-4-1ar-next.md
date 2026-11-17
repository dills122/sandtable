# SU-02: repeated `4/1AR` in initial deployment

**Provisional planning choice:** represent one `4/1AR` battalion identity,
carrying both scenario occurrences as provenance; leave the missing separator
and reason for the repeated text unresolved. This is supported by the single
OA row and a matching printed counter, not an explicit erratum for `4/1AR`.
Do not create two independent battalions or replace the repeat with a guessed
different unit. Complete source reconciliation remains necessary before
runtime setup admission.

## Source evidence

* [Scenario §60.31](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
  printed p. 27 / PDF p. 3, has the normalized sequence `1/1AR; 2/1AR;
  3/1AR; 4/1AR; XXI Cp; XXVI Cp 4/1AR; XXI Cp (Ar).` Both `4/1AR` strings
  are visible; between `XXVI Cp` and the second string is whitespace with no
  visible semicolon. Both appear within the “Anywhere in Libya” entry. The
  two mentions by themselves do not establish two different unit identities.
* [Axis OA](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf), PDF p. 16,
  lists one row for **4th Bn, 1 Arty Rgppmnto**, counter abbreviation
  `4/1AR`, ID `mm`, `3 × 100/17 Howitzer`, arrival `D`. Neighboring rows
  `1/1AR`, `2/1AR` and `3/1AR` are distinct; OA note `c` says the grouping
  is administrative, its battalions independent and without a formal HQ.
  OA key p. 4 defines `D` as deployed at GT1 OpStage 1. This is one row in
  the inspected OA section, not proof no later variant exists elsewhere.
* [Counter scan](https://spigames.net/PDFv10/CNA_Counters.pdf) PDF p. 1,
  Counter Section 2 left block, shows `1/1AR`, `2/1AR`, `3/1AR` at the end of
  one row and `4/1AR` as the first item of the next, followed by `1/2AR`
  through `4/2AR`. The `4/1AR` counter visibly has artillery symbol,
  battalion mark and printed stacking value 1; the section header says one
  section of that type. This is positive physical-counter evidence, not an
  exhaustive search for every duplicate or substitute counter in the game.
* [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
  PDF p. 3 §60.31 expressly calls out **XXI Corps Artillery** as listed twice,
  not `4/1AR`. A bounded text search of the acquired errata found no `4/1AR`
  correction; extraction and other editions may be incomplete. Do not extend
  that explicit correction to a unit it does not name.
* Other-scenario check: printed p. 30 / PDF p. 6 §61.41 includes **2nd**
  Artillery Raggruppamento in a later setup, while the inspected OA separately
  lists `1/2AR` through `4/2AR` as later arrivals `1/26`. This does not fix
  the repeated initial `4/1AR` or prove an omitted intended replacement.
  No exhaustive other-scenario or later-errata search is claimed.

The checked scan hashes are scenario
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`,
Axis OA `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`,
counter scan `bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9`,
and errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.

## Choices and tests

The proposal retains one initial independent battalion, three stated howitzer
TOE points, and the owner-selected legal Libya placement; it does not assert
which printed mention is erroneous. Do not join `4/1AR` to `XXVI Cp` as an
attached child based on absent punctuation: the OA says independent, and
scenario text does not explicitly create that relationship. Do not substitute
`4/2AR`: its distinct counter and later OA arrival do not supply a correction
to the earlier line. A possible omitted different unit, an alternate printing,
or additional correction remain unproven rather than disproved.

Test two raw source references resolving to one canonical identity, one
placement decision and one 3-point inventory; preserve source locators and
conflict flag. Test the separate XXI erratum independently, 4/2AR's distinct
identity and later date, stacking/placement legality, and opponent-view
privacy. Reject a duplicate placement without erasing either transcription.
No runtime changes, gameplay tests or scenario reachability claims.
