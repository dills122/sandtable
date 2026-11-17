# XXI Corps artillery: source audit

**Disposition:** [corps-artillery note](2026-11-17-graziani-corps-artillery-next.md)
matches the checked scenario, erratum, OA rows and local counter row. Its
single-XXI interpretation is well supported, while the second `4/1AR` and
missing separator remain separate unresolved source defects. The scan shows
one XXI in the inspected row, not an exhaustive physical-counter inventory.

Visually checked [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
printed p. 27 / PDF p. 3 §60.31, SHA-256
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`;
[September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF p. 3 §60.31, `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`;
[Axis OA](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf) PDF pp. 4,
16, `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`;
[counter scan](https://spigames.net/PDFv10/CNA_Counters.pdf) PDF p. 1,
`bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9`;
and [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 44–45 §32.83, `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Same-edition source pass; no later errata or full counter-sheet search.

## Cross-check

* Scenario line has `1/1AR; 2/1AR; 3/1AR; 4/1AR; XXI Cp; XXVI Cp 4/1AR; XXI Cp (Ar).`
  The scan has a space but no visible separator between `XXVI Cp` and the
  second `4/1AR`. Errata §60.31 explicitly says XXI Corps Artillery appears
  twice; it does not expressly fix the repeated `4/1AR` or supply missing
  punctuation. Treating the two XXI mentions as one identity is supported;
  claiming the erratum resolves both issues is not.
* OA lists **XXI Cp** (`kk`) as 3 × 75/27 gun, 6 × 100/17 howitzer, 3 ×
  Light AA; **XXVI Cp** (`kk`) as 9 × 75/27 gun, 3 × Light AA; **X Cp**
  (`kk`) as 4 × 100/17 howitzer; **4/1AR** (`mm`) as 3 × 100/17 howitzer.
  All four rows bear `D`; OA key defines it as deployed in GT1 OpStage 1.
  Note `c` makes the raggruppamento administrative, with no formal HQ and
  independent battalions; note `d` places X at Soluch but says not garrison.
* In the inspected counter row, `XXICp`, `XXIICp`, `XXIIICp`, `XXVCp`,
  `XXVICp`, `XX Cp` and `X Cp` are distinct visible labels. XXI and XXVI each
  carry artillery symbol, regiment mark and printed stacking value 1; `XX Cp`
  has Benghazi garrison marking. This supports the OA distinction, not a
  chosen Libya hex, exhaustive counter inventory, or a reading that merges
  XXI with XXVI. `XXV` is distinct from `XXVI`.
* Land-only §32.83(3) removes assigned **heavy** AA TOE from Italian `kk`
  artillery; the OA rows in question list **light** AA, so that clause does
  not itself remove their listed light AA. §32.83 has other category-specific
  provisions; this finding is not a blanket guarantee that all AA survives.

## Conditional checks

1. Parse both XXI occurrences with erratum provenance as one source identity;
   require a legal player-selected Libya placement and stacking before runtime.
2. Retain XXI, XXVI, X and 4/1AR as distinct identities and verify exact OA
   quantities. Preserve second `4/1AR` and its missing separator as unknown;
   do not create a second copy or claim the XXI erratum corrects that text.
3. Apply heavy-AA-only `kk` removal to synthetic mixed light/heavy entries;
   retain listed light entries under that clause and test other applicable AA
   rules separately. Keep private placement out of opponent-facing responses.

No implementation changes or gameplay tests; scenario placement and complete
counter availability remain separate checks.
