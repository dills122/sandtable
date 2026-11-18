# Independent review: Benghazi A4827 six sides

**Result: correction needed.** Map A's terrain key labels the double-dashed
symbol **Unfinished Road**, not “unimproved road.” Correct the key inventory in
the source note. The existing “track-like” qualification near the city remains
appropriate: distinguish the single heavy dashed track symbol from double-dashed
unfinished road and from overprinted railroad before attributing a crossing.

The listed key/context crop clips most of the terrain key and excludes the
Summary of Important Locations. The source note's key and summary claims can
be reproduced from the same unmodified PDF with these additional crops:

```sh
pdftoppm -f 1 -l 1 -scale-to 7000 -x 1500 -y 200 -W 1400 -H 1050 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/a4827-review/fullkey
pdftoppm -f 1 -l 1 -scale-to 7000 -x 900 -y 2500 -W 1450 -H 850 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/a4827-review/summary
```

Independent visual check of PDF p. 1 and the city crop: the six named neighbors
occupy the stated positions. Four sea-facing sides show no clear route crossing
in their exposed portions, although the upper-right vertex is crowded. The
coastal road and blue rail lines near the right and lower-right sides overlap
city, training and other marks. Rail and dashed lines extend into `4828` and
`4728`, but this image alone does not certify every crossing at the junction.
The lower-left coast reaches the side near its lower vertex. No movement
legality follows from these observations.

The city's printed number is obscured; surrounding numbers and the map summary
support its identification as `A4827`. The summary marks Benghazi as major city,
port and victory condition, with a dash under assigned garrison. The key matches
the soldiers to training area, the anchor to port, and blue blocks to major city;
overprint limits separation of the symbols in the city itself. The referenced
scenario `B4827` conflict is retained as reported; this review did not
independently inspect the scenario text or decide a publisher correction.

Checked pinned JSON directly: the seven hex records and two `A4827` side
records match the note. `A4728|A4827` lists road; `A4827|A4828` lists road and
railroad; both side and up are null. No other side record names `A4827`. This
file-content check cannot prove absent artwork or legal access. The rail toward
`4728` remains an appropriate follow-up, not a certified correction.

PDF SHA-256 and both published crop hashes reproduced. No runtime changes or
tests; review limited to source and pinned-data comparison.
