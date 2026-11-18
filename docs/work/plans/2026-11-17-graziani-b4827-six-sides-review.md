# Map B B4827: independent visual review

**Result:** Map B reading and six neighboring IDs agree with the scan. One
reproduction issue: the stated key/summary crop cuts off the Benghazi row, and
its listed PNG hash does not match the crop reproduced here. This does not
change the coordinate conflict; it limits what that particular crop shows.

Reviewed [map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 1–2,
and [scenario scan](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
PDF p. 3 / printed p. 27. Map B's printed `4827` is inland, covered with
yellow rings and stippling matching the key's Rock/Gravel sample. No Benghazi
label, major-city blocks or port symbol are visible in that cell. In the
unrotated crop, its neighbors clockwise from upper left are `4927`, `4928`,
`4828`, `4728`, `4727`, `4826`. `4927` appears clear; the other five show the
same patterned terrain. No distinct feature crossing is visible on the six
shared sides. Fine ink and scan quality limit negative observations; these do
not establish movement or supply legality.

Map A's location summary lists Benghazi `A4827`, marked major city, port and
victory condition. The named city and port are visible on Map A; the city
hex's own number is obscured by overprinting. Scenario §60.31 instead prints
`Benghazi (B4827): Benghazi Garrison`. These are conflicting printed facts;
this inspection supplies no official correction or seam transform.

Pinned `hexes.v1.json` gives B4827 gravel, no settlement and noncoastal;
`hexsides.v1.json` has no B4827 entry. Absence of a side record does not prove
absence of a feature. The pinned Vassal-derived transcription is a comparison,
not independent confirmation of the map scan.

Reproduced the B crop using the reviewed note's command: SHA-256
`00fbd7171e20f757a5430efadf9112eedb9c5dc4b6eb6339c18d9e786d528160`.
Reproducing its key/summary command yielded
`60bb1273946b98fe770923349fe808ad6e167cd1f23b0feb90e632cdcfa10f3c`,
not the listed `493f44db4aecedacde21667af7d35492ad1871ff32c3854bf51f8cdc383d8ec3`;
renderer differences can affect hashes. The following separate crop includes
the complete summary and Benghazi row:

```sh
pdftoppm -f 1 -l 1 -scale-to 7000 -x 850 -y 2500 -W 1500 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/b4827-independent-summary
```

Its SHA-256 is `93a67ce6178021fdfec7090c9e4a57fa6a331824c08dd111eafc8bc1cb1a5f46`.
The map PDF hash matches the reviewed note; scenario PDF SHA-256 is
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
Same scan as prior work, so this is informed reinspection, not a separate
edition. No runtime changes or tests.
