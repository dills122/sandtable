# Graziani source double-entry pilot

**Status:** research aid with two attributed readings. The 11-row manifest narrows review
to specific Matruh, 6th NZ and recovered-formation facts. It is not a complete
roster, accepted source contract, rights clearance or release gate. The initial
reading is the work of the researcher who prepared the existing register. A
fresh-context independent agent completed the second reading blind to the first;
this is not a separate human reviewer or proof of source rights. See
[review disposition](review-disposition.md).

## Independent review protocol

1. A different reviewer accesses the four sources identified below, verifies the
   SHA-256 values in `manifest.json`, and independently reads the original pages
   at each manifest row's source. Avoid consulting `reading-a.json` until the new
   entry is complete. A source locator should identify the printed page or PDF
   page plus rule, table, row or formation.
2. Copy `reading-template.json` to a separate file; it contains only manifest
   fields, unknown values and unverified locators, not the first reading. Create
   a separate JSON reading with `version`, `scope`, a distinct stable
   `reader_id`, `manifest_sha256`, `sources` and `rows` as in the schema below.
   Use the exact IDs and fields in `manifest.json`; fill values from the source,
   not from an existing reading. Record uncertainty as JSON `null`, never `0`,
   `false` or an empty string. Booleans mean whether the named printed statement
   or marker is present; they are not assertions that the statement is correct.
3. Bind the reading to the manifest's canonical SHA-256. To compute it without
   importing the original reading:

   ```sh
   python3 -B -c 'import json,sys; from pathlib import Path; sys.path.insert(0, "docs/work/plans/graziani-source-review"); from compare_readings import manifest_hash; print(manifest_hash(json.loads(Path("docs/work/plans/graziani-source-review/manifest.json").read_text())))'
   ```

4. Compare both readings with `compare_readings.py`, supplying each actual source
   file as `--source ID=PATH`. Use quoted absolute paths if they contain spaces.
   The tool hashes the bytes and rejects missing, altered or mismatched sources.
   Inspect every disagreement, differing locator, missing/extra field and unknown;
   keep both original entries and record a sourced disposition separately. A
   locator mismatch requires review even if two labels describe the same place;
   matching labels still need human verification against the actual source. A
   source conflict between two different rules remains a conflict even if both
   readers agree on it.
5. Obtain a reviewer disposition, rights/provenance decision and any necessary
   versioned ruling before source data is used in production. An empty issue list
   means only that two attributed readings agree within this manifest. Reader IDs
   are self-declared: software cannot establish that they are different people or
   that either reading is correct.

## Reading format

Each `rows` entry contains a manifest row ID, a nonempty source locator, and a
`values` object whose field names exactly match that row's manifest. Permitted
values: JSON integers, booleans, nonempty strings, or `null` for unknown. Keep
coordinates and markers as strings. Example shape with deliberately fictitious
values and digest:

```json
{
  "version": 1,
  "scope": "graziani-matruh-nz-formations-v1",
  "reader_id": "independent-reviewer",
  "manifest_sha256": "use-the-computed-manifest-digest",
  "sources": {"copy-the-exact-manifest-sources": "and-their-digests"},
  "rows": [{"id": "example", "locator": "page and case", "values": {"field": null}}]
}
```

The example is illustrative, intentionally not a valid reading. Add every
manifest row and field. The untouched template is also intentionally incomplete:
against reading A and the verified source bytes it reports 34 unknown fields,
11 differing and 11 unverified locators, and an unverified reader, with exit `1`.

Field interpretation is intentionally narrow:

- `hex` is the printed section-and-hex identifier; the three `*_attached`
  booleans ask whether each named unit is listed as attached in the scenario.
- `buffs_assigned` and `hampshires_assigned` ask what the errata explicitly says;
  `no_assigned_units`, `attachment_max` and `movement_assault_exception` ask what
  the Matruh OA itself prints, before reconciling the conflict with errata.
- `turn` and `stage` mean game turn and operation stage, in that order. `light`,
  `medium` and `heavy` are the NZ brigade row's listed truck points, not an
  inferred Land-only conversion. The example row asks what §20.11 actually
  prints even if it conflicts with the schedule.
- `listed_rows` counts the individual printed OA rows, including headquarters
  and explicitly listed tank attachments; it does not count personnel or all
  scenario counters. `deployed_marker` is the repeated printed arrival marker;
  `tank_attachment` is the tank battalion designator in the OA note.

The NZ brigade OA row count covers its HQ and three listed battalions. A source
reader who cannot verify a value should leave it `null` and explain uncertainty
in the review disposition. No field asks the reviewer to adopt a ruling.

Compare command shape:

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-source-review/manifest.json \
  docs/work/plans/graziani-source-review/reading-a.json \
  /private/tmp/independent-reading.json \
  --source scenario=/private/tmp/sandtable-scenarios.pdf \
  --source errata=/path/to/ERR_CampaignforNorthAfrica.pdf \
  --source commonwealth-charts=/private/tmp/sandtable-cw-charts.pdf \
  --source land-rules=/path/to/CNA_LandGameRules.pdf
```

Exit `0` means no disagreements, placeholder reader/locator, differing locators,
missing/extra records or unknowns; `1` reports such issues; `2` means invalid input or source bytes. The
report includes both attributed locators for every expected row. Comparison is type-sensitive:
`true`, `1`, `0` and `null` are distinct. Duplicate JSON keys and row IDs reject.
The tool does not adjudicate the discrepancy or change the manifest or source.

The checked-in `reading-a.json` was validated against the four actual source
hashes. Passing it as both input readings correctly exits `2` because the reader
identities are identical. This verifies the fail-closed path; it is not a second
reading. Reader names are attestations, not a security mechanism.

## Source package references

- `scenario`: [scenario scan](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
  printed p. 27, §60.41.
- `errata`: [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
  PDF p. 1 §4.44b item 2 and PDF p. 4 embedded OA image rows.
- `commonwealth-charts`: [Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf),
  PDF p. 3 schedule, p. 10 Matruh and p. 18 2nd NZ OA.
- `land-rules`: [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
  printed p. 31 §20.11.

Retrieved scans remain outside Git; links and hashes identify material, not chain
of custody or a redistribution license. The archived errata and Grognard GIFs
contain pixel-identical OA images; they do not supply independent scan evidence.

Focused self-tests:

```sh
python3 -B -m unittest discover -s docs/work/plans/graziani-source-review -p 'test_*.py' -v
```
